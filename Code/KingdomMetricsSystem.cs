using System;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;

namespace RulerBox
{
    public class TimedEffect
    {
        public string Id; 
        public float TimeRemaining;
        public float StabilityPerSecond;
        public float StabilityModifier; // New: Affects target stability

        // Constructor for 2 arguments (Legacy support)
        public TimedEffect(float duration, float stabilityPerSecond)
        {
            this.Id = "";
            TimeRemaining = duration;
            StabilityPerSecond = stabilityPerSecond;
            StabilityModifier = 0f;
        }

        // Constructor for 3 arguments 
        public TimedEffect(string id, float duration, float stabilityPerSecond)
        {
            Id = id;
            TimeRemaining = duration;
            StabilityPerSecond = stabilityPerSecond;
            StabilityModifier = 0f;
        }

        // Constructor for 4 arguments
        public TimedEffect(string id, float duration, float stabilityPerSecond, float stabilityModifier)
        {
            Id = id;
            TimeRemaining = duration;
            StabilityPerSecond = stabilityPerSecond;
            StabilityModifier = stabilityModifier;
        }
    }

    public class LeaderState
    {
        public string Id;
        public string Name;
        public string Type; // e.g., "Head of Government"
        public int Level;   // 1, 2, 3...
        
        // Dynamic Link
        public Actor UnitLink; 
        public string IconPath; // Fallback if unit link is missing
        
        // Modifiers
        public float StabilityBonus;
        public float PPGainBonus;
        public float AttackBonus;
        public float ResearchBonus;
        public float TaxBonus;
        public float CorruptionReduction;
    }

    public static class KingdomMetricsSystem
    {
        private const float UpdateInterval = 0.25f;
        private static float accum;

        public static readonly List<string> TrackedResources = new List<string>
        {
            "wood", "stone", "gold", "wheat", "bread", "meat", "fish", "berries", 
            "herbs", "CommonMetals", "mithril", "adamantine", "pie", "tea", "cider"
        };

        public static void TickAll(float deltaTime)
        {
            if (deltaTime <= 0f) return;

            accum += deltaTime;
            if (accum < UpdateInterval)
                return;
            accum = 0f;

            // Iterate through all tracked kingdoms
            // Create a copy of keys to avoid modification errors if needed
            var keys = new List<Kingdom>(db.Keys);
            foreach (var k in keys)
            {
                if (k == null || !k.isAlive() || k.data == null)
                {
                    db.Remove(k);
                    continue;
                }

                var d = db[k];
                
                // Tick effects
                for (int i = d.ActiveEffects.Count - 1; i >= 0; i--)
                {
                    var eff = d.ActiveEffects[i];
                    d.Stability += eff.StabilityPerSecond * deltaTime;
                    eff.TimeRemaining -= deltaTime * 0.1f; // 10x Slower decay for effects (longer duration)
                    if (eff.TimeRemaining <= 0f)
                        d.ActiveEffects.RemoveAt(i);
                }

                RecalculateForKingdom(k, d);
            }
            
            // If we have a selection, update specific UI related
            if (Main.selectedKingdom != null)
            {
                ResourcesTradeWindow.Update();
            }
        }

        public static void RecalculateAllForKingdom(Kingdom k, Data d)
        {
            if (k == null || d == null) return;
            RecalculateForKingdom(k, d);
            ResourcesTradeWindow.Update();
        }

        public static void RecalculateForKingdom(Kingdom k, Data d)
        {
            if (d == null) return;
            d.KRef = k;
            float now = Time.unscaledTime;
            float delta = (d.LastUpdateWorldTime <= 0f) ? UpdateInterval : Mathf.Max(0.01f, now - d.LastUpdateWorldTime);
            d.LastUpdateWorldTime = now;

            Recalculate(k, d, delta);
        }

                private static void ApplyActiveEffects(Data d)
        {
            if (d.ActiveEffects == null) return;
            for (int i = d.ActiveEffects.Count - 1; i >= 0; i--)
            {
                var eff = d.ActiveEffects[i];
                // Apply effects logic
            }
        }

