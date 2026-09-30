using System;
using CavesOfOoo.Core;
namespace CavesOfOoo.Rendering
{
 /// <summary>Original common forms refine only a successful current native
 /// recipe. State, quests, removal, physics and saved ownership stay native.</summary>
 internal static class SpreadEnvironmentRecipes
 {
  private static readonly string[] FloorFamilies={"ring-floor","sumphold-ground","tine-ground","quillhold-ground","marrowstye-ground","tally-ground"};
  private static readonly string[] RoadFamilies={"ring-floor","tine-path","gantry-path","quillhold-path","marrowstye-path","tally-path"};
  internal static SpawnRing3DRecipe Refine(Zone zone,Entity owner,SpawnRing3DRecipe native)
  {
   if(!SpreadPresentationScope.IsActive(zone)||owner==null||!ReferenceEquals(native.Owner,owner)
      ||native.Failure!=null||native.ModelId==null||!native.Batched||native.Transient||native.ComponentId!=null
      ||native.ModelId.StartsWith("reference-glade-",StringComparison.Ordinal)||native.ModelId.StartsWith("spread-scenery-",StringComparison.Ordinal))return native;
   string family=Family(zone,owner,native.ModelId);if(family==null)return native;
   var cell=zone.GetEntityCell(owner);var render=owner.GetPart<RenderPart>();var physics=owner.GetPart<PhysicsPart>();
   if(cell==null||!cell.Objects.Contains(owner)||render==null||!render.Visible||!ReferenceEquals(render.ParentEntity,owner)
      ||physics==null||!ReferenceEquals(physics.ParentEntity,owner)||physics.Takeable||physics.InInventory!=null||physics.Equipped!=null
      ||owner.HasTag("Item")||owner.HasTag("Creature")||owner.HasPart<SpatialFootprintPart>()||owner.HasPart<MultiCellPilotPropPart>()
      ||!string.IsNullOrEmpty(render.VisualID)||!string.IsNullOrEmpty(render.VisualVariant))return native;
   int variant;char last=native.ModelId[native.ModelId.Length-1];
   if(last>='0'&&last<='3')variant=last-'0';
   else{unchecked{uint hash=2166136261;foreach(char c in native.ModelId)hash=(hash^c)*16777619;if(owner.ID!=null)foreach(char c in owner.ID)hash=(hash^c)*16777619;variant=(int)(hash%4);}}
   return new SpawnRing3DRecipe(owner,SpreadEnvironmentSource.ModelId(family,variant),native.ComponentId,native.Position,native.Transient,native.Batched,quarterTurns:native.QuarterTurns);
  }
  // Both initial admission and final refinement use this exact native source.
  internal static string GatheringFamily(Zone zone,Entity owner)
  {
   string family=owner?.BlueprintName=="StoneburrPatch"?"stoneburr":owner?.BlueprintName=="FrostLichenPatch"?"frost-lichen":null;
   if(family==null||!SpreadPresentationScope.IsActive(zone))return null;
   var cell=zone.GetEntityCell(owner);var render=owner.GetPart<RenderPart>();var physics=owner.GetPart<PhysicsPart>();var harvest=owner.GetPart<HarvestablePart>();
   if(cell==null||!ReferenceEquals(owner.SpatialZone,zone)||!ReferenceEquals(cell.ParentZone,zone)||!cell.Objects.Contains(owner)
      ||render==null||!render.Visible||render.ParentEntity!=owner||render.RenderString!=(family=="stoneburr"?"%":"\"")||render.ColorString!=(family=="stoneburr"?"&Y":"&C")
      ||physics==null||physics.ParentEntity!=owner||physics.Takeable||physics.InInventory!=null||physics.Equipped!=null
      ||harvest==null||harvest.ParentEntity!=owner||harvest.Harvested||harvest.YieldBlueprint!=(family=="stoneburr"?"StoneburrSeed":"FrostLichen")
      ||owner.HasTag("Creature")||owner.HasTag("Item")||owner.HasPart<SpatialFootprintPart>()||owner.HasPart<MultiCellPilotPropPart>()
      ||!string.IsNullOrEmpty(render.VisualID)||!string.IsNullOrEmpty(render.VisualVariant)||!string.IsNullOrEmpty(render.GlyphVariants))return null;
   return family;
  }
  private static string Family(Zone zone,Entity owner,string nativeModel)
  {
   switch(owner.BlueprintName){
    case "StoneburrPatch":case "FrostLichenPatch":return In(nativeModel,"ring-bush")?GatheringFamily(zone,owner):null;
    case "Floor":return In(nativeModel,FloorFamilies)?"paving":null;
    case "StoneFloor":return In(nativeModel,"wellmeet-floor")?"paving":null;
    case "RoadStone":return In(nativeModel,RoadFamilies)?"road":null;
    case "Tree":return In(nativeModel,"ring-tree")?"tree":null;
    case "Hedge":return In(nativeModel,"spread-hedge")?"hedge":null;
    case "VineWall":return In(nativeModel,"ring-vine-wall")?"vine-wall":null;
    case "CropRow":return In(nativeModel,"spread-stubble")?"stubble":null;
    case "RipeCropRow":return In(nativeModel,"spread-stubble")?"stubble":In(nativeModel,"spread-barley")?"grain":null;
    case "FlowerField":case "CharmFlowers":return In(nativeModel,"spread-flowers")?"flowers":null;
    case "DryBrush":return In(nativeModel,"ring-dry-brush")?"dry-brush":null;
    case "Rock":return In(nativeModel,"ring-rock")?"rock":null;
    default:return null;
   }
  }
  private static bool In(string model,string[] families)
  {foreach(string family in families)if(In(model,family))return true;return false;}
  private static bool In(string model,string family)
  {
   // Exact known family and single 0..3 variant, without per-owner allocations.
   if(model==null||model.Length!=family.Length+2)return false;char variant=model[model.Length-1];
   return variant>='0'&&variant<='3'&&model[model.Length-2]=='-'&&string.CompareOrdinal(model,0,family,0,family.Length)==0;
  }
 }
}
