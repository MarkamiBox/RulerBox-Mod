using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace RulerBox
{
    public static class LawsWindow
    {
        private static GameObject root;
        private static Sprite windowInnerSprite;
        
        public static void Initialize(Transform parent)
        {
            if (root != null) return;
            windowInnerSprite = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.windowInnerSliced.png");
            
            // Root container
            root = new GameObject("LawsWindowRoot");
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
            
            // Title
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(root.transform, false);
            var title = titleGO.AddComponent<Text>();
            title.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            title.text = "National Laws";
            title.alignment = TextAnchor.MiddleCenter;
            title.color = Color.white;
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 10;
            title.resizeTextMaxSize = 14;
            titleGO.AddComponent<LayoutElement>().preferredHeight = 24f;
            
            // Scroll container
            var scrollGO = new GameObject("LawsScroll");
            scrollGO.transform.SetParent(root.transform, false);
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
            
            // ScrollRect
            var scrollRect = scrollGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            
            // Viewport
            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(scrollGO.transform, false);
            var viewportRT = viewportGO.AddComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = new Vector2(2f, 4.5f);
            viewportRT.offsetMax = new Vector2(-2f, -4f);
            viewportGO.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
            viewportGO.AddComponent<Mask>().showMaskGraphic = false;
            
            // Content
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRT = contentGO.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot = new Vector2(0.5f, 1f);
            
            // Vertical Layout for content
            var contentVL = contentGO.AddComponent<VerticalLayoutGroup>();
            contentVL.childAlignment = TextAnchor.UpperLeft;
            contentVL.spacing = 4;
            contentVL.padding = new RectOffset(2, 2, 2, 2);
            contentVL.childControlWidth = true;
            contentVL.childControlHeight = true;
            contentVL.childForceExpandWidth = true;
            contentVL.childForceExpandHeight = false;
            
            contentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.viewport = viewportRT;
            scrollRect.content = contentRT;
            
            // --- MILITARY LAWS --- [cite: 23]
            AddCategoryHeader(contentRT, "Military Laws");
            AddLawRow(contentRT, "conscription", "Conscription", "Disarmed", "Volunteer", "Limited", "Extensive", "Required"); // [cite: 23, 100]
            AddLawRow(contentRT, "war_bonds", "War Bonds", "Inactive", "Moderate", "Maximum"); // [cite: 29, 114]
            AddLawRow(contentRT, "elitist_military", "Elitist Stance", "Default", "Expanded"); // [cite: 33, 115]
            
            // --- GOVERNMENTAL LAWS --- [cite: 36]
            AddCategoryHeader(contentRT, "Governmental Laws");
            AddLawRow(contentRT, "party_loyalty", "Party Loyalty", "Minimum", "Low", "Standard", "High", "Maximum"); // [cite: 36, 116]
            AddLawRow(contentRT, "power_sharing", "Centralization", "Decentralized", "Balanced", "Centralized"); // [cite: 41, 118]

            // --- SOCIAL LAWS --- [cite: 43]
            AddCategoryHeader(contentRT, "Social Laws");
            AddLawRow(contentRT, "press_regulation", "Press Regulation", "Free Press", "Laxed", "Mixed", "State Focus", "Propaganda"); // [cite: 43, 122]
            AddLawRow(contentRT, "firearm_regulation", "Firearm Reg.", "No Restr.", "Reduced", "Standard", "Expanded", "Illegal"); // [cite: 47, 123]
            AddLawRow(contentRT, "religious_emphasis", "Religion", "Atheism", "Secularism", "State Rel."); // [cite: 50, 124]
            AddLawRow(contentRT, "population_growth", "Pop. Growth", "Balanced", "Encouraged", "Mandatory"); // [cite: 54, 125]

            // --- ECONOMIC LAWS --- [cite: 56]
            AddCategoryHeader(contentRT, "Economic Laws");
            AddLawRow(contentRT, "industrial_spec", "Industry Spec.", "Extraction", "Balanced", "Manufact."); // [cite: 56, 118]
            AddLawRow(contentRT, "resource_subsidy", "Res. Subsidy", "None", "Limited", "Moderate", "Generous"); // [cite: 57, 119]
            AddLawRow(contentRT, "working_hours", "Working Hours", "Minimum", "Reduced", "Standard", "Extended", "Unlimited"); // [cite: 59, 120]
            AddLawRow(contentRT, "research_focus", "Research Focus", "Civilian", "Balanced", "Military"); // [cite: 63, 121]

            // --- IDEOLOGY LAWS --- [cite: 66]
            AddCategoryHeader(contentRT, "Ideology Laws");
            AddLawRow(contentRT, "monarch", "Monarch", "Ceremonial", "Constitutional", "Absolute"); // [cite: 75, 126]
            AddLawRow(contentRT, "collective_theory", "Collective Theory", "Maoist", "Marxist", "Leninist", "Stalinist", "Trotskyism"); // [cite: 80, 128]
            AddLawRow(contentRT, "elective_assembly", "Elective Assembly", "Direct", "Indirect", "Technocratic"); // [cite: 85, 129]
            AddLawRow(contentRT, "democracy_style", "Democracy Style", "Parliamentary", "Semi-Pres.", "Presidential"); // [cite: 88, 130]
            AddLawRow(contentRT, "state_doctrine", "State Doctrine", "Corporatist", "Classical", "Stratocracy", "Clerical", "Falangism"); // [cite: 91, 131]

            // Bottom Back Button
            var bottomRow = new GameObject("BottomRow");
            bottomRow.transform.SetParent(root.transform, false);
            var bottomHL = bottomRow.AddComponent<HorizontalLayoutGroup>();
            bottomHL.childAlignment = TextAnchor.MiddleCenter;
            bottomRow.AddComponent<LayoutElement>().preferredHeight = 26f;
            
            BuildBackButton(bottomRow.transform, "Back", () => 
            {
                SetVisible(false);
                TopPanelUI.ReturnToEconomyMain();
            });

            root.SetActive(false);
        }

        public static void SetVisible(bool visible)
        {
            if (root != null) root.SetActive(visible);
        }
        
        public static bool IsVisible() => root != null && root.activeSelf;
        
        public static void Refresh(Kingdom k)
        {
            if (!IsVisible() || k == null) return;
            
            var d = KingdomMetricsSystem.Get(k);
            if (d == null) return;
            
            Transform content = root.transform.Find("LawsScroll/Viewport/Content");
            if (content != null)
            {
                UpdateRowHighlight(content, "ConscriptionRow", d.Law_Conscription);
                UpdateRowHighlight(content, "WarBondsRow", d.Law_WarBonds);
                UpdateRowHighlight(content, "ElitistStanceRow", d.Law_ElitistMilitary);
                
                UpdateRowHighlight(content, "PartyLoyaltyRow", d.Law_PartyLoyalty);
                UpdateRowHighlight(content, "CentralizationRow", d.Law_Centralization);
                
                UpdateRowHighlight(content, "PressRegulationRow", d.Law_PressRegulation);
                UpdateRowHighlight(content, "FirearmReg.Row", d.Law_FirearmRegulation);
                UpdateRowHighlight(content, "ReligionRow", d.Law_Religion);
                UpdateRowHighlight(content, "Pop.GrowthRow", d.Law_PopulationGrowth);
                
                UpdateRowHighlight(content, "IndustrySpec.Row", d.Law_IndustrialSpec);
                UpdateRowHighlight(content, "Res.SubsidyRow", d.Law_ResourceSubsidy);
                UpdateRowHighlight(content, "WorkingHoursRow", d.Law_WorkingHours);
                UpdateRowHighlight(content, "ResearchFocusRow", d.Law_ResearchFocus);

                UpdateRowHighlight(content, "MonarchRow", d.Law_Monarch);
                UpdateRowHighlight(content, "CollectiveTheoryRow", d.Law_CollectiveTheory);
                UpdateRowHighlight(content, "ElectiveAssemblyRow", d.Law_ElectiveAssembly);
                UpdateRowHighlight(content, "DemocracyStyleRow", d.Law_DemocracyStyle);
                UpdateRowHighlight(content, "StateDoctrineRow", d.Law_StateDoctrine);
            }
        }

        private static void AddCategoryHeader(Transform parent, string text)
        {
            var go = new GameObject("Header_" + text);
            go.transform.SetParent(parent, false);
            var txt = go.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = text;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.color = new Color(1f, 0.8f, 0.2f);
            txt.fontStyle = FontStyle.Bold;
            txt.fontSize = 10;
            go.AddComponent<LayoutElement>().preferredHeight = 14f;
        }

        private static void AddLawRow(Transform parent, string lawId, string displayName, params string[] levels)
        {
            var rowGO = new GameObject(displayName.Replace(" ", "") + "Row");
            rowGO.transform.SetParent(parent, false);
            
            var v = rowGO.AddComponent<VerticalLayoutGroup>();
            v.childAlignment = TextAnchor.UpperLeft;
            v.spacing = 2;
            v.childControlWidth = true;
            v.childControlHeight = true;
            
            var rowLE = rowGO.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 30f; // Will grow with content usually
            
            // Label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(rowGO.transform, false);
            var label = labelGO.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.text = displayName;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = Color.white;
            label.fontSize = 9;
            labelGO.AddComponent<LayoutElement>().preferredHeight = 12f;
            
            // Buttons Row
            var buttonsRow = new GameObject("ButtonsRow");
            buttonsRow.transform.SetParent(rowGO.transform, false);
            var h = buttonsRow.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleLeft;
            h.spacing = 1;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = true;
            
            buttonsRow.AddComponent<LayoutElement>().preferredHeight = 16f;
            
            foreach (var level in levels)
            {
                CreateLawButton(buttonsRow.transform, lawId, level);
            }
        }

        private static void CreateLawButton(Transform parent, string lawId, string level)
        {
            var go = new GameObject(level + "Btn");
            go.transform.SetParent(parent, false);
            
            var img = go.AddComponent<Image>();
            img.sprite = windowInnerSprite;
            img.type = Image.Type.Sliced;
            img.color = new Color(0.2f, 0.2f, 0.25f, 1f);
            
            go.AddComponent<LayoutElement>().flexibleWidth = 1f;
            
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() =>
            {
                var k = Main.selectedKingdom;
                if (k == null) return;
                var d = KingdomMetricsSystem.Get(k);
                SetActiveLaw(d, lawId, level);
                Refresh(k);
                KingdomMetricsSystem.RecalculateForKingdom(k, d); // Force recalc immediately
                TopPanelUI.Refresh(); 
            });
            
            var txtGO = new GameObject("Text");
            txtGO.transform.SetParent(go.transform, false);
            var txt = txtGO.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = level;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.fontSize = 6;
            txt.resizeTextForBestFit = true;
            txt.resizeTextMinSize = 4;
            txt.resizeTextMaxSize = 8;
            
            var rt = txt.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            
            ChipTooltips.AttachSimpleTooltip(go, () => GetLawTooltip(lawId, level));
        }

        private static void SetActiveLaw(KingdomMetricsSystem.Data d, string lawId, string level)
        {
            switch(lawId)
            {
                case "conscription": d.Law_Conscription = level; break;
                case "war_bonds": d.Law_WarBonds = level; break;
                case "elitist_military": d.Law_ElitistMilitary = level; break;
                case "party_loyalty": d.Law_PartyLoyalty = level; break;
                case "power_sharing": d.Law_Centralization = level; break;
                case "press_regulation": d.Law_PressRegulation = level; break;
                case "firearm_regulation": d.Law_FirearmRegulation = level; break;
                case "religious_emphasis": d.Law_Religion = level; break;
                case "population_growth": d.Law_PopulationGrowth = level; break;
                case "industrial_spec": d.Law_IndustrialSpec = level; break;
                case "resource_subsidy": d.Law_ResourceSubsidy = level; break;
                case "working_hours": d.Law_WorkingHours = level; break;
                case "research_focus": d.Law_ResearchFocus = level; break;
                case "monarch": d.Law_Monarch = level; break;
                case "collective_theory": d.Law_CollectiveTheory = level; break;
                case "elective_assembly": d.Law_ElectiveAssembly = level; break;
                case "democracy_style": d.Law_DemocracyStyle = level; break;
                case "state_doctrine": d.Law_StateDoctrine = level; break;
            }
        }

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
                            // Simple check if button text matches active level
                            var txt = child.GetComponentInChildren<Text>();
                            // Special handling for abbreviated button text if needed, currently exact match
                            bool match = (txt != null && txt.text == activeLevel) || child.name.StartsWith(activeLevel);
                            
                            img.color = match ? new Color(0.6f, 0.9f, 0.4f, 1f) : new Color(0.2f, 0.2f, 0.25f, 1f);
                        }
                    }
                }
            }
        }
        
        private static void BuildBackButton(Transform parent, string label, Action onClick)
        {
            var go = new GameObject("BackBtn");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = windowInnerSprite;
            img.type = Image.Type.Sliced;
            
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 80; le.preferredHeight = 20;
            
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());
            
            var txtGO = new GameObject("Text");
            txtGO.transform.SetParent(go.transform, false);
            var txt = txtGO.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = label;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.fontSize = 8;
            
            var rt = txt.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static string GetLawTooltip(string lawId, string level)
        {
            // Tooltips based on aaa.txt descriptions
            string desc = "";
            switch(lawId)
            {
                case "conscription":
                    if(level=="Disarmed") desc = "Manpower -50%, Tax +5% [cite: 109]";
                    if(level=="Volunteer") desc = "Default [cite: 110]";
                    if(level=="Limited") desc = "Manpower +50%, Tax -10%, Build -10% [cite: 112]";
                    if(level=="Extensive") desc = "Manpower +100%, Tax -25%, Build -25% [cite: 113]";
                    if(level=="Required") desc = "Manpower +150%, Tax -65%, Build -50% [cite: 114]";
                    break;
                case "war_bonds":
                    if(level=="Inactive") desc = "Default [cite: 114]";
                    if(level=="Moderate") desc = "Tax x1.5, Stability -8% [cite: 115]";
                    if(level=="Maximum") desc = "Tax x2.25, Stability -15% [cite: 115]";
                    break;
                case "elitist_military":
                    if(level=="Default") desc = "No effect [cite: 116]";
                    if(level=="Expanded") desc = "Military Power +25%, Corruption +0.1% [cite: 116]";
                    break;
                case "party_loyalty":
                    if(level=="Minimum") desc = "Tax x1.1, Ideology -15% [cite: 117]";
                    if(level=="Standard") desc = "Default [cite: 117]";
                    if(level=="Maximum") desc = "Tax x0.9, Ideology +25% [cite: 117]";
                    break;
                case "power_sharing":
                    if(level=="Decentralized") desc = "Tax +5% [cite: 118]";
                    if(level=="Centralized") desc = "Stability +2.5% [cite: 118]";
                    break;
                case "press_regulation":
                    if(level=="Free Press") desc = "Tax x1.1, Corruption -0.1 [cite: 122]";
                    if(level=="Propaganda") desc = "Stability +10%, Tax x0.9 [cite: 123]";
                    break;
                case "firearm_regulation":
                    if(level=="No Restr.") desc = "Tax x1.13, Stability -5% [cite: 124]";
                    if(level=="Illegal") desc = "Stability +15%, Tax 0% [cite: 124]";
                    break;
                case "religious_emphasis":
                    if(level=="Atheism") desc = "Tax x1.05, Stability Normal [cite: 125]";
                    if(level=="State Rel.") desc = "Stability +5%, Tax x0.95 [cite: 125]";
                    break;
                case "population_growth":
                    if(level=="Encouraged") desc = "Growth +2.5%, Tax x0.85 [cite: 126]";
                    if(level=="Mandatory") desc = "Growth +5%, Tax x0.7 [cite: 126]";
                    break;
                case "working_hours":
                    if(level=="Minimum") desc = "Stability +10%, Tax x0.75 [cite: 120]";
                    if(level=="Unlimited") desc = "Stability -15%, Tax x1.5 [cite: 120]";
                    break;
            }
            return $"<b>{level}</b>\n{desc}";
        }
    }
}