using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class MundaneCropWateringAdversarialTests : ConnectedCropTimeTestBase
    {
        Entity player, cropOwner, water;
        CropPart crop;
        const string Prefix = "WaterCrop|";
        string Command => Prefix + Uri.EscapeDataString(water.ID);

        [SetUp] public void Prepare()
        {
            player = Factory.CreateEntity("Player"); Assert.True(Zone.AddEntity(player, 4, 5));
            var soil = Factory.CreateEntity("Grass"); soil.AddPart(new CultivatedSoilPart()); soil.SetTag("Plantable");
            Assert.True(Zone.AddEntity(soil, 5, 5));
            cropOwner = Crop(water: 0, stageLength: 30); crop = cropOwner.GetPart<CropPart>();
            Assert.True(Reconcile(crop, Zone, 0));
            water = Factory.CreateEntity("Waterskin"); water.GetPart<WaterskinPart>().Charges = 3;
            Assert.True(player.GetPart<InventoryPart>().AddObject(water));
        }
        bool Act(string command = null) => InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(cropOwner, command ?? Command), player, Zone).Success;
        int Rows() => WorldInteractionSystem.GatherActions(cropOwner, player).Count(a => a.Command.StartsWith(Prefix, StringComparison.Ordinal));
        void Unchanged() { Assert.AreEqual(3, water.GetPart<WaterskinPart>().Charges); Assert.AreEqual(0, crop.MoistureTicks); }

        [TestCase("WaterCrop|")] [TestCase("WaterCrop|missing")]
        [TestCase("WaterCrop||")] [TestCase("WaterCrop|%")]
        [TestCase("WaterCrop|%00")] [TestCase("WaterCrop|ignored|extra")]
        public void MalformedOrUnknownSelectionRefusesWithoutPayment(string command)
        {
            Assert.False(Act(command)); Unchanged();
            Assert.True(Act(), "The identical physical setup remains a usable counter to parser rejection.");
        }

        [TestCase("ground")] [TestCase("foreign-backlink")]
        [TestCase("equipped")] [TestCase("stack")]
        [TestCase("two-parts")] [TestCase("duplicate-id")]
        public void AmbiguousOrNonCarriedSourceCannotSpendWater(string fault)
        {
            Assert.AreEqual(1, Rows());
            if (fault == "ground")
            {
                Assert.True(player.GetPart<InventoryPart>().RemoveObject(water));
                Assert.True(Zone.AddEntity(water, 4, 5));
            }
            if (fault == "foreign-backlink") water.GetPart<PhysicsPart>().InInventory = new Entity();
            if (fault == "equipped") water.GetPart<PhysicsPart>().Equipped = player;
            if (fault == "stack") water.AddPart(new StackerPart { StackCount = 2 });
            if (fault == "two-parts") water.AddPart(new LiquidVesselPart { LiquidId = "water", Volume = 3, Capacity = 12 });
            if (fault == "duplicate-id")
            {
                var other = Factory.CreateEntity("Waterskin"); other.ID = water.ID; other.GetPart<WaterskinPart>().Charges = 3;
                Assert.True(player.GetPart<InventoryPart>().AddObject(other));
            }
            Assert.AreEqual(0, Rows()); Assert.False(Act()); Unchanged();
        }

        [TestCase(-1)] [TestCase(4)]
        public void InvalidWaterQuantityCannotBeNormalizedIntoAFreeSupply(int charges)
        {
            water.GetPart<WaterskinPart>().Charges = charges;
            Assert.AreEqual(0, Rows()); Assert.False(Act());
            Assert.AreEqual(charges, water.GetPart<WaterskinPart>().Charges); Assert.AreEqual(0, crop.MoistureTicks);
        }

        [TestCase(false)] [TestCase(true)]
        public void ActionBlockingStatusControlsRealSelectionWithoutAdvancingTime(bool blocked)
        {
            if (blocked) Assert.True(player.ApplyEffect(new ParalyzedEffect(3)));
            Assert.AreEqual(blocked ? 0 : 1, Rows()); Assert.AreEqual(!blocked, Act());
            Assert.AreEqual(blocked ? 3 : 2, water.GetPart<WaterskinPart>().Charges);
            Assert.AreEqual(blocked ? 0 : 40, crop.MoistureTicks); Assert.AreEqual(0, Clock.TickCount);
        }

        [Test] public void ActorlessWorldQueryIsPureAndNeverOffersUnownedWater()
        {
            Assert.False(WorldInteractionSystem.GatherActions(cropOwner).Any(a => a.Command.StartsWith(Prefix, StringComparison.Ordinal)));
            Unchanged(); Assert.AreEqual(1, Rows());
        }

        [Test] public void SameNameVesselsExposeTheirActualDifferentRemainingSupplies()
        {
            var partial = Factory.CreateEntity("Waterskin"); partial.GetPart<WaterskinPart>().Charges = 1;
            Assert.True(player.GetPart<InventoryPart>().AddObject(partial));
            var rows = WorldInteractionSystem.GatherActions(cropOwner, player)
                .Where(a => a.Command.StartsWith(Prefix, StringComparison.Ordinal)).ToArray();
            Assert.AreEqual(2, rows.Length);
            StringAssert.Contains("(1 of 3)", rows.Single(a => a.Command == Command).Display);
            StringAssert.Contains("(1 of 1)", rows.Single(a => a.Command == Prefix + Uri.EscapeDataString(partial.ID)).Display);
            Assert.True(rows.All(a => a.Display.Length <= 41), "The actual world menu shows the entire finite cost.");
            Unchanged(); Assert.AreEqual(1, partial.GetPart<WaterskinPart>().Charges);
        }

        [Test] public void OuterFailureAfterOldWaterExpiresPreservesDryStateAndDiscardsDryFraction()
        {
            crop.Water(1); Clock.AdvanceClock(109); player.AddPart(new ThrowAfter());
            Assert.False(Act());
            Assert.AreEqual(3, water.GetPart<WaterskinPart>().Charges);
            AssertState(crop, 0, 1, 0); Assert.AreEqual(109, crop.LastGrowthWorldTick);
            Assert.AreEqual(0, crop.GrowthWetTickRemainder);
            Assert.True(string.IsNullOrEmpty(cropOwner.GetPart<RenderPart>().BackgroundColor));
        }

        [Test] public void LegacyMaturityFactoryRemovingTheCropCannotChargeItsSelectedWater()
        {
            crop.HarvestAtMaturity = false; crop.GrowthStage = 1; crop.TicksInStage = 29; crop.Water(1);
            int callbacks = 0;
            HookOutput(_ => { callbacks++; Zone.RemoveEntity(cropOwner); });
            Clock.AdvanceClock(10);
            Assert.False(Act()); Assert.Greater(callbacks, 0, "Watering must first settle genuinely due legacy growth.");
            Assert.AreEqual(3, water.GetPart<WaterskinPart>().Charges);
            Assert.IsNull(Zone.GetEntityCell(cropOwner)); Assert.AreEqual(0, Count("CandyCarrot"));
        }

        [TestCase("future")] [TestCase("fraction")] [TestCase("version")]
        public void InvalidCropClockIsNotRepairedByPayingWater(string fault)
        {
            if (fault == "future") crop.LastGrowthWorldTick = 10;
            if (fault == "fraction") crop.GrowthWetTickRemainder = 9;
            if (fault == "version") crop.GrowthTimingVersion = 99;
            string before = crop.LastGrowthWorldTick + ":" + crop.GrowthWetTickRemainder + ":" + crop.GrowthTimingVersion;
            Assert.AreEqual(0, Rows()); Assert.False(Act()); Unchanged();
            Assert.AreEqual(before, crop.LastGrowthWorldTick + ":" + crop.GrowthWetTickRemainder + ":" + crop.GrowthTimingVersion);
        }

        [TestCase(false)] [TestCase(true)]
        public void SuccessReceiptAndGesturePublishOnlyForCommittedPaidSupply(bool outerFailure)
        {
            bool enabled = Diag.IsChannelEnabled("crop"); Diag.SetChannel("crop", true);
            var oldGesture = EntityVisualHooks.InteractionCallback; int gestures = 0;
            EntityVisualHooks.InteractionCallback = (actor, target, zone) => { if (actor == player && target == cropOwner) gestures++; };
            try
            {
                if (outerFailure) player.AddPart(new ThrowAfter());
                // Factories restart numeric IDs in each fixture. Scope receipts
                // to this action, not older buffer records with the same IDs.
                var earlier = Diag.Snapshot(Diag.BufferCapacity).Select(r => r.TraceId).ToHashSet();
                Assert.AreEqual(!outerFailure, Act());
                Assert.AreEqual(outerFailure ? 0 : 1, gestures);
                Assert.AreEqual(outerFailure ? 0 : 1, Diag.Snapshot(Diag.BufferCapacity)
                    .Count(r => !earlier.Contains(r.TraceId) && r.Kind == "HandWatered" && r.ActorId == player.ID && r.TargetId == cropOwner.ID));
                if (outerFailure) Unchanged();
                else
                {
                    earlier = Diag.Snapshot(Diag.BufferCapacity).Select(r => r.TraceId).ToHashSet();
                    Assert.False(Act());
                    Assert.True(Diag.Snapshot(Diag.BufferCapacity).Any(r => !earlier.Contains(r.TraceId) && r.Kind == "HandWaterRejected" && r.ActorId == player.ID && r.TargetId == cropOwner.ID));
                    Assert.AreEqual(2, water.GetPart<WaterskinPart>().Charges);
                }
            }
            finally { EntityVisualHooks.InteractionCallback = oldGesture; Diag.SetChannel("crop", enabled); }
        }

        [Test] public void SameActorCannotSpendAnotherVesselInsidePendingWatering()
        {
            var otherWater = Factory.CreateEntity("Waterskin"); otherWater.GetPart<WaterskinPart>().Charges = 3;
            Assert.True(player.GetPart<InventoryPart>().AddObject(otherWater));
            var second = Crop(water: 0, stageLength: 30, x: 3);
            var soil = Factory.CreateEntity("Grass"); soil.AddPart(new CultivatedSoilPart()); soil.SetTag("Plantable");
            Assert.True(Zone.AddEntity(soil, 3, 5));
            var probe = new NestedWater { Target = second, Vessel = otherWater, Zone = Zone }; player.AddPart(probe);
            Assert.True(Act()); Assert.False(probe.NestedSucceeded); Assert.True(probe.Attempted);
            Assert.AreEqual(2, water.GetPart<WaterskinPart>().Charges);
            Assert.AreEqual(3, otherWater.GetPart<WaterskinPart>().Charges);
            Assert.AreEqual(0, second.GetPart<CropPart>().MoistureTicks);
        }

        [TestCase(false)] [TestCase(true)]
        public void ActualWellRefillFeedsPreparedCropOnlyAfterWellFunctions(bool repaired)
        {
            var well = Factory.CreateEntity("RepairLinedWell"); well.GetPart<RepairablePart>().Repaired = repaired;
            Assert.True(Zone.AddEntity(well, 4, 4)); water.GetPart<WaterskinPart>().Charges = 0;
            Assert.AreEqual(repaired, InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(water, "FillWaterskin"), player, Zone).Success);
            Assert.AreEqual(repaired, Act()); Assert.AreEqual(repaired ? 2 : 0, water.GetPart<WaterskinPart>().Charges);
            Assert.AreEqual(repaired ? 40 : 0, crop.MoistureTicks);
        }

        sealed class ThrowAfter : Part
        {
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "AfterInventoryAction" && e.GetStringParameter("Command")?.StartsWith(Prefix, StringComparison.Ordinal) == true)
                    throw new InvalidOperationException("Do not publish a rolled-back watering.");
                return true;
            }
        }
        sealed class NestedWater : Part
        {
            public Entity Target, Vessel; public Zone Zone; public bool Attempted, NestedSucceeded;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "AfterInventoryAction" && !Attempted && e.GetStringParameter("Command")?.StartsWith(Prefix, StringComparison.Ordinal) == true)
                {
                    Attempted = true;
                    NestedSucceeded = InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(Target,
                        Prefix + Uri.EscapeDataString(Vessel.ID)), ParentEntity, Zone).Success;
                }
                return true;
            }
        }
    }
}
