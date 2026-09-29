#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace CavesOfOoo.Editor
{
    /// <summary>Explicit optional two-state residual-coal import. No borrowed asset, open
    /// scene, gameplay owner or unrelated output is rewritten.</summary>
    public static class SpreadCookingCoalsBuilder
    {
        const string Art="Assets/Art3D/SpreadCookingCoals",Folder="Assets/Resources/SpreadCookingCoals3D";
        [Serializable] sealed class Source{public int schemaVersion;public string id;public string[] palette;public Row[] models;}
        [Serializable] sealed class Envelope{public float[] min,max;}
        [Serializable] sealed class Row{public string id,sourceBlueprint,path,kind,sha256;public bool rigged;public string[] clips,sockets;public int triangles;public Envelope boundsBlender;}
        [Serializable] public sealed class Report{public string status,error,source,sourceHash;public string[] assets;public int models;}
        public static Report Build(string sourceDirectory,string reportPath)
        {
            var report=new Report{source=Path.GetFullPath(sourceDirectory)};var changed=new List<string>();
            try
            {
                if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Idle editor required for residual-coal import.");
                string catalog=Path.Combine(report.source,"catalog.json");report.sourceHash=Hash(catalog);
                var data=JsonUtility.FromJson<Source>(File.ReadAllText(catalog));var glade=ReferenceGladeVoxelLibrary.Load();
                if(glade==null)throw new InvalidOperationException("Existing approved palette required.");glade.Validate();
                var texture=glade.Material.GetTexture("_BaseMap")as Texture2D;
                if(report.sourceHash!=SpreadCookingCoalsLibrary.ReviewedCatalogSha256||data==null||data.schemaVersion!=1||data.models==null||data.models.Length!=2||data.palette==null||data.palette.Length!=24||texture==null||!texture.isReadable||texture.width!=24||texture.height!=1)
                    throw new InvalidOperationException("Exact reviewed original residual-coal source required.");
                for(int i=0;i<24;i++)if(!ColorUtility.TryParseHtmlString(data.palette[i],out var c)||Vector4.Distance(c,texture.GetPixel(i,0))>.00001f)
                    throw new InvalidOperationException("Residual-coal source differs from approved palette.");
                var ids=new HashSet<string>(StringComparer.Ordinal);
                foreach(var row in data.models)
                {
                    if(row==null||!SpreadCookingCoalsLibrary.IsModelId(row.id)||!ids.Add(row.id)||row.sourceBlueprint!="SpreadCookingCoals"||row.kind!="entity"
                        ||row.path!="models/"+row.id+".fbx"||row.rigged||row.clips==null||row.clips.Length!=0||row.sockets==null||row.sockets.Length!=0
                        ||row.triangles!=260)
                        throw new InvalidOperationException("Invalid original residual-coal source identity.");
                    string input=Path.Combine(report.source,row.path);if(!File.Exists(input)||Hash(input)!=row.sha256)throw new InvalidOperationException("Residual-coal FBX hash mismatch: "+row.id);
                    Preflight<GameObject>(Art+"/"+row.id+".fbx");Preflight<Mesh>(Folder+"/"+row.id+".asset");Preflight<GameObject>(Folder+"/"+row.id+".prefab");
                }
                Preflight<SpreadCookingCoalsLibrary>(Folder+"/Library.asset");
                EnsureFolder(Art);EnsureFolder(Folder);var entries=new List<SpreadCookingCoalsLibrary.Entry>();
                foreach(var row in data.models)
                {
                    string asset=Art+"/"+row.id+".fbx",meshPath=Folder+"/"+row.id+".asset",prefabPath=Folder+"/"+row.id+".prefab";
                    if(!File.Exists(asset)||Hash(asset)!=row.sha256){File.Copy(Path.Combine(report.source,row.path),asset,true);AssetDatabase.ImportAsset(asset,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);}
                    changed.Add(asset);Configure(asset);var model=AssetDatabase.LoadAssetAtPath<GameObject>(asset);
                    if(model==null||model.GetComponentsInChildren<Collider>(true).Length!=0||model.GetComponentsInChildren<Rigidbody>(true).Length!=0
                        ||model.GetComponentsInChildren<MonoBehaviour>(true).Length!=0||model.GetComponentsInChildren<Animator>(true).Length!=0||model.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length!=0)
                        throw new InvalidOperationException("Source must be inert static residual coals.");
                    var preview=EditorSceneManager.NewPreviewScene();GameObject root=null;Mesh prepared=null;
                    try
                    {
                        root=new GameObject(row.id);SceneManager.MoveGameObjectToScene(root,preview);
                        var instance=(GameObject)PrefabUtility.InstantiatePrefab(model,preview);instance.transform.SetParent(root.transform,false);
                        prepared=Flatten(root,instance,row.id);Object.DestroyImmediate(instance);
                        if(prepared.GetIndexCount(0)!=780||prepared.vertexCount<100||prepared.vertexCount>4096)throw new InvalidOperationException("Imported residual-coal differs from bounded source geometry.");
                        var mesh=SaveMesh(prepared,meshPath);prepared=null;changed.Add(meshPath);
                        root.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=root.AddComponent<MeshRenderer>();renderer.sharedMaterial=glade.Material;
                        renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;renderer.receiveShadows=true;
                        var spec=new SpawnRing3DCatalog.Model{id=row.id,path=prefabPath,sourceBlueprint="SpreadCookingCoals",kind="entity",rigFamily="",rigged=false,
                            materialFamily="reference-glade-palette",boundsCenter=mesh.bounds.center,boundsSize=mesh.bounds.size,triangles=260,clips=Array.Empty<string>(),sockets=Array.Empty<string>()};
                        var prefab=PrefabUtility.SaveAsPrefabAsset(root,prefabPath,out bool saved);if(!saved||prefab==null)throw new InvalidOperationException("Original residual-coal prefab save refused.");
                        entries.Add(new SpreadCookingCoalsLibrary.Entry{Id=row.id,Prefab=prefab,Mesh=mesh,Material=glade.Material,Spec=spec});changed.Add(prefabPath);
                    }
                    finally{if(prepared!=null)Object.DestroyImmediate(prepared);if(root!=null)Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(preview);}
                }
                // Publish the optional library only after its complete original pair validates.
                var candidate=ScriptableObject.CreateInstance<SpreadCookingCoalsLibrary>();
                try
                {
                    candidate.SourceSha256=report.sourceHash;candidate.Entries=entries.ToArray();candidate.Validate();
                    string path=Folder+"/Library.asset";var current=AssetDatabase.LoadAssetAtPath<SpreadCookingCoalsLibrary>(path);
                    if(current==null){AssetDatabase.CreateAsset(candidate,path);current=candidate;candidate=null;}
                    else EditorUtility.CopySerialized(candidate,current);
                    current.Validate();EditorUtility.SetDirty(current);AssetDatabase.SaveAssetIfDirty(current);changed.Add(path);
                }
                finally{if(candidate!=null)Object.DestroyImmediate(candidate);}
                report.models=entries.Count;report.status="success";
            }
            catch(Exception ex){report.status="failed";report.error=ex.ToString();}
            finally
            {
                report.assets=changed.Distinct().ToArray();string full=Path.GetFullPath(reportPath);Directory.CreateDirectory(Path.GetDirectoryName(full));
                File.WriteAllText(full,JsonUtility.ToJson(report,true));
            }
            return report;
        }
        static Mesh Flatten(GameObject root,GameObject source,string id)
        {
            var filters=source.GetComponentsInChildren<MeshFilter>(true);
            if(filters.Length!=11||source.GetComponentsInChildren<Renderer>(true).Length!=11)throw new InvalidOperationException("Exact eleven authored coal/ash/stone pieces required.");
            var names=new HashSet<string>(StringComparer.Ordinal);var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
            foreach(var filter in filters.OrderBy(f=>f.name,StringComparer.Ordinal))
            {
                // The frozen paired Blender export gives the second state's
                // objects the exact .001 suffix. Their ordinal piece order agrees.
                string suffix=id==SpreadCookingCoalsLibrary.Cooled?".001":"";
                string name=filter.name;
                if(suffix.Length!=0){if(!name.EndsWith(suffix,StringComparison.Ordinal))throw new InvalidOperationException("Wrong cooled source piece suffix.");name=name.Substring(0,name.Length-suffix.Length);}
                int expected=PieceTriangles(name);
                if(expected==0||!names.Add(name))throw new InvalidOperationException("Unknown or duplicate original coal piece.");
                var renderer=filter.GetComponent<MeshRenderer>();var mesh=filter.sharedMesh;
                if(renderer==null||!renderer.enabled||renderer.forceRenderingOff||renderer.sharedMaterials.Length!=1||mesh==null||!mesh.isReadable
                    ||mesh.subMeshCount!=1||mesh.GetTopology(0)!=MeshTopology.Triangles||mesh.GetIndexCount(0)!=expected*3
                    ||mesh.normals.Length!=mesh.vertexCount||mesh.uv.Length!=mesh.vertexCount||mesh.bindposeCount!=0||mesh.blendShapeCount!=0)
                    throw new InvalidOperationException("Invalid original residual-coal source piece.");
                for(var parent=filter.transform;parent!=null;parent=parent.parent){if(!parent.gameObject.activeSelf)throw new InvalidOperationException("Inactive original coal piece.");if(parent==source.transform)break;}
                var matrix=root.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;var normalMatrix=matrix.inverse.transpose;
                var xyz=mesh.vertices;var n=mesh.normals;var t=mesh.uv;int offset=vertices.Count;
                for(int i=0;i<xyz.Length;i++)
                {
                    var v=matrix.MultiplyPoint3x4(xyz[i]);var normal=normalMatrix.MultiplyVector(n[i]).normalized;
                    if(!Finite(v.x)||!Finite(v.y)||!Finite(v.z)||!Finite(normal.x)||!Finite(normal.y)||!Finite(normal.z)||!Finite(t[i].x)||!Finite(t[i].y))
                        throw new InvalidOperationException("Nonfinite imported residual-coal geometry.");
                    vertices.Add(v);normals.Add(normal);uv.Add(t[i]);
                }
                var indices=mesh.GetTriangles(0);
                for(int i=0;i<indices.Length;i+=3)
                {
                    triangles.Add(offset+indices[i]);triangles.Add(offset+indices[i+(matrix.determinant<0?2:1)]);triangles.Add(offset+indices[i+(matrix.determinant<0?1:2)]);
                }
            }
            var result=new Mesh{name=source.name+" (owned)"};result.SetVertices(vertices);result.SetNormals(normals);result.SetUVs(0,uv);result.SetTriangles(triangles,0);result.RecalculateBounds();return result;
        }
        static int PieceTriangles(string name)
        {
            if(name=="ash-bed")return 28;
            for(int i=0;i<7;i++)if(name=="charcoal-"+i)return 28;
            for(int i=0;i<3;i++)if(name=="edge-stone-"+i)return 12;
            return 0;
        }
        static Mesh SaveMesh(Mesh mesh,string path)
        {mesh.name=Path.GetFileNameWithoutExtension(path);var current=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(current==null){AssetDatabase.CreateAsset(mesh,path);current=mesh;}else{EditorUtility.CopySerialized(mesh,current);Object.DestroyImmediate(mesh);}EditorUtility.SetDirty(current);AssetDatabase.SaveAssetIfDirty(current);return current;}
        static void Configure(string path)
        {
            var importer=AssetImporter.GetAtPath(path)as ModelImporter;if(importer==null)throw new InvalidOperationException("Residual-coal model importer missing.");
            string before=EditorJsonUtility.ToJson(importer);importer.globalScale=1;importer.useFileScale=true;importer.bakeAxisConversion=false;importer.preserveHierarchy=true;
            importer.importCameras=false;importer.importLights=false;importer.addCollider=false;importer.importVisibility=false;importer.importNormals=ModelImporterNormals.Import;
            importer.importTangents=ModelImporterTangents.None;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.importAnimation=false;
            importer.animationType=ModelImporterAnimationType.None;importer.avatarSetup=ModelImporterAvatarSetup.NoAvatar;importer.isReadable=true;importer.optimizeGameObjects=false;
            if(before!=EditorJsonUtility.ToJson(importer))importer.SaveAndReimport();
        }
        static bool Finite(float n)=>!float.IsNaN(n)&&!float.IsInfinity(n);
        static void Preflight<T>(string path)where T:Object
        {if((File.Exists(path)||AssetDatabase.LoadMainAssetAtPath(path)!=null)&&AssetDatabase.LoadAssetAtPath<T>(path)==null)throw new InvalidOperationException("Refusing foreign output type: "+path);}
        static void EnsureFolder(string path)
        {if(AssetDatabase.IsValidFolder(path))return;string parent=Path.GetDirectoryName(path).Replace('\\','/');EnsureFolder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));}
        static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
    }
}
#endif
