using System;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelOpeningArtCatalogTests
    {
        private EntityFactory _factory;
        private Texture2D _atlas;
        private FlatPixelGreyboxCatalog _catalog;
        private Zone _zone;
        private const int X = 4, Y = 6;

        [OneTimeSetUp]
        public void LoadContent() => _factory = DensityReferenceGladeTests.Factory();

        [SetUp]
        public void SetUp()
        {
            _atlas = new Texture2D(160, 24, TextureFormat.RGBA32, false)
            { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            _zone = new Zone(ReferenceGladePlan.ZoneID);
            _zone.GetCell(X, Y).Explored = _zone.GetCell(X, Y).IsVisible = true;
        }

        [TearDown]
        public void TearDown()
        {
            _catalog?.Dispose();
            if (_atlas != null) Object.DestroyImmediate(_atlas);
        }

        [TestCase("Player", "", 0)]
        [TestCase("Wall", "reference-glade-dark-ruin", 1)]
        [TestCase("Wall", "reference-glade-low-wall", 2)]
        [TestCase("Reeds", "reference-glade-pale-reeds", 3)]
        [TestCase("Bush", "reference-glade-green-grass", 5)]
        [TestCase("Rubble", "reference-glade-gravel", 6)]
        [TestCase("BrokenColumn", "reference-glade-dark-ruin", 6)]
        [TestCase("RipeCropRow", "", 7)]
        [TestCase("CropRow", "", 8)]
        [TestCase("Warden", "", 9)]
        public void KnownOpeningOwnersUseTheirExactCachedNativeFrame(string blueprint, string visual, int frame)
        {
            var owner = Add(blueprint, visual);
            CreateCatalog(_atlas);
            var result = Resolve();
            AssertFrame(result, frame);
            Assert.That(result.Owner, Is.SameAs(owner));
            Assert.That(result.ForegroundColor, Is.EqualTo(Color.white));
            Assert.That(Resolve().ForegroundTile, Is.SameAs(result.ForegroundTile));
            Assert.That(result.BlocksMovement, Is.EqualTo(_zone.GetCell(X, Y).BlocksMovement()));
        }

        [Test]
        public void PaleReedVariantsAreStableAndRemainWithinTheirOwnTwoFrames()
        {
            var owner = Add("Reeds", "reference-glade-pale-reeds");
            CreateCatalog(_atlas);
            AssertFrame(Resolve(), 3);
            _zone.RemoveEntity(owner);
            _zone.AddEntity(owner, X + 1, Y);
            _zone.GetCell(X + 1, Y).Explored = _zone.GetCell(X + 1, Y).IsVisible = true;
            Assert.That(_catalog.TryResolve(_zone, X + 1, Y, out var alternate), Is.True);
            AssertFrame(alternate, 4);
            Assert.That(_catalog.TryResolve(_zone, X + 1, Y, out var repeated), Is.True);
            Assert.That(repeated.ForegroundTile, Is.SameAs(alternate.ForegroundTile));
        }

        [Test]
        public void CrossableLowWallRetainsItsRoleWithoutInventingBlocking()
        {
            var owner = Add("Wall", "reference-glade-low-wall");
            owner.Tags.Remove("Wall");
            owner.Tags.Remove("Solid");
            owner.GetPart<PhysicsPart>().Solid = false;
            CreateCatalog(_atlas);
            AssertFrame(Resolve(), 2);
            Assert.That(Resolve().BlocksMovement, Is.False);
        }

        [Test]
        public void HarvestedFieldChangesToStubbleWithoutChangingItsOwnerOrGround()
        {
            var owner = Add("RipeCropRow");
            CreateCatalog(_atlas);
            var ripe = Resolve();
            AssertFrame(ripe, 7);
            owner.GetPart<FieldHarvestPart>().Harvested = true;
            var spent = Resolve();
            AssertFrame(spent, 8);
            Assert.That(spent.Owner, Is.SameAs(owner));
            Assert.That(spent.GroundTile, Is.SameAs(ripe.GroundTile));
            Assert.That(spent.GroundKind, Is.EqualTo(ripe.GroundKind));
            owner.GetPart<FieldHarvestPart>().ParentEntity = new Entity();
            AssertFallback(Resolve());
        }

        [TestCase("DrawgourdCrop", "")]
        [TestCase("GlowQuartz", "reference-glade-lit-wall")]
        [TestCase("GlowQuartzVein", "reference-glade-lit-wall")]
        [TestCase("PetDog", "")]
        [TestCase("MarlbackScrabbler", "")]
        [TestCase("Wall", "foreign-wall")]
        [TestCase("Bush", "foreign-bush")]
        [TestCase("Chest", "reference-glade-low-wall")]
        public void OtherSpeciesServicesAndUnrecognizedVisualRolesRemainExplicitGreyboxes(string blueprint, string visual)
        {
            Add(blueprint, visual);
            CreateCatalog(_atlas);
            AssertFallback(Resolve());
        }

        [Test]
        public void MissingOrMalformedAtlasRetainsWorkingGreyboxFallback()
        {
            Add("Player");
            CreateCatalog(null);
            AssertFallback(Resolve());
            _catalog.Dispose();
            var malformed = new Texture2D(159, 24);
            try
            {
                CreateCatalog(malformed);
                AssertFallback(Resolve());
                Assert.That(Resolve().ForegroundTile.sprite.texture, Is.Not.SameAs(malformed));
            }
            finally { _catalog.Dispose(); Object.DestroyImmediate(malformed); }
        }

        [Test]
        public void VisibleWaterAndRememberedTerrainKeepExistingStateAndDimmingContracts()
        {
            var owner = Add("Wall", "reference-glade-dark-ruin");
            owner.GetPart<RenderPart>().RenderLayer = 1;
            CreateCatalog(_atlas);
            _zone.TileState.WriteCoating(X, Y, "water", 6);
            string before = _zone.TileState.ToSaveString();
            var visible = Resolve();
            AssertFrame(visible, 1);
            Assert.That(visible.GroundKind, Is.EqualTo(FlatPixelGreyboxGround.Water));
            _zone.GetCell(X, Y).IsVisible = false;
            var memory = Resolve();
            AssertFrame(memory, 1);
            Assert.That(memory.ForegroundColor, Is.EqualTo(new Color(.35f, .35f, .35f, 1)));
            Assert.That(memory.GroundKind, Is.Not.EqualTo(FlatPixelGreyboxGround.Water));
            Assert.That(memory.BlocksMovement, Is.False);
            Assert.That(_zone.TileState.ToSaveString(), Is.EqualTo(before));
        }

        [Test]
        public void HiddenActorUnexploredAndForeignZonesCannotGainProductionFrames()
        {
            Add("Player");
            CreateCatalog(_atlas);
            AssertFrame(Resolve(), 0);
            _zone.GetCell(X, Y).IsVisible = false;
            Assert.That(Resolve().ForegroundTile, Is.Null);
            _zone.GetCell(X, Y).Explored = false;
            Assert.That(_catalog.TryResolve(_zone, X, Y, out _), Is.False);
            var other = new Zone("Overworld.12.10.0");
            other.GetCell(X, Y).Explored = other.GetCell(X, Y).IsVisible = true;
            other.AddEntity(_factory.CreateEntity("Player"), X, Y);
            Assert.That(_catalog.TryResolve(other, X, Y, out _), Is.False);
        }

        [Test]
        public void DefaultCatalogBindsTheImportedProductionAtlasWithNativePixelSettings()
        {
            Add("Player");
            var resource = Resources.Load<Texture2D>("Sprites/Live16x24/opening-base");
            Assert.That(resource, Is.Not.Null, "Shipping art must be present; fallback alone is not integration evidence.");
            Assert.That(resource.width, Is.EqualTo(160));
            Assert.That(resource.height, Is.EqualTo(24));
            Assert.That(resource.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(resource.mipmapCount, Is.EqualTo(1));
            Assert.That(resource.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            _catalog = new FlatPixelGreyboxCatalog();
            var result = Resolve();
            Assert.That(result.ForegroundTile.sprite.texture, Is.SameAs(resource));
            Assert.That(result.ForegroundTile.sprite.rect, Is.EqualTo(new Rect(0, 0, 16, 24)));
            Assert.That(result.ForegroundColor, Is.EqualTo(Color.white));
        }

        [Test]
        public void DisposalDestroysOwnedFramesButPreservesBorrowedAtlasAndOtherCatalog()
        {
            Add("Player");
            CreateCatalog(_atlas);
            var first = Resolve().ForegroundTile;
            var firstSprite = first.sprite;
            var second = NewCatalog(_atlas);
            try
            {
                Assert.That(second.TryResolve(_zone, X, Y, out var peer), Is.True);
                _catalog.Dispose();
                _catalog.Dispose();
                Assert.That(first == null, Is.True);
                Assert.That(firstSprite == null, Is.True);
                Assert.That(_atlas != null, Is.True, "A Resources/injected texture is borrowed.");
                Assert.That(peer.ForegroundTile != null, Is.True);
                Assert.That(peer.ForegroundTile.sprite.texture, Is.SameAs(_atlas));
                Assert.That(_catalog.TryResolve(_zone, X, Y, out _), Is.False);
            }
            finally { second.Dispose(); }
        }

        private Entity Add(string blueprint, string visual = "")
        {
            var owner = _factory.CreateEntity(blueprint);
            Assert.That(owner, Is.Not.Null, blueprint);
            owner.GetPart<RenderPart>().VisualID = visual;
            _zone.AddEntity(owner, X, Y);
            return owner;
        }
        private void CreateCatalog(Texture2D atlas) => _catalog = NewCatalog(atlas);
        private static FlatPixelGreyboxCatalog NewCatalog(Texture2D atlas)
        {
            var constructor = typeof(FlatPixelGreyboxCatalog).GetConstructor(new[] { typeof(Texture2D) });
            Assert.That(constructor, Is.Not.Null, "The production atlas must be injectable without Resources or gameplay mutation.");
            return (FlatPixelGreyboxCatalog)constructor.Invoke(new object[] { atlas });
        }
        private FlatPixelGreyboxCell Resolve()
        {
            Assert.That(_catalog.TryResolve(_zone, X, Y, out var result), Is.True);
            return result;
        }
        private void AssertFallback(FlatPixelGreyboxCell result)
        {
            Assert.That(result.ForegroundTile, Is.Not.Null);
            Assert.That(result.ForegroundTile.sprite.texture, Is.Not.SameAs(_atlas));
            Assert.That(result.ForegroundColor, Is.Not.EqualTo(Color.white));
        }
        private void AssertFrame(FlatPixelGreyboxCell result, int frame)
        {
            Assert.That(result.ForegroundTile, Is.Not.Null);
            var sprite = result.ForegroundTile.sprite;
            Assert.That(sprite.texture, Is.SameAs(_atlas));
            Assert.That(sprite.rect, Is.EqualTo(new Rect(frame * 16, 0, 16, 24)));
            Assert.That(sprite.pixelsPerUnit, Is.EqualTo(16));
            Assert.That(sprite.pivot, Is.EqualTo(new Vector2(8, 12)));
            Assert.That(result.ForegroundTile.colliderType, Is.EqualTo(Tile.ColliderType.None));
        }
    }
}
