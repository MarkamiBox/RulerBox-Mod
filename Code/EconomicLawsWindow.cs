using UnityEngine;
using UnityEngine.UI;
using System;

namespace RulerBox
{
    public static class EconomicLawsWindow
    {
        private static GameObject root;
        private static Sprite windowInnerSprite;
        
        // ================== ECONOMIC LAWS WINDOW ==================
        public static void Initialize(Transform parent)
        {
            if (root != null) return;
            windowInnerSprite = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.windowInnerSliced.png");
            
            // Root container
            root = new GameObject("EconomicLawsRoot");
            root.transform.SetParent(parent, false);
            
            // Full stretch
            var rt = root.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            
            // Vertical layout
            var v = root.AddComponent<VerticalLayoutGroup>();
            v.childAlignment = TextAnchor.UpperCenter;
            v.spacing = 6;
            v.padding = new RectOffset(4, 4, 4, 4);
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            
            // Title text top center
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(root.transform, false);
            var title = titleGO.AddComponent<Text>();
            title.font = Resources.Load<Font>("Fonts/Roboto-Regular") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            title.text = "Economic Laws";
            title.alignment = TextAnchor.MiddleCenter;
            title.color = Color.white;
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 8;
            title.resizeTextMaxSize = 12;
            var titleLE = titleGO.AddComponent<LayoutElement>();
            titleLE.preferredHeight = 20f;
            
            // Scroll container
            var scrollGO = new GameObject("LawsScroll");
            scrollGO.transform.SetParent(root.transform, false);
            
            // Scroll Rect
            var scrollLE = scrollGO.AddComponent<LayoutElement>();
            scrollLE.preferredHeight = 140f;
            scrollLE.flexibleHeight = 1f;
            
            // Background
            var bgImg = scrollGO.AddComponent<Image>();
            if (windowInnerSprite != null)
            {
                bgImg.sprite = windowInnerSprite;
                bgImg.type = Image.Type.Sliced;
                bgImg.color = Color.white;
            }
            /*else
            {                                                       To Remove: Darken bg even without sprite
                //bgImg.color = new Color(0f, 0f, 0f, 0.35f);
            }*/
            
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
            viewportRT.offsetMin = new Vector2(2f, 4.5f);
            viewportRT.offsetMax = new Vector2(-2f, -4f);
            var viewportImg = viewportGO.AddComponent<Image>();
            viewportImg.color = new Color(0f, 0f, 0f, 0.01f);
            
            // Mask
            var mask = viewportGO.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            
            // Content inside scroll
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRT = contentGO.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot     = new Vector2(0.5f, 1f);
            contentRT.anchoredPosition = Vector2.zero;
            contentRT.sizeDelta = new Vector2(0f, 0f);
            
            // Vertical Layout for content
            var contentVL = contentGO.AddComponent<VerticalLayoutGroup>();
            contentVL.childAlignment = TextAnchor.UpperCenter;
            contentVL.spacing = 4;
            contentVL.padding = new RectOffset(2, 2, 2, 2);
            contentVL.childControlWidth = true;
            contentVL.childControlHeight = true;
            contentVL.childForceExpandWidth = true;
            contentVL.childForceExpandHeight = false;
            
            // Content Size Fitter
            var fitter = contentGO.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            
            // Assign to ScrollRect
            scrollRect.viewport = viewportRT;
            scrollRect.content  = contentRT;
            
            // Add all laws
            AddLawRow(contentRT, "taxation", "Taxation Laws",
                "Minimum", "Low", "Normal", "High", "Maximum");
            AddLawRow(contentRT, "military_spending",   "Military Spending",
                "None", "Low", "Medium", "High", "Maximum");
            AddLawRow(contentRT, "security_spending",   "Security Spending",
                "None", "Low", "Medium", "High", "Maximum");
            AddLawRow(contentRT, "government_spending", "Government Spending",
                "None", "Low", "Medium", "High", "Maximum");
            AddLawRow(contentRT, "welfare_spending", "Healthcare Spending",
                "None", "Low", "Medium", "High", "Maximum");
            AddLawRow(contentRT, "education_spending",  "Education Spending",
                "None", "Low", "Medium", "High", "Maximum");
            AddLawRow(contentRT, "research_spending",   "Research Spending",
                "None", "Low", "Medium", "High", "Maximum");
            AddLawRow(contentRT, "anti_corruption",     "Anti Corruption Spending",
                "None", "Low", "Medium", "High", "Maximum");
            
            // Bottom
            var bottomRow = new GameObject("BottomRow");
            bottomRow.transform.SetParent(root.transform, false);
            var bottomHL = bottomRow.AddComponent<HorizontalLayoutGroup>();
            bottomHL.childAlignment = TextAnchor.MiddleCenter;
            bottomHL.spacing = 0;
            bottomHL.childControlWidth = false;
            bottomHL.childControlHeight = true;
            bottomHL.childForceExpandWidth = false;
            bottomHL.childForceExpandHeight = false;
            
            // Layout Element
            var bottomLE = bottomRow.AddComponent<LayoutElement>();
            bottomLE.preferredHeight = 26f;
            
            // Back Button
            BuildBackButton(bottomRow.transform, "Back", () => TopPanelUI.ReturnToEconomyMain());
            root.SetActive(false);
        }

