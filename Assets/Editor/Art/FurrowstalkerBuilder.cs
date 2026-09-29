#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;
namespace CavesOfOoo.Editor
{
    /// <summary>Explicit two-form hunter import. Writes only this pack; borrowed palettes, source files, scenes and original rigs remain untouched.</summary>
    public static class FurrowstalkerBuilder
    {
        const string Folder="Assets/Resources/Furrowstalker3D",Art="Assets/Art3D/Furrowstalker";
        static readonly string[] ClipNames=FurrowstalkerLibrary.ClipNames;
        [Serializable]sealed class Source{public int schemaVersion;public string id;public string[] palette;public Row[] models;}
        [Serializable]sealed class Envelope{public float[] min,max;}
        [Serializable]sealed class Row{public string id,sourceBlueprint,path,rigFamily,sha256;public bool rigged;public string[] clips,sockets,bones;public int triangles;public Envelope boundsBlender;}
        [Serializable]public sealed class Report{public string status,error,source,sourceHash;public string[] assets;public int models;}
        public static Report Build(string sourceDirectory,string reportPath)
        {
            var report=new Report{source=Path.GetFullPath(sourceDirectory)};var changed=new List<string>();
            try
            {
                if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Idle editor required for original exploration art import.");
                string catalog=Path.Combine(report.source,"catalog.json");var data=JsonUtility.FromJson<Source>(File.ReadAllText(catalog));report.sourceHash=Hash(catalog);
                if(report.sourceHash!="5061e17deddea74f77d8f4797bc57df7073fe86172af5912dcfaeaed3e081154")throw new InvalidOperationException("Reviewed original hunter catalog hash differs.");
                var glade=ReferenceGladeVoxelLibrary.Load();if(glade==null)throw new InvalidOperationException("Approved palette required.");glade.Validate();
                var texture=glade.Material.GetTexture("_BaseMap")as Texture2D;
                if(data?.models==null||data.models.Length!=2||data.schemaVersion!=1||data.id!="spread-furrowstalker-original"||data.palette?.Length!=24
                    ||texture==null||texture.width!=24||texture.height!=1||!texture.isReadable)
                    throw new InvalidOperationException("Exact two-source hunter contract and approved palette required.");
                for(int i=0;i<24;i++)if(!ColorUtility.TryParseHtmlString(data.palette[i],out var c)||Vector4.Distance(c,texture.GetPixel(i,0))>.00001f)
                    throw new InvalidOperationException("Original form palette differs from approved swatches.");
                var ids=new HashSet<string>(StringComparer.Ordinal);
                foreach(var row in data.models)
                {
                    bool rigged=row!=null&&row.id==FurrowstalkerLibrary.Hunter;
                    if(row==null||FurrowstalkerLibrary.Blueprint(row.id)==null||row.sourceBlueprint!=FurrowstalkerLibrary.Blueprint(row.id)||!ids.Add(row.id)
                        ||row.path!="models/"+row.id+".fbx"||row.rigged!=rigged||row.rigFamily!=(rigged?"quadruped":"")
                        ||row.triangles!=(rigged?480:192)||row.clips==null||!row.clips.SequenceEqual(rigged?ClipNames:Array.Empty<string>())
                        ||row.sockets==null||row.sockets.Length!=0||row.bones==null||!row.bones.SequenceEqual(rigged?FurrowstalkerLibrary.BoneNames:Array.Empty<string>())
                        ||row.boundsBlender?.min?.Length!=3||row.boundsBlender.max?.Length!=3)
                        throw new InvalidOperationException("Invalid original source identity/anatomy.");
                    for(int i=0;i<3;i++)if(!Finite(row.boundsBlender.min[i])||!Finite(row.boundsBlender.max[i])||row.boundsBlender.max[i]<=row.boundsBlender.min[i]
                        ||Math.Abs(row.boundsBlender.min[i])>2||Math.Abs(row.boundsBlender.max[i])>2)throw new InvalidOperationException("Invalid original source envelope.");
                    string input=Path.Combine(report.source,row.path);if(row.sha256!=(rigged?"b4fc0f6e80fb0015b5b015319d37480e992f693ebab2024d904bc96855339481":"d280a7fe6997c041a3a04d796aa6e62205fa68ed79106fb542929d1e4a1e9dc7")||!File.Exists(input)||Hash(input)!=row.sha256)throw new InvalidOperationException("Source FBX hash mismatch: "+row.id);
                    Preflight<GameObject>(Art+"/"+row.id+".fbx");Preflight<GameObject>(Folder+"/"+row.id+".prefab");Preflight<Mesh>(Folder+"/"+row.id+".asset");
                    if(rigged)Preflight<AnimatorController>(Art+"/"+row.id+".controller");
                }
                Preflight<FurrowstalkerLibrary>(Folder+"/Library.asset");
                EnsureFolder(Art);EnsureFolder(Folder);var entries=new List<FurrowstalkerLibrary.Entry>();
                foreach(var row in data.models)
                {
                    string asset=Art+"/"+row.id+".fbx",prefabPath=Folder+"/"+row.id+".prefab";
                    if(!File.Exists(asset)||Hash(asset)!=row.sha256){File.Copy(Path.Combine(report.source,row.path),asset,true);AssetDatabase.ImportAsset(asset,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);changed.Add(asset);}
                    Configure(asset,row.rigged);var model=AssetDatabase.LoadAssetAtPath<GameObject>(asset);if(model==null)throw new InvalidOperationException("Original FBX import failed.");
                    if(model.GetComponentsInChildren<Collider>(true).Length!=0||model.GetComponentsInChildren<Rigidbody>(true).Length!=0||model.GetComponentsInChildren<MonoBehaviour>(true).Length!=0)
                        throw new InvalidOperationException("Original art may not introduce simulation components.");
                    var preview=EditorSceneManager.NewPreviewScene();GameObject root=null;Mesh prepared=null;
                    try
                    {
                        root=new GameObject(row.id);SceneManager.MoveGameObjectToScene(root,preview);
                        var instance=(GameObject)PrefabUtility.InstantiatePrefab(model,preview);instance.transform.SetParent(root.transform,false);
                        Renderer renderer;Material[] materials=new[]{glade.Material};
                        if(row.rigged)
                        {
                            var skins=instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);var animators=instance.GetComponentsInChildren<Animator>(true);
                            if(skins.Length!=1||animators.Length!=1||animators[0].avatar==null||!animators[0].avatar.isValid||skins[0].sharedMesh==null||!skins[0].sharedMesh.isReadable)
                                throw new InvalidOperationException("One original native hunter rig required.");
                            var clips=AssetDatabase.LoadAllAssetsAtPath(asset).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__",StringComparison.Ordinal)).ToArray();
                            foreach(string n in ClipNames)if(clips.Count(c=>c.name==n&&c.length>0)!=1)throw new InvalidOperationException("Missing original clip: "+n);
                            string controller=Art+"/"+row.id+".controller";animators[0].runtimeAnimatorController=Controller(controller,clips);changed.Add(controller);
                            animators[0].applyRootMotion=false;animators[0].cullingMode=AnimatorCullingMode.CullUpdateTransforms;
                            prepared=Object.Instantiate(skins[0].sharedMesh);prepared.RecalculateBounds();renderer=skins[0];
                            // Retain imported animation/culling envelope on the renderer; mesh bound is the visible bind geometry.
                        }
                        else
                        {
                            if(instance.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length!=0||instance.GetComponentsInChildren<Animator>(true).Length!=0)
                                throw new InvalidOperationException("Static form unexpectedly contains a rig.");
                            prepared=Flatten(root,instance);
                            Object.DestroyImmediate(instance);root.AddComponent<MeshFilter>();renderer=root.AddComponent<MeshRenderer>();
                        }
                        int triangles=0;for(int i=0;i<prepared.subMeshCount;i++)triangles+=(int)prepared.GetIndexCount(i)/3;
                        if(triangles!=row.triangles||prepared.vertexCount<100||prepared.vertexCount>65535)throw new InvalidOperationException("Imported geometry differs from bounded source.");
                        var mesh=SaveMesh(prepared,Folder+"/"+row.id+".asset");prepared=null;changed.Add(Folder+"/"+row.id+".asset");
                        if(renderer is SkinnedMeshRenderer skin)skin.sharedMesh=mesh;else renderer.GetComponent<MeshFilter>().sharedMesh=mesh;
                        renderer.sharedMaterials=materials;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;renderer.receiveShadows=true;
                        var spec=new SpawnRing3DCatalog.Model{id=row.id,path=prefabPath,sourceBlueprint=row.sourceBlueprint,kind=row.rigged?"actor":"entity",rigFamily=row.rigFamily,rigged=row.rigged,
                            materialFamily="reference-glade-palette",boundsCenter=renderer.bounds.center,boundsSize=renderer.bounds.size,triangles=triangles,clips=row.rigged?(string[])ClipNames.Clone():Array.Empty<string>(),sockets=Array.Empty<string>()};
                        var prefab=PrefabUtility.SaveAsPrefabAsset(root,prefabPath,out bool saved);if(!saved||prefab==null)throw new InvalidOperationException("Original prefab save refused.");
                        entries.Add(new FurrowstalkerLibrary.Entry{Id=row.id,Prefab=prefab,Mesh=mesh,Materials=materials,Spec=spec});changed.Add(prefabPath);
                    }
                    finally{if(prepared!=null)Object.DestroyImmediate(prepared);if(root!=null)Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(preview);}
                }
                string path=Folder+"/Library.asset";var library=AssetDatabase.LoadAssetAtPath<FurrowstalkerLibrary>(path);
                if(library==null){library=ScriptableObject.CreateInstance<FurrowstalkerLibrary>();AssetDatabase.CreateAsset(library,path);}
                library.Entries=entries.ToArray();library.Validate();EditorUtility.SetDirty(library);AssetDatabase.SaveAssetIfDirty(library);changed.Add(path);
                report.models=entries.Count;report.status="passed";
            }
            catch(Exception e){report.status="failed";report.error=e.ToString();throw;}
            finally{report.assets=changed.Distinct().ToArray();string full=Path.GetFullPath(reportPath);Directory.CreateDirectory(Path.GetDirectoryName(full));File.WriteAllText(full,JsonUtility.ToJson(report,true));}
            return report;
        }
        static Mesh Flatten(GameObject root,GameObject source)
        {
            var filters=source.GetComponentsInChildren<MeshFilter>(true);const int slots=1;
            if(filters.Length!=slots||source.GetComponentsInChildren<Renderer>(true).Length!=slots)throw new InvalidOperationException("Exactly one original static remains mesh required.");
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>[slots];for(int i=0;i<slots;i++)indices[i]=new List<int>();
            foreach(var filter in filters)
            {
                const int slot=0;var mesh=filter.sharedMesh;
                if(slot>=slots||indices[slot].Count!=0||mesh==null||!mesh.isReadable||mesh.subMeshCount!=1||mesh.GetTopology(0)!=MeshTopology.Triangles
                    ||filter.GetComponent<MeshRenderer>()==null||mesh.normals.Length!=mesh.vertexCount||mesh.uv.Length!=mesh.vertexCount)
                    throw new InvalidOperationException("Invalid original static source piece.");
                var matrix=root.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix;var normalMatrix=matrix.inverse.transpose;
                int offset=vertices.Count;var vs=mesh.vertices;var ns=mesh.normals;var us=mesh.uv;
                for(int i=0;i<vs.Length;i++){var v=matrix.MultiplyPoint3x4(vs[i]);var n=normalMatrix.MultiplyVector(ns[i]).normalized;
                    if(!Finite(v.x)||!Finite(v.y)||!Finite(v.z)||!Finite(n.x)||!Finite(n.y)||!Finite(n.z)||!Finite(us[i].x)||!Finite(us[i].y))throw new InvalidOperationException("Nonfinite static source buffer.");
                    vertices.Add(v);normals.Add(n);uv.Add(us[i]);}
                var ts=mesh.triangles;bool flip=matrix.determinant<0;
                for(int i=0;i<ts.Length;i+=3){indices[slot].Add(ts[i]+offset);indices[slot].Add(ts[i+(flip?2:1)]+offset);indices[slot].Add(ts[i+(flip?1:2)]+offset);}
            }
            var result=new Mesh{name=source.name+" (owned)"};result.SetVertices(vertices);result.SetNormals(normals);result.SetUVs(0,uv);result.subMeshCount=slots;
            for(int i=0;i<slots;i++){if(indices[i].Count==0){Object.DestroyImmediate(result);throw new InvalidOperationException("Missing original material piece.");}result.SetTriangles(indices[i],i);}
            result.RecalculateBounds();return result;
        }
        static Mesh SaveMesh(Mesh prepared,string path)
        {prepared.name=Path.GetFileNameWithoutExtension(path);var current=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(current==null){AssetDatabase.CreateAsset(prepared,path);current=prepared;}else{EditorUtility.CopySerialized(prepared,current);Object.DestroyImmediate(prepared);}EditorUtility.SetDirty(current);AssetDatabase.SaveAssetIfDirty(current);return current;}
        static bool Finite(float n)=>!float.IsNaN(n)&&!float.IsInfinity(n);
        static void Configure(string path, bool rigged)
        {
            var importer=AssetImporter.GetAtPath(path)as ModelImporter;if(importer==null)throw new InvalidOperationException("Animal model importer missing.");
            string before=EditorJsonUtility.ToJson(importer);importer.globalScale=1;importer.useFileScale=true;importer.bakeAxisConversion=false;importer.preserveHierarchy=true;
            importer.importCameras=false;importer.importLights=false;importer.addCollider=false;importer.importVisibility=false;importer.importNormals=ModelImporterNormals.Import;
            importer.importTangents=ModelImporterTangents.None;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.importAnimation=rigged;
            importer.animationType=rigged?ModelImporterAnimationType.Generic:ModelImporterAnimationType.None;importer.avatarSetup=rigged?ModelImporterAvatarSetup.CreateFromThisModel:ModelImporterAvatarSetup.NoAvatar;importer.isReadable=true;
            importer.optimizeGameObjects=false;importer.animationCompression=ModelImporterAnimationCompression.Off;
            if(before!=EditorJsonUtility.ToJson(importer))importer.SaveAndReimport();
            if(!rigged)return;
            var clips=new List<ModelImporterClipAnimation>();
            foreach(var name in ClipNames)
            {
                var matches=importer.defaultClipAnimations.Where(c=>Matches(c.name,name)||Matches(c.takeName,name)).ToArray();
                if(matches.Length!=1)throw new InvalidOperationException("Exact authored take required: "+name);
                var c=matches[0];c.name=name;c.loopTime=name=="Idle"||name=="Walk";c.loopPose=c.loopTime;clips.Add(c);
            }
            before=EditorJsonUtility.ToJson(importer);importer.clipAnimations=clips.ToArray();if(before!=EditorJsonUtility.ToJson(importer))importer.SaveAndReimport();
        }
        static bool Matches(string value,string name)=>value==name||value?.EndsWith("|"+name,StringComparison.Ordinal)==true||value?.EndsWith("/"+name,StringComparison.Ordinal)==true||value?.EndsWith("@"+name,StringComparison.Ordinal)==true;
        static AnimatorController Controller(string path,AnimationClip[] clips)
        {
            var result=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);if(result==null)result=AnimatorController.CreateAnimatorControllerAtPath(path);
            if(result.layers.Length!=1)throw new InvalidOperationException("Unexpected owned controller layers.");var machine=result.layers[0].stateMachine;
            if(machine.stateMachines.Length!=0||machine.anyStateTransitions.Length!=0||machine.entryTransitions.Length!=0)throw new InvalidOperationException("Unexpected owned controller transitions.");
            foreach(var s in machine.states)if(!ClipNames.Contains(s.state.name)||s.state.transitions.Length!=0)throw new InvalidOperationException("Unexpected owned state.");
            foreach(var name in ClipNames)
            {
                var found=machine.states.Where(s=>s.state.name==name).ToArray();if(found.Length>1)throw new InvalidOperationException("Duplicate owned animation state.");
                var state=found.Length==0?machine.AddState(name):found[0].state;state.motion=clips.Single(c=>c.name==name);state.writeDefaultValues=false;state.speed=1;
                if(name=="Idle")machine.defaultState=state;EditorUtility.SetDirty(state);
            }
            EditorUtility.SetDirty(machine);EditorUtility.SetDirty(result);AssetDatabase.SaveAssetIfDirty(result);return result;
        }
        static void Preflight<T>(string path)where T:Object
        {if((File.Exists(path)||AssetDatabase.LoadMainAssetAtPath(path)!=null)&&AssetDatabase.LoadAssetAtPath<T>(path)==null)throw new InvalidOperationException("Refusing foreign asset type: "+path);}
        static void EnsureFolder(string path)
        {if(AssetDatabase.IsValidFolder(path))return;string parent=Path.GetDirectoryName(path).Replace('\\','/');EnsureFolder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(path));}
        static string Hash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
    }
}
#endif
