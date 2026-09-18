#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
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
    /// <summary>Explicit standalone GPU acceptance probe. Uses actual village
    /// shaders and URP camera requests in one owned preview scene. No gameplay,
    /// asset import, pipeline mutation, RenderSettings or synthetic shader time.</summary>
    public static class Village3DShaderGpuProbe
    {
        const int Size=512,WorldLayer=12,CompositeLayer=13;
        const double ColorTolerance=2.0/255.0;
        static readonly Color32 Visible=new Color32(255,255,255,255);
        static readonly Color32 Unseen=new Color32(255,255,255,0);
        static readonly Color32 Memory=new Color32(120,120,120,128);
        [Serializable] public sealed class PixelSample { public string name;public int x,y,r,g,b,a; }
        [Serializable] public sealed class FrameRecord { public string name,png,sha256;public double realtime;public PixelSample[] samples;public double meanLuma; }
        [Serializable] public sealed class Metric { public string name;public double value; }
        [Serializable] public sealed class CaseRecord { public string id;public bool passed;public string error;public FrameRecord[] frames;public Metric[] metrics; }
        [Serializable] public sealed class Report
        {
            public string runId,scope,startedUtc,finishedUtc,status,error,unityVersion,gpu,graphicsApi,colorSpace,reportPath;
            public int width,height,worldRendererIndex,compositeRendererIndex,passed,failed;
            public bool activeScenePreserved,sceneDirtyFlagsPreserved,renderTextureActiveRestored,asyncCompilationRestored;
            public string[] unexpectedLogs,honestyBounds;public CaseRecord[] cases;
        }
        sealed class Frame
        {
            public Color32[] pixels;public FrameRecord record;
        }
        sealed class Case
        {
            public readonly string Id;public readonly List<FrameRecord> Frames=new List<FrameRecord>();public readonly List<Metric> Metrics=new List<Metric>();
            public Case(string id){Id=id;}
            public void Value(string name,double value)=>Metrics.Add(new Metric{name=name,value=value});
            public void Require(bool pass,string reason){if(!pass)throw new InvalidOperationException(reason);}
        }

        [MenuItem("Tools/Caves of Ooo/Village 3D/Run Standalone Shader GPU Probe")]
        public static void RunMenu()=>Run();
        public static void RunFromCommandLine()
        {
            string report=null;var args=Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length-1;i++)if(args[i]=="-village3dGpuReport")report=args[i+1];
            // Failure is thrown after the JSON/PNGs are written; an owned batch
            // launcher must preserve Unity's nonzero failure exit status.
            Run(report);
        }
        public static Report Run(string reportPath=null)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)
                throw new InvalidOperationException("Run this explicit GPU probe after the owned Play/compile run finishes.");
            string id=Guid.NewGuid().ToString("N");
            reportPath=Path.GetFullPath(reportPath??Path.Combine(Application.dataPath,"../Docs/Verification/Village3D",id,"shader-gpu.json"));
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            var report=new Report{runId=id,scope="standalone URP shader GPU probe — not native gameplay",startedUtc=DateTime.UtcNow.ToString("O"),reportPath=reportPath,width=Size,height=Size,unityVersion=Application.unityVersion,gpu=SystemInfo.graphicsDeviceName,graphicsApi=SystemInfo.graphicsDeviceType.ToString(),colorSpace=QualitySettings.activeColorSpace.ToString()};
            var results=new List<CaseRecord>();var logs=new List<string>();
            int activeHandle=SceneManager.GetActiveScene().handle;var current=Enumerable.Range(0,SceneManager.sceneCount).Select(i=>SceneManager.GetSceneAt(i)).ToArray();var dirty=current.Select(s=>s.isDirty).ToArray();
            var previousRT=RenderTexture.active;bool previousAsyncCompilation=ShaderUtil.allowAsyncCompilation;Probe p=null;
            Application.LogCallback observe=(message,stack,type)=>{if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)logs.Add(type+": "+message+"\n"+stack);};
            Application.logMessageReceived+=observe;
            try
            {
                ShaderUtil.allowAsyncCompilation=false;
                p=new Probe(Path.GetDirectoryName(reportPath));report.worldRendererIndex=p.Library.RendererIndex;report.compositeRendererIndex=0;
                RunCase(results,p,"01_palette_visible_unseen",c=>
                {
                    var blank=p.Blank(c,"blank");p.Fog(Visible);var visible=p.Snap(c,"visible");p.Fog(Unseen);var unseen=p.Snap(c,"unseen");
                    double positive=MeanDelta(visible,blank);double hidden=MaxDelta(unseen,blank);c.Value("visible_mean_rgb_delta",positive);c.Value("unseen_max_rgb_delta",hidden);
                    c.Require(positive>.04,"Visible palette control did not render substantial geometry.");c.Require(hidden<=ColorTolerance,"Unseen palette leaked color.");
                });
                RunCase(results,p,"02_wrong_dimensions_and_default_fail_closed",c=>
                {
                    var blank=p.Blank(c,"blank");p.Fog(Visible);var visible=p.Snap(c,"positive_80x25");
                    p.World.SetTexture("_FogLight",p.WrongSize);var wrong=p.Snap(c,"wrong_2x2_white");p.World.SetTexture("_FogLight",null);var unbound=p.Snap(c,"unbound_default");
                    c.Value("positive_mean_rgb_delta",MeanDelta(visible,blank));c.Value("wrong_size_max_rgb_delta",MaxDelta(wrong,blank));c.Value("default_max_rgb_delta",MaxDelta(unbound,blank));
                    c.Require(MeanDelta(visible,blank)>.04,"Correct-sized positive control was blank.");c.Require(MaxDelta(wrong,blank)<=ColorTolerance&&MaxDelta(unbound,blank)<=ColorTolerance,"Wrong-sized or default mask revealed palette geometry.");
                });
                RunCase(results,p,"03_transient_memory_vs_visible",c=>
                {
                    var blank=p.Blank(c,"blank");p.Transient(1);p.Fog(Memory);var memory=p.Snap(c,"transient_memory");p.Fog(Visible);var visible=p.Snap(c,"transient_visible");
                    c.Value("memory_max_rgb_delta",MaxDelta(memory,blank));c.Value("visible_mean_rgb_delta",MeanDelta(visible,blank));
                    c.Require(MaxDelta(memory,blank)<=ColorTolerance,"Transient renderer appeared in remembered cells.");c.Require(MeanDelta(visible,blank)>.04,"Transient positive control was blank.");
                });
                RunCase(results,p,"04_one_mesh_crosses_cell_mask",c=>
                {
                    var blank=p.Blank(c,"blank");p.Fog(Visible);var full=p.Snap(c,"all_visible");p.SplitFogAtX40();var split=p.Snap(c,"left_unseen_right_visible");
                    double leftPositive=MeanDelta(full,blank,.17f,.43f,.25f,.75f),rightPositive=MeanDelta(full,blank,.57f,.83f,.25f,.75f);
                    double left=MaxDelta(split,blank,.17f,.43f,.25f,.75f),right=MeanDelta(split,full,.57f,.83f,.25f,.75f);
                    c.Value("left_control_signal",leftPositive);c.Value("right_control_signal",rightPositive);c.Value("hidden_left_max_delta",left);c.Value("visible_right_mean_delta",right);
                    c.Require(leftPositive>.05&&rightPositive>.05,"Both halves must render before the mask changes.");c.Require(left<=ColorTolerance&&right<=ColorTolerance,"One owner was masked as a whole or cell UV mapping is incorrect.");
                });
                RunCase(results,p,"05_memory_ignores_live_light_and_caster",c=>
                {
                    p.UseGround();p.Fog(Memory);var memoryA=p.Snap(c,"memory_neutral_light_no_caster");
                    p.Caster.enabled=true;p.Sun.shadows=LightShadows.Soft;p.Sun.color=new Color(1,.06f,.02f);p.Sun.intensity=2;p.Sun.transform.rotation=Quaternion.Euler(35,60,0);
                    var memoryB=p.Snap(c,"memory_red_light_visible_shadow_caster");
                    p.Fog(Visible);var liveB=p.Snap(c,"visible_red_light");p.Caster.enabled=false;p.DefaultLight();var liveA=p.Snap(c,"visible_neutral_light");
                    c.Value("memory_max_delta",MaxDelta(memoryA,memoryB));c.Value("visible_light_change_mean_delta",MeanDelta(liveA,liveB));c.Value("memory_signal",memoryA.record.meanLuma);
                    c.Require(memoryA.record.meanLuma>.05,"Remembered receiver was blank.");c.Require(MaxDelta(memoryA,memoryB)<=ColorTolerance,"Memory used live light/shadows.");c.Require(MeanDelta(liveA,liveB)>.05,"Light-change positive control was ineffective.");
                });
                RunCase(results,p,"06_memory_obeys_native_rgb",c=>
                {
                    p.Fog(new Color32(40,40,40,128));var low=p.Snap(c,"memory_rgb40");p.Fog(new Color32(190,190,190,128));var high=p.Snap(c,"memory_rgb190");
                    double increase=high.record.meanLuma-low.record.meanLuma;c.Value("mean_luma_increase",increase);c.Value("low_luma",low.record.meanLuma);c.Value("high_luma",high.record.meanLuma);
                    c.Require(low.record.meanLuma>.005&&increase>.08,"Remembered RGB failed to affect actual pixels.");
                });
                RunCase(results,p,"07_water_memory_ignores_real_elapsed_time",c=>
                {
                    p.UseWater();p.Water.SetFloat("_WaveSpeed",7.13f);p.Water.SetFloat("_WaveStrength",.35f);p.Fog(Memory);
                    var memoryA=p.Snap(c,"memory_time_a");Thread.Sleep(300);var memoryB=p.Snap(c,"memory_time_b");
                    p.Fog(Visible);var liveA=p.Snap(c,"visible_time_a");Thread.Sleep(300);var liveB=p.Snap(c,"visible_time_b");
                    double elapsed=memoryB.record.realtime-memoryA.record.realtime;int animated=ChangedPixels(liveA,liveB,3);
                    c.Value("memory_elapsed_realtime",elapsed);c.Value("memory_max_rgb_delta",MaxDelta(memoryA,memoryB));c.Value("visible_changed_pixels_gt3",animated);c.Value("visible_max_rgb_delta",MaxDelta(liveA,liveB));
                    c.Require(elapsed>=.25&&memoryA.record.meanLuma>.01,"Memory time pair lacked elapsed time or visible signal.");c.Require(MaxDelta(memoryA,memoryB)<=ColorTolerance,"Remembered water changed with real shader time.");c.Require(animated>500&&MaxDelta(liveA,liveB)>.02,"Visible-water time control did not animate enough to prove the branch.");
                });
                RunCase(results,p,"08_visible_caster_produces_real_shadow",c=>
                {
                    p.UseGround();p.Fog(Visible);p.Sun.shadows=LightShadows.Soft;var clean=p.Snap(c,"receiver_without_caster");
                    p.Caster.enabled=true;p.CasterFog(Visible);var shadow=p.Snap(c,"visible_caster_shadow");p.Sun.shadows=LightShadows.None;var noShadow=p.Snap(c,"same_caster_light_shadows_off");
                    int dark=DarkenedPixels(clean,shadow,8);c.Value("shadow_darkened_pixels_gt8",dark);c.Value("shadow_control_mean_luma",clean.record.meanLuma);c.Value("disabled_shadow_max_delta",MaxDelta(clean,noShadow));
                    c.Require(clean.record.meanLuma>.1,"Receiver control was not lit.");c.Require(dark>100,"Visible caster did not produce a measurable real shadow.");c.Require(MaxDelta(clean,noShadow)<=ColorTolerance,"The visible-caster difference persists when shadow rendering is disabled.");
                });
                RunCase(results,p,"09_remembered_unseen_casters_do_not_shadow",c=>
                {
                    p.UseGround();p.Fog(Visible);p.Sun.shadows=LightShadows.Soft;var clean=p.Snap(c,"receiver_without_caster");p.Caster.enabled=true;
                    p.CasterFog(Visible);var visible=p.Snap(c,"positive_visible_caster");p.CasterFog(Memory);var memory=p.Snap(c,"remembered_caster");p.CasterFog(Unseen);var unseen=p.Snap(c,"unseen_caster");p.CasterTransient(1);p.CasterFog(Memory);var transientMemory=p.Snap(c,"transient_remembered_caster");
                    c.Value("positive_darkened_pixels_gt8",DarkenedPixels(clean,visible,8));c.Value("memory_max_rgb_delta",MaxDelta(clean,memory));c.Value("unseen_max_rgb_delta",MaxDelta(clean,unseen));c.Value("transient_memory_max_rgb_delta",MaxDelta(clean,transientMemory));
                    c.Require(DarkenedPixels(clean,visible,8)>100,"Shadow suppression test lacked a functioning visible shadow.");c.Require(MaxDelta(clean,memory)<=ColorTolerance&&MaxDelta(clean,unseen)<=ColorTolerance&&MaxDelta(clean,transientMemory)<=ColorTolerance,"Remembered/unseen caster leaked a shadow.");
                });
                RunCase(results,p,"10_water_visibility_symmetry",c=>
                {
                    p.UseWater();p.Water.SetFloat("_WaveStrength",0);var blank=p.Blank(c,"blank");p.Fog(Visible);var visible=p.Snap(c,"water_visible");
                    p.Fog(Unseen);var unseen=p.Snap(c,"water_unseen");p.Water.SetTexture("_FogLight",p.WrongSize);var wrong=p.Snap(c,"water_wrong_size");p.Fog(Memory);p.Transient(1);var memory=p.Snap(c,"water_transient_memory");
                    c.Value("visible_mean_rgb_delta",MeanDelta(visible,blank));c.Value("unseen_max_delta",MaxDelta(unseen,blank));c.Value("wrong_size_max_delta",MaxDelta(wrong,blank));c.Value("transient_memory_max_delta",MaxDelta(memory,blank));
                    c.Require(MeanDelta(visible,blank)>.02,"Water visible control failed.");c.Require(MaxDelta(unseen,blank)<=ColorTolerance&&MaxDelta(wrong,blank)<=ColorTolerance&&MaxDelta(memory,blank)<=ColorTolerance,"Water masking diverged from palette masking.");
                });
                RunCase(results,p,"11_renderer2d_composite_actual_rt_orientation",c=>p.CompositeCase(c));
                RunCase(results,p,"12_gameplay_exposure_preserves_darkness_and_light_ratios",c=>
                {
                    c.Require(p.World.HasProperty("_Exposure")&&p.Water.HasProperty("_Exposure"),"Both world materials require explicit scene exposure.");
                    foreach(bool water in new[]{false,true})
                    {
                        if(water)p.UseWater();
                        var material=water?p.Water:p.World;string label=water?"water":"stone";
                        material.SetFloat("_Exposure",1);p.Fog(new Color32(102,102,102,255));var original=p.Snap(c,label+"_native_daylight");
                        material.SetFloat("_Exposure",2.2f);var graded=p.Snap(c,label+"_graded_daylight");
                        double ratio=graded.record.meanLuma/original.record.meanLuma;c.Value(label+"_exposure_ratio",ratio);
                        c.Require(original.record.meanLuma>.005&&ratio>2.05&&ratio<2.35,"Exposure must scale actual lit color without substituting native light.");
                        p.Fog(new Color32(51,51,51,255));var darker=p.Snap(c,label+"_half_native_light");
                        c.Require(darker.record.meanLuma/graded.record.meanLuma>.46&&darker.record.meanLuma/graded.record.meanLuma<.54,"Native light contrast was flattened by grading.");
                        var blank=p.Blank(c,label+"_blank");p.Fog(new Color32(0,0,0,255));var unlit=p.Snap(c,label+"_zero_native_light");
                        c.Require(MaxDelta(blank,unlit)<=ColorTolerance,"Exposure invented light in a genuinely unlit cell.");
                        p.Fog(Unseen);var hidden=p.Snap(c,label+"_unseen_high_exposure");
                        c.Require(MaxDelta(blank,hidden)<=ColorTolerance,"Exposure revealed unseen geometry.");
                    }
                });
            }
            catch(Exception error){report.error=error.ToString();}
            finally
            {
                try{p?.Dispose();}catch(Exception error){report.error=(report.error??"")+"\nCleanup: "+error;}
                RenderTexture.active=previousRT;ShaderUtil.allowAsyncCompilation=previousAsyncCompilation;Application.logMessageReceived-=observe;
                report.activeScenePreserved=SceneManager.GetActiveScene().handle==activeHandle;
                report.sceneDirtyFlagsPreserved=current.Select((s,i)=>s.IsValid()&&s.isDirty==dirty[i]).All(x=>x);
                report.renderTextureActiveRestored=RenderTexture.active==previousRT;report.asyncCompilationRestored=ShaderUtil.allowAsyncCompilation==previousAsyncCompilation;report.cases=results.ToArray();report.passed=results.Count(x=>x.passed);report.failed=results.Count(x=>!x.passed);report.unexpectedLogs=logs.ToArray();
                report.status=report.error==null&&report.failed==0&&results.Count==12&&logs.Count==0&&report.activeScenePreserved&&report.sceneDirtyFlagsPreserved&&report.renderTextureActiveRestored&&report.asyncCompilationRestored?"PASS":"FAIL";
                report.finishedUtc=DateTime.UtcNow.ToString("O");report.honestyBounds=new[]{"Actual GPU color/shadow/composite pixel evidence in a standalone preview, not native gameplay or art-quality review.","Color readbacks use512x512 linear RGBA8; no screen-resolution/performance claim.","DepthOnly pass is not independently forced or inspected; color visibility and real ShadowCaster are exercised through URP.","A negative observation only passes alongside substantial positive rendered geometry, animation or shadow controls.","No global RenderSettings, pipeline asset or synthetic shader-time mutation. URP updates its usual per-camera shader state internally."};
                File.WriteAllText(reportPath,JsonUtility.ToJson(report,true));
            }
            if(report.status!="PASS")throw new InvalidOperationException("Village 3D standalone shader GPU probe failed; inspect "+reportPath);
            Debug.Log("[Village3D] Standalone shader GPU probe12/12 PASS: "+reportPath);return report;
        }
        static void RunCase(List<CaseRecord> results,Probe p,string id,Action<Case> run)
        {
            var c=new Case(id);var record=new CaseRecord{id=id};
            try{p.Reset();run(c);record.passed=true;}catch(Exception error){record.error=error.ToString();}
            record.frames=c.Frames.ToArray();record.metrics=c.Metrics.ToArray();results.Add(record);
        }

        sealed class Probe:IDisposable
        {
            public Village3DLibrary Library;public Material World,Water;public Texture2D WrongSize;public Renderer Caster;public Light Sun;
            readonly string folder;readonly List<Object> owned=new List<Object>();Scene scene;GameObject root,subject,ground;Renderer subjectRenderer,groundRenderer;
            Camera camera;RenderTexture target;Texture2D fog,casterFog;Material casterMaterial;readonly MaterialPropertyBlock block=new MaterialPropertyBlock();
            public Probe(string folder)
            {
                this.folder=folder;
                try
                {
                    Library=Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath);if(Library==null)throw new InvalidOperationException("Import actual Village3D Library before GPU acceptance.");Library.Validate();
                    var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
                    if(pipeline==null||Library.RendererIndex<0||Library.RendererIndex>=pipeline.rendererDataList.Length||pipeline.rendererDataList[Library.RendererIndex]!=Library.Renderer||pipeline.rendererDataList.Length==0||!(pipeline.rendererDataList[0] is Renderer2DData)||!pipeline.supportsSoftShadows)
                        throw new InvalidOperationException("Expected registered 3D renderer, original Renderer2D at0 and supported soft shadows.");
                    foreach(var mat in new[]{Library.WorldMaterial,Library.WaterMaterial,Library.CompositeMaterial})
                        if(mat==null||!mat.shader.isSupported||ShaderUtil.GetShaderMessages(mat.shader).Any(m=>m.severity.ToString()=="Error"))throw new InvalidOperationException("Actual village shader unsupported or failed import.");
                    scene=EditorSceneManager.NewPreviewScene();root=Make("Owned village shader probe",WorldLayer);
                    camera=Make("Probe world camera",WorldLayer).AddComponent<Camera>();ConfigureCamera(camera,Library.RendererIndex,WorldLayer,new Vector3(40,20,12.5f),Quaternion.Euler(90,0,0),2);
                    Sun=Make("Probe directional light",WorldLayer).AddComponent<Light>();Sun.type=LightType.Directional;Sun.cullingMask=1<<WorldLayer;Sun.lightmapBakeType=LightmapBakeType.Realtime;Sun.shadowStrength=1;Sun.shadowBias=.01f;Sun.shadowNormalBias=.03f;Sun.GetUniversalAdditionalLightData();
                    target=Own(new RenderTexture(Size,Size,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear){name="Owned shader probe RT",antiAliasing=1,filterMode=FilterMode.Point});target.Create();
                    fog=Texture(80,25);casterFog=Texture(80,25);WrongSize=Texture(2,2);Fill(WrongSize,Visible);
                    World=Clone(Library.WorldMaterial);Water=Clone(Library.WaterMaterial);casterMaterial=Clone(Library.WorldMaterial);
                    subject=Primitive(PrimitiveType.Cube,"One multi-cell probe mesh",new Vector3(40,.125f,12.5f),new Vector3(3,.25f,3));subjectRenderer=subject.GetComponent<Renderer>();
                    ground=Primitive(PrimitiveType.Plane,"Lit shadow receiver",new Vector3(40,0,12.5f),Vector3.one);groundRenderer=ground.GetComponent<Renderer>();
                    var caster=Primitive(PrimitiveType.Cube,"Visibility-controlled shadow-only caster",new Vector3(40,1.5f,12.5f),new Vector3(1,3,1));Caster=caster.GetComponent<Renderer>();Caster.shadowCastingMode=ShadowCastingMode.ShadowsOnly;Caster.sharedMaterial=casterMaterial;
                    subjectRenderer.shadowCastingMode=groundRenderer.shadowCastingMode=ShadowCastingMode.Off;
                    Reset();
                }
                catch{Dispose();throw;}
            }
            T Own<T>(T value)where T:Object{owned.Add(value);value.hideFlags=HideFlags.HideAndDontSave;return value;}
            GameObject Make(string name,int layer){var go=new GameObject(name){layer=layer};SceneManager.MoveGameObjectToScene(go,scene);if(root!=null)go.transform.SetParent(root.transform,false);return go;}
            GameObject Primitive(PrimitiveType type,string name,Vector3 position,Vector3 scale)
            {
                var go=GameObject.CreatePrimitive(type);SceneManager.MoveGameObjectToScene(go,scene);go.name=name;go.layer=WorldLayer;go.transform.SetParent(root.transform,false);go.transform.position=position;go.transform.localScale=scale;
                var collider=go.GetComponent<Collider>();if(collider!=null)Object.DestroyImmediate(collider);return go;
            }
            Material Clone(Material source)=>Own(new Material(source));
            Texture2D Texture(int width,int height)=>Own(new Texture2D(width,height,TextureFormat.RGBA32,false,true){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp});
            static void Fill(Texture2D texture,Color32 color){var pixels=new Color32[texture.width*texture.height];for(int i=0;i<pixels.Length;i++)pixels[i]=color;texture.SetPixels32(pixels);texture.Apply(false,false);}
            void ConfigureCamera(Camera value,int rendererIndex,int layer,Vector3 pos,Quaternion rotation,float size)
            {
                value.enabled=false;value.transform.position=pos;value.transform.rotation=rotation;value.orthographic=true;value.orthographicSize=size;value.nearClipPlane=.1f;value.farClipPlane=45;value.aspect=1;value.rect=new Rect(0,0,1,1);value.cullingMask=1<<layer;value.clearFlags=CameraClearFlags.SolidColor;value.backgroundColor=Color.black;value.allowHDR=false;value.allowMSAA=false;value.useOcclusionCulling=false;
                value.scene=scene;value.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(scene);var data=value.GetUniversalAdditionalCameraData();data.renderType=CameraRenderType.Base;data.SetRenderer(rendererIndex);data.renderPostProcessing=false;data.renderShadows=true;data.requiresDepthOption=CameraOverrideOption.Off;data.requiresColorOption=CameraOverrideOption.Off;
            }
            public void Reset()
            {
                subject.SetActive(true);ground.SetActive(false);Caster.enabled=false;camera.orthographicSize=2;DefaultLight();
                foreach(var mat in new[]{World,Water,casterMaterial}){mat.SetTexture("_BaseMap",Texture2D.whiteTexture);mat.SetColor("_BaseColor",new Color(.55f,.65f,.45f,1));mat.SetFloat("_AmbientStrength",0);mat.SetFloat("_SunStrength",1);mat.SetFloat("_Transient",0);mat.SetFloat("_WaveStrength",0);}
                foreach(var mat in new[]{World,Water,casterMaterial})if(mat.HasProperty("_Exposure"))mat.SetFloat("_Exposure",1);
                Water.SetColor("_BaseColor",new Color(.18f,.52f,.48f,1));World.SetTexture("_FogLight",fog);Water.SetTexture("_FogLight",fog);casterMaterial.SetTexture("_FogLight",casterFog);
                subjectRenderer.sharedMaterial=groundRenderer.sharedMaterial=World;Transient(0);Fog(Visible);CasterFog(Visible);
            }
            public void DefaultLight(){Sun.color=Color.white;Sun.intensity=1;Sun.shadows=LightShadows.None;Sun.transform.rotation=Quaternion.Euler(45,-45,0);}
            public void UseGround(){subject.SetActive(false);ground.SetActive(true);camera.orthographicSize=4;}
            public void UseWater(){subjectRenderer.sharedMaterial=Water;groundRenderer.sharedMaterial=Water;}
            public void Fog(Color32 color){Fill(fog,color);World.SetTexture("_FogLight",fog);Water.SetTexture("_FogLight",fog);}
            public void CasterFog(Color32 color)=>Fill(casterFog,color);
            public void CasterTransient(float value)=>casterMaterial.SetFloat("_Transient",value);
            public void SplitFogAtX40(){var pixels=new Color32[80*25];for(int y=0;y<25;y++)for(int x=0;x<80;x++)pixels[y*80+x]=x<40?Unseen:Visible;fog.SetPixels32(pixels);fog.Apply(false,false);}
            public void Transient(float value){block.Clear();block.SetFloat("_Transient",value);subjectRenderer.SetPropertyBlock(block);groundRenderer.SetPropertyBlock(block);}
            public Frame Blank(Case c,string name)
            {
                bool a=subject.activeSelf,b=ground.activeSelf;subject.SetActive(false);ground.SetActive(false);try{return Snap(c,name);}finally{subject.SetActive(a);ground.SetActive(b);}
            }
            public Frame Snap(Case c,string name)=>Render(c,name,camera,target);
            Frame Render(Case c,string name,Camera from,RenderTexture destination)
            {
                var request=new UniversalRenderPipeline.SingleCameraRequest{destination=destination};
                if(!RenderPipeline.SupportsRenderRequest(from,request))throw new InvalidOperationException("URP SingleCameraRequest is not available; do not accept blank output.");
                var prior=RenderTexture.active;Texture2D read=null;
                try
                {
                    // Every frame begins with a known clear; the actual camera also clears.
                    RenderTexture.active=destination;GL.Clear(true,true,Color.black);RenderPipeline.SubmitRenderRequest(from,request);
                    RenderTexture.active=destination;read=new Texture2D(Size,Size,TextureFormat.RGBA32,false,true);read.ReadPixels(new Rect(0,0,Size,Size),0,0);read.Apply(false,false);
                    var pixels=read.GetPixels32();string file=Path.Combine(folder,c.Id+"-"+name+".png");byte[] png=read.EncodeToPNG();File.WriteAllBytes(file,png);
                    var result=new Frame{pixels=pixels,record=new FrameRecord{name=name,png=file,sha256=Digest(read.GetRawTextureData<byte>().ToArray()),realtime=Time.realtimeSinceStartupAsDouble,meanLuma=pixels.Average(p=>(p.r+p.g+p.b)/(3.0*255.0)),samples=Samples(pixels)}};
                    c.Frames.Add(result.record);return result;
                }
                finally{RenderTexture.active=prior;if(read!=null)Object.DestroyImmediate(read);}
            }
            public void CompositeCase(Case c)
            {
                subject.SetActive(false);ground.SetActive(false);Caster.enabled=false;Fog(new Color32(255,255,255,128));camera.orthographicSize=2;
                var colors=new[]{Color.red,Color.green,Color.blue,new Color(1,1,0,1)};var markerObjects=new List<GameObject>();
                for(int i=0;i<4;i++)
                {
                    var marker=Primitive(PrimitiveType.Cube,"Actual RT quadrant "+i,new Vector3(i%2==0?39:41,.05f,i<2?11.5f:13.5f),new Vector3(1,.1f,1));markerObjects.Add(marker);
                    var mat=Clone(World);mat.SetColor("_BaseColor",colors[i]);marker.GetComponent<Renderer>().sharedMaterial=mat;
                }
                Camera compositeCamera=null;GameObject quad=null;RenderTexture output=null;
                try
                {
                    var source=Snap(c,"actual_world_rt_four_markers");
                    for(int i=0;i<4;i++){var value=MeanRgb(source,i%2==0?.25f:.75f,i<2?.25f:.75f);c.Value("source_marker_"+i+"_expected_color_distance",Vector3.Distance(value,new Vector3(colors[i].r,colors[i].g,colors[i].b)));c.Require(Vector3.Distance(value,new Vector3(colors[i].r,colors[i].g,colors[i].b))<.025f,"World RT marker orientation/color control failed at"+i);}
                    compositeCamera=Make("Actual Renderer2D composite camera",CompositeLayer).AddComponent<Camera>();ConfigureCamera(compositeCamera,0,CompositeLayer,new Vector3(0,0,-10),Quaternion.identity,2);
                    quad=Make("Actual village composite quad",CompositeLayer);var mesh=Own(new Mesh{name="Probe composite XY mesh"});mesh.vertices=new[]{new Vector3(-2,-2,0),new Vector3(2,-2,0),new Vector3(2,2,0),new Vector3(-2,2,0)};mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateBounds();quad.AddComponent<MeshFilter>().sharedMesh=mesh;
                    var mat=Clone(Library.CompositeMaterial);mat.SetTexture("_MainTex",target);mat.SetFloat("_FlipY",0);quad.AddComponent<MeshRenderer>().sharedMaterial=mat;
                    output=Own(new RenderTexture(Size,Size,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear){antiAliasing=1,filterMode=FilterMode.Point});output.Create();
                    var noFlip=Render(c,"composite_flip0",compositeCamera,output);mat.SetFloat("_FlipY",1);var flip=Render(c,"composite_flip1_control",compositeCamera,output);
                    for(int i=0;i<4;i++)
                    {
                        float x=i%2==0?.25f:.75f,y=i<2?.25f:.75f;double direct=Vector3.Distance(MeanRgb(noFlip,x,y),MeanRgb(source,x,y)),flipped=Vector3.Distance(MeanRgb(flip,x,y),MeanRgb(source,x,1-y));
                        c.Value("flip0_marker_"+i+"_distance",direct);c.Value("flip1_marker_"+i+"_swapped_distance",flipped);
                        c.Require(direct<.025&&flipped<.025,"Actual RenderTexture composite orientation disagrees with explicit FlipY at marker"+i);
                    }
                    c.Require(MeanDelta(noFlip,flip)>.05,"FlipY control produced no measurable color movement.");
                }
                finally{foreach(var obj in markerObjects)Object.DestroyImmediate(obj);if(quad!=null)Object.DestroyImmediate(quad);if(compositeCamera!=null)Object.DestroyImmediate(compositeCamera.gameObject);}
            }
            public void Dispose()
            {
                if(scene.IsValid()){EditorSceneManager.ClosePreviewScene(scene);scene=default;}
                for(int i=owned.Count-1;i>=0;i--)if(owned[i]!=null){if(owned[i] is RenderTexture rt)rt.Release();Object.DestroyImmediate(owned[i]);}owned.Clear();
            }
        }
        static PixelSample[] Samples(Color32[] pixels)
        {
            var names=new[]{"SW","SE","NW","NE","center"};var xs=new[]{128,384,128,384,256};var ys=new[]{128,128,384,384,256};var result=new PixelSample[5];
            for(int i=0;i<result.Length;i++){var p=pixels[ys[i]*Size+xs[i]];result[i]=new PixelSample{name=names[i],x=xs[i],y=ys[i],r=p.r,g=p.g,b=p.b,a=p.a};}return result;
        }
        static Vector3 MeanRgb(Frame frame,float u,float v)
        {
            int cx=(int)(u*Size),cy=(int)(v*Size);var sum=Vector3.zero;for(int y=cy-3;y<=cy+3;y++)for(int x=cx-3;x<=cx+3;x++){var p=frame.pixels[y*Size+x];sum+=new Vector3(p.r,p.g,p.b);}return sum/(49*255f);
        }
        static double MeanDelta(Frame a,Frame b,float x0=0,float x1=1,float y0=0,float y1=1)
        {
            double total=0;int n=0;for(int y=(int)(y0*Size);y<(int)(y1*Size);y++)for(int x=(int)(x0*Size);x<(int)(x1*Size);x++){int i=y*Size+x;var p=a.pixels[i];var q=b.pixels[i];total+=Math.Abs(p.r-q.r)+Math.Abs(p.g-q.g)+Math.Abs(p.b-q.b);n+=3;}return total/(n*255.0);
        }
        static double MaxDelta(Frame a,Frame b,float x0=0,float x1=1,float y0=0,float y1=1)
        {
            int max=0;for(int y=(int)(y0*Size);y<(int)(y1*Size);y++)for(int x=(int)(x0*Size);x<(int)(x1*Size);x++){int i=y*Size+x;var p=a.pixels[i];var q=b.pixels[i];max=Math.Max(max,Math.Max(Math.Abs(p.r-q.r),Math.Max(Math.Abs(p.g-q.g),Math.Abs(p.b-q.b))));}return max/255.0;
        }
        static int ChangedPixels(Frame a,Frame b,int threshold)
        {int n=0;for(int i=0;i<a.pixels.Length;i++){var p=a.pixels[i];var q=b.pixels[i];if(Math.Max(Math.Abs(p.r-q.r),Math.Max(Math.Abs(p.g-q.g),Math.Abs(p.b-q.b)))>threshold)n++;}return n;}
        static int DarkenedPixels(Frame lit,Frame shadow,int threshold)
        {int n=0;for(int i=0;i<lit.pixels.Length;i++){var p=lit.pixels[i];var q=shadow.pixels[i];if((p.r+p.g+p.b-q.r-q.g-q.b)/3.0>threshold)n++;}return n;}
        static string Digest(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
    }
}
#endif
