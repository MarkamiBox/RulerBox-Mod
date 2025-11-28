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
            contentVL.padding = new RectOffset(30, 30, 4, 4); // Increased padding to fix stretching
            contentVL.childControlWidth = true;
            contentVL.childControlHeight = true;
            contentVL.childForceExpandWidth = true;
            contentVL.childForceExpandHeight = false;
            
            contentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.viewport = viewportRT;
            scrollRect.content = contentRT;
            
            // --- MILITARY LAWS ---
            AddCategoryHeader(contentRT, "Military Laws");
            AddLawRow(contentRT, "conscription", "Conscription", "Disarmed", "Volunteer", "Limited", "Extensive", "Required"); 
            AddLawRow(contentRT, "war_bonds", "War Bonds", "Inactive", "Moderate", "Maximum"); 
            AddLawRow(contentRT, "elitist_military", "Elitist Stance", "Default", "Expanded"); 
            
            // --- GOVERNMENTAL LAWS ---
            AddCategoryHeader(contentRT, "Governmental Laws");
            AddLawRow(contentRT, "party_loyalty", "Party Loyalty", "Minimum", "Low", "Standard", "High", "Maximum"); 
            AddLawRow(contentRT, "power_sharing", "Centralization", "Decentralized", "Balanced", "Centralized"); 

            // --- SOCIAL LAWS ---
            AddCategoryHeader(contentRT, "Social Laws");
            AddLawRow(contentRT, "press_regulation", "Press Regulation", "Free Press", "Laxed", "Mixed", "State Focus", "Propaganda"); 
            AddLawRow(contentRT, "firearm_regulation", "Firearm Reg.", "No Restr.", "Reduced", "Standard", "Expanded", "Illegal"); 
            AddLawRow(contentRT, "religious_emphasis", "Religion", "Atheism", "Secularism", "State Rel."); 
            AddLawRow(contentRT, "population_growth", "Pop. Growth", "Balanced", "Encouraged", "Mandatory"); 

            // --- ECONOMIC LAWS ---
            AddCategoryHeader(contentRT, "Economic Laws");
            AddLawRow(contentRT, "industrial_spec", "Industry Spec.", "Extraction", "Balanced", "Manufact."); 
            AddLawRow(contentRT, "resource_subsidy", "Res. Subsidy", "None", "Limited", "Moderate", "Generous"); 
            AddLawRow(contentRT, "working_hours", "Working Hours", "Minimum", "Reduced", "Standard", "Extended", "Unlimited"); 
            AddLawRow(contentRT, "research_focus", "Research Focus", "Civilian", "Balanced", "Military"); 

            // --- IDEOLOGY LAWS ---
            AddCategoryHeader(contentRT, "Ideology Laws");
            AddLawRow(contentRT, "monarch", "Monarch", "Ceremonial", "Constitutional", "Absolute"); 
            AddLawRow(contentRT, "collective_theory", "Collective Theory", "Maoist", "Marxist", "Leninist", "Stalinist", "Trotskyism"); 
            AddLawRow(contentRT, "elective_assembly", "Elective Assembly", "Direct", "Indirect", "Technocratic"); 
            AddLawRow(contentRT, "democracy_style", "Democracy Style", "Parliamentary", "Semi-Pres.", "Presidential"); 
            AddLawRow(contentRT, "state_doctrine", "State Doctrine", "Corporatist", "Classical", "Stratocracy", "Clerical", "Falangism"); 

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
            rowLE.preferredHeight = 32f;
            rowLE.flexibleWidth = 1f;
            
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
            
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.preferredHeight = 18f;
            
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() =>
            {
                var k = Main.selectedKingdom;
                if (k == null) return;
                var d = KingdomMetricsSystem.Get(k);
                SetActiveLaw(d, lawId, level);
                KingdomMetricsSystem.RecalculateForKingdom(k, d);
                Refresh(k);
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
                            var txt = child.GetComponentInChildren<Text>();
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
            img.color = Color.white;
            
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 80; 
            le.preferredHeight = 20;
            
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
            string desc = "";
            switch(lawId)
            {
                case "conscription":
                    if(level=="Disarmed") desc = "Manpower Cap -50%, Gain -50%\nTax +5%";
                    else if(level=="Volunteer") desc = "Default";
                    else if(level=="Limited") desc = "Manpower Cap +50%, Gain +48%\nTax -10%, Build Speed -10%\nRecruit Time +25%";
                    else if(level=="Extensive") desc = "Manpower Cap +100%, Gain +99%\nTax -25%, Build Speed -25%\nRecruit Time +50%";
                    else if(level=="Required") desc = "Manpower Cap +150%, Gain +150%\nTax -65%, Build Speed -50%\nRecruit Time +75%";
                    break;
                case "war_bonds":
                    if(level=="Inactive") desc = "Default";
                    else if(level=="Moderate") desc = "Tax x1.5\nStability -8%, War Exhaustion +0.05\nMil. Upkeep x0.75";
                    else if(level=="Maximum") desc = "Tax x2.25\nStability -15%, War Exhaustion +0.15\nMil. Upkeep x0.5";
                    break;
                case "elitist_military":
                    if(level=="Default") desc = "No effect";
                    else if(level=="Expanded") desc = "Military Power Gain +25%\nCorruption +0.1";
                    break;
                case "party_loyalty":
                    if(level=="Minimum") desc = "Tax x1.1, PP Gain +25%\nIdeology Power -15%";
                    else if(level=="Low") desc = "PP Gain +15%\nIdeology Power -5%";
                    else if(level=="Standard") desc = "Default";
                    else if(level=="High") desc = "Ideology Power +10%\nPP Gain -10%";
                    else if(level=="Maximum") desc = "Tax x0.9, PP Gain -20%\nIdeology Power +25%";
                    break;
                case "power_sharing":
                    if(level=="Decentralized") desc = "Tax +5%, Unrest Reduction +5%";
                    else if(level=="Balanced") desc = "Default";
                    else if(level=="Centralized") desc = "Stability +2.5%";
                    break;
                case "press_regulation":
                    if(level=="Free Press") desc = "Tax x1.1, Corruption -0.1\nIntegration x1.25\nUnrest Reduction -25%";
                    else if(level=="Laxed") desc = "Tax x1.05, Corruption -0.05\nIntegration x1.1\nUnrest Reduction -10%";
                    else if(level=="Mixed") desc = "Tax x0.95, Stability +5\nIntegration x0.9\nUnrest Reduction +15%";
                    else if(level=="State Focus") desc = "Tax x0.9, Stability +10\nIntegration x0.85\nUnrest Reduction +25%";
                    else if(level=="Propaganda") desc = "Tax x0.9, Stability +10\nWE Gain -0.03\nUnrest Reduction +25%";
                    break;
                case "firearm_regulation":
                    if(level=="No Restr.") desc = "Tax x1.13, City Resistance +50%\nStability -5, Unrest Red. -40%";
                    else if(level=="Reduced") desc = "Tax x1.05, City Resistance +25%\nStability -2.5, Unrest Red. -20%";
                    else if(level=="Standard") desc = "Default";
                    else if(level=="Expanded") desc = "Stability +5, Unrest Red. +15%\nCity Resistance -33%";
                    else if(level=="Illegal") desc = "Stability +15, Unrest Red. +40%\nCity Resistance -60%";
                    break;
                case "religious_emphasis":
                    if(level=="Atheism") desc = "Tax +5%, Ideology +5%\nResearch Output +10%\nUnrest Reduction -10%";
                    else if(level=="Secularism") desc = "Default";
                    else if(level=="State Rel.") desc = "Stability +5%, Manpower +10%\nUnrest Red. +10%, WE Gain -0.005\nIdeology -5%, Research -10%";
                    break;
                case "population_growth":
                    if(level=="Balanced") desc = "Default";
                    else if(level=="Encouraged") desc = "Pop Growth +2.5%\nTax -15%, Factory/Res Output -15%";
                    else if(level=="Mandatory") desc = "Pop Growth +5%\nTax -30%, Factory/Res Output -30%";
                    break;
                case "industrial_spec":
                    if(level=="Extraction") desc = "Resource Output +25%\nFactory Output -25%\nTax -20%";
                    else if(level=="Balanced") desc = "Default";
                    else if(level=="Manufact.") desc = "Factory Output +25%\nResource Output -25%\nTax -20%";
                    break;
                case "resource_subsidy":
                    if(level=="None") desc = "Default";
                    else if(level=="Limited") desc = "Trade Income x2\nTax -10%, Output -15%";
                    else if(level=="Moderate") desc = "Trade Income x3.5\nTax -15%, Output -25%";
                    else if(level=="Generous") desc = "Trade Income x5\nTax -25%, Output -30%";
                    break;
                case "working_hours":
                    if(level=="Minimum") desc = "Stability +10, Unrest Red. +50%\nTax -25%, Output -25%";
                    else if(level=="Reduced") desc = "Stability +5, Unrest Red. +30%\nTax -15%, Output -10%";
                    else if(level=="Standard") desc = "Default";
                    else if(level=="Extended") desc = "Tax +25%, Output +10%\nStability -5, Unrest Red. -25%";
                    else if(level=="Unlimited") desc = "Tax +50%, Output +30%\nStability -15, Unrest Red. -60%";
                    break;
                case "research_focus":
                    if(level=="Civilian") desc = "Econ/Pol Research +30%\nRaw Research -15%\nResearch Cost +25%";
                    else if(level=="Balanced") desc = "Default";
                    else if(level=="Military") desc = "Mil Research +30%\nRaw Research -15%\nResearch Cost +25%";
                    break;
                case "monarch":
                    if(level=="Ceremonial") desc = "Default";
                    else if(level=="Constitutional") desc = "Stability +5, Manpower +10%\nPP Gain +20%, Tax +15%\nIdeology -25%, Unrest Red. -20%";
                    else if(level=="Absolute") desc = "Stability +10, Manpower +20%\nPP Gain +20%, Tax +15%\nIdeology -50%, Unrest Red. -30%";
                    break;
                case "collective_theory":
                    if(level=="Maoist") desc = "Resource Output +25%\nTax -7.5%";
                    else if(level=="Marxism") desc = "Factory Output +15%\nTax -7.5%";
                    else if(level=="Leninist") desc = "PP Gain +15%\nTax -7.5%";
                    else if(level=="Stalinist") desc = "Stability +5, Unrest Red. +10%\nCorruption +0.02\nTax -7.5%";
                    else if(level=="Trotskyism") desc = "WE Gain -0.02, Justification -15%\nTax -7.5%";
                    break;
                case "elective_assembly":
                    if(level=="Direct") desc = "Tax +35%\nPP Gain -5%";
                    else if(level=="Indirect") desc = "Leader XP +5";
                    else if(level=="Technocratic") desc = "Research Efficiency +20%\nSpending Cost -10%";
                    break;
                case "democracy_style":
                    if(level=="Parliamentary") desc = "Leader XP +15\nRecruit Pool +10";
                    else if(level=="Semi-Pres.") desc = "Invest. Availability +25%\nInvest. Cost -5%";
                    else if(level=="Presidential") desc = "PP Gain +15%\nStability +5";
                    break;
                case "state_doctrine":
                    if(level=="Corporatist") desc = "Factory Output +25%";
                    else if(level=="Classical") desc = "Aligned Leaders +50%\nBase XP -5";
                    else if(level=="Stratocracy") desc = "Manpower +20%";
                    else if(level=="Clerical") desc = "Pop Growth +0.5%\nWE Gain -0.0025";
                    else if(level=="Falangism") desc = "Build Speed +10%, Unrest Red. +15%\nJustification +10%";
                    break;
            }
            return $"<b>{level}</b>\n{desc}";
        }
    }
}