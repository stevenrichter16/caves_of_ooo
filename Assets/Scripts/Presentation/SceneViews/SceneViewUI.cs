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

        [Header("Scene canvas")]
        [Tooltip("Full canvas width in tiles. The campfire scene uses 80.")]
        public int CanvasWidth = 80;
        [Tooltip("Full canvas height in tiles. The campfire scene uses 28.")]
        public int CanvasHeight = 28;
        [Tooltip("Top-left corner of the scene render area in world tile coordinates. (0, 0) is typical for full-screen.")]
        public Vector2Int CanvasOrigin = Vector2Int.zero;

        private SceneRenderer _sceneRenderer;
        private bool _isRendering;

        private void Awake()
        {
            EnsureRenderer();
            if (ZoneRenderer == null)
                ZoneRenderer = FindObjectOfType<ZoneRenderer>();
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

            _isRendering = true;
            if (ZoneRenderer != null) ZoneRenderer.Paused = true;
            // Render once immediately so the player sees the scene without
            // a 1-frame delay.
            RenderToTilemap();
        }

        private void HandleDeactivated()
        {
            _isRendering = false;
            ClearTilemap();
            if (ZoneRenderer != null) ZoneRenderer.Paused = false;
        }

        private void Update()
        {
            // M2 is a static composition — re-rendering each frame is
            // redundant but harmless. M3 will introduce per-frame
            // animation that requires Update-driven re-render.
            if (!_isRendering || Tilemap == null) return;
            RenderToTilemap();
        }

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

                    if (cell.Glyph == ' ' || cell.Glyph == '\0')
                    {
                        Tilemap.SetTile(pos, null);
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
