using System;
using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;

namespace RulerBox
{
    // Represents a timed effect that modifies kingdom stability over a duration
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

    // Main system for calculating and updating kingdom metrics
    public static class KingdomMetricsSystem
    {
        private const float UpdateInterval = 0.25f;
        private static float accum;

        // List of common resources to track to avoid iterating everything
        public static readonly List<string> TrackedResources = new List<string>
        {
            "wood", "stone", "gold", "wheat", "bread", "meat", "fish", "berries", 
            "herbs", "CommonMetals", "mithril", "adamantine", "pie", "tea", "cider"
        };

        // Retrieve or create the Data object for a given kingdom
        public static void Tick(float deltaTime)
        {
            if (deltaTime <= 0f) return;

            accum += deltaTime;
            if (accum < UpdateInterval)
                return;
            accum = 0f;

            var k = Main.selectedKingdom;
            if (k == null)
                return;

            var d = Get(k);

            // === Apply timed effects ===
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

        // Get or create Data for a kingdom
        private static void RecalculateAllForKingdom(Kingdom k, Data d)
        {
            if (k == null || d == null) return;

            float now = Time.unscaledTime;
            float deltaWorldSeconds;

            if (d.LastUpdateWorldTime <= 0f)
            {
                deltaWorldSeconds = UpdateInterval;
            }
            else
            {
                deltaWorldSeconds = Mathf.Max(0.01f, now - d.LastUpdateWorldTime);
            }

            d.LastUpdateWorldTime = now;
            d.KRef = k;

            Recalculate(k, d, deltaWorldSeconds);
            ResourcesTradeWindow.Update();
        }

        // ==================== CORE RECALCULATE ======================
        private static void Recalculate(Kingdom k, Data d, float deltaWorldSeconds)
        {
            if (k == null || d == null) return;

            float secondsPerYear = GetSecondsPerYear();
            float yearsPassed = secondsPerYear > 0f ? deltaWorldSeconds / secondsPerYear : 0f;
            if (yearsPassed <= 0f) yearsPassed = 0.0001f;

            // --- 0) WorldBox local tax rate (traits / laws) ---
            float taxRateLocal = Mathf.Clamp01(k.getTaxRateLocal());
            d.TaxRateLocal = taxRateLocal;

            // === 0.1) Laws / conscription hook & RESET UPKEEP ===
            d.LawUpkeepPct = 0f;
            
            // Reset Modifiers for this tick
            d.ManpowerMaxMultiplier = 1.0f; 
            d.PopulationGrowthBonus = 0f;
            d.GeniusTraitChanceBoost = 0f;
            d.PlagueResistance = 0f;
            d.WarExhaustionGainMultiplier = 1.0f;
            d.StabilityTargetModifier = 0f;
            d.FlatManpowerPerCity = 0;
            d.CityWealthBonusCapPct = 30f; // Reset to base
            d.CorruptionLevel = 0f; // Reset base corruption
            d.ManpowerRegenRate = 0.015f; // Reset to new base (1.5% per min)
            d.MilitaryUpkeepModifier = 1.0f; // New
            d.BuildingSpeedModifier = 1.0f; // New
            d.ResearchOutputModifier = 1.0f; // New
            d.FactoryOutputModifier = 1.0f; // New (Economy)
            d.ResourceOutputModifier = 1.0f; // New (Economy)

            UpdateLawsFromKingdom(k, d);

            // --- 1) Iterate units once: wealth + demographics + "genius" ---
            double totalWealth = 0;
            int babies = 0;      // 0-4
            int teens = 0;      // 5-15
            int elders = 0;      // >=60
            int veterans = 0;
            int genius = 0;

            var units = k.getUnits();
            if (units != null)
            {
                foreach (var unit in units)
                {
                    if (unit == null || !unit.isAlive())
                        continue;

                    if (unit.money > 0)
                        totalWealth += unit.money;

                    int age = unit.age;
                    if (age < 5) babies++;
                    else if (age < 16) teens++;
                    else if (age >= 60) elders++;

                    if (unit.hasTrait("veteran")) veterans++;
                    if (unit.hasTrait("genius")) genius++;
                }
            }

            d.Babies = babies;
            d.Teens = teens;
            d.Elders = elders;
            d.Veterans = veterans;
            d.Genius = genius;

            long currentPop = Math.Max(0, k.getPopulationPeople());
            d.Population = currentPop;

            double fallbackGDP = 0;
            double baseTaxable = totalWealth;

            if (baseTaxable <= 0 && currentPop > 0)
            {
                fallbackGDP = currentPop * d.PerCapitaGDP;
                baseTaxable = fallbackGDP;
            }

            d.TaxBaseWealth = totalWealth;
            d.TaxBaseFallbackGDP = fallbackGDP;

            // ==================== SECTION: ECONOMY ====================

            // A) Income
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
            float econScale = ComputeEconomyScale(currentPop) * industryMod;
            
            // Add Trade Income
            long tradeIn = TradeManager.GetTradeIncome(k);
            d.TradeIncome = tradeIn;

            d.Income = SafeLong(taxableBase * econScale) + tradeIn;

            // B) Expenses
            long military = SafeLong(d.Soldiers * d.MilitaryCostPerSoldier * d.MilitaryUpkeepModifier);
            long infra = d.Cities * d.CostPerCity + d.Buildings * d.CostPerBuilding;

            var (childCost, elderCost, veteranCost) = GetDemographicCostPerHead(k);
            long babiesCost = SafeLong(babies * childCost);
            long eldersCost = SafeLong(elders * elderCost);
            long veteransCost = SafeLong(veterans * veteranCost);
            long demo = babiesCost + eldersCost + veteransCost;

            d.ExpensesMilitary = military;
            d.ExpensesInfrastructure = infra;
            d.ExpensesDemography = demo;

            float warOverheadPct = we01 * d.MaxWarOverheadPct;
            long baseExpenses = military + infra + demo;
            long warOverhead = SafeLong(baseExpenses * (warOverheadPct / 100.0));
            d.ExpensesWarOverhead = warOverhead;

            long lawCost = 0;
            if (d.Income > 0)
            {
                lawCost = SafeLong(d.Income * d.LawUpkeepPct);
            }
            else
            {
                lawCost = SafeLong(currentPop * 2.0f * d.LawUpkeepPct * 10f);
            }
            d.ExpensesLawUpkeep = lawCost;

            // Trade Expenses
            long tradeOut = TradeManager.GetTradeExpenses(k);
            d.TradeExpenses = tradeOut;

            // CORRUPTION COST
            // Corruption increases ALL expenses by its percentage level
            d.CorruptionLevel = Mathf.Clamp01(d.CorruptionLevel);
            long corruptionCost = SafeLong((baseExpenses + warOverhead + lawCost) * d.CorruptionLevel);
            d.ExpensesCorruption = corruptionCost;

            d.Expenses = Math.Max(0, SafeLong((baseExpenses + warOverhead) * econScale) 
                                     + lawCost + tradeOut + corruptionCost);

            // Update Treasury
            d.TreasuryTimer += deltaWorldSeconds;

            if (d.TreasuryTimer >= 5f)
            {
                // Income is usually projected yearly rate, so we apply a portion of it
                // Assuming metrics calculate "Yearly" rates, and 5s is roughly 1 month in fast speed
                // We'll just apply the yearly balance / 12 for now
                long yearlyBalance = d.Income - d.Expenses;
                d.Treasury += SafeLong(yearlyBalance / 12.0);
                d.TreasuryTimer = 0f;
            }

            float popf = Mathf.Max(1f, (float)currentPop);
            float incomeAbs = Mathf.Max(1f, Mathf.Abs((float)d.Income));
            float balanceNorm = Mathf.Clamp((float)d.Balance / incomeAbs, -1f, 1f);
            d.EconBalanceIndex = balanceNorm * 100f;

            d.EconTreasuryPerCapita = d.Treasury / popf;
            d.EconIncomePerCapita = d.Income / popf;
            d.EconExpensesPerCapita = d.Expenses / popf;

            if (d.Expenses > 0 && d.ExpensesWarOverhead > 0)
                d.EconWarOverheadShare = (float)d.ExpensesWarOverhead / d.Expenses * 100f;
            else
                d.EconWarOverheadShare = 0f;

            // ==================== SECTION: RESOURCES ====================
            UpdateResources(k, d);

            // ==================== SECTION: POPULATION ====================
            UpdatePopulation(k, d, currentPop, deltaWorldSeconds);

            // ==================== SECTION: MANPOWER =====================
            UpdateManpower(k, d, deltaWorldSeconds); 

            // ==================== SECTION: WAR / WE =====================
            UpdateWarExhaustion(k, d, yearsPassed);

            // ==================== SECTION: STABILITY ====================
            if (!d.HasInitializedStability)
            {
                d.Stability = 50f;
                d.HasInitializedStability = true;
            }

            UpdateStability(k, d, yearsPassed);

            // Derived indices for UI
            float we01After = Mathf.Clamp01(d.WarExhaustion / 100f);
            float mobil01 = Mathf.Clamp01(d.MobilizationRate * 2f);
            d.WarPressureIndex = Mathf.Clamp01(0.7f * we01After + 0.3f * mobil01) * 100f;

            float stabLack = Mathf.Clamp01((50f - d.Stability) / 50f);
            float unemp01 = Mathf.Clamp01(d.UnemploymentRate / 40f);
            float homeless01 = Mathf.Clamp01(d.HomelessRate / 20f);
            float hunger01 = Mathf.Clamp01(d.HungerRate / 20f);
            
            float tension01 = 0.25f * stabLack + 0.20f * unemp01 + 0.15f * homeless01 + 0.15f * hunger01 + 0.25f * we01After;
            d.InternalTensionIndex = Mathf.Clamp01(tension01) * 100f;

            float order = (d.Stability / 100f) * (1f - tension01);
            d.PublicOrderIndex = Mathf.Clamp01(order) * 100f;
        }

        private static void ApplyRiseOfNationsLaws(Data d)
        {
            [cite_start]// 1. Conscription [cite: 109-114]
            switch (d.Law_Conscription)
            {
                case "Disarmed":
                    d.ManpowerMaxMultiplier = 0.5f; // -50% (approx)
                    d.ManpowerRegenRate *= 0.5f;
                    d.TaxRateLocal *= 1.05f;
                    break;
                case "Limited":
                    d.ManpowerMaxMultiplier = 1.5f; // +50%
                    d.ManpowerRegenRate *= 1.5f;
                    d.TaxRateLocal *= 0.90f; // -10%
                    d.BuildingSpeedModifier *= 0.90f; // -10%
                    break;
                case "Extensive":
                    d.ManpowerMaxMultiplier = 2.0f; // +100%
                    d.ManpowerRegenRate *= 2.0f;
                    d.TaxRateLocal *= 0.75f; // -25%
                    d.BuildingSpeedModifier *= 0.75f; // -25%
                    break;
                case "Required":
                    d.ManpowerMaxMultiplier = 2.5f; // +150%
                    d.ManpowerRegenRate *= 2.5f;
                    d.TaxRateLocal *= 0.35f; // -65%
                    d.BuildingSpeedModifier *= 0.50f; // -50%
                    break;
            }

            [cite_start]// 2. War Bonds [cite: 114-115]
            switch (d.Law_WarBonds)
            {
                case "Moderate":
                    d.TaxRateLocal *= 1.5f;
                    d.MilitaryUpkeepModifier *= 0.75f;
                    d.StabilityTargetModifier -= 8f;
                    d.WarExhaustionGainMultiplier += 0.05f;
                    break;
                case "Maximum":
                    d.TaxRateLocal *= 2.25f;
                    d.MilitaryUpkeepModifier *= 0.5f;
                    d.StabilityTargetModifier -= 15f;
                    d.WarExhaustionGainMultiplier += 0.15f;
                    break;
            }

            [cite_start]// 3. Elitist Military Stance [cite: 115-116]
            if (d.Law_ElitistMilitary == "Expanded")
            {
                d.CorruptionLevel += 0.1f; // +0.1% is technically 0.001, but text implies significant impact
                // Military XP gain logic would go here
            }

            [cite_start]// 4. Party Loyalty [cite: 116-117]
            switch (d.Law_PartyLoyalty)
            {
                case "Minimum": d.TaxRateLocal *= 1.1f; break;
                case "Maximum": d.TaxRateLocal *= 0.9f; break;
                // Other effects are political (XP, PP gain)
            }

            [cite_start]// 5. Power Sharing (Iberian) [cite: 118]
            switch (d.Law_PowerSharing)
            {
                case "Decentralized": d.TaxRateLocal *= 1.05f; break; // +5% Tax
                case "Centralized": d.StabilityTargetModifier += 2.5f; break;
            }

            [cite_start]// 6. Industrial Specialization [cite: 118-119]
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

            [cite_start]// 7. Resource Subsidization [cite: 119]
            switch (d.Law_ResourceSubsidy)
            {
                case "Limited": d.TaxRateLocal *= 0.9f; d.FactoryOutputModifier *= 0.8f; break;
                case "Moderate": d.TaxRateLocal *= 0.85f; d.FactoryOutputModifier *= 0.7f; break;
                case "Generous": d.TaxRateLocal *= 0.75f; d.FactoryOutputModifier *= 0.6f; break;
            }

            [cite_start]// 8. Working Hours [cite: 120]
            switch (d.Law_WorkingHours)
            {
                case "Minimum": 
                    d.PopulationGrowthBonus += 0.01f; 
                    d.StabilityTargetModifier += 10f; 
                    d.TaxRateLocal *= 0.75f; 
                    break;
                case "Reduced": 
                    d.StabilityTargetModifier += 5f; 
                    d.TaxRateLocal *= 0.85f; 
                    break;
                case "Extended": 
                    d.StabilityTargetModifier -= 5f; 
                    d.TaxRateLocal *= 1.25f; 
                    d.BuildingSpeedModifier *= 1.25f;
                    break;
                case "Unlimited": 
                    d.PopulationGrowthBonus -= 0.015f; 
                    d.StabilityTargetModifier -= 15f; 
                    d.TaxRateLocal *= 1.5f; 
                    d.BuildingSpeedModifier *= 1.5f;
                    break;
            }

            [cite_start]// 9. Research Focus [cite: 121]
            switch (d.Law_ResearchFocus)
            {
                case "Civilian": 
                case "Military": 
                    d.ResearchOutputModifier *= 0.85f; 
                    break;
            }

            [cite_start]// 10. Press Regulation [cite: 122-123]
            switch (d.Law_PressRegulation)
            {
                case "Free Press": d.TaxRateLocal *= 1.1f; d.CorruptionLevel -= 0.1f; break;
                case "Laxed": d.TaxRateLocal *= 1.05f; d.CorruptionLevel -= 0.05f; break;
                case "Mixed": d.TaxRateLocal *= 0.95f; d.StabilityTargetModifier += 5f; break;
                case "State Focus": d.TaxRateLocal *= 0.9f; d.StabilityTargetModifier += 10f; break;
                case "Propaganda": d.TaxRateLocal *= 0.9f; d.StabilityTargetModifier += 10f; d.WarExhaustionGainMultiplier -= 0.03f; break;
            }

            [cite_start]// 11. Firearm Regulation [cite: 123-124]
            switch (d.Law_FirearmRegulation)
            {
                case "No Restr.": d.TaxRateLocal *= 1.13f; d.StabilityTargetModifier -= 5f; break;
                case "Reduced": d.TaxRateLocal *= 1.05f; d.StabilityTargetModifier -= 2.5f; break;
                case "Expanded": d.StabilityTargetModifier += 5f; break;
                case "Illegal": d.StabilityTargetModifier += 15f; break;
            }

            [cite_start]// 12. Religious Emphasis [cite: 124-125]
            switch (d.Law_Religion)
            {
                case "Atheism": 
                    d.TaxRateLocal *= 1.05f; 
                    d.ResearchOutputModifier *= 1.1f; 
                    break;
                case "State Rel.": 
                    d.TaxRateLocal *= 0.95f; 
                    d.ResearchOutputModifier *= 0.9f; 
                    d.StabilityTargetModifier += 5f; 
                    d.PopulationGrowthBonus += 0.0125f;
                    d.WarExhaustionGainMultiplier -= 0.05f; // -0.005 in text, boosted for effect
                    break;
            }

            [cite_start]// 13. Population Growth [cite: 125-126]
            switch (d.Law_PopulationGrowth)
            {
                case "Encouraged": d.PopulationGrowthBonus += 0.025f; d.TaxRateLocal *= 0.85f; break;
                case "Mandatory": d.PopulationGrowthBonus += 0.05f; d.TaxRateLocal *= 0.7f; break;
            }

            [cite_start]// 14. Monarch [cite: 126-127]
            switch (d.Law_Monarch)
            {
                case "Constitutional":
                    d.StabilityTargetModifier += 5f;
                    d.ManpowerMaxMultiplier *= 1.1f;
                    d.TaxRateLocal *= 1.15f;
                    break;
                case "Absolute":
                    d.StabilityTargetModifier += 10f;
                    d.ManpowerMaxMultiplier *= 1.2f;
                    d.TaxRateLocal *= 1.15f;
                    d.MilitaryUpkeepModifier *= 0.9f;
                    break;
            }

            [cite_start]// 15. Collective Theory [cite: 128]
            switch (d.Law_CollectiveTheory)
            {
                case "Maoism": d.ResourceOutputModifier *= 1.25f; break;
                case "Marxism": d.FactoryOutputModifier *= 1.15f; break;
                case "Stalinism": d.StabilityTargetModifier += 5f; d.CorruptionLevel += 0.02f; break;
                case "Trotskyism": d.TaxRateLocal *= 0.925f; d.WarExhaustionGainMultiplier -= 0.02f; break;
            }
            
            [cite_start]// 16. Elective Assembly [cite: 129]
            switch(d.Law_ElectiveAssembly)
            {
                case "Direct": d.TaxRateLocal *= 1.35f; break;
                case "Technocratic": d.ResearchOutputModifier *= 1.2f; break;
            }
            
            [cite_start]// 17. Democracy Style [cite: 130]
            switch(d.Law_DemocracyStyle)
            {
                case "Presidential": d.StabilityTargetModifier += 5f; break;
            }
            
            [cite_start]// 18. State Doctrine [cite: 131]
            switch(d.Law_StateDoctrine)
            {
                case "Corporatism": d.FactoryOutputModifier *= 1.25f; break;
                case "Stratocracy": d.ManpowerMaxMultiplier *= 1.2f; break;
                case "Clerical": d.PopulationGrowthBonus += 0.005f; d.WarExhaustionGainMultiplier -= 0.025f; break;
            }
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
                int diff = current - previous; // Rate per update interval
                d.ResourceRates[resId] = diff;
                d.ResourceStockpiles[resId] = current;
            }
        }

