using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Storylets;
using CavesOfOoo.Rendering;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadSceneryRecipesTests
 {
  private DensityLootTestScope scope;
  [SetUp]public void Setup(){scope=new DensityLootTestScope();}
  [TearDown]public void Teardown(){scope.Dispose();}
  private Entity Owner(string name)
  {
   if(name!="OldStump")return scope.Factory.CreateEntity(name);
   var e=new Entity{ID="OldStump",BlueprintName="OldStump"};e.AddPart(new RenderPart{RenderString="o",ColorString="&y"});
   e.AddPart(new PhysicsPart{Solid=false});e.AddPart(new QuestMarkerTriggerPart{Fact="bmo_stump_reached",Value=1});return e;
  }
  [TestCase("BerryBush",0)] [TestCase("BerryBush",1)]
  [TestCase("Signpost",0)] [TestCase("Signpost",1)]
  [TestCase("HollowStump",0)] [TestCase("HollowStump",1)]
  [TestCase("Beehive",0)] [TestCase("Beehive",1)]
  [TestCase("RiverShrine",0)] [TestCase("RiverShrine",1)]
  [TestCase("FlowerField",0)] [TestCase("FlowerField",1)]
  [TestCase("StoneFloor",0)] [TestCase("StoneFloor",1)]
  [TestCase("StoneWall",0)] [TestCase("StoneWall",1)]
  [TestCase("Chair",0)] [TestCase("Chair",1)]
  [TestCase("Bed",0)] [TestCase("Bed",1)]
  [TestCase("Well",0)] [TestCase("Well",1)]
  [TestCase("Oven",0)] [TestCase("Oven",1)]
  [TestCase("WatchLantern",0)] [TestCase("WatchLantern",1)]
  [TestCase("CampfireGroundMarker",0)] [TestCase("CampfireGroundMarker",1)]
  [TestCase("WellGroundMarker",0)] [TestCase("WellGroundMarker",1)]
  [TestCase("OvenGroundMarker",0)] [TestCase("OvenGroundMarker",1)]
  [TestCase("LanternGroundMarker",0)] [TestCase("LanternGroundMarker",1)]
  [TestCase("Shrine",0)] [TestCase("Shrine",1)]
  [TestCase("AlchemyShelf",0)] [TestCase("AlchemyShelf",1)]
  [TestCase("AlchemyStill",0)] [TestCase("AlchemyStill",1)]
  [TestCase("TinkersForge",0)] [TestCase("TinkersForge",1)]
  [TestCase("OldStump",0)] [TestCase("OldStump",1)]
  [TestCase("PressurePlate",0)] [TestCase("PressurePlate",1)]
  [TestCase("BearTrap",0)] [TestCase("BearTrap",1)]
  [TestCase("FireTrap",0)] [TestCase("FireTrap",1)]
  [TestCase("SpikeTrap",0)] [TestCase("SpikeTrap",1)]
  [TestCase("WeaponRack",0)] [TestCase("WeaponRack",1)]
  public void ExactNativeStateHasDistinctModelWithoutMutation(string name,int variant)
  {
   var e=Owner(name);var parts=e.Parts.ToArray();var r=e.GetPart<RenderPart>();var ph=e.GetPart<PhysicsPart>();
   string glyph=r.RenderString,color=r.ColorString,id=e.ID;bool solid=ph.Solid;
   Assert.True(SpreadSceneryRecipes.TryModel(e,variant,out var model));
   Assert.AreEqual("spread-scenery-"+name.ToLowerInvariant()+"-"+variant,model);
   Assert.True(SpreadSceneryRecipes.TryModel(e,variant,out var repeated));Assert.AreEqual(model,repeated);
   CollectionAssert.AreEqual(parts,e.Parts);Assert.AreEqual(id,e.ID);Assert.AreEqual(glyph,r.RenderString);Assert.AreEqual(color,r.ColorString);Assert.AreEqual(solid,ph.Solid);
  }
  [TestCase("hidden")][TestCase("portable")][TestCase("carried")][TestCase("equipped")][TestCase("creature")][TestCase("item")]
  [TestCase("glyph")][TestCase("visual")][TestCase("visual-variant")][TestCase("glyph-variants")][TestCase("foreign-render")][TestCase("foreign-physics")]
  public void IdenticalOwnerRejectsChangedAuthorityOrAppearance(string change)
  {
   var e=Owner("Signpost");Assert.True(SpreadSceneryRecipes.TryModel(e,0,out _));var r=e.GetPart<RenderPart>();var p=e.GetPart<PhysicsPart>();
   switch(change){case "hidden":r.Visible=false;break;case "portable":p.Takeable=true;break;case "carried":p.InInventory=new Entity();break;
    case "equipped":p.Equipped=new Entity();break;case "creature":e.Tags["Creature"]="";break;case "item":e.Tags["Item"]="";break;
    case "glyph":r.RenderString="X";break;case "visual":r.VisualID="other";break;case "visual-variant":r.VisualVariant="other";break;
    case "glyph-variants":r.GlyphVariants="XYZ";break;case "foreign-render":r.ParentEntity=new Entity();break;case "foreign-physics":p.ParentEntity=new Entity();break;}
   Assert.False(SpreadSceneryRecipes.TryModel(e,0,out var model));Assert.Null(model);
  }
  [TestCase(-1)][TestCase(2)][TestCase(int.MaxValue)]public void InvalidVariantDoesNotAlias(int variant)
  {Assert.False(SpreadSceneryRecipes.TryModel(Owner("Signpost"),variant,out var model));Assert.Null(model);}
  [TestCase("PhysicalObject")][TestCase("Terrain")][TestCase("Wall")][TestCase("VillageDoor")][TestCase("LiquidFlask")]
  public void UnlistedKnownOwnersRemainUnclaimed(string name)
  {Assert.False(SpreadSceneryRecipes.TryModel(Owner(name),0,out var model));Assert.Null(model);}
  [Test]public void NullOwnerRemainsUnclaimed(){Assert.False(SpreadSceneryRecipes.TryModel(null,0,out var model));Assert.Null(model);}
  [TestCase("fact")][TestCase("value")][TestCase("missing")][TestCase("foreign")]
  public void QuestStumpCannotBorrowAppearanceWithoutExactLiveContract(string change)
  {
   var e=Owner("OldStump");Assert.True(SpreadSceneryRecipes.TryModel(e,0,out _));var q=e.GetPart<QuestMarkerTriggerPart>();
   if(change=="fact")q.Fact="other";else if(change=="value")q.Value=2;else if(change=="foreign")q.ParentEntity=new Entity();else e.RemovePart(q);
   Assert.False(SpreadSceneryRecipes.TryModel(e,0,out _));
  }
  [TestCase("BerryBush")][TestCase("HollowStump")][TestCase("Beehive")]
  public void SpentHarvestOwnerIsNotPresentedAsIntact(string name)
  {var e=Owner(name);Assert.True(SpreadSceneryRecipes.TryModel(e,0,out _));e.GetPart<HarvestablePart>().Harvested=true;Assert.False(SpreadSceneryRecipes.TryModel(e,0,out _));}
  [TestCase(false,10,.6f)][TestCase(true,0,.6f)][TestCase(true,10,0f)]
  public void DisabledOrEmptyActualLightUsesUnlitLantern(bool enabled,int radius,float intensity)
  {
   var e=Owner("WatchLantern");Assert.True(SpreadSceneryRecipes.TryModel(e,1,out var lit));var light=e.GetPart<LightSourcePart>();
   light.Enabled=enabled;light.Radius=radius;light.Intensity=intensity;Assert.True(SpreadSceneryRecipes.TryModel(e,1,out var off));
   Assert.AreEqual("spread-scenery-watchlantern-unlit-1",off);Assert.AreNotEqual(lit,off);Assert.AreEqual(enabled,light.Enabled);
  }
  [Test]public void AuthoredFloorVariantsRemainExact()
  {var e=Owner("StoneFloor");Assert.IsNotEmpty(e.GetPart<RenderPart>().GlyphVariants);Assert.True(SpreadSceneryRecipes.TryModel(e,0,out _));e.GetPart<RenderPart>().GlyphVariants+="X";Assert.False(SpreadSceneryRecipes.TryModel(e,0,out _));}
 }
}
