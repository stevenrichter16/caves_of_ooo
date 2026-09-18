using System;
using System.Collections;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Allocation-free live view of physical occupants. Canonical Cell.Objects
    /// remains the save/render anchor list. This view merges anchored single-cell
    /// entities with indexed bodies once each; enumeration order is stable but
    /// not a render-layer contract. Snapshot before dispatching mutating events.
    /// </summary>
    public readonly struct CellOccupants : IReadOnlyList<Entity>
    {
        private readonly Cell _cell;
        internal CellOccupants(Cell cell) { _cell = cell; }
        public int Count
        {
            get
            {
                if (_cell == null) return 0;
                if ((_cell.SpatialOwners?.Count ?? 0) == 0 && (_cell.SpatialAnchors?.Count ?? 0) == 0)
                    return _cell.Objects.Count;
                int count = _cell.Objects.Count + (_cell.SpatialOwners?.Count ?? 0);
                if (_cell.SpatialAnchors != null)
                    foreach (var e in _cell.SpatialAnchors)
                        if (_cell.Objects.Contains(e)) count--;
                return count;
            }
        }
        public Entity this[int index]
        {
            get
            {
                if (index < 0 || _cell == null) throw new ArgumentOutOfRangeException(nameof(index));
                if ((_cell.SpatialOwners?.Count ?? 0) == 0 && (_cell.SpatialAnchors?.Count ?? 0) == 0)
                    return _cell.Objects[index];
                foreach (var e in _cell.Objects)
                {
                    if (_cell.SpatialAnchors?.Contains(e) == true) continue;
                    if (index-- == 0) return e;
                }
                if (_cell.SpatialOwners != null && index < _cell.SpatialOwners.Count)
                    return _cell.SpatialOwners[index];
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
        public bool Contains(Entity entity)
        {
            if (_cell == null || entity == null) return false;
            return _cell.SpatialOwners?.Contains(entity) == true
                || (_cell.Objects.Contains(entity) && _cell.SpatialAnchors?.Contains(entity) != true);
        }
        public Enumerator GetEnumerator() => new Enumerator(_cell);
        IEnumerator<Entity> IEnumerable<Entity>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public struct Enumerator : IEnumerator<Entity>
        {
            private readonly Cell _cell;
            private int _anchorIndex, _bodyIndex;
            public Entity Current { get; private set; }
            object IEnumerator.Current => Current;
            internal Enumerator(Cell cell) { _cell=cell; _anchorIndex=0; _bodyIndex=0; Current=null; }
            public bool MoveNext()
            {
                if (_cell == null) return false;
                while (_anchorIndex < _cell.Objects.Count)
                {
                    var e = _cell.Objects[_anchorIndex++];
                    if (_cell.SpatialAnchors?.Contains(e) == true) continue;
                    Current=e; return true;
                }
                if (_cell.SpatialOwners != null && _bodyIndex < _cell.SpatialOwners.Count)
                { Current=_cell.SpatialOwners[_bodyIndex++]; return true; }
                Current=null; return false;
            }
            public void Reset() { _anchorIndex=0; _bodyIndex=0; Current=null; }
            public void Dispose() { }
        }
    }
}
