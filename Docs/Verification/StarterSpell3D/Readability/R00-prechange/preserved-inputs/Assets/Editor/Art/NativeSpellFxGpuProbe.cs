#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CavesOfOoo.Core;
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
    /// <summary>Explicit GPU proof over imported spell meshes and the actual native backend.
    /// Owns a preview scene, cameras, readbacks and material copies; issues no gameplay commands.</summary>
    public static class NativeSpellFxGpuProbe
    {
        const int Size = 512, Layer = NativeZone3DRenderSurface.WorldLayer;
        static readonly string[] Spells = { "Pyromancy_EmberSpit", "Pyromancy_FlamingHands", "Hydromancy_JetBlast",
            "Galvanism_GroundSurge", "Cryomancy_RimeGrip", "Spellcraft_Calm", "Hydromancy_ConjureRain" };
        static readonly Color32 Visible = new Color32(255,255,255,255), Memory = new Color32(120,120,120,128), Unseen = new Color32(0,0,0,0);
        [Serializable] public sealed class FrameRecord { public string name, path, sha256; public int coloredPixels; }
        [Serializable] public sealed class FaceRecord
        {
            public string mesh, importedGeometrySha256; public int triangleIndex, a, b, c, positivePixels, negativePixels;
            public Vector3 importedOutwardNormal; public Quaternion displayRotation; public float displayScale;
        }
        [Serializable] public sealed class ColorUpload
        {
            public string piece;
            public Vector4 importedLinear, initialVector, retainedVector, freshVector;
            public Color initialColor, retainedColor, freshColor;
        }
        [Serializable] public sealed class CaseRecord
        {
            public string id, error; public bool passed; public int positivePixels, negativePixels, nativeMeshCount;
            public float nativeContactSeconds, sampledStudyFrame;
            public int colorSamples, doubleLinearDifferentPixels;
            public float meanNativeReferenceDifference, meanDoubleLinearDifference;
            public float maximumInitialVectorError, maximumRetainedVectorError, maximumFreshVectorError;
            public ColorUpload[] colorUploads;
            public string[] framePaths; public FrameRecord[] frames; public FaceRecord[] faces;
        }
        [Serializable] public sealed class Report
        {
            public string status, runId, sourceSha256, error, graphicsApi, gpu, unityVersion, startedUtc, finishedUtc, reportPath;
            public int width = Size, height = Size;
            public bool activeScenePreserved, sceneDirtyFlagsPreserved, renderTextureActiveRestored, asyncCompilationRestored, settingsRestored;
            public string[] unexpectedLogs, honestyBounds; public CaseRecord[] cases;
        }
        sealed class Frame { public Color32[] Pixels; public FrameRecord Record; }

        /// <summary>Pure receipt acceptance: a matching run/source, all seven paired cases,
        /// functioning positive controls, no negative leakage, and preserved borrowed state.</summary>
        public static bool ValidateReport(string json, string expectedRunId, string expectedSourceSha256)
        {
            try
            {
                var r = JsonUtility.FromJson<Report>(json);
                if (r == null || r.status != "PASS" || string.IsNullOrEmpty(expectedRunId) || r.runId != expectedRunId
                    || string.IsNullOrEmpty(expectedSourceSha256) || expectedSourceSha256.Length != 64 || r.sourceSha256 != expectedSourceSha256
                    || string.IsNullOrEmpty(r.graphicsApi) || r.graphicsApi == "Null" || !string.IsNullOrEmpty(r.error)
                    || !r.activeScenePreserved || !r.sceneDirtyFlagsPreserved || !r.renderTextureActiveRestored
                    || !r.asyncCompilationRestored || !r.settingsRestored || r.unexpectedLogs == null || r.unexpectedLogs.Length != 0
                    || !DateTime.TryParse(r.startedUtc, out var start) || !DateTime.TryParse(r.finishedUtc, out var finish) || finish < start
                    || r.cases == null || r.cases.Length != Spells.Length * 3) return false;
                var expected = new HashSet<string>(Spells.SelectMany(s => new[] { s + ":face", s + ":assembled", s + ":color" }));
                foreach (var c in r.cases)
                    if (c == null || !expected.Remove(c.id) || !c.passed || !string.IsNullOrEmpty(c.error)
                        || c.positivePixels < 16 || c.negativePixels != 0 || c.framePaths == null || c.framePaths.Length < 2
                        || c.framePaths.Any(string.IsNullOrWhiteSpace)) return false;
                foreach (var c in r.cases.Where(c => c.id.EndsWith(":color",StringComparison.Ordinal)))
                    if (c.colorSamples < 16 || c.doubleLinearDifferentPixels < 16 || c.framePaths.Length < 3
                        || !Finite(c.meanNativeReferenceDifference) || c.meanNativeReferenceDifference > 1f/255
                        || !Finite(c.meanDoubleLinearDifference) || c.meanDoubleLinearDifference <= 1f/255) return false;
                return expected.Count == 0;
            }
            catch { return false; }
        }

        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0;

        public static void RunFromCommandLine()
        {
            var args = Environment.GetCommandLineArgs(); int i = Array.IndexOf(args, "-spellFxGpuReport");
            Run(i >= 0 && i + 1 < args.Length ? args[i + 1] : null);
        }
        public static Report Run(string reportPath = null)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Run the standalone spell GPU probe after Play and compilation stop.");
            string run = Guid.NewGuid().ToString("N");
            reportPath = Path.GetFullPath(reportPath ?? Path.Combine("Docs/Verification/StarterSpell3D/Integration", "gpu-" + run, "report.json"));
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            var report = new Report { status = "RUNNING", runId = run, reportPath = reportPath, startedUtc = DateTime.UtcNow.ToString("O"),
                graphicsApi = SystemInfo.graphicsDeviceType.ToString(), gpu = SystemInfo.graphicsDeviceName, unityVersion = Application.unityVersion };
            var rows = new List<CaseRecord>(); var logs = new List<string>();
            int active = SceneManager.GetActiveScene().handle;
            var scenes = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToArray();
            var dirty = scenes.Select(s => s.isDirty).ToArray(); var previousRT = RenderTexture.active;
            bool previousAsync = ShaderUtil.allowAsyncCompilation;
            var previousMode = SpellFxSettings.Mode; float previousSpeed = SpellFxSettings.AnimationSpeed, previousFlash = SpellFxSettings.FlashIntensity;
            Application.LogCallback observe = (message, stack, type) => { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) logs.Add(type + ": " + message + "\n" + stack); };
            Application.logMessageReceived += observe;
            Probe probe = null;
            try
            {
                report.sourceSha256 = Hash(File.ReadAllBytes("ArtSource/StarterSpell3D/runtime/starter_spell_library.json"));
                if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) throw new InvalidOperationException("An actual graphics device is required; a null GPU cannot prove face culling.");
                ShaderUtil.allowAsyncCompilation = false; SpellFxSettings.Mode = SpellFxMode.Full; SpellFxSettings.AnimationSpeed = 1; SpellFxSettings.FlashIntensity = 0;
                probe = new Probe(Path.GetDirectoryName(reportPath));
                foreach (string spell in Spells)
                {
                    var entry = probe.Library.Find(spell);
                    RunCase(rows, spell + ":face", c => probe.FaceAtlas(entry, c));
                    RunCase(rows, spell + ":assembled", c => probe.Assembled(entry, c));
                    RunCase(rows, spell + ":color", c => probe.LinearColor(entry, c));
                }
            }
            catch (Exception error) { report.error = error.ToString(); }
            finally
            {
                try { probe?.Dispose(); } catch (Exception error) { report.error = (report.error ?? "") + "\nCleanup: " + error; }
                RenderTexture.active = previousRT; ShaderUtil.allowAsyncCompilation = previousAsync;
                SpellFxSettings.Mode = previousMode; SpellFxSettings.AnimationSpeed = previousSpeed; SpellFxSettings.FlashIntensity = previousFlash;
                Application.logMessageReceived -= observe;
                report.activeScenePreserved = SceneManager.GetActiveScene().handle == active;
                report.sceneDirtyFlagsPreserved = scenes.Select((s,i) => s.IsValid() && s.isDirty == dirty[i]).All(v => v);
                report.renderTextureActiveRestored = RenderTexture.active == previousRT; report.asyncCompilationRestored = ShaderUtil.allowAsyncCompilation == previousAsync;
                report.settingsRestored = SpellFxSettings.Mode == previousMode && SpellFxSettings.AnimationSpeed == previousSpeed && SpellFxSettings.FlashIntensity == previousFlash;
                report.cases = rows.ToArray(); report.unexpectedLogs = logs.ToArray(); report.finishedUtc = DateTime.UtcNow.ToString("O");
                report.honestyBounds = new[] {
                    "Actual imported triangles/normals and Palette shader rendered by URP on the reported GPU; no triangle reversal, normal recalculation or shared asset mutation.",
                    "Face sheets use one largest nondegenerate face per distinct imported mesh, rotated uniformly so its imported outward normal faces the camera; the countercamera views the opposite side. Per-face signal is required.",
                    "Assembled frames use NativeSpellFxRenderer with copied synthetic valid outcomes in an isolated zone at actual contact time. They do not prove real player input, rig gestures, every outcome variant or gameplay feel.",
                    "Color cases compare the same imported meshes, contact pose, camera and Palette shader against a raw-vector single-linear reference and deliberate double-linear counter. They verify actual output parity without changing the approved asset swatches.",
                    "Memory/unseen counters keep the geometry enabled and change only an owned 80x25 fog texture; runtime-hidden additionally disables native visibility. No world queues, saves or gameplay commands are touched." };
                report.status = "PASS";
                if (!ValidateReport(JsonUtility.ToJson(report), run, report.sourceSha256)) report.status = "FAIL";
                File.WriteAllText(reportPath, JsonUtility.ToJson(report, true) + "\n");
            }
            if (report.status != "PASS") throw new InvalidOperationException("Native spell GPU acceptance failed; inspect " + reportPath);
            Debug.Log("[NativeSpellGpu] 21/21 paired cases PASS: " + reportPath); return report;
        }
        static void RunCase(List<CaseRecord> rows, string id, Action<CaseRecord> run)
        {
            var row = new CaseRecord { id = id };
            try { run(row); row.passed = true; } catch (Exception error) { row.error = error.ToString(); }
            rows.Add(row);
        }

        sealed class Probe : IDisposable
        {
            public NativeSpellFxLibrary Library;
            readonly List<Object> owned = new List<Object>(); readonly string folder;
            Scene scene; GameObject root, atlas; Camera camera, source; RenderTexture output;
            Texture2D faceFog; Material faceMaterial; NativeZone3DRenderSurface surface; NativeSpellFxRenderer fx; Zone zone;
            public Probe(string folder)
            {
                this.folder = folder;
                try
                {
                    var village = Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath);
                    Library = Resources.Load<NativeSpellFxLibrary>(NativeSpellFxLibrary.ResourcePath);
                    Require(village != null && Library != null, "Imported native village and spell libraries are required."); village.Validate(); Library.Validate();
                    Require(Library.Material.shader.isSupported && !ShaderUtil.GetShaderMessages(Library.Material.shader).Any(m => m.severity.ToString() == "Error"), "Actual Palette shader failed GPU import.");
                    scene = EditorSceneManager.NewPreviewScene(); root = Make("Owned native spell GPU preview");
                    camera = Make("Face atlas camera").AddComponent<Camera>(); Configure(camera, village.RendererIndex);
                    source = Make("Owned XY framing camera").AddComponent<Camera>(); source.enabled = false; source.orthographic = true;
                    source.orthographicSize = 3.5f; source.aspect = 1; source.transform.position = new Vector3(40.5f,12.5f,-10);
                    output = Own(new RenderTexture(Size,Size,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear) { antiAliasing = 1, filterMode = FilterMode.Point }); Require(output.Create(), "GPU readback target allocation failed.");
                    source.targetTexture = output;
                    surface = new NativeZone3DRenderSurface(root.transform, village.Renderer, village.RendererIndex, village.CompositeMaterial,
                        new[] { village.WorldMaterial, village.WaterMaterial }, 2.2f);
                    surface.Sync(source,true,false); surface.WorldCamera.enabled = false;
                    surface.WorldCamera.scene = scene; surface.WorldCamera.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
                    zone = new Zone("IsolatedSpellGpuProbe"); foreach (var cell in zone.Cells) { cell.Explored = true; cell.IsVisible = true; }
                    Fill(surface.FogTexture,Visible);
                    fx = new NativeSpellFxRenderer(Library); fx.SetZone(zone); fx.SetSurface(surface);
                    faceFog = Own(new Texture2D(80,25,TextureFormat.RGBA32,false,true) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp }); Fill(faceFog,Visible);
                    faceMaterial = Own(new Material(Library.Material)); faceMaterial.SetTexture("_FogLight",faceFog); faceMaterial.SetTexture("_BaseMap",Texture2D.whiteTexture);
                    faceMaterial.SetFloat("_Transient",1); faceMaterial.SetFloat("_AmbientStrength",1); faceMaterial.SetFloat("_SunStrength",0); faceMaterial.SetFloat("_Exposure",1); faceMaterial.SetColor("_BaseColor",Color.white);
                }
                catch { Dispose(); throw; }
            }
            T Own<T>(T value) where T : Object { owned.Add(value); value.hideFlags = HideFlags.HideAndDontSave; return value; }
            GameObject Make(string name)
            {
                var go = new GameObject(name) { layer = Layer, hideFlags = HideFlags.HideAndDontSave };
                SceneManager.MoveGameObjectToScene(go,scene); if (root != null) go.transform.SetParent(root.transform,false); return go;
            }
            void Configure(Camera value, int rendererIndex)
            {
                value.enabled = false; value.orthographic = true; value.aspect = 1; value.nearClipPlane = .1f; value.farClipPlane = 100;
                value.cullingMask = 1 << Layer; value.clearFlags = CameraClearFlags.SolidColor; value.backgroundColor = Color.black;
                value.allowHDR = false; value.allowMSAA = false; value.useOcclusionCulling = false; value.scene = scene;
                value.overrideSceneCullingMask = EditorSceneManager.GetSceneCullingMask(scene);
                var data = value.GetUniversalAdditionalCameraData(); data.SetRenderer(rendererIndex); data.renderType = CameraRenderType.Base;
                data.renderPostProcessing = false; data.renderShadows = false; data.requiresDepthOption = CameraOverrideOption.Off; data.requiresColorOption = CameraOverrideOption.Off;
            }
            public void FaceAtlas(NativeSpellFxEntry entry, CaseRecord row)
            {
                Require(entry != null,"Missing spell entry."); fx.ClearAll(); if (atlas != null) Object.DestroyImmediate(atlas); atlas = Make("Imported triangle atlas");
                var meshes = entry.Pieces.Select(p => p.Mesh).Distinct().ToArray(); int columns = Mathf.CeilToInt(Mathf.Sqrt(meshes.Length)), rows = Mathf.CeilToInt(meshes.Length/(float)columns);
                var center = new Vector3(40,4,12.5f); camera.orthographicSize = Math.Max(columns,rows)*.5f+.25f;
                camera.transform.SetPositionAndRotation(center + Vector3.back*10,Quaternion.identity);
                var records = new List<FaceRecord>(); var centers = new List<Vector3>();
                for (int m = 0; m < meshes.Length; m++)
                {
                    var mesh = meshes[m]; var vertices = mesh.vertices; var normals = mesh.normals; var indices = mesh.triangles;
                    float area = 0; int best = -1;
                    for (int i = 0; i < indices.Length; i += 3)
                    {
                        float candidate = Vector3.Cross(vertices[indices[i+1]]-vertices[indices[i]],vertices[indices[i+2]]-vertices[indices[i]]).sqrMagnitude;
                        if (candidate > area) { area = candidate; best = i; }
                    }
                    Require(best >= 0 && normals.Length == vertices.Length,"No nondegenerate imported triangle with normals: " + mesh.name);
                    int a = indices[best], b = indices[best+1], c = indices[best+2];
                    Vector3 normal = (normals[a]+normals[b]+normals[c]).normalized;
                    Require(normal.sqrMagnitude > .9f,"Missing outward normal: " + mesh.name);
                    Vector3 centroid = (vertices[a]+vertices[b]+vertices[c])/3;
                    float radius = Math.Max(Vector3.Distance(vertices[a],centroid),Math.Max(Vector3.Distance(vertices[b],centroid),Vector3.Distance(vertices[c],centroid)));
                    float scale = .4f/radius; Quaternion rotation = Quaternion.FromToRotation(normal,Vector3.back);
                    Vector3 at = center + new Vector3(m%columns-(columns-1)*.5f,m/columns-(rows-1)*.5f,0); centers.Add(at);
                    var triangle = Own(new Mesh { name = "Unchanged imported face " + mesh.name }); triangle.vertices = vertices; triangle.normals = normals;
                    triangle.triangles = new[] { a,b,c }; triangle.RecalculateBounds();
                    var go = Make(mesh.name); go.transform.SetParent(atlas.transform,false); go.transform.SetPositionAndRotation(at-rotation*(centroid*scale),rotation); go.transform.localScale = Vector3.one*scale;
                    go.AddComponent<MeshFilter>().sharedMesh = triangle; var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = faceMaterial;
                    renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                    records.Add(new FaceRecord { mesh = mesh.name, importedGeometrySha256 = GeometryHash(mesh), triangleIndex = best/3, a=a,b=b,c=c,
                        importedOutwardNormal = normal, displayRotation = rotation, displayScale = scale });
                }
                var front = Snap(row,"front",camera);
                for (int i = 0; i < records.Count; i++) records[i].positivePixels = CountPanel(front.Pixels,camera,centers[i]);
                camera.transform.SetPositionAndRotation(center+Vector3.forward*10,Quaternion.Euler(0,180,0));
                var back = Snap(row,"back",camera);
                for (int i = 0; i < records.Count; i++) records[i].negativePixels = CountPanel(back.Pixels,camera,centers[i]);
                row.faces = records.ToArray(); row.positivePixels = front.Record.coloredPixels; row.negativePixels = back.Record.coloredPixels;
                Require(records.Count == meshes.Length && records.All(r => r.positivePixels >= 4 && r.negativePixels == 0),"Every imported mesh needs a visible outward face and a blank inward control; inspect per-face counts.");
                Require(row.positivePixels >= 16 && row.negativePixels == 0,"Atlas face-culling positive/negative controls failed.");
            }
            public void Assembled(NativeSpellFxEntry entry, CaseRecord row)
            {
                if (atlas != null) atlas.SetActive(false); fx.ClearAll();
                foreach (var cell in zone.Cells) { cell.Explored = true; cell.IsVisible = true; } Fill(surface.FogTexture,Visible);
                var sequence = Sequence(entry.SpellId); float duration = fx.Play(sequence); Require(duration > 0,"Native backend rejected the imported spell: " + fx.Failure);
                float contact = entry.ReleaseFrame/entry.SampleRate + (entry.StudyContactFrame > entry.ReleaseFrame ? Mathf.Min(.65f,sequence.Path.Count*.025f) : 0);
                fx.Update(contact); row.nativeContactSeconds = contact; row.sampledStudyFrame = entry.StudyContactFrame; row.nativeMeshCount = fx.ActiveMeshCount;
                Require(row.nativeMeshCount > 0,"No actual native mesh at the copied contact time.");
                var visible = Snap(row,"contact_visible",surface.WorldCamera); row.positivePixels = visible.Record.coloredPixels;
                Fill(surface.FogTexture,Memory); var memory = Snap(row,"contact_memory_gpu",surface.WorldCamera);
                Fill(surface.FogTexture,Unseen); var unseen = Snap(row,"contact_unseen_gpu",surface.WorldCamera);
                Fill(surface.FogTexture,Visible); foreach (var cell in zone.Cells) cell.IsVisible = false; fx.Update(0);
                var hidden = Snap(row,"contact_runtime_hidden",surface.WorldCamera);
                row.negativePixels = Math.Max(memory.Record.coloredPixels,Math.Max(unseen.Record.coloredPixels,hidden.Record.coloredPixels));
                Require(row.positivePixels >= 16 && row.negativePixels == 0 && fx.ActiveMeshCount == 0,"Assembled contact must render visibly and fail closed for memory, unseen and runtime-hidden controls.");
            }
            public void LinearColor(NativeSpellFxEntry entry, CaseRecord row)
            {
                Require(QualitySettings.activeColorSpace == ColorSpace.Linear,"Color calibration requires the project's actual Linear color space.");
                if (atlas != null) atlas.SetActive(false); fx.ClearAll();
                foreach (var cell in zone.Cells) { cell.Explored = true; cell.IsVisible = true; } Fill(surface.FogTexture,Visible);
                var sequence = Sequence(entry.SpellId); Require(fx.Play(sequence)>0,"Native color fixture must accept the real entry.");
                float contact = entry.ReleaseFrame/entry.SampleRate + (entry.StudyContactFrame > entry.ReleaseFrame ? Mathf.Min(.65f,sequence.Path.Count*.025f) : 0);
                fx.Update(contact);row.nativeContactSeconds=contact;row.sampledStudyFrame=entry.StudyContactFrame;row.nativeMeshCount=fx.ActiveMeshCount;
                Require(row.nativeMeshCount>0,"Native color fixture must have actual visible geometry.");
                var actual = Snap(row,"native_upload",surface.WorldCamera);
                var views=fx.Root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();
                var block=new MaterialPropertyBlock();int id=Shader.PropertyToID("_BaseColor");
                var uploads=new List<ColorUpload>();
                foreach(var view in views)
                {
                    var mesh=view.GetComponent<MeshFilter>().sharedMesh;var piece=entry.Pieces.Single(p=>p.Mesh==mesh);
                    view.GetPropertyBlock(block);
                    var upload=new ColorUpload{piece=piece.Id,importedLinear=piece.Color,initialVector=block.GetVector(id),initialColor=block.GetColor(id)};
                    block.SetVector(id,(Vector4)piece.Color);view.SetPropertyBlock(block);view.GetPropertyBlock(block);
                    upload.retainedVector=block.GetVector(id);upload.retainedColor=block.GetColor(id);uploads.Add(upload);
                }
                // Diagnostic: overwriting an existing Color-typed entry may retain
                // its conversion flag. It is not an independent vector reference.
                Snap(row,"retained_block_vector",surface.WorldCamera);
                for(int i=0;i<views.Length;i++)
                {
                    var fresh=new MaterialPropertyBlock();fresh.SetVector(id,uploads[i].importedLinear);
                    views[i].SetPropertyBlock(fresh);views[i].GetPropertyBlock(block);
                    uploads[i].freshVector=block.GetVector(id);uploads[i].freshColor=block.GetColor(id);
                }
                row.colorUploads=uploads.ToArray();
                row.maximumInitialVectorError=uploads.Max(u=>Vector4.Distance(u.initialVector,u.importedLinear));
                row.maximumRetainedVectorError=uploads.Max(u=>Vector4.Distance(u.retainedVector,u.importedLinear));
                row.maximumFreshVectorError=uploads.Max(u=>Vector4.Distance(u.freshVector,u.importedLinear));
                var reference=Snap(row,"single_linear_reference",surface.WorldCamera);
                for(int i=0;i<views.Length;i++)
                {
                    var counter=new MaterialPropertyBlock();counter.SetVector(id,(Vector4)((Color)uploads[i].importedLinear).linear);
                    views[i].SetPropertyBlock(counter);
                }
                var wrong=Snap(row,"double_linear_counter",surface.WorldCamera);
                double actualDifference=0,wrongDifference=0;int mismatches=0;
                for(int i=0;i<reference.Pixels.Length;i++)
                {
                    var expected=reference.Pixels[i];if(expected.r<=2&&expected.g<=2&&expected.b<=2)continue;
                    row.colorSamples++;var observed=actual.Pixels[i];var counter=wrong.Pixels[i];
                    int dr=Math.Abs(observed.r-expected.r),dg=Math.Abs(observed.g-expected.g),db=Math.Abs(observed.b-expected.b);
                    int wr=Math.Abs(counter.r-expected.r),wg=Math.Abs(counter.g-expected.g),wb=Math.Abs(counter.b-expected.b);
                    if(Math.Max(dr,Math.Max(dg,db))>1)mismatches++;
                    if(Math.Max(wr,Math.Max(wg,wb))>1)row.doubleLinearDifferentPixels++;
                    actualDifference+=dr+dg+db;wrongDifference+=wr+wg+wb;
                }
                row.positivePixels=row.colorSamples;row.negativePixels=mismatches;
                row.meanNativeReferenceDifference=(float)(actualDifference/Math.Max(1,row.colorSamples)/765);
                row.meanDoubleLinearDifference=(float)(wrongDifference/Math.Max(1,row.colorSamples)/765);
                Require(row.colorSamples>=16&&row.doubleLinearDifferentPixels>=16&&row.meanDoubleLinearDifference>1f/255,
                    "Color reference must be visible and discriminate the deliberate double-linear counter.");
                Require(mismatches==0&&row.meanNativeReferenceDifference<=1f/255,
                    "Native shader output must match the single-linear reference at the same contact pose; double conversion darkens the imported swatches.");
            }
            SpellFxSequence Sequence(string spell)
            {
                var start = new Point(38,12); var path = Enumerable.Range(39,4).Select(x => new Point(x,12)).ToArray();
                var cells = new[] { new Point(42,12) }; var target = cells[0]; var applied = Array.Empty<string>();
                if (spell == "Pyromancy_FlamingHands") { path = Array.Empty<Point>(); target = new Point(39,12); cells = new[] { target }; }
                else if (spell == "Hydromancy_JetBlast") { path = path.Take(2).ToArray(); target = new Point(40,12); cells = new[] { new Point(39,12),new Point(40,11),target,new Point(40,13) }; applied = new[] { "WetEffect" }; }
                else if (spell == "Hydromancy_ConjureRain") { path = Array.Empty<Point>(); target = new Point(41,12); cells = new[] { target }; }
                else if (spell == "Cryomancy_RimeGrip") applied = new[] { "FrozenEffect" };
                else if (spell == "Spellcraft_Calm") applied = new[] { "Pacified" };
                else if (spell == "Galvanism_GroundSurge") applied = new[] { "ElectrifiedEffect" };
                return new SpellFxSequence(spell,zone,null,start,path,cells,new[] { new SpellFxTargetResult("isolated-contact",target,target,appliedEffects:applied) });
            }
            Frame Snap(CaseRecord row, string name, Camera from)
            {
                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = output };
                Require(RenderPipeline.SupportsRenderRequest(from,request),"URP SingleCameraRequest is unavailable.");
                var prior = RenderTexture.active; Texture2D read = null;
                try
                {
                    RenderTexture.active = output; GL.Clear(true,true,Color.black); RenderPipeline.SubmitRenderRequest(from,request);
                    RenderTexture.active = output; read = new Texture2D(Size,Size,TextureFormat.RGBA32,false,true); read.ReadPixels(new Rect(0,0,Size,Size),0,0); read.Apply(false,false);
                    var pixels = read.GetPixels32(); byte[] png = read.EncodeToPNG(); string file = Path.Combine(folder,row.id.Replace(':','_')+"_"+name+".png"); File.WriteAllBytes(file,png);
                    var record = new FrameRecord { name=name,path=file,sha256=Hash(png),coloredPixels=CountPixels(pixels,0,Size,0,Size) };
                    row.frames = (row.frames ?? Array.Empty<FrameRecord>()).Concat(new[] { record }).ToArray(); row.framePaths = row.frames.Select(f => f.path).ToArray();
                    return new Frame { Pixels=pixels,Record=record };
                }
                finally { RenderTexture.active = prior; if (read != null) Object.DestroyImmediate(read); }
            }
            static int CountPanel(Color32[] pixels, Camera camera, Vector3 center)
            {
                var viewport = camera.WorldToViewportPoint(center); float half = .48f/(2*camera.orthographicSize);
                return CountPixels(pixels,Mathf.Clamp((int)((viewport.x-half)*Size),0,Size),Mathf.Clamp((int)((viewport.x+half)*Size),0,Size),
                    Mathf.Clamp((int)((viewport.y-half)*Size),0,Size),Mathf.Clamp((int)((viewport.y+half)*Size),0,Size));
            }
            public void Dispose()
            {
                fx?.Dispose(); fx = null; surface?.Dispose(); surface = null;
                if (scene.IsValid()) { EditorSceneManager.ClosePreviewScene(scene); scene = default; }
                for (int i = owned.Count-1; i >= 0; i--) if (owned[i] != null) { if (owned[i] is RenderTexture rt) rt.Release(); Object.DestroyImmediate(owned[i]); }
                owned.Clear();
            }
        }
        static void Fill(Texture2D texture, Color32 color) { var pixels = new Color32[texture.width*texture.height]; for (int i=0;i<pixels.Length;i++) pixels[i]=color; texture.SetPixels32(pixels); texture.Apply(false,false); }
        static int CountPixels(Color32[] pixels, int x0, int x1, int y0, int y1) { int count=0; for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++){var p=pixels[y*Size+x];if(p.r>2||p.g>2||p.b>2)count++;}return count; }
        static string GeometryHash(Mesh mesh)
        {
            using (var memory = new MemoryStream()) using (var writer = new BinaryWriter(memory))
            { foreach(var v in mesh.vertices){writer.Write(v.x);writer.Write(v.y);writer.Write(v.z);}foreach(var n in mesh.normals){writer.Write(n.x);writer.Write(n.y);writer.Write(n.z);}foreach(int i in mesh.triangles)writer.Write(i);writer.Flush();return Hash(memory.ToArray()); }
        }
        static string Hash(byte[] bytes) { using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant(); }
        static void Require(bool condition, string error) { if(!condition)throw new InvalidOperationException(error); }
    }
}
#endif
