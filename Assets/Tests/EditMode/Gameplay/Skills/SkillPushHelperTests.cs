using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// SPELLCRAFT SM1 (Docs/SPELLCRAFT-STATUS-SYNERGY.md §5, P1) —
    /// knockback extracted to a shared primitive.
    ///
    /// <para>The plan's shockwave (Ground Surge), jet blast and Undertow
    /// all move a creature one cell. Today the only push in the codebase
    /// is inlined in <c>Cudgel_GroundPound.cs:124-131</c>, so every new
    /// spell would copy-paste the destination-solid and
    /// occupancy checks — and copy-pasted guards drift.</para>
    ///
    /// <para>These tests pin the contract BEFORE the extraction, so the
    /// refactor is provably behaviour-preserving. <c>TryPush</c> returns
    /// whether the creature actually moved, which callers need: a push
    /// that fails against a wall should still read as a landed hit, not
    /// as a silently swallowed no-op.</para>
    /// </summary>
    [TestFixture]
    public class SkillPushHelperTests
    {
        private Zone _zone;

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            _zone = new Zone();
        }

        /// <summary>A cell is solid when something in it carries the
        /// "Solid" tag (Cell.IsSolid, Cell.cs:87) — there is no solidity
        /// field to set.</summary>
        private Entity Wall(int x, int y)
        {
            var w = new Entity { ID = $"wall{x}_{y}", BlueprintName = "Wall" };
            w.Tags["Solid"] = "";
            w.AddPart(new RenderPart { DisplayName = "wall", RenderString = "#" });
            _zone.AddEntity(w, x, y);
            return w;
        }

        private Entity Creature(string name, int x, int y)
        {
            var e = new Entity { ID = name, BlueprintName = name };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Value = 20 };
            e.AddPart(new RenderPart { DisplayName = name, RenderString = "@", ColorString = "&y" });
            _zone.AddEntity(e, x, y);
            return e;
        }

        // ── The happy path ───────────────────────────────────────

        [Test]
        public void TryPush_MovesTheTargetOneCellAway()
        {
            var actor = Creature("actor", 5, 5);
            var target = Creature("target", 6, 5);

            bool moved = SkillCombatHelpers.TryPush(actor, target, _zone);

            Assert.IsTrue(moved, "an unobstructed push moves the target");
            var pos = _zone.GetEntityPosition(target);
            Assert.AreEqual(7, pos.x, "pushed directly away from the actor");
            Assert.AreEqual(5, pos.y);
        }

        [Test]
        public void TryPush_PushesAlongDiagonals()
        {
            var actor = Creature("actor", 5, 5);
            var target = Creature("target", 6, 6);

            Assert.IsTrue(SkillCombatHelpers.TryPush(actor, target, _zone));
            var pos = _zone.GetEntityPosition(target);
            Assert.AreEqual(7, pos.x, "diagonal pushes continue the diagonal");
            Assert.AreEqual(7, pos.y);
        }

        // ── The guards (counter-checks) ──────────────────────────

        [Test]
        public void TryPush_FailsIntoAWall_AndReportsIt()
        {
            var actor = Creature("actor", 5, 5);
            var target = Creature("target", 6, 5);
            Wall(7, 5);

            bool moved = SkillCombatHelpers.TryPush(actor, target, _zone);

            Assert.IsFalse(moved, "a wall stops the push");
            var pos = _zone.GetEntityPosition(target);
            Assert.AreEqual(6, pos.x, "and the target does not teleport into it");
            Assert.AreEqual(5, pos.y);
        }

        [Test]
        public void TryPush_FailsIntoAnotherCreature()
        {
            var actor = Creature("actor", 5, 5);
            var target = Creature("target", 6, 5);
            Creature("bystander", 7, 5);

            Assert.IsFalse(SkillCombatHelpers.TryPush(actor, target, _zone),
                "creatures do not stack");
            Assert.AreEqual(6, _zone.GetEntityPosition(target).x);
        }

        [Test]
        public void TryPush_IgnoresNonCreatureOccupants()
        {
            // Counter-check to the test above: the occupancy guard must
            // reject CREATURES only. An item lying on the floor is not
            // an obstacle, and treating it as one would make pushes fail
            // unpredictably wherever loot has dropped — which, after the
            // loot overhaul, is everywhere.
            var actor = Creature("actor", 5, 5);
            var target = Creature("target", 6, 5);
            var item = new Entity { ID = "loot", BlueprintName = "Dagger" };
            item.AddPart(new RenderPart { DisplayName = "dagger", RenderString = "/" });
            _zone.AddEntity(item, 7, 5);

            Assert.IsTrue(SkillCombatHelpers.TryPush(actor, target, _zone),
                "loot on the floor does not block a shove");
            Assert.AreEqual(7, _zone.GetEntityPosition(target).x);
        }

        // ── Null / boundary safety ───────────────────────────────

        [Test]
        public void TryPush_NullArgumentsAreGracefulFalse()
        {
            var actor = Creature("actor", 5, 5);
            var target = Creature("target", 6, 5);

            Assert.IsFalse(SkillCombatHelpers.TryPush(null, target, _zone));
            Assert.IsFalse(SkillCombatHelpers.TryPush(actor, null, _zone));
            Assert.IsFalse(SkillCombatHelpers.TryPush(actor, target, null));
        }

        [Test]
        public void TryPush_OffTheMapEdgeFails()
        {
            // Zone is 80x25 (Zone.cs:13-14), so the real edge is x=79.
            var actor = Creature("actor", 78, 5);
            var target = Creature("target", 79, 5);

            Assert.IsFalse(SkillCombatHelpers.TryPush(actor, target, _zone),
                "the zone edge stops the push instead of throwing");
            Assert.AreEqual(79, _zone.GetEntityPosition(target).x);
        }

        [Test]
        public void TryPush_CoincidentActorAndTargetFails()
        {
            // Self-referential gate: with no direction to push along,
            // the helper must decline rather than pick one arbitrarily.
            var actor = Creature("actor", 5, 5);
            var target = Creature("target", 5, 5);

            Assert.IsFalse(SkillCombatHelpers.TryPush(actor, target, _zone),
                "no direction means no push");
        }

        [Test]
        public void TryPush_UsesTheTargetsCurrentPosition_NotAStaleOne()
        {
            // The inlined original re-resolved the target's position
            // right before pushing, precisely because earlier hits in a
            // multi-target power may have moved it. Pin that.
            var actor = Creature("actor", 5, 5);
            var target = Creature("target", 6, 5);
            _zone.MoveEntity(target, 8, 5);

            Assert.IsTrue(SkillCombatHelpers.TryPush(actor, target, _zone));
            Assert.AreEqual(9, _zone.GetEntityPosition(target).x,
                "pushed from where it is now, not from where it was");
        }

        // ── Multi-cell pushes (new capability the spells need) ───

        [Test]
        public void TryPush_MultipleCellsMovesTheFullDistance()
        {
            var actor = Creature("actor", 2, 5);
            var target = Creature("target", 3, 5);

            Assert.IsTrue(SkillCombatHelpers.TryPush(actor, target, _zone, cells: 3));
            Assert.AreEqual(6, _zone.GetEntityPosition(target).x);
        }

        [Test]
        public void TryPush_MultiCellStopsAtTheFirstObstacle_AndKeepsGroundGained()
        {
            // A shove into a wall should not be all-or-nothing: the
            // target slides as far as it can. Returning true here is
            // deliberate — it moved, just not the whole way.
            var actor = Creature("actor", 2, 5);
            var target = Creature("target", 3, 5);
            Wall(6, 5);

            Assert.IsTrue(SkillCombatHelpers.TryPush(actor, target, _zone, cells: 3),
                "partial movement still counts as a push");
            Assert.AreEqual(5, _zone.GetEntityPosition(target).x,
                "stopped one short of the wall, keeping the ground it gained");
        }

        [Test]
        public void TryPush_ZeroOrNegativeCellsIsANoOp()
        {
            var actor = Creature("actor", 5, 5);
            var target = Creature("target", 6, 5);

            Assert.IsFalse(SkillCombatHelpers.TryPush(actor, target, _zone, cells: 0));
            Assert.IsFalse(SkillCombatHelpers.TryPush(actor, target, _zone, cells: -2));
            Assert.AreEqual(6, _zone.GetEntityPosition(target).x);
        }
    }
}
