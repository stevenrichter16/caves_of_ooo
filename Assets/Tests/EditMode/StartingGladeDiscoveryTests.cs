using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class StartingGladeDiscoveryTests
    {
        DensityLootTestScope scope;
        EntityFactory oldHarvestFactory;
        EntityFactory F=>scope.Factory;
        [SetUp] public void Setup(){oldHarvestFactory=HarvestablePart.Factory;scope=new DensityLootTestScope();HarvestablePart.Factory=F;}
        [TearDown] public void Cleanup(){scope.Dispose();HarvestablePart.Factory=oldHarvestFactory;}
        Zone Build(int seed=64){var z=new Zone(ReferenceGladePlan.ZoneID);Assert.True(new ReferenceGladeBuilder(seed).BuildZone(z,F,new Random(12)));return z;}
        static Entity At(Zone z,string bp,int x,int y)=>z.GetCell(x,y).Objects.Single(e=>e.BlueprintName==bp);
        Entity Player(Zone z,int x,int y){var p=F.CreateEntity("Player");Assert.True(z.AddEntity(p,x,y));return p;}

        [TestCase(1)][TestCase(64)][TestCase(1729)]
        public void ReedPondHasSubstantialWaterAndReachableFiniteBasin(int seed)
        {
            var z=Build(seed);int wet=0;
            for(int y=1;y<=9;y++)for(int x=28;x<=39;x++)if(z.TileState.HasCoating(x,y,"water"))wet++;
            Assert.That(wet,Is.InRange(25,65));
            var basin=At(z,"SpreadDrawPoint",36,9);Assert.AreEqual(3,basin.GetPart<LiquidPoolPart>().Volume);
            Assert.True(DensityReferenceGladeTests.Reach(z,40,12).Contains((36,10)));
            Assert.False(z.TileState.HasCoating(36,10,"water"),"dry drinking approach");
        }
        [Test]
        public void PondUsesWetGroundWithoutRiverOnlyAnimatedBankOwners()
        {
            var z=Build();Assert.True(z.TileState.HasCoating(34,5,"water"));
            Assert.False(z.GetReadOnlyEntities().Any(e=>e.BlueprintName=="Bank"),"river bank animation requires river-specific depth data and its opaque fallback obscures water");
            Assert.AreEqual(1,z.GetReadOnlyEntities().Count(e=>e.HasPart<LiquidPoolPart>()),"only the finite draw basin supplies liquid");
        }
        [TestCase(1)][TestCase(64)][TestCase(1729)]
        public void VisibleFieldStripsContainExactlyThreeFiniteGleanings(int seed)
        {
            var z=Build(seed);var rows=z.GetReadOnlyEntities().Where(e=>e.BlueprintName=="CropRow"||e.BlueprintName=="RipeCropRow").ToArray();
            Assert.AreEqual(20,rows.Length);var ripe=rows.Where(e=>e.HasPart<FieldHarvestPart>()).ToArray();Assert.AreEqual(3,ripe.Length);
            Assert.True(ripe.All(e=>!e.GetPart<FieldHarvestPart>().Harvested&&e.GetPart<FieldHarvestPart>().YieldCount==1));
            Assert.AreEqual(1,z.GetReadOnlyEntities().Count(e=>e.HasPart<CampfirePart>()));
        }
        [TestCase(false)][TestCase(true)]
        public void RealGrainHarvestIsFiniteAndRequiresReach(bool remote)
        {
            var z=Build();var row=At(z,"RipeCropRow",44,10);var p=Player(z,remote?40:43,10);var inv=p.GetPart<InventoryPart>();
            int before=inv.Objects.Count(e=>e.BlueprintName=="Emberwheat");
            for(int i=0;i<2;i++){var ev=GameEvent.New("InventoryAction");ev.SetParameter("Command","Harvest");ev.SetParameter("Actor",p);ev.SetParameter("Zone",z);row.FireEvent(ev);}
            Assert.AreEqual(!remote,row.GetPart<FieldHarvestPart>().Harvested);Assert.AreEqual(before+(remote?0:1),inv.Objects.Count(e=>e.BlueprintName=="Emberwheat"));
            Assert.AreSame(row,At(z,"RipeCropRow",44,10),"spent owner remains, no replacement crop");
        }
        [TestCase(false)][TestCase(true)]
        public void NativeCookingUsesRealFoodAndRejectsColdShelter(bool cold)
        {
            var z=Build();var fire=At(z,"Campfire",42,8);Assert.True(fire.GetPart<CampfirePart>().FiniteCooking);
            if(cold)fire.GetPart<ThermalPart>().Temperature=25;
            var p=Player(z,42,9);var food=F.CreateEntity("Emberwheat");p.GetPart<InventoryPart>().AddObject(food);
            Assert.AreEqual(!cold,CookingService.TryCook(p,food,z,F));
            Assert.AreEqual(cold,p.GetPart<InventoryPart>().Objects.Contains(food));
        }
        [TestCase(false)][TestCase(true)]
        public void WaterDrawIsFiniteAndRejectsAnEmptyBasin(bool empty)
        {
            var z=Build();var basin=At(z,"SpreadDrawPoint",36,9);var pool=basin.GetPart<LiquidPoolPart>();if(empty)pool.Volume=0;
            var p=Player(z,36,10);var vessel=F.CreateEntity("Waterskin");p.GetPart<InventoryPart>().AddObject(vessel);var skin=vessel.GetPart<WaterskinPart>();skin.Charges=0;
            Assert.AreEqual(!empty,WaterVesselService.TryAct(p,vessel,z,"FillWaterskin"));Assert.AreEqual(empty?0:3,skin.Charges);Assert.AreEqual(0,pool.Volume);
            Assert.False(WaterVesselService.TryAct(p,vessel,z,"FillWaterskin"),"wet scenery cannot refill the finite source");
        }
        [Test]
        public void BeamReallyHaulsOutOfTheShortcutAndKeepsAnUntouchedAlternative()
        {
            var z=Build();var beam=At(z,"FallenBeam",26,19);var p=Player(z,27,19);
            Assert.True(z.GetCell(26,19).BlocksMovement(p));Assert.True(DensityReferenceGladeTests.Reach(z,27,19).Contains((25,19)),"go around without moving beam");
            Assert.AreEqual(DragVerdict.Ok,DragSystem.TryGrab(p,beam,z));Assert.True(MovementSystem.TryMove(p,z,1,0));DragSystem.Release(p);
            Assert.AreEqual((27,19),z.GetEntityPosition(beam));Assert.False(z.GetCell(26,19).BlocksMovement(p));
        }
        [TestCase("RipeCropRow")][TestCase("Campfire")][TestCase("SpreadDrawPoint")][TestCase("FallenBeam")][TestCase("RoadStone")]
        public void MissingNewContentRefusesBeforePublishingAnything(string bp)
        {F.Blueprints.Remove(bp);var z=new Zone(ReferenceGladePlan.ZoneID);Assert.False(new ReferenceGladeBuilder(64).BuildZone(z,F,new Random(1)));Assert.AreEqual(0,z.EntityCount);Assert.AreEqual(0,z.GenReservedCells.Count);Assert.False(z.TileState.Has(34,5));}
        [TestCase(1)][TestCase(64)][TestCase(1729)]
        public void ArrivalCrossAndEveryExitStayOpenWithOriginalRewardsAndPeople(int seed)
        {
            var z=Build(seed);var reached=DensityReferenceGladeTests.Reach(z,40,12);
            Assert.True(reached.Any(c=>c.Item1==0)&&reached.Any(c=>c.Item1==79)&&reached.Any(c=>c.Item2==0)&&reached.Any(c=>c.Item2==24));
            for(int x=2;x<78;x++)Assert.False(z.GetCell(x,16).BlocksMovement(),"east-west through route "+x);
            Assert.AreEqual(1,z.GetReadOnlyEntities().Count(e=>e.BlueprintName=="Chest"));Assert.AreEqual(2,z.GetReadOnlyEntities().Count(e=>e.BlueprintName=="GlowQuartzVein"));
            Assert.AreEqual(3,z.GetReadOnlyEntities().Count(e=>e.BlueprintName.StartsWith("Marlback")));
            Assert.False(z.GetReadOnlyEntities().Any(e=>e.HasPart<BitLockerPart>()));
        }

        [TestCase(26,14,1)][TestCase(50,7,1)][TestCase(44,6,0)][TestCase(53,5,1)]
        public void VisibleWallOrientationMatchesItsActualBoundary(int x,int y,int quarterTurns)
        {Assert.AreEqual(quarterTurns,At(Build(),"Wall",x,y).GetIntProperty("ReferenceGladeQuarterTurns"));}
        [Test]
        public void BasinHasAnEmptyTakeableVesselWithoutMintingExtraWater()
        {var skin=At(Build(),"Waterskin",37,10);Assert.True(skin.GetPart<PhysicsPart>().Takeable);Assert.AreEqual(0,skin.GetPart<WaterskinPart>().Charges);Assert.AreEqual(3,skin.GetPart<WaterskinPart>().Capacity);}
        [TestCase("Campfire",42,8)][TestCase("SpreadDrawPoint",36,9)][TestCase("FallenBeam",26,19)]
        public void DiscoveriesExplainTheirOwnPracticalUse(string bp,int x,int y)
        {var e=At(Build(),bp,x,y);Assert.That(e.GetPart<ExaminablePart>().Text.Length,Is.GreaterThan(80));Assert.False(string.IsNullOrEmpty(e.GetPart<RenderPart>().DisplayName));}
    }
}
