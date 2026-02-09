using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// MonoBehaviour that renders a Zone onto a Unity Tilemap.
    /// Reads each cell's top visible entity's RenderPart and draws
    /// the appropriate glyph in the appropriate color.
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
        /// Background color for empty cells.
        /// </summary>
        public Color BackgroundColor = new Color(0.05f, 0.05f, 0.05f);

        private Tilemap _tilemap;
        private bool _dirty = true;

        private void Awake()
        {
            _tilemap = GetComponent<Tilemap>();
        }

        /// <summary>
        /// Set the zone to render. Triggers a full redraw.
        /// </summary>
        public void SetZone(Zone zone)
        {
            CurrentZone = zone;
            _dirty = true;
        }

        /// <summary>
        /// Mark the display as needing a refresh.
        /// Call this after entities move, are added/removed, etc.
        /// </summary>
        public void MarkDirty()
        {
            _dirty = true;
        }

        private void LateUpdate()
        {
            if (_dirty && CurrentZone != null)
            {
                RenderZone();
                _dirty = false;
            }
        }

        /// <summary>
        /// Full redraw of the zone onto the tilemap.
        /// </summary>
        public void RenderZone()
        {
            if (CurrentZone == null || _tilemap == null) return;

            _tilemap.ClearAllTiles();

            for (int x = 0; x < Zone.Width; x++)
            {
                for (int y = 0; y < Zone.Height; y++)
                {
                    RenderCell(x, y);
                }
            }
        }

        private void RenderCell(int x, int y)
        {
            Cell cell = CurrentZone.GetCell(x, y);
            if (cell == null) return;

            // Unity tilemap Y is inverted relative to our grid (0=bottom in Unity, 0=top in roguelike)
            Vector3Int tilePos = new Vector3Int(x, Zone.Height - 1 - y, 0);

            Entity topEntity = cell.GetTopVisibleObject();
            if (topEntity == null)
            {
                // Empty cell — draw a dark dot
                Tile bgTile = CP437TilesetGenerator.GetTile('.');
                _tilemap.SetTile(tilePos, bgTile);
                _tilemap.SetTileFlags(tilePos, TileFlags.None);
                _tilemap.SetColor(tilePos, BackgroundColor);
                return;
            }

            RenderPart render = topEntity.GetPart<RenderPart>();
            if (render == null) return;

            // Get the glyph character
            char glyph = '.';
            if (!string.IsNullOrEmpty(render.RenderString) && render.RenderString.Length > 0)
                glyph = render.RenderString[0];

            // Get the tile
            Tile tile = CP437TilesetGenerator.GetTile(glyph);
            if (tile == null) return;

            _tilemap.SetTile(tilePos, tile);
            _tilemap.SetTileFlags(tilePos, TileFlags.None);

            // Parse and apply color
            Color color = QudColorParser.Parse(render.ColorString);
            _tilemap.SetColor(tilePos, color);
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
    }
}
