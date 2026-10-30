using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class SkillModifierTests : FiftyWorldFixture
    {
        [SetUp] public void LoadActualSkills() { SkillRegistry.ResetForTests(); SkillRegistry.EnsureInitialized(); }
        [TearDown] public void ResetSkills() => SkillRegistry.ResetForTests();
        SkillsPart Skills => Actor.GetPart<SkillsPart>();
        static System.Collections.Generic.IReadOnlyList<StatModifier> Bonus(string definition) => SkillModifiers.Parse(definition);

        [Test]
        public void DefinitionsOnlyAcceptPositiveKnownRanksAndCombineDuplicates()
        {
            var values = Bonus("LongBlades_Deflect:1,LongBlades_Deflect:-9,LongBlades_Deflect:2,FakeSkill:7,Strength:8,ThrowingSkill:0");
            Assert.AreEqual(1, values.Count);
            Assert.AreEqual("LongBlades_Deflect", values[0].StatName);
            Assert.AreEqual(3, values[0].Amount);
        }

        [Test]
        public void OverlappingSourcesCapGameplayButKeepIndependentReceipts()
        {
            Skills.AddSkill(nameof(LongBlades_Deflect));
            var first = SkillModifiers.Apply(Actor, Bonus("LongBlades_Deflect:2"));
            var second = SkillModifiers.Apply(Actor, Bonus("LongBlades_Deflect:2"));
            Assert.AreEqual(1, Skills.GetLearnedRank(nameof(LongBlades_Deflect)));
            Assert.AreEqual(4, Skills.GetSkill(nameof(LongBlades_Deflect)).BonusRank);
            Assert.AreEqual(3, Skills.GetRank(nameof(LongBlades_Deflect)));
            SkillModifiers.Remove(Actor, first);
            Assert.AreEqual(3, Skills.GetRank(nameof(LongBlades_Deflect)));
            SkillModifiers.Remove(Actor, second);
            Assert.AreEqual(1, Skills.GetRank(nameof(LongBlades_Deflect)));
        }

        [Test]
        public void ReplaceIsIdempotentAndRankedParryReadsEffectiveRank()
        {
            var receipt = SkillModifiers.Apply(Actor, Bonus("LongBlades_Deflect:1"));
            receipt = SkillModifiers.Replace(Actor, receipt, Bonus("LongBlades_Deflect:1"));
            Assert.AreEqual(0, Skills.GetLearnedRank(nameof(LongBlades_Deflect)));
            Assert.AreEqual(1, Skills.GetRank(nameof(LongBlades_Deflect)));
            Assert.AreEqual(15, BladeDefense.DeflectChance(Skills.GetRank(nameof(LongBlades_Deflect))));
            receipt = SkillModifiers.Replace(Actor, receipt, Bonus("LongBlades_Deflect:2"));
            Assert.AreEqual(22, BladeDefense.DeflectChance(Skills.GetRank(nameof(LongBlades_Deflect))));
            SkillModifiers.Remove(Actor, receipt);
            Assert.Null(Skills.GetSkill(nameof(LongBlades_Deflect)));
        }

        [Test]
        public void TemporaryRootDoesNotUnlockPermanentChildPurchases()
        {
            var receipt = SkillModifiers.Apply(Actor, Bonus("LongBladesSkill:1"));
            Actor.GetStat("SP").BaseValue = 20;
            var result = BuySkillAction.Execute(Actor, nameof(LongBlades_Deflect));
            Assert.False(result.Succeeded);
            Assert.AreEqual(BuySkillAction.FailureReason.MissingPrereq, result.Reason);
            Assert.AreEqual(20, Actor.GetStat("SP").BaseValue);
            SkillModifiers.Remove(Actor, receipt);
        }

        [Test]
        public void BuyingBorrowedRanksChargesLearnedCostAndSurvivesRemoval()
        {
            Skills.AddSkill(nameof(LongBladesSkill));
            Actor.GetStat("SP").BaseValue = 20;
            var receipt = SkillModifiers.Apply(Actor, Bonus("LongBlades_Deflect:3"));
            var original = Skills.GetSkill(nameof(LongBlades_Deflect));
            var first = BuySkillAction.Execute(Actor, nameof(LongBlades_Deflect));
            var second = BuySkillAction.Execute(Actor, nameof(LongBlades_Deflect));
            Assert.True(first.Succeeded); Assert.True(second.Succeeded);
            Assert.AreEqual(0, first.OldRank); Assert.AreEqual(1, second.OldRank);
            Assert.AreEqual(18, Actor.GetStat("SP").BaseValue);
            Assert.AreSame(original, Skills.GetSkill(nameof(LongBlades_Deflect)));
            SkillModifiers.Remove(Actor, receipt);
            Assert.AreEqual(2, Skills.GetLearnedRank(nameof(LongBlades_Deflect)));
            Assert.AreEqual(2, Skills.GetRank(nameof(LongBlades_Deflect)));
        }

        [Test]
        public void SavedBorrowedRanksRestoreOnceAndRemoveWithoutTouchingLearnedRanks()
        {
            Skills.AddSkill(nameof(LongBlades_Deflect));
            var receipt = SkillModifiers.Apply(Actor, Bonus("LongBlades_Deflect:1,CudgelSkill:1"));
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(Actor);
            loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(loaded);
            var skills = loaded.GetPart<SkillsPart>();
            Assert.AreEqual(2, skills.GetRank(nameof(LongBlades_Deflect)));
            Assert.AreEqual(0, skills.GetLearnedRank(nameof(CudgelSkill)));
            SkillModifiers.Remove(loaded, receipt);
            Assert.AreEqual(1, skills.GetRank(nameof(LongBlades_Deflect)));
            Assert.Null(skills.GetSkill(nameof(CudgelSkill)));
        }

        [Test]
        public void ActiveSkillsShareOneAbilityAndKeepIdentityAndCooldownAcrossSavedUnequip()
        {
            var first = SkillModifiers.Apply(Actor, Bonus("Persuasion_Recruit:1"));
            var second = SkillModifiers.Apply(Actor, Bonus("Persuasion_Recruit:1"));
            var abilities = Actor.GetPart<ActivatedAbilitiesPart>();
            var skill = Skills.GetSkill(nameof(Persuasion_Recruit));
            Guid id = skill.ActivatedAbilityID;
            var ability = abilities.GetAbility(id);
            ability.CooldownRemaining = 25;
            abilities.AssignAbilityToSlot(id, 8);
            Assert.AreEqual(1, abilities.AbilityList.Count(a => a.SourcePowerClass == nameof(Persuasion_Recruit)));
            SkillModifiers.Remove(Actor, first);
            Assert.AreSame(ability, abilities.GetAbility(id));
            SkillModifiers.Remove(Actor, second);
            Assert.Null(abilities.GetAbility(id));
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(Actor);
            loaded.FireEventAndRelease(GameEvent.New("EndTurn"));
            var restored = SkillModifiers.Apply(loaded, Bonus("Persuasion_Recruit:1"));
            var loadedSkill = loaded.GetPart<SkillsPart>().GetSkill(nameof(Persuasion_Recruit));
            var loadedAbilities = loaded.GetPart<ActivatedAbilitiesPart>();
            Assert.AreEqual(id, loadedSkill.ActivatedAbilityID);
            Assert.AreEqual(24, loadedAbilities.GetAbility(id).CooldownRemaining);
            Assert.AreEqual(8, loadedAbilities.GetSlotForAbility(id));
            SkillModifiers.Remove(loaded, restored);
        }

        [Test]
        public void LearningBorrowedAbilityPreservesItsBindingAndCooldown()
        {
            var receipt = SkillModifiers.Apply(Actor, Bonus("Persuasion_Recruit:1"));
            var abilityId = Skills.GetSkill(nameof(Persuasion_Recruit)).ActivatedAbilityID;
            var abilities = Actor.GetPart<ActivatedAbilitiesPart>();
            abilities.GetAbility(abilityId).CooldownRemaining = 12;
            Skills.AddSkill(nameof(Persuasion_Recruit), "book");
            SkillModifiers.Remove(Actor, receipt);
            Assert.AreEqual(1, Skills.GetLearnedRank(nameof(Persuasion_Recruit)));
            Assert.AreEqual(abilityId, Skills.GetSkill(nameof(Persuasion_Recruit)).ActivatedAbilityID);
            Assert.AreEqual(12, abilities.GetAbility(abilityId).CooldownRemaining);
        }

        [Test]
        public void BorrowedDodgeReversesSavedStatContributionAndKeepsTraining()
        {
            int before = Actor.GetStatValue("DV");
            var receipt = SkillModifiers.Apply(Actor, Bonus("AcrobaticsDodgePower:1"));
            Assert.AreEqual(before + 2, Actor.GetStatValue("DV"));
            Actor.GetStat("DV").BaseValue += 3;
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(Actor);
            SkillModifiers.Remove(loaded, receipt);
            Assert.AreEqual(before + 3, loaded.GetStatValue("DV"));
            Assert.Null(loaded.GetPart<AcrobaticsDodgePower>());
        }

        [Test]
        public void LegacyDodgeBaseShiftCanStillBeRemovedAfterLoad()
        {
            int before = Actor.GetStatValue("DV");
            Skills.AddSkill(nameof(AcrobaticsDodgePower));
            Actor.GetStat("DV").Bonus -= 2;
            Actor.GetStat("DV").BaseValue += 2;
            Actor.GetPart<AcrobaticsDodgePower>().AppliedDodgeBonus = null;
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(Actor);
            loaded.GetPart<SkillsPart>().RemoveSkill(loaded.GetPart<AcrobaticsDodgePower>());
            Assert.AreEqual(before, loaded.GetStatValue("DV"));
        }

        [TestCase("QuarryToothTrinket", "Strength:2,Agility:-1", "CudgelSkill")]
        [TestCase("TonguelessBellTrinket", "Ego:2,Strength:-1", "Persuasion_Recruit")]
        public void OldEquippedTrinketsMigrateExactStatReceiptToOnlySkillBonusOnce(string blueprint, string oldBonuses, string skill)
        {
            int strength = Actor.GetStatValue("Strength"), agility = Actor.GetStatValue("Agility"), ego = Actor.GetStatValue("Ego");
            var item = Carry(blueprint);
            var equipment = item.GetPart<EquippablePart>();
            equipment.EquipSkillBonuses = ""; equipment.EquipBonuses = oldBonuses;
            Assert.True(InventorySystem.Equip(Actor, item));
            Actor.GetStat("Strength").BaseValue += 3;
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(Actor);
            loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(loaded);
            var saved = loaded.GetPart<InventoryPart>().GetAllEquipped().Single(x => x.ID == item.ID);
            Assert.AreEqual(strength + 3, loaded.GetStatValue("Strength"));
            Assert.AreEqual(agility, loaded.GetStatValue("Agility"));
            Assert.AreEqual(ego, loaded.GetStatValue("Ego"));
            Assert.AreEqual("", saved.GetPart<EquippablePart>().EquipBonuses);
            Assert.AreEqual(1, loaded.GetPart<SkillsPart>().GetRank(skill));
            Assert.AreEqual(0, loaded.GetPart<SkillsPart>().GetLearnedRank(skill));
            Assert.True(InventorySystem.UnequipItem(loaded, saved));
            Assert.AreEqual(0, loaded.GetPart<SkillsPart>().GetRank(skill));
            Assert.AreEqual(strength + 3, loaded.GetStatValue("Strength"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void LegacyMigrationUsesExactReceiptOrOldDefinitionAndKeepsLearnedAbility(bool missingReceipt)
        {
            int strength = Actor.GetStatValue("Strength"), ego = Actor.GetStatValue("Ego");
            Skills.AddSkill(nameof(Persuasion_Recruit));
            var abilityId = Skills.GetSkill(nameof(Persuasion_Recruit)).ActivatedAbilityID;
            Actor.GetPart<ActivatedAbilitiesPart>().GetAbility(abilityId).CooldownRemaining = 17;
            var item = Carry("TonguelessBellTrinket");
            var equipment = item.GetPart<EquippablePart>();
            equipment.EquipSkillBonuses = ""; equipment.EquipBonuses = "Ego:2,Strength:-1";
            Assert.True(InventorySystem.Equip(Actor, item));
            if (missingReceipt) equipment.AppliedEquipBonuses = null;
            else equipment.EquipBonuses = "Ego:9,Strength:-8"; // saved receipt remains exact
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(Actor);
            var saved = loaded.GetPart<InventoryPart>().GetAllEquipped().Single(x => x.ID == item.ID);
            Assert.AreEqual(strength, loaded.GetStatValue("Strength"));
            Assert.AreEqual(ego, loaded.GetStatValue("Ego"));
            Assert.AreEqual(1, loaded.GetPart<SkillsPart>().GetLearnedRank(nameof(Persuasion_Recruit)));
            Assert.True(InventorySystem.UnequipItem(loaded, saved));
            var ability = loaded.GetPart<ActivatedAbilitiesPart>().GetAbility(abilityId);
            Assert.NotNull(ability);
            Assert.AreEqual(17, ability.CooldownRemaining);
            Assert.AreEqual(1, loaded.GetPart<SkillsPart>().GetRank(nameof(Persuasion_Recruit)));
            Assert.AreEqual(0, loaded.GetPart<SkillsPart>().GetRank(nameof(Persuasion_Dismiss)));
        }

        [Test]
        public void OldCarriedTrinketMigrationDoesNotGrantSkillOrRemoveAnyStats()
        {
            int strength = Actor.GetStatValue("Strength");
            var item = Carry("QuarryToothTrinket");
            item.GetPart<EquippablePart>().EquipSkillBonuses = "";
            item.GetPart<EquippablePart>().EquipBonuses = "Strength:2,Agility:-1";
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(Actor);
            var saved = loaded.GetPart<InventoryPart>().Objects.Single(x => x.ID == item.ID);
            Assert.AreEqual(0, loaded.GetPart<SkillsPart>().GetRank(nameof(CudgelSkill)));
            Assert.AreEqual(strength, loaded.GetStatValue("Strength"));
            Assert.AreEqual("CudgelSkill:1", saved.GetPart<EquippablePart>().EquipSkillBonuses);
            Assert.True(InventorySystem.Equip(loaded, saved));
            Assert.AreEqual(1, loaded.GetPart<SkillsPart>().GetRank(nameof(CudgelSkill)));
        }
    }
}
