using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Factory-backed defenses: natural armor, equipment and DV effects
    /// must agree with the hit roll and the inventory's displayed defense.</summary>
    public class DensityPhase1DodgeTests
    {
        EntityFactory _factory;

        [OneTimeSetUp]
        public void LoadBlueprints()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
        }

        [SetUp]
        public void Reset() { MessageLog.Clear(); Diag.ResetAll(); }

        Entity Create(string name)
        {
            var entity = _factory.CreateEntity(name);
            Assert.NotNull(entity, name);
            return entity;
        }

        static StatusEffectsPart Effects(Entity entity)
        {
            var effects = entity.GetPart<StatusEffectsPart>();
            if (effects == null) { effects = new StatusEffectsPart(); entity.AddPart(effects); }
            return effects;
        }

        static Effect MakeEffect(string kind)
        {
            switch (kind)
            {
                case "Stunned": return new StunnedEffect();
                case "Confused": return new ConfusedEffect();
                case "Hobbled": return new HobbledEffect();
                case "Paralyzed": return new ParalyzedEffect();
                case "Berserk": return new BerserkEffect();
                default: throw new ArgumentException(kind);
            }
        }

        [TestCase("Viper", 5)]
        [TestCase("SunStriker", 6)]
        public void NaturalDodge_IsCountedWithBody_AndRemovingItRemovesOnlyThatBonus(string name, int naturalDV)
        {
            var actor = Create(name);
            Assert.NotNull(actor.GetPart<Body>());
            var armor = actor.GetPart<ArmorPart>();
            Assert.AreEqual(naturalDV, armor.DV);
            int unarmoredDV = 6 + StatUtils.GetModifier(actor, "Agility");
            Assert.AreEqual(unarmoredDV + naturalDV, CombatSystem.GetDV(actor));
            armor.DV = 0;
            Assert.AreEqual(unarmoredDV, CombatSystem.GetDV(actor));
        }

        [TestCase(3)] [TestCase(-3)] [TestCase(0)]
        public void BodyAndNoBody_AgreeOnNaturalDefense_WhenNeitherWearsArmor(int naturalDV)
        {
            var actor = Create("Creature");
            actor.GetPart<ArmorPart>().DV = naturalDV;
            int expected = 6 + StatUtils.GetModifier(actor, "Agility") + naturalDV;
            Assert.AreEqual(expected, CombatSystem.GetDV(actor));
            Assert.IsTrue(actor.RemovePart(actor.GetPart<Body>()));
            Assert.AreEqual(expected, CombatSystem.GetDV(actor));
        }

        [Test]
        public void NaturalAndWornDefense_AddOnce_AndCarryingArmorDoesNotCount()
        {
            var actor = Create("Creature");
            actor.GetPart<ArmorPart>().DV = 5;
            var armor = Create("LeatherArmor");
            Assert.IsTrue(actor.GetPart<InventoryPart>().AddObject(armor));
            int bareDV = 6 + StatUtils.GetModifier(actor, "Agility") + 5;
            Assert.AreEqual(bareDV, CombatSystem.GetDV(actor));
            Assert.IsTrue(InventorySystem.Equip(actor, armor));
            Assert.AreEqual(bareDV - 1, CombatSystem.GetDV(actor));
            Assert.IsTrue(InventorySystem.UnequipItem(actor, armor));
            Assert.AreEqual(bareDV, CombatSystem.GetDV(actor));
        }

        [Test]
        public void NoBody_LegacyEquipmentStillSelectsWornArmorInsteadOfStackingNaturalArmor()
        {
            var actor = Create("Creature");
            actor.RemovePart(actor.GetPart<Body>());
            actor.GetPart<ArmorPart>().DV = 5;
            var inventory = actor.GetPart<InventoryPart>();
            var armor = Create("LeatherArmor");
            int baseDV = 6 + StatUtils.GetModifier(actor, "Agility");
            Assert.IsTrue(inventory.Equip(armor, "Body"));
            Assert.AreEqual(baseDV - 1, CombatSystem.GetDV(actor));
            Assert.IsTrue(inventory.Unequip("Body"));
            Assert.AreEqual(baseDV + 5, CombatSystem.GetDV(actor));
        }

        [TestCase("Creature")] [TestCase("Viper")] [TestCase("SunStriker")]
        [TestCase("MarlbackBreacher")] [TestCase("Player")]
        public void CreaturesHaveZeroDVAdjustment_WithRoomForNegativeEffects(string name)
        {
            var dv = Create(name).GetStat("DV");
            Assert.NotNull(dv, name + " must receive DV adjustments");
            Assert.AreEqual(0, dv.Value);
            Assert.AreEqual(-50, dv.Min);
            Assert.AreEqual(200, dv.Max);
            Assert.IsNull(Create("Dagger").GetStat("DV"), "the stat belongs to creatures, not items");
        }

        [TestCase("Player", "Stunned", 4)] [TestCase("Viper", "Stunned", 4)]
        [TestCase("Player", "Confused", 3)] [TestCase("Viper", "Confused", 3)]
        [TestCase("Player", "Hobbled", 3)] [TestCase("Viper", "Hobbled", 3)]
        [TestCase("Player", "Paralyzed", 6)] [TestCase("Viper", "Paralyzed", 6)]
        [TestCase("Player", "Berserk", 2)] [TestCase("Viper", "Berserk", 2)]
        public void StatusPenalty_ChangesEffectiveDefense_AndRemovalRestoresIt(string name, string kind, int penalty)
        {
            var actor = Create(name);
            var control = Create(name);
            var effects = Effects(actor);
            var effect = MakeEffect(kind);
            int baseline = CombatSystem.GetDV(actor);
            Assert.IsTrue(effects.ApplyEffect(effect));
            Assert.AreEqual(baseline - penalty, CombatSystem.GetDV(actor),
                "Confused includes its separate -2 Agility, worth another -1 DV");
            Assert.AreEqual(baseline, CombatSystem.GetDV(control), "a different actor is unaffected");
            Assert.IsTrue(effects.RemoveEffect(effect));
            Assert.AreEqual(baseline, CombatSystem.GetDV(actor));
        }

        [TestCase("Player")] [TestCase("Viper")]
        public void DodgeSkill_ImprovesEffectiveDefense_AndRemovalRestoresIt(string name)
        {
            var actor = Create(name);
            var skills = actor.GetPart<SkillsPart>();
            if (skills == null) { skills = new SkillsPart(); actor.AddPart(skills); }
            int baseline = CombatSystem.GetDV(actor);
            var dodge = new AcrobaticsDodgePower();
            Assert.IsFalse(skills.RemoveSkill(dodge));
            Assert.AreEqual(baseline, CombatSystem.GetDV(actor));
            Assert.IsTrue(skills.AddSkill(dodge));
            Assert.AreEqual(baseline + 2, CombatSystem.GetDV(actor));
            Assert.IsTrue(skills.RemoveSkill(dodge));
            Assert.AreEqual(baseline, CombatSystem.GetDV(actor));
        }

        [Test]
        public void DVAdjustment_UsesBaseBonusPenaltyAndBoost_AndMissingStatRemainsSafe()
        {
            var actor = Create("Player");
            var dv = actor.GetStat("DV");
            int baseline = CombatSystem.GetDV(actor);
            dv.BaseValue = 2; dv.Bonus = 3; dv.Penalty = 7; dv.Boost = 1;
            Assert.AreEqual(baseline - 1, CombatSystem.GetDV(actor));
            actor.Statistics.Remove("DV");
            Assert.AreEqual(baseline, CombatSystem.GetDV(actor), "old entities without the new stat remain valid");
        }

        [Test]
        public void ActualHitRoll_NaturalDodgeAvoidsAnAttack_StunMakesTheSameRollLand()
        {
            var defender = Create("Viper");
            defender.GetStat("Agility").BaseValue = 16;
            var attacker = Create("Creature");
            attacker.GetStat("Agility").BaseValue = 16;
            var zone = new Zone("density-dv");
            Assert.IsTrue(zone.AddEntity(attacker, 5, 5));
            Assert.IsTrue(zone.AddEntity(defender, 6, 5));
            CombatSystem.PerformSingleAttack(attacker, defender, null, true, zone, new FixedHitRng());
            var roll = DiagQuery.Apply(new DiagQuery.Filter { Category = "damage", Kind = "HitRoll", Limit = 1 }).Records.Single();
            StringAssert.Contains("\"dv\":11", roll.PayloadJson);
            StringAssert.Contains("\"landed\":false", roll.PayloadJson);
            Assert.IsTrue(Effects(defender).ApplyEffect(new StunnedEffect()));
            Diag.ResetAll();
            CombatSystem.PerformSingleAttack(attacker, defender, null, true, zone, new FixedHitRng());
            roll = DiagQuery.Apply(new DiagQuery.Filter { Category = "damage", Kind = "HitRoll", Limit = 1 }).Records.Single();
            StringAssert.Contains("\"dv\":7", roll.PayloadJson);
            StringAssert.Contains("\"landed\":true", roll.PayloadJson);
        }

        sealed class FixedHitRng : System.Random
        {
            public override int Next(int minValue, int maxValue) => maxValue == 21 ? 9 : minValue;
        }

        [Test]
        public void InventoryDefense_UsesSingleComputedEntry_AndTracksEquipmentAndEffects()
        {
            var actor = Create("Player");
            actor.GetPart<ArmorPart>().DV = 5;
            actor.Statistics["AV"] = new Stat { Name = "AV", BaseValue = 99, Max = 100 };
            actor.Statistics["Custom"] = new Stat { Name = "Custom", BaseValue = 7 };
            AssertInventoryDefense(actor);
            var armor = Create("LeatherArmor");
            Assert.IsTrue(actor.GetPart<InventoryPart>().AddObject(armor));
            Assert.IsTrue(InventorySystem.Equip(actor, armor));
            AssertInventoryDefense(actor);
            Assert.IsTrue(Effects(actor).ApplyEffect(new HobbledEffect()));
            AssertInventoryDefense(actor);
            Assert.IsTrue(InventorySystem.UnequipItem(actor, armor));
            AssertInventoryDefense(actor);
        }

        static void AssertInventoryDefense(Entity actor)
        {
            var stats = InventoryScreenData.Build(actor).PlayerStats;
            Assert.AreEqual(1, stats.Count(x => x.Name == "DV"));
            Assert.AreEqual(1, stats.Count(x => x.Name == "AV"));
            Assert.AreEqual(CombatSystem.GetDV(actor).ToString(), stats.Single(x => x.Name == "DV").Value);
            Assert.AreEqual(CombatSystem.GetAV(actor).ToString(), stats.Single(x => x.Name == "AV").Value);
            Assert.AreEqual("7", stats.Single(x => x.Name == "Custom").Value);
        }
    }
}
