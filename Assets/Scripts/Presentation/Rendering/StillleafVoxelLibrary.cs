using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Already-voxel Stillleaf scenery. Entries describe art only: native
    /// entity membership owns visibility, interaction state, occupancy and damage.</summary>
    public sealed class StillleafVoxelLibrary : ScriptableObject
    {
        public const string ResourcePath = "StillleafVoxel3D/Library";
        public const int VariantCount = 4;
        private static readonly string[] Families = { "ground", "floor", "tepuibone", "marble", "iron", "door", "open-door", "shelf", "bear", "slime", "spring", "boots", "wall" };
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
        public static StillleafVoxelLibrary Load() => Resources.Load<StillleafVoxelLibrary>(ResourcePath);

        /// <summary>Validates the complete build-visible kit and atomically
        /// publishes its lookup. Broken assets fail loudly before presentation.</summary>
        public void Validate()
        {
            index = null;
            if (Entries == null || Entries.Length != Ids.Length)
                throw new InvalidOperationException("Stillleaf voxel kit requires four variants of 13 families.");
            var ring = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
            if (ring == null || ring.WorldMaterial == null)
                throw new InvalidOperationException("Stillleaf voxel kit requires the native ring palette material.");
            var candidates = new Dictionary<string, Entry>(StringComparer.Ordinal);
            foreach (var e in Entries)
            {
                if (e == null || string.IsNullOrEmpty(e.Id) || e.Prefab == null || e.Mesh == null
                    || !e.Mesh.isReadable || e.Mesh.vertexCount == 0 || e.Spec == null
                    || e.Id != e.Spec.id || candidates.ContainsKey(e.Id))
                    throw new InvalidOperationException("Invalid or duplicate Stillleaf voxel entry.");
                var filter = e.Prefab.GetComponent<MeshFilter>();
                var renderer = e.Prefab.GetComponent<MeshRenderer>();
                var spec = e.Spec;
                bool isGround = e.Id.StartsWith("stillleaf-ground-", StringComparison.Ordinal)
                    || e.Id.StartsWith("stillleaf-floor-", StringComparison.Ordinal);
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
                    || spec.path != "Assets/Resources/StillleafVoxel3D/" + e.Id + ".prefab"
                    || spec.boundsCenter != e.Mesh.bounds.center || spec.boundsSize != e.Mesh.bounds.size
                    || spec.triangles != (int)e.Mesh.GetIndexCount(0) / 3
                    || spec.clips == null || spec.clips.Length != 0
                    || spec.sockets == null || spec.sockets.Length != 0)
                    throw new InvalidOperationException("Stillleaf voxel mesh, prefab or metadata mismatch: " + e.Id);
                candidates.Add(e.Id, e);
            }
            foreach (string id in Ids)
                if (!candidates.ContainsKey(id))
                    throw new InvalidOperationException("Missing Stillleaf voxel variant: " + id);
            index = candidates;
        }

        public Entry Find(string id)
        {
            if (id == null || !id.StartsWith("stillleaf-", StringComparison.Ordinal)) return null;
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
                    ids[n++] = "stillleaf-" + family + "-" + variant;
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
                case "floor": offset = 4; break;
                case "tepuibone": offset = 8; break;
                case "marble": offset = 12; break;
                case "iron": offset = 16; break;
                case "door": offset = 20; break;
                case "open-door": offset = 24; break;
                case "shelf": offset = 28; break;
                case "bear": offset = 32; break;
                case "slime": offset = 36; break;
                case "spring": offset = 40; break;
                case "boots": offset = 44; break;
                case "wall": offset = 48; break;
                default: throw new ArgumentException("Unknown Stillleaf voxel family.", nameof(family));
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
                case "SandstoneFloor": return "ground";
                case "StoneFloor": return "ground";
                case "SealedLibraryFloor": return "floor";
                case "LibraryTepuiboneWall": return "tepuibone";
                case "LibraryMemoryMarbleWall": return "marble";
                case "LibraryChoirIronWall": return "iron";
                case "SealedLibraryDoor": return "door";
                case "SealedArchiveShelf": return "shelf";
                case "CaveBear": return "bear";
                case "CaveSlime": return "slime";
                case "ConvalescencePool": return "spring";
                case "IronshodBoots": return "boots";
                case "TepuiWall": return "wall";
                default: return null;
            }
        }
    }
}
