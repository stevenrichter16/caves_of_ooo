using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Pins the contract that effects applied DURING the owner's own
    /// active turn (when <c>TurnManager.CurrentActor</c> is the owner)
    /// survive the apply turn — their first <c>OnTurnEnd</c> is skipped
    /// via <see cref="Effect.JustApplied"/>.
    ///
    /// Without this skip, mid-turn effect application paths
    /// (trap-stepping, self-targeted tonic, self-buff mutation) end up
    /// with their <c>Duration</c> ticked in the very <c>EndTurn</c>
    /// that wraps the triggering action. <c>StunnedEffect(1)</c> from
    /// a BearTrap becomes 0 turns of effective stun. <c>BleedingEffect</c>
    /// rolls its first save before the player has had a single turn of
    /// bleed. <c>BurningEffect(intensity=1.5)</c> loses one of its
    /// 5 turns.
    ///
    /// First-fix attempt (commit 8185e2b) used a per-part
    /// <c>_isOwnerActing</c> flag set by a <c>BeginTakeAction</c>
    /// listener. That failed in production: the player's
    /// <c>StatusEffectsPart</c> is lazy-created on FIRST effect
    /// application by <see cref="Entity.EnsureStatusEffectsPart"/>, so
    /// the very first effect (e.g., trap stun on a player who's never
    /// had any prior status applied) created a fresh part with
    /// <c>_isOwnerActing=false</c> — the BeginTakeAction event had
    /// fired earlier with no listener to flip the flag.
    ///
    /// Fix uses <c>TurnManager.Active.CurrentActor</c> directly: a
    /// canonical "who's acting" source that's correct regardless of
    /// part-creation order.
    ///
    /// Bug surfaced from playtest of feat/trap-furniture (the Stun
    /// effect from BearTrap appearing and disappearing in the same
    /// log block).
    /// </summary>
    public class EffectTickOnApplyTurnTests
    {
        [SetUp]
        public void Setup() => MessageLog.Clear();

        // =========================================================
        // Test helpers
        // =========================================================

        /// <summary>
        /// Build a creature with the bare-bones stats needed by the
        /// effects under test. Deliberately does NOT include
        /// <c>StatusEffectsPart</c> in the initial parts list — that's
        /// the production shape (Creature blueprint doesn't include it
        /// either; it's lazy-created by Entity.EnsureStatusEffectsPart
        /// on first ApplyEffect). The fix must work without the part
        /// existing at the time TurnManager fires BeginTakeAction.
        /// </summary>
        private static Entity CreatePlayer(int hp = 100)
        {
            var e = new Entity { BlueprintName = "TestPlayer" };
            e.Tags["Player"] = "";
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = hp, Max = hp, Owner = e };
            e.Statistics["HP"] = new Stat { Name = "HP", BaseValue = hp, Max = hp, Owner = e };
            e.Statistics["DV"] = new Stat { BaseValue = 4, Owner = e };
            e.Statistics["Toughness"] = new Stat { BaseValue = 10, Owner = e };
            e.Statistics["Agility"] = new Stat { BaseValue = 10, Owner = e };
            e.Statistics["Speed"] = new Stat { BaseValue = 100, Owner = e };
            e.AddPart(new RenderPart { DisplayName = "test creature" });
            return e;
        }

        /// <summary>
        /// Build a TurnManager with the entity registered, and run
        /// ProcessUntilPlayerTurn so CurrentActor == player and
        /// WaitingForInput == true. This is the production "player ready
        /// to take action" state — the moment when input arrives,
        /// TryMove fires, traps fire effects.
        /// </summary>
        private static TurnManager BeginPlayerTurn(Entity player)
        {
            var tm = new TurnManager();
            tm.AddEntity(player);
            tm.ProcessUntilPlayerTurn();
            return tm;
        }

        // =========================================================
        // Core invariant: effects applied while owner is the
        // CurrentActor skip their first OnTurnEnd tick.
        // =========================================================

        [Test]
        public void EffectAppliedDuringOwnAction_SkipsFirstOnTurnEnd()
        {
            // Production-shape: TurnManager has CurrentActor == player.
            // ApplyEffect during this window must mark JustApplied=true.
            var player = CreatePlayer();
            var tm = BeginPlayerTurn(player);
            Assert.AreSame(player, tm.CurrentActor,
                "Sanity: ProcessUntilPlayerTurn must leave CurrentActor == player.");

            var stun = new StunnedEffect(duration: 1);
            player.ApplyEffect(stun);

            Assert.IsTrue(stun.JustApplied,
                "ApplyEffect during the player's active turn must set JustApplied=true. " +
                "If this fails, the TurnManager.Active / CurrentActor handoff is broken.");

            tm.EndTurn(player);

            Assert.IsTrue(player.HasEffect<StunnedEffect>(),
                "Stun applied mid-action must survive the apply turn's EndTurn.");
            Assert.AreEqual(1, stun.Duration,
                "Duration must NOT decrement on the apply turn's EndTurn.");
            Assert.IsFalse(stun.JustApplied,
                "JustApplied must clear after the first skip.");
        }

        [Test]
        public void EffectAppliedDuringOwnAction_NextEndTurnTicksNormally()
        {
            // Verify the skip is single-shot.
            var player = CreatePlayer();
            var tm = BeginPlayerTurn(player);

            var stun = new StunnedEffect(duration: 1);
            player.ApplyEffect(stun);

            tm.EndTurn(player);   // skipped

            Assert.IsTrue(player.HasEffect<StunnedEffect>(),
                "After apply turn's EndTurn, stun must still be active.");

            // Next turn — TurnManager picks player again, BeginTakeAction
            // is blocked by stun, EndTurn fires, Duration ticks 1 -> 0.
            tm.ProcessUntilPlayerTurn();

            Assert.IsFalse(player.HasEffect<StunnedEffect>(),
                "Second turn's EndTurn must tick the stun normally and remove it.");
        }

        [Test]
        public void EffectAppliedWhileOwnerNotCurrentActor_TicksOnFirstEndTurn()
        {
            // Counter-check: when CurrentActor is null or != owner,
            // JustApplied stays false. Mirrors the on-hit-melee shape:
            // defender's CurrentActor != defender (it's the attacker).
            var defender = CreatePlayer();
            // No TurnManager / CurrentActor set up — TurnManager.Active
            // may be null or set to an irrelevant instance.
            // Either way, CurrentActor == defender is false.
            new TurnManager();   // Active set, CurrentActor == null

            var stun = new StunnedEffect(duration: 2);
            defender.ApplyEffect(stun);

            Assert.IsFalse(stun.JustApplied,
                "Effect applied while owner is NOT CurrentActor must have JustApplied=false. " +
                "Otherwise on-hit-melee Stun would last one extra turn on every hit.");

            defender.FireEvent(GameEvent.New("EndTurn"));

            Assert.AreEqual(1, stun.Duration,
                "EndTurn must tick the effect normally when JustApplied=false.");
        }

        // =========================================================
        // Each trap effect: the user-visible bug being fixed.
        // =========================================================

        [Test]
        public void StunDuration1_FromTrap_BlocksExactlyOneNextTurn()
        {
            // The exact shape of the user's bug report.
            var player = CreatePlayer();
            var tm = BeginPlayerTurn(player);

            // Mid-action apply (trap-step shape)
            player.ApplyEffect(new StunnedEffect(duration: 1));
            tm.EndTurn(player);   // EndTurn — JustApplied=true → skip

            Assert.IsTrue(player.HasEffect<StunnedEffect>(),
                "Stun(1) from trap must survive the apply turn.");

            // Turn N+1: ProcessUntilPlayerTurn picks player again.
            // BeginTakeAction is blocked by stun → playerAlreadyBlocked
            // → EndTurn fires, Duration ticks 1 -> 0 -> cleanup.
            tm.ProcessUntilPlayerTurn();

            Assert.IsFalse(player.HasEffect<StunnedEffect>(),
                "After turn N+1's blocked turn, the stun must have ticked 1 -> 0 and been removed.");
        }

        [Test]
        public void BleedingFromTrap_SurvivesAtLeastOneFullTurn()
        {
            // BleedingEffect.OnTurnEnd does the save-against-cure roll.
            // Without the fix, a bleed applied mid-action would roll its
            // first save in the EndTurn of the apply turn — possibly
            // ending the bleed before the player took a single tick.
            var rng = new Random(0);
            var player = CreatePlayer();
            var tm = BeginPlayerTurn(player);

            // High SaveTarget so any non-skipped save would virtually
            // always still be passable; skip means the save isn't even
            // rolled this turn.
            var bleed = new BleedingEffect(saveTarget: 100, damageDice: "1d2", rng: rng);
            player.ApplyEffect(bleed);
            int saveTargetBefore = bleed.SaveTarget;

            tm.EndTurn(player);   // skipped — no save roll, no SaveTarget--

            Assert.IsTrue(player.HasEffect<BleedingEffect>(),
                "Bleeding from a trap must survive the apply turn.");
            Assert.AreEqual(saveTargetBefore, bleed.SaveTarget,
                "SaveTarget must NOT decrement on the apply turn (the OnTurnEnd save logic was skipped).");
        }

        [Test]
        public void BurningFromFireTrap_FullDurationPreserved()
        {
            // FireTrap applies BurningEffect(intensity=1.5). OnApply (no
            // FuelPart on the test creature) sets Duration = ceil(1.5*3) = 5.
            var player = CreatePlayer();
            var tm = BeginPlayerTurn(player);

            var burn = new BurningEffect(intensity: 1.5f, rng: new Random(1));
            player.ApplyEffect(burn);
            Assert.AreEqual(5, burn.Duration,
                "Sanity: BurningEffect(1.5) without FuelPart sets Duration=ceil(1.5*3)=5.");

            tm.EndTurn(player);   // skipped

            Assert.AreEqual(5, burn.Duration,
                "Apply turn's EndTurn must NOT decrement Burning's Duration.");
        }

        // =========================================================
        // Counter-checks: defender (other-applied) effect path.
        // =========================================================

        [Test]
        public void OnHitOnDefender_DoesNotSkipDefenderTick()
        {
            // When attacker is the CurrentActor and applies an effect
            // to a defender, the defender's effect must NOT have
            // JustApplied set — they're not the currently-acting entity.
            // Otherwise on-hit Stun(2) from a Mace would last 3 turns
            // instead of 2.
            var attacker = CreatePlayer();
            var defender = CreatePlayer();
            attacker.Tags.Remove("Player");

            var tm = new TurnManager();
            tm.AddEntity(attacker);
            tm.AddEntity(defender);
            tm.ProcessUntilPlayerTurn();   // CurrentActor = defender (the Player-tagged one)

            // Re-pick: CurrentActor must NOT be the attacker for our
            // counter-check. Force the scenario: clear CurrentActor and
            // set it to the attacker manually, mirroring "attacker's
            // turn, applying effect to defender".
            tm.EndTurn(defender);   // clears CurrentActor

            // Now simulate the attacker's mid-action ApplyEffect on the
            // defender. We can't easily set CurrentActor=attacker without
            // running ProcessUntilPlayerTurn, but the simpler counter-
            // check is: with CurrentActor != defender, JustApplied stays
            // false on a defender-side ApplyEffect.
            var stun = new StunnedEffect(duration: 2);
            defender.ApplyEffect(stun);

            Assert.IsFalse(stun.JustApplied,
                "Defender's on-hit effect must NOT have JustApplied set — " +
                "TurnManager.CurrentActor != defender at the moment of apply.");
        }

        [Test]
        public void EffectAppliedBetweenTurns_DoesNotSkip()
        {
            // After EndTurn, CurrentActor is null. Effects applied during
            // this between-turns window (rare; hypothetical script-driven
            // path) must NOT have JustApplied set.
            var player = CreatePlayer();
            var tm = BeginPlayerTurn(player);
            tm.EndTurn(player);

            Assert.IsNull(tm.CurrentActor,
                "Sanity: CurrentActor must be null between turns.");

            var stun = new StunnedEffect(duration: 2);
            player.ApplyEffect(stun);

            Assert.IsFalse(stun.JustApplied,
                "Effects applied while CurrentActor == null must have JustApplied=false.");
        }

        // =========================================================
        // PRODUCTION PATH INTEGRATION
        // =========================================================

        [Test]
        public void TurnManagerFlow_StunFromTrap_PersistsAcrossPlayerTurnEnd()
        {
            // Mirrors the EXACT production sequence that fires when the
            // player steps onto a BearTrap:
            //   1. TurnManager.ProcessUntilPlayerTurn fires BeginTakeAction
            //      → player WaitingForInput
            //   2. (Player presses key)
            //   3. MovementSystem.TryMove → trap → ApplyEffect on player
            //   4. EndTurnAndProcess → TurnManager.EndTurn(player)
            //
            // Critical: the player has NO StatusEffectsPart at step 1,
            // because the production Creature blueprint doesn't include
            // it. Step 3 lazy-creates the part. The fix must work
            // through this path.
            var player = CreatePlayer();
            // No StatusEffectsPart added — exercises the lazy-create
            // path that the previous fix attempt failed to handle.
            Assert.IsNull(player.GetPart<StatusEffectsPart>(),
                "Sanity: player must NOT have StatusEffectsPart yet — " +
                "this is the production shape that broke the previous fix.");

            var tm = new TurnManager();
            tm.AddEntity(player);
            var actor = tm.ProcessUntilPlayerTurn();
            Assert.AreSame(player, actor);
            Assert.IsTrue(tm.WaitingForInput);
            Assert.AreSame(player, tm.CurrentActor);

            // Step 3: trap-step apply (also auto-creates StatusEffectsPart)
            var stun = new StunnedEffect(duration: 1);
            player.ApplyEffect(stun);

            Assert.IsTrue(stun.JustApplied,
                "JustApplied MUST be true even though StatusEffectsPart was " +
                "lazy-created milliseconds ago. The TurnManager.Active.CurrentActor " +
                "query is the canonical truth — it doesn't depend on the part " +
                "having existed when BeginTakeAction fired earlier.");

            // Step 4
            tm.EndTurn(player);

            Assert.IsTrue(player.HasEffect<StunnedEffect>(),
                "Stunned MUST survive the EndTurn that closes the move action.");
            Assert.AreEqual(1, stun.Duration,
                "Duration must NOT decrement on the apply turn's EndTurn.");
        }

        [Test]
        public void TurnManagerFlow_TrapStun_BlocksExactlyOnePlayerTurnAfter()
        {
            // Full multi-turn integration without manually adding
            // StatusEffectsPart — full production-path shape.
            var player = CreatePlayer();
            var tm = new TurnManager();
            tm.AddEntity(player);

            // Turn N: player ready for input.
            tm.ProcessUntilPlayerTurn();
            // Mid-action apply (trap-step shape)
            player.ApplyEffect(new StunnedEffect(duration: 1));
            tm.EndTurn(player);

            Assert.IsTrue(player.HasEffect<StunnedEffect>(),
                "Stun must survive turn N's EndTurn (the apply turn).");

            // Turn N+1: ProcessUntilPlayerTurn picks player.
            // Stun.AllowAction=false → BeginTakeAction blocked → EndTurn
            // fires, stun ticks 1 -> 0 -> cleanup.
            tm.ProcessUntilPlayerTurn();

            Assert.IsFalse(player.HasEffect<StunnedEffect>(),
                "After turn N+1, the stun must have been ticked and removed.");
        }

        // =========================================================
        // Pre-fix regression pin
        // =========================================================

        [Test]
        public void StunDuration1_AppliedMidAction_DoesNotEvaporateSameTurn()
        {
            // The smoking gun. Pre-fix, StunnedEffect(1) applied during
            // the owner's action would tick 1 -> 0 in the very EndTurn
            // that closes the action — effectively 0 turns of stun.
            var player = CreatePlayer();
            var tm = BeginPlayerTurn(player);
            player.ApplyEffect(new StunnedEffect(duration: 1));
            tm.EndTurn(player);

            Assert.IsTrue(player.HasEffect<StunnedEffect>(),
                "Stun(1) applied mid-action MUST survive the apply turn's EndTurn. " +
                "If this fails, the bug has regressed and BearTrap stuns are useless.");
        }
    }
}
