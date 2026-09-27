#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>Controlled measurements of the actual owned camera and production
    /// materials. These diagnose current output; they do not assert art acceptance.</summary>
    public sealed class ReferenceGladeRenderMeasurementTests
    {
        [TestCase("mottle")][TestCase("shadows")][TestCase("sampling")][TestCase("soft-quality")][TestCase("mottle-strong")]
        public void MeasureActualOwnedCameraWithMatchedControl(string mode)
        {
            using (var f = new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
            {
                f.Set("FullReveal", true); f.Refresh();
                var surface = f.Get<NativeZone3DRenderSurface>("ActiveSurface"); var camera = surface.WorldCamera;
                var material = surface.MaterialFor(ReferenceGladeVoxelLibrary.Load().Material);
                var light = surface.Sun; var data = light.GetUniversalAdditionalLightData();
                float mottle = material.GetFloat("_GroundMottleStrength"); var shadows = light.shadows; var quality = data.softShadowQuality;
                var borrowed = camera.targetTexture; var global = GraphicsSettings.currentRenderPipeline;
                int version = f.Zone.EntityVersion; var positions = f.Zone.GetReadOnlyEntities().Select(f.Zone.GetEntityPosition).ToArray();
                string tiles = f.Zone.TileState.ToSaveString(); int builds = f.Get<int>("GroundBuildCount");
                string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/Verification/DensityCompletion/ReferenceGlade/Art/SeventhMeasurements", mode + "-" + Guid.NewGuid().ToString("N")));
                Directory.CreateDirectory(path);
                try
                {
                    // Freeze one aspect/projection and all native geometry for the
                    // pair. Only the owned scalar/light/temporary target differs.
                    int width = borrowed.width, height = borrowed.height;
                    if (mode == "mottle") { light.shadows = LightShadows.None; material.SetFloat("_GroundMottleStrength", 0); }
                    if (mode == "shadows" || mode == "mottle-strong") light.shadows = LightShadows.None;
                    if (mode == "soft-quality") data.softShadowQuality = SoftShadowQuality.Medium;
                    float baselineMottle = material.GetFloat("_GroundMottleStrength");
                    var baseline = Draw(camera, width, height, 1, out var baselineTarget);
                    if (mode == "mottle") material.SetFloat("_GroundMottleStrength", mottle);
                    if (mode == "mottle-strong") material.SetFloat("_GroundMottleStrength", .65f);
                    if (mode == "shadows") light.shadows = shadows;
                    if (mode == "soft-quality") data.softShadowQuality = SoftShadowQuality.High;
                    var alternative = Draw(camera, width, height, mode == "sampling" ? 2 : 1, out var alternativeTarget);
                    Save(path + "/baseline.png", baseline, width, height); Save(path + "/alternative.png", alternative, width, height);
                    float mean = baseline.Average(Luma); Assert.Greater(mean, .01f, "An empty/black render is not a measurement.");
                    var delta = baseline.Zip(alternative, (a,b) => Mathf.Abs(Luma(a)-Luma(b))).ToArray();
                    var floor = baseline.Select((c,i)=>(c,i)).Where(p => Floor(p.c) && Floor(alternative[p.i])).Select(p=>p.i).ToArray();
                    var metrics = new Metrics { mode = mode, width = width, height = height, mottleStrength = mottle,
                        renderScale = ((UniversalRenderPipelineAsset)global).renderScale, borrowedTargetSamples = borrowed.antiAliasing,
                        borrowedTargetSrgb = borrowed.sRGB, cameraMsaa = camera.allowMSAA, borrowedTargetFilter = borrowed.filterMode.ToString(),
                        baselineTarget = baselineTarget, alternativeTarget = alternativeTarget,
                        baselineMottleStrength = baselineMottle, alternativeMottleStrength = material.GetFloat("_GroundMottleStrength"),
                        exposure = material.GetFloat("_Exposure"), ambient = material.GetFloat("_AmbientStrength"), sun = material.GetFloat("_SunStrength"),
                        shadowStrength = light.shadowStrength, shadowBias = light.shadowBias, shadowNormalBias = light.shadowNormalBias,
                        shadowQuality = quality.ToString(), cameraAltitude = camera.transform.position.y, cameraPitch = camera.transform.eulerAngles.x,
                        baselineMean = mean, alternativeMean = alternative.Average(Luma), changedPixels = delta.Count(x=>x>1f/255),
                        meanAbsoluteDifference = delta.Average(), maxDifference = delta.Max(), floorPixels = floor.Length,
                        baselineFloorSpread = Spread(floor.Select(i=>Luma(baseline[i])).ToArray()), alternativeFloorSpread = Spread(floor.Select(i=>Luma(alternative[i])).ToArray()),
                        normalizedFloorDelta = floor.Length==0 ? 0 : floor.Average(i=>Mathf.Abs(Luma(baseline[i])-Luma(alternative[i]))/Mathf.Max(.001f,Luma(baseline[i]))) };
                    File.WriteAllText(path + "/metrics.json", JsonUtility.ToJson(metrics, true));
                    Assert.Greater(floor.Length, width*height/8, "The actual camera must include substantial teal ground.");
                    if (mode == "mottle") Assert.Greater(delta.Max(), 1f/255, "Actual batched floor must receive the active scalar.");
                    Assert.AreSame(borrowed, camera.targetTexture); Assert.AreSame(global, GraphicsSettings.currentRenderPipeline);
                    Assert.AreEqual(version, f.Zone.EntityVersion); Assert.AreEqual(builds, f.Get<int>("GroundBuildCount"));
                    CollectionAssert.AreEqual(positions, f.Zone.GetReadOnlyEntities().Select(f.Zone.GetEntityPosition)); Assert.AreEqual(tiles, f.Zone.TileState.ToSaveString());
                }
                finally { material.SetFloat("_GroundMottleStrength", mottle); light.shadows = shadows; data.softShadowQuality = quality; }
            }
        }
        static Color[] Draw(Camera camera, int width, int height, int factor, out TargetSettings settings)
        {
            settings = null; var previous = RenderTexture.active; var previousTarget = camera.targetTexture;
            RenderTexture high = null, output = null; Texture2D pixels = null;
            try
            {
                high = new RenderTexture(width*factor, height*factor, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
                    { filterMode = FilterMode.Bilinear, antiAliasing = 1, memorylessMode = RenderTextureMemoryless.None };
                Assert.True(high.Create());
                settings = new TargetSettings { factor=factor,renderWidth=high.width,renderHeight=high.height,outputWidth=width,outputHeight=height,
                    samples=high.antiAliasing,srgb=high.sRGB,filter=high.filterMode.ToString(),format=high.format.ToString(),depth=high.depth };
                camera.targetTexture = high;
                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = high };
                Assert.True(RenderPipeline.SupportsRenderRequest(camera, request)); RenderPipeline.SubmitRenderRequest(camera, request);
                if (factor > 1)
                {
                    output = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
                        { filterMode = FilterMode.Bilinear, antiAliasing = 1, memorylessMode = RenderTextureMemoryless.None };
                    Assert.True(output.Create()); Graphics.Blit(high, output);
                }
                RenderTexture.active = output != null ? output : high;
                pixels = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
                pixels.ReadPixels(new Rect(0,0,width,height),0,0); pixels.Apply(); return pixels.GetPixels();
            }
            finally
            {
                camera.targetTexture = previousTarget; RenderTexture.active = previous;
                if (pixels != null) Object.DestroyImmediate(pixels);
                if (output != null) { output.Release(); Object.DestroyImmediate(output); }
                if (high != null) { high.Release(); Object.DestroyImmediate(high); }
            }
        }
        static float Luma(Color c) => .2126f*c.r + .7152f*c.g + .0722f*c.b;
        // A recorded color mask for rough teal-floor statistics, not semantic
        // segmentation or a promise that every selected pixel is a bare plane.
        static bool Floor(Color c) => c.r < .13f && c.g > .08f && c.g < .32f && c.b > .07f && c.b < .31f && c.g > c.r*1.8f;
        static float Spread(float[] values) { if(values.Length==0)return 0;float mean=values.Average();return Mathf.Sqrt(values.Average(x=>(x-mean)*(x-mean))); }
        static void Save(string path, Color[] colors, int width, int height)
        {var texture=new Texture2D(width,height,TextureFormat.RGBA32,false,false);try{texture.SetPixels(colors);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());}finally{Object.DestroyImmediate(texture);}}
        [Serializable] sealed class TargetSettings { public int factor,renderWidth,renderHeight,outputWidth,outputHeight,samples,depth; public bool srgb; public string filter,format; }
        [Serializable] sealed class Metrics
        {
            public string mode, borrowedTargetFilter, shadowQuality; public int width,height,borrowedTargetSamples,changedPixels,floorPixels;
            public TargetSettings baselineTarget,alternativeTarget;
            public bool borrowedTargetSrgb,cameraMsaa; public float mottleStrength,renderScale,exposure,ambient,sun,shadowStrength,shadowBias,shadowNormalBias,cameraAltitude,cameraPitch;
            public float baselineMottleStrength,alternativeMottleStrength;
            public float baselineMean,alternativeMean,meanAbsoluteDifference,maxDifference,baselineFloorSpread,alternativeFloorSpread,normalizedFloorDelta;
        }
    }
}
#endif
