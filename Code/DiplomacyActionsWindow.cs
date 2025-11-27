using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace RulerBox
{
    public static class DiplomacyActionsWindow
    {
        private static GameObject root;
        private static Sprite windowInnerSprite;
        
        // --- UI References ---
        private static Image headerLeftFlagIcon;
        private static Image headerLeftFlagBg;
        private static Image headerRightFlagIcon;
        private static Image headerRightFlagBg;
        
        private static Text headerKingdomName;
        private static Text headerRulerInfo;
        private static Text headerPopInfo;

        // Content Transforms for the horizontal rows
        private static Transform alliesContent; 
        private static Transform warsContent;
        
        private static Kingdom targetKingdom;

        public static void Initialize(Transform parent)
        {
            if (root != null) return;

            windowInnerSprite = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.windowInnerSliced.png");

            // === ROOT CONTAINER ===
            root = new GameObject("DiplomacyActionsRoot", typeof(RectTransform));
            root.transform.SetParent(parent, false);

            // Stretch to fill parent completely
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Background
            var bg = root.AddComponent<Image>();
            if (windowInnerSprite != null) { bg.sprite = windowInnerSprite; bg.type = Image.Type.Sliced; }
            bg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f); // Increased opacity slightly for readability

            // Main Vertical Layout
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

            // === 3. SPLIT SECTION (Left: Relations | Right: Buttons) ===
            CreateSplitSection(root.transform);

            root.SetActive(false);
        }

        public static void Open(Kingdom k)
        {
            if (root == null) return;
            targetKingdom = k;
            
            DiplomacyWindow.SetVisible(false);
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            
            Refresh();
        }

        public static void Close()
        {
            if (root != null) root.SetActive(false);
            DiplomacyWindow.SetVisible(true);
            DiplomacyWindow.Refresh(Main.selectedKingdom);
        }

        public static bool IsVisible() => root != null && root.activeSelf;

        public static void Refresh()
        {
            if (!IsVisible() || targetKingdom == null) return;

            // --- 1. Header Update ---
            Color mainColor = Color.white;
            Color bannerColor = Color.white;

            if (targetKingdom.kingdomColor != null)
            {
                mainColor = targetKingdom.kingdomColor.getColorMain32();
                bannerColor = targetKingdom.kingdomColor.getColorBanner();
            }

            if (headerLeftFlagBg) { headerLeftFlagBg.color = mainColor; headerLeftFlagBg.sprite = targetKingdom.getElementBackground(); }
            if (headerLeftFlagIcon) { headerLeftFlagIcon.color = bannerColor; headerLeftFlagIcon.sprite = targetKingdom.getElementIcon(); }
            if (headerRightFlagBg) { headerRightFlagBg.color = mainColor; headerRightFlagBg.sprite = targetKingdom.getElementBackground(); }
            if (headerRightFlagIcon) { headerRightFlagIcon.color = bannerColor; headerRightFlagIcon.sprite = targetKingdom.getElementIcon(); }

            headerKingdomName.text = targetKingdom.data.name;
            string ruler = targetKingdom.king != null ? targetKingdom.king.getName() : "None";
            headerRulerInfo.text = $"Ruler: {ruler}";
            headerPopInfo.text = $"Population: {targetKingdom.getPopulationTotal()}";

            // --- 2. Relations List Update ---
            RefreshRelationsList();
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
            bg.color = new Color(0.15f, 0.15f, 0.15f, 0.5f);

            var h = container.AddComponent<HorizontalLayoutGroup>();
            h.padding = new RectOffset(8, 8, 4, 4);
            h.spacing = 10;
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;

            var le = container.AddComponent<LayoutElement>();
            le.preferredHeight = 50f;
            le.minHeight = 50f;
            le.flexibleHeight = 0;

            CreateFlag(container.transform, "FlagLeft", out headerLeftFlagBg, out headerLeftFlagIcon);

            var infoStack = new GameObject("InfoStack", typeof(RectTransform));
            infoStack.transform.SetParent(container.transform, false);
            var v = infoStack.AddComponent<VerticalLayoutGroup>();
            v.spacing = 0;
            v.childAlignment = TextAnchor.MiddleCenter;
            
            var stackLE = infoStack.AddComponent<LayoutElement>();
            stackLE.flexibleWidth = 1f;

            headerKingdomName = CreateText(infoStack.transform, "Kingdom", 10, FontStyle.Bold, Color.white);
            headerKingdomName.alignment = TextAnchor.MiddleCenter;
            headerRulerInfo = CreateText(infoStack.transform, "Ruler", 8, FontStyle.Normal, new Color(0.8f, 0.8f, 0.8f));
            headerRulerInfo.alignment = TextAnchor.MiddleCenter;
            headerPopInfo = CreateText(infoStack.transform, "Pop", 8, FontStyle.Normal, new Color(0.8f, 0.8f, 0.8f));
            headerPopInfo.alignment = TextAnchor.MiddleCenter;

            CreateFlag(container.transform, "FlagRight", out headerRightFlagBg, out headerRightFlagIcon);
        }

        private static void CreateFlag(Transform parent, string name, out Image bgImg, out Image iconImg)
        {
            var flagWrapper = new GameObject(name, typeof(RectTransform));
            flagWrapper.transform.SetParent(parent, false);
            var flagLE = flagWrapper.AddComponent<LayoutElement>();
            flagLE.preferredWidth = 40f; flagLE.preferredHeight = 40f;
            flagLE.minWidth = 40f; flagLE.minHeight = 40f;
            
            var flagBgObj = new GameObject("FlagBG", typeof(RectTransform));
            flagBgObj.transform.SetParent(flagWrapper.transform, false);
            bgImg = flagBgObj.AddComponent<Image>();
            Stretch(bgImg.rectTransform);

            var flagIconObj = new GameObject("FlagIcon", typeof(RectTransform));
            flagIconObj.transform.SetParent(flagWrapper.transform, false);
            iconImg = flagIconObj.AddComponent<Image>();
            Stretch(iconImg.rectTransform, 2);
        }

        private static void CreateSplitSection(Transform parent)
        {
            var container = new GameObject("SplitSection", typeof(RectTransform));
            container.transform.SetParent(parent, false);

            var bg = container.AddComponent<Image>();
            if (windowInnerSprite != null) { bg.sprite = windowInnerSprite; bg.type = Image.Type.Sliced; }
            bg.color = new Color(0.15f, 0.15f, 0.15f, 0.3f);

            var h = container.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 2;
            h.padding = new RectOffset(2, 2, 2, 2);
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = true;
            h.childForceExpandHeight = true;

            var le = container.AddComponent<LayoutElement>();
            le.preferredHeight = 75f; 
            le.minHeight = 75f; 
            le.flexibleHeight = 0f; 
            le.preferredWidth = 20f; 
            le.minWidth = 20f; 

            // === Left Column (Relations List) ===
            CreateLeftColumn(container.transform);

            // === Right Column (Action Buttons) ===
            CreateRightColumn(container.transform);
        }

        // ====================================================================================
        // UPDATED CREATE LEFT COLUMN
        // ====================================================================================
        private static void CreateLeftColumn(Transform parent)
        {
            var col = new GameObject("LeftCol", typeof(RectTransform));
            col.transform.SetParent(parent, false);
            
            var v = col.AddComponent<VerticalLayoutGroup>();
            v.spacing = 4;
            v.padding = new RectOffset(4, 4, 4, 4);
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            var le = col.AddComponent<LayoutElement>();
            le.preferredWidth = 20f; 
            le.minWidth = 20f;
            le.flexibleWidth = 1f;
            le.flexibleHeight = 1f;

            // --- 1. ALLIES HEADER ---
            var t1 = CreateText(col.transform, "Allies", 8, FontStyle.Bold, new Color(0.8f, 1f, 0.8f));
            t1.alignment = TextAnchor.MiddleLeft;

            // --- 2. ALLIES ROW (Horizontal Scroll) ---
            alliesContent = CreateSingleRelationRow(col.transform, "AlliesRow", new Color(0, 0.3f, 0, 0.2f), 32f);

            // --- 3. WARS HEADER ---
            var t2 = CreateText(col.transform, "Active Wars", 8, FontStyle.Bold, new Color(1f, 0.8f, 0.8f));
            t2.alignment = TextAnchor.MiddleLeft;

            // --- 4. WARS ROW (Horizontal Scroll) ---
            warsContent = CreateSingleRelationRow(col.transform, "WarsRow", new Color(0.3f, 0, 0, 0.2f), 32f);

            // --- 5. SPACER (Pushes content to top) ---
            var spacer = new GameObject("Spacer", typeof(RectTransform));
            spacer.transform.SetParent(col.transform, false);
            var spacerLE = spacer.AddComponent<LayoutElement>();
            spacerLE.flexibleHeight = 1f; 
        }

        private static Transform CreateSingleRelationRow(Transform parent, string name, Color tint, float height)
        {
            var rowObj = new GameObject(name, typeof(RectTransform));
            rowObj.transform.SetParent(parent, false);

            var le = rowObj.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
            le.flexibleHeight = 0;
            le.flexibleWidth = 1;

            var bg = rowObj.AddComponent<Image>();
            if (windowInnerSprite != null) { bg.sprite = windowInnerSprite; bg.type = Image.Type.Sliced; }
            bg.color = tint;

            var scroll = rowObj.AddComponent<ScrollRect>();
            scroll.horizontal = true; 
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 25f;

            var viewport = new GameObject("Viewport", typeof(RectTransform));
            viewport.transform.SetParent(rowObj.transform, false);
            
            var vRT = viewport.GetComponent<RectTransform>();
            vRT.anchorMin = Vector2.zero;
            vRT.anchorMax = Vector2.one;
            vRT.sizeDelta = Vector2.zero;
            vRT.pivot = new Vector2(0, 0.5f);

            viewport.AddComponent<RectMask2D>();
            viewport.AddComponent<Image>().color = Color.clear;
            
            var contentObj = new GameObject("Content", typeof(RectTransform));
            contentObj.transform.SetParent(viewport.transform, false);

            var h = contentObj.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleLeft;
            h.spacing = 4;
            h.padding = new RectOffset(4, 4, 0, 0);
            h.childControlWidth = false; 
            h.childControlHeight = false;
            h.childForceExpandWidth = false; 
            h.childForceExpandHeight = false;

            var fitter = contentObj.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained; 

            var cRT = contentObj.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 0); 
            cRT.anchorMax = new Vector2(0, 1);
            cRT.pivot = new Vector2(0, 0.5f);
            cRT.sizeDelta = Vector2.zero;

            scroll.viewport = vRT;
            scroll.content = cRT;

            return contentObj.transform;
        }

        private static void CreateRightColumn(Transform parent)
        {
            var col = new GameObject("RightCol", typeof(RectTransform));
            col.transform.SetParent(parent, false);

            var v = col.AddComponent<VerticalLayoutGroup>();
            v.spacing = 2; 
            v.padding = new RectOffset(0,0,5,0);
            v.childControlWidth = true; 
            v.childControlHeight = true; 
            v.childForceExpandWidth = true; 
            v.childForceExpandHeight = false;
            
            var le = col.AddComponent<LayoutElement>();
            le.preferredWidth = 60f; 
            le.flexibleWidth = 0f; 
            le.flexibleHeight = 0.1f;

            CreateDiplomacyBtn(col.transform, "Declare War", new Color(0.6f, 0.1f, 0.1f, 1f), () => {
                if(Main.selectedKingdom != null && targetKingdom != null)
                {
                    if (Main.selectedKingdom.isEnemy(targetKingdom)) {
                        WorldTip.showNow("We are already at war!", false, "top", 2f, "#FF0000");
                        return;
                    }

                    EventsSystem.AllowPlayerWar = true;
                    try {
                        var warAsset = AssetManager.war_types_library.get("war_conquest");
                        if (warAsset != null) {
                            World.world.diplomacy.startWar(Main.selectedKingdom, targetKingdom, warAsset, true);
                            EventsUI.ShowPopup($"War declared on {targetKingdom.data.name}!", EventButtonType.War, targetKingdom, null, null, null);
                            Close();
                        }
                    } finally { EventsSystem.AllowPlayerWar = false; }
                }
                Refresh();
            });

            CreateDiplomacyBtn(col.transform, "Make Peace", new Color(0.2f, 0.4f, 0.8f, 1f), () => {
                if(Main.selectedKingdom != null && targetKingdom != null) {
                    if (!Main.selectedKingdom.isEnemy(targetKingdom)) {
                        WorldTip.showNow("We are not at war.", false, "top", 2f, "#FFFF00");
                        return;
                    }
                    var wars = World.world.wars.getWars(Main.selectedKingdom);
                    War activeWar = null;
                    foreach(var w in wars) {
                        if (!w.hasEnded() && (w.isAttacker(targetKingdom) || w.isDefender(targetKingdom))) {
                            activeWar = w; break;
                        }
                    }
                    if (activeWar != null) {
                        World.world.wars.endWar(activeWar, WarWinner.Peace);
                        EventsUI.ShowPopup($"Peace treaty signed with {targetKingdom.data.name}.", EventButtonType.Peace, targetKingdom, null, null, null);
                        Close();
                    }
                }
                Refresh();
            });

            CreateDiplomacyBtn(col.transform, "Form Alliance", new Color(0.1f, 0.5f, 0.1f, 1f), () => {
                 if(Main.selectedKingdom != null && targetKingdom != null) {
                    if (Main.selectedKingdom.isEnemy(targetKingdom)) {
                        WorldTip.showNow("We are at war! Make peace first.", false, "top", 2f, "#FF5A5A");
                        return;
                    }
                    var relation = World.world.diplomacy.getRelation(Main.selectedKingdom, targetKingdom);
                    var opinion = relation?.getOpinion(Main.selectedKingdom, targetKingdom);
                    int score = opinion != null ? opinion.total : 0;

                    if (score < 30) {
                        WorldTip.showNow($"They refuse! (Opinion: {score}/30)", false, "top", 2f, "#FF5A5A");
                        return;
                    }

                    bool success = false;
                    if (!Main.selectedKingdom.hasAlliance() && !targetKingdom.hasAlliance()) {
                        World.world.alliances.newAlliance(Main.selectedKingdom, targetKingdom);
                        success = true;
                    }
                    else if (Main.selectedKingdom.hasAlliance() && !targetKingdom.hasAlliance()) {
                        Main.selectedKingdom.getAlliance().join(targetKingdom);
                        success = true;
                    }
                    else if (!Main.selectedKingdom.hasAlliance() && targetKingdom.hasAlliance()) {
                        targetKingdom.getAlliance().join(Main.selectedKingdom);
                        success = true;
                    }
                    else {
                        WorldTip.showNow("Both kingdoms are already in different alliances.", false, "top", 2f, "#FFFF00");
                        return;
                    }

                    if (success) {
                        EventsUI.ShowPopup($"Alliance formed with {targetKingdom.data.name}!", EventButtonType.Diplomacy, targetKingdom, null, null, null);
                        Close();
                    }
                }
                Refresh();
            });

            CreateDiplomacyBtn(col.transform, "Non-Aggression", new Color(0.1f, 0.4f, 0.5f, 1f), () => {
                if(Main.selectedKingdom != null && targetKingdom != null) {
                    var relation = World.world.diplomacy.getRelation(Main.selectedKingdom, targetKingdom);
                    var opinion = relation?.getOpinion(Main.selectedKingdom, targetKingdom);
                    int score = opinion != null ? opinion.total : 0;
                    if (score < 0) {
                        WorldTip.showNow($"They distrust us. (Opinion: {score})", false, "top", 2f, "#FF5A5A");
                        return;
                    }
                    World.world.diplomacy.eventFriendship(Main.selectedKingdom);
                    EventsUI.ShowPopup($"Non-Aggression Pact signed with {targetKingdom.data.name}.", EventButtonType.Peace, targetKingdom, null, null, null);
                    Close();
                }
                Refresh();
            });
        }

        private static void CreateDiplomacyBtn(Transform parent, string label, Color color, System.Action onClick)
        {
            var btnObj = new GameObject("Btn_" + label, typeof(RectTransform));
            btnObj.transform.SetParent(parent, false);

            var le = btnObj.AddComponent<LayoutElement>();
            le.preferredHeight = 25f; 
            le.minHeight = 25f;
            le.flexibleWidth = 1f;

            var img = btnObj.AddComponent<Image>();
            if (windowInnerSprite != null) { img.sprite = windowInnerSprite; img.type = Image.Type.Sliced; }
            img.color = color;

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var txt = CreateText(btnObj.transform, label, 5, FontStyle.Bold, Color.white);
            txt.alignment = TextAnchor.MiddleCenter;
            Stretch(txt.rectTransform);
        }

        // ================================================================================================
        // REFRESH LOGIC
        // ================================================================================================

        private static void RefreshRelationsList()
        {
            if(alliesContent == null || warsContent == null) return;

            // Clear existing children
            foreach (Transform t in alliesContent) Object.Destroy(t.gameObject);
            foreach (Transform t in warsContent) Object.Destroy(t.gameObject);

            // Populate Allies
            if (targetKingdom.hasAlliance())
            {
                foreach (var ally in targetKingdom.getAlliance().kingdoms_list)
                {
                    if (ally != targetKingdom && ally.isAlive())
                        CreateSmallFlag(ally, alliesContent);
                }
            }

            // Populate Wars
            var wars = World.world.wars.getWars(targetKingdom);
            foreach (var war in wars)
            {
                if (!war.hasEnded())
                {
                    bool isAttacker = war.isAttacker(targetKingdom);
                    IEnumerable<Kingdom> enemies = isAttacker ? war.getDefenders() : war.getAttackers();
                    foreach (var enemy in enemies)
                    {
                        if (enemy != targetKingdom && enemy.isAlive())
                            CreateSmallFlag(enemy, warsContent);
                    }
                }
            }
        }

        private static void CreateSmallFlag(Kingdom k, Transform parent)
        {
            var flagObj = new GameObject("Flag_" + k.data.name, typeof(RectTransform));
            flagObj.transform.SetParent(parent, false);
            
            // CRITICAL ADDITION: Layout Element to define size in Horizontal Group
            var le = flagObj.AddComponent<LayoutElement>();
            le.minWidth = 30f;
            le.minHeight = 30f;
            le.preferredWidth = 30f;
            le.preferredHeight = 30f;

            var bg = flagObj.AddComponent<Image>();
            bg.sprite = k.getElementBackground();
            if (k.kingdomColor != null) bg.color = k.kingdomColor.getColorMain32();

            var iconObj = new GameObject("Icon", typeof(RectTransform));
            iconObj.transform.SetParent(flagObj.transform, false);
            Stretch(iconObj.GetComponent<RectTransform>(), 2); 

            var ico = iconObj.AddComponent<Image>();
            ico.sprite = k.getElementIcon();
            if (k.kingdomColor != null) ico.color = k.kingdomColor.getColorBanner();

            var btn = flagObj.AddComponent<Button>();
            btn.onClick.AddListener(() => {
                 Open(k);
            });
        }

        // ================================================================================================
        // HELPERS
        // ================================================================================================

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