using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>The current North shrine is an underscore. Its replacement is
    /// one neutral owner identity, not a new shrine state or a donation effect.</summary>
    public sealed class FlatPixelRiverShrineTests
    {
        const string North = "Overworld.11.9.0";
        const string Resource = "Sprites/Live16x24/river-shrine";
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
            zone = Attach(North);
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

        static void Frame(Tile tile, Texture2D texture)
        {
            Assert.NotNull(tile); Assert.NotNull(tile.sprite); Assert.AreSame(texture, tile.sprite.texture);
            Assert.AreEqual(new Rect(0, 0, 16, 24), tile.sprite.rect);
            Assert.AreEqual(new Vector2(8, 12), tile.sprite.pivot); Assert.AreEqual(16, tile.sprite.pixelsPerUnit);
            Assert.AreEqual(TileFlags.None, tile.flags); Assert.AreEqual(Tile.ColliderType.None, tile.colliderType);
        }

        void Create(Texture2D texture, bool injectWorld = false)
        {
            var type = typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelRiverShrineCatalog");
            Assert.NotNull(type, "The current river shrine has no optional native identity adapter.");
            var ctor = type.GetConstructor(new[] { typeof(Texture2D) }); Assert.NotNull(ctor);
            adapter = (IDisposable)ctor.Invoke(new object[] { texture });
            query = (Query)Delegate.CreateDelegate(typeof(Query), adapter, type.GetMethod("TryResolve", Instance));
            if (!injectWorld) return;
            world = new FlatPixelGreyboxCatalog((Texture2D)null);
            var field = typeof(FlatPixelGreyboxCatalog).GetField("_riverShrineArt", Instance); Assert.NotNull(field);
            field.SetValue(world, adapter);
        }

        Tile Native(Entity owner)
        { Assert.True(query(owner, out var tile)); Frame(tile, atlas); return tile; }

        [TestCase(North)] [TestCase("Overworld.11.10.0")] [TestCase("Overworld.12.11.0")]
        public void DefaultCurrentOpeningWorldAndReaderShareTheNativeShrine(string id)
        {
            zone = Attach(id); var owner = Add("RiverShrine"); world = new FlatPixelGreyboxCatalog();
            Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            var value = Resolve(); Assert.AreSame(owner, value.Owner);
            Assert.True(value.NativeForeground, "The existing shrine still renders its literal underscore.");
            var imported = Resources.Load<Texture2D>(Resource); Assert.NotNull(imported);
            Assert.AreEqual(16, imported.width); Assert.AreEqual(24, imported.height);
            Assert.AreEqual(FilterMode.Point, imported.filterMode); Assert.AreEqual(TextureWrapMode.Clamp, imported.wrapMode);
            Assert.AreEqual(1, imported.mipmapCount); Frame(value.ForegroundTile, imported);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, 10, 10, out var sprite, out var tint));
            Assert.AreSame(value.ForegroundTile.sprite, sprite); Assert.AreEqual(Color.white, tint);
            Assert.AreSame(value.ForegroundTile, Resolve().ForegroundTile); Assert.False(value.BlocksMovement);
        }

        [TestCase("identity")] [TestCase("missing-sanctuary")] [TestCase("foreign-sanctuary")]
        [TestCase("foreign-render")] [TestCase("invisible")] [TestCase("glyph")]
        [TestCase("visual-id")] [TestCase("visual-variant")] [TestCase("glyph-variants")]
        [TestCase("missing-physics")] [TestCase("foreign-physics")] [TestCase("solid")]
        [TestCase("takeable")] [TestCase("carried")] [TestCase("equipped")]
        [TestCase("creature")] [TestCase("item")] [TestCase("player")] [TestCase("footprint")]
        public void TheIdentityCannotBeBorrowedByAnotherOwnerOrIncompatiblePresentation(string fault)
        {
            Create(atlas); var owner = Add("RiverShrine"); Native(owner);
            var sanctuary = owner.GetPart<SanctuaryPart>(); var render = owner.GetPart<RenderPart>();
            var physics = owner.GetPart<PhysicsPart>();
            if (fault == "identity") owner.BlueprintName = "Shrine";
            if (fault == "missing-sanctuary") Assert.True(owner.RemovePart(sanctuary));
            if (fault == "foreign-sanctuary") sanctuary.ParentEntity = new Entity();
            if (fault == "foreign-render") render.ParentEntity = new Entity();
            if (fault == "invisible") render.Visible = false;
            if (fault == "glyph") render.RenderString = "&";
            if (fault == "visual-id") render.VisualID = "another-shrine";
            if (fault == "visual-variant") render.VisualVariant = "another-shrine";
            if (fault == "glyph-variants") render.GlyphVariants = "_-";
            if (fault == "missing-physics") Assert.True(owner.RemovePart(physics));
            if (fault == "foreign-physics") physics.ParentEntity = new Entity();
            if (fault == "solid") physics.Solid = true;
            if (fault == "takeable") physics.Takeable = true;
            if (fault == "carried") physics.InInventory = new Entity();
            if (fault == "equipped") physics.Equipped = new Entity();
            if (fault == "creature") owner.SetTag("Creature");
            if (fault == "item") owner.SetTag("Item");
            if (fault == "player") owner.SetTag("Player");
            if (fault == "footprint") owner.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;1,0" });
            Assert.False(query(owner, out _), fault);
        }

        [TestCase(12, true)] [TestCase(4, false)]
        public void DonationChangesOnlyTheActorAndDoesNotInventAShrineFrame(int drams, bool paid)
        {
            Create(atlas, true); var shrine = Add("RiverShrine"); var tile = Native(shrine);
            var actor = new Entity { ID = Guid.NewGuid().ToString("N"), BlueprintName = "ShrineVisitor" };
            actor.AddPart(new PhysicsPart()); actor.AddPart(new StatusEffectsPart());
            actor.SetIntProperty(TradeSystem.CURRENCY_PROP, drams); Assert.True(zone.AddEntity(actor, 9, 10));
            var properties = shrine.Properties.ToArray(); var tags = shrine.Tags.ToArray();
            var owners = zone.GetReadOnlyEntities().ToArray();
            for (int i = 0; i < 10; i++)
            {
                Assert.AreSame(tile, Resolve().ForegroundTile);
                Assert.True(world.TryGetPresentationSubjectSprite(zone, shrine, 10, 10, out var sprite, out _));
                Assert.AreSame(tile.sprite, sprite);
            }
            Assert.AreEqual(drams, actor.GetIntProperty(TradeSystem.CURRENCY_PROP, -1));
            Assert.IsNull(actor.GetEffect<StoneskinEffect>(), "Rendering must not dispatch the donation.");
            var action = GameEvent.New("InventoryAction");
            try
            {
                action.SetParameter("Command", "DonateAtShrine"); action.SetParameter("Actor", (object)actor);
                action.SetParameter("Zone", (object)zone); shrine.FireEvent(action); Assert.True(action.Handled);
            }
            finally { action.Release(); }
            Assert.AreEqual(paid ? drams - SanctuaryPart.DonationCost : drams,
                actor.GetIntProperty(TradeSystem.CURRENCY_PROP, -1));
            var blessing = actor.GetEffect<StoneskinEffect>();
            if (paid)
            {
                Assert.NotNull(blessing); Assert.AreEqual(SanctuaryPart.BlessingReduction, blessing.Reduction);
                Assert.AreEqual(SanctuaryPart.BlessingDuration, blessing.Duration);
            }
            else Assert.IsNull(blessing);
            Assert.AreSame(tile, Resolve().ForegroundTile); Assert.AreSame(tile, Native(shrine));
            Assert.IsNull(shrine.GetEffect<StoneskinEffect>());
            CollectionAssert.AreEqual(properties, shrine.Properties.ToArray());
            CollectionAssert.AreEqual(tags, shrine.Tags.ToArray());
            CollectionAssert.AreEqual(owners, zone.GetReadOnlyEntities().ToArray());
            Assert.AreSame(zone.GetCell(10, 10), zone.GetEntityCell(shrine));
            Assert.AreEqual(FilterMode.Bilinear, atlas.filterMode, "Borrowed pixels are not edited.");
        }

        [Test] public void HiddenUnexploredAndStaleGraphDoNotExposeTheShrine()
        {
            Create(atlas, true); Add("RiverShrine"); var cell = zone.GetCell(10, 10);
            Frame(Resolve().ForegroundTile, atlas); cell.IsVisible = false;
            Assert.IsNull(Resolve().Owner); cell.Explored = false;
            Assert.False(world.TryResolvePresentation(zone, 10, 10, out _));
            cell.Explored = cell.IsVisible = true; Frame(Resolve().ForegroundTile, atlas);
            manager.CachedZones[North] = new Zone(North);
            Assert.False(world.TryResolvePresentation(zone, 10, 10, out _));
        }

        [Test] public void CoverMoveAndRemovalRetainTheActualSelectedOwner()
        {
            Create(atlas, true); var shrine = Add("RiverShrine"); var tile = Native(shrine);
            var covering = new Entity { ID = Guid.NewGuid().ToString("N"), BlueprintName = "PassingVisitor" };
            covering.AddPart(new RenderPart { RenderString = "@", RenderLayer = 10 });
            covering.AddPart(new PhysicsPart()); Assert.True(zone.AddEntity(covering, 10, 10));
            Assert.AreSame(covering, Resolve().Owner); Assert.AreNotSame(tile, Resolve().ForegroundTile);
            Assert.True(zone.RemoveEntity(covering)); Assert.AreSame(tile, Resolve().ForegroundTile);
            Assert.True(zone.MoveEntity(shrine, 11, 10)); zone.GetCell(10, 10).Objects.Add(shrine);
            Assert.IsNull(Resolve().Owner, "A stale cell-list entry does not restore world authority.");
            Assert.True(world.TryResolvePresentation(zone, 11, 10, out var moved));
            Assert.AreSame(shrine, moved.Owner); Assert.AreSame(tile, moved.ForegroundTile);
            Assert.True(zone.RemoveEntity(shrine));
            Assert.True(world.TryResolvePresentation(zone, 11, 10, out var removed)); Assert.IsNull(removed.Owner);
        }

        [Test] public void OutsideOpeningKeepsTheCanonicalGlyphAndReader()
        {
            Create(atlas, true); zone = Attach("Overworld.12.12.0"); var shrine = Add("RiverShrine");
            Native(shrine); Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            Assert.AreSame(shrine, Resolve().Owner); Assert.False(Resolve().NativeForeground);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, shrine, 10, 10, out var sprite, out _));
            Assert.AreNotSame(atlas, sprite.texture);
        }

        [TestCase(false)] [TestCase(true)]
        public void MissingOrMalformedOptionalPixelsRetainTheUnderscore(bool malformed)
        {
            var bad = malformed ? new Texture2D(15, 24) : null;
            try
            {
                Create(bad, true); var shrine = Add("RiverShrine"); Assert.False(query(shrine, out _));
                var value = Resolve(); Assert.AreSame(shrine, value.Owner); Assert.False(value.NativeForeground);
                Assert.NotNull(value.ForegroundTile); Assert.AreEqual("_", shrine.GetPart<RenderPart>().RenderString);
            }
            finally { if (bad != null) Object.DestroyImmediate(bad); }
        }

        [Test] public void ExplicitInjectedWorldDoesNotImplicitlyLoadTheNewResource()
        {
            Add("RiverShrine"); world = new FlatPixelGreyboxCatalog((Texture2D)null);
            Assert.False(Resolve().NativeForeground); Assert.NotNull(Resolve().ForegroundTile);
        }

        [Test] public void DisposalReleasesOnlyTheCachedSpriteAndTile()
        {
            Create(atlas); var tile = Native(Add("RiverShrine")); var sprite = tile.sprite;
            adapter.Dispose(); adapter.Dispose(); Assert.True(tile == null); Assert.True(sprite == null);
            Assert.True(atlas != null); Assert.False(query(content.Factory.CreateEntity("RiverShrine"), out _));
        }
    }
}