        // Show or hide the window
        public static void SetVisible(bool visible)
        {
            if (root != null) root.SetActive(visible);
        }
        
        // Check if window is visible
        public static bool IsVisible()
        {
            return root != null && root.activeSelf;
        }
        
        // Refresh the displayed laws based on kingdom data
        public static void Refresh(Kingdom k)
        {
            if (!IsVisible() || k == null) return;
            
            var d = KingdomMetricsSystem.Get(k);
            if (d == null) return;
            
            // Update highlights
            Transform content = root.transform.Find("LawsScroll/Viewport/Content");
            if (content != null) {
                UpdateRowHighlight(content, "TaxationLawsRow", d.TaxationLevel);
                UpdateRowHighlight(content, "MilitarySpendingRow", d.MilitarySpending);
                UpdateRowHighlight(content, "SecuritySpendingRow", d.SecuritySpending);
                UpdateRowHighlight(content, "GovernmentSpendingRow", d.GovernmentSpending);
                UpdateRowHighlight(content, "HealthcareSpendingRow", d.WelfareSpending);
                UpdateRowHighlight(content, "EducationSpendingRow", d.EducationSpending);
                UpdateRowHighlight(content, "ResearchSpendingRow", d.ResearchSpending);
                UpdateRowHighlight(content, "AntiCorruptionSpendingRow", d.AntiCorruption);
            }
        }

        // Add a law row with buttons for each level
        private static void AddLawRow(Transform parent, string lawId, string displayName, params string[] levels)
        {
            var rowGO = new GameObject(displayName.Replace(" ", "") + "Row");
            rowGO.transform.SetParent(parent, false);
            
            // Vertical Layout for row
            var v = rowGO.AddComponent<VerticalLayoutGroup>();
            v.childAlignment = TextAnchor.UpperCenter;
            v.spacing = 2;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = false;
            v.childForceExpandHeight = false;
            
            // Layout Element for row
            var rowLE = rowGO.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 30f;
            
            // Law label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(rowGO.transform, false);
            var label = labelGO.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.text = displayName;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 7;
            label.resizeTextMaxSize = 10;
            
            // Buttons row
            var buttonsRow = new GameObject("ButtonsRow");
            buttonsRow.transform.SetParent(rowGO.transform, false);
            
            // Horizontal Layout for buttons
            var h = buttonsRow.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleCenter;
            h.spacing = 2;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = false;
            h.padding = new RectOffset(2, 2, 0, 0);
            var buttonsLE = buttonsRow.AddComponent<LayoutElement>();
            buttonsLE.preferredHeight = 18f;
            
            // Create buttons for each level
            foreach (var level in levels)
            {
                CreateLawButton(buttonsRow.transform, lawId, level);
            }
        }

        // Create a button for a specific law level
        private static void CreateLawButton(Transform parent, string lawId, string level)
        {
            var go = new GameObject(level.Replace(" ", "") + "Btn");
            go.transform.SetParent(parent, false);
            
            // Background Image
            var img = go.AddComponent<Image>();
            img.sprite = windowInnerSprite;
            img.type = Image.Type.Sliced;
            img.color = new Color(0.2f, 0.2f, 0.25f, 1f);
            
            // Layout Element
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 16f;
            le.flexibleWidth = 1f;
            
            // Button
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() =>
            {
                var k = Main.selectedKingdom;
                if (k == null) return;
                var d = KingdomMetricsSystem.Get(k);
                if (d == null) return;
                SetActiveLaw(d, lawId, level);
                TopPanelUI.Refresh(); 
                EconomicLawsWindow.Refresh(k);
                WorldTip.showNow($"Set {lawId} -> {level}", false, "top", 1f, "#9EE07A");
            });
            
