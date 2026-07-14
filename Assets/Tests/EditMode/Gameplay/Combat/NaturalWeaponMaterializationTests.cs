using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Tests.TestSupport;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// FUN-P0 M1.b — natural weapons must be MATERIALIZED at spawn.
    ///
    /// Pre-M1.b latent engine bug (verification sweep, 2026-07-12):
    /// Body.RegenerateDefaultEquipment was never called on the factory spawn
    /// path (only by ExtraArm/Regeneration mutations and a debug key), so
    /// _DefaultBehavior stayed null on every hand, GatherMeleeWeapons found
    /// no weapons, and EVERY creature in the game attacked with the
    /// hardcoded 1d2 null-weapon punch — Snapjaws never once swung their
    /// declared 1d4/pen1 claws in live play.
    ///
    /// The fix materializes ONLY for blueprints that declare a NaturalWeapon
    /// prop, deliberately keeping prop-less entities (Player, villagers) on
    /// the legacy single-punch path to bound the blast radius; the M1.b
    /// content pass gives every hostile a prop.
    /// </summary>
    public class NaturalWeaponMaterializationTests
    {
        private static ScenarioTestHarness _harness;

        [OneTimeSetUp]
        public void OneTimeSetUp() => _harness = new ScenarioTestHarness();

        [OneTimeTearDown]
        public void OneTimeTearDown() { _harness?.Dispose(); _harness = null; }

        private static (int hands, int materialized, MeleeWeaponPart first) InspectHands(Entity creature)
        {
            var body = creature.GetPart<Body>();
            Assert.IsNotNull(body, "creature must have a Body part");
            var root = body.GetBody();
            Assert.IsNotNull(root, "anatomy must be initialized");

            int hands = 0, materialized = 0;
            MeleeWeaponPart first = null;
            var parts = root.GetParts();
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i].Type != "Hand") continue;
                hands++;
                if (parts[i]._DefaultBehavior == null) continue;
                materialized++;
                if (first == null)
                    first = parts[i]._DefaultBehavior.GetPart<MeleeWeaponPart>();
            }
            return (hands, materialized, first);
        }

        [Test]
        public void Snapjaw_MaterializesDeclaredClaw_AtSpawn()
        {
            // Snapjaw declares NaturalWeapon=SnapjawClaw (1d4, pen 1). After
            // factory creation, its hands must carry a LIVE claw entity —
            // not just the blueprint string.
            var snapjaw = _harness.Factory.CreateEntity("Snapjaw");
            Assert.IsNotNull(snapjaw);

            var (hands, materialized, claw) = InspectHands(snapjaw);
            Assert.Greater(hands, 0, "Snapjaw anatomy must have hands.");
            Assert.AreEqual(hands, materialized,
                "Every hand must materialize its natural weapon at spawn — " +
                "unmaterialized hands mean the creature punches at 1d2 forever.");
            Assert.IsNotNull(claw, "Materialized claw must carry MeleeWeaponPart.");
            Assert.AreEqual("1d4", claw.BaseDamage, "SnapjawClaw dice.");
            Assert.AreEqual(1, claw.PenBonus, "SnapjawClaw penetration.");
        }

        [Test]
        [TestCase("BatBite", "1d2", 0)]
        [TestCase("SlimePseudopod", "1d3", 0)]
        [TestCase("BearClaw", "1d4", 1)]
        [TestCase("ScorpionSting", "1d3", 1)]
        [TestCase("WurmBite", "1d6", 1)]
        [TestCase("SpiderFang", "1d3", 0)]
        [TestCase("ViperFang", "1d2", 0)]
        [TestCase("ApeFist", "1d4", 0)]
        [TestCase("ScavengerClaw", "1d3", 0)]
        [TestCase("SentryBlade", "1d4+1", 1)]
        [TestCase("GolemFist", "1d6", 2)]
        [TestCase("BanditKnife", "1d4", 0)]
        [TestCase("AmbushKnife", "1d4", 1)]
        [TestCase("ProwlerClaw", "1d4+1", 1)]
        [TestCase("StalkerClaw", "1d4+1", 1)]
        [TestCase("GuardianFist", "1d6", 2)]
        [TestCase("GlowmawBite", "1d4", 0)]
        [TestCase("TrollFist", "1d6", 2)]
        [TestCase("MimicBite", "1d4", 1)]
        public void FactoryCase_PinsDiceAndPenetration(string caseName, string dice, int pen)
        {
            // M1.b dice budget: humanoid anatomy swings twice (primary +
            // off-hand at -2, no swing-chance gate), so effective output is
            // ~2x the die pinned here. Tuned against the mortal 40 HP player.
            var weapon = NaturalWeaponFactory.Create(caseName);
            var melee = weapon.GetPart<MeleeWeaponPart>();
            Assert.IsNotNull(melee, caseName);
            Assert.AreEqual(dice, melee.BaseDamage, $"{caseName} dice");
            Assert.AreEqual(pen, melee.PenBonus, $"{caseName} penetration");
        }

        [Test]
        public void FactoryDefaultCase_UnknownName_StaysHarmless()
        {
            // Counter-check: an unknown case name must fall through to the
            // 1d2 default, not throw — a typo'd blueprint prop degrades to
            // a fist instead of crashing zone generation.
            var weapon = NaturalWeaponFactory.Create("TotallyUnknownWeaponName");
            var melee = weapon.GetPart<MeleeWeaponPart>();
            Assert.IsNotNull(melee);
            Assert.AreEqual("1d2", melee.BaseDamage);
        }

        [Test]
        public void Villager_WithoutNaturalWeaponProp_StaysUnmaterialized()
        {
            // Counter-check pinning the deliberate blast-radius bound:
            // prop-less blueprints keep the legacy null-fist path (single
            // 1d2 punch), so villagers and the player do not silently gain
            // double fist swings from the M1.b engine fix.
            var villager = _harness.Factory.CreateEntity("Villager");
            Assert.IsNotNull(villager);

            var (hands, materialized, _) = InspectHands(villager);
            Assert.Greater(hands, 0, "Villager anatomy must have hands.");
            Assert.AreEqual(0, materialized,
                "Prop-less blueprints must NOT materialize default behaviors — " +
                "the M1.b fix is scoped to NaturalWeapon-prop carriers only.");
        }
    }
}
