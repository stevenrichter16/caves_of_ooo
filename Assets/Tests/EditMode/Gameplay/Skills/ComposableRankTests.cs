using System;
using System.IO;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using CavesOfOoo.Rendering;

namespace CavesOfOoo.Tests
{
    public class ComposableRankTests
    {
        Entity actor;
        const string Power="Acrobatics_Vault";
        [SetUp] public void Setup()
        {
            SkillRegistry.InitializeFromJson(@"{""Skills"":[{""Name"":""Acrobatics"",""Class"":""AcrobaticsSkill"",""Cost"":1,""Powers"":[{""Name"":""Vault"",""Class"":""Acrobatics_Vault"",""Cost"":1,""MaxRank"":3,""RankCosts"":[1,2,3]}]}]}");
            actor=new Entity{ID="rank-test"};
            actor.Statistics["SP"]=new Stat{Owner=actor,Name="SP",BaseValue=20};
            actor.AddPart(new ActivatedAbilitiesPart());actor.AddPart(new SkillsPart());
            Assert.True(BuySkillAction.Execute(actor,"AcrobaticsSkill").Succeeded);
        }
        [TearDown] public void Cleanup()=>SkillRegistry.ResetForTests();
        [Test] public void UpgradesSpendNextCost_OnePartAndAbility_CooldownAndSlotSurvive()
        {
            var skills=actor.GetPart<SkillsPart>();
            Assert.True(BuySkillAction.Execute(actor,Power).Succeeded);
            var part=skills.GetSkill(Power);var id=part.ActivatedAbilityID;
            var abilities=actor.GetPart<ActivatedAbilitiesPart>();var ability=abilities.GetAbility(id);
            ability.CooldownRemaining=7;abilities.AssignAbilityToSlot(id,9);
            for(int rank=2;rank<=3;rank++)
            {
                var result=BuySkillAction.Execute(actor,Power,rank-1);
                Assert.True(result.Succeeded);Assert.AreEqual(rank,result.CostPaid);
                Assert.AreEqual(rank-1,result.OldRank);Assert.AreEqual(rank,result.NewRank);
                Assert.AreSame(part,skills.GetSkill(Power));Assert.AreEqual(rank,skills.GetRank(Power));
                Assert.AreEqual(id,part.ActivatedAbilityID);Assert.AreEqual(7,ability.CooldownRemaining);
                Assert.AreSame(ability,abilities.GetAbilityBySlot(9));
            }
            Assert.AreEqual(13,actor.GetStatValue("SP"));Assert.AreEqual(2,skills.SkillList.Count);
            Assert.False(BuySkillAction.Execute(actor,Power).Succeeded);Assert.AreEqual(13,actor.GetStatValue("SP"));
        }
        [Test] public void DuplicateGrantDoesNotUpgrade_AndAbsentRankIsZero()
        {
            var skills=actor.GetPart<SkillsPart>();Assert.AreEqual(0,skills.GetRank(Power));
            Assert.True(skills.AddSkill(Power,"book"));Assert.False(skills.AddSkill(Power,"book"));
            Assert.AreEqual(1,skills.GetRank(Power));Assert.AreEqual(19,actor.GetStatValue("SP"));
        }
        [Test] public void UpgradeRechecksParentAndSP_WithoutDeletingBookOwnership()
        {
            var skills=actor.GetPart<SkillsPart>();skills.AddSkill(Power,"book");skills.RemoveSkill(skills.GetSkill("AcrobaticsSkill"));
            Assert.AreEqual(BuySkillAction.FailureReason.MissingPrereq,BuySkillAction.Execute(actor,Power).Reason);
            Assert.AreEqual(1,skills.GetRank(Power));skills.AddSkill("AcrobaticsSkill");actor.GetStat("SP").BaseValue=1;
            Assert.AreEqual(BuySkillAction.FailureReason.InsufficientSP,BuySkillAction.Execute(actor,Power).Reason);
            Assert.AreEqual(1,skills.GetRank(Power));Assert.AreEqual(1,actor.GetStatValue("SP"));
        }
        [Test] public void BrowserRetainsOwnership_AssignmentAndCancel_RejectsStaleRank()
        {
            BuySkillAction.Execute(actor,Power);var browser=new SkillBrowserModel();browser.Open(actor);browser.SelectClass(Power);
            Assert.AreEqual(SkillsScreenRowState.Owned,browser.Selected.State);Assert.True(browser.CanAssign);
            StringAssert.Contains("1/3",browser.StateLabel(browser.Selected));Assert.True(browser.ReviewPurchase());
            browser.Cancel();Assert.False(browser.ConfirmPurchase());Assert.AreEqual(18,actor.GetStatValue("SP"));
            Assert.True(browser.ReviewPurchase());BuySkillAction.Execute(actor,Power);
            Assert.False(browser.ConfirmPurchase());Assert.AreEqual(2,actor.GetPart<SkillsPart>().GetRank(Power));
            Assert.AreEqual(16,actor.GetStatValue("SP"));Assert.True(browser.ReviewPurchase());Assert.True(browser.ConfirmPurchase());
            StringAssert.Contains("MAX 3/3",browser.StateLabel(browser.Selected));Assert.False(browser.ConfirmPurchase());
        }
        [Test] public void BrowserRejectsChangedCost_ThenCanReviewNewPrice()
        {
            BuySkillAction.Execute(actor,Power);var browser=new SkillBrowserModel();browser.Open(actor);browser.SelectClass(Power);browser.ReviewPurchase();
            SkillRegistry.TryGetPowerByClass(Power,out var data);data.RankCosts[1]=4;
            Assert.False(browser.ConfirmPurchase());Assert.AreEqual(18,actor.GetStatValue("SP"));
            Assert.True(browser.ReviewPurchase());Assert.True(browser.ConfirmPurchase());Assert.AreEqual(14,actor.GetStatValue("SP"));
        }
        [Test] public void SaveRoundTrip_RestoresRankAndSingleAuthoritativePart()
        {
            BuySkillAction.Execute(actor,Power);BuySkillAction.Execute(actor,Power);
            var loaded=PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);var skills=loaded.GetPart<SkillsPart>();
            Assert.AreEqual(2,skills.GetRank(Power));Assert.AreEqual(2,skills.SkillList.Count);
            Assert.AreSame(loaded.GetPart<Acrobatics_Vault>(),skills.GetSkill(Power));
            Assert.AreEqual(actor.GetPart<SkillsPart>().GetSkill(Power).ActivatedAbilityID,skills.GetSkill(Power).ActivatedAbilityID);
        }
        [TestCase(0)] [TestCase(4)] public void InvalidRankRejectedDuringGraphDecode(int rank)
        {
            actor.GetPart<SkillsPart>().AddSkill(Power);actor.GetPart<SkillsPart>().GetSkill(Power).Rank=rank;
            Assert.Throws<InvalidDataException>(()=>PartRoundTripHelper.RoundTripEntity(actor));
        }
        [Test] public void UpgradedSkillCanBeRemoved_WithoutTouchingOtherSkills()
        {
            BuySkillAction.Execute(actor,Power);BuySkillAction.Execute(actor,Power);
            var skills=actor.GetPart<SkillsPart>();var part=skills.GetSkill(Power);var id=part.ActivatedAbilityID;
            Assert.True(skills.RemoveSkill(part));Assert.AreEqual(0,skills.GetRank(Power));
            Assert.Null(actor.GetPart<ActivatedAbilitiesPart>().GetAbility(id));
            Assert.True(skills.HasSkill("AcrobaticsSkill"));Assert.AreEqual(1,skills.SkillList.Count);
        }
        public sealed class ThrowingRankSkill : BaseSkillPart
        {
            public override ActivatedAbilitySpec DeclareActivatedAbility(Entity owner)=>throw new InvalidOperationException("test setup failure");
        }
        [Test] public void ThrowingAcquisitionRefundsSP_AndDetachesFailedPart()
        {
            SkillRegistry.InitializeFromJson(@"{""Skills"":[{""Class"":""ThrowingRankSkill"",""Cost"":1}]}");
            BuySkillAction.Result result=null;
            Assert.DoesNotThrow(()=>result=BuySkillAction.Execute(actor,"ThrowingRankSkill"));
            Assert.False(result.Succeeded);Assert.AreEqual(19,actor.GetStatValue("SP"));
            Assert.False(actor.GetPart<SkillsPart>().HasSkill("ThrowingRankSkill"));Assert.Null(actor.GetPart<ThrowingRankSkill>());
        }
        [Test] public void InvalidRankMetadataIsRejectedAtIngestion()
        {
            UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Warning,new System.Text.RegularExpressions.Regex("invalid rank metadata"));
            SkillRegistry.InitializeFromJson(@"{""Skills"":[{""Class"":""AcrobaticsSkill"",""Cost"":1,""Powers"":[{""Class"":""Acrobatics_Vault"",""Cost"":1,""MaxRank"":3,""RankCosts"":[1]}]}]}");
            Assert.False(SkillRegistry.TryGetPowerByClass(Power,out _));
        }
        [Test] public void UnrankedRootStillOnlyBuysOnce()
        {Assert.False(BuySkillAction.Execute(actor,"AcrobaticsSkill").Succeeded);Assert.AreEqual(19,actor.GetStatValue("SP"));}
        [Test] public void MalformedCostsCannotGrantFreeUpgrade()
        {
            actor.GetPart<SkillsPart>().AddSkill(Power);SkillRegistry.TryGetPowerByClass(Power,out var data);data.RankCosts=new[]{1};
            Assert.False(BuySkillAction.Execute(actor,Power).Succeeded);Assert.AreEqual(1,actor.GetPart<SkillsPart>().GetRank(Power));
        }
    }
}
