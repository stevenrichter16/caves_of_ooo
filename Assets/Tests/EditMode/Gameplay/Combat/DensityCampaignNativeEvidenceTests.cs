using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Scenarios.Custom;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class DensityCampaignNativeEvidenceTests
    {
        static Diag.Entry Row(string id,string category,string kind,string payload=null,string actor="player")
            =>new Diag.Entry{TraceId=id,Category=category,Kind=kind,ActorId=actor,PayloadJson=payload,Turn=100};
        static List<Diag.Entry> Map()=>new List<Diag.Entry>{Row("mark","scenario","DensityCampaignActionStart"),
            Row("step","worldmap","Stepped","{\"turnsCost\":10,\"toWorldX\":3,\"toWorldY\":6}"),Row("end","turn","End")};
        static List<Diag.Entry> Rest()=>new List<Diag.Entry>{Row("mark","scenario","DensityCampaignActionStart"),
            Row("rest","furniture","Rested","{\"site\":\"bed\",\"healed\":17,\"clockAdvanced\":60}")};
        [Test] public void NativeMapTravelReceiptSubtractsOnlyItsActualPureClock()
        {Assert.True(DensityCampaignNativeEvidence.TryClock(Map(),"mark","player","map",1000,1000,20,100,out var r));Assert.AreEqual(1,r.CompletedTurns);Assert.AreEqual(10,r.PureClock);}
        [Test] public void RecoveryReceiptUsesActualHealthDeltaAndNoSyntheticEnergyTurns()
        {Assert.True(DensityCampaignNativeEvidence.TryClock(Rest(),"mark","player","rest",1000,1000,60,100,out var r));Assert.AreEqual(0,r.CompletedTurns);Assert.AreEqual(60,r.PureClock);Assert.True(DensityCampaignNativeEvidence.RecoveryMatches(r,"bed",23,40,40));Assert.False(DensityCampaignNativeEvidence.RecoveryMatches(r,"bed",40,40,40));}
        [Test] public void PureRestCannotMintSchedulerEnergyBetweenItsOwnReceiptAndObservation()
        {Assert.False(DensityCampaignNativeEvidence.TryClock(Rest(),"mark","player","rest",1000,2000,70,100,out _));}
        [TestCase("wrong-owner")][TestCase("duplicate-step")][TestCase("missing-end")][TestCase("bad-clock")][TestCase("future-marker")][TestCase("duplicate-trace")]
        public void PureClockCannotHideMissingOrForeignPaidAction(string mutation)
        {var r=Map();if(mutation=="wrong-owner"){var x=r[1];x.ActorId="npc";r[1]=x;}
            if(mutation=="duplicate-step"){var x=r[1];x.TraceId="step2";r.Add(x);}
            if(mutation=="missing-end")r.RemoveAt(2);if(mutation=="bad-clock"){var x=r[1];x.PayloadJson="{\"turnsCost\":20}";r[1]=x;}
            if(mutation=="future-marker")r.Insert(1,Row("future","scenario","DensityCampaignActionStart"));
            if(mutation=="duplicate-trace"){var x=r[1];x.TraceId="mark";r[1]=x;}
            Assert.False(DensityCampaignNativeEvidence.TryClock(r,"mark","player","map",1000,1000,20,100,out _));}
        [TestCase("ordinary-clock")][TestCase("wrong-rest-site")][TestCase("wrong-heal")][TestCase("unchanged-hp")]
        public void RestOrInventoryValueCannotMasqueradeAsRecovery(string mutation)
        {var r=Rest();if(mutation=="ordinary-clock"){Assert.False(DensityCampaignNativeEvidence.TryClock(r,"mark","player","local",1000,1000,60,100,out _));return;}
            Assert.True(DensityCampaignNativeEvidence.TryClock(r,"mark","player","rest",1000,1000,60,100,out var clock));
            Assert.False(DensityCampaignNativeEvidence.RecoveryMatches(clock,mutation=="wrong-rest-site"?"campfire":"bed",mutation=="unchanged-hp"?40:23,mutation=="wrong-heal"?39:40,40));}
        static List<Diag.Entry> Sale()=>new List<Diag.Entry>{Row("mark","scenario","DensityCampaignActionStart"),
            new Diag.Entry{TraceId="sale",Category="trade",Kind="Sold",ActorId="player",TargetId="trader",PayloadJson="{\"itemId\":\"earned\",\"price\":7,\"dramsAfter\":34}"}};
        [Test] public void RealWholeStackSaleAndPurchaseJoinOnlyActualCurrencyDeltas()
        {Assert.True(DensityCampaignNativeEvidence.TradeMatches(Sale(),"mark","player","trader","earned",false,7,27,34,100,93));
            var b=Sale();var x=b[1];x.Kind="Bought";x.PayloadJson="{\"itemId\":\"earned\",\"price\":7,\"dramsAfter\":20}";b[1]=x;
            Assert.True(DensityCampaignNativeEvidence.TradeMatches(b,"mark","player","trader","earned",true,7,27,20,100,107));}
        [TestCase("no-commit")][TestCase("wrong-item")][TestCase("wrong-owner")][TestCase("wrong-stock-purse")][TestCase("value-not-drams")][TestCase("duplicate-commit")]
        public void QuotedValueOrUnrelatedRecordCannotFundCampaignSupplies(string mutation)
        {var r=Sale();int after=34,trader=93;if(mutation=="no-commit")r.RemoveAt(1);
            if(mutation=="wrong-item"){var x=r[1];x.PayloadJson=x.PayloadJson.Replace("earned","starter");r[1]=x;}
            if(mutation=="wrong-owner"){var x=r[1];x.TargetId="other-trader";r[1]=x;}
            if(mutation=="wrong-stock-purse")trader=100;if(mutation=="value-not-drams")after=47;
            if(mutation=="duplicate-commit"){var x=r[1];x.TraceId="duplicate-sale";r.Add(x);}
            Assert.False(DensityCampaignNativeEvidence.TradeMatches(r,"mark","player","trader","earned",false,7,27,after,100,trader));}
    }
}
