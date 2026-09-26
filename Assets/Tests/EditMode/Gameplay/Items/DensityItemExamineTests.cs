using System;
using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Existing Examine events, not a proposed service API, prove the player-facing contract.</summary>
    public sealed class DensityItemExamineTests
    {
        private EntityFactory factory;

        [SetUp]
        public void Setup()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,
                "Resources/Content/Blueprints/Objects.json")));
            MessageLog.Clear();
        }

        private Entity Item(string name) => factory.CreateEntity(name);

        private static string Examine(Entity item)
        {
            MessageLog.Clear();
            var action = GameEvent.New("InventoryAction");
            action.SetParameter("Command", "Examine");
            item.FireEventAndRelease(action);
            return MessageLog.GetLast() ?? "";
        }

        [TestCase("Dagger", "1d4", "+1", "+0")]
        [TestCase("LongSword", "1d8", "+2", "+0")]
        [TestCase("EchoKnife", "1d4+3", "+1", "+3")]
        public void WeaponReportsItsOwnDiceAndModifiers(string name, string dice, string pen, string hit)
        {
            string text = Examine(Item(name));
            StringAssert.Contains("Damage: " + dice + " per penetration", text);
            StringAssert.Contains("Hit bonus: " + hit, text);
            StringAssert.Contains("Penetration bonus: " + pen, text);
        }

        [Test]
        public void WeaponReadsChangedLiveFields_InsteadOfGuessingFromItsBlueprintName()
        {
            var item = Item("Dagger");
            var weapon = item.GetPart<MeleeWeaponPart>();
            weapon.BaseDamage = "2d3+7"; weapon.HitBonus = -2; weapon.PenBonus = -4;
            weapon.Stat = "Willpower"; weapon.MaxStrengthBonus = 0;
            string text = Examine(item);
            StringAssert.Contains("Damage: 2d3+7 per penetration", text);
            StringAssert.Contains("Hit bonus: -2", text);
            StringAssert.Contains("Penetration bonus: -4", text);
            StringAssert.Contains("Willpower modifier", text);
            StringAssert.Contains("cap 0", text);
            StringAssert.DoesNotContain("Damage: 1d4", text);
        }

        [TestCase("Dagger", 3)]
        [TestCase("LongSword", CombatSystem.LEGACY_UNCAPPED_MAX_STR_BONUS)]
        public void StrengthCapMatchesTheActualCombatLimit(string name, int cap)
        {
            string text = Examine(Item(name));
            StringAssert.Contains("Strength modifier", text);
            StringAssert.Contains("cap " + cap, text);
            StringAssert.DoesNotContain("cap -1", text);
        }

        [TestCase("FlamingSword", "30%", "Set ablaze", "Fire")]
        [TestCase("IceSword", "30%", "Frozen over", "Ice")]
        [TestCase("CryoLance", "30%", "Frozen over", "Ice")]
        [TestCase("EmberSpear", "30%", "Set ablaze", "Fire")]
        [TestCase("AcidicDagger", "30%", "5 damage a turn", "Acid")]
        [TestCase("VenomDagger", "50%", "1d4 damage a turn for 6 turns", "Poison")]
        [TestCase("ThunderHammer", "30%", "Electrified", "Lightning")]
        [TestCase("DissolutionMaul", "40%", "5 damage a turn", "Acid")]
        public void ElementalWeaponDescribesConstructedEffectsAndAttributes(string name, string chance,
            string detail, string attribute)
        {
            string text = Examine(Item(name));
            StringAssert.Contains("On damaging hits", text);
            StringAssert.Contains(chance, text);
            StringAssert.Contains(detail, text);
            StringAssert.Contains("Attributes:", text);
            StringAssert.Contains(attribute, text);
        }

        [TestCase("Mace", "15%", "Stunned")]
        [TestCase("LongSword", "25%", "Bleeding")]
        [TestCase("Spear", "10%", "Confused")]
        public void PhysicalClassAttemptsAreVisibleWithoutElementalSpecs(string name, string chance, string effect)
        {
            string text = Examine(Item(name));
            StringAssert.Contains(chance, text);
            StringAssert.Contains(effect, text);
            StringAssert.DoesNotContain("Set ablaze", text);
        }

        [Test]
        public void SporebladeNameDoesNotInventSporeEmission()
        {
            string text = Examine(Item("Sporeblade"));
            StringAssert.Contains("Damage: 1d8+1", text);
            StringAssert.Contains("Bleeding", text);
            StringAssert.DoesNotContain("spore emission", text.ToLowerInvariant());
            StringAssert.DoesNotContain("fungal-spores", text);
        }

        [TestCase("LeatherArmor", "+3", "-1")]
        [TestCase("Buckler", "+1", "+1")]
        [TestCase("WardedCloak", "+0", "+2")]
        public void ArmorReportsContributionsRatherThanActorTotals(string name, string av, string dv)
        {
            string text = Examine(Item(name));
            StringAssert.Contains("AV: " + av, text);
            StringAssert.Contains("DV: " + dv, text);
            StringAssert.DoesNotContain("Damage:", text);
        }

        [TestCase("IronshodBoots", true)]
        [TestCase("LeatherBoots", false)]
        public void ArmorAdvertisesItsActualSpeedPenalty(string name, bool slows)
        {
            string text = Examine(Item(name));
            StringAssert.Contains("Equip slots: Feet", text);
            if (slows) StringAssert.Contains("Speed: -5", text);
            else StringAssert.DoesNotContain("Speed:", text);
        }

        [TestCase("DissolutionMaul", "Hand,Hand")]
        [TestCase("Dagger", "Hand")]
        public void EquipmentShowsItsEffectiveSlots(string name, string slots)
        {
            StringAssert.Contains("Equip slots: " + slots, Examine(Item(name)));
        }

        [TestCase("HealingTonic")]
        [TestCase("PoisonTonic")]
        [TestCase("FireTonic")]
        [TestCase("AcidTonic")]
        [TestCase("LightningTonic")]
        [TestCase("FrostTonic")]
        [TestCase("WaterTonic")]
        [TestCase("BleedTonic")]
        [TestCase("CharredTonic")]
        [TestCase("Antidote")]
        [TestCase("BurnSalve")]
        [TestCase("Panacea")]
        [TestCase("SpeedTonic")]
        [TestCase("StrengthTonic")]
        [TestCase("StoneskinTonic")]
        public void WorldExamineIncludesTheExistingTruthfulTonicPayload(string name)
        {
            var item = Item(name);
            Assert.IsTrue(TonicExamineService.TryDescribe(item, out string oldPopup));
            string payload = oldPopup.Substring(oldPopup.IndexOf("\n\n", StringComparison.Ordinal) + 2);
            StringAssert.Contains(payload, Examine(item));
        }

        [Test]
        public void ExistingFlavorEnhancementAndAfflictionArePreservedOnce()
        {
            var item = Item("BreacherCleaver");
            string flavor = item.GetPart<ExaminablePart>().Text;
            var enhancement = new EnhancementSerrated(); enhancement.ApplyTier(2); item.AddPart(enhancement);
            item.ApplyEffect(new PoisonedEffect(7, "1d3"));
            string text = Examine(item);
            StringAssert.Contains("Damage: 1d10", text);
            Assert.AreEqual(1, Count(text, flavor));
            Assert.AreEqual(1, Count(text, enhancement.GetEffectDescription()));
            Assert.AreEqual(1, Count(text, "Afflicted:"));
            StringAssert.Contains("1d3 damage a turn for 7 turns", text);
        }

        [TestCase("Viper")]
        [TestCase("MarlbackScrabbler")]
        [TestCase("Chest")]
        public void NonItemsKeepTheirExistingDescriptionWithoutItemMechanics(string name)
        {
            string text = Examine(Item(name));
            StringAssert.Contains("You see", text);
            StringAssert.DoesNotContain("Damage:", text);
            StringAssert.DoesNotContain("Equip slots:", text);
            StringAssert.DoesNotContain("AV:", text);
        }

        private static int Count(string text, string fragment)
        {
            int result = 0, offset = 0;
            while ((offset = text.IndexOf(fragment, offset, StringComparison.Ordinal)) >= 0)
            { result++; offset += fragment.Length; }
            return result;
        }
    }
}
