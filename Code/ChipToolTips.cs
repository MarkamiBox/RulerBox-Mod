using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace RulerBox
{
    // Generic, reusable tooltip manager for Hub chips
    public static class ChipTooltips
    {
        private static GameObject tooltip;
        private static Text tooltipText;
        private static System.Func<string> currentProvider;

        // ==============================================================================================
        // MANPOWER TOOLTIP
        // ==============================================================================================
        public static void AttachManpowerTooltip(GameObject chip)
        {
            EnsureTooltip();
            var et = AddOrGetEventTrigger(chip);
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener((_) =>
            {
                var k = Main.selectedKingdom;
                currentProvider = () => ManpowerTooltip(k);
                Show();
            });
            et.triggers.Add(enter);
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener((_) => Hide());
            et.triggers.Add(exit);
        }

        // Manpower tooltip text
        public static string ManpowerTooltip(Kingdom k)
        {
            var d = KingdomMetricsSystem.Get(k);
            if (d == null) return "No data";
            
            string modeStr = "Normal";
            if (ArmySystem.CurrentMode == ArmySelectionMode.Recruit) 
                modeStr = "<color=#7CFC00>RECRUIT (Drag civilians)</color>";
            else if (ArmySystem.CurrentMode == ArmySelectionMode.Dismiss) 
                modeStr = "<color=#FF5A5A>DISMISS (Drag soldiers)</color>";
            
            string regenStr = ColorGold($"{d.ManpowerRegenRate * 100f:0.##}% / min");

            return
                $"<b>Manpower Command</b>\n" +
                $"Current Mode: {modeStr}\n" +
                $"[B] Cycle Modes (Recruit/Dismiss/Normal)\n\n" +
                //$"Active Soldiers: {ColorGold(FormatBig(d.Soldiers))}\n" +
                $"Eligible Civilians: {ColorGold(FormatBig(d.Adults - d.Soldiers))}\n\n" +
                $"<b>Manpower Points (Draft Currency)</b>\n" +
                $"Available: {ColorGold(FormatBig(d.ManpowerCurrent))}\n" +
                $"Capacity:  {FormatBig(d.ManpowerMax)}\n" +
                $"Regeneration: {regenStr}\n\n" +
                $"<color=#999999>Drafting costs 1 Manpower Point.\n" +
                $"Dismissing refunds Manpower Point.</color>";
        }

        // ==============================================================================================
        // ECONOMY TOOLTIP
        // ==============================================================================================
        
        //  Attach economy tooltip to a chip
        public static void AttachEconomyTooltip(GameObject chip)
        {
            EnsureTooltip();
            var et = AddOrGetEventTrigger(chip);
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener((_) =>
            {
                var k = Main.selectedKingdom;
                currentProvider = () => EconomyTooltip(k);
                Show();
            });
            et.triggers.Add(enter);
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener((_) => Hide());
            et.triggers.Add(exit);
        }

        // Economy tooltip text
        public static string EconomyTooltip(Kingdom k)
        {
            var d = KingdomMetricsSystem.Get(k);
            if (d == null) return "No data";
            
            bool extended = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            var treasury = Money(d.Treasury, whenPositive: "gold", zeroGold: true);
            var income   = Money(d.Income);
            var expenses = Money(-Math.Abs(d.Expenses));
            var balance  = Money(d.Balance, whenPositive: "gold", zeroGold: true);
            
            if (!extended)
            {
                // Compact view
                string tradeStr = "";
                if (d.TradeIncome > 0 || d.TradeExpenses > 0)
                {
                    tradeStr = $"\nTrade: {ColorGreen("+" + FormatBig(d.TradeIncome))} / {ColorRed("-" + FormatBig(d.TradeExpenses))}";
                }
                string corruptionStr = "";
                if (d.CorruptionLevel > 0)
                {
                    corruptionStr = $"\nCorruption: {ColorRed("-" + FormatBig((long)Math.Abs(d.ExpensesCorruption)))}";
                }

                return
                    $"Treasury: {treasury}\n\n" +
                    $"Income:   {income}\n" +
                    $"Expenses: {expenses}\n" +
                    tradeStr + 
                    corruptionStr + "\n" +
                    $"Balance:  {balance}\n\n" +
                    $"<color=#999999>Hold <b>Shift</b> for detailed breakdown</color>";
            }
            
            // ---- Extended breakdown ----
            string taxRateStr   = ColorGold($"{d.TaxRateLocal * 100f:0.##}%");
            string warPenStr    = d.TaxPenaltyFromWar != 0 ? ColorRed($"-{d.TaxPenaltyFromWar:0.##}%") : ColorGold("0%");
            string stabModStr   = d.TaxModifierFromStability != 0 ? (d.TaxModifierFromStability > 0 ? ColorGreen($"+{d.TaxModifierFromStability:0.##}%") : ColorRed($"{d.TaxModifierFromStability:0.##}%")) : ColorGold("0%");
            string cityModStr   = d.TaxModifierFromCities != 0 ? ColorGold($"+{d.TaxModifierFromCities:0.##}%") : ColorGold("0%");
            string baseWealth   = Money(SafeLong(d.TaxBaseWealth));
            string incomeBase   = Money(d.IncomeBeforeModifiers);
            string incomeWar    = Money(d.IncomeAfterWarPenalty);
            string incomeStab   = Money(d.IncomeAfterStability);
            string incomeCities = Money(d.IncomeAfterCityBonus);
            string incomeTrade  = Money(d.TradeIncome);
            string armyCost     = Money(-Math.Abs(d.ExpensesMilitary));
            string infraCost    = Money(-Math.Abs(d.ExpensesInfrastructure));
            string demoCost     = Money(-Math.Abs(d.ExpensesDemography));
            string warOverhead  = Money(-Math.Abs(d.ExpensesWarOverhead));
            string lawsCost     = Money(-Math.Abs(d.ExpensesLawUpkeep));
            string tradeCost    = Money(-Math.Abs(d.TradeExpenses));
            string corruptCost  = Money(-Math.Abs(d.ExpensesCorruption));

            // New Modifiers
            string buildSpeed = d.BuildingSpeedModifier != 1f ? ColorGold($"{d.BuildingSpeedModifier*100:0}%") : "100%";
            string factoryOut = d.FactoryOutputModifier != 1f ? ColorGold($"{d.FactoryOutputModifier*100:0}%") : "100%";
            string resOut     = d.ResourceOutputModifier != 1f ? ColorGold($"{d.ResourceOutputModifier*100:0}%") : "100%";
            string tradeMod   = d.TradeIncomeModifier != 1f ? ColorGold($"{d.TradeIncomeModifier*100:0}%") : "100%";
            string investCost = d.InvestmentCostModifier != 1f ? ColorGold($"{d.InvestmentCostModifier*100:0}%") : "100%";

            return
                $"Treasury: {treasury}\n" +
                $"Balance:  {balance}\n\n" +
                $"<b>INCOME</b> (Level: {d.TaxationLevel})\n" +
                $"- Base taxable wealth: {baseWealth}\n" +
                $"- Raw tax {taxRateStr}:             {incomeBase}\n" +
                $"- After war penalty ({warPenStr}):    {incomeWar}\n" +
                $"- After stability ({stabModStr}):     {incomeStab}\n" +
                $"- After cities bonus ({cityModStr}):  {incomeCities}\n" +
                $"- Trade Income:                       {incomeTrade}\n" +
                $"- Final income:       {income}\n\n" +
                $"<b>EXPENSES</b>\n" +
                $"- Army ({FormatBig(d.Soldiers)} soldiers):        {armyCost}\n" +
                $"- Infrastructure ({FormatBig(d.Cities)} cities, {FormatBig(d.Buildings)} bld): {infraCost}\n" +
                $"- Demography:                         {demoCost}\n" +
                $"- War overhead:                       {warOverhead}\n" +
                $"- Laws & Policies:                    {lawsCost}\n" +
                $"- Trade Import Costs:                 {tradeCost}\n" +
                $"- Corruption Drain ({d.CorruptionLevel*100:0}%):    {corruptCost}\n" +
                $"- Total expenses:     {expenses}\n\n" +
                $"<b>MODIFIERS</b>\n" +
                $"- Build Speed: {buildSpeed}\n" +
                $"- Factory Output: {factoryOut}\n" +
                $"- Resource Output: {resOut}\n" +
                $"- Trade Income Mod: {tradeMod}\n" +
                $"- Investment Cost: {investCost}";
        }

        // ==============================================================================================
        // POPULATION TOOLTIP
        // ==============================================================================================
        
        // Attach population tooltip to a chip
        public static void AttachPopulationTooltip(GameObject chip)
        {
            EnsureTooltip();
            var et = AddOrGetEventTrigger(chip);
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener((_) =>
            {
                var k = Main.selectedKingdom;
                currentProvider = () => PopulationTooltip(k);
                Show();
            });
            et.triggers.Add(enter);

            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener((_) => Hide());
            et.triggers.Add(exit);
        }
        
        // Population tooltip text
        public static string PopulationTooltip(Kingdom k)
        {
            var d = KingdomMetricsSystem.Get(k);
            if (d == null) return "No data";
            
            long delta = d.Population - d.PrevPopulation;
            string deltaStr = delta >= 0
                ? ColorGreen("+" + FormatBig(delta))
                : ColorRed(FormatBig(delta));
            string growthStr = ColorGold($"{d.AvgGrowthRate:0.##}%");
            
            bool extended = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (!extended)
            {
                return
                    $"Population: {ColorGold(FormatBig(d.Population))}\n\n" +
                    $"Population Change: {deltaStr}\n" +
                    $"-Average Growth Rate: {growthStr}\n\n" +
                    $"<color=#999999>Hold <b>Shift</b> for detailed breakdown</color>";
            }
            string happyRatio = d.Population > 0
                ? ColorGold($"{(float)d.HappyUnits / d.Population * 100f:0.##}%")
                : ColorGold("N/A");
            string unemployed = ColorGold(FormatBig(d.Unemployed));
            string homeless   = ColorGold(FormatBig(d.Homeless));
            string hungry     = ColorGold(FormatBig(d.Hungry));
            string starving   = ColorGold(FormatBig(d.Starving));
            string sick       = ColorGold(FormatBig(d.Sick));
            string babies     = ColorGold(FormatBig(d.Babies));
            string teens      = ColorGold(FormatBig(d.Teens));
            string elders     = ColorGold(FormatBig(d.Elders));
            string veterans   = ColorGold(FormatBig(d.Veterans));
            string genius     = ColorGold(FormatBig(d.Genius));
            
            string researchEff = ColorGold($"{d.ResearchOutputModifier * 100f:0.##}%");
            string plagueRes = ColorGold($"{d.PlagueResistanceModifier * 100f:0.##}%");
            string geniusChance = ColorGold($"+{d.GeniusChanceModifier * 100f:0.##}%");
            string popGrowth = ColorGold($"+{d.PopulationGrowthBonus * 100f:0.##}%");

            return
                $"Population: {ColorGold(FormatBig(d.Population))}\n" +
                $"Change: {deltaStr}\n" +
                $"-Average Growth Rate: {growthStr}\n\n" +
                $"<b>Demographics</b>\n" +
                $"-Children: {ColorGold(FormatBig(d.Children))}\n" +
                $"-Babies (0–4): {babies}\n" +
                $"-Teens (5–15): {teens}\n" +
                $"-Adults: {ColorGold(FormatBig(d.Adults))}\n" +
                $"-Elders (60+): {elders}\n" +
                $"-Veterans: {veterans}\n" +
                $"-Geniuses: {genius}\n\n" +
                $"<b>Social stats</b>\n" +
                $"-Unemployed: {unemployed}\n" +
                $"-Homeless: {homeless}\n" +
                $"-Hungry: {hungry}\n" +
                $"-Starving: {starving}\n" +
                $"-Sick: {sick}\n" +
                $"-Happy Units: {ColorGold(FormatBig(d.HappyUnits))} ({happyRatio})\n" +
                $"-Research Eff: {researchEff}\n" +
                $"-Plague Res: {plagueRes}\n" +
                $"-Genius Chance: {geniusChance}\n" +
                $"-Growth Bonus: {popGrowth}";
        }

        // ==============================================================================================
        // WAR TOOLTIP
        // ==============================================================================================
        
        // Attach war tooltip to a chip
        public static void AttachWarTooltip(GameObject chip)
        {
            EnsureTooltip();
            var et = AddOrGetEventTrigger(chip);
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener((_) =>
            {
                var k = Main.selectedKingdom;
                currentProvider = () => WarTooltip(k);
                Show();
            });
            et.triggers.Add(enter);
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener((_) => Hide());
            et.triggers.Add(exit);
        }
        
        // War tooltip text
        public static string WarTooltip(Kingdom k)
        {
            var d = KingdomMetricsSystem.Get(k);
            if (d == null) return "No data";
            
            var cur = ColorGold($"{d.WarExhaustion:0.#}");
            var chg = d.WEChange >= 0
                ? ColorGreen($"+{d.WEChange:0.#}")
                : ColorRed($"{d.WEChange:0.#}");
            string taxEffect = d.TaxPenaltyFromWar != 0
                ? ColorRed($"-{d.TaxPenaltyFromWar:0.##}%")
                : ColorGold("0%");
            string manpowerEffect = d.WarEffectOnManpowerPct != 0
                ? ColorRed($"{d.WarEffectOnManpowerPct:0.##}%")
                : ColorGold("0%");
            string stabEffect = d.WarEffectOnStabilityPerYear != 0
                ? ColorRed($"{d.WarEffectOnStabilityPerYear:0.##} pp / year")
                : ColorGold("0 pp / year");
                
            string gainMult = d.WarExhaustionGainMultiplier != 1f 
                ? (d.WarExhaustionGainMultiplier > 1f ? ColorRed($"{d.WarExhaustionGainMultiplier:0.##}x") : ColorGreen($"{d.WarExhaustionGainMultiplier:0.##}x"))
                : ColorGold("1x");
            
            string mpMax = d.ManpowerMaxMultiplier != 1f ? ColorGold($"{d.ManpowerMaxMultiplier*100:0}%") : "100%";
            string milUpkeep = d.MilitaryUpkeepModifier != 1f ? ColorGold($"{d.MilitaryUpkeepModifier*100:0}%") : "100%";
            string milAttack = d.MilitaryAttackModifier != 0f ? ColorGold($"+{d.MilitaryAttackModifier*100:0}%") : "0%";
            string justTime = d.JustificationTimeModifier != 1f ? ColorGold($"{d.JustificationTimeModifier*100:0}%") : "100%";

            return
                $"{cur}% War Exhaustion\n\n" +
                $"Change: {chg} / update\n" +
                $"Gain Multiplier: {gainMult}\n\n" +
                $"Effects:\n" +
                $"-Tax Income: {taxEffect}\n" +
                $"-Manpower Capacity: {manpowerEffect}\n" +
                $"-Stability Drift: {stabEffect}\n\n" +
                $"<b>MODIFIERS</b>\n" +
                $"- Manpower Cap: {mpMax}\n" +
                $"- Army Upkeep: {milUpkeep}\n" +
                $"- Army Attack: {milAttack}\n" +
                $"- Justification Time: {justTime}\n";
        }

        // ==============================================================================================
        // STABILITY TOOLTIP
        // ==============================================================================================
        
        // Attach stability tooltip to a chip
        public static void AttachStabilityTooltip(GameObject chip)
        {
            EnsureTooltip();
            var et = AddOrGetEventTrigger(chip);
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener((_) =>
            {
                var k = Main.selectedKingdom;
                currentProvider = () => StabilityTooltip(k);
                Show();
            });
            et.triggers.Add(enter);
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener((_) => Hide());
            et.triggers.Add(exit);
        }
        
        // Stability tooltip text
        public static string StabilityTooltip(Kingdom k)
        {
            var d = KingdomMetricsSystem.Get(k);
            if (d == null) return "No data";
            
            var change60 = d.StabilityChange >= 0
                ? ColorGreen($"+{d.StabilityChange:0.#} pp")
                : ColorRed($"{d.StabilityChange:0.#} pp");
            string taxEffect = d.TaxModifierFromStability != 0
                ? (d.TaxModifierFromStability > 0 ? ColorGreen($"+{d.TaxModifierFromStability:0.##}%") : ColorRed($"{d.TaxModifierFromStability:0.##}%"))
                : ColorGold("0%");
            string corr = d.CorruptionLevel > 0 
                ? ColorRed($"-{d.CorruptionLevel * 40f:0.#} pp/yr")
                : ColorGold("0");
            
            float targetStab = 50f + d.StabilityTargetModifier;
            string targetStr = ColorGold($"{targetStab:0.#}%");
            string corruptionStr = ColorRed($"{d.CorruptionLevel*100f:0.#}%");

            string unrestRed = d.UnrestReductionModifier != 1f ? ColorGold($"{d.UnrestReductionModifier*100:0}%") : "100%";
            string rebelSup = d.RebelSuppressionModifier != 1f ? ColorGold($"{d.RebelSuppressionModifier*100:0}%") : "100%";
            string integSpeed = d.IntegrationSpeedModifier != 1f ? ColorGold($"{d.IntegrationSpeedModifier*100:0}%") : "100%";

            return
                $"{ColorGold($"{d.Stability:0.#}%")} Stability (Base 50%)\n" +
                $"Target Equilibrium: {targetStr}\n\n" +
                $"Change: {change60}\n\n" +
                $"Corruption: {corruptionStr}\n\n" +
                $"Effects:\n" +
                $"-Tax Income: {taxEffect}\n" +
                $"-Corruption Impact: {corr}\n\n" +
                $"<b>MODIFIERS</b>\n" +
                $"- Unrest Reduction: {unrestRed}\n" +
                $"- Rebel Suppression: {rebelSup}\n" +
                $"- Integration Speed: {integSpeed}";
        }

        // ==============================================================================================
        // HELPERS
        // ==============================================================================================

        // Attach a simple tooltip to a GameObject with text provided by a function
        public static void AttachSimpleTooltip(GameObject go, System.Func<string> textProvider)
        {
            EnsureTooltip();
            var et = AddOrGetEventTrigger(go);
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ =>
            {
                currentProvider = textProvider;
                if (tooltip != null)
                {
                    tooltipText.text = currentProvider();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(tooltip.GetComponent<RectTransform>());
                    tooltip.SetActive(true);
                    tooltip.transform.SetAsLastSibling();
                }
            });
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ =>
            {
                if (tooltip != null) tooltip.SetActive(false);
                if (currentProvider == textProvider) currentProvider = null;
            });
            et.triggers.Add(enter);
            et.triggers.Add(exit);
        }

        // Ensure the tooltip GameObject is created
        private static void EnsureTooltip()
        {
            if (tooltip != null) return;
            
            tooltip = new GameObject("RulerBox_Tooltip");
            tooltip.transform.SetParent(DebugConfig.instance?.transform, false);
            tooltip.transform.SetAsLastSibling();
            
            // Background image
            var img = tooltip.AddComponent<Image>();
            img.sprite = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.ToolTip.png");
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            
            // Layout
            var rt = tooltip.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot     = new Vector2(0f, 1f);               
            rt.sizeDelta = Vector2.zero;                         
            
            // Padding and layout group
            var vlg = tooltip.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(12, 12, 8, 8);         
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;
            
            // Content size fitter
            var fit = tooltip.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;
            
            // Text
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(tooltip.transform, false);
            tooltipText = textGO.AddComponent<Text>();
            tooltipText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            tooltipText.supportRichText = true;
            tooltipText.alignment = TextAnchor.MiddleLeft;     
            tooltipText.color = Color.white;
            tooltipText.resizeTextForBestFit = false;            
            tooltipText.fontSize = 6; 
            tooltipText.horizontalOverflow = HorizontalWrapMode.Wrap;
            tooltipText.verticalOverflow   = VerticalWrapMode.Overflow; 
            tooltipText.lineSpacing = 1.0f;
            tooltipText.raycastTarget = false;
            
            // Text RectTransform
            var textRT = tooltipText.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            
            // Layout Element for width control
            var textLE = textGO.AddComponent<LayoutElement>();
            textLE.minWidth = 100f;
            textLE.preferredWidth = 110f; 
            
            // Tooltip follower
            tooltip.AddComponent<TooltipFollower>();
            tooltip.SetActive(false);
        }

        // Add or get EventTrigger component
        private static EventTrigger AddOrGetEventTrigger(GameObject go)
        {
            var et = go.GetComponent<EventTrigger>();
            if (et == null) et = go.AddComponent<EventTrigger>();
            return et;
        }

        // Force hide the tooltip
        public static void ForceHideTooltip()
        {
            if (tooltip != null) tooltip.SetActive(false);
            currentProvider = null;
        }

        // Show the tooltip
        private static void Show()
        {
            if (tooltip == null) return;
            
            if (currentProvider != null) tooltipText.text = currentProvider();
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltip.GetComponent<RectTransform>());
            tooltip.SetActive(true);
            tooltip.transform.SetAsLastSibling();
        }

        // Hide the tooltip
        private static void Hide()
        {
            if (tooltip != null) tooltip.SetActive(false);
        }

        // Tooltip follower component
        private class TooltipFollower : MonoBehaviour
        {
            private RectTransform rt;
            private void Start() { rt = GetComponent<RectTransform>(); }
            
            // Update is called once per frame
            void Update()
            {
                if (tooltip != null && tooltip.activeSelf)
                {
                    if (currentProvider != null)
                        tooltipText.text = currentProvider();
                    Vector3 mousePos = Input.mousePosition;
                    Vector3 offset = new Vector3(15, -15, 0);                
                    float screenWidth = Screen.width;
                    if (mousePos.x + rt.sizeDelta.x + offset.x > screenWidth)
                    {
                        offset.x = -rt.sizeDelta.x - 15;
                    }
                    if (mousePos.y + offset.y - rt.sizeDelta.y < 0)
                    {
                        offset.y = rt.sizeDelta.y + 15;
                    }
                    transform.position = mousePos + offset;
                }
            }
        }

        // Clear simple tooltip from a GameObject
        public static void ClearSimpleTooltip(GameObject go)
        {
            if (go == null) return;
            
            var et = go.GetComponent<EventTrigger>();
            if (et == null || et.triggers == null) return;
            
            for (int i = et.triggers.Count - 1; i >= 0; i--)
            {
                var ev = et.triggers[i].eventID;
                if (ev == EventTriggerType.PointerEnter || ev == EventTriggerType.PointerExit)
                {
                    et.triggers.RemoveAt(i);
                }
            }
        }

        // Hide tooltip immediately
        public static void HideTooltipNow()
        {
            if (tooltip != null) tooltip.SetActive(false);
            currentProvider = null;
        }

        // Utility formatting
        public static string ColorGold(string txt)  => $"<color=#FFD700>{txt}</color>";
        public static string ColorGreen(string txt) => $"<color=#7CFC00>{txt}</color>";
        public static string ColorRed(string txt) => $"<color=#FF5A5A>{txt}</color>";
        public static string FormatBig(long v) => v.ToString("#,0").Replace(',', ' ');
        
        // Format big numbers with suffixes
        private static long SafeLong(double v)
        {
            if (v > long.MaxValue) return long.MaxValue;
            if (v < long.MinValue) return long.MinValue;
            return (long)Math.Round(v);
        }
        
        // Format money with color coding
        public static string Money(long v, string whenPositive = "green", bool zeroGold = false)
        {
            var s = $"${FormatBig(v)}";
            if (v > 0) return whenPositive == "gold" ? ColorGold(s) : ColorGreen(s);
            if (v < 0) return ColorRed(s);
            return zeroGold ? ColorGold(s) : s;
        }
    }
}