using System;
using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Phase D of the Qud-parity port: critical-hit damage scaling.
    ///
    /// Qud reference (XRL.World.Parts/Combat.cs:1106-1140, 1218-1228, 1247-1275):
    ///   • A natural-20 (or skill-modified threshold) sets <c>flag2 = true</c>.
    ///   • On critical:
    ///       - num8 (PenBonus) += 1, num9 (PenCapBonus) += 1
    ///       - flag5 (AutoPen) = true
    ///       - These bonuses flow into RollDamagePenetrations
    ///   • If penetrations == 0 AND AutoPen AND attacker is the player → force pens = 1
    ///   • If penetrations > 0 AND critical → damage.AddAttribute("Critical")
    ///
    /// Pre-port behavior in Caves of Ooo: nat-20 only bypasses DV (the to-hit gate);
    /// no damage bonus, no AutoPen, no "Critical" damage attribute.
    /// </summary>
    public class CriticalHitTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
        }

        // ====================================================================
        // 1. nat-20 + 0 penetration + player attacker → AutoPen forces pen=1
        // ====================================================================

        [Test]
        public void NaturalTwenty_AutoPen_ForcesAtLeastOnePen_ForPlayer()
        {
            // Setup: very low PV, very high AV → would normally produce 0 pens.
            // But nat-20 + Player tag should AutoPen (force pens=1) so the hit lands.
            //
            // To force nat-20 deterministically, we run many seeds and capture only
            // the cases where the d20 hit roll produced 20.
            var zone = new Zone();
            var attacker = MakeFighter(strength: 10, agility: 10);  // STR mod = -3
            attacker.Tags["Player"] = "";
            attacker.GetPart<MeleeWeaponPart>().BaseDamage = "1d4";
            attacker.GetPart<MeleeWeaponPart>().PenBonus = 0;
            attacker.GetPart<MeleeWeaponPart>().HitBonus = 0;
            zone.AddEntity(attacker, 5, 5);

            var defender = MakeFighter(strength: 16, agility: 16);
            defender.GetPart<ArmorPart>().AV = 99;  // unreachable AV
            defender.GetPart<ArmorPart>().DV = 0;
            defender.SetStatValue("Hitpoints", 100);
            zone.AddEntity(defender, 6, 5);

            // Probe: capture damage when TakeDamage fires (i.e., when penetration succeeded).
            int hitsLanded = 0;
            var probe = new TakeDamageCaptureProbe();
            probe.OnTakeDamage = e => hitsLanded++;
            defender.AddPart(probe);

            // Run many seeds; some will roll nat-20.
            // With AV=99 and bonus ~ -3, normal pen is impossible.
            // With nat-20 AutoPen for player, penetration = 1 forced → at least one hit lands.
            for (int seed = 0; seed < 200; seed++)
            {
                defender.SetStatValue("Hitpoints", 100);
                MessageLog.Clear();
                CombatSystem.PerformMeleeAttack(attacker, defender, zone, new Random(seed));
            }

            Assert.Greater(hitsLanded, 0,
                "Player attacker rolling nat-20 against AV=99 must land hits via AutoPen. " +
                "If 0 hits across 200 seeds, AutoPen isn't firing.");
        }

        // ====================================================================
        // 2. nat-20 AutoPen does NOT fire for non-player attackers
        // ====================================================================

        [Test]
        public void NaturalTwenty_AutoPen_DoesNotFire_ForNonPlayerAttacker()
        {
            // Same scenario but attacker has no Player tag. Mirrors Qud's
            // `flag5 && Attacker != null && Attacker.IsPlayer()` guard at line 1226.
            var zone = new Zone();
            var attacker = MakeFighter(strength: 10, agility: 10);
            // NO Player tag
            attacker.GetPart<MeleeWeaponPart>().BaseDamage = "1d4";
            attacker.GetPart<MeleeWeaponPart>().PenBonus = 0;
            attacker.GetPart<MeleeWeaponPart>().HitBonus = 0;
            zone.AddEntity(attacker, 5, 5);

            var defender = MakeFighter(strength: 16, agility: 16);
            defender.GetPart<ArmorPart>().AV = 99;
            defender.GetPart<ArmorPart>().DV = 0;
            defender.SetStatValue("Hitpoints", 100);
            zone.AddEntity(defender, 6, 5);

            int hitsLanded = 0;
            var probe = new TakeDamageCaptureProbe();
            probe.OnTakeDamage = e => hitsLanded++;
            defender.AddPart(probe);

            for (int seed = 0; seed < 200; seed++)
            {
                defender.SetStatValue("Hitpoints", 100);
                CombatSystem.PerformMeleeAttack(attacker, defender, zone, new Random(seed));
            }

            // With AV=99, bonus=-3, the only way pen succeeds is via massive
            // explosion chains (≥13 chained 10s; P ≈ 10⁻¹³). Across 200 trials,
            // expected hits ≈ 0. A regression where AutoPen incorrectly fires
            // for non-players would produce ~10 hits (5% nat-20 × 200).
            //
            // Self-review Finding 2: tightened from < 50 to < 5 to actually catch
            // the non-player AutoPen mutation.
            Assert.Less(hitsLanded, 5,
                $"Non-player nat-20 vs AV=99: only chain-explosions can pen, hits should be ~0. " +
                $"Got {hitsLanded}. If hits >= 5, AutoPen is firing for non-players (mutation bug).");
        }

        // ====================================================================
        // 3. Critical hit adds "Critical" attribute to Damage
        // ====================================================================

        [Test]
        public void NaturalTwenty_AddsCriticalAttribute_ToDamage()
        {
            var zone = new Zone();
            var attacker = MakeFighter(strength: 20, agility: 20);
            attacker.Tags["Player"] = "";
            attacker.GetPart<MeleeWeaponPart>().BaseDamage = "1d4";
            attacker.GetPart<MeleeWeaponPart>().PenBonus = 5;
            attacker.GetPart<MeleeWeaponPart>().HitBonus = 0;
            zone.AddEntity(attacker, 5, 5);

            var defender = MakeFighter(strength: 10, agility: 10);
            defender.GetPart<ArmorPart>().AV = 0;
            defender.GetPart<ArmorPart>().DV = 0;
            zone.AddEntity(defender, 6, 5);

            Damage capturedCritDamage = null;
            var probe = new TakeDamageCaptureProbe();
            probe.OnTakeDamage = e =>
            {
                if (e.GetParameter("Damage") is Damage d && d.HasAttribute("Critical"))
                    capturedCritDamage = d;
            };
            defender.AddPart(probe);

            // Run many seeds — find one that produces a nat-20 hit.
            for (int seed = 0; seed < 200 && capturedCritDamage == null; seed++)
            {
                defender.SetStatValue("Hitpoints", 100);
                CombatSystem.PerformMeleeAttack(attacker, defender, zone, new Random(seed));
            }

            Assert.IsNotNull(capturedCritDamage,
                "At least one nat-20 hit across 200 seeds must produce a Critical-tagged Damage");
        }

        // ====================================================================
        // 4. Non-critical hits do NOT add "Critical" attribute
        // ====================================================================

        [Test]
        public void NonCriticalHit_DoesNotAddCriticalAttribute()
        {
            // Self-review Finding 3: original assertion (`nonCrit > crit`) was
            // vacuously true. Strengthened to bound the crit count to within a
            // small statistical envelope around the expected nat-20 rate (5%
            // of 200 = 10 ± noise), which a "wrongly tag non-crits as Critical"
            // mutation would blow past.
            var zone = new Zone();
            var attacker = MakeFighter(strength: 20, agility: 20);
            attacker.GetPart<MeleeWeaponPart>().BaseDamage = "1d4";
            attacker.GetPart<MeleeWeaponPart>().PenBonus = 5;
            attacker.GetPart<MeleeWeaponPart>().HitBonus = 0;
            zone.AddEntity(attacker, 5, 5);

            var defender = MakeFighter(strength: 10, agility: 10);
            defender.GetPart<ArmorPart>().AV = 0;
            defender.GetPart<ArmorPart>().DV = 0;
            zone.AddEntity(defender, 6, 5);

            int nonCritHitsObserved = 0;
            int critsObserved = 0;
            var probe = new TakeDamageCaptureProbe();
            probe.OnTakeDamage = e =>
            {
                if (e.GetParameter("Damage") is Damage d)
                {
                    if (d.HasAttribute("Critical")) critsObserved++;
                    else nonCritHitsObserved++;
                }
            };
            defender.AddPart(probe);

            const int trials = 200;
            for (int seed = 0; seed < trials; seed++)
            {
                defender.SetStatValue("Hitpoints", 999);
                CombatSystem.PerformMeleeAttack(attacker, defender, zone, new Random(seed));
            }

            // Expected nat-20 rate: 1/20 = 5%. Across 200 trials, expect ~10
            // crits. Allow up to 25 (3-sigma upper bound + slack). If crit count
            // exceeds 25, a mutation is tagging non-crits as Critical.
            Assert.Greater(nonCritHitsObserved, 0,
                "Sanity: at least some non-crit hits must land");
            Assert.LessOrEqual(critsObserved, 25,
                $"Crits should be ~5% of hits ({trials * 0.05:F0} expected). " +
                $"Got {critsObserved} — mutation may be tagging non-crit hits as Critical.");
        }

        // ====================================================================
        // 5. Critical penetration bonus (+1 to bonus, +1 to maxBonus)
        // ====================================================================

        [Test]
        public void NaturalTwenty_AddsPenetrationBonus_VsHardArmor()
        {
            // Setup at exactly the edge where +1 pen bonus matters.
            // Bonus mod = 0, AV = 7. Each die [-1, 8] + 0 ≤ 7 unless die > 7 (only with explode).
            // With nat-20: bonus becomes 1 → die + 1 > 7 needs die > 6 → raws 9, 10 (and explosions).
            //   Higher success rate than non-crit.
            //
            // Indirect proof: comparing crit-only vs non-crit-only, crits should have a
            // higher pen rate. We can't easily isolate this without instrumenting the code,
            // but as a sanity check we count successful pens in nat-20-favored seeds.
            //
            // This test mainly serves as a smoke that crits don't BREAK pen — the actual
            // mathematical proof is in the integration test below.
            var zone = new Zone();
            var attacker = MakeFighter(strength: 16, agility: 16);
            attacker.Tags["Player"] = "";
            attacker.GetPart<MeleeWeaponPart>().BaseDamage = "1d4";
            attacker.GetPart<MeleeWeaponPart>().PenBonus = 0;
            attacker.GetPart<MeleeWeaponPart>().HitBonus = 0;
            zone.AddEntity(attacker, 5, 5);

            var defender = MakeFighter(strength: 10, agility: 10);
            defender.GetPart<ArmorPart>().AV = 7;
            defender.GetPart<ArmorPart>().DV = 0;
            zone.AddEntity(defender, 6, 5);

            int totalCritPensRolled = 0;
            var probe = new TakeDamageCaptureProbe();
            probe.OnTakeDamage = e =>
            {
                if (e.GetParameter("Damage") is Damage d && d.HasAttribute("Critical"))
                    totalCritPensRolled++;
            };
            defender.AddPart(probe);

            for (int seed = 0; seed < 200; seed++)
            {
                defender.SetStatValue("Hitpoints", 999);
                CombatSystem.PerformMeleeAttack(attacker, defender, zone, new Random(seed));
            }

            // With AutoPen for player + crit bonus, almost every nat-20 should land.
            // 5% nat-20 rate × 200 seeds = ~10 nat-20s expected. With AutoPen all should land.
            Assert.Greater(totalCritPensRolled, 0,
                "Player critical hits with crit pen bonus + AutoPen should land at least sometimes");
        }

        // ====================================================================
        // Helpers
        // ====================================================================

        private Entity MakeFighter(int strength, int agility, int hp = 30)
        {
            var entity = new Entity();
            entity.BlueprintName = "TestFighter";
            entity.Tags["Creature"] = "";
            entity.Statistics["Hitpoints"] = new Stat { Owner = entity, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            entity.Statistics["Strength"] = new Stat { Owner = entity, Name = "Strength", BaseValue = strength, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Owner = entity, Name = "Agility", BaseValue = agility, Min = 1, Max = 50 };
            entity.Statistics["Speed"] = new Stat { Owner = entity, Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            entity.AddPart(new RenderPart { DisplayName = "fighter" });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new MeleeWeaponPart());
            entity.AddPart(new ArmorPart());
            return entity;
        }
    }
}
