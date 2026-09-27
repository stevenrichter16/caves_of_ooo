#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
namespace CavesOfOoo.Editor
{
    /// <summary>Adopts only the twelve original fitted forms. Complete source,
    /// material and owned-output preflight precedes the first persistent write.</summary>
    public static class SpreadEquipment3DBuilder
    {
        const string Source = "ArtSource/SpreadEquipment3D/worn-source.json";
        [Serializable] public sealed class Report { public string status, error, sourceHash; public string[] assets; public int models, vertices; }
        [MenuItem("Caves of Ooo/Art/Build Spread fitted equipment")]
        public static void RunMenu() => Run();
        public static Report Run(string reportPath = "Docs/Verification/DensityCompletion/SpreadBiome/Equipment/native-import.json")
        {
            var report = new Report(); var changed = new List<string>();
            try
            {
                if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                    throw new InvalidOperationException("Fitted equipment import requires Edit mode.");
                string text = File.ReadAllText(Source); report.sourceHash = Hash(Source);
                if (report.sourceHash != SpreadEquipmentSourceHash.Value) throw new InvalidOperationException("Fitted source hash changed.");
                var source = JsonUtility.FromJson<SpreadEquipmentSource>(text); source.Validate();
                var portable = SpreadPortable3DLibrary.Load(); if (portable == null) throw new InvalidOperationException("Portable palette must be adopted first.");
                portable.Validate();
                string libraryPath = SpreadEquipment3DLibrary.Folder + "/Library.asset";
                Preflight<SpreadEquipment3DLibrary>(libraryPath);
                foreach (var model in source.models) Preflight<Mesh>(SpreadEquipment3DLibrary.Folder + "/" + model.id + ".asset");
                var plans = new List<Plan>();
                foreach (var model in source.models) plans.Add(Prepare(model));
                EnsureFolder(SpreadEquipment3DLibrary.Folder); var entries = new List<SpreadEquipment3DLibrary.Entry>();
                for (int i = 0; i < source.models.Length; i++)
                {
                    var model = source.models[i]; string path = SpreadEquipment3DLibrary.Folder + "/" + model.id + ".asset";
                    var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if (mesh == null) { mesh = new Mesh { name = model.id }; AssetDatabase.CreateAsset(mesh, path); }
                    var plan = plans[i]; mesh.Clear(false); mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
                    mesh.SetVertices(plan.Vertices); mesh.SetUVs(0, plan.Uvs); mesh.SetTriangles(plan.Triangles, 0);
                    mesh.boneWeights = plan.Weights.ToArray(); mesh.bindposes = new[] { Matrix4x4.identity };
                    mesh.RecalculateNormals(); mesh.RecalculateBounds();
                    if (mesh.vertexCount != plan.Vertices.Count || mesh.GetIndexCount(0) != plan.Triangles.Count)
                        throw new InvalidOperationException("Fitted persistent mesh buffer write incomplete.");
                    EditorUtility.SetDirty(mesh); AssetDatabase.SaveAssetIfDirty(mesh); changed.Add(path); report.vertices += mesh.vertexCount;
                    entries.Add(new SpreadEquipment3DLibrary.Entry { Id = model.id, Slot = model.slot, Mesh = mesh });
                }
                var library = AssetDatabase.LoadAssetAtPath<SpreadEquipment3DLibrary>(libraryPath);
                if (library == null) { library = ScriptableObject.CreateInstance<SpreadEquipment3DLibrary>(); AssetDatabase.CreateAsset(library, libraryPath); }
                library.Entries = entries.ToArray(); library.Material = portable.Material; library.SourceSha256 = report.sourceHash;
                library.Validate(); EditorUtility.SetDirty(library); AssetDatabase.SaveAssetIfDirty(library); changed.Add(libraryPath);
                report.models = entries.Count; report.status = "passed";
            }
            catch (Exception error) { report.status = "failed"; report.error = error.ToString(); throw; }
            finally
            {
                report.assets = changed.ToArray();
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(reportPath))); File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));
            }
            return report;
        }
        sealed class Plan
        {
            public readonly List<Vector3> Vertices = new List<Vector3>(); public readonly List<Vector2> Uvs = new List<Vector2>();
            public readonly List<int> Triangles = new List<int>(); public readonly List<BoneWeight> Weights = new List<BoneWeight>();
        }
        static Plan Prepare(SpreadEquipmentSource.Model model)
        {
            var plan = new Plan();
            // Six outward quads, duplicated at face boundaries for crisp cuboid
            // normals. No temporary Unity object, mesh or active scene mutation.
            var vertices = new[] {
                new Vector3(-.5f,-.5f,-.5f),new Vector3(-.5f,-.5f,.5f),new Vector3(-.5f,.5f,.5f),new Vector3(-.5f,.5f,-.5f),
                new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,.5f,-.5f),new Vector3(.5f,.5f,.5f),new Vector3(.5f,-.5f,.5f),
                new Vector3(-.5f,-.5f,-.5f),new Vector3(.5f,-.5f,-.5f),new Vector3(.5f,-.5f,.5f),new Vector3(-.5f,-.5f,.5f),
                new Vector3(-.5f,.5f,-.5f),new Vector3(-.5f,.5f,.5f),new Vector3(.5f,.5f,.5f),new Vector3(.5f,.5f,-.5f),
                new Vector3(-.5f,-.5f,-.5f),new Vector3(-.5f,.5f,-.5f),new Vector3(.5f,.5f,-.5f),new Vector3(.5f,-.5f,-.5f),
                new Vector3(-.5f,-.5f,.5f),new Vector3(.5f,-.5f,.5f),new Vector3(.5f,.5f,.5f),new Vector3(-.5f,.5f,.5f) };
            var triangles = new int[36];
            for (int face = 0; face < 6; face++)
            { int v = face*4, t = face*6; triangles[t]=v;triangles[t+1]=v+1;triangles[t+2]=v+2;triangles[t+3]=v;triangles[t+4]=v+2;triangles[t+5]=v+3; }
            foreach (var box in model.boxes)
            {
                int start = plan.Vertices.Count; var center = new Vector3(box.center[0], box.center[1], box.center[2]); var size = new Vector3(box.size[0], box.size[1], box.size[2]);
                foreach (var v in vertices)
                {
                    plan.Vertices.Add(center + Vector3.Scale(v, size)); plan.Uvs.Add(new Vector2((box.paint + .5f) / 42f, .5f));
                    plan.Weights.Add(new BoneWeight { boneIndex0 = 0, weight0 = 1 });
                }
                foreach (int triangle in triangles) plan.Triangles.Add(start + triangle);
            }
            return plan;
        }
        static string Hash(string path) { using (var hash = SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant(); }
        static void Preflight<T>(string path) where T : Object
        { var asset = AssetDatabase.LoadMainAssetAtPath(path); if ((asset != null && !(asset is T)) || (asset == null && File.Exists(path))) throw new InvalidOperationException("Refusing foreign output type: " + path); }
        static void EnsureFolder(string path)
        { var parent = Path.GetDirectoryName(path).Replace('\\', '/'); if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent); if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, Path.GetFileName(path)); }
    }
}
#endif