        private static void Recalculate(Kingdom k, Data d, float deltaWorldSeconds)
        {
            if (k == null || d == null) return;

            float secondsPerYear = GetSecondsPerYear();
            float yearsPassed = secondsPerYear > 0f ? deltaWorldSeconds / secondsPerYear : 0f;
            if (yearsPassed <= 0f) yearsPassed = 0.0001f;

            // 0. Update Counts
            UpdateCounts(k, d);

            // 1. Base Tax & Reset Modifiers
            d.TaxRateLocal = Mathf.Clamp01(k.getTaxRateLocal());
            
            // Reset modifiers
            d.StabilityTargetModifier = 0f;
            d.WarExhaustionGainMultiplier = 1.0f;
            d.ManpowerMaxMultiplier = 1.0f;
            d.ManpowerRegenRate = 0.015f; 
            d.PopulationGrowthBonus = 0f;
            d.CorruptionLevel = 0f; 
            d.MilitaryUpkeepModifier = 1.0f;
            d.BuildingSpeedModifier = 1.0f;
            d.ResearchOutputModifier = 1.0f;
            d.FactoryOutputModifier = 1.0f;
            d.ResourceOutputModifier = 1.0f;
            d.TradeIncomeModifier = 1.0f;
            d.IntegrationSpeedModifier = 1.0f;
            d.PoliticalPowerGainModifier = 1.0f;
            d.IdeologyPowerModifier = 1.0f;
            d.UnrestReductionModifier = 1.0f;
            d.CityResistanceModifier = 1.0f;
            d.RebelSuppressionModifier = 1.0f;
            d.InvestmentCostModifier = 1.0f;
            d.InvestmentAvailabilityModifier = 1.0f;
            d.LeaderXPModifier = 0f;
            d.JustificationTimeModifier = 1.0f;
            d.GeniusChanceModifier = 0f;
            d.PlagueResistanceModifier = 0f;
            d.MilitaryAttackModifier = 0f; 

            // 2. Apply Laws & Leaders
            ApplyRiseOfNationsLaws(d);
            ApplyEconomicLaws_Modifiers(d);
            ApplyLeaderModifiers(d); 
            ApplyActivePolicies(d);
            ApplyActiveEffects(d);

            // --- Base Corruption Calculation ---
            // 2.5% corruption per city beyond the first
            if (d.Cities > 1)
            {
                d.CorruptionLevel += (d.Cities - 1) * 0.025f;
            }
            
            // Decay event-based corruption
            if (d.CorruptionFromEvents > 0)
            {
                d.CorruptionFromEvents -= 0.0005f * deltaWorldSeconds; // Decay rate: 0.05% per second (Way slower)
                if (d.CorruptionFromEvents < 0) d.CorruptionFromEvents = 0;
            }
            d.CorruptionLevel += d.CorruptionFromEvents;
            // --- Anarchy / Low Stability Penalties ---
            bool isAnarchy = d.Stability <= 0f;
            if (isAnarchy)
            {
                // Corruption "increase 200%" -> effectively tripled or forced high
                // Since base corruption is 0-1, we can just multiply by 3 or add a flat amount
                d.CorruptionLevel = Mathf.Min(1.0f, d.CorruptionLevel * 3.0f);
            }


            // 3. Economy Calc (Income)
            // MOVED UP: Wealth calculation for Corruption Logic
            double totalWealth = 0;
            if (k.units != null) {
                foreach (var unit in k.units) {
                    if (unit != null && unit.isAlive()) totalWealth += unit.money;
                }
            }
            d.TaxBaseWealth = totalWealth;
            
            // --- Corruption from High Taxes ---
            double estimatedTaxIncome = totalWealth * d.TaxRateLocal;
            float corruptionFromTax = (float)(estimatedTaxIncome / 1000.0) * 0.01f; // 1% per 1000 gold
            d.CorruptionLevel += corruptionFromTax;

            d.CorruptionLevel = Mathf.Clamp01(d.CorruptionLevel);
            
            double baseTaxable = totalWealth;
            if (baseTaxable <= 0) baseTaxable = d.Population * d.PerCapitaGDP;
            d.TaxBaseFallbackGDP = baseTaxable;

            double taxableBase = baseTaxable * d.TaxRateLocal;
            d.IncomeBeforeModifiers = SafeLong(taxableBase);

            float we01 = Mathf.Clamp01(d.WarExhaustion / 100f);
            d.TaxPenaltyFromWar = we01 * d.MaxWeTaxPenaltyPct;
            taxableBase *= (1.0 - d.TaxPenaltyFromWar / 100.0);
            d.IncomeAfterWarPenalty = SafeLong(taxableBase);
            
            d.TaxModifierFromStability = ((d.Stability - 50f) / 50f) * d.MaxStabTaxBonusPct;
            taxableBase *= (1.0 + d.TaxModifierFromStability / 100.0);
            d.IncomeAfterStability = SafeLong(taxableBase);
            
            float cityBonus = Mathf.Clamp(d.Cities * d.CityWealthBonusPerCityPct, 0f, d.CityWealthBonusCapPct);
            d.TaxModifierFromCities = cityBonus;
            taxableBase *= (1.0 + cityBonus / 100.0);
            d.IncomeAfterCityBonus = SafeLong(taxableBase);
            
            float industryMod = (d.FactoryOutputModifier + d.ResourceOutputModifier) / 2f;
            float econScale = ComputeEconomyScale(d.Population) * industryMod;
            
            long tradeIn = SafeLong(TradeManager.GetTradeIncome(k) * d.TradeIncomeModifier);
            d.TradeIncome = tradeIn;
            
            d.Income = SafeLong(taxableBase * econScale) + tradeIn;

             if (isAnarchy)
            {
                // Income decrease (User requested) -> e.g. -50%
                d.Income = SafeLong(d.Income * 0.5f);
            }

            // 4. Expenses Calc
            // Administration & Logistics Scaling
            // "kingdom with 3 cities pay more" -> We scale aggresively starting from city 2+
            float infraScale = 1.0f + Mathf.Max(0, (d.Cities - 1) * 0.15f); // +15% admin cost per additional city (3 cities = +30%)
            // "military scaling do for every 20 citizen" -> 1% per 20 citizens
            float milScale = 1.0f + ((d.Population / 20.0f) * 0.01f); // +1% per 20 citizens (e.g. 100 pop = +5%, 1000 pop = +50%)

            long military = SafeLong(d.Soldiers * d.MilitaryCostPerSoldier * d.MilitaryUpkeepModifier * milScale);
            long infra = SafeLong((d.Cities * d.CostPerCity + d.Buildings * d.CostPerBuilding) * infraScale);
            long demo = 0; 
            
            d.ExpensesMilitary = military;
            d.ExpensesInfrastructure = infra;
            d.ExpensesDemography = demo;
            
            long baseExpenses = military + infra + demo;
            // War Overhead Accumulator Logic
            if (d.WarExhaustion > 5f)
            {
                // Increase very slowly if WE > 5
                d.WarOverheadAccumulator += 0.005f * deltaWorldSeconds; 
            }
            else
            {
                // Decay if low WE
                d.WarOverheadAccumulator -= 0.01f * deltaWorldSeconds;
            }
            d.WarOverheadAccumulator = Mathf.Clamp(d.WarOverheadAccumulator, 0f, 100f);

            // Use Accumulator for overhead expenses
            long warOverhead = SafeLong(baseExpenses * (d.WarOverheadAccumulator / 100.0));
            d.ExpensesWarOverhead = warOverhead;

            long econSpending = CalculateEconomicSpending(d);
            d.ExpensesLawUpkeep = econSpending;
            
            long tradeOut = TradeManager.GetTradeExpenses(k);
            d.TradeExpenses = tradeOut;

            long corruptionCost = SafeLong((baseExpenses + warOverhead) * d.CorruptionLevel);
            d.ExpensesCorruption = corruptionCost;

            d.Expenses = Math.Max(0, SafeLong((baseExpenses + warOverhead) * econScale) + tradeOut + corruptionCost + econSpending);

            if (isAnarchy)
            {
                // Expenses increase 200% -> Total = 300%
                d.Expenses = SafeLong(d.Expenses * 3.0f);
            }

            // 5. Update Treasury (Every 5 seconds)
            d.TreasuryTimer += deltaWorldSeconds;
            if (d.TreasuryTimer >= 5f)
            {
                long yearlyBalance = d.Income - d.Expenses;
                d.Treasury += SafeLong(yearlyBalance / 12.0);
                d.TreasuryTimer = 0f;
            }

            // Check for bankruptcy/low income and reset laws
            /*if (d.Balance <= 0)
            {
                if (d.MilitarySpending != "None") d.MilitarySpending = "None";
                if (d.SecuritySpending != "None") d.SecuritySpending = "None";
                if (d.GovernmentSpending != "None") d.GovernmentSpending = "None";
                if (d.WelfareSpending != "None") d.WelfareSpending = "None";
                if (d.EducationSpending != "None") d.EducationSpending = "None";
                if (d.ResearchSpending != "None") d.ResearchSpending = "None";
                if (d.AntiCorruption != "None") d.AntiCorruption = "None";
                EconomicLawsWindow.Refresh(k);
            }*/
            
            UpdateResources(k, d);
            UpdateManpower(k, d, deltaWorldSeconds);
            UpdateWarExhaustion(k, d, deltaWorldSeconds); 
            
            if (!d.HasInitializedStability) { d.Stability = 50f; d.HasInitializedStability = true; }
            UpdateStability(k, d, deltaWorldSeconds);

            // --- 6. Plague Risk Calculation ---
            // Accumulate risk over time
            d.PlagueRiskAccumulator += (Math.Min(d.Population, 20000) / 100f) * deltaWorldSeconds * 0.001f; // Slower accumulation
            // Decay resistance over time
            float decayRate = 0.02f;
            if (d.WelfareSpending == "High") decayRate = 0.0005f;
            else if (d.WelfareSpending == "Maximum") decayRate = 0.0001f;
            
            d.PlagueResistanceDecay += deltaWorldSeconds * decayRate;

            // --- Anarchy Population Decline ---
            if (isAnarchy && k.units != null && k.units.Count > 0)
            {
                // Active population decline
                // Kill chance proportional to delta time. e.g. 1% of population dies every few seconds?
                // Let's make it relatively slow but noticeable: 0.1% per second
                float killChance = 0.001f * deltaWorldSeconds;
                if (UnityEngine.Random.value < 0.05f) // Optimization: only run check half the time or for partial list
                {
                   int killBudget = Mathf.Max(1, (int)(d.Population * killChance));
                    // Simple kill loop - iterate backwards or just pick randoms?
                    // Safe approach: pick random
                   int attempts = 0;
                   while(killBudget > 0 && attempts < 20)
                   {
                        attempts++;
                        if (k.units.Count == 0) break;
                        var victim = k.units.GetRandom();
                        if (victim != null && victim.isAlive() && !victim.isKing() && !victim.isCityLeader())
                        {
                            victim.getHit(10000f, true, AttackType.Other, null, true, false);
                            killBudget--;
                        }
                   }
                }
            }

            float risk = 10f + d.PlagueRiskAccumulator;
            risk += d.Cities * 2f;
            
            float resistance = 500f; // Base resistance to prevent immediate plague
            resistance += (d.PlagueResistanceModifier * 100f);
            resistance -= d.PlagueResistanceDecay;
            
            d.PlagueRisk = Mathf.Clamp(risk, 0f, 100f);
            d.PlagueResistance = Mathf.Max(0f, resistance); // Store for display/logic

            // --- 7. Research Progress ---
            if (k.getCulture() != null)
            {
                float knowledgeGain = 0.1f * d.ResearchOutputModifier * deltaWorldSeconds;
                //k.getCulture().data.knowledge_progress += knowledgeGain;
            }
        }

