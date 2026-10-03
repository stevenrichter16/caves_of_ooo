using System;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class ConnectedCropTimeAdversarialTests : ConnectedCropTimeTestBase
    {
        [Test] public void FutureStampRefusesWithoutWaterOrProgressRepair()
        {
            var crop = Crop().GetPart<CropPart>(); Reconcile(crop, Zone, 100);
            Assert.False(Reconcile(crop, Zone, 99)); AssertState(crop, 0, 0, 40);
            Assert.AreEqual(100, Read(crop, "LastGrowthWorldTick"));
            crop.Water(80); Assert.AreEqual(40, crop.MoistureTicks, "invalid future stamp cannot be silently repaired by watering");
        }

        [TestCase(-1)] [TestCase(10)] [TestCase(int.MaxValue)]
        public void InvalidFractionRefusesRatherThanGrantingGrowth(int fraction)
        {
            var crop = Crop().GetPart<CropPart>(); Reconcile(crop, Zone, 0);
            Write(crop, "GrowthWetTickRemainder", fraction);
            Assert.False(Reconcile(crop, Zone, 100)); AssertState(crop, 0, 0, 40);
            Assert.AreEqual(0, Read(crop, "LastGrowthWorldTick"));
        }

        [TestCase(-1)] [TestCase(3)]
        public void InvalidStageDoesNotConsumeMoisture(int stage)
        {
            var crop = Crop().GetPart<CropPart>(); Reconcile(crop, Zone, 0); crop.GrowthStage = stage;
            Assert.False(Reconcile(crop, Zone, 100)); Assert.AreEqual(40, crop.MoistureTicks);
        }

        [Test] public void RemovedOrForeignOwnerCannotAdvance()
        {
            var owner = Crop(); var crop = owner.GetPart<CropPart>(); Reconcile(crop, Zone, 0);
            Zone.RemoveEntity(owner); Assert.False(Reconcile(crop, Zone, 100));
            var elsewhere = new Zone("elsewhere"); Assert.True(elsewhere.AddEntity(owner, 5, 5));
            Assert.False(Reconcile(crop, Zone, 100)); AssertState(crop, 0, 0, 40);
        }

        [Test] public void ReplacedPartCannotAdvanceOrDrainCurrentOwner()
        {
            var owner = Crop(); var old = owner.GetPart<CropPart>(); Reconcile(old, Zone, 0);
            owner.RemovePart(old); owner.AddPart(new CropPart { MoistureTicks = 17 });
            Assert.False(Reconcile(old, Zone, 100)); AssertState(old, 0, 0, 40);
            Assert.AreEqual(17, owner.GetPart<CropPart>().MoistureTicks);
        }

        [Test] public void NegativeClockRefusesWithoutInitializingLegacyCrop()
        {
            var crop = Crop().GetPart<CropPart>(); Assert.False(Reconcile(crop, Zone, -1));
            AssertState(crop, 0, 0, 40); Assert.AreEqual(0, Read(crop, "GrowthTimingVersion"));
        }

        [Test] public void LargeElapsedTimeIsBoundedByMoistureAndStages()
        {
            var crop = Crop(water: int.MaxValue, stageLength: int.MaxValue).GetPart<CropPart>();
            Reconcile(crop, Zone, 0); Reconcile(crop, Zone, int.MaxValue);
            AssertState(crop, 0, int.MaxValue / 10, int.MaxValue - int.MaxValue / 10);
            Assert.AreEqual(int.MaxValue % 10, Read(crop, "GrowthWetTickRemainder"));
        }

        [Test] public void MissingYieldFactoryConsumesOnlyElapsedWetUnits_ThenRecoversAfterWater()
        {
            var owner = Crop(false, water: 4, stageLength: 2); var crop = owner.GetPart<CropPart>();
            Reconcile(crop, Zone, 0); CropSystem.Factory = null; Reconcile(crop, Zone, 100);
            Assert.NotNull(Zone.GetEntityCell(owner)); AssertState(crop, 1, 2, 0);
            CropSystem.Factory = Factory; Reconcile(crop, Zone, 110);
            Assert.AreEqual(0, Count("CandyCarrot"), "dry held legacy crops do not retry for free");
            Clock.AdvanceClock(110); crop.Water(1); Reconcile(crop, Zone, 120);
            Assert.IsNull(Zone.GetEntityCell(owner)); Assert.AreEqual(2, Count("CandyCarrot"));
        }

        [Test] public void ReentrantYieldFactoryCannotReplayElapsedTimeOrDuplicateProduce()
        {
            var owner = Crop(false, water: 40, stageLength: 2); var crop = owner.GetPart<CropPart>();
            Reconcile(crop, Zone, 0); int callbacks = 0;
            HookOutput(_ => { callbacks++; Reconcile(crop, Zone, 400); });
            Reconcile(crop, Zone, 400);
            Assert.AreEqual(2, callbacks); Assert.AreEqual(2, Count("CandyCarrot"));
            Assert.IsNull(Zone.GetEntityCell(owner));
        }

        [Test] public void MutatingYieldCallbackCannotPublishFromAReplacedCrop()
        {
            var owner = Crop(false, water: 40, stageLength: 2); var crop = owner.GetPart<CropPart>();
            Reconcile(crop, Zone, 0);
            HookOutput(_ => { Zone.RemoveEntity(owner); });
            Reconcile(crop, Zone, 400);
            Assert.AreEqual(0, Count("CandyCarrot"));
        }

        [Test] public void NpcTickEndDoesNotAdvanceEvenWithElapsedTimeWaiting()
        {
            var crop = Crop().GetPart<CropPart>(); Reconcile(crop, Zone, 0); Clock.AdvanceClock(20);
            var world = new Entity(); world.AddPart(new CropSystemPart());
            var tick = GameEvent.New("TickEnd"); tick.SetParameter("Actor", new Entity());
            try { world.FireEvent(tick); } finally { tick.Release(); }
            AssertState(crop, 0, 0, 40);
            ReconcileZone(Zone, Clock.TickCount); AssertState(crop, 0, 2, 38);
        }

        [TestCase(-1)] [TestCase(2)]
        public void UnknownTimingVersionNeverSilentlyResetsWaterOrClock(int version)
        {
            var crop = Crop().GetPart<CropPart>(); Reconcile(crop, Zone, 0);
            Write(crop, "GrowthTimingVersion", version);
            Assert.False(Reconcile(crop, Zone, 100)); AssertState(crop, 0, 0, 40);
            Assert.AreEqual(0, Read(crop, "LastGrowthWorldTick"));
        }

        [TestCase(0)] [TestCase(-1)]
        public void InvalidStageLengthCannotProduceAnInstantHarvest(int length)
        {
            var owner = Crop(false); var crop = owner.GetPart<CropPart>(); Reconcile(crop, Zone, 0);
            crop.TicksPerStage = length;
            Assert.False(Reconcile(crop, Zone, 100)); Assert.AreEqual(0, Count("CandyCarrot"));
            Assert.NotNull(Zone.GetEntityCell(owner)); Assert.AreEqual(40, crop.MoistureTicks);
        }

        [Test] public void LoadInitializationDoesNotAdvanceAlreadyStampedInactiveGraphs()
        {
            var stamped = Crop().GetPart<CropPart>(); Reconcile(stamped, Zone, 0);
            var legacy = Crop(x: 8).GetPart<CropPart>();
            CropTime.InitializeLegacyZone(Zone, 100);
            AssertState(stamped, 0, 0, 40); Assert.AreEqual(0, stamped.LastGrowthWorldTick);
            AssertState(legacy, 0, 0, 40); Assert.AreEqual(100, legacy.LastGrowthWorldTick);
            ReconcileZone(Zone, 110);
            AssertState(stamped, 0, 11, 29); AssertState(legacy, 0, 1, 39);
        }

        [Test] public void CarriedAliasCannotGrowWhileAlsoListedOnTheGround()
        {
            var owner = Crop(); var crop = owner.GetPart<CropPart>(); Reconcile(crop, Zone, 0);
            owner.GetPart<PhysicsPart>().InInventory = new Entity();
            Assert.False(Reconcile(crop, Zone, 100)); AssertState(crop, 0, 0, 40);
        }
    }
}
