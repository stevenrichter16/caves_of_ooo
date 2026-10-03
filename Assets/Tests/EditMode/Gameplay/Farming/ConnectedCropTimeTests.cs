using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    // Reflection keeps the RED suite compilable before the new clock API exists.
    public abstract class ConnectedCropTimeTestBase
    {
        protected EntityFactory Factory;
        protected Zone Zone;
        protected TurnManager Clock;
        EntityFactory oldFactory, oldSeedFactory;
        TurnManager oldClock;
        Zone oldZone;
        static readonly FieldInfo ActiveClock = typeof(TurnManager).GetField("<Active>k__BackingField", BindingFlags.Static | BindingFlags.NonPublic);

        [SetUp] public void SetUp()
        {
            oldFactory = CropSystem.Factory; oldSeedFactory = SeedPart.Factory;
            oldClock = TurnManager.Active; oldZone = SettlementRuntime.ActiveZone;
            Clock = new TurnManager(); Zone = new Zone("connected-crop-time");
            SettlementRuntime.ActiveZone = Zone;
            Factory = new EntityFactory();
            Factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            Factory.RegisterPartType<ConnectedCropTimeCreationProbe>();
            CropSystem.Factory = SeedPart.Factory = Factory;
            ConnectedCropTimeCreationProbe.Created = null;
        }

        [TearDown] public void TearDown()
        {
            CropSystem.Factory = oldFactory; SeedPart.Factory = oldSeedFactory;
            SettlementRuntime.ActiveZone = oldZone; ActiveClock.SetValue(null, oldClock);
            ConnectedCropTimeCreationProbe.Created = null;
        }

        protected Entity Crop(bool standing = true, int water = 40, int stage = 0, int stageLength = 20, int x = 5)
        {
            var owner = Factory.CreateEntity("CandyCarrotCrop");
            var crop = owner.GetPart<CropPart>();
            crop.HarvestAtMaturity = standing; crop.GrowthStage = stage;
            crop.TicksPerStage = stageLength; crop.MoistureTicks = water;
            crop.StageGlyphsRaw = ".,t,T"; crop.StageColorsRaw = "&w,&g,&G";
            if (standing) { crop.SeedYieldBlueprint = "CandyCarrotSeed"; crop.SeedYieldCount = 1; }
            Assert.True(Zone.AddEntity(owner, x, 5));
            return owner;
        }

        protected static FieldInfo Field(string name)
        {
            var field = typeof(CropPart).GetField(name);
            Assert.NotNull(field, "Saved crop timing field is missing: " + name);
            return field;
        }
        protected static int Read(CropPart crop, string name) => (int)Field(name).GetValue(crop);
        protected static void Write(CropPart crop, string name, int value) => Field(name).SetValue(crop, value);
        protected static bool Reconcile(CropPart crop, Zone zone, int tick)
        {
            var type = typeof(CropPart).Assembly.GetType("CavesOfOoo.Core.CropTime");
            Assert.NotNull(type, "CropTime is required for actual elapsed world-time cultivation.");
            var method = type.GetMethod("Reconcile", BindingFlags.Static | BindingFlags.Public, null,
                new[] { typeof(CropPart), typeof(Zone), typeof(int) }, null);
            Assert.NotNull(method, "CropTime.Reconcile(CropPart, Zone, int) is missing.");
            return (bool)method.Invoke(null, new object[] { crop, zone, tick });
        }
        protected static void ReconcileZone(Zone zone, int tick)
        {
            var type = typeof(CropPart).Assembly.GetType("CavesOfOoo.Core.CropTime");
            Assert.NotNull(type, "Zone arrival needs the shared CropTime reconciler.");
            var method = type.GetMethod("ReconcileZone", BindingFlags.Static | BindingFlags.Public, null,
                new[] { typeof(Zone), typeof(int) }, null);
            Assert.NotNull(method); method.Invoke(null, new object[] { zone, tick });
        }
        protected Entity Actor()
        {
            var actor = new Entity { ID = Guid.NewGuid().ToString("N"), BlueprintName = "Player" };
            actor.SetTag("Player"); actor.AddPart(new PhysicsPart()); actor.AddPart(new InventoryPart { MaxWeight = 200 });
            actor.Statistics["Hitpoints"] = new Stat { Owner = actor, Name = "Hitpoints", BaseValue = 30, Max = 30 };
            Assert.True(Zone.AddEntity(actor, 5, 5)); return actor;
        }
        protected int Count(string blueprint) => Zone.GetReadOnlyEntities().Count(e => e.BlueprintName == blueprint);
        protected void HookOutput(Action<Entity> callback)
        {
            Factory.Blueprints["CandyCarrot"].Parts["ConnectedCropTimeCreationProbe"] = new System.Collections.Generic.Dictionary<string, string>();
            ConnectedCropTimeCreationProbe.Created = callback;
        }
        protected static void AssertState(CropPart crop, int stage, int progress, int moisture)
        {
            Assert.AreEqual(stage, crop.GrowthStage, "stage");
            Assert.AreEqual(progress, crop.TicksInStage, "progress");
            Assert.AreEqual(moisture, crop.MoistureTicks, "moisture");
        }
    }

    public sealed class ConnectedCropTimeCreationProbe : Part
    {
        public static Action<Entity> Created;
        public override void Initialize() => Created?.Invoke(ParentEntity);
    }

    public sealed class ConnectedCropTimeTests : ConnectedCropTimeTestBase
    {
        [Test] public void LegacyCropBaselinesAtRestoredTickWithoutRetroactiveGrowth()
        {
            var crop = Crop(water: 23, stage: 1).GetPart<CropPart>(); crop.TicksInStage = 7;
            Assert.True(Reconcile(crop, Zone, 1000)); AssertState(crop, 1, 7, 23);
            Assert.AreEqual(1000, Read(crop, "LastGrowthWorldTick"));
            Assert.True(Reconcile(crop, Zone, 1010)); AssertState(crop, 1, 8, 22);
        }

        [Test] public void TenWetWorldTicksAdvanceOneUnit_RepeatedObservationIsIdempotent()
        {
            var crop = Crop().GetPart<CropPart>(); Reconcile(crop, Zone, 0);
            Reconcile(crop, Zone, 9); AssertState(crop, 0, 0, 40);
            Assert.AreEqual(9, Read(crop, "GrowthWetTickRemainder"));
            Reconcile(crop, Zone, 10); Reconcile(crop, Zone, 10);
            AssertState(crop, 0, 1, 39); Assert.AreEqual(0, Read(crop, "GrowthWetTickRemainder"));
        }

        [Test] public void WateringSettlesPriorDryTime_AndDoesNotBankItsFraction()
        {
            var crop = Crop(water: 1).GetPart<CropPart>(); Reconcile(crop, Zone, 0);
            Clock.AdvanceClock(19); crop.Water(3);
            AssertState(crop, 0, 1, 3); Assert.AreEqual(0, Read(crop, "GrowthWetTickRemainder"));
            Clock.AdvanceClock(1); Reconcile(crop, Zone, Clock.TickCount); AssertState(crop, 0, 1, 3);
            Clock.AdvanceClock(9); Reconcile(crop, Zone, Clock.TickCount); AssertState(crop, 0, 2, 2);
        }

        [Test] public void WateringWhileWetPreservesNineTickFraction_AndUsesMaxTopUp()
        {
            var crop = Crop(water: 2).GetPart<CropPart>(); Reconcile(crop, Zone, 0);
            Clock.AdvanceClock(9); crop.Water(3); crop.Water(3);
            Assert.AreEqual(9, Read(crop, "GrowthWetTickRemainder")); AssertState(crop, 0, 0, 3);
            Clock.AdvanceClock(1); Reconcile(crop, Zone, Clock.TickCount); AssertState(crop, 0, 1, 2);
        }

        [TestCase(0, 5)] [TestCase(0, 40)] [TestCase(1, 40)]
        public void AbsenceMatchesLocalElapsedStepsThroughStagesAndDryOut(int stage, int water)
        {
            var away = Crop(water: water, stage: stage, stageLength: 3).GetPart<CropPart>();
            var local = Crop(water: water, stage: stage, stageLength: 3, x: 8).GetPart<CropPart>();
            Reconcile(away, Zone, 0); Reconcile(local, Zone, 0);
            Reconcile(away, Zone, 507);
            for (int tick = 1; tick <= 507; tick++) Reconcile(local, Zone, tick);
            AssertState(away, local.GrowthStage, local.TicksInStage, local.MoistureTicks);
            Assert.AreEqual(Read(local, "GrowthWetTickRemainder"), Read(away, "GrowthWetTickRemainder"));
            Assert.AreEqual(507, Read(away, "LastGrowthWorldTick"));
        }

        [Test] public void StandingCropRipensOnLastWetUnit_AndRemainsUntilHarvest()
        {
            var owner = Crop(water: 4, stageLength: 2); var crop = owner.GetPart<CropPart>();
            Reconcile(crop, Zone, 0); Reconcile(crop, Zone, 4000);
            AssertState(crop, 2, 0, 0); Assert.AreEqual("T", owner.GetPart<RenderPart>().RenderString);
            Assert.NotNull(Zone.GetEntityCell(owner)); Assert.AreEqual(0, Count("CandyCarrot"));
        }

        [Test] public void LegacyCropProducesOnceOnFinalWetUnit_WithoutReplay()
        {
            var owner = Crop(false, water: 4, stageLength: 2); var crop = owner.GetPart<CropPart>();
            Reconcile(crop, Zone, 0); ReconcileZone(Zone, 4000); ReconcileZone(Zone, 4000);
            Assert.IsNull(Zone.GetEntityCell(owner)); Assert.AreEqual(2, Count("CandyCarrot"));
            Assert.False(Reconcile(crop, Zone, 5000)); Assert.AreEqual(2, Count("CandyCarrot"));
        }

        [Test] public void PendingFractionAndTimestampSurviveActualPartSaveGraph()
        {
            var owner = Crop(water: 10); var crop = owner.GetPart<CropPart>();
            Reconcile(crop, Zone, 100); Reconcile(crop, Zone, 117);
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(owner);
            Zone.RemoveEntity(owner); Assert.True(Zone.AddEntity(loaded, 5, 5));
            var restored = loaded.GetPart<CropPart>();
            Assert.AreEqual(117, Read(restored, "LastGrowthWorldTick"));
            Assert.AreEqual(7, Read(restored, "GrowthWetTickRemainder"));
            Reconcile(restored, Zone, 120); AssertState(restored, 0, 2, 8);
            Reconcile(restored, Zone, 120); AssertState(restored, 0, 2, 8);
        }

        [Test] public void PlantStartsAtCurrentTick_AndCannotInheritEarlierWorldTime()
        {
            var actor = Actor(); Assert.True(Zone.AddEntity(Factory.CreateEntity("Grass"), 5, 5));
            var seed = Factory.CreateEntity("CandyCarrotSeed"); Assert.True(actor.GetPart<InventoryPart>().AddObject(seed));
            Clock.AdvanceClock(1200);
            Assert.True(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(seed, "PlantSeed"), actor, Zone).Success);
            var planted = Zone.GetReadOnlyEntities().Single(e => e.HasPart<CropPart>()).GetPart<CropPart>();
            Assert.AreEqual(1200, Read(planted, "LastGrowthWorldTick"));
            planted.Water(40); Reconcile(planted, Zone, 1200); AssertState(planted, 0, 0, 40);
        }

        [Test] public void HarvestActionSettlesDueGrowthBeforeCheckingRipeState()
        {
            var actor = Actor(); var owner = Crop(water: 4, stageLength: 2);
            Reconcile(owner.GetPart<CropPart>(), Zone, 0); Clock.AdvanceClock(40);
            var result = InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(owner, "HarvestCultivatedCrop"), actor, Zone);
            Assert.True(result.Success, result.ErrorMessage); Assert.AreEqual(2, Count("CandyCarrot"));
            Assert.AreEqual(1, Count("CandyCarrotSeed"));
        }

        [Test] public void PlayerTickEndCannotManufactureTimeBeforeSchedulerAdvances()
        {
            var crop = Crop(water: 5).GetPart<CropPart>(); Reconcile(crop, Zone, 0);
            var world = new Entity(); world.AddPart(new CropSystemPart()); var actor = Actor();
            var tick = GameEvent.New("TickEnd"); tick.SetParameter("Actor", actor);
            try { world.FireEvent(tick); world.FireEvent(tick); } finally { tick.Release(); }
            AssertState(crop, 0, 0, 5);
            Clock.AdvanceClock(20); ReconcileZone(Zone, Clock.TickCount); AssertState(crop, 0, 2, 3);
        }
    }
}
