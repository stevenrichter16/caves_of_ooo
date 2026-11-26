using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class RainClaySeamTests : FiftyWorldFixture
    {
        const string SeamBlueprint = "RainClaySeam";
        const string PacketBlueprint = "RainClaySpoil";
        const string ScreeningPrefix = "MaterialProcess|screen_size_grades|";
        const int ShowerStart = 2400;
        const int ShowerEnd = 3000;
        const int GraceTicks = 150;
        EntityFactory previousForge;

        [SetUp]
        public void BindForge()
        {
            previousForge = ForgePart.Factory;
            ForgePart.Factory = Factory;
            MaterialProcessRegistry.ResetForTests();
        }

        [TearDown]
        public void RestoreForge()
        {
            ForgePart.Factory = previousForge;
            MaterialProcessRegistry.ResetForTests();
        }

        OverworldZoneManager EnterFreshAllotment()
        {
            var manager = OverworldZoneManager.CreateDetached(Factory, 2, true);
            Assert.True(Zone.RemoveEntity(Actor));
            Zone = manager.GetZone(RepairCultivationSite.ZoneID);
            manager.SetActiveZone(Zone);
            SettlementRuntime.ActiveZone = Zone;
            Assert.True(Zone.AddEntity(Actor, 40, 12));
            Zone.ForEachCell((cell, _, _) => { cell.Explored = true; cell.IsVisible = true; });
            return manager;
        }

        Entity Seam()
        {
            var owners = Zone.GetReadOnlyEntities().Where(entity => entity.BlueprintName == SeamBlueprint).ToArray();
            Assert.That(owners, Has.Length.EqualTo(1), "The fresh allotment has one separate finite rain seam.");
            return owners[0];
        }

        void StandBeside(Entity owner)
        {
            var cell = Zone.GetEntityCell(owner);
            var next = new[] { (1, 0), (0, 1), (-1, 0), (0, -1) }
                .Select(step => Zone.GetCell(cell.X + step.Item1, cell.Y + step.Item2))
                .First(candidate => candidate != null && !candidate.BlocksMovement(Actor));
            Assert.True(Zone.MoveEntity(Actor, next.X, next.Y));
        }

        void AdvanceTo(int tick)
        {
            Assert.That(tick, Is.GreaterThanOrEqualTo(Clock.TickCount));
            Clock.AdvanceClock(tick - Clock.TickCount);
        }

        bool HasHarvest(Entity source) => Actions(source).Any(row => row.Command == "Harvest" && !row.IsDisabled);

        [TestCase(true)]
        [TestCase(false)]
        public void TheOptionalSeamLeavesBothCatchFramesAndTheSeedStandInPlace(bool includeSeam)
        {
            var source = Factory.Blueprints[SeamBlueprint];
            if (!includeSeam) Factory.Blueprints.Remove(SeamBlueprint);
            try
            {
                EnterFreshAllotment();
                var sites = Zone.GetReadOnlyEntities()
                    .Where(owner => owner.HasPart<RainCollectorPart>() || owner.BlueprintName == "LateKnotflaxStand")
                    .Select(owner => (owner.BlueprintName, Zone.GetEntityPosition(owner), owner.HasPart<SeedFramePart>()))
                    .ToArray();
                Assert.That(sites, Is.EquivalentTo(new[]
                {
                    ("CampAwning", (60, 8), true), ("CampAwning", (64, 8), true), ("LateKnotflaxStand", (60, 7), false)
                }));
                Assert.That(Zone.GetReadOnlyEntities().Count(owner => owner.BlueprintName == SeamBlueprint),
                    Is.EqualTo(includeSeam ? 1 : 0));
            }
            finally { if (!includeSeam) Factory.Blueprints.Add(SeamBlueprint, source); }
        }

        Entity CollectPacket(Entity seam)
        {
            StandBeside(seam);
            Assert.True(HasHarvest(seam));
            Assert.True(Act(seam, "Harvest"));
            return Pack.Objects.Single(item => item.BlueprintName == PacketBlueprint);
        }

        [Test]
        public void FreshSeamKeepsTheOrdinaryClayBankAndDrySpoil()
        {
            EnterFreshAllotment();
            var bank = Zone.GetReadOnlyEntities().Single(entity => entity.BlueprintName == "RepairClayBank");
            var dryHeap = Zone.GetReadOnlyEntities().Single(entity => entity.BlueprintName == "DryClaySpoilHeap");
            StandBeside(bank);
            Assert.True(HasHarvest(bank));
            Assert.True(Act(bank, "Harvest"));
            Assert.That(Count("FireClay"), Is.EqualTo(4));
            Assert.That(dryHeap.GetPart<HarvestablePart>().YieldBlueprint, Is.EqualTo("DryClaySpoil"));
            var seam = Seam();
            Assert.That(Zone.GetEntityCell(seam).IsInterior, Is.False);
            Assert.That(seam.GetPart<PhysicsPart>().Solid, Is.False);
            Assert.That(seam.GetPart<HarvestablePart>()?.GetType().Name, Is.EqualTo("RainSeamHarvestPart"));
        }

        [Test]
        public void CollectionOpensWithRainAndClosesAtTheEndOfItsGrace()
        {
            EnterFreshAllotment();
            var seam = Seam();
            StandBeside(seam);
            AdvanceTo(ShowerStart - 1);
            Assert.False(HasHarvest(seam));
            Assert.False(Act(seam, "Harvest"));
            AdvanceTo(ShowerStart);
            Assert.True(HasHarvest(seam));
            AdvanceTo(ShowerEnd);
            Assert.True(HasHarvest(seam));
            AdvanceTo(ShowerEnd + GraceTicks - 1);
            var quoted = Actions(seam).Single(row => row.Command == "Harvest" && !row.IsDisabled);
            AdvanceTo(ShowerEnd + GraceTicks);
            Assert.False(HasHarvest(seam));
            Assert.False(Act(seam, quoted.Command));
            Assert.That(Count(PacketBlueprint), Is.Zero);
            Assert.That(seam.GetPart<HarvestablePart>().Harvested, Is.False);
        }

        [Test]
        public void CollectionDuringGraceKeepsAnExactSpentSeamAndWetPacketThroughSave()
        {
            var manager = EnterFreshAllotment();
            var seam = Seam();
            var cell = Zone.GetEntityCell(seam);
            AdvanceTo(ShowerEnd + 10);
            var packet = CollectPacket(seam);
            Assert.That(packet.GetEffect<WetEffect>()?.Moisture, Is.EqualTo(.25f).Within(.0001f));
            Assert.That(Count(PacketBlueprint), Is.EqualTo(1));
            Assert.That(Zone.GetEntityCell(seam), Is.SameAs(cell));
            Assert.That(seam.GetPart<HarvestablePart>().Harvested, Is.True);

            AdvanceTo(4200);
            Assert.That(RegionalWeather.Current(Zone, Clock.TickCount, RegionalWeather.PolicyVersion).Raining, Is.True);
            Assert.False(HasHarvest(seam));
            Assert.False(Act(seam, "Harvest"));
            var saved = HotbarSaveFixture.RoundTrip(GameSessionState.Capture("rain-seam", "test", manager, Clock, Actor));
            var savedZone = saved.ZoneManager.GetZone(RepairCultivationSite.ZoneID);
            var savedSeam = savedZone.GetReadOnlyEntities().Single(entity => entity.BlueprintName == SeamBlueprint);
            Assert.That(savedSeam.ID, Is.EqualTo(seam.ID));
            Assert.That(savedSeam.GetPart<HarvestablePart>().Harvested, Is.True);
            var savedPacket = saved.Player.GetPart<InventoryPart>().Objects.Single(item => item.BlueprintName == PacketBlueprint);
            Assert.That(savedPacket.ID, Is.EqualTo(packet.ID));
            Assert.That(savedPacket.GetEffect<WetEffect>()?.Moisture, Is.EqualTo(.25f).Within(.0001f));
        }

        [Test]
        public void TheWetPacketBecomesScreenableAfterTheExistingGroundDrying()
        {
            EnterFreshAllotment();
            var sieve = Carry("HandSieve");
            AdvanceTo(ShowerEnd + 10);
            var packet = CollectPacket(Seam());
            bool CanScreen(InventoryAction row) => !row.IsDisabled && row.Command.StartsWith(ScreeningPrefix, StringComparison.Ordinal);
            Assert.False(Actions(packet).Any(CanScreen));
            Assert.That(Count("FireClay") + Count("CoarseGrit"), Is.Zero);

            Assert.True(Pack.RemoveObject(packet));
            var ground = Zone.GetEntityCell(Actor);
            Assert.True(Zone.AddEntity(packet, ground.X, ground.Y));
            for (int turn = 0; turn < 30; turn++) MaterialSimSystem.TickMaterialEntities(Zone);
            Assert.That(packet.GetEffect<WetEffect>(), Is.Null);
            Assert.True(Zone.RemoveEntity(packet));
            Assert.True(Pack.AddObject(packet));
            Assert.True(Act(packet, Actions(packet).Single(CanScreen).Command));
            Assert.That(Count(PacketBlueprint), Is.Zero);
            Assert.That(Count("FireClay"), Is.EqualTo(2));
            Assert.That(Count("CoarseGrit"), Is.EqualTo(1));
            Assert.That(Pack.Objects, Does.Contain(sieve));
        }

        [Test]
        public void CreationCallbackCannotAliasNewPacketWithAnExistingItem()
        {
            EnterFreshAllotment();
            var incumbent = Carry("HandSieve");
            var seam = Seam();
            StandBeside(seam);
            AdvanceTo(ShowerStart);
            const string probeName = nameof(RainClayCreationCallback);
            var blueprintParts = Factory.Blueprints[PacketBlueprint].Parts;
            Factory.RegisterPartType<RainClayCreationCallback>(probeName);
            blueprintParts.Add(probeName, new Dictionary<string, string>());
            bool reached = false;
            RainClayCreationCallback.Created = probe =>
            {
                var packet = probe.ParentEntity;
                packet.ID = incumbent.ID;
                reached = true;
                // Restore the canonical template and leave only the identity collision.
                blueprintParts.Remove(probeName);
                packet.RemovePart(probe);
            };
            try
            {
                Assert.False(Act(seam, "Harvest"));
                Assert.That(reached, Is.True);
                Assert.That(seam.GetPart<HarvestablePart>().Harvested, Is.False);
                Assert.That(Pack.Objects.Single(item => item.ID == incumbent.ID), Is.SameAs(incumbent));
                Assert.That(Count(PacketBlueprint), Is.Zero);
            }
            finally
            {
                blueprintParts.Remove(probeName);
                RainClayCreationCallback.Created = null;
            }

            var valid = CollectPacket(seam);
            Assert.That(valid.ID, Is.Not.EqualTo(incumbent.ID));
            Assert.That(Count(PacketBlueprint), Is.EqualTo(1));
        }
    }

    public sealed class RainClayCreationCallback : Part
    {
        public static Action<RainClayCreationCallback> Created;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "ObjectCreated") Created?.Invoke(this);
            return true;
        }
    }
}
