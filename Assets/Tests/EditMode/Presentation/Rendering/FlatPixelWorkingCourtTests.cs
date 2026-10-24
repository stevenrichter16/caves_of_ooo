using System;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelWorkingCourtTests
    {
        const string Kitchen = "Overworld.12.11.0";
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        DensityLootTestScope content;
        Texture2D atlas;
        FlatPixelGreyboxCatalog world;
        OverworldZoneManager manager;
        Zone zone;
        Entity grass, floor;
        [SetUp] public void Setup()
        {
            content = new DensityLootTestScope(); atlas = new Texture2D(48, 24);
            manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
            zone = new Zone(Kitchen); manager.SetActiveZone(zone);
            zone.GetCell(10,10).Explored = zone.GetCell(10,10).IsVisible = true;
            grass = content.Factory.CreateEntity("Grass"); floor = content.Factory.CreateEntity("StoneFloor");
            floor.Properties["SpreadResident.Role"] = floor.Properties["ConnectedSpread.Role"] = "court-floor";
            Assert.True(zone.AddEntity(grass,10,10)); Assert.True(zone.AddEntity(floor,10,10));
        }
        [TearDown] public void Cleanup()
        { world?.Dispose(); if(atlas != null) Object.DestroyImmediate(atlas); content?.Dispose(); }
        void Create(Texture2D texture)
        {
            var type = typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelWorkingCourtCatalog");
            Assert.NotNull(type, "Real court-floor has no native ground adapter yet.");
            var art = type.GetConstructor(new[]{typeof(Texture2D)}).Invoke(new object[]{texture});
            world = new FlatPixelGreyboxCatalog((Texture2D)null);
            var field = typeof(FlatPixelGreyboxCatalog).GetField("_workingCourtArt",I); Assert.NotNull(field); field.SetValue(world,art);
        }
        FlatPixelGreyboxCell Resolve()
        { Assert.True(world.TryResolvePresentation(zone,10,10,out var value)); return value; }
        void ExpectNative(FlatPixelGreyboxCell value)
        {
            Assert.AreSame(atlas,value.GroundTile.sprite.texture);
            Assert.AreEqual(new Vector2(16,24),value.GroundTile.sprite.rect.size);
            Assert.AreEqual(Color.white,value.GroundColor);
            Assert.True(grass.HasTag("Plantable")); Assert.AreSame(zone.GetCell(10,10),zone.GetEntityCell(grass));
        }
        [Test] public void RealSparseStonesUseNativeGroundAndSharedExamineWithoutADot()
        {
            Create(atlas); var cell=Resolve(); ExpectNative(cell);
            Assert.AreSame(floor,cell.Owner); Assert.IsNull(cell.ForegroundTile);
            Assert.True(world.TryGetPresentationSubjectSprite(zone,floor,10,10,out var sprite,out var tint));
            Assert.AreSame(cell.GroundTile.sprite,sprite); Assert.AreEqual(Color.white,tint);
            Assert.AreSame(cell.GroundTile,Resolve().GroundTile);
            Assert.AreEqual(FilterMode.Bilinear,atlas.filterMode,"Borrowed texture must not be modified.");
        }
        [TestCase("Player")] [TestCase("HealingTonic")] [TestCase("Chest")]
        public void StonesRemainGroundUnderTheActualCoveringOwner(string blueprint)
        {
            Create(atlas); var cover=content.Factory.CreateEntity(blueprint); cover.GetPart<RenderPart>().RenderLayer=100;
            Assert.True(zone.AddEntity(cover,10,10)); var cell=Resolve(); ExpectNative(cell);
            Assert.AreSame(cover,cell.Owner); Assert.NotNull(cell.ForegroundTile);
        }
        [TestCase("removed")] [TestCase("marker")] [TestCase("foreign-render")]
        [TestCase("foreign-physics")] [TestCase("carried")] [TestCase("solid")]
        [TestCase("takeable")] [TestCase("barren")] [TestCase("visual")]
        [TestCase("duplicate")] [TestCase("stale-cell")] [TestCase("no-grass")] [TestCase("solid-tag")] [TestCase("player-tag")] [TestCase("zero-stack")]
        public void InvalidOrAmbiguousFloorCannotSupplyTheMaterial(string fault)
        {
            Create(atlas); ExpectNative(Resolve());
            if(fault=="removed") Assert.True(zone.RemoveEntity(floor));
            if(fault=="marker") floor.Properties.Remove("ConnectedSpread.Role");
            if(fault=="foreign-render") floor.GetPart<RenderPart>().ParentEntity=grass;
            if(fault=="foreign-physics") floor.GetPart<PhysicsPart>().ParentEntity=grass;
            if(fault=="carried") floor.GetPart<PhysicsPart>().InInventory=grass;
            if(fault=="solid") floor.GetPart<PhysicsPart>().Solid=true;
            if(fault=="takeable") floor.GetPart<PhysicsPart>().Takeable=true;
            if(fault=="barren") floor.Tags["Barren"]="";
            if(fault=="visual") floor.GetPart<RenderPart>().VisualID="custom";
            if(fault=="no-grass") zone.RemoveEntity(grass);
            if(fault=="solid-tag") floor.Tags["Solid"]="";
            if(fault=="player-tag") floor.Tags["Player"]="";
            if(fault=="zero-stack") floor.AddPart(new StackerPart { StackCount=0 });
            if(fault=="duplicate") { var other=content.Factory.CreateEntity("StoneFloor"); other.Properties["SpreadResident.Role"]=other.Properties["ConnectedSpread.Role"]="court-floor"; zone.AddEntity(other,10,10); }
            if(fault=="stale-cell") { zone.AddEntity(floor,11,10); zone.GetCell(10,10).Objects.Add(floor); }
            Assert.AreNotSame(atlas,Resolve().GroundTile.sprite.texture);
        }
        [TestCase("water")] [TestCase("oil")] [TestCase("crop")] [TestCase("soil")] [TestCase("road")]
        public void ExistingGroundAndHazardsOutrankStones(string reason)
        {
            Create(atlas); ExpectNative(Resolve());
            if(reason=="water"||reason=="oil") zone.TileState.WriteCoating(10,10,reason,4);
            if(reason=="crop") zone.AddEntity(content.Factory.CreateEntity("ClaspbeanCrop"),10,10);
            if(reason=="soil") grass.AddPart(new CultivatedSoilPart());
            if(reason=="road") zone.AddEntity(content.Factory.CreateEntity("RoadStone"),10,10);
            Assert.AreNotSame(atlas,Resolve().GroundTile.sprite.texture);
        }
        [Test] public void HiddenCellAndOtherGraphsCannotRevealTheLiveCourtMaterial()
        {
            Create(atlas); ExpectNative(Resolve()); zone.GetCell(10,10).IsVisible=false;
            Assert.AreNotSame(atlas,Resolve().GroundTile.sprite.texture);
            zone.GetCell(10,10).IsVisible=true; manager.CachedZones[Kitchen]=new Zone(Kitchen);
            Assert.False(world.TryResolvePresentation(zone,10,10,out _));
        }
        [Test] public void AllocatedTilesAndSpritesAreDisposedButBorrowedAtlasSurvives()
        {
            Create(atlas); var tile=Resolve().GroundTile; var sprite=tile.sprite;
            world.Dispose(); world.Dispose();
            Assert.True(tile==null); Assert.True(sprite==null); Assert.True(atlas!=null);
        }
        [Test] public void MalformedTextureIsUnavailableWithoutImplicitResourceLoading()
        {
            var bad=new Texture2D(47,24);
            try { Create(bad); Assert.AreNotSame(bad,Resolve().GroundTile.sprite.texture); Assert.NotNull(Resolve().ForegroundTile); }
            finally { Object.DestroyImmediate(bad); }
        }
        [Test] public void MissingAtlasFallsBackAndDisposeKeepsBorrowedTexture()
        {
            Create(null); Assert.NotNull(Resolve().ForegroundTile); world.Dispose(); world.Dispose();
            Assert.NotNull(atlas); Assert.False(world.TryResolvePresentation(zone,10,10,out _));
        }
    }
}
