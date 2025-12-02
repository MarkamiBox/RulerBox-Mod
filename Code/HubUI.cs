using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;
using NCMS.Utils;

namespace RulerBox
{
    public static class HubUI
    {
        private static GameObject root;
        private static GameObject contentRow;
        private static Text economyText;
        private static Text populationText;
        private static Text manpowerText;
        private static Text warExhaustionText;
        private static Text stabilityText;
        private static Image flagBg;
        private static Image flagIcon; 

        public static void Initialize()
        {
            if (root != null) return;
            CreateHubUI();
            SetVisibility(false);
        }
        
        // create the main hub UI
        private static void CreateHubUI()
        {
            root = new GameObject("RulerBox_Hub");
            root.transform.SetParent(DebugConfig.instance?.transform);
            
            // background
            var bg = root.AddComponent<Image>();
            bg.sprite = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.HubBarUI.png");
            bg.type = Image.Type.Sliced;
            bg.raycastTarget = true;
            
            // position and size
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0, 0);
            rt.sizeDelta = new Vector2(800, 80);
            
            // Close when clicking the background
            var trigger = root.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener((_) => SetVisibility(false));
            trigger.triggers.Add(entry);
            
            // content row
            contentRow = new GameObject("ContentRow");
            contentRow.transform.SetParent(root.transform, false);
            var rowRT = contentRow.AddComponent<RectTransform>();
            rowRT.anchorMin = new Vector2(0, 0);
            rowRT.anchorMax = new Vector2(1, 1);
            rowRT.offsetMin = new Vector2(20, 10);
            rowRT.offsetMax = new Vector2(-20, -10);
            
            // horizontal layout
            var h = contentRow.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleLeft;
            h.spacing = 15;
            h.padding = new RectOffset(-40, 20, 10, 10);
            
            // build flag button
            var flag = BuildFlag(15f, 80f);
            flag.transform.SetParent(contentRow.transform, false);
            
            // economy chip 
            var economy = BuildChip("Economy", out economyText);
            economy.transform.SetParent(contentRow.transform, false);
            ChipTooltips.AttachEconomyTooltip(economy);
            
            // population chip
            var population = BuildChip("Population", out populationText);
            population.transform.SetParent(contentRow.transform, false);
            ChipTooltips.AttachPopulationTooltip(population);
            
            // manpower chip
            var manpower = BuildChip("Manpower", out manpowerText);
            manpower.transform.SetParent(contentRow.transform, false);
            ChipTooltips.AttachManpowerTooltip(manpower);
            
            // war exhaustion chip
            var warExhaustion = BuildChip("War", out warExhaustionText);
            warExhaustion.transform.SetParent(contentRow.transform, false);
            ChipTooltips.AttachWarTooltip(warExhaustion);
            
            // stability chip
            var stability = BuildChip("Stability", out stabilityText);
            stability.transform.SetParent(contentRow.transform, false);
            ChipTooltips.AttachStabilityTooltip(stability);
        }

        // build the kingdom flag button
        private static GameObject BuildFlag(float width, float height)
        {
            var wrapper = new GameObject("FlagWrapper");
            
            // layout element
            var le = wrapper.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;
            le.minWidth = width;
            le.minHeight = height;
            
            // button background
            var bgClickable = wrapper.AddComponent<Image>();
            bgClickable.color = Color.clear; 
            
            // button component
            var btn = wrapper.AddComponent<Button>();
            btn.transition = Selectable.Transition.ColorTint;
            btn.onClick.AddListener(() =>
            {
                if (Main.selectedKingdom != null)
                {
                    TopPanelUI.Toggle();
                }
            });
            
            // flag background and icon
            var bgGO = new GameObject("FlagBG");
            bgGO.transform.SetParent(wrapper.transform, false);
            flagBg = bgGO.AddComponent<Image>();
            
            // set rect transform to fill
            var bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            
            // icon
            var iconGO = new GameObject("FlagIcon");
            iconGO.transform.SetParent(wrapper.transform, false);
            flagIcon = iconGO.AddComponent<Image>();
            
            // set rect transform to fill with padding
            var iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.anchorMin = Vector2.zero;
            iconRT.anchorMax = Vector2.one;
            iconRT.offsetMin = new Vector2(3, 3);
            iconRT.offsetMax = new Vector2(-3, -3);
            return wrapper;
        }

        // build a info chip with label and value text
        private static GameObject BuildChip(string label, out Text valueText)
        {
            var chip = new GameObject($"{label}Chip");
            var img = chip.AddComponent<Image>();
            img.sprite = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.ToolTip.png");
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            
            // size
            var rt = chip.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(190, 40);
            
            // inner container
            var inner = new GameObject("Inner");
            inner.transform.SetParent(chip.transform, false);
            var innerRT = inner.AddComponent<RectTransform>();
            innerRT.anchorMin = Vector2.zero;
            innerRT.anchorMax = Vector2.one;
            innerRT.offsetMin = new Vector2(8, 4);
            innerRT.offsetMax = new Vector2(-8, -4);
            
            // horizontal layout
            var h = inner.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleRight;
            h.spacing = 8;
            h.childForceExpandWidth = false;
            
            // label text
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(inner.transform, false);
            var labelText = labelGO.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            labelText.text = label;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = new Color(0.8f, 0.9f, 1f, 0.9f);
            labelText.resizeTextForBestFit = true;
            labelText.resizeTextMinSize = 8;
            labelText.resizeTextMaxSize = 18;
            
            // value text
            var valueGO = new GameObject("Value");
            valueGO.transform.SetParent(inner.transform, false);
            valueText = valueGO.AddComponent<Text>();
            valueText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            valueText.text = "-";
            valueText.alignment = TextAnchor.MiddleRight;
            valueText.color = new Color(1f, 0.9f, 0.5f, 1f);
            valueText.resizeTextForBestFit = true;
            valueText.resizeTextMinSize = 12;
            valueText.resizeTextMaxSize = 22;
            return chip;
        }

        // set hub visibility
        public static void SetVisibility(bool visible)
        {
            if (root == null) Initialize();
            root.SetActive(visible);
            if (visible) Refresh();
            else TopPanelUI.SetVisible(false);
        }
        
        // refresh the displayed data
        public static void Refresh()
        {
            var k = Main.selectedKingdom;
            if (k == null) return;
            
            // update flag
            if (flagBg != null && flagIcon != null)
            {
                var col = k.kingdomColor;
                flagBg.sprite = k.getElementBackground();
                flagBg.color = col.getColorMain32();
                flagIcon.sprite = k.getElementIcon();
                flagIcon.color = col.getColorBanner();
            }
            
            // update metrics
            var d = KingdomMetricsSystem.Get(k);
            if (d != null)
            {
                var bal = ChipTooltips.Money(d.Treasury, whenPositive: "gold", zeroGold: true);
                economyText.text = bal;

                populationText.text = ChipTooltips.ColorGold(
                    ChipTooltips.FormatBig(d.Population)
                );
                
                // Manpower: "Soldiers / Army Cap"
                manpowerText.text =
                    $"{ChipTooltips.FormatBig(d.ManpowerCurrent)}/" +
                    $"{ChipTooltips.FormatBig(d.ManpowerMax)}";
                warExhaustionText.text = ChipTooltips.ColorGold($"{d.WarExhaustion:0.#}");
                stabilityText.text = ChipTooltips.ColorGold($"{d.Stability:0.#}%");
            }
        }
    }
}