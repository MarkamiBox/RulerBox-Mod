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
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.5f);

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
            scrollBg.color = new Color(0, 0, 0, 0.3f);

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
            
            // Content Layout (Horizontal rows of vertical bars)
            var hGroup = content.AddComponent<HorizontalLayoutGroup>();
            hGroup.childAlignment = TextAnchor.LowerLeft;
            hGroup.spacing = 10;
            hGroup.padding = new RectOffset(5, 5, 5, 5);
            hGroup.childControlHeight = true;
            hGroup.childControlWidth = false;
            hGroup.childForceExpandHeight = true;
            hGroup.childForceExpandWidth = false;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            scrollRect.viewport = vpRT;
            scrollRect.content = (RectTransform)scrollContent;

            root.SetActive(false);
        }

        public static void SetVisible(bool visible)
        {
            if (root == null) return;
            root.SetActive(visible);
            if (visible)
            {
                Refresh();
            }
        }

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
            var col = new GameObject($"Rank_{rank}_{data.kingdom.data.name}");
            col.transform.SetParent(scrollContent, false);
            
            var le = col.AddComponent<LayoutElement>();
            le.preferredWidth = 100f;
            le.minWidth = 100f;

            var vGroup = col.AddComponent<VerticalLayoutGroup>();
            vGroup.spacing = 5;
            vGroup.childControlHeight = false; // We control bar height manually
            vGroup.childForceExpandHeight = false;
            vGroup.childAlignment = TextAnchor.LowerCenter;

            // 1. Score Text (Top)
            var scoreTxt = CreateText(col.transform, FormatNumber(data.score), 10, FontStyle.Normal, Color.yellow);
            scoreTxt.alignment = TextAnchor.LowerCenter;
            var scoreRT = scoreTxt.GetComponent<RectTransform>();
            scoreRT.sizeDelta = new Vector2(100, 20);

            // 2. Bar Graph (Middle)
            // Normalized height based on max score (max height ~ 200 units?)
            float fillAmount = (maxScore > 0) ? (data.score / maxScore) : 0;
            float barHeight = Mathf.Max(10f, fillAmount * 250f); // Min height 10, Max 250

            var barObj = new GameObject("Bar");
            barObj.transform.SetParent(col.transform, false);
            var barImg = barObj.AddComponent<Image>();
            
            // Determine Color
            if (data.kingdom == Main.selectedKingdom) barImg.color = BarColorPlayer;
            else if (Main.selectedKingdom != null && data.kingdom.isEnemy(Main.selectedKingdom)) barImg.color = BarColorEnemy;
            else barImg.color = BarColorNeutral;

            var barRT = barObj.GetComponent<RectTransform>();
            barRT.sizeDelta = new Vector2(40, barHeight); // Width 40

            // 3. Flag (Bottom)
            var flagContainer = new GameObject("Flag");
            flagContainer.transform.SetParent(col.transform, false);
            var flagRT = flagContainer.AddComponent<RectTransform>();
            flagRT.sizeDelta = new Vector2(40, 40);

            // Flag Background
            var flagBg = new GameObject("FlagBG").AddComponent<Image>();
            flagBg.transform.SetParent(flagContainer.transform, false);
            flagBg.sprite = data.kingdom.getElementBackground();
            flagBg.color = data.kingdom.kingdomColor.getColorMain32();
            flagBg.rectTransform.sizeDelta = new Vector2(40, 40);

            // Flag Icon
            var flagIcon = new GameObject("FlagIcon").AddComponent<Image>();
            flagIcon.transform.SetParent(flagContainer.transform, false);
            flagIcon.sprite = data.kingdom.getElementIcon();
            flagIcon.color = data.kingdom.kingdomColor.getColorBanner();
            flagIcon.rectTransform.sizeDelta = new Vector2(40, 40);

            // 4. Rank & Name (Bottomest)
            string nameStr = $"#{rank}\n{data.kingdom.data.name}";
            var nameTxt = CreateText(col.transform, nameStr, 11, FontStyle.Bold, Color.white);
            nameTxt.alignment = TextAnchor.UpperCenter;
            nameTxt.rectTransform.sizeDelta = new Vector2(100, 40);
        }

        private static float CalculatePowerScore(Kingdom k)
        {
            float score = 0f;

            // 1. Army Strength (High Weight)
            int warriors = k.countTotalWarriors();
            score += warriors * 5.0f;

            // 2. Population (Medium Weight)
            int pop = k.getPopulationPeople();
            score += pop * 1.0f;

            // 3. Buildings (Development)
            if (k.cities != null)
            {
                foreach (var city in k.cities)
                {
                    if (city.buildings != null)
                        score += city.buildings.Count * 2.0f;
                }
            }

            // 4. Resources & GDP Estimation
            // Player kingdom has detailed GDP, AI doesn't always update fully in Metrics.
            // We approximate GDP: (Pop * Base) + (Warriors * Upkeep) + (TechLevel * Bonus)
            float estimatedGDP = (pop * 10f) + (warriors * 5f);
            
            // Check if we have specific metrics available
            if (KingdomMetricsSystem.db.TryGetValue(k, out var data))
            {
                // Use cached income if available and recent
                estimatedGDP = data.Income;
            }
            
            score += estimatedGDP * 0.1f; // Weight GDP lower as it's a big number

            return score;
        }

        private static void CreateHeader(Transform parent, string title)
        {
            var headerObj = new GameObject("Header");
            headerObj.transform.SetParent(parent, false);
            var le = headerObj.AddComponent<LayoutElement>();
            le.preferredHeight = 20f;
            le.minHeight = 20f;

            var txt = CreateText(headerObj.transform, title, 5, FontStyle.Bold, Color.white);
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
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
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