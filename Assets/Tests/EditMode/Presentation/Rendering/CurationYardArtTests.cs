using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
 // Generated source authority/imported geometry and explicit presenter refresh.
 // This does not claim ordinary discovery, paid input or readable camera pixels.
 public sealed class CurationYardArtTests
 {
  const string Prefix="curation-yard-",Folder="CurationYard3D/";
  static SpawnRing3DRecipe Recipe(SpawnRing3DIntegrationFixture f,Entity e)=>SpawnRing3DRecipes.Resolve(f.Zone,e,f.Library.Definition);
  static Entity Generated(SpawnRing3DIntegrationFixture f,string bp)=>f.Zone.GetReadOnlyEntities().FirstOrDefault(e=>e.BlueprintName==bp)??throw new AssertionException("SOURCE PRECONDITION: generated "+bp+" must exist before interpreting art RED.");
  static int Case(Entity e,string part)=> (int)e.GetPart(part).GetType().GetField("CaseNumber").GetValue(e.GetPart(part));
  static bool Current(Part part,Zone z)=>part!=null&&(bool)part.GetType().GetMethod("IsCurrent").Invoke(part,new object[]{z});
  static Mesh Exact(SpawnRing3DIntegrationFixture f,Entity e,string suffix)
  {
   var r=Recipe(f,e);Assert.AreSame(e,r.Owner);Assert.Null(r.Failure);Assert.AreEqual(Prefix+suffix,r.ModelId);f.Refresh();Assert.True(f.Rendered(e));
   Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(e,out var proof),proof.Failure);Assert.AreEqual(r.ModelId,proof.ModelId);
   var prefab=Resources.Load<GameObject>(Folder+r.ModelId);Assert.NotNull(prefab,r.ModelId);var mesh=prefab.GetComponent<MeshFilter>().sharedMesh;
   Assert.AreSame(mesh,proof.ExpectedMesh);Assert.AreSame(ReferenceGladeVoxelLibrary.Load().Material,proof.ExpectedMaterial);Assert.Greater(mesh.vertexCount,24);Assert.GreaterOrEqual(mesh.uv.Distinct().Count(),2);
   Assert.That(prefab.GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(prefab.GetComponentsInChildren<MonoBehaviour>(true),Is.Empty);Assert.That(prefab.GetComponentsInChildren<Animator>(true),Is.Empty);return mesh;
  }
  [TestCase("CurationIntakeIndex","intake-index")][TestCase("CurationToolCabinet","tool-cabinet")]
  [TestCase("CurationSaltBench","salt-bench")][TestCase("CurationQuarantineRail","quarantine-rail")]
  public void ActualGeneratedFixtureHasItsOwnVisibleOriginalFormWithoutChangingContents(string bp,string suffix)
  {
   using(var f=new SpawnRing3DIntegrationFixture(MarrowstyeCompositionPlan.ZoneID))
   {
    var e=Generated(f,bp);var at=f.Zone.GetEntityPosition(e);var owners=f.Zone.GetReadOnlyEntities().ToArray();var stock=e.GetPart<ContainerPart>()?.Contents.ToArray();var count=stock?.Select(x=>x.GetPart<StackerPart>()?.StackCount??1).ToArray();var source=Exact(f,e,suffix);
    var native=(SpawnRing3DRecipe)typeof(SpawnRing3DRecipes).GetMethod("ResolveNative",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{f.Zone,e,f.Library.Definition,null});Assert.AreEqual(Prefix+suffix,native.ModelId,"Initial native admission must not strand the current prop in glyph fallback.");
    Assert.AreEqual(at,f.Zone.GetEntityPosition(e));CollectionAssert.AreEquivalent(owners,f.Zone.GetReadOnlyEntities());if(stock!=null){CollectionAssert.AreEqual(stock,e.GetPart<ContainerPart>().Contents);CollectionAssert.AreEqual(count,stock.Select(x=>x.GetPart<StackerPart>()?.StackCount??1));}
    e.GetPart<RenderPart>().Visible=false;f.Refresh();Assert.False(f.Rendered(e));e.GetPart<RenderPart>().Visible=true;f.Refresh();Assert.AreSame(source,Exact(f,e,suffix));f.Zone.RemoveEntity(e);f.Refresh();Assert.False(f.Rendered(e));
   }
  }
  [TestCase(1)][TestCase(2)]
  public void ExactMarkedBodyAndMatchingBayKeepTheirCaseThroughRealHaulAndSavedReplacement(int number)
  {
   using(var f=new SpawnRing3DIntegrationFixture(MarrowstyeCompositionPlan.ZoneID))
   {
    var bodies=f.Zone.GetReadOnlyEntities().Where(e=>e.BlueprintName=="SaltCuredBody"&&e.GetPart("CurationReceivingBody")!=null).ToArray();Assert.AreEqual(2,bodies.Length,"SOURCE PRECONDITION: publish real enrichment/marker before interpreting this art RED.");var body=bodies.Single(e=>Case(e,"CurationReceivingBody")==number);
    var bay=f.Zone.GetReadOnlyEntities().Single(e=>e.BlueprintName=="CurationReceivingBay"&&Case(e,"CurationReceivingBay")==number);Assert.True(Current(body.GetPart("CurationReceivingBody"),f.Zone));Assert.True(Current(bay.GetPart("CurationReceivingBay"),f.Zone));
    var mesh=Exact(f,body,"salt-cured-body-"+number);Exact(f,bay,"receiving-bay-"+number);var other=Exact(f,bodies.Single(e=>e!=body),"salt-cured-body-"+(3-number));Assert.AreNotSame(mesh,other,"Case identity changes the actual form, not just model name.");Assert.False(mesh.vertices.SequenceEqual(other.vertices));
    string id=body.ID;var at=f.Zone.GetEntityPosition(body);Assert.AreEqual(90,body.GetPart<HandlingPart>().Weight);Assert.True(f.Zone.MoveEntity(f.Player,at.x,at.y+1));
    try{Assert.AreEqual(DragVerdict.Ok,DragSystem.TryGrab(f.Player,body,f.Zone));Assert.True(MovementSystem.TryMoveTo(f.Player,f.Zone,at.x,at.y+2));Assert.AreEqual((at.x,at.y+1),f.Zone.GetEntityPosition(body));}
    finally{DragSystem.Release(f.Player);}
    Assert.AreSame(mesh,Exact(f,body,"salt-cured-body-"+number));var loaded=f.RoundTrip();f.BindLoaded(loaded);var replacement=f.Zone.GetReadOnlyEntities().Single(e=>e.ID==id);Assert.AreNotSame(body,replacement);Assert.True(Current(replacement.GetPart("CurationReceivingBody"),f.Zone));Assert.AreSame(mesh,Exact(f,replacement,"salt-cured-body-"+number));Assert.False(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(body,out _));
   }
  }
  [Test]public void UnmarkedSaltCuredBodyRetainsItsExistingRegionalAppearance()
  {using(var f=new SpawnRing3DIntegrationFixture(MarrowstyeCompositionPlan.ZoneID)){var body=f.Add("SaltCuredBody");Assert.Null(body.GetPart("CurationReceivingBody"));StringAssert.StartsWith("marrowstye-cured-",Recipe(f,body).ModelId);}}
  [TestCase("index")][TestCase("case")][TestCase("part-owner")]
  public void DamagedCaseAuthorityCannotBorrowEitherTaggedBody(string fault)
  {
   using(var f=new SpawnRing3DIntegrationFixture(MarrowstyeCompositionPlan.ZoneID))
   {
    var e=f.Zone.GetReadOnlyEntities().FirstOrDefault(x=>x.GetPart("CurationReceivingBody")!=null);Assert.NotNull(e,"SOURCE PRECONDITION: generated marked body.");var p=e.GetPart("CurationReceivingBody");Assert.True(Current(p,f.Zone));var field=p.GetType().GetField(fault=="index"?"Index":"CaseNumber");var old=field.GetValue(p);
    try{if(fault=="part-owner")p.ParentEntity=f.Player;else field.SetValue(p,fault=="index"?(object)f.Player:0);Assert.False(Current(p,f.Zone));Assert.False(Recipe(f,e).ModelId?.StartsWith(Prefix+"salt-cured-body-",StringComparison.Ordinal)==true);}
    finally{field.SetValue(p,old);p.ParentEntity=e;}
   }
  }
  [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)]
  public void NativeDoorOpenCloseSelectsVisibleQuarantineStateAndAxis(int quarter)
  {
   using(var f=new SpawnRing3DIntegrationFixture(MarrowstyeCompositionPlan.ZoneID))
   {
    var gate=Generated(f,"CurationQuarantineGate");var door=gate.GetPart<DoorPart>();Assert.NotNull(door);var locked=gate.GetPart<LockPart>();Assert.NotNull(locked);Assert.True(locked.IsLocked);f.Approach(gate);door.QuarterTurns=quarter;
    var closed=Exact(f,gate,"quarantine-gate-closed");Assert.AreEqual(quarter,Recipe(f,gate).QuarterTurns);Assert.True(f.Zone.GetEntityCell(gate).BlocksMovement(f.Player));
    // A real owned matching key is staged only to exercise the existing lock/door commands.
    var key=f.Factory.CreateEntity("CurationInspectionKey");Assert.NotNull(key);key.GetPart<KeyPart>().KeyId=locked.KeyId;Assert.True(f.Player.GetPart<InventoryPart>().AddObject(key));Assert.True(InventorySystem.PerformAction(f.Player,gate,"Unlock",f.Zone));Assert.False(locked.IsLocked);Assert.True(door.TrySetOpen(f.Player,f.Zone,true));
    var open=Exact(f,gate,"quarantine-gate-open");Assert.AreNotSame(closed,open);Assert.False(closed.vertices.SequenceEqual(open.vertices));Assert.AreEqual(quarter,Recipe(f,gate).QuarterTurns);Assert.False(f.Zone.GetEntityCell(gate).BlocksMovement(f.Player));Assert.True(door.TrySetOpen(f.Player,f.Zone,false));Assert.AreSame(closed,Exact(f,gate,"quarantine-gate-closed"));
   }
  }
  [TestCase("CurationSaltRake","salt-rake")][TestCase("CurationCounterfoil","counterfoil")][TestCase("CurationInspectionKey","inspection-key")]
  [TestCase("CurationTransferDocket","transfer-docket")][TestCase("CurationDiscrepancyReport","discrepancy-report")]
  public void RealPortableOwnerDisappearsOnPickupAndReturnsWithSameSavedIdentity(string bp,string suffix)
  {
   using(var f=new SpawnRing3DIntegrationFixture(MarrowstyeCompositionPlan.ZoneID))
   {
    Assert.True(f.Factory.Blueprints.ContainsKey(bp),"SOURCE PRECONDITION: actual portable content.");var e=f.Add(bp);string id=e.ID;f.Approach(e);var mesh=Exact(f,e,suffix);Assert.True(Recipe(f,e).Transient);Assert.True(InventorySystem.Pickup(f.Player,e,f.Zone));f.Refresh();Assert.False(f.Rendered(e));Assert.False(Recipe(f,e).ModelId?.StartsWith(Prefix,StringComparison.Ordinal)==true);Assert.True(InventorySystem.Drop(f.Player,e,f.Zone));Assert.AreSame(mesh,Exact(f,e,suffix));var loaded=f.RoundTrip();f.BindLoaded(loaded);var current=f.Zone.GetReadOnlyEntities().Single(x=>x.ID==id);Assert.AreNotSame(e,current);Assert.AreSame(mesh,Exact(f,current,suffix));
   }
  }
  [Test]public void SaltRakeUsesOnlyItsActualEquippedOwnerThenClearsOnUnequip()
  {
   using(var f=new SpawnRing3DIntegrationFixture(MarrowstyeCompositionPlan.ZoneID))
   {
    Assert.True(f.Factory.Blueprints.ContainsKey("CurationSaltRake"));var rake=f.Equip(f.Player,"CurationSaltRake");f.Refresh();Assert.True(f.Equipment(f.Player,rake,out var view));Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedEquipmentStyle(f.Player,rake,out var proof),proof.Failure);Assert.AreEqual(Prefix+"salt-rake",proof.ModelId);Assert.AreSame(Resources.Load<GameObject>(Folder+Prefix+"salt-rake").GetComponent<MeshFilter>().sharedMesh,proof.ExpectedMesh);Assert.True(SpawnRing3DIntegrationFixture.Drawn(view));Assert.True(InventorySystem.UnequipItem(f.Player,rake));f.Refresh();Assert.False(f.Equipment(f.Player,rake,out _));SpawnRing3DIntegrationFixture.Hidden(view);
   }
  }
  [TestCase("hidden")][TestCase("glyph")][TestCase("foreign-physics")][TestCase("foreign-spatial")][TestCase("foreign-cell")][TestCase("custom")]
  public void StaleOrChangedPropNeverAcquiresOriginalYardGeometry(string fault)
  {
   using(var f=new SpawnRing3DIntegrationFixture(MarrowstyeCompositionPlan.ZoneID))
   {
    Assert.True(f.Factory.Blueprints.ContainsKey("CurationSaltBench"));var e=f.Add("CurationSaltBench");var p=e.GetPart<PhysicsPart>();var cell=f.Zone.GetEntityCell(e);var priorCell=cell.ParentZone;var spatial=typeof(Entity).GetField("SpatialZone",BindingFlags.Instance|BindingFlags.NonPublic);var prior=spatial.GetValue(e);
    try{if(fault=="hidden")e.GetPart<RenderPart>().Visible=false;if(fault=="glyph")e.GetPart<RenderPart>().RenderString="?";if(fault=="foreign-physics")p.ParentEntity=f.Player;if(fault=="foreign-spatial")spatial.SetValue(e,new Zone(f.Zone.ZoneID));if(fault=="foreign-cell")cell.ParentZone=new Zone(f.Zone.ZoneID);if(fault=="custom")e.GetPart<RenderPart>().VisualID="other";Assert.False(Recipe(f,e).ModelId?.StartsWith(Prefix,StringComparison.Ordinal)==true);}
    finally{p.ParentEntity=e;spatial.SetValue(e,prior);cell.ParentZone=priorCell;}
   }
  }
 }
}
