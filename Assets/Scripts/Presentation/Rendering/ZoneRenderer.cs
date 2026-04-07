using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// MonoBehaviour that renders a Zone onto a Unity Tilemap.
    /// World rendering is ASCII-only: the top visible entity contributes a single
    /// CP437 glyph from RenderString and a foreground tint from ColorString.
    /// 
    /// This is the only component that bridges the pure C# simulation
    /// to Unity's rendering. Attach to a GameObject with a Tilemap + Grid.
    /// </summary>
    [RequireComponent(typeof(Tilemap))]
    public class ZoneRenderer : MonoBehaviour
    {
        /// <summary>
        /// The zone currently being rendered.
        /// </summary>
        public Zone CurrentZone { get; private set; }

        /// <summary>
        /// Background tone for visually blank cells.
        /// </summary>
        public Color BackgroundColor = new Color(0.05f, 0.05f, 0.05f);
        public Color UnexploredColor = new Color(0.15f, 0.15f, 0.18f, 0.75f);

        /// <summary>
        /// When true, zone rendering is suppressed (e.g. inventory screen is open).
        /// </summary>
        public bool Paused;

        /// <summary>
        /// The player entity. Used for field-of-view calculations.
        /// </summary>
        public Entity PlayerEntity;

        /// <summary>
        /// FOV radius in tiles from the player.
        /// </summary>
        public int FovRadius = 999;

        /// <summary>
        /// Number of message log lines shown at the bottom of the screen.
        /// </summary>
        public int MessageLineCount = 4;

        /// <summary>
        /// Reference orthographic size at which message text appears at 1:1 scale.
        /// At other zoom levels, message text scales proportionally.
        /// </summary>
        public float MessageReferenceZoom = 20f;

        private Tilemap _tilemap;
        private Tilemap _bgTilemap;
        private Tilemap _fxTilemap;
        private Tilemap _msgBgTilemap;
        private Tilemap _msgTilemap;
        private Tilemap _lookTilemap;
        private Transform _msgGridTransform;
        private AsciiFxRenderer _asciiFxRenderer;
        private CampfireEmberRenderer _campfireEmberRenderer;
        private WorldCursorRenderer _worldCursorRenderer;
        private LookOverlayRenderer _lookOverlayRenderer;
        private bool _dirty = true;
        private int _lastMessageCount = -1;
        private float _lastMessageTime;
        private int _lastFlashStamp;
        private float _flashUntil;
        private const float LogIdleStart = 8f;     // seconds idle before fade starts
        private const float LogIdleEnd = 12f;      // fully faded at this idle duration
        private const float LogIdleAlphaFloor = 0.35f;
        private const float FlashDuration = 0.3f;
        private bool _messagesDirty = true;
        private float _prevIdleFadeAlpha = -1f;
        private bool _wasPaused;
        private float _ambientTimer;
        private float _dustMoteSpawnTimer;
        private const float DustMoteSpawnInterval = 3.5f;
        private Camera _mainCamera;
        private LightMap _lightMap;
        private readonly List<Vector2Int> _waterTilePositions = new List<Vector2Int>();
        private readonly HashSet<string> _loggedRenderIssues = new HashSet<string>();
        private WorldCursorState _worldCursorState;
        private Entity _cursorPlayer;
        private LookSnapshot _currentLookSnapshot;
        private Vector3 _lastCameraPosition;
        private float _lastCameraSize = -1f;
        private float _lastCameraAspect = -1f;

        public bool HasBlockingFx => _asciiFxRenderer?.HasBlockingFx ?? false;

        /// <summary>
        /// Exposed background tilemap (sortingOrder -1). UI overlays like DialogueUI
        /// can draw opaque bg fills here — they'll show through cleared cells on the
        /// main tilemap and sit behind foreground glyphs.
        /// </summary>
        public Tilemap BgTilemap => _bgTilemap;
        private Tilemap _popupBgTilemap;
        private Tilemap _popupFgTilemap;
        /// <summary>Popup background tilemap (sortingOrder 6). For DialogueUI/TradeUI bg fills.</summary>
        public Tilemap PopupBgTilemap => _popupBgTilemap;
        /// <summary>Popup foreground tilemap (sortingOrder 7). For DialogueUI/TradeUI glyphs.</summary>
        public Tilemap PopupFgTilemap => _popupFgTilemap;

        private void Awake()
        {
            _lastMessageTime = Time.time;
            _lastFlashStamp = MessageLog.FlashStamp;
            _mainCamera = Camera.main;
            _tilemap = GetComponent<Tilemap>();

            Grid grid = GetComponentInParent<Grid>();
            Transform gridParent = grid != null ? grid.transform : (transform.parent != null ? transform.parent : transform);

            // Background tilemap: solid color blocks behind foreground glyphs.
            // Same grid, same cell size — just a lower sorting order.
            var bgTilemapObj = new GameObject("BgTilemap");
            bgTilemapObj.transform.SetParent(gridParent, false);
            _bgTilemap = bgTilemapObj.AddComponent<Tilemap>();
            var bgRenderer = bgTilemapObj.AddComponent<TilemapRenderer>();
            bgRenderer.sortingOrder = -1; // below world tilemap

            var fxTilemapObj = new GameObject("FxTilemap");
            fxTilemapObj.transform.SetParent(gridParent, false);
            _fxTilemap = fxTilemapObj.AddComponent<Tilemap>();
            var fxRenderer = fxTilemapObj.AddComponent<TilemapRenderer>();
            fxRenderer.sortingOrder = 1; // above world, below messages
            _asciiFxRenderer = new AsciiFxRenderer(_fxTilemap);

            var emberObj = new GameObject("CampfireEmbers");
            emberObj.transform.SetParent(gridParent, false);
            _campfireEmberRenderer = emberObj.AddComponent<CampfireEmberRenderer>();

            // Create a separate tilemap for messages with narrow half-width cells
            var msgGridObj = new GameObject("MessageGrid");
            _msgGridTransform = msgGridObj.transform;
            var msgGrid = msgGridObj.AddComponent<Grid>();
            msgGrid.cellSize = new Vector3(0.5f, 1f, 0f);

            var msgBgObj = new GameObject("MessageBgTilemap");
            msgBgObj.transform.SetParent(msgGridObj.transform);
            _msgBgTilemap = msgBgObj.AddComponent<Tilemap>();
            var msgBgRenderer = msgBgObj.AddComponent<TilemapRenderer>();
            msgBgRenderer.sortingOrder = 2; // background behind message text

            var msgTmObj = new GameObject("MessageTilemap");
            msgTmObj.transform.SetParent(msgGridObj.transform);
            _msgTilemap = msgTmObj.AddComponent<Tilemap>();
            var msgRenderer = msgTmObj.AddComponent<TilemapRenderer>();
            msgRenderer.sortingOrder = 3; // text above background

            var lookTmObj = new GameObject("LookOverlayTilemap");
            lookTmObj.transform.SetParent(msgGridObj.transform);
            _lookTilemap = lookTmObj.AddComponent<Tilemap>();
            var lookRenderer = lookTmObj.AddComponent<TilemapRenderer>();
            lookRenderer.sortingOrder = 5;

            _worldCursorRenderer = new WorldCursorRenderer(gridParent, _tilemap);
            _lookOverlayRenderer = new LookOverlayRenderer(_lookTilemap, _msgGridTransform, MessageReferenceZoom);

            // Dedicated tilemaps for dialogue/popup UI — must sort ABOVE the
            // message log (order 3) and look overlay (order 5) so popups aren't
            // hidden behind the world log.
            var popupBgObj = new GameObject("PopupBgTilemap");
            popupBgObj.transform.SetParent(gridParent, false);
            _popupBgTilemap = popupBgObj.AddComponent<Tilemap>();
            var popupBgRenderer = popupBgObj.AddComponent<TilemapRenderer>();
            popupBgRenderer.sortingOrder = 6;

            var popupFgObj = new GameObject("PopupFgTilemap");
            popupFgObj.transform.SetParent(gridParent, false);
            _popupFgTilemap = popupFgObj.AddComponent<Tilemap>();
            var popupFgRenderer = popupFgObj.AddComponent<TilemapRenderer>();
            popupFgRenderer.sortingOrder = 7;
        }

        /// <summary>
        /// Set the zone to render. Triggers a full redraw.
        /// </summary>
        public void SetZone(Zone zone)
        {
            CurrentZone = zone;
            _asciiFxRenderer?.SetZone(zone);
            _worldCursorRenderer?.SetZone(zone);
            _lookOverlayRenderer?.Clear();
            _currentLookSnapshot = null;
            _worldCursorState = null;
            _cursorPlayer = null;
            _dirty = true;
            RefreshWaterCache();

            // Register campfire positions for free-floating ember rendering
            if (_campfireEmberRenderer != null)
            {
                _campfireEmberRenderer.SetZone(zone);
                if (zone != null)
                {
                    foreach (var entity in zone.GetAllEntities())
                    {
                        if (entity.GetPart<CampfirePart>() != null)
                        {
                            var cell = zone.GetEntityCell(entity);
                            if (cell != null)
                                _campfireEmberRenderer.RegisterCampfire(entity, cell.X, cell.Y);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Mark the display as needing a refresh.
        /// Call this after entities move, are added/removed, etc.
        /// </summary>
        public void MarkDirty()
        {
            _dirty = true;
            _messagesDirty = true;
        }

        private void LateUpdate()
        {
            bool newMessages = MessageLog.Count != _lastMessageCount;
            if (newMessages)
            {
                _lastMessageTime = Time.time;
                _messagesDirty = true;
            }
            if (MessageLog.FlashStamp != _lastFlashStamp)
            {
                _lastFlashStamp = MessageLog.FlashStamp;
                _flashUntil = Time.time + FlashDuration;
                _messagesDirty = true;
            }
            Camera cam = _mainCamera;
            bool cameraChanged = HasCameraViewChanged(cam);
            if (cameraChanged)
                _messagesDirty = true;

            // Check if idle-fade alpha changed (drives continuous fade-out)
            float idle = Time.time - _lastMessageTime;
            float idleFadeAlpha = Mathf.Lerp(
                1f, LogIdleAlphaFloor,
                Mathf.InverseLerp(LogIdleStart, LogIdleEnd, idle));
            if (!Mathf.Approximately(idleFadeAlpha, _prevIdleFadeAlpha))
                _messagesDirty = true;
            // Flash also drives per-frame changes while active
            if (Time.time < _flashUntil)
                _messagesDirty = true;

            _asciiFxRenderer?.Update(Time.deltaTime);

            if (Paused)
            {
                // Only clear auxiliary layers on the transition into paused state
                if (!_wasPaused)
                {
                    if (_bgTilemap != null) _bgTilemap.ClearAllTiles();
                    if (_fxTilemap != null) _fxTilemap.ClearAllTiles();
                    if (_campfireEmberRenderer != null)
                        _campfireEmberRenderer.gameObject.SetActive(false);
                    _worldCursorRenderer?.Clear();
                    _lookOverlayRenderer?.Clear();
                }

                // Still update messages while paused (overlay popups don't hide the message area)
                if (_messagesDirty)
                {
                    RenderMessages();
                    _lastMessageCount = MessageLog.Count;
                    _prevIdleFadeAlpha = idleFadeAlpha;
                    _messagesDirty = false;
                }

                CacheCameraView(cam);
                _wasPaused = true;
                return;
            }

            // Re-enable embers on transition from paused to unpaused
            if (_wasPaused)
            {
                if (_campfireEmberRenderer != null)
                    _campfireEmberRenderer.gameObject.SetActive(true);
                _wasPaused = false;
                _dirty = true; // Force full redraw to restore bg/fx layers
            }

            if (_dirty && CurrentZone != null)
            {
                RenderZone();
                _dirty = false;
            }

            UpdateAmbientAnimations(Time.deltaTime);

            if (_messagesDirty)
            {
                RenderMessages();
                _lastMessageCount = MessageLog.Count;
                _prevIdleFadeAlpha = idleFadeAlpha;
                _messagesDirty = false;
            }

            if (_worldCursorState != null && _worldCursorState.Active)
                _worldCursorRenderer?.SetCursor(_worldCursorState, _cursorPlayer);
            else
                _worldCursorRenderer?.Clear();

            if (_currentLookSnapshot != null)
                _lookOverlayRenderer?.Render(_currentLookSnapshot, cam);
            else
                _lookOverlayRenderer?.Clear();

            CacheCameraView(cam);
        }

        /// <summary>
        /// Full redraw of the zone onto the tilemap.
        /// </summary>
        public void RenderZone()
        {
            if (CurrentZone == null || _tilemap == null) return;

            _tilemap.ClearAllTiles();
            _bgTilemap?.ClearAllTiles();

            // Compute field of view from the player's position
            if (PlayerEntity != null)
            {
                var playerCell = CurrentZone.GetEntityCell(PlayerEntity);
                if (playerCell != null)
                    FieldOfView.Compute(CurrentZone, playerCell.X, playerCell.Y, FovRadius);
            }

            // Compute lighting from all light sources
            if (_lightMap == null)
                _lightMap = new LightMap();
            _lightMap.Compute(CurrentZone);

            for (int x = 0; x < Zone.Width; x++)
            {
                for (int y = 0; y < Zone.Height; y++)
                {
                    RenderCell(x, y);
                }
            }

            RefreshWaterCache();
        }

        private static readonly Color RememberedColor = new Color(0.2f, 0.2f, 0.2f);

        private void RenderCell(int x, int y)
        {
            Cell cell = CurrentZone.GetCell(x, y);
            if (cell == null) return;

            // Unity tilemap Y is inverted relative to our grid (0=bottom in Unity, 0=top in roguelike)
            Vector3Int tilePos = new Vector3Int(x, Zone.Height - 1 - y, 0);

            // Fog of war: unexplored cells use a solid bg block tinted with UnexploredColor
            // (alpha supported — the bg SolidBlock is fully opaque pixels so the tint shows)
            if (!cell.Explored)
            {
                if (_bgTilemap != null)
                {
                    Tile bgTile = CP437TilesetGenerator.GetTile(CP437TilesetGenerator.SolidBlock);
                    _bgTilemap.SetTile(tilePos, bgTile);
                    _bgTilemap.SetTileFlags(tilePos, TileFlags.None);
                    _bgTilemap.SetColor(tilePos, UnexploredColor);
                }
                _tilemap.SetTile(tilePos, null);
                return;
            }

            Entity topEntity = cell.GetTopVisibleObject();
            if (topEntity == null)
            {
                Tile emptyTile = CP437TilesetGenerator.GetTile(AsciiWorldRenderPolicy.EmptyGlyph);
                _tilemap.SetTile(tilePos, emptyTile);
                _tilemap.SetTileFlags(tilePos, TileFlags.None);
                _tilemap.SetColor(tilePos, BackgroundColor);
                return;
            }

            // Remembered but not visible: show terrain/walls in dark gray, hide creatures/items
            if (!cell.IsVisible)
            {
                RenderRememberedCell(cell, x, y, tilePos);
                return;
            }

            RenderPart render = topEntity.GetPart<RenderPart>();
            if (render == null) return;

            char glyph;
            if (!string.IsNullOrEmpty(render.GlyphVariants))
            {
                glyph = render.ResolveGlyph(x, y);
            }
            else
            {
                glyph = AsciiWorldRenderPolicy.GetGlyphOrFallback(render, out string glyphIssue);
                LogRenderIssueOnce(topEntity, glyphIssue);
            }

            Tile tile = CP437TilesetGenerator.GetTile(glyph);
            if (tile == null) return;

            _tilemap.SetTile(tilePos, tile);
            _tilemap.SetTileFlags(tilePos, TileFlags.None);

            // Parse and apply color. Rendering participates in entity event flow,
            // so effects/parts can mutate color similarly to Qud's RenderEvent path.
            string colorString = AsciiWorldRenderPolicy.GetColorOrFallback(render, out string colorIssue);
            LogRenderIssueOnce(topEntity, colorIssue);
            var renderEvent = GameEvent.New("Render");
            renderEvent.SetParameter("Entity", (object)topEntity);
            renderEvent.SetParameter("RenderPart", (object)render);
            renderEvent.SetParameter("ColorString", colorString ?? "");
            renderEvent.SetParameter("DetailColor", render.DetailColor ?? "");
            topEntity.FireEvent(renderEvent);

            string eventColor = renderEvent.GetStringParameter("ColorString", colorString);
            if (AsciiWorldRenderPolicy.IsValidColorString(eventColor))
                colorString = eventColor;
            else
                colorString = AsciiWorldRenderPolicy.FallbackColorString;
            renderEvent.Release();

            Color color = QudColorParser.Parse(colorString);

            // Apply lighting tint
            if (_lightMap != null)
                color = _lightMap.ApplyToColor(color, x, y);

            _tilemap.SetColor(tilePos, color);

            // Render background color if specified.
            // Uses the same tilePos (same Y-inversion) on the background tilemap.
            if (_bgTilemap != null && !string.IsNullOrEmpty(render.BackgroundColor))
            {
                Color bgColor = QudColorParser.ParseBackground(render.BackgroundColor);
                if (bgColor.a > 0f)
                {
                    Color darkBg = QudColorParser.DarkenForBackground(bgColor);

                    // Apply lighting to background too
                    if (_lightMap != null)
                        darkBg = _lightMap.ApplyToColor(darkBg, x, y);

                    Tile bgTile = CP437TilesetGenerator.GetTile(CP437TilesetGenerator.SolidBlock);
                    _bgTilemap.SetTile(tilePos, bgTile);
                    _bgTilemap.SetTileFlags(tilePos, TileFlags.None);
                    _bgTilemap.SetColor(tilePos, darkBg);
                }
            }
        }

        private void RenderRememberedCell(Cell cell, int x, int y, Vector3Int tilePos)
        {
            // Only show terrain (layer 0-1) in remembered cells
            Entity terrainEntity = null;
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                var rp = cell.Objects[i].GetPart<RenderPart>();
                if (rp != null && rp.Visible && rp.RenderLayer <= 1)
                {
                    terrainEntity = cell.Objects[i];
                    break;
                }
            }

            if (terrainEntity != null)
            {
                var rp = terrainEntity.GetPart<RenderPart>();
                char g;
                if (!string.IsNullOrEmpty(rp.GlyphVariants))
                    g = rp.ResolveGlyph(x, y);
                else
                    g = AsciiWorldRenderPolicy.GetGlyphOrFallback(rp, out _);

                Tile t = CP437TilesetGenerator.GetTile(g);
                if (t != null)
                {
                    _tilemap.SetTile(tilePos, t);
                    _tilemap.SetTileFlags(tilePos, TileFlags.None);
                    _tilemap.SetColor(tilePos, RememberedColor);

                    // Dim background too
                    if (_bgTilemap != null && !string.IsNullOrEmpty(rp.BackgroundColor))
                    {
                        Color bgColor = QudColorParser.ParseBackground(rp.BackgroundColor);
                        if (bgColor.a > 0f)
                        {
                            Color dimBg = QudColorParser.DarkenForBackground(bgColor, 0.08f);
                            Tile bgTile = CP437TilesetGenerator.GetTile(CP437TilesetGenerator.SolidBlock);
                            _bgTilemap.SetTile(tilePos, bgTile);
                            _bgTilemap.SetTileFlags(tilePos, TileFlags.None);
                            _bgTilemap.SetColor(tilePos, dimBg);
                        }
                    }
                }
            }
            else
            {
                Tile emptyTile = CP437TilesetGenerator.GetTile(AsciiWorldRenderPolicy.EmptyGlyph);
                _tilemap.SetTile(tilePos, emptyTile);
                _tilemap.SetTileFlags(tilePos, TileFlags.None);
                _tilemap.SetColor(tilePos, BackgroundColor);
            }
        }

        private void LogRenderIssueOnce(Entity entity, string issue)
        {
            if (entity == null || string.IsNullOrWhiteSpace(issue))
                return;

            string key = $"{entity.ID}:{issue}";
            if (_loggedRenderIssues.Add(key))
                Debug.LogWarning($"[ASCII] {entity.BlueprintName ?? entity.ID}: {issue}");
        }

        /// <summary>
        /// Render recent messages at the bottom of the visible screen
        /// using the narrow-text message tilemap.
        /// </summary>
        private static readonly Color MsgBgColor = new Color(0.05f, 0.05f, 0.08f, 0.92f);

        private void RenderMessages()
        {
            if (_msgTilemap == null || MessageLineCount <= 0) return;

            _msgTilemap.ClearAllTiles();
            if (_msgBgTilemap != null)
                _msgBgTilemap.ClearAllTiles();

            // Fetch a wider window than we'll display so adjacent-duplicate coalescing
            // doesn't leave the log sparse during spam storms (10 identical hits => 1 line).
            int fetchCount = MessageLineCount * 8;
            var rawRecent = MessageLog.GetRecent(fetchCount);
            var rawTicks  = MessageLog.GetRecentTicks(fetchCount);
            if (rawRecent.Count == 0) return;

            // Coalesce adjacent duplicates walking newest->oldest until we have enough lines.
            // displayed[0] = newest visible line.
            var displayed = new List<(string text, int tick, int count)>(MessageLineCount);
            int rIdx = rawRecent.Count - 1;
            while (rIdx >= 0 && displayed.Count < MessageLineCount)
            {
                string cur = rawRecent[rIdx];
                int curTick = rIdx < rawTicks.Count ? rawTicks[rIdx] : 0;
                int n = 1;
                while (rIdx - 1 >= 0 && rawRecent[rIdx - 1] == cur)
                {
                    n++;
                    rIdx--;
                }
                displayed.Add((cur, curTick, n));
                rIdx--;
            }

            var cam = _mainCamera;
            if (cam == null) return;

            // Scale the message grid so text stays a consistent screen size
            // regardless of camera zoom level
            float scale = cam.orthographicSize / MessageReferenceZoom;
            _msgGridTransform.localScale = new Vector3(scale, scale, 1f);

            // Find the bottom tile row in scaled tile coordinates
            float worldBottom = cam.transform.position.y - cam.orthographicSize;
            int bottomTileY = Mathf.CeilToInt((worldBottom + 0.5f * scale) / scale);

            // How many half-width characters fit across the visible width
            float worldWidth = cam.orthographicSize * cam.aspect * 2f;
            int maxChars = Mathf.FloorToInt(worldWidth / (0.5f * scale));

            // Left edge in tile coordinates so text starts at the screen edge
            float worldLeft = cam.transform.position.x - cam.orthographicSize * cam.aspect;
            int startX = Mathf.CeilToInt(worldLeft / (0.5f * scale));

            // Ambience: idle-fade (log dims while untouched) + focus-flash (brief red
            // bg tint + yellow top-line override when a critical announcement fires).
            float idle = Time.time - _lastMessageTime;
            float idleFadeAlpha = Mathf.Lerp(
                1f,
                LogIdleAlphaFloor,
                Mathf.InverseLerp(LogIdleStart, LogIdleEnd, idle));
            bool flashActive = Time.time < _flashUntil;
            float flashT = flashActive
                ? Mathf.Clamp01((_flashUntil - Time.time) / FlashDuration) * 0.5f
                : 0f;

            // Tint the bg toward red while flashing, then apply idle-fade alpha.
            Color bgColor = MsgBgColor;
            if (flashActive)
                bgColor = Color.Lerp(bgColor, QudColorParser.BrightRed, flashT);
            bgColor.a = MsgBgColor.a * idleFadeAlpha;

            // Render background box behind messages
            if (_msgBgTilemap != null)
            {
                var blockTile = CP437TilesetGenerator.GetTextTile((char)219);
                if (blockTile != null)
                {
                    int bgLeft = startX - 1;
                    int bgRight = startX + maxChars;
                    int bgBottom = bottomTileY - 1;
                    // Derive bg top from the actual topmost text row so the box
                    // always covers all glyphs regardless of zoom/scale.
                    int topTextY = bottomTileY + (MessageLineCount - 1) * 2;
                    int bgTop = topTextY + 2;

                    for (int y = bgBottom; y <= bgTop; y++)
                    {
                        for (int x = bgLeft; x <= bgRight; x++)
                        {
                            var pos = new Vector3Int(x, y, 0);
                            _msgBgTilemap.SetTile(pos, blockTile);
                            _msgBgTilemap.SetTileFlags(pos, TileFlags.None);
                            _msgBgTilemap.SetColor(pos, bgColor);
                        }
                    }
                }
            }

            // Column layout per line (half-width cells on the msg grid):
            //   [0..3]  tick gutter (4 chars: "NNN ")
            //   [4]     category icon
            //   [5]     space
            //   [6..]   message text (optionally suffixed " (xN)" when coalesced)
            const int GutterWidth = 4;
            const int IconCol = 4;
            const int TextCol = 6;
            int textMaxChars = maxChars - TextCol;
            if (textMaxChars < 1) textMaxChars = 1;

            // Render newest message at screen bottom, older ones above with spacer rows.
            // Age-graduate opacity: newest = fully opaque white, older lines fade to gray.
            for (int i = 0; i < MessageLineCount; i++)
            {
                int tileY = bottomTileY + (i * 2);
                if (i >= displayed.Count) continue;

                var entry = displayed[i];

                // Alpha ramp: newest=1.0 -> oldest=0.35 across MessageLineCount lines.
                float t = (MessageLineCount <= 1) ? 0f : (float)i / (MessageLineCount - 1);
                float alpha = Mathf.Lerp(1.0f, 0.35f, t) * idleFadeAlpha;

                // Text color: hue shift from white (newest) -> gray (older).
                Color baseColor = Color.Lerp(QudColorParser.White, QudColorParser.Gray, t);
                // Top line flashes yellow while the focus-flash window is active.
                if (flashActive && i == 0)
                    baseColor = QudColorParser.BrightYellow;
                Color textColor = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

                // Tick gutter: "NNN " right-aligned in 3 chars + trailing space (4 total).
                string tickStr = FormatTick(entry.tick);
                Color gutterColor = new Color(
                    QudColorParser.DarkGray.r,
                    QudColorParser.DarkGray.g,
                    QudColorParser.DarkGray.b,
                    alpha);
                DrawMsgText(startX, tileY, tickStr, gutterColor, GutterWidth);

                // Category icon (inferred from message content).
                InferMsgCategory(entry.text, out char icon, out Color iconBase);
                if (icon != ' ')
                {
                    Color iconColor = new Color(iconBase.r, iconBase.g, iconBase.b, alpha);
                    DrawMsgText(startX + IconCol, tileY, icon.ToString(), iconColor, 1);
                }

                // Message text + optional " (xN)" suffix when coalesced.
                string body = entry.count > 1 ? entry.text + " (x" + entry.count + ")" : entry.text;
                DrawMsgText(startX + TextCol, tileY, body, textColor, textMaxChars);
            }
        }

        /// <summary>
        /// Formats a tick number into a 4-char gutter string "NNN " (right-pads).
        /// Wraps past 999 by showing modulo to keep alignment stable.
        /// </summary>
        private static string FormatTick(int tick)
        {
            int n = tick % 1000;
            if (n < 10)  return "  " + n + " ";
            if (n < 100) return " "  + n + " ";
            return n + " ";
        }

        /// <summary>
        /// Infer a single-char semantic icon + color from a log message string.
        /// Fallback is a blank icon (space) and gray color.
        /// </summary>
        private static void InferMsgCategory(string msg, out char icon, out Color color)
        {
            if (string.IsNullOrEmpty(msg)) { icon = ' '; color = QudColorParser.Gray; return; }
            if (msg.Contains(" heals ") || msg.Contains(" heal ")) { icon = '+'; color = QudColorParser.BrightGreen; return; }
            if (msg.Contains(" is killed") || msg.Contains(" dies")) { icon = '*'; color = QudColorParser.BrightYellow; return; }
            if (msg.Contains("advance to level") || msg.Contains(" gain ")) { icon = '^'; color = QudColorParser.BrightYellow; return; }
            if (msg.Contains(" hits ") || msg.Contains(" damage") || msg.Contains(" blasts ") || msg.Contains(" slashes ") || msg.Contains(" bites ")) { icon = '-'; color = QudColorParser.BrightRed; return; }
            if (msg.Contains(" catches fire") || msg.Contains(" is confused") || msg.Contains(" is poisoned") || msg.Contains(" is stunned") || msg.Contains(" is paralyzed") || msg.Contains(" is bleeding")) { icon = '!'; color = QudColorParser.White; return; }
            if (msg.Contains(" picks up ") || msg.Contains(" drops ") || msg.Contains(" equips ") || msg.Contains(" unequips ") || msg.Contains(" eats ")) { icon = '"'; color = QudColorParser.DarkCyan; return; }
            icon = ' ';
            color = QudColorParser.Gray;
        }

        private void DrawMsgText(int x, int tileY, string text, Color color, int maxChars)
        {
            if (text == null) return;
            int len = text.Length;
            if (len > maxChars) len = maxChars;

            for (int i = 0; i < len; i++)
            {
                char c = text[i];
                if (c == ' ') continue;

                var tile = CP437TilesetGenerator.GetTextTile(c);
                if (tile == null) continue;

                var pos = new Vector3Int(x + i, tileY, 0);
                _msgTilemap.SetTile(pos, tile);
                _msgTilemap.SetTileFlags(pos, TileFlags.None);
                _msgTilemap.SetColor(pos, color);
            }
        }

        /// <summary>
        /// Refresh a single cell (more efficient than full redraw for movement).
        /// </summary>
        public void RefreshCell(int x, int y)
        {
            RenderCell(x, y);
        }

        /// <summary>
        /// Refresh two cells (for movement: old position + new position).
        /// </summary>
        public void RefreshMovement(int oldX, int oldY, int newX, int newY)
        {
            RenderCell(oldX, oldY);
            RenderCell(newX, newY);
        }

        public void SetWorldCursorState(WorldCursorState state, Entity player)
        {
            _worldCursorState = state;
            _cursorPlayer = player;
        }

        public void ClearWorldCursor()
        {
            _worldCursorState = null;
            _cursorPlayer = null;
            _worldCursorRenderer?.Clear();
        }

        public void SetLookSnapshot(LookSnapshot snapshot)
        {
            _currentLookSnapshot = snapshot;
        }

        public void ClearLookSnapshot()
        {
            _currentLookSnapshot = null;
            _lookOverlayRenderer?.Clear();
        }

        public bool ScreenToZoneCell(Vector2 screenPosition, Camera camera, out int x, out int y)
        {
            x = -1;
            y = -1;

            if (CurrentZone == null || _tilemap == null || camera == null)
                return false;

            Vector3 world = camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -camera.transform.position.z));
            Vector3Int tileCell = _tilemap.WorldToCell(world);

            int zoneX = tileCell.x;
            int zoneY = Zone.Height - 1 - tileCell.y;
            if (!CurrentZone.InBounds(zoneX, zoneY))
                return false;

            x = zoneX;
            y = zoneY;
            return true;
        }

        public bool TryGetVisibleZoneBounds(Camera camera, out int minX, out int maxX, out int minY, out int maxY)
        {
            minX = 0;
            maxX = Zone.Width - 1;
            minY = 0;
            maxY = Zone.Height - 1;

            if (CurrentZone == null || _tilemap == null || camera == null)
                return false;

            float halfH = camera.orthographicSize;
            float halfW = halfH * camera.aspect;
            float left = camera.transform.position.x - halfW;
            float right = camera.transform.position.x + halfW;
            float bottom = camera.transform.position.y - halfH;
            float top = camera.transform.position.y + halfH;

            bool foundX = false;
            int visibleMinX = Zone.Width - 1;
            int visibleMaxX = 0;
            for (int tileX = 0; tileX < Zone.Width; tileX++)
            {
                float centerX = _tilemap.GetCellCenterWorld(new Vector3Int(tileX, 0, 0)).x;
                if (centerX < left || centerX > right)
                    continue;

                foundX = true;
                if (tileX < visibleMinX)
                    visibleMinX = tileX;
                if (tileX > visibleMaxX)
                    visibleMaxX = tileX;
            }

            bool foundY = false;
            int visibleMinY = Zone.Height - 1;
            int visibleMaxY = 0;
            for (int tileY = 0; tileY < Zone.Height; tileY++)
            {
                float centerY = _tilemap.GetCellCenterWorld(new Vector3Int(0, tileY, 0)).y;
                if (centerY < bottom || centerY > top)
                    continue;

                int zoneY = Zone.Height - 1 - tileY;
                foundY = true;
                if (zoneY < visibleMinY)
                    visibleMinY = zoneY;
                if (zoneY > visibleMaxY)
                    visibleMaxY = zoneY;
            }

            if (!foundX || !foundY)
                return false;

            minX = visibleMinX;
            maxX = visibleMaxX;
            minY = visibleMinY;
            maxY = visibleMaxY;
            return true;
        }

        // Water color cycle: 3 shades of blue/cyan
        private static readonly Color[] WaterColors =
        {
            QudColorParser.DarkBlue,
            QudColorParser.DarkCyan,
            QudColorParser.BrightBlue
        };

        /// <summary>
        /// Rebuild the cached list of water tile positions from the current zone.
        /// Called on zone load and after full redraws.
        /// </summary>
        private void RefreshWaterCache()
        {
            _waterTilePositions.Clear();
            if (CurrentZone == null) return;

            for (int x = 0; x < Zone.Width; x++)
            {
                for (int y = 0; y < Zone.Height; y++)
                {
                    Cell cell = CurrentZone.GetCell(x, y);
                    if (cell == null) continue;

                    Entity top = cell.GetTopVisibleObject();
                    if (top == null) continue;

                    var render = top.GetPart<RenderPart>();
                    if (render != null && render.RenderString == "~")
                        _waterTilePositions.Add(new Vector2Int(x, y));
                }
            }
        }

        /// <summary>
        /// Animate tiles that should shimmer or flicker (water, torches).
        /// Runs every frame, but only updates colors on the existing tilemap —
        /// doesn't re-set tiles.
        /// </summary>
        private void UpdateAmbientAnimations(float deltaTime)
        {
            if (CurrentZone == null || _tilemap == null) return;

            _ambientTimer += deltaTime;

            for (int i = 0; i < _waterTilePositions.Count; i++)
            {
                var pos = _waterTilePositions[i];
                int x = pos.x;
                int y = pos.y;

                Cell cell = CurrentZone.GetCell(x, y);
                if (cell == null || !cell.IsVisible) continue;

                // Verify water is still the top visible entity (creature may be standing on it)
                Entity top = cell.GetTopVisibleObject();
                if (top == null) continue;
                var render = top.GetPart<RenderPart>();
                if (render == null || render.RenderString != "~") continue;

                float phase = _ambientTimer * 2f + x * 0.7f + y * 1.3f;
                int colorIndex = ((int)phase) % WaterColors.Length;
                if (colorIndex < 0) colorIndex += WaterColors.Length;

                Vector3Int tilePos = new Vector3Int(x, Zone.Height - 1 - y, 0);
                _tilemap.SetTileFlags(tilePos, TileFlags.None);
                _tilemap.SetColor(tilePos, WaterColors[colorIndex]);
            }

            // Dust motes: spawn occasional faint particles in lit areas
            _dustMoteSpawnTimer += deltaTime;
            if (_dustMoteSpawnTimer >= DustMoteSpawnInterval && _asciiFxRenderer != null)
            {
                _dustMoteSpawnTimer = 0f;
                for (int attempt = 0; attempt < 10; attempt++)
                {
                    int rx = UnityEngine.Random.Range(0, Zone.Width);
                    int ry = UnityEngine.Random.Range(0, Zone.Height);
                    Cell dustCell = CurrentZone.GetCell(rx, ry);
                    if (dustCell != null && dustCell.IsVisible && !dustCell.IsWall() &&
                        _lightMap != null && _lightMap.GetBrightness(rx, ry) > _lightMap.AmbientLevel)
                    {
                        _asciiFxRenderer.SpawnDustMote(rx, ry);
                        break;
                    }
                }
            }
        }

        private bool HasCameraViewChanged(Camera camera)
        {
            if (camera == null)
                return false;

            if (_lastCameraSize < 0f)
                return true;

            return camera.transform.position != _lastCameraPosition ||
                   !Mathf.Approximately(camera.orthographicSize, _lastCameraSize) ||
                   !Mathf.Approximately(camera.aspect, _lastCameraAspect);
        }

        private void CacheCameraView(Camera camera)
        {
            if (camera == null)
                return;

            _lastCameraPosition = camera.transform.position;
            _lastCameraSize = camera.orthographicSize;
            _lastCameraAspect = camera.aspect;
        }
    }
}
