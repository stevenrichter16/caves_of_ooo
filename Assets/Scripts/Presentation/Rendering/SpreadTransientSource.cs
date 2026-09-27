using System;
using CavesOfOoo.Core;
namespace CavesOfOoo.Rendering
{
 /// <summary>Read-only current native source for scoped ephemeral geometry.
 /// Amount is the exact simulation value; Band uses the existing gas shade
 /// thresholds. A sample never owns, changes or extends its source lifetime.</summary>
 public readonly struct SpreadTransientSample
 {
  public readonly Entity Owner;
  public readonly string Kind,GasType,Color;
  public readonly int X,Y,Amount,Band;
  internal SpreadTransientSample(Entity owner,string kind,string gasType,string color,int x,int y,int amount,int band)
  {Owner=owner;Kind=kind;GasType=gasType;Color=color;X=x;Y=y;Amount=amount;Band=band;}
 }
 public static class SpreadTransientSource
 {
  /// <summary>Exact live standard GasFactory owner only. Malformed/custom owners
  /// retain their existing fallback; no definition or RenderPart is repaired.</summary>
  public static bool TryGas(Zone zone,Entity owner,out SpreadTransientSample sample)
  {
   sample=default;
   if(owner==null||!SpreadPresentationScope.IsActive(zone))return false;
   var cell=zone.GetEntityCell(owner);var gas=owner.GetPart<GasPoolPart>();var render=owner.GetPart<RenderPart>();var physics=owner.GetPart<PhysicsPart>();
   if(cell==null||!cell.Objects.Contains(owner)||!cell.IsVisible||!cell.Explored||gas==null||!ReferenceEquals(gas.ParentEntity,owner)
      ||gas.Density<=0||GasRegistry.Get(gas.GasId)==null||owner.BlueprintName!=gas.GasId+"Cloud"||!owner.HasTag("Gas")
      ||owner.HasTag("Creature")||owner.HasTag("Item")||owner.HasPart<SpatialFootprintPart>()||owner.HasPart<MultiCellPilotPropPart>()
      ||physics==null||!ReferenceEquals(physics.ParentEntity,owner)||physics.Solid||physics.Takeable||physics.InInventory!=null||physics.Equipped!=null
      ||render==null||!ReferenceEquals(render.ParentEntity,owner)||!render.Visible
      ||!string.IsNullOrEmpty(render.VisualID)||!string.IsNullOrEmpty(render.VisualVariant)||!string.IsNullOrEmpty(render.GlyphVariants)
      ||render.RenderString!=GasVisuals.GlyphForDensity(gas.Density).ToString()||render.ColorString!=gas.ColorString||!ValidColor(gas.ColorString))return false;
   int band=gas.Density>=GasVisuals.DARK_THRESHOLD?3:gas.Density>=GasVisuals.MEDIUM_THRESHOLD?2:1;
   sample=new SpreadTransientSample(owner,gas.GasId,gas.GasType,gas.ColorString,cell.X,cell.Y,gas.Density,band);return true;
  }
  /// <summary>The existing primary tile-mark priority, limited to supported
  /// elemental/cloud sources. Coatings/residues keep their own higher-priority
  /// native/fallback treatment; this cannot suppress an unrepresented mark.</summary>
  public static bool TryElement(Zone zone,int x,int y,out SpreadTransientSample sample,bool representedPermanentWater=false)
  {
   sample=default;
   if(!SpreadPresentationScope.IsActive(zone)||!zone.InBounds(x,y))return false;
   var cell=zone.GetCell(x,y);if(cell==null||!cell.IsVisible||!cell.Explored)return false;
   var state=zone.TileState.Get(x,y);if(state==null||state.Coatings==null||state.Residues==null||state.Residues.Count>0)return false;
   if(state.Coatings.Count>0&&!(representedPermanentWater&&state.Coatings.Count==1&&state.Coatings[0]?.Id=="water"&&state.Coatings[0].Turns==ZoneTileState.Permanent))return false;
   string kind,color;int amount;
   if(state.Charge>0){kind="charge";color="&W";amount=state.Charge;}
   else if(state.Heat>0){kind="heat";color="&R";amount=state.Heat;}
   else if(state.Cold>0){kind="cold";color="&C";amount=state.Cold;}
   else if((state.Cloud=="steam"||state.Cloud=="smoke")&&state.CloudTurns>0){kind=state.Cloud;color=kind=="steam"?"&Y":"&K";amount=state.CloudTurns;}
   else return false;
   sample=new SpreadTransientSample(null,kind,"",color,x,y,amount,Math.Min(2,amount));return true;
  }
  /// <summary>Read-only highest-priority authored coating/residue. Unknown
  /// layers keep fallback; represented permanent water remains the ground's
  /// responsibility. Amount retains actual remaining turns, including Permanent.</summary>
  public static bool TrySurfaceMark(Zone zone,int x,int y,out SpreadTransientSample sample,bool representedPermanentWater=false)
  {
   sample=default;if(!SpreadPresentationScope.IsActive(zone)||!zone.InBounds(x,y))return false;
   var cell=zone.GetCell(x,y);if(cell==null||!cell.Explored||!cell.IsVisible)return false;
   var state=zone.TileState.Get(x,y);if(state==null||state.Residues==null||state.Coatings==null)return false;
   if(state.Residues.Count>0)
   {
    var layer=state.Residues[0];if(!LiveLayer(layer)||(layer.Id!="embers"&&layer.Id!="petals"))return false;
    sample=new SpreadTransientSample(null,"residue:"+layer.Id,"",layer.Id=="embers"?"&R":"&w",x,y,layer.Turns,1);return true;
   }
   if(state.Coatings.Count==0)return false;
   foreach(var layer in state.Coatings)if(!LiveLayer(layer))return false;
   var selected=state.Coatings[0];
   if(representedPermanentWater&&state.Coatings.Count==1&&selected.Id=="water"&&selected.Turns==ZoneTileState.Permanent)return false;
   foreach(var layer in state.Coatings)if(layer.Id=="ice")selected=layer;
   foreach(var layer in state.Coatings)if(layer.Id=="oil")selected=layer;
   if(LiquidRegistry.Get(selected.Id)==null)return false;
   sample=new SpreadTransientSample(null,"coating:"+selected.Id,"",selected.Id=="oil"?"&m":selected.Id=="ice"?"&C":"&B",x,y,selected.Turns,1);return true;
  }
  static bool LiveLayer(ZoneTileState.Layer layer)=>layer!=null&&!string.IsNullOrEmpty(layer.Id)&&(layer.Turns>0||layer.Turns==ZoneTileState.Permanent);
  internal static bool ValidColor(string color)=>color!=null&&color.Length==2&&color[0]=='&'&&"krwgbmcyKRWGBMCY".IndexOf(color[1])>=0;
 }
}
