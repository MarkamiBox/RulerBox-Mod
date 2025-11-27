using UnityEngine;
using UnityEngine.UI;
using System;

namespace RulerBox
{
    public static class EconomyWindow
    {
        private static GameObject root;
        private static Sprite windowInnerSprite;
        private static Sprite bgSprite;
        // Economy UI fields
        private static Text ecoIncomeText;
        private static Text ecoExpensesText;
        private static Text balanceText;
        // Income list breakdown
        private static Text incomeTaxBaseWealthText;
        private static Text incomeTaxBaseFallbackText;
        private static Text incomeBeforeModsText;
        private static Text incomeAfterWarText;
        private static Text incomeAfterStabText;
        private static Text incomeAfterCitiesText;
        // Expense breakdown
        private static Text expensesMilitaryText;
        private static Text expensesInfraText;
        private static Text expensesDemoText;
        private static Text expensesWarText;
        private static Text expensesLawsText;
        // Colors (from original TopPanelUI)
        private static readonly Color PositiveColor = new Color(0.48f, 0.99f, 0f, 1f);
        private static readonly Color NegativeColor = new Color(1f, 0.35f, 0.35f, 1f);
        private static readonly Color NeutralColor  = new Color(1f, 0.84f, 0f, 1f);
        public static void Initialize(Transform parent)
        {
            // Initialize sub-windows
            ResourcesTradeWindow.Initialize(parent);
            TradeWindow.Initialize(parent);
            if (root != null) return;
            windowInnerSprite = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.windowInnerSliced.png");
            //bgSprite          = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.EconHub.png");
            // Root
            root = new GameObject("EconomyRoot");
            root.transform.SetParent(parent, false);
            // Full-stretch RectTransform
            var rt = root.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            // General vertical layout
            var v = root.AddComponent<VerticalLayoutGroup>();
            v.childAlignment = TextAnchor.UpperCenter;
            v.spacing = 6;
            v.padding = new RectOffset(6, 6, 6, 6);
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            // Main row (Income | Expenses)
            var mainRow = new GameObject("MainRow");
            mainRow.transform.SetParent(root.transform, false);
            var mainHL = mainRow.AddComponent<HorizontalLayoutGroup>();
            mainHL.childAlignment = TextAnchor.UpperCenter;
            mainHL.spacing = 8;
            mainHL.childControlWidth = true;
            mainHL.childControlHeight = true;
            mainHL.childForceExpandWidth = true;
            mainHL.childForceExpandHeight = true;
            var mainLE = mainRow.AddComponent<LayoutElement>();
            mainLE.preferredHeight = 110f;
            mainLE.flexibleHeight = 0f;
            // Left Column: INCOME
            var incomeCol = new GameObject("IncomeColumn");
            incomeCol.transform.SetParent(mainRow.transform, false);
            var incomeV = incomeCol.AddComponent<VerticalLayoutGroup>();
            incomeV.childAlignment = TextAnchor.UpperCenter;
            incomeV.spacing = 3;
            incomeV.childControlWidth = true;
            incomeV.childControlHeight = true;
            incomeV.childForceExpandWidth = true;
            incomeV.childForceExpandHeight = false;
            var incomeColLE = incomeCol.AddComponent<LayoutElement>();
            incomeColLE.flexibleWidth = 1f;
            var incomeHeaderGO = new GameObject("IncomeHeader");
            incomeHeaderGO.transform.SetParent(incomeCol.transform, false);
            var incomeHeaderText = incomeHeaderGO.AddComponent<Text>();
            incomeHeaderText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            incomeHeaderText.text = "Income";
            incomeHeaderText.alignment = TextAnchor.UpperCenter;  
            incomeHeaderText.color = new Color(0.48f, 0.99f, 0f, 1f);
            incomeHeaderText.resizeTextMinSize = 8;
            incomeHeaderText.resizeTextMaxSize = 10;
            // Income List (scroll)
            BuildIncomeScroll(incomeCol.transform);
            // Total Income (outside list)
            ecoIncomeText = BuildRow(incomeCol.transform, "Total income:", usePanelBackground: true);
            ecoIncomeText.color = PositiveColor;
            // Right Column: EXPENSES
            var expensesCol = new GameObject("ExpensesColumn");
            expensesCol.transform.SetParent(mainRow.transform, false);
            var expensesV = expensesCol.AddComponent<VerticalLayoutGroup>();
            expensesV.childAlignment = TextAnchor.UpperCenter;
            expensesV.spacing = 3;
            expensesV.childControlWidth = true;
            expensesV.childControlHeight = true;
            expensesV.childForceExpandWidth = true;
            expensesV.childForceExpandHeight = false;
            var expensesColLE = expensesCol.AddComponent<LayoutElement>();
            expensesColLE.flexibleWidth = 1f;
            var expensesHeaderGO = new GameObject("ExpensesHeader");
            expensesHeaderGO.transform.SetParent(expensesCol.transform, false);
            var expensesHeaderText = expensesHeaderGO.AddComponent<Text>();
            expensesHeaderText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            expensesHeaderText.text = "Expenses";
            expensesHeaderText.alignment = TextAnchor.UpperCenter; 
            expensesHeaderText.color = new Color(1f, 0.35f, 0.35f, 1f);
            expensesHeaderText.resizeTextMinSize = 8;
            expensesHeaderText.resizeTextMaxSize = 10;
            // Expenses List (scroll)
            BuildExpensesScroll(expensesCol.transform);
            // Total Expenses (outside list)
            ecoExpensesText = BuildRow(expensesCol.transform, "Total expenses:", usePanelBackground: true);
            ecoExpensesText.color = NegativeColor;
            // Bottom Row: BALANCE + Buttons
            var balanceRow = new GameObject("BalanceRow");
            balanceRow.transform.SetParent(root.transform, false);
            // Balance background
            var balanceBg = balanceRow.AddComponent<Image>();
            balanceBg.sprite = windowInnerSprite;
            balanceBg.type = Image.Type.Sliced;
            // Balance layout
            var balanceH = balanceRow.AddComponent<HorizontalLayoutGroup>();
            balanceH.childAlignment = TextAnchor.MiddleCenter;
            balanceH.spacing = 0;
            balanceH.childControlWidth = true;
            balanceH.childControlHeight = true;
            balanceH.childForceExpandWidth = false;
            balanceH.childForceExpandHeight = false;
            var balanceLE = balanceRow.AddComponent<LayoutElement>();
            balanceLE.preferredHeight = 22f;
            // Balance label
            var balanceValueGO = new GameObject("Value");
            balanceValueGO.transform.SetParent(balanceRow.transform, false);
            balanceText = balanceValueGO.AddComponent<Text>();
            balanceText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            balanceText.text = "0";
            balanceText.alignment = TextAnchor.MiddleCenter;
            balanceText.color = NeutralColor;
            balanceText.resizeTextMinSize = 8;
            balanceText.resizeTextMaxSize = 10;
            // bottom buttons row
            var buttonsRow = new GameObject("BottomButtonsRow");
            buttonsRow.transform.SetParent(root.transform, false);
            var buttonsHL = buttonsRow.AddComponent<HorizontalLayoutGroup>();
            buttonsHL.childAlignment = TextAnchor.MiddleCenter;
            buttonsHL.spacing = 6;
            buttonsHL.childControlWidth = true;
            buttonsHL.childControlHeight = true;
            buttonsHL.childForceExpandWidth = true;
            buttonsHL.childForceExpandHeight = false;
            var buttonsLE = buttonsRow.AddComponent<LayoutElement>();
            buttonsLE.preferredHeight = 30f;
            buttonsLE.flexibleHeight = 0f;
            // Buttons
            BuildBottomButton(buttonsRow.transform, "Economic Laws", () => {
                SetVisible(false);
                EconomicLawsWindow.SetVisible(true);
                EconomicLawsWindow.Refresh(Main.selectedKingdom);
            });
            BuildBottomButton(buttonsRow.transform, "Investments", () => {
                SetVisible(false);
                InvestmentsWindow.SetVisible(true);
                InvestmentsWindow.Refresh(Main.selectedKingdom);
            });
            BuildBottomButton(buttonsRow.transform, "Resources\n& Trade", () => {
                SetVisible(false);
                ResourcesTradeWindow.SetVisible(true);
                ResourcesTradeWindow.Refresh(Main.selectedKingdom);
            });
            root.SetActive(false);
        }
        // --- PUBLIC METHODS ---
        public static void SetVisible(bool visible)
        {
            if (root != null) 
            {
                if (visible)
                {
                    ResourcesTradeWindow.SetVisible(false);
                }
                root.SetActive(visible);
            }
        }
        public static bool IsVisible()
        {
            return root != null && root.activeSelf;
        }
        public static void Refresh(Kingdom k)
        {
            if (!IsVisible() || k == null) return;
            
            var d = KingdomMetricsSystem.Get(k);
            if (d == null) return;
            // Totals
            SetColoredNumber(ecoIncomeText, d.Income, forcePositive: true);
            SetColoredNumber(ecoExpensesText, d.Expenses, forceNegative: true);
            SetColoredNumber(balanceText, d.Income - d.Expenses);
            // Breakdown INCOME
            SetColoredNumber(incomeTaxBaseWealthText, d.TaxBaseWealth);
            SetColoredNumber(incomeTaxBaseFallbackText, d.TaxBaseFallbackGDP);
            SetColoredNumber(incomeBeforeModsText, d.IncomeBeforeModifiers);
            SetColoredNumber(incomeAfterWarText, d.IncomeAfterWarPenalty);
            SetColoredNumber(incomeAfterStabText, d.IncomeAfterStability);
            SetColoredNumber(incomeAfterCitiesText, d.IncomeAfterCityBonus);
            // Breakdown EXPENSES
            SetColoredNumber(expensesMilitaryText, d.ExpensesMilitary, forceNegative: true);
            SetColoredNumber(expensesInfraText, d.ExpensesInfrastructure, forceNegative: true);
            SetColoredNumber(expensesDemoText, d.ExpensesDemography, forceNegative: true);
            SetColoredNumber(expensesWarText, d.ExpensesWarOverhead, forceNegative: true);
            SetColoredNumber(expensesLawsText, d.ExpensesLawUpkeep, forceNegative: true);
        }
        // --- ORIGINAL HELPERS ---

