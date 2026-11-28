using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    public enum PackNavigation { Up, Down, Left, Right, PageUp, PageDown, First, Last }

    /// <summary>Immutable geometry and ordinal navigation for the pack only.
    /// Coordinates are canvas-local, top-left based. No inventory rules live here.</summary>
    public sealed class InventoryPackLayout
    {
        public readonly float PixelWidth, PixelHeight, RequestedScale, Scale, Width, Height, ColumnWidth, NameWidth;
        public readonly float RowHeight;
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
            float listHeight = Mathf.Max(1, Height - 270);
            RowHeight = Mathf.Min(36, listHeight / 2);
            Rows = Math.Max(2, Mathf.FloorToInt(listHeight / RowHeight));
            Capacity = Rows * Columns;
            ListBounds = new Rect(24, 190, listWidth, Rows * RowHeight);
            DetailBounds = details > 0 ? new Rect(36 + listWidth, 132, details, Height - 212) : default;
        }
        public static InventoryPackLayout Create(float width, float height, float textScale = 1)
            => new InventoryPackLayout(width, height, textScale);
        public Rect RowBounds(int ordinal)
        {
            int local = Math.Max(0, ordinal) % Capacity;
            return new Rect(ListBounds.x + local / Rows * (ColumnWidth + 12),
                ListBounds.y + local % Rows * RowHeight, ColumnWidth, RowHeight);
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

        internal readonly struct PackRow
        {
            public readonly int Slot, ItemOrdinal;
            public readonly string Category;
            public readonly bool Continued;
            public bool IsHeader => ItemOrdinal < 0;

            public PackRow(int slot, int itemOrdinal, string category = null, bool continued = false)
            {
                Slot = slot;
                ItemOrdinal = itemOrdinal;
                Category = category;
                Continued = continued;
            }
        }

        /// <summary>Maps selectable item ordinals to painted slots. Category headings use slots
        /// but have no item ordinal, so they cannot become a selection or action target.</summary>
        internal sealed class GroupedPages
        {
            readonly InventoryPackLayout _geometry;
            readonly int[] _itemSlots;
            readonly List<List<PackRow>> _pages = new List<List<PackRow>>();

            public GroupedPages(InventoryPackLayout geometry, IReadOnlyList<string> categories)
            {
                _geometry = geometry;
                _itemSlots = new int[categories.Count];
                int slot = 0;
                for (int item = 0; item < categories.Count; item++)
                {
                    string category = categories[item];
                    bool newCategory = item == 0 || category != categories[item - 1];
                    // A heading always shares its column with at least one item.
                    if (newCategory && slot % geometry.Rows == geometry.Rows - 1) slot++;
                    if (newCategory || slot % geometry.Rows == 0)
                        Add(new PackRow(slot++, -1, category, continued: !newCategory));
                    _itemSlots[item] = slot;
                    Add(new PackRow(slot++, item));
                }
            }

            void Add(PackRow row)
            {
                int page = row.Slot / _geometry.Capacity;
                if (page == _pages.Count) _pages.Add(new List<PackRow>());
                _pages[page].Add(row);
            }

            public IReadOnlyList<PackRow> Visible(int selected)
                => _itemSlots.Length == 0 ? Array.Empty<PackRow>()
                    : _pages[_itemSlots[selected] / _geometry.Capacity];

            public int Move(int selected, PackNavigation direction)
            {
                int count = _itemSlots.Length;
                if (count == 0 || direction == PackNavigation.First) return 0;
                if (direction == PackNavigation.Last) return count - 1;
                if (direction == PackNavigation.Up) return Math.Max(0, selected - 1);
                if (direction == PackNavigation.Down) return Math.Min(count - 1, selected + 1);

                int slot = _itemSlots[selected];
                int target = _geometry.Move(slot, _itemSlots[count - 1] + 1, direction);
                if (target == slot) return selected;
                // Look for the item closest to the requested visual slot. When switching
                // columns, an empty destination column leaves the current owner selected.
                bool horizontal = direction == PackNavigation.Left || direction == PackNavigation.Right;
                int best = selected, distance = int.MaxValue;
                foreach (var row in _pages[target / _geometry.Capacity])
                {
                    if (row.IsHeader || horizontal && row.Slot / _geometry.Rows != target / _geometry.Rows) continue;
                    int offset = Math.Abs(target - row.Slot);
                    if (offset >= distance) continue;
                    distance = offset;
                    best = row.ItemOrdinal;
                }
                return best;
            }
        }
    }
}
