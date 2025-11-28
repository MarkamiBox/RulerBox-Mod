using System;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;

namespace RulerBox
{
    public class TimedEffect
    {
        public float TimeRemaining;
        public float StabilityPerSecond;

        public TimedEffect(float duration, float stabilityPerSecond)
        {
            TimeRemaining = duration;
            StabilityPerSecond = stabilityPerSecond;
        }
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

        public static void Tick(float deltaTime)
        {
            if (deltaTime <= 0f) return;

            accum += deltaTime;
            if (accum < UpdateInterval)
                return;
            accum = 0f;

            var k = Main.selectedKingdom;
            if (k == null) return;

            var d = Get(k);

            for (int i = d.ActiveEffects.Count - 1; i >= 0; i--)
            {
                var eff = d.ActiveEffects[i];
                d.Stability += eff.StabilityPerSecond * deltaTime;
                eff.TimeRemaining -= deltaTime;
                if (eff.TimeRemaining <= 0f)
                    d.ActiveEffects.RemoveAt(i);
            }

            RecalculateAllForKingdom(k, d);
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

        private static void Recalculate(Kingdom k, Data d, float deltaWorldSeconds)
        {
            if (k == null || d == null) return;

            float secondsPerYear = GetSecondsPerYear();
            float yearsPassed = secondsPerYear > 0f ? deltaWorldSeconds / secondsPerYear : 0f;
            if (yearsPassed <= 0f) yearsPassed = 0.0001f;

            // 0. Base Tax & Reset Modifiers
            d.TaxRateLocal = Mathf.Clamp01(k.getTaxRateLocal());
            
            // Reset Modifiers
            d.StabilityTargetModifier = 0f;
            d.WarExhaustionGainMultiplier = 1.0f;
            d.ManpowerMaxMultiplier = 1.0f;
            d.ManpowerRegenRate = 0.015f; // Base 1.5%
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

            // 1. Apply Laws Modifiers
            ApplyRiseOfNationsLaws(d);
            ApplyEconomicLaws_Modifiers(d);

            // 2. Economy Calc (Income)
            double totalWealth = 0;
            var units = k.getUnits();
            if (units != null) {
                foreach (var unit in units) {
                    if (unit != null && unit.isAlive()) totalWealth += unit.money;
                }
            }
            d.TaxBaseWealth = totalWealth;
            
            double baseTaxable = totalWealth;
            if (baseTaxable <= 0) baseTaxable = k.getPopulationPeople() * d.PerCapitaGDP;
            d.TaxBaseFallbackGDP = baseTaxable;

            double taxableBase = baseTaxable * d.TaxRateLocal;
            d.IncomeBeforeModifiers = SafeLong(taxableBase);

            // Apply modifiers to Income
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
            
            // Apply Factory/Resource Output to Income (Simplified Economy Scale)
            float industryMod = (d.FactoryOutputModifier + d.ResourceOutputModifier) / 2f;
            float econScale = ComputeEconomyScale(k.getPopulationPeople()) * industryMod;
            
            long tradeIn = SafeLong(TradeManager.GetTradeIncome(k) * d.TradeIncomeModifier);
            d.TradeIncome = tradeIn;
            
            d.Income = SafeLong(taxableBase * econScale) + tradeIn;

            // 3. Expenses Calc
            long military = SafeLong(d.Soldiers * d.MilitaryCostPerSoldier * d.MilitaryUpkeepModifier);
            long infra = d.Cities * d.CostPerCity + d.Buildings * d.CostPerBuilding;
            long demo = 0; 
            d.ExpensesMilitary = military;
            d.ExpensesInfrastructure = infra;
            d.ExpensesDemography = demo;
            
            long baseExpenses = military + infra + demo;
            float warOverheadPct = we01 * d.MaxWarOverheadPct;
            long warOverhead = SafeLong(baseExpenses * (warOverheadPct / 100.0));
            d.ExpensesWarOverhead = warOverhead;

            // Calculate Economic Laws Spending
            long econSpending = CalculateEconomicSpending(d);
            d.ExpensesLawUpkeep = econSpending;
            
            long tradeOut = TradeManager.GetTradeExpenses(k);
            d.TradeExpenses = tradeOut;

            d.CorruptionLevel = Mathf.Clamp01(d.CorruptionLevel);
            long corruptionCost = SafeLong((baseExpenses + warOverhead) * d.CorruptionLevel);
            d.ExpensesCorruption = corruptionCost;

            d.Expenses = Math.Max(0, SafeLong((baseExpenses + warOverhead) * econScale) + tradeOut + corruptionCost + econSpending);

            // Treasury Update
            d.TreasuryTimer += deltaWorldSeconds;
            if (d.TreasuryTimer >= 5f)
            {
                long yearlyBalance = d.Income - d.Expenses;
                d.Treasury += SafeLong(yearlyBalance / 12.0);
                d.TreasuryTimer = 0f;
            }
            
            // --- Resources, Population, Manpower, War Exhaustion, Stability ---
            UpdateResources(k, d);
            UpdatePopulation(k, d, k.getPopulationPeople(), deltaWorldSeconds);
            UpdateManpower(k, d, deltaWorldSeconds);
            UpdateWarExhaustion(k, d, yearsPassed);
            if (!d.HasInitializedStability) { d.Stability = 50f; d.HasInitializedStability = true; }
            UpdateStability(k, d, yearsPassed);
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
                    d.StabilityTargetModifier -= 15f;
                    d.WarExhaustionGainMultiplier += 0.03f;
                    break;
                case "Maximum": 
                    d.TaxRateLocal *= 1.75f; 
                    d.StabilityTargetModifier -= 40f;
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

            total += GetCost(d.MilitarySpending, inc, 0.35f, 0.40f, 0.45f, 0.50f);
            total += GetCost(d.SecuritySpending, inc, 0.1125f, 0.125f, 0.1375f, 0.15f);
            total += GetCost(d.GovernmentSpending, inc, 0.05f, 0.10f, 0.15f, 0.20f);
            total += GetCost(d.WelfareSpending, inc, 0.05f, 0.10f, 0.15f, 0.20f);
            total += GetCost(d.EducationSpending, inc, 0.05f, 0.10f, 0.15f, 0.20f);
            total += GetCost(d.ResearchSpending, inc, 0.15f, 0.20f, 0.25f, 0.30f);
            total += GetCost(d.AntiCorruption, inc, 0.15f, 0.20f, 0.25f, 0.30f);
            
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
            if (d.ResourceRates == null) d.ResourceRates = new Dictionary<string, int>();
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
        
        private static void UpdateWarExhaustion(Kingdom k, Data d, float years)
        {
             // Basic simulation of War Exhaustion logic
        }

        private static void UpdateStability(Kingdom k, Data d, float years)
        {
             float target = 50f + d.StabilityTargetModifier;
             d.Stability = Mathf.MoveTowards(d.Stability, target, 5f * years);
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
            
            public long Treasury;
            public long Income;
            public long Expenses;
            public long TradeIncome;
            public long TradeExpenses;
            public long ExpensesCorruption;
            public long Balance => Income - Expenses;
            
            // Economic Laws
            public string TaxationLevel = "Normal";
            public string MilitarySpending = "None";
            public string SecuritySpending = "None";
            public string GovernmentSpending = "None";
            public string WelfareSpending = "None";
            public string EducationSpending = "None";
            public string ResearchSpending = "None";
            public string AntiCorruption = "None";

            // Rise of Nations Laws
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
            public float TaxRateLocal = 0.05f;
            public double TaxBaseWealth;
            public double TaxBaseFallbackGDP;
            public long PerCapitaGDP = 15;
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
            public long MilitaryCostPerSoldier = 10;
            public int Cities;
            public long CostPerCity = 5;
            public int Buildings;
            public long CostPerBuilding = 2;
            public long ExpensesMilitary;
            public long ExpensesInfrastructure;
            public long ExpensesDemography;
            public long ExpensesWarOverhead;
            public float MaxWarOverheadPct = 25f;
            public long ExpensesLawUpkeep;
            public float CorruptionLevel;
            public int Adults;
            public long ManpowerCurrent;
            public long ManpowerMax;
            public float ManpowerAccumulator;
            public float WarExhaustion;
            public float Stability;
            public bool HasInitializedStability;
            
            // Expanded Modifiers List
            public float StabilityTargetModifier;
            public float WarExhaustionGainMultiplier = 1.0f;
            public float ManpowerMaxMultiplier = 1.0f;
            public float ManpowerRegenRate = 0.015f;
            public float PopulationGrowthBonus;
            public float MilitaryUpkeepModifier = 1.0f;
            public float BuildingSpeedModifier = 1.0f;
            public float FactoryOutputModifier = 1.0f;
            public float ResourceOutputModifier = 1.0f;
            public float ResearchOutputModifier = 1.0f;
            public float TradeIncomeModifier = 1.0f;
            public float IntegrationSpeedModifier = 1.0f;
            public float PoliticalPowerGainModifier = 1.0f;
            public float IdeologyPowerModifier = 1.0f;
            public float UnrestReductionModifier = 1.0f;
            public float CityResistanceModifier = 1.0f;
            public float RebelSuppressionModifier = 1.0f;
            public float InvestmentCostModifier = 1.0f;
            public float InvestmentAvailabilityModifier = 1.0f;
            public float LeaderXPModifier = 0f;
            public float JustificationTimeModifier = 1.0f;
            public float GeniusChanceModifier = 0f;
            public float PlagueResistanceModifier = 0f;

            // Misc
            public Dictionary<string, int> ResourceStockpiles = new Dictionary<string, int>();
            public Dictionary<string, int> ResourceRates = new Dictionary<string, int>();
            public float AvgGrowthRate;
            public long Population;
            
            // Legacy placeholders
            public int Babies, Teens, Elders, Veterans, Genius;
            public long Children, Homeless, Hungry, Starving, Sick, HappyUnits, Unemployed;
            public float UnemploymentRate, HomelessRate, HungerRate, StarvationRate, SicknessRate, HappinessRate, ChildrenShare, ElderShare;
            public float EconBalanceIndex, EconTreasuryPerCapita, EconIncomePerCapita, EconExpensesPerCapita, EconWarOverheadShare;
            public long PrevPopulation, PopSamplePop = -1;
            public float PopSampleYears, WEChange, WarEffectOnManpowerPct, WarEffectOnStabilityPerYear, StabilityChange, WarPressureIndex, InternalTensionIndex, PublicOrderIndex;
        }
    }

    // Gameplay Patches to hook modifiers
    [HarmonyPatch(typeof(City), "getMaxWarriors")]
    public static class Patch_City_GetMaxWarriors
    {
        public static void Postfix(City __instance, ref int __result)
        {
            if (__instance == null || __instance.kingdom == null) return;
            var d = KingdomMetricsSystem.Get(__instance.kingdom);
            if (d != null && d.ManpowerMaxMultiplier != 1.0f)
            {
                __result = (int)(__result * d.ManpowerMaxMultiplier);
            }
        }
    }
    
    [HarmonyPatch(typeof(Building), "getConstructionProgress")]
    public static class Patch_Building_Construction
    {
        // Note: Ideally this would patch updateBuild, but for visualization we might adjust progress speed logic elsewhere. 
    }
}