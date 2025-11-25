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
            bread = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.Resource.iconResMythril.png");

            // === ROOT CONTAINER (The Red Container) ===
            // This is the main box that holds everything.
            root = new GameObject("DiplomacyRoot", typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var rt = root.GetComponent<RectTransform>();
            // Stretch to fill the parent (TopPanelUI content area)
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // BACKGROUND (Red as requested)
            var bg = root.AddComponent<Image>();
            // Using a sprite makes it look like a UI panel, but colored red for visibility
            if (windowInnerSprite != null) { bg.sprite = windowInnerSprite; bg.type = Image.Type.Sliced; }
            bg.color = new Color(1f, 0f, 0f, 0.1f); // Semi-transparent Red

            // MAIN VERTICAL LAYOUT
            var rootV = root.AddComponent<VerticalLayoutGroup>();
            rootV.childAlignment = TextAnchor.UpperCenter;
            rootV.spacing = 4;
            rootV.padding = new RectOffset(4, 4, 4, 4); // Padding inside the red box
            rootV.childControlWidth = true;
            rootV.childControlHeight = true;
            rootV.childForceExpandWidth = true;
            rootV.childForceExpandHeight = false; // Let the flexible element fill space

            // === 2. HEADER PANEL (Flags + Info) ===
            CreateHeader(root.transform);

            // === 3. RELATIONS PANEL (Allies/Wars Scroll) ===
            CreateRelationsSection(root.transform);
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

            var le = container.AddComponent<LayoutElement>();
            le.preferredHeight = 40f;
            le.minHeight = 40f;
            le.flexibleHeight = 0;

            var scroll = container.AddComponent<ScrollRect>();
            scroll.horizontal = true; scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(container.transform, false);
            Stretch(viewport.GetComponent<RectTransform>(), 2);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            viewport.AddComponent<Image>().color = Color.clear;

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            relationsContent = content.transform;
            
            var h = content.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleLeft;
            h.spacing = 4;
            h.childControlWidth = false; 
            h.childControlHeight = false;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;

            var cRT = content.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 0); cRT.anchorMax = new Vector2(0, 1);
            cRT.pivot = new Vector2(0, 0.5f);
            
            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = cRT;
        }


        // ================================================================================================
        // HELPERS
        // ================================================================================================

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
            var chip = new GameObject("Rel_" + k.data.name, typeof(RectTransform));
            chip.transform.SetParent(relationsContent, false);
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

            // Tiny Flag
            var flagObj = new GameObject("Flag", typeof(RectTransform));
            flagObj.transform.SetParent(btnObj.transform, false);
            var fLe = flagObj.AddComponent<LayoutElement>();
            fLe.preferredWidth = 16f;
            fLe.preferredHeight = 22f;
            
            var fBg = flagObj.AddComponent<Image>();
            fBg.sprite = k.getElementBackground();
            fBg.color = k.kingdomColor.getColorMain32();

            var fIco = new GameObject("Ico", typeof(RectTransform));
            fIco.transform.SetParent(flagObj.transform, false);
            Stretch(fIco.GetComponent<RectTransform>());
            var iImg = fIco.AddComponent<Image>();
            iImg.sprite = k.getElementIcon();
            iImg.color = k.kingdomColor.getColorBanner();

            // Name
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