// Native owner, state and submission contracts for Beating materials.
// Root-frozen atlas:34 frames,544x24; 16x24 native static cells.
using System;
using System.Collections.Generic;
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
    public sealed class FlatPixelBeatingMaterialsTests
    {
        const string Home = "Overworld.16.13.0";
        const string Atlas = "Sprites/Live16x24/beating-materials";
        const string Vegetation = "Sprites/Live16x24/north-vegetation";
        const string Anchors = "Sprites/Live16x24/opening-anchors";
        const int X = 10, Y = 10;
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags S = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        DensityLootTestScope content;
        OverworldZoneManager manager;
        FlatPixelGreyboxCatalog world;
        Zone zone;
        Dictionary<string, LiquidDefinition> liquids, previousLiquids;
        bool previouslyInitialized;
        EntityFactory previousHarvestFactory;
        FlatPixelFallbackCatalog injectedSelector;
        IDisposable injectedAdapter;

        [SetUp] public void Setup()
        {
            liquids = (Dictionary<string, LiquidDefinition>)typeof(LiquidRegistry).GetField("_byId", S).GetValue(null);
            previousLiquids = new Dictionary<string, LiquidDefinition>(liquids);
            previouslyInitialized = LiquidRegistry.IsInitialized;
            LiquidRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/LiquidDefinitions").Select(t => t.text));
            Assert.AreEqual("water", LiquidRegistry.Get("brine")?.GroundReactionFamily);
            previousHarvestFactory = HarvestablePart.Factory;
            content = new DensityLootTestScope(); HarvestablePart.Factory = content.Factory;
            manager = OverworldZoneManager.CreateDetached(content.Factory, 65, true);
            Assert.AreEqual(BiomeType.Beating, manager.WorldMap.GetBiome(16, 13));
            Assert.IsNull(manager.WorldMap.GetPOI(16, 13));
            Assert.True(BeatingCompositionPlan.IsWildernessZone(Home));
            zone = Attach(Home); world = new FlatPixelGreyboxCatalog();
        }
        [TearDown] public void Cleanup()
        {
            try { world?.Dispose(); injectedAdapter?.Dispose(); injectedSelector?.Dispose(); content?.Dispose(); }
            finally
            {
                HarvestablePart.Factory = previousHarvestFactory;
                if (liquids != null && previousLiquids != null)
                {
                    liquids.Clear(); foreach (var pair in previousLiquids) liquids[pair.Key] = pair.Value;
                    typeof(LiquidRegistry).GetField("_initialized", S).SetValue(null, previouslyInitialized);
                }
            }
        }
        Zone Attach(string id) { var graph = new Zone(id); manager.SetActiveZone(graph); Reveal(graph); return graph; }
        static void Reveal(Zone graph)
        { for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++) graph.GetCell(x, y).Explored = graph.GetCell(x, y).IsVisible = true; }
        Entity Add(string blueprint, int x = X, int y = Y, bool seed = true)
        {
            var owner = content.Factory.CreateEntity(blueprint); Assert.NotNull(owner, blueprint);
            Assert.True(zone.AddEntity(owner, x, y));
            if (seed) owner.GetPart<TileStateSourcePart>()?.Seed(zone, x, y);
            return owner;
        }
        FlatPixelGreyboxCell View(int x = X, int y = Y)
        { Assert.True(world.TryResolvePresentation(zone, x, y, out var view)); return view; }
        static bool IsFloor(string blueprint) => blueprint == "Sand" || blueprint == "SaltCrust" || blueprint == "SandstoneFloor" || blueprint == "RoadStone";
        static string ResourceFor(string blueprint) => blueprint == "DryBrush" ? Vegetation : blueprint == "Signpost" ? Anchors : Atlas;
        static void NativeTile(Tile tile, string resource)
        {
            var texture = Resources.Load<Texture2D>(resource); Assert.NotNull(texture, resource);
            Assert.NotNull(tile); Assert.NotNull(tile.sprite); Assert.AreSame(texture, tile.sprite.texture);
            Assert.AreEqual(new Vector2(16, 24), tile.sprite.rect.size);
            Assert.AreEqual(new Vector2(8, 12), tile.sprite.pivot); Assert.AreEqual(16, tile.sprite.pixelsPerUnit);
            Assert.AreEqual(TileFlags.None, tile.flags); Assert.AreEqual(Tile.ColliderType.None, tile.colliderType);
        }
        void Native(Entity owner, int x = X, int y = Y)
        {
            var view = View(x, y); Assert.AreSame(owner, view.Owner); string resource = ResourceFor(owner.BlueprintName);
            var tile = IsFloor(owner.BlueprintName) ? view.GroundTile : view.ForegroundTile;
            if (IsFloor(owner.BlueprintName)) { Assert.True(view.LocalGround); Assert.AreEqual(Color.white, view.GroundColor); }
            else { Assert.True(view.NativeForeground); Assert.AreEqual(Color.white, view.ForegroundColor); }
            NativeTile(tile, resource); AssertFamilyFrame(owner, tile);
            Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone), "Regional materials must not admit a new opening scene.");
            Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, x, y, out var portrait, out var tint));
            Assert.AreSame(tile.sprite.texture, portrait.texture); Assert.AreEqual(tile.sprite.rect, portrait.rect);
            Assert.AreEqual(Color.white, tint);
        }
        static void AssertFamilyFrame(Entity owner, Tile tile)
        {
            int frame = (int)tile.sprite.rect.x / 16, first = -1, last = -1;
            switch (owner.BlueprintName)
            {
                case "Sand": first = 0; last = 3; break;
                case "SaltCrust": first = 4; last = 7; break;
                case "DuneCrest": first = 8; last = 11; break;
                case "SandstoneFloor": first = 12; last = 15; break;
                case "SandstoneWall": first = 16; last = 17; break;
                case "Rock": first = 18; last = 19; break;
                case "BrinePool": first = owner.GetPart<LiquidPoolPart>().Volume == 0 ? 24 : 20; last = first + 3; break;
                case "PaleSaltVein": first = last = 28; break;
                case "Bones": first = last = 29; break;
                case "Saltbriar": first = 30; last = 31; break;
                case "DryBrush": first = last = 3; break;
                case "RoadStone": first = 32; last = 33; break;
                case "Signpost": first = last = 8; break;
            }
            Assert.That(frame, Is.InRange(first, last), owner.BlueprintName + " cannot borrow another material's pixels.");
        }
        void Fallback(int x = X, int y = Y)
        {
            Assert.True(world.TryResolveFallback(zone, x, y, out var fallback)); var view = View(x, y);
            Assert.AreSame(fallback.Owner, view.Owner); Assert.False(view.NativeForeground);
            Assert.AreSame(fallback.ForegroundTile, view.ForegroundTile); Assert.False(view.RepresentedBrine);
        }
        static Type AdapterType()
        {
            var type = typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelBeatingMaterialsCatalog");
            Assert.NotNull(type, "The Beating material adapter has not been implemented."); return type;
        }
        bool Supported(Zone graph) => (bool)AdapterType().GetMethod("IsSupportedZone", S).Invoke(null, new object[] { graph });
        static void Write(object owner, string name, object value)
        { var field = owner.GetType().GetField(name, I); Assert.NotNull(field, name); field.SetValue(owner, value); }
        static object Read(object owner, string name)
        { var field = owner.GetType().GetField(name, I); Assert.NotNull(field, name); return field.GetValue(owner); }
        object Adapter()
        { var adapter = Read(world, "_beatingMaterialsArt"); if (adapter == null) { View(); adapter = Read(world, "_beatingMaterialsArt"); } Assert.NotNull(adapter); return adapter; }
        bool Ground(out Tile tile, out Entity floor)
        {
            object[] args = { zone, X, Y, null, null };
            bool result = (bool)Adapter().GetType().GetMethod("TryResolveGround", I).Invoke(Adapter(), args);
            tile = (Tile)args[3]; floor = (Entity)args[4]; return result;
        }
        bool Subject(Entity owner, int x = X, int y = Y)
        {
            object[] args = { zone, owner, x, y, null };
            return (bool)Adapter().GetType().GetMethod("TryGetSubjectSprite", I).Invoke(Adapter(), args);
        }
        void Inject(Texture2D packet, Texture2D vegetation = null, bool missingVegetation = false)
        {
            world.Dispose(); world = new FlatPixelGreyboxCatalog((Texture2D)null);
            injectedSelector = new FlatPixelFallbackCatalog();
            injectedAdapter = (IDisposable)Activator.CreateInstance(AdapterType(), new object[] { packet,
                missingVegetation ? null : vegetation ?? Resources.Load<Texture2D>(Vegetation),
                Resources.Load<Texture2D>(Anchors), injectedSelector });
            Write(world, "_beatingMaterialsArt", injectedAdapter);
        }
        static bool HasLedger(OverworldZoneManager owner)
        {
            var field = typeof(LairStacks).GetField("Ledgers", S); Assert.NotNull(field);
            var table = (System.Runtime.CompilerServices.ConditionalWeakTable<OverworldZoneManager, LairStackLedgerPart>)field.GetValue(null);
            return table.TryGetValue(owner, out _);
        }

        [TestCase("Sand")] [TestCase("SaltCrust")] [TestCase("DuneCrest")]
        [TestCase("SandstoneFloor")] [TestCase("SandstoneWall")] [TestCase("Rock")]
        [TestCase("BrinePool")] [TestCase("RoadStone")] [TestCase("DryBrush")]
        [TestCase("Signpost")] [TestCase("PaleSaltVein")] [TestCase("Bones")] [TestCase("Saltbriar")]
        public void ExactShippedOwnerUsesItsOwnMaterialButCustomIdentityDoesNot(string blueprint)
        {
            if (blueprint == "SaltCrust") Add("Sand");
            var owner = Add(blueprint); string before = zone.TileState.ToSaveString(); int version = zone.EntityVersion;
            bool solid = owner.GetPart<PhysicsPart>().Solid; Native(owner);
            Assert.AreEqual(solid, View().BlocksMovement); Assert.AreEqual(before, zone.TileState.ToSaveString()); Assert.AreEqual(version, zone.EntityVersion);
            if (blueprint == "DuneCrest") { Assert.IsNull(owner.GetPart<MaterialPart>()); Assert.IsNull(owner.GetPart<ThermalPart>()); Assert.IsNull(owner.GetPart<DestructiblePart>()); }
            if (blueprint == "Rock") Assert.IsNull(owner.GetPart<DestructiblePart>());
            if (blueprint == "Signpost") { Assert.True(solid); Assert.False(owner.HasTag("Solid")); }
            owner.GetPart<RenderPart>().VisualVariant = "unmapped material"; Fallback(); Assert.False(Subject(owner));
            owner.GetPart<RenderPart>().VisualVariant = ""; Native(owner);
        }

        [TestCase(Formation.WindBarrens, "Overworld.16.13.0", "Rock")]
        [TestCase(Formation.SaltPan, "Overworld.15.13.0", "SaltCrust")]
        [TestCase(Formation.BrineLens, "Overworld.17.13.0", "BrinePool")]
        [TestCase(Formation.RuinField, "Overworld.19.12.0", "SandstoneWall")]
        [TestCase(Formation.CaravanRoad, "Overworld.19.13.0", "RoadStone")]
        [TestCase(Formation.DuneBelt, "Overworld.19.14.0", "DuneCrest")]
        public void SixNaturalFormationGraphsSupplyRealOwnersWithoutPlanReplay(Formation formation, string id, string signature)
        {
            var point = WorldMap.FromZoneID(id);
            Assert.True(BeatingCompositionPlan.IsWildernessZone(id)); Assert.AreEqual(BiomeType.Beating, manager.WorldMap.GetBiome(point.x, point.y));
            Assert.IsNull(manager.WorldMap.GetPOI(point.x, point.y)); Assert.AreEqual(formation, FormationSelector.For(BiomeType.Beating, id));
            if (manager.CachedZones.ContainsKey(id)) manager = OverworldZoneManager.CreateDetached(content.Factory, 65, true);
            zone = manager.GetZone(id); manager.SetActiveZone(zone); Reveal(zone); Assert.True(Supported(zone));
            var owner = zone.GetReadOnlyEntities().FirstOrDefault(e => e.BlueprintName == signature
                && zone.GetEntityCell(e) is Cell c && world.TryResolveFallback(zone, c.X, c.Y, out var selected)
                && ReferenceEquals(selected.Owner, e) && !selected.Remembered);
            Assert.NotNull(owner, "The real formation needs a disclosed " + signature + " control.");
            var at = zone.GetEntityCell(owner); string state = zone.TileState.ToSaveString(); int version = zone.EntityVersion;
            Native(owner, at.X, at.Y); Assert.AreEqual(state, zone.TileState.ToSaveString()); Assert.AreEqual(version, zone.EntityVersion);
            owner.GetPart<RenderPart>().VisualID = "different authored owner"; Fallback(at.X, at.Y);
            owner.GetPart<RenderPart>().VisualID = ""; Native(owner, at.X, at.Y);
        }

        [TestCase("biome")] [TestCase("poi")] [TestCase("tile-shape")] [TestCase("poi-shape")]
        public void WarmAuthorityIsRevokedByTheCurrentMapAndRestoredWithoutCachingApproval(string fault)
        {
            var owner = Add("Rock"); Native(owner); var map = manager.WorldMap; var tiles = map.Tiles; var pois = map.POIs;
            var biome = tiles[16, 13]; var poi = pois[16, 13];
            try
            {
                for (int i = 0; i < 3; i++) Assert.True(Supported(zone));
                if (fault == "biome") tiles[16, 13] = BiomeType.Sodden;
                if (fault == "poi") map.SetPOI(16, 13, new PointOfInterest(POIType.MerchantCamp, "scope counter"));
                if (fault == "tile-shape") map.Tiles = new BiomeType[1, 1];
                if (fault == "poi-shape") map.POIs = new PointOfInterest[1, 1];
                Assert.False(Supported(zone)); Fallback();
            }
            finally { map.Tiles = tiles; map.POIs = pois; tiles[16, 13] = biome; pois[16, 13] = poi; }
            Native(owner);
        }
        [TestCase("Overworld.15.15.0")] [TestCase("Overworld.2.19.0")]
        [TestCase("Overworld.19.18.0")] [TestCase("Overworld.19.19.0")]
        [TestCase("Overworld.16.7.0")] [TestCase("Overworld.16.13.1")] [TestCase("Overworld.16.013.0")]
        public void MutableLabelsCannotAdmitSitesDepthOtherCountryOrAliases(string id)
        {
            Native(Add("Rock")); var point = WorldMap.FromZoneID(id); var map = manager.WorldMap;
            var biome = map.Tiles[point.x, point.y]; var poi = map.POIs[point.x, point.y];
            try
            {
                map.Tiles[point.x, point.y] = BiomeType.Beating; map.POIs[point.x, point.y] = null;
                zone = Attach(id); Add("Rock"); Assert.False(Supported(zone)); Fallback();
            }
            finally { map.Tiles[point.x, point.y] = biome; map.POIs[point.x, point.y] = poi; }
        }
        [TestCase(false)] [TestCase(true)]
        public void SavedModernOrLegacyLairAuthoritySurvivesErasedLabels(bool legacy)
        {
            Native(Add("Rock")); manager = OverworldZoneManager.CreateDetached(content.Factory, 65, true);
            manager.WorldMap.SetPOI(16, 13, new PointOfInterest(POIType.Lair, "saved claim", null, 1, "DesertProwler"));
            if (legacy) LairStacks.Restore(manager, null);
            zone = manager.GetZone(Home); manager.SetActiveZone(zone); Reveal(zone);
            var claim = LairStacks.Inspect(manager, Home); Assert.NotNull(claim); Assert.AreEqual(legacy, claim.Legacy);
            manager.WorldMap.SetPOI(16, 13, null); Assert.False(Supported(zone));
            Assert.True((bool)typeof(LairStacks).GetMethod("HasSavedColumn", S).Invoke(null, new object[] { manager, Home }));
            Assert.AreSame(zone, manager.CachedZones[Home]); Assert.AreEqual(BiomeType.Beating, manager.WorldMap.GetBiome(16, 13));
        }
        [Test] public void CurrentGraphIdentityAndReadOnlyLedgerAbsenceAreBothRequired()
        {
            Add("Sand"); var owner = Add("Rock"); Assert.False(HasLedger(manager));
            var members = zone.GetReadOnlyEntities().ToArray(); string state = zone.TileState.ToSaveString(); int count = manager.CachedZoneCount, version = zone.EntityVersion;
            for (int i = 0; i < 8; i++) { Native(owner); Assert.True(Supported(zone)); }
            Assert.False(HasLedger(manager)); Assert.AreEqual(count, manager.CachedZoneCount); Assert.AreEqual(version, zone.EntityVersion);
            CollectionAssert.AreEquivalent(members, zone.GetReadOnlyEntities()); Assert.AreEqual(state, zone.TileState.ToSaveString());
            var original = zone; manager.SetActiveZone(new Zone(Home)); Assert.False(Supported(original));
            Assert.False(world.TryResolvePresentation(original, X, Y, out _)); Assert.False(world.TryGetPresentationSubjectSprite(original, owner, X, Y, out _, out _));
            zone = new Zone(Home); Reveal(zone); Assert.IsNull(WorldLocationContext.For(zone)); Add("Rock"); Assert.False(Supported(zone)); Fallback();
            manager.SetActiveZone(original); zone = original; Native(owner);
        }

        [TestCase("hidden")] [TestCase("remembered")] [TestCase("unexplored")]
        public void NativeReaderAndWorldCannotDiscloseHiddenOrRememberedLiveMaterials(string fault)
        {
            var owner = Add("Rock"); Native(owner);
            if (fault == "hidden") owner.GetPart<RenderPart>().Visible = false;
            else { zone.GetCell(X, Y).IsVisible = false; zone.GetCell(X, Y).Explored = fault == "remembered"; }
            Assert.False(Subject(owner));
            if (fault == "unexplored") Assert.False(world.TryResolvePresentation(zone, X, Y, out _));
            else Assert.False(View().NativeForeground);
            owner.GetPart<RenderPart>().Visible = true; zone.GetCell(X, Y).Explored = zone.GetCell(X, Y).IsVisible = true; Native(owner);
        }
        [Test] public void CoveredRemovedOrStaleFootprintOwnerCannotKeepItsReaderImage()
        {
            var owner = Add("Rock"); owner.AddPart(new SpatialFootprintPart { CellsRaw = "0,0" }); Native(owner);
            var actor = Add("Player"); actor.GetPart<RenderPart>().RenderLayer = 100;
            Assert.AreSame(actor, View().Owner); Assert.False(Subject(owner));
            Assert.True(zone.MoveEntity(actor, X - 2, Y)); Native(owner);
            Assert.True(zone.TryChangeFootprint(owner, "0,0;1,0")); Assert.False(Subject(owner)); Assert.False(View().NativeForeground);
            Assert.False(View(X + 1, Y).NativeForeground);
            Assert.True(zone.TryChangeFootprint(owner, "0,0")); Native(owner);
            owner.GetPart<SpatialFootprintPart>().CellsRaw = "0,0;0,1"; Assert.False(Subject(owner));
            owner.GetPart<SpatialFootprintPart>().CellsRaw = "0,0"; Native(owner);
            Assert.True(zone.RemoveEntity(owner)); Assert.False(Subject(owner)); Assert.AreNotSame(owner, View().Owner);
        }
        [Test] public void UnderfootBrushBorrowsItsOwnFrameWithoutReplacingTheCoveringPlayer()
        {
            Add("SandstoneFloor"); var brush = Add("DryBrush"); Native(brush);
            var expected = View().ForegroundTile.sprite;
            var player = Add("Player"); player.GetPart<RenderPart>().RenderLayer = 100;
            Assert.AreSame(player, View().Owner);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, brush, X, Y, out var sprite, out var tint));
            Assert.AreSame(expected, sprite); Assert.AreEqual(Color.white, tint);
            var document = ExamineDocumentBuilder.BuildWorld(brush, zone, zone.GetCell(X, Y), player);
            Assert.True(ExamineDocumentBuilder.IsCurrentSubject(document));
            using (var paint = new Paint(world, zone))
            {
                paint.Step(); Assert.AreSame(View().ForegroundTile, paint.At(X, Y));
                Assert.True(paint.Renderer.TryGetFlatExaminePortrait(document, out var portrait));
                Assert.AreSame(expected.texture, portrait.Texture);
                Assert.AreEqual(new Rect(.75f, 0, .25f, 1), portrait.Uv);
                Assert.AreEqual(Color.white, portrait.Tint); Assert.False(portrait.IsModel);
                Assert.AreSame(player, View().Owner);
            }
        }
        [TestCase("hidden")] [TestCase("moved")] [TestCase("carried")]
        [TestCase("foreign-identity")] [TestCase("burning")] [TestCase("missing-vegetation")]
        public void UnderfootBrushRefusesStaleChangedOrUnavailableArt(string fault)
        {
            Add("SandstoneFloor"); var brush = Add("DryBrush"); Native(brush);
            var resource = Resources.Load<Texture2D>(Vegetation);
            var player = Add("Player"); player.GetPart<RenderPart>().RenderLayer = 100;
            var document = ExamineDocumentBuilder.BuildWorld(brush, zone, zone.GetCell(X, Y), player);
            Assert.True(ExamineDocumentBuilder.IsCurrentSubject(document));
            if (fault == "hidden") zone.GetCell(X, Y).IsVisible = false;
            else if (fault == "moved") Assert.True(zone.MoveEntity(brush, X + 1, Y));
            else if (fault == "carried") brush.GetPart<PhysicsPart>().InInventory = player;
            else if (fault == "foreign-identity") brush.GetPart<RenderPart>().VisualVariant = "unmapped brush";
            else if (fault == "burning") brush.GetPart<ThermalPart>().Temperature = brush.GetPart<ThermalPart>().FlameTemperature + 1;
            else Inject(Resources.Load<Texture2D>(Atlas), missingVegetation: true);
            Assert.False(Subject(brush), "The regional adapter must not supply the ordinary brush image");
            Assert.True(!world.TryGetPresentationSubjectSprite(zone, brush, X, Y, out var sprite, out _) || sprite.texture != resource);
            using (var paint = new Paint(world, zone))
            {
                Assert.True(paint.Renderer.TryGetFlatExaminePortrait(document, out var portrait));
                Assert.AreNotSame(resource, portrait.Texture);
                if (fault == "hidden" || fault == "moved" || fault == "carried")
                { Assert.False(ExamineDocumentBuilder.IsCurrentSubject(document)); Assert.IsNull(portrait.Texture); }
            }
        }
        [TestCase("Rock", "foreign-render")] [TestCase("DuneCrest", "foreign-physics")]
        [TestCase("SandstoneWall", "duplicate-material")] [TestCase("Rock", "duplicate-render")]
        [TestCase("Bones", "carried")] [TestCase("Signpost", "takeable")]
        [TestCase("PaleSaltVein", "foreign-harvest")] [TestCase("Saltbriar", "wrong-yield")]
        [TestCase("DuneCrest", "non-solid")] [TestCase("DryBrush", "zero-hp")]
        [TestCase("Rock", "wrong-blueprint")] [TestCase("DryBrush", "foreign-state-meaning")]
        public void MalformedRepurposedAndCustodiedOwnersRetainTruthfulFallback(string blueprint, string fault)
        {
            var owner = Add(blueprint); Native(owner); var foreign = new Entity();
            switch (fault)
            {
                case "foreign-render": owner.GetPart<RenderPart>().ParentEntity = foreign; break;
                case "foreign-physics": owner.GetPart<PhysicsPart>().ParentEntity = foreign; break;
                case "duplicate-material": owner.AddPart(new MaterialPart { MaterialID = "Stone" }); break;
                case "duplicate-render": owner.AddPart(new RenderPart { RenderString = owner.GetPart<RenderPart>().RenderString }); break;
                case "carried": owner.GetPart<PhysicsPart>().InInventory = foreign; break;
                case "takeable": owner.GetPart<PhysicsPart>().Takeable = true; break;
                case "foreign-harvest": owner.GetPart<HarvestablePart>().ParentEntity = foreign; break;
                case "wrong-yield": owner.GetPart<HarvestablePart>().YieldBlueprint = "Bone"; break;
                case "non-solid": owner.GetPart<PhysicsPart>().Solid = false; break;
                case "zero-hp": owner.GetPart<DestructiblePart>().HP = 0; break;
                case "wrong-blueprint": owner.BlueprintName = "custom rock"; break;
                case "foreign-state-meaning": owner.AddPart(new CropPart()); break;
            }
            Assert.False(Subject(owner));
            var view = View(); if (view.Owner == owner) Assert.False(view.NativeForeground);
        }
        [TestCase("DryBrush", "burning")] [TestCase("Saltbriar", "frozen")]
        [TestCase("BrinePool", "electrified")] [TestCase("DryBrush", "nan")]
        public void NeutralPixelsCannotReplaceCurrentOwnerTintOrInvalidThermalState(string blueprint, string state)
        {
            var owner = Add(blueprint); Native(owner);
            if (state == "burning") Assert.True(owner.ForceApplyEffect(new BurningEffect(), null, zone));
            if (state == "frozen") Assert.True(owner.ForceApplyEffect(new FrozenEffect(1), null, zone));
            // Entity.ForceApplyEffect still respects the material status door:
            // stone rejects electricity; conductive brine is the real positive.
            if (state == "electrified") Assert.True(owner.ForceApplyEffect(new ElectrifiedEffect(1), null, zone));
            if (state == "nan") owner.GetPart<ThermalPart>().Temperature = float.NaN;
            Fallback(); Assert.False(Subject(owner));
        }

        [TestCase(false)] [TestCase(true)]
        public void ActualSaltCrustOutranksSandInEitherInsertionOrderWithoutHidingAnActor(bool reversed)
        {
            Entity sand, crust;
            if (reversed) { crust = Add("SaltCrust"); sand = Add("Sand"); }
            else { sand = Add("Sand"); crust = Add("SaltCrust"); }
            Assert.True(Ground(out var tile, out var floor)); Assert.AreSame(crust, floor); NativeTile(tile, Atlas);
            Assert.True(View().LocalGround); Assert.AreSame(tile, View().GroundTile);
            Assert.IsNull(View().ForegroundTile, "An admitted lower Sand glyph must not be printed over its native crust.");
            var actor = Add("Player"); actor.GetPart<RenderPart>().RenderLayer = 100;
            Assert.AreSame(actor, View().Owner); Assert.True(View().NativeForeground); Assert.NotNull(View().ForegroundTile);
            Assert.AreSame(tile, View().GroundTile); Assert.True(Ground(out _, out floor)); Assert.AreSame(crust, floor);
            Assert.True(zone.RemoveEntity(crust)); Assert.True(Ground(out var sandTile, out floor)); Assert.AreSame(sand, floor);
            Assert.AreNotSame(tile, sandTile); Assert.AreSame(actor, View().Owner); Assert.NotNull(View().ForegroundTile);
        }
        [TestCase("SandstoneFloor")] [TestCase("RoadStone")]
        public void DistinctEngineeredBasesOrCrustOnThemAreAmbiguous(string blueprint)
        {
            var floor = Add(blueprint); Native(floor); var extra = Add("Sand"); Assert.False(Ground(out _, out _));
            Assert.True(zone.RemoveEntity(extra)); Native(floor); extra = Add("SaltCrust"); Assert.False(Ground(out _, out _));
            Assert.True(zone.RemoveEntity(extra)); Native(floor);
        }
        [TestCase("DuneCrest")] [TestCase("DryBrush")] [TestCase("Bones")]
        public void ExactNeutralDryOverlayRetainsOnlyItsActualSandUnderlay(string blueprint)
        {
            var sand = Add("Sand"); var owner = Add(blueprint); Native(owner);
            Assert.True(Ground(out var tile, out var floor)); Assert.AreSame(sand, floor); Assert.AreSame(tile, View().GroundTile);
            Assert.True(zone.RemoveEntity(sand)); Assert.False(Ground(out _, out _)); Assert.False(View().LocalGround); Native(owner);
            sand = Add("Sand"); Assert.True(Ground(out _, out floor)); Assert.AreSame(sand, floor);
            owner.GetPart<RenderPart>().VisualVariant = "unmapped dry form"; Assert.False(Ground(out _, out _));
        }
        [Test] public void HiddenForeignAndCustodiedFloorOrCultivationCannotEraseVisibleSand()
        {
            var sand = Add("Sand"); Native(sand); var baseline = View().GroundTile;
            var crust = Add("SaltCrust"); var render = crust.GetPart<RenderPart>(); render.Visible = false;
            Assert.True(Ground(out var tile, out var floor)); Assert.AreSame(sand, floor); Assert.AreSame(baseline, tile);
            render.Visible = true; Assert.True(Ground(out _, out floor)); Assert.AreSame(crust, floor);
            render.ParentEntity = new Entity(); Assert.True(Ground(out _, out floor)); Assert.AreSame(sand, floor);
            render.ParentEntity = crust; crust.GetPart<PhysicsPart>().InInventory = new Entity();
            Assert.True(Ground(out _, out floor)); Assert.AreSame(sand, floor);
            crust.GetPart<PhysicsPart>().InInventory = null; Assert.True(Ground(out _, out floor)); Assert.AreSame(crust, floor);
            Assert.True(zone.RemoveEntity(crust));
            var brush = Add("DryBrush"); var soil = new CultivatedSoilPart(); brush.AddPart(soil); brush.SetTag("Plantable");
            brush.GetPart<RenderPart>().Visible = false; Assert.True(CultivatedSoilPart.IsCultivated(zone, zone.GetCell(X, Y)));
            Assert.True(Ground(out tile, out floor)); Assert.AreSame(sand, floor); Assert.AreSame(baseline, tile);
            brush.GetPart<RenderPart>().Visible = true; Assert.False(Ground(out _, out _));
            brush.GetPart<RenderPart>().ParentEntity = new Entity(); Assert.True(Ground(out _, out floor)); Assert.AreSame(sand, floor);
            brush.GetPart<RenderPart>().ParentEntity = brush; Assert.False(Ground(out _, out _));
        }
        [TestCase("coating")] [TestCase("residue")] [TestCase("heat")] [TestCase("unknown-terrain")]
        public void DryNativeFloorCannotConcealIndependentStateOrUnknownGround(string fault)
        {
            var sand = Add("Sand"); Native(sand); var actor = Add("Player"); actor.GetPart<RenderPart>().RenderLayer = 100;
            Assert.True(Ground(out _, out var floor)); Assert.AreSame(sand, floor);
            if (fault == "coating") zone.TileState.WriteCoating(X, Y, "water", 3);
            if (fault == "residue") zone.TileState.WriteResidue(X, Y, "embers", 3);
            if (fault == "heat") zone.TileState.AddHeat(X, Y, 1);
            if (fault == "unknown-terrain") { var foreign = Add("Grass"); foreign.GetPart<RenderPart>().RenderLayer = 0; }
            Assert.False(Ground(out _, out _)); Assert.False(View().LocalGround); Assert.AreSame(actor, View().Owner);
        }

        [TestCase("PaleSaltVein", "PaleSalt")] [TestCase("Bones", "Bone")] [TestCase("Saltbriar", "SaltbriarSprig")]
        public void RealHarvestRemovesNativeActivityAndRetainedSpentReferenceCannotReappear(string blueprint, string product)
        {
            Add("Sand"); var owner = Add(blueprint); var harvest = owner.GetPart<HarvestablePart>();
            Assert.AreEqual(product, harvest.YieldBlueprint); Assert.AreEqual(1, harvest.YieldMin); Assert.AreEqual(2, harvest.YieldMax); Native(owner);
            harvest.Harvested = true; Fallback(); Assert.False(Subject(owner)); harvest.Harvested = false; Native(owner);
            var actor = Add("Player", X - 1, Y); var action = GameEvent.New("InventoryAction");
            try
            {
                action.SetParameter("Actor", actor); action.SetParameter("Zone", zone); action.SetParameter("Command", "Harvest");
                action.SetParameter("Random", new System.Random(11)); owner.FireEvent(action);
                Assert.True(action.Handled, "Use the actual finite harvest transaction, not visual removal.");
            }
            finally { action.Release(); }
            Assert.True(harvest.Harvested); Assert.IsNull(zone.GetEntityCell(owner)); Assert.False(Subject(owner)); Assert.AreNotSame(owner, View().Owner);
            Assert.True(DensityLootTestScope.Gear(actor).Any(e => e.BlueprintName == product)
                || zone.GetCell(X, Y).Objects.Any(e => e.BlueprintName == product), "The real activity must produce its declared material.");
        }

        [Test] public void RealBrineProjectionAndDepletedStockKeepSeparateWetMaterialStates()
        {
            var owner = Add("BrinePool", seed: false); var pool = owner.GetPart<LiquidPoolPart>();
            Assert.AreEqual("water", owner.GetPart<TileStateSourcePart>().Coating);
            Assert.AreEqual("brine", owner.GetPart<TileStateSourcePart>().EffectiveCoating);
            Assert.True(zone.TileState.HasCoating(X, Y, "brine")); Assert.False(zone.TileState.HasCoating(X, Y, "water"));
            Native(owner); var full = View().ForegroundTile; Assert.True(View().RepresentedBrine); Assert.False(View().RepresentedMire);
            pool.Volume = 0; Native(owner); var film = View().ForegroundTile; Assert.AreNotSame(full, film); Assert.True(View().RepresentedBrine);
            Assert.True(zone.TileState.HasCoating(X, Y, "brine")); Assert.False(View().LocalGround);
            zone.TileState.Clear(X, Y); Fallback(); Assert.False(Subject(owner));
            owner.GetPart<TileStateSourcePart>().Seed(zone, X, Y); Native(owner); Assert.AreSame(film, View().ForegroundTile);
            Assert.AreEqual(4, zone.TileState.CoatingTurns(X, Y, "brine"));
            pool.Volume = 90; Native(owner); Assert.AreSame(full, View().ForegroundTile);
            Assert.True(zone.RemoveEntity(owner)); Assert.False(zone.TileState.HasCoating(X, Y, "brine")); Assert.False(View().RepresentedBrine);
        }
        [TestCase("mixture")] [TestCase("plain-water")] [TestCase("ice")] [TestCase("expired")]
        [TestCase("wrong-liquid")] [TestCase("foreign-source")] [TestCase("duplicate-pool")] [TestCase("negative-stock")]
        [TestCase("frozen")] [TestCase("wet-effect")]
        public void BrineCannotAcquireNeutralNativeBodyForUnsupportedTruth(string fault)
        {
            var owner = Add("BrinePool"); Native(owner);
            if (fault == "mixture") zone.TileState.WriteCoating(X, Y, "oil", 3);
            if (fault == "plain-water" || fault == "ice") { zone.TileState.Clear(X, Y); zone.TileState.WriteCoating(X, Y, fault == "ice" ? "ice" : "water", 3); }
            if (fault == "expired") zone.TileState.Get(X, Y).Coatings[0].Turns = 0;
            if (fault == "wrong-liquid") owner.GetPart<LiquidPoolPart>().LiquidId = "water";
            if (fault == "foreign-source") owner.GetPart<TileStateSourcePart>().ParentEntity = new Entity();
            if (fault == "duplicate-pool") owner.AddPart(new LiquidPoolPart { LiquidId = "brine", Volume = 90 });
            if (fault == "negative-stock") owner.GetPart<LiquidPoolPart>().Volume = -1;
            if (fault == "frozen") owner.GetPart<ThermalPart>().Temperature = -8;
            if (fault == "wet-effect") Assert.True(owner.ForceApplyEffect(new WetEffect(), null, zone));
            Fallback(); Assert.False(Subject(owner)); Assert.False(View().LocalGround);
        }
        [Test] public void HiddenPoolCannotSuppressItsIndependentLiveCoatingOrEraseUnrelatedDryGround()
        {
            var sand = Add("Sand"); Native(sand); var tile = View().GroundTile;
            var pool = Add("BrinePool", seed: false); pool.GetPart<RenderPart>().Visible = false;
            Assert.True(zone.TileState.HasCoating(X, Y, "brine")); Assert.False(View().LocalGround); Assert.False(View().RepresentedBrine);
            zone.TileState.Clear(X, Y); Assert.True(View().LocalGround); Assert.AreSame(tile, View().GroundTile); Assert.AreSame(sand, View().Owner);
            pool.GetPart<TileStateSourcePart>().Seed(zone, X, Y); Assert.False(View().LocalGround); Assert.False(View().RepresentedBrine);
        }

        [TestCase(false)] [TestCase(true)]
        public void MissingOrMalformedMainAtlasCannotReloadResourcesOrDisableValidBorrowedGroups(bool malformed)
        {
            Texture2D bad = malformed ? new Texture2D(17, 24) : null;
            try
            {
                var owner = Add("Rock"); Native(owner); Inject(bad); Fallback(); Assert.False(Subject(owner));
                Assert.True(zone.RemoveEntity(owner)); var brush = Add("DryBrush"); Native(brush);
                Assert.True(zone.RemoveEntity(brush)); var sign = Add("Signpost"); Native(sign);
                Assert.True(zone.RemoveEntity(sign)); var packedRoad = Add("RoadStone");
                Fallback(); Assert.False(View().LocalGround); Assert.False(Subject(packedRoad));
            }
            finally { world.Dispose(); if (bad != null) Object.DestroyImmediate(bad); }
        }
        [Test] public void MissingBorrowedGroupDoesNotDisableTheMainAtlasAndDisposalOwnsOnlyGeneratedFrames()
        {
            var source = Resources.Load<Texture2D>(Atlas); Assert.NotNull(source); Inject(source, missingVegetation: true);
            var brush = Add("DryBrush"); Fallback(); Assert.False(Subject(brush)); Assert.True(zone.RemoveEntity(brush));
            var rock = Add("Rock"); Native(rock); var tile = View().ForegroundTile; var sprite = tile.sprite;
            world.Dispose(); world.Dispose(); Assert.True(tile == null); Assert.True(sprite == null); Assert.True(source != null);
            Assert.True(injectedSelector.TryResolve(zone, X, Y, out var fallback)); Assert.AreSame(rock, fallback.Owner);
        }

        [Test] public void FrozenAtlasUsesNativePointSampledCellsAndStableCachedMaterials()
        {
            var atlas = Resources.Load<Texture2D>(Atlas); Assert.NotNull(atlas);
            Assert.AreEqual(544, atlas.width); Assert.AreEqual(24, atlas.height); Assert.AreEqual(FilterMode.Point, atlas.filterMode);
            Assert.AreEqual(1, atlas.mipmapCount);
            var sand = Add("Sand"); Native(sand); var tile = View().GroundTile;
            for (int i = 0; i < 16; i++) { Native(sand); Assert.AreSame(tile, View().GroundTile); }
            Assert.AreEqual(34, AdapterType().GetField("FrameCount", S).GetValue(null));
        }
        [TestCase(90, "none", '\0')] [TestCase(0, "none", '\0')]
        [TestCase(90, "charge", '*')] [TestCase(0, "heat", '^')]
        [TestCase(90, "embers", '"')] [TestCase(0, "veil-mist", '\'')]
        public void SubmittedBrineSuppressesOnlyItsRepresentedWaveAndKeepsIndependentMarks(int stock, string hazard, char expected)
        {
            var owner = Add("BrinePool"); owner.GetPart<LiquidPoolPart>().Volume = stock;
            if (hazard == "charge") zone.TileState.AddCharge(X, Y, 1);
            if (hazard == "heat") zone.TileState.AddHeat(X, Y, 1);
            if (hazard == "embers") zone.TileState.WriteResidue(X, Y, "embers", 3);
            if (hazard == "veil-mist") zone.TileState.WriteCloud(X, Y, "veil-mist", 3);
            string state = zone.TileState.ToSaveString();
            using (var paint = new Paint(world, zone))
            {
                paint.Step(); NativeTile(paint.At(X, Y), Atlas); AssertFamilyFrame(owner, paint.At(X, Y)); Assert.True(View().RepresentedBrine);
                if (expected == '\0') Assert.IsNull(paint.Mark); else Assert.AreSame(CP437TilesetGenerator.GetTile(expected), paint.Mark);
                Assert.AreEqual(state, zone.TileState.ToSaveString()); Assert.True(zone.TileState.HasCoating(X, Y, "brine"));
                long resolved = EnvironmentSpriteRenderer.Perf.CellsResolved; paint.Step(); Assert.AreEqual(resolved, EnvironmentSpriteRenderer.Perf.CellsResolved);
                zone.GetCell(X, Y).IsVisible = false; paint.Dirty(X, Y); paint.Step(); Assert.IsNull(paint.Mark);
            }
        }
        [Test] public void SubmittedCoverAndMissingBodyCannotSuppressTheBrineWave()
        {
            var owner = Add("BrinePool");
            using (var paint = new Paint(world, zone))
            {
                paint.Step(); NativeTile(paint.At(X, Y), Atlas); Assert.IsNull(paint.Mark);
                var actor = Add("Player"); actor.GetPart<RenderPart>().RenderLayer = 100; paint.Dirty(X, Y); paint.Step();
                Assert.AreSame(actor, View().Owner); Assert.False(View().RepresentedBrine); Assert.AreSame(CP437TilesetGenerator.GetTile('~'), paint.Mark);
                Assert.True(zone.RemoveEntity(actor)); paint.Dirty(X, Y); paint.Step(); NativeTile(paint.At(X, Y), Atlas); Assert.IsNull(paint.Mark);
                owner.GetPart<RenderPart>().VisualVariant = "unmapped brine"; paint.Dirty(X, Y); paint.Step();
                Assert.False(View().RepresentedBrine); Assert.AreSame(CP437TilesetGenerator.GetTile('~'), paint.Mark);
            }
        }
        [Test] public void SubmittedMissingMainAtlasKeepsTheActualBrineWave()
        {
            var owner = Add("BrinePool"); Native(owner); Assert.True(View().RepresentedBrine);
            string state = zone.TileState.ToSaveString();
            Inject(null);
            using (var paint = new Paint(world, zone))
            {
                paint.Step(); Fallback(); Assert.False(View().LocalGround);
                Assert.AreSame(View().ForegroundTile, paint.At(X, Y));
                Assert.AreSame(CP437TilesetGenerator.GetTile('~'), paint.Mark,
                    "No submitted native body means the actual coating keeps its wave mark.");
                Assert.True(zone.TileState.HasCoating(X, Y, "brine")); Assert.AreEqual(state, zone.TileState.ToSaveString());
                zone.GetCell(X, Y).IsVisible = false; paint.Dirty(X, Y); paint.Step();
                Assert.IsNull(paint.Mark, "A missing atlas must not disclose live brine through fog.");
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void SubmittedSaltCrustRepaintsActualLowerGroundWithoutErasingCoveringActor(bool reversed)
        {
            Entity sand, crust;
            if (reversed) { crust = Add("SaltCrust"); sand = Add("Sand"); }
            else { sand = Add("Sand"); crust = Add("SaltCrust"); }
            Assert.True(Ground(out var crustTile, out var floor)); Assert.AreSame(crust, floor);
            using (var paint = new Paint(world, zone))
            {
                paint.Step(); NativeTile(paint.GroundAt(X, Y), Atlas); AssertFamilyFrame(crust, paint.GroundAt(X, Y));
                Assert.AreSame(crustTile, paint.GroundAt(X, Y)); Assert.IsNull(paint.At(X, Y));
                var actor = Add("Player"); actor.GetPart<RenderPart>().RenderLayer = 100; paint.Dirty(X, Y); paint.Step();
                Assert.AreSame(actor, View().Owner); Assert.True(View().NativeForeground); Assert.NotNull(paint.At(X, Y));
                Assert.AreSame(View().ForegroundTile, paint.At(X, Y)); Assert.AreSame(crustTile, paint.GroundAt(X, Y));
                Assert.True(zone.RemoveEntity(crust)); paint.Dirty(X, Y); paint.Step();
                Assert.True(Ground(out var sandTile, out floor)); Assert.AreSame(sand, floor);
                Assert.AreNotSame(crustTile, sandTile); Assert.AreSame(sandTile, paint.GroundAt(X, Y));
                AssertFamilyFrame(sand, paint.GroundAt(X, Y)); Assert.AreSame(actor, View().Owner);
                Assert.AreSame(View().ForegroundTile, paint.At(X, Y)); Assert.NotNull(paint.At(X, Y));
                Assert.True(zone.RemoveEntity(actor)); paint.Dirty(X, Y); paint.Step();
                Native(sand); Assert.AreSame(sandTile, paint.GroundAt(X, Y)); Assert.IsNull(paint.At(X, Y));
            }
        }
        [Test] public void SubmittedWallDestructionRemovesOnlyTheActualWallAndRetainsNeighborMaterial()
        {
            Add("SandstoneFloor"); var wall = Add("SandstoneWall"); var neighbor = Add("SandstoneWall", X + 1, Y);
            using (var paint = new Paint(world, zone))
            {
                paint.Step(); NativeTile(paint.At(X, Y), Atlas); var neighborTile = paint.At(X + 1, Y); NativeTile(neighborTile, Atlas);
                long full = EnvironmentSpriteRenderer.Perf.FullPasses;
                var oldFactory = DestructionSystem.EntityFactoryRef;
                try { DestructionSystem.EntityFactoryRef = content.Factory; Assert.AreEqual(DestroyVerdict.Destroyed, DestructionSystem.Damage(wall, 999, null, zone)); }
                finally { DestructionSystem.EntityFactoryRef = oldFactory; }
                Assert.IsNull(zone.GetEntityCell(wall)); Assert.False(Subject(wall)); Assert.False(paint.FullDirty); paint.Step();
                Assert.AreNotSame(wall, View().Owner); Assert.AreSame(neighborTile, paint.At(X + 1, Y)); Native(neighbor, X + 1, Y);
                Assert.AreEqual(full, EnvironmentSpriteRenderer.Perf.FullPasses);
            }
        }

        // Existing submitted ZoneRenderer fixture; no alternate drawing implementation.
        sealed class Paint : IDisposable
        {
            readonly HotbarSaveFixture saved;
            readonly GameObject root;
            readonly Tilemap overlay, marks, ground;
            public ZoneRenderer Renderer { get; }
            readonly Action<int, int, string> oldCell = ZoneRenderHooks.CellDirtyCallback;
            readonly Action<string> oldFull = ZoneRenderHooks.FullDirtyCallback;
            readonly FieldInfo bound = typeof(ZoneTileStateSystem).GetField("_boundRenderZone", S);
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
                    saved = new HotbarSaveFixture(); root = new GameObject("Beating submitted grid", typeof(Grid)); saved.Root.transform.SetParent(root.transform, false);
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
            public void Dirty(int x, int y) => Renderer.MarkCellDirty(x, y, "Beating current material");
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
