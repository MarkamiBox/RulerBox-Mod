using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

namespace RulerBox
{
    public static class ResourcesTradeWindow
    {
        private static GameObject root;
        private static Sprite windowInnerSprite;
        private static Sprite bgSprite;
        private static Sprite tradeButtonSprite;

        public static void Initialize(Transform parent)
        {
            TradeWindow.Initialize(parent);

            if (root != null) return;

            windowInnerSprite = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.windowInnerSliced.png");
            bgSprite          = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.InvHub.png");
            tradeButtonSprite = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.ButtonTrade.png");
            
            // === ROOT ===
            root = new GameObject("ResourcesTradeWindowRoot");
            root.transform.SetParent(parent, false);
            
            // Full Stretch
            var rt = root.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Vertical Layout
            var v = root.AddComponent<VerticalLayoutGroup>();
            v.childAlignment = TextAnchor.UpperCenter;
            v.spacing = 6;
            v.padding = new RectOffset(4, 4, 4, 4);
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;

            // === TITLE ===
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(root.transform, false);
            var title = titleGO.AddComponent<Text>();
            title.font = Resources.Load<Font>("Fonts/Roboto-Regular") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            title.text = "Resources";
            title.alignment = TextAnchor.MiddleCenter;
            title.color = Color.white;
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 10;
            title.resizeTextMaxSize = 14;
            titleGO.AddComponent<LayoutElement>().preferredHeight = 24f;

            // === SCROLL VIEW ===
            var scrollGO = new GameObject("ResourcesScroll");
            scrollGO.transform.SetParent(root.transform, false);
            var scrollLE = scrollGO.AddComponent<LayoutElement>();
            scrollLE.preferredHeight = 200f;
            scrollLE.flexibleHeight = 1f;

            // Background
            var bgImg = scrollGO.AddComponent<Image>();
            if (windowInnerSprite != null)
            {
                bgImg.sprite = windowInnerSprite;
                bgImg.type = Image.Type.Sliced;
            }
            else
            {
                bgImg.color = new Color(0f, 0f, 0f, 0.35f);
            }

            // ScrollRect
            var scrollRect = scrollGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // Viewport
            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(scrollGO.transform, false);
            var viewportRT = viewportGO.AddComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = new Vector2(2, 2);
            viewportRT.offsetMax = new Vector2(-2, -2);
            viewportGO.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            viewportGO.AddComponent<Mask>().showMaskGraphic = false;

            // Content
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRT = contentGO.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1f);

            // Vertical Layout for Content
            var contentVL = contentGO.AddComponent<VerticalLayoutGroup>();
            contentVL.childAlignment = TextAnchor.UpperCenter;
            contentVL.spacing = 4;
            contentVL.padding = new RectOffset(4, 4, 2, 2);
            contentVL.childControlWidth = true;
            contentVL.childControlHeight = true;
            contentVL.childForceExpandWidth = false; 
            contentVL.childForceExpandHeight = false;

            contentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRT;
            scrollRect.content = contentRT;

            // === BOTTOM ROW (BACK BUTTON) ===
            var bottomRow = new GameObject("BottomRow");
            bottomRow.transform.SetParent(root.transform, false);
            var bottomHL = bottomRow.AddComponent<HorizontalLayoutGroup>();
            bottomHL.childAlignment = TextAnchor.MiddleCenter;
            bottomHL.spacing = 0;
            bottomHL.childControlWidth = false;
            bottomHL.childControlHeight = true;
            bottomHL.childForceExpandWidth = false;
            bottomHL.childForceExpandHeight = false;

            var bottomLE = bottomRow.AddComponent<LayoutElement>();
            bottomLE.preferredHeight = 26f;

            BuildButton(bottomRow.transform, "Back", () => 
            {
                SetVisible(false);
                TopPanelUI.ReturnToEconomyMain();
            });

            root.SetActive(false);
        }
        
        // Helpers
        public static void SetVisible(bool visible)
        {
            if (root != null) root.SetActive(visible);
        }

        public static bool IsVisible() => root != null && root.activeSelf;

        private static float refreshTimer = 0f;
        public static void Update()
        {
            TradeWindow.UpdateLogic();
            if (!IsVisible()) return;
            refreshTimer += Time.deltaTime;
            if (refreshTimer > 0.5f)
            {
                refreshTimer = 0f;
                Refresh(Main.selectedKingdom);
            }
        }

        // Refreshes the resource list for the given kingdom
        public static void Refresh(Kingdom k)
        {
            if (root == null || k == null) return;

            Transform content = root.transform.Find("ResourcesScroll/Viewport/Content");
            if (content == null) return;

            foreach (Transform child in content)
            {
                GameObject.Destroy(child.gameObject);
            }

            var data = KingdomMetricsSystem.Get(k);
            
            foreach (string resId in KingdomMetricsSystem.TrackedResources)
            {
                BuildResourceRow(content, resId, data);
            }
        }

