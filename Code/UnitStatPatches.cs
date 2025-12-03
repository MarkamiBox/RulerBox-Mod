using HarmonyLib;
using UnityEngine;
using System.Reflection;

namespace RulerBox
{
    [HarmonyPatch(typeof(Actor), "updateStats")]
    public static class UnitStatPatches
    {
        public static void Postfix(Actor __instance)
        {
            if (__instance == null || __instance.kingdom == null) return;
            if (__instance.stats == null) return;

            var d = KingdomMetricsSystem.Get(__instance.kingdom);
            if (d == null) return;

            // Apply Military Attack Modifier
            // We use "damage" string id if S.damage is not directly available, but usually it is.
            // To be safe, we'll try to use the stat directly if we can access the collection.
            
            if (d.MilitaryAttackModifier != 0f)
            {
                float current = __instance.stats["damage"];
                float bonus = current * d.MilitaryAttackModifier;
                __instance.stats["damage"] += bonus;
            }
        }
    }
}
