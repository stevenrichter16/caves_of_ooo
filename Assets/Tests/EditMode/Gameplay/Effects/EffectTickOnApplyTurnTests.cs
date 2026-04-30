using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Pins the contract that effects applied DURING the owner's own
    /// active turn (between BeginTakeAction and EndTurn) survive the
    /// apply turn — their first OnTurnEnd is skipped via
    /// <see cref="Effect.JustApplied"/>.
    ///
    /// Without this skip, mid-turn effect application paths
    /// (trap-stepping, self-targeted tonic, self-buff mutation) end up
    /// with their Duration ticked in the very EndTurn that wraps the
    /// triggering action. <c>StunnedEffect(1)</c> from a BearTrap
    /// becomes 0 turns of effective stun. <c>BleedingEffect</c> rolls
    /// its first save before the player has had a single turn of bleed.
    /// <c>BurningEffect(intensity=1.5)</c> loses one of its 5 turns.
    ///
    /// Bug surfaced from playtest of feat/trap-furniture (the Stun
    /// effect from BearTrap appearing and disappearing in the same
    /// log block). Reportedly the same shape as a previously-known
    /// pattern with self-applied fire spells.
    /// </summary>
    public class EffectTickOnApplyTurnTests
    {
        [SetUp]
        public void Setup() => MessageLog.Clear();

        private Entity CreateCreature(int hp = 100)
        {
            var e = new Entity();
            e.BlueprintName = "TestCreature";
            e.Statistics["HP"] = new Stat { BaseValue = hp, Max = hp, Owner = e };
            e.Statistics["Hitpoints"] = new Stat { BaseValue = hp, Max = hp, Owner = e };
            e.Statistics["DV"] = new Stat { BaseValue = 4, Owner = e };
            e.Statistics["Toughness"] = new Stat { BaseValue = 10, Owner = e };
            e.Statistics["Agility"] = new Stat { BaseValue = 10, Owner = e };
            e.AddPart(new RenderPart { DisplayName = "test creature" });
            // StatusEffectsPart must be present BEFORE BeginTakeAction fires —
            // otherwise the part isn't there to flip _isOwnerActing=true on
            // the event, and the later ApplyEffect (which auto-creates the
            // part) sees _isOwnerActing=false (default on the fresh part).
            // In production this is never an issue because creature blueprints
            // include StatusEffectsPart from initialization. The test must
            // mirror that ordering.
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        // =========================================================
        // The core invariant: effects applied during the owner's own
        // currently-active turn skip their first OnTurnEnd tick.
        // =========================================================

        [Test]
        public void EffectAppliedDuringOwnAction_SkipsFirstOnTurnEnd()
        {
            // Simulate the "step on trap" flow: owner is mid-action
            // (BeginTakeAction has fired, EndTurn hasn't yet), then a
            // mid-action handler calls ApplyEffect on the owner.
            var e = CreateCreature();
            e.FireEvent(GameEvent.New("BeginTakeAction"));

            var stun = new StunnedEffect(duration: 1);
            e.ApplyEffect(stun);

            // First EndTurn — must NOT tick the just-applied effect.
            e.FireEvent(GameEvent.New("EndTurn"));

            Assert.IsTrue(e.HasEffect<StunnedEffect>(),
                "StunnedEffect(1) applied mid-action must survive the apply turn's EndTurn. " +
                "If this fails, the JustApplied skip is broken and trap stuns evaporate same turn.");
            Assert.AreEqual(1, stun.Duration,
                "Duration must NOT decrement on the apply turn's EndTurn.");
            Assert.IsFalse(stun.JustApplied,
                "JustApplied must clear after the first skip so the next EndTurn ticks normally.");
        }

        [Test]
        public void EffectAppliedDuringOwnAction_NextEndTurnTicksNormally()
        {
            // Verify the skip is single-shot: second EndTurn ticks normally.
            var e = CreateCreature();
            e.FireEvent(GameEvent.New("BeginTakeAction"));

            var stun = new StunnedEffect(duration: 1);
            e.ApplyEffect(stun);

            e.FireEvent(GameEvent.New("EndTurn"));   // skipped
            // Player's NEXT turn — fire BeginTakeAction (blocked by stun) + EndTurn
            e.FireEvent(GameEvent.New("BeginTakeAction"));
            e.FireEvent(GameEvent.New("EndTurn"));   // ticks: Duration 1 -> 0 -> cleanup

            Assert.IsFalse(e.HasEffect<StunnedEffect>(),
                "Second EndTurn must tick the stun normally and remove it.");
        }

        [Test]
        public void EffectAppliedWhileOwnerNotActing_TicksOnFirstEndTurn()
        {
            // Counter-check: when owner is NOT mid-action (no BeginTakeAction
            // before apply), JustApplied stays false and existing behavior
            // is preserved. This is the on-hit-melee shape: defender's
            // _isOwnerActing is false because attacker is the actor.
            var e = CreateCreature();

            var stun = new StunnedEffect(duration: 2);
            e.ApplyEffect(stun);

            Assert.IsFalse(stun.JustApplied,
                "Effect applied while owner is not mid-action must NOT have JustApplied set. " +
                "Otherwise on-hit-melee Stun would last one extra turn on every hit.");

            e.FireEvent(GameEvent.New("EndTurn"));

            Assert.AreEqual(1, stun.Duration,
                "EndTurn must tick the effect normally when JustApplied=false.");
        }

        // =========================================================
        // Each trap effect: the user-visible bug being fixed.
        // =========================================================

        [Test]
        public void StunDuration1_FromTrap_BlocksExactlyOneNextTurn()
        {
            // The exact shape of the user's bug report: BearTrap applies
            // StunnedEffect(1) to the player who stepped on it. Without
            // the fix, the stun evaporates same turn (0 effective stun).
            // With the fix, exactly one subsequent turn is blocked.
            var e = CreateCreature();
            e.FireEvent(GameEvent.New("BeginTakeAction"));   // player turn N
            e.ApplyEffect(new StunnedEffect(duration: 1));   // applied mid-action
            e.FireEvent(GameEvent.New("EndTurn"));            // EndTurn N — skip

            // Turn N+1: try to act. Stun must block.
            var beginN1 = GameEvent.New("BeginTakeAction");
            bool turnN1 = e.FireEvent(beginN1);
            Assert.IsFalse(turnN1, "Turn N+1 must be blocked by the stun.");
            Assert.IsTrue(beginN1.Handled);
            e.FireEvent(GameEvent.New("EndTurn"));            // EndTurn N+1 — tick 1->0 -> remove

            // Turn N+2: stun cleared, can act.
            var beginN2 = GameEvent.New("BeginTakeAction");
            bool turnN2 = e.FireEvent(beginN2);
            Assert.IsTrue(turnN2, "Turn N+2 must succeed after the stun has expired.");
        }

        [Test]
        public void BleedingFromTrap_SurvivesAtLeastOneFullTurn()
        {
            // BleedingEffect.OnTurnEnd does the save-against-cure roll.
            // Without the fix, a bleed applied mid-action would roll its
            // first save in the EndTurn of the apply turn — possibly
            // ending the bleed before the player took a single bleed
            // damage tick.
            var rng = new Random(0);  // deterministic seed
            var e = CreateCreature();
            e.FireEvent(GameEvent.New("BeginTakeAction"));
            // High SaveTarget so any non-skipped save would virtually
            // always still be passable after enough ticks; skip means
            // the save isn't even rolled this turn.
            var bleed = new BleedingEffect(saveTarget: 100, damageDice: "1d2", rng: rng);
            e.ApplyEffect(bleed);
            int saveTargetBefore = bleed.SaveTarget;

            e.FireEvent(GameEvent.New("EndTurn"));   // skipped — no save roll, no SaveTarget--

            Assert.IsTrue(e.HasEffect<BleedingEffect>(),
                "Bleeding from a trap must survive the apply turn.");
            Assert.AreEqual(saveTargetBefore, bleed.SaveTarget,
                "SaveTarget must NOT decrement on the apply turn (the OnTurnEnd save logic was skipped).");
        }

        [Test]
        public void BurningFromFireTrap_FullDurationPreserved()
        {
            // FireTrap applies BurningEffect(intensity=1.5). OnApply (no
            // FuelPart on the test creature) sets Duration = ceil(1.5*3) = 5.
            // Without the fix, the apply turn's EndTurn ticks 5 -> 4
            // immediately. With the fix, Duration stays 5 after the apply
            // turn and ticks 5 -> 4 on the next turn's EndTurn.
            var e = CreateCreature();
            e.FireEvent(GameEvent.New("BeginTakeAction"));

            var burn = new BurningEffect(intensity: 1.5f, rng: new Random(1));
            e.ApplyEffect(burn);
            int durationAfterApply = burn.Duration;
            Assert.AreEqual(5, durationAfterApply,
                "Sanity: BurningEffect(1.5) without FuelPart sets Duration=ceil(1.5*3)=5.");

            e.FireEvent(GameEvent.New("EndTurn"));   // skipped

            Assert.AreEqual(5, burn.Duration,
                "Apply turn's EndTurn must NOT decrement Burning's Duration.");

            // Next turn: tick normally.
            e.FireEvent(GameEvent.New("BeginTakeAction"));
            e.FireEvent(GameEvent.New("EndTurn"));

            Assert.AreEqual(4, burn.Duration,
                "Second EndTurn must tick Burning's Duration normally (5 -> 4).");
        }

        // =========================================================
        // Counter-check: defender (other-applied) effect path.
        // =========================================================

        [Test]
        public void OnHitOnDefender_DoesNotSkipDefenderTick()
        {
            // The on-hit-melee shape: attacker is acting, defender is
            // not. Effect on defender must NOT have JustApplied set,
            // so existing on-hit Stun(2) from a Mace lands and ticks
            // exactly as before.
            var defender = CreateCreature();
            // No BeginTakeAction on defender — they're not the actor.

            var stun = new StunnedEffect(duration: 2);
            defender.ApplyEffect(stun);

            Assert.IsFalse(stun.JustApplied,
                "Defender's on-hit effect must NOT have JustApplied set — " +
                "they're not the currently-acting entity. Otherwise on-hit " +
                "Stun(2) from a melee swing would last 3 turns instead of 2.");

            // Defender's first turn: blocked by stun. Tick normally.
            defender.FireEvent(GameEvent.New("BeginTakeAction"));
            defender.FireEvent(GameEvent.New("EndTurn"));
            Assert.AreEqual(1, stun.Duration,
                "Defender's first EndTurn must tick Duration 2 -> 1.");

            // Second turn: still stunned, then expires.
            defender.FireEvent(GameEvent.New("BeginTakeAction"));
            defender.FireEvent(GameEvent.New("EndTurn"));
            Assert.IsFalse(defender.HasEffect<StunnedEffect>(),
                "On-hit Stun(2) must end on the defender's second EndTurn — total 2 turns of stun.");
        }

        [Test]
        public void IsOwnerActing_ClearsAfterEndTurn()
        {
            // Pins the lifecycle: _isOwnerActing must clear after EndTurn
            // so subsequent applies (between turns, e.g. spells targeting
            // this entity from another's turn) don't get JustApplied=true.
            var e = CreateCreature();
            e.FireEvent(GameEvent.New("BeginTakeAction"));
            e.FireEvent(GameEvent.New("EndTurn"));

            // Now between turns. Apply an effect from "outside" — should
            // NOT have JustApplied set.
            var stun = new StunnedEffect(duration: 2);
            e.ApplyEffect(stun);

            Assert.IsFalse(stun.JustApplied,
                "After the owner's EndTurn, _isOwnerActing must clear. " +
                "Effects applied between turns must NOT have JustApplied set.");
        }

        // =========================================================
        // Explicit pre-fix regression pin: this is the old behavior
        // that the user reported as a bug.
        // =========================================================

        [Test]
        public void StunDuration1_AppliedMidAction_DoesNotEvaporateSameTurn()
        {
            // This is the smoking gun. Pre-fix, StunnedEffect(1) applied
            // during the owner's action would tick 1 -> 0 in the very
            // EndTurn that closes the action — effectively 0 turns of stun.
            // Post-fix, the stun must persist past the apply EndTurn.
            var e = CreateCreature();
            e.FireEvent(GameEvent.New("BeginTakeAction"));
            e.ApplyEffect(new StunnedEffect(duration: 1));
            e.FireEvent(GameEvent.New("EndTurn"));

            Assert.IsTrue(e.HasEffect<StunnedEffect>(),
                "Stun(1) applied mid-action MUST survive the apply turn's EndTurn. " +
                "If this asserts fails, the bug has regressed and BearTrap stuns are useless.");
        }
    }
}
