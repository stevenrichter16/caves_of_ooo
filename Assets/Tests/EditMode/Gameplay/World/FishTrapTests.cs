using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The camp fish trap (Docs/MACHINE-INVENTED-USES.md 3n): a kit set down on the shore of a fishable pond (a connected stretch of cascade-basin water), which catches a fish every half day up to four,
    /// drawing from a place-bound supply the pond's own anchor holds and every trap on that water shares. Time is reconciled on use, never by a timer; reading it changes nothing.
    /// </summary>
    public sealed class FishTrapTests : FiftyWorldFixture
    {
        OverworldZoneManager manager; string wildId; Zone wild; const int AX = 22, AY = 11;
        const int PondX0 = 24, PondY0 = 9, PondX1 = 29, PondY1 = 14;   // 6 x 6 = 36 cells of water

        [SetUp] public void SetupTrap()
        {
            PackableFixtureRegistry.ResetForTests(); Diag.ResetAll(); MessageLog.Clear(); Clock = new TurnManager();
            manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            wildId = Enumerable.Range(0, WorldMap.Width * WorldMap.Height).Select(i => WorldMap.ToZoneID(i % WorldMap.Width, i / WorldMap.Width, 0))
                .First(id => HeldPlaceZones.Classify(manager, id) == HeldPlaceZoneClass.WildSurface);
            wild = new Zone(wildId);
            for (int x = 0; x < Zone.Width; x++) for (int y = 0; y < Zone.Height; y++) Assert.True(wild.AddEntity(Factory.CreateEntity("Grass"), x, y));
            Zone.RemoveEntity(Actor); Zone = wild; Assert.True(Zone.AddEntity(Actor, AX, AY));
            manager.ReplaceLoadedState(new Dictionary<string, Zone> { { wildId, wild } }, wildId, new Dictionary<string, List<ZoneConnection>>());
            SettlementRuntime.ActiveZone = wild; Pack.MaxWeight = 400;
            for (int x = PondX0; x <= PondX1; x++) for (int y = PondY0; y <= PondY1; y++) Assert.True(wild.AddEntity(Factory.CreateEntity("SprayPool"), x, y));
        }
        [TearDown] public void TearDownTrap() { PackableFixtureRegistry.ResetForTests(); FiftyWorldCreationProbe.Created = null; }

        // ── helpers ──

        PlacementSpec Shore => new PlacementSpec { Solid = false, GroundClass = PlacementCheck.ShoreGround };
        PlacementResult At(int x, int y, PlacementSpec spec) => PlacementCheck.Evaluate(manager, wild, x, y, spec);
        InventoryAction[] SetDownRows(Entity kit) => Actions(kit).Where(a => a.Name == "SetDownFixture" && !a.IsDisabled).ToArray();
        static string Dir(InventoryAction a) => a.Display.StartsWith("set down ", StringComparison.Ordinal) ? a.Display.Substring(9).Split(' ')[0] : "";
        /// <summary>Walk the actor to (x, y), carry a trap, and set it down in the named direction through the real verb.</summary>
        Entity SetTrap(int ax, int ay, string direction)
        {
            wild.RemoveEntity(Actor); Assert.True(wild.AddEntity(Actor, ax, ay));
            var kit = Carry("CampFishTrap"); var row = SetDownRows(kit).FirstOrDefault(r => Dir(r) == direction);
            Assert.NotNull(row, "a set-down row " + direction + " at (" + ax + "," + ay + "): " + string.Join(" / ", Actions(kit).Select(a => a.Display + (a.IsDisabled ? " [" + a.DisabledCode + "]" : ""))));
            Assert.True(Act(kit, row.Command), string.Join("|", MessageLog.GetRecent(3))); return kit;
        }
        FishTrapPart Part(Entity trap) => trap.GetPart<FishTrapPart>();
        void Days(double d) => Clock.AdvanceClock((int)(d * WorldClock.DayLengthTicks));
        /// <summary>Stand beside the trap (the row only exists within one cell) and return its take row.</summary>
        InventoryAction TakeRow(Entity trap)
        {
            var c = wild.GetEntityCell(trap); var me = wild.GetEntityCell(Actor);
            if (Math.Max(Math.Abs(c.X - me.X), Math.Abs(c.Y - me.Y)) > 1) { wild.RemoveEntity(Actor); Assert.True(wild.AddEntity(Actor, c.X - 1, c.Y)); }
            return World(trap).Single(a => a.Name == "TakeTrapFish");
        }
        List<InventoryAction> World(Entity fixture) => WorldInteractionSystem.GatherActions(fixture, Actor);
        int Perch() => Pack.Objects.Where(o => o.BlueprintName == "LakePerch").Sum(o => o.GetPart<StackerPart>()?.StackCount ?? 1);
        string Read(Entity e) { var l = new List<string>(); StateReader.Collect(e, true, wild, wild.GetEntityCell(e), Actor, l); return string.Join(" ", l); }
        FishStockPart Stock() { FishPond.TryFind(wild, PondX0 - 1, PondY0 + 1, out var anchor); return anchor.GetPart<FishStockPart>(); }

        // ── placement ──

        [Test] public void ShoreGroundNeedsAFishablePondBeside_AndOtherKitsStillRefuseTheWaterEdge_Counter()
        {
            Assert.True(At(PondX0 - 1, PondY0 + 2, Shore).Ok, string.Join(",", At(PondX0 - 1, PondY0 + 2, Shore).Reasons));
            StringAssert.Contains("class-specific", string.Join(",", At(35, 11, Shore).Reasons), "open ground far from the water is not shore");
            var ordinary = At(PondX0 - 1, PondY0 + 2, new PlacementSpec { Solid = false, GroundClass = PlacementCheck.OpenGround });
            Assert.False(ordinary.Ok, "counter: an ordinary kit is still refused beside the water (the hazard rule is unchanged)");
        }

        [Test] public void APuddleOrABogOrFrozenWaterIsNotAPond_Counter()
        {
            for (int x = 40; x <= 45; x++) for (int y = 9; y <= 14; y++) Assert.True(wild.AddEntity(Factory.CreateEntity("WaterPuddle"), x, y));
            Assert.False(At(39, 11, Shore).Ok, "a 36-cell puddle is not fishable: its blueprint does not opt in");
            for (int x = 50; x <= 55; x++) for (int y = 9; y <= 14; y++) Assert.True(wild.AddEntity(Factory.CreateEntity("PeatBog"), x, y));
            Assert.False(At(49, 11, Shore).Ok, "a bog is not a pond");
            for (int x = 60; x <= 62; x++) for (int y = 9; y <= 11; y++) Assert.True(wild.AddEntity(Factory.CreateEntity("SprayPool"), x, y));
            Assert.False(At(59, 10, Shore).Ok, "nine cells of cascade water is a pool, not a pond (twelve are needed)");
            Assert.True(At(PondX0 - 1, PondY0 + 2, Shore).Ok, "counter: the real pond still counts");
        }

        [Test] public void TheSetDownVerbOffersOnlyShoreCellsAndTheTrapStandsThere()
        {
            wild.RemoveEntity(Actor); Assert.True(wild.AddEntity(Actor, PondX0 - 2, PondY0 + 2)); var kit = Carry("CampFishTrap");
            var dirs = SetDownRows(kit).Select(Dir).ToList(); CollectionAssert.Contains(dirs, "east", "one step east is the shore"); CollectionAssert.DoesNotContain(dirs, "west", "one step west is open ground with no water beside it");
            var trap = SetTrap(PondX0 - 2, PondY0 + 2, "east"); var cell = wild.GetEntityCell(trap); Assert.AreEqual(PondX0 - 1, cell.X);
            Assert.True(HeldFixtureActions.IsPlayerHeld(trap));
        }

        [Test] public void TheKitIsRegisteredStockedAndAShippedBlueprint()
        {
            var def = PackableFixtureRegistry.Get("camp-fishtrap"); Assert.NotNull(def); Assert.AreEqual(PlacementCheck.ShoreGround, def.GroundClass); Assert.False(def.Solid);
            var made = Factory.CreateEntity("CampFishTrap"); Assert.NotNull(made.GetPart<FishTrapPart>()); Assert.True(made.GetPart<PhysicsPart>().Takeable); Assert.AreEqual("camp-fishtrap", made.GetPart<PackableFixturePart>().Kind);
            StringAssert.Contains("TinkerStock", File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/Loot/LootTables.json")));
            Assert.True(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/Loot/LootTables.json")).Contains("\"CampFishTrap\""), "the Tinker stocks it");
            Assert.NotNull(Factory.CreateEntity(FishTrapPart.FishBlueprint), "the fish is a real blueprint"); Assert.True(Factory.CreateEntity("SprayPool").HasTag(FishPond.FishTag), "the cascade basins opt in");
            Assert.False(Factory.CreateEntity("WaterPuddle").HasTag(FishPond.FishTag), "counter: puddles do not");
        }

        // ── catching, reading and taking ──

        [Test] public void SharedPondReview_CreationCallbackCannotTakeTheSameFishTwice()
        {
            var trap = SetTrap(PondX0 - 2, PondY0 + 2, "east");
            Part(trap).Held = 1;
            bool nested = false;
            Factory.RegisterPartType<FiftyWorldCreationProbe>();
            Factory.Blueprints["LakePerch"].Parts["FiftyWorldCreationProbe"] = new Dictionary<string, string>();
            FiftyWorldCreationProbe.Created = _ =>
            {
                FiftyWorldCreationProbe.Created = null;
                var e = GameEvent.New("InventoryAction");
                e.SetParameter("Actor", Actor); e.SetParameter("Zone", wild);
                e.SetParameter("Command", FishTrapPart.Command);
                nested = !trap.FireEventAndRelease(e);
            };
            Assert.True(Act(trap, FishTrapPart.Command));
            Assert.AreEqual(1, Perch(), "One held fish cannot become two through creation reentry.");
            Assert.False(nested, "The trap must be claimed before creating its output.");
            Assert.AreEqual(0, Part(trap).Held);
        }

        [Test] public void SharedPondReview_OppositeShoresOfALargePondKeepOneAnchor()
        {
            LargePond();
            Assert.True(FishPond.TryFind(wild, 3, 11, out var west));
            Assert.True(FishPond.TryFind(wild, 74, 11, out var east));
            Assert.AreSame(west, east, "All 1260 connected cells share one pond, not one budget per traversal origin.");
            var stock = FishStockPart.For(wild, west, WorldClock.CurrentTick); stock.Available = 1;
            Assert.AreSame(stock, FishStockPart.For(wild, east, WorldClock.CurrentTick));
            Assert.AreEqual(1, east.GetPart<FishStockPart>().Available);
        }

        void LargePond()
        {
            foreach (var water in wild.GetReadOnlyEntities().Where(e => e.HasPart<LiquidPoolPart>()).ToArray())
                Assert.True(wild.RemoveEntity(water));
            for (int x = 4; x <= 73; x++) for (int y = 3; y <= 20; y++)
                Assert.True(wild.AddEntity(Factory.CreateEntity("SprayPool"), x, y));
        }

        [Test] public void SharedPondLegacy_ExhaustedFarAnchorSurvivesSavedGraph()
        {
            LargePond();
            var far = FishPond.StructuralAt(wild, 73, 3);
            far.AddPart(new FishStockPart { Capacity = 6, Available = 0, LastTick = WorldClock.CurrentTick });
            var restored = PartRoundTripHelper.RoundTripEntityViaTokenGraph(far);
            Assert.True(wild.RemoveEntity(far)); Assert.True(wild.AddEntity(restored, 73, 3));
            Assert.True(FishPond.TryFind(wild, 3, 11, out var west));
            Assert.True(FishPond.TryFind(wild, 74, 11, out var east));
            Assert.AreSame(restored, west, "Expanding a saved search must not abandon its exhausted stock.");
            Assert.AreSame(restored, east);
            Assert.AreEqual(0, FishStockPart.For(wild, west, WorldClock.CurrentTick).Available);
        }

        [Test] public void SharedPondLegacy_MultipleSavedBudgetsSelectOneWithoutSumming()
        {
            LargePond();
            var first = FishPond.StructuralAt(wild, 20, 4);
            var other = FishPond.StructuralAt(wild, 70, 8);
            first.AddPart(new FishStockPart { Capacity = 4, Available = 1, LastTick = WorldClock.CurrentTick });
            other.AddPart(new FishStockPart { Capacity = 8, Available = 8, LastTick = WorldClock.CurrentTick });
            Assert.True(FishPond.TryFind(wild, 3, 11, out var west));
            Assert.True(FishPond.TryFind(wild, 74, 11, out var east));
            Assert.AreSame(first, west); Assert.AreSame(first, east);
            Assert.AreEqual(1, FishStockPart.For(wild, west, WorldClock.CurrentTick).Available);
            Assert.AreEqual(8, other.GetPart<FishStockPart>().Available, "Do not merge, top up or rewrite another saved budget.");
        }

        [Test] public void ACarriedTrapIsAKitThatOffersNothingAndSaysWhatItIs_Counter()
        {
            var kit = Carry("CampFishTrap"); Assert.IsEmpty(Actions(kit).Where(a => a.Name == "TakeTrapFish")); StringAssert.Contains("set down", Guide(kit));
        }

        string Guide(Entity e, Entity who = null) { var g = new System.Collections.Generic.List<string>(); UseGuides.Collect(e, null, null, who ?? Actor, g); return string.Join(" ", g); }


        [Test] public void ItCatchesOneFishEveryHalfDayUpToFourAndReadingChangesNothing()
        {
            var trap = SetTrap(PondX0 - 2, PondY0 + 2, "east"); var part = Part(trap);
            Assert.True(TakeRow(trap).IsDisabled); Assert.AreEqual("empty", TakeRow(trap).DisabledCode); StringAssert.Contains("half day", TakeRow(trap).DisabledReason);
            Days(0.5); Assert.AreEqual(1, part.HeldNow(wild, WorldClock.CurrentTick)); Assert.AreEqual(0, part.Held, "reading never changes the trap"); StringAssert.Contains("take the fish (1)", TakeRow(trap).Display);
            Days(1.5); Assert.AreEqual(4, part.HeldNow(wild, WorldClock.CurrentTick)); Days(10); Assert.AreEqual(4, part.HeldNow(wild, WorldClock.CurrentTick), "four at most");
            Assert.AreEqual(0, part.Held); Assert.True(FishPond.TryFind(wild, PondX0 - 1, PondY0 + 2, out var anchor)); Assert.IsNull(anchor.GetPart<FishStockPart>(), "reading did not even make the pond's stock");
        }

        [Test] public void TakingTheFishGivesRealPerchEmptiesTheTrapAndTheTimeKeepsRunning()
        {
            var trap = SetTrap(PondX0 - 2, PondY0 + 2, "east"); var part = Part(trap); Days(2.1);
            Assert.True(Act(trap, TakeRow(trap).Command), string.Join("|", MessageLog.GetRecent(3)));
            Assert.AreEqual(4, Perch()); Assert.AreEqual(0, part.Held); StringAssert.Contains("take 4 fish", string.Join(" ", MessageLog.GetRecent(3)).Replace("took", "take"));
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter { Category = "furniture", Kind = "FishTaken", Limit = 5 }).Records.Count);
            Days(0.5); Assert.AreEqual(1, part.HeldNow(wild, WorldClock.CurrentTick), "it fishes again");
        }

        [Test] public void TwoTrapsShareOnePondSupplyAndItRefillsOneFishADay()
        {
            var a = SetTrap(PondX0 - 2, PondY0 + 2, "east"); var b = SetTrap(PondX0 - 2, PondY0 + 4, "east");
            Days(3); Assert.True(Act(a, TakeRow(a).Command)); var stock = Stock(); Assert.NotNull(stock, "the pond's stock exists once someone fishes it");
            Assert.That(stock.Capacity, Is.InRange(FishStockPart.MinCapacity, FishStockPart.MaxCapacity)); Assert.AreEqual(stock.Capacity - 4, stock.Available);
            stock.Available = 1;   // the pond is nearly spent
            Assert.True(Act(b, TakeRow(b).Command)); Assert.AreEqual(0, Stock().Available, "the second trap took what was left"); Assert.AreEqual(5, Perch(), "four from the first, one from the second, and none made from nothing");
            Days(0.6); Assert.AreEqual(0, Part(a).HeldNow(wild, WorldClock.CurrentTick), "a fished-out pond gives a trap nothing yet");
            Days(1.0); Assert.AreEqual(1, Stock().AvailableAt(WorldClock.CurrentTick), "one fish has come back a day later");
            Part(a).Reconcile(wild, WorldClock.CurrentTick); Part(b).Reconcile(wild, WorldClock.CurrentTick);
            Assert.AreEqual(1, Part(a).Held + Part(b).Held, "two traps, one fish: they share it"); Assert.AreEqual(0, Stock().Available);
        }

        [Test] public void APondNeverGivesMoreThanItsCapacityPlusWhatItRefills()
        {
            var traps = new[] { SetTrap(PondX0 - 2, PondY0 + 1, "east"), SetTrap(PondX0 - 2, PondY0 + 3, "east"), SetTrap(PondX0 - 2, PondY0 + 5, "east") };
            Days(5); foreach (var t in traps) { var row = TakeRow(t); if (!row.IsDisabled) Assert.True(Act(t, row.Command)); } var stock = Stock(); Assert.NotNull(stock);
            Assert.LessOrEqual(Perch(), stock.Capacity, "three traps and five days still cannot beat the supply"); Assert.AreEqual(stock.Capacity - Perch(), stock.Available);
        }

        [Test] public void StockCapacityIsStableForAPondAndDifferentPondsAreIndependent()
        {
            var a = SetTrap(PondX0 - 2, PondY0 + 2, "east"); Days(1); Act(a, TakeRow(a).Command); int cap = Stock().Capacity;
            Assert.AreEqual(cap, FishStockPart.For(wild, Stock().ParentEntity, WorldClock.CurrentTick).Capacity, "asking again gives the same stock"); Assert.That(cap, Is.InRange(4, 8));
            for (int x = 40; x <= 45; x++) for (int y = 9; y <= 14; y++) wild.AddEntity(Factory.CreateEntity("SprayPool"), x, y);
            FishPond.TryFind(wild, 39, 11, out var other); Assert.AreNotSame(Stock().ParentEntity, other, "a second pond has its own anchor"); Assert.IsNull(other.GetPart<FishStockPart>(), "and its own, untouched stock");
        }

        [Test] public void AnInterruptedTakeLeavesTheTrapAndThePondAsTheyWereAndYouCarryNoFish()
        {
            var trap = SetTrap(PondX0 - 2, PondY0 + 2, "east"); Days(2.1); string command = TakeRow(trap).Command; int last = Part(trap).LastTick, progress = Part(trap).Progress; FailAfter();
            Assert.False(Act(trap, command)); Assert.AreEqual(0, Perch()); Assert.AreEqual(0, Part(trap).Held, "nothing was committed to the trap"); Assert.AreEqual(last, Part(trap).LastTick); Assert.AreEqual(progress, Part(trap).Progress);
            var stock = Stock(); if (stock != null) Assert.AreEqual(stock.Capacity, stock.Available, "and the pond was not drawn on");
        }

        [Test] public void AFullPackTakesAsManyAsFitAndTheRestStayInTheTrap()
        {
            var trap = SetTrap(PondX0 - 2, PondY0 + 2, "east"); Days(2.1); var perch = Factory.CreateEntity("LakePerch"); int unit = HandlingService.GetWeight(perch);
            Pack.MaxWeight = Pack.GetCarriedWeight() + unit * 2;
            Assert.True(Act(trap, TakeRow(trap).Command)); Assert.AreEqual(2, Perch()); Assert.AreEqual(2, Part(trap).Held, "two stay in the trap"); StringAssert.Contains("pack is full", string.Join(" ", MessageLog.GetRecent(3)));
            Pack.MaxWeight = Pack.GetCarriedWeight(); Assert.False(Act(trap, TakeRow(trap).Command)); Assert.AreEqual(2, Part(trap).Held, "a full pack takes nothing and changes nothing");
        }

        [Test] public void IfTheWaterGoesTheTrapSaysSoAndCatchesNothing()
        {
            var trap = SetTrap(PondX0 - 2, PondY0 + 2, "east"); Days(2.1);
            foreach (var e in wild.GetReadOnlyEntities().Where(e => e.BlueprintName == "SprayPool").ToList()) wild.RemoveEntity(e);
            var row = TakeRow(trap); Assert.True(row.IsDisabled); Assert.AreEqual("no-pond", row.DisabledCode); StringAssert.Contains("not beside a pond", row.DisabledReason);
            Assert.AreEqual(0, Part(trap).HeldNow(wild, WorldClock.CurrentTick));
        }

        [Test] public void TakingTheTrapUpBringsItUpToDateFirst_ItKeepsItsFishAndFishesNoMoreUntilSetDownAgain()
        {
            var trap = SetTrap(PondX0 - 2, PondY0 + 2, "east"); Days(1.1); Assert.AreEqual(0, Part(trap).Held, "fixture: nothing has reconciled the trap yet");
            var take = World(trap).Single(a => a.Name == "TakeUpFixture"); Assert.True(Act(trap, take.Command)); Assert.True(Pack.Objects.Contains(trap));
            Assert.AreEqual(2, Part(trap).Held, "the two it caught while it stood were counted before the clock stopped"); Assert.True(FishPond.TryFind(wild, PondX0 - 1, PondY0 + 2, out var anchor)); Assert.AreEqual(FishStockPart.MaxCapacity >= 2 ? anchor.GetPart<FishStockPart>().Capacity - 2 : 0, anchor.GetPart<FishStockPart>().Available, "and drawn from the pond");
            Days(5); Assert.AreEqual(2, Part(trap).HeldNow(wild, WorldClock.CurrentTick), "carried, it catches nothing"); Assert.IsEmpty(Actions(trap).Where(a => a.Name == "TakeTrapFish")); StringAssert.Contains("holds 2 fish", Read(trap));
            wild.RemoveEntity(Actor); Assert.True(wild.AddEntity(Actor, PondX0 - 2, PondY0 + 2)); var row = SetDownRows(trap).First(r => Dir(r) == "east"); Assert.True(Act(trap, row.Command));
            Days(0.5); Assert.AreEqual(3, Part(trap).HeldNow(wild, WorldClock.CurrentTick), "set down again it fishes again, from where it left off");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FailedFullPackTakeUpPreservesTheTrapAndSharedStockUntilAValidRetry(bool existingStock)
        {
            var trap = SetTrap(PondX0 - 2, PondY0 + 2, "east");
            var part = Part(trap);
            var fixture = trap.GetPart<PackableFixturePart>();
            var cell = wild.GetEntityCell(trap);
            Assert.True(FishPond.TryFind(wild, cell.X, cell.Y, out var anchor));
            var stock = existingStock ? FishStockPart.For(wild, anchor, WorldClock.CurrentTick) : null;
            if (stock != null) stock.Available = 1;
            var stockBefore = stock == null ? default : (stock.Available, stock.LastTick, stock.Capacity);

            Days(0.2);
            Assert.AreEqual(0, part.Reconcile(wild, WorldClock.CurrentTick));
            Days(0.9);
            int tick = WorldClock.CurrentTick;
            int projected = part.HeldNow(wild, tick);
            int available = stock?.AvailableAt(tick) ?? 0;
            Assert.AreEqual(2, projected);
            var before = (part.Held, part.Progress, part.LastTick, fixture.SetTick);
            string command = World(trap).Single(row => row.Name == "TakeUpFixture").Command;
            int limit = Pack.MaxWeight;
            Pack.MaxWeight = Pack.GetCarriedWeight();

            Assert.False(Act(trap, command));
            Assert.AreSame(cell, wild.GetEntityCell(trap));
            Assert.True(HeldFixtureActions.IsPlayerHeld(trap));
            Assert.False(Pack.Objects.Contains(trap));
            Assert.AreEqual(tick, WorldClock.CurrentTick);
            Assert.AreEqual(before, (part.Held, part.Progress, part.LastTick, fixture.SetTick));
            Assert.AreSame(stock, anchor.GetPart<FishStockPart>());
            if (stock != null) Assert.AreEqual(stockBefore, (stock.Available, stock.LastTick, stock.Capacity));

            Pack.MaxWeight = limit;
            Assert.True(Act(trap, command));
            Assert.True(Pack.Objects.Contains(trap));
            Assert.AreEqual(projected, part.Held);
            Assert.AreEqual(tick, part.LastTick);
            var settled = anchor.GetPart<FishStockPart>();
            Assert.NotNull(settled);
            if (stock != null) Assert.AreSame(stock, settled);
            int left = (stock == null ? settled.Capacity : available) - projected;
            Assert.AreEqual(left, settled.Available);
            Assert.False(Act(trap, command), "a completed pickup cannot be paid twice");
            Assert.AreEqual(left, settled.Available);
            Assert.AreEqual(projected, part.Held);
        }

        [Test] public void AnInterruptedPickupRestoresBothTheLoadedTrapAndItsPondWhileBlockingACompetingCatch()
        {
            var trap = SetTrap(PondX0 - 2, PondY0 + 2, "east");
            var other = SetTrap(PondX0 - 2, PondY0 + 3, "east");
            var cell = wild.GetEntityCell(trap);
            var part = Part(trap);
            Assert.True(FishPond.TryFind(wild, cell.X, cell.Y, out var anchor));
            var stock = FishStockPart.For(wild, anchor, WorldClock.CurrentTick);
            stock.Capacity = 6;
            stock.Available = 4;
            Days(0.6);
            Assert.AreEqual(1, part.Reconcile(wild, WorldClock.CurrentTick));
            Days(0.9);
            int tick = WorldClock.CurrentTick;
            int expected = part.HeldNow(wild, tick);
            int expectedRemaining = stock.AvailableAt(tick) - (expected - part.Held);
            Assert.AreEqual(3, expected);
            Assert.Greater(expectedRemaining, 0, "the competing trap could still draw after the pickup debit");
            var stockBefore = (stock.Available, stock.LastTick, stock.Capacity);
            var fixture = trap.GetPart<PackableFixturePart>();
            var before = (part.Held, part.Progress, part.LastTick, fixture.SetTick);
            var otherBefore = (Part(other).Held, Part(other).Progress, Part(other).LastTick);
            var rival = Factory.CreateEntity("Player");
            Assert.True(wild.AddEntity(rival, PondX0 - 1, PondY0 + 4));
            var rivalPack = rival.GetPart<InventoryPart>();
            rivalPack.MaxWeight = 400;
            var competing = WorldInteractionSystem.GatherActions(other, rival).Single(row => row.Name == "TakeTrapFish");
            Assert.False(competing.IsDisabled);

            bool staged = false, supplierHadFish = false, rivalSucceeded = false;
            var interrupt = new LateFixturePickupProbe
            {
                Fixture = trap,
                Observe = () =>
                {
                    staged = wild.GetEntityCell(trap) == null && Pack.Objects.Contains(trap);
                    supplierHadFish = Part(other).HeldNow(wild, tick) > 0;
                    rivalSucceeded = InventorySystem.PerformAction(rival, other, competing.Command, wild);
                }
            };
            Actor.AddPart(interrupt);
            string command = World(trap).Single(row => row.Name == "TakeUpFixture").Command;

            Assert.False(Act(trap, command));
            Assert.True(staged && supplierHadFish, "the callback ran after provisional custody changed and a genuine alternative catch still existed");
            Assert.False(rivalSucceeded);
            Assert.IsFalse(rivalPack.Objects.Any(item => item.BlueprintName == "LakePerch"));
            Assert.AreSame(cell, wild.GetEntityCell(trap));
            Assert.True(HeldFixtureActions.IsPlayerHeld(trap));
            Assert.False(Pack.Objects.Contains(trap));
            Assert.AreEqual(before, (part.Held, part.Progress, part.LastTick, fixture.SetTick));
            Assert.AreEqual(otherBefore, (Part(other).Held, Part(other).Progress, Part(other).LastTick));
            Assert.AreSame(stock, anchor.GetPart<FishStockPart>());
            Assert.AreEqual(stockBefore, (stock.Available, stock.LastTick, stock.Capacity));

            Assert.True(Actor.RemovePart(interrupt));
            Assert.True(Act(trap, command));
            Assert.True(Pack.Objects.Contains(trap));
            Assert.AreEqual(expected, part.Held);
            Assert.AreEqual(expectedRemaining, stock.Available);
        }

        [Test] public void APartiallyAffordablePackUpPreservesBothTrapsAndTheirSharedPond()
        {
            var first = SetTrap(PondX0 - 2, PondY0 + 2, "east");
            var second = SetTrap(PondX0 - 2, PondY0 + 3, "east");
            var firstCell = wild.GetEntityCell(first);
            var secondCell = wild.GetEntityCell(second);
            Assert.True(FishPond.TryFind(wild, firstCell.X, firstCell.Y, out var anchor));
            Days(0.6);
            int now = WorldClock.CurrentTick;
            Assert.AreEqual(1, Part(first).HeldNow(wild, now));
            Assert.AreEqual(1, Part(second).HeldNow(wild, now));
            var firstBefore = (Part(first).Held, Part(first).Progress, Part(first).LastTick);
            var secondBefore = (Part(second).Held, Part(second).Progress, Part(second).LastTick);
            string command = World(second).Single(row => row.Name == "PackUpCamp").Command;
            int limit = Pack.MaxWeight;
            Pack.MaxWeight = Pack.GetCarriedWeight() + InventoryPart.GetItemWeight(first);

            Assert.False(Act(second, command));
            Assert.AreSame(firstCell, wild.GetEntityCell(first));
            Assert.AreSame(secondCell, wild.GetEntityCell(second));
            Assert.True(HeldFixtureActions.IsPlayerHeld(first));
            Assert.True(HeldFixtureActions.IsPlayerHeld(second));
            Assert.False(Pack.Objects.Contains(first) || Pack.Objects.Contains(second));
            Assert.AreEqual(firstBefore, (Part(first).Held, Part(first).Progress, Part(first).LastTick));
            Assert.AreEqual(secondBefore, (Part(second).Held, Part(second).Progress, Part(second).LastTick));
            Assert.IsNull(anchor.GetPart<FishStockPart>());

            Pack.MaxWeight = limit;
            Assert.True(Act(second, command));
            Assert.True(Pack.Objects.Contains(first) && Pack.Objects.Contains(second));
            Assert.AreEqual(1, Part(first).Held);
            Assert.AreEqual(1, Part(second).Held);
            var stock = anchor.GetPart<FishStockPart>();
            Assert.NotNull(stock);
            Assert.AreEqual(stock.Capacity - 2, stock.Available);
        }

        [Test] public void TheTrapAndItsPondRideTheSave()
        {
            var trap = SetTrap(PondX0 - 2, PondY0 + 2, "east"); Days(2.1); Act(trap, TakeRow(trap).Command); Days(1); Part(trap).Reconcile(wild, WorldClock.CurrentTick); var stock = Stock();
            var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(trap).GetPart<FishTrapPart>(); Assert.AreEqual(Part(trap).Held, copy.Held); Assert.AreEqual(Part(trap).LastTick, copy.LastTick); Assert.AreEqual(Part(trap).Progress, copy.Progress);
            var s2 = PartRoundTripHelper.RoundTripEntityViaTokenGraph(stock.ParentEntity).GetPart<FishStockPart>(); Assert.AreEqual(stock.Capacity, s2.Capacity); Assert.AreEqual(stock.Available, s2.Available); Assert.AreEqual(stock.LastTick, s2.LastTick);
        }

        [Test] public void TheCampNoteProjectsWhatTheTrapWouldHoldByNowWithoutChangingIt()
        {
            var trap = SetTrap(PondX0 - 2, PondY0 + 2, "east"); Assert.AreEqual("", HeldFixtureReader.Status(new[] { trap }), "an empty trap says nothing");
            Days(1.1); StringAssert.Contains("fish trap holds 2 by now", HeldFixtureReader.Status(new[] { trap })); Assert.AreEqual(0, Part(trap).Held, "reading the note changes nothing");
        }

        // ── the pond is the same pond (review fixes) ──

        [Test] public void DrawingWaterOrFreezingTheAnchorNeverMovesOrResetsThePondsStock()
        {
            var trap = SetTrap(PondX0 - 2, PondY0 + 2, "east"); Days(3); Assert.True(Act(trap, TakeRow(trap).Command)); var stock = Stock(); var anchor = stock.ParentEntity; int left = stock.Available;
            anchor.GetPart<LiquidPoolPart>().Volume = 0;   // flasks drew the anchor cell dry
            Assert.True(FishPond.TryFind(wild, PondX0 - 1, PondY0 + 2, out var again)); Assert.AreSame(anchor, again, "a dry anchor is still the anchor"); Assert.AreSame(stock, again.GetPart<FishStockPart>()); Assert.AreEqual(left, again.GetPart<FishStockPart>().Available);
            wild.TileState.WriteCoating(PondX0, PondY0, "ice", 100);   // and frost holds it
            Assert.True(FishPond.TryFind(wild, PondX0 - 1, PondY0 + 2, out var frozen)); Assert.AreSame(anchor, frozen, "a frozen anchor is still the anchor");
        }

        [Test] public void DryingABridgingCellDoesNotSplitOnePondIntoTwoStocks()
        {
            var trap = SetTrap(PondX0 - 2, PondY0 + 2, "east"); Days(3); Assert.True(Act(trap, TakeRow(trap).Command)); var anchor = Stock().ParentEntity;
            for (int y = PondY0; y <= PondY1; y++) wild.GetCell(PondX0 + 2, y).Objects.Where(o => o.HasPart<LiquidPoolPart>()).ToList().ForEach(o => o.GetPart<LiquidPoolPart>().Volume = 0);   // a dry column through the middle
            Assert.True(FishPond.TryFind(wild, PondX0 - 1, PondY0 + 2, out var a)); Assert.AreSame(anchor, a, "still one pond with one anchor and one stock");
            Assert.True(FishPond.TryFind(wild, PondX1 + 1, PondY0 + 2, out var b)); Assert.AreSame(anchor, b, "seen from the far shore too");
        }

        [Test] public void CascadeBasinsGeneratedBeforeTheTagExistedAreStillFishable_Counter()
        {
            foreach (var e in wild.GetReadOnlyEntities().Where(e => e.BlueprintName == "SprayPool")) e.Tags.Remove(FishPond.FishTag);
            Assert.True(At(PondX0 - 1, PondY0 + 2, Shore).Ok, "an older save's pool has no tag but is recognised by name");
            for (int x = 40; x <= 45; x++) for (int y = 9; y <= 14; y++) { var puddle = Factory.CreateEntity("WaterPuddle"); wild.AddEntity(puddle, x, y); }
            Assert.False(At(39, 11, Shore).Ok, "counter: untagged puddles are still not ponds");
        }

        [Test] public void IceAcrossThePondMakesItUnfishable_Counter()
        {
            for (int x = PondX0; x <= PondX1; x++) for (int y = PondY0; y <= PondY1; y++) wild.TileState.WriteCoating(x, y, "ice", 100);
            Assert.False(At(PondX0 - 1, PondY0 + 2, Shore).Ok, "frozen water cannot be fished through");
        }

        [Test] public void ATrapSeesTheBiggerOfTwoPondsAndNeverMergesThem()
        {
            // a 30-cell pond to the west and an 18-cell pond to the east, with the trap's own column (54) between them: two ponds, not one
            for (int x = 49; x <= 53; x++) for (int y = 9; y <= 14; y++) Assert.True(wild.AddEntity(Factory.CreateEntity("SprayPool"), x, y));
            for (int x = 55; x <= 57; x++) for (int y = 9; y <= 14; y++) Assert.True(wild.AddEntity(Factory.CreateEntity("SprayPool"), x, y));
            Assert.True(FishPond.TryFind(wild, 54, 11, out var anchor)); var at = wild.GetEntityCell(anchor);
            Assert.AreEqual(49, at.X, "the larger, western pond is the one fished"); Assert.AreEqual(9, at.Y);
            Assert.AreEqual(30, FishPond.Component(wild, 54, 11).Count, "the east pond's cells are not added to it");
            Assert.True(FishPond.TryFind(wild, 58, 11, out var east)); Assert.AreNotSame(anchor, east, "from the east shore it is a different pond with its own stock"); Assert.AreEqual(18, FishPond.Component(wild, 58, 11).Count);
        }

        [Test] public void TheMovePickerCannotPutTheTrapInWaterOrOffTheShore_Counter()
        {
            var trap = SetTrap(PondX0 - 2, PondY0 + 2, "east");
            Assert.False(PlacementCheck.Evaluate(manager, wild, PondX0, PondY0 + 2, Shore, trap).Ok, "not into the water");
            Assert.False(PlacementCheck.Evaluate(manager, wild, PondX0 - 3, PondY0 + 2, Shore, trap).Ok, "not onto ground with no pond beside it");
            Assert.True(PlacementCheck.Evaluate(manager, wild, PondX0 - 1, PondY0 + 3, Shore, trap).Ok, "along the shore is fine");
        }

        [Test] public void TheReaderNamesTheCatchAndHowTheWaterLooksNeverACount()
        {
            var trap = SetTrap(PondX0 - 2, PondY0 + 2, "east"); Days(1.1); string line = Read(trap);
            StringAssert.Contains("Holds 2 fish", line); StringAssert.Contains("full of fish", line);
            Days(2); Act(trap, TakeRow(trap).Command); Stock().Available = 0; line = Read(trap);
            StringAssert.Contains("fished out for now", line); Assert.False(System.Text.RegularExpressions.Regex.IsMatch(line, @"\b\d+ of \d+\b"), "no pond arithmetic is shown");
        }

        // ── when to come back (quality of life) ──

        [Test] public void TheReaderSaysWhenTheNextFishIsDue_InTheNameOfTheTimeOfDayNeverATickCount()
        {
            var trap = SetTrap(PondX0 - 2, PondY0 + 2, "east"); int t0 = WorldClock.CurrentTick;
            string line = Read(trap); StringAssert.Contains("next fish is due around " + BandAt(t0 + FishTrapPart.CatchTicks), line);
            Assert.False(System.Text.RegularExpressions.Regex.IsMatch(line, @"\b\d{3,}\b"), "no tick counts: " + line);
            Days(0.5); line = Read(trap);   // one is in; the clock for the next starts from the catch, a half day on
            StringAssert.Contains("Holds 1 fish", line); StringAssert.Contains("next fish is due around " + BandAt(WorldClock.CurrentTick + FishTrapPart.CatchTicks), line);
            Assert.AreEqual(0, Part(trap).Held, "reading changes nothing");
        }

        [Test] public void WhenTheNextFishIsAboutToComeTheReaderSaysSoInsteadOfNamingTheBand()
        {
            var trap = SetTrap(PondX0 - 2, PondY0 + 2, "east"); Days(0.45);   // 540 of the 600 ticks gone
            string line = Read(trap); StringAssert.Contains("due soon", line); StringAssert.DoesNotContain("due around", line);
        }

        [Test] public void AFullTrapSaysItIsFullAndPromisesNoFurtherFish_Counter()
        {
            var trap = SetTrap(PondX0 - 2, PondY0 + 2, "east"); Days(2.5); string line = Read(trap);
            StringAssert.Contains("Holds 4 fish", line); StringAssert.Contains("It is full: it catches no more until you empty it", line); StringAssert.DoesNotContain("due", line);
        }

        [Test] public void AFishedOutPondPromisesNoNextFish_Counter()
        {
            var trap = SetTrap(PondX0 - 2, PondY0 + 2, "east"); Days(1.1); Act(trap, TakeRow(trap).Command); Stock().Available = 0; string line = Read(trap);
            StringAssert.Contains("fished out for now", line); StringAssert.DoesNotContain("due", line);
        }

        [Test] public void TheDisabledTakeRowOfAnEmptyTrapSaysWhenToComeBack()
        {
            var trap = SetTrap(PondX0 - 2, PondY0 + 2, "east"); var row = TakeRow(trap);
            Assert.AreEqual("empty", row.DisabledCode); StringAssert.Contains("half day", row.DisabledReason); StringAssert.Contains("due around " + BandAt(WorldClock.CurrentTick + FishTrapPart.CatchTicks), row.DisabledReason);
        }

        [Test] public void TheMachinesScenariosPondTakesTheTrapTwoCellsWestOfTheStartThroughTheRealVerb()
        {
            foreach (var cell in wild.GetReadOnlyEntities().Where(e => e.BlueprintName == "SprayPool").ToList()) wild.RemoveEntity(cell);   // the fixture's own pond: use only the scenario's
            Assert.False(FishPond.TryFind(wild, AX - 3, AY, out _), "counter: with no pond the shore is not a shore");
            new CavesOfOoo.Scenarios.Custom.CampMachinesShowcase().Apply(new CavesOfOoo.Scenarios.ScenarioContext(wild, Factory, Actor, Clock));
            wild.RemoveEntity(Actor); Assert.True(wild.AddEntity(Actor, AX - 2, AY));
            var kit = Pack.Objects.First(o => o.BlueprintName == "CampFishTrap");
            Assert.True(SetDownRows(kit).Any(r => Dir(r) == "west"), string.Join(" / ", Actions(kit).Select(a => a.Display + (a.IsDisabled ? " [" + a.DisabledCode + "]" : ""))));
            Assert.True(Act(kit, SetDownRows(kit).First(r => Dir(r) == "west").Command), string.Join("|", MessageLog.GetRecent(3)));
            var at = wild.GetEntityPosition(kit); Assert.AreEqual(AX - 3, at.x, "it lands on the shore cell, one short of the water"); Assert.AreEqual(AY, at.y);
            Assert.False(PackableFixturePart.IsInertKit(kit), "and it stands, fishing");
        }

        [Test] public void AwayFromFishableWaterTheSetDownRowSaysWhatTheTrapNeeds_NotJustThatTheGroundIsWrong()
        {
            wild.RemoveEntity(Actor); Assert.True(wild.AddEntity(Actor, 5, 5)); var kit = Carry("CampFishTrap");
            var row = Actions(kit).Single(a => a.Name == "SetDownFixture"); Assert.True(row.IsDisabled); Assert.AreEqual(PlacementCheck.ClassSpecific, row.DisabledCode);
            StringAssert.Contains("beside water", row.DisabledReason); StringAssert.Contains("cascade basin", row.DisabledReason);
        }

        [Test] public void OtherKitsKeepTheGenericRefusalWording_Counter()
        {
            Assert.AreEqual("That cannot be set on this kind of ground.", HeldFixtureActions.Reason(PlacementCheck.ClassSpecific), "the code-only wording is unchanged for everything that has no better thing to say");
            var awning = PackableFixtureRegistry.Get("camp-awning"); var trap = PackableFixtureRegistry.Get("camp-fishtrap");
            Assert.AreEqual(HeldFixtureActions.Reason(PlacementCheck.ClassSpecific), HeldFixtureActions.Reason(PlacementCheck.ClassSpecific, awning));
            Assert.AreNotEqual(HeldFixtureActions.Reason(PlacementCheck.ClassSpecific), HeldFixtureActions.Reason(PlacementCheck.ClassSpecific, trap));
            Assert.AreEqual(HeldFixtureActions.Reason(PlacementCheck.Ground), HeldFixtureActions.Reason(PlacementCheck.Ground, trap), "only the ground-class refusal is specialised");
        }

        static string BandAt(int tick) => WorldClock.BandName(WorldClock.GetBand(tick), 0);
    }

    internal sealed class LateFixturePickupProbe : Part
    {
        internal Entity Fixture;
        internal Action Observe;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "AfterInventoryAction" || e.GetParameter<Entity>("Item") != Fixture) return true;
            Observe();
            throw new InvalidOperationException("Interrupt the provisional fixture pickup.");
        }
    }
}
