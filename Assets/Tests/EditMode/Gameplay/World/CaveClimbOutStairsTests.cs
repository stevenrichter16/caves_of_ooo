using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// User direction (2026-10-29): "there needs to be more stairs up in the caves below the
    /// surface level." Measured on seed 64 before this change: seven of eight first-floor cave
    /// zones reached sideways had no up-stair at all, and no cave floor had more than one.
    /// Ordinary cave floors now carry an up-stair in each horizontal third. Such a climb-out
    /// leads to the zone above; where that zone has no way down yet, the climb opens one at the
    /// arrival cell so the route works both ways. Paired stairs keep their exact behaviour.
    /// </summary>
    public sealed class CaveClimbOutStairsTests
    {
        static EntityFactory factory;
        OverworldZoneManager manager;

        [OneTimeSetUp] public void LoadOnce()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }
        [SetUp] public void Setup() { manager = OverworldZoneManager.CreateDetached(factory, 64); Diag.ResetAll(); MessageLog.Clear(); }

        static List<Entity> Ups(Zone zone) => zone.GetAllEntities().Where(e => e.HasPart<StairsUpPart>()).ToList();
        static List<Entity> ClimbOuts(Zone zone) => Ups(zone).Where(e => e.GetIntProperty(CaveClimbOutStairsBuilder.MarkerProperty) == 1).ToList();
        static int Third(int x) => x * 3 / Zone.Width;

        Entity Traveller(Zone zone, int x, int y)
        {
            var p = factory.CreateEntity("Player"); Assert.NotNull(p); Assert.True(zone.AddEntity(p, x, y)); return p;
        }

        // ════════════════ placement ════════════════

        bool Ordinary(int x, int z)
        {
            string id = WorldMap.ToZoneID(x, 6, z);
            return UndergroundLayoutPlan.Select(64, id, manager.WorldMap.GetBiome(x, 6), manager.WorldMap.GetPOI(x, 6), factory).IsOrdinaryColumn;
        }
        List<int> OrdinaryColumns(int z, int count)
        {
            var found = new List<int>();
            for (int x = 2; x < 24 && found.Count < count; x++) if (Ordinary(x, z)) found.Add(x);
            Assert.AreEqual(count, found.Count, "precondition: enough ordinary columns on row 6");
            return found;
        }

        [TestCase(1)] [TestCase(2)] [TestCase(3)]
        public void OrdinaryCaveFloorsOfferAnUpStairInEachThird(int z)
        {
            foreach (int x in OrdinaryColumns(z, 3))
            {
                var zone = manager.GetZone(WorldMap.ToZoneID(x, 6, z));
                var ups = Ups(zone);
                Assert.GreaterOrEqual(ups.Count, 3, zone.ZoneID + " at least three ways up");
                for (int third = 0; third < 3; third++)
                    Assert.True(ups.Any(u => Third(zone.GetEntityPosition(u).x) == third), zone.ZoneID + " an up-stair in third " + third);
                foreach (var up in ClimbOuts(zone))
                {
                    var cell = zone.GetEntityCell(up);
                    Assert.True(cell.IsPassable(), "a climb-out stands on open floor");
                    Assert.AreEqual(1, cell.Objects.Count(o => o.HasPart<StairsUpPart>() || o.HasPart<StairsDownPart>()), "never stacked on another stair");
                }
            }
        }

        [Test] public void AnAuthoredColumnGetsNoClimbOuts()
        {
            // Counter-check: Morrowfast's column (3,6) is authored; its caves keep their own stairs.
            Assert.False(Ordinary(3, 1), "precondition: Morrowfast's column is not ordinary");
            Assert.IsEmpty(ClimbOuts(manager.GetZone(WorldMap.ToZoneID(3, 6, 1))));
        }

        [Test] public void TheSurfaceGetsNoClimbOuts()
        {
            var surface = manager.GetZone(WorldMap.ToZoneID(8, 6, 0));
            Assert.IsEmpty(ClimbOuts(surface), "climb-outs belong to cave floors only");
        }

        [Test] public void AuthoredSinkholeFloorsAreUnchanged()
        {
            // The Stillleaf sinkhole column (2,4) is authored: its descent and sealed floor keep their own stairs.
            Assert.IsEmpty(ClimbOuts(manager.GetZone("Overworld.2.4.1")));
            Assert.IsEmpty(ClimbOuts(manager.GetZone("Overworld.2.4.2")));
        }

        [Test] public void PlacementIsDeterministicForTheSameWorld()
        {
            string id = WorldMap.ToZoneID(OrdinaryColumns(1, 1)[0], 6, 1);
            var first = ClimbOuts(manager.GetZone(id)).Select(e => manager.GetZone(id).GetEntityPosition(e)).OrderBy(p => p.x).ToList();
            Assert.IsNotEmpty(first, "precondition: the floor has climb-outs");
            var other = OverworldZoneManager.CreateDetached(factory, 64); var zone = other.GetZone(id);
            var second = ClimbOuts(zone).Select(e => zone.GetEntityPosition(e)).OrderBy(p => p.x).ToList();
            CollectionAssert.AreEqual(first, second);
        }

        /// <summary>A seed-64 column on row 6 whose surface has no way down, generated surface first
        /// (the order a player meets it). Fails loudly rather than testing a vacuous precondition.</summary>
        int ColumnWithoutWayDown()
        {
            for (int x = 2; x < 24; x++)
            {
                if (!Ordinary(x, 1)) continue;
                var surface = manager.GetZone(WorldMap.ToZoneID(x, 6, 0));
                if (!surface.GetAllEntities().Any(e => e.HasPart<StairsDownPart>())) return x;
            }
            Assert.Fail("no seed-64 column on row 6 lacks a way down"); return -1;
        }

        // ════════════════ climbing ════════════════

        [Test] public void ClimbingOutWhereNoWayDownExistsArrivesAboveAndOpensAWayBack()
        {
            int col = ColumnWithoutWayDown();
            var surface = manager.GetZone(WorldMap.ToZoneID(col, 6, 0));
            var cave = manager.GetZone(WorldMap.ToZoneID(col, 6, 1));
            var climb = ClimbOuts(cave).First(); var (x, y) = cave.GetEntityPosition(climb);
            Assert.IsEmpty(surface.GetAllEntities().Where(e => e.HasPart<StairsDownPart>()), "precondition: no way down above");
            var player = Traveller(cave, x, y); manager.SetActiveZone(cave);

            var up = ZoneTransitionSystem.TransitionPlayerVertical(player, cave, false, x, y, manager);
            Assert.True(up.Success, up.ErrorReason); Assert.AreSame(surface, up.NewZone);
            Assert.LessOrEqual(Mathf.Max(Mathf.Abs(up.NewPlayerX - x), Mathf.Abs(up.NewPlayerY - y)), 20, "arrives near the same spot");
            var opened = surface.GetCell(up.NewPlayerX, up.NewPlayerY).Objects.Where(o => o.HasPart<StairsDownPart>()).ToList();
            Assert.AreEqual(1, opened.Count, "a way back down opens where you arrive");
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter { Category = "event", Kind = "CaveClimbOutOpened", Limit = 5 }).Records.Count);

            manager.SetActiveZone(surface);
            var down = ZoneTransitionSystem.TransitionPlayerVertical(player, surface, true, up.NewPlayerX, up.NewPlayerY, manager);
            Assert.True(down.Success, down.ErrorReason); Assert.AreSame(cave, down.NewZone);
            Assert.AreEqual((x, y), (down.NewPlayerX, down.NewPlayerY), "the way back leads to the same climb-out");

            manager.SetActiveZone(cave);
            var again = ZoneTransitionSystem.TransitionPlayerVertical(player, cave, false, x, y, manager);
            Assert.True(again.Success, again.ErrorReason);
            Assert.AreEqual(1, surface.GetAllEntities().Count(e => e.HasPart<StairsDownPart>()), "a second climb reuses the opened way down");
        }

        [Test] public void AnOrdinaryUpStairWithoutAReturnStillRefuses()
        {
            // Counter-check for the W6.7 rule: only marked climb-outs may open a way down.
            int col = OrdinaryColumns(1, 1)[0];
            var cave = manager.GetZone(WorldMap.ToZoneID(col, 6, 1));
            var surface = manager.GetZone(WorldMap.ToZoneID(col, 6, 0));
            foreach (var e in surface.GetAllEntities().Where(e => e.HasPart<StairsDownPart>()).ToList()) surface.RemoveEntity(e);
            var open = Enumerable.Range(0, Zone.Width).SelectMany(cx => Enumerable.Range(0, Zone.Height).Select(cy => (cx, cy)))
                .First(c => cave.GetCell(c.cx, c.cy).IsPassable() && !cave.GetCell(c.cx, c.cy).Objects.Any(o => o.HasPart<StairsUpPart>() || o.HasPart<StairsDownPart>()));
            Assert.True(cave.AddEntity(factory.CreateEntity("StairsUp"), open.cx, open.cy));
            var player = Traveller(cave, open.cx, open.cy); manager.SetActiveZone(cave);
            var refused = ZoneTransitionSystem.TransitionPlayerVertical(player, cave, false, open.cx, open.cy, manager);
            Assert.False(refused.Success); StringAssert.Contains("return stairs", refused.ErrorReason);
            Assert.IsEmpty(surface.GetAllEntities().Where(e => e.HasPart<StairsDownPart>()), "nothing was opened");
        }

        [Test] public void ClimbingOutWhereAWayDownAlreadyExistsUsesItAndOpensNothing()
        {
            int col = ColumnWithoutWayDown();
            var surface = manager.GetZone(WorldMap.ToZoneID(col, 6, 0));
            var cave = manager.GetZone(WorldMap.ToZoneID(col, 6, 1));
            var open = Enumerable.Range(1, Zone.Width - 2).SelectMany(cx => Enumerable.Range(1, Zone.Height - 2).Select(cy => (cx, cy)))
                .First(c => surface.GetCell(c.cx, c.cy).IsPassable());
            Assert.True(surface.AddEntity(factory.CreateEntity("StairsDown"), open.cx, open.cy));
            var climb = ClimbOuts(cave).First(); var (x, y) = cave.GetEntityPosition(climb);
            var player = Traveller(cave, x, y); manager.SetActiveZone(cave);
            var up = ZoneTransitionSystem.TransitionPlayerVertical(player, cave, false, x, y, manager);
            Assert.True(up.Success, up.ErrorReason); Assert.AreEqual((open.cx, open.cy), (up.NewPlayerX, up.NewPlayerY));
            Assert.AreEqual(1, surface.GetAllEntities().Count(e => e.HasPart<StairsDownPart>()), "no second way down is opened");
            Assert.AreEqual(0, DiagQuery.Apply(new DiagQuery.Filter { Category = "event", Kind = "CaveClimbOutOpened", Limit = 5 }).Records.Count);
        }
    }
}
