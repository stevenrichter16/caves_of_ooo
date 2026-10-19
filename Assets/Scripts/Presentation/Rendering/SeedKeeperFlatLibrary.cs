using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Bounded, reproducible export of the keeper's planar pixel art.
    /// Pixel coordinates are top-left based; absent pixels contain no geometry.</summary>
    [Serializable] public sealed class SeedKeeperFlatSource
    {
        [Serializable] public sealed class Pixel { public int x, y, color; }
        public int schemaVersion, width, height;
        public string[] palette;
        public Pixel[] pixels;

        public void Validate()
        {
            bool supportedCanvas = (width == 64 && height == 80) || (width == 16 && height == 24);
            if (schemaVersion != 1 || !supportedCanvas || palette == null || palette.Length < 1 || palette.Length > 256
                || pixels == null || pixels.Length < 100 || pixels.Length > width * height)
                throw new ArgumentException("A bounded 16 by 24 or legacy 64 by 80 planar Seed Keeper export is required.");
            foreach (string color in palette)
                if (color == null || color.Length != 7 || color[0] != '#' || !ColorUtility.TryParseHtmlString(color, out _))
                    throw new ArgumentException("Seed Keeper palette must use opaque RGB swatches.");
            var occupied = new HashSet<int>();
            foreach (var pixel in pixels)
                if (pixel == null || pixel.x < 0 || pixel.x >= width || pixel.y < 0 || pixel.y >= height
                    || pixel.color < 0 || pixel.color >= palette.Length || !occupied.Add(pixel.y * width + pixel.x))
                    throw new ArgumentException("Seed Keeper pixels must be unique, in bounds and use an existing palette swatch.");
        }
    }

    /// <summary>Exact opt-in replacement for the keeper's existing native model.
    /// The old humanoid library remains intact; no gameplay owner is created here.</summary>
    public sealed class SeedKeeperFlatLibrary : ScriptableObject
    {
        public const string ResourcePath = "SeedKeeperFlat/Library";
        public const string ModelId = "spread-person-seed-keeper";
        public const string PrefabPath = "Assets/Resources/SeedKeeperFlat/seed-keeper.prefab";
        // Uniformly fit occupied pixels inside the existing keeper envelope.
        // Leave headroom below its strict 1.65-unit validation boundary.
        public const float ArtworkWidth = 1.05f, ArtworkHeight = 1.6f;
        public string SourceJson, SourceSha256;
        public GameObject Prefab;
        public Mesh Mesh;
        public Material Material;
        public SpawnRing3DCatalog.Model Spec;
        bool ready;
        public static SeedKeeperFlatLibrary Load() => Resources.Load<SeedKeeperFlatLibrary>(ResourcePath);
        public static bool IsModelId(string id) => id == ModelId;
        public static Quaternion WorldRotation => Quaternion.Euler(NativeZone3DRenderSurface.CameraPitchDegrees, 0, 0);
        void OnValidate() { ready = false; }
        public SeedKeeperFlatLibrary Find(string id)
        {
            if (!IsModelId(id)) return null;
            if (!ready) Validate();
            return this;
        }
        public void Validate()
        {
            ready = false;
            if (string.IsNullOrEmpty(SourceJson) || Hash(SourceJson) != SourceSha256 || Mesh == null || Prefab == null || Material == null || Spec == null)
                throw new InvalidOperationException("Incomplete flat Seed Keeper provenance or resources.");
            var source = JsonUtility.FromJson<SeedKeeperFlatSource>(SourceJson);
            if (source == null) throw new InvalidOperationException("Missing flat Seed Keeper source.");
            source.Validate();
            var renderer = Prefab.GetComponent<MeshRenderer>();
            if (!Prefab.activeSelf || Prefab.transform.childCount != 0 || Prefab.GetComponents<Component>().Length != 3
                || Prefab.transform.localPosition != Vector3.zero || Prefab.transform.localRotation != Quaternion.identity || Prefab.transform.localScale != Vector3.one
                || Prefab.GetComponent<MeshFilter>()?.sharedMesh != Mesh || renderer == null || renderer.sharedMaterial != Material
                || renderer.sharedMaterials.Length != 1 || !renderer.enabled || renderer.forceRenderingOff
                || !Mesh.isReadable || Mesh.vertexCount != source.pixels.Length * 4 || Mesh.subMeshCount != 1 || Mesh.GetTopology(0) != MeshTopology.Triangles
                || Mesh.GetIndexCount(0) != source.pixels.Length * 6 || Mesh.bindposeCount != 0 || Mesh.blendShapeCount != 0)
                throw new InvalidOperationException("Flat Seed Keeper must remain one inert planar pixel mesh.");
            var texture = Material.GetTexture("_BaseMap") as Texture2D;
            if (Material.shader == null || Material.shader.name != "CavesOfOoo/Village3D/Palette" || Material.GetColor("_BaseColor") != Color.white
                || !Material.HasProperty("_FogLight") || !Material.HasProperty("_Transient") || !Material.HasProperty("_Exposure")
                || texture == null || !texture.isReadable || texture.width != source.palette.Length || texture.height != 1
                || texture.filterMode != FilterMode.Point || texture.wrapMode != TextureWrapMode.Clamp || texture.mipmapCount != 1)
                throw new InvalidOperationException("Flat Seed Keeper must use its exact opaque point-filtered native palette.");
            for (int i = 0; i < source.palette.Length; i++)
            {
                ColorUtility.TryParseHtmlString(source.palette[i], out var color);
                if (Vector4.Distance(color, texture.GetPixel(i, 0)) > .008f) throw new InvalidOperationException("Seed Keeper palette differs from the pixel source.");
            }
            var vertices = Mesh.vertices; var normals = Mesh.normals; var uv = Mesh.uv;
            if (normals.Length != vertices.Length || uv.Length != vertices.Length) throw new InvalidOperationException("Incomplete planar pixel buffers.");
            foreach (var v in vertices)
                if (!Finite(v.x) || !Finite(v.y) || !Finite(v.z) || Mathf.Abs(v.z) > .00001f || v.y < -.00001f || v.y > 1.65f || Mathf.Abs(v.x) > .6f)
                    throw new InvalidOperationException("Flat Seed Keeper geometry left its bounded planar envelope.");
            for (int i = 0; i < vertices.Length; i++)
                if ((normals[i] - Vector3.back).sqrMagnitude > .000001f || !Finite(uv[i].x) || !Finite(uv[i].y)
                    || Mathf.Abs(uv[i].y - .5f) > .00001f || uv[i].x <= 0 || uv[i].x >= 1)
                    throw new InvalidOperationException("Invalid front face or palette sampling in flat Seed Keeper.");
            // Counts and planarity alone cannot prove that the image still matches
            // its source. Validate each bounded exported quad, swatch and winding.
            int minX = source.width, minY = source.height, maxX = -1, maxY = -1;
            foreach (var pixel in source.pixels) { minX = Math.Min(minX, pixel.x); minY = Math.Min(minY, pixel.y); maxX = Math.Max(maxX, pixel.x); maxY = Math.Max(maxY, pixel.y); }
            float step = Mathf.Min(ArtworkWidth / (maxX - minX + 1), ArtworkHeight / (maxY - minY + 1)), pivotX = (minX + maxX + 1) * .5f;
            var triangles = Mesh.GetTriangles(0);
            for (int i = 0; i < source.pixels.Length; i++)
            {
                var pixel = source.pixels[i]; int v = i * 4, t = i * 6;
                float left = (pixel.x - pivotX) * step, bottom = (maxY - pixel.y) * step;
                for (int k = 0; k < 4; k++)
                {
                    var expected = new Vector3(left + (k == 1 || k == 2 ? step : 0), bottom + (k >= 2 ? step : 0), 0);
                    if ((vertices[v + k] - expected).sqrMagnitude > 1e-10f || Mathf.Abs(uv[v + k].x - (pixel.color + .5f) / source.palette.Length) > .000001f)
                        throw new InvalidOperationException("Flat Seed Keeper geometry or swatch differs from the pixel source.");
                }
                if (triangles[t] != v || triangles[t + 1] != v + 2 || triangles[t + 2] != v + 1
                    || triangles[t + 3] != v || triangles[t + 4] != v + 3 || triangles[t + 5] != v + 2)
                    throw new InvalidOperationException("Flat Seed Keeper winding differs from the pixel source.");
            }
            if (Mesh.bounds.size.z > .00001f || Mesh.bounds.size.x < .9f || Mesh.bounds.size.x > 1.2f || Mesh.bounds.size.y < 1.35f || Mesh.bounds.size.y > 1.65f
                || Spec.id != ModelId || Spec.path != PrefabPath || Spec.sourceBlueprint != "SpreadSeedKeeper" || Spec.kind != "actor"
                || Spec.rigged || Spec.rigFamily != "" || Spec.materialFamily != "seed-keeper-flat-palette" || Spec.triangles != source.pixels.Length * 2
                || (Spec.boundsCenter - Mesh.bounds.center).sqrMagnitude > .000001f || (Spec.boundsSize - Mesh.bounds.size).sqrMagnitude > .000001f
                || Spec.clips == null || Spec.clips.Length != 0 || Spec.sockets == null || Spec.sockets.Length != 0)
                throw new InvalidOperationException("Flat Seed Keeper shape or source specification differs.");
            ready = true;
        }
        public static string Hash(string text)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLowerInvariant();
        }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
