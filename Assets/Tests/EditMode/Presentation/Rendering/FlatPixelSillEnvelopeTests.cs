// Native RED baseline imported before runtime integration.
// Public-renderer fixture reused from FlatPixelHedgeContinuityTests / FlatPixelWorkingCourtTests.
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelSillEnvelopeTests
    {
        const string Sill = "Overworld.10.10.0";
        const string Envelope = "Sprites/Live16x24/sill-envelope";
        const string Stone = "Sprites/Live16x24/cellar-floor";
        const int X = 10, Y = 10;
        DensityLootTestScope content;
        OverworldZoneManager manager;
        FlatPixelGreyboxCatalog world;
        Zone zone;

        [SetUp] public void Setup()
        {
            content = new DensityLootTestScope();
            manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
            zone = Attach(Sill); world = new FlatPixelGreyboxCatalog();
            var poi = manager.WorldMap.GetPOI(10, 10);
            Assert.NotNull(poi); Assert.AreEqual(POIType.Village, poi.Type);
            Assert.AreEqual("Sill", poi.Name); Assert.AreEqual("Villagers", poi.Faction);
            Assert.True(string.IsNullOrEmpty(poi.Profile));
            Assert.AreEqual(BiomeType.Spread, manager.WorldMap.GetBiome(10, 10));
        }
        [TearDown] public void Cleanup() { world?.Dispose(); content?.Dispose(); }
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
        FlatPixelGreyboxCell Resolve(int x = X, int y = Y)
        { Assert.True(world.TryResolvePresentation(zone, x, y, out var value)); return value; }
        static void Frame(Tile tile, string resource, int frame)
        {
            Assert.NotNull(tile); Assert.NotNull(tile.sprite);
            var atlas = Resources.Load<Texture2D>(resource); Assert.NotNull(atlas, resource);
            Assert.AreSame(atlas, tile.sprite.texture);
            Assert.AreEqual(new Rect(frame * 16, 0, 16, 24), tile.sprite.rect);
            Assert.AreEqual(new Vector2(8, 12), tile.sprite.pivot);
            Assert.AreEqual(16, tile.sprite.pixelsPerUnit);
            Assert.AreEqual(TileFlags.None, tile.flags); Assert.AreEqual(Tile.ColliderType.None, tile.colliderType);
        }
        void NativeForeground(string resource, int frame)
        {
            var value = Resolve(); Assert.True(value.NativeForeground);
            Assert.AreEqual(Color.white, value.ForegroundColor); Frame(value.ForegroundTile, resource, frame);
        }
        void IdentityFallback()
        {
            Assert.True(world.TryResolveFallback(zone, X, Y, out var baseline));
            var value = Resolve(); Assert.AreSame(baseline.Owner, value.Owner);
            Assert.False(value.NativeForeground); Assert.AreSame(baseline.ForegroundTile, value.ForegroundTile);
        }
        void NativeGround(string resource)
        {
            var value = Resolve(); var atlas = Resources.Load<Texture2D>(resource); Assert.NotNull(atlas);
            Assert.AreSame(atlas, value.GroundTile.sprite.texture); Assert.AreEqual(Color.white, value.GroundColor);
            Assert.AreEqual(new Vector2(16, 24), value.GroundTile.sprite.rect.size);
        }
        void NotEnvelopeGround()
        {
            var texture = Resolve().GroundTile.sprite.texture;
            Assert.AreNotSame(Resources.Load<Texture2D>(Envelope), texture);
            Assert.AreNotSame(Resources.Load<Texture2D>(Stone), texture);
        }
        void Neighbors(int mask)
        {
            if ((mask & 1) != 0) Add("VineWall", X, Y - 1);
            if ((mask & 2) != 0) Add("VineWall", X + 1, Y);
            if ((mask & 4) != 0) Add("VineWall", X, Y + 1);
            if ((mask & 8) != 0) Add("VineWall", X - 1, Y);
        }

        [TestCase("StoneWall", "Sprites/Live16x24/opening-cellar", 8)]
        [TestCase("AlchemyStill", "Sprites/Live16x24/north-works", 0)]
        public void ExactSillOwnersBorrowAcceptedPixelsWithoutAdmittingOpening(string blueprint, string resource, int frame)
        {
            var owner = Add(blueprint); NativeForeground(resource, frame);
            Assert.AreSame(owner, Resolve().Owner); Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            owner.GetPart<RenderPart>().VisualID = "custom-source"; IdentityFallback();
            owner.GetPart<RenderPart>().VisualID = ""; NativeForeground(resource, frame);
        }

        [TestCase("MarketStall")] [TestCase("Well")] [TestCase("WellGroundMarker")]
        [TestCase("Apothecary")] [TestCase("Hedge")] [TestCase("RepairLinedWell")]
        public void PendingFamiliesDoNotAcquireAnOpeningAlias(string blueprint)
        { Add(blueprint); IdentityFallback(); Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone)); }

        [Test] public void AllCardinalVineShapesDescribeRealWallsAndRetainGaps()
        {
            for (int mask = 0; mask < 16; mask++)
            {
                zone = Attach(Sill); var center = Add("VineWall"); Neighbors(mask);
                Add("VineWall", X - 1, Y - 1); Add("VineWall", X + 2, Y);
                NativeForeground(Envelope, mask); var value = Resolve();
                Assert.AreSame(center, value.Owner); Assert.True(value.BlocksMovement);
                Assert.AreEqual(14, center.GetPart<DestructiblePart>().HP);
                foreach (var at in new[] { (X, Y - 1), (X + 1, Y), (X, Y + 1), (X - 1, Y) })
                    foreach (var owner in zone.GetCell(at.Item1, at.Item2).Objects.ToArray()) zone.RemoveEntity(owner);
                NativeForeground(Envelope, 0);
            }
        }

        [TestCase("hidden")] [TestCase("unexplored")] [TestCase("covered")]
        [TestCase("wrong-owner")] [TestCase("gone")]
        public void AnUnqualifiedNeighborNeverDrawsAConnection(string fault)
        {
            Add("VineWall"); var east = Add("VineWall", X + 1); NativeForeground(Envelope, 2);
            var cell = zone.GetCell(X + 1, Y);
            if (fault == "hidden") cell.IsVisible = false;
            if (fault == "unexplored") cell.Explored = false;
            if (fault == "covered") Add("Dagger", X + 1).GetPart<RenderPart>().RenderLayer = 100;
            if (fault == "wrong-owner") { zone.RemoveEntity(east); Add("StoneWall", X + 1); }
            if (fault == "gone") east.GetPart<DestructiblePart>().Gone = true;
            NativeForeground(Envelope, 0);
        }

        [TestCase("visual")] [TestCase("non-solid")] [TestCase("zero-hp")]
        [TestCase("takeable")] [TestCase("foreign-physics")]
        public void InvalidVineCenterFallsBackInsteadOfBorrowingIntactWallArt(string fault)
        {
            var wall = Add("VineWall"); NativeForeground(Envelope, 0);
            if (fault == "visual") wall.GetPart<RenderPart>().VisualVariant = "custom";
            if (fault == "non-solid") wall.GetPart<PhysicsPart>().Solid = false;
            if (fault == "zero-hp") wall.GetPart<DestructiblePart>().HP = 0;
            if (fault == "takeable") wall.GetPart<PhysicsPart>().Takeable = true;
            if (fault == "foreign-physics") wall.GetPart<PhysicsPart>().ParentEntity = content.Factory.CreateEntity("VineWall");
            IdentityFallback();
        }

        [TestCase(0, true, 17)] [TestCase(0, false, 16)]
        [TestCase(1, true, 19)] [TestCase(1, false, 18)]
        [TestCase(2, true, 17)] [TestCase(3, false, 18)]
        public void DoorAxisAndActualClosedStateChooseEssentialFrames(int turns, bool open, int frame)
        {
            var owner = Add("VillageDoor"); var door = owner.GetPart<DoorPart>();
            door.QuarterTurns = turns; door.IsOpen = open;
            owner.GetPart<RenderPart>().RenderString = open ? "/" : "+";
            NativeForeground(Envelope, frame); Assert.AreSame(owner, Resolve().Owner);
            Assert.AreEqual(!open, Resolve().BlocksMovement);
            door.IsOpen = !open; owner.GetPart<RenderPart>().RenderString = !open ? "/" : "+";
            NativeForeground(Envelope, 16 + (turns & 1) * 2 + (!open ? 1 : 0));
        }

        [Test] public void LockedDoorUsesClosedArtEvenWhenSavedOpenFlagIsTrue()
        {
            var owner = Add("VillageDoor"); var door = owner.GetPart<DoorPart>();
            door.IsOpen = true; door.QuarterTurns = 0; NativeForeground(Envelope, 17);
            var keyhole = new LockPart { IsLocked = true }; owner.AddPart(keyhole);
            Assert.True(door.IsClosed); NativeForeground(Envelope, 16);
            keyhole.IsLocked = false; Assert.False(door.IsClosed); NativeForeground(Envelope, 17);
        }

        [TestCase("Grass", Envelope)] [TestCase("StoneFloor", Stone)]
        public void ActualFloorMaterialStaysBelowARealCoveringOwner(string blueprint, string material)
        {
            var floor = Add(blueprint); NativeGround(material); Assert.IsNull(Resolve().ForegroundTile);
            var item = Add("HealingTonic"); item.GetPart<RenderPart>().RenderLayer = 100;
            NativeGround(material); Assert.AreSame(item, Resolve().Owner); Assert.NotNull(Resolve().ForegroundTile);
            zone.RemoveEntity(item); NativeGround(material); Assert.AreSame(floor, Resolve().Owner);
            floor.GetPart<RenderPart>().VisualVariant = "custom"; NotEnvelopeGround();
        }

        [TestCase("water")] [TestCase("oil")] [TestCase("cultivation")]
        [TestCase("crop")] [TestCase("well-marker")] [TestCase("road")]
        public void HazardsAndSpecificGroundOwnersOutrankTheGrassMaterial(string reason)
        {
            var grass = Add("Grass"); NativeGround(Envelope);
            if (reason == "water" || reason == "oil") zone.TileState.WriteCoating(X, Y, reason, 4);
            if (reason == "cultivation") grass.AddPart(new CultivatedSoilPart());
            if (reason == "crop") Add("ClaspbeanCrop");
            if (reason == "well-marker") Add("WellGroundMarker");
            if (reason == "road") Add("RoadStone");
            NotEnvelopeGround();
        }

        [TestCase("WellGroundMarker")] [TestCase("SpikeTrap")]
        public void HiddenOrInvalidCompetingOwnerCannotDisclosePresenceByErasingGround(string blueprint)
        {
            Add("Grass"); NativeGround(Envelope);
            var baseline = Resolve().GroundTile; var baselineColor = Resolve().GroundColor;
            var marker = Add(blueprint); var render = marker.GetPart<RenderPart>();
            var physics = marker.GetPart<PhysicsPart>(); Assert.NotNull(render); Assert.NotNull(physics);
            if (blueprint == "SpikeTrap") Assert.True(marker.HasPart<TriggerOnStepPart>());
            render.Visible = false;
            NativeGround(Envelope); Assert.AreSame(baseline, Resolve().GroundTile);
            Assert.AreEqual(baselineColor, Resolve().GroundColor,
                "An undisclosed occupant must not change the visible floor material.");

            render.Visible = true; NotEnvelopeGround();
            physics.ParentEntity = new Entity();
            NativeGround(Envelope); Assert.AreSame(baseline, Resolve().GroundTile);
            physics.ParentEntity = marker; NotEnvelopeGround();

            physics.InInventory = new Entity();
            NativeGround(Envelope); Assert.AreSame(baseline, Resolve().GroundTile);
            physics.InInventory = null; NotEnvelopeGround();
            physics.Equipped = new Entity();
            NativeGround(Envelope); Assert.AreSame(baseline, Resolve().GroundTile);
            physics.Equipped = null; NotEnvelopeGround();

            render.ParentEntity = new Entity();
            NativeGround(Envelope); Assert.AreSame(baseline, Resolve().GroundTile);
            render.ParentEntity = marker; NotEnvelopeGround();
            Assert.True(zone.RemoveEntity(marker));
            NativeGround(Envelope); Assert.AreSame(baseline, Resolve().GroundTile);
        }

        [TestCase("poi-missing")] [TestCase("poi-type")] [TestCase("poi-name")]
        [TestCase("poi-profile")] [TestCase("biome")] [TestCase("stale-graph")]
        public void SillIdentityAndCurrentWorldGraphAreRequired(string fault)
        {
            Add("VineWall"); NativeForeground(Envelope, 0);
            var original = manager.WorldMap.GetPOI(10, 10);
            if (fault == "poi-missing") manager.WorldMap.SetPOI(10, 10, null);
            if (fault == "poi-type") manager.WorldMap.SetPOI(10, 10, new PointOfInterest(POIType.Lair, "Sill", "Villagers"));
            if (fault == "poi-name") manager.WorldMap.SetPOI(10, 10, new PointOfInterest(POIType.Village, "Other", "Villagers"));
            if (fault == "poi-profile") manager.WorldMap.SetPOI(10, 10, new PointOfInterest(POIType.Village, "Sill", "Villagers", profile: "Morrowfast"));
            if (fault == "biome") manager.WorldMap.Tiles[10, 10] = BiomeType.Sodden;
            if (fault == "stale-graph") manager.CachedZones[Sill] = new Zone(Sill);
            if (fault == "stale-graph") Assert.False(world.TryResolvePresentation(zone, X, Y, out _));
            else IdentityFallback();
            manager.WorldMap.SetPOI(10, 10, original); manager.WorldMap.Tiles[10, 10] = BiomeType.Spread;
            manager.CachedZones[Sill] = zone; NativeForeground(Envelope, 0);
        }

        [Test] public void SameNamedManagerlessZoneAndOtherSettlementCannotAdoptTheKit()
        {
            Add("VineWall"); NativeForeground(Envelope, 0);
            zone = new Zone(Sill); zone.GetCell(X, Y).Explored = zone.GetCell(X, Y).IsVisible = true;
            Add("VineWall"); IdentityFallback();
            zone = Attach("Overworld.7.8.0"); Add("VineWall"); IdentityFallback();
        }

        [Test] public void HiddenCellsDoNotRevealLiveEnvelopeState()
        {
            Add("Grass"); Add("VineWall"); NativeForeground(Envelope, 0); NativeGround(Envelope);
            zone.GetCell(X, Y).IsVisible = false; var remembered = Resolve();
            Assert.True(remembered.Remembered); Assert.False(remembered.NativeForeground); NotEnvelopeGround();
            zone.GetCell(X, Y).Explored = false; Assert.False(world.TryResolvePresentation(zone, X, Y, out _));
            zone.GetCell(X, Y).Explored = zone.GetCell(X, Y).IsVisible = true; NativeForeground(Envelope, 0);
        }

        [Test] public void WarmQueriesKeepCurrentOwnersAndSimulationUnchanged()
        {
            Add("Grass"); var wall = Add("VineWall"); Neighbors(10); NativeForeground(Envelope, 10);
            var first = Resolve(); int version = zone.EntityVersion; int hp = wall.GetPart<DestructiblePart>().HP;
            for (int i = 0; i < 512; i++)
            {
                var next = Resolve(); Assert.AreSame(first.ForegroundTile, next.ForegroundTile);
                Assert.AreSame(first.GroundTile, next.GroundTile); Assert.AreSame(wall, next.Owner);
            }
            Assert.AreEqual(version, zone.EntityVersion); Assert.AreEqual(hp, wall.GetPart<DestructiblePart>().HP);
            Assert.True(wall.GetPart<PhysicsPart>().Solid); Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone));
        }

        [TestCase(false)] [TestCase(true)]
        public void InjectedMissingOrMalformedEnvelopeDoesNotReloadAndSharedGroupsStayIndependent(bool malformed)
        {
            var source = malformed ? new Texture2D(383, 24) : null;
            var cellar = new Texture2D(144, 24); var still = new Texture2D(144, 24); var floors = new Texture2D(64, 24);
            using (var selector = new FlatPixelFallbackCatalog())
            using (var art = new FlatPixelSillEnvelopeCatalog(source, cellar, still, floors, selector))
            {
                try
                {
                    var vine = Add("VineWall");
                    Assert.False(art.TryResolveForeground(zone, X, Y, vine, out _), "Injection must not fall back to the installed envelope resource.");
                    Assert.True(zone.RemoveEntity(vine));
                    var wall = Add("StoneWall"); Assert.True(art.TryResolveForeground(zone, X, Y, wall, out var wallTile));
                    Assert.AreSame(cellar, wallTile.sprite.texture); Assert.AreEqual(new Rect(128, 0, 16, 24), wallTile.sprite.rect);
                    Assert.True(zone.RemoveEntity(wall));
                    var station = Add("AlchemyStill"); Assert.True(art.TryResolveForeground(zone, X, Y, station, out var stillTile));
                    Assert.AreSame(still, stillTile.sprite.texture); Assert.True(zone.RemoveEntity(station));
                    var floor = Add("StoneFloor"); Assert.True(art.TryResolveGround(zone, X, Y, out var floorTile, out var actual));
                    Assert.AreSame(floor, actual); Assert.AreSame(floors, floorTile.sprite.texture);
                    Assert.True(zone.RemoveEntity(floor)); Add("Grass"); Assert.False(art.TryResolveGround(zone, X, Y, out _, out _));
                }
                finally
                {
                    art.Dispose();
                    if (source != null) UnityEngine.Object.DestroyImmediate(source);
                    UnityEngine.Object.DestroyImmediate(cellar); UnityEngine.Object.DestroyImmediate(still); UnityEngine.Object.DestroyImmediate(floors);
                }
            }
        }

        [Test] public void AdapterDisposesCachedFramesButNeverBorrowedAtlasOrSelector()
        {
            var source = new Texture2D(384, 24);
            using (var selector = new FlatPixelFallbackCatalog())
            using (var art = new FlatPixelSillEnvelopeCatalog(source, null, null, null, selector))
            {
                try
                {
                    var wall = Add("VineWall"); Assert.True(art.TryResolveForeground(zone, X, Y, wall, out var tile));
                    var sprite = tile.sprite; Assert.AreSame(source, sprite.texture);
                    Assert.True(art.TryResolveForeground(zone, X, Y, wall, out var again)); Assert.AreSame(tile, again);
                    Assert.AreEqual(FilterMode.Bilinear, source.filterMode, "Borrowed source settings must remain untouched.");
                    art.Dispose(); art.Dispose();
                    Assert.True(tile == null); Assert.True(sprite == null); Assert.True(source != null);
                    Assert.False(art.TryResolveForeground(zone, X, Y, wall, out _));
                    Assert.True(selector.TryResolve(zone, X, Y, out var fallback)); Assert.AreSame(wall, fallback.Owner);
                }
                finally { art.Dispose(); UnityEngine.Object.DestroyImmediate(source); }
            }
        }

        [Test] public void SillReaderUsesIsolatedVineAndRejectsCoveredHiddenOrForeignSubjects()
        {
            var vine = Add("VineWall"); Add("VineWall", X + 1); NativeForeground(Envelope, 2);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, vine, X, Y, out var native, out var tint));
            Assert.AreSame(Resources.Load<Texture2D>(Envelope), native.texture);
            Assert.AreEqual(new Rect(0, 0, 16, 24), native.rect); Assert.AreEqual(Color.white, tint);
            var cover = Add("Dagger"); cover.GetPart<RenderPart>().RenderLayer = 100;
            Assert.True(world.TryGetPresentationSubjectSprite(zone, vine, X, Y, out var covered, out _));
            Assert.AreNotSame(native.texture, covered.texture);
            Assert.True(zone.RemoveEntity(cover)); zone.GetCell(X, Y).IsVisible = false;
            Assert.True(world.TryGetPresentationSubjectSprite(zone, vine, X, Y, out var hidden, out _));
            Assert.AreNotSame(native.texture, hidden.texture);
            zone.GetCell(X, Y).IsVisible = true;
            var foreign = content.Factory.CreateEntity("VineWall"); foreign.ID = vine.ID;
            Assert.True(world.TryGetPresentationSubjectSprite(zone, foreign, X, Y, out var stale, out _));
            Assert.AreNotSame(native.texture, stale.texture);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, vine, X, Y, out var restored, out _));
            Assert.AreSame(native, restored);
        }

        [TestCase(-1)] [TestCase(4)]
        public void InvalidDoorAxisDoesNotInventAValidOrientation(int turns)
        {
            var owner = Add("VillageDoor"); NativeForeground(Envelope, 17);
            owner.GetPart<DoorPart>().QuarterTurns = turns; IdentityFallback();
        }

        [Test] public void RealStoneFloorWinsOverLowerGrassButDuplicateStoneDoesNotInventMaterialAuthority()
        {
            Add("Grass"); var stone = Add("StoneFloor"); NativeGround(Stone);
            Assert.AreSame(stone, Resolve().Owner); Assert.IsNull(Resolve().ForegroundTile);
            var duplicate = Add("StoneFloor"); NotEnvelopeGround();
            Assert.True(zone.RemoveEntity(duplicate)); NativeGround(Stone);
        }
    }
}
