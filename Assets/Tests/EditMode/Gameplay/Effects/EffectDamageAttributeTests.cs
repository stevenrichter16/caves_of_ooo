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
        // Log-message-reflects-post-resistance damage
        //
        // Reported during EmberSpear playtest: log line said "glowmaw takes 2
        // fire damage" but glowmaw has HR=50, so the actual HP delta was 1.
        // The DOT effects (Burning, Acidic) were logging the PRE-resistance
        // roll, misleading players into thinking resistance wasn't firing.
        //
        // The fix reads Damage.Amount AFTER ApplyDamage returns —
        // ApplyResistances mutates it in place (CombatSystem.cs:701).
        // ====================================================================

        [Test]
        public void BurningEffect_OnTurnStartLog_ShowsPostResistanceDamage()
        {
            // Apply Burning to a HR=50 target. Pre-fix, the log would say
            // "takes N fire damage" where N is the pre-resistance roll —
            // misleading the player into thinking the resistance didn't
            // fire. Post-fix, N is the actually-applied (halved) damage.
            //
            // We pin the relationship "log-displayed N == actual HP delta"
            // rather than testing the exact number, since RollDamage is
            // RNG-dependent.
            MessageLog.Clear();
            var resistant = MakeCreature(hp: 100, heatResistance: 50);
            var burn = new BurningEffect(intensity: 5.0f, rng: new Random(7));
            resistant.ApplyEffect(burn);

            int hpBefore = resistant.GetStatValue("Hitpoints");
            burn.OnTurnStart(resistant, MakeTickContext());
            int hpAfter = resistant.GetStatValue("Hitpoints");
            int actualDelta = hpBefore - hpAfter;

            // Find the "takes X fire damage" message.
            string fireMsg = null;
            foreach (string m in MessageLog.GetMessages())
            {
                if (m.Contains("fire damage."))
                {
                    fireMsg = m;
                    break;
                }
            }
            Assert.IsNotNull(fireMsg,
                "BurningEffect.OnTurnStart must log a 'takes X fire damage.' message.");

            // Parse out the number. Format: "X takes N fire damage."
            int loggedDamage = ExtractNumberBefore(fireMsg, "fire damage.");
            Assert.AreEqual(actualDelta, loggedDamage,
                "BurningEffect's log message must show the POST-resistance " +
                "damage (matching the actual HP delta). Pre-fix: log showed " +
                "the pre-resistance roll, misleading the player. " +
                $"Got log='{fireMsg}', actualDelta={actualDelta}.");
        }

        [Test]
        public void AcidicEffect_OnTurnStartLog_ShowsPostResistanceDamage()
        {
            // Mirror test for AcidicEffect on an AR=50 target.
            MessageLog.Clear();
            var resistant = MakeOrganicCreature(hp: 100, acidResistance: 50);
            var acid = new AcidicEffect(corrosion: 1.0f);
            resistant.ApplyEffect(acid);

            int hpBefore = resistant.GetStatValue("Hitpoints");
            acid.OnTurnStart(resistant, MakeTickContext());
            int hpAfter = resistant.GetStatValue("Hitpoints");
            int actualDelta = hpBefore - hpAfter;

            string acidMsg = null;
            foreach (string m in MessageLog.GetMessages())
            {
                if (m.Contains("acid damage."))
                {
                    acidMsg = m;
                    break;
                }
            }
            Assert.IsNotNull(acidMsg,
                "AcidicEffect.OnTurnStart must log a 'takes X acid damage.' message.");

            int loggedDamage = ExtractNumberBefore(acidMsg, "acid damage.");
            Assert.AreEqual(actualDelta, loggedDamage,
                "AcidicEffect's log message must show the POST-resistance " +
                "damage (matching the actual HP delta). " +
                $"Got log='{acidMsg}', actualDelta={actualDelta}.");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        /// <summary>
        /// Parse "... takes N suffix" → return N. Returns -1 if not parseable.
        /// </summary>
        private static int ExtractNumberBefore(string message, string suffix)
        {
            int idx = message.IndexOf(suffix);
            if (idx <= 0) return -1;
            // Walk backwards from suffix to collect digits.
            int end = idx - 1;
            while (end >= 0 && message[end] == ' ') end--;
            int start = end;
            while (start >= 0 && char.IsDigit(message[start])) start--;
            start++;
            if (start > end) return -1;
            return int.Parse(message.Substring(start, end - start + 1));
        }


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
