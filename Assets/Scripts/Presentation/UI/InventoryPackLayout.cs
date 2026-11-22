using System;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    public enum PackNavigation { Up, Down, Left, Right, PageUp, PageDown, First, Last }

    /// <summary>Immutable geometry and ordinal navigation for the pack only.
    /// Coordinates are canvas-local, top-left based. No inventory rules live here.</summary>
    public sealed class InventoryPackLayout
    {
        public readonly float PixelWidth, PixelHeight, RequestedScale, Scale, Width, Height, ColumnWidth, NameWidth;
        public readonly int Rows, Columns, Capacity;
        public readonly Rect ListBounds, DetailBounds;
        public bool HasDetails => DetailBounds.width > 0;
        InventoryPackLayout(float width, float height, float scale)
        {
            PixelWidth = width; PixelHeight = height;
            RequestedScale = float.IsNaN(scale) ? 1 : Mathf.Clamp(scale, 1, 2);
            Scale = Mathf.Min(RequestedScale, Mathf.Max(1, Mathf.Min(width / 480, height / 360)));
            Width = Mathf.Max(1, width / Scale); Height = Mathf.Max(1, height / Scale);
            float details = Width >= 1280 && Height >= 500 ? Mathf.Clamp(Width * .22f, 280, 360) : 0;
            float listWidth = Width - 48 - (details > 0 ? details + 12 : 0);
            Columns = (listWidth - 12) / 2 - 142 >= 320 ? 2 : 1;
            ColumnWidth = (listWidth - (Columns - 1) * 12) / Columns;
            NameWidth = ColumnWidth - 142;
            Rows = Math.Max(1, Mathf.FloorToInt((Height - 270) / 24)); Capacity = Rows * Columns;
            ListBounds = new Rect(24, 190, listWidth, Rows * 24);
            DetailBounds = details > 0 ? new Rect(36 + listWidth, 132, details, Height - 212) : default;
        }
        public static InventoryPackLayout Create(float width, float height, float textScale = 1)
            => new InventoryPackLayout(width, height, textScale);
        public Rect RowBounds(int ordinal)
        {
            int local = Math.Max(0, ordinal) % Capacity;
            return new Rect(ListBounds.x + local / Rows * (ColumnWidth + 12), ListBounds.y + local % Rows * 24, ColumnWidth, 24);
        }
        public int PageStart(int selected) => Math.Max(0, selected) / Capacity * Capacity;
        public int Move(int selected, int count, PackNavigation move)
        {
            if (count <= 0) return 0;
            selected = Mathf.Clamp(selected, 0, count - 1);
            if (move == PackNavigation.First) return 0;
            if (move == PackNavigation.Last) return count - 1;
            int page = PageStart(selected), col = (selected - page) / Rows, row = (selected - page) % Rows;
            if (move == PackNavigation.Left || move == PackNavigation.Right)
            {
                int targetCol = Mathf.Clamp(col + (move == PackNavigation.Left ? -1 : 1), 0, Columns - 1);
                int start = page + targetCol * Rows;
                return start >= count ? selected : Math.Min(start + row, Math.Min(start + Rows, count) - 1);
            }
            if (move == PackNavigation.PageUp || move == PackNavigation.PageDown)
            {
                int target = Mathf.Clamp(page + (move == PackNavigation.PageUp ? -Capacity : Capacity), 0, (count - 1) / Capacity * Capacity);
                int targetColumn = Math.Min(col, (count - 1 - target) / Rows);
                int targetStart = target + targetColumn * Rows;
                return targetStart + Math.Min(row, Math.Min(Rows, count - targetStart) - 1);
            }
            int delta = move == PackNavigation.Up ? -1 : 1;
            if (row + delta >= 0 && row + delta < Math.Min(Rows, count - page - col * Rows)) return selected + delta;
            int nextPage = page + delta * Capacity;
            if (nextPage < 0 || nextPage >= count) return selected;
            int nextCol = Math.Min(col, (count - 1 - nextPage) / Rows);
            int nextStart = nextPage + nextCol * Rows;
            return delta > 0 ? nextStart : Math.Min(nextStart + Rows, count) - 1;
        }
    }
}
