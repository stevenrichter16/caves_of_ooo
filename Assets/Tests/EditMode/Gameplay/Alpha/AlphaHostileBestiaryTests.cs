using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// ALPHA-READINESS item 2 — hostile-bestiary (P0, verified). 17
    /// creatures — including 3 of 4 lair bosses and the starting-area
    /// IceWights — carried no Faction tag and were permanently neutral:
    /// they never attacked, and bump-to-attack (gated on IsHostile)
    /// barely let the player fight them. A new "Beasts" faction with
    /// -100 player reputation makes the wild dangerous again.
    /// </summary>
    [TestFixture]
    public class AlphaHostileBestiaryTests
    {
        private static EntityFactory _factory;

        // The formerly-factionless seventeen (verifier-confirmed complete).
        private static readonly string[] FormerlyNeutral = {
            "CaveBat", "CaveSlime", "CaveBear", "Glowmaw", "Scorpion",
            "SandWurm", "GiantSpider", "Viper", "JungleApe", "RuinScavenger",
            "SkeletalSentry", "StoneGolem", "DesertProwler", "JungleStalker",
            "AncientGuardian", "IceWight", "CharredHusk",
        };

        [OneTimeSetUp]
        public void LoadOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            FactionManager.Initialize(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Data/Factions.json")));
        }

        [TearDown]
        public void TearDown()
        {
            FactionManager.Reset();
        }

        // ── SM1+SM2: the seventeen are hostile ───────────────────

        [Test]
        public void AllFormerlyNeutralCreatures_AreNowHostileToPlayer()
        {
            var player = _factory.CreateEntity("Player");
            foreach (var name in FormerlyNeutral)
            {
                var creature = _factory.CreateEntity(name);
                Assert.IsNotNull(creature, name);
                Assert.IsTrue(FactionManager.IsHostile(creature, player),
                    $"{name} must be hostile to the player — it was permanently " +
                    "neutral (no Faction tag), so it never attacked and could " +
                    "barely be attacked");
            }
        }

        [Test]
        public void Wildlife_ThreatensVillagers_Too()
        {
            var bear = _factory.CreateEntity("CaveBear");
            var villager = _factory.CreateEntity("Villager");
            Assert.IsTrue(FactionManager.IsHostile(bear, villager),
                "Beasts and Villagers must be mutually hostile");
        }

        [Test]
        public void FriendlyNpcs_StayFriendly()
        {
            // Counter-check: the hostility fix must not leak onto the
            // village roster.
            var player = _factory.CreateEntity("Player");
            foreach (var name in new[] { "Villager", "PetDog", "Magpie", "Merchant" })
            {
                var npc = _factory.CreateEntity(name);
                Assert.IsFalse(FactionManager.IsHostile(npc, player),
                    $"{name} must remain non-hostile to the player");
            }
        }

        // ── SM3: Glowmaw ambush commits personal hostility ───────

        [Test]
        public void GlowmawDrop_SetsPersonalHostility_NotJustTarget()
        {
            // Belt-and-braces (verifier-corrected phrasing): the ambush
            // commitment must survive any future faction math, so the
            // drop marks the player as a PERSONAL enemy. Faction-level
            // hostility exists even pre-drop (SM1) — the counter-check
            // is on the personal channel only.
            var zone = new Zone("z");
            var glowmaw = _factory.CreateEntity("Glowmaw");
            var player = _factory.CreateEntity("Player");
            zone.AddEntity(glowmaw, 5, 5);
            zone.AddEntity(player, 5, 6); // within TriggerRadius 2

            var brain = glowmaw.GetPart<BrainPart>();
            Assert.IsNotNull(brain, "Glowmaw needs a Brain for the ambush part");
            brain.CurrentZone = zone;

            Assert.IsFalse(brain.IsPersonallyHostileTo(player),
                "counter-check: no PERSONAL hostility before the drop");

            var e = GameEvent.New("TakeTurn");
            glowmaw.FireEventAndRelease(e);

            Assert.IsTrue(brain.IsPersonallyHostileTo(player),
                "the drop must commit personal hostility, not just a Target ref");
        }
    }
}
