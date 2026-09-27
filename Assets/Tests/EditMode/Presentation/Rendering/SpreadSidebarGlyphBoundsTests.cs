using System;
using System.Linq;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object=UnityEngine.Object;
namespace CavesOfOoo.Tests
{
 // Reproduces the measured zero text inset, not a claim of outside-frustum glyphs.
 public sealed class SpreadSidebarGlyphBoundsTests
 {
  [TestCase(11.48f,false)][TestCase(11.48f,true)][TestCase(17f,true)]
  public void ActualSidebarGlyphsKeepHalfARowAtBothScreenEdges(float size,bool longLog)
  {
   GameObject root=null;RenderTexture target=null;
   try
   {
    root=new GameObject("Owned sidebar glyph bounds");root.SetActive(true);
    var camObject=new GameObject("Owned sidebar camera");camObject.transform.SetParent(root.transform,false);var camera=camObject.AddComponent<Camera>();camera.orthographic=true;
    target=new RenderTexture(1920,1080,0);camera.targetTexture=target;
    var layout=GameplayViewportLayout.Measure(16f/9f,size,20,34);
    camera.rect=layout.SidebarRect;camera.aspect=layout.SidebarAspect;camera.orthographicSize=size;camera.transform.position=new Vector3(layout.SidebarWorldWidth*.5f,size,-10);
    var go=new GameObject("Owned text grid");go.transform.SetParent(root.transform,false);var grid=go.AddComponent<Grid>();grid.cellSize=new Vector3(.5f,1,0);
    var fgObject=new GameObject("Owned foreground");fgObject.transform.SetParent(go.transform,false);var fg=fgObject.AddComponent<Tilemap>();
    var bgObject=new GameObject("Owned background");bgObject.transform.SetParent(go.transform,false);var bg=bgObject.AddComponent<Tilemap>();
    var render=new GameplaySidebarRenderer(fg,bg,go.transform,20);
    string log=longLog?string.Join(" ",Enumerable.Repeat("LONG-CURRENT-TEXT",120)):"quiet turn";
    var snapshot=new SidebarSnapshot(new[]{"HP 40/40"},"-",null,new[]{new SidebarLogEntry(log,0,1,1)});
    var position=camera.transform.position;var rect=camera.rect;float beforeSize=camera.orthographicSize;
    render.Render(snapshot,camera,34,false,0);
    Assert.AreEqual(new Vector3(.5f,.5f,0),fg.tileAnchor,"Same native tile anchor as the retained live measurement.");
    Assert.AreEqual(new Vector3(1.25f,39.5f,0),fg.GetCellCenterLocal(new Vector3Int(2,39,0)),"Active Grid must provide actual cell transforms; an inactive Grid returns zero centres in this native fixture.");
    float minX=float.PositiveInfinity,minY=float.PositiveInfinity,maxX=float.NegativeInfinity,maxY=float.NegativeInfinity;int count=0;
    foreach(var cell in fg.cellBounds.allPositionsWithin)
    {
     var sprite=fg.GetSprite(cell);if(sprite==null||!sprite.name.StartsWith("Text_"))continue;
     int code=Convert.ToInt32(sprite.name.Substring(5),16);if(code<=32||code>=127)continue;count++;
     var b=sprite.bounds;var center=fg.GetCellCenterLocal(cell);var matrix=fg.orientationMatrix*fg.GetTransformMatrix(cell);
     for(int i=0;i<4;i++){var p=new Vector3((i&1)==0?b.min.x:b.max.x,(i&2)==0?b.min.y:b.max.y,0);var screen=camera.WorldToScreenPoint(fg.transform.TransformPoint(center+matrix.MultiplyPoint3x4(p)));minX=Mathf.Min(minX,screen.x);maxX=Mathf.Max(maxX,screen.x);minY=Mathf.Min(minY,screen.y);maxY=Mathf.Max(maxY,screen.y);}
    }
    Assert.Greater(count,15,"Actual atlas glyphs, not an empty measurement.");var pixels=camera.pixelRect;float halfRow=pixels.height/(20f*2f)*.5f;
    TestContext.WriteLine("text inset pixels L="+(minX-pixels.xMin)+" R="+(pixels.xMax-maxX)+" B="+(minY-pixels.yMin)+" T="+(pixels.yMax-maxY)+" required="+halfRow);
    Assert.GreaterOrEqual(minX,pixels.xMin-.01f);Assert.LessOrEqual(maxX,pixels.xMax+.01f);
    Assert.GreaterOrEqual(pixels.yMax-maxY,halfRow-.01f,"VITALS must not touch the top capture row.");Assert.GreaterOrEqual(minY-pixels.yMin,halfRow-.01f,"Newest log text must have a bottom inset.");
    Assert.AreEqual(position,camera.transform.position);Assert.AreEqual(rect,camera.rect);Assert.AreEqual(beforeSize,camera.orthographicSize);
   }
   finally{if(root!=null)Object.DestroyImmediate(root);if(target!=null){target.Release();Object.DestroyImmediate(target);}}
  }
 }
}
