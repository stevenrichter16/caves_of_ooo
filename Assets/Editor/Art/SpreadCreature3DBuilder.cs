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
    /// <summary>Explicit bounded adoption of five tested original creature-form FBXs.
    /// No global catalog, palette, scene, pipeline or source rig is rewritten.</summary>
    public static class SpreadCreature3DBuilder
    {
        const string Folder="Assets/Resources/SpreadCreature3D",Art="Assets/Art3D/SpreadBiome/Creatures";
        static readonly string[] ClipNames={"Idle","Walk","Interact","Attack","Hit"};
        [Serializable]sealed class Source{public int schemaVersion;public string id;public string[] palette;public Row[] models;}
        [Serializable]sealed class Row{public string id,sourceBlueprint,path,rigFamily,sha256;public bool rigged;public string[] clips,sockets,bones;public int triangles;}
        [Serializable]public sealed class Report{public string status,error,source,sourceHash;public string[] assets;public int models;}
        public static Report Build(string sourceDirectory,string reportPath)
        {
            var report=new Report{source=Path.GetFullPath(sourceDirectory)};var changed=new List<string>();
            try
            {
                if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Wait for idle native editor before scoped animal import.");
                string catalog=Path.Combine(report.source,"catalog.json");var data=JsonUtility.FromJson<Source>(File.ReadAllText(catalog));report.sourceHash=Hash(catalog);
                var glade=ReferenceGladeVoxelLibrary.Load();if(glade==null)throw new InvalidOperationException("Approved palette required.");glade.Validate();
                var texture=glade.Material.GetTexture("_BaseMap")as Texture2D;
                if(data?.models==null||data.models.Length!=5||data.schemaVersion!=1||data.id!="spread-lair-creatures"||data.palette?.Length!=24||texture==null||texture.width!=24||texture.height!=1||!texture.isReadable)
                    throw new InvalidOperationException("Exact five-source creature contract required.");
                for(int i=0;i<24;i++)
                {if(!ColorUtility.TryParseHtmlString(data.palette[i],out var c)||Vector4.Distance(c,texture.GetPixel(i,0))>.00001f)throw new InvalidOperationException("Animal palette differs from approved swatches.");}
                // Complete source, output type and existing importer preflight before writes.
                var ids=new HashSet<string>(StringComparer.Ordinal);
                foreach(var row in data.models)
                {
                    string id=row.id;
                    string expectedRig=SpreadCreature3DLibrary.RigFamily(id);
                    if(string.IsNullOrEmpty(id)||expectedRig==null||row.sourceBlueprint!=SpreadCreature3DLibrary.Blueprint(id)||!ids.Add(id)||row.path!="models/"+id+".fbx"||!row.rigged||row.rigFamily!=expectedRig
                        ||row.triangles<=0||row.clips==null||!row.clips.SequenceEqual(ClipNames)||row.sockets==null||row.sockets.Length!=0||row.bones==null||!row.bones.SequenceEqual(SpreadCreature3DLibrary.BoneNames(id)))
                        throw new InvalidOperationException("Invalid source animal anatomy/identity.");
                    var path=Path.Combine(report.source,row.path);if(!File.Exists(path)||Hash(path)!=row.sha256)throw new InvalidOperationException("Source FBX hash mismatch: "+id);
                    Preflight<GameObject>(Art+"/"+id+".fbx");Preflight<GameObject>(Folder+"/Actors/"+id+".prefab");Preflight<AnimatorController>(Art+"/"+id+".controller");
                }
                Preflight<SpreadCreature3DLibrary>(Folder+"/Library.asset");
                EnsureFolder(Art);EnsureFolder(Folder+"/Actors");var entries=new List<SpreadCreature3DLibrary.Entry>();
                foreach(var row in data.models)
                {
                    string asset=Art+"/"+row.id+".fbx",prefabPath=Folder+"/Actors/"+row.id+".prefab";
                    if(!File.Exists(asset)||Hash(asset)!=row.sha256){File.Copy(Path.Combine(report.source,row.path),asset,true);AssetDatabase.ImportAsset(asset,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);changed.Add(asset);}
                    Configure(asset);var model=AssetDatabase.LoadAssetAtPath<GameObject>(asset);if(model==null)throw new InvalidOperationException("Animal FBX import failed.");
                    var clips=AssetDatabase.LoadAllAssetsAtPath(asset).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__",StringComparison.Ordinal)).ToArray();
                    foreach(string n in ClipNames)if(clips.Count(c=>c.name==n&&c.length>0)!=1)throw new InvalidOperationException("Missing native animal clip: "+n);
                    string controllerPath=Art+"/"+row.id+".controller";var controller=Controller(controllerPath,clips);changed.Add(controllerPath);
                    var preview=EditorSceneManager.NewPreviewScene();GameObject root=null;
                    try
                    {
                        root=new GameObject(row.id);SceneManager.MoveGameObjectToScene(root,preview);
                        var instance=(GameObject)PrefabUtility.InstantiatePrefab(model,preview);instance.transform.SetParent(root.transform,false);
                        var skins=instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);var animators=instance.GetComponentsInChildren<Animator>(true);
                        if(skins.Length!=1||animators.Length!=1||animators[0].avatar==null||!animators[0].avatar.isValid||skins[0].sharedMesh==null||!skins[0].sharedMesh.isReadable)
                            throw new InvalidOperationException("Expected one readable native animal rig.");
                        if(instance.GetComponentsInChildren<Collider>(true).Length!=0||instance.GetComponentsInChildren<Rigidbody>(true).Length!=0)
                            throw new InvalidOperationException("Animal asset may not supply simulation or picking components.");
                        if(skins[0].sharedMaterials.Length!=1)throw new InvalidOperationException("Single original animal palette required.");
                        skins[0].sharedMaterial=glade.Material;skins[0].shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;skins[0].receiveShadows=true;
                        animators[0].runtimeAnimatorController=controller;animators[0].applyRootMotion=false;animators[0].cullingMode=AnimatorCullingMode.CullUpdateTransforms;
                        var mesh=skins[0].sharedMesh;int triangles=0;for(int i=0;i<mesh.subMeshCount;i++)triangles+=(int)mesh.GetIndexCount(i)/3;
                        if(triangles!=row.triangles)throw new InvalidOperationException("Imported triangle count differs from source.");
                        var spec=new SpawnRing3DCatalog.Model{id=row.id,path=prefabPath,sourceBlueprint=row.sourceBlueprint,kind="actor",rigFamily=row.rigFamily,rigged=true,
                            materialFamily="reference-glade-palette",boundsCenter=skins[0].bounds.center,boundsSize=skins[0].bounds.size,triangles=triangles,clips=(string[])ClipNames.Clone(),sockets=Array.Empty<string>()};
                        var prefab=PrefabUtility.SaveAsPrefabAsset(root,prefabPath,out bool saved);if(!saved||prefab==null)throw new InvalidOperationException("Animal prefab save refused.");
                        entries.Add(new SpreadCreature3DLibrary.Entry{Id=row.id,Prefab=prefab,Mesh=mesh,Spec=spec});changed.Add(prefabPath);
                    }
                    finally{if(root!=null)Object.DestroyImmediate(root);EditorSceneManager.ClosePreviewScene(preview);}
                }
                string libraryPath=Folder+"/Library.asset";var library=AssetDatabase.LoadAssetAtPath<SpreadCreature3DLibrary>(libraryPath);
                if(library==null){library=ScriptableObject.CreateInstance<SpreadCreature3DLibrary>();AssetDatabase.CreateAsset(library,libraryPath);}
                library.Entries=entries.ToArray();library.Validate();EditorUtility.SetDirty(library);AssetDatabase.SaveAssetIfDirty(library);changed.Add(libraryPath);
                report.models=entries.Count;report.status="passed";
            }
            catch(Exception e){report.status="failed";report.error=e.ToString();throw;}
            finally{report.assets=changed.ToArray();string full=Path.GetFullPath(reportPath);Directory.CreateDirectory(Path.GetDirectoryName(full));File.WriteAllText(full,JsonUtility.ToJson(report,true));}
            return report;
        }
        static void Configure(string path)
        {
            var importer=AssetImporter.GetAtPath(path)as ModelImporter;if(importer==null)throw new InvalidOperationException("Animal model importer missing.");
            string before=EditorJsonUtility.ToJson(importer);importer.globalScale=1;importer.useFileScale=true;importer.bakeAxisConversion=false;importer.preserveHierarchy=true;
            importer.importCameras=false;importer.importLights=false;importer.addCollider=false;importer.importVisibility=false;importer.importNormals=ModelImporterNormals.Import;
            importer.importTangents=ModelImporterTangents.None;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.importAnimation=true;
            importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.isReadable=true;
            importer.optimizeGameObjects=false;importer.animationCompression=ModelImporterAnimationCompression.Off;
            if(before!=EditorJsonUtility.ToJson(importer))importer.SaveAndReimport();
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
