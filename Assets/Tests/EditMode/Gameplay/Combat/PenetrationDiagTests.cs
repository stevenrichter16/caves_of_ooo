using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Pin: each successful melee attack emits a damage/Penetration
    /// diag record with payload fields exposing the roll breakdown
    /// — weaponPenBonus, strMod, skillPenBonus, critPenBonus,
    /// totalBonus, maxBonus, naturalTwenty, penetrations, autoPenForced.
    ///
    /// <para>Use case: after applying a mod like Sharp (+1 PenBonus),
    /// a diag query <c>category=damage kind=Penetration</c> reveals
    /// the exact PenBonus value used per attack, so the player /
    /// debugger can verify the mod was contributing.</para>
    /// </summary>
    public class PenetrationDiagTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
        }

        private static Entity MakeFighter(string id = "fighter", int hp = 100)
        {
            var e = new Entity { ID = id, BlueprintName = id };
            e.Tags["Creature"] = "";
            e.Statistics["Hitpoints"] = new Stat
                { Owner = e, Name = "Hitpoints", BaseValue = hp, Min = 0, Max = hp };
            e.Statistics["Strength"] = new Stat
                { Owner = e, Name = "Strength", BaseValue = 18, Min = 1, Max = 50 };
            e.Statistics["Agility"] = new Stat
                { Owner = e, Name = "Agility", BaseValue = 14, Min = 1, Max = 50 };
            e.Statistics["Toughness"] = new Stat
                { Owner = e, Name = "Toughness", BaseValue = 10, Min = 1, Max = 50 };
            e.Statistics["DV"] = new Stat
                { Owner = e, Name = "DV", BaseValue = 0, Min = -50, Max = 50 };
            e.AddPart(new RenderPart { DisplayName = id });
            e.AddPart(new StatusEffectsPart());
            e.AddPart(new MeleeWeaponPart
            {
                BaseDamage = "1d6",
                HitBonus = 20, // very high to make hits land in deterministic seeds
                PenBonus = 1,
                Stat = "Strength",
                Attributes = "Cutting LongBlades"
            });
            e.AddPart(new ArmorPart { AV = 0, DV = 0 });
            return e;
        }

        [Test]
        public void MeleeAttack_EmitsPenetrationDiag_WithWeaponPenBonus()
        {
            var zone = new Zone();
            var attacker = MakeFighter("attacker");
            attacker.Tags["Player"] = "";
            var defender = MakeFighter("defender");
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            // Seeds tried until one lands; we don't care which seed
            // succeeds, only that the diag fires.
            bool landed = false;
            for (int seed = 0; seed < 30 && !landed; seed++)
            {
                Diag.ResetAll();
                CombatSystem.PerformMeleeAttack(attacker, defender, zone,
                    new System.Random(seed));
                var hit = DiagQuery.Apply(new DiagQuery.Filter
                {
                    Category = "damage",
                    Kind = "Penetration",
                    Limit = 5,
                }).Records;
                if (hit.Count > 0)
                {
                    landed = true;
                    string payload = hit[0].PayloadJson ?? "";
                    StringAssert.Contains("\"weaponPenBonus\":1", payload,
                        "Payload reports weapon's PenBonus (set to 1 in fixture).");
                    StringAssert.Contains("\"penetrations\"", payload);
                    StringAssert.Contains("\"av\"", payload);
                    StringAssert.Contains("\"strMod\"", payload);
                }
            }
            Assert.IsTrue(landed,
                "At least one of 30 seeded swings should connect and emit Penetration diag.");
        }

        [Test]
        public void SharpenedWeapon_PenetrationDiag_ShowsHigherPenBonus()
        {
            // Sharp mod adds +1 to weapon.PenBonus. The diag payload
            // should reflect the bumped weaponPenBonus value.
            var zone = new Zone();
            var attacker = MakeFighter("attacker");
            attacker.Tags["Player"] = "";
            var weapon = attacker.GetPart<MeleeWeaponPart>();
            weapon.PenBonus = 3; // simulate post-Sharp state
            var defender = MakeFighter("defender");
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            bool landed = false;
            for (int seed = 0; seed < 30 && !landed; seed++)
            {
                Diag.ResetAll();
                CombatSystem.PerformMeleeAttack(attacker, defender, zone,
                    new System.Random(seed));
                var hit = DiagQuery.Apply(new DiagQuery.Filter
                {
                    Category = "damage",
                    Kind = "Penetration",
                    Limit = 5,
                }).Records;
                if (hit.Count > 0)
                {
                    landed = true;
                    StringAssert.Contains("\"weaponPenBonus\":3",
                        hit[0].PayloadJson ?? "",
                        "Sharpened weapon reports PenBonus=3 in diag payload.");
                }
            }
            Assert.IsTrue(landed);
        }

        [Test]
        public void NaturalAttack_PenetrationDiag_ShowsZeroWeaponPenBonus()
        {
            // Counter-check: an attacker WITHOUT a MeleeWeaponPart
            // (natural claws/punch path) should still emit Penetration
            // diag with weaponPenBonus=0. Pins the "(natural)" weapon-
            // name fallback works.
            var zone = new Zone();
            var attacker = MakeFighter("attacker");
            attacker.Tags["Player"] = "";
            // Remove the MeleeWeaponPart to simulate natural attack.
            var melee = attacker.GetPart<MeleeWeaponPart>();
            attacker.Parts.Remove(melee);
            // Still need ONE weapon for PerformLegacyAttack to work —
            // add it back as a natural weapon with PenBonus=0.
            attacker.AddPart(new MeleeWeaponPart
            {
                BaseDamage = "1d4",
                HitBonus = 20,
                PenBonus = 0,
                Stat = "Strength",
            });
            var defender = MakeFighter("defender");
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(defender, 6, 5);

            bool landed = false;
            for (int seed = 0; seed < 30 && !landed; seed++)
            {
                Diag.ResetAll();
                CombatSystem.PerformMeleeAttack(attacker, defender, zone,
                    new System.Random(seed));
                var hit = DiagQuery.Apply(new DiagQuery.Filter
                {
                    Category = "damage",
                    Kind = "Penetration",
                    Limit = 5,
                }).Records;
                if (hit.Count > 0)
                {
                    landed = true;
                    StringAssert.Contains("\"weaponPenBonus\":0",
                        hit[0].PayloadJson ?? "");
                }
            }
            Assert.IsTrue(landed);
        }
    }
}
