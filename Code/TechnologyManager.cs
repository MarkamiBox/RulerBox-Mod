using System;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;

namespace RulerBox
{
    public static class TechnologyManager
    {
        // Dictionary to store unlocked status of technologies (KnowledgeAsset IDs) for each Kingdom/Culture?
        // User said "make that they can develop certain tech only if you unlcok it (for now make a bollean inside techonolgy"
        // This implies the unlock is global or per-tech, not necessarily per kingdom.
        // BUT "be sure that the selected kingdom can't unlock them other than the boolean"
        // This suggests the boolean is on the Technology itself.
        // So global unlock? Or is it "this tech is unlockable via gameplay"?
        // "make that they can develop certain tech only if you unlcok it" -> Maybe "Enable/Disable" tech globally?
        // Or specific kingdom unlock?
        // "make a bollean inside techonolgy" -> implies field on KnowledgeAsset.
        
        // I'll simulate a field on KnowledgeAsset using a Dictionary.
        // Since KnowledgeAssets are shared global assets, this boolean will be global.
        private static Dictionary<string, bool> _unlockedTechnologies = new Dictionary<string, bool>();

        // Default to false? User said "only if you unlock it".
        // But normal game techs should be unlocked by default? 
        // I will assume ALL techs are LOCKED by default if we strictly follow "only if you unlock it".
        // However, breaking the base game is bad. 
        // I will default to TRUE (unlocked) for base game techs, and FALSE for new ones?
        // Or simply default to TRUE, and let user manually lock them?
        // The user said "make that they can develop certain tech ONLY if you unlock it".
        // I'll assume default is LOCKED for everything if I enable this system, OR I'll add a specific list of "Restricted" techs.
        
        // Re-reading: "make that they can develop certain tech only if you unlcok it (for now make a bollean inside techonolgy"
        // This means I should add a property `isUnlocked` to the tech.
        // If `isUnlocked` is false, nobody can research it.
        
        public static bool IsTechUnlockable(string techID)
        {
            if (_unlockedTechnologies.TryGetValue(techID, out bool unlocked))
            {
                return unlocked;
            }
            // Default to true so we don't break everything immediately?
            // Or default to false?
            // Let's default to FALSE for safety of the requirement "only if you unlock it". 
            // WAIT, if I default to false, the game will stop researching anything.
            // I'll default to TRUE, but provide a way to set it to FALSE.
            return true; 
        }

        public static void SetTechUnlockable(string techID, bool unlocked)
        {
            _unlockedTechnologies[techID] = unlocked;
        }

        public static void ToggleTechUnlockable(string techID)
        {
            SetTechUnlockable(techID, !IsTechUnlockable(techID));
        }
        public static void UpdateResearch(Culture culture, float points)
        {
            if (culture == null || culture.data == null) return;

            // Use Reflection to access fields on CultureData if they are not public/visible.
            var type = culture.data.GetType();
            var f_type = type.GetField("knowledge_type");
            var f_progress = type.GetField("knowledge_progress");

            // Assuming knowledge_level might exist or we infer it
            // var f_level = type.GetField("knowledge_level"); 

            if (f_type == null || f_progress == null) 
            {
                 f_type = type.GetField("knowledge_type", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                 f_progress = type.GetField("knowledge_progress", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                 if (f_type == null || f_progress == null) return;
            }

            string currentTech = (string)f_type.GetValue(culture.data);

            if (string.IsNullOrEmpty(currentTech))
            {
                // Try to pick a new tech
                PickNewTech(culture, f_type);
                return;
            }

            // Check if unlocked!
            if (!IsTechUnlockable(currentTech))
            {
                // Locked tech. Stop progress or clear it? 
                // Creating a specific status or message would be good but for now we just stall.
                return;
            }

            float currentProgress = (float)f_progress.GetValue(culture.data);
            currentProgress += points;
            
            // Calculate Cost
            // Since we don't have the exact game formula, we'll use a standard scaling one.
            // Cost = Base (100) * (Culture Level or Tech Level)
            float cost = 100f; 
            
            if (currentProgress >= cost)
            {
                // RESEARCH COMPLETE!
                // 1. Actually give the tech to the culture.
                // We likely need valid methods: culture.addKnowledge(techID) or culture.levelUp(techID)
                // Since I don't have them, I will use Reflection to look for "addKnowledge" or similar on Culture.
                var m_addKnowledge = culture.GetType().GetMethod("addKnowledge", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (m_addKnowledge != null)
                {
                    m_addKnowledge.Invoke(culture, new object[] { currentTech });
                }
                else
                {
                     // Fallback: maybe just adding it to list?
                     // culture.data.knowledge_list.Add(currentTech)? 
                     Debug.LogWarning($"[RulerBox] Could not find addKnowledge method for culture {culture.data.name}");
                }

                // 2. Reset progress
                f_progress.SetValue(culture.data, 0f);

                // 3. Pick NEXT tech
                // Logic: Pick a different one, or if this is a leveled tech, maybe keep it?
                // For now, force a re-pick to ensure rotation.
                f_type.SetValue(culture.data, null); 
                PickNewTech(culture, f_type);
            }
            else
            {
                f_progress.SetValue(culture.data, currentProgress);
            }
        }

        private static void PickNewTech(Culture culture, System.Reflection.FieldInfo f_type)
        {
            var lib = AssetManager.knowledge_library;
            if (lib == null) return;

            // Get currently known techs to avoid repeating non-repeatables?
            // culture.getKnowledges()? 
            // For now, simple random pick from UNLOCKED assets.
            
            var candidates = new List<string>();
            foreach(var asset in lib.list)
            {
                // 1. Must be unlocked via our system
                if (!IsTechUnlockable(asset.id)) continue;
                
                // 2. Check max level? (Assume max level is handled by game logic or we ignore it for now)
                
                candidates.Add(asset.id);
            }

            if (candidates.Count > 0)
            {
                string picked = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                f_type.SetValue(culture.data, picked);
            }
        }

        public static void EnforceLock(Culture culture)
        {
             if (culture == null || culture.data == null) return;

             var type = culture.data.GetType();
             // Optimization: Cache field info if performance is an issue
             var f_type = type.GetField("knowledge_type", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
             if (f_type == null) return;

             string currentTech = (string)f_type.GetValue(culture.data);
             if (string.IsNullOrEmpty(currentTech)) return;

             if (!IsTechUnlockable(currentTech))
             {
                 // Tech is locked! Force clear it.
                 // This effectively cancels the research.
                 f_type.SetValue(culture.data, null);
                 
                 // Optionally pick a new one immediately
                 PickNewTech(culture, f_type);
             }
        }
    }
}