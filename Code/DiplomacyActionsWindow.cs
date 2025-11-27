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

        private static Transform relationsContent;
        private static Transform alliesContent;
        private static Transform warsContent;

        // New: Kingdom we are inspecting
        private static Kingdom targetKingdom;

        public static void Initialize(Transform parent)
        {
            if (root != null) return;

            windowInnerSprite = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.windowInnerSliced.png");

            // === ROOT CONTAINER ===
            root = new GameObject("DiplomacyActionsRoot", typeof(RectTransform));
            root.transform.SetParent(parent, false);

            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Background
            var bg = root.AddComponent<Image>();
            if (windowInnerSprite != null) { bg.sprite = windowInnerSprite; bg.type = Image.Type.Sliced; }
            bg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            // Main Layout
            var rootV = root.AddComponent<VerticalLayoutGroup>();
            rootV.childAlignment = TextAnchor.UpperCenter;
            rootV.spacing = 4;
            rootV.padding = new RectOffset(4, 4, 4, 4);
            rootV.childControlWidth = true;
            rootV.childControlHeight = true;
            rootV.childForceExpandWidth = true;
            rootV.childForceExpandHeight = false;

            // === 1. TITLE ===
            var titleGO = new GameObject("Title", typeof(RectTransform));
            titleGO.transform.SetParent(root.transform, false);
            var titleText = titleGO.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.text = "Diplomatic Actions";
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;
            titleText.resizeTextForBestFit = true;
            titleText.resizeTextMinSize = 10;
            titleText.resizeTextMaxSize = 14;
            
            var titleLE = titleGO.AddComponent<LayoutElement>();
            titleLE.minHeight = 20f;
            titleLE.preferredHeight = 20f;
            titleLE.flexibleHeight = 0;

            // === 2. HEADER PANEL ===
            CreateHeader(root.transform);

            // === 3. SPLIT SECTION ===
            CreateSplitSection(root.transform);

            // === 4. BOTTOM CLOSE BUTTON ===
            CreateBottomBar(root.transform);

            root.SetActive(false);
        }

        public static void Open(Kingdom k)
        {
            if (root == null) return;
            targetKingdom = k;
            
            // Hide main Diplomacy window, show this one
            DiplomacyWindow.SetVisible(false);
            root.SetActive(true);
            root.transform.SetAsLastSibling();
            
            Refresh();
        }

        public static void Close()
        {
            if (root != null) root.SetActive(false);
            DiplomacyWindow.SetVisible(true); // Go back
            DiplomacyWindow.Refresh(Main.selectedKingdom);
        }

        public static bool IsVisible() => root != null && root.activeSelf;

        public static void Refresh()
        {
            if (!IsVisible() || targetKingdom == null) return;

            // 1. Header Logic
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

            // 2. Relations
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
            bg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

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

            headerKingdomName = CreateText(infoStack.transform, "Kingdom", 9, FontStyle.Bold, Color.white);
            headerKingdomName.alignment = TextAnchor.MiddleCenter;
            headerRulerInfo = CreateText(infoStack.transform, "Ruler", 6, FontStyle.Normal, new Color(0.8f, 0.8f, 0.8f));
            headerRulerInfo.alignment = TextAnchor.MiddleCenter;
            headerPopInfo = CreateText(infoStack.transform, "Pop", 6, FontStyle.Normal, new Color(0.8f, 0.8f, 0.8f));
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
            // Note: Named "SplitSection" here. The Find() call must match this.
            var container = new GameObject("SplitSection", typeof(RectTransform));
            container.transform.SetParent(parent, false);

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
            le.flexibleHeight = 1f;

            CreateLeftColumn(container.transform);
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

            var label = CreateText(col.transform, "Relations", 8, FontStyle.Bold, new Color(0.8f, 0.8f, 0.8f));
            label.alignment = TextAnchor.MiddleCenter;
            var lLe = label.gameObject.AddComponent<LayoutElement>();
            lLe.minHeight = 14f; lLe.preferredHeight = 14f; lLe.flexibleHeight = 0;

            var listObj = new GameObject("RelationsList", typeof(RectTransform));
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
            Stretch(viewport.GetComponent<RectTransform>());
            viewport.AddComponent<RectMask2D>();

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            relationsContent = content.transform;
            
            var vList = content.AddComponent<VerticalLayoutGroup>();
            vList.childAlignment = TextAnchor.UpperLeft;
            vList.spacing = 2; 
            vList.padding = new RectOffset(2,2,2,2);
            vList.childControlWidth = true; 
            vList.childControlHeight = true;      
            vList.childForceExpandWidth = true;
            vList.childForceExpandHeight = false;

            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var cRT = content.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 1); cRT.anchorMax = new Vector2(1, 1); cRT.pivot = new Vector2(0.5f, 1);

            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = cRT;

            alliesContent = CreateRelationSubSection(relationsContent, "Allies", new Color(0, 0.3f, 0, 0.2f));
            warsContent = CreateRelationSubSection(relationsContent, "Wars", new Color(0.3f, 0, 0, 0.2f));
        }

        private static Transform CreateRelationSubSection(Transform parent, string title, Color bgCol)
        {
            var section = new GameObject(title + "Section", typeof(RectTransform));
            section.transform.SetParent(parent, false);
            
            var v = section.AddComponent<VerticalLayoutGroup>();
            v.spacing = 2;
            v.padding = new RectOffset(2, 2, 2, 2);
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            var bg = section.AddComponent<Image>();
            if (windowInnerSprite != null) { bg.sprite = windowInnerSprite; bg.type = Image.Type.Sliced; }
            bg.color = bgCol;

            var txt = CreateText(section.transform, title, 8, FontStyle.Bold, Color.white);
            txt.alignment = TextAnchor.MiddleLeft;
            
            var gridObj = new GameObject("Grid", typeof(RectTransform));
            gridObj.transform.SetParent(section.transform, false);
            var grid = gridObj.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(18, 18);
            grid.spacing = new Vector2(2, 2);
            grid.constraint = GridLayoutGroup.Constraint.Flexible;

            return gridObj.transform;
        }

        private static void CreateRightColumn(Transform parent)
        {
            var col = new GameObject("RightCol", typeof(RectTransform));
            col.transform.SetParent(parent, false);
            var v = col.AddComponent<VerticalLayoutGroup>();
            v.spacing = 4; 
            v.padding = new RectOffset(0,0,10,0);
            v.childControlWidth = true; 
            v.childControlHeight = true; 
            v.childForceExpandWidth = true; 
            v.childForceExpandHeight = false;
            
            var le = col.AddComponent<LayoutElement>();
            le.preferredWidth = 110f; 
            le.flexibleWidth = 0f; 
            le.flexibleHeight = 1f;

            CreateDiplomacyBtn("Declare War", Color.red, () => {
                if(Main.selectedKingdom != null && targetKingdom != null)
                {
                    World.world.diplomacy.startWar(Main.selectedKingdom, targetKingdom, AssetManager.war_types_library.get("conquest"), true);
                    WorldTip.showNow("War Declared!", false, "top", 2f, "#FF0000");
                    Refresh();
                    DiplomacyWindow.Refresh(Main.selectedKingdom);
                }
            });

            CreateDiplomacyBtn("Form Alliance", Color.green, () => {
                 if(Main.selectedKingdom != null && targetKingdom != null)
                {
                    if(World.world.diplomacy.getRelation(Main.selectedKingdom, targetKingdom) == null)
                    {
                         WorldTip.showNow("Alliance Proposal Sent (Simulated)", false, "top", 2f, "#00FF00");
                    }
                }
            });

            CreateDiplomacyBtn("Non-Aggression Pact", Color.cyan, () => {
                 WorldTip.showNow("Pact Signed (Simulated)", false, "top", 2f, "#00FFFF");
            });
        }

        private static void CreateDiplomacyBtn(string label, Color color, System.Action onClick)
        {
            var btnObj = new GameObject("Btn_" + label, typeof(RectTransform));
            
            btnObj.transform.SetParent(root.transform.Find("SplitSection/RightCol"), false);
            
            if(btnObj.transform.parent == null) return; 

            var le = btnObj.AddComponent<LayoutElement>();
            le.preferredHeight = 30f; 
            le.minHeight = 30f;
            le.flexibleWidth = 1f;

            var img = btnObj.AddComponent<Image>();
            if (windowInnerSprite != null) { img.sprite = windowInnerSprite; img.type = Image.Type.Sliced; }
            img.color = new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f, 1f);

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var txt = CreateText(btnObj.transform, label, 9, FontStyle.Bold, Color.white);
            txt.alignment = TextAnchor.MiddleCenter;
            Stretch(txt.rectTransform);
        }

        private static void CreateBottomBar(Transform parent)
        {
            var row = new GameObject("BottomBar", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleCenter;
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 24f; le.minHeight = 24f; le.flexibleHeight = 0;

            var btnObj = new GameObject("CloseBtn", typeof(RectTransform));
            btnObj.transform.SetParent(row.transform, false);
            var ble = btnObj.AddComponent<LayoutElement>();
            ble.preferredWidth = 80f; ble.preferredHeight = 20f;

            var img = btnObj.AddComponent<Image>();
            if (windowInnerSprite != null) { img.sprite = windowInnerSprite; img.type = Image.Type.Sliced; }
            img.color = new Color(0.3f, 0.3f, 0.3f, 1f);

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(Close);

            var txt = CreateText(btnObj.transform, "Back", 10, FontStyle.Normal, Color.white);
            txt.alignment = TextAnchor.MiddleCenter;
            Stretch(txt.rectTransform);
        }

        private static void RefreshRelationsList()
        {
            foreach (Transform t in alliesContent) Object.Destroy(t.gameObject);
            foreach (Transform t in warsContent) Object.Destroy(t.gameObject);

            if (targetKingdom.hasAlliance())
            {
                foreach (var ally in targetKingdom.getAlliance().kingdoms_list)
                {
                    if (ally != targetKingdom && ally.isAlive())
                        CreateSmallFlag(ally, alliesContent);
                }
            }

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
            
            var bg = flagObj.AddComponent<Image>();
            bg.sprite = k.getElementBackground();
            if (k.kingdomColor != null) bg.color = k.kingdomColor.getColorMain32();

            var iconObj = new GameObject("Icon", typeof(RectTransform));
            iconObj.transform.SetParent(flagObj.transform, false);
            Stretch(iconObj.GetComponent<RectTransform>(), 1);

            var ico = iconObj.AddComponent<Image>();
            ico.sprite = k.getElementIcon();
            if (k.kingdomColor != null) ico.color = k.kingdomColor.getColorBanner();

            var btn = flagObj.AddComponent<Button>();
            btn.onClick.AddListener(() => {
                 Open(k);
            });
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