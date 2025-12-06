using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System;

namespace RulerBox
{
    public static class RankingsWindow
    {
        private static GameObject root;
        private static Transform scrollContent;
        private static Sprite windowInnerSprite;
        
        // Configuration
        private static readonly Color BarColorPlayer = new Color(0.48f, 0.99f, 0f, 1f); // Green
        private static readonly Color BarColorEnemy = new Color(1f, 0.35f, 0.35f, 1f); // Red
        private static readonly Color BarColorNeutral = new Color(1f, 0.84f, 0f, 1f); // Gold/Neutral

        public static void Initialize(Transform parent)
        {
            if (root != null) return;

            windowInnerSprite = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.windowInnerSliced.png");

            // 1. Create Window Root
            root = new GameObject("RankingsWindow", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Background / blocker
            var bg = root.AddComponent<Image>();
            if (windowInnerSprite != null) { bg.sprite = windowInnerSprite; bg.type = Image.Type.Sliced; }
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0f);

            // 2. Main Layout Container
            var mainContainer = new GameObject("MainContainer");
            mainContainer.transform.SetParent(root.transform, false);
            var mcRT = mainContainer.AddComponent<RectTransform>();
            mcRT.anchorMin = new Vector2(0.05f, 0.05f);
            mcRT.anchorMax = new Vector2(0.95f, 0.95f);
            mcRT.offsetMin = Vector2.zero;
            mcRT.offsetMax = Vector2.zero;

            var vGroup = mainContainer.AddComponent<VerticalLayoutGroup>();
            vGroup.spacing = 10;
            vGroup.childControlHeight = true;
            vGroup.childControlWidth = true;
            vGroup.childForceExpandHeight = false;

            // 3. Header
            CreateHeader(mainContainer.transform, "Kingdom Rankings");

            // 4. Scroll View (Horizontal)
            var scrollObj = new GameObject("ScrollArea");
            scrollObj.transform.SetParent(mainContainer.transform, false);
            var scrollLE = scrollObj.AddComponent<LayoutElement>();
            scrollLE.flexibleHeight = 1f; // Fill remaining space

            var scrollBg = scrollObj.AddComponent<Image>();
            scrollBg.color = new Color(0, 0, 0, 0f);

            var scrollRect = scrollObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;

            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            var vpRT = viewport.AddComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero;
            vpRT.anchorMax = Vector2.one;
            vpRT.sizeDelta = Vector2.zero;
            viewport.AddComponent<RectMask2D>();

            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            scrollContent = content.AddComponent<RectTransform>();
            
            // --- Content Anchors for Horizontal Scrolling ---
            // Anchor Min (0,0) and Max (0,1) stretches it vertically to fill viewport height
            var cRT = scrollContent.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 0); 
            cRT.anchorMax = new Vector2(0, 1); 
            cRT.pivot = new Vector2(0, 1);
            cRT.sizeDelta = Vector2.zero; // Reset offsets
            
            // Content Layout
            var hGroup = content.AddComponent<HorizontalLayoutGroup>();
            hGroup.childAlignment = TextAnchor.LowerLeft;
            hGroup.spacing = 2;
            hGroup.padding = new RectOffset(1, 1, 1, 1);
            // Crucial: ChildControlHeight true + ChildForceExpandHeight true makes all columns maximize their height
            hGroup.childControlHeight = true; 
            hGroup.childControlWidth = false;
            hGroup.childForceExpandHeight = true;
            hGroup.childForceExpandWidth = false;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained; // Height matches viewport via anchors

            scrollRect.viewport = vpRT;
            scrollRect.content = cRT;

            root.SetActive(false);
        }

        public static void SetVisible(bool visible)
        {
            if (root == null && visible) Initialize(DebugConfig.instance.transform); // Auto-init helper if null
            if (root == null) return;
            root.SetActive(visible);
            if (visible)
            {
                Refresh();
            }
        }
        
        public static bool IsVisible() => root != null && root.activeSelf;

        private static void Refresh()
        {
            // Clear old items
            if (scrollContent == null) return;
            foreach (Transform child in scrollContent)
            {
                UnityEngine.Object.Destroy(child.gameObject);
            }

            // 1. Collect Data
            List<RankingData> rankedList = new List<RankingData>();
            float maxScore = 0f;

            foreach (var k in World.world.kingdoms.list)
            {
                if (!k.isAlive() || k.data.id == Globals.NEUTRAL_KINGDOM_NUMERIC_ID) continue;

                float score = CalculatePowerScore(k);
                if (score > maxScore) maxScore = score;

                rankedList.Add(new RankingData { kingdom = k, score = score });
            }

            // 2. Sort by Score Descending
            rankedList.Sort((a, b) => b.score.CompareTo(a.score));

            // 3. Create UI Items
            for (int i = 0; i < rankedList.Count; i++)
            {
                CreateRankingItem(rankedList[i], i + 1, maxScore);
            }
        }

