using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Rendering
{
    /// <summary>One original cached card sprite. The renderer scales its four-by-five
    /// world-unit rectangle to the existing eight-column card width. The one-pixel
    /// inset separates neighboring cards; border and dark fill share a state tint.</summary>
    public static class HudCardFrame
    {
        static Texture2D texture; static Tile tile;
        public static Tile GetTile()
        {
            if (texture == null || tile == null || tile.sprite == null) Build();
            return tile;
        }
        static void Build()
        {
            Release();
            texture = new Texture2D(64, 80, TextureFormat.RGBA32, false)
                { name = "HudCardFrame", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            var pixels = new Color[64 * 80]; var fill = new Color(.07f, .09f, .11f, 1);
            for (int y = 1; y < 79; y++) for (int x = 1; x < 63; x++)
                pixels[y * 64 + x] = x == 1 || x == 62 || y == 1 || y == 78 ? Color.white : fill;
            texture.SetPixels(pixels); texture.Apply(false, false);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 64, 80), new Vector2(.5f, .5f), 16);
            sprite.name = "HudCardFrame"; sprite.hideFlags = HideFlags.HideAndDontSave;
            tile = ScriptableObject.CreateInstance<Tile>(); tile.name = "HudCardFrame"; tile.sprite = sprite;
            tile.color = Color.white; tile.hideFlags = HideFlags.HideAndDontSave;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Release()
        {
            if (tile != null) { Destroy(tile.sprite); Destroy(tile); }
            Destroy(texture); tile = null; texture = null;
        }
        static void Destroy(Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value);
        }
    }
}
