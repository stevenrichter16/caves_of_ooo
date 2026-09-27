using CavesOfOoo.Core;
namespace CavesOfOoo.Rendering
{
 /// <summary>Immutable resolved draw request from the existing FX clocks. This
 /// describes one displayed cell; it owns no timer, impact or gameplay state.</summary>
 public readonly struct SpreadParticleSample
 {
  public readonly int X,Y;public readonly char Glyph;public readonly string Color,Shape;
  public SpreadParticleSample(int x,int y,char glyph,string color,string shape){X=x;Y=y;Glyph=glyph;Color=color;Shape=shape;}
 }
 public static class SpreadParticleSource
 {
  /// <summary>Native XZ yaw for the already resolved beam glyph; simulation
  /// rows increase south, so slash-up points toward positive world Z.</summary>
  public static int YawDegrees(string shape)=>shape=="north-south"?90:(shape=="diagonal-up"||shape=="sleep")?-45:shape=="diagonal-down"?45:0;
  /// <summary>Read-only authority for decorative legacy marks. Floating digits
  /// remain UI; unsupported colors and empty marks retain existing fallback.</summary>
  public static bool TrySample(Zone zone,int x,int y,char glyph,string color,out SpreadParticleSample sample)
  {
   sample=default;
   if(!SpreadPresentationScope.IsActive(zone)||!zone.InBounds(x,y)||char.IsDigit(glyph)||char.IsWhiteSpace(glyph)||char.IsControl(glyph)
      ||color==null||color.Length!=2||color[0]!='&'||"kKrRgGbBcCmMwWyY".IndexOf(color[1])<0)return false;
   var cell=zone.GetCell(x,y);if(cell==null||!cell.Explored||!cell.IsVisible)return false;
   string shape=glyph=='!'?"alert":glyph=='z'?"sleep":glyph=='V'?"down-chevron":glyph=='|'?"north-south":(glyph=='-'||glyph=='=')?"east-west":glyph=='/'?"diagonal-up":glyph=='\\'?"diagonal-down":"spark";
   sample=new SpreadParticleSample(x,y,glyph,color,shape);return true;
  }
 }
}