        private static void UpdatePopulation(Kingdom k, Data d, long currentPop, float delta)
        {
            d.Population = currentPop;
            d.PrevPopulation = d.PopSamplePop;

            if (d.PopSamplePop < 0 || (d.PopSamplePop <= 0 && currentPop > 0))
            {
                d.PopSamplePop = currentPop;
                d.PopSampleYears = 0f;
            }

            d.PopSampleYears += delta;

            if (d.PopSampleYears >= 20f)
            {
                long oldPop = d.PopSamplePop;
                long newPop = currentPop;

                if (oldPop > 0)
                {
                    long diff = newPop - oldPop;
                    d.AvgGrowthRate = ((float)diff / oldPop) * (100f / d.PopSampleYears);
                }
                d.PopSamplePop = currentPop;
                d.PopSampleYears = 0f;
            }

            d.AvgGrowthRate += (d.PopulationGrowthBonus * 100f);

            d.Children = Math.Max(0, k.countChildren());
            d.Adults = Math.Max(0, k.countAdults());
            d.Homeless = Math.Max(0, k.countHomeless());
            d.Hungry = Math.Max(0, k.countHungry());
            d.Starving = Math.Max(0, k.countStarving());
            d.Sick = Math.Max(0, k.countSick());
            d.HappyUnits = Math.Max(0, k.countHappyUnits());
            d.Unemployed = Math.Max(0, d.Adults - d.Soldiers);

            float popTotal = Mathf.Max(1f, (float)d.Population);
            float adultsF = Mathf.Max(1f, (float)d.Adults);

            d.UnemploymentRate = (d.Adults > 0) ? (float)d.Unemployed / adultsF * 100f : 0f;
            d.HomelessRate = (float)d.Homeless / popTotal * 100f;
            d.HungerRate = (float)d.Hungry / popTotal * 100f;
            d.StarvationRate = (float)d.Starving / popTotal * 100f;
            d.SicknessRate = (float)d.Sick / popTotal * 100f;
            d.HappinessRate = (float)d.HappyUnits / popTotal * 100f;
            d.ChildrenShare = (float)d.Children / popTotal * 100f;
            d.ElderShare = (float)d.Elders / popTotal * 100f;
        }

