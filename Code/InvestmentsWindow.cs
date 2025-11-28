using UnityEngine;
using UnityEngine.UI;
using System;

namespace RulerBox
{
    public static class InvestmentsWindow
    {
        private static GameObject root;
        private static Sprite windowInnerSprite;
        private static Sprite bgSprite;
        private static Sprite iconButtonInvSprite;
        private static Sprite placeholderIconSprite;

        public static void Initialize(Transform parent)
        {
            if (root != null) return;

            windowInnerSprite   = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.windowInnerSliced.png");
            bgSprite            = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.InvHub.png");
            iconButtonInvSprite      = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.ButtonInv.png");
            placeholderIconSprite = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.Investment.ResBread.png");

            if (BuildingPlacementTool.Instance == null)
            {
                var toolGo = new GameObject("RulerBox_PlacementTool");
                GameObject.DontDestroyOnLoad(toolGo);
                toolGo.AddComponent<BuildingPlacementTool>();
            }

            root = new GameObject("InvestmentsWindowRoot");
            root.transform.SetParent(parent, false);

            var rt = root.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var v = root.AddComponent<VerticalLayoutGroup>();
            v.childAlignment        = TextAnchor.UpperCenter;
            v.spacing               = 6;
            v.padding               = new RectOffset(4, 4, 4, 4);
            v.childControlWidth     = true;
            v.childControlHeight    = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight= false;

            // === TITLE ===
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(root.transform, false);
            var title = titleGO.AddComponent<Text>();
            title.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            title.text = "Investments";
            title.alignment = TextAnchor.MiddleCenter;
            title.color = Color.white;
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 10;
            title.resizeTextMaxSize = 12;
            titleGO.AddComponent<LayoutElement>().preferredHeight = 24f;

            // === SCROLL ===
            var scrollGO = new GameObject("InvestmentsScroll");
            scrollGO.transform.SetParent(root.transform, false);
            var scrollLE = scrollGO.AddComponent<LayoutElement>();
            scrollLE.preferredHeight = 140f;
            scrollLE.flexibleHeight  = 1f;

            var bgImg = scrollGO.AddComponent<Image>();
            if (windowInnerSprite != null)
            {
                bgImg.sprite = windowInnerSprite;
                bgImg.type   = Image.Type.Sliced;
            }
            else
            {
                bgImg.color = new Color(0f, 0f, 0f, 0.35f);
            }

            var scrollRect = scrollGO.AddComponent<ScrollRect>();
            scrollRect.horizontal   = false;
            scrollRect.vertical     = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(scrollGO.transform, false);
            var viewportRT = viewportGO.AddComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = new Vector2(2, 2);
            viewportRT.offsetMax = new Vector2(-2, -2);
            viewportGO.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
            viewportGO.AddComponent<Mask>().showMaskGraphic = false;

            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRT = contentGO.AddComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot     = new Vector2(0.5f, 1f);

            var contentVL = contentGO.AddComponent<VerticalLayoutGroup>();
            
            // Layout del contenuto
            contentVL.childAlignment        = TextAnchor.UpperCenter;
            contentVL.spacing               = 4;
            contentVL.padding               = new RectOffset(4, 4, 2, 2);
            contentVL.childControlWidth     = true;
            contentVL.childControlHeight    = true;
            contentVL.childForceExpandWidth = false;
            contentVL.childForceExpandHeight= false;

            contentGO.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRT;
            scrollRect.content  = contentRT;

            foreach (var def in InvestmentsList.Definitions)
            {
                BuildInvestmentRow(contentRT, def);
            }

            // === BOTTOM: BACK ===
            var bottomRow = new GameObject("BottomRow");
            bottomRow.transform.SetParent(root.transform, false);
            var bottomHL = bottomRow.AddComponent<HorizontalLayoutGroup>();
            bottomHL.childAlignment        = TextAnchor.MiddleCenter;
            bottomHL.childControlWidth     = true;
            bottomHL.childControlHeight    = true;
            bottomHL.childForceExpandWidth = false;
            bottomHL.childForceExpandHeight= false;
            bottomRow.AddComponent<LayoutElement>().preferredHeight = 26f;

            BuildBackButton(bottomRow.transform, "Back", () => TopPanelUI.ReturnToEconomyMain());

            root.SetActive(false);
        }

        public static void SetVisible(bool visible)
        {
            if (root != null)
            {
                root.SetActive(visible);
                if (visible && BuildingPlacementTool.Instance != null)
                {
                    BuildingPlacementTool.Instance.CancelPlacement();
                }
            }
        }

        public static bool IsVisible() => root != null && root.activeSelf;

        public static void Refresh(Kingdom k)
        {
            if (!IsVisible() || k == null) return;
        }

