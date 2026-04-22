using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using UnityEngine;
using UnityEngine.Tilemaps;
using Stopwatch = System.Diagnostics.Stopwatch;

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
        /// Persistent sidebar width in narrow text columns.
        /// </summary>
        public int SidebarWidthChars = 34;

        /// <summary>
        /// Reference orthographic size at which message text appears at 1:1 scale.
        /// At other zoom levels, message text scales proportionally.
        /// </summary>
        public float MessageReferenceZoom = 20f;

        private Tilemap _tilemap;
        private Tilemap _bgTilemap;
        private Tilemap _fxTilemap;
        private Tilemap _sidebarBgTilemap;
        private Tilemap _sidebarTilemap;
        private Transform _sidebarGridTransform;
        private Material _sidebarUiMaterial;
        private Grid _hotbarGrid;
        private Tilemap _hotbarBgTilemap;
        private Tilemap _hotbarTilemap;
        private Transform _hotbarGridTransform;
        private Material _hotbarUiMaterial;
        private Grid _popupOverlayGrid;
        private Tilemap _popupOverlayBgTilemap;
        private Tilemap _popupOverlayTilemap;
        private Transform _popupOverlayGridTransform;
        private Material _popupOverlayUiMaterial;
        private AsciiFxRenderer _asciiFxRenderer;
        private CampfireEmberRenderer _campfireEmberRenderer;
        private WorldCursorRenderer _worldCursorRenderer;
        private GameplaySidebarRenderer _sidebarRenderer;
        private GameplayHotbarRenderer _hotbarRenderer;

        /// <summary>
        /// Phase 10 companion — when true, the sidebar's LOG section is
        /// replaced by a THOUGHTS section listing every Creature's current
        /// <see cref="BrainPart.LastThought"/>. Toggled by InputHandler on
        /// 't' keypress. Non-blocking: the player keeps full game input
        /// while visible (no InputState change, no early-return). Reuses the
        /// sidebar's existing tilemap + layout instead of drawing a separate
        /// overlay on the play area — the logger container doubles as the
        /// thought container so both surfaces share the same space.
        /// </summary>
        public bool ShowThoughtLog { get; set; }
        private bool _dirty = true;
        private int _lastFlashStamp;
        private float _flashUntil;
        private const float FlashDuration = 0.3f;
        private bool _wasPaused;
        private float _ambientTimer;
        private float _dustMoteSpawnTimer;
        private const float DustMoteSpawnInterval = 3.5f;
        private Camera _mainCamera;
        private Camera _sidebarCamera;
        private Camera _hotbarCamera;
        private Camera _popupOverlayCamera;
        private LightMap _lightMap;
        private readonly List<Vector2Int> _waterTilePositions = new List<Vector2Int>();

        /// <summary>
        /// A cell immediately west or east of a river channel column, cached
        /// for per-frame reflection tinting in UpdateAmbientAnimations.
        /// </summary>
        private struct ReflectionAdjacentTile
        {
            public int X, Y;
            /// <summary>True = use WaterBankColors (east flank of a bank cell), false = WaterCoreColors (west flank of a core cell).</summary>
            public bool UseBankPalette;
        }
        private readonly List<ReflectionAdjacentTile> _waterAdjacentPositions = new List<ReflectionAdjacentTile>();

        /// <summary>
        /// A leaf / twig that drifts south with the current at a cell-sub-
        /// precision position. Rendered by overriding the water glyph at
        /// the matching cell — no entity, no physics, just a visual loop.
        /// Port of river.ascii's debris pool.
        /// </summary>
        private struct DebrisDrifter
        {
            /// <summary>Cross-flow offset from centerline in half-widths (-0.6..0.6).</summary>
            public float XRel;
            /// <summary>Zone-heights per second — HTML range 0.12..0.30.</summary>
            public float Speed;
            /// <summary>Starting offset in [0, 1) so drifters don't bunch at zone load.</summary>
            public float Phase;
            /// <summary>'o' (60%) or '.' (40%) per HTML preset.</summary>
            public char Glyph;
        }

        /// <summary>Pool of debris drifters, re-randomized on each zone load.</summary>
        private DebrisDrifter[] _debris = System.Array.Empty<DebrisDrifter>();

        /// <summary>
        /// Per-row cached meander center column. Populated in
        /// RefreshWaterCache so DebrisGlyphAt doesn't recompute the sine
        /// for every cell every frame.
        /// </summary>
        private int[] _centerXPerRow = System.Array.Empty<int>();

        private readonly HashSet<string> _loggedRenderIssues = new HashSet<string>();
        private WorldCursorState _worldCursorState;
        private Entity _cursorPlayer;
        private LookSnapshot _currentLookSnapshot;
        private int _selectedHotbarSlot = -1;
        private ActivatedAbility _pendingHotbarAbility;
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
        public Tilemap SidebarBgTilemap => _sidebarBgTilemap;
        public Tilemap SidebarTilemap => _sidebarTilemap;
        public Camera SidebarCamera => _sidebarCamera;
        public Tilemap HotbarBgTilemap => _hotbarBgTilemap;
        public Tilemap HotbarTilemap => _hotbarTilemap;
        public Camera HotbarCamera => _hotbarCamera;
        public Tilemap CenteredPopupBgTilemap => _popupOverlayBgTilemap;
        public Tilemap CenteredPopupFgTilemap => _popupOverlayTilemap;
        public Camera PopupOverlayCamera => _popupOverlayCamera;
        /// <summary>Popup background tilemap (sortingOrder 6). For DialogueUI/TradeUI bg fills.</summary>
        public Tilemap PopupBgTilemap => _popupBgTilemap;
        /// <summary>Popup foreground tilemap (sortingOrder 7). For DialogueUI/TradeUI glyphs.</summary>
        public Tilemap PopupFgTilemap => _popupFgTilemap;
        public int SidebarLogScrollOffsetRows => _sidebarRenderer?.LogScrollOffsetRows ?? 0;

        private void Awake()
        {
            _lastFlashStamp = MessageLog.FlashStamp;
            _mainCamera = Camera.main;
            _tilemap = GetComponent<Tilemap>();
            GameplayRenderLayers.SetLayerRecursive(gameObject, GameplayRenderLayers.WorldLayer);

            Grid grid = GetComponentInParent<Grid>();
            Transform gridParent = grid != null ? grid.transform : (transform.parent != null ? transform.parent : transform);

            // Background tilemap: solid color blocks behind foreground glyphs.
            // Same grid, same cell size — just a lower sorting order.
            var bgTilemapObj = new GameObject("BgTilemap");
            bgTilemapObj.transform.SetParent(gridParent, false);
            GameplayRenderLayers.SetLayerRecursive(bgTilemapObj, GameplayRenderLayers.WorldLayer);
            _bgTilemap = bgTilemapObj.AddComponent<Tilemap>();
            var bgRenderer = bgTilemapObj.AddComponent<TilemapRenderer>();
            bgRenderer.sortingOrder = -1; // below world tilemap

            var fxTilemapObj = new GameObject("FxTilemap");
            fxTilemapObj.transform.SetParent(gridParent, false);
            GameplayRenderLayers.SetLayerRecursive(fxTilemapObj, GameplayRenderLayers.WorldLayer);
            _fxTilemap = fxTilemapObj.AddComponent<Tilemap>();
            var fxRenderer = fxTilemapObj.AddComponent<TilemapRenderer>();
            fxRenderer.sortingOrder = 1; // above world, below sidebar
            _asciiFxRenderer = new AsciiFxRenderer(_fxTilemap);

            var emberObj = new GameObject("CampfireEmbers");
            emberObj.transform.SetParent(gridParent, false);
            GameplayRenderLayers.SetLayerRecursive(emberObj, GameplayRenderLayers.WorldLayer);
            _campfireEmberRenderer = emberObj.AddComponent<CampfireEmberRenderer>();

            // Dedicated narrow-text grid for the persistent sidebar.
            var sidebarGridObj = new GameObject("SidebarGrid");
            _sidebarGridTransform = sidebarGridObj.transform;
            GameplayRenderLayers.SetLayerRecursive(sidebarGridObj, GameplayRenderLayers.SidebarLayer);
            var sidebarGrid = sidebarGridObj.AddComponent<Grid>();
            sidebarGrid.cellSize = new Vector3(0.5f, 1f, 0f);

            var sidebarBgObj = new GameObject("SidebarBgTilemap");
            sidebarBgObj.transform.SetParent(sidebarGridObj.transform);
            GameplayRenderLayers.SetLayerRecursive(sidebarBgObj, GameplayRenderLayers.SidebarLayer);
            _sidebarBgTilemap = sidebarBgObj.AddComponent<Tilemap>();
            var sidebarBgRenderer = sidebarBgObj.AddComponent<TilemapRenderer>();
            ConfigureSidebarTilemapRenderer(sidebarBgRenderer, 2);

            var sidebarTmObj = new GameObject("SidebarTilemap");
            sidebarTmObj.transform.SetParent(sidebarGridObj.transform);
            GameplayRenderLayers.SetLayerRecursive(sidebarTmObj, GameplayRenderLayers.SidebarLayer);
            _sidebarTilemap = sidebarTmObj.AddComponent<Tilemap>();
            var sidebarRenderer = sidebarTmObj.AddComponent<TilemapRenderer>();
            ConfigureSidebarTilemapRenderer(sidebarRenderer, 3);

            var hotbarGridObj = new GameObject("HotbarGrid");
            _hotbarGridTransform = hotbarGridObj.transform;
            GameplayRenderLayers.SetLayerRecursive(hotbarGridObj, GameplayRenderLayers.HotbarLayer);
            _hotbarGrid = hotbarGridObj.AddComponent<Grid>();
            _hotbarGrid.cellSize = new Vector3(0.5f, 1f, 0f);

            var hotbarBgObj = new GameObject("HotbarBgTilemap");
            hotbarBgObj.transform.SetParent(hotbarGridObj.transform, false);
            GameplayRenderLayers.SetLayerRecursive(hotbarBgObj, GameplayRenderLayers.HotbarLayer);
            _hotbarBgTilemap = hotbarBgObj.AddComponent<Tilemap>();
            var hotbarBgRenderer = hotbarBgObj.AddComponent<TilemapRenderer>();
            ConfigureHotbarTilemapRenderer(hotbarBgRenderer, 0);

            var hotbarTmObj = new GameObject("HotbarTilemap");
            hotbarTmObj.transform.SetParent(hotbarGridObj.transform, false);
            GameplayRenderLayers.SetLayerRecursive(hotbarTmObj, GameplayRenderLayers.HotbarLayer);
            _hotbarTilemap = hotbarTmObj.AddComponent<Tilemap>();
            var hotbarRenderer = hotbarTmObj.AddComponent<TilemapRenderer>();
            ConfigureHotbarTilemapRenderer(hotbarRenderer, 1);

            _worldCursorRenderer = new WorldCursorRenderer(gridParent, _tilemap, GameplayRenderLayers.WorldLayer);
            _sidebarRenderer = new GameplaySidebarRenderer(_sidebarTilemap, _sidebarBgTilemap, _sidebarGridTransform, MessageReferenceZoom);
            _hotbarRenderer = new GameplayHotbarRenderer(_hotbarTilemap, _hotbarBgTilemap);

            var popupOverlayGridObj = new GameObject("PopupOverlayGrid");
            _popupOverlayGridTransform = popupOverlayGridObj.transform;
            GameplayRenderLayers.SetLayerRecursive(popupOverlayGridObj, GameplayRenderLayers.PopupOverlayLayer);
            _popupOverlayGrid = popupOverlayGridObj.AddComponent<Grid>();
            _popupOverlayGrid.cellSize = new Vector3(1f, 1f, 0f);

            var popupOverlayBgObj = new GameObject("CenteredPopupBgTilemap");
            popupOverlayBgObj.transform.SetParent(popupOverlayGridObj.transform, false);
            GameplayRenderLayers.SetLayerRecursive(popupOverlayBgObj, GameplayRenderLayers.PopupOverlayLayer);
            _popupOverlayBgTilemap = popupOverlayBgObj.AddComponent<Tilemap>();
            var popupOverlayBgRenderer = popupOverlayBgObj.AddComponent<TilemapRenderer>();
            ConfigurePopupOverlayTilemapRenderer(popupOverlayBgRenderer, 0);

            var popupOverlayFgObj = new GameObject("CenteredPopupFgTilemap");
            popupOverlayFgObj.transform.SetParent(popupOverlayGridObj.transform, false);
            GameplayRenderLayers.SetLayerRecursive(popupOverlayFgObj, GameplayRenderLayers.PopupOverlayLayer);
            _popupOverlayTilemap = popupOverlayFgObj.AddComponent<Tilemap>();
            var popupOverlayFgRenderer = popupOverlayFgObj.AddComponent<TilemapRenderer>();
            ConfigurePopupOverlayTilemapRenderer(popupOverlayFgRenderer, 1);

            // Dedicated tilemaps for dialogue/popup UI — must sort ABOVE the
            // persistent sidebar (order 3) so popups aren't hidden behind the UI.
            var popupBgObj = new GameObject("PopupBgTilemap");
            popupBgObj.transform.SetParent(gridParent, false);
            GameplayRenderLayers.SetLayerRecursive(popupBgObj, GameplayRenderLayers.WorldLayer);
            _popupBgTilemap = popupBgObj.AddComponent<Tilemap>();
            var popupBgRenderer = popupBgObj.AddComponent<TilemapRenderer>();
            popupBgRenderer.sortingOrder = 6;

            var popupFgObj = new GameObject("PopupFgTilemap");
            popupFgObj.transform.SetParent(gridParent, false);
            GameplayRenderLayers.SetLayerRecursive(popupFgObj, GameplayRenderLayers.WorldLayer);
            _popupFgTilemap = popupFgObj.AddComponent<Tilemap>();
            var popupFgRenderer = popupFgObj.AddComponent<TilemapRenderer>();
            popupFgRenderer.sortingOrder = 7;
        }

        private void OnDestroy()
        {
            DestroyOwnedMaterial(ref _sidebarUiMaterial);
            DestroyOwnedMaterial(ref _hotbarUiMaterial);
            DestroyOwnedMaterial(ref _popupOverlayUiMaterial);
        }

        /// <summary>
        /// Set the zone to render. Triggers a full redraw.
        /// </summary>
        public void SetZone(Zone zone)
        {
            CurrentZone = zone;
            _asciiFxRenderer?.SetZone(zone);
            _worldCursorRenderer?.SetZone(zone);
            _sidebarRenderer?.ResetLogScroll();
            _sidebarRenderer?.Clear();
            _sidebarRenderer?.Invalidate();
            _hotbarRenderer?.Clear();
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
            MarkDirty("Unknown");
        }

        public void MarkDirty(string source)
        {
            using (PerformanceMarkers.Zone.MarkDirty.Auto())
            {
                PerformanceDiagnostics.RecordMarkDirty(source);
                _dirty = true;
                _sidebarRenderer?.Invalidate();
            }
        }

        private void LateUpdate()
        {
            long frameStart = Stopwatch.GetTimestamp();
            using (PerformanceMarkers.Zone.LateUpdate.Auto())
            {
                PerformanceDiagnostics.BeginFrame(
                    MessageLog.TickProvider != null ? MessageLog.TickProvider() : 0,
                    Paused,
                    _dirty);

                if (_mainCamera == null)
                    _mainCamera = Camera.main;

                UpdatePopupOverlayGridLayout();
                UpdateHotbarGridLayout();

                if (MessageLog.FlashStamp != _lastFlashStamp)
                {
                    _lastFlashStamp = MessageLog.FlashStamp;
                    _flashUntil = Time.time + FlashDuration;
                }

                Camera cam = _mainCamera;
                _asciiFxRenderer?.Update(Time.deltaTime);
                if (_asciiFxRenderer != null)
                {
                    PerformanceDiagnostics.RecordAsciiFxCounts(
                        _asciiFxRenderer.ActiveProjectileCount,
                        _asciiFxRenderer.ActiveBurstCount,
                        _asciiFxRenderer.ActiveParticleCount,
                        _asciiFxRenderer.ActiveAuraCount,
                        _asciiFxRenderer.ActiveBeamCount,
                        _asciiFxRenderer.ActiveChargeOrbitCount,
                        _asciiFxRenderer.ActiveRingWaveCount,
                        _asciiFxRenderer.ActiveChainArcCount,
                        _asciiFxRenderer.ActiveColumnRiseCount,
                        _asciiFxRenderer.ActiveDustMoteCount);
                }

                if (Paused)
                {
                    // Only clear auxiliary layers on the transition into paused state
                    if (!_wasPaused)
                    {
                        if (_bgTilemap != null)
                        {
                            _bgTilemap.ClearAllTiles();
                            PerformanceDiagnostics.RecordTilemapClear();
                        }

                        if (_fxTilemap != null)
                        {
                            _fxTilemap.ClearAllTiles();
                            PerformanceDiagnostics.RecordTilemapClear();
                        }

                        if (_campfireEmberRenderer != null)
                            _campfireEmberRenderer.gameObject.SetActive(false);
                        _worldCursorRenderer?.Clear();
                        _sidebarRenderer?.Clear();
                        _hotbarRenderer?.Clear();
                    }

                    CacheCameraView(cam);
                    _wasPaused = true;
                    PerformanceDiagnostics.EndFrame(
                        PerformanceDiagnostics.ElapsedMilliseconds(frameStart, Stopwatch.GetTimestamp()));
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

                using (PerformanceMarkers.Zone.UpdateAmbientAnimations.Auto())
                    UpdateAmbientAnimations(Time.deltaTime);

                RenderSidebar(cam);
                RenderHotbar();

                if (_worldCursorState != null && _worldCursorState.Active)
                    _worldCursorRenderer?.SetCursor(_worldCursorState, _cursorPlayer);
                else
                    _worldCursorRenderer?.Clear();

                CacheCameraView(cam);
                PerformanceDiagnostics.EndFrame(
                    PerformanceDiagnostics.ElapsedMilliseconds(frameStart, Stopwatch.GetTimestamp()));
            }
        }

        /// <summary>
        /// Full redraw of the zone onto the tilemap.
        /// </summary>
        public void RenderZone()
        {
            using (PerformanceMarkers.Zone.RenderZone.Auto())
            {
                if (CurrentZone == null || _tilemap == null) return;

                _tilemap.ClearAllTiles();
                PerformanceDiagnostics.RecordTilemapClear();
                if (_bgTilemap != null)
                {
                    _bgTilemap.ClearAllTiles();
                    PerformanceDiagnostics.RecordTilemapClear();
                }

                // Compute field of view from the player's position
                if (PlayerEntity != null)
                {
                    var playerCell = CurrentZone.GetEntityCell(PlayerEntity);
                    if (playerCell != null)
                    {
                        using (PerformanceMarkers.Zone.ComputeFov.Auto())
                            FieldOfView.Compute(CurrentZone, playerCell.X, playerCell.Y, FovRadius);
                    }
                }

                // Compute lighting from all light sources
                if (_lightMap == null)
                    _lightMap = new LightMap();
                using (PerformanceMarkers.Zone.ComputeLightMap.Auto())
                    _lightMap.Compute(CurrentZone);

                int cellsRendered = Zone.Width * Zone.Height;
                PerformanceDiagnostics.RecordZoneRedraw(cellsRendered);
                for (int x = 0; x < Zone.Width; x++)
                {
                    for (int y = 0; y < Zone.Height; y++)
                        RenderCell(x, y);
                }

                RefreshWaterCache();
            }
        }

        private static readonly Color RememberedColor = new Color(0.2f, 0.2f, 0.2f);

        private void RenderCell(int x, int y)
        {
            if (PerformanceDiagnostics.DetailedCellProfilingEnabled)
            {
                using (PerformanceMarkers.Zone.RenderCell.Auto())
                    RenderCellCore(x, y);
                return;
            }

            RenderCellCore(x, y);
        }

        private void RenderCellCore(int x, int y)
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

        private void RenderSidebar(Camera camera)
        {
            using (PerformanceMarkers.Zone.RenderSidebar.Auto())
            {
                Camera sidebarCamera = _sidebarCamera != null ? _sidebarCamera : camera;
                if (sidebarCamera == null || !sidebarCamera.enabled)
                {
                    _sidebarRenderer?.Clear();
                    return;
                }

                bool flashActive = Time.time < _flashUntil;
                float flashT = flashActive
                    ? Mathf.Clamp01((_flashUntil - Time.time) / FlashDuration)
                    : 0f;

                // Phase 10 — the 't' toggle swaps the sidebar's bottom
                // section (LOG) for THOUGHTS by passing showThoughts into
                // Build. Data-driven dispatch inside the sidebar renderer
                // selects DrawLogPanel vs DrawThoughtsPanel based on
                // snapshot.ThoughtEntries being null/non-null. No separate
                // tilemap or camera — same panel, different content.
                SidebarSnapshot snapshot = SidebarStateBuilder.Build(
                    PlayerEntity, CurrentZone, _currentLookSnapshot,
                    showThoughts: ShowThoughtLog);
                _sidebarRenderer?.Render(snapshot, sidebarCamera, SidebarWidthChars, flashActive, flashT);
            }
        }

        private void RenderHotbar()
        {
            using (PerformanceMarkers.Zone.RenderHotbar.Auto())
            {
                Camera hotbarCamera = _hotbarCamera;
                if (hotbarCamera == null || !hotbarCamera.enabled)
                {
                    _hotbarRenderer?.Clear();
                    return;
                }

                HotbarSnapshot snapshot = HotbarStateBuilder.Build(PlayerEntity, _selectedHotbarSlot, _pendingHotbarAbility);
                _hotbarRenderer?.Render(snapshot, hotbarCamera);
            }
        }

        private void ConfigureSidebarTilemapRenderer(TilemapRenderer renderer, int sortingOrder)
        {
            if (renderer == null)
                return;

            renderer.sortingOrder = sortingOrder;

            Material uiMaterial = GetUnlitSpriteMaterial(ref _sidebarUiMaterial, "SidebarUI-Unlit");
            if (uiMaterial != null)
                renderer.sharedMaterial = uiMaterial;
        }

        private void ConfigurePopupOverlayTilemapRenderer(TilemapRenderer renderer, int sortingOrder)
        {
            if (renderer == null)
                return;

            renderer.sortingOrder = sortingOrder;

            Material uiMaterial = GetUnlitSpriteMaterial(ref _popupOverlayUiMaterial, "CenteredPopupUI-Unlit");
            if (uiMaterial != null)
                renderer.sharedMaterial = uiMaterial;
        }

        private void ConfigureHotbarTilemapRenderer(TilemapRenderer renderer, int sortingOrder)
        {
            if (renderer == null)
                return;

            renderer.sortingOrder = sortingOrder;

            Material uiMaterial = GetUnlitSpriteMaterial(ref _hotbarUiMaterial, "HotbarUI-Unlit");
            if (uiMaterial != null)
                renderer.sharedMaterial = uiMaterial;
        }

        private static Material GetUnlitSpriteMaterial(ref Material material, string materialName)
        {
            if (material != null)
                return material;

            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            if (shader == null)
            {
                Debug.LogWarning("[Rendering] Failed to find an unlit sprite shader for UI tilemaps.");
                return null;
            }

            material = new Material(shader)
            {
                name = materialName
            };
            material.hideFlags = HideFlags.HideAndDontSave;
            return material;
        }

        private static void DestroyOwnedMaterial(ref Material material)
        {
            if (material == null)
                return;

            if (Application.isPlaying)
                Object.Destroy(material);
            else
                Object.DestroyImmediate(material);

            material = null;
        }

        private void UpdatePopupOverlayGridLayout()
        {
            if (_popupOverlayGrid == null || _popupOverlayCamera == null)
                return;

            float cellWidth = CenteredPopupLayout.ComputeCellWidth(_popupOverlayCamera.aspect);
            Vector3 cellSize = _popupOverlayGrid.cellSize;
            if (Mathf.Abs(cellSize.x - cellWidth) > 0.0001f || Mathf.Abs(cellSize.y - 1f) > 0.0001f)
                _popupOverlayGrid.cellSize = new Vector3(cellWidth, 1f, 0f);
        }

        private void UpdateHotbarGridLayout()
        {
            if (_hotbarGrid == null || _hotbarCamera == null)
                return;

            float cellWidth = GameplayHotbarLayout.ComputeCellWidth(_hotbarCamera.aspect);
            Vector3 cellSize = _hotbarGrid.cellSize;
            if (Mathf.Abs(cellSize.x - cellWidth) > 0.0001f || Mathf.Abs(cellSize.y - 1f) > 0.0001f)
                _hotbarGrid.cellSize = new Vector3(cellWidth, 1f, 0f);
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
            _sidebarRenderer?.Invalidate();
        }

        public void ClearLookSnapshot()
        {
            _currentLookSnapshot = null;
            _sidebarRenderer?.Invalidate();
        }

        public void SetSidebarCamera(Camera sidebarCamera)
        {
            _sidebarCamera = sidebarCamera;
            _sidebarRenderer?.Invalidate();
        }

        public void SetHotbarCamera(Camera hotbarCamera)
        {
            _hotbarCamera = hotbarCamera;
            UpdateHotbarGridLayout();
        }

        public void SetPopupOverlayCamera(Camera popupOverlayCamera)
        {
            _popupOverlayCamera = popupOverlayCamera;
            UpdatePopupOverlayGridLayout();
        }

        public void SetHotbarState(int selectedSlot, ActivatedAbility pendingAbility)
        {
            _selectedHotbarSlot = selectedSlot;
            _pendingHotbarAbility = pendingAbility;
        }

        public bool ScrollSidebarLogOlder(int rows = 1)
        {
            return _sidebarRenderer != null && _sidebarRenderer.ScrollOlder(rows);
        }

        public bool ScrollSidebarLogNewer(int rows = 1)
        {
            return _sidebarRenderer != null && _sidebarRenderer.ScrollNewer(rows);
        }

        public bool ScreenToZoneCell(Vector2 screenPosition, Camera camera, out int x, out int y)
        {
            x = -1;
            y = -1;

            if (CurrentZone == null || _tilemap == null || camera == null)
                return false;

            if (!camera.pixelRect.Contains(screenPosition))
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

        public bool TryGetHotbarSlotAtScreenPosition(Vector2 screenPosition, out int slot)
        {
            slot = -1;
            return GameplayHotbarLayout.TryGetSlotAtScreenPosition(
                _hotbarCamera,
                _hotbarTilemap,
                screenPosition,
                out slot);
        }

        public bool TryGetVisibleZoneBounds(Camera camera, out int minX, out int maxX, out int minY, out int maxY)
        {
            minX = 0;
            maxX = Zone.Width - 1;
            minY = 0;
            maxY = Zone.Height - 1;

            if (CurrentZone == null || _tilemap == null || camera == null)
                return false;

            Vector3 worldBottomLeft = camera.ViewportToWorldPoint(new Vector3(0f, 0f, -camera.transform.position.z));
            Vector3 worldTopRight = camera.ViewportToWorldPoint(new Vector3(1f, 1f, -camera.transform.position.z));
            float left = worldBottomLeft.x;
            float right = worldTopRight.x;
            float bottom = worldBottomLeft.y;
            float top = worldTopRight.y;

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

        // Water color cycle: 3 shades of blue/cyan. Used for village-decor
        // puddles (stationary shimmer). River cells use the depth-shaded
        // WaterCoreColors / WaterBankColors palettes below.
        private static readonly Color[] WaterColors =
        {
            QudColorParser.DarkBlue,
            QudColorParser.DarkCyan,
            QudColorParser.BrightBlue
        };

        /// <summary>
        /// Palette for the deeper (center) column of the river — weighted
        /// toward dark blues so the channel reads as having depth. Shares
        /// DarkCyan with the bank palette to blend smoothly at the boundary.
        /// Must be the same length as WaterBankColors and FlowGlyphs so
        /// glyph + color animations stay in lockstep.
        /// </summary>
        private static readonly Color[] WaterCoreColors =
        {
            QudColorParser.DarkBlue,
            QudColorParser.DarkBlue,
            QudColorParser.DarkCyan
        };

        /// <summary>
        /// Palette for the shallower (outer) column of the river — brighter
        /// cyans and blues so the bank edge reads as sunlight-catching
        /// ripples. Shares DarkCyan with the core palette.
        /// </summary>
        private static readonly Color[] WaterBankColors =
        {
            QudColorParser.DarkCyan,
            QudColorParser.BrightBlue,
            QudColorParser.BrightCyan
        };

        // -------- River scalar-field sampler (density-ramp port) --------
        //
        // Adapted from the river.ascii demo: instead of phase-cycling through
        // a fixed glyph array, each water cell samples a scalar field
        //   val = mix(ripples, noise)
        // and picks a glyph + color by thresholds. Ripples travel along the
        // flow direction (+y), noise drifts slowly to keep the field organic.

        /// <summary>Along-flow (per-row) frequency of the primary ripple wave.</summary>
        private const float RippleFreq1 = 0.28f;

        /// <summary>Along-flow frequency of the faster detail ripple.</summary>
        private const float RippleFreq2 = 0.55f;

        /// <summary>Along-flow frequency of the slow undulation.</summary>
        private const float RippleFreq3 = 0.11f;

        /// <summary>Temporal speed of primary ripple (at flow=1 → ~4 phase units/sec).</summary>
        private const float RippleSpeed1 = 4.0f;

        /// <summary>Temporal speed of detail ripple.</summary>
        private const float RippleSpeed2 = 6.5f;

        /// <summary>Temporal speed of slow undulation.</summary>
        private const float RippleSpeed3 = 2.0f;

        /// <summary>Cross-flow phase coupling for primary ripple (bank lags core).</summary>
        private const float RippleCross1 = 2.0f;

        /// <summary>Cross-flow phase coupling for detail ripple (opposite sign creates beat).</summary>
        private const float RippleCross2 = -3.0f;

        /// <summary>Amplitude weight for the secondary (detail) ripple.</summary>
        private const float RippleAmp2 = 0.55f;

        /// <summary>Amplitude weight for the tertiary (slow) ripple.</summary>
        private const float RippleAmp3 = 0.80f;

        /// <summary>Final rescaling of the combined ripple sum → roughly [-1, 1].</summary>
        private const float RippleMixWeight = 0.4f;

        /// <summary>Global flow-speed multiplier. HTML presets: calm=0.55 / steady=1.0 / rapids=1.9.</summary>
        private const float FlowSpeedMult = 0.8f;

        /// <summary>Amplitude of the organic noise term layered onto ripples.</summary>
        private const float TurbulenceAmount = 0.5f;

        /// <summary>Along-flow sample frequency for turbulence noise.</summary>
        private const float TurbFreqAlong = 0.12f;

        /// <summary>Cross-flow sample frequency for turbulence noise.</summary>
        private const float TurbFreqCross = 0.18f;

        /// <summary>How fast the noise field drifts through time (adds slow organic shift).</summary>
        private const float TurbTimeScale = 0.4f;

        /// <summary>val threshold above which a cell renders as '*' foam (biggest breakers).</summary>
        private const float FoamCutoffLarge = 1.15f;

        /// <summary>val threshold above which a cell renders as '≈' foam (small crest).</summary>
        private const float FoamCutoffSmall = 0.85f;

        /// <summary>val threshold for '=' glyph (heavy water).</summary>
        private const float DensityThreshHeavy = 0.55f;

        /// <summary>val threshold for '-' glyph (medium flow).</summary>
        private const float DensityThreshMedium = 0.15f;

        /// <summary>val threshold for '~' glyph (standard ripple).</summary>
        private const float DensityThreshTilde = -0.25f;

        /// <summary>val threshold for '.' glyph (calm). Below this, the cell renders as space.</summary>
        private const float DensityThreshDot = -0.65f;

        /// <summary>
        /// BG tint strength on the river cells themselves. Stronger than
        /// ReflectionTintStrength (0.22) because the water IS the light
        /// source — its own cells should glow visibly even when the fg
        /// glyph is space (empty patch of calm water).
        /// </summary>
        private const float WaterBgTintStrength = 0.5f;

        /// <summary>
        /// How strongly the reflection tint blends the water's current color
        /// into flanking cells' background. 0 = no reflection, 1 = fully
        /// replace bg with the (darkened) water color. 0.22 keeps ground
        /// cells readable while still visibly linking them to the river.
        /// </summary>
        private const float ReflectionTintStrength = 0.22f;

        // ---- Debris drifters (river.ascii leaf pool) ----

        /// <summary>How many leaves/twigs drift in the river at any time. HTML uses 3..6 across presets; 4 sits in the middle and feels right for a 25-row zone.</summary>
        private const int DebrisCount = 4;

        /// <summary>Color string for debris. DarkYellow is the nearest Qud palette match to HTML's amber #c99755.</summary>
        private const string DebrisColor = "&w";

        /// <summary>Cell-distance threshold (|y-debrisY| and |x-targetX|) for "debris is at this cell." HTML uses 0.75 in normalized coords; here in raw cells.</summary>
        private const float DebrisCellMatchDistance = 0.75f;

        // ---- Sampling helpers (port of the river.ascii scalar field) ----

        /// <summary>Cheap deterministic hash → [0, 1]. Port of the HTML demo's hash().</summary>
        private static float Hash01(int x, int y)
        {
            float n = Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f;
            return n - Mathf.Floor(n);
        }

        /// <summary>2D smoothed value noise → [0, 1]. Bilinear interp with smoothstep.</summary>
        private static float ValueNoise(float x, float y)
        {
            int xi = Mathf.FloorToInt(x);
            int yi = Mathf.FloorToInt(y);
            float xf = x - xi;
            float yf = y - yi;

            float a = Hash01(xi,     yi);
            float b = Hash01(xi + 1, yi);
            float c = Hash01(xi,     yi + 1);
            float d = Hash01(xi + 1, yi + 1);

            float u = xf * xf * (3f - 2f * xf);
            float v = yf * yf * (3f - 2f * yf);

            return a * (1f - u) * (1f - v) + b * u * (1f - v)
                 + c * (1f - u) * v       + d * u * v;
        }

        /// <summary>
        /// Sample the river's scalar field at (x, y) and time t. Returns a
        /// value in roughly [-1.8, 1.8]; thresholds on it pick the glyph
        /// and color. High = crest / foam, low = calm / empty.
        ///
        /// Port of river.ascii's sample(). Axes swapped: HTML flows in +x,
        /// we flow in +y, so ripples use y as the along-flow coordinate.
        /// </summary>
        private static float SampleRiverVal(int x, int y, float t, bool isBank)
        {
            // rel ≈ cross-flow position. In HTML rel∈[0,1]; here we have
            // only two cells (core + bank) so we pick representative
            // values that give a visible — but not extreme — speed
            // differential via the parabolic profile below.
            float rel = isBank ? 0.75f : 0.25f;

            // Parabolic speed profile: current is strongest at the center
            // line and drops off toward the banks (friction).
            float flow = (1f - rel * rel * 0.65f) * FlowSpeedMult;

            // Three traveling sines at different frequencies and speeds.
            // Minus sign on t means bands move in +y (south) over time.
            float r1 = Mathf.Sin(y * RippleFreq1 - t * RippleSpeed1 * flow + rel * RippleCross1);
            float r2 = Mathf.Sin(y * RippleFreq2 - t * RippleSpeed2 * flow + rel * RippleCross2) * RippleAmp2;
            float r3 = Mathf.Sin(y * RippleFreq3 - t * RippleSpeed3 * flow)                      * RippleAmp3;

            // Organic turbulence: noise sampled with time advancing one
            // axis, so the field visibly drifts without ever repeating.
            float turb = (ValueNoise(y * TurbFreqAlong + t * TurbTimeScale,
                                     x * TurbFreqCross) - 0.5f) * TurbulenceAmount;

            return (r1 + r2 + r3) * RippleMixWeight + turb;
        }

        /// <summary>
        /// Density-ramp glyph: mid-line heavy → thin → calm → dot → space.
        /// The trailing ' ' (space) is intentional — empty cells let the
        /// bg tint show as "deep still water" and add visual variety to
        /// the channel silhouette.
        /// </summary>
        private static char DensityGlyph(float val)
        {
            if (val > DensityThreshHeavy)  return '=';
            if (val > DensityThreshMedium) return '-';
            if (val > DensityThreshTilde)  return '~';
            if (val > DensityThreshDot)    return '.';
            return ' ';
        }

        /// <summary>
        /// Foam glyph at ripple peaks, or '\0' if below foam cutoffs. '*'
        /// for the biggest breakers, '≈' for gentler crests. White color
        /// is applied separately by WaterColorForVal.
        /// </summary>
        private static char FoamGlyph(float val)
        {
            if (val > FoamCutoffLarge) return '*';
            if (val > FoamCutoffSmall) return '\u2248';  // ≈
            return '\0';
        }

        /// <summary>
        /// Pick the fg color for a water cell. Foam overrides to white;
        /// otherwise val picks an index within the core/bank palette so
        /// crests glow brighter, troughs sink toward DarkBlue.
        /// </summary>
        private static Color WaterColorForVal(float val, bool isBank)
        {
            if (val > FoamCutoffSmall) return QudColorParser.White;
            Color[] palette = isBank ? WaterBankColors : WaterCoreColors;
            if (val > DensityThreshMedium) return palette[2];
            if (val > DensityThreshTilde)  return palette[1];
            return palette[0];
        }

        /// <summary>
        /// Return the glyph of any debris drifter currently occupying this
        /// cell, or '\0' if none. Drifters wrap south → off-bottom-edge →
        /// re-enter from top, with a small margin so they don't pop in/out
        /// visibly at the boundaries.
        /// </summary>
        private char DebrisGlyphAt(int x, int y, float t)
        {
            if (_debris == null || _debris.Length == 0) return '\0';
            if (_centerXPerRow == null || y < 0 || y >= _centerXPerRow.Length) return '\0';
            int centerX = _centerXPerRow[y];
            if (centerX < 0) return '\0'; // no river on this row

            for (int i = 0; i < _debris.Length; i++)
            {
                var d = _debris[i];
                // pos ∈ [-0.05, 1.05) — HTML's margin lets drifters enter
                // and exit off-screen without a visible snap.
                float pos = ((d.Speed * t + d.Phase) % 1.1f) - 0.05f;
                float debrisY = pos * Zone.Height;
                float targetX = centerX + d.XRel;
                if (Mathf.Abs(y - debrisY) < DebrisCellMatchDistance &&
                    Mathf.Abs(x - targetX) < DebrisCellMatchDistance)
                {
                    return d.Glyph;
                }
            }
            return '\0';
        }

        /// <summary>
        /// Rebuild the cached list of water tile positions from the current zone.
        /// Called on zone load and after full redraws.
        /// </summary>
        private void RefreshWaterCache()
        {
            _waterTilePositions.Clear();
            _waterAdjacentPositions.Clear();
            if (CurrentZone == null) return;

            // Per-row centerline cache for debris targeting. First core
            // cell we see at each row wins; rows with no river stay at -1
            // and debris skips them.
            _centerXPerRow = new int[Zone.Height];
            for (int i = 0; i < _centerXPerRow.Length; i++) _centerXPerRow[i] = -1;

            for (int x = 0; x < Zone.Width; x++)
            {
                for (int y = 0; y < Zone.Height; y++)
                {
                    Cell cell = CurrentZone.GetCell(x, y);
                    if (cell == null) continue;

                    Entity top = cell.GetTopVisibleObject();
                    if (top == null) continue;

                    var render = top.GetPart<RenderPart>();
                    if (render == null || render.RenderString != "~") continue;

                    _waterTilePositions.Add(new Vector2Int(x, y));

                    // Reflection tint: a core cell spills a dark-blue glow
                    // onto its WEST neighbor; a bank cell spills a bright-
                    // cyan glow onto its EAST neighbor. Other orientations
                    // skipped so the effect reads as "sun on two sides of
                    // the channel" rather than "fog around everything."
                    if (!top.HasTag("FlowsSouth")) continue;
                    bool isBank = top.Tags["FlowsSouth"] == "bank";

                    if (!isBank && _centerXPerRow[y] < 0)
                        _centerXPerRow[y] = x;

                    int adjX = isBank ? x + 1 : x - 1;
                    if (CurrentZone.InBounds(adjX, y))
                    {
                        _waterAdjacentPositions.Add(new ReflectionAdjacentTile
                        {
                            X = adjX,
                            Y = y,
                            UseBankPalette = isBank
                        });
                    }
                }
            }

            InitDebrisPool();
        }

        /// <summary>
        /// Randomize the debris drifters on each zone load. Unseeded —
        /// each playthrough sees slightly different leaf timing, and the
        /// pool isn't gameplay-observable so non-determinism is fine.
        /// </summary>
        private void InitDebrisPool()
        {
            _debris = new DebrisDrifter[DebrisCount];
            var rng = new System.Random();
            for (int i = 0; i < DebrisCount; i++)
            {
                _debris[i] = new DebrisDrifter
                {
                    XRel  = (float)(rng.NextDouble() - 0.5) * 1.2f,  // -0.6..0.6
                    Speed = 0.12f + (float)rng.NextDouble() * 0.18f, // 0.12..0.30
                    Phase = (float)rng.NextDouble(),                 // 0..1
                    Glyph = rng.NextDouble() < 0.6 ? 'o' : '.'
                };
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

                // River cells carry a FlowsSouth tag (set by RiverBuilder).
                // Tag VALUE is "core" (center column — deeper palette) or
                // "bank" (outer column — shallower, brighter palette).
                // Village decor (no tag) keeps its stationary shimmer.
                bool isFlowing = top.HasTag("FlowsSouth");
                Vector3Int tilePos = new Vector3Int(x, Zone.Height - 1 - y, 0);

                if (isFlowing)
                {
                    bool isBank = top.Tags["FlowsSouth"] == "bank";

                    // Sample the scalar field and pick glyph + color by
                    // density thresholds. Foam wins over plain density.
                    float val = SampleRiverVal(x, y, _ambientTimer, isBank);
                    char foam = FoamGlyph(val);
                    char glyph = foam != '\0' ? foam : DensityGlyph(val);
                    Color color = WaterColorForVal(val, isBank);

                    // Debris override: a drifting leaf takes precedence
                    // over water glyph AND foam (leaves visibly ride
                    // crests). Color flips to DebrisColor so the leaf
                    // contrasts against the blue channel.
                    char debris = DebrisGlyphAt(x, y, _ambientTimer);
                    if (debris != '\0')
                    {
                        glyph = debris;
                        color = QudColorParser.Parse(DebrisColor);
                    }

                    // Paint the bg tile BEFORE the fg tile so space glyphs
                    // (very-calm cells) reveal dim blue underneath rather
                    // than the bright floor. The tint tracks the current
                    // palette index so it visibly ripples with the wave.
                    PaintWaterBgTint(tilePos, isBank, val);

                    // SetTile must precede SetTileFlags/SetColor because
                    // SetTile re-applies the tile's own color and reverts
                    // the flags to LockColor.
                    Tile glyphTile = CP437TilesetGenerator.GetTile(glyph);
                    if (glyphTile != null)
                        _tilemap.SetTile(tilePos, glyphTile);

                    _tilemap.SetTileFlags(tilePos, TileFlags.None);
                    _tilemap.SetColor(tilePos, color);
                }
                else
                {
                    // Stationary spatial-shimmer for the village decor puddle.
                    float phase = _ambientTimer * 2f + x * 0.7f + y * 1.3f;
                    int colorIndex = ((int)phase) % WaterColors.Length;
                    if (colorIndex < 0) colorIndex += WaterColors.Length;

                    _tilemap.SetTileFlags(tilePos, TileFlags.None);
                    _tilemap.SetColor(tilePos, WaterColors[colorIndex]);
                }
            }

            // Reflection tint: for every cached adjacent (flank) cell,
            // blend the water's current color onto the background tilemap.
            // Uses the same phase formula as the water it reflects so tint
            // and wave stay in lockstep. Runs every frame — worst case 1
            // frame flicker after a RenderZone stomp, imperceptible at
            // 60fps.
            UpdateReflectionTint();

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

            // (Random whitecap sparkles were removed when ripple-driven foam
            // replaced them in the density-ramp sampler — foam now appears
            // where the wave actually peaks, driven by SampleRiverVal.)
        }

        /// <summary>
        /// Paint a dim blue tint onto the background of a river cell itself.
        /// Stronger than the flank reflection tint because the water is the
        /// light source — its own cells should visibly glow even when the
        /// foreground glyph is space (DensityGlyph returns ' ' for very
        /// calm patches).
        /// </summary>
        private void PaintWaterBgTint(Vector3Int tilePos, bool isBank, float val)
        {
            if (_bgTilemap == null) return;

            // Pick palette index by val so the bg tint pulses with the wave.
            Color[] palette = isBank ? WaterBankColors : WaterCoreColors;
            int idx = val > DensityThreshMedium ? 2 : val > DensityThreshTilde ? 1 : 0;
            Color water = palette[idx];
            Color darkened = QudColorParser.DarkenForBackground(water);
            Color tinted = Color.Lerp(BackgroundColor, darkened, WaterBgTintStrength);

            if (_bgTilemap.GetTile(tilePos) == null)
            {
                Tile solid = CP437TilesetGenerator.GetTile(CP437TilesetGenerator.SolidBlock);
                if (solid != null) _bgTilemap.SetTile(tilePos, solid);
            }
            _bgTilemap.SetTileFlags(tilePos, TileFlags.None);
            _bgTilemap.SetColor(tilePos, tinted);
        }

        /// <summary>
        /// Paint a phase-aware bluish tint onto the background tilemap for
        /// every cached flank cell. Runs every frame; O(count) where count
        /// is roughly the river's length. An empty-floor flank cell needs
        /// a SolidBlock tile painted once before SetColor does anything —
        /// subsequent frames just update color.
        /// </summary>
        private void UpdateReflectionTint()
        {
            if (_bgTilemap == null || CurrentZone == null) return;

            Tile solid = CP437TilesetGenerator.GetTile(CP437TilesetGenerator.SolidBlock);
            if (solid == null) return;

            for (int i = 0; i < _waterAdjacentPositions.Count; i++)
            {
                var adj = _waterAdjacentPositions[i];
                Cell cell = CurrentZone.GetCell(adj.X, adj.Y);
                if (cell == null || !cell.IsVisible) continue;

                // Sample the same scalar field the adjacent water cell
                // uses, so tint and wave stay in lockstep. We pass the
                // flank's own (x, y) — the noise term's cross-flow shift
                // between adjacent columns is imperceptible, not worth
                // recomputing the water cell's exact coords.
                Color[] palette = adj.UseBankPalette ? WaterBankColors : WaterCoreColors;
                float val = SampleRiverVal(adj.X, adj.Y, _ambientTimer, adj.UseBankPalette);
                int colorIndex = val > DensityThreshMedium ? 2
                               : val > DensityThreshTilde  ? 1
                               : 0;
                Color waterColor = palette[colorIndex];
                Color darkened = QudColorParser.DarkenForBackground(waterColor);
                Color tinted = Color.Lerp(BackgroundColor, darkened, ReflectionTintStrength);

                Vector3Int tilePos = new Vector3Int(adj.X, Zone.Height - 1 - adj.Y, 0);

                // SolidBlock provides the tintable surface on otherwise
                // empty floor cells. Only paint once per cell — after
                // the first frame the tile is already set (SetTile is
                // idempotent but wasteful if called every frame on every
                // flank).
                if (_bgTilemap.GetTile(tilePos) == null)
                    _bgTilemap.SetTile(tilePos, solid);

                _bgTilemap.SetTileFlags(tilePos, TileFlags.None);
                _bgTilemap.SetColor(tilePos, tinted);
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
