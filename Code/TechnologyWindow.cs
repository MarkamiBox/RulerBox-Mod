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

            // 1. Create Main Window Panel
            root = new GameObject("TechnologyWindow");
            root.transform.SetParent(parent, false);
            var rt = root.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.01f, 0.01f);
            rt.anchorMax = new Vector2(0.99f, 0.99f);
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            // Main Horizontal Layout
            var h = root.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.UpperLeft;
            h.spacing = 10;
            h.padding = new RectOffset(5, 5, 5, 5);
            h.childControlWidth = true;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false; // Fix: Don't force left panel to expand
            h.childForceExpandHeight = true;

            // --- LEFT PANEL (30%) ---
            leftPanel = new GameObject("LeftPanel");
            leftPanel.transform.SetParent(root.transform, false);
            var leftLe = leftPanel.AddComponent<LayoutElement>();
            // TWEAK HERE: Left Panel Width
            leftLe.flexibleWidth = 0f; // Don't grow
            leftLe.preferredWidth = 60; 
            // TWEAK HERE: Left Panel Height (0 = auto, 1 = fill)
            leftLe.flexibleHeight = 0f; 
            leftLe.preferredHeight = 50; // Fixed height logic if needed, or rely on content

            // Background
            var leftImg = leftPanel.AddComponent<Image>();
            leftImg.sprite = windowInnerSprite;
            leftImg.type = Image.Type.Sliced;
            leftImg.color = new Color(1f, 0.5f, 0.5f, 0.5f); // Reddish for testing

            var leftV = leftPanel.AddComponent<VerticalLayoutGroup>();
            leftV.childAlignment = TextAnchor.UpperCenter;
            leftV.spacing = 2;
            leftV.padding = new RectOffset(15, 15, 15, 15);
            leftV.childControlWidth = true;
            leftV.childControlHeight = true; // Control children height
            leftV.childForceExpandHeight = false; // Don't force stretch children


            // 1. "Current Research" Header
            CreateTextRow(leftPanel.transform, "Current:", Color.white, 6);

            // 2. Current Research Item (Button/Icon)
            // Identify what IS researching?
            // Currently simulated by TechnologyManager. But we don't have a specific "active" field exposed easily besides logic.
            // We'll calculate it in Refresh().
            GameObject currentResearchObj = new GameObject("CurrentResearchItem");
            currentResearchObj.transform.SetParent(leftPanel.transform, false);
            var crLe = currentResearchObj.AddComponent<LayoutElement>();
            crLe.preferredHeight = 120;
            currentResearchText = CreateText(currentResearchObj.transform, "Nothing", Color.yellow, 6);

            // Spacer
            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(leftPanel.transform, false);
            spacer.AddComponent<LayoutElement>().preferredHeight = 20;

            // 3. "Tech Points" Header
            CreateTextRow(leftPanel.transform, "Knowledge Points:", Color.white, 6);

            // 4. Tech Points Value
            techPowerText = CreateTextRow(leftPanel.transform, "0", Color.green, 6);

            // --- RIGHT PANEL (Rest) ---
            rightPanel = new GameObject("RightPanel");
            rightPanel.transform.SetParent(root.transform, false);
            var rightLe = rightPanel.AddComponent<LayoutElement>();
            rightLe.flexibleWidth = 1f;
            rightLe.flexibleHeight = 1f;

            // Scroll View for Tech Tree
            GameObject scrollContainer = new GameObject("ScrollContainer");
            scrollContainer.transform.SetParent(rightPanel.transform, false);
            var scrollRt = scrollContainer.AddComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero; scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = Vector2.zero; scrollRt.offsetMax = Vector2.zero;

            var scrollBg = scrollContainer.AddComponent<Image>();
            scrollBg.color = new Color(0, 0, 0, 0.2f);

            var scrollRect = scrollContainer.AddComponent<ScrollRect>();
            
            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollContainer.transform, false);
            var viewRt = viewport.AddComponent<RectTransform>();
            viewRt.anchorMin = Vector2.zero; viewRt.anchorMax = Vector2.one;
            viewRt.sizeDelta = Vector2.zero;
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            viewport.AddComponent<Image>().color = new Color(1,1,1,0.01f);

            // Content
            contentContainer = new GameObject("Content");
            contentContainer.transform.SetParent(viewport.transform, false);
            var contentRt = contentContainer.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(0, 1);
            contentRt.pivot = new Vector2(0, 1);
            contentRt.sizeDelta = new Vector2(2000, 2000); 

            scrollRect.content = contentRt;
            scrollRect.viewport = viewRt;
            scrollRect.horizontal = true;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 25f;

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
            // Clear existing
            foreach(Transform child in contentContainer.transform)
            {
                GameObject.Destroy(child.gameObject);
            }

            int x = 0;
            int y = 0;
            int cellSize = 110;
            int padding = 30;
            int cols = 8;
            
            var lib = AssetManager.knowledge_library;
            if (lib == null) return;

            foreach(var asset in lib.list)
            {
                if (!asset.show_in_knowledge_window) continue;
                
                bool isUnlocked = TechnologyManager.IsTechUnlockable(asset.id);
                CreateTechNode(asset, x * (cellSize + padding) + padding, -y * (cellSize + padding) - padding, isUnlocked);
                
                x++;
                if (x >= cols)
                {
                    x = 0;
                    y++;
                }
            }
            
            var rt = contentContainer.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(cols * (cellSize + padding) + padding, (y + 1) * (cellSize + padding) + padding);
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
