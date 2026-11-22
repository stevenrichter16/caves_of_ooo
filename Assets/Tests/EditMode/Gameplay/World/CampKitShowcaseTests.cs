using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Scenarios;
using CavesOfOoo.Scenarios.Builders;
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
            Assert.LessOrEqual(weight, 240, "Strength 16 carries 240 before it is overburdened (the soft limit; the hard pack cap of 150 is pinned further down, by what the scenarios give at that cap)");
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
            bool oldFlag = ScenarioRunner.PendingWildStart; var oldType = ScenarioRunner.PendingScenario; var oldLast = ScenarioRunner.LastLaunched; Pack.MaxWeight = 400;
            try
            {
                ScenarioRunner.PendingScenario = typeof(CampKitsWildShowcase); ScenarioRunner.PendingWildStart = true;
                handle.Invoke(null, new object[] { Zone, Factory, Actor, new TurnManager() });
                Assert.IsNull(ScenarioRunner.PendingScenario); Assert.False(ScenarioRunner.PendingWildStart, "one-shot, like the scenario itself");
            }
            finally { ScenarioRunner.PendingWildStart = oldFlag; ScenarioRunner.PendingScenario = oldType; ScenarioRunner.LastLaunched = oldLast; }
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
            var oldLast = ScenarioRunner.LastLaunched; var oldBiome = ScenarioRunner.PendingWildBiome; var oldFlag = ScenarioRunner.PendingWildStart; var oldType = ScenarioRunner.PendingScenario; Pack.MaxWeight = 400;
            try
            {
                ScenarioRunner.PendingScenario = typeof(CampAwningShowcase); ScenarioRunner.PendingWildStart = true; ScenarioRunner.PendingWildBiome = BiomeType.Beating;
                handle.Invoke(null, new object[] { Zone, Factory, Actor, new TurnManager() });
                Assert.AreEqual(BiomeType.Spread, ScenarioRunner.PendingWildBiome, "a later scenario that asks for a wild start gets the Spread unless it asks otherwise");
            }
            finally { ScenarioRunner.PendingWildBiome = oldBiome; ScenarioRunner.PendingWildStart = oldFlag; ScenarioRunner.PendingScenario = oldType; ScenarioRunner.LastLaunched = oldLast; }
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

        // ── the sounding rod's fixture: a wild start one floor down, under ground the rod accepts ──

        [Test] public void ADepthOneWildStartIsAnOrdinaryCaveTheFreshGamePlacesTheActorInAndTheRodCanSound()
        {
            foreach (int seed in new[] { 64, 65, 7, 1234 })
            {
                var manager = OverworldZoneManager.CreateDetached(Factory, seed, true); string id = FreshGameStart.WildStartZoneId(manager, BiomeType.Spread, 1);
                Assert.NotNull(id, "seed " + seed); var (x, y, depth) = WorldMap.FromZoneID(id); Assert.AreEqual(1, depth, "one floor down");
                Assert.AreEqual(HeldPlaceZoneClass.WildSurface, HeldPlaceZones.Classify(manager, WorldMap.ToZoneID(x, y, 0)), "under ordinary wild ground, as the camp kits want");
                Assert.AreNotEqual(SoundingRodPart.GleanersCellarZone, id);
                var fresh = Factory.CreateEntity("Player"); var brain = fresh.GetPart<BrainPart>(); if (brain != null) fresh.RemovePart(brain);
                Assert.True(FreshGameStart.TryPlace(manager, fresh, id, out var placed), "seed " + seed + ": the fresh game finds a safe cell in the cave"); Assert.AreEqual(id, placed.ZoneID);
                Assert.IsNull(SoundingRodPart.Refusal(fresh, placed, out var why, out _), "seed " + seed + ": the rod can sound here: " + why);
                Assert.AreEqual(FreshGameStart.WildStartZoneId(manager, BiomeType.Spread, 0), WorldMap.ToZoneID(x, y, 0), "the surface request and the cave request name the same column");
            }
        }

        [Test] public void TheDepthIsCarriedThroughTheResolverAndIgnoredWithoutAWildStart_Counter()
        {
            var manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            Assert.AreEqual(FreshGameStart.WildStartZoneId(manager, BiomeType.Spread, 1), FreshGameStart.ResolveStartZone("", true, true, manager, BiomeType.Spread, 1));
            Assert.AreNotEqual(FreshGameStart.ResolveStartZone("", true, true, manager, BiomeType.Spread, 1), FreshGameStart.ResolveStartZone("", true, true, manager), "the cave and the surface are different zones");
            Assert.AreEqual("", FreshGameStart.ResolveStartZone("", true, false, manager, BiomeType.Spread, 1), "a depth without a wild-start request changes nothing");
        }

        [Test] public void TheWildDepthRequestIsOneShotAndTheRodScenarioGivesTheRod()
        {
            var handle = typeof(ScenarioRunner).GetMethod("HandleAfterBootstrap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var oldLast = ScenarioRunner.LastLaunched; int oldDepth = ScenarioRunner.PendingWildDepth; var oldFlag = ScenarioRunner.PendingWildStart; var oldType = ScenarioRunner.PendingScenario; Pack.MaxWeight = 400;
            try
            {
                ScenarioRunner.PendingScenario = typeof(SoundingRodShowcase); ScenarioRunner.PendingWildStart = true; ScenarioRunner.PendingWildDepth = 1;
                handle.Invoke(null, new object[] { Zone, Factory, Actor, new TurnManager() });
                Assert.AreEqual(0, ScenarioRunner.PendingWildDepth, "a later scenario starts on the surface unless it asks otherwise");
                foreach (var (blueprint, count) in SoundingRodShowcase.Loadout) Assert.GreaterOrEqual(Pack.Objects.Count(o => o.BlueprintName == blueprint), count, blueprint);
            }
            finally { ScenarioRunner.PendingWildDepth = oldDepth; ScenarioRunner.PendingWildStart = oldFlag; ScenarioRunner.PendingScenario = oldType; ScenarioRunner.LastLaunched = oldLast; }
        }

        // ── a start that did not happen as asked says so, and a cave start is not turned into a field ──

        [Test] public void AFreshCaveStartKeepsItsFloorSoNoFarmPlotIsSeededUnderground_AndTheSurfaceStillGetsOne_Counter()
        {
            var manager = OverworldZoneManager.CreateDetached(Factory, 64, true); string id = FreshGameStart.WildStartZoneId(manager, BiomeType.Spread, 1); var cave = manager.GetZone(id);
            Assert.False(FreshGameStart.WantsFarmPlotAt(cave), "the seeder would turn the player's own cave floor to grass, and the rod would then refuse it");
            Assert.True(FreshGameStart.WantsFarmPlotAt(manager.GetZone(WorldMap.ToZoneID(WorldMap.FromZoneID(id).x, WorldMap.FromZoneID(id).y, 0))), "an ordinary surface start is unchanged");
            Assert.False(FreshGameStart.WantsFarmPlotAt(null)); Assert.True(FreshGameStart.WantsFarmPlotAt(new Zone("SomeTestZone")), "a zone that is not a world depth keeps the old behaviour");
        }

        [Test] public void TheRodScenarioSaysSoWhenTheStartWasNotUnderground_AndStaysQuietWhenItWas_Counter()
        {
            Pack.MaxWeight = 400; var surface = Context(out _); MessageLog.Clear(); new SoundingRodShowcase().Apply(surface);
            StringAssert.Contains("did not start you underground", string.Join("|", MessageLog.GetRecent(8)));
            var caveZone = new Zone("Overworld.10.10.1"); Zone.RemoveEntity(Actor); Assert.True(caveZone.AddEntity(Actor, 10, 10)); Zone = caveZone;
            MessageLog.Clear(); new SoundingRodShowcase().Apply(new ScenarioContext(caveZone, Factory, Actor, new TurnManager()));
            StringAssert.DoesNotContain("did not start you underground", string.Join("|", MessageLog.GetRecent(8)));
        }

        [Test] public void TheAwningScenarioSaysSoWhenTheStartWasNotInTheBeating_AndStaysQuietWhenItWas_Counter()
        {
            Pack.MaxWeight = 400; var spread = Context(out _); MessageLog.Clear(); new CampAwningShowcase().Apply(spread);
            StringAssert.Contains("did not start you in the Beating", string.Join("|", MessageLog.GetRecent(8)));
            var beating = new Zone("Overworld.16.16.0"); Zone.RemoveEntity(Actor); Assert.True(beating.AddEntity(Actor, 10, 10)); Zone = beating;
            MessageLog.Clear(); new CampAwningShowcase().Apply(new ScenarioContext(beating, Factory, Actor, new TurnManager()));
            StringAssert.DoesNotContain("did not start you in the Beating", string.Join("|", MessageLog.GetRecent(8)));
        }

        // ── the newer machines' fixture ──

        [Test] public void EveryMachinesLoadoutBlueprintExistsAndTheLoadoutIsCarriable_AndItGivesWhatEachMachineNeeds()
        {
            foreach (var (blueprint, count) in CampMachinesShowcase.Loadout) { Assert.NotNull(Factory.CreateEntity(blueprint), blueprint); Assert.Greater(count, 0, blueprint); }
            int weight = CampMachinesShowcase.Loadout.Sum(l => (Factory.CreateEntity(l.Blueprint).GetPart<PhysicsPart>()?.Weight ?? 0) * l.Count); Assert.LessOrEqual(weight, 240, "Strength 16 carries 240 before it is overburdened (the soft limit; the hard pack cap is pinned by TheMachinesScenarioGivesItsWholeLoadoutToAPlayerWhoHoldsTheShippedPackCap)");
            bool Has(string bp, int n = 1) => CampMachinesShowcase.Loadout.Any(l => l.Blueprint == bp && l.Count >= n);
            Assert.True(Has("CampTrough") && Has("LeatherArmor") && Has("ColdwardCloak") && Has("FireClay", ProofingRules.PermanentClay), "the lasting proofing");
            foreach (var element in ProofingRegistry.All) Assert.True(CampMachinesShowcase.Loadout.Any(l => ProofingRegistry.ElementsOf(Factory.CreateEntity(l.Blueprint)).Contains(element)), "a reagent for " + element.Id);
            Assert.GreaterOrEqual(CampMachinesShowcase.Loadout.Single(l => l.Blueprint == "FrostLichen").Count * 2, ProofingRules.PermanentUnits + 1, "enough cold to proof a garment for good and still coat another (cold:2 a lichen)"); Assert.True(Has("SoundingRod"), "sounding");
            Assert.True(Has("CampHearth") && Has("CampStill") && Has("LiquidFlask", CampMachinesShowcase.MireFlasks + 3) && Has("SalvagedTimber", 4) && Has("FlintStriker"), "baking clay");
            var data = PlaceSiteCatalog.Get("Cinderhold", "Pump") ?? Load(); foreach (var m in data.Lasting.Materials) Assert.True(Has(m.Blueprint, m.Count), "the pump's " + m.Blueprint);
            Assert.AreEqual(0, CampMachinesShowcase.MireUnits % 1); Assert.GreaterOrEqual(CampMachinesShowcase.MireUnits, LiquidSettlingPart.ClayRatio, "each flask can bake a clay");
        }

        static PlaceSiteData Load() { PlaceSiteCatalog.LoadFromJson(System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath, "Resources/Content/Data/PlaceSites.json"))); return PlaceSiteCatalog.Get("Cinderhold", "Pump"); }

        [Test] public void TheMachinesScenarioFillsBothFlasksWithMireAndGivesEverythingItNames()
        {
            Pack.MaxWeight = 400; var ctx = Context(out _); new CampMachinesShowcase().Apply(ctx);
            foreach (var (blueprint, count) in CampMachinesShowcase.Loadout) Assert.GreaterOrEqual(Pack.Objects.Where(o => o.BlueprintName == blueprint).Sum(o => o.GetPart<StackerPart>()?.StackCount ?? 1), count, blueprint);
            var flasks = Pack.Objects.Where(o => o.BlueprintName == "LiquidFlask").Select(o => o.GetPart<LiquidVesselPart>()).ToArray(); Assert.AreEqual(5, flasks.Length); Assert.True(flasks.Take(CampMachinesShowcase.MireFlasks).All(f => f.LiquidId == CampMachinesShowcase.MireLiquid && f.Volume == CampMachinesShowcase.MireUnits)); Assert.True(flasks.Skip(CampMachinesShowcase.MireFlasks).All(f => f.Volume == 0 && f.LiquidId == ""), "the other three are empty, for proofing");
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

        // ── the pack cap the fixtures are actually played at ──
        // Every test above raises the cap first (400, or the world fixture's 200), and the weight pins measure the soft Strength limit, so the shipped HARD cap never showed: the real player's pack holds 150,
        // and a loadout past it arrived partly with only a console warning (found in the first hand-play of the machines fixture, 2026-11-21).

        int ShippedCap() { int cap = Factory.CreateEntity("Player").GetPart<InventoryPart>().MaxWeight; Assert.GreaterOrEqual(cap, 0, "the shipped player has a hard carry cap"); return cap; }
        int Held(string blueprint) => Pack.Objects.Where(o => o.BlueprintName == blueprint).Sum(o => o.GetPart<StackerPart>()?.StackCount ?? 1);

        [Test] public void TheMachinesScenarioGivesItsWholeLoadoutToAPlayerWhoHoldsTheShippedPackCap()
        {
            int shipped = ShippedCap();
            Assert.Greater(ScenarioLoadout.WeightOf(Factory, CampMachinesShowcase.Loadout), shipped, "counter: this loadout outweighs the shipped cap, so a pack that is given no room drops its later items");
            Pack.MaxWeight = shipped; MessageLog.Clear(); new CampMachinesShowcase().Apply(Context(out _));
            foreach (var (blueprint, count) in CampMachinesShowcase.Loadout) Assert.GreaterOrEqual(Held(blueprint), count, blueprint);
            StringAssert.DoesNotContain("did not receive", string.Join("|", MessageLog.GetRecent(40)), "and nothing is reported missing");
        }

        [Test] public void TheRoomTheMachinesScenarioMakesIsTheCapacityTheSidebarShowsWithRoomToSpare()
        {
            Pack.MaxWeight = ShippedCap(); new CampMachinesShowcase().Apply(Context(out _));
            Assert.GreaterOrEqual(Pack.MaxWeight, Pack.GetMaxCarryWeight(), "the hard cap is lifted to the capacity the sidebar shows");
            Assert.GreaterOrEqual(Pack.MaxWeight - Pack.GetCarriedWeight(), 40, "and room is left above the loadout for what the steps make: clay, flakes, a fish");
        }

        [Test] public void TheMachinesScenarioNeverTightensABiggerCap_AndLeavesAnUncappedPackUncapped()
        {
            Pack.MaxWeight = 5000; new CampMachinesShowcase().Apply(Context(out _)); Assert.AreEqual(5000, Pack.MaxWeight, "a bigger cap is not lowered");
            foreach (var e in Pack.Objects.ToArray()) Pack.RemoveObject(e);
            Pack.MaxWeight = -1; new CampMachinesShowcase().Apply(Context(out _)); Assert.AreEqual(-1, Pack.MaxWeight, "no hard cap stays none");
            Assert.GreaterOrEqual(Held("SalvagedTimber"), 6, "and the loadout arrived whole");
        }

        [Test] public void EveryCampFixtureGivesItsWholeLoadoutAtTheShippedPackCap()
        {
            int shipped = ShippedCap();
            var fixtures = new (IScenario Scenario, (string Blueprint, int Count)[] Loadout)[]
            {
                (new CampKitsWildShowcase(), CampKitsShowcase.Loadout), (new CampKitsWildNightShowcase(), CampKitsShowcase.Loadout), (new CampAwningShowcase(), CampAwningShowcase.Loadout),
                (new SoundingRodShowcase(), SoundingRodShowcase.Loadout), (new ConcordCarriageShowcase(), ConcordCarriageShowcase.Loadout), (new CampMachinesShowcase(), CampMachinesShowcase.Loadout),
            };
            foreach (var (scenario, loadout) in fixtures)
            {
                foreach (var e in Pack.Objects.ToArray()) Pack.RemoveObject(e);
                foreach (var e in Zone.GetReadOnlyEntities().Where(e => e.BlueprintName == CampKitsShowcase.FireBlueprint).ToArray()) Zone.RemoveEntity(e);
                Pack.MaxWeight = shipped; scenario.Apply(Context(out _));
                foreach (var (blueprint, count) in loadout) Assert.GreaterOrEqual(Held(blueprint), count, scenario.GetType().Name + " gives " + blueprint + " at a pack cap of " + shipped);
            }
        }

        [Test] public void AFixtureLoadoutThatDoesNotArriveIsSaidInTheMessageLog_AndOneThatDoesIsNot()
        {
            MessageLog.Clear(); var ctx = Context(out _);
            Assert.IsEmpty(ScenarioLoadout.Give(ctx, new[] { ("Dagger", 1) }, "[T]"));
            StringAssert.DoesNotContain("[T]", string.Join("|", MessageLog.GetRecent(8)), "counter: a loadout that all arrived says nothing");
            LogAssert.Expect(LogType.Error, "EntityFactory: unknown blueprint 'NoSuchLoadoutBlueprint'");
            var missing = ScenarioLoadout.Give(ctx, new[] { ("NoSuchLoadoutBlueprint", 2), ("Dagger", 1) }, "[T]");
            Assert.AreEqual(1, missing.Count); StringAssert.Contains("NoSuchLoadoutBlueprint (0 of 2)", missing[0]);
            string log = string.Join("|", MessageLog.GetRecent(8)); StringAssert.Contains("[T]", log); StringAssert.Contains("NoSuchLoadoutBlueprint", log);
            Assert.AreEqual(2, Held("Dagger"), "the other items still arrive, and a dagger already carried does not hide a shortfall in the count");
        }

        [Test] public void ALoadoutHeavierThanTheSidebarCapacityStillArrivesWhole_AndTheCapRisesToHoldIt()
        {
            var ctx = Context(out _); Pack.MaxWeight = ShippedCap(); MessageLog.Clear();
            int soft = Pack.GetMaxCarryWeight(), n = soft + 60;   // timber weighs one a piece: more than Strength alone carries, so the lift must follow the loadout and not stop at the sidebar's figure
            Assert.Greater(n + Pack.GetCarriedWeight(), soft, "counter: this really is past the soft capacity");
            Assert.IsEmpty(ScenarioLoadout.Give(ctx, new[] { ("SalvagedTimber", n) }, "[T]"), "nothing is reported missing");
            Assert.GreaterOrEqual(Held("SalvagedTimber"), n, "and every one arrived");
            Assert.GreaterOrEqual(Pack.MaxWeight, Pack.GetCarriedWeight(), "the hard cap rose to what is now carried");
        }

        [Test] public void AnAlreadyCarriedItemCannotHideAFailedFixtureGrant()
        {
            Carry("Dagger");
            var blueprint = Factory.Blueprints["Dagger"];
            Factory.Blueprints.Remove("Dagger");
            try
            {
                LogAssert.Expect(LogType.Error, "EntityFactory: unknown blueprint 'Dagger'");
                var missing = ScenarioLoadout.Give(Context(out _), new[] { ("Dagger", 1) }, "[T]");
                CollectionAssert.AreEqual(new[] { "Dagger (0 of 1)" }, missing);
                Assert.AreEqual(1, Held("Dagger"), "the old dagger remains, but no new one arrived");
            }
            finally { Factory.Blueprints["Dagger"] = blueprint; }
        }

        [Test] public void RepeatedMissingBlueprintRowsReportTheCombinedRequestedCount()
        {
            LogAssert.Expect(LogType.Error, "EntityFactory: unknown blueprint 'NoSuchLoadoutBlueprint'");
            LogAssert.Expect(LogType.Error, "EntityFactory: unknown blueprint 'NoSuchLoadoutBlueprint'");
            var missing = ScenarioLoadout.Give(Context(out _), new[] { ("NoSuchLoadoutBlueprint", 2), ("NoSuchLoadoutBlueprint", 3) }, "[T]");
            CollectionAssert.AreEqual(new[] { "NoSuchLoadoutBlueprint (0 of 5)" }, missing);
        }

        [Test] public void NothingToGiveChangesNothing_NoItemsNoCapLiftNoMessage_Boundary()
        {
            MessageLog.Clear(); var ctx = Context(out _); Pack.MaxWeight = ShippedCap(); int items = Pack.Objects.Count, cap = Pack.MaxWeight;
            Assert.Less(cap, Pack.GetMaxCarryWeight(), "counter: the shipped cap sits below the sidebar capacity, so an unconditional lift would be visible");
            Assert.IsEmpty(ScenarioLoadout.Give(ctx, null, "[T]"));
            Assert.IsEmpty(ScenarioLoadout.Give(ctx, new (string, int)[0], "[T]"));
            Assert.IsEmpty(ScenarioLoadout.Give(ctx, new[] { ("Dagger", 0), ("Dagger", -3) }, "[T]"), "zero and negative counts give nothing");
            Assert.AreEqual(items, Pack.Objects.Count, "no items arrived"); Assert.AreEqual(cap, Pack.MaxWeight, "and the cap was not lifted for nothing");
            StringAssert.DoesNotContain("[T]", string.Join("|", MessageLog.GetRecent(8)), "and nothing was said");
            Assert.AreEqual(0, ScenarioLoadout.WeightOf(null, CampMachinesShowcase.Loadout), "no factory weighs nothing"); Assert.AreEqual(0, ScenarioLoadout.WeightOf(Factory, null), "no loadout weighs nothing");
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
