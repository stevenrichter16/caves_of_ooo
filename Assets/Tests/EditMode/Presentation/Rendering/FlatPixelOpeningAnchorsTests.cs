using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelOpeningAnchorsTests
    {
        private EntityFactory _factory;
        private Texture2D _baseAtlas, _castAtlas, _anchors;
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
            _anchors = new Texture2D(192, 24, TextureFormat.RGBA32, false);
            _zone = new Zone(ReferenceGladePlan.ZoneID);
            Reveal(X, Y);
        }
        [TearDown]
        public void TearDown()
        {
            _catalog?.Dispose();
            if (_baseAtlas != null) Object.DestroyImmediate(_baseAtlas);
            if (_castAtlas != null) Object.DestroyImmediate(_castAtlas);
            if (_anchors != null) Object.DestroyImmediate(_anchors);
        }

        [TestCase("RepairLinedWell", 0)]
        [TestCase("RepairLinedWell", 1)]
        [TestCase("SpreadDrawPoint", 2)]
        [TestCase("SpreadDrawPoint", 3)]
        [TestCase("StairsDown", 4)]
        [TestCase("FallenBeam", 5)]
        [TestCase("Chest", 6)]
        [TestCase("WoodenBarrel", 7)]
        [TestCase("Signpost", 8)]
        [TestCase("MushroomRing", 9)]
        [TestCase("GlowQuartzVein", 10)]
        [TestCase("Wall", 11)]
        public void ExactSupportedOwnersResolveTheirNativeStateWithoutChangingCollision(string blueprint, int frame)
        {
            var owner = AddForFrame(blueprint, frame);
            CreateCatalog();
            var result = Resolve();
            AssertFrame(result, frame);
            Assert.That(result.Owner, Is.SameAs(owner));
            Assert.That(result.ForegroundColor, Is.EqualTo(Color.white));
            Assert.That(result.BlocksMovement, Is.EqualTo(_zone.GetCell(X, Y).BlocksMovement()));
            Assert.That(Resolve().ForegroundTile, Is.SameAs(result.ForegroundTile));
        }

        [TestCase("RepairLinedWell", "Well")]
        [TestCase("RepairLinedWell", "Repairable")]
        [TestCase("SpreadDrawPoint", "LiquidPool")]
        [TestCase("StairsDown", "StairsDown")]
        [TestCase("FallenBeam", "Physics")]
        [TestCase("FallenBeam", "Handling")]
        [TestCase("Chest", "Container")]
        [TestCase("WoodenBarrel", "Container")]
        [TestCase("MushroomRing", "Harvestable")]
        [TestCase("GlowQuartzVein", "Harvestable")]
        public void MissingOrForeignFunctionalPartsCannotAdvertiseUsableAnchor(string blueprint, string partName)
        {
            var owner = Add(blueprint, blueprint == "GlowQuartzVein" ? "reference-glade-lit-wall" : "");
            CreateCatalog();
            Assert.That(Resolve().ForegroundTile.sprite.texture, Is.SameAs(_anchors), "Require the valid positive control first.");
            var part = owner.GetPart(partName);
            Assert.That(part, Is.Not.Null);
            part.ParentEntity = new Entity();
            AssertFallback(Resolve());
            part.ParentEntity = owner;
            Assert.That(Resolve().ForegroundTile.sprite.texture, Is.SameAs(_anchors));
            Assert.That(owner.RemovePart(part), Is.True);
            AssertFallback(Resolve());
        }

        [TestCase("wrong-recipe")]
        [TestCase("portable")]
        public void IncompatibleRepairMetadataDoesNotPretendToBeALinedWell(string fault)
        {
            var owner = Add("RepairLinedWell");
            var repair = owner.GetPart<RepairablePart>();
            Assert.That(repair.RecipeId, Is.EqualTo("clay-well-lining"));
            if (fault == "wrong-recipe") repair.RecipeId = "rope-well-fault";
            else repair.PortableEquipment = true;
            CreateCatalog();
            AssertFallback(Resolve());
        }

        [Test]
        public void RepairAndWaterStatePairsAreFreshWhileSurroundingGroundRemainsIndependent()
        {
            var well = Add("RepairLinedWell");
            CreateCatalog();
            AssertFrame(Resolve(), 0);
            well.GetPart<RepairablePart>().Repaired = true;
            AssertFrame(Resolve(), 1);
            _zone.RemoveEntity(well);
            var basin = Add("SpreadDrawPoint");
            _zone.TileState.WriteCoating(X, Y, "water", ZoneTileState.Permanent);
            string before = _zone.TileState.ToSaveString();
            var filled = Resolve();
            AssertFrame(filled, 2);
            basin.GetPart<LiquidPoolPart>().Volume = 0;
            var empty = Resolve();
            AssertFrame(empty, 3);
            Assert.That(empty.GroundKind, Is.EqualTo(FlatPixelGreyboxGround.Water));
            Assert.That(empty.GroundTile, Is.SameAs(filled.GroundTile));
            Assert.That(_zone.TileState.ToSaveString(), Is.EqualTo(before));
            basin.GetPart<LiquidPoolPart>().Volume = -1;
            AssertFrame(Resolve(), 3);
            basin.GetPart<LiquidPoolPart>().Volume = 2;
            basin.GetPart<LiquidPoolPart>().LiquidId = "acid";
            AssertFallback(Resolve());
        }

        [TestCase("MushroomRing", "Mushroom", 9)]
        [TestCase("GlowQuartzVein", "GlowQuartz", 10)]
        public void HarvestSourceRequiresExpectedUnspentYieldAndNativeHarvestRemovesItsArt(string blueprint, string yield, int frame)
        {
            var source = AddForFrame(blueprint, frame);
            var harvest = source.GetPart<HarvestablePart>();
            Assert.That(harvest.YieldBlueprint, Is.EqualTo(yield));
            CreateCatalog();
            AssertFrame(Resolve(), frame);
            harvest.YieldBlueprint = "RawMeat";
            AssertFallback(Resolve());
            harvest.YieldBlueprint = yield;
            harvest.Harvested = true;
            AssertFallback(Resolve());
            harvest.Harvested = false;
            AssertFrame(Resolve(), frame);
            var actor = _factory.CreateEntity("Player");
            Assert.That(_zone.AddEntity(actor, X + 1, Y), Is.True);
            var oldFactory = HarvestablePart.Factory;
            try
            {
                HarvestablePart.Factory = _factory;
                Assert.That(DensityHarvestSecurityTests.Act(source, actor, _zone), Is.True);
            }
            finally { HarvestablePart.Factory = oldFactory; }
            Assert.That(harvest.Harvested, Is.True);
            Assert.That(_zone.GetEntityCell(source), Is.Null);
            Assert.That(Resolve().Owner, Is.Not.SameAs(source));
            if (Resolve().ForegroundTile != null) AssertFallback(Resolve());
        }

        [Test]
        public void SharedLitWallVisualIdNeverConfusesMasonryWithMineralOrDroppedQuartz()
        {
            var wall = Add("Wall", "reference-glade-lit-wall");
            CreateCatalog();
            AssertFrame(Resolve(), 11);
            _zone.RemoveEntity(wall);
            var vein = Add("GlowQuartzVein", "reference-glade-lit-wall");
            AssertFrame(Resolve(), 10);
            vein.GetPart<RenderPart>().VisualID = "foreign-mineral";
            AssertFallback(Resolve());
            _zone.RemoveEntity(vein);
            Add("GlowQuartz", "reference-glade-lit-wall");
            AssertFallback(Resolve());
        }

        [TestCase("RepairLinedWell")]
        [TestCase("SpreadDrawPoint")]
        [TestCase("MushroomRing")]
        [TestCase("GlowQuartzVein")]
        public void RememberedLowTerrainCannotRevealCurrentFunctionalState(string blueprint)
        {
            // Deliberately uses the existing single-atlas API so this visibility
            // regression can fail before the new anchor constructor exists.
            var owner = Add(blueprint, "reference-glade-lit-wall");
            owner.GetPart<RenderPart>().RenderLayer = 0;
            owner.SetTag("Terrain");
            _catalog = new FlatPixelGreyboxCatalog(null);
            Assert.That(Resolve().Owner, Is.SameAs(owner));
            _zone.GetCell(X, Y).IsVisible = false;
            Assert.That(Resolve().Owner, Is.Null);
            Assert.That(Resolve().ForegroundTile, Is.Null);
        }

        [Test]
        public void OrdinaryRememberedWallStillAppearsDimmedWithNoLiveCollisionClaim()
        {
            Add("Wall", "reference-glade-lit-wall").GetPart<RenderPart>().RenderLayer = 1;
            CreateCatalog();
            _zone.GetCell(X, Y).IsVisible = false;
            var memory = Resolve();
            AssertFrame(memory, 11);
            Assert.That(memory.ForegroundColor, Is.EqualTo(new Color(.35f, .35f, .35f, 1)));
            Assert.That(memory.BlocksMovement, Is.False);
        }

        [Test]
        public void BeamMoveAndContainerReplacementFollowCurrentOwners()
        {
            var beam = Add("FallenBeam");
            CreateCatalog();
            AssertFrame(Resolve(), 5);
            Assert.That(_zone.MoveEntity(beam, X + 1, Y), Is.True);
            Reveal(X + 1, Y);
            Assert.That(Resolve().ForegroundTile, Is.Null);
            Assert.That(_catalog.TryResolve(_zone, X + 1, Y, out var moved), Is.True);
            AssertFrame(moved, 5);
            var chest = Add("Chest");
            AssertFrame(Resolve(), 6);
            var container = chest.GetPart<ContainerPart>();
            var item = _factory.CreateEntity("GlowQuartz");
            Assert.That(container.AddItem(item), Is.True);
            container.Locked = true;
            AssertFrame(Resolve(), 6);
            container.Locked = false;
            Assert.That(container.RemoveItem(item), Is.True);
            AssertFrame(Resolve(), 6);
            _zone.RemoveEntity(chest);
            Add("AshPile");
            AssertFallback(Resolve());
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MissingOrMalformedAnchorsDoNotDisableBaseOrCast(bool malformed)
        {
            var bad = malformed ? new Texture2D(191, 24) : null;
            try
            {
                _catalog = NewCatalog(_baseAtlas, _castAtlas, bad);
                var owner = Add("Player");
                Assert.That(Resolve().ForegroundTile.sprite.texture, Is.SameAs(_baseAtlas));
                _zone.RemoveEntity(owner); owner = Add("PetDog");
                Assert.That(Resolve().ForegroundTile.sprite.texture, Is.SameAs(_castAtlas));
                _zone.RemoveEntity(owner); Add("Chest");
                AssertFallback(Resolve());
            }
            finally { _catalog?.Dispose(); if (bad != null) Object.DestroyImmediate(bad); }
        }

        [Test]
        public void AnchorOnlyAtlasWorksAndEarlierInjectionOverloadsNeverLoadAnchorsImplicitly()
        {
            Add("Chest");
            _catalog = NewCatalog(null, null, _anchors);
            AssertFrame(Resolve(), 6);
            _catalog.Dispose();
            _catalog = new FlatPixelGreyboxCatalog(_baseAtlas, _castAtlas);
            AssertFallback(Resolve());
            _catalog.Dispose();
            _catalog = new FlatPixelGreyboxCatalog(_baseAtlas);
            AssertFallback(Resolve());
        }

        [Test]
        public void DefaultCatalogBindsEveryImportedAnchorStateAlongsideBaseAndCast()
        {
            var anchors = Resources.Load<Texture2D>("Sprites/Live16x24/opening-anchors");
            var baseArt = Resources.Load<Texture2D>("Sprites/Live16x24/opening-base");
            var cast = Resources.Load<Texture2D>("Sprites/Live16x24/opening-cast");
            Assert.That(anchors, Is.Not.Null, "Shipping anchor art must exist; synthetic routing alone is not integration.");
            Assert.That(baseArt, Is.Not.Null);
            Assert.That(cast, Is.Not.Null);
            Assert.That(anchors.width, Is.EqualTo(192));
            Assert.That(anchors.height, Is.EqualTo(24));
            Assert.That(anchors.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(anchors.wrapMode, Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(anchors.mipmapCount, Is.EqualTo(1));
            _catalog = new FlatPixelGreyboxCatalog();
            var names = new[] { "RepairLinedWell", "RepairLinedWell", "SpreadDrawPoint", "SpreadDrawPoint",
                "StairsDown", "FallenBeam", "Chest", "WoodenBarrel", "Signpost", "MushroomRing", "GlowQuartzVein", "Wall" };
            for (int frame = 0; frame < names.Length; frame++)
            {
                var owner = AddForFrame(names[frame], frame);
                var result = Resolve();
                Assert.That(result.ForegroundTile.sprite.texture, Is.SameAs(anchors), names[frame]);
                Assert.That(result.ForegroundTile.sprite.rect, Is.EqualTo(new Rect(frame * 16, 0, 16, 24)));
                Assert.That(result.ForegroundColor, Is.EqualTo(Color.white));
                Assert.That(result.Owner, Is.SameAs(owner));
                Assert.That(_zone.RemoveEntity(owner), Is.True);
            }
            var player = Add("Player");
            Assert.That(Resolve().ForegroundTile.sprite.texture, Is.SameAs(baseArt));
            _zone.RemoveEntity(player);
            Add("PetDog");
            Assert.That(Resolve().ForegroundTile.sprite.texture, Is.SameAs(cast));
        }

        [Test]
        public void AnchorDisposalKeepsBorrowedTextureAndIndependentPeerFramesAlive()
        {
            Add("Chest");
            CreateCatalog();
            var tile = Resolve().ForegroundTile;
            var sprite = tile.sprite;
            using (var peer = NewCatalog(_baseAtlas, _castAtlas, _anchors))
            {
                Assert.That(peer.TryResolve(_zone, X, Y, out var other), Is.True);
                _catalog.Dispose(); _catalog.Dispose();
                Assert.That(tile == null && sprite == null, Is.True);
                Assert.That(_anchors != null && _baseAtlas != null && _castAtlas != null, Is.True);
                Assert.That(other.ForegroundTile != null, Is.True);
                Assert.That(other.ForegroundTile.sprite.texture, Is.SameAs(_anchors));
                Assert.That(_catalog.TryResolve(_zone, X, Y, out _), Is.False);
            }
        }

        private Entity AddForFrame(string blueprint, int frame)
        {
            var owner = Add(blueprint, frame >= 10 ? "reference-glade-lit-wall" : "");
            if (frame == 1) owner.GetPart<RepairablePart>().Repaired = true;
            if (frame == 3) owner.GetPart<LiquidPoolPart>().Volume = 0;
            return owner;
        }
        private Entity Add(string blueprint, string visual = "")
        {
            var owner = _factory.CreateEntity(blueprint);
            Assert.That(owner, Is.Not.Null, blueprint);
            owner.GetPart<RenderPart>().VisualID = visual;
            Assert.That(_zone.AddEntity(owner, X, Y), Is.True);
            return owner;
        }
        private void Reveal(int x, int y) => _zone.GetCell(x, y).Explored = _zone.GetCell(x, y).IsVisible = true;
        private void CreateCatalog() => _catalog = NewCatalog(_baseAtlas, _castAtlas, _anchors);
        private static FlatPixelGreyboxCatalog NewCatalog(Texture2D baseAtlas, Texture2D castAtlas, Texture2D anchors)
        {
            var ctor = typeof(FlatPixelGreyboxCatalog).GetConstructor(new[] { typeof(Texture2D), typeof(Texture2D), typeof(Texture2D) });
            Assert.That(ctor, Is.Not.Null, "Functional anchor atlas must be independently injectable and optional.");
            return (FlatPixelGreyboxCatalog)ctor.Invoke(new object[] { baseAtlas, castAtlas, anchors });
        }
        private FlatPixelGreyboxCell Resolve()
        {
            Assert.That(_catalog.TryResolve(_zone, X, Y, out var result), Is.True);
            return result;
        }
        private void AssertFallback(FlatPixelGreyboxCell cell)
        {
            Assert.That(cell.ForegroundTile, Is.Not.Null);
            Assert.That(cell.ForegroundTile.sprite.texture, Is.Not.SameAs(_anchors));
            Assert.That(cell.ForegroundColor, Is.Not.EqualTo(Color.white));
        }
        private void AssertFrame(FlatPixelGreyboxCell cell, int frame)
        {
            Assert.That(cell.ForegroundTile, Is.Not.Null);
            var sprite = cell.ForegroundTile.sprite;
            Assert.That(sprite.texture, Is.SameAs(_anchors));
            Assert.That(sprite.rect, Is.EqualTo(new Rect(frame * 16, 0, 16, 24)));
            Assert.That(sprite.pixelsPerUnit, Is.EqualTo(16));
            Assert.That(sprite.pivot, Is.EqualTo(new Vector2(8, 12)));
            Assert.That(cell.ForegroundTile.colliderType, Is.EqualTo(Tile.ColliderType.None));
        }
    }
}
