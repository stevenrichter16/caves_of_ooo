#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Editor
{
    /// <summary>Explicit import of the seven saved Blender studies. Owns only
    /// Art3D/SpellFx and Resources/SpellFx3D; never edits scenes or shared rigs.</summary>
    public static class NativeSpellFxAssetBuilder
    {
        const string Art = "Assets/Art3D/SpellFx";
        const string LibraryPath = "Assets/Resources/SpellFx3D/Library.asset";
        const string Source = "ArtSource/StarterSpell3D";
        static readonly string[] StudyIds = { "ember_spit", "flaming_hands", "jet_blast", "ground_surge", "rime_grip", "calm", "conjure_rain" };
        static readonly string[] SpellIds = { "Pyromancy_EmberSpit", "Pyromancy_FlamingHands", "Hydromancy_JetBlast", "Galvanism_GroundSurge", "Cryomancy_RimeGrip", "Spellcraft_Calm", "Hydromancy_ConjureRain" };
        static readonly string[] BoneNames = { "Root", "Spine", "Head", "Arm.L", "Arm.R", "Leg.L", "Leg.R", "Hand.L", "Hand.R" };
        [Serializable] sealed class Export { public int schemaVersion, fps, frameCount, releaseFrame; public string sourceBlendSha256; public MaterialData[] materials; public MeshData[] meshes; public Study[] studies; }
        [Serializable] sealed class MaterialData { public string id; public float[] rgbaSrgb; }
        [Serializable] sealed class MeshData { public string id; public float[] vertices, normals; public int[] triangles; public int materialIndex; }
        [Serializable] sealed class Study { public string id; public float contactFrame, clearFrame, authoredDistanceCells, authoredMuzzleForward; public float[] projectileProgress; public Piece[] pieces; }
        [Serializable] sealed class Piece { public string id, meshId, role, anchor, condition; public int forwardCell, lateralCell, variant; public bool reducedEssential; public Pose[] poses; }
        [Serializable] sealed class Pose { public float[] position, rotation, scale; }
        [Serializable] public sealed class Report { public string status, error, sourceBlendSha256, runtimeJsonSha256; public int meshes, triangles, pieces; public List<ClipReport> clips = new List<ClipReport>(); }
        [Serializable] public sealed class ClipReport { public string id, sourceTake, sourceGuid, clipGuid; public int curves; public float maximumPoseDegrees, maximumRestFrameDifference; public List<BoneReport> bones = new List<BoneReport>(); }
        [Serializable] public sealed class BoneReport { public string name, sourcePath, nativePath; public Quaternion sourceRest, nativeRest; public Vector3 sourcePosition, nativePosition; }

        [MenuItem("Tools/Caves of Ooo/Starter magic/Import polished Blender studies")]
        public static void Build() => Import(null);
        public static void RunFromCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, "-spellFxReport");
            Import(i >= 0 && i + 1 < args.Length ? args[i + 1] : null);
        }
        public static void Import(string reportPath)
        {
            var report = new Report { status = "RUNNING" };
            try
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before explicit art import.");
                string json = File.ReadAllText(Source + "/runtime/starter_spell_library.json");
                var data = JsonUtility.FromJson<Export>(json);
                Require(data != null && data.schemaVersion == 1 && data.fps == 100 && data.frameCount == 111 && data.releaseFrame == 22, "Unexpected sampled library schema.");
                report.sourceBlendSha256 = Hash(File.ReadAllBytes(Source + "/starter_spells.blend"));
                report.runtimeJsonSha256 = Hash(System.Text.Encoding.UTF8.GetBytes(json));
                Require(report.sourceBlendSha256 == data.sourceBlendSha256, "Runtime mesh export does not match the saved Blender source.");
                Require(data.studies.Length == StudyIds.Length && data.studies.Select(s => s.id).OrderBy(s => s).SequenceEqual(StudyIds.OrderBy(s => s)), "Expected exactly seven starter studies.");
                foreach (string id in StudyIds) Require(File.Exists(Source + "/exports/" + id + ".fbx"), "Missing source FBX " + id);
                var village = Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath);
                Require(village != null && village.FindModel(village.PlayerModelId) != null, "Existing native player rig is required.");
                foreach (string folder in new[] { Art + "/Source", Art + "/Meshes", Art + "/Animations", Art + "/Materials", "Assets/Resources/SpellFx3D" }) Folder(folder);
                var shader = Shader.Find("CavesOfOoo/Village3D/Palette"); Require(shader != null, "Existing fog palette shader is required.");
                var material = Owned(Art + "/Materials/SpellFolk.mat", () => new Material(shader));
                material.SetTexture("_BaseMap", Texture2D.whiteTexture); material.SetTexture("_FogLight", Texture2D.blackTexture);
                material.SetFloat("_Transient", 1); material.SetColor("_BaseColor", Color.white); material.enableInstancing = true; Save(material);
                var meshes = new Dictionary<string, Mesh>(StringComparer.Ordinal);
                var colors = new Dictionary<string, Color>(StringComparer.Ordinal);
                foreach (var row in data.meshes)
                {
                    Require(row.vertices.Length == row.normals.Length && row.vertices.Length % 3 == 0, "Malformed mesh " + row.id);
                    Require(row.materialIndex >= 0 && row.materialIndex < data.materials.Length, "Invalid material index.");
                    var mesh = Owned(Art + "/Meshes/" + SafeName(row.id) + ".asset", () => new Mesh()); mesh.Clear(); mesh.name = row.id;
                    mesh.vertices = Triples(row.vertices); mesh.normals = Triples(row.normals); mesh.triangles = row.triangles; mesh.RecalculateBounds(); Save(mesh);
                    meshes.Add(row.id, mesh);
                    var rgba = data.materials[row.materialIndex].rgbaSrgb;
                    Require(rgba.Length == 4 && rgba[3] == 1, "Only opaque authored materials are supported.");
                    colors.Add(row.id, new Color(rgba[0], rgba[1], rgba[2], rgba[3]).linear);
                    report.meshes++; report.triangles += row.triangles.Length / 3;
                }
                var entries = new List<NativeSpellFxEntry>();
                var ring = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
                Require(ring != null, "Existing native ring library is required.");
                var nativePrefabs = village.Models.Select(m => m.Prefab).Concat(ring.Models.Select(m => m.Prefab)).Distinct().ToArray();
                foreach (var study in data.studies)
                {
                    int index = Array.IndexOf(StudyIds, study.id);
                    var clip = ImportCast(study.id, village.FindModel(village.PlayerModelId), report);
                    var pieces = new List<NativeSpellFxPiece>();
                    foreach (var p in study.pieces)
                        pieces.Add(new NativeSpellFxPiece {
                            Id = p.id, Mesh = meshes[p.meshId], Color = colors[p.meshId],
                            Role = (NativeSpellFxRole)Enum.Parse(typeof(NativeSpellFxRole), p.role),
                            Anchor = (NativeSpellFxAnchor)Enum.Parse(typeof(NativeSpellFxAnchor), p.anchor),
                            Condition = (NativeSpellFxCondition)Enum.Parse(typeof(NativeSpellFxCondition), p.condition),
                            ForwardCell = p.forwardCell, LateralCell = p.lateralCell, Variant = p.variant, ReducedEssential = p.reducedEssential,
                            Poses = p.poses.Select(v => new NativeSpellFxPose { Position = V3(v.position), Rotation = new Quaternion(v.rotation[0], v.rotation[1], v.rotation[2], v.rotation[3]), Scale = V3(v.scale) }).ToArray()
                        });
                    entries.Add(new NativeSpellFxEntry { SpellId = SpellIds[index], CastClip = clip, CastBindings = CastBindings(study.id, clip, nativePrefabs), CastDuration = .66f,
                        SampleRate = data.fps, ReleaseFrame = data.releaseFrame, StudyContactFrame = study.contactFrame, StudyClearFrame = study.clearFrame,
                        AuthoredDistanceCells = study.authoredDistanceCells, LaunchDistance = study.authoredMuzzleForward,
                        ProjectileProgress = study.projectileProgress, Pieces = pieces.ToArray() });
                    report.pieces += pieces.Count;
                }
                var library = Owned(LibraryPath, ScriptableObject.CreateInstance<NativeSpellFxLibrary>);
                library.Material = material; library.Entries = entries.ToArray(); library.Validate(); Save(library);
                report.status = "PASS";
            }
            catch (Exception error) { report.status = "FAIL"; report.error = error.ToString(); throw; }
            finally
            {
                string path = string.IsNullOrEmpty(reportPath) ? Path.Combine(Path.GetTempPath(), "StarterSpellImport-" + Guid.NewGuid().ToString("N") + ".json") : Path.GetFullPath(reportPath);
                Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path, JsonUtility.ToJson(report, true) + "\n");
                Debug.Log("[StarterSpellImport] " + report.status + " " + path);
            }
        }

        static NativeSpellCastBinding[] CastBindings(string id, AnimationClip cast, GameObject[] prefabs)
        {
            var result = new List<NativeSpellCastBinding>();
            var paths = new HashSet<string>(StringComparer.Ordinal);
            var original = AnimationUtility.GetCurveBindings(cast);
            foreach (var prefab in prefabs)
            {
                var animator = prefab.GetComponentInChildren<Animator>(true); if (animator == null) continue;
                var transforms = animator.GetComponentsInChildren<Transform>(true);
                // Non-humanoid models retain their existing generic interaction gesture.
                if (BoneNames.Any(n => transforms.Count(t => t.name == n) != 1)) continue;
                var bones = BoneNames.ToDictionary(n => n, n => transforms.Single(t => t.name == n));
                string rigPath = AnimationUtility.CalculateTransformPath(bones["Root"].parent, animator.transform);
                Require(!string.IsNullOrEmpty(rigPath), "Expected explicit armature under native Animator.");
                if (!paths.Add(rigPath)) continue;
                var mapped = original.Select(b => new { Source = b, Bone = bones[b.path.Substring(b.path.LastIndexOf('/') + 1)] }).ToArray();
                bool unchanged = mapped.All(m => m.Source.path == AnimationUtility.CalculateTransformPath(m.Bone, animator.transform));
                AnimationClip clip = cast;
                if (!unchanged)
                {
                    string suffix = SafeName(prefab.name.Replace(' ', '_'));
                    clip = Owned(Art + "/Animations/" + id + "__" + suffix + ".anim", () => new AnimationClip());
                    clip.ClearCurves(); clip.name = id + "_Cast_" + suffix; clip.frameRate = 100; clip.wrapMode = WrapMode.Once;
                    foreach (var m in mapped)
                    {
                        var binding = m.Source; binding.path = AnimationUtility.CalculateTransformPath(m.Bone, animator.transform);
                        AnimationUtility.SetEditorCurve(clip, binding, AnimationUtility.GetEditorCurve(cast, m.Source));
                    }
                    clip.EnsureQuaternionContinuity(); Save(clip);
                }
                // Verify every actual compatible rig's rest orientation against the clip,
                // including exports that rename the armature. No blind path-only retarget.
                var instance = Object.Instantiate(prefab);
                try
                {
                    var a = instance.GetComponentInChildren<Animator>(true); a.enabled = false;
                    var posed = BoneNames.Select(n => a.GetComponentsInChildren<Transform>(true).Single(t => t.name == n)).ToArray();
                    var rest = posed.Select(b => b.localRotation).ToArray(); clip.SampleAnimation(a.gameObject, 0);
                    for (int b = 0; b < BoneNames.Length; b++) Require(Quaternion.Angle(rest[b], posed[b].localRotation) < .2f, "Native clip changes bind rest: " + prefab.name + "/" + BoneNames[b]);
                }
                finally { Object.DestroyImmediate(instance); }
                result.Add(new NativeSpellCastBinding { RigPath = rigPath, Clip = clip });
            }
            Require(result.Count > 1, "Expected both original town and renamed ring armature bindings.");
            return result.ToArray();
        }

        static AnimationClip ImportCast(string id, GameObject nativePrefab, Report report)
        {
            string path = Art + "/Source/" + id + ".fbx";
            byte[] bytes = File.ReadAllBytes(Source + "/exports/" + id + ".fbx");
            if (!File.Exists(path) || !File.ReadAllBytes(path).SequenceEqual(bytes)) { File.WriteAllBytes(path, bytes); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport); }
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            string before = EditorJsonUtility.ToJson(importer);
            importer.globalScale = 1; importer.useFileScale = true; importer.bakeAxisConversion = false; importer.preserveHierarchy = true;
            importer.importCameras = false; importer.importLights = false; importer.addCollider = false; importer.importVisibility = false;
            importer.importNormals = ModelImporterNormals.Import; importer.importTangents = ModelImporterTangents.None;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importAnimation = true; importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel; importer.optimizeGameObjects = false;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            if (before != EditorJsonUtility.ToJson(importer)) importer.SaveAndReimport();
            var sourceClips = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal)).ToArray();
            Require(sourceClips.Length == 1, "Expected one baked study take: " + id + "; found " + sourceClips.Length);
            var sourceClip = sourceClips[0];
            var source = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            var native = Object.Instantiate(nativePrefab);
            var row = new ClipReport { id = id, sourceTake = sourceClip.name, sourceGuid = AssetDatabase.AssetPathToGUID(path) }; report.clips.Add(row);
            try
            {
                foreach (var animator in source.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
                var nativeAnimator = native.GetComponentInChildren<Animator>(true); Require(nativeAnimator != null, "Missing native Animator."); nativeAnimator.enabled = false;
                var sourceRig = source.GetComponentsInChildren<Transform>(true).Single(t => t.name == id + "__CasterRig");
                var sourceBones = BoneNames.Select(n => sourceRig.GetComponentsInChildren<Transform>(true).Single(t => t.name == n)).ToArray();
                var nativeBones = BoneNames.Select(n => nativeAnimator.GetComponentsInChildren<Transform>(true).Single(t => t.name == n)).ToArray();
                sourceClip.SampleAnimation(source, 0);
                var sourceRest = sourceBones.Select(b => b.localRotation).ToArray();
                var nativeRest = nativeBones.Select(b => b.localRotation).ToArray();
                var samples = new Quaternion[BoneNames.Length, 67];
                for (int b = 0; b < BoneNames.Length; b++)
                {
                    row.maximumRestFrameDifference = Mathf.Max(row.maximumRestFrameDifference, Quaternion.Angle(sourceRest[b], nativeRest[b]));
                    row.bones.Add(new BoneReport { name = BoneNames[b], sourcePath = AnimationUtility.CalculateTransformPath(sourceBones[b], source.transform),
                        nativePath = AnimationUtility.CalculateTransformPath(nativeBones[b], nativeAnimator.transform), sourceRest = sourceRest[b], nativeRest = nativeRest[b],
                        sourcePosition = sourceBones[b].localPosition, nativePosition = nativeBones[b].localPosition });
                    // Both exports use the unchanged source bone hierarchy and roll.
                    // A global FBX facing change may alter Root's rest frame only.
                    if (b > 0) Require(Quaternion.Angle(sourceRest[b], nativeRest[b]) < .1f, "Source bone basis changed; explicit retargeting required: " + id + "/" + BoneNames[b]);
                    if (b > 0) Require(sourceBones[b].parent.name == nativeBones[b].parent.name, "Bone hierarchy mismatch.");
                }
                for (int frame = 0; frame <= 66; frame++)
                {
                    sourceClip.SampleAnimation(source, frame / 100f);
                    for (int b = 0; b < BoneNames.Length; b++)
                    {
                        Quaternion q = nativeRest[b] * Quaternion.Inverse(sourceRest[b]) * sourceBones[b].localRotation;
                        if (frame > 0 && Quaternion.Dot(samples[b, frame - 1], q) < 0) q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
                        samples[b, frame] = q;
                        row.maximumPoseDegrees = Mathf.Max(row.maximumPoseDegrees, Quaternion.Angle(nativeRest[b], q));
                    }
                }
                Require(row.maximumPoseDegrees > 5, "Imported source has no actual gesture.");
                string clipPath = Art + "/Animations/" + id + ".anim";
                var clip = Owned(clipPath, () => new AnimationClip()); clip.ClearCurves(); clip.name = id + "_Cast"; clip.frameRate = 100; clip.wrapMode = WrapMode.Once;
                for (int b = 0; b < BoneNames.Length; b++)
                {
                    Require(Quaternion.Angle(samples[b, 0], samples[b, 66]) < .2f, "Cast does not settle to rest: " + BoneNames[b]);
                    string bonePath = row.bones[b].nativePath;
                    for (int axis = 0; axis < 4; axis++)
                    {
                        var keys = new Keyframe[67]; for (int frame = 0; frame <= 66; frame++) keys[frame] = new Keyframe(frame / 100f, samples[b, frame][axis]);
                        var curve = new AnimationCurve(keys);
                        for (int k = 0; k < keys.Length; k++) { AnimationUtility.SetKeyLeftTangentMode(curve, k, AnimationUtility.TangentMode.Linear); AnimationUtility.SetKeyRightTangentMode(curve, k, AnimationUtility.TangentMode.Linear); }
                        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(bonePath, typeof(Transform), "m_LocalRotation." + "xyzw"[axis]), curve); row.curves++;
                    }
                }
                clip.EnsureQuaternionContinuity(); Save(clip); row.clipGuid = AssetDatabase.AssetPathToGUID(clipPath); return clip;
            }
            finally { Object.DestroyImmediate(source); Object.DestroyImmediate(native); }
        }

        static string SafeName(string id) { Require(!string.IsNullOrWhiteSpace(id) && id.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'), "Unsafe exported mesh ID."); return id; }
        static Vector3 V3(float[] v) { Require(v != null && v.Length == 3, "Invalid vector."); return new Vector3(v[0], v[1], v[2]); }
        static Vector3[] Triples(float[] values) { var result = new Vector3[values.Length / 3]; for (int i = 0; i < result.Length; i++) result[i] = new Vector3(values[i * 3], values[i * 3 + 1], values[i * 3 + 2]); return result; }
        static T Owned<T>(string path, Func<T> create) where T : Object
        { var value = AssetDatabase.LoadAssetAtPath<T>(path); if (value != null) return value; Require(!File.Exists(path) && AssetDatabase.LoadMainAssetAtPath(path) == null, "Unexpected asset at owned path " + path); value = create(); AssetDatabase.CreateAsset(value, path); return value; }
        static void Save(Object value) { EditorUtility.SetDirty(value); AssetDatabase.SaveAssetIfDirty(value); }
        static void Folder(string path) { if (AssetDatabase.IsValidFolder(path)) return; Folder(Path.GetDirectoryName(path).Replace('\\', '/')); AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path)); }
        static string Hash(byte[] bytes) { using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
#endif
