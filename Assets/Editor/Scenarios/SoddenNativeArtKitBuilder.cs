#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Explicit reproducible import of the original Sodden source kit.
    /// Writes only owned persistent art assets, never gameplay or a loaded scene.</summary>
    public static class SoddenNativeArtKitBuilder
    {
        public const string SourcePath = "ArtSource/SoddenNativeArt3D/kit.json";
        [Serializable] sealed class Box { public Vector3 center, size; public int color; }
        [Serializable] sealed class Model { public string id, family, sourceBlueprint; public int variant; public Box[] boxes; }
        [Serializable] sealed class Kit { public int schemaVersion; public string[] palette; public Model[] models; }
        [Serializable] sealed class Report { public string status, sourceSha256, error; public int models, boxes, triangles; }

        public static void Run(string reportPath = "Docs/Verification/SoddenArtDirection/native-kit-import.json")
        {
            var report = new Report(); GameObject primitive = null;
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                    throw new InvalidOperationException("Import Sodden art in clean Edit mode.");
                string json = File.ReadAllText(SourcePath); var kit = JsonUtility.FromJson<Kit>(json);
                using (var sha = SHA256.Create()) report.sourceSha256 = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(json))).Replace("-", "").ToLowerInvariant();
                if (report.sourceSha256 != SoddenNativeArtLibrary.ReviewedSourceSha256 || kit == null || kit.schemaVersion != 1
                    || kit.palette == null || kit.palette.Length != SoddenNativeArtLibrary.PaletteHex.Length
                    || kit.models == null || kit.models.Length != SoddenNativeArtLibrary.ModelIds.Count)
                    throw new InvalidOperationException("Unreviewed or incomplete Sodden native source.");
                var colors = new Color[kit.palette.Length];
                for (int i = 0; i < colors.Length; i++)
                    if (kit.palette[i] != SoddenNativeArtLibrary.PaletteHex[i] || !ColorUtility.TryParseHtmlString(kit.palette[i], out colors[i]))
                        throw new InvalidOperationException("Sodden source palette disagrees with native contract.");
                var remaining = new HashSet<string>(SoddenNativeArtLibrary.ModelIds, StringComparer.Ordinal);
                foreach (var model in kit.models)
                {
                    if (model == null || model.id == null || !remaining.Remove(model.id)
                        || model.sourceBlueprint != SoddenNativeArtLibrary.Blueprint(model.id) || model.boxes == null || model.boxes.Length < 2 || model.boxes.Length > 60)
                        throw new InvalidOperationException("Malformed or repeated Sodden source model.");
                    foreach (var box in model.boxes)
                        if (box == null || box.color < 0 || box.color >= colors.Length || !Finite(box.center) || !Finite(box.size)
                            || box.size.x <= 0 || box.size.y <= 0 || box.size.z <= 0
                            || Mathf.Abs(box.center.x) + box.size.x * .5f > .5001f || Mathf.Abs(box.center.z) + box.size.z * .5f > .5001f
                            || box.center.y - box.size.y * .5f < -.0501f || box.center.y + box.size.y * .5f > 3.2501f)
                            throw new InvalidOperationException("Sodden source box exceeds its native cell.");
                }
                if (remaining.Count != 0) throw new InvalidOperationException("Missing Sodden source model.");
                var ring = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
                if (ring?.WorldMaterial == null) throw new InvalidOperationException("Native fog-aware ring shader required.");
                string folder = SoddenNativeArtLibrary.Folder; Directory.CreateDirectory(folder); AssetDatabase.Refresh();
                string palettePath = folder + "/Palette.asset";
                var palette = AssetDatabase.LoadAssetAtPath<Texture2D>(palettePath);
                if (palette == null)
                {
                    RefuseWrongType(palettePath);
                    palette = new Texture2D(colors.Length, 1, TextureFormat.RGBA32, false) { name = "Sodden native palette" };
                    AssetDatabase.CreateAsset(palette, palettePath);
                }
                if (palette.width != colors.Length || palette.height != 1) throw new InvalidOperationException("Owned Sodden palette dimensions changed.");
                palette.filterMode = FilterMode.Point; palette.wrapMode = TextureWrapMode.Clamp;
                palette.SetPixels(colors); palette.Apply(false, false); EditorUtility.SetDirty(palette);
                string materialPath = folder + "/Palette.mat"; var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    RefuseWrongType(materialPath); material = new Material(ring.WorldMaterial) { name = "Sodden native fog-aware palette" };
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                material.SetTexture("_BaseMap", palette); material.SetColor("_BaseColor", Color.white);
                material.enableInstancing = true; EditorUtility.SetDirty(material);
                primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var cube = primitive.GetComponent<MeshFilter>().sharedMesh; var cv = cube.vertices; var ct = cube.triangles;
                var entries = new List<SoddenNativeArtLibrary.Entry>(kit.models.Length);
                foreach (var model in kit.models)
                {
                    var vertices = new List<Vector3>(model.boxes.Length * 24); var uvs = new List<Vector2>(model.boxes.Length * 24);
                    var triangles = new List<int>(model.boxes.Length * 36);
                    foreach (var box in model.boxes)
                    {
                        int offset = vertices.Count;
                        foreach (var point in cv) { vertices.Add(box.center + Vector3.Scale(point, box.size)); uvs.Add(new Vector2((box.color + .5f) / colors.Length, .5f)); }
                        foreach (int index in ct) triangles.Add(offset + index);
                    }
                    string meshPath = folder + "/" + model.id + ".asset"; var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    if (mesh == null) { RefuseWrongType(meshPath); mesh = new Mesh { name = "sodden-native-art-" + model.id }; AssetDatabase.CreateAsset(mesh, meshPath); }
                    else mesh.Clear();
                    mesh.SetVertices(vertices); mesh.SetUVs(0, uvs); mesh.SetTriangles(triangles, 0);
                    mesh.RecalculateNormals(); mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh);
                    string prefabPath = folder + "/" + model.id + ".prefab"; var root = new GameObject("sodden-native-art-" + model.id); GameObject prefab;
                    try
                    {
                        root.AddComponent<MeshFilter>().sharedMesh = mesh; root.AddComponent<MeshRenderer>().sharedMaterial = material;
                        prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                        if (prefab == null) throw new InvalidOperationException("Sodden prefab import failed.");
                    }
                    finally { UnityEngine.Object.DestroyImmediate(root); }
                    entries.Add(new SoddenNativeArtLibrary.Entry { Id = model.id, Prefab = prefab, Mesh = mesh,
                        Spec = new SpawnRing3DCatalog.Model { id = model.id, path = prefabPath, sourceBlueprint = model.sourceBlueprint,
                            kind = SoddenNativeArtLibrary.KindForModel(model.id), materialFamily = "sodden-native-palette", rigFamily = "none",
                            boundsCenter = mesh.bounds.center, boundsSize = mesh.bounds.size, triangles = triangles.Count / 3,
                            clips = Array.Empty<string>(), sockets = Array.Empty<string>() } });
                    report.boxes += model.boxes.Length; report.triangles += triangles.Count / 3;
                }
                string libraryPath = folder + "/Library.asset"; var library = AssetDatabase.LoadAssetAtPath<SoddenNativeArtLibrary>(libraryPath);
                if (library == null) { RefuseWrongType(libraryPath); library = ScriptableObject.CreateInstance<SoddenNativeArtLibrary>(); AssetDatabase.CreateAsset(library, libraryPath); }
                library.SourceSha256 = report.sourceSha256; library.Material = material; library.Entries = entries.ToArray();
                library.Validate(); EditorUtility.SetDirty(library); AssetDatabase.SaveAssets(); report.models = entries.Count; report.status = "passed";
            }
            catch (Exception error) { report.status = "failed"; report.error = error.ToString(); throw; }
            finally
            {
                if (primitive != null) UnityEngine.Object.DestroyImmediate(primitive);
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(reportPath)));
                File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));
            }
        }
        static bool Finite(Vector3 value) => !float.IsNaN(value.x) && !float.IsInfinity(value.x)
            && !float.IsNaN(value.y) && !float.IsInfinity(value.y) && !float.IsNaN(value.z) && !float.IsInfinity(value.z);
        static void RefuseWrongType(string path)
        { if (AssetDatabase.LoadMainAssetAtPath(path) != null) throw new InvalidOperationException("Unexpected existing asset at " + path); }
    }
}
#endif
