using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    // Public-presentation RED: compiles against the pre-continuity API.
    public sealed class FlatPixelSillMemoryTests
    {
        const int X = 10, Y = 10;
        const string Sill = "Overworld.10.10.0";
        const string Environment = "Sprites/Live16x24/sill-environment";
        const string Envelope = "Sprites/Live16x24/sill-envelope";
        static readonly Color MemoryTint = new Color(.35f, .35f, .35f, 1);
        DensityLootTestScope content;
        OverworldZoneManager manager;
        Zone zone;
        FlatPixelGreyboxCatalog world;
        FlatPixelSillEnvelopeCatalog injected;
        FlatPixelFallbackCatalog selector;

        [SetUp] public void Setup()
        {
            content = new DensityLootTestScope();
            manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
            zone = Attach(Sill); world = new FlatPixelGreyboxCatalog();
            Assert.True(FlatPixelSillEnvelopeCatalog.IsSupportedZone(zone));
        }
        [TearDown] public void Cleanup()
        { world?.Dispose(); injected?.Dispose(); selector?.Dispose(); content?.Dispose(); }
        Zone Attach(string id)
        {
            var next = new Zone(id); manager.SetActiveZone(next);
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                next.GetCell(x, y).Explored = next.GetCell(x, y).IsVisible = true;
            return next;
        }
        Entity Add(string blueprint, int x = X, int y = Y)
        {
            var owner = content.Factory.CreateEntity(blueprint); Assert.NotNull(owner);
            Assert.True(zone.AddEntity(owner, x, y)); return owner;
        }
        FlatPixelGreyboxCell View()
        { Assert.True(world.TryResolvePresentation(zone, X, Y, out var value)); return value; }
        void Remember() => zone.GetCell(X, Y).IsVisible = false;
        static Tile Material(FlatPixelGreyboxCell value, bool ground)
            => ground ? value.GroundTile : value.ForegroundTile;
        static void Frame(Tile tile, string path, int first, int last)
        {
            Assert.NotNull(tile); Assert.NotNull(tile.sprite);
            Assert.AreSame(Resources.Load<Texture2D>(path), tile.sprite.texture);
            Assert.That(tile.sprite.rect.x / 16, Is.InRange((float)first, (float)last));
            Assert.AreEqual(new Vector2(16, 24), tile.sprite.rect.size);
            Assert.AreEqual(new Vector2(8, 12), tile.sprite.pivot);
            Assert.AreEqual(16, tile.sprite.pixelsPerUnit);
            Assert.AreEqual(TileFlags.None, tile.flags);
            Assert.AreEqual(Tile.ColliderType.None, tile.colliderType);
        }
        void FallbackOnly()
        {
            Assert.True(world.TryResolveFallback(zone, X, Y, out var fallback));
            var value = View(); Assert.False(value.NativeForeground); Assert.False(value.LocalGround);
            Assert.AreSame(fallback.Owner, value.Owner);
            Assert.AreSame(fallback.ForegroundTile, value.ForegroundTile);
            Assert.AreSame(fallback.GroundTile, value.GroundTile);
            Assert.AreEqual(fallback.ForegroundColor, value.ForegroundColor);
            Assert.AreEqual(fallback.GroundColor, value.GroundColor);
        }

        [TestCase("Grass", true, Environment, 0, 3)]
        [TestCase("StoneFloor", true, Environment, 4, 7)]
        [TestCase("StoneWall", false, Environment, 8, 8)]
        [TestCase("VineWall", false, Envelope, 0, 0)]
        public void SelectedStaticMemoryReusesApprovedMaterialWithOneDim(string blueprint, bool ground, string path, int first, int last)
        {
            var owner = Add(blueprint); var visible = View();
            Frame(Material(visible, ground), path, first, last);
            Remember(); var memory = View();
            Assert.True(memory.Remembered); Assert.AreSame(owner, memory.Owner);
            Frame(Material(memory, ground), path, first, last);
            Assert.AreSame(Material(visible, ground), Material(memory, ground));
            Assert.AreEqual(MemoryTint, ground ? memory.GroundColor : memory.ForegroundColor);
            Assert.False(memory.BlocksMovement, "Memory is not collision authority.");
            if (ground) { Assert.True(memory.LocalGround); Assert.IsNull(memory.ForegroundTile); }
            else Assert.True(memory.NativeForeground);
            Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, X, Y, out var portrait, out _));
            Assert.AreNotSame(Resources.Load<Texture2D>(path), portrait.texture,
                "Memory must not authorize a native live subject portrait.");
        }

        [Test] public void RememberedVineIsIsolatedRegardlessOfVisibleOrHiddenNeighborChanges()
        {
            Add("VineWall"); var east = Add("VineWall", X + 1); Frame(View().ForegroundTile, Envelope, 2, 2);
            Remember(); var memory = View(); Frame(memory.ForegroundTile, Envelope, 0, 0);
            zone.GetCell(X + 1, Y).IsVisible = false;
            east.GetPart<DestructiblePart>().HP = 0; east.GetPart<DestructiblePart>().Gone = true;
            Assert.AreSame(memory.ForegroundTile, View().ForegroundTile);
            Assert.True(zone.RemoveEntity(east)); Add("StoneWall", X + 1);
            Add("VineWall", X, Y - 1); Add("VineWall", X - 1); Add("VineWall", X, Y + 1);
            Assert.AreSame(memory.ForegroundTile, View().ForegroundTile);
            zone.GetCell(X, Y).IsVisible = true; Frame(View().ForegroundTile, Envelope, 13, 13);
        }

        [TestCase("Grass", true)] [TestCase("StoneWall", false)]
        public void HiddenMutableValuesDoNotChangeStaticMemory(string blueprint, bool ground)
        {
            var owner = Add(blueprint); Remember(); var before = View();
            Frame(Material(before, ground), Environment, ground ? 0 : 8, ground ? 3 : 8);
            if (ground) owner.AddPart(new CultivatedSoilPart());
            else
            {
                owner.GetPart<DestructiblePart>().HP = 0;
                owner.GetPart<DestructiblePart>().Gone = true;
                owner.GetPart<PhysicsPart>().Solid = false;
            }
            var thermal = owner.GetPart<ThermalPart>();
            if (thermal == null) { thermal = new ThermalPart(); owner.AddPart(thermal); }
            thermal.Temperature = float.NaN;
            zone.TileState.WriteCoating(X, Y, "water", ZoneTileState.Permanent);
            zone.TileState.WriteCoating(X, Y, "oil", 5);
            var after = View(); Assert.AreSame(Material(before, ground), Material(after, ground));
            Assert.AreEqual(MemoryTint, ground ? after.GroundColor : after.ForegroundColor);
            zone.GetCell(X, Y).IsVisible = true;
            Assert.AreNotSame(Material(before, ground), Material(View(), ground),
                "Disclosed cultivation/coating or broken wall must recover the existing live refusal.");
        }

        [Test] public void MemoryUsesOnlySelectedStaticOwnerAndNeverResolvesHiddenLowerGround()
        {
            var grass = Add("Grass"); var floor = Add("StoneFloor"); Remember();
            Assert.AreSame(floor, View().Owner); Frame(View().GroundTile, Environment, 4, 7);
            floor.GetPart<RenderPart>().Visible = false;
            Assert.AreSame(grass, View().Owner); Frame(View().GroundTile, Environment, 0, 3);
            var wall = Add("StoneWall"); var memory = View(); Assert.AreSame(wall, memory.Owner);
            Frame(memory.ForegroundTile, Environment, 8, 8);
            Assert.True(world.TryResolveFallback(zone, X, Y, out var fallback));
            Assert.AreSame(fallback.GroundTile, memory.GroundTile);
            Assert.AreEqual(fallback.GroundColor, memory.GroundColor);
            grass.AddPart(new CultivatedSoilPart()); grass.GetPart<RenderPart>().VisualVariant = "hidden-change";
            Assert.AreSame(memory.GroundTile, View().GroundTile);
            Assert.AreSame(memory.ForegroundTile, View().ForegroundTile);
        }

        [Test] public void LiveFamiliesRemainAbsentWhileTheirUnderlyingStaticMemoryStaysUnchanged()
        {
            foreach (string name in new[] { "VillageDoor", "WaterPuddle", "MarketStall", "Player", "ClaspbeanCrop" })
            {
                zone = Attach(Sill); var grass = Add("Grass"); Remember(); var baseline = View();
                Frame(baseline.GroundTile, Environment, 0, 3);
                var live = Add(name); live.GetPart<RenderPart>().RenderLayer = 1;
                var after = View(); Assert.AreSame(grass, after.Owner, name);
                Assert.AreSame(baseline.GroundTile, after.GroundTile, name);
                Assert.IsNull(after.ForegroundTile, name); Assert.False(after.NativeForeground, name);
                Assert.AreEqual(baseline.GroundColor, after.GroundColor, name);
            }
        }

        [TestCase("custom")] [TestCase("foreign-render")]
        [TestCase("duplicate-physics")] [TestCase("wide-footprint")]
        public void InvalidStaticMemoryIdentityKeepsExactFallback(string fault)
        {
            var grass = Add("Grass"); Remember(); Frame(View().GroundTile, Environment, 0, 3);
            if (fault == "custom") grass.GetPart<RenderPart>().VisualVariant = "custom-memory";
            if (fault == "foreign-render") grass.GetPart<RenderPart>().ParentEntity = new Entity();
            if (fault == "duplicate-physics") grass.AddPart(new PhysicsPart());
            if (fault == "wide-footprint") grass.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;1,0" });
            FallbackOnly();
        }

        [Test] public void MemoryRequiresExploredCurrentExactSillGraph()
        {
            Add("Grass"); Remember(); Frame(View().GroundTile, Environment, 0, 3);
            zone.GetCell(X, Y).Explored = false; Assert.False(world.TryResolvePresentation(zone, X, Y, out _));
            zone.GetCell(X, Y).Explored = true;
            manager.CachedZones[Sill] = new Zone(Sill); Assert.False(world.TryResolvePresentation(zone, X, Y, out _));
            manager.CachedZones[Sill] = zone; Frame(View().GroundTile, Environment, 0, 3);
            manager.WorldMap.GetPOI(10, 10).Name = "Other village"; FallbackOnly();
            zone = Attach("Overworld.7.8.0"); Add("Grass"); Remember(); FallbackOnly();
        }

        void Inject(Texture2D materials, bool legacy)
        {
            world.Dispose(); injected?.Dispose(); selector?.Dispose();
            world = new FlatPixelGreyboxCatalog((Texture2D)null); selector = new FlatPixelFallbackCatalog();
            injected = new FlatPixelSillEnvelopeCatalog(
                legacy ? Resources.Load<Texture2D>(Envelope) : null,
                legacy ? Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.CellarResourcePath) : null,
                null, legacy ? Resources.Load<Texture2D>(FlatPixelCellarFloorCatalog.ResourcePath) : null,
                selector, materials);
            typeof(FlatPixelGreyboxCatalog).GetField("_sillEnvelopeArt", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(world, injected);
        }
        [Test] public void MissingOrMalformedMemoryArtKeepsIndependentLegacyOrGlyphFallback()
        {
            var wrong = new Texture2D(143, 24);
            try
            {
                foreach (var optional in new Texture2D[] { null, wrong })
                {
                    zone = Attach(Sill); Add("Grass"); Remember(); Inject(optional, true);
                    Frame(View().GroundTile, Envelope, 20, 23); Assert.AreEqual(MemoryTint, View().GroundColor);
                    Inject(optional, false); FallbackOnly();
                }
                zone = Attach(Sill); Add("VineWall"); Remember();
                Inject(Resources.Load<Texture2D>(Environment), false); FallbackOnly();
            }
            finally { UnityEngine.Object.DestroyImmediate(wrong); }
        }

        [Test] public void WarmMemoryQueriesAreReadOnlyAndDisposeOnlyCachedViews()
        {
            var source = new Texture2D(144, 24);
            try
            {
                var wall = Add("StoneWall"); var probe = new RenderProbe(); wall.AddPart(probe);
                Remember(); Inject(source, false); var first = View();
                Assert.True(first.NativeForeground); Assert.AreSame(source, first.ForegroundTile.sprite.texture);
                string state = zone.TileState.ToSaveString(); int version = zone.EntityVersion;
                int hp = wall.GetPart<DestructiblePart>().HP;
                for (int i = 0; i < 256; i++) Assert.AreSame(first.ForegroundTile, View().ForegroundTile);
                Assert.AreEqual(state, zone.TileState.ToSaveString()); Assert.AreEqual(version, zone.EntityVersion);
                Assert.AreEqual(hp, wall.GetPart<DestructiblePart>().HP); Assert.Zero(probe.Events);
                Assert.AreEqual(FilterMode.Bilinear, source.filterMode);
                var tile = first.ForegroundTile; var sprite = tile.sprite;
                world.Dispose(); world.Dispose(); Assert.True(tile == null); Assert.True(sprite == null);
                Assert.True(source != null); Assert.True(selector.TryResolve(zone, X, Y, out var fallback));
                Assert.AreSame(wall, fallback.Owner);
            }
            finally { world.Dispose(); UnityEngine.Object.DestroyImmediate(source); }
        }
        sealed class RenderProbe : Part
        {
            public override string Name => "SillMemoryRenderProbe";
            public int Events;
            public override bool HandleEvent(GameEvent e) { if (e.ID == "Render") Events++; return true; }
        }
    }
}
