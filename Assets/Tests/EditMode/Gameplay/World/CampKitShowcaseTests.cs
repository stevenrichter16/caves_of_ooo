using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Scenarios;
using CavesOfOoo.Scenarios.Custom;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The "Camp Kits" scenario is a hand-play fixture, so what it must not do is quietly rot: a renamed blueprint would hand the player nothing (the builder only logs
    /// and skips), and a pack too heavy to hold the kits would drop the later ones without a word. These pin the loadout against the real content.
    /// </summary>
    public sealed class CampKitShowcaseTests : FiftyWorldFixture
    {
        static readonly string[] Kits = { "CampBox", "CampHearth", "CampBench", "CampStill" };

        [SetUp] public void SetupShowcase() => PackableFixtureRegistry.ResetForTests();
        [TearDown] public void TearDownShowcase() => PackableFixtureRegistry.ResetForTests();

        [Test] public void EveryLoadoutBlueprintExistsInTheRealContent()
        {
            foreach (var (blueprint, count) in CampKitsShowcase.Loadout)
            { Assert.NotNull(Factory.CreateEntity(blueprint), blueprint + " must be a real blueprint"); Assert.Greater(count, 0, blueprint); }
            Assert.NotNull(Factory.CreateEntity(CampKitsShowcase.FireBlueprint), "the fire the torch is lit at");
            LogAssert.Expect(LogType.Error, "EntityFactory: unknown blueprint 'NoSuchBlueprintForTheShowcase'");
            Assert.IsNull(Factory.CreateEntity("NoSuchBlueprintForTheShowcase"), "counter: the check is real, an unknown name is null");
        }

        [Test] public void AllFourKitsAreGivenAndEachIsAShippedPackableKind()
        {
            foreach (var kit in Kits)
            {
                Assert.True(CampKitsShowcase.Loadout.Any(l => l.Blueprint == kit && l.Count == 1), kit + " is in the loadout exactly once");
                var part = Factory.CreateEntity(kit).GetPart<PackableFixturePart>();
                Assert.NotNull(part, kit + " is a packable fixture");
                Assert.NotNull(PackableFixtureRegistry.Get(part.Kind), kit + " has a registered kind, so set down finds a definition");
            }
        }

        [Test] public void TheLoadoutIsLightEnoughForTheSmallestPlayerToCarry()
        {
            int weight = CampKitsShowcase.Loadout.Sum(l => (Factory.CreateEntity(l.Blueprint).GetPart<PhysicsPart>()?.Weight ?? 0) * l.Count);
            Assert.LessOrEqual(weight, 240, "Strength 16 carries 240; a heavier fixture would silently drop its last items");
            Assert.GreaterOrEqual(weight, 66, "counter: the four kits alone weigh 66, so a zero here means the weight read is broken");
        }

        [Test] public void TheLoadoutCarriesWhatEachKitNeedsToBeTried()
        {
            bool Has(string bp, int n = 1) => CampKitsShowcase.Loadout.Any(l => l.Blueprint == bp && l.Count >= n);
            Assert.True(Has("SalvagedTimber", 4), "hearth fuel (one timber is ten fuel)");
            Assert.True(Has("Torch"), "the carried flame that relights the hearth");
            Assert.True(Has("IronSpikeComponent") && Has("OakHaftComponent") && Has("LeatherBindingComponent"), "a craftable set for the bench");
            Assert.True(Has("MendleafSprig", 2) && Has("FireMoss"), "reagents for the still");
            Assert.True(Has("Dagger"), "something to put in the box");
            Assert.True(Has("FlintStriker"), "a flame where none burns (the spark path)");
            Assert.True(Has("IronSpikeComponent", 2), "one spike for the bench craft and one to knap the striker");
            Assert.True(Has("LiquidFlask"), "a flask to fill with mire for the still");
            Assert.AreEqual(LiquidSettlingPart.Feed, CampKitsShowcase.MireLiquid, "the flask is filled with the liquid the still settles");
            Assert.AreEqual(0, CampKitsShowcase.MireUnits % LiquidSettlingPart.Ratio, "a whole number of batches, no dregs");
        }

        // ── the wild-ground variants ──

        ScenarioContext Context(out TurnManager turns) { turns = new TurnManager(); return new ScenarioContext(Zone, Factory, Actor, turns); }

        [Test] public void TheWildScenariosGiveTheSameSuppliesAsTheOpeningOne()
        {
            foreach (var scenario in new IScenario[] { new CampKitsWildShowcase(), new CampKitsWildNightShowcase() })
            {
                var ctx = Context(out _); Pack.MaxWeight = 400; scenario.Apply(ctx);
                foreach (var kit in Kits) Assert.True(Pack.Objects.Any(o => o.BlueprintName == kit), scenario.GetType().Name + " gives " + kit);
                Assert.True(Pack.Objects.Any(o => o.BlueprintName == "FlintStriker")); Assert.AreEqual(CampKitsShowcase.MireUnits, Pack.Objects.Single(o => o.BlueprintName == "LiquidFlask").GetPart<LiquidVesselPart>().Volume);
                Assert.True(Zone.GetReadOnlyEntities().Any(e => e.BlueprintName == CampKitsShowcase.FireBlueprint), "the campfire");
                foreach (var e in Pack.Objects.ToArray()) Pack.RemoveObject(e);
                foreach (var e in Zone.GetReadOnlyEntities().Where(e => e.BlueprintName == CampKitsShowcase.FireBlueprint).ToArray()) Zone.RemoveEntity(e);
            }
        }

        [Test] public void TheNightScenarioStartsAtDarkAndHurt_AndTheDayOneStartsAtNeitherCounter()
        {
            Pack.MaxWeight = 400; var hp = Actor.GetStat("Hitpoints"); int max = hp.Max;
            var day = Context(out var dayTurns); new CampKitsWildShowcase().Apply(day); Assert.AreEqual(0, dayTurns.TickCount); Assert.AreEqual(max, hp.Value, "the daytime variant leaves you whole");
            foreach (var e in Pack.Objects.ToArray()) Pack.RemoveObject(e);
            var night = Context(out var nightTurns); new CampKitsWildNightShowcase().Apply(night);
            Assert.AreEqual(DayBand.Dark, WorldClock.GetBand(nightTurns.TickCount)); Assert.AreEqual(900, nightTurns.TickCount); Assert.AreEqual((int)Math.Round(max * CampKitsWildNightShowcase.HealthFraction), hp.Value, 1);
            Assert.Less(hp.Value, max);
        }

        [Test] public void TheDarkStartNeverRunsTheClockBackwards()
        {
            Assert.AreEqual(900, CampKitsWildNightShowcase.DarkStart(0)); Assert.AreEqual(900, CampKitsWildNightShowcase.DarkStart(899)); Assert.AreEqual(900, CampKitsWildNightShowcase.DarkStart(900));
            Assert.AreEqual(1000, CampKitsWildNightShowcase.DarkStart(1000), "already dark: stay"); Assert.AreEqual(2100, CampKitsWildNightShowcase.DarkStart(1200), "the next day's dark");
            Assert.AreEqual(2100, CampKitsWildNightShowcase.DarkStart(1500));
            for (int tick = 0; tick < 3000; tick += 37) { int to = CampKitsWildNightShowcase.DarkStart(tick); Assert.GreaterOrEqual(to, tick); Assert.AreEqual(DayBand.Dark, WorldClock.GetBand(to)); }
        }

        [Test] public void AWildStartIsAnOrdinarySpreadZoneTheKitRulesAcceptAtEverySeed()
        {
            foreach (int seed in new[] { 64, 65, 7, 1234 })
            {
                var manager = OverworldZoneManager.CreateDetached(Factory, seed, true); string id = FreshGameStart.WildStartZoneId(manager);
                Assert.NotNull(id, "seed " + seed); Assert.AreEqual(HeldPlaceZoneClass.WildSurface, HeldPlaceZones.Classify(manager, id));
                var (x, y, _) = WorldMap.FromZoneID(id); Assert.AreEqual(BiomeType.Spread, manager.WorldMap.GetBiome(x, y)); Assert.AreEqual(id, FreshGameStart.WildStartZoneId(manager), "the same seed gives the same start");
            }
            Assert.IsNull(FreshGameStart.WildStartZoneId(null));
        }

        [Test] public void ResolvingTheStartZoneHonoursTheSceneThenTheScenarioAndOtherwiseChangesNothing()
        {
            var manager = OverworldZoneManager.CreateDetached(Factory, 64, true); string wild = FreshGameStart.WildStartZoneId(manager);
            Assert.AreEqual("Overworld.3.3.0", FreshGameStart.ResolveStartZone("Overworld.3.3.0", true, true, manager), "the scene's own destination wins");
            Assert.AreEqual(wild, FreshGameStart.ResolveStartZone("", true, true, manager)); Assert.AreEqual(wild, FreshGameStart.ResolveStartZone(null, true, true, manager));
            Assert.AreEqual("", FreshGameStart.ResolveStartZone("", false, true, manager), "no scenario pending: an ordinary game never reads the request");
            Assert.AreEqual("", FreshGameStart.ResolveStartZone("", true, false, manager), "a scenario that did not ask for wild ground keeps the ordinary start");
            Assert.AreEqual("", FreshGameStart.ResolveStartZone("  ", false, false, manager));
        }

        [Test] public void ApplyingAScenarioClearsTheWildStartRequestSoItCannotLeakIntoALaterGame()
        {
            var handle = typeof(ScenarioRunner).GetMethod("HandleAfterBootstrap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static); Assert.NotNull(handle);
            bool oldFlag = ScenarioRunner.PendingWildStart; var oldType = ScenarioRunner.PendingScenario; Pack.MaxWeight = 400;
            try
            {
                ScenarioRunner.PendingScenario = typeof(CampKitsWildShowcase); ScenarioRunner.PendingWildStart = true;
                handle.Invoke(null, new object[] { Zone, Factory, Actor, new TurnManager() });
                Assert.IsNull(ScenarioRunner.PendingScenario); Assert.False(ScenarioRunner.PendingWildStart, "one-shot, like the scenario itself");
            }
            finally { ScenarioRunner.PendingWildStart = oldFlag; ScenarioRunner.PendingScenario = oldType; }
        }

        // ── the awning's fixture: a wild start in the Beating at the Height hours ──

        [Test] public void AWildStartCanBeAskedForTheBeatingAndStillDefaultsToTheSpread_Counter()
        {
            foreach (int seed in new[] { 64, 65, 7, 1234 })
            {
                var manager = OverworldZoneManager.CreateDetached(Factory, seed, true); string id = FreshGameStart.WildStartZoneId(manager, BiomeType.Beating);
                Assert.NotNull(id, "seed " + seed + ": there is wild ground in the Beating"); Assert.AreEqual(HeldPlaceZoneClass.WildSurface, HeldPlaceZones.Classify(manager, id));
                var (x, y, _) = WorldMap.FromZoneID(id); Assert.AreEqual(BiomeType.Beating, manager.WorldMap.GetBiome(x, y)); Assert.AreEqual(0, WorldMap.FromZoneID(id).z, "the surface");
                var spread = FreshGameStart.WildStartZoneId(manager); var (sx, sy, _) = WorldMap.FromZoneID(spread);
                Assert.AreEqual(BiomeType.Spread, manager.WorldMap.GetBiome(sx, sy), "the default is unchanged"); Assert.AreEqual(spread, FreshGameStart.WildStartZoneId(manager, BiomeType.Spread));
            }
        }

        [Test] public void ResolvingTheStartZoneCarriesTheAskedBiomeAndIgnoresItWhenNoWildStartWasAsked_Counter()
        {
            var manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            Assert.AreEqual(FreshGameStart.WildStartZoneId(manager, BiomeType.Beating), FreshGameStart.ResolveStartZone("", true, true, manager, BiomeType.Beating));
            Assert.AreEqual(FreshGameStart.WildStartZoneId(manager), FreshGameStart.ResolveStartZone("", true, true, manager), "no biome asked: the Spread, as before");
            Assert.AreEqual("", FreshGameStart.ResolveStartZone("", true, false, manager, BiomeType.Beating), "a biome without a wild-start request changes nothing");
            Assert.AreEqual("Overworld.3.3.0", FreshGameStart.ResolveStartZone("Overworld.3.3.0", true, true, manager, BiomeType.Beating), "the scene's own destination still wins");
        }

        [Test] public void TheWildBiomeRequestIsOneShotLikeTheWildStartItself()
        {
            var handle = typeof(ScenarioRunner).GetMethod("HandleAfterBootstrap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var oldBiome = ScenarioRunner.PendingWildBiome; var oldFlag = ScenarioRunner.PendingWildStart; var oldType = ScenarioRunner.PendingScenario; Pack.MaxWeight = 400;
            try
            {
                ScenarioRunner.PendingScenario = typeof(CampAwningShowcase); ScenarioRunner.PendingWildStart = true; ScenarioRunner.PendingWildBiome = BiomeType.Beating;
                handle.Invoke(null, new object[] { Zone, Factory, Actor, new TurnManager() });
                Assert.AreEqual(BiomeType.Spread, ScenarioRunner.PendingWildBiome, "a later scenario that asks for a wild start gets the Spread unless it asks otherwise");
            }
            finally { ScenarioRunner.PendingWildBiome = oldBiome; ScenarioRunner.PendingWildStart = oldFlag; ScenarioRunner.PendingScenario = oldType; }
        }

        [Test] public void TheAwningScenarioStartsAtTheHeightBandBareheadedWithTheAwningAndNothingMoreThanItNeeds()
        {
            Pack.MaxWeight = 400; var ctx = Context(out var turns); new CampAwningShowcase().Apply(ctx);
            Assert.AreEqual(DayBand.Height, WorldClock.GetBand(turns.TickCount), "the glare only burns in the Height band");
            Assert.NotNull(Pack.Objects.SingleOrDefault(o => o.BlueprintName == "CampAwning"), "the awning is in the pack");
            Assert.False(BeatingGlareSystem.HasHeadCover(Actor), "nothing on the head: the glare would parch you");
            foreach (var (blueprint, count) in CampAwningShowcase.Loadout) Assert.GreaterOrEqual(Pack.Objects.Where(o => o.BlueprintName == blueprint).Sum(o => o.GetPart<StackerPart>()?.StackCount ?? 1), count, blueprint);
        }

        [Test] public void TheAwningScenarioKeepsTheClockWhereItIsWhenItIsAlreadyHeightAndNeverRunsItBackwards_Counter()
        {
            Pack.MaxWeight = 400; var ctx = Context(out var turns); turns.AdvanceClock(WorldClock.DayLengthTicks + (int)DayBand.Height * WorldClock.BandLengthTicks + 50);
            int before = turns.TickCount; new CampAwningShowcase().Apply(ctx); Assert.AreEqual(before, turns.TickCount, "already Height: no change");
            var late = Context(out var turns2); turns2.AdvanceClock((int)DayBand.Dusk * WorldClock.BandLengthTicks + 10); new CampAwningShowcase().Apply(late);
            Assert.AreEqual(DayBand.Height, WorldClock.GetBand(turns2.TickCount)); Assert.GreaterOrEqual(turns2.TickCount, WorldClock.DayLengthTicks, "past Height today: tomorrow's");
        }

        // ── the newer machines' fixture ──

        [Test] public void EveryMachinesLoadoutBlueprintExistsAndTheLoadoutIsCarriable_AndItGivesWhatEachMachineNeeds()
        {
            foreach (var (blueprint, count) in CampMachinesShowcase.Loadout) { Assert.NotNull(Factory.CreateEntity(blueprint), blueprint); Assert.Greater(count, 0, blueprint); }
            int weight = CampMachinesShowcase.Loadout.Sum(l => (Factory.CreateEntity(l.Blueprint).GetPart<PhysicsPart>()?.Weight ?? 0) * l.Count); Assert.LessOrEqual(weight, 240, "Strength 16 carries 240");
            bool Has(string bp, int n = 1) => CampMachinesShowcase.Loadout.Any(l => l.Blueprint == bp && l.Count >= n);
            Assert.True(Has("CampTrough") && Has("LeatherArmor") && Has("PitchpodResin", 2) && Has("FireClay"), "steeping"); Assert.True(Has("SoundingRod"), "sounding");
            Assert.True(Has("CampHearth") && Has("CampStill") && Has("LiquidFlask", 2) && Has("SalvagedTimber", 4) && Has("FlintStriker"), "baking clay");
            var data = PlaceSiteCatalog.Get("Cinderhold", "Pump") ?? Load(); foreach (var m in data.Lasting.Materials) Assert.True(Has(m.Blueprint, m.Count), "the pump's " + m.Blueprint);
            Assert.AreEqual(0, CampMachinesShowcase.MireUnits % 1); Assert.GreaterOrEqual(CampMachinesShowcase.MireUnits, LiquidSettlingPart.ClayRatio, "each flask can bake a clay");
        }

        static PlaceSiteData Load() { PlaceSiteCatalog.LoadFromJson(System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath, "Resources/Content/Data/PlaceSites.json"))); return PlaceSiteCatalog.Get("Cinderhold", "Pump"); }

        [Test] public void TheMachinesScenarioFillsBothFlasksWithMireAndGivesEverythingItNames()
        {
            Pack.MaxWeight = 400; var ctx = Context(out _); new CampMachinesShowcase().Apply(ctx);
            foreach (var (blueprint, count) in CampMachinesShowcase.Loadout) Assert.GreaterOrEqual(Pack.Objects.Where(o => o.BlueprintName == blueprint).Sum(o => o.GetPart<StackerPart>()?.StackCount ?? 1), count, blueprint);
            var flasks = Pack.Objects.Where(o => o.BlueprintName == "LiquidFlask").Select(o => o.GetPart<LiquidVesselPart>()).ToArray(); Assert.AreEqual(2, flasks.Length); Assert.True(flasks.All(f => f.LiquidId == CampMachinesShowcase.MireLiquid && f.Volume == CampMachinesShowcase.MireUnits));
            Assert.AreEqual(LiquidSettlingPart.Feed, CampMachinesShowcase.MireLiquid, "the liquid the still settles and bakes");
        }

        [Test] public void TheMachinesScenarioPutsASteamVentSixCellsEastSoTheStillCanSettleWithoutAFire()
        {
            Pack.MaxWeight = 400; var ctx = Context(out _); var me = Zone.GetEntityPosition(Actor); new CampMachinesShowcase().Apply(ctx);
            var vent = Zone.GetReadOnlyEntities().SingleOrDefault(e => e.BlueprintName == "SteamVent"); Assert.NotNull(vent, "the vent is spawned"); var at = Zone.GetEntityPosition(vent);
            Assert.AreEqual(me.x + CampMachinesShowcase.VentOffset, at.x); Assert.AreEqual(me.y, at.y); Assert.True(NaturalHeat.IsSource(vent), "and it is a real heat source for the still");
        }

        [Test] public void TheMachinesScenarioDigsAFishablePondWestSoTheTrapCanBeTriedWithoutATrip()
        {
            Pack.MaxWeight = 400; var ctx = Context(out _); var me = Zone.GetEntityPosition(Actor);
            int shoreX = me.x - (CampMachinesShowcase.PondEdge - 1);
            Assert.False(FishPond.TryFind(Zone, shoreX, me.y, out _), "counter: before the scenario there is no pond beside that cell");
            new CampMachinesShowcase().Apply(ctx);
            Assert.True(FishPond.TryFind(Zone, shoreX, me.y, out var anchor), "the cell on the pond's near shore is beside a pond a trap can fish");
            Assert.GreaterOrEqual(FishPond.Component(Zone, shoreX - 1, me.y).Count, FishPond.MinCells, "and it is big enough");
            Assert.AreEqual(CampMachinesShowcase.PondWidth * CampMachinesShowcase.PondHeight, Zone.GetReadOnlyEntities().Count(e => e.BlueprintName == FishPond.LegacyBlueprint));
        }

        // ── the Concord carriage fixture ──

        [Test] public void TheCarriageScenarioGivesDramsGoodsAndOneParcelAlreadyWaitingAtCinderhold()
        {
            foreach (var (blueprint, count) in ConcordCarriageShowcase.Loadout) { Assert.NotNull(Factory.CreateEntity(blueprint), blueprint); Assert.Greater(count, 0, blueprint); }
            Assert.NotNull(Factory.CreateEntity(ConcordCarriageShowcase.WaitingBlueprint), "the waiting parcel is a real blueprint");
            Pack.MaxWeight = 400; var ctx = Context(out var turns); new ConcordCarriageShowcase().Apply(ctx);
            Assert.AreEqual(ConcordCarriageShowcase.Drams, TradeSystem.GetDrams(Actor));
            foreach (var (blueprint, count) in ConcordCarriageShowcase.Loadout) Assert.GreaterOrEqual(Pack.Objects.Where(o => o.BlueprintName == blueprint).Sum(o => o.GetPart<StackerPart>()?.StackCount ?? 1), count, blueprint);
            var cart = Actor.GetPart<ConcordCarriagePart>(); Assert.NotNull(cart); Assert.AreEqual(1, cart.Count);
            string cinderhold = WorldMap.ToZoneID(6, 6, 0), counter = WorldMap.ToZoneID(18, 18, 0);
            Assert.AreEqual(cinderhold, cart.To[0]); Assert.AreEqual(counter, cart.From[0]); Assert.AreEqual(ConcordCarriageShowcase.WaitingBlueprint, cart.Goods[0].BlueprintName);
            Assert.AreEqual(1, ConcordCarriage.ReadyHere(Actor, cinderhold, turns.TickCount), "it is ready to collect at Cinderhold at once");
            Assert.AreEqual(0, ConcordCarriage.ReadyHere(Actor, counter, turns.TickCount), "counter: nothing is waiting at the Last Counter");
            Assert.True(ConcordCarriage.Eligible(Actor, Factory.CreateEntity("ConcordFactor")).Count > 0, "the goods include things the cart will take");
            Assert.True(Pack.Objects.Any(o => o.BlueprintName == "BlastcapSpore"), "and a spore it will refuse, to see clause five");
            foreach (var e in Pack.Objects.ToArray()) Pack.RemoveObject(e);
            Actor.RemovePart(cart);
        }
    }
}
