using System;
using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Scenarios.Custom;
namespace CavesOfOoo.Tests
{
    public sealed class ReferenceGladeCombatEvidenceTests
    {
        static Diag.Entry Row(string id,string kind,string actor="player",string target="target",string cause="swing",string json=null)
            =>new Diag.Entry{TraceId=id,Category="damage",Kind=kind,ActorId=actor,TargetId=target,CauseTraceId=cause,PayloadJson=json};
        static List<Diag.Entry> Valid(bool lethal=false)
        {
            var marker=Row("start",ReferenceGladeCombatEvidence.MarkerKind);marker.Category="scenario";
            return new List<Diag.Entry>{marker,Row("hit","HitRoll"),Row("damage","DamageDealt",json:lethal?"{\"amount\":4,\"hpAfter\":0,\"lethal\":true}":"{\"amount\":4,\"hpAfter\":11,\"lethal\":false}"),Row("response","HitRoll","target","player","retaliation")};
        }
        static ReferenceGladeCombatEvidence.Result Inspect(List<Diag.Entry> rows,string marker="start")
            =>ReferenceGladeCombatEvidence.Inspect(rows,marker,"player","target");
        [TestCase(false)][TestCase(true)]public void ActualSameCausePlayerDamageAndTargetResponseAreRecognized(bool lethal)
        {var result=Inspect(Valid(lethal));Assert.True(result.WindowValid);Assert.True(result.PlayerAttempt);Assert.True(result.PlayerDamage);Assert.AreEqual(lethal,result.PlayerLethal);Assert.True(result.HostileAttempt);Assert.AreEqual("swing",result.DamageCause);}
        [Test]public void MissIsAnAttemptButNeverDamageOrLethal()
        {var rows=Valid();rows.RemoveAt(2);var result=Inspect(rows);Assert.True(result.WindowValid);Assert.True(result.PlayerAttempt);Assert.False(result.PlayerDamage);Assert.False(result.PlayerLethal);}
        [TestCase("npc-actor")][TestCase("wrong-target")][TestCase("different-cause")][TestCase("missing-cause")][TestCase("missing-hit")][TestCase("damage-before-hit")][TestCase("stale-before-marker")][TestCase("missing-damage-id")][TestCase("wrong-category")]
        public void ForeignOrUncorrelatedDamageCannotBecomePlayerCombat(string mutation)
        {
            var rows=Valid(true);var damage=rows[2];
            switch(mutation){case "npc-actor":damage.ActorId="warden";break;case "wrong-target":damage.TargetId="other";break;case "different-cause":damage.CauseTraceId="other";break;case "missing-cause":damage.CauseTraceId=null;break;case "missing-damage-id":damage.TraceId=null;break;case "wrong-category":damage.Category="effect";break;}
            rows[2]=damage;
            if(mutation=="missing-hit")rows.RemoveAt(1);
            if(mutation=="damage-before-hit"){rows.RemoveAt(2);rows.Insert(1,damage);}
            if(mutation=="stale-before-marker"){rows.RemoveAt(2);rows.Insert(0,damage);}
            var result=Inspect(rows);Assert.False(result.PlayerDamage);Assert.False(result.PlayerLethal);
        }
        [TestCase("{}")] [TestCase("not json")] [TestCase("{\"amount\":0,\"hpAfter\":0,\"lethal\":true}")]
        [TestCase("{\"amount\":-3,\"hpAfter\":0,\"lethal\":true}")] [TestCase("{\"amount\":\"4\",\"hpAfter\":0,\"lethal\":true}")]
        public void MalformedOrNonpositiveDamageIsNotCounted(string json)
        {var rows=Valid(true);var d=rows[2];d.PayloadJson=json;rows[2]=d;Assert.False(Inspect(rows).PlayerDamage);Assert.False(Inspect(rows).PlayerLethal);}
        [TestCase("{\"amount\":4,\"hpAfter\":2,\"lethal\":true}")][TestCase("{\"amount\":4,\"lethal\":true}")][TestCase("{\"amount\":4,\"hpAfter\":0,\"lethal\":false}")]
        public void LethalRequiresExplicitZeroHealthAndTrueFlag(string json)
        {var rows=Valid();var d=rows[2];d.PayloadJson=json;rows[2]=d;var result=Inspect(rows);Assert.True(result.PlayerDamage);Assert.False(result.PlayerLethal);}
        [TestCase("missing")][TestCase("duplicate")][TestCase("wrong-kind")][TestCase("foreign-actor")]
        public void MissingRotatedOrUnboundMarkerInvalidatesWholeWindow(string mutation)
        {var rows=Valid(true);if(mutation=="missing")rows.RemoveAt(0);else if(mutation=="duplicate")rows.Insert(1,rows[0]);else{var mark=rows[0];if(mutation=="wrong-kind")mark.Kind="other";else mark.ActorId="npc";rows[0]=mark;}var r=Inspect(rows);Assert.False(r.WindowValid);Assert.False(r.PlayerAttempt);Assert.False(r.PlayerDamage);Assert.False(r.PlayerLethal);Assert.False(r.HostileAttempt);}
        [Test]public void NpcKillAfterPlayerWoundDoesNotBecomePlayerDefeat()
        {var rows=Valid();rows.Add(Row("npc-kill","DamageDealt","warden","target","npc","{\"amount\":20,\"hpAfter\":0,\"lethal\":true}"));var r=Inspect(rows);Assert.True(r.PlayerDamage);Assert.False(r.PlayerLethal);}
        [Test]public void OtherHostileResponseCannotImpersonateSelectedTarget()
        {var rows=Valid();var r=rows[3];r.ActorId="other";rows[3]=r;Assert.False(Inspect(rows).HostileAttempt);}

