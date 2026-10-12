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
            Assert.AreEqual(13,b.Disciplines.Count);
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
    }
}
