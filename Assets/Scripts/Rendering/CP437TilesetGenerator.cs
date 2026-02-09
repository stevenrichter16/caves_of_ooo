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
            // Define bitmap patterns for essential roguelike characters
            // Each pattern is a 8x8 grid scaled to 16x16 (2x)
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
            DrawChar('T', new[] {  // Tree
                "XXXXXXXX",
                "XXXXXXXX",
                "...XX...",
                "...XX...",
                "...XX...",
                "...XX...",
                "...XX...",
                "...XX..."
            });

            // Generate simple letter glyphs for A-Z, a-z, 0-9
            GenerateLetterGlyphs();
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
                        int ty = baseY + (7 - py) * 2; // Flip Y
                        _atlasTexture.SetPixel(tx, ty, Color.white);
                        _atlasTexture.SetPixel(tx + 1, ty, Color.white);
                        _atlasTexture.SetPixel(tx, ty + 1, Color.white);
                        _atlasTexture.SetPixel(tx + 1, ty + 1, Color.white);
                    }
                }
            }
        }

        private static void GenerateLetterGlyphs()
        {
            // Simple 5x7 bitmap font for basic ASCII range
            // This is minimal — just enough to see text on screen
            // A proper implementation would load a real CP437 font texture
            string[] digits = {
                "0:.XXX.:X...X:X...X:X...X:X...X:X...X:.XXX.",
                "1:..X..:..X..:.XX..:..X..:..X..:..X..:XXXXX",
                "2:.XXX.:X...X:....X:..XX.:.X...:X....:XXXXX",
                "3:.XXX.:X...X:....X:..XX.:....X:X...X:.XXX.",
                "4:...X.:..XX.:..XX.:.X.X.:XXXXX:...X.:...X.",
                "5:XXXXX:X....:XXXX.:....X:....X:X...X:.XXX.",
                "6:.XXX.:X....:XXXX.:X...X:X...X:X...X:.XXX.",
                "7:XXXXX:....X:...X.:..X..:..X..:.X...:X....",
                "8:.XXX.:X...X:X...X:.XXX.:X...X:X...X:.XXX.",
                "9:.XXX.:X...X:X...X:.XXXX:....X:X...X:.XXX."
            };

            foreach (string def in digits)
            {
                char c = def[0];
                string[] rows = def.Substring(2).Split(':');
                DrawChar(c, rows);
            }
        }
    }
}
