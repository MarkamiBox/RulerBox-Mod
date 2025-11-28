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
        
        // Track the last kingdom to know when to refresh the pool
        private static Kingdom lastKingdom;
        private static List<LeaderState> recruitmentPool = new List<LeaderState>();

        // Random titles for generated leaders
        private static readonly string[] RandomTitles = { 
            "High Marshall", "Royal Advisor", "Grand Treasurer", "Spymaster", 
            "Chief Justice", "Head of Research", "Fleet Admiral", "High Priest", 
            "Minister of Defense", "Governor", "Diplomat", "Reformer" 
        };

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

            // Main Horizontal Layout
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
            cV.childControlWidth = true; // IMPORTANT: Ensures children expand
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

            // Regenerate pool if kingdom changed or pool is empty
            if (lastKingdom != k || recruitmentPool.Count == 0)
            {
                GenerateRecruitmentPool(k);
                lastKingdom = k;
            }

            if (recruitmentContent == null || activeContent == null) return;

            // Update Header
            int count = d.ActiveLeaders != null ? d.ActiveLeaders.Count : 0;
            if (activeHeader != null) activeHeader.text = $"{count}/3 Leaders";

            // 1. Rebuild Recruitment List
            foreach (Transform t in recruitmentContent) Object.Destroy(t.gameObject);
            foreach (var leader in recruitmentPool)
            {
                // Don't show if already hired
                if (d.ActiveLeaders != null && d.ActiveLeaders.Any(l => l.Id == leader.Id)) continue;
                CreateLeaderButton(recruitmentContent, leader, false);
            }

            // 2. Rebuild Active List (Right side)
            foreach (Transform t in activeContent) Object.Destroy(t.gameObject);
            if (d.ActiveLeaders != null)
            {
                // Clean up dead leaders logic is handled in KingdomMetricsSystem, 
                // but this visual refresh ensures they disappear from the list.
                foreach (var leader in d.ActiveLeaders)
                {
                    CreateLeaderButton(activeContent, leader, true);
                }
            }
        }

        // ====================================================================================
        // BUTTON CREATION (FIXED LAYOUT)
        // ====================================================================================
        private static void CreateLeaderButton(Transform parent, LeaderState leader, bool isActive)
        {
            if (parent == null || leader == null) return;

            var btnObj = new GameObject("LeaderBtn");
            btnObj.transform.SetParent(parent, false);

            var img = btnObj.AddComponent<Image>();
            img.sprite = windowInnerSprite;
            img.type = Image.Type.Sliced;
            // Greenish if active, dark grey if recruit
            img.color = isActive ? new Color(0.2f, 0.45f, 0.2f, 0.9f) : new Color(0.25f, 0.25f, 0.3f, 0.9f);

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => OnLeaderClicked(leader, isActive));

            var le = btnObj.AddComponent<LayoutElement>();
            le.preferredHeight = 44f;
            le.minHeight = 44f;
            le.flexibleWidth = 1f; // Important: stretch to width

            var h = btnObj.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 8;
            h.padding = new RectOffset(6, 6, 4, 4);
            // FIX: This enables the children (icon + text) to have size
            h.childControlWidth = true; 
            h.childControlHeight = true;
            h.childForceExpandWidth = false; 
            h.childForceExpandHeight = false;
            h.childAlignment = TextAnchor.MiddleLeft;

            // --- LEFT: Icon ---
            var iconContainer = new GameObject("IconContainer");
            iconContainer.transform.SetParent(btnObj.transform, false);
            var iconBg = iconContainer.AddComponent<Image>();
            
            if (Main.selectedKingdom != null)
            {
                iconBg.sprite = Main.selectedKingdom.getElementBackground();
                if(Main.selectedKingdom.kingdomColor != null) 
                    iconBg.color = Main.selectedKingdom.kingdomColor.getColorMain32();
            }
            
            var iconContainerLE = iconContainer.AddComponent<LayoutElement>();
            iconContainerLE.preferredWidth = 32f; 
            iconContainerLE.preferredHeight = 32f;
            iconContainerLE.minWidth = 32f;

            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(iconContainer.transform, false);
            var iconImg = iconObj.AddComponent<Image>();
            
            // Try get unit sprite
            Sprite sprite = null;
            if (leader.UnitLink != null) { try { sprite = leader.UnitLink.getSpriteToRender(); } catch { } }
            if (sprite == null && !string.IsNullOrEmpty(leader.IconPath)) sprite = Resources.Load<Sprite>("ui/Icons/" + leader.IconPath);
            if (sprite == null && Main.selectedKingdom != null) sprite = Main.selectedKingdom.getElementIcon(); 
            
            iconImg.sprite = sprite;
            iconImg.preserveAspect = true;
            
            var iconRT = iconObj.GetComponent<RectTransform>();
            iconRT.anchorMin = Vector2.zero; iconRT.anchorMax = Vector2.one;
            iconRT.offsetMin = new Vector2(2,2); iconRT.offsetMax = new Vector2(-2,-2);

            // --- RIGHT: Info Text ---
            var infoObj = new GameObject("Info");
            infoObj.transform.SetParent(btnObj.transform, false);
            var v = infoObj.AddComponent<VerticalLayoutGroup>();
            v.childAlignment = TextAnchor.MiddleLeft;
            v.spacing = 0;
            v.childControlWidth = true; // FIX
            v.childControlHeight = true;
            v.childForceExpandHeight = false;

            var infoLE = infoObj.AddComponent<LayoutElement>();
            infoLE.flexibleWidth = 1f; // Fill remaining space

            CreateText(infoObj.transform, $"{leader.Name}", 9, FontStyle.Bold);
            CreateText(infoObj.transform, $"{leader.Type}", 8, FontStyle.Normal, new Color(1f, 0.85f, 0.4f));

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

        // ====================================================================================
        // RANDOM GENERATION
        // ====================================================================================
        private static void GenerateRecruitmentPool(Kingdom k)
        {
            recruitmentPool.Clear();
            if (k == null || k.units == null) return;

            // 1. Pick up to 5 random adult units
            var candidates = k.units.Where(a => a.isAlive() && a.isAdult() && !a.isKing() && !a.isCityLeader())
                                    .OrderBy(x => UnityEngine.Random.value)
                                    .Take(5)
                                    .ToList();
            
            int needed = 5;

            // 2. Create leaders from real units
            foreach(var unit in candidates)
            {
                recruitmentPool.Add(CreateRandomLeader(unit));
                needed--;
            }

            // 3. Fill rest with "Abstract" leaders if kingdom has few units
            for(int i=0; i<needed; i++)
            {
                recruitmentPool.Add(CreateRandomLeader(null));
            }
        }

        private static LeaderState CreateRandomLeader(Actor unit)
        {
            string title = RandomTitles[UnityEngine.Random.Range(0, RandomTitles.Length)];
            
            // Random Stats
            float stab = 0, pp = 0, atk = 0, res = 0, tax = 0, corr = 0;

            // Give 1-2 random perks
            int perks = UnityEngine.Random.Range(1, 3);
            for(int i=0; i<perks; i++)
            {
                int type = UnityEngine.Random.Range(0, 6);
                switch(type)
                {
                    case 0: stab += UnityEngine.Random.Range(2f, 6f); break; // +Stability
                    case 1: pp += UnityEngine.Random.Range(5f, 15f); break;   // +Political Power
                    case 2: atk += UnityEngine.Random.Range(5f, 15f); break;  // +Army Attack
                    case 3: res += UnityEngine.Random.Range(5f, 15f); break;  // +Research
                    case 4: tax += UnityEngine.Random.Range(5f, 15f); break;  // +Tax
                    case 5: corr += UnityEngine.Random.Range(0.05f, 0.15f); break; // -Corruption
                }
            }

            return CreateLeader(unit, title, stab, pp, atk, res, tax, corr);
        }

        private static LeaderState CreateLeader(Actor unit, string type, float stab, float pp, float atk, float res, float tax, float corr)
        {
            string name = "Unknown";
            string icon = "icon_citizen";

            if (unit != null)
            {
                name = unit.getName();
            }
            else
            {
                // Fallback name generator
                string[] names = { "Cyrus", "Darius", "Alexander", "Sargon", "Hammurabi", "Leonidas", "Pericles", "Solon" };
                name = names[UnityEngine.Random.Range(0, names.Length)];
            }

            return new LeaderState
            {
                Id = System.Guid.NewGuid().ToString(),
                Name = name,
                Type = type,
                Level = 1,
                UnitLink = unit,
                IconPath = icon,
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
            string s = $"<b>{l.Type}</b>\n";
            if (l.UnitLink != null)
            {
                string status = l.UnitLink.isAlive() ? "<color=#7CFC00>Alive</color>" : "<color=#FF5A5A>Deceased</color>";
                s += $"Unit: {l.UnitLink.getName()} ({status})\n";
            }
            s += "\n<b>Effects:</b>\n";
            if (l.StabilityBonus > 0) s += $"Stability: <color=#7CFC00>+{l.StabilityBonus:0.#}</color>\n";
            if (l.PPGainBonus > 0) s += $"Pol. Power: <color=#7CFC00>+{l.PPGainBonus*100:0}%</color>\n";
            if (l.AttackBonus > 0) s += $"Army Attack: <color=#7CFC00>+{l.AttackBonus*100:0}%</color>\n";
            if (l.ResearchBonus > 0) s += $"Research Eff: <color=#7CFC00>+{l.ResearchBonus*100:0}%</color>\n";
            if (l.TaxBonus > 0) s += $"Tax Income: <color=#7CFC00>+{l.TaxBonus*100:0}%</color>\n";
            if (l.CorruptionReduction > 0) s += $"Corruption: <color=#7CFC00>-{l.CorruptionReduction*100:0}%</color>\n";
            
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