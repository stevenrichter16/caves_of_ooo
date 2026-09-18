using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Per-cell shader data: RGB is gameplay illumination; alpha is
    /// 0 unseen, .5 remembered, 1 currently visible. Sampling never discovers a cell.</summary>
    public static class Village3DVisibility
    {
        public static Color SampleCell(Cell cell, LightMap lightMap, bool fullReveal)
        {
            if (cell == null || (!fullReveal && !cell.Explored)) return Color.clear;
            if (!fullReveal && !cell.IsVisible)
            {
                float remembered = ZoneRenderer.RememberedBrightnessFor(cell.ParentZone.AmbientLevel);
                return new Color(remembered, remembered, remembered, .5f);
            }
            Color tint = lightMap != null
                ? lightMap.ApplyToColor(Color.white, cell.X, cell.Y)
                : fullReveal ? Color.white : cell.ParentZone.AmbientTint * cell.ParentZone.AmbientLevel;
            tint.a = 1;
            return tint;
        }
    }
}