        public static void UpdateCounts(Kingdom k, Data d)
        {
            // 1. Buildings & Cities
            if (k.cities != null)
            {
                d.Cities = k.cities.Count;
                int bCount = 0;
                foreach(var c in k.cities)
                {
                    if (c.buildings != null) bCount += c.buildings.Count;
                }
                d.Buildings = bCount;
            }
            else
            {
                d.Cities = 0;
                d.Buildings = 0;
            }
            
            // 2. Population (General)
            d.PrevPopulation = d.Population;
            d.Population = k.getPopulationPeople();
            d.AvgGrowthRate = (d.PopulationGrowthBonus * 100f); 

            // 3. Reset Counters
            d.Adults = 0;
            d.Soldiers = 0;
            d.Children = 0;
            d.Babies = 0;
            d.Teens = 0;
            d.Elders = 0;
            d.Veterans = 0;
            d.Genius = 0;
            d.Homeless = 0;
            d.Unemployed = 0;
            d.Hungry = 0;
            d.Starving = 0;
            d.Sick = 0;
            d.HappyUnits = 0;

            // 4. Detailed Iteration (Using k.units property)
            if (k.units != null)
            {
                foreach(var a in k.units)
                {
                    if (a == null || !a.isAlive()) continue;

                    // --- Age & Role ---
                    if (!a.isAdult())
                    {
                        d.Children++;
                        if (a.isBaby()) d.Babies++;
                        else d.Teens++;

                        // --- Education System: Genius Generation ---
                        if (d.GeniusChanceModifier > 0f && !a.hasTrait("genius"))
                        {
                            if (UnityEngine.Random.value < (d.GeniusChanceModifier / 240f))
                            {
                                a.addTrait("genius");
                            }
                        }
                    }
                    else 
                    {
                        d.Adults++;
                        if (a.getAge() >= 60) d.Elders++;
                        
                        // Check Professions
                        bool isMilitary = a.isWarrior();
                        bool isLeader = a.isCityLeader() || a.isKing();

                        if (isMilitary) d.Soldiers++;
                        
                        // Unemployed: Adult, not military, not leader
                        if (!isMilitary && !isLeader) d.Unemployed++; 
                    }

                    // --- Traits & Status ---
                    if (a.hasTrait("genius")) d.Genius++;
                    if (a.hasTrait("veteran")) d.Veterans++;
                    
                    if (a.isHappy()) d.HappyUnits++;
                    if (a.isHungry()) d.Hungry++;
                    if (a.isStarving()) d.Starving++;
                    if (a.isSick()) d.Sick++;
                    
                    // --- Housing ---
                    if (!a.hasHouse()) d.Homeless++;
                }
            }
        }

        private static void ApplyLeaderModifiers(Data d)
        {
            if (d.ActiveLeaders == null) return;
            
            for (int i = d.ActiveLeaders.Count - 1; i >= 0; i--)
            {
                var leader = d.ActiveLeaders[i];
                
                // CRASH FIX: Check if leader object itself is null
                if (leader == null)
                {
                    d.ActiveLeaders.RemoveAt(i);
                    continue;
                }

                // Check if unit is dead (only if it has a link)
                if (leader.UnitLink != null && !leader.UnitLink.isAlive())
                {
                    d.ActiveLeaders.RemoveAt(i);
                    string name = leader.Name ?? "Leader";
                    WorldTip.showNow($"{name} has died!", true, "top", 3f);
                    continue;
                }

                d.StabilityTargetModifier += leader.StabilityBonus;
                d.PoliticalPowerGainModifier += leader.PPGainBonus; 
                d.MilitaryAttackModifier += leader.AttackBonus;
                d.ResearchOutputModifier += leader.ResearchBonus;
                d.TaxRateLocal += leader.TaxBonus; 
                d.CorruptionLevel -= leader.CorruptionReduction;
            }
        }
        
        // --- Economic Laws (New System) ---
        private static void ApplyEconomicLaws_Modifiers(Data d)
        {
            // 1. Taxation
            switch (d.TaxationLevel)
            {
                case "Minimum": 
                    d.TaxRateLocal *= 0.40f; // -60%
                    d.StabilityTargetModifier += 20f;
                    d.WarExhaustionGainMultiplier -= 0.10f;
                    break;
                case "Low": 
                    d.TaxRateLocal *= 0.70f; // -30%
                    d.StabilityTargetModifier += 10f;
                    d.WarExhaustionGainMultiplier -= 0.05f;
                    break;
                case "Normal":
                    d.StabilityTargetModifier -= 5f; // From tooltip description
                    break;
                case "High": 
                    d.TaxRateLocal *= 1.30f; 
                    d.StabilityTargetModifier -= 10f;
                    d.WarExhaustionGainMultiplier += 0.03f;
                    break;
                case "Maximum": 
                    d.TaxRateLocal *= 1.75f; 
                    d.StabilityTargetModifier -= 20f;
                    d.WarExhaustionGainMultiplier += 0.10f;
                    break;
            }

            // 2. Military Spending
            switch (d.MilitarySpending)
            {
                case "None":
                    // Base: Very Slow (1.5%/m) -> d.ManpowerRegenRate = 0.015 is default
                    break;
                case "Low":
                    d.ManpowerMaxMultiplier *= 1.4f;
                    d.ManpowerRegenRate = 0.030f; // 3.0%
                    d.StabilityTargetModifier += 7.5f;
                    d.WarExhaustionGainMultiplier += 0.20f;
                    break;
                case "Medium":
                    d.ManpowerMaxMultiplier *= 1.6f;
                    d.ManpowerRegenRate = 0.050f; // 5.0%
                    d.StabilityTargetModifier += 10.0f;
                    d.WarExhaustionGainMultiplier += 0.30f;
                    break;
                case "High":
                    d.ManpowerMaxMultiplier *= 1.8f;
                    d.ManpowerRegenRate = 0.075f; // 7.5%
                    d.StabilityTargetModifier += 12.5f;
                    d.WarExhaustionGainMultiplier += 0.35f;
                    break;
                case "Maximum":
                    d.ManpowerMaxMultiplier *= 2.0f;
                    d.ManpowerRegenRate = 0.100f; // 10.0%
                    d.StabilityTargetModifier += 15.0f;
                    d.WarExhaustionGainMultiplier += 0.40f;
                    d.CorruptionLevel += 0.15f;
                    break;
            }

            // 3. Security Spending
            // Tooltip: Stab -15, WE Gain +1.5%?, Corruption +4%?
            // "Security" usually implies repression here based on negative stability stats
            switch(d.SecuritySpending)
            {
                case "Low": 
                    d.StabilityTargetModifier -= 15.0f; 
                    d.WarExhaustionGainMultiplier += 0.015f; 
                    d.CorruptionLevel += 0.04f; 
                    break;
                case "Medium": 
                    d.StabilityTargetModifier -= 10.0f; 
                    d.WarExhaustionGainMultiplier += 0.01f; 
                    d.CorruptionLevel += 0.06f; 
                    break;
                case "High": 
                    d.StabilityTargetModifier -= 5.0f; 
                    d.WarExhaustionGainMultiplier += 0.005f; 
                    d.CorruptionLevel += 0.08f; 
                    break;
                case "Maximum": 
                    // Stab Normal
                    d.WarExhaustionGainMultiplier += 0.10f; 
                    d.CorruptionLevel += 0.10f; 
                    break;
            }

            // 4. Government Spending
            switch(d.GovernmentSpending)
            {
                case "Low": 
                    d.FactoryOutputModifier *= 0.95f; // Eff -5%
                    d.StabilityTargetModifier -= 4.0f;
                    break;
                case "Medium": 
                    // Normal Eff
                    d.CorruptionLevel -= 0.10f; 
                    break;
                case "High": 
                    d.FactoryOutputModifier *= 1.10f; 
                    d.StabilityTargetModifier += 10.0f; 
                    d.CorruptionLevel -= 0.20f; 
                    break;
                case "Maximum": 
                    d.FactoryOutputModifier *= 1.20f; 
                    d.StabilityTargetModifier += 20.0f; 
                    d.CorruptionLevel -= 0.30f; 
                    break;
            }

            // 5. Healthcare (Welfare)
            switch(d.WelfareSpending)
            {
                case "Low":
                    d.PlagueResistanceModifier += 0.20f;
                    d.StabilityTargetModifier -= 5.0f;
                    break;
                case "Medium":
                    d.PlagueResistanceModifier += 0.40f;
                    d.WarExhaustionGainMultiplier -= 0.03f;
                    break;
                case "High":
                    d.PlagueResistanceModifier += 0.60f;
                    d.StabilityTargetModifier += 5.0f;
                    d.WarExhaustionGainMultiplier -= 0.06f;
                    break;
                case "Maximum":
                    d.PlagueResistanceModifier += 0.80f;
                    d.StabilityTargetModifier += 10.0f;
                    d.WarExhaustionGainMultiplier -= 0.10f;
                    break;
            }

            // 6. Education
            switch(d.EducationSpending)
            {
                case "Low":
                    d.GeniusChanceModifier += 0.02f;
                    d.WarExhaustionGainMultiplier -= 0.02f;
                    break;
                case "Medium":
                    d.GeniusChanceModifier += 0.05f;
                    d.StabilityTargetModifier += 10.0f;
                    d.WarExhaustionGainMultiplier -= 0.04f;
                    break;
                case "High":
                    d.GeniusChanceModifier += 0.08f;
                    d.StabilityTargetModifier += 20.0f;
                    d.WarExhaustionGainMultiplier -= 0.07f;
                    break;
                case "Maximum":
                    d.GeniusChanceModifier += 0.10f;
                    d.StabilityTargetModifier += 30.0f;
                    d.WarExhaustionGainMultiplier -= 0.10f;
                    break;
            }

            // 7. Research Spending
            switch(d.ResearchSpending)
            {
                case "Low":
                    d.ResearchOutputModifier *= 2.5f;
                    d.StabilityTargetModifier -= 5.0f;
                    break;
                case "Medium":
                    d.ResearchOutputModifier *= 4.0f;
                    d.WarExhaustionGainMultiplier -= 0.04f;
                    break;
                case "High":
                    d.ResearchOutputModifier *= 5.0f;
                    d.StabilityTargetModifier += 5.0f;
                    d.WarExhaustionGainMultiplier -= 0.08f;
                    break;
                case "Maximum":
                    d.ResearchOutputModifier *= 6.0f;
                    d.StabilityTargetModifier += 10.0f;
                    d.WarExhaustionGainMultiplier -= 0.12f;
                    break;
            }

            // 8. Anti Corruption
            switch(d.AntiCorruption)
            {
                case "Low":
                    d.CorruptionLevel -= 0.20f;
                    d.StabilityTargetModifier += 12.0f;
                    break;
                case "Medium":
                    d.CorruptionLevel -= 0.30f;
                    d.StabilityTargetModifier += 18.0f;
                    break;
                case "High":
                    d.CorruptionLevel -= 0.40f;
                    d.StabilityTargetModifier += 24.0f;
                    break;
                case "Maximum":
                    d.CorruptionLevel -= 0.50f;
                    d.StabilityTargetModifier += 30.0f;
                    break;
            }
        }

