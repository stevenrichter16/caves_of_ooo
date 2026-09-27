using System;
using CavesOfOoo.Core;
namespace CavesOfOoo.Rendering
{
 /// <summary>Approved common scenery only after native appearance/state
 /// resolution succeeds. No failed quest/scene owner or native layout is repaired.</summary>
 internal static class SpreadCommonTerrain
 {
  internal static SpawnRing3DRecipe Refine(Zone zone,Entity owner,SpawnRing3DRecipe native)
  {
   if(!SpreadPresentationScope.IsActive(zone)||owner==null||native.ModelId==null||native.Failure!=null
      ||!ReferenceEquals(native.Owner,owner)||!native.Batched||native.Transient||native.ComponentId!=null
      ||native.ModelId.StartsWith("reference-glade-",StringComparison.Ordinal))return native;
   string family=Family(owner.BlueprintName);if(family==null)return native;
   var cell=zone.GetEntityCell(owner);var render=owner.GetPart<RenderPart>();var physics=owner.GetPart<PhysicsPart>();
   if(cell==null||!cell.Objects.Contains(owner)||render==null||!ReferenceEquals(render.ParentEntity,owner)||!render.Visible
      ||physics==null||!ReferenceEquals(physics.ParentEntity,owner)||physics.Takeable||physics.InInventory!=null||physics.Equipped!=null
      ||owner.HasTag("Item")||owner.HasTag("Creature")||owner.HasPart<SpatialFootprintPart>()||owner.HasPart<MultiCellPilotPropPart>()
      ||!string.IsNullOrEmpty(render.VisualID)||!string.IsNullOrEmpty(render.VisualVariant))return native;
   // Preserve stable native model variation where it exists. Single-body source
   // forms acquire deterministic art variation without consuming simulation RNG.
   int variant;char suffix=native.ModelId[native.ModelId.Length-1];
   if(suffix>='0'&&suffix<='3')variant=suffix-'0';
   else
   {
    unchecked{uint value=2166136261;foreach(char c in native.ModelId)value=(value^c)*16777619;
     if(owner.ID!=null)foreach(char c in owner.ID)value=(value^c)*16777619;
     value=(value^(uint)cell.X)*16777619;value=(value^(uint)cell.Y)*16777619;variant=(int)(value%4);}
   }
   return new SpawnRing3DRecipe(owner,ReferenceGladeVoxelLibrary.ModelId(family,variant),native.ComponentId,native.Position,native.Transient,native.Batched,quarterTurns:native.QuarterTurns);
  }
  private static string Family(string blueprint)
  {
   switch(blueprint)
   {
    case "Grass":return "ground";case "Reeds":return "pale-reeds";case "Bush":return "green-grass";
    case "Wall":return "low-wall";case "Rubble":return "gravel";case "BrokenColumn":return "dark-ruin";
    case "Chest":return "chest";case "WoodenBarrel":return "barrel";case "MushroomRing":return "mushroom-ring";
    default:return null;
   }
  }
 }
}
