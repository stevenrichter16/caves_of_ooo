#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Editor
{
    /// <summary>Offline, exact one-output physical variation. All source checks
    /// precede writes; borrowed original body/rig/palette assets stay untouched.</summary>
    public static class SpreadLatchcoilBuilder
    {
        const string Folder = "Assets/Resources/SpreadLatchcoil3D";
        [Serializable] public sealed class Report
        {
            public string status, error;
            public string[] written;
            public int models;
            public bool borrowedBytesUnchanged;
        }
        public static Report Build(string reportPath)
        {
            var report = new Report();
            var written = new List<string>();
            try
            {
                if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
                    throw new InvalidOperationException("Idle editor required for rare-body import.");
                var animals = SpreadBiomeActorLibrary.Load();
                var glade = ReferenceGladeVoxelLibrary.Load();
                if (animals == null || glade == null) throw new InvalidOperationException("Approved original source required.");
                animals.Validate(); glade.Validate();
                var original = animals.Find(SpreadLatchcoilLibrary.SourceModel);
                var source = original.Prefab;
                var skin = source.GetComponentInChildren<SkinnedMeshRenderer>(true);
                var animator = source.GetComponentInChildren<Animator>(true);
                if (skin == null || animator == null || animator.avatar == null || !animator.avatar.isValid
                    || animator.runtimeAnimatorController == null || source.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length != 1
                    || source.GetComponentsInChildren<Collider>(true).Length != 0 || original.Mesh == null)
                    throw new InvalidOperationException("Exact native viper rig is incomplete.");
                var borrowed = Capture(source, original.Mesh, glade.Material);
                Preflight<SpreadLatchcoilLibrary>(Folder + "/Library.asset");
                var plans = new List<Plan>();
                foreach (string id in SpreadLatchcoilLibrary.ModelIds)
                {
                    Preflight<Mesh>(Folder + "/" + id + ".asset");
                    Preflight<GameObject>(Folder + "/" + id + ".prefab");
                    plans.Add(Prepare(id, source, skin, original.Mesh));
                }
                AssertUnchanged(borrowed);
                EnsureFolder(Folder);
                var entries = new List<SpreadLatchcoilLibrary.Entry>();
                for (int i = 0; i < plans.Count; i++)
                {
                    string id = SpreadLatchcoilLibrary.ModelIds[i], path = Folder + "/" + id;
                    var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path + ".asset");
                    if (mesh == null) { mesh = new Mesh(); AssetDatabase.CreateAsset(mesh, path + ".asset"); }
                    plans[i].Fill(mesh); mesh.name = id;
                    SpreadLatchcoilLibrary.ValidatePair(original.Mesh, mesh);
                    EditorUtility.SetDirty(mesh); AssetDatabase.SaveAssetIfDirty(mesh); written.Add(path + ".asset");
                    var scene = EditorSceneManager.NewPreviewScene(); GameObject instance = null;
                    try
                    {
                        instance = (GameObject)PrefabUtility.InstantiatePrefab(source, scene); instance.name = id;
                        var ownedSkin = instance.GetComponentInChildren<SkinnedMeshRenderer>(true);
                        var envelope = ownedSkin.localBounds;
                        ownedSkin.sharedMesh = mesh; ownedSkin.sharedMaterial = glade.Material;
                        envelope.Encapsulate(mesh.bounds.min); envelope.Encapsulate(mesh.bounds.max); ownedSkin.localBounds = envelope;
                        var ownedAnimator = instance.GetComponentInChildren<Animator>(true); ownedAnimator.applyRootMotion = false;
                        ownedAnimator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                        var spec = new SpawnRing3DCatalog.Model
                        {
                            id = id, path = path + ".prefab", sourceBlueprint = SpreadLatchcoilLibrary.Blueprint(id),
                            kind = "actor", rigFamily = "serpent", rigged = true, materialFamily = "reference-glade-palette",
                            boundsCenter = ownedSkin.bounds.center, boundsSize = ownedSkin.bounds.size,
                            triangles = (int)mesh.GetIndexCount(0) / 3,
                            clips = new[] { "Idle", "Walk", "Interact", "Attack", "Hit" },
                            sockets = Array.Empty<string>()
                        };
                        var prefab = PrefabUtility.SaveAsPrefabAsset(instance, path + ".prefab", out bool saved);
                        if (!saved || prefab == null) throw new InvalidOperationException("Rare-body prefab save refused.");
                        entries.Add(new SpreadLatchcoilLibrary.Entry { Id = id, Mesh = mesh, Prefab = prefab, Spec = spec });
                        written.Add(path + ".prefab");
                    }
                    finally { if (instance != null) Object.DestroyImmediate(instance); EditorSceneManager.ClosePreviewScene(scene); }
                }
                string libraryPath = Folder + "/Library.asset";
                var library = AssetDatabase.LoadAssetAtPath<SpreadLatchcoilLibrary>(libraryPath);
                if (library == null) { library = ScriptableObject.CreateInstance<SpreadLatchcoilLibrary>(); AssetDatabase.CreateAsset(library, libraryPath); }
                library.SourcePrefab = source; library.SourceMesh = original.Mesh; library.Material = glade.Material; library.Entries = entries.ToArray();
                library.Validate(); EditorUtility.SetDirty(library); AssetDatabase.SaveAssetIfDirty(library); written.Add(libraryPath);
                AssertUnchanged(borrowed); report.borrowedBytesUnchanged = true; report.models = entries.Count; report.status = "passed";
            }
            catch (Exception exception) { report.status = "failed"; report.error = exception.ToString(); throw; }
            finally
            {
                report.written = written.ToArray(); string full = Path.GetFullPath(reportPath);
                Directory.CreateDirectory(Path.GetDirectoryName(full)); File.WriteAllText(full, JsonUtility.ToJson(report, true));
            }
            return report;
        }
        sealed class Plan
        {
            internal Mesh Source;
            internal List<Vector3> Vertices, Normals;
            internal List<Vector2> Uvs;
            internal List<BoneWeight> Weights;
            internal List<int> Indices;
            internal void Fill(Mesh mesh)
            {
                mesh.Clear(false); mesh.indexFormat = Vertices.Count > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : Source.indexFormat;
                mesh.SetVertices(Vertices); mesh.SetNormals(Normals); mesh.SetUVs(0, Uvs);
                mesh.SetTriangles(Indices, 0); mesh.boneWeights = Weights.ToArray(); mesh.bindposes = Source.bindposes;
                if (Source.colors32.Length != 0)
                    mesh.colors32 = Source.colors32.Concat(Enumerable.Repeat(new Color32(255,255,255,255), Vertices.Count - Source.vertexCount)).ToArray();
                if (Source.tangents.Length != 0)
                    mesh.tangents = Source.tangents.Concat(Enumerable.Repeat(new Vector4(1,0,0,1), Vertices.Count - Source.vertexCount)).ToArray();
                for (int channel = 1; channel < 8; channel++)
                {
                    var values = new List<Vector4>(); Source.GetUVs(channel, values);
                    if (values.Count == 0) continue;
                    values.AddRange(Enumerable.Repeat(Vector4.zero, Vertices.Count - Source.vertexCount)); mesh.SetUVs(channel, values);
                }
                mesh.RecalculateBounds();
            }
        }
        static Plan Prepare(string id, GameObject prefab, SkinnedMeshRenderer skin, Mesh source)
        {
            if (SpreadLatchcoilLibrary.Blueprint(id) == null || source == null || !source.isReadable
                || source.subMeshCount != 1 || source.normals.Length != source.vertexCount || source.uv.Length != source.vertexCount
                || source.boneWeights.Length != source.vertexCount || source.bindposeCount != skin.bones.Length || source.blendShapeCount != 0)
                throw new InvalidOperationException("Unsupported original body buffers.");
            var indices = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < skin.bones.Length; i++)
            {
                var bone = skin.bones[i];
                if (bone == null || !bone.IsChildOf(prefab.transform) || !indices.TryAdd(bone.name, i)) throw new InvalidOperationException("Invalid native body bone.");
            }
            if (indices.Count != 23 || new[] { "Root", "Body", "Head" }.Concat(Enumerable.Range(0, 20).Select(n => "Coil." + n)).Any(n => !indices.ContainsKey(n)))
                throw new InvalidOperationException("Exact original 23-bone serpent anatomy required.");
            var plan = new Plan { Source = source, Vertices = source.vertices.ToList(), Normals = source.normals.ToList(),
                Uvs = source.uv.ToList(), Weights = source.boneWeights.ToList(), Indices = source.triangles.ToList() };
            var fromMesh = prefab.transform.worldToLocalMatrix * skin.transform.localToWorldMatrix;
            var toMesh = fromMesh.inverse;
            if (!Finite(fromMesh.determinant) || Mathf.Abs(fromMesh.determinant) < .00001f) throw new InvalidOperationException("Invalid original body transform.");
            Bounds BoneBounds(string name)
            {
                var points = plan.Vertices.Where((v, i) => plan.Weights[i].boneIndex0 == indices[name]).Select(fromMesh.MultiplyPoint3x4).ToArray();
                if (points.Length == 0) throw new InvalidOperationException("Unweighted mark anchor bone.");
                var b = new Bounds(points[0], Vector3.zero); foreach (var p in points) b.Encapsulate(p); return b;
            }
            void Box(Vector3 center, Vector3 size, string bone, int swatch)
            {
                if (size.x <= 0 || size.y <= 0 || size.z <= 0) throw new InvalidOperationException("Invalid physical mark size.");
                var corners = new[] { new Vector3(-1,-1,-1), new Vector3(1,-1,-1), new Vector3(1,1,-1), new Vector3(-1,1,-1),
                    new Vector3(-1,-1,1), new Vector3(1,-1,1), new Vector3(1,1,1), new Vector3(-1,1,1) };
                int[][] faces = { new[]{4,5,6,7},new[]{1,0,3,2},new[]{5,1,2,6},new[]{0,4,7,3},new[]{7,6,2,3},new[]{0,1,5,4} };
                foreach (var face in faces)
                {
                    int start = plan.Vertices.Count;
                    var normal = Vector3.Cross(corners[face[1]] - corners[face[0]], corners[face[2]] - corners[face[0]]).normalized;
                    normal = toMesh.inverse.transpose.MultiplyVector(normal).normalized;
                    foreach (int corner in face)
                    {
                        var point = toMesh.MultiplyPoint3x4(center + Vector3.Scale(corners[corner], size) * .5f);
                        if (!Finite(point.x) || !Finite(point.y) || !Finite(point.z)) throw new InvalidOperationException("Nonfinite physical mark.");
                        plan.Vertices.Add(point); plan.Normals.Add(normal); plan.Uvs.Add(new Vector2((swatch + .5f) / 24, .5f));
                        plan.Weights.Add(new BoneWeight { boneIndex0 = indices[bone], weight0 = 1 });
                    }
                    int[] triangles = toMesh.determinant > 0 ? new[]{0,1,2,0,2,3} : new[]{0,2,1,0,3,2};
                    foreach (int index in triangles) plan.Indices.Add(start + index);
                }
            }
            // Four open pale collars: two dorsal lips and one alternating
            // side strip. Their real geometry follows distinct existing coils.
            // Every top lip is embedded into the original local surface; the
            // central gap preserves the original dark dorsal scale and face.
            int number = 0;
            foreach (int segment in new[] { 3, 7, 12, 17 })
            {
                string bone = "Coil." + segment;
                var body = BoneBounds(bone);
                float z = body.center.z;
                foreach (float side in new[] { -1f, 1f })
                {
                    float x = body.center.x + side * body.size.x * .31f;
                    float surface = OriginalSurfaceHeight(x, z, indices[bone], source, fromMesh);
                    Box(new Vector3(x, surface + .002f, z), new Vector3(body.size.x * .32f, .015f, .025f), bone, 17);
                }
                float edge = number++ % 2 == 0 ? body.min.x + .002f : body.max.x - .002f;
                Box(new Vector3(edge, body.center.y - .003f, z), new Vector3(.014f, body.size.y * .70f, .025f), bone, 17);
            }
            return plan;
        }
        static float OriginalSurfaceHeight(float x, float z, int bone, Mesh source, Matrix4x4 toRoot)
        {
            var vertices = source.vertices; var weights = source.boneWeights; var triangles = source.triangles;
            float highest = float.NegativeInfinity;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int ia = triangles[i], ib = triangles[i + 1], ic = triangles[i + 2];
                if (weights[ia].boneIndex0 != bone || weights[ib].boneIndex0 != bone || weights[ic].boneIndex0 != bone) continue;
                var a = toRoot.MultiplyPoint3x4(vertices[ia]); var b = toRoot.MultiplyPoint3x4(vertices[ib]); var c = toRoot.MultiplyPoint3x4(vertices[ic]);
                float denominator = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
                if (Mathf.Abs(denominator) < .000001f) continue;
                float u = ((b.z - c.z) * (x - c.x) + (c.x - b.x) * (z - c.z)) / denominator;
                float v = ((c.z - a.z) * (x - c.x) + (a.x - c.x) * (z - c.z)) / denominator;
                float w = 1 - u - v;
                if (u >= -.00001f && v >= -.00001f && w >= -.00001f)
                    highest = Mathf.Max(highest, u * a.y + v * b.y + w * c.y);
            }
            if (!Finite(highest)) throw new InvalidOperationException("No original body surface beneath the coil band.");
            return highest;
        }
        static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
        static void Preflight<T>(string path) where T : Object
        {
            var current = AssetDatabase.LoadMainAssetAtPath(path);
            if ((File.Exists(path) || current != null) && AssetDatabase.LoadAssetAtPath<T>(path) == null)
                throw new InvalidOperationException("Refusing unrelated output: " + path);
            if (current != null && EditorUtility.IsDirty(current)) throw new InvalidOperationException("Refusing dirty owned output: " + path);
        }
        static Dictionary<string,string> Capture(params Object[] objects)
        {
            var hashes = new Dictionary<string,string>(StringComparer.Ordinal);
            foreach (var value in objects)
            {
                string path = AssetDatabase.GetAssetPath(value);
                if (string.IsNullOrEmpty(path) || !File.Exists(path)) throw new InvalidOperationException("Persistent original source required.");
                foreach (string dependency in AssetDatabase.GetDependencies(path, true).Concat(new[]{path}))
                    foreach (string file in new[]{dependency,dependency+".meta"}) if (File.Exists(file) && !hashes.ContainsKey(file)) hashes.Add(file,Hash(file));
            }
            return hashes;
        }
        static string Hash(string path) { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant(); }
        static void AssertUnchanged(Dictionary<string,string> hashes)
        { foreach (var row in hashes) if (!File.Exists(row.Key) || Hash(row.Key) != row.Value) throw new InvalidOperationException("Borrowed source changed: " + row.Key); }
        static void EnsureFolder(string path)
        { if (AssetDatabase.IsValidFolder(path)) return; string parent = Path.GetDirectoryName(path).Replace('\\','/'); EnsureFolder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path)); }
    }
}
#endif
