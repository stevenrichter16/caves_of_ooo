using System;
using System.IO;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadEquipmentSourceTests
 {
  static SpreadEquipmentSource Source()=>JsonUtility.FromJson<SpreadEquipmentSource>(File.ReadAllText(Environment.GetEnvironmentVariable("COO_GEAR_SOURCE")??Path.Combine(Application.dataPath,"../ArtSource/SpreadEquipment3D/worn-source.json")));
  [Test] public void ExactTwelveFittedFormsHaveBoundedAuthoredSource(){var s=Source();Assert.DoesNotThrow(s.Validate);Assert.AreEqual(12,s.models.Length);}
  [TestCase("missing")][TestCase("duplicate")][TestCase("unknown")][TestCase("slot")][TestCase("null-boxes")][TestCase("empty-boxes")][TestCase("null-box")][TestCase("nonfinite")][TestCase("negative-size")][TestCase("far")][TestCase("color")][TestCase("huge")][TestCase("schema")][TestCase("palette")]
  public void WholePackPreflightRefusesMalformedSource(string mode)
  {
   var s=Source();var m=s.models[0];var b=m.boxes[0];
   switch(mode){case "missing":Array.Resize(ref s.models,11);break;case "duplicate":s.models[1]=m;break;case "unknown":m.id="borrowed-fake";break;case "slot":m.slot="Hand";break;
   case "null-boxes":m.boxes=null;break;case "empty-boxes":m.boxes=Array.Empty<SpreadEquipmentSource.Box>();break;case "null-box":m.boxes[0]=null;break;
   case "nonfinite":b.center[0]=float.NaN;break;case "negative-size":b.size[0]=-.1f;break;case "far":b.center[1]=5;break;case "color":b.paint=42;break;
   case "huge":m.boxes=new SpreadEquipmentSource.Box[257];break;case "schema":s.schemaVersion=2;break;case "palette":s.paletteCells=24;break;}
   Assert.Throws<InvalidOperationException>(s.Validate,mode);
  }
 }
}
