using HarmonyLib;
using UnityEngine;
using System.Reflection;

namespace RulerBox
{
    public static class EconomyPatches
    {
        // --- Building Speed Patch ---
        // We patch the update method to add extra construction progress
        // [HarmonyPatch(typeof(Building), "update")] // FIXME: Method not found
        public static class BuildingConstructionPatch
        {
            public static void Postfix(Building __instance, float pElapsed)
            {
                /*
                if (__instance == null || !__instance.isUnderConstruction()) return;
                if (__instance.kingdom == null) return;

                var d = KingdomMetricsSystem.Get(__instance.kingdom);
                // Base modifier is 1.0f. If > 1.0f, we add extra progress.
                if (d == null || d.BuildingSpeedModifier <= 1.0f) return;

                // Calculate extra speed
                // e.g. Modifier 1.5f -> +50% speed
                // We need to call construct() or similar.
                // Since we don't know the exact internal method for "add progress", 
                // we'll try to use reflection to find 'construct' or 'increaseBuildProgress'.
                
                // However, safely, we can just try to call the public method if it exists.
                // Most likely: __instance.construct(amount)
                
                float bonus = (d.BuildingSpeedModifier - 1.0f);
                if (bonus > 0f)
                {
                    // Add extra progress proportional to elapsed time
                    // Standard build speed is usually 1.0 * dt
                    // So we add bonus * dt
                    
                    // We use Traverse to call private/protected method if needed, 
                    // or just public if available. 
                    // 'construct' is often public.
                    
                    // __instance.construct(pElapsed * bonus); 
                    // We'll use a safe reflection call to avoid compile errors if method signature varies.
                    
                    try 
                    {
                        // Assuming standard WorldBox method 'construct(float)'
                        // We can't call it directly if we don't link against the assembly, 
                        // but we are in the mod project so we should have references.
                        // I'll assume 'construct' is available.
                        // FIXME: construct method not found
                        // __instance.construct();pElapsed * bonus);
                    }
                    catch 
                    {
                        // Fallback or ignore
                    }
                }
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

                // Logic:
                // PopulationGrowthBonus is e.g. 0.05 (5% extra growth rate).
                // We want to occasionally spawn a unit.
                // Let's say base growth produces 1 unit every X seconds.
                // We want to add a small chance per tick.
                
                // pElapsed is usually delta time.
                // Chance = Bonus * pElapsed * Constant
                
                float chance = d.PopulationGrowthBonus * pElapsed * 0.01f; 
                
                if (UnityEngine.Random.value < chance)
                {
                    // Try to produce a baby
                    // produceUnit() is the standard method.
                    // FIXME: produceUnit method not found, using spawnNewUnit
                    World.world.units.spawnNewUnit(__instance.getSpecies(), __instance.getTile(), true, true, 0f);
                }
            }
        }
    }
}
