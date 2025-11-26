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
        private static Sprite bread;
        
        // --- UI References ---
        private static Image headerLeftFlagIcon;
        private static Image headerLeftFlagBg;
        private static Image headerRightFlagIcon; // New Right Flag
        private static Image headerRightFlagBg;   // New Right Flag
        
        private static Text headerKingdomName;
        private static Text headerRulerInfo;
        private static Text headerPopInfo;

        private static Transform alliesContent;
        private static Transform warsContent;

        private static InputField searchInput;
        private static Transform kingdomListContent;
        private static Transform actionsListContent;

        // Indicators (Placeholder references if needed later)
        private static Text textCorruption;
        private static Text textWarExhaustion;
        private static Text textPoliticalPower;
        private static Text textStability;

        public static void Initialize(Transform parent)
        {
            if (root != null) return;

            windowInnerSprite = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.windowInnerSliced.png");
            bread = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.Resource.iconResMythril.png");

            // === ROOT CONTAINER ===
            root = new GameObject("DiplomacyRoot", typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = root.AddComponent<Image>();
            if (windowInnerSprite != null) { bg.sprite = windowInnerSprite; bg.type = Image.Type.Sliced; }
            bg.color = new Color(1f, 0f, 0f, 0.1f); 

            var rootV = root.AddComponent<VerticalLayoutGroup>();
            rootV.childAlignment = TextAnchor.UpperCenter;
            rootV.spacing = 4;
            rootV.padding = new RectOffset(4, 4, 4, 4);
            rootV.childControlWidth = true;
            rootV.childControlHeight = true;
            rootV.childForceExpandWidth = true;
            rootV.childForceExpandHeight = false;

            // === 2. HEADER PANEL (Flags + Info) ===
            CreateHeader(root.transform);

            // === 3. RELATIONS PANEL (Allies/Wars Scroll) ===
            CreateRelationsSection(root.transform);

            // === 4. SPLIT SECTION (Kingdoms + Actions) ===
            CreateSplitSection(root.transform);

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
            Color mainColor = Color.white;
            Color bannerColor = Color.white;

            if (k.kingdomColor != null)
            {
                mainColor = k.kingdomColor.getColorMain32();
                bannerColor = k.kingdomColor.getColorBanner();
            }

            // Left Flag
            if (headerLeftFlagBg) { headerLeftFlagBg.color = mainColor; headerLeftFlagBg.sprite = k.getElementBackground(); }
            if (headerLeftFlagIcon) { headerLeftFlagIcon.color = bannerColor; headerLeftFlagIcon.sprite = k.getElementIcon(); }

            // Right Flag
            if (headerRightFlagBg) { headerRightFlagBg.color = mainColor; headerRightFlagBg.sprite = k.getElementBackground(); }
            if (headerRightFlagIcon) { headerRightFlagIcon.color = bannerColor; headerRightFlagIcon.sprite = k.getElementIcon(); }

            // Text
            headerKingdomName.text = k.data.name;
            string ruler = k.king != null ? k.king.getName() : "None";
            headerRulerInfo.text = $"Ruler: {ruler}";
            headerPopInfo.text = $"Population: {k.getPopulationTotal()}";

            // 2. Relations
            RefreshRelations(k);

            // 3. Lists
            if (searchInput) RefreshSearchList(searchInput.text);
            
            // Note: Action buttons are static and don't need constant refreshing unless you add dynamic logic
        }

        // ================================================================================================
        // UI CONSTRUCTION
        // ================================================================================================

        private static void CreateHeader(Transform parent)
        {
            var container = new GameObject("HeaderPanel", typeof(RectTransform));
            container.transform.SetParent(parent, false);
            
            var bg = container.AddComponent<Image>();
            if (windowInnerSprite != null) { bg.sprite = windowInnerSprite; bg.type = Image.Type.Sliced; }
            bg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            var h = container.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(8, 8, 4, 4);
            h.spacing = 10;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;

            var le = container.AddComponent<LayoutElement>();
            le.preferredHeight = 40f; // Reduced height as requested
            le.minHeight = 40f;
            le.flexibleHeight = 0;

            // === LEFT FLAG ===
            CreateFlag(container.transform, "FlagLeft", out headerLeftFlagBg, out headerLeftFlagIcon);

            // === INFO ===
            var infoStack = new GameObject("InfoStack", typeof(RectTransform));
            infoStack.transform.SetParent(container.transform, false);
            var v = infoStack.AddComponent<VerticalLayoutGroup>();
            v.spacing = 0;
            v.childAlignment = TextAnchor.MiddleCenter;
            
            var stackLE = infoStack.AddComponent<LayoutElement>();
            stackLE.flexibleWidth = 1f;

            headerKingdomName = CreateText(infoStack.transform, "Kingdom", 9, FontStyle.Bold, Color.white);
            headerKingdomName.alignment = TextAnchor.MiddleCenter;
            headerRulerInfo = CreateText(infoStack.transform, "Ruler", 6, FontStyle.Normal, new Color(0.8f, 0.8f, 0.8f));
            headerRulerInfo.alignment = TextAnchor.MiddleCenter;
            headerPopInfo = CreateText(infoStack.transform, "Pop", 6, FontStyle.Normal, new Color(0.8f, 0.8f, 0.8f));
            headerPopInfo.alignment = TextAnchor.MiddleCenter;

            // === RIGHT FLAG ===
            CreateFlag(container.transform, "FlagRight", out headerRightFlagBg, out headerRightFlagIcon);
        }

        private static void CreateFlag(Transform parent, string name, out Image bgImg, out Image iconImg)
        {
            var flagWrapper = new GameObject(name, typeof(RectTransform));
            flagWrapper.transform.SetParent(parent, false);
            var flagLE = flagWrapper.AddComponent<LayoutElement>();
            flagLE.preferredWidth = 32f; flagLE.preferredHeight = 32f;
            flagLE.minWidth = 32f; flagLE.minHeight = 32f;
            
            var flagBgObj = new GameObject("FlagBG", typeof(RectTransform));
            flagBgObj.transform.SetParent(flagWrapper.transform, false);
            bgImg = flagBgObj.AddComponent<Image>();
            Stretch(flagBgObj.GetComponent<RectTransform>());

            var flagIconObj = new GameObject("FlagIcon", typeof(RectTransform));
            flagIconObj.transform.SetParent(flagWrapper.transform, false);
            iconImg = flagIconObj.AddComponent<Image>();
            Stretch(flagIconObj.GetComponent<RectTransform>(), 2);
        }

        private static void CreateRelationsSection(Transform parent)
        {
            var container = new GameObject("RelationsPanel", typeof(RectTransform));
            container.transform.SetParent(parent, false);
            
            var bg = container.AddComponent<Image>();
            if (windowInnerSprite != null) { bg.sprite = windowInnerSprite; bg.type = Image.Type.Sliced; }
            bg.color = new Color(0, 0, 0, 0.4f);

            var v = container.AddComponent<VerticalLayoutGroup>();
            v.spacing = 2;
            v.padding = new RectOffset(2, 2, 2, 2);
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = true;

            var le = container.AddComponent<LayoutElement>();
            le.preferredHeight = 60f; // Slightly smaller than 68f
            le.minHeight = 60f;
            le.flexibleHeight = 0;

            alliesContent = CreateSingleRelationRow(container.transform, "AlliesRow", new Color(0, 0.2f, 0, 0.3f));
            warsContent = CreateSingleRelationRow(container.transform, "WarsRow", new Color(0.2f, 0, 0, 0.3f));
        }

        private static Transform CreateSingleRelationRow(Transform parent, string name, Color tint)
        {
            var rowObj = new GameObject(name, typeof(RectTransform));
            rowObj.transform.SetParent(parent, false);

            var bg = rowObj.AddComponent<Image>();
            if (windowInnerSprite != null) { bg.sprite = windowInnerSprite; bg.type = Image.Type.Sliced; }
            bg.color = tint;

            var scroll = rowObj.AddComponent<ScrollRect>();
            scroll.horizontal = true; scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(rowObj.transform, false);
            Stretch(viewport.GetComponent<RectTransform>(), 1);
            
            viewport.AddComponent<RectMask2D>();
            viewport.AddComponent<Image>().color = Color.clear;

            var contentObj = new GameObject("Content", typeof(RectTransform));
            contentObj.transform.SetParent(viewport.transform, false);
            
            var h = contentObj.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleLeft;
            h.spacing = 2;
            h.childControlWidth = false; 
            h.childControlHeight = false;
            h.childForceExpandWidth = false; 
            h.childForceExpandHeight = false;

            var cRT = contentObj.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 0); cRT.anchorMax = new Vector2(0, 1);
            cRT.pivot = new Vector2(0, 0.5f);
            
            var fitter = contentObj.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = cRT;

            return contentObj.transform;
        }

        private static void CreateSplitSection(Transform parent)
        {
            var container = new GameObject("SplitSection", typeof(RectTransform));
            container.transform.SetParent(parent, false);

            // Background as requested
            var bg = container.AddComponent<Image>();
            if (windowInnerSprite != null) { bg.sprite = windowInnerSprite; bg.type = Image.Type.Sliced; }
            bg.color = new Color(0.15f, 0.15f, 0.15f, 0.3f);

            var h = container.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 4;
            h.padding = new RectOffset(4, 4, 4, 4);
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;

            var le = container.AddComponent<LayoutElement>();
            le.flexibleHeight = 1f; // Fill remaining space

            // === Left Column (Kingdom List) ===
            CreateLeftColumn(container.transform);

            // === Right Column (Actions) ===
            CreateRightColumn(container.transform);
        }

        private static void CreateLeftColumn(Transform parent)
        {
            var col = new GameObject("LeftCol", typeof(RectTransform));
            col.transform.SetParent(parent, false);
            
            var v = col.AddComponent<VerticalLayoutGroup>();
            v.spacing = 2;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            var le = col.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f; 
            le.flexibleHeight = 1f;

            // Search Bar
            var searchObj = new GameObject("SearchBox", typeof(RectTransform));
            searchObj.transform.SetParent(col.transform, false);
            var sLe = searchObj.AddComponent<LayoutElement>();
            sLe.preferredHeight = 20f; sLe.minHeight = 20f; sLe.flexibleHeight = 0;
            var sBg = searchObj.AddComponent<Image>();
            if (windowInnerSprite != null) { sBg.sprite = windowInnerSprite; sBg.type = Image.Type.Sliced; }
            sBg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            searchInput = searchObj.AddComponent<InputField>();
            var phText = CreateText(searchObj.transform, "Search...", 9, FontStyle.Italic, new Color(1,1,1,0.5f));
            Stretch(phText.rectTransform, 4);
            searchInput.placeholder = phText;
            var txtText = CreateText(searchObj.transform, "", 9, FontStyle.Normal, Color.white);
            Stretch(txtText.rectTransform, 4);
            searchInput.textComponent = txtText;
            searchInput.onValueChanged.AddListener(RefreshSearchList);

            // List Scroll Container
            var listObj = new GameObject("KingdomList", typeof(RectTransform));
            listObj.transform.SetParent(col.transform, false);
            var listLe = listObj.AddComponent<LayoutElement>();
            listLe.flexibleHeight = 1f;
            var lBg = listObj.AddComponent<Image>();
            if (windowInnerSprite != null) { lBg.sprite = windowInnerSprite; lBg.type = Image.Type.Sliced; }
            lBg.color = new Color(0, 0, 0, 0.2f);

            var scroll = listObj.AddComponent<ScrollRect>();
            scroll.vertical = true; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(listObj.transform, false);
            var vpRT = viewport.GetComponent<RectTransform>();
            Stretch(vpRT); // Stretch viewport to fill listObj
            viewport.AddComponent<RectMask2D>();

            // CONTENT
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            kingdomListContent = content.transform;

            var cRT = content.GetComponent<RectTransform>();
            // Stretch Horizontally (0-1), Align Top (1)
            cRT.anchorMin = new Vector2(0, 1); 
            cRT.anchorMax = new Vector2(1, 1); 
            cRT.pivot = new Vector2(0.5f, 1);
            
            // === FIX: Force size to match viewport exactly ===
            cRT.offsetMin = Vector2.zero;
            cRT.offsetMax = Vector2.zero;
            cRT.sizeDelta = new Vector2(0, 0); // Height will be controlled by ContentSizeFitter

            var vList = content.AddComponent<VerticalLayoutGroup>();
            vList.childAlignment = TextAnchor.UpperCenter;
            vList.spacing = 1; 
            vList.childControlWidth = true; 
            vList.childControlHeight = false; 
            vList.childForceExpandWidth = true;
            
            // Add padding so items aren't glued to edges
            vList.padding = new RectOffset(2, 2, 2, 2); 

            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            scroll.viewport = vpRT; 
            scroll.content = cRT;
        }

        private static void CreateRightColumn(Transform parent)
        {
            var col = new GameObject("RightCol", typeof(RectTransform));
            col.transform.SetParent(parent, false);
            var v = col.AddComponent<VerticalLayoutGroup>();
            v.spacing = 2; v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = true;
            var le = col.AddComponent<LayoutElement>();
            le.preferredWidth = 10f; 
            le.flexibleWidth = 0f; 
            le.flexibleHeight = 1f;

            var bg = col.AddComponent<Image>();
            if (windowInnerSprite != null) { bg.sprite = windowInnerSprite; bg.type = Image.Type.Sliced; }
            bg.color = new Color(0, 0, 0, 0.2f);
            
            var scroll = col.AddComponent<ScrollRect>();
            scroll.vertical = true; scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(col.transform, false);
            Stretch(viewport.GetComponent<RectTransform>());
            viewport.AddComponent<RectMask2D>();
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            actionsListContent = content.transform;
            var vList = content.AddComponent<VerticalLayoutGroup>();
            vList.childAlignment = TextAnchor.UpperCenter; vList.spacing = 2; vList.padding = new RectOffset(2,2,2,2);
            vList.childControlWidth = true; vList.childControlHeight = false; vList.childForceExpandWidth = true;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var cRT = content.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 1); cRT.anchorMax = new Vector2(1, 1); cRT.pivot = new Vector2(0.5f, 1);
            scroll.viewport = viewport.GetComponent<RectTransform>(); 
            scroll.content = cRT;

            CreateActionBtn("Laws", () => TopPanelUI.OpenEconomicLaws());
            CreateActionBtn("Doctrines", null);
            CreateActionBtn("Leaders", null);
            CreateActionBtn("Policies", null);
            CreateActionBtn("Ideologies", null);
            CreateActionBtn("National Flags", null);
        }

        // ================================================================================================
        // REFRESH LOGIC & HELPERS
        // ================================================================================================

        private static void RefreshRelations(Kingdom k)
        {
            if (alliesContent != null) foreach (Transform child in alliesContent) Object.Destroy(child.gameObject);
            if (warsContent != null)   foreach (Transform child in warsContent)   Object.Destroy(child.gameObject);

            if (k.hasAlliance())
            {
                foreach (var ally in k.getAlliance().kingdoms_list)
                {
                    if (ally != k && ally.isAlive())
                        CreateRelationChip(ally, Color.green, alliesContent);
                }
            }

            var wars = World.world.wars.getWars(k);
            foreach (var war in wars)
            {
                if (!war.hasEnded())
                {
                    bool weAreAttackers = war.isAttacker(k);
                    IEnumerable<Kingdom> enemies = weAreAttackers ? war.getDefenders() : war.getAttackers();

                    foreach (var enemy in enemies)
                    {
                        if (enemy != k && enemy.isAlive())
                            CreateRelationChip(enemy, Color.red, warsContent);
                    }
                }
            }
        }

        private static void CreateRelationChip(Kingdom k, Color borderColor, Transform parent)
        {
            var chip = new GameObject("Rel_" + k.data.name, typeof(RectTransform));
            chip.transform.SetParent(parent, false);
            var le = chip.AddComponent<LayoutElement>();
            le.minWidth = 18f; le.minHeight = 18f;
            le.preferredWidth = 18f; le.preferredHeight = 18f;

            var bg = chip.AddComponent<Image>();
            if (windowInnerSprite != null) { bg.sprite = windowInnerSprite; bg.type = Image.Type.Sliced; }
            bg.color = borderColor;

            var iconObj = new GameObject("Icon", typeof(RectTransform));
            iconObj.transform.SetParent(chip.transform, false);
            Stretch(iconObj.GetComponent<RectTransform>(), 1);

            var fBg = iconObj.AddComponent<Image>();
            fBg.sprite = k.getElementBackground();
            if (k.kingdomColor != null) fBg.color = k.kingdomColor.getColorMain32();

            var ico = new GameObject("Ico", typeof(RectTransform));
            ico.transform.SetParent(iconObj.transform, false);
            Stretch(ico.GetComponent<RectTransform>());
            var img = ico.AddComponent<Image>();
            img.sprite = k.getElementIcon();
            if (k.kingdomColor != null) img.color = k.kingdomColor.getColorBanner();

            var btn = chip.AddComponent<Button>();
            btn.onClick.AddListener(() => { Main.selectedKingdom = k; HubUI.Refresh(); TopPanelUI.Refresh(); });
        }

        private static void RefreshSearchList(string filter)
        {
            if(kingdomListContent == null) return;
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
            var btnObj = new GameObject("KBtn_" + k.data.name, typeof(RectTransform));
            btnObj.transform.SetParent(kingdomListContent, false);
            
            // Button Layout Element (Size in the list)
            var le = btnObj.AddComponent<LayoutElement>();
            le.preferredHeight = 26f;
            le.minHeight = 26f;
            le.flexibleWidth = 1f;

            // Button Background
            var img = btnObj.AddComponent<Image>();
            if (windowInnerSprite != null) { img.sprite = windowInnerSprite; img.type = Image.Type.Sliced; }
            img.color = new Color(0.2f, 0.2f, 0.22f, 1f);

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => {
                Main.selectedKingdom = k;
                HubUI.Refresh();
                TopPanelUI.Refresh();
            });

            // === FIX HERE: Layout Group Settings ===
            var h = btnObj.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 6;
            h.padding = new RectOffset(4, 4, 2, 2);
            h.childAlignment = TextAnchor.MiddleLeft;
            
            // IMPORTANT: These must be TRUE for LayoutElements to work
            h.childControlWidth = true; 
            h.childControlHeight = true; 
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;

            // === Flag Container ===
            var flagObj = new GameObject("Flag", typeof(RectTransform));
            flagObj.transform.SetParent(btnObj.transform, false);
            
            var fLe = flagObj.AddComponent<LayoutElement>();
            // Set strict size for the flag
            fLe.minWidth = 18f;
            fLe.minHeight = 22f;
            fLe.preferredWidth = 18f;
            fLe.preferredHeight = 22f;
            fLe.flexibleWidth = 0f; // Don't stretch
            
            // Flag Background
            var fBg = flagObj.AddComponent<Image>();
            fBg.sprite = k.getElementBackground();
            if (k.kingdomColor != null) fBg.color = k.kingdomColor.getColorMain32();

            // Flag Icon
            var fIco = new GameObject("Ico", typeof(RectTransform));
            fIco.transform.SetParent(flagObj.transform, false);
            Stretch(fIco.GetComponent<RectTransform>());
            var iImg = fIco.AddComponent<Image>();
            iImg.sprite = k.getElementIcon();
            if (k.kingdomColor != null) iImg.color = k.kingdomColor.getColorBanner();

            // === Name Text ===
            var txt = CreateText(btnObj.transform, k.data.name, 9, FontStyle.Normal, Color.white);
            txt.alignment = TextAnchor.MiddleLeft;
            
            // Text fills remaining space
            var txtLE = txt.gameObject.AddComponent<LayoutElement>();
            txtLE.flexibleWidth = 1f; 
            txtLE.minWidth = 10f;
        }

        private static void CreateActionBtn(string label, System.Action onClick)
        {
            var btnObj = new GameObject("Btn_" + label, typeof(RectTransform));
            btnObj.transform.SetParent(actionsListContent, false);
            var le = btnObj.AddComponent<LayoutElement>();
            le.preferredHeight = 26f; le.flexibleWidth = 1f;

            var img = btnObj.AddComponent<Image>();
            if (windowInnerSprite != null) { img.sprite = windowInnerSprite; img.type = Image.Type.Sliced; }
            img.color = new Color(0.25f, 0.25f, 0.3f, 1f);

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => { if (onClick != null) onClick.Invoke(); });

            var txt = CreateText(btnObj.transform, label, 9, FontStyle.Normal, Color.white);
            txt.alignment = TextAnchor.MiddleCenter;
            Stretch(txt.rectTransform);
        }

        private static Text CreateText(Transform parent, string content, int size, FontStyle style, Color col)
        {
            var go = new GameObject("Text", typeof(RectTransform));
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

        private static void Stretch(RectTransform rt, float offset = 0f)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(offset, offset); rt.offsetMax = new Vector2(-offset, -offset);
        }
    }
}