        // === ROW INVESTIMENTO ===
        private static void BuildInvestmentRow(Transform parent, InvestmentsList.InvestmentDefinition def)
        {
            var rowGO = new GameObject(def.Id + "_Row");
            rowGO.transform.SetParent(parent, false);

            // Background
            var rowImg = rowGO.AddComponent<Image>();
            rowImg.sprite = bgSprite != null ? bgSprite : windowInnerSprite;
            rowImg.type   = Image.Type.Sliced;

            var rowHL = rowGO.AddComponent<HorizontalLayoutGroup>();
            rowHL.childAlignment        = TextAnchor.MiddleLeft;
            rowHL.spacing               = 15;
            rowHL.padding               = new RectOffset(12, 4, 2, 2);
            rowHL.childControlWidth     = true;
            rowHL.childControlHeight    = true;
            rowHL.childForceExpandWidth = false;
            rowHL.childForceExpandHeight= false;
            var rowLE = rowGO.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 32f;
            rowLE.preferredWidth  = 155f;   
            rowLE.flexibleWidth   = 0f;

            // Icon
            var iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(rowGO.transform, false);
            var iconImg = iconGO.AddComponent<Image>();
            Sprite customSprite = null;
            if (!string.IsNullOrEmpty(def.IconPath))
            {
                customSprite = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.Investment."+def.IconPath+".png");
                if (customSprite == null)
                {
                    customSprite = Resources.Load<Sprite>("RulerBox.Resources.UI.Investment."+def.IconPath+".png");
                }
            }
            iconImg.sprite = (customSprite != null) ? customSprite : placeholderIconSprite;
            iconImg.preserveAspect = true;

            var iconLE = iconGO.AddComponent<LayoutElement>();
            iconLE.preferredWidth  = 14f;
            iconLE.preferredHeight = 24f;
            iconLE.minWidth        = 14f;

            // Name
            var nameGO = new GameObject("Name");
            nameGO.transform.SetParent(rowGO.transform, false);
            var nameText = nameGO.AddComponent<Text>();
            nameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            nameText.text = def.Name;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.color = Color.white;
            nameText.resizeTextForBestFit = false;
            nameText.fontSize = 9;

            var nameRT = nameText.GetComponent<RectTransform>();
            nameRT.anchorMin = Vector2.zero;
            nameRT.anchorMax = Vector2.one;
            nameRT.offsetMin = Vector2.zero;
            nameRT.offsetMax = Vector2.zero;

            var nameLE = nameGO.AddComponent<LayoutElement>();
            nameLE.flexibleWidth   = 1f;  
            nameLE.preferredHeight = 24f;

            // Place Button
            var btnGO = new GameObject("PlaceBtn");
            btnGO.transform.SetParent(rowGO.transform, false);
            var btnImg = btnGO.AddComponent<Image>();
            btnImg.sprite = iconButtonInvSprite != null ? iconButtonInvSprite : windowInnerSprite;
            btnImg.preserveAspect = true;

            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(() => OnPlaceClicked(def));

            var btnLE = btnGO.AddComponent<LayoutElement>();
            btnLE.preferredWidth  = 24f;
            btnLE.preferredHeight = 24f;
            btnLE.minWidth        = 24f;

            // Tooltip
            ChipTooltips.AttachSimpleTooltip(rowGO, () => GetInvestmentTooltip(def));
        }

        private static void OnPlaceClicked(InvestmentsList.InvestmentDefinition def)
        {
            ChipTooltips.ForceHideTooltip();

            Kingdom k = Main.selectedKingdom;
            if (k == null) return;

            if (k.capital != null)
            {
                if (!BuildingPlacementTool.CheckAffordability(k.capital, def))
                {
                    WorldTip.showNow("Capital cannot afford this (checking other cities...)", true, "top", 2f);
                }
            }
            else
            {
                WorldTip.showNow("Kingdom has no capital!", true, "top", 2f);
                return;
            }

            TopPanelUI.CloseAllWindows();

            if (BuildingPlacementTool.Instance != null)
            {
                BuildingPlacementTool.Instance.StartPlacement(k, def);
            }
        }

        private static string GetInvestmentTooltip(InvestmentsList.InvestmentDefinition def)
        {
            string tooltip = $"<b>{def.Name}</b>\n{def.Description}";

            Kingdom k = Main.selectedKingdom;
            if (k != null && def.TreasuryPctCost > 0f)
            {
                var data = KingdomMetricsSystem.Get(k);
                if (data != null && data.Treasury > 0)
                {
                    long goldCost = (long)(data.Treasury * def.TreasuryPctCost);
                    if (goldCost > 0)
                    {
                        tooltip += $"\n\nTreasury Cost: <color=#FFD700>{goldCost} Gold</color> ({(def.TreasuryPctCost*100):0}%)";
                    }
                }
            }

            if (def.Costs.Count > 0)
            {
                tooltip += "\n\nRequires:";
                foreach (var c in def.Costs)
                {
                    string resName = AssetManager.resources.get(c.ResourceId)?.id ?? c.ResourceId;
                    tooltip += $"\n- {c.Amount} {resName}";
                }
            }

            tooltip += "\n\n<b>Left Click</b> to place\n<b>Right Click</b> to cancel";
            return tooltip;
        }

        private static void BuildBackButton(Transform parent, string label, Action onClick)
        {
            var go = new GameObject(label.Replace(" ", "") + "Button");
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.sprite = windowInnerSprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth  = 90;
            le.preferredHeight = 20;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick?.Invoke());

            var txtGO = new GameObject("Text");
            txtGO.transform.SetParent(go.transform, false);
            var txt = txtGO.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = label;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.resizeTextForBestFit = true;
            txt.resizeTextMinSize = 6;
            txt.resizeTextMaxSize = 8;

            var rt = txt.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
