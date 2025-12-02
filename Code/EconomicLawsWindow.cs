using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace RulerBox
{
    public static class EconomicLawsWindow
    {
        private static GameObject root;
        private static Sprite windowInnerSprite;
        private static Transform content;
        private static Text treasuryText;
        private static Text incomeText;
        
        private static Kingdom lastKingdom;
        private static float updateTimer = 0f;

        public static void Initialize(Transform parent)
        {
            if (root != null) return;
            Debug.Log("[RulerBox] EconomicLawsWindow: Initializing UI...");

            try 
            {
                content = null;
                treasuryText = null;
                incomeText = null;

                windowInnerSprite = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.windowInnerSliced.png");

                root = new GameObject("EconomicLawsWindow");
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

                CreateMainPanel(root.transform);

                root.SetActive(false);
                Debug.Log("[RulerBox] EconomicLawsWindow: Initialization Complete.");
            }
            catch (System.Exception e)
            {
                Debug.LogError("[RulerBox] EconomicLawsWindow Init CRASHED: " + e.ToString());
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

        public static void Update()
        {
            if (root == null || !root.activeSelf) return;
            updateTimer += Time.deltaTime;
            if (updateTimer > 1f)
            {
                updateTimer = 0f;
                RefreshHeader();
            }
        }

        public static void Refresh()
        {
            if (root == null || content == null) return;
            if (!root.activeSelf) return;

            try
            {
                var k = Main.selectedKingdom;
                if (k == null) return;

                var d = KingdomMetricsSystem.Get(k);
                if (d == null) return;

                RefreshHeader();

                if (lastKingdom != k)
                {
                    RebuildList(d);
                    lastKingdom = k;
                }
                else
                {
                    // Refresh existing items (update active state)
                    foreach(Transform child in content)
                    {
                        var btn = child.GetComponent<Button>();
                        if (btn != null)
                        {
                            // Find which law this button represents
                            // This is a bit hacky, ideally we store a reference
                            // For now, we just rebuild if kingdom changes.
                            // To support dynamic updates without rebuild, we'd need a map.
                            // Let's just rebuild for simplicity if needed, or rely on clicks updating.
                        }
                    }
                    // Force rebuild to ensure correct states
                    RebuildList(d);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[RulerBox] EconomicLawsWindow Refresh Error: " + e.Message);
            }
        }

        private static void RefreshHeader()
        {
            var k = Main.selectedKingdom;
            if (k == null) return;
            var d = KingdomMetricsSystem.Get(k);
            if (d == null) return;

            if (treasuryText != null) treasuryText.text = $"Treasury: {d.Treasury}g";
            if (incomeText != null) 
            {
                string col = d.Income >= 0 ? "#7CFC00" : "#FF5A5A";
                incomeText.text = $"Income: <color={col}>{d.Income}g/yr</color>";
            }
        }

        private static void RebuildList(KingdomData d)
        {
            foreach (Transform t in content) Object.Destroy(t.gameObject);

            CreateLawItem("Taxation", "taxation", d.TaxationLevel, new[] { "none", "low", "medium", "high", "maximum" });
            CreateLawItem("Military Spending", "military_spending", d.MilitarySpending, new[] { "none", "low", "medium", "high", "maximum" });
            CreateLawItem("Security Spending", "security_spending", d.SecuritySpending, new[] { "none", "low", "medium", "high", "maximum" });
            CreateLawItem("Government Spending", "government_spending", d.GovernmentSpending, new[] { "none", "low", "medium", "high", "maximum" });
            CreateLawItem("Welfare Spending", "welfare_spending", d.WelfareSpending, new[] { "none", "low", "medium", "high", "maximum" });
            CreateLawItem("Education Spending", "education_spending", d.EducationSpending, new[] { "none", "low", "medium", "high", "maximum" });
            CreateLawItem("Research Spending", "research_spending", d.ResearchSpending, new[] { "none", "low", "medium", "high", "maximum" });
            CreateLawItem("Anti-Corruption", "anti_corruption", d.AntiCorruption, new[] { "none", "low", "medium", "high", "maximum" });
        }

        private static void CreateLawItem(string title, string id, string currentLevel, string[] options)
        {
            var item = new GameObject("LawItem");
            item.transform.SetParent(content, false);
            var le = item.AddComponent<LayoutElement>();
            le.minHeight = 60f; le.preferredHeight = 60f; le.flexibleWidth = 1f;

            var bg = item.AddComponent<Image>();
            bg.sprite = windowInnerSprite; bg.type = Image.Type.Sliced;
            bg.color = new Color(0.2f, 0.2f, 0.25f, 0.8f);

            var v = item.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(5, 5, 5, 5); v.spacing = 2;
            v.childControlWidth = true; v.childControlHeight = true;

            // Title
            CreateText(item.transform, title, 10, FontStyle.Bold, Color.white).alignment = TextAnchor.MiddleCenter;

            // Options Row
            var row = new GameObject("Options");
            row.transform.SetParent(item.transform, false);
            var h = row.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 2; h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = true;

            foreach (var opt in options)
            {
                CreateOptionButton(row.transform, id, opt, currentLevel == opt);
            }
        }

        private static void CreateOptionButton(Transform parent, string id, string level, bool isActive)
        {
            var btnObj = new GameObject(level);
            btnObj.transform.SetParent(parent, false);
            
            var img = btnObj.AddComponent<Image>();
            img.sprite = windowInnerSprite; img.type = Image.Type.Sliced;
            img.color = isActive ? new Color(0.2f, 0.6f, 0.2f) : new Color(0.3f, 0.3f, 0.35f);

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => OnLawClicked(id, level));

            var t = CreateText(btnObj.transform, level.ToUpper(), 7, FontStyle.Normal, Color.white);
            t.alignment = TextAnchor.MiddleCenter;

            ChipTooltips.AttachSimpleTooltip(btnObj, () => GetLawTooltip(id, level));
        }

        private static void OnLawClicked(string id, string level)
        {
            var k = Main.selectedKingdom;
            if (k == null) return;
            var d = KingdomMetricsSystem.Get(k);
            
            // Update Data
            switch(id)
            {
                case "taxation": d.TaxationLevel = level; break;
                case "military_spending": d.MilitarySpending = level; break;
                case "security_spending": d.SecuritySpending = level; break;
                case "government_spending": d.GovernmentSpending = level; break;
                case "welfare_spending": d.WelfareSpending = level; break;
                case "education_spending": d.EducationSpending = level; break;
                case "research_spending": d.ResearchSpending = level; break;
                case "anti_corruption": d.AntiCorruption = level; break;
            }

            KingdomMetricsSystem.RecalculateForKingdom(k, d);
            Refresh(); // Rebuild UI to show active state
        }

        private static string GetLawTooltip(string id, string lvl)
        {
            float pct = 0f;
            
            // 1) Calculate Cost Percentage
            if (id != "taxation") 
            {
                // Default structure (Balanced)
                if (lvl == "none") pct = 0f;
                else if (lvl == "low") pct = 0.02f;
                else if (lvl == "medium") pct = 0.04f;
                else if (lvl == "high") pct = 0.06f;
                else if (lvl == "maximum") pct = 0.08f;
                
                // Specific overrides (Balanced)
                if (id == "military_spending") 
                    pct = lvl switch { "none"=>0f, "low"=>0.08f, "medium"=>0.12f, "high"=>0.16f, "maximum"=>0.20f, _=>0f };
                else if (id == "security_spending")
                    pct = lvl switch { "none"=>0f, "low"=>0.03f, "medium"=>0.05f, "high"=>0.07f, "maximum"=>0.09f, _=>0f };
                else if (id == "anti_corruption")
                    pct = lvl switch { "none"=>0f, "low"=>0.04f, "medium"=>0.06f, "high"=>0.08f, "maximum"=>0.10f, _=>0f };
                else if (id == "research_spending")
                    pct = lvl switch { "none"=>0f, "low"=>0.04f, "medium"=>0.06f, "high"=>0.08f, "maximum"=>0.10f, _=>0f };
            }

            string costStr = (pct > 0) ? $"\nCost: <color=#FF5A5A>{pct*100:0.#}% Income</color>" : "";
            
            // 2) Descriptions
            string desc = "";
            if (id == "taxation")
            {
                if (lvl == "none") desc = "No taxes. High stability, no income.";
                else if (lvl == "low") desc = "Low taxes. Good stability.";
                else if (lvl == "medium") desc = "Standard taxes.";
                else if (lvl == "high") desc = "High taxes. -10 Stability.";
                else if (lvl == "maximum") desc = "Maximum taxes. -25 Stability.";
            }
            else if (id == "military_spending")
            {
                if (lvl == "none") desc = "No army funding.";
                else desc = "Increases army attack and max manpower.";
            }
            else if (id == "security_spending")
            {
                if (lvl == "none") desc = "No security.";
                else desc = "Reduces corruption and rebellion chance.";
            }
            else if (id == "government_spending")
            {
                if (lvl == "none") desc = "Minimal government.";
                else desc = "Increases max cities and political power.";
            }
            else if (id == "welfare_spending")
            {
                if (lvl == "none") desc = "No welfare.";
                else desc = "Increases population growth and stability.";
            }
            else if (id == "education_spending")
            {
                if (lvl == "none") desc = "No education.";
                else desc = "Increases tech speed and leader quality.";
            }
            else if (id == "research_spending")
            {
                if (lvl == "none") desc = "No research grants.";
                else desc = "Greatly boosts research speed.";
            }
            else if (id == "anti_corruption")
            {
                if (lvl == "none") desc = "Corruption runs rampant.";
                else desc = "Directly reduces corruption.";
            }

            return $"<b>{lvl.ToUpper()}</b>\n{desc}{costStr}";
        }

        private static void CreateMainPanel(Transform parent)
        {
            var container = new GameObject("Container");
            container.transform.SetParent(parent, false);
            var le = container.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f; le.flexibleHeight = 1f;
            
            var v = container.AddComponent<VerticalLayoutGroup>();
            v.spacing = 4; v.padding = new RectOffset(4, 4, 4, 4);
            v.childControlWidth = true; v.childControlHeight = true;

            // Header Stats
            var stats = new GameObject("Stats");
            stats.transform.SetParent(container.transform, false);
            var h = stats.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 10; h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = true; h.childControlHeight = true;
            
            var sle = stats.AddComponent<LayoutElement>();
            sle.preferredHeight = 30f; sle.minHeight = 30f;

            treasuryText = CreateText(stats.transform, "Treasury: 0g", 12, FontStyle.Bold, new Color(1f, 0.85f, 0.4f));
            incomeText = CreateText(stats.transform, "Income: 0g/yr", 12, FontStyle.Bold, Color.white);
            treasuryText.alignment = TextAnchor.MiddleCenter;
            incomeText.alignment = TextAnchor.MiddleCenter;

            // Scroll View
            var scrollObj = new GameObject("Scroll");
            scrollObj.transform.SetParent(container.transform, false);
            var sle2 = scrollObj.AddComponent<LayoutElement>();
            sle2.flexibleHeight = 1f;

            var scroll = scrollObj.AddComponent<ScrollRect>();
            scroll.vertical = true; scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 20f;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            var vpRT = viewport.AddComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
            viewport.AddComponent<RectMask2D>();

            var contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewport.transform, false);
            var cRT = contentObj.AddComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 1); cRT.anchorMax = new Vector2(1, 1);
            cRT.pivot = new Vector2(0.5f, 1);

            var cv = contentObj.AddComponent<VerticalLayoutGroup>();
            cv.spacing = 2; cv.childControlWidth = true; cv.childControlHeight = true; cv.childForceExpandHeight = false;
            
            var csf = contentObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = vpRT; scroll.content = cRT;
            content = contentObj.transform;
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