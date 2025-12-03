using HarmonyLib;
using System.Collections.Generic;

namespace RulerBox
{
    public static class SoldierJobHelper
    {
        private static bool IsCustomLeader(Actor actor)
        {
            if (actor == null || actor.kingdom == null) return false;

            // Get data for the actor's kingdom
            var data = KingdomMetricsSystem.Get(actor.kingdom);
            if (data == null || data.ActiveLeaders == null) return false;

            // Check if this actor is in the ActiveLeaders list
            foreach (var leader in data.ActiveLeaders)
            {
                if (leader.UnitLink == actor) return true;
            }
            return false;
        }

        public static bool IsSoldier(Actor actor)
        {
            if (actor == null || actor.hasDied()) return false;
            if (actor.isKing() || actor.isCityLeader()) return false;
            if (IsCustomLeader(actor)) return false;
            // Only select warriors of the focused kingdom
            return (actor._profession == UnitProfession.Warrior && actor.kingdom == Main.selectedKingdom);
        }

        // Check if an actor is eligible for recruitment as a soldier
        public static bool IsRecruitable(Actor actor)
        {
            if (actor == null || actor.hasDied()) return false;
            if (actor.kingdom != Main.selectedKingdom) return false;
            
            // Eligibility: Adult, not leader/king, not soldier
            if (!actor.isAdult()) return false;
            if (actor.isKing() || actor.isCityLeader()) return false;
            if (IsCustomLeader(actor)) return false;
            if (actor._profession == UnitProfession.Warrior) return false; 

            return true;
        }
    }

}