        private static long CalculateEconomicSpending(Data d)
        {
            long total = 0;
            // Costs calculated as % of Income
            long inc = d.Income; 

            total += GetCost(d.MilitarySpending, inc, 0.10f, 0.25f, 0.40f, 0.60f);
            total += GetCost(d.SecuritySpending, inc, 0.05f, 0.15f, 0.25f, 0.35f);
            total += GetCost(d.GovernmentSpending, inc, 0.05f, 0.15f, 0.25f, 0.35f);
            total += GetCost(d.WelfareSpending, inc, 0.05f, 0.15f, 0.25f, 0.35f);
            total += GetCost(d.EducationSpending, inc, 0.05f, 0.15f, 0.25f, 0.35f);
            total += GetCost(d.ResearchSpending, inc, 0.10f, 0.20f, 0.30f, 0.40f);
            total += GetCost(d.AntiCorruption, inc, 0.10f, 0.20f, 0.30f, 0.40f);
            
            return total;
        }

        private static long GetCost(string level, long income, float low, float med, float high, float max)
        {
            float pct = 0f;
            switch(level)
            {
                case "Low": pct = low; break;
                case "Medium": pct = med; break;
                case "High": pct = high; break;
                case "Maximum": pct = max; break;
            }
            return (long)(income * pct);
        }

