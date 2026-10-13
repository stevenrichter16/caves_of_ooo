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

        static List<Diag.Entry> BlockedActionWindow(int blockedTurns = 2)
        {
            // The submitted action's Begin happened before its marker. Later
            // skipped turns must each carry an ordered, actor-owned witness.
            var rows = new List<Diag.Entry>
            { Row("mark","scenario",DensityCampaignNativeEvidence.MarkerKind), Row("ordinary-end","turn","End") };
            for (int i=0;i<blockedTurns;i++)
            {
                rows.Add(Row("begin-"+i,"turn","Begin"));
                rows.Add(Row("blocked-"+i,"turn","ActionBlocked","{\"phase\":\"BeginTakeAction\"}"));
                rows.Add(Row("end-"+i,"turn","End"));
            }
            rows.Add(Row("next-input-begin","turn","Begin"));
            return rows;
        }

        [TestCase("local",1)][TestCase("local",2)][TestCase("map",1)][TestCase("map",2)]
        public void WitnessedBeginVetoesExplainExtraPaidTurnsWithoutRelaxingEnergy(string kind,int blockedTurns)
        {
            var rows=BlockedActionWindow(blockedTurns);int pure=0;
            if(kind=="map"){rows.Insert(1,Map()[1]);pure=10;}
            int elapsed=(blockedTurns+1)*10+pure;
            Assert.True(DensityCampaignNativeEvidence.TryClock(rows,"mark","player",kind,1000,1000,elapsed,100,out var receipt));
            Assert.AreEqual(blockedTurns+1,receipt.CompletedTurns);Assert.AreEqual(pure,receipt.PureClock);
            Assert.False(DensityCampaignNativeEvidence.TryClock(rows,"mark","player",kind,1000,1100,elapsed,100,out _),
                "Blocked-turn witnesses cannot hide unexplained scheduler energy.");
        }

        [TestCase("wrong-actor")][TestCase("missing-begin")][TestCase("missing-end")]
        [TestCase("duplicate-block")][TestCase("invented-extra-end")][TestCase("missing-block")]
        [TestCase("end-before-block")][TestCase("wrong-phase")]
        public void ExtraPaidTurnNeedsExactlyOneOrderedBeginVetoWitness(string mutation)
        {
            var rows=BlockedActionWindow();
            if(mutation=="wrong-actor"){var row=rows[3];row.ActorId="npc";rows[3]=row;}
            if(mutation=="missing-begin")rows.RemoveAt(2);
            if(mutation=="missing-end")rows.RemoveAt(4);
            if(mutation=="duplicate-block")rows.Insert(4,Row("extra-block","turn","ActionBlocked","{\"phase\":\"BeginTakeAction\"}"));
            if(mutation=="invented-extra-end")rows.Insert(rows.Count-1,Row("invented-end","turn","End"));
            if(mutation=="missing-block")rows.RemoveAt(3);
            if(mutation=="end-before-block"){var row=rows[3];rows[3]=rows[4];rows[4]=row;}
            if(mutation=="wrong-phase"){var row=rows[3];row.PayloadJson="{\"phase\":\"Attack\"}";rows[3]=row;}
            int paidEnds=rows.Count(r=>r.Category=="turn"&&r.Kind=="End"&&r.ActorId=="player");
            Assert.False(DensityCampaignNativeEvidence.TryClock(rows,"mark","player","local",1000,1000,paidEnds*10,100,out _),
                "A balanced energy equation alone is not evidence of a legitimate skipped turn.");
        }

        [Test] public void SecondOrdinaryEndCannotMasqueradeAsAnAutomaticBlockedTurn()
        {
            var rows=BlockedActionWindow(0);rows.Insert(2,Row("unwitnessed-end","turn","End"));
            Assert.False(DensityCampaignNativeEvidence.TryClock(rows,"mark","player","local",1000,1000,20,100,out _));
        }

        public sealed class BeginVetoProbe : Part
        {
            public bool Block;
            public override bool WantEvent(int id)=>true;
            public override bool HandleEvent(GameEvent e)=>e.ID!="BeginTakeAction"||!Block;
        }

        [TestCase(true)][TestCase(false)]
        public void SchedulerRecordsActualBeginVetoBetweenItsOwnedBoundaries(bool blocked)
        {
            var previousTurns=TurnManager.Active;var previousWorld=TurnManager.World;
            bool previousChannel=Diag.IsChannelEnabled("turn");
            try
            {
                TurnManager.World=null;Diag.SetChannel("turn",true);
                var actor=new Entity{ID=Guid.NewGuid().ToString("N"),BlueprintName="NativeClockProbe"};
                actor.SetTag("Player");actor.AddPart(new BeginVetoProbe{Block=blocked});
                var turns=new TurnManager();turns.AddEntity(actor);
                Assert.AreSame(actor,turns.ProcessUntilPlayerTurn());
                var rows=Diag.Snapshot(Diag.BufferCapacity).Where(r=>r.Category=="turn"&&r.ActorId==actor.ID).ToArray();
                CollectionAssert.AreEqual(blocked?new[]{"Begin","ActionBlocked","End"}:new[]{"Begin"},rows.Select(r=>r.Kind));
                if(blocked)StringAssert.Contains("\"phase\":\"BeginTakeAction\"",rows[1].PayloadJson);
                Assert.AreEqual(blocked?20:10,turns.TickCount);Assert.AreEqual(TurnManager.ActionThreshold,turns.GetEnergy(actor));
            }
            finally
            {
                TurnManager.World=previousWorld;Diag.SetChannel("turn",previousChannel);
                typeof(TurnManager).GetProperty("Active").SetValue(null,previousTurns);
            }
        }
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
