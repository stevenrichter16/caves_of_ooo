using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// G.11 — gas-avoidance navigation. Two layers:
    ///   (a) GasNavigationWeight.ForCell — the per-cell penalty (pure,
    ///       density-scaled, immunity-aware, capped).
    ///   (b) FindPath.Search integration — with an actor, A* routes AROUND
    ///       gas; with no actor (or an immune actor) it goes straight
    ///       through; gas is a SOFT cost (a gas-only route is still
    ///       traversed, never treated as a wall).
    /// </summary>
    public class GasNavigationWeightTests
    {
        [SetUp]
        public void Setup()
        {
            GasRegistry.Initialize(@"{ ""Gases"":[
              { ""Id"":""nav-poison"", ""GasType"":""Poison"", ""Glyph"":""°"",
                ""Color"":""&g"", ""DefaultDensity"":50, ""DefaultLevel"":1,
                ""BehaviorKind"":""Poison"" } ] }");
        }

        [TearDown]
        public void TearDown() => GasRegistry.ResetForTests();

        private static GasPoolPart GasAt(Zone z, int x, int y, int density)
            => GasFactory.SpawnGas(z, x, y, "nav-poison", density: density)?.GetPart<GasPoolPart>();

        private static Entity Actor(Zone z, int x, int y, string immuneTo = null)
        {
            var e = new Entity { ID = "actor", BlueprintName = "Walker" };
            e.Tags["Creature"] = "";
            e.AddPart(new PhysicsPart { Solid = true });
            if (!string.IsNullOrEmpty(immuneTo))
                e.AddPart(new GasImmunityPart { GasType = immuneTo });
            if (x >= 0) z.AddEntity(e, x, y);
            return e;
        }

        private static void Wall(Zone z, int x, int y)
        {
            var w = new Entity { ID = "wall_" + x + "_" + y, BlueprintName = "Wall" };
            w.Tags["Solid"] = "";
            w.AddPart(new PhysicsPart { Solid = true });
            z.AddEntity(w, x, y);
        }

        // Reconstruct the set of cells a path visits (start + each step).
        private static HashSet<(int, int)> PathCells(FindPath p, int startX, int startY)
        {
            var cells = new HashSet<(int, int)> { (startX, startY) };
            int x = startX, y = startY;
            foreach (var (dx, dy) in p.Steps) { x += dx; y += dy; cells.Add((x, y)); }
            return cells;
        }

        // ════════════════════════════════════════════════════════════
        //   PART I — ForCell penalty (pure)
        // ════════════════════════════════════════════════════════════

        [Test]
        public void ForCell_NoGas_ZeroPenalty()
        {
            var z = new Zone("NavNoGas");
            var actor = Actor(z, -1, 0); // not placed
            Assert.AreEqual(0, GasNavigationWeight.ForCell(z.GetCell(5, 5), actor));
        }

        [Test]
        public void ForCell_NullCellOrActor_ZeroPenalty()
        {
            var z = new Zone("NavNull");
            Assert.AreEqual(0, GasNavigationWeight.ForCell(null, Actor(z, -1, 0)));
            GasAt(z, 5, 5, 100);
            Assert.AreEqual(0, GasNavigationWeight.ForCell(z.GetCell(5, 5), null));
        }

        [Test]
        public void ForCell_GasNonImmune_PositivePenalty()
        {
            var z = new Zone("NavGas");
            GasAt(z, 5, 5, 100);
            int w = GasNavigationWeight.ForCell(z.GetCell(5, 5), Actor(z, -1, 0));
            Assert.AreEqual(GasNavigationWeight.BASE_PENALTY + 100 / GasNavigationWeight.DENSITY_DIVISOR, w);
        }

        [Test]
        public void ForCell_ImmuneToGasType_ZeroPenalty()
        {
            var z = new Zone("NavImmune");
            GasAt(z, 5, 5, 100);
            int w = GasNavigationWeight.ForCell(z.GetCell(5, 5), Actor(z, -1, 0, immuneTo: "Poison"));
            Assert.AreEqual(0, w, "immune to Poison → no avoidance of poison gas");
        }

        [Test]
        public void ForCell_ImmuneToDifferentGasType_PenaltyStillApplies()
        {
            // Immunity is gas-type-specific: immune to Stun, but this is
            // Poison gas → still avoided. Counter to "any immunity skips".
            var z = new Zone("NavImmuneOther");
            GasAt(z, 5, 5, 100);
            int w = GasNavigationWeight.ForCell(z.GetCell(5, 5), Actor(z, -1, 0, immuneTo: "Stun"));
            Assert.Greater(w, 0);
        }

        [Test]
        public void ForCell_DenserGas_HigherPenalty()
        {
            var z = new Zone("NavDense");
            GasAt(z, 5, 5, 20);
            GasAt(z, 6, 5, 200);
            var actor = Actor(z, -1, 0);
            Assert.Greater(GasNavigationWeight.ForCell(z.GetCell(6, 5), actor),
                           GasNavigationWeight.ForCell(z.GetCell(5, 5), actor));
        }

        [Test]
        public void ForCell_VeryDenseGas_CappedAtMax()
        {
            var z = new Zone("NavCap");
            GasAt(z, 5, 5, 100000);
            Assert.AreEqual(GasNavigationWeight.MAX_PENALTY,
                GasNavigationWeight.ForCell(z.GetCell(5, 5), Actor(z, -1, 0)));
        }

        // ════════════════════════════════════════════════════════════
        //   PART II — FindPath integration
        // ════════════════════════════════════════════════════════════

        [Test]
        public void Search_NoActor_PathsStraightThroughGas()
        {
            // Backward-compat: no actor → no gas weighting (existing callers
            // unchanged). Straight E path passes through the gas cell.
            var z = new Zone("NavStraight");
            GasAt(z, 8, 5, 100);
            var p = FindPath.Search(z, 5, 5, 11, 5);
            Assert.IsTrue(p.Usable);
            Assert.IsTrue(PathCells(p, 5, 5).Contains((8, 5)),
                "no-actor path goes straight through the gas");
        }

        [Test]
        public void Search_WithActor_PathsAroundGas()
        {
            var z = new Zone("NavAround");
            GasAt(z, 8, 5, 100);
            var actor = Actor(z, 5, 5);
            var p = FindPath.Search(z, 5, 5, 11, 5, actor: actor);
            Assert.IsTrue(p.Usable);
            Assert.IsFalse(PathCells(p, 5, 5).Contains((8, 5)),
                "actor detours around the gas cell");
        }

        [Test]
        public void Search_ImmuneActor_PathsThroughGas()
        {
            var z = new Zone("NavImmunePath");
            GasAt(z, 8, 5, 100);
            var actor = Actor(z, 5, 5, immuneTo: "Poison");
            var p = FindPath.Search(z, 5, 5, 11, 5, actor: actor);
            Assert.IsTrue(p.Usable);
            Assert.IsTrue(PathCells(p, 5, 5).Contains((8, 5)),
                "immune actor has no reason to detour — goes straight");
        }

        [Test]
        public void Search_GasOnlyRoute_StillTraverses()
        {
            // A full wall column at x=8 except the gap at (8,5), which holds
            // gas. The ONLY way east is through the gas — soft cost, not a
            // wall, so the actor still traverses it.
            var z = new Zone("NavGasGap");
            for (int y = 0; y < Zone.Height; y++)
                if (y != 5) Wall(z, 8, y);
            GasAt(z, 8, 5, 100);
            var actor = Actor(z, 5, 5);
            var p = FindPath.Search(z, 5, 5, 11, 5, actor: actor);
            Assert.IsTrue(p.Usable, "gas is a soft cost, not an impassable wall");
            Assert.IsTrue(PathCells(p, 5, 5).Contains((8, 5)),
                "forced through the gas-filled gap");
        }

        [Test]
        public void Search_WithActor_NoGas_SamePathAsNoActor()
        {
            // Counter: with no gas anywhere, the actor param changes nothing.
            var z = new Zone("NavNoGasSame");
            var withActor = FindPath.Search(z, 5, 5, 11, 5, actor: Actor(z, 5, 5));
            var noActor = FindPath.Search(z, 5, 5, 11, 5);
            Assert.AreEqual(noActor.Steps.Count, withActor.Steps.Count,
                "no gas → actor param is a no-op");
        }
    }
}
