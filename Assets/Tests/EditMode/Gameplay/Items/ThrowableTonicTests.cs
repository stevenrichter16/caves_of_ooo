using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Scenarios;
using CavesOfOoo.Tests.TestSupport;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Throwable consumables — tonics shatter on impact and apply
    /// their effect in a 3×3 AOE around the landing cell. The
    /// existing direct-hit path (`ThrowItemCommand.TryApplyThrownTonic`)
    /// is upgraded to:
    ///   - AOE radius 1 around impact cell (was single-target)
    ///   - Always shatter (miss path + wall-hit path also shatter,
    ///     not just direct creature hits)
    ///   - Item never re-enters the zone after shatter (no ground
    ///     drop)
    ///
    /// User-visible invariant: throwing a tonic = battlefield-control
    /// move. Drinking = self-buff. Symmetry between drink/throw is
    /// the design intent.
    ///
    /// These tests sit on top of the elemental tonics shipped in
    /// `feat/elemental-tonics` (AcidTonic, LightningTonic, FrostTonic,
    /// WaterTonic) — they're the most testable payloads because each
    /// applies a distinct StatusEffect class we can pin.
    /// </summary>
    public class ThrowableTonicTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetup() => _harness = new ScenarioTestHarness();
        [OneTimeTearDown]
        public void OneTimeTearDown() { _harness?.Dispose(); _harness = null; }

        [SetUp]
        public void Setup() => MessageLog.Clear();

        // ====================================================================
        // 1. Direct-hit AOE: adjacent creature also gets the effect
        // ====================================================================

        [Test]
        public void ThrownTonic_DirectHit_AdjacentCreatureAlsoReceivesEffect()
        {
            // Layout: thrower at (10,10), primary target snapjaw at (12,10),
            // adjacent snapjaw at (12,11). Throw an AcidTonic at (12,10).
            // BOTH snapjaws should end up with AcidicEffect (3×3 AOE).
            var ctx = _harness.CreateContext();
            var thrower = SetupThrowerWithTonic(ctx, "AcidTonic", out _);
            var primary = ctx.Spawn("Snapjaw").NotRegisteredForTurns().At(12, 10);
            var adjacent = ctx.Spawn("Snapjaw").NotRegisteredForTurns().At(12, 11);

            ExecuteThrow(thrower, ctx, 12, 10);

            Assert.IsTrue(primary.GetPart<StatusEffectsPart>().HasEffect<AcidicEffect>(),
                "Primary hit creature should have AcidicEffect.");
            Assert.IsTrue(adjacent.GetPart<StatusEffectsPart>().HasEffect<AcidicEffect>(),
                "Creature adjacent to impact should also have AcidicEffect (radius-1 AOE).");
        }

        // ====================================================================
        // 2. Counter-check: creature outside radius does NOT receive the effect
        // ====================================================================

        [Test]
        public void ThrownTonic_DirectHit_CreatureOutsideRadius_DoesNotReceiveEffect()
        {
            // Snapjaw at (15,10) is 3 cells from impact (12,10) — Chebyshev
            // distance 3, outside radius 1.
            var ctx = _harness.CreateContext();
            var thrower = SetupThrowerWithTonic(ctx, "AcidTonic", out _);
            ctx.Spawn("Snapjaw").NotRegisteredForTurns().At(12, 10); // primary
            var farAway = ctx.Spawn("Snapjaw").NotRegisteredForTurns().At(15, 10);

            ExecuteThrow(thrower, ctx, 12, 10);

            var farEffects = farAway.GetPart<StatusEffectsPart>();
            // Far creature may or may not have a StatusEffectsPart at all
            // (lazily created by ApplyEffect). Either way, no AcidicEffect.
            if (farEffects != null)
                Assert.IsFalse(farEffects.HasEffect<AcidicEffect>(),
                    "Creature 3 cells from impact must NOT receive radius-1 AOE.");
        }

        // ====================================================================
        // 3. Miss to empty cell — tonic shatters; item is NOT in zone after
        // ====================================================================

        [Test]
        public void ThrownTonic_MissToEmptyCell_ItemConsumed_NotOnGround()
        {
            // Throw at (15,10) with no creatures in the 3×3 around it.
            // Tonic should shatter; the bottle should not appear at (15,10)
            // or anywhere else in the zone.
            var ctx = _harness.CreateContext();
            var thrower = SetupThrowerWithTonic(ctx, "AcidTonic", out var tonic);

            ExecuteThrow(thrower, ctx, 15, 10);

            Assert.IsNull(ctx.Zone.GetEntityCell(tonic),
                "Shattered tonic must not remain in the zone.");
        }

        // ====================================================================
        // 4. Miss with creature in landing cell — that creature gets effect
        // ====================================================================

        [Test]
        public void ThrownTonic_MissCellWithCreature_AppliesEffect()
        {
            // The "miss" path triggers when LineTargeting returns no
            // HitEntity but reaches a non-blocked cell. If a creature
            // happens to be there (e.g. the trace didn't intersect it
            // because it was added between trace + apply, or AOE),
            // it should still receive the effect.
            //
            // Practical setup: spawn snapjaw OFF the throw line, then
            // throw at a cell adjacent to the snapjaw so it's in AOE
            // radius 1 of an empty landing cell.
            var ctx = _harness.CreateContext();
            var thrower = SetupThrowerWithTonic(ctx, "AcidTonic", out _);
            var nearby = ctx.Spawn("Snapjaw").NotRegisteredForTurns().At(15, 11);

            // Throw at (15, 10) — empty cell adjacent to the snapjaw.
            ExecuteThrow(thrower, ctx, 15, 10);

            Assert.IsTrue(nearby.GetPart<StatusEffectsPart>().HasEffect<AcidicEffect>(),
                "Creature in 3×3 around miss cell should receive AOE effect.");
        }

        // ====================================================================
        // 5. Miss to fully empty 3×3 — logs informative message, still shatters
        // ====================================================================

        [Test]
        public void ThrownTonic_MissToEmptyArea_LogsShatter_AndStillConsumes()
        {
            var ctx = _harness.CreateContext();
            var thrower = SetupThrowerWithTonic(ctx, "AcidTonic", out var tonic);

            ExecuteThrow(thrower, ctx, 15, 10);

            // Exact wording is implementation detail; assert the message
            // mentions shatter and the tonic name.
            var messages = MessageLog.GetAllEntries();
            bool hasShatter = false;
            for (int i = 0; i < messages.Count; i++)
            {
                if (messages[i].Text.IndexOf("shatter", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    hasShatter = true;
                    break;
                }
            }
            Assert.IsTrue(hasShatter,
                "Empty-area shatter should produce a 'shatters' log line for player feedback.");

            Assert.IsNull(ctx.Zone.GetEntityCell(tonic),
                "Tonic must still be consumed even if AOE hit nothing.");
        }

        // ====================================================================
        // 6. Counter-check: NON-tonic items still ground-land on miss
        // ====================================================================

        [Test]
        public void ThrownNonTonicItem_MissesEmptyCell_LandsOnGroundUnchanged()
        {
            // Regression guard: only tonics shatter. A thrown stone / rock /
            // weapon should land on the ground exactly as before.
            var ctx = _harness.CreateContext();
            var thrower = ctx.Spawn("Villager").NotRegisteredForTurns().At(10, 10);
            var inv = thrower.GetPart<InventoryPart>() ?? new InventoryPart { MaxWeight = 50 };
            if (thrower.GetPart<InventoryPart>() == null) thrower.AddPart(inv);

            // Use a Bone — known throwable per existing AIBehaviorPartTests.
            var bone = new Entity { BlueprintName = "TestBone" };
            bone.AddPart(new RenderPart { DisplayName = "test bone" });
            bone.AddPart(new PhysicsPart { Takeable = true, Weight = 1 });
            bone.AddPart(new HandlingPart { Carryable = true, Throwable = true, Weight = 1 });
            inv.AddObject(bone);

            var throwCmd = new ThrowItemCommand(bone, 11, 10);
            var result = InventorySystem.ExecuteCommand(throwCmd, thrower, ctx.Zone);
            Assert.IsTrue(result.Success, "Throw bone should succeed: " + result.ErrorMessage);

            Assert.IsNotNull(ctx.Zone.GetEntityCell(bone),
                "Non-tonic items must still land on the ground after being thrown — only tonics shatter.");
        }

        // ====================================================================
        // 7. Multiple creatures in AOE — all receive the effect
        // ====================================================================

        [Test]
        public void ThrownTonic_AoeHits_AllCreaturesInRadius()
        {
            // Five snapjaws clustered in a 3×3 around the target cell —
            // every one should get the tonic's effect from a single throw.
            //
            // Layout note: positions deliberately avoid the trace path
            // (10,10)→(15,10). A snapjaw on that path would cause
            // LineTargeting to stop early, making the AOE center the
            // first creature instead of the aimed cell. The test pins
            // "AOE applies to all 5 within radius 1 of the impact cell"
            // — not "trace stops at first creature."
            var ctx = _harness.CreateContext();
            var thrower = SetupThrowerWithTonic(ctx, "FrostTonic", out _);

            // s1 is the aimed-at target; HitEntity will resolve to s1 and
            // ImpactCell to (15,10). All 5 snapjaws are within radius 1
            // of (15,10) and not on the throw trajectory.
            var s1 = ctx.Spawn("Snapjaw").NotRegisteredForTurns().At(15, 10);
            var s2 = ctx.Spawn("Snapjaw").NotRegisteredForTurns().At(16, 10);
            var s3 = ctx.Spawn("Snapjaw").NotRegisteredForTurns().At(15, 11);
            var s4 = ctx.Spawn("Snapjaw").NotRegisteredForTurns().At(16, 11);
            var s5 = ctx.Spawn("Snapjaw").NotRegisteredForTurns().At(14, 11);

            ExecuteThrow(thrower, ctx, 15, 10);

            Assert.IsTrue(s1.GetPart<StatusEffectsPart>().HasEffect<FrozenEffect>(), "s1 (target) hit");
            Assert.IsTrue(s2.GetPart<StatusEffectsPart>().HasEffect<FrozenEffect>(), "s2 (right) hit");
            Assert.IsTrue(s3.GetPart<StatusEffectsPart>().HasEffect<FrozenEffect>(), "s3 (below-mid) hit");
            Assert.IsTrue(s4.GetPart<StatusEffectsPart>().HasEffect<FrozenEffect>(), "s4 (below-right) hit");
            Assert.IsTrue(s5.GetPart<StatusEffectsPart>().HasEffect<FrozenEffect>(), "s5 (below-left) hit");
        }

        // ====================================================================
        // 8. Friendly-fire: same-faction creature in AOE still gets effect
        // ====================================================================

        [Test]
        public void ThrownTonic_AoeHitsAllCreatures_RegardlessOfFaction()
        {
            // Documents the friendly-fire policy: AOE doesn't filter by
            // faction. Throwing a tonic in your own faction's midst hits
            // them too. (The "elemental tonic = battlefield trade-off"
            // design intent.)
            var ctx = _harness.CreateContext();
            var thrower = SetupThrowerWithTonic(ctx, "WaterTonic", out _);
            var ally = ctx.Spawn("Villager").NotRegisteredForTurns().At(15, 10);
            var enemy = ctx.Spawn("Snapjaw").NotRegisteredForTurns().At(15, 11);

            ExecuteThrow(thrower, ctx, 15, 10);

            Assert.IsTrue(ally.GetPart<StatusEffectsPart>().HasEffect<WetEffect>(),
                "Friendly creature in AOE receives effect (friendly-fire policy).");
            Assert.IsTrue(enemy.GetPart<StatusEffectsPart>().HasEffect<WetEffect>(),
                "Hostile creature in AOE receives effect.");
        }

        // ====================================================================
        // 9. Tonic stack: throw one, stack decrements; remainder not consumed
        // ====================================================================

        [Test]
        public void ThrownTonic_StackOfTonics_StackSurvives_ButShrinks()
        {
            // Regression guard that the shatter path doesn't accidentally
            // destroy the entire stack when only one was thrown. The exact
            // decrement semantics live in StackerPart.RemoveOne and are
            // tested separately in StackerPart unit tests; this test pins
            // the user-visible invariant: throw a tonic from a stack →
            // you still have some left, throw didn't nuke the whole stack.
            var ctx = _harness.CreateContext();
            var thrower = ctx.Spawn("Villager").NotRegisteredForTurns().At(10, 10);
            var inv = thrower.GetPart<InventoryPart>() ?? new InventoryPart { MaxWeight = 50 };
            if (thrower.GetPart<InventoryPart>() == null) thrower.AddPart(inv);

            var stack = _harness.Factory.CreateEntity("AcidTonic");
            stack.AddPart(new HandlingPart { Carryable = true, Throwable = true, Weight = 1 });
            // Tonic blueprints inherit a StackerPart from Item with default
            // StackCount=1. Bump the EXISTING stacker rather than adding a
            // second part — GetPart<StackerPart>() returns the first match
            // and a duplicate would be ignored on the throw path.
            stack.GetPart<StackerPart>().StackCount = 3;
            inv.AddObject(stack);

            ctx.Spawn("Snapjaw").NotRegisteredForTurns().At(12, 10);

            var throwCmd = new ThrowItemCommand(stack, 12, 10);
            var result = InventorySystem.ExecuteCommand(throwCmd, thrower, ctx.Zone);
            Assert.IsTrue(result.Success, "Throw should succeed: " + result.ErrorMessage);

            var stacker = stack.GetPart<StackerPart>();
            Assert.IsTrue(inv.Objects.Contains(stack),
                "Remaining stack must still be in the thrower's inventory after throwing one.");
            Assert.Greater(stacker.StackCount, 0,
                "At least one tonic must remain in the stack after a single throw.");
            Assert.Less(stacker.StackCount, 3,
                "Stack count must have decreased — at least one was thrown.");
        }

        // ====================================================================
        // 10. Wall hit: tonic shatters at last-traversable cell, item removed
        // ====================================================================

        [Test]
        public void ThrownTonic_HitsWall_ShattersAtLastTraversable_ItemNotOnGround()
        {
            // Place a wall blocking the throw. LineTargeting returns
            // BlockedBySolid + a LastTraversableCell. Tonic should shatter
            // there, item removed.
            var ctx = _harness.CreateContext();
            var thrower = SetupThrowerWithTonic(ctx, "AcidTonic", out var tonic);

            // Wall at (12,10) blocks the throw past it.
            var wall = ctx.Spawn("Wall").NotRegisteredForTurns().At(12, 10);

            // Throw past the wall.
            ExecuteThrow(thrower, ctx, 14, 10);

            Assert.IsNull(ctx.Zone.GetEntityCell(tonic),
                "Tonic should be consumed even when a wall blocks the throw.");
        }

        // ====================================================================
        // 11. Counter-check: non-tonic into wall still lands at last traversable
        // ====================================================================

        [Test]
        public void ThrownNonTonicItem_HitsWall_LandsAtLastTraversable_NotConsumed()
        {
            var ctx = _harness.CreateContext();
            var thrower = ctx.Spawn("Villager").NotRegisteredForTurns().At(10, 10);
            var inv = thrower.GetPart<InventoryPart>() ?? new InventoryPart { MaxWeight = 50 };
            if (thrower.GetPart<InventoryPart>() == null) thrower.AddPart(inv);

            var bone = new Entity { BlueprintName = "TestBone" };
            bone.AddPart(new RenderPart { DisplayName = "test bone" });
            bone.AddPart(new PhysicsPart { Takeable = true, Weight = 1 });
            bone.AddPart(new HandlingPart { Carryable = true, Throwable = true, Weight = 1 });
            inv.AddObject(bone);

            ctx.Spawn("Wall").NotRegisteredForTurns().At(12, 10);

            var throwCmd = new ThrowItemCommand(bone, 14, 10);
            var result = InventorySystem.ExecuteCommand(throwCmd, thrower, ctx.Zone);
            Assert.IsTrue(result.Success);

            Assert.IsNotNull(ctx.Zone.GetEntityCell(bone),
                "Non-tonic item should still land on the ground when blocked by a wall.");
        }

        // ====================================================================
        // 12. Direct hit consumption: tonic not on ground, victim has effect
        // ====================================================================

        [Test]
        public void ThrownTonic_DirectHit_TonicConsumed_VictimReceivesEffect()
        {
            // Sanity guard for the direct-hit case after refactor: the
            // existing single-target behavior (TryApplyThrownTonic) must
            // still work end-to-end after the AOE upgrade.
            var ctx = _harness.CreateContext();
            var thrower = SetupThrowerWithTonic(ctx, "LightningTonic", out var tonic);
            var victim = ctx.Spawn("Snapjaw").NotRegisteredForTurns().At(12, 10);

            ExecuteThrow(thrower, ctx, 12, 10);

            Assert.IsTrue(victim.GetPart<StatusEffectsPart>().HasEffect<ElectrifiedEffect>(),
                "Direct-hit victim should still get the tonic's effect after the AOE refactor.");
            Assert.IsNull(ctx.Zone.GetEntityCell(tonic),
                "Direct-hit consumed tonic must not appear on the ground.");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private static Entity SetupThrowerWithTonic(
            ScenarioContext ctx,
            string tonicBlueprint,
            out Entity tonic)
        {
            var thrower = ctx.Spawn("Villager").NotRegisteredForTurns().At(10, 10);
            // Bump strength so range covers the test layout (~5 cells).
            var str = thrower.GetStat("Strength");
            if (str != null) str.BaseValue = 16;

            var inv = thrower.GetPart<InventoryPart>();
            if (inv == null)
            {
                inv = new InventoryPart { MaxWeight = 50 };
                thrower.AddPart(inv);
            }

            tonic = _harness.Factory.CreateEntity(tonicBlueprint);
            // HandlingService.CanThrow requires HandlingPart with Throwable=true.
            tonic.AddPart(new HandlingPart { Carryable = true, Throwable = true, Weight = 1 });
            inv.AddObject(tonic);
            return thrower;
        }

        private static InventoryCommandResult ExecuteThrow(
            Entity thrower,
            ScenarioContext ctx,
            int targetX,
            int targetY)
        {
            var inv = thrower.GetPart<InventoryPart>();
            // Find any throwable in inventory (the tonic we just added).
            Entity tonic = null;
            for (int i = 0; i < inv.Objects.Count; i++)
            {
                if (inv.Objects[i].GetPart<TonicPart>() != null)
                {
                    tonic = inv.Objects[i];
                    break;
                }
            }
            if (tonic == null) throw new InvalidOperationException("No tonic in thrower inventory");

            var cmd = new ThrowItemCommand(tonic, targetX, targetY);
            var result = InventorySystem.ExecuteCommand(cmd, thrower, ctx.Zone);
            Assert.IsTrue(result.Success, "Throw should succeed: " + result.ErrorMessage);
            return result;
        }
    }
}
