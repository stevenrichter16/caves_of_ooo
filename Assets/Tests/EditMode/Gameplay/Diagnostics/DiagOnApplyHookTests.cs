using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// D2.1 hook integration tests — every fresh effect application
    /// produces an <c>effect/OnApply</c> diag record.
    ///
    /// Plan ref: <c>Docs/D2-HOOKS-PLAN.md</c> §4 D2.1.
    /// Pairs with D1.2's <c>effect/OnRemove</c> for full effect-lifecycle
    /// observability.
    ///
    /// Hook insertion site: <c>StatusEffectsPart.ApplyEffectInternal</c>
    /// between <c>_effects.Add(effect)</c> and
    /// <c>effect.Applied(ParentEntity)</c>. Stacking (existing effect
    /// of same type returns true from <c>OnStack</c>) does NOT fire
    /// a second OnApply record — see counter-check #5.
    ///
    /// Five invariants:
    ///   1. ApplyEffect produces an effect/OnApply record.
    ///   2. Payload includes the effect type name.
    ///   3. Payload includes JustApplied (true mid-own-turn,
    ///      false cross-actor).
    ///   4. ActorId == source.ID when source is provided.
    ///   5. Counter-check: stacking re-application of same type
    ///      does NOT emit a second OnApply.
    /// </summary>
    public class DiagOnApplyHookTests
    {
        [SetUp]
        public void SetUp()
        {
            Diag.ResetAll();
        }

        // ====================================================================
        // 1. Apply produces an OnApply record
        // ====================================================================

        [Test]
        public void ApplyEffect_ProducesOnApplyRecord()
        {
            var entity = MakeMinimalCreature();
            int before = Diag.Snapshot(2000).Count;

            entity.ApplyEffect(new StunnedEffect(duration: 2));

            var records = Diag.Snapshot(2000);
            int diff = records.Count - before;
            Assert.GreaterOrEqual(diff, 1,
                "ApplyEffect must produce at least one new diag record. " +
                $"Got {diff} new records (expected ≥ 1).");

            var onApply = records.FirstOrDefault(r =>
                r.Category == "effect" &&
                r.Kind == "OnApply" &&
                r.TargetId == entity.ID);
            Assert.IsNotNull(onApply,
                $"Expected effect/OnApply record with TargetId={entity.ID}. " +
                $"Records: [{string.Join(", ", records.Select(r => r.Category + "/" + r.Kind))}]");
        }

        // ====================================================================
        // 2. Payload includes the effect type name
        // ====================================================================

        [Test]
        public void OnApplyRecord_PayloadIncludesEffectTypeName()
        {
            var entity = MakeMinimalCreature();
            entity.ApplyEffect(new StunnedEffect(duration: 2));

            var records = Diag.Snapshot(2000);
            var onApply = records.First(r => r.Category == "effect" && r.Kind == "OnApply");
            Assert.IsTrue(onApply.PayloadJson.Contains("StunnedEffect"),
                $"OnApply payload must include the effect's type name. " +
                $"Got: {onApply.PayloadJson}");
        }

        // ====================================================================
        // 3. Payload includes JustApplied flag, distinguishing trap-step
        //    (own turn → true) from cross-actor application (→ false)
        // ====================================================================

        [Test]
        public void OnApplyRecord_PayloadIncludesJustApplied_TrueForOwnTurn()
        {
            // Tag with "Player" so ProcessUntilPlayerTurn returns this entity
            // as CurrentActor instead of looping over NPCs forever.
            var player = MakeMinimalCreature();
            player.Tags["Player"] = "";
            var tm = new TurnManager();
            tm.AddEntity(player);
            tm.ProcessUntilPlayerTurn();
            Assert.AreSame(player, tm.CurrentActor,
                "Sanity: player must be the current actor.");

            player.ApplyEffect(new StunnedEffect(duration: 2));

            var records = Diag.Snapshot(2000);
            var onApply = records.First(r => r.Category == "effect" && r.Kind == "OnApply");
            // JsonConvert serializes booleans lowercase: justApplied:true
            Assert.IsTrue(onApply.PayloadJson.Contains("\"justApplied\":true"),
                $"When applying during the owner's own turn, payload must say " +
                $"justApplied=true. Got: {onApply.PayloadJson}");
        }

        [Test]
        public void OnApplyRecord_PayloadIncludesJustApplied_FalseForCrossActor()
        {
            // Mimic on-hit melee: attacker is the active turn-taker, applies
            // an effect to defender. CurrentActor == attacker, ParentEntity
            // == defender → JustApplied must be false.
            //
            // Tagging attacker with "Player" so ProcessUntilPlayerTurn
            // returns deterministically (without it, the loop runs all-NPCs
            // and never exits since both have Speed > 0).
            var attacker = MakeMinimalCreature();
            attacker.ID = "attacker-001";
            attacker.Tags["Player"] = "";
            var defender = MakeMinimalCreature();
            defender.ID = "defender-001";

            var tm = new TurnManager();
            tm.AddEntity(attacker);
            tm.AddEntity(defender);
            tm.ProcessUntilPlayerTurn();
            Assert.AreSame(attacker, tm.CurrentActor,
                "Sanity: ProcessUntilPlayerTurn must pick the player-tagged attacker.");

            // Apply BleedingEffect to defender — defender is NOT the current
            // actor, so JustApplied=false.
            defender.ApplyEffect(
                new BleedingEffect(saveTarget: 100, damageDice: "1d2", rng: new Random(0)),
                source: attacker);

            var records = Diag.Snapshot(2000);
            var onApply = records.First(r =>
                r.Category == "effect" && r.Kind == "OnApply" &&
                r.TargetId == "defender-001");
            Assert.IsTrue(onApply.PayloadJson.Contains("\"justApplied\":false"),
                $"Cross-actor application (CurrentActor != ParentEntity) must record " +
                $"justApplied=false. Got: {onApply.PayloadJson}");
        }

        // ====================================================================
        // 4. ActorId reflects the source argument
        // ====================================================================

        [Test]
        public void OnApplyRecord_ActorIdMatchesSource()
        {
            var attacker = MakeMinimalCreature();
            attacker.ID = "attacker-007";
            var defender = MakeMinimalCreature();
            defender.ID = "defender-007";

            defender.ApplyEffect(
                new BleedingEffect(saveTarget: 100, damageDice: "1d2", rng: new Random(0)),
                source: attacker);

            var records = Diag.Snapshot(2000);
            var onApply = records.First(r =>
                r.Category == "effect" && r.Kind == "OnApply" &&
                r.TargetId == defender.ID);
            Assert.AreEqual("attacker-007", onApply.ActorId,
                "OnApply record's ActorId must match the source entity passed to ApplyEffect.");
        }

        // ====================================================================
        // 5. ForceApplyEffect path records `forced:true` in payload
        //    (regular ApplyEffect records `forced:false`)
        // ====================================================================

        [Test]
        public void ForceApplyEffect_PayloadIncludesForcedTrue()
        {
            var entity = MakeMinimalCreature();
            entity.ForceApplyEffect(new StunnedEffect(duration: 2));

            var records = Diag.Snapshot(2000);
            var onApply = records.First(r => r.Category == "effect" && r.Kind == "OnApply");
            Assert.IsTrue(onApply.PayloadJson.Contains("\"forced\":true"),
                $"ForceApplyEffect must record forced=true. Got: {onApply.PayloadJson}");
        }

        [Test]
        public void RegularApplyEffect_PayloadIncludesForcedFalse()
        {
            // Counter-check to test #5: confirms the `forced` flag actually
            // distinguishes the two paths (without it, a buggy impl that
            // always wrote forced=true would pass test #5 vacuously).
            var entity = MakeMinimalCreature();
            entity.ApplyEffect(new StunnedEffect(duration: 2));

            var records = Diag.Snapshot(2000);
            var onApply = records.First(r => r.Category == "effect" && r.Kind == "OnApply");
            Assert.IsTrue(onApply.PayloadJson.Contains("\"forced\":false"),
                $"Regular ApplyEffect must record forced=false. Got: {onApply.PayloadJson}");
        }

        // ====================================================================
        // 6. Counter-check: stacking re-application does NOT emit
        //    a second OnApply record (the existing effect's OnStack
        //    handles it, returning true at StatusEffectsPart.cs:69-72)
        // ====================================================================

        [Test]
        public void StackingReApplication_DoesNotEmitSecondOnApplyRecord()
        {
            var entity = MakeMinimalCreature();

            // First apply — fresh, must emit OnApply.
            entity.ApplyEffect(new BleedingEffect(saveTarget: 15, damageDice: "1d2", rng: new Random(0)));
            int afterFirst = Diag.Snapshot(2000).Count(r => r.Category == "effect" && r.Kind == "OnApply");
            Assert.AreEqual(1, afterFirst, "First Bleeding apply must produce exactly one OnApply record.");

            // Second apply of same type — stacks via OnStack(), should NOT
            // emit a second OnApply (the stack branch returns BEFORE the
            // _effects.Add path where the hook lives).
            entity.ApplyEffect(new BleedingEffect(saveTarget: 20, damageDice: "1d3", rng: new Random(1)));
            int afterSecond = Diag.Snapshot(2000).Count(r => r.Category == "effect" && r.Kind == "OnApply");
            Assert.AreEqual(1, afterSecond,
                "Stacking re-application (same effect type) must NOT emit a second OnApply record. " +
                "If this fails, the hook is misplaced — moved before the stack branch instead of after. " +
                $"Got {afterSecond} OnApply records, expected 1.");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private static Entity MakeMinimalCreature()
        {
            var e = new Entity
            {
                BlueprintName = "TestCreature",
                ID = "test-" + Guid.NewGuid().ToString("N").Substring(0, 6)
            };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
            { Name = "Hitpoints", BaseValue = 100, Max = 100, Owner = e };
            e.Statistics["DV"] = new Stat { Name = "DV", BaseValue = 4, Owner = e };
            e.Statistics["Toughness"] = new Stat { BaseValue = 10, Owner = e };
            e.Statistics["Speed"] = new Stat { BaseValue = 100, Owner = e };
            e.AddPart(new RenderPart { DisplayName = "test creature" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }
    }
}
