using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class GleanersCellarBuilderTests
    {
        const string Address="Overworld.11.10.1", Role="GleanersCellar.Role";
        DensityLootTestScope scope;
        EntityFactory Factory=>scope.Factory;
        [SetUp] public void Setup(){CellarCreationProbe.Remembered=null;scope=new DensityLootTestScope();}
        [TearDown] public void Cleanup(){CellarCreationProbe.Remembered=null;scope.Dispose();}
        static Type BuilderType
        {get{var type=typeof(Zone).Assembly.GetType("CavesOfOoo.Core.GleanersCellarBuilder");Assert.NotNull(type,"The default glade needs its optional native supply cellar.");return type;}}
        bool Build(Zone z,int seed=64,Random rng=null)
            =>((IZoneBuilder)Activator.CreateInstance(BuilderType,seed)).BuildZone(z,Factory,rng??new Random(8));
        Zone Built(int seed=64){var z=new Zone(Address);Assert.True(Build(z,seed));return z;}
        static Entity Owner(Zone z,string role)=>z.GetReadOnlyEntities().Single(e=>e.GetProperty(Role)==role);
        static IEnumerable<Entity> Graph(Zone z)
        {
            var queue=new Queue<Entity>(z.GetReadOnlyEntities());var seen=new HashSet<Entity>();
            while(queue.Count>0){var e=queue.Dequeue();if(!seen.Add(e))continue;yield return e;
                foreach(var c in e.GetPart<ContainerPart>()?.Contents??Enumerable.Empty<Entity>())queue.Enqueue(c);
                foreach(var c in e.GetPart<InventoryPart>()?.Objects??Enumerable.Empty<Entity>())queue.Enqueue(c);
                foreach(var c in e.GetPart<InventoryPart>()?.EquippedItems.Values??Enumerable.Empty<Entity>())queue.Enqueue(c);}
        }
        [TestCase(64)][TestCase(1729)][TestCase(729490642)]
        public void FreshCellarHasSafeReturnAndFiniteRealRepairAndCapabilitySupplies(int seed)
        {
            var z=Built(seed);var up=Owner(z,"stairs");Assert.True(up.HasPart<StairsUpPart>());Assert.AreEqual((40,12),z.GetEntityPosition(up));
            Assert.False(z.GetCell(40,12).BlocksMovement());Assert.False(z.GetReadOnlyEntities().Any(e=>e.HasPart<StairsDownPart>()));
            var cache=Owner(z,"supplies");var container=cache.GetPart<ContainerPart>();Assert.NotNull(container);Assert.False(container.IsLocked);
            Assert.AreEqual(2,container.Contents.Where(e=>e.BlueprintName=="FireClay").Sum(e=>e.GetPart<StackerPart>()?.StackCount??1));
            var payload=container.Contents.Where(e=>e.BlueprintName!="FireClay").ToArray();Assert.AreEqual(1,payload.Length);
            Assert.That(payload[0].BlueprintName,Is.EqualTo("ShatteredRimeGrimoire").Or.EqualTo("Buckler"));
            if(payload[0].BlueprintName=="ShatteredRimeGrimoire")Assert.AreEqual(10,payload[0].GetPart<GrimoireChargePart>().Charges);
            foreach(var item in container.Contents){Assert.AreSame(cache,item.GetPart<PhysicsPart>().InInventory);Assert.Null(typeof(Entity).GetField("SpatialZone",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(item));Assert.True(item.GetPart<PhysicsPart>().Takeable);}
            var graph=Graph(z).ToArray();Assert.AreEqual(graph.Length,graph.Select(e=>e.ID).Distinct().Count());
            Assert.True(graph.All(e=>e.Parts.All(p=>ReferenceEquals(p.ParentEntity,e))));Assert.False(graph.Any(e=>e.HasPart<BitLockerPart>()));
            var guard=Owner(z,"guard");Assert.AreEqual("MarlbackScrabbler",guard.BlueprintName);Assert.AreEqual(15,guard.GetStatValue("Hitpoints"));
            Assert.Greater(Math.Max(Math.Abs(z.GetEntityPosition(guard).x-40),Math.Abs(z.GetEntityPosition(guard).y-12)),10);
        }
        [TestCase(64)][TestCase(1729)][TestCase(729490642)][TestCase(int.MinValue)][TestCase(int.MaxValue)]
        public void SidePassageReachesTheSameCacheAndReturnWithoutMovingBeamOrEnteringGuardCorridor(int seed)
        {
            var z=Built(seed);var guard=Owner(z,"guard");var g=z.GetEntityPosition(guard);var cache=z.GetEntityPosition(Owner(z,"supplies"));
            var reached=Reach(z,(40,12),p=>Math.Abs(p.x-g.x)<=4&&Math.Abs(p.y-g.y)<=2);
            Assert.True(reached.Any(p=>Math.Max(Math.Abs(p.x-cache.x),Math.Abs(p.y-cache.y))<=1),"A permanent walled side approach must remain around the optional beam.");
            var direct=Reach(z,(40,12),p=>p.y<10||p.y>14,guard);
            Assert.True(direct.Any(p=>p.x>=cache.x-2),"The direct guard corridor must be a real alternative after confronting or controlling its occupant.");
            Assert.True(z.GetCell(g.x,g.y-2).BlocksMovement());Assert.True(z.GetCell(g.x,g.y+2).BlocksMovement(),"Walls separate the bypass from the direct sightline.");
        }
        [TestCase(64)][TestCase(1729)]
        public void OptionalBeamHasActualReachableHaulingShoulderAndLanding(int seed)
        {
            var z=Built(seed);var beam=Owner(z,"beam");var b=z.GetEntityPosition(beam);var actor=Factory.CreateEntity("Player");
            Assert.True(Reach(z,(40,12)).Contains((b.x-1,b.y)));Assert.True(z.AddEntity(actor,b.x-1,b.y));
            Assert.AreEqual(DragVerdict.Ok,DragSystem.TryGrab(actor,beam,z));
            Assert.True(MovementSystem.TryMoveTo(actor,z,b.x-2,b.y));Assert.AreEqual((b.x-1,b.y),z.GetEntityPosition(beam));
            int side=b.y<12?-1:1;
            Assert.True(MovementSystem.TryMoveTo(actor,z,b.x-2,b.y+side));Assert.True(MovementSystem.TryMoveTo(actor,z,b.x-2,b.y+2*side));
            DragSystem.Release(actor);Assert.False(z.GetCell(b.x,b.y).BlocksMovement());Assert.False(z.GetCell(b.x-1,b.y).BlocksMovement());
            Assert.AreEqual((b.x-2,b.y+side),z.GetEntityPosition(beam));
        }
        [Test]
        public void SeedChangesApproachAndPayloadWithoutConsumingCallerRandom()
        {
            var rng=new Random(991);var z=new Zone(Address);Assert.True(Build(z,64,rng));Assert.AreEqual(new Random(991).Next(),rng.Next());
            var other=Built(1729);Assert.AreNotEqual(z.GetEntityPosition(Owner(z,"beam")),other.GetEntityPosition(Owner(other,"beam")));
            Assert.AreNotEqual(Owner(z,"supplies").GetPart<ContainerPart>().Contents.Last().BlueprintName,Owner(other,"supplies").GetPart<ContainerPart>().Contents.Last().BlueprintName);
            Assert.AreEqual(Signature(z),Signature(Built(64)));
        }
        [TestCase("Overworld.11.10.0")][TestCase("Overworld.11.10.2")][TestCase("Overworld.12.10.1")][TestCase("Overworld.011.10.1")]
        public void ForeignOrAliasAddressesAreNotPatched(string id)
        {var z=new Zone(id);Assert.False(Build(z));Assert.Zero(z.EntityCount);Assert.Zero(z.GenReservedCells.Count);}
        [Test]
        public void ExistingOrDepletedOwnersAreNeverRecreated()
        {var z=Built();var c=Owner(z,"supplies").GetPart<ContainerPart>();foreach(var e in c.Contents.ToArray())c.RemoveItem(e);var old=z.GetReadOnlyEntities().ToArray();Assert.False(Build(z));CollectionAssert.AreEquivalent(old,z.GetReadOnlyEntities());Assert.Zero(c.Contents.Count);}
        [TestCase("Floor")][TestCase("StoneWall")][TestCase("StairsUp")][TestCase("Crate")][TestCase("FireClay")][TestCase("FallenBeam")][TestCase("MarlbackScrabbler")][TestCase("ShatteredRimeGrimoire")][TestCase("Buckler")]
        public void MissingContentRefusesBeforePublishingOwnersOrReservations(string bp)
        {Factory.Blueprints.Remove(bp);var z=new Zone(Address);z.GenReservedCells.Add((7,7));Assert.False(Build(z));Assert.Zero(z.EntityCount);CollectionAssert.AreEquivalent(new[]{(7,7)},z.GenReservedCells);}
        [TestCase("late-wall-passable")][TestCase("late-stairs-blocked")][TestCase("late-beam-too-heavy")]
        [TestCase("crate-full")][TestCase("clay-not-portable")][TestCase("wall-passable")][TestCase("beam-not-solid")][TestCase("bad-stairs")][TestCase("multi-cell")][TestCase("duplicate-id")][TestCase("foreign-parent")][TestCase("creation-throws")]
        public void MalformedPhysicalOrCallbackContentIsRejectedAtomically(string mode)
        {
            var z=new Zone(Address);z.GenReservedCells.Add((7,7));
            if(mode=="crate-full")Factory.Blueprints["Crate"].Parts["Container"]["MaxItems"]="0";
            if(mode=="clay-not-portable")Factory.Blueprints["FireClay"].Parts["Physics"]["Takeable"]="false";
            if(mode=="wall-passable"){Factory.Blueprints["StoneWall"].Parts["Physics"]["Solid"]="false";Factory.Blueprints["StoneWall"].Tags.Remove("Solid");}
            if(mode=="beam-not-solid")Factory.Blueprints["FallenBeam"].Parts["Physics"]["Solid"]="false";
            if(mode=="bad-stairs")Factory.Blueprints["StairsUp"].Parts.Remove("StairsUp");
            if(mode=="multi-cell")Factory.Blueprints["Crate"].Parts["SpatialFootprint"]=new Dictionary<string,string>{{"CellsRaw","79,24"}};
            if(mode=="duplicate-id"||mode=="foreign-parent"||mode=="creation-throws")
            {Factory.RegisterPartType<CellarCreationProbe>();Factory.Blueprints["FireClay"].Parts["CellarCreationProbe"]=new Dictionary<string,string>{{"Mode",mode}};}
            if(mode.StartsWith("late-",StringComparison.Ordinal))
            {
                Factory.RegisterPartType<CellarCreationProbe>();string target=mode=="late-wall-passable"?"StoneWall":mode=="late-stairs-blocked"?"StairsUp":"FallenBeam";
                Factory.Blueprints[target].Parts["CellarCreationProbe"]=new Dictionary<string,string>{{"Mode","remember"}};
                Factory.Blueprints["FireClay"].Parts["CellarCreationProbe"]=new Dictionary<string,string>{{"Mode",mode}};
            }
            bool result=true;Assert.DoesNotThrow(()=>result=Build(z));Assert.False(result);Assert.Zero(z.EntityCount);CollectionAssert.AreEquivalent(new[]{(7,7)},z.GenReservedCells);
        }
        public sealed class CellarCreationProbe:Part
        {
            public string Mode;public static Entity Remembered;
            public override bool HandleEvent(GameEvent e)
            {
                if(e.ID!="ObjectCreated")return true;
                if(Mode=="creation-throws")throw new InvalidOperationException("cellar probe");
                if(Mode=="duplicate-id")ParentEntity.ID="same-clay-id";
                if(Mode=="foreign-parent")ParentEntity.GetPart<RenderPart>().ParentEntity=new Entity();
                if(Mode=="remember"&&Remembered==null)Remembered=ParentEntity;
                if(Mode=="late-wall-passable"){Remembered.GetPart<PhysicsPart>().Solid=false;Remembered.Tags.Remove("Solid");}
                if(Mode=="late-stairs-blocked")Remembered.GetPart<PhysicsPart>().Solid=true;
                if(Mode=="late-beam-too-heavy"){Remembered.GetPart<HandlingPart>().Weight=1000;Remembered.GetPart<PhysicsPart>().Weight=1000;}
                return true;
            }
        }
        static string Signature(Zone z)=>string.Join("|",z.GetReadOnlyEntities().Select(e=>e.BlueprintName+":"+z.GetEntityPosition(e)).OrderBy(s=>s,StringComparer.Ordinal));
        static HashSet<(int x,int y)> Reach(Zone z,(int x,int y) from,Func<(int x,int y),bool> excluded=null,Entity ignore=null)
        {
            var seen=new HashSet<(int,int)>{from};var queue=new Queue<(int x,int y)>();queue.Enqueue(from);
            while(queue.Count>0){var p=queue.Dequeue();for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)
                {if(dx==0&&dy==0)continue;var n=(x:p.x+dx,y:p.y+dy);var c=z.GetCell(n.x,n.y);if(c==null||excluded?.Invoke(n)==true||c.BlocksMovement(ignore)||!seen.Add(n))continue;queue.Enqueue(n);}}
            return seen;
        }
    }
}
