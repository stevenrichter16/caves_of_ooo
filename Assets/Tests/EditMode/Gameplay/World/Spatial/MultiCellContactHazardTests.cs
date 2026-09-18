using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    public class MultiCellContactHazardTests
    {
        private static Entity Body(string shape="0,0;1,0;0,1;1,1",bool solid=false)
        {
            var e=MultiCellSpatialTests.Body(shape,solid);e.Tags["Creature"]="";
            e.Statistics["Hitpoints"]=new Stat {Owner=e,Name="Hitpoints",BaseValue=100,Min=0,Max=100};
            return e;
        }
        private sealed class DoseCounter : IObjectGasBehaviorPart
        {
            public int Count; public System.Action<Entity> OnDose;
            public override bool ApplyGas(Entity target,Zone zone)
            {if(target.HasTag("Creature")) {Count++;OnDose?.Invoke(target);return true;}return false;}
        }
        private static DoseCounter Gas(Zone zone,int x,int y,string family="poison",int level=1,bool stable=true)
        {
            var e=new Entity {ID=System.Guid.NewGuid().ToString()};e.Tags["Gas"]="";
            // These probes exercise contact/exposure. Unstable neighboring clouds
            // can legitimately merge and change their strength before dispatch.
            e.AddPart(new GasPoolPart {GasType=family,Level=level,Density=100,Stable=stable});
            var behavior=new DoseCounter();e.AddPart(behavior);Assert.IsTrue(zone.AddEntity(e,x,y));return behavior;
        }
        [Test] public void PathToContactStopsAtBodyEdgeWithoutEnteringTarget()
        {
            var z=new Zone();var actor=Body(solid:true);var target=Body("0,0;1,0;2,0;3,0",true);
            z.AddEntity(actor,18,10);z.AddEntity(target,10,10);
            var path=FindPath.ToContact(z,actor,target);Assert.IsTrue(path.Usable);
            int x=18,y=10;
            foreach(var d in path.Steps)
            {x+=d.dx;y+=d.dy;Assert.IsTrue(z.CanPlaceFootprint(actor,x,y));}
            Assert.IsTrue(z.MoveEntity(actor,x,y));Assert.AreEqual(1,SpatialQuery.Distance(z,actor,target));
            Assert.Greater(x,13,"must stop on reachable east edge");
        }
        [Test] public void ContactPathDoesNotChooseAnUnreachableNearEdge()
        {
            var z=new Zone();var actor=Body(solid:true);var target=Body(solid:true);
            z.AddEntity(actor,8,12);z.AddEntity(target,14,10);
            for(int y=8;y<=12;y++) {var wall=MultiCellSpatialTests.Body(null);wall.Tags["Solid"]="";z.AddEntity(wall,12,y);}
            var path=FindPath.ToContact(z,actor,target);Assert.IsTrue(path.Usable);
            foreach(var step in path.Steps) Assert.IsTrue(MovementSystem.TryMove(actor,z,step.dx,step.dy));
            Assert.AreEqual(1,SpatialQuery.Distance(z,actor,target));
        }
        [Test] public void GasDosesRemoteBodySurfaceAndSeparateTicksRemainSeparate()
        {
            var z=new Zone();var body=Body();z.AddEntity(body,10,10);var gas=Gas(z,11,11);
            GasSystem.OnTickEnd(z);Assert.AreEqual(1,gas.Count);
            GasSystem.OnTickEnd(z);Assert.AreEqual(2,gas.Count);
        }
        [Test] public void GasOverlapUsesOneDosePerFamilyForWholeBody()
        {
            var z=new Zone();var body=Body();z.AddEntity(body,10,10);
            var a=Gas(z,10,10);var b=Gas(z,11,10);var other=Gas(z,11,11,"confusion");
            GasSystem.OnTickEnd(z);Assert.AreEqual(1,a.Count+b.Count);Assert.AreEqual(1,other.Count);
        }
        [Test] public void GasOverlapChoosesStrongestFamilyDoseIndependentOfAnchor()
        {
            var z=new Zone();var body=Body();z.AddEntity(body,10,10);
            var weak=Gas(z,10,10,level:1);var strong=Gas(z,11,11,level:3);
            GasSystem.OnTickEnd(z);Assert.AreEqual(0,weak.Count);Assert.AreEqual(1,strong.Count);
            Assert.AreEqual(1,weak.BaseGas.Level);Assert.AreEqual(3,strong.BaseGas.Level);
            Assert.AreEqual(100,weak.BaseGas.Density);Assert.AreEqual(100,strong.BaseGas.Density);
            Assert.AreEqual((10,10),z.GetEntityPosition(weak.ParentEntity));
            Assert.AreEqual((11,11),z.GetEntityPosition(strong.ParentEntity));
        }
        [TestCase(false)] [TestCase(true)]
        public void GasFamilySelectionFollowsPostDispersalStrength(bool stable)
        {
            var z=new Zone();var body=Body();Assert.IsTrue(z.AddEntity(body,10,10));
            var weak=Gas(z,10,10,level:1,stable:stable);
            var strong=Gas(z,11,11,level:3,stable:stable);
            Assert.AreEqual(1,weak.BaseGas.Level);Assert.AreEqual(3,strong.BaseGas.Level);
            Assert.AreEqual(100,weak.BaseGas.Density);Assert.AreEqual(100,strong.BaseGas.Density);
            // Weak: decay1, no spread. Strong: decay1, one NW spread of30.
            // Its receiver becomes level3/density129 versus the donor's69.
            // Adapt the forced sequence to the tag snapshot's order; the
            // assertion does not depend on HashSet enumeration ordering.
            var first=z.GetEntitiesWithTag("Gas")[0];
            var rng=ReferenceEquals(first,weak.ParentEntity)
                ?new GasMergeRng(1,99,1,0,1,7,30):new GasMergeRng(1,0,1,7,30,1,99);
            var field=typeof(GasSystem).GetField("_rng",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(field);var previous=(System.Random)field.GetValue(null);
            try {GasSystem.SetRngForTests(rng);GasSystem.OnTickEnd(z);}
            finally {GasSystem.SetRngForTests(previous);}
            Assert.AreEqual(stable?0:7,rng.Calls,"Stable contact probes must not consume dispersal RNG.");
            Assert.AreEqual(stable?1:3,weak.BaseGas.Level);Assert.AreEqual(3,strong.BaseGas.Level);
            Assert.AreEqual(stable?100:129,weak.BaseGas.Density);Assert.AreEqual(stable?100:69,strong.BaseGas.Density);
            Assert.AreEqual(stable?0:1,weak.Count);Assert.AreEqual(stable?1:0,strong.Count);
            Assert.AreEqual(1,weak.Count+strong.Count,"One family still doses the whole body only once.");
            Assert.AreEqual(2,z.GetEntitiesWithTag("Gas").Count);
            Assert.AreEqual((10,10),z.GetEntityPosition(weak.ParentEntity));
            Assert.AreEqual((11,11),z.GetEntityPosition(strong.ParentEntity));
        }
        private sealed class GasMergeRng : System.Random
        {
            private readonly int[] values;
            public int Calls {get;private set;}
            public GasMergeRng(params int[] values) {this.values=values;}
            public override int Next(int maxValue)=>Next(0,maxValue);
            public override int Next(int minValue,int maxValue)
            {
                Assert.Less(Calls,values.Length,"Unexpected dispersal RNG call.");
                int value=values[Calls++];Assert.That(value,Is.InRange(minValue,maxValue-1));return value;
            }
        }
        [Test] public void SingleBodyGasCounterAndHoleRemainUnchanged()
        {
            var z=new Zone();var body=Body("0,0;2,0");z.AddEntity(body,10,10);
            var gap=Gas(z,11,10);GasSystem.OnTickEnd(z);Assert.AreEqual(0,gap.Count);
            z.RemoveEntity(body);var single=Body(null);z.AddEntity(single,11,10);
            GasSystem.OnTickEnd(z);Assert.AreEqual(1,gap.Count);
        }
        [Test] public void WalkingBodyIntoMultipleGasCellsDosesOneFamily()
        {
            var z=new Zone();var body=Body();z.AddEntity(body,10,10);
            var a=Gas(z,12,10);var b=Gas(z,12,11);
            Assert.IsTrue(MovementSystem.TryMove(body,z,1,0));Assert.AreEqual(1,a.Count+b.Count);
        }
        [Test] public void GasBatchSkipsBehaviorRemovedByEarlierDoseWithoutThrowing()
        {
            var z=new Zone();var body=Body();z.AddEntity(body,10,10);
            var a=Gas(z,10,10);var b=Gas(z,11,11,"confusion");
            a.OnDose=_=>b.ParentEntity.RemovePart(b);
            Assert.DoesNotThrow(()=>GasSystem.OnTickEnd(z));
            Assert.AreEqual(1,a.Count);Assert.AreEqual(0,b.Count);
        }
        [Test] public void GasBatchDoesNotDoseBodyRelocatedOutOfLaterPool()
        {
            var z=new Zone();var body=Body();z.AddEntity(body,10,10);
            var a=Gas(z,10,10);var b=Gas(z,11,11,"confusion");
            a.OnDose=_=>z.MoveEntity(body,20,10);
            Assert.DoesNotThrow(()=>GasSystem.OnTickEnd(z));
            Assert.AreEqual(1,a.Count);Assert.AreEqual(0,b.Count);
        }
        [Test] public void TerrainSourceCoversFullBodyButLeavesHoleAndStopsAfterRemoval()
        {
            var z=new Zone();var source=MultiCellSpatialTests.Body("0,0;2,0",false);
            source.AddPart(new TileStateSourcePart {Heat=7});z.AddEntity(source,10,10);
            ZoneTileStateSystem.SeedTerrainSources(z);
            Assert.AreEqual(ZoneTileState.MaxEnergy,z.TileState.Get(12,10).Heat);Assert.IsNull(z.TileState.Get(11,10));
            z.RemoveEntity(source);z.TileState.Clear(10,10);z.TileState.Clear(12,10);ZoneTileStateSystem.SeedTerrainSources(z);
            Assert.IsNull(z.TileState.Get(12,10));
        }
        [Test] public void TileReactionHitsWholeCreatureOncePerResolutionAndNextResolutionHitsAgain()
        {
            TileReactionSystem.Initialize("{\"Reactions\":[{\"ID\":\"body_test\",\"InputEnergy\":\"charge\",\"MinEnergy\":1,\"OccupantDamage\":5}]}");
            try
            {
                var z=new Zone();var body=Body();z.AddEntity(body,10,10);
                z.TileState.AddCharge(10,10,3);z.TileState.AddCharge(11,11,3);
                TileReactionSystem.ResolveZone(z);Assert.AreEqual(95,body.GetStatValue("Hitpoints"));
                TileReactionSystem.ResolveZone(z);Assert.AreEqual(90,body.GetStatValue("Hitpoints"));
            }
            finally {TileReactionSystem.ResetForTests();}
        }
    }
}
