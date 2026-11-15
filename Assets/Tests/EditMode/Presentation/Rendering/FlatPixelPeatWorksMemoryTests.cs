using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed partial class FlatPixelSoddenTravelTests
    {
        void PeatHome() { zone=Attach(SoddenDistrictPlan.WorksZoneID); }
        string PeatResource(string bp) => bp=="Grass" ? FenAtlas : bp=="StoneFloor"
            ? "Sprites/Live16x24/cellar-floor" : "Sprites/Live16x24/opening-cellar";
        [TestCase("Grass")][TestCase("StoneFloor")][TestCase("StoneWall")]
        public void PeatMemoryKeepsStaticFrameSingleDimAndNoReader(string bp)
        {
            PeatHome();var owner=Add(bp);var live=View();bool floor=bp!="StoneWall";
            var liveTile=floor?live.GroundTile:live.ForegroundTile;NativeTile(liveTile,PeatResource(bp));
            zone.GetCell(X,Y).IsVisible=false;var memory=View();var tile=floor?memory.GroundTile:memory.ForegroundTile;
            Assert.True(memory.Remembered);Assert.False(memory.BlocksMovement);Assert.AreSame(owner,memory.Owner);
            Assert.AreSame(liveTile,tile);Assert.AreEqual(new Color(.35f,.35f,.35f,1f),floor?memory.GroundColor:memory.ForegroundColor);
            if(floor){Assert.True(memory.LocalGround);Assert.IsNull(memory.ForegroundTile);}
            bool reader=world.TryGetPresentationSubjectSprite(zone,owner,X,Y,out var portrait,out _);
            Assert.False(reader&&portrait.texture==tile.sprite.texture);
            using(var paint=new Paint(world,zone)){paint.Step();Assert.AreSame(tile,floor?paint.GroundAt(X,Y):paint.At(X,Y));Assert.IsNull(paint.Mark);}
            zone.GetCell(X,Y).IsVisible=true;var again=View();Assert.AreSame(liveTile,floor?again.GroundTile:again.ForegroundTile);
        }
        [TestCase("Grass")][TestCase("StoneFloor")][TestCase("StoneWall")]
        public void PeatMemoryIgnoresUnseenLiveStateAndNeighbours(string bp)
        {
            PeatHome();var owner=Add(bp);zone.GetCell(X,Y).IsVisible=false;bool floor=bp!="StoneWall";
            var before=View();var tile=floor?before.GroundTile:before.ForegroundTile;NativeTile(tile,PeatResource(bp));
            owner.GetPart<PhysicsPart>().Solid=!owner.GetPart<PhysicsPart>().Solid;
            owner.GetPart<RenderPart>().ColorString="&R";
            if(owner.GetPart<DestructiblePart>() is DestructiblePart wall)wall.HP=0;
            if(owner.GetPart<ThermalPart>() is ThermalPart thermal)thermal.Temperature=1000;
            zone.TileState.AddHeat(X,Y,3);zone.TileState.AddCharge(X,Y,4);zone.TileState.WriteResidue(X,Y,"embers",5);
            var actor=Add("Player");actor.GetPart<RenderPart>().RenderLayer=100;Add("Rock",X+1,Y);
            string state=zone.TileState.ToSaveString();var after=View();Assert.AreSame(owner,after.Owner);
            Assert.AreSame(tile,floor?after.GroundTile:after.ForegroundTile);
            Assert.AreEqual(floor?before.GroundColor:before.ForegroundColor,floor?after.GroundColor:after.ForegroundColor);Assert.AreEqual(state,zone.TileState.ToSaveString());
            using(var paint=new Paint(world,zone)){paint.Step();Assert.AreSame(tile,floor?paint.GroundAt(X,Y):paint.At(X,Y));Assert.IsNull(paint.Mark);}
        }
        [TestCase("unexplored")][TestCase("hidden-render")][TestCase("visual-id")][TestCase("glyph")]
        [TestCase("crop")][TestCase("takeable")][TestCase("duplicate-render")][TestCase("foreign-physics")]
        [TestCase("brain")][TestCase("body")]
        public void PeatMemoryRefusesUnsupportedIdentity(string fault)
        {
            PeatHome();var owner=Add("Grass");zone.GetCell(X,Y).IsVisible=false;NativeTile(View().GroundTile,FenAtlas);
            if(fault=="unexplored")zone.GetCell(X,Y).Explored=false;
            if(fault=="hidden-render")owner.GetPart<RenderPart>().Visible=false;
            if(fault=="visual-id")owner.GetPart<RenderPart>().VisualID="custom";
            if(fault=="glyph")owner.GetPart<RenderPart>().RenderString="!";
            if(fault=="crop")owner.AddPart(new CropPart());
            if(fault=="brain")owner.AddPart(new BrainPart());
            if(fault=="body")owner.AddPart(new Body());
            if(fault=="takeable")owner.GetPart<PhysicsPart>().Takeable=true;
            if(fault=="duplicate-render")owner.AddPart(new RenderPart());
            if(fault=="foreign-physics")owner.GetPart<PhysicsPart>().ParentEntity=content.Factory.CreateEntity("Grass");
            bool ok=world.TryResolvePresentation(zone,X,Y,out var view);
            Assert.False(ok&&view.GroundTile?.sprite?.texture==Resources.Load<Texture2D>(FenAtlas));
        }
        [TestCase("stale")][TestCase("detached")][TestCase("biome")][TestCase("poi")][TestCase("lair")][TestCase("shape")]
        public void PeatMemoryRequiresCurrentWorldAuthority(string fault)
        {
            PeatHome();Add("Grass");zone.GetCell(X,Y).IsVisible=false;NativeTile(View().GroundTile,FenAtlas);
            if(fault=="stale")manager.CachedZones[zone.ZoneID]=new Zone(zone.ZoneID);
            if(fault=="detached")manager.CachedZones.Remove(zone.ZoneID);
            if(fault=="biome")manager.WorldMap.Tiles[17,7]=BiomeType.Spread;
            if(fault=="poi")manager.WorldMap.SetPOI(17,7,new PointOfInterest(POIType.Village,"other"));
            if(fault=="shape")manager.WorldMap.Tiles=new BiomeType[1,1];
            if(fault=="lair"){manager.WorldMap.SetPOI(17,7,new PointOfInterest(POIType.Lair,"old",null,1,"DesertProwler"));LairStacks.Restore(manager,null);manager.WorldMap.SetPOI(17,7,null);}
            bool ok=world.TryResolvePresentation(zone,X,Y,out var v);
            if(fault=="stale"||fault=="detached")Assert.False(ok);
            else { Assert.True(ok);Assert.AreNotSame(Resources.Load<Texture2D>(FenAtlas),v.GroundTile.sprite.texture); }
        }
        [TestCase("Overworld.15.7.0")][TestCase("Overworld.16.7.0")][TestCase("Overworld.017.7.0")][TestCase("Overworld.17.7.1")]
        public void PeatMemoryDoesNotExpandToOtherAddresses(string id)
        {
            zone=Attach(id);Add("Grass");zone.GetCell(X,Y).IsVisible=false;var v=View();
            Assert.AreNotSame(Resources.Load<Texture2D>(FenAtlas),v.GroundTile.sprite.texture);
        }
        [TestCase("Duckboard")][TestCase("PeatBank")][TestCase("DeadTree")][TestCase("Reeds")][TestCase("MirePool")]
        public void PeatMemoryDoesNotPromoteLiveOrAdjacentFamilies(string bp)
        {
            PeatHome();var owner=Add(bp);var live=View().ForegroundTile;zone.GetCell(X,Y).IsVisible=false;
            Assert.True(world.TryResolveFallback(zone,X,Y,out var fallback));var v=View();
            Assert.AreSame(fallback.Owner,v.Owner);Assert.AreSame(fallback.ForegroundTile,v.ForegroundTile);
            Assert.False(v.NativeForeground);Assert.False(v.RepresentedMire);
            bool reader=world.TryGetPresentationSubjectSprite(zone,owner,X,Y,out var portrait,out _);
            Assert.False(reader&&portrait.texture==live.sprite.texture);
        }
        [Test] public void PeatMemoryLegacyInjectedGroundStillUsesItsOwnBorrowedGroup()
        {
            PeatHome();Inject(Resources.Load<Texture2D>(Atlas));Add("Grass");var live=View().GroundTile;
            NativeTile(live,GrassAtlas);zone.GetCell(X,Y).IsVisible=false;Assert.AreSame(live,View().GroundTile);
        }
        [Test] public void PeatMemoryMissingGroupsKeepFallbackWithoutReloadingResources()
        {
            PeatHome();Add("StoneFloor");zone.GetCell(X,Y).IsVisible=false;
            world.Dispose();world=new FlatPixelGreyboxCatalog((Texture2D)null);injectedSelector=new FlatPixelFallbackCatalog();
            injectedAdapter=new FlatPixelSoddenTravelCatalog(null,null,null,null,null,injectedSelector);
            Write(world,"_soddenTravelArt",injectedAdapter);var v=View();
            Assert.AreNotSame(Resources.Load<Texture2D>("Sprites/Live16x24/cellar-floor"),v.GroundTile.sprite.texture);
        }
    }
}
