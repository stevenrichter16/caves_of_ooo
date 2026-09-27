using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadParticleSourceTests
 {
  OverworldZoneManager manager;Zone zone;
  [SetUp]public void Setup(){manager=OverworldZoneManager.CreateDetached(new EntityFactory(),64);manager.WorldMap.Tiles[4,9]=BiomeType.Spread;manager.WorldMap.SetPOI(4,9,null);zone=new Zone("Overworld.4.9.0");manager.SetActiveZone(zone);foreach(var c in zone.Cells){c.IsVisible=true;c.Explored=true;}}
  [TestCase('*',"spark")][TestCase('+',"spark")][TestCase('o',"spark")][TestCase('Z',"spark")][TestCase('\u00B7',"spark")]
  [TestCase('=',"east-west")][TestCase('|',"north-south")][TestCase('-',"east-west")][TestCase('/',"diagonal-up")][TestCase('\\',"diagonal-down")]
  public void ExistingResolvedDecorationPreservesItsCellColorAndOrientation(char glyph,string shape)
  {int version=zone.EntityVersion;string tiles=zone.TileState.ToSaveString();Assert.True(SpreadParticleSource.TrySample(zone,20,10,glyph,"&M",out var s));Assert.AreEqual(20,s.X);Assert.AreEqual(10,s.Y);Assert.AreEqual(glyph,s.Glyph);Assert.AreEqual("&M",s.Color);Assert.AreEqual(shape,s.Shape);Assert.AreEqual(version,zone.EntityVersion);Assert.AreEqual(tiles,zone.TileState.ToSaveString());}
  [TestCase("east-west",0)][TestCase("north-south",90)][TestCase("diagonal-up",-45)][TestCase("diagonal-down",45)]
  public void ActualBeamDirectionMatchesRowNorthProjection(string shape,int yaw){Assert.AreEqual(yaw,SpreadParticleSource.YawDegrees(shape));}
  [TestCase('0')][TestCase('1')][TestCase('2')][TestCase('3')][TestCase('4')][TestCase('5')][TestCase('6')][TestCase('7')][TestCase('8')][TestCase('9')][TestCase(' ')][TestCase('\n')][TestCase('\0')]
  public void NumbersAndEmptyMarksRetainReadableUiOrEmptyFallback(char glyph){Assert.False(SpreadParticleSource.TrySample(zone,20,10,glyph,"&M",out _));}
  [TestCase("foreign")][TestCase("hidden")][TestCase("unexplored")][TestCase("detached")][TestCase("replaced")][TestCase("out")][TestCase("unknown-color")][TestCase("null")]
  public void IdenticalDecorationMustBelongToActuallyVisibleCurrentSpread(string fault)
  {Assert.True(SpreadParticleSource.TrySample(zone,20,10,'*',"&R",out _));int x=20;string color="&R";if(fault=="foreign")manager.WorldMap.Tiles[4,9]=BiomeType.Sodden;else if(fault=="hidden")zone.GetCell(20,10).IsVisible=false;else if(fault=="unexplored")zone.GetCell(20,10).Explored=false;else if(fault=="detached")zone=new Zone(zone.ZoneID);else if(fault=="replaced")manager.CachedZones[zone.ZoneID]=new Zone(zone.ZoneID);else if(fault=="out")x=-1;else if(fault=="unknown-color")color="invalid";else zone=null;Assert.False(SpreadParticleSource.TrySample(zone,x,10,'*',color,out _));}
 }
}