        // Build a single row with label and value text
        private static Text BuildRow(Transform parent, string label, bool usePanelBackground = false)
        {
            var row = new GameObject(label.Replace(" ", "") + "Row");
            row.transform.SetParent(parent, false);

            if (usePanelBackground && windowInnerSprite != null)
            {
                var bg = row.AddComponent<Image>();
                bg.sprite = windowInnerSprite;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
            }

            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleLeft;
            h.spacing = 4;
            h.childControlWidth = true;
            h.childForceExpandWidth = true;
            // Label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(row.transform, false);
            var labelText = labelGO.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.text = label;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = Color.white;
            labelText.resizeTextForBestFit = true;
            labelText.resizeTextMinSize = 6;
            labelText.resizeTextMaxSize = 8;
            // Value
            var valueGO = new GameObject("Value");
            valueGO.transform.SetParent(row.transform, false);
            var valueText = valueGO.AddComponent<Text>();
            valueText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            valueText.text = "-";
            valueText.alignment = TextAnchor.MiddleRight;
            valueText.color = NeutralColor;
            valueText.resizeTextForBestFit = true;
            valueText.resizeTextMinSize = 6;
            valueText.resizeTextMaxSize = 8;
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 8;
            return valueText;
        }   
        // Build a single row for the scroll lists
        private static Text BuildListRow(Transform parent, string label)
        {
            var row = new GameObject(label.Replace(" ", "") + "ListRow");
            row.transform.SetParent(parent, false);
            // Layout
            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleLeft;
            h.spacing = 3;
            h.childControlWidth = true;
            h.childForceExpandWidth = true;
            // Label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(row.transform, false);
            var labelText = labelGO.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.text = label;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = Color.white;
            labelText.resizeTextForBestFit = true;
            labelText.resizeTextMinSize = 8;
            labelText.resizeTextMaxSize = 10;
            // Value
            var valueGO = new GameObject("Value");
            valueGO.transform.SetParent(row.transform, false);
            var valueText = valueGO.AddComponent<Text>();
            valueText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            valueText.text = "-";
            valueText.alignment = TextAnchor.MiddleRight;
            valueText.color = NeutralColor;
            valueText.resizeTextForBestFit = true;
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 14;
            return valueText;
        }
        // Build a bottom button
        private static Button BuildBottomButton(Transform parent, string label, Action onClick = null)
        {
            var go = new GameObject(label.Replace(" ", "") + "Button");
            go.transform.SetParent(parent, false);
            // Button background
            var img = go.AddComponent<Image>();
            img.sprite = windowInnerSprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            // Layout
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 90;
            le.preferredHeight = 20;
            // Button component
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() =>
            {
                if (onClick != null)
                {
                    onClick.Invoke();
                }
                else
                {
                    WorldTip.showNow($"{label} panel (WIP)", false, "top", 1.2f, "#FFFFFF");
                }
            });
            // Button label
            var txtGO = new GameObject("Text");
            txtGO.transform.SetParent(go.transform, false);
            var txt = txtGO.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = label;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.resizeTextForBestFit = true;
            txt.resizeTextMinSize = 6;
            txt.resizeTextMaxSize = 8;
            // Text RectTransform
            var rt = txt.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return btn;
        }
        // Build Income Scroll List
        private static void BuildIncomeScroll(Transform parent)
        {
            var scrollGO = new GameObject("IncomeScroll");
            scrollGO.transform.SetParent(parent, false);
            // Layout
            var rt = scrollGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            // Layout Element
            var le = scrollGO.AddComponent<LayoutElement>();
            le.preferredHeight = 50f;
            le.flexibleHeight = 0f;
            // Background
            var bgImg = scrollGO.AddComponent<Image>();
            if (windowInnerSprite != null)
            {
                bgImg.sprite = windowInnerSprite;
                bgImg.type = Image.Type.Sliced;
            }
            else
            {
                bgImg.color = new Color(0f, 0f, 0f, 0.35f);
            }
            // ScrollRect
            var scrollRect = scrollGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            // Viewport
            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(scrollGO.transform, false);
            var viewportRT = viewportGO.AddComponent<RectTransform>();
            viewportRT.anchorMin = new Vector2(0f, 0f);
            viewportRT.anchorMax = new Vector2(1f, 1f);
            viewportRT.offsetMin = new Vector2(2f, 5f); 
            viewportRT.offsetMax = new Vector2(-2f, -4f);
            var viewportImg = viewportGO.AddComponent<Image>();
            viewportImg.color = new Color(0f, 0f, 0f, 0.05f);
            // Mask
            var mask = viewportGO.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            // Content
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRT = contentGO.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot     = new Vector2(0.5f, 1f);
            contentRT.anchoredPosition = Vector2.zero;
            contentRT.sizeDelta = new Vector2(0f, 0f);
            // Vertical Layout
            var v = contentGO.AddComponent<VerticalLayoutGroup>();
            v.childAlignment = TextAnchor.UpperLeft;
            v.spacing = 2;
            v.padding = new RectOffset(2, 2, 2, 2);
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            // Content Size Fitter
            var fitter = contentGO.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            // Assign to ScrollRect
            scrollRect.viewport = viewportRT;
            scrollRect.content  = contentRT;
            // income breakdown rows
            incomeTaxBaseWealthText   = BuildListRow(contentRT, "Base Tax");
            incomeTaxBaseFallbackText = BuildListRow(contentRT, "GDP Tax");
            incomeBeforeModsText      = BuildListRow(contentRT, "Raw tax");
            incomeAfterWarText        = BuildListRow(contentRT, "War");
            incomeAfterStabText       = BuildListRow(contentRT, "Stability");
            incomeAfterCitiesText     = BuildListRow(contentRT, "City bonus");
        }

