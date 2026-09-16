using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Already-voxel Olderdeep scenery. Entries describe art only: native
    /// entity membership owns visibility, interaction state, occupancy and damage.</summary>
    public sealed class OlderdeepVoxelLibrary : ScriptableObject
    {
        public const string ResourcePath = "OlderdeepVoxel3D/Library";
        public const int VariantCount = 4;
        private static readonly string[] Families = { "ground", "rooted", "plume", "niche", "plaque", "oldest", "jar", "listener", "tender", "wall" };
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
        public static OlderdeepVoxelLibrary Load() => Resources.Load<OlderdeepVoxelLibrary>(ResourcePath);

        /// <summary>Validates the complete build-visible kit and atomically
        /// publishes its lookup. Broken assets fail loudly before presentation.</summary>
        public void Validate()
        {
            index = null;
            if (Entries == null || Entries.Length != Ids.Length)
                throw new InvalidOperationException("Olderdeep voxel kit requires four variants of 10 families.");
            var ring = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
            if (ring == null || ring.WorldMaterial == null)
                throw new InvalidOperationException("Olderdeep voxel kit requires the native ring palette material.");
            var candidates = new Dictionary<string, Entry>(StringComparer.Ordinal);
            foreach (var e in Entries)
            {
                if (e == null || string.IsNullOrEmpty(e.Id) || e.Prefab == null || e.Mesh == null
                    || !e.Mesh.isReadable || e.Mesh.vertexCount == 0 || e.Spec == null
                    || e.Id != e.Spec.id || candidates.ContainsKey(e.Id))
                    throw new InvalidOperationException("Invalid or duplicate Olderdeep voxel entry.");
                var filter = e.Prefab.GetComponent<MeshFilter>();
                var renderer = e.Prefab.GetComponent<MeshRenderer>();
                var spec = e.Spec;
                bool isGround = e.Id.StartsWith("olderdeep-ground-", StringComparison.Ordinal);
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
                    || spec.path != "Assets/Resources/OlderdeepVoxel3D/" + e.Id + ".prefab"
                    || spec.boundsCenter != e.Mesh.bounds.center || spec.boundsSize != e.Mesh.bounds.size
                    || spec.triangles != (int)e.Mesh.GetIndexCount(0) / 3
                    || spec.clips == null || spec.clips.Length != 0
                    || spec.sockets == null || spec.sockets.Length != 0)
                    throw new InvalidOperationException("Olderdeep voxel mesh, prefab or metadata mismatch: " + e.Id);
                candidates.Add(e.Id, e);
            }
            foreach (string id in Ids)
                if (!candidates.ContainsKey(id))
                    throw new InvalidOperationException("Missing Olderdeep voxel variant: " + id);
            index = candidates;
        }

        public Entry Find(string id)
        {
            if (id == null || !id.StartsWith("olderdeep-", StringComparison.Ordinal)) return null;
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
                    ids[n++] = "olderdeep-" + family + "-" + variant;
            return ids;
        }

        /// <summary>Returns a cached identifier for a known family and variant.
        /// Invalid authoring inputs throw instead of borrowing another family's art.</summary>
        public static string ModelId(string family, int variant)
        {
            int offset;
            switch (family)
            {
                case "ground": offset = 0; break;
                case "rooted": offset = 4; break;
                case "plume": offset = 8; break;
                case "niche": offset = 12; break;
                case "plaque": offset = 16; break;
                case "oldest": offset = 20; break;
                case "jar": offset = 24; break;
                case "listener": offset = 28; break;
                case "tender": offset = 32; break;
                case "wall": offset = 36; break;
                default: throw new ArgumentException("Unknown Olderdeep voxel family.", nameof(family));
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
                case "StoneFloor": return "ground";
                case "SandstoneFloor": return "ground";
                case "TheRooted": return "rooted";
                case "FoundingPlume": return "plume";
                case "NicheHome": return "niche";
                case "PlaqueWall": return "plaque";
                case "PlaqueOldest": return "oldest";
                case "BeetleJar": return "jar";
                case "FoundingListener": return "listener";
                case "FoundingPlaqueTender": return "tender";
                default: return null;
            }
        }
    }
}