        private static void UpdateManpower(Kingdom k, Data d, float delta)
        {
            // 1. Calculate Cap for Manpower Points (Soft limit currency)
            long baseEligible = Math.Max(0, d.Adults - d.Soldiers);
            // Cap increases with population and military tech/laws
            long maxPoints = SafeLong(baseEligible * 0.5f * d.ManpowerMaxMultiplier) + (d.Cities * 10);
            
            d.ManpowerMax = maxPoints;
            
            // 2. Regenerate Manpower Points over time based on Law Modifier
            // d.ManpowerRegenRate is percentage of max per minute (e.g. 0.015 = 1.5%).
            // Accumulate fractional values to avoid rounding errors.
            if (d.ManpowerCurrent < d.ManpowerMax)
            {
                float regenAmount = (float)maxPoints * d.ManpowerRegenRate * (delta / 60f);
                d.ManpowerAccumulator += regenAmount;

                if (d.ManpowerAccumulator >= 1f)
                {
                    long add = (long)d.ManpowerAccumulator;
                    d.ManpowerAccumulator -= add;
                    d.ManpowerCurrent += add;
                }

                // Clamp
                if (d.ManpowerCurrent > d.ManpowerMax) 
                {
                    d.ManpowerCurrent = d.ManpowerMax;
                    d.ManpowerAccumulator = 0f;
                }
            }
            else
            {
                d.ManpowerAccumulator = 0f;
            }
        }

