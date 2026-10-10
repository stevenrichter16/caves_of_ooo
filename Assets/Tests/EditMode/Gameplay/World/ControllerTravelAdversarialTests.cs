using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using NUnit.Framework;
using static CavesOfOoo.Tests.ControllerTravelTests;

namespace CavesOfOoo.Tests
{
    public class ControllerTravelAdversarialTests
    {
        [TestCase(0, 0)] [TestCase(2, 0)] [TestCase(0, -2)]
        [TestCase(int.MinValue, 1)] [TestCase(1, int.MaxValue)]
        public void InvalidHeadingRefusesWithoutNormalizingOrOverflow(int dx, int dy)
        {
            var zone = KnownZone(); var actor = Actor(zone);
            Assert.IsFalse(ControllerTravel.TryStepTowardEdge(zone, actor, dx, dy, out int sx, out int sy));
            Assert.AreEqual((0, 0), (sx, sy));
        }

        [Test]
        public void MissingAndDetachedContextsFailClosed()
        {
            var zone = KnownZone(); var actor = Actor(zone);
            NoPoint(null, actor, 8, 5); NoPoint(zone, null, 8, 5);
            Assert.IsTrue(ControllerTravel.HasDanger(null, actor));
            Assert.IsTrue(ControllerTravel.HasDanger(zone, null));
            var otherZone = KnownZone(); NoPoint(otherZone, actor, 8, 5);
            zone.RemoveEntity(actor); NoPoint(zone, actor, 8, 5);
            Assert.IsTrue(ControllerTravel.HasDanger(zone, actor));
        }

        [Test]
        public void WorldMapRefusesEveryLocalTravelMode()
        {
            var zone = KnownZone(true, WorldMap.WorldMapZoneID); var actor = Actor(zone);
            NoPoint(zone, actor, 8, 5);
            Assert.IsFalse(ControllerTravel.TryStepTowardEdge(zone, actor, 1, 0, out int dx, out int dy));
            Assert.AreEqual((0, 0), (dx, dy));
            Assert.IsFalse(ControllerTravel.TryStepTowardFrontier(zone, actor, out dx, out dy));
            Assert.AreEqual((0, 0), (dx, dy));
        }

        [TestCase(-1, 5)] [TestCase(80, 5)] [TestCase(5, -1)] [TestCase(5, 25)]
        [TestCase(int.MaxValue, int.MinValue)]
        public void OutsidePointCannotBecomeZoneCrossing(int x, int y)
        { var zone = KnownZone(); NoPoint(zone, Actor(zone), x, y); }

