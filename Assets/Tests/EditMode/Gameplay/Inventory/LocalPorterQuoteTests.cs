using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class LocalPorterQuoteTests : FiftyWorldFixture
    {
        [Test]
        public void QuoteNamesTheActualRouteWeightAndFareWithoutMovingOrPaying()
        {
            var (porter, parcel, shelf, clay) = ArrangeQuote(2);
            var worker = porter.ParentEntity;
            int turn = Clock.TickCount;

            Assert.That(porter.TryQuote(Actor, parcel, shelf, Zone, out var quote, out var refusal), Is.True, refusal);
            Assert.That(refusal, Is.Null.Or.Empty);
            Assert.That(quote.LoadedWeight, Is.EqualTo(5));
            Assert.That(quote.Steps.Count, Is.InRange(1, 32));
            Assert.That(quote.Fare, Is.EqualTo(2 + (quote.Steps.Count + 7) / 8 + (quote.LoadedWeight + 3) / 4));
            Assert.That(Clock.TickCount, Is.EqualTo(turn));
            Assert.That(Zone.GetEntityPosition(Actor), Is.EqualTo((10, 10)));
            Assert.That(Zone.GetEntityPosition(worker), Is.EqualTo((11, 10)));
            AssertUnchanged(porter, parcel, shelf, clay, 2, 50);
        }

        [TestCase("partial")]
        [TestCase("capacity")]
        [TestCase("purse")]
        [TestCase("wrong-zone")]
        [TestCase("blocked-route")]
        public void UnavailableDeliveryQuotesRefuseAndKeepMoneyAndCustody(string reason)
        {
            int units = reason == "partial" ? 1 : 2;
            var (porter, parcel, shelf, clay) = ArrangeQuote(units);
            Zone commandZone = Zone;
            switch (reason)
            {
                case "capacity":
                    porter.ParentEntity.GetPart<InventoryPart>().MaxWeight = 4;
                    break;
                case "purse":
                    TradeSystem.SetDrams(Actor, 0);
                    break;
                case "wrong-zone":
                    commandZone = new Zone(Zone.ZoneID);
                    break;
                case "blocked-route":
                    for (int y = 9; y <= 11; y++)
                    for (int x = 18; x <= 20; x++)
                        if (x != 19 || y != 10) Place("Wall", x, y);
                    break;
            }

            Assert.That(porter.TryQuote(Actor, parcel, shelf, commandZone, out _, out var refusal), Is.False);
            Assert.That(refusal, Is.Not.Null.And.Not.Empty);
            AssertUnchanged(porter, parcel, shelf, clay, units, reason == "purse" ? 0 : 50);
        }

        [Test]
        public void RootedPorterRefusesWithoutRunningAMovementContest()
        {
            var (porter, parcel, shelf, clay) = ArrangeQuote(2);
            var worker = porter.ParentEntity;
            var rooted = new RootedMovementProbe();
            int turn = Clock.TickCount;
            Assert.That(porter.TryQuote(Actor, parcel, shelf, Zone, out _, out _), Is.True);
            Assert.That(worker.ApplyEffect(rooted, Actor, Zone), Is.True);

            Assert.That(porter.TryQuote(Actor, parcel, shelf, Zone, out _, out var refusal), Is.False);
            Assert.That(refusal, Is.Not.Null.And.Not.Empty);
            Assert.That(rooted.MovementContests, Is.Zero);
            Assert.That(rooted.Duration, Is.EqualTo(4));
            Assert.That(Clock.TickCount, Is.EqualTo(turn));
            Assert.That(Zone.GetEntityPosition(worker), Is.EqualTo((11, 10)));
            AssertUnchanged(porter, parcel, shelf, clay, 2, 50);
        }

        [Test]
        public void ScaldingSteamAtTheOnlyStandingCellRefusesTheQuote()
        {
            var (porter, parcel, shelf, clay) = ArrangeQuote(2);
            var worker = porter.ParentEntity;
            var step = LeaveOneReceivingStep();
            int clearX = step.X, clearY = step.Y;
            var source = Place("OilSeep", clearX, clearY);
            var thermal = source.GetPart<ThermalPart>();
            var steam = new SteamEffect(.8f);
            thermal.Temperature = 25;
            Assert.That(source.ApplyEffect(steam), Is.True);
            Assert.That(TerrainNavigationWeight.ForStep(Zone, clearX, clearY, worker), Is.Zero);
            Assert.That(porter.TryQuote(Actor, parcel, shelf, Zone, out _, out _), Is.True);
            thermal.Temperature = 180;
            Assert.That(TerrainNavigationWeight.ForStep(Zone, clearX, clearY, worker), Is.GreaterThan(0));
            int turn = Clock.TickCount;
            int hitPoints = worker.GetStatValue("Hitpoints");

            Assert.That(porter.TryQuote(Actor, parcel, shelf, Zone, out _, out var refusal), Is.False);
            Assert.That(refusal, Is.Not.Null.And.Not.Empty);
            Assert.That(worker.GetStatValue("Hitpoints"), Is.EqualTo(hitPoints));
            Assert.That(steam.Density, Is.EqualTo(.8f));
            Assert.That(Clock.TickCount, Is.EqualTo(turn));
            AssertUnchanged(porter, parcel, shelf, clay, 2, 50);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void GritKeepsTheOnlyReceivingStepUsableUntilTheTractionExpires(bool oily)
        {
            var (porter, parcel, shelf, clay) = ArrangeQuote(2);
            var step = LeaveOneReceivingStep();
            if (oily) Zone.TileState.WriteCoating(step.X, step.Y, "oil", 30);
            int turn = Clock.TickCount;

            Assert.That(porter.TryQuote(Actor, parcel, shelf, Zone, out _, out _), Is.EqualTo(!oily));
            Zone.TileState.WriteResidue(step.X, step.Y, CombatUtilityActions.GritResidue, 1);
            Assert.That(porter.TryQuote(Actor, parcel, shelf, Zone, out var quote, out var refusal), Is.True, refusal);
            Assert.That(quote.Steps, Does.Contain(step));
            Assert.That(quote.Steps.Count, Is.EqualTo(7));
            Assert.That(quote.LoadedWeight, Is.EqualTo(5));
            Assert.That(quote.Fare, Is.EqualTo(5));

            Zone.TileState.Tick();
            Assert.That(Zone.TileState.HasResidue(step.X, step.Y, CombatUtilityActions.GritResidue), Is.False);
            Assert.That(Zone.TileState.HasCoating(step.X, step.Y, "oil"), Is.EqualTo(oily));
            Assert.That(porter.TryQuote(Actor, parcel, shelf, Zone, out _, out _), Is.EqualTo(!oily));
            Assert.That(Clock.TickCount, Is.EqualTo(turn));
            AssertUnchanged(porter, parcel, shelf, clay, 2, 50);
        }

        [TestCase("embers")]
        [TestCase("heat")]
        [TestCase("acid")]
        [TestCase("pool")]
        [TestCase("trigger")]
        public void GritDoesNotAdmitAnotherHazardAtTheReceivingStep(string hazard)
        {
            var (porter, parcel, shelf, clay) = ArrangeQuote(2);
            var step = LeaveOneReceivingStep();
            Zone.TileState.WriteCoating(step.X, step.Y, "oil", 30);
            Zone.TileState.WriteResidue(step.X, step.Y, CombatUtilityActions.GritResidue, 12);
            switch (hazard)
            {
                case "embers": Zone.TileState.WriteResidue(step.X, step.Y, "embers", 3); break;
                case "heat": Zone.TileState.AddHeat(step.X, step.Y, 2); break;
                case "acid": Zone.TileState.WriteCoating(step.X, step.Y, "acid", 3); break;
                case "pool": Place("WaterPuddle", step.X, step.Y); break;
                case "trigger": Place("RuneOfFrost", step.X, step.Y); break;
            }

            Assert.That(porter.TryQuote(Actor, parcel, shelf, Zone, out _, out var refusal), Is.False);
            Assert.That(refusal, Does.Contain("clear route"));
            AssertUnchanged(porter, parcel, shelf, clay, 2, 50);
        }

        private Cell LeaveOneReceivingStep()
        {
            const int clearX = 18, clearY = 10;
            for (int y = 9; y <= 11; y++)
            for (int x = 18; x <= 20; x++)
                if ((x, y) != (19, 10) && (x, y) != (clearX, clearY)) Place("Wall", x, y);
            return Zone.GetCell(clearX, clearY);
        }

        private (LocalPorterPart porter, Entity parcel, Entity shelf, Entity clay) ArrangeQuote(int units)
        {
            var worker = Place("Creature", 11, 10);
            var porter = new LocalPorterPart();
            worker.AddPart(porter);
            worker.SetTag("Faction", "Villagers");
            worker.GetPart<InventoryPart>().MaxWeight = 200;
            worker.GetStat("Hitpoints").BaseValue = 20;
            worker.GetStat("Strength").BaseValue = 16;
            var brain = worker.GetPart<BrainPart>();
            brain.Passive = true;
            brain.Staying = true;
            brain.Wanders = false;
            brain.WandersRandomly = false;
            var parcel = Place("LiningClayParcel", 10, 11);
            var shelf = Place("LiningClayReceivingShelf", 19, 10);
            Zone.GetEntityCell(worker).IsVisible = true;
            Zone.GetEntityCell(parcel).IsVisible = true;
            Zone.GetEntityCell(shelf).IsVisible = true;
            var clay = Carry("PreparedLiningClay");
            clay.GetPart<StackerPart>().StackCount = units;
            Assert.That(InventorySystem.ExecuteCommand(new PutInContainerCommand(parcel, clay), Actor, Zone).Success, Is.True);
            TradeSystem.SetDrams(Actor, 50);
            TradeSystem.SetDrams(worker, 7);
            return (porter, parcel, shelf, clay);
        }

        private void AssertUnchanged(LocalPorterPart porter, Entity parcel, Entity shelf, Entity clay, int units, int drams)
        {
            Assert.That(TradeSystem.GetDrams(Actor), Is.EqualTo(drams));
            Assert.That(TradeSystem.GetDrams(porter.ParentEntity), Is.EqualTo(7));
            Assert.That(Zone.GetEntityPosition(parcel), Is.EqualTo((10, 11)));
            Assert.That(parcel.GetPart<PhysicsPart>().InInventory, Is.Null);
            Assert.That(parcel.GetPart<ContainerPart>().Contents, Is.EqualTo(new[] { clay }));
            Assert.That(clay.GetPart<PhysicsPart>().InInventory, Is.SameAs(parcel));
            Assert.That(clay.GetPart<StackerPart>().StackCount, Is.EqualTo(units));
            Assert.That(shelf.GetPart<ContainerPart>().Contents, Is.Empty);
            Assert.That(Pack.Contains(parcel), Is.False);
            Assert.That(porter.ParentEntity.GetPart<InventoryPart>().Contains(parcel), Is.False);
        }

        private sealed class RootedMovementProbe : RootedEffect
        {
            public int MovementContests { get; private set; }

            public override bool AllowMovement(Entity target)
            {
                MovementContests++;
                return base.AllowMovement(target);
            }
        }
    }
}
