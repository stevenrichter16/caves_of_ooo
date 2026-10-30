using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    public class SkillBrowserModelTests
    {
        Entity actor;
        [SetUp] public void Setup()
        {
            SkillRegistry.ResetForTests(); SkillRegistry.EnsureInitialized();
            actor = new Entity { ID = "browser-test" };
            actor.Statistics["SP"] = new Stat { Owner=actor, Name="SP", BaseValue=10 };
            actor.Statistics["Agility"] = new Stat { Owner=actor, Name="Agility", BaseValue=18 };
            actor.AddPart(new ActivatedAbilitiesPart()); actor.AddPart(new SkillsPart());
        }
        [TearDown] public void Cleanup() => SkillRegistry.ResetForTests();
        [Test] public void OpenAndBrowse_DoNotTeachOrSpend()
        {
            var b=new SkillBrowserModel(); b.Open(actor);
            Assert.AreEqual(14,b.Disciplines.Count);
            Assert.True(b.Disciplines.Exists(d => d.Class == "ThrowingSkill"),
                "The shipped throwing discipline must remain visible when browsing.");
            b.SelectClass("Acrobatics_Vault");
            StringAssert.Contains("Agility 18 / 15",b.Requirements);
            Assert.AreEqual(10,actor.GetStatValue("SP"));
            Assert.False(actor.GetPart<SkillsPart>().HasSkill("Acrobatics_Vault"));
        }
        [Test] public void ReviewCancelAndConfirm_OnlyConfirmationSpends()
        {
            var b=new SkillBrowserModel(); b.Open(actor); b.SelectClass("AcrobaticsSkill");
            Assert.True(b.ReviewPurchase()); Assert.AreEqual(10,actor.GetStatValue("SP"));
            b.Cancel(); Assert.False(actor.GetPart<SkillsPart>().HasSkill("AcrobaticsSkill"));
            b.ReviewPurchase(); Assert.True(b.ConfirmPurchase());
            Assert.AreEqual(9,actor.GetStatValue("SP"));
            Assert.False(b.ConfirmPurchase()); Assert.AreEqual(9,actor.GetStatValue("SP"));
        }
        [Test] public void ConfirmationRevalidatesPoints()
        {
            var b=new SkillBrowserModel(); b.Open(actor); b.SelectClass("AcrobaticsSkill"); b.ReviewPurchase();
            actor.GetStat("SP").BaseValue=0;
            Assert.False(b.ConfirmPurchase()); Assert.False(actor.GetPart<SkillsPart>().HasSkill("AcrobaticsSkill"));
        }
        [Test] public void BookOwnedVault_CanAssignWithoutOwningTree()
        {
            actor.GetPart<SkillsPart>().AddSkill("Acrobatics_Vault");
            var b=new SkillBrowserModel(); b.Open(actor); b.SelectClass("Acrobatics_Vault");
            Assert.True(b.CanAssign); Assert.False(b.ReviewPurchase());
            Assert.True(b.Assign(9));
            Assert.AreEqual("Acrobatics_Vault",actor.GetPart<ActivatedAbilitiesPart>().GetAbilityBySlot(9).SourcePowerClass);
            b.SelectClass("AcrobaticsDodgePower"); Assert.False(b.CanAssign); Assert.False(b.Assign(9));
        }
        [Test] public void OverviewEdgesAreReal_IndependentSiblingsAreNotChained()
        {
            var b=new SkillBrowserModel(); b.Open(actor); b.SelectClass("Acrobatics_Vault");
            CollectionAssert.AreEqual(new[]{"AcrobaticsSkill"},b.Prerequisites);
            b.SelectClass("Axe_Decapitate");
            CollectionAssert.Contains(b.Prerequisites,"Axe_Dismember");
        }
        [Test] public void ReopenAndViewChangeRetainSelection()
        {
            var b=new SkillBrowserModel(); b.Open(actor); b.SelectClass("Acrobatics_Vault");
            b.Overview=true; b.Open(actor);
            Assert.AreEqual("Acrobatics_Vault",b.Selected.Class); Assert.True(b.Overview);
        }

        [Test] public void GearRanks_ShowEffectiveCapWithoutAdvancingLearnedRankOrCost()
        {
            var skills=actor.GetPart<SkillsPart>();
            skills.AddSkill("LongBladesSkill");
            Assert.True(BuySkillAction.Execute(actor,"LongBlades_Deflect").Succeeded);
            var receipt=SkillModifiers.Apply(actor,SkillModifiers.Parse("LongBlades_Deflect:3"));
            int points=actor.GetStatValue("SP");
            var b=new SkillBrowserModel(); b.Open(actor); b.SelectClass("LongBlades_Deflect");

            Assert.AreEqual(1,b.Selected.OwnedRank);
            Assert.AreEqual(3,b.Selected.EffectiveRank);
            Assert.AreEqual(3,b.Selected.BonusRank);
            Assert.True(b.Selected.CanUpgrade);
            StringAssert.Contains("GEAR +3",b.StateLabel(b.Selected));
            StringAssert.Contains("Rank: 3/3",b.Details);
            StringAssert.Contains("Learned: 1. Gear: +3 while equipped.",b.Details);
            StringAssert.Contains("Rank capped at 3.",b.Details);
            StringAssert.Contains("Current: 28%",b.Details);
            StringAssert.Contains("Next learned rank 2: 22%",b.Details);
            Assert.True(b.ReviewPurchase());
            Assert.AreEqual(1,b.PendingRank);
            Assert.AreEqual(1,b.PendingCost);
            b.Cancel();
            Assert.AreEqual(points,actor.GetStatValue("SP"));
            SkillModifiers.Remove(actor,receipt); b.Refresh();
            Assert.AreEqual(1,b.Selected.EffectiveRank);
            StringAssert.DoesNotContain("Gear:",b.Details);
            StringAssert.Contains("Current: 15%",b.Details);
        }

        [Test] public void GearOnlyActiveSkill_CanAssignButDoesNotClaimPermanentLearning()
        {
            var receipt=SkillModifiers.Apply(actor,SkillModifiers.Parse("Acrobatics_Vault:1"));
            var b=new SkillBrowserModel(); b.Open(actor); b.SelectClass("Acrobatics_Vault");
            Assert.AreEqual(0,b.Selected.OwnedRank);
            Assert.AreEqual(1,b.Selected.EffectiveRank);
            Assert.True(b.Selected.CanLearn);
            Assert.True(b.CanAssign);
            Assert.True(b.Assign(9));
            StringAssert.Contains("Learned: 0. Gear: +1 while equipped.",b.Details);
            Assert.False(b.ReviewPurchase(),"The unlearned parent still gates a permanent purchase.");
            SkillModifiers.Remove(actor,receipt); b.Refresh();
            Assert.False(b.CanAssign);
            Assert.IsNull(actor.GetPart<ActivatedAbilitiesPart>().GetAbilityBySlot(9));
            Assert.AreEqual(10,actor.GetStatValue("SP"));
        }

        [Test] public void GearOnlyPrerequisite_IsShownAsStillNeedingLearning()
        {
            SkillModifiers.Apply(actor,SkillModifiers.Parse("AcrobaticsSkill:1"));
            var b=new SkillBrowserModel(); b.Open(actor); b.SelectClass("Acrobatics_Vault");
            StringAssert.Contains("- Acrobatics (gear only; learn first)",b.Requirements);
            Assert.False(b.ReviewPurchase());
            Assert.AreEqual(10,actor.GetStatValue("SP"));
        }

        [Test] public void LearningGearOnlySkill_PurchasesFirstRankAndSurvivesUnequipping()
        {
            var receipt=SkillModifiers.Apply(actor,SkillModifiers.Parse("LongBlades_Deflect:1"));
            actor.GetPart<SkillsPart>().AddSkill("LongBladesSkill");
            var b=new SkillBrowserModel(); b.Open(actor); b.SelectClass("LongBlades_Deflect");
            Assert.True(b.Selected.CanLearn);
            Assert.False(b.Selected.CanUpgrade);
            Assert.True(b.ReviewPurchase());
            Assert.AreEqual(0,b.PendingRank);
            Assert.True(b.ConfirmPurchase());
            Assert.AreEqual(1,b.Selected.OwnedRank);
            Assert.AreEqual(2,b.Selected.EffectiveRank);
            Assert.AreEqual(9,actor.GetStatValue("SP"));
            SkillModifiers.Remove(actor,receipt); b.Refresh();
            Assert.AreEqual(1,b.Selected.OwnedRank);
            Assert.AreEqual(1,b.Selected.EffectiveRank);
            Assert.AreEqual(0,b.Selected.BonusRank);
        }
    }
}
