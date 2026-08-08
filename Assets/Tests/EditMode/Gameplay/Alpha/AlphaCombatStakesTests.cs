using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// ALPHA-READINESS item 1 — combat-stakes (P0, verified). The combat
    /// ENGINE is production-grade but the NUMBERS were dev placeholders:
    /// a 500 HP player, ~20 bruisers punching with the 1d2 default fist
    /// (including four whose entity-level MeleeWeapon parts are DEAD CODE
    /// — the body-part-aware attack path never reads them), flat XP 10
    /// across the bestiary, and no venom on the venomous. These tests pin
    /// the alpha tuning so it cannot silently regress.
    /// </summary>
    [TestFixture]
    public class AlphaCombatStakesTests
    {
        private static EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadBlueprintsOnce()
        {
            _factory = new EntityFactory();
            string path = Path.Combine(Application.dataPath,
                "Resources/Content/Blueprints/Objects.json");
            _factory.LoadBlueprints(File.ReadAllText(path));
        }

        // ── Helpers ──────────────────────────────────────────────

        private static MeleeWeaponPart ResolvedHandWeapon(Entity creature)
        {
            var body = creature.GetPart<Body>();
            Assert.IsNotNull(body, $"{creature.BlueprintName} must have a Body");
            body.RegenerateDefaultEquipment();
            var parts = body.GetBody().GetParts();
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i]._DefaultBehavior != null)
                {
                    var w = parts[i]._DefaultBehavior.GetPart<MeleeWeaponPart>();
                    if (w != null) return w;
                }
            }
            Assert.Fail($"{creature.BlueprintName}: no resolved natural weapon on any body part");
            return null;
        }

        private static int XpOf(string blueprint)
        {
            var e = _factory.CreateEntity(blueprint);
            Assert.IsNotNull(e, blueprint);
            var stat = e.GetStat("XPValue");
            Assert.IsNotNull(stat, $"{blueprint} must have an XPValue stat");
            return stat.Value;
        }

        // ── SM1: mortal player ───────────────────────────────────

        [Test]
        public void Player_Hitpoints_AlphaTuned_NotDevPlaceholder()
        {
            var player = _factory.CreateEntity("Player");
            var hp = player.GetStat("Hitpoints");
            Assert.AreEqual(40, hp.Value, "alpha-tuned starting HP");
            Assert.AreEqual(40, hp.Max,
                "the 500 HP dev placeholder made combat unlosable — healing, " +
                "tonics, +2 HP/level, and death recovery were all inert");
        }

        // ── SM2: tier-scaled XP ──────────────────────────────────

        [Test]
        public void Xp_RiskMatchesReward_AcrossTiers()
        {
            // The risk/reward inversion: a StoneGolem (T3, 50 HP, 2d6
            // fists) must be worth more than a SnapjawHunter (T1).
            Assert.Greater(XpOf("StoneGolem"), XpOf("SnapjawHunter"));
            Assert.Greater(XpOf("CaveBear"), XpOf("Snapjaw"));
            Assert.Greater(XpOf("AncientGuardian"), XpOf("StoneGolem"));
            Assert.Greater(XpOf("SandWurm"), XpOf("CaveBear"));
        }

        [Test]
        public void Xp_BruisersNoLongerInheritTheFlatBase()
        {
            // Every retuned creature overrides the base Creature XPValue 10.
            string[] retuned = {
                "CaveBat", "CaveSlime", "CaveBear", "Scorpion", "SandWurm",
                "GiantSpider", "Viper", "JungleApe", "RuinScavenger",
                "SkeletalSentry", "StoneGolem", "DesertProwler",
                "JungleStalker", "AncientGuardian", "ChoirTendril",
            };
            foreach (var name in retuned)
                Assert.AreNotEqual(10, XpOf(name),
                    $"{name} still inherits the flat base XPValue 10");
        }

        // ── SM3: natural weapons for the bruisers ────────────────

        [Test]
        public void Factory_BruiserNaturalWeapons_HaveTunedDamage()
        {
            // (name, expected damage, expected pen)
            (string, string, int)[] table = {
                ("CaveBearClaw", "2d4", 1), ("GolemFist", "2d6", 2),
                ("WurmBite", "2d6+1", 1), ("ViperBite", "1d3", 1),
                ("ApeFist", "1d6+1", 0), ("GuardianFist", "2d6+2", 2),
                ("BoneBlade", "1d6+1", 1), ("ProwlerClaw", "2d4", 1),
                ("StalkerClaw", "2d4", 1), ("ChoirLash", "2d4", 0),
                ("WightTouch", "1d6", 1), ("HuskTouch", "1d6", 1),
                ("ScorpionSting", "1d3", 1), ("SpiderBite", "1d4", 0),
                ("ScavengerClaw", "1d4", 1), ("SlimePseudopod", "1d3", 0),
                ("BatBite", "1d2", 0),
                // dead-code MeleeWeapon-part conversions:
                ("GlowmawBite", "2d4", 1), ("TrollFist", "2d6", 1),
                ("MimicBite", "1d8", 1), ("BanditBlade", "1d6", 1),
            };
            foreach (var (name, dmg, pen) in table)
            {
                var w = NaturalWeaponFactory.Create(name).GetPart<MeleeWeaponPart>();
                Assert.AreEqual(dmg, w.BaseDamage, name);
                Assert.AreEqual(pen, w.PenBonus, name + " pen");
            }
        }

        [Test]
        public void EndToEnd_StoneGolem_PunchesWithGolemFist_Not1d2()
        {
            var golem = _factory.CreateEntity("StoneGolem");
            var w = ResolvedHandWeapon(golem);
            Assert.AreEqual("2d6", w.BaseDamage,
                "the T3 golem hit for 1d2 (default fist) — threat tiers were fake");
        }

        [Test]
        public void EndToEnd_DeadCodeMeleeParts_NowRealThroughBodyPath()
        {
            // Glowmaw/SleepingTroll/MimicChest/AmbushBandit carried
            // entity-level MeleeWeapon parts the body-part-aware attack
            // path NEVER reads (verifier finding) — they also punched 1d2.
            Assert.AreEqual("2d4", ResolvedHandWeapon(_factory.CreateEntity("Glowmaw")).BaseDamage);
            Assert.AreEqual("2d6", ResolvedHandWeapon(_factory.CreateEntity("SleepingTroll")).BaseDamage);
            Assert.AreEqual("1d8", ResolvedHandWeapon(_factory.CreateEntity("MimicChest")).BaseDamage);
            Assert.AreEqual("1d6", ResolvedHandWeapon(_factory.CreateEntity("AmbushBandit")).BaseDamage);
        }

        [Test]
        public void EndToEnd_UntunedCreature_KeepsDefaultFist()
        {
            // Counter-check: a creature deliberately NOT in the bruiser
            // table still resolves the 1d2 default fist — the wiring is
            // per-blueprint, not a global override.
            var v = _factory.CreateEntity("Villager");
            var w = ResolvedHandWeapon(v);
            Assert.AreEqual("1d2", w.BaseDamage);
        }

        // ── SM4: venom on the venomous ───────────────────────────

        [Test]
        public void Venom_ViperScorpionSpider_CarryPoisonedOnHit()
        {
            StringAssert.Contains("Poisoned",
                NaturalWeaponFactory.Create("ViperBite").GetPart<MeleeWeaponPart>().OnHitEffectsRaw);
            StringAssert.Contains("Poisoned",
                NaturalWeaponFactory.Create("ScorpionSting").GetPart<MeleeWeaponPart>().OnHitEffectsRaw);
            StringAssert.Contains("Poisoned",
                NaturalWeaponFactory.Create("SpiderBite").GetPart<MeleeWeaponPart>().OnHitEffectsRaw);
        }

        [Test]
        public void Venom_NonVenomousClaw_HasNoOnHitSpec()
        {
            // Counter-check per §3.4: venom must not leak onto ordinary claws.
            var w = NaturalWeaponFactory.Create("CaveBearClaw").GetPart<MeleeWeaponPart>();
            Assert.IsTrue(string.IsNullOrEmpty(w.OnHitEffectsRaw),
                "CaveBearClaw must carry no on-hit effect spec");
        }
    }
}