        // --- UI Construction Helpers ---

        private static void CreateRankingItem(RankingData data, int rank, float maxScore)
        {
            // Container for a single kingdom column
            // It will be stretched vertically by the parent HorizontalLayoutGroup
            var col = new GameObject($"Rank_{rank}_{data.kingdom.data.name}");
            col.transform.SetParent(scrollContent, false);
            
            var le = col.AddComponent<LayoutElement>();
            le.preferredWidth = 100f;
            le.minWidth = 100f;

            var vGroup = col.AddComponent<VerticalLayoutGroup>();
            vGroup.spacing = 2;
            vGroup.childControlHeight = true; // Control children height
            vGroup.childForceExpandHeight = false; // Don't force them all to expand equally
            vGroup.childAlignment = TextAnchor.LowerCenter;

            // 1. Score Text (Fixed height at top)
            var scoreTxt = CreateText(col.transform, FormatNumber(data.score), 10, FontStyle.Normal, Color.yellow);
            scoreTxt.alignment = TextAnchor.LowerCenter;
            var scoreLE = scoreTxt.gameObject.AddComponent<LayoutElement>();
            scoreLE.preferredHeight = 20f;
            scoreLE.minHeight = 20f;
            scoreLE.flexibleHeight = 0f; // Fixed

            // 2. Bar Container (Flexible height - fills remaining space)
            // This is the empty space the bar CAN fill.
            var barContainer = new GameObject("BarContainer");
            barContainer.transform.SetParent(col.transform, false);
            var containerLE = barContainer.AddComponent<LayoutElement>();
            containerLE.flexibleHeight = 1f; // Take all available space
            containerLE.minHeight = 10f;     // Minimum size so it doesn't vanish

            // 2b. The Bar Fill (Actual colored image)
            var barObj = new GameObject("BarFill");
            barObj.transform.SetParent(barContainer.transform, false);
            var barImg = barObj.AddComponent<Image>();
            
            // Determine Color
            if (data.kingdom == Main.selectedKingdom) barImg.color = BarColorPlayer;
            else if (Main.selectedKingdom != null && data.kingdom.isEnemy(Main.selectedKingdom)) barImg.color = BarColorEnemy;
            else barImg.color = BarColorNeutral;

            // --- ANCHOR LOGIC ---
            // We set the anchors so the bar fills a percentage of the parent container
            float fillPercentage = (maxScore > 0) ? (data.score / maxScore) : 0f;
            
            var barRT = barObj.GetComponent<RectTransform>();
            barRT.anchorMin = new Vector2(0.2f, 0f); // 20% margin left/right
            barRT.anchorMax = new Vector2(0.8f, fillPercentage); // Height based on score
            barRT.offsetMin = Vector2.zero;
            barRT.offsetMax = Vector2.zero;

            // 3. Flag (Fixed height)
            var flagContainer = new GameObject("Flag");
            flagContainer.transform.SetParent(col.transform, false);
            var flagLE = flagContainer.AddComponent<LayoutElement>();
            flagLE.preferredHeight = 40f;
            flagLE.minHeight = 40f;
            flagLE.flexibleHeight = 0f;

            // Flag Background
            var flagBg = new GameObject("FlagBG").AddComponent<Image>();
            flagBg.transform.SetParent(flagContainer.transform, false);
            flagBg.sprite = data.kingdom.getElementBackground();
            flagBg.color = data.kingdom.kingdomColor.getColorMain32();
            flagBg.rectTransform.anchorMin = new Vector2(0.3f, 0f);
            flagBg.rectTransform.anchorMax = new Vector2(0.7f, 1f);
            flagBg.rectTransform.offsetMin = Vector2.zero;
            flagBg.rectTransform.offsetMax = Vector2.zero;

            // Flag Icon
            var flagIcon = new GameObject("FlagIcon").AddComponent<Image>();
            flagIcon.transform.SetParent(flagContainer.transform, false);
            flagIcon.sprite = data.kingdom.getElementIcon();
            flagIcon.color = data.kingdom.kingdomColor.getColorBanner();
            flagIcon.rectTransform.anchorMin = new Vector2(0.3f, 0f);
            flagIcon.rectTransform.anchorMax = new Vector2(0.7f, 1f);
            flagIcon.rectTransform.offsetMin = Vector2.zero;
            flagIcon.rectTransform.offsetMax = Vector2.zero;

            // 4. Rank & Name (Fixed height at bottom)
            string nameStr = $"#{rank}\n{data.kingdom.data.name}";
            var nameTxt = CreateText(col.transform, nameStr, 11, FontStyle.Bold, Color.white);
            nameTxt.alignment = TextAnchor.UpperCenter;
            var nameLE = nameTxt.gameObject.AddComponent<LayoutElement>();
            nameLE.preferredHeight = 40f;
            nameLE.minHeight = 40f;
            nameLE.flexibleHeight = 0f;
        }

