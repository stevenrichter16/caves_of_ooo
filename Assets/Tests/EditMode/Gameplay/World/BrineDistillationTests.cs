using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class BrineDistillationTests : FiftyWorldFixture
    {
        Entity still, fire, source, receiver;
        [SetUp] public void SetUpDistillation()
        {
            Pack.MaxWeight = 400; still = Place("AlchemyStill");
            fire = Place("SpreadCookingCoals", 9, 10);
            fire.GetPart<FuelPart>().MaxFuel = 50; fire.GetPart<FuelPart>().FuelMass = 25; fire.GetPart<ThermalPart>().Temperature = 500;
            source = Flask(8, "brine"); receiver = Flask(0, "");
        }
        string Row() => Actions(still).Single(a => a.Name == "DistillBrine" && !a.IsDisabled).Command;
        LiquidVesselPart V(Entity e) => e.GetPart<LiquidVesselPart>();
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
