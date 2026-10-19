#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Editor
{
    /// <summary>Explicit, keeper-only import of a native planar pixel export.
    /// Never runs on load and never regenerates an unrelated art library.</summary>
    public static class SeedKeeperFlatBuilder
    {
        const string Folder = "Assets/Resources/SeedKeeperFlat";
        [Serializable] public sealed class Report
        {
            public string status, error, source, sourceSha256;
            public int pixels, triangles, paletteColors;
            public string[] assets;
        }
        public static Report Build(string sourceJsonPath, string reportPath)
        {
            var report = new Report { source = Path.GetFullPath(sourceJsonPath) };
            var paths = new List<string>();
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                    throw new InvalidOperationException("An idle editor is required for the flat Seed Keeper import.");
                string json = File.ReadAllText(report.source);
                var data = JsonUtility.FromJson<SeedKeeperFlatSource>(json);
                if (data == null) throw new InvalidOperationException("Missing planar pixel source.");
                float step = ValidatePixelStep(data);
                report.sourceSha256 = SeedKeeperFlatLibrary.Hash(json);
                int minX = data.pixels.Min(p => p.x), maxX = data.pixels.Max(p => p.x), maxY = data.pixels.Max(p => p.y);
                float pivotX = (minX + maxX + 1) * .5f;
                var paletteSource = ReferenceGladeVoxelLibrary.Load()?.Material;
                if (paletteSource == null) throw new InvalidOperationException("Native fog-aware palette shader is unavailable.");
                Preflight<Mesh>(Folder + "/seed-keeper.asset"); Preflight<Texture2D>(Folder + "/Palette.asset");
                Preflight<Material>(Folder + "/Palette.mat"); Preflight<GameObject>(SeedKeeperFlatLibrary.PrefabPath);
                Preflight<SeedKeeperFlatLibrary>(Folder + "/Library.asset");
                if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Resources", "SeedKeeperFlat");

                var palette = new Texture2D(data.palette.Length, 1, TextureFormat.RGBA32, false, false)
                { name = "Seed Keeper palette", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, anisoLevel = 0 };
                var colors = new Color[data.palette.Length];
                for (int i = 0; i < colors.Length; i++) ColorUtility.TryParseHtmlString(data.palette[i], out colors[i]);
                palette.SetPixels(colors); palette.Apply(false, false);
                palette = Save(palette, Folder + "/Palette.asset", paths);
                var material = new Material(paletteSource) { name = "Seed Keeper planar palette" };
                material.SetTexture("_BaseMap", palette); material.SetColor("_BaseColor", Color.white);
                material.SetFloat("_GroundMottleStrength", 0); material.SetFloat("_GroundContactStrength", 0);
                material = Save(material, Folder + "/Palette.mat", paths);

                var mesh = new Mesh { name = "Seed Keeper flat native pixels" };
                var vertices = new Vector3[data.pixels.Length * 4]; var normals = new Vector3[vertices.Length];
                var uv = new Vector2[vertices.Length]; var indices = new int[data.pixels.Length * 6];
                for (int i = 0; i < data.pixels.Length; i++)
                {
                    var pixel = data.pixels[i]; int v = i * 4, t = i * 6;
                    float left = (pixel.x - pivotX) * step, bottom = (maxY - pixel.y) * step;
                    vertices[v] = new Vector3(left, bottom, 0); vertices[v + 1] = new Vector3(left + step, bottom, 0);
                    vertices[v + 2] = new Vector3(left + step, bottom + step, 0); vertices[v + 3] = new Vector3(left, bottom + step, 0);
                    for (int k = 0; k < 4; k++) { normals[v + k] = Vector3.back; uv[v + k] = new Vector2((pixel.color + .5f) / data.palette.Length, .5f); }
                    // The camera looks along local +Z; these front faces point -Z.
                    indices[t] = v; indices[t + 1] = v + 2; indices[t + 2] = v + 1;
                    indices[t + 3] = v; indices[t + 4] = v + 3; indices[t + 5] = v + 2;
                }
                mesh.vertices = vertices; mesh.normals = normals; mesh.uv = uv; mesh.triangles = indices; mesh.RecalculateBounds();
                mesh = Save(mesh, Folder + "/seed-keeper.asset", paths);
                GameObject prefab;
                var preview = EditorSceneManager.NewPreviewScene(); GameObject root = null;
                try
                {
                    root = new GameObject("Seed Keeper flat"); SceneManager.MoveGameObjectToScene(root, preview);
                    root.AddComponent<MeshFilter>().sharedMesh = mesh; var renderer = root.AddComponent<MeshRenderer>();
                    renderer.sharedMaterial = material; renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; renderer.receiveShadows = true;
                    prefab = PrefabUtility.SaveAsPrefabAsset(root, SeedKeeperFlatLibrary.PrefabPath, out bool saved);
                    if (!saved || prefab == null) throw new InvalidOperationException("Flat keeper prefab save failed.");
                    paths.Add(SeedKeeperFlatLibrary.PrefabPath);
                }
                finally { if (root != null) Object.DestroyImmediate(root); EditorSceneManager.ClosePreviewScene(preview); }
                var library = ScriptableObject.CreateInstance<SeedKeeperFlatLibrary>();
                library.SourceJson = json; library.SourceSha256 = report.sourceSha256;
                library.Mesh = mesh; library.Material = material; library.Prefab = prefab;
                library.Spec = new SpawnRing3DCatalog.Model
                {
                    id = SeedKeeperFlatLibrary.ModelId, path = SeedKeeperFlatLibrary.PrefabPath, sourceBlueprint = "SpreadSeedKeeper", kind = "actor",
                    rigFamily = "", rigged = false, materialFamily = "seed-keeper-flat-palette", boundsCenter = mesh.bounds.center, boundsSize = mesh.bounds.size,
                    triangles = data.pixels.Length * 2, clips = Array.Empty<string>(), sockets = Array.Empty<string>()
                };
                try { library.Validate(); library = Save(library, Folder + "/Library.asset", paths); library.Validate(); }
                finally { if (library != null && !AssetDatabase.Contains(library)) Object.DestroyImmediate(library); }
                report.pixels = data.pixels.Length; report.paletteColors = data.palette.Length; report.triangles = data.pixels.Length * 2; report.status = "success";
            }
            catch (Exception error) { report.status = "failed"; report.error = error.ToString(); }
            finally
            {
                report.assets = paths.ToArray(); string output = Path.GetFullPath(reportPath);
                Directory.CreateDirectory(Path.GetDirectoryName(output)); File.WriteAllText(output, JsonUtility.ToJson(report, true));
            }
            return report;
        }
        // Pure preflight: height fitting can make a narrow source too thin. Check
        // the same complete envelope as Library.Validate before any live asset is saved.
        static float ValidatePixelStep(SeedKeeperFlatSource data)
        {
            data.Validate();
            int columns = data.pixels.Max(p => p.x) - data.pixels.Min(p => p.x) + 1;
            int rows = data.pixels.Max(p => p.y) - data.pixels.Min(p => p.y) + 1;
            float step = Mathf.Min(SeedKeeperFlatLibrary.ArtworkWidth / columns, SeedKeeperFlatLibrary.ArtworkHeight / rows);
            float width = columns * step, height = rows * step;
            if (width < .9f || width > 1.2f || height < 1.35f || height > 1.65f)
                throw new InvalidOperationException("Source silhouette is outside the approved keeper proportions.");
            return step;
        }
        static void Preflight<T>(string path) where T : Object
        {
            var existing = AssetDatabase.LoadMainAssetAtPath(path);
            if (existing != null && !(existing is T)) throw new InvalidOperationException("Refusing an unrelated asset at " + path);
        }
        static T Save<T>(T candidate, string path, List<string> paths) where T : Object
        {
            var current = AssetDatabase.LoadAssetAtPath<T>(path);
            if (current == null) { AssetDatabase.CreateAsset(candidate, path); current = candidate; }
            else { EditorUtility.CopySerialized(candidate, current); Object.DestroyImmediate(candidate); }
            EditorUtility.SetDirty(current); AssetDatabase.SaveAssetIfDirty(current); paths.Add(path); return current;
        }
    }
}
#endif
