// Regression coverage for native Sodden material ownership and submitted rendering.
// Existing travel packet:28 frames; board0..15, old mire16/17, peat18/19, tree20/21.
// Default ground/mire now use the separate36-frame Sodden environment atlas.
// Explicit six-input injection retains all original28-frame compatibility pins.
// Public presentation/submitted drawing paths are primary; adapter injection uses
// the explicitly agreed constructor via reflection so preimplementation compiles.
using System;
using System.Collections.Generic;
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
    public sealed class FlatPixelSoddenTravelTests
    {
        const string Crossing = "Overworld.16.7.0";
        const string Atlas = "Sprites/Live16x24/sodden-travel";
        const string GrassAtlas = "Sprites/Live16x24/sill-envelope";
        const string FenAtlas = "Sprites/Live16x24/sodden-ground";
        const int X = 10, Y = 10;
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic;
        DensityLootTestScope content;
        OverworldZoneManager manager;
        FlatPixelGreyboxCatalog world;
        Zone zone;
        Dictionary<string, LiquidDefinition> liquids, previousLiquids;
        bool previouslyInitialized;
        FlatPixelFallbackCatalog injectedSelector;
        IDisposable injectedAdapter;
        bool legacyInjection;
        string CurrentGroundAtlas => legacyInjection ? GrassAtlas : FenAtlas;
        string CurrentMireAtlas => legacyInjection ? Atlas : FenAtlas;

        [SetUp] public void Setup()
        {
            legacyInjection = false;
            // Match the existing KitchenBrine borrowing/restoration pattern.
            liquids = (Dictionary<string, LiquidDefinition>)typeof(LiquidRegistry).GetField("_byId", Static).GetValue(null);
            previousLiquids = new Dictionary<string, LiquidDefinition>(liquids);
            previouslyInitialized = LiquidRegistry.IsInitialized;
            LiquidRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/LiquidDefinitions").Select(t => t.text));
            Assert.AreEqual("water", LiquidRegistry.Get("bog-mire")?.GroundReactionFamily);
            content = new DensityLootTestScope();
            manager = OverworldZoneManager.CreateDetached(content.Factory, 65, true);
            Assert.AreEqual(BiomeType.Sodden, manager.WorldMap.GetBiome(16, 7));
            Assert.IsNull(manager.WorldMap.GetPOI(16, 7), "Do not rewrite a POI to manufacture scope acceptance.");
            Assert.True(SoddenDistrict.Eligible(manager, Crossing));
            zone = Attach(Crossing); world = new FlatPixelGreyboxCatalog();
        }
        [TearDown] public void Cleanup()
        {
            try { world?.Dispose(); injectedAdapter?.Dispose(); injectedSelector?.Dispose(); content?.Dispose(); }
            finally
            {
                if (liquids != null && previousLiquids != null)
                {
                    liquids.Clear(); foreach (var entry in previousLiquids) liquids[entry.Key] = entry.Value;
                    typeof(LiquidRegistry).GetField("_initialized", Static).SetValue(null, previouslyInitialized);
                }
            }
        }
        Zone Attach(string id)
        {
            var next = new Zone(id); manager.SetActiveZone(next);
            Reveal(next); return next;
        }
        static void Reveal(Zone graph)
        {
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                graph.GetCell(x, y).Explored = graph.GetCell(x, y).IsVisible = true;
        }
        Entity Add(string name, int x = X, int y = Y, bool seed = true)
        {
            var e = content.Factory.CreateEntity(name); Assert.NotNull(e);
            Assert.True(zone.AddEntity(e, x, y));
            if (seed) e.GetPart<TileStateSourcePart>()?.Seed(zone, x, y);
            return e;
        }
        FlatPixelGreyboxCell View(int x = X, int y = Y)
        { Assert.True(world.TryResolvePresentation(zone, x, y, out var view)); return view; }
        static void NativeTile(Tile tile, string resource)
        {
            var source = Resources.Load<Texture2D>(resource); Assert.NotNull(source, resource);
            Assert.NotNull(tile); Assert.NotNull(tile.sprite); Assert.AreSame(source, tile.sprite.texture);
            Assert.AreEqual(new Vector2(16, 24), tile.sprite.rect.size);
            Assert.AreEqual(new Vector2(8, 12), tile.sprite.pivot); Assert.AreEqual(16, tile.sprite.pixelsPerUnit);
            Assert.AreEqual(TileFlags.None, tile.flags); Assert.AreEqual(Tile.ColliderType.None, tile.colliderType);
        }
        void Native(Entity owner, string resource = Atlas, int x = X, int y = Y)
        {
            if (owner.BlueprintName == "MirePool") resource = CurrentMireAtlas;
            var view = View(x, y); Assert.AreSame(owner, view.Owner); Assert.True(view.NativeForeground);
            NativeTile(view.ForegroundTile, resource); Assert.AreEqual(Color.white, view.ForegroundColor);
            Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone), "This kit must not widen opening admission.");
            Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, x, y, out var sprite, out var tint));
            Assert.AreSame(Resources.Load<Texture2D>(resource), sprite.texture);
            if (owner.BlueprintName == "Duckboard") Assert.AreEqual(new Rect(0, 0, 16, 24), sprite.rect);
            else if (owner.BlueprintName == "MirePool" && !legacyInjection)
                Assert.AreEqual(new Rect((owner.GetPart<LiquidPoolPart>().Volume == 0 ? 20 : 4) * 16, 0, 16, 24), sprite.rect);
            else Assert.AreEqual(view.ForegroundTile.sprite.rect, sprite.rect);
            Assert.AreEqual(Color.white, tint);
        }
        void IdentityFallback(int x = X, int y = Y)
        {
            Assert.True(world.TryResolveFallback(zone, x, y, out var baseline)); var view = View(x, y);
            Assert.AreSame(baseline.Owner, view.Owner); Assert.False(view.NativeForeground);
            Assert.AreSame(baseline.ForegroundTile, view.ForegroundTile);
        }
        void BoardFrame(int mask)
        {
            var view = View(); Assert.True(view.NativeForeground); NativeTile(view.ForegroundTile, Atlas);
            Assert.AreEqual(new Rect(mask * 16, 0, 16, 24), view.ForegroundTile.sprite.rect);
        }

        static void Write(object owner, string name, object value)
        { var field = owner.GetType().GetField(name, I); Assert.NotNull(field, name); field.SetValue(owner, value); }
        static object Read(object owner, string name)
        { var field = owner.GetType().GetField(name, I); Assert.NotNull(field, name); return field.GetValue(owner); }
        void NotSoddenNative(Entity subject)
        {
            var view = View();
            if (view.Owner == subject) Assert.False(view.NativeForeground);
            if (world.TryGetPresentationSubjectSprite(zone, subject, X, Y, out var sprite, out _))
                { Assert.AreNotSame(Resources.Load<Texture2D>(Atlas), sprite.texture); Assert.AreNotSame(Resources.Load<Texture2D>(FenAtlas), sprite.texture); }
        }
        void Inject(Texture2D packet)
        {
            legacyInjection = true;
            world.Dispose(); world = new FlatPixelGreyboxCatalog((Texture2D)null);
            injectedSelector = new FlatPixelFallbackCatalog();
            var type = typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelSoddenTravelCatalog");
            Assert.NotNull(type, "Sodden travel adapter is not implemented yet.");
            // Signature explicitly supplied by the runtime owner; no compile-time dependency on the new class.
            injectedAdapter = (IDisposable)Activator.CreateInstance(type, new object[] { packet,
                Resources.Load<Texture2D>("Sprites/Live16x24/opening-base"),
                Resources.Load<Texture2D>(GrassAtlas), Resources.Load<Texture2D>("Sprites/Live16x24/cellar-floor"),
                Resources.Load<Texture2D>("Sprites/Live16x24/opening-cellar"), injectedSelector });
            Write(world, "_soddenTravelArt", injectedAdapter);
        }
        static void BoardTile(Tile tile, int mask)
        { NativeTile(tile, Atlas); Assert.AreEqual(new Rect(mask * 16, 0, 16, 24), tile.sprite.rect); }

        int MireFrame(Tile tile, bool depleted)
        {
            NativeTile(tile, CurrentMireAtlas);
            int frame = (int)tile.sprite.rect.x / 16;
            if (legacyInjection)
                CollectionAssert.Contains(depleted ? new[] { 17, 25, 26, 27 } : new[] { 16, 22, 23, 24 }, frame,
                    "Legacy injected mire stock retains its approved static material family.");
            else Assert.That(frame, Is.InRange(depleted ? 20 : 4, depleted ? 35 : 19));
            return frame;
        }

        [Test] public void LegacyInjectedMireAreaRetainsSparseVariationsWithoutChangingStockOrHazards()
        {
            Inject(Resources.Load<Texture2D>(Atlas));
            // Exercise real current owners over an area, not a copy of the coordinate hash.
            var owners = new List<Entity>();
            for (int y = 4; y < 12; y++) for (int x = 4; x < 36; x++) owners.Add(Add("MirePool", x, y));
            string state = zone.TileState.ToSaveString(); int version = zone.EntityVersion;
            var filledFrames = new HashSet<int>(); var filmFrames = new HashSet<int>();
            var samples = new Dictionary<int, Entity>();
            var filledTiles = new Dictionary<Entity, Tile>(); var filmTiles = new Dictionary<Entity, Tile>();
            int filledBase = 0, filmBase = 0;
            foreach (var owner in owners)
            {
                var cell = zone.GetEntityCell(owner); Assert.NotNull(cell);
                int x = cell.X, y = cell.Y;
                var filled = View(x, y); Assert.AreSame(owner, filled.Owner); Assert.True(filled.NativeForeground);
                Assert.True(filled.RepresentedMire); Assert.False(filled.RepresentedBrine);
                int filledFrame = MireFrame(filled.ForegroundTile, false); filledFrames.Add(filledFrame);
                if (filledFrame == 16) filledBase++;
                if (!samples.ContainsKey(filledFrame)) samples.Add(filledFrame, owner);
                filledTiles.Add(owner, filled.ForegroundTile);
                for (int repeat = 0; repeat < 3; repeat++) Assert.AreSame(filled.ForegroundTile, View(x, y).ForegroundTile);
                Native(owner, Atlas, x, y);
                Assert.AreEqual(60, owner.GetPart<LiquidPoolPart>().Volume);
                owner.GetPart<LiquidPoolPart>().Volume = 0;
                var film = View(x, y); Assert.AreSame(owner, film.Owner); Assert.True(film.NativeForeground);
                Assert.True(film.RepresentedMire); Assert.False(film.RepresentedBrine); Assert.False(film.LocalGround);
                int filmFrame = MireFrame(film.ForegroundTile, true); filmFrames.Add(filmFrame);
                if (filmFrame == 17) filmBase++;
                filmTiles.Add(owner, film.ForegroundTile); Assert.AreNotSame(filled.ForegroundTile, film.ForegroundTile);
                for (int repeat = 0; repeat < 3; repeat++) Assert.AreSame(film.ForegroundTile, View(x, y).ForegroundTile);
                Native(owner, Atlas, x, y); Assert.AreEqual(0, owner.GetPart<LiquidPoolPart>().Volume);
                Assert.True(zone.TileState.HasCoating(x, y, "bog-mire")); Assert.False(zone.TileState.HasCoating(x, y, "water"));
                owner.GetPart<LiquidPoolPart>().Volume = 60;
                Assert.AreSame(filled.ForegroundTile, View(x, y).ForegroundTile);
                Assert.AreEqual(200, owner.GetPart<DestructiblePart>().HP);
            }
            CollectionAssert.AreEquivalent(new[] { 16, 22, 23, 24 }, filledFrames);
            CollectionAssert.AreEquivalent(new[] { 17, 25, 26, 27 }, filmFrames);
            Assert.That(filledBase, Is.InRange(160, 224), "Quiet base material must dominate an ordinary area.");
            Assert.That(filmBase, Is.InRange(160, 224), "Depleted film must retain the same restrained density.");
            Assert.AreEqual(state, zone.TileState.ToSaveString()); Assert.AreEqual(version, zone.EntityVersion);
            // Replacing an owner at a known coordinate must not reshuffle the material.
            foreach (var old in samples.Values)
            {
                var cell = zone.GetEntityCell(old); int x = cell.X, y = cell.Y;
                Assert.True(zone.RemoveEntity(old)); var replacement = Add("MirePool", x, y);
                Assert.AreNotSame(old, replacement); Assert.AreSame(filledTiles[old], View(x, y).ForegroundTile);
                replacement.GetPart<LiquidPoolPart>().Volume = 0;
                Assert.AreSame(filmTiles[old], View(x, y).ForegroundTile);
                Assert.True(View(x, y).RepresentedMire); Assert.True(zone.TileState.HasCoating(x, y, "bog-mire"));
            }
        }

        [TestCase("Duckboard", false, 8)] [TestCase("PeatBank", true, 10)]
        [TestCase("DeadTree", true, 12)] [TestCase("MirePool", false, 200)]
        public void ExactShippedMaterialIsNativeButCustomIdentityIsNot(string blueprint, bool solid, int hp)
        {
            var e = Add(blueprint); string state = zone.TileState.ToSaveString(); Native(e);
            Assert.AreEqual(solid, View().BlocksMovement); Assert.AreEqual(hp, e.GetPart<DestructiblePart>().HP);
            Assert.AreEqual(state, zone.TileState.ToSaveString(), "Rendering/reader lookup must not seed or mutate state.");
            int frame = (int)View().ForegroundTile.sprite.rect.x / 16;
            if (blueprint == "Duckboard") Assert.AreEqual(0, frame);
            if (blueprint == "MirePool") MireFrame(View().ForegroundTile, false);
            if (blueprint == "PeatBank") Assert.That(frame, Is.InRange(18, 19));
            if (blueprint == "DeadTree") Assert.That(frame, Is.InRange(20, 21));
            e.GetPart<RenderPart>().VisualVariant = "custom"; IdentityFallback();
            e.GetPart<RenderPart>().VisualVariant = ""; Native(e);
        }
        [Test] public void ReedsReuseApprovedFramesWithoutAliasingCustomGlyphVariants()
        {
            var reeds = Add("Reeds"); Native(reeds, "Sprites/Live16x24/opening-base");
            Assert.AreEqual(new Rect((3 + ((X + Y) & 1)) * 16, 0, 16, 24), View().ForegroundTile.sprite.rect);
            Assert.False(View().BlocksMovement); reeds.GetPart<RenderPart>().GlyphVariants = "|/"; IdentityFallback();
        }
        [Test] public void AllSixteenBoardMasksDescribeOnlyCurrentCardinalBoards()
        {
            for (int mask = 0; mask < 16; mask++)
            {
                zone = Attach(Crossing); var board = Add("Duckboard");
                if ((mask & 1) != 0) Add("Duckboard", X, Y - 1);
                if ((mask & 2) != 0) Add("Duckboard", X + 1, Y);
                if ((mask & 4) != 0) Add("Duckboard", X, Y + 1);
                if ((mask & 8) != 0) Add("Duckboard", X - 1, Y);
                Add("Duckboard", X + 1, Y + 1); BoardFrame(mask); Assert.False(View().BlocksMovement);
                foreach (var at in new[] { (X, Y - 1), (X + 1, Y), (X, Y + 1), (X - 1, Y) })
                    foreach (var e in zone.GetCell(at.Item1, at.Item2).Objects.ToArray()) zone.RemoveEntity(e);
                BoardFrame(0); Assert.AreEqual(8, board.GetPart<DestructiblePart>().HP);
            }
        }
        [TestCase("hidden")] [TestCase("unexplored")] [TestCase("covered")]
        [TestCase("gone")] [TestCase("foreign-material")]
        public void BoardConnectionCannotReadHiddenCoveredOrInvalidNeighbor(string fault)
        {
            Add("Duckboard"); var east = Add("Duckboard", X + 1, Y); BoardFrame(2);
            if (fault == "hidden") zone.GetCell(X + 1, Y).IsVisible = false;
            if (fault == "unexplored") zone.GetCell(X + 1, Y).Explored = false;
            if (fault == "covered") Add("Dagger", X + 1, Y).GetPart<RenderPart>().RenderLayer = 100;
            if (fault == "gone") east.GetPart<DestructiblePart>().Gone = true;
            if (fault == "foreign-material") east.GetPart<MaterialPart>().ParentEntity = new Entity();
            BoardFrame(0);
        }
        [Test] public void RealMireDefinitionAndSourceStayMireWhenStockIsDepleted()
        {
            var mire = Add("MirePool"); Assert.AreEqual("&y", mire.GetPart<RenderPart>().ColorString);
            Assert.AreEqual("bog-mire", mire.GetPart<TileStateSourcePart>().EffectiveCoating);
            Assert.True(zone.TileState.HasCoating(X, Y, "bog-mire")); Assert.False(zone.TileState.HasCoating(X, Y, "water"));
            Native(mire); var filled = View().ForegroundTile;
            mire.GetPart<LiquidPoolPart>().Volume = 0; mire.GetPart<TileStateSourcePart>().Seed(zone, X, Y);
            string state = zone.TileState.ToSaveString(); Native(mire);
            MireFrame(filled, false); MireFrame(View().ForegroundTile, true);
            Assert.AreNotSame(filled, View().ForegroundTile);
            Assert.AreEqual(state, zone.TileState.ToSaveString()); Assert.AreEqual(0, mire.GetPart<LiquidPoolPart>().Volume);
            Assert.True(zone.TileState.HasCoating(X, Y, "bog-mire"));
            Assert.False(View().RepresentedBrine); Assert.AreNotEqual(FlatPixelGreyboxGround.Water, View().GroundKind);
            Assert.False(View().LocalGround, "An exhausted but still coated pool cannot become borrowed dry Grass.");
        }
        [TestCase("water")] [TestCase("ice")] [TestCase("oil")] [TestCase("unknown-coating")]
        public void MireWithAdditionalLayerKeepsTruthfulFallback(string coating)
        {
            Add("MirePool"); Native(View().Owner); zone.TileState.WriteCoating(X, Y, coating, 3); IdentityFallback();
            Assert.True(zone.TileState.HasCoating(X, Y, "bog-mire")); Assert.False(View().RepresentedBrine);
        }
        [Test] public void DryGroundRemainsBelowActorButNeverUnderLiveWaterOrMire()
        {
            var grass = Add("Grass"); var actor = Add("Player"); actor.GetPart<RenderPart>().RenderLayer = 100;
            var dry = View(); Assert.AreSame(actor, dry.Owner); Assert.True(dry.LocalGround); NativeTile(dry.GroundTile, CurrentGroundAtlas);
            foreach (string coating in new[] { "water", "bog-mire", "ice", "oil" })
            {
                zone.TileState.WriteCoating(X, Y, coating, 3); var wet = View(); Assert.AreSame(actor, wet.Owner);
                Assert.False(wet.LocalGround, coating + " cannot borrow opaque dry Grass.");
                Assert.AreNotSame(dry.GroundTile, wet.GroundTile); zone.TileState.Clear(X, Y);
            }
            Assert.AreSame(dry.GroundTile, View().GroundTile); Assert.AreSame(zone.GetCell(X, Y), zone.GetEntityCell(grass));
        }
        [Test] public void HiddenOwnerCannotChangeSubstrateButIndependentLiveCoatingCan()
        {
            Add("Grass"); var dry = View(); Assert.True(dry.LocalGround); NativeTile(dry.GroundTile, CurrentGroundAtlas);
            var mire = Add("MirePool", seed: false); mire.GetPart<RenderPart>().Visible = false;
            Assert.True(zone.TileState.HasCoating(X, Y, "bog-mire"), "AddEntity projects a real pool even without source.Seed.");
            Assert.False(View().LocalGround, "Hiding the source cannot erase its actual projected coating.");
            zone.TileState.Clear(X, Y); // Controlled fixture: separate hidden owner from independent hazard.
            Assert.AreSame(dry.GroundTile, View().GroundTile); Assert.AreEqual("Grass", View().Owner?.BlueprintName);
            mire.GetPart<TileStateSourcePart>().Seed(zone, X, Y);
            Assert.False(View().LocalGround); Assert.AreNotSame(dry.GroundTile, View().GroundTile);
            Assert.AreEqual("Grass", View().Owner?.BlueprintName); Assert.True(zone.TileState.HasCoating(X, Y, "bog-mire"));
        }
        // 15,8 is intentionally covered by the ordinary-formation positives below.
        [TestCase("Overworld.16.7.1")] [TestCase("Overworld.10.10.0")]
        public void SoddenMaterialsDoNotSilentlyAdmitDepthOrOtherRegionalFamilies(string id)
        { zone = Attach(id); Add("Duckboard"); IdentityFallback(); }
        [Test] public void StaleCurrentGraphRejectsAnOtherwiseQualifiedOwner()
        {
            var board = Add("Duckboard"); Native(board); manager.SetActiveZone(new Zone(Crossing));
            Assert.False(world.TryResolvePresentation(zone, X, Y, out _));
        }
        [TestCase(false)] [TestCase(true)]
        public void HiddenAndRememberedMireNeverReadItsLiveNativeStock(bool explored)
        {
            var mire = Add("MirePool"); Native(mire); zone.GetCell(X, Y).IsVisible = false; zone.GetCell(X, Y).Explored = explored;
            if (world.TryResolvePresentation(zone, X, Y, out var view)) Assert.False(view.NativeForeground);
            if (world.TryGetPresentationSubjectSprite(zone, mire, X, Y, out var sprite, out _))
                { Assert.AreNotSame(Resources.Load<Texture2D>(Atlas), sprite.texture); Assert.AreNotSame(Resources.Load<Texture2D>(FenAtlas), sprite.texture); }
        }
        [Test] public void ActualFreshShelterGenerationSuppliesTheMaterialsWithoutFixtureReskins()
        {
            Assert.True(SoddenDistrict.Eligible(manager, SoddenDistrictPlan.StopZoneID));
            zone = manager.GetZone(SoddenDistrictPlan.StopZoneID); Assert.NotNull(zone); manager.SetActiveZone(zone); Reveal(zone);
            foreach (string id in new[] { "Duckboard", "MirePool", "DeadTree", "Reeds", "StoneWall" })
            {
                var owner = zone.GetReadOnlyEntities().FirstOrDefault(e => e.BlueprintName == id); Assert.NotNull(owner, id);
                var cell = zone.GetEntityCell(owner); Assert.NotNull(cell);
                string expected = id == "Reeds" ? "Sprites/Live16x24/opening-base"
                    : id == "StoneWall" ? "Sprites/Live16x24/opening-cellar" : Atlas;
                Native(owner, expected, cell.X, cell.Y);
                if (id == "MirePool") Assert.True(zone.TileState.HasCoating(cell.X, cell.Y, "bog-mire"));
            }
        }

        [Test] public void ShippedAtlasUsesNativePointSampledCellsAndQueriesReuseTheirFrames()
        {
            var e = Add("Duckboard"); Native(e); var tile = View().ForegroundTile;
            var texture = Resources.Load<Texture2D>(Atlas);
            Assert.AreEqual(448, texture.width); Assert.AreEqual(24, texture.height);
            Assert.AreEqual(FilterMode.Point, texture.filterMode); Assert.AreEqual(TextureWrapMode.Clamp, texture.wrapMode);
            Assert.AreEqual(1, texture.mipmapCount); int version = zone.EntityVersion;
            for (int i = 0; i < 32; i++) Assert.AreSame(tile, View().ForegroundTile);
            Assert.AreEqual(version, zone.EntityVersion); Assert.AreEqual(8, e.GetPart<DestructiblePart>().HP);
        }
        [TestCase("Duckboard", "carried")] [TestCase("DeadTree", "equipped")]
        [TestCase("PeatBank", "takeable")] [TestCase("Duckboard", "glyph")]
        [TestCase("DeadTree", "glyph-variants")] [TestCase("PeatBank", "visual-id")]
        [TestCase("Duckboard", "foreign-render")] [TestCase("PeatBank", "foreign-physics")]
        [TestCase("DeadTree", "foreign-material")] [TestCase("Duckboard", "foreign-structure")]
        [TestCase("PeatBank", "duplicate-render")] [TestCase("Duckboard", "duplicate-physics")]
        [TestCase("DeadTree", "duplicate-material")] [TestCase("PeatBank", "item")]
        [TestCase("Duckboard", "zero-stack")] [TestCase("DeadTree", "foreign-stack")]
        [TestCase("PeatBank", "wrong-material")] [TestCase("Duckboard", "solid")]
        [TestCase("PeatBank", "non-solid")] [TestCase("DeadTree", "gone")]
        [TestCase("Duckboard", "zero-hp")] [TestCase("PeatBank", "same-glyph-other-blueprint")]
        public void MalformedOrRepurposedOwnerDoesNotAcquireNeutralArt(string blueprint, string fault)
        {
            Add("Grass"); var e = Add(blueprint); Native(e); var other = new Entity();
            switch (fault)
            {
                case "carried": e.GetPart<PhysicsPart>().InInventory = other; break;
                case "equipped": e.GetPart<PhysicsPart>().Equipped = other; break;
                case "takeable": e.GetPart<PhysicsPart>().Takeable = true; break;
                case "glyph": e.GetPart<RenderPart>().RenderString = "?"; break;
                case "glyph-variants": e.GetPart<RenderPart>().GlyphVariants = "Tt"; break;
                case "visual-id": e.GetPart<RenderPart>().VisualID = "other-material"; break;
                case "foreign-render": e.GetPart<RenderPart>().ParentEntity = other; break;
                case "foreign-physics": e.GetPart<PhysicsPart>().ParentEntity = other; break;
                case "foreign-material": e.GetPart<MaterialPart>().ParentEntity = other; break;
                case "foreign-structure": e.GetPart<DestructiblePart>().ParentEntity = other; break;
                case "duplicate-render": e.AddPart(new RenderPart()); break;
                case "duplicate-physics": e.AddPart(new PhysicsPart()); break;
                case "duplicate-material": e.AddPart(new MaterialPart { MaterialID = "Wood" }); break;
                case "item": e.SetTag("Item"); break;
                case "zero-stack": e.AddPart(new StackerPart { StackCount = 0 }); break;
                case "foreign-stack": e.AddPart(new StackerPart { StackCount = 1 }); e.GetPart<StackerPart>().ParentEntity = other; break;
                case "wrong-material": e.GetPart<MaterialPart>().MaterialID = "Metal"; break;
                case "solid": e.GetPart<PhysicsPart>().Solid = true; break;
                case "non-solid": e.GetPart<PhysicsPart>().Solid = false; e.Tags.Remove("Solid"); break;
                case "gone": e.GetPart<DestructiblePart>().Gone = true; break;
                case "zero-hp": e.GetPart<DestructiblePart>().HP = 0; break;
                case "same-glyph-other-blueprint": e.BlueprintName = "PeatBog"; break;
            }
            NotSoddenNative(e);
        }
        [TestCase("negative-stock")] [TestCase("water-id")] [TestCase("source-oil")]
        [TestCase("foreign-pool")] [TestCase("foreign-source")] [TestCase("foreign-thermal")]
        [TestCase("duplicate-pool")] [TestCase("frozen-temperature")]
        [TestCase("nan-temperature")] [TestCase("frozen-effect")] [TestCase("electrified-effect")]
        [TestCase("burning-effect")] [TestCase("wet-effect")] [TestCase("missing-coating")] [TestCase("expired-coating")]
        public void MireCannotClaimNeutralWetArtForUnsupportedLiveState(string fault)
        {
            var e = Add("MirePool"); Native(e); var other = new Entity();
            switch (fault)
            {
                case "negative-stock": e.GetPart<LiquidPoolPart>().Volume = -1; break;
                case "water-id": e.GetPart<LiquidPoolPart>().LiquidId = "water"; break;
                case "source-oil": e.GetPart<TileStateSourcePart>().Coating = "oil"; break;
                case "foreign-pool": e.GetPart<LiquidPoolPart>().ParentEntity = other; break;
                case "foreign-source": e.GetPart<TileStateSourcePart>().ParentEntity = other; break;
                case "foreign-thermal": e.GetPart<ThermalPart>().ParentEntity = other; break;
                case "duplicate-pool": e.AddPart(new LiquidPoolPart { LiquidId = "bog-mire", Volume = 10 }); break;
                case "frozen-temperature": e.GetPart<ThermalPart>().Temperature = e.GetPart<ThermalPart>().FreezeTemperature; break;
                case "nan-temperature": e.GetPart<ThermalPart>().Temperature = float.NaN; break;
                case "frozen-effect": e.ForceApplyEffect(new FrozenEffect(1), null, zone); break;
                case "electrified-effect": e.ForceApplyEffect(new ElectrifiedEffect(1), null, zone); break;
                case "burning-effect": e.ForceApplyEffect(new BurningEffect(), null, zone); break;
                case "wet-effect": e.ForceApplyEffect(new WetEffect(), null, zone); break;
                case "missing-coating": zone.TileState.Clear(X, Y); break;
                case "expired-coating": zone.TileState.Get(X, Y).Coatings[0].Turns = 0; break;
            }
            IdentityFallback();
        }
        [TestCase("heat")] [TestCase("cold")] [TestCase("charge")]
        public void TileEnergyKeepsNeutralMireBodyWhileOwnerEffectsUseFallback(string energy)
        {
            var e = Add("MirePool"); Native(e); float temperature = e.GetPart<ThermalPart>().Temperature;
            if (energy == "heat") zone.TileState.AddHeat(X, Y, 1);
            if (energy == "cold") zone.TileState.AddCold(X, Y, 1);
            if (energy == "charge") zone.TileState.AddCharge(X, Y, 1);
            string state = zone.TileState.ToSaveString(); Native(e);
            Assert.AreEqual(temperature, e.GetPart<ThermalPart>().Temperature); Assert.AreEqual(state, zone.TileState.ToSaveString());
            // These are tile energy values, not a claim about the submitted hazard mark.
            e.ForceApplyEffect(new WetEffect(), null, zone); IdentityFallback();
        }
        [Test] public void ActualPoolProjectionRenewalAndLastOwnerRemovalKeepDistinctTruths()
        {
            var first = Add("MirePool", seed: false);
            Assert.AreEqual(ZoneTileState.Permanent, zone.TileState.CoatingTurns(X, Y, "bog-mire"));
            Assert.AreEqual(1, zone.TileState.Get(X, Y).Coatings.Count);
            first.GetPart<LiquidPoolPart>().Volume = 0; Native(first);
            zone.TileState.Clear(X, Y); IdentityFallback();
            first.GetPart<TileStateSourcePart>().Seed(zone, X, Y);
            Assert.AreEqual(4, zone.TileState.CoatingTurns(X, Y, "bog-mire"));
            Assert.False(zone.TileState.HasCoating(X, Y, "water")); Native(first);
            var second = Add("MirePool", seed: false); Native(second);
            Assert.AreEqual(1, zone.TileState.Get(X, Y).Coatings.Count, "Same-liquid projection must merge, not fabricate water.");
            Assert.True(zone.RemoveEntity(first)); Assert.True(zone.TileState.HasCoating(X, Y, "bog-mire")); Native(second);
            Assert.True(zone.RemoveEntity(second)); Assert.False(zone.TileState.HasCoating(X, Y, "bog-mire"));
            Assert.IsNull(View().Owner); Assert.False(View().NativeForeground);
        }
        [TestCase("biome")] [TestCase("poi")] [TestCase("map-shape")]
        public void RealMapAuthorityIsRequiredAndReversible(string fault)
        {
            var e = Add("Duckboard"); Native(e); var map = manager.WorldMap; var tiles = map.Tiles;
            var poi = map.GetPOI(16, 7); var biome = map.GetBiome(16, 7);
            try
            {
                if (fault == "biome") map.Tiles[16, 7] = BiomeType.Beating;
                if (fault == "poi") map.SetPOI(16, 7, new PointOfInterest(POIType.Village, "Counterexample"));
                if (fault == "map-shape") map.Tiles = new BiomeType[1, 1];
                IdentityFallback();
            }
            finally { map.Tiles = tiles; map.Tiles[16, 7] = biome; map.SetPOI(16, 7, poi); }
            Native(e);
        }
        [Test] public void SameAddressWithoutWorldContextCannotBorrowRegionalArt()
        {
            var e = Add("Duckboard"); Native(e);
            zone = new Zone(Crossing); Reveal(zone); Assert.IsNull(WorldLocationContext.For(zone));
            Add("Duckboard"); IdentityFallback();
        }
        [Test] public void ExactSingleCellFootprintIsSupportedButRealLargerAndStaleBodiesAreNot()
        {
            var e = Add("Duckboard"); e.AddPart(new SpatialFootprintPart { CellsRaw = "0,0" }); Native(e);
            Assert.True(zone.TryChangeFootprint(e, "0,0;1,0")); IdentityFallback();
            Assert.True(world.TryResolvePresentation(zone, X + 1, Y, out var occupied)); Assert.AreSame(e, occupied.Owner);
            Assert.False(occupied.NativeForeground, "A16x24 stamp must not duplicate a larger physical body.");
            Assert.True(zone.TryChangeFootprint(e, "0,0")); Native(e);
            e.GetPart<SpatialFootprintPart>().CellsRaw = "0,0;0,1"; NotSoddenNative(e);
        }
        [Test] public void CoverRemovalAndOldReaderSubjectNeverReuseThePreviousNativeOwner()
        {
            var board = Add("Duckboard"); Native(board); Add("Duckboard", X + 1); BoardFrame(2);
            var actor = Add("Player"); actor.GetPart<RenderPart>().RenderLayer = 100;
            Assert.AreSame(actor, View().Owner); NotSoddenNative(board);
            Assert.True(zone.MoveEntity(actor, X - 2, Y)); Native(board); BoardFrame(2);
            Assert.True(zone.RemoveEntity(board)); Assert.AreNotSame(board, View().Owner); NotSoddenNative(board);
        }
        [TestCase("StoneFloor", "Sprites/Live16x24/cellar-floor", 0, 3)]
        [TestCase("Grass", FenAtlas, 0, 3)]
        public void ExactGroundUsesTheBorrowedRangeAndForeignVariantsStayFallback(string blueprint, string resource, int low, int high)
        {
            var e = Add(blueprint); var normal = View(); Assert.True(normal.LocalGround); NativeTile(normal.GroundTile, resource);
            Assert.IsNull(normal.ForegroundTile); Assert.That((int)normal.GroundTile.sprite.rect.x / 16, Is.InRange(low, high));
            e.GetPart<RenderPart>().VisualVariant = "custom-floor"; Assert.False(View().LocalGround);
            e.GetPart<RenderPart>().VisualVariant = ""; Assert.AreSame(normal.GroundTile, View().GroundTile);
            e.AddPart(new CultivatedSoilPart()); Assert.False(View().LocalGround);
        }
        [TestCase("Reeds")] [TestCase("DeadTree")] [TestCase("Duckboard")]
        public void SubmittedExactDryOverlayRetainsOnlyItsActualOrdinaryGrass(string blueprint)
        {
            var grass = Add("Grass"); var baseline = View(); NativeTile(baseline.GroundTile, CurrentGroundAtlas);
            var overlay = Add(blueprint);
            string atlas = blueprint == "Reeds" ? "Sprites/Live16x24/opening-base" : Atlas;
            Native(overlay, atlas); Assert.True(View().LocalGround);
            Assert.AreSame(baseline.GroundTile, View().GroundTile);
            string state = zone.TileState.ToSaveString(); int version = zone.EntityVersion;
            using (var paint = new Paint(world, zone))
            {
                paint.Step(); NativeTile(paint.At(X, Y), atlas);
                Assert.AreSame(baseline.GroundTile, paint.GroundAt(X, Y));
                long full = EnvironmentSpriteRenderer.Perf.FullPasses;
                Assert.AreEqual(state, zone.TileState.ToSaveString()); Assert.AreEqual(version, zone.EntityVersion);
                long cells = EnvironmentSpriteRenderer.Perf.CellsResolved; paint.Step();
                Assert.AreEqual(cells, EnvironmentSpriteRenderer.Perf.CellsResolved);
                Assert.True(zone.RemoveEntity(grass)); paint.Dirty(X, Y); paint.Step();
                Assert.AreSame(overlay, View().Owner); Assert.False(View().LocalGround);
                var absent = paint.GroundAt(X, Y);
                Assert.True(absent == null || absent.sprite.texture != Resources.Load<Texture2D>(CurrentGroundAtlas),
                    "An approved overlay cannot invent a Grass owner beneath itself.");
                var replacement = Add("Grass"); paint.Dirty(X, Y); paint.Step();
                Assert.AreSame(zone.GetCell(X, Y), zone.GetEntityCell(replacement));
                Assert.AreSame(baseline.GroundTile, paint.GroundAt(X, Y));
                Assert.AreEqual(full, EnvironmentSpriteRenderer.Perf.FullPasses);
            }
        }
        [TestCase("mire")] [TestCase("coating")] [TestCase("unknown-terrain")]
        [TestCase("cultivated")] [TestCase("custom-variant")]
        public void ExactDryOverlayCannotBypassActualCompetingGround(string condition)
        {
            Add("Grass"); var board = Add("Duckboard"); Assert.True(View().LocalGround);
            Entity extra = null; CultivatedSoilPart soil = null;
            if (condition == "mire") extra = Add("MirePool");
            if (condition == "coating") zone.TileState.WriteCoating(X, Y, "water", 3);
            if (condition == "unknown-terrain") { extra = Add("Duckboard"); extra.BlueprintName = "UnknownBogTerrain"; }
            if (condition == "cultivated") { soil = new CultivatedSoilPart(); board.AddPart(soil); }
            if (condition == "custom-variant") board.GetPart<RenderPart>().VisualVariant = "changed-board-state";
            string state = zone.TileState.ToSaveString();
            Assert.False(View().LocalGround, condition + " must keep its real ground priority.");
            Assert.AreEqual(state, zone.TileState.ToSaveString());
            if (extra != null) Assert.True(zone.RemoveEntity(extra));
            if (condition == "coating") zone.TileState.Clear(X, Y);
            if (soil != null) Assert.True(board.RemovePart(soil));
            if (condition == "custom-variant") board.GetPart<RenderPart>().VisualVariant = "";
            Native(board); Assert.True(View().LocalGround); NativeTile(View().GroundTile, CurrentGroundAtlas);
        }

        [Test] public void StoneFloorWinsOverGrassAndDuplicateFloorsRemainConservativeUnderNeutralBoards()
        {
            Add("Grass"); var stone = Add("StoneFloor"); NativeTile(View().GroundTile, "Sprites/Live16x24/cellar-floor");
            var duplicate = Add("StoneFloor"); Assert.False(View().LocalGround);
            Assert.True(zone.RemoveEntity(duplicate)); NativeTile(View().GroundTile, "Sprites/Live16x24/cellar-floor");
            var board = Add("Duckboard"); Native(board); Assert.True(View().LocalGround);
            NativeTile(View().GroundTile, "Sprites/Live16x24/cellar-floor");
            Assert.AreSame(board, View().Owner, "The actual dry overlay remains selected above its lower floor.");
            Assert.True(zone.RemoveEntity(board)); Assert.AreSame(stone, View().Owner);
        }
        [TestCase("Overworld.16.7.0", FenAtlas)] [TestCase("Overworld.10.10.0", "Sprites/Live16x24/sill-environment")]
        public void HiddenOrForeignRenderCultivationOwnerCannotEraseVisibleDryGround(string id, string expectedAtlas)
        {
            zone = Attach(id);
            var grass = Add("Grass");
            var baseline = View();
            Assert.True(baseline.LocalGround); NativeTile(baseline.GroundTile, expectedAtlas);
            var cell = zone.GetCell(X, Y);

            // Repurpose an actual non-floor terrain owner using public parts/tags.
            // A second Grass would correctly hit the separate duplicate-floor
            // ambiguity rule and would not isolate this disclosure failure.
            var bed = Add("Duckboard");
            bed.SetTag("Plantable");
            var soil = new CultivatedSoilPart(); bed.AddPart(soil);
            var render = bed.GetPart<RenderPart>();
            render.Visible = false;
            Assert.True(CultivatedSoilPart.IsCultivated(zone, cell),
                "Exercise the actual gameplay helper: it does not validate Render visibility.");
            var hidden = View();
            Assert.AreSame(grass, hidden.Owner);
            Assert.True(hidden.LocalGround, "Hidden owner-local cultivation must not erase disclosed ordinary ground.");
            Assert.AreSame(baseline.GroundTile, hidden.GroundTile);
            Assert.AreEqual(baseline.GroundColor, hidden.GroundColor);

            render.Visible = true;
            Assert.AreSame(bed, View().Owner);
            Assert.False(View().LocalGround, "The visible actual competing cultivated terrain still vetoes dry material.");

            render.ParentEntity = new Entity();
            Assert.True(CultivatedSoilPart.IsCultivated(zone, cell),
                "The gameplay helper also ignores Render ownership; this is not a custody counter.");
            var foreign = View();
            Assert.AreSame(grass, foreign.Owner);
            Assert.True(foreign.LocalGround);
            Assert.AreSame(baseline.GroundTile, foreign.GroundTile);
            Assert.AreEqual(baseline.GroundColor, foreign.GroundColor);

            render.ParentEntity = bed;
            Assert.False(View().LocalGround, "Restoring disclosed ownership restores real terrain priority.");
            render.Visible = false;
            Assert.True(bed.RemovePart(soil));
            Assert.False(CultivatedSoilPart.IsCultivated(zone, cell));
            Assert.AreSame(baseline.GroundTile, View().GroundTile);
            Assert.True(zone.RemoveEntity(bed));
            Assert.AreSame(grass, View().Owner);
            Assert.AreSame(baseline.GroundTile, View().GroundTile);
        }

        [TestCase(SoddenDistrictPlan.StopZoneID)]
        [TestCase(SoddenDistrictPlan.CrossingZoneID)]
        [TestCase(SoddenDistrictPlan.WorksZoneID)]
        public void WarmRegionalAuthorityLookupDoesNotAllocateAndStillReadsCurrentMap(string id)
        {
            zone = Attach(id);
            Assert.True(FlatPixelSoddenTravelCatalog.IsSupportedZone(zone));
            var point = WorldMap.FromZoneID(id); // Fixture preparation only, outside measurement.
            var previous = manager.WorldMap.Tiles[point.x, point.y];
            try
            {
                manager.WorldMap.Tiles[point.x, point.y] = BiomeType.Beating;
                Assert.False(FlatPixelSoddenTravelCatalog.IsSupportedZone(zone),
                    "An allocation fix must continue reading each exact district's actual map cell.");
            }
            finally { manager.WorldMap.Tiles[point.x, point.y] = previous; }
            Assert.True(FlatPixelSoddenTravelCatalog.IsSupportedZone(zone));

            try
            {
                // Validate this runtime's counter with retained, observable allocations.
                // A missing/no-op counter cannot silently certify the paint-path budget.
                var probe = new byte[32][];
                long probeBefore = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < probe.Length; i++) probe[i] = new byte[64];
                long probeBytes = GC.GetAllocatedBytesForCurrentThread() - probeBefore;
                GC.KeepAlive(probe);
                if (probeBytes <= 0) Assert.Inconclusive("Current-thread allocation counter did not detect the positive control.");

                for (int i = 0; i < 64; i++) FlatPixelSoddenTravelCatalog.IsSupportedZone(zone);
                bool all = true; long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 1024; i++) all &= FlatPixelSoddenTravelCatalog.IsSupportedZone(zone);
                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                Assert.True(all);
                Assert.AreEqual(0, allocated,
                    "Per-cell regional authority must not repeatedly split/parse an exact finite address.");
            }
            catch (PlatformNotSupportedException) { Assert.Inconclusive("Current-thread allocation counter is unavailable on this runtime."); }
            catch (NotImplementedException) { Assert.Inconclusive("Current-thread allocation counter is not implemented on this runtime."); }
        }

        [Test] public void ExplicitNullCatalogCannotSecretlyReloadOptionalRegionalResources()
        {
            var e = Add("Duckboard"); Native(e); world.Dispose(); world = new FlatPixelGreyboxCatalog((Texture2D)null);
            IdentityFallback(); Assert.True(Resources.Load<Texture2D>(Atlas) != null, "Countercheck runs with the real resource installed.");
        }
        [TestCase(false)] [TestCase(true)]
        public void MissingOrMalformedPacketFallsBackWithoutDisablingBorrowedMaterialGroups(bool malformed)
        {
            var bad = malformed ? new Texture2D(447, 24) : null;
            try
            {
                var board = Add("Duckboard"); Native(board); Inject(bad); IdentityFallback();
                Assert.True(zone.RemoveEntity(board)); var reeds = Add("Reeds"); Native(reeds, "Sprites/Live16x24/opening-base");
                Assert.True(zone.RemoveEntity(reeds)); Add("Grass"); NativeTile(View().GroundTile, CurrentGroundAtlas);
                var wall = Add("StoneWall"); Native(wall, "Sprites/Live16x24/opening-cellar");
                Assert.AreEqual(new Rect(8 * 16, 0, 16, 24), View().ForegroundTile.sprite.rect);
            }
            finally { world?.Dispose(); if (bad != null) Object.DestroyImmediate(bad); }
        }
        [Test] public void DisposingTheCatalogReleasesOwnedFramesButPreservesBorrowedArtAndSelector()
        {
            var source = Resources.Load<Texture2D>(Atlas); Assert.NotNull(source); Inject(source);
            var board = Add("Duckboard"); Native(board); var tile = View().ForegroundTile; var sprite = tile.sprite;
            world.Dispose(); world.Dispose(); Assert.True(tile == null); Assert.True(sprite == null); Assert.True(source != null);
            Assert.True(injectedSelector.TryResolve(zone, X, Y, out var selected)); Assert.AreSame(board, selected.Owner);
        }
        [Test] public void SubmittedBoardDestructionCapsNeighborsWithoutAFullRedraw()
        {
            for (int x = X - 1; x <= X + 1; x++) Add("Grass", x, Y);
            Add("Duckboard", X - 1); var center = Add("Duckboard"); Add("Duckboard", X + 1);
            using (var paint = new Paint(world, zone))
            {
                paint.Step(); BoardTile(paint.At(X - 1, Y), 2); BoardTile(paint.At(X, Y), 10); BoardTile(paint.At(X + 1, Y), 8);
                long full = EnvironmentSpriteRenderer.Perf.FullPasses;
                Assert.AreEqual(DestroyVerdict.Destroyed, DestructionSystem.Damage(center, 999, null, zone));
                Assert.IsNull(zone.GetEntityCell(center)); Assert.False(paint.FullDirty); paint.Step();
                BoardTile(paint.At(X - 1, Y), 0); BoardTile(paint.At(X + 1, Y), 0);
                var gap = paint.At(X, Y); Assert.True(gap == null || gap.sprite.texture != Resources.Load<Texture2D>(Atlas));
                Assert.False(zone.GetCell(X, Y).BlocksMovement()); Assert.AreEqual(full, EnvironmentSpriteRenderer.Perf.FullPasses);
                long cells = EnvironmentSpriteRenderer.Perf.CellsResolved; paint.Step(); Assert.AreEqual(cells, EnvironmentSpriteRenderer.Perf.CellsResolved);
            }
        }
        [Test] public void SubmittedNeighborDisclosureAndCoverChangesRepaintTheCurrentBoard()
        {
            Add("Duckboard"); Add("Duckboard", X + 1);
            using (var paint = new Paint(world, zone))
            {
                paint.Step(); BoardTile(paint.At(X, Y), 2); long full = EnvironmentSpriteRenderer.Perf.FullPasses;
                zone.GetCell(X + 1, Y).IsVisible = false; paint.Dirty(X + 1, Y); paint.Step(); BoardTile(paint.At(X, Y), 0);
                zone.GetCell(X + 1, Y).IsVisible = true; paint.Dirty(X + 1, Y); paint.Step(); BoardTile(paint.At(X, Y), 2);
                var cover = Add("Dagger", X + 1); cover.GetPart<RenderPart>().RenderLayer = 100;
                paint.Dirty(X + 1, Y); paint.Step(); BoardTile(paint.At(X, Y), 0);
                Assert.True(zone.RemoveEntity(cover)); paint.Dirty(X + 1, Y); paint.Step(); BoardTile(paint.At(X, Y), 2);
                zone.GetCell(X, Y).IsVisible = false; paint.Dirty(X, Y); paint.Step();
                var hidden = paint.At(X, Y); Assert.True(hidden == null || hidden.sprite.texture != Resources.Load<Texture2D>(Atlas));
                Assert.AreEqual(full, EnvironmentSpriteRenderer.Perf.FullPasses);
            }
        }
        [Test] public void SubmittedMireDepletionRemainsWetAndUnchangedStateDoesNotRepaint()
        {
            var mire = Add("MirePool");
            using (var paint = new Paint(world, zone))
            {
                paint.Step(); MireFrame(paint.At(X, Y), false); var filled = paint.At(X, Y);
                mire.GetPart<LiquidPoolPart>().Volume = 0; paint.Dirty(X, Y); string before = zone.TileState.ToSaveString(); paint.Step();
                MireFrame(paint.At(X, Y), true); Assert.AreNotSame(filled, paint.At(X, Y));
                Assert.AreEqual(before, zone.TileState.ToSaveString()); Assert.True(zone.TileState.HasCoating(X, Y, "bog-mire"));
                long cells = EnvironmentSpriteRenderer.Perf.CellsResolved; paint.Step(); Assert.AreEqual(cells, EnvironmentSpriteRenderer.Perf.CellsResolved);
            }
        }

        [TestCase(60, "none", '\0')] [TestCase(0, "none", '\0')]
        [TestCase(60, "charge", '*')] [TestCase(0, "charge", '*')]
        [TestCase(60, "heat", '^')] [TestCase(60, "cold", '^')]
        [TestCase(60, "embers", '"')] [TestCase(60, "veil-mist", '\'')]
        public void SubmittedMireRelinquishesOnlyItsOwnWave(int stock, string hazard, char expected)
        {
            var mire = Add("MirePool"); mire.GetPart<LiquidPoolPart>().Volume = stock;
            if (hazard == "charge") zone.TileState.AddCharge(X, Y, 1);
            if (hazard == "heat") zone.TileState.AddHeat(X, Y, 1);
            if (hazard == "cold") zone.TileState.AddCold(X, Y, 1);
            if (hazard == "embers") zone.TileState.WriteResidue(X, Y, "embers", 3);
            if (hazard == "veil-mist") zone.TileState.WriteCloud(X, Y, "veil-mist", 3);
            string state = zone.TileState.ToSaveString();
            using (var paint = new Paint(world, zone))
            {
                paint.Step(); NativeTile(paint.At(X, Y), CurrentMireAtlas);
                MireFrame(paint.At(X, Y), stock == 0);
                if (expected == '\0') Assert.IsNull(paint.Mark);
                else Assert.AreSame(CP437TilesetGenerator.GetTile(expected), paint.Mark);
                Assert.AreEqual(state, zone.TileState.ToSaveString()); Assert.True(zone.TileState.HasCoating(X, Y, "bog-mire"));
                long full = EnvironmentSpriteRenderer.Perf.FullPasses, cells = EnvironmentSpriteRenderer.Perf.CellsResolved;
                paint.Step(); Assert.AreEqual(cells, EnvironmentSpriteRenderer.Perf.CellsResolved);
                zone.GetCell(X, Y).IsVisible = false; paint.Dirty(X, Y); paint.Step(); Assert.IsNull(paint.Mark);
                Assert.AreEqual(full, EnvironmentSpriteRenderer.Perf.FullPasses);
            }
        }
        [Test] public void SubmittedMireCoverUncoverRestoresTheWaveUntilNativeSurfaceReturns()
        {
            var mire = Add("MirePool");
            using (var paint = new Paint(world, zone))
            {
                paint.Step(); NativeTile(paint.At(X, Y), CurrentMireAtlas); Assert.IsNull(paint.Mark);
                long full = EnvironmentSpriteRenderer.Perf.FullPasses;
                var actor = Add("Player"); actor.GetPart<RenderPart>().RenderLayer = 100;
                paint.Dirty(X, Y); paint.Step(); Assert.AreSame(actor, View().Owner);
                Assert.AreSame(CP437TilesetGenerator.GetTile('~'), paint.Mark);
                Assert.AreEqual(new Color(.30f, .55f, .95f), paint.MarkTint);
                NotSoddenNative(mire);
                Assert.True(zone.MoveEntity(actor, X - 1, Y)); paint.Dirty(X, Y); paint.Step();
                NativeTile(paint.At(X, Y), CurrentMireAtlas); Assert.IsNull(paint.Mark);
                Assert.AreEqual(full, EnvironmentSpriteRenderer.Perf.FullPasses);
                Assert.True(zone.TileState.HasCoating(X, Y, "bog-mire"));
            }
        }
        [TestCase("oil")] [TestCase("ice")] [TestCase("unknown-coating")]
        [TestCase("wet-owner")] [TestCase("burning-owner")]
        [TestCase("missing-resource")] [TestCase("malformed-resource")]
        public void SubmittedMireRefusalRetainsTheExistingCoatingCue(string refusal)
        {
            var mire = Add("MirePool"); Native(mire); Texture2D bad = null;
            try
            {
                if (refusal == "oil" || refusal == "ice" || refusal == "unknown-coating")
                    zone.TileState.WriteCoating(X, Y, refusal, 3);
                if (refusal == "wet-owner") mire.ForceApplyEffect(new WetEffect(), null, zone);
                if (refusal == "burning-owner") mire.ForceApplyEffect(new BurningEffect(), null, zone);
                if (refusal == "missing-resource") Inject(null);
                if (refusal == "malformed-resource") { bad = new Texture2D(447, 24); Inject(bad); }
                string state = zone.TileState.ToSaveString();
                using (var paint = new Paint(world, zone))
                {
                    paint.Step(); Assert.False(View().NativeForeground);
                    var submitted = paint.At(X, Y);
                    Assert.True(submitted == null || submitted.sprite.texture != Resources.Load<Texture2D>(CurrentMireAtlas));
                    Assert.AreSame(CP437TilesetGenerator.GetTile('~'), paint.Mark);
                    Color expected = refusal == "oil" ? new Color(.35f, .25f, .45f)
                        : refusal == "ice" ? new Color(.85f, .95f, 1) : new Color(.30f, .55f, .95f);
                    Assert.AreEqual(expected, paint.MarkTint);
                    Assert.AreEqual(state, zone.TileState.ToSaveString()); Assert.True(zone.TileState.HasCoating(X, Y, "bog-mire"));
                }
            }
            finally { if (bad != null) Object.DestroyImmediate(bad); }
        }

        // Ordinary-area scope extension. Positive graphs use the real seed65
        // map and complete pipeline, never FormationOverride or rewritten POIs.
        string OrdinaryAddress(Formation formation)
        {
            // Exact seed65 addresses entered by ordinary travel in the six before captures.
            string id = formation == Formation.BogFace ? "Overworld.15.8.0"
                : formation == Formation.ReedMaze ? "Overworld.16.8.0"
                : formation == Formation.DrownedCopse ? "Overworld.17.8.0"
                : formation == Formation.OpenMire ? "Overworld.18.8.0"
                : formation == Formation.Causeway ? "Overworld.19.7.0"
                : formation == Formation.PeatCuts ? "Overworld.15.5.0" : null;
            Assert.NotNull(id, "Expected one of the six ordinary Sodden formations.");
            var point = WorldMap.FromZoneID(id);
            Assert.False(SoddenDistrictPlan.IsSupportedZone(id)); Assert.True(SoddenCompositionPlan.IsWildernessZone(id));
            Assert.AreEqual(BiomeType.Sodden, manager.WorldMap.GetBiome(point.x, point.y));
            Assert.IsNull(manager.WorldMap.GetPOI(point.x, point.y));
            Assert.AreEqual(formation, FormationSelector.For(BiomeType.Sodden, id));
            return id;
        }
        void GenerateOrdinary(Formation formation)
        {
            string id = OrdinaryAddress(formation);
            Assert.False(manager.CachedZones.ContainsKey(id), "The proving graph must come from the actual cold pipeline.");
            zone = manager.GetZone(id); Assert.NotNull(zone, id); manager.SetActiveZone(zone); Reveal(zone);
            Assert.AreEqual(formation, FormationSelector.For(BiomeType.Sodden, id));
            Assert.AreSame(zone, manager.CachedZones[id]); Assert.AreSame(manager, WorldLocationContext.For(zone));
        }
        Entity SelectedGenerated(string blueprint, Func<Cell, bool> accepts = null)
        {
            foreach (var owner in zone.GetReadOnlyEntities())
            {
                if (owner.BlueprintName != blueprint) continue;
                var cell = zone.GetEntityCell(owner);
                if (cell == null || accepts != null && !accepts(cell)) continue;
                if (world.TryResolveFallback(zone, cell.X, cell.Y, out var selected)
                    && !selected.Remembered && ReferenceEquals(selected.Owner, owner)) return owner;
            }
            Assert.Fail(zone.ZoneID + " has no selected real " + blueprint + " for this assertion."); return null;
        }
        void AssertNativeGenerated(Entity owner)
        {
            var cell = zone.GetEntityCell(owner); Assert.NotNull(cell);
            Native(owner, owner.BlueprintName == "Reeds" ? "Sprites/Live16x24/opening-base" : Atlas, cell.X, cell.Y);
        }
        static bool HasRuntimeLairLedger(OverworldZoneManager owner)
        {
            // Inspection only: Inspect/BindForSave would themselves create a ledger.
            var field = typeof(LairStacks).GetField("Ledgers", Static); Assert.NotNull(field);
            var ledgers = (System.Runtime.CompilerServices.ConditionalWeakTable<OverworldZoneManager, LairStackLedgerPart>)field.GetValue(null);
            return ledgers.TryGetValue(owner, out _);
        }

        [TestCase(Formation.OpenMire)] [TestCase(Formation.PeatCuts)] [TestCase(Formation.ReedMaze)]
        [TestCase(Formation.DrownedCopse)] [TestCase(Formation.Causeway)] [TestCase(Formation.BogFace)]
        public void OrdinaryNaturalFormationUsesExistingMaterialOwnersAndRefusesCustomIdentity(Formation formation)
        {
            GenerateOrdinary(formation);
            string signature = zone.TileState.ToSaveString(); int version = zone.EntityVersion;
            string key = formation == Formation.Causeway ? "Duckboard" : formation == Formation.DrownedCopse ? "DeadTree"
                : formation == Formation.PeatCuts || formation == Formation.BogFace ? "PeatBank"
                : formation == Formation.ReedMaze ? "Reeds" : "MirePool";
            var subject = SelectedGenerated(key); AssertNativeGenerated(subject);
            if (formation != Formation.DrownedCopse) AssertNativeGenerated(SelectedGenerated("MirePool"));
            var grass = SelectedGenerated("Grass", c => zone.TileState.Get(c.X, c.Y)?.IsEmpty != false
                && !c.Objects.Any(e => e.HasPart<CultivatedSoilPart>()));
            var at = zone.GetEntityCell(grass); var dry = View(at.X, at.Y);
            Assert.True(dry.LocalGround); NativeTile(dry.GroundTile, CurrentGroundAtlas);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, grass, at.X, at.Y, out var portrait, out _));
            Assert.AreSame(Resources.Load<Texture2D>(CurrentGroundAtlas), portrait.texture);
            Assert.AreEqual(signature, zone.TileState.ToSaveString()); Assert.AreEqual(version, zone.EntityVersion);
            var render = subject.GetPart<RenderPart>(); string original = render.VisualVariant;
            try
            {
                render.VisualVariant = "foreign ordinary look"; var cell = zone.GetEntityCell(subject);
                IdentityFallback(cell.X, cell.Y);
            }
            finally { render.VisualVariant = original; }
            AssertNativeGenerated(subject);
        }

        [TestCase(Formation.OpenMire, "biome")] [TestCase(Formation.BogFace, "poi")]
        [TestCase(Formation.ReedMaze, "map-shape")] [TestCase(Formation.Causeway, "poi-shape")]
        public void OrdinaryAuthorityReadsItsOwnCoordinatesAndRevokesWarmApproval(Formation formation, string fault)
        {
            string id = OrdinaryAddress(formation); var point = WorldMap.FromZoneID(id);
            zone = Attach(id); var board = Add("Duckboard"); Native(board);
            var map = manager.WorldMap; var tiles = map.Tiles; var pois = map.POIs;
            var previousBiome = map.GetBiome(point.x, point.y); var previousPOI = map.GetPOI(point.x, point.y);
            try
            {
                for (int i = 0; i < 4; i++) Assert.True(FlatPixelSoddenTravelCatalog.IsSupportedZone(zone));
                if (fault == "biome") map.Tiles[point.x, point.y] = BiomeType.Beating;
                if (fault == "poi") map.SetPOI(point.x, point.y, new PointOfInterest(POIType.MerchantCamp, "ordinary counterexample"));
                if (fault == "map-shape") map.Tiles = new BiomeType[1, 1];
                if (fault == "poi-shape") map.POIs = new PointOfInterest[1, 1];
                Assert.False(FlatPixelSoddenTravelCatalog.IsSupportedZone(zone)); IdentityFallback();
            }
            finally
            {
                map.Tiles = tiles; map.POIs = pois;
                map.Tiles[point.x, point.y] = previousBiome; map.SetPOI(point.x, point.y, previousPOI);
            }
            Native(board);
        }

        [TestCase(false)] [TestCase(true)]
        public void OrdinarySavedLairAuthorityOutlivesErasedMapLabels(bool legacy)
        {
            string id = OrdinaryAddress(Formation.OpenMire); var point = WorldMap.FromZoneID(id);
            zone = Attach(id); var control = Add("Duckboard"); Native(control);
            var ordinaryManager = manager; var ordinaryGraph = zone;
            try
            {
                manager = OverworldZoneManager.CreateDetached(content.Factory, 65, true);
                Assert.IsNull(manager.WorldMap.GetPOI(point.x, point.y));
                manager.WorldMap.SetPOI(point.x, point.y, new PointOfInterest(POIType.Lair, "saved authority probe", null, 1, "MawToad"));
                if (legacy) LairStacks.Restore(manager, null);
                zone = manager.GetZone(id); Assert.NotNull(zone); manager.SetActiveZone(zone); Reveal(zone);
                var claim = LairStacks.Inspect(manager, id); Assert.NotNull(claim); Assert.AreEqual(legacy, claim.Legacy);
                manager.WorldMap.SetPOI(point.x, point.y, null);
                Assert.AreEqual(BiomeType.Sodden, manager.WorldMap.GetBiome(point.x, point.y));
                Assert.AreSame(zone, manager.CachedZones[id]);
                Assert.False(FlatPixelSoddenTravelCatalog.IsSupportedZone(zone), "The saved lair still owns this otherwise ordinary-looking column.");
                var ground = SelectedGenerated("Grass"); var cell = zone.GetEntityCell(ground);
                Add("Duckboard", cell.X, cell.Y); IdentityFallback(cell.X, cell.Y);
                Assert.NotNull(LairStacks.Inspect(manager, id), "Presentation must not discard the saved authority.");
            }
            finally { manager = ordinaryManager; zone = ordinaryGraph; }
            Native(control);
        }

        [TestCase("unbound")] [TestCase("replaced")]
        public void OrdinaryMatchingAddressDoesNotAuthorizeUnboundOrReplacedGraph(string fault)
        {
            string id = OrdinaryAddress(Formation.ReedMaze); zone = Attach(id);
            var owner = Add("Duckboard"); Native(owner);
            if (fault == "unbound")
            {
                zone = new Zone(id); Reveal(zone); Assert.IsNull(WorldLocationContext.For(zone));
                owner = Add("Duckboard"); IdentityFallback();
            }
            else
            {
                manager.SetActiveZone(new Zone(id));
                Assert.False(world.TryResolvePresentation(zone, X, Y, out _));
                Assert.False(world.TryGetPresentationSubjectSprite(zone, owner, X, Y, out _, out _));
            }
            Assert.False(FlatPixelSoddenTravelCatalog.IsSupportedZone(zone));
            manager.SetActiveZone(zone); Native(owner);
        }

        [TestCase("Overworld.16.1.0")] [TestCase("Overworld.16.4.0")]
        [TestCase("Overworld.10.11.0")] [TestCase("Overworld.16.08.0")]
        public void OrdinaryScopeDoesNotAdmitAuthoredSitesWrongCountryOrNoncanonicalAliases(string id)
        {
            string normalID = OrdinaryAddress(Formation.BogFace); zone = Attach(normalID); Native(Add("Duckboard"));
            var point = WorldMap.FromZoneID(id); var map = manager.WorldMap;
            var previousBiome = map.GetBiome(point.x, point.y); var previousPOI = map.GetPOI(point.x, point.y);
            try
            {
                // Counterexample only: altered map labels cannot grant canonical ordinary-address authority.
                map.Tiles[point.x, point.y] = BiomeType.Sodden; map.SetPOI(point.x, point.y, null);
                zone = Attach(id); Add("Duckboard");
                Assert.False(FlatPixelSoddenTravelCatalog.IsSupportedZone(zone)); IdentityFallback();
            }
            finally { map.Tiles[point.x, point.y] = previousBiome; map.SetPOI(point.x, point.y, previousPOI); }
            zone = Attach(normalID); Native(Add("Duckboard"));
        }

        [TestCase("Grass")] [TestCase("DeadTree")]
        public void OrdinaryCopseSubmitsRealPlainWaterWithoutClaimingMire(string blueprint)
        {
            GenerateOrdinary(Formation.DrownedCopse);
            var owner = SelectedGenerated(blueprint, c =>
            {
                var s = zone.TileState.Get(c.X, c.Y);
                return s != null && s.Coatings.Count == 1 && s.Coatings[0].Id == "water" && s.Coatings[0].Turns > 0
                    && s.Residues.Count == 0 && s.Heat == 0 && s.Cold == 0 && s.Charge == 0 && string.IsNullOrEmpty(s.Cloud);
            });
            var cell = zone.GetEntityCell(owner); int x = cell.X, y = cell.Y;
            Assert.True(FlatPixelSoddenTravelCatalog.IsSupportedZone(zone));
            string state = zone.TileState.ToSaveString();
            using (var paint = new Paint(world, zone))
            {
                paint.Step(); var wet = View(x, y);
                Assert.AreSame(owner, wet.Owner); Assert.False(wet.RepresentedMire); Assert.False(wet.RepresentedBrine); Assert.False(wet.LocalGround);
                if (blueprint == "DeadTree") NativeTile(paint.At(x, y), Atlas);
                Assert.AreSame(CP437TilesetGenerator.GetTile('~'), paint.MarkAt(x, y));
                Assert.AreEqual(state, zone.TileState.ToSaveString());
                Assert.True(zone.TileState.RemoveCoating(x, y, "water")); paint.Dirty(x, y); paint.Step();
                Assert.IsNull(paint.MarkAt(x, y)); Assert.True(View(x, y).LocalGround); NativeTile(paint.GroundAt(x, y), CurrentGroundAtlas);
                zone.TileState.WriteCoating(x, y, "water", ZoneTileState.Permanent); paint.Dirty(x, y); paint.Step();
                Assert.AreSame(CP437TilesetGenerator.GetTile('~'), paint.MarkAt(x, y)); Assert.False(View(x, y).RepresentedMire);
                Assert.True(zone.TileState.HasCoating(x, y, "water")); Assert.False(zone.TileState.HasCoating(x, y, "bog-mire"));
            }
        }

        [Test] public void OrdinaryQueriesDoNotCreateLedgerGenerateGraphOrChangeMaterialState()
        {
            string id = OrdinaryAddress(Formation.Causeway); zone = Attach(id);
            var grass = Add("Grass"); var board = Add("Duckboard");
            Assert.False(HasRuntimeLairLedger(manager), "This test needs a genuinely uninitialized ledger.");
            var owners = zone.GetReadOnlyEntities().ToArray(); int version = zone.EntityVersion, cached = manager.CachedZoneCount;
            string state = zone.TileState.ToSaveString(); var map = manager.WorldMap;
            for (int i = 0; i < 16; i++)
            {
                Native(board); Assert.True(View().LocalGround); NativeTile(View().GroundTile, CurrentGroundAtlas);
                Assert.True(FlatPixelSoddenTravelCatalog.IsSupportedZone(zone));
            }
            Assert.AreSame(map, manager.WorldMap); Assert.AreSame(zone, manager.CachedZones[id]);
            Assert.AreEqual(cached, manager.CachedZoneCount); Assert.AreEqual(version, zone.EntityVersion);
            Assert.AreEqual(state, zone.TileState.ToSaveString()); CollectionAssert.AreEquivalent(owners, zone.GetReadOnlyEntities());
            Assert.AreSame(grass, zone.GetCell(X, Y).Objects.First(e => e.BlueprintName == "Grass"));
            Assert.False(HasRuntimeLairLedger(manager), "Read-only scope must not call Inspect, TryPlan or other ledger initialization.");
            // No allocation claim: the existing native allocation probes are inconclusive on this runtime.
        }

        [Test] public void OrdinaryNaturalCausewayDestructionRepaintsOnlyCurrentNeighborConnections()
        {
            GenerateOrdinary(Formation.Causeway);
            var board = SelectedGenerated("Duckboard", c => c.X < Zone.Width - 1
                && world.TryResolveFallback(zone, c.X + 1, c.Y, out var neighbor) && neighbor.Owner?.BlueprintName == "Duckboard");
            var cell = zone.GetEntityCell(board); int x = cell.X, y = cell.Y;
            Assert.True(world.TryResolveFallback(zone, x + 1, y, out var removedView)); var removed = removedView.Owner;
            using (var paint = new Paint(world, zone))
            {
                paint.Step(); NativeTile(paint.At(x, y), Atlas);
                int before = (int)paint.At(x, y).sprite.rect.x / 16; Assert.AreNotEqual(0, before & 2);
                long full = EnvironmentSpriteRenderer.Perf.FullPasses;
                Assert.AreEqual(DestroyVerdict.Destroyed, DestructionSystem.Damage(removed, 999, null, zone));
                Assert.IsNull(zone.GetEntityCell(removed)); Assert.False(paint.FullDirty); paint.Step();
                NativeTile(paint.At(x, y), Atlas); int after = (int)paint.At(x, y).sprite.rect.x / 16;
                Assert.AreEqual(before & ~2, after); Assert.AreEqual(full, EnvironmentSpriteRenderer.Perf.FullPasses);
                long resolved = EnvironmentSpriteRenderer.Perf.CellsResolved; paint.Step();
                Assert.AreEqual(resolved, EnvironmentSpriteRenderer.Perf.CellsResolved);
            }
        }

        // Existing KitchenBrine submitted-paint pattern, kept local and small.
        // No alternative renderer, synthetic native selector or new test framework.
        sealed class Paint : IDisposable
        {
            readonly HotbarSaveFixture saved;
            readonly GameObject root;
            readonly Tilemap overlay, marks, ground;
            public ZoneRenderer Renderer { get; }
            readonly Action<int, int, string> oldCell = ZoneRenderHooks.CellDirtyCallback;
            readonly Action<string> oldFull = ZoneRenderHooks.FullDirtyCallback;
            readonly FieldInfo bound = typeof(ZoneTileStateSystem).GetField("_boundRenderZone", Static);
            readonly object oldBound; readonly Zone previous; readonly Action<int, int> oldChanged; readonly Action oldSight;
            readonly Dictionary<FieldInfo, object> perf = new Dictionary<FieldInfo, object>();
            public Paint(FlatPixelGreyboxCatalog catalog, Zone z)
            {
                oldBound = bound.GetValue(null);
                if (oldBound is WeakReference<Zone> weak && weak.TryGetTarget(out previous))
                { oldChanged = previous.TileState.OnCellChanged; oldSight = previous.TileState.OnSightChanged; }
                try
                {
                    foreach (var field in typeof(EnvironmentSpriteRenderer.Perf).GetFields(BindingFlags.Public | BindingFlags.Static)) perf[field] = field.GetValue(null);
                    saved = new HotbarSaveFixture(); root = new GameObject("Sodden submitted grid", typeof(Grid)); saved.Root.transform.SetParent(root.transform, false);
                    var main = saved.Root.GetComponent<Tilemap>(); var bg = new GameObject("Background", typeof(Tilemap)); bg.transform.SetParent(root.transform, false); ground = bg.GetComponent<Tilemap>();
                    var mark = new GameObject("Hazards", typeof(Tilemap)); mark.transform.SetParent(root.transform, false); marks = mark.GetComponent<Tilemap>();
                    Renderer = saved.Renderer; Write(Renderer, "_tilemap", main); Write(Renderer, "_bgTilemap", bg.GetComponent<Tilemap>()); Write(Renderer, "_tileStateTilemap", marks);
                    var camera = new GameObject("Camera", typeof(Camera)); camera.transform.SetParent(root.transform, false); camera.GetComponent<Camera>().orthographic = true; Write(Renderer, "_mainCamera", camera.GetComponent<Camera>());
                    var env = saved.Root.AddComponent<EnvironmentSpriteRenderer>(); env.Init(root.transform, main, bg.GetComponent<Tilemap>()); env.RenderingEnabled = true; env.FlatPixelGreyboxEnabled = true;
                    Write(env, "_flatPixelGreybox", catalog); overlay = (Tilemap)Read(env, "_overlayTilemap");
                    Write(Renderer, "_envSpriteRenderer", env); Write(Renderer, "_flatPixelModeEnabled", true); Renderer.SetZone(z);
                    ZoneRenderHooks.CellDirtyCallback = Renderer.MarkCellDirty; ZoneRenderHooks.FullDirtyCallback = Renderer.MarkDirty;
                }
                catch { Dispose(); throw; }
            }
            public bool FullDirty => (bool)Read(Renderer, "_fullDirty");
            public Tile At(int x, int y) => overlay.GetTile<Tile>(new Vector3Int(x, Zone.Height - 1 - y, 0));
            public Tile GroundAt(int x, int y) => ground.GetTile<Tile>(new Vector3Int(x, Zone.Height - 1 - y, 0));
            public TileBase Mark => marks.GetTile(new Vector3Int(X, Zone.Height - 1 - Y, 0));
            public TileBase MarkAt(int x, int y) => marks.GetTile(new Vector3Int(x, Zone.Height - 1 - y, 0));
            public Color MarkTint => marks.GetColor(new Vector3Int(X, Zone.Height - 1 - Y, 0));
            public void Dirty(int x, int y) => Renderer.MarkCellDirty(x, y, "Sodden current material");
            public void Step()
            { try { typeof(ZoneRenderer).GetMethod("LateUpdate", I).Invoke(Renderer, null); } catch (TargetInvocationException e) { throw e.InnerException ?? e; } }
            public void Dispose()
            {
                try { saved?.Dispose(); if (root != null) Object.DestroyImmediate(root); }
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
