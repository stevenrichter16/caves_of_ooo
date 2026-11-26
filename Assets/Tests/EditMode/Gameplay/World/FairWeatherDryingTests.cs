using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class FairWeatherDryingTests
    {
        const string North = "Overworld.11.9.0", Offer = "DryInFairAir|";
        static readonly FieldInfo ActiveClock = typeof(TurnManager).GetField("<Active>k__BackingField",
            BindingFlags.Static | BindingFlags.NonPublic);

        HaulingContentScope content;
        TurnManager previousClock, clock;
        Zone previousZone, zone;
        Entity actor, shelf, striker;
        WetEffect wet;

        [SetUp]
        public void OpenNorthWorks()
        {
            previousClock = TurnManager.Active;
            previousZone = SettlementRuntime.ActiveZone;
            content = new HaulingContentScope();
            var factory = content.Factory;
            var manager = OverworldZoneManager.CreateDetached(factory, 64, true);
            content.Seed(unchecked(64 ^ FormationSelector.StableIndex(North, int.MaxValue)));
            zone = manager.GetZone(North);
            manager.SetActiveZone(zone);
            clock = new TurnManager();
            Assert.False(Weather().Raining);
            Assert.True(Weather().Valid);
            shelf = zone.GetReadOnlyEntities().First(e => e.GetProperty("SpreadWorksite.Role") == "drying-shelf");
            var (x, y) = zone.GetEntityPosition(shelf);
            actor = factory.CreateEntity("Player");
            var approach = new[] { (x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1) }
                .First(p => zone.CanPlaceFootprint(actor, p.Item1, p.Item2));
            Assert.True(zone.AddEntity(actor, approach.Item1, approach.Item2));
            var at = zone.GetEntityCell(shelf);
            at.IsVisible = at.Explored = true;
            striker = factory.CreateEntity("FlintStriker");
            Assert.True(actor.GetPart<InventoryPart>().AddObject(striker));
            wet = new WetEffect(.5f);
            Assert.True(striker.ApplyEffect(wet));
            wet.Moisture = .5f;
        }

        [TearDown]
        public void RestoreScope()
        {
            content?.Dispose();
            ActiveClock.SetValue(null, previousClock);
            SettlementRuntime.ActiveZone = previousZone;
        }

        RegionalWeather.Observation Weather()
            => RegionalWeather.Current(zone, clock.TickCount, RegionalWeather.PolicyVersion);

        InventoryAction[] Choices()
        {
            var at = zone.GetEntityCell(shelf);
            return WorldInteractionSystem.GatherCellActions(shelf, at, actor)
                .Where(row => row.Command.StartsWith(Offer, StringComparison.Ordinal)).ToArray();
        }

        bool Act(Entity subject, string command)
            => InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(subject, command), actor, zone).Success;

        [Test]
        public void AiredStrikerKeepsItsIdentityAndLightsAnOrdinaryTorchWithOneStrike()
        {
            var torch = content.Factory.CreateEntity("Torch");
            Assert.True(actor.GetPart<InventoryPart>().AddObject(torch));
            Assert.True(InventorySystem.Equip(actor, torch));
            Assert.True(Act(torch, "ExtinguishTorch"));
            Assert.False(torch.GetPart<ThermalPart>().IsAflame);
            Assert.False(Act(torch, "LightTorch"));
            var tool = striker.GetPart<FlintStrikerPart>();
            int original = tool.Strikes;
            var row = Choices().SingleOrDefault();
            Assert.NotNull(row, "The built shelf should offer one exact wet item.");
            Assert.AreEqual(ActionTime.OneAction, row.Time);
            Assert.True(Act(row.Via ?? shelf, row.Command));
            Assert.AreSame(wet, striker.GetEffect<WetEffect>());
            Assert.AreEqual(.25f, wet.Moisture);
            Assert.AreSame(actor, striker.GetPart<PhysicsPart>().InInventory);
            Assert.AreEqual(original, tool.Strikes);
            Assert.False(Act(row.Via ?? shelf, row.Command));
            Assert.True(Act(torch, "LightTorch"));
            Assert.True(torch.GetPart<ThermalPart>().IsAflame);
            Assert.AreEqual(original - 1, tool.Strikes);
            Assert.IsEmpty(shelf.GetPart<ContainerPart>().Contents);
        }

        [TestCase("rain")]
        [TestCase("roof")]
        [TestCase("wet floor")]
        public void AChangedConditionInvalidatesTheExactOfferWithoutDrying(string changed)
        {
            var row = Choices().SingleOrDefault();
            Assert.NotNull(row);
            var position = zone.GetEntityCell(actor);
            if (changed == "rain")
            {
                int rainyTick = Enumerable.Range(1, RegionalWeather.CycleWindows)
                    .Select(n => n * RegionalWeather.WindowTicks)
                    .First(t => RegionalWeather.Current(zone, t, RegionalWeather.PolicyVersion).Raining);
                clock.AdvanceClock(rainyTick);
                Assert.True(Weather().Raining);
            }
            else if (changed == "roof") position.IsInterior = true;
            else zone.TileState.WriteCoating(position.X, position.Y, "water", ZoneTileState.Permanent);
            Assert.False(Act(row.Via ?? shelf, row.Command));
            Assert.IsEmpty(Choices());
            Assert.AreSame(wet, striker.GetEffect<WetEffect>());
            Assert.AreEqual(.5f, wet.Moisture);
        }

        [Test]
        public void FailedDryingRestoresItsOwnQuarterAndKeepsIndependentWetness()
        {
            var row = Choices().SingleOrDefault();
            Assert.NotNull(row);
            actor.AddPart(new FiftyWorldEventProbe
            {
                Callback = e =>
                {
                    if (e.ID == "AfterInventoryAction") wet.Moisture += .10f;
                }
            });
            Assert.False(Act(row.Via ?? shelf, row.Command));
            Assert.AreSame(wet, striker.GetEffect<WetEffect>());
            Assert.AreEqual(.60f, wet.Moisture, .0001f);
        }

        [Test]
        public void UndoDoesNotRewetAnEffectThatALateObserverRehomed()
        {
            var row = Choices().SingleOrDefault();
            Assert.NotNull(row);
            var second = content.Factory.CreateEntity("FlintStriker");
            var effects = striker.GetPart<StatusEffectsPart>();
            actor.AddPart(new FiftyWorldEventProbe
            {
                Callback = e =>
                {
                    if (e.ID == "AfterInventoryAction")
                    {
                        second.AddPart(effects);
                        wet.Owner = second;
                    }
                }
            });
            Assert.False(Act(row.Via ?? shelf, row.Command));
            Assert.AreSame(second, effects.ParentEntity);
            Assert.AreSame(second, wet.Owner);
            Assert.AreEqual(.25f, wet.Moisture);
        }

        [Test]
        public void ASmallRemainderClearsWithoutChangingTheItemOrGrantingMoreStrikes()
        {
            wet.Moisture = .10f;
            var tool = striker.GetPart<FlintStrikerPart>();
            int strikes = tool.Strikes;
            var row = Choices().SingleOrDefault();
            Assert.NotNull(row);
            Assert.True(Act(row.Via ?? shelf, row.Command));
            Assert.False(striker.HasEffect<WetEffect>());
            Assert.IsEmpty(Choices());
            Assert.AreEqual(strikes, tool.Strikes);
            Assert.AreSame(actor, striker.GetPart<PhysicsPart>().InInventory);
        }

        [Test]
        public void AnOrdinaryShelfOffersItsNormalStorageWithoutAWeatherAction()
        {
            var (x, y) = zone.GetEntityPosition(shelf);
            Assert.True(zone.RemoveEntity(shelf));
            shelf = content.Factory.CreateEntity("AlchemyShelf");
            Assert.True(zone.AddEntity(shelf, x, y));
            Assert.True(InventorySystem.GetActions(actor, shelf).Any(row => row.Command == "OpenContainer"));
            Assert.IsEmpty(Choices());
            Assert.AreEqual(.5f, wet.Moisture);
        }
    }
}
