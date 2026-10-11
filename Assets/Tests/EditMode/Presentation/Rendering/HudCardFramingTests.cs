using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Current API only: import with first RED. These inspect actual submitted
    // background/icon pixels and tile transforms, never an invented visual model.
    public sealed class HudCardFramingTests
    {
        GameObject root; Tilemap fg,bg; Camera camera; GameplayHotbarRenderer renderer;
        Entity actor; ActivatedAbilitiesPart abilities; ActivatedAbility first;
        [SetUp] public void Setup()
        {
            root=new GameObject("HUD framed cards");root.AddComponent<Grid>().cellSize=new Vector3(.6f,1,0);
            var front=new GameObject("fg");front.transform.SetParent(root.transform,false);fg=front.AddComponent<Tilemap>();
            var back=new GameObject("bg");back.transform.SetParent(root.transform,false);bg=back.AddComponent<Tilemap>();
            var eye=new GameObject("camera");eye.transform.SetParent(root.transform,false);camera=eye.AddComponent<Camera>();camera.orthographic=true;camera.aspect=8;
            renderer=new GameplayHotbarRenderer(fg,bg);actor=new Entity();abilities=new ActivatedAbilitiesPart();actor.AddPart(abilities);
            first=abilities.GetAbility(abilities.AddAbility("Flaming Hands","CommandOne","Skill",AbilityTargetingMode.AdjacentCell,1,"Pyromancy_FlamingHands"));
            abilities.AddAbility("Jet Blast","CommandTwo","Skill",AbilityTargetingMode.DirectionLine,4,"Hydromancy_JetBlast");
            PerformanceDiagnostics.ResetAll();
        }
        [TearDown] public void Cleanup(){Object.DestroyImmediate(root);PerformanceDiagnostics.ResetAll();}
        [Test] public void AllTenCardsSubmitThinInsetFramesInTheirExistingFootprints()
        {
            renderer.Render(HotbarStateBuilder.Build(actor,0,null),camera);
            float width=GameplayHotbarLayout.ComputeCellWidth(camera.aspect);
            for(int i=0;i<10;i++)
            {
                var p=new Vector3Int(i*8,0,0);var sprite=bg.GetSprite(p);Assert.NotNull(sprite);
                Assert.AreEqual(64,sprite.rect.width);Assert.AreEqual(80,sprite.rect.height);
                var pixels=sprite.texture.GetPixels((int)sprite.rect.x,(int)sprite.rect.y,64,80);
                Assert.AreEqual(0,pixels[0].a,"Inset outside edge separates adjacent cards.");
                Assert.Greater(pixels[1+10*64].r,pixels[32+40*64].r,"A real brighter one-pixel border surrounds a filled body.");
                Bounds b=Placed(bg,p);Assert.AreEqual(i*8*width,b.min.x,.001f);Assert.AreEqual((i+1)*8*width,b.max.x,.001f);
                Assert.AreEqual(0,b.min.y,.001f);Assert.AreEqual(5,b.max.y,.001f);
            }
            Assert.AreEqual(6,GameplayHotbarLayout.GridHeight);
        }
        [Test] public void HeaderBackgroundCoversEveryCellAndFollowsAspectChanges()
        {
            var snapshot=HotbarStateBuilder.Build(actor,0,null);
            foreach(float aspect in new[]{8f,10f,8f})
            {
                camera.aspect=aspect;
                float width=GameplayHotbarLayout.ComputeCellWidth(aspect);
                root.GetComponent<Grid>().cellSize=new Vector3(width,1,0);
                renderer.Render(snapshot,camera);
                for(int x=0;x<GameplayHotbarLayout.GridWidth;x++)
                {
                    var p=new Vector3Int(x,5,0);Assert.NotNull(bg.GetSprite(p));
                    var b=Placed(bg,p);
                    Assert.AreEqual(x*width,b.min.x,.001f,"No exposed gap at the left cell edge.");
                    Assert.AreEqual((x+1)*width,b.max.x,.001f,"No exposed gap at the right cell edge.");
                    Assert.AreEqual(5,b.min.y,.001f);Assert.AreEqual(6,b.max.y,.001f);
                }
            }
        }
        [Test] public void LargerActualIconLeavesClearGapsFromBothNameRowsAndState()
        {
            renderer.Render(HotbarStateBuilder.Build(actor,0,null),camera);
            var p=new Vector3Int(3,2,0);Assert.NotNull(fg.GetSprite(p));
            Bounds icon=Placed(fg,p);Assert.AreEqual(1.75f,icon.size.y,.001f);
            float nameTop=RowBounds(fg,1).Max(b=>b.max.y),secondTop=RowBounds(fg,0).Max(b=>b.max.y);
            float stateBottom=RowBounds(fg,4).Min(b=>b.min.y);
            Assert.Greater(icon.min.y,nameTop+.05f);Assert.Greater(icon.min.y,secondTop+.05f);
            Assert.Less(icon.max.y,stateBottom-.05f);Assert.Less(icon.max.y,5);
        }
        [Test] public void SelectingAnotherCardErasesOldFrameTintWithoutChangingNames()
        {
            renderer.Render(HotbarStateBuilder.Build(actor,0,null),camera);
            Color selected=bg.GetColor(Vector3Int.zero),normal=bg.GetColor(new Vector3Int(8,0,0));Assert.AreNotEqual(selected,normal);
            var row0=Row(0);var row1=Row(1);renderer.Render(HotbarStateBuilder.Build(actor,1,null),camera);
            Assert.AreEqual(normal,bg.GetColor(Vector3Int.zero));Assert.AreEqual(selected,bg.GetColor(new Vector3Int(8,0,0)));
            CollectionAssert.AreEqual(row0,Row(0));CollectionAssert.AreEqual(row1,Row(1));
        }
        [Test] public void CancelledPendingFrameReturnsToSelectedColor()
        {
            renderer.Render(HotbarStateBuilder.Build(actor,0,null),camera);Color selected=bg.GetColor(Vector3Int.zero);
            renderer.Render(HotbarStateBuilder.Build(actor,0,first),camera);Assert.AreNotEqual(selected,bg.GetColor(Vector3Int.zero));
            renderer.Render(HotbarStateBuilder.Build(actor,0,null),camera);Assert.AreEqual(selected,bg.GetColor(Vector3Int.zero));
        }
        [Test] public void RemovedCardsAndExplicitClearEraseTheirBackgroundFrames()
        {
            var snapshot=HotbarStateBuilder.Build(actor,0,null);renderer.Render(snapshot,camera);
            Assert.NotNull(bg.GetTile(Vector3Int.zero));renderer.Render(null,camera);
            for(int i=0;i<10;i++)Assert.IsNull(bg.GetTile(new Vector3Int(i*8,0,0)));
            renderer.Render(snapshot,camera);Assert.NotNull(bg.GetTile(Vector3Int.zero));renderer.Clear();
            Assert.AreEqual(0,bg.GetUsedTilesCount());Assert.AreEqual(0,fg.GetUsedTilesCount());renderer.Render(snapshot,camera);
            Assert.NotNull(bg.GetTile(Vector3Int.zero));
        }
        [Test] public void UnchangedFramesKeepBackgroundAndForegroundSentinelsWithoutWork()
        {
            var snapshot=HotbarStateBuilder.Build(actor,0,null);renderer.Render(snapshot,camera);
            bg.SetColor(Vector3Int.zero,Color.magenta);fg.SetColor(new Vector3Int(1,4,0),Color.green);
            PerformanceDiagnostics.BeginFrame(0,false,false);renderer.Render(snapshot,camera);PerformanceDiagnostics.EndFrame(0);
            Assert.AreEqual(0,PerformanceDiagnostics.LastCompletedFrameSnapshot.HotbarRenderCount);
            Assert.AreEqual(0,PerformanceDiagnostics.LastCompletedFrameSnapshot.TilemapClearCount);
            Assert.AreEqual(Color.magenta,bg.GetColor(Vector3Int.zero));Assert.AreEqual(Color.green,fg.GetColor(new Vector3Int(1,4,0)));
        }
        TileBase[] Row(int y)=>Enumerable.Range(0,16).Select(x=>fg.GetTile(new Vector3Int(x,y,0))).ToArray();
        static IEnumerable<Bounds> RowBounds(Tilemap map,int y)
        {for(int x=0;x<8;x++){var p=new Vector3Int(x,y,0);if(map.GetSprite(p)!=null)yield return Placed(map,p);}}
        static Bounds Placed(Tilemap map,Vector3Int p)
        {
            var b=map.GetSprite(p).bounds;var m=map.orientationMatrix*map.GetTransformMatrix(p);var center=map.GetCellCenterLocal(p);
            var placed=new Bounds(center+m.MultiplyPoint3x4(b.min),Vector3.zero);placed.Encapsulate(center+m.MultiplyPoint3x4(b.max));return placed;
        }
    }
}
