#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace CavesOfOoo.Editor
{
    /// <summary>Explicit standalone art-scene creation. No gameplay bootstrap,
    /// entities, save access, normal scene replacement, or implicit editor startup.</summary>
    public static class Village3DShowcaseBuilder
    {
        public const string ScenePath="Assets/Art3D/Village/Scenes/VillageShowcase.unity";
        const string Folder="Assets/Art3D/Village/Scenes";
        const string RootName="Village Art Showcase — no gameplay";
        const int Layer=Village3DPresenter.WorldLayer;
        [Serializable] public sealed class Report
        {
            public string scenePath,sceneGuid,contentPrefabPath,status,error;
            public int owners,roofs,interiors,staticPlacements,rendererIndex;
            public bool activeScenePreserved;
            public string[] unchangedGuidPaths;
        }
        [MenuItem("Tools/Caves of Ooo/Village 3D/Build Art Showcase")]
        public static void BuildMenu()=>Build();
        public static void BuildFromCommandLine()=>Build();
        public static Report Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)
                throw new InvalidOperationException("Finish the owned Play/compile run before creating the art scene.");
            for(int i=0;i<SceneManager.sceneCount;i++)
                if(SceneManager.GetSceneAt(i).path==ScenePath)throw new InvalidOperationException("Close the existing showcase before rebuilding its owned asset.");
            var library=Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath);
            if(library==null)throw new InvalidOperationException("Import the completed village Library first.");
            library.Validate();ValidateRenderer(library);
            var definition=library.Definition;
            // All refs and owner metadata validate before owned asset writes.
            foreach(var model in definition.models)if(library.FindModel(model.id)==null)throw new InvalidOperationException("Missing model: "+model.id);
            var ownerIds=new HashSet<string>(definition.owners.Select(o=>o.ownerId),StringComparer.Ordinal);
            if(ownerIds.Count!=71)throw new InvalidOperationException("Expected every one of the71 native owners.");
            string sceneGuid=AssetDatabase.AssetPathToGUID(ScenePath);
            var originalActive=SceneManager.GetActiveScene();
            int activeHandle=originalActive.handle;
            var openScenes=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i)).ToArray();
            var dirty=openScenes.Select(s=>s.isDirty).ToArray();
            var scenePaths=openScenes.Select(s=>s.path).ToArray();
            var report=new Report{scenePath=ScenePath,contentPrefabPath=Folder+"/VillageShowcaseContent.prefab",rendererIndex=library.RendererIndex,status="building"};
            var guids=new Dictionary<string,string>(StringComparer.Ordinal);
            foreach(string path in new[]{ScenePath,Folder+"/VillageShowcaseContent.prefab",Folder+"/VillageShowcaseReveal.asset",Folder+"/VillageShowcaseWorld.mat",Folder+"/VillageShowcaseWater.mat"})
            {string guid=AssetDatabase.AssetPathToGUID(path);if(!string.IsNullOrEmpty(guid))guids.Add(path,guid);}
            Scene preview=default;
            try
            {
                EnsureFolder(Folder);
                var reveal=RevealTexture();
                var world=ShowcaseMaterial(Folder+"/VillageShowcaseWorld.mat",library.WorldMaterial,reveal);
                var water=ShowcaseMaterial(Folder+"/VillageShowcaseWater.mat",library.WaterMaterial,reveal);
                preview=EditorSceneManager.NewPreviewScene();
                var root=NewObject(RootName,preview,null);
                var staticRoot=NewObject("Static art",preview,root.transform);
                foreach(var placement in definition.staticPlacements)Place(library,placement,placement.id,staticRoot.transform,preview,world,water);
                var ownersRoot=NewObject("Owners",preview,root.transform);
                var roofs=new List<GameObject>();var interiors=new List<GameObject>();
                foreach(var owner in definition.owners)
                {
                    var obj=Place(library,owner,owner.ownerId,ownersRoot.transform,preview,world,water);
                    if(owner.kind=="roof")roofs.Add(obj);
                    else if(owner.visibleWhen=="room-open")interiors.Add(obj);
                }
                // Deliberate display pose, not the normal bootstrap spawn or a simulation entity.
                var player=new Village3DManifest.Placement{modelId="character-teal",position=Village3DProjection.CellCentre(40,23),scale=Vector3.one};
                Place(library,player,"Player preview — art pose only",root.transform,preview,world,water);
                root.AddComponent<Village3DShowcaseCutaway>().Configure(roofs.ToArray(),interiors.ToArray(),false);
                AddCameraAndLight(root.transform,preview,library.RendererIndex);
                if(roofs.Count!=5||interiors.Count!=21)throw new InvalidOperationException("Unexpected roof/interior contract; review the authored manifest before rebuilding.");
                SavePrefabBackedScene(root,definition);
                AssetDatabase.ImportAsset(ScenePath,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
                report.sceneGuid=AssetDatabase.AssetPathToGUID(ScenePath);
                if(string.IsNullOrEmpty(report.sceneGuid)||(!string.IsNullOrEmpty(sceneGuid)&&sceneGuid!=report.sceneGuid))throw new InvalidOperationException("Showcase scene GUID was lost or replaced.");
                foreach(var entry in guids)if(AssetDatabase.AssetPathToGUID(entry.Key)!=entry.Value)throw new InvalidOperationException("Owned asset GUID changed: "+entry.Key);
                report.unchangedGuidPaths=guids.Keys.ToArray();report.owners=definition.owners.Length;
                report.staticPlacements=definition.staticPlacements.Length;report.roofs=roofs.Count;report.interiors=interiors.Count;
                report.status="saved-art-scene";
            }
            catch(Exception error){report.status="failed";report.error=error.ToString();throw;}
            finally
            {
                if(preview.IsValid())EditorSceneManager.ClosePreviewScene(preview);
                report.activeScenePreserved=SceneManager.GetActiveScene().handle==activeHandle&&SceneManager.sceneCount==openScenes.Length;
                for(int i=0;i<openScenes.Length;i++)report.activeScenePreserved&=openScenes[i].IsValid()&&openScenes[i].isDirty==dirty[i]
                    &&openScenes[i].path==scenePaths[i]&&i<SceneManager.sceneCount&&SceneManager.GetSceneAt(i).handle==openScenes[i].handle;
                File.WriteAllText(Path.Combine(Path.GetTempPath(),"Village3DShowcase-build.json"),JsonUtility.ToJson(report,true));
            }
            if(!report.activeScenePreserved)throw new InvalidOperationException("Current scene state changed while building the isolated art showcase.");
            return report;
        }
        // Public PrefabUtility serializes the complete hierarchy and its local references.
        // The tiny scene wrapper below is an explicit Unity6000 serialization-format
        // adapter, validated by reopening a candidate before publishing the final path.
        // It avoids creating/opening/saving any ordinary scene while an untitled scene
        // is open. There is no private preview-flag or save-guard bypass.
        static void SavePrefabBackedScene(GameObject root,Village3DManifest definition)
        {
            const string prefabPath=Folder+"/VillageShowcaseContent.prefab";
            if(AssetDatabase.LoadMainAssetAtPath(prefabPath)!=null&&AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)==null)
                throw new InvalidOperationException("Unexpected asset at showcase content path.");
            var content=PrefabUtility.SaveAsPrefabAsset(root,prefabPath,out bool saved);
            if(!saved||content==null)throw new InvalidOperationException("Owned showcase hierarchy could not be saved as a prefab.");
            if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(content.transform,out string guid,out long transformId)
                ||guid.Length!=32||!guid.All(Uri.IsHexDigit)||transformId==0)
                throw new InvalidOperationException("Saved showcase root lacks a persistent transform identity.");
            if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(content,out string objectGuid,out long objectId)||objectGuid!=guid||objectId==0)
                throw new InvalidOperationException("Saved showcase root lacks a persistent object identity.");
            string yaml=SceneWrapper(guid,transformId,objectId);
            string candidate=Folder+"/__VillageShowcaseCandidate_"+Guid.NewGuid().ToString("N")+".unity";
            try
            {
                File.WriteAllText(AbsoluteAsset(candidate),yaml,new System.Text.UTF8Encoding(false));
                AssetDatabase.ImportAsset(candidate,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
                ValidateSavedScene(candidate,definition);
                // Keep the final .meta and therefore its existing GUID. This does not
                // claim an atomic update of the prefab/material dependency graph.
                File.WriteAllText(AbsoluteAsset(ScenePath),yaml,new System.Text.UTF8Encoding(false));
                AssetDatabase.ImportAsset(ScenePath,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
                ValidateSavedScene(ScenePath,definition);
            }
            finally
            {
                // Unique, operation-owned staging asset only. Never delete ScenePath.
                if(AssetDatabase.LoadMainAssetAtPath(candidate)!=null)AssetDatabase.DeleteAsset(candidate);
                else
                {
                    string absolute=AbsoluteAsset(candidate);
                    if(File.Exists(absolute))File.Delete(absolute);
                    if(File.Exists(absolute+".meta"))File.Delete(absolute+".meta");
                }
            }
        }
        static string AbsoluteAsset(string path)=>Path.GetFullPath(Path.Combine(Application.dataPath,"..",path));
        static string SceneWrapper(string guid,long rootTransformId,long rootObjectId)
        {
            // Shape verified against Unity6000-generated PrefabInstance/stripped
            // Transform records in the actual imported model prefabs, and SceneRoots
            // in Assets/Scenes/Main/SampleScene.unity. IDs are local to this new file.
            return "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n"
                +"--- !u!1001 &100100000\nPrefabInstance:\n  m_ObjectHideFlags: 0\n  serializedVersion: 2\n"
                +"  m_Modification:\n    serializedVersion: 3\n    m_TransformParent: {fileID: 0}\n"
                +"    m_Modifications:\n    - target: {fileID: "+rootObjectId.ToString(System.Globalization.CultureInfo.InvariantCulture)+", guid: "+guid+", type: 3}\n      propertyPath: m_Name\n      value: "+RootName+"\n      objectReference: {fileID: 0}\n"
                +"    m_RemovedComponents: []\n    m_RemovedGameObjects: []\n    m_AddedGameObjects: []\n    m_AddedComponents: []\n"
                +"  m_SourcePrefab: {fileID: 100100000, guid: "+guid+", type: 3}\n"
                +"--- !u!4 &100100001 stripped\nTransform:\n  m_CorrespondingSourceObject: {fileID: "+rootTransformId.ToString(System.Globalization.CultureInfo.InvariantCulture)+", guid: "+guid+", type: 3}\n"
                +"  m_PrefabInstance: {fileID: 100100000}\n  m_PrefabAsset: {fileID: 0}\n"
                +"--- !u!1660057539 &9223372036854775807\nSceneRoots:\n  m_ObjectHideFlags: 0\n  m_Roots:\n  - {fileID: 100100001}\n";
        }
        static void ValidateSavedScene(string path,Village3DManifest definition)
        {
            Scene check=default;
            try
            {
                check=EditorSceneManager.OpenPreviewScene(path);
                var roots=check.GetRootGameObjects();
                if(roots.Length!=1||roots[0].name!=RootName)throw new InvalidOperationException("Saved showcase scene has missing/duplicate content roots.");
                var root=roots[0];var owners=root.transform.Find("Owners");var scenery=root.transform.Find("Static art");
                if(owners==null||owners.childCount!=definition.owners.Length||scenery==null||scenery.childCount!=definition.staticPlacements.Length)
                    throw new InvalidOperationException("Saved showcase hierarchy is incomplete.");
                foreach(var p in definition.owners)
                {
                    var t=owners.Find(p.ownerId);
                    if(t==null||Vector3.Distance(t.localPosition,p.position)>.00001f||t.localScale!=p.scale
                        ||Quaternion.Angle(t.localRotation,Quaternion.Euler(0,p.rotationY,0))>.001f)
                        throw new InvalidOperationException("Saved owner transform differs: "+p.ownerId);
                }
                var cutaway=root.GetComponent<Village3DShowcaseCutaway>();
                if(cutaway==null||root.GetComponentInChildren<Camera>(true)==null)throw new InvalidOperationException("Saved showcase components are incomplete.");
                // Actual file→scene object remapping, rather than a transient JSON
                // oracle: both directions must change the exact reloaded owner roots.
                foreach(bool open in new[]{false,true,false})
                {
                    cutaway.SetCutaway(open);
                    foreach(var p in definition.owners)
                    {
                        bool expected=p.kind=="roof"?!open:p.visibleWhen=="room-open"?open:true;
                        if(owners.Find(p.ownerId).gameObject.activeSelf!=expected)
                            throw new InvalidOperationException("Persistent cutaway reference failed: "+p.ownerId);
                    }
                }
            }
            finally{if(check.IsValid())EditorSceneManager.ClosePreviewScene(check);}
        }

        static void ValidateRenderer(Village3DLibrary library)
        {
            var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if(pipeline==null||library.RendererIndex<0||library.RendererIndex>=pipeline.rendererDataList.Length||pipeline.rendererDataList[library.RendererIndex]!=library.Renderer)
                throw new InvalidOperationException("The Library's 3D renderer is not installed in the active pipeline.");
            if(!pipeline.supportsSoftShadows)throw new InvalidOperationException("Import the explicitly approved soft-shadow pipeline configuration first.");
        }
        static GameObject NewObject(string name,Scene scene,Transform parent)
        {
            var go=new GameObject(name){layer=Layer};SceneManager.MoveGameObjectToScene(go,scene);
            if(parent!=null)go.transform.SetParent(parent,false);return go;
        }
        static GameObject Place(Village3DLibrary library,Village3DManifest.Placement p,string name,Transform parent,Scene scene,Material world,Material water)
        {
            var obj=(GameObject)PrefabUtility.InstantiatePrefab(library.FindModel(p.modelId),scene);
            obj.name=name;obj.transform.SetParent(parent,false);
            obj.transform.localPosition=p.position;obj.transform.localRotation=Quaternion.Euler(0,p.rotationY,0);obj.transform.localScale=p.scale;
            foreach(var t in obj.GetComponentsInChildren<Transform>(true))t.gameObject.layer=Layer;
            foreach(var renderer in obj.GetComponentsInChildren<Renderer>(true))
            {
                var mats=renderer.sharedMaterials;
                for(int i=0;i<mats.Length;i++)
                {
                    if(mats[i]==library.WorldMaterial)mats[i]=world;
                    else if(mats[i]==library.WaterMaterial)mats[i]=water;
                    else throw new InvalidOperationException("Unexpected imported material on "+name+"/"+renderer.name);
                }
                renderer.sharedMaterials=mats;renderer.shadowCastingMode=ShadowCastingMode.On;renderer.receiveShadows=true;
            }
            if(obj.GetComponentsInChildren<Collider>(true).Length!=0||obj.GetComponentsInChildren<Rigidbody>(true).Length!=0)
                throw new InvalidOperationException("Art prefab unexpectedly owns physics: "+p.modelId);
            foreach(var animator in obj.GetComponentsInChildren<Animator>(true)){animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;}
            return obj;
        }
        static void AddCameraAndLight(Transform parent,Scene scene,int rendererIndex)
        {
            var cameraObject=NewObject("Village art camera",scene,parent);var camera=cameraObject.AddComponent<Camera>();
            camera.transform.position=new Vector3(40,35,12.5f);camera.transform.rotation=Quaternion.Euler(90,0,0);
            camera.orthographic=true;camera.orthographicSize=12.75f;camera.nearClipPlane=.1f;camera.farClipPlane=80;
            camera.cullingMask=1<<Layer;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.035f,.03f,1);
            camera.allowHDR=false;camera.allowMSAA=false;camera.useOcclusionCulling=false;
            var data=camera.GetUniversalAdditionalCameraData();data.renderType=CameraRenderType.Base;data.SetRenderer(rendererIndex);
            data.renderPostProcessing=false;data.requiresColorOption=CameraOverrideOption.Off;data.requiresDepthOption=CameraOverrideOption.Off;
            cameraObject.AddComponent<Village3DShowcaseFrame>().ApplyViewport(1500,1020);
            var lightObject=NewObject("Village art soft daylight",scene,parent);var sun=lightObject.AddComponent<Light>();
            sun.type=LightType.Directional;sun.color=new Color(1,.94f,.82f);sun.intensity=1.1f;sun.cullingMask=1<<Layer;
            sun.shadows=LightShadows.Soft;sun.shadowStrength=.55f;sun.shadowBias=.04f;sun.shadowNormalBias=.15f;
            sun.transform.rotation=Quaternion.Euler(55,-35,0);sun.GetUniversalAdditionalLightData();
        }
        static Texture2D RevealTexture()
        {
            string path=Folder+"/VillageShowcaseReveal.asset";var t=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(t==null){RejectWrongAsset(path);t=new Texture2D(80,25,TextureFormat.RGBA32,false,true){name="Village art full reveal"};AssetDatabase.CreateAsset(t,path);}
            if(t.width!=80||t.height!=25||t.mipmapCount!=1||UnityEngine.Experimental.Rendering.GraphicsFormatUtility.IsSRGBFormat(t.graphicsFormat))throw new InvalidOperationException("Owned reveal texture contract changed.");
            var pixels=new Color32[80*25];for(int i=0;i<pixels.Length;i++)pixels[i]=new Color32(255,255,255,255);
            t.filterMode=FilterMode.Point;t.wrapMode=TextureWrapMode.Clamp;t.SetPixels32(pixels);t.Apply(false,false);Save(t);return t;
        }
        static Material ShowcaseMaterial(string path,Material source,Texture2D reveal)
        {
            var owned=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(owned==null){RejectWrongAsset(path);owned=new Material(source);AssetDatabase.CreateAsset(owned,path);}
            else EditorUtility.CopySerialized(source,owned);
            owned.name=Path.GetFileNameWithoutExtension(path);owned.SetTexture("_FogLight",reveal);owned.SetFloat("_Transient",0);Save(owned);return owned;
        }
        static void Save(Object obj){EditorUtility.SetDirty(obj);AssetDatabase.SaveAssetIfDirty(obj);}
        static void RejectWrongAsset(string path){if(AssetDatabase.LoadMainAssetAtPath(path)!=null||File.Exists(path))throw new InvalidOperationException("Unexpected asset at owned path: "+path);}
        static void EnsureFolder(string folder){if(AssetDatabase.IsValidFolder(folder))return;var parent=Path.GetDirectoryName(folder).Replace('\\','/');EnsureFolder(parent);AssetDatabase.CreateFolder(parent,Path.GetFileName(folder));}

        [MenuItem("Tools/Caves of Ooo/Village 3D/Open Art Showcase")]
        public static void OpenArtShowcase()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before explicitly opening the art scene.");
            if(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath)==null)Build();
            // Only this explicit menu replaces the open scene; the build never does.
            if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        }
        [MenuItem("Tools/Caves of Ooo/Village 3D/Toggle Showcase Cutaway")]
        public static void ToggleShowcaseCutaway()
        {
            var scene=SceneManager.GetActiveScene();
            if(scene.path!=ScenePath)throw new InvalidOperationException("Open the standalone art showcase before toggling its cutaway.");
            var c=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Village3DShowcaseCutaway>(true)).Single();
            Undo.RecordObject(c,"Toggle village art cutaway");
            foreach(var t in c.GetComponentsInChildren<Transform>(true))Undo.RecordObject(t.gameObject,"Toggle village art cutaway");
            c.Toggle();if(!EditorApplication.isPlaying)EditorSceneManager.MarkSceneDirty(scene);
        }

        /// <summary>Optional explicit GPU capture of the saved standalone scene.
        /// No Camera.Render/Built-in fallback and no live gameplay scene capture.</summary>
        public static void CaptureFromCommandLine()
        {
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/Village3D/showcase-"+Guid.NewGuid().ToString("N")));
            CaptureShowcaseNative(Path.Combine(folder,"overhead.png"));
            CaptureShowcaseNative(Path.Combine(folder,"cutaway.png"),true);
            Debug.Log("[Village3D] Native showcase captures: "+folder);
        }

        public static string CaptureShowcaseNative(string pngPath,bool cutaway=false,int width=1500,int height=1020)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Capture only outside an owned Play run.");
            if(width<=0||height<=0||width>8192||height>8192)throw new ArgumentOutOfRangeException(nameof(width));
            pngPath=Path.GetFullPath(pngPath??throw new ArgumentNullException(nameof(pngPath)));
            var previous=RenderTexture.active;int activeHandle=SceneManager.GetActiveScene().handle;Scene preview=default;RenderTexture rt=null;Texture2D image=null;
            try
            {
                preview=EditorSceneManager.OpenPreviewScene(ScenePath);
                var root=preview.GetRootGameObjects().Single(r=>r.name==RootName);
                root.GetComponent<Village3DShowcaseCutaway>().SetCutaway(cutaway);
                var camera=root.GetComponentInChildren<Camera>(true);camera.scene=preview;camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(preview);
                camera.GetComponent<Village3DShowcaseFrame>().ApplyViewport(width,height);
                foreach(var animator in root.GetComponentsInChildren<Animator>(true)){animator.Rebind();animator.Play("Idle",0,0);animator.Update(0);}
                rt=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32){name="Owned village art capture",antiAliasing=1};rt.Create();
                var request=new UniversalRenderPipeline.SingleCameraRequest{destination=rt};
                if(!RenderPipeline.SupportsRenderRequest(camera,request))throw new InvalidOperationException("Active URP renderer does not yet support the explicit camera request.");
                RenderPipeline.SubmitRenderRequest(camera,request);
                RenderTexture.active=rt;image=new Texture2D(width,height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply(false,false);
                Directory.CreateDirectory(Path.GetDirectoryName(pngPath));File.WriteAllBytes(pngPath,image.EncodeToPNG());return pngPath;
            }
            finally
            {
                RenderTexture.active=previous;if(image!=null)Object.DestroyImmediate(image);if(rt!=null){rt.Release();Object.DestroyImmediate(rt);}
                if(preview.IsValid())EditorSceneManager.ClosePreviewScene(preview);
                if(SceneManager.GetActiveScene().handle!=activeHandle)Debug.LogError("[Village3D] Art capture changed the active scene unexpectedly.");
            }
        }
    }
}
#endif
