using UnityEngine;
using UnityEngine.UI;
using System;

namespace RulerBox
{
    public static class TopPanelUI
    {
        private static GameObject root;
        private static GameObject contentContainer;

        private static Button diplomacyTabBtn;
        private static Button economyTabBtn;
        private static Button technologyTabBtn;

        private static Image bgImage;
        private static Sprite mainHubSprite;
        private static Sprite techHubSprite;

        public enum HubTab { Diplomacy, Economy, Technology }
        private static HubTab currentTab = HubTab.Economy;

        private static bool showEconomicLaws = false;
        private static bool showInvestments = false; 
        
        public static void Initialize()
        {
            if (root != null) return;
            Debug.Log("[RulerBox] TopPanelUI: Initializing...");

            try
            {
                root = new GameObject("RulerBox_MainHub");
                root.transform.SetParent(DebugConfig.instance?.transform, false);

                bgImage = root.AddComponent<Image>();
                bgImage.sprite = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.MainHub.png");
                bgImage.type = Image.Type.Sliced;
                bgImage.color = Color.white;

                var rt = root.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1.05f, 0.7f);
                rt.anchorMax = new Vector2(1.05f, 0.7f);
                rt.pivot     = new Vector2(1.05f, 0.7f);
                rt.anchoredPosition = new Vector2(-20f, 0f);
                rt.sizeDelta = new Vector2(230, 230);

                CreateTabs(root.transform);

                var containerGO = new GameObject("ContentContainer");
                containerGO.transform.SetParent(root.transform, false);
                contentContainer = containerGO;

                var contentRT = containerGO.AddComponent<RectTransform>();
                contentRT.anchorMin = new Vector2(0f, 0f);
                contentRT.anchorMax = new Vector2(1f, 1f);
                contentRT.offsetMin = new Vector2(25f, 23f);
                contentRT.offsetMax = new Vector2(-25f, -45f);

                // Initialize Sub-Windows
                InitSubWindow("EconomyWindow", () => EconomyWindow.Initialize(contentContainer.transform));
                InitSubWindow("EconomicLaws", () => EconomicLawsWindow.Initialize(contentContainer.transform));
                InitSubWindow("LawsWindow", () => LawsWindow.Initialize(contentContainer.transform));
                InitSubWindow("Investments", () => InvestmentsWindow.Initialize(contentContainer.transform)); 
                InitSubWindow("Diplomacy", () => DiplomacyWindow.Initialize(contentContainer.transform));
                InitSubWindow("DiplomacyActions", () => DiplomacyActionsWindow.Initialize(contentContainer.transform));
                InitSubWindow("Technology", () => TechnologyWindow.Initialize(contentContainer.transform));
                InitSubWindow("Policies", () => PoliciesWindow.Initialize(contentContainer.transform));                
                InitSubWindow("LeadersWindow", () => LeadersWindow.Initialize(contentContainer.transform));

                SetTab(HubTab.Economy);
                root.SetActive(false);
                Debug.Log("[RulerBox] TopPanelUI: Initialization Complete.");
            }
            catch (Exception e)
            {
                Debug.LogError("[RulerBox] TopPanelUI CRITICAL FAIL: " + e.ToString());
            }
        }

        private static void InitSubWindow(string name, Action initMethod)
        {
            try {
                Debug.Log($"[RulerBox] Initializing SubWindow: {name}...");
                initMethod();
            } catch (Exception e) {
                Debug.LogError($"[RulerBox] Failed to initialize {name}: {e.Message}");
            }
        }

        // ... [Rest of the file remains unchanged] ...
        private static HorizontalLayoutGroup tabLayoutGroup;
        private static RectTransform tabsRect;

        // Create Tab Buttons
        private static void CreateTabs(Transform parent)
        {
            var tabsRow = new GameObject("TabsRow");
            tabsRow.transform.SetParent(parent, false);
            tabsRect = tabsRow.AddComponent<RectTransform>();
            tabsRect.anchorMin = new Vector2(0f, 1f);
            tabsRect.anchorMax = new Vector2(1f, 1f);
            tabsRect.pivot     = new Vector2(0.6f, 1f);
            tabsRect.offsetMin = new Vector2(9f, -34f);
            tabsRect.offsetMax = new Vector2(-9f, -24f);

            var tabsImg = tabsRow.AddComponent<Image>();
            tabsImg.color = new Color(1f, 0f, 0f, 0f); 

            tabLayoutGroup = tabsRow.AddComponent<HorizontalLayoutGroup>();
            tabLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
            tabLayoutGroup.spacing = 0f;
            tabLayoutGroup.padding = new RectOffset(8, 8, 0, 0);
            tabLayoutGroup.childControlWidth = true;
            tabLayoutGroup.childControlHeight = true;
            tabLayoutGroup.childForceExpandWidth = false;

            diplomacyTabBtn = BuildTabButton(tabsRow.transform, "Diplomacy", () => OnTabClicked(HubTab.Diplomacy));
            economyTabBtn   = BuildTabButton(tabsRow.transform, "Economy",   () => OnTabClicked(HubTab.Economy));
            technologyTabBtn= BuildTabButton(tabsRow.transform, "Technology", () => OnTabClicked(HubTab.Technology));
        }

