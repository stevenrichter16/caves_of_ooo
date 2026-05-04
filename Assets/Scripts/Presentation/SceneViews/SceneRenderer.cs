using System.Collections.Generic;
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
    /// The Unity-side wrapper (<c>SceneViewUI</c>) advances animation by calling
    /// <see cref="Tick"/> each frame, then calls <see cref="RenderCampfire"/> to
    /// paint into <see cref="Frame"/>, which is then written onto a Tilemap.
    ///
    /// M2 shipped hardcoded static composition (logs, ground, tent, distant
    /// trees, prompts, scene text). M3 adds per-frame animation: probabilistic
    /// flame glyphs, spark particles, star twinkle, occasional crackles, wind
    /// gusts. All animation is RNG-driven and seeded — same (seed, tick count)
    /// always produces the same Frame.
    ///
    /// Plan: Docs/Plans/SCENE_VIEW_SYSTEM_IMPLEMENTATION_PLAN.md M3
    /// Visual spec: Docs/Mockups/scene-views/campfire.html (the JS prototype is
    /// the authoritative spec — anything ambiguous in the C# port, the JS wins).
    /// </summary>
    public sealed class SceneRenderer
    {
        public readonly int Width;
        public readonly int Height;
        public readonly SceneCell[] Frame;
        public readonly int Seed;

        // Read-only animation state. Tests use these to verify Tick behavior;
        // future HUD wiring (e.g. "FLAME: CRACKLING") can also read them.
        public float Time => _t;
        public int SparkCount => _sparks.Count;
        public float CrackleLevel => _crackleLevel;
        public float WindGust => _windGust;

        // M4 dissolve transition. Forward dissolve: mask transitions all-1
        // (pre-scene, world below shows through cleared overlay) to all-0
        // (scene visible). Reverse: 0 → 1.
        public const float DISSOLVE_DURATION = 1.6f;
        public bool IsDissolving { get; private set; }
        public bool DissolveIsReverse { get; private set; }
        public float DissolveProgress { get; private set; }

        private const int DEFAULT_SEED = 12345;
        private const int STAR_COUNT = 60;
        private const int SKY_BOTTOM = 8;

        private readonly System.Random _rng;
        private readonly Star[] _stars;
        private readonly List<Spark> _sparks = new List<Spark>(128);
        private readonly float[] _mask;

        private float _t;
        private float _crackleLevel;
        private float _windGust;
        private float _nextCrackle;
        private float _nextGust;
        private float _dissolveElapsed;

        public SceneRenderer(int width, int height) : this(width, height, DEFAULT_SEED) { }

        public SceneRenderer(int width, int height, int seed)
        {
            Width = width;
            Height = height;
            Seed = seed;
            Frame = new SceneCell[width * height];
            _rng = new System.Random(seed);
            _stars = new Star[STAR_COUNT];
            _mask = new float[width * height];
            InitStars();
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
        /// Advance one tick of animation state — increments time, decays
        /// crackle/wind, ages sparks, fires periodic crackle/gust events,
        /// spawns sparks per JS spawn rate, advances star twinkle phases.
        /// Does NOT touch the frame buffer; <see cref="RenderCampfire"/>
        /// reads the resulting state to draw.
        /// </summary>
        public void Tick(float deltaTime)
        {
            _t += deltaTime;

            if (_t > _nextCrackle)
            {
                _crackleLevel = 0.7f + (float)_rng.NextDouble() * 0.3f;
                _nextCrackle = _t + 3f + (float)_rng.NextDouble() * 4f;
                int burst = 4 + (int)(_rng.NextDouble() * 5);
                for (int i = 0; i < burst; i++) SpawnSpark(_crackleLevel + 0.4f);
            }
            _crackleLevel *= 0.92f;

            if (_t > _nextGust)
            {
                _windGust = ((float)_rng.NextDouble() - 0.5f) * 1.5f;
                _nextGust = _t + 4f + (float)_rng.NextDouble() * 5f;
            }
            _windGust *= 0.96f;

            if (_rng.NextDouble() < 0.4 + _crackleLevel * 0.4f)
                SpawnSpark(1f + _crackleLevel);

            for (int i = 0; i < _sparks.Count; i++)
            {
                var s = _sparks[i];
                s.X += s.Vx;
                s.Y += s.Vy;
                s.Vy *= 0.985f;
                s.Age++;
                _sparks[i] = s;
            }
            for (int i = _sparks.Count - 1; i >= 0; i--)
                if (_sparks[i].Age >= _sparks[i].Max)
                    _sparks.RemoveAt(i);

            for (int i = 0; i < _stars.Length; i++)
                _stars[i].Phase += _stars[i].Rate;
        }

        /// <summary>
        /// Render the Campfire scene into the frame buffer using current
        /// animation state. Pure: reads state, writes Frame, no state mutation
        /// other than RNG draws for the per-cell flame and ember glow.
        /// </summary>
        public void RenderCampfire()
        {
            Clear();
            DrawSky();
            DrawDistantTrees();
            DrawTent();
            DrawGround();
            DrawLogs();
            DrawFlameAnimated();
            DrawSparks();
            DrawSceneText();
            DrawPrompts();
            // Dissolve composition pass — only runs while a transition is
            // active. Overlay clears (Glyph=' ') cells where mask is high
            // so the world tilemap below can show through; soft-edge cells
            // darken the scene glyph for a fade.
            if (IsDissolving) DrawDissolveOverlay();
        }

        /// <summary>
        /// Begin a dissolve transition. <paramref name="reverse"/>=false is
        /// the entry transition (mask 1 → 0, world being covered by scene).
        /// reverse=true is the exit transition (mask 0 → 1, scene being
        /// uncovered to reveal the world). Resets elapsed time to 0; mask
        /// is initialized so a render before the first <see cref="UpdateDissolve"/>
        /// call still composes coherently (forward init=1, reverse init=0).
        /// </summary>
        public void StartDissolve(bool reverse = false)
        {
            IsDissolving = true;
            DissolveIsReverse = reverse;
            DissolveProgress = 0f;
            _dissolveElapsed = 0f;
            float init = reverse ? 0f : 1f;
            for (int i = 0; i < _mask.Length; i++) _mask[i] = init;
        }

        /// <summary>
        /// Advance the dissolve transition by <paramref name="deltaTime"/>
        /// seconds. Recomputes the mask field from the radial reveal radius.
        /// Once <see cref="DissolveProgress"/> reaches 1, clears
        /// <see cref="IsDissolving"/> — subsequent calls are no-ops.
        /// </summary>
        public void UpdateDissolve(float deltaTime)
        {
            if (!IsDissolving) return;
            _dissolveElapsed += deltaTime;
            DissolveProgress = Mathf.Clamp01(_dissolveElapsed / DISSOLVE_DURATION);

            // JS: cxx = W/2, cyy = H/2 + 2 (offset down so the iris feels
            // like it opens around the campfire/composition center, not
            // dead-center of the canvas).
            float cxx = Width * 0.5f;
            float cyy = Height * 0.5f + 2f;
            float maxR = Mathf.Sqrt(cxx * cxx + cyy * cyy);

            // Reverse runs the iris backward — at p=0 it's fully open
            // (effProgress=1), at p=1 it's fully closed (effProgress=0).
            float effProgress = DissolveIsReverse
                ? 1f - DissolveProgress
                : DissolveProgress;
            float reveal = effProgress * maxR * 1.2f;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    float dx = x - cxx;
                    float dy = y - cyy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float edge = reveal - d;
                    float m;
                    if (edge > 1f) m = 0f;             // inside revealed area
                    else if (edge > 0f) m = 1f - edge; // soft edge fade
                    else m = 1f;                        // outside reveal
                    _mask[y * Width + x] = m;
                }
            }

            if (DissolveProgress >= 1f) IsDissolving = false;
        }

        /// <summary>
        /// Bounds-protected mask accessor. Returns 0 (scene visible) for
        /// out-of-range coords rather than throwing.
        /// </summary>
        public float GetMask(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return 0f;
            return _mask[y * Width + x];
        }

        private void DrawDissolveOverlay()
        {
            // Mask thresholds:
            //   m > 0.5  → fully cleared (Glyph='\0' — transparent so world below shows)
            //   m > 0.05 → soft edge: probabilistic clear OR darkened scene
            //   else     → leave scene cell as drawn
            //
            // Sentinel: '\0' marks "intentionally transparent for dissolve",
            // distinct from ' ' which marks "scene-blank but should occlude
            // the world below". SceneViewUI.RenderToTilemap respects this
            // distinction by clearing the tile only for '\0' and painting an
            // opaque background tile for ' '.
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    float m = _mask[y * Width + x];
                    int idx = y * Width + x;
                    if (m > 0.5f)
                    {
                        Frame[idx].Glyph = '\0';
                        Frame[idx].Foreground = Color.black;
                    }
                    else if (m > 0.05f)
                    {
                        if (_rng.NextDouble() < m)
                        {
                            Frame[idx].Glyph = '\0';
                            Frame[idx].Foreground = Color.black;
                        }
                        else
                        {
                            Frame[idx].Foreground *= (1f - m);
                        }
                    }
                }
            }
        }

        // ====================================================================
        // Composition layers
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
        private static readonly Color LOG_EMBER    = new Color(1.00f, 0.45f, 0.10f);
        private static readonly Color FLAME_BASE   = new Color(1.00f, 0.90f, 0.40f);
        private static readonly Color FLAME_MID    = new Color(1.00f, 0.55f, 0.18f);
        private static readonly Color FLAME_TIP    = new Color(0.85f, 0.30f, 0.10f);
        private static readonly Color FLAME_EMBER  = new Color(0.55f, 0.18f, 0.08f);
        private static readonly Color SPARK_HOT    = new Color(1.00f, 0.85f, 0.30f);
        private static readonly Color SPARK_COOL   = new Color(0.55f, 0.18f, 0.05f);
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

        private struct Star
        {
            public float X, Y, Phase, Rate, Bright;
            public bool Gold;
        }

        private void InitStars()
        {
            // 60 stars, x in [0,W), y in [0,SKY_BOTTOM), random twinkle phase/
            // rate/brightness, 6% gold. NextDouble() call order matches JS so
            // the seeded sequence parallels the prototype's intent.
            for (int i = 0; i < STAR_COUNT; i++)
            {
                _stars[i] = new Star
                {
                    X      = (float)_rng.NextDouble() * Width,
                    Y      = (float)_rng.NextDouble() * SKY_BOTTOM,
                    Phase  = (float)_rng.NextDouble() * Mathf.PI * 2f,
                    Rate   = 0.025f + (float)_rng.NextDouble() * 0.04f,
                    Bright = 0.4f + (float)_rng.NextDouble() * 0.6f,
                    Gold   = _rng.NextDouble() < 0.06,
                };
            }
        }

        private void DrawSky()
        {
            for (int y = 0; y < SKY_BOTTOM; y++)
            {
                float yNorm = y / (float)SKY_BOTTOM;
                Color skyColor = Color.Lerp(
                    SKY_COLOR,
                    new Color(0.05f, 0.05f, 0.13f),
                    yNorm);
                for (int x = 0; x < Width; x++)
                    Frame[y * Width + x].Foreground = skyColor;
            }
            for (int i = 0; i < _stars.Length; i++)
            {
                var s = _stars[i];
                float tw = (Mathf.Sin(s.Phase) + 1f) * 0.5f;
                char ch =
                    tw > 0.85f ? '*' :
                    tw > 0.55f ? '+' :
                    tw > 0.30f ? '\'' :
                    '.';
                int sx = (int)s.X;
                int sy = (int)s.Y;
                float blend = Mathf.Clamp01(0.25f + tw * s.Bright * 0.5f);
                Color baseC = s.Gold ? STAR_GOLD : STAR_DIM;
                Color starC = Color.Lerp(SKY_COLOR, baseC, blend);
                Set(sx, sy, ch, starC);
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
                float v = (Mathf.Sin(x * 0.4f) + Mathf.Sin(x * 0.17f + 1.3f)) / 2f;
                char ch = v > 0.4f ? '~' : (v > -0.2f ? '-' : '.');
                int distFire = Mathf.Abs(x - FIRE_CX);
                float lit = Mathf.Max(0f, 1f - distFire / 22f);
                float flicker = Mathf.Sin(_t * 5f + x * 0.4f) * lit * 0.04f;
                Color color = Color.Lerp(GROUND_COOL, GROUND_LIT,
                    Mathf.Clamp01(lit + flicker));
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
            // Random ember glow on a single log cell (JS: ~30% per frame).
            if (_rng.NextDouble() < 0.3)
            {
                int erow = (int)(_rng.NextDouble() * LOGS_ART.Length);
                int ecol = (int)(_rng.NextDouble() * LOGS_ART[erow].Length);
                if (erow < LOGS_ART.Length && ecol < LOGS_ART[erow].Length &&
                    LOGS_ART[erow][ecol] != ' ')
                {
                    int ex = logsX + ecol;
                    int ey = LOG_TOP_Y + erow;
                    if (ex >= 0 && ex < Width && ey >= 0 && ey < Height)
                        Frame[ey * Width + ex].Foreground = LOG_EMBER;
                }
            }
        }

        // ---- Flame (probabilistic per-cell glyph + intensity-weighted color +
        //      wind-gust lateral skew + crackle-driven boost) ----

        private const int FLAME_HEIGHT = 6;
        private const int FLAME_HALF_WIDTH = 5;

        private void DrawFlameAnimated()
        {
            for (int dy = 0; dy < FLAME_HEIGHT; dy++)
            {
                int y = LOG_TOP_Y - 1 - dy;
                if (y < 0 || y >= Height) continue;
                for (int dx = -FLAME_HALF_WIDTH; dx <= FLAME_HALF_WIDTH; dx++)
                {
                    float skew = _windGust * dy * 0.4f;
                    int x = Mathf.RoundToInt(FIRE_CX + dx + skew);
                    if (x < 0 || x >= Width) continue;
                    float heightFactor = dy / (float)FLAME_HEIGHT;
                    float distFromCenter = Mathf.Abs(dx) / (float)FLAME_HALF_WIDTH;
                    float widthAtHeight = 1f - heightFactor * 0.6f;
                    if (distFromCenter / Mathf.Max(0.2f, widthAtHeight) > 1f) continue;
                    float intensity = (1f - heightFactor * 0.85f) * (1f - distFromCenter * 0.6f);
                    intensity *= 1f + _crackleLevel * 0.4f;
                    intensity += (Mathf.Sin(_t * 4f + x * 0.7f + y * 1.1f) + 1f) * 0.08f;
                    char ch = FlameChar(intensity);
                    if (ch == ' ') continue;
                    Set(x, y, ch, FlameColor(dy, intensity));
                }
            }
        }

        // Probabilistic glyph per intensity tier — direct port of JS flameChar.
        private char FlameChar(float intensity)
        {
            if (intensity < 0.05f) return ' ';
            double r = _rng.NextDouble();
            if (intensity > 0.85f)
            {
                if (r < 0.30) return '@';
                if (r < 0.60) return '#';
                if (r < 0.85) return '%';
                return '&';
            }
            if (intensity > 0.60f)
            {
                if (r < 0.20) return '#';
                if (r < 0.50) return '%';
                if (r < 0.80) return '&';
                return '*';
            }
            if (intensity > 0.35f)
            {
                if (r < 0.30) return '&';
                if (r < 0.60) return '*';
                if (r < 0.85) return '^';
                return ',';
            }
            if (intensity > 0.15f)
            {
                if (r < 0.40) return '^';
                if (r < 0.70) return '*';
                if (r < 0.90) return ',';
                return '\'';
            }
            if (r < 0.50) return '\'';
            if (r < 0.80) return ',';
            return '.';
        }

        // Color picked by height-from-base tier with intensity + per-cell jitter.
        // JS uses HSL; we map to the existing FLAME_* RGB constants by scaling
        // brightness, which keeps color-space consistency with the rest of the
        // composition.
        private Color FlameColor(int heightFromBase, float intensity)
        {
            Color baseC =
                heightFromBase < 1 ? FLAME_BASE :
                heightFromBase < 3 ? FLAME_MID  :
                heightFromBase < 5 ? FLAME_TIP  :
                                     FLAME_EMBER;
            float jitter = 0.85f + (float)_rng.NextDouble() * 0.30f;
            float intensityFactor = 0.55f + Mathf.Clamp01(intensity) * 0.45f;
            float scale = Mathf.Clamp01(jitter * intensityFactor);
            return baseC * scale;
        }

        // ---- Sparks ----

        private struct Spark
        {
            public float X, Y, Vx, Vy;
            public int Age, Max;
        }

        private void SpawnSpark(float intensity)
        {
            _sparks.Add(new Spark
            {
                X   = FIRE_CX - 2.5f + (float)_rng.NextDouble() * 5f,
                Y   = LOG_TOP_Y - 1 - (float)_rng.NextDouble() * 1.5f,
                Vx  = ((float)_rng.NextDouble() - 0.5f) * 0.18f,
                Vy  = -0.20f - (float)_rng.NextDouble() * 0.14f * intensity,
                Age = 0,
                Max = 14 + (int)(_rng.NextDouble() * 10 * intensity),
            });
        }

        private void DrawSparks()
        {
            for (int i = 0; i < _sparks.Count; i++)
            {
                var s = _sparks[i];
                int x = (int)s.X;
                int y = (int)s.Y;
                if (x < 0 || x >= Width || y < 0 || y >= Height) continue;
                // Max is always ≥ 14 from SpawnSpark, so no zero-check needed.
                float fade = 1f - (s.Age / (float)s.Max);
                char ch =
                    fade > 0.7f ? '*' :
                    fade > 0.4f ? '+' :
                    fade > 0.2f ? '\'' :
                                  '.';
                Color color = Color.Lerp(SPARK_COOL, SPARK_HOT, Mathf.Clamp01(fade));
                Set(x, y, ch, color);
            }
        }

        // ---- Scene text ----
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

                bool isKeyLetter = i > 0 && i + 1 < PROMPT_LINE.Length &&
                                    PROMPT_LINE[i - 1] == '[' && PROMPT_LINE[i + 1] == ']';
                Color color = isKeyLetter ? PROMPT_KEY : PROMPT_AMBER;
                Set(px + i, promptY, c, color);
            }
        }
    }
}
