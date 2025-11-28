using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace RulerBox
{
    public static class LeadersWindow
    {
        private static GameObject root;
        private static Sprite windowInnerSprite;
        private static Transform recruitmentContent;
        private static Transform activeContent;
        private static Text activeHeader;
        
        // Default random names pool (fallback)
        private static readonly string[] Names = { "Alexander", "Cyrus", "Augustus", "Bismarck", "Churchill", "Caesar", "Napoleon", "Victoria", "Lincoln", "Gandhi" };
        private static List<LeaderState> recruitmentPool = new List<LeaderState>();
        private static bool initializedPool = false;

        public static void Initialize(Transform parent)
        {
            if (root != null) return;
            windowInnerSprite = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.windowInnerSliced.png");

            // Root
            root = new GameObject("LeadersWindow");
            root.transform.SetParent(parent, false);
            var rt = root.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            // Main Horizontal Layout (Split Left/Right)
            var h = root.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 6;
            h.padding = new RectOffset(6, 6, 6, 6);
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = true;

            // --- LEFT COLUMN (Recruitment) ---
            CreateRecruitmentPanel(root.transform);

            // --- RIGHT COLUMN (Active) ---
            CreateActivePanel(root.transform);

            root.SetActive(false);
        }

        private static void CreateRecruitmentPanel(Transform parent)
        {
            var panel = new GameObject("RecruitmentPanel");
            panel.transform.SetParent(parent, false);
            
            var le = panel.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;

            var v = panel.AddComponent<VerticalLayoutGroup>();
            v.spacing = 4;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandHeight = false;

            // Header
            var header = CreateText(panel.transform, "Recruit Leaders", 10, FontStyle.Bold);
            header.alignment = TextAnchor.MiddleCenter;

            // Scroll View
            var scrollObj = new GameObject("Scroll");
            scrollObj.transform.SetParent(panel.transform, false);
            var scrollLe = scrollObj.AddComponent<LayoutElement>();
            scrollLe.flexibleHeight = 1f;

            var bg = scrollObj.AddComponent<Image>();
            bg.sprite = windowInnerSprite;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0, 0, 0, 0.3f);

            var scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.vertical = true;
            scroll.horizontal = false;
            
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            var vpRT = viewport.AddComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = new Vector2(2, 2); vpRT.offsetMax = new Vector2(-2, -2);
            viewport.AddComponent<RectMask2D>();

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            recruitmentContent = content.transform;
            var cRT = content.AddComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 1); cRT.anchorMax = new Vector2(1, 1);
            cRT.pivot = new Vector2(0.5f, 1);

            var cV = content.AddComponent<VerticalLayoutGroup>();
            cV.spacing = 2;
            cV.childControlWidth = true;
            cV.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = vpRT;
            scroll.content = cRT;
        }

        private static void CreateActivePanel(Transform parent)
        {
            var panel = new GameObject("ActivePanel");
            panel.transform.SetParent(parent, false);
            
            var le = panel.AddComponent<LayoutElement>();
            le.flexibleWidth = 0.8f; 

            var bg = panel.AddComponent<Image>();
            bg.sprite = windowInnerSprite;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.2f, 0.2f, 0.25f, 0.8f);

            var v = panel.AddComponent<VerticalLayoutGroup>();
            v.spacing = 4;
            v.padding = new RectOffset(4, 4, 4, 4);
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandHeight = false;

            // Capacity Header
            activeHeader = CreateText(panel.transform, "0/3 Leaders", 10, FontStyle.Bold);
            activeHeader.alignment = TextAnchor.MiddleCenter;
            activeHeader.color = Color.yellow;

            // Active List Container
            var listObj = new GameObject("ActiveList");
            listObj.transform.SetParent(panel.transform, false);
            activeContent = listObj.transform;
            
            var listV = listObj.AddComponent<VerticalLayoutGroup>();
            listV.spacing = 2;
            listV.childControlWidth = true;
            listV.childForceExpandHeight = false;
            
            listObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        public static void SetVisible(bool visible)
        {
            if (root != null)
            {
                root.SetActive(visible);
                if (visible) Refresh();
            }
        }

        public static bool IsVisible() => root != null && root.activeSelf;

        public static void Refresh()
        {
            if (root == null || !root.activeSelf) return;
            var k = Main.selectedKingdom;
            if (k == null) return;

            var d = KingdomMetricsSystem.Get(k);
            if (!initializedPool) GenerateRecruitmentPool(k);

            // Safety Check for NRE
            if (recruitmentContent == null || activeContent == null) return;

            // Update Capacity
            int count = d.ActiveLeaders != null ? d.ActiveLeaders.Count : 0;
            if (activeHeader != null) activeHeader.text = $"{count}/3 Leaders";

            // Rebuild Recruitment List
            foreach (Transform t in recruitmentContent) Object.Destroy(t.gameObject);
            foreach (var leader in recruitmentPool)
            {
                if (d.ActiveLeaders != null && d.ActiveLeaders.Any(l => l.Id == leader.Id)) continue;
                CreateLeaderButton(recruitmentContent, leader, false);
            }

            // Rebuild Active List
            foreach (Transform t in activeContent) Object.Destroy(t.gameObject);
            if (d.ActiveLeaders != null)
            {
                foreach (var leader in d.ActiveLeaders)
                {
                    CreateLeaderButton(activeContent, leader, true);
                }
            }
        }

        private static void CreateLeaderButton(Transform parent, LeaderState leader, bool isActive)
        {
            if (parent == null || leader == null) return;

            var btnObj = new GameObject("LeaderBtn");
            btnObj.transform.SetParent(parent, false);

            var img = btnObj.AddComponent<Image>();
            img.sprite = windowInnerSprite;
            img.type = Image.Type.Sliced;
            // Highlight if active (greenish) else greyish
            img.color = isActive ? new Color(0.2f, 0.4f, 0.2f, 0.8f) : new Color(0.3f, 0.3f, 0.35f, 0.8f);

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => OnLeaderClicked(leader, isActive));

            var le = btnObj.AddComponent<LayoutElement>();
            le.preferredHeight = 44f;
            le.minHeight = 44f;
            le.flexibleWidth = 1f; // Ensure button stretches horizontally

            var h = btnObj.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 8;
            h.padding = new RectOffset(4, 4, 4, 4);
            
            // --- FIX START ---
            // Change these to TRUE so the layout group actually sizes the icon and text areas
            h.childControlWidth = true; 
            h.childControlHeight = true;
            h.childForceExpandWidth = false; // Keep false so the Icon stays fixed width
            h.childForceExpandHeight = false; 
            // --- FIX END ---
            
            h.childAlignment = TextAnchor.MiddleLeft;

            // --- LEFT: Sprite (Unit Icon Style) ---
            var iconContainer = new GameObject("IconContainer");
            iconContainer.transform.SetParent(btnObj.transform, false);
            var iconBg = iconContainer.AddComponent<Image>();
            
            // Fallback Kingdom Style
            if (Main.selectedKingdom != null)
            {
                iconBg.sprite = Main.selectedKingdom.getElementBackground();
                if(Main.selectedKingdom.kingdomColor != null) 
                    iconBg.color = Main.selectedKingdom.kingdomColor.getColorMain32();
            }
            
            var iconContainerLE = iconContainer.AddComponent<LayoutElement>();
            iconContainerLE.preferredWidth = 32f; 
            iconContainerLE.preferredHeight = 32f;
            iconContainerLE.minWidth = 32f; // Ensure it doesn't shrink
            iconContainerLE.minHeight = 32f;

            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(iconContainer.transform, false);
            var iconImg = iconObj.AddComponent<Image>();
            
            // ... (Icon loading logic remains the same) ...
            Sprite sprite = null;
            if (leader.UnitLink != null)
            {
                try { sprite = leader.UnitLink.getSpriteToRender(); } catch { }
            }
            if (sprite == null && !string.IsNullOrEmpty(leader.IconPath))
            {
                sprite = Resources.Load<Sprite>("ui/Icons/" + leader.IconPath);
            }
            if (sprite == null && Main.selectedKingdom != null) 
            {
                sprite = Main.selectedKingdom.getElementIcon(); 
            }
            
            iconImg.sprite = sprite;
            iconImg.preserveAspect = true;
            
            // Stretch icon inside container
            var iconRT = iconObj.GetComponent<RectTransform>();
            iconRT.anchorMin = Vector2.zero; iconRT.anchorMax = Vector2.one;
            iconRT.offsetMin = new Vector2(2,2); iconRT.offsetMax = new Vector2(-2,-2);

            // --- RIGHT: Info ---
            var infoObj = new GameObject("Info");
            infoObj.transform.SetParent(btnObj.transform, false);
            var v = infoObj.AddComponent<VerticalLayoutGroup>();
            v.childAlignment = TextAnchor.MiddleLeft;
            v.spacing = 0;
            // Important: Ensure the vertical group in Info also controls children
            v.childControlWidth = true; 
            v.childControlHeight = true; 
            v.childForceExpandHeight = false;

            var infoLE = infoObj.AddComponent<LayoutElement>();
            // No preferred width fixed, just flexible to fill remaining space
            infoLE.flexibleWidth = 1f; 

            // Text Creation
            CreateText(infoObj.transform, $"{leader.Name}", 9, FontStyle.Bold);
            // Updated color to Gold to match screenshot
            var subTxt = CreateText(infoObj.transform, $"{leader.Type}", 8, FontStyle.Normal, new Color(1f, 0.8f, 0.2f)); 
            
            // Tooltip
            ChipTooltips.AttachSimpleTooltip(btnObj, () => GetLeaderTooltip(leader));
        }

        private static void OnLeaderClicked(LeaderState leader, bool isActive)
        {
            var k = Main.selectedKingdom;
            if (k == null) return;
            var d = KingdomMetricsSystem.Get(k);

            if (isActive)
            {
                // Dismiss
                d.ActiveLeaders.Remove(leader);
                KingdomMetricsSystem.RecalculateForKingdom(k, d);
                Refresh();
                WorldTip.showNow($"Dismissed {leader.Name}", false, "top", 1.5f, "#FF5A5A");
            }
            else
            {
                // Recruit
                if (d.ActiveLeaders.Count >= 3)
                {
                    WorldTip.showNow("Cabinet is full (3/3)", false, "top", 1.5f, "#FF5A5A");
                    return;
                }
                d.ActiveLeaders.Add(leader);
                KingdomMetricsSystem.RecalculateForKingdom(k, d);
                Refresh();
                WorldTip.showNow($"Recruited {leader.Name}!", false, "top", 1.5f, "#9EE07A");
            }
        }

        private static void GenerateRecruitmentPool(Kingdom k)
        {
            recruitmentPool.Clear();
            if (k == null) return;

            // Get Potential Actors
            List<Actor> candidates = new List<Actor>();
            if (k.units != null)
            {
                // Simple reservoir sample to get random units
                foreach(Actor a in k.units)
                {
                    if (a == null || !a.isAlive() || !a.isAdult()) continue;
                    if (candidates.Count < 5) candidates.Add(a);
                    else if (UnityEngine.Random.value < 0.2f) candidates[UnityEngine.Random.Range(0, 5)] = a;
                }
            }

            // Create 5 Leaders using actors if available
            // 1. Head of Gov
            recruitmentPool.Add(CreateLeader(k, candidates, "Head of Government", 5f, 15f, 0f, 0f, 0f, 0f));
            // 2. Chief of Staff
            recruitmentPool.Add(CreateLeader(k, candidates, "Chief of Staff", 0f, 0f, 10f, 0f, 0f, 0f));
            // 3. Head of Research
            recruitmentPool.Add(CreateLeader(k, candidates, "Head of Research", 0f, 0f, 0f, 5f, 0f, 0f));
            // 4. Finance Minister
            recruitmentPool.Add(CreateLeader(k, candidates, "Finance Minister", 0f, 0f, 0f, 0f, 15f, 0f));
            // 5. Chief Judge
            recruitmentPool.Add(CreateLeader(k, candidates, "Chief Judge", 3f, -3f, 0f, 0f, 0f, 0.0025f));
            
            initializedPool = true;
        }

        private static LeaderState CreateLeader(Kingdom k, List<Actor> candidates, string type, float stab, float pp, float atk, float res, float tax, float corr)
        {
            // Pick a unit
            Actor linkedUnit = null;
            string name = Names[UnityEngine.Random.Range(0, Names.Length)];
            
            if (candidates != null && candidates.Count > 0)
            {
                // Pick and remove so we don't reuse same unit for multiple roles (optional)
                int idx = UnityEngine.Random.Range(0, candidates.Count);
                linkedUnit = candidates[idx];
                name = linkedUnit.getName(); // Use actual unit name
            }

            return new LeaderState
            {
                Id = System.Guid.NewGuid().ToString(),
                Name = name,
                Type = type,
                Level = 1,
                UnitLink = linkedUnit, // Bind the actor
                StabilityBonus = stab,
                PPGainBonus = pp / 100f,
                AttackBonus = atk / 100f,
                ResearchBonus = res / 100f,
                TaxBonus = tax / 100f,
                CorruptionReduction = corr
            };
        }

        private static string GetLeaderTooltip(LeaderState l)
        {
            string s = $"<b>{l.Type}</b> (Lvl {l.Level})\n";
            if (l.UnitLink != null)
            {
                string status = l.UnitLink.isAlive() ? "<color=#7CFC00>Alive</color>" : "<color=#FF5A5A>Deceased</color>";
                s += $"Unit: {l.UnitLink.getName()} ({status})\n";
            }
            if (l.StabilityBonus != 0) s += $"Stability: <color=#7CFC00>+{l.StabilityBonus}%</color>\n";
            if (l.PPGainBonus != 0) s += $"Pol. Power: {(l.PPGainBonus>0?"<color=#7CFC00>+":"<color=#FF5A5A>")}{l.PPGainBonus*100:0}%</color>\n";
            if (l.AttackBonus != 0) s += $"Army Attack: <color=#7CFC00>+{l.AttackBonus*100:0}%</color>\n";
            if (l.ResearchBonus != 0) s += $"Research Eff: <color=#7CFC00>+{l.ResearchBonus*100:0}%</color>\n";
            if (l.TaxBonus != 0) s += $"Tax Income: <color=#7CFC00>+{l.TaxBonus*100:0}%</color>\n";
            if (l.CorruptionReduction != 0) s += $"Corruption: <color=#7CFC00>-{l.CorruptionReduction*100:0.25}%</color>\n";
            
            s += "\n<size=9><i>Click to " + (Main.selectedKingdom != null && KingdomMetricsSystem.Get(Main.selectedKingdom).ActiveLeaders.Contains(l) ? "Dismiss" : "Recruit") + "</i></size>";
            return s;
        }

        private static Text CreateText(Transform parent, string content, int size, FontStyle style, Color? col = null)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var txt = go.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = content;
            txt.fontSize = size;
            txt.fontStyle = style;
            txt.color = col ?? Color.white;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.resizeTextForBestFit = false;
            txt.supportRichText = true;
            return txt;
        }
    }
}