        private static void ApplyRiseOfNationsLaws(Data d)
        {
            // 1. Conscription
            switch (d.Law_Conscription)
            {
                case "Disarmed":
                    d.ManpowerMaxMultiplier *= 0.5f; 
                    d.ManpowerRegenRate *= 0.5f;
                    d.TaxRateLocal *= 1.05f;
                    break;
                case "Limited":
                    d.ManpowerMaxMultiplier *= 1.5f; 
                    d.ManpowerRegenRate *= 1.5f;
                    d.TaxRateLocal *= 0.90f; 
                    d.BuildingSpeedModifier *= 0.90f; 
                    break;
                case "Extensive":
                    d.ManpowerMaxMultiplier *= 2.0f; 
                    d.ManpowerRegenRate *= 2.0f;
                    d.TaxRateLocal *= 0.75f; 
                    d.BuildingSpeedModifier *= 0.75f; 
                    break;
                case "Required":
                    d.ManpowerMaxMultiplier *= 2.5f; 
                    d.ManpowerRegenRate *= 2.5f;
                    d.TaxRateLocal *= 0.35f; 
                    d.BuildingSpeedModifier *= 0.50f; 
                    d.StabilityTargetModifier -= 10f; 
                    break;
            }

            // 2. War Bonds
            switch (d.Law_WarBonds)
            {
                case "Moderate":
                    d.TaxRateLocal *= 1.5f;
                    d.MilitaryUpkeepModifier *= 0.75f;
                    d.StabilityTargetModifier -= 8f;
                    d.UnrestReductionModifier *= 0.8f; 
                    d.WarExhaustionGainMultiplier += 0.05f;
                    break;
                case "Maximum":
                    d.TaxRateLocal *= 2.25f;
                    d.MilitaryUpkeepModifier *= 0.5f;
                    d.StabilityTargetModifier -= 15f;
                    d.UnrestReductionModifier *= 0.65f; 
                    d.WarExhaustionGainMultiplier += 0.15f;
                    break;
            }

            // 3. Elitist Military Stance
            if (d.Law_ElitistMilitary == "Expanded")
            {
                d.CorruptionLevel += 0.001f;
            }

            // 4. Party Loyalty
            switch (d.Law_PartyLoyalty)
            {
                case "Minimum": 
                    d.TaxRateLocal *= 1.1f; 
                    d.PoliticalPowerGainModifier *= 1.25f;
                    d.IdeologyPowerModifier *= 0.85f;
                    d.LeaderXPModifier += 10f;
                    break;
                case "Low":
                    d.PoliticalPowerGainModifier *= 1.15f;
                    d.IdeologyPowerModifier *= 0.95f;
                    d.LeaderXPModifier += 5f;
                    break;
                case "High":
                    d.PoliticalPowerGainModifier *= 0.9f;
                    d.IdeologyPowerModifier *= 1.1f;
                    d.LeaderXPModifier -= 3f;
                    break;
                case "Maximum": 
                    d.TaxRateLocal *= 0.9f; 
                    d.PoliticalPowerGainModifier *= 0.8f;
                    d.IdeologyPowerModifier *= 1.25f;
                    d.LeaderXPModifier -= 8f;
                    break;
            }

            // 5. Power Sharing
            switch (d.Law_Centralization)
            {
                case "Decentralized": 
                    d.TaxRateLocal *= 1.05f; 
                    d.UnrestReductionModifier *= 1.05f;
                    break; 
                case "Centralized": 
                    d.StabilityTargetModifier += 2.5f; 
                    break;
            }

            // 6. Press Regulation
            switch (d.Law_PressRegulation)
            {
                case "Free Press": 
                    d.TaxRateLocal *= 1.1f; 
                    d.CorruptionLevel -= 0.1f; 
                    d.IntegrationSpeedModifier *= 1.25f;
                    d.UnrestReductionModifier *= 0.75f;
                    break;
                case "Laxed": 
                    d.TaxRateLocal *= 1.05f; 
                    d.CorruptionLevel -= 0.05f; 
                    d.IntegrationSpeedModifier *= 1.1f;
                    d.UnrestReductionModifier *= 0.9f; 
                    break;
                case "Mixed": 
                    d.TaxRateLocal *= 0.95f; 
                    d.StabilityTargetModifier += 5f; 
                    d.IntegrationSpeedModifier *= 0.9f;
                    d.UnrestReductionModifier *= 1.15f; 
                    break;
                case "State Focus": 
                    d.TaxRateLocal *= 0.9f; 
                    d.StabilityTargetModifier += 10f; 
                    d.IntegrationSpeedModifier *= 0.85f;
                    d.UnrestReductionModifier *= 1.25f; 
                    break;
                case "Propaganda": 
                    d.TaxRateLocal *= 0.9f; 
                    d.StabilityTargetModifier += 10f; 
                    d.WarExhaustionGainMultiplier -= 0.03f; 
                    d.PoliticalPowerGainModifier *= 1.1f; 
                    d.UnrestReductionModifier *= 1.25f; 
                    break;
            }

            // 7. Firearm Regulation
            switch (d.Law_FirearmRegulation)
            {
                case "No Restr.": 
                    d.TaxRateLocal *= 1.13f; 
                    d.StabilityTargetModifier -= 5f; 
                    d.CityResistanceModifier *= 1.5f; 
                    d.UnrestReductionModifier *= 0.6f;
                    break;
                case "Reduced": 
                    d.TaxRateLocal *= 1.05f; 
                    d.StabilityTargetModifier -= 2.5f; 
                    d.CityResistanceModifier *= 1.25f; 
                    d.UnrestReductionModifier *= 0.8f; 
                    break;
                case "Expanded": 
                    d.StabilityTargetModifier += 5f; 
                    d.CityResistanceModifier *= 0.67f; 
                    d.UnrestReductionModifier *= 1.15f;
                    d.RebelSuppressionModifier += 0.5f;
                    break;
                case "Illegal": 
                    d.StabilityTargetModifier += 15f; 
                    d.CityResistanceModifier *= 0.4f; 
                    d.UnrestReductionModifier *= 1.4f; 
                    d.RebelSuppressionModifier += 1.0f;
                    break;
            }

            // 8. Religious Emphasis
            switch (d.Law_Religion)
            {
                case "Atheism": 
                    d.TaxRateLocal *= 1.05f; 
                    d.ResearchOutputModifier *= 1.1f; 
                    d.IdeologyPowerModifier *= 1.05f;
                    d.IntegrationSpeedModifier *= 1.05f;
                    d.UnrestReductionModifier *= 0.9f; 
                    break;
                case "State Rel.": 
                    d.TaxRateLocal *= 0.95f; 
                    d.ResearchOutputModifier *= 0.9f; 
                    d.StabilityTargetModifier += 5f; 
                    d.PopulationGrowthBonus += 0.0125f;
                    d.WarExhaustionGainMultiplier -= 0.005f; 
                    d.UnrestReductionModifier *= 1.1f;
                    d.IntegrationSpeedModifier *= 0.95f;
                    d.IdeologyPowerModifier *= 0.95f;
                    d.ManpowerMaxMultiplier *= 1.1f; 
                    break;
            }

            // 9. Population Growth
            switch (d.Law_PopulationGrowth)
            {
                case "Encouraged": 
                    d.PopulationGrowthBonus += 0.025f; 
                    d.TaxRateLocal *= 0.85f; 
                    d.FactoryOutputModifier *= 0.85f;
                    d.ResourceOutputModifier *= 0.85f;
                    break;
                case "Mandatory": 
                    d.PopulationGrowthBonus += 0.05f; 
                    d.TaxRateLocal *= 0.7f; 
                    d.FactoryOutputModifier *= 0.7f;
                    d.ResourceOutputModifier *= 0.7f;
                    break;
            }

            // 10. Industrial Specialization
            switch (d.Law_IndustrialSpec)
            {
                case "Extraction": 
                    d.ResourceOutputModifier *= 1.25f; 
                    d.FactoryOutputModifier *= 0.75f; 
                    d.TaxRateLocal *= 0.8f; 
                    break;
                case "Manufacturing": 
                    d.ResourceOutputModifier *= 0.75f; 
                    d.FactoryOutputModifier *= 1.25f; 
                    d.TaxRateLocal *= 0.8f; 
                    break;
            }

            // 11. Resource Subsidization
            switch (d.Law_ResourceSubsidy)
            {
                case "Limited": 
                    d.TradeIncomeModifier *= 2f; 
                    d.TaxRateLocal *= 0.9f; 
                    d.FactoryOutputModifier *= 0.8f; 
                    d.ResourceOutputModifier *= 0.85f; 
                    break;
                case "Moderate": 
                    d.TradeIncomeModifier *= 3.5f; 
                    d.TaxRateLocal *= 0.85f; 
                    d.FactoryOutputModifier *= 0.7f; 
                    break;
                case "Generous": 
                    d.TradeIncomeModifier *= 5f; 
                    d.TaxRateLocal *= 0.75f; 
                    d.FactoryOutputModifier *= 0.6f; 
                    break;
            }

            // 12. Working Hours
            switch (d.Law_WorkingHours)
            {
                case "Minimum": 
                    d.PopulationGrowthBonus += 0.01f; 
                    d.StabilityTargetModifier += 10f; 
                    d.UnrestReductionModifier *= 1.5f;
                    d.TaxRateLocal *= 0.75f; 
                    d.FactoryOutputModifier *= 0.75f;
                    d.ResourceOutputModifier *= 0.8f;
                    break;
                case "Reduced": 
                    d.StabilityTargetModifier += 5f; 
                    d.UnrestReductionModifier *= 1.3f;
                    d.TaxRateLocal *= 0.85f; 
                    d.FactoryOutputModifier *= 0.9f;
                    break;
                case "Extended": 
                    d.StabilityTargetModifier -= 5f; 
                    d.UnrestReductionModifier *= 0.75f;
                    d.TaxRateLocal *= 1.25f; 
                    d.BuildingSpeedModifier *= 1.25f;
                    d.FactoryOutputModifier *= 1.1f;
                    d.ResourceOutputModifier *= 1.25f;
                    d.PopulationGrowthBonus -= 0.005f;
                    break;
                case "Unlimited": 
                    d.PopulationGrowthBonus -= 0.015f; 
                    d.StabilityTargetModifier -= 15f; 
                    d.UnrestReductionModifier *= 0.4f;
                    d.TaxRateLocal *= 1.5f; 
                    d.BuildingSpeedModifier *= 1.5f;
                    d.FactoryOutputModifier *= 1.3f;
                    d.ResourceOutputModifier *= 1.3f;
                    break;
            }

            // 13. Research Focus
            switch (d.Law_ResearchFocus)
            {
                case "Civilian": 
                    d.ResearchOutputModifier *= 0.85f; 
                    break;
                case "Military": 
                    d.ResearchOutputModifier *= 0.85f; 
                    break;
            }
            
            // 14. Monarch
            switch (d.Law_Monarch)
            {
                case "Constitutional":
                    d.StabilityTargetModifier += 5f;
                    d.ManpowerMaxMultiplier *= 1.1f;
                    d.PoliticalPowerGainModifier *= 1.2f;
                    d.TaxRateLocal *= 1.15f;
                    d.MilitaryUpkeepModifier *= 0.9f;
                    d.UnrestReductionModifier *= 0.8f;
                    d.IdeologyPowerModifier *= 0.75f;
                    break;
                case "Absolute":
                    d.StabilityTargetModifier += 10f;
                    d.ManpowerMaxMultiplier *= 1.2f;
                    d.PoliticalPowerGainModifier *= 1.2f;
                    d.TaxRateLocal *= 1.15f;
                    d.MilitaryUpkeepModifier *= 0.9f;
                    d.UnrestReductionModifier *= 0.7f;
                    d.IdeologyPowerModifier *= 0.5f;
                    break;
            }

            // 15. Collective Theory
            switch (d.Law_CollectiveTheory)
            {
                case "Maoism": d.ResourceOutputModifier *= 1.25f; break;
                case "Marxism": d.FactoryOutputModifier *= 1.15f; break;
                case "Leninist": d.PoliticalPowerGainModifier *= 1.15f; break;
                case "Stalinist": d.StabilityTargetModifier += 5f; d.UnrestReductionModifier *= 1.1f; d.CorruptionLevel += 0.02f; break;
                case "Trotskyism": d.TaxRateLocal *= 0.925f; d.WarExhaustionGainMultiplier -= 0.02f; d.JustificationTimeModifier *= 0.85f; break;
            }
            
            // 16. Elective Assembly
            switch(d.Law_ElectiveAssembly)
            {
                case "Direct": d.TaxRateLocal *= 1.35f; d.PoliticalPowerGainModifier *= 0.95f; break;
                case "Indirect": d.LeaderXPModifier += 5f; break;
                case "Technocratic": d.ResearchOutputModifier *= 1.2f; break;
            }
            
            // 17. Democracy Style
            switch(d.Law_DemocracyStyle)
            {
                case "Parliamentary": d.LeaderXPModifier += 15f; break;
                case "Semi-Presidential": d.InvestmentAvailabilityModifier *= 1.25f; d.InvestmentCostModifier *= 0.95f; break;
                case "Presidential": d.PoliticalPowerGainModifier *= 1.15f; d.StabilityTargetModifier += 5f; break;
            }
            
            // 18. State Doctrine
            switch(d.Law_StateDoctrine)
            {
                case "Corporatist": d.FactoryOutputModifier *= 1.25f; break;
                case "Classical": d.LeaderXPModifier -= 5f; break; 
                case "Stratocracy": d.ManpowerMaxMultiplier *= 1.2f; break;
                case "Clerical": d.PopulationGrowthBonus += 0.005f; d.WarExhaustionGainMultiplier -= 0.0025f; d.UnrestReductionModifier *= 1.1f; break;
                case "Falangism": d.BuildingSpeedModifier *= 1.1f; d.IntegrationSpeedModifier *= 1.1f; d.UnrestReductionModifier *= 1.15f; d.JustificationTimeModifier *= 1.1f; break;
            }
        }

