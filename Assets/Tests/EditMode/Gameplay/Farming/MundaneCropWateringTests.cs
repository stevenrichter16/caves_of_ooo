using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    // These tests intentionally use existing menu and command APIs so the RED
    // fixture compiles before CropWateringService is introduced.
    public sealed class MundaneCropWateringTests : ConnectedCropTimeTestBase
    {
        const string Prefix = "WaterCrop|";
        Entity player;
        [SetUp] public void ResetActor() => player = null;

        Entity Player()
        {
            if (player != null) return player;
            player = Factory.CreateEntity("Player");
            Assert.True(Zone.AddEntity(player, 4, 5));
            return player;
        }

        Entity Planted(bool prepared = true, bool standing = true, int water = 0, int stage = 0, int length = 30)
        {
            var soil = Factory.CreateEntity("Grass");
            soil.SetTag("Plantable");
            if (prepared) soil.AddPart(new CultivatedSoilPart());
            Assert.True(Zone.AddEntity(soil, 5, 5));
            var owner = Crop(standing, water, stage, length);
            Assert.True(Reconcile(owner.GetPart<CropPart>(), Zone, Clock.TickCount));
            if (water > 0) owner.GetPart<CropPart>().Water(water);
            return owner;
        }

        Entity Vessel(string kind = "skin", int units = 3)
        {
            var vessel = Factory.CreateEntity(kind == "flask" ? "LiquidFlask" : kind == "gourd" ? "DrawgourdShell" : "Waterskin");
            Assert.NotNull(vessel);
            if (kind == "flask")
            {
                var liquid = vessel.GetPart<LiquidVesselPart>();
                Assert.NotNull(liquid);
                liquid.LiquidId = units == 0 ? "" : "water";
                liquid.Volume = units;
            }
            else vessel.GetPart<WaterskinPart>().Charges = units;
            Assert.True(Player().GetPart<InventoryPart>().AddObject(vessel));
            return vessel;
        }

        static int Units(Entity vessel) => vessel.GetPart<WaterskinPart>()?.Charges ?? vessel.GetPart<LiquidVesselPart>().Volume;
        static string Command(Entity vessel) => Prefix + Uri.EscapeDataString(vessel.ID);
        bool Act(Entity crop, Entity vessel) => InventorySystem.ExecuteCommand(
            new PerformInventoryActionCommand(crop, Command(vessel)), Player(), Zone).Success;
        InventoryAction[] Rows(Entity crop) => WorldInteractionSystem.GatherActions(crop, Player())
            .Where(a => a.Command.StartsWith(Prefix, StringComparison.Ordinal)).ToArray();

        [TestCase("skin")] [TestCase("gourd")] [TestCase("flask")]
        public void ExactCarriedWaterUnitTopsOnePreparedCropWithoutMakingAPool(string kind)
        {
            var crop = Planted(); var vessel = Vessel(kind);
            var row = Rows(crop).SingleOrDefault(a => a.Command == Command(vessel));
            Assert.NotNull(row, "A real planted crop must offer the exact carried vessel in the world menu.");
            StringAssert.Contains("water", row.Display.ToLowerInvariant());
            Assert.True(Act(crop, vessel));
            Assert.AreEqual(2, Units(vessel));
            AssertState(crop.GetPart<CropPart>(), 0, 0, 40);
            Assert.AreEqual(CropPart.WET_SOIL_BG, crop.GetPart<RenderPart>().BackgroundColor);
            Assert.False(Zone.GetCell(5, 5).Objects.Any(e => e.HasPart<LiquidPoolPart>()));
            Assert.False(Zone.TileState.HasCoating(5, 5, "water"));
            Assert.True(CultivatedSoilPart.IsCultivated(Zone, Zone.GetCell(5, 5)));
            Assert.AreSame(Player(), vessel.GetPart<PhysicsPart>().InInventory);
        }

        [TestCase("skin")] [TestCase("flask")]
        public void LastUnitLeavesTheSameReusableEmptyVessel(string kind)
        {
            var crop = Planted(); var vessel = Vessel(kind, 1);
            Assert.True(Act(crop, vessel));
            Assert.AreEqual(0, Units(vessel));
            Assert.Contains(vessel, Player().GetPart<InventoryPart>().Objects);
            if (kind == "flask") Assert.AreEqual("", vessel.GetPart<LiquidVesselPart>().LiquidId);
            Assert.IsEmpty(Rows(crop));
            Assert.False(Act(crop, vessel));
            Assert.AreEqual(0, Units(vessel));
        }

        [TestCase("ripe")] [TestCase("full")]
        public void AlreadyRipeOrFullyWateredCropsDoNotSpendAnotherUnit(string condition)
        {
            var owner = Planted(water: condition == "full" ? 40 : 0, stage: condition == "ripe" ? 2 : 0);
            var vessel = Vessel();
            Assert.IsEmpty(Rows(owner)); Assert.False(Act(owner, vessel));
            Assert.AreEqual(3, Units(vessel));
            Assert.AreEqual(condition == "full" ? 40 : 0, owner.GetPart<CropPart>().MoistureTicks);
        }

        [Test] public void WetTopUpPreservesFractionAndUsesMaximumRatherThanAddition()
        {
            var owner = Planted(water: 2); var crop = owner.GetPart<CropPart>(); var vessel = Vessel();
            Clock.AdvanceClock(9);
            Assert.True(Act(owner, vessel));
            AssertState(crop, 0, 0, 40); Assert.AreEqual(9, crop.GrowthWetTickRemainder);
            Assert.False(Act(owner, vessel)); Assert.AreEqual(2, Units(vessel));
            Clock.AdvanceClock(1); Assert.True(Reconcile(crop, Zone, Clock.TickCount));
            AssertState(crop, 0, 1, 39); Assert.AreEqual(0, crop.GrowthWetTickRemainder);
        }

        [Test] public void OldDryIntervalCannotBorrowTheNewWater()
        {
            var owner = Planted(water: 1); var crop = owner.GetPart<CropPart>(); var vessel = Vessel();
            Clock.AdvanceClock(109);
            Assert.True(Act(owner, vessel));
            AssertState(crop, 0, 1, 40); Assert.AreEqual(0, crop.GrowthWetTickRemainder);
            Assert.AreEqual(109, crop.LastGrowthWorldTick);
            Clock.AdvanceClock(1); Assert.True(Reconcile(crop, Zone, Clock.TickCount));
            AssertState(crop, 0, 1, 40);
        }

        [Test] public void MenuReadsDoNotReconcileElapsedGrowthOrSpendResources()
        {
            var owner = Planted(water: 2); var crop = owner.GetPart<CropPart>(); var vessel = Vessel();
            Clock.AdvanceClock(109);
            for (int i = 0; i < 3; i++) Assert.AreEqual(1, Rows(owner).Length);
            AssertState(crop, 0, 0, 2); Assert.AreEqual(0, crop.LastGrowthWorldTick);
            Assert.AreEqual(0, crop.GrowthWetTickRemainder); Assert.AreEqual(3, Units(vessel));
            Assert.AreEqual(109, Clock.TickCount);
        }

        [TestCase(false)] [TestCase(true)]
        public void CompatibilityBoundaryRequiresPreparedSoilEvenForLegacyCrops(bool prepared)
        {
            var owner = Planted(prepared, standing: false); var vessel = Vessel();
            Assert.AreEqual(prepared ? 1 : 0, Rows(owner).Length);
            Assert.AreEqual(prepared, Act(owner, vessel));
            Assert.AreEqual(prepared ? 2 : 3, Units(vessel));
            Assert.AreEqual(prepared ? 40 : 0, owner.GetPart<CropPart>().MoistureTicks);
        }

        [TestCase(false)] [TestCase(true)]
        public void DueMaturityIsSettledBeforeRefusingWaterAndNeverDebits(bool standing)
        {
            var owner = Planted(standing: standing, water: 1, stage: 1, length: 2);
            var crop = owner.GetPart<CropPart>(); crop.TicksInStage = 1; var vessel = Vessel();
            Clock.AdvanceClock(10);
            Assert.False(Act(owner, vessel)); Assert.AreEqual(3, Units(vessel));
            Assert.AreEqual(standing, Zone.GetEntityCell(owner) != null);
            Assert.AreEqual(standing ? 0 : 2, Count("CandyCarrot"));
            if (standing) AssertState(crop, 2, 0, 0);
        }

        [TestCase("skin")] [TestCase("flask")]
        public void OuterFailureRestoresOnlyNewSupplyAndRetainsEarnedElapsedGrowth(string kind)
        {
            var owner = Planted(water: 2); var crop = owner.GetPart<CropPart>(); var vessel = Vessel(kind);
            Clock.AdvanceClock(19); Player().AddPart(new FailAfterWater());
            Assert.False(Act(owner, vessel));
            Assert.AreEqual(3, Units(vessel));
            AssertState(crop, 0, 1, 1); Assert.AreEqual(19, crop.LastGrowthWorldTick);
            Assert.AreEqual(9, crop.GrowthWetTickRemainder);
            Assert.AreEqual(CropPart.WET_SOIL_BG, owner.GetPart<RenderPart>().BackgroundColor);
        }

        [TestCase("skin")] [TestCase("flask")]
        public void SelectedEmptyOrRemovedVesselNeverFallsBackToOtherWater(string kind)
        {
            var owner = Planted(); var chosen = Vessel(kind, 1); var spare = Vessel("gourd");
            Assert.AreEqual(2, Rows(owner).Length);
            Assert.True(Player().GetPart<InventoryPart>().RemoveObject(chosen));
            Assert.False(Act(owner, chosen)); Assert.AreEqual(3, Units(spare));
            Assert.AreEqual(0, owner.GetPart<CropPart>().MoistureTicks);
            Assert.True(Act(owner, spare)); Assert.AreEqual(2, Units(spare));
        }

        [TestCase("oil")] [TestCase("acid")] [TestCase("")]
        public void NonWaterOrEmptyLiquidVesselCannotIrrigate(string liquid)
        {
            var owner = Planted(); var vessel = Vessel("flask", liquid == "" ? 0 : 3);
            vessel.GetPart<LiquidVesselPart>().LiquidId = liquid;
            Assert.IsEmpty(Rows(owner)); Assert.False(Act(owner, vessel));
            Assert.AreEqual(liquid == "" ? 0 : 3, Units(vessel));
            Assert.AreEqual(liquid, vessel.GetPart<LiquidVesselPart>().LiquidId);
            Assert.AreEqual(0, owner.GetPart<CropPart>().MoistureTicks);
        }

        [TestCase("far")] [TestCase("removed")] [TestCase("flooded")] [TestCase("dead")]
        public void ChangedPhysicalContextRefusesThePreviouslyOfferedWater(string change)
        {
            var owner = Planted(); var vessel = Vessel(); Assert.AreEqual(1, Rows(owner).Length);
            if (change == "far") Assert.True(Zone.MoveEntity(Player(), 20, 20));
            if (change == "removed") Assert.True(Zone.RemoveEntity(owner));
            if (change == "flooded")
            {
                var pool = new Entity { ID = Guid.NewGuid().ToString("N") };
                pool.AddPart(new PhysicsPart()); pool.AddPart(new LiquidPoolPart { LiquidId = "water", Volume = 3 });
                Assert.True(Zone.AddEntity(pool, 5, 5));
            }
            if (change == "dead") Player().GetStat("Hitpoints").BaseValue = 0;
            Assert.False(Act(owner, vessel)); Assert.AreEqual(3, Units(vessel));
            Assert.AreEqual(0, owner.GetPart<CropPart>().MoistureTicks);
        }

        [Test] public void EmptyPreparedSoilHasNoWaterStorageOrPaidWateringAction()
        {
            var crop = Planted(); var vessel = Vessel(); Assert.True(Zone.RemoveEntity(crop));
            var soil = Zone.GetCell(5, 5).Objects.Single(e => e.HasPart<CultivatedSoilPart>());
            Assert.IsEmpty(Rows(soil)); Assert.False(Act(soil, vessel)); Assert.AreEqual(3, Units(vessel));
            Assert.False(Zone.GetCell(5, 5).Objects.Any(e => e.HasPart<LiquidPoolPart>()));
        }

        sealed class FailAfterWater : Part
        {
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "AfterInventoryAction" && e.GetStringParameter("Command").StartsWith(Prefix, StringComparison.Ordinal))
                    throw new InvalidOperationException("Intentional watering rollback boundary.");
                return true;
            }
        }
    }
}
