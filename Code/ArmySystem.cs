using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using System.Linq;

namespace RulerBox
{
    // Different army selection modes
    public enum ArmySelectionMode
    {
        Normal,     // Standard control
        Recruit,    // Drag over civilians -> Recruit
        Dismiss     // Drag over soldiers -> Dismiss
    }

    public static class ArmySystem
    {
        // Data structure for guard orders
        private class GuardOrder
        {
            public WorldTile HomeTile;
            public int RadiusTiles;
        }
        private static readonly Dictionary<Actor, GuardOrder> guardOrders = new Dictionary<Actor, GuardOrder>();
        private static readonly List<Actor> lastSelection = new List<Actor>();
        private static readonly MethodInfo miSetTileTarget = AccessTools.Method(typeof(Actor), "setTileTarget", new[] { typeof(WorldTile) });
        private const int DefaultGuardRadiusTiles = 5;
        public static ArmySelectionMode CurrentMode = ArmySelectionMode.Normal;

        // Main tick function
        public static void Tick(float dt)
        {
            if (Main.selectedKingdom == null) return;
            HandleHotkeys();
            UpdateGuardOrders();
        }
        // Handle hotkey inputs
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
        // Display current mode tip
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
        // Handle new selection of actors
        public static void OnNewSelection(IEnumerable<Actor> actors)
        {
            lastSelection.Clear();
            if (actors == null) return;
            var k = Main.selectedKingdom;
            if (k == null) return;
            List<Actor> processed = new List<Actor>(); // Filter list based on current mode
            foreach (Actor a in actors)
            {
                if (a == null || a.hasDied() || a.kingdom != k) continue;
                if (CurrentMode == ArmySelectionMode.Normal)
                {
                    if (SoldierJobHelper.IsSoldier(a)) lastSelection.Add(a);
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
        // Attempt to recruit an actor as a soldier
        private static void TryRecruit(Actor a)
        {
            // Eligibility Check: Adult, not king/leader, not already soldier, City capacity
            if (!a.isAdult()) return;
            if (a.isKing() || a.isCityLeader()) return;
            if (SoldierJobHelper.IsSoldier(a)) return;
            if (a.city == null) return;
            // Check Manpower (Currency)
            var d = KingdomMetricsSystem.Get(a.kingdom);
            if (d.ManpowerCurrent < 1) 
            {
                // Optional: Fail feedback
                return;
            }
            // Execute Recruit -> Force job to warrior
            a.setProfession(UnitProfession.Warrior);
            d.ManpowerCurrent--;             
            a.startColorEffect();
        }
        // Attempt to dismiss an actor from soldier role
        private static void TryDismiss(Actor a)
        {
            if (!SoldierJobHelper.IsSoldier(a)) return;
            a.setProfession(UnitProfession.Unit); // Revert to citizen
            var d = KingdomMetricsSystem.Get(a.kingdom); // Refund Manpower
            if (UnityEngine.Random.value < 0.33f)
            {
                d.ManpowerCurrent++;
            }
            a.startColorEffect();
        }
        // Update guard orders for soldiers
        private static void UpdateGuardOrders()
        {
            if (guardOrders.Count == 0) return;
            List<Actor> toRemove = new List<Actor>();
            // Iterate through guard orders
            foreach (var kvp in guardOrders)
            {
                var actor = kvp.Key;
                var order = kvp.Value;
                if (actor == null || actor.hasDied() || actor.kingdom != Main.selectedKingdom || !SoldierJobHelper.IsSoldier(actor))
                {
                    toRemove.Add(actor);
                    continue;
                }
                if (order.HomeTile == null) { 
                    toRemove.Add(actor); 
                    continue; 
                }
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
            foreach(var a in toRemove) guardOrders.Remove(a);
        }
        // Set guard point for currently selected soldiers
        private static void SetGuardPointForSelection()
        {
            if (lastSelection.Count == 0) return;
            WorldTile tile = World.world?.getMouseTilePos();
            if (tile == null) return;
            // Assign guard orders
            int count = 0;
            foreach(var a in lastSelection)
            {
                if (a == null || a.hasDied()) continue;
                if (!guardOrders.ContainsKey(a)) guardOrders[a] = new GuardOrder();
                guardOrders[a].HomeTile = tile;
                guardOrders[a].RadiusTiles = DefaultGuardRadiusTiles;
                IssueMoveOrder(a, tile);
                count++;
            }
            WorldTip.showNow($"Guard point set for {count} soldiers", false, "top", 2f, "#9EE07A");
        }
        // Clear guard points for currently selected soldiers
        private static void ClearGuardPointForSelection()
        {
            if (lastSelection.Count == 0) return;
            int count = 0;
            foreach(var a in lastSelection)
            {
                if(guardOrders.Remove(a)) count++;
            }
            WorldTip.showNow($"Guard cleared for {count} soldiers", false, "top", 2f, "#9EE07A");
        }
        // Issue move order to an actor
        private static void IssueMoveOrder(Actor actor, WorldTile tile)
        {
            if (actor == null || tile == null || miSetTileTarget == null) return;
            try { miSetTileTarget.Invoke(actor, new object[] { tile }); } catch { }
        }
    }
}