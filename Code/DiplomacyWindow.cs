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
        private static Sprite bgSprite;

        // Header References
        private static Image flagBg;
        private static Image flagIcon;
        private static Text kingdomNameText;
        private static Text rulerNameText;
        private static Text populationText;

        // Relations
        private static Transform relationsContent;

        // Search
        private static InputField searchInput;
        private static Transform kingdomSearchContent;

        // Actions
        private static Transform actionsContent;

        // Indicators
        private static Text corruptionText;
        private static Text warExhaustionText;
        private static Text politicalPowerText;
        private static Text stabilityText;

        public static void Initialize(Transform parent)
        {
            if (root != null) return;

            windowInnerSprite = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.windowInnerSliced.png");
            bgSprite = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.InvHub.png");

            // 1. Root Object (The "Container")
            root = new GameObject("DiplomacyRoot");
            root.transform.SetParent(parent, false);

            var rt = root.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var rootV = root.AddComponent<VerticalLayoutGroup>();
            rootV.childAlignment = TextAnchor.UpperCenter;
            rootV.spacing = 4;
            rootV.padding = new RectOffset(4, 4, 4, 4);
            rootV.childControlWidth = true;
            rootV.childControlHeight = true;
            rootV.childForceExpandWidth = true;
            rootV.childForceExpandHeight = false;

            // 2. Title Row
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(root.transform, false);
            var title = titleGO.AddComponent<Text>();
            title.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            title.text = "Diplomacy";
            title.alignment = TextAnchor.MiddleCenter;
            title.color = Color.white;
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 10;
            title.resizeTextMaxSize = 14;
            titleGO.AddComponent<LayoutElement>().preferredHeight = 20f;

            // 3. Info Header (Flag + Text)
            BuildHeaderSection(root.transform);

            // 4. Horizontal Relations Scroll (Allies/Wars)
            BuildRelationsSection(root.transform);

            // 5. Main Split Section (Search Left | Actions Right)
            BuildSplitSection(root.transform);

            // 6. Footer Indicators
            BuildIndicatorsSection(root.transform);

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

            // Update Header
            if (k.kingdomColor != null)
            {
                flagBg.color = k.kingdomColor.getColorMain32();
                flagIcon.color = k.kingdomColor.getColorBanner();
            }
            flagBg.sprite = k.getElementBackground();
            flagIcon.sprite = k.getElementIcon();

            kingdomNameText.text = k.data.name;
            string ruler = k.king != null ? k.king.getName() : "None";
            rulerNameText.text = $"Ruler: {ruler}";
            populationText.text = $"Population: {k.getPopulationTotal()}";

            // Update Relations
            RefreshRelations(k);

            // Update Search List (only if input changed or first load)
            RefreshSearchList(searchInput.text);

            // Update Indicators
            var d = KingdomMetricsSystem.Get(k);
            if (d != null)
            {
                corruptionText.text = $"Corr: {d.CorruptionLevel * 100:0}%";
                warExhaustionText.text = $"WE: {d.WarExhaustion:0}";
                politicalPowerText.text = $"PP: {k.data.renown}"; // Placeholder
                stabilityText.text = $"Stab: {d.Stability:0}%";
            }
        }

        // ==============================================================================================
        // SECTION BUILDERS
        // ==============================================================================================

        private static void BuildHeaderSection(Transform parent)
        {
            var headerObj = new GameObject("HeaderSection");
            headerObj.transform.SetParent(parent, false);
            
            var hl = headerObj.AddComponent<HorizontalLayoutGroup>();
            hl.childAlignment = TextAnchor.MiddleLeft;
            hl.spacing = 10;
            hl.padding = new RectOffset(4, 4, 0, 0);
            hl.childControlWidth = true;
            hl.childControlHeight = true;
            hl.childForceExpandWidth = true;

            headerObj.AddComponent<LayoutElement>().preferredHeight = 40f;

            // Flag Box
            var flagBox = new GameObject("FlagBox");
            flagBox.transform.SetParent(headerObj.transform, false);
            flagBox.AddComponent<LayoutElement>().preferredWidth = 36f;
            
            var flagBgObj = new GameObject("FlagBg");
            flagBgObj.transform.SetParent(flagBox.transform, false);
            flagBg = flagBgObj.AddComponent<Image>();
            flagBg.rectTransform.anchorMin = Vector2.zero;
            flagBg.rectTransform.anchorMax = Vector2.one;

            var flagIconObj = new GameObject("FlagIcon");
            flagIconObj.transform.SetParent(flagBox.transform, false);
            flagIcon = flagIconObj.AddComponent<Image>();
            flagIcon.rectTransform.anchorMin = Vector2.zero;
            flagIcon.rectTransform.anchorMax = Vector2.one;
            flagIcon.rectTransform.offsetMin = new Vector2(2, 2);
            flagIcon.rectTransform.offsetMax = new Vector2(-2, -2);

            // Text Info
            var infoCol = new GameObject("InfoCol");
            infoCol.transform.SetParent(headerObj.transform, false);
            var infoVL = infoCol.AddComponent<VerticalLayoutGroup>();
            infoVL.childAlignment = TextAnchor.MiddleLeft;
            infoVL.spacing = 2;
            infoCol.AddComponent<LayoutElement>().flexibleWidth = 1f;

            kingdomNameText = CreateText(infoCol.transform, "-", 10, FontStyle.Bold, TextAnchor.MiddleLeft);
            rulerNameText = CreateText(infoCol.transform, "-", 8, FontStyle.Normal, TextAnchor.MiddleLeft);
            populationText = CreateText(infoCol.transform, "-", 8, FontStyle.Normal, TextAnchor.MiddleLeft);
        }

        private static void BuildRelationsSection(Transform parent)
        {
            // Container for label + scroll
            var sectionObj = new GameObject("RelationsSection");
            sectionObj.transform.SetParent(parent, false);
            var vl = sectionObj.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 2;
            sectionObj.AddComponent<LayoutElement>().preferredHeight = 40f;

            // Label? (Optional, skipping to save space, or add small one)
            // Scroll View
            var scrollObj = new GameObject("RelationsScroll");
            scrollObj.transform.SetParent(sectionObj.transform, false);
            scrollObj.AddComponent<LayoutElement>().flexibleHeight = 1f;

            var bg = scrollObj.AddComponent<Image>();
            bg.sprite = windowInnerSprite;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0, 0, 0, 0.3f);

            var scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            var vpRT = viewport.AddComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = new Vector2(2, 2); vpRT.offsetMax = new Vector2(-2, -2);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            viewport.AddComponent<Image>().color = Color.clear;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            relationsContent = content.transform;
            
            var contentHL = content.AddComponent<HorizontalLayoutGroup>();
            contentHL.childAlignment = TextAnchor.MiddleLeft;
            contentHL.spacing = 4;
            contentHL.padding = new RectOffset(2, 2, 2, 2);
            contentHL.childControlWidth = false;
            contentHL.childControlHeight = false;

            var contentFitter = content.AddComponent<ContentSizeFitter>();
            contentFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            var cRT = content.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 0); cRT.anchorMax = new Vector2(0, 1);
            cRT.pivot = new Vector2(0, 0.5f);

            scroll.viewport = vpRT;
            scroll.content = cRT;
        }

        private static void BuildSplitSection(Transform parent)
        {
            var splitRow = new GameObject("SplitSection");
            splitRow.transform.SetParent(parent, false);
            var h = splitRow.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 4;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;

            // Flexible height to fill remaining space
            var le = splitRow.AddComponent<LayoutElement>();
            le.flexibleHeight = 1f;

            // --- LEFT: Search & List ---
            var leftCol = new GameObject("LeftCol");
            leftCol.transform.SetParent(splitRow.transform, false);
            var leftV = leftCol.AddComponent<VerticalLayoutGroup>();
            leftV.spacing = 2;

            // Search Input
            var searchObj = new GameObject("SearchInput");
            searchObj.transform.SetParent(leftCol.transform, false);
            searchObj.AddComponent<LayoutElement>().preferredHeight = 20f;
            
            var sImg = searchObj.AddComponent<Image>();
            sImg.sprite = windowInnerSprite; sImg.type = Image.Type.Sliced;
            sImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            searchInput = searchObj.AddComponent<InputField>();
            var ph = CreateText(searchObj.transform, "Search...", 10, FontStyle.Italic, TextAnchor.MiddleLeft);
            ph.color = new Color(1, 1, 1, 0.5f);
            var txt = CreateText(searchObj.transform, "", 10, FontStyle.Normal, TextAnchor.MiddleLeft);
            searchInput.placeholder = ph;
            searchInput.textComponent = txt;
            searchInput.onValueChanged.AddListener(RefreshSearchList);

            // Scroll List
            var searchScroll = CreateVerticalScroll(leftCol.transform, "SearchList");
            kingdomSearchContent = searchScroll.GetComponent<ScrollRect>().content;

            // --- RIGHT: Actions ---
            var rightCol = new GameObject("RightCol");
            rightCol.transform.SetParent(splitRow.transform, false);
            var rightV = rightCol.AddComponent<VerticalLayoutGroup>();
            rightV.spacing = 2;

            // Header "Decisions / Laws"
            var rightHeader = CreateText(rightCol.transform, "Actions", 10, FontStyle.Bold, TextAnchor.MiddleCenter);
            rightHeader.GetComponent<LayoutElement>().preferredHeight = 16f;

            // Scroll Actions
            var actionsScroll = CreateVerticalScroll(rightCol.transform, "ActionsList");
            actionsContent = actionsScroll.GetComponent<ScrollRect>().content;
        }

        private static void BuildIndicatorsSection(Transform parent)
        {
            var row = new GameObject("IndicatorsRow");
            row.transform.SetParent(parent, false);
            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleCenter;
            h.spacing = 10;
            h.padding = new RectOffset(4, 4, 2, 2);
            
            row.AddComponent<LayoutElement>().preferredHeight = 24f;

            // 4 Indicators
            corruptionText = BuildSingleIndicator(row.transform, "iconSkull", Color.red);
            warExhaustionText = BuildSingleIndicator(row.transform, "iconWar", Color.red);
            politicalPowerText = BuildSingleIndicator(row.transform, "iconKingdom", Color.yellow);
            stabilityText = BuildSingleIndicator(row.transform, "iconPeace", Color.cyan);
        }

        private static Text BuildSingleIndicator(Transform parent, string iconName, Color iconColor)
        {
            var indObj = new GameObject("Ind_" + iconName);
            indObj.transform.SetParent(parent, false);
            var h = indObj.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 4;
            h.childControlWidth = true;
            h.childForceExpandWidth = false;

            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(indObj.transform, false);
            var img = iconObj.AddComponent<Image>();
            var sprite = Resources.Load<Sprite>("ui/Icons/" + iconName);
            if (sprite == null) sprite = windowInnerSprite; // Fallback
            img.sprite = sprite;
            img.color = iconColor;
            
            var le = iconObj.AddComponent<LayoutElement>();
            le.preferredWidth = 16; le.preferredHeight = 16;

            var txt = CreateText(indObj.transform, "0", 10, FontStyle.Bold, TextAnchor.MiddleLeft);
            return txt;
        }

        private static GameObject CreateVerticalScroll(Transform parent, string name)
        {
            var scrollObj = new GameObject(name);
            scrollObj.transform.SetParent(parent, false);
            scrollObj.AddComponent<LayoutElement>().flexibleHeight = 1f;

            var bg = scrollObj.AddComponent<Image>();
            bg.sprite = windowInnerSprite; bg.type = Image.Type.Sliced;
            bg.color = new Color(0, 0, 0, 0.2f);

            var scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.vertical = true; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            var vpRT = viewport.AddComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = new Vector2(2, 2); vpRT.offsetMax = new Vector2(-2, -2);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            viewport.AddComponent<Image>().color = Color.clear;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var cRT = content.AddComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 1); cRT.anchorMax = new Vector2(1, 1);
            cRT.pivot = new Vector2(0.5f, 1);

            var v = content.AddComponent<VerticalLayoutGroup>();
            v.spacing = 2;
            v.childControlWidth = true;
            v.childForceExpandWidth = true;
            v.childControlHeight = true;
            v.childForceExpandHeight = false;

            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = vpRT;
            scroll.content = cRT;
            return scrollObj;
        }

        // ==============================================================================================
        // LOGIC & UPDATES
        // ==============================================================================================

        private static void RefreshRelations(Kingdom k)
        {
            foreach (Transform child in relationsContent) Object.Destroy(child.gameObject);

            // 1. Allies
            if (k.hasAlliance())
            {
                foreach (var ally in k.getAlliance().kingdoms_list)
                {
                    if (ally != k && ally.isAlive())
                        CreateRelationChip(ally, Color.green);
                }
            }

            // 2. Wars
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

            var inner = new GameObject("Icon");
            inner.transform.SetParent(chip.transform, false);
            var innerRT = inner.AddComponent<RectTransform>();
            innerRT.anchorMin = Vector2.zero; innerRT.anchorMax = Vector2.one;
            innerRT.offsetMin = new Vector2(2, 2); innerRT.offsetMax = new Vector2(-2, -2);

            var flagBg = inner.AddComponent<Image>();
            flagBg.sprite = k.getElementBackground();
            flagBg.color = k.kingdomColor.getColorMain32();

            var icon = new GameObject("IconImg").AddComponent<Image>();
            icon.transform.SetParent(inner.transform, false);
            icon.rectTransform.anchorMin = Vector2.zero; icon.rectTransform.anchorMax = Vector2.one;
            icon.sprite = k.getElementIcon();
            icon.color = k.kingdomColor.getColorBanner();

            var btn = chip.AddComponent<Button>();
            btn.onClick.AddListener(() => {
                Main.selectedKingdom = k;
                HubUI.Refresh();
                TopPanelUI.Refresh();
            });
        }

        private static void RefreshSearchList(string filter)
        {
            foreach (Transform child in kingdomSearchContent) Object.Destroy(child.gameObject);

            foreach (var k in World.world.kingdoms.list)
            {
                if (!k.isAlive() || k.data.id == Globals.NEUTRAL_KINGDOM_NUMERIC_ID) continue;
                if (!string.IsNullOrEmpty(filter) && !k.data.name.ToLower().Contains(filter.ToLower())) continue;

                CreateKingdomListItem(k);
            }
        }

        private static void CreateKingdomListItem(Kingdom k)
        {
            var item = new GameObject("Item_" + k.data.name);
            item.transform.SetParent(kingdomSearchContent, false);
            var le = item.AddComponent<LayoutElement>();
            le.preferredHeight = 24f;

            var img = item.AddComponent<Image>();
            img.sprite = windowInnerSprite; img.type = Image.Type.Sliced;
            img.color = new Color(0.25f, 0.25f, 0.25f, 1f);

            var btn = item.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => {
                Main.selectedKingdom = k;
                HubUI.Refresh();
                TopPanelUI.Refresh();
            });

            var h = item.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(2, 2, 2, 2);
            h.spacing = 4;
            h.childForceExpandWidth = false;

            // Tiny Flag
            var fObj = new GameObject("Flag");
            fObj.transform.SetParent(item.transform, false);
            var fLe = fObj.AddComponent<LayoutElement>();
            fLe.preferredWidth = 14; fLe.preferredHeight = 20;
            var fImg = fObj.AddComponent<Image>();
            fImg.sprite = k.getElementBackground();
            fImg.color = k.kingdomColor.getColorMain32();
            
            var iObj = new GameObject("Icon");
            iObj.transform.SetParent(fObj.transform, false);
            var iImg = iObj.AddComponent<Image>();
            iImg.sprite = k.getElementIcon();
            iImg.color = k.kingdomColor.getColorBanner();
            iImg.rectTransform.anchorMin = Vector2.zero; iImg.rectTransform.anchorMax = Vector2.one;

            // Name
            var nameTxt = CreateText(item.transform, k.data.name, 10, FontStyle.Normal, TextAnchor.MiddleLeft);
            nameTxt.GetComponent<LayoutElement>().flexibleWidth = 1f;
        }

        private static void CreateActionsList(Transform parent)
        {
            // Re-create static list of buttons if needed, or call once
            foreach (Transform child in actionsContent) Object.Destroy(child.gameObject);

            CreateActionButton("Laws", () => TopPanelUI.OpenEconomicLaws());
            CreateActionButton("Leaders", () => WorldTip.showNow("Leaders: Coming Soon", false, "top", 1f));
            CreateActionButton("Policies", () => WorldTip.showNow("Policies: Coming Soon", false, "top", 1f));
            CreateActionButton("Ideologies", () => WorldTip.showNow("Ideologies: Coming Soon", false, "top", 1f));
            CreateActionButton("National Flags", () => WorldTip.showNow("National Flags: Coming Soon", false, "top", 1f));
        }

        private static void CreateActionButton(string label, System.Action onClick)
        {
            var btnObj = new GameObject("Btn_" + label);
            btnObj.transform.SetParent(actionsContent, false);
            var le = btnObj.AddComponent<LayoutElement>();
            le.preferredHeight = 24f;

            var img = btnObj.AddComponent<Image>();
            img.sprite = windowInnerSprite; img.type = Image.Type.Sliced;
            img.color = new Color(0.3f, 0.3f, 0.4f, 1f);

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());

            CreateText(btnObj.transform, label, 10, FontStyle.Normal, TextAnchor.MiddleCenter);
        }

        // Helper
        private static Text CreateText(Transform parent, string content, int fontSize, FontStyle style, TextAnchor align)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var txt = go.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = content;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.alignment = align;
            txt.color = Color.white;
            txt.raycastTarget = false;
            
            var rt = txt.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            
            return txt;
        }
    }
}