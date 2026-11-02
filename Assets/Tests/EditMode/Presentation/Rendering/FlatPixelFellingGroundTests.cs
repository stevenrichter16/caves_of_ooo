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
        void SolidReaderDeclines(Entity owner)
        {
            // The public reader deliberately supplies semantic glyph fallback on refusal.
            var adapter=(FlatPixelStumpMaterialsCatalog)typeof(FlatPixelGreyboxCatalog)
                .GetField("_fellingGroundArt",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(world);
            Assert.NotNull(adapter);Assert.False(adapter.TryGetSubjectSprite(zone,owner,x,y,out _));
            if(world.TryGetPresentationSubjectSprite(zone,owner,x,y,out var sprite,out _))
                Assert.AreNotSame(Resources.Load<Texture2D>(FlatPixelStumpMaterialsCatalog.ResourcePath),sprite.texture);
        }
        [Test] public void SavedLairClaimOutlivesRestoredFellingLabel()
        {
            var owner=PickSolid(true);NativeSolid(owner);var original=manager.WorldMap.GetPOI(3,5);
            manager.WorldMap.SetPOI(3,5,new PointOfInterest(POIType.Lair,"saved legacy",null,1,"DesertProwler"));
            LairStacks.Restore(manager,null);Assert.NotNull(LairStacks.Inspect(manager,zone.ZoneID));
            manager.WorldMap.SetPOI(3,5,original);
            if(world.TryResolvePresentation(zone,x,y,out var v))Assert.False(v.NativeForeground);
            SolidReaderDeclines(owner);
        }
        Entity PickSolid(bool opaque)
        {
            var cell=zone.Cells.Cast<Cell>().First(c=>c.Objects.Count==1
                &&c.Objects[0].BlueprintName=="TepuiStone"&&c.Objects[0].GetPart<PhysicsPart>().Solid
                &&c.Objects[0].HasTag("Wall")==opaque);
            x=cell.X;y=cell.Y;return cell.Objects[0];
        }
        void NativeSolid(Entity owner)
        {
            var v=View();Assert.True(v.NativeForeground,"authored solid still has glyph presentation");
            Assert.AreSame(owner,v.Owner);Assert.False(v.LocalGround);Assert.True(v.BlocksMovement);
            Assert.AreSame(Resources.Load<Texture2D>(FlatPixelStumpMaterialsCatalog.ResourcePath),v.ForegroundTile.sprite.texture);
            int frame=(int)(v.ForegroundTile.sprite.rect.x/16);
            Assert.True(owner.HasTag("Wall")?(frame>=4&&frame<=5||frame>=26&&frame<=40):(frame>=20&&frame<=21||frame>=56&&frame<=70),"wrong solid role frame "+frame);
            Assert.True(world.TryGetPresentationSubjectSprite(zone,owner,x,y,out var sprite,out var tint));
            Assert.AreEqual(v.ForegroundTile.sprite.rect,sprite.rect);Assert.AreEqual(Color.white,tint);
        }
        [TestCase(true)] [TestCase(false)]
        public void AuthoredSolidUsesItsOwnRoleWithoutChangingCollisionOrSight(bool opaque)
        {
            var owner=PickSolid(opaque);int version=zone.EntityVersion;string state=zone.TileState.ToSaveString();
            NativeSolid(owner);Assert.AreEqual(opaque,zone.GetCell(x,y).IsWall());
            Assert.AreEqual(version,zone.EntityVersion);Assert.AreEqual(state,zone.TileState.ToSaveString());
        }
        [Test] public void EverySelectedAuthoredSolidUsesTheRightRoleWhileWaterAndRitualRemainExcluded()
        {
            int walls=0,ridges=0,water=0,selected=0;
            foreach(var spec in FellingSceneDefinition.Load().cells)
            {
                if(spec.water){water++;Assert.True(world.TryResolvePresentation(zone,spec.x,spec.y,out var w));Assert.False(w.LocalGround);continue;}
                if(!spec.solid)continue;
                if(spec.opaque)walls++;else ridges++;
                x=spec.x;y=spec.y;var owner=zone.GetCell(x,y).Objects.First(o=>o.ID=="felling-terrain:"+x+":"+y);
                Assert.AreEqual(spec.opaque,zone.GetCell(x,y).IsWall());Assert.True(zone.GetCell(x,y).BlocksMovement());
                if(ReferenceEquals(View().Owner,owner)){NativeSolid(owner);selected++;}
            }
            Assert.AreEqual(195,walls);Assert.AreEqual(103,ridges);Assert.AreEqual(57,water);Assert.Greater(selected,200);
            foreach(var l in FellingSceneDefinition.Load().landmarks)
            {Assert.True(world.TryResolvePresentation(zone,l.x,l.y,out var v));Assert.False(v.LocalGround);}
        }
        [TestCase("id")] [TestCase("tag")] [TestCase("wall")] [TestCase("solid-tag")]
        [TestCase("physics")] [TestCase("render")] [TestCase("layer")] [TestCase("variants")]
        [TestCase("terrain")] [TestCase("plantable")] [TestCase("crop-state")] [TestCase("coating")]
        [TestCase("hidden-render")] [TestCase("unseen")] [TestCase("memory")] [TestCase("revision")]
        [TestCase("poi")] [TestCase("graph")] [TestCase("detached")]
        public void AlteredSolidMeaningOrAuthorityDeclinesNativeStone(string change)
        {
            var owner=PickSolid(true);NativeSolid(owner);
            switch(change)
            {
                case "id":owner.ID="foreign";break;
                case "tag":owner.Tags.Remove(FellingSceneRuntime.TerrainTag);break;
                case "wall":owner.Tags.Remove("Wall");break;
                case "solid-tag":owner.Tags.Remove("Solid");break;
                case "physics":owner.GetPart<PhysicsPart>().Solid=false;break;
                case "render":owner.GetPart<RenderPart>().ColorString="&R";break;
                case "layer":owner.GetPart<RenderPart>().RenderLayer=2;break;
                case "variants":owner.GetPart<RenderPart>().GlyphVariants="";break;
                case "terrain":owner.Tags.Remove("Terrain");break;
                case "plantable":owner.SetTag("Plantable");break;
                case "crop-state":owner.AddPart(new CultivatedSoilPart());break;
                case "coating":zone.TileState.WriteCoating(x,y,"oil",5);break;
                case "hidden-render":owner.GetPart<RenderPart>().Visible=false;break;
                case "unseen":zone.GetCell(x,y).Explored=zone.GetCell(x,y).IsVisible=false;break;
                case "memory":zone.GetCell(x,y).IsVisible=false;break;
                case "revision":FellingSceneRuntime.GetState(zone).Revision=99;break;
                case "poi":manager.WorldMap.SetPOI(3,5,new PointOfInterest(POIType.MerchantCamp,"foreign"));break;
                case "graph":manager.CachedZones[zone.ZoneID]=new Zone(zone.ZoneID);break;
                case "detached":zone.RemoveEntity(owner);break;
            }
            if(world.TryResolvePresentation(zone,x,y,out var v))Assert.False(v.NativeForeground,change);
            SolidReaderDeclines(owner);
        }
        [TestCase(true)] [TestCase(false)]
        public void SameRoleDisclosedJoinsNeverUseHiddenRememberedOrForeignNeighbor(bool opaque)
        {
            var c=zone.Cells.Cast<Cell>().First(c=>c.Objects.Count==1&&c.Objects[0].BlueprintName=="TepuiStone"
                &&c.Objects[0].GetPart<PhysicsPart>().Solid&&c.Objects[0].HasTag("Wall")==opaque
                &&zone.GetCell(c.X+1,c.Y)?.Objects.Count==1
                &&zone.GetCell(c.X+1,c.Y).Objects[0].BlueprintName=="TepuiStone"
                &&zone.GetCell(c.X+1,c.Y).Objects[0].GetPart<PhysicsPart>().Solid
                &&zone.GetCell(c.X+1,c.Y).Objects[0].HasTag("Wall")==opaque);
            x=c.X;y=c.Y;NativeSolid(c.Objects[0]);var joined=View().ForegroundTile.sprite.rect;
            var neighbor=zone.GetCell(x+1,y);neighbor.IsVisible=false;var hidden=View().ForegroundTile.sprite.rect;
            Assert.AreNotEqual(joined,hidden);neighbor.Objects[0].ID="foreign";Assert.AreEqual(hidden,View().ForegroundTile.sprite.rect);
            neighbor.IsVisible=true;Assert.AreEqual(hidden,View().ForegroundTile.sprite.rect);
        }
        [Test] public void CoveredOrSwappedSolidCannotOverrideSelectedIdentity()
        {
            var owner=PickSolid(false);NativeSolid(owner);int a=x,b=y;
            var player=content.Factory.CreateEntity("Player");Assert.True(zone.AddEntity(player,x,y));
            Assert.AreSame(player,View().Owner);SolidReaderDeclines(owner);
            zone.RemoveEntity(player);var other=PickSolid(true);int c=x,d=y;
            zone.RemoveEntity(owner);zone.RemoveEntity(other);Assert.True(zone.AddEntity(owner,c,d));Assert.True(zone.AddEntity(other,a,b));
            Assert.False(View().NativeForeground);x=a;y=b;Assert.False(View().NativeForeground);
        }
        [Test] public void MissingOptionalAtlasAndDisposedCatalogKeepSolidFallback()
        {
            var owner=PickSolid(true);NativeSolid(owner);
            using(var empty=new FlatPixelGreyboxCatalog((Texture2D)null))
            {Assert.True(empty.TryResolvePresentation(zone,x,y,out var v));Assert.False(v.NativeForeground);}
            world.Dispose();Assert.False(world.TryResolvePresentation(zone,x,y,out _));
        }
    }
}
