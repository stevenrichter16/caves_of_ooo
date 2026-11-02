using System;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed partial class FlatPixelMorrowfastGroundTests
    {
        [TestCase(MorrowfastSceneRuntime.FloorName, false)]
        [TestCase(MorrowfastSceneRuntime.FloorName, true)]
        [TestCase(MorrowfastSceneRuntime.GroundName, false)]
        [TestCase(MorrowfastSceneRuntime.GroundName, true)]
        public void AuthoredGroundReaderUsesExactWorldMaterial(string role, bool covered)
        {
            var (x, y, floor) = Find(role, false);
            var expected = At(x, y).GroundTile.sprite;
            Entity actor = null;
            if (covered)
            {
                actor = new Entity { ID = Guid.NewGuid().ToString("N"), BlueprintName = "ReaderViewer" };
                actor.SetTag("Player"); actor.AddPart(new PhysicsPart());
                actor.AddPart(new RenderPart { RenderString = "@", Visible = true, RenderLayer = 100 });
                Assert.True(zone.AddEntity(actor, x, y));
            }
            Assert.True(world.TryGetPresentationSubjectSprite(zone, floor, x, y, out var sprite, out var tint));
            Assert.AreSame(expected.texture, sprite.texture);
            Assert.AreEqual(expected.rect, sprite.rect); Assert.AreEqual(Color.white, tint);
            if (covered) Assert.AreSame(actor, At(x, y).Owner, "The reader cannot replace the world-selected actor.");
        }

        [TestCase("hidden-cell")][TestCase("memory")][TestCase("hidden-owner")]
        [TestCase("renamed")][TestCase("retagged")][TestCase("foreign-render")]
        [TestCase("carried")][TestCase("duplicate")][TestCase("wrong-coordinate")]
        [TestCase("detached")][TestCase("missing-scene")]
        public void AuthoredGroundReaderRefusesLostAuthority(string loss)
        {
            var (x, y, floor) = Find(MorrowfastSceneRuntime.FloorName, false);
            using (var selector = new FlatPixelFallbackCatalog())
            using (var art = new FlatPixelSettlementGroundCatalog(selector))
            {
                Assert.True(art.TryGetSubjectSprite(zone, floor, x, y, out _));
                var c = zone.GetCell(x, y);
                if (loss == "hidden-cell") c.Explored = c.IsVisible = false;
                if (loss == "memory") c.IsVisible = false;
                if (loss == "hidden-owner") floor.GetPart<RenderPart>().Visible = false;
                if (loss == "renamed") floor.GetPart<RenderPart>().DisplayName = "other";
                if (loss == "retagged") floor.Tags.Remove(MorrowfastSceneRuntime.TerrainTag);
                if (loss == "foreign-render") floor.GetPart<RenderPart>().ParentEntity = new Entity();
                if (loss == "carried") floor.GetPart<PhysicsPart>().InInventory = new Entity();
                if (loss == "duplicate") { var (_, _, second) = Find(MorrowfastSceneRuntime.GroundName, false); zone.RemoveEntity(second); Assert.True(zone.AddEntity(second, x, y)); }
                if (loss == "wrong-coordinate") x++;
                if (loss == "detached") manager.CachedZones.Remove(zone.ZoneID);
                if (loss == "missing-scene") zone.RemoveEntity(MorrowfastSceneRuntime.GetState(zone).ParentEntity);
                Assert.False(art.TryGetSubjectSprite(zone, floor, x, y, out _), loss);
            }
        }

        [TestCase(MorrowfastSceneRuntime.FloorName)]
        [TestCase(MorrowfastSceneRuntime.GroundName)]
        public void MissingOptionalGroundArtDoesNotBorrowAnotherPortrait(string role)
        {
            var (x, y, floor) = Find(role, false);
            using (var selector = new FlatPixelFallbackCatalog())
            using (var art = new FlatPixelSettlementGroundCatalog(null, null, null, null, null, selector))
                Assert.False(art.TryGetSubjectSprite(zone, floor, x, y, out _));
        }
    }
}
