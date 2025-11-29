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
            public int Cost;
        }

        // --- POLICIES FROM YOUR TEXT FILE ---
        public static readonly List<PolicyDef> Policies = new List<PolicyDef>
        {
            new PolicyDef { Id = "vassalage", Name = "Vassalage", Cost = 100, Description = "Allows the recruitment of vassals to manage distant lands, increasing tax efficiency but slightly raising autonomy." },
            new PolicyDef { Id = "mercenary_contracts", Name = "Mercenary Contracts", Cost = 250, Description = "Enables hiring mercenary companies for immediate military support at a high gold cost." },
            new PolicyDef { Id = "royal_guard", Name = "Royal Guard", Cost = 500, Description = "Establish an elite guard unit for the monarch, increasing stability and defense but at a high upkeep cost." },
            new PolicyDef { Id = "feudal_obligations", Name = "Feudal Obligations", Cost = 150, Description = "Standardizes military service requirements from lords, increasing manpower but slightly lowering stability." },
            new PolicyDef { Id = "spy_network", Name = "Spy Network", Cost = 300, Description = "Develops a kingdom-wide spy network to uncover plots and increase diplomatic visibility." },
            new PolicyDef { Id = "naval_dominance", Name = "Naval Dominance", Cost = 400, Description = "Focuses resources on building a powerful navy to control trade routes and coastal regions." },
            new PolicyDef { Id = "fortification_effort", Name = "Fortification Effort", Cost = 200, Description = "Mandates the construction of defensive structures in border provinces, increasing defensiveness but costing gold." },
            new PolicyDef { Id = "diplomatic_corps", Name = "Diplomatic Corps", Cost = 150, Description = "Establishes a permanent corps of diplomats to improve relations with neighboring kingdoms." },
            new PolicyDef { Id = "legal_reform", Name = "Legal Reform", Cost = 350, Description = "Standardizes laws across the kingdom, increasing stability and tax income but costing significant gold to implement." },
            new PolicyDef { Id = "cultural_assimilation", Name = "Cultural Assimilation", Cost = 250, Description = "Promotes the dominant culture in newly conquered lands, speeding up integration but increasing unrest." },
            new PolicyDef { Id = "religious_inquisition", Name = "Religious Inquisition", Cost = 400, Description = "Enforces religious unity, increasing stability and piety but significantly raising unrest in diverse regions." },
            new PolicyDef { Id = "trade_guilds", Name = "Trade Guilds", Cost = 200, Description = "Encourages the formation of trade guilds, boosting trade income and production but reducing royal control over the economy." }
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

                // Two Column Layout (Like LeadersWindow)
                var h = root.AddComponent<HorizontalLayoutGroup>();
                h.spacing = 2;
                h.padding = new RectOffset(1, 1, 1, 1);
                h.childControlWidth = true;
                h.childControlHeight = true;
                h.childForceExpandWidth = true;
                h.childForceExpandHeight = true;

                // Create Left (Available) and Right (Enacted) panels
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

            // Ensure list exists
            if (d.ActivePolicies == null) d.ActivePolicies = new HashSet<string>();

            // --- REFRESH AVAILABLE LIST ---
            if (availableContent != null)
            {
                foreach (Transform t in availableContent) Object.Destroy(t.gameObject);
                
                foreach (var policy in Policies)
                {
                    // If NOT active, show in available list
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
            // Greenish for active, Dark Blueish for available
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

            // Text Stack
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

            // Name
            CreateText(textStack.transform, policy.Name, 9, FontStyle.Bold, Color.white);
            
            // Cost or Status
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
                // Repeal
                d.ActivePolicies.Remove(policy.Id);
                WorldTip.showNow($"Repealed {policy.Name}", false, "top", 1.5f, "#FF5A5A");
            }
            else
            {
                // Enact
                if (d.Treasury >= policy.Cost)
                {
                    d.Treasury -= policy.Cost;
                    d.ActivePolicies.Add(policy.Id);
                    WorldTip.showNow($"Enacted {policy.Name}!", false, "top", 1.5f, "#9EE07A");
                }
                else
                {
                    WorldTip.showNow($"Not enough gold ({d.Treasury}/{policy.Cost})", false, "top", 1.5f, "#FF5A5A");
                    return; // Don't refresh if failed
                }
            }
            
            KingdomMetricsSystem.RecalculateForKingdom(k, d);
            Refresh();
            TopPanelUI.Refresh(); // Update gold display
        }

        private static string GetPolicyTooltip(PolicyDef p)
        {
            return $"<b><color=#FFD700>{p.Name}</color></b>\n\n{p.Description}\n\n<color=#888888>(Click to Toggle)</color>";
        }

        // --- UI CREATION HELPERS (Reused structure) ---

        private static void CreateAvailablePanel(Transform parent)
        {
            var container = CreatePanelBase(parent, "AvailablePanel"); 
            
            availableHeader = CreateText(container.transform, "Available", 9, FontStyle.Bold, Color.white);
            availableHeader.alignment = TextAnchor.MiddleCenter;
            
            var le = availableHeader.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 24f; le.minHeight = 24f;

            availableContent = CreateScrollList(container.transform, "AvailableScroll");
        }

        private static void CreateActivePanel(Transform parent)
        {
            var container = CreatePanelBase(parent, "ActivePanel"); 
            
            activeHeader = CreateText(container.transform, "Enacted", 9, FontStyle.Bold, new Color(1f, 0.85f, 0.4f)); 
            activeHeader.alignment = TextAnchor.MiddleCenter;
            
            var le = activeHeader.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = 24f; le.minHeight = 24f;

            activeContent = CreateScrollList(container.transform, "ActiveScroll");
        }

        private static GameObject CreatePanelBase(Transform parent, string name)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var le = panel.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f; le.flexibleHeight = 1f;
            
            var bg = panel.AddComponent<Image>();
            if (windowInnerSprite != null) { bg.sprite = windowInnerSprite; bg.type = Image.Type.Sliced; }
            bg.color = new Color(0, 0, 0, 0.001f); 
            
            var v = panel.AddComponent<VerticalLayoutGroup>();
            v.spacing = 1; 
            v.padding = new RectOffset(1, 1, 1, 1);
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
            scroll.horizontal = false; 
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
            v.spacing = 2; 
            v.childControlWidth = true; 
            v.childControlHeight = true; 
            v.childForceExpandHeight = false;
            
            // --- IMPORTANT: CONTENT RESIZING ---
            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            // Horizontal fit not needed for list items, they expand to width
            
            scroll.viewport = vpRT; scroll.content = cRT;
            return content.transform;
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