using HarmonyLib;
using UnityEngine;
using System.Reflection;

namespace RulerBox
{
    // TODO: implement
    [HarmonyPatch(typeof(Culture), "update")]
    public static class TechnologyPatches
    {
        // Enforce the lock!
        // If the base game or anything else tries to research a locked tech, we stop it.
        public static void Postfix(Culture __instance)
        {
            if (__instance == null || __instance.data == null) return;
            TechnologyManager.EnforceLock(__instance);
        }
    }
}