        // Builds a single resource row
        private static void BuildResourceRow(Transform parent, string resId, KingdomMetricsSystem.Data data)
        {
            var rowGO = new GameObject(resId + "_Row");
            rowGO.transform.SetParent(parent, false);

            var rowImg = rowGO.AddComponent<Image>();
            rowImg.sprite = bgSprite != null ? bgSprite : windowInnerSprite;
            rowImg.type = Image.Type.Sliced;

            var rowHL = rowGO.AddComponent<HorizontalLayoutGroup>();
            rowHL.childAlignment = TextAnchor.MiddleLeft;
            rowHL.spacing = 10;
            rowHL.padding = new RectOffset(8, 8, 4, 4);
            rowHL.childControlWidth = true;
            rowHL.childControlHeight = true;
            rowHL.childForceExpandWidth = false;
            rowHL.childForceExpandHeight = false;

            var rowLE = rowGO.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 40f;
            rowLE.preferredWidth = 155f; // Matches InvestmentsWindow width
            rowLE.flexibleWidth = 0f;    // Prevents stretching

            // 1. Icon (Left)
            var iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(rowGO.transform, false);
            var iconImg = iconGO.AddComponent<Image>();            
            ResourceAsset asset = AssetManager.resources.get(resId);
            
            if (asset == null)
            {
                if (resId == "mithril") asset = AssetManager.resources.get("mythril");
                if (resId == "CommonMetals") asset = AssetManager.resources.get("common_metals");
            }

            Sprite sprite = null;

            if (asset != null)
            {
                // A. Try Standard Path
                if (!string.IsNullOrEmpty(asset.path_icon))
                {
                    sprite = Resources.Load<Sprite>("ui/Icons/" + asset.path_icon);
                }
                // B. Try Construction from Gameplay Sprite (e.g. iconRes + id)
                if (sprite == null && !string.IsNullOrEmpty(asset.path_gameplay_sprite))
                {
                    sprite = Resources.Load<Sprite>("ui/Icons/iconRes" + asset.path_gameplay_sprite);
                }
                // C. Try Mod Embedded (if you have custom pngs)
                if (sprite == null && !string.IsNullOrEmpty(asset.path_icon))
                {
                    sprite = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.Resource." + asset.path_icon + ".png");
                }
            }

            // D. Fallbacks using the ID string directly
            if (sprite == null && !string.IsNullOrEmpty(resId))
            {
                sprite = Resources.Load<Sprite>("ui/Icons/icon" + resId); // e.g. iconGold
            }
            if (sprite == null && !string.IsNullOrEmpty(resId))
            {
                sprite = Resources.Load<Sprite>("ui/Icons/iconRes" + resId); // e.g. iconResMythril
            }
            
            // E. Final Fallback (Placeholder)
            if (sprite == null)
            {
                // Use bread or adamantine as generic fallback so it's not invisible
                sprite = Resources.Load<Sprite>("ui/Icons/iconResBread"); 
            }

            iconImg.sprite = sprite;
            
            var iconLE = iconGO.AddComponent<LayoutElement>();
            iconLE.preferredWidth = 24f;
            iconLE.preferredHeight = 24f;
            iconLE.minWidth = 24f;

            // 2. Name & Stats (Middle)
            var infoGO = new GameObject("Info");
            infoGO.transform.SetParent(rowGO.transform, false);
            var infoVL = infoGO.AddComponent<VerticalLayoutGroup>();
            infoVL.childAlignment = TextAnchor.MiddleLeft;
            infoVL.spacing = 0;

            var infoLE = infoGO.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1f;
            infoLE.preferredHeight = 30f;

            // Name
            CreateText(infoGO, asset != null ? asset.id : resId, 8, FontStyle.Bold);
            
            // Stats
            int amount = data.ResourceStockpiles.ContainsKey(resId) ? data.ResourceStockpiles[resId] : 0;
            int rate = data.ResourceRates.ContainsKey(resId) ? (int)data.ResourceRates[resId] : 0;

            string rateStr = rate > 0 ? $"+{rate}" : $"{rate}";
            string colorHex = rate < 0 ? "#FF5A5A" : "#7CFC00";

            string statsString = $"Stockpile: {amount} <color={colorHex}>({rateStr})</color>";
            
            CreateText(infoGO, statsString, 9, FontStyle.Normal);

            // 3. Trade Button
            var btnGO = new GameObject("TradeBtn");
            btnGO.transform.SetParent(rowGO.transform, false);
            var btnImg = btnGO.AddComponent<Image>();
            btnImg.sprite = tradeButtonSprite != null ? tradeButtonSprite : windowInnerSprite;
            btnImg.preserveAspect = true;

            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(() => 
            {
                SetVisible(false);
                TradeWindow.Open(resId);
            });

            var btnLE = btnGO.AddComponent<LayoutElement>();
            btnLE.preferredWidth = 24f; 
            btnLE.preferredHeight = 24f;
            btnLE.minWidth = 24f;
        }

        // Creates a Text UI element
        private static Text CreateText(GameObject parent, string content, int fontSize, FontStyle style)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent.transform, false);
            var txt = go.AddComponent<Text>();
            txt.font = Resources.Load<Font>("Fonts/Roboto-Regular") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = content;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.color = Color.white;
            txt.supportRichText = true;
            // Ensure text doesn't overflow weirdly
            txt.resizeTextForBestFit = false; 
            return txt;
        }

        // Builds a button with given label and click action
        private static void BuildButton(Transform parent, string label, Action onClick)
        {
            var go = new GameObject("Button_" + label);
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.sprite = windowInnerSprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 80;
            le.preferredHeight = 24;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var txt = CreateText(go, label, 10, FontStyle.Normal);
            txt.alignment = TextAnchor.MiddleCenter;
            var rt = txt.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
