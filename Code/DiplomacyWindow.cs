using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace RulerBox
{
    public static class DiplomacyWindow
    {
        private static GameObject root;
        private static Sprite windowInnerSprite;

        // Header References
        private static Image leftFlagBg;
        private static Image leftFlagIcon;
        private static Image rightFlagBg;
        private static Image rightFlagIcon;

        private static Text kingdomNameText;
        private static Text rulerNameText;
        private static Text populationText;

        // Relations strip (Allies / Wars)
        private static RectTransform relationsContentRT;

        // Middle row: search + categories
        private static InputField searchInput;
        private static RectTransform searchListContentRT;
        private static RectTransform categoriesContentRT;

        // Bottom indicators
        private static Text corruptionText;
        private static Text warExhaustionText;
        private static Text politicalPowerText;
        private static Text stabilityText;

        public static void Initialize(Transform parent)
        {
            if (root != null) return;

            // Standard sliced BG
            windowInnerSprite = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.windowInnerSliced.png");

            // ROOT (same pattern as EconomyWindow / InvestmentsWindow)
            root = new GameObject("DiplomacyRoot");
            root.transform.SetParent(parent, false);

            var rt = root.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var v = root.AddComponent<VerticalLayoutGroup>();
            v.childAlignment        = TextAnchor.UpperCenter;
            v.spacing               = 6;
            v.padding               = new RectOffset(8, 8, 8, 8);
            v.childControlWidth     = true;
            v.childControlHeight    = false;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight= false;

            // === TITLE ===
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(root.transform, false);
            var titleText = titleGO.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.text = "Diplomacy";
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;
            titleText.resizeTextForBestFit = true;
            titleText.resizeTextMinSize = 12;
            titleText.resizeTextMaxSize = 16;

            var titleLE = titleGO.AddComponent<LayoutElement>();
            titleLE.minHeight = 24f;
            titleLE.preferredHeight = 24f;
            titleLE.flexibleHeight = 0f;

            // === HEADER (flags + info) ===
            CreateHeaderRow(root.transform);

            // === RELATIONS STRIP (Allies / Wars, horizontal scroll) ===
            CreateRelationsStrip(root.transform);

            // === MIDDLE ROW (Search list + category buttons) ===
            CreateMiddleRow(root.transform);

            // === BOTTOM INDICATORS (Corruption, WarExh, PP, Stability) ===
            CreateIndicatorsRow(root.transform);

            root.SetActive(false);
        }

        // --------------------------------------------------------------------
        //  UI BUILDERS
        // --------------------------------------------------------------------

        private static void CreateHeaderRow(Transform parent)
        {
            var headerRow = new GameObject("HeaderRow");
            headerRow.transform.SetParent(parent, false);

            var bg = headerRow.AddComponent<Image>();
            if (windowInnerSprite != null)
            {
                bg.sprite = windowInnerSprite;
                bg.type   = Image.Type.Sliced;
            }
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);

            var hLayout = headerRow.AddComponent<HorizontalLayoutGroup>();
            hLayout.childAlignment        = TextAnchor.MiddleCenter;
            hLayout.spacing               = 15;
            hLayout.padding               = new RectOffset(10, 10, 5, 5);
            hLayout.childControlWidth     = true;
            hLayout.childControlHeight    = true;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight= false;

            var le = headerRow.AddComponent<LayoutElement>();
            le.preferredHeight = 50f;
            le.minHeight       = 50f;
            le.flexibleHeight  = 0f;

            // Left flag
            CreateFlag(headerRow.transform, out leftFlagBg, out leftFlagIcon);

            // Center info
            var infoCol = new GameObject("InfoColumn");
            infoCol.transform.SetParent(headerRow.transform, false);

            var vLayout = infoCol.AddComponent<VerticalLayoutGroup>();
            vLayout.childAlignment        = TextAnchor.MiddleCenter;
            vLayout.spacing               = 2;
            vLayout.childControlWidth     = true;
            vLayout.childControlHeight    = false;
            vLayout.childForceExpandWidth = true;

            var infoLE = infoCol.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1f;

            kingdomNameText = CreateText(infoCol.transform, "Kingdom Name", 12, FontStyle.Bold);
            rulerNameText   = CreateText(infoCol.transform, "Ruler: ---",    10, FontStyle.Normal);
            populationText  = CreateText(infoCol.transform, "Population: ---",10, FontStyle.Normal);

            // Right flag
            CreateFlag(headerRow.transform, out rightFlagBg, out rightFlagIcon);
        }

        private static void CreateFlag(Transform parent, out Image bgImage, out Image iconImage)
        {
            var wrapper = new GameObject("FlagWrapper");
            wrapper.transform.SetParent(parent, false);

            var le = wrapper.AddComponent<LayoutElement>();
            le.preferredWidth = 40f;
            le.preferredHeight= 40f;
            le.minWidth       = 40f;
            le.minHeight      = 40f;

            var bgObj = new GameObject("FlagBG");
            bgObj.transform.SetParent(wrapper.transform, false);
            bgImage = bgObj.AddComponent<Image>();

            var bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;

            var iconObj = new GameObject("FlagIcon");
            iconObj.transform.SetParent(wrapper.transform, false);
            iconImage = iconObj.AddComponent<Image>();

            var iconRT = iconObj.GetComponent<RectTransform>();
            iconRT.anchorMin = Vector2.zero;
            iconRT.anchorMax = Vector2.one;
            iconRT.offsetMin = new Vector2(2, 2);
            iconRT.offsetMax = new Vector2(-2, -2);
        }

        private static Text CreateText(Transform parent, string content, int size, FontStyle style)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var txt = go.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = content;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.fontSize = size;
            txt.fontStyle = style;
            txt.resizeTextForBestFit = true;
            txt.resizeTextMinSize = 8;
            txt.resizeTextMaxSize = size;
            return txt;
        }

        private static void CreateRelationsStrip(Transform parent)
        {
            var row = new GameObject("RelationsStrip");
            row.transform.SetParent(parent, false);

            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 48f;
            le.flexibleHeight  = 0f;

            var bg = row.AddComponent<Image>();
            if (windowInnerSprite != null)
            {
                bg.sprite = windowInnerSprite;
                bg.type = Image.Type.Sliced;
            }
            else
            {
                bg.color = new Color(0f, 0f, 0f, 0.35f);
            }

            var scroll = row.AddComponent<ScrollRect>();
            scroll.horizontal   = true;
            scroll.vertical     = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia      = true;

            // Viewport
            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(row.transform, false);
            var viewportRT = viewportGO.AddComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = new Vector2(2, 2);
            viewportRT.offsetMax = new Vector2(-2, -2);

            var viewportImg = viewportGO.AddComponent<Image>();
            viewportImg.color = new Color(0, 0, 0, 0.01f);
            viewportGO.AddComponent<Mask>().showMaskGraphic = false;

            // Content (horizontal)
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);
            relationsContentRT = contentGO.AddComponent<RectTransform>();
            relationsContentRT.anchorMin = new Vector2(0, 0);
            relationsContentRT.anchorMax = new Vector2(0, 1);
            relationsContentRT.pivot     = new Vector2(0, 0.5f);

            var h = contentGO.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment        = TextAnchor.MiddleLeft;
            h.spacing               = 4;
            h.padding               = new RectOffset(4, 4, 4, 4);
            h.childControlWidth     = true;
            h.childControlHeight    = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight= false;

            var fitter = contentGO.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit   = ContentSizeFitter.FitMode.Unconstrained;

            scroll.viewport = viewportRT;
            scroll.content  = relationsContentRT;
        }

        private static void CreateMiddleRow(Transform parent)
        {
            var row = new GameObject("MiddleRow");
            row.transform.SetParent(parent, false);

            var hl = row.AddComponent<HorizontalLayoutGroup>();
            hl.childAlignment        = TextAnchor.UpperCenter;
            hl.spacing               = 6;
            hl.padding               = new RectOffset(0, 0, 0, 0);
            hl.childControlWidth     = true;
            hl.childControlHeight    = true;
            hl.childForceExpandWidth = true;
            hl.childForceExpandHeight= true;

            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 120f;
            le.flexibleHeight  = 1f;

            // LEFT: Search panel
            CreateSearchPanel(row.transform);

            // RIGHT: Categories panel
            CreateCategoriesPanel(row.transform);
        }

        private static void CreateSearchPanel(Transform parent)
        {
            var panel = new GameObject("SearchPanel");
            panel.transform.SetParent(parent, false);

            var le = panel.AddComponent<LayoutElement>();
            le.flexibleWidth = 2f;

            var bg = panel.AddComponent<Image>();
            if (windowInnerSprite != null)
            {
                bg.sprite = windowInnerSprite;
                bg.type   = Image.Type.Sliced;
            }

            var v = panel.AddComponent<VerticalLayoutGroup>();
            v.childAlignment        = TextAnchor.UpperCenter;
            v.spacing               = 4;
            v.padding               = new RectOffset(4, 4, 4, 4);
            v.childControlWidth     = true;
            v.childControlHeight    = false;
            v.childForceExpandWidth = true;

            // Search label
            var labelGO = new GameObject("SearchLabel");
            labelGO.transform.SetParent(panel.transform, false);
            var label = labelGO.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.text = "Search Country";
            label.alignment = TextAnchor.MiddleLeft;
            label.color = Color.white;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 8;
            label.resizeTextMaxSize = 12;
            labelGO.AddComponent<LayoutElement>().preferredHeight = 18f;

            // InputField
            var inputGO = new GameObject("SearchInput");
            inputGO.transform.SetParent(panel.transform, false);

            var inputBG = inputGO.AddComponent<Image>();
            inputBG.color = new Color(0f, 0f, 0f, 0.4f);

            var inputRT = inputGO.AddComponent<RectTransform>();
            inputRT.anchorMin = new Vector2(0, 0);
            inputRT.anchorMax = new Vector2(1, 0);
            inputRT.sizeDelta = new Vector2(0, 22f);

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(inputGO.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = new Vector2(0, 0);
            textRT.anchorMax = new Vector2(1, 1);
            textRT.offsetMin = new Vector2(4, 2);
            textRT.offsetMax = new Vector2(-4, -2);

            var inputText = textGO.AddComponent<Text>();
            inputText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            inputText.text = "";
            inputText.alignment = TextAnchor.MiddleLeft;
            inputText.color = Color.white;

            searchInput = inputGO.AddComponent<InputField>();
            searchInput.textComponent = inputText;
            searchInput.placeholder = null;

            inputGO.AddComponent<LayoutElement>().preferredHeight = 22f;

            // Scroll with list of countries (buttons)
            var scrollGO = new GameObject("SearchListScroll");
            scrollGO.transform.SetParent(panel.transform, false);
            var scrollLE = scrollGO.AddComponent<LayoutElement>();
            scrollLE.preferredHeight = 80f;
            scrollLE.flexibleHeight = 1f;

            var scrollBG = scrollGO.AddComponent<Image>();
            scrollBG.color = new Color(0f, 0f, 0f, 0.3f);

            var scroll = scrollGO.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(scrollGO.transform, false);
            var viewportRT = viewportGO.AddComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = new Vector2(2, 2);
            viewportRT.offsetMax = new Vector2(-2, -2);

            viewportGO.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            viewportGO.AddComponent<Mask>().showMaskGraphic = false;

            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);
            searchListContentRT = contentGO.AddComponent<RectTransform>();
            searchListContentRT.anchorMin = new Vector2(0, 1);
            searchListContentRT.anchorMax = new Vector2(1, 1);
            searchListContentRT.pivot     = new Vector2(0.5f, 1f);

            var vContent = contentGO.AddComponent<VerticalLayoutGroup>();
            vContent.childAlignment        = TextAnchor.UpperCenter;
            vContent.spacing               = 2;
            vContent.padding               = new RectOffset(2, 2, 2, 2);
            vContent.childControlWidth     = true;
            vContent.childControlHeight    = true;
            vContent.childForceExpandWidth = true;
            vContent.childForceExpandHeight= false;

            var fitter = contentGO.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewportRT;
            scroll.content  = searchListContentRT;
        }

        private static void CreateCategoriesPanel(Transform parent)
        {
            var panel = new GameObject("CategoriesPanel");
            panel.transform.SetParent(parent, false);

            var le = panel.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;

            var bg = panel.AddComponent<Image>();
            if (windowInnerSprite != null)
            {
                bg.sprite = windowInnerSprite;
                bg.type   = Image.Type.Sliced;
            }

            var v = panel.AddComponent<VerticalLayoutGroup>();
            v.childAlignment        = TextAnchor.UpperCenter;
            v.spacing               = 4;
            v.padding               = new RectOffset(4, 4, 4, 4);
            v.childControlWidth     = true;
            v.childControlHeight    = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight= false;

            // Content holder for possible future scroll / dynamic
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(panel.transform, false);
            categoriesContentRT = contentGO.AddComponent<RectTransform>();

            var v2 = contentGO.AddComponent<VerticalLayoutGroup>();
            v2.childAlignment        = TextAnchor.UpperCenter;
            v2.spacing               = 4;
            v2.childControlWidth     = true;
            v2.childControlHeight    = true;
            v2.childForceExpandWidth = true;
            v2.childForceExpandHeight= false;

            // Static buttons for now – logic will be added later
            BuildCategoryButton(contentGO.transform, "Laws");
            BuildCategoryButton(contentGO.transform, "Leaders");
            BuildCategoryButton(contentGO.transform, "Policies");
            BuildCategoryButton(contentGO.transform, "Ideologies");
            BuildCategoryButton(contentGO.transform, "National Flags");
        }

        private static void BuildCategoryButton(Transform parent, string label)
        {
            var btnGO = new GameObject(label.Replace(" ", "") + "Button");
            btnGO.transform.SetParent(parent, false);

            var img = btnGO.AddComponent<Image>();
            if (windowInnerSprite != null)
            {
                img.sprite = windowInnerSprite;
                img.type   = Image.Type.Sliced;
            }
            img.color = new Color(0f, 0f, 0f, 0.7f);

            var btn = btnGO.AddComponent<Button>();
            // TODO: hook up to sub-windows later

            var le = btnGO.AddComponent<LayoutElement>();
            le.preferredHeight = 22f;

            var textGO = new GameObject("Label");
            textGO.transform.SetParent(btnGO.transform, false);
            var txt = textGO.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = label;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            var tr = textGO.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero;
            tr.offsetMax = Vector2.zero;
        }

        private static void CreateIndicatorsRow(Transform parent)
        {
            var row = new GameObject("IndicatorsRow");
            row.transform.SetParent(parent, false);

            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment        = TextAnchor.MiddleCenter;
            h.spacing               = 4;
            h.padding               = new RectOffset(2, 2, 2, 2);
            h.childControlWidth     = true;
            h.childControlHeight    = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight= false;

            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 26f;
            le.flexibleHeight  = 0f;

            BuildIndicatorChip(row.transform, "Corruption",     out corruptionText);
            BuildIndicatorChip(row.transform, "War Exhaustion", out warExhaustionText);
            BuildIndicatorChip(row.transform, "Political Power",out politicalPowerText);
            BuildIndicatorChip(row.transform, "Stability",      out stabilityText);
        }

        private static void BuildIndicatorChip(Transform parent, string label, out Text valueText)
        {
            var chip = new GameObject(label.Replace(" ", "") + "Chip");
            chip.transform.SetParent(parent, false);

            var bg = chip.AddComponent<Image>();
            if (windowInnerSprite != null)
            {
                bg.sprite = windowInnerSprite;
                bg.type   = Image.Type.Sliced;
            }
            bg.color = new Color(0f, 0f, 0f, 0.8f);

            var h = chip.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment        = TextAnchor.MiddleCenter;
            h.spacing               = 2;
            h.padding               = new RectOffset(4, 4, 2, 2);
            h.childControlWidth     = true;
            h.childControlHeight    = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight= false;

            var le = chip.AddComponent<LayoutElement>();
            le.preferredHeight = 22f;
            le.flexibleWidth   = 1f;

            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(chip.transform, false);
            var labelText = labelGO.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.text = label + ":";
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = Color.white;
            labelText.resizeTextForBestFit = true;
            labelText.resizeTextMinSize = 7;
            labelText.resizeTextMaxSize = 10;

            var valGO = new GameObject("Value");
            valGO.transform.SetParent(chip.transform, false);
            valueText = valGO.AddComponent<Text>();
            valueText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            valueText.text = "0";
            valueText.alignment = TextAnchor.MiddleRight;
            valueText.color = Color.white;
            valueText.resizeTextForBestFit = true;
            valueText.resizeTextMinSize = 7;
            valueText.resizeTextMaxSize = 10;
        }

        // --------------------------------------------------------------------
        //  VISIBILITY + REFRESH
        // --------------------------------------------------------------------

        public static void SetVisible(bool visible)
        {
            if (root != null) root.SetActive(visible);
        }

        public static bool IsVisible()
        {
            return root != null && root.activeSelf;
        }

        public static void Refresh(Kingdom k)
        {
            if (!IsVisible() || k == null) return;

            // Header basics
            try
            {
                if (kingdomNameText != null)
                    kingdomNameText.text = k.data.name;

                if (populationText != null)
                    populationText.text = "Population: " + k.getPopulationPeople();

                // Ruler name is a bit tricky in vanilla; keep generic for now
                if (rulerNameText != null)
                    rulerNameText.text = "Ruler: (unknown)";
            }
            catch { }

            // TODO: set actual flag sprites & colors once we hook into kingdom data fields.

            // Clear relations & search lists for now – logic will be added when we wire diplomacy
            if (relationsContentRT != null)
            {
                foreach (Transform child in relationsContentRT)
                    GameObject.Destroy(child.gameObject);
            }
            if (searchListContentRT != null)
            {
                foreach (Transform child in searchListContentRT)
                    GameObject.Destroy(child.gameObject);
            }

            // Indicators – plug into metrics system when you’re ready
            var d = KingdomMetricsSystem.Get(k);
            if (d != null)
            {
                // NOTE: adjust field names to match your Data struct (these are placeholders).
                if (corruptionText     != null) corruptionText.text     = "0%"; // d.Corruption * 100f + "%";
                if (warExhaustionText != null) warExhaustionText.text = "0%"; // d.WarExhaustion * 100f + "%";
                if (politicalPowerText!= null) politicalPowerText.text= "0";  // d.PoliticalPower.ToString();
                if (stabilityText     != null) stabilityText.text     = d.Stability.ToString("0"); // Stability already used in events
            }
        }
    }
}
