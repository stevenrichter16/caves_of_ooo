using System;
using System.Linq;
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
    /// <summary>Actual kitchen capture shows glyph 6 hives and noisy small court
    /// stones. Art must identify existing owners without creating a hive lifecycle
    /// or moving the court. The first tests exercise the shipped rendering seam.</summary>
    public sealed class FlatPixelKitchenOrchardTests
    {
        const string Kitchen = "Overworld.12.11.0";
        const string HiveResource = "Sprites/Live16x24/orchard-hive";
        const string CourtResource = "Sprites/Live16x24/working-court-v2";
        const string OldCourtResource = "Sprites/Live16x24/working-court";
        const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        delegate bool Query(Entity owner, out Tile tile);
        DensityLootTestScope content;
        OverworldZoneManager manager;
        Zone zone;
        Texture2D atlas;
        IDisposable adapter;
        Query query;
        FlatPixelGreyboxCatalog world;

        [SetUp] public void SetUp()
        {
            content = new DensityLootTestScope();
            manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
            zone = Attach(Kitchen);
            atlas = new Texture2D(16, 24);
        }

        [TearDown] public void TearDown()
        {
            world?.Dispose(); adapter?.Dispose();
            if (atlas != null) Object.DestroyImmediate(atlas);
            content?.Dispose();
        }

        Zone Attach(string id)
        {
            var result = new Zone(id); manager.SetActiveZone(result);
            for (int y = 9; y <= 11; y++) for (int x = 9; x <= 11; x++)
                result.GetCell(x, y).Explored = result.GetCell(x, y).IsVisible = true;
            return result;
        }

        Entity Add(string blueprint)
        {
            var owner = content.Factory.CreateEntity(blueprint); Assert.NotNull(owner);
            Assert.True(zone.AddEntity(owner, 10, 10)); return owner;
        }

        FlatPixelGreyboxCell Resolve()
        { Assert.True(world.TryResolvePresentation(zone, 10, 10, out var value)); return value; }

        static Texture2D Imported(string resource, int width)
        {
            var texture = Resources.Load<Texture2D>(resource); Assert.NotNull(texture, resource);
            Assert.AreEqual(width, texture.width); Assert.AreEqual(24, texture.height);
            Assert.AreEqual(FilterMode.Point, texture.filterMode); Assert.AreEqual(TextureWrapMode.Clamp, texture.wrapMode);
            Assert.AreEqual(1, texture.mipmapCount); return texture;
        }

        static void Frame(Tile tile, Texture2D texture)
        {
            Assert.NotNull(tile); Assert.NotNull(tile.sprite); Assert.AreSame(texture, tile.sprite.texture);
            Assert.AreEqual(new Rect(0, 0, 16, 24), tile.sprite.rect);
            Assert.AreEqual(new Vector2(8, 12), tile.sprite.pivot); Assert.AreEqual(16, tile.sprite.pixelsPerUnit);
            Assert.AreEqual(TileFlags.None, tile.flags); Assert.AreEqual(Tile.ColliderType.None, tile.colliderType);
        }

        void Create(Texture2D texture, bool injectWorld = false)
        {
            var type = typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelOrchardCatalog");
            Assert.NotNull(type, "Existing wild hives have no native optional identity adapter.");
            var ctor = type.GetConstructor(new[] { typeof(Texture2D) }); Assert.NotNull(ctor);
            adapter = (IDisposable)ctor.Invoke(new object[] { texture });
            query = (Query)Delegate.CreateDelegate(typeof(Query), adapter, type.GetMethod("TryResolve", Instance));
            if (!injectWorld) return;
            world = new FlatPixelGreyboxCatalog((Texture2D)null);
            var field = typeof(FlatPixelGreyboxCatalog).GetField("_orchardArt", Instance); Assert.NotNull(field);
            field.SetValue(world, adapter);
        }

        Tile Native(Entity owner)
        { Assert.True(query(owner, out var tile)); Frame(tile, atlas); return tile; }

        [TestCase("Overworld.11.10.0")] [TestCase(GleanersCellarBuilder.ZoneID)]
        [TestCase("Overworld.11.9.0")] [TestCase("Overworld.11.8.0")]
        [TestCase("Overworld.12.10.0")] [TestCase(Kitchen)]
        public void RealHiveUsesTheSameNativeIdentityInEachAdmittedGraphAndReader(string id)
        {
            zone = Attach(id); var owner = Add("Beehive"); world = new FlatPixelGreyboxCatalog();
            Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            var value = Resolve(); Assert.AreSame(owner, value.Owner);
            Assert.True(value.NativeForeground, "The existing hive still renders as its literal glyph 6.");
            var imported = Imported(HiveResource, 16); Frame(value.ForegroundTile, imported);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, 10, 10, out var sprite, out var tint));
            Assert.AreSame(value.ForegroundTile.sprite, sprite); Assert.AreEqual(Color.white, tint);
            Assert.AreSame(value.ForegroundTile, Resolve().ForegroundTile);
            Assert.False(value.BlocksMovement); Assert.False(owner.GetPart<HarvestablePart>().Harvested);
        }

        [TestCase("spent")] [TestCase("wrong-yield")] [TestCase("zero-min")] [TestCase("reversed-range")]
        [TestCase("excess-count")] [TestCase("zero-chance")] [TestCase("excess-chance")]
        [TestCase("foreign-harvest")] [TestCase("missing-harvest")]
        [TestCase("foreign-render")] [TestCase("invisible")] [TestCase("visual-id")]
        [TestCase("visual-variant")] [TestCase("glyph-variants")] [TestCase("glyph")]
        [TestCase("foreign-physics")] [TestCase("missing-physics")] [TestCase("solid")]
        [TestCase("takeable")] [TestCase("carried")] [TestCase("equipped")]
        [TestCase("creature")] [TestCase("item")] [TestCase("player")]
        [TestCase("footprint")] [TestCase("zero-stack")] [TestCase("foreign-stack")]
        [TestCase("identity")]
        public void UnspentExactHiveContractCannotBeBorrowedByOtherOwnersOrStates(string fault)
        {
            Create(atlas); var owner = Add("Beehive"); Native(owner);
            var harvest = owner.GetPart<HarvestablePart>(); var render = owner.GetPart<RenderPart>();
            var physics = owner.GetPart<PhysicsPart>();
            if (fault == "spent") harvest.Harvested = true;
            if (fault == "wrong-yield") harvest.YieldBlueprint = "WildBerries";
            if (fault == "zero-min") harvest.YieldMin = 0;
            if (fault == "reversed-range") harvest.YieldMin = harvest.YieldMax + 1;
            if (fault == "excess-count") harvest.YieldMax = HarvestablePart.MaximumYieldCount + 1;
            if (fault == "zero-chance") harvest.YieldChance = 0;
            if (fault == "excess-chance") harvest.YieldChance = 101;
            if (fault == "foreign-harvest") harvest.ParentEntity = new Entity();
            if (fault == "missing-harvest") Assert.True(owner.RemovePart(harvest));
            if (fault == "foreign-render") render.ParentEntity = new Entity();
            if (fault == "invisible") render.Visible = false;
            if (fault == "visual-id") render.VisualID = "other-hive";
            if (fault == "visual-variant") render.VisualVariant = "other-hive";
            if (fault == "glyph-variants") render.GlyphVariants = "67";
            if (fault == "glyph") render.RenderString = "%";
            if (fault == "foreign-physics") physics.ParentEntity = new Entity();
            if (fault == "missing-physics") Assert.True(owner.RemovePart(physics));
            if (fault == "solid") physics.Solid = true;
            if (fault == "takeable") physics.Takeable = true;
            if (fault == "carried") physics.InInventory = new Entity();
            if (fault == "equipped") physics.Equipped = new Entity();
            if (fault == "creature") owner.SetTag("Creature");
            if (fault == "item") owner.SetTag("Item");
            if (fault == "player") owner.SetTag("Player");
            if (fault == "footprint") owner.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;1,0" });
            if (fault == "zero-stack") owner.AddPart(new StackerPart { StackCount = 0 });
            if (fault == "foreign-stack") { var stack = new StackerPart(); owner.AddPart(stack); stack.ParentEntity = new Entity(); }
            if (fault == "identity") owner.BlueprintName = "UnrelatedWaxObject";
            Assert.False(query(owner, out _), fault);
        }

        [Test] public void PresentationQueriesDoNotSpendHoneyRollYieldOrChangeOwners()
        {
            Create(atlas, true); var owner = Add("Beehive"); var harvest = owner.GetPart<HarvestablePart>();
            harvest.YieldMin = 2; harvest.YieldMax = 4; harvest.YieldChance = 60;
            var original = zone.GetReadOnlyEntities().ToArray(); var tile = Native(owner);
            for (int i = 0; i < 20; i++)
            {
                Assert.AreSame(tile, Resolve().ForegroundTile);
                Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, 10, 10, out var sprite, out _));
                Assert.AreSame(tile.sprite, sprite);
            }
            CollectionAssert.AreEqual(original, zone.GetReadOnlyEntities().ToArray());
            Assert.AreSame(zone.GetCell(10, 10), zone.GetEntityCell(owner));
            Assert.False(harvest.Harvested); Assert.AreEqual(2, harvest.YieldMin);
            Assert.AreEqual(4, harvest.YieldMax); Assert.AreEqual(60, harvest.YieldChance);
            Assert.AreEqual(FilterMode.Bilinear, atlas.filterMode, "Borrowed texture is not edited.");
        }

        [Test] public void FogRemovalStaleGraphAndOutsideOpeningKeepDisclosureAndAdmission()
        {
            Create(atlas, true); var owner = Add("Beehive"); var cell = zone.GetCell(10, 10);
            Frame(Resolve().ForegroundTile, atlas); cell.IsVisible = false;
            Assert.IsNull(Resolve().Owner); cell.Explored = false;
            Assert.False(world.TryResolvePresentation(zone, 10, 10, out _));
            cell.Explored = cell.IsVisible = true; Frame(Resolve().ForegroundTile, atlas);
            Assert.True(zone.MoveEntity(owner, 11, 10)); cell.Objects.Add(owner);
            Assert.IsNull(Resolve().Owner, "Stale cell membership is not world authority.");
            Assert.True(world.TryResolvePresentation(zone, 11, 10, out var moved)); Assert.AreSame(owner, moved.Owner);
            manager.CachedZones[Kitchen] = new Zone(Kitchen);
            Assert.False(world.TryResolvePresentation(zone, 11, 10, out _));
            zone = Attach("Overworld.12.12.0"); owner = Add("Beehive");
            Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone)); Assert.AreSame(owner, Resolve().Owner);
            Assert.False(Resolve().NativeForeground);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, 10, 10, out var sprite, out _));
            Assert.AreNotSame(atlas, sprite.texture);
        }

        [Test] public void SuccessfulHarvestRemovesTheHiveInsteadOfInventingADepletedFrame()
        {
            Create(atlas, true); Add("Grass"); var hive = Add("Beehive");
            var actor = new Entity { ID = Guid.NewGuid().ToString("N"), BlueprintName = "HiveHarvester" };
            actor.AddPart(new PhysicsPart()); actor.AddPart(new InventoryPart()); Assert.True(zone.AddEntity(actor, 9, 10));
            var previous = HarvestablePart.Factory;
            try
            {
                HarvestablePart.Factory = content.Factory; Frame(Resolve().ForegroundTile, atlas);
                Assert.True(DensityHarvestSecurityTests.Act(hive, actor, zone));
                Assert.True(hive.GetPart<HarvestablePart>().Harvested); Assert.IsNull(zone.GetEntityCell(hive));
                Assert.False(query(hive, out _)); Assert.AreNotSame(hive, Resolve().Owner);
                Assert.IsNull(Resolve().ForegroundTile, "The original ground becomes visible after harvest.");
                int count = actor.GetPart<InventoryPart>().Objects.Where(e => e.BlueprintName == "Honeycomb")
                    .Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);
                Assert.That(count, Is.InRange(1, 2)); Assert.False(DensityHarvestSecurityTests.Act(hive, actor, zone));
                Assert.AreEqual(count, actor.GetPart<InventoryPart>().Objects.Where(e => e.BlueprintName == "Honeycomb")
                    .Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1));
            }
            finally { HarvestablePart.Factory = previous; }
        }

        [TestCase(false)] [TestCase(true)]
        public void MissingOrMalformedOptionalHiveArtPreservesTheCanonicalGlyph(bool malformed)
        {
            var bad = malformed ? new Texture2D(15, 24) : null;
            try
            {
                Create(bad, true); var hive = Add("Beehive"); Assert.False(query(hive, out _));
                var value = Resolve(); Assert.AreSame(hive, value.Owner); Assert.False(value.NativeForeground);
                Assert.NotNull(value.ForegroundTile); Assert.AreEqual("6", hive.GetPart<RenderPart>().RenderString);
            }
            finally { if (bad != null) Object.DestroyImmediate(bad); }
        }

        [Test] public void DisposalDestroysCachedFramesButNotTheirBorrowedPixels()
        {
            Create(atlas); var tile = Native(Add("Beehive")); var sprite = tile.sprite;
            adapter.Dispose(); adapter.Dispose(); Assert.True(tile == null); Assert.True(sprite == null);
            Assert.True(atlas != null); Assert.False(query(content.Factory.CreateEntity("Beehive"), out _));
        }

        [Test] public void DefaultCourtUsesQuieterVersionWhileOriginalRemainsRecoverable()
        {
            Add("Grass"); var floor = Add("StoneFloor");
            floor.Properties["SpreadResident.Role"] = floor.Properties["ConnectedSpread.Role"] = "court-floor";
            var original = zone.GetReadOnlyEntities().ToArray(); world = new FlatPixelGreyboxCatalog();
            var value = Resolve(); var revised = Imported(CourtResource, 48); var old = Imported(OldCourtResource, 48);
            Assert.AreSame(revised, value.GroundTile.sprite.texture, "Default rendering still uses the dense pebble atlas.");
            Assert.AreNotSame(old, revised); Assert.AreSame(floor, value.Owner); Assert.IsNull(value.ForegroundTile);
            Assert.AreEqual(new Vector2(16, 24), value.GroundTile.sprite.rect.size);
            Assert.AreEqual(Color.white, value.GroundColor);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, floor, 10, 10, out var sprite, out var tint));
            Assert.AreSame(value.GroundTile.sprite, sprite); Assert.AreEqual(Color.white, tint);
            CollectionAssert.AreEqual(original, zone.GetReadOnlyEntities().ToArray());
            using (var retained = new FlatPixelWorkingCourtCatalog(old))
            {
                Assert.True(retained.TryResolve(zone, 10, 10, out var tile, out var exactFloor));
                Assert.AreSame(old, tile.sprite.texture); Assert.AreSame(floor, exactFloor);
            }
            Assert.True(old != null);
        }

        [Test] public void ExplicitlyInjectedOldWorldDoesNotSilentlyLoadNewOrchardArt()
        {
            var hive = Add("Beehive"); world = new FlatPixelGreyboxCatalog((Texture2D)null);
            var value = Resolve(); Assert.AreSame(hive, value.Owner); Assert.False(value.NativeForeground);
            Assert.NotNull(value.ForegroundTile);
        }
    }
}