        private static void ApplyActivePolicies(Data d)
        {
            if (d.ActivePolicies == null) return;

            foreach (string pid in d.ActivePolicies)
            {
                // Find definition to get Upkeep cost
                var def = PoliciesWindow.Policies.Find(p => p.Id == pid);
                if (def != null)
                {
                    d.ExpensesLawUpkeep += def.Upkeep;
                }

                // Apply Effects
                switch (pid)
                {
                    case "welfare_act":
                        d.StabilityTargetModifier += 5f;
                        d.TaxRateLocal *= 0.90f;
                        break;
                    case "public_service":
                        d.FactoryOutputModifier *= 1.2f;
                        d.ResourceOutputModifier *= 1.2f;
                        break;
                    case "military_service":
                        d.MilitaryUpkeepModifier *= 0.9f;
                        d.ManpowerMaxMultiplier *= 1.2f;
                        break;
                    case "central_authority":
                        d.UnrestReductionModifier *= 1.33f; // "Unrest Reduction +33%"
                        d.IntegrationSpeedModifier *= 1.25f;
                        break;
                    case "prosperity_act":
                        d.TaxRateLocal *= 1.1f;
                        break;
                    case "infrastructure":
                        d.TaxRateLocal *= 1.1f;
                        d.BuildingSpeedModifier *= 1.05f;
                        break;
                    case "war_fund":
                        d.TaxRateLocal *= 1.5f;
                        d.MilitaryUpkeepModifier *= 0.75f;
                        d.StabilityTargetModifier -= 20f;
                        d.UnrestReductionModifier *= 0.85f;
                        d.WarExhaustionGainMultiplier += 0.01f; 
                        break;
                    case "martial_law":
                        d.TaxRateLocal *= 0.75f;
                        d.StabilityTargetModifier += 60f;
                        d.UnrestReductionModifier *= 1.33f;
                        d.FactoryOutputModifier *= 0.8f;
                        d.ResourceOutputModifier *= 0.8f;
                        break;
                    case "research_bureau":
                        d.ResearchOutputModifier *= 1.2f;
                        break;
                    case "encourage_dev":
                        d.TaxRateLocal *= 0.9f;
                        d.InvestmentAvailabilityModifier *= 1.5f;
                        break;
                    case "tax_reform":
                        d.TaxRateLocal *= 1.25f;
                        break;
                    case "forced_labour":
                        d.TaxRateLocal *= 1.15f;
                        d.FactoryOutputModifier *= 1.4f;
                        d.ResourceOutputModifier *= 1.5f;
                        d.PopulationGrowthBonus -= 0.05f;
                        d.CorruptionLevel -= 0.10f;
                        break;
                }
            }
        }

        // --- Helper Methods ---
        private static float GetSecondsPerYear()
        {
            var ms = World.world?.map_stats;
            if (ms == null || ms.world_ages_speed_multiplier <= 0f) return 60f;
            return ms.current_world_ages_duration / ms.world_ages_speed_multiplier;
        }

        private static float ComputeEconomyScale(long population)
        {
            if (population <= 0) return 0f;
            float t = Mathf.Clamp01(Mathf.Log10(population) / 4f);
            return Mathf.Lerp(0.2f, 1f, t);
        }
        
        private static long SafeLong(double v)
        {
            if (v > long.MaxValue) return long.MaxValue;
            if (v < long.MinValue) return long.MinValue;
            return (long)Math.Round(v);
        }

        private static void UpdateResources(Kingdom k, Data d)
        {
            if (d.ResourceRates == null) d.ResourceRates = new Dictionary<string, float>();
            if (d.ResourceStockpiles == null) d.ResourceStockpiles = new Dictionary<string, int>();

            var currentStockpiles = new Dictionary<string, int>();
            if (k.cities != null)
            {
                foreach (var city in k.cities)
                {
                    if (city == null || !city.isAlive()) continue;
                    foreach (string resId in TrackedResources)
                    {
                        if (!currentStockpiles.ContainsKey(resId)) currentStockpiles[resId] = 0;
                        currentStockpiles[resId] += city.getResourcesAmount(resId);
                    }
                }
            }

            foreach (string resId in TrackedResources)
            {
                int current = currentStockpiles.ContainsKey(resId) ? currentStockpiles[resId] : 0;
                int previous = d.ResourceStockpiles.ContainsKey(resId) ? d.ResourceStockpiles[resId] : 0;
                d.ResourceRates[resId] = current - previous;
                d.ResourceStockpiles[resId] = current;
            }
        }

        private static void UpdatePopulation(Kingdom k, Data d, long currentPop, float delta)
        {
            d.Population = currentPop;
            d.AvgGrowthRate = (d.PopulationGrowthBonus * 100f); 
            d.Adults = k.countAdults();
            d.Soldiers = k.countTotalWarriors();
        }
        
        private static void UpdateManpower(Kingdom k, Data d, float delta)
        {
            long baseEligible = Math.Max(0, d.Adults - d.Soldiers);
            long maxPoints = SafeLong(baseEligible * 0.5f * d.ManpowerMaxMultiplier) + (d.Cities * 10);
            d.ManpowerMax = maxPoints;
            
            if (d.ManpowerCurrent < d.ManpowerMax) {
                float regenAmount = (float)maxPoints * d.ManpowerRegenRate * (delta / 60f);
                d.ManpowerAccumulator += regenAmount;
                if (d.ManpowerAccumulator >= 1f) {
                    long add = (long)d.ManpowerAccumulator;
                    d.ManpowerAccumulator -= add;
                    d.ManpowerCurrent += add;
                }
            }
        }
        
        private static void UpdateWarExhaustion(Kingdom k, Data d, float delta)
        {
            // 1. Check for active wars
            var wars = World.world.wars.getWars(k);
            bool atWar = false;
            foreach(var w in wars)
            {
                if (!w.hasEnded()) { atWar = true; break; }
            }

            // 2. Calculate Change
            if (atWar)
            {
                // Base gain: +0.2 per second (modified by laws)
                // E.g. 60 seconds (1 year) of war = +12 WE (without modifiers)
                float baseGain = 0.05f * delta;
                d.WEChange = baseGain * d.WarExhaustionGainMultiplier;
                
                // Move towards 100
                d.WarExhaustion = Mathf.MoveTowards(d.WarExhaustion, 30f, d.WEChange);
            }
            else
            {
                // Base decay: -1.5 per second (faster recovery peace)
                float decay = 1.5f * delta;
                d.WEChange = -decay;
                
                // Move towards 0
                d.WarExhaustion = Mathf.MoveTowards(d.WarExhaustion, 0f, decay);
            }

            // 3. Update Effect Placeholders for Tooltips
            // Manpower Capacity reduced by WE% (e.g. 50 WE = -25% capacity)
            d.WarEffectOnManpowerPct = (d.WarExhaustion / 2f); 
            
            // Stability Drift caused by War (e.g. 100 WE = -10 stability/year)
            d.WarEffectOnStabilityPerYear = (d.WarExhaustion * 0.1f);
        }