            // Text
            var txtGO = new GameObject("Text");
            txtGO.transform.SetParent(go.transform, false);
            var txt = txtGO.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = level;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.resizeTextForBestFit = true;
            txt.resizeTextMinSize = 6;
            txt.resizeTextMaxSize = 8;
            
            // Stretch Text
            var rt = txt.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            
            // Attach Tooltip
            ChipTooltips.AttachSimpleTooltip(go, () => GetLawTooltip(lawId, level));
        }

        // Build a back button at the bottom
        private static void BuildBackButton(Transform parent, string label, Action onClick)
        {
            var go = new GameObject(label.Replace(" ", "") + "Button");
            go.transform.SetParent(parent, false);
            
            // Background Image
            var img = go.AddComponent<Image>();
            img.sprite = windowInnerSprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            
            // Layout Element
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 90;
            le.preferredHeight = 20;
            
            // Button
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());
            
            // Text
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
            
            // Stretch Text
            var rt = txt.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // Set the active law level in kingdom data
        private static void SetActiveLaw(KingdomMetricsSystem.Data d, string lawId, string lvl)
        {
            switch (lawId)
            {
                case "taxation": d.TaxationLevel = lvl; break;
                case "military_spending": d.MilitarySpending = lvl; break;
                case "security_spending": d.SecuritySpending = lvl; break;
                case "government_spending": d.GovernmentSpending = lvl; break;
                case "welfare_spending": d.WelfareSpending = lvl; break;
                case "education_spending": d.EducationSpending = lvl; break;
                case "research_spending": d.ResearchSpending = lvl; break;
                case "anti_corruption": d.AntiCorruption = lvl; break;
            }
        }

        // Update the highlight of buttons in a law row
        private static void UpdateRowHighlight(Transform content, string rowName, string activeLevel) 
        {    
            var row = content.Find(rowName);
            if (row != null) {
                var btns = row.Find("ButtonsRow");
                if (btns != null) {
                    foreach (Transform child in btns)
                    {
                        var img = child.GetComponent<Image>();
                        if (img != null)
                        {
                            img.color = child.name.StartsWith(activeLevel.Replace(" ", ""), StringComparison.OrdinalIgnoreCase)
                                ? new Color(0.6f, 0.9f, 0.4f, 1f) 
                                : new Color(0.2f, 0.2f, 0.25f, 1f);
                        }
                    }
                }
            }
        }

