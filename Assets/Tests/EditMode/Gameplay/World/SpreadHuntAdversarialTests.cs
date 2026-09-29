using System;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class HuntCreationProbe:Part
 {public static Action<Entity> Callback;public override bool HandleEvent(GameEvent e){if(e.ID=="ObjectCreated")Callback?.Invoke(ParentEntity);return true;}}
 public sealed class SpreadHuntAdversarialTests
 {
  static Entity Foreign(HuntFixture f,int x=40,int y=12){var e=new Entity{ID=Guid.NewGuid().ToString("N"),BlueprintName="callback-foreign"};if(e.GetPart<PhysicsPart>()==null)e.AddPart(new PhysicsPart());Assert.True(f.Zone.AddEntity(e,x,y));return e;}
  [Test]public void FirstAuthorizationCannotAddAnUnrelatedOwnerBeforeTheSnapshot()
  {using(var f=new HuntFixture()){Entity foreign=null;var original=f.Sources;Assert.False(f.Place(authority:()=>{if(foreign==null)foreign=Foreign(f);return true;}));Assert.IsNotNull(foreign);Assert.AreSame(f.Zone,HuntFixture.Spatial(foreign));Assert.True(original.All(e=>HuntFixture.Spatial(e)==f.Zone));}}
  [Test]public void OrdinaryReadOnlyAuthorizationPermitsTheSameCompleteTransaction()
  {using(var f=new HuntFixture()){int calls=0;Assert.True(f.Place(authority:()=>{calls++;return true;}));Assert.Greater(calls,3);Assert.True(f.Final());}}
  [Test]public void SourceDamageDuringAuthorizationIsNotSilentlyRepaired()
  {using(var f=new HuntFixture()){var source=f.Sources[0];int hp=source.GetStatValue("Hitpoints");bool changed=false;Assert.False(f.Place(authority:()=>{if(!changed){source.GetStat("Hitpoints").BaseValue--;changed=true;}return true;}));Assert.True(changed);Assert.AreEqual(hp-1,source.GetStatValue("Hitpoints"));Assert.AreSame(f.Zone,HuntFixture.Spatial(source));}}
  [Test]public void CallbackAfterFirstDetachRestoresCleanSourcesButKeepsForeignAddition()
  {using(var f=new HuntFixture()){var sources=f.Sources;var before=sources.Select(f.Zone.GetEntityPosition).ToArray();Entity foreign=null;Assert.False(f.Place(authority:()=>{if(foreign==null&&sources.Any(e=>HuntFixture.Spatial(e)==null))foreign=Foreign(f);return true;}));Assert.IsNotNull(foreign);Assert.AreSame(f.Zone,HuntFixture.Spatial(foreign));for(int i=0;i<sources.Length;i++)Assert.AreEqual(before[i],f.Zone.GetEntityPosition(sources[i]));}}
  [Test]public void IndependentlyMovedOriginalOwnerIsNotClobberedDuringPartialRollback()
  {using(var f=new HuntFixture()){var sources=f.Sources;var before=sources.Select(f.Zone.GetEntityPosition).ToArray();bool changed=false;var moved=sources.Last();Assert.False(f.Place(authority:()=>{if(!changed&&sources.Any(e=>HuntFixture.Spatial(e)==null)){Assert.True(f.Zone.MoveEntity(moved,42,12));changed=true;}return true;}));Assert.True(changed);Assert.AreEqual((42,12),f.Zone.GetEntityPosition(moved));for(int i=0;i<sources.Length-1;i++)Assert.AreEqual(before[i],f.Zone.GetEntityPosition(sources[i]));}}
  [Test]public void ChangedDetachedSourceGraphIsNotReclaimedByRollback()
  {using(var f=new HuntFixture()){var sources=f.Sources;Entity changed=null;int weight=0;Assert.False(f.Place(authority:()=>{if(changed==null){changed=sources.FirstOrDefault(e=>HuntFixture.Spatial(e)==null);if(changed!=null){weight=changed.GetPart<PhysicsPart>().Weight;changed.GetPart<PhysicsPart>().Weight++;}}return true;}));Assert.IsNotNull(changed);Assert.IsNull(HuntFixture.Spatial(changed));Assert.AreEqual(weight+1,changed.GetPart<PhysicsPart>().Weight);}}
  [Test]public void AForeignBlockerAtTheOriginalCellSurvivesRollback()
  {using(var f=new HuntFixture()){var sources=f.Sources;var positions=sources.Select(f.Zone.GetEntityPosition).ToArray();Entity foreign=null,removed=null;Assert.False(f.Place(authority:()=>{if(foreign==null){int index=Array.FindIndex(sources,e=>HuntFixture.Spatial(e)==null);if(index>=0){removed=sources[index];foreign=Foreign(f,positions[index].x,positions[index].y);foreign.GetPart<PhysicsPart>().Solid=true;}}return true;}));Assert.IsNotNull(foreign);Assert.AreSame(f.Zone,HuntFixture.Spatial(foreign));Assert.IsNull(HuntFixture.Spatial(removed));}}
  [TestCase("move")][TestCase("damage")]
  public void CallbackCannotModifyEarlierPlacedNewHunterBeforePairConfiguration(string fault)
  {using(var f=new HuntFixture()){bool changed=false;Entity target=null;Assert.False(f.Place(authority:()=>{if(!changed){target=f.Zone.GetReadOnlyEntities().FirstOrDefault(e=>e.BlueprintName=="Furrowstalker");if(target!=null){changed=true;if(fault=="move")Assert.True(f.Zone.MoveEntity(target,42,12));else target.GetStat("Hitpoints").BaseValue--;}}return true;}));Assert.True(changed);Assert.AreSame(f.Zone,HuntFixture.Spatial(target),"Independent modification must not be overwritten.");}}
  [TestCase("move")][TestCase("damage")]
  public void SecondOwnerAuthorizationCannotChangeEarlierPlacedHunter(string fault)
  {using(var f=new HuntFixture()){bool changed=false;Entity target=null;Assert.False(f.Place(authority:()=>{if(!changed&&f.Zone.GetReadOnlyEntities().Any(e=>e.BlueprintName=="ReedbackGrazer")){target=f.Zone.GetReadOnlyEntities().FirstOrDefault(e=>e.BlueprintName=="Furrowstalker");if(target!=null){changed=true;if(fault=="move")Assert.True(f.Zone.MoveEntity(target,42,12));else target.GetStat("Hitpoints").BaseValue--;}}return true;}));Assert.True(changed);Assert.AreSame(f.Zone,HuntFixture.Spatial(target),"Independent modification must not be overwritten.");}}
  [TestCase("party")][TestCase("reserved")][TestCase("interior")][TestCase("dead")]
  public void ProtectedOriginalActorPreventsTheEntireTwoSourceReplacement(string fault)
  {using(var f=new HuntFixture()){var target=f.Bird;if(fault=="party")target.GetPart<BrainPart>().PartyLeader=f.Sources[0];if(fault=="reserved")f.Zone.GenReservedCells.Add(f.Zone.GetEntityPosition(target));if(fault=="interior")f.Zone.GetEntityCell(target).IsInterior=true;if(fault=="dead")target.GetStat("Hitpoints").BaseValue=0;var owners=f.Zone.GetReadOnlyEntities().ToArray();Assert.False(f.Place());CollectionAssert.AreEquivalent(owners,f.Zone.GetReadOnlyEntities());Assert.True(f.Sources.All(e=>HuntFixture.Spatial(e)==f.Zone));}}
  [TestCase("damage")][TestCase("reserved")][TestCase("interior")][TestCase("transparent")]
  public void CoveredVariantCannotClaimInvalidOriginalTrees(string fault)
  {using(var f=new HuntFixture()){foreach(var tree in f.Zone.GetReadOnlyEntities().Where(e=>e.BlueprintName=="Tree")){if(fault=="damage")tree.GetPart<DestructiblePart>().HP--;if(fault=="reserved")f.Zone.GenReservedCells.Add(f.Zone.GetEntityPosition(tree));if(fault=="interior")f.Zone.GetEntityCell(tree).IsInterior=true;if(fault=="transparent")tree.Tags.Remove("Solid");}var owners=f.Zone.GetReadOnlyEntities().ToArray();Assert.False(f.Place(true));CollectionAssert.AreEquivalent(owners,f.Zone.GetReadOnlyEntities());}}
  [Test]public void OpenVariantNeedsNoTreeClaimWhenAllOrdinaryTreesAreAbsent()
  {using(var f=new HuntFixture()){foreach(var tree in f.Zone.GetReadOnlyEntities().Where(e=>e.BlueprintName=="Tree").ToArray())f.Zone.RemoveEntity(tree);Assert.True(f.Place(false));Assert.True(f.Final());}}
  [Test]public void OriginalSourceDuplicateIdCannotBeClaimedOrRemoved()
  {using(var f=new HuntFixture()){var source=f.Sources[0];var duplicate=Foreign(f);duplicate.ID=source.ID;var owners=f.Zone.GetReadOnlyEntities().ToArray();Assert.False(f.Place());CollectionAssert.AreEquivalent(owners,f.Zone.GetReadOnlyEntities());Assert.AreSame(f.Zone,HuntFixture.Spatial(source));}}
  [Test]public void MatchingReplacementMagpieCannotStandInForTheExactOriginalRoll()
  {using(var f=new HuntFixture()){var bird=f.Bird;var at=f.Zone.GetEntityPosition(bird);Assert.True(f.Zone.RemoveEntity(bird));var other=f.Factory.CreateEntity("Magpie");other.ID=bird.ID;Assert.True(f.Zone.AddEntity(other,at.x,at.y));Assert.False(f.Place());Assert.AreSame(f.Zone,HuntFixture.Spatial(other));Assert.IsNull(HuntFixture.Spatial(bird));}}
  [Test]public void StalePopulationProducerCannotClaimEarlierZoneSources()
  {using(var f=new HuntFixture()){var sources=f.Sources;Assert.True(f.Population.BuildZone(new Zone("other"),f.Factory,new Random(55)));Assert.False(f.Place());Assert.True(sources.All(e=>HuntFixture.Spatial(e)==f.Zone));}}
  [Test]public void ASecondAttemptCannotReuseEitherConsumedSourceAllowance()
  {using(var f=new HuntFixture()){Assert.True(f.Place());var owners=f.Zone.GetReadOnlyEntities().ToArray();Assert.False(f.Place());CollectionAssert.AreEquivalent(owners,f.Zone.GetReadOnlyEntities());}}
  [Test]public void AlreadyStockedMagpieMustNotBeRefreshedIntoAnEarlierReceipt()
  {using(var f=new HuntFixture()){Assert.True(new TradeStockBuilder().BuildZone(f.Zone,f.Factory,new Random(8)));Assert.False(f.Population.AmbientSourceReceipt.IsCurrent);var owners=f.Zone.GetReadOnlyEntities().ToArray();Assert.False(f.Place());CollectionAssert.AreEquivalent(owners,f.Zone.GetReadOnlyEntities());}}
  [Test]public void LaterOrdinaryStockForOtherMagpiesDoesNotInvalidateAcceptedPair()
  {using(var f=new HuntFixture()){Assert.True(f.Place());Assert.True(new TradeStockBuilder().BuildZone(f.Zone,f.Factory,new Random(8)));Assert.True(f.Zone.GetReadOnlyEntities().Where(e=>e.BlueprintName=="Magpie").All(e=>e.GetPart<InventoryPart>().Objects.Count>0));Assert.True(f.Final());}}
  [Test]public void LaterBlockerOnTheAcceptedPairAnchorInvalidatesColdCommitment()
  {using(var f=new HuntFixture()){Assert.True(f.Place());var at=f.Zone.GetEntityPosition(f.Grazer);var blocker=Foreign(f,at.x,at.y);blocker.GetPart<PhysicsPart>().Solid=true;Assert.False(f.Final());}}
  [Test]public void FactoryCreationCallbackCannotAddAnUnrelatedExistingZoneOwner()
  {using(var f=new HuntFixture()){Entity foreign=null;HuntCreationProbe.Callback=e=>foreign=Foreign(f);f.Factory.RegisterPartType<HuntCreationProbe>();f.Factory.Blueprints["Furrowstalker"].Parts["HuntCreationProbe"]=new System.Collections.Generic.Dictionary<string,string>();try{Assert.False(f.Place());Assert.NotNull(foreign);Assert.AreSame(f.Zone,HuntFixture.Spatial(foreign));Assert.True(f.Sources.All(e=>HuntFixture.Spatial(e)==f.Zone));}finally{HuntCreationProbe.Callback=null;}}}
  [Test]public void ReadOnlyFactoryCreationCallbackAllowsTheSamePlacement()
  {using(var f=new HuntFixture()){int calls=0;HuntCreationProbe.Callback=e=>calls++;f.Factory.RegisterPartType<HuntCreationProbe>();f.Factory.Blueprints["Furrowstalker"].Parts["HuntCreationProbe"]=new System.Collections.Generic.Dictionary<string,string>();try{Assert.True(f.Place());Assert.AreEqual(1,calls);Assert.True(f.Final());}finally{HuntCreationProbe.Callback=null;}}}
  [Test]public void FinalAuthorizationCannotAddAnOwnerAfterBothPairLinksWereConfigured()
  {using(var f=new HuntFixture()){Entity foreign=null;var sources=f.Sources;Assert.False(f.Place(authority:()=>{if(foreign==null&&f.Zone.GetReadOnlyEntities().Any(e=>e.Parts.Any(p=>p.Name=="SpreadPredator"&&(bool)p.GetType().GetField("Configured").GetValue(p))))foreign=Foreign(f);return true;}));Assert.IsNotNull(foreign);Assert.AreSame(f.Zone,HuntFixture.Spatial(foreign));Assert.True(sources.All(e=>HuntFixture.Spatial(e)==f.Zone));}}
 }
}