        private static void UpdateWarExhaustion(Kingdom k, Data d, float yearsPassed)
        {
            float oldWE = d.WarExhaustion;
            bool hasWar = false;
            int warCount = 0;
            float worstWarScore = 0f;
            float dailyGain = 0f;

            foreach (War war in k.getWars())
            {
                if (war == null || war.hasEnded()) continue;

                // Only count if we are a participant
                bool isAttacker = war.isAttacker(k);
                bool isDefender = war.isDefender(k);
                if (!isAttacker && !isDefender) continue;

                hasWar = true;
                warCount++;
                dailyGain += 2.0f; // Base gain for being at war

                float durationYears = Mathf.Max(0, war.getDuration());
                float sidePop = 0f, sideDeaths = 0f, sideArmy = 0f;

                if (isAttacker)
                {
                    sidePop = war.countAttackersPopulation();
                    sideDeaths = war.getDeadAttackers();
                    sideArmy = war.countAttackersWarriors();
                }
                else if (isDefender)
                {
                    sidePop = war.countDefendersPopulation();
                    sideDeaths = war.getDeadDefenders();
                    sideArmy = war.countDefendersWarriors();
                }

                // Heavy penalties for deaths
                float totalPop = d.Population > 0 ? d.Population : 1;
                float lossRatio = sideDeaths / totalPop;
                dailyGain += lossRatio * 500f; 

                float casualtyRate = (sidePop > 0f) ? Mathf.Clamp01(sideDeaths / sidePop) : 0f;
                float mobilizationRatio = (sidePop > 0f) ? Mathf.Clamp01(sideArmy / sidePop) : 0f;
                float durationFactor = Mathf.Clamp01(durationYears / 10f);

                float warScore = 10f + 40f * casualtyRate + 25f * mobilizationRatio + 25f * durationFactor;
                if (warScore > worstWarScore) worstWarScore = warScore;
            }

            float targetWE;
            if (!hasWar)
            {
                // Recovery
                targetWE = 0f;
                // Recover faster based on stability
                float recovery = 5f * (d.Stability / 50f); 
                d.WarExhaustion = Mathf.MoveTowards(oldWE, 0f, recovery * yearsPassed);
            }
            else
            {
                // Accumulate logic
                float multiWarFactor = Mathf.Clamp01((warCount - 1) / 3f);
                float gain = dailyGain * yearsPassed * d.WarExhaustionGainMultiplier;
                
                // If WE is low, it climbs towards the "Score". If high, it climbs purely by accumulation
                targetWE = worstWarScore + multiWarFactor * 20f;
                if (d.Stability < 50f) targetWE += Mathf.Clamp01((50f - d.Stability) / 50f) * 20f;
                
                // Blend step accumulation
                d.WarExhaustion = Mathf.Clamp(d.WarExhaustion + gain, 0f, 100f);
            }

            d.WEChange = d.WarExhaustion - oldWE;

            float we01Local = d.WarExhaustion / 100f;
            d.WarEffectOnManpowerPct = -we01Local * 30f;
            d.WarEffectOnStabilityPerYear = -we01Local * 20f;
        }

