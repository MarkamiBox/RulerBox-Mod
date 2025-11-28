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
            "Corrupt Official", "Tyrant", "Usurper", "Fanatic" // Added some "flavor" titles
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
                h.spacing = 10;
                h.padding = new RectOffset(8, 8, 8, 8);
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
                    List<GameObject> toDestroy = new List<GameObject>();
                    foreach (Transform t in recruitmentContent) toDestroy.Add(t.gameObject);
                    foreach (var go in toDestroy) Object.Destroy(go);

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
                    List<GameObject> toDestroy = new List<GameObject>();
                    foreach (Transform t in activeContent) toDestroy.Add(t.gameObject);
                    foreach (var go in toDestroy) Object.Destroy(go);

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
            le.minHeight = 52f;
            le.preferredHeight = 52f;
            le.flexibleWidth = 1f;

            var bg = btnObj.AddComponent<Image>();
            bg.sprite = windowInnerSprite;
            bg.type = Image.Type.Sliced;
            bg.color = isActive ? new Color(0.2f, 0.35f, 0.2f, 0.95f) : new Color(0.25f, 0.28f, 0.32f, 0.95f);

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(() => OnLeaderClicked(leader, isActive));

            var h = btnObj.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 10;
            h.padding = new RectOffset(8, 8, 6, 6);
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false; 
            h.childAlignment = TextAnchor.MiddleLeft;

            // --- AVATAR RENDERING ---
            var avatarBgObj = new GameObject("AvatarBackground");
            avatarBgObj.transform.SetParent(btnObj.transform, false);
            var avatarLE = avatarBgObj.AddComponent<LayoutElement>();
            avatarLE.minWidth = 44f; 
            avatarLE.preferredWidth = 44f;
            avatarLE.minHeight = 44f; 
            avatarLE.preferredHeight = 44f;

            var avatarBgImg = avatarBgObj.AddComponent<Image>();
            avatarBgImg.sprite = Main.selectedKingdom?.getElementIcon();
            avatarBgImg.color = new Color(0.3f, 0.3f, 0.3f, 1f); // Dark background

            if (leader.UnitLink != null)
            {
                var unitImgObj = new GameObject("UnitSprite");
                unitImgObj.transform.SetParent(avatarBgObj.transform, false);
                
                var unitRT = unitImgObj.AddComponent<RectTransform>();
                unitRT.anchorMin = Vector2.zero; unitRT.anchorMax = Vector2.one;
                unitRT.sizeDelta = Vector2.zero;

                var unitImg = unitImgObj.AddComponent<Image>();
                unitImg.preserveAspect = true;

                try
                {
                    unitImg.sprite = leader.UnitLink.getSpriteToRender();
                    
                    // --- FIX: Use kingdomColor object instead of incorrect ColorAsset method ---
                    if (leader.UnitLink.kingdom != null && leader.UnitLink.kingdom.kingdomColor != null)
                    {
                        unitImg.color = leader.UnitLink.kingdom.kingdomColor.getColorMain(); 
                    }
                    else
                    {
                        unitImg.color = Color.white;
                    }
                }
                catch 
                {
                   unitImg.color = Color.clear; 
                }
            }

            // --- TEXT INFO ---
            var textStack = new GameObject("InfoStack");
            textStack.transform.SetParent(btnObj.transform, false);
            
            var textVL = textStack.AddComponent<VerticalLayoutGroup>();
            textVL.childAlignment = TextAnchor.MiddleLeft;
            textVL.spacing = 2;
            textVL.childControlHeight = true; textVL.childControlWidth = true;
            textVL.childForceExpandHeight = false; textVL.childForceExpandWidth = true;

            var textStackLE = textStack.AddComponent<LayoutElement>();
            textStackLE.flexibleWidth = 1f;

            CreateText(textStack.transform, leader.Name, 7, FontStyle.Bold, Color.white);
            CreateText(textStack.transform, leader.Type, 8, FontStyle.Normal, new Color(1f, 0.85f, 0.4f));
            
            // Format Summary with Colors for Bonus/Malus
            string summary = FormatSummary(leader);
            CreateText(textStack.transform, summary, 6, FontStyle.Normal, new Color(0.9f, 0.9f, 0.9f));

            ChipTooltips.AttachSimpleTooltip(btnObj, () => GetLeaderTooltip(leader));
        }

        private static void CreateRecruitPanel(Transform parent)
        {
            var container = CreatePanelBase(parent, "RecruitPanel", 1f);
            
            var header = CreateText(container.transform, "Recruit Leaders", 8, FontStyle.Bold, Color.white);
            header.alignment = TextAnchor.MiddleCenter;
            
            var le = header.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 34f; le.minHeight = 34f;

            recruitmentContent = CreateScrollList(container.transform, "RecruitScroll");
        }

        private static void CreateActivePanel(Transform parent)
        {
            var container = CreatePanelBase(parent, "ActivePanel", 0.8f);
            
            activeHeader = CreateText(container.transform, "0/3 Leaders", 8, FontStyle.Bold, new Color(1f, 0.85f, 0.4f)); 
            activeHeader.alignment = TextAnchor.MiddleCenter;
            
            var le = activeHeader.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 34f; le.minHeight = 34f;

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
            bg.color = new Color(0, 0, 0, 0.4f); 
            
            var v = panel.AddComponent<VerticalLayoutGroup>();
            v.spacing = 6; v.padding = new RectOffset(6, 6, 6, 6);
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
            bg.color = new Color(0, 0, 0, 0.25f); 
            
            var scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.vertical = true; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 20f;
            
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            var vpRT = viewport.AddComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = new Vector2(4, 4); vpRT.offsetMax = new Vector2(-4, -4);
            viewport.AddComponent<RectMask2D>();

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var cRT = content.AddComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 1); cRT.anchorMax = new Vector2(1, 1);
            cRT.pivot = new Vector2(0.5f, 1);
            
            var v = content.AddComponent<VerticalLayoutGroup>();
            v.spacing = 5; v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandHeight = false;
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

        // --- NEW: GENERATE MIXED STATS (BONUS + MALUS) ---
        private static LeaderState CreateRandomLeader(Actor unit)
        {
            string title = RandomTitles[UnityEngine.Random.Range(0, RandomTitles.Length)];
            
            float stab = 0, pp = 0, atk = 0, res = 0, tax = 0, corr = 0;
            int perks = UnityEngine.Random.Range(1, 3); // 1 to 2 stats

            for(int i=0; i<perks; i++)
            {
                int type = UnityEngine.Random.Range(0, 6);
                // 30% Chance for a trait to be negative (Malus)
                bool isMalus = UnityEngine.Random.value < 0.3f; 
                float sign = isMalus ? -1f : 1f;

                switch(type)
                {
                    case 0: stab += UnityEngine.Random.Range(2f, 6f) * sign; break; 
                    case 1: pp += UnityEngine.Random.Range(5f, 15f) * sign; break;   
                    case 2: atk += UnityEngine.Random.Range(5f, 15f) * sign; break; 
                    case 3: res += UnityEngine.Random.Range(5f, 15f) * sign; break;  
                    case 4: tax += UnityEngine.Random.Range(5f, 15f) * sign; break;  
                    case 5: 
                        // Corruption logic is reversed: Negative value = Good (Reduction), Positive = Bad (Increase)
                        // If 'isMalus' (bad trait), we ADD corruption (+). If good, we SUBTRACT (-).
                        corr += UnityEngine.Random.Range(0.05f, 0.15f) * (isMalus ? 1f : -1f); 
                        break; 
                }
            }

            string name = (unit != null) ? unit.getName() : "Generated Leader";
            
            return new LeaderState
            {
                Id = System.Guid.NewGuid().ToString(), Name = name, Type = title, Level = 1,
                UnitLink = unit, StabilityBonus = stab, PPGainBonus = pp/100f, AttackBonus = atk/100f,
                ResearchBonus = res/100f, TaxBonus = tax/100f, CorruptionReduction = -corr // Store as "Corruption Impact" logic
            };
        }

        private static string FormatSummary(LeaderState l)
        {
            string s = "";
            // Helper to format green/red
            string Col(float val, string txt) => val > 0 ? $"<color=#7CFC00>+{txt}</color>" : $"<color=#FF5A5A>{txt}</color>";
            // Reversed logic for corruption
            string ColRev(float val, string txt) => val < 0 ? $"<color=#7CFC00>{txt}</color>" : $"<color=#FF5A5A>+{txt}</color>";

            if (l.StabilityBonus != 0) s += Col(l.StabilityBonus, $"Stab {l.StabilityBonus:0.#}") + " ";
            if (l.PPGainBonus != 0) s += Col(l.PPGainBonus, $"PP {l.PPGainBonus*100:0}%") + " ";
            if (l.AttackBonus != 0) s += Col(l.AttackBonus, $"Atk {l.AttackBonus*100:0}%") + " ";
            
            if (s == "") s = "Check bonuses...";
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
            string Col(float v) => v > 0 ? "<color=#7CFC00>" : "<color=#FF5A5A>";
            
            // Standard Stats
            if (l.StabilityBonus != 0) s += $"Stability: {Col(l.StabilityBonus)}{l.StabilityBonus:0.#}</color>\n";
            if (l.PPGainBonus != 0) s += $"Political Power: {Col(l.PPGainBonus)}{Val(l.PPGainBonus, "%")}</color>\n";
            if (l.AttackBonus != 0) s += $"Army Attack: {Col(l.AttackBonus)}{Val(l.AttackBonus, "%")}</color>\n";
            if (l.ResearchBonus != 0) s += $"Research: {Col(l.ResearchBonus)}{Val(l.ResearchBonus, "%")}</color>\n";
            if (l.TaxBonus != 0) s += $"Tax Income: {Col(l.TaxBonus)}{Val(l.TaxBonus, "%")}</color>\n";
            
            // Corruption (Recall: l.CorruptionReduction stored the raw impact in previous methods, 
            // but in CreateRandomLeader I stored it as inverted. Let's assume stored value: Negative = Reduction (Good), Positive = Increase (Bad))
            // Actually, KingdomMetrics uses "CorruptionReduction", so Positive Value = Good.
            // My CreateRandom generates 'corr'. If Malus -> Positive Corr. 
            // I assigned CorruptionReduction = -corr.
            // So: If Malus (High Corr), 'corr' is +, so CorruptionReduction is -.
            // If Bonus (Low Corr), 'corr' is -, so CorruptionReduction is +.
            
            if (l.CorruptionReduction != 0) 
            {
                // Reduction > 0 means Corruption Goes Down (Good)
                string cColor = l.CorruptionReduction > 0 ? "<color=#7CFC00>" : "<color=#FF5A5A>";
                string sign = l.CorruptionReduction > 0 ? "-" : "+"; // Display as "-5% Corruption" (Good)
                s += $"Corruption: {cColor}{sign}{Mathf.Abs(l.CorruptionReduction)*100:0.##}%</color>\n";
            }

            return s;
        }

        private static Text CreateText(Transform parent, string txt, int size, FontStyle style, Color col)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.text = txt; 
            t.fontSize = size; 
            t.fontStyle = style; 
            t.color = col;
            t.alignment = TextAnchor.MiddleLeft; t.horizontalOverflow = HorizontalWrapMode.Wrap;
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0,0,0,0.5f);
            shadow.effectDistance = new Vector2(1, -1);
            return t;
        }
    }
}