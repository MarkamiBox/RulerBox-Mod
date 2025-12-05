using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace RulerBox
{
    public static class EventsSystem
    {
        public static bool AllowPlayerWar = false;
        public static bool IsPlayerInitiated = false;
        private static float timeSinceLastEvent = 0f;
        private const float EventCooldown = 180f; // 3 Minutes
        
        // ruler event data structure       
        private class RulerEvent
        {
            public int Id;
            public EventButtonType Type;
            public Kingdom From;
            public Kingdom To;
            public War War;
            public Alliance Alliance;
            public GameObject ButtonGO;
            public string Message;
        }
        private static readonly Dictionary<int, RulerEvent> events = new Dictionary<int, RulerEvent>();
        private static int nextId = 1;
        
        // trigger a trade proposal event
        public static void TriggerTradeProposal(Kingdom other, string resourceId, int amount, bool isSelling, bool isBulk)
        {
            var player = Main.selectedKingdom;
            if (player == null) return;
            int price = TradeManager.CalculatePrice(resourceId, amount);
            string actionStr = isSelling ? "sell" : "buy";
            string freqStr = isBulk ? "once" : "every 2 min";
            string msg = $"Proposal: {actionStr} {amount} {resourceId} for {price}g ({freqStr}) with {other.data.name}?";
            EventsUI.ShowPopup(
                msg,
                EventButtonType.Diplomacy, 
                other,
                onOk: null,
                onAccept: () => 
                {
                    if (isBulk)
                    {
                        // One-time
                        Kingdom source = isSelling ? player : other;
                        Kingdom target = isSelling ? other : player;
                        bool success = TradeManager.ExecuteOneTimeTrade(source, target, resourceId, amount, price);
                        if(success) WorldTip.showNow("Trade successful!", false, "top", 2f, "#9EE07A");
                    }
                    else
                    {
                        // Periodic
                        Kingdom source = isSelling ? player : other;
                        Kingdom target = isSelling ? other : player;
                        TradeManager.AddContract(source, target, resourceId, amount, price, isSelling);
                    }
                },
                onRefuse: () => { /* Cancelled, do nothing */ },
                acceptTooltip: "Confirm Deal",
                refuseTooltip: "Cancel",
                acceptLabel: "Deal",
                refuseLabel: "Cancel"
            );
        }
        
        // trigger a trade cancelled event
        public static void TriggerTradeCancelled(Kingdom withKingdom, string resource, string reason)
        {
            int id = nextId++;
            string title = $"Trade ({resource}) cancelled!";
            var evt = new RulerEvent
            {
                Id = id,
                Type = EventButtonType.Random,
                From = withKingdom,
                Message = $"Trade for {resource} with {withKingdom.data.name} ended: {reason}"
            };
            events[id] = evt;
            evt.ButtonGO = EventsUI.AddEventButton(EventButtonType.Random, title, id);
        }
        
        // tick method to be called every frame or time step
        public static void Tick(float dt)
        {
            // If time is stopped, don't advance popups or random events
            if (dt <= 0f || World.world.isPaused())
                return;
            if (Main.selectedKingdom == null)
            {
                // No focused kingdom, just close the popup.
                EventsUI.HidePopup();
                return;
            }
            // Let UI handle popup auto-close.
            EventsUI.TickPopup(dt);
            // Event Cooldown
            timeSinceLastEvent += dt;

            // Random event trigger logic (to change)
            if (timeSinceLastEvent >= EventCooldown && UnityEngine.Random.value < 0.0005f) // ~0.05% per tick
            {
                TriggerRandomEvent();
            }

            // --- Plague Check ---
            CheckPlague(dt);
        }

        private static void CheckPlague(float dt)
        {
            var k = Main.selectedKingdom;
            if (k == null) return;
            var d = KingdomMetricsSystem.Get(k);
            if (d == null) return;
            
            // Prevent plague in very small/new kingdoms
            if (d.Population < 30) return;

            // CURE / PREVENTION logic for Maximum Welfare
            if (d.WelfareSpending == "Maximum" || d.WelfareSpending == "High")
            {
                if (k.units != null)
                {
                    bool curedAny = false;
                    foreach (var actor in k.units)
                    {
                        if (actor != null && actor.hasTrait("plague"))
                        {
                            actor.removeTrait("plague");
                            curedAny = true;
                        }
                    }
                }
                
                // Also reset risk to ensure it doesn't build up silently
                d.PlagueRiskAccumulator = 0f;
                d.PlagueResistanceDecay = 0f;
                
                return; // Stop here, do not trigger new plague
            }

            // Calculate true risk (unclamped) to ensure it can eventually overcome high resistance
            float trueRisk = 10f + (d.Cities * 2f) + d.PlagueRiskAccumulator;
            
            if (trueRisk > d.PlagueResistance)
            {                
                // 1. Notify Player
                WorldTip.showNow($"A Plague has started in {k.data.name}!", false, "top", 5f, "#FF5A5A");
                
                // 2. Infect Units
                if (k.units != null)
                {
                    int infectedCount = 0;
                    foreach (var actor in k.units)
                    {
                        if (actor != null && actor.isAlive() && !actor.hasTrait("plague") && !actor.hasTrait("immune"))
                        {
                            // 10% chance to infect initially
                            if (UnityEngine.Random.value < 0.1f)
                            {
                                actor.addTrait("plague");
                                infectedCount++;
                            }
                        }
                    }
                    // WorldTip.showNow($"{infectedCount} citizens infected!", false, "top", 3f, "#FF5A5A");
                }

                // 3. Reset Risk/Resistance to restart cycle
                d.PlagueRiskAccumulator = 0f;
                d.PlagueResistanceDecay = 0f;
                
                // Force recalculate to update UI immediately
                KingdomMetricsSystem.RecalculateAllForKingdom(k, d);
            }
        }


        
        // =====================================================================
        // ========================= PUBLIC API =================================
        // =====================================================================

        // Called when a war is declared against our focused kingdom.
        public static void OnWarDeclared(Kingdom attacker, Kingdom defender, War war)
        {
            if (IsPlayerInitiated) return;

            var me = Main.selectedKingdom;
            if (me == null || defender == null || attacker == null || war == null)
                return;

            // Only show event when THIS war is against the focused kingdom.
            if (defender != me)
                return;
            int id = nextId++;
            string title = $"{attacker.data.name} declared war on us";
            var evt = new RulerEvent
            {
                Id = id,
                Type = EventButtonType.War,
                From = attacker,
                To = defender,
                War = war
            };
            events[id] = evt;
            evt.ButtonGO = EventsUI.AddEventButton(EventButtonType.War, title, id);
        }
        
        // Called when a war involving our focused kingdom ends with peace.
        public static void OnWarEndedWithPeace(War war)
        {
            if (IsPlayerInitiated) return;

            var me = Main.selectedKingdom;
            if (me == null || war == null)
                return;
            // Detect if our kingdom participated in this war.
            bool isAttackerSide = false;
            bool isDefenderSide = false;
            foreach (var k in war.getAttackers())
            {
                if (k == me)
                {
                    isAttackerSide = true;
                    break;
                }
            }
            if (!isAttackerSide)
            {
                foreach (var k in war.getDefenders())
                {
                    if (k == me)
                    {
                        isDefenderSide = true;
                        break;
                    }
                }
            }
            if (!isAttackerSide && !isDefenderSide)
                return;
            Kingdom other = war.getMainAttacker();
            if (other == null || other == me)
                other = war.getMainDefender();
            if (other == null || other == me)
                return;
            int id = nextId++;
            string title = $"{other.data.name} has offered peace";
            var evt = new RulerEvent
            {
                Id = id,
                Type = EventButtonType.Peace,
                From = other,
                To = me,
                War = war,
                Message = title
            };
            events[id] = evt;
            evt.ButtonGO = EventsUI.AddEventButton(EventButtonType.Peace, title, id);
        }
        
        // Called when an alliance is formed that includes our focused kingdom.
        public static void OnAllianceFormed(Alliance alliance, Kingdom k1, Kingdom k2)
        {
            if (IsPlayerInitiated) return;
            
            var me = Main.selectedKingdom;
            if (me == null || alliance == null || k1 == null || k2 == null)
                return;
            bool meIsK1 = (k1 == me);
            bool meIsK2 = (k2 == me);
            if (!meIsK1 && !meIsK2)
                return;
            Kingdom other = meIsK1 ? k2 : k1;
            int id = nextId++;
            string title = $"{other.data.name} wants to form an alliance";
            var evt = new RulerEvent
            {
                Id = id,
                Type = EventButtonType.Diplomacy,
                From = other,
                To = me,
                Alliance = alliance,
                Message = title
            };
            events[id] = evt;
            evt.ButtonGO = EventsUI.AddEventButton(EventButtonType.Diplomacy, title, id);
        }
        
        // Open the event popup for the given event ID
        public static void OpenEvent(int id)
        {
            if (!events.TryGetValue(id, out var evt) || evt == null)
                return;
            if (evt.ButtonGO != null)
            {
                UnityEngine.Object.Destroy(evt.ButtonGO);
                evt.ButtonGO = null;
            }
            var fromName = evt.From != null ? evt.From.data.name : "Unknown";
            
            // Handle Random events with options
            if (evt.Type == EventButtonType.Random && randomEventPerId.TryGetValue(id, out var def))
            {
                var option1 = def.Options.Count > 0 ? def.Options[0] : null;
                var option2 = def.Options.Count > 1 ? def.Options[1] : null;
                
                // Define callbacks for Accept/Refuse based on the available options
                Action onAccept = option1 != null ? () => { option1.Action(Main.selectedKingdom); CloseEvent(id); } : null;
                Action onRefuse = option2 != null ? () => { option2.Action(Main.selectedKingdom); CloseEvent(id); } : null;
                EventsUI.ShowPopup(
                    def.Text, 
                    EventButtonType.Random,
                    evt.From,
                    onOk: onAccept == null && onRefuse == null ? // If no options, use OK button
                        () => CloseEvent(id) : null,
                    onAccept: onAccept,
                    onRefuse: onRefuse,
                    acceptTooltip: option1?.Tooltip,
                    refuseTooltip: option2?.Tooltip,
                    acceptLabel: option1?.Text ?? "Accept",
                    refuseLabel: option2?.Text ?? "Refuse"
                );
                return;
            }
            
            // Handle other event types
            switch (evt.Type)
            {
                case EventButtonType.War:
                {
                    string msg = $"{fromName} declared war on us";
                    EventsUI.ShowPopup(
                        msg,
                        EventButtonType.War,
                        evt.From,
                        onOk: () => CloseEvent(id),
                        onAccept: null,
                        onRefuse: null
                    );
                    break;
                }
                case EventButtonType.Peace:
                {
                    string msg = evt.Message ?? $"{fromName} has offered peace";

                    EventsUI.ShowPopup(
                        msg,
                        EventButtonType.Peace,
                        evt.From,
                        onOk: null,
                        onAccept: () =>
                        {
                            WorldTip.showNow("Peace accepted.", false, "top", 2f, "#9EE07A");
                            CloseEvent(id);
                        },
                        onRefuse: () =>
                        {
                            var me = Main.selectedKingdom;
                            var other = evt.From;
                            var war = evt.War;
                            
                            // Restart the war
                            if (me != null && other != null && war != null)
                            {
                                var asset = war.getAsset();
                                World.world.wars.newWar(other, me, asset);
                                WorldTip.showNow("Peace refused. The war continues!", false, "top", 2f, "#FF5A5A");
                            }
                            else
                            {
                                WorldTip.showNow("Peace refused (could not restart war).", false, "top", 2f, "#FF5A5A");
                            }
                            CloseEvent(id);
                        }
                    );
                    break;
                }
                case EventButtonType.Diplomacy:
                {
                    bool hasAlliance = Main.selectedKingdom != null && Main.selectedKingdom.hasAlliance();
                    string msg;
                    
                    // Custom message if provided
                    if (evt.Message != null)
                    {
                        msg = evt.Message;
                    }
                    else
                    {
                        msg = hasAlliance
                            ? $"{fromName} wants to join your alliance"
                            : $"{fromName} wants you to join his alliance";
                    }
                    
                    // Show popup with Accept/Refuse options
                    EventsUI.ShowPopup(
                        msg,
                        EventButtonType.Diplomacy,
                        evt.From,
                        onOk: null,
                        onAccept: () =>
                        {
                            WorldTip.showNow("Alliance accepted.", false, "top", 2f, "#9EE07A");
                            CloseEvent(id);
                        },
                        onRefuse: () =>
                        {
                            var me = Main.selectedKingdom;
                            var alliance = evt.Alliance;
                            
                            // Leave the alliance
                            if (me != null && alliance != null && alliance.kingdoms_hashset.Contains(me))
                            {
                                alliance.leave(me, pRecalc: true);

                                if (alliance.kingdoms_hashset.Count == 0)
                                {
                                    World.world.alliances.dissolveAlliance(alliance);
                                }

                                WorldTip.showNow("Alliance refused. We are no longer part of that alliance.", false, "top", 2f, "#FF5A5A");
                            }
                            else
                            {
                                WorldTip.showNow("Alliance refused (no active alliance to leave).", false, "top", 2f, "#FF5A5A");
                            }
                            CloseEvent(id);
                        }
                    );
                    break;
                }
                default:
                {
                    string msg = evt.Message ?? "Unknown event";
                    EventsUI.ShowPopup(
                        msg,
                        EventButtonType.Random,
                        evt.From, 
                        onOk: () => CloseEvent(id),
                        onAccept: null,
                        onRefuse: null
                    );
                    break;
                }
            }
        }

        // Called when we are removed from / leave an alliance
        public static void OnAllianceLeft(Kingdom me, Alliance alliance)
        {
            if (me == null || alliance == null)
                return;

            Kingdom other = null;
            foreach (var k in alliance.kingdoms_list)
            {
                if (k != null && k != me)
                {
                    other = k;
                    break;
                }
            }

            string otherName = other != null ? other.data.name : alliance.data.name;
            string title = $"{otherName} removed us from the alliance";
            int id = nextId++;
            var evt = new RulerEvent
            {
                Id = id,
                Type = EventButtonType.Random,
                From = other,
                To = me,
                Alliance = alliance,
                Message = title
            };
            events[id] = evt;
            evt.ButtonGO = EventsUI.AddEventButton(EventButtonType.Random, title, id);
        }

        // Called when an alliance we are in is dissolved
        public static void OnAllianceDissolved(Kingdom me, Alliance alliance)
        {
            if (me == null || alliance == null)
                return;
            Kingdom other = null;
            foreach (var k in alliance.kingdoms_list)
            {
                if (k != null && k != me)
                {
                    other = k;
                    break;
                }
            }
            
            string otherName = other != null ? other.data.name : alliance.data.name;
            string title = $"Our alliance with {otherName} has been dissolved";
            int id = nextId++;
            var evt = new RulerEvent
            {
                Id = id,
                Type = EventButtonType.Random, 
                From = other,
                To = me,
                Alliance = alliance,
                Message = title
            };
            events[id] = evt;
            evt.ButtonGO = EventsUI.AddEventButton(EventButtonType.Random, title, id);
        }

        // Close and remove the event with the given ID
        private static void CloseEvent(int id)
        {
            if (events.TryGetValue(id, out var evt) && evt != null)
            {
                if (evt.ButtonGO != null)
                {
                    UnityEngine.Object.Destroy(evt.ButtonGO);
                    evt.ButtonGO = null;
                }
            }
            events.Remove(id);
            EventsUI.HidePopup();
        }

        // =====================================================================
        // ============================= RandomEvent ===========================
        // =====================================================================

        // Trigger a random event based on kingdom metrics and conditions
        public static void TriggerRandomEvent()
        {
            var k = Main.selectedKingdom;
            if (k == null) return;
            
            // Get kingdom metrics (needed for event triggers)
            var d = KingdomMetricsSystem.Get(k);
            if (d == null) return;
            if (d.Population < 15) return; // Ignore tiny kingdoms
            
            // Use the existing logic from EventsList to find a valid event based on triggers
            var def = EventsList.GetRandomEvent(k);
            if (def == null) return;
            
            // Reset Cooldown
            timeSinceLastEvent = 0f;

            // spawn the event
            int id = nextId++;
            var rulerEvt = new RulerEvent
            {
                Id = id,
                Type = EventButtonType.Random,
                From = k,
                To = null,
                War = null,
                Alliance = null,
                Message = def.Title
            };
            events[id] = rulerEvt;
            randomEventPerId[id] = def; // Store the EventDef for later use
            
            // Add button to UI
            rulerEvt.ButtonGO = EventsUI.AddEventButton(EventButtonType.Random, def.Title, id);
        }

        // Check if the given kingdom has any active wars
        private static bool HasActiveWar(Kingdom k, out int warCount)
        {
            warCount = 0;
            if (k == null)
                return false;
            
            // Check for active wars
            var wars = k.getWars(false);
            if (wars == null)
                return false;
            
            // Count active wars involving this kingdom
            foreach (var war in wars)
            {
                if (war == null || war.hasEnded())
                    continue;
                if (!war.isAttacker(k) && !war.isDefender(k))
                    continue;
                warCount++;
            }
            return warCount > 0;
        }

        // Store EventDef for random events by their assigned event ID
        private static Dictionary<int, EventsList.EventDef> randomEventPerId 
            = new Dictionary<int, EventsList.EventDef>();
    }

    // =====================================================================
    // ============================= PATCHES ===============================
    // =====================================================================

    //patch WarManager.newWar to detect wars declared against our focused kingdom.
    [HarmonyPatch(typeof(WarManager), nameof(WarManager.newWar))]
    public static class Patch_WarManager_NewWar
    {
        [HarmonyPrefix]
        public static bool Prefix(Kingdom pAttacker, Kingdom pDefender, WarTypeAsset pType, ref War __result)
        {
            // If the player is the attacker
            if (pAttacker != null && pAttacker == Main.selectedKingdom)
            {
                if (EventsSystem.AllowPlayerWar == false)
                {
                    __result = null; 
                    return false; 
                }
            }
            return true;
        }

        [HarmonyPostfix]
        public static void Postfix(War __result, Kingdom pAttacker, Kingdom pDefender)
        {
            if (__result != null)
            {
                EventsSystem.OnWarDeclared(pAttacker, pDefender, __result);
            }
        }
    }

    // patch DiplomacyManager.startWar to prevent crash if newWar returns null (blocked by us)
    [HarmonyPatch(typeof(DiplomacyManager), nameof(DiplomacyManager.startWar))]
    public static class Patch_DiplomacyManager_StartWar
    {
        [HarmonyPrefix]
        public static bool Prefix(Kingdom pAttacker, Kingdom pDefender, WarTypeAsset pAsset, bool pLog)
        {
             // If the player is the attacker and War is not allowed, block execution to prevent NRE
             if (pAttacker != null && pAttacker == Main.selectedKingdom)
             {
                 if (EventsSystem.AllowPlayerWar == false)
                 {
                     // Block the call entirely
                     return false; 
                 }
             }
             return true;
        }
    }
    
    // Patch WarManager.endWar to detect wars that end with peace involving our focused kingdom.
    [HarmonyPatch(typeof(WarManager), nameof(WarManager.endWar))]
    public static class Patch_WarManager_EndWar
    {
        [HarmonyPostfix]
        public static void Postfix(War pWar, WarWinner pWinner)
        {
            if (pWar == null) return;
            
            // Check if our focused kingdom was involved in this war
            if (pWinner == WarWinner.Peace)
            {
                EventsSystem.OnWarEndedWithPeace(pWar);
            }
        }
    }

    // When an alliance is formed that includes our focused kingdom.
    [HarmonyPatch(typeof(AllianceManager), nameof(AllianceManager.newAlliance))]
    public static class Patch_AllianceManager_NewAlliance
    {
        [HarmonyPostfix]
        public static void Postfix(Alliance __result, Kingdom pKingdom, Kingdom pKingdom2)
        {
            if (__result == null || pKingdom == null || pKingdom2 == null)
                return;
            EventsSystem.OnAllianceFormed(__result, pKingdom, pKingdom2);
        }
    }
    
    // When we leave an alliance.
    [HarmonyPatch(typeof(Alliance), nameof(Alliance.leave))]
    public static class Patch_Alliance_Leave
    {
        [HarmonyPostfix]
        public static void Postfix(Alliance __instance, Kingdom pKingdom, bool pRecalc)
        {
            var me = Main.selectedKingdom;
            if (me == null || pKingdom != me)
                return;
            EventsSystem.OnAllianceLeft(me, __instance);
        }
    }
    
    // When an alliance we are in is dissolved.
    [HarmonyPatch(typeof(AllianceManager), nameof(AllianceManager.dissolveAlliance))]
    public static class Patch_AllianceManager_Dissolve
    {
        [HarmonyPrefix]
        public static void Prefix(Alliance pAlliance)
        {
            var me = Main.selectedKingdom;
            if (me == null || pAlliance == null)
                return;
            if (!pAlliance.kingdoms_hashset.Contains(me))
                return;
            EventsSystem.OnAllianceDissolved(me, pAlliance);
        }
    }

}

