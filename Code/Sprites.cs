using UnityEngine;
using System.IO;

namespace RulerBox
{
    public static class Sprites
    {
        public static Sprite GetSprite(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogError($"[RulerBox] Image not found: {path}");
                return null;
            }

            byte[] data = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            tex.LoadImage(data);
            tex.filterMode = FilterMode.Point; // Pixel art style usually
            
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
