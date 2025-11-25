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

        public static void Initialize(Transform parent)
        {
            if (root != null) return;

            // Load standard background resource if available
            windowInnerSprite = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.windowInnerSliced.png");

            // 1. ROOT CONTAINER (Matches EconomyWindow)
            root = new GameObject("DiplomacyRoot");
            root.transform.SetParent(parent, false);

            var rt = root.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Vertical layout for the whole window content
            var rootV = root.AddComponent<VerticalLayoutGroup>();
            rootV.childAlignment = TextAnchor.UpperCenter;
            rootV.spacing = 6;
            rootV.padding = new RectOffset(8, 8, 8, 8);
            rootV.childControlWidth = true;
            rootV.childControlHeight = false; 
            rootV.childForceExpandWidth = true;
            rootV.childForceExpandHeight = false;

            // 2. TITLE 
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
            titleLE.flexibleHeight = 0;

            // 3. HEADER ROW (Flag | Info | Flag)
            CreateHeaderRow(root.transform);

            // ... More content will go here later ...

            root.SetActive(false);
        }

        private static void CreateHeaderRow(Transform parent)
        {
            // Container for the header row
            var headerRow = new GameObject("HeaderRow");
            headerRow.transform.SetParent(parent, false);

            // Optional: Add a background to the header itself
            var bg = headerRow.AddComponent<Image>();
            if (windowInnerSprite != null) { bg.sprite = windowInnerSprite; bg.type = Image.Type.Sliced; }
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);

            var hLayout = headerRow.AddComponent<HorizontalLayoutGroup>();
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.spacing = 15;
            hLayout.padding = new RectOffset(10, 10, 5, 5);
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;
            hLayout.childForceExpandWidth = false; // Don't stretch flags
            hLayout.childForceExpandHeight = false;

            // Fix Height of the header row
            var le = headerRow.AddComponent<LayoutElement>();
            le.preferredHeight = 50f;
            le.minHeight = 50f;
            le.flexibleHeight = 0;

            // === LEFT FLAG ===
            CreateFlag(headerRow.transform, out leftFlagBg, out leftFlagIcon);

            // === CENTER INFO (Name, Ruler, Pop) ===
            var infoCol = new GameObject("InfoColumn");
            infoCol.transform.SetParent(headerRow.transform, false);
            
            var vLayout = infoCol.AddComponent<VerticalLayoutGroup>();
            vLayout.childAlignment = TextAnchor.MiddleCenter;
            vLayout.spacing = 2;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = true;

            // Info column takes all remaining width
            var infoLE = infoCol.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1f; 

            kingdomNameText = CreateText(infoCol.transform, "Kingdom Name", 12, FontStyle.Bold);
            rulerNameText = CreateText(infoCol.transform, "Ruler: ---", 10, FontStyle.Normal);
            populationText = CreateText(infoCol.transform, "Pop: ---", 10, FontStyle.Normal);

            // === RIGHT FLAG ===
            CreateFlag(headerRow.transform, out rightFlagBg, out rightFlagIcon);
        }

        private static void CreateFlag(Transform parent, out Image bgImage, out Image iconImage)
        {
            var wrapper = new GameObject("FlagWrapper");
            wrapper.transform.SetParent(parent, false);
            
            var le = wrapper.AddComponent<LayoutElement>();
            le.preferredWidth = 40f;
            le.preferredHeight = 40f;
            le.minWidth = 40f;
            le.minHeight = 40f;

            // Background (The solid color part)
            var bgObj = new GameObject("FlagBG");
            bgObj.transform.SetParent(wrapper.transform, false);
            bgImage = bgObj.AddComponent<Image>();
            
            var bgRT = bgObj.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;

            // Icon (The symbol)
            var iconObj = new GameObject("FlagIcon");
            iconObj.transform.SetParent(wrapper.transform, false);
            iconImage = iconObj.AddComponent<Image>();
            
            var iconRT = iconObj.GetComponent<RectTransform>();
            iconRT.anchorMin = Vector2.zero; iconRT.anchorMax = Vector2.one;
            // Add slight padding so icon isn't edge-to-edge
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
            txt.fontSize = size;
            txt.fontStyle = style;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.raycastTarget = false;
            return txt;
        }

        public static void SetVisible(bool visible)
        {
            if (root != null) root.SetActive(visible);
        }

        public static bool IsVisible() => root != null && root.activeSelf;

        public static void Refresh(Kingdom k)
        {
            if (!IsVisible() || k == null) return;

            // Set Colors
            if (k.kingdomColor != null)
            {
                Color mainColor = k.kingdomColor.getColorMain32();
                Color bannerColor = k.kingdomColor.getColorBanner();

                leftFlagBg.color = mainColor;
                leftFlagIcon.color = bannerColor;
                rightFlagBg.color = mainColor;
                rightFlagIcon.color = bannerColor;
            }

            // Set Sprites
            Sprite bg = k.getElementBackground();
            Sprite icon = k.getElementIcon();

            leftFlagBg.sprite = bg;
            leftFlagIcon.sprite = icon;
            rightFlagBg.sprite = bg;
            rightFlagIcon.sprite = icon;

            // Set Text
            kingdomNameText.text = k.data.name;
            
            string ruler = "None";
            if (k.king != null) ruler = k.king.getName();
            rulerNameText.text = $"Ruler: {ruler}";

            populationText.text = $"Population: {k.getPopulationTotal()}";
        }
    }
}