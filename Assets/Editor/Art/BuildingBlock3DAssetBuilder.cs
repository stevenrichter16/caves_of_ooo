#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;
namespace CavesOfOoo.Editor
{
    /// <summary>Explicit asset-only import. Does not edit user scenes or add simulation collision.</summary>
    public static class BuildingBlock3DAssetBuilder
    {
        const string Root="Assets/Art3D/BuildingBlocks";
        [Serializable] public class Catalog {public int schemaVersion;public Model[] models;public string[] validationErrors;}
        [Serializable] public class Model {public string id,family,path,pivot,material;public int variant,triangles,zeroAreaTriangles,cellSize;}
        [Serializable] public class Assembly {public Block[] blocks;}
        [Serializable] public class Block {public string owner,model;public int[] cell;public float height;public int quarterTurns;}
        [Serializable] public class Receipt {public string status,error;public int models,assemblyBlocks;public string[] guids;}
        [MenuItem("Caves of Ooo/3D Art/Import Building Blocks")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Import requires edit mode.");
            string source=Path.GetFullPath("ArtSource/BuildingBlocks3D/build");
            var c=JsonUtility.FromJson<Catalog>(File.ReadAllText(source+"/catalog.json"));
            var assembly=JsonUtility.FromJson<Assembly>(File.ReadAllText(source+"/assembly.json"));
            if(c==null||c.schemaVersion!=1||c.models==null||c.models.Length!=72||c.validationErrors==null||c.validationErrors.Length!=0)
                throw new InvalidOperationException("A complete validated72-piece source kit is required.");
            var ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(var m in c.models)
            {
                if(m==null||string.IsNullOrEmpty(m.id)||m.id.Any(ch=>!char.IsLetterOrDigit(ch)&&ch!='-')||!ids.Add(m.id)
                    ||m.path!="models/"+m.id+".fbx"||m.pivot!="bottom-centre"||m.cellSize!=1||m.triangles<=0||m.zeroAreaTriangles!=0
                    ||m.variant<0||m.variant>3||!File.Exists(source+"/"+m.path))throw new InvalidOperationException("Invalid block source contract.");
            }
            foreach(var family in c.models.GroupBy(m=>m.family))
                if(!family.Select(m=>m.variant).OrderBy(i=>i).SequenceEqual(new[]{0,1,2,3}))throw new InvalidOperationException("Incomplete family: "+family.Key);
            if(assembly?.blocks==null||assembly.blocks.Length<50)throw new InvalidOperationException("Missing cell assembly.");
            var owners=new HashSet<string>(StringComparer.Ordinal);
            foreach(var block in assembly.blocks)
                if(block==null||!ids.Contains(block.model)||string.IsNullOrEmpty(block.owner)||!owners.Add(block.owner)||block.cell==null||block.cell.Length!=2
                    ||float.IsNaN(block.height)||float.IsInfinity(block.height)||block.height<0||block.quarterTurns<0||block.quarterTurns>3)
                    throw new InvalidOperationException("Invalid independent block placement.");
            foreach(string dir in new[]{"Models","Textures","Materials","Prefabs","Assemblies"})Directory.CreateDirectory(Root+"/"+dir);
            AssetDatabase.Refresh();Copy(source+"/textures/BuildingPalette.png",Root+"/Textures/BuildingPalette.png");
            var textureImporter=(TextureImporter)AssetImporter.GetAtPath(Root+"/Textures/BuildingPalette.png");
            textureImporter.textureType=TextureImporterType.Default;textureImporter.sRGBTexture=true;textureImporter.mipmapEnabled=false;textureImporter.wrapMode=TextureWrapMode.Clamp;
            textureImporter.textureCompression=TextureImporterCompression.Uncompressed;textureImporter.alphaSource=TextureImporterAlphaSource.None;textureImporter.SaveAndReimport();
            // Ordinary authoring material. Native integration must assign a presenter-owned
            // fog material; do not install a global reveal mask merely to preview a prefab.
            string materialPath=Root+"/Materials/BuildingPalette-Authoring.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null){var shader=Shader.Find("Universal Render Pipeline/Lit");if(shader==null)throw new InvalidOperationException("URP Lit missing.");material=new Material(shader);AssetDatabase.CreateAsset(material,materialPath);}
            material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/BuildingPalette.png"));material.SetColor("_BaseColor",Color.white);material.SetFloat("_Smoothness",.12f);material.enableInstancing=true;EditorUtility.SetDirty(material);
            var prefabs=new Dictionary<string,GameObject>();var guids=new List<string>();
            foreach(var m in c.models)
            {
                string path=Root+"/Models/"+m.id+".fbx";Copy(source+"/"+m.path,path);
                var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.globalScale=1;importer.useFileScale=true;importer.bakeAxisConversion=false;importer.preserveHierarchy=true;
                importer.importNormals=ModelImporterNormals.Import;importer.importTangents=ModelImporterTangents.None;importer.isReadable=true;importer.importAnimation=false;
                importer.animationType=ModelImporterAnimationType.None;importer.addCollider=false;importer.importCameras=false;importer.importLights=false;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();
                var preview=EditorSceneManager.NewPreviewScene();GameObject root=null;
                try
                {
                    root=new GameObject(m.id);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,preview);
                    var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),preview);model.transform.SetParent(root.transform,false);
                    foreach(var r in root.GetComponentsInChildren<MeshRenderer>(true))r.sharedMaterials=Enumerable.Repeat(material,r.sharedMaterials.Length).ToArray();
                    var meshes=root.GetComponentsInChildren<MeshFilter>(true);
                    if(meshes.Sum(f=>f.sharedMesh.triangles.Length/3)!=m.triangles)throw new InvalidOperationException("Imported topology drift: "+m.id);
                    foreach(var f in meshes)foreach(var vertex in f.sharedMesh.vertices)
                    {
                        var v=root.transform.InverseTransformPoint(f.transform.TransformPoint(vertex));
                        if(Mathf.Abs(v.x)>.5001f||Mathf.Abs(v.z)>.5001f||v.y<-.0001f)throw new InvalidOperationException("Imported cell bounds drift: "+m.id);
                    }
                    string prefabPath=Root+"/Prefabs/"+m.id+".prefab";var prefab=PrefabUtility.SaveAsPrefabAsset(root,prefabPath,out bool success);
                    if(!success||prefab==null)throw new InvalidOperationException("Prefab save failed: "+m.id);prefabs.Add(m.id,prefab);guids.Add(AssetDatabase.AssetPathToGUID(prefabPath));
                }
                finally{if(root!=null)Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(preview);}
            }
            var scene=EditorSceneManager.NewPreviewScene();GameObject house=null;
            try
            {
                house=new GameObject("BlockHouse_IndependentCells");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(house,scene);
                foreach(var b in assembly.blocks)
                {
                    var child=(GameObject)PrefabUtility.InstantiatePrefab(prefabs[b.model],scene);child.name=b.owner+"__"+b.model;child.transform.SetParent(house.transform,false);
                    child.transform.localPosition=new Vector3(b.cell[0],b.height,b.cell[1]);child.transform.localRotation=Quaternion.Euler(0,-90*b.quarterTurns,0);
                }
                PrefabUtility.SaveAsPrefabAsset(house,Root+"/Assemblies/BlockHouse.prefab",out bool success);if(!success)throw new InvalidOperationException("Assembly prefab save failed.");
            }
            finally{if(house!=null)Object.DestroyImmediate(house);EditorSceneManager.ClosePreviewScene(scene);}
            Copy(source+"/catalog.json",Root+"/catalog.json");Copy(source+"/assembly.json",Root+"/assembly.json");AssetDatabase.SaveAssets();
            Directory.CreateDirectory("Docs/Verification/BuildingBlocks3D");File.WriteAllText("Docs/Verification/BuildingBlocks3D/import.json",JsonUtility.ToJson(new Receipt{status="PASS",models=prefabs.Count,assemblyBlocks=assembly.blocks.Length,guids=guids.ToArray()},true));
        }
        static void Copy(string from,string to){if(!File.Exists(to)||!File.ReadAllBytes(from).SequenceEqual(File.ReadAllBytes(to)))File.Copy(from,to,true);AssetDatabase.ImportAsset(to,ImportAssetOptions.ForceSynchronousImport);}
    }
}
#endif
