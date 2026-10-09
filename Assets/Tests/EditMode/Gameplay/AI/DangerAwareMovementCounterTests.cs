using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    // Native state, ordering, collision and actual steam integration controls.
    public sealed class DangerAwareMovementCounterTests
    {
        Zone zone;Entity actor;
        [SetUp]public void Setup()
        {
            LiquidRegistry.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(Application.dataPath,"Resources/Content/Data/LiquidDefinitions"),"*.json").Select(File.ReadAllText));
            zone=new Zone("danger-choice-counter");actor=new Entity{ID="counter-walker"};actor.SetTag("Creature");actor.AddPart(new PhysicsPart{Solid=true});actor.AddPart(new StatusEffectsPart());
            foreach(string s in new[]{"Hitpoints","Strength","Toughness","Agility"})actor.Statistics[s]=new Stat{Name=s,BaseValue=20,Max=100};Assert.True(zone.AddEntity(actor,10,10));
        }
        [TearDown]public void Teardown()=>LiquidRegistry.ResetForTests();
        void Acid(int x,int y){var e=new Entity{ID=Guid.NewGuid().ToString("N")};e.AddPart(new LiquidPoolPart{LiquidId="acid",Volume=100});Assert.True(zone.AddEntity(e,x,y));}
        sealed class AttemptProbe : Part
        {
            public readonly System.Collections.Generic.List<(int,int)> Cells=new System.Collections.Generic.List<(int,int)>();
            public override bool HandleEvent(GameEvent e)
            {if(e.ID=="BeforeMove"){var cell=e.GetParameter<Cell>("TargetCell");Cells.Add((cell.X,cell.Y));}return true;}
        }
        AttemptProbe Probe()
        {var p=actor.GetPart<PhysicsPart>();actor.RemovePart(p);var probe=new AttemptProbe();actor.AddPart(probe);actor.AddPart(p);return probe;}
        void Wall(int x,int y,bool physicalOnly=false)
        {var wall=new Entity{ID=Guid.NewGuid().ToString("N")};wall.AddPart(new PhysicsPart{Solid=true});if(!physicalOnly)wall.SetTag("Solid");Assert.True(zone.AddEntity(wall,x,y));}
        [TestCase(8,5,10,11)][TestCase(12,5,10,11)][TestCase(5,8,11,10)][TestCase(5,12,11,10)]
        public void UnequalAxisBearingRejectsAFallbackThatDoesNotIncreaseDistance(int threatX,int threatY,int expectedX,int expectedY)
        {
            Acid(10+Math.Sign(10-threatX),10+Math.Sign(10-threatY));
            Assert.True(AIHelpers.TryStepAway(actor,zone,10,10,threatX,threatY));var at=zone.GetEntityPosition(actor);
            Assert.AreEqual((expectedX,expectedY),at);Assert.Greater(AIHelpers.ChebyshevDistance(at.x,at.y,threatX,threatY),AIHelpers.ChebyshevDistance(10,10,threatX,threatY));
        }
        [TestCase(false)][TestCase(true)]
        public void BlockedLowCostCandidateIsNotSentThroughMovementAsAProbe(bool physicsOnly)
        {
            Acid(11,11);Wall(11,10,physicsOnly);var probe=Probe();
            Assert.True(AIHelpers.TryStepAway(actor,zone,10,10,8,8));Assert.AreEqual((10,11),zone.GetEntityPosition(actor));
            CollectionAssert.AreEqual(new[]{(10,11)},probe.Cells);
        }
        [TestCase(false)][TestCase(true)]
        public void HazardousDiagonalPreservesExistingBodyCornerRule(bool footprint)
        {
            if(footprint){Assert.True(zone.RemoveEntity(actor));actor.AddPart(new SpatialFootprintPart{CellsRaw="0,0"});Assert.True(zone.AddEntity(actor,10,10));}
            Acid(11,11);Wall(11,10);Wall(10,11);var probe=Probe();
            Assert.AreEqual(!footprint,AIHelpers.TryStepAway(actor,zone,10,10,8,8));
            Assert.AreEqual(footprint?(10,10):(11,11),zone.GetEntityPosition(actor));
            Assert.AreEqual(footprint?0:1,probe.Cells.Count);
        }
        [Test]public void SaferLockedDoorUsesExistingMatchingKeyAsOneStationaryAction()
        {
            Acid(11,10);actor.SetTag("CanOpenDoors");actor.AddPart(new InventoryPart());
            var key=new Entity{ID="matching-key"};key.AddPart(new PhysicsPart{Takeable=true});key.AddPart(new KeyPart{KeyId="retreat"});Assert.True(actor.GetPart<InventoryPart>().AddObject(key));
            var door=new Entity{ID="safe-locked-door"};door.AddPart(new PhysicsPart());door.AddPart(new RenderPart());var part=new DoorPart{IsOpen=false};door.AddPart(part);var latch=new LockPart{KeyId="retreat",IsLocked=true};door.AddPart(latch);Assert.True(zone.AddEntity(door,11,9));
            var probe=Probe();Assert.True(AIHelpers.TryStepAway(actor,zone,10,10,8,10));Assert.False(latch.IsLocked);Assert.False(part.IsOpen);Assert.AreEqual((10,10),zone.GetEntityPosition(actor));CollectionAssert.AreEqual(new[]{(11,9)},probe.Cells);
        }
        [Test]public void SandedIceRestoresActualCleanDirectPreference()
        {
            zone.TileState.WriteCoating(11,10,"ice",10);zone.TileState.WriteResidue(11,10,"grit",10);Assert.Zero(TerrainNavigationWeight.ForStep(zone,11,10,actor));
            Assert.True(AIHelpers.TryApproachWithPathfinding(actor,zone,10,10,15,10));Assert.AreEqual((11,10),zone.GetEntityPosition(actor));
        }
        [TestCase(false)][TestCase(true)]
        public void EqualDangerRetreatCostsKeepPreferredDirection(bool diagonal)
        {
            if(diagonal){Acid(11,11);Acid(11,10);Acid(10,11);}else{Acid(11,10);Acid(11,9);Acid(11,11);}
            Assert.True(AIHelpers.TryStepAway(actor,zone,10,10,8,diagonal?8:10));Assert.AreEqual((11,diagonal?11:10),zone.GetEntityPosition(actor));
        }
        [Test]public void ChoosingASaferDoorSpendsTheTurnWithoutThenEnteringDanger()
        {
            Acid(11,10);actor.SetTag("CanOpenDoors");var door=new Entity{ID="safe-door"};door.AddPart(new PhysicsPart());door.AddPart(new RenderPart());var part=new DoorPart{IsOpen=false};door.AddPart(part);Assert.True(zone.AddEntity(door,11,9));
            Assert.True(AIHelpers.TryStepAway(actor,zone,10,10,8,10));Assert.True(part.IsOpen);Assert.AreEqual((10,10),zone.GetEntityPosition(actor));
        }
    }
    public sealed class DangerAwareHotSteamMovementTests : HotSteamContactFixture
    {
        [TestCase(false)][TestCase(true)]
        public void ExistingRealHotContactChangesApproachUnlessFullyHeatImmune(bool immune)
        {
            Assert.True(zone.MoveEntity(actor,8,10));actor.GetStat("HeatResistance").BaseValue=immune?100:0;
            Assert.AreEqual(!immune,TerrainNavigationWeight.ForStep(zone,9,10,actor)>0);
            Assert.True(AIHelpers.TryApproachWithPathfinding(actor,zone,8,10,15,10));var at=zone.GetEntityPosition(actor);
            Assert.AreEqual(immune,at==(9,10));Assert.Zero(TerrainNavigationWeight.ForStep(zone,at.x,at.y,actor));
        }
    }
}
