using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>Derived, non-serialized occupancy for opted-in bodies. All writes
    /// go through Zone so one canonical owner and its whole body move together.</summary>
    internal sealed class ZoneSpatialIndex
    {
        private sealed class Entry
        {
            internal Cell Anchor;
            internal Vector2Int[] Offsets;
            internal string Raw;
        }
        private readonly Zone _zone;
        private readonly Dictionary<Entity, Entry> _entries = new Dictionary<Entity, Entry>();
        internal ZoneSpatialIndex(Zone zone) { _zone=zone; }
        internal Vector2Int[] Offsets(Entity e) => _entries.TryGetValue(e,out var r) ? r.Offsets : e.GetPart<SpatialFootprintPart>()?.Offsets;
        internal bool Matches(Entity entity) => _entries.TryGetValue(entity,out var entry)
            && entry.Raw == entity.GetPart<SpatialFootprintPart>()?.CellsRaw;
        internal void Register(Entity entity, Cell anchor)
        {
            var part=entity.GetPart<SpatialFootprintPart>();
            if(part==null) return;
            var entry=new Entry {Anchor=anchor,Offsets=part.Offsets,Raw=part.CellsRaw};
            _entries[entity]=entry;
            if(anchor.SpatialAnchors==null) anchor.SpatialAnchors=new List<Entity>(1);
            anchor.SpatialAnchors.Add(entity);
            foreach(var cell in new OccupiedCells(_zone,anchor.X,anchor.Y,entry.Offsets))
            {
                if(cell==null) continue;
                if(cell.SpatialOwners==null) cell.SpatialOwners=new List<Entity>(1);
                cell.SpatialOwners.Add(entity);

            }
        }
        internal void Unregister(Entity entity)
        {
            if(!_entries.TryGetValue(entity,out var entry)) return;
            entry.Anchor.SpatialAnchors?.Remove(entity);
            foreach(var cell in new OccupiedCells(_zone,entry.Anchor.X,entry.Anchor.Y,entry.Offsets))
            {
                cell?.SpatialOwners?.Remove(entity);

            }
            _entries.Remove(entity);
        }
        internal void Clear()
        {
            foreach(var pair in _entries)
            {
                var e=pair.Value;
                e.Anchor.SpatialAnchors?.Remove(pair.Key);
                foreach(var cell in new OccupiedCells(_zone,e.Anchor.X,e.Anchor.Y,e.Offsets))
                    cell?.SpatialOwners?.Remove(pair.Key);
            }
            _entries.Clear();
        }
    }
}