        private static void UpdateStability(Kingdom k, Data d, float yearsPassed)
        {
            float driftSpeedPerYear = 5f;
            float driftTarget = 50f + d.StabilityTargetModifier;
            driftTarget = Mathf.Clamp(driftTarget, 0f, 100f);

            float current = d.Stability;
            float change = 0f;

            // Natural drift towards target
            if (Mathf.Abs(driftTarget - current) > 0.01f)
            {
                float dir = Mathf.Sign(driftTarget - current);
                float driftStep = driftSpeedPerYear * yearsPassed * dir;
                if (Mathf.Abs(driftStep) > Mathf.Abs(driftTarget - current)) driftStep = driftTarget - current;
                change += driftStep;
            }

            change += d.WarEffectOnStabilityPerYear * yearsPassed;

            // Balance impact
            if (d.Balance < 0) change += -5f * yearsPassed;
            else if (d.Balance > 0) change += 2f * yearsPassed;

            // Population happiness impact
            if (d.Population > 0)
            {
                float happyRatio = (float)d.HappyUnits / d.Population;
                float miseryRatio = (float)(d.Hungry + d.Starving + d.Sick) / d.Population;
                change += (happyRatio - 0.5f) * 20f * yearsPassed;
                change += -miseryRatio * 40f * yearsPassed;
            }

            // Corruption penalty (Heavier now)
            d.CorruptionLevel = Mathf.Clamp01(d.CorruptionLevel);
            change += -(d.CorruptionLevel * 40f) * yearsPassed;

            d.Stability = Mathf.Clamp(d.Stability + change, 0f, 100f);
            d.StabilityChange = change;
        }

        // ==================== LAWS APPLICATION ======================
        private static void UpdateLawsFromKingdom(Kingdom k, Data d)
        {
            ApplyTaxation(d);
            ApplyMilitarySpending(d);
            ApplySecuritySpending(d);
            ApplyGovernmentSpending(d);
            ApplyWelfare(d);
            ApplyEducation(d);
            ApplyResearch(d);
            ApplyAntiCorruption(d);
        }

