using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace RulerBox
{
    public static class DiplomacyWindow
    {
        private static GameObject root;
        private static Sprite windowInnerSprite;
        
        // --- UI References ---
        private static Image headerFlagIcon;
        private static Image headerFlagBg;
        private static Text headerKingdomName;
        private static Text headerRulerInfo;
        private static Text headerPopInfo;

        private static Transform relationsContent;
        private static InputField searchInput;
        private static Transform kingdomListContent;
        private static Transform actionsListContent;

        // Indicators
        private static Text textCorruption;
        private static Text textWarExhaustion;
        private static Text textPoliticalPower;
        private static Text textStability;

        public static void Initialize(Transform parent)
        {
            if (root != null) return;

            // Load resources
            windowInnerSprite = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.windowInnerSliced.png");

            // === ROOT CONTAINER ===
            // This ensures it fills the "ContentContainer" from TopPanelUI exactly
            root = new GameObject("DiplomacyRoot");
            root.transform.SetParent(parent, false);

            var rt = root.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Main Vertical Stack
            var rootV = root.AddComponent<VerticalLayoutGroup>();
            rootV.childAlignment = TextAnchor.UpperCenter;
            rootV.spacing = 4;
            rootV.padding = new RectOffset(4, 4, 4, 4);
            rootV.childControlWidth = true;
            rootV.childControlHeight = false; // Let children define their height, except the flexible middle
            rootV.childForceExpandWidth = true;
            rootV.childForceExpandHeight = false;

            // === 1. TITLE ===
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(root.transform, false);
            var titleText = titleGO.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.text = "Diplomacy";
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;
            titleText.resizeTextForBestFit = true;
            titleText.resizeTextMinSize = 10;
            titleText.resizeTextMaxSize = 16;
            
            var titleLE = titleGO.AddComponent<LayoutElement>();
            titleLE.minHeight = 24f;
            titleLE.preferredHeight = 24f;
            titleLE.flexibleHeight = 0;

            // === 2. HEADER PANEL (Flag + Info) ===
            CreateHeader(root.transform);

            // === 3. RELATIONS PANEL (Scroll) ===
            CreateRelationsSection(root.transform);

            // === 4. SPLIT VIEW (Search List | Buttons) ===
            // This container needs flexible height to fill the rest of the window
            var splitGO = new GameObject("SplitView");
            splitGO.transform.SetParent(root.transform, false);
            
            var splitH = splitGO.AddComponent<HorizontalLayoutGroup>();
            splitH.spacing = 4;
            splitH.childControlWidth = true;
            splitH.childControlHeight = true;
            splitH.childForceExpandWidth = false; // Don't force, let LayoutElements handle width
            splitH.childForceExpandHeight = true;

            var splitLE = splitGO.AddComponent<LayoutElement>();
            splitLE.flexibleHeight = 1f; // THIS makes it fill vertical space

            // Left Column (Search + List)
            CreateLeftColumn(splitGO.transform);

            // Right Column (Buttons)
            CreateRightColumn(splitGO.transform);

            // === 5. FOOTER INDICATORS ===
            CreateFooter(root.transform);

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

            // 1. Header
            if (k.kingdomColor != null)
            {
                headerFlagBg.color = k.kingdomColor.getColorMain32();
                headerFlagIcon.color = k.kingdomColor.getColorBanner();
            }
            headerFlagBg.sprite = k.getElementBackground();
            headerFlagIcon.sprite = k.getElementIcon();

            headerKingdomName.text = k.data.name;
            string ruler = k.king != null ? k.king.getName() : "None";
            headerRulerInfo.text = $"Ruler: {ruler}";
            headerPopInfo.text = $"Population: {k.getPopulationTotal()}";

            // 2. Relations
            RefreshRelations(k);

            // 3. Search List
            RefreshSearchList(searchInput.text);

            // 4. Indicators
            var d = KingdomMetricsSystem.Get(k);
            if (d != null)
            {
                textCorruption.text = $"{d.CorruptionLevel * 100:0}%";
                textWarExhaustion.text = $"{d.WarExhaustion:0}";
                textPoliticalPower.text = $"{k.data.renown}";
                textStability.text = $"{d.Stability:0}%";
            }
        }

        // ================================================================================================
        // UI BUILDERS
        // ================================================================================================

        private static void CreateHeader(Transform parent)
        {
            var container = new GameObject("HeaderPanel");
            container.transform.SetParent(parent, false);
            
            var bg = container.AddComponent<Image>();
            bg.sprite = windowInnerSprite; 
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.15f, 0.15f, 0.15f, 1f); // Dark background

            var h = container.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(8, 8, 4, 4);
            h.spacing = 10;
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;

            var le = container.AddComponent<LayoutElement>();
            le.preferredHeight = 50f;
            le.minHeight = 50f;
            le.flexibleHeight = 0;

            // Flag
            var flagWrapper = new GameObject("FlagWrapper");
            flagWrapper.transform.SetParent(container.transform, false);
            var flagLE = flagWrapper.AddComponent<LayoutElement>();
            flagLE.preferredWidth = 40f;
            flagLE.preferredHeight = 40f;
            flagLE.minWidth = 40f;
            flagLE.minHeight = 40f;
            
            var flagBgObj = new GameObject("FlagBG");
            flagBgObj.transform.SetParent(flagWrapper.transform, false);
            headerFlagBg = flagBgObj.AddComponent<Image>();
            var fBgRT = flagBgObj.GetComponent<RectTransform>();
            fBgRT.anchorMin = Vector2.zero; fBgRT.anchorMax = Vector2.one; 
            fBgRT.offsetMin = Vector2.zero; fBgRT.offsetMax = Vector2.zero;

            var flagIconObj = new GameObject("FlagIcon");
            flagIconObj.transform.SetParent(flagWrapper.transform, false);
            headerFlagIcon = flagIconObj.AddComponent<Image>();
            var fIconRT = flagIconObj.GetComponent<RectTransform>();
            fIconRT.anchorMin = Vector2.zero; fIconRT.anchorMax = Vector2.one; 
            fIconRT.offsetMin = new Vector2(2,2); fIconRT.offsetMax = new Vector2(-2,-2);

            // Info Text Stack
            var infoStack = new GameObject("InfoStack");
            infoStack.transform.SetParent(container.transform, false);
            var v = infoStack.AddComponent<VerticalLayoutGroup>();
            v.spacing = 0;
            v.childAlignment = TextAnchor.MiddleLeft;
            
            var stackLE = infoStack.AddComponent<LayoutElement>();
            stackLE.flexibleWidth = 1f; // Fill remaining width

            headerKingdomName = CreateText(infoStack.transform, "Kingdom", 11, FontStyle.Bold, Color.white);
            headerRulerInfo = CreateText(infoStack.transform, "Ruler", 9, FontStyle.Normal, new Color(0.8f, 0.8f, 0.8f));
            headerPopInfo = CreateText(infoStack.transform, "Pop", 9, FontStyle.Normal, new Color(0.8f, 0.8f, 0.8f));
        }

        private static void CreateRelationsSection(Transform parent)
        {
            var container = new GameObject("RelationsPanel");
            container.transform.SetParent(parent, false);
            
            // Black background bar
            var bg = container.AddComponent<Image>();
            bg.sprite = windowInnerSprite; 
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0, 0, 0, 0.5f);

            var le = container.AddComponent<LayoutElement>();
            le.preferredHeight = 40f;
            le.minHeight = 40f;
            le.flexibleHeight = 0;

            // ScrollRect
            var scroll = container.AddComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(container.transform, false);
            var vpRT = viewport.AddComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = new Vector2(4, 4); vpRT.offsetMax = new Vector2(-4, -4);
            
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            viewport.AddComponent<Image>().color = Color.clear;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            relationsContent = content.transform;
            
            var h = content.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleLeft;
            h.spacing = 4;
            h.childControlWidth = false; // Let children set their size
            h.childControlHeight = false;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            var cRT = content.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 0); cRT.anchorMax = new Vector2(0, 1);
            cRT.pivot = new Vector2(0, 0.5f);

            scroll.viewport = vpRT;
            scroll.content = cRT;
        }

        private static void CreateLeftColumn(Transform parent)
        {
            var col = new GameObject("LeftCol");
            col.transform.SetParent(parent, false);
            
            var v = col.AddComponent<VerticalLayoutGroup>();
            v.spacing = 2;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false; // We control height via LayoutElement

            var le = col.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f; 
            le.flexibleHeight = 1f; // Fill vertical

            // Search Bar
            var searchObj = new GameObject("SearchBox");
            searchObj.transform.SetParent(col.transform, false);
            
            var sLe = searchObj.AddComponent<LayoutElement>();
            sLe.preferredHeight = 22f;
            sLe.minHeight = 22f;
            sLe.flexibleHeight = 0;
            
            var sBg = searchObj.AddComponent<Image>();
            sBg.sprite = windowInnerSprite; sBg.type = Image.Type.Sliced;
            sBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            searchInput = searchObj.AddComponent<InputField>();
            
            var phText = CreateText(searchObj.transform, "Search...", 9, FontStyle.Italic, new Color(1,1,1,0.5f));
            phText.rectTransform.offsetMin = new Vector2(6, 0);
            searchInput.placeholder = phText;

            var txtText = CreateText(searchObj.transform, "", 9, FontStyle.Normal, Color.white);
            txtText.rectTransform.offsetMin = new Vector2(6, 0);
            searchInput.textComponent = txtText;
            searchInput.onValueChanged.AddListener(RefreshSearchList);

            // Scroll List
            var listObj = new GameObject("KingdomList");
            listObj.transform.SetParent(col.transform, false);
            
            var listLe = listObj.AddComponent<LayoutElement>();
            listLe.flexibleHeight = 1f; // Fill remaining vertical

            var lBg = listObj.AddComponent<Image>();
            lBg.sprite = windowInnerSprite; lBg.type = Image.Type.Sliced;
            lBg.color = new Color(0, 0, 0, 0.2f);

            var scroll = listObj.AddComponent<ScrollRect>();
            scroll.vertical = true; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(listObj.transform, false);
            var vpRT = viewport.AddComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = Vector2.zero; vpRT.offsetMax = Vector2.zero;
            viewport.AddComponent<RectMask2D>();

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            kingdomListContent = content.transform;
            
            var vList = content.AddComponent<VerticalLayoutGroup>();
            vList.childAlignment = TextAnchor.UpperCenter;
            vList.spacing = 2;
            vList.padding = new RectOffset(0,0,2,2);
            vList.childControlWidth = true;
            vList.childControlHeight = false;
            vList.childForceExpandWidth = true;

            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var cRT = content.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 1); cRT.anchorMax = new Vector2(1, 1);
            cRT.pivot = new Vector2(0.5f, 1);

            scroll.viewport = vpRT;
            scroll.content = cRT;
        }

        private static void CreateRightColumn(Transform parent)
        {
            var col = new GameObject("RightCol");
            col.transform.SetParent(parent, false);
            
            var v = col.AddComponent<VerticalLayoutGroup>();
            v.spacing = 2;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = true;

            var le = col.AddComponent<LayoutElement>();
            le.preferredWidth = 100f; // Fixed width for buttons column
            le.flexibleWidth = 0f;
            le.flexibleHeight = 1f;

            // Background for buttons list
            var bg = col.AddComponent<Image>();
            bg.sprite = windowInnerSprite; bg.type = Image.Type.Sliced;
            bg.color = new Color(0, 0, 0, 0.2f);

            // List of buttons
            var scroll = col.AddComponent<ScrollRect>();
            scroll.vertical = true; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(col.transform, false);
            var vpRT = viewport.AddComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = Vector2.zero; vpRT.offsetMax = Vector2.zero;
            viewport.AddComponent<RectMask2D>();

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            actionsListContent = content.transform;

            var vList = content.AddComponent<VerticalLayoutGroup>();
            vList.childAlignment = TextAnchor.UpperCenter;
            vList.spacing = 2;
            vList.padding = new RectOffset(2,2,2,2);
            vList.childControlWidth = true;
            vList.childControlHeight = false;
            vList.childForceExpandWidth = true;

            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var cRT = content.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 1); cRT.anchorMax = new Vector2(1, 1);
            cRT.pivot = new Vector2(0.5f, 1);

            scroll.viewport = vpRT;
            scroll.content = cRT;

            // Static Buttons
            CreateActionBtn("Laws", () => TopPanelUI.OpenEconomicLaws());
            CreateActionBtn("Leaders", null);
            CreateActionBtn("Policies", null);
            CreateActionBtn("Ideologies", null);
            CreateActionBtn("National Flags", null);
        }

        private static void CreateFooter(Transform parent)
        {
            var row = new GameObject("Footer");
            row.transform.SetParent(parent, false);
            
            // Background for footer
            var bg = row.AddComponent<Image>();
            bg.sprite = windowInnerSprite; bg.type = Image.Type.Sliced;
            bg.color = new Color(0.1f, 0.1f, 0.1f, 1f);

            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleCenter;
            h.spacing = 15;
            h.padding = new RectOffset(10, 10, 0, 0);
            h.childControlWidth = false; // Critical: Don't stretch children icons
            h.childControlHeight = false;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 28f;
            le.minHeight = 28f;
            le.flexibleHeight = 0;

            // Indicators
            MakeInd(row.transform, "iconSkull", new Color(1f, 0.4f, 0.4f), out textCorruption);
            MakeInd(row.transform, "iconWar", new Color(1f, 0.4f, 0.4f), out textWarExhaustion);
            MakeInd(row.transform, "iconKingdom", new Color(1f, 0.9f, 0.4f), out textPoliticalPower);
            MakeInd(row.transform, "iconPeace", new Color(0.4f, 0.8f, 1f), out textStability);
        }

        private static void MakeInd(Transform parent, string iconName, Color col, out Text txt)
        {
            var ind = new GameObject("Ind_" + iconName);
            ind.transform.SetParent(parent, false);
            
            // Horizontal layout for Icon + Text
            var h = ind.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 4;
            h.childControlWidth = false; 
            h.childForceExpandWidth = false;
            h.childAlignment = TextAnchor.MiddleCenter;

            // Sizing for the whole indicator group
            var le = ind.AddComponent<LayoutElement>();
            le.preferredHeight = 20f;
            le.minWidth = 40f; 

            // Icon
            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(ind.transform, false);
            var img = iconObj.AddComponent<Image>();
            var spr = Resources.Load<Sprite>("ui/Icons/" + iconName);
            if (spr == null) spr = windowInnerSprite;
            img.sprite = spr;
            img.color = col;
            img.preserveAspect = true; // Important: prevents stretching

            var iconLe = iconObj.AddComponent<LayoutElement>();
            iconLe.preferredWidth = 16; 
            iconLe.preferredHeight = 16;

            // Text
            txt = CreateText(ind.transform, "0", 10, FontStyle.Bold, col);
            txt.alignment = TextAnchor.MiddleLeft;
            var txtLe = txt.gameObject.AddComponent<LayoutElement>();
            txtLe.minWidth = 20f;
        }

        // ================================================================================================
        // HELPERS
        // ================================================================================================

        private static void CreateActionBtn(string label, System.Action onClick)
        {
            var btnObj = new GameObject("Btn_" + label);
            btnObj.transform.SetParent(actionsListContent, false);
            
            var le = btnObj.AddComponent<LayoutElement>();
            le.preferredHeight = 28f;
            le.flexibleWidth = 1f;

            var img = btnObj.AddComponent<Image>();
            img.sprite = windowInnerSprite; img.type = Image.Type.Sliced;
            img.color = new Color(0.25f, 0.25f, 0.3f, 1f);

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => {
                if (onClick != null) onClick.Invoke();
                else WorldTip.showNow(label + " coming soon", false, "top", 1f);
            });

            var txt = CreateText(btnObj.transform, label, 10, FontStyle.Normal, Color.white);
            txt.alignment = TextAnchor.MiddleCenter;
            txt.rectTransform.anchorMin = Vector2.zero; txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.offsetMin = Vector2.zero; txt.rectTransform.offsetMax = Vector2.zero;
        }

        private static void RefreshRelations(Kingdom k)
        {
            foreach (Transform child in relationsContent) Object.Destroy(child.gameObject);

            // Allies
            if (k.hasAlliance())
            {
                foreach (var ally in k.getAlliance().kingdoms_list)
                {
                    if (ally != k && ally.isAlive())
                        CreateRelationChip(ally, Color.green);
                }
            }
            // Enemies
            var wars = World.world.wars.getWars(k);
            foreach (var war in wars)
            {
                if (!war.hasEnded())
                {
                    var enemy = war.getMainDefender() == k ? war.getMainAttacker() : war.getMainDefender();
                    if (enemy != null && enemy != k)
                        CreateRelationChip(enemy, Color.red);
                }
            }
        }

        private static void CreateRelationChip(Kingdom k, Color borderColor)
        {
            var chip = new GameObject("Rel_" + k.data.name);
            chip.transform.SetParent(relationsContent, false);
            var le = chip.AddComponent<LayoutElement>();
            le.preferredWidth = 32; le.preferredHeight = 32;

            var bg = chip.AddComponent<Image>();
            bg.sprite = windowInnerSprite; bg.type = Image.Type.Sliced;
            bg.color = borderColor;

            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(chip.transform, false);
            var rt = iconObj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(2,2); rt.offsetMax = new Vector2(-2,-2);

            var fBg = iconObj.AddComponent<Image>();
            fBg.sprite = k.getElementBackground();
            fBg.color = k.kingdomColor.getColorMain32();

            var ico = new GameObject("Ico").AddComponent<Image>();
            ico.transform.SetParent(iconObj.transform, false);
            ico.rectTransform.anchorMin = Vector2.zero; ico.rectTransform.anchorMax = Vector2.one;
            ico.sprite = k.getElementIcon();
            ico.color = k.kingdomColor.getColorBanner();

            var btn = chip.AddComponent<Button>();
            btn.onClick.AddListener(() => {
                Main.selectedKingdom = k;
                HubUI.Refresh();
                TopPanelUI.Refresh();
            });
        }

        private static void RefreshSearchList(string filter)
        {
            foreach (Transform t in kingdomListContent) Object.Destroy(t.gameObject);

            foreach (var k in World.world.kingdoms.list)
            {
                if (!k.isAlive() || k.data.id == Globals.NEUTRAL_KINGDOM_NUMERIC_ID) continue;
                if (!string.IsNullOrEmpty(filter) && !k.data.name.ToLower().Contains(filter.ToLower())) continue;

                CreateKingdomButton(k);
            }
        }

        private static void CreateKingdomButton(Kingdom k)
        {
            var btnObj = new GameObject("KBtn_" + k.data.name);
            btnObj.transform.SetParent(kingdomListContent, false);
            
            var le = btnObj.AddComponent<LayoutElement>();
            le.preferredHeight = 28f;
            le.flexibleWidth = 1f;

            var img = btnObj.AddComponent<Image>();
            img.sprite = windowInnerSprite; img.type = Image.Type.Sliced;
            img.color = new Color(0.2f, 0.2f, 0.22f, 1f);

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => {
                Main.selectedKingdom = k;
                HubUI.Refresh();
                TopPanelUI.Refresh();
            });

            var h = btnObj.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 6;
            h.padding = new RectOffset(4, 4, 2, 2);
            h.childControlWidth = false;
            h.childForceExpandWidth = false;
            h.childAlignment = TextAnchor.MiddleLeft;

            // Tiny Flag
            var flagObj = new GameObject("Flag");
            flagObj.transform.SetParent(btnObj.transform, false);
            var fLe = flagObj.AddComponent<LayoutElement>();
            fLe.preferredWidth = 18f;
            fLe.preferredHeight = 24f;
            
            var fBg = flagObj.AddComponent<Image>();
            fBg.sprite = k.getElementBackground();
            fBg.color = k.kingdomColor.getColorMain32();

            var fIco = new GameObject("Ico").AddComponent<Image>();
            fIco.transform.SetParent(flagObj.transform, false);
            fIco.rectTransform.anchorMin = Vector2.zero; fIco.rectTransform.anchorMax = Vector2.one;
            fIco.sprite = k.getElementIcon();
            fIco.color = k.kingdomColor.getColorBanner();

            // Name
            var txt = CreateText(btnObj.transform, k.data.name, 9, FontStyle.Normal, Color.white);
            txt.alignment = TextAnchor.MiddleLeft;
            var txtLE = txt.gameObject.AddComponent<LayoutElement>();
            txtLE.flexibleWidth = 1f;
        }

        private static Text CreateText(Transform parent, string content, int size, FontStyle style, Color col)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var txt = go.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = content;
            txt.fontSize = size;
            txt.fontStyle = style;
            txt.color = col;
            txt.raycastTarget = false;
            txt.resizeTextForBestFit = false;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Truncate;
            return txt;
        }
    }
}