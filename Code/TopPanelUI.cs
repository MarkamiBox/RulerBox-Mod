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

                var img = root.AddComponent<Image>();
                img.sprite = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.MainHub.png");
                img.type = Image.Type.Sliced;
                img.color = Color.white;

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
                
                // --- FIX: Explicitly Initialize LeadersWindow ---
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
        // Create Tab Buttons
        private static void CreateTabs(Transform parent)
        {
            var tabsRow = new GameObject("TabsRow");
            tabsRow.transform.SetParent(parent, false);
            var tabsRT = tabsRow.AddComponent<RectTransform>();
            tabsRT.anchorMin = new Vector2(0f, 1f);
            tabsRT.anchorMax = new Vector2(1f, 1f);
            tabsRT.pivot     = new Vector2(0.6f, 1f);
            tabsRT.offsetMin = new Vector2(9f, -34f);
            tabsRT.offsetMax = new Vector2(-9f, -24f);

            var hTabs = tabsRow.AddComponent<HorizontalLayoutGroup>();
            hTabs.childAlignment = TextAnchor.MiddleCenter;
            hTabs.spacing = 3f;
            hTabs.padding = new RectOffset(8, 8, 0, 0);
            hTabs.childControlWidth = true;
            hTabs.childControlHeight = true;
            hTabs.childForceExpandWidth = true;

            diplomacyTabBtn = BuildTabButton(tabsRow.transform, "Diplomacy", () => OnTabClicked(HubTab.Diplomacy));
            economyTabBtn   = BuildTabButton(tabsRow.transform, "Economy",   () => OnTabClicked(HubTab.Economy));
            technologyTabBtn= BuildTabButton(tabsRow.transform, "Technology",() => OnTabClicked(HubTab.Technology));
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
            
            // Fix: Also hide LeadersWindow when switching tabs
            LeadersWindow.SetVisible(false);

            var selectedColor   = new Color(0.25f, 0.25f, 0.3f, 0.01f);
            var unselectedColor = new Color(0.15f, 0.15f, 0.2f, 0.01f);

            if (diplomacyTabBtn) diplomacyTabBtn.GetComponent<Image>().color = unselectedColor;
            if (economyTabBtn)   economyTabBtn.GetComponent<Image>().color   = unselectedColor;
            if (technologyTabBtn)technologyTabBtn.GetComponent<Image>().color= unselectedColor;

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

        public static void OpenEconomicLaws()
        {
            EconomyWindow.SetVisible(false);
            InvestmentsWindow.SetVisible(false);
            DiplomacyWindow.SetVisible(false);
            
            EconomicLawsWindow.SetVisible(false);
            RankingsWindow.SetVisible(false);
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
            
            EconomyWindow.SetVisible(false);
            EconomicLawsWindow.SetVisible(false);
            InvestmentsWindow.SetVisible(false);
            ResourcesTradeWindow.SetVisible(false);
            TradeWindow.SetVisible(false);
            DiplomacyWindow.SetVisible(false);
            DiplomacyActionsWindow.SetVisible(false);
            LawsWindow.SetVisible(false);
            LeadersWindow.SetVisible(false);
            RankingsWindow.SetVisible(false);
            PoliciesWindow.SetVisible(false);
            
            if (currentTab == HubTab.Economy)
            {
                EconomyWindow.SetVisible(true);
            }
            Refresh();
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
            le.minWidth = 10; le.preferredWidth = 10; le.minHeight = 18; le.preferredHeight = 18;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(() => onClick.Invoke());

            var txtGO = new GameObject("Text");
            txtGO.transform.SetParent(go.transform, false);
            var txt = txtGO.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = label;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.resizeTextForBestFit = true;
            txt.resizeTextMinSize = 6; txt.resizeTextMaxSize = 8;
            var rt = txt.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return btn;
        }
    }
}