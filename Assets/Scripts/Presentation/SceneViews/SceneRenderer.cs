using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// One cell in a SceneRenderer frame buffer. Glyph + foreground/background
    /// colors. Plain struct so it's cheap to copy and trivial to test.
    /// </summary>
    public struct SceneCell
    {
        public char Glyph;
        public Color Foreground;
        public Color Background;
    }

    /// <summary>
    /// Pure C# renderer that builds a frame buffer (grid of <see cref="SceneCell"/>)
    /// for a Scene View. Owns no MonoBehaviour state; testable in isolation.
    /// The Unity-side wrapper (<c>SceneViewUI</c>) reads <see cref="Frame"/>
    /// and writes it onto a Tilemap each frame.
    ///
    /// M2 ships hardcoded Campfire composition (logs, ground, tent, distant
    /// trees, stars, scene text, prompts, frozen-frame flame). M3 will add
    /// per-frame animation variation. M5 will refactor the hardcoded
    /// composition into <c>SceneViewData</c> assets.
    ///
    /// Plan: Docs/Plans/SCENE_VIEW_SYSTEM_IMPLEMENTATION_PLAN.md M2.
    /// Visual spec: Docs/Mockups/scene-views/campfire.html.
    /// </summary>
    public sealed class SceneRenderer
    {
        public readonly int Width;
        public readonly int Height;
        public readonly SceneCell[] Frame;

        public SceneRenderer(int width, int height)
        {
            Width = width;
            Height = height;
            Frame = new SceneCell[width * height];
        }

        /// <summary>
        /// Bounds-protected cell accessor. Returns a default (empty, black)
        /// cell on out-of-range indices instead of throwing.
        /// </summary>
        public SceneCell GetCell(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return default;
            return Frame[y * Width + x];
        }

        /// <summary>
        /// Render the Campfire scene's static composition into the frame
        /// buffer. M2: deterministic, no animation. M3 will introduce a
        /// time parameter for per-frame variation.
        /// </summary>
        public void RenderCampfire()
        {
            Clear();
            DrawSky();
            DrawDistantTrees();
            DrawTent();
            DrawGround();
            DrawLogs();
            DrawFlameStatic();
            DrawSceneText();
            DrawPrompts();
        }

        // ====================================================================
        // Composition layers — each writes into the frame buffer.
        // Layer order determines paint-over priority.
        // ====================================================================

        private static readonly Color SKY_COLOR    = new Color(0.04f, 0.04f, 0.10f);
        private static readonly Color STAR_DIM     = new Color(0.55f, 0.55f, 0.78f);
        private static readonly Color STAR_GOLD    = new Color(0.85f, 0.65f, 0.20f);
        private static readonly Color TREE_DISTANT = new Color(0.18f, 0.14f, 0.28f);
        private static readonly Color TENT_DARK    = new Color(0.10f, 0.07f, 0.06f);
        private static readonly Color TENT_FIRELIT = new Color(0.30f, 0.18f, 0.10f);
        private static readonly Color GROUND_COOL  = new Color(0.16f, 0.13f, 0.10f);
        private static readonly Color GROUND_LIT   = new Color(0.45f, 0.30f, 0.16f);
        private static readonly Color LOG_COLOR    = new Color(0.50f, 0.30f, 0.16f);
        private static readonly Color LOG_HIGHLIGHT= new Color(0.62f, 0.38f, 0.18f);
        private static readonly Color FLAME_BASE   = new Color(1.00f, 0.90f, 0.40f);
        private static readonly Color FLAME_MID    = new Color(1.00f, 0.55f, 0.18f);
        private static readonly Color FLAME_TIP    = new Color(0.85f, 0.30f, 0.10f);
        private static readonly Color FLAME_EMBER  = new Color(0.55f, 0.18f, 0.08f);
        private static readonly Color TEXT_AMBER   = new Color(0.78f, 0.55f, 0.25f);
        private static readonly Color PROMPT_AMBER = new Color(0.85f, 0.65f, 0.30f);
        private static readonly Color PROMPT_KEY   = new Color(1.00f, 0.85f, 0.45f);

        private void Clear()
        {
            for (int i = 0; i < Frame.Length; i++)
            {
                Frame[i].Glyph = ' ';
                Frame[i].Foreground = Color.black;
                Frame[i].Background = Color.black;
            }
        }

        private void Set(int x, int y, char glyph, Color fg)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return;
            Frame[y * Width + x].Glyph = glyph;
            Frame[y * Width + x].Foreground = fg;
        }

        // ---- Sky + stars ----
        // Stars are at fixed positions (rng-seeded once in M5; for M2 they're
        // deterministic constants).
        private static readonly (int x, int y, bool gold)[] STAR_POSITIONS = {
            (5,1,false),(12,2,true),(18,0,false),(23,3,false),(31,1,false),
            (37,2,false),(45,0,true),(52,3,false),(58,1,false),(63,2,false),
            (69,0,false),(74,3,true),(7,5,false),(15,6,false),(22,4,true),
            (28,7,false),(34,5,false),(41,7,false),(48,6,false),(55,4,false),
            (62,7,true),(67,5,false),(73,6,false),(78,4,false),(2,3,false),
            (10,7,false),(20,2,false),(50,1,false),(60,5,true),(75,7,false)
        };

        private void DrawSky()
        {
            // Sky background gradient (just sets background color rows; the
            // glyph stays ' ' until a star overrides).
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Frame[y * Width + x].Foreground = SKY_COLOR;
                }
            }
            // Stars
            foreach (var (sx, sy, gold) in STAR_POSITIONS)
            {
                Set(sx, sy, '.', gold ? STAR_GOLD : STAR_DIM);
            }
        }

        // ---- Distant trees (right side) ----
        private static readonly (int x, int y, int h)[] TREE_POSITIONS = {
            (60,16,3),(64,15,4),(68,16,3),(72,17,2),(75,16,3)
        };

        private void DrawDistantTrees()
        {
            foreach (var (tx, ty, th) in TREE_POSITIONS)
            {
                for (int dy = 0; dy < th; dy++)
                {
                    int y = ty - dy;
                    char ch = dy == th - 1 ? 'T' : '|';
                    Set(tx, y, ch, TREE_DISTANT);
                }
            }
        }

        // ---- Tent silhouette (left side) ----
        private static readonly string[] TENT_ART = {
            "    /\\    ",
            "   /  \\   ",
            "  /    \\  ",
            " /  /\\  \\ ",
            "/__/  \\__\\"
        };
        private const int TENT_X = 5;
        private const int TENT_Y = 14;

        private void DrawTent()
        {
            for (int row = 0; row < TENT_ART.Length; row++)
            {
                string line = TENT_ART[row];
                for (int col = 0; col < line.Length; col++)
                {
                    char c = line[col];
                    if (c == ' ') continue;
                    bool firelit = col >= line.Length - 2;
                    Set(TENT_X + col, TENT_Y + row, c,
                        firelit ? TENT_FIRELIT : TENT_DARK);
                }
            }
        }

        // ---- Ground line + firelight pool ----
        private const int GROUND_Y = 19;
        private const int FIRE_CX = 40;

        private void DrawGround()
        {
            for (int x = 0; x < Width; x++)
            {
                // Procedural variation: ~ - . by sine pattern
                float v = (Mathf.Sin(x * 0.4f) + Mathf.Sin(x * 0.17f + 1.3f)) / 2f;
                char ch = v > 0.4f ? '~' : (v > -0.2f ? '-' : '.');
                // Ground brightens near fire (firelight pool)
                int distFire = Mathf.Abs(x - FIRE_CX);
                float lit = Mathf.Max(0, 1 - distFire / 22f);
                Color color = Color.Lerp(GROUND_COOL, GROUND_LIT, lit);
                Set(x, GROUND_Y, ch, color);
            }
        }

        // ---- Logs (3 rows, anchor of the scene) ----
        private static readonly string[] LOGS_ART = {
            "  __[=][==[=====[==]==[=====]==][=]__  ",
            " /  | |   |     |   |     |   | |  \\  ",
            " \\__|_|___|_____|___|_____|___|_|__/  "
        };
        private const int LOG_TOP_Y = 16;

        private void DrawLogs()
        {
            int logsX = FIRE_CX - LOGS_ART[0].Length / 2;
            for (int row = 0; row < LOGS_ART.Length; row++)
            {
                string line = LOGS_ART[row];
                for (int col = 0; col < line.Length; col++)
                {
                    char c = line[col];
                    if (c == ' ') continue;
                    Color color = row == 0 ? LOG_HIGHLIGHT : LOG_COLOR;
                    Set(logsX + col, LOG_TOP_Y + row, c, color);
                }
            }
        }

        // ---- Flame (frozen one-frame composition; animation in M3) ----
        // Hardcoded glyphs picked to match a representative frame from the JS
        // prototype's flame draw. Per-cell intensity drives color choice.
        // Format per row: "row-offset-from-base char-array" where char-array
        // is the glyph pattern from -5 to +5 around FIRE_CX (length 11).
        private static readonly string[] FLAME_FRAME = {
            // dy=1 (just above logs) — densest core
            "  &#%@&%#&  ",
            // dy=2
            "   *&%@%&*   ",
            // dy=3
            "    *&%&*    ",
            // dy=4
            "     ^*^     ",
            // dy=5
            "      ,'      ",
            // dy=6 (top tip — sparse)
            "       .       "
        };

        private void DrawFlameStatic()
        {
            for (int dy = 0; dy < FLAME_FRAME.Length; dy++)
            {
                int y = LOG_TOP_Y - 1 - dy;
                if (y < 0) continue;
                string row = FLAME_FRAME[dy];
                int rowHalfWidth = row.Length / 2;
                for (int col = 0; col < row.Length; col++)
                {
                    char c = row[col];
                    if (c == ' ') continue;
                    int x = FIRE_CX - rowHalfWidth + col;
                    Color color =
                        dy <= 0 ? FLAME_BASE :
                        dy <= 2 ? FLAME_MID :
                        dy <= 4 ? FLAME_TIP : FLAME_EMBER;
                    Set(x, y, c, color);
                }
            }
        }

        // ---- Scene text (single line for M2; rotation comes in M3) ----
        private const string SCENE_TEXT =
            "\"The fire warms what your hands cannot reach.   The night, here, is only at your back.\"";
        private const int SCENE_TEXT_Y = 22;

        private void DrawSceneText()
        {
            int tx = Mathf.Max(0, (Width - SCENE_TEXT.Length) / 2);
            for (int i = 0; i < SCENE_TEXT.Length && tx + i < Width; i++)
            {
                char c = SCENE_TEXT[i];
                if (c == ' ') continue;
                Set(tx + i, SCENE_TEXT_Y, c, TEXT_AMBER);
            }
        }

        // ---- UI prompts (bottom row) ----
        private const string PROMPT_LINE =
            "[E] RETURN     [R] REST     [C] COOK     [T] TALK";

        private void DrawPrompts()
        {
            int promptY = Height - 2;
            int px = Mathf.Max(0, (Width - PROMPT_LINE.Length) / 2);
            for (int i = 0; i < PROMPT_LINE.Length && px + i < Width; i++)
            {
                char c = PROMPT_LINE[i];
                if (c == ' ') continue;

                // Highlight the bracket-letter (e.g. the 'E' in "[E]")
                bool isKeyLetter = i > 0 && i + 1 < PROMPT_LINE.Length &&
                                    PROMPT_LINE[i - 1] == '[' && PROMPT_LINE[i + 1] == ']';
                Color color = isKeyLetter ? PROMPT_KEY : PROMPT_AMBER;
                Set(px + i, promptY, c, color);
            }
        }
    }
}
