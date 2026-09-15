using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Native final-pipeline contracts: travel, defense space,
    /// dependency atomicity and structural authority. No simulated save migration.</summary>
    public class GinmereCompositionAdversarialTests
    {
        [TestCase(false)] [TestCase(true)]
        public void Adversarial_RemovedNativeWaterDriesWhileSurvivingWaterRenewsItsLease(bool destroy)
        {
            var factory=GrovelandsCompositionTests.Factory();var zone=new Zone("Overworld.2.7.2");
            Assert.IsTrue(new GinmereCompositionBuilder(64).BuildZone(zone,factory,new Random(1)));
            var pools=zone.GetAllEntities().Where(e=>e.BlueprintName=="MirePool").Take(2).ToArray();
            var removed=zone.GetEntityCell(pools[0]);var survivor=zone.GetEntityCell(pools[1]);
            Assert.AreEqual(4,zone.TileState.CoatingTurns(removed.X,removed.Y,"water"),"The player should encounter water on the first action, not only after a terrain tick.");
            if(destroy)Assert.AreEqual(DestroyVerdict.Destroyed,DestructionSystem.Damage(pools[0],1000,null,zone));
            else zone.RemoveEntity(pools[0]);
            for(int turn=0;turn<6;turn++){ZoneTileStateSystem.SeedTerrainSources(zone);zone.TileState.Tick();}
            Assert.IsFalse(zone.TileState.HasCoating(removed.X,removed.Y,"water"));
            Assert.AreEqual(3,zone.TileState.CoatingTurns(survivor.X,survivor.Y,"water"));
            Assert.IsNull(zone.GetEntityCell(pools[0]));Assert.NotNull(zone.GetEntityCell(pools[1]));
        }
        [TestCase(1)] [TestCase(2)]
        public void Adversarial_DirectLowerLevelLoadRetainsNativeReturnRoute(int depth)
        {
            var manager=new OverworldZoneManager(GrovelandsCompositionTests.Factory(),64);
            var zone=manager.GetZone("Overworld.2.7."+depth);
            Assert.IsTrue(zone.GetAllEntities().Any(e=>e.HasPart<StairsUpPart>()),"Loading a lower Ginmere level first must not strand its visitor.");
            var parent=manager.GetZone("Overworld.2.7."+(depth-1));
            var edge=manager.GetConnections(parent.ZoneID).Single(c=>c.SourceZoneID==parent.ZoneID&&c.TargetZoneID==zone.ZoneID&&c.Type=="StairsDown");
            Assert.IsTrue(parent.GetCell(edge.SourceX,edge.SourceY).Objects.Any(e=>e.HasPart<StairsDownPart>()));
            Assert.IsTrue(zone.GetCell(edge.TargetX,edge.TargetY).Objects.Any(e=>e.HasPart<StairsUpPart>()));
        }
        [TestCase("Sack","Container",1)] [TestCase("MirePool","LiquidPool",2)]
        [TestCase("MirePool","TileStateSource",2)] [TestCase("PrickleBrowGecko","Brain",2)]
        [TestCase("PricklebrowNest","PricklebrowNest",2)]
        [TestCase("MirePool","Thermal",2)] [TestCase("MirePool","Destructible",2)] [TestCase("MirePool","BurnOffGas",2)]
        public void Adversarial_PresentButMalformedBlueprintRejectsBeforeAnyNativePlacement(string bp,string part,int depth)
        {
            var factory=GrovelandsCompositionTests.Factory();Assert.IsTrue(factory.Blueprints[bp].Parts.Remove(part));
            var zone=new Zone("Overworld.2.7."+depth);zone.GenReservedCells.Add((79,24));
            Assert.IsFalse(new GinmereCompositionBuilder(64).BuildZone(zone,factory,new Random(1)));
            Assert.AreEqual(0,zone.EntityCount);CollectionAssert.AreEquivalent(new[]{(79,24)},zone.GenReservedCells);
        }
        [TestCase("Grass",0)] [TestCase("Tree",0)] [TestCase("SandstoneFloor",0)] [TestCase("SandstoneFloor",1)]
        [TestCase("RopeAnchor",1)] [TestCase("HealingTonic",1)] [TestCase("MirePool",2)]
        [TestCase("PrickleBrowGecko",2)] [TestCase("PricklebrowNest",2)]
        public void Adversarial_MissingNativeContentCannotLeavePartialTerrain(string bp,int depth)
        {
            var factory=GrovelandsCompositionTests.Factory();Assert.IsTrue(factory.Blueprints.Remove(bp));
            var z=new Zone("Overworld.2.7."+depth);
            Assert.IsFalse(new GinmereCompositionBuilder(64).BuildZone(z,factory,new Random(1)));
            Assert.AreEqual(0,z.EntityCount);Assert.AreEqual(0,z.GenReservedCells.Count);
        }

        [TestCase("Overworld.2.7.0")] [TestCase("Overworld.2.7.1")] [TestCase("Overworld.4.6.2")]
        public void Adversarial_NestFinalizerHasNoAuthorityOutsideGinmereFloor(string id)
        {
            var z=new Zone(id);
            Assert.IsFalse(new GinmereNestFinalizer().BuildZone(z,GrovelandsCompositionTests.Factory(),new Random(1)));
            Assert.AreEqual(0,z.EntityCount);
        }

        [TestCase(2048)] [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void Adversarial_FinalNativeStackKeepsReciprocalStairsAndStaticTraversal(int seed)
        {
            var manager=new OverworldZoneManager(GrovelandsCompositionTests.Factory(),seed);
            var levels=new[]{manager.GetZone("Overworld.2.7.0"),manager.GetZone("Overworld.2.7.1"),manager.GetZone("Overworld.2.7.2")};
            for(int depth=0;depth<3;depth++)
            {
                var z=levels[depth];
                Assert.IsTrue(z.GetAllEntities().Any(e=>e.BlueprintName==(depth==0?"SinkholeLip":depth==1?"RopeAnchor":"PricklebrowNest")));
                foreach(var stair in z.GetAllEntities().Where(e=>e.HasPart<StairsUpPart>()||e.HasPart<StairsDownPart>()))
                {var c=z.GetEntityCell(stair);Assert.IsTrue(z.GenReservedCells.Contains((c.X,c.Y)));Assert.IsFalse(c.BlocksMovement(),z.ZoneID+" obstructed native stair");}
                foreach(var actor in z.GetAllEntities().Where(e=>e.HasPart<BrainPart>()||e.HasTag("Creature")).ToArray())z.RemoveEntity(actor);
                Assert.IsTrue(FormationReachability.FullyReached(z,FormationReachability.FloodFromWest(z)),z.ZoneID+" static pocket seed"+seed);
                if(depth==2)continue;
                var edge=manager.GetConnections(z.ZoneID).Single(c=>c.SourceZoneID==z.ZoneID&&c.TargetZoneID==levels[depth+1].ZoneID&&c.Type=="StairsDown");
                Assert.IsTrue(z.GetCell(edge.SourceX,edge.SourceY).Objects.Any(e=>e.HasPart<StairsDownPart>()));
                Assert.IsTrue(levels[depth+1].GetCell(edge.TargetX,edge.TargetY).Objects.Any(e=>e.HasPart<StairsUpPart>()));
            }
        }

        [TestCase(2048,false)] [TestCase(64,false)] [TestCase(1729,false)]
        [TestCase(64,true)] [TestCase(1729,true)]
        public void Adversarial_FinalFloorNestUsesRealTriggerAndSixteenScheduledDefenders(int seed,bool block)
        {
            var factory=GrovelandsCompositionTests.Factory();var priorFactory=PricklebrowNestPart.Factory;var priorTurn=TurnManager.Active;
            try
            {
                FactionManager.Initialize();PricklebrowNestPart.Factory=factory;var turns=new TurnManager();
                var zone=new OverworldZoneManager(factory,seed).GetZone("Overworld.2.7.2");
                var nest=zone.GetAllEntities().Single(e=>e.BlueprintName=="PricklebrowNest");var c=zone.GetEntityCell(nest);
                var actor=new Entity{BlueprintName="GinmereTestVisitor"};actor.SetTag("Creature");actor.SetTag("Player");
                actor.AddPart(new PhysicsPart{Solid=true});actor.Statistics["Hitpoints"]=new Stat{Name="Hitpoints",BaseValue=1000,Max=1000};
                Assert.IsTrue(zone.AddEntity(actor,c.X,c.Y));
                int before=zone.GetAllEntities().Count(e=>e.BlueprintName=="PrickleBrowGecko"),scheduled=turns.EntityCount;
                var added=new List<(int,int)>();
                if(block)for(int dy=-5;dy<=5;dy++)for(int dx=-5;dx<=5;dx++)
                {var p=(c.X+dx,c.Y+dy);if(zone.InBounds(p.Item1,p.Item2)&&zone.GenReservedCells.Add(p))added.Add(p);}
                Trigger(nest,actor,c);
                Assert.AreEqual(!block,nest.GetPart<PricklebrowNestPart>().Triggered);
                Assert.AreEqual(before+(block?0:16),zone.GetAllEntities().Count(e=>e.BlueprintName=="PrickleBrowGecko"));
                Assert.AreEqual(scheduled+(block?0:16),turns.EntityCount);
                if(block){foreach(var p in added)zone.GenReservedCells.Remove(p);Trigger(nest,actor,c);
                    Assert.IsTrue(nest.GetPart<PricklebrowNestPart>().Triggered);Assert.AreEqual(before+16,zone.GetAllEntities().Count(e=>e.BlueprintName=="PrickleBrowGecko"));}
                Trigger(nest,actor,c);Assert.AreEqual(before+16,zone.GetAllEntities().Count(e=>e.BlueprintName=="PrickleBrowGecko"),"A second entry cannot duplicate a native swarm.");
            }
            finally
            {
                PricklebrowNestPart.Factory=priorFactory;
                typeof(TurnManager).GetProperty("Active",BindingFlags.Public|BindingFlags.Static).SetValue(null,priorTurn);
            }
        }
        private static void Trigger(Entity nest,Entity actor,Cell c)
        {var evt=GameEvent.New("EntityEnteredCell");evt.SetParameter("Actor",actor);evt.SetParameter("Cell",c);nest.FireEventAndRelease(evt);}
    }
}
