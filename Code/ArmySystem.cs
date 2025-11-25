using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using System.Linq;

namespace RulerBox
{
    public enum ArmySelectionMode
    {
        Normal,     // Standard control
        Recruit,    // Drag over civilians -> Recruits
        Dismiss     // Drag over soldiers -> Dismiss
    }

    public static class ArmySystem
    {
        private class GuardOrder
        {
            public WorldTile HomeTile;
            public int RadiusTiles;
        }

        private static readonly Dictionary<Actor, GuardOrder> _guardOrders = new Dictionary<Actor, GuardOrder>();
        private static readonly List<Actor> _lastSelection = new List<Actor>();
        private static readonly MethodInfo _miSetTileTarget = AccessTools.Method(typeof(Actor), "setTileTarget", new[] { typeof(WorldTile) });
        
        private const int DefaultGuardRadiusTiles = 5;
        public static ArmySelectionMode CurrentMode = ArmySelectionMode.Normal;

        public static void Tick(float dt)
        {
            if (Main.selectedKingdom == null) return;

            HandleHotkeys();
            UpdateGuardOrders();
        }

        private static void HandleHotkeys()
        {
            // Mode Switching (Single Button Cycle)
            if (Input.GetKeyDown(KeyCode.B))
            {
                switch (CurrentMode)
                {
                    case ArmySelectionMode.Normal:
                        CurrentMode = ArmySelectionMode.Recruit;
                        break;
                    case ArmySelectionMode.Recruit:
                        CurrentMode = ArmySelectionMode.Dismiss;
                        break;
                    case ArmySelectionMode.Dismiss:
                        CurrentMode = ArmySelectionMode.Normal;
                        break;
                }
                ShowModeTip();
            }

            // Guard Commands (Only in Normal Mode)
            if (CurrentMode == ArmySelectionMode.Normal)
            {
                if (Input.GetKeyDown(KeyCode.N)) SetGuardPointForSelection();
                if (Input.GetKeyDown(KeyCode.M)) ClearGuardPointForSelection();
            }
        }

        private static void ShowModeTip()
        {
            string msg = "Mode: Normal (Control)";
            string color = "#FFFFFF";

            switch (CurrentMode)
            {
                case ArmySelectionMode.Recruit: msg = "Mode: RECRUIT (Drag civilians)"; color = "#7CFC00"; break;
                case ArmySelectionMode.Dismiss: msg = "Mode: DISMISS (Drag soldiers)"; color = "#FF5A5A"; break;
            }
            WorldTip.showNow(msg, false, "top", 2f, color);
        }

        public static void OnNewSelection(IEnumerable<Actor> actors)
        {
            _lastSelection.Clear();
            if (actors == null) return;

            var k = Main.selectedKingdom;
            if (k == null) return;

            // Filter list based on current mode
            List<Actor> processed = new List<Actor>();

            foreach (Actor a in actors)
            {
                if (a == null || a.hasDied() || a.kingdom != k) continue;

                if (CurrentMode == ArmySelectionMode.Normal)
                {
                    if (SoldierJobHelper.IsSoldier(a)) _lastSelection.Add(a);
                }
                else if (CurrentMode == ArmySelectionMode.Recruit)
                {
                    TryRecruit(a);
                }
                else if (CurrentMode == ArmySelectionMode.Dismiss)
                {
                    TryDismiss(a);
                }
            }
        }

        private static void TryRecruit(Actor a)
        {
            // Eligibility Check: Adult, not king/leader, not already soldier
            if (!a.isAdult()) return;
            if (a.isKing() || a.isCityLeader()) return;
            if (SoldierJobHelper.IsSoldier(a)) return;
            
            // Check City capacity (Hard limit - though we boosted cap via patch, it must exist)
            if (a.city == null) return;
            
            // Check Manpower (Currency)
            var d = KingdomMetricsSystem.Get(a.kingdom);
            if (d.ManpowerCurrent < 1) 
            {
                // Optional: Fail feedback
                return;
            }

            // Execute Recruit
            // Force job to warrior
            a.setProfession(UnitProfession.Warrior);
            // Update Manpower
            d.ManpowerCurrent--; 
            
            // FX
            a.startColorEffect();
        }

        private static void TryDismiss(Actor a)
        {
            if (!SoldierJobHelper.IsSoldier(a)) return;

            // Execute Dismiss
            a.setProfession(UnitProfession.Unit); // Revert to citizen
            
            // Refund Manpower
            var d = KingdomMetricsSystem.Get(a.kingdom);
            // Add 1/3 of a "manpower point" back. Since manpower is long/int, we can probabilistic add 1
            if (UnityEngine.Random.value < 0.33f)
            {
                d.ManpowerCurrent++;
            }

            // FX
            a.startColorEffect();
        }

        private static void UpdateGuardOrders()
        {
             if (_guardOrders.Count == 0) return;
             List<Actor> toRemove = new List<Actor>();

             foreach (var kvp in _guardOrders)
             {
                 var actor = kvp.Key;
                 var order = kvp.Value;

                 if (actor == null || actor.hasDied() || actor.kingdom != Main.selectedKingdom || !SoldierJobHelper.IsSoldier(actor))
                 {
                     toRemove.Add(actor);
                     continue;
                 }
                 if (order.HomeTile == null) { toRemove.Add(actor); continue; }

                 if (actor.current_tile != null)
                 {
                     int distSq = Toolbox.SquaredDistTile(actor.current_tile, order.HomeTile);
                     int maxSq = order.RadiusTiles * order.RadiusTiles;

                     if (distSq > maxSq)
                     {
                         try { actor.cancelAllBeh(); } catch {}
                         IssueMoveOrder(actor, order.HomeTile);
                     }
                 }
             }

             foreach(var a in toRemove) _guardOrders.Remove(a);
        }

        private static void SetGuardPointForSelection()
        {
            if (_lastSelection.Count == 0) return;
            WorldTile tile = World.world?.getMouseTilePos();
            if (tile == null) return;

            int count = 0;
            foreach(var a in _lastSelection)
            {
                if (a == null || a.hasDied()) continue;
                if (!_guardOrders.ContainsKey(a)) _guardOrders[a] = new GuardOrder();
                _guardOrders[a].HomeTile = tile;
                _guardOrders[a].RadiusTiles = DefaultGuardRadiusTiles;
                IssueMoveOrder(a, tile);
                count++;
            }
            WorldTip.showNow($"Guard point set for {count} soldiers", false, "top", 2f, "#9EE07A");
        }

        private static void ClearGuardPointForSelection()
        {
            if (_lastSelection.Count == 0) return;
            int count = 0;
            foreach(var a in _lastSelection)
            {
                if(_guardOrders.Remove(a)) count++;
            }
            WorldTip.showNow($"Guard cleared for {count} soldiers", false, "top", 2f, "#9EE07A");
        }

        private static void IssueMoveOrder(Actor actor, WorldTile tile)
        {
            if (actor == null || tile == null || _miSetTileTarget == null) return;
            try { _miSetTileTarget.Invoke(actor, new object[] { tile }); } catch { }
        }
    }
}