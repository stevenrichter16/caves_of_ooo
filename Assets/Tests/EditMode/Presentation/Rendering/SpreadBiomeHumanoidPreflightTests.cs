using System;
using CavesOfOoo.Rendering;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadBiomeHumanoidPreflightTests
 {
  [TestCase("valid",false)][TestCase("nan",true)][TestCase("unknown",true)][TestCase("null",true)]
  [TestCase("positive-infinity",true)][TestCase("negative-infinity",true)][TestCase("lower-boundary",false)]
  [TestCase("oversize",true)][TestCase("wrong-id",true)][TestCase("swatch",true)][TestCase("headwear",true)][TestCase("garment",true)]
  public void InvalidSourceIsRejectedBeforeAnyAssetWrite(string mutation,bool reject)
  {
   var row=new SpreadBiomeHumanoidSource.Role{blueprint="Farmer",id="spread-person-farmer",glyph="@",body=6,skin=18,accent=22,hair=19,headwear="straw-hat",garment="apron",stature=1};
   switch(mutation)
   {
    case "nan":row.stature=float.NaN;break;
    case "unknown":row.blueprint="NotARealCreature";row.id=null;break;
    case "null":row.blueprint=null;row.id=null;break;
    case "positive-infinity":row.stature=float.PositiveInfinity;break;
    case "negative-infinity":row.stature=float.NegativeInfinity;break;
    case "lower-boundary":row.stature=.65f;break;
    case "oversize":row.stature=1.0001f;break;
    case "wrong-id":row.id="spread-person-scribe";break;
    case "swatch":row.skin=24;break;
    case "headwear":row.headwear="foreign";break;
    case "garment":row.garment="foreign";break;
   }
   if(reject)Assert.Throws<ArgumentException>(()=>SpreadBiomeHumanoidSource.ValidateRole(row));else Assert.DoesNotThrow(()=>SpreadBiomeHumanoidSource.ValidateRole(row));
  }
 }
}
