// Contract-first Stump material coverage. Reflection keeps the absent adapter
// compilable so the first native run records specific missing-feature RED.
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
    public sealed class FlatPixelStumpMaterialsTests
    {
        const string Home = "Overworld.2.5.0", Atlas = "Sprites/Live16x24/stump-materials";
        const string Floor = "TepuiStone", Pool = "SprayPool", Tank = "TankBrocchinia", Vein = "TepuiboneVein";
        const string Vegetation = "Sprites/Live16x24/north-vegetation", Opening = "Sprites/Live16x24/opening-base";
        const int X = 10, Y = 10;
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags S = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        DensityLootTestScope content;
        OverworldZoneManager manager;
        Zone zone;
        FlatPixelGreyboxCatalog world;
        FlatPixelFallbackCatalog injectedSelector;
        IDisposable injectedAdapter;
        Dictionary<string, LiquidDefinition> liquids, previousLiquids;
        bool previouslyInitialized;
        EntityFactory previousHarvestFactory;
        Action<WorldAudioUtilityReceipt> previousUtilityAudio;
        Action<WorldAudioWorkReceipt> previousWorkAudio;
        Action<WorldAudioCue, Entity, Zone, int, int> previousAudioHook;
        Action<string> previousMessage;

        [SetUp] public void Setup()
        {
            // Detached domain actions must not feed a borrowed live sound/log sink.
            previousUtilityAudio = WorldAudioUtility.Callback; previousWorkAudio = WorldAudioWork.Callback;
            previousAudioHook = WorldAudioHooks.Callback; previousMessage = MessageLog.OnMessage;
            WorldAudioUtility.Callback = null; WorldAudioWork.Callback = null; WorldAudioHooks.Callback = null; MessageLog.OnMessage = null;
            liquids = (Dictionary<string, LiquidDefinition>)typeof(LiquidRegistry).GetField("_byId", S).GetValue(null);
            previousLiquids = new Dictionary<string, LiquidDefinition>(liquids); previouslyInitialized = LiquidRegistry.IsInitialized;
            LiquidRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/LiquidDefinitions").Select(t => t.text));
            Assert.NotNull(LiquidRegistry.Get("water"));
            previousHarvestFactory = HarvestablePart.Factory;
            content = new DensityLootTestScope(); HarvestablePart.Factory = content.Factory;
            manager = OverworldZoneManager.CreateDetached(content.Factory, 65, true);
            Assert.AreEqual(BiomeType.Stump, manager.WorldMap.GetBiome(2, 5)); Assert.IsNull(manager.WorldMap.GetPOI(2, 5));
            Assert.True(StumpCompositionPlan.IsWildernessZone(Home)); zone = Attach(Home); world = new FlatPixelGreyboxCatalog();
        }
        [TearDown] public void Cleanup()
        {
            try { world?.Dispose(); injectedAdapter?.Dispose(); injectedSelector?.Dispose(); content?.Dispose(); }
            finally
            {
                HarvestablePart.Factory = previousHarvestFactory;
                WorldAudioUtility.Callback = previousUtilityAudio; WorldAudioWork.Callback = previousWorkAudio;
                WorldAudioHooks.Callback = previousAudioHook; MessageLog.OnMessage = previousMessage;
                if (liquids != null && previousLiquids != null)
                { liquids.Clear(); foreach (var pair in previousLiquids) liquids[pair.Key] = pair.Value; typeof(LiquidRegistry).GetField("_initialized", S).SetValue(null, previouslyInitialized); }
            }
        }
        Zone Attach(string id) { var graph = new Zone(id); manager.SetActiveZone(graph); Reveal(graph); return graph; }
        static void Reveal(Zone graph)
        { for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++) graph.GetCell(x, y).Explored = graph.GetCell(x, y).IsVisible = true; }
        Entity Add(string name, int x = X, int y = Y)
        { var owner = content.Factory.CreateEntity(name); Assert.NotNull(owner, name); Assert.True(zone.AddEntity(owner, x, y)); return owner; }
        FlatPixelGreyboxCell View(int x = X, int y = Y)
        { Assert.True(world.TryResolvePresentation(zone, x, y, out var value)); return value; }
        static Type AdapterType()
        { var type = typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelStumpMaterialsCatalog"); Assert.NotNull(type, "Stump current-owner material catalog is not implemented."); return type; }
        static void Write(object target, string name, object value)
        { var field = target.GetType().GetField(name, I); Assert.NotNull(field, name); field.SetValue(target, value); }
        static object Read(object target, string name)
        { var field = target.GetType().GetField(name, I); Assert.NotNull(field, name); return field.GetValue(target); }
        static bool Call(object target, string method, object[] args)
        { var found = target.GetType().GetMethod(method, I); Assert.NotNull(found, method); return (bool)found.Invoke(target, args); }
        object Adapter()
        { AdapterType(); var value = Read(world, "_stumpMaterialsArt"); if (value == null) { View(); value = Read(world, "_stumpMaterialsArt"); } Assert.NotNull(value); return value; }
        bool Supported(Zone graph)
        { var method = AdapterType().GetMethod("IsSupportedZone", S); Assert.NotNull(method); return (bool)method.Invoke(null, new object[] { graph }); }
        bool Ground(out Tile tile, out Entity floor, int x = X, int y = Y)
        { object[] args = { zone, x, y, null, null }; bool ok = Call(Adapter(), "TryResolveGround", args); tile = (Tile)args[3]; floor = (Entity)args[4]; return ok; }
        bool Water(out Tile tile, out Entity source, int x = X, int y = Y)
        { object[] args = { zone, x, y, null, null }; bool ok = Call(Adapter(), "TryResolveWaterGround", args); tile = (Tile)args[3]; source = (Entity)args[4]; return ok; }
        bool Subject(Entity owner, int x = X, int y = Y)
        { object[] args = { zone, owner, x, y, null }; return Call(Adapter(), "TryGetSubjectSprite", args); }
        static string ResourceFor(string name) => name == "Tree" ? Vegetation : name == "Bush" ? Opening : Atlas;
        static void NativeTile(Tile tile, string resource = Atlas)
        {
            var source = Resources.Load<Texture2D>(resource); Assert.NotNull(source, resource); Assert.NotNull(tile); Assert.NotNull(tile.sprite);
            Assert.AreSame(source, tile.sprite.texture); Assert.AreEqual(new Vector2(16, 24), tile.sprite.rect.size);
            Assert.AreEqual(new Vector2(8, 12), tile.sprite.pivot); Assert.AreEqual(16, tile.sprite.pixelsPerUnit);
            Assert.AreEqual(TileFlags.None, tile.flags); Assert.AreEqual(Tile.ColliderType.None, tile.colliderType);
        }
        static void Family(Entity owner, Tile tile)
        {
            int first = -1, last = -1;
            switch (owner.BlueprintName)
            {
                case Floor: first = 0; last = 3; break;
                case "TepuiWall": first = 4; last = 5; break;
                case "GrainRidge": first = 6; last = 9; break;
                case "DescentLedge": first = 10; last = 11; break;
                case Pool: first = owner.GetPart<LiquidPoolPart>().Volume > 0 ? 12 : 16; last = first + 3; break;
                case "StoneDome": first = 20; last = 21; break;
                case Tank: first = 22; last = 23; break;
                case Vein: first = 24; last = 25; break;
                case "Tree": first = last = 0; break;
                case "Bush": first = last = 5; break;
            }
            int frame = (int)tile.sprite.rect.x / 16;
            int boundary = BoundaryStart(owner.BlueprintName);
            if (boundary >= 0 && frame >= 26)
                Assert.That(frame, Is.InRange(boundary, boundary + 14), owner.BlueprintName);
            else Assert.That(frame, Is.InRange(first, last), owner.BlueprintName);
        }
        void Native(Entity owner, int x = X, int y = Y)
        {
            Assert.True(Supported(zone)); var value = View(x, y); Assert.AreSame(owner, value.Owner);
            bool ground = owner.BlueprintName == Floor || owner.BlueprintName == Pool;
            var tile = ground ? value.GroundTile : value.ForegroundTile; NativeTile(tile, ResourceFor(owner.BlueprintName)); Family(owner, tile);
            if (ground)
            {
                Assert.True(value.LocalGround); Assert.AreEqual(Color.white, value.GroundColor); Assert.IsNull(value.ForegroundTile);
                Assert.AreEqual(owner.BlueprintName == Pool ? FlatPixelGreyboxGround.Water : FlatPixelGreyboxGround.Ground, value.GroundKind);
            }
            else { Assert.True(value.NativeForeground); Assert.AreEqual(Color.white, value.ForegroundColor); }
            Assert.AreEqual(owner.GetPart<PhysicsPart>().Solid, value.BlocksMovement);
            Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone), "Regional art cannot broaden opening admission.");
            Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, x, y, out var sprite, out var tint));
            Assert.AreSame(tile.sprite.texture, sprite.texture); Assert.AreEqual(tile.sprite.rect, sprite.rect); Assert.AreEqual(Color.white, tint);
        }
        void Fallback(int x = X, int y = Y)
        {
            Assert.True(world.TryResolveFallback(zone, x, y, out var expected)); var value = View(x, y);
            Assert.AreSame(expected.Owner, value.Owner); Assert.False(value.NativeForeground); Assert.AreSame(expected.ForegroundTile, value.ForegroundTile);
        }
        void Inject(Texture2D atlas, string missing = null)
        {
            world.Dispose(); injectedAdapter?.Dispose(); injectedSelector?.Dispose(); world = new FlatPixelGreyboxCatalog((Texture2D)null);
            injectedSelector = new FlatPixelFallbackCatalog();
            var ctor = AdapterType().GetConstructor(new[] { typeof(Texture2D), typeof(Texture2D), typeof(Texture2D), typeof(FlatPixelFallbackCatalog) });
            Assert.NotNull(ctor, "Main, vegetation and opening are independently optional borrowed texture groups.");
            injectedAdapter = (IDisposable)ctor.Invoke(new object[] { atlas, missing == "tree" ? null : Resources.Load<Texture2D>(Vegetation), missing == "bush" ? null : Resources.Load<Texture2D>(Opening), injectedSelector });
            Write(world, "_stumpMaterialsArt", injectedAdapter);
        }
        bool Act(Entity owner, Entity actor, string command)
        {
            var action = GameEvent.New("InventoryAction");
            try { action.SetParameter("Actor", actor); action.SetParameter("Zone", zone); action.SetParameter("Command", command); action.SetParameter("Random", new System.Random(1)); owner.FireEvent(action); return action.Handled; }
            finally { action.Release(); }
        }
        static bool HasLedger(OverworldZoneManager owner)
        { return ((System.Runtime.CompilerServices.ConditionalWeakTable<OverworldZoneManager, LairStackLedgerPart>)typeof(LairStacks).GetField("Ledgers", S).GetValue(null)).TryGetValue(owner, out _); }

        [TestCase(Floor)] [TestCase("TepuiWall")] [TestCase("GrainRidge")] [TestCase("DescentLedge")]
        [TestCase(Pool)] [TestCase("StoneDome")] [TestCase(Tank)] [TestCase(Vein)] [TestCase("Tree")] [TestCase("Bush")]
        public void ExactFactoryOwnersUseTheirNativeFamilyAndReaderButCustomIdentityDoesNot(string name)
        {
            var owner = Add(name); int version = zone.EntityVersion; string state = zone.TileState.ToSaveString(); Native(owner);
            Assert.AreEqual(version, zone.EntityVersion); Assert.AreEqual(state, zone.TileState.ToSaveString());
            owner.GetPart<RenderPart>().VisualVariant = "different current identity"; Fallback(); Assert.False(Subject(owner));
            if (name == Floor || name == Pool) Assert.False(View().LocalGround);
            owner.GetPart<RenderPart>().VisualVariant = ""; Native(owner);
        }
        [Test] public void OrdinaryAuthoredFloorVariantsAreAcceptedButUnfamiliarVariantsAreNot()
        {
            var owner = Add(Floor); var render = owner.GetPart<RenderPart>(); Assert.AreEqual("...,.'`", render.GlyphVariants); Native(owner);
            render.GlyphVariants = "xyz"; Assert.False(Ground(out _, out _)); Fallback(); Assert.False(Subject(owner));
            render.GlyphVariants = "...,.'`"; Native(owner);
        }
        [Test] public void OnlyEighteenCanonicalCurrentOrdinaryAddressesAreSupported()
        {
            int count = 0;
            for (int y = 0; y < WorldMap.Height; y++) for (int x = 0; x < WorldMap.Width; x++)
            {
                if (manager.WorldMap.GetBiome(x, y) != BiomeType.Stump) continue;
                string id = WorldMap.ToZoneID(x, y); zone = Attach(id);
                bool expected = StumpCompositionPlan.IsWildernessZone(id) && manager.WorldMap.GetPOI(x, y) == null;
                Assert.AreEqual(expected, Supported(zone), id); if (expected) count++;
            }
            Assert.AreEqual(18, count); Assert.False(Supported(null));
        }
        [TestCase("Overworld.2.5.0", Formation.CascadeGorge, Pool)]
        [TestCase("Overworld.2.2.0", Formation.Grainfield, "GrainRidge")]
        [TestCase("Overworld.4.5.0", Formation.ButtressRidge, Vein)]
        [TestCase("Overworld.3.4.0", Formation.SummitScrub, "StoneDome")]
        [TestCase("Overworld.2.3.0", Formation.RimForest, Tank)]
        public void FiveRealSeed65FormationsSupplyTheirSelectedOwnerWithoutFixtureInjection(string id, Formation formation, string name)
        {
            // Setup deliberately cached an empty Home graph; generate from a fresh
            // detached manager so CascadeGorge cannot accidentally reuse it.
            manager = OverworldZoneManager.CreateDetached(content.Factory, 65, true);
            zone = manager.GetZone(id); manager.SetActiveZone(zone); Reveal(zone);
            Assert.AreEqual(formation, StumpCompositionPlan.Create(id, 65).Formation); Assert.True(Supported(zone));
            var owner = zone.GetReadOnlyEntities().FirstOrDefault(e => e.BlueprintName == name && zone.GetEntityCell(e) is Cell c
                && world.TryResolveFallback(zone, c.X, c.Y, out var selected) && ReferenceEquals(selected.Owner, e));
            Assert.NotNull(owner, "Actual cold generation must supply the requested natural owner."); var at = zone.GetEntityCell(owner); Native(owner, at.X, at.Y);
            owner.GetPart<RenderPart>().VisualID = "custom source"; Fallback(at.X, at.Y); Assert.False(Subject(owner, at.X, at.Y));
        }
        [TestCase("biome")] [TestCase("poi")] [TestCase("map-shape")] [TestCase("poi-shape")]
        public void CurrentMapMutationRevokesWarmAdmissionAndRestoringItRestoresArt(string fault)
        {
            var owner = Add(Tank); Native(owner); var map = manager.WorldMap; var tiles = map.Tiles; var pois = map.POIs;
            try
            {
                if (fault == "biome") tiles[2,5] = BiomeType.Spread;
                if (fault == "poi") pois[2,5] = new PointOfInterest(POIType.Lair, "current site"); // camps are sites since 2026-10-30; a lair still revokes
                if (fault == "map-shape") map.Tiles = new BiomeType[1,1];
                if (fault == "poi-shape") map.POIs = new PointOfInterest[1,1];
                Assert.False(Supported(zone)); Fallback(); Assert.False(Subject(owner));
            }
            finally { map.Tiles = tiles; map.POIs = pois; tiles[2,5] = BiomeType.Stump; pois[2,5] = null; }
            Native(owner);
        }
        [TestCase("Overworld.2.4.0")] [TestCase("Overworld.3.3.0")] [TestCase("Overworld.3.5.0")]
        [TestCase("Overworld.3.7.0")] [TestCase("Overworld.2.5.1")] [TestCase("Overworld.02.5.0")] [TestCase("Overworld.11.10.0")]
        public void RelabelingCannotAdmitSpecialSitesDepthAliasesOrOtherRegions(string id)
        {
            Native(Add(Tank)); var pos = WorldMap.FromZoneID(id); manager.WorldMap.Tiles[pos.x,pos.y] = BiomeType.Stump; manager.WorldMap.POIs[pos.x,pos.y] = null;
            zone = Attach(id); var owner = Add(Tank); Assert.False(Supported(zone)); Assert.False(Subject(owner));
        }
        [Test] public void SavedLairClaimStillRefusesAfterItsMapLabelIsRemoved()
        {
            Native(Add(Tank)); manager = OverworldZoneManager.CreateDetached(content.Factory, 65, true);
            manager.WorldMap.SetPOI(2,5,new PointOfInterest(POIType.Lair,"saved legacy site",null,1,"DesertProwler")); LairStacks.Restore(manager,null);
            manager.GetZone(Home); Assert.NotNull(LairStacks.Inspect(manager,Home)); manager.WorldMap.SetPOI(2,5,null);
            zone = Attach(Home); Add(Floor); Assert.False(Supported(zone)); Assert.False(View().LocalGround);
        }
        [Test] public void QueriesAreReadOnlyDoNotCreateALairLedgerAndRejectAReplacedGraph()
        {
            var owner = Add(Tank); Assert.False(HasLedger(manager)); int count = manager.CachedZoneCount, version = zone.EntityVersion;
            var members = zone.GetReadOnlyEntities().ToArray(); string state = zone.TileState.ToSaveString();
            for (int i = 0; i < 4; i++) Native(owner);
            Assert.False(HasLedger(manager)); Assert.AreEqual(count, manager.CachedZoneCount); Assert.AreEqual(version, zone.EntityVersion);
            CollectionAssert.AreEquivalent(members, zone.GetReadOnlyEntities()); Assert.AreEqual(state, zone.TileState.ToSaveString());
            var prior = zone; manager.SetActiveZone(new Zone(Home)); Assert.False(Supported(prior)); Assert.False(world.TryResolvePresentation(prior,X,Y,out _));
            zone = new Zone(Home); Reveal(zone); Assert.IsNull(WorldLocationContext.For(zone)); Assert.False(Supported(zone));
            manager.SetActiveZone(prior); zone = prior; Native(owner);
        }
        [TestCase("hidden")] [TestCase("remembered")] [TestCase("unexplored")]
        public void NativeBodyWaterAndPortraitDoNotRevealUndisclosedSources(string fault)
        {
            var owner = Add(Pool); Native(owner);
            if (fault == "hidden") owner.GetPart<RenderPart>().Visible = false;
            else { zone.GetCell(X,Y).IsVisible = false; zone.GetCell(X,Y).Explored = fault == "remembered"; }
            Assert.False(Water(out _,out _)); Assert.False(Subject(owner));
            if (fault == "unexplored") Assert.False(world.TryResolvePresentation(zone,X,Y,out _)); else Assert.False(View().LocalGround);
            owner.GetPart<RenderPart>().Visible = true; zone.GetCell(X,Y).IsVisible = zone.GetCell(X,Y).Explored = true; Native(owner);
        }
        [Test] public void SelectedOwnerFootprintAndRemovalKeepReadersBoundToTheActualBody()
        {
            var owner = Add(Tank); owner.AddPart(new SpatialFootprintPart{CellsRaw="0,0"}); Native(owner);
            var actor = Add("Player"); actor.GetPart<RenderPart>().RenderLayer = 100; Assert.AreSame(actor,View().Owner); Assert.False(Subject(owner));
            Assert.True(zone.MoveEntity(actor,X-2,Y)); Native(owner);
            Assert.True(zone.TryChangeFootprint(owner,"0,0;1,0")); Fallback(); Assert.False(Subject(owner));
            Assert.True(zone.TryChangeFootprint(owner,"0,0")); Native(owner);
            owner.GetPart<SpatialFootprintPart>().CellsRaw = "0,0;0,1"; Assert.False(Subject(owner)); owner.GetPart<SpatialFootprintPart>().CellsRaw = "0,0"; Native(owner);
            Assert.True(zone.RemoveEntity(owner)); Assert.False(Subject(owner));
        }
        [TestCase("foreign-render")] [TestCase("foreign-physics")] [TestCase("duplicate-render")]
        [TestCase("carried")] [TestCase("equipped")] [TestCase("takeable")] [TestCase("wrong-glyph")] [TestCase("wrong-blueprint")]
        public void MalformedOwnersCannotBorrowTheTankIdentity(string fault)
        {
            var owner = Add(Tank); Native(owner); var foreign = new Entity();
            switch (fault)
            {
                case "foreign-render": owner.GetPart<RenderPart>().ParentEntity = foreign; break;
                case "foreign-physics": owner.GetPart<PhysicsPart>().ParentEntity = foreign; break;
                case "duplicate-render": owner.AddPart(new RenderPart{RenderString="u",RenderLayer=2}); break;
                case "carried": owner.GetPart<PhysicsPart>().InInventory = foreign; break;
                case "equipped": owner.GetPart<PhysicsPart>().Equipped = foreign; break;
                case "takeable": owner.GetPart<PhysicsPart>().Takeable = true; break;
                case "wrong-glyph": owner.GetPart<RenderPart>().RenderString = "T"; break;
                case "wrong-blueprint": owner.BlueprintName = "DifferentBromeliad"; break;
            }
            Assert.False(Subject(owner)); if (View().Owner == owner) Fallback();
        }
        [TestCase("DescentLedge","solid")] [TestCase("GrainRidge","material")]
        [TestCase(Tank,"missing-well")] [TestCase(Tank,"foreign-well")] [TestCase(Tank,"finite-pool")]
        [TestCase(Vein,"wrong-yield")] [TestCase(Vein,"spent")] [TestCase(Vein,"foreign-harvest")]
        public void ExactBlueprintDoesNotOverrideChangedGameplayMeaning(string name, string fault)
        {
            var owner = Add(name); Native(owner);
            if (fault == "solid") owner.GetPart<PhysicsPart>().Solid = true;
            if (fault == "material") owner.AddPart(new MaterialPart{MaterialID="Wood"});
            if (fault == "missing-well") owner.RemovePart(owner.GetPart<WellPart>());
            if (fault == "foreign-well") owner.GetPart<WellPart>().ParentEntity = new Entity();
            if (fault == "finite-pool") owner.AddPart(new LiquidPoolPart{LiquidId="water",Volume=1});
            if (fault == "wrong-yield") owner.GetPart<HarvestablePart>().YieldBlueprint = "Bone";
            if (fault == "spent") owner.GetPart<HarvestablePart>().Harvested = true;
            if (fault == "foreign-harvest") owner.GetPart<HarvestablePart>().ParentEntity = new Entity();
            Fallback(); Assert.False(Subject(owner));
        }
        [TestCase("TepuiWall")] [TestCase("Tree")] [TestCase("Bush")]
        public void PartialDamageKeepsItsRealBodyAndDestructionDoesNotLeaveTheOldSprite(string name)
        {
            Add(Floor); var owner = Add(name); Native(owner); var part = owner.GetPart<DestructiblePart>(); int before = part.HP;
            var previous = DestructionSystem.EntityFactoryRef;
            try
            {
                DestructionSystem.EntityFactoryRef = content.Factory;
                Assert.AreEqual(DestroyVerdict.Damaged,DestructionSystem.Damage(owner,1,null,zone)); Assert.That(part.HP,Is.InRange(1,before-1)); Native(owner);
                Assert.AreEqual(DestroyVerdict.Destroyed,DestructionSystem.Damage(owner,999,null,zone)); Assert.True(part.Gone); Assert.IsNull(zone.GetEntityCell(owner)); Assert.False(Subject(owner));
                Assert.AreEqual(name == "TepuiWall" ? 1 : 0,zone.GetCell(X,Y).Objects.Count(e=>e.BlueprintName=="Rubble"));
            }
            finally { DestructionSystem.EntityFactoryRef = previous; }
        }
        [TestCase("Tree","burning")] [TestCase("Bush","wet")] [TestCase("TepuiWall","nan")]
        public void ActiveTintOrInvalidThermalStateCannotKeepNeutralMaterialArt(string name, string fault)
        {
            var owner = Add(name); Native(owner);
            if (fault == "nan") owner.GetPart<ThermalPart>().Temperature = float.NaN;
            else Assert.True(owner.ForceApplyEffect(fault == "wet" ? (Effect)new WetEffect() : new BurningEffect(),null,zone));
            Fallback(); Assert.False(Subject(owner));
        }
        [TestCase("GrainRidge")] [TestCase("DescentLedge")] [TestCase("StoneDome")] [TestCase(Tank)] [TestCase("Bush")]
        public void ExactDryTerrainOverlayRetainsOnlyItsActualUnderlyingTepuiStone(string name)
        {
            var floor = Add(Floor); var owner = Add(name); Native(owner); Assert.True(Ground(out var tile,out var actual)); Assert.AreSame(floor,actual); NativeTile(tile);
            Assert.True(zone.RemoveEntity(floor)); Assert.False(Ground(out _,out _)); Assert.False(View().LocalGround); Native(owner);
        }
        [TestCase("cultivation")] [TestCase("plantable")] [TestCase("unknown-terrain")] [TestCase("duplicate-floor")]
        [TestCase("water")] [TestCase("heat")] [TestCase("charge")]
        public void CoveredGroundDoesNotEraseCultivationOtherTerrainOrIndependentState(string fault)
        {
            var floor = Add(Floor); Native(floor); var actor = Add("Player"); actor.GetPart<RenderPart>().RenderLayer = 100;
            Assert.True(Ground(out _,out var actual)); Assert.AreSame(floor,actual);
            if (fault == "cultivation") floor.AddPart(new CultivatedSoilPart());
            if (fault == "plantable") floor.SetTag("Plantable");
            if (fault == "unknown-terrain") Add("StoneFloor");
            if (fault == "duplicate-floor") Add(Floor);
            if (fault == "water") zone.TileState.WriteCoating(X,Y,"water",ZoneTileState.Permanent);
            if (fault == "heat") zone.TileState.AddHeat(X,Y,1);
            if (fault == "charge") zone.TileState.AddCharge(X,Y,1);
            Assert.False(Ground(out _,out _)); Assert.False(View().LocalGround); Assert.AreSame(actor,View().Owner);
        }
        [Test] public void HiddenOrForeignGroundCompetitorCannotVetoVisiblePlainFloor()
        {
            var floor = Add(Floor); Native(floor); var other = Add("StoneFloor"); var render = other.GetPart<RenderPart>();
            render.Visible = false; Assert.True(Ground(out _,out var actual)); Assert.AreSame(floor,actual);
            render.Visible = true; Assert.False(Ground(out _,out _)); render.ParentEntity = new Entity(); Assert.True(Ground(out _,out actual)); Assert.AreSame(floor,actual);
            render.ParentEntity = other; Assert.False(Ground(out _,out _)); Assert.True(zone.RemoveEntity(other)); Native(floor);
        }
        [TestCase(40)] [TestCase(1)] [TestCase(0)]
        public void SprayWaterUsesActualSourceStockAndRemainsWetUnderACoveringActor(int stock)
        {
            Add(Floor); var source = Add(Pool); source.GetPart<LiquidPoolPart>().Volume = stock; Native(source);
            Assert.True(Water(out var tile,out var actual)); Assert.AreSame(source,actual); Family(source,tile); Assert.False(Ground(out _,out _));
            Assert.AreEqual(ZoneTileState.Permanent,zone.TileState.CoatingTurns(X,Y,"water"));
            var actor = Add("Player"); actor.GetPart<RenderPart>().RenderLayer = 100;
            Assert.AreSame(actor,View().Owner); Assert.NotNull(View().ForegroundTile); Assert.True(View().LocalGround); Assert.AreEqual(FlatPixelGreyboxGround.Water,View().GroundKind);
            Assert.AreSame(tile,View().GroundTile); Assert.False(Subject(source)); Assert.True(Water(out _,out actual)); Assert.AreSame(source,actual);
            Assert.True(zone.MoveEntity(actor,X-2,Y)); Native(source);
        }
        [TestCase("wrong-liquid")] [TestCase("negative-stock")] [TestCase("foreign-pool")] [TestCase("duplicate-pool")]
        [TestCase("missing-water")] [TestCase("temporary-water")] [TestCase("oil")] [TestCase("ice")] [TestCase("second-source")]
        public void WetLookingCellsCannotBypassExactSourceAndCoatingGuards(string fault)
        {
            var source = Add(Pool); Native(source);
            if (fault == "wrong-liquid") source.GetPart<LiquidPoolPart>().LiquidId = "brine";
            if (fault == "negative-stock") source.GetPart<LiquidPoolPart>().Volume = -1;
            if (fault == "foreign-pool") source.GetPart<LiquidPoolPart>().ParentEntity = new Entity();
            if (fault == "duplicate-pool") source.AddPart(new LiquidPoolPart{LiquidId="water",Volume=40});
            if (fault == "missing-water") zone.TileState.Clear(X,Y);
            if (fault == "temporary-water") { zone.TileState.Clear(X,Y); zone.TileState.WriteCoating(X,Y,"water",2); }
            if (fault == "oil" || fault == "ice") zone.TileState.WriteCoating(X,Y,fault,3);
            if (fault == "second-source") Add(Pool);
            Assert.False(Water(out _,out _)); Assert.False(Subject(source)); Assert.False(View().LocalGround);
        }
        [Test] public void LastActualFlaskFillChangesNativeStockFrameButLeavesNaturalWetOwner()
        {
            var source = Add(Pool); var pool = source.GetPart<LiquidPoolPart>(); pool.Volume = 1; Native(source); var full = View().GroundTile;
            var actor = Add("Player",X-1,Y); var flask = content.Factory.CreateEntity("LiquidFlask"); Assert.NotNull(flask);
            Assert.True(actor.GetPart<InventoryPart>().AddObject(flask));
            string command = "FillLiquidVessel|" + Uri.EscapeDataString(source.ID) + "|water";
            using (var paint = new Paint(world,zone))
            {
                paint.Step(); NativeTile(paint.GroundAt(X,Y)); long passes = EnvironmentSpriteRenderer.Perf.FullPasses;
                Assert.True(LiquidVesselService.TryAct(actor,flask,zone,command)); Assert.AreEqual(0,pool.Volume); Assert.AreEqual(1,flask.GetPart<LiquidVesselPart>().Volume);
                Assert.AreSame(source,zone.GetEntityCell(source).Objects.Single(e=>e==source)); Assert.AreEqual(ZoneTileState.Permanent,zone.TileState.CoatingTurns(X,Y,"water"));
                paint.Step(); Native(source); Assert.AreNotSame(full,paint.GroundAt(X,Y)); Assert.AreEqual(passes,EnvironmentSpriteRenderer.Perf.FullPasses); Assert.IsNull(paint.Mark);
                Assert.False(LiquidVesselService.TryAct(actor,flask,zone,command)); Assert.AreEqual(0,pool.Volume);
                Assert.True(zone.RemoveEntity(source)); paint.Step(); Assert.False(Water(out _,out _)); Assert.False(zone.TileState.HasCoating(X,Y,"water")); Assert.False(Subject(source));
            }
        }
        [Test] public void TankDrinkingHasNoFiniteStockOrProjectedWetFloorAndKeepsItsSprite()
        {
            Add(Floor); var tank = Add(Tank); Native(tank); var body = View().ForegroundTile; Assert.IsNull(tank.GetPart<LiquidPoolPart>());
            var actor = Add("Player",X-1,Y); Assert.True(Act(tank,actor,"DrawWaterAtWell")); Assert.True(Act(tank,actor,"DrawWaterAtWell"));
            Assert.IsNull(tank.GetPart<LiquidPoolPart>()); Assert.False(zone.TileState.HasCoating(X,Y,"water")); Assert.False(Water(out _,out _)); Native(tank); Assert.AreSame(body,View().ForegroundTile);
            tank.AddPart(new RepairablePart()); Assert.False(tank.GetPart<WellPart>().IsUsable); Assert.False(Act(tank,actor,"DrawWaterAtWell")); Fallback(); Assert.False(Subject(tank));
        }
        [Test] public void ActualVeinHarvestRemovesItsBodyAndKeepsExistingPortableProductIdentity()
        {
            var floor = Add(Floor); var source = Add(Vein); Native(source); var actor = Add("Player",X-1,Y);
            Assert.True(Act(source,actor,"Harvest")); Assert.True(source.GetPart<HarvestablePart>().Harvested); Assert.IsNull(zone.GetEntityCell(source)); Assert.False(Subject(source));
            var products = DensityLootTestScope.Gear(actor).Concat(zone.GetCell(X,Y).Objects).Where(e=>e.BlueprintName=="Tepuibone").Distinct().ToArray();
            Assert.IsNotEmpty(products); Assert.That(products.Sum(e=>e.GetPart<StackerPart>()?.StackCount??1),Is.InRange(1,2));
            Assert.False(Act(source,actor,"Harvest")); Assert.AreSame(floor,zone.GetCell(X,Y).Objects.Single(e=>e.BlueprintName==Floor));
            var productAtlas = Resources.Load<Texture2D>(FlatPixelHarvestProductsCatalog.ResourcePath); Assert.NotNull(productAtlas);
            using (var paint = new Paint(world,zone))
            {
                paint.Renderer.Paused = true;
                foreach (var product in products)
                {
                    Assert.AreSame(actor,product.GetPart<PhysicsPart>().InInventory,"The fresh actor must retain its actual earned product.");
                    var document = ExamineDocumentBuilder.BuildInventory(product,actor);
                    Assert.NotNull(document); Assert.True(ExamineDocumentBuilder.IsCurrentSubject(document));
                    Assert.True(paint.Renderer.TryGetFlatExaminePortrait(document,out var portrait));
                    Assert.AreSame(productAtlas,portrait.Texture);
                    Assert.AreEqual(new Rect(6*16f/productAtlas.width,0,16f/productAtlas.width,1),portrait.Uv);
                    Assert.AreEqual(Color.white,portrait.Tint); Assert.False(portrait.IsModel);
                }
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void MissingOrMalformedMainCannotReloadOrDisableExactTreeAndBush(bool malformed)
        {
            var owner = Add(Tank); Native(owner); Texture2D bad = malformed ? new Texture2D(16,24) : null;
            try
            {
                Inject(bad); Fallback(); Assert.False(Subject(owner)); Assert.True(zone.RemoveEntity(owner));
                foreach (string name in new[]{"Tree","Bush"}) { var borrowed = Add(name); Native(borrowed); Assert.True(zone.RemoveEntity(borrowed)); }
                var source = Add(Pool); string state = zone.TileState.ToSaveString();
                Assert.AreEqual(ZoneTileState.Permanent,zone.TileState.CoatingTurns(X,Y,"water"));
                using (var paint = new Paint(world,zone))
                {
                    paint.Step(); Assert.AreSame(source,View().Owner); Fallback();
                    Assert.False(Water(out _,out _)); Assert.False(View().LocalGround);
                    Assert.AreSame(CP437TilesetGenerator.GetTile('~'),paint.Mark,"Missing native water must restore its actual submitted wave.");
                    Assert.AreEqual(state,zone.TileState.ToSaveString()); Assert.AreEqual(40,source.GetPart<LiquidPoolPart>().Volume);
                }
            }
            finally { world.Dispose(); if (bad != null) Object.DestroyImmediate(bad); }
        }
        [TestCase("tree","Tree")] [TestCase("bush","Bush")]
        public void MissingBorrowedGroupCannotDisableMainAndDisposalDoesNotDestroySources(string group, string name)
        {
            var source = Resources.Load<Texture2D>(Atlas); Assert.NotNull(source); Inject(source,group);
            var borrowed = Add(name); Fallback(); Assert.False(Subject(borrowed)); Assert.True(zone.RemoveEntity(borrowed));
            var owner = Add(Tank); Native(owner); var tile = View().ForegroundTile; var sprite = tile.sprite;
            world.Dispose(); world.Dispose(); Assert.True(tile==null); Assert.True(sprite==null); Assert.True(source!=null);
            Assert.True(injectedSelector.TryResolve(zone,X,Y,out var selected)); Assert.AreSame(owner,selected.Owner);
        }
        [Test] public void ImportedSeventyOneFramesPreserveOriginalFamiliesAndRequireTheSelector()
        {
            Assert.AreEqual(71,AdapterType().GetField("FrameCount",S).GetValue(null)); Assert.AreEqual(Atlas,AdapterType().GetField("ResourcePath",S).GetValue(null));
            var atlas = Resources.Load<Texture2D>(Atlas); Assert.NotNull(atlas); Assert.AreEqual(1136,atlas.width); Assert.AreEqual(24,atlas.height);
            Assert.AreEqual(FilterMode.Point,atlas.filterMode); Assert.AreEqual(TextureWrapMode.Clamp,atlas.wrapMode); Assert.AreEqual(1,atlas.mipmapCount);
            var owner = Add(Tank); Native(owner); var tile = View().ForegroundTile; for (int i=0;i<5;i++){Native(owner);Assert.AreSame(tile,View().ForegroundTile);}
            var empty = (IDisposable)Activator.CreateInstance(AdapterType(),new object[]{atlas,null,null,null});
            try { object[] args = {zone,X,Y,owner,null}; Assert.False(Call(empty,"TryResolveForeground",args)); }
            finally { empty.Dispose(); empty.Dispose(); } Assert.True(atlas!=null);
        }
        [TestCase("heat",'^')] [TestCase("charge",'*')] [TestCase("cloud",'\'')]
        public void SubmittedWaterSuppressesOnlyItsOwnWaveAndRetainsIndependentHazardMarks(string state, char glyph)
        {
            Add(Floor); var source = Add(Pool);
            using (var paint = new Paint(world,zone))
            {
                paint.Step(); Native(source); Assert.IsNull(paint.Mark); NativeTile(paint.GroundAt(X,Y));
                if (state=="heat") zone.TileState.AddHeat(X,Y,1);
                if (state=="charge") zone.TileState.AddCharge(X,Y,1);
                if (state=="cloud") zone.TileState.WriteCloud(X,Y,"veil-mist",3);
                string saved = zone.TileState.ToSaveString(); paint.Step(); Assert.True(Water(out _,out _)); NativeTile(paint.GroundAt(X,Y));
                Assert.AreSame(CP437TilesetGenerator.GetTile(glyph),paint.Mark); Assert.AreEqual(saved,zone.TileState.ToSaveString());
                zone.GetCell(X,Y).IsVisible=false; paint.Dirty(X,Y); paint.Step(); Assert.IsNull(paint.Mark); Assert.False(View().LocalGround);
            }
        }

        // Exposed edges, not connection bits: zone north (y-1) is visual top.
        static readonly Vector2Int[] Cardinal = { Vector2Int.down, Vector2Int.right, Vector2Int.up, Vector2Int.left };
        static int BoundaryStart(string name) => name == "TepuiWall" ? 26 : name == "GrainRidge" ? 41 : name == "StoneDome" ? 56 : -1;
        void Perimeter(Entity owner, int mask, int x = X, int y = Y)
        {
            Native(owner,x,y); int frame = (int)View(x,y).ForegroundTile.sprite.rect.x / 16;
            if (mask != 0) Assert.AreEqual(BoundaryStart(owner.BlueprintName)+mask-1,frame,owner.BlueprintName+" exposed mask "+mask);
            else
            {
                int first = owner.BlueprintName == "TepuiWall" ? 4 : owner.BlueprintName == "GrainRidge" ? 6 : 20;
                Assert.That(frame,Is.InRange(first,first+(owner.BlueprintName == "GrainRidge" ? 3 : 1)),"Joined interior keeps its original family.");
            }
        }
        Entity[] JoinedCross(string name)
        {
            var neighbors = new Entity[4];
            for (int i=0;i<4;i++) neighbors[i]=Add(name,X+Cardinal[i].x,Y+Cardinal[i].y);
            return neighbors;
        }
        [TestCase("TepuiWall")] [TestCase("GrainRidge")] [TestCase("StoneDome")]
        public void PerimeterAllSixteenMasksUseExactCardinalEdgesAndKeepReaderParity(string name)
        {
            var owner=Add(name); var neighbors=new Entity[4];
            for (int mask=0;mask<16;mask++)
            {
                for(int i=0;i<4;i++)
                {
                    if(neighbors[i]!=null){Assert.True(zone.RemoveEntity(neighbors[i]));neighbors[i]=null;}
                    if((mask&(1<<i))==0) neighbors[i]=Add(name,X+Cardinal[i].x,Y+Cardinal[i].y);
                }
                int version=zone.EntityVersion, cached=manager.CachedZoneCount; string state=zone.TileState.ToSaveString();
                Perimeter(owner,mask); // Includes asymmetric N=1/E=2 and isolated=15.
                Assert.AreEqual(version,zone.EntityVersion); Assert.AreEqual(cached,manager.CachedZoneCount);
                Assert.AreEqual(state,zone.TileState.ToSaveString()); Assert.False(HasLedger(manager));
            }
            var prior=zone; manager.SetActiveZone(new Zone(Home));
            Assert.False(Supported(prior)); Assert.False(world.TryResolvePresentation(prior,X,Y,out _));
            manager.SetActiveZone(prior); Perimeter(owner,15);
        }
        [TestCase("TepuiWall")] [TestCase("GrainRidge")] [TestCase("StoneDome")]
        public void PerimeterUndisclosedCoveredOrInvalidNeighborsEqualAnAbsentNeighbor(string name)
        {
            var owner=Add(name); var neighbors=JoinedCross(name); Perimeter(owner,0);
            string[] faults={"hidden","remembered","unexplored","covered","custom","foreign-render","foreign-physics","duplicate-render","carried","non-solid","changed-meaning","footprint"};
            foreach(string fault in faults)
            {
                var north=neighbors[0]; var cell=zone.GetCell(X,Y-1); Entity cover=null;
                switch(fault)
                {
                    case "hidden": north.GetPart<RenderPart>().Visible=false; break;
                    case "remembered": cell.IsVisible=false; break;
                    case "unexplored": cell.Explored=cell.IsVisible=false; break;
                    case "covered": cover=Add("Player",X,Y-1);cover.GetPart<RenderPart>().RenderLayer=100;break;
                    case "custom": north.GetPart<RenderPart>().VisualVariant="different body";break;
                    case "foreign-render": north.GetPart<RenderPart>().ParentEntity=new Entity();break;
                    case "foreign-physics": north.GetPart<PhysicsPart>().ParentEntity=new Entity();break;
                    case "duplicate-render": north.AddPart(new RenderPart{RenderString="#",RenderLayer=2});break;
                    case "carried": north.GetPart<PhysicsPart>().InInventory=new Entity();break;
                    case "non-solid": north.GetPart<PhysicsPart>().Solid=false;break;
                    case "changed-meaning": north.AddPart(new RepairablePart());break;
                    case "footprint": north.AddPart(new SpatialFootprintPart{CellsRaw="0,0;1,0"});break;
                }
                Assert.DoesNotThrow(()=>Perimeter(owner,1),fault);
                Assert.True(zone.RemoveEntity(north)); if(cover!=null)Assert.True(zone.RemoveEntity(cover));
                Perimeter(owner,1); // A hidden real owner and no owner are indistinguishable.
                cell.Explored=cell.IsVisible=true; neighbors[0]=Add(name,X,Y-1); Perimeter(owner,0);
            }
        }
        [TestCase("TepuiWall")] [TestCase("GrainRidge")] [TestCase("StoneDome")]
        public void PerimeterOnlySameFamilyCardinalBodiesJoin(string name)
        {
            var owner=Add(name);
            foreach(var delta in new[]{new Vector2Int(-1,-1),new Vector2Int(1,-1),new Vector2Int(-1,1),new Vector2Int(1,1)})
                Add(name,X+delta.x,Y+delta.y);
            string other=name=="TepuiWall"?"GrainRidge":"TepuiWall";
            var different=Add(other,X,Y-1); Perimeter(owner,15);
            Assert.True(zone.RemoveEntity(different)); var north=Add(name,X,Y-1); Perimeter(owner,14);
            var ledge=Add("DescentLedge",X+1,Y); Perimeter(owner,14);
            Assert.True(zone.RemoveEntity(ledge)); Add(name,X+1,Y); Perimeter(owner,12);
            Assert.True(zone.RemoveEntity(north)); Perimeter(owner,13);
        }
        [TestCase("TepuiWall")] [TestCase("GrainRidge")] [TestCase("StoneDome")]
        public void PerimeterZoneEdgesNeverReadOrLoadAnotherZone(string name)
        {
            int count=manager.CachedZoneCount;
            foreach(var corner in new[]{new Vector2Int(0,0),new Vector2Int(Zone.Width-1,0),new Vector2Int(0,Zone.Height-1),new Vector2Int(Zone.Width-1,Zone.Height-1)})
            {
                var owner=Add(name,corner.x,corner.y); int mask=0;
                for(int i=0;i<4;i++)
                {
                    int nx=corner.x+Cardinal[i].x,ny=corner.y+Cardinal[i].y;
                    if(zone.GetCell(nx,ny)==null)mask|=1<<i;else Add(name,nx,ny);
                }
                Perimeter(owner,mask,corner.x,corner.y); // NW=9, NE=3, SW=12, SE=6.
                Assert.AreEqual(count,manager.CachedZoneCount);
            }
        }
        [TestCase("TepuiWall")] [TestCase("GrainRidge")] [TestCase("StoneDome")]
        public void PerimeterDoesNotConfuseIndependentTileOverlaysWithSolidOwnerState(string name)
        {
            var owner=Add(name); JoinedCross(name); Perimeter(owner,0);
            zone.TileState.WriteCoating(X,Y-1,"water",ZoneTileState.Permanent); zone.TileState.AddHeat(X,Y-1,1); zone.TileState.AddCharge(X,Y-1,1);
            string state=zone.TileState.ToSaveString(); Perimeter(owner,0); Assert.AreEqual(state,zone.TileState.ToSaveString());
        }
        [TestCase("TepuiWall")] [TestCase("GrainRidge")] [TestCase("StoneDome")]
        public void PerimeterSubmittedNeighborCoverAndUncoverRepaintWithoutFullRedrawOrStateChanges(string name)
        {
            var owner=Add(name); JoinedCross(name);
            using(var paint=new Paint(world,zone))
            {
                paint.Step(); Perimeter(owner,0); var interior=paint.At(X,Y); Assert.NotNull(interior);
                long full=EnvironmentSpriteRenderer.Perf.FullPasses; string state=zone.TileState.ToSaveString();
                var cover=Add("Player",X,Y-1); cover.GetPart<RenderPart>().RenderLayer=100; paint.Dirty(X,Y-1); paint.Step();
                Perimeter(owner,1); Assert.AreSame(View().ForegroundTile,paint.At(X,Y)); Assert.AreNotSame(interior,paint.At(X,Y));
                Assert.True(zone.RemoveEntity(cover)); paint.Dirty(X,Y-1); paint.Step();
                Perimeter(owner,0); Assert.AreSame(interior,paint.At(X,Y)); Assert.AreEqual(full,EnvironmentSpriteRenderer.Perf.FullPasses);
                Assert.AreEqual(state,zone.TileState.ToSaveString());
            }
        }
        [Test] public void PerimeterRealWallDestructionExposesItsNeighborsThroughTheExistingDirtyHook()
        {
            var owner=Add("TepuiWall"); var north=JoinedCross("TepuiWall")[0]; var previous=DestructionSystem.EntityFactoryRef;
            try
            {
                DestructionSystem.EntityFactoryRef=content.Factory;
                using(var paint=new Paint(world,zone))
                {
                    paint.Step(); Perimeter(owner,0); long full=EnvironmentSpriteRenderer.Perf.FullPasses; string state=zone.TileState.ToSaveString();
                    Assert.AreEqual(DestroyVerdict.Destroyed,DestructionSystem.Damage(north,999,null,zone)); paint.Step();
                    Perimeter(owner,1); Assert.AreSame(View().ForegroundTile,paint.At(X,Y));
                    Assert.AreEqual(full,EnvironmentSpriteRenderer.Perf.FullPasses); Assert.AreEqual(state,zone.TileState.ToSaveString());
                    Assert.AreEqual(1,zone.GetCell(X,Y-1).Objects.Count(e=>e.BlueprintName=="Rubble"));
                }
            }
            finally{DestructionSystem.EntityFactoryRef=previous;}
        }
        // Reuses the existing submitted ZoneRenderer fixture, not a second painter.
        sealed class Paint : IDisposable
        {
            readonly HotbarSaveFixture saved;
            readonly GameObject root;
            readonly Tilemap overlay, marks, ground;
            public ZoneRenderer Renderer { get; }
            readonly Action<int,int,string> oldCell=ZoneRenderHooks.CellDirtyCallback;
            readonly Action<string> oldFull=ZoneRenderHooks.FullDirtyCallback;
            readonly FieldInfo bound=typeof(ZoneTileStateSystem).GetField("_boundRenderZone",S);
            readonly object oldBound; readonly Zone previous; readonly Action<int,int> oldChanged; readonly Action oldSight;
            readonly Dictionary<FieldInfo,object> perf=new Dictionary<FieldInfo,object>();
            public Paint(FlatPixelGreyboxCatalog catalog,Zone z)
            {
                oldBound=bound.GetValue(null);
                if(oldBound is WeakReference<Zone> weak && weak.TryGetTarget(out previous)){oldChanged=previous.TileState.OnCellChanged;oldSight=previous.TileState.OnSightChanged;}
                try
                {
                    foreach(var field in typeof(EnvironmentSpriteRenderer.Perf).GetFields(BindingFlags.Public|BindingFlags.Static))perf[field]=field.GetValue(null);
                    saved=new HotbarSaveFixture();root=new GameObject("Stump submitted grid",typeof(Grid));saved.Root.transform.SetParent(root.transform,false);
                    var main=saved.Root.GetComponent<Tilemap>();var bg=new GameObject("Background",typeof(Tilemap));bg.transform.SetParent(root.transform,false);ground=bg.GetComponent<Tilemap>();
                    var mark=new GameObject("Hazards",typeof(Tilemap));mark.transform.SetParent(root.transform,false);marks=mark.GetComponent<Tilemap>();
                    Renderer=saved.Renderer;Write(Renderer,"_tilemap",main);Write(Renderer,"_bgTilemap",ground);Write(Renderer,"_tileStateTilemap",marks);
                    var camera=new GameObject("Camera",typeof(Camera));camera.transform.SetParent(root.transform,false);camera.GetComponent<Camera>().orthographic=true;Write(Renderer,"_mainCamera",camera.GetComponent<Camera>());
                    var env=saved.Root.AddComponent<EnvironmentSpriteRenderer>();env.Init(root.transform,main,ground);env.RenderingEnabled=true;env.FlatPixelGreyboxEnabled=true;
                    Write(env,"_flatPixelGreybox",catalog);overlay=(Tilemap)Read(env,"_overlayTilemap");Write(Renderer,"_envSpriteRenderer",env);Write(Renderer,"_flatPixelModeEnabled",true);Renderer.SetZone(z);
                    ZoneRenderHooks.CellDirtyCallback=Renderer.MarkCellDirty;ZoneRenderHooks.FullDirtyCallback=Renderer.MarkDirty;
                }
                catch { Dispose();throw; }
            }
            public Tile At(int x,int y)=>overlay.GetTile<Tile>(new Vector3Int(x,Zone.Height-1-y,0));
            public Tile GroundAt(int x,int y)=>ground.GetTile<Tile>(new Vector3Int(x,Zone.Height-1-y,0));
            public TileBase Mark=>marks.GetTile(new Vector3Int(X,Zone.Height-1-Y,0));
            public void Dirty(int x,int y)=>Renderer.MarkCellDirty(x,y,"Stump current material");
            public void Step(){try{typeof(ZoneRenderer).GetMethod("LateUpdate",I).Invoke(Renderer,null);}catch(TargetInvocationException e){throw e.InnerException??e;}}
            public void Dispose()
            {
                try { saved?.Dispose();if(root!=null)Object.DestroyImmediate(root); }
                finally
                {
                    ZoneRenderHooks.CellDirtyCallback=oldCell;ZoneRenderHooks.FullDirtyCallback=oldFull;bound.SetValue(null,oldBound);
                    if(previous!=null){previous.TileState.OnCellChanged=oldChanged;previous.TileState.OnSightChanged=oldSight;}
                    foreach(var entry in perf)entry.Key.SetValue(null,entry.Value);
                }
            }
        }
    }
}