        [TestCase(false)] [TestCase(true)]
        public void DeadActorCannotPlan(bool deathHandled)
        {
            var zone = KnownZone(); var actor = Actor(zone);
            if (deathHandled) actor.SetTag("_DeathHandled"); else actor.GetStat("Hitpoints").BaseValue = 0;
            Assert.IsTrue(ControllerTravel.HasDanger(zone, actor)); NoPoint(zone, actor, 9, 5);
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void HiddenInvisibleOrDeadEnemyDoesNotRevealThreat(int mode)
        {
            var zone = KnownZone(); var actor = Actor(zone);
            var enemy = Threat(zone, actor, 15, 10);
            if (mode == 0) zone.GetCell(15, 10).IsVisible = false;
            if (mode == 1) enemy.GetPart<RenderPart>().Visible = false;
            if (mode == 2) enemy.GetStat("Hitpoints").BaseValue = 0;
            Assert.IsFalse(ControllerTravel.HasDanger(zone, actor));
            Assert.IsTrue(ControllerTravel.TryStepTowardPoint(zone, actor, 9, 5, out _, out _));
            zone.GetCell(15, 10).IsVisible = true;
            enemy.GetPart<RenderPart>().Visible = true; enemy.GetStat("Hitpoints").BaseValue = 20;
            Assert.IsTrue(ControllerTravel.HasDanger(zone, actor));
        }

        [Test]
        public void PassiveCreatureStopsTravelOnlyOncePersonallyHostile()
        {
            var zone = KnownZone(); var actor = Actor(zone);
            var enemy = Threat(zone, actor, 15, 10);
            var brain = enemy.GetPart<BrainPart>(); brain.PersonalEnemies.Clear(); brain.Passive = true;
            actor.GetPart<BrainPart>().PersonalEnemies.Add(enemy);
            Assert.IsTrue(FactionManager.IsHostile(enemy, actor), "countercheck retains faction-level hostility");
            Assert.IsFalse(ControllerTravel.HasDanger(zone, actor));
            brain.PersonalEnemies.Add(actor);
            Assert.IsTrue(ControllerTravel.HasDanger(zone, actor));
        }

        [Test]
        public void VisibleRemoteFootprintOfThreatCountsEvenWhenAnchorHidden()
        {
            var zone = KnownZone(); var actor = Actor(zone);
            var enemy = new Entity(); enemy.SetTag("Creature");
            enemy.AddPart(new BrainPart()); enemy.AddPart(new RenderPart());
            enemy.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;1,0" });
            enemy.Statistics["Hitpoints"] = new Stat { Owner = enemy, Name = "Hitpoints", BaseValue = 10, Max = 10 };
            enemy.GetPart<BrainPart>().PersonalEnemies.Add(actor);
            Assert.IsTrue(zone.AddEntity(enemy, 15, 10)); zone.GetCell(16, 10).IsVisible = true;
            Assert.IsFalse(zone.GetCell(15, 10).IsVisible);
            Assert.IsTrue(ControllerTravel.HasDanger(zone, actor));
            zone.GetCell(16, 10).IsVisible = false;
            Assert.IsFalse(ControllerTravel.HasDanger(zone, actor));
        }

        [Test]
        public void UnknownTerrainDoesNotChangeFrontierOrRevealIt()
        {
            var zone = KnownZone(); var actor = Actor(zone);
            var unknown = zone.GetCell(10, 5); unknown.Explored = false;
            Assert.IsTrue(ControllerTravel.TryStepTowardFrontier(zone, actor, out int ax, out int ay));
            var wall = Place(zone, 10, 5, new PhysicsPart { Solid = true });
            Assert.IsTrue(ControllerTravel.TryStepTowardFrontier(zone, actor, out int bx, out int by));
            Assert.AreEqual((ax, ay), (bx, by));
            Assert.IsFalse(unknown.Explored); Assert.IsFalse(unknown.IsVisible);
            Assert.AreSame(wall, unknown.Objects[0]);
        }

        [Test]
        public void UnknownGapCannotBeUsedAsShortcutToKnownPoint()
        {
            var zone = Corridor(); var actor = Actor(zone);
            zone.GetCell(8, 5).Explored = false;
            NoPoint(zone, actor, 12, 5);
            Know(zone, 8, 5); WalkTo(zone, actor, 12, 5);
        }

        [Test]
        public void UnreachableFrontierDoesNotLureActorIntoAnotherRegion()
        {
            var zone = KnownZone(); var actor = Actor(zone);
            for (int y = 0; y < Zone.Height; y++) Place(zone, 20, y, null, "Solid");
            zone.GetCell(40, 10).Explored = false;
            Assert.IsFalse(ControllerTravel.TryStepTowardFrontier(zone, actor, out int dx, out int dy));
            Assert.AreEqual((0, 0), (dx, dy));
            zone.GetCell(10, 10).Explored = false;
            Assert.IsTrue(ControllerTravel.TryStepTowardFrontier(zone, actor, out _, out _));
        }

        [Test]
        public void VisibleButNotYetExploredNeighborIsNotAnUnknownFrontier()
        {
            var zone = KnownZone(); var actor = Actor(zone);
            var cell = zone.GetCell(10, 5); cell.Explored = false; cell.IsVisible = true;
            Assert.IsFalse(ControllerTravel.TryStepTowardFrontier(zone, actor, out _, out _));
            cell.IsVisible = false;
            Assert.IsTrue(ControllerTravel.TryStepTowardFrontier(zone, actor, out _, out _));
        }

        [Test]
        public void WideBodyCannotUseOneCellKnownPassage()
        {
            var zone = Corridor(); var actor = Actor(zone, shape: "0,0;0,1");
            NoPoint(zone, actor, 12, 5);
            for (int x = 5; x <= 12; x++) Know(zone, x, 6);
            WalkTo(zone, actor, 12, 5);
        }

        [Test]
        public void RemoteFootHazardAndCollisionAreBothChecked()
        {
            var zone = Corridor(); var actor = Actor(zone, shape: "0,0;0,1");
            for (int x = 5; x <= 12; x++) Know(zone, x, 6);
            var gas = Place(zone, 8, 6, new GasPoolPart { Density = 20 });
            NoPoint(zone, actor, 12, 5);
            zone.RemoveEntity(gas);
            var wall = Place(zone, 8, 6, new PhysicsPart { Solid = true });
            NoPoint(zone, actor, 12, 5);
            zone.RemoveEntity(wall); WalkTo(zone, actor, 12, 5);
        }

        [Test]
        public void WideActorStopsAtPhysicalBoundaryBeforeAnchorReachesIt()
        {
            var zone = KnownZone(); var actor = Actor(zone, 78, 10, "0,0;1,0");
            Assert.IsFalse(ControllerTravel.TryStepTowardEdge(zone, actor, 1, 0, out int dx, out int dy));
            Assert.AreEqual((0, 0), (dx, dy)); NoPoint(zone, actor, 79, 10);
        }

        [Test]
        public void CandidateAnchorHoleDoesNotHideUnknownPhysicalCell()
        {
            var zone = KnownZone(false); var actor = Actor(zone, shape: "1,0");
            Know(zone, 5, 5); Know(zone, 6, 5);
            NoPoint(zone, actor, 6, 5);
            Know(zone, 7, 5);
            Assert.IsTrue(ControllerTravel.TryStepTowardPoint(zone, actor, 6, 5, out int dx, out int dy));
            Assert.AreEqual((1, 0), (dx, dy));
        }

        [Test]
        public void StalePlacedFootprintFailsClosedWithoutRepairingIt()
        {
            var zone = KnownZone(); var actor = Actor(zone, shape: "0,0;1,0");
            actor.GetPart<SpatialFootprintPart>().CellsRaw = "0,0;2,0";
            NoPoint(zone, actor, 12, 5);
            Assert.AreEqual("0,0;2,0", actor.GetPart<SpatialFootprintPart>().CellsRaw);
            Assert.AreEqual((5, 5), zone.GetEntityPosition(actor));
        }

        [Test]
        public void DiagonalCannotSqueezeBetweenTwoBlockedWholePlacements()
        {
            var zone = KnownZone(false); var actor = Actor(zone);
            Know(zone, 6, 5); Know(zone, 5, 6); Know(zone, 6, 6);
            var east = Place(zone, 6, 5, new PhysicsPart { Solid = true });
            Place(zone, 5, 6, new PhysicsPart { Solid = true });
            NoPoint(zone, actor, 6, 6);
            zone.RemoveEntity(east);
            Assert.IsTrue(ControllerTravel.TryStepTowardPoint(zone, actor, 6, 6, out int dx, out int dy));
            Assert.AreEqual((1, 1), (dx, dy));
        }

        [Test]
        public void DiagonalCannotUseUnknownSidesToCutCorner()
        {
            var zone = KnownZone(false); var actor = Actor(zone); Know(zone, 6, 6);
            NoPoint(zone, actor, 6, 6);
            Know(zone, 6, 5);
            Assert.IsTrue(ControllerTravel.TryStepTowardPoint(zone, actor, 6, 6, out _, out _));
        }

        [Test]
        public void OccupancyIsRecheckedBetweenCallsRatherThanCached()
        {
            var zone = Corridor(); var actor = Actor(zone);
            Assert.IsTrue(ControllerTravel.TryStepTowardPoint(zone, actor, 12, 5, out _, out _));
            var blocker = Place(zone, 6, 5, new PhysicsPart { Solid = true }, "Creature");
            NoPoint(zone, actor, 12, 5);
            zone.RemoveEntity(blocker);
            Assert.IsTrue(ControllerTravel.TryStepTowardPoint(zone, actor, 12, 5, out _, out _));
        }

        [Test]
        public void CurrentHazardStopsConvenienceUntilCleared()
        {
            var zone = Corridor(); var actor = Actor(zone);
            var gas = new GasPoolPart { Density = 20 }; Place(zone, 5, 5, gas);
            Assert.IsTrue(ControllerTravel.HasDanger(zone, actor)); NoPoint(zone, actor, 12, 5);
            gas.Density = 0;
            Assert.IsFalse(ControllerTravel.HasDanger(zone, actor));
            Assert.IsTrue(ControllerTravel.TryStepTowardPoint(zone, actor, 12, 5, out _, out _));
        }

        [Test]
        public void JammedSupportedTrapAndOwnFactionRuneAreSafeCountercases()
        {
            var zone = Corridor(); var actor = Actor(zone);
            var trap = Place(zone, 8, 5, new SpikeTrapTriggerPart()); trap.BlueprintName = "SpikeTrap";
            trap.AddPart(new TrapJammingPart { Jammed = true });
            Assert.IsTrue(TrapJammingPart.IsJammed(trap));
            var rune = Place(zone, 9, 5, new RuneFlameTriggerPart { TriggerFaction = "Player" });
            Assert.IsTrue(ControllerTravel.TryStepTowardPoint(zone, actor, 12, 5, out _, out _));
            trap.GetPart<TrapJammingPart>().Jammed = false; NoPoint(zone, actor, 12, 5);
            trap.GetPart<TrapJammingPart>().Jammed = true;
            rune.GetPart<RuneFlameTriggerPart>().TriggerFaction = "Other"; NoPoint(zone, actor, 12, 5);
        }

        [Test]
        public void ActualBurningEffectWithoutThermalPartStillBlocks()
        {
            var zone = Corridor(); var actor = Actor(zone);
            var owner = Place(zone, 8, 5, new StatusEffectsPart());
            var fire = new BurningEffect { Owner = owner, Duration = 5 };
            owner.GetPart<StatusEffectsPart>().RestoreEffectsForLoad(new List<Effect> { fire });
            NoPoint(zone, actor, 12, 5);
            fire.Duration = 0;
            Assert.IsTrue(ControllerTravel.TryStepTowardPoint(zone, actor, 12, 5, out _, out _));
        }

        [Test]
        public void UnseenAdjacentHotSteamMayConservativelyStopWithoutRevealingSource()
        {
            var zone = Corridor(); var actor = Actor(zone);
            var thermal = new ThermalPart { Temperature = 150, FlameTemperature = 400 };
            var source = Place(zone, 8, 6, thermal); source.AddPart(new StatusEffectsPart());
            source.GetPart<StatusEffectsPart>().RestoreEffectsForLoad(new List<Effect> { new SteamEffect() });
            Assert.Greater(TerrainNavigationWeight.ForStep(zone, 8, 5, actor), 0);
            NoPoint(zone, actor, 12, 5);
            Assert.IsFalse(zone.GetCell(8, 6).Explored); Assert.IsFalse(zone.GetCell(8, 6).IsVisible);
            thermal.Temperature = 25;
            Assert.IsTrue(ControllerTravel.TryStepTowardPoint(zone, actor, 12, 5, out _, out _));
        }

        private sealed class EventCounter : Part
        {
            public int Count;
            public override bool HandleEvent(GameEvent e) { Count++; return true; }
        }
        private sealed class VetoCounter : Effect
        {
            public override string DisplayName => "travel test veto";
            public int Calls;
            public override bool AllowAction(Entity actor) { Calls++; return false; }
            public override bool AllowMovement(Entity actor) { Calls++; return false; }
        }

        [Test]
        public void RepeatedQueriesHaveNoEventsEffectCallbacksTurnsOrWorldChanges()
        {
            var zone = KnownZone(); var actor = Actor(zone);
            var counter = new EventCounter(); actor.AddPart(counter);
            var veto = new VetoCounter { Owner = actor, Duration = 5 };
            actor.GetPart<StatusEffectsPart>().RestoreEffectsForLoad(new List<Effect> { veto });
            int version = zone.EntityVersion, hp = actor.GetStatValue("Hitpoints"); counter.Count = 0;
            for (int i = 0; i < 20; i++)
            {
                Assert.IsTrue(ControllerTravel.TryStepTowardPoint(zone, actor, 12, 5, out int dx, out int dy));
                Assert.AreEqual((1, 0), (dx, dy));
            }
            Assert.AreEqual(0, counter.Count); Assert.AreEqual(0, veto.Calls);
            Assert.AreEqual((5, 5), zone.GetEntityPosition(actor));
            Assert.AreEqual(version, zone.EntityVersion); Assert.AreEqual(hp, actor.GetStatValue("Hitpoints"));
            Assert.AreEqual(5, veto.Duration);
            actor.FireEvent("Countercheck"); Assert.Greater(counter.Count, 0);
        }

        [Test, Timeout(5000)]
        public void EnclosedKnownRegionTerminatesWithoutRetainingAPath()
        {
            var zone = KnownZone(); var actor = Actor(zone);
            for (int y = 0; y < Zone.Height; y++) Place(zone, 70, y, null, "Solid");
            NoPoint(zone, actor, 75, 5);
            Assert.IsTrue(ControllerTravel.TryStepTowardPoint(zone, actor, 12, 5, out _, out _));
            NoPoint(zone, actor, 5, 5);
            Assert.AreEqual((5, 5), zone.GetEntityPosition(actor), "discarding a suggestion cancels without pending work");
        }

        [Test]
        public void ReachabilityBuildIncludesCurrentCellAndKnownReachableCells()
        {
            var zone = Corridor(); var actor = Actor(zone);
            var map = ControllerTravel.BuildReachable(zone, actor);
            Assert.IsNotNull(map); Assert.IsTrue(map.CanReach(5, 5));
            Assert.IsTrue(map.CanReach(12, 5)); Assert.IsFalse(map.CanReach(13, 5));
            Assert.IsFalse(map.CanReach(5, 4));
            NoPoint(zone, actor, 5, 5); // Reachable does not imply a nonzero movement step.
        }

        [TestCase(-1, 5)] [TestCase(80, 5)] [TestCase(5, -1)] [TestCase(5, 25)]
        [TestCase(int.MinValue, int.MaxValue)]
        public void ReachabilityLookupRejectsOutsideCoordinates(int x, int y)
        {
            var zone = KnownZone(); var actor = Actor(zone);
            Assert.IsFalse(ControllerTravel.BuildReachable(zone, actor).CanReach(x, y));
        }

        [Test]
        public void ReachabilityInvalidOrDangerousContextReturnsEmptySnapshot()
        {
            var zone = KnownZone(); var actor = Actor(zone);
            Assert.IsFalse(ControllerTravel.BuildReachable(null, actor).CanReach(5, 5));
            Assert.IsFalse(ControllerTravel.BuildReachable(zone, null).CanReach(5, 5));
            Threat(zone, actor, 15, 10);
            var unsafeMap = ControllerTravel.BuildReachable(zone, actor);
            Assert.IsFalse(unsafeMap.CanReach(5, 5)); Assert.IsFalse(unsafeMap.CanReach(6, 5));
            zone.GetCell(15, 10).IsVisible = false;
            Assert.IsTrue(ControllerTravel.BuildReachable(zone, actor).CanReach(6, 5));
        }

        [Test]
        public void ReachabilityMatchesPointAdmissionAcrossKnownPartition()
        {
            var zone = Corridor(); var actor = Actor(zone);
            Place(zone, 8, 5, new DoorPart { IsOpen = false });
            var map = ControllerTravel.BuildReachable(zone, actor);
            for (int x = 6; x <= 13; x++)
            {
                bool point = ControllerTravel.TryStepTowardPoint(zone, actor, x, 5, out _, out _);
                Assert.AreEqual(point, map.CanReach(x, 5), "admission mismatch at " + x);
            }
            Assert.IsTrue(map.CanReach(7, 5)); Assert.IsFalse(map.CanReach(9, 5));
        }

        [Test]
        public void ReachabilitySnapshotDoesNotReadLiveWorldDuringLookup()
        {
            var zone = Corridor(); var actor = Actor(zone);
            var map = ControllerTravel.BuildReachable(zone, actor);
            Assert.IsTrue(map.CanReach(12, 5));
            for (int x = 5; x <= 12; x++)
            { zone.GetCell(x, 5).Explored = false; zone.GetCell(x, 5).IsVisible = false; }
            Place(zone, 8, 5, new PhysicsPart { Solid = true });
            actor.GetStat("Hitpoints").BaseValue = 0;
            // A map is only a menu-building snapshot; executing travel MUST re-query.
            for (int i = 0; i < 1000; i++) Assert.IsTrue(map.CanReach(12, 5));
            Assert.IsFalse(ControllerTravel.BuildReachable(zone, actor).CanReach(12, 5));
            NoPoint(zone, actor, 12, 5);
        }

        [Test]
        public void ReachabilityMapsDoNotShareMutableResultsBetweenBuilds()
        {
            var zone = Corridor(); var actor = Actor(zone);
            var open = ControllerTravel.BuildReachable(zone, actor);
            var wall = Place(zone, 8, 5, new PhysicsPart { Solid = true });
            var blocked = ControllerTravel.BuildReachable(zone, actor);
            zone.RemoveEntity(wall);
            var reopened = ControllerTravel.BuildReachable(zone, actor);
            Assert.IsTrue(open.CanReach(12, 5)); Assert.IsFalse(blocked.CanReach(12, 5));
            Assert.IsTrue(reopened.CanReach(12, 5));
        }

        [Test]
        public void ReachabilityBuildRetainsWholeBodyAndDiagonalRules()
        {
            var zone = Corridor(); var actor = Actor(zone, shape: "0,0;0,1");
            Assert.IsFalse(ControllerTravel.BuildReachable(zone, actor).CanReach(12, 5));
            for (int x = 5; x <= 12; x++) Know(zone, x, 6);
            Assert.IsTrue(ControllerTravel.BuildReachable(zone, actor).CanReach(12, 5));
            var diagonalZone = KnownZone(false); var single = Actor(diagonalZone);
            Know(diagonalZone, 6, 6);
            Assert.IsFalse(ControllerTravel.BuildReachable(diagonalZone, single).CanReach(6, 6));
            Know(diagonalZone, 6, 5);
            Assert.IsTrue(ControllerTravel.BuildReachable(diagonalZone, single).CanReach(6, 6));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void ReachabilityBuildRejectsGasFireAndTriggers(int kind)
        {
            var zone = Corridor(); var actor = Actor(zone);
            Part hazard = kind == 0 ? (Part)new GasPoolPart { Density = 20 }
                : kind == 1 ? (Part)new ThermalPart { Temperature = 500, FlameTemperature = 400 }
                : new SpikeTrapTriggerPart();
            var owner = Place(zone, 8, 5, hazard);
            Assert.IsFalse(ControllerTravel.BuildReachable(zone, actor).CanReach(12, 5));
            zone.RemoveEntity(owner);
            Assert.IsTrue(ControllerTravel.BuildReachable(zone, actor).CanReach(12, 5));
        }

        [Test]
        public void ReachabilityBuildHasNoEventsMovementOrVirtualVetoCallbacks()
        {
            var zone = KnownZone(); var actor = Actor(zone);
            var counter = new EventCounter(); actor.AddPart(counter);
            var veto = new VetoCounter { Duration = 5 };
            actor.GetPart<StatusEffectsPart>().RestoreEffectsForLoad(new List<Effect> { veto });
            counter.Count = 0;
            var map = ControllerTravel.BuildReachable(zone, actor);
            int reachable = 0;
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                if (map.CanReach(x, y)) reachable++;
            Assert.AreEqual(Zone.Width * Zone.Height, reachable);
            Assert.AreEqual(0, counter.Count); Assert.AreEqual(0, veto.Calls);
            Assert.AreEqual((5, 5), zone.GetEntityPosition(actor)); Assert.AreEqual(5, veto.Duration);
        }
    }
}
