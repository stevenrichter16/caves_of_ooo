using System;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object=UnityEngine.Object;
namespace CavesOfOoo.Tests
{
 public sealed class WorldAffordancePixelTests
 {
  // Actual native diagnostic a00c749f measured this map viewport and zoom.
  // Test only two short corners in an owned black preview; live terrain contrast
  // and readability remain the separately reviewed same-camera screenshots.
  [TestCase(0f)][TestCase(.37f)]
  public void BothCurrentCornersHaveReadablePixelAreaAndClearToNoPixels(float pixelOffset)
  {
   using(var core=new WorldAffordanceQueryTests.Fixture())
   {
    var scene=EditorSceneManager.NewPreviewScene();GameObject root=null;WorldAffordanceRenderer marker=null;RenderTexture target=null;Texture2D readback=null;var oldActive=RenderTexture.active;
    try
    {
     var row=core.Row();root=new GameObject("Owned cue pixel fixture");SceneManager.MoveGameObjectToScene(root,scene);root.AddComponent<Grid>();
     var tiles=new GameObject("owned tilemap");tiles.transform.SetParent(root.transform,false);var map=tiles.AddComponent<Tilemap>();
     marker=new WorldAffordanceRenderer(root.transform,map,31);marker.Refresh(core.Player,core.Zone,(WorldAffordance)core.Find());Assert.True(marker.IsVisible);
     var go=new GameObject("owned cue pixel camera");go.transform.SetParent(root.transform,false);var camera=go.AddComponent<Camera>();camera.scene=scene;
     camera.enabled=false;camera.orthographic=true;camera.orthographicSize=16.8f;camera.aspect=1461f/918;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.allowHDR=false;camera.allowMSAA=false;
     float unitsPerPixel=33.6f/918;camera.transform.position=new Vector3(10.5f+pixelOffset*unitsPerPixel,13.5f+pixelOffset*unitsPerPixel,-10);
     camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
     target=new RenderTexture(1461,918,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB){antiAliasing=1};Assert.True(target.Create());camera.targetTexture=target;
     readback=new Texture2D(1461,918,TextureFormat.RGBA32,false,false);int split=Mathf.RoundToInt(camera.WorldToScreenPoint(new Vector3(11.5f,14.5f,0)).x);
     var visible=Pixels(camera,target,readback,split);marker.Clear();var cleared=Pixels(camera,target,readback,split);
     Assert.AreEqual(0,cleared[0]+cleared[1],"The paired clear must remove both actual rendered corners.");
     Assert.Greater(visible[0],0,"The actual lower line must first reach the GPU.");Assert.Greater(visible[1],0,"The actual upper line must first reach the GPU.");
     Assert.That(visible[0],Is.GreaterThanOrEqualTo(16),"Lower short corner needs more than a fragile one-pixel trace at the measured native viewport.");
     Assert.That(visible[1],Is.GreaterThanOrEqualTo(16),"Upper short corner needs more than a fragile one-pixel trace at the measured native viewport.");
     Assert.False(row.GetPart<FieldHarvestPart>().Harvested);Assert.AreEqual(2,core.Zone.GetReadOnlyEntities().Count);
    }
    finally
    {
     RenderTexture.active=oldActive;marker?.Dispose();if(root!=null)Object.DestroyImmediate(root);if(readback!=null)Object.DestroyImmediate(readback);
     if(target!=null){target.Release();Object.DestroyImmediate(target);}if(scene.IsValid())EditorSceneManager.ClosePreviewScene(scene);
    }
   }
  }
  static int[] Pixels(Camera camera,RenderTexture target,Texture2D readback,int split)
  {
   var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target};Assert.True(RenderPipeline.SupportsRenderRequest(camera,request));RenderPipeline.SubmitRenderRequest(camera,request);
   RenderTexture.active=target;readback.ReadPixels(new Rect(0,0,target.width,target.height),0,0);readback.Apply(false,false);var pixels=readback.GetPixels32();var result=new int[2];
   for(int y=0;y<target.height;y++)for(int x=0;x<target.width;x++){var c=pixels[y*target.width+x];if(Math.Max(c.r,Math.Max(c.g,c.b))>=32)result[x<split?0:1]++;}return result;
  }
 }
}
