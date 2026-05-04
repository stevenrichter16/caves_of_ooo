using CavesOfOoo.Rendering;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// MonoBehaviour wrapper that drives the <see cref="SceneRenderer"/>
    /// and writes its frame buffer onto a Unity Tilemap. Activates/
    /// deactivates in response to <see cref="SceneViewManager"/> events,
    /// pausing the underlying <see cref="ZoneRenderer"/> while a scene
    /// is open.
    ///
    /// Plan: Docs/Plans/SCENE_VIEW_SYSTEM_IMPLEMENTATION_PLAN.md M2.
    /// </summary>
    public class SceneViewUI : MonoBehaviour
    {
        [Header("Render targets")]
        [Tooltip("Tilemap that the scene's frame buffer is written into. Must be attached to a Grid + Camera that can render full-screen.")]
        public Tilemap Tilemap;

        [Tooltip("Optional reference to the world's ZoneRenderer. If null, it is auto-discovered via FindObjectOfType in Awake. The Paused flag is toggled on scene enter/exit.")]
        public ZoneRenderer ZoneRenderer;

        [Tooltip("Optional reference to the gameplay camera's CameraFollow controller. If null, auto-discovered via FindObjectOfType in Awake. While a scene is open, the camera is switched to fullscreen UI view (SetUIView) so the SidebarCamera and HotbarCamera (both clear to Color.black) don't paint as black bars on the right and bottom of the scene composition.")]
        public CameraFollow CameraFollow;

        [Header("Scene canvas")]
        [Tooltip("Full canvas width in tiles. The campfire scene uses 80.")]
        public int CanvasWidth = 80;
        [Tooltip("Full canvas height in tiles. The campfire scene uses 28.")]
        public int CanvasHeight = 28;
        [Tooltip("Top-left corner of the scene render area in world tile coordinates. (0, 0) is typical for full-screen.")]
        public Vector2Int CanvasOrigin = Vector2Int.zero;

        private SceneRenderer _sceneRenderer;
        private bool _isRendering;
        // M4: when true, Update is driving the exit (reverse) dissolve and
        // will tear down rendering once the dissolve completes.
        private bool _exitDissolveActive;

        private void Awake()
        {
            EnsureRenderer();
            if (ZoneRenderer == null)
                ZoneRenderer = FindObjectOfType<ZoneRenderer>();
            if (CameraFollow == null)
                CameraFollow = FindObjectOfType<CameraFollow>();
        }

        private void EnsureRenderer()
        {
            if (_sceneRenderer == null)
                _sceneRenderer = new SceneRenderer(CanvasWidth, CanvasHeight);
        }

        private void OnEnable()
        {
            EnsureRenderer();
            SceneViewManager.OnActivated += HandleActivated;
            SceneViewManager.OnDeactivated += HandleDeactivated;
        }

        private void OnDisable()
        {
            SceneViewManager.OnActivated -= HandleActivated;
            SceneViewManager.OnDeactivated -= HandleDeactivated;
        }

        private void HandleActivated(string sceneID)
        {
            // M2 only renders the Campfire scene. M5 will resolve sceneID
            // to a SceneViewData asset and dispatch to the appropriate
            // composition.
            if (sceneID != "Campfire") return;

            // Reconstruct the renderer so re-entries restart animation (sparks
            // empty, t=0, fresh stars). Per plan §"Open Design Questions" #2:
            // scenes are moments; revisits are new moments.
            _sceneRenderer = new SceneRenderer(CanvasWidth, CanvasHeight);
            _sceneRenderer.StartDissolve(reverse: false);
            _isRendering = true;
            _exitDissolveActive = false;
            // Leave ZoneRenderer unpaused while the iris opens — the world
            // tilemap below shows through cleared overlay cells, giving a
            // true "world → scene" transition. We pause it once the forward
            // dissolve completes (scene fully covers the screen anyway).
            if (ZoneRenderer != null) ZoneRenderer.Paused = false;
            // Bug A fix: switch the gameplay camera to its full-screen UI
            // view layout. Without this, the SidebarCamera and HotbarCamera
            // (each clearing to Color.black) keep painting their rects on
            // the right and bottom of the screen and look like black bars
            // around the otherwise-correct scene. SetUIView also disables
            // those two cameras and centers the gameplay camera on the
            // scene canvas (gridWidth/2, gridHeight/2) with an ortho size
            // sized to fit the scene. RestoreGameView (called once the
            // reverse dissolve fully completes, in Update) puts the split
            // sidebar/hotbar layout back.
            if (CameraFollow != null) CameraFollow.SetUIView(CanvasWidth, CanvasHeight);
            RenderToTilemap();
        }

        private void HandleDeactivated()
        {
            // M4: don't tear down immediately — start the reverse dissolve
            // and keep rendering until it completes. Update() handles the
            // final cleanup once IsDissolving clears.
            if (!_isRendering || _sceneRenderer == null) return;
            _sceneRenderer.StartDissolve(reverse: true);
            _exitDissolveActive = true;
            // Unpause the world so it's visible underneath as the scene
            // dissolves away.
            if (ZoneRenderer != null) ZoneRenderer.Paused = false;
        }

        private void Update()
        {
            if (!_isRendering || Tilemap == null) return;
            _sceneRenderer.Tick(Time.deltaTime);
            if (_sceneRenderer.IsDissolving)
                _sceneRenderer.UpdateDissolve(Time.deltaTime);
            RenderToTilemap();

            // Post-render dissolve transitions
            if (!_sceneRenderer.IsDissolving)
            {
                if (_exitDissolveActive)
                {
                    // DISSOLVING_OUT → IDLE: tear down rendering.
                    _exitDissolveActive = false;
                    _isRendering = false;
                    ClearTilemap();
                    // Bug A fix (paired with SetUIView in HandleActivated):
                    // restore the split sidebar/hotbar layout AFTER the
                    // reverse dissolve completes — not on HandleDeactivated,
                    // because then the dissolve would play with the wrong
                    // camera framing (the world would jump-cut from scene
                    // framing back to player framing mid-iris).
                    if (CameraFollow != null) CameraFollow.RestoreGameView();
                }
                else if (ZoneRenderer != null && !ZoneRenderer.Paused)
                {
                    // DISSOLVING_IN → ACTIVE: scene now fully covers; pause
                    // the world tilemap to skip its render work.
                    ZoneRenderer.Paused = true;
                }
            }
        }

        // CP437 0xDB ('█' solid block) — used to paint an opaque background
        // for cells the scene leaves blank (' '), so the world tilemap below
        // the overlay is fully occluded while the scene is active. Cleared-by-
        // dissolve cells use the '\0' sentinel and remain transparent so the
        // world peeks through during the radial transition. See SceneRenderer
        // .DrawDissolveOverlay for the producer side. Using Û explicitly
        // (not the literal char) to avoid encoding ambiguity in source.
        private const char SCENE_BACKGROUND_GLYPH = '\u00DB';

        private void RenderToTilemap()
        {
            _sceneRenderer.RenderCampfire();

            for (int y = 0; y < _sceneRenderer.Height; y++)
            {
                for (int x = 0; x < _sceneRenderer.Width; x++)
                {
                    var cell = _sceneRenderer.GetCell(x, y);
                    var pos = new Vector3Int(
                        CanvasOrigin.x + x,
                        // Unity tilemaps grow upward in y; the scene grid
                        // grows downward. Flip y to match.
                        CanvasOrigin.y + (_sceneRenderer.Height - 1 - y),
                        0);

                    // '\0' = dissolve-cleared: transparent, world peeks through.
                    if (cell.Glyph == '\0')
                    {
                        Tilemap.SetTile(pos, null);
                        continue;
                    }

                    // ' ' = scene-blank: paint an opaque background block so
                    // the world below doesn't bleed through during the active
                    // (non-dissolving) phase. Without this, the many empty
                    // cells in the campfire composition (sky between stars,
                    // ground outside the firelight pool, areas around tent/
                    // logs) would all be transparent and the player would
                    // see the world map under the scene.
                    if (cell.Glyph == ' ')
                    {
                        var bgTile = CP437TilesetGenerator.GetTile(SCENE_BACKGROUND_GLYPH);
                        Tilemap.SetTile(pos, bgTile);
                        Tilemap.SetTileFlags(pos, TileFlags.None);
                        Tilemap.SetColor(pos, Color.black);
                        continue;
                    }

                    var tile = CP437TilesetGenerator.GetTile(cell.Glyph);
                    Tilemap.SetTile(pos, tile);
                    Tilemap.SetTileFlags(pos, TileFlags.None);
                    Tilemap.SetColor(pos, cell.Foreground);
                }
            }
        }

        private void ClearTilemap()
        {
            if (Tilemap == null) return;
            for (int y = 0; y < _sceneRenderer.Height; y++)
            {
                for (int x = 0; x < _sceneRenderer.Width; x++)
                {
                    var pos = new Vector3Int(
                        CanvasOrigin.x + x,
                        CanvasOrigin.y + (_sceneRenderer.Height - 1 - y),
                        0);
                    Tilemap.SetTile(pos, null);
                }
            }
        }
    }
}
