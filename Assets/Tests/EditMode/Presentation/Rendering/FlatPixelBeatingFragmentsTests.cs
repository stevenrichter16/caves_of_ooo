using CavesOfOoo.Core;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
 public sealed partial class FlatPixelBeatingMaterialsTests
 {
  [TestCase("SandstoneFloor")][TestCase("Sand")]
  public void NeutralColumnRetainsRealDryFloor(string bp)
  {Add(bp);var column=Add("BrokenColumn");var v=View();Assert.AreSame(column,v.Owner);NativeTile(v.ForegroundTile,"Sprites/Live16x24/opening-base");Assert.True(v.LocalGround);NativeTile(v.GroundTile,Atlas);Assert.False(v.BlocksMovement);}
  [TestCase("no-floor")][TestCase("crop")][TestCase("wet")][TestCase("custom")][TestCase("solid")]
  public void ColumnDoesNotInventOrSuppressUnsupportedGround(string fault)
  {if(fault!="no-floor")Add("SandstoneFloor");var c=Add("BrokenColumn");if(fault=="crop")c.AddPart(new CropPart());if(fault=="wet")zone.TileState.WriteCoating(X,Y,"water",ZoneTileState.Permanent);if(fault=="custom")c.GetPart<RenderPart>().VisualID="unknown";if(fault=="solid")c.GetPart<PhysicsPart>().Solid=true;Assert.False(View().LocalGround);}
 }
}
