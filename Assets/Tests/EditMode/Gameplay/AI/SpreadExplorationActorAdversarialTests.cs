using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public class SpreadExplorationActorAdversarialTests
    {
        [TestCase(0)][TestCase(1)]
        public void OnlyActualPlayerDamageBypassesTheRemainingTerritoryWarning(int amount)
        {using(var f=new SpreadExplorationActorTests.Fixture()){f.Territory();f.Move(f.Player,11,10);f.Turn();CombatSystem.ApplyDamage(f.Actor,amount,f.Player,f.Zone);f.Turn();Assert.AreEqual(40-amount,f.Actor.GetStatValue("Hitpoints"));Assert.AreEqual(amount,f.Probe.Attacks);Assert.AreEqual(amount==1,f.Brain.IsPersonallyHostileTo(f.Player));}}
        [TestCase("SpreadTerritory")][TestCase("SpreadGrazer")]
        public void ExistingFactoryRegistersTheRoleWithoutAParallelRegistry(string name)
        {var factory=new EntityFactory();factory.LoadBlueprints("{\"Objects\":[{\"Name\":\"ActorRoleProbe\",\"Parts\":[{\"Name\":\""+name+"\"}]}]}");var actor=factory.CreateEntity("ActorRoleProbe");Assert.NotNull(actor);var role=actor.Parts.Single(p=>p.Name==name);Assert.AreSame(actor,role.ParentEntity);}
        [TestCase("SpreadTerritoryPart")][TestCase("SpreadGrazerPart")]
        public void DetachedRoleCannotConfigureAFormerOwner(string kind)
        {using(var f=new SpreadExplorationActorTests.Fixture()){f.Add(kind);f.Actor.RemovePart(f.Role);Assert.False(kind=="SpreadTerritoryPart"?f.Call("Configure",f.Zone,f.Post,8,8,14,13,2):f.Call("ConfigureForage",f.Zone,f.Food,f.Reserve));}}
        [TestCase("carried")][TestCase("foreign-physics")][TestCase("detached")]
        public void TerritoryRefusesAnInvalidPostWithoutArming(string kind)
        {using(var f=new SpreadExplorationActorTests.Fixture()){f.Add("SpreadTerritoryPart");if(kind=="carried")f.Post.GetPart<PhysicsPart>().InInventory=f.Player;if(kind=="foreign-physics")f.Post.GetPart<PhysicsPart>().ParentEntity=f.Player;if(kind=="detached")f.Zone.RemoveEntity(f.Post);Assert.False(f.Call("Configure",f.Zone,f.Post,8,8,14,13,2));Assert.False((bool)f.Get("Configured"));}}
        [TestCase("dead")][TestCase("foreign-physics")][TestCase("foreign-brain")]
        public void OwnerMustBeCurrentLivingAndOwnedBeforeConfiguration(string kind)
        {using(var f=new SpreadExplorationActorTests.Fixture()){f.Add("SpreadGrazerPart");if(kind=="dead")f.Actor.GetStat("Hitpoints").BaseValue=0;if(kind=="foreign-physics")f.Actor.GetPart<PhysicsPart>().ParentEntity=f.Player;if(kind=="foreign-brain")f.Brain.ParentEntity=f.Player;Assert.False(f.Call("ConfigureForage",f.Zone,f.Food,f.Reserve));}}
        [TestCase("dead")][TestCase("removed")][TestCase("foreign-physics")]
        public void StaleWarningTargetCannotReceiveAnAttack(string kind)
        {using(var f=new SpreadExplorationActorTests.Fixture()){f.Territory();f.Move(f.Player,11,10);for(int i=0;i<3;i++)f.Turn();if(kind=="dead")f.Player.GetStat("Hitpoints").BaseValue=0;if(kind=="removed")f.Zone.RemoveEntity(f.Player);if(kind=="foreign-physics")f.Player.GetPart<PhysicsPart>().ParentEntity=f.Actor;f.Turn();Assert.AreEqual(0,f.Probe.Attacks);Assert.IsNull(f.Get("WarningTarget"));}}
        [Test] public void ClearedFactionHostilityReleasesWarningWithoutChangingReputation()
        {using(var f=new SpreadExplorationActorTests.Fixture()){f.Territory();f.Move(f.Player,11,10);f.Turn();PlayerReputation.Set("OutlandRaiders",100);f.Turn();Assert.IsNull(f.Get("WarningTarget"));Assert.AreEqual(100,PlayerReputation.Get("OutlandRaiders"));Assert.False(f.Brain.IsPersonallyHostileTo(f.Player));}}
        [Test] public void ExplicitPersonalEnemyTakesPrecedenceOverCloserGenericIntruder()
        {using(var f=new SpreadExplorationActorTests.Fixture()){f.Territory();f.Move(f.Player,15,10);f.Brain.SetPersonallyHostile(f.Player,false);var intruder=f.Prop("intruder",10,11);intruder.Tags["Creature"]="true";intruder.Tags["Faction"]="Villagers";intruder.AddPart(new BrainPart());intruder.Statistics["Hitpoints"]=new Stat{Name="Hitpoints",BaseValue=10,Max=10,Owner=intruder};f.Turn();Assert.AreSame(f.Player,f.Brain.Target);Assert.AreEqual(0,f.Probe.Attacks);Assert.AreEqual((11,10),f.Zone.GetEntityPosition(f.Actor));}}
        [Test] public void ForeignCurrentZoneCannotOperateSavedDuty()
        {using(var f=new SpreadExplorationActorTests.Fixture()){f.Territory();f.Brain.CurrentZone=new Zone(f.Zone.ZoneID);f.Turn();Assert.IsNull(f.Get("WarningTarget"));Assert.AreEqual((10,10),f.Zone.GetEntityPosition(f.Actor));}}
        [TestCase("foreign-harvest")][TestCase("foreign-render")][TestCase("carried")][TestCase("morphed")]
        public void ChangedAllocatedFoodRefusesConsumption(string kind)
        {using(var f=new SpreadExplorationActorTests.Fixture()){f.Grazer();if(kind=="foreign-harvest")f.Food.GetPart<FieldHarvestPart>().ParentEntity=f.Player;if(kind=="foreign-render")f.Food.GetPart<RenderPart>().ParentEntity=f.Player;if(kind=="carried")f.Food.GetPart<PhysicsPart>().InInventory=f.Player;if(kind=="morphed")f.Food.BlueprintName="Crop";f.Turn();Assert.False(f.Food.GetPart<FieldHarvestPart>().Harvested);Assert.False((bool)f.Get("Fed"));}}
        [Test] public void HealthyFlightPaysOnlyOneStepAndDoesNotAlterGoalStackOrFood()
        {using(var f=new SpreadExplorationActorTests.Fixture()){f.Grazer();f.Move(f.Player,12,10);var turn=new TurnManager();turn.AddEntity(f.Actor);turn.AddEntity(f.Player);turn.ProcessUntilPlayerTurn();Assert.AreEqual((9,10),f.Zone.GetEntityPosition(f.Actor));Assert.AreEqual(1,f.Probe.Ends);Assert.AreEqual(0,turn.GetEnergy(f.Actor));Assert.AreEqual(1,f.Brain.GoalCount);Assert.False(f.Food.GetPart<FieldHarvestPart>().Harvested);}}
        [Test] public void CorneredGrazerDoesNotAttackOrEatThroughFear()
        {using(var f=new SpreadExplorationActorTests.Fixture()){f.Grazer();f.Move(f.Player,11,10);foreach(var p in new[]{(9,10),(9,9),(9,11)})f.Prop("wall"+p,p.Item1,p.Item2).GetPart<PhysicsPart>().Solid=true;f.Turn();Assert.AreEqual((10,10),f.Zone.GetEntityPosition(f.Actor));Assert.AreEqual(0,f.Probe.Attacks);Assert.False(f.Food.GetPart<FieldHarvestPart>().Harvested);}}
        [Test] public void BlockedForageHasSavedFiniteApproachBudget()
        {using(var f=new SpreadExplorationActorTests.Fixture()){f.Move(f.Food,15,10);f.Grazer();foreach(var p in new[]{(9,9),(10,9),(11,9),(9,10),(11,10),(9,11),(10,11),(11,11)})f.Prop("wall"+p,p.Item1,p.Item2).GetPart<PhysicsPart>().Solid=true;for(int i=0;i<30;i++)f.Turn();Assert.AreEqual(24,f.Get("ApproachAttempts"));f.RoundTrip();f.Turn();Assert.AreEqual(24,f.Get("ApproachAttempts"));Assert.False(f.Food.GetPart<FieldHarvestPart>().Harvested);}}
        [Test] public void PendingForageSurvivesFullGraphWithoutReplacingEitherRow()
        {using(var f=new SpreadExplorationActorTests.Fixture()){f.Grazer();f.RoundTrip();Assert.AreSame(f.Food,f.Get("Food"));Assert.AreSame(f.Reserve,f.Get("ReservedRow"));f.Turn();Assert.True(f.Food.GetPart<FieldHarvestPart>().Harvested);Assert.False(f.Reserve.GetPart<FieldHarvestPart>().Harvested);}}
        [Test] public void GrazerDoesNotIgnoreARealWideThreat()
        {using(var f=new SpreadExplorationActorTests.Fixture()){f.Grazer();f.Zone.RemoveEntity(f.Player);f.Player.AddPart(new SpatialFootprintPart{CellsRaw="0,0;-1,0"});Assert.True(f.Zone.AddEntity(f.Player,12,10));f.Turn();Assert.AreEqual((9,10),f.Zone.GetEntityPosition(f.Actor));Assert.False(f.Food.GetPart<FieldHarvestPart>().Harvested);}}
        [Test] public void DutyWarningSeesBodyInsideEvenWhenItsAnchorIsOutside()
        {using(var f=new SpreadExplorationActorTests.Fixture()){f.Territory();f.Zone.RemoveEntity(f.Player);f.Player.AddPart(new SpatialFootprintPart{CellsRaw="0,0;-1,0"});Assert.True(f.Zone.AddEntity(f.Player,15,10));f.Turn();Assert.AreSame(f.Player,f.Get("WarningTarget"));}}

        [TestCase("unchanged")][TestCase("actor-removed")][TestCase("actor-moved")]
        [TestCase("row-removed")][TestCase("row-moved")][TestCase("field-replaced")]
        [TestCase("role-replaced")][TestCase("reserve-removed")][TestCase("calm")][TestCase("threat")]
        public void FeedingGestureFollowsOneCommittedMealAndRejectsStalePresentation(string change)
        {
            using(var f=new SpreadExplorationActorTests.Fixture())
            {
                f.Grazer();var role=f.Role;var field=f.Food.GetPart<FieldHarvestPart>();int interactions=0,attacks=0;
                var hook=typeof(EntityVisualHooks).GetProperty("InteractionCallback",System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static);
                Assert.NotNull(hook,"Requires the actual typed interaction hook, not a fabricated attack.");
                Action<Entity,Entity,Zone> observer=(actor,target,zone)=>
                {
                    interactions++;Assert.AreSame(f.Actor,actor);Assert.AreSame(f.Food,target);Assert.AreSame(f.Zone,zone);
                    Assert.True((bool)f.Get("Fed"));Assert.True(field.Harvested);Assert.False(f.Reserve.GetPart<FieldHarvestPart>().Harvested);
                };
                hook.SetValue(null,Delegate.CreateDelegate(hook.PropertyType,observer.Target,observer.Method));
                EntityVisualHooks.AttackCallback=(a,b,z)=>attacks++;
                bool dirty=false;
                ZoneRenderHooks.CellDirtyCallback=(x,y,cause)=>
                {
                    if(cause!="FieldGrazed")return;dirty=true;
                    if(change=="actor-removed")f.Zone.RemoveEntity(f.Actor);
                    if(change=="actor-moved")Assert.True(f.Zone.MoveEntity(f.Actor,30,15));
                    if(change=="row-removed")f.Zone.RemoveEntity(f.Food);
                    if(change=="row-moved")Assert.True(f.Zone.MoveEntity(f.Food,30,15));
                    if(change=="field-replaced"){f.Food.RemovePart(field);f.Food.AddPart(new FieldHarvestPart{Harvested=true});}
                    if(change=="role-replaced"){f.Actor.RemovePart(role);f.Actor.AddPart((Part)Activator.CreateInstance(role.GetType()));}
                    if(change=="reserve-removed")f.Zone.RemoveEntity(f.Reserve);
                };
                if(change=="calm")f.Brain.PushGoal(new NoFightGoal(10));
                if(change=="threat")f.Move(f.Player,12,10);
                f.Turn();bool fed=change!="calm"&&change!="threat";
                Assert.AreEqual(fed,dirty);Assert.AreEqual(fed,field.Harvested);Assert.AreEqual(fed,(bool)role.GetType().GetField("Fed").GetValue(role));
                Assert.AreEqual(change=="unchanged"?1:0,interactions);Assert.AreEqual(0,attacks);Assert.AreEqual(0,f.Actor.GetPart<InventoryPart>().Objects.Count);
                if(change=="unchanged"){f.Turn();Assert.AreEqual(1,interactions);Assert.False(f.Reserve.GetPart<FieldHarvestPart>().Harvested);}
            }
        }

        [Test] public void GrazerProgressIsCommittedBeforePresentationCallback()
        {using(var f=new SpreadExplorationActorTests.Fixture()){f.Grazer();bool callback=false;ZoneRenderHooks.CellDirtyCallback=(x,y,cause)=>{if(cause!="FieldGrazed")return;callback=true;Assert.True((bool)f.Get("Fed"));Assert.True(f.Food.GetPart<FieldHarvestPart>().Harvested);};f.Turn();Assert.True(callback);}}
    }
}
