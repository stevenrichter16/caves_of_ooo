#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace CavesOfOoo.Editor
{
    /// <summary>Explicit import of a completed native-ring Blender export.
    /// Owns only MultiCellPilot art/library assets; never rewrites village/pipeline assets,
    /// modifies an open scene, deletes obsolete content or starts automatically.</summary>
    public static class MultiCellPilot3DAssetBuilder
    {
        const string Art="Assets/Art3D/MultiCellPilot";
        const string LibraryPath="Assets/Resources/MultiCellPilot3D/Library.asset";
        static readonly string[] ClipNames={"Idle","Walk","Interact","Attack","Hit"};
        [Serializable] public sealed class ModelReport
        {
            public string id,asset,prefab,guid;public int vertices,triangles,renderers,skinnedRenderers;
            public Vector3 boundsCenter,boundsSize;public string[] clips,sockets;
        }
        [Serializable] public sealed class Report
        {
            public string runId,sourceRoot,catalogSha256,startedUtc,finishedUtc,status,error,libraryAsset;
            public int modelCount,blueprintCount,fellingOwnerCount,rendererIndex;
            public string[] changedFiles,warnings,borrowedAssets;public bool borrowedAssetsUnchanged;
            public ModelReport[] models;
        }
        sealed class Context
        {
            public readonly List<string> Changes=new List<string>(),Warnings=new List<string>();
            public readonly List<ModelReport> Models=new List<ModelReport>();
            public readonly Dictionary<string,string> ExistingGuids=new Dictionary<string,string>(StringComparer.Ordinal);
            public readonly Dictionary<string,string> BeforeHashes=new Dictionary<string,string>(StringComparer.Ordinal);
            public readonly Dictionary<string,string> BorrowedHashes=new Dictionary<string,string>(StringComparer.Ordinal);
            public readonly Report Report=new Report();public string ReportPath;
        }
        [MenuItem("Tools/Caves of Ooo/Multi-cell Pilot 3D/Import completed Blender export")]
        public static void ChooseAndBuild()
        {
            string source=EditorUtility.OpenFolderPanel("Completed spawn-ring export (catalog + models + textures)","","");
            if(!string.IsNullOrEmpty(source))Build(source);
        }
        public static void BuildFromCommandLine()
        {
            var args=Environment.GetCommandLineArgs();string source=Argument(args,"-multiCellPilot3dSource");
            if(string.IsNullOrEmpty(source))throw new ArgumentException("Supply -multiCellPilot3dSource <completed export directory>.");
            Build(source,Argument(args,"-multiCellPilot3dReport"));
        }
        public static Report Build(string sourceDirectory,string reportPath=null)
        {
            var c=new Context();c.Report.runId=Guid.NewGuid().ToString("N");c.Report.startedUtc=DateTime.UtcNow.ToString("O");
            c.ReportPath=string.IsNullOrEmpty(reportPath)?Path.Combine(Path.GetTempPath(),"MultiCellPilot3DImport-"+c.Report.runId+".json"):Path.GetFullPath(reportPath);
            try
            {
                if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop the owned play/test run before importing ring art.");
                if(EditorApplication.isCompiling)throw new InvalidOperationException("Wait for current compilation before explicit asset import.");
                string source=Path.GetFullPath(sourceDirectory);c.Report.sourceRoot=source;
                string catalogFile=SafeSourceFile(source,"catalog.json");
                var definition=MultiCellPilot3DCatalog.Parse(File.ReadAllText(catalogFile));
                // A source contract proposal with no real measured bounds/triangles fails Parse.
                // Check all files before writing any asset; no layout sample is imported as gameplay.
                foreach(var model in definition.models)SafeSourceFile(source,model.path);
                string paletteFile=SafeSourceFile(source,definition.paletteTexture);
                string groundFile=SafeSourceFile(source,"textures/PilotGroundAlbedo.png");
                var village=Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath);
                if(village==null)throw new InvalidOperationException("Import the validated shared village resources first.");village.Validate();
                var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                if(pipeline==null||village.RendererIndex<0||village.RendererIndex>=pipeline.rendererDataList.Length||pipeline.rendererDataList[village.RendererIndex]!=village.Renderer)
                    throw new InvalidOperationException("The active URP does not contain the validated shared forward renderer.");
                foreach(var material in new[]{village.WorldMaterial,village.WaterMaterial,village.CompositeMaterial})
                {
                    if(material.shader==null)throw new InvalidOperationException("Borrowed shader is unavailable.");
                    var errors=ShaderUtil.GetShaderMessages(material.shader).Where(m=>m.severity.ToString()=="Error").ToArray();
                    if(errors.Length>0)throw new InvalidOperationException("Borrowed shader has compiler errors: "+material.shader.name);
                }
                foreach(var borrowed in new Object[]{village,pipeline,village.Renderer,village.WorldMaterial,village.WaterMaterial,village.CompositeMaterial,
                    village.WorldMaterial.shader,village.WaterMaterial.shader,village.CompositeMaterial.shader})
                    TrackBorrowed(c,AssetDatabase.GetAssetPath(borrowed));
                c.Report.catalogSha256=Hash(File.ReadAllBytes(catalogFile));c.Report.libraryAsset=LibraryPath;
                c.Report.modelCount=definition.models.Length;c.Report.rendererIndex=village.RendererIndex;
                foreach(string folder in new[]{"Definitions","Models","Textures","Materials","Prefabs","Animations"})EnsureAssetFolder(Art+"/"+folder);
                EnsureAssetFolder("Assets/Resources/MultiCellPilot3D");TrackGuid(c,LibraryPath);
                CopyIfChanged(c,catalogFile,Art+"/Definitions/catalog.json");
                string palettePath=Art+"/Textures/PilotPalette.png";
                CopyIfChanged(c,paletteFile,palettePath);ConfigurePalette(palettePath);
                var palette=AssetDatabase.LoadAssetAtPath<Texture2D>(palettePath);
                if(palette==null||palette.width<=0||palette.height<=0)throw new InvalidOperationException("Ring palette import failed.");
                string groundPath=Art+"/Textures/PilotGroundAlbedo.png";
                CopyIfChanged(c,groundFile,groundPath);ConfigurePalette(groundPath);
                var groundTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(groundPath);
                if(groundTexture==null)throw new InvalidOperationException("Pilot ground texture import failed.");
                var unseen=CreateUnseenFog(c);
                var world=GetMaterial(c,Art+"/Materials/PilotPalette.mat",village.WorldMaterial.shader);
                world.SetTexture("_BaseMap",palette);world.SetTexture("_FogLight",unseen);world.SetFloat("_Transient",0);world.enableInstancing=true;
                var water=GetMaterial(c,Art+"/Materials/PilotTar.mat",village.WaterMaterial.shader);
                water.SetTexture("_FogLight",unseen);water.SetFloat("_Transient",0);water.SetColor("_BaseColor",new Color(.034f,.055f,.064f,1));water.enableInstancing=true;
                var ground=GetMaterial(c,Art+"/Materials/PilotGround.mat",village.WorldMaterial.shader);
                ground.SetTexture("_BaseMap",groundTexture);ground.SetTexture("_FogLight",unseen);ground.SetFloat("_Transient",0);ground.enableInstancing=true;
                SaveOwned(world);SaveOwned(water);SaveOwned(ground);
                var bindings=new List<MultiCellPilot3DLibrary.ModelBinding>(definition.models.Length);
                foreach(var model in definition.models)
                {
                    string modelPath=Art+"/Models/"+model.id+".fbx";
                    CopyIfChanged(c,SafeSourceFile(source,model.path),modelPath);ConfigureModel(modelPath,model);
                    var imported=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                    if(imported==null)throw new InvalidOperationException("Imported ring model missing: "+model.id);
                    var clips=ResolveClips(modelPath,model);var controller=model.rigged?BuildController(c,model.id,clips):null;
                    string prefabPath=Art+"/Prefabs/"+model.id+".prefab";TrackGuid(c,prefabPath);
                    var preview=EditorSceneManager.NewPreviewScene();GameObject wrapper=null;
                    try
                    {
                        wrapper=new GameObject(model.id);SceneManager.MoveGameObjectToScene(wrapper,preview);
                        var instance=(GameObject)PrefabUtility.InstantiatePrefab(imported,preview);
                        instance.name=model.id+"__Model";instance.transform.SetParent(wrapper.transform,false);
                        AssignMaterials(instance,world,water,model.id);
                        var animators=instance.GetComponentsInChildren<Animator>(true);
                        if(model.rigged)
                        {
                            if(animators.Length!=1||animators[0].avatar==null||!animators[0].avatar.isValid)
                                throw new InvalidOperationException(model.id+" must import exactly one valid Generic avatar/Animator.");
                            animators[0].runtimeAnimatorController=controller;animators[0].applyRootMotion=false;
                            animators[0].cullingMode=AnimatorCullingMode.CullUpdateTransforms;
                        }
                        else if(animators.Length!=0)throw new InvalidOperationException(model.id+" unexpectedly contains an Animator.");
                        foreach(var collider in instance.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
                        foreach(var body in instance.GetComponentsInChildren<Rigidbody>(true))Object.DestroyImmediate(body);
                        var evidence=ValidateImportedModel(model,wrapper,modelPath,prefabPath,clips);
                        var prefab=PrefabUtility.SaveAsPrefabAsset(wrapper,prefabPath,out bool success);
                        if(!success||prefab==null)throw new InvalidOperationException("Ring prefab save failed: "+model.id);
                        evidence.guid=AssetDatabase.AssetPathToGUID(prefabPath);c.Models.Add(evidence);
                        bindings.Add(new MultiCellPilot3DLibrary.ModelBinding{Id=model.id,Prefab=prefab});
                    }
                    finally{if(wrapper!=null)Object.DestroyImmediate(wrapper);EditorSceneManager.ClosePreviewScene(preview);}
                }
                // Failed imports may leave partial owned art writes, recorded below. No existing
                // library binding table is replaced until every model has validated successfully.
                var library=AssetDatabase.LoadAssetAtPath<MultiCellPilot3DLibrary>(LibraryPath);
                if(library==null)
                {
                    RefuseWrongAssetType(LibraryPath);library=ScriptableObject.CreateInstance<MultiCellPilot3DLibrary>();
                    AssetDatabase.CreateAsset(library,LibraryPath);c.Changes.Add(LibraryPath);
                }
                library.Catalog=AssetDatabase.LoadAssetAtPath<TextAsset>(Art+"/Definitions/catalog.json");
                library.WorldMaterial=world;library.TarMaterial=water;library.GroundMaterial=ground;library.Models=bindings.ToArray();
                library.InvalidateCaches();library.Validate();SaveOwned(library);
                AssertGuidsStable(c);AssertBorrowedUnchanged(c);c.Report.borrowedAssetsUnchanged=true;
                c.Report.status="passed-import-validation";
                c.Warnings.Add("Real GPU masks, native player controls, species animation quality and visual reference likeness still require separate captures.");
                c.Warnings.Add("No obsolete owned art was deleted; failed imports can leave partial owned writes for review/rerun.");
                Debug.Log("[MultiCellPilot3D] Imported "+bindings.Count+" native-ring model prefabs; report "+c.ReportPath);
                return c.Report;
            }
            catch(Exception error){c.Report.status="failed";c.Report.error=error.ToString();Debug.LogError("[MultiCellPilot3D] Explicit import failed: "+error.Message);throw;}
            finally
            {
                c.Report.finishedUtc=DateTime.UtcNow.ToString("O");c.Report.models=c.Models.ToArray();
                foreach(var tracked in c.BeforeHashes)if(FileHash(tracked.Key)!=tracked.Value)c.Changes.Add(tracked.Key);
                c.Report.changedFiles=c.Changes.Distinct().ToArray();c.Report.warnings=c.Warnings.ToArray();c.Report.borrowedAssets=c.BorrowedHashes.Keys.ToArray();
                EnsureDirectory(Path.GetDirectoryName(c.ReportPath));File.WriteAllText(c.ReportPath,JsonUtility.ToJson(c.Report,true));
            }
        }
        static void TrackBorrowed(Context c,string path)
        {
            if(string.IsNullOrEmpty(path))throw new InvalidOperationException("Borrowed resource is not a persistent asset.");
            foreach(string file in new[]{path,path+".meta"})c.BorrowedHashes[file]=FileHash(file);
        }
        static void AssertBorrowedUnchanged(Context c)
        {foreach(var pair in c.BorrowedHashes)if(FileHash(pair.Key)!=pair.Value)throw new InvalidOperationException("Borrowed resource unexpectedly changed: "+pair.Key);}
        static bool MaterialName(string actual,string expected)
        {
            if(actual==expected)return true;
            if(string.IsNullOrEmpty(actual)||!actual.StartsWith(expected+".",StringComparison.Ordinal)||actual.Length==expected.Length+1)return false;
            for(int i=expected.Length+1;i<actual.Length;i++)if(actual[i]<'0'||actual[i]>'9')return false;return true;
        }
        static string SafeSourceFile(string root,string relative)
        {
            if(string.IsNullOrEmpty(relative)||Path.IsPathRooted(relative)||relative.Contains('\\'))throw new ArgumentException("Invalid relative source path.");
            string prefix=Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
            string full=Path.GetFullPath(Path.Combine(prefix,relative));
            if(!full.StartsWith(prefix,StringComparison.Ordinal)||!File.Exists(full))throw new FileNotFoundException("Required completed source file missing or outside source root.",full);
            return full;
        }
        static void ConfigurePalette(string path)
        {
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;if(importer==null)throw new InvalidOperationException("Texture importer missing.");
            string before=EditorJsonUtility.ToJson(importer);
            importer.maxTextureSize=4096;importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.mipmapEnabled=false;importer.isReadable=false;
            importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaSource=TextureImporterAlphaSource.None;
            if(before!=EditorJsonUtility.ToJson(importer))importer.SaveAndReimport();
        }
        static void ConfigureModel(string path,MultiCellPilot3DCatalog.Model model)
        {
            var importer=AssetImporter.GetAtPath(path) as ModelImporter;if(importer==null)throw new InvalidOperationException("Model importer missing: "+path);
            string before=EditorJsonUtility.ToJson(importer);
            importer.globalScale=1;importer.useFileScale=true;importer.bakeAxisConversion=false;
            importer.preserveHierarchy=true;importer.importCameras=false;importer.importLights=false;importer.addCollider=false;
            importer.importVisibility=false;importer.importNormals=ModelImporterNormals.Import;importer.importTangents=ModelImporterTangents.None;
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            importer.importAnimation=model.rigged;importer.animationType=model.rigged?ModelImporterAnimationType.Generic:ModelImporterAnimationType.None;
            // Native ground patches combine current static meshes at runtime;
            // player builds cannot read the default non-readable FBX buffers.
            importer.isReadable=!model.rigged;
            importer.optimizeGameObjects=false;importer.animationCompression=ModelImporterAnimationCompression.Off;
            if(model.rigged)importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            if(before!=EditorJsonUtility.ToJson(importer))importer.SaveAndReimport();
            if(!model.rigged)return;
            // Default clips retain exact source take ranges; only canonicalize names/loop flags.
            var defaults=importer.defaultClipAnimations;var clips=new List<ModelImporterClipAnimation>();
            foreach(string expected in ClipNames)
            {
                var matches=defaults.Where(c=>MatchesClip(c.name,expected)||MatchesClip(c.takeName,expected)).ToArray();
                if(matches.Length!=1)throw new InvalidOperationException(model.id+" expected exactly one source take for "+expected+"; found "+matches.Length);
                var clip=matches[0];clip.name=expected;clip.loopTime=expected=="Idle"||expected=="Walk";clip.loopPose=clip.loopTime;
                clips.Add(clip);
            }
            string clipsBefore=EditorJsonUtility.ToJson(importer);importer.clipAnimations=clips.ToArray();
            if(clipsBefore!=EditorJsonUtility.ToJson(importer))importer.SaveAndReimport();
        }
        static bool MatchesClip(string source,string expected)
            =>source==expected||(!string.IsNullOrEmpty(source)&&(source.EndsWith("|"+expected,StringComparison.Ordinal)||source.EndsWith("/"+expected,StringComparison.Ordinal)||source.EndsWith("@"+expected,StringComparison.Ordinal)));
        static Dictionary<string,AnimationClip> ResolveClips(string path,MultiCellPilot3DCatalog.Model model)
        {
            var result=new Dictionary<string,AnimationClip>(StringComparer.Ordinal);
            if(!model.rigged)return result;
            var clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__",StringComparison.Ordinal)).ToArray();
            foreach(string name in ClipNames)
            {
                var matches=clips.Where(c=>c.name==name).ToArray();
                if(matches.Length!=1||matches[0].length<=0)throw new InvalidOperationException(model.id+" lacks unique nonempty clip "+name);
                result.Add(name,matches[0]);
            }
            return result;
        }
        static AnimatorController BuildController(Context c,string id,Dictionary<string,AnimationClip> clips)
        {
            string path=Art+"/Animations/"+id+".controller";TrackGuid(c,path);
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if(controller==null){RefuseWrongAssetType(path);controller=AnimatorController.CreateAnimatorControllerAtPath(path);c.Changes.Add(path);}
            if(controller.layers.Length!=1)throw new InvalidOperationException("Owned controller has unexpected additional layers: "+path);
            var machine=controller.layers[0].stateMachine;
            if(machine.stateMachines.Length!=0||machine.anyStateTransitions.Length!=0||machine.entryTransitions.Length!=0)
                throw new InvalidOperationException("Owned controller must not contain unreviewed submachines/transitions: "+path);
            foreach(var child in machine.states)
                if(!clips.ContainsKey(child.state.name)||child.state.transitions.Length!=0)throw new InvalidOperationException("Owned controller has unreviewed state/transition: "+path);
            foreach(string name in ClipNames)
            {
                var states=machine.states.Where(s=>s.state.name==name).ToArray();
                if(states.Length>1)throw new InvalidOperationException("Duplicate animation state: "+name);
                var state=states.Length==0?machine.AddState(name):states[0].state;
                state.motion=clips[name];state.speed=1;state.writeDefaultValues=false;
                if(name=="Idle")machine.defaultState=state;EditorUtility.SetDirty(state);
            }
            EditorUtility.SetDirty(machine);SaveOwned(controller);return controller;
        }
        static void AssignMaterials(GameObject instance,Material world,Material water,string id)
        {
            var renderers=instance.GetComponentsInChildren<Renderer>(true);
            if(renderers.Length==0)throw new InvalidOperationException("No renderable geometry: "+id);
            foreach(var renderer in renderers)
            {
                var source=renderer.sharedMaterials;if(source.Length==0)throw new InvalidOperationException("No material slots: "+renderer.name);
                var target=new Material[source.Length];
                for(int i=0;i<source.Length;i++)
                {
                    string materialName=source[i]!=null?source[i].name:"";
                    if(MaterialName(materialName,"PilotTar"))target[i]=water;
                    else if(MaterialName(materialName,"PilotPalette"))target[i]=world;
                    else throw new InvalidOperationException("Unknown FBX material "+materialName+" on "+id+"/"+renderer.name);
                }
                renderer.sharedMaterials=target;renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
            }
        }
        static ModelReport ValidateImportedModel(MultiCellPilot3DCatalog.Model model,GameObject wrapper,string asset,string prefab,Dictionary<string,AnimationClip> clips)
        {
            var renderers=wrapper.GetComponentsInChildren<Renderer>(true);bool first=true;var bounds=new Bounds();int vertices=0,triangles=0,skinned=0;
            foreach(var renderer in renderers)
            {
                if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);
                Mesh mesh=null;if(renderer is SkinnedMeshRenderer skin){skinned++;mesh=skin.sharedMesh;if(skin.bones==null||skin.bones.Length==0||skin.bones.Any(b=>b==null))throw new InvalidOperationException("Missing imported bones: "+model.id);}
                else if(renderer is MeshRenderer)mesh=renderer.GetComponent<MeshFilter>()?.sharedMesh;
                if(mesh==null||mesh.vertexCount<=0)throw new InvalidOperationException("Missing mesh: "+model.id+"/"+renderer.name);
                vertices+=mesh.vertexCount;for(int s=0;s<mesh.subMeshCount;s++)triangles+=(int)mesh.GetIndexCount(s)/3;
            }
            if(!Finite(bounds.center)||!Finite(bounds.size)||bounds.size.x<=0||bounds.size.y<=0||bounds.size.z<=0)throw new InvalidOperationException("Nonfinite/empty imported bounds: "+model.id);
            // Static axis/units drift must not silently become a compensating runtime transform.
            // Animated bind-pose bounds are reported; actual skin/clip gates remain native.
            if(!model.rigged)
            {
                float tolerance=Mathf.Max(.035f,model.boundsSize.magnitude*.02f);
                if((bounds.center-model.boundsCenter).magnitude>tolerance||(bounds.size-model.boundsSize).magnitude>tolerance)
                    throw new InvalidOperationException("Imported bounds disagree with exported Unity axes/units: "+model.id+" actual="+bounds+" expected="+model.boundsCenter+" / "+model.boundsSize);
            }
            if(triangles!=model.triangles)throw new InvalidOperationException("Imported triangle count disagrees with completed export: "+model.id+" actual="+triangles+" expected="+model.triangles);
            var transforms=wrapper.GetComponentsInChildren<Transform>(true);var sockets=new List<string>();
            var actualSockets=transforms.Where(t=>t.name.StartsWith("Equipment.",StringComparison.Ordinal)).Select(t=>t.name).ToArray();
            if(actualSockets.Length!=(model.sockets?.Length??0))throw new InvalidOperationException("Unexpected equipment socket on species rig: "+model.id);
            foreach(string socket in model.sockets??Array.Empty<string>())
            {if(transforms.Count(t=>t.name==socket)!=1)throw new InvalidOperationException("Missing/duplicate exact equipment socket "+model.id+"/"+socket);sockets.Add(socket);}
            if(model.rigged&&skinned==0)throw new InvalidOperationException("Rigged model imported without skin: "+model.id);
            if(!model.rigged&&skinned!=0)throw new InvalidOperationException("Static model imported with skin: "+model.id);
            return new ModelReport{id=model.id,asset=asset,prefab=prefab,vertices=vertices,triangles=triangles,renderers=renderers.Length,skinnedRenderers=skinned,boundsCenter=bounds.center,boundsSize=bounds.size,clips=clips.Keys.ToArray(),sockets=sockets.ToArray()};
        }
        static Texture2D CreateUnseenFog(Context c)
        {
            string path=Art+"/Textures/PilotFogUnbound.asset";TrackGuid(c,path);
            var t=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(t==null){RefuseWrongAssetType(path);t=new Texture2D(80,25,TextureFormat.RGBA32,false,true){name="PilotFogUnbound"};AssetDatabase.CreateAsset(t,path);c.Changes.Add(path);}
            if(t.width!=80||t.height!=25)throw new InvalidOperationException("Owned fog default dimensions changed.");
            t.filterMode=FilterMode.Point;t.wrapMode=TextureWrapMode.Clamp;t.SetPixels32(new Color32[80*25]);t.Apply(false,false);SaveOwned(t);return t;
        }
        static Material GetMaterial(Context c,string path,Shader shader)
        {
            TrackGuid(c,path);var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){RefuseWrongAssetType(path);material=new Material(shader);AssetDatabase.CreateAsset(material,path);c.Changes.Add(path);}
            else material.shader=shader;return material;
        }
        static void CopyIfChanged(Context c,string source,string destination)
        {
            TrackGuid(c,destination);byte[] bytes=File.ReadAllBytes(source);string absolute=AbsoluteAsset(destination);
            if(File.Exists(absolute)&&Hash(File.ReadAllBytes(absolute))==Hash(bytes))return;
            EnsureAssetFolder(Path.GetDirectoryName(destination).Replace('\\','/'));
            File.WriteAllBytes(absolute,bytes);AssetDatabase.ImportAsset(destination,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);c.Changes.Add(destination);
        }
        static void TrackGuid(Context c,string path)
        {
            string guid=AssetDatabase.AssetPathToGUID(path);
            if(!string.IsNullOrEmpty(guid)&&!c.ExistingGuids.ContainsKey(path))c.ExistingGuids.Add(path,guid);
            foreach(string tracked in new[]{path,path+".meta"})
                if(!c.BeforeHashes.ContainsKey(tracked))c.BeforeHashes.Add(tracked,FileHash(tracked));
        }
        static string FileHash(string assetPath)
        {string absolute=AbsoluteAsset(assetPath);return File.Exists(absolute)?Hash(File.ReadAllBytes(absolute)):null;}
        static void AssertGuidsStable(Context c)
        {foreach(var pair in c.ExistingGuids)if(AssetDatabase.AssetPathToGUID(pair.Key)!=pair.Value)throw new InvalidOperationException("Existing GUID changed: "+pair.Key);}
        static void RefuseWrongAssetType(string path)
        {if(AssetDatabase.LoadMainAssetAtPath(path)!=null||File.Exists(AbsoluteAsset(path)))throw new InvalidOperationException("Existing asset at owned path has unexpected type: "+path);}
        static void SaveOwned(Object value){EditorUtility.SetDirty(value);AssetDatabase.SaveAssetIfDirty(value);}
        static void EnsureAssetFolder(string folder)
        {if(AssetDatabase.IsValidFolder(folder))return;string parent=Path.GetDirectoryName(folder).Replace('\\','/');EnsureAssetFolder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(folder));}
        static string AbsoluteAsset(string path)=>Path.GetFullPath(Path.Combine(Application.dataPath,"..",path));
        static void EnsureDirectory(string directory){if(!string.IsNullOrEmpty(directory))Directory.CreateDirectory(directory);}
        static string Hash(byte[] data){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(data)).Replace("-","").ToLowerInvariant();}
        static bool Finite(Vector3 v)=>!(float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z)||float.IsInfinity(v.x)||float.IsInfinity(v.y)||float.IsInfinity(v.z));
        static string Argument(string[] args,string name){for(int i=0;i<args.Length-1;i++)if(args[i]==name)return args[i+1];return null;}
    }
}
#endif
