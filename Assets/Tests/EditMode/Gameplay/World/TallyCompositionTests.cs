using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public class TallyCompositionTests
 {
  [SetUp] public void Setup()=>CinderholdCompositionTests.LoadLoot();
  [TearDown] public void Teardown()=>LootTableRegistry.ResetForTests();
  [Test] public void ExactAuthoredExchangeHasThreeMeaningfulFormations()
  {
   Assert.AreEqual("Overworld.10.14.0",TallyCompositionPlan.ZoneID);Assert.AreEqual("CentralExchange",TallyCompositionPlan.ProfileID);
   Assert.AreEqual(BiomeType.Spread,WorldMapAuthoring.BiomeAt(10,14));Assert.AreEqual(1,WorldMapAuthoring.TierAt(10,14));Assert.IsTrue(WorldMapAuthoring.IsRoad(10,14));Assert.IsFalse(WorldMapAuthoring.IsRiver(10,14));
   var plans=Enumerable.Range(0,32).Select(s=>TallyCompositionPlan.Create(TallyCompositionPlan.ZoneID,s)).ToArray();
   Assert.AreEqual(3,plans.Select(p=>p.FormationName).Distinct().Count());Assert.GreaterOrEqual(plans.Select(p=>string.Join("/",p.Rooms.OrderBy(r=>r.Y).ThenBy(r=>r.X).Select(r=>r.Role))).Distinct().Count(),3);
   Assert.AreEqual(plans[0].Signature(),TallyCompositionPlan.Create(TallyCompositionPlan.ZoneID,0).Signature());Assert.AreNotEqual(plans[0].Signature(),plans[1].Signature());
  }
  [TestCase(null)][TestCase("")][TestCase("Overworld.10.14.1")][TestCase("Overworld.010.14.0")][TestCase("Overworld.11.14.0")]
  public void ScopeDoesNotBorrowNeighbors(string id){Assert.IsFalse(TallyCompositionPlan.IsSupportedZone(id));Assert.Throws<ArgumentException>(()=>TallyCompositionPlan.Create(id,1));}
  [TestCase(64)][TestCase(1729)][TestCase(729490642)][TestCase(int.MinValue)][TestCase(int.MaxValue)]
  public void ExchangeRoutesServeDistinctStorageRestAndRentalRooms(int seed)
  {
   var z=new Zone(TallyCompositionPlan.ZoneID);var b=new TallyCompositionBuilder(seed);Assert.AreEqual(1000,b.Priority);Assert.IsTrue(b.BuildZone(z,GrovelandsCompositionTests.Factory(),new Random(1)));
   var p=b.Plan;CollectionAssert.IsSubsetOf(new[]{"GoodsStore","RentalFrontage","RestCourt","ExchangeHall"},p.Rooms.Select(r=>r.Role));
   var seen=LastCounterCompositionTests.FloodAllCells(z);Assert.IsTrue(LastCounterCompositionTests.AllOpenCellsReached(z,seen));foreach(var r in p.Rooms)Assert.IsTrue(seen[r.DoorX,r.DoorY],r.Role);
   int road=0;for(int y=0;y<25;y++)for(int x=0;x<80;x++)
   {Assert.AreEqual(1,z.GetCell(x,y).Objects.Count(e=>e.BlueprintName==p.GroundAt(x,y)));if(p.GroundAt(x,y)=="RoadStone")road++;if(p.IsApproach(x,y)){Assert.IsFalse(z.GetCell(x,y).BlocksMovement());Assert.IsTrue(z.GenReservedCells.Contains((x,y)));}}
   Assert.That(road,Is.InRange(100,650));Assert.IsFalse(z.GetAllEntities().Any(e=>e.HasPart<LiquidPoolPart>()));Assert.IsFalse(z.GetCell(40,12).BlocksMovement());
   var store=p.Rooms.Single(r=>r.Role=="GoodsStore");var rest=p.Rooms.Single(r=>r.Role=="RestCourt");
   Assert.GreaterOrEqual(Enumerable.Range(store.X,store.Width).Sum(x=>Enumerable.Range(store.Y,store.Height).Count(y=>p.ObjectAt(x,y)=="Crate")),4);
   Assert.GreaterOrEqual(Enumerable.Range(rest.X,rest.Width).Sum(x=>Enumerable.Range(rest.Y,rest.Height).Count(y=>p.ObjectAt(x,y)=="Bed")),2);
  }
  [Test] public void LateGoodsAreNativeAndDoNotCreateForeignQuestGivers()
  {
   var f=GrovelandsCompositionTests.Factory();var z=new Zone(TallyCompositionPlan.ZoneID);var b=new TallyCompositionBuilder(64);Assert.IsTrue(b.BuildZone(z,f,new Random(1)));var late=new TallyProfileBuilder(b);Assert.AreEqual(3860,late.Priority);Assert.IsTrue(late.BuildZone(z,f,new Random(1)));Assert.IsFalse(late.BuildZone(z,f,new Random(1)));
   Assert.IsTrue(z.GetAllEntities().Any(e=>e.BlueprintName=="Chest"&&e.HasPart<ContainerPart>()));Assert.IsTrue(z.GetAllEntities().Any(e=>e.BlueprintName=="Campfire"&&e.HasPart<CampfirePart>()));
   Assert.IsFalse(z.GetAllEntities().Any(e=>new[]{"ConcordFactor","FilerClerk","CurationSorter","LastCounterSign"}.Contains(e.BlueprintName)));
  }
  [TestCase(64)][TestCase(1729)][TestCase(729490642)]
  public void FinalNativeServicesHaveTheirPlannedFrontagesStockAndDryAccess(int seed)
  {
   var z=new OverworldZoneManager(GrovelandsCompositionTests.Factory(),seed).GetZone(TallyCompositionPlan.ZoneID);var p=TallyCompositionPlan.Create(TallyCompositionPlan.ZoneID,seed);
   foreach(string bp in new[]{"Merchant","Quartermaster","Innkeeper"})
   {var e=z.GetAllEntities().Single(v=>v.BlueprintName==bp);Assert.IsTrue(p.TryGetServiceCell(bp,out int x,out int y));Assert.AreEqual((x,y),z.GetEntityPosition(e),bp);Assert.AreEqual("SaccharineConcord",e.GetTag("Faction"));}
   var rental=z.GetAllEntities().Single(v=>v.BlueprintName=="Quartermaster").GetPart<InventoryPart>();foreach(string bp in new[]{"LoanerDagger","LoanerSpear","LoanerLongsword"})Assert.IsTrue(rental.Objects.Any(e=>e.BlueprintName==bp));
   Assert.IsFalse(z.GetAllEntities().Any(e=>e.HasPart<LiquidPoolPart>()));Assert.IsTrue(z.GetCell(40,12).Objects.Any(e=>e.BlueprintName=="Well"));
   var positions=z.GetAllEntities().Where(e=>e.HasPart<ContainerPart>()||e.HasPart<CampfirePart>()||e.HasPart<TraderPart>()).Select(e=>(e.BlueprintName,cell:z.GetEntityPosition(e))).ToArray();
   foreach(var actor in z.GetAllEntities().Where(e=>e.HasPart<BrainPart>()||e.HasTag("Creature")).ToArray())z.RemoveEntity(actor);
   var seen=LastCounterCompositionTests.FloodAllCells(z);Assert.IsTrue(LastCounterCompositionTests.AllOpenCellsReached(z,seen));foreach(var owner in positions)Assert.IsTrue(CinderholdCompositionTests.Neighbors(owner.cell.x,owner.cell.y).Any(n=>z.InBounds(n.x,n.y)&&seen[n.x,n.y]),owner.BlueprintName+" at "+owner.cell);
  }
  [Test] public void CoveredExchangeSpineOpensItsWholeLoadingFaceInsteadOfCopyingClosedQuadrants()
  {
   var plans=Enumerable.Range(0,32).Select(seed=>TallyCompositionPlan.Create(TallyCompositionPlan.ZoneID,seed)).ToArray();
   foreach(var p in plans)
   {var hall=p.Rooms.Single(r=>r.Role=="ExchangeHall");int opening=Enumerable.Range(hall.X,hall.Width).Count(x=>p.ObjectAt(x,hall.DoorY)==null);
    if(p.FormationName=="CoveredExchangeSpine")
    {Assert.GreaterOrEqual(opening,hall.Width-4);int outside=hall.DoorY==hall.Y?-1:1;foreach(int dx in new[]{-8,0,8}){int x=hall.DoorX+dx,y=hall.DoorY+outside*2;Assert.AreEqual("RoadStone",p.GroundAt(x,y));Assert.IsTrue(p.IsReserved(x,y));Assert.IsNull(p.ObjectAt(x,y));Assert.IsFalse(p.IsInterior(x,y));}}
    else Assert.AreEqual(3,opening,"The other formations retain protected small-entry shelter circulation.");
   }
  }
  [TestCase("crate",8,9)][TestCase("wall",12,107)]
  public void QuietBroadMassesReplaceBrightPerCellStripes(string family,int a,int b)
  {
   var lib=TallyVoxelKitLibrary.Load();Assert.NotNull(lib);
   for(int v=0;v<4;v++)
   {var mesh=lib.Find(TallyVoxelKitLibrary.ModelId(family,v)).Mesh;Assert.LessOrEqual(mesh.vertexCount,48,"Two broad boxes at most, no repeated thin slats.");
    var slots=mesh.uv.Select(uv=>(int)(uv.y*8)*16+(int)(uv.x*16)).Distinct().ToArray();CollectionAssert.AreEquivalent(new[]{a,b},slots);}
  }
  [Test] public void CompleteArtKitIsQuietAndNativeSized()
  {
   var lib=TallyVoxelKitLibrary.Load();Assert.NotNull(lib);lib.Validate();Assert.AreEqual(16,lib.Entries.Length);
   foreach(var e in lib.Entries){Assert.LessOrEqual(e.Mesh.vertexCount,240);Assert.LessOrEqual(e.Mesh.bounds.size.x,1.001f);Assert.LessOrEqual(e.Mesh.bounds.size.z,1.001f);Assert.LessOrEqual(e.Mesh.uv.Distinct().Count(),2);}
   Assert.AreEqual("ground",TallyVoxelKitLibrary.Family("Floor"));Assert.AreEqual("path",TallyVoxelKitLibrary.Family("RoadStone"));Assert.AreEqual("wall",TallyVoxelKitLibrary.Family("TentWall"));Assert.IsNull(TallyVoxelKitLibrary.Family("ConcordFactor"));
  }
 }
}
