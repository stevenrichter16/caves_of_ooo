using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public class LastCounterCompositionTests
    {
        public const string Id="Overworld.18.18.0";
        [SetUp] public void LoadLoot()=>CinderholdCompositionTests.LoadLoot();
        [TearDown] public void ClearLoot()=>LootTableRegistry.ResetForTests();
        [Test] public void ExactFrontierIsDeterministicButNotAnAuthoredRoadOrRiver()
        {
            Assert.IsTrue(LastCounterCompositionPlan.IsSupportedZone(Id));
            Assert.AreEqual(LastCounterCompositionPlan.Create(Id,64).Signature(),LastCounterCompositionPlan.Create(Id,64).Signature());
            Assert.AreNotEqual(LastCounterCompositionPlan.Create(Id,64).Signature(),LastCounterCompositionPlan.Create(Id,1729).Signature());
            Assert.AreEqual(BiomeType.Beating,WorldMapAuthoring.BiomeAt(18,18));Assert.AreEqual(3,WorldMapAuthoring.TierAt(18,18));
            Assert.IsFalse(WorldMapAuthoring.IsRoad(18,18));Assert.IsFalse(WorldMapAuthoring.IsRiver(18,18));
        }
        [TestCase(null)] [TestCase("")] [TestCase("Overworld.18.18.1")] [TestCase("Overworld.018.18.0")]
        [TestCase("Overworld.19.18.0")] [TestCase("Overworld.19.19.0")] [TestCase("Overworld.5.17.0")]
        public void NeighboringAbandonedPostsNeverAcquireTheOccupiedProfile(string id)
        {Assert.IsFalse(LastCounterCompositionPlan.IsSupportedZone(id));Assert.Throws<ArgumentException>(()=>LastCounterCompositionPlan.Create(id,64));}
        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void WorkingWestAndQuietEastHaveDryConnectedFrontages(int seed)
        {
            var z=new Zone(Id);var b=new LastCounterCompositionBuilder(seed);Assert.AreEqual(1000,b.Priority);Assert.IsTrue(b.BuildZone(z,GrovelandsCompositionTests.Factory(),new Random(1)));var p=b.Plan;
            CollectionAssert.IsSubsetOf(new[]{"SupplyPost","RestShelter","WorkersHouse"},p.Rooms.Select(r=>r.Role));
            Assert.GreaterOrEqual(p.Rooms.Select(r=>r.Width*r.Height).Distinct().Count(),3);
            Assert.IsTrue(p.Rooms.All(r=>r.X+r.Width<58));
            var seen=FloodAllCells(z);Assert.IsTrue(AllOpenCellsReached(z,seen));
            foreach(var r in p.Rooms)Assert.IsTrue(seen[r.DoorX,r.DoorY],r.Role);
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)
            {
                Assert.AreEqual(1,z.GetCell(x,y).Objects.Count(e=>e.BlueprintName==p.GroundAt(x,y)));
                Assert.AreNotEqual("RoadStone",p.GroundAt(x,y));
                if(p.IsApproach(x,y)){Assert.IsFalse(z.GetCell(x,y).BlocksMovement());Assert.IsTrue(z.GenReservedCells.Contains((x,y)));}
                if(x>=62){Assert.IsFalse(p.IsInterior(x,y));Assert.IsTrue(p.IsReserved(x,y),"Eastern quiet field excludes initial population.");}
            }
            Assert.IsFalse(z.GetAllEntities().Any(e=>e.HasPart<LiquidPoolPart>()));Assert.IsFalse(z.GetCell(40,12).BlocksMovement());
        }
        [Test] public void NativeProfileHasOneRealEnvoyStockedChestFireAndExaminableSign()
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone(Id);var b=new LastCounterCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));
            var late=new LastCounterProfileBuilder(b);Assert.AreEqual(3860,late.Priority);Assert.IsTrue(late.BuildZone(z,f,new Random(1)));
            foreach(string bp in new[]{"SaccharineEnvoy","LastCounterSign","Campfire","Chest"})Assert.AreEqual(1,z.GetAllEntities().Count(e=>e.BlueprintName==bp),bp);
            var envoy=z.GetAllEntities().Single(e=>e.BlueprintName=="SaccharineEnvoy");Assert.AreEqual("SaccharineEnvoy_1",envoy.GetPart<ConversationPart>().ConversationID);Assert.AreEqual("EnvoyStock",envoy.GetPart<TraderPart>().StockTable);
            var sign=z.GetAllEntities().Single(e=>e.BlueprintName=="LastCounterSign");Assert.IsTrue(sign.GetPart<PhysicsPart>().Solid);Assert.NotNull(sign.GetPart<ExaminablePart>());Assert.IsNull(sign.GetPart<DestructiblePart>());
            var chest=z.GetAllEntities().Single(e=>e.BlueprintName=="Chest");var items=chest.GetPart<ContainerPart>().Contents;
            Assert.IsTrue(items.Any(e=>e.BlueprintName=="HealingTonic"));Assert.IsTrue(items.Any(e=>e.BlueprintName=="DriedMeat"));
            Assert.NotNull(z.GetAllEntities().Single(e=>e.BlueprintName=="Campfire").GetPart<CampfirePart>());
            var before=z.GetAllEntities().ToArray();Assert.IsFalse(late.BuildZone(z,f,new Random(2)));CollectionAssert.AreEquivalent(before,z.GetAllEntities());
        }
        [TestCase(1)] [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void FinalVillageRetainsNativeServicesButLeavesEasternApproachEmpty(int seed)
        {
            var z=new OverworldZoneManager(GrovelandsCompositionTests.Factory(),seed).GetZone(Id);
            foreach(string bp in new[]{"SaccharineEnvoy","LastCounterSign","Merchant","Quartermaster","Elder"})Assert.AreEqual(1,z.GetAllEntities().Count(e=>e.BlueprintName==bp),bp);
            Assert.IsFalse(z.GetAllEntities().Any(e=>e.HasPart<LiquidPoolPart>()));Assert.IsTrue(z.GetCell(40,12).Objects.Any(e=>e.BlueprintName=="Well"));
            foreach(var e in z.GetAllEntities().Where(e=>e.HasPart<BrainPart>()))Assert.Less(z.GetEntityCell(e).X,62);
            var places=z.GetAllEntities().Where(e=>new[]{"SaccharineEnvoy","LastCounterSign","Campfire","Chest","Well","Merchant"}.Contains(e.BlueprintName)).Select(e=>(e.BlueprintName,Position:z.GetEntityPosition(e))).ToArray();
            foreach(var e in z.GetAllEntities().Where(e=>e.HasPart<BrainPart>()||e.HasTag("Creature")).ToArray())z.RemoveEntity(e);
            var seen=FloodAllCells(z);Assert.IsTrue(AllOpenCellsReached(z,seen));
            foreach(var owner in places)
            {
                var c=owner.Position;
                Assert.IsTrue(CinderholdCompositionTests.Neighbors(c.x,c.y).Any(n=>z.InBounds(n.x,n.y)&&seen[n.x,n.y]),
                    owner.BlueprintName+" at "+c+" neighbors: "+string.Join("; ",CinderholdCompositionTests.Neighbors(c.x,c.y).Select(n=>n+":"+(z.InBounds(n.x,n.y)?string.Join(",",z.GetCell(n.x,n.y).Objects.Select(e=>e.BlueprintName)):"outside"))));
            }
        }
        [Test] public void SeedChangesSupplyRestAndHouseholdRelationshipsNotOnlyCoordinateJitter()
        {
            var property=typeof(LastCounterCompositionPlan).GetProperty("FormationName");Assert.NotNull(property,"Named formations are part of the semantic preview contract.");
            var names=new System.Collections.Generic.HashSet<string>();var orders=new System.Collections.Generic.HashSet<string>();
            foreach(int seed in Enumerable.Range(0,32).Concat(new[]{int.MinValue,int.MaxValue}))
            {
                var p=LastCounterCompositionPlan.Create(Id,seed);string name=(string)property.GetValue(p);Assert.IsFalse(string.IsNullOrEmpty(name));names.Add(name);
                orders.Add(string.Join("/",p.Rooms.OrderBy(r=>r.Y+r.Height/2).ThenBy(r=>r.X).Select(r=>r.Role)));
            }
            Assert.AreEqual(3,names.Count);Assert.GreaterOrEqual(orders.Count,3,"Three room-role orderings distinguish receiving above, below and stepped arrangements.");
        }
        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void OutdoorSupplyForecourtHasUnequalOpenReturnsAndClusteredRealGoods(int seed)
        {
            var p=LastCounterCompositionPlan.Create(Id,seed);int crates=0;var walls=new System.Collections.Generic.HashSet<(int x,int y)>();
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)
            {
                bool inRoom=p.Rooms.Any(r=>x>=r.X&&x<r.X+r.Width&&y>=r.Y&&y<r.Y+r.Height);
                if(!inRoom&&p.ObjectAt(x,y)=="SandstoneWall"){walls.Add((x,y));Assert.Less(x,58);Assert.IsFalse(p.IsApproach(x,y));}
                if(!inRoom&&p.ObjectAt(x,y)=="Crate"){crates++;Assert.Less(x,58);Assert.IsFalse(p.IsApproach(x,y));}
            }
            Assert.That(walls.Count,Is.InRange(7,24));Assert.That(crates,Is.InRange(2,5));
            var sizes=new System.Collections.Generic.List<int>();while(walls.Count>0)
            {
                int size=0;var q=new System.Collections.Generic.Queue<(int x,int y)>();var first=walls.First();walls.Remove(first);q.Enqueue(first);
                while(q.Count>0){var c=q.Dequeue();size++;foreach(var n in CinderholdCompositionTests.Neighbors(c.x,c.y))if(walls.Remove(n))q.Enqueue(n);}sizes.Add(size);
            }
            Assert.AreEqual(2,sizes.Count);Assert.AreEqual(2,sizes.Distinct().Count(),"Asymmetric open returns frame a yard rather than clone another room.");
        }
        [TestCase(64)] [TestCase(1729)] [TestCase(729490642)]
        public void RestMatsGoodsAndSparseWesternShouldersCommunicateDifferentNativeUses(int seed)
        {
            var p=LastCounterCompositionPlan.Create(Id,seed);var rest=p.Rooms.Single(r=>r.Role=="RestShelter");var supply=p.Rooms.Single(r=>r.Role=="SupplyPost");
            Func<LastCounterCompositionPlan.Room,string,int> count=(r,bp)=>Enumerable.Range(r.X,r.Width).Sum(x=>Enumerable.Range(r.Y,r.Height).Count(y=>p.ObjectAt(x,y)==bp));
            Assert.That(count(rest,"Bed"),Is.InRange(2,3));Assert.GreaterOrEqual(count(rest,"Chair"),2);Assert.GreaterOrEqual(count(supply,"Crate"),4);Assert.AreEqual(0,count(supply,"Bed"));
            int brush=0,rubble=0;for(int y=0;y<25;y++)for(int x=0;x<80;x++)
            {
                var bp=p.ObjectAt(x,y);if(bp!="DryBrush"&&bp!="Rubble")continue;if(bp=="DryBrush")brush++;else rubble++;
                Assert.Less(x,58);Assert.IsFalse(p.IsInterior(x,y));Assert.IsFalse(p.IsApproach(x,y));Assert.IsTrue(p.IsReserved(x,y));
            }
            Assert.That(brush,Is.InRange(10,24));Assert.That(rubble,Is.InRange(4,10));
            for(int y=0;y<25;y++)for(int x=62;x<80;x++)Assert.IsNull(p.ObjectAt(x,y),"The eastern silence must survive the richer occupied post.");
        }
        // Native movement can occupy boundary cells. Unlike the wilderness
        // repair helper, this player-flow flood includes every one of80x25 cells
        // and requires cardinal travel from the actual western arrival.
        public static bool[,] FloodAllCells(Zone z)
        {
            var seen=new bool[80,25];var q=new System.Collections.Generic.Queue<(int x,int y)>();
            if(!z.GetCell(0,12).BlocksMovement()){q.Enqueue((0,12));seen[0,12]=true;}
            while(q.Count>0)
            {var c=q.Dequeue();foreach(var n in CinderholdCompositionTests.Neighbors(c.x,c.y))if(z.InBounds(n.x,n.y)&&!seen[n.x,n.y]&&!z.GetCell(n.x,n.y).BlocksMovement()){seen[n.x,n.y]=true;q.Enqueue(n);}}
            return seen;
        }
        public static bool AllOpenCellsReached(Zone z,bool[,] seen)
        {for(int y=0;y<25;y++)for(int x=0;x<80;x++)if(!z.GetCell(x,y).BlocksMovement()&&!seen[x,y])return false;return true;}
    }
}
