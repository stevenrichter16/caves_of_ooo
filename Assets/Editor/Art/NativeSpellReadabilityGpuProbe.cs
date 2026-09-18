#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Editor
{
    /// <summary>Actual GPU contract checks for the two spell-only readability materials.
    /// Uses owned test geometry and a preview scene; never changes the game camera,
    /// imported art, global light, world state, queues, settings or saves.</summary>
    public static class NativeSpellReadabilityGpuProbe
    {
        const int Size = 256, Layer = NativeZone3DRenderSurface.WorldLayer;
        static readonly string[] Shaders = { "Luminous", "SoftGlow" };
        static readonly string[] Common = { "self-light-dark", "split-fog", "memory", "unseen", "bounds", "missing-fog", "wrong-dimensions", "opaque-occlusion" };
        static string[] Ids => Shaders.SelectMany(s => Common.Select(c => s + ":" + c))
            .Concat(new[] { "SoftGlow:vertex-gradient", "SoftGlow:alpha-zero", "SoftGlow:no-depth-write" }).ToArray();
        [Serializable] public sealed class FrameRecord { public string name, path, sha256; public int coloredPixels; }
        [Serializable] public sealed class CaseRecord
        {
            public string id, error; public bool passed;
            public int positivePixels, negativePixels, counterPixels;
            public float centerMean, middleMean, edgeMean;
            public string[] framePaths; public FrameRecord[] frames;
        }
        [Serializable] public sealed class Report
        {
            public string status, runId, sourceSha256, error, graphicsApi, gpu, unityVersion, startedUtc, finishedUtc, reportPath;
            public int width = Size, height = Size;
            public bool activeScenePreserved, sceneDirtyFlagsPreserved, renderTextureActiveRestored, asyncCompilationRestored, ownedObjectsDisposed;
            public string[] unexpectedLogs, honestyBounds, shaderPaths; public CaseRecord[] cases;
        }
        sealed class Frame { public Color32[] Pixels; public FrameRecord Record; }
        public static bool ValidateReport(string json, string expectedRunId, string expectedSourceSha256)
        {
            try
            {
                var r = JsonUtility.FromJson<Report>(json);
                if (r == null || r.status != "PASS" || string.IsNullOrEmpty(expectedRunId) || r.runId != expectedRunId
                    || string.IsNullOrEmpty(expectedSourceSha256) || expectedSourceSha256.Length != 64 || r.sourceSha256 != expectedSourceSha256
                    || string.IsNullOrEmpty(r.graphicsApi) || r.graphicsApi == "Null" || !string.IsNullOrEmpty(r.error)
                    || r.width != Size || r.height != Size || !r.activeScenePreserved || !r.sceneDirtyFlagsPreserved
                    || !r.renderTextureActiveRestored || !r.asyncCompilationRestored || !r.ownedObjectsDisposed
                    || r.unexpectedLogs == null || r.unexpectedLogs.Length != 0
                    || !DateTime.TryParse(r.startedUtc, out var start) || !DateTime.TryParse(r.finishedUtc, out var end) || end < start
                    || r.cases == null || r.cases.Length != Ids.Length) return false;
                var ids = new HashSet<string>(Ids);
                foreach (var row in r.cases)
                    if (row == null || !ids.Remove(row.id) || !row.passed || !string.IsNullOrEmpty(row.error)
                        || row.positivePixels < 16 || row.negativePixels != 0 || row.framePaths == null
                        || row.framePaths.Length < 2 || row.framePaths.Any(string.IsNullOrWhiteSpace)) return false;
                var gradient = r.cases.Single(c => c.id == "SoftGlow:vertex-gradient");
                if (!Finite(gradient.centerMean) || !Finite(gradient.middleMean) || !Finite(gradient.edgeMean)
                    || gradient.centerMean <= gradient.middleMean + .03f || gradient.middleMean <= gradient.edgeMean + .03f
                    || gradient.edgeMean > .15f || gradient.counterPixels < 16) return false;
                if (r.cases.Single(c => c.id == "SoftGlow:no-depth-write").counterPixels < 16) return false;
                return ids.Count == 0;
            }
            catch { return false; }
        }
        public static void RunFromCommandLine()
        {
            var args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, "-spellReadabilityGpuReport");
            Run(i >= 0 && i + 1 < args.Length ? args[i + 1] : null);
        }
        public static Report Run(string reportPath = null)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Run this owned preview only after Play and compilation stop.");
            string run = Guid.NewGuid().ToString("N");
            reportPath = Path.GetFullPath(reportPath ?? Path.Combine("Docs/Verification/StarterSpell3D/Readability", "gpu-" + run, "report.json"));
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            var report = new Report { status = "RUNNING", runId = run, reportPath = reportPath, startedUtc = DateTime.UtcNow.ToString("O"),
                graphicsApi = SystemInfo.graphicsDeviceType.ToString(), gpu = SystemInfo.graphicsDeviceName, unityVersion = Application.unityVersion };
            var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray();
            var dirty = scenes.Select(s => s.isDirty).ToArray(); int active = SceneManager.GetActiveScene().handle;
            var oldTarget = RenderTexture.active; bool oldAsync = ShaderUtil.allowAsyncCompilation;
            var rows = new List<CaseRecord>(); var logs = new List<string>(); Probe probe = null;
            Application.LogCallback log = (message, stack, type) => { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) logs.Add(type + ": " + message + "\n" + stack); };
            Application.logMessageReceived += log;
            try
            {
                Require(SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null, "Actual GPU rendering is required.");
                report.shaderPaths = Shaders.Select(name => AssetDatabase.GetAssetPath(Shader.Find("CavesOfOoo/Spell3D/" + name))).ToArray();
                Require(report.shaderPaths.All(p => !string.IsNullOrEmpty(p) && File.Exists(p)), "Both production readability shaders must be imported.");
                report.sourceSha256 = SourceHash(report.shaderPaths);
                ShaderUtil.allowAsyncCompilation = false;
                probe = new Probe(Path.GetDirectoryName(reportPath));
                foreach (string shader in Shaders)
                    foreach (string kind in Common) RunCase(rows, shader + ":" + kind, row => probe.Check(shader, kind, row));
                RunCase(rows, "SoftGlow:vertex-gradient", probe.Gradient);
                RunCase(rows, "SoftGlow:alpha-zero", probe.AlphaZero);
                RunCase(rows, "SoftGlow:no-depth-write", probe.NoDepthWrite);
            }
            catch (Exception error) { report.error = error.ToString(); }
            finally
            {
                try { probe?.Dispose(); } catch (Exception error) { report.error = (report.error ?? "") + "\nCleanup: " + error; }
                RenderTexture.active = oldTarget; ShaderUtil.allowAsyncCompilation = oldAsync;
                Application.logMessageReceived -= log;
                report.activeScenePreserved = SceneManager.GetActiveScene().handle == active;
                report.sceneDirtyFlagsPreserved = scenes.Select((s, i) => s.IsValid() && s.isDirty == dirty[i]).All(v => v);
                report.renderTextureActiveRestored = RenderTexture.active == oldTarget;
                report.asyncCompilationRestored = ShaderUtil.allowAsyncCompilation == oldAsync;
                report.ownedObjectsDisposed = probe == null || probe.Clean;
                report.cases = rows.ToArray(); report.unexpectedLogs = logs.ToArray(); report.finishedUtc = DateTime.UtcNow.ToString("O");
                report.honestyBounds = new[] {
                    "Actual URP draw/readback of the two production spell shaders on owned quad/fan fixtures at physical XZ cells. No gameplay commands, user cameras, imported mesh mutations or saves.",
                    "Dark positive controls keep fog alpha visible while RGB is black and ordinary ambient/sun terms are zero; emission-zero changes only that material parameter.",
                    "Split masks, memory/unseen alpha, physical out-of-world positions and missing/wrong-size textures verify fail-closed per-fragment visibility.",
                    "SoftGlow gradient is interpolated authored vertex alpha, paired with identical geometry whose alpha is flattened to one. BaseColor-alpha zero separately suppresses it.",
                    "Depth-write test deliberately renders invisible glow before an opaque blue rear plane, paired with a known opaque front-depth writer; this forces a detectable depth-state result.",
                    "These material contracts do not establish actual spell composition, occupied-cell placement, native player inputs, motion readability or performance. Existing imported-art and native gameplay probes remain separate gates." };
                report.status = "PASS";
                if (!ValidateReport(JsonUtility.ToJson(report), run, report.sourceSha256)) report.status = "FAIL";
                File.WriteAllText(reportPath, JsonUtility.ToJson(report, true) + "\n");
            }
            if (report.status != "PASS") throw new InvalidOperationException("Readability GPU probe failed; inspect " + reportPath);
            Debug.Log("[NativeSpellReadabilityGpu] 19/19 physical shader cases PASS: " + reportPath); return report;
        }
        static void RunCase(List<CaseRecord> rows, string id, Action<CaseRecord> action)
        { var row = new CaseRecord { id = id }; try { action(row); row.passed = true; } catch (Exception e) { row.error = e.ToString(); } rows.Add(row); }
        sealed class Probe : IDisposable
        {
            readonly List<Object> owned = new List<Object>(); readonly string folder;
            Scene scene; GameObject root, subject, blocker; Camera camera; RenderTexture output;
            Mesh quad, fan; Texture2D fog, wrongFog; Material luminous, glow, occluder;
            static readonly Vector3 Center = new Vector3(40, 0, 12.5f);
            static readonly Vector4 Tint = new Vector4(.18f, .78f, .36f, 1);
            public bool Clean => !scene.IsValid() && owned.Count == 0;
            public Probe(string folder)
            {
                this.folder = folder;
                try
                {
                    var village = Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath);
                    Require(village != null, "Actual native URP renderer library required.");
                    scene = EditorSceneManager.NewPreviewScene(); root = Make("Owned readability GPU probe");
                    camera = Make("Owned top-down contract camera").AddComponent<Camera>();
                    camera.enabled = false; camera.orthographic = true; camera.orthographicSize = 1.2f; camera.aspect = 1;
                    camera.nearClipPlane = .1f; camera.farClipPlane = 30; camera.cullingMask = 1 << Layer;
                    camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
                    camera.allowHDR = false; camera.allowMSAA = false; camera.useOcclusionCulling = false;
                    camera.scene = scene; camera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
                    var data = camera.GetUniversalAdditionalCameraData(); data.SetRenderer(village.RendererIndex); data.renderType = CameraRenderType.Base;
                    data.renderPostProcessing = false; data.renderShadows = false; data.requiresDepthOption = CameraOverrideOption.Off; data.requiresColorOption = CameraOverrideOption.Off;
                    output = Own(new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear) { antiAliasing = 1, filterMode = FilterMode.Point });
                    Require(output.Create(), "GPU target allocation failed."); camera.targetTexture = output;
                    fog = Texture(80, 25); wrongFog = Texture(16, 16); Fill(wrongFog, 255);
                    luminous = Material("Luminous"); glow = Material("SoftGlow"); occluder = Material("Luminous");
                    quad = Mesh(false); fan = Mesh(true);
                    subject = Draw("Owned shader fixture", quad, luminous); blocker = Draw("Owned occluder", quad, occluder);
                }
                catch { Dispose(); throw; }
            }
            T Own<T>(T value) where T : Object { owned.Add(value); value.hideFlags = HideFlags.HideAndDontSave; return value; }
            GameObject Make(string name)
            { var go = new GameObject(name) { layer = Layer, hideFlags = HideFlags.HideAndDontSave }; SceneManager.MoveGameObjectToScene(go, scene); if (root != null) go.transform.SetParent(root.transform, false); return go; }
            Texture2D Texture(int width, int height) => Own(new Texture2D(width, height, TextureFormat.RGBA32, false, true) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp });
            Material Material(string name)
            {
                var shader = Shader.Find("CavesOfOoo/Spell3D/" + name); Require(shader != null && shader.isSupported, "Missing/unsupported production shader " + name);
                Require(!ShaderUtil.GetShaderMessages(shader).Any(m => m.severity.ToString() == "Error"), "Shader compile error " + name);
                var material = Own(new Material(shader)); material.SetTexture("_BaseMap", Texture2D.whiteTexture);
                material.SetTexture("_FogLight", fog); material.SetFloat("_Transient", 1); material.SetFloat("_AmbientStrength", 0);
                material.SetFloat("_SunStrength", 0); material.SetFloat("_Exposure", 1); material.SetFloat("_Emission", .8f); material.SetVector("_BaseColor", Tint); return material;
            }
            Mesh Mesh(bool gradient)
            {
                var vertices = new[] { new Vector3(-.75f,0,-.75f), new Vector3(-.75f,0,.75f), new Vector3(.75f,0,.75f), new Vector3(.75f,0,-.75f), Vector3.zero };
                var mesh = Own(new Mesh { name = gradient ? "Owned linear vertex-alpha fan" : "Owned opaque control quad" }); mesh.vertices = vertices;
                mesh.triangles = new[] { 4,0,1,4,1,2,4,2,3,4,3,0 }; mesh.normals = Enumerable.Repeat(Vector3.up,5).ToArray(); mesh.uv = new Vector2[5];
                mesh.colors = Enumerable.Range(0,5).Select(i => new Color(1,1,1,!gradient || i == 4 ? 1 : 0)).ToArray(); mesh.RecalculateBounds(); return mesh;
            }
            GameObject Draw(string name, Mesh mesh, Material material)
            { var go = Make(name); go.AddComponent<MeshFilter>().sharedMesh = mesh; var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material; renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false; return go; }
            Material Reset(string shader)
            {
                var material = shader == "Luminous" ? luminous : glow;
                subject.SetActive(true); subject.GetComponent<MeshFilter>().sharedMesh = quad; subject.GetComponent<MeshRenderer>().sharedMaterial = material;
                subject.transform.position = Center; subject.transform.localScale = Vector3.one;
                blocker.SetActive(false); blocker.transform.position = Center + Vector3.up * .4f; blocker.transform.localScale = Vector3.one * 1.1f;
                foreach (var m in new[] { luminous, glow, occluder }) { m.renderQueue = -1; m.SetTexture("_FogLight", fog); m.SetVector("_BaseColor", Tint); m.SetFloat("_Emission", .8f); }
                Fill(fog, 255); Aim(Center); return material;
            }
            void Aim(Vector3 point) => camera.transform.SetPositionAndRotation(point + Vector3.up * 10, Quaternion.LookRotation(Vector3.down, Vector3.forward));
            public void Check(string shader, string kind, CaseRecord row)
            {
                var material = Reset(shader); var good = Snap(row, "visible_dark"); row.positivePixels = good.Record.coloredPixels;
                switch (kind)
                {
                    case "self-light-dark": material.SetFloat("_Emission", 0); row.negativePixels = Snap(row, "emission_zero").Record.coloredPixels; break;
                    case "split-fog":
                        var pixels = new Color32[80 * 25]; for (int z=0;z<25;z++) for(int x=0;x<80;x++) pixels[z*80+x]=new Color32(0,0,0,(byte)(x>=40?255:0)); fog.SetPixels32(pixels); fog.Apply(false,false);
                        var split = Snap(row,"physical_split"); row.positivePixels = Count(split.Pixels,Size/2,Size,0,Size); row.negativePixels = Count(split.Pixels,0,Size/2,0,Size); break;
                    case "memory": Fill(fog,128); row.negativePixels = Snap(row,"memory").Record.coloredPixels; break;
                    case "unseen": Fill(fog,0); row.negativePixels = Snap(row,"unseen").Record.coloredPixels; break;
                    case "missing-fog": material.SetTexture("_FogLight",null); row.negativePixels = Snap(row,"missing_binding").Record.coloredPixels; break;
                    case "wrong-dimensions": material.SetTexture("_FogLight",wrongFog); row.negativePixels = Snap(row,"16x16_binding").Record.coloredPixels; break;
                    case "bounds":
                        var positions = new[] { new Vector3(-.8f,0,12.5f),new Vector3(80.8f,0,12.5f),new Vector3(40,0,-.8f),new Vector3(40,0,25.8f) };
                        for(int i=0;i<positions.Length;i++){subject.transform.position=positions[i];Aim(positions[i]);row.negativePixels=Math.Max(row.negativePixels,Snap(row,"outside_"+i).Record.coloredPixels);} break;
                    case "opaque-occlusion": occluder.SetVector("_BaseColor",Vector4.zero); blocker.SetActive(true); row.negativePixels=Snap(row,"opaque_front").Record.coloredPixels; break;
                    default: throw new InvalidOperationException("Unknown GPU case " + kind);
                }
                Require(row.positivePixels >= 16 && row.negativePixels == 0,"Expected visible control and zero physical leakage: " + row.id);
            }
            public void Gradient(CaseRecord row)
            {
                Reset("SoftGlow"); subject.GetComponent<MeshFilter>().sharedMesh = fan;
                var frame=Snap(row,"authored_vertex_gradient"); row.positivePixels=frame.Record.coloredPixels;
                row.centerMean=Sample(frame.Pixels,Center); row.middleMean=Sample(frame.Pixels,Center+Vector3.right*.375f); row.edgeMean=Sample(frame.Pixels,Center+Vector3.right*.70f);
                subject.GetComponent<MeshFilter>().sharedMesh=quad; var flat=Snap(row,"flattened_alpha_counter"); row.counterPixels=Different(frame.Pixels,flat.Pixels);
                Require(Finite(row.centerMean)&&Finite(row.middleMean)&&Finite(row.edgeMean)&&row.centerMean>row.middleMean+.03f&&row.middleMean>row.edgeMean+.03f&&row.edgeMean<=.15f&&row.counterPixels>=16,
                    "Halo must interpolate authored vertex alpha with a detectable flat-alpha counter.");
            }
            public void AlphaZero(CaseRecord row)
            {
                var material=Reset("SoftGlow"); subject.GetComponent<MeshFilter>().sharedMesh=fan;
                row.positivePixels=Snap(row,"alpha_one").Record.coloredPixels;
                var color=Tint;color.w=0;material.SetVector("_BaseColor",color);row.negativePixels=Snap(row,"alpha_zero").Record.coloredPixels;
                Require(row.positivePixels>=16&&row.negativePixels==0,"Base alpha zero must suppress the complete glow.");
            }
            public void NoDepthWrite(CaseRecord row)
            {
                Reset("SoftGlow"); subject.SetActive(false); blocker.SetActive(true);blocker.transform.position=Center;blocker.transform.localScale=Vector3.one;
                occluder.SetVector("_BaseColor",new Vector4(.03f,.1f,.9f,1));occluder.renderQueue=2000;
                var reference=Snap(row,"opaque_rear_reference");row.positivePixels=reference.Record.coloredPixels;
                subject.SetActive(true);subject.transform.position=Center+Vector3.up*.4f;glow.renderQueue=1900;glow.SetFloat("_Emission",0);
                var observed=Snap(row,"invisible_glow_before_rear");row.negativePixels=Different(reference.Pixels,observed.Pixels);
                luminous.renderQueue=1900;luminous.SetVector("_BaseColor",Vector4.zero);subject.GetComponent<MeshRenderer>().sharedMaterial=luminous;
                var counter=Snap(row,"opaque_depth_writer_counter");row.counterPixels=Different(reference.Pixels,counter.Pixels);
                Require(row.positivePixels>=16&&row.negativePixels==0&&row.counterPixels>=16,"Glow must not write depth; actual opaque counter must occlude the later rear plane.");
            }
            float Sample(Color32[] pixels,Vector3 world)
            {
                Vector3 p=camera.WorldToViewportPoint(world);int x=(int)(p.x*Size),y=(int)(p.y*Size);float sum=0;int n=0;
                for(int j=Math.Max(0,y-2);j<Math.Min(Size,y+3);j++)for(int i=Math.Max(0,x-2);i<Math.Min(Size,x+3);i++){var c=pixels[j*Size+i];sum+=Math.Max(c.r,Math.Max(c.g,c.b))/255f;n++;}return sum/Math.Max(1,n);
            }
            Frame Snap(CaseRecord row,string name)
            {
                var request=new UniversalRenderPipeline.SingleCameraRequest{destination=output};Require(RenderPipeline.SupportsRenderRequest(camera,request),"URP render request unsupported.");
                var previous=RenderTexture.active;Texture2D read=null;
                try
                {
                    RenderTexture.active=output;GL.Clear(true,true,Color.black);RenderPipeline.SubmitRenderRequest(camera,request);RenderTexture.active=output;
                    read=new Texture2D(Size,Size,TextureFormat.RGBA32,false,true);read.ReadPixels(new Rect(0,0,Size,Size),0,0);read.Apply(false,false);
                    var pixels=read.GetPixels32();byte[] png=read.EncodeToPNG();string path=Path.Combine(folder,row.id.Replace(':','_')+"_"+name+".png");File.WriteAllBytes(path,png);
                    var frame=new FrameRecord{name=name,path=path,sha256=Hash(png),coloredPixels=Count(pixels,0,Size,0,Size)};
                    row.frames=(row.frames??Array.Empty<FrameRecord>()).Concat(new[]{frame}).ToArray();row.framePaths=row.frames.Select(f=>f.path).ToArray();return new Frame{Pixels=pixels,Record=frame};
                }
                finally{RenderTexture.active=previous;if(read!=null)Object.DestroyImmediate(read);}
            }
            public void Dispose()
            {
                if(scene.IsValid()){EditorSceneManager.ClosePreviewScene(scene);scene=default;}
                for(int i=owned.Count-1;i>=0;i--)if(owned[i]!=null){if(owned[i] is RenderTexture rt)rt.Release();Object.DestroyImmediate(owned[i]);}owned.Clear();
            }
        }
        static void Fill(Texture2D texture,byte alpha){var pixels=Enumerable.Repeat(new Color32(0,0,0,alpha),texture.width*texture.height).ToArray();texture.SetPixels32(pixels);texture.Apply(false,false);}
        static int Count(Color32[] pixels,int x0,int x1,int y0,int y1){int n=0;for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++){var p=pixels[y*Size+x];if(p.r>2||p.g>2||p.b>2)n++;}return n;}
        static int Different(Color32[] a,Color32[] b){int n=0;for(int i=0;i<a.Length;i++)if(Math.Max(Math.Abs(a[i].r-b[i].r),Math.Max(Math.Abs(a[i].g-b[i].g),Math.Abs(a[i].b-b[i].b)))>1)n++;return n;}
        static string SourceHash(string[] shaders){var files=shaders.Concat(new[]{"Assets/Art3D/Village/Shaders/Village3DCommon.hlsl"});return Hash(Encoding.UTF8.GetBytes(string.Join("\n",files.Select(p=>p+"\n"+Hash(File.ReadAllBytes(p))))));}
        static string Hash(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
        static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value)&&value>=0;
        static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    }
}
#endif
