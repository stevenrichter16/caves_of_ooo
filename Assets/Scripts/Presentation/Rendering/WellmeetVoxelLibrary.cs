using System;
using System.Collections.Generic;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Already-voxel Wellmeet scenery. Entries describe art only: native
    /// entity membership owns visibility, interaction state, occupancy and damage.</summary>
    public sealed class WellmeetVoxelLibrary : ScriptableObject
    {
        public const string ResourcePath = "WellmeetVoxel3D/Library";
        public const int VariantCount = 4;
        private static readonly string[] Families = { "ground", "floor", "tent", "cloth", "well", "host", "salt", "bed", "chair", "oven", "shrine", "lantern", "rack", "adult", "child", "corner", "path", "shelf", "marker" };
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
        public static WellmeetVoxelLibrary Load() => Resources.Load<WellmeetVoxelLibrary>(ResourcePath);

        /// <summary>Validates the complete build-visible kit and atomically
        /// publishes its lookup. Broken assets fail loudly before presentation.</summary>
        public void Validate()
        {
            index = null;
            if (Entries == null || Entries.Length != Ids.Length)
                throw new InvalidOperationException("Wellmeet voxel kit requires four variants of 19 families.");
            var ring = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
            if (ring == null || ring.WorldMaterial == null)
                throw new InvalidOperationException("Wellmeet voxel kit requires the native ring palette material.");
            var candidates = new Dictionary<string, Entry>(StringComparer.Ordinal);
            foreach (var e in Entries)
            {
                if (e == null || string.IsNullOrEmpty(e.Id) || e.Prefab == null || e.Mesh == null
                    || !e.Mesh.isReadable || e.Mesh.vertexCount == 0 || e.Spec == null
                    || e.Id != e.Spec.id || candidates.ContainsKey(e.Id))
                    throw new InvalidOperationException("Invalid or duplicate Wellmeet voxel entry.");
                var filter = e.Prefab.GetComponent<MeshFilter>();
                var renderer = e.Prefab.GetComponent<MeshRenderer>();
                var spec = e.Spec;
                bool isGround = e.Id.StartsWith("wellmeet-ground-", StringComparison.Ordinal)
                    || e.Id.StartsWith("wellmeet-floor-", StringComparison.Ordinal)
                    || e.Id.StartsWith("wellmeet-path-", StringComparison.Ordinal);
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
                    || spec.path != "Assets/Resources/WellmeetVoxel3D/" + e.Id + ".prefab"
                    || spec.boundsCenter != e.Mesh.bounds.center || spec.boundsSize != e.Mesh.bounds.size
                    || spec.triangles != (int)e.Mesh.GetIndexCount(0) / 3
                    || spec.clips == null || spec.clips.Length != 0
                    || spec.sockets == null || spec.sockets.Length != 0)
                    throw new InvalidOperationException("Wellmeet voxel mesh, prefab or metadata mismatch: " + e.Id);
                candidates.Add(e.Id, e);
            }
            foreach (string id in Ids)
                if (!candidates.ContainsKey(id))
                    throw new InvalidOperationException("Missing Wellmeet voxel variant: " + id);
            index = candidates;
        }

        public Entry Find(string id)
        {
            if (id == null || !id.StartsWith("wellmeet-", StringComparison.Ordinal)) return null;
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
                    ids[n++] = "wellmeet-" + family + "-" + variant;
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
                case "tent": offset = 8; break;
                case "cloth": offset = 12; break;
                case "well": offset = 16; break;
                case "host": offset = 20; break;
                case "salt": offset = 24; break;
                case "bed": offset = 28; break;
                case "chair": offset = 32; break;
                case "oven": offset = 36; break;
                case "shrine": offset = 40; break;
                case "lantern": offset = 44; break;
                case "rack": offset = 48; break;
                case "adult": offset = 52; break;
                case "child": offset = 56; break;
                case "corner": offset = 60; break;
                case "path": offset = 64; break;
                case "shelf": offset = 68; break;
                case "marker": offset = 72; break;
                default: throw new ArgumentException("Unknown Wellmeet voxel family.", nameof(family));
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
                case "WellGroundMarker": return "marker";
                case "OvenGroundMarker": return "marker";
                case "LanternGroundMarker": return "marker";
                case "CampfireGroundMarker": return "marker";
                case "Farmer": return "adult";
                case "WellKeeper": return "adult";
                case "RoadStone": return "path";
                case "AlchemyShelf": return "shelf";
                case "Sand": return "ground";
                case "StoneFloor": return "floor";
                case "TentWall": return "tent";
                case "GuestClothPole": return "cloth";
                case "Well": return "well";
                case "TentRightHost": return "host";
                case "SaltMaster": return "salt";
                case "Bed": return "bed";
                case "Chair": return "chair";
                case "Oven": return "oven";
                case "Shrine": return "shrine";
                case "WatchLantern": return "lantern";
                case "WeaponRack": return "rack";
                case "Elder": return "adult";
                case "Innkeeper": return "adult";
                case "Merchant": return "adult";
                case "Quartermaster": return "adult";
                case "Scribe": return "adult";
                case "Tinker": return "adult";
                case "Villager": return "adult";
                case "Warden": return "adult";
                case "VillageChild": return "child";
                default: return null;
            }
        }
    }
}
