using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
namespace CavesOfOoo.Tests { public class TallyCompositionAdversarialTests {
[SetUp] public void Setup()=>CinderholdCompositionTests.LoadLoot();
[TearDown] public void Teardown()=>LootTableRegistry.ResetForTests();
        [TestCase("RoadStone")] [TestCase("Crate")]
        [TestCase("Floor")] [TestCase("StoneFloor")] [TestCase("TentWall")]
        [TestCase("Chest")] [TestCase("Campfire")]
        public void MissingLateOrEarlyDependencyRejectsBeforeMutation(string bp)
        {
            var f=GrovelandsCompositionTests.Factory();Assert.IsTrue(f.Blueprints.Remove(bp));var z=new Zone(TallyCompositionPlan.ZoneID);z.GenReservedCells.Add((79,24));
            Assert.IsFalse(new TallyCompositionBuilder(64).BuildZone(z,f,new Random(1)));Assert.AreEqual(0,z.EntityCount);CollectionAssert.AreEquivalent(new[]{(79,24)},z.GenReservedCells);
        }
        [TestCase("TentWall","Thermal")]
        [TestCase("Chest","Container")] [TestCase("Campfire","Campfire")]
        public void FailSoftMissingNativeFunctionCannotCreateVisualOnlyService(string bp,string part)
        {
            var f=GrovelandsCompositionTests.Factory();Assert.IsTrue(f.Blueprints[bp].Parts.Remove(part));var z=new Zone(TallyCompositionPlan.ZoneID);
            Assert.IsFalse(new TallyCompositionBuilder(64).BuildZone(z,f,new Random(1)));Assert.AreEqual(0,z.EntityCount);Assert.AreEqual(0,z.GenReservedCells.Count);
        }
        [Test] public void ForeignLateGraphAndReplayedSupplyCannotDuplicateLoot()
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone(TallyCompositionPlan.ZoneID);var b=new TallyCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));var late=new TallyProfileBuilder(b);
            Assert.IsFalse(late.BuildZone(new Zone(z.ZoneID),f,new Random(1)));Assert.IsTrue(late.BuildZone(z,f,new Random(1)));
            var chest=z.GetAllEntities().Single(e=>e.BlueprintName=="Chest");var items=chest.GetPart<ContainerPart>().Contents.ToArray();z.RemoveEntity(chest);
            Assert.IsFalse(late.BuildZone(z,f,new Random(1)));Assert.IsFalse(z.GetAllEntities().Any(e=>e.BlueprintName=="Chest"));CollectionAssert.AreEquivalent(items,chest.GetPart<ContainerPart>().Contents);
        }
        [Test] public void CaveFilterReadsLiveArrivalNeighborhoodAndGraphIdentity()
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone(TallyCompositionPlan.ZoneID);var b=new TallyCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));
            Cell c=null;for(int y=1;y<24&&c==null;y++)for(int x=1;x<61&&c==null;x++)if(b.CanPlaceCaveEntrance(z,z.GetCell(x,y)))c=z.GetCell(x,y);
            Assert.NotNull(c);Assert.IsFalse(b.CanPlaceCaveEntrance(z,new Zone(z.ZoneID).GetCell(c.X,c.Y)));
            var pool=f.CreateEntity("WaterPuddle");z.AddEntity(pool,c.X+1,c.Y);Assert.IsFalse(b.CanPlaceCaveEntrance(z,c));z.RemoveEntity(pool);Assert.IsTrue(b.CanPlaceCaveEntrance(z,c));
            var wall=f.CreateEntity("TentWall");z.AddEntity(wall,c.X+1,c.Y);Assert.IsFalse(b.CanPlaceCaveEntrance(z,c));z.RemoveEntity(wall);Assert.IsTrue(b.CanPlaceCaveEntrance(z,c));
        }

[Test] public void ThirtyFourBaseLayoutsHaveConnectedNativeDoors()
{
 var f=GrovelandsCompositionTests.Factory();foreach(int seed in Enumerable.Range(0,32).Concat(new[]{int.MinValue,int.MaxValue}))
 {var z=new Zone(TallyCompositionPlan.ZoneID);var b=new TallyCompositionBuilder(seed);Assert.IsTrue(b.BuildZone(z,f,new Random(1)),"seed "+seed);var seen=LastCounterCompositionTests.FloodAllCells(z);Assert.IsTrue(LastCounterCompositionTests.AllOpenCellsReached(z,seen),"seed "+seed);}
}
[TestCase("Merchant","ExchangeHall")][TestCase("Quartermaster","RentalFrontage")][TestCase("Innkeeper","RestCourt")]
public void ServicesHaveReservedRolesButUnreservedNativePlacementCells(string bp,string role)
{
 var p=TallyCompositionPlan.Create(TallyCompositionPlan.ZoneID,64);Assert.IsTrue(p.TryGetServiceCell(bp,out int x,out int y));var r=p.Rooms.Single(v=>v.Role==role);Assert.IsTrue(x>r.X&&x<r.X+r.Width-1&&y>r.Y&&y<r.Y+r.Height-1);Assert.IsTrue(p.IsInterior(x,y));Assert.IsFalse(p.IsReserved(x,y));Assert.IsFalse(p.IsApproach(x,y));Assert.IsNull(p.ObjectAt(x,y));Assert.IsFalse(p.TryGetServiceCell("ConcordFactor",out _,out _));
}
[TestCase(false)][TestCase(true)]
public void ActualExchangeQuartermasterRentsNativeLoanerOnlyWithEnoughInk(bool funded)
{
 FactionManager.Initialize();try{
 var z=new OverworldZoneManager(GrovelandsCompositionTests.Factory(),64).GetZone(TallyCompositionPlan.ZoneID);var owner=z.GetAllEntities().Single(e=>e.BlueprintName=="Quartermaster");var stock=owner.GetPart<InventoryPart>();var item=stock.Objects.Single(e=>e.BlueprintName=="LoanerDagger");var buyer=CinderholdCompositionTests.Player();int cost=RentalSystem.GetRentalCost(item,buyer,owner);Assert.Greater(cost,0);RentalSystem.SetInk(buyer,funded?cost:0);
 Assert.AreEqual(funded,RentalSystem.TryRent(buyer,owner,item));Assert.AreEqual(funded,RentalSystem.IsRented(item));Assert.AreEqual(funded,buyer.GetPart<InventoryPart>().Objects.Contains(item));Assert.AreEqual(!funded,stock.Objects.Contains(item));Assert.AreEqual(0,RentalSystem.GetInk(buyer));
 }finally{FactionManager.Reset();}
}
}}
