using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace RulerBox
{
    public static class TradeWindow
    {
        private static GameObject root;
        private static Sprite windowInnerSprite;
        private static Sprite bgSprite;
        private static string currentResourceId;
        private static Text titleText;

        public static void Initialize(Transform parent)
        {
            if (root != null) return;

            windowInnerSprite = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.windowInnerSliced.png");
            bgSprite          = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.InvHub.png");

            // === ROOT ===
            root = new GameObject("TradeWindowRoot");
            root.transform.SetParent(parent, false);

            // Full stretch
            var rt = root.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Layout
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
            titleText = titleGO.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.text = "Trade";
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;
            titleText.resizeTextForBestFit = true;
            titleText.resizeTextMinSize = 10;
            titleText.resizeTextMaxSize = 14;
            titleGO.AddComponent<LayoutElement>().preferredHeight = 24f;

            // === SCROLL VIEW ===
            var scrollGO = new GameObject("TradeScroll");
            scrollGO.transform.SetParent(root.transform, false);
            var scrollLE = scrollGO.AddComponent<LayoutElement>();
            scrollLE.preferredHeight = 200f;
            scrollLE.flexibleHeight = 1f;

            // === SCROLLVIEW CONTENT ===
            var bgImg = scrollGO.AddComponent<Image>();
            bgImg.sprite = windowInnerSprite;
            bgImg.type = Image.Type.Sliced;
            if(bgImg.sprite == null) bgImg.color = new Color(0,0,0,0.35f);

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

            // Content Layout
            var contentVL = contentGO.AddComponent<VerticalLayoutGroup>();
            contentVL.childAlignment = TextAnchor.UpperCenter;
            contentVL.spacing = 2; 
            contentVL.padding = new RectOffset(2, 2, 2, 2);
            contentVL.childControlWidth = true; 
            contentVL.childControlHeight = true;
            contentVL.childForceExpandWidth = false; 
            contentVL.childForceExpandHeight = false;

            contentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRT;
            scrollRect.content = contentRT;

            // === BOTTOM ROW ===
            var bottomRow = new GameObject("BottomRow");
            bottomRow.transform.SetParent(root.transform, false);
            var bottomHL = bottomRow.AddComponent<HorizontalLayoutGroup>();
            bottomHL.childAlignment = TextAnchor.MiddleCenter;
            bottomHL.childControlWidth = false;
            bottomHL.childForceExpandWidth = false;
            bottomRow.AddComponent<LayoutElement>().preferredHeight = 26f;

            BuildButton(bottomRow.transform, "Back", () =>
            {
                SetVisible(false);
                ResourcesTradeWindow.SetVisible(true);
            }, 80f, 22f);

            root.SetActive(false);
        }

        // --- PUBLIC API ---
        public static void Open(string resourceId)
        {
            if (root == null) return;
            currentResourceId = resourceId;
            if (titleText != null) titleText.text = "Trade: " + resourceId;
            ResourcesTradeWindow.SetVisible(false);
            SetVisible(true);
            Refresh(Main.selectedKingdom);
        }

        public static void SetVisible(bool visible)
        {
            if (root != null) root.SetActive(visible);
        }

        public static bool IsVisible() => root != null && root.activeSelf;

        public static void UpdateLogic()
        {
            if (!IsVisible()) return;
            var k = Main.selectedKingdom;
            if (k == null || !k.isAlive()) SetVisible(false);
        }

        public static void Refresh(Kingdom k)
        {
            if (root == null || !IsVisible()) return;
            Transform content = root.transform.Find("TradeScroll/Viewport/Content");
            if (content == null) return;
            foreach (Transform child in content) GameObject.Destroy(child.gameObject);
            
            if (k == null || World.world?.kingdoms == null) return;

            foreach (var other in World.world.kingdoms.list)
            {
                if (other == null || !other.isAlive() || other == k) continue;
                BuildKingdomRow(content, k, other);
            }
        }

        // --- UI BUILDERS ---
        private static void BuildKingdomRow(Transform parent, Kingdom player, Kingdom other)
        {
            var rowGO = new GameObject(other.data.id + "_TradeRow");
            rowGO.transform.SetParent(parent, false);

            // Background
            var bg = rowGO.AddComponent<Image>();
            bg.sprite = bgSprite != null ? bgSprite : windowInnerSprite;
            bg.type = Image.Type.Sliced;

            // Layout
            var rowHL = rowGO.AddComponent<HorizontalLayoutGroup>();
            rowHL.childAlignment = TextAnchor.MiddleLeft;
            rowHL.spacing = 8; 
            rowHL.padding = new RectOffset(4, 2, 2, 2);
            rowHL.childControlWidth = true;
            rowHL.childControlHeight = true;
            rowHL.childForceExpandWidth = false;

            // Layout Element
            var rowLE = rowGO.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 40f;
            rowLE.preferredWidth = 155f;
            rowLE.minWidth = 155f;
            rowLE.flexibleWidth = 0f;

            // === 1. FLAG ===
            var flag = BuildFlag(other);
            flag.transform.SetParent(rowGO.transform, false);
            
            var flagLE = flag.GetComponent<LayoutElement>();
            flagLE.preferredWidth = 30f;
            flagLE.preferredHeight = 20f;
            flagLE.minWidth = 30f;

            // === 2. TEXT (Flexible) ===
            var txtStackGO = new GameObject("TextStack");
            txtStackGO.transform.SetParent(rowGO.transform, false);
            
            var txtVL = txtStackGO.AddComponent<VerticalLayoutGroup>();
            txtVL.childAlignment = TextAnchor.MiddleLeft;
            txtVL.spacing = -2;
            txtVL.childControlWidth = true;
            txtVL.childForceExpandWidth = true;

            var textStackLE = txtStackGO.AddComponent<LayoutElement>();
            textStackLE.flexibleWidth = 1f; 
            textStackLE.minWidth = 0f;      

            var nameTxt = CreateText(txtStackGO.transform, other.data.name, 6, FontStyle.Bold, Color.yellow);
            nameTxt.horizontalOverflow = HorizontalWrapMode.Wrap; 

            int stock = 0;
            if (other.cities != null)
            {
                foreach (var c in other.cities)
                {
                    if (c != null && c.isAlive())
                    {
                        stock += c.getResourcesAmount(currentResourceId);
                    }
                }
            }

            string unitStr = $"[{currentResourceId}: {stock}]";
            CreateText(txtStackGO.transform, unitStr, 5, FontStyle.Normal, new Color(0.8f, 0.8f, 0.8f));

            // === 3. CONTROLS (Right - Fixed Width) ===
            var controlsGO = new GameObject("Controls");
            controlsGO.transform.SetParent(rowGO.transform, false);

            var controlsHL = controlsGO.AddComponent<HorizontalLayoutGroup>();
            controlsHL.childAlignment = TextAnchor.MiddleRight;
            controlsHL.spacing = 1;
            controlsHL.childControlWidth = true;
            controlsHL.childForceExpandWidth = false;

            var controlsLE = controlsGO.AddComponent<LayoutElement>();
            controlsLE.minWidth = 76f; 
            controlsLE.preferredWidth = 76f;
            controlsLE.flexibleWidth = 0f;

            InputField inputField = BuildTinyInput(controlsGO.transform, "1");

            int GetAmount() {
                if (int.TryParse(inputField.text, out int result)) return Math.Max(1, result);
                return 1;
            }

            // A. Cancel Button (Layout Fixed)
            var btnCancel = BuildButton(controlsGO.transform, "X", () => { 
                TradeManager.CancelAllTrades(player, other, currentResourceId);
            }, 10f, 10f, new Color(0.5f, 0.1f, 0.1f));
            
            var btnCancelLE = btnCancel.GetComponent<LayoutElement>();
            if(btnCancelLE != null)
            {
                btnCancelLE.flexibleHeight = 0f; // Prevent stretching to row height
                btnCancelLE.preferredHeight = 12f;
            }

            // B. Sell Stack
            BuildVerticalButtonStack(controlsGO.transform, "Sell", "Bulk", 
                () => EventsSystem.TriggerTradeProposal(other, currentResourceId, GetAmount(), true, false), 
                () => EventsSystem.TriggerTradeProposal(other, currentResourceId, GetAmount(), true, true)
            );

            // C. Input
            inputField.gameObject.transform.SetSiblingIndex(2);

            // D. Buy Stack
            BuildVerticalButtonStack(controlsGO.transform, "Buy", "Bulk", 
                () => EventsSystem.TriggerTradeProposal(other, currentResourceId, GetAmount(), false, false),
                () => EventsSystem.TriggerTradeProposal(other, currentResourceId, GetAmount(), false, true)
            );
        }

        // Builds a vertical stack of two small buttons
        private static void BuildVerticalButtonStack(Transform parent, string topLabel, string botLabel, Action onTop, Action onBot)
        {
            var colGO = new GameObject("BtnCol");
            colGO.transform.SetParent(parent, false);
            
            // Vertical Layout
            var vl = colGO.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 1; 
            vl.childControlWidth = true;
            vl.childControlHeight = true;
            vl.childForceExpandHeight = false;

            // Layout Element
            var le = colGO.AddComponent<LayoutElement>();
            le.preferredWidth = 14f;
            le.preferredHeight = 16f; 
            le.minWidth = 14f;

            void MakeMini(string txt, Action act)
            {
                var bGO = new GameObject("MiniBtn");
                bGO.transform.SetParent(colGO.transform, false);
                var img = bGO.AddComponent<Image>();
                img.sprite = windowInnerSprite;
                img.type = Image.Type.Sliced;
                
                var btn = bGO.AddComponent<Button>();
                btn.targetGraphic = img;
                btn.onClick.AddListener(() => act?.Invoke());
                
                var tGO = new GameObject("Text");
                tGO.transform.SetParent(bGO.transform, false);
                var t = tGO.AddComponent<Text>();
                t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                t.text = txt;
                t.fontSize = 6;
                t.alignment = TextAnchor.MiddleCenter;
                t.color = Color.white;                
                t.raycastTarget = false; 

                var rt = t.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                
                bGO.AddComponent<LayoutElement>().flexibleHeight = 1f;
            }

            MakeMini(topLabel, onTop);
            MakeMini(botLabel, onBot);
        }

        // Builds a small input field
        private static InputField BuildTinyInput(Transform parent, string def)
        {
            var go = new GameObject("Input");
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.sprite = windowInnerSprite;
            img.type = Image.Type.Sliced;
            img.color = new Color(0, 0, 0, 0.5f);
            img.raycastTarget = true; 

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 22f; 
            le.preferredHeight = 20f;
            le.minWidth = 22f;

            var input = go.AddComponent<InputField>();
            
            var txtGO = new GameObject("Text");
            txtGO.transform.SetParent(go.transform, false);
            var txt = txtGO.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = def;
            txt.fontSize = 8;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.raycastTarget = false; 

            input.textComponent = txt;

            var rt = txt.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(0, -1);
            rt.offsetMax = new Vector2(0, 0);
            
            return input;
        }

        // Creates a text element
        private static Text CreateText(Transform parent, string content, int fontSize, FontStyle style, Color col)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var txt = go.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = content;
            txt.fontSize = fontSize;
            txt.fontStyle = style;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.color = col;
            txt.resizeTextForBestFit = false;
            
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f; 
            le.minHeight = fontSize + 4;
            return txt;
        }

        // Builds a button
        private static GameObject BuildButton(Transform parent, string label, Action onClick, float width, float height, Color? colorOverride = null)
        {
            var go = new GameObject("Btn_" + label);
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.sprite = windowInnerSprite;
            img.type = Image.Type.Sliced;
            img.color = colorOverride ?? Color.white;

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;
            le.minWidth = width; 
            le.flexibleWidth = 0; 

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var txtGO = new GameObject("Label");
            txtGO.transform.SetParent(go.transform, false);
            var txt = txtGO.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = label;
            txt.fontSize = 6; 
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.resizeTextForBestFit = true;
            txt.resizeTextMinSize = 5;
            txt.resizeTextMaxSize = 9;

            var rt = txt.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return go;
        }

        // Builds a kingdom flag UI element
        private static GameObject BuildFlag(Kingdom k)
        {
            var wrapper = new GameObject("FlagWrapper");

            var le = wrapper.AddComponent<LayoutElement>();
            le.preferredWidth  = 18f;
            le.preferredHeight = 35f;
            le.minWidth        = 18f;
            le.minHeight       = 35f;
            le.flexibleWidth   = 0f;
            le.flexibleHeight  = 0f;

            var bgGO = new GameObject("FlagBG");
            bgGO.transform.SetParent(wrapper.transform, false);
            var bg = bgGO.AddComponent<Image>();

            var bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;

            var iconGO = new GameObject("FlagIcon");
            iconGO.transform.SetParent(wrapper.transform, false);
            var icon = iconGO.AddComponent<Image>();

            var iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.anchorMin = Vector2.zero;
            iconRT.anchorMax = Vector2.one;
            iconRT.offsetMin = new Vector2(2, 2);
            iconRT.offsetMax = new Vector2(-2, -2);

            if (k.kingdomColor != null)
            {
                bg.color   = k.kingdomColor.getColorMain32();
                icon.color = k.kingdomColor.getColorBanner();
            }

            bg.sprite   = k.getElementBackground();
            icon.sprite = k.getElementIcon();

            return wrapper;
        }
    }
}