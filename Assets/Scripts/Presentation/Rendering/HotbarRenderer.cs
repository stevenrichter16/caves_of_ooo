using System.Collections.Generic;
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
        private static readonly TileBase[] BlankRow = new TileBase[GameplayHotbarLayout.GridWidth];
        private static readonly TileBase[] BlankSlot = new TileBase[GameplayHotbarLayout.SlotWidth * 4];
        private readonly List<HotbarSlotSnapshot> _lastSlots =
            new List<HotbarSlotSnapshot>(GameplayHotbarLayout.SlotCount);
        private bool _hasContent;
        private bool _needsInitialClear = true;
        private string _lastTitle;
        private string _lastSummary;
        private string _lastHint;

        public GameplayHotbarRenderer(Tilemap tilemap, Tilemap backgroundTilemap)
        {
            _tilemap = tilemap;
            _backgroundTilemap = backgroundTilemap;
        }

        public void Clear()
        {
            // Explicit lifecycle clears always clear supplied maps and invalidate
            // the retained presentation, even when the next snapshot is equal.
            _hasContent = false;
            _needsInitialClear = false;
            _lastSlots.Clear();
            _lastTitle = _lastSummary = _lastHint = null;
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
                if (_tilemap == null || _backgroundTilemap == null || camera == null || !camera.enabled)
                {
                    if (_hasContent || _needsInitialClear)
                        Clear();
                    return;
                }

                string title = snapshot?.Title ?? "GRIMOIRES";
                string summary = snapshot?.SummaryText ?? string.Empty;
                string hint = snapshot?.HintText ?? string.Empty;
                IReadOnlyList<HotbarSlotSnapshot> slots = snapshot?.Slots;
                int slotCount = slots?.Count ?? 0;
                bool headerChanged = !_hasContent || title != _lastTitle || hint != _lastHint;
                bool summaryChanged = !_hasContent || summary != _lastSummary;
                bool slotsChanged = !_hasContent || SlotsChanged(slots, slotCount);
                if (!headerChanged && !summaryChanged && !slotsChanged)
                    return;

                PerformanceDiagnostics.RecordHotbarRender();
                bool firstPaint = !_hasContent;
                if (firstPaint)
                {
                    if (_needsInitialClear)
                        Clear();
                    DrawBackground();
                }
                if (headerChanged)
                {
                    if (!firstPaint)
                        ClearRow(GameplayHotbarLayout.GridHeight - 1);
                    DrawText(1, GameplayHotbarLayout.GridHeight - 1, title, QudColorParser.White, 18);
                    DrawRightAligned(
                        GameplayHotbarLayout.GridWidth - 2,
                        GameplayHotbarLayout.GridHeight - 1,
                        hint,
                        QudColorParser.DarkGray,
                        GameplayHotbarLayout.GridWidth - 20);
                }
                if (summaryChanged)
                {
                    if (!firstPaint)
                        ClearRow(GameplayHotbarLayout.GridHeight - 2);
                    DrawText(1, GameplayHotbarLayout.GridHeight - 2, summary, QudColorParser.Gray, GameplayHotbarLayout.GridWidth - 2);
                }
                if (slotsChanged)
                {
                    // Ordinary snapshots have ten ordered, non-overlapping slots.
                    // Arbitrary callers may provide sparse/reordered/duplicate
                    // indices, so replay those in order after clearing old boxes.
                    bool granular = !firstPaint && HasOrderedSlots(_lastSlots) &&
                        HasOrderedSlots(slots) && _lastSlots.Count == slotCount;
                    if (!firstPaint && !granular)
                    {
                        for (int i = 0; i < _lastSlots.Count; i++)
                            ClearSlot(_lastSlots[i].SlotIndex);
                        for (int i = 0; i < slotCount; i++)
                            ClearSlot(slots[i].SlotIndex);
                    }
                    for (int i = 0; i < slotCount; i++)
                    {
                        if (granular && HotbarStateBuilder.SlotsEqual(_lastSlots[i], slots[i]))
                            continue;
                        if (granular)
                            ClearSlot(slots[i].SlotIndex);
                        DrawSlot(slots[i]);
                    }
                }

                // Copy values: IReadOnlyList does not guarantee the caller's
                // underlying array/list is immutable between render calls.
                _lastSlots.Clear();
                for (int i = 0; i < slotCount; i++)
                    _lastSlots.Add(slots[i]);
                _lastTitle = title;
                _lastSummary = summary;
                _lastHint = hint;
                _hasContent = true;
            }
        }

        private bool SlotsChanged(IReadOnlyList<HotbarSlotSnapshot> slots, int count)
        {
            if (_lastSlots.Count != count)
                return true;
            for (int i = 0; i < count; i++)
                if (!HotbarStateBuilder.SlotsEqual(_lastSlots[i], slots[i]))
                    return true;
            return false;
        }

        private static bool HasOrderedSlots(IReadOnlyList<HotbarSlotSnapshot> slots)
        {
            if (slots == null)
                return true;
            for (int i = 0; i < slots.Count; i++)
                if (slots[i].SlotIndex != i)
                    return false;
            return true;
        }

        private void ClearRow(int y)
        {
            _tilemap.SetTilesBlock(new BoundsInt(0, y, 0, GameplayHotbarLayout.GridWidth, 1, 1), BlankRow);
        }

        private void ClearSlot(int slotIndex)
        {
            _tilemap.SetTilesBlock(new BoundsInt(slotIndex * GameplayHotbarLayout.SlotWidth, 0, 0,
                GameplayHotbarLayout.SlotWidth, 4, 1), BlankSlot);
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
