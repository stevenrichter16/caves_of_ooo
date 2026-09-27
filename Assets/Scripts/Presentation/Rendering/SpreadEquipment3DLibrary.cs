using System;
using System.Collections.Generic;
using UnityEngine;
namespace CavesOfOoo.Rendering
{
    /// <summary>Persistent fitted-wearable meshes share the scoped portable
    /// palette. One-bone bindposes let a single native pair follow two limbs.</summary>
    public sealed class SpreadEquipment3DLibrary : ScriptableObject
    {
        public const string ResourcePath = "SpreadEquipment3D/Library";
        public const string Folder = "Assets/Resources/SpreadEquipment3D";
        [Serializable] public sealed class Entry { public string Id, Slot; public Mesh Mesh; }
        public Entry[] Entries;
        public Material Material;
        public string SourceSha256;
        private Dictionary<string, Entry> index;
        private HashSet<Mesh> meshes;
        public static SpreadEquipment3DLibrary Load() => Resources.Load<SpreadEquipment3DLibrary>(ResourcePath);
        public Entry Find(string id)
        {
            if (id == null || !id.StartsWith("spread-worn-", StringComparison.Ordinal)) return null;
            if (index == null) Validate();
            return index.TryGetValue(id, out var entry) ? entry : null;
        }
        public bool ContainsMesh(Mesh mesh)
        {
            if (mesh == null) return false;
            if (index == null) Validate();
            return meshes.Contains(mesh);
        }
        public void Validate()
        {
            index = null; meshes = null;
            var portable = SpreadPortable3DLibrary.Load();
            if (portable == null || Material == null || Material != portable.Material
                || Entries == null || Entries.Length != SpreadEquipmentSource.Blueprints.Count
                || SourceSha256 != SpreadEquipmentSourceHash.Value)
                throw new InvalidOperationException("Incomplete or foreign fitted-equipment library.");
            var expected = new HashSet<string>(StringComparer.Ordinal);
            foreach (string name in SpreadEquipmentSource.Blueprints) expected.Add("spread-worn-" + name.ToLowerInvariant());
            var candidates = new Dictionary<string, Entry>(StringComparer.Ordinal); var meshSet = new HashSet<Mesh>();
            foreach (var entry in Entries)
            {
                if (entry == null || entry.Id == null || !expected.Remove(entry.Id) || entry.Mesh == null
                    || entry.Slot != ExpectedSlot(entry.Id)
                    || !meshSet.Add(entry.Mesh) || !entry.Mesh.isReadable || entry.Mesh.vertexCount < 24
                    || entry.Mesh.vertexCount > 6144 || entry.Mesh.subMeshCount != 1
                    || entry.Mesh.bindposeCount != 1 || entry.Mesh.bindposes[0] != Matrix4x4.identity)
                    throw new InvalidOperationException("Invalid fitted equipment entry.");
                var vertices = entry.Mesh.vertices; var uv = entry.Mesh.uv; var weights = entry.Mesh.boneWeights;
                if (uv.Length != vertices.Length || weights.Length != vertices.Length || entry.Mesh.normals.Length != vertices.Length)
                    throw new InvalidOperationException("Incomplete fitted equipment buffers.");
                for (int i = 0; i < vertices.Length; i++)
                {
                    var v = vertices[i]; var weight = weights[i];
                    if (!Finite(v.x) || !Finite(v.y) || !Finite(v.z) || Mathf.Abs(v.x) > 1 || Mathf.Abs(v.y) > 1 || Mathf.Abs(v.z) > 1
                        || !Finite(uv[i].x) || uv[i].x <= 0 || uv[i].x >= 1 || uv[i].y != .5f
                        || weight.boneIndex0 != 0 || weight.weight0 != 1 || weight.weight1 != 0 || weight.weight2 != 0 || weight.weight3 != 0)
                        throw new InvalidOperationException("Malformed fitted equipment vertex.");
                }
                candidates.Add(entry.Id, entry);
            }
            index = candidates; meshes = meshSet;
        }
        private static string ExpectedSlot(string id)
        {
            if (id == "spread-worn-leathercap" || id == "spread-worn-ironhelmet") return "Head";
            if (id == "spread-worn-leatherboots" || id == "spread-worn-ironshodboots") return "Feet";
            if (id == "spread-worn-leathergloves") return "Handwear";
            if (id == "spread-worn-cloak" || id == "spread-worn-wardedcloak") return "Back";
            return "Body";
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private void OnValidate() { index = null; meshes = null; }
    }
}