        static List<Diag.Entry> TonicRows()
        {
            var marker = Row("support", "ReferenceGladeCombatSupportStart", "player", "player");
            marker.Category = "scenario";
            var tonic = Row("tonic", "TonicApplied", "player", "player", null,
                "{\"item\":\"starter-tonic\",\"consumed\":true,\"healing\":\"4d6+4\"}");
            tonic.Category = "event";
            return new List<Diag.Entry> { marker, tonic };
        }
        static bool TonicReceipt(List<Diag.Entry> rows, int before = 2, int after = 1)
        {
            var method = typeof(ReferenceGladeCombatEvidence).GetMethod("HasConsumedTonic");
            return method != null && (bool)method.Invoke(null,
                new object[] { rows, "support", "player", "starter-tonic", before, after });
        }
        [TestCase(2, 1)] [TestCase(1, 0)]
        public void FreshExactOwnTonicAndOneActualUnitProveConsumption(int before, int after)
        { Assert.True(TonicReceipt(TonicRows(), before, after)); }

        [TestCase("stale")] [TestCase("foreign-user")] [TestCase("foreign-recipient")]
        [TestCase("other-item")] [TestCase("unconsumed")] [TestCase("string-flag")]
        [TestCase("wrong-category")] [TestCase("missing-id")] [TestCase("duplicate-event")]
        [TestCase("duplicate-marker")] [TestCase("wrong-marker")] [TestCase("malformed")]
        public void RefusedForeignOrUnboundTonicReceiptCannotCount(string mutation)
        {
            var rows = TonicRows(); var tonic = rows[1];
            switch (mutation)
            {
                case "foreign-user": tonic.ActorId = "npc"; break;
                case "foreign-recipient": tonic.TargetId = "npc"; break;
                case "other-item": tonic.PayloadJson = "{\"item\":\"new-tonic\",\"consumed\":true}"; break;
                case "unconsumed": tonic.PayloadJson = "{\"item\":\"starter-tonic\",\"consumed\":false}"; break;
                case "string-flag": tonic.PayloadJson = "{\"item\":\"starter-tonic\",\"consumed\":\"true\"}"; break;
                case "wrong-category": tonic.Category = "damage"; break;
                case "missing-id": tonic.TraceId = null; break;
                case "malformed": tonic.PayloadJson = "not json"; break;
            }
            rows[1] = tonic;
            if (mutation == "stale") { rows.RemoveAt(1); rows.Insert(0, tonic); }
            if (mutation == "duplicate-event") rows.Add(tonic);
            if (mutation == "duplicate-marker") rows.Insert(1, rows[0]);
            if (mutation == "wrong-marker") { var marker = rows[0]; marker.Kind = "other"; rows[0] = marker; }
            Assert.False(TonicReceipt(rows));
        }
        [TestCase(2, 2)] [TestCase(2, 0)] [TestCase(0, -1)]
        public void TonicEventWithoutExactOneUnitChangeCannotCount(int before, int after)
        { Assert.False(TonicReceipt(TonicRows(), before, after)); }
    }
}
