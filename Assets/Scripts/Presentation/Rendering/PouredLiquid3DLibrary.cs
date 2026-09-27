using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Cached native pool swatches. Every prefab borrows the same shipped
    /// flat spring mesh; only exact registered liquid colors select a swatch.</summary>
    public sealed class PouredLiquid3DLibrary : ScriptableObject
    {
        public const string ResourcePath = "PouredLiquid3D/Library";
        public const string Folder = "Assets/Resources/PouredLiquid3D";
        public static IReadOnlyList<string> Colors { get; } = Array.AsReadOnly(new[]
            { "&B", "&C", "&G", "&K", "&R", "&W", "&Y", "&c", "&g", "&m", "&w", "&y" });
        [Serializable] public sealed class Entry
        {
            public string ColorCode;
            public GameObject Prefab;
            public Material Material;
            public SpawnRing3DCatalog.Model Spec;
        }
        public Entry[] Entries;
        private Dictionary<string, Entry> index;
        private Material[] materials;
        public static PouredLiquid3DLibrary Load() => Resources.Load<PouredLiquid3DLibrary>(ResourcePath);
        public IReadOnlyList<Material> Materials { get { if (index == null) Validate(); return materials; } }

        // Hex avoids case-only filenames on the normal macOS filesystem.
        public static string ModelId(string color)
        {
            for (int i = 0; i < Colors.Count; i++)
                if (Colors[i] == color) return "poured-liquid-" + ((int)color[1]).ToString("x2");
            return null;
        }
        public static string ResolveOwner(Entity owner)
        {
            if (owner == null || owner.BlueprintName != "PouredLiquidPool" || !LiquidRegistry.IsInitialized
                || owner.HasTag("Creature") || owner.HasTag("Item")) return null;
            var pool = owner.GetPart<LiquidPoolPart>(); var physics = owner.GetPart<PhysicsPart>();
            var render = owner.GetPart<RenderPart>();
            if (pool == null || !ReferenceEquals(pool.ParentEntity, owner) || pool.Volume <= 0
                || physics == null || !ReferenceEquals(physics.ParentEntity, owner) || physics.Takeable
                || physics.InInventory != null || physics.Equipped != null || render == null || !render.Visible
                || !ReferenceEquals(render.ParentEntity, owner) || !string.IsNullOrEmpty(render.VisualID)
                || !string.IsNullOrEmpty(render.VisualVariant) || !string.IsNullOrEmpty(render.GlyphVariants)) return null;
            var definition = LiquidRegistry.Get(pool.LiquidId);
            if (definition == null || definition.Id != pool.LiquidId || string.IsNullOrEmpty(definition.Glyph)
                || render.RenderString != definition.Glyph || render.ColorString != definition.Color) return null;
            return ModelId(definition.Color);
        }
        public void Validate()
        {
            index = null; materials = null;
            var ring = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
            var spring = StillleafVoxelLibrary.Load()?.Find(StillleafVoxelLibrary.ModelId("spring", 0));
            if (Entries == null || Entries.Length != Colors.Count || ring == null || ring.WorldMaterial == null
                || spring?.Mesh == null || !spring.Mesh.isReadable)
                throw new InvalidOperationException("Poured liquid art requires twelve swatches and the shipped flat pool.");
            var candidates = new Dictionary<string, Entry>(StringComparer.Ordinal);
            var palette = new Material[Colors.Count]; var materialSet = new HashSet<Material>();
            for (int i = 0; i < Entries.Length; i++)
            {
                var e = Entries[i]; string id = ModelId(e?.ColorCode);
                if (id == null || e.Prefab == null || e.Material == null || e.Spec == null || candidates.ContainsKey(id)
                    || !materialSet.Add(e.Material) || e.Material == ring.WorldMaterial || e.Material.shader != ring.WorldMaterial.shader
                    || !e.Material.HasProperty("_FogLight") || !e.Material.HasProperty("_Transient")
                    || e.Material.GetColor("_BaseColor") != QudColorParser.Parse(e.ColorCode)
                    || !(e.Material.GetTexture("_BaseMap") is Texture2D white) || !white.isReadable
                    || white.width != 1 || white.height != 1 || white.GetPixel(0, 0) != Color.white)
                    throw new InvalidOperationException("Invalid poured liquid color asset.");
                var filter = e.Prefab.GetComponent<MeshFilter>(); var renderer = e.Prefab.GetComponent<MeshRenderer>(); var s = e.Spec;
                if (filter == null || filter.sharedMesh != spring.Mesh || renderer == null || renderer.sharedMaterial != e.Material
                    || e.Prefab.GetComponentsInChildren<Renderer>(true).Length != 1 || renderer.sharedMaterials.Length != 1
                    || e.Prefab.GetComponentsInChildren<Collider>(true).Length != 0 || e.Prefab.GetComponentsInChildren<MonoBehaviour>(true).Length != 0
                    || e.Prefab.transform.localPosition != Vector3.zero || e.Prefab.transform.localRotation != Quaternion.identity
                    || e.Prefab.transform.localScale != Vector3.one || s.id != id || s.path != Folder + "/" + id + ".prefab"
                    || s.kind != "entity" || s.materialFamily != "poured-liquid" || s.rigFamily != "none" || s.rigged
                    || s.boundsCenter != spring.Mesh.bounds.center || s.boundsSize != spring.Mesh.bounds.size
                    || s.triangles != (int)spring.Mesh.GetIndexCount(0) / 3 || s.clips == null || s.clips.Length != 0
                    || s.sockets == null || s.sockets.Length != 0)
                    throw new InvalidOperationException("Poured liquid prefab or metadata mismatch: " + id);
                candidates.Add(id, e); palette[i] = e.Material;
            }
            foreach (var color in Colors)
                if (!candidates.ContainsKey(ModelId(color))) throw new InvalidOperationException("Missing poured liquid color: " + color);
            index = candidates; materials = palette;
        }
        public Entry Find(string id)
        {
            if (id == null || !id.StartsWith("poured-liquid-", StringComparison.Ordinal)) return null;
            if (index == null) Validate(); return index.TryGetValue(id, out var value) ? value : null;
        }
        private void OnValidate() { index = null; materials = null; }
    }
}