        // --- LAW EFFECTS ---
        private static void ApplyTaxation(Data d)
        {
            switch (d.TaxationLevel.ToLower())
            {
                case "minimum": 
                    d.TaxRateLocal *= 0.40f; 
                    d.StabilityTargetModifier += 20f; 
                    d.WarExhaustionGainMultiplier -= 0.10f; 
                    break;
                case "low":     
                    d.TaxRateLocal *= 0.70f; 
                    d.StabilityTargetModifier += 10f; 
                    d.WarExhaustionGainMultiplier -= 0.05f; 
                    break;
                case "normal":  
                    d.StabilityTargetModifier -= 5f; 
                    break; 
                case "high":    
                    d.TaxRateLocal *= 1.30f; 
                    d.StabilityTargetModifier -= 15f; 
                    d.WarExhaustionGainMultiplier += 0.03f; 
                    break;
                case "maximum": 
                    d.TaxRateLocal *= 1.75f; 
                    d.StabilityTargetModifier -= 40f; 
                    d.WarExhaustionGainMultiplier += 0.10f; 
                    break;
            }
        }

        // --- MILITARY SPENDING ---
        private static void ApplyMilitarySpending(Data d)
        {
            switch (d.MilitarySpending.ToLower())
            {
                case "none": 
                    d.ManpowerRegenRate = 0.015f; // Base 1.5% / min
                    break;
                case "low":
                    d.LawUpkeepPct += 0.1125f;
                    d.StabilityTargetModifier += 7.5f;
                    d.WarExhaustionGainMultiplier += 0.20f;
                    d.ManpowerMaxMultiplier *= 1.4f;
                    d.FlatManpowerPerCity += 5;
                    d.ManpowerRegenRate = 0.03f; // 3.0% / min
                    break;
                case "medium": 
                    d.LawUpkeepPct += 0.40f;
                    d.StabilityTargetModifier += 10f;
                    d.WarExhaustionGainMultiplier += 0.30f;
                    d.ManpowerMaxMultiplier *= 1.6f;
                    d.FlatManpowerPerCity += 10;
                    d.ManpowerRegenRate = 0.05f; // 5.0% / min
                    break;
                case "high":
                    d.LawUpkeepPct += 0.45f;
                    d.StabilityTargetModifier += 12.5f;
                    d.WarExhaustionGainMultiplier += 0.35f;
                    d.ManpowerMaxMultiplier *= 1.8f;
                    d.FlatManpowerPerCity += 20;
                    d.ManpowerRegenRate = 0.075f; // 7.5% / min
                    break;
                case "maximum":
                    d.LawUpkeepPct += 0.50f;
                    d.StabilityTargetModifier += 15f;
                    d.WarExhaustionGainMultiplier += 0.40f;
                    d.ManpowerMaxMultiplier *= 2.0f;
                    d.FlatManpowerPerCity += 40;
                    d.ManpowerRegenRate = 0.10f; // 10.0% / min
                    break;
            }
        }

        // --- SECURITY SPENDING ---
        private static void ApplySecuritySpending(Data d)
        {
            switch (d.SecuritySpending.ToLower())
            {
                case "none": 
                    break;
                case "low":
                    d.LawUpkeepPct += 0.1125f;
                    d.StabilityTargetModifier -= 15f;
                    d.WarExhaustionGainMultiplier += 0.015f;
                    d.CorruptionLevel += 0.04f;
                    d.FlatManpowerPerCity += 10;
                    break;
                case "medium":
                    d.LawUpkeepPct += 0.125f;
                    d.StabilityTargetModifier -= 10f;
                    d.WarExhaustionGainMultiplier += 0.01f;
                    d.CorruptionLevel += 0.06f;
                    d.FlatManpowerPerCity += 15;
                    break;
                case "high":
                    d.LawUpkeepPct += 0.1375f;
                    d.StabilityTargetModifier -= 5f;
                    d.WarExhaustionGainMultiplier += 0.005f;
                    d.CorruptionLevel += 0.08f;
                    d.FlatManpowerPerCity += 20;
                    break;
                case "maximum":
                    d.LawUpkeepPct += 0.15f;
                    d.StabilityTargetModifier += 0f;
                    d.WarExhaustionGainMultiplier += 0.10f;
                    d.CorruptionLevel += 0.10f;
                    d.FlatManpowerPerCity += 25;
                    break;
            }
        }

        // --- GOVERNMENT SPENDING ---
        private static void ApplyGovernmentSpending(Data d)
        {
            switch (d.GovernmentSpending.ToLower())
            {
                case "none": 
                    break;
                case "low":
                    d.LawUpkeepPct += 0.05f;
                    d.StabilityTargetModifier -= 4f;
                    d.CityWealthBonusCapPct = 25f; 
                    break;
                case "medium":
                    d.LawUpkeepPct += 0.10f;
                    d.StabilityTargetModifier += 0f;
                    d.CorruptionLevel -= 0.10f;
                    d.CityWealthBonusCapPct = 30f; 
                    break;
                case "high":
                    d.LawUpkeepPct += 0.15f;
                    d.StabilityTargetModifier += 10f;
                    d.CorruptionLevel -= 0.20f;
                    d.CityWealthBonusCapPct = 40f; 
                    break;
                case "maximum":
                    d.LawUpkeepPct += 0.20f;
                    d.StabilityTargetModifier += 20f;
                    d.CorruptionLevel -= 0.30f;
                    d.CityWealthBonusCapPct = 50f;
                    break;
            }
        }

