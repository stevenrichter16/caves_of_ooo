using System;
using CavesOfOoo.Core;
using UnityEngine;
namespace CavesOfOoo.Rendering
{
 /// <summary>Current saved cultivation/repair owners, in any supported native
 /// biome. Selection never changes terrain, inventory, growth or repaired state.</summary>
 public static class RepairCultivationRecipes
 {
  public const string Prefix="repair-cultivation-",SoilModel=Prefix+"cultivated-soil";
  public static bool Handles(string bp)
  {
   switch(bp){case "KnotflaxCrop":case "HearthbulbCrop":case "SeamleafCrop":case "RepairLinedWell":case "RepairRopeWell":case "RepairWoodenGate":case "RepairClayBank":case "RepairTimberPile":case "RepairCordBundle":case "KnotflaxSeed":case "HearthbulbSeed":case "SeamleafSeed":case "KnotflaxCord":case "Hearthbulb":case "RoastedHearthbulb":case "SeamleafSprig":case "SalvagedTimber":return true;default:return false;}
  }
  static bool Current(Zone zone,Entity e,bool retainNativeTerrain=false)
  {
   if(zone==null||e==null)return false;
   var c=zone?.GetEntityCell(e);var p=e?.GetPart<PhysicsPart>();var r=e?.GetPart<RenderPart>();
   if(c==null||c.ParentZone!=zone||e.SpatialZone!=zone||!c.Objects.Contains(e)||!AreaCompositionScope.Allows(zone)
    ||p==null||p.ParentEntity!=e||p.InInventory!=null||p.Equipped!=null||r==null||r.ParentEntity!=e||!r.Visible
    ||e.HasTag("Creature")||e.HasPart<SpatialFootprintPart>()||e.HasPart<MultiCellPilotPropPart>()
    ||!retainNativeTerrain&&(!string.IsNullOrEmpty(r.VisualID)||!string.IsNullOrEmpty(r.VisualVariant)||!string.IsNullOrEmpty(r.GlyphVariants)))return false;
   var manager=WorldLocationContext.For(zone);return manager==null||manager.CachedZones.TryGetValue(zone.ZoneID,out var live)&&live==zone;
  }
  static bool Appearance(Entity e,string glyph,string color)
  {var r=e.GetPart<RenderPart>();return r.RenderString==glyph&&r.ColorString==color;}
  public static bool HasCultivatedSoil(Zone zone,Entity terrain)
  {
   if(terrain==null||terrain.GetPart<CultivatedSoilPart>()?.ParentEntity!=terrain||!Current(zone,terrain,true)||!terrain.HasTag("Terrain")||!terrain.HasTag("Plantable"))return false;
   var p=terrain.GetPart<PhysicsPart>();return !p.Solid&&!p.Takeable&&!terrain.HasTag("Solid")&&!terrain.HasTag("Item")&&CultivatedSoilPart.IsCultivated(zone,zone.GetEntityCell(terrain));
  }
  public static string ResolveModel(Zone zone,Entity e)
  {
   if(e==null||!Handles(e.BlueprintName)||!Current(zone,e))return null;
   var p=e.GetPart<PhysicsPart>();var d=e.GetPart<DestructiblePart>();if(d!=null&&(d.ParentEntity!=e||d.Gone||d.HP<=0))return null;
   string bp=e.BlueprintName;
   if(bp=="KnotflaxCrop"||bp=="HearthbulbCrop"||bp=="SeamleafCrop")
   {
    var crop=e.GetPart<CropPart>();string family=bp=="KnotflaxCrop"?"knotflax":bp=="HearthbulbCrop"?"hearthbulb":"seamleaf";
    string glyphs=family=="knotflax"?".,;,#":family=="hearthbulb"?".,;,%":".,;,\"",colors=family=="knotflax"?"&w,&g,&C":family=="hearthbulb"?"&w,&g,&Y":"&w,&g,&G";
    if(crop==null||crop.ParentEntity!=e||!crop.HarvestAtMaturity||crop.GrowthStage<0||crop.GrowthStage>2||crop.StageGlyphsRaw!=glyphs||crop.StageColorsRaw!=colors
     ||p.Takeable||p.Solid||!e.HasTag("Crop")||e.HasTag("Item")||!Appearance(e,crop.GlyphForStage(crop.GrowthStage).ToString(),crop.ColorForStage(crop.GrowthStage)))return null;
    return Prefix+family+"-"+crop.GrowthStage+(crop.MoistureTicks>0?"-wet":"-dry");
   }
   if(bp=="RepairLinedWell"||bp=="RepairRopeWell"||bp=="RepairWoodenGate")
   {
    var repair=e.GetPart<RepairablePart>();var composition=e.GetPart<CompositionPart>();
    string fault=bp=="RepairLinedWell"?"clay-well-lining":bp=="RepairRopeWell"?"rope-well-line":"timber-gate-frame";
    var recipe=RepairRecipeRegistry.Get(fault);
    if(p.Takeable||e.HasTag("Item")||repair?.ParentEntity!=e||repair.RecipeId!=fault||composition?.ParentEntity!=e||recipe==null||!composition.Contains(recipe.Composition))return null;
    if(bp=="RepairWoodenGate")
    {
     var door=e.GetPart<DoorPart>();if(door?.ParentEntity!=e||door.QuarterTurns<0||door.QuarterTurns>3||p.Solid||!Appearance(e,door.IsClosed?"+":"/","&y"))return null;
     return Prefix+"wooden-gate-"+(!repair.Repaired?"broken":door.IsClosed?"closed":"open");
    }
    if(e.GetPart<WellPart>()?.ParentEntity!=e||!Appearance(e,"O","&c"))return null;
    return Prefix+(bp=="RepairLinedWell"?"lined-well-":"rope-well-")+(repair.Repaired?"restored":"broken");
   }
   if(bp=="RepairClayBank"||bp=="RepairTimberPile"||bp=="RepairCordBundle")
   {
    var h=e.GetPart<HarvestablePart>();string family=bp=="RepairClayBank"?"clay-bank":bp=="RepairTimberPile"?"timber-pile":"cord-bundle";
    if(p.Takeable||e.HasTag("Item")||h?.ParentEntity!=e||h.Harvested||!Appearance(e,bp=="RepairTimberPile"?"=":"~",bp=="RepairClayBank"?"&R":bp=="RepairTimberPile"?"&y":"&w"))return null;
    return Prefix+family;
   }
   if(!p.Takeable||!e.HasTag("Item")||e.HasTag("Natural"))return null;
   var stack=e.GetPart<StackerPart>();if(stack!=null&&(stack.ParentEntity!=e||stack.StackCount<=0))return null;
   string model=null,glyph=null,color=null;
   switch(bp)
   {
    case "KnotflaxSeed":model="knotflax-seed";glyph=",";color="&w";break;
    case "HearthbulbSeed":model="hearthbulb-seed";glyph=",";color="&w";break;
    case "SeamleafSeed":model="seamleaf-seed";glyph=",";color="&w";break;
    case "KnotflaxCord":model="knotflax-cord";glyph="~";color="&w";break;
    case "Hearthbulb":model="hearthbulb";glyph="%";color="&Y";break;
    case "RoastedHearthbulb":model="roasted-hearthbulb";glyph="%";color="&o";break;
    case "SeamleafSprig":model="seamleaf-sprig";glyph=";";color="&G";break;
    case "SalvagedTimber":model="salvaged-timber";glyph="=";color="&y";break;
   }
   return model!=null&&Appearance(e,glyph,color)?Prefix+model:null;
  }
  internal static SpawnRing3DRecipe Recipe(Zone zone,Entity owner,string model)
  {
   var cell=zone.GetEntityCell(owner);bool portable=owner.GetPart<PhysicsPart>().Takeable;var door=owner.GetPart<DoorPart>();
   return new SpawnRing3DRecipe(owner,model,null,Village3DProjection.CellCentre(cell.X,cell.Y),portable,!portable&&door==null,quarterTurns:door?.QuarterTurns??0);
  }
 }
}
