using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Renders the dedicated gameplay hotbar strip.
    /// </summary>
    public sealed class GameplayHotbarRenderer
    {
        private static readonly Color HotbarBgColor = Color.black;
        private static readonly Color EmptyColor = QudColorParser.DarkGray;

        private readonly Tilemap _tilemap;
        private readonly Tilemap _backgroundTilemap;

        public GameplayHotbarRenderer(Tilemap tilemap, Tilemap backgroundTilemap)
        {
            _tilemap = tilemap;
            _backgroundTilemap = backgroundTilemap;
        }

        public void Clear()
        {
            if (_tilemap != null)
            {
                _tilemap.ClearAllTiles();
                PerformanceDiagnostics.RecordTilemapClear();
            }

            if (_backgroundTilemap != null)
            {
                _backgroundTilemap.ClearAllTiles();
                PerformanceDiagnostics.RecordTilemapClear();
            }
        }

        public void Render(HotbarSnapshot snapshot, Camera camera)
        {
            using (PerformanceMarkers.Ui.HotbarRender.Auto())
            {
                PerformanceDiagnostics.RecordHotbarRender();
                Clear();

                if (_tilemap == null || _backgroundTilemap == null || camera == null || !camera.enabled)
                    return;

                DrawBackground();
                DrawText(1, GameplayHotbarLayout.GridHeight - 1, snapshot?.Title ?? "GRIMOIRES", QudColorParser.White, 18);
                DrawRightAligned(
                    GameplayHotbarLayout.GridWidth - 2,
                    GameplayHotbarLayout.GridHeight - 1,
                    snapshot?.HintText ?? string.Empty,
                    QudColorParser.DarkGray,
                    GameplayHotbarLayout.GridWidth - 20);
                DrawText(1, GameplayHotbarLayout.GridHeight - 2, snapshot?.SummaryText ?? string.Empty, QudColorParser.Gray, GameplayHotbarLayout.GridWidth - 2);

                if (snapshot?.Slots == null)
                    return;

                for (int i = 0; i < snapshot.Slots.Count; i++)
                    DrawSlot(snapshot.Slots[i]);
            }
        }

        private void DrawBackground()
        {
            Tile blockTile = CP437TilesetGenerator.GetTextTile(CP437TilesetGenerator.SolidBlock);
            if (blockTile == null)
                return;

            for (int y = 0; y < GameplayHotbarLayout.GridHeight; y++)
            {
                for (int x = 0; x < GameplayHotbarLayout.GridWidth; x++)
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);
                    _backgroundTilemap.SetTile(pos, blockTile);
                    _backgroundTilemap.SetTileFlags(pos, TileFlags.None);
                    _backgroundTilemap.SetColor(pos, HotbarBgColor);
                }
            }
        }

        private void DrawSlot(HotbarSlotSnapshot slot)
        {
            int originX = slot.SlotIndex * GameplayHotbarLayout.SlotWidth;
            const int slotWidth = GameplayHotbarLayout.SlotWidth;
            const int slotHeight = 4;
            Color borderColor = slot.Pending
                ? QudColorParser.BrightCyan
                : (slot.Selected ? QudColorParser.White : QudColorParser.DarkGray);
            Color accentColor = !string.IsNullOrEmpty(slot.AccentColorCode)
                ? QudColorParser.Parse(slot.AccentColorCode)
                : QudColorParser.White;
            Color labelColor = slot.Occupied
                ? (slot.Usable ? accentColor : QudColorParser.BrightRed)
                : EmptyColor;

            DrawBox(originX, 0, slotWidth, slotHeight, borderColor);
            DrawText(originX + 1, 2, "[" + slot.Hotkey + "]", slot.Selected ? QudColorParser.White : QudColorParser.Gray, 3);

            if (!slot.Occupied)
            {
                DrawText(originX + 1, 1, "empty", EmptyColor, slotWidth - 2);
                return;
            }

            DrawChar(originX + slotWidth - 2, 2, slot.Glyph, accentColor);
            DrawText(originX + 1, 1, slot.ShortName, labelColor, slotWidth - 2);
            if (slot.CooldownRemaining > 0)
                DrawText(originX + slotWidth - 3, 1, slot.CooldownRemaining.ToString(), QudColorParser.BrightRed, 2);
        }

        private void DrawBox(int x, int y, int w, int h, Color color)
        {
            DrawChar(x, y + h - 1, CP437TilesetGenerator.BoxTopLeft, color);
            DrawChar(x + w - 1, y + h - 1, CP437TilesetGenerator.BoxTopRight, color);
            DrawChar(x, y, CP437TilesetGenerator.BoxBottomLeft, color);
            DrawChar(x + w - 1, y, CP437TilesetGenerator.BoxBottomRight, color);

            for (int i = 1; i < w - 1; i++)
            {
                DrawChar(x + i, y + h - 1, CP437TilesetGenerator.BoxHorizontal, color);
                DrawChar(x + i, y, CP437TilesetGenerator.BoxHorizontal, color);
            }

            for (int i = 1; i < h - 1; i++)
            {
                DrawChar(x, y + i, CP437TilesetGenerator.BoxVertical, color);
                DrawChar(x + w - 1, y + i, CP437TilesetGenerator.BoxVertical, color);
            }
        }

        private void DrawRightAligned(int rightX, int y, string text, Color color, int maxWidth)
        {
            if (string.IsNullOrEmpty(text) || maxWidth <= 0)
                return;

            int safeLength = Mathf.Min(text.Length, maxWidth);
            int startX = Mathf.Max(0, rightX - safeLength + 1);
            DrawText(startX, y, text, color, maxWidth);
        }

        private void DrawText(int x, int y, string text, Color color, int maxChars)
        {
            if (_tilemap == null || string.IsNullOrEmpty(text) || maxChars <= 0)
                return;

            int len = Mathf.Min(text.Length, maxChars);
            for (int i = 0; i < len; i++)
            {
                char c = text[i];
                if (c == ' ')
                    continue;

                DrawChar(x + i, y, c, color);
            }
        }

        private void DrawChar(int x, int y, char c, Color color)
        {
            Tile tile = CP437TilesetGenerator.GetTextTile(c);
            if (tile == null)
                return;

            Vector3Int pos = new Vector3Int(x, y, 0);
            _tilemap.SetTile(pos, tile);
            _tilemap.SetTileFlags(pos, TileFlags.None);
            _tilemap.SetColor(pos, color);
        }
    }
}