        private static float CalculatePowerScore(Kingdom k)
        {
            var d = KingdomMetricsSystem.Get(k);
            // Force update to ensure AI kingdoms have stats calculated for this frame
            // We do NOT rely on accumulated stats like Treasury which AI might lack.
            KingdomMetricsSystem.RecalculateForKingdom(k, d);

            // --- 1. ECONOMY (Potential GDP) ---
            // Income: Daily estimated wealth generation. Fair for all.
            float ecoScore = d.Income * 10.0f;

            // Resources: Physical stockpiles in cities.
            float resourceWealth = 0f;
            if (d.ResourceStockpiles != null)
            {
                foreach(var kvp in d.ResourceStockpiles) resourceWealth += kvp.Value;
            }
            ecoScore += resourceWealth * 0.5f;

            // Corruption: Reduces effective economic output.
            float corruptionFactor = Mathf.Clamp01(1.0f - d.CorruptionLevel);
            ecoScore *= corruptionFactor;

            // --- 2. MILITARY (Projected Power) ---
            // Active Soldiers: Actual army size.
            float militaryScore = d.Soldiers * 25.0f;

            // Potential Manpower: Capacity to draft. Fairer than current manpower which depletes.
            militaryScore += d.ManpowerMax * 5.0f;

            // Army Quality: Modifiers from tech/traits/laws.
            float armyQuality = 1.0f + d.MilitaryAttackModifier;
            militaryScore *= armyQuality;

            // Leader Bonus: King's Warfare stat.
            if (k.king != null)
            {
                militaryScore += k.king.stats["warfare"] * 50.0f;
            }

            // War Exhaustion: Reduces readiness.
            float wePenalty = 1.0f - (d.WarExhaustion * 0.005f);
            militaryScore *= Mathf.Clamp(wePenalty, 0.5f, 1.0f);

            // --- 3. CIVIC (Development & Stability) ---
            // Population & Urbanization
            float civicScore = d.Population * 2.0f;
            civicScore += d.Cities * 200.0f;
            civicScore += d.Buildings * 5.0f;

            // Technology: Estimated via modifiers
            float techScore = d.ResearchOutputModifier * 500.0f;
            civicScore += techScore;

            // Stability: Use Target Equilibrium (instant) instead of Current (drifted) for fairness.
            float targetStability = 50f + d.StabilityTargetModifier;
            float stabilityMult = targetStability / 50.0f;
            civicScore *= Mathf.Clamp(stabilityMult, 0.1f, 2.5f);

            // King's Civic Skills
            if (k.king != null)
            {
                civicScore += k.king.stats["stewardship"] * 20.0f;
                civicScore += k.king.stats["diplomacy"] * 10.0f;
                civicScore += k.king.stats["intelligence"] * 10.0f;
            }

            if(k == Main.selectedKingdom) 
            {   
                // Make it harder for player to reach top ranks
                ecoScore *= 0.5f;
                militaryScore *= 0.5f;
                civicScore *= 0.5f;
            }

            return ecoScore + militaryScore + civicScore;
        }

        private static void CreateHeader(Transform parent, string title)
        {
            var headerObj = new GameObject("Header");
            headerObj.transform.SetParent(parent, false);
            var le = headerObj.AddComponent<LayoutElement>();
            le.preferredHeight = 10f;
            le.minHeight = 10f;

            var txt = CreateText(headerObj.transform, title, 11, FontStyle.Bold, Color.white);
            txt.alignment = TextAnchor.MiddleCenter;
            var rt = txt.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Text CreateText(Transform parent, string content, int size, FontStyle style, Color col)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var txt = go.AddComponent<Text>();
            txt.font = Resources.Load<Font>("Fonts/Roboto-Regular") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = content;
            txt.fontSize = size;
            txt.fontStyle = style;
            txt.color = col;
            txt.raycastTarget = false;
            txt.resizeTextForBestFit = false;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Truncate;
            return txt;
        }

        private static string FormatNumber(float num)
        {
            if (num >= 1000000) return (num / 1000000f).ToString("0.#") + "M";
            if (num >= 1000) return (num / 1000f).ToString("0.#") + "k";
            return num.ToString("0");
        }

        private class RankingData
        {
            public Kingdom kingdom;
            public float score;
        }
    }
}