using System;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class MachineOilStrainerFlatTests
    {
        DensityLootTestScope content; OverworldZoneManager manager; Zone zone;
        FlatPixelGreyboxCatalog world; Entity bench;
        [SetUp] public void Setup()
        {
            content=new DensityLootTestScope();manager=OverworldZoneManager.CreateDetached(content.Factory,65,true);
            zone=new Zone(SoddenDistrictPlan.WorksZoneID);manager.SetActiveZone(zone);
            foreach(var c in zone.Cells)c.Explored=c.IsVisible=true;
            zone.AddEntity(content.Factory.CreateEntity("StoneFloor"),59,5);
            bench=content.Factory.CreateEntity(OilStrainingPart.Blueprint);zone.AddEntity(bench,59,5);
            world=new FlatPixelGreyboxCatalog();
        }
        [TearDown] public void Cleanup(){world?.Dispose();content?.Dispose();}
        bool Portrait()=>world.TryGetPresentationSubjectSprite(zone,bench,59,5,out var sprite,out _)
            && sprite.texture==Resources.Load<Texture2D>(FlatPixelWildFaunaCatalog.PropAtlasResourcePath);
        [Test] public void ActualFlatWorldAndReaderReuseTheSameApprovedPlainBench()
        {
            Assert.True(world.TryResolvePresentation(zone,59,5,out var v));Assert.AreSame(bench,v.Owner);
            Assert.True(v.NativeForeground,"The 3D model does not establish flat presentation.");
            var atlas=Resources.Load<Texture2D>(FlatPixelWildFaunaCatalog.PropAtlasResourcePath);
            Assert.AreSame(atlas,v.ForegroundTile.sprite.texture);
            Assert.AreEqual(new Rect(16,atlas.height-264,16,24),v.ForegroundTile.sprite.rect);
            Assert.True(world.TryGetPresentationSubjectSprite(zone,bench,59,5,out var portrait,out var tint));
            Assert.AreSame(v.ForegroundTile.sprite,portrait);Assert.AreEqual(Color.white,tint);
        }
        [TestCase("hidden")][TestCase("unexplored")][TestCase("invisible")][TestCase("color")]
        [TestCase("glyph")][TestCase("layer")][TestCase("visual")][TestCase("foreign-part")]
        [TestCase("extra-part")][TestCase("carried")][TestCase("moved")][TestCase("stale-zone")]
        public void NoncanonicalOrUndisclosedBenchDoesNotBorrowNativeArt(string defect)
        {
            var r=bench.GetPart<RenderPart>();
            switch(defect)
            {
                case "hidden":zone.GetCell(59,5).IsVisible=false;break;
                case "unexplored":zone.GetCell(59,5).Explored=false;break;
                case "invisible":r.Visible=false;break;
                case "color":r.ColorString="&r";break;
                case "glyph":r.RenderString="n";break;
                case "layer":r.RenderLayer=9;break;
                case "visual":r.VisualID="foreign";break;
                case "foreign-part":bench.GetPart<OilStrainingPart>().ParentEntity=new Entity();break;
                case "extra-part":bench.AddPart(new ContainerPart());break;
                case "carried":bench.GetPart<PhysicsPart>().InInventory=new Entity();break;
                case "moved":zone.RemoveEntity(bench);zone.AddEntity(bench,60,5);break;
                case "stale-zone":manager.SetActiveZone(new Zone(zone.ZoneID));break;
            }
            using(var catalog=new FlatPixelWildFaunaCatalog())Assert.False(catalog.TryResolve(zone,bench,out _));
            Assert.False(Portrait());
        }
        [Test] public void MissingOptionalArtRemainsOnFallback()
        { world.Dispose();world=new FlatPixelGreyboxCatalog((Texture2D)null);Assert.False(Portrait()); }
        [TestCase(false)][TestCase(true)] public void StaleSpatialAttachmentDoesNotBorrowTheBench(bool foreign)
        {
            typeof(Entity).GetField("SpatialZone",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
                .SetValue(bench,foreign?new Zone(zone.ZoneID):null);
            using(var catalog=new FlatPixelWildFaunaCatalog())Assert.False(catalog.TryResolve(zone,bench,out _));
            Assert.False(Portrait());
        }
        [Test] public void OldRigRemainsOutsideThisAlias()
        {
            var rig=content.Factory.CreateEntity("AbandonedFieldWorkRig");zone.AddEntity(rig,56,5);
            using(var catalog=new FlatPixelWildFaunaCatalog())Assert.False(catalog.TryResolve(zone,rig,out _));
        }
    }
}
