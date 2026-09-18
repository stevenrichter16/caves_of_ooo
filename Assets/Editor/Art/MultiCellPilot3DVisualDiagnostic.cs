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
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace CavesOfOoo.Editor
{
    /// <summary>Read-only art diagnosis: actual imported geometry and native
    /// surface, paired shader/light conditions in one owned preview scene.
    /// Captures are evidence, not an automatic visual-quality acceptance.</summary>
    public static class MultiCellPilot3DVisualDiagnostic
    {
        const string NativeScene="Assets/Scenes/Main/SampleScene.unity";
        [Serializable] public sealed class SceneRestore {public string originalActiveScene;public bool sceneSetupRestored;}
        [Serializable] public sealed class Frame
        {public string name,png,pngSha256;public int width,height,darkPixels;public bool targetSRgb;public double meanLuma;}
        [Serializable] public sealed class Report
        {public string runId,status,error,scope,gpu,graphicsApi,colorSpace,sourceScene,sourceAmbientMode;public Color sourceAmbientSky,sourceAmbientEquator,sourceAmbientGround;public float sourceAmbientIntensity;public float[] sourceAmbientProbe;public string[] untouchedProbeUsages,unexpectedLogs;public bool activeScenePreserved,sceneDirtyFlagsPreserved;public Frame[] frames;}
        public static void RunFromCommandLine()
        {
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/MultiCellPilot/visual-diagnostic"));
            var args=Environment.GetCommandLineArgs();for(int i=0;i<args.Length-1;i++)if(args[i]=="-pilotVisualDiagnostic")folder=Path.GetFullPath(args[i+1]);
            var setup=EditorSceneManager.GetSceneManagerSetup();var previous=SceneManager.GetActiveScene();
            var sample=SceneManager.GetSceneByPath(NativeScene);bool opened=!sample.IsValid()||!sample.isLoaded;
            try
            {
                // Additive loading preserves existing saved and untitled scene
                // contents. Only the active scene supplies the captured SH.
                if(opened)sample=EditorSceneManager.OpenScene(NativeScene,OpenSceneMode.Additive);
                if(!SceneManager.SetActiveScene(sample))throw new InvalidOperationException("Could not activate actual native SampleScene.");
                Run(folder);
            }
            finally
            {
                if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
                if(opened&&sample.IsValid())EditorSceneManager.CloseScene(sample,true);
                var after=EditorSceneManager.GetSceneManagerSetup();bool restored=setup.Length==after.Length&&setup.Select((s,i)=>s.path==after[i].path&&s.isLoaded==after[i].isLoaded&&s.isActive==after[i].isActive).All(v=>v);
                if(Directory.Exists(folder))File.WriteAllText(Path.Combine(folder,"scene-restore.json"),JsonUtility.ToJson(new SceneRestore{originalActiveScene=previous.path,sceneSetupRestored=restored},true));
                if(!restored)throw new InvalidOperationException("Diagnostic scene setup restoration failed.");
            }
        }
        public static void Run(string folder)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Run outside an active Play/test/compile gate.");
            if(SceneManager.GetActiveScene().path!=NativeScene)throw new InvalidOperationException("Activate actual native SampleScene before this diagnosis; untitled ambient is not native evidence.");
            if(Directory.Exists(folder)&&Directory.EnumerateFileSystemEntries(folder).Any())throw new InvalidOperationException("Use a new empty diagnostic folder.");
            Directory.CreateDirectory(folder);var frames=new List<Frame>();var logs=new List<string>();
            var sourceSH=RenderSettings.ambientProbe;
            var report=new Report{runId=Guid.NewGuid().ToString("N"),scope="Standalone actual-model native-surface GPU diagnosis using captured source-editor ambient SH; white diagnostic native light/fog, no gameplay or final art acceptance",gpu=SystemInfo.graphicsDeviceName,graphicsApi=SystemInfo.graphicsDeviceType.ToString(),colorSpace=QualitySettings.activeColorSpace.ToString(),sourceScene=SceneManager.GetActiveScene().path,sourceAmbientMode=RenderSettings.ambientMode.ToString(),sourceAmbientSky=RenderSettings.ambientSkyColor,sourceAmbientEquator=RenderSettings.ambientEquatorColor,sourceAmbientGround=RenderSettings.ambientGroundColor,sourceAmbientIntensity=RenderSettings.ambientIntensity,sourceAmbientProbe=new float[27]};
            for(int channel=0;channel<3;channel++)for(int coefficient=0;coefficient<9;coefficient++)report.sourceAmbientProbe[channel*9+coefficient]=sourceSH[channel,coefficient];
            int active=SceneManager.GetActiveScene().handle;var scenes=Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray();var dirty=scenes.Select(s=>s.isDirty).ToArray();
            bool async=ShaderUtil.allowAsyncCompilation;var prior=RenderTexture.active;
            Scene preview=default;GameObject root=null;NativeZone3DRenderSurface surface=null;var owned=new List<Object>();
            Application.LogCallback observe=(message,stack,type)=>{if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)logs.Add(type+": "+message+"\n"+stack);};Application.logMessageReceived+=observe;
            try
            {
                ShaderUtil.allowAsyncCompilation=false;
                var village=Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath);village.Validate();
                var pilot=Resources.Load<MultiCellPilot3DLibrary>(MultiCellPilot3DLibrary.ResourcePath);pilot.Validate();
                preview=EditorSceneManager.NewPreviewScene();root=new GameObject("Owned pilot GPU visual diagnosis");SceneManager.MoveGameObjectToScene(root,preview);
                var sourceObject=new GameObject("Borrowed diagnostic source");sourceObject.transform.SetParent(root.transform,false);
                var source=sourceObject.AddComponent<Camera>();source.enabled=false;source.orthographic=true;source.orthographicSize=3;source.aspect=4f/3;source.transform.position=new Vector3(40.3f,12.5f,-10);
                surface=new NativeZone3DRenderSurface(root.transform,village.Renderer,village.RendererIndex,village.CompositeMaterial,new[]{pilot.WorldMaterial},2.2f);
                foreach(var entry in new[]{("PilotRidgeN_0",new Vector3(38,0,14)),("PilotBoulder_0",new Vector3(42,0,12.5f))})
                {
                    var model=Object.Instantiate(pilot.FindModel(entry.Item1),surface.ContentRoot);model.name=entry.Item1;model.transform.position=entry.Item2;surface.PrepareModel(model,false);
                }
                var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);floor.transform.SetParent(surface.ContentRoot,false);floor.transform.position=new Vector3(40,0,12.5f);floor.transform.localScale=new Vector3(1,1,.7f);floor.layer=NativeZone3DRenderSurface.WorldLayer;
                var groundMaterial=new Material(pilot.WorldMaterial);owned.Add(groundMaterial);groundMaterial.SetTexture("_BaseMap",Texture2D.whiteTexture);groundMaterial.SetTexture("_FogLight",surface.FogTexture);groundMaterial.SetColor("_BaseColor",new Color(.14f,.12f,.10f));groundMaterial.SetFloat("_Exposure",2.2f);floor.GetComponent<Renderer>().sharedMaterial=groundMaterial;
                var actorMaterial=surface.MaterialFor(pilot.WorldMaterial);var originalMap=actorMaterial.GetTexture("_BaseMap");
                var renderers=surface.ContentRoot.GetComponentsInChildren<Renderer>(true);
                if(renderers.Length<3)throw new InvalidOperationException("Actual subject renderers missing.");
                var originalUsages=renderers.Select(r=>r.lightProbeUsage).ToArray();
                var originalBlocks=renderers.Select(r=>{var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);return block;}).ToArray();
                report.untouchedProbeUsages=renderers.Select((r,i)=>r.name+":"+originalUsages[i]).ToArray();
                foreach(int height in new[]{216,768})
                {
                    int width=height*4/3;var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32){antiAliasing=1,filterMode=FilterMode.Point};target.Create();owned.Add(target);source.targetTexture=target;
                    surface.Sync(source,true,false);var camera=surface.WorldCamera;camera.enabled=false;camera.scene=preview;camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(preview);camera.GetUniversalAdditionalCameraData().renderShadows=true;
                    actorMaterial.SetFloat("_SunStrength",.9f);groundMaterial.SetFloat("_SunStrength",.9f);
                    actorMaterial.SetTexture("_BaseMap",originalMap);Fill(surface.FogTexture,255);surface.Sun.shadows=LightShadows.Soft;
                    for(int i=0;i<renderers.Length;i++){renderers[i].lightProbeUsage=originalUsages[i];renderers[i].SetPropertyBlock(originalBlocks[i]);}
                    Capture("native_default_probe_"+height,camera,target,folder,frames);
                    ApplySH(renderers,sourceSH);
                    Capture("baseline_"+height,camera,target,folder,frames);
                    surface.Sun.shadows=LightShadows.None;Capture("shadow_off_"+height,camera,target,folder,frames);
                    Fill(surface.FogTexture,128);Capture("unlit_palette_"+height,camera,target,folder,frames);
                    actorMaterial.SetTexture("_BaseMap",Texture2D.whiteTexture);Capture("unlit_white_"+height,camera,target,folder,frames);
                    Fill(surface.FogTexture,255);Capture("lit_white_no_shadow_"+height,camera,target,folder,frames);
                    actorMaterial.SetFloat("_SunStrength",0);groundMaterial.SetFloat("_SunStrength",0);Capture("ambient_only_source_editor_SH_"+height,camera,target,folder,frames);
                    var neutral=new SphericalHarmonicsL2();neutral.AddAmbientLight(new Color(.6f,.6f,.6f));
                    ApplySH(renderers,neutral);
                    Capture("ambient_only_neutral_SH_"+height,camera,target,folder,frames);
                    actorMaterial.SetTexture("_BaseMap",originalMap);actorMaterial.SetFloat("_SunStrength",.9f);groundMaterial.SetFloat("_SunStrength",.9f);
                    Capture("neutral_SH_palette_no_shadow_"+height,camera,target,folder,frames);
                    surface.Sun.shadows=LightShadows.Soft;Capture("neutral_SH_palette_shadow_"+height,camera,target,folder,frames);
                    source.targetTexture=null;
                }
                report.status="CAPTURED";
            }
            catch(Exception e){report.status="FAILED";report.error=e.ToString();}
            finally
            {
                RenderTexture.active=prior;
                surface?.Dispose();if(root!=null)Object.DestroyImmediate(root);
                for(int i=owned.Count-1;i>=0;i--){if(owned[i]is RenderTexture rt)rt.Release();if(owned[i]!=null)Object.DestroyImmediate(owned[i]);}
                if(preview.IsValid())EditorSceneManager.ClosePreviewScene(preview);
                ShaderUtil.allowAsyncCompilation=async;RenderTexture.active=prior;Application.logMessageReceived-=observe;report.unexpectedLogs=logs.ToArray();
                if(logs.Count>0)report.status="FAILED";
                report.activeScenePreserved=SceneManager.GetActiveScene().handle==active;report.sceneDirtyFlagsPreserved=scenes.Select((s,i)=>s.IsValid()&&s.isDirty==dirty[i]).All(v=>v);report.frames=frames.ToArray();File.WriteAllText(Path.Combine(folder,"diagnostic.json"),JsonUtility.ToJson(report,true));
            }
            if(report.status!="CAPTURED"||!report.activeScenePreserved||!report.sceneDirtyFlagsPreserved)throw new InvalidOperationException("Pilot GPU diagnosis failed; inspect report. "+report.error);
            Debug.Log("[Pilot3D] Visual diagnostic captured "+frames.Count+" paired frames: "+folder);
        }
        static void Fill(Texture2D texture,byte alpha)
        {var pixels=Enumerable.Repeat(new Color32(255,255,255,alpha),texture.width*texture.height).ToArray();texture.SetPixels32(pixels);texture.Apply(false,false);}
        static void ApplySH(Renderer[] renderers,SphericalHarmonicsL2 sh)
        {foreach(var renderer in renderers){var block=new MaterialPropertyBlock();block.CopySHCoefficientArraysFrom(new[]{sh});renderer.lightProbeUsage=LightProbeUsage.CustomProvided;renderer.SetPropertyBlock(block);}}
        static void Capture(string name,Camera camera,RenderTexture target,string folder,List<Frame> frames)
        {
            var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target};
            if(!RenderPipeline.SupportsRenderRequest(camera,request))throw new InvalidOperationException("GPU request unsupported.");
            Texture2D read=null;var prior=RenderTexture.active;
            try
            {
                RenderTexture.active=target;GL.Clear(true,true,Color.black);RenderPipeline.SubmitRenderRequest(camera,request);RenderTexture.active=target;
                read=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false,true);read.ReadPixels(new Rect(0,0,target.width,target.height),0,0);read.Apply(false,false);var pixels=read.GetPixels32();
                string path=Path.Combine(folder,name+".png");var png=read.EncodeToPNG();File.WriteAllBytes(path,png);string digest;using(var sha=SHA256.Create())digest=BitConverter.ToString(sha.ComputeHash(png)).Replace("-","").ToLowerInvariant();
                frames.Add(new Frame{name=name,png=path,pngSha256=digest,width=target.width,height=target.height,targetSRgb=target.sRGB,darkPixels=pixels.Count(p=>Math.Max(p.r,Math.Max(p.g,p.b))<12),meanLuma=pixels.Average(p=>(p.r+p.g+p.b)/(3.0*255))});
                if((name.StartsWith("unlit_white_")||name.StartsWith("ambient_only_neutral_SH_"))&&pixels.Count(p=>p.r+p.g+p.b>45)<pixels.Length/8)throw new InvalidOperationException("Known positive geometry control is mostly blank: "+name);
            }
            finally{RenderTexture.active=prior;if(read!=null)Object.DestroyImmediate(read);}
        }
    }
}
#endif
