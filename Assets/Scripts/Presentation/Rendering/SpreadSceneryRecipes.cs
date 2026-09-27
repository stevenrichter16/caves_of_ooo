using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Storylets;
namespace CavesOfOoo.Rendering
{
 /// <summary>Exact missing-scenery appearance/state only. Receiving world and
 /// preservation of native recipe refusals are separate caller obligations.</summary>
 public static class SpreadSceneryRecipes
 {
  private sealed class Definition
  {
   internal readonly string Glyph, Variants;
   internal readonly bool Solid;
   internal readonly Func<Entity, Part> RequiredPart;
   internal readonly string[] Models;
   internal Definition(string name, string glyph, bool solid, string variants, Func<Entity, Part> requiredPart)
   {
    Glyph = glyph; Solid = solid; Variants = variants; RequiredPart = requiredPart;
    string prefix = "spread-scenery-" + name.ToLowerInvariant();
    Models = new[] { prefix + "-0", prefix + "-1", prefix + "-unlit-0", prefix + "-unlit-1" };
   }
  }
  private static readonly Dictionary<string,Definition> Definitions=new Dictionary<string,Definition>(StringComparer.Ordinal)
  {
   {"BerryBush",new Definition("BerryBush",";",false,"",e => e.GetPart<HarvestablePart>())},
   {"Signpost",new Definition("Signpost","I",true,"",e => e.GetPart<RegionalSignpostPart>())},
   {"HollowStump",new Definition("HollowStump","u",true,"",e => e.GetPart<HarvestablePart>())},
   {"Beehive",new Definition("Beehive","6",false,"",e => e.GetPart<HarvestablePart>())},
   {"RiverShrine",new Definition("RiverShrine","_",false,"",e => e.GetPart<SanctuaryPart>())},
   {"FlowerField",new Definition("FlowerField","*",false,"",e => e.GetPart<FlowerCharmPart>())},
   {"StoneFloor",new Definition("StoneFloor",".",false,"...,.'`",null)},
   {"StoneWall",new Definition("StoneWall","#",true,"",null)},
   {"Chair",new Definition("Chair","h",false,"",e => e.GetPart<ChairPart>())},
   {"Bed",new Definition("Bed","=",false,"",e => e.GetPart<BedPart>())},
   {"Well",new Definition("Well","O",true,"",e => e.GetPart<WellPart>())},
   {"Oven",new Definition("Oven","#",true,"",null)},
   {"WatchLantern",new Definition("WatchLantern","!",true,"",e => e.GetPart<LightSourcePart>())},
   {"CampfireGroundMarker",new Definition("CampfireGroundMarker",".",false,"",null)},
   {"WellGroundMarker",new Definition("WellGroundMarker",".",false,"",null)},
   {"OvenGroundMarker",new Definition("OvenGroundMarker",".",false,"",null)},
   {"LanternGroundMarker",new Definition("LanternGroundMarker",".",false,"",null)},
   {"Shrine",new Definition("Shrine","_",false,"",e => e.GetPart<SanctuaryPart>())},
   {"AlchemyShelf",new Definition("AlchemyShelf","n",true,"",e => e.GetPart<ContainerPart>())},
   {"AlchemyStill",new Definition("AlchemyStill","&",false,"",e => e.GetPart<AlchemyStillPart>())},
   {"TinkersForge",new Definition("TinkersForge","n",false,"",e => e.GetPart<ForgePart>())},
   {"OldStump",new Definition("OldStump","o",false,"",e => e.GetPart<QuestMarkerTriggerPart>())},
   {"PressurePlate",new Definition("PressurePlate","^",false,"",e => e.GetPart<PressurePlateTriggerPart>())},
   {"BearTrap",new Definition("BearTrap","^",false,"",e => e.GetPart<BearTrapTriggerPart>())},
   {"FireTrap",new Definition("FireTrap","^",false,"",e => e.GetPart<FireTrapTriggerPart>())},
   {"SpikeTrap",new Definition("SpikeTrap","^",false,"",e => e.GetPart<SpikeTrapTriggerPart>())},
   {"WeaponRack",new Definition("WeaponRack","T",true,"",e => e.GetPart<ContainerPart>())},
  };
  public static bool TryModel(Entity owner,int variant,out string modelId)
  {
   modelId=null;
   if(owner?.BlueprintName==null||variant<0||variant>1||!Definitions.TryGetValue(owner.BlueprintName,out var definition)
      ||owner.HasTag("Creature")||owner.HasTag("Item")||owner.HasPart<SpatialFootprintPart>()||owner.HasPart<MultiCellPilotPropPart>())return false;
   var render=owner.GetPart<RenderPart>();var physics=owner.GetPart<PhysicsPart>();
   if(render==null||!ReferenceEquals(render.ParentEntity,owner)||!render.Visible||render.RenderString!=definition.Glyph
      ||!string.IsNullOrEmpty(render.VisualID)||!string.IsNullOrEmpty(render.VisualVariant)
      ||(render.GlyphVariants??"")!=definition.Variants||physics==null||!ReferenceEquals(physics.ParentEntity,owner)
      ||physics.Takeable||physics.InInventory!=null||physics.Equipped!=null||physics.Solid!=definition.Solid||(!definition.Solid&&owner.HasTag("Solid")))return false;
   if(definition.RequiredPart != null)
   {var part=definition.RequiredPart(owner);if(part==null||!ReferenceEquals(part.ParentEntity,owner))return false;}
   if(owner.GetPart<HarvestablePart>()?.Harvested==true)return false;
   if(owner.BlueprintName=="OldStump")
   {var quest=owner.GetPart<QuestMarkerTriggerPart>();if(quest==null||!ReferenceEquals(quest.ParentEntity,owner)||quest.Fact!="bmo_stump_reached"||quest.Value!=1)return false;}
   int stateOffset = 0;
   if(owner.BlueprintName=="WatchLantern")
   {
    var light=owner.GetPart<LightSourcePart>();
    if(light==null||!ReferenceEquals(light.ParentEntity,owner)||light.Radius<0||float.IsNaN(light.Intensity)||float.IsInfinity(light.Intensity)||light.Intensity<0)return false;
    if(!light.Enabled||light.Radius==0||light.Intensity==0)stateOffset = 2;
   }
   modelId=definition.Models[stateOffset+variant];return true;
  }
 }
}