        private static void BuildExpensesScroll(Transform parent)
        {
            var scrollGO = new GameObject("ExpensesScroll");
            scrollGO.transform.SetParent(parent, false);
            // Layout
            var rt = scrollGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            // Layout Element
            var le = scrollGO.AddComponent<LayoutElement>();
            le.preferredHeight = 50f;
            le.flexibleHeight = 0f;
            // Background
            var bgImg = scrollGO.AddComponent<Image>();
            if (windowInnerSprite != null)
            {
                bgImg.sprite = windowInnerSprite;
                bgImg.type = Image.Type.Sliced;
                bgImg.color = Color.white;
            }
            else
            {
                bgImg.color = new Color(0f, 0f, 0f, 0.35f);
            }
            // ScrollRect
            var scrollRect = scrollGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            // Viewport
            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(scrollGO.transform, false);
            var viewportRT = viewportGO.AddComponent<RectTransform>();
            viewportRT.anchorMin = new Vector2(0f, 0f);
            viewportRT.anchorMax = new Vector2(1f, 1f);
            viewportRT.offsetMin = new Vector2(2f, 5f);
            viewportRT.offsetMax = new Vector2(-2f, -4f);
            var viewportImg = viewportGO.AddComponent<Image>();
            viewportImg.color = new Color(0f, 0f, 0f, 0.05f);
            // Mask
            var mask = viewportGO.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            // Content
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRT = contentGO.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot     = new Vector2(0.5f, 1f);
            contentRT.anchoredPosition = Vector2.zero;
            contentRT.sizeDelta = new Vector2(0f, 0f);
            // Vertical Layout
            var v = contentGO.AddComponent<VerticalLayoutGroup>();
            v.childAlignment = TextAnchor.UpperLeft;
            v.spacing = 2;
            v.padding = new RectOffset(2, 2, 2, 2);
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            // Content Size Fitter
            var fitter = contentGO.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            // Assign to ScrollRect
            scrollRect.viewport = viewportRT;
            scrollRect.content  = contentRT;
            // expense breakdown rows
            expensesMilitaryText = BuildListRow(contentRT, "Military upkeep");
            expensesInfraText    = BuildListRow(contentRT, "Infrastructure");
            expensesDemoText     = BuildListRow(contentRT, "Demography");
            expensesWarText      = BuildListRow(contentRT, "War overhead");
            expensesLawsText     = BuildListRow(contentRT, "Laws & Policies");
        }
        // Set colored number text based on value
        private static void SetColoredNumber(Text text, double value, bool forcePositive = false, bool forceNegative = false)
        {
            if (text == null) return;
            double abs = Math.Abs(value);
            string s;
            if (abs >= 1_000_000_000d) s = (value / 1_000_000_000d).ToString("0.#") + "B";
            else if (abs >= 1_000_000d) s = (value / 1_000_000d).ToString("0.#") + "M";
            else if (abs >= 1_000d) s = (value / 1_000d).ToString("0.#") + "k";
            else s = value.ToString("0");
            text.text = s;
            if (forcePositive)
            {
                text.color = PositiveColor;
                return;
            }
            if (forceNegative)
            {
                text.color = NegativeColor;
                return;
            }
            if (value > 0) text.color = PositiveColor;
            else if (value < 0) text.color = NegativeColor;
            else text.color = NeutralColor;
        }
    }
}