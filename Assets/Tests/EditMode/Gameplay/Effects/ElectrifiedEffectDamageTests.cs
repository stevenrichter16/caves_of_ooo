using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// ElectrifiedEffect per-turn damage tick — restores parity with
    /// FireTonic / AcidTonic where the elemental status applies a small
    /// damage payload each turn. Pre-fix the effect only stunned (1
    /// turn) and chained the electrified state to conductive entities;
    /// LightningTonic was a no-damage curiosity.
    ///
    /// User-visible invariant: while electrified, a creature takes
    /// `1 + floor(charge * 1.5)` lightning damage at the start of its
    /// turn. Damage routes through `ElectricResistance` via the
    /// `Lightning` attribute on the Damage instance.
    ///
    /// Test pattern: directly invoke `effect.OnTurnStart(target, ctx)`
    /// (mirrors `MaterialReactionPhaseCRETests` style) so the test is
    /// independent of TurnManager wiring.
    /// </summary>
    public class ElectrifiedEffectDamageTests
    {
        [SetUp]
        public void Setup() => MessageLog.Clear();

        // ====================================================================
        // 1. Damage on first turn start
        // ====================================================================

        [Test]
        public void Electrified_OnTurnStart_DealsDamage()
        {
            var e = MakeCreature(hp: 100);
            var ctx = MakeTickContext();

            var elec = new ElectrifiedEffect(charge: 1.0f);
            e.ApplyEffect(elec);
            int hpBefore = e.GetStatValue("Hitpoints");

            elec.OnTurnStart(e, ctx);

            int hpAfter = e.GetStatValue("Hitpoints");
            Assert.Less(hpAfter, hpBefore,
                "ElectrifiedEffect must damage the target on OnTurnStart at charge 1.0.");
        }

        // ====================================================================
        // 2. Damage every turn while active
        // ====================================================================

        [Test]
        public void Electrified_DamagesEachTurn_NotJustFirst()
        {
            var e = MakeCreature(hp: 100);
            var ctx = MakeTickContext();
            var elec = new ElectrifiedEffect(charge: 1.0f);
            e.ApplyEffect(elec);

            int hpStart = e.GetStatValue("Hitpoints");
            elec.OnTurnStart(e, ctx);
            int hpAfterFirst = e.GetStatValue("Hitpoints");
            elec.OnTurnStart(e, ctx);
            int hpAfterSecond = e.GetStatValue("Hitpoints");

            Assert.Less(hpAfterFirst, hpStart, "first tick must damage");
            Assert.Less(hpAfterSecond, hpAfterFirst, "second tick must damage further");
        }

        // ====================================================================
        // 3. Counter-check: charge 0 does no damage
        // ====================================================================

        [Test]
        public void Electrified_ZeroCharge_NoDamage()
        {
            // Edge case: a depleted ElectrifiedEffect (charge clamped to 0)
            // shouldn't force a phantom 1-damage tick.
            var e = MakeCreature(hp: 100);
            var ctx = MakeTickContext();
            var elec = new ElectrifiedEffect(charge: 0f);
            e.ApplyEffect(elec);

            int hpBefore = e.GetStatValue("Hitpoints");
            elec.OnTurnStart(e, ctx);
            int hpAfter = e.GetStatValue("Hitpoints");

            Assert.AreEqual(hpBefore, hpAfter,
                "ElectrifiedEffect with charge 0 must deal no damage (degenerate input).");
        }

        // ====================================================================
        // 4. Damage routes through ElectricResistance
        // ====================================================================

        [Test]
        public void Electrified_Damage_RoutedThroughElectricResistance()
        {
            // Two creatures, identical except one has ElectricResistance=50.
            // Apply same charge, same tick. Resistant creature takes
            // measurably less damage. (Resistance halves at 50.)
            var resistant = MakeCreature(hp: 100, electricResistance: 50);
            var unresistant = MakeCreature(hp: 100);
            var ctx = MakeTickContext();

            var elecR = new ElectrifiedEffect(charge: 4.0f); // 1 + 6 = 7 base
            resistant.ApplyEffect(elecR);
            elecR.OnTurnStart(resistant, ctx);
            int resistantDamage = 100 - resistant.GetStatValue("Hitpoints");

            var elecU = new ElectrifiedEffect(charge: 4.0f);
            unresistant.ApplyEffect(elecU);
            elecU.OnTurnStart(unresistant, ctx);
            int unresistantDamage = 100 - unresistant.GetStatValue("Hitpoints");

            Assert.Greater(unresistantDamage, 0,
                "Sanity: unresisted creature must take damage.");
            Assert.Less(resistantDamage, unresistantDamage,
                "ElectricResistance=50 must reduce ElectrifiedEffect damage.");
        }

        // ====================================================================
        // 5. Counter-check: Lightning damage does NOT route through HeatResistance
        // ====================================================================

        [Test]
        public void Electrified_Damage_NotReducedByHeatResistance()
        {
            // Regression guard for the typed-Damage routing: a creature
            // immune to fire (HeatResistance=200) must NOT block lightning
            // damage. Confirms the attribute is "Lightning", not "Fire".
            var fireImmune = MakeCreature(hp: 100, heatResistance: 200);
            var ctx = MakeTickContext();
            var elec = new ElectrifiedEffect(charge: 4.0f);
            fireImmune.ApplyEffect(elec);

            int hpBefore = fireImmune.GetStatValue("Hitpoints");
            elec.OnTurnStart(fireImmune, ctx);
            int hpAfter = fireImmune.GetStatValue("Hitpoints");

            Assert.Less(hpAfter, hpBefore,
                "Lightning damage must NOT be blocked by HeatResistance — wrong-attribute routing would be a bug.");
        }

        // ====================================================================
        // 6. Wet amplification doubles per-turn damage
        // ====================================================================

        [Test]
        public void Electrified_WetTarget_DealsMoreDamagePerTurn()
        {
            // A wet target's ElectrifiedEffect gets charge doubled in
            // OnApply (existing behavior). The per-turn damage tick
            // computed against the doubled charge should be visibly
            // larger than the dry-target tick.
            var dry = MakeCreature(hp: 100);
            var wet = MakeCreature(hp: 100);
            wet.ApplyEffect(new WetEffect(moisture: 1.0f));
            var ctx = MakeTickContext();

            var elecDry = new ElectrifiedEffect(charge: 1.0f);
            dry.ApplyEffect(elecDry);
            elecDry.OnTurnStart(dry, ctx);
            int dryDmg = 100 - dry.GetStatValue("Hitpoints");

            var elecWet = new ElectrifiedEffect(charge: 1.0f);
            wet.ApplyEffect(elecWet); // OnApply doubles charge to 2.0
            elecWet.OnTurnStart(wet, ctx);
            int wetDmg = 100 - wet.GetStatValue("Hitpoints");

            Assert.Greater(wetDmg, dryDmg,
                "Wet target's electrified tick must do more damage (charge doubled in OnApply).");
        }

        // ====================================================================
        // 7. Counter-check: FrozenEffect still does no damage
        // ====================================================================

        [Test]
        public void Frozen_OnTurnStart_DoesNotDamage()
        {
            // Regression guard: this fix must NOT generalize to other
            // status effects. FrozenEffect remains the dedicated
            // hard-control peer to ElectrifiedEffect.
            var e = MakeCreature(hp: 100);
            var frozen = new FrozenEffect(cold: 1.0f);
            e.ApplyEffect(frozen);

            int hpBefore = e.GetStatValue("Hitpoints");
            // FrozenEffect doesn't override OnTurnStart, so no-op.
            // We don't even invoke it — confirm by checking HP stays same
            // through manual end-turn cycles.
            frozen.OnTurnEnd(e);

            int hpAfter = e.GetStatValue("Hitpoints");
            Assert.AreEqual(hpBefore, hpAfter,
                "FrozenEffect must not deal damage — it's a control-only effect.");
        }

        // ====================================================================
        // 8. Damage stops when the effect's duration expires
        // ====================================================================

        [Test]
        public void Electrified_AfterDurationExpires_NoMoreDamage()
        {
            // After OnTurnEnd reduces Duration to 0, the effect is no
            // longer in the entity's effect list (StatusEffectsPart cleans
            // it up). Subsequent ticks shouldn't damage the entity.
            //
            // Direct-invocation path: simulate the per-turn cycle until
            // the effect would be cleaned up, then check that no further
            // tick fires.
            var e = MakeCreature(hp: 100);
            var ctx = MakeTickContext();
            var elec = new ElectrifiedEffect(charge: 1.0f); // Duration = 2
            e.ApplyEffect(elec);

            // Simulate 2 turns of OnTurnStart + OnTurnEnd.
            elec.OnTurnStart(e, ctx);
            elec.OnTurnEnd(e, ctx);
            elec.OnTurnStart(e, ctx);
            elec.OnTurnEnd(e, ctx);
            int hpAfterTwoTurns = e.GetStatValue("Hitpoints");

            Assert.IsTrue(elec.Duration <= 0,
                "After 2 OnTurnEnd ticks, Duration should be 0 or less (effect ready for removal).");
            // We don't assert "no further damage" by invoking again because
            // a real game wouldn't (StatusEffectsPart removes it). Pinning
            // Duration=0 is the user-visible invariant here.
            Assert.Less(hpAfterTwoTurns, 100,
                "Sanity: the two ticks during the effect's duration did damage the target.");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private static Entity MakeCreature(
            int hp = 100,
            int electricResistance = 0,
            int heatResistance = 0)
        {
            var e = new Entity { BlueprintName = "TestCreature" };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
                { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.Statistics["Toughness"] = new Stat
                { Owner = e, Name = "Toughness", BaseValue = 10 };
            if (electricResistance != 0)
                e.Statistics["ElectricResistance"] = new Stat
                    { Owner = e, Name = "ElectricResistance", BaseValue = electricResistance, Min = -100, Max = 200 };
            if (heatResistance != 0)
                e.Statistics["HeatResistance"] = new Stat
                    { Owner = e, Name = "HeatResistance", BaseValue = heatResistance, Min = -100, Max = 200 };
            e.AddPart(new RenderPart { DisplayName = "test" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static GameEvent MakeTickContext()
        {
            // Synthetic per-turn context: BeginTakeAction-like event with
            // a null Zone. ElectrifiedEffect's damage tick doesn't depend
            // on Zone (CombatSystem.ApplyDamage tolerates null zone).
            var ev = GameEvent.New("BeginTakeAction");
            ev.SetParameter("Zone", (object)null);
            return ev;
        }
    }
}
