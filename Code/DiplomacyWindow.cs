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

        // Relations References
        private static Transform alliesContent;
        private static Transform warsContent;

        // Search & Lists References
        private static InputField searchInput;
        private static Transform kingdomListContent;
        private static Transform actionsListContent;

        // Indicator References
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
            root = new GameObject("DiplomacyRoot", typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Background (Red tint)
            var bg = root.AddComponent<Image>();
            if (windowInnerSprite != null) { bg.sprite = windowInnerSprite; bg.type = Image.Type.Sliced; }
            bg.color = new Color(1f, 0f, 0f, 0.1f); 

            // Main Layout
            var rootV = root.AddComponent<VerticalLayoutGroup>();
            rootV.childAlignment = TextAnchor.UpperCenter;
            rootV.spacing = 4;
            rootV.padding = new RectOffset(4, 4, 4, 4);
            rootV.childControlWidth = true;
            rootV.childControlHeight = true;
            rootV.childForceExpandWidth = true;
            rootV.childForceExpandHeight = false;

            // === 2. HEADER PANEL ===
            CreateHeader(root.transform);

            // === 3. RELATIONS PANEL ===
            CreateRelationsSection(root.transform);

            // === 4. SPLIT VIEW ===
            var splitGO = new GameObject("SplitView", typeof(RectTransform));
            splitGO.transform.SetParent(root.transform, false);
            
            var splitH = splitGO.AddComponent<HorizontalLayoutGroup>();
            splitH.spacing = 4;
            splitH.childControlWidth = true;
            splitH.childControlHeight = true;
            splitH.childForceExpandWidth = true;
            splitH.childForceExpandHeight = true;

            var splitLE = splitGO.AddComponent<LayoutElement>();
            splitLE.flexibleHeight = 1f; 

            // Left Column
            //CreateLeftColumn(splitGO.transform);

            // Right Column
            //CreateRightColumn(splitGO.transform);

            // === 5. FOOTER ===
            //CreateFooter(root.transform);

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

            // Safety check: if init failed but window is open
            if (headerKingdomName == null) return;

            // 1. Header
            if (k.kingdomColor != null)
            {
                if(headerFlagBg) headerFlagBg.color = k.kingdomColor.getColorMain32();
                if(headerFlagIcon) headerFlagIcon.color = k.kingdomColor.getColorBanner();
            }
            if(headerFlagBg) headerFlagBg.sprite = k.getElementBackground();
            if(headerFlagIcon) headerFlagIcon.sprite = k.getElementIcon();

            headerKingdomName.text = k.data.name;
            string ruler = k.king != null ? k.king.getName() : "None";
            headerRulerInfo.text = $"Ruler: {ruler}";
            headerPopInfo.text = $"Population: {k.getPopulationTotal()}";

            // 2. Relations
            RefreshRelations(k);

            // 3. Search List
            if(searchInput) RefreshSearchList(searchInput.text);

            // 4. Indicators
            var d = KingdomMetricsSystem.Get(k);
            if (d != null)
            {
                if(textCorruption) textCorruption.text = $"{d.CorruptionLevel * 100:0}%";
                if(textWarExhaustion) textWarExhaustion.text = $"{d.WarExhaustion:0}";
                if(textPoliticalPower) textPoliticalPower.text = $"{k.data.renown}";
                if(textStability) textStability.text = $"{d.Stability:0}%";
            }
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
            h.childAlignment = TextAnchor.MiddleLeft;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;

            var le = container.AddComponent<LayoutElement>();
            le.preferredHeight = 50f;
            le.minHeight = 50f;
            le.flexibleHeight = 0;

            // Flag
            var flagWrapper = new GameObject("FlagWrapper", typeof(RectTransform));
            flagWrapper.transform.SetParent(container.transform, false);
            var flagLE = flagWrapper.AddComponent<LayoutElement>();
            flagLE.preferredWidth = 40f; flagLE.preferredHeight = 40f;
            flagLE.minWidth = 40f; flagLE.minHeight = 40f;
            
            var flagBgObj = new GameObject("FlagBG", typeof(RectTransform));
            flagBgObj.transform.SetParent(flagWrapper.transform, false);
            headerFlagBg = flagBgObj.AddComponent<Image>();
            Stretch(flagBgObj.GetComponent<RectTransform>());

            var flagIconObj = new GameObject("FlagIcon", typeof(RectTransform));
            flagIconObj.transform.SetParent(flagWrapper.transform, false);
            headerFlagIcon = flagIconObj.AddComponent<Image>();
            Stretch(flagIconObj.GetComponent<RectTransform>(), 2);

            // Info Text Stack
            var infoStack = new GameObject("InfoStack", typeof(RectTransform));
            infoStack.transform.SetParent(container.transform, false);
            var v = infoStack.AddComponent<VerticalLayoutGroup>();
            v.spacing = 0;
            v.childAlignment = TextAnchor.MiddleLeft;
            
            var stackLE = infoStack.AddComponent<LayoutElement>();
            stackLE.flexibleWidth = 1f;

            headerKingdomName = CreateText(infoStack.transform, "Kingdom", 9, FontStyle.Bold, Color.white);
            headerRulerInfo = CreateText(infoStack.transform, "Ruler", 6, FontStyle.Normal, new Color(0.8f, 0.8f, 0.8f));
            headerPopInfo = CreateText(infoStack.transform, "Pop", 6, FontStyle.Normal, new Color(0.8f, 0.8f, 0.8f));
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
            le.preferredHeight = 68f; 
            le.minHeight = 68f;
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
            Stretch(viewport.GetComponent<RectTransform>(), 2);
            viewport.AddComponent<RectMask2D>();
            viewport.AddComponent<Image>().color = Color.clear;

            var contentObj = new GameObject("Content", typeof(RectTransform));
            contentObj.transform.SetParent(viewport.transform, false);
            
            var h = contentObj.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleLeft;
            h.spacing = 2;
            h.childControlWidth = false; h.childControlHeight = false;
            h.childForceExpandWidth = false; h.childForceExpandHeight = false;

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
            le.flexibleWidth = 1f; le.flexibleHeight = 1f;

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

            // List
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
            Stretch(vpRT);
            viewport.AddComponent<RectMask2D>();
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            kingdomListContent = content.transform;
            var vList = content.AddComponent<VerticalLayoutGroup>();
            vList.childAlignment = TextAnchor.UpperCenter;
            vList.spacing = 1; vList.childControlWidth = true; vList.childControlHeight = false; vList.childForceExpandWidth = true;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var cRT = content.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 1); cRT.anchorMax = new Vector2(1, 1); cRT.pivot = new Vector2(0.5f, 1);
            scroll.viewport = vpRT; scroll.content = cRT;
        }

        private static void CreateRightColumn(Transform parent)
        {
            var col = new GameObject("RightCol", typeof(RectTransform));
            col.transform.SetParent(parent, false);
            var v = col.AddComponent<VerticalLayoutGroup>();
            v.spacing = 2; v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = true;
            var le = col.AddComponent<LayoutElement>();
            le.preferredWidth = 100f; le.flexibleWidth = 0f; le.flexibleHeight = 1f;

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
            //scroll.viewport = vpRT; 
            scroll.content = cRT;

            CreateActionBtn("Laws", () => TopPanelUI.OpenEconomicLaws());
            CreateActionBtn("Leaders", null);
            CreateActionBtn("Policies", null);
            CreateActionBtn("Ideologies", null);
            CreateActionBtn("National Flags", null);
        }

        private static void CreateFooter(Transform parent)
        {
            var row = new GameObject("Footer", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            
            var bg = row.AddComponent<Image>();
            if (windowInnerSprite != null) { bg.sprite = windowInnerSprite; bg.type = Image.Type.Sliced; }
            bg.color = new Color(0.1f, 0.1f, 0.1f, 1f);

            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleCenter;
            h.spacing = 10;
            h.padding = new RectOffset(4, 4, 0, 0);
            h.childControlWidth = false;
            h.childControlHeight = false;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 24f;
            le.minHeight = 24f;
            le.flexibleHeight = 0;

            MakeInd(row.transform, "iconSkull", new Color(1f, 0.4f, 0.4f), out textCorruption);
            MakeInd(row.transform, "iconWar", new Color(1f, 0.4f, 0.4f), out textWarExhaustion);
            MakeInd(row.transform, "iconKingdom", new Color(1f, 0.9f, 0.4f), out textPoliticalPower);
            MakeInd(row.transform, "iconPeace", new Color(0.4f, 0.8f, 1f), out textStability);
        }

        private static void MakeInd(Transform parent, string iconName, Color col, out Text txt)
        {
            var ind = new GameObject("Ind_" + iconName, typeof(RectTransform));
            ind.transform.SetParent(parent, false);
            
            var h = ind.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 3;
            h.childControlWidth = false; 
            h.childControlHeight = false;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            h.childAlignment = TextAnchor.MiddleCenter;

            var le = ind.AddComponent<LayoutElement>();
            le.preferredHeight = 20f;
            le.minWidth = 30f; 
            le.preferredWidth = 50f;
            le.flexibleWidth = 0f;

            var iconObj = new GameObject("Icon", typeof(RectTransform));
            iconObj.transform.SetParent(ind.transform, false);
            var img = iconObj.AddComponent<Image>();
            var spr = Resources.Load<Sprite>("ui/Icons/" + iconName);
            if (spr == null) spr = windowInnerSprite;
            img.sprite = spr;
            img.color = col;
            img.preserveAspect = true; 

            var iconLe = iconObj.AddComponent<LayoutElement>();
            iconLe.preferredWidth = 14; 
            iconLe.preferredHeight = 14;

            txt = CreateText(ind.transform, "0", 9, FontStyle.Bold, col);
            txt.alignment = TextAnchor.MiddleLeft;
            var txtLe = txt.gameObject.AddComponent<LayoutElement>();
            txtLe.minWidth = 15f;
        }

        private static void CreateActionBtn(string label, System.Action onClick)
        {
            var btnObj = new GameObject("Btn_" + label, typeof(RectTransform));
            btnObj.transform.SetParent(actionsListContent, false);
            var le = btnObj.AddComponent<LayoutElement>();
            le.preferredHeight = 26f;
            le.flexibleWidth = 1f;

            var img = btnObj.AddComponent<Image>();
            if (windowInnerSprite != null) { img.sprite = windowInnerSprite; img.type = Image.Type.Sliced; }
            img.color = new Color(0.25f, 0.25f, 0.3f, 1f);

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => {
                if (onClick != null) onClick.Invoke();
                else WorldTip.showNow(label + " coming soon", false, "top", 1f);
            });

            var txt = CreateText(btnObj.transform, label, 9, FontStyle.Normal, Color.white);
            txt.alignment = TextAnchor.MiddleCenter;
            Stretch(txt.rectTransform);
        }

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
                    var enemy = war.getMainDefender() == k ? war.getMainAttacker() : war.getMainDefender();
                    if (enemy != null && enemy != k)
                        CreateRelationChip(enemy, Color.red, warsContent);
                }
            }
        }

        private static void CreateRelationChip(Kingdom k, Color borderColor, Transform parent)
        {
            var chip = new GameObject("Rel_" + k.data.name, typeof(RectTransform));
            chip.transform.SetParent(parent, false);
            var le = chip.AddComponent<LayoutElement>();
            le.preferredWidth = 30; le.preferredHeight = 30;

            var bg = chip.AddComponent<Image>();
            if (windowInnerSprite != null) { bg.sprite = windowInnerSprite; bg.type = Image.Type.Sliced; }
            bg.color = borderColor;

            var iconObj = new GameObject("Icon", typeof(RectTransform));
            iconObj.transform.SetParent(chip.transform, false);
            Stretch(iconObj.GetComponent<RectTransform>(), 2);

            var fBg = iconObj.AddComponent<Image>();
            fBg.sprite = k.getElementBackground();
            fBg.color = k.kingdomColor.getColorMain32();

            var ico = new GameObject("Ico", typeof(RectTransform));
            ico.transform.SetParent(iconObj.transform, false);
            Stretch(ico.GetComponent<RectTransform>());
            var img = ico.AddComponent<Image>();
            img.sprite = k.getElementIcon();
            img.color = k.kingdomColor.getColorBanner();

            var btn = chip.AddComponent<Button>();
            btn.onClick.AddListener(() => {
                Main.selectedKingdom = k;
                HubUI.Refresh();
                TopPanelUI.Refresh();
            });
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
            
            var le = btnObj.AddComponent<LayoutElement>();
            le.preferredHeight = 26f;
            le.flexibleWidth = 1f;

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

            var h = btnObj.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 4;
            h.padding = new RectOffset(2, 2, 2, 2);
            h.childControlWidth = false;
            h.childForceExpandWidth = false;
            h.childAlignment = TextAnchor.MiddleLeft;

            var flagObj = new GameObject("Flag", typeof(RectTransform));
            flagObj.transform.SetParent(btnObj.transform, false);
            var fLe = flagObj.AddComponent<LayoutElement>();
            fLe.preferredWidth = 16f; fLe.preferredHeight = 22f;
            
            var fBg = flagObj.AddComponent<Image>();
            fBg.sprite = k.getElementBackground();
            fBg.color = k.kingdomColor.getColorMain32();

            var fIco = new GameObject("Ico", typeof(RectTransform));
            fIco.transform.SetParent(flagObj.transform, false);
            Stretch(fIco.GetComponent<RectTransform>());
            var iImg = fIco.AddComponent<Image>();
            iImg.sprite = k.getElementIcon();
            iImg.color = k.kingdomColor.getColorBanner();

            var txt = CreateText(btnObj.transform, k.data.name, 9, FontStyle.Normal, Color.white);
            txt.alignment = TextAnchor.MiddleLeft;
            var txtLE = txt.gameObject.AddComponent<LayoutElement>();
            txtLE.flexibleWidth = 1f;
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
            rt.anchorMin = Vector2.zero; 
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(offset, offset);
            rt.offsetMax = new Vector2(-offset, -offset);
        }
    }
}