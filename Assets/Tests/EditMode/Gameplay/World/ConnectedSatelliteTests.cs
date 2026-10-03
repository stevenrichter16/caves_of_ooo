using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class ConnectedSatelliteTests
 {
  const BindingFlags All=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
  sealed class FirstRoll:Random {public override int Next(int max)=>max>=100?max-1:max>1?1:0;public override int Next(int min,int max)=>max==101?100:min;public override double NextDouble()=>.99;}
  internal sealed class Fixture:IDisposable
  {
   internal readonly HaulingContentScope Scope=new HaulingContentScope();internal EntityFactory Factory=>Scope.Factory;
   internal readonly Zone Zone=new Zone("Overworld.13.10.0");internal readonly SpreadCompositionBuilder Terrain=new SpreadCompositionBuilder(64){FormationOverride=Formation.OldRoad};
   internal readonly PopulationBuilder Population;internal readonly ContainerBuilder Containers=new ContainerBuilder(BiomeType.Spread,1,ContainerPlacementService.ZoneKind.Wilderness){CaptureSourceReceipts=true};
   internal Entity[] Owners;internal Func<bool> Final;
   internal Fixture(int count=2,string actor="MarlbackScrabbler",int? capacity=null,Action<Zone,EntityFactory> prepare=null)
   {
    Scope.Seed(64);if(capacity.HasValue)foreach(string bp in new[]{"Crate","Sack"})Factory.Blueprints[bp].Parts["Container"]["MaxItems"]=capacity.Value.ToString();Assert.True(Terrain.BuildZone(Zone,Factory,new Random(64)));
    foreach(var e in Zone.GetReadOnlyEntities().Where(e=>!(bool)typeof(DoorPart).GetMethod("IsBareGround",All).Invoke(null,new object[]{e})).ToArray())Zone.RemoveEntity(e);Zone.GenReservedCells.Clear();prepare?.Invoke(Zone,Factory);
    Population=new PopulationBuilder(new PopulationTable{Name="SpreadTier1",Entries=new List<PopulationEntry>{new PopulationEntry{BlueprintName=actor,EncounterGroup="SpreadTier1Encounter",MinCount=count,MaxCount=count}}}){CaptureSourceReceipts=true};
    Assert.True(Population.BuildZone(Zone,Factory,new Random(17)));Assert.True(Containers.BuildZone(Zone,Factory,new FirstRoll()));
    Assert.True(Population.SourceReceipt.IsCurrent);Assert.True(Containers.SourceReceipt.IsCurrent);
   }
   internal bool Place(string family,Func<bool> authority=null)
   {
    var type=typeof(Zone).Assembly.GetType("CavesOfOoo.Core.ConnectedSpreadSatellite");Assert.NotNull(type,"Optional wet/heavy situations need real cold generation.");
    object[] a={Zone,Factory,Terrain,Population,Containers,(SpreadExplorationFamily)Enum.Parse(typeof(SpreadExplorationFamily),family),authority??(()=>true),null,null};
    bool ok=(bool)type.GetMethod("TryPlace").Invoke(null,a);Owners=(Entity[])a[7];Final=(Func<bool>)a[8];return ok;
   }
   internal Func<bool> Proof(IEnumerable<Entity> owners)=>(Func<bool>)typeof(SpreadGenerationReceipt).GetMethod("CaptureFinalState",All).Invoke(null,new object[]{Zone,owners});
   public void Dispose()=>Scope.Dispose();
  }
  [TestCase("WetCrossing","Emberwheat",2,"ClaspbeanPulp")][TestCase("HeavyFrame","FireClay",2,null)]
  public void RealRolledCacheRetainsOriginalStockAndGetsOneFiniteUsefulPacket(string family,string bp,int quantity,string extra)
  {
   using(var f=new Fixture())
   {
    var original=f.Zone.GetReadOnlyEntities().ToArray();var creatures=f.Population.SourceReceipt.Owners.ToArray();var caches=f.Containers.SourceReceipt.Owners.ToArray();var goods=caches.SelectMany(e=>e.GetPart<ContainerPart>().Contents).ToArray();var counts=goods.ToDictionary(e=>e,e=>e.GetPart<StackerPart>()?.StackCount??1);
    Assert.True(f.Place(family));Assert.True(f.Final());Assert.True(original.All(e=>f.Zone.GetEntityCell(e)!=null));CollectionAssert.AreEquivalent(creatures,f.Zone.GetReadOnlyEntities().Where(e=>e.HasTag("Creature")));
    var all=caches.SelectMany(e=>e.GetPart<ContainerPart>().Contents).ToArray();Assert.True(goods.All(all.Contains));foreach(var e in goods)Assert.AreEqual(counts[e],e.GetPart<StackerPart>()?.StackCount??1);
    var added=all.Except(goods).ToArray();Assert.AreEqual(quantity,added.Where(e=>e.BlueprintName==bp).Sum(e=>e.GetPart<StackerPart>()?.StackCount??1));Assert.AreEqual(1,added.Count(e=>e.BlueprintName=="DitchkeepersFootwork"));if(extra!=null)Assert.AreEqual(1,added.Count(e=>e.BlueprintName==extra));
    Assert.False(f.Zone.GetCell(40,12).BlocksMovement());Assert.False(f.Place(family));Assert.AreEqual(added.Length,caches.SelectMany(e=>e.GetPart<ContainerPart>().Contents).Except(goods).Count());
   }
  }
  [TestCase(1,"MarlbackScrabbler")][TestCase(3,"MarlbackScrabbler")][TestCase(2,"Viper")]
  public void WetVariantNeverConvertsAnotherRollIntoTwoAttackers(int count,string actor)
  {using(var f=new Fixture(count,actor)){var owners=f.Zone.GetReadOnlyEntities().ToArray();var proof=f.Proof(owners);Assert.False(f.Place("WetCrossing"));Assert.True(proof());CollectionAssert.AreEquivalent(owners,f.Zone.GetReadOnlyEntities());}}
  [TestCase("authority")][TestCase("capture")][TestCase("locked")][TestCase("reserved")][TestCase("missing")]
  public void MissingSourceOrUnsafeGeometryCannotConsumeOrLeaveASite(string fault)
  {using(var f=new Fixture()){if(fault=="capture")f.Containers.CaptureSourceReceipts=false;if(fault=="locked")foreach(var e in f.Containers.SourceReceipt.Owners)e.GetPart<ContainerPart>().Locked=true;if(fault=="reserved")f.Zone.ForEachCell((c,x,y)=>f.Zone.GenReservedCells.Add((x,y)));if(fault=="missing")f.Factory.Blueprints.Remove("DitchkeepersFootwork");var owners=f.Zone.GetReadOnlyEntities().ToArray();var proof=f.Proof(owners);Assert.False(f.Place("HeavyFrame",()=>fault!="authority"));Assert.True(proof());CollectionAssert.AreEquivalent(owners,f.Zone.GetReadOnlyEntities());}}
  [TestCase(16,false)][TestCase(18,true)][TestCase(20,true)]
  public void ActualFrameObeysExistingStrengthLimitAndAllBuildsHaveTheOutsideRoute(int strength,bool allowed)
  {using(var f=new Fixture()){Assert.True(f.Place("HeavyFrame"));var frame=f.Owners.Single(e=>e.BlueprintName=="ConnectedHeavyFrame");Assert.AreEqual(136,frame.GetPart<HandlingPart>().Weight);var p=f.Zone.GetEntityPosition(frame);var player=new Entity{ID=Guid.NewGuid().ToString("N")};player.AddPart(new PhysicsPart());player.AddPart(new InventoryPart());player.Statistics["Strength"]=new Stat{Owner=player,Name="Strength",BaseValue=strength,Max=100};Assert.True(f.Zone.AddEntity(player,p.x,p.y-1));Assert.AreEqual(allowed,DragSystem.TryGrab(player,frame,f.Zone)==DragVerdict.Ok);Assert.True(Reach(f.Zone,(p.x,p.y-2),(p.x,p.y+2)));}}
  [Test]public void WetLaneUsesActualLiquidAndOpaqueCoverWithAnUnwetDryApproach()
  {using(var f=new Fixture()){Assert.True(f.Place("WetCrossing"));var pools=f.Owners.Where(e=>e.BlueprintName=="WaterPuddle").ToArray();Assert.AreEqual(4,pools.Length);Assert.True(pools.All(e=>e.GetPart<LiquidPoolPart>()?.LiquidId=="water"));var walls=f.Owners.Where(e=>e.BlueprintName=="StoneWall").ToArray();Assert.GreaterOrEqual(walls.Length,6);Assert.True(walls.All(e=>e.GetPart<PhysicsPart>().Solid));var cache=f.Owners.Single(e=>e.HasPart<ContainerPart>());var at=f.Zone.GetEntityPosition(cache);Assert.True(Reach(f.Zone,(40,12),(at.x-1,at.y)));Assert.True(Reach(f.Zone,(40,12),(at.x+1,at.y)));}}
  [TestCase("clear",true)][TestCase("foliage",true)][TestCase("blocking",false)][TestCase("hazard",false)]
  [TestCase("reserved",false)][TestCase("interior",false)][TestCase("actor",false)][TestCase("placement",false)]
  public void UntouchedCorridorFoliageIsAllowedButHazardsAndActualPlacementOccupantsAreNot(string kind,bool allowed)
  {
   Entity feature=null;
   using(var f=new Fixture(prepare:(z,factory)=>
   {
    // A single candidate rectangle isolates corridor admission from finding
    // another empty site. It never replaces an original producer-owned entity.
    z.ForEachCell((c,x,y)=>c.IsInterior=x<14||x>21||y<8||y>16);
    if(kind=="clear")return;int xx=kind=="placement"?15:14;
    string bp=kind=="blocking"?"StoneWall":kind=="hazard"?"WaterPuddle":kind=="actor"?"MarlbackScrabbler":"FrostLichenPatch";
    feature=factory.CreateEntity(bp);Assert.True(z.AddEntity(feature,xx,12));
    if(kind=="reserved")z.GenReservedCells.Add((14,12));
    if(kind=="interior")z.GetCell(14,12).IsInterior=true;
   }))
   {
    Assert.True(f.Population.SourceReceipt.IsCurrent);Assert.True(f.Containers.SourceReceipt.IsCurrent);
    var original=f.Zone.GetReadOnlyEntities().ToArray();var proof=f.Proof(original);
    var featureProof=feature==null?null:f.Proof(new[]{feature});
    Assert.AreEqual(allowed,f.Place("WetCrossing"));
    Assert.True(original.All(e=>f.Zone.GetEntityCell(e)!=null));if(feature!=null)Assert.True(featureProof());
    if(allowed)Assert.True(f.Final());else{Assert.True(proof());CollectionAssert.AreEquivalent(original,f.Zone.GetReadOnlyEntities());}
   }
  }
  [TestCase(64)][TestCase(1729)][TestCase(729490642)][TestCase(42)][TestCase(9091)]
  public void NewWorldChoosesOneLiteralNearbySatelliteWithoutGeneratingAnyGraph(int seed)
  {using(var scope=new HaulingContentScope()){var m=OverworldZoneManager.CreateDetached(scope.Factory,seed,true);var rows=m.Exploration.Entries.Where(e=>e.Family.ToString()=="WetCrossing"||e.Family.ToString()=="HeavyFrame").ToArray();Assert.AreEqual(1,rows.Length);Assert.AreEqual(0,m.CachedZoneCount);var at=WorldMap.FromZoneID(rows[0].ZoneID);Assert.LessOrEqual(Math.Abs(at.x-11)+Math.Abs(at.y-10),4);Assert.AreEqual("SeedKeepersPlot",m.Exploration.Find("Overworld.11.8.0").Family.ToString());Assert.AreEqual("WaysideKitchen",m.Exploration.Find("Overworld.12.11.0").Family.ToString());var world=SpreadExplorationPlan.BindForSave(m,null);var restore=typeof(SpreadExplorationPlan).GetMethod("Restore",All);var restored=(SpreadExplorationPlan)restore.Invoke(null,new object[]{m,world});Assert.AreEqual(rows[0].Family,restored.Find(rows[0].ZoneID).Family);}}
  static bool Reach(Zone z,(int x,int y)start,(int x,int y)end)
  {var q=new Queue<(int x,int y)>();var seen=new HashSet<(int,int)>{start};q.Enqueue(start);while(q.Count>0){var p=q.Dequeue();if(p==end)return true;for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){var n=(p.x+dx,p.y+dy);if(n.Item1<0||n.Item1>=Zone.Width||n.Item2<0||n.Item2>=Zone.Height||!seen.Add(n)||z.GetCell(n.Item1,n.Item2).BlocksMovement())continue;q.Enqueue(n);}}return false;}
 }
}
