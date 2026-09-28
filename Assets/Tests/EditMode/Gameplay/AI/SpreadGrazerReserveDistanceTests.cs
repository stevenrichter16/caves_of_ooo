using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadGrazerReserveDistanceTests
 {
  sealed class Fixture:IDisposable
  {
   public readonly SpreadExplorationActorTests.Fixture F=new SpreadExplorationActorTests.Fixture();
   public Zone Z=>F.Zone;public SpreadGrazerPart Role=>F.Actor.GetPart<SpreadGrazerPart>();
   public Fixture(int reserveX=60){F.Actor.BlueprintName="ReedbackGrazer";F.Brain.Passive=true;F.Add("SpreadGrazerPart");F.Move(F.Reserve,reserveX,15);for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++){var g=new Entity{ID="floor-"+x+","+y,BlueprintName="Grass"};g.Tags["Terrain"]="true";Assert.True(Z.AddEntity(g,x,y));}}
   public void Wall(int x,int y){var e=F.Prop("wall-"+x+","+y,x,y);e.GetPart<PhysicsPart>().Solid=true;}
   public bool Place()=>SpreadExplorationActorPlacement.TryGleanings(Z,F.Actor,()=>true);
   public void Dispose()=>F.Dispose();
  }
  [TestCase(16)][TestCase(60)]public void ReservedPlayerSupplyMayRemainFarFromTheSingleActualMeal(int x)
  {using(var f=new Fixture(x)){var owners=f.Z.GetReadOnlyEntities().ToArray();var food=f.Z.GetEntityPosition(f.F.Food);var reserve=f.Z.GetEntityPosition(f.F.Reserve);Assert.True(f.Place());Assert.AreSame(f.F.Food,f.Role.Food);Assert.AreSame(f.F.Reserve,f.Role.ReservedRow);Assert.AreEqual(food,f.Z.GetEntityPosition(f.F.Food));Assert.AreEqual(reserve,f.Z.GetEntityPosition(f.F.Reserve));CollectionAssert.AreEquivalent(owners,f.Z.GetReadOnlyEntities());var proof=SpreadExplorationActorPlacement.CaptureFinalGeometry(f.Z,f.F.Actor);Assert.NotNull(proof);Assert.True(proof());f.F.Turn();Assert.True(f.Role.Fed);Assert.True(f.F.Food.GetPart<FieldHarvestPart>().Harvested);Assert.False(f.F.Reserve.GetPart<FieldHarvestPart>().Harvested);f.F.Turn();Assert.False(f.F.Reserve.GetPart<FieldHarvestPart>().Harvested);CollectionAssert.AreEquivalent(owners,f.Z.GetReadOnlyEntities());}}
  [Test]public void DirectConfigurationKeepsTheActualMealApproachBounded()
  {using(var f=new Fixture()){Assert.True(f.Role.ConfigureForage(f.Z,f.F.Food,f.F.Reserve));}using(var f=new Fixture()){f.F.Move(f.F.Food,55,10);Assert.False(f.Role.ConfigureForage(f.Z,f.F.Food,f.F.Reserve));}}
  [TestCase("removed")][TestCase("spent")][TestCase("sealed")]
  public void UnavailableDistantReserveCannotConfigureOrConsume(string fault)
  {using(var f=new Fixture()){if(fault=="removed")f.Z.RemoveEntity(f.F.Reserve);if(fault=="spent")f.F.Reserve.GetPart<FieldHarvestPart>().Harvested=true;if(fault=="sealed")for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)if(dx!=0||dy!=0)f.Wall(60+dx,15+dy);var before=f.Z.GetEntityPosition(f.F.Actor);Assert.False(f.Place());Assert.False(f.Role.Configured);Assert.False(f.F.Food.GetPart<FieldHarvestPart>().Harvested);Assert.AreEqual(before,f.Z.GetEntityPosition(f.F.Actor));}}
  [TestCase("removed")][TestCase("spent")][TestCase("moved")]
  public void CurrentDistantReserveIdentityStillGatesTheActualFeed(string fault)
  {using(var f=new Fixture()){Assert.True(f.Place());if(fault=="removed")f.Z.RemoveEntity(f.F.Reserve);if(fault=="spent")f.F.Reserve.GetPart<FieldHarvestPart>().Harvested=true;if(fault=="moved")f.F.Move(f.F.Reserve,61,15);f.F.Turn();Assert.False(f.Role.Fed);Assert.False(f.F.Food.GetPart<FieldHarvestPart>().Harvested);}}
  [Test]public void LateSealedDistantReserveInvalidatesAcceptedGeometry()
  {using(var f=new Fixture()){Assert.True(f.Place());var proof=SpreadExplorationActorPlacement.CaptureFinalGeometry(f.Z,f.F.Actor);Assert.True(proof());for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)if(dx!=0||dy!=0)f.Wall(60+dx,15+dy);Assert.False(proof());Assert.False(f.Role.Fed);}}
  [Test]public void SavedDistantReserveStillPermitsOnlyOnePaidMeal()
  {using(var f=new Fixture()){Assert.True(f.Place());f.F.RoundTrip();Assert.AreEqual(60,f.Role.ReservedX);Assert.AreSame(f.F.Reserve,f.Role.ReservedRow);var turns=new TurnManager();turns.AddEntity(f.F.Actor);turns.AddEntity(f.F.Player);Assert.AreSame(f.F.Player,turns.ProcessUntilPlayerTurn());Assert.AreEqual(1,f.F.Probe.Ends);Assert.True(f.Role.Fed);Assert.False(f.F.Reserve.GetPart<FieldHarvestPart>().Harvested);Assert.Zero(f.F.Probe.Attacks);}}
 }
}
