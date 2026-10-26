using System;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Existing artwork for three observed East owners. Runtime type lookup keeps
    // this fixture compilable and genuinely RED before the companion exists.
    public sealed class FlatPixelEastFixtureTests
    {
        const string East = "Overworld.12.10.0";
        const int X = 10, Y = 10;
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        delegate bool Query(Zone zone, int x, int y, out Tile tile);
        delegate bool Portrait(Zone zone, Entity subject, int x, int y, out Sprite sprite);
        DensityLootTestScope content;
        OverworldZoneManager manager;
        FlatPixelFallbackCatalog selector;
        FlatPixelGreyboxCatalog world;
        Texture2D cellar, states, peat;
        IDisposable adapter;
        Query query;
        Portrait portrait;
        Zone zone;

        [SetUp] public void Setup()
        {
            content = new DensityLootTestScope();
            manager = OverworldZoneManager.CreateDetached(content.Factory, 64, true);
            selector = new FlatPixelFallbackCatalog();
            cellar = new Texture2D(144, 24); states = new Texture2D(192, 24); peat = new Texture2D(272, 24);
            zone = Attach(East);
        }
        [TearDown] public void Cleanup()
        {
            world?.Dispose(); adapter?.Dispose(); selector?.Dispose();
            if (cellar != null) Object.DestroyImmediate(cellar);
            if (states != null) Object.DestroyImmediate(states);
            if (peat != null) Object.DestroyImmediate(peat);
            content?.Dispose();
        }
        Zone Attach(string id)
        {
            var result = new Zone(id); manager.SetActiveZone(result);
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                result.GetCell(x, y).Explored = result.GetCell(x, y).IsVisible = true;
            return result;
        }
        Entity Add(string blueprint = "Wall", int x = X, int y = Y)
        { var owner = content.Factory.CreateEntity(blueprint); Assert.NotNull(owner); Assert.True(zone.AddEntity(owner, x, y)); return owner; }
        void Create(Texture2D stone, Texture2D gates, Texture2D bog, bool inject = true)
        {
            var type = typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelEastFixtureCatalog");
            Assert.NotNull(type, "East's plain wall, pen gate and peat still lack their scoped native route.");
            var ctor = type.GetConstructor(new[] { typeof(Texture2D), typeof(Texture2D), typeof(Texture2D), typeof(FlatPixelFallbackCatalog) });
            Assert.NotNull(ctor); adapter = (IDisposable)ctor.Invoke(new object[] { stone, gates, bog, selector });
            query = (Query)Delegate.CreateDelegate(typeof(Query), adapter, type.GetMethod("TryResolve", I));
            portrait = (Portrait)Delegate.CreateDelegate(typeof(Portrait), adapter, type.GetMethod("TryGetSubjectSprite", I));
            if (!inject) return;
            world = new FlatPixelGreyboxCatalog((Texture2D)null); Write(world, "_eastFixtureArt", adapter);
        }
        static void Write(object target, string name, object value)
        { var field = target.GetType().GetField(name, I); Assert.NotNull(field, name); field.SetValue(target, value); }
        static object Read(object target, string name) => target.GetType().GetField(name, I).GetValue(target);
        static void Frame(Tile tile, Texture2D atlas, int index)
        {
            Assert.NotNull(tile); Assert.NotNull(tile.sprite); Assert.AreSame(atlas, tile.sprite.texture);
            Assert.AreEqual(new Rect(index * 16, 0, 16, 24), tile.sprite.rect);
            Assert.AreEqual(new Vector2(8, 12), tile.sprite.pivot); Assert.AreEqual(16, tile.sprite.pixelsPerUnit);
            Assert.AreEqual(TileFlags.None, tile.flags); Assert.AreEqual(Tile.ColliderType.None, tile.colliderType);
        }
        Tile Expect(Texture2D atlas, int index)
        { Assert.True(query(zone, X, Y, out var tile)); Frame(tile, atlas, index); return tile; }
        FlatPixelGreyboxCell Visible()
        { Assert.True(world.TryResolvePresentation(zone, X, Y, out var value)); return value; }
        void Refused(Entity owner)
        {
            Assert.False(query(zone, X, Y, out _)); Assert.False(portrait(zone, owner, X, Y, out _));
            if (world.TryResolvePresentation(zone, X, Y, out var value) && value.ForegroundTile != null)
            {
                Assert.AreNotSame(cellar, value.ForegroundTile.sprite.texture);
                Assert.AreNotSame(states, value.ForegroundTile.sprite.texture);
                Assert.AreNotSame(peat, value.ForegroundTile.sprite.texture);
            }
        }

        [TestCase("Wall", false, "opening-cellar", 8, true)]
        [TestCase("FrontierPenGate", false, "opening-states", 1, true)]
        [TestCase("FrontierPenGate", true, "opening-states", 2, false)]
        [TestCase("PeatBog", false, "nella-corner", 16, false)]
        public void OrdinaryWorldAndExactReaderReuseApprovedFrames(string blueprint, bool open, string resource, int frame, bool blocked)
        {
            var owner = Add(blueprint);
            if (open) Assert.True(owner.GetPart<DoorPart>().TrySetOpen(Add("Player", X - 1), zone, true));
            world = new FlatPixelGreyboxCatalog(); var value = Visible();
            Assert.AreSame(owner, value.Owner); Assert.True(value.NativeForeground, "Observed owner still renders as a glyph.");
            var source = Resources.Load<Texture2D>("Sprites/Live16x24/" + resource); Assert.NotNull(source);
            Frame(value.ForegroundTile, source, frame); Assert.AreEqual(Color.white, value.ForegroundColor);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, X, Y, out var sprite, out var tint));
            Assert.AreSame(value.ForegroundTile.sprite, sprite); Assert.AreEqual(Color.white, tint);
            Assert.AreEqual(blocked, value.BlocksMovement); Assert.AreEqual(blocked, zone.GetCell(X, Y).BlocksMovement());
        }

        [Test] public void RealDoorOpenCloseAndOwnedLockUseDoorTruthWithoutChangingSolidOrInventingRepair()
        {
            Create(cellar, states, peat); var gate = Add("FrontierPenGate"); var actor = Add("Player", X - 1);
            var door = gate.GetPart<DoorPart>(); var physics = gate.GetPart<PhysicsPart>();
            Assert.False(physics.Solid); Assert.IsNull(gate.GetPart<RepairablePart>()); Assert.IsNull(gate.GetPart<MaterialPart>());
            var closed = Expect(states, 1); Assert.True(Visible().BlocksMovement);
            Assert.True(door.TrySetOpen(actor, zone, true)); var opened = Expect(states, 2);
            Assert.AreNotSame(closed, opened); Assert.False(Visible().BlocksMovement); Assert.False(physics.Solid);
            var latch = new LockPart { IsLocked = true }; gate.AddPart(latch);
            Assert.True(door.IsOpen); Assert.True(door.IsClosed); Assert.AreSame(closed, Expect(states, 1));
            Assert.False(door.TrySetOpen(actor, zone, false)); Assert.AreSame(closed, Expect(states, 1));
            latch.IsLocked = false; Assert.AreSame(opened, Expect(states, 2));
            Assert.True(door.TrySetOpen(actor, zone, false)); Assert.AreSame(closed, Expect(states, 1));
            Assert.False(physics.Solid); Assert.AreEqual((X - 1, Y), zone.GetEntityPosition(actor));
        }

        [TestCase("Wall")] [TestCase("FrontierPenGate")] [TestCase("PeatBog")]
        public void OnlyTheSelectedOwnerOccupiesTheWorldWhileTheCoveredExactReaderRemainsCorrect(string blueprint)
        {
            Create(cellar, states, peat); var owner = Add(blueprint); Assert.True(query(zone, X, Y, out var tile));
            var actor = Add("Player"); actor.GetPart<RenderPart>().RenderLayer = 100;
            Assert.False(query(zone, X, Y, out _)); Assert.AreSame(actor, Visible().Owner);
            Assert.True(portrait(zone, owner, X, Y, out var sprite)); Assert.AreSame(tile.sprite, sprite);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, X, Y, out sprite, out _)); Assert.AreSame(tile.sprite, sprite);
            Assert.True(zone.RemoveEntity(actor)); Assert.AreSame(tile, Visible().ForegroundTile);
        }

        [TestCase("hidden")] [TestCase("unexplored")] [TestCase("foreign-cell")]
        [TestCase("replaced-graph")] [TestCase("wrong-biome")] [TestCase("poi")]
        public void CurrentExactDisclosedEastAuthorityPrecedesNativeResolution(string fault)
        {
            Create(cellar, states, peat); var owner = Add("FrontierPenGate"); Expect(states, 1); var cell = zone.GetCell(X, Y);
            if (fault == "hidden") cell.IsVisible = false;
            if (fault == "unexplored") cell.Explored = false;
            if (fault == "foreign-cell")
            { var other = new Cell(X, Y, new Zone(East)) { IsVisible = true, Explored = true }; other.Objects.Add(owner); zone.Cells[X, Y] = other; }
            if (fault == "replaced-graph") manager.CachedZones[East] = new Zone(East);
            if (fault == "wrong-biome") manager.WorldMap.Tiles[12, 10] = BiomeType.Sodden;
            if (fault == "poi") manager.WorldMap.SetPOI(12, 10, new PointOfInterest(POIType.Village, "scope counter"));
            Refused(owner);
            zone.Cells[X, Y] = cell; cell.IsVisible = cell.Explored = true; manager.CachedZones[East] = zone;
            manager.WorldMap.Tiles[12, 10] = BiomeType.Spread; manager.WorldMap.SetPOI(12, 10, null); Expect(states, 1);
        }

        [TestCase("Overworld.11.8.0")] [TestCase("Overworld.12.11.0")] [TestCase("Overworld.12.12.0")]
        public void NellaKitchenAndOtherAddressesDoNotAcquireTheEastFixtureGroup(string id)
        {
            Create(cellar, states, peat);
            foreach (var blueprint in new[] { "Wall", "FrontierPenGate", "PeatBog" })
            { zone = Attach(id); Refused(Add(blueprint)); }
        }

        [TestCase("Wall")] [TestCase("FrontierPenGate")] [TestCase("PeatBog")]
        public void KnownIdentityCannotOverrideForeignPartsReskinsCustodyOrInvalidStructure(string blueprint)
        {
            Create(cellar, states, peat);
            foreach (string fault in new[] { "visual-id", "variant", "glyph-variants", "foreign-render", "invisible", "foreign-physics", "missing-physics",
                "takeable", "carried", "equipped", "creature", "item", "player", "footprint", "zero-stack", "foreign-stack", "foreign-structure", "missing-structure", "gone", "zero-hp" })
            {
                zone = Attach(East); var owner = Add(blueprint); Assert.True(query(zone, X, Y, out _), fault + " positive control");
                Fault(owner, fault); Refused(owner);
            }
        }

        [Test] public void WallNeedsActualStoneWallSemanticsAndDamageKeepsItsNativeIdentityUntilDestruction()
        {
            Create(cellar, states, peat);
            foreach (string fault in new[] { "wall-tag", "not-solid", "wrong-material", "foreign-material", "missing-material", "other-glyph" })
            { zone = Attach(East); var owner = Add(); Expect(cellar, 8); Fault(owner, fault); Refused(owner); }
            zone = Attach(East); var wall = Add(); var tile = Expect(cellar, 8); DestructionSystem.EntityFactoryRef = content.Factory;
            Assert.AreEqual(DestroyVerdict.Damaged, DestructionSystem.Damage(wall, 4, null, zone)); Assert.AreSame(tile, Expect(cellar, 8));
            Assert.AreEqual(DestroyVerdict.Destroyed, DestructionSystem.Damage(wall, 999, null, zone));
            Assert.IsNull(zone.GetEntityCell(wall)); Assert.False(query(zone, X, Y, out _)); Assert.False(portrait(zone, wall, X, Y, out _));
            Assert.False(zone.GetCell(X, Y).BlocksMovement()); Assert.True(zone.GetCell(X, Y).Objects.Exists(e => e.BlueprintName == "Rubble"));
        }

        [Test] public void GateRejectsForeignDoorLockAndInventedRepairButDoesNotRequireWoodMaterial()
        {
            Create(cellar, states, peat);
            foreach (string fault in new[] { "foreign-door", "missing-door", "foreign-lock", "repair", "solid", "other-glyph" })
            { zone = Attach(East); var owner = Add("FrontierPenGate"); Expect(states, 1); Fault(owner, fault); Refused(owner); }
        }

        [Test] public void PeatSharesNellaIdentityWhileZeroStockDoesNotInventDryGroundOrMutateWater()
        {
            Create(cellar, states, peat);
            foreach (string fault in new[] { "terrain-tag", "wrong-material", "foreign-material", "foreign-pool", "missing-pool", "other-liquid", "foreign-source", "missing-source", "other-coating", "solid" })
            { zone = Attach(East); var owner = Add("PeatBog"); Expect(peat, 16); Fault(owner, fault); Refused(owner); }
            zone = Attach(East); var bog = Add("PeatBog"); var pool = bog.GetPart<LiquidPoolPart>(); var source = bog.GetPart<TileStateSourcePart>();
            var tile = Expect(peat, 16); int version = zone.EntityVersion; string state = zone.TileState.ToSaveString();
            pool.Volume = 0; bog.GetPart<DestructiblePart>().HP = 1;
            for (int i = 0; i < 24; i++) { Assert.AreSame(tile, Expect(peat, 16)); Assert.AreSame(tile, Visible().ForegroundTile); }
            Assert.AreEqual(0, pool.Volume); Assert.AreEqual("water", pool.LiquidId); Assert.AreEqual("water", source.Coating);
            Assert.AreEqual(4, source.CoatingTurns); Assert.AreEqual(version, zone.EntityVersion); Assert.AreEqual(state, zone.TileState.ToSaveString());
        }

        [Test] public void StaleMovedOrSameIdSubjectsCannotBorrowAnotherCellsArt()
        {
            Create(cellar, states, peat); var owner = Add(); var old = zone.GetCell(X, Y); Expect(cellar, 8);
            Assert.True(zone.MoveEntity(owner, X + 1, Y)); old.Objects.Add(owner); Refused(owner);
            var copy = content.Factory.CreateEntity("Wall"); copy.ID = owner.ID; old.Objects.Add(copy); Refused(copy);
            Assert.True(portrait(zone, owner, X + 1, Y, out _)); Assert.False(portrait(zone, copy, X + 1, Y, out _));
            Assert.True(zone.RemoveEntity(owner)); zone.GetCell(X + 1, Y).Objects.Add(owner);
            Assert.False(portrait(zone, owner, X + 1, Y, out _));
        }

        [TestCase(0, false)] [TestCase(0, true)] [TestCase(1, false)] [TestCase(1, true)] [TestCase(2, false)] [TestCase(2, true)]
        public void AUnavailableSourceGroupFallsBackWithoutDisablingOtherGroupsOrLoadingResources(int group, bool malformed)
        {
            Texture2D bad = malformed ? new Texture2D(16, 23) : null;
            try
            {
                Create(group == 0 ? bad : cellar, group == 1 ? bad : states, group == 2 ? bad : peat);
                var names = new[] { "Wall", "FrontierPenGate", "PeatBog" }; var sources = new[] { cellar, states, peat }; var frames = new[] { 8, 1, 16 };
                for (int i = 0; i < 3; i++)
                {
                    zone = Attach(East); var owner = Add(names[i]);
                    if (i == group) { Refused(owner); Assert.AreSame(owner, Visible().Owner); Assert.False(Visible().NativeForeground); }
                    else Frame(Visible().ForegroundTile, sources[i], frames[i]);
                }
                if (bad != null) Assert.True(bad != null);
            }
            finally { if (bad != null) Object.DestroyImmediate(bad); }
        }

        [Test] public void ResourceFreeInjectionAndDisposalNeverOwnBorrowedDependencies()
        {
            var owner = Add("FrontierPenGate"); world = new FlatPixelGreyboxCatalog((Texture2D)null);
            Assert.False(Visible().NativeForeground); Assert.IsNull(Read(world, "_eastFixtureArt")); world.Dispose(); world = null;
            Create(cellar, states, peat); var tile = Expect(states, 1); var sprite = tile.sprite;
            adapter.Dispose(); adapter.Dispose(); Assert.True(tile == null); Assert.True(sprite == null);
            Assert.True(cellar != null); Assert.True(states != null); Assert.True(peat != null); Assert.True(selector.TryResolve(zone, X, Y, out _));
            Assert.False(query(zone, X, Y, out _)); Assert.False(portrait(zone, owner, X, Y, out _));
        }

        [Test] public void RealDoorAndWallActionsUpdateSubmittedArtWithoutMovingTheActor()
        {
            Create(cellar, states, peat); var gate = Add("FrontierPenGate"); var actor = Add("Player", X - 1);
            var wall = Add("Wall", X + 2); DestructionSystem.EntityFactoryRef = content.Factory;
            using (var paint = new Paint(world, zone))
            {
                Frame(paint.At(X, Y), states, 1); Frame(paint.At(X + 2, Y), cellar, 8);
                Assert.True(gate.GetPart<DoorPart>().TrySetOpen(actor, zone, true)); paint.Step(); Frame(paint.At(X, Y), states, 2);
                Assert.True(gate.GetPart<DoorPart>().TrySetOpen(actor, zone, false)); paint.Step(); Frame(paint.At(X, Y), states, 1);
                Assert.AreEqual(DestroyVerdict.Destroyed, DestructionSystem.Damage(wall, 999, null, zone)); paint.Step();
                var replacement = paint.At(X + 2, Y);
                Assert.True(replacement == null || replacement.sprite.texture != cellar, "Removed wall must not remain submitted as intact masonry.");
                Assert.False(zone.GetCell(X + 2, Y).BlocksMovement()); Assert.AreEqual((X - 1, Y), zone.GetEntityPosition(actor));
                long resolved = EnvironmentSpriteRenderer.Perf.CellsResolved; paint.Step(); Assert.AreEqual(resolved, EnvironmentSpriteRenderer.Perf.CellsResolved);
            }
        }

        static void Fault(Entity owner, string fault)
        {
            var render = owner.GetPart<RenderPart>(); var physics = owner.GetPart<PhysicsPart>(); var foreign = new Entity();
            switch (fault)
            {
                case "visual-id": render.VisualID = "custom-east-fixture"; break;
                case "variant": render.VisualVariant = "custom-east-fixture"; break;
                case "glyph-variants": render.GlyphVariants = "#~"; break;
                case "foreign-render": render.ParentEntity = foreign; break;
                case "invisible": render.Visible = false; break;
                case "foreign-physics": physics.ParentEntity = foreign; break;
                case "missing-physics": owner.RemovePart(physics); break;
                case "takeable": physics.Takeable = true; break;
                case "carried": physics.InInventory = foreign; break;
                case "equipped": physics.Equipped = foreign; break;
                case "creature": owner.SetTag("Creature"); break;
                case "item": owner.SetTag("Item"); break;
                case "player": owner.SetTag("Player"); break;
                case "footprint": owner.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;0,1" }); break;
                case "zero-stack": owner.AddPart(new StackerPart { StackCount = 0 }); break;
                case "foreign-stack": var stack = new StackerPart { StackCount = 1 }; owner.AddPart(stack); stack.ParentEntity = foreign; break;
                case "foreign-structure": owner.GetPart<DestructiblePart>().ParentEntity = foreign; break;
                case "missing-structure": owner.RemovePart(owner.GetPart<DestructiblePart>()); break;
                case "gone": owner.GetPart<DestructiblePart>().Gone = true; break;
                case "zero-hp": owner.GetPart<DestructiblePart>().HP = 0; break;
                case "wall-tag": owner.Tags.Remove("Wall"); break;
                case "terrain-tag": owner.Tags.Remove("Terrain"); break;
                case "not-solid": physics.Solid = false; break;
                case "solid": physics.Solid = true; break;
                case "wrong-material": owner.GetPart<MaterialPart>().MaterialID = "Plastic"; break;
                case "foreign-material": owner.GetPart<MaterialPart>().ParentEntity = foreign; break;
                case "missing-material": owner.RemovePart(owner.GetPart<MaterialPart>()); break;
                case "other-glyph": render.RenderString = "?"; break;
                case "foreign-door": owner.GetPart<DoorPart>().ParentEntity = foreign; break;
                case "missing-door": owner.RemovePart(owner.GetPart<DoorPart>()); break;
                case "foreign-lock": var latch = new LockPart { IsLocked = true }; owner.AddPart(latch); latch.ParentEntity = foreign; break;
                case "repair": owner.AddPart(new RepairablePart()); break;
                case "foreign-pool": owner.GetPart<LiquidPoolPart>().ParentEntity = foreign; break;
                case "missing-pool": owner.RemovePart(owner.GetPart<LiquidPoolPart>()); break;
                case "other-liquid": owner.GetPart<LiquidPoolPart>().LiquidId = "oil"; break;
                case "foreign-source": owner.GetPart<TileStateSourcePart>().ParentEntity = foreign; break;
                case "missing-source": owner.RemovePart(owner.GetPart<TileStateSourcePart>()); break;
                case "other-coating": owner.GetPart<TileStateSourcePart>().Coating = "oil"; break;
            }
        }

        // Ordinary DoorPart already marks full dirty for sight/lighting. This
        // fixture deliberately uses those hooks; it does not manually repaint.
        sealed class Paint : IDisposable
        {
            readonly HotbarSaveFixture saved;
            readonly GameObject grid;
            readonly ZoneRenderer renderer;
            readonly Tilemap overlay;
            readonly Action<int, int, string> oldCell = ZoneRenderHooks.CellDirtyCallback;
            readonly Action<string> oldFull = ZoneRenderHooks.FullDirtyCallback;
            readonly FieldInfo bound = typeof(ZoneTileStateSystem).GetField("_boundRenderZone", BindingFlags.Static | BindingFlags.NonPublic);
            readonly object oldBound; readonly Zone previous; readonly Action<int, int> oldChanged; readonly Action oldSight;
            readonly Dictionary<FieldInfo, object> perf = new Dictionary<FieldInfo, object>();
            public Paint(FlatPixelGreyboxCatalog catalog, Zone zone)
            {
                oldBound = bound.GetValue(null);
                if (oldBound is WeakReference<Zone> weak && weak.TryGetTarget(out previous)) { oldChanged = previous.TileState.OnCellChanged; oldSight = previous.TileState.OnSightChanged; }
                try
                {
                    foreach (var field in typeof(EnvironmentSpriteRenderer.Perf).GetFields(BindingFlags.Public | BindingFlags.Static)) perf[field] = field.GetValue(null);
                    saved = new HotbarSaveFixture(); grid = new GameObject("East fixture cadence grid", typeof(Grid)); saved.Root.transform.SetParent(grid.transform, false);
                    var main = saved.Root.GetComponent<Tilemap>(); var background = new GameObject("East fixture background", typeof(Tilemap)); background.transform.SetParent(saved.Root.transform, false);
                    renderer = saved.Renderer; Write(renderer, "_tilemap", main); Write(renderer, "_bgTilemap", background.GetComponent<Tilemap>());
                    var camera = new GameObject("East fixture camera", typeof(Camera)); camera.transform.SetParent(saved.Root.transform, false); camera.GetComponent<Camera>().orthographic = true; Write(renderer, "_mainCamera", camera.GetComponent<Camera>());
                    var env = saved.Root.AddComponent<EnvironmentSpriteRenderer>(); env.Init(saved.Root.transform, main, background.GetComponent<Tilemap>()); env.RenderingEnabled = true;
                    Write(env, "_flatPixelGreybox", catalog); overlay = (Tilemap)Read(env, "_overlayTilemap");
                    Write(renderer, "_envSpriteRenderer", env); Write(renderer, "_flatPixelModeEnabled", true); renderer.SetZone(zone);
                    ZoneRenderHooks.CellDirtyCallback = renderer.MarkCellDirty; ZoneRenderHooks.FullDirtyCallback = renderer.MarkDirty; Step();
                }
                catch { Dispose(); throw; }
            }
            public void Step()
            { try { typeof(ZoneRenderer).GetMethod("LateUpdate", I).Invoke(renderer, null); } catch (TargetInvocationException e) { throw e.InnerException ?? e; } }
            public Tile At(int x, int y) => overlay.GetTile<Tile>(new Vector3Int(x, Zone.Height - 1 - y, 0));
            public void Dispose()
            {
                try { saved?.Dispose(); if (grid != null) Object.DestroyImmediate(grid); }
                finally
                {
                    ZoneRenderHooks.CellDirtyCallback = oldCell; ZoneRenderHooks.FullDirtyCallback = oldFull; bound.SetValue(null, oldBound);
                    if (previous != null) { previous.TileState.OnCellChanged = oldChanged; previous.TileState.OnSightChanged = oldSight; }
                    foreach (var entry in perf) entry.Key.SetValue(null, entry.Value);
                }
            }
        }
    }
}
