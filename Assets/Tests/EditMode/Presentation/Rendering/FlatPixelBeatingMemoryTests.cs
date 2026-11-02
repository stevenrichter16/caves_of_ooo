using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    public sealed partial class FlatPixelBeatingMaterialsTests
    {
        [TestCase("Sand")] [TestCase("SaltCrust")] [TestCase("SandstoneFloor")]
        [TestCase("RoadStone")] [TestCase("SandstoneWall")] [TestCase("StoneWall")]
        public void MemoryStaticMaterialKeepsFrameWithSingleDimAndNoReader(string blueprint)
        {
            var owner = Add(blueprint); var live = View(); bool floor = IsFloor(blueprint);
            var liveTile = floor ? live.GroundTile : live.ForegroundTile;
            NativeTile(liveTile, blueprint == "StoneWall" ? "Sprites/Live16x24/sill-environment" : Atlas);
            if(blueprint == "StoneWall") Assert.AreEqual(8,(int)liveTile.sprite.rect.x / 16);
            else AssertFamilyFrame(owner,liveTile);
            var texture = liveTile.sprite.texture; var rect = liveTile.sprite.rect;
            zone.GetCell(X,Y).IsVisible = false;
            var memory = View(); var tile = floor ? memory.GroundTile : memory.ForegroundTile;
            Assert.True(memory.Remembered); Assert.False(memory.BlocksMovement);
            Assert.AreSame(owner,memory.Owner); Assert.NotNull(tile);
            Assert.AreSame(texture,tile.sprite.texture); Assert.AreEqual(rect,tile.sprite.rect);
            Assert.AreEqual(new Color(.35f,.35f,.35f,1f),floor ? memory.GroundColor : memory.ForegroundColor);
            if(floor) { Assert.True(memory.LocalGround); Assert.IsNull(memory.ForegroundTile); }
            Assert.False(Subject(owner)); Assert.False(memory.RepresentedBrine);
            zone.GetCell(X,Y).IsVisible = true;
            var again=View();Assert.AreSame(liveTile,floor?again.GroundTile:again.ForegroundTile);
        }

        [TestCase("Sand")] [TestCase("SandstoneWall")]
        public void MemoryIgnoresHiddenLiveStateAndOccupants(string blueprint)
        {
            var owner=Add(blueprint);zone.GetCell(X,Y).IsVisible=false;
            var before=View();bool floor=IsFloor(blueprint);var tile=floor?before.GroundTile:before.ForegroundTile;
            Assert.AreSame(Resources.Load<Texture2D>(Atlas),tile.sprite.texture);
            owner.GetPart<PhysicsPart>().Solid=!owner.GetPart<PhysicsPart>().Solid;
            if(owner.GetPart<DestructiblePart>() is DestructiblePart wall) wall.HP=0;
            if(owner.GetPart<ThermalPart>() is ThermalPart thermal) thermal.Temperature=1000;
            zone.TileState.AddHeat(X,Y,3);zone.TileState.AddCharge(X,Y,4);zone.TileState.WriteResidue(X,Y,"embers",5);
            var actor=Add("Player");actor.GetPart<RenderPart>().RenderLayer=100;
            Add("Rock",X+1,Y);
            var after=View();Assert.AreSame(owner,after.Owner);Assert.AreSame(tile,floor?after.GroundTile:after.ForegroundTile);
            Assert.AreEqual(before.GroundColor,after.GroundColor);Assert.AreEqual(before.ForegroundColor,after.ForegroundColor);
            using(var paint=new Paint(world,zone))
            { paint.Step(); Assert.AreSame(tile,floor?paint.GroundAt(X,Y):paint.At(X,Y));Assert.IsNull(paint.Mark); }
        }

        [TestCase("unexplored")] [TestCase("hidden-render")] [TestCase("visual-id")]
        [TestCase("glyph")] [TestCase("crop")] [TestCase("takeable")]
        [TestCase("stale-graph")] [TestCase("missing-atlas")]
        public void MemoryRefusesUnownedOrUnsupportedIdentity(string fault)
        {
            var owner=Add("Sand");Native(owner);zone.GetCell(X,Y).IsVisible=false;
            if(fault=="unexplored")zone.GetCell(X,Y).Explored=false;
            if(fault=="hidden-render")owner.GetPart<RenderPart>().Visible=false;
            if(fault=="visual-id")owner.GetPart<RenderPart>().VisualID="custom";
            if(fault=="glyph")owner.GetPart<RenderPart>().RenderString="!";
            if(fault=="crop")owner.AddPart(new CropPart());
            if(fault=="takeable")owner.GetPart<PhysicsPart>().Takeable=true;
            if(fault=="stale-graph")manager.SetActiveZone(new Zone(Home));
            if(fault=="missing-atlas")Inject(null);
            bool ok=world.TryResolvePresentation(zone,X,Y,out var result);
            Assert.False(ok && result.GroundTile?.sprite?.texture==Resources.Load<Texture2D>(Atlas));
        }

        [TestCase("BrinePool")] [TestCase("TarSeep")] [TestCase("Bones")]
        [TestCase("Saltbriar")] [TestCase("DryBrush")] [TestCase("Rock")]
        public void MemoryNeverPromotesLiveFamilies(string blueprint)
        {
            Add("Sand");var owner=Add(blueprint);zone.GetCell(X,Y).IsVisible=false;
            var memory=View();Assert.True(world.TryResolveFallback(zone,X,Y,out var fallback));
            Assert.AreSame(fallback.Owner,memory.Owner);Assert.False(memory.NativeForeground);
            Assert.False(memory.RepresentedBrine);Assert.False(memory.RepresentedMire);
            bool reader=world.TryGetPresentationSubjectSprite(zone,owner,X,Y,out var sprite,out _);
            Assert.False(reader && sprite.texture==Resources.Load<Texture2D>(ResourceFor(blueprint)));
        }
    }
}
