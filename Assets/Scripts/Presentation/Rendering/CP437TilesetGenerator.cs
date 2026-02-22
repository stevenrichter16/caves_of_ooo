using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Generates a CP437 tileset at runtime using Unity's built-in font rendering.
    /// CP437 is the classic IBM PC / DOS codepage with 256 glyphs.
    /// Each glyph is rendered as a white character on transparent background,
    /// then tinted by the renderer using Qud color codes.
    /// </summary>
    public static class CP437TilesetGenerator
    {
        public const int GlyphSize = 16;
        public const int Columns = 16;
        public const int Rows = 16;

        private static Dictionary<char, Tile> _tileCache;
        private static Texture2D _atlasTexture;

        /// <summary>
        /// Get a Tile for a specific character. Creates the tileset on first call.
        /// </summary>
        public static Tile GetTile(char c)
        {
            if (_tileCache == null)
                GenerateTileset();

            if (_tileCache.TryGetValue(c, out Tile tile))
                return tile;

            // Fallback to '?' for unknown characters
            _tileCache.TryGetValue('?', out tile);
            return tile;
        }

        /// <summary>
        /// Get or create the full glyph atlas texture.
        /// </summary>
        public static Texture2D GetAtlasTexture()
        {
            if (_atlasTexture == null)
                GenerateTileset();
            return _atlasTexture;
        }

        /// <summary>
        /// Clear the cached tileset (for reloading).
        /// </summary>
        public static void ClearCache()
        {
            _tileCache = null;
            _atlasTexture = null;
        }

        private static void GenerateTileset()
        {
            _tileCache = new Dictionary<char, Tile>(256);

            int texWidth = Columns * GlyphSize;
            int texHeight = Rows * GlyphSize;
            _atlasTexture = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
            _atlasTexture.filterMode = FilterMode.Point;
            _atlasTexture.wrapMode = TextureWrapMode.Clamp;

            // Clear to transparent
            Color[] clearPixels = new Color[texWidth * texHeight];
            _atlasTexture.SetPixels(clearPixels);

            // Render each ASCII printable character as a glyph
            // We use a simple bitmap font approach for the key roguelike characters
            RenderGlyphs();

            _atlasTexture.Apply();

            // Create individual tiles from the atlas
            for (int i = 0; i < 256; i++)
            {
                char c = (char)i;
                int col = i % Columns;
                int row = Rows - 1 - (i / Columns); // Flip Y for Unity

                Rect spriteRect = new Rect(col * GlyphSize, row * GlyphSize, GlyphSize, GlyphSize);
                Vector2 pivot = new Vector2(0.5f, 0.5f);

                Sprite sprite = Sprite.Create(_atlasTexture, spriteRect, pivot, GlyphSize);
                sprite.name = $"CP437_{i:X2}";

                Tile tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.color = Color.white;
                tile.name = sprite.name;

                _tileCache[c] = tile;
            }
        }

        /// <summary>
        /// Renders key roguelike characters into the atlas texture using bitmap patterns.
        /// Each glyph is a 16x16 white-on-transparent bitmap.
        /// </summary>
        private static void RenderGlyphs()
        {
            // First: generate full alphabet + punctuation as proper letter glyphs
            GenerateLetterGlyphs();

            // Then: override specific characters with game-specific glyphs.
            // These take precedence over the letter forms.
            DrawChar('@', new[] {  // Player
                "..XXXX..",
                ".X....X.",
                "X.X..X.X",
                "X......X",
                "X.X..X.X",
                "X..XX..X",
                ".X....X.",
                "..XXXX.."
            });
            DrawChar('#', new[] {  // Wall
                ".X..X...",
                ".X..X...",
                "XXXXXXXX",
                ".X..X...",
                ".X..X...",
                "XXXXXXXX",
                ".X..X...",
                ".X..X..."
            });
            DrawChar('.', new[] {  // Floor
                "........",
                "........",
                "........",
                "........",
                "........",
                "...XX...",
                "...XX...",
                "........"
            });
            DrawChar('/', new[] {  // Weapon (dagger, sword)
                "......X.",
                ".....X..",
                "....X...",
                "...X....",
                "..X.....",
                ".X......",
                "X.......",
                "........"
            });
            DrawChar('s', new[] {  // Snapjaw
                "..XXXX..",
                ".X....X.",
                "X......X",
                ".XXXXXX.",
                "X......X",
                "X......X",
                ".XXXXXX.",
                "........"
            });
            DrawChar('?', new[] {  // Unknown
                "..XXXX..",
                ".X....X.",
                "......X.",
                ".....X..",
                "...XX...",
                "........",
                "...XX...",
                "...XX..."
            });
            DrawChar('>', new[] {  // Stairs down
                "X.......",
                "XX......",
                "XXX.....",
                "XXXX....",
                "XXX.....",
                "XX......",
                "X.......",
                "........"
            });
            DrawChar('<', new[] {  // Stairs up
                ".......X",
                "......XX",
                ".....XXX",
                "....XXXX",
                ".....XXX",
                "......XX",
                ".......X",
                "........"
            });
            DrawChar('+', new[] {  // Door/cross
                "...XX...",
                "...XX...",
                "XXXXXXXX",
                "XXXXXXXX",
                "...XX...",
                "...XX...",
                "........",
                "........"
            });
            DrawChar('-', new[] {  // Horizontal line
                "........",
                "........",
                "........",
                "XXXXXXXX",
                "XXXXXXXX",
                "........",
                "........",
                "........"
            });
            DrawChar('|', new[] {  // Vertical line
                "...XX...",
                "...XX...",
                "...XX...",
                "...XX...",
                "...XX...",
                "...XX...",
                "...XX...",
                "...XX..."
            });
            DrawChar('~', new[] {  // Water
                "........",
                "........",
                ".XX..XX.",
                "X..XX..X",
                "........",
                "........",
                "........",
                "........"
            });
            DrawChar16('T', new[] {  // Tree
                "................",
                "......XXXX......",
                "....XXXXXXXX....",
                "...XXXXXXXXXX...",
                "..XXXXXXXXXXXX..",
                "..XXXXX.XXXXXX..",
                "..XXXXXXXXXXXX..",
                "...XXXXXXXXXX...",
                "....XXXXXXXX....",
                ".....XXXXXX.....",
                ".......XX.......",
                ".......XX.......",
                ".......XX.......",
                ".......XX.......",
                "......XXXX......",
                "................"
            });
        }

        private static void DrawChar(char c, string[] pattern)
        {
            int charIndex = (int)c;
            int col = charIndex % Columns;
            int row = Rows - 1 - (charIndex / Columns);

            int baseX = col * GlyphSize;
            int baseY = row * GlyphSize;

            for (int py = 0; py < 8 && py < pattern.Length; py++)
            {
                for (int px = 0; px < 8 && px < pattern[py].Length; px++)
                {
                    if (pattern[py][px] == 'X')
                    {
                        // Scale 2x
                        int tx = baseX + px * 2;
                        int ty = baseY + (7 - py) * 2;
                        _atlasTexture.SetPixel(tx, ty, Color.white);
                        _atlasTexture.SetPixel(tx + 1, ty, Color.white);
                        _atlasTexture.SetPixel(tx, ty + 1, Color.white);
                        _atlasTexture.SetPixel(tx + 1, ty + 1, Color.white);
                    }
                }
            }
        }

        /// <summary>
        /// Draw a character glyph from a full 16x16 pattern (no scaling).
        /// Each row is a 16-character string where 'X' = white pixel.
        /// </summary>
        private static void DrawChar16(char c, string[] pattern)
        {
            int charIndex = (int)c;
            int col = charIndex % Columns;
            int row = Rows - 1 - (charIndex / Columns);

            int baseX = col * GlyphSize;
            int baseY = row * GlyphSize;

            for (int py = 0; py < 16 && py < pattern.Length; py++)
            {
                for (int px = 0; px < 16 && px < pattern[py].Length; px++)
                {
                    if (pattern[py][px] == 'X')
                    {
                        int tx = baseX + px;
                        int ty = baseY + (15 - py);
                        _atlasTexture.SetPixel(tx, ty, Color.white);
                    }
                }
            }
        }

        private static void GenerateLetterGlyphs()
        {
            var font = GetFontData();
            foreach (var kvp in font)
            {
                DrawFromHex(kvp.Key, kvp.Value);
            }
        }

        /// <summary>
        /// Draw a character glyph from hex-encoded row data.
        /// Each pair of hex chars = one 8-pixel row, MSB = leftmost pixel.
        /// Scaled 2x to fill the 16x16 glyph cell.
        /// </summary>
        private static void DrawFromHex(char c, string hexRows)
        {
            int charIndex = (int)c;
            int col = charIndex % Columns;
            int row = Rows - 1 - (charIndex / Columns);

            int baseX = col * GlyphSize;
            int baseY = row * GlyphSize;

            int rowCount = hexRows.Length / 2;
            for (int py = 0; py < rowCount && py < 8; py++)
            {
                int hi = HexVal(hexRows[py * 2]);
                int lo = HexVal(hexRows[py * 2 + 1]);
                int rowBits = (hi << 4) | lo;

                for (int px = 0; px < 8; px++)
                {
                    if ((rowBits & (0x80 >> px)) != 0)
                    {
                        // Scale 2x
                        int tx = baseX + px * 2;
                        int ty = baseY + (7 - py) * 2;
                        _atlasTexture.SetPixel(tx, ty, Color.white);
                        _atlasTexture.SetPixel(tx + 1, ty, Color.white);
                        _atlasTexture.SetPixel(tx, ty + 1, Color.white);
                        _atlasTexture.SetPixel(tx + 1, ty + 1, Color.white);
                    }
                }
            }
        }

        private static int HexVal(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            return 0;
        }

        // --- Narrow text tiles for message log (8×16 pixels, 0.5×1.0 units) ---

        public const int TextGlyphW = 8;
        public const int TextGlyphH = 16;

        private static Dictionary<char, Tile> _textTileCache;
        private static Texture2D _textAtlasTexture;

        /// <summary>
        /// Get a narrow text tile for message rendering.
        /// Half the width of game tiles for a thinner, smaller look.
        /// </summary>
        public static Tile GetTextTile(char c)
        {
            if (_textTileCache == null)
                GenerateTextTileset();

            if (_textTileCache.TryGetValue(c, out Tile tile))
                return tile;

            _textTileCache.TryGetValue('?', out tile);
            return tile;
        }

        private static void GenerateTextTileset()
        {
            _textTileCache = new Dictionary<char, Tile>(256);

            int texW = Columns * TextGlyphW;
            int texH = Rows * TextGlyphH;
            _textAtlasTexture = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
            _textAtlasTexture.filterMode = FilterMode.Point;
            _textAtlasTexture.wrapMode = TextureWrapMode.Clamp;

            Color[] clear = new Color[texW * texH];
            _textAtlasTexture.SetPixels(clear);

            // Render the same hex font data at 1x horizontal, 2x vertical
            GenerateLetterGlyphsNarrow();

            _textAtlasTexture.Apply();

            for (int i = 0; i < 256; i++)
            {
                char ch = (char)i;
                int col = i % Columns;
                int row = Rows - 1 - (i / Columns);

                Rect rect = new Rect(col * TextGlyphW, row * TextGlyphH, TextGlyphW, TextGlyphH);
                // pixelsPerUnit = TextGlyphH → tile is 0.5 × 1.0 Unity units
                Sprite sprite = Sprite.Create(_textAtlasTexture, rect, new Vector2(0.5f, 0.5f), TextGlyphH);
                sprite.name = $"Text_{i:X2}";

                Tile tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.color = Color.white;
                tile.name = sprite.name;
                _textTileCache[ch] = tile;
            }
        }

        /// <summary>
        /// Render letter glyphs into the narrow text atlas (1x horiz, 2x vert).
        /// </summary>
        private static void GenerateLetterGlyphsNarrow()
        {
            var font = GetFontData();
            foreach (var kvp in font)
            {
                string hex = kvp.Value.Replace(" ", "");
                DrawFromHexNarrow(kvp.Key, hex);
            }
        }

        private static void DrawFromHexNarrow(char c, string hexRows)
        {
            int charIndex = (int)c;
            int col = charIndex % Columns;
            int row = Rows - 1 - (charIndex / Columns);

            int baseX = col * TextGlyphW;
            int baseY = row * TextGlyphH;

            int rowCount = hexRows.Length / 2;
            for (int py = 0; py < rowCount && py < 8; py++)
            {
                int hi = HexVal(hexRows[py * 2]);
                int lo = HexVal(hexRows[py * 2 + 1]);
                int rowBits = (hi << 4) | lo;

                for (int px = 0; px < 8; px++)
                {
                    if ((rowBits & (0x80 >> px)) != 0)
                    {
                        // 1x horizontal, 2x vertical
                        int tx = baseX + px;
                        int ty = baseY + (7 - py) * 2;
                        _textAtlasTexture.SetPixel(tx, ty, Color.white);
                        _textAtlasTexture.SetPixel(tx, ty + 1, Color.white);
                    }
                }
            }
        }

        /// <summary>
        /// Shared hex-encoded 8x8 font data used by both game and text tile generators.
        /// </summary>
        private static Dictionary<char, string> GetFontData()
        {
            return new Dictionary<char, string>
            {
                { ' ', "0000000000000000" },
                { '!', "183C3C1818001800" },
                { '"', "6C6C6C0000000000" },
                { '$', "183E603C067C1800" },
                { '%', "00C6CC1830660600" },
                { '&', "386C3876DCCC7600" },
                { '\'', "1818300000000000" },
                { '(', "0C18303030180C00" },
                { ')', "30180C0C0C183000" },
                { '*', "00663CFF3C660000" },
                { ',', "0000000000181830" },
                { '-', "000000FE00000000" },
                { '.', "0000000000183C18" },
                { ':', "0018180000181800" },
                { ';', "0018180000181830" },
                { '=', "00007E0000007E00" },
                { '[', "3C30303030303C00" },
                { ']', "3C0C0C0C0C0C3C00" },
                { '{', "0E18187018180E00" },
                { '}', "7018180E18187000" },
                { '^', "10386CC600000000" },
                { '_', "000000000000FE00" },
                { '`', "3018180000000000" },
                { '0', "3C66666E76663C00" },
                { '1', "1838181818183C00" },
                { '2', "3C66060C30607E00" },
                { '3', "3C66061C06663C00" },
                { '4', "0C1C3C6C7E0C0C00" },
                { '5', "7E607C0606663C00" },
                { '6', "3C60607C66663C00" },
                { '7', "7E060C1830303000" },
                { '8', "3C66663C66663C00" },
                { '9', "3C66663E06063C00" },
                { 'A', "183C66667E666600" },
                { 'B', "7C66667C66667C00" },
                { 'C', "3C66606060663C00" },
                { 'D', "786C6666666C7800" },
                { 'E', "7E60607860607E00" },
                { 'F', "7E60607860606000" },
                { 'G', "3C66606E66663E00" },
                { 'H', "6666667E66666600" },
                { 'I', "3C18181818183C00" },
                { 'J', "1E0C0C0C0CCC7800" },
                { 'K', "666C7870786C6600" },
                { 'L', "6060606060607E00" },
                { 'M', "C6EEFED6C6C6C600" },
                { 'N', "66767E7E6E666600" },
                { 'O', "3C66666666663C00" },
                { 'P', "7C66667C60606000" },
                { 'Q', "3C6666666A6C3600" },
                { 'R', "7C66667C6C666600" },
                { 'S', "3C66603C06663C00" },
                { 'T', "7E18181818181800" },
                { 'U', "6666666666663C00" },
                { 'V', "66666666663C1800" },
                { 'W', "C6C6C6D6FEEEC600" },
                { 'X', "66663C183C666600" },
                { 'Y', "6666663C18181800" },
                { 'Z', "7E060C1830607E00" },
                { 'a', "00003C063E663E00" },
                { 'b', "60607C6666667C00" },
                { 'c', "00003C6660663C00" },
                { 'd', "06063E6666663E00" },
                { 'e', "00003C667E603C00" },
                { 'f', "1C36307830303000" },
                { 'g', "00003E66663E063C" },
                { 'h', "60607C6666666600" },
                { 'i', "1800381818183C00" },
                { 'j', "060006060606063C" },
                { 'k', "6060666C786C6600" },
                { 'l', "3818181818183C00" },
                { 'm', "0000ECFED6C6C600" },
                { 'n', "00007C6666666600" },
                { 'o', "00003C6666663C00" },
                { 'p', "00007C66667C6060" },
                { 'q', "00003E66663E0606" },
                { 'r', "00007C6660606000" },
                { 's', "00003E603C067C00" },
                { 't', "30307C3030361C00" },
                { 'u', "0000666666663E00" },
                { 'v', "00006666663C1800" },
                { 'w', "0000C6C6D6FE6C00" },
                { 'x', "0000663C183C6600" },
                { 'y', "00006666663E063C" },
                { 'z', "00007E0C18307E00" },
            };
        }
    }
}
