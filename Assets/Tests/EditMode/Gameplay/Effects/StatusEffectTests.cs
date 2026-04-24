using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    public class StatusEffectTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
        }

        private Entity CreateCreature(int hp = 100, int dv = 4, int toughness = 16, int agility = 16)
        {
            var e = new Entity();
            e.BlueprintName = "TestCreature";
            e.Statistics["HP"] = new Stat { BaseValue = hp, Max = hp, Owner = e };
            e.Statistics["Hitpoints"] = new Stat { BaseValue = hp, Max = hp, Owner = e };
            e.Statistics["DV"] = new Stat { BaseValue = dv, Owner = e };
            e.Statistics["Toughness"] = new Stat { BaseValue = toughness, Owner = e };
            e.Statistics["Agility"] = new Stat { BaseValue = agility, Owner = e };
            e.AddPart(new RenderPart { DisplayName = "test creature" });
            return e;
        }

        // ========================
        // Effect Base / StatusEffectsPart Lifecycle
        // ========================

        [Test]
        public void ApplyEffect_AddsEffectToEntity()
        {
            var e = CreateCreature();
            var poison = new PoisonedEffect(5, "1d3", new Random(42));

            bool result = e.ApplyEffect(poison);

            Assert.IsTrue(result);
            Assert.IsTrue(e.HasEffect<PoisonedEffect>());
        }

        [Test]
        public void ApplyEffect_AutoCreatesStatusEffectsPart()
        {
            var e = CreateCreature();
            Assert.IsNull(e.GetPart<StatusEffectsPart>());

            e.ApplyEffect(new StunnedEffect(2));

            Assert.IsNotNull(e.GetPart<StatusEffectsPart>());
        }

        [Test]
        public void ApplyEffect_BeforeApplyEventCanBlock()
        {
            var e = CreateCreature();
            var probe = new EffectEventProbePart { BlockBeforeApply = true };
            e.AddPart(probe);

            bool applied = e.ApplyEffect(new StunnedEffect(2));

            Assert.IsFalse(applied);
            Assert.IsFalse(e.HasEffect<StunnedEffect>());
        }

        [Test]
        public void ApplyEffect_FiresEffectAppliedEvent()
        {
            var e = CreateCreature();
            var probe = new EffectEventProbePart();
            e.AddPart(probe);

            bool applied = e.ApplyEffect(new StunnedEffect(2));

            Assert.IsTrue(applied);
            Assert.AreEqual(1, probe.AppliedCount);
            Assert.AreEqual(nameof(StunnedEffect), probe.LastAppliedEffectType);
        }

        [Test]
        public void RemoveEffect_RemovesAndCallsOnRemove()
        {
            var e = CreateCreature();
            e.ApplyEffect(new StunnedEffect(2));
            Assert.IsTrue(e.HasEffect<StunnedEffect>());

            bool removed = e.RemoveEffect<StunnedEffect>();

            Assert.IsTrue(removed);
            Assert.IsFalse(e.HasEffect<StunnedEffect>());
        }

        [Test]
        public void RemoveEffect_ReturnsFalseWhenNotPresent()
        {
            var e = CreateCreature();
            Assert.IsFalse(e.RemoveEffect<StunnedEffect>());
        }

        [Test]
        public void RemoveEffect_FiresEffectRemovedEvent()
        {
            var e = CreateCreature();
            var probe = new EffectEventProbePart();
            e.AddPart(probe);
            e.ApplyEffect(new StunnedEffect(2));

            bool removed = e.RemoveEffect<StunnedEffect>();

            Assert.IsTrue(removed);
            Assert.AreEqual(1, probe.RemovedCount);
            Assert.AreEqual(nameof(StunnedEffect), probe.LastRemovedEffectType);
        }

        [Test]
        public void GetEffect_ReturnsEffectInstance()
        {
            var e = CreateCreature();
            var poison = new PoisonedEffect(5, "1d3", new Random(42));
            e.ApplyEffect(poison);

            var retrieved = e.GetEffect<PoisonedEffect>();
            Assert.AreSame(poison, retrieved);
        }

        [Test]
        public void GetEffect_ReturnsNullWhenMissing()
        {
            var e = CreateCreature();
            Assert.IsNull(e.GetEffect<StunnedEffect>());
        }

        [Test]
        public void Effect_OwnerIsSetOnApply()
        {
            var e = CreateCreature();
            var poison = new PoisonedEffect(5);
            e.ApplyEffect(poison);

            Assert.AreSame(e, poison.Owner);
        }

        [Test]
        public void Effect_OwnerIsClearedOnRemove()
        {
            var e = CreateCreature();
            var poison = new PoisonedEffect(5);
            e.ApplyEffect(poison);
            e.RemoveEffect<PoisonedEffect>();

            Assert.IsNull(poison.Owner);
        }

        // ========================
        // Duration Ticking & Expiration
        // ========================

        [Test]
        public void Duration_DecrementsOnEndTurn()
        {
            var e = CreateCreature();
            var burn = new BurningEffect(intensity: 1.0f, rng: new Random(42));
            e.ApplyEffect(burn);

            e.FireEvent(GameEvent.New("EndTurn"));
            Assert.AreEqual(2, burn.Duration);

            e.FireEvent(GameEvent.New("EndTurn"));
            Assert.AreEqual(1, burn.Duration);
        }

        [Test]
        public void Duration_EffectRemovedAtZero()
        {
            var e = CreateCreature();
            var stun = new StunnedEffect(1);
            e.ApplyEffect(stun);

            Assert.IsTrue(e.HasEffect<StunnedEffect>());
            e.FireEvent(GameEvent.New("EndTurn")); // Duration goes 1 -> 0, then cleanup

            Assert.IsFalse(e.HasEffect<StunnedEffect>());
        }

        [Test]
        public void Duration_IndefiniteDoesNotExpire()
        {
            var e = CreateCreature();
            // Bleeding is indefinite — uses save-based recovery.
            // Use a very high save target and low toughness so it never passes.
            var bleed = new BleedingEffect(saveTarget: 100, damageDice: "1d2", rng: new Random(42));
            e.ApplyEffect(bleed);

            for (int i = 0; i < 10; i++)
                e.FireEvent(GameEvent.New("EndTurn"));

            // Should still be active (save target was too high to pass)
            Assert.IsTrue(e.HasEffect<BleedingEffect>());
        }

        // ========================
        // Stun / Paralysis — Turn Blocking
        // ========================

        [Test]
        public void Stunned_BlocksTurn()
        {
            var e = CreateCreature();
            e.ApplyEffect(new StunnedEffect(3));

            var takeTurn = GameEvent.New("TakeTurn");
            bool result = e.FireEvent(takeTurn);

            Assert.IsFalse(result, "TakeTurn should be blocked by stun");
            Assert.IsTrue(takeTurn.Handled);
        }

        [Test]
        public void Stunned_BlocksBeginTakeAction()
        {
            var e = CreateCreature();
            e.ApplyEffect(new StunnedEffect(3));

            var beginTakeAction = GameEvent.New("BeginTakeAction");
            bool result = e.FireEvent(beginTakeAction);

            Assert.IsFalse(result, "BeginTakeAction should be blocked by stun");
            Assert.IsTrue(beginTakeAction.Handled);
        }

        [Test]
        public void Paralyzed_BlocksTurn()
        {
            var e = CreateCreature();
            e.ApplyEffect(new ParalyzedEffect(3));

            var takeTurn = GameEvent.New("TakeTurn");
            bool result = e.FireEvent(takeTurn);

            Assert.IsFalse(result, "TakeTurn should be blocked by paralysis");
        }

        [Test]
        public void Frozen_BlocksBeginTakeAction_WhenColdAbove50()
        {
            // Reported gameplay bug: RuneOfFrost triggers, log shows
            // "X is frozen and cannot act!" but player can still move.
            // This test pins the contract: BeginTakeAction MUST return
            // false when FrozenEffect is active with Cold > 0.5. If it
            // returns true, the TurnManager will set WaitingForInput=true
            // and InputHandler will process movement.
            var e = CreateCreature();
            e.ApplyEffect(new FrozenEffect(cold: 1.0f));

            var beginTakeAction = GameEvent.New("BeginTakeAction");
            bool result = e.FireEvent(beginTakeAction);

            Assert.IsFalse(result,
                "BeginTakeAction must be blocked when frozen with Cold=1.0. If this test fails, the player can still move while the log says they're frozen.");
            Assert.IsTrue(beginTakeAction.Handled,
                "The event's Handled flag must be set so TurnManager's FireEvent short-circuits correctly.");
        }

        [Test]
        public void Frozen_BlocksAction_WhileAnyColdRemains()
        {
            // FrozenEffect.AllowAction blocks at ANY Cold > 0 (not the
            // old Cold > 0.5 half-threshold). This pins the semantic
            // that "effect present" === "frozen" — no partial-thaw
            // window where the log says frozen but the player can act.
            var e = CreateCreature();
            e.ApplyEffect(new FrozenEffect(cold: 0.4f));

            var beginTakeAction = GameEvent.New("BeginTakeAction");
            bool result = e.FireEvent(beginTakeAction);

            Assert.IsFalse(result,
                "BeginTakeAction must be blocked for any Cold > 0. If this flips " +
                "back to Assert.IsTrue, the partial-thaw UX bug has regressed.");
        }

        [Test]
        public void Frozen_ProcessUntilPlayerTurn_DoesNotCollapseEntireFreeze_InOneCall()
        {
            // Reported gameplay bug: stepping on a frost rune applies
            // FrozenEffect(Cold=1.0), but the very next input moves the
            // player — diagnostic logs showed 41 OnTurnEnd calls running
            // inside a single Update() frame through ProcessUntilPlayerTurn's
            // spin-until-player-can-act loop, collapsing the entire thaw
            // into zero real-world-play time.
            //
            // Fix: when the player's BeginTakeAction is blocked,
            // ProcessUntilPlayerTurn returns early (WaitingForInput=true)
            // instead of cycling the player through more skipped turns
            // in the same frame.
            //
            // This test pins the invariant by running ProcessUntilPlayerTurn
            // in a freshly-frozen scenario and counting OnTurnEnd calls: at
            // most ONE thaw from the player's EndTurn should fire in the
            // call — not 40.
            var player = CreateCreature();
            player.SetTag("Player");
            player.AddPart(new ThermalPart { Temperature = 25f, FreezeTemperature = 0f });

            // Install a probe effect that counts OnTurnEnd fires on the
            // player. Positioned BEFORE the FrozenEffect in the effect
            // list so it always runs (AllowAction=true on the probe
            // doesn't block the gate).
            var probe = new TurnEndCounterEffect();
            player.ApplyEffect(probe);
            player.ApplyEffect(new FrozenEffect(cold: 1.0f));

            var turnManager = new TurnManager();
            turnManager.AddEntity(player);

            // Give the player 1000 energy so BeginTakeAction fires
            // immediately on the first iteration.
            var tickMethod = typeof(TurnManager).GetMethod("Tick");
            for (int i = 0; i < 10; i++) turnManager.Tick();

            int probeBefore = probe.Count;
            turnManager.ProcessUntilPlayerTurn();

            int thawsThisCall = probe.Count - probeBefore;
            Assert.LessOrEqual(thawsThisCall, 1,
                $"ProcessUntilPlayerTurn fired OnTurnEnd {thawsThisCall} times on the frozen " +
                "player in a single call. Must be ≤ 1 — more means the freeze collapses to " +
                "one Unity frame regardless of its nominal duration (pre-fix was 41).");
        }

        /// <summary>Test-only effect that counts OnTurnEnd fires. Never blocks action.</summary>
        private class TurnEndCounterEffect : Effect
        {
            public override string DisplayName => "counter";
            public int Count;
            public TurnEndCounterEffect() { Duration = DURATION_INDEFINITE; }
            public override void OnTurnEnd(Entity target) { Count++; }
        }

        [Test]
        public void Frozen_AllowsAction_WhenFullyThawed()
        {
            // Sanity: once Cold == 0, the effect should allow action.
            // In practice OnTurnEnd sets Duration=0 the same tick this
            // becomes true, so the effect doesn't linger — but verify
            // the predicate itself.
            var frozen = new FrozenEffect(cold: 1.0f);
            frozen.Cold = 0f; // simulate full thaw
            Assert.IsTrue(frozen.AllowAction(null),
                "AllowAction must return true at Cold == 0 so the final thaw frame doesn't keep blocking.");
        }

        [Test]
        public void Frozen_AlsoBlocksMovement_ViaTryMoveEx()
        {
            // Actual player-input path in InputHandler.cs:460 uses TryMoveEx,
            // not TryMove. Mirror the exact call shape here.
            var zone = new Zone("TestZone");
            var frozen = CreateCreature();
            frozen.AddPart(new PhysicsPart { Solid = false });
            zone.AddEntity(frozen, 5, 5);
            frozen.ApplyEffect(new FrozenEffect(cold: 1.0f));

            var (moved, blockedBy) = MovementSystem.TryMoveEx(frozen, zone, dx: 1, dy: 0);

            Assert.IsFalse(moved,
                "TryMoveEx (the player-input path) must also block when frozen.");
            var cell = zone.GetEntityCell(frozen);
            Assert.AreEqual(5, cell.X);
            Assert.AreEqual(5, cell.Y);
        }

        [Test]
        public void Frozen_AlsoBlocksMovement_ViaBeforeMove()
        {
            // Reported gameplay bug: player steps on RuneOfFrost, gets
            // FrozenEffect, log says "frozen and cannot act" — but can
            // still move. Root cause: StatusEffectsPart only gates
            // BeginTakeAction. The player-input path goes through
            // MovementSystem.TryMoveEx, which fires BeforeMove — a
            // different event — and no part consults AllowAction there.
            //
            // Fix: StatusEffectsPart must also block BeforeMove when
            // any active effect returns AllowAction=false. Defense in
            // depth against any path that moves an entity without
            // first going through the turn-manager gate.
            var zone = new Zone("TestZone");
            var frozen = CreateCreature();
            frozen.AddPart(new PhysicsPart { Solid = false });
            zone.AddEntity(frozen, 5, 5);
            frozen.ApplyEffect(new FrozenEffect(cold: 1.0f));

            // Try to move the frozen entity. Without the fix, TryMove
            // returns true and the entity ends up at (6,5).
            bool moved = MovementSystem.TryMove(frozen, zone, dx: 1, dy: 0);

            Assert.IsFalse(moved,
                "Frozen entity (Cold=1.0) must not be able to move. MovementSystem.TryMove is the player-input entry point; if it returns true here, the player can move despite the frozen message.");
            var cell = zone.GetEntityCell(frozen);
            Assert.AreEqual(5, cell.X, "Entity must remain at starting X.");
            Assert.AreEqual(5, cell.Y, "Entity must remain at starting Y.");
        }

        [Test]
        public void Stunned_DoesNotBlockAfterExpired()
        {
            var e = CreateCreature();
            e.ApplyEffect(new StunnedEffect(1));

            // End turn to expire the stun
            e.FireEvent(GameEvent.New("EndTurn"));
            Assert.IsFalse(e.HasEffect<StunnedEffect>());

            // Next TakeTurn should go through
            var takeTurn = GameEvent.New("TakeTurn");
            bool result = e.FireEvent(takeTurn);
            Assert.IsTrue(result);
        }

        // ========================
        // Poison — Damage Per Turn
        // ========================

        [Test]
        public void Poison_DealsDamageOnTurnStart()
        {
            var e = CreateCreature(hp: 100);
            var rng = new Random(42);
            e.ApplyEffect(new PoisonedEffect(5, "1d3", rng));

            int hpBefore = e.GetStatValue("HP");
            e.FireEvent(GameEvent.New("TakeTurn"));
            int hpAfter = e.GetStatValue("HP");

            Assert.Less(hpAfter, hpBefore, "Poison should deal damage on turn start");
        }

        [Test]
        public void Poison_ExpiresAfterDuration()
        {
            var e = CreateCreature(hp: 1000);
            e.ApplyEffect(new PoisonedEffect(2, "1d3", new Random(42)));

            // Turn 1
            e.FireEvent(GameEvent.New("TakeTurn"));
            e.FireEvent(GameEvent.New("EndTurn"));
            Assert.IsTrue(e.HasEffect<PoisonedEffect>());

            // Turn 2
            e.FireEvent(GameEvent.New("TakeTurn"));
            e.FireEvent(GameEvent.New("EndTurn"));
            Assert.IsFalse(e.HasEffect<PoisonedEffect>(), "Poison should expire after 2 turns");
        }

        // ========================
        // Burning — Damage + Duration Reset Stacking
        // ========================

        [Test]
        public void Burning_DealsDamageOnTurnStart()
        {
            var e = CreateCreature(hp: 100);
            e.ApplyEffect(new BurningEffect(intensity: 1.0f, rng: new Random(42)));

            int hpBefore = e.GetStatValue("HP");
            e.FireEvent(GameEvent.New("TakeTurn"));
            int hpAfter = e.GetStatValue("HP");

            Assert.Less(hpAfter, hpBefore);
        }

        [Test]
        public void Burning_StackIncreasesIntensity()
        {
            var e = CreateCreature(hp: 1000);
            var burn = new BurningEffect(intensity: 1.0f, rng: new Random(42));
            e.ApplyEffect(burn);

            // Apply another burn — should increase intensity by incoming * 0.5
            e.ApplyEffect(new BurningEffect(intensity: 2.0f, rng: new Random(42)));
            Assert.AreEqual(2.0f, burn.Intensity, 0.01f, "Stacking should increase intensity");

            // Should still be same instance (stacked, not duplicated)
            var sep = e.GetPart<StatusEffectsPart>();
            int burnCount = 0;
            foreach (var eff in sep.GetAllEffects())
                if (eff is BurningEffect) burnCount++;
            Assert.AreEqual(1, burnCount, "Burning should stack (not duplicate)");
        }

        // ========================
        // Bleeding — Save-Based Recovery
        // ========================

        [Test]
        public void Bleeding_DealsDamageOnTurnStart()
        {
            var e = CreateCreature(hp: 100, toughness: 16);
            e.ApplyEffect(new BleedingEffect(100, "1d2", new Random(42))); // high save so it doesn't clear

            int hpBefore = e.GetStatValue("HP");
            e.FireEvent(GameEvent.New("TakeTurn"));
            int hpAfter = e.GetStatValue("HP");

            Assert.Less(hpAfter, hpBefore);
        }

        [Test]
        public void Bleeding_CanRecoverViaSave()
        {
            // Very low save target + high toughness = should recover quickly
            var e = CreateCreature(hp: 1000, toughness: 30);
            e.ApplyEffect(new BleedingEffect(saveTarget: 2, damageDice: "1d2", rng: new Random(42)));

            // Tick a few times — should eventually pass the save
            bool recovered = false;
            for (int i = 0; i < 20; i++)
            {
                e.FireEvent(GameEvent.New("TakeTurn"));
                e.FireEvent(GameEvent.New("EndTurn"));
                if (!e.HasEffect<BleedingEffect>())
                {
                    recovered = true;
                    break;
                }
            }

            Assert.IsTrue(recovered, "Bleeding should eventually be recovered via save");
        }

        [Test]
        public void Bleeding_SaveTargetDecreasesOverTime()
        {
            var e = CreateCreature(hp: 1000, toughness: 16);
            var bleed = new BleedingEffect(saveTarget: 100, damageDice: "1d2", rng: new Random(42));
            e.ApplyEffect(bleed);

            int initialSave = bleed.SaveTarget;
            e.FireEvent(GameEvent.New("EndTurn"));
            Assert.Less(bleed.SaveTarget, initialSave, "SaveTarget should decrease each turn");
        }

        [Test]
        public void Bleeding_StackUpgradesSaveTarget()
        {
            var e = CreateCreature(hp: 1000);
            var bleed1 = new BleedingEffect(saveTarget: 10, damageDice: "1d2", rng: new Random(42));
            e.ApplyEffect(bleed1);

            // Stack with a higher save target
            e.ApplyEffect(new BleedingEffect(saveTarget: 20, damageDice: "1d2", rng: new Random(42)));

            Assert.AreEqual(20, bleed1.SaveTarget, "Stacking should upgrade save target");
        }

        // ========================
        // Confused — No Stacking (CanApply Blocks)
        // ========================

        [Test]
        public void Confused_AppliesStatPenalties()
        {
            var e = CreateCreature(dv: 10, agility: 20);
            e.ApplyEffect(new ConfusedEffect(4));

            Assert.AreEqual(2, e.GetStat("DV").Penalty);
            Assert.AreEqual(2, e.GetStat("Agility").Penalty);
        }

        [Test]
        public void Confused_RestoresStatsOnRemove()
        {
            var e = CreateCreature(dv: 10, agility: 20);
            e.ApplyEffect(new ConfusedEffect(4));
            e.RemoveEffect<ConfusedEffect>();

            Assert.AreEqual(0, e.GetStat("DV").Penalty);
            Assert.AreEqual(0, e.GetStat("Agility").Penalty);
        }

        [Test]
        public void Confused_CannotStackDuplicate()
        {
            var e = CreateCreature();
            e.ApplyEffect(new ConfusedEffect(4));

            bool secondApply = e.ApplyEffect(new ConfusedEffect(4));
            Assert.IsFalse(secondApply, "Confusion should reject duplicate application");

            // Penalties should only be applied once
            Assert.AreEqual(2, e.GetStat("DV").Penalty);
        }

        // ========================
        // Stun/Paralysis — Stat Mods Apply/Restore
        // ========================

        [Test]
        public void Stunned_AppliesDVPenalty()
        {
            var e = CreateCreature(dv: 10);
            e.ApplyEffect(new StunnedEffect(2));

            Assert.AreEqual(4, e.GetStat("DV").Penalty);
        }

        [Test]
        public void Stunned_RestoresDVOnRemove()
        {
            var e = CreateCreature(dv: 10);
            e.ApplyEffect(new StunnedEffect(2));
            e.RemoveEffect<StunnedEffect>();

            Assert.AreEqual(0, e.GetStat("DV").Penalty);
        }

        [Test]
        public void Paralyzed_AppliesDVPenalty()
        {
            var e = CreateCreature(dv: 10);
            e.ApplyEffect(new ParalyzedEffect(2));

            Assert.AreEqual(6, e.GetStat("DV").Penalty);
        }

        [Test]
        public void Paralyzed_RestoresDVOnRemove()
        {
            var e = CreateCreature(dv: 10);
            e.ApplyEffect(new ParalyzedEffect(2));
            e.RemoveEffect<ParalyzedEffect>();

            Assert.AreEqual(0, e.GetStat("DV").Penalty);
        }

        // ========================
        // Stacking — Poison / Stun Extend Duration
        // ========================

        [Test]
        public void Poison_StackExtendsDuration()
        {
            var e = CreateCreature();
            var poison = new PoisonedEffect(5, "1d3", new Random(42));
            e.ApplyEffect(poison);

            e.ApplyEffect(new PoisonedEffect(3, "1d3", new Random(42)));

            Assert.AreEqual(8, poison.Duration, "Stacking poison should extend duration");

            // Should still only have one poison effect
            var sep = e.GetPart<StatusEffectsPart>();
            int count = 0;
            foreach (var eff in sep.GetAllEffects())
                if (eff is PoisonedEffect) count++;
            Assert.AreEqual(1, count);
        }

        [Test]
        public void Stunned_StackExtendsDuration()
        {
            var e = CreateCreature();
            var stun = new StunnedEffect(2);
            e.ApplyEffect(stun);

            e.ApplyEffect(new StunnedEffect(3));

            Assert.AreEqual(5, stun.Duration);
        }

        [Test]
        public void Paralyzed_StackExtendsDuration()
        {
            var e = CreateCreature();
            var para = new ParalyzedEffect(3);
            e.ApplyEffect(para);

            e.ApplyEffect(new ParalyzedEffect(2));

            Assert.AreEqual(5, para.Duration);
        }

        // ========================
        // Death Cleanup
        // ========================

        [Test]
        public void Death_RemovesAllEffects()
        {
            var e = CreateCreature();
            e.ApplyEffect(new PoisonedEffect(5, "1d3", new Random(42)));
            e.ApplyEffect(new StunnedEffect(2));
            e.ApplyEffect(new BurningEffect(intensity: 1.0f, rng: new Random(42)));

            e.FireEvent(GameEvent.New("Died"));

            var sep = e.GetPart<StatusEffectsPart>();
            Assert.AreEqual(0, sep.EffectCount, "All effects should be removed on death");
        }

        [Test]
        public void Death_RestoresStatMods()
        {
            var e = CreateCreature(dv: 10);
            e.ApplyEffect(new StunnedEffect(5));
            Assert.AreEqual(4, e.GetStat("DV").Penalty);

            e.FireEvent(GameEvent.New("Died"));

            Assert.AreEqual(0, e.GetStat("DV").Penalty, "DV penalty should be restored on death");
        }

        // ========================
        // Color Override
        // ========================

        [Test]
        public void Effect_ProvidesColorOverride()
        {
            var e = CreateCreature();
            e.ApplyEffect(new PoisonedEffect(5));

            var sep = e.GetPart<StatusEffectsPart>();
            Assert.AreEqual("&G", sep.GetRenderColorOverride());
        }

        [Test]
        public void Effect_FirstEffectColorWins()
        {
            var e = CreateCreature();
            e.ApplyEffect(new PoisonedEffect(5)); // &G
            e.ApplyEffect(new BurningEffect(intensity: 1.0f));   // &R

            var sep = e.GetPart<StatusEffectsPart>();
            Assert.AreEqual("&G", sep.GetRenderColorOverride(), "First effect's color should take priority");
        }

        [Test]
        public void Effect_NoColorOverrideWhenNoEffects()
        {
            var e = CreateCreature();
            e.AddPart(new StatusEffectsPart());

            var sep = e.GetPart<StatusEffectsPart>();
            Assert.IsNull(sep.GetRenderColorOverride());
        }

        // ========================
        // Multiple Effects
        // ========================

        [Test]
        public void MultipleEffects_CanCoexist()
        {
            var e = CreateCreature();
            e.ApplyEffect(new PoisonedEffect(5, "1d3", new Random(42)));
            e.ApplyEffect(new BurningEffect(intensity: 1.0f, rng: new Random(42)));
            e.ApplyEffect(new ConfusedEffect(4));

            Assert.IsTrue(e.HasEffect<PoisonedEffect>());
            Assert.IsTrue(e.HasEffect<BurningEffect>());
            Assert.IsTrue(e.HasEffect<ConfusedEffect>());

            var sep = e.GetPart<StatusEffectsPart>();
            Assert.AreEqual(3, sep.EffectCount);
        }

        [Test]
        public void MultipleEffects_AllTickOnEndTurn()
        {
            var e = CreateCreature(hp: 1000);
            var poison = new PoisonedEffect(3, "1d3", new Random(42));
            var burn = new BurningEffect(intensity: 1.0f, rng: new Random(42));
            e.ApplyEffect(poison);
            e.ApplyEffect(burn);

            // BurningEffect without FuelPart: Duration = ceil(1.0 * 3) = 3
            e.FireEvent(GameEvent.New("EndTurn"));

            Assert.AreEqual(2, poison.Duration);
            Assert.AreEqual(2, burn.Duration);
        }

        [Test]
        public void MultipleEffects_OnlyExpiredOnesRemoved()
        {
            var e = CreateCreature(hp: 1000);
            e.ApplyEffect(new PoisonedEffect(3, "1d3", new Random(42)));
            // Intensity 0.3 => Duration = ceil(0.3 * 3) = 1
            e.ApplyEffect(new BurningEffect(intensity: 0.3f, rng: new Random(42)));

            e.FireEvent(GameEvent.New("EndTurn")); // Burn expires (1->0), Poison ticks (3->2)

            Assert.IsTrue(e.HasEffect<PoisonedEffect>());
            Assert.IsFalse(e.HasEffect<BurningEffect>(), "Burning with duration 1 should expire after 1 EndTurn");
        }

        // ========================
        // Edge Cases
        // ========================

        [Test]
        public void ApplyEffect_NullEffect_ReturnsFalse()
        {
            var e = CreateCreature();
            e.AddPart(new StatusEffectsPart());

            var sep = e.GetPart<StatusEffectsPart>();
            Assert.IsFalse(sep.ApplyEffect(null));
        }

        [Test]
        public void RemoveAllEffects_EmptyList_NoError()
        {
            var e = CreateCreature();
            e.AddPart(new StatusEffectsPart());

            var sep = e.GetPart<StatusEffectsPart>();
            sep.RemoveAllEffects(); // should not throw
            Assert.AreEqual(0, sep.EffectCount);
        }

        [Test]
        public void Effect_StunAndPoisonTogether_StunBlocksTurnButPoisonStillApplied()
        {
            // When stunned, TakeTurn is blocked. But poison ticks on TakeTurn too.
            // The stun check happens first and blocks event propagation,
            // so poison does NOT tick when stunned (this is correct — stun blocks everything).
            var e = CreateCreature(hp: 100);
            e.ApplyEffect(new StunnedEffect(2));
            e.ApplyEffect(new PoisonedEffect(5, "1d3", new Random(42)));

            int hpBefore = e.GetStatValue("HP");
            e.FireEvent(GameEvent.New("TakeTurn")); // Stun blocks turn

            // Since stun blocks the event, poison OnTurnStart doesn't fire
            Assert.AreEqual(hpBefore, e.GetStatValue("HP"),
                "Stun should block all TakeTurn processing including poison");
        }

        private class EffectEventProbePart : Part
        {
            public bool BlockBeforeApply;
            public int AppliedCount;
            public int RemovedCount;
            public string LastAppliedEffectType;
            public string LastRemovedEffectType;

            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "BeforeApplyEffect" && BlockBeforeApply)
                    return false;

                if (e.ID == "EffectApplied")
                {
                    AppliedCount++;
                    LastAppliedEffectType = e.GetStringParameter("EffectType");
                }
                else if (e.ID == "EffectRemoved")
                {
                    RemovedCount++;
                    LastRemovedEffectType = e.GetStringParameter("EffectType");
                }

                return true;
            }
        }
    }
}
