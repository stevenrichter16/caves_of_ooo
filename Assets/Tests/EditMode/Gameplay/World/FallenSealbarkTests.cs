using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class FallenSealbarkTests : FiftyWorldFixture
    {
        const string FallenBark = "RainFallenSealbark";
        const string Slat = "SealbarkSlat";
        const string ClaySeam = "RainClaySeam";
        const string ClayPacket = "RainClaySpoil";
        const int RainStarts = 2400, RainEnds = 3000, Grace = 150;

        OverworldZoneManager EnterAllotment(int version = SpreadExplorationPlan.CurrentVersion)
        {
            var manager = OverworldZoneManager.CreateDetached(Factory, 2, true);
            if (version != SpreadExplorationPlan.CurrentVersion)
            {
                var world = new Entity();
                world.Properties[SpreadExplorationPlan.PropertyKey] = $"{version}|2|0";
                world.Properties[SpreadExplorationPlan.WorldKeyProperty] = "0123456789abcdef0123456789abcdef";
                var restore = typeof(SpreadExplorationPlan).GetMethod("Restore", BindingFlags.Static | BindingFlags.NonPublic);
                typeof(OverworldZoneManager).GetProperty(nameof(OverworldZoneManager.Exploration))
                    .SetValue(manager, restore.Invoke(null, new object[] { manager, world }));
            }
            Assert.True(Zone.RemoveEntity(Actor));
            Zone = manager.GetZone(RepairCultivationSite.ZoneID);
            manager.SetActiveZone(Zone);
            SettlementRuntime.ActiveZone = Zone;
            Assert.True(Zone.AddEntity(Actor, 40, 12));
            Zone.ForEachCell((cell, _, _) => { cell.IsVisible = true; cell.Explored = true; });
            return manager;
        }

        Entity Owner(string blueprint)
        {
            var entries = Zone.GetReadOnlyEntities().Where(owner => owner.BlueprintName == blueprint).ToArray();
            Assert.That(entries, Has.Length.EqualTo(1), "The generated allotment should contain one " + blueprint);
            return entries[0];
        }

        void StandBeside(Entity owner)
        {
            var at = Zone.GetEntityCell(owner);
            var next = new[] { (at.X + 1, at.Y), (at.X, at.Y + 1), (at.X - 1, at.Y), (at.X, at.Y - 1) }
                .Select(step => Zone.GetCell(step.Item1, step.Item2)).First(cell => cell != null && !cell.BlocksMovement(Actor));
            Assert.True(Zone.MoveEntity(Actor, next.X, next.Y));
        }

        void AtTick(int tick)
        {
            Assert.GreaterOrEqual(tick, Clock.TickCount);
            Clock.AdvanceClock(tick - Clock.TickCount);
        }

        InventoryAction[] HarvestRows(Entity owner)
        {
            var cell = Zone.GetEntityCell(owner);
            return CellMenu(cell.X, cell.Y).Where(row => !row.IsDisabled && row.Command == "Harvest").ToArray();
        }

        void ChooseHarvest(Entity owner)
        {
            var cell = Zone.GetEntityCell(owner);
            Assert.True(ChooseRow(HarvestRows(owner).Single(), cell.X, cell.Y));
        }

        (char glyph, string color) Appearance(Entity owner)
        {
            var appearance = typeof(FlatPixelFallbackCatalog).Assembly.GetType("CavesOfOoo.Rendering.FieldOpportunityAppearance")
                ?.GetMethod("TryGet", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(appearance);
            object[] args = { Zone, owner, '\0', null };
            Assert.True((bool)appearance.Invoke(null, args));
            return ((char)args[2], (string)args[3]);
        }

        [Test]
        public void TheFallenBarkChangesVisiblyUntilTheLastTickOfItsGrace()
        {
            EnterAllotment();
            var bark = Owner(FallenBark);
            var harvest = bark.GetPart<RainSeamHarvestPart>();
            Assert.NotNull(harvest);
            Assert.That(Zone.GetEntityCell(bark), Is.SameAs(Zone.GetEntityCell(harvest.Ground)));
            Assert.False(Zone.GetEntityCell(bark).IsInterior);
            Assert.False(bark.GetPart<PhysicsPart>().Solid);
            StandBeside(bark);
            AtTick(RainStarts - 1);
            var shut = Appearance(bark);
            Assert.That(HarvestRows(bark), Is.Empty);
            Assert.False(Act(bark, "Harvest"));

            AtTick(RainStarts);
            Assert.True(RegionalWeather.Current(Zone, Clock.TickCount, RegionalWeather.PolicyVersion).Raining);
            var open = Appearance(bark);
            Assert.That(open, Is.Not.EqualTo(shut));
            Assert.That(HarvestRows(bark), Has.Length.EqualTo(1));
            AtTick(RainEnds + Grace - 1);
            var stale = HarvestRows(bark).Single();
            Assert.That(Appearance(bark), Is.Not.EqualTo(shut));

            AtTick(RainEnds + Grace);
            Assert.That(HarvestRows(bark), Is.Empty);
            Assert.That(Appearance(bark), Is.EqualTo(shut));
            Assert.False(Act(bark, stale.Command));
            Assert.False(harvest.Harvested);
            Assert.AreEqual(0, Count(Slat));
        }

        [TestCase(RainStarts)]
        [TestCase(RainEnds + 15)]
        public void WetWeatherYieldsOneOrdinaryDrySlatAndItsExistingPrepareAction(int tick)
        {
            EnterAllotment();
            var bark = Owner(FallenBark);
            var cell = Zone.GetEntityCell(bark);
            AtTick(tick);
            StandBeside(bark);
            ChooseHarvest(bark);

            Assert.That(Zone.GetEntityCell(bark), Is.SameAs(cell));
            Assert.True(bark.GetPart<HarvestablePart>().Harvested);
            Assert.False(Act(bark, "Harvest"));
            Assert.AreEqual(1, Count(Slat));
            var slat = Pack.Objects.Single(owner => owner.BlueprintName == Slat);
            Assert.False(slat.HasEffect<WetEffect>());
            Assert.That(slat.GetPart<PhysicsPart>().InInventory, Is.SameAs(Actor));
            Assert.AreEqual(1, HandlingService.GetWeight(slat));
            Assert.True(Act(slat, Choice(slat, BotanicalProcessingPart.Command)));
            Assert.AreEqual(0, Count(Slat));
            Assert.AreEqual(1, Count("SalvagedTimber"));
        }

        [Test]
        public void TheClayAndBarkAreIndependentlyWorkableAtTheSameAllotment()
        {
            EnterAllotment();
            var bark = Owner(FallenBark);
            var clay = Owner(ClaySeam);
            Assert.That(Zone.GetEntityCell(bark), Is.Not.SameAs(Zone.GetEntityCell(clay)));
            AtTick(RainStarts);
            StandBeside(bark);
            ChooseHarvest(bark);
            StandBeside(clay);
            Assert.False(clay.GetPart<HarvestablePart>().Harvested);
            ChooseHarvest(clay);

            Assert.True(bark.GetPart<HarvestablePart>().Harvested);
            Assert.True(clay.GetPart<HarvestablePart>().Harvested);
            Assert.AreEqual(1, Count(Slat));
            Assert.AreEqual(1, Count(ClayPacket));
            Assert.True(Pack.Objects.Single(item => item.BlueprintName == ClayPacket).HasEffect<WetEffect>());
        }

        [Test]
        public void MissingOptionalClayContentDoesNotRemoveTheFallenBark()
        {
            Assert.True(Factory.Blueprints.Remove(ClaySeam));
            EnterAllotment();
            Assert.That(Zone.GetReadOnlyEntities().Any(owner => owner.BlueprintName == ClaySeam), Is.False);
            var bark = Owner(FallenBark);
            AtTick(RainStarts);
            StandBeside(bark);
            ChooseHarvest(bark);
            Assert.AreEqual(1, Count(Slat));
        }

        [Test]
        public void AnOlderWorldCanFindBarkOnItsFirstAllotmentVisit()
        {
            var manager = EnterAllotment(17);
            Assert.AreEqual(17, manager.Exploration.Version);
            Assert.False(Zone.GetReadOnlyEntities().Any(owner => owner.BlueprintName == ClaySeam));
            var bark = Owner(FallenBark);
            StandBeside(bark);
            AtTick(RainStarts);
            ChooseHarvest(bark);
            Assert.AreEqual(1, Count(Slat));
        }

        [Test]
        public void TheSameSpentBarkAndSlatSurviveTravelAndBinaryReload()
        {
            var manager = EnterAllotment();
            var home = Zone;
            var bark = Owner(FallenBark);
            AtTick(RainEnds + 15);
            StandBeside(bark);
            var standing = Zone.GetEntityPosition(Actor);
            ChooseHarvest(bark);
            var slat = Pack.Objects.Single(owner => owner.BlueprintName == Slat);

            var away = new Zone("Overworld.2.7.0");
            Assert.True(home.TryTransferEntityTo(Actor, away, 10, 10));
            manager.SetActiveZone(away);
            manager.UnloadZone(home.ZoneID);
            Assert.That(manager.GetZone(home.ZoneID), Is.SameAs(home));
            Assert.True(away.TryTransferEntityTo(Actor, home, standing.x, standing.y));
            manager.SetActiveZone(home);
            SettlementRuntime.ActiveZone = home;
            AtTick(4200);
            Assert.True(RegionalWeather.Current(home, Clock.TickCount, RegionalWeather.PolicyVersion).Raining);
            Assert.That(Owner(FallenBark), Is.SameAs(bark));
            Assert.That(HarvestRows(bark), Is.Empty);

            using var stream = new MemoryStream();
            GameSessionState.Capture("fallen-sealbark", "spent", manager, Clock, Actor).Save(new SaveWriter(stream));
            stream.Position = 0;
            var saved = GameSessionState.Load(new SaveReader(stream, Factory));
            Zone = saved.ZoneManager.ActiveZone;
            Actor = saved.Player;
            Clock = saved.TurnManager;
            SettlementRuntime.ActiveZone = Zone;
            var restoredBark = Owner(FallenBark);
            var restoredSlat = Pack.Objects.Single(owner => owner.BlueprintName == Slat);
            Assert.AreEqual(bark.ID, restoredBark.ID);
            Assert.AreEqual(slat.ID, restoredSlat.ID);
            Assert.True(restoredBark.GetPart<HarvestablePart>().Harvested);
            Assert.False(restoredSlat.HasEffect<WetEffect>());
            Assert.That(HarvestRows(restoredBark), Is.Empty);
            Assert.False(Act(restoredBark, "Harvest"));
            Assert.AreEqual(1, Count(Slat));
        }
    }
}
