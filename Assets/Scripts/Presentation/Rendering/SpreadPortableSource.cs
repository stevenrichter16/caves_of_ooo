using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace CavesOfOoo.Rendering
{
    /// <summary>Read-only, bounded preflight for the exact source mesh pack.
    /// The editor verifies its source hashes before any persistent write.</summary>
    [Serializable]
    public sealed class SpreadPortableSource
    {
        public int schemaVersion;
        public string[] palette;
        public Model[] models;
        [Serializable]
        public sealed class Model
        {
            public string id, blueprint, form, source, sourceSha256;
            public bool borrowed;
            public float[] positions;
            public int[] triangles, paletteIndices;
            public float pitch;
        }
        public void Validate()
        {
            if (schemaVersion != 1 || palette == null || palette.Length < 24 || palette.Length > 64
                || models == null || models.Length < 181 || models.Length > 600)
                throw new InvalidOperationException("Incomplete portable source pack.");
            foreach (string color in palette)
                if (color == null || !Regex.IsMatch(color, "^#[0-9A-Fa-f]{6}$"))
                    throw new InvalidOperationException("Invalid portable palette.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var model in models)
            {
                if (model == null || model.id == null || !Regex.IsMatch(model.id, "^spread-portable-[a-z0-9-]+$")
                    || !ids.Add(model.id) || string.IsNullOrEmpty(model.blueprint) || string.IsNullOrEmpty(model.form)
                    || model.sourceSha256 == null || !Regex.IsMatch(model.sourceSha256, "^[0-9a-f]{64}$")
                    || model.source == null || model.source.Contains("..") || model.source.Contains("\\")
                    || (model.borrowed ? !Regex.IsMatch(model.source, "^ArtSource/World3D/item-candidates/models/item-[A-Za-z0-9]+[.]fbx$")
                        : model.source != "models/" + model.id + ".fbx"))
                    throw new InvalidOperationException("Invalid portable identity or source.");
                ValidateGeometry(model, palette.Length);
            }
        }
        private static void ValidateGeometry(Model model, int paletteLength)
        {
            var xyz = model.positions; var triangles = model.triangles; var paint = model.paletteIndices;
            if (xyz == null || paint == null || triangles == null || paint.Length < 3 || paint.Length > 65535
                || xyz.Length != paint.Length * 3 || triangles.Length == 0 || triangles.Length % 3 != 0
                || triangles.Length > 393210 || !Finite(model.pitch) || model.pitch <= 0 || model.pitch > .03501f)
                throw new InvalidOperationException("Invalid portable buffer lengths.");
            for (int i = 0; i < paint.Length; i++)
            {
                float x = xyz[i*3], y = xyz[i*3+1], z = xyz[i*3+2];
                if (!Finite(x) || !Finite(y) || !Finite(z) || Math.Abs(x) > .501f || Math.Abs(z) > .501f
                    || y < -.0251f || y > 1.001f || paint[i] < 0 || paint[i] >= paletteLength)
                    throw new InvalidOperationException("Invalid portable vertex or paint.");
            }
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int a = triangles[i], b = triangles[i+1], c = triangles[i+2];
                if (a < 0 || b < 0 || c < 0 || a >= paint.Length || b >= paint.Length || c >= paint.Length
                    || a == b || b == c || c == a)
                    throw new InvalidOperationException("Invalid portable triangle index.");
                double ux = xyz[b*3]-xyz[a*3], uy = xyz[b*3+1]-xyz[a*3+1], uz = xyz[b*3+2]-xyz[a*3+2];
                double vx = xyz[c*3]-xyz[a*3], vy = xyz[c*3+1]-xyz[a*3+1], vz = xyz[c*3+2]-xyz[a*3+2];
                double nx = uy*vz-uz*vy, ny = uz*vx-ux*vz, nz = ux*vy-uy*vx;
                if (nx*nx+ny*ny+nz*nz < 1e-18)
                    throw new InvalidOperationException("Degenerate portable source triangle.");
            }
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
