using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Bug-fix coverage: <c>PerformSingleAttack</c> used to construct the
    /// "X hits Y for N damage! (M HP remaining)" log line BEFORE the typed
    /// <c>ApplyDamage</c> ran, so it printed the RAW pre-resistance damage
    /// and a projected (also-raw) HP-remaining count. Phase E elemental
    /// resistance happens inside <c>ApplyDamage</c> — by the time HP is
    /// actually decremented, the real damage and remaining HP can both be
    /// half of what the log claimed.
    ///
    /// Surfaced when the FlamingSword scenario shipped: the player swings
    /// at a Glowmaw (HeatResistance=50), the log claims "12 damage / 188 HP
    /// remaining," but the live entity is at HP=194 (only 6 actually landed).
    /// The HP bar is correct; the log is the liar.
    ///
    /// Test strategy: each test snapshots HP before, runs the attack,
    /// snapshots HP after, then parses N out of the log line "for N damage".
    /// The contract is "N must equal hpBefore - hpAfter (the actual landed
    /// amount)". This is robust to penetration-chain randomness — we don't
    /// need to predict the exact damage, only verify the log is consistent
    /// with reality.
    /// </summary>
    public class CombatHitLogResistanceTests
    {
        // Captures "for N damage" where N is one or more digits.
        private static readonly Regex DamageRegex = new Regex(
            @"for (\d+) damage", RegexOptions.Compiled);

        // Captures "(N HP remaining)".
        private static readonly Regex HpRemainingRegex = new Regex(
            @"\((\d+) HP remaining\)", RegexOptions.Compiled);

        [SetUp]
        public void Setup() => MessageLog.Clear();

        // ====================================================================
        // 1. Hit log "for N damage" matches actual HP delta
        //    on a heat-resistant target hit by a Fire-tagged weapon.
        //    THIS IS THE BUG. Pre-fix this test fails.
        // ====================================================================

        [Test]
        public void HitLog_OnHeatResistantTarget_FireDamage_ReportsActualLandedAmount()
        {
            var (attacker, defender, zone) = MakeAttackScenario(
                attackerAttributes: "Cutting Fire LongBlades",
                defenderHeatResistance: 50);

            int hpBefore = defender.GetStatValue("Hitpoints", -1);
            CombatSystem.PerformMeleeAttack(attacker, defender, zone, new Random(42));
            int hpAfter = defender.GetStatValue("Hitpoints", -1);
            int actualDelta = hpBefore - hpAfter;

            // Sanity check: the resistance MUST have done something — a Fire
            // hit on a HR=50 target should land less than the raw amount.
            // Without this, the log assertion below could pass for the wrong
            // reason (e.g., resistance silently disabled).
            Assert.Greater(actualDelta, 0,
                "The attack should have dealt some damage. If 0, the test " +
                "setup isn't exercising the code path under inspection.");

            string hitLine = FindHitLine();
            Assert.IsNotNull(hitLine,
                "Expected a 'hits ... for ... damage' line. Log:\n" + DumpLog());

            int? loggedDamage = ParseLoggedDamage(hitLine);
            Assert.IsNotNull(loggedDamage,
                $"Couldn't parse 'for N damage' out of: {hitLine}");
            Assert.AreEqual(actualDelta, loggedDamage.Value,
                "The hit log's 'for N damage' must match the actual HP delta. " +
                $"HP before={hpBefore}, HP after={hpAfter}, actual delta={actualDelta}, " +
                $"but log claimed {loggedDamage.Value}. Hit line: {hitLine}");

            // Also check the "(M HP remaining)" suffix matches reality, if present.
            int? loggedHpRemaining = ParseLoggedHpRemaining(hitLine);
            if (loggedHpRemaining.HasValue)
            {
                Assert.AreEqual(hpAfter, loggedHpRemaining.Value,
                    "The hit log's '(M HP remaining)' must match the actual " +
                    $"post-damage HP. Real={hpAfter}, log claimed={loggedHpRemaining.Value}. " +
                    $"Hit line: {hitLine}");
            }
        }

        // ====================================================================
        // 2. Counter-check: without resistance, log is consistent — proves
        //    the fix doesn't regress the no-resistance case.
        // ====================================================================

        [Test]
        public void HitLog_OnNonResistantTarget_FireDamage_ReportsConsistentAmount()
        {
            var (attacker, defender, zone) = MakeAttackScenario(
                attackerAttributes: "Cutting Fire LongBlades",
                defenderHeatResistance: null);   // no HR stat at all

            int hpBefore = defender.GetStatValue("Hitpoints", -1);
            CombatSystem.PerformMeleeAttack(attacker, defender, zone, new Random(42));
            int hpAfter = defender.GetStatValue("Hitpoints", -1);
            int actualDelta = hpBefore - hpAfter;

            Assert.Greater(actualDelta, 0, "Attack should have dealt damage.");

            string hitLine = FindHitLine();
            Assert.IsNotNull(hitLine, "Expected hit line. Log:\n" + DumpLog());

            int? loggedDamage = ParseLoggedDamage(hitLine);
            Assert.IsNotNull(loggedDamage);
            Assert.AreEqual(actualDelta, loggedDamage.Value,
                "Without resistance, log and reality already agree. " +
                $"HP delta={actualDelta}, log={loggedDamage.Value}. Hit line: {hitLine}");
        }

        // ====================================================================
        // 3. Counter-check: non-Fire damage on heat-resistant target is
        //    also consistent — proves resistance doesn't fire on non-Fire
        //    damage and the log handles that case correctly.
        // ====================================================================

        [Test]
        public void HitLog_OnHeatResistantTarget_NonFireDamage_ReportsConsistentAmount()
        {
            var (attacker, defender, zone) = MakeAttackScenario(
                attackerAttributes: "Cutting LongBlades",        // NO Fire
                defenderHeatResistance: 50);

            int hpBefore = defender.GetStatValue("Hitpoints", -1);
            CombatSystem.PerformMeleeAttack(attacker, defender, zone, new Random(42));
            int hpAfter = defender.GetStatValue("Hitpoints", -1);
            int actualDelta = hpBefore - hpAfter;

            Assert.Greater(actualDelta, 0, "Attack should have dealt damage.");

            string hitLine = FindHitLine();
            Assert.IsNotNull(hitLine, "Expected hit line. Log:\n" + DumpLog());

            int? loggedDamage = ParseLoggedDamage(hitLine);
            Assert.IsNotNull(loggedDamage);
            Assert.AreEqual(actualDelta, loggedDamage.Value,
                "Non-Fire damage bypasses HeatResistance — log and reality " +
                $"should agree. HP delta={actualDelta}, log={loggedDamage.Value}. Hit line: {hitLine}");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        /// <summary>
        /// Build attacker (Player-tagged for AutoPen, Strength=20 Agility=20),
        /// defender (HP=500 to survive penetration chains, AV=0 DV=-10 to
        /// guarantee hit lands), and a zone with both placed adjacent. The
        /// attacker's weapon uses a deterministic-ish dice expression with
        /// known average, but we don't depend on the exact value — the test
        /// is robust to any actual damage outcome via HP-delta comparison.
        /// </summary>
        private static (Entity attacker, Entity defender, Zone zone) MakeAttackScenario(
            string attackerAttributes, int? defenderHeatResistance)
        {
            var zone = new Zone();

            var attacker = MakeFighter("attacker", strength: 20, agility: 20, hp: 30);
            attacker.Tags["Player"] = "";
            var weapon = attacker.GetPart<MeleeWeaponPart>();
            weapon.BaseDamage = "1d1+9";    // each penetration = 10 damage exactly
            weapon.PenBonus = 5;            // few but non-trivial penetration chain
            weapon.Attributes = attackerAttributes;
            zone.AddEntity(attacker, 5, 5);

            var defender = MakeFighter("defender", strength: 10, agility: 0, hp: 500);
            defender.GetPart<ArmorPart>().AV = 0;
            defender.GetPart<ArmorPart>().DV = -10;   // can't dodge
            if (defenderHeatResistance.HasValue)
            {
                defender.Statistics["HeatResistance"] = new Stat
                {
                    Owner = defender, Name = "HeatResistance",
                    BaseValue = defenderHeatResistance.Value, Min = 0, Max = 200
                };
            }
            zone.AddEntity(defender, 6, 5);

            return (attacker, defender, zone);
        }

        private static string FindHitLine()
        {
            foreach (var msg in MessageLog.GetRecent(20))
            {
                // The hit line follows "{attacker} hits {defender} for N damage!"
                // (or "{attacker} CRITICALLY hits ..." on nat-20s).
                if (msg.Contains("hits") && msg.Contains("damage") && DamageRegex.IsMatch(msg))
                    return msg;
            }
            return null;
        }

        private static int? ParseLoggedDamage(string hitLine)
        {
            var m = DamageRegex.Match(hitLine);
            return m.Success ? (int?)int.Parse(m.Groups[1].Value) : null;
        }

        private static int? ParseLoggedHpRemaining(string hitLine)
        {
            var m = HpRemainingRegex.Match(hitLine);
            return m.Success ? (int?)int.Parse(m.Groups[1].Value) : null;
        }

        private static string DumpLog()
        {
            var sb = new System.Text.StringBuilder();
            foreach (var msg in MessageLog.GetRecent(20))
                sb.AppendLine("  " + msg);
            return sb.ToString();
        }

        private static Entity MakeFighter(string id, int strength, int agility, int hp = 30)
        {
            var entity = new Entity { ID = id };
            entity.BlueprintName = "TestFighter";
            entity.Tags["Creature"] = "";
            entity.Statistics["Hitpoints"] = new Stat
                { Owner = entity, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            entity.Statistics["Strength"] = new Stat
                { Owner = entity, Name = "Strength", BaseValue = strength, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat
                { Owner = entity, Name = "Agility", BaseValue = agility, Min = 0, Max = 50 };
            entity.Statistics["Speed"] = new Stat
                { Owner = entity, Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = id });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new MeleeWeaponPart());
            entity.AddPart(new ArmorPart());
            return entity;
        }
    }
}
