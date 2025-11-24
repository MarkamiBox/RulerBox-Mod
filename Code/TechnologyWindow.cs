using UnityEngine;
using UnityEngine.UI;

namespace RulerBox
{
    public static class TechnologyWindow
    {
        private static GameObject root;

        public static void Initialize(Transform parent)
        {
            if (root != null) return;
            root = new GameObject("TechnologyRoot");
            root.transform.SetParent(parent, false);
            var rt = root.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            var t = root.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.text = "Technology Window\n(Work In Progress)";
            t.alignment = TextAnchor.MiddleCenter;

            root.SetActive(false);
        }

        public static void SetVisible(bool v) => root?.SetActive(v);
        public static bool IsVisible() => root != null && root.activeSelf;
        public static void Refresh(Kingdom k) { /* TODO */ }
    }
}