using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Rendering;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadEquipmentRecipesTests
 {
  private DensityLootTestScope scope;
  private Entity actor;
  [SetUp] public void Setup(){scope=new DensityLootTestScope();actor=scope.Factory.CreateEntity("Player");foreach(var e in actor.GetPart<InventoryPart>().GetAllEquipped().ToArray())Assert.True(InventorySystem.UnequipItem(actor,e));}
  [TearDown] public void Teardown(){scope.Dispose();}
  private Entity Equip(string name){var e=scope.Factory.CreateEntity(name);Assert.NotNull(e);Assert.True(actor.GetPart<InventoryPart>().AddObject(e));Assert.True(InventorySystem.Equip(actor,e));return e;}
  [TestCase("Dagger","Hand")]
  [TestCase("ForgedWeapon","Hand")]
  [TestCase("LeatherArmor","Body")]
  [TestCase("ChainMail","Body")]
  [TestCase("LongSword","Hand")]
  [TestCase("Battleaxe","Hand")]
  [TestCase("Greatsword","Hand")]
  [TestCase("ShortSword","Hand")]
  [TestCase("Mace","Hand")]
  [TestCase("Spear","Hand")]
  [TestCase("Hatchet","Hand")]
  [TestCase("Claymore","Hand")]
  [TestCase("Cudgel","Hand")]
  [TestCase("Buckler","Hand")]
  [TestCase("IronHelmet","Head")]
  [TestCase("LeatherBoots","Feet")]
  [TestCase("LeatherGloves","Handwear")]
  [TestCase("LeatherCap","Head")]
  [TestCase("IronshodBoots","Feet")]
  [TestCase("WardedCloak","Back")]
  [TestCase("IronBuckler","Hand")]
  [TestCase("PlateArmor","Body")]
  [TestCase("Cloak","Back")]
  [TestCase("LoanerDagger","Hand")]
  [TestCase("LoanerSpear","Hand")]
  [TestCase("LoanerLongsword","Hand")]
  [TestCase("Warhammer","Hand")]
  [TestCase("ChoirSpine","Hand")]
  [TestCase("OldWorldPipe","Hand")]
  [TestCase("Sporeblade","Hand")]
  [TestCase("FlamingSword","Hand")]
  [TestCase("IceSword","Hand")]
  [TestCase("CryoLance","Hand")]
  [TestCase("EmberSpear","Hand")]
  [TestCase("AcidicDagger","Hand")]
  [TestCase("VenomDagger","Hand")]
  [TestCase("ThunderHammer","Hand")]
  [TestCase("EchoKnife","Hand")]
  [TestCase("TemporalShard","Hand")]
  [TestCase("SeveranceEdge","Hand")]
  [TestCase("GlassblownStiletto","Hand")]
  [TestCase("DissolutionMaul","Hand")]
  [TestCase("FirstRootGlaive","Hand")]
  [TestCase("PalimpsestBlade","Hand")]
  [TestCase("BreacherCleaver","Hand")]
  [TestCase("Torch","Hand")]
  [TestCase("TemperedLongSword","Hand")]
  [TestCase("CounterweightMaul","Hand")]
  [TestCase("FineRingMail","Body")]
  [TestCase("RivetedPlate","Body")]
  public void EveryConcreteEquippableHasItsOwnExactStateRecipe(string name,string slot)
  {
   var item=Equip(name);int version=EquipmentChangeBus.GlobalVersion;var native=actor.GetPart<InventoryPart>().EquippedItems.ToArray();
   Assert.True(SpreadEquipmentRecipes.TryRecipe(actor,item,out var recipe),name);
   Assert.AreEqual(slot,recipe.Slot);Assert.AreEqual(slot=="Feet"||slot=="Handwear"?2:1,recipe.Pieces);
   Assert.AreEqual((slot=="Hand"?"spread-portable-":"spread-worn-")+name.ToLowerInvariant(),recipe.ModelId);
   Assert.AreEqual(version,EquipmentChangeBus.GlobalVersion);CollectionAssert.AreEqual(native,actor.GetPart<InventoryPart>().EquippedItems);
  }
  [TestCase("carried")][TestCase("foreign-physics")][TestCase("foreign-equip")][TestCase("foreign-inventory")][TestCase("foreign-body")]
  [TestCase("wrong-equipped-owner")][TestCase("double-owner")][TestCase("missing-cache")][TestCase("missing-body-slot")][TestCase("missing-first-slot")]
  [TestCase("wrong-slot")][TestCase("unknown")][TestCase("natural")][TestCase("creature")][TestCase("foreign-render")][TestCase("hidden")][TestCase("visual-override")][TestCase("foreign-weapon")]
  public void InvalidNativeGraphRefusesWithoutRepair(string mode)
  {
   var item=Equip("Dagger");var inv=actor.GetPart<InventoryPart>();var body=actor.GetPart<Body>();var physics=item.GetPart<PhysicsPart>();var part=inv.FindEquippedBodyPart(item);
   switch(mode){
    case "carried":Assert.True(InventorySystem.UnequipItem(actor,item));break;
    case "foreign-physics":physics.ParentEntity=new Entity();break;
    case "foreign-equip":item.GetPart<EquippablePart>().ParentEntity=new Entity();break;
    case "foreign-inventory":inv.ParentEntity=new Entity();break;
    case "foreign-body":body.ParentEntity=new Entity();break;
    case "wrong-equipped-owner":physics.Equipped=new Entity();break;
    case "double-owner":physics.InInventory=actor;break;
    case "missing-cache":inv.EquippedItems.Clear();break;
    case "missing-body-slot":part._Equipped=null;break;
    case "missing-first-slot":part.FirstSlotForEquipped=false;break;
    case "wrong-slot":part.Type="Head";break;
    case "unknown":item.BlueprintName="BorrowedFakeDagger";break;
    case "natural":item.SetTag("Natural");break;
    case "creature":item.SetTag("Creature");break;
    case "foreign-render":item.GetPart<RenderPart>().ParentEntity=new Entity();break;
    case "hidden":item.GetPart<RenderPart>().Visible=false;break;
    case "visual-override":item.GetPart<RenderPart>().VisualID="foreign";break;
    case "foreign-weapon":item.GetPart<MeleeWeaponPart>().ParentEntity=new Entity();break;
   }
   var slots=inv.EquippedItems.ToArray();int version=EquipmentChangeBus.GlobalVersion;
   Assert.False(SpreadEquipmentRecipes.TryRecipe(actor,item,out _),mode);Assert.AreEqual(version,EquipmentChangeBus.GlobalVersion);CollectionAssert.AreEqual(slots,inv.EquippedItems);
  }
  [Test] public void CurrentForgedAssemblyChangesHeldModelAndRefusesForeignPart()
  {
   var item=Equip("ForgedWeapon");var part=new WeaponAssemblyPart{BladeBlueprint="SteelBladeComponent",HaftBlueprint="OakHaftComponent",BindingBlueprint="LeatherBindingComponent"};item.AddPart(part);
   Assert.True(SpreadEquipmentRecipes.TryRecipe(actor,item,out var first));Assert.AreEqual("spread-portable-forged-blade-oak-leather",first.ModelId);
   part.BladeBlueprint="IronSpikeComponent";Assert.True(SpreadEquipmentRecipes.TryRecipe(actor,item,out var next));Assert.AreEqual("spread-portable-forged-spike-oak-leather",next.ModelId);
   part.ParentEntity=new Entity();Assert.False(SpreadEquipmentRecipes.TryRecipe(actor,item,out _));
  }
  [Test] public void TwoHandedEquipmentStillHasOneLogicalRecipe()
  {var item=Equip("Greatsword");Assert.AreEqual(2,actor.GetPart<InventoryPart>().EquippedItems.Values.Count(e=>ReferenceEquals(e,item)));Assert.True(SpreadEquipmentRecipes.TryRecipe(actor,item,out var recipe));Assert.AreEqual(1,recipe.Pieces);}
  [Test] public void UnequipReequipSameNativeIdentityRevalidatesCurrentOwner()
  {var item=Equip("Dagger");Assert.True(SpreadEquipmentRecipes.TryRecipe(actor,item,out _));Assert.True(InventorySystem.UnequipItem(actor,item));Assert.False(SpreadEquipmentRecipes.TryRecipe(actor,item,out _));Assert.True(InventorySystem.Equip(actor,item));Assert.True(SpreadEquipmentRecipes.TryRecipe(actor,item,out _));}
  [Test] public void NullInputsHaveNoRecipe(){Assert.False(SpreadEquipmentRecipes.TryRecipe(null,null,out _));Assert.False(SpreadEquipmentRecipes.TryRecipe(actor,null,out _));}
  [Test] public void IndividualLateralFootCannotClaimAHumanoidBootPair()
  {
   actor.GetPart<Body>().SetBody(AnatomyFactory.CreateQuadruped());var item=Equip("LeatherBoots");
   var slot=actor.GetPart<InventoryPart>().FindEquippedBodyPart(item);Assert.AreNotEqual(Laterality.NONE,slot.GetLaterality());
   Assert.False(SpreadEquipmentRecipes.TryRecipe(actor,item,out _));Assert.AreSame(actor,item.GetPart<PhysicsPart>().Equipped);
  }
  [Test] public void SameItemNativeHandRelocationChangesAttachmentIdentity()
  {
   var item=Equip("Dagger");Assert.True(SpreadEquipmentRecipes.TryRecipe(actor,item,out var before));
   var first=actor.GetPart<InventoryPart>().FindEquippedBodyPart(item);var other=actor.GetPart<Body>().GetParts().Single(x=>x.Type=="Hand"&&!ReferenceEquals(x,first));
   Assert.True(InventorySystem.UnequipItem(actor,item));Assert.True(InventorySystem.Equip(actor,item,other));
   Assert.True(SpreadEquipmentRecipes.TryRecipe(actor,item,out var after));Assert.AreNotEqual(before.AttachmentKey,after.AttachmentKey);
  }
 }
}