        private static void UpdateStability(Kingdom k, Data d, float delta)
        {
            // 1. Calculate Target Stability
            // Base 50 + Modifiers (Laws, Leaders, etc.)
            float target = 50f + d.StabilityTargetModifier;
            
            // Apply Corruption Penalty directly to target equilibrium
            // e.g. 10% Corruption = -5 Stability target
            target -= (d.CorruptionLevel * 50f);

            // Apply War Exhaustion Penalty to target
            // e.g. 50 WE = -5 Stability target
            target -= (d.WarExhaustion * 0.1f);

            // Apply Unrest Reduction Modifier
            // Higher Unrest Reduction = Higher Stability (Less Revolt)
            if (d.UnrestReductionModifier != 1.0f)
            {
                target += (d.UnrestReductionModifier - 1.0f) * 20f;
            }

            // Apply Active Effects Modifiers
            if (d.ActiveEffects != null)
            {
                foreach (var eff in d.ActiveEffects)
                {
                    target += eff.StabilityModifier;
                }
            }

            // 2. Calculate Drift
            float oldStab = d.Stability;
            
            // Move towards target at speed of 2.0 per second
            // This makes stability changes feel "weighty" rather than instant
            d.Stability = Mathf.MoveTowards(d.Stability, target, 2.0f * delta);
            
            // 3. Set Change Rate for UI
            // We multiply by (1/delta) to get the rate per second, 
            // but for UI readability we might just want the raw difference per tick 
            // or formatted nicely. The UI expects a value to color Green/Red.
            d.StabilityChange = (d.Stability - oldStab) / delta; 
        }

        public static readonly Dictionary<Kingdom, Data> db = new Dictionary<Kingdom, Data>();
        public static Data Get(Kingdom k) {
            if (k == null) return null;
            if (!db.TryGetValue(k, out var d)) { d = new Data { KRef = k }; db[k] = d; }
            return d;
        }

        public class Data
        {
            public Kingdom KRef;
            public float LastUpdateWorldTime;
            public float TreasuryTimer;
            public List<TimedEffect> ActiveEffects = new List<TimedEffect>();
            public List<LeaderState> ActiveLeaders = new List<LeaderState>();
            public HashSet<string> ActivePolicies = new HashSet<string>();
            
            public long Treasury;
            public long Income;
            public long Expenses;
            public long TradeIncome;
            public long TradeExpenses;
            public long ExpensesCorruption;
            public long Balance => Income - Expenses;
            
            // Law States
            public string TaxationLevel = "Normal";
            public string MilitarySpending = "None";
            public string SecuritySpending = "None";
            public string GovernmentSpending = "None";
            public string WelfareSpending = "None";
            public string EducationSpending = "None";
            public string ResearchSpending = "None";
            public string AntiCorruption = "None";
            
            public string Law_Conscription = "Volunteer";
            public string Law_WarBonds = "Inactive";
            public string Law_ElitistMilitary = "Default";
            public string Law_PartyLoyalty = "Standard";
            public string Law_Centralization = "Balanced";
            public string Law_PressRegulation = "Laxed";
            public string Law_FirearmRegulation = "Standard";
            public string Law_Religion = "Secularism";
            public string Law_PopulationGrowth = "Balanced";
            public string Law_IndustrialSpec = "Balanced";
            public string Law_ResourceSubsidy = "None";
            public string Law_WorkingHours = "Standard";
            public string Law_ResearchFocus = "Balanced";
            public string Law_Monarch = "Ceremonial";
            public string Law_CollectiveTheory = "Marxist";
            public string Law_ElectiveAssembly = "Indirect";
            public string Law_DemocracyStyle = "Semi-Presidential";
            public string Law_StateDoctrine = "Classical";
            
            // Metrics
            public float TaxRateLocal = 0.10f;
            public double TaxBaseWealth;
            public double TaxBaseFallbackGDP;
            public long PerCapitaGDP = 6;
            public long IncomeBeforeModifiers;
            public float TaxPenaltyFromWar;
            public float MaxWeTaxPenaltyPct = 40f;
            public float TaxModifierFromStability;
            public float MaxStabTaxBonusPct = 10f;
            public float TaxModifierFromCities;
            public float CityWealthBonusPerCityPct = 3f;
            public float CityWealthBonusCapPct = 30f;
            public long IncomeAfterWarPenalty;
            public long IncomeAfterStability;
            public long IncomeAfterCityBonus;
            public int Soldiers;
            public long MilitaryCostPerSoldier = 3;
            public int Cities;
            public long CostPerCity = 5;
            public int Buildings;
            public long CostPerBuilding = 1;
            public long ExpensesMilitary;
            public long ExpensesInfrastructure;
            public long ExpensesDemography;
            public long ExpensesWarOverhead;

            // Economy Indicators
            public float EconBalanceIndex;
            public float EconTreasuryPerCapita;
            public float EconIncomePerCapita;
            public float EconExpensesPerCapita;
            public float EconWarOverheadShare;

            // Population
            public long Population;
            public long PrevPopulation;
            public long PopSamplePop = -1;
            public float PopSampleYears = 0f;
            public float AvgGrowthRate = 0f;
            public long Children; public long Adults; public int Babies; public int Teens; public int Elders; public int Veterans; public int Genius;
            public long Homeless; public long Hungry; public long Starving; public long Sick; public long HappyUnits; public long Unemployed;
            public float UnemploymentRate; public float HomelessRate; public float HungerRate; public float StarvationRate; public float SicknessRate; public float HappinessRate; public float ChildrenShare; public float ElderShare;

            // Army
            // public int Soldiers; public int Cities; public int Buildings; // Removed duplicates
            public long ManpowerCurrent; public long ManpowerMax; public long ManpowerMaxIncrease;
            public float ManpowerCurrentRatio; public float MobilizationRate; public float MilitaryBurdenRate;
            public bool AllowChildSoldiers; public bool AllowElderSoldiers; public bool AllowVeteranDraft;

            // State
            public float WarExhaustion; public float WEChange; public float WarEffectOnManpowerPct; public float WarEffectOnStabilityPerYear;
            public float Stability; public float StabilityChange; public bool HasInitializedStability;
            public float CorruptionLevel;

            // High Stakes Modifiers
            public float ManpowerMaxMultiplier = 1.0f;
            public float PopulationGrowthBonus = 0f;
            public float GeniusTraitChanceBoost = 0f;
            public float PlagueResistance = 0f;
            public float WarExhaustionGainMultiplier = 1.0f;
            public float StabilityTargetModifier = 0f;
            public int FlatManpowerPerCity = 0;
            
            public float ManpowerRegenRate = 0.015f; // Default base (fraction of max per minute)
            public float ManpowerAccumulator = 0f;    // Accumulates fractional updates
            // Missing Modifiers
            public float MilitaryAttackModifier;
            public long ExpensesLawUpkeep;
            public float BuildingSpeedModifier;
            public float FactoryOutputModifier;
            public float ResourceOutputModifier;
            public float TradeIncomeModifier;
            public float InvestmentCostModifier;
            public float ResearchOutputModifier;
            public float PlagueResistanceModifier;
            public float GeniusChanceModifier;
            public float MilitaryUpkeepModifier;
            public float JustificationTimeModifier;
            public float IntegrationSpeedModifier;
            public float PoliticalPowerGainModifier;
            public float IdeologyPowerModifier;
            public float UnrestReductionModifier;
            public float CityResistanceModifier;
            public float RebelSuppressionModifier;
            public float InvestmentAvailabilityModifier;
            public float LeaderXPModifier;
            public float PlagueRisk;
            public float PlagueRiskAccumulator; // Increases over time
            public float CorruptionFromEvents; // Stores corruption from events, decays over time
            public float PlagueResistanceDecay; // Increases over time (reducing total resistance)
            public float WarOverheadAccumulator; // NEW: Accumulates over time when WE > 5
            public float MaxWarOverheadPct;
            public System.Collections.Generic.Dictionary<string, int> ResourceStockpiles = new System.Collections.Generic.Dictionary<string, int>();
            public System.Collections.Generic.Dictionary<string, float> ResourceRates = new System.Collections.Generic.Dictionary<string, float>();
        }
        // ==============================================================================================
        // NATIVE SAVE/LOAD SYNC
        // ==============================================================================================
        public static void SyncToKingdom(Kingdom k)
        {
            if (k == null || k.data == null) return;
            var d = Get(k);
            if (d == null) return;

            // Serialize complex objects manually
            string policiesStr = string.Join(",", d.ActivePolicies);
            string leadersStr = Newtonsoft.Json.JsonConvert.SerializeObject(d.ActiveLeaders);
            string effectsStr = Newtonsoft.Json.JsonConvert.SerializeObject(d.ActiveEffects);

            // Set keys using native system
            // We use a prefix 'rb_' to avoid collisions
            k.data.set("rb_treasury_str", d.Treasury.ToString());
            k.data.set("rb_income", (int)d.Income); // int usually enough for display logic, but purely visual?
            // Actually we re-calc metrics every tick, so we only need to save STATE:
            // - Treasury
            // - Laws
            // - Policies
            // - Active Leaders/Effects (timers)
            // - WarExhaustion / Stability state
            
            k.data.set("rb_policies", policiesStr);
            k.data.set("rb_leaders", leadersStr);
            k.data.set("rb_effects", effectsStr);

            k.data.set("rb_tax_level", d.TaxationLevel);
            k.data.set("rb_mil_spending", d.MilitarySpending);
            // ... (Add all Law strings here) ...
            k.data.set("rb_law_conscription", d.Law_Conscription);
            k.data.set("rb_law_warbonds", d.Law_WarBonds);
            k.data.set("rb_law_elitist", d.Law_ElitistMilitary);
            k.data.set("rb_law_loyalty", d.Law_PartyLoyalty);
            k.data.set("rb_law_central", d.Law_Centralization);
            k.data.set("rb_law_press", d.Law_PressRegulation);
            k.data.set("rb_law_firearm", d.Law_FirearmRegulation);
            k.data.set("rb_law_religion", d.Law_Religion);
            k.data.set("rb_law_pop", d.Law_PopulationGrowth);
            k.data.set("rb_law_ind", d.Law_IndustrialSpec);
            k.data.set("rb_law_res", d.Law_ResourceSubsidy);
            k.data.set("rb_law_work", d.Law_WorkingHours);
            k.data.set("rb_law_research", d.Law_ResearchFocus);
            k.data.set("rb_law_monarch", d.Law_Monarch);
            k.data.set("rb_law_collective", d.Law_CollectiveTheory);
            k.data.set("rb_law_elective", d.Law_ElectiveAssembly);
            k.data.set("rb_law_democracy", d.Law_DemocracyStyle);
            k.data.set("rb_law_doctrine", d.Law_StateDoctrine);

            k.data.set("rb_war_exhaustion", d.WarExhaustion);
            k.data.set("rb_stability", d.Stability);
            k.data.set("rb_war_overhead_acc", d.WarOverheadAccumulator);
            k.data.set("rb_corruption", d.CorruptionLevel);
            k.data.set("rb_plague_risk", d.PlagueRiskAccumulator);
            k.data.set("rb_plague_res", d.PlagueResistance);
            k.data.set("rb_manpower", d.ManpowerCurrent.ToString());
        }

