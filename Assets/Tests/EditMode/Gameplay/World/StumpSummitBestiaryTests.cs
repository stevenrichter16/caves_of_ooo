using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>W6.3b: every authored summit/sima animal is an instantiated
    /// creature with real cell-sized art. These tests precede its content.</summary>
    public class StumpSummitBestiaryTests
    {
        private EntityFactory _factory;
        [OneTimeSetUp] public void LoadContent()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,
                "Resources/Content/Blueprints/Objects.json")));
        }

        [TestCase("SummitSinger", true)]
        [TestCase("BrocchiniaSentinel", true)]
        [TestCase("SkySari", false)]
        [TestCase("PrickleBrowGecko", false)]
        [TestCase("HelmwoodFrog", true)]
        public void SummitSimaAnimalExistsWithItsTemperamentAndCellSizedArt(string blueprint, bool passive)
        {
            Assert.IsTrue(_factory.Blueprints.ContainsKey(blueprint), blueprint + " must actually ship");
            var creature = _factory.CreateEntity(blueprint);
            Assert.IsNotNull(creature.GetPart<BrainPart>());
            Assert.AreEqual(passive, creature.GetPart<BrainPart>().Passive);
            Assert.Greater(creature.GetStatValue("Hitpoints"), 0);
            string file = null;
            foreach (var row in EnvironmentSpriteRenderer.CreatureSprites)
                if (row.Blueprint == blueprint)
                {
                    file = row.File;
                    Assert.AreEqual(row.Glyph.ToString(), creature.GetPart<RenderPart>().RenderString);
                }
            Assert.IsNotNull(file, "A missing mapping silently falls back to a letter");
            var sprite = Resources.Load<Sprite>("Sprites/Environment/" + file);
            Assert.IsNotNull(sprite, blueprint + " must load as a sprite");
            Assert.AreEqual(new Vector2(16, 16), sprite.rect.size);
            Assert.AreEqual(FilterMode.Point, sprite.texture.filterMode);
        }

        [TestCase("SkySari", "2d8")]
        [TestCase("PrickleBrowGecko", "1d3")]
        public void AuthoredNaturalAttackActuallyReachesDamageRoll(string blueprint, string dice)
        {
            var attacker = _factory.CreateEntity(blueprint);
            AssertAttackDice(attacker, dice);
        }

        [Test]
        public void EquippedHandWeaponStillWinsOverAnAuthoredBodyAttack()
        {
            var attacker = _factory.CreateEntity("SkySari");
            var root = AnatomyFactory.CreateSimple();
            var hand = AnatomyFactory.CreatePart("Hand");
            root.AddPart(hand);
            var weapon = new Entity();
            weapon.AddPart(new MeleeWeaponPart { BaseDamage = "1d7", HitBonus = 100, PenBonus = 20 });
            hand._Equipped = weapon;
            hand.FirstSlotForEquipped = true;
            hand.Primary = true;
            attacker.GetPart<Body>().SetBody(root);
            AssertAttackDice(attacker, "1d7");
        }

        [TestCase("SummitSinger", 0, 4)]
        [TestCase("HelmwoodFrog", 0, 4)]
        [TestCase("SkySari", 2, 2)]
        public void AdultFrogsAndEagleHaveTheirOwnLimbs(string blueprint, int wings, int feet)
        {
            var parts = _factory.CreateEntity(blueprint).GetPart<Body>().GetParts();
            Assert.AreEqual(wings, parts.FindAll(p => p.Type == "Wing").Count);
            Assert.AreEqual(feet, parts.FindAll(p => p.Type == "Feet").Count);
            if (blueprint != "SkySari")
                Assert.IsFalse(parts.Exists(p => p.Type == "Tail"), "adult frogs have no severable tail");
        }

        [Test]
        public void UntaggedHandlessBodyKeepsItsExistingFallback()
        {
            var attacker = _factory.CreateEntity("SkySari");
            attacker.Tags.Remove("BodyNaturalAttack");
            AssertAttackDice(attacker, "1d2");
        }

        private static void AssertAttackDice(Entity attacker, string expected)
        {
            // Exercise the public melee route, including body weapon selection.
            // High accuracy/penetration removes chance as a reason for a vacuous pass.
            var natural = attacker.GetPart<MeleeWeaponPart>();
            natural.HitBonus = 100;
            natural.PenBonus = 20;
            var target = new Entity();
            target.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 100000, Max = 100000 };
            var zone = new Zone();
            zone.AddEntity(attacker, 5, 5);
            zone.AddEntity(target, 6, 5);
            Diag.ResetAll();
            try
            {
                for (int seed = 0; seed < 30; seed++)
                    CombatSystem.PerformMeleeAttack(attacker, target, zone, new System.Random(seed));
                var records = DiagQuery.Apply(new DiagQuery.Filter
                    { Category = "damage", Kind = "DamageRoll", Limit = 100 }).Records;
                Assert.Greater(records.Count, 0, "the attack must actually penetrate");
                foreach (var record in records)
                    StringAssert.Contains("\"damageDice\":\"" + expected + "\"", record.PayloadJson);
            }
            finally { Diag.ResetAll(); }
        }
    }
}
