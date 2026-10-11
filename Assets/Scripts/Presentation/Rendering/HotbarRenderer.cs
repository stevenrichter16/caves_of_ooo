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
        private static readonly Color HotbarBgColor = new Color(.025f, .045f, .055f, 1f);
        private static readonly Color EmptyColor = QudColorParser.Gray;

        private readonly Tilemap _tilemap;
        private readonly Tilemap _backgroundTilemap;
        private static readonly TileBase[] BlankRow = new TileBase[GameplayHotbarLayout.GridWidth];
        private static readonly TileBase[] BlankSlot = new TileBase[GameplayHotbarLayout.SlotWidth * 5];
        private readonly List<HotbarSlotSnapshot> _lastSlots =
            new List<HotbarSlotSnapshot>(GameplayHotbarLayout.SlotCount);
        private bool _hasContent;
        private bool _needsInitialClear = true;
        private string _lastTitle;
        private string _lastSummary;
        private string _lastHint;
        private float _lastAspect = -1;
        private float _cellWidth;

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
            _lastAspect = -1;
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
                // Device guidance is presentation state, independent of the cached
                // gameplay snapshot. Resolve it before comparing retained text.
                if (snapshot != null && NativeGamepadInput.IsConnected)
                    hint = snapshot.PendingSlot >= 0
                        ? "[LS] direction  [A/RT] confirm  [B] cancel"
                        : "[D-pad L/R] cycle  [X] cast";
                IReadOnlyList<HotbarSlotSnapshot> slots = snapshot?.Slots;
                int slotCount = Mathf.Min(GameplayHotbarLayout.SlotCount, slots?.Count ?? 0);
                bool layoutChanged = !Mathf.Approximately(_lastAspect, camera.aspect);
                _cellWidth = GameplayHotbarLayout.ComputeCellWidth(camera.aspect);
                bool headerChanged = !_hasContent || title != _lastTitle || hint != _lastHint;
                bool summaryChanged = !_hasContent || summary != _lastSummary;
                bool slotsChanged = !_hasContent || layoutChanged || SlotsChanged(slots, slotCount);
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
                else if (layoutChanged)
                    DrawBackground();
                if (headerChanged || summaryChanged)
                {
                    if (!firstPaint) ClearRow(GameplayHotbarLayout.GridHeight - 1);
                    int hintWidth = Mathf.Min(hint.Length, GameplayHotbarLayout.GridWidth - 24);
                    int detailWidth = GameplayHotbarLayout.GridWidth - hintWidth - 4;
                    DrawText(1, GameplayHotbarLayout.GridHeight - 1,
                        HudHotbarPresentation.Fit(string.IsNullOrEmpty(summary) ? title : summary, detailWidth),
                        QudColorParser.White, detailWidth);
                    DrawRightAligned(GameplayHotbarLayout.GridWidth - 2, GameplayHotbarLayout.GridHeight - 1,
                        hint, QudColorParser.Gray, hintWidth);
                }
                if (slotsChanged)
                {
                    // Ordinary snapshots have ten ordered, non-overlapping slots.
                    // Arbitrary callers may provide sparse/reordered/duplicate
                    // indices, so replay those in order after clearing old boxes.
                    bool granular = !firstPaint && !layoutChanged && HasOrderedSlots(_lastSlots) &&
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
                _lastAspect = camera.aspect;
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
            if (slotIndex < 0 || slotIndex >= GameplayHotbarLayout.SlotCount) return;
            var bounds = new BoundsInt(slotIndex * GameplayHotbarLayout.SlotWidth, 0, 0,
                GameplayHotbarLayout.SlotWidth, 5, 1);
            _tilemap.SetTilesBlock(bounds, BlankSlot);
            _backgroundTilemap.SetTilesBlock(bounds, BlankSlot);
        }

        private void DrawBackground()
        {
            Tile blockTile = HudPixelFont.GetTile(CP437TilesetGenerator.SolidBlock);
            if (blockTile == null)
                return;

            for (int y = GameplayHotbarLayout.GridHeight - 1; y < GameplayHotbarLayout.GridHeight; y++)
            {
                for (int x = 0; x < GameplayHotbarLayout.GridWidth; x++)
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);
                    _backgroundTilemap.SetTile(pos, blockTile);
                    _backgroundTilemap.SetTileFlags(pos, TileFlags.None);
                    _backgroundTilemap.SetTransformMatrix(pos, Matrix4x4.Scale(new Vector3(_cellWidth * 2, 1, 1)));
                    _backgroundTilemap.SetColor(pos, HotbarBgColor);
                }
            }
        }

        private void DrawSlot(HotbarSlotSnapshot slot)
        {
            if (slot.SlotIndex < 0 || slot.SlotIndex >= GameplayHotbarLayout.SlotCount) return;
            int x = slot.SlotIndex * GameplayHotbarLayout.SlotWidth;
            const int width = GameplayHotbarLayout.SlotWidth;
            Color edge = slot.Pending ? QudColorParser.BrightCyan
                : slot.Selected ? QudColorParser.BrightYellow : QudColorParser.DarkGray;
            Color accent = !string.IsNullOrEmpty(slot.AccentColorCode)
                ? QudColorParser.Parse(slot.AccentColorCode) : QudColorParser.White;
            Color text = slot.Occupied ? (slot.Usable ? QudColorParser.White : QudColorParser.Gray) : EmptyColor;

            // Numbers, state, icon and names have disjoint rows. Explicit AIM,
            // CD and EMPTY labels survive monochrome viewing/color differences.
            DrawCardBackground(slot);
            DrawChar(x + 1, 4, slot.Hotkey, slot.Selected ? QudColorParser.BrightYellow : QudColorParser.Gray);
            DrawText(x + 2, 4, HudHotbarPresentation.StateLabel(slot),
                slot.Pending ? QudColorParser.BrightCyan : slot.CooldownRemaining > 0 ? QudColorParser.BrightRed : QudColorParser.Gray, width - 2);
            if (slot.Selected || slot.Pending) DrawChar(x + 1, 3, slot.Pending ? '^' : '*', edge);
            if (!slot.Occupied) { DrawCentered(x, 1, "empty", EmptyColor); return; }

            var pos = new Vector3Int(x + 3, 2, 0);
            _tilemap.SetTile(pos, HudSkillIconAtlas.GetTile(slot.Icon));
            _tilemap.SetTileFlags(pos, TileFlags.None);
            _tilemap.SetColor(pos, Color.white);
            // Uniformly enlarge the original art between state and two name rows.
            // Y bounds2.125..3.875 leave positive gaps from both text bands.
            _tilemap.SetTransformMatrix(pos, Matrix4x4.TRS(new Vector3(_cellWidth * .5f, .5f, 0),
                Quaternion.identity, new Vector3(1.75f, 1.75f, 1)));
            HudHotbarPresentation.NameLines(slot.DisplayName, out string first, out string second);
            DrawCentered(x, 1, first, text);
            DrawCentered(x, 0, second, text);
        }

        private void DrawCardBackground(HotbarSlotSnapshot slot)
        {
            var pos = new Vector3Int(slot.SlotIndex * GameplayHotbarLayout.SlotWidth, 0, 0);
            Color frame = slot.Pending ? QudColorParser.BrightCyan : slot.Selected ? QudColorParser.BrightYellow
                : slot.Occupied ? new Color(.35f, .46f, .51f, 1) : new Color(.23f, .29f, .32f, 1);
            _backgroundTilemap.SetTile(pos, HudCardFrame.GetTile());
            _backgroundTilemap.SetTileFlags(pos, TileFlags.None);
            _backgroundTilemap.SetColor(pos, frame);
            // A single cached four-by-five sprite replaces forty background tiles.
            // Translation accounts for this map's unchanged half-cell anchor.
            _backgroundTilemap.SetTransformMatrix(pos, Matrix4x4.TRS(new Vector3(_cellWidth * 3.5f, 2, 0),
                Quaternion.identity, new Vector3(_cellWidth * 2, 1, 1)));
        }

        private void DrawCentered(int x, int y, string text, Color color)
        {
            DrawText(x + Mathf.Max(0, (GameplayHotbarLayout.SlotWidth - text.Length) / 2), y,
                text, color, GameplayHotbarLayout.SlotWidth);
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
            Tile tile = HudPixelFont.GetTile(c);
            if (tile == null)
                return;

            Vector3Int pos = new Vector3Int(x, y, 0);
            _tilemap.SetTile(pos, tile);
            _tilemap.SetTileFlags(pos, TileFlags.None);
            _tilemap.SetColor(pos, color);
        }
    }
}