        // --- WELFARE SPENDING ---
        private static void ApplyWelfare(Data d)
        {
            switch (d.WelfareSpending.ToLower())
            {
                case "none": 
                    break;
                case "low":
                    d.LawUpkeepPct += 0.05f;
                    d.StabilityTargetModifier -= 5f;
                    d.PlagueResistance = 0.20f;
                    break;
                case "medium":
                    d.LawUpkeepPct += 0.10f;
                    d.StabilityTargetModifier += 0f;
                    d.WarExhaustionGainMultiplier -= 0.03f;
                    d.PlagueResistance = 0.40f;
                    break;
                case "high":
                    d.LawUpkeepPct += 0.15f;
                    d.StabilityTargetModifier += 5f;
                    d.WarExhaustionGainMultiplier -= 0.06f;
                    d.PlagueResistance = 0.60f;
                    break;
                case "maximum":
                    d.LawUpkeepPct += 0.20f;
                    d.StabilityTargetModifier += 10f;
                    d.WarExhaustionGainMultiplier -= 0.10f;
                    d.PlagueResistance = 0.80f;
                    break;
            }
        }

        // --- EDUCATION SPENDING ---
        private static void ApplyEducation(Data d)
        {
            switch (d.EducationSpending.ToLower())
            {
                case "none":
                    break;
                case "low":
                    d.LawUpkeepPct += 0.05f;
                    d.GeniusTraitChanceBoost = 0.02f;
                    break;
                case "medium":
                    d.LawUpkeepPct += 0.10f;
                    d.StabilityTargetModifier += 10f;
                    d.WarExhaustionGainMultiplier -= 0.04f;
                    d.GeniusTraitChanceBoost = 0.05f;
                    break;
                case "high":
                    d.LawUpkeepPct += 0.15f;
                    d.StabilityTargetModifier += 20f;
                    d.WarExhaustionGainMultiplier -= 0.07f;
                    d.GeniusTraitChanceBoost = 0.08f;
                    break;
                case "maximum":
                    d.LawUpkeepPct += 0.20f;
                    d.StabilityTargetModifier += 30f;
                    d.WarExhaustionGainMultiplier -= 0.10f;
                    d.GeniusTraitChanceBoost = 0.10f;
                    break;
            }
        }

        // --- RESEARCH SPENDING ---
        private static void ApplyResearch(Data d)
        {
            switch (d.ResearchSpending.ToLower())
            {
                case "none": 
                    break;
                case "low":
                    d.LawUpkeepPct += 0.15f;
                    d.StabilityTargetModifier -= 5f;
                    d.TechSpeedMultiplier = 2.5f;
                    break;
                case "medium":
                    d.LawUpkeepPct += 0.20f;
                    d.StabilityTargetModifier += 0f;
                    d.WarExhaustionGainMultiplier -= 0.04f;
                    d.TechSpeedMultiplier = 4.0f;
                    break;
                case "high":
                    d.LawUpkeepPct += 0.25f;
                    d.StabilityTargetModifier += 5f;
                    d.WarExhaustionGainMultiplier -= 0.08f;
                    d.TechSpeedMultiplier = 5.0f;
                    break;
                case "maximum":
                    d.LawUpkeepPct += 0.30f;
                    d.StabilityTargetModifier += 10f;
                    d.WarExhaustionGainMultiplier -= 0.12f;
                    d.TechSpeedMultiplier = 6.0f;
                    break;
            }
        }

        // --- ANTI-CORRUPTION SPENDING ---
        private static void ApplyAntiCorruption(Data d)
        {
            switch (d.AntiCorruption.ToLower())
            {
                case "none":
                    d.CorruptionLevel += 0.0f; // Base
                    break;
                case "low":
                    d.LawUpkeepPct += 0.15f;
                    d.StabilityTargetModifier += 12f;
                    d.CorruptionLevel -= 0.20f;
                    break;
                case "medium":
                    d.LawUpkeepPct += 0.20f;
                    d.StabilityTargetModifier += 18f;
                    d.CorruptionLevel -= 0.30f;
                    break;
                case "high":
                    d.LawUpkeepPct += 0.25f;
                    d.StabilityTargetModifier += 24f;
                    d.CorruptionLevel -= 0.40f;
                    break;
                case "maximum":
                    d.LawUpkeepPct += 0.30f;
                    d.StabilityTargetModifier += 30f;
                    d.CorruptionLevel -= 0.50f;
                    break;
            }
        }

        // ==================== UTILITIES ============================
        private static long SafeLong(double v)
        {
            if (v > long.MaxValue) return long.MaxValue;
            if (v < long.MinValue) return long.MinValue;
            return (long)Math.Round(v);
        }

        // Economy scaling based on population size (logarithmic)
        private static float ComputeEconomyScale(long population)
        {
            if (population <= 0) return 0f;
            float t = Mathf.Clamp01(Mathf.Log10(population) / 4f);
            return Mathf.Lerp(0.2f, 1f, t);
        }

        // Get the number of real-world seconds that correspond to one in-game year
        private static float GetSecondsPerYear()
        {
            var ms = World.world?.map_stats;
            if (ms == null || ms.world_ages_speed_multiplier <= 0f)
                return 60f;
            return ms.current_world_ages_duration / ms.world_ages_speed_multiplier;
        }

        // Demographic cost per head by age group
        private static (float, float, float) GetDemographicCostPerHead(Kingdom k)
        {
            return (0.1f, 0.3f, 0.5f);
        }

        // ==================== DATA STORAGE ===========================
        public static readonly Dictionary<Kingdom, Data> db = new();

        // Retrieve or create the metrics data for a kingdom
        public static Data Get(Kingdom k)
        {
            if (k == null) return null;
            if (!db.TryGetValue(k, out var d))
            {
                d = new Data { KRef = k };
                db[k] = d;
            }
            return d;
        }

