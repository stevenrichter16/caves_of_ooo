using System;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed partial class FlatPixelSpreadEnvironmentTests
    {
        [TestCase(0)][TestCase(1)]
        public void OrdinaryReedsReuseApprovedBodyAndActualGrassThroughPainterAndReaders(int parity)
        {
            int x=X+parity;var grass=Add("Grass",x,Y);var reed=Add("Reeds",x,Y);var player=Add("Player",x-1,Y);
            int version=zone.EntityVersion;string state=zone.TileState.ToSaveString();var view=View(x,Y);
            Assert.AreSame(reed,view.Owner);Assert.True(view.NativeForeground,"Reeds still uses ASCII.");
            Frame(view.ForegroundTile,Bush,3+parity,3+parity);Assert.True(view.LocalGround,"Reeds erases its actual grass.");
            Frame(view.GroundTile,Turf,0,3);Assert.False(view.BlocksMovement);
            using(var paint=new Paint(world,zone))
            {
                paint.Step();Frame(paint.At(x,Y),Bush,3+parity,3+parity);Frame(paint.GroundAt(x,Y),Turf,0,3);
                Assert.True(world.TryGetPresentationSubjectSprite(zone,reed,x,Y,out var body,out var tint));
                Assert.AreSame(paint.At(x,Y).sprite,body);Assert.AreEqual(Color.white,tint);
                var doc=ExamineDocumentBuilder.BuildWorld(reed,zone,zone.GetCell(x,Y),player);
                Assert.True(ExamineDocumentBuilder.IsCurrentSubject(doc));Assert.True(paint.Renderer.TryGetFlatExaminePortrait(doc,out var portrait));
                Assert.AreSame(body.texture,portrait.Texture);
            }
            Assert.AreEqual(version,zone.EntityVersion);Assert.AreEqual(state,zone.TileState.ToSaveString());
            Assert.True(zone.MoveEntity(player,x,Y));Assert.AreSame(player,View(x,Y).Owner);Assert.True(View(x,Y).LocalGround);
            using(var paint=new Paint(world,zone))
            {
                paint.Step();var doc=ExamineDocumentBuilder.BuildWorld(reed,zone,zone.GetCell(x,Y),player);
                Assert.True(ExamineDocumentBuilder.IsCurrentSubject(doc));Assert.True(paint.Renderer.TryGetFlatExaminePortrait(doc,out var portrait));
                Assert.AreSame(Art(Bush),portrait.Texture);
                Assert.True(world.TryGetPresentationSubjectSprite(zone,reed,x,Y,out var sprite,out _));
                Assert.AreEqual(new Rect((3+parity)*16,0,16,24),sprite.rect);
            }
            Assert.True(zone.MoveEntity(player,x-1,Y));Frame(View(x,Y).ForegroundTile,Bush,3+parity,3+parity);
            Assert.True(zone.RemoveEntity(grass));Assert.False(View(x,Y).LocalGround,"Never invent turf for a reed-only cell.");
            Assert.True(View(x,Y).NativeForeground);
        }
        [Test]
        public void ReedsKeepRealWaterAndLiveMarksSeparateFromDryGrass()
        {
            Create(inject:true);Add("Grass");var reed=Add("Reeds");Assert.True(ground(zone,X,Y,out _,out _));
            using(var paint=new Paint(world,zone))
            {
                foreach(string coating in new[]{"water","oil"})
                {
                    zone.TileState.WriteCoating(X,Y,coating,4);paint.Dirty(X,Y);paint.Step();
                    Assert.False(ground(zone,X,Y,out _,out _),"Reeds must not gain the fixture hazard exemption.");
                    Assert.False(View().LocalGround);Assert.NotNull(paint.Mark(X,Y));Frame(paint.At(X,Y),Bush,3,3);
                    zone.TileState.RemoveCoating(X,Y,coating);paint.Dirty(X,Y);paint.Step();Frame(paint.GroundAt(X,Y),Turf,0,3);
                }
            }
            foreach(string other in new[]{"WaterPuddle","StoneFloor","CropRow"})
            {var e=Add(other);Assert.False(ground(zone,X,Y,out _,out _),other);zone.RemoveEntity(e);Assert.True(ground(zone,X,Y,out _,out _));}
            var water=Add("WaterPuddle",X+1,Y);var pool=water.GetPart<LiquidPoolPart>();var volume=pool.Volume;
            var before=View(X+1,Y);Assert.False(foreground(zone,X+1,Y,water,out _));Assert.False(ground(zone,X+1,Y,out _,out _));
            Assert.True(foreground(zone,X,Y,reed,out _));Assert.AreEqual(volume,pool.Volume);Assert.AreSame(water,View(X+1,Y).Owner);
            Assert.AreEqual(before.NativeForeground,View(X+1,Y).NativeForeground);
        }
        [Test]
        public void ReedsRequireCurrentDisclosedPlacedOwnerEvenForUnderfootReader()
        {
            Create(inject:true);Add("Grass");var reed=Add("Reeds");var player=Add("Player",X-1,Y);
            Assert.True(foreground(zone,X,Y,reed,out _));var doc=ExamineDocumentBuilder.BuildWorld(reed,zone,zone.GetCell(X,Y),player);
            var cell=zone.GetCell(X,Y);cell.IsVisible=false;Assert.False(foreground(zone,X,Y,reed,out _));Assert.False(portrait(zone,reed,X,Y,out _));Assert.False(View().NativeForeground);
            cell.IsVisible=true;cell.Explored=false;Assert.False(ground(zone,X,Y,out _,out _));Assert.False(portrait(zone,reed,X,Y,out _));cell.Explored=true;
            reed.GetPart<RenderPart>().Visible=false;Assert.False(portrait(zone,reed,X,Y,out _));Assert.True(ground(zone,X,Y,out _,out _));reed.GetPart<RenderPart>().Visible=true;
            Assert.True(zone.MoveEntity(player,X,Y));Assert.False(foreground(zone,X,Y,reed,out _));Assert.True(portrait(zone,reed,X,Y,out _));
            Assert.True(zone.MoveEntity(reed,X+1,Y));Assert.False(portrait(zone,reed,X,Y,out _));Assert.True(zone.MoveEntity(reed,X,Y));
            Assert.True(zone.RemoveEntity(reed));Assert.False(portrait(zone,reed,X,Y,out _));Assert.False(ExamineDocumentBuilder.IsCurrentSubject(doc));
        }
        [TestCase("glyph")][TestCase("variant")][TestCase("color")][TestCase("layer")][TestCase("background")]
        [TestCase("visual")][TestCase("foreign-render")][TestCase("duplicate-render")][TestCase("foreign-physics")]
        [TestCase("carried")][TestCase("equipped")][TestCase("solid")][TestCase("footprint")]
        [TestCase("harvest")][TestCase("source")][TestCase("material")][TestCase("thermal")][TestCase("destructible")]
        public void ReedsRejectChangedMeaningOrForeignOwners(string fault)
        {
            Create(inject:true);Add("Grass");var e=Add("Reeds");Assert.True(foreground(zone,X,Y,e,out _),"Positive control");
            var r=e.GetPart<RenderPart>();var p=e.GetPart<PhysicsPart>();
            switch(fault)
            {
                case "glyph":r.RenderString="?";break;case "variant":r.GlyphVariants="?!";break;case "color":r.ColorString="&R";break;
                case "layer":r.RenderLayer=0;break;case "background":r.BackgroundColor="^r";break;case "visual":r.VisualID="other";break;
                case "foreign-render":r.ParentEntity=new Entity();break;case "duplicate-render":e.Parts.Add(new RenderPart{ParentEntity=e});break;
                case "foreign-physics":p.ParentEntity=new Entity();break;case "carried":p.InInventory=new Entity();break;case "equipped":p.Equipped=new Entity();break;
                case "solid":p.Solid=true;break;case "footprint":e.AddPart(new SpatialFootprintPart{CellsRaw="0,0;1,0"});break;
                case "harvest":e.AddPart(new HarvestablePart());break;case "source":e.AddPart(new TileStateSourcePart{Coating="water",CoatingTurns=4});break;
                case "material":e.AddPart(new MaterialPart{MaterialID="Plant"});break;case "thermal":e.AddPart(new ThermalPart());break;
                case "destructible":e.AddPart(new DestructiblePart());break;
            }
            Assert.False(foreground(zone,X,Y,e,out _),fault);Assert.False(portrait(zone,e,X,Y,out _),fault);Assert.False(View().NativeForeground,fault);
        }
        [Test]
        public void ReedsRemainOrdinarySpreadOnlyEvenWhenTheSiblingCatalogAdmitsASite()
        {
            Create();Add("Grass");var original=Add("Reeds");Assert.True(foreground(zone,X,Y,original,out _));
            Write(adapter,"admits",new Func<Zone,bool>(_=>true));
            foreach(string id in new[]{"Overworld.11.10.0","Overworld.7.8.0","Overworld.15.6.0","Overworld.19.12.0","Overworld.10.10.0","Overworld.10.11.1"})
            {zone=Attach(id);Add("Grass");var e=Add("Reeds");Assert.False(foreground(zone,X,Y,e,out _),id);Assert.False(portrait(zone,e,X,Y,out _),id);Assert.False(ground(zone,X,Y,out _,out _),id);}
            zone=Attach(Home);Add("Grass");var reed=Add("Reeds");manager.WorldMap.Tiles[10,11]=BiomeType.Sodden;
            Assert.False(foreground(zone,X,Y,reed,out _));Assert.False(portrait(zone,reed,X,Y,out _));Assert.False(ground(zone,X,Y,out _,out _));
            manager.WorldMap.Tiles[10,11]=BiomeType.Spread;manager.CachedZones[Home]=new Zone(Home);
            Assert.False(foreground(zone,X,Y,reed,out _));Assert.False(portrait(zone,reed,X,Y,out _));Assert.False(ground(zone,X,Y,out _,out _));
        }
        [Test]
        public void ReedsMissingAtlasKeepsGlyphAndDisposalOwnsOnlyItsCachedViews()
        {
            foreach(bool available in new[]{false,true})
            {
                ReleaseAdapter();Create(new[]{Art(Turf),Art(Road),Art(Hedge),Art(Trees),available?Art(Bush):null},true);
                zone=Attach(Home);Add("Grass");var reed=Add("Reeds");Assert.AreEqual(available,foreground(zone,X,Y,reed,out var body));
                Assert.AreEqual(available,portrait(zone,reed,X,Y,out _));Assert.AreEqual(available,ground(zone,X,Y,out _,out _));
                Assert.AreEqual(available,View().NativeForeground);var hedge=Add("Hedge",X+1,Y);Assert.True(foreground(zone,X+1,Y,hedge,out _));
                if(!available)continue;var sprite=body.sprite;adapter.Dispose();adapter.Dispose();Assert.True(body==null);Assert.True(sprite==null);
                Assert.NotNull(Art(Bush));Assert.True(selector.TryResolve(zone,X,Y,out _));Assert.False(foreground(zone,X,Y,reed,out _));
            }
        }
    }
}