        public static void OnTabClicked(HubTab tab)
        {
            if (tab == HubTab.Economy && currentTab == HubTab.Economy)
            {
                CloseAllWindows();
                return;
            }
            SetTab(tab);
            Refresh();
        }

        private static void SetTab(HubTab tab)
        {
            currentTab = tab;
            showEconomicLaws = false;
            showInvestments = false;

            DiplomacyWindow.SetVisible(false);
            DiplomacyActionsWindow.SetVisible(false);
            TechnologyWindow.SetVisible(false);
            EconomyWindow.SetVisible(false);
            EconomicLawsWindow.SetVisible(false);
            RankingsWindow.SetVisible(false);
            InvestmentsWindow.SetVisible(false);
            ResourcesTradeWindow.SetVisible(false); 
            TradeWindow.SetVisible(false);
            PoliciesWindow.SetVisible(false);
            LawsWindow.SetVisible(false);
            LeadersWindow.SetVisible(false);

            var selectedColor   = new Color(0.25f, 0.25f, 0.3f, 0.01f);
            var unselectedColor = new Color(0.15f, 0.15f, 0.2f, 0.01f);

            if (diplomacyTabBtn) diplomacyTabBtn.GetComponent<Image>().color = unselectedColor;
            if (economyTabBtn)   economyTabBtn.GetComponent<Image>().color   = unselectedColor;
            if (technologyTabBtn)technologyTabBtn.GetComponent<Image>().color= unselectedColor;

            var rt = root.GetComponent<RectTransform>();
            if (tab == HubTab.Technology)
            {
                // Ensure sprite is loaded
                if (techHubSprite == null)
                {
                    techHubSprite = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.TechHub.png");
                }
                if (bgImage != null && techHubSprite != null) 
                {
                    bgImage.sprite = techHubSprite;
                    bgImage.color = Color.white; 
                }
                
                // Increase size for Tech Window
                // TWEAK HERE: Width, Height
                rt.sizeDelta = new Vector2(400, 250); 
                
                UpdateButtonLayout(true);
            }
            else
            {
                if (mainHubSprite == null) mainHubSprite = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.MainHub.png");
                if (bgImage != null && mainHubSprite != null) 
                {
                    bgImage.sprite = mainHubSprite;
                }
                // Reset to default size
                rt.sizeDelta = new Vector2(230, 230);
                
                UpdateButtonLayout(false);
            }

            switch (tab)
            {
                case HubTab.Diplomacy:
                    DiplomacyWindow.SetVisible(true);
                    if (diplomacyTabBtn) diplomacyTabBtn.GetComponent<Image>().color = selectedColor;
                    break;
                case HubTab.Economy:
                    EconomyWindow.SetVisible(true);
                    if (economyTabBtn) economyTabBtn.GetComponent<Image>().color = selectedColor;
                    break;
                case HubTab.Technology:
                    TechnologyWindow.SetVisible(true);
                    if (technologyTabBtn) technologyTabBtn.GetComponent<Image>().color = selectedColor;
                    break;
            }
        }

        private static void UpdateButtonLayout(bool isTechMode)
        {
            // Helper to resize a button
            void ResizeBtn(Button btn, float w, float h, int textSize)
            {
                if(btn == null) return;
                var le = btn.GetComponent<LayoutElement>();
                if(le != null)
                {
                    le.minWidth = w; 
                    le.preferredWidth = w;
                    le.minHeight = h; 
                    le.preferredHeight = h;
                }
                var txt = btn.GetComponentInChildren<Text>();
                if(txt != null)
                {
                    txt.resizeTextMaxSize = textSize;
                }
            }

            if (isTechMode)
            {
                float w = 60f; 
                float h = 10f;
                int fontSize = 1;
                ResizeBtn(diplomacyTabBtn, w, h, fontSize);
                ResizeBtn(economyTabBtn, w, h, fontSize);
                ResizeBtn(technologyTabBtn, w, h, fontSize);
                
                if(tabLayoutGroup != null) tabLayoutGroup.spacing = 0f; 
                
                // Postion Tabs on the RIGHT (Fixed Width)
                if(tabsRect != null)
                {
                     tabsRect.anchorMin = new Vector2(1f, 1f); 
                     tabsRect.anchorMax = new Vector2(1f, 1f);
                     tabsRect.pivot = new Vector2(1f, 1f);
                     
                     float containerWidth = 200f; 
                     
                     tabsRect.sizeDelta = new Vector2(containerWidth, 0f); 
                     tabsRect.sizeDelta = new Vector2(containerWidth, 20f); 
                     
                     float xOffset = -9f; 
                     float yOffset = -20f;
                     tabsRect.anchoredPosition = new Vector2(xOffset, yOffset); 
                }
            }
            else
            {
                float w = 60f; 
                float h = 18f; 
                int fontSize = 2;
                ResizeBtn(diplomacyTabBtn, w, h, fontSize);
                ResizeBtn(economyTabBtn, w, h, fontSize);
                ResizeBtn(technologyTabBtn, w, h, fontSize);
                
                if(tabLayoutGroup != null) tabLayoutGroup.spacing = 6f;
                
                if(tabsRect != null)
                {
                     tabsRect.anchorMin = new Vector2(0f, 1f);
                     tabsRect.anchorMax = new Vector2(1f, 1f);
                     tabsRect.offsetMin = new Vector2(9f, -34f);
                     tabsRect.offsetMax = new Vector2(-9f, -24f);
                }
            }
        }

