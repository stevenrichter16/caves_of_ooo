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
        // Phase 10 — dedicated thought-overlay grid + tilemaps (narrow text,
        // own layer + camera, mirrors the sidebar setup).
        private Grid _thoughtOverlayGrid;
        private Tilemap _thoughtOverlayBgTilemap;
        private Tilemap _thoughtOverlayTilemap;
        private Transform _thoughtOverlayGridTransform;
        private Material _thoughtOverlayUiMaterial;
        private Camera _thoughtOverlayCamera;
        private AsciiFxRenderer _asciiFxRenderer;
        private CampfireEmberRenderer _campfireEmberRenderer;
        private WorldCursorRenderer _worldCursorRenderer;
        private GameplaySidebarRenderer _sidebarRenderer;

        /// <summary>
        /// Phase 10 — second <see cref="GameplaySidebarRenderer"/> instance
        /// that renders the standalone thought overlay onto the world
        /// tilemap. Intentionally the same class as the main logger/sidebar
        /// renderer (per the design followup request) — one class serves
        /// both UIs; the only difference is the snapshot they receive.
        /// </summary>
        private GameplaySidebarRenderer _thoughtOverlayRenderer;
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

        /// <summary>Phase 10 — accessor for the thought-overlay camera set via <see cref="SetThoughtOverlayCamera"/>.</summary>
        public Camera ThoughtOverlayCamera => _thoughtOverlayCamera;

        public Tilemap ThoughtOverlayTilemap => _thoughtOverlayTilemap;
        public Tilemap ThoughtOverlayBgTilemap => _thoughtOverlayBgTilemap;
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

            // Phase 10 — dedicated Grid + tilemaps for the thought overlay,
            // mirroring the sidebar setup precisely: own layer, own cellSize
            // (0.5x1 narrow text), own TilemapRenderer material. Will be
            // rendered by its own Camera assigned via SetThoughtOverlayCamera.
            var thoughtOverlayGridObj = new GameObject("ThoughtOverlayGrid");
            _thoughtOverlayGridTransform = thoughtOverlayGridObj.transform;
            GameplayRenderLayers.SetLayerRecursive(thoughtOverlayGridObj, GameplayRenderLayers.ThoughtOverlayLayer);
            _thoughtOverlayGrid = thoughtOverlayGridObj.AddComponent<Grid>();
            _thoughtOverlayGrid.cellSize = new Vector3(0.5f, 1f, 0f);

            var thoughtOverlayBgObj = new GameObject("ThoughtOverlayBgTilemap");
            thoughtOverlayBgObj.transform.SetParent(thoughtOverlayGridObj.transform);
            GameplayRenderLayers.SetLayerRecursive(thoughtOverlayBgObj, GameplayRenderLayers.ThoughtOverlayLayer);
            _thoughtOverlayBgTilemap = thoughtOverlayBgObj.AddComponent<Tilemap>();
            var thoughtOverlayBgRenderer = thoughtOverlayBgObj.AddComponent<TilemapRenderer>();
            ConfigureThoughtOverlayTilemapRenderer(thoughtOverlayBgRenderer, 2);

            var thoughtOverlayTmObj = new GameObject("ThoughtOverlayTilemap");
            thoughtOverlayTmObj.transform.SetParent(thoughtOverlayGridObj.transform);
            GameplayRenderLayers.SetLayerRecursive(thoughtOverlayTmObj, GameplayRenderLayers.ThoughtOverlayLayer);
            _thoughtOverlayTilemap = thoughtOverlayTmObj.AddComponent<Tilemap>();
            var thoughtOverlayTmRenderer = thoughtOverlayTmObj.AddComponent<TilemapRenderer>();
            ConfigureThoughtOverlayTilemapRenderer(thoughtOverlayTmRenderer, 3);

            // Same class as the main sidebar — one class serves both UIs.
            // ownsTilemap:true because the overlay owns its dedicated tilemaps
            // (no shared writers), so Clear() on each Render is safe.
            _thoughtOverlayRenderer = new GameplaySidebarRenderer(
                _thoughtOverlayTilemap, _thoughtOverlayBgTilemap,
                _thoughtOverlayGridTransform, MessageReferenceZoom);

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
            DestroyOwnedMaterial(ref _thoughtOverlayUiMaterial);
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

        /// <summary>
        /// Phase 10 — width in narrow-text chars of the thought overlay
        /// panel. Mirrors SidebarWidthChars for the main sidebar.
        /// </summary>
        public int ThoughtOverlayWidthChars => 32;

        /// <summary>
        /// Phase 10 — render the standalone thought overlay to its own
        /// dedicated tilemaps via its own dedicated Camera. Called from
        /// the main render loop alongside the sidebar; gated by
        /// <see cref="ShowThoughtLog"/>. When the toggle is off, we clear
        /// once (so stale tiles don't linger) and disable the camera so it
        /// stops rendering every frame.
        /// </summary>
        private void RenderThoughtOverlay()
        {
            if (_thoughtOverlayRenderer == null) return;

            if (!ShowThoughtLog || _thoughtOverlayCamera == null)
            {
                if (_thoughtOverlayRenderer.IsVisible)
                    _thoughtOverlayRenderer.Clear();
                if (_thoughtOverlayCamera != null && _thoughtOverlayCamera.enabled)
                    _thoughtOverlayCamera.enabled = false;
                return;
            }

            if (!_thoughtOverlayCamera.enabled)
                _thoughtOverlayCamera.enabled = true;

            var thoughtSnapshot = SidebarStateBuilder.BuildThoughtOverlay(CurrentZone);
            _thoughtOverlayRenderer.Render(
                thoughtSnapshot, _thoughtOverlayCamera, ThoughtOverlayWidthChars,
                flashActive: false, flashT: 0f);
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

                SidebarSnapshot snapshot = SidebarStateBuilder.Build(
                    PlayerEntity, CurrentZone, _currentLookSnapshot);
                _sidebarRenderer?.Render(snapshot, sidebarCamera, SidebarWidthChars, flashActive, flashT);

                // Phase 10 — sibling render pass for the thought overlay
                // (same GameplaySidebarRenderer class, different instance +
                // tilemaps + camera). Gated by ShowThoughtLog; short-circuits
                // cheaply when off.
                RenderThoughtOverlay();
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

        private void ConfigureThoughtOverlayTilemapRenderer(TilemapRenderer renderer, int sortingOrder)
        {
            if (renderer == null)
                return;

            renderer.sortingOrder = sortingOrder;

            Material uiMaterial = GetUnlitSpriteMaterial(ref _thoughtOverlayUiMaterial, "ThoughtOverlayUI-Unlit");
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

        /// <summary>
        /// Phase 10 — assign the thought-overlay camera created by
        /// <see cref="Bootstrap.GameBootstrap"/>. Called once at bootstrap;
        /// the camera's culling mask, viewport rect, and URP stack position
        /// are configured by <see cref="Cameras.CameraFollow"/>.
        /// </summary>
        public void SetThoughtOverlayCamera(Camera thoughtOverlayCamera)
        {
            _thoughtOverlayCamera = thoughtOverlayCamera;
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
