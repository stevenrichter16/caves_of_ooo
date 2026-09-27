using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class FreshGamePlacementTests
 {
  Zone zone;Entity actor;
  [SetUp]public void Setup(){zone=new Zone("start");actor=Owner("Player");actor.SetTag("Player");actor.SetTag("Creature");actor.Statistics["Hitpoints"]=new Stat{Name="Hitpoints",BaseValue=40,Max=40};FactionManager.Initialize();}
  static Entity Owner(string bp,bool solid=false){var e=new Entity{BlueprintName=bp};e.AddPart(new PhysicsPart{Solid=solid});return e;}
  bool Place(Zone z=null,Entity a=null,int x=40,int y=12){var type=typeof(Zone).Assembly.GetType("CavesOfOoo.Core.FreshGamePlacement");Assert.NotNull(type,"fresh placement must honor physical occupancy before actual Zone.AddEntity");var method=type.GetMethod("TryPlace",BindingFlags.Public|BindingFlags.Static);Assert.NotNull(method);return (bool)method.Invoke(null,new object[]{z??zone,a??actor,x,y});}
  [TestCase(true)][TestCase(false)] public void PhysicsOnlyCenterOwnerIsPreservedAndOnlySolidOwnerDisplacesStart(bool solid){var prop=Owner("heavy table",solid);zone.AddEntity(prop,40,12);Assert.True(Place());Assert.AreEqual((40,12),zone.GetEntityPosition(prop));Assert.AreEqual(!solid,zone.GetEntityPosition(actor)==(40,12));}
  [TestCase(true)][TestCase(false)]public void NonSolidCreatureIsNotAValidStartingCell(bool creature){var prop=Owner("occupant");if(creature)prop.SetTag("Creature");zone.AddEntity(prop,40,12);Assert.True(Place());Assert.AreEqual(!creature,zone.GetEntityPosition(actor)==(40,12));}
  [TestCase("trap")][TestCase("pool")][TestCase("gas")][TestCase("heat")][TestCase("cold")][TestCase("charge")][TestCase("coating")][TestCase("cloud")]
  public void ExistingCellHazardsAreNotSilentlyConsumedOrUsedAsStarts(string kind){var e=Owner(kind);if(kind=="trap")e.AddPart(new SpikeTrapTriggerPart());if(kind=="pool")e.AddPart(new LiquidPoolPart{LiquidId="water",Volume=1});if(kind=="gas")e.AddPart(new GasPoolPart());zone.AddEntity(e,40,12);if(kind=="heat")zone.TileState.AddHeat(40,12,1);if(kind=="cold")zone.TileState.AddCold(40,12,1);if(kind=="charge")zone.TileState.AddCharge(40,12,1);if(kind=="coating")zone.TileState.WriteCoating(40,12,"oil",2);if(kind=="cloud")zone.TileState.WriteCloud(40,12,"smoke",2);string state=zone.TileState.ToSaveString();Assert.True(Place());Assert.AreNotEqual((40,12),zone.GetEntityPosition(actor));Assert.AreEqual((40,12),zone.GetEntityPosition(e));Assert.AreEqual(state,zone.TileState.ToSaveString());}
  [Test]public void SecondaryFootprintOccupancyBlocksASeeminglyEmptyAnchor(){var e=Owner("wide table",true);e.AddPart(new SpatialFootprintPart{CellsRaw="0,0;1,0"});Assert.True(zone.AddEntity(e,39,12));Assert.IsEmpty(zone.GetCell(40,12).Objects);Assert.True(Place());Assert.AreNotEqual((40,12),zone.GetEntityPosition(actor));Assert.AreEqual((39,12),zone.GetEntityPosition(e));}
  [Test]public void FreshMultiCellBodyUsesAllItsCells(){actor.AddPart(new SpatialFootprintPart{CellsRaw="0,0;1,0"});zone.AddEntity(Owner("solid",true),41,12);Assert.True(Place());Assert.AreNotEqual((40,12),zone.GetEntityPosition(actor));Assert.True(zone.CanPlaceFootprint(actor,zone.GetEntityPosition(actor).x,zone.GetEntityPosition(actor).y));}
  [TestCase(true)][TestCase(false)]public void ExistingActorIsNeverRelocatedOrDuplicated(bool foreign){var other=foreign?new Zone("foreign"):zone;other.AddEntity(actor,3,3);Assert.False(Place());Assert.AreEqual((3,3),other.GetEntityPosition(actor));if(foreign)Assert.IsNull(zone.GetEntityCell(actor));}
  [Test]public void CompletelyBlockedZoneFailsWithoutForcedCenter(){zone.ForEachCell((c,x,y)=>zone.AddEntity(Owner("block",true),x,y));int n=zone.EntityCount;Assert.False(Place());Assert.AreEqual(n,zone.EntityCount);Assert.IsNull(zone.GetEntityCell(actor));}
  [Test]public void SealedOneCellPocketIsSkippedWithoutDeletingItsWalls(){for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)if(dx!=0||dy!=0)zone.AddEntity(Owner("block",true),40+dx,12+dy);Assert.True(Place());Assert.AreNotEqual((40,12),zone.GetEntityPosition(actor));Assert.AreEqual(9,zone.EntityCount);}
  [TestCase(true)][TestCase(false)]public void ImmediatePersonallyHostileOwnerMovesStartButFriendlyControlDoesNot(bool hostile){var e=Owner("person");e.SetTag("Creature");var brain=new BrainPart();e.AddPart(brain);if(hostile)brain.SetPersonallyHostile(actor,false);zone.AddEntity(e,42,12);Assert.True(Place());Assert.AreEqual(!hostile,zone.GetEntityPosition(actor)==(40,12));}
  [TestCase("dead")][TestCase("death-handled")][TestCase("carried")][TestCase("equipped")]
  public void InvalidActorFailsWithoutWorldMutation(string kind){if(kind=="dead")actor.GetStat("Hitpoints").BaseValue=0;if(kind=="death-handled")actor.SetTag("_DeathHandled");if(kind=="carried")actor.GetPart<PhysicsPart>().InInventory=Owner("other");if(kind=="equipped")actor.GetPart<PhysicsPart>().Equipped=Owner("other");Assert.False(Place());Assert.AreEqual(0,zone.EntityCount);}
  [TestCase(true)][TestCase(false)]public void TwoCellPocketRequiresRealCardinalRouteToAnEdge(bool sealedPocket){for(int x=39;x<=42;x++)for(int y=11;y<=13;y++)if((x!=40&&x!=41)||y!=12){if(!sealedPocket&&x==42&&y==12)continue;zone.AddEntity(Owner("wall",true),x,y);}int n=zone.EntityCount;Assert.True(Place());Assert.AreEqual(!sealedPocket,zone.GetEntityPosition(actor)==(40,12));Assert.AreEqual(n+1,zone.EntityCount);}
  [Test]public void EntireSealedInteriorRefusesInsteadOfCallingAnAdjacentStepAnExit(){for(int x=0;x<Zone.Width;x++){zone.AddEntity(Owner("wall",true),x,0);zone.AddEntity(Owner("wall",true),x,Zone.Height-1);}for(int y=1;y<Zone.Height-1;y++){zone.AddEntity(Owner("wall",true),0,y);zone.AddEntity(Owner("wall",true),Zone.Width-1,y);}int n=zone.EntityCount;Assert.False(Place());Assert.AreEqual(n,zone.EntityCount);}
 }
}