        public static void OpenEconomicLaws()
        {
            EconomyWindow.SetVisible(false);
            InvestmentsWindow.SetVisible(false);
            DiplomacyWindow.SetVisible(false);
            
            EconomicLawsWindow.SetVisible(false);
            RankingsWindow.SetVisible(false);
            LawsWindow.SetVisible(false);
            PoliciesWindow.SetVisible(false);
            showEconomicLaws = true;
            showInvestments = false;
            Refresh();
        }

        public static void OpenLaws()
        {
            EconomyWindow.SetVisible(false);
            InvestmentsWindow.SetVisible(false);
            DiplomacyWindow.SetVisible(false);
            
            LawsWindow.SetVisible(true);
            LawsWindow.Refresh(Main.selectedKingdom);
            Refresh();
        }

        public static void OpenInvestments()
        {
            EconomyWindow.SetVisible(false);
            EconomicLawsWindow.SetVisible(false);
            
            InvestmentsWindow.SetVisible(true);
            showInvestments = true;
            showEconomicLaws = false;
            Refresh();
        }

        public static void ReturnToEconomyMain()
        {
            CloseAllWindows();
        }

        public static void CloseAllWindows()
        {
            showEconomicLaws = false;
            showInvestments = false;
            
            // Force hide all known windows using safety checks
            if(EconomyWindow.IsVisible()) EconomyWindow.SetVisible(false);
            if(EconomicLawsWindow.IsVisible()) EconomicLawsWindow.SetVisible(false);
            if(InvestmentsWindow.IsVisible()) InvestmentsWindow.SetVisible(false);
            if(ResourcesTradeWindow.IsVisible()) ResourcesTradeWindow.SetVisible(false);
            if(TradeWindow.IsVisible()) TradeWindow.SetVisible(false);
            if(DiplomacyWindow.IsVisible()) DiplomacyWindow.SetVisible(false);
            if(DiplomacyActionsWindow.IsVisible()) DiplomacyActionsWindow.SetVisible(false);
            if(LeadersWindow.IsVisible()) LeadersWindow.SetVisible(false);
            if(RankingsWindow.IsVisible()) RankingsWindow.SetVisible(false);
            if(PoliciesWindow.IsVisible()) PoliciesWindow.SetVisible(false);
            if(LawsWindow.IsVisible()) LawsWindow.SetVisible(false);
            if(TechnologyWindow.IsVisible()) TechnologyWindow.SetVisible(false);
            
            if (currentTab == HubTab.Economy)
            {
                EconomyWindow.SetVisible(true);
            }
            Refresh();
        }

        public static void SetVisible(bool visible)
        {
            if (root == null) Initialize();
            root.SetActive(visible);
            if (visible) Refresh();
        }

        public static void Refresh()
        {
            if (root == null || !root.activeSelf) return;
            var k = Main.selectedKingdom;
            
            if (DiplomacyWindow.IsVisible()) DiplomacyWindow.Refresh(k);
            
            if (currentTab == HubTab.Economy)
            {
                if (showEconomicLaws) EconomicLawsWindow.Refresh(k);
                else if (showInvestments) InvestmentsWindow.Refresh(k);
                else if (!TradeWindow.IsVisible() && !ResourcesTradeWindow.IsVisible()) EconomyWindow.Refresh(k);
            }
            
            if (TechnologyWindow.IsVisible()) TechnologyWindow.Refresh(k);
        }

        public static void Show() { if (root == null) Initialize(); root.SetActive(true); Refresh(); }
        public static void Hide() { if (root != null) root.SetActive(false); }
        public static void Toggle() { if (root == null) Initialize(); root.SetActive(!root.activeSelf); if (root.activeSelf) Refresh(); }

        private static Button BuildTabButton(Transform parent, string label, Action onClick)
        {
            var go = new GameObject(label + "Tab");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.001f);
            var le = go.AddComponent<LayoutElement>();
            le.minWidth = 50; le.preferredWidth = 60; 
            le.minHeight = 20; le.preferredHeight = 20;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(() => onClick.Invoke());

            var txtGO = new GameObject("Text");
            txtGO.transform.SetParent(go.transform, false);
            var txt = txtGO.AddComponent<Text>();
            txt.font = Resources.Load<Font>("Fonts/Roboto-Regular") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = label;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.resizeTextForBestFit = true;
            txt.resizeTextMinSize = 10; txt.resizeTextMaxSize = 14; 
            var rt = txt.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return btn;
        }
    }
}