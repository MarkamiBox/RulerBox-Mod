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

            // Row Background
            var bg = rowObj.AddComponent<Image>();
            if (windowInnerSprite != null) { bg.sprite = windowInnerSprite; bg.type = Image.Type.Sliced; }
            bg.color = tint;

            // Scroll
            var scroll = rowObj.AddComponent<ScrollRect>();
            scroll.horizontal = true; 
            scroll.vertical = false;
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
            // === FIX HERE: Enable Control so chips can be resized ===
            h.childControlWidth = true; 
            h.childControlHeight = true;
            // =======================================================
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

        private static void RefreshRelations(Kingdom k)
        {
            // Clear existing chips
            if (alliesContent != null) foreach (Transform child in alliesContent) Object.Destroy(child.gameObject);
            if (warsContent != null)   foreach (Transform child in warsContent)   Object.Destroy(child.gameObject);

            // 1. Allies
            if (k.hasAlliance())
            {
                foreach (var ally in k.getAlliance().kingdoms_list)
                {
                    if (ally != k && ally.isAlive())
                    {
                        CreateRelationChip(ally, Color.green, alliesContent);
                    }
                }
            }

            // 2. Enemies (Wars)
            var wars = World.world.wars.getWars(k);
            foreach (var war in wars)
            {
                if (war.hasEnded()) continue;

                // Determine which side 'k' is on. 
                bool weAreAttackers = war.isAttacker(k);
                
                // FIX: Use IEnumerable instead of List
                IEnumerable<Kingdom> enemies = weAreAttackers ? war.getDefenders() : war.getAttackers();

                // Loop through ALL enemies in this war
                foreach (var enemy in enemies)
                {
                    if (enemy != k && enemy.isAlive())
                    {
                        CreateRelationChip(enemy, Color.red, warsContent);
                    }
                }
            }
        }

        private static void CreateRelationChip(Kingdom k, Color borderColor, Transform parent)
        {
            var chip = new GameObject("Rel_" + k.data.name, typeof(RectTransform));
            chip.transform.SetParent(parent, false);
            
            // Define size here (18x18 is small)
            var le = chip.AddComponent<LayoutElement>();
            le.minWidth = 18f;
            le.minHeight = 18f;
            le.preferredWidth = 18f;
            le.preferredHeight = 18f;

            var bg = chip.AddComponent<Image>();
            if (windowInnerSprite != null) { bg.sprite = windowInnerSprite; bg.type = Image.Type.Sliced; }
            bg.color = borderColor;

            // Icon Container
            var iconObj = new GameObject("Icon", typeof(RectTransform));
            iconObj.transform.SetParent(chip.transform, false);
            Stretch(iconObj.GetComponent<RectTransform>(), 1); // 1px padding

            var fBg = iconObj.AddComponent<Image>();
            fBg.sprite = k.getElementBackground();
            if (k.kingdomColor != null)
                fBg.color = k.kingdomColor.getColorMain32();
            else 
                fBg.color = Color.white;

            // Inner Symbol
            var ico = new GameObject("Ico", typeof(RectTransform));
            ico.transform.SetParent(iconObj.transform, false);
            Stretch(ico.GetComponent<RectTransform>());
            
            var img = ico.AddComponent<Image>();
            img.sprite = k.getElementIcon();
            if (k.kingdomColor != null)
                img.color = k.kingdomColor.getColorBanner();
            else
                img.color = Color.white;

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
            fLe.preferredWidth = 5f; fLe.preferredHeight = 7f;
            
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