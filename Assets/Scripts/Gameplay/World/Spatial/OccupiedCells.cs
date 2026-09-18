using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>Allocation-free translated body cells; null entries denote an
    /// out-of-bounds candidate. An unplaced entity has no current cells.</summary>
    public readonly struct OccupiedCells : IReadOnlyList<Cell>
    {
        private readonly Zone _zone;
        private readonly int _x, _y;
        private readonly Vector2Int[] _offsets;
        internal OccupiedCells(Zone zone, int x, int y, Vector2Int[] offsets)
        { _zone=zone; _x=x; _y=y; _offsets=offsets; }
        public int Count => _zone == null ? 0 : _offsets == null ? 1 : _offsets.Length;
        public Cell this[int index]
        {
            get
            {
                if(index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index));
                return _zone.GetCell(_x+(_offsets == null ? 0 : _offsets[index].x),
                    _y+(_offsets == null ? 0 : _offsets[index].y));
            }
        }
        public Enumerator GetEnumerator() => new Enumerator(this);
        IEnumerator<Cell> IEnumerable<Cell>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public struct Enumerator : IEnumerator<Cell>
        {
            private OccupiedCells _cells; private int _index;
            internal Enumerator(OccupiedCells cells) { _cells=cells; _index=-1; }
            public Cell Current => _cells[_index];
            object IEnumerator.Current => Current;
            public bool MoveNext() => ++_index < _cells.Count;
            public void Reset() { _index=-1; }
            public void Dispose() { }
        }
    }
}