        // Kingdom Metrics Data Structure
        public class Data
        {
            public Kingdom KRef;
            public float LastUpdateWorldTime;
            public float TreasuryTimer = 0f;
            public List<TimedEffect> ActiveEffects = new List<TimedEffect>();

            public long Treasury;
            public long Income;
            public long Expenses;
            public long TradeIncome;
            public long TradeExpenses;
            public long ExpensesCorruption;
            
            public long Balance => Income - Expenses;

            // Resources
            public Dictionary<string, int> ResourceStockpiles = new Dictionary<string, int>();
            public Dictionary<string, int> ResourceRates = new Dictionary<string, int>();

            public float TaxRateLocal = 0.05f;
            public float TaxPenaltyFromWar;
            public float TaxModifierFromStability;
            public float TaxModifierFromCities;

            // Laws
            public string TaxationLevel = "normal";
            public string MilitarySpending = "none";
            public string SecuritySpending = "none";
            public string GovernmentSpending = "none";
            public string WelfareSpending = "none";
            public string EducationSpending = "none";
            public string ResearchSpending = "none";
            public string AntiCorruption = "none";

            public float LawUpkeepPct = 0f;
            public long ExpensesLawUpkeep;

            // Tuning
            public long PerCapitaGDP = 15;
            public float MaxWeTaxPenaltyPct = 40f;
            public float MaxStabTaxBonusPct = 10f;
            public float CityWealthBonusPerCityPct = 3f;
            public float CityWealthBonusCapPct = 30f;
            public long MilitaryCostPerSoldier = 10;
            public long CostPerCity = 5;
            public long CostPerBuilding = 2;
            public float MaxWarOverheadPct = 25f;

            public double TaxBaseWealth;
            public double TaxBaseFallbackGDP;
            public long IncomeBeforeModifiers;
            public long IncomeAfterWarPenalty;
            public long IncomeAfterStability;
            public long IncomeAfterCityBonus;

            public long PopularityIndex;
            public long TechProgressSpeed;

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
            public int Soldiers; public int Cities; public int Buildings;
            public long ManpowerCurrent; public long ManpowerMax; public long ManpowerMaxIncrease;
            public float ManpowerCurrentRatio; public float MobilizationRate; public float MilitaryBurdenRate;
            public bool AllowChildSoldiers; public bool AllowElderSoldiers; public bool AllowVeteranDraft;

            // State
            public float WarExhaustion; public float WEChange; public float WarEffectOnManpowerPct; public float WarEffectOnStabilityPerYear;
            public float Stability; public float StabilityChange; public bool HasInitializedStability;
            public float CorruptionLevel;
            public float WarPressureIndex; public float InternalTensionIndex; public float PublicOrderIndex;

            // High Stakes Modifiers
            public float ManpowerMaxMultiplier = 1.0f;
            public float PopulationGrowthBonus = 0f;
            public float GeniusTraitChanceBoost = 0f;
            public float PlagueResistance = 0f;
            public float WarExhaustionGainMultiplier = 1.0f;
            public float StabilityTargetModifier = 0f;
            public int FlatManpowerPerCity = 0;
            public float TechSpeedMultiplier = 1.0f;
            
            public float ManpowerRegenRate = 0.015f; // Default base (fraction of max per minute)
            public float ManpowerAccumulator = 0f;    // Accumulates fractional updates

            // --- NEW/UPDATED LAW FIELDS ---
            public string Law_Conscription = "Volunteer";
            public string Law_WarBonds = "Inactive";
            public string Law_ElitistMilitary = "Default";
            public string Law_PartyLoyalty = "Standard";
            public string Law_PowerSharing = "Balanced"; // Renamed from Centralization
            public string Law_PressRegulation = "Mixed";
            public string Law_FirearmRegulation = "Standard";
            public string Law_Religion = "Secularism";
            public string Law_PopulationGrowth = "Balanced";
            public string Law_IndustrialSpec = "Balanced";
            public string Law_ResourceSubsidy = "None";
            public string Law_WorkingHours = "Standard";
            public string Law_ResearchFocus = "Balanced";
            
            // Ideology Laws
            public string Law_Monarch = "None";
            public string Law_CollectiveTheory = "Marxism";
            public string Law_ElectiveAssembly = "Indirect";
            public string Law_DemocracyStyle = "Semi-Presidential";
            public string Law_StateDoctrine = "Classical";

            // --- NEW METRIC MODIFIERS ---
            public float MilitaryUpkeepModifier = 1.0f;
            public float BuildingSpeedModifier = 1.0f;
            public float FactoryOutputModifier = 1.0f;
            public float ResourceOutputModifier = 1.0f;
            public float ResearchOutputModifier = 1.0f;
        }
    }

    // ================= MANPOWER GAMEPLAY PATCH =================
    // This patch injects a bonus to the city's max_warriors stat based on the Military Spending law.
    [HarmonyPatch(typeof(City), "getMaxWarriors")]
    public static class Patch_City_GetMaxWarriors
    {
        public static void Postfix(City __instance, ref int __result)
        {
            if (__instance == null || __instance.kingdom == null) return;

            // Get Mod Data
            var d = KingdomMetricsSystem.Get(__instance.kingdom);
            if (d == null) return;

            // If we have a multiplier > 1 or flat bonus, apply it
            if (d.ManpowerMaxMultiplier > 1.0f || d.FlatManpowerPerCity > 0)
            {
                // Apply multipliers to the result calculated by the game
                float val = (float)__result;
                val = (val * d.ManpowerMaxMultiplier) + d.FlatManpowerPerCity;
                __result = (int)val;
            }
        }
    }
}