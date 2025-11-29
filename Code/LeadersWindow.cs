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
        
        // Placeholders for avatar circle sprites. Replace these paths if you find the exact game assets.
        // If not found, it will fall back to the windowInnerSprite (square frame).
        private static Sprite circleBgSprite; // Solid circle for background
        private static Sprite circleFrameSprite; // Hollow circle for the colored ring

        private static Transform recruitmentContent;
        private static Transform activeContent;
        private static Text activeHeader;
        
        private static Kingdom lastKingdom;
        private static List<LeaderState> recruitmentPool = new List<LeaderState>();

        private static readonly string[] RandomTitles = { 
            "High Marshall", "Royal Advisor", "Grand Treasurer", "Spymaster", 
            "Chief Justice", "Head of Research", "Fleet Admiral", "High Priest", 
            "Minister of Defense", "Governor", "Diplomat", "Reformer",
            "Corrupt Official", "Tyrant", "Usurper", "Fanatic", "Visionary"
        };

        public static void Initialize(Transform parent)
        {
            if (root != null) return;
            Debug.Log("[RulerBox] LeadersWindow: Initializing UI...");

            try 
            {
                recruitmentContent = null;
                activeContent = null;
                activeHeader = null;

                // Load resources
                windowInnerSprite = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.windowInnerSliced.png");
                // Try to load standard circle sprites from game resources. 
                // NOTE: You might need to adjust these paths to match actual game assets if these don't exist.
                circleBgSprite = Resources.Load<Sprite>("ui/elements/circle_bg"); 
                circleFrameSprite = Resources.Load<Sprite>("ui/elements/circle_frame");

                root = new GameObject("LeadersWindow");
                root.transform.SetParent(parent, false);
                var rt = root.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

                var h = root.AddComponent<HorizontalLayoutGroup>();
                h.spacing = 12; // Increased spacing between panels
                h.padding = new RectOffset(10, 10, 10, 10);
                h.childControlWidth = true;
                h.childControlHeight = true;
                h.childForceExpandWidth = true;
                h.childForceExpandHeight = true;

                CreateRecruitPanel(root.transform);
                CreateActivePanel(root.transform);

                root.SetActive(false);
                Debug.Log("[RulerBox] LeadersWindow: Initialization Complete.");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[RulerBox] LeadersWindow Init CRASHED: " + e.ToString());
                if (root != null) Object.Destroy(root);
                root = null;
            }
        }

        private static void TrySelfInitialize()
        {
            if (root != null) return;
            try
            {
                GameObject hub = GameObject.Find("RulerBox_MainHub");
                if (hub != null)
                {
                    Transform container = hub.transform.Find("ContentContainer");
                    if (container != null) Initialize(container);
                }
            }
            catch { }
        }

        public static void SetVisible(bool visible)
        {
            if (root == null) TrySelfInitialize();
            if (root != null)
            {
                root.SetActive(visible);
                if (visible) Refresh();
            }
        }

        public static bool IsVisible() => root != null && root.activeSelf;

        public static void Refresh()
        {
            if (root == null || recruitmentContent == null || activeContent == null) return;
            if (!root.activeSelf) return;

            try
            {
                var k = Main.selectedKingdom;
                if (k == null) return;

                var d = KingdomMetricsSystem.Get(k);
                if (d == null) return;

                if (lastKingdom != k || recruitmentPool.Count == 0)
                {
                    GenerateRecruitmentPool(k);
                    lastKingdom = k;
                }

                int activeCount = d.ActiveLeaders != null ? d.ActiveLeaders.Count : 0;
                if (activeHeader != null) activeHeader.text = $"{activeCount}/3 Leaders";

                // RECRUITMENT LIST
                if (recruitmentContent != null)
                {
                    foreach (Transform t in recruitmentContent) Object.Destroy(t.gameObject);
                    foreach (var leader in recruitmentPool)
                    {
                        if (leader == null) continue;
                        if (d.ActiveLeaders != null && d.ActiveLeaders.Any(l => l.Id == leader.Id)) continue;
                        CreateLeaderItem(recruitmentContent, leader, false);
                    }
                }

                // ACTIVE LIST
                if (activeContent != null)
                {
                    foreach (Transform t in activeContent) Object.Destroy(t.gameObject);
                    if (d.ActiveLeaders != null)
                    {
                        foreach (var leader in d.ActiveLeaders)
                        {
                            if (leader != null) CreateLeaderItem(activeContent, leader, true);
                        }
                    }
                }

                Canvas.ForceUpdateCanvases();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[RulerBox] LeadersWindow Refresh Error: " + e.Message);
            }
        }

        private static void CreateLeaderItem(Transform parent, LeaderState leader, bool isActive)
        {
            if (parent == null || leader == null) return;

            var btnObj = new GameObject("LeaderItem");
            btnObj.transform.SetParent(parent, false);

            var le = btnObj.AddComponent<LayoutElement>();
            // Increased height for larger avatar and better spacing
            le.minHeight = 64f; 
            le.preferredHeight = 64f;
            le.flexibleWidth = 1f;

            var bg = btnObj.AddComponent<Image>();
            bg.sprite = windowInnerSprite;
            bg.type = Image.Type.Sliced;
            bg.color = isActive ? new Color(0.2f, 0.35f, 0.2f, 0.95f) : new Color(0.25f, 0.28f, 0.32f, 0.95f);

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(() => OnLeaderClicked(leader, isActive));

            var h = btnObj.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 12; // More space between avatar and text
            h.padding = new RectOffset(8, 8, 6, 6);
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false; 
            h.childAlignment = TextAnchor.MiddleLeft;

            // --- NEW LAYERED AVATAR SYSTEM ---
            var avatarRoot = new GameObject("AvatarRoot");
            avatarRoot.transform.SetParent(btnObj.transform, false);
            var avLe = avatarRoot.AddComponent<LayoutElement>();
            avLe.minWidth = 52f; avLe.preferredWidth = 52f;
            avLe.minHeight = 52f; avLe.preferredHeight = 52f;

            // Layer 1: Dark Inner Background Circle
            var bgCircleObj = new GameObject("BgCircle");
            bgCircleObj.transform.SetParent(avatarRoot.transform, false);
            Stretch(bgCircleObj.AddComponent<RectTransform>());
            var bgCircleImg = bgCircleObj.AddComponent<Image>();
            // Use loaded circle sprite, fallback to sliced square if missing
            bgCircleImg.sprite = circleBgSprite != null ? circleBgSprite : windowInnerSprite;
            bgCircleImg.type = circleBgSprite != null ? Image.Type.Simple : Image.Type.Sliced;
            bgCircleImg.color = new Color(0.15f, 0.15f, 0.15f, 1f); // Dark gray

            // Layer 2: Unit Sprite centered inside
            if (leader.UnitLink != null)
            {
                var unitSpriteObj = new GameObject("UnitSprite");
                unitSpriteObj.transform.SetParent(avatarRoot.transform, false);
                var unitRT = Stretch(unitSpriteObj.AddComponent<RectTransform>());
                // Add padding so unit is inside the frame borders
                unitRT.offsetMin = new Vector2(4, 4); unitRT.offsetMax = new Vector2(-4, -4); 
                var uImg = unitSpriteObj.AddComponent<Image>();
                uImg.preserveAspect = true;
                try {
                    uImg.sprite = leader.UnitLink.getSpriteToRender();
                    uImg.color = Color.white; // Base sprite color
                } catch { uImg.color = Color.clear; }
            }

            // Layer 3: Colored Frame Ring on top
            var frameObj = new GameObject("FrameRing");
            frameObj.transform.SetParent(avatarRoot.transform, false);
            Stretch(frameObj.AddComponent<RectTransform>());
            var frameImg = frameObj.AddComponent<Image>();
            // Use loaded frame sprite, fallback to sliced square
            frameImg.sprite = circleFrameSprite != null ? circleFrameSprite : windowInnerSprite;
            frameImg.type = circleFrameSprite != null ? Image.Type.Simple : Image.Type.Sliced;
            
            // Apply Kingdom Color to the frame
            if (leader.UnitLink != null && leader.UnitLink.kingdom != null) {
                frameImg.color = leader.UnitLink.kingdom.getColor().getColorMain(); 
            } else {
                frameImg.color = Color.gray;
            }
            // ---------------------------------

            // --- TEXT INFO ---
            var textStack = new GameObject("InfoStack");
            textStack.transform.SetParent(btnObj.transform, false);
            
            var textVL = textStack.AddComponent<VerticalLayoutGroup>();
            textVL.childAlignment = TextAnchor.MiddleLeft;
            textVL.spacing = 2; // Spacing between lines
            textVL.childControlHeight = true; textVL.childControlWidth = true;
            textVL.childForceExpandHeight = false; textVL.childForceExpandWidth = true;

            var textStackLE = textStack.AddComponent<LayoutElement>();
            textStackLE.flexibleWidth = 1f;

            CreateText(textStack.transform, leader.Name, 11, FontStyle.Bold, Color.white);
            CreateText(textStack.transform, leader.Type, 10, FontStyle.Italic, new Color(1f, 0.85f, 0.4f));
            
            string summary = FormatSummary(leader);
            CreateText(textStack.transform, summary, 9, FontStyle.Normal, new Color(0.9f, 0.9f, 0.9f));

            ChipTooltips.AttachSimpleTooltip(btnObj, () => GetLeaderTooltip(leader));
        }

        // Helper to stretch RectTransform to fill parent
        private static RectTransform Stretch(RectTransform rt) {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return rt;
        }

        private static void CreateRecruitPanel(Transform parent)
        {
            // Increased flexibleWidth slightly
            var container = CreatePanelBase(parent, "RecruitPanel", 1.1f); 
            
            var header = CreateText(container.transform, "Recruit Leaders", 12, FontStyle.Bold, Color.white);
            header.alignment = TextAnchor.MiddleCenter;
            
            var le = header.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 32f; le.minHeight = 32f;

            recruitmentContent = CreateScrollList(container.transform, "RecruitScroll");
        }

        private static void CreateActivePanel(Transform parent)
        {
            // Increased flexibleWidth slightly
            var container = CreatePanelBase(parent, "ActivePanel", 0.9f); 
            
            activeHeader = CreateText(container.transform, "0/3 Leaders", 12, FontStyle.Bold, new Color(1f, 0.85f, 0.4f)); 
            activeHeader.alignment = TextAnchor.MiddleCenter;
            
            var le = activeHeader.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 32f; le.minHeight = 32f;

            activeContent = CreateScrollList(container.transform, "ActiveScroll");
        }

        private static GameObject CreatePanelBase(Transform parent, string name, float flexibleWidth)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var le = panel.AddComponent<LayoutElement>();
            le.flexibleWidth = flexibleWidth; le.flexibleHeight = 1f;
            
            var bg = panel.AddComponent<Image>();
            if (windowInnerSprite != null) { bg.sprite = windowInnerSprite; bg.type = Image.Type.Sliced; }
            bg.color = new Color(0, 0, 0, 0.5f); // Slightly darker panel
            
            var v = panel.AddComponent<VerticalLayoutGroup>();
            // Increased padding so items don't touch edges
            v.spacing = 6; v.padding = new RectOffset(8, 8, 8, 8);
            v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandHeight = false;
            return panel;
        }

        private static Transform CreateScrollList(Transform parent, string name)
        {
            var scrollObj = new GameObject(name);
            scrollObj.transform.SetParent(parent, false);
            var le = scrollObj.AddComponent<LayoutElement>();
            le.flexibleHeight = 1f;
            
            var bg = scrollObj.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.3f); 
            
            var scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.vertical = true; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 25f;
            
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            var vpRT = viewport.AddComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
            // Adjusted viewport padding
            vpRT.offsetMin = new Vector2(2, 2); vpRT.offsetMax = new Vector2(-2, -2);
            viewport.AddComponent<RectMask2D>();

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var cRT = content.AddComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 1); cRT.anchorMax = new Vector2(1, 1);
            cRT.pivot = new Vector2(0.5f, 1);
            
            var v = content.AddComponent<VerticalLayoutGroup>();
            // Spacing between items in list
            v.spacing = 6; v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandHeight = false;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            scroll.viewport = vpRT; scroll.content = cRT;
            return content.transform;
        }

        private static void OnLeaderClicked(LeaderState leader, bool isActive)
        {
            var k = Main.selectedKingdom;
            if (k == null) return;
            var d = KingdomMetricsSystem.Get(k);

            if (isActive)
            {
                d.ActiveLeaders.Remove(leader);
                KingdomMetricsSystem.RecalculateForKingdom(k, d);
                Refresh();
                WorldTip.showNow($"Dismissed {leader.Name}", false, "top", 1.5f, "#FF5A5A");
            }
            else
            {
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
            List<Actor> candidates = new List<Actor>();
            if (k.units != null)
            {
                foreach(var u in k.units)
                {
                    if (u != null && u.isAlive() && u.isAdult() && !u.isKing()) candidates.Add(u);
                }
                candidates.Shuffle();
            }
            int count = 0;
            foreach (var unit in candidates)
            {
                if (count >= 5) break;
                recruitmentPool.Add(CreateRandomLeader(unit));
                count++;
            }
            while (count < 5)
            {
                recruitmentPool.Add(CreateRandomLeader(null));
                count++;
            }
        }

        // --- UPDATED: GENERATE MIXED STATS (Guarantees Good + Bad usually) ---
        private static LeaderState CreateRandomLeader(Actor unit)
        {
            string title = RandomTitles[UnityEngine.Random.Range(0, RandomTitles.Length)];
            
            float stab = 0, pp = 0, atk = 0, res = 0, tax = 0, corr = 0;
            
            // 10% chance for a purely "good" leader
            bool mostlyGood = UnityEngine.Random.value < 0.10f;
            
            int traitCount = UnityEngine.Random.Range(2, 5); // 2 to 4 traits
            int badTraitsApplied = 0;
            int goodTraitsApplied = 0;

            for(int i=0; i < traitCount; i++)
            {
                bool isBad = false;
                if (!mostlyGood)
                {
                    // Logic to ensure mix: 
                    // If it's the last trait and we haven't had a bad one yet, force bad.
                    // If it's the last trait and we haven't had a good one yet, force good.
                    if (i == traitCount - 1) {
                        if (badTraitsApplied == 0) isBad = true;
                        else if (goodTraitsApplied == 0) isBad = false;
                        else isBad = UnityEngine.Random.value > 0.5f;
                    } else {
                         // Normal roll: 40% chance of bad trait
                         isBad = UnityEngine.Random.value < 0.4f;
                    }
                }

                if (isBad) badTraitsApplied++; else goodTraitsApplied++;
                ApplyStat(ref stab, ref pp, ref atk, ref res, ref tax, ref corr, isBad);
            }

            string name = (unit != null) ? unit.getName() : "Generated Leader";
            
            return new LeaderState
            {
                Id = System.Guid.NewGuid().ToString(), Name = name, Type = title, Level = 1,
                UnitLink = unit, StabilityBonus = stab, PPGainBonus = pp/100f, AttackBonus = atk/100f,
                ResearchBonus = res/100f, TaxBonus = tax/100f, CorruptionReduction = corr
            };
        }

        private static void ApplyStat(ref float stab, ref float pp, ref float atk, ref float res, ref float tax, ref float corr, bool isMalus)
        {
            float sign = isMalus ? -1f : 1f; // Negative multiplier for Malus
            int type = UnityEngine.Random.Range(0, 6);

            // Adjusted ranges for slightly stronger traits given they are mixed
            switch(type)
            {
                case 0: stab += UnityEngine.Random.Range(4f, 10f) * sign; break; 
                case 1: pp += UnityEngine.Random.Range(15f, 30f) * sign; break;   
                case 2: atk += UnityEngine.Random.Range(15f, 35f) * sign; break; 
                case 3: res += UnityEngine.Random.Range(15f, 30f) * sign; break;  
                case 4: tax += UnityEngine.Random.Range(15f, 25f) * sign; break;  
                case 5: 
                    // Corruption Logic: Positive 'corr' reduces corruption (Good).
                    // If isMalus (Bad), use negative sign to increase corruption.
                    corr += UnityEngine.Random.Range(0.08f, 0.2f) * (isMalus ? -1f : 1f); 
                    break; 
            }
        }

        private static string FormatSummary(LeaderState l)
        {
            string s = "";
            string Col(float val, string txt) => val >= 0 ? $"<color=#7CFC00>+{txt}</color>" : $"<color=#FF5A5A>{txt}</color>";
            string ColCorr(float val, string txt) => val >= 0 ? $"<color=#7CFC00>-{txt}</color>" : $"<color=#FF5A5A>+{txt}</color>";

            // Only show significant values in summary
            if (Mathf.Abs(l.StabilityBonus) > 1f) s += Col(l.StabilityBonus, $"Stab {l.StabilityBonus:0.#}") + " ";
            if (Mathf.Abs(l.PPGainBonus) > 0.05f) s += Col(l.PPGainBonus, $"PP {l.PPGainBonus*100:0}%") + " ";
            if (Mathf.Abs(l.AttackBonus) > 0.05f) s += Col(l.AttackBonus, $"Atk {l.AttackBonus*100:0}%") + " ";
            if (Mathf.Abs(l.CorruptionReduction) > 0.05f) s += ColCorr(l.CorruptionReduction, $"Corr {Mathf.Abs(l.CorruptionReduction)*100:0}%") + " ";
            
            if (s == "") s = "Check tooltip for details...";
            return s;
        }

        private static string GetLeaderTooltip(LeaderState l)
        {
            string s = $"<b><color=#FFD700>{l.Type}</color></b>\n";
            if (l.UnitLink != null)
            {
                string status = l.UnitLink.isAlive() ? "<color=#7CFC00>Alive</color>" : "<color=#FF5A5A>Deceased</color>";
                s += $"Unit: {l.UnitLink.getName()} ({status})\n";
            }
            else s += "(Abstract Leader)\n";
            s += "\n<b>Effects:</b>\n";

            string Val(float v, string suffix="") => (v > 0 ? "+" : "") + (v*100).ToString("0") + suffix;
            string Col(float v) => v >= 0 ? "<color=#7CFC00>" : "<color=#FF5A5A>";
            
            if (l.StabilityBonus != 0) s += $"Stability: {Col(l.StabilityBonus)}{l.StabilityBonus:0.#}</color>\n";
            if (l.PPGainBonus != 0) s += $"Pol. Power: {Col(l.PPGainBonus)}{Val(l.PPGainBonus, "%")}</color>\n";
            if (l.AttackBonus != 0) s += $"Army Attack: {Col(l.AttackBonus)}{Val(l.AttackBonus, "%")}</color>\n";
            if (l.ResearchBonus != 0) s += $"Research: {Col(l.ResearchBonus)}{Val(l.ResearchBonus, "%")}</color>\n";
            if (l.TaxBonus != 0) s += $"Tax Income: {Col(l.TaxBonus)}{Val(l.TaxBonus, "%")}</color>\n";
            
            if (l.CorruptionReduction != 0) 
            {
                string cCol = l.CorruptionReduction > 0 ? "<color=#7CFC00>" : "<color=#FF5A5A>";
                string sign = l.CorruptionReduction > 0 ? "-" : "+";
                s += $"Corruption: {cCol}{sign}{Mathf.Abs(l.CorruptionReduction)*100:0.##}%</color>\n";
            }

            return s;
        }

        private static Text CreateText(Transform parent, string txt, int size, FontStyle style, Color col)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.text = txt; t.fontSize = size; t.fontStyle = style; t.color = col;
            t.alignment = TextAnchor.MiddleLeft; t.horizontalOverflow = HorizontalWrapMode.Wrap;
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0,0,0,0.5f);
            shadow.effectDistance = new Vector2(1, -1);
            return t;
        }
    }
}