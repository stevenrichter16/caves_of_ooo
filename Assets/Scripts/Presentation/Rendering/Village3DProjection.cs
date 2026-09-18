using System;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>One-unit cells on the XZ ground plane. Simulation row zero is north.
    /// Mirrors the existing XY tilemap; height never changes logical cell ownership.</summary>
    public static class Village3DProjection
    {
        public static Vector3 CellCentre(int x, int y, float height = 0f)
        {
            if (x < 0 || x >= Zone.Width || y < 0 || y >= Zone.Height || !Finite(height))
                throw new ArgumentOutOfRangeException(nameof(x), "A finite height and an in-bounds zone cell are required.");
            return new Vector3(x + .5f, height, Zone.Height - y - .5f);
        }

        public static bool TryWorldToCell(Vector3 point, out int x, out int y)
        {
            x = y = -1;
            if (!Finite(point.x) || !Finite(point.y) || !Finite(point.z)
                || point.x < 0 || point.x >= Zone.Width || point.z < 0 || point.z >= Zone.Height)
                return false;
            x = Mathf.FloorToInt(point.x);
            y = Zone.Height - 1 - Mathf.FloorToInt(point.z);
            return true;
        }

        /// <summary>Map a screen point through the actual displayed map rectangle.
        /// The camera position is the existing XY camera's position, not the 3D camera.</summary>
        public static bool TryScreenToCell(Vector2 point, Rect viewport, Vector3 sourceCameraPosition,
            float orthographicSize, out int x, out int y)
        {
            x = y = -1;
            if (!Finite(point.x) || !Finite(point.y) || !Finite(viewport.x) || !Finite(viewport.y)
                || !Finite(viewport.width) || !Finite(viewport.height) || viewport.width <= 0 || viewport.height <= 0
                || !Finite(orthographicSize) || orthographicSize <= 0 || !viewport.Contains(point)
                || !Finite(sourceCameraPosition.x) || !Finite(sourceCameraPosition.y))
                return false;
            float halfWidth = orthographicSize * viewport.width / viewport.height;
            float wx = sourceCameraPosition.x + ((point.x - viewport.x) / viewport.width * 2 - 1) * halfWidth;
            float wz = sourceCameraPosition.y + ((point.y - viewport.y) / viewport.height * 2 - 1) * orthographicSize;
            return TryWorldToCell(new Vector3(wx, 0, wz), out x, out y);
        }

        internal static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
