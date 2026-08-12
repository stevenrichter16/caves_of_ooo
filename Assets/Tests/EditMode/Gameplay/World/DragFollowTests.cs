using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// D3 of Drag &amp; Haul (<c>Docs/DRAG-AND-HAUL.md</c> §7) — the load
    /// comes with you.
    ///
    /// <para><b>The follow rule:</b> when the hauler moves A → B, the load
    /// moves to <b>A</b> — the cell the hauler just left. It trails you.
    /// This needs no pathfinding, because the destination is always a cell
    /// that was passable one tick ago (the hauler was standing in it), and
    /// it produces the right feel for free: the load is always behind you,
    /// and turning a corner swings it around.</para>
    ///
    /// <para>The interesting cases are all about the vacated cell not being
    /// available after all — someone stepped into it, or the hauler did not
    /// really move.</para>
    /// </summary>
    public class DragFollowTests
    {
        [SetUp]
        public void SetUp()
        {
            Diag.ResetAll();
            MessageLog.Clear();
        }

        private static Entity Hauler(int strength = 16)
        {
            var e = new Entity { ID = "hauler", BlueprintName = "Player" };
            e.Tags["Creature"] = "";
            e.Statistics["Strength"] = new Stat
            { Owner = e, Name = "Strength", BaseValue = strength, Min = 0, Max = 40 };
            e.AddPart(new PhysicsPart { Weight = 100, Takeable = false });
            return e;
        }

        private static Entity Load(int weight = 120, string id = "load")
        {
            var e = new Entity { ID = id, BlueprintName = "MillStone" };
            // NOT Solid: a load the hauler could never walk past would make
            // most of these scenarios untestable and is a D4 question.
            e.AddPart(new PhysicsPart { Weight = weight, Takeable = false });
            e.AddPart(new HandlingPart { Weight = weight, Carryable = false });
            return e;
        }

        /// <summary>Hauler at (5,5) holding a load at (6,5).</summary>
        private static (Zone zone, Entity actor, Entity load) Hauling()
        {
            var zone = new Zone("T");
            var actor = Hauler();
            var load = Load();
            zone.AddEntity(actor, 5, 5);
            zone.AddEntity(load, 6, 5);
            Assert.AreEqual(DragVerdict.Ok, DragSystem.TryGrab(actor, load, zone),
                "fixture precondition: the grab must succeed");
            return (zone, actor, load);
        }

        private static (int x, int y) At(Zone zone, Entity e) => zone.GetEntityPosition(e);

        // ════════════════════════════════════════════════════════
        // The rule
        // ════════════════════════════════════════════════════════

        [Test]
        public void TheLoadMovesIntoTheCellTheHaulerLeft()
        {
            var (zone, actor, load) = Hauling();

            MovementSystem.TryMoveTo(actor, zone, 4, 5);

            Assert.AreEqual((4, 5), At(zone, actor));
            Assert.AreEqual((5, 5), At(zone, load), "the load takes the vacated cell");
        }

        [Test]
        public void ItTrailsAcrossSeveralSteps()
        {
            // Each step re-reads the hauler's old cell, so the load stays
            // exactly one behind rather than teleporting to the finish.
            var (zone, actor, load) = Hauling();

            MovementSystem.TryMoveTo(actor, zone, 4, 5);
            MovementSystem.TryMoveTo(actor, zone, 3, 5);
            MovementSystem.TryMoveTo(actor, zone, 2, 5);

            Assert.AreEqual((2, 5), At(zone, actor));
            Assert.AreEqual((3, 5), At(zone, load), "one cell behind, every step");
        }

        [Test]
        public void ItSwingsAroundACorner()
        {
            // The whole reason the rule is "the vacated cell" rather than
            // "keep the relative offset": turning drags the load around
            // behind you instead of sliding it sideways through a wall.
            var (zone, actor, load) = Hauling();

            MovementSystem.TryMoveTo(actor, zone, 5, 4);   // north
            Assert.AreEqual((5, 5), At(zone, load));

            MovementSystem.TryMoveTo(actor, zone, 4, 4);   // then west
            Assert.AreEqual((5, 4), At(zone, load), "the load follows the path, not a vector");
        }

        [Test]
        public void ADiagonalStepWorksTheSameWay()
        {
            var (zone, actor, load) = Hauling();
            MovementSystem.TryMoveTo(actor, zone, 4, 4);
            Assert.AreEqual((5, 5), At(zone, load));
        }

        [Test]
        public void AForcedMoveDragsTheLoadToo()
        {
            // Being shoved while hauling still takes the load with you —
            // ForceMoveTo runs the same AfterMove pipeline, so the follow
            // rule is not something knockback can quietly bypass.
            var (zone, actor, load) = Hauling();

            MovementSystem.ForceMoveTo(actor, zone, 4, 5);

            Assert.AreEqual((4, 5), At(zone, actor));
            Assert.AreEqual((5, 5), At(zone, load));
        }

        // ════════════════════════════════════════════════════════
        // When the vacated cell will not have it
        // ════════════════════════════════════════════════════════

        [Test]
        public void AMoveThatDidNotHappen_DoesNotDragTheLoad()
        {
            // The follow rule hangs off AfterMove, which a blocked move
            // never fires — so this is really asserting that nothing else
            // in the chain moves the load speculatively.
            var (zone, actor, load) = Hauling();

            var wall = new Entity { ID = "wall", BlueprintName = "Wall" };
            wall.AddPart(new PhysicsPart { Solid = true, Takeable = false });
            zone.AddEntity(wall, 4, 5);

            MovementSystem.TryMoveTo(actor, zone, 4, 5);

            Assert.AreEqual((5, 5), At(zone, actor), "precondition: the wall stopped us");
            Assert.AreEqual((6, 5), At(zone, load),
                "a move that did not happen must not drag the load");
            Assert.IsTrue(DragSystem.IsDragging(actor), "and must not break the grip");
        }

        [Test]
        public void TheGripBreaksWhenTheLoadCannotFollow()
        {
            var (zone, actor, load) = Hauling();

            // Wall off the cell the hauler is about to leave by putting a
            // solid thing in it alongside the hauler.
            var blocker = new Entity { ID = "rubble", BlueprintName = "Rock" };
            blocker.AddPart(new PhysicsPart { Solid = true, Takeable = false });
            zone.AddEntity(blocker, 5, 5);

            MovementSystem.TryMoveTo(actor, zone, 4, 5);

            Assert.IsFalse(DragSystem.IsDragging(actor), "the grip must break");
            Assert.IsFalse(DragSystem.IsBeingDragged(load));
            Assert.AreEqual((6, 5), At(zone, load), "the load stayed where it was");
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter
            { Category = "drag", Kind = "Slipped", Limit = 10 }).Records.Count,
                "losing your grip is a distinct, queryable event");
        }

        [Test]
        public void SlippingTellsThePlayer()
        {
            var (zone, actor, load) = Hauling();
            var blocker = new Entity { ID = "rubble", BlueprintName = "Rock" };
            blocker.AddPart(new PhysicsPart { Solid = true, Takeable = false });
            zone.AddEntity(blocker, 5, 5);
            MessageLog.Clear();

            MovementSystem.TryMoveTo(actor, zone, 4, 5);

            StringAssert.Contains("slip", string.Join(" ", MessageLog.GetRecent(5)).ToLowerInvariant());
        }

        // ════════════════════════════════════════════════════════
        // Counter-checks — the rule must not fire when it shouldn't
        // ════════════════════════════════════════════════════════

        [Test]
        public void AnEntityHaulingNothing_MovesAlone()
        {
            var zone = new Zone("T");
            var actor = Hauler();
            var bystander = Load(id: "bystander");
            zone.AddEntity(actor, 5, 5);
            zone.AddEntity(bystander, 6, 5);

            MovementSystem.TryMoveTo(actor, zone, 4, 5);

            Assert.AreEqual((6, 5), At(zone, bystander),
                "an ungrabbed object must not follow anybody");
        }

        [Test]
        public void AfterReleasing_TheLoadStopsFollowing()
        {
            // The counter-check that makes every test above meaningful: if
            // the follow rule keyed off something other than the live link,
            // this would keep dragging.
            var (zone, actor, load) = Hauling();

            MovementSystem.TryMoveTo(actor, zone, 4, 5);
            Assert.AreEqual((5, 5), At(zone, load));

            DragSystem.Release(actor);
            MovementSystem.TryMoveTo(actor, zone, 3, 5);

            Assert.AreEqual((5, 5), At(zone, load), "released loads stay put");
        }

        [Test]
        public void MovingTheLoadItselfDoesNotDragTheHauler()
        {
            // The link is directional. Shoving the millstone must not yank
            // the person holding it.
            var (zone, actor, load) = Hauling();

            MovementSystem.ForceMoveTo(load, zone, 7, 5);

            Assert.AreEqual((5, 5), At(zone, actor), "the hauler stays put");
        }

        // ════════════════════════════════════════════════════════
        // Degenerate
        // ════════════════════════════════════════════════════════

        [Test]
        public void AZeroDistanceMove_DoesNotStackTheLoadOnTheHauler()
        {
            // MovementSystem.FireCellEnteredEvents has a cell-change-only
            // guard, but AfterMove itself still fires for a move to the
            // cell you already occupy. If the follow rule did not notice,
            // the load would be moved on top of the hauler.
            var (zone, actor, load) = Hauling();

            MovementSystem.TryMoveTo(actor, zone, 5, 5);

            Assert.AreEqual((5, 5), At(zone, actor));
            Assert.AreEqual((6, 5), At(zone, load), "nothing moved, so nothing follows");
        }
    }
}
