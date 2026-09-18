using System;
using System.Linq;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Native presentation boundaries, independent of the browser's fine grid.</summary>
    public sealed class MorrowfastScenePresenterTests
    {
        [TestCase(0f, 0f, 21.25f, 25f)]
        [TestCase(1536f, 1024f, 58.75f, 0f)]
        [TestCase(768f, 512f, 40f, 12.5f)]
        public void EntireTopdownCanvasUsesOneUniformScale(float x, float y, float wx, float wy)
        {
            Vector2 world = MorrowfastScenePresenter.ImageToWorld(new Vector2(x, y));
            Assert.AreEqual(wx, world.x, 0.0001f);
            Assert.AreEqual(wy, world.y, 0.0001f);
            Vector2 source = MorrowfastScenePresenter.WorldToImage(world);
            Assert.AreEqual(x, source.x, 0.001f);
            Assert.AreEqual(y, source.y, 0.001f);
        }

        [TestCase(0f, 0f, true, 21, 0)]
        [TestCase(768f, 0f, true, 40, 0)]
        [TestCase(1535f, 1023f, true, 58, 24)]
        [TestCase(-1f, 100f, false, -1, -1)]
        [TestCase(1536f, 100f, false, -1, -1)]
        [TestCase(100f, 1024f, false, -1, -1)]
        public void PickingIncludesTheNorthernImageRowsAndRejectsOutside(float px, float py, bool expected, int x, int y)
        {
            Assert.AreEqual(expected, MorrowfastScenePresenter.TryImageToCell(new Vector2(px, py), out int actualX, out int actualY));
            Assert.AreEqual(x, actualX);
            Assert.AreEqual(y, actualY);
        }

        [Test]
        public void NonfiniteSamplesCannotAliasAValidTile()
        {
            Assert.IsFalse(MorrowfastScenePresenter.TryImageToCell(new Vector2(float.NaN, 30), out _, out _));
            Assert.IsFalse(MorrowfastScenePresenter.TryImageToCell(new Vector2(30, float.PositiveInfinity), out _, out _));
        }

        [TestCase(1.5f, 12.75f)]
        [TestCase(2f, 12.75f)]
        [TestCase(1f, 18.75f)]
        public void CameraFitsTheCompleteArtWithoutStretching(float aspect, float halfHeight)
            => Assert.AreEqual(halfHeight, MorrowfastScenePresenter.CameraHalfHeight(aspect), 0.0001f);

        [Test]
        public void RoomCutawayMembershipDoesNotRevealAnAdjacentRoom()
        {
            var polygon = new[] { new MorrowfastArtDefinition.Point { x=10,y=10 }, new MorrowfastArtDefinition.Point { x=20,y=10 },
                new MorrowfastArtDefinition.Point { x=20,y=20 }, new MorrowfastArtDefinition.Point { x=10,y=20 } };
            Assert.IsTrue(MorrowfastScenePresenter.IsInsideRoom(new Vector2(15,15), polygon));
            Assert.IsFalse(MorrowfastScenePresenter.IsInsideRoom(new Vector2(25,15), polygon));
            Assert.IsFalse(MorrowfastScenePresenter.IsInsideRoom(new Vector2(float.NaN,15), polygon));
        }

        [Test]
        public void ArtDefinitionContainsEverySourceOwnerAndItsSeparatelyAddressableLayers()
        {
            var definition = MorrowfastArtDefinition.Load();
            Assert.NotNull(definition);
            Assert.AreEqual(5, definition.rooms.Length);
            Assert.GreaterOrEqual(definition.owners.Length, 71);
            Assert.GreaterOrEqual(definition.layers.Length, 145);
            foreach (var layer in definition.layers) Assert.NotNull(definition.FindOwner(layer.ownerId), layer.id);
            Assert.AreEqual(40.96f, definition.pixelsPerCell);
            Assert.AreEqual(21.25f, definition.originX);
        }

        [Test]
        public void ParserRefusesLayerPathsAndOrphanOwnersBeforeRendererBinding()
        {
            string valid = Resources.Load<TextAsset>("SceneArt/Morrowfast/art-definition").text;
            var invalidPath = MorrowfastArtDefinition.Parse(valid);
            invalidPath.layers[0].resource = "SceneArt/Morrowfast/../elsewhere";
            Assert.Throws<ArgumentException>(() => invalidPath.Validate());
            var orphan = MorrowfastArtDefinition.Parse(valid);
            orphan.layers[0].ownerId = "absent-owner";
            Assert.Throws<ArgumentException>(() => orphan.Validate());
            var duplicate = MorrowfastArtDefinition.Parse(valid);
            duplicate.owners[1].id = duplicate.owners[0].id;
            Assert.Throws<ArgumentException>(() => duplicate.Validate());
        }

        [Test]
        public void ImportedSourceAssetsKeepExactPixelsAndFractionalPixelsPerUnit()
        {
            var definition = MorrowfastArtDefinition.Load();
            foreach (var layer in definition.layers)
            {
                var sprite = Resources.Load<Sprite>(layer.resource);
                Assert.NotNull(sprite, layer.id);
                Assert.AreEqual(40.96f, sprite.pixelsPerUnit, 0.0001f, layer.id);
                Assert.AreEqual(Vector2.zero, sprite.pivot, layer.id);
                Assert.AreEqual(new Vector2(layer.bounds[2], layer.bounds[3]), sprite.rect.size, layer.id);
                Assert.AreEqual(FilterMode.Point, sprite.texture.filterMode, layer.id);
                Assert.IsTrue(sprite.texture.isReadable, layer.id);
            }
        }
    }
}
