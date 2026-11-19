using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Extras-only multi-stack pooling (Docs/MACHINE-MULTI-STACK-CLAIMS.md, spec S12b, after the independent critique). The shipped serrated kit needs two spurgrass spines; when they
    /// are two carried stacks the row used to vanish. Legacy first and unchanged: pooling engages only where no single stack is sufficient. Each positive case has its counter.
    /// </summary>
    public sealed class MachineMultiStackClaimTests : FiftyWorldFixture
    {
        const string Spine = "SpurgrassSpine", Cord = "KnotflaxCord", Kit = "SerratedEdgeComponent";
        const string Prefix = "MaterialProcess|serrated_kit|";
        EntityFactory oldForge;
        [SetUp] public void SetupPool() { oldForge = ForgePart.Factory; ForgePart.Factory = Factory; MaterialProcessRegistry.ResetForTests(); }
        [TearDown] public void RestorePool() { ForgePart.Factory = oldForge; MaterialProcessRegistry.ResetForTests(); }

        sealed class IdleEffect : Effect { public IdleEffect() { Duration = DURATION_INDEFINITE; } public override string DisplayName => "idle"; }
        Entity Knife() => Carry("Dagger");
        /// <summary>A separate carried stack with a chosen ID and count (the real inventory would merge identical units, so this builds the split directly).</summary>
        Entity Separate(string id, int count = 1, string blueprint = Spine)
        {
            var e = Factory.CreateEntity(blueprint); e.ID = id; if (count > 1) e.GetPart<StackerPart>().StackCount = count;
            Pack.Objects.Add(e); e.GetPart<PhysicsPart>().InInventory = Actor; return e;
        }
        string Cmd(Entity cord) => Actions(cord).Single(a => a.Command.StartsWith(Prefix)).Command;
        bool Offered(Entity cord) => Actions(cord).Any(a => a.Command.StartsWith(Prefix));
        int Units(string id) => Pack.Objects.Where(e => e.ID == id).Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);
        string[] Layout() => Pack.Objects.Select(e => e.ID + ":" + (e.GetPart<StackerPart>()?.StackCount ?? 1)).ToArray();

        [Test] public void TwoSingleUnitStacksPoolAndTheRowAppearsAndCommitsConservingEveryUnit()
        {
            Knife(); var cord = Carry(Cord); Separate("a"); Separate("b");
            Assert.True(Offered(cord), "RED on HEAD: the row vanished when the spines were two stacks");
            Assert.True(Act(cord, Cmd(cord))); Assert.AreEqual(1, Count(Kit)); Assert.AreEqual(0, Count(Cord)); Assert.AreEqual(0, Count(Spine));
            Assert.False(Pack.Objects.Any(e => e.BlueprintName == Spine), "both emptied stacks are fully detached, never a zero-unit stack left carried");
        }

        [Test] public void OneStackOfTwoBehavesExactlyAsBefore_Parity()
        {
            Knife(); var cord = Carry(Cord); Separate("only", 2); Assert.True(Act(cord, Cmd(cord))); Assert.AreEqual(1, Count(Kit)); Assert.AreEqual(0, Count(Spine));
        }

        [Test] public void ALowIdInsufficientStackBesideAHighIdSufficientOneKeepsTheLegacyChoice_Parity()
        {
            Knife(); var cord = Carry(Cord); Separate("a", 1); Separate("b", 3);
            Assert.True(Act(cord, Cmd(cord))); Assert.AreEqual(1, Units("a"), "legacy spends two from the one stack that is sufficient on its own"); Assert.AreEqual(1, Units("b"));
        }

        [Test] public void PoolingSpendsTheLowestIdsFirstAmongEqualStacksAndLeavesTheRest()
        {
            Knife(); var cord = Carry(Cord); Separate("c"); Separate("a"); Separate("b");
            Assert.True(Act(cord, Cmd(cord))); Assert.AreEqual(0, Units("a")); Assert.AreEqual(0, Units("b")); Assert.AreEqual(1, Units("c")); Assert.AreEqual(1, Count(Kit));
        }

        [Test] public void OneSpineTotalIsNotEnoughAndNothingIsOffered_Counter()
        {
            Knife(); var cord = Carry(Cord); Separate("a"); Assert.False(Offered(cord)); Assert.AreEqual(1, Count(Spine));
        }

        [Test] public void AStackCarryingAnInertRecoveryMarkPoolsAndALiveOneNeverDoes()
        {
            Knife(); var cord = Carry(Cord); var a = Separate("a"); a.AddPart(new ThrownRecoveryPart()); Separate("b");
            Assert.True(Offered(cord), "an inert tracking part is not payload");
            a.GetPart<ThrownRecoveryPart>().ThrowerId = Actor.ID; Assert.False(Offered(cord), "a live claim is payload: never pooled");
            Assert.AreEqual(2, Count(Spine));
        }

        [Test] public void AnEffectTouchedStackNeverPools_NoStateLaundering()
        {
            Knife(); var cord = Carry(Cord); var a = Separate("a"); var effects = new StatusEffectsPart(); a.AddPart(effects); Assert.True(effects.ApplyEffect(new IdleEffect(), Actor)); Separate("b");
            Assert.False(Offered(cord)); Assert.AreEqual(2, Count(Spine));
        }

        [Test] public void ALiveClaimedSufficientStackIsIgnoredAndTheLegacyChoiceStillApplies_Parity()
        {
            // The live-claim stack is not admissible, so legacy already chose the other sufficient stack; pooling never touches the claimed one.
            Knife(); var cord = Carry(Cord); var live = Separate("x", 2); live.AddPart(new ThrownRecoveryPart { ThrowerId = Actor.ID }); Separate("y", 2);
            Assert.True(Offered(cord)); Assert.True(Act(cord, Cmd(cord))); Assert.AreEqual(2, Units("x")); Assert.AreEqual(0, Units("y")); Assert.NotNull(live.GetPart<ThrownRecoveryPart>());
        }

        [Test] public void OtherBlueprintsAreNeverMixedIn_Counter()
        {
            Knife(); var cord = Carry(Cord); Separate("a"); Separate("b", 1, "KnotflaxCord"); Assert.False(Offered(cord)); // one spine plus an unrelated stack
        }

        [Test] public void AnOuterFailureAfterTheCommandRestoresEveryStackAtItsOriginalPlace()
        {
            Knife(); var cord = Carry(Cord); Separate("a"); Separate("b"); Separate("c"); var before = Layout(); var command = Cmd(cord);
            FailAfter(); Assert.False(Act(cord, command)); CollectionAssert.AreEqual(before, Layout()); Assert.AreEqual(0, Count(Kit));
        }

        [Test] public void AStackRemovedBetweenQuoteAndCommitRefusesAndSpendsNothing()
        {
            Knife(); var cord = Carry(Cord); Separate("a"); var b = Separate("b"); var command = Cmd(cord); var before = Layout();
            Pack.RemoveObject(b); Assert.False(Act(cord, command)); Assert.AreEqual(0, Count(Kit)); Assert.AreEqual(1, Units("a")); Assert.AreEqual(1, Count(Cord));
            Assert.IsEmpty(before.Except(Layout()).Where(l => !l.StartsWith("b:")));
        }

        [Test] public void ANewLowerIdStackAfterTheQuoteIsNotATakeAndSurvives()
        {
            Knife(); var cord = Carry(Cord); Separate("m"); Separate("n"); var command = Cmd(cord); Separate("a");
            Assert.True(Act(cord, command)); Assert.AreEqual(1, Units("a"), "the quoted takes were m and n"); Assert.AreEqual(0, Units("m")); Assert.AreEqual(0, Units("n"));
        }

        [Test] public void EveryStackCountVectorMatchesTheLegacyOracleOrPools_DifferentialSweep()
        {
            // Legacy oracle: the lowest-ID stack with at least 2 units is spent (2 from it); otherwise pooling needs 2 units in total.
            for (int a = 0; a <= 3; a++) for (int b = 0; b <= 3; b++) for (int c = 0; c <= 3; c++)
            {
                foreach (var e in Pack.Objects.ToArray()) Pack.RemoveObject(e);
                Knife(); var cord = Carry(Cord); int[] counts = { a, b, c }; string[] ids = { "p", "q", "r" };
                for (int i = 0; i < 3; i++) if (counts[i] > 0) Separate(ids[i], counts[i]);
                int total = counts.Sum(); bool legacy = counts.Any(n => n >= 2), expectOffered = total >= 2;
                Assert.AreEqual(expectOffered, Offered(cord), a + "," + b + "," + c);
                if (!expectOffered) continue;
                Assert.True(Act(cord, Cmd(cord)), a + "," + b + "," + c);
                int[] after = ids.Select(Units).ToArray(); Assert.AreEqual(total - 2, after.Sum(), "exactly two units are spent: " + a + "," + b + "," + c);
                if (legacy) { int pick = Array.FindIndex(counts, n => n >= 2); for (int i = 0; i < 3; i++) Assert.AreEqual(i == pick ? counts[i] - 2 : counts[i], after[i], "legacy choice kept: " + a + "," + b + "," + c); }
            }
        }
    }
}
