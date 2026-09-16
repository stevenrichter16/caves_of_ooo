using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Already-voxel DrownedLedger scenery. Entries describe art only: native
    /// entity membership owns visibility, interaction state, occupancy and damage.</summary>
    public sealed class DrownedLedgerVoxelKitLibrary : ScriptableObject
    {
        public const string ResourcePath = "DrownedLedgerVoxel3D/Library";
        public const int VariantCount = 4;
        private static readonly string[] Families = { "preserved", "stake", "table", "scribe", "sorter", "parcel", "boards" };
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
        public static DrownedLedgerVoxelKitLibrary Load() => Resources.Load<DrownedLedgerVoxelKitLibrary>(ResourcePath);

        /// <summary>Validates the complete build-visible kit and atomically
        /// publishes its lookup. Broken assets fail loudly before presentation.</summary>
        public void Validate()
        {
            index = null;
            if (Entries == null || Entries.Length != Ids.Length)
                throw new InvalidOperationException("DrownedLedger voxel kit requires four variants of 7 families.");
            var ring = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
            if (ring == null || ring.WorldMaterial == null)
                throw new InvalidOperationException("DrownedLedger voxel kit requires the native ring palette material.");
            var candidates = new Dictionary<string, Entry>(StringComparer.Ordinal);
            foreach (var e in Entries)
            {
                if (e == null || string.IsNullOrEmpty(e.Id) || e.Prefab == null || e.Mesh == null
                    || !e.Mesh.isReadable || e.Mesh.vertexCount == 0 || e.Spec == null
                    || e.Id != e.Spec.id || candidates.ContainsKey(e.Id))
                    throw new InvalidOperationException("Invalid or duplicate DrownedLedger voxel entry.");
                var filter = e.Prefab.GetComponent<MeshFilter>();
                var renderer = e.Prefab.GetComponent<MeshRenderer>();
                var spec = e.Spec;
                bool isGround = e.Id.StartsWith("drownedledger-ground-", StringComparison.Ordinal);
                string expectedKind = isGround ? "ground" : "entity";
                if (filter == null || filter.sharedMesh != e.Mesh || renderer == null
                    || renderer.sharedMaterials.Length != 1 || renderer.sharedMaterial != ring.WorldMaterial
                    || e.Prefab.GetComponentsInChildren<MeshRenderer>(true).Length != 1
                    || e.Prefab.GetComponentsInChildren<Collider>(true).Length != 0
                    || e.Prefab.GetComponentsInChildren<MonoBehaviour>(true).Length != 0
                    || e.Prefab.GetComponentsInChildren<Light>(true).Length != 0
                    || e.Prefab.transform.localPosition != Vector3.zero
                    || e.Prefab.transform.localRotation != Quaternion.identity
                    || e.Prefab.transform.localScale != Vector3.one
                    || spec.kind != expectedKind || spec.materialFamily != "ring-palette"
                    || spec.rigFamily != "none" || spec.rigged
                    || spec.path != "Assets/Resources/DrownedLedgerVoxel3D/" + e.Id + ".prefab"
                    || spec.boundsCenter != e.Mesh.bounds.center || spec.boundsSize != e.Mesh.bounds.size
                    || spec.triangles != (int)e.Mesh.GetIndexCount(0) / 3
                    || spec.clips == null || spec.clips.Length != 0
                    || spec.sockets == null || spec.sockets.Length != 0)
                    throw new InvalidOperationException("DrownedLedger voxel mesh, prefab or metadata mismatch: " + e.Id);
                candidates.Add(e.Id, e);
            }
            foreach (string id in Ids)
                if (!candidates.ContainsKey(id))
                    throw new InvalidOperationException("Missing DrownedLedger voxel variant: " + id);
            index = candidates;
        }

        public Entry Find(string id)
        {
            if (id == null || !id.StartsWith("drownedledger-", StringComparison.Ordinal)) return null;
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
                    ids[n++] = "drownedledger-" + family + "-" + variant;
            return ids;
        }

        /// <summary>Returns a cached identifier for a known family and variant.
        /// Invalid authoring inputs throw instead of borrowing another family's art.</summary>
        public static string ModelId(string family, int variant)
        {
            int offset;
            switch (family)
            {
                case "preserved": offset = 0; break;
                case "stake": offset = 4; break;
                case "table": offset = 8; break;
                case "scribe": offset = 12; break;
                case "sorter": offset = 16; break;
                case "parcel": offset = 20; break;
                case "boards": offset = 24; break;
                default: throw new ArgumentException("Unknown DrownedLedger voxel family.", nameof(family));
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
                case "PreFellingBody": return "preserved";
                case "SurveyStake": return "stake";
                case "ReadingTable": return "table";
                case "RecensionScribe": return "scribe";
                case "CurationSorter": return "sorter";
                case "SealedBogTakenBody": return "parcel";
                case "Duckboard": return "boards";
                default: return null;
            }
        }
    }
}