        public static void SyncFromKingdom(Kingdom k)
        {
            if (k == null || k.data == null) return;
            var d = Get(k); // Will create new if missing

            // Load Treasury
            string treasStr;
            k.data.get("rb_treasury_str", out treasStr);
            if (string.IsNullOrEmpty(treasStr)) treasStr = "0";
            long.TryParse(treasStr, out d.Treasury);

            // Load Laws (Strings)
            k.data.get("rb_tax_level", out d.TaxationLevel);
            if(d.TaxationLevel == null) d.TaxationLevel = "Normal";

            k.data.get("rb_mil_spending", out d.MilitarySpending);
            if(d.MilitarySpending == null) d.MilitarySpending = "None";
            
            k.data.get("rb_law_conscription", out d.Law_Conscription);
            if(d.Law_Conscription == null) d.Law_Conscription = "Volunteer";

            k.data.get("rb_law_warbonds", out d.Law_WarBonds);
            if(d.Law_WarBonds == null) d.Law_WarBonds = "Inactive";

            k.data.get("rb_law_elitist", out d.Law_ElitistMilitary);
            if(d.Law_ElitistMilitary == null) d.Law_ElitistMilitary = "Default";

            k.data.get("rb_law_loyalty", out d.Law_PartyLoyalty);
            if(d.Law_PartyLoyalty == null) d.Law_PartyLoyalty = "Standard";

            k.data.get("rb_law_central", out d.Law_Centralization);
            if(d.Law_Centralization == null) d.Law_Centralization = "Balanced";

            k.data.get("rb_law_press", out d.Law_PressRegulation);
            if(d.Law_PressRegulation == null) d.Law_PressRegulation = "Laxed";

            k.data.get("rb_law_firearm", out d.Law_FirearmRegulation);
            if(d.Law_FirearmRegulation == null) d.Law_FirearmRegulation = "Standard";

            k.data.get("rb_law_religion", out d.Law_Religion);
            if(d.Law_Religion == null) d.Law_Religion = "Secularism";
            
            k.data.get("rb_law_pop", out d.Law_PopulationGrowth);
            if(d.Law_PopulationGrowth == null) d.Law_PopulationGrowth = "Balanced";

            k.data.get("rb_law_ind", out d.Law_IndustrialSpec);
            if(d.Law_IndustrialSpec == null) d.Law_IndustrialSpec = "Balanced";

            k.data.get("rb_law_res", out d.Law_ResourceSubsidy);
            if(d.Law_ResourceSubsidy == null) d.Law_ResourceSubsidy = "None";

            k.data.get("rb_law_work", out d.Law_WorkingHours);
            if(d.Law_WorkingHours == null) d.Law_WorkingHours = "Standard";

            k.data.get("rb_law_research", out d.Law_ResearchFocus);
            if(d.Law_ResearchFocus == null) d.Law_ResearchFocus = "Balanced";

            k.data.get("rb_law_monarch", out d.Law_Monarch);
            if(d.Law_Monarch == null) d.Law_Monarch = "Ceremonial";

            k.data.get("rb_law_collective", out d.Law_CollectiveTheory);
            if(d.Law_CollectiveTheory == null) d.Law_CollectiveTheory = "Marxist";

            k.data.get("rb_law_elective", out d.Law_ElectiveAssembly);
            if(d.Law_ElectiveAssembly == null) d.Law_ElectiveAssembly = "Indirect";

            k.data.get("rb_law_democracy", out d.Law_DemocracyStyle);
            if(d.Law_DemocracyStyle == null) d.Law_DemocracyStyle = "Semi-Presidential";

            k.data.get("rb_law_doctrine", out d.Law_StateDoctrine);
            if(d.Law_StateDoctrine == null) d.Law_StateDoctrine = "Classical";

            // Load Metrics State
            k.data.get("rb_war_exhaustion", out d.WarExhaustion); 
            // Default 0f is fine

            k.data.get("rb_stability", out d.Stability);
            // Check if we got 0, likely default. But 0 stability is valid...
            // We use the Init flag elsewhere, so we can clamp or trust it.
            // If it's a NEW load (all 0), we want 50.
            // But we don't know if it's new simply by 0 value.
            // Let's assume if ALL vars are 0/null, it's new.
            // But 'd' is already existing potentially.
            // We'll trust the load. If 0, it's 0.
            d.HasInitializedStability = true;
            
            k.data.get("rb_war_overhead_acc", out d.WarOverheadAccumulator);

            k.data.get("rb_corruption", out d.CorruptionLevel);
            k.data.get("rb_plague_risk", out d.PlagueRiskAccumulator);
            k.data.get("rb_plague_res", out d.PlagueResistance);

            string mpStr;
            k.data.get("rb_manpower", out mpStr);
            if(string.IsNullOrEmpty(mpStr)) mpStr = "0";
            long.TryParse(mpStr, out d.ManpowerCurrent);

            // Load Complex
            string policiesStr;
            k.data.get("rb_policies", out policiesStr);
            d.ActivePolicies.Clear();
            if (!string.IsNullOrEmpty(policiesStr))
            {
                var arr = policiesStr.Split(',');
                foreach(var s in arr) if(!string.IsNullOrEmpty(s)) d.ActivePolicies.Add(s);
            }

            string leadersStr;
            k.data.get("rb_leaders", out leadersStr);
            if (!string.IsNullOrEmpty(leadersStr))
            {
                try { d.ActiveLeaders = Newtonsoft.Json.JsonConvert.DeserializeObject<List<LeaderState>>(leadersStr) ?? new List<LeaderState>(); }
                catch { d.ActiveLeaders = new List<LeaderState>(); }
            }

            string effectsStr;
            k.data.get("rb_effects", out effectsStr);
            if (!string.IsNullOrEmpty(effectsStr))
            {
                try { d.ActiveEffects = Newtonsoft.Json.JsonConvert.DeserializeObject<List<TimedEffect>>(effectsStr) ?? new List<TimedEffect>(); }
                catch { d.ActiveEffects = new List<TimedEffect>(); }
            }
        }
    }
}