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
using Object = UnityEngine.Object;

namespace CavesOfOoo.Editor
{
    /// <summary>Explicit, rerunnable import of the completed Blender export.
    /// No startup hook, no scene mutation, no deletion, no importer auto-run.
    /// Owns only Art3D/Village, Resources/Village3D/Library and two serialized URP fields.
    /// The root reviewer must first accept the actual Unity axis/rig probe.</summary>
    public static class Village3DAssetBuilder
    {
        const string Art="Assets/Art3D/Village";
        const string LibraryPath="Assets/Resources/Village3D/Library.asset";
        const string PipelinePath="Assets/Settings/UniversalRP.asset";
        const string RendererPath=Art+"/Rendering/Village3DRenderer.asset";
        const string PackageRoot="Packages/com.unity.render-pipelines.universal";
        const string PlayerModel="character-teal";
        static readonly string[] ClipNames={"Idle","Walk","Interact","Attack","Hit"};
        static readonly string[] SocketNames={"Equipment.Head","Equipment.Hand.L","Equipment.Hand.R","Equipment.Back"};

        [Serializable] public sealed class ModelReport
        {
            public string id,asset,prefab,guid;public int vertices,triangles,renderers,skinnedRenderers;
            public Vector3 boundsCenter,boundsSize;public string[] clips,sockets;
        }
        [Serializable] public sealed class Report
        {
            public string runId,sourceRoot,manifestSha256,startedUtc,finishedUtc,status,error;
            public string pipelineAsset,rendererAsset,libraryAsset,urpBeforeSnapshot,urpAfterSnapshot;
            public int defaultRendererBefore,defaultRendererAfter,rendererIndex;
            public bool softShadowsBefore,softShadowsAfter;
            public string[] renderersBefore,renderersAfter,allowedPipelineChanges;
            public string[] changedFiles,warnings;public ModelReport[] models;
        }
        sealed class Context
        {
            public readonly List<string> Changes=new List<string>(),Warnings=new List<string>();
            public readonly List<ModelReport> Models=new List<ModelReport>();
            public readonly Dictionary<string,string> ExistingGuids=new Dictionary<string,string>(StringComparer.Ordinal);
            public readonly Dictionary<string,string> BeforeHashes=new Dictionary<string,string>(StringComparer.Ordinal);
            public readonly Report Report=new Report();public string ReportPath;
        }

