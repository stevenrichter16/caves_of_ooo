using System;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// BurningEffect / AcidicEffect damage routing tests.
    ///
    /// Before the fix that ships with these tests, both effect classes
    /// used <c>CombatSystem.ApplyDamage(target, int amount, ...)</c>
    /// — the int overload that wraps amount in a <c>Damage</c> with NO
    /// attributes. <c>ApplyResistances</c> checks attributes via
    /// <c>IsHeatDamage</c> / <c>IsAcidDamage</c>, so a fire-immune or
    /// acid-immune creature still took full DoT damage. Asymmetric with
    /// the weapon-swing path which already routes correctly.
    ///
    /// User-visible invariant: HeatResistance / AcidResistance reduce
    /// (or amplify, with negative values) ALL damage of the matching
    /// type, regardless of whether it came from a weapon swing or a
    /// status-effect tick.
    /// </summary>
    public class EffectDamageAttributeTests
    {
        [SetUp]
        public void Setup() => MessageLog.Clear();

        // ====================================================================
        // BurningEffect: HeatResistance must reduce per-turn fire damage
        // ====================================================================

        [Test]
        public void BurningEffect_HeatResistance100_NullifiesDamage()
        {
            var fireImmune = MakeCreature(hp: 100, heatResistance: 100);
            var burn = new BurningEffect(intensity: 5.0f, rng: new Random(42));
            fireImmune.ApplyEffect(burn);

            int hpBefore = fireImmune.GetStatValue("Hitpoints");
            burn.OnTurnStart(fireImmune, MakeTickContext());
            int hpAfter = fireImmune.GetStatValue("Hitpoints");

            Assert.AreEqual(hpBefore, hpAfter,
                "HeatResistance=100 must nullify BurningEffect damage; pre-fix the int " +
                "overload of ApplyDamage stripped the Fire attribute and bypassed resistance.");
        }

        [Test]
        public void BurningEffect_HeatResistance50_HalvesDamage_Approx()
        {
            // Compare resisted vs unresisted. Resisted should take strictly less.
            var resistant = MakeCreature(hp: 100, heatResistance: 50);
            var unresisted = MakeCreature(hp: 100);
            var ctx = MakeTickContext();

            // Same RNG seed to control DamageTier roll variance.
            var burnR = new BurningEffect(intensity: 5.0f, rng: new Random(7));
            resistant.ApplyEffect(burnR);
            burnR.OnTurnStart(resistant, ctx);

            var burnU = new BurningEffect(intensity: 5.0f, rng: new Random(7));
            unresisted.ApplyEffect(burnU);
            burnU.OnTurnStart(unresisted, ctx);

            int resistantDmg = 100 - resistant.GetStatValue("Hitpoints");
            int unresistedDmg = 100 - unresisted.GetStatValue("Hitpoints");

            Assert.Greater(unresistedDmg, 0,
                "Sanity: unresisted must take damage from BurningEffect at intensity 5.");
            Assert.Less(resistantDmg, unresistedDmg,
                "HeatResistance=50 must reduce BurningEffect damage relative to unresisted.");
        }

        [Test]
        public void BurningEffect_HeatResistanceNegative50_AmplifiesDamage()
        {
            // Vulnerability case: HR = -50 should amplify damage.
            // Damage = base * (100 - (-50)) / 100 = base * 1.5.
            var vulnerable = MakeCreature(hp: 100, heatResistance: -50);
            var unresisted = MakeCreature(hp: 100);
            var ctx = MakeTickContext();

            var burnV = new BurningEffect(intensity: 5.0f, rng: new Random(7));
            vulnerable.ApplyEffect(burnV);
            burnV.OnTurnStart(vulnerable, ctx);

            var burnU = new BurningEffect(intensity: 5.0f, rng: new Random(7));
            unresisted.ApplyEffect(burnU);
            burnU.OnTurnStart(unresisted, ctx);

            int vulnerableDmg = 100 - vulnerable.GetStatValue("Hitpoints");
            int unresistedDmg = 100 - unresisted.GetStatValue("Hitpoints");

            Assert.Greater(vulnerableDmg, unresistedDmg,
                "HeatResistance=-50 (vulnerability) must amplify BurningEffect damage.");
        }

        // ====================================================================
        // BurningEffect counter-check: wrong-attribute resistance must NOT block
        // ====================================================================

        [Test]
        public void BurningEffect_ColdResistance_DoesNotReduceFireDamage()
        {
            // A creature with ColdResistance=100 (intended to nullify Cold,
            // not Fire) must still take full fire damage. Catches a bug where
            // the typed Damage routes through the wrong attribute.
            var coldImmune = MakeCreature(hp: 100, coldResistance: 100);
            var unresisted = MakeCreature(hp: 100);
            var ctx = MakeTickContext();

            var burnC = new BurningEffect(intensity: 5.0f, rng: new Random(7));
            coldImmune.ApplyEffect(burnC);
            burnC.OnTurnStart(coldImmune, ctx);

            var burnU = new BurningEffect(intensity: 5.0f, rng: new Random(7));
            unresisted.ApplyEffect(burnU);
            burnU.OnTurnStart(unresisted, ctx);

            int coldImmuneDmg = 100 - coldImmune.GetStatValue("Hitpoints");
            int unresistedDmg = 100 - unresisted.GetStatValue("Hitpoints");

            Assert.AreEqual(unresistedDmg, coldImmuneDmg,
                "ColdResistance must NOT block BurningEffect damage (different attribute).");
        }

        // ====================================================================
        // AcidicEffect: AcidResistance must reduce per-turn acid damage
        // ====================================================================

        [Test]
        public void AcidicEffect_AcidResistance100_NullifiesDamage()
        {
            var acidImmune = MakeCreature(hp: 100, acidResistance: 100);
            // AcidicEffect requires Organic material to deal damage.
            acidImmune.AddPart(new MaterialPart { MaterialID = "Flesh", MaterialTagsRaw = "Organic" });

            var acid = new AcidicEffect(corrosion: 1.0f);
            acidImmune.ApplyEffect(acid);

            int hpBefore = acidImmune.GetStatValue("Hitpoints");
            acid.OnTurnStart(acidImmune, MakeTickContext());
            int hpAfter = acidImmune.GetStatValue("Hitpoints");

            Assert.AreEqual(hpBefore, hpAfter,
                "AcidResistance=100 must nullify AcidicEffect damage.");
        }

        [Test]
        public void AcidicEffect_AcidResistanceNegative50_AmplifiesDamage()
        {
            // Scorpion-like vulnerability: chitin dissolves faster.
            var vulnerable = MakeOrganicCreature(hp: 100, acidResistance: -50);
            var unresisted = MakeOrganicCreature(hp: 100);
            var ctx = MakeTickContext();

            var acidV = new AcidicEffect(corrosion: 1.0f);
            vulnerable.ApplyEffect(acidV);
            acidV.OnTurnStart(vulnerable, ctx);

            var acidU = new AcidicEffect(corrosion: 1.0f);
            unresisted.ApplyEffect(acidU);
            acidU.OnTurnStart(unresisted, ctx);

            int vulnerableDmg = 100 - vulnerable.GetStatValue("Hitpoints");
            int unresistedDmg = 100 - unresisted.GetStatValue("Hitpoints");

            Assert.Greater(vulnerableDmg, unresistedDmg,
                "AcidResistance=-50 (vulnerability) must amplify AcidicEffect damage.");
        }

        [Test]
        public void AcidicEffect_NoResistance_DealsFullDamage()
        {
            // Counter-check: an organic creature with no AcidResistance
            // takes the un-resisted damage. Pin the formula:
            // 1 + floor(corrosion * 4) at corrosion=1.0 → 5 damage.
            var unresisted = MakeOrganicCreature(hp: 100);
            var acid = new AcidicEffect(corrosion: 1.0f);
            unresisted.ApplyEffect(acid);

            int hpBefore = unresisted.GetStatValue("Hitpoints");
            acid.OnTurnStart(unresisted, MakeTickContext());
            int hpAfter = unresisted.GetStatValue("Hitpoints");

            int dmg = hpBefore - hpAfter;
            Assert.AreEqual(5, dmg,
                "Unresisted AcidicEffect at corrosion 1.0 should deal 5 damage (1 + floor(1.0 * 4)).");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private static Entity MakeCreature(
            int hp = 100,
            int heatResistance = 0,
            int acidResistance = 0,
            int coldResistance = 0,
            int electricResistance = 0)
        {
            var e = new Entity { BlueprintName = "TestCreature" };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
                { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.Statistics["Toughness"] = new Stat
                { Owner = e, Name = "Toughness", BaseValue = 10 };
            if (heatResistance != 0)
                e.Statistics["HeatResistance"] = new Stat
                    { Owner = e, Name = "HeatResistance", BaseValue = heatResistance, Min = -100, Max = 200 };
            if (acidResistance != 0)
                e.Statistics["AcidResistance"] = new Stat
                    { Owner = e, Name = "AcidResistance", BaseValue = acidResistance, Min = -100, Max = 200 };
            if (coldResistance != 0)
                e.Statistics["ColdResistance"] = new Stat
                    { Owner = e, Name = "ColdResistance", BaseValue = coldResistance, Min = -100, Max = 200 };
            if (electricResistance != 0)
                e.Statistics["ElectricResistance"] = new Stat
                    { Owner = e, Name = "ElectricResistance", BaseValue = electricResistance, Min = -100, Max = 200 };
            e.AddPart(new RenderPart { DisplayName = "test" });
            e.AddPart(new StatusEffectsPart());
            return e;
        }

        private static Entity MakeOrganicCreature(int hp = 100, int acidResistance = 0)
        {
            var e = MakeCreature(hp: hp, acidResistance: acidResistance);
            // AcidicEffect.OnTurnStart only damages organic targets.
            e.AddPart(new MaterialPart { MaterialID = "Flesh", MaterialTagsRaw = "Organic" });
            return e;
        }

        private static GameEvent MakeTickContext()
        {
            var ev = GameEvent.New("BeginTakeAction");
            ev.SetParameter("Zone", (object)null);
            return ev;
        }
    }
}
