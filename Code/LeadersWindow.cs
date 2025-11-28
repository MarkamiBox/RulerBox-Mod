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
            "Minister of Defense", "Governor", "Diplomat", "Reformer" 
        };

        public static void Initialize(Transform parent)
        {
            if (root != null) return;
            Debug.Log("[RulerBox] LeadersWindow: Initializing UI...");

            try 
            {
                // Reset static references to ensure clean state
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
                // CLEANUP: Destroy broken root so we can try again later
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
                    if (container != null)
                    {
                        Initialize(container);
                    }
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
            // SAFETY CHECK: If UI isn't ready, don't crash the game
            if (root == null || recruitmentContent == null || activeContent == null) return;
            if (!root.activeSelf) return;

            try
            {
                var k = Main.selectedKingdom;
                if (k == null) return;

                var d = KingdomMetricsSystem.Get(k);
                if (d == null) return;

                // Generate Pool Logic
                if (lastKingdom != k || recruitmentPool.Count == 0)
                {
                    GenerateRecruitmentPool(k);
                    lastKingdom = k;
                }

                // Update Header
                int activeCount = d.ActiveLeaders != null ? d.ActiveLeaders.Count : 0;
                if (activeHeader != null) activeHeader.text = $"{activeCount}/3 Leaders";

                // --- RECRUITMENT LIST ---
                if (recruitmentContent != null)
                {
                    // Clear old items manually to be safe
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

                // --- ACTIVE LIST ---
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

                // Force Layout Update
                Canvas.ForceUpdateCanvases();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[RulerBox] LeadersWindow Refresh Error (Recoverable): " + e.Message);
            }
        }

        private static void CreateLeaderItem(Transform parent, LeaderState leader, bool isActive)
        {
            if (parent == null || leader == null) return;

            var btnObj = new GameObject("LeaderItem");
            btnObj.transform.SetParent(parent, false);

            var le = btnObj.AddComponent<LayoutElement>();
            le.minHeight = 50f;
            le.preferredHeight = 50f;
            le.flexibleWidth = 1f;

            var bg = btnObj.AddComponent<Image>();
            bg.sprite = windowInnerSprite;
            bg.type = Image.Type.Sliced;
            bg.color = isActive ? new Color(0.2f, 0.4f, 0.2f, 0.9f) : new Color(0.25f, 0.25f, 0.3f, 0.9f);

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(() => OnLeaderClicked(leader, isActive));

            var h = btnObj.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 8;
            h.padding = new RectOffset(6, 6, 4, 4);
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false; 
            h.childAlignment = TextAnchor.MiddleLeft;

            // Avatar
            var avatarObj = new GameObject("Avatar");
            avatarObj.transform.SetParent(btnObj.transform, false);
            var avatarLE = avatarObj.AddComponent<LayoutElement>();
            avatarLE.minWidth = 40f; avatarLE.preferredWidth = 40f;
            avatarLE.minHeight = 40f; avatarLE.preferredHeight = 40f;

            var avatarImg = avatarObj.AddComponent<Image>();
            avatarImg.preserveAspect = true;
            Sprite s = null;
            try { if (leader.UnitLink != null) s = leader.UnitLink.getSpriteToRender(); } catch { }
            if (s == null) s = Main.selectedKingdom?.getElementIcon();
            avatarImg.sprite = s;

            // Text
            var textStack = new GameObject("InfoStack");
            textStack.transform.SetParent(btnObj.transform, false);
            
            var textVL = textStack.AddComponent<VerticalLayoutGroup>();
            textVL.childAlignment = TextAnchor.MiddleLeft;
            textVL.spacing = 0;
            textVL.childControlHeight = true; textVL.childControlWidth = true;
            textVL.childForceExpandHeight = false; textVL.childForceExpandWidth = true;

            var textStackLE = textStack.AddComponent<LayoutElement>();
            textStackLE.flexibleWidth = 1f;

            CreateText(textStack.transform, leader.Name, 10, FontStyle.Bold, Color.white);
            CreateText(textStack.transform, leader.Type, 9, FontStyle.Italic, new Color(1f, 0.9f, 0.5f));
            
            string summary = "";
            if (leader.StabilityBonus > 0) summary += $"Stab +{leader.StabilityBonus:0.#} ";
            if (leader.PPGainBonus > 0) summary += $"PP +{leader.PPGainBonus*100:0}% ";
            if (summary == "") summary = "View bonuses...";
            CreateText(textStack.transform, summary, 8, FontStyle.Normal, new Color(0.8f, 0.8f, 0.8f));

            ChipTooltips.AttachSimpleTooltip(btnObj, () => GetLeaderTooltip(leader));
        }

        private static void CreateRecruitPanel(Transform parent)
        {
            var container = CreatePanelBase(parent, "RecruitPanel", 1f);
            var header = CreateText(container.transform, "Recruit Leaders", 12, FontStyle.Bold, Color.white);
            header.alignment = TextAnchor.MiddleCenter;
            header.GetComponent<LayoutElement>().preferredHeight = 30f;
            recruitmentContent = CreateScrollList(container.transform, "RecruitScroll");
        }

        private static void CreateActivePanel(Transform parent)
        {
            var container = CreatePanelBase(parent, "ActivePanel", 0.8f);
            activeHeader = CreateText(container.transform, "0/3 Leaders", 12, FontStyle.Bold, Color.yellow);
            activeHeader.alignment = TextAnchor.MiddleCenter;
            activeHeader.GetComponent<LayoutElement>().preferredHeight = 30f;
            activeContent = CreateScrollList(container.transform, "ActiveScroll");
        }

        private static GameObject CreatePanelBase(Transform parent, string name, float flexibleWidth)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var le = panel.AddComponent<LayoutElement>();
            le.flexibleWidth = flexibleWidth; le.flexibleHeight = 1f;
            
            var bg = panel.AddComponent<Image>();
            bg.sprite = windowInnerSprite; bg.type = Image.Type.Sliced;
            bg.color = new Color(0, 0, 0, 0.3f);
            
            var v = panel.AddComponent<VerticalLayoutGroup>();
            v.spacing = 4; v.padding = new RectOffset(4, 4, 4, 4);
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
            bg.color = new Color(0, 0, 0, 0.2f);
            var scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.vertical = true; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            
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
            v.spacing = 4; v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandHeight = false;
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

        private static LeaderState CreateRandomLeader(Actor unit)
        {
            string title = RandomTitles[UnityEngine.Random.Range(0, RandomTitles.Length)];
            float stab = 0, pp = 0, atk = 0, res = 0, tax = 0, corr = 0;
            int perks = UnityEngine.Random.Range(1, 3);
            for(int i=0; i<perks; i++)
            {
                int type = UnityEngine.Random.Range(0, 6);
                switch(type)
                {
                    case 0: stab += UnityEngine.Random.Range(2f, 6f); break; 
                    case 1: pp += UnityEngine.Random.Range(5f, 15f); break;   
                    case 2: atk += UnityEngine.Random.Range(5f, 15f); break; 
                    case 3: res += UnityEngine.Random.Range(5f, 15f); break;  
                    case 4: tax += UnityEngine.Random.Range(5f, 15f); break;  
                    case 5: corr += UnityEngine.Random.Range(0.05f, 0.15f); break; 
                }
            }
            string name = (unit != null) ? unit.getName() : "Generated Leader";
            return new LeaderState
            {
                Id = System.Guid.NewGuid().ToString(), Name = name, Type = title, Level = 1,
                UnitLink = unit, StabilityBonus = stab, PPGainBonus = pp/100f, AttackBonus = atk/100f,
                ResearchBonus = res/100f, TaxBonus = tax/100f, CorruptionReduction = corr
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
            else s += "(Abstract Leader)\n";
            s += "\n<b>Bonuses:</b>\n";
            if (l.StabilityBonus > 0) s += $"Stability: +{l.StabilityBonus:0.#}\n";
            if (l.PPGainBonus > 0) s += $"Political Power: +{l.PPGainBonus*100:0}%\n";
            if (l.AttackBonus > 0) s += $"Army Attack: +{l.AttackBonus*100:0}%\n";
            if (l.ResearchBonus > 0) s += $"Research: +{l.ResearchBonus*100:0}%\n";
            if (l.TaxBonus > 0) s += $"Tax Income: +{l.TaxBonus*100:0}%\n";
            if (l.CorruptionReduction > 0) s += $"Corruption: -{l.CorruptionReduction*100:0.##}\n";
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
            return t;
        }
    }
}