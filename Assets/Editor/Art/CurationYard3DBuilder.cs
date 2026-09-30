#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace CavesOfOoo.Editor
{
    /// <summary>Exact15-form original Curation adoption. Completes source and all output
    /// preflights before writes, borrows the glade material and edits no scene.</summary>
    public static class CurationYard3DBuilder
    {
        private const string Source = "ArtSource/CurationYard3D/kit.json";
        public static void Run()=>Build("Docs/Verification/CurationReceivingYard/Art/static-import.json");
        public static void Build(string reportPath)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Scenery import requires idle Edit mode.");
            string json = File.ReadAllText(Source);
            string hash = Hash(System.Text.Encoding.UTF8.GetBytes(json));
            if (hash != CurationYard3DLibrary.ReviewedSourceSha256)
                throw new InvalidOperationException("Scenery source differs from the reviewed pack.");
            var source = JsonUtility.FromJson<CurationYardSource>(json);
            if (source == null) throw new InvalidOperationException("Missing scenery source.");
            source.Validate();
            var glade = ReferenceGladeVoxelLibrary.Load();
            if (glade == null) throw new InvalidOperationException("Approved glade material is unavailable.");
            glade.Validate();
            string folder = CurationYard3DLibrary.Folder;
            Preflight<CurationYard3DLibrary>(folder + "/Library.asset");
            foreach (var model in source.models)
            {
                Preflight<Mesh>(folder + "/" + model.id + ".asset");
                Preflight<GameObject>(folder + "/" + model.id + ".prefab");
            }
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Resources", "CurationYard3D");
            Scene preview = EditorSceneManager.NewPreviewScene();
            var entries = new List<CurationYard3DLibrary.Entry>(source.models.Length);
            try
            {
                foreach (var model in source.models)
                {
                    var vertices = new List<Vector3>(model.boxes.Length * 24);
                    var uv = new List<Vector2>(model.boxes.Length * 24);
                    var triangles = new List<int>(model.boxes.Length * 36);
                    foreach (var box in model.boxes) AppendBox(box, vertices, uv, triangles);
                    string meshPath = folder + "/" + model.id + ".asset";
                    var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    if (mesh == null) { mesh = new Mesh { name = model.id }; AssetDatabase.CreateAsset(mesh, meshPath); }
                    mesh.Clear(false); mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
                    mesh.RecalculateNormals(); mesh.RecalculateBounds();
                    EditorUtility.SetDirty(mesh); AssetDatabase.SaveAssetIfDirty(mesh);
                    string prefabPath = folder + "/" + model.id + ".prefab";
                    var root = new GameObject(model.id); GameObject prefab;
                    SceneManager.MoveGameObjectToScene(root, preview);
                    try
                    {
                        root.AddComponent<MeshFilter>().sharedMesh = mesh;
                        root.AddComponent<MeshRenderer>().sharedMaterial = glade.Material;
                        prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                    }
                    finally { UnityEngine.Object.DestroyImmediate(root); }
                    if (prefab == null) throw new InvalidOperationException("Scenery prefab save failed: " + model.id);
                    entries.Add(new CurationYard3DLibrary.Entry { Id = model.id, Mesh = mesh, Prefab = prefab,
                        Spec = new SpawnRing3DCatalog.Model { id = model.id, path = prefabPath, kind = "entity",
                            materialFamily = "reference-glade-palette", rigFamily = "none", boundsCenter = mesh.bounds.center,
                            boundsSize = mesh.bounds.size, triangles = triangles.Count / 3,
                            clips = Array.Empty<string>(), sockets = Array.Empty<string>() } });
                }
                string path = folder + "/Library.asset";
                var library = AssetDatabase.LoadAssetAtPath<CurationYard3DLibrary>(path);
                if (library == null) { library = ScriptableObject.CreateInstance<CurationYard3DLibrary>(); AssetDatabase.CreateAsset(library, path); }
                library.Entries = entries.ToArray(); library.Material = glade.Material; library.SourceSha256 = hash;
                library.Validate(); EditorUtility.SetDirty(library); AssetDatabase.SaveAssetIfDirty(library);
                Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
                int boxes = 0; foreach (var model in source.models) boxes += model.boxes.Length;
                File.WriteAllText(reportPath, JsonUtility.ToJson(new Receipt { sourceSha256 = hash, models = entries.Count,
                    boxes = boxes, vertices = boxes * 24, triangles = boxes * 12, paletteCells = 24 }, true));
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }
        private static void AppendBox(CurationYardSource.Box box, List<Vector3> vertices, List<Vector2> uv, List<int> triangles)
        {
            var c = new Vector3(box.center[0], box.center[1], box.center[2]);
            var size = new Vector3(box.size[0], box.size[1], box.size[2]);
            // Six disconnected faces preserve hard voxel normals, 24 vertices.
            Vector3[] face = {
                new Vector3(-.5f,-.5f,-.5f),new Vector3(-.5f,.5f,-.5f),new Vector3(.5f,.5f,-.5f),new Vector3(.5f,-.5f,-.5f),
                new Vector3(.5f,-.5f,.5f),new Vector3(.5f,.5f,.5f),new Vector3(-.5f,.5f,.5f),new Vector3(-.5f,-.5f,.5f),
                new Vector3(-.5f,-.5f,.5f),new Vector3(-.5f,.5f,.5f),new Vector3(-.5f,.5f,-.5f),new Vector3(-.5f,-.5f,-.5f),
                new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,.5f,-.5f),new Vector3(.5f,.5f,.5f),new Vector3(.5f,-.5f,.5f),
                new Vector3(-.5f,.5f,-.5f),new Vector3(-.5f,.5f,.5f),new Vector3(.5f,.5f,.5f),new Vector3(.5f,.5f,-.5f),
                new Vector3(-.5f,-.5f,.5f),new Vector3(-.5f,-.5f,-.5f),new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,-.5f,.5f)
            };
            int start = vertices.Count;
            foreach (var point in face) { vertices.Add(c + Vector3.Scale(point, size)); uv.Add(new Vector2((box.color + .5f) / 24f, .5f)); }
            for (int i = 0; i < 6; i++)
            { int n = start + i * 4; triangles.Add(n); triangles.Add(n + 1); triangles.Add(n + 2); triangles.Add(n); triangles.Add(n + 2); triangles.Add(n + 3); }
        }
        private static void Preflight<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            if ((asset != null && !(asset is T)) || (asset == null && File.Exists(path)))
                throw new InvalidOperationException("Refusing foreign or unimported scenery output: " + path);
        }
        private static string Hash(byte[] bytes)
        { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
        [Serializable] private sealed class Receipt { public string sourceSha256; public int models, boxes, vertices, triangles, paletteCells; }
    }
}
#endif
