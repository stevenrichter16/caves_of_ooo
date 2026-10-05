using System;
using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Skills;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class ElementalSpellDamageContractTests
    {
        [SetUp] public void Setup() => SpellCastFixture.Reset();
        [TearDown] public void TearDown() { ResonanceSystem.ResetForTests(); SpellCastFixture.RestoreRuntime(); }

        [TestCase(typeof(Pyromancy_Kindle))]
        [TestCase(typeof(Pyromancy_EmberSpit))]
        [TestCase(typeof(Pyromancy_FlameJet))]
        [TestCase(typeof(Pyromancy_Backdraft))]
        [TestCase(typeof(Pyromancy_FlamingHands))]
        [TestCase(typeof(Pyromancy_EmberVein))]
        [TestCase(typeof(Pyromancy_Conflagration))]
        [TestCase(typeof(Pyromancy_Pyroclasm))]
        [TestCase(typeof(Cryomancy_IceLance))]
        [TestCase(typeof(Cryomancy_RimeGrip))]
        [TestCase(typeof(Cryomancy_RimeNova))]
        [TestCase(typeof(Hydromancy_Quench))]
        [TestCase(typeof(Hydromancy_JetBlast))]
        [TestCase(typeof(Hydromancy_Undertow))]
        [TestCase(typeof(Galvanism_ArcBolt))]
        [TestCase(typeof(Galvanism_GroundSurge))]
        [TestCase(typeof(Galvanism_BacklashCoil))]
        [TestCase(typeof(Galvanism_Overload))]
        [TestCase(typeof(Galvanism_RailSpike))]
        [TestCase(typeof(Galvanism_Thunderclap))]
        [TestCase(typeof(Corrosion_AcidSpray))]
        public void EveryDirectElementalAttackHonorsTheSameUniversalInvestment(Type power)
        {
            int plain = Hit(power, false); int invested = Hit(power, true);
            Assert.Greater(plain, 0, "The counterexample must actually hit.");
            Assert.AreEqual(plain + 3, invested, "Spellcraft + Empower applies once to the direct spell damage, not its environmental aftermath.");
        }

        private static int Hit(Type power, bool invested)
        {
            var f = new SpellCastFixture(); var target = f.Creature("target", 6, 5);
            if (power == typeof(Pyromancy_Pyroclasm)) target.ApplyEffect(new BurningEffect(1f), f.Actor, f.Zone);
            if (power == typeof(Galvanism_Overload)) target.ApplyEffect(new WetEffect(0.8f), f.Actor, f.Zone);
            var skill = f.Learn(power);
            if (invested) { f.Learn<SpellcraftSkill>(); f.Learn<Spellcraft_Empower>(); }
            Assert.True(f.Cast(skill)); return 200 - target.GetStatValue("Hitpoints");
        }

        [TestCase(typeof(Pyromancy_FlameJet), typeof(PyromancySkill), "burning")]
        [TestCase(typeof(Cryomancy_RimeGrip), typeof(CryomancySkill), "wet")]
        [TestCase(typeof(Galvanism_GroundSurge), typeof(GalvanismSkill), "wet")]
        [TestCase(typeof(Corrosion_AcidSpray), typeof(CorrosionSkill), "acid")]
        public void ConditionalSchoolBonusRequiresItsActualTargetState(Type power, Type school, string mark)
        {
            int[] hit = new int[2];
            for (int i = 0; i < 2; i++)
            {
                var f = new SpellCastFixture(); var target = f.Creature("target", 6, 5); var spell = f.Learn(power); f.Learn(school);
                if (i == 1)
                    target.ApplyEffect(mark == "burning" ? (Effect)new BurningEffect(1f) : mark == "wet" ? new WetEffect(.8f) : new AcidicEffect(.8f), f.Actor, f.Zone);
                Assert.True(f.Cast(spell)); hit[i] = 200 - target.GetStatValue("Hitpoints");
            }
            Assert.AreEqual(hit[0] + Math.Max(1, hit[0] / 4), hit[1]);
        }

        [Test]
        public void StructuralTargetsReceiveInvestmentThroughTheirOwnHpPool()
        {
            var f = new SpellCastFixture(); var spell = f.Learn<Pyromancy_EmberSpit>(); f.Learn<SpellcraftSkill>(); f.Learn<Spellcraft_Empower>();
            var prop = new Entity { ID = "structural", BlueprintName = "structural" };
            prop.AddPart(new RenderPart { DisplayName = "test timber" }); prop.AddPart(new DestructiblePart { HP = 100, MaxHP = 100 });
            Assert.True(f.Zone.AddEntity(prop, 6, 5)); Assert.Null(prop.GetStat("Hitpoints"));
            Assert.True(f.Cast(spell)); Assert.AreEqual(94, prop.GetPart<DestructiblePart>().HP);
        }

        [TestCase(typeof(Rites_ShatteredRime), 9)]
        [TestCase(typeof(Rites_StillHeart), 0)]
        public void RiteResonanceSchoolDoesNotInventResistanceAttributes(Type rite, int expectedDamage)
        {
            var f = new SpellCastFixture(); var victim = f.Creature("target", 6, 5); victim.Statistics["ColdResistance"].BaseValue = 100;
            var ink = f.GiveInk(); var spell = f.Learn(rite); f.Learn<SpellcraftSkill>(); f.Learn<Spellcraft_Empower>();
            Assert.True(f.Cast(spell)); Assert.AreEqual(expectedDamage, 200 - victim.GetStatValue("Hitpoints")); Assert.AreEqual(9, ink.Charges);
        }

        [Test]
        public void AZeroDamageRiteDoesNotInventDamageOrConsumeDamageBuffs()
        {
            var f = new SpellCastFixture(); var ink = f.GiveInk(); var ley = f.Learn<Spellcraft_LeyTap>(); var heart = f.Learn<Pyromancy_HeartFlame>();
            var veil = f.Learn<Rites_ScaldingVeil>(); f.Learn<SpellcraftSkill>(); f.Learn<Spellcraft_Empower>();
            Assert.True(f.Cast(ley)); Assert.True(f.Cast(heart)); int hp = f.Actor.GetStatValue("Hitpoints");
            f.Actor.ApplyEffect(new WetEffect(.8f), f.Actor, f.Zone); Assert.True(f.Cast(veil));
            Assert.AreEqual(hp, f.Actor.GetStatValue("Hitpoints")); Assert.AreEqual(60, ley.PendingBonus); Assert.AreEqual(3, heart.ChargesRemaining); Assert.AreEqual(9, ink.Charges);
        }

        [TestCase(false)] [TestCase(true)]
        public void QuenchAlsoReceivesHydromancyMoistureInvestment(bool invested)
        {
            var f = new SpellCastFixture(); var target = f.Creature("target", 6, 5); var spell = f.Learn<Hydromancy_Quench>();
            if (invested) f.Learn<HydromancySkill>();
            Assert.True(f.Cast(spell)); Assert.AreEqual(invested ? 1f : .8f, target.GetEffect<WetEffect>().Moisture, .0001f);
        }

        [TestCase(false)] [TestCase(true)]
        public void ConjureWaterAlsoReceivesHydromancyMoistureInvestment(bool invested)
        {
            var oldFactory = MaterialReactionResolver.Factory;
            try
            {
                var factory = new EntityFactory(); factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
                MaterialReactionResolver.Factory = factory;
                var f = new SpellCastFixture(); var target = f.Creature("target", 7, 5); var spell = f.Learn<Hydromancy_ConjureWater>();
                if (invested) f.Learn<HydromancySkill>();
                Assert.True(f.Cast(spell)); Assert.AreEqual(invested ? .75f : .5f, target.GetEffect<WetEffect>().Moisture, .0001f);
            }
            finally { MaterialReactionResolver.Factory = oldFactory; }
        }
    }
}
