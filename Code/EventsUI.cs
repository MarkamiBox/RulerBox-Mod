using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace RulerBox
{
    // Types of event buttons
    public enum EventButtonType
    {
        Peace,
        War,
        Diplomacy,
        Random
    }

    // UI class to manage the events bar and event popups.
    public static class EventsUI
    {
        private static GameObject root;
        private static RectTransform contentRT;
        
        // --- Popup window ---
        private static GameObject popupRoot;
        private static Text popupText;
        private static Button popupOkButton;
        private static Button popupAcceptButton;
        private static Button popupRefuseButton;
        private static float popupTimer;
        private const float PopupAutoCloseSeconds = 5f;
        private static bool popupAutoCloseEnabled = true;
        private static Image popupFlagBg;
        private static Image popupFlagIcon;
        private static GameObject popupFlagWrapper;
        private static HorizontalLayoutGroup popupHeaderLayout;

        // initialize the events bar UI
        public static void Initialize()
        {
            if (root != null) return;
            
            // root bar
            root = new GameObject("RulerBox_EventsBar");
            root.transform.SetParent(DebugConfig.instance?.transform, false);
            root.transform.SetAsLastSibling(); 
            
            // background image (invisible, just for raycast)
            var bg = root.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.05f, 0.001f);
            bg.raycastTarget = false;
            
            // rect transform
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.3f);
            rt.anchorMax = new Vector2(1f, 0.3f);
            rt.pivot     = new Vector2(0f, 0.5f);
            rt.offsetMin = new Vector2(25f, -30f);
            rt.offsetMax = new Vector2(-10f,  20f);
            
            // scroll rect
            var scrollGO = new GameObject("Scroll");
            scrollGO.transform.SetParent(root.transform, false);
            
            // scroll rect components
            var scrollRT = scrollGO.AddComponent<RectTransform>();
            scrollRT.anchorMin = new Vector2(0f, 0f);
            scrollRT.anchorMax = new Vector2(1f, 1f);
            scrollRT.offsetMin = new Vector2(4f, 4f);
            scrollRT.offsetMax = new Vector2(-4f, -4f);
            
            // background for scroll area
            var scrollBg = scrollGO.AddComponent<Image>();
            scrollBg.color = new Color(0f, 0f, 0f, 0.25f);
            scrollBg.raycastTarget = false;
            
            // mask
            var mask = scrollGO.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            
            // scroll rect itself
            var scroll = scrollGO.AddComponent<ScrollRect>();
            scroll.horizontal   = true;
            scroll.vertical     = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia      = true;
            
            // content holder for scroll rect
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(scrollGO.transform, false);
            
            // content rect transform
            contentRT = contentGO.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 0f);
            contentRT.anchorMax = new Vector2(0f, 1f);
            contentRT.pivot     = new Vector2(0f, 0.5f);
            
            // layout group for horizontal list
            var layout = contentGO.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment         = TextAnchor.MiddleLeft;
            layout.spacing                = 4f;
            layout.padding                = new RectOffset(4, 4, 2, 2);
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth  = false;
            layout.childControlHeight     = true;
            layout.childControlWidth      = true;
            
            // content size fitter to auto-size the content rect
            var fitter = contentGO.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit   = ContentSizeFitter.FitMode.Unconstrained;
            scroll.content = contentRT;
            root.SetActive(false);
        }

        /// Show or hide the events bar
        public static void SetVisible(bool visible)
        {
            if (root == null) Initialize();
            root.SetActive(visible);
            if (!visible)
            {
                HidePopup();
            }
        }
        
        /// Get the resource name for a given event button type
        private static string GetEventSpriteResource(EventButtonType type)
        {
            switch (type)
            {
                case EventButtonType.Peace:
                    return "RulerBox.Resources.UI.Events.PeaceEvent.png";
                case EventButtonType.War:
                    return "RulerBox.Resources.UI.Events.WarEvent.png";
                case EventButtonType.Diplomacy:
                    return "RulerBox.Resources.UI.Events.DiplomacyEvent.png";
                case EventButtonType.Random:
                default:
                    return "RulerBox.Resources.UI.Events.RandomEvent.png";
            }
        }

        /// Add an event button to the events bar
        public static GameObject AddEventButton(EventButtonType type, string title, int eventId)
        {
            if (contentRT == null) return null;
            
            // create the button game object
            var go = new GameObject($"Event_{type}_{eventId}");
            go.transform.SetParent(contentRT, false);
            
            // components
            var img = go.AddComponent<Image>();
            string resName = GetEventSpriteResource(type);
            var sprite = Mod.EmbededResources.LoadSprite(resName);
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
            img.preserveAspect = false;
            
            // rect transform
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth  = 25f;
            le.preferredHeight = 40f;
            le.minWidth        = 25f;
            le.minHeight       = 40f;
            
            // button component
            var btn = go.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            btn.onClick.AddListener(() =>
            {
                EventsSystem.OpenEvent(eventId);
            });
            
            // Show the bar if hidden
            if (!root.activeSelf)
                root.SetActive(true);
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRT);
            return go;
        }

        // =====================================================================
        // ========================== POPUP WINDOW ==============================
        // =====================================================================

        /// Ensure the popup window is created
        private static void EnsurePopup()
        {
            if (popupRoot != null) return;
            popupRoot = new GameObject("RulerBox_EventPopup");
            popupRoot.transform.SetParent(DebugConfig.instance?.transform, false);
            popupRoot.transform.SetAsLastSibling();
            
            // background image
            var bg = popupRoot.AddComponent<Image>();
            bg.sprite = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.EventsHub.png");
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;
            bg.raycastTarget = true;
            
            // rect transform
            var rt = popupRoot.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.6f);
            rt.anchorMax = new Vector2(0.5f, 0.6f);
            rt.pivot     = new Vector2(0.5f, 0.6f);
            rt.sizeDelta = new Vector2(150f, 80f);
            rt.anchoredPosition = new Vector2(0f, 0f);
            
            // inner content
            var inner = new GameObject("Inner");
            inner.transform.SetParent(popupRoot.transform, false);
            var innerRT = inner.AddComponent<RectTransform>();
            innerRT.anchorMin = new Vector2(0f, 0f);
            innerRT.anchorMax = new Vector2(1f, 1f);
            innerRT.offsetMin = new Vector2(8f, 6f);
            innerRT.offsetMax = new Vector2(-8f, -6f);
            
            // layout group
            var v = inner.AddComponent<VerticalLayoutGroup>();
            v.childAlignment = TextAnchor.MiddleCenter;
            v.spacing = 4f;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            
            // header row (flag + text)
            var header = new GameObject("Header");
            header.transform.SetParent(inner.transform, false);
            
            // header layout
            var headerRT = header.AddComponent<RectTransform>();
            popupHeaderLayout = header.AddComponent<HorizontalLayoutGroup>();   
            var headerLayout = popupHeaderLayout;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.spacing = 6f;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandWidth = true;
            headerLayout.childForceExpandHeight = false;
            headerLayout.padding = new RectOffset(2, 2, 2, 2);
            
            // header layout element
            var headerLE = header.AddComponent<LayoutElement>();
            headerLE.preferredHeight = 40f;
            
            // flag (left side)
            var flagWrapper = new GameObject("Flag");
            flagWrapper.transform.SetParent(header.transform, false);
            popupFlagWrapper = flagWrapper;
            
            // flag layout element
            var flagLE = flagWrapper.AddComponent<LayoutElement>();
            flagLE.preferredWidth  = 38f;
            flagLE.preferredHeight = 42f;
            flagLE.minWidth        = 38f;
            flagLE.minHeight       = 42f;
            
            // flag rect transform
            var flagRT = flagWrapper.GetComponent<RectTransform>();
            if (flagRT == null)
                flagRT = flagWrapper.AddComponent<RectTransform>();
            flagRT.sizeDelta = new Vector2(85f, 85f);
            
            // background banner
            var bgGO = new GameObject("FlagBG");
            bgGO.transform.SetParent(flagWrapper.transform, false);
            popupFlagBg = bgGO.AddComponent<Image>();
            
            // bg rect transform
            var bgRT2 = bgGO.GetComponent<RectTransform>();
            bgRT2.anchorMin = Vector2.zero;
            bgRT2.anchorMax = Vector2.one;
            bgRT2.offsetMin = Vector2.zero;
            bgRT2.offsetMax = Vector2.zero;
            
            // icon inside banner
            var iconGO = new GameObject("FlagIcon");
            iconGO.transform.SetParent(flagWrapper.transform, false);
            popupFlagIcon = iconGO.AddComponent<Image>();
            
            // icon rect transform
            var iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.anchorMin = Vector2.zero;
            iconRT.anchorMax = Vector2.one;
            iconRT.offsetMin = new Vector2(3, 3);
            iconRT.offsetMax = new Vector2(-3, -3);
            
            // Text to the right of the flag
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(header.transform, false);
            popupText = textGO.AddComponent<Text>();
            popupText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            popupText.supportRichText = true;
            popupText.alignment = TextAnchor.MiddleLeft;
            popupText.color = Color.white;
            popupText.resizeTextForBestFit = true;
            popupText.resizeTextMinSize = 6;
            popupText.resizeTextMaxSize = 14;
            
            // text rect transform
            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = new Vector2(0f, 0f);
            textRT.anchorMax = new Vector2(1f, 1f);
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            
            // text layout element
            var textLE = textGO.AddComponent<LayoutElement>();
            textLE.flexibleWidth = 1f;
            textLE.minHeight = 20f;
            
            // Buttons row
            var buttonsRow = new GameObject("ButtonsRow");
            buttonsRow.transform.SetParent(inner.transform, false);
            var rowRT = buttonsRow.AddComponent<RectTransform>();
            rowRT.anchorMin = new Vector2(0f, 0f);
            rowRT.anchorMax = new Vector2(1f, 0f);
            
            // buttons layout
            var h = buttonsRow.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.LowerCenter;
            h.spacing = 8f;
            h.childControlWidth = true;
            h.childForceExpandWidth = false;
            h.childControlHeight = true;
            h.childForceExpandHeight = false;
            
            // buttons row layout element
            var rowLE = buttonsRow.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 26f;
            
            // Helper to build a button
            Button BuildButton(string name, string label)
            {
                var bGO = new GameObject(name);
                bGO.transform.SetParent(buttonsRow.transform, false);
                
                // button image
                var bImg = bGO.AddComponent<Image>();
                bImg.sprite = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.special_buttonRed.png");
                bImg.type = Image.Type.Sliced;
                bImg.color = Color.white;
                
                // button rect transform
                var bRT = bGO.GetComponent<RectTransform>();
                bRT.sizeDelta = new Vector2(60f, 22f);
                
                // button component
                var b = bGO.AddComponent<Button>();
                b.transition = Selectable.Transition.ColorTint;
                
                // button text
                var tGO = new GameObject("Text");
                tGO.transform.SetParent(bGO.transform, false);
                var t = tGO.AddComponent<Text>();
                t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                t.text = label;
                t.alignment = TextAnchor.MiddleCenter;
                t.color = Color.white;
                t.resizeTextForBestFit = true;
                t.resizeTextMinSize = 5;
                t.resizeTextMaxSize = 7;
                
                // text rect transform 
                var tRT2 = tGO.GetComponent<RectTransform>();
                tRT2.anchorMin = new Vector2(0f, 0f);
                tRT2.anchorMax = new Vector2(1f, 1f);
                tRT2.offsetMin = Vector2.zero;
                tRT2.offsetMax = Vector2.zero;
                
                // button layout element
                var le = bGO.AddComponent<LayoutElement>();
                le.preferredWidth = 70f;
                le.preferredHeight = 22f;
                return b;
            }

            // build buttons
            popupOkButton     = BuildButton("OkButton", "OK");
            popupAcceptButton = BuildButton("AcceptButton", "Accept");
            popupRefuseButton = BuildButton("RefuseButton", "Refuse");
            popupRoot.SetActive(false);
        }

        /// Show the event popup window
        public static void ShowPopup(
            string message,
            EventButtonType type,
            Kingdom sourceKingdom,
            System.Action onOk,
            System.Action onAccept,
            System.Action onRefuse,
            string acceptTooltip = null,
            string refuseTooltip = null,
            string acceptLabel = "Accept",
            string refuseLabel = "Refuse"
            )
        {
            EnsurePopup();
            
            // Show / hide flag: no flag for Random events
            bool showFlag = (type != EventButtonType.Random) && (sourceKingdom != null);
            
            // Enable/disable the whole flag wrapper so layout doesn't reserve space
            if (popupFlagWrapper != null)
                popupFlagWrapper.SetActive(showFlag);
            if (popupFlagBg != null)
                popupFlagBg.gameObject.SetActive(showFlag);
            if (popupFlagIcon != null)
                popupFlagIcon.gameObject.SetActive(showFlag);
            if (showFlag && sourceKingdom != null)
            {
                var col = sourceKingdom.kingdomColor;
                popupFlagBg.sprite  = sourceKingdom.getElementBackground();
                popupFlagBg.color   = col.getColorMain32();
                popupFlagIcon.sprite = sourceKingdom.getElementIcon();
            }
            popupText.text = message;
            
            // If Random: no flag, only text child -> center everything
            if (type == EventButtonType.Random)
            {
                if (popupHeaderLayout != null)
                    popupHeaderLayout.childAlignment = TextAnchor.MiddleCenter;

                popupText.alignment = TextAnchor.MiddleCenter;
            }
            else
            {
                if (popupHeaderLayout != null)
                    popupHeaderLayout.childAlignment = TextAnchor.MiddleLeft;

                popupText.alignment = TextAnchor.MiddleLeft;
            }
            
            // Clear previous listeners
            popupOkButton.onClick.RemoveAllListeners();
            popupAcceptButton.onClick.RemoveAllListeners();
            popupRefuseButton.onClick.RemoveAllListeners();
            
            // Default behavior: all buttons close the popup.
            popupOkButton.onClick.AddListener(() =>
            {
                onOk?.Invoke();
                HidePopup();
            });
            popupAcceptButton.onClick.AddListener(() =>
            {
                onAccept?.Invoke();
                HidePopup();
            });
            popupRefuseButton.onClick.AddListener(() =>
            {
                onRefuse?.Invoke();
                HidePopup();
            });
            
            // Auto-close if there are no choices
            bool hasChoices = (onAccept != null) || (onRefuse != null);
            popupAutoCloseEnabled = !hasChoices;
            popupTimer = 0f;
            
            // Configure which buttons are visible depending on type.
            switch (type)
            {
                case EventButtonType.War:
                    popupOkButton.gameObject.SetActive(true);
                    popupAcceptButton.gameObject.SetActive(false);
                    popupRefuseButton.gameObject.SetActive(false);
                    break;
                case EventButtonType.Peace:
                case EventButtonType.Diplomacy:
                    popupOkButton.gameObject.SetActive(false);
                    popupAcceptButton.gameObject.SetActive(true);
                    popupRefuseButton.gameObject.SetActive(true);
                    break;
                case EventButtonType.Random:
                default:
                    // Random event: if it has choices, show Accept/Refuse instead of OK
                    bool hasAccept = onAccept != null;
                    bool hasRefuse = onRefuse != null;
                    bool hasOk     = onOk     != null;
                    if (hasAccept || hasRefuse)
                    {
                        popupOkButton.gameObject.SetActive(false);
                        popupAcceptButton.gameObject.SetActive(hasAccept);
                        popupRefuseButton.gameObject.SetActive(hasRefuse);
                    }
                    else
                    {
                        popupOkButton.gameObject.SetActive(hasOk);
                        popupAcceptButton.gameObject.SetActive(false);
                        popupRefuseButton.gameObject.SetActive(false);
                    }
                    break;
            }
            if (popupAcceptButton != null)
            {
                var txt = popupAcceptButton.GetComponentInChildren<Text>();
                if (txt != null)
                    txt.text = acceptLabel;
            }
            if (popupRefuseButton != null)
            {
                var txt = popupRefuseButton.GetComponentInChildren<Text>();
                if (txt != null)
                    txt.text = refuseLabel;
            }
            
            // tooltips for accept/refuse buttons
            // First remove any previous tooltip bindings on these buttons
            ChipTooltips.ClearSimpleTooltip(popupAcceptButton.gameObject);
            ChipTooltips.ClearSimpleTooltip(popupRefuseButton.gameObject);
            if (!string.IsNullOrEmpty(acceptTooltip) && popupAcceptButton.gameObject.activeSelf)
            {
                ChipTooltips.AttachSimpleTooltip(
                    popupAcceptButton.gameObject,
                    () => acceptTooltip
                );
            }
            if (!string.IsNullOrEmpty(refuseTooltip) && popupRefuseButton.gameObject.activeSelf)
            {
                ChipTooltips.AttachSimpleTooltip(
                    popupRefuseButton.gameObject,
                    () => refuseTooltip
                );
            }
            popupRoot.SetActive(true);
        }

        /// Hide the event popup window
        public static void HidePopup()
        {
            if (popupRoot != null)
            {
                popupRoot.SetActive(false);
                popupTimer = 0f;
            }
            ChipTooltips.HideTooltipNow();
        }
        
        /// Tick function to be called every frame to handle auto-close of popup
        public static void TickPopup(float dt)
        {
            if (popupRoot == null || !popupRoot.activeSelf)
                return;

            if (!popupAutoCloseEnabled)
                return;

            popupTimer += dt;
            if (popupTimer >= PopupAutoCloseSeconds)
            {
                HidePopup();
            }
        }
    }
}
