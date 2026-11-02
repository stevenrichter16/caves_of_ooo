using CavesOfOoo.Core;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed partial class FlatPixelGrovelandsMaterialsTests
    {
        [TestCase("Grass")][TestCase("VineWall")]
        public void StaticMemoryUsesApprovedGroundOrIsolatedVineAndSingleTint(string name)
        {
            var owner=Add(name);var live=View();bool floor=name=="Grass";
            NativeTile(floor?live.GroundTile:live.ForegroundTile,ResourceFor(name));
            zone.GetCell(X,Y).IsVisible=false;var memory=View();
            var tile=floor?memory.GroundTile:memory.ForegroundTile;
            NativeTile(tile,ResourceFor(name));Assert.True(memory.Remembered);Assert.False(memory.BlocksMovement);
            Assert.AreSame(owner,memory.Owner);Assert.AreEqual(new Color(.35f,.35f,.35f,1),floor?memory.GroundColor:memory.ForegroundColor);
            Assert.AreEqual(floor?live.GroundTile.sprite.rect:new Rect(0,0,16,24),tile.sprite.rect);
            bool reader=world.TryGetPresentationSubjectSprite(zone,owner,X,Y,out var portrait,out _);
            Assert.False(reader && portrait.texture==Resources.Load<Texture2D>(ResourceFor(name)));
            Assert.False(memory.RepresentedMire);Assert.False(memory.RepresentedBrine);
            zone.GetCell(X,Y).IsVisible=true;var again=View();Assert.AreSame(floor?live.GroundTile:live.ForegroundTile,floor?again.GroundTile:again.ForegroundTile);
        }
        [TestCase("Grass")][TestCase("VineWall")]
        public void StaticMemoryIgnoresHiddenConditionsAndNeighbors(string name)
        {
            var owner=Add(name);zone.GetCell(X,Y).IsVisible=false;var first=View();bool floor=name=="Grass";
            var tile=floor?first.GroundTile:first.ForegroundTile;NativeTile(tile,ResourceFor(name));
            owner.GetPart<PhysicsPart>().Solid=!owner.GetPart<PhysicsPart>().Solid;
            if(owner.GetPart<DestructiblePart>() is DestructiblePart d)d.HP=0;
            if(owner.GetPart<ThermalPart>() is ThermalPart t)t.Temperature=1000;
            zone.TileState.AddHeat(X,Y,3);zone.TileState.AddCharge(X,Y,2);zone.TileState.WriteResidue(X,Y,"embers",4);
            Add("VineWall",X+1,Y);Add("VineWall",X,Y+1);Add("Player");
            var after=View();Assert.AreSame(owner,after.Owner);Assert.AreSame(tile,floor?after.GroundTile:after.ForegroundTile);
            Assert.AreEqual(first.GroundColor,after.GroundColor);Assert.AreEqual(first.ForegroundColor,after.ForegroundColor);
        }
        [TestCase("unexplored")][TestCase("hidden-render")][TestCase("visual-id")]
        [TestCase("glyph")][TestCase("crop")][TestCase("takeable")][TestCase("stale")][TestCase("site")]
        public void StaticMemoryRefusesUnsupportedOwnership(string fault)
        {
            var owner=Add("Grass");NativeTile(View().GroundTile,ForestGround);zone.GetCell(X,Y).IsVisible=false;
            if(fault=="unexplored")zone.GetCell(X,Y).Explored=false;
            if(fault=="hidden-render")owner.GetPart<RenderPart>().Visible=false;
            if(fault=="visual-id")owner.GetPart<RenderPart>().VisualID="custom";
            if(fault=="glyph")owner.GetPart<RenderPart>().RenderString="!";
            if(fault=="crop")owner.AddPart(new CropPart());
            if(fault=="takeable")owner.GetPart<PhysicsPart>().Takeable=true;
            if(fault=="stale")manager.SetActiveZone(new Zone(Home));
            if(fault=="site")manager.WorldMap.SetPOI(4,7,new PointOfInterest(POIType.MerchantCamp,"counter"));
            bool ok=world.TryResolvePresentation(zone,X,Y,out var result);
            Assert.False(ok&&result.GroundTile?.sprite?.texture==Resources.Load<Texture2D>(ForestGround));
        }
        [Test]public void StaticMemoryOptionalGroupsRetainDeclaredFallback()
        {
            Add("Grass");zone.GetCell(X,Y).IsVisible=false;
            InjectForestFloor(null);NativeTile(View().GroundTile,Sill);
            InjectForestFloor(null,false);Assert.False(View().GroundTile?.sprite?.texture==Resources.Load<Texture2D>(ForestGround));
            InjectForestFloor(Resources.Load<Texture2D>(ForestGround),false);NativeTile(View().GroundTile,ForestGround);
            injectedAdapter.Dispose();Assert.False(View().GroundTile?.sprite?.texture==Resources.Load<Texture2D>(ForestGround));
        }
        [TestCase("Tree")][TestCase("Bush")][TestCase("GroveSeep")][TestCase("CompostRow")]
        public void StaticMemoryKeepsOtherFamiliesOnFallback(string name)
        {
            Add("Grass");var owner=Add(name);zone.GetCell(X,Y).IsVisible=false;
            Assert.True(world.TryResolveFallback(zone,X,Y,out var fallback));var memory=View();Assert.AreSame(fallback.Owner,memory.Owner);
            Assert.False(memory.NativeForeground);Assert.False(memory.RepresentedMire);
            bool reader=world.TryGetPresentationSubjectSprite(zone,owner,X,Y,out var portrait,out _);
            Assert.False(reader && portrait.texture==Resources.Load<Texture2D>(ResourceFor(name)));
        }
    }
}
