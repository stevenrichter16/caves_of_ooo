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

        /// <summary>
        /// When true, zone rendering is suppressed (e.g. inventory screen is open).
        /// </summary>
        public bool Paused;

        /// <summary>
        /// Number of message log lines shown at the bottom of the screen.
        /// </summary>
        public int MessageLineCount = 4;

        /// <summary>
        /// Reference orthographic size at which message text appears at 1:1 scale.
        /// At other zoom levels, message text scales proportionally.
        /// </summary>
        public float MessageReferenceZoom = 12.5f;

        private Tilemap _tilemap;
        private Tilemap _msgTilemap;
        private Transform _msgGridTransform;
        private bool _dirty = true;
        private int _lastMessageCount = -1;

        private void Awake()
        {
            _tilemap = GetComponent<Tilemap>();

            // Create a separate tilemap for messages with narrow half-width cells
            var msgGridObj = new GameObject("MessageGrid");
            _msgGridTransform = msgGridObj.transform;
            var msgGrid = msgGridObj.AddComponent<Grid>();
            msgGrid.cellSize = new Vector3(0.5f, 1f, 0f);

            var msgTmObj = new GameObject("MessageTilemap");
            msgTmObj.transform.SetParent(msgGridObj.transform);
            _msgTilemap = msgTmObj.AddComponent<Tilemap>();
            var msgRenderer = msgTmObj.AddComponent<TilemapRenderer>();
            msgRenderer.sortingOrder = 1; // render above game world
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
            if (Paused) return;

            // Re-render messages if new ones arrived (even without a full dirty)
            bool newMessages = MessageLog.Count != _lastMessageCount;

            if (_dirty && CurrentZone != null)
            {
                RenderZone();
                RenderMessages();
                _dirty = false;
                _lastMessageCount = MessageLog.Count;
            }
            else if (newMessages)
            {
                RenderMessages();
                _lastMessageCount = MessageLog.Count;
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
        /// Render recent messages at the bottom of the visible screen
        /// using the narrow-text message tilemap.
        /// </summary>
        private void RenderMessages()
        {
            if (_msgTilemap == null || MessageLineCount <= 0) return;

            _msgTilemap.ClearAllTiles();

            var recent = MessageLog.GetRecent(MessageLineCount);
            if (recent.Count == 0) return;

            var cam = Camera.main;
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

            // Render newest message at screen bottom, older ones above with spacer rows
            for (int i = 0; i < MessageLineCount; i++)
            {
                int tileY = bottomTileY + (i * 2);
                int msgIndex = recent.Count - 1 - i;

                if (msgIndex >= 0)
                {
                    Color color = i == 0 ? QudColorParser.White : QudColorParser.Gray;
                    DrawMsgText(startX, tileY, recent[msgIndex], color, maxChars);
                }
            }
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
    }
}
