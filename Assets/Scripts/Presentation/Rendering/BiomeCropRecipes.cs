using System;
using CavesOfOoo.Core;
namespace CavesOfOoo.Rendering
{
 /// <summary>Closed catalogue-driven botany. Reads the exact current entity and
 /// its saved plant state; imported seeds keep their form outside their origin biome.</summary>
 public static class BiomeCropRecipes
 {
  public const string Prefix="biome-crop-";
  /// <summary>Only the current graph of an ordinary receiving-world cave may
  /// widen the native renderer. Address strings and detached clones gain nothing.</summary>
  public static bool IsOrdinaryCave(Zone zone)
  {
   if(zone==null)return false;var manager=WorldLocationContext.For(zone);
   return manager!=null&&manager.CachedZones.TryGetValue(zone.ZoneID,out var current)&&current==zone
    &&BiomeCropPlan.IsOrdinaryCave(manager,zone.ZoneID);
  }
  public static bool Handles(string blueprint)=>BiomeCropCatalog.ByBlueprint(blueprint)!=null;
  public static string ResolveModel(Zone zone,Entity entity)
  {
   var row=entity==null?null:BiomeCropCatalog.ByBlueprint(entity.BlueprintName);
   if(zone==null||row==null||!AreaCompositionScope.Allows(zone))return null;
   var cell=zone.GetEntityCell(entity);var p=entity.GetPart<PhysicsPart>();var r=entity.GetPart<RenderPart>();
   if(cell==null||cell.ParentZone!=zone||entity.SpatialZone!=zone||!cell.Objects.Contains(entity)
    ||p?.ParentEntity!=entity||p.InInventory!=null||p.Equipped!=null||r?.ParentEntity!=entity||!r.Visible
    ||entity.HasTag("Creature")||entity.HasTag("Natural")||entity.HasPart<SpatialFootprintPart>()||entity.HasPart<MultiCellPilotPropPart>()
    ||!string.IsNullOrEmpty(r.VisualID)||!string.IsNullOrEmpty(r.VisualVariant)||!string.IsNullOrEmpty(r.GlyphVariants))return null;
   var manager=WorldLocationContext.For(zone);if(manager!=null&&(!manager.CachedZones.TryGetValue(zone.ZoneID,out var current)||current!=zone))return null;
   var destroyed=entity.GetPart<DestructiblePart>();if(destroyed!=null&&(destroyed.ParentEntity!=entity||destroyed.Gone||destroyed.HP<=0))return null;
   string prefix=Prefix+row.ModelStem;
   if(entity.BlueprintName==row.CropBlueprint)
   {
    var crop=entity.GetPart<CropPart>();
    if(crop?.ParentEntity!=entity||!crop.HarvestAtMaturity||crop.GrowthStage<0||crop.GrowthStage>2
     ||crop.StageGlyphsRaw!=row.StageGlyphsRaw||crop.StageColorsRaw!=row.StageColorsRaw
     ||crop.YieldBlueprint!=row.YieldBlueprint||crop.SeedYieldBlueprint!=row.SeedBlueprint
     ||p.Takeable||p.Solid||!entity.HasTag("Crop")||entity.HasTag("Item")
     ||r.RenderString!=crop.GlyphForStage(crop.GrowthStage).ToString()||r.ColorString!=crop.ColorForStage(crop.GrowthStage))return null;
    return prefix+"-"+crop.GrowthStage+(crop.MoistureTicks>0?"-wet":"-dry");
   }
   if(!p.Takeable||p.Solid||!entity.HasTag("Item")||entity.HasTag("Crop"))return null;
   var stack=entity.GetPart<StackerPart>();if(stack!=null&&(stack.ParentEntity!=entity||stack.StackCount<=0))return null;
   if(entity.BlueprintName==row.SeedBlueprint)
   {
    var seed=entity.GetPart<SeedPart>();if(seed?.ParentEntity!=entity||seed.CropBlueprint!=row.CropBlueprint||!seed.RequireCultivatedSoil)return null;
    return prefix+"-seed";
   }
   return entity.BlueprintName==row.YieldBlueprint?prefix+"-harvest":null;
  }
  internal static SpawnRing3DRecipe Recipe(Zone zone,Entity owner,string model)
  {var cell=zone.GetEntityCell(owner);bool portable=owner.GetPart<PhysicsPart>().Takeable;return new SpawnRing3DRecipe(owner,model,null,Village3DProjection.CellCentre(cell.X,cell.Y),portable,!portable);}
 }
}
