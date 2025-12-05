using HarmonyLib;
using UnityEngine;
using System.Reflection;

namespace RulerBox
{
    [HarmonyPatch(typeof(Culture), "update")]
    public static class TechnologyPatches
    {
        // Enforce the lock!
        // If the base game or anything else tries to research a locked tech, we stop it.
        public static void Postfix(Culture __instance)
        {
            if (__instance == null || __instance.data == null) return;

            // Use Reflection to check knowledge_type
            // We optimize by not doing this every frame if possible, but for safety we do.
            // (Ideally, we'd cache the FieldInfo, but static init is fine)
            
            // Note: Since we are in a patch, we can use __instance.data directly if we link, 
            // but for safety/copy-paste we use reflection or dynamic if unsure of fields.
            // Use TechnologyManager's logic since it already has reflection.
            
            // However, TechnologyManager only has "UpdateResearch". 
            // We'll duplicate the check logic or make a helper.
            
            // Helper choice: TechnologyManager.EnforceLock(__instance);
            TechnologyManager.EnforceLock(__instance);
        }
    }
}
