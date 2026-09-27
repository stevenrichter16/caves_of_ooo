using CavesOfOoo.Core;using CavesOfOoo.Data;using CavesOfOoo.Rendering;using NUnit.Framework;
namespace CavesOfOoo.Tests{public sealed class SpreadParticleSignalSourceTests{
 [TestCase('!',"alert")][TestCase('z',"sleep")][TestCase('V',"down-chevron")][TestCase('*',"spark")][TestCase('Z',"spark")]
 public void ExistingBehaviorSignalKeepsItsOwnNativeShape(char glyph,string shape){var m=OverworldZoneManager.CreateDetached(new EntityFactory(),64);m.WorldMap.Tiles[4,9]=BiomeType.Spread;m.WorldMap.SetPOI(4,9,null);var z=new Zone("Overworld.4.9.0");m.SetActiveZone(z);z.GetCell(20,10).Explored=z.GetCell(20,10).IsVisible=true;Assert.True(SpreadParticleSource.TrySample(z,20,10,glyph,"&Y",out var s));Assert.AreEqual(shape,s.Shape);}
 [Test] public void SleepDiagonalUsesTheSameAscendingDirectionAsSlash(){Assert.AreEqual(-45,SpreadParticleSource.YawDegrees("sleep"));Assert.AreEqual(SpreadParticleSource.YawDegrees("diagonal-up"),SpreadParticleSource.YawDegrees("sleep"));}
}}
