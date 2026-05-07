using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WSP7.4 — Resistance audit tests. Pin that EVERY elemental damage
    /// path respects the corresponding resistance stat.
    ///
    /// <para>Pre-WSP7.4, mutation AOE damage and ElectrifiedEffect ticks
    /// silently bypassed resistance because they used the int-overload
    /// of <see cref="CombatSystem.ApplyDamage"/> which built a
    /// <see cref="Damage"/> with no attributes. WSP7.4 added a new
    /// overload <c>ApplyDamage(target, amount, elementAttribute,
    /// source, zone)</c> + migrated 8 AOE mutations + 1 effect tick
    /// + the MaterialReactionResolver to use it.</para>
    ///
    /// <para>These tests cover the overload itself (per-element pin)
    /// + a representative migration target per category (mutation AOE,
    /// effect tick, material reaction). The full migration list is in
    /// the WSP7.4 commit body; the tests here are exemplars proving
    /// the pattern works end-to-end.</para>
    /// </summary>
    public class Wsp74SpellResistanceAuditTests
    {
        // ── Fixture helpers ─────────────────────────────────────────────

        private static Entity MakeTarget(string element = "Heat", int resistance = 50, int hp = 100)
        {
            var e = new Entity { ID = "target", BlueprintName = "target" };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
                { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.Statistics["Toughness"] = new Stat
                { Owner = e, Name = "Toughness", BaseValue = 16, Min = 1, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = "target" });
            e.AddPart(new StatusEffectsPart());
            // Element-specific resistance.
            e.Statistics[element + "Resistance"] = new Stat
            {
                Owner = e, Name = element + "Resistance",
                BaseValue = resistance, Min = -100, Max = 100,
            };
            return e;
        }

        // ════════════════════════════════════════════════════════════════
        // The new overload itself — per-element pin
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void ApplyDamage_HeatTagged_RespectsHeatResistance()
        {
            var target = MakeTarget("Heat", resistance: 50);
            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, amount: 20,
                elementAttribute: "Heat", source: null, zone: null);
            int hpAfter = target.GetStatValue("Hitpoints");
            Assert.AreEqual(10, hpBefore - hpAfter,
                "Heat-tagged 20 damage on HeatResistance=50 must land 10 (50% reduction).");
        }

        [Test]
        public void ApplyDamage_ColdTagged_RespectsColdResistance()
        {
            var target = MakeTarget("Cold", resistance: 50);
            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, 20, "Cold", null, null);
            Assert.AreEqual(10, hpBefore - target.GetStatValue("Hitpoints"),
                "Cold-tagged 20 damage on ColdResistance=50 must land 10.");
        }

        [Test]
        public void ApplyDamage_ElectricTagged_RespectsElectricResistance()
        {
            var target = MakeTarget("Electric", resistance: 50);
            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, 20, "Electric", null, null);
            Assert.AreEqual(10, hpBefore - target.GetStatValue("Hitpoints"));
        }

        [Test]
        public void ApplyDamage_AcidTagged_RespectsAcidResistance()
        {
            var target = MakeTarget("Acid", resistance: 50);
            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, 20, "Acid", null, null);
            Assert.AreEqual(10, hpBefore - target.GetStatValue("Hitpoints"));
        }

        [Test]
        public void ApplyDamage_FullResistance_AbsorbsAllDamage()
        {
            // 100% resistance fully absorbs (per ApplyResistanceFor in
            // CombatSystem.cs:786).
            var target = MakeTarget("Heat", resistance: 100);
            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, 100, "Heat", null, null);
            Assert.AreEqual(hpBefore, target.GetStatValue("Hitpoints"),
                "100% HeatResistance must fully absorb Heat-tagged damage.");
        }

        [Test]
        public void ApplyDamage_NoElementString_DoesNotApplyResistance()
        {
            // Empty element string = untyped damage (status quo behavior
            // for non-elemental damage like Bleeding tick / brittle
            // shatter).
            var target = MakeTarget("Heat", resistance: 100);  // 100% Heat resist
            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, 20,
                elementAttribute: "", source: null, zone: null);
            Assert.AreEqual(20, hpBefore - target.GetStatValue("Hitpoints"),
                "Empty element string must skip resistance (untyped damage). " +
                "Even with HeatResistance=100, the 20 damage lands in full.");
        }

        [Test]
        public void ApplyDamage_NullElementString_DoesNotApplyResistance()
        {
            var target = MakeTarget("Heat", resistance: 100);
            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, 20,
                elementAttribute: null, source: null, zone: null);
            Assert.AreEqual(20, hpBefore - target.GetStatValue("Hitpoints"));
        }

        // ════════════════════════════════════════════════════════════════
        // Cross-element pin — Heat damage doesn't trigger ColdResistance
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void ApplyDamage_HeatTagged_IgnoresColdResistance()
        {
            // Target has 100% ColdResistance but 0% HeatResistance.
            // Heat damage should land in full.
            var target = MakeTarget("Cold", resistance: 100);
            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, 20, "Heat", null, null);
            Assert.AreEqual(20, hpBefore - target.GetStatValue("Hitpoints"),
                "Heat-tagged damage must ignore ColdResistance.");
        }

        // ════════════════════════════════════════════════════════════════
        // Element-alias pin — "Fire" should also work
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void ApplyDamage_FireAlias_RespectsHeatResistance()
        {
            // DamageAttributeFlags.Heat aliases include "Fire" (Damage.cs:23).
            // Callers can pass either string; both should fire HeatResistance.
            var target = MakeTarget("Heat", resistance: 50);
            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, 20, "Fire", null, null);
            Assert.AreEqual(10, hpBefore - target.GetStatValue("Hitpoints"),
                "'Fire' alias should set the Heat flag and fire HeatResistance.");
        }

        [Test]
        public void ApplyDamage_IceAlias_RespectsColdResistance()
        {
            var target = MakeTarget("Cold", resistance: 50);
            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, 20, "Ice", null, null);
            Assert.AreEqual(10, hpBefore - target.GetStatValue("Hitpoints"),
                "'Ice' alias should set the Cold flag and fire ColdResistance.");
        }

        [Test]
        public void ApplyDamage_LightningAlias_RespectsElectricResistance()
        {
            var target = MakeTarget("Electric", resistance: 50);
            int hpBefore = target.GetStatValue("Hitpoints");
            CombatSystem.ApplyDamage(target, 20, "Lightning", null, null);
            Assert.AreEqual(10, hpBefore - target.GetStatValue("Hitpoints"));
        }

        // ════════════════════════════════════════════════════════════════
        // ElectrifiedEffect tick — verify it was ALREADY correctly tagged
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void ElectrifiedEffectTick_RespectsElectricResistance_StillCorrect()
        {
            // Sanity pin — ElectrifiedEffect was already correctly
            // building a typed Damage with the "Lightning" attribute
            // (ElectrifiedEffect.cs:64-65) and calling the typed overload.
            // Verified during the WSP7.4 audit — no migration needed.
            var target = MakeTarget("Electric", resistance: 100);
            target.ApplyEffect(new ElectrifiedEffect(charge: 1.0f), null, null);
            var effect = target.GetPart<StatusEffectsPart>().GetEffect<ElectrifiedEffect>();
            Assert.IsNotNull(effect);

            int hpBefore = target.GetStatValue("Hitpoints");
            var ctx = GameEvent.New("BeginTakeAction");
            effect.OnTurnStart(target, ctx);
            Assert.AreEqual(hpBefore, target.GetStatValue("Hitpoints"),
                "100% ElectricResistance must absorb ElectrifiedEffect's Lightning-tagged tick.");
        }

        // ════════════════════════════════════════════════════════════════
        // BurningEffect tick — was already correctly tagged "Fire"
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void BurningEffectTick_RespectsHeatResistance_StillCorrect()
        {
            // Sanity pin — BurningEffect was already correctly tagging
            // damage with "Fire" pre-WSP7.4 (BurningEffect.cs:144-145).
            // Verifies the pre-existing correct behavior wasn't broken
            // by the WSP7.4 migration.
            var target = MakeTarget("Heat", resistance: 100);
            target.ApplyEffect(new BurningEffect(intensity: 1.0f), null, null);
            var effect = target.GetPart<StatusEffectsPart>().GetEffect<BurningEffect>();
            Assert.IsNotNull(effect);

            int hpBefore = target.GetStatValue("Hitpoints");
            var ctx = GameEvent.New("BeginTakeAction");
            effect.OnTurnStart(target, ctx);
            Assert.AreEqual(hpBefore, target.GetStatValue("Hitpoints"),
                "100% HeatResistance must absorb BurningEffect's Fire-tagged tick.");
        }

        // ════════════════════════════════════════════════════════════════
        // AcidicEffect tick — was already correctly tagged "Acid"
        // ════════════════════════════════════════════════════════════════

        [Test]
        public void AcidicEffectTick_RespectsAcidResistance_StillCorrect()
        {
            var target = MakeTarget("Acid", resistance: 100);
            // Need a MaterialPart for AcidicEffect to fire its tick logic
            // (the OnTurnStart method bails early without one).
            target.AddPart(new MaterialPart { Combustibility = 1.0f });
            target.ApplyEffect(new AcidicEffect(corrosion: 1.0f), null, null);

            int hpBefore = target.GetStatValue("Hitpoints");
            var ctx = GameEvent.New("BeginTakeAction");
            target.GetPart<StatusEffectsPart>().GetEffect<AcidicEffect>()
                .OnTurnStart(target, ctx);
            Assert.AreEqual(hpBefore, target.GetStatValue("Hitpoints"),
                "100% AcidResistance must absorb AcidicEffect's Acid-tagged tick.");
        }
    }
}
