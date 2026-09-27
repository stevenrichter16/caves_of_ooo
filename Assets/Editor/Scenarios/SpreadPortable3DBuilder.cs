#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
namespace CavesOfOoo.Editor
{
    /// <summary>Explicit, item-only adoption. All source buffers, source hashes
    /// and output collisions are checked before touching any owned asset.</summary>
    public static class SpreadPortable3DBuilder
    {
        private const string Source = "ArtSource/SpreadPortable3D/portable-source.json";
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Portable import requires clean Edit mode.");
            string json = File.ReadAllText(Source);
            if (Hash(File.ReadAllBytes(Source)) != SpreadPortableModelIds.SourceSha256)
                throw new InvalidOperationException("Portable source revision does not match its reviewed catalog.");
            var pack = JsonUtility.FromJson<SpreadPortableSource>(json); pack.Validate();
            var expected = new HashSet<string>(SpreadPortableModelIds.All, StringComparer.Ordinal);
            var colors = new Color[pack.palette.Length];
            for (int i = 0; i < colors.Length; i++)
                if (!ColorUtility.TryParseHtmlString(pack.palette[i], out colors[i]))
                    throw new InvalidOperationException("Invalid portable palette color.");
            foreach (var model in pack.models)
            {
                if (!expected.Remove(model.id)) throw new InvalidOperationException("Foreign portable model.");
                string sourcePath = model.borrowed ? model.source : "ArtSource/SpreadPortable3D/" + model.source;
                if (!File.Exists(sourcePath) || Hash(File.ReadAllBytes(sourcePath)) != model.sourceSha256)
                    throw new InvalidOperationException("Portable source file changed: " + sourcePath);
            }
            if (expected.Count != 0) throw new InvalidOperationException("Portable source pack is incomplete.");
            var ring = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
            if (ring?.WorldMaterial == null) throw new InvalidOperationException("Native palette material unavailable.");
            string folder = SpreadPortable3DLibrary.Folder;
            Preflight<SpreadPortable3DLibrary>(folder + "/Library.asset");
            Preflight<Texture2D>(folder + "/Palette.asset"); Preflight<Material>(folder + "/Palette.mat");
            foreach (var model in pack.models)
            {
                Preflight<Mesh>(folder + "/" + model.id + ".asset");
                Preflight<GameObject>(folder + "/" + model.id + ".prefab");
            }
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Resources", "SpreadPortable3D");
            var palette = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/Palette.asset");
            if (palette == null)
            {
                palette = new Texture2D(colors.Length, 1, TextureFormat.RGBA32, false) { name = "Spread portable palette" };
                AssetDatabase.CreateAsset(palette, folder + "/Palette.asset");
            }
            else if (!palette.isReadable || palette.width != colors.Length || palette.height != 1)
                throw new InvalidOperationException("Owned portable palette has incompatible dimensions.");
            palette.filterMode = FilterMode.Point; palette.wrapMode = TextureWrapMode.Clamp;
            palette.SetPixels(colors); palette.Apply(false, false); EditorUtility.SetDirty(palette); AssetDatabase.SaveAssetIfDirty(palette);
            var material = AssetDatabase.LoadAssetAtPath<Material>(folder + "/Palette.mat");
            if (material == null)
            {
                material = new Material(ring.WorldMaterial) { name = "Spread portable palette" };
                AssetDatabase.CreateAsset(material, folder + "/Palette.mat");
            }
            else { material.shader = ring.WorldMaterial.shader; material.CopyPropertiesFromMaterial(ring.WorldMaterial); }
            material.SetTexture("_BaseMap", palette); material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Transient", 0); material.enableInstancing = true;
            if (material.HasProperty("_GroundMottleStrength")) material.SetFloat("_GroundMottleStrength", 0);
            EditorUtility.SetDirty(material); AssetDatabase.SaveAssetIfDirty(material);
            var entries = new List<SpreadPortable3DLibrary.Entry>(pack.models.Length);
            foreach (var model in pack.models)
            {
                string meshPath = folder + "/" + model.id + ".asset", prefabPath = folder + "/" + model.id + ".prefab";
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (mesh == null) { mesh = new Mesh { name = model.id }; AssetDatabase.CreateAsset(mesh, meshPath); }
                // Write complete buffers directly to this owned persistent mesh.
                // CopySerialized from a temporary mesh leaves stale native data.
                var xyz = new Vector3[model.paletteIndices.Length]; var uv = new Vector2[xyz.Length];
                for (int i = 0; i < xyz.Length; i++)
                {
                    xyz[i] = new Vector3(model.positions[i*3], model.positions[i*3+1], model.positions[i*3+2]);
                    uv[i] = new Vector2((model.paletteIndices[i]+.5f)/colors.Length, .5f);
                }
                mesh.Clear(); mesh.indexFormat = IndexFormat.UInt16;
                mesh.vertices = xyz; mesh.uv = uv; mesh.triangles = model.triangles;
                mesh.RecalculateNormals(); mesh.RecalculateBounds();
                EditorUtility.SetDirty(mesh); AssetDatabase.SaveAssetIfDirty(mesh);
                GameObject prefab; var root = new GameObject(model.id);
                try
                {
                    root.AddComponent<MeshFilter>().sharedMesh = mesh;
                    root.AddComponent<MeshRenderer>().sharedMaterial = material;
                    prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
                if (prefab == null) throw new InvalidOperationException("Portable prefab save failed: " + model.id);
                entries.Add(new SpreadPortable3DLibrary.Entry { Id=model.id, Mesh=mesh, Prefab=prefab,
                    Spec=new SpawnRing3DCatalog.Model { id=model.id, path=prefabPath, kind="entity", materialFamily="spread-portable",
                        sourceBlueprint=model.blueprint, rigFamily="none", boundsCenter=mesh.bounds.center, boundsSize=mesh.bounds.size,
                        triangles=model.triangles.Length/3, clips=Array.Empty<string>(), sockets=Array.Empty<string>() } });
            }
            var library = AssetDatabase.LoadAssetAtPath<SpreadPortable3DLibrary>(folder + "/Library.asset");
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<SpreadPortable3DLibrary>();
                AssetDatabase.CreateAsset(library, folder + "/Library.asset");
            }
            library.SourceSha256=SpreadPortableModelIds.SourceSha256; library.Palette=palette; library.Material=material;
            library.Entries=entries.ToArray(); library.Validate(); EditorUtility.SetDirty(library); AssetDatabase.SaveAssetIfDirty(library);
            const string report = "Docs/Verification/DensityCompletion/SpreadBiome/Portables/native-import.json";
            Directory.CreateDirectory(Path.GetDirectoryName(report));
            File.WriteAllText(report, JsonUtility.ToJson(new Receipt { sourceSha256=library.SourceSha256, models=entries.Count,
                borrowedSources=pack.models.Count(m=>m.borrowed), vertices=entries.Sum(e=>e.Mesh.vertexCount),
                triangles=entries.Sum(e=>e.Spec.triangles), paletteCells=colors.Length }, true));
        }
        private static string Hash(byte[] bytes)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
        private static void Preflight<T>(string path) where T : UnityEngine.Object
        {
            var asset=AssetDatabase.LoadMainAssetAtPath(path);
            if ((asset!=null && !(asset is T)) || (asset==null && File.Exists(path)))
                throw new InvalidOperationException("Refusing foreign/unimported portable output: " + path);
        }
        [Serializable] private sealed class Receipt
        { public string sourceSha256; public int models,borrowedSources,vertices,triangles,paletteCells; }
    }
}
#endif
