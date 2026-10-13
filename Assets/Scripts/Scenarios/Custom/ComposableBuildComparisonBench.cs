using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Skills;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Finite, explicitly granted laboratory. Scripted policies measure
    /// tradeoffs, not optimal play, discovery, or a difficulty guarantee.</summary>
    [Scenario(name:"Composable Build Comparison",category:"Combat",description:"Equal six-SP loadouts, shared movement, Vault and counter-cap experiments.")]
    public sealed partial class ComposableBuildComparisonBench : IScenario
    {
        public enum Profile { Counterfighter, Hybrid, Ranked, Control }
        public enum Encounter { OpenCommitment, HeldEntrance, MixedLane }
        public const int Seeds=32, ExpectedCases=42, ActionLimit=60;
        public string RunId { get; private set; }
        public int Cases { get; private set; }
        public int Failures { get; private set; }
        public readonly List<string> Audit=new List<string>();
        public readonly List<object> Observations=new List<object>();
        public readonly List<TrialResult> Trials=new List<TrialResult>();
        public sealed class TrialResult
        {
            public Profile Build; public Encounter Scene; public int Seed,Speed; public bool Vault,Uncapped;
            public string Outcome="timeout";
            public int InitialAV,InitialDV,Actions,Ticks,Damage,MinHp=40,EndHp,Tonics,DefenseRolls,Parries,Guarded,CounterRolls,Counters,CounterDamage,CounterMisses,SpentRefusals,Commitments,Interruptions;
            public readonly Dictionary<string,int> Decisions=new Dictionary<string,int>();
        }
        public void Apply(ScenarioContext context)
        {
            RunId=Guid.NewGuid().ToString("N");Cases=Failures=0;Audit.Clear();Observations.Clear();Trials.Clear();
            foreach(Profile p in Enum.GetValues(typeof(Profile)))foreach(Encounter e in Enum.GetValues(typeof(Encounter)))
                foreach(bool vault in new[]{false,true})RunCase(context.Factory,p,e,vault,false,100,"build");
            foreach(Profile p in new[]{Profile.Counterfighter,Profile.Hybrid,Profile.Ranked})
                foreach(int speed in new[]{50,100,200})foreach(bool uncapped in new[]{false,true})
                    RunCase(context.Factory,p,Encounter.HeldEntrance,false,uncapped,speed,"cap");
            string path=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../Docs/Verification/ComposableBuilds/Comparison",RunId));
            Directory.CreateDirectory(path);
            File.WriteAllText(Path.Combine(path,"report.json"),JsonConvert.SerializeObject(new{runId=RunId,complete=Cases==ExpectedCases&&Failures==0,cases=Cases,failures=Failures,
                fixture="Six purchased SP. All attributes18, HP40, ordinary LongSword/Buckler/LeatherArmor, two real tonics. Physical Vault manual is supplied in its arm. Controlled maps and personally hostile authored creatures. World entity omitted; material/coating decay retained. No forced combat rolls. Seeds1..32; action limit60.",
                policy="Heal at14, retreat at8 without supplies. Walk off visible commitment unless holding the doorway. Jet Blast interrupts commitments or an adjacent threat; guard adjacent threat; Lunge at2; Tumble out of threatened lane; Vault at2 or toward retreat; ordinary melee/approach. Shared policies are fixed, not optimal.",
                cannotVerify="Natural discovery, novice comprehension, optimal strategy, general balance, real expedition performance or enjoyment. Cap arms change only the spent gate. Initial seeds paired; draw streams diverge after different decisions.",Audit,Observations,Trials},Formatting.Indented));
        }
        void RunCase(EntityFactory factory,Profile p,Encounter e,bool vault,bool uncapped,int speed,string experiment)
        {
            string name=experiment+"/"+p+"/"+e+"/vault="+vault+"/uncapped="+uncapped+"/speed="+speed;Cases++;
            try
            {
                var results=new List<TrialResult>();
                for(int seed=1;seed<=Seeds;seed++){var r=RunTrial(factory,p,e,seed,vault,uncapped,speed);results.Add(r);Trials.Add(r);}
                Observations.Add(new{name,summary=Summarize(results)});Audit.Add("PASS "+name);
            }
            catch(Exception ex){Failures++;Audit.Add("FAIL "+name+": "+ex);Observations.Add(new{name,error=ex.ToString()});}
        }
        static object Summarize(List<TrialResult> rows)
        {
            var damage=rows.Select(r=>(double)r.Damage).OrderBy(x=>x).ToArray();double mean=damage.Average();
            double error=1.96*Math.Sqrt(damage.Sum(x=>(x-mean)*(x-mean))/(damage.Length-1)/damage.Length);
            return new{n=rows.Count,deaths=rows.Count(r=>r.Outcome=="dead"),victories=rows.Count(r=>r.Outcome=="victory"),withdrawals=rows.Count(r=>r.Outcome=="withdrawn"),timeouts=rows.Count(r=>r.Outcome=="timeout"),
                damageMean=mean,damageMedian=(damage[15]+damage[16])/2,damageP90=damage[28],damageMeanApprox95=new[]{mean-error,mean+error},
                deathWilson95=Wilson(rows.Count(r=>r.Outcome=="dead"),rows.Count),actionsMean=rows.Average(r=>r.Actions),tonicsMean=rows.Average(r=>r.Tonics),endHpMean=rows.Average(r=>r.EndHp),minHpMean=rows.Average(r=>r.MinHp),
                defenseRolls=rows.Sum(r=>r.DefenseRolls),parries=rows.Sum(r=>r.Parries),guarded=rows.Sum(r=>r.Guarded),counterRolls=rows.Sum(r=>r.CounterRolls),counters=rows.Sum(r=>r.Counters),counterDamage=rows.Sum(r=>r.CounterDamage),spentRefusals=rows.Sum(r=>r.SpentRefusals),
                counterPerEligibleProcWilson95=Wilson(rows.Sum(r=>r.Counters),rows.Sum(r=>r.CounterRolls)),
                counterPerFullParryWilson95=Wilson(rows.Sum(r=>r.Counters),rows.Sum(r=>r.Parries)),
                parryPerDefenseRollWilson95=Wilson(rows.Sum(r=>r.Parries),rows.Sum(r=>r.DefenseRolls)),
                decisions=rows.SelectMany(r=>r.Decisions).GroupBy(k=>k.Key).ToDictionary(g=>g.Key,g=>g.Sum(k=>k.Value))};
        }
        static double[] Wilson(int yes,int total)
        {
            if(total==0)return null;double p=(double)yes/total,z=1.96,d=1+z*z/total,c=(p+z*z/(2*total))/d,h=z*Math.Sqrt(p*(1-p)/total+z*z/(4*total*total))/d;return new[]{c-h,c+h};
        }
        public static Entity CreatePlayer(EntityFactory factory,Zone zone,Profile profile,bool vault,int speed=100)
        {
            if(speed<25||speed>200)throw new ArgumentOutOfRangeException(nameof(speed));
            if(profile<Profile.Counterfighter||profile>Profile.Control)throw new ArgumentOutOfRangeException(nameof(profile));
            var a=factory.CreateEntity("Player");Require(a!=null&&a.GetPart<SkillsPart>()?.SkillList.Count==0,"plain authored player with no preset skills");
            a.GetPart<BrainPart>().CurrentZone=zone;a.GetPart<BrainPart>().Wanders=false;
            foreach(string name in new[]{"Strength","Agility","Toughness","Intelligence","Willpower","Ego"})SetStat(a,name,18,50);
            SetStat(a,"Hitpoints",40,40);SetStat(a,"Speed",speed,200);SetStat(a,"SP",6,999);SetStat(a,"DV",0,200);
            Require(zone.AddEntity(a,10,10),"player placement");
            foreach(string bp in new[]{"LongSword","Buckler","LeatherArmor"}){var item=Carry(factory,a,bp);Require(InventorySystem.Equip(a,item),"equip "+bp);}
            Carry(factory,a,"HealingTonic");Carry(factory,a,"HealingTonic");
            string[] purchases=profile==Profile.Counterfighter?new[]{"LongBladesSkill","LongBlades_Deflect","LongBlades_EnGarde","LongBlades_Riposte","LongBlades_Expertise","LongBlades_Lunge"}
                :profile==Profile.Hybrid?new[]{"LongBladesSkill","LongBlades_Deflect","LongBlades_EnGarde","LongBlades_Riposte","HydromancySkill","Hydromancy_JetBlast"}
                :profile==Profile.Ranked?new[]{"LongBladesSkill","LongBlades_Deflect","LongBlades_Deflect","LongBlades_EnGarde","LongBlades_Riposte","LongBlades_Riposte"}
                :new[]{"HydromancySkill","Hydromancy_JetBlast","Hydromancy_Undertow","AcrobaticsSkill","AcrobaticsDodgePower","Acrobatics_Tumble"};
            foreach(string name in purchases){int before=a.GetStatValue("SP");var result=BuySkillAction.Execute(a,name);Require(result.Succeeded&&a.GetStatValue("SP")==before-1,"purchase "+name+": "+result.Reason);}
            Require(a.GetStatValue("SP")==0,"six spent points");
            if(vault)
            {
                var book=Carry(factory,a,"DitchkeepersFootwork");var read=GameEvent.New("InventoryAction");read.SetParameter("Command","ReadGrimoire");read.SetParameter("Actor",(object)a);book.FireEventAndRelease(read);
                Require(a.GetPart<SkillsPart>().HasSkill("Acrobatics_Vault")&&a.GetStatValue("SP")==0,"physical manual teaches without SP");
            }
            return a;
        }
        static void SetStat(Entity a,string name,int value,int max)=>a.Statistics[name]=new Stat{Owner=a,Name=name,BaseValue=value,Min=name=="DV"?-50:0,Max=max};
        static Entity Carry(EntityFactory f,Entity a,string bp){var item=f.CreateEntity(bp);Require(item!=null&&a.GetPart<InventoryPart>().AddObject(item),"supply "+bp);return item;}
        public static TrialResult RunTrial(EntityFactory factory,Profile profile,Encounter encounter,int seed,bool vault,bool uncapped=false,int speed=100)
        {
            using(var scope=new FiftySecondCombatBench.DetachedScope())
            {
                bool capture=Diag.DetailedCaptureEnabled,damageChannel=Diag.IsChannelEnabled("damage"),aiChannel=Diag.IsChannelEnabled("ai");var oldFactory=MaterialReactionResolver.Factory;var oldLoadout=LoadoutPart.Factory;var oldRng=LoadoutPart.Rng;
                var oldLootFactory=LootDropSystem.Factory;var oldLootRng=LootDropSystem.Rng;var oldCorpseFactory=CorpsePart.Factory;
                try
                {
                    if(!damageChannel)Diag.SetChannel("damage",true);if(!aiChannel)Diag.SetChannel("ai",true);Diag.DetailedCaptureEnabled=true;
                    var rng=new Random(seed);LoadoutPart.Factory=factory;LoadoutPart.Rng=rng;MaterialReactionResolver.Factory=factory;
                    LootDropSystem.Factory=factory;LootDropSystem.Rng=new Random(seed^171);CorpsePart.Factory=factory;
                    var zone=new Zone("comparison-"+Guid.NewGuid().ToString("N"));var player=CreatePlayer(factory,zone,profile,vault,speed);
                    player.GetPart<BrainPart>().Rng=rng;if(player.GetPart<RiposteAllowancePart>() is RiposteAllowancePart allowance)allowance.ComparisonUncapped=uncapped;
                    var trial=new Trial{Player=player,Zone=zone,Rng=rng,Turns=new TurnManager(),Result=new TrialResult{Build=profile,Scene=encounter,Seed=seed,Speed=speed,Vault=vault,Uncapped=uncapped,InitialAV=CombatSystem.GetAV(player),InitialDV=CombatSystem.GetDV(player)}};
                    trial.Turns.AddEntity(player);
                    if(encounter==Encounter.OpenCommitment)Spawn(factory,trial,"Shambler",12,10);
                    else if(encounter==Encounter.HeldEntrance)
                    {Wall(factory,zone,10,9);Wall(factory,zone,10,11);Spawn(factory,trial,"MarlbackScrabbler",11,9);Spawn(factory,trial,"MarlbackScrabbler",11,11);}
                    else
                    {Wall(factory,zone,12,11);Wall(factory,zone,12,12);Spawn(factory,trial,"MarlbackScrabbler",11,10);Spawn(factory,trial,"MarlbackCindercaller",15,10);}
                    var prior=Diag.Snapshot(1);trial.LastTrace=prior.Count==0?null:prior[0].TraceId;
                    var r=trial.Result;int cycles=0;
                    trial.Turns.ProcessUntilPlayerTurn();Collect(trial);
                    while(Alive(player,zone)&&r.Actions<ActionLimit&&cycles++<120)
                    {
                        if(trial.Enemies.All(e=>!Alive(e,zone))){r.Outcome="victory";break;}
                        Require(trial.Turns.CurrentActor==player&&trial.Turns.WaitingForInput,"real player turn");
                        if(player.GetPart<StatusEffectsPart>()?.IsActionBlocked()==true){trial.Turns.ProcessUntilPlayerTurn();Collect(trial);continue;}
                        var cell=zone.GetEntityCell(player);FieldOfView.Compute(zone,cell.X,cell.Y,10);
                        string decision=Act(trial);r.Decisions.TryGetValue(decision,out int n);r.Decisions[decision]=n+1;r.Actions++;
                        trial.Turns.EndTurn(player,zone);trial.Turns.ProcessUntilPlayerTurn();
                        MaterialSimSystem.TickMaterialEntities(zone);ZoneTileStateSystem.OnPlayerTurnEnd(zone);Collect(trial);
                        cell=zone.GetEntityCell(player);if(trial.Retreating&&cell!=null&&cell.X<=6&&Alive(player,zone)){r.Outcome="withdrawn";break;}
                    }
                    if(!Alive(player,zone))r.Outcome="dead";else if(trial.Enemies.All(e=>!Alive(e,zone)))r.Outcome="victory";
                    r.EndHp=Math.Max(0,player.GetStatValue("Hitpoints"));r.MinHp=Math.Min(r.MinHp,r.EndHp);r.Ticks=trial.Turns.TickCount;return r;
                }
                finally {Diag.DetailedCaptureEnabled=capture;if(!damageChannel)Diag.SetChannel("damage",false);if(!aiChannel)Diag.SetChannel("ai",false);MaterialReactionResolver.Factory=oldFactory;LoadoutPart.Factory=oldLoadout;LoadoutPart.Rng=oldRng;LootDropSystem.Factory=oldLootFactory;LootDropSystem.Rng=oldLootRng;CorpsePart.Factory=oldCorpseFactory;}
            }
        }
        sealed class Trial
        {
            public Entity Player;public Zone Zone;public Random Rng;public TurnManager Turns;public TrialResult Result;public string LastTrace;public bool Retreating;
            public readonly List<Entity> Enemies=new List<Entity>();
        }
        static void Spawn(EntityFactory factory,Trial t,string blueprint,int x,int y)
        {var e=factory.CreateEntity(blueprint);Require(e!=null&&t.Zone.AddEntity(e,x,y),"enemy "+blueprint);var brain=e.GetPart<BrainPart>();Require(brain!=null,"enemy brain");brain.CurrentZone=t.Zone;brain.Rng=t.Rng;brain.SetPersonallyHostile(t.Player);if(e.GetPart<CorpsePart>() is CorpsePart corpse)corpse.TestRng=new Random(t.Result.Seed^x^y);t.Enemies.Add(e);t.Turns.AddEntity(e);}
        static void Wall(EntityFactory factory,Zone zone,int x,int y){var wall=factory.CreateEntity("StoneWall");Require(wall!=null&&zone.AddEntity(wall,x,y),"opaque wall");Require(zone.GetCell(x,y).IsWall(),"wall blocks sight");}
        static bool Alive(Entity e,Zone z)=>e?.SpatialZone==z&&e.GetStatValue("Hitpoints")>0&&!CombatSystem.IsDeathHandled(e);
        static void Require(bool value,string reason){if(!value)throw new InvalidOperationException(reason);}
        static void Collect(Trial t)
        {
            var records=Diag.Snapshot(Diag.BufferCapacity);int start=0;
            if(t.LastTrace!=null){while(start<records.Count&&records[start].TraceId!=t.LastTrace)start++;Require(start<records.Count,"diagnostic ring overrun");start++;}
            for(int i=start;i<records.Count;i++)
            {
                var e=records[i];var r=t.Result;
                if(e.Kind=="DamageDealt"&&e.TargetId==t.Player.ID){var p=JObject.Parse(e.PayloadJson);r.Damage+=(int)p["ActualDamage"];r.MinHp=Math.Min(r.MinHp,(int)p["hpAfter"]);}
                else if(e.Kind=="BladeDefense"&&e.ActorId==t.Player.ID){var p=JObject.Parse(e.PayloadJson);r.DefenseRolls++;if((bool)p["parried"])r.Parries++;if((bool)p["guarded"])r.Guarded++;}
                else if(e.Kind=="RiposteRoll"&&e.ActorId==t.Player.ID)r.CounterRolls++;
                else if(e.Kind=="RiposteIssued"&&e.ActorId==t.Player.ID){var p=JObject.Parse(e.PayloadJson);r.Counters++;r.CounterDamage+=(int)p["ActualDamage"];if((string)p["outcome"]=="NaturalMiss")r.CounterMisses++;}
                else if(e.Kind=="RiposteRejected"&&e.ActorId==t.Player.ID&&e.PayloadJson.Contains("spent"))r.SpentRefusals++;
                else if(e.Kind=="CommittedMeleeWindup"&&t.Enemies.Any(x=>x.ID==e.ActorId))r.Commitments++;
                else if(e.Kind=="CommittedMeleeCancelled"&&t.Enemies.Any(x=>x.ID==e.ActorId))r.Interruptions++;
            }
            if(records.Count>0)t.LastTrace=records[records.Count-1].TraceId;
        }
    }
}
