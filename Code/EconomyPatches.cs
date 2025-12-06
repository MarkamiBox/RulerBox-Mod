using HarmonyLib;
using UnityEngine;
using System.Reflection;

namespace RulerBox
{
    public static class EconomyPatches
    {
        // --- Building Speed Patch ---
        // [HarmonyPatch(typeof(Building), "update")] // FIXME: Method not found
        public static class BuildingConstructionPatch
        {
            public static void Postfix(Building __instance, float pElapsed)
            {
                /*
                    TODO: Implement
                */
            }
        }

        // --- Population Growth Patch ---
        [HarmonyPatch(typeof(City), "update")]
        public static class CityGrowthPatch
        {
            public static void Postfix(City __instance, float pElapsed)
            {
                if (__instance == null || __instance.kingdom == null) return;
                
                var d = KingdomMetricsSystem.Get(__instance.kingdom);
                if (d == null || d.PopulationGrowthBonus <= 0f) return;
                
                float chance = d.PopulationGrowthBonus * pElapsed * 0.01f; 
                
                if (UnityEngine.Random.value < chance)
                {
                    World.world.units.spawnNewUnit(__instance.getSpecies(), __instance.getTile(), true, true, 0f);
                }
            }
        }
    }
}
