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
            return
                $"<b>Manpower Command</b>\n" +
                $"Current Mode: {modeStr}\n" +
                $"[B] Cycle Modes (Recruit/Dismiss/Normal)\n\n" +
                //$"Active Soldiers: {ColorGold(FormatBig(d.Soldiers))}\n" +
                $"Eligible Civilians: {ColorGold(FormatBig(d.Adults - d.Soldiers))}\n\n" +
                $"<b>Manpower Points (Draft Currency)</b>\n" +
                $"Available: {ColorGold(FormatBig(d.ManpowerCurrent))}\n" +
                $"Capacity:  {FormatBig(d.ManpowerMax)}\n\n" +
                $"<color=#999999>Drafting costs 1 Manpower Point per soldier.\n" +
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
                return
                    $"Treasury: {treasury}\n\n" +
                    $"Income:   {income}\n" +
                    $"Expenses: {expenses}\n" +
                    tradeStr + "\n" +
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
            return
                $"Treasury: {treasury}\n" +
                $"Balance:  {balance}\n\n" +
                $"<b>INCOME</b>\n" +
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
                $"- Total expenses:     {expenses}";
        }

        // ==============================================================================================
        // POPULATION TOOLTIP
        // ==============================================================================================
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
                $"-Happy Units: {ColorGold(FormatBig(d.HappyUnits))} ({happyRatio})";
        }

        // ==============================================================================================
        // WAR TOOLTIP
        // ==============================================================================================
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

            return
                $"{cur}% War Exhaustion\n\n" +
                $"Change: {chg} / update\n\n" +
                $"Effects:\n" +
                $"-Tax Income: {taxEffect}\n" +
                $"-Manpower Capacity: {manpowerEffect}\n" +
                $"-Stability Drift: {stabEffect}\n";
        }

        // ==============================================================================================
        // STABILITY TOOLTIP
        // ==============================================================================================
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

            string corr = ColorRed($"-{d.CorruptionLevel * 40f:0.#} pp/yr");

            return
                $"{ColorGold($"{d.Stability:0.#}%")} Stability (Base 50%)\n\n" +
                $"Change: {change60}\n\n" +
                $"Effects:\n" +
                $"-Tax Income: {taxEffect}\n" +
                $"-Corruption Impact: {corr}\n";
        }

        // ==============================================================================================
        // HELPERS
        // ==============================================================================================

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

        private static void EnsureTooltip()
        {
            if (tooltip != null) return;

            tooltip = new GameObject("RulerBox_Tooltip");
            tooltip.transform.SetParent(DebugConfig.instance?.transform, false);
            tooltip.transform.SetAsLastSibling();
            
            var img = tooltip.AddComponent<Image>();
            img.sprite = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.ToolTip.png");
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;

            var rt = tooltip.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot     = new Vector2(0f, 1f);               
            rt.sizeDelta = Vector2.zero;                         

            var vlg = tooltip.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(12, 12, 8, 8);         
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            var fit = tooltip.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fit.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

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

            var textRT = tooltipText.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            var textLE = textGO.AddComponent<LayoutElement>();
            textLE.minWidth = 100f;
            textLE.preferredWidth = 110f; 

            tooltip.AddComponent<TooltipFollower>();
            tooltip.SetActive(false);
        }

        private static EventTrigger AddOrGetEventTrigger(GameObject go)
        {
            var et = go.GetComponent<EventTrigger>();
            if (et == null) et = go.AddComponent<EventTrigger>();
            return et;
        }
        
        public static void ForceHideTooltip()
        {
            if (tooltip != null) tooltip.SetActive(false);
            currentProvider = null;
        }

        private static void Show()
        {
            if (tooltip == null) return;
            if (currentProvider != null) tooltipText.text = currentProvider();
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltip.GetComponent<RectTransform>());
            tooltip.SetActive(true);
            tooltip.transform.SetAsLastSibling();
        }

        private static void Hide()
        {
            if (tooltip != null) tooltip.SetActive(false);
        }

        private class TooltipFollower : MonoBehaviour
        {
            private RectTransform _rt;
            private void Start() { _rt = GetComponent<RectTransform>(); }

            void Update()
            {
                if (tooltip != null && tooltip.activeSelf)
                {
                    if (currentProvider != null)
                        tooltipText.text = currentProvider();

                    Vector3 mousePos = Input.mousePosition;
                    Vector3 offset = new Vector3(15, -15, 0);
                    
                    float screenWidth = Screen.width;
                    
                    if (mousePos.x + _rt.sizeDelta.x + offset.x > screenWidth)
                    {
                        offset.x = -_rt.sizeDelta.x - 15;
                    }
                    
                    if (mousePos.y + offset.y - _rt.sizeDelta.y < 0)
                    {
                        offset.y = _rt.sizeDelta.y + 15;
                    }

                    transform.position = mousePos + offset;
                }
            }
        }

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
        
        private static long SafeLong(double v)
        {
            if (v > long.MaxValue) return long.MaxValue;
            if (v < long.MinValue) return long.MinValue;
            return (long)Math.Round(v);
        }
        
        public static string Money(long v, string whenPositive = "green", bool zeroGold = false)
        {
            var s = $"${FormatBig(v)}";
            if (v > 0) return whenPositive == "gold" ? ColorGold(s) : ColorGreen(s);
            if (v < 0) return ColorRed(s);
            return zeroGold ? ColorGold(s) : s;
        }
    }
}