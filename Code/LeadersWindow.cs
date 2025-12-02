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

                windowInnerSprite = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.windowInnerSliced.png");

                root = new GameObject("LeadersWindow");
                root.transform.SetParent(parent, false);
                var rt = root.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

                var h = root.AddComponent<HorizontalLayoutGroup>();
                h.spacing = 2;
                h.padding = new RectOffset(1, 1, 1, 1);
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
            le.minHeight = 44f; 
            le.preferredHeight = 44f;
            le.flexibleWidth = 1f;

            var bg = btnObj.AddComponent<Image>();
            bg.sprite = windowInnerSprite;
            bg.type = Image.Type.Sliced;
            bg.color = isActive ? new Color(0.2f, 0.35f, 0.2f, 0.95f) : new Color(0.25f, 0.28f, 0.32f, 0.95f);

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(() => OnLeaderClicked(leader, isActive));

            var h = btnObj.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 2;
            h.padding = new RectOffset(10, 10, 2, 2); 
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false; 
            h.childAlignment = TextAnchor.MiddleLeft;

            // --- TEXT INFO ONLY (No Avatar) ---
            var textStack = new GameObject("InfoStack");
            textStack.transform.SetParent(btnObj.transform, false);
            
            var textVL = textStack.AddComponent<VerticalLayoutGroup>();
            textVL.childAlignment = TextAnchor.MiddleLeft;
            textVL.spacing = -1;
            textVL.childControlHeight = true; textVL.childControlWidth = true;
            textVL.childForceExpandHeight = false; textVL.childForceExpandWidth = true;

            var textStackLE = textStack.AddComponent<LayoutElement>();
            textStackLE.flexibleWidth = 1f;
            textStackLE.minWidth = 50f;

            CreateText(textStack.transform, leader.Name, 8, FontStyle.Bold, Color.white);
            CreateText(textStack.transform, leader.Type, 7, FontStyle.Italic, new Color(1f, 0.85f, 0.4f));
            
            ChipTooltips.AttachSimpleTooltip(btnObj, () => GetLeaderTooltip(leader));
        }

        private static void CreateRecruitPanel(Transform parent)
        {
            var container = CreatePanelBase(parent, "RecruitPanel", 1.0f); 
            
            var header = CreateText(container.transform, "Recruit Leaders", 8, FontStyle.Bold, Color.white);
            header.alignment = TextAnchor.MiddleCenter;
            
            var le = header.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 24f; le.minHeight = 24f;

            recruitmentContent = CreateScrollList(container.transform, "RecruitScroll");
        }

        private static void CreateActivePanel(Transform parent)
        {
            var container = CreatePanelBase(parent, "ActivePanel", 0.9f); 
            
            activeHeader = CreateText(container.transform, "0/3 Leaders", 8, FontStyle.Bold, new Color(1f, 0.85f, 0.4f)); 
            activeHeader.alignment = TextAnchor.MiddleCenter;
            
            var le = activeHeader.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 24f; le.minHeight = 24f;

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
            bg.color = new Color(0, 0, 0, 0.001f); 
            
            var v = panel.AddComponent<VerticalLayoutGroup>();
            v.spacing = 1; v.padding = new RectOffset(1, 1, 1, 1);
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
            scroll.vertical = true; 
            scroll.horizontal = true; 
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 20f;
            
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            var vpRT = viewport.AddComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = new Vector2(2, 2); vpRT.offsetMax = new Vector2(-2, -2);
            viewport.AddComponent<RectMask2D>();

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var cRT = content.AddComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 1); cRT.anchorMax = new Vector2(1, 1);
            cRT.pivot = new Vector2(0.5f, 1);
            
            var v = content.AddComponent<VerticalLayoutGroup>();
            v.spacing = 4; 
            v.childControlWidth = true; 
            v.childControlHeight = true; 
            v.childForceExpandHeight = false;
            
            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            
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
                // Recruitment Cost Logic
                float ppMod = Mathf.Max(0.1f, d.PoliticalPowerGainModifier);
                long cost = (long)(500 / ppMod);
                
                if (d.Treasury < cost)
                {
                     WorldTip.showNow($"Need {cost} Gold to recruit!", true, "top", 1.5f, "#FF5A5A");
                     return;
                }
                d.Treasury -= cost;

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

        private static LeaderState CreateRandomLeader(Actor unit)
        {
            string title = RandomTitles[UnityEngine.Random.Range(0, RandomTitles.Length)];
            
            float stab = 0, pp = 0, atk = 0, res = 0, tax = 0, corr = 0;
            
            // 1. Apply Stats based on Unit Traits (if unit exists)
            // FIX: Use saved_traits instead of traits (obsolete)
            if (unit != null && unit.data != null && unit.data.saved_traits != null)
            {
                var t = unit.data.saved_traits;
                if (t.Contains("greedy")) { tax += 0.15f; corr -= 0.15f; } // Greedy: +Tax, +Corruption (negative reduction)
                if (t.Contains("honest")) { corr += 0.20f; } // Honest: -Corruption
                if (t.Contains("genius")) { res += 0.30f; pp += 0.10f; }
                if (t.Contains("stupid")) { res -= 0.15f; pp -= 0.10f; }
                if (t.Contains("strong")) { atk += 0.10f; }
                if (t.Contains("weak")) { atk -= 0.10f; }
                if (t.Contains("tough")) { stab += 5f; }
                if (t.Contains("paranoid")) { stab -= 5f; pp += 0.20f; } // Paranoid: Unstable but controlling
                if (t.Contains("pacifist")) { atk -= 0.20f; stab += 5f; }
                if (t.Contains("bloodlust")) { atk += 0.20f; stab -= 5f; }
                if (t.Contains("ambitious")) { pp += 0.25f; corr -= 0.05f; }
                if (t.Contains("content")) { pp -= 0.10f; stab += 5f; }
            }

            // 2. Add Random Variance (smaller than before if unit exists)
            int traitCount = UnityEngine.Random.Range(1, 3); 
            bool mostlyGood = UnityEngine.Random.value < 0.5f;

            for(int i=0; i < traitCount; i++)
            {
                bool isBad = UnityEngine.Random.value < 0.3f;
                ApplyStat(ref stab, ref pp, ref atk, ref res, ref tax, ref corr, isMalus: isBad);
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
            float sign = isMalus ? -1f : 1f; 
            int type = UnityEngine.Random.Range(0, 6);

            switch(type)
            {
                case 0: stab += UnityEngine.Random.Range(2f, 5f) * sign; break; 
                case 1: pp += UnityEngine.Random.Range(5f, 15f) * sign; break;   
                case 2: atk += UnityEngine.Random.Range(5f, 15f) * sign; break; 
                case 3: res += UnityEngine.Random.Range(5f, 15f) * sign; break;  
                case 4: tax += UnityEngine.Random.Range(5f, 10f) * sign; break;  
                case 5: corr += UnityEngine.Random.Range(0.05f, 0.10f) * (isMalus ? -1f : 1f); break; 
            }
        }

        private static string GetLeaderTooltip(LeaderState l)
        {
            string s = $"<b><color=#FFD700>{l.Type}</color></b>\n";
            if (l.UnitLink != null) s += $"Unit: {l.UnitLink.getName()}\n";
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
            t.font = Resources.Load<Font>("Fonts/Roboto-Regular") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.text = txt; t.fontSize = size; t.fontStyle = style; t.color = col;
            t.alignment = TextAnchor.MiddleLeft; t.horizontalOverflow = HorizontalWrapMode.Wrap;
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0,0,0,0.5f);
            shadow.effectDistance = new Vector2(1, -1);
            return t;
        }
    }
}