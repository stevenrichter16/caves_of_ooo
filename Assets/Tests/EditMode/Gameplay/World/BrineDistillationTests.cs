using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class BrineDistillationTests : FiftyWorldFixture
    {
        Entity still, fire, source, receiver;
        EntityFactory previousStillFactory;
        [SetUp] public void SetUpDistillation()
        {
            previousStillFactory = AlchemyStillPart.Factory;
            AlchemyStillPart.Factory = Factory;
            Pack.MaxWeight = 400; still = Place("AlchemyStill");
            fire = Place("SpreadCookingCoals", 9, 10);
            fire.GetPart<FuelPart>().MaxFuel = 50; fire.GetPart<FuelPart>().FuelMass = 25; fire.GetPart<ThermalPart>().Temperature = 500;
            source = Flask(8, "brine"); receiver = Flask(0, "");
        }
        [TearDown] public void RestoreStillFactory() => AlchemyStillPart.Factory = previousStillFactory;
        string Row() => Actions(still).Single(a => a.Name == "DistillBrine" && !a.IsDisabled).Command;
        LiquidVesselPart V(Entity e) => e.GetPart<LiquidVesselPart>();

        const string KeepCrust = "DistillBrineKeepCrust";

        void ShowDryWorkCell()
        {
            Place("Floor", 10, 10);
            var cell = Zone.GetEntityCell(Actor);
            cell.IsVisible = cell.Explored = true;
        }

        InventoryAction KeepRow(Entity output) => Actions(still).Single(action => action.Name == KeepCrust
            && !action.IsDisabled && action.Command.Split('|').Contains(Uri.EscapeDataString(output.ID)));

        Entity[] Crust(Zone zone) => zone.GetReadOnlyEntities().Where(e => e.BlueprintName == "BrineCrust").ToArray();

        string LiveUseGuide() => UseGuides.SectionText(still, Zone, Zone.GetEntityCell(still), Actor)
            .Split('\n').Single(line => line.StartsWith("Right now", StringComparison.Ordinal));

        [TestCase("brine", "4 brine in one flask", "hot fire within 2 cells")]
        [TestCase("receiver", "separate empty single flask", "hot fire within 2 cells")]
        [TestCase("fire", "hot fire within 2 cells", "separate empty single flask")]
        public void TheMenuAndLiveReaderNameTheActualBatchBlocker(string blocker, string required, string unrelated)
        {
            ShowDryWorkCell();
            if (blocker == "brine")
            {
                V(source).Volume = 3;
                var second = Flask(3, "brine");
                Assert.That(V(second).Volume, Is.EqualTo(3), "Six carried measures spread across two flasks are not one batch.");
            }
            if (blocker == "receiver") Assert.True(Pack.RemoveObject(receiver));
            if (blocker == "fire") fire.GetPart<ThermalPart>().Temperature = 25;

            var rows = Actions(still).Where(row => row.Name == "DistillBrine" || row.Name == KeepCrust).ToArray();
            Assert.That(rows.All(row => row.IsDisabled), Is.True);
            var blocked = rows.Single(row => row.Name == "DistillBrine");
            Assert.That(blocked.DisabledReason, Does.Contain(required).IgnoreCase);
            Assert.That(blocked.Hint, Is.EqualTo(blocked.DisabledReason), "The menu must show the reason before selecting the row.");
            Assert.That(blocked.DisabledReason, Does.Not.Contain(unrelated).IgnoreCase);
            if (blocker == "fire") Assert.That(blocked.DisabledReason, Does.Contain("5 fuel").IgnoreCase);
            Assert.That(LiveUseGuide(), Does.Contain(blocked.DisabledReason.TrimEnd('.')));
            Assert.That(V(source).Volume, Is.EqualTo(blocker == "brine" ? 3 : 8));
            Assert.That(V(receiver).Volume, Is.Zero);
            Assert.That(fire.GetPart<FuelPart>().FuelMass, Is.EqualTo(25));
        }

        [TestCase("wet")]
        [TestCase("occupied")]
        public void AOneUnitReceiverStillReportsTheCrustGroundBlocker(string obstruction)
        {
            ShowDryWorkCell();
            V(receiver).Capacity = 1;
            Entity item = null;
            if (obstruction == "wet") Zone.TileState.WriteCoating(10, 10, "water", 5);
            else item = Place("FireClay", 10, 10);

            var rows = Actions(still);
            Assert.False(rows.Any(row => row.Name == "DistillBrine" && !row.IsDisabled));
            var blocked = rows.Single(row => row.Name == KeepCrust);
            Assert.True(blocked.IsDisabled);
            Assert.That(blocked.DisabledReason, Does.Contain("clear, dry ground").IgnoreCase);
            Assert.That(blocked.Hint, Is.EqualTo(blocked.DisabledReason));
            Assert.That(LiveUseGuide(), Does.Contain(blocked.DisabledReason.TrimEnd('.')));

            V(receiver).Capacity = 2;
            Assert.That(Row(), Does.StartWith("DistillBrine|"), "Ordinary water needs no loose solid output cell.");
            V(receiver).Capacity = 1;
            if (item != null) Assert.True(Zone.RemoveEntity(item));
            else Assert.True(Zone.TileState.RemoveCoating(10, 10, "water"));
            Assert.True(Act(still, KeepRow(receiver).Command));
            Assert.That(V(receiver).Volume, Is.EqualTo(1));
            Assert.That(Crust(Zone).Length, Is.EqualTo(1));
        }

        [Test]
        public void KeepingCrustUsesOneWaterInsteadOfTwoAndKeepsTheExactGroundOutputOnLoad()
        {
            ShowDryWorkCell();
            var otherReceiver = Flask(0, "", 8);
            Assert.That(Actions(still).Count(a => a.Name == "DistillBrine" && !a.IsDisabled), Is.EqualTo(2));
            Assert.That(Actions(still).Count(a => a.Name == KeepCrust && !a.IsDisabled), Is.EqualTo(2));
            var retained = KeepRow(receiver);
            Assert.That(retained.Command, Does.StartWith(KeepCrust + "|"));
            Assert.That(retained.Display + " " + retained.Hint, Does.Contain("crust").IgnoreCase);

            Assert.True(Act(still, retained.Command));
            Assert.That(V(source).Volume, Is.EqualTo(4));
            Assert.That(V(source).LiquidId, Is.EqualTo("brine"));
            Assert.That(V(receiver).Volume, Is.EqualTo(1));
            Assert.That(V(receiver).LiquidId, Is.EqualTo("water"));
            Assert.That(V(otherReceiver).Volume, Is.Zero);
            Assert.That(fire.GetPart<FuelPart>().FuelMass, Is.EqualTo(20));
            var crust = Crust(Zone).Single();
            Assert.That(Zone.GetEntityCell(crust), Is.SameAs(Zone.GetEntityCell(Actor)));
            Assert.That(crust.GetPart<PhysicsPart>().InInventory, Is.Null);
            Assert.True(crust.HasTag("NoTrade"));
            Assert.That(crust.GetPart<PhysicsPart>().Weight, Is.EqualTo(1));
            Assert.That(crust.GetPart<CommercePart>().Value, Is.Zero);
            Assert.IsNull(crust.GetPart<LiquidVesselPart>());
            Assert.That(crust.GetPart<StackerPart>()?.StackCount ?? 1, Is.EqualTo(1));
            Assert.That(crust.GetPart<ReagentPart>().PropertiesRaw, Is.EqualTo("conductive:1"));
            Assert.False(Act(still, retained.Command));
            Assert.That(Crust(Zone).Single(), Is.SameAs(crust));

            var manager = OverworldZoneManager.CreateDetached(Factory, 64);
            manager.ReplaceLoadedState(new Dictionary<string, Zone> { { Zone.ZoneID, Zone } }, Zone.ZoneID,
                new Dictionary<string, List<ZoneConnection>>());
            Clock.RestoreSavedState(Clock.TickCount, true, Actor,
                new List<TurnManager.SavedTurnEntry> { new TurnManager.SavedTurnEntry { Entity = Actor, Energy = 1000 } });
            var loaded = HotbarSaveFixture.RoundTrip(GameSessionState.Capture("retained-brine", "ground-crust", manager, Clock, Actor));
            var loadedZone = loaded.ZoneManager.GetZone(Zone.ZoneID);
            var loadedCrust = Crust(loadedZone).Single();
            Assert.That(loadedCrust.ID, Is.EqualTo(crust.ID));
            Assert.That(loadedZone.GetEntityCell(loadedCrust), Is.SameAs(loadedZone.GetEntityCell(loaded.Player)));
            Assert.True(loadedCrust.HasTag("NoTrade"));
            var loadedPack = loaded.Player.GetPart<InventoryPart>();
            Assert.That(V(loadedPack.Objects.Single(item => item.ID == receiver.ID)).Volume, Is.EqualTo(1));
            Assert.That(V(loadedPack.Objects.Single(item => item.ID == source.ID)).Volume, Is.EqualTo(4));
            Assert.That(loadedZone.GetReadOnlyEntities().Single(item => item.ID == fire.ID).GetPart<FuelPart>().FuelMass, Is.EqualTo(20));
        }

        [Test]
        public void AnExactOneUnitReceiverCanKeepCrustButCannotTakeTheOrdinaryTwoWaterBatch()
        {
            ShowDryWorkCell();
            V(receiver).Capacity = 1;
            Assert.False(Actions(still).Any(row => row.Name == "DistillBrine" && !row.IsDisabled));
            Assert.True(Act(still, KeepRow(receiver).Command));
            Assert.That(V(receiver).Volume, Is.EqualTo(1));
            Assert.That(Crust(Zone).Length, Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MissingOrUnqualifiedCrustLeavesOrdinaryDistillationAvailable(bool missing)
        {
            ShowDryWorkCell();
            string oldCrustQuote = KeepRow(receiver).Command;
            var original = Factory.Blueprints["BrineCrust"];
            string price = original.Parts["Commerce"]["Value"];
            if (missing) Factory.Blueprints.Remove("BrineCrust");
            else original.Parts["Commerce"]["Value"] = "1";
            try
            {
                Assert.False(Actions(still).Any(row => row.Name == KeepCrust && !row.IsDisabled));
                Assert.False(Act(still, oldCrustQuote));
                Assert.True(Act(still, Row()));
                Assert.That(V(receiver).Volume, Is.EqualTo(2));
                Assert.That(Crust(Zone), Is.Empty);
            }
            finally
            {
                original.Parts["Commerce"]["Value"] = price;
                Factory.Blueprints["BrineCrust"] = original;
            }
        }

        [TestCase("wet")]
        [TestCase("object")]
        public void BlockedCrustDestinationRefusesStaleQuoteButOrdinaryWaterStillWorks(string obstruction)
        {
            ShowDryWorkCell();
            string stale = KeepRow(receiver).Command;
            if (obstruction == "wet") Zone.TileState.WriteCoating(10, 10, "water", 5);
            else Place("FireClay", 10, 10);

            Assert.False(Actions(still).Any(row => row.Name == KeepCrust && !row.IsDisabled));
            Assert.False(Act(still, stale));
            Assert.That(V(source).Volume, Is.EqualTo(8));
            Assert.That(V(receiver).Volume, Is.Zero);
            Assert.That(fire.GetPart<FuelPart>().FuelMass, Is.EqualTo(25));
            Assert.That(Crust(Zone), Is.Empty);

            Assert.True(Act(still, Row()));
            Assert.That(V(source).Volume, Is.EqualTo(4));
            Assert.That(V(receiver).Volume, Is.EqualTo(2));
            Assert.That(fire.GetPart<FuelPart>().FuelMass, Is.EqualTo(20));
            Assert.That(Crust(Zone), Is.Empty);
        }

        [Test]
        public void LateWetnessRetractsTheStagedCrustAndRefundsOnlyTheSelectedBatch()
        {
            ShowDryWorkCell();
            Entity stagedCrust = null;
            int hp = Actor.GetStatValue("Hitpoints");
            Actor.AddPart(new DistillationProbe { Run = () =>
            {
                stagedCrust = Crust(Zone).SingleOrDefault();
                Zone.TileState.WriteCoating(10, 10, "water", 5);
                V(source).Volume--;
                fire.GetPart<FuelPart>().FuelMass += 2;
                Actor.GetStat("Hitpoints").Penalty += 2;
            } });
            string command = KeepRow(receiver).Command;

            Assert.False(Act(still, command));
            Assert.NotNull(stagedCrust, "The callback ran after the actual crust had been staged.");
            Assert.That(Zone.GetEntityCell(stagedCrust), Is.Null);
            Assert.That(Crust(Zone), Is.Empty);
            Assert.True(Zone.TileState.HasCoating(10, 10, "water"));
            Assert.That(V(source).Volume, Is.EqualTo(7));
            Assert.That(V(source).LiquidId, Is.EqualTo("brine"));
            Assert.That(V(receiver).Volume, Is.Zero);
            Assert.That(V(receiver).LiquidId, Is.Empty);
            Assert.That(fire.GetPart<FuelPart>().FuelMass, Is.EqualTo(27));
            Assert.That(Actor.GetStatValue("Hitpoints"), Is.EqualTo(hp - 2));
        }

        [Test] public void OneBatchPreservesExcessBrineAndWritesWaterIntoTheExactReceiver()
        {
            Assert.True(Act(still, Row())); Assert.AreEqual(4, V(source).Volume); Assert.AreEqual("brine", V(source).LiquidId);
            Assert.AreEqual(2, V(receiver).Volume); Assert.AreEqual("water", V(receiver).LiquidId);
            Assert.AreEqual(20, fire.GetPart<FuelPart>().FuelMass);
        }
        [Test] public void ExactFourFeedAndFiveFuelCanBeSpentOnce()
        {
            V(source).Volume = 4; fire.GetPart<FuelPart>().FuelMass = 5;
            Assert.True(Act(still, Row())); Assert.AreEqual(0, V(source).Volume); Assert.AreEqual("", V(source).LiquidId);
            Assert.AreEqual(0, fire.GetPart<FuelPart>().FuelMass); Assert.AreEqual(2, V(receiver).Volume);
            Assert.False(Actions(still).Any(a => a.Name == "DistillBrine" && !a.IsDisabled));
        }
        [TestCase("source-volume")][TestCase("source-liquid")][TestCase("receiver")][TestCase("cold")]
        [TestCase("fuel")][TestCase("move")][TestCase("actor")][TestCase("capacity")]
        public void StaleQuoteNeverSubstitutesOrSpends(string change)
        {
            string command = Row();
            switch (change)
            {
                case "source-volume": V(source).Volume = 7; break;
                case "source-liquid": V(source).LiquidId = "acid"; break;
                case "receiver": V(receiver).LiquidId = "oil"; V(receiver).Volume = 1; break;
                case "cold": fire.GetPart<ThermalPart>().Temperature = 25; break;
                case "fuel": fire.GetPart<FuelPart>().FuelMass = 24; break;
                case "move": Zone.MoveEntity(fire, 18, 10); break;
                case "actor": Zone.MoveEntity(Actor, 10, 11); break;
                case "capacity": V(receiver).Capacity = 11; break;
            }
            Assert.False(Act(still, command)); Assert.AreEqual(change == "source-volume" ? 7 : 8, V(source).Volume);
            Assert.AreEqual(change == "receiver" ? 1 : 0, V(receiver).Volume);
            Assert.AreEqual(change == "fuel" ? 24 : 25, fire.GetPart<FuelPart>().FuelMass);
        }
        [TestCase("brine-short")][TestCase("receiver-small")][TestCase("receiver-filled")][TestCase("stacked")]
        [TestCase("fuel-short")][TestCase("cold")][TestCase("wrong-liquid")]
        public void IneligibleInputsHaveNoEnabledDistillation(string change)
        {
            switch (change)
            {
                case "brine-short": V(source).Volume = 3; break;
                case "receiver-small": V(receiver).Capacity = 1; break;
                case "receiver-filled": V(receiver).LiquidId = "water"; V(receiver).Volume = 1; break;
                case "stacked":
                    if (receiver.GetPart<StackerPart>() == null) receiver.AddPart(new StackerPart { StackCount = 2, MaxStack = 20 });
                    else receiver.GetPart<StackerPart>().StackCount = 2;
                    break;
                case "fuel-short": fire.GetPart<FuelPart>().FuelMass = 4; break;
                case "cold": fire.GetPart<ThermalPart>().Temperature = 25; break;
                case "wrong-liquid": V(source).LiquidId = "bog-mire"; break;
            }
            Assert.False(Actions(still).Any(a => a.Name == "DistillBrine" && !a.IsDisabled));
        }
        [Test] public void CancelledOuterActionRestoresOnlyItsLiquidAndFuelDeltas()
        {
            int hp = Actor.GetStatValue("Hitpoints");
            Actor.AddPart(new DistillationProbe { Run = () => { V(source).Volume--; Actor.GetStat("Hitpoints").Penalty += 2; fire.GetPart<FuelPart>().FuelMass += 2; } });
            string command = Row();
            Assert.False(Act(still, command)); Assert.AreEqual(7, V(source).Volume); Assert.AreEqual(0, V(receiver).Volume);
            Assert.AreEqual("", V(receiver).LiquidId); Assert.AreEqual(27, fire.GetPart<FuelPart>().FuelMass);
            Assert.AreEqual(hp - 2, Actor.GetStatValue("Hitpoints"));
        }
        [Test] public void RepeatedRetainedCommandCannotDistillAgain()
        {
            string command = Row(); Assert.True(Act(still, command)); Assert.False(Act(still, command));
            Assert.AreEqual(4, V(source).Volume); Assert.AreEqual(2, V(receiver).Volume);
        }
        [Test] public void Review_MultipleVesselPairsAreVisiblyDistinguishable()
        {
            Flask(4, "brine"); Flask(0, "", 8);
            var rows = Actions(still).Where(a => a.Name == "DistillBrine" && !a.IsDisabled).ToArray();
            Assert.AreEqual(4, rows.Length); Assert.AreEqual(4, rows.Select(a => a.Display).Distinct().Count());
            Assert.True(rows.Any(a => a.Display.Contains("8/12"))); Assert.True(rows.Any(a => a.Display.Contains("0/8")));
        }
        [Test] public void ProducedWaterAndExcessBrineSurviveFullGraphReload()
        {
            string s = source.ID, r = receiver.ID; Assert.True(Act(still, Row()));
            var manager = OverworldZoneManager.CreateDetached(Factory, 64);
            manager.ReplaceLoadedState(new Dictionary<string, Zone> { { Zone.ZoneID, Zone } }, Zone.ZoneID, new Dictionary<string, List<ZoneConnection>>());
            Clock.RestoreSavedState(Clock.TickCount, true, Actor, new List<TurnManager.SavedTurnEntry> { new TurnManager.SavedTurnEntry { Entity = Actor, Energy = 1000 } });
            var loaded = HotbarSaveFixture.RoundTrip(GameSessionState.Capture("distillation", "finite-brine", manager, Clock, Actor));
            var pack = loaded.Player.GetPart<InventoryPart>();
            Assert.AreEqual(4, pack.Objects.Single(e => e.ID == s).GetPart<LiquidVesselPart>().Volume);
            var water = pack.Objects.Single(e => e.ID == r).GetPart<LiquidVesselPart>();
            Assert.AreEqual(2, water.Volume); Assert.AreEqual("water", water.LiquidId);
        }
        public sealed class DistillationProbe : Part
        {
            public Action Run;
            public override bool HandleEvent(GameEvent e) { if (e.ID != "AfterInventoryAction") return true; Run(); return false; }
        }
    }
}
