using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests {
public sealed class SpreadPortableSourceTests {
private SpreadPortableSource Read()=>JsonUtility.FromJson<SpreadPortableSource>(File.ReadAllText(Environment.GetEnvironmentVariable("COO_PORTABLE_SOURCE") ?? Path.GetFullPath(Path.Combine(Application.dataPath,"../ArtSource/SpreadPortable3D/portable-source.json"))));
[Test]public void RealPackedSourcesAreCompleteAndOwned(){var source=Read();Assert.DoesNotThrow(source.Validate);Assert.AreEqual(396,source.models.Length);Assert.AreEqual(149,source.models.Count(m=>m.borrowed));}
[TestCase("schema")][TestCase("paletteMissing")][TestCase("paletteColor")][TestCase("empty")][TestCase("duplicate")][TestCase("foreignId")][TestCase("pathEscape")][TestCase("sourceDigest")][TestCase("nonfinite")][TestCase("missingCoordinates")][TestCase("badIndex")][TestCase("badPaint")][TestCase("noTriangles")][TestCase("degenerate")][TestCase("outOfCell")][TestCase("underground")][TestCase("tooTall")][TestCase("wrongPitch")][TestCase("nanPitch")][TestCase("borrowedPath")]
public void EntireSourcePreflightRejectsMalformedWithoutPartialImport(string change){var s=Read();var m=s.models[0];switch(change){case "schema":s.schemaVersion=0;break;case "paletteMissing":s.palette=null;break;case "paletteColor":s.palette[0]="#zzzzzz";break;case "empty":s.models=Array.Empty<SpreadPortableSource.Model>();break;case "duplicate":s.models[1]=m;break;case "foreignId":m.id="ring-player";break;case "pathEscape":m.source="../foreign.fbx";break;case "sourceDigest":m.sourceSha256="bad";break;case "nonfinite":m.positions[0]=float.NaN;break;case "missingCoordinates":m.positions=new float[5];break;case "badIndex":m.triangles[0]=int.MaxValue;break;case "badPaint":m.paletteIndices[0]=s.palette.Length;break;case "noTriangles":m.triangles=Array.Empty<int>();break;case "degenerate":m.triangles[1]=m.triangles[0];break;case "outOfCell":m.positions[0]=1;break;case "underground":m.positions[1]=-.1f;break;case "tooTall":m.positions[1]=2;break;case "wrongPitch":m.pitch=.5f;break;case "nanPitch":m.pitch=float.NaN;break;case "borrowedPath":m.source="Assets/Resources/foreign.fbx";break;}Assert.Throws<InvalidOperationException>(s.Validate);}
[Test]public void ValidationDoesNotRewriteAnyBuffer(){var s=Read();var m=s.models[0];var xyz=m.positions.ToArray();var tri=m.triangles.ToArray();var paint=m.paletteIndices.ToArray();s.Validate();CollectionAssert.AreEqual(xyz,m.positions);CollectionAssert.AreEqual(tri,m.triangles);CollectionAssert.AreEqual(paint,m.paletteIndices);}
}}
