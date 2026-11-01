using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelFellingGroundTests
    {
        DensityLootTestScope content;
        OverworldZoneManager manager;
        Zone zone;
        FlatPixelGreyboxCatalog world;
        Entity floor;
        int x,y;
        [SetUp] public void Setup()
        {
            content=new DensityLootTestScope();manager=OverworldZoneManager.CreateDetached(content.Factory,64,true);
            zone=manager.GetZone(FellingSiteBuilder.ZoneID);manager.SetActiveZone(zone);
            Assert.True(FellingSceneRuntime.IsActive(zone));
            foreach(var c in zone.Cells)c.Explored=c.IsVisible=true;
            var cell=zone.Cells.Cast<Cell>().First(c=>c.Objects.Count==1&&c.Objects[0].BlueprintName=="TepuiStone"
                &&!c.Objects[0].GetPart<PhysicsPart>().Solid&&!c.Objects[0].HasTag("Wall"));
            x=cell.X;y=cell.Y;floor=cell.Objects[0];world=new FlatPixelGreyboxCatalog();
        }
        [TearDown] public void Cleanup(){world?.Dispose();content?.Dispose();}
        FlatPixelGreyboxCell View(){Assert.True(world.TryResolvePresentation(zone,x,y,out var v));return v;}
        [Test] public void AuthoredDryGroundUsesApprovedPinkStoneWithoutChangingGeometry()
        {
            int version=zone.EntityVersion;var before=zone.GetCell(x,y).BlocksMovement();var v=View();
            Assert.True(v.LocalGround,"Felling's ordinary pink stone is still an ASCII field");
            Assert.AreSame(Resources.Load<Texture2D>(FlatPixelStumpMaterialsCatalog.ResourcePath),v.GroundTile.sprite.texture);
            Assert.That(v.GroundTile.sprite.rect.x/16,Is.InRange(0,3));Assert.IsNull(v.ForegroundTile);
            Assert.AreEqual(before,v.BlocksMovement);Assert.AreEqual(version,zone.EntityVersion);
        }
        [Test] public void AuthoredOpenDryCellsHaveNativeGroundWithoutReinterpretingLandmarks()
        {
            int count=0;foreach(var spec in FellingSceneDefinition.Load().cells)
            {
                if(spec.water||spec.solid||spec.opaque)continue;
                if(FellingSceneDefinition.Load().landmarks.Any(l=>l.x==spec.x&&l.y==spec.y))continue;
                Assert.True(world.TryResolvePresentation(zone,spec.x,spec.y,out var v));
                Assert.True(v.LocalGround,"dry source cell "+spec.x+","+spec.y);count++;
            }
            Assert.AreEqual(1638,count);
        }
        [Test] public void AuthorizedUnderfootReaderKeepsTheGroundIdentity()
        {
            var player=content.Factory.CreateEntity("Player");Assert.True(zone.AddEntity(player,x,y));
            Assert.True(View().LocalGround);
            Assert.True(world.TryGetPresentationSubjectSprite(zone,floor,x,y,out var sprite,out _));
            Assert.AreSame(View().GroundTile.sprite,sprite);
        }
        [TestCase("tag")] [TestCase("id")] [TestCase("solid")] [TestCase("wall")]
        [TestCase("render")] [TestCase("prepared")] [TestCase("hidden")]
        [TestCase("coating")] [TestCase("revision")] [TestCase("foreign-zone")]
        public void AlteredAuthoredMeaningNeverBorrowsDryGround(string change)
        {
            switch(change)
            {
                case "tag":floor.Tags.Remove(FellingSceneRuntime.TerrainTag);break;
                case "id":floor.ID="foreign-terrain";break;
                case "solid":floor.GetPart<PhysicsPart>().Solid=true;break;
                case "wall":floor.SetTag("Wall");break;
                case "render":floor.GetPart<RenderPart>().ColorString="&R";break;
                case "prepared":floor.AddPart(new CultivatedSoilPart());break;
                case "hidden":floor.GetPart<RenderPart>().Visible=false;break;
                case "coating":zone.TileState.WriteCoating(x,y,"oil",5);break;
                case "revision":FellingSceneRuntime.GetState(zone).Revision=99;break;
                case "foreign-zone":zone.ZoneID="Overworld.3.6.0";break;
            }
            if(world.TryResolvePresentation(zone,x,y,out var v))Assert.False(v.LocalGround,change);
        }
        [Test] public void SolidGeometryAndBlackwaterAreNeverPresentedAsOpenDryFloor()
        {
            int count=0;foreach(var spec in FellingSceneDefinition.Load().cells)
            {
                if(!spec.solid&&!spec.water&&!spec.opaque)continue;
                Assert.True(world.TryResolvePresentation(zone,spec.x,spec.y,out var v));
                Assert.False(v.LocalGround,"solid/water "+spec.x+","+spec.y);Assert.True(v.BlocksMovement);count++;
            }
            Assert.AreEqual(355,count);
        }
        [TestCase(POIType.MerchantCamp)] [TestCase(POIType.Sinkhole)]
        public void RevokedFellingAuthorityCannotFallThroughToGenericSites(POIType replacement)
        {
            Assert.True(View().LocalGround);
            manager.WorldMap.SetPOI(3,5,new PointOfInterest(replacement,"replacement"));
            FellingSceneRuntime.GetState(zone).Revision=99;floor.ID="foreign-terrain";
            Assert.False(View().LocalGround);
        }
        [Test] public void HiddenRememberedAndReplacementGraphsCannotReuseLiveFloor()
        {
            Assert.True(View().LocalGround);
            zone.GetCell(x,y).IsVisible=false;
            if(world.TryResolvePresentation(zone,x,y,out var remembered))Assert.False(remembered.LocalGround);
            zone.GetCell(x,y).IsVisible=true;
            manager.CachedZones[zone.ZoneID]=new Zone(zone.ZoneID);
            if(world.TryResolvePresentation(zone,x,y,out var replaced))Assert.False(replaced.LocalGround);
            Assert.False(world.TryGetPresentationSubjectSprite(zone,floor,x,y,out _,out _));
        }
        [Test] public void ExplicitlyMissingOptionalResourcesKeepFallback()
        {
            using(var empty=new FlatPixelGreyboxCatalog((Texture2D)null))
            {Assert.True(empty.TryResolvePresentation(zone,x,y,out var v));Assert.False(v.LocalGround);}
        }
    }
}
