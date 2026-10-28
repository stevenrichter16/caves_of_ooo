using System;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelOpeningCastTests
    {
        private EntityFactory _factory;
        private Texture2D _baseAtlas, _castAtlas;
        private FlatPixelGreyboxCatalog _catalog;
        private Zone _zone;
        private const int X = 4, Y = 6;

        [OneTimeSetUp]
        public void LoadContent() => _factory = DensityReferenceGladeTests.Factory();

        [SetUp]
        public void SetUp()
        {
            _baseAtlas = new Texture2D(160, 24, TextureFormat.RGBA32, false);
            _castAtlas = new Texture2D(64, 24, TextureFormat.RGBA32, false);
            _zone = new Zone(ReferenceGladePlan.ZoneID);
            Reveal(X, Y);
        }

        [TearDown]
        public void TearDown()
        {
            _catalog?.Dispose();
            if (_baseAtlas != null) Object.DestroyImmediate(_baseAtlas);
            if (_castAtlas != null) Object.DestroyImmediate(_castAtlas);
        }

        [TestCase("Villager", 0)]
        [TestCase("PetDog", 1)]
        [TestCase("MarlbackScrabbler", 2)]
        [TestCase("MarlbackGleaner", 3)]
        public void ExactCreatureOwnersUseDistinctNativeCastFrames(string blueprint, int frame)
        {
            var owner = Add(blueprint);
            CreateCatalog();
            Assert.That(owner.HasTag("Creature"), Is.True);
            var result = Resolve();
            AssertCast(result, frame);
            Assert.That(result.Owner, Is.SameAs(owner));
            Assert.That(result.ForegroundColor, Is.EqualTo(Color.white));
            Assert.That(Resolve().ForegroundTile, Is.SameAs(result.ForegroundTile));
            owner.GetPart<RenderPart>().VisualID = "reference-glade-low-wall";
            if (blueprint == "Villager")
            {
                // Portable neutral residents reject reskins before the older
                // opening route; the other three local cast contracts persist.
                var reskinned = Resolve();
                Assert.That(reskinned.ForegroundTile.sprite.texture, Is.Not.SameAs(_castAtlas));
                Assert.That(reskinned.ForegroundTile.sprite.texture, Is.Not.SameAs(_baseAtlas));
                owner.GetPart<RenderPart>().VisualID = null;
                AssertCast(Resolve(), frame);
            }
            else AssertCast(Resolve(), frame, "A terrain VisualID cannot turn a creature into masonry.");
        }

        [TestCase("Villager")]
        [TestCase("PetDog")]
        [TestCase("MarlbackScrabbler")]
        [TestCase("MarlbackGleaner")]
        public void MissingCreatureTagCannotAdvertiseLivingCastArt(string blueprint)
        {
            var owner = Add(blueprint);
            owner.Tags.Remove("Creature");
            owner.SetTag("creature");
            CreateCatalog();
            AssertFallback(Resolve());
        }

        [TestCase("unknown-identity")]
        [TestCase("foreign-render-part")]
        [TestCase("corpse")]
        public void FamiliarVisualIdDoesNotAuthorizeUnrelatedOwners(string mismatch)
        {
            var owner = Add("MarlbackGleaner");
            owner.GetPart<RenderPart>().VisualID = "reference-glade-low-wall";
            if (mismatch == "unknown-identity") owner.BlueprintName = "UnrelatedModRaider";
            else if (mismatch == "corpse") { owner.BlueprintName = "Corpse"; owner.Tags.Remove("Creature"); }
            else owner.GetPart<RenderPart>().ParentEntity = new Entity();
            CreateCatalog();
            AssertFallback(Resolve());
        }

        [Test]
        public void HiddenAndRememberedActorsCannotLeaveCurrentCastArtBehind()
        {
            var owner = Add("PetDog");
            CreateCatalog();
            AssertCast(Resolve(), 1);
            owner.GetPart<RenderPart>().Visible = false;
            Assert.That(Resolve().ForegroundTile, Is.Null);
            owner.GetPart<RenderPart>().Visible = true;
            owner.GetPart<RenderPart>().RenderLayer = 0;
            owner.SetTag("Terrain");
            _zone.GetCell(X, Y).IsVisible = false;
            Assert.That(Resolve().ForegroundTile, Is.Null);
            _zone.GetCell(X, Y).IsVisible = true;
            AssertCast(Resolve(), 1);
            _zone.GetCell(X, Y).Explored = false;
            Assert.That(_catalog.TryResolve(_zone, X, Y, out _), Is.False);
        }

        [Test]
        public void CurrentCellMoveAndRemovalFollowActualOwnerWithoutGhosts()
        {
            var owner = Add("Villager");
            CreateCatalog();
            var first = Resolve().ForegroundTile;
            Assert.That(_zone.MoveEntity(owner, X + 1, Y), Is.True);
            Reveal(X + 1, Y);
            Assert.That(Resolve().ForegroundTile, Is.Null);
            Assert.That(_catalog.TryResolve(_zone, X + 1, Y, out var moved), Is.True);
            AssertCast(moved, 0);
            Assert.That(moved.ForegroundTile, Is.SameAs(first));
            _zone.RemoveEntity(owner);
            Assert.That(_catalog.TryResolve(_zone, X + 1, Y, out var removed), Is.True);
            Assert.That(removed.ForegroundTile, Is.Null);
        }

        [Test]
        public void NativeDeathRemovesLivingArtRatherThanPaintingDroppedObjectsAsTheActor()
        {
            // A minimal real native entity avoids unrelated town/brain behavior;
            // HandleDeath is the production removal path, not an HP-only simulation.
            var owner = new Entity { ID = "cast-death-control", BlueprintName = "PetDog" };
            owner.SetTag("Creature"); owner.SetTag("NoDropOnDeath");
            owner.AddPart(new RenderPart { RenderLayer = 10, RenderString = "d", DisplayName = "cast dog" });
            owner.AddPart(new PhysicsPart());
            owner.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 1, Min = 0, Max = 1 };
            _zone.AddEntity(owner, X, Y);
            CreateCatalog();
            AssertCast(Resolve(), 1);
            CombatSystem.HandleDeath(owner, null, _zone);
            Assert.That(CombatSystem.IsDeathHandled(owner), Is.True);
            Assert.That(_zone.GetEntityCell(owner), Is.Null);
            var after = Resolve();
            Assert.That(after.Owner, Is.Not.SameAs(owner));
            if (after.ForegroundTile != null) AssertFallback(after);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MissingOrMalformedCastPreservesBaseArt(bool malformed)
        {
            var owner = Add("Player");
            Texture2D wrong = malformed ? new Texture2D(63, 24) : null;
            try
            {
                _catalog = NewCatalog(_baseAtlas, wrong);
                Assert.That(Resolve().ForegroundTile.sprite.texture, Is.SameAs(_baseAtlas));
                _zone.RemoveEntity(owner);
                Add("Villager");
                AssertFallback(Resolve());
            }
            finally { _catalog?.Dispose(); if (wrong != null) Object.DestroyImmediate(wrong); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MissingOrMalformedBasePreservesCastArt(bool malformed)
        {
            var owner = Add("PetDog");
            Texture2D wrong = malformed ? new Texture2D(159, 24) : null;
            try
            {
                _catalog = NewCatalog(wrong, _castAtlas);
                AssertCast(Resolve(), 1);
                _zone.RemoveEntity(owner);
                Add("Player");
                AssertFallback(Resolve());
            }
            finally { _catalog?.Dispose(); if (wrong != null) Object.DestroyImmediate(wrong); }
        }

        [Test]
        public void OneTextureInjectionNeverLoadsCastImplicitly()
        {
            var owner = Add("PetDog");
            _catalog = new FlatPixelGreyboxCatalog(_baseAtlas);
            AssertFallback(Resolve());
            _zone.RemoveEntity(owner); Add("Player");
            Assert.That(Resolve().ForegroundTile.sprite.texture, Is.SameAs(_baseAtlas));
        }

        [Test]
        public void DefaultCatalogBindsBothImportedAtlasesWithoutFallingBackForCast()
        {
            var cast = Resources.Load<Texture2D>("Sprites/Live16x24/opening-cast");
            var baseArt = Resources.Load<Texture2D>("Sprites/Live16x24/opening-base");
            Assert.That(cast, Is.Not.Null, "Shipping cast art must exist; synthetic routing alone is not integration.");
            Assert.That(baseArt, Is.Not.Null);
            Assert.That(cast.width, Is.EqualTo(64));
            Assert.That(cast.height, Is.EqualTo(24));
            Assert.That(cast.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(cast.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(cast.mipmapCount, Is.EqualTo(1));
            _catalog = new FlatPixelGreyboxCatalog();
            var names = new[] { "Villager", "PetDog", "MarlbackScrabbler", "MarlbackGleaner" };
            for (int i = 0; i < names.Length; i++)
            {
                var owner = Add(names[i]);
                var result = Resolve();
                Assert.That(result.ForegroundTile.sprite.texture, Is.SameAs(cast), names[i]);
                Assert.That(result.ForegroundTile.sprite.rect, Is.EqualTo(new Rect(i * 16, 0, 16, 24)));
                Assert.That(result.ForegroundColor, Is.EqualTo(Color.white));
                _zone.RemoveEntity(owner);
            }
            Add("Player");
            Assert.That(Resolve().ForegroundTile.sprite.texture, Is.SameAs(baseArt));
        }

        [Test]
        public void CatalogDisposalReleasesBothFrameSetsButKeepsBorrowedTexturesAndPeers()
        {
            var owner = Add("PetDog");
            CreateCatalog();
            var castTile = Resolve().ForegroundTile;
            var castSprite = castTile.sprite;
            _zone.RemoveEntity(owner); Add("Player");
            var baseTile = Resolve().ForegroundTile;
            var baseSprite = baseTile.sprite;
            using (var peer = NewCatalog(_baseAtlas, _castAtlas))
            {
                Assert.That(peer.TryResolve(_zone, X, Y, out var peerCell), Is.True);
                _catalog.Dispose(); _catalog.Dispose();
                Assert.That(castTile == null && castSprite == null && baseTile == null && baseSprite == null, Is.True);
                Assert.That(_baseAtlas != null && _castAtlas != null, Is.True);
                Assert.That(peerCell.ForegroundTile != null, Is.True);
                Assert.That(_catalog.TryResolve(_zone, X, Y, out _), Is.False);
            }
        }

        private Entity Add(string blueprint)
        {
            var owner = _factory.CreateEntity(blueprint);
            Assert.That(owner, Is.Not.Null, blueprint);
            Assert.That(_zone.AddEntity(owner, X, Y), Is.True);
            return owner;
        }
        private void Reveal(int x, int y) => _zone.GetCell(x, y).Explored = _zone.GetCell(x, y).IsVisible = true;
        private void CreateCatalog() => _catalog = NewCatalog(_baseAtlas, _castAtlas);
        private static FlatPixelGreyboxCatalog NewCatalog(Texture2D baseAtlas, Texture2D castAtlas)
        {
            var ctor = typeof(FlatPixelGreyboxCatalog).GetConstructor(new[] { typeof(Texture2D), typeof(Texture2D) });
            Assert.That(ctor, Is.Not.Null, "Base and cast atlas ownership must be independently injectable.");
            return (FlatPixelGreyboxCatalog)ctor.Invoke(new object[] { baseAtlas, castAtlas });
        }
        private FlatPixelGreyboxCell Resolve()
        {
            Assert.That(_catalog.TryResolve(_zone, X, Y, out var result), Is.True);
            return result;
        }
        private void AssertFallback(FlatPixelGreyboxCell cell)
        {
            Assert.That(cell.ForegroundTile, Is.Not.Null);
            Assert.That(cell.ForegroundTile.sprite.texture, Is.Not.SameAs(_baseAtlas));
            Assert.That(cell.ForegroundTile.sprite.texture, Is.Not.SameAs(_castAtlas));
            Assert.That(cell.ForegroundColor, Is.Not.EqualTo(Color.white));
        }
        private void AssertCast(FlatPixelGreyboxCell cell, int frame, string message = null)
        {
            Assert.That(cell.ForegroundTile, Is.Not.Null, message);
            var sprite = cell.ForegroundTile.sprite;
            Assert.That(sprite.texture, Is.SameAs(_castAtlas), message);
            Assert.That(sprite.rect, Is.EqualTo(new Rect(frame * 16, 0, 16, 24)), message);
            Assert.That(sprite.pixelsPerUnit, Is.EqualTo(16));
            Assert.That(sprite.pivot, Is.EqualTo(new Vector2(8, 12)));
            Assert.That(cell.ForegroundTile.colliderType, Is.EqualTo(Tile.ColliderType.None));
        }
    }
}
