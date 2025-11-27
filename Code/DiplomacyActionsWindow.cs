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

        // Grids for flags (inside the scroll view)
        private static Transform alliesGrid;
        private static Transform warsGrid;
        
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
            bg.color = new Color(0.1f, 0.1f, 0.15f, 0.5f);

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

            // === 4. BOTTOM CLOSE BUTTON ===
            //CreateBottomBar(root.transform);

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

            // IMPORTANT: flexibleHeight = 1f to fill the space between header and footer
            var le = container.AddComponent<LayoutElement>();
            le.flexibleHeight = 0.2f; 

            // === Left Column (Relations List) ===
            CreateLeftColumn(container.transform);

            // === Right Column (Action Buttons) ===
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

            // Label
            var label = CreateText(col.transform, "Relations", 9, FontStyle.Bold, new Color(0.9f, 0.9f, 0.9f));
            label.alignment = TextAnchor.MiddleCenter;
            var lLe = label.gameObject.AddComponent<LayoutElement>();
            lLe.minHeight = 8f; 
            lLe.preferredHeight = 8f; 
            lLe.flexibleHeight = 0;
            // Scroll Container
            var listObj = new GameObject("RelationsList", typeof(RectTransform));
            listObj.transform.SetParent(col.transform, false);
            var listLe = listObj.AddComponent<LayoutElement>();
            listLe.flexibleHeight = 0.1f; // Takes remaining height
            var lBg = listObj.AddComponent<Image>();
            if (windowInnerSprite != null) { 
                lBg.sprite = windowInnerSprite; 
                lBg.type = Image.Type.Sliced; 
            }
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
            var vList = content.AddComponent<VerticalLayoutGroup>();
            vList.childAlignment = TextAnchor.UpperLeft;
            vList.spacing = 4; 
            vList.padding = new RectOffset(4,4,4,4);
            vList.childControlWidth = true; 
            vList.childControlHeight = true;
            vList.childForceExpandWidth = true;
            vList.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var cRT = content.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 1); 
            cRT.anchorMax = new Vector2(1, 1); 
            cRT.pivot = new Vector2(0.5f, 1);
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = cRT;
            // Create Subsections
            alliesGrid = CreateRelationSubSection(content.transform, "Allies", new Color(0, 0.3f, 0, 0.2f));
            warsGrid = CreateRelationSubSection(content.transform, "Wars", new Color(0.3f, 0, 0, 0.2f));
        }

        private static Transform CreateRelationSubSection(Transform parent, string title, Color bgCol)
        {
            var section = new GameObject(title + "Section", typeof(RectTransform));
            section.transform.SetParent(parent, false);
            
            var v = section.AddComponent<VerticalLayoutGroup>();
            v.spacing = 4;
            v.padding = new RectOffset(4, 4, 4, 4);
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            var bg = section.AddComponent<Image>();
            if (windowInnerSprite != null) { bg.sprite = windowInnerSprite; bg.type = Image.Type.Sliced; }
            bg.color = bgCol;

            // Title
            var txt = CreateText(section.transform, title, 9, FontStyle.Bold, Color.white);
            txt.alignment = TextAnchor.MiddleLeft;
            
            // Grid for flags
            var gridObj = new GameObject("Grid", typeof(RectTransform));
            gridObj.transform.SetParent(section.transform, false);
            
            var grid = gridObj.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(22, 22);
            grid.spacing = new Vector2(4, 4);
            grid.constraint = GridLayoutGroup.Constraint.Flexible;

            // FIX: Grid needs ContentSizeFitter to expand properly in a Vertical Layout
            var fitter = gridObj.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return gridObj.transform;
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

            // Action Buttons (Colors Preserved)
            CreateDiplomacyBtn(col.transform, "Declare War", new Color(0.6f, 0.1f, 0.1f, 1f), () => {
                if(Main.selectedKingdom != null && targetKingdom != null)
                {
                    // FIX: Enable AllowPlayerWar so the patch lets us start a war
                    EventsSystem.AllowPlayerWar = true;
                    try
                    {
                        var warAsset = AssetManager.war_types_library.get("conquest");
                        if (warAsset != null)
                        {
                            World.world.diplomacy.startWar(Main.selectedKingdom, targetKingdom, warAsset, true);
                            WorldTip.showNow("War Declared!", false, "top", 2f, "#FF0000");
                        }
                        else
                        {
                            WorldTip.showNow("Error: War asset not found", false, "top", 2f, "#FF0000");
                        }
                    }
                    finally
                    {
                        EventsSystem.AllowPlayerWar = false; // Reset
                    }
                    
                    Refresh();
                    DiplomacyWindow.Refresh(Main.selectedKingdom);
                }
            });

            CreateDiplomacyBtn(col.transform, "Form Alliance", new Color(0.1f, 0.5f, 0.1f, 1f), () => {
                 if(Main.selectedKingdom != null && targetKingdom != null)
                {
                    if(World.world.diplomacy.getRelation(Main.selectedKingdom, targetKingdom) == null) 
                    {
                         WorldTip.showNow("Alliance Proposal Sent (Simulated)", false, "top", 2f, "#00FF00");
                    }
                    else
                    {
                        WorldTip.showNow("Already have relations!", false, "top", 2f, "#FFFF00");
                    }
                }
            });

            CreateDiplomacyBtn(col.transform, "Non-Aggression Pact", new Color(0.1f, 0.4f, 0.5f, 1f), () => {
                 WorldTip.showNow("Pact Signed (Simulated)", false, "top", 2f, "#00FFFF");
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

        private static void CreateBottomBar(Transform parent)
        {
            var row = new GameObject("BottomBar", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            
            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlHeight = true;
            h.childControlWidth = false;

            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 24f; 
            le.minHeight = 24f; 
            le.flexibleHeight = 0; // Fix: Prevents stretching vertically

            var btnObj = new GameObject("CloseBtn", typeof(RectTransform));
            btnObj.transform.SetParent(row.transform, false);
            
            var ble = btnObj.AddComponent<LayoutElement>();
            ble.preferredWidth = 100f; 
            ble.preferredHeight = 22f;

            var img = btnObj.AddComponent<Image>();
            if (windowInnerSprite != null) { img.sprite = windowInnerSprite; img.type = Image.Type.Sliced; }
            img.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(Close);

            var txt = CreateText(btnObj.transform, "Back", 7, FontStyle.Normal, Color.white);
            txt.alignment = TextAnchor.MiddleCenter;
            Stretch(txt.rectTransform);
        }

        // ================================================================================================
        // REFRESH LOGIC
        // ================================================================================================

        private static void RefreshRelationsList()
        {
            if(alliesGrid == null || warsGrid == null) return;

            // Clear existing children
            foreach (Transform t in alliesGrid) Object.Destroy(t.gameObject);
            foreach (Transform t in warsGrid) Object.Destroy(t.gameObject);

            // Populate Allies
            if (targetKingdom.hasAlliance())
            {
                foreach (var ally in targetKingdom.getAlliance().kingdoms_list)
                {
                    if (ally != targetKingdom && ally.isAlive())
                        CreateSmallFlag(ally, alliesGrid);
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
                            CreateSmallFlag(enemy, warsGrid);
                    }
                }
            }
        }

        private static void CreateSmallFlag(Kingdom k, Transform parent)
        {
            var flagObj = new GameObject("Flag_" + k.data.name, typeof(RectTransform));
            flagObj.transform.SetParent(parent, false);
            
            // Layout size controlled by Grid
            
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