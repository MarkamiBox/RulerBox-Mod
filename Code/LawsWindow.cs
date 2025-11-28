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
            
            // --- LAW CATEGORIES ---

            // Military
            AddCategoryHeader(contentRT, "Military Laws");
            AddLawRow(contentRT, "conscription", "Conscription", "Disarmed", "Volunteer", "Limited", "Extensive", "Required");
            AddLawRow(contentRT, "war_bonds", "War Bonds", "Inactive", "Moderate", "Maximum");
            
            // Governmental
            AddCategoryHeader(contentRT, "Governmental Laws");
            AddLawRow(contentRT, "party_loyalty", "Party Loyalty", "Minimum", "Low", "Standard", "High", "Maximum");
            // Power Sharing is unique to Iberian Union in doc, excluding for generic or adding generic version? Added as generic.
            AddLawRow(contentRT, "power_sharing", "Centralization", "Decentralized", "Balanced", "Centralized");

            // Social
            AddCategoryHeader(contentRT, "Social Laws");
            AddLawRow(contentRT, "press_regulation", "Press Regulation", "Free Press", "Laxed", "Mixed", "State Focus", "Propaganda");
            AddLawRow(contentRT, "firearm_regulation", "Firearm Reg.", "No Restr.", "Reduced", "Standard", "Expanded", "Illegal");
            AddLawRow(contentRT, "religious_emphasis", "Religion", "Atheism", "Secularism", "State Rel.");
            AddLawRow(contentRT, "population_growth", "Pop. Growth", "Balanced", "Encouraged", "Mandatory");

            // Economic
            AddCategoryHeader(contentRT, "Economic Laws");
            AddLawRow(contentRT, "industrial_spec", "Industry Spec.", "Extraction", "Balanced", "Manufact.");
            AddLawRow(contentRT, "resource_subsidy", "Res. Subsidy", "None", "Limited", "Moderate", "Generous");
            AddLawRow(contentRT, "working_hours", "Working Hours", "Minimum", "Reduced", "Standard", "Extended", "Unlimited");
            AddLawRow(contentRT, "research_focus", "Research Focus", "Civilian", "Balanced", "Military");

            // Bottom Back Button
            var bottomRow = new GameObject("BottomRow");
            bottomRow.transform.SetParent(root.transform, false);
            var bottomHL = bottomRow.AddComponent<HorizontalLayoutGroup>();
            bottomHL.childAlignment = TextAnchor.MiddleCenter;
            bottomRow.AddComponent<LayoutElement>().preferredHeight = 26f;
            
            BuildBackButton(bottomRow.transform, "Back", () => 
            {
                SetVisible(false);
                // Assuming we go back to Diplomacy
                DiplomacyWindow.SetVisible(true);
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
                UpdateRowHighlight(content, "ConscriptionRow", d.Law_PowerSharing);
                UpdateRowHighlight(content, "WarBondsRow", d.Law_WarBonds);
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
                case "conscription": d.Law_PowerSharing = level; break;
                case "war_bonds": d.Law_WarBonds = level; break;
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
            // Basic tooltips derived from the provided text. 
            // Implementation details are simplified for brevity.
            string desc = "";
            switch(lawId)
            {
                case "conscription":
                    if(level=="Disarmed") desc = "Manpower -50%, Tax +5%";
                    if(level=="Volunteer") desc = "Default";
                    if(level=="Limited") desc = "Manpower +50%, Tax -10%";
                    if(level=="Extensive") desc = "Manpower +100%, Tax -25%";
                    if(level=="Required") desc = "Manpower +150%, Tax -65%";
                    break;
                case "war_bonds":
                    if(level=="Inactive") desc = "Default";
                    if(level=="Moderate") desc = "Tax +50%, Stability -8";
                    if(level=="Maximum") desc = "Tax +125%, Stability -15";
                    break;
                case "party_loyalty":
                    if(level=="Minimum") desc = "Tax +10%";
                    if(level=="Standard") desc = "Default";
                    if(level=="Maximum") desc = "Tax -10%";
                    break;
                case "power_sharing":
                    if(level=="Decentralized") desc = "Tax +5%";
                    if(level=="Centralized") desc = "Stability +2.5%";
                    break;
                case "press_regulation":
                    if(level=="Free Press") desc = "Tax +10%";
                    if(level=="Propaganda") desc = "Stability +10, Tax -10%";
                    break;
                case "firearm_regulation":
                    if(level=="No Restr.") desc = "Tax +13%, Stability -5";
                    if(level=="Illegal") desc = "Stability +15, Tax -60%";
                    break;
                case "religious_emphasis":
                    if(level=="Atheism") desc = "Tech +5%";
                    if(level=="State Rel.") desc = "Stability +5%, Tax -5%";
                    break;
                case "population_growth":
                    if(level=="Encouraged") desc = "Growth +2.5%, Tax -10%";
                    if(level=="Mandatory") desc = "Growth +5%, Tax -20%";
                    break;
                case "working_hours":
                    if(level=="Minimum") desc = "Stability +10, Tax -25%";
                    if(level=="Unlimited") desc = "Stability -15, Tax +50%";
                    break;
            }
            return $"<b>{level}</b>\n{desc}";
        }
    }
}