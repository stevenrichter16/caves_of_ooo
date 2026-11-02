using System;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Shares the real Beating owner/atlas/submitted-renderer fixture, not a mock pipeline.
    public sealed partial class FlatPixelBeatingMaterialsTests
    {
        Entity StumpPool(string id = "Overworld.3.4.0")
        { zone = Attach(id); Assert.True(FlatPixelStumpMaterialsCatalog.IsSupportedZone(zone)||FlatPixelSoddenTravelCatalog.IsSupportedZone(zone)); return Add("BrinePool"); }
        FlatPixelBeatingMaterialsCatalog StumpAdapter()
        { View(); return (FlatPixelBeatingMaterialsCatalog)Read(world, "_stumpBrineArt"); }
        bool StumpSubject(Entity owner, int x = X, int y = Y)
            => world.TryGetPresentationSubjectSprite(zone, owner, x, y, out var sprite, out _)
                && sprite != null && sprite.texture == Resources.Load<Texture2D>(Atlas)
                && sprite.rect.x >= 20*16 && sprite.rect.x < 28*16;

        [TestCase("Overworld.3.4.0", 90)] [TestCase("Overworld.3.4.0", 0)]
        [TestCase("Overworld.2.5.0", 90)] [TestCase("Overworld.2.5.0", 0)]
        [TestCase("Overworld.16.5.0",90)] [TestCase("Overworld.16.5.0",0)]
        public void StumpBrineStockAndCoveredReaderPreserveRealState(string id, int stock)
        {
            var pool = StumpPool(id); pool.GetPart<LiquidPoolPart>().Volume = stock;
            Native(pool); Assert.True(View().RepresentedBrine); var tile = View().ForegroundTile;
            string state = zone.TileState.ToSaveString(); int version = zone.EntityVersion;
            for (int i = 0; i < 12; i++) { Native(pool); Assert.AreSame(tile, View().ForegroundTile); }
            Assert.AreEqual(state, zone.TileState.ToSaveString()); Assert.AreEqual(version, zone.EntityVersion);
            var actor = Add("Player"); actor.GetPart<RenderPart>().RenderLayer = 100;
            Assert.AreSame(actor, View().Owner); Assert.False(View().RepresentedBrine);
            Assert.True(StumpSubject(pool)); Assert.True(zone.TileState.HasCoating(X,Y,"brine"));
            Assert.True(world.TryGetPresentationSubjectSprite(zone,pool,X,Y,out var covered,out _));
            Assert.AreEqual(tile.sprite.rect,covered.rect);
            zone.GetCell(X,Y).IsVisible = false; Assert.False(StumpSubject(pool)); Assert.False(View().RepresentedBrine);
            zone.GetCell(X,Y).IsVisible = true; Assert.True(StumpSubject(pool));
            Assert.True(zone.RemoveEntity(pool)); Assert.False(StumpSubject(pool)); Assert.False(View().RepresentedBrine);
        }

        [TestCase("missing")] [TestCase("expired")] [TestCase("mixture")] [TestCase("ice")]
        [TestCase("wrong-liquid")] [TestCase("negative-stock")] [TestCase("frozen")]
        [TestCase("vapor")] [TestCase("nan-vapor")] [TestCase("electrified")]
        [TestCase("foreign-source")] [TestCase("duplicate-pool")] [TestCase("footprint")]
        [TestCase("hidden")] [TestCase("moved")] [TestCase("variant")]
        [TestCase("mixture","Overworld.16.5.0")] [TestCase("missing","Overworld.16.5.0")]
        [TestCase("vapor","Overworld.16.5.0")] [TestCase("negative-stock","Overworld.16.5.0")]
        [TestCase("hidden","Overworld.16.5.0")] [TestCase("duplicate-pool","Overworld.16.5.0")]
        public void StumpBrineRejectsUnsupportedStateAfterPositiveBaseline(string fault,string id="Overworld.3.4.0")
        {
            var pool = StumpPool(id); Native(pool);
            if (fault == "missing") zone.TileState.Clear(X,Y);
            if (fault == "expired") zone.TileState.Get(X,Y).Coatings[0].Turns = 0;
            if (fault == "mixture") zone.TileState.WriteCoating(X,Y,"oil",3);
            if (fault == "ice") { zone.TileState.Clear(X,Y); zone.TileState.WriteCoating(X,Y,"ice",3); }
            if (fault == "wrong-liquid") pool.GetPart<LiquidPoolPart>().LiquidId = "water";
            if (fault == "negative-stock") pool.GetPart<LiquidPoolPart>().Volume = -1;
            if (fault == "frozen") pool.GetPart<ThermalPart>().Temperature = -8;
            if (fault == "vapor") { var t=pool.GetPart<ThermalPart>(); t.VaporTemperature=50; t.Temperature=50; Assert.Greater(t.FlameTemperature,50); }
            if (fault == "nan-vapor") pool.GetPart<ThermalPart>().VaporTemperature = float.NaN;
            if (fault == "electrified") Assert.True(pool.ForceApplyEffect(new ElectrifiedEffect(1), null, zone));
            if (fault == "foreign-source") pool.GetPart<TileStateSourcePart>().ParentEntity = new Entity();
            if (fault == "duplicate-pool") pool.AddPart(new LiquidPoolPart { LiquidId="brine", Volume=90 });
            if (fault == "footprint") pool.AddPart(new SpatialFootprintPart { CellsRaw="0,0;1,0" });
            if (fault == "hidden") pool.GetPart<RenderPart>().Visible = false;
            if (fault == "moved") { Assert.True(zone.RemoveEntity(pool)); Assert.True(zone.AddEntity(pool,X+1,Y)); }
            if (fault == "variant") pool.GetPart<RenderPart>().VisualVariant = "unknown brine";
            Assert.False(View().RepresentedBrine); Assert.False(StumpSubject(pool));
            if (View().Owner == pool) Assert.False(View().NativeForeground);
        }

        [TestCase(90,"none",'\0')] [TestCase(0,"none",'\0')]
        [TestCase(90,"charge",'*')] [TestCase(0,"heat",'^')]
        [TestCase(90,"embers",'"')] [TestCase(0,"veil-mist",'\'')]
        [TestCase(90,"none",'\0',"Overworld.16.5.0")] [TestCase(0,"charge",'*',"Overworld.16.5.0")]
        [TestCase(90,"heat",'^',"Overworld.16.5.0")]
        public void StumpBrineSubmittedMarksRemainAuthoritative(int stock,string hazard,char expected,string id="Overworld.3.4.0")
        {
            var pool=StumpPool(id); pool.GetPart<LiquidPoolPart>().Volume=stock; Native(pool);
            if(hazard=="charge")zone.TileState.AddCharge(X,Y,1);
            if(hazard=="heat")zone.TileState.AddHeat(X,Y,1);
            if(hazard=="embers")zone.TileState.WriteResidue(X,Y,"embers",3);
            if(hazard=="veil-mist")zone.TileState.WriteCloud(X,Y,"veil-mist",3);
            using(var paint=new Paint(world,zone))
            {
                paint.Step(); NativeTile(paint.At(X,Y),Atlas);
                if(expected=='\0')Assert.IsNull(paint.Mark);else Assert.AreSame(CP437TilesetGenerator.GetTile(expected),paint.Mark);
                if(hazard=="none")
                {
                    var actor=Add("Player");actor.GetPart<RenderPart>().RenderLayer=100;
                    paint.Dirty(X,Y);paint.Step();Assert.True(StumpSubject(pool));
                    Assert.AreSame(CP437TilesetGenerator.GetTile('~'),paint.Mark);
                }
                zone.GetCell(X,Y).IsVisible=false;paint.Dirty(X,Y);paint.Step();Assert.IsNull(paint.Mark);
            }
        }

        [TestCase("Sand")] [TestCase("DryBrush")] [TestCase("TarSeep")] [TestCase("StoneWall")]
        public void StumpBrineInstanceCannotAdmitOtherBeatingFamiliesOrGround(string blueprint)
        {
            var pool=StumpPool();Native(pool);var adapter=StumpAdapter();Assert.NotNull(adapter);
            Assert.True(zone.RemoveEntity(pool));var other=Add(blueprint);
            Assert.False(adapter.TryResolveForeground(zone,X,Y,other,out _));
            Assert.False(adapter.TryGetSubjectSprite(zone,other,X,Y,out _));
            Assert.False(adapter.TryResolveGround(zone,X,Y,out _,out _));
            var actor=Add("Player");actor.GetPart<RenderPart>().RenderLayer=100;
            Assert.False(adapter.TryGetSubjectSprite(zone,other,X,Y,out _));
        }

        [TestCase("Overworld.3.5.0")] [TestCase("Overworld.3.4.1")]
        [TestCase("Overworld.03.4.0")] [TestCase("Overworld.16.13.0")]
        public void StumpBrineInstanceCannotFollowForeignArea(string id)
        {
            var pool=StumpPool();Native(pool);var adapter=StumpAdapter();
            zone=Attach(id);pool=Add("BrinePool");
            Assert.False(adapter.TryResolveForeground(zone,X,Y,pool,out _));
            Assert.False(adapter.TryGetSubjectSprite(zone,pool,X,Y,out _));
        }

        [Test] public void StumpBrineWarmGraphAuthorityIsRechecked()
        {
            var pool=StumpPool();Native(pool);var original=zone;
            manager.SetActiveZone(new Zone(original.ZoneID));
            Assert.False(StumpSubject(pool));
            manager.SetActiveZone(original);Native(pool);
            manager.WorldMap.Tiles[3,4]=BiomeType.Beating;
            Assert.False(StumpSubject(pool));Assert.False(View().RepresentedBrine);
        }

        [TestCase(false)] [TestCase(true)]
        [TestCase(false,"Overworld.16.5.0")] [TestCase(true,"Overworld.16.5.0")]
        public void StumpBrineOptionalAtlasFailureAndDisposalPreserveFallbackAndBorrowedResources(bool malformed,string id="Overworld.3.4.0")
        {
            var pool=StumpPool(id);Native(pool);var first=View().ForegroundTile;var sprite=first.sprite;
            world.Dispose();Assert.True(first==null);Assert.True(sprite==null);
            world=new FlatPixelGreyboxCatalog((Texture2D)null);injectedSelector=new FlatPixelFallbackCatalog();
            Texture2D bad=malformed?new Texture2D(17,24):null;
            try
            {
                var adapter=new FlatPixelBeatingMaterialsCatalog(bad,null,null,injectedSelector);
                injectedAdapter=adapter;var admission=AdapterType().GetMethod(id=="Overworld.16.5.0"?"AdmittingSoddenBrine":"AdmittingStumpBrine",I);
                Assert.NotNull(admission);admission.Invoke(adapter,null);Write(world,id=="Overworld.16.5.0"?"_soddenBrineArt":"_stumpBrineArt",adapter);
                Fallback();Assert.False(StumpSubject(pool));
                using(var paint=new Paint(world,zone))
                {paint.Step();Assert.AreSame(CP437TilesetGenerator.GetTile('~'),paint.Mark);}
                adapter.Dispose();adapter.Dispose();Assert.NotNull(Resources.Load<Texture2D>(Atlas));
                Assert.True(injectedSelector.TryResolve(zone,X,Y,out var fallback));Assert.AreSame(pool,fallback.Owner);
            }
            finally { world.Dispose();if(bad!=null)Object.DestroyImmediate(bad); }
        }

        [Test] public void BeatingBrineVaporBoundaryCannotKeepLiquidPixels()
        {
            var pool=Add("BrinePool");Native(pool);var thermal=pool.GetPart<ThermalPart>();
            thermal.VaporTemperature=50; thermal.Temperature=50; Assert.Greater(thermal.FlameTemperature,50);Fallback();Assert.False(Subject(pool));
        }
    }
}