        // Generate tooltip text for a law level
        private static string GetLawTooltip(string lawId, string level)
        {
            var k = Main.selectedKingdom;
            if (k == null) return "";
            
            var d = KingdomMetricsSystem.Get(k);
            float pct = 0f;
            string lvl = level.ToLowerInvariant();
            string id  = lawId.ToLowerInvariant();
            
            // 1) Calculate Cost Percentage
            if (id != "taxation") 
            {
                // Default structure
                if (lvl == "none") pct = 0f;
                else if (lvl == "low") pct = 0.05f;
                else if (lvl == "medium") pct = 0.15f;
                else if (lvl == "high") pct = 0.25f;
                else if (lvl == "maximum") pct = 0.35f;
                
                // Specific overrides
                if (id == "military_spending") 
                    pct = lvl switch { "none"=>0f, "low"=>0.10f, "medium"=>0.25f, "high"=>0.40f, "maximum"=>0.60f, _=>0f };
                else if (id == "security_spending")
                    pct = lvl switch { "none"=>0f, "low"=>0.05f, "medium"=>0.15f, "high"=>0.25f, "maximum"=>0.35f, _=>0f };
                else if (id == "anti_corruption")
                    pct = lvl switch { "none"=>0f, "low"=>0.10f, "medium"=>0.20f, "high"=>0.30f, "maximum"=>0.40f, _=>0f };
                else if (id == "research_spending")
                    pct = lvl switch { "none"=>0f, "low"=>0.10f, "medium"=>0.20f, "high"=>0.30f, "maximum"=>0.40f, _=>0f };
            }

            // Cost String
            long costVal = (long)(d.Income * pct);
            string costStr;
            if (id == "taxation")
            {
                 costStr = "Effect on Income";
            }
            else
            {
                 if (pct <= 0f) costStr = "No Upkeep Cost";
                 else costStr = $"Cost: ${costVal} ({pct*100:0.##}% of Income)";
            }
            string effects = "Effects:\n";
            
            // Effects Strings
            switch(id) 
            {
                case "taxation":
                    effects += lvl switch {
                        "minimum" => "Income: -60%\nStability: +20.0\nWE Gain: -10%",
                        "low"     => "Income: -30%\nStability: +10.0\nWE Gain: -5%",
                        "normal"  => "Income: Normal\nStability: -5.0\nWE Gain: Normal",
                        "high"    => "Income: +30%\nStability: -10.0\nWE Gain: +3%",
                        "maximum" => "Income: +75%\nStability: -20.0\nWE Gain: +10%",
                        _ => ""
                    };
                    break;

                case "military_spending":
                    effects += lvl switch {
                        "none"    => "Base (No active spending)\nManpower Recovery: Very Slow (1.5%/m)",
                        "low"     => "Manpower: x1.4\nRecovery: Slow (3.0%/m)\nStability: +7.5\nWE Gain: +20%",
                        "medium"  => "Manpower: x1.6\nRecovery: Moderate (5.0%/m)\nStability: +10.0\nWE Gain: +30%",
                        "high"    => "Manpower: x1.8\nRecovery: Fast (7.5%/m)\nStability: +12.5\nWE Gain: +35%",
                        "maximum" => "Manpower: x2.0\nRecovery: Very Fast (10.0%/m)\nStability: +15.0\nWE Gain: +40%",
                        _ => ""
                    };
                    break;

                case "security_spending":
                    effects += lvl switch {
                        "none"    => "Base (No active spending)",
                        "low"     => "Soldiers/City: +10\nStability: -15.0\nWE Gain: +1.5%\nCorruption: +4%",
                        "medium"  => "Soldiers/City: +15\nStability: -10.0\nWE Gain: +1%\nCorruption: +6%",
                        "high"    => "Soldiers/City: +20\nStability: -5.0\nWE Gain: +0.5%\nCorruption: +8%",
                        "maximum" => "Soldiers/City: +25\nStability: Normal\nWE Gain: +10%\nCorruption: +10%",
                        _ => ""
                    };
                    break;

                case "anti_corruption":
                    effects += lvl switch {
                        "none"    => "Base (No active measures)",
                        "low"     => "Corruption: -20%\nStability: +12.0",
                        "medium"  => "Corruption: -30%\nStability: +18.0",
                        "high"    => "Corruption: -40%\nStability: +24.0",
                        "maximum" => "Corruption: -50%\nStability: +30.0",
                        _ => ""
                    };
                    break;

                case "government_spending":
                    effects += lvl switch {
                        "none"    => "Base (No active spending)",
                        "low"     => "City Efficiency: -5%\nStability: -4.0",
                        "medium"  => "City Efficiency: Normal\nCorruption: -10%",
                        "high"    => "City Efficiency: +10%\nStability: +10.0\nCorruption: -20%",
                        "maximum" => "City Efficiency: +20%\nStability: +20.0\nCorruption: -30%",
                        _ => ""
                    };
                    break;

                case "education_spending":
                    effects += lvl switch {
                        "none"    => "Base (No active spending)",
                        "low"     => "Genius Chance: +2%\nStability: Normal\nWE Gain: -2%",
                        "medium"  => "Genius Chance: +5%\nStability: +10.0\nWE Gain: -4%",
                        "high"    => "Genius Chance: +8%\nStability: +20.0\nWE Gain: -7%",
                        "maximum" => "Genius Chance: +10%\nStability: +30.0\nWE Gain: -10%",
                        _ => ""
                    };
                    break;

                case "welfare_spending": 
                    effects += lvl switch {
                        "none"    => "Base (No active spending)",
                        "low"     => "Plague Resist: 20%\nStability: -5.0",
                        "medium"  => "Plague Resist: 40%\nStability: Normal\nWE Gain: -3%",
                        "high"    => "Plague Resist: 60%\nStability: +5.0\nWE Gain: -6%",
                        "maximum" => "Plague Resist: 80%\nStability: +10.0\nWE Gain: -10%",
                        _ => ""
                    };
                    break;

                case "research_spending":
                    effects += lvl switch {
                        "none"    => "Base (No active spending)",
                        "low"     => "Tech Speed: x2.5\nStability: -5.0",
                        "medium"  => "Tech Speed: x4.0\nStability: Normal\nWE Gain: -4%",
                        "high"    => "Tech Speed: x5.0\nStability: +5.0\nWE Gain: -8%",
                        "maximum" => "Tech Speed: x6.0\nStability: +10.0\nWE Gain: -12%",
                        _ => ""
                    };
                    break;
            }
            return $"<b>{level} {lawId.Replace("_", " ").ToUpper()}</b>\n{costStr}\n\n{effects}";
        }
    }
}