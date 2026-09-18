using System;
using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class MorrowfastDerivedActorTests
    {
        [Test] public void IneligibleTerrainDoesNotInvokeTheSourceViewScan()
        {
            var root = new GameObject("Source eligibility fixture");
            try
            {
                var renderer = root.AddComponent<AnimatedEntityRenderer>(); renderer.Init(null);
                int probes = 0; renderer.SetSourceEntityPredicate(e => { probes++; return false; });
                var terrain = new Entity { BlueprintName = "TepuiStone" };
                Assert.IsFalse(renderer.CanRender(terrain)); Assert.AreEqual(0, probes);
                var zone = new Zone("ordinary"); zone.AddEntity(terrain, 40, 12);
                renderer.SyncZone(zone); Assert.AreEqual(0, probes, "An unrenderable cell cannot require a 145-layer source-body search.");
                var player = new Entity { BlueprintName = "Player" }; player.AddPart(new RenderPart { RenderString = "@" });
                Assert.IsTrue(renderer.CanRender(player)); Assert.Greater(probes, 0, "The eligibility control must still consult actual source ownership for a catalog actor.");
                int beforeReskin = probes; player.GetPart<RenderPart>().RenderString = "?";
                Assert.IsFalse(renderer.CanRender(player)); Assert.AreEqual(beforeReskin, probes,
                    "The catalog's reskin guard rejects a mismatched glyph before any source scan.");
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase("MorrowfastFarra", "actor.morrowfast_farra", "western-shopkeeper-sprite", 5, 3)]
        [TestCase("MorrowfastEdden", "actor.morrowfast_edden", "east-robed-resident-sprite", 4, 3)]
        public void AdditionalResidentsResolveToDetailedSourceBodySheetsWithoutGroundOrInventedMotion(string blueprint, string id, string sourceName, int dx, int dy)
        {
            var entity = new Entity { BlueprintName = blueprint }; entity.AddPart(new RenderPart { RenderString = "@" });
            Assert.IsTrue(EntityVisualCatalog.TryGetAsset(entity, out var asset), blueprint);
            Assert.AreEqual(id, asset.Definition.ID); Assert.AreEqual(41, asset.Definition.PixelsPerUnit);
            Assert.AreEqual(48, asset.Definition.FrameWidth); Assert.AreEqual(64, asset.Definition.FrameHeight);
            Assert.IsFalse(asset.HasCastingArt);
            var sheet = Resources.Load<Texture2D>(asset.Definition.Sheet); Assert.NotNull(sheet); Assert.IsTrue(sheet.isReadable);
            Assert.AreEqual(new Vector2Int(192, 1024), new Vector2Int(sheet.width, sheet.height));
            var source = new Texture2D(2, 2);
            try
            {
                source.LoadImage(File.ReadAllBytes(Path.Combine(Application.dataPath, "../ArtSource/Morrowfast/build/sprites/" + sourceName + ".png")));
                var pixels = sheet.GetPixels32(); var original = source.GetPixels32(); int opaque = 0;
                for (int row = 0; row < 16; row++) for (int y = 0; y < 64; y++) for (int x = 0; x < 192; x++)
                {
                    int sx = x - dx, sy = y - dy;
                    byte expectedAlpha = x < 48 && sx >= 0 && sx < source.width && sy >= 0 && sy < source.height
                        ? original[(source.height - 1 - sy) * source.width + sx].a : (byte)0;
                    var actual = pixels[(sheet.height - 1 - row * 64 - y) * sheet.width + x];
                    Assert.AreEqual(expectedAlpha, actual.a, $"{id} row{row} pixel{x},{y}");
                    if (row == 0 && actual.a != 0) opaque++;
                    if (row > 0) Assert.AreEqual(pixels[(sheet.height - 1 - y) * sheet.width + x], actual, "Every state/facing repeats the honest source pose.");
                }
                Assert.Greater(opaque, 500);
                foreach (EntityVisualState state in Enum.GetValues(typeof(EntityVisualState)))
                {
                    Assert.AreEqual(1, asset.GetFrameCount(state));
                    foreach (EntityVisualFacing facing in Enum.GetValues(typeof(EntityVisualFacing)))
                    { var sprite = asset.GetFrame(state, facing, 0); Assert.NotNull(sprite); Assert.AreEqual(41, sprite.pixelsPerUnit); }
                }
            }
            finally { Object.DestroyImmediate(source); }
            entity.GetPart<RenderPart>().RenderString = "?";
            Assert.IsFalse(EntityVisualCatalog.TryGetAsset(entity, out _), "Blueprint fallback must retain the existing canonical-glyph guard.");
            entity.GetPart<RenderPart>().VisualID = id;
            Assert.IsTrue(EntityVisualCatalog.TryGetAsset(entity, out _), "The explicit visual ID used by the native extra resident remains valid.");
        }
    }
}
