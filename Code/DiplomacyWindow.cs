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
        
        // Header UI Elements
        private static Image headerFlagBg;
        private static Image headerFlagIcon;
        private static Text headerKingdomName;
        private static Text headerRulerName;
        private static Text headerPopulation;

        // Relations UI Elements
        private static Transform relationsContent;

        // Search UI Elements
        private static InputField searchInput;
        private static Transform kingdomListContent;

        // Actions UI Elements
        private static Transform actionsContent;

        // Bottom Indicators
        private static Text textCorruption;
        private static Text textWarExhaustion;
        private static Text textPoliticalPower;
        private static Text textStability;

        public static void Initialize(Transform parent)
        {
            if (root != null) return;

            windowInnerSprite = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.windowInnerSliced.png");
            // Using InvHub as generic background if available, otherwise fallback
            bgSprite = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.InvHub.png"); 

            // === 1. Root Window Setup ===
            root = new GameObject("DiplomacyWindowRoot");
            root.transform.SetParent(parent, false);
            
            var rt = root.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            var vGroup = root.AddComponent<VerticalLayoutGroup>();
            vGroup.childAlignment = TextAnchor.UpperCenter;
            vGroup.spacing = 4;
            vGroup.padding = new RectOffset(6, 6, 6, 6);
            vGroup.childControlWidth = true;
            vGroup.childControlHeight = false;
            vGroup.childForceExpandWidth = true;
            vGroup.childForceExpandHeight = false;

            // === 2. Title ===
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(root.transform, false);
            var titleText = titleObj.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.text = "Diplomacy";
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;
            titleText.resizeTextForBestFit = true;
            titleText.resizeTextMinSize = 12;
            titleText.resizeTextMaxSize = 16;
            titleObj.AddComponent<LayoutElement>().preferredHeight = 24f;

            // === 3. Header (Flag | Info) ===
            CreateHeader(root.transform);

            // === 4. Horizontal Relations Scroll (Allies/Wars) ===
            CreateRelationsScroll(root.transform);

            // === 5. Middle Split (Search List | Actions List) ===
            var midRow = new GameObject("MiddleRow");
            midRow.transform.SetParent(root.transform, false);
            var midLayout = midRow.AddComponent<HorizontalLayoutGroup>();
            midLayout.spacing = 4;
            midLayout.childControlWidth = true;
            midLayout.childControlHeight = true;
            midLayout.childForceExpandWidth = true;
            midLayout.childForceExpandHeight = true;
            var midLE = midRow.AddComponent<LayoutElement>();
            midLE.flexibleHeight = 1f; // Take remaining vertical space

            // Left Column: Search Bar & Kingdom List
            CreateKingdomSearchList(midRow.transform);

            // Right Column: Actions Buttons
            CreateActionsList(midRow.transform);

            // === 6. Bottom Indicators ===
            CreateIndicators(root.transform);

            root.SetActive(false);
        }

        private static void CreateHeader(Transform parent)
        {
            var headerObj = new GameObject("Header");
            headerObj.transform.SetParent(parent, false);
            var hLayout = headerObj.AddComponent<HorizontalLayoutGroup>();
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.spacing = 10;
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = false;
            headerObj.AddComponent<LayoutElement>().preferredHeight = 50f;

            // Flag Container
            var flagObj = new GameObject("Flag");
            flagObj.transform.SetParent(headerObj.transform, false);
            var flagRT = flagObj.AddComponent<RectTransform>();
            flagRT.sizeDelta = new Vector2(40, 40);
            
            headerFlagBg = flagObj.AddComponent<Image>();
            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(flagObj.transform, false);
            var iconRT = iconObj.AddComponent<RectTransform>();
            iconRT.anchorMin = Vector2.zero; iconRT.anchorMax = Vector2.one;
            iconRT.offsetMin = Vector2.zero; iconRT.offsetMax = Vector2.zero;
            headerFlagIcon = iconObj.AddComponent<Image>();

            // Text Info Column
            var infoObj = new GameObject("Info");
            infoObj.transform.SetParent(headerObj.transform, false);
            var vLayout = infoObj.AddComponent<VerticalLayoutGroup>();
            vLayout.childAlignment = TextAnchor.MiddleLeft;
            vLayout.spacing = 2;
            vLayout.childControlWidth = true; 
            vLayout.childControlHeight = false; 
            var infoLE = infoObj.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1f;
            infoLE.preferredHeight = 50f;

            headerKingdomName = CreateText(infoObj.transform, "Kingdom Name", 12, FontStyle.Bold);
            headerRulerName = CreateText(infoObj.transform, "Ruler: ---", 10, FontStyle.Normal);
            headerPopulation = CreateText(infoObj.transform, "Population: 0", 10, FontStyle.Normal);
        }

        private static void CreateRelationsScroll(Transform parent)
        {
            var scrollObj = new GameObject("RelationsScroll");
            scrollObj.transform.SetParent(parent, false);
            scrollObj.AddComponent<LayoutElement>().preferredHeight = 45f;

            var bg = scrollObj.AddComponent<Image>();
            if (windowInnerSprite != null) { bg.sprite = windowInnerSprite; bg.type = Image.Type.Sliced; }
            bg.color = new Color(0, 0, 0, 0.3f);

            var scrollRect = scrollObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            
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
            var cRT = content.AddComponent<RectTransform>();
            // Horizontal scroll setup
            cRT.anchorMin = new Vector2(0, 0); cRT.anchorMax = new Vector2(0, 1);
            cRT.pivot = new Vector2(0, 0.5f);
            
            var hLayout = content.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 4;
            hLayout.padding = new RectOffset(4, 4, 4, 4);
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = false;
            
            content.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = vpRT;
            scrollRect.content = cRT;
        }

        private static void CreateKingdomSearchList(Transform parent)
        {
            var container = new GameObject("KingdomListContainer");
            container.transform.SetParent(parent, false);
            container.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var vLayout = container.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 2;

            // Search Bar
            var searchObj = new GameObject("SearchInput");
            searchObj.transform.SetParent(container.transform, false);
            searchObj.AddComponent<LayoutElement>().preferredHeight = 20f;
            var sImg = searchObj.AddComponent<Image>();
            if (windowInnerSprite != null) { sImg.sprite = windowInnerSprite; sImg.type = Image.Type.Sliced; }
            sImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            
            searchInput = searchObj.AddComponent<InputField>();
            var placeHolder = CreateText(searchObj.transform, "Search...", 10, FontStyle.Italic);
            placeHolder.color = new Color(1,1,1,0.5f);
            var text = CreateText(searchObj.transform, "", 10, FontStyle.Normal);
            
            searchInput.placeholder = placeHolder;
            searchInput.textComponent = text;
            searchInput.onValueChanged.AddListener(OnSearchChanged);

            // Scroll View
            var scrollObj = new GameObject("Scroll");
            scrollObj.transform.SetParent(container.transform, false);
            scrollObj.AddComponent<LayoutElement>().flexibleHeight = 1f;
            
            var bg = scrollObj.AddComponent<Image>();
            if (windowInnerSprite != null) { bg.sprite = windowInnerSprite; bg.type = Image.Type.Sliced; }
            bg.color = new Color(0,0,0,0.2f);

            var scrollRect = scrollObj.AddComponent<ScrollRect>();
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            var vpRT = viewport.AddComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = new Vector2(2, 2); vpRT.offsetMax = new Vector2(-2, -2);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            viewport.AddComponent<Image>().color = Color.clear;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            kingdomListContent = content.transform;
            var cRT = content.AddComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 1); cRT.anchorMax = new Vector2(1, 1);
            cRT.pivot = new Vector2(0.5f, 1);

            var vContent = content.AddComponent<VerticalLayoutGroup>();
            vContent.spacing = 2;
            vContent.childControlWidth = true;
            vContent.childControlHeight = false;
            vContent.childForceExpandWidth = true;

            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = vpRT;
            scrollRect.content = cRT;
        }

        private static void CreateActionsList(Transform parent)
        {
            var container = new GameObject("ActionsListContainer");
            container.transform.SetParent(parent, false);
            container.AddComponent<LayoutElement>().flexibleWidth = 1f; // Equal width to search list
            
            var bg = container.AddComponent<Image>();
            if (windowInnerSprite != null) { bg.sprite = windowInnerSprite; bg.type = Image.Type.Sliced; }
            bg.color = new Color(0,0,0,0.2f);

            var scrollRect = container.AddComponent<ScrollRect>();
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(container.transform, false);
            var vpRT = viewport.AddComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = new Vector2(2, 2); vpRT.offsetMax = new Vector2(-2, -2);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            viewport.AddComponent<Image>().color = Color.clear;

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            actionsContent = content.transform;
            var cRT = content.AddComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 1); cRT.anchorMax = new Vector2(1, 1);
            cRT.pivot = new Vector2(0.5f, 1);

            var vContent = content.AddComponent<VerticalLayoutGroup>();
            vContent.spacing = 2;
            vContent.childControlWidth = true;
            vContent.childControlHeight = false;
            vContent.childForceExpandWidth = true;

            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = vpRT;
            scrollRect.content = cRT;

            // Add Action Buttons
            CreateActionButton("Laws", () => TopPanelUI.OpenEconomicLaws());
            CreateActionButton("Leaders", () => WorldTip.showNow("Leaders: Coming Soon", false, "top", 1f));
            CreateActionButton("Policies", () => WorldTip.showNow("Policies: Coming Soon", false, "top", 1f));
            CreateActionButton("Ideologies", () => WorldTip.showNow("Ideologies: Coming Soon", false, "top", 1f));
            CreateActionButton("National Flags", () => WorldTip.showNow("National Flags: Coming Soon", false, "top", 1f));
        }

        private static void CreateIndicators(Transform parent)
        {
            var row = new GameObject("Indicators");
            row.transform.SetParent(parent, false);
            var hLayout = row.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 10;
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            row.AddComponent<LayoutElement>().preferredHeight = 24f;

            // Helper to create an indicator
            GameObject MakeInd(string iconName, Color iconColor, out Text txt)
            {
                var obj = new GameObject("Ind");
                obj.transform.SetParent(row.transform, false);
                var hl = obj.AddComponent<HorizontalLayoutGroup>();
                hl.spacing = 4;
                hl.childControlWidth = false;
                hl.childForceExpandWidth = false;

                var iconObj = new GameObject("Icon");
                iconObj.transform.SetParent(obj.transform, false);
                var img = iconObj.AddComponent<Image>();
                
                // Try load icon or use fallback
                var sprite = Resources.Load<Sprite>("ui/Icons/" + iconName); 
                if (sprite == null) sprite = windowInnerSprite; // Fallback
                
                img.sprite = sprite;
                img.color = iconColor;
                iconObj.AddComponent<LayoutElement>().preferredWidth = 16f;
                iconObj.AddComponent<LayoutElement>().preferredHeight = 16f;

                txt = CreateText(obj.transform, "0", 10, FontStyle.Bold);
                txt.alignment = TextAnchor.MiddleLeft;
                return obj;
            }

            // Corruption (Icon: Skull or similar)
            MakeInd("iconSkull", Color.red, out textCorruption);
            
            // War Exhaustion (Icon: War swords)
            MakeInd("iconWar", Color.red, out textWarExhaustion);

            // Political Power (Icon: Kingdom/Crown, using Renown as proxy)
            MakeInd("iconKingdom", Color.yellow, out textPoliticalPower);

            // Stability (Icon: Peace/Scales)
            MakeInd("iconPeace", Color.cyan, out textStability);
        }

        // --- Logic & Updates ---

        public static void SetVisible(bool v)
        {
            if (root != null) root.SetActive(v);
        }

        public static bool IsVisible() => root != null && root.activeSelf;

        public static void Refresh(Kingdom k)
        {
            if (!IsVisible() || k == null) return;

            // 1. Update Header
            if (k.kingdomColor != null)
            {
                headerFlagBg.color = k.kingdomColor.getColorMain32();
                headerFlagIcon.color = k.kingdomColor.getColorBanner();
            }
            headerFlagBg.sprite = k.getElementBackground();
            headerFlagIcon.sprite = k.getElementIcon();

            headerKingdomName.text = k.data.name;
            headerRulerName.text = k.king != null ? $"Ruler: {k.king.getName()}" : "Ruler: None";
            headerPopulation.text = $"Population: {k.getPopulationTotal()}";

            // 2. Update Relations
            PopulateRelations(k);

            // 3. Update Search List
            PopulateSearchList(searchInput.text);

            // 4. Update Indicators
            var d = KingdomMetricsSystem.Get(k);
            if (d != null)
            {
                textCorruption.text = $"{d.CorruptionLevel * 100:0}%";
                textWarExhaustion.text = $"{d.WarExhaustion:0}";
                textStability.text = $"{d.Stability:0}%";
                textPoliticalPower.text = k.data.renown.ToString(); // PP Proxy
            }
        }

        private static void PopulateRelations(Kingdom k)
        {
            foreach (Transform t in relationsContent) Object.Destroy(t.gameObject);

            // Add Allies
            if (k.hasAlliance())
            {
                foreach (var ally in k.getAlliance().kingdoms_list)
                {
                    if (ally != k && ally.isAlive())
                        CreateRelationIcon(ally, Color.green);
                }
            }

            // Add Enemies
            var wars = World.world.wars.getWars(k);
            foreach (var war in wars)
            {
                if (!war.hasEnded())
                {
                    var enemy = war.getMainDefender() == k ? war.getMainAttacker() : war.getMainDefender();
                    if (enemy != null && enemy != k)
                        CreateRelationIcon(enemy, Color.red);
                }
            }
        }

        private static void CreateRelationIcon(Kingdom k, Color borderColor)
        {
            var btnObj = new GameObject("Rel_" + k.data.name);
            btnObj.transform.SetParent(relationsContent, false);
            
            var le = btnObj.AddComponent<LayoutElement>();
            le.preferredWidth = 30; le.preferredHeight = 30;

            // Flag Background
            var bg = btnObj.AddComponent<Image>();
            bg.sprite = k.getElementBackground();
            bg.color = k.kingdomColor.getColorMain32();

            // Flag Icon
            var icon = new GameObject("Icon").AddComponent<Image>();
            icon.transform.SetParent(btnObj.transform, false);
            var rt = icon.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(2,2); rt.offsetMax = new Vector2(-2,-2);
            icon.sprite = k.getElementIcon();
            icon.color = k.kingdomColor.getColorBanner();

            // Status Border (Green/Red)
            var borderObj = new GameObject("Border");
            borderObj.transform.SetParent(btnObj.transform, false);
            var borderImg = borderObj.AddComponent<Image>();
            borderImg.sprite = windowInnerSprite;
            borderImg.type = Image.Type.Sliced;
            borderImg.color = borderColor;
            var bRT = borderImg.rectTransform;
            bRT.anchorMin = Vector2.zero; bRT.anchorMax = Vector2.one;

            var btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => {
                Main.selectedKingdom = k;
                // Refresh entire UI context for the new kingdom
                HubUI.Refresh();
                TopPanelUI.Refresh();
            });
        }

        private static void PopulateSearchList(string filter)
        {
            foreach (Transform t in kingdomListContent) Object.Destroy(t.gameObject);

            var kingdoms = World.world.kingdoms.list;
            foreach (var k in kingdoms)
            {
                if (!k.isAlive() || k.data.id == Globals.NEUTRAL_KINGDOM_NUMERIC_ID) continue;
                if (!string.IsNullOrEmpty(filter) && !k.data.name.ToLower().Contains(filter.ToLower())) continue;

                CreateKingdomListButton(k);
            }
        }

        private static void CreateKingdomListButton(Kingdom k)
        {
            var btnObj = new GameObject("KBtn_" + k.data.name);
            btnObj.transform.SetParent(kingdomListContent, false);
            
            var le = btnObj.AddComponent<LayoutElement>();
            le.preferredHeight = 24;
            le.flexibleWidth = 1f;

            var img = btnObj.AddComponent<Image>();
            if (windowInnerSprite != null) { img.sprite = windowInnerSprite; img.type = Image.Type.Sliced; }
            img.color = new Color(0.3f, 0.3f, 0.35f, 1f);

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => {
                Main.selectedKingdom = k;
                HubUI.Refresh();
                TopPanelUI.Refresh();
            });

            var hLayout = btnObj.AddComponent<HorizontalLayoutGroup>();
            hLayout.padding = new RectOffset(2, 2, 2, 2);
            hLayout.spacing = 5;
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = false;
            hLayout.childForceExpandWidth = false;

            // Small Flag
            var flagObj = new GameObject("Flag");
            flagObj.transform.SetParent(btnObj.transform, false);
            var fImg = flagObj.AddComponent<Image>();
            fImg.sprite = k.getElementBackground();
            fImg.color = k.kingdomColor.getColorMain32();
            flagObj.AddComponent<LayoutElement>().preferredWidth = 14;
            flagObj.AddComponent<LayoutElement>().preferredHeight = 20;

            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(flagObj.transform, false);
            var iImg = iconObj.AddComponent<Image>();
            iImg.sprite = k.getElementIcon();
            iImg.color = k.kingdomColor.getColorBanner();
            iImg.rectTransform.anchorMin = Vector2.zero; iImg.rectTransform.anchorMax = Vector2.one;

            // Name Text
            var nameTxt = CreateText(btnObj.transform, k.data.name, 10, FontStyle.Normal);
            nameTxt.alignment = TextAnchor.MiddleLeft;
            nameTxt.GetComponent<LayoutElement>().flexibleWidth = 1f;
        }

        private static void OnSearchChanged(string val)
        {
            if (IsVisible()) PopulateSearchList(val);
        }

        private static void CreateActionButton(string label, System.Action onClick)
        {
            var btnObj = new GameObject("Btn_" + label);
            btnObj.transform.SetParent(actionsContent, false);
            
            var le = btnObj.AddComponent<LayoutElement>();
            le.preferredHeight = 24;
            le.flexibleWidth = 1f;

            var img = btnObj.AddComponent<Image>();
            if (windowInnerSprite != null) { img.sprite = windowInnerSprite; img.type = Image.Type.Sliced; }
            img.color = new Color(0.4f, 0.4f, 0.45f, 1f);

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var txt = CreateText(btnObj.transform, label, 10, FontStyle.Normal);
            txt.alignment = TextAnchor.MiddleCenter;
            txt.rectTransform.anchorMin = Vector2.zero; txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.offsetMin = Vector2.zero; txt.rectTransform.offsetMax = Vector2.zero;
        }

        private static Text CreateText(Transform parent, string content, int fontSize, FontStyle style)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var txt = go.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = content;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.color = Color.white;
            txt.raycastTarget = false;
            return txt;
        }
    }
}