using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Scenarios.Custom;
using CavesOfOoo.Skills;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class DensityCombatNativeEvidenceTests
    {
        static Diag.Entry Row(string id,string category,string kind,string actor="enemy",string target=null,string payload=null)
            =>new Diag.Entry{TraceId=id,Category=category,Kind=kind,ActorId=actor,TargetId=target,PayloadJson=payload};
        static List<Diag.Entry> Tactic()=>new List<Diag.Entry>{
            Row("mark","scenario",DensityCombatNativeEvidence.MarkerKind,"player","enemy"),
            Row("begin","turn-verbose","Begin"),
            Row("routed","skill","CommandRouted","enemy","enemy","{\"command\":\"CommandIceLance\"}"),
            Row("used","ai","TacticUsed","enemy","player","{\"command\":\"CommandIceLance\",\"cooldown\":8}"),
            Row("end","turn-verbose","End")};
        static bool Used(List<Diag.Entry> rows)=>DensityCombatNativeEvidence.TryTacticTurn(rows,"mark","player","enemy","CommandIceLance",out _);
        static void Edit(List<Diag.Entry> rows,int i,Func<Diag.Entry,Diag.Entry> f)=>rows[i]=f(rows[i]);
        [Test] public void ActualOwnedRoutedCommandAndCommittedTacticShareOneCompletedEnemyTurn()
        {var rows=Tactic();Assert.True(DensityCombatNativeEvidence.TryTacticTurn(rows,"mark","player","enemy","CommandIceLance",out var result));Assert.AreEqual(8,result.Cooldown);Assert.AreEqual("begin",result.BeginTrace);Assert.AreEqual("end",result.EndTrace);Assert.AreEqual("used",result.ActionTrace);}
        [TestCase("missing-marker")][TestCase("wrong-marker-player")][TestCase("wrong-marker-target")][TestCase("duplicate-marker")]
        [TestCase("routed-command")][TestCase("missing-routed")][TestCase("foreign-routed-owner")][TestCase("foreign-target")]
        [TestCase("zero-cooldown")][TestCase("malformed-payload")][TestCase("duplicate-action")][TestCase("missing-end")]
        [TestCase("used-outside-turn")][TestCase("duplicate-trace")][TestCase("future-key")][TestCase("foreign-end")]
        public void TacticCountsCannotSubstituteForFreshActorTargetCommandAndTurn(string mutation)
        {
            var r=Tactic();
            if(mutation=="missing-marker")r.RemoveAt(0);
            if(mutation=="wrong-marker-player")Edit(r,0,x=>{x.ActorId="other";return x;});
            if(mutation=="wrong-marker-target")Edit(r,0,x=>{x.TargetId="other";return x;});
            if(mutation=="duplicate-marker")r.Add(r[0]);
            if(mutation=="routed-command")Edit(r,2,x=>{x.PayloadJson="{\"command\":\"CommandCalm\"}";return x;});
            if(mutation=="missing-routed")r.RemoveAt(2);
            if(mutation=="foreign-routed-owner")Edit(r,2,x=>{x.ActorId="other";return x;});
            if(mutation=="foreign-target")Edit(r,3,x=>{x.TargetId="other";return x;});
            if(mutation=="zero-cooldown")Edit(r,3,x=>{x.PayloadJson="{\"command\":\"CommandIceLance\",\"cooldown\":0}";return x;});
            if(mutation=="malformed-payload")Edit(r,3,x=>{x.PayloadJson="{";return x;});
            if(mutation=="duplicate-action")r.Insert(4,Row("used2","ai","TacticUsed","enemy","player","{\"command\":\"CommandIceLance\",\"cooldown\":8}"));
            if(mutation=="missing-end")r.RemoveAt(4);
            if(mutation=="used-outside-turn"){var used=r[3];r.RemoveAt(3);r.Add(used);}
            if(mutation=="duplicate-trace")Edit(r,2,x=>{x.TraceId="begin";return x;});
            if(mutation=="future-key")r.Insert(1,Row("next","scenario",DensityCombatNativeEvidence.MarkerKind,"player","enemy"));
            if(mutation=="foreign-end")Edit(r,4,x=>{x.ActorId="other";return x;});
            Assert.False(Used(r));
        }
        static List<Diag.Entry> Fallback(bool move=false)=>new List<Diag.Entry>{
            Row("mark","scenario",DensityCombatNativeEvidence.MarkerKind,"player","enemy"),Row("begin","turn-verbose","Begin"),
            Row("reject","ai","TacticRejected","enemy","player","{\"command\":\"CommandIceLance\",\"reason\":\"cooldown\",\"cooldown\":6}"),
            move?Row("move","scenario",DensityCombatNativeEvidence.MoveKind,"enemy",null,"{\"scheduledActor\":\"enemy\",\"forced\":false,\"oldX\":10,\"oldY\":10,\"newX\":11,\"newY\":10}")
                :Row("hit","damage","HitRoll","enemy","player"),Row("end","turn-verbose","End")};
        [TestCase(false)][TestCase(true)] public void CoolingActualEnemyPerformsOneOrdinaryAttackOrNonforcedMove(bool move)
        {Assert.True(DensityCombatNativeEvidence.TryCooldownFallback(Fallback(move),"mark","player","enemy","CommandIceLance",6,5,out var result));Assert.AreEqual(move?"move":"melee",result.Kind);}
        [TestCase("missing-action")][TestCase("wrong-hit-target")][TestCase("chance-not-cooldown")][TestCase("foreign-reject")]
        [TestCase("cooldown-not-ticked")][TestCase("cooldown-restarted")][TestCase("used-too")][TestCase("second-turn")]
        [TestCase("forced-move")][TestCase("foreign-current-actor")][TestCase("zero-move")]
        public void RefusalAloneOrUnrelatedChangeCannotClaimFallback(string mutation)
        {
            bool move=mutation.Contains("move")||mutation=="foreign-current-actor";var r=Fallback(move);int after=5;
            if(mutation=="missing-action")r.RemoveAt(3);
            if(mutation=="wrong-hit-target")Edit(r,3,x=>{x.TargetId="other";return x;});
            if(mutation=="chance-not-cooldown")Edit(r,2,x=>{x.PayloadJson="{\"command\":\"CommandIceLance\",\"reason\":\"chance\",\"cooldown\":6}";return x;});
            if(mutation=="foreign-reject")Edit(r,2,x=>{x.ActorId="other";return x;});
            if(mutation=="cooldown-not-ticked")after=6;if(mutation=="cooldown-restarted")after=8;
            if(mutation=="used-too")r.Insert(4,Row("used","ai","TacticUsed","enemy","player","{\"command\":\"CommandEmberSpit\",\"cooldown\":8}"));
            if(mutation=="second-turn"){r.Add(Row("begin2","turn-verbose","Begin"));r.Add(Row("end2","turn-verbose","End"));}
            if(mutation=="forced-move")Edit(r,3,x=>{x.PayloadJson=x.PayloadJson.Replace("false","true");return x;});
            if(mutation=="foreign-current-actor")Edit(r,3,x=>{x.PayloadJson=x.PayloadJson.Replace("\"enemy\"","\"other\"");return x;});
            if(mutation=="zero-move")Edit(r,3,x=>{x.PayloadJson=x.PayloadJson.Replace("\"newX\":11","\"newX\":10");return x;});
            Assert.False(DensityCombatNativeEvidence.TryCooldownFallback(r,"mark","player","enemy","CommandIceLance",6,after,out _));
        }
        [Test] public void BlockedPlayerEndsAreCountedInsteadOfAssumingOneActionPerKey()
        {
            var r=Fallback();r.Insert(1,Row("playerEnd","turn","End","player"));r.Add(Row("playerBegin","turn","Begin","player"));r.Add(Row("blockedEnd","turn","End","player"));
            Assert.AreEqual(2,DensityCombatNativeEvidence.CountEnds(r,"mark","player","enemy","player"));
            Assert.True(DensityCombatNativeEvidence.EnergyMatches(1000,1000,20,100,2));
            Assert.False(DensityCombatNativeEvidence.EnergyMatches(1000,1000,20,100,1));
        }
        [TestCase(-1,0,1,100,1)][TestCase(1000,0,-1,100,1)][TestCase(1000,0,0,0,1)][TestCase(1000,0,0,100,-1)]
        public void InvalidEnergyInputsRefuse(int before,int after,int ticks,int speed,int turns)
        {Assert.False(DensityCombatNativeEvidence.EnergyMatches(before,after,ticks,speed,turns));}
        static Entity Actor(Zone zone)
        {
            var e=new Entity{ID="enemy",BlueprintName="IceWight"};e.SetTag("Creature");
            e.Statistics["Hitpoints"]=new Stat{Name="Hitpoints",BaseValue=24,Max=24};
            e.AddPart(new BrainPart());zone.AddEntity(e,10,10);e.GetPart<BrainPart>().CurrentZone=zone;return e;
        }
        [Test] public void CalmWitnessUsesExactCurrentTopStationaryLiveGoal()
        {var z=new Zone("test");var e=Actor(z);var goal=new NoFightGoal(50,false);e.GetPart<BrainPart>().PushGoal(goal);Assert.True(DensityCombatNativeEvidence.CurrentCalm(e,z,goal));}
        [TestCase("finished")][TestCase("buried")][TestCase("wander")][TestCase("removed")][TestCase("dead")][TestCase("foreign-zone")][TestCase("different-goal")]
        public void StalePacificationCannotSuspendThreatPolicy(string mutation)
        {
            var z=new Zone("test");var e=Actor(z);var g=new NoFightGoal(50,false);var b=e.GetPart<BrainPart>();b.PushGoal(g);
            if(mutation=="finished")g.Age=50;if(mutation=="buried")b.PushGoal(new NoFightGoal(10,false));
            if(mutation=="wander")g.Wander=true;if(mutation=="removed")z.RemoveEntity(e);
            if(mutation=="dead")e.GetStat("Hitpoints").BaseValue=0;if(mutation=="foreign-zone")b.CurrentZone=new Zone("other");
            if(mutation=="different-goal")g=new NoFightGoal(50,false);
            Assert.False(DensityCombatNativeEvidence.CurrentCalm(e,z,g));
        }
        [Test] public void ActualSkillAndAbilityShareOriginalGuidAndOwner()
        {
            var z=new Zone("test");var e=Actor(z);e.AddPart(new ActivatedAbilitiesPart());var skills=new SkillsPart();e.AddPart(skills);
            var skill=new Spellcraft_Calm();Assert.True(skills.AddSkill(skill,"native-witness-fixture"));
            Assert.True(DensityCombatNativeEvidence.OwnsAbility(e,skill.ActivatedAbilityID,"CommandCalm",out var found));
            Assert.AreSame(e.GetPart<ActivatedAbilitiesPart>().GetAbility(skill.ActivatedAbilityID),found);
            skill.ParentEntity=new Entity();Assert.False(DensityCombatNativeEvidence.OwnsAbility(e,skill.ActivatedAbilityID,"CommandCalm",out _));
        }

        [TestCase(false)][TestCase(true)]
        public void ExistingDetachedSchedulerProducesTheActualDiagnosticContract(bool cooling)
        {
            // This is a synthetic integration control, not native play. Reuse the
            // existing ordinary-stat bench's isolated fixture and explicit energy/RNG.
            var type=typeof(DensityCombatSchedulerBench);
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic;
            var factory=new CavesOfOoo.Data.EntityFactory();
            factory.LoadBlueprints(System.IO.File.ReadAllText(System.IO.Path.Combine(UnityEngine.Application.dataPath,"Resources/Content/Blueprints/Objects.json")));
            var scopeType=type.GetNestedType("DetachedScope",System.Reflection.BindingFlags.NonPublic);
            var channels=new[]{"scenario","ai","skill","turn","turn-verbose"};
            var old=channels.ToDictionary(c=>c,Diag.IsChannelEnabled);
            EntityMovedVisualHandler observer=null;
            try
            {
                foreach(var c in channels)Diag.SetChannel(c,true);
                using(var scope=(IDisposable)Activator.CreateInstance(scopeType,flags,null,new object[]{factory},null))
                {
                    var bench=new DensityCombatSchedulerBench();
                    var arena=type.GetMethod("Arena",flags).Invoke(bench,new object[]{factory,"IceWight",13});
                    Entity enemy=(Entity)arena.GetType().GetField("Actor").GetValue(arena);
                    Entity player=(Entity)arena.GetType().GetField("Player").GetValue(arena);
                    var ability=enemy.GetPart<ActivatedAbilitiesPart>().AbilityList.Single();
                    if(cooling)ability.CooldownRemaining=5;
                    observer=(owner,zone,ox,oy,nx,ny,forced)=>
                    {
                        if(owner!=enemy)return;
                        Diag.Record("scenario",DensityCombatNativeEvidence.MoveKind,owner,payload:new
                            {scheduledActor=TurnManager.Active?.CurrentActor?.ID,forced,oldX=ox,oldY=oy,newX=nx,newY=ny});
                    };
                    EntityVisualHooks.MovedCallback+=observer;
                    Diag.Record("scenario",DensityCombatNativeEvidence.MarkerKind,player,enemy);
                    string marker=Diag.Snapshot(1).Single().TraceId;
                    Assert.True((bool)type.GetMethod("RunOne",flags).Invoke(bench,new[]{arena}));
                    var rows=Diag.Snapshot(Diag.BufferCapacity);
                    if(cooling)Assert.True(DensityCombatNativeEvidence.TryCooldownFallback(rows,marker,player.ID,enemy.ID,ability.Command,5,ability.CooldownRemaining,out _));
                    else Assert.True(DensityCombatNativeEvidence.TryTacticTurn(rows,marker,player.ID,enemy.ID,ability.Command,out _));
                    int turns=DensityCombatNativeEvidence.CountEnds(rows,marker,player.ID,enemy.ID,enemy.ID);
                    Assert.AreEqual(1,turns);
                    Assert.True(DensityCombatNativeEvidence.EnergyMatches(1100,TurnManager.Active.GetEnergy(enemy),TurnManager.Active.TickCount,enemy.GetStatValue("Speed"),turns));
                }
            }
            finally
            {if(observer!=null)EntityVisualHooks.MovedCallback-=observer;foreach(var pair in old)Diag.SetChannel(pair.Key,pair.Value);}
        }
        // Exact retained native window a2b5a838: one input, two player Ends,
        // IceLance followed by its cooldown fallback. Only timestamps omitted.
        static List<Diag.Entry> FrozenNativeWindow() => new List<Diag.Entry> {
            new Diag.Entry{TraceId="a66da958",Category="scenario",Kind="DensityCombatNativeKeyStart",ActorId="2702",TargetId="52674",Turn=20,PayloadJson="{\"runId\":\"a2b5a8386f65495a99ffe042f8084e55\",\"policy\":\"ranged\",\"kind\":\"ordinary-observe-enemy\"}"},
            new Diag.Entry{TraceId="401ecfb6",Category="scenario",Kind="ReferenceGladeCombatKeyStart",ActorId="2702",TargetId="52674",Turn=20,PayloadJson="{\"runId\":\"a2b5a8386f65495a99ffe042f8084e55\",\"kind\":\"ordinary-observe-enemy\"}"},
            new Diag.Entry{TraceId="6cb50684",Category="turn",Kind="End",ActorId="2702",TargetId=null,Turn=20,PayloadJson="{\"blueprintName\":\"Player\",\"hp\":40}"},
            new Diag.Entry{TraceId="bc07a108",Category="turn-verbose",Kind="Begin",ActorId="52674",TargetId=null,Turn=25,PayloadJson="{\"blueprintName\":\"IceWight\",\"hp\":24}"},
            new Diag.Entry{TraceId="53254925",Category="ai",Kind="AssistRejected",ActorId="52674",TargetId="2702",Turn=25,PayloadJson="{\"command\":\"\",\"reason\":\"caller-ineligible\",\"cooldown\":0,\"skillClass\":\"\"}"},
            new Diag.Entry{TraceId="f04655a8",Category="damage",Kind="DamageDealt",ActorId="52674",TargetId="2702",Turn=25,PayloadJson="{\"amount\":2,\"hpAfter\":38,\"lethal\":false,\"attributes\":[\"Spell\",\"Cold\"]}"},
            new Diag.Entry{TraceId="59e2214c",Category="damage",Kind="SpellDamage",ActorId="52674",TargetId="2702",Turn=25,PayloadJson="{\"powerClass\":\"Cryomancy_IceLance\",\"diceRolled\":\"1d6\",\"rawRoll\":2,\"elementAttribute\":\"Cold\",\"actualDamage\":2}"},
            new Diag.Entry{TraceId="2be6d554",Category="effect",Kind="OnApply",ActorId="52674",TargetId="2702",Turn=25,PayloadJson="{\"effect\":\"FrozenEffect\",\"duration\":-1,\"justApplied\":false,\"forced\":false}"},
            new Diag.Entry{TraceId="0c56f193",Category="tile",Kind="TileWritten",ActorId="52674",TargetId="52674",Turn=25,PayloadJson="{\"x\":59,\"y\":16,\"layer\":\"energy\",\"id\":\"cold\",\"magnitude\":1,\"ability\":\"Cryomancy_IceLance\",\"writtenTiles\":3}"},
            new Diag.Entry{TraceId="38bcdc04",Category="skill",Kind="CommandRouted",ActorId="52674",TargetId="52674",Turn=25,PayloadJson="{\"command\":\"CommandIceLance\",\"skillClass\":\"Cryomancy_IceLance\",\"displayName\":\"Ice Lance\"}"},
            new Diag.Entry{TraceId="ad4efb09",Category="ai",Kind="TacticUsed",ActorId="52674",TargetId="2702",Turn=25,PayloadJson="{\"command\":\"CommandIceLance\",\"reason\":\"\",\"cooldown\":8,\"skillClass\":\"\"}"},
            new Diag.Entry{TraceId="8e4ab9ce",Category="turn-verbose",Kind="End",ActorId="52674",TargetId=null,Turn=25,PayloadJson="{\"blueprintName\":\"IceWight\",\"hp\":24}"},
            new Diag.Entry{TraceId="3dcdeca7",Category="turn",Kind="Begin",ActorId="2702",TargetId=null,Turn=30,PayloadJson="{\"blueprintName\":\"Player\",\"hp\":38}"},
            new Diag.Entry{TraceId="0f59c106",Category="turn",Kind="End",ActorId="2702",TargetId=null,Turn=30,PayloadJson="{\"blueprintName\":\"Player\",\"hp\":38}"},
            new Diag.Entry{TraceId="621aeecc",Category="turn-verbose",Kind="Begin",ActorId="52674",TargetId=null,Turn=39,PayloadJson="{\"blueprintName\":\"IceWight\",\"hp\":24}"},
            new Diag.Entry{TraceId="528e5c8d",Category="ai",Kind="TacticRejected",ActorId="52674",TargetId="2702",Turn=39,PayloadJson="{\"command\":\"CommandIceLance\",\"reason\":\"cooldown\",\"cooldown\":7,\"skillClass\":\"\"}"},
            new Diag.Entry{TraceId="f47fcf3b",Category="ai",Kind="TacticRejected",ActorId="52674",TargetId="2702",Turn=39,PayloadJson="{\"command\":\"\",\"reason\":\"no-eligible-ability\",\"cooldown\":0,\"skillClass\":\"\"}"},
            new Diag.Entry{TraceId="4309dc04",Category="scenario",Kind="DensityCombatNativeMoveObserved",ActorId="52674",TargetId=null,Turn=39,PayloadJson="{\"scheduledActor\":\"52674\",\"forced\":false,\"oldX\":65,\"oldY\":22,\"newX\":64,\"newY\":21}"},
            new Diag.Entry{TraceId="35fe8d79",Category="turn-verbose",Kind="End",ActorId="52674",TargetId=null,Turn=39,PayloadJson="{\"blueprintName\":\"IceWight\",\"hp\":24}"},
            new Diag.Entry{TraceId="02a19257",Category="effect",Kind="NativeSpellRejected",ActorId="52674",TargetId=null,Turn=40,PayloadJson="{\"spellId\":\"Cryomancy_IceLance\",\"zoneId\":\"Overworld.11.0.3\",\"pieces\":0,\"reason\":\"native-surface-unavailable\",\"duration\":0.0,\"backend\":\"native\"}"},
        };
        const string FrozenMarker="a66da958", FrozenPlayer="2702", FrozenEnemy="52674";
        static bool PaidFrozen(List<Diag.Entry> r,int ambient=1,int after=1000)
            => DensityCombatNativeEvidence.TryPaidPlayerInput(r,FrozenMarker,FrozenPlayer,FrozenEnemy,
                1000,after,20,100,ambient,false,out _);
        static bool FrozenFallback(List<Diag.Entry> r,int after=6)
            => DensityCombatNativeEvidence.TryTacticThenCooldownFallback(r,FrozenMarker,FrozenPlayer,FrozenEnemy,
                "CommandIceLance",after,out _);
        [Test] public void ActualFrozenNativeWindowAccountsOneInputAndTwoPaidPlayerTurns()
        {var r=FrozenNativeWindow();Assert.True(DensityCombatNativeEvidence.TryPaidPlayerInput(r,FrozenMarker,FrozenPlayer,FrozenEnemy,
            1000,1000,20,100,1,false,out int turns));Assert.AreEqual(2,turns);}
        [TestCase("ambient")][TestCase("energy")][TestCase("missing-blocked-begin")][TestCase("duplicate-end")][TestCase("foreign-begin")]
        public void FrozenPaymentRequiresActualEnergyAmbientAndPairedAutomaticTurn(string mutation)
        {var r=FrozenNativeWindow();int ambient=1,energy=1000;
            if(mutation=="ambient")ambient=2;if(mutation=="energy")energy=900;
            int begin=r.FindIndex(x=>x.TraceId=="3dcdeca7");
            if(mutation=="missing-blocked-begin")r.RemoveAt(begin);
            if(mutation=="foreign-begin")Edit(r,begin,x=>{x.ActorId="foreign";return x;});
            if(mutation=="duplicate-end"){var x=r.Single(x=>x.TraceId=="0f59c106");x.TraceId="extra-end";r.Add(x);}
            Assert.False(PaidFrozen(r,ambient,energy));}
        [Test] public void OneOrdinaryInputAndScheduledZoneAmbientControlRemainExact()
        {var r=FrozenNativeWindow();r.RemoveAll(x=>x.TraceId=="3dcdeca7"||x.TraceId=="0f59c106");
            Assert.True(DensityCombatNativeEvidence.TryPaidPlayerInput(r,FrozenMarker,FrozenPlayer,FrozenEnemy,1000,1000,10,100,1,false,out int n));Assert.AreEqual(1,n);
            Assert.True(DensityCombatNativeEvidence.TryPaidPlayerInput(FrozenNativeWindow(),FrozenMarker,FrozenPlayer,FrozenEnemy,1000,1000,20,100,2,true,out n));Assert.AreEqual(2,n);}
        [Test] public void ActualFrozenWindowTacticThenFallbackRemainTwoDistinctCompletedEnemyTurns()
        {Assert.True(DensityCombatNativeEvidence.TryTacticThenCooldownFallback(FrozenNativeWindow(),FrozenMarker,FrozenPlayer,FrozenEnemy,"CommandIceLance",6,out var found));
            Assert.AreEqual("621aeecc",found.BeginTrace);Assert.AreEqual("35fe8d79",found.EndTrace);Assert.AreEqual("4309dc04",found.ActionTrace);Assert.AreEqual(6,found.Cooldown);}
        [TestCase("missing-used")][TestCase("missing-routed")][TestCase("missing-action")][TestCase("foreign-action")][TestCase("wrong-final-cooldown")][TestCase("duplicate-used")]
        public void TwoTurnFallbackCannotBeInferredFromUnrelatedOrIncompleteRecords(string mutation)
        {var r=FrozenNativeWindow();int after=6;
            if(mutation=="missing-used")r.RemoveAll(x=>x.TraceId=="ad4efb09");
            if(mutation=="missing-routed")r.RemoveAll(x=>x.TraceId=="38bcdc04");
            if(mutation=="missing-action")r.RemoveAll(x=>x.TraceId=="4309dc04");
            if(mutation=="foreign-action"){int i=r.FindIndex(x=>x.TraceId=="4309dc04");Edit(r,i,x=>{x.ActorId="foreign";return x;});}
            if(mutation=="wrong-final-cooldown")after=7;
            if(mutation=="duplicate-used"){var x=r.Single(x=>x.TraceId=="ad4efb09");x.TraceId="duplicate-used";r.Insert(r.FindIndex(x=>x.TraceId=="8e4ab9ce"),x);}
            Assert.False(FrozenFallback(r,after));}

    }
}
