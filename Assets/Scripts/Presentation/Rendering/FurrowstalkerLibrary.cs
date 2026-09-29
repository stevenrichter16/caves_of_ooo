using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Two optional original hunter forms. Current anatomy and corpse provenance select them independently of hunt phase.</summary>
    public sealed class FurrowstalkerLibrary : ScriptableObject
    {
        public const string ResourcePath = "Furrowstalker3D/Library";
        public const string Hunter = "spread-furrowstalker", Remains = "spread-furrowstalker-remains";
        public static readonly string[] ModelIds = { Hunter, Remains };
        public static readonly string[] ClipNames = { "Idle", "Walk", "Interact", "Attack", "Hit" };
        public static readonly string[] BoneNames = { "Root", "Body", "Neck", "Head", "Jaw", "LegFront.L", "LegRear.L", "LegFront.R", "LegRear.R", "Tail", "TailTip" };
        [Serializable] public sealed class Entry
        { public string Id; public GameObject Prefab; public Mesh Mesh; public Material[] Materials; public SpawnRing3DCatalog.Model Spec; }
        public Entry[] Entries;
        Dictionary<string, Entry> index;
        public static FurrowstalkerLibrary Load() => Resources.Load<FurrowstalkerLibrary>(ResourcePath);
        void OnValidate() { index = null; }
        public static string Blueprint(string id)
        { switch(id) { case Hunter: return "Furrowstalker"; case Remains: return "FurrowstalkerCorpse"; default: return null; } }
        public Entry Find(string id)
        { if (Blueprint(id) == null) return null; if (index == null) Validate(); return index.TryGetValue(id, out var value) ? value : null; }
        public void Validate()
        {
            index = null;
            var palette = ReferenceGladeVoxelLibrary.Load()?.Material;
            if (palette == null || Entries == null || Entries.Length != 2)
                throw new InvalidOperationException("Two original hunter forms and the existing approved palette required.");
            var next = new Dictionary<string, Entry>(StringComparer.Ordinal);
            foreach (var e in Entries)
            {
                if (e == null || Blueprint(e.Id) == null || next.ContainsKey(e.Id) || e.Prefab == null || e.Spec == null
                    || e.Mesh == null || !e.Mesh.isReadable || e.Mesh.vertexCount < 100 || e.Mesh.vertexCount > 65535)
                    throw new InvalidOperationException("Invalid original exploration form.");
                bool rigged = e.Id == Hunter; const int slots = 1;
                if (e.Prefab.transform.localPosition != Vector3.zero || e.Prefab.transform.localRotation != Quaternion.identity
                    || e.Prefab.transform.localScale != Vector3.one || e.Mesh.subMeshCount != slots || e.Mesh.blendShapeCount != 0
                    || e.Materials == null || e.Materials.Length != slots || e.Materials[0] != palette
                    || e.Prefab.GetComponentsInChildren<Collider>(true).Length != 0 || e.Prefab.GetComponentsInChildren<Rigidbody>(true).Length != 0
                    || e.Prefab.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                    throw new InvalidOperationException("Inert original form/material ownership failed: " + e.Id);
                var renderers = e.Prefab.GetComponentsInChildren<Renderer>(true);
                var skins = e.Prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var filters = e.Prefab.GetComponentsInChildren<MeshFilter>(true);
                var animators = e.Prefab.GetComponentsInChildren<Animator>(true);
                if (renderers.Length != 1 || !renderers[0].enabled || !ActiveParents(renderers[0].transform, e.Prefab.transform)
                    || !renderers[0].sharedMaterials.SequenceEqual(e.Materials)
                    || renderers[0].forceRenderingOff || renderers[0].shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.On || !renderers[0].receiveShadows)
                    throw new InvalidOperationException("Actual original renderer differs from its complete material contract.");
                if (rigged)
                {
                    if (skins.Length != 1 || filters.Length != 0 || animators.Length != 1 || skins[0].sharedMesh != e.Mesh
                        || animators[0].avatar == null || !animators[0].avatar.isValid || animators[0].applyRootMotion
                        || animators[0].runtimeAnimatorController == null || skins[0].bones.Length != BoneNames.Length
                        || skins[0].rootBone == null || !skins[0].rootBone.IsChildOf(e.Prefab.transform))
                        throw new InvalidOperationException("Original hunter rig is incomplete.");
                    var bones = skins[0].bones;
                    if (!bones.Select(b => b == null ? null : b.name).OrderBy(n => n).SequenceEqual(BoneNames.OrderBy(n => n))
                        || bones.Any(b => b == null || !b.IsChildOf(e.Prefab.transform)) || e.Mesh.bindposeCount != bones.Length
                        || e.Mesh.boneWeights.Length != e.Mesh.vertexCount)
                        throw new InvalidOperationException("Original hunter has foreign/missing bones.");
                    foreach (var w in e.Mesh.boneWeights)
                        if (w.weight0 != 1 || w.weight1 != 0 || w.weight2 != 0 || w.weight3 != 0 || w.boneIndex0 < 0 || w.boneIndex0 >= bones.Length)
                            throw new InvalidOperationException("Hunter cuboids require rigid actual bone weights.");
                    var clips = animators[0].runtimeAnimatorController.animationClips;
                    if (clips.Length != 5 || ClipNames.Any(n => clips.Count(c => c != null && c.name == n && c.length > 0) != 1))
                        throw new InvalidOperationException("Five original hunter clips required.");
                }
                else if (skins.Length != 0 || animators.Length != 0 || filters.Length != 1 || filters[0].sharedMesh != e.Mesh
                    || e.Mesh.bindposeCount != 0 || e.Mesh.boneWeights.Length != 0)
                    throw new InvalidOperationException("Static original form may not carry a rig.");
                if (e.Mesh.normals.Length != e.Mesh.vertexCount || e.Mesh.uv.Length != e.Mesh.vertexCount)
                    throw new InvalidOperationException("Incomplete original geometry buffers.");
                foreach (var n in e.Mesh.normals) if (!Finite(n.x) || !Finite(n.y) || !Finite(n.z) || n.sqrMagnitude < .9f || n.sqrMagnitude > 1.1f)
                    throw new InvalidOperationException("Invalid original normal.");
                foreach (var uv in e.Mesh.uv) if (!Finite(uv.x) || !Finite(uv.y) || uv.y != .5f || !Enumerable.Range(0, 24).Any(i => Mathf.Abs(uv.x - (i + .5f) / 24f) < .00001f))
                    throw new InvalidOperationException("Invalid original palette coordinate.");
                var points = e.Mesh.vertices;
                var bounds = new Bounds(points[0], Vector3.zero);
                foreach (var v in points) { if (!Finite(v.x) || !Finite(v.y) || !Finite(v.z)) throw new InvalidOperationException("Nonfinite original form."); bounds.Encapsulate(v); }
                if ((bounds.center - e.Mesh.bounds.center).sqrMagnitude > .000001f || (bounds.size - e.Mesh.bounds.size).sqrMagnitude > .000001f)
                    throw new InvalidOperationException("Original geometry bounds differ from actual vertices.");
                int triangles = 0;
                for (int slot = 0; slot < slots; slot++)
                { if (e.Mesh.GetTopology(slot) != UnityEngine.MeshTopology.Triangles || e.Mesh.GetIndexCount(slot) == 0) throw new InvalidOperationException("Empty original material piece."); triangles += (int)e.Mesh.GetIndexCount(slot) / 3; }
                if (triangles != (rigged ? 480 : 192)) throw new InvalidOperationException("Original hunter triangle contract differs.");
                var spec = e.Spec;
                if (spec.id != e.Id || spec.sourceBlueprint != Blueprint(e.Id) || spec.rigged != rigged || spec.kind != (rigged ? "actor" : "entity")
                    || spec.path != "Assets/Resources/Furrowstalker3D/" + e.Id + ".prefab" || spec.triangles != triangles
                    || spec.materialFamily != "reference-glade-palette" || spec.rigFamily != (rigged ? "quadruped" : "")
                    || spec.clips == null || !spec.clips.SequenceEqual(rigged ? ClipNames : Array.Empty<string>()) || spec.sockets == null || spec.sockets.Length != 0)
                    throw new InvalidOperationException("Original form metadata differs from submitted geometry.");
                next.Add(e.Id, e);
            }
            index = next;
        }
        static bool ActiveParents(Transform child, Transform root)
        { for (var t = child; t != null; t = t.parent) { if (!t.gameObject.activeSelf) return false; if (t == root) return true; } return false; }
        static bool Finite(float x) => !float.IsNaN(x) && !float.IsInfinity(x);
        internal static SpawnRing3DRecipe Refine(Zone zone, Entity owner, SpawnRing3DRecipe native)
        {
            string bp = owner?.BlueprintName;
            if (bp != "Furrowstalker" && bp != "FurrowstalkerCorpse") return native;
            if (!SpreadPresentationScope.IsActive(zone) || !ReferenceEquals(native.Owner, owner)
                || native.Failure != null && native.Failure != "unmodeled-native-blueprint") return native;
            var cell = zone.GetEntityCell(owner); var r = owner.GetPart<RenderPart>(); var p = owner.GetPart<PhysicsPart>();
            if (owner.SpatialZone != zone || cell == null || cell.ParentZone != zone || !cell.Objects.Contains(owner)
                || r == null || r.ParentEntity != owner || !r.Visible || p == null || p.ParentEntity != owner
                || p.InInventory != null || p.Equipped != null || !string.IsNullOrEmpty(r.VisualID) || !string.IsNullOrEmpty(r.VisualVariant)
                || !string.IsNullOrEmpty(r.GlyphVariants) || owner.HasPart<SpatialFootprintPart>() || owner.HasPart<MultiCellPilotPropPart>()) return native;
            string model;
            if (bp == "Furrowstalker")
            {
                var brain = owner.GetPart<BrainPart>(); var body = owner.GetPart<Body>(); var role = owner.GetPart<SpreadPredatorPart>();
                if (r.RenderString != "f" || r.ColorString != "&y" || p.Takeable || !owner.HasTag("Creature") || owner.HasTag("Item")
                    || brain == null || brain.ParentEntity != owner || body == null || body.ParentEntity != owner
                    || role == null || role.ParentEntity != owner) return native;
                // Anatomy survives aborted, fed and saved roles; phase is gameplay, not visual identity.
                model = Hunter;
            }
            else
            {
                if (r.RenderString != "%" || r.ColorString != "&y" || !p.Takeable || !owner.HasTag("Corpse") || owner.HasTag("Creature")
                    || owner.GetProperty("SourceBlueprint") != "Furrowstalker" || string.IsNullOrEmpty(owner.GetProperty("SourceID"))) return native;
                model = Remains;
            }
            if (Load()?.Find(model) == null) return native;
            return new SpawnRing3DRecipe(owner, model, native.ComponentId, Village3DProjection.CellCentre(cell.X, cell.Y), true, false, quarterTurns: native.QuarterTurns);
        }
    }
}
