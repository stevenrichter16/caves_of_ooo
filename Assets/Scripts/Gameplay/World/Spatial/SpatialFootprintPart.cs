using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// A fixed-orientation physical body, with one entity/HP/inventory at one
    /// canonical anchor. Offsets are dx,dy pairs, positive y south. The anchor
    /// need not be occupied. Set before placement; use Zone.TryChangeFootprint
    /// for an atomic change while placed. The public primitive field is saved
    /// by the native Part serializer; parsed geometry is runtime-only.
    /// </summary>
    public sealed class SpatialFootprintPart : Part
    {
        public string CellsRaw = "0,0";
        private string _parsedRaw;
        private Vector2Int[] _offsets;
        internal Vector2Int[] Offsets
        {
            get
            {
                if (_parsedRaw != CellsRaw || _offsets == null)
                {
                    _parsedRaw = CellsRaw;
                    _offsets = Parse(CellsRaw);
                }
                return _offsets;
            }
        }
        public override void OnBeforeSave(SaveWriter writer)
        {
            if(ParentEntity?.SpatialZone != null && !ParentEntity.SpatialZone.IsFootprintCurrent(ParentEntity))
                throw new InvalidOperationException("Change a placed footprint through Zone.TryChangeFootprint before saving.");
        }
        private static readonly Vector2Int[] Invalid = Array.Empty<Vector2Int>();
        private static Vector2Int[] Parse(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw) || raw.Length > 24000) return Invalid;
            var pairs = raw.Split(';');
            if (pairs.Length > Zone.Width * Zone.Height) return Invalid;
            var result = new Vector2Int[pairs.Length];
            var seen = new HashSet<Vector2Int>();
            for (int i = 0; i < pairs.Length; i++)
            {
                var xy = pairs[i].Split(',');
                if (xy.Length != 2
                    || !int.TryParse(xy[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int x)
                    || !int.TryParse(xy[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int y)
                    || x <= -Zone.Width || x >= Zone.Width || y <= -Zone.Height || y >= Zone.Height)
                    return Invalid;
                var p = new Vector2Int(x,y);
                if (!seen.Add(p)) return Invalid;
                result[i] = p;
            }
            return result;
        }
    }
}
