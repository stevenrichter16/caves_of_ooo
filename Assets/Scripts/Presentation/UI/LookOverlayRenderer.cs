using CavesOfOoo.Core;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Renders a compact inspect panel on a dedicated narrow-text overlay tilemap.
    /// </summary>
    public sealed class LookOverlayRenderer
    {
        private readonly Tilemap _tilemap;
        private readonly Transform _gridTransform;
        private readonly float _referenceZoom;

        public bool IsVisible { get; private set; }

        public LookOverlayRenderer(Tilemap tilemap, Transform gridTransform, float referenceZoom)
        {
            _tilemap = tilemap;
            _gridTransform = gridTransform;
            _referenceZoom = referenceZoom;
        }

        public void Render(LookSnapshot snapshot, Camera camera)
        {
            Clear();

            if (_tilemap == null || snapshot == null || camera == null)
                return;

            float scale = camera.orthographicSize / _referenceZoom;
            _gridTransform.localScale = new Vector3(scale, scale, 1f);

            float worldTop = camera.transform.position.y + camera.orthographicSize;
            int topTileY = Mathf.FloorToInt((worldTop - 0.5f * scale) / scale);

            float worldLeft = camera.transform.position.x - camera.orthographicSize * camera.aspect;
            int startX = Mathf.CeilToInt(worldLeft / (0.5f * scale));

            float worldWidth = camera.orthographicSize * camera.aspect * 2f;
            int maxChars = Mathf.Max(1, Mathf.FloorToInt(worldWidth / (0.5f * scale)));

            DrawLine(startX, topTileY, snapshot.Header, QudColorParser.White, maxChars);
            DrawLine(startX, topTileY - 2, snapshot.Summary, QudColorParser.Gray, maxChars);

            for (int i = 0; i < snapshot.DetailLines.Count && i < 2; i++)
                DrawLine(startX, topTileY - ((i + 2) * 2), snapshot.DetailLines[i], QudColorParser.DarkGray, maxChars);

            IsVisible = true;
        }

        public void Clear()
        {
            IsVisible = false;
            if (_tilemap != null)
                _tilemap.ClearAllTiles();
        }

        private void DrawLine(int x, int y, string text, Color color, int maxChars)
        {
            if (_tilemap == null || string.IsNullOrEmpty(text))
                return;

            int len = Mathf.Min(text.Length, maxChars);
            for (int i = 0; i < len; i++)
            {
                char c = text[i];
                if (c == ' ')
                    continue;

                Tile tile = CP437TilesetGenerator.GetTextTile(c);
                if (tile == null)
                    continue;

                Vector3Int tilePos = new Vector3Int(x + i, y, 0);
                _tilemap.SetTile(tilePos, tile);
                _tilemap.SetTileFlags(tilePos, TileFlags.None);
                _tilemap.SetColor(tilePos, color);
            }
        }
    }
}
