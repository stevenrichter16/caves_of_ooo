using System;
using System.Collections.Generic;
using System.IO;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Creates only twelve owned color materials/prefabs, one white
    /// pixel and their library. The shipped pool mesh and palette stay borrowed.</summary>
    public static class PouredLiquid3DBuilder
    {
        public static void Run()
        {
            string folder = PouredLiquid3DLibrary.Folder;
            var ring = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
            var spring = StillleafVoxelLibrary.Load()?.Find(StillleafVoxelLibrary.ModelId("spring", 0));
            if (ring?.WorldMaterial == null || spring?.Mesh == null || !spring.Mesh.isReadable
                || spring.Mesh.vertexCount != 24 || spring.Mesh.bounds.max.y <= .035f || spring.Mesh.bounds.max.y >= .06f)
                throw new InvalidOperationException("Shipped native flat pool or palette is unavailable.");
            // Refuse wrong-type collisions for the complete bounded output set
            // before touching any persistent asset. No broad refresh/save/delete.
            Preflight<Texture2D>(folder + "/White.asset"); Preflight<PouredLiquid3DLibrary>(folder + "/Library.asset");
            foreach (var color in PouredLiquid3DLibrary.Colors)
            {
                string id = PouredLiquid3DLibrary.ModelId(color);
                Preflight<Material>(folder + "/" + id + ".mat"); Preflight<GameObject>(folder + "/" + id + ".prefab");
            }
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Resources", "PouredLiquid3D");
            var white = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/White.asset");
            if (white == null)
            {
                white = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "Poured liquid white", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
                white.SetPixel(0, 0, Color.white); white.Apply(); AssetDatabase.CreateAsset(white, folder + "/White.asset");
            }
            else if (!white.isReadable || white.width != 1 || white.height != 1 || white.GetPixel(0, 0) != Color.white)
                throw new InvalidOperationException("Existing poured liquid white source is malformed.");
            var entries = new List<PouredLiquid3DLibrary.Entry>();
            foreach (var color in PouredLiquid3DLibrary.Colors)
            {
                string id = PouredLiquid3DLibrary.ModelId(color), materialPath = folder + "/" + id + ".mat", prefabPath = folder + "/" + id + ".prefab";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null) { material = new Material(ring.WorldMaterial) { name = id }; AssetDatabase.CreateAsset(material, materialPath); }
                else { material.shader = ring.WorldMaterial.shader; material.CopyPropertiesFromMaterial(ring.WorldMaterial); }
                material.SetTexture("_BaseMap", white); material.SetColor("_BaseColor", QudColorParser.Parse(color)); material.SetFloat("_Transient", 0);
                if (material.HasProperty("_GroundMottleStrength")) material.SetFloat("_GroundMottleStrength", 0);
                EditorUtility.SetDirty(material); AssetDatabase.SaveAssetIfDirty(material);
                var root = new GameObject(id); GameObject prefab;
                try
                {
                    root.AddComponent<MeshFilter>().sharedMesh = spring.Mesh; root.AddComponent<MeshRenderer>().sharedMaterial = material;
                    prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
                if (prefab == null) throw new InvalidOperationException("Failed to save poured liquid prefab: " + id);
                entries.Add(new PouredLiquid3DLibrary.Entry { ColorCode = color, Prefab = prefab, Material = material,
                    Spec = new SpawnRing3DCatalog.Model { id = id, path = prefabPath, kind = "entity", materialFamily = "poured-liquid", rigFamily = "none",
                        boundsCenter = spring.Mesh.bounds.center, boundsSize = spring.Mesh.bounds.size, triangles = (int)spring.Mesh.GetIndexCount(0) / 3,
                        clips = Array.Empty<string>(), sockets = Array.Empty<string>() } });
            }
            var library = AssetDatabase.LoadAssetAtPath<PouredLiquid3DLibrary>(folder + "/Library.asset");
            if (library == null) { library = ScriptableObject.CreateInstance<PouredLiquid3DLibrary>(); AssetDatabase.CreateAsset(library, folder + "/Library.asset"); }
            library.Entries = entries.ToArray(); library.Validate(); EditorUtility.SetDirty(library); AssetDatabase.SaveAssetIfDirty(library);
            string path = "Docs/Verification/DensityCompletion/PouredLiquidArt/native-build.json";
            Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path, JsonUtility.ToJson(new Receipt {
                materials = entries.Count, prefabs = entries.Count, sharedMesh = AssetDatabase.GetAssetPath(spring.Mesh), sharedVertices = spring.Mesh.vertexCount }, true));
        }
        static void Preflight<T>(string path) where T : UnityEngine.Object
        {
            var existing = AssetDatabase.LoadMainAssetAtPath(path);
            if ((existing != null && !(existing is T)) || (existing == null && File.Exists(path)))
                throw new InvalidOperationException("Refusing foreign or unimported asset: " + path);
        }
        [Serializable] sealed class Receipt { public int materials, prefabs, sharedVertices; public string sharedMesh; }
    }
}
