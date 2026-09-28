using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public class SpreadExplorationActorTests
    {
        internal sealed class Scope : IDisposable
        {
            readonly List<Action> restore = new List<Action>();
            public Scope()
            {
                foreach (var type in new[] { typeof(FactionManager), typeof(PlayerReputation), typeof(TurnManager),
                    typeof(WorldClock), typeof(MessageLog), typeof(SettlementRuntime), typeof(SettlementManager),
                    typeof(AsciiFxBus), typeof(SpellFxBus), typeof(ZoneRenderHooks), typeof(EntityVisualHooks) })
                    foreach (var field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        if (field.IsLiteral) continue;
                        object value = field.GetValue(null);
                        if (!field.IsInitOnly) restore.Add(() => field.SetValue(null, value));
                        if (value is IDictionary dict)
                        {
                            var entries = new List<DictionaryEntry>(); foreach (DictionaryEntry item in dict) entries.Add(item);
                            restore.Add(() => { dict.Clear(); foreach (var item in entries) dict.Add(item.Key, item.Value); });
                        }
                        else if (value is IList list && !list.IsReadOnly && !list.IsFixedSize)
                        {
                            var items = list.Cast<object>().ToArray(); restore.Add(() => { list.Clear(); foreach (var item in items) list.Add(item); });
                        }
                        else if (value != null && value.GetType().IsGenericType)
                        {
                            var definition = value.GetType().GetGenericTypeDefinition();
                            if (definition == typeof(HashSet<>) || definition == typeof(Queue<>))
                            {
                                var items = ((IEnumerable)value).Cast<object>().ToArray();
                                var clear = value.GetType().GetMethod("Clear"); var add = value.GetType().GetMethod(definition == typeof(HashSet<>) ? "Add" : "Enqueue");
                                restore.Add(() => { clear.Invoke(value, null); foreach (var item in items) add.Invoke(value, new[] { item }); });
                            }
                        }
                        if (!field.IsInitOnly && typeof(Delegate).IsAssignableFrom(field.FieldType)) field.SetValue(null, null);
                    }
                FactionManager.Initialize(); MessageLog.Clear();
            }
            public void Dispose() { for (int i = restore.Count - 1; i >= 0; i--) restore[i](); }

        }
        public sealed class AttackProbe:Part {public override string Name=>"ExplorationAttackProbe";public int Attacks;public int Ends;public override bool HandleEvent(GameEvent e){if(e.ID=="BeforeMeleeAttack"){Attacks++;return false;}if(e.ID=="EndTurn")Ends++;return true;}}
        public sealed class NoActionProbe:Part {public override string Name=>"ExplorationNoActionProbe";public override bool HandleEvent(GameEvent e)=>e.ID!="BeginTakeAction";}
        internal sealed class Fixture:IDisposable
        {
            readonly Scope scope=new Scope();public Zone Zone=new Zone("Overworld.7.9.0");public Entity Actor,Player,Post,Food,Reserve;public Part Role;
            public Fixture()
            {
                Actor=Creature("actor","OutlandRaiders",10,10);Player=Creature("player","Player",20,10);Player.Tags["Player"]="true";Player.GetPart<BrainPart>().Passive=true;
                Post=Prop("post",9,9);Food=Row("food",11,10);Reserve=Row("reserve",12,11);Actor.AddPart(new AttackProbe());
            }
            public BrainPart Brain=>Actor.GetPart<BrainPart>();public AttackProbe Probe=>Actor.GetPart<AttackProbe>();
            Entity Creature(string id,string faction,int x,int y){var e=new Entity{ID=id,BlueprintName=id};e.Tags["Creature"]="true";e.Tags["Faction"]=faction;e.AddPart(new PhysicsPart{Solid=true});e.AddPart(new InventoryPart());e.AddPart(new BrainPart{Wanders=false,WandersRandomly=false,CurrentZone=Zone,Rng=new Random(1)});foreach(var n in new[]{"Hitpoints","Speed","Strength"})e.Statistics[n]=new Stat{Name=n,BaseValue=n=="Speed"?100:n=="Hitpoints"?40:18,Min=0,Max=n=="Speed"?200:40,Owner=e};Assert.True(Zone.AddEntity(e,x,y));return e;}
            public Entity Prop(string id,int x,int y){var e=new Entity{ID=id,BlueprintName="Hedge"};e.AddPart(new PhysicsPart());Assert.True(Zone.AddEntity(e,x,y));return e;}
            public Entity Row(string id,int x,int y){var e=Prop(id,x,y);e.BlueprintName="RipeCropRow";e.AddPart(new RenderPart{DisplayName="ripe row",RenderString="!",ColorString="&Y"});e.AddPart(new ExaminablePart());e.AddPart(new FieldHarvestPart());return e;}
            public void Move(Entity e,int x,int y)=>Assert.True(Zone.MoveEntity(e,x,y));
            public void Turn()=>Actor.FireEventAndRelease(GameEvent.New("TakeTurn"));
            public void Add(string name){var t=typeof(BrainPart).Assembly.GetType("CavesOfOoo.Core."+name);Assert.NotNull(t,"Missing actor role API "+name);Role=(Part)Activator.CreateInstance(t);Actor.AddPart(Role);}
            public bool Call(string method,params object[] args){var m=Role.GetType().GetMethod(method);Assert.NotNull(m,"Missing "+method);return (bool)m.Invoke(Role,args);}
            public object Get(string field){var f=Role.GetType().GetField(field);Assert.NotNull(f,field);return f.GetValue(Role);}
            public void Territory(){Add("SpreadTerritoryPart");Assert.True(Call("Configure",Zone,Post,8,8,14,13,2));}
            public void Grazer(){Brain.Passive=true;Add("SpreadGrazerPart");Assert.True(Call("ConfigureForage",Zone,Food,Reserve));}
            public void RoundTrip()
            {
                var factory=new EntityFactory();var manager=OverworldZoneManager.CreateDetached(factory,64);manager.SetActiveZone(Zone);
                var state=GameSessionState.Capture("role-test","role-test",manager,new TurnManager(),Player,0);
                using(var stream=new System.IO.MemoryStream()){state.Save(new SaveWriter(stream));stream.Position=0;var loaded=GameSessionState.Load(new SaveReader(stream,factory));Zone=loaded.ZoneManager.ActiveZone;Player=loaded.Player;}
                Actor=Zone.GetReadOnlyEntities().Single(e=>e.ID=="actor");Post=Zone.GetReadOnlyEntities().Single(e=>e.ID=="post");Food=Zone.GetReadOnlyEntities().Single(e=>e.ID=="food");Reserve=Zone.GetReadOnlyEntities().Single(e=>e.ID=="reserve");Role=Actor.Parts.Single(p=>p.GetType().Name.StartsWith("Spread")&&p.GetType().Name.EndsWith("Part"));Brain.CurrentZone=Zone;
            }
            public void Dispose()=>scope.Dispose();
        }
        [Test] public void OrdinaryHostileWithoutRoleStillAttacks(){using(var f=new Fixture()){f.Move(f.Player,11,10);f.Turn();Assert.AreEqual(1,f.Probe.Attacks);}}
        [Test] public void OrdinaryCalmStillBlocksAttacks(){using(var f=new Fixture()){f.Move(f.Player,11,10);f.Brain.PushGoal(new NoFightGoal(10));f.Turn();Assert.AreEqual(0,f.Probe.Attacks);}}
        [Test] public void OutsideTerritorySuppressesGenericRaiderChase(){using(var f=new Fixture()){f.Territory();f.Move(f.Player,15,10);var before=f.Zone.GetEntityPosition(f.Actor);f.Turn();Assert.AreEqual(before,f.Zone.GetEntityPosition(f.Actor));Assert.IsNull(f.Brain.Target);Assert.AreEqual(0,f.Probe.Attacks);}}
        [Test] public void IncursionWarnsAndLeavesTwoCompleteActionsBeforeAttack(){using(var f=new Fixture()){f.Territory();f.Move(f.Player,11,10);for(int i=0;i<3;i++){f.Turn();Assert.AreEqual(0,f.Probe.Attacks);}f.Turn();Assert.AreEqual(1,f.Probe.Attacks);Assert.False(f.Brain.IsPersonallyHostileTo(f.Player));}}
        [Test] public void LeavingTerritoryReleasesOnlyItsOwnPursuit(){using(var f=new Fixture()){f.Territory();f.Move(f.Player,11,10);for(int i=0;i<4;i++)f.Turn();Assert.AreEqual(1,f.Probe.Attacks);f.Move(f.Player,15,10);f.Turn();Assert.IsNull(f.Brain.Target);Assert.False(f.Brain.IsPersonallyHostileTo(f.Player));}}
        [Test] public void GenuinePersonalEnemyOutsideTerritoryRemainsHostile(){using(var f=new Fixture()){f.Territory();f.Move(f.Player,15,10);f.Brain.SetPersonallyHostile(f.Player,false);f.Turn();Assert.True(f.Brain.IsPersonallyHostileTo(f.Player));Assert.AreEqual((11,10),f.Zone.GetEntityPosition(f.Actor));Assert.AreSame(f.Player,f.Brain.Target);}}
        [Test] public void ReentryRequiresFreshWarning(){using(var f=new Fixture()){f.Territory();f.Move(f.Player,11,10);for(int i=0;i<4;i++)f.Turn();f.Move(f.Player,15,10);f.Turn();f.Move(f.Player,11,10);f.Turn();Assert.AreEqual(1,f.Probe.Attacks);}}
        [Test] public void TerritoryCalmHasPriorityAndDoesNotSpendGrace(){using(var f=new Fixture()){f.Territory();f.Move(f.Player,11,10);f.Turn();var grace=f.Get("GraceRemaining");f.Brain.PushGoal(new NoFightGoal(10));f.Turn();Assert.AreEqual(grace,f.Get("GraceRemaining"));Assert.AreEqual(0,f.Probe.Attacks);}}
        [Test] public void InvalidatedPostDoesNotTurnRoleIntoGlobalHostile(){using(var f=new Fixture()){f.Territory();f.Zone.RemoveEntity(f.Post);f.Move(f.Player,11,10);f.Turn();Assert.AreEqual(0,f.Probe.Attacks);Assert.IsNull(f.Brain.Target);}}
        [Test] public void MovedPostDoesNotMoveTerritory(){using(var f=new Fixture()){f.Territory();f.Move(f.Post,30,20);f.Move(f.Player,11,10);f.Turn();Assert.AreEqual(0,f.Probe.Attacks);Assert.IsNull(f.Get("WarningTarget"));}}
        [Test] public void TerritoryWarningAndExactPostSurviveGraphLoad(){using(var f=new Fixture()){f.Territory();f.Move(f.Player,11,10);f.Turn();f.RoundTrip();Assert.AreSame(f.Post,f.Get("Post"));Assert.AreSame(f.Player,f.Get("WarningTarget"));Assert.AreEqual(2,f.Get("GraceRemaining"));f.Turn();Assert.AreEqual(1,f.Get("GraceRemaining"));Assert.AreEqual(0,f.Probe.Attacks);}}
        [Test] public void HealthyGrazerFleesClosePlayerBeforeEating(){using(var f=new Fixture()){f.Grazer();f.Move(f.Player,12,10);var before=f.Zone.GetEntityPosition(f.Actor);f.Turn();Assert.AreNotEqual(before,f.Zone.GetEntityPosition(f.Actor));Assert.False(f.Food.GetPart<FieldHarvestPart>().Harvested);Assert.AreEqual(40,f.Actor.GetStatValue("Hitpoints"));Assert.AreEqual(0,f.Probe.Attacks);}}
        [Test] public void GrazerConsumesExactRowOnceAndCreatesNoFood(){using(var f=new Fixture()){f.Grazer();int count=f.Zone.GetReadOnlyEntities().Count;f.Turn();Assert.True(f.Food.GetPart<FieldHarvestPart>().Harvested);Assert.False(f.Reserve.GetPart<FieldHarvestPart>().Harvested);Assert.True((bool)f.Get("Fed"));for(int i=0;i<5;i++)f.Turn();Assert.AreEqual(count,f.Zone.GetReadOnlyEntities().Count);Assert.AreEqual(0,f.Actor.GetPart<InventoryPart>().Objects.Count);}}
        [Test] public void LastReservedRowConsumedPreventsFeeding(){using(var f=new Fixture()){f.Grazer();f.Reserve.GetPart<FieldHarvestPart>().Harvested=true;f.Turn();Assert.False(f.Food.GetPart<FieldHarvestPart>().Harvested);Assert.False((bool)f.Get("Fed"));}}
        [Test] public void RemovedReservedRowPreventsFeeding(){using(var f=new Fixture()){f.Grazer();f.Zone.RemoveEntity(f.Reserve);f.Turn();Assert.False(f.Food.GetPart<FieldHarvestPart>().Harvested);}}
        [Test] public void MovedFoodIsNotPursuedAsTheAllocatedSource(){using(var f=new Fixture()){f.Grazer();f.Move(f.Food,30,20);f.Turn();Assert.False(f.Food.GetPart<FieldHarvestPart>().Harvested);Assert.AreEqual((10,10),f.Zone.GetEntityPosition(f.Actor));}}
        [Test] public void SameRowCannotBeBothFoodAndReserve(){using(var f=new Fixture()){f.Add("SpreadGrazerPart");Assert.False(f.Call("ConfigureForage",f.Zone,f.Food,f.Food));}}
        [Test] public void GrazerCalmBlocksFlightAndFeeding(){using(var f=new Fixture()){f.Grazer();f.Move(f.Player,12,10);f.Brain.PushGoal(new NoFightGoal(10));f.Turn();Assert.AreEqual((10,10),f.Zone.GetEntityPosition(f.Actor));Assert.False(f.Food.GetPart<FieldHarvestPart>().Harvested);}}
        [Test] public void GrazerDoesNotInterruptCurrentWorkGoal(){using(var f=new Fixture()){f.Grazer();f.Brain.PushGoal(new WaitGoal(10));f.Turn();Assert.False(f.Food.GetPart<FieldHarvestPart>().Harvested);Assert.IsInstanceOf<WaitGoal>(f.Brain.PeekGoal());}}
        [Test] public void RecruitedGrazerDoesNotConsumeWhileFollowing(){using(var f=new Fixture()){f.Grazer();f.Brain.SetPartyLeader(f.Player);f.Brain.PushGoal(new FollowLeaderGoal(f.Player));f.Turn();Assert.False(f.Food.GetPart<FieldHarvestPart>().Harvested);}}
        [Test] public void SavedFedAllowanceDoesNotRearmOnAnotherRipeRow(){using(var f=new Fixture()){f.Grazer();f.Turn();f.RoundTrip();Assert.True((bool)f.Get("Fed"));Assert.AreSame(f.Food,f.Get("Food"));Assert.False(f.Call("ConfigureForage",f.Zone,f.Reserve,f.Food));f.Turn();Assert.False(f.Reserve.GetPart<FieldHarvestPart>().Harvested);}}
        [Test] public void PaidSchedulerFeedingEndsExactlyOneActorAction(){using(var f=new Fixture()){f.Grazer();var turns=new TurnManager();turns.AddEntity(f.Actor);turns.AddEntity(f.Player);Assert.AreSame(f.Player,turns.ProcessUntilPlayerTurn());Assert.AreEqual(1,f.Probe.Ends);Assert.AreEqual(0,turns.GetEnergy(f.Actor));Assert.True(f.Food.GetPart<FieldHarvestPart>().Harvested);Assert.AreEqual(1,f.Brain.GoalCount);}}
        [Test] public void ActionVetoLeavesForageAndActorUntouched(){using(var f=new Fixture()){f.Grazer();f.Actor.AddPart(new NoActionProbe());var turns=new TurnManager();turns.AddEntity(f.Actor);turns.AddEntity(f.Player);turns.ProcessUntilPlayerTurn();Assert.False(f.Food.GetPart<FieldHarvestPart>().Harvested);Assert.AreEqual((10,10),f.Zone.GetEntityPosition(f.Actor));Assert.AreEqual(1,f.Probe.Ends);}}
    }
}
