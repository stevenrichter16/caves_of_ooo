using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    // Native movement-choice contracts, authored before production.
    // Public behavior tests of existing commands, not a new planner API.
    public sealed class DangerAwareMovementTests
    {
        Zone zone; Entity actor;
        [SetUp] public void Setup()
        {
            LiquidRegistry.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(Application.dataPath,"Resources/Content/Data/LiquidDefinitions"),"*.json").Select(File.ReadAllText));
            GasRegistry.Initialize("{\"Gases\":[{\"Id\":\"nav-poison\",\"GasType\":\"Poison\",\"DefaultDensity\":100,\"BehaviorKind\":\"Poison\"}]}");
            zone=new Zone("danger-aware-choice"); actor=new Entity{ID="walker"};actor.SetTag("Creature");
            actor.AddPart(new PhysicsPart{Solid=true});actor.AddPart(new StatusEffectsPart());
            foreach(string s in new[]{"Hitpoints","Strength","Toughness","Agility"})actor.Statistics[s]=new Stat{Name=s,BaseValue=20,Max=100};
            Assert.True(zone.AddEntity(actor,10,10));
        }
        [TearDown] public void Teardown(){LiquidRegistry.ResetForTests();GasRegistry.ResetForTests();}
        Entity Pool(int x,int y,string id="acid")
        {var e=new Entity{ID=Guid.NewGuid().ToString("N")};e.AddPart(new LiquidPoolPart{LiquidId=id,Volume=100});Assert.True(zone.AddEntity(e,x,y));return e;}
        void Wall(int x,int y)
        {var e=new Entity{ID=Guid.NewGuid().ToString("N")};e.SetTag("Solid");e.AddPart(new PhysicsPart{Solid=true});Assert.True(zone.AddEntity(e,x,y));}
        void Resistance(string name,int amount)=>actor.Statistics[name]=new Stat{Name=name,BaseValue=amount,Max=1000};
        bool Approach(int x=15,int y=10){var at=zone.GetEntityPosition(actor);return AIHelpers.TryApproachWithPathfinding(actor,zone,at.x,at.y,x,y);}
        int HereCost(){var at=zone.GetEntityPosition(actor);return TerrainNavigationWeight.ForStep(zone,at.x,at.y,actor);}
        [TestCase("acid")][TestCase("gas")][TestCase("ice")]
        public void ImmediateDangerUsesExistingClearDetour(string hazard)
        {
            if(hazard=="gas")Assert.NotNull(GasFactory.SpawnGas(zone,11,10,"nav-poison",density:100));
            else if(hazard=="ice")zone.TileState.WriteCoating(11,10,"ice",10);
            else Pool(11,10);
            Assert.Greater(TerrainNavigationWeight.ForStep(zone,11,10,actor),0);
            Assert.True(Approach());Assert.AreNotEqual((11,10),zone.GetEntityPosition(actor));Assert.Zero(HereCost());
        }
        [TestCase("none")][TestCase("water")]
        public void HarmlessIdealStepKeepsItsOrdinaryDirectChoice(string pool)
        {if(pool!="none")Pool(11,10,pool);Assert.True(Approach());Assert.AreEqual((11,10),zone.GetEntityPosition(actor));}
        [TestCase(99,false)][TestCase(100,true)]
        public void AcidResistanceAffectsActualApproachChoice(int resistance,bool direct)
        {Resistance("AcidResistance",resistance);Pool(11,10);Assert.True(Approach());Assert.AreEqual(direct,zone.GetEntityPosition(actor)==(11,10));}
        [TestCase("Poison",true)][TestCase("poison",false)][TestCase("Cryo",false)]
        public void ExactActualGasImmunityControlsTheChoice(string immunity,bool direct)
        {actor.AddPart(new GasImmunityPart{GasType=immunity});Assert.NotNull(GasFactory.SpawnGas(zone,11,10,"nav-poison",density:100));Assert.True(Approach());Assert.AreEqual(direct,zone.GetEntityPosition(actor)==(11,10));}
        [Test] public void SecondaryPhysicalFootAvoidsDangerEvenWhenAnchorIsClear()
        {
            Assert.True(zone.RemoveEntity(actor));actor.AddPart(new SpatialFootprintPart{CellsRaw="0,0;0,1"});Assert.True(zone.AddEntity(actor,10,10));Pool(11,11);
            Assert.Zero(TerrainNavigationWeight.ForCell(zone.GetCell(11,10),actor));Assert.Greater(TerrainNavigationWeight.ForStep(zone,11,10,actor),0);
            Assert.True(Approach());Assert.Zero(HereCost());
        }
        [Test] public void RepeatedApproachActuallyPassesThePoolAndReachesTheKnownWaypoint()
        {
            Pool(11,10);for(int i=0;i<12&&zone.GetEntityPosition(actor)!=(15,10);i++){Assert.True(Approach());Assert.Zero(HereCost());}
            Assert.AreEqual((15,10),zone.GetEntityPosition(actor));
        }
        [Test] public void SoleHazardousPassageRemainsUsable()
        {
            for(int x=0;x<Zone.Width;x++)for(int y=0;y<Zone.Height;y++)if(y!=10)Wall(x,y);
            Pool(11,10);Assert.True(Approach());Assert.AreEqual((11,10),zone.GetEntityPosition(actor));
        }
        [TestCase(false)][TestCase(true)]
        public void RetreatSelectsCleanOutwardStepForCardinalAndDiagonalContact(bool diagonal)
        {
            Pool(11,diagonal?11:10);Assert.True(AIHelpers.TryStepAway(actor,zone,10,10,8,diagonal?8:10));
            var at=zone.GetEntityPosition(actor);Assert.Zero(HereCost());Assert.Greater(AIHelpers.ChebyshevDistance(at.x,at.y,8,diagonal?8:10),2);
        }
        [TestCase(false)][TestCase(true)]
        public void CleanRetreatKeepsPreferredDirection(bool diagonal)
        {Assert.True(AIHelpers.TryStepAway(actor,zone,10,10,8,diagonal?8:10));Assert.AreEqual((11,diagonal?11:10),zone.GetEntityPosition(actor));}
        [Test] public void SoleHazardousRetreatExitDoesNotTurnIntoAForbiddenWall()
        {Pool(11,10);Wall(11,9);Wall(11,11);Assert.True(AIHelpers.TryStepAway(actor,zone,10,10,8,10));Assert.AreEqual((11,10),zone.GetEntityPosition(actor));}
        [Test] public void DoorOperationStillSpendsOneStationaryRetreatAction()
        {
            actor.SetTag("CanOpenDoors");var door=new Entity{ID="door"};door.AddPart(new PhysicsPart());door.AddPart(new RenderPart());var p=new DoorPart{IsOpen=false};door.AddPart(p);Assert.True(zone.AddEntity(door,11,10));
            Assert.True(AIHelpers.TryStepAway(actor,zone,10,10,8,10));Assert.True(p.IsOpen);Assert.AreEqual((10,10),zone.GetEntityPosition(actor));
        }
        [Test] public void ForcedDisplacementStillContactsDangerousDestination()
        {Pool(11,10);Assert.True(MovementSystem.ForceMoveTo(actor,zone,11,10));Assert.AreEqual((11,10),zone.GetEntityPosition(actor));Assert.Greater(HereCost(),0);}
    }
    public sealed class DangerAwareFiringPositionTests
    {
        FiftySecondCombatFixture f;
        [SetUp]public void Setup()
        {
            f=new FiftySecondCombatFixture();
            LiquidRegistry.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(Application.dataPath,"Resources/Content/Data/LiquidDefinitions"),"*.json").Select(File.ReadAllText));
        }
        [TearDown]public void Teardown(){LiquidRegistry.ResetForTests();f.Dispose();}
        void Pool(int x,int y)
        {var e=new Entity{ID=Guid.NewGuid().ToString("N")};e.AddPart(new LiquidPoolPart{LiquidId="acid",Volume=100});Assert.True(f.Zone.AddEntity(e,x,y));}
        [TestCase(false)][TestCase(true)]
        public void CoolingCasterChoosesCleanFiringStepUnlessActuallyImmune(bool immune)
        {
            var actor=f.Npc();var threat=f.Hostile(actor,12);var ally=f.Npc(11,10,"");actor.GetPart<CombatTacticsPart>().RepositionForShot=true;
            if(immune)actor.Statistics["AcidResistance"]=new Stat{Name="AcidResistance",BaseValue=100,Max=100};
            foreach(var a in actor.GetPart<ActivatedAbilitiesPart>().AbilityList)a.CooldownRemaining=5;
            Pool(11,9);f.Act(actor,threat);
            Assert.AreEqual((11,immune?9:11),f.Zone.GetEntityPosition(actor));Assert.AreEqual(1000,threat.GetStatValue("Hitpoints"));Assert.AreEqual(500,ally.GetStatValue("Hitpoints"));
            Assert.True(actor.GetPart<ActivatedAbilitiesPart>().AbilityList.All(a=>a.CooldownRemaining==5));
        }
        [Test] public void BothDangerousFiringChoicesStillPermitOneOrdinaryStep()
        {
            var actor=f.Npc();var threat=f.Hostile(actor,12);f.Npc(11,10,"");actor.GetPart<CombatTacticsPart>().RepositionForShot=true;
            foreach(var a in actor.GetPart<ActivatedAbilitiesPart>().AbilityList)a.CooldownRemaining=5;
            Pool(11,9);Pool(11,11);f.Act(actor,threat);Assert.AreNotEqual((10,10),f.Zone.GetEntityPosition(actor));Assert.AreEqual(1000,threat.GetStatValue("Hitpoints"));
        }
        [Test] public void PreferredRangeBackstepUsesTheSameDangerAwareRetreatChoice()
        {
            var actor=f.Npc();var threat=f.Hostile(actor,11);actor.GetPart<CombatTacticsPart>().PreferredRange=3;Pool(9,10);
            foreach(var a in actor.GetPart<ActivatedAbilitiesPart>().AbilityList)a.CooldownRemaining=5;
            f.Act(actor,threat);var at=f.Zone.GetEntityPosition(actor);Assert.AreEqual(9,at.x);Assert.AreNotEqual(10,at.y);
            Assert.Zero(TerrainNavigationWeight.ForStep(f.Zone,at.x,at.y,actor));Assert.AreEqual(1000,threat.GetStatValue("Hitpoints"));
        }
    }
}
