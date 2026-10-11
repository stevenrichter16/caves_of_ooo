using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class CompanionStepAsideTests
    {
        Zone zone; Entity leader,follower; const string Command="CompanionStepAside";
        [SetUp] public void Setup()
        {
            FactionManager.Initialize();zone=new Zone("companion-aside");leader=Creature("leader",10,10);leader.SetTag("Player");follower=Creature("follower",11,10);
            for(int y=8;y<=12;y++)for(int x=9;x<=13;x++)zone.GetCell(x,y).IsVisible=zone.GetCell(x,y).Explored=true;
            Assert.True(follower.ApplyEffect(new RecruitedEffect(leader),leader,zone));
        }
        Entity Creature(string id,int x,int y)
        {var e=new Entity{ID=id,BlueprintName=id};e.SetTag("Creature");e.SetTag("Faction","Villagers");e.AddPart(new PhysicsPart{Solid=true});e.AddPart(new RenderPart{DisplayName=id});e.AddPart(new InventoryPart());e.AddPart(new StatusEffectsPart());e.AddPart(new BrainPart{CurrentZone=zone,Rng=new Random(1)});e.Statistics["Hitpoints"]=new Stat{Name="Hitpoints",BaseValue=20,Max=20};Assert.True(zone.AddEntity(e,x,y));return e;}
        bool Offered()=>WorldInteractionSystem.GatherActions(follower,leader).Any(a=>a.Command==Command);
        bool Run()=>InventorySystem.PerformAction(leader,follower,Command,zone);
        Entity Put(Part part,int x,int y){var e=new Entity();e.AddPart(part);Assert.True(zone.AddEntity(e,x,y));return e;}
        void Enclose(bool leaveEast)
        {for(int y=9;y<=11;y++)for(int x=10;x<=12;x++)if((x,y)!=(11,10)&&(x,y)!=(10,10)&&(!leaveEast||(x,y)!=(12,10))){var wall=Put(new PhysicsPart{Solid=true},x,y);wall.SetTag("Solid");}}
        [TestCase(false)] [TestCase(true)] public void AskMovesOnlyCompanionOneSafeCellAndPreservesSavedOrder(bool stay)
        {
            if(stay)Assert.True(InventorySystem.PerformAction(leader,follower,"CompanionStay",zone));Enclose(true);Assert.True(Offered());
            Assert.True(Run());Assert.AreEqual((12,10),zone.GetEntityPosition(follower));Assert.AreEqual((10,10),zone.GetEntityPosition(leader));
            Assert.AreEqual(stay,CompanionOrders.IsStaying(follower));Assert.AreSame(leader,follower.GetPart<BrainPart>().PartyLeader);
        }
        [TestCase("water")] [TestCase("gas")] [TestCase("trigger")] [TestCase("friend")] [TestCase("unknown")]
        public void OnlyAvailableHazardOrOccupiedCellIsRefused(string hazard)
        {
            Enclose(true);Assert.True(Offered());Entity obstruction=null;
            if(hazard=="water")obstruction=Put(new LiquidPoolPart{LiquidId="water",Volume=100},12,10);
            if(hazard=="gas")obstruction=Put(new GasPoolPart(),12,10);
            if(hazard=="trigger")obstruction=Put(new StepTrap(),12,10);
            if(hazard=="friend")obstruction=Creature("friend",12,10);
            if(hazard=="unknown")zone.GetCell(12,10).IsVisible=false;
            Assert.False(Offered());Assert.False(Run());Assert.AreEqual((11,10),zone.GetEntityPosition(follower));
            if(obstruction!=null)Assert.True(zone.RemoveEntity(obstruction));else zone.GetCell(12,10).IsVisible=true;
            Assert.True(Offered());Assert.True(Run());
        }
        [Test] public void FullyEnclosedCompanionIsNotSwappedWithItsLeader()
        {Enclose(false);Assert.False(Offered());Assert.False(Run());Assert.AreEqual((11,10),zone.GetEntityPosition(follower));Assert.AreEqual((10,10),zone.GetEntityPosition(leader));}
        public sealed class Callback:Part{public string Event;public Action Change;public bool Veto;public override string Name=>"StepAsideCallback";public override bool HandleEvent(GameEvent e){if(e.ID==Event){Change?.Invoke();return !Veto;}return true;}}
        [TestCase(false)] [TestCase(true)] public void VoluntaryVetoAndRootingPreventMovement(bool root)
        {Enclose(true);Assert.True(Offered());if(root)Assert.True(follower.ApplyEffect(new RootedEffect(),leader,zone));else follower.AddPart(new Callback{Event="BeforeMove",Veto=true});Assert.False(Run());Assert.AreEqual((11,10),zone.GetEntityPosition(follower));}
        [Test] public void LateOuterFailureRestoresOriginalCellAndStayOrder()
        {Enclose(true);Assert.True(InventorySystem.PerformAction(leader,follower,"CompanionStay",zone));Assert.True(Offered());leader.AddPart(new Callback{Event="AfterInventoryAction",Change=()=>throw new InvalidOperationException("aside rollback")});Assert.False(Run());Assert.AreEqual((11,10),zone.GetEntityPosition(follower));Assert.True(CompanionOrders.IsStaying(follower));}
        [Test] public void BeforeMoveCallbackCannotIntroduceAHazardAfterSafeChoice()
        {Enclose(true);Assert.True(Offered());follower.AddPart(new Callback{Event="BeforeMove",Change=()=>Put(new StepTrap(),12,10)});Assert.False(Run());Assert.AreEqual((11,10),zone.GetEntityPosition(follower));}
        [TestCase("dismiss")] [TestCase("far")] [TestCase("dead")] public void StaleOrUnauthorizedOrderCannotMoveAnyone(string change)
        {Assert.True(Offered());if(change=="dismiss")follower.GetEffect<RecruitedEffect>().Dismiss(leader);if(change=="far")Assert.True(zone.MoveEntity(leader,20,10));if(change=="dead")follower.GetStat("Hitpoints").BaseValue=0;Assert.False(Offered());Assert.False(Run());Assert.AreEqual((11,10),zone.GetEntityPosition(follower));}
        public sealed class StepTrap:TriggerOnStepPart { public override string Name=>"CompanionSafetyProbeTrap"; protected override void OnTrigger(Entity actor,Zone at){actor.GetStat("Hitpoints").BaseValue-=1;} }
        [Test] public void NonSolidFootprintTriggerAtRemoteAnchorStillMakesLandingUnsafe()
        {
            Enclose(true);Assert.True(Offered());var trap=new Entity();trap.AddPart(new PhysicsPart{Solid=false});trap.AddPart(new StepTrap());trap.AddPart(new SpatialFootprintPart{CellsRaw="0,0;-1,0"});Assert.True(zone.AddEntity(trap,13,10));
            Assert.True(zone.GetCell(12,10).Occupants.Contains(trap));Assert.False(zone.GetCell(12,10).Objects.Contains(trap));
            Assert.False(Offered());Assert.False(Run());Assert.AreEqual((11,10),zone.GetEntityPosition(follower));Assert.AreEqual(20,follower.GetStatValue("Hitpoints"));
            Assert.True(zone.RemoveEntity(trap));Assert.True(Offered());Assert.True(Run());
        }
        [Test] public void StunnedCompanionCannotPerformStepAside()
        {Enclose(true);Assert.True(Offered());Assert.True(follower.ApplyEffect(new StunnedEffect(2),leader,zone));Assert.False(Run());Assert.AreEqual((11,10),zone.GetEntityPosition(follower));}
        [Test] public void MultiCellCompanionIsNotMovedBySingleCellCourtesy()
        {Assert.True(Offered());Assert.True(zone.RemoveEntity(follower));follower.AddPart(new SpatialFootprintPart{CellsRaw="0,0;1,0"});Assert.True(zone.AddEntity(follower,11,10));Assert.False(Offered());Assert.False(Run());Assert.AreEqual((11,10),zone.GetEntityPosition(follower));}
        [TestCase(false)] [TestCase(true)] public void IndependentBeforeMoveRelocationIsNotRewoundOrMovedAgain(bool toChosenDestination)
        {Enclose(true);Assert.True(Offered());int landing=toChosenDestination?12:13;follower.AddPart(new Callback{Event="BeforeMove",Change=()=>Assert.True(zone.MoveEntity(follower,landing,10))});Assert.False(Run());Assert.AreEqual((landing,10),zone.GetEntityPosition(follower));}
        [Test] public void RollbackDoesNotOverlapANewOccupantOfTheOriginalCell()
        {
            Enclose(true);Assert.True(Offered());Entity newcomer=null;leader.AddPart(new Callback{Event="AfterInventoryAction",Change=()=>{newcomer=Creature("newcomer",11,10);throw new InvalidOperationException("occupied rollback");}});
            Assert.False(Run());Assert.AreEqual((11,10),zone.GetEntityPosition(newcomer));Assert.AreEqual((12,10),zone.GetEntityPosition(follower));Assert.AreEqual(1,zone.GetCell(11,10).Occupants.Count(e=>e.HasTag("Creature")));
        }
    }
}
