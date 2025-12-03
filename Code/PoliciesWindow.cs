using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace RulerBox
{
    public static class PoliciesWindow
    {
        private static GameObject root;
        private static Sprite windowInnerSprite;

        private static Transform availableContent;
        private static Transform activeContent;
        private static Text activeHeader;
        private static Text availableHeader;
        
        // --- POLICY DATA STRUCTURE ---
        public class PolicyDef
        {
            public string Id;
            public string Name;
            public string Description;
            public int Cost;   // One-time Gold Cost
            public int Upkeep; // Gold per update
            
            // Used for tooltip generation
            public string EffectsDescription; 
        }

        // --- POLICIES FROM YOUR TEXT FILE ---
        // (Costs scaled x10 from PP values for Gold balance)
        public static readonly List<PolicyDef> Policies = new List<PolicyDef>
        {
            new PolicyDef { Id = "welfare_act", Name = "The Welfare Act", Cost = 400, Upkeep = 6, 
                Description = "We will assist in providing the needs to those who are unable to provide for themselves.",
                EffectsDescription = "Stability: +5\nTax Income: -5%" },

            new PolicyDef { Id = "public_service", Name = "The Public Service Act", Cost = 550, Upkeep = 8, 
                Description = "Encourage population to employ themselves in the public sector to improve output.",
                EffectsDescription = "Factory Output: +20%\nResource Output: +20%" },

            new PolicyDef { Id = "military_service", Name = "The Military Service Act", Cost = 400, Upkeep = 7, 
                Description = "We will incentivize those who join in our armed forces.",
                EffectsDescription = "Military Upkeep: -10%\nManpower Cap: +20%" },

            new PolicyDef { Id = "central_authority", Name = "Strengthen Central Authority", Cost = 500, Upkeep = 5, 
                Description = "Hasten integration of newly conquered lands by taking a more aggressive stance.",
                EffectsDescription = "Unrest Reduction: +33%" },

            new PolicyDef { Id = "prosperity_act", Name = "The Prosperity Act", Cost = 200, Upkeep = 2, 
                Description = "As long as we are united, our nation will be better off than ever.",
                EffectsDescription = "Tax Income: +10%" },

            new PolicyDef { Id = "infrastructure", Name = "Improve Infrastructure", Cost = 400, Upkeep = 7, 
                Description = "By improving our national highways and ports, we seek to strengthen our economic position.",
                EffectsDescription = "Tax Income: +10%\nBuilding Speed: +5%" },

            new PolicyDef { Id = "war_fund", Name = "Emergency War Fund", Cost = 1000, Upkeep = 40, 
                Description = "Raise additional funds to maintain our military in these dire times!",
                EffectsDescription = "Tax Income: +40%\nMilitary Upkeep: -25%\nStability: -10\nUnrest Reduction: -15%\nWE Gain: +0.01" },

            new PolicyDef { Id = "martial_law", Name = "Martial Law", Cost = 1000, Upkeep = 20, 
                Description = "The military will fulfill the duties of maintaining order.",
                EffectsDescription = "Tax Income: -25%\nStability: +60\nUnrest Reduction: +33%\nOutput: -20%" },

            new PolicyDef { Id = "research_bureau", Name = "Advance Research Bureau", Cost = 550, Upkeep = 8, 
                Description = "Place a higher priority on advancing our research efforts.",
                EffectsDescription = "Research Output: +20%" },

            new PolicyDef { Id = "encourage_dev", Name = "Encourage Development", Cost = 750, Upkeep = 5, 
                Description = "Tax breaks and incentives aimed at encouraging domestic development.",
                EffectsDescription = "Tax Income: -10%\nInvestment Avail: +50%" },
            
            new PolicyDef { Id = "tax_reform", Name = "Tax Reform", Cost = 600, Upkeep = 6, 
                Description = "Making taxes easier to be filled and harder for tax evasion to happen.",
                EffectsDescription = "Tax Income: +25%" },

            new PolicyDef { Id = "forced_labour", Name = "Forced Labour", Cost = 350, Upkeep = 6, 
                Description = "Employ dissidents in factories and mines. 'Off to the gulag!'",
                EffectsDescription = "Tax: +15%\nOutput: +40%\nPop Growth: -2%\nCorruption: -5%" }
        };

        public static void Initialize(Transform parent)
        {
            if (root != null) return;

            try 
            {
                availableContent = null;
                activeContent = null;
                activeHeader = null;
                availableHeader = null;

                windowInnerSprite = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.windowInnerSliced.png");

                // Root Object
                root = new GameObject("PoliciesWindow");
                root.transform.SetParent(parent, false);
                var rt = root.AddComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

                // Two Column Layout
                var h = root.AddComponent<HorizontalLayoutGroup>();
                h.spacing = 2;
                h.padding = new RectOffset(1, 1, 1, 1);
                h.childControlWidth = true;
                h.childControlHeight = true;
                h.childForceExpandWidth = true;
                h.childForceExpandHeight = true;

                CreateAvailablePanel(root.transform);
                CreateActivePanel(root.transform);

                root.SetActive(false);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[RulerBox] PoliciesWindow Init Error: " + e.ToString());
            }
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
            if (d == null) return;

            if (d.ActivePolicies == null) d.ActivePolicies = new HashSet<string>();

            // --- REFRESH AVAILABLE LIST ---
            if (availableContent != null)
            {
                foreach (Transform t in availableContent) Object.Destroy(t.gameObject);
                
                foreach (var policy in Policies)
                {
                    if (!d.ActivePolicies.Contains(policy.Id))
                    {
                        CreatePolicyItem(availableContent, policy, false);
                    }
                }
                int availableCount = Policies.Count - d.ActivePolicies.Count;
                if(availableHeader != null) availableHeader.text = $"Available ({availableCount})";
            }

            // --- REFRESH ENACTED LIST ---
            if (activeContent != null)
            {
                foreach (Transform t in activeContent) Object.Destroy(t.gameObject);
                
                foreach (var policyId in d.ActivePolicies)
                {
                    var policy = Policies.Find(p => p.Id == policyId);
                    if (policy != null)
                    {
                        CreatePolicyItem(activeContent, policy, true);
                    }
                }
                if(activeHeader != null) activeHeader.text = $"Enacted ({d.ActivePolicies.Count})";
            }
        }

        private static void CreatePolicyItem(Transform parent, PolicyDef policy, bool isActive)
        {
            var btnObj = new GameObject("PolicyItem");
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
            btn.onClick.AddListener(() => OnPolicyClicked(policy, isActive));

            var h = btnObj.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 2;
            h.padding = new RectOffset(70, 70, 2, 2); 
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false; 
            h.childAlignment = TextAnchor.MiddleLeft;

            var textStack = new GameObject("InfoStack");
            textStack.transform.SetParent(btnObj.transform, false);
            
            var textVL = textStack.AddComponent<VerticalLayoutGroup>();
            textVL.childAlignment = TextAnchor.MiddleLeft;
            textVL.spacing = 0;
            textVL.childControlHeight = true; textVL.childControlWidth = true;
            textVL.childForceExpandHeight = false; textVL.childForceExpandWidth = true;

            var textStackLE = textStack.AddComponent<LayoutElement>();
            textStackLE.flexibleWidth = 1f;
            textStackLE.minWidth = 50f;

            CreateText(textStack.transform, policy.Name, 9, FontStyle.Bold, Color.white);
            
            if (isActive)
            {
                CreateText(textStack.transform, "Enacted", 8, FontStyle.Italic, new Color(0.6f, 0.9f, 0.6f));
            }
            else
            {
                CreateText(textStack.transform, $"Cost: {policy.Cost}g", 8, FontStyle.Normal, new Color(1f, 0.85f, 0.4f));
            }

            ChipTooltips.AttachSimpleTooltip(btnObj, () => GetPolicyTooltip(policy));
        }

        private static void OnPolicyClicked(PolicyDef policy, bool isActive)
        {
            var k = Main.selectedKingdom;
            if (k == null) return;
            var d = KingdomMetricsSystem.Get(k);
            if (d.ActivePolicies == null) d.ActivePolicies = new HashSet<string>();

            if (isActive)
            {
                d.ActivePolicies.Remove(policy.Id);
                WorldTip.showNow($"Repealed {policy.Name}", false, "top", 1.5f, "#FF5A5A");
            }
            else
            {
                if (d.Treasury >= policy.Cost)
                {
                    d.Treasury -= policy.Cost;
                    d.ActivePolicies.Add(policy.Id);
                    WorldTip.showNow($"Enacted {policy.Name}!", false, "top", 1.5f, "#9EE07A");
                }
                else
                {
                    WorldTip.showNow($"Not enough gold ({d.Treasury}/{policy.Cost})", false, "top", 1.5f, "#FF5A5A");
                    return; 
                }
            }
            
            KingdomMetricsSystem.RecalculateForKingdom(k, d);
            Refresh();
            TopPanelUI.Refresh();
        }

        // --- TOOLTIP WITH SHIFT SUPPORT ---
        private static string GetPolicyTooltip(PolicyDef p)
        {
            bool showDetails = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            
            string baseInfo = $"<b><color=#FFD700>{p.Name}</color></b>\n{p.Description}";
            string costInfo = $"\n\nUpkeep: <color=#FF5A5A>{p.Upkeep}g</color> / update";

            if (showDetails)
            {
                return baseInfo + costInfo + $"\n\n<b>Effects:</b>\n{p.EffectsDescription}";
            }
            else
            {
                return baseInfo + "\n\nHold <b>[SHIFT]</b> for Effects";
            }
        }

        // --- UI HELPERS ---
        private static void CreateAvailablePanel(Transform parent)
        {
            var container = CreatePanelBase(parent, "AvailablePanel"); 
            availableHeader = CreateText(container.transform, "Available", 9, FontStyle.Bold, Color.white);
            availableHeader.alignment = TextAnchor.MiddleCenter;
            availableHeader.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
            availableContent = CreateScrollList(container.transform, "AvailableScroll");
        }

        private static void CreateActivePanel(Transform parent)
        {
            var container = CreatePanelBase(parent, "ActivePanel"); 
            activeHeader = CreateText(container.transform, "Enacted", 9, FontStyle.Bold, new Color(1f, 0.85f, 0.4f)); 
            activeHeader.alignment = TextAnchor.MiddleCenter;
            activeHeader.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
            activeContent = CreateScrollList(container.transform, "ActiveScroll");
        }

        private static GameObject CreatePanelBase(Transform parent, string name)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            panel.AddComponent<LayoutElement>().flexibleWidth = 1f;
            
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
            scrollObj.AddComponent<LayoutElement>().flexibleHeight = 1f;
            
            var bg = scrollObj.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.3f); 
            
            var scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.vertical = true; scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
            
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
            v.spacing = 2; v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandHeight = false;
            
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            scroll.viewport = vpRT; scroll.content = cRT;
            return content.transform;
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