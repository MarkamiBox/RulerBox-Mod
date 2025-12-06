using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using NCMS.Utils; 
using System.IO;

namespace RulerBox
{
    public static class TechnologyWindow
    {
        private static GameObject root;
        private static GameObject leftPanel;
        private static GameObject rightPanel;
        private static GameObject contentContainer;
        private static Text techPowerText;
        private static Text currentResearchText;
        private static Sprite windowInnerSprite;

        public static void Initialize(Transform parent)
        {
            if (root != null) return;
            
            // Load resources
            windowInnerSprite = Mod.EmbededResources.LoadSprite("RulerBox.Resources.UI.windowInnerSliced.png");

            // Create Main Window Panel
            root = new GameObject("TechnologyWindow");
            root.transform.SetParent(parent, false);
            var rt = root.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.01f, 0.01f);
            rt.anchorMax = new Vector2(0.99f, 0.99f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            // Background
            var img = root.AddComponent<Image>();
            if (windowInnerSprite != null) {
                img.sprite = windowInnerSprite;
                img.type = Image.Type.Sliced;
            }
            img.color = Color.white;

            // Coming Soon Text
            var textObj = new GameObject("ComingSoonText");
            textObj.transform.SetParent(root.transform, false);
            var txt = textObj.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = "(Coming Soon)";
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.resizeTextForBestFit = true;
            txt.resizeTextMinSize = 20;
            txt.resizeTextMaxSize = 40;
            
            var txtRt = textObj.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; 
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            root.SetActive(false);
        }

        public static void SetVisible(bool v)
        {
             if (root != null) 
             {
                 root.SetActive(v);
                 if (v) RefreshContent();
             }
        }

        public static bool IsVisible() => root != null && root.activeSelf;

        public static void Refresh(Kingdom k)
        {
            if (k == null || k.getCulture() == null) return;
            var culture = k.getCulture();

            // 1. Tech Points
            float progress = 0f;
            var type = culture.data.GetType();
            var f_progress = type.GetField("knowledge_progress", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (f_progress != null)
            {
                progress = (float)f_progress.GetValue(culture.data);
            }
            if(techPowerText != null) techPowerText.text = ((int)progress).ToString();

            // 2. Current Research
            // Use reflection to find 'knowledge_type' (current researching tech ID)
            string currentTech = "";
            var f_type = type.GetField("knowledge_type", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if(f_type != null) currentTech = (string)f_type.GetValue(culture.data);

            if(currentResearchText != null)
            {
                currentResearchText.text = string.IsNullOrEmpty(currentTech) ? "None" : currentTech.Replace("_", " ");
            }
        }

        private static void RefreshContent()
        {
            // Placeholder: Do nothing
        }

        private static void CreateTechNode(KnowledgeAsset asset, int x, int y, bool isUnlocked)
        {
            GameObject node = new GameObject("TechNode_" + asset.id);
            node.transform.SetParent(contentContainer.transform, false);
            var rt = node.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(100, 100);

            var img = node.AddComponent<Image>();
            if (windowInnerSprite != null) {
                img.sprite = windowInnerSprite;
                img.type = Image.Type.Sliced;
            }
            img.color = isUnlocked ? new Color(0.2f, 0.2f, 0.2f, 1f) : new Color(0.3f, 0.1f, 0.1f, 1f); 

            // Icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(node.transform, false);
            var iconRt = iconObj.AddComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.5f, 0.7f); iconRt.anchorMax = new Vector2(0.5f, 0.7f);
            iconRt.sizeDelta = new Vector2(40, 40);
            var iconImg = iconObj.AddComponent<Image>();
            iconImg.sprite = asset.getIcon();
            
            // Label
            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(node.transform, false);
            var textRt = textObj.AddComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0.05f, 0.05f); textRt.anchorMax = new Vector2(0.95f, 0.4f);
            var txt = textObj.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = asset.id.Replace("_", " "); 
            txt.resizeTextForBestFit = true;
            txt.resizeTextMinSize = 6; 
            txt.resizeTextMaxSize = 8;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = isUnlocked ? Color.white : Color.gray;
        }

        private static Text CreateTextRow(Transform parent, string content, Color color, int fontSize)
        {
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(parent, false);
            var txt = textObj.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = content;
            txt.color = color;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontSize = fontSize;
            return txt;
        }
        
        private static Text CreateText(Transform parent, string content, Color color, int fontSize)
        {
             var txt = parent.gameObject.AddComponent<Text>();
             txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
             txt.text = content;
             txt.color = color;
             txt.alignment = TextAnchor.MiddleCenter;
             txt.fontSize = fontSize;
             return txt;
        }
    }
}
