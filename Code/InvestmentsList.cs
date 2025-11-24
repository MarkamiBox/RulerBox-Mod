using System;
using System.Collections.Generic;
using UnityEngine;

namespace RulerBox
{
    public static class InvestmentsList
    {
        public class Cost
        {
            public string ResourceId;
            public int Amount;
            public Cost(string id, int amt) { ResourceId = id; Amount = amt; }
        }

        public class InvestmentDefinition
        {
            public string Id;
            public string Name;
            public string Description;
            public string IconPath;
            public float TreasuryPctCost = 0f;
            public Func<Kingdom, string> GetBuildingAssetId;
            public List<Cost> Costs = new List<Cost>();
        }

        public static readonly List<InvestmentDefinition> Definitions = new List<InvestmentDefinition>()
        {
            // --- HOUSING ---
            new InvestmentDefinition
            {
                Id = "housing_t0",
                Name = "Basic House",
                Description = "A crude shelter.\n\n Result: +3 Housing",
                IconPath = "icon_tech_house_tier_0", 
                TreasuryPctCost = 0.05f, 
                Costs = new List<Cost> { new Cost("wood", 10) },
                GetBuildingAssetId = k => GetRaceBuild(k, "house", "_0")
            },

            new InvestmentDefinition
            {
                Id = "housing_t1",
                Name = "Small House",
                Description = "Basic dwelling for a family.\n\n Result: +4 Housing",
                IconPath = "icon_tech_house_tier_1", 
                TreasuryPctCost = 0.05f, 
                Costs = new List<Cost> { new Cost("wood", 15) },
                GetBuildingAssetId = k => GetRaceBuild(k, "house", "_1")
            },

            new InvestmentDefinition
            {
                Id = "housing_t2",
                Name = "Medium House",
                Description = "A comfortable home.\n\n Result: +5 Housing",
                IconPath = "icon_tech_house_tier_2", 
                TreasuryPctCost = 0.06f, 
                Costs = new List<Cost> { new Cost("wood", 25), new Cost("stone", 5) },
                GetBuildingAssetId = k => GetRaceBuild(k, "house", "_2")
            },

            new InvestmentDefinition
            {
                Id = "housing_t3",
                Name = "Large House",
                Description = "Spacious living quarters.\n\n Result: +6 Housing",
                IconPath = "icon_tech_house_tier_3", 
                TreasuryPctCost = 0.07f, 
                Costs = new List<Cost> { new Cost("wood", 40), new Cost("stone", 10) },
                GetBuildingAssetId = k => GetRaceBuild(k, "house", "_3")
            },

            new InvestmentDefinition
            {
                Id = "housing_t4",
                Name = "Manor House",
                Description = "Luxurious residence.\n\n Result: +8 Housing",
                IconPath = "icon_tech_house_tier_4", 
                TreasuryPctCost = 0.08f, 
                Costs = new List<Cost> { new Cost("wood", 60), new Cost("stone", 20) },
                GetBuildingAssetId = k => GetRaceBuild(k, "house", "_4")
            },

            new InvestmentDefinition
            {
                Id = "housing_t5",
                Name = "Grand Residence",
                Description = "The pinnacle of urban living.\n\n Result: +10 Housing",
                IconPath = "icon_tech_house_tier_5", 
                TreasuryPctCost = 0.10f, 
                Costs = new List<Cost> { new Cost("wood", 100), new Cost("stone", 40), new Cost("gold", 5) },
                GetBuildingAssetId = k => GetRaceBuild(k, "house", "_5")
            },

            // --- MILITARY ---
            new InvestmentDefinition
            {
                Id = "barracks_build",
                Name = "Barracks",
                Description = "Hub for training soldiers.\n\n Result: Enables warrior training",
                IconPath = "icon_tech_barracks",
                TreasuryPctCost = 0.25f, 
                Costs = new List<Cost> { new Cost("wood", 50), new Cost("stone", 20) },
                GetBuildingAssetId = k => GetRaceBuild(k, "barracks")
            },
            new InvestmentDefinition
            {
                Id = "watch_tower_build",
                Name = "Watch Tower",
                Description = "Defensive structure.\n\n Result: Shoots arrows at enemies",
                IconPath = "icon_tech_watch_tower_bonus",
                TreasuryPctCost = 0.15f, 
                Costs = new List<Cost> { new Cost("wood", 30), new Cost("stone", 10) },
                GetBuildingAssetId = k => GetRaceBuild(k, "watch_tower")
            },

            // --- ECONOMY & UTILITY ---
            new InvestmentDefinition
            {
                Id = "mine_build",
                Name = "Mine",
                Description = "Extracts metals.\n\n Result: Produces gold and metal",
                IconPath = "icon_tech_building_mine",
                TreasuryPctCost = 0.20f, 
                Costs = new List<Cost> { new Cost("wood", 40), new Cost("stone", 10) },
                GetBuildingAssetId = k => "mine" 
            },

            // --- CULTURE & FAITH ---
            new InvestmentDefinition
            {
                Id = "temple_build",
                Name = "Temple",
                Description = "Boosts culture.\n\n Result: +Culture knowledge gain",
                IconPath = "icon_tech_temple",
                TreasuryPctCost = 0.35f, 
                Costs = new List<Cost> { new Cost("wood", 50), new Cost("stone", 50), new Cost("gold", 10) },
                GetBuildingAssetId = k => GetRaceBuild(k, "temple")
            },
            new InvestmentDefinition
            {
                Id = "statue_build",
                Name = "Statue",
                Description = "Monument.\n\n Result: Increases loyalty",
                IconPath = "iconStone", 
                TreasuryPctCost = 0.10f, 
                Costs = new List<Cost> { new Cost("stone", 50) },
                GetBuildingAssetId = k => "statue"
            },
            new InvestmentDefinition
            {
                Id = "well_build",
                Name = "Well",
                Description = "Provides water.\n\n Result: Protects from fire",
                IconPath = "icon_tech_well",
                TreasuryPctCost = 0.05f, 
                Costs = new List<Cost> { new Cost("stone", 10) },
                GetBuildingAssetId = k => "well"
            },
            new InvestmentDefinition
            {
                Id = "bonfire_build",
                Name = "Bonfire",
                Description = "Gathering place.\n\n Result: City center marker",
                IconPath = "icon_fire",
                TreasuryPctCost = 0.05f, 
                Costs = new List<Cost> { new Cost("wood", 10) },
                GetBuildingAssetId = k => "bonfire"
            }
        };

        public static InvestmentDefinition GetById(string id)
        {
            return Definitions.Find(d => d.Id == id);
        }

        private static string GetRaceBuild(Kingdom k, string baseId, string suffix = "")
        {
            if (k == null) return null;
            ActorAsset raceAsset = k.getActorAsset();
            if (raceAsset == null) return null;
            return $"{baseId}_{raceAsset.id}{suffix}";
        }
    }
}