using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace RulerBox
{
    public static class Patches
    {
        // --- From UnitStatPatches.cs ---
        [HarmonyPatch(typeof(Actor), "updateStats")]
        public static class UnitStatPatches
        {
            public static void Postfix(Actor __instance)
            {
                if (__instance == null || __instance.kingdom == null) return;
                if (__instance.stats == null) return;

                var d = KingdomMetricsSystem.Get(__instance.kingdom);
                if (d == null) return;
                
                if (d.MilitaryAttackModifier != 0f)
                {
                    float current = __instance.stats["damage"];
                    float bonus = current * d.MilitaryAttackModifier;
                    __instance.stats["damage"] += bonus;
                }
            }
        }

        // Gameplay Patches to hook modifiers
        [HarmonyPatch(typeof(City), "getMaxWarriors")]
        public static class Patch_City_GetMaxWarriors
        {
            public static void Postfix(City __instance, ref int __result)
            {
                if (__instance == null || __instance.kingdom == null) return;
                var d = KingdomMetricsSystem.Get(__instance.kingdom);
                if (d != null && d.ManpowerMaxMultiplier != 1.0f)
                {
                    __result = (int)(__result * d.ManpowerMaxMultiplier);
                }
            }
        }

        // Patch the selection method to filter only soldiers or recruitable civilians
        [HarmonyPatch(typeof(SelectedUnit), nameof(SelectedUnit.selectMultiple))]
        public static class Patch_SelectOnlySoldiers
        {
            [HarmonyPrefix]
            public static void Prefix(ListPool<Actor> pActors)
            {
                if (pActors == null || pActors.Count == 0) return;

                var mode = ArmySystem.CurrentMode;

                // Filter list in-place
                for (int i = pActors.Count - 1; i >= 0; i--)
                {
                    Actor a = pActors[i];
                    bool keep = false;

                    if (mode == ArmySelectionMode.Normal)
                    {
                        // Normal: Only select existing soldiers
                        if (SoldierJobHelper.IsSoldier(a)) keep = true;
                    }
                    else if (mode == ArmySelectionMode.Recruit)
                    {
                        // Recruit: Only select recruitable civilians
                        if (SoldierJobHelper.IsRecruitable(a)) keep = true;
                    }
                    else if (mode == ArmySelectionMode.Dismiss)
                    {
                        // Dismiss: Only select existing soldiers
                        if (SoldierJobHelper.IsSoldier(a)) keep = true;
                    }

                    if (!keep)
                    {
                        pActors.RemoveAt(i);
                    }
                }

                // Pass the filtered list to ArmySystem to perform the recruit/dismiss logic
                ArmySystem.OnNewSelection(pActors);
                
                // If we are in a special mode (Recruit/Dismiss), we clear the visual selection list 
                // so the game doesn't keep them highlighted as "selected units".
                if (mode != ArmySelectionMode.Normal)
                {
                    pActors.Clear(); 
                }
            }
        }

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
        
        [HarmonyPatch(typeof(MapBox), "saveSave")]
        public static class Patch_MapBox_Save
        {
            // Prefix to ensure data is in Kingdom.data BEFORE it is written to disk
            public static void Prefix()
            {
                if (World.world?.kingdoms?.list != null)
                {
                    foreach (var k in World.world.kingdoms.list)
                    {
                        KingdomMetricsSystem.SyncToKingdom(k);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(MapBox), "loadSave")]
        public static class Patch_MapBox_Load
        {
            public static void Postfix()
            {
                if (World.world?.kingdoms?.list != null)
                {
                    foreach (var k in World.world.kingdoms.list)
                    {
                        KingdomMetricsSystem.SyncFromKingdom(k);
                    }
                }
            }
        }
    }
}
