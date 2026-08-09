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

        // ── Pull (SM5: Undertow drags a back-line caster to you) ──

        [Test]
        public void TryPull_DragsTheTargetOneCellCloser()
        {
            var actor = Creature("actor", 5, 5);
            var target = Creature("target", 8, 5);

            bool moved = SkillCombatHelpers.TryPull(actor, target, _zone);

            Assert.IsTrue(moved);
            Assert.AreEqual(7, _zone.GetEntityPosition(target).x,
                "dragged one cell toward the caster");
        }

        [Test]
        public void TryPull_StopsAdjacent_NeverIntoTheCaster()
        {
            // The critical guard. A pull that kept going would end with
            // the target standing on top of the caster, which no other
            // movement in the game allows.
            var actor = Creature("actor", 5, 5);
            var target = Creature("target", 8, 5);

            Assert.IsTrue(SkillCombatHelpers.TryPull(actor, target, _zone, cells: 10));

            var pos = _zone.GetEntityPosition(target);
            Assert.AreEqual(6, pos.x, "it stops one cell short");
            Assert.AreEqual(5, pos.y);
            Assert.AreEqual(5, _zone.GetEntityPosition(actor).x,
                "and the caster has not been displaced");
        }

        [Test]
        public void TryPull_AlreadyAdjacentIsANoOp()
        {
            var actor = Creature("actor", 5, 5);
            var target = Creature("target", 6, 5);

            Assert.IsFalse(SkillCombatHelpers.TryPull(actor, target, _zone),
                "there is nowhere closer to drag them");
            Assert.AreEqual(6, _zone.GetEntityPosition(target).x);
        }

        [Test]
        public void TryPull_BlockedByAWallBetween()
        {
            var actor = Creature("actor", 5, 5);
            var target = Creature("target", 8, 5);
            Wall(7, 5);

            Assert.IsFalse(SkillCombatHelpers.TryPull(actor, target, _zone, cells: 3),
                "you cannot drag someone through stone");
            Assert.AreEqual(8, _zone.GetEntityPosition(target).x);
        }

        [Test]
        public void TryPull_BlockedByACreatureBetween()
        {
            var actor = Creature("actor", 5, 5);
            var target = Creature("target", 8, 5);
            Creature("interposed", 7, 5);

            Assert.IsFalse(SkillCombatHelpers.TryPull(actor, target, _zone, cells: 3),
                "a body in the way stops the drag");
        }

        [Test]
        public void TryPull_StopsAdjacentEvenWhenThePullerIsNotACreature()
        {
            // Makes the stop-adjacent guard load-bearing. With a CREATURE
            // puller the occupancy check already refuses the final step,
            // so mutation-testing found the guard unreachable. A puller
            // WITHOUT the Creature tag — a future whirlpool fixture or
            // tentacle prop — is the case the guard actually exists for.
            var fixture = new Entity { ID = "whirlpool", BlueprintName = "Whirlpool" };
            fixture.AddPart(new RenderPart { DisplayName = "whirlpool", RenderString = "o" });
            _zone.AddEntity(fixture, 5, 5);
            var target = Creature("target", 9, 5);

            Assert.IsTrue(SkillCombatHelpers.TryPull(fixture, target, _zone, cells: 10));

            var pos = _zone.GetEntityPosition(target);
            Assert.AreEqual(6, pos.x,
                "it stops beside the puller, not inside it");
            Assert.AreEqual(5, pos.y);
        }

        [Test]
        public void TryPull_PullsAlongDiagonals()
        {
            var actor = Creature("actor", 5, 5);
            var target = Creature("target", 8, 8);

            Assert.IsTrue(SkillCombatHelpers.TryPull(actor, target, _zone, cells: 2));
            var pos = _zone.GetEntityPosition(target);
            Assert.AreEqual(6, pos.x);
            Assert.AreEqual(6, pos.y);
        }

        [Test]
        public void TryPull_NullOrCoincidentIsGracefulFalse()
        {
            var actor = Creature("actor", 5, 5);
            var target = Creature("target", 8, 5);
            var onTop = Creature("onTop", 5, 5);

            Assert.IsFalse(SkillCombatHelpers.TryPull(null, target, _zone));
            Assert.IsFalse(SkillCombatHelpers.TryPull(actor, null, _zone));
            Assert.IsFalse(SkillCombatHelpers.TryPull(actor, target, null));
            Assert.IsFalse(SkillCombatHelpers.TryPull(actor, target, _zone, cells: 0));
            Assert.IsFalse(SkillCombatHelpers.TryPull(actor, onTop, _zone),
                "no direction means no pull");
        }

        [Test]
        public void TryPull_RunsTheMovementPipeline_LikePushDoes()
        {
            // Symmetry with TryPush: a drag is a move. If it skipped the
            // pipeline, a creature yanked into a pool would not get wet
            // and the renderer would never repaint.
            var actor = Creature("actor", 5, 5);
            var target = Creature("target", 8, 5);
            bool afterMove = false;
            target.AddPart(new PullWatcherPart(() => afterMove = true));

            Assert.IsTrue(SkillCombatHelpers.TryPull(actor, target, _zone));
            Assert.IsTrue(afterMove, "a pull must run the movement pipeline too");
        }

        private class PullWatcherPart : Part
        {
            public override string Name => "PullWatcher";
            private readonly System.Action _onMove;
            public PullWatcherPart(System.Action onMove) { _onMove = onMove; }
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "AfterMove") _onMove?.Invoke();
                return true;
            }
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
