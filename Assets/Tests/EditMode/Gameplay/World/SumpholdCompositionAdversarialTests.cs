using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Data;
using System.Collections.Generic;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public class SumpholdCompositionAdversarialTests
    {
        [TestCase("Floor")] [TestCase("StoneFloor")] [TestCase("SandstoneWall")] [TestCase("WaterPuddle")]
        [TestCase("PeatBank")] [TestCase("Duckboard")] [TestCase("BoatFrame")] [TestCase("PeatCutter")] [TestCase("TollRolls")]
        public void MissingRequiredContentRejectsAtomically(string bp)
        {
            var f=GrovelandsCompositionTests.Factory();Assert.IsTrue(f.Blueprints.Remove(bp));var z=new Zone(SumpholdCompositionTests.Id);z.GenReservedCells.Add((79,24));
            Assert.IsFalse(new SumpholdCompositionBuilder(64).BuildZone(z,f,new Random(1)));Assert.AreEqual(0,z.EntityCount);CollectionAssert.AreEquivalent(new[]{(79,24)},z.GenReservedCells);
        }
        [TestCase("WaterPuddle","LiquidPool")] [TestCase("PeatBank","Destructible")] [TestCase("Duckboard","Material")]
        [TestCase("BoatFrame","Physics")] [TestCase("TollRolls","Render")] [TestCase("PeatCutter","Conversation")]
        public void MalformedNativePartCannotSilentlyShipAPartialSettlement(string bp,string part)
        {
            var f=GrovelandsCompositionTests.Factory();Assert.IsTrue(f.Blueprints[bp].Parts.Remove(part));var z=new Zone(SumpholdCompositionTests.Id);
            Assert.IsFalse(new SumpholdCompositionBuilder(64).BuildZone(z,f,new Random(1)));Assert.AreEqual(0,z.EntityCount);Assert.AreEqual(0,z.GenReservedCells.Count);
        }
        [Test] public void PopulatedGraphRejectsButClearedNativeGenerationCanRetry()
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone(SumpholdCompositionTests.Id);var b=new SumpholdCompositionBuilder(64);var late=new SumpholdProfileBuilder(b);
            Assert.IsTrue(b.BuildZone(z,f,new Random(1)));Assert.IsTrue(late.BuildZone(z,f,new Random(1)));var old=z.GetAllEntities().ToArray();
            Assert.IsFalse(b.BuildZone(z,f,new Random(2)));CollectionAssert.AreEquivalent(old,z.GetAllEntities());
            foreach(var e in old)z.RemoveEntity(e);Assert.IsTrue(b.BuildZone(z,f,new Random(2)));Assert.IsTrue(late.BuildZone(z,f,new Random(2)));
            Assert.IsFalse(z.GetAllEntities().Any(old.Contains));
        }
        [Test] public void BlockedLateOwnerRejectsWholeProfileWithoutMutatingTerrain()
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone(SumpholdCompositionTests.Id);var b=new SumpholdCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));
            var p=b.Plan.Profile[0];Assert.IsTrue(z.AddEntity(f.CreateEntity("SandstoneWall"),p.X,p.Y));var before=z.GetAllEntities().ToArray();
            Assert.IsFalse(new SumpholdProfileBuilder(b).BuildZone(z,f,new Random(1)));CollectionAssert.AreEquivalent(before,z.GetAllEntities());
        }
        [TestCase("PeatBank")] [TestCase("Duckboard")]
        public void NativeWorkMaterialsReallyBreakWithoutCreatingAHiddenWaterTrap(string bp)
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone(SumpholdCompositionTests.Id);var b=new SumpholdCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));
            var owner=z.GetAllEntities().First(e=>e.BlueprintName==bp);var c=z.GetEntityCell(owner);Assert.IsTrue(DestructionSystem.IsBreakable(owner));
            Assert.IsNotNull(owner.GetPart<MaterialPart>());DestructionSystem.Damage(owner,10000,null,z);Assert.IsNull(z.GetEntityCell(owner));
            Assert.IsFalse(c.Objects.Any(e=>e.HasPart<LiquidPoolPart>()));Assert.IsTrue(c.Objects.Any(e=>e.BlueprintName==b.Plan.GroundAt(c.X,c.Y)));
        }
        [Test] public void ActualRolledStairsAreDryAndReservedBeforeServices()
        {
            int n=0;var f=GrovelandsCompositionTests.Factory();
            foreach(int seed in new[]{1,2,3,4,64,1729,729490642})
            {
                var z=new OverworldZoneManager(f,seed).GetZone(SumpholdCompositionTests.Id);
                foreach(var e in z.GetAllEntities().Where(e=>e.HasPart<StairsDownPart>()))
                {n++;var c=z.GetEntityCell(e);Assert.IsTrue(z.GenReservedCells.Contains((c.X,c.Y)));Assert.IsFalse(c.BlocksMovement());Assert.IsFalse(c.Objects.Any(o=>o.HasPart<LiquidPoolPart>()));}
            }
            Assert.Greater(n,0);
        }
        [Test] public void BaseAndArrivalReportSuccessAndReferenceIdentityRejection()
        {
            Diag.ResetAll();var f=GrovelandsCompositionTests.Factory();var z=new Zone(SumpholdCompositionTests.Id);var b=new SumpholdCompositionBuilder(64);
            Assert.IsTrue(b.BuildZone(z,f,new Random(1)));var arrivals=new SumpholdArrivalReservationBuilder(b);
            Assert.IsTrue(arrivals.BuildZone(z,f,new Random(1)));Assert.IsFalse(arrivals.BuildZone(new Zone(z.ZoneID),f,new Random(1)));
            foreach(var kind in new[]{"SumpholdCompositionPlanned","SumpholdArrivalsReserved","SumpholdArrivalsRejected"})
                Assert.Greater(DiagQuery.Apply(new DiagQuery.Filter{Category="worldgen",Kind=kind,Limit=10}).Records.Count,0,kind);
            Diag.ResetAll();
        }
        private sealed class FirstChoiceRandom:Random{public override int Next(int maxValue)=>0;}
        [TestCase(false,false)] [TestCase(false,true)] [TestCase(true,false)] [TestCase(true,true)]
        public void RealHouseDramaRespectsOptInReservedInteriorAndWetFallback(bool interior,bool respect)
        {
            HouseDramaLoader.Reset();HouseDramaRuntime.Reset();
            try
            {
                const string dramaId="SumpholdReservationProbe";
                HouseDramaLoader.Register(new HouseDramaData{ID=dramaId,NpcRoles=new List<NpcRoleData>{
                    new NpcRoleData{Id="one",Role="RisingInheritor",Alive=true},
                    new NpcRoleData{Id="two",Role="SilencedHelper",Alive=true}}});
                var f=GrovelandsCompositionTests.Factory();var z=new Zone(SumpholdCompositionTests.Id);
                for(int y=0;y<25;y++)for(int x=0;x<80;x++)
                    if(y!=5||x<5||x>7)z.AddEntity(f.CreateEntity("SandstoneWall"),x,y);
                z.AddEntity(f.CreateEntity(interior?"StoneFloor":"Floor"),5,5);
                z.AddEntity(f.CreateEntity("WaterPuddle"),6,5);
                z.AddEntity(f.CreateEntity(interior?"StoneFloor":"Floor"),7,5);
                z.GenReservedCells.Add((5,5));z.GenReservedCells.Add((6,5));
                var builder=new HouseDramaZoneBuilder(dramaId);
                var option=typeof(HouseDramaZoneBuilder).GetProperty("RespectReservations");
                Assert.NotNull(option,"Scoped opt-in must not change other villages by default.");Assert.AreEqual(false,option.GetValue(builder));option.SetValue(builder,respect);
                Assert.IsTrue(builder.BuildZone(z,f,new FirstChoiceRandom()));
                var actors=z.GetAllEntities().Where(e=>e.HasPart<HouseDramaPart>()).ToArray();
                Assert.AreEqual(respect?1:2,actors.Length);
                if(respect)
                {Assert.AreEqual((7,5),z.GetEntityPosition(actors[0]));Assert.IsFalse(z.GetCell(5,5).Objects.Any(e=>e.HasPart<BrainPart>()));Assert.IsFalse(z.GetCell(6,5).Objects.Any(e=>e.HasPart<BrainPart>()));}
                else Assert.IsTrue(z.GetCell(5,5).Objects.Any(e=>e.HasPart<HouseDramaPart>()),"Unscoped behavior is the positive legacy countercontrol.");
            }
            finally{HouseDramaLoader.Reset();HouseDramaRuntime.Reset();}
        }
        [Test] public void ActualNativeWaterContactCoatsThePlayerWhileDryBoardsDoNot()
        {
            LiquidRegistry.InitializeFromJsonSources(UnityEngine.Resources.LoadAll<UnityEngine.TextAsset>("Content/Data/LiquidDefinitions").Select(a=>a.text));
            try
            {
                var f=GrovelandsCompositionTests.Factory();var z=new Zone(SumpholdCompositionTests.Id);var b=new SumpholdCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));
                foreach(var bp in new[]{"WaterPuddle","Duckboard"})
                {
                    var target=z.GetAllEntities().Where(e=>e.BlueprintName==bp).Select(e=>z.GetEntityCell(e)).First(c=>
                        z.InBounds(c.X-1,c.Y)&&!b.Plan.IsWet(c.X-1,c.Y)&&!z.GetCell(c.X-1,c.Y).BlocksMovement());
                    var player=new Entity{BlueprintName="Player"};player.SetTag("Creature");player.SetTag("Player");player.AddPart(new PhysicsPart{Solid=false});player.AddPart(new StatusEffectsPart());
                    player.Statistics["Strength"]=new Stat{Name="Strength",BaseValue=16};player.Statistics["Toughness"]=new Stat{Name="Toughness",BaseValue=14};
                    z.AddEntity(player,target.X-1,target.Y);Assert.IsTrue(MovementSystem.TryMove(player,z,1,0));
                    Assert.AreEqual(bp=="WaterPuddle",player.HasEffect<WetEffect>());Assert.AreEqual(bp=="WaterPuddle",player.HasEffect<LiquidCoveredEffect>());
                    Assert.IsFalse(target.Objects.Any(e=>e.HasPart<TileStateSourcePart>()),"No fictional renewal source.");
                    var state=z.TileState.Get(target.X,target.Y);
                    Assert.AreEqual(bp=="WaterPuddle",state!=null&&state.Coatings.Any(c=>c.Id=="water"&&c.Turns==ZoneTileState.Permanent),"Native Zone.ProjectPool mirrors only the live owner.");
                    if(bp=="WaterPuddle")
                    {
                        var pool=target.Objects.Single(e=>e.BlueprintName=="WaterPuddle");z.RemoveEntity(pool);
                        var after=z.TileState.Get(target.X,target.Y);Assert.IsFalse(after!=null&&after.Coatings.Any(c=>c.Id=="water"));
                        Assert.IsTrue(target.Objects.Any(e=>e.BlueprintName==b.Plan.GroundAt(target.X,target.Y)));
                    }
                    z.RemoveEntity(player);
                }
            }
            finally{LiquidRegistry.ResetForTests();}
        }
        [Test] public void LoadedHouseDramaPipelineOptInIsScopedToSumpholdNotOrdinaryVillages()
        {
            HouseDramaLoader.Reset();HouseDramaRuntime.Reset();
            try
            {
                var drama=new HouseDramaData{ID="SumpholdPipelineProbe",NpcRoles=new List<NpcRoleData>{new NpcRoleData{Id="one",Role="RisingInheritor",Alive=true}}};
                HouseDramaLoader.Register(drama);HouseDramaRuntime.RegisterDrama(drama);HouseDramaRuntime.ActivateDrama(drama.ID);
                var manager=new OverworldZoneManager(GrovelandsCompositionTests.Factory(),64);
                var method=typeof(OverworldZoneManager).GetMethod("GetPipelineForZone",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
                foreach(var id in new[]{SumpholdCompositionTests.Id,"Overworld.6.6.0","Overworld.5.17.0"})
                {
                    var pipeline=(ZoneGenerationPipeline)method.Invoke(manager,new object[]{id});var pop=pipeline.Builders.OfType<HouseDramaZoneBuilder>().Single();
                    Assert.AreEqual(id!="Overworld.5.17.0",typeof(HouseDramaZoneBuilder).GetProperty("RespectReservations").GetValue(pop));
                }
            }
            finally{HouseDramaLoader.Reset();HouseDramaRuntime.Reset();}
        }
    }
}
