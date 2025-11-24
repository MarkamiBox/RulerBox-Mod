using HarmonyLib;
using System.Collections.Generic;

namespace RulerBox
{
    public static class SoldierJobHelper
    {
        public static bool IsSoldier(Actor actor)
        {
            if (actor == null || actor.hasDied()) return false;
            // Only select warriors of the focused kingdom
            return (actor._profession == UnitProfession.Warrior && actor.kingdom == Main.selectedKingdom);
        }

        public static bool IsRecruitable(Actor actor)
        {
            if (actor == null || actor.hasDied()) return false;
            if (actor.kingdom != Main.selectedKingdom) return false;
            
            // Eligibility: Adult, not leader/king, not soldier
            if (!actor.isAdult()) return false;
            if (actor.isKing() || actor.isCityLeader()) return false;
            if (actor._profession == UnitProfession.Warrior) return false; 

            return true;
        }
    }

    /// <summary>
    /// Patch that filters drag-selection based on ArmySystem.CurrentMode.
    /// </summary>
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
}