using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Dedicated post-GREEN taxonomy probes. These are catalog-level boundaries,
    // not a second claim of native RED or ordinary gameplay acceptance.
    public sealed class FlatPixelPatchbearerMotionAdversarialTests
    {
        EntityEquipmentContentFixture content;
        Zone zone;
        Entity actor;
        Texture2D works, motion;
        FlatPixelPatchbearerMotionCatalog art;

        [SetUp] public void Setup()
        {
            content = new EntityEquipmentContentFixture();
            zone = new Zone("Overworld.11.9.0");
            OverworldZoneManager.CreateDetached(content.Factory, 64, true).SetActiveZone(zone);
            foreach (var cell in zone.Cells) cell.IsVisible = cell.Explored = true;
            actor = content.Create("MarlbackPatchbearer");
            Assert.True(zone.AddEntity(actor, 8, 9));
            works = new Texture2D(144, 24); motion = new Texture2D(128, 24);
            art = new FlatPixelPatchbearerMotionCatalog(works, motion);
        }

        [TearDown] public void Cleanup()
        {
            try
            {
                art?.Dispose();
                if (works != null) Object.DestroyImmediate(works);
                if (motion != null) Object.DestroyImmediate(motion);
            }
            finally { content?.Dispose(); }
        }

        EntityVisualAsset Resolve(Entity owner)
        {
            Assert.True(art.TryGetAsset(zone, owner, out var value));
            Assert.NotNull(value); return value;
        }

        [TestCase(true)] [TestCase(false)]
        public void MalformedWorksWidthOrMotionHeightCannotBorrowAValidPeerAtlas(bool wrongWorks)
        {
            var valid = Resolve(actor);
            var malformed = wrongWorks ? new Texture2D(143, 24) : new Texture2D(128, 25);
            try
            {
                using (var invalid = new FlatPixelPatchbearerMotionCatalog(
                    wrongWorks ? malformed : works, wrongWorks ? motion : malformed))
                {
                    Assert.False(invalid.TryGetAsset(zone, actor, out var denied));
                    Assert.IsNull(denied);
                }
                Assert.True(malformed != null && works != null && motion != null);
                Assert.AreSame(valid, Resolve(actor), "A rejected independent catalog must not damage the valid one.");
            }
            finally { Object.DestroyImmediate(malformed); }
        }

        [Test] public void NullContextsRefuseAndDoNotPoisonTheNextValidLookup()
        {
            var valid = Resolve(actor);
            Assert.False(art.TryGetAsset(null, actor, out var noZone)); Assert.IsNull(noZone);
            Assert.False(art.TryGetAsset(zone, null, out var noOwner)); Assert.IsNull(noOwner);
            Assert.False(art.TryGetAsset(null, null, out var neither)); Assert.IsNull(neither);
            Assert.AreSame(valid, Resolve(actor));
        }

        [TestCase(true)] [TestCase(false)]
        public void DestroyedBorrowedTextureRevokesTheCachedAssetWithoutDestroyingTheOther(bool destroyWorks)
        {
            var asset = Resolve(actor);
            var idle = asset.GetFrame(EntityVisualState.Idle, EntityVisualFacing.East, 0);
            var step = asset.GetFrame(EntityVisualState.Walk, EntityVisualFacing.East, 0);
            var destroyed = destroyWorks ? works : motion;
            var survivor = destroyWorks ? motion : works;
            Object.DestroyImmediate(destroyed);
            Assert.True(destroyed == null); Assert.False(ReferenceEquals(destroyed, null));
            Assert.False(art.TryGetAsset(zone, actor, out var denied)); Assert.IsNull(denied);
            Assert.True(survivor != null);
            art.Dispose();
            Assert.True(idle == null && step == null, "Catalog still owns its slices after a borrowed texture is destroyed.");
            Assert.True(survivor != null);
        }

        [Test] public void DuplicateTextIdentifierCannotBorrowPlacementOrAnotherActorsMedicineState()
        {
            var full = Resolve(actor);
            var other = content.Create("MarlbackPatchbearer"); other.ID = actor.ID;
            var tonic = other.GetPart<FieldMedicinePart>().FindCarriedMedicine(); Assert.NotNull(tonic);
            Assert.True(other.GetPart<InventoryPart>().RemoveObject(tonic));
            Assert.False(art.TryGetAsset(zone, other, out var unplaced)); Assert.IsNull(unplaced);
            Assert.AreSame(full, Resolve(actor));
            Assert.True(zone.AddEntity(other, 9, 9));
            var empty = Resolve(other); Assert.AreNotSame(full, empty);
            Assert.AreEqual(new Rect(128, 0, 16, 24), empty.GetFrame(EntityVisualState.Idle, EntityVisualFacing.East, 0).rect);
            Assert.AreSame(full, Resolve(actor));
            Assert.True(zone.RemoveEntity(other));
            Assert.False(art.TryGetAsset(zone, other, out var removed)); Assert.IsNull(removed);
            Assert.AreSame(full, Resolve(actor));
        }
    }
}
