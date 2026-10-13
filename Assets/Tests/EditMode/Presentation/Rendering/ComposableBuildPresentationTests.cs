using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class ComposableBuildPresentationTests
    {
        Entity actor;
        [SetUp] public void Setup(){AbilityClarityTestSupport.Reset();actor=AbilityClarityTestSupport.Actor();}
        [TearDown] public void Cleanup()=>SkillRegistry.ResetForTests();
        [TestCase("none",true)] [TestCase("LongBlades_Deflect",false)] [TestCase("LongBlades_EnGarde",false)]
        public void RiposteWarnsWithoutSource_ButPurchaseIsAllowed(string source,bool warning)
        {
            actor.GetPart<SkillsPart>().AddSkill(new LongBladesSkill());
            if(source!="none")actor.GetPart<SkillsPart>().AddSkill(source);
            var browser=new SkillBrowserModel();browser.Open(actor);browser.SelectClass("LongBlades_Riposte");
            Assert.AreEqual(warning,!string.IsNullOrEmpty(AbilityDetailsBuilder.PurchaseWarning(actor,"LongBlades_Riposte")));
            Assert.True(browser.ReviewPurchase());Assert.AreEqual(warning,!string.IsNullOrEmpty(browser.PendingWarning));
            Assert.AreEqual(100,actor.GetStatValue("SP"));browser.Cancel();Assert.AreEqual(100,actor.GetStatValue("SP"));
            browser.ReviewPurchase();Assert.True(browser.ConfirmPurchase());Assert.AreEqual(99,actor.GetStatValue("SP"));
        }
        [Test] public void RealRankDescriptionsShowCurrentAndNextNotSummed()
        {
            actor.GetPart<SkillsPart>().AddSkill(new LongBladesSkill());
            BuySkillAction.Execute(actor,"LongBlades_Deflect");
            string text=AbilityDetailsBuilder.BuildForSkill(actor,"LongBlades_Deflect");
            StringAssert.Contains("Current: 15%",text);StringAssert.Contains("Next rank 2: 22%",text);
            StringAssert.Contains("Purchase: 1 SP",text);
            BuySkillAction.Execute(actor,"LongBlades_Deflect");BuySkillAction.Execute(actor,"LongBlades_Deflect");
            text=AbilityDetailsBuilder.BuildForSkill(actor,"LongBlades_Deflect");
            StringAssert.Contains("Rank: 3/3",text);StringAssert.DoesNotContain("Next rank",text);
            StringAssert.DoesNotContain("Purchase:",text);
        }
        [Test] public void LiveAllowanceDescriptionDoesNotRenewSpentBudget()
        {
            actor.GetPart<SkillsPart>().AddSkill(new LongBlades_Riposte());
            var allowance=actor.GetPart<RiposteAllowancePart>();allowance.Spent=true;
            StringAssert.Contains("spent until your turn completes",AbilityDetailsBuilder.BuildForSkill(actor,"LongBlades_Riposte"));
            Assert.True(allowance.Spent);allowance.Spent=false;
            StringAssert.Contains("Counter allowance: available",AbilityDetailsBuilder.BuildForSkill(actor,"LongBlades_Riposte"));
        }
    }
}
