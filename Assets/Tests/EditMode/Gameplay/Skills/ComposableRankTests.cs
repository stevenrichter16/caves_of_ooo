using System;
using System.Collections.Generic;
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
        public sealed class DoublyThrowingRankSkill : BaseSkillPart
        {
            public static int RemovalAttempts;
            public override bool AddSkill(Entity owner)
            {
                StatShifter.SetStatShift("DV", 2);
                return true;
            }
            public override ActivatedAbilitySpec DeclareActivatedAbility(Entity owner)
                => throw new InvalidOperationException("acquisition failed");
            public override bool RemoveSkill(Entity owner)
            {
                RemovalAttempts++;
                throw new InvalidOperationException("teardown also failed");
            }
        }
        [Test] public void ThrowingAcquisitionAndTeardown_ReturnsFailureAndAllowsLaterPurchase()
        {
            // Hypothesis: a failing cleanup callback must not escape the purchase
            // transaction or leave its stat effect or transient purchase gate behind.
            SkillRegistry.InitializeFromJson(@"{""Skills"":[{""Class"":""DoublyThrowingRankSkill"",""Cost"":1},{""Class"":""LongBladesSkill"",""Cost"":1}]}");
            actor.Statistics["DV"]=new Stat{Owner=actor,Name="DV",BaseValue=0};
            DoublyThrowingRankSkill.RemovalAttempts=0;
            BuySkillAction.Result result=null;
            Assert.DoesNotThrow(()=>result=BuySkillAction.Execute(actor,"DoublyThrowingRankSkill"));
            Assert.False(result.Succeeded);
            Assert.AreEqual(BuySkillAction.FailureReason.SetupFailed,result.Reason);
            Assert.AreEqual(0,result.CostPaid);
            Assert.AreEqual(19,actor.GetStatValue("SP"));
            Assert.AreEqual(0,actor.GetStatValue("DV"));
            Assert.AreEqual(1,DoublyThrowingRankSkill.RemovalAttempts);
            Assert.Null(actor.GetPart<DoublyThrowingRankSkill>());
            Assert.False(actor.GetPart<SkillsPart>().HasSkill("DoublyThrowingRankSkill"));
            Assert.AreEqual(0,actor.GetPart<ActivatedAbilitiesPart>().AbilityByGuid.Count);
            Assert.True(BuySkillAction.Execute(actor,"LongBladesSkill").Succeeded);
            Assert.AreEqual(18,actor.GetStatValue("SP"));
        }
        public sealed class RankLoadProbePart : Part
        {
            public static int LoadCalls;
            public override void OnAfterLoad(SaveReader reader) => LoadCalls++;
        }
        [TestCase(true)] [TestCase(false)]
        public void SessionDecode_RejectsDuplicateSkillClassesBeforePublication_ButAllowsDistinctClasses(bool duplicate)
        {
            // Both ranks are independently valid. Only the duplicate class makes
            // this graph malformed, and it must fail before reputation or hooks publish.
            SkillRegistry.InitializeFromJson(@"{""Skills"":[{""Class"":""LongBladesSkill"",""Cost"":1,""Powers"":[{""Class"":""LongBlades_Deflect"",""Cost"":1,""MaxRank"":3},{""Class"":""LongBlades_Riposte"",""Cost"":1,""MaxRank"":3}]}]}");
            var savedActor=new Entity{ID="saved-rank-owner"};
            savedActor.AddPart(new RankLoadProbePart());
            savedActor.AddPart(new SkillsPart());
            savedActor.AddPart(new LongBlades_Deflect{Rank=1});
            if(duplicate) savedActor.AddPart(new LongBlades_Deflect{Rank=2});
            else savedActor.AddPart(new LongBlades_Riposte{Rank=2});
            var originalReputation=new Dictionary<string,int>(PlayerReputation.GetAll());
            try
            {
                PlayerReputation.Restore(new Dictionary<string,int>{{"RankAudit",17}});
                using var stream=new MemoryStream();
                GameSessionState.Capture("rank-audit","test",null,null,savedActor).Save(new SaveWriter(stream));
                PlayerReputation.Restore(new Dictionary<string,int>{{"RankAudit",91}});
                RankLoadProbePart.LoadCalls=0;
                stream.Position=0;
                var reader=new SaveReader(stream,null);
                if(duplicate)
                {
                    Assert.Throws<InvalidDataException>(()=>GameSessionState.Load(reader));
                    Assert.AreEqual(91,PlayerReputation.GetAll()["RankAudit"]);
                    Assert.AreEqual(0,RankLoadProbePart.LoadCalls);
                }
                else
                {
                    var loaded=GameSessionState.Load(reader);
                    Assert.AreEqual(17,PlayerReputation.GetAll()["RankAudit"]);
                    Assert.AreEqual(1,RankLoadProbePart.LoadCalls);
                    Assert.AreEqual(2,loaded.Player.GetPart<SkillsPart>().SkillList.Count);
                    Assert.AreEqual(1,loaded.Player.GetPart<SkillsPart>().GetRank("LongBlades_Deflect"));
                    Assert.AreEqual(2,loaded.Player.GetPart<SkillsPart>().GetRank("LongBlades_Riposte"));
                }
            }
            finally { PlayerReputation.Restore(originalReputation); }
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
