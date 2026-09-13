using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Already-voxel Beating scenery. Entries describe art only: native
    /// entity membership owns visibility, liquid contact, occupancy and damage.</summary>
    public sealed class BeatingVoxelLibrary : ScriptableObject
    {
        public const string ResourcePath = "BeatingVoxel3D/Library";
        public const int VariantCount = 4;
        private static readonly string[] Families = { "sand", "pan", "road", "crust", "dune", "ruin", "vein", "brine", "bones", "sign", "rubble", "briar" };
        private static readonly string[] Ids = MakeIds();

        [Serializable]
        public sealed class Entry
        {
            public string Id;
            public GameObject Prefab;
            public Mesh Mesh;
            public SpawnRing3DCatalog.Model Spec;
        }

        public Entry[] Entries;
        private Dictionary<string, Entry> index;
        public static BeatingVoxelLibrary Load() => Resources.Load<BeatingVoxelLibrary>(ResourcePath);

        /// <summary>Validates the complete build-visible kit and atomically
        /// publishes its lookup. Broken assets fail loudly before presentation.</summary>
        public void Validate()
        {
            index = null;
            if (Entries == null || Entries.Length != Ids.Length)
                throw new InvalidOperationException("Beating voxel kit requires four variants of twelve families.");
            var ring = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
            if (ring == null || ring.WorldMaterial == null)
                throw new InvalidOperationException("Beating voxel kit requires the native ring palette material.");
            var candidates = new Dictionary<string, Entry>(StringComparer.Ordinal);
            foreach (var e in Entries)
            {
                if (e == null || string.IsNullOrEmpty(e.Id) || e.Prefab == null || e.Mesh == null
                    || !e.Mesh.isReadable || e.Mesh.vertexCount == 0 || e.Spec == null
                    || e.Id != e.Spec.id || candidates.ContainsKey(e.Id))
                    throw new InvalidOperationException("Invalid or duplicate Beating voxel entry.");
                var filter = e.Prefab.GetComponent<MeshFilter>();
                var renderer = e.Prefab.GetComponent<MeshRenderer>();
                var spec = e.Spec;
                bool isGround = e.Id.StartsWith("beating-sand-", StringComparison.Ordinal)
                    || e.Id.StartsWith("beating-pan-", StringComparison.Ordinal)
                    || e.Id.StartsWith("beating-road-", StringComparison.Ordinal);
                string expectedKind = isGround ? "ground" : "entity";
                if (filter == null || filter.sharedMesh != e.Mesh || renderer == null
                    || renderer.sharedMaterials.Length != 1 || renderer.sharedMaterial != ring.WorldMaterial
                    || e.Prefab.GetComponentsInChildren<MeshRenderer>(true).Length != 1
                    || e.Prefab.GetComponentsInChildren<Collider>(true).Length != 0
                    || e.Prefab.GetComponentsInChildren<MonoBehaviour>(true).Length != 0
                    || e.Prefab.transform.localPosition != Vector3.zero
                    || e.Prefab.transform.localRotation != Quaternion.identity
                    || e.Prefab.transform.localScale != Vector3.one
                    || spec.kind != expectedKind || spec.materialFamily != "ring-palette"
                    || spec.rigFamily != "none" || spec.rigged
                    || spec.path != "Assets/Resources/BeatingVoxel3D/" + e.Id + ".prefab"
                    || spec.boundsCenter != e.Mesh.bounds.center || spec.boundsSize != e.Mesh.bounds.size
                    || spec.triangles != (int)e.Mesh.GetIndexCount(0) / 3
                    || spec.clips == null || spec.clips.Length != 0
                    || spec.sockets == null || spec.sockets.Length != 0)
                    throw new InvalidOperationException("Beating voxel mesh, prefab or metadata mismatch: " + e.Id);
                candidates.Add(e.Id, e);
            }
            foreach (string id in Ids)
                if (!candidates.ContainsKey(id))
                    throw new InvalidOperationException("Missing Beating voxel variant: " + id);
            index = candidates;
        }

        public Entry Find(string id)
        {
            if (id == null || !id.StartsWith("beating-", StringComparison.Ordinal)) return null;
            if (index == null) Validate();
            return index.TryGetValue(id, out var entry) ? entry : null;
        }

        private void OnValidate() => index = null;

        private static string[] MakeIds()
        {
            var ids = new string[Families.Length * VariantCount];
            int n = 0;
            foreach (string family in Families)
                for (int variant = 0; variant < VariantCount; variant++)
                    ids[n++] = "beating-" + family + "-" + variant;
            return ids;
        }

        /// <summary>Returns a cached identifier for a known family and variant.
        /// Invalid authoring inputs throw instead of borrowing another family's art.</summary>
        public static string ModelId(string family, int variant)
        {
            int offset;
            switch (family)
            {
                case "sand": offset = 0; break;
                case "pan": offset = 4; break;
                case "road": offset = 8; break;
                case "crust": offset = 12; break;
                case "dune": offset = 16; break;
                case "ruin": offset = 20; break;
                case "vein": offset = 24; break;
                case "brine": offset = 28; break;
                case "bones": offset = 32; break;
                case "sign": offset = 36; break;
                case "rubble": offset = 40; break;
                case "briar": offset = 44; break;
                default: throw new ArgumentException("Unknown Beating voxel family.", nameof(family));
            }
            if (variant < 0 || variant >= VariantCount)
                throw new ArgumentOutOfRangeException(nameof(variant));
            return Ids[offset + variant];
        }

        /// <summary>Exact visual aliases only; no inference from tags, names or lore.</summary>
        public static string Family(string blueprint)
        {
            switch (blueprint)
            {
                case "Sand": return "sand";
                case "RoadStone": case "SandstoneFloor": return "road";
                case "SaltCrust": return "crust";
                case "DuneCrest": return "dune";
                case "SandstoneWall": return "ruin";
                case "PaleSaltVein": return "vein";
                case "BrinePool": return "brine";
                case "Bones": return "bones";
                case "Signpost": return "sign";
                case "Rubble": return "rubble";
                case "Saltbriar": return "briar";
                default: return null;
            }
        }
    }
}