        [MenuItem("Tools/Caves of Ooo/Village 3D/Import completed Blender export")]
        public static void ChooseAndBuild()
        {
            string source=EditorUtility.OpenFolderPanel("Completed village export (manifest + models + textures)","","");
            if(!string.IsNullOrEmpty(source))Build(source);
        }
        public static void BuildFromCommandLine()
        {
            var args=Environment.GetCommandLineArgs();
            string source=Argument(args,"-village3dSource");
            if(string.IsNullOrEmpty(source))throw new ArgumentException("Supply -village3dSource <completed export directory>.");
            Build(source,Argument(args,"-village3dReport"));
        }
        public static Report Build(string sourceDirectory,string reportPath=null)
        {
            var c=new Context();c.Report.runId=Guid.NewGuid().ToString("N");
            c.Report.startedUtc=DateTime.UtcNow.ToString("O");
            c.ReportPath=string.IsNullOrEmpty(reportPath)?Path.Combine(Path.GetTempPath(),"Village3DImport-"+c.Report.runId+".json"):Path.GetFullPath(reportPath);
            try
            {
                if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop the owned test/play run before explicit asset import.");
                if(EditorApplication.isCompiling)throw new InvalidOperationException("Wait for current compilation before explicit asset import.");
                string source=Path.GetFullPath(sourceDirectory);
                c.Report.sourceRoot=source;
                string manifestFile=SafeSourceFile(source,"manifest.json");
                string json=File.ReadAllText(manifestFile);
                var native=MorrowfastSceneDefinition.Load();
                if(native==null)throw new InvalidOperationException("Native Morrowfast definition is required before importing art.");
                var manifest=Village3DManifest.Parse(json,native);
                ValidateMetadata(manifest);
                // Validate every required source before writing any destination file.
                foreach(var model in manifest.models)SafeSourceFile(source,model.path);
                SafeSourceFile(source,manifest.paletteTexture);
                var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
                if(pipeline==null||GraphicsSettings.defaultRenderPipeline!=pipeline)
                    throw new InvalidOperationException("Expected configured pipeline at "+PipelinePath+"; no GraphicsSettings change is permitted.");
                if(QualitySettings.renderPipeline!=null&&QualitySettings.renderPipeline!=pipeline)
                    throw new InvalidOperationException("Current quality overrides the expected pipeline; resolve explicitly, do not replace it here.");
                RequireNoUnsavedPipelineEdits(pipeline);
                ReadPipeline(c,pipeline,before:true);
                if(c.Report.defaultRendererBefore!=0)throw new InvalidOperationException("Expected existing default renderer index 0.");
                EnsureDirectory(Path.GetDirectoryName(c.ReportPath));
                c.Report.manifestSha256=Hash(File.ReadAllBytes(manifestFile));
                c.Report.pipelineAsset=PipelinePath;c.Report.rendererAsset=RendererPath;c.Report.libraryAsset=LibraryPath;
                c.Report.allowedPipelineChanges=new[]{"m_RendererDataList: append/reuse Village3DRenderer", "m_SoftShadowsSupported: true"};
                c.Report.urpBeforeSnapshot=Path.Combine(Path.GetDirectoryName(c.ReportPath),c.Report.runId+"-UniversalRP-before.asset.txt");
                c.Report.urpAfterSnapshot=Path.Combine(Path.GetDirectoryName(c.ReportPath),c.Report.runId+"-UniversalRP-after.asset.txt");
                File.Copy(AbsoluteAsset(PipelinePath),c.Report.urpBeforeSnapshot,true);
                foreach(string folder in new[]{"Models","Textures","Materials","Prefabs","Animations","Definitions","Rendering"})EnsureAssetFolder(Art+"/"+folder);
                EnsureAssetFolder("Assets/Resources/Village3D");
                TrackGuid(c,PipelinePath);TrackGuid(c,RendererPath);TrackGuid(c,LibraryPath);
                // Shaders are separately reviewed source files adopted alongside this builder.
                var worldShader=RequireShader(Art+"/Shaders/Village3DPalette.shader");
                var waterShader=RequireShader(Art+"/Shaders/Village3DWater.shader");
                var compositeShader=RequireShader(Art+"/Shaders/Village3DComposite.shader");
                foreach(var shader in new[]{worldShader,waterShader})
                    foreach(string property in new[]{"_FogLight","_Transient"})
                        if(shader.FindPropertyIndex(property)<0)throw new InvalidOperationException(shader.name+" lacks "+property);
                if(compositeShader.FindPropertyIndex("_MainTex")<0)throw new InvalidOperationException("Composite lacks _MainTex.");

                CopyIfChanged(c,manifestFile,Art+"/Definitions/manifest.json");
                string palettePath=Art+"/Textures/VillagePalette.png";
                CopyIfChanged(c,SafeSourceFile(source,manifest.paletteTexture),palettePath);
                ConfigurePalette(palettePath);
                var palette=AssetDatabase.LoadAssetAtPath<Texture2D>(palettePath);
                if(palette==null||palette.width<1||palette.height<1)throw new InvalidOperationException("Palette import failed.");
                var unseen=CreateUnseenFog(c);
                var world=GetMaterial(c,Art+"/Materials/VillageWorld.mat",worldShader);
                world.SetTexture("_BaseMap",palette);world.SetTexture("_FogLight",unseen);world.SetFloat("_Transient",0);world.enableInstancing=true;
                var water=GetMaterial(c,Art+"/Materials/VillageWater.mat",waterShader);
                water.SetTexture("_FogLight",unseen);water.SetFloat("_Transient",0);water.SetColor("_BaseColor",new Color(.09f,.21f,.23f,1));water.enableInstancing=true;
                var composite=GetMaterial(c,Art+"/Materials/VillageComposite.mat",compositeShader);
                composite.SetTexture("_MainTex",Texture2D.blackTexture);
                if(composite.HasProperty("_FlipY"))composite.SetFloat("_FlipY",0);
                SaveOwned(world);SaveOwned(water);SaveOwned(composite);

                var bindings=new List<Village3DLibrary.ModelBinding>(manifest.models.Length);
                foreach(var model in manifest.models)
                {
                    string modelPath=Art+"/Models/"+model.id+".fbx";
                    CopyIfChanged(c,SafeSourceFile(source,model.path),modelPath);
                    ConfigureModel(modelPath,model);
                    var imported=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                    if(imported==null)throw new InvalidOperationException("Imported model missing: "+model.id);
                    var clips=ResolveClips(modelPath,model);
                    var controller=model.rigged?BuildController(c,model.id,clips):null;
                    string prefabPath=Art+"/Prefabs/"+model.id+".prefab";TrackGuid(c,prefabPath);
                    var preview=EditorSceneManager.NewPreviewScene();
                    GameObject wrapper=null;
                    try
                    {
                        wrapper=new GameObject(model.id);SceneManager.MoveGameObjectToScene(wrapper,preview);
                        var instance=(GameObject)PrefabUtility.InstantiatePrefab(imported,preview);
                        instance.name=model.id+"__Model";instance.transform.SetParent(wrapper.transform,false);
                        // Preserve imported local transforms; axis correction belongs to validated export.
                        AssignMaterials(instance,world,water,model.id);
                        var animators=instance.GetComponentsInChildren<Animator>(true);
                        if(model.rigged)
                        {
                            if(animators.Length!=1)throw new InvalidOperationException(model.id+" must import exactly one Generic Animator.");
                            var animator=animators[0];
                            if(animator.avatar==null||!animator.avatar.isValid)throw new InvalidOperationException(model.id+" lacks a valid imported Generic avatar.");
                            animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;
                        }
                        else if(animators.Length!=0)throw new InvalidOperationException(model.id+" unexpectedly contains an Animator.");
                        foreach(var collider in instance.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(collider);
                        foreach(var body in instance.GetComponentsInChildren<Rigidbody>(true))Object.DestroyImmediate(body);
                        var report=ValidateImportedModel(model,wrapper,modelPath,prefabPath,clips);
                        var prefab=PrefabUtility.SaveAsPrefabAsset(wrapper,prefabPath,out bool success);
                        if(!success||prefab==null)throw new InvalidOperationException("Prefab save failed: "+model.id);
                        report.guid=AssetDatabase.AssetPathToGUID(prefabPath);c.Models.Add(report);
                        bindings.Add(new Village3DLibrary.ModelBinding{Id=model.id,Prefab=prefab});
                    }
                    finally{if(wrapper!=null)Object.DestroyImmediate(wrapper);EditorSceneManager.ClosePreviewScene(preview);}
                }
                // Publish metadata refs only after all imported models validate. On failure, owned
                // imports may be partial; the report records them for review/rerun, not asset rollback.
                var renderer=GetRenderer(c);
                int rendererIndex=AppendRendererAndSoftShadows(c,pipeline,renderer);
                var library=AssetDatabase.LoadAssetAtPath<Village3DLibrary>(LibraryPath);
                if(library==null)
                {
                    RefuseWrongAssetType(LibraryPath);
                    library=ScriptableObject.CreateInstance<Village3DLibrary>();AssetDatabase.CreateAsset(library,LibraryPath);c.Changes.Add(LibraryPath);
                }
                library.Manifest=AssetDatabase.LoadAssetAtPath<TextAsset>(Art+"/Definitions/manifest.json");
                library.WorldMaterial=world;library.WaterMaterial=water;library.CompositeMaterial=composite;
                library.Renderer=renderer;library.RendererIndex=rendererIndex;library.Models=bindings.ToArray();library.PlayerModelId=PlayerModel;
                library.InvalidateCaches();library.Validate();SaveOwned(library);
                ReadPipeline(c,pipeline,before:false);
                if(c.Report.defaultRendererAfter!=c.Report.defaultRendererBefore)throw new InvalidOperationException("Default renderer index changed.");
                for(int i=0;i<c.Report.renderersBefore.Length;i++)
                    if(c.Report.renderersBefore[i]!=c.Report.renderersAfter[i])throw new InvalidOperationException("Existing renderer entry changed at "+i);
                AssertGuidsStable(c);
                File.Copy(AbsoluteAsset(PipelinePath),c.Report.urpAfterSnapshot,true);
                c.Report.status="passed-import-validation";
                c.Warnings.Add("GPU shader rendering, texture orientation, shadow masking, skin deformation and visual quality require native Unity captures; asset validation is not that proof.");
                c.Warnings.Add("No obsolete art assets were deleted; retained unreferenced assets require a separately reviewed cleanup.");
                Debug.Log("[Village3D] Imported "+bindings.Count+" model prefabs; report "+c.ReportPath);
                return c.Report;
            }
            catch(Exception error){c.Report.status="failed";c.Report.error=error.ToString();Debug.LogError("[Village3D] Explicit import failed: "+error.Message);throw;}
            finally
            {
                c.Report.finishedUtc=DateTime.UtcNow.ToString("O");c.Report.models=c.Models.ToArray();
                foreach(var tracked in c.BeforeHashes)
                    if(FileHash(tracked.Key)!=tracked.Value)c.Changes.Add(tracked.Key);
                c.Report.changedFiles=c.Changes.Distinct().ToArray();c.Report.warnings=c.Warnings.ToArray();
                EnsureDirectory(Path.GetDirectoryName(c.ReportPath));File.WriteAllText(c.ReportPath,JsonUtility.ToJson(c.Report,true));
            }
        }

        static void ValidateMetadata(Village3DManifest m)
        {
            if(m.coordinates!="Unity X east, Y height, Z north; one unit per cell")throw new ArgumentException("Unexpected coordinate contract.");
            if(m.paletteTexture!="textures/VillagePalette.png")throw new ArgumentException("Unexpected palette path.");
            if(!m.models.Any(p=>p.id==PlayerModel&&p.rigged))throw new ArgumentException("Expected rigged "+PlayerModel);
            foreach(var owner in m.owners)
                if(owner.visibleWhen!="owner-visible"&&owner.visibleWhen!="room-open")throw new ArgumentException("Unknown owner visibility policy: "+owner.ownerId);
            foreach(var model in m.models)
            {
                if(model.triangles<=0)throw new ArgumentException("Completed export needs positive geometry: "+model.id);
                if(model.rigged)
                {
                    if(model.clips==null||model.clips.Length!=ClipNames.Length||!new HashSet<string>(model.clips,StringComparer.Ordinal).SetEquals(ClipNames))throw new ArgumentException("Rig clip declaration mismatch: "+model.id);
                    if(model.sockets==null||model.sockets.Length!=SocketNames.Length||!new HashSet<string>(model.sockets,StringComparer.Ordinal).SetEquals(SocketNames))throw new ArgumentException("Four equipment sockets required: "+model.id);
                }
            }
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
            importer.textureType=TextureImporterType.Default;importer.sRGBTexture=true;importer.mipmapEnabled=false;importer.isReadable=false;
            importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaSource=TextureImporterAlphaSource.None;
            if(before!=EditorJsonUtility.ToJson(importer))importer.SaveAndReimport();
        }
        static void ConfigureModel(string path,Village3DManifest.Model model)
        {
            var importer=AssetImporter.GetAtPath(path) as ModelImporter;if(importer==null)throw new InvalidOperationException("Model importer missing: "+path);
            string before=EditorJsonUtility.ToJson(importer);
            importer.globalScale=1;importer.useFileScale=true;importer.bakeAxisConversion=false;
            importer.preserveHierarchy=true;importer.importCameras=false;importer.importLights=false;importer.addCollider=false;
            importer.importVisibility=false;importer.importNormals=ModelImporterNormals.Import;importer.importTangents=ModelImporterTangents.None;
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            importer.importAnimation=model.rigged;importer.animationType=model.rigged?ModelImporterAnimationType.Generic:ModelImporterAnimationType.None;
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
        static Dictionary<string,AnimationClip> ResolveClips(string path,Village3DManifest.Model model)
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
                    if(materialName=="VillageWater"||materialName.StartsWith("VillageWater.",StringComparison.Ordinal)||renderer.name.EndsWith("__Water",StringComparison.Ordinal))target[i]=water;
                    else if(materialName=="VillagePalette"||materialName.StartsWith("VillagePalette.",StringComparison.Ordinal))target[i]=world;
                    else throw new InvalidOperationException("Unknown FBX material "+materialName+" on "+id+"/"+renderer.name);
                }
                renderer.sharedMaterials=target;renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
            }
        }
        static ModelReport ValidateImportedModel(Village3DManifest.Model model,GameObject wrapper,string asset,string prefab,Dictionary<string,AnimationClip> clips)
        {
            var renderers=wrapper.GetComponentsInChildren<Renderer>(true);bool first=true;var bounds=new Bounds();int vertices=0,triangles=0,skinned=0;
            foreach(var renderer in renderers)
            {
                if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);
                Mesh mesh=null;if(renderer is SkinnedMeshRenderer skin){skinned++;mesh=skin.sharedMesh;if(skin.bones==null||skin.bones.Length==0)throw new InvalidOperationException("Missing imported bones: "+model.id);}
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
            var transforms=wrapper.GetComponentsInChildren<Transform>(true);var sockets=new List<string>();
            foreach(string socket in model.sockets??Array.Empty<string>())
            {if(transforms.Count(t=>t.name==socket)!=1)throw new InvalidOperationException("Missing/duplicate exact equipment socket "+model.id+"/"+socket);sockets.Add(socket);}
            if(model.rigged&&skinned==0)throw new InvalidOperationException("Rigged model imported without skin: "+model.id);
            if(!model.rigged&&skinned!=0)throw new InvalidOperationException("Static model imported with skin: "+model.id);
            return new ModelReport{id=model.id,asset=asset,prefab=prefab,vertices=vertices,triangles=triangles,renderers=renderers.Length,skinnedRenderers=skinned,boundsCenter=bounds.center,boundsSize=bounds.size,clips=clips.Keys.ToArray(),sockets=sockets.ToArray()};
        }
        static UniversalRendererData GetRenderer(Context c)
        {
            var value=AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if(value==null){RefuseWrongAssetType(RendererPath);value=ScriptableObject.CreateInstance<UniversalRendererData>();AssetDatabase.CreateAsset(value,RendererPath);c.Changes.Add(RendererPath);}
            value.renderingMode=RenderingMode.Forward;value.depthPrimingMode=DepthPrimingMode.Disabled;
            value.opaqueLayerMask=-1;value.transparentLayerMask=-1;value.prepassLayerMask=-1;
            if(value.postProcessData==null)value.postProcessData=AssetDatabase.LoadAssetAtPath<PostProcessData>(PackageRoot+"/Runtime/Data/PostProcessData.asset");
            ResourceReloader.ReloadAllNullIn(value,PackageRoot);SaveOwned(value);return value;
        }
        static int AppendRendererAndSoftShadows(Context c,UniversalRenderPipelineAsset pipeline,UniversalRendererData renderer)
        {
            var serialized=new SerializedObject(pipeline);serialized.Update();
            var list=Required(serialized,"m_RendererDataList");var selected=Required(serialized,"m_DefaultRendererIndex");var shadows=Required(serialized,"m_SoftShadowsSupported");
            if(selected.intValue!=0)throw new InvalidOperationException("Default renderer changed while importing.");
            int index=-1;for(int i=0;i<list.arraySize;i++)if(list.GetArrayElementAtIndex(i).objectReferenceValue==renderer){if(index>=0)throw new InvalidOperationException("Existing renderer is registered twice.");index=i;}
            if(index<0){index=list.arraySize;list.InsertArrayElementAtIndex(index);list.GetArrayElementAtIndex(index).objectReferenceValue=renderer;}
            shadows.boolValue=true;serialized.ApplyModifiedPropertiesWithoutUndo();SaveOwned(pipeline);c.Changes.Add(PipelinePath);c.Report.rendererIndex=index;return index;
        }
        static void ReadPipeline(Context c,UniversalRenderPipelineAsset pipeline,bool before)
        {
            var so=new SerializedObject(pipeline);var list=Required(so,"m_RendererDataList");var refs=new string[list.arraySize];
            for(int i=0;i<refs.Length;i++)refs[i]=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(list.GetArrayElementAtIndex(i).objectReferenceValue));
            int index=Required(so,"m_DefaultRendererIndex").intValue;bool shadows=Required(so,"m_SoftShadowsSupported").boolValue;
            if(before){c.Report.defaultRendererBefore=index;c.Report.softShadowsBefore=shadows;c.Report.renderersBefore=refs;}
            else{c.Report.defaultRendererAfter=index;c.Report.softShadowsAfter=shadows;c.Report.renderersAfter=refs;}
        }
        static SerializedProperty Required(SerializedObject o,string name)=>o.FindProperty(name)??throw new InvalidOperationException("Installed URP serialized API missing: "+name);
        static void RequireNoUnsavedPipelineEdits(Object pipeline)
        {if(EditorUtility.IsDirty(pipeline))throw new InvalidOperationException("URP asset has unsaved edits; save/review them explicitly before this bounded importer.");}
        static Texture2D CreateUnseenFog(Context c)
        {
            string path=Art+"/Textures/VillageFogUnbound.asset";TrackGuid(c,path);
            var t=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(t==null){RefuseWrongAssetType(path);t=new Texture2D(80,25,TextureFormat.RGBA32,false,true){name="VillageFogUnbound"};AssetDatabase.CreateAsset(t,path);c.Changes.Add(path);}
            if(t.width!=80||t.height!=25)throw new InvalidOperationException("Owned fog default dimensions changed.");
            t.filterMode=FilterMode.Point;t.wrapMode=TextureWrapMode.Clamp;t.SetPixels32(new Color32[80*25]);t.Apply(false,false);SaveOwned(t);return t;
        }
        static Material GetMaterial(Context c,string path,Shader shader)
        {
            TrackGuid(c,path);var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){RefuseWrongAssetType(path);material=new Material(shader);AssetDatabase.CreateAsset(material,path);c.Changes.Add(path);}
            else material.shader=shader;return material;
        }
        static Shader RequireShader(string path)
        {
            var shader=AssetDatabase.LoadAssetAtPath<Shader>(path);if(shader==null)throw new InvalidOperationException("Adopt reviewed shader first: "+path);
            var errors=ShaderUtil.GetShaderMessages(shader).Where(m=>m.severity.ToString()=="Error").ToArray();
            if(errors.Length>0)throw new InvalidOperationException("Shader import error: "+path+" "+string.Join("; ",errors.Select(m=>m.message)));
            return shader;
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
