using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class SpreadScenerySourceTests
    {
        internal static SpreadScenerySource Valid()
        {
            return new SpreadScenerySource { schemaVersion = 1,
                palette = SpreadScenerySource.ApprovedPalette.ToArray(),
                models = SpreadScenerySource.ModelIds.Select(id => new SpreadScenerySource.Model {
                    id = id, boxes = new[] { new SpreadScenerySource.Box {
                        center = new[] { 0f, .1f, 0f }, size = new[] { .2f, .2f, .2f }, color = 4
                    } }
                }).ToArray() };
        }
        [Test] public void CompleteExactPackPassesWithoutChangingSource()
        {
            var source = Valid(); string before = JsonUtility.ToJson(source);
            source.Validate(); source.Validate(); Assert.AreEqual(64, source.models.Length);
            Assert.AreEqual(before, JsonUtility.ToJson(source));
        }
        [Test] public void ActualAuthoredPackHasExactCoverageAndDistinctGeometry()
        {
            string path = Environment.GetEnvironmentVariable("COO_SCENERY_SOURCE")
                ?? Path.GetFullPath(Path.Combine(Application.dataPath, "../ArtSource/SpreadScenery3D/kit.json"));
            var source = JsonUtility.FromJson<SpreadScenerySource>(File.ReadAllText(path));
            source.Validate();
            Assert.AreEqual(64, source.models.Select(m => string.Join("|",m.boxes.Select(b=>JsonUtility.ToJson(b)))).Distinct().Count(),
                "Every model variant has intentionally distinct authored geometry.");
            Assert.LessOrEqual(source.models.Max(m => m.boxes.Length), 512);
            CollectionAssert.AreEqual(SpreadScenerySource.ApprovedPalette, source.palette);
        }
        [Test] public void AuthoredWellUsesStoneAndWoodWithoutGrassColoredMasonry()
        {
            string path=Environment.GetEnvironmentVariable("COO_SCENERY_SOURCE")
                ?? Path.GetFullPath(Path.Combine(Application.dataPath,"../ArtSource/SpreadScenery3D/kit.json"));
            var source=JsonUtility.FromJson<SpreadScenerySource>(File.ReadAllText(path));
            foreach(var model in source.models.Where(m=>m.id.StartsWith("spread-scenery-well-",StringComparison.Ordinal)))
                Assert.True(model.boxes.All(b=>b.color<13||b.color>16),model.id);
        }
        [TestCase("spread-scenery-stonefloor-0","ground")]
        [TestCase("spread-scenery-stonefloor-1","ground")]
        [TestCase("spread-scenery-wellgroundmarker-0","entity")]
        [TestCase("spread-scenery-ovengroundmarker-0","entity")]
        [TestCase("spread-scenery-lanterngroundmarker-0","entity")]
        [TestCase("spread-scenery-campfiregroundmarker-0","entity")]
        public void OnlyRealFloorSuppressesTheNativeCellGroundFallback(string id,string kind)
        {Assert.AreEqual(kind,SpreadScenerySource.KindForModel(id));}
        [TestCase("schema")][TestCase("no-palette")][TestCase("palette-short")][TestCase("foreign-palette")]
        [TestCase("no-models")][TestCase("missing-model")][TestCase("null-model")][TestCase("duplicate")]
        [TestCase("foreign-id")][TestCase("path-id")][TestCase("no-boxes")][TestCase("empty-boxes")]
        [TestCase("null-box")][TestCase("bad-color")][TestCase("no-center")][TestCase("short-size")]
        [TestCase("zero-size")][TestCase("negative-size")][TestCase("nan-center")][TestCase("infinite-size")]
        [TestCase("tiny-size")][TestCase("wide")][TestCase("low")][TestCase("high")][TestCase("too-many-boxes")]
        public void InvalidPackRefusesBeforePersistentImport(string change)
        {
            var source = Valid(); source.Validate(); var model = source.models[0]; var box = model.boxes[0];
            switch(change)
            {
                case "schema": source.schemaVersion = 2; break;
                case "no-palette": source.palette = null; break;
                case "palette-short": source.palette = source.palette.Take(23).ToArray(); break;
                case "foreign-palette": source.palette[0] = "#123456"; break;
                case "no-models": source.models = null; break;
                case "missing-model": source.models = source.models.Skip(1).ToArray(); break;
                case "null-model": source.models[0] = null; break;
                case "duplicate": source.models[1].id = model.id; break;
                case "foreign-id": model.id = "spread-scenery-unknown-0"; break;
                case "path-id": model.id = "../Library"; break;
                case "no-boxes": model.boxes = null; break;
                case "empty-boxes": model.boxes = Array.Empty<SpreadScenerySource.Box>(); break;
                case "null-box": model.boxes[0] = null; break;
                case "bad-color": box.color = 24; break;
                case "no-center": box.center = null; break;
                case "short-size": box.size = new float[2]; break;
                case "zero-size": box.size[0] = 0; break;
                case "negative-size": box.size[0] = -.1f; break;
                case "nan-center": box.center[1] = float.NaN; break;
                case "infinite-size": box.size[2] = float.PositiveInfinity; break;
                case "tiny-size": box.size[0] = float.Epsilon; break;
                case "wide": box.center[0] = .49f; break;
                case "low": box.center[1] = -.05f; break;
                case "high": box.center[1] = 1.65f; break;
                case "too-many-boxes": model.boxes = Enumerable.Repeat(box, 513).ToArray(); break;
            }
            Assert.Throws<InvalidOperationException>(() => source.Validate());
        }
        [TestCase(null)][TestCase("BerryBush")][TestCase("spread-scenery-berrybush-2")]
        public void UnreviewedModelIdentityIsNotRecognized(string id)
        { Assert.False(SpreadScenerySource.IsModelId(id)); }
    }
}
