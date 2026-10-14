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
        [TestCase(0,0,30)] [TestCase(1,15,45)] [TestCase(2,22,52)] [TestCase(3,28,58)]
        public void EnGardeSeparatesCurrentDefenseFromGuardedTotalWithoutDoubleAdding(int rank,int passive,int guarded)
        {
            using(var f=new FiftySecondCombatFixture())
            {
                var reader=f.Actor(new LongBlades_EnGarde());
                if(rank>0)
                {
                    var deflect=new LongBlades_Deflect();
                    FiftySecondCombatFixture.Learn(reader,deflect);deflect.Rank=rank;
                }
                string preview="While guarding: "+guarded+"% parry (+30 points).";
                string inactive=AbilityDetailsBuilder.BuildForSkill(reader,"LongBlades_EnGarde");
                StringAssert.Contains("Defense now (guard inactive): "+passive+"%",inactive);
                StringAssert.Contains(preview,inactive);
                var guard=new EnGardeEffect();Assert.True(reader.ApplyEffect(guard,reader,f.Zone));
                string active=AbilityDetailsBuilder.BuildForSkill(reader,"LongBlades_EnGarde");
                StringAssert.Contains("Defense now (guard active): "+guarded+"%",active);
                StringAssert.Contains(preview,active);
                StringAssert.DoesNotContain("While guarding: "+(guarded+30)+"%",active);
                Assert.False(guard.Spent,"Reading the preview must not spend the guard.");
                Assert.AreEqual(2,guard.Duration);
                StringAssert.Contains(preview,AbilityDetailsBuilder.BuildForAbility(reader,
                    reader.GetPart<LongBlades_EnGarde>().ActivatedAbilityID));
            }
        }
        [TestCase(true)] [TestCase(false)]
        public void InactiveGuardObjectCannotMakePreviewClaimDefenseIsActive(bool spent)
        {
            using(var f=new FiftySecondCombatFixture())
            {
                var reader=f.Actor(new LongBlades_EnGarde());var guard=new EnGardeEffect();
                Assert.True(reader.ApplyEffect(guard,reader,f.Zone));
                if(spent)guard.Spent=true;else guard.Duration=0;
                string text=AbilityDetailsBuilder.BuildForSkill(reader,"LongBlades_EnGarde");
                StringAssert.Contains("Defense now (guard inactive): 0%",text);
                StringAssert.Contains("While guarding: 30% parry (+30 points).",text);
                StringAssert.DoesNotContain("Defense now (guard active)",text);
            }
        }
        [TestCase(true)] [TestCase(false)]
        public void GuardPreviewKeepsCurrentWeaponAndActionRefusals(bool missingBlade)
        {
            using(var f=new FiftySecondCombatFixture())
            {
                var reader=f.Actor(new LongBlades_EnGarde());
                if(missingBlade)BladeDefense.FindLongBlade(reader).Attributes="Cudgel";
                else Assert.True(reader.ApplyEffect(new StunnedEffect()));
                string text=AbilityDetailsBuilder.BuildForSkill(reader,"LongBlades_EnGarde");
                StringAssert.Contains("Defense now (guard inactive): "+
                    (missingBlade?"needs a held long blade.":"unable to react."),text);
                StringAssert.DoesNotContain("Defense now (guard inactive): 30%",text);
                StringAssert.Contains("While guarding: 30% parry (+30 points).",text);
            }
        }
        [Test] public void GuardActivationPreviewBelongsToEnGardeDetailsOnly()
        {
            StringAssert.DoesNotContain("While guarding:",AbilityDetailsBuilder.BuildForSkill(actor,"LongBlades_Deflect"));
            StringAssert.DoesNotContain("While guarding:",AbilityDetailsBuilder.BuildForSkill(actor,"LongBlades_Riposte"));
            StringAssert.DoesNotContain("guard inactive",AbilityDetailsBuilder.BuildForSkill(actor,"Axe_Decapitate"));
        }
    }
}
