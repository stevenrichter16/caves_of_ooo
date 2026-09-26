using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Three authored pool swatches using the shipped flat spring
    /// geometry and ring palette. Assets are borrowed; gameplay owns the pools.</summary>
    public sealed class DensityPhase1VoxelLibrary : ScriptableObject
    {
        public const string ResourcePath = "DensityPhase1Voxel3D/Library";
        private static readonly string[] Ids = { "density-pool-acid", "density-pool-memory", "density-pool-mirror" };

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
        public static DensityPhase1VoxelLibrary Load() => Resources.Load<DensityPhase1VoxelLibrary>(ResourcePath);

        /// <summary>Validate the three build-visible assets before publishing
        /// the lookup. Validation allocates only on load or explicit invalidation.</summary>
        public void Validate()
        {
            index = null;
            if (Entries == null || Entries.Length != Ids.Length)
                throw new InvalidOperationException("Density pool kit requires three authored swatches.");
            var ring = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
            if (ring == null || ring.WorldMaterial == null)
                throw new InvalidOperationException("Density pool kit requires the native ring palette material.");
            var candidates = new Dictionary<string, Entry>(StringComparer.Ordinal);
            foreach (var entry in Entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.Id) || entry.Prefab == null || entry.Mesh == null
                    || !entry.Mesh.isReadable || entry.Mesh.vertexCount == 0 || entry.Spec == null
                    || entry.Id != entry.Spec.id || candidates.ContainsKey(entry.Id))
                    throw new InvalidOperationException("Invalid or duplicate density pool entry.");
                var filter = entry.Prefab.GetComponent<MeshFilter>();
                var renderer = entry.Prefab.GetComponent<MeshRenderer>();
                var spec = entry.Spec;
                if (filter == null || filter.sharedMesh != entry.Mesh || renderer == null
                    || renderer.sharedMaterials.Length != 1 || renderer.sharedMaterial != ring.WorldMaterial
                    || entry.Prefab.GetComponentsInChildren<MeshRenderer>(true).Length != 1
                    || entry.Prefab.GetComponentsInChildren<Collider>(true).Length != 0
                    || entry.Prefab.GetComponentsInChildren<MonoBehaviour>(true).Length != 0
                    || entry.Prefab.transform.localPosition != Vector3.zero
                    || entry.Prefab.transform.localRotation != Quaternion.identity
                    || entry.Prefab.transform.localScale != Vector3.one
                    || spec.kind != "entity" || spec.materialFamily != "ring-palette"
                    || spec.rigFamily != "none" || spec.rigged
                    || spec.path != "Assets/Resources/DensityPhase1Voxel3D/" + entry.Id + ".prefab"
                    || spec.boundsCenter != entry.Mesh.bounds.center || spec.boundsSize != entry.Mesh.bounds.size
                    || spec.triangles != (int)entry.Mesh.GetIndexCount(0) / 3
                    || spec.clips == null || spec.clips.Length != 0
                    || spec.sockets == null || spec.sockets.Length != 0)
                    throw new InvalidOperationException("Density pool mesh, prefab or metadata mismatch: " + entry.Id);
                candidates.Add(entry.Id, entry);
            }
            foreach (string id in Ids)
                if (!candidates.ContainsKey(id))
                    throw new InvalidOperationException("Missing density pool swatch: " + id);
            index = candidates;
        }

        public Entry Find(string id)
        {
            if (id == null || !id.StartsWith("density-pool-", StringComparison.Ordinal)) return null;
            if (index == null) Validate();
            return index.TryGetValue(id, out var entry) ? entry : null;
        }

        private void OnValidate() => index = null;

        /// <summary>Exact blueprint aliases only. Unknown owners retain their
        /// ordinary native fallback rather than borrowing a pool's appearance.</summary>
        public static string ModelId(string blueprint)
        {
            switch (blueprint)
            {
                case "AcidPool": return Ids[0];
                case "MemoryBathPool": return Ids[1];
                case "MirrorMucilagePool": return Ids[2];
                default: return null;
            }
        }
    }
}
