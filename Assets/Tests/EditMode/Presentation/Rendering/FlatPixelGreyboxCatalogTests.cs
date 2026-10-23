using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelGreyboxCatalogTests
    {
        private EntityFactory _factory;
        private FlatPixelGreyboxCatalog _catalog;
        private Zone _zone;
        private Entity _ground;
        private const int X = 4, Y = 5;

        [OneTimeSetUp]
        public void LoadContent() => _factory = DensityReferenceGladeTests.Factory();

        [SetUp]
        public void SetUp()
        {
            _catalog = new FlatPixelGreyboxCatalog();
            _zone = new Zone(ReferenceGladePlan.ZoneID);
            _ground = Add("Grass");
            _zone.GetCell(X, Y).Explored = true;
            _zone.GetCell(X, Y).IsVisible = true;
        }

        [TearDown]
        public void TearDown() => _catalog.Dispose();

        [TestCase("Grass", FlatPixelGreyboxKind.Ground)]
        [TestCase("RoadStone", FlatPixelGreyboxKind.Road)]
        [TestCase("Wall", FlatPixelGreyboxKind.Wall)]
        [TestCase("Player", FlatPixelGreyboxKind.Player)]
        [TestCase("Warden", FlatPixelGreyboxKind.Actor)]
        [TestCase("PetDog", FlatPixelGreyboxKind.Actor)]
        [TestCase("MarlbackScrabbler", FlatPixelGreyboxKind.Actor)]
        [TestCase("Reeds", FlatPixelGreyboxKind.Vegetation)]
        [TestCase("Bush", FlatPixelGreyboxKind.Vegetation)]
        [TestCase("Rubble", FlatPixelGreyboxKind.Rubble)]
        [TestCase("BrokenColumn", FlatPixelGreyboxKind.Rubble)]
        [TestCase("Chest", FlatPixelGreyboxKind.Prop)]
        [TestCase("Waterskin", FlatPixelGreyboxKind.LooseItem)]
        [TestCase("CropRow", FlatPixelGreyboxKind.CropSpent)]
        [TestCase("RipeCropRow", FlatPixelGreyboxKind.CropRipe)]
        [TestCase("SpreadDrawPoint", FlatPixelGreyboxKind.WaterSource)]
        public void NativeOwnerChoosesAnHonestGreyboxCategory(string blueprint, FlatPixelGreyboxKind expected)
        {
            _zone.RemoveEntity(_ground);
            var owner = Add(blueprint);

            var cell = Resolve();

            Assert.AreEqual(expected, cell.Kind);
            Assert.AreSame(owner, cell.Owner);
            Assert.AreEqual(_zone.GetCell(X, Y).BlocksMovement(), cell.BlocksMovement);
            Assert.IsFalse(cell.Remembered);
            Assert.NotNull(cell.GroundTile);
            if (expected != FlatPixelGreyboxKind.Ground && expected != FlatPixelGreyboxKind.Road)
                Assert.NotNull(cell.ForegroundTile);
        }

        [Test]
        public void GroundUnderActorFollowsActualWaterCoating_ThenDrainsWithoutInventedPondGeometry()
        {
            var actor = Add("Player");
            var dry = Resolve();
            _zone.TileState.WriteCoating(X, Y, "water", ZoneTileState.Permanent);
            var wet = Resolve();
            Assert.AreSame(actor, wet.Owner);
            Assert.AreEqual(FlatPixelGreyboxKind.Player, wet.Kind);
            Assert.AreEqual(FlatPixelGreyboxGround.Water, wet.GroundKind);
            Assert.AreNotEqual(dry.GroundColor, wet.GroundColor);

            Assert.IsTrue(_zone.TileState.RemoveCoating(X, Y, "water"));
            Assert.AreEqual(dry.GroundKind, Resolve().GroundKind);
            Assert.AreEqual(dry.GroundColor, Resolve().GroundColor);
        }

        [Test]
        public void EmptyFiniteDrawPointShowsAnEmptyBasin_EvenWhenWetGroundRemains()
        {
            var source = Add("SpreadDrawPoint");
            var filled = Resolve();
            Assert.AreEqual(FlatPixelGreyboxKind.WaterSource, filled.Kind);
            source.GetPart<LiquidPoolPart>().Volume = 0;
            var empty = Resolve();
            Assert.AreSame(source, empty.Owner);
            Assert.AreEqual(FlatPixelGreyboxKind.EmptyWaterSource, empty.Kind);
            Assert.AreNotSame(filled.ForegroundTile, empty.ForegroundTile);
            Assert.AreNotEqual(filled.ForegroundColor, empty.ForegroundColor);
            Assert.AreEqual(FlatPixelGreyboxGround.Water, empty.GroundKind,
                "Native pool placement leaves a permanent ground coating after drawing the finite volume; wet ground is not a refill promise.");
        }

        [Test]
        public void CropStagesAndMoistureComeFromTheCurrentPart_NotTheBlueprintName()
        {
            var crop = Add("DrawgourdCrop");
            var growth = crop.GetPart<CropPart>();
            growth.GrowthStage = 0;
            growth.MoistureTicks = 0;
            Assert.AreEqual(FlatPixelGreyboxKind.CropSeed, Resolve().Kind);
            Assert.AreEqual(FlatPixelGreyboxGround.DrySoil, Resolve().GroundKind);
            growth.GrowthStage = 1;
            growth.MoistureTicks = 5;
            Assert.AreEqual(FlatPixelGreyboxKind.CropSprout, Resolve().Kind);
            Assert.AreEqual(FlatPixelGreyboxGround.WetSoil, Resolve().GroundKind);
            growth.GrowthStage = 2;
            Assert.IsTrue(growth.HarvestAtMaturity);
            Assert.AreEqual(FlatPixelGreyboxKind.CropRipe, Resolve().Kind);
            growth.MoistureTicks = 0;
            Assert.AreEqual(FlatPixelGreyboxKind.CropRipe, Resolve().Kind);
            Assert.AreEqual(FlatPixelGreyboxGround.DrySoil, Resolve().GroundKind);
        }

        [Test]
        public void IllegalLegacyCropMaturityOrForeignCropPartCannotAdvertiseHarvest()
        {
            var crop = Add("EmberwheatCrop");
            var growth = crop.GetPart<CropPart>();
            Assert.IsFalse(growth.HarvestAtMaturity);
            growth.GrowthStage = 2;
            Assert.AreEqual(FlatPixelGreyboxKind.Unknown, Resolve().Kind);
            growth.GrowthStage = 1;
            Assert.AreEqual(FlatPixelGreyboxKind.CropSprout, Resolve().Kind);
            growth.ParentEntity = new Entity();
            Assert.AreEqual(FlatPixelGreyboxKind.Unknown, Resolve().Kind);
        }

        [Test]
        public void HarvestedFieldRowShowsSpentState_AndRemovalRestoresActualUnderlyingGround()
        {
            var row = Add("RipeCropRow");
            var ready = Resolve();
            row.GetPart<FieldHarvestPart>().Harvested = true;
            var spent = Resolve();
            Assert.AreEqual(FlatPixelGreyboxKind.CropSpent, spent.Kind);
            Assert.AreNotSame(ready.ForegroundTile, spent.ForegroundTile);
            _zone.RemoveEntity(row);
            var removed = Resolve();
            Assert.AreSame(_ground, removed.Owner);
            Assert.AreEqual(FlatPixelGreyboxKind.Ground, removed.Kind);
            Assert.IsNull(removed.ForegroundTile);
        }

        [Test]
        public void RepairStateAndObstacleStateAreReadFromCurrentOwners()
        {
            var gate = Add("GleanersBuckledWicket");
            var fault = gate.GetPart<RepairablePart>();
            Assert.IsFalse(fault.Repaired);
            var broken = Resolve();
            Assert.AreEqual(FlatPixelGreyboxKind.DamagedProp, broken.Kind);
            Assert.AreEqual(_zone.GetCell(X, Y).BlocksMovement(), broken.BlocksMovement);
            fault.Repaired = true;
            Assert.AreEqual(FlatPixelGreyboxKind.Prop, Resolve().Kind);
            gate.GetPart<PhysicsPart>().Solid = false;
            gate.Tags.Remove("Solid");
            var door = gate.GetPart<DoorPart>();
            if (door != null) door.IsOpen = true;
            Assert.AreEqual(_zone.GetCell(X, Y).BlocksMovement(), Resolve().BlocksMovement);
        }

        [Test]
        public void UnexploredCellsAreRejected_AndMemoryCannotRevealActorsItemsOrCurrentCoatings()
        {
            Add("Player");
            _zone.TileState.WriteCoating(X, Y, "water", ZoneTileState.Permanent);
            var cell = _zone.GetCell(X, Y);
            cell.Explored = false;
            Assert.IsFalse(_catalog.TryResolve(_zone, X, Y, out _));
            cell.Explored = true;
            cell.IsVisible = false;

            var remembered = Resolve();

            Assert.IsTrue(remembered.Remembered);
            Assert.AreSame(_ground, remembered.Owner);
            Assert.AreEqual(FlatPixelGreyboxKind.Ground, remembered.Kind);
            Assert.AreNotEqual(FlatPixelGreyboxGround.Water, remembered.GroundKind);
            Assert.IsFalse(remembered.BlocksMovement, "Do not expose current hidden occupants through topology metadata.");
            Assert.IsNull(remembered.ForegroundTile);
            cell.IsVisible = true;
            Assert.AreEqual(FlatPixelGreyboxKind.Player, Resolve().Kind);
            Assert.AreEqual(FlatPixelGreyboxGround.Water, Resolve().GroundKind);
        }

        [Test]
        public void HiddenAndRemovedTopOwnersDoNotLeaveGhostSprites()
        {
            var item = Add("Waterskin");
            Assert.AreSame(item, Resolve().Owner);
            item.GetPart<RenderPart>().Visible = false;
            Assert.AreSame(_ground, Resolve().Owner);
            item.GetPart<RenderPart>().Visible = true;
            Assert.AreSame(item, Resolve().Owner);
            _zone.RemoveEntity(item);
            Assert.AreSame(_ground, Resolve().Owner);
        }

        [TestCase("Player")]
        [TestCase("Waterskin")]
        [TestCase("DrawgourdCrop")]
        [TestCase("SpreadDrawPoint")]
        public void LowLayerOrTerrainTagCannotMakeLiveOccupantStateVisibleThroughMemory(string blueprint)
        {
            var owner = Add(blueprint);
            owner.GetPart<RenderPart>().RenderLayer = 0;
            owner.SetTag("Terrain");
            Assert.AreSame(owner, Resolve().Owner);
            _zone.GetCell(X, Y).IsVisible = false;

            var remembered = Resolve();

            Assert.AreSame(_ground, remembered.Owner);
            Assert.AreEqual(FlatPixelGreyboxKind.Ground, remembered.Kind);
            Assert.IsNull(remembered.ForegroundTile);
            Assert.IsFalse(remembered.BlocksMovement);
        }

        [Test]
        public void UnknownObjectsGetAnExplicitPlaceholder_WithoutBeingPromotedToKnownServices()
        {
            var owner = new Entity { BlueprintName = "UnrecognizedModObject" };
            owner.AddPart(new RenderPart { RenderString = "?", RenderLayer = 9 });
            _zone.AddEntity(owner, X, Y);
            var cell = Resolve();
            Assert.AreSame(owner, cell.Owner);
            Assert.AreEqual(FlatPixelGreyboxKind.Unknown, cell.Kind);
            Assert.NotNull(cell.ForegroundTile);
        }

        [Test]
        public void AdmissionHonorsAddressAndManagedBiomeAuthority()
        {
            Assert.IsTrue(FlatPixelGreyboxCatalog.IsAdmitted(_zone));
            Assert.IsFalse(FlatPixelGreyboxCatalog.IsAdmitted(null));
            foreach (string id in new[] { "Overworld.11.10.1", "Overworld.11.9.0", "other" })
            {
                var other = new Zone(id);
                other.GetCell(X, Y).Explored = other.GetCell(X, Y).IsVisible = true;
                Assert.IsFalse(FlatPixelGreyboxCatalog.IsAdmitted(other));
                Assert.IsFalse(_catalog.TryResolve(other, X, Y, out _));
            }
            var manager = OverworldZoneManager.CreateDetached(_factory, 64);
            var real = manager.GetZone(ReferenceGladePlan.ZoneID);
            Assert.IsTrue(FlatPixelGreyboxCatalog.IsAdmitted(real));
            manager.WorldMap.Tiles[11, 10] = BiomeType.Sodden;
            Assert.IsFalse(FlatPixelGreyboxCatalog.IsAdmitted(real));
            Assert.IsFalse(_catalog.TryResolve(real, X, Y, out _));
        }

        [Test]
        public void RealSeed64OpeningHasBoundedGreyboxCoverage_WithoutChangingSimulation()
        {
            var manager = OverworldZoneManager.CreateDetached(_factory, 64, enableExploration: true);
            var real = manager.GetZone(ReferenceGladePlan.ZoneID);
            var entities = real.GetReadOnlyEntities().ToArray();
            var positions = entities.Select(e => real.GetEntityPosition(e)).ToArray();
            var kinds = new HashSet<FlatPixelGreyboxKind>();
            var blueprints = new HashSet<string>();
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
            {
                var native = real.GetCell(x, y);
                native.Explored = native.IsVisible = true;
                Assert.IsTrue(_catalog.TryResolve(real, x, y, out var cell), $"{x},{y}");
                Assert.AreSame(native.GetTopVisibleObject(), cell.Owner, $"{x},{y}");
                Assert.NotNull(cell.GroundTile);
                Assert.AreNotEqual(FlatPixelGreyboxKind.Unknown, cell.Kind, cell.Owner?.BlueprintName);
                Assert.AreEqual(native.BlocksMovement(), cell.BlocksMovement);
                kinds.Add(cell.Kind);
                if (cell.Owner != null) blueprints.Add(cell.Owner.BlueprintName);
            }
            Assert.Contains(FlatPixelGreyboxKind.Wall, kinds.ToList());
            Assert.Contains(FlatPixelGreyboxKind.Actor, kinds.ToList());
            Assert.Contains(FlatPixelGreyboxKind.CropRipe, kinds.ToList());
            Assert.Contains("DrawgourdCrop", blueprints.ToList());
            Assert.Contains("GleanersBuckledWicket", blueprints.ToList());
            CollectionAssert.AreEqual(entities, real.GetReadOnlyEntities());
            CollectionAssert.AreEqual(positions, entities.Select(e => real.GetEntityPosition(e)).ToArray());
            TestContext.WriteLine("Seed64 visible owner inventory: " + string.Join(", ", blueprints.OrderBy(x => x)));
        }

        [Test]
        public void AtlasFramesAreNativePixelCells_AndSharedWithinOnlyTheirOwningCatalog()
        {
            var ground = Resolve();
            Add("Player");
            var actor = Resolve();
            Assert.AreSame(ground.GroundTile, actor.GroundTile);
            Assert.AreSame(ground.GroundTile.sprite.texture, actor.ForegroundTile.sprite.texture);
            foreach (var tile in new[] { ground.GroundTile, actor.ForegroundTile })
            {
                var sprite = tile.sprite;
                Assert.AreEqual(new Vector2(16, 24), sprite.rect.size);
                Assert.AreEqual(new Vector2(8, 12), sprite.pivot);
                Assert.AreEqual(16f, sprite.pixelsPerUnit);
                Assert.AreEqual(FilterMode.Point, sprite.texture.filterMode);
                Assert.AreEqual(1, sprite.texture.mipmapCount);
            }
            Assert.IsTrue(actor.ForegroundTile.sprite.texture.GetPixels32().All(p => p.a == 0 || p.a == 255));
            var texture = actor.ForegroundTile.sprite.texture;
            var spriteToDispose = actor.ForegroundTile.sprite;
            var tileToDispose = actor.ForegroundTile;
            using (var other = new FlatPixelGreyboxCatalog())
            {
                Assert.IsTrue(other.TryResolve(_zone, X, Y, out var otherCell));
                Assert.AreNotSame(texture, otherCell.ForegroundTile.sprite.texture);
                _catalog.Dispose();
                Assert.IsTrue(texture == null);
                Assert.IsTrue(spriteToDispose == null);
                Assert.IsTrue(tileToDispose == null);
                Assert.IsTrue(otherCell.ForegroundTile.sprite.texture != null);
                Assert.IsFalse(_catalog.TryResolve(_zone, X, Y, out _));
            }
        }

        [Test]
        public void InvalidCoordinatesDoNotResolveOutsideTheZone()
        {
            Assert.IsFalse(_catalog.TryResolve(null, X, Y, out _));
            Assert.IsFalse(_catalog.TryResolve(_zone, -1, Y, out _));
            Assert.IsFalse(_catalog.TryResolve(_zone, Zone.Width, Y, out _));
            Assert.IsFalse(_catalog.TryResolve(_zone, X, Zone.Height, out _));
        }

        private Entity Add(string blueprint)
        {
            var entity = _factory.CreateEntity(blueprint);
            Assert.NotNull(entity, blueprint);
            Assert.IsTrue(_zone.AddEntity(entity, X, Y));
            return entity;
        }

        private FlatPixelGreyboxCell Resolve()
        {
            Assert.IsTrue(_catalog.TryResolve(_zone, X, Y, out var cell));
            return cell;
        }
    }
}
