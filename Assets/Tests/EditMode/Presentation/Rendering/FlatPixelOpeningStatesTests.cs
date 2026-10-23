using System;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelOpeningStatesTests
    {
        EntityFactory factory;
        Texture2D atlas, baseArt, cast, anchors;
        FlatPixelGreyboxCatalog catalog;
        Zone zone;
        const int X = 10, Y = 10;
        [OneTimeSetUp] public void Load() => factory = DensityReferenceGladeTests.Factory();
        [SetUp] public void Setup()
        {
            atlas = new Texture2D(192, 24); baseArt = new Texture2D(160, 24);
            cast = new Texture2D(64, 24); anchors = new Texture2D(192, 24);
            zone = new Zone(ReferenceGladePlan.ZoneID);
            zone.GetCell(X, Y).IsVisible = zone.GetCell(X, Y).Explored = true;
        }
        [TearDown] public void Cleanup()
        {
            catalog?.Dispose();
            Object.DestroyImmediate(atlas); Object.DestroyImmediate(baseArt);
            Object.DestroyImmediate(cast); Object.DestroyImmediate(anchors);
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)][TestCase(4)][TestCase(5)]
        [TestCase(6)][TestCase(7)][TestCase(8)][TestCase(9)][TestCase(10)][TestCase(11)]
        public void ExactOwnerAndStateSelectDistinctCachedFrames(int frame)
        {
            var owner = AddFrame(frame); Create(); var result = Resolve(); AssertFrame(result, frame);
            Assert.AreSame(owner, result.Owner); Assert.AreEqual(Color.white, result.ForegroundColor);
            Assert.AreEqual(zone.GetCell(X, Y).BlocksMovement(), result.BlocksMovement);
            Assert.AreSame(result.ForegroundTile, Resolve().ForegroundTile);
        }
        [TestCase(0,"Door")][TestCase(0,"Repairable")][TestCase(4,"Campfire")][TestCase(4,"Thermal")]
        [TestCase(4,"Fuel")][TestCase(8,"Crop")][TestCase(10,"TorchLight")][TestCase(10,"LightSource")]
        [TestCase(10,"Thermal")][TestCase(10,"Fuel")][TestCase(11,"Waterskin")]
        public void MissingAndForeignStatePartsCannotAdvertiseNativeFunction(int frame, string name)
        {
            var owner=AddFrame(frame); Create(); AssertFrame(Resolve(),frame);
            var part=owner.GetPart(name); Assert.NotNull(part); part.ParentEntity=new Entity(); Fallback();
            part.ParentEntity=owner; AssertFrame(Resolve(),frame); Assert.True(owner.RemovePart(part)); Fallback();
        }
        [Test] public void WicketRepairDoesNotOpenItAndLockStillMeansClosed()
        {
            var owner=AddFrame(0); Create(); AssertFrame(Resolve(),0);
            var door=owner.GetPart<DoorPart>(); door.IsOpen=true; AssertFrame(Resolve(),0);
            owner.GetPart<RepairablePart>().Repaired=true; AssertFrame(Resolve(),2);
            var latch=new LockPart{IsLocked=true}; owner.AddPart(latch); AssertFrame(Resolve(),1);
            latch.IsLocked=false; AssertFrame(Resolve(),2);
            door.IsOpen=false; AssertFrame(Resolve(),1);
            owner.GetPart<RepairablePart>().PortableEquipment=true; Fallback();
        }
        [TestCase(149.99f,3)][TestCase(150f,4)][TestCase(500f,4)]
        public void ResidualHeatIsNotDestructiveFireOrCookingService(float temperature,int frame)
        {
            var owner=AddFrame(4); owner.GetPart<ThermalPart>().Temperature=temperature;
            owner.GetPart<FuelPart>().FuelMass=0; Create(); AssertFrame(Resolve(),frame);
            Assert.False(owner.HasEffect<BurningEffect>());
        }
        [TestCase(float.NaN)][TestCase(float.PositiveInfinity)]
        public void NonfiniteHeatCannotPretendToBeResidualCoals(float temperature)
        {var owner=AddFrame(4); owner.GetPart<ThermalPart>().Temperature=temperature; Create(); Fallback();}
        [TestCase(-1)][TestCase(3)]
        public void InvalidCropStageDoesNotMasqueradeAsMaturity(int stage)
        {var owner=AddFrame(8);owner.GetPart<CropPart>().GrowthStage=stage;Create();Fallback();}
        [Test] public void GourdRequiresItsOwnHarvestContractAndDoesNotReplaceOtherCrops()
        {
            var owner=AddFrame(8);Create();AssertFrame(Resolve(),8);
            owner.GetPart<CropPart>().HarvestAtMaturity=false;Fallback();
            owner.GetPart<CropPart>().HarvestAtMaturity=true;owner.GetPart<CropPart>().YieldBlueprint="RawMeat";Fallback();
            owner.GetPart<CropPart>().YieldBlueprint="DrawgourdShell";owner.BlueprintName="EmberWheatCrop";Fallback();
        }
        [TestCase("disabled")][TestCase("cold")][TestCase("empty")][TestCase("no-rate")][TestCase("wet")][TestCase("frozen")]
        public void ControlledTorchUsesActualFlameConditions(string change)
        {
            var owner=AddFrame(10);Create();AssertFrame(Resolve(),10);
            if(change=="disabled")owner.GetPart<LightSourcePart>().Enabled=false;
            if(change=="cold")owner.GetPart<ThermalPart>().Temperature=25;
            if(change=="empty")owner.GetPart<FuelPart>().FuelMass=0;
            if(change=="no-rate")owner.GetPart<FuelPart>().BurnRate=0;
            if(change=="wet")Assert.True(owner.ApplyEffect(new WetEffect(1)));
            if(change=="frozen")Assert.True(owner.ApplyEffect(new FrozenEffect(1)));
            AssertFrame(Resolve(),9);
        }
        [Test] public void DestructiveTorchFireRemainsVisibleWhenControlledLightIsOff()
        {
            var owner=AddFrame(9);Create();AssertFrame(Resolve(),9);
            Assert.True(owner.ApplyEffect(new BurningEffect()));AssertFrame(Resolve(),10);
            Assert.True(owner.RemoveEffect<BurningEffect>());AssertFrame(Resolve(),9);
        }
        [TestCase(5,4)][TestCase(10,9)]
        public void ForeignEffectOwnershipCannotClaimAnActualFlame(int frame,int nonburning)
        {
            var owner=AddFrame(frame);
            if(frame==10){owner.GetPart<LightSourcePart>().Enabled=false;Assert.True(owner.ApplyEffect(new BurningEffect()));}
            Create();AssertFrame(Resolve(),frame);
            var status=owner.GetPart<StatusEffectsPart>();var effect=owner.GetEffect<BurningEffect>();
            status.ParentEntity=new Entity();AssertFrame(Resolve(),nonburning);
            status.ParentEntity=owner;AssertFrame(Resolve(),frame);
            effect.Owner=new Entity();AssertFrame(Resolve(),nonburning);
        }
        [TestCase("Campfire")][TestCase("Torch")]
        public void RememberedLowTerrainDoesNotExposeCurrentFireState(string name)
        {
            var owner=Add(name);owner.GetPart<RenderPart>().RenderLayer=0;owner.Tags.Remove("Item");owner.SetTag("Terrain");
            owner.GetPart<PhysicsPart>().Takeable=false;catalog=new FlatPixelGreyboxCatalog(null);
            Assert.AreSame(owner,Resolve().Owner);zone.GetCell(X,Y).IsVisible=false;
            Assert.IsNull(Resolve().Owner);Assert.IsNull(Resolve().ForegroundTile);
        }
        [Test] public void WaterskinRemainsAVesselWhenEmptiedAndDisappearsWhenPickedUp()
        {
            var owner=AddFrame(11);Create();AssertFrame(Resolve(),11);owner.GetPart<WaterskinPart>().Charges=3;
            var tile=Resolve().ForegroundTile;owner.GetPart<WaterskinPart>().Charges=0;Assert.AreSame(tile,Resolve().ForegroundTile);
            Assert.True(zone.RemoveEntity(owner));Assert.IsNull(Resolve().ForegroundTile);
        }
        [TestCase(false)][TestCase(true)]
        public void MissingOrMalformedStatesPreserveEarlierGroups(bool malformed)
        {
            var invalid=malformed?new Texture2D(191,24):null;
            try
            {
                catalog=NewCatalog(baseArt,cast,anchors,invalid);
                foreach(var pair in new[]{("Player",baseArt),("PetDog",cast),("Chest",anchors)})
                {var owner=Add(pair.Item1);Assert.AreSame(pair.Item2,Resolve().ForegroundTile.sprite.texture);zone.RemoveEntity(owner);}
                AddFrame(4);Fallback();
            }
            finally {if(invalid!=null)Object.DestroyImmediate(invalid);}
        }
        [Test] public void StatesAreIndependentAndEarlierInjectionNeverLoadsThem()
        {
            AddFrame(4);catalog=NewCatalog(null,null,null,atlas);AssertFrame(Resolve(),4);catalog.Dispose();
            catalog=new FlatPixelGreyboxCatalog(baseArt,cast,anchors);Fallback();
        }
        [Test] public void DisposeDoesNotDestroyBorrowedTextureOrPeerFrames()
        {
            AddFrame(4);Create();var tile=Resolve().ForegroundTile;var sprite=tile.sprite;
            using(var peer=NewCatalog(null,null,null,atlas))
            {
                catalog.Dispose();catalog.Dispose();Assert.True(tile==null&&sprite==null);Assert.True(atlas!=null);
                Assert.True(peer.TryResolve(zone,X,Y,out var value));Assert.AreSame(atlas,value.ForegroundTile.sprite.texture);
                Assert.False(catalog.TryResolve(zone,X,Y,out _));
            }
        }
        [Test] public void DefaultCatalogBindsEveryImportedStateWithoutReplacingEarlierGroups()
        {
            var states=Resources.Load<Texture2D>("Sprites/Live16x24/opening-states");
            Assert.NotNull(states,"Shipping states must be imported; synthetic routing is not integration.");
            Assert.AreEqual(192,states.width);Assert.AreEqual(24,states.height);Assert.AreEqual(FilterMode.Point,states.filterMode);
            Assert.AreEqual(TextureWrapMode.Clamp,states.wrapMode);Assert.AreEqual(1,states.mipmapCount);
            catalog=new FlatPixelGreyboxCatalog();
            for(int frame=0;frame<12;frame++)
            {
                var owner=AddFrame(frame);var value=Resolve();Assert.AreSame(states,value.ForegroundTile.sprite.texture);
                Assert.AreEqual(new Rect(frame*16,0,16,24),value.ForegroundTile.sprite.rect);Assert.AreEqual(Color.white,value.ForegroundColor);
                Assert.True(zone.RemoveEntity(owner));
            }
            foreach(var pair in new[]{("Player","opening-base"),("PetDog","opening-cast"),("Chest","opening-anchors")})
            {var owner=Add(pair.Item1);Assert.AreSame(Resources.Load<Texture2D>("Sprites/Live16x24/"+pair.Item2),Resolve().ForegroundTile.sprite.texture);zone.RemoveEntity(owner);}
        }
        [Test] public void RealGeneratedOpeningHasNoInitialForegroundFallbackButUnknownRuntimeOwnersStillDo()
        {
            var textures=new[]{Resources.Load<Texture2D>("Sprites/Live16x24/opening-base"),Resources.Load<Texture2D>("Sprites/Live16x24/opening-cast"),
                Resources.Load<Texture2D>("Sprites/Live16x24/opening-anchors"),Resources.Load<Texture2D>("Sprites/Live16x24/opening-states")};
            foreach(var texture in textures)Assert.NotNull(texture,"All accepted source groups must be imported before coverage can pass.");
            var manager=OverworldZoneManager.CreateDetached(factory,64,enableExploration:true);zone=manager.GetZone(ReferenceGladePlan.ZoneID);
            catalog=new FlatPixelGreyboxCatalog();int foreground=0;
            for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
            {
                var cell=zone.GetCell(x,y);cell.IsVisible=cell.Explored=true;
                Assert.True(catalog.TryResolve(zone,x,y,out var value));Assert.AreSame(cell.GetTopVisibleObject(),value.Owner);
                if(value.ForegroundTile==null){Assert.That(value.Owner?.BlueprintName,Is.EqualTo("Grass").Or.EqualTo("RoadStone"));continue;}
                foreground++;CollectionAssert.Contains(textures,value.ForegroundTile.sprite.texture,value.Owner?.BlueprintName+" at "+x+","+y);
            }
            Assert.Greater(foreground,100,"The positive control must inspect the actual populated opening.");
            var unknown=new Entity{ID="uncatalogued-runtime",BlueprintName="UncataloguedRuntimeObject"};
            unknown.AddPart(new RenderPart{RenderLayer=100,RenderString="?"});unknown.AddPart(new PhysicsPart());
            Assert.True(zone.AddEntity(unknown,X,Y));var fallback=Resolve();Assert.AreSame(unknown,fallback.Owner);
            Assert.NotNull(fallback.ForegroundTile);CollectionAssert.DoesNotContain(textures,fallback.ForegroundTile.sprite.texture);
        }
        Entity AddFrame(int frame)
        {
            var name=frame<3?"GleanersBuckledWicket":frame<6?"Campfire":frame<9?"DrawgourdCrop":frame<11?"Torch":"Waterskin";
            var owner=Add(name);
            if(frame<3){owner.GetPart<RepairablePart>().Repaired=frame>0;owner.GetPart<DoorPart>().IsOpen=frame==2;}
            else if(frame<6){owner.GetPart<CampfirePart>().FiniteCooking=true;owner.GetPart<ThermalPart>().Temperature=frame==3?25:500;if(frame==5)Assert.True(owner.ApplyEffect(new BurningEffect()));}
            else if(frame<9)owner.GetPart<CropPart>().GrowthStage=frame-6;
            else if(frame<11)owner.GetPart<LightSourcePart>().Enabled=frame==10;
            return owner;
        }
        Entity Add(string name){var e=factory.CreateEntity(name);Assert.NotNull(e);Assert.True(zone.AddEntity(e,X,Y));return e;}
        void Create()=>catalog=NewCatalog(baseArt,cast,anchors,atlas);
        internal static FlatPixelGreyboxCatalog NewCatalog(Texture2D a,Texture2D b,Texture2D c,Texture2D d)
        {
            var ctor=typeof(FlatPixelGreyboxCatalog).GetConstructor(new[]{typeof(Texture2D),typeof(Texture2D),typeof(Texture2D),typeof(Texture2D)});
            Assert.NotNull(ctor,"Fourth optional state atlas must not silently consult Resources.");
            return (FlatPixelGreyboxCatalog)ctor.Invoke(new object[]{a,b,c,d});
        }
        FlatPixelGreyboxCell Resolve(){Assert.True(catalog.TryResolve(zone,X,Y,out var value));return value;}
        void Fallback(){var value=Resolve();Assert.NotNull(value.ForegroundTile);Assert.AreNotSame(atlas,value.ForegroundTile.sprite.texture);}
        void AssertFrame(FlatPixelGreyboxCell value,int frame)
        {Assert.NotNull(value.ForegroundTile);Assert.AreSame(atlas,value.ForegroundTile.sprite.texture);Assert.AreEqual(new Rect(frame*16,0,16,24),value.ForegroundTile.sprite.rect);}
    }
}
