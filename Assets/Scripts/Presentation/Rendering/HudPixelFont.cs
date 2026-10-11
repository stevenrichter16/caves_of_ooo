using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>HUD-only original5×7 pixel glyphs. Fixed95 ASCII cache; no world/menu atlas mutation.</summary>
    public static class HudPixelFont
    {
        const int CellWidth = 6, CellHeight = 10, Columns = 16;
        static Texture2D atlas, primitives;
        static readonly char[] PrimitiveCharacters = { (char)0xDA, (char)0xBF, (char)0xC0, (char)0xD9, (char)0xC4, (char)0xB3, (char)0xC3, (char)0xB4, (char)0xC2, (char)0xC1, (char)0xDB, (char)0xCD };
        static Tile[] tiles;
        static readonly Dictionary<char, string> Rows = new Dictionary<char, string>
        {
            { (char)32, "00000000000000" },
            { (char)33, "04040404040004" },
            { (char)34, "0A0A0A00000000" },
            { (char)35, "0A0A1F0A1F0A0A" },
            { (char)36, "040F140E051E04" },
            { (char)37, "19190204081313" },
            { (char)38, "0C12140C15120D" },
            { (char)39, "04040800000000" },
            { (char)40, "02040808080402" },
            { (char)41, "08040202020408" },
            { (char)42, "00150E1F0E1500" },
            { (char)43, "0004041F040400" },
            { (char)44, "00000000000408" },
            { (char)45, "0000001F000000" },
            { (char)46, "00000000000004" },
            { (char)47, "01010204081010" },
            { (char)48, "0E11131519110E" },
            { (char)49, "040C040404040E" },
            { (char)50, "0E11010204081F" },
            { (char)51, "1E01010601011E" },
            { (char)52, "02060A121F0202" },
            { (char)53, "1F101E0101110E" },
            { (char)54, "0610101E11110E" },
            { (char)55, "1F010204080808" },
            { (char)56, "0E11110E11110E" },
            { (char)57, "0E11110F01020C" },
            { (char)58, "00000400000400" },
            { (char)59, "00000400000408" },
            { (char)60, "01020408040201" },
            { (char)61, "00001F001F0000" },
            { (char)62, "10080402040810" },
            { (char)63, "0E110102040004" },
            { (char)64, "0E11171517100F" },
            { (char)65, "0E11111F111111" },
            { (char)66, "1E11111E11111E" },
            { (char)67, "0E11101010110E" },
            { (char)68, "1E11111111111E" },
            { (char)69, "1F10101E10101F" },
            { (char)70, "1F10101E101010" },
            { (char)71, "0E11101711110F" },
            { (char)72, "1111111F111111" },
            { (char)73, "0E04040404040E" },
            { (char)74, "0702020212120C" },
            { (char)75, "11121418141211" },
            { (char)76, "1010101010101F" },
            { (char)77, "111B1515111111" },
            { (char)78, "11191513111111" },
            { (char)79, "0E11111111110E" },
            { (char)80, "1E11111E101010" },
            { (char)81, "0E11111115120D" },
            { (char)82, "1E11111E141211" },
            { (char)83, "0F10100E01011E" },
            { (char)84, "1F040404040404" },
            { (char)85, "1111111111110E" },
            { (char)86, "11111111110A04" },
            { (char)87, "11111115151B11" },
            { (char)88, "11110A040A1111" },
            { (char)89, "11110A04040404" },
            { (char)90, "1F01020408101F" },
            { (char)91, "0E08080808080E" },
            { (char)92, "10100804020101" },
            { (char)93, "0E02020202020E" },
            { (char)94, "040A1100000000" },
            { (char)95, "0000000000001F" },
            { (char)96, "08040200000000" },
            { (char)97, "00000E010F110F" },
            { (char)98, "10101E1111111E" },
            { (char)99, "00000E1110110E" },
            { (char)100, "01010F1111110F" },
            { (char)101, "00000E111F100E" },
            { (char)102, "0609081C080808" },
            { (char)103, "000F11110F010E" },
            { (char)104, "10101E11111111" },
            { (char)105, "04000C0404040E" },
            { (char)106, "0200060202120C" },
            { (char)107, "101011121C1211" },
            { (char)108, "0C04040404040E" },
            { (char)109, "00001A15151515" },
            { (char)110, "00001E11111111" },
            { (char)111, "00000E1111110E" },
            { (char)112, "001E11111E1010" },
            { (char)113, "000F11110F0101" },
            { (char)114, "00001619101010" },
            { (char)115, "00000F100E011E" },
            { (char)116, "08081C08080906" },
            { (char)117, "0000111111130D" },
            { (char)118, "00001111110A04" },
            { (char)119, "0000111515150A" },
            { (char)120, "0000110A040A11" },
            { (char)121, "001111110F010E" },
            { (char)122, "00001F0204081F" },
            { (char)123, "02040408040402" },
            { (char)124, "04040404040404" },
            { (char)125, "08040402040408" },
            { (char)126, "00000A15000000" }
        };

        public static Tile GetTile(char c)
        {
            c = Cp437.Map(c);
            bool primitive = c >= 128 && System.Array.IndexOf(PrimitiveCharacters, c) >= 0;
            if (!primitive && (c < 32 || c > 126)) c = '?';
            if (atlas == null || primitives == null || tiles == null || tiles['?'] == null || tiles['?'].sprite == null) Build();
            if (tiles[c] == null || tiles[c].sprite == null) Build();
            return tiles[c];
        }

        static void Build()
        {
            Release();
            atlas = new Texture2D(Columns * CellWidth, 6 * CellHeight, TextureFormat.RGBA32, false)
                { name = "HudPixelFont", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            atlas.SetPixels(new Color[atlas.width * atlas.height]);
            tiles = new Tile[256];
            for (int c = 32; c <= 126; c++)
            {
                int index = c - 32, x = index % Columns * CellWidth, y = index / Columns * CellHeight;
                string bits = Rows[(char)c];
                for (int row = 0; row < 7; row++)
                {
                    int mask = System.Convert.ToInt32(bits.Substring(row * 2, 2), 16);
                    for (int column = 0; column < 5; column++)
                        if ((mask & (1 << (4 - column))) != 0) atlas.SetPixel(x + column, y + 8 - row, Color.white);
                }
                var sprite = Sprite.Create(atlas, new Rect(x, y, CellWidth, CellHeight), new Vector2(.5f, .5f), 12);
                sprite.name = "Text_" + c.ToString("X2"); sprite.hideFlags = HideFlags.HideAndDontSave;
                var tile = ScriptableObject.CreateInstance<Tile>(); tile.name = sprite.name; tile.hideFlags = HideFlags.HideAndDontSave;
                tile.sprite = sprite; tile.color = Color.white;
                // Same scale in both axes: glyph pixels stay square; ink fits .5-unit sidebar advance.
                tile.transform = Matrix4x4.Scale(new Vector3(1.1f, 1.1f, 1));
                tiles[c] = tile;
            }
            atlas.Apply(false, false);
            // The legacy narrow text atlas allocates these glyphs but never paints
            // their pixels. Own the bounded HUD primitives without changing that
            // shared world/menu cache. Their .5x1 geometry matches the old grid.
            primitives = new Texture2D(PrimitiveCharacters.Length * 8, 16, TextureFormat.RGBA32, false)
                { name = "HudPrimitives", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            var pixels = new Color[primitives.width * primitives.height];
            for (int i = 0; i < PrimitiveCharacters.Length; i++)
            {
                char c = PrimitiveCharacters[i];
                for (int y = 0; y < 16; y++) for (int x = 0; x < 8; x++)
                    if (PrimitivePixel(c, x, y)) pixels[y * primitives.width + i * 8 + x] = Color.white;
            }
            primitives.SetPixels(pixels); primitives.Apply(false, false);
            for (int i = 0; i < PrimitiveCharacters.Length; i++)
            {
                char c = PrimitiveCharacters[i];
                var sprite = Sprite.Create(primitives, new Rect(i * 8, 0, 8, 16), new Vector2(.5f, .5f), 16);
                sprite.name = "Text_" + ((int)c).ToString("X2"); sprite.hideFlags = HideFlags.HideAndDontSave;
                var tile = ScriptableObject.CreateInstance<Tile>(); tile.name = sprite.name; tile.hideFlags = HideFlags.HideAndDontSave;
                tile.sprite = sprite; tile.color = Color.white; tiles[c] = tile;
            }
        }

        static bool PrimitivePixel(char c, int x, int y)
        {
            if (c == (char)0xDB) return true;
            if (c == (char)0xCD) return y == 6 || y == 10;
            bool left = c == (char)0xC4 || c == (char)0xBF || c == (char)0xD9 || c == (char)0xB4 || c == (char)0xC2 || c == (char)0xC1;
            bool right = c == (char)0xC4 || c == (char)0xDA || c == (char)0xC0 || c == (char)0xC3 || c == (char)0xC2 || c == (char)0xC1;
            bool up = c == (char)0xB3 || c == (char)0xC0 || c == (char)0xD9 || c == (char)0xC3 || c == (char)0xB4 || c == (char)0xC1;
            bool down = c == (char)0xB3 || c == (char)0xDA || c == (char)0xBF || c == (char)0xC3 || c == (char)0xB4 || c == (char)0xC2;
            return y == 8 && ((left && x <= 4) || (right && x >= 4))
                || x == 4 && ((up && y >= 8) || (down && y <= 8));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Release()
        {
            if (tiles != null) foreach (var tile in tiles)
                if (tile != null) { Destroy(tile.sprite); Destroy(tile); }
            Destroy(atlas); Destroy(primitives); tiles = null; atlas = null; primitives = null;
        }
        static void Destroy(Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value);
        }
    }
}
