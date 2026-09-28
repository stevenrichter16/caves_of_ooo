using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Four optional original forms. Exact native owners select them; the pack never becomes a global presentation prerequisite.</summary>
    public sealed class QuestFreeSpreadArtLibrary : ScriptableObject
    {
        public const string ResourcePath = "QuestFreeSpread3D/Library";
        public const string Grazer = "questfree-spread-grazer", Remains = "questfree-spread-grazer-remains",
            Full = "questfree-spread-draw-full", Empty = "questfree-spread-draw-empty";
        public static readonly string[] ModelIds = { Grazer, Remains, Full, Empty };
        public static readonly string[] ClipNames = { "Idle", "Walk", "Interact", "Attack", "Hit" };
        public static readonly string[] BoneNames = { "Root", "Body", "Neck", "Head", "LegFront.L", "LegRear.L", "LegFront.R", "LegRear.R", "Tail" };
        [Serializable] public sealed class Entry
        { public string Id; public GameObject Prefab; public Mesh Mesh; public Material[] Materials; public SpawnRing3DCatalog.Model Spec; }
        public Entry[] Entries;
        Dictionary<string, Entry> index;
        public static QuestFreeSpreadArtLibrary Load() => Resources.Load<QuestFreeSpreadArtLibrary>(ResourcePath);
        void OnValidate() { index = null; }
        public static string Blueprint(string id)
        { switch(id) { case Grazer: return "ReedbackGrazer"; case Remains: return "ReedbackGrazerCorpse"; case Full: case Empty: return "SpreadDrawPoint"; default: return null; } }
        public Entry Find(string id)
        { if (Blueprint(id) == null) return null; if (index == null) Validate(); return index.TryGetValue(id, out var value) ? value : null; }
        public void Validate()
        {
            index = null;
            var palette = ReferenceGladeVoxelLibrary.Load()?.Material;
            var water = PouredLiquid3DLibrary.Load()?.Find(PouredLiquid3DLibrary.ModelId("&c"))?.Material;
            if (palette == null || water == null || Entries == null || Entries.Length != 4)
                throw new InvalidOperationException("Four original forms and existing approved palette/water materials required.");
            var next = new Dictionary<string, Entry>(StringComparer.Ordinal);
            foreach (var e in Entries)
            {
                if (e == null || Blueprint(e.Id) == null || next.ContainsKey(e.Id) || e.Prefab == null || e.Spec == null
                    || e.Mesh == null || !e.Mesh.isReadable || e.Mesh.vertexCount < 100 || e.Mesh.vertexCount > 65535)
                    throw new InvalidOperationException("Invalid original exploration form.");
                bool rigged = e.Id == Grazer; int slots = e.Id == Full ? 2 : 1;
                if (e.Prefab.transform.localPosition != Vector3.zero || e.Prefab.transform.localRotation != Quaternion.identity
                    || e.Prefab.transform.localScale != Vector3.one || e.Mesh.subMeshCount != slots || e.Mesh.blendShapeCount != 0
                    || e.Materials == null || e.Materials.Length != slots || e.Materials[0] != palette || slots == 2 && e.Materials[1] != water
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
                        throw new InvalidOperationException("Original grazer rig is incomplete.");
                    var bones = skins[0].bones;
                    if (!bones.Select(b => b == null ? null : b.name).OrderBy(n => n).SequenceEqual(BoneNames.OrderBy(n => n))
                        || bones.Any(b => b == null || !b.IsChildOf(e.Prefab.transform)) || e.Mesh.bindposeCount != bones.Length
                        || e.Mesh.boneWeights.Length != e.Mesh.vertexCount)
                        throw new InvalidOperationException("Original grazer has foreign/missing bones.");
                    foreach (var w in e.Mesh.boneWeights)
                        if (w.weight0 != 1 || w.weight1 != 0 || w.weight2 != 0 || w.weight3 != 0 || w.boneIndex0 < 0 || w.boneIndex0 >= bones.Length)
                            throw new InvalidOperationException("Grazer cuboids require rigid actual bone weights.");
                    var clips = animators[0].runtimeAnimatorController.animationClips;
                    if (clips.Length != 5 || ClipNames.Any(n => clips.Count(c => c != null && c.name == n && c.length > 0) != 1))
                        throw new InvalidOperationException("Five original grazer clips required.");
                }
                else if (skins.Length != 0 || animators.Length != 0 || filters.Length != 1 || filters[0].sharedMesh != e.Mesh
                    || e.Mesh.bindposeCount != 0 || e.Mesh.boneWeights.Length != 0)
                    throw new InvalidOperationException("Static original form may not carry a rig.");
                if (e.Mesh.normals.Length != e.Mesh.vertexCount || e.Mesh.uv.Length != e.Mesh.vertexCount)
                    throw new InvalidOperationException("Incomplete original geometry buffers.");
                foreach (var n in e.Mesh.normals) if (!Finite(n.x) || !Finite(n.y) || !Finite(n.z) || n.sqrMagnitude < .9f || n.sqrMagnitude > 1.1f)
                    throw new InvalidOperationException("Invalid original normal.");
                foreach (var uv in e.Mesh.uv) if (!Finite(uv.x) || !Finite(uv.y) || uv.x < 0 || uv.x > 1 || uv.y != .5f)
                    throw new InvalidOperationException("Invalid original palette coordinate.");
                var points = e.Mesh.vertices;
                var bounds = new Bounds(points[0], Vector3.zero);
                foreach (var v in points) { if (!Finite(v.x) || !Finite(v.y) || !Finite(v.z)) throw new InvalidOperationException("Nonfinite original form."); bounds.Encapsulate(v); }
                if ((bounds.center - e.Mesh.bounds.center).sqrMagnitude > .000001f || (bounds.size - e.Mesh.bounds.size).sqrMagnitude > .000001f)
                    throw new InvalidOperationException("Original geometry bounds differ from actual vertices.");
                int triangles = 0;
                for (int slot = 0; slot < slots; slot++)
                { if (e.Mesh.GetTopology(slot) != UnityEngine.MeshTopology.Triangles || e.Mesh.GetIndexCount(slot) == 0) throw new InvalidOperationException("Empty original material piece."); triangles += (int)e.Mesh.GetIndexCount(slot) / 3; }
                var spec = e.Spec;
                if (spec.id != e.Id || spec.sourceBlueprint != Blueprint(e.Id) || spec.rigged != rigged || spec.kind != (rigged ? "actor" : "entity")
                    || spec.path != "Assets/Resources/QuestFreeSpread3D/" + e.Id + ".prefab" || spec.triangles != triangles
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
            if (bp != "ReedbackGrazer" && bp != "ReedbackGrazerCorpse" && bp != "SpreadDrawPoint") return native;
            if (!SpreadPresentationScope.IsActive(zone) || !ReferenceEquals(native.Owner, owner)
                || native.Failure != null && native.Failure != "unmodeled-native-blueprint") return native;
            var cell = zone.GetEntityCell(owner); var r = owner.GetPart<RenderPart>(); var p = owner.GetPart<PhysicsPart>();
            if (cell == null || !cell.Objects.Contains(owner) || r == null || r.ParentEntity != owner || !r.Visible || p == null || p.ParentEntity != owner
                || p.InInventory != null || p.Equipped != null || !string.IsNullOrEmpty(r.VisualID) || !string.IsNullOrEmpty(r.VisualVariant)
                || !string.IsNullOrEmpty(r.GlyphVariants) || owner.HasPart<SpatialFootprintPart>() || owner.HasPart<MultiCellPilotPropPart>()) return native;
            string model;
            if (bp == "ReedbackGrazer")
            {
                var brain = owner.GetPart<BrainPart>(); var body = owner.GetPart<Body>(); var grazer = owner.GetPart<SpreadGrazerPart>();
                if (r.RenderString != "g" || r.ColorString != "&y" || p.Takeable || !owner.HasTag("Creature") || owner.HasTag("Item")
                    || brain == null || brain.ParentEntity != owner || body == null || body.ParentEntity != owner || grazer == null || grazer.ParentEntity != owner) return native;
                model = Grazer;
            }
            else if (bp == "ReedbackGrazerCorpse")
            {
                if (r.RenderString != "%" || r.ColorString != "&y" || !p.Takeable || !owner.HasTag("Corpse") || owner.HasTag("Creature")
                    || owner.GetProperty("SourceBlueprint") != "ReedbackGrazer" || string.IsNullOrEmpty(owner.GetProperty("SourceID"))) return native;
                model = Remains;
            }
            else
            {
                var pool = owner.GetPart<LiquidPoolPart>();
                if (r.RenderString != "~" || r.ColorString != "&c" || p.Takeable || p.Solid || owner.HasTag("Creature") || owner.HasTag("Item")
                    || !owner.HasTag("Terrain") || pool == null || pool.ParentEntity != owner || pool.LiquidId != "water" || pool.Volume < 0 || pool.Volume > 3
                    || owner.HasPart<WellPart>() || owner.HasPart<TileStateSourcePart>()) return native;
                model = pool.Volume > 0 ? Full : Empty;
            }
            if (Load()?.Find(model) == null) return native;
            return new SpawnRing3DRecipe(owner, model, native.ComponentId, Village3DProjection.CellCentre(cell.X, cell.Y), true, false, quarterTurns: native.QuarterTurns);
        }
    }
}
