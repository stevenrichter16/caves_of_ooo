using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Storylets;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadPosyQuestVisualTests
 {
  const string Home="Overworld.5.9.0";
  static Entity Role(SpawnRing3DIntegrationFixture f,string conversation)
  {
   var owner=f.Zone.GetReadOnlyEntities().Single(e=>e.BlueprintName=="Villager"&&e.GetPart<ConversationPart>()?.ConversationID==conversation);
   var render=owner.GetPart<RenderPart>();Assert.NotNull(render);Assert.AreSame(owner,render.ParentEntity);
   Assert.AreSame(owner,owner.GetPart<ConversationPart>().ParentEntity);Assert.AreEqual(Home,owner.GetProperty("SettlementId"));
   bool baker=conversation=="Baker_Quest";Assert.AreEqual(baker?"b":"h",render.RenderString);Assert.AreEqual(baker?"&w":"&K",render.ColorString);
   var beacon=owner.GetPart<QuestBeaconPart>();if(baker){Assert.NotNull(beacon);Assert.AreSame(owner,beacon.ParentEntity);Assert.AreEqual("MessageForHermit",beacon.Quest);}else Assert.IsNull(beacon);
   return owner;
  }
  static void Approved(SpawnRing3DIntegrationFixture f,Entity owner)
  {
   f.Set("FullReveal",true);f.Refresh();var recipe=SpawnRing3DRecipes.Resolve(f.Zone,owner,f.Library.Definition);
   Assert.AreEqual(SpreadBiomeHumanoidLibrary.ModelId("Villager"),recipe.ModelId,recipe.Failure);
   Assert.True(f.Find(owner,out var body,out var id));Assert.AreEqual(recipe.ModelId,id);Assert.True(f.Rendered(owner));
   Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(owner,out var proof),proof.Failure);
   Assert.AreSame(SpreadBiomeHumanoidLibrary.Load().Find(id).Mesh,proof.SubmittedMesh);
   var animator=body.GetComponentInChildren<Animator>(true);Assert.NotNull(animator);Assert.NotNull(animator.runtimeAnimatorController);
   foreach(string clip in new[]{"Idle","Walk","Interact","Attack","Hit"})Assert.True(animator.runtimeAnimatorController.animationClips.Any(c=>c!=null&&c.name==clip));
  }
  static void Refused(SpawnRing3DIntegrationFixture f,Entity owner)
   =>Assert.AreNotEqual(SpreadBiomeHumanoidLibrary.ModelId("Villager"),SpawnRing3DRecipes.Resolve(f.Zone,owner,f.Library.Definition).ModelId);
  [TestCase(64,"Baker_Quest")][TestCase(64,"Hermit_Quest")][TestCase(1,"Baker_Quest")][TestCase(1,"Hermit_Quest")][TestCase(1729,"Baker_Quest")][TestCase(1729,"Hermit_Quest")]
  public void ActualCanonicalPosyQuestOwnersKeepAnApprovedAnimatedBody(int seed,string conversation)
  {
   using(var f=new SpawnRing3DIntegrationFixture(Home))
   {
    f.Manager=OverworldZoneManager.CreateDetached(f.Factory,seed);f.Zone=f.Manager.GetZone(Home);f.Manager.SetActiveZone(f.Zone);f.Reveal();f.Bind(f.Zone);
    var owner=Role(f,conversation);var pos=f.Zone.GetEntityPosition(owner);int version=f.Zone.EntityVersion;string tiles=f.Zone.TileState.ToSaveString();
    string id=owner.ID;Approved(f,owner);Assert.AreSame(owner,Role(f,conversation));Assert.AreEqual(id,owner.ID);Assert.AreEqual(pos,f.Zone.GetEntityPosition(owner));Assert.AreEqual(version,f.Zone.EntityVersion);Assert.AreEqual(tiles,f.Zone.TileState.ToSaveString());
   }
  }
  [TestCase("glyph")][TestCase("color")][TestCase("conversation")][TestCase("foreign-conversation")]
  [TestCase("render-owner")][TestCase("visual")][TestCase("portable")][TestCase("home")]
  public void EachCurrentRoleRequiresItsCompleteAppearanceAndNativeOwnerContract(string mutation)
  {
   using(var f=new SpawnRing3DIntegrationFixture(Home))foreach(string role in new[]{"Baker_Quest","Hermit_Quest"})
   {
    var owner=Role(f,role);var r=owner.GetPart<RenderPart>();var c=owner.GetPart<ConversationPart>();
    switch(mutation){case "glyph":r.RenderString="?";break;case "color":r.ColorString="&Y";break;case "conversation":c.ConversationID="Unrelated";break;case "foreign-conversation":c.ParentEntity=f.Player;break;case "render-owner":r.ParentEntity=f.Player;break;case "visual":r.VisualID="foreign-appearance";break;case "portable":owner.GetPart<PhysicsPart>().Takeable=true;break;case "home":owner.Properties["SettlementId"]="Overworld.10.10.0";break;}
    Refused(f,owner);
   }
  }
  [TestCase("missing")][TestCase("wrong")][TestCase("foreign")]
  public void BakerNeedsItsActualMessageQuestBeacon(string mutation)
  {
   using(var f=new SpawnRing3DIntegrationFixture(Home)){var owner=Role(f,"Baker_Quest");var beacon=owner.GetPart<QuestBeaconPart>();if(mutation=="missing")Assert.True(owner.RemovePart(beacon));else if(mutation=="wrong")beacon.Quest="RootBeerGuyCase";else beacon.ParentEntity=f.Player;Refused(f,owner);}
  }
  [Test]public void HermitDoesNotMasqueradeAsAnUnrelatedQuestGiver()
  {using(var f=new SpawnRing3DIntegrationFixture(Home)){var owner=Role(f,"Hermit_Quest");owner.AddPart(new QuestBeaconPart{Quest="RootBeerGuyCase"});Refused(f,owner);}}
  [Test]public void RealQuestOwnersRetainTheirAppearanceWhenTransferredToAnotherApprovedSpreadZone()
  {
   using(var f=new SpawnRing3DIntegrationFixture(Home))
   {
    var owners=new[]{Role(f,"Baker_Quest"),Role(f,"Hermit_Quest")};var original=f.Zone;var destination=f.Manager.GetZone("Overworld.12.10.0");
    foreach(var owner in owners)original.RemoveEntity(owner);f.Zone=destination;f.Manager.SetActiveZone(destination);
    foreach(var owner in owners){var cell=f.FreeCell();Assert.True(destination.AddEntity(owner,cell.x,cell.y));}
    f.Reveal();f.Bind(destination);foreach(var owner in owners){Assert.AreEqual(Home,owner.GetProperty("SettlementId"));Approved(f,owner);}
   }
  }
  [Test]public void HiddenCurrentOwnersCannotAcquireVisibleBodyClaims()
  {using(var f=new SpawnRing3DIntegrationFixture(Home))foreach(string role in new[]{"Baker_Quest","Hermit_Quest"}){var owner=Role(f,role);owner.GetPart<RenderPart>().Visible=false;Refused(f,owner);}}
  [Test]public void ForeignReceivingAuthorityCannotAcquireTheScopedAppearance()
  {using(var f=new SpawnRing3DIntegrationFixture(Home)){var a=Role(f,"Baker_Quest");var b=Role(f,"Hermit_Quest");f.Manager.WorldMap.Tiles[5,9]=BiomeType.Sodden;Refused(f,a);Refused(f,b);}}
  [Test]public void DisplayNameAndColorsAloneNeverGrantQuestAppearance()
  {using(var f=new SpawnRing3DIntegrationFixture(Home)){var owner=f.Add("Villager");owner.Properties["SettlementId"]=Home;var r=owner.GetPart<RenderPart>();r.DisplayName="the hermit";r.RenderString="h";r.ColorString="&K";Refused(f,owner);}}
  [TestCase("reskinned-native-actor")][TestCase("not-current-zone-member")]
  public void EarlierNamedNativeRefusalsRemainAuthoritative(string reason)
  {
   using(var f=new SpawnRing3DIntegrationFixture(Home))
   {
    var owner=Role(f,"Baker_Quest");var prior=new SpawnRing3DRecipe(owner,null,"native-contract",new Vector3(3,4,5),false,true,reason,2);
    var m=typeof(SpreadBiomeHumanoidLibrary).GetMethod("Refine",BindingFlags.Static|BindingFlags.NonPublic);Assert.NotNull(m);
    var after=(SpawnRing3DRecipe)m.Invoke(null,new object[]{f.Zone,owner,prior});Assert.AreEqual(prior.Failure,after.Failure);Assert.AreEqual(prior.ModelId,after.ModelId);Assert.AreEqual(prior.Position,after.Position);Assert.AreEqual(prior.ComponentId,after.ComponentId);Assert.AreEqual(prior.QuarterTurns,after.QuarterTurns);
   }
  }
 }
}
