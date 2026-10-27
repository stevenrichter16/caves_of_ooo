// Four exact Overwrit bodies; production is absent at the first native RED.
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
    public sealed class FlatPixelOverwritMaterialsTests
    {
        const string Home = "Overworld.0.9.0", Atlas = "Sprites/Live16x24/overwrit-materials";
        const string Floor = "OverwritGround", Growth = "OverwritNewGrowth", Bench = "OverwritPilgrimBench", Marker = "OverwritWaymarker";
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

        [SetUp] public void Setup()
        {
            liquids = (Dictionary<string, LiquidDefinition>)typeof(LiquidRegistry).GetField("_byId", S).GetValue(null);
            previousLiquids = new Dictionary<string, LiquidDefinition>(liquids); previouslyInitialized = LiquidRegistry.IsInitialized;
            LiquidRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/LiquidDefinitions").Select(t => t.text));
            content = new DensityLootTestScope(); manager = OverworldZoneManager.CreateDetached(content.Factory, 65, true);
            Assert.AreEqual(BiomeType.Overwrit, manager.WorldMap.GetBiome(0, 9)); Assert.IsNull(manager.WorldMap.GetPOI(0, 9));
            Assert.True(OverwritCompositionPlan.IsWildernessZone(Home)); zone = Attach(Home); world = new FlatPixelGreyboxCatalog();
        }
        [TearDown] public void Cleanup()
        {
            try { world?.Dispose(); injectedAdapter?.Dispose(); injectedSelector?.Dispose(); content?.Dispose(); }
            finally
            {
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
        { Assert.True(world.TryResolvePresentation(zone, x, y, out var result)); return result; }
        static Type AdapterType()
        { var type = typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelOverwritMaterialsCatalog"); Assert.NotNull(type, "Overwrit native adapter is not implemented."); return type; }
        static void Write(object target, string name, object value)
        { var field = target.GetType().GetField(name, I); Assert.NotNull(field, name); field.SetValue(target, value); }
        static object Read(object target, string name)
        { var field = target.GetType().GetField(name, I); Assert.NotNull(field, name); return field.GetValue(target); }
        object Adapter()
        { var value = Read(world, "_overwritMaterialsArt"); if (value == null) { View(); value = Read(world, "_overwritMaterialsArt"); } Assert.NotNull(value); return value; }
        bool Supported(Zone graph) => (bool)AdapterType().GetMethod("IsSupportedZone", S).Invoke(null, new object[] { graph });
        bool Ground(out Tile tile, out Entity floor, int x = X, int y = Y)
        { var adapter = Adapter(); object[] args = { zone, x, y, null, null }; bool ok = (bool)adapter.GetType().GetMethod("TryResolveGround", I).Invoke(adapter, args); tile = (Tile)args[3]; floor = (Entity)args[4]; return ok; }
        bool Subject(Entity owner, int x = X, int y = Y)
        { var adapter = Adapter(); object[] args = { zone, owner, x, y, null }; return (bool)adapter.GetType().GetMethod("TryGetSubjectSprite", I).Invoke(adapter, args); }
        static void NativeTile(Tile tile)
        {
            var source = Resources.Load<Texture2D>(Atlas); Assert.NotNull(source, Atlas); Assert.NotNull(tile); Assert.NotNull(tile.sprite);
            Assert.AreSame(source, tile.sprite.texture); Assert.AreEqual(new Vector2(16, 24), tile.sprite.rect.size);
            Assert.AreEqual(new Vector2(8, 12), tile.sprite.pivot); Assert.AreEqual(16, tile.sprite.pixelsPerUnit);
            Assert.AreEqual(TileFlags.None, tile.flags); Assert.AreEqual(Tile.ColliderType.None, tile.colliderType);
        }
        static void Family(Entity owner, Tile tile)
        {
            int first = owner.BlueprintName == Floor ? 0 : owner.BlueprintName == Growth ? 4 : owner.BlueprintName == Bench ? 6 : 7;
            int last = owner.BlueprintName == Floor ? 3 : owner.BlueprintName == Growth ? 5 : first;
            Assert.That((int)tile.sprite.rect.x / 16, Is.InRange(first, last));
        }
        void Native(Entity owner, int x = X, int y = Y)
        {
            var value = View(x, y); Assert.AreSame(owner, value.Owner); bool ground = owner.BlueprintName == Floor;
            var tile = ground ? value.GroundTile : value.ForegroundTile; NativeTile(tile); Family(owner, tile);
            if (ground) { Assert.True(value.LocalGround); Assert.AreEqual(Color.white, value.GroundColor); Assert.IsNull(value.ForegroundTile); }
            else { Assert.True(value.NativeForeground); Assert.AreEqual(Color.white, value.ForegroundColor); }
            Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone), "Regional art must not widen opening admission.");
            Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, x, y, out var sprite, out var tint));
            Assert.AreSame(tile.sprite.texture, sprite.texture); Assert.AreEqual(tile.sprite.rect, sprite.rect); Assert.AreEqual(Color.white, tint);
        }
        void Fallback(int x = X, int y = Y)
        {
            Assert.True(world.TryResolveFallback(zone, x, y, out var expected)); var value = View(x, y);
            Assert.AreSame(expected.Owner, value.Owner); Assert.False(value.NativeForeground); Assert.AreSame(expected.ForegroundTile, value.ForegroundTile);
        }
        void Inject(Texture2D atlas)
        {
            world.Dispose(); injectedAdapter?.Dispose(); injectedSelector?.Dispose(); world = new FlatPixelGreyboxCatalog((Texture2D)null);
            injectedSelector = new FlatPixelFallbackCatalog(); injectedAdapter = (IDisposable)Activator.CreateInstance(AdapterType(), new object[] { atlas, injectedSelector });
            Write(world, "_overwritMaterialsArt", injectedAdapter);
        }
        static bool HasLedger(OverworldZoneManager owner)
        { return ((System.Runtime.CompilerServices.ConditionalWeakTable<OverworldZoneManager, LairStackLedgerPart>)typeof(LairStacks).GetField("Ledgers", S).GetValue(null)).TryGetValue(owner, out _); }

        [TestCase(Floor)] [TestCase(Growth)] [TestCase(Bench)] [TestCase(Marker)]
        public void ExactOwnersUseFrozenNativeFamiliesAndPublicReaderButCustomVariantDoesNot(string name)
        {
            var owner = Add(name); int version = zone.EntityVersion; string state = zone.TileState.ToSaveString(); Native(owner);
            Assert.AreEqual(owner.GetPart<PhysicsPart>().Solid, View().BlocksMovement);
            Assert.AreEqual(version, zone.EntityVersion); Assert.AreEqual(state, zone.TileState.ToSaveString());
            owner.GetPart<RenderPart>().VisualVariant = "different current identity";
            Fallback(); Assert.False(Subject(owner)); if (name == Floor) Assert.False(View().LocalGround);
            owner.GetPart<RenderPart>().VisualVariant = ""; Native(owner);
        }
        [Test] public void FiniteOrdinaryTableAdmitsExactlyTwentyTwoCurrentAddresses()
        {
            int supported = 0;
            for (int y = 0; y < WorldMap.Height; y++) for (int x = 0; x < WorldMap.Width; x++)
            {
                // Only 23 Overwrit map cells need a graph; other-biome relabeling
                // is covered separately without allocating 400 full chunks.
                if (manager.WorldMap.GetBiome(x, y) != BiomeType.Overwrit) continue;
                string id = WorldMap.ToZoneID(x, y); zone = Attach(id);
                bool expected = OverwritCompositionPlan.IsWildernessZone(id) && manager.WorldMap.GetBiome(x, y) == BiomeType.Overwrit && manager.WorldMap.GetPOI(x, y) == null;
                Assert.AreEqual(expected, Supported(zone), id); if (expected) supported++;
            }
            Assert.AreEqual(22, supported); Assert.False(Supported(null));
        }
        [TestCase("Overworld.0.9.0", OverwritFormation.GreenRim, Growth)]
        [TestCase("Overworld.0.10.0", OverwritFormation.Blank, Floor)]
        [TestCase("Overworld.4.12.0", OverwritFormation.PilgrimageRim, Bench)]
        [TestCase("Overworld.0.13.0", OverwritFormation.DryRim, Growth)]
        public void FourNaturalSeed65RolesSupplyActualOwnersWithoutInjection(string id, OverwritFormation role, string name)
        {
            manager = OverworldZoneManager.CreateDetached(content.Factory, 65, true); zone = manager.GetZone(id); manager.SetActiveZone(zone); Reveal(zone);
            Assert.AreEqual(role, OverwritCompositionPlan.Create(id, 65).Formation); Assert.True(Supported(zone));
            var owner = zone.GetReadOnlyEntities().FirstOrDefault(e => e.BlueprintName == name && !e.HasPart<CultivatedSoilPart>() && zone.GetEntityCell(e) is Cell c
                && world.TryResolveFallback(zone, c.X, c.Y, out var selected) && ReferenceEquals(selected.Owner, e));
            Assert.NotNull(owner, "The natural role must expose the requested owner; marker is not an observed seed65 claim."); var at = zone.GetEntityCell(owner);
            int version = zone.EntityVersion; Native(owner, at.X, at.Y); Assert.AreEqual(version, zone.EntityVersion);
            owner.GetPart<RenderPart>().VisualID = "custom source"; Fallback(at.X, at.Y); Assert.False(Subject(owner, at.X, at.Y));
        }
        [TestCase("Overworld.0.9.0", "HollowchimeCrop")]
        [TestCase("Overworld.4.12.0", "MargincressCrop")]
        [TestCase("Overworld.3.14.0", "BinderrootCrop")]
        public void ActualCropBedsModifyTheSameFloorAndCannotBePaintedAsPlainGround(string id, string cropName)
        {
            manager = OverworldZoneManager.CreateDetached(content.Factory, 65, true); zone = manager.GetZone(id); manager.SetActiveZone(zone); Reveal(zone);
            Assert.True(Supported(zone)); var crops = zone.GetReadOnlyEntities().Where(e => e.BlueprintName == cropName).ToArray(); Assert.AreEqual(2, crops.Length);
            foreach (var crop in crops)
            {
                var c = zone.GetEntityCell(crop); var floor = c.Objects.Single(e => e.BlueprintName == Floor);
                var soil = floor.GetPart<CultivatedSoilPart>(); Assert.NotNull(soil); Assert.AreSame(floor, soil.ParentEntity); Assert.True(floor.HasTag("Plantable"));
                Assert.False(Ground(out _, out _, c.X, c.Y)); Assert.False(View(c.X, c.Y).LocalGround); Assert.AreSame(crop, View(c.X, c.Y).Owner);
                Assert.False(Subject(floor, c.X, c.Y));
            }
            var plain = zone.GetReadOnlyEntities().First(e => e.BlueprintName == Floor && !e.HasPart<CultivatedSoilPart>() && zone.GetEntityCell(e).Objects.Count == 1);
            var p = zone.GetEntityCell(plain); Native(plain, p.X, p.Y);
        }
        [TestCase("biome")] [TestCase("poi")] [TestCase("map-shape")] [TestCase("poi-shape")]
        public void MutableCurrentMapCanRevokeAndRestoreWarmAdmission(string fault)
        {
            var owner = Add(Bench); Native(owner); var map = manager.WorldMap; var tiles = map.Tiles; var pois = map.POIs;
            try
            {
                if (fault == "biome") tiles[0,9] = BiomeType.Spread;
                if (fault == "poi") pois[0,9] = new PointOfInterest(POIType.MerchantCamp, "current site");
                if (fault == "map-shape") map.Tiles = new BiomeType[1,1];
                if (fault == "poi-shape") map.POIs = new PointOfInterest[1,1];
                Assert.False(Supported(zone)); Fallback(); Assert.False(Subject(owner));
            }
            finally { map.Tiles = tiles; map.POIs = pois; tiles[0,9] = BiomeType.Overwrit; pois[0,9] = null; }
            Native(owner);
        }
        [TestCase("Overworld.2.11.0")] [TestCase("Overworld.0.9.1")] [TestCase("Overworld.00.9.0")] [TestCase("Overworld.4.10.0")]
        public void RewritingMapLabelsCannotAdmitUnsayingDepthOrAlias(string id)
        {
            Native(Add(Bench)); var p = WorldMap.FromZoneID(id); manager.WorldMap.Tiles[p.x,p.y] = BiomeType.Overwrit; manager.WorldMap.POIs[p.x,p.y] = null;
            zone = Attach(id); var owner = Add(Bench); Assert.False(Supported(zone)); Fallback(); Assert.False(Subject(owner));
        }
        [Test] public void SavedLegacyLairClaimOutlivesRemovedMapLabel()
        {
            Native(Add(Bench)); manager = OverworldZoneManager.CreateDetached(content.Factory, 65, true);
            manager.WorldMap.SetPOI(0,9,new PointOfInterest(POIType.Lair,"saved legacy site",null,1,"DesertProwler")); LairStacks.Restore(manager,null);
            var generated = manager.GetZone(Home); var claim = LairStacks.Inspect(manager,Home); Assert.NotNull(claim); Assert.True(claim.Legacy);
            manager.WorldMap.SetPOI(0,9,null); zone = Attach(Home); Add(Floor); Assert.AreNotSame(generated,zone);
            Assert.False(Supported(zone)); Assert.True((bool)typeof(LairStacks).GetMethod("HasSavedColumn",S).Invoke(null,new object[]{manager,Home})); Assert.False(View().LocalGround);
        }
        [Test] public void QueriesDoNotCreateLedgerOrMutateWorldAndRejectReplacedGraph()
        {
            var owner = Add(Bench); Assert.False(HasLedger(manager)); int count = manager.CachedZoneCount, version = zone.EntityVersion;
            var members = zone.GetReadOnlyEntities().ToArray(); string state = zone.TileState.ToSaveString();
            for (int n = 0; n < 5; n++) { Native(owner); Assert.True(Supported(zone)); }
            Assert.False(HasLedger(manager)); Assert.AreEqual(count,manager.CachedZoneCount); Assert.AreEqual(version,zone.EntityVersion);
            CollectionAssert.AreEquivalent(members,zone.GetReadOnlyEntities()); Assert.AreEqual(state,zone.TileState.ToSaveString());
            var prior = zone; manager.SetActiveZone(new Zone(Home)); Assert.False(Supported(prior)); Assert.False(world.TryResolvePresentation(prior,X,Y,out _));
            zone = new Zone(Home); Reveal(zone); Add(Bench); Assert.IsNull(WorldLocationContext.For(zone)); Assert.False(Supported(zone));
            manager.SetActiveZone(prior); zone = prior; Native(owner);
        }
        [TestCase("hidden")] [TestCase("remembered")] [TestCase("unexplored")]
        public void NativeBodyAndPortraitRespectCurrentDisclosure(string fault)
        {
            var owner = Add(Growth); Native(owner);
            if (fault == "hidden") owner.GetPart<RenderPart>().Visible = false;
            else { zone.GetCell(X,Y).IsVisible = false; zone.GetCell(X,Y).Explored = fault == "remembered"; }
            Assert.False(Subject(owner));
            if (fault == "unexplored") Assert.False(world.TryResolvePresentation(zone,X,Y,out _)); else Assert.False(View().NativeForeground);
            owner.GetPart<RenderPart>().Visible = true; zone.GetCell(X,Y).Explored = zone.GetCell(X,Y).IsVisible = true; Native(owner);
        }
        [Test] public void SelectedOwnerCurrentFootprintAndRemovalControlPortrait()
        {
            var owner = Add(Bench); owner.AddPart(new SpatialFootprintPart{CellsRaw="0,0"}); Native(owner);
            var actor = Add("Player"); actor.GetPart<RenderPart>().RenderLayer = 100; Assert.AreSame(actor,View().Owner); Assert.False(Subject(owner));
            Assert.True(zone.MoveEntity(actor,X-2,Y)); Native(owner);
            Assert.True(zone.TryChangeFootprint(owner,"0,0;1,0")); Fallback(); Assert.False(Subject(owner)); Assert.True(zone.TryChangeFootprint(owner,"0,0")); Native(owner);
            owner.GetPart<SpatialFootprintPart>().CellsRaw = "0,0;0,1"; Assert.False(Subject(owner)); owner.GetPart<SpatialFootprintPart>().CellsRaw = "0,0"; Native(owner);
            Assert.True(zone.RemoveEntity(owner)); Assert.False(Subject(owner)); Assert.AreNotSame(owner,View().Owner);
        }
        [TestCase("foreign-render")] [TestCase("foreign-physics")] [TestCase("duplicate-render")] [TestCase("duplicate-material")]
        [TestCase("carried")] [TestCase("equipped")] [TestCase("takeable")] [TestCase("zero-stack")] [TestCase("wrong-glyph")] [TestCase("wrong-blueprint")]
        public void MalformedOwnershipCustodyOrIdentityCannotBorrowBenchPixels(string fault)
        {
            var owner = Add(Bench); Native(owner); var foreign = new Entity();
            switch (fault)
            {
                case "foreign-render": owner.GetPart<RenderPart>().ParentEntity = foreign; break;
                case "foreign-physics": owner.GetPart<PhysicsPart>().ParentEntity = foreign; break;
                case "duplicate-render": owner.AddPart(new RenderPart{RenderString="_",RenderLayer=1}); break;
                case "duplicate-material": owner.AddPart(new MaterialPart{MaterialID="Wood"}); break;
                case "carried": owner.GetPart<PhysicsPart>().InInventory = foreign; break;
                case "equipped": owner.GetPart<PhysicsPart>().Equipped = foreign; break;
                case "takeable": owner.GetPart<PhysicsPart>().Takeable = true; break;
                case "zero-stack": owner.AddPart(new StackerPart{StackCount=0}); break;
                case "wrong-glyph": owner.GetPart<RenderPart>().RenderString = "I"; break;
                case "wrong-blueprint": owner.BlueprintName = "RestingPilgrimBench"; break;
            }
            Assert.False(Subject(owner)); if (View().Owner == owner) Fallback();
        }
        [TestCase(Bench,"restable")] [TestCase(Marker,"sign")] [TestCase(Growth,"harvest")]
        [TestCase(Floor,"material")] [TestCase(Marker,"wrong-wreckage")] [TestCase(Bench,"wrong-material")]
        public void AddedOrChangedMechanicsDoNotKeepTheOldStaticIdentity(string name, string fault)
        {
            var owner = Add(name); Native(owner);
            if (fault == "restable") owner.SetTag("Restable");
            if (fault == "sign") owner.AddPart(new RegionalSignpostPart());
            if (fault == "harvest") owner.AddPart(new HarvestablePart{YieldBlueprint="WildBerries"});
            if (fault == "material") owner.AddPart(new MaterialPart{MaterialID="Stone"});
            if (fault == "wrong-wreckage") owner.GetPart<DestructiblePart>().WreckageBlueprint = "SalvagedTimber";
            if (fault == "wrong-material") owner.GetPart<MaterialPart>().MaterialID = "Stone";
            Fallback(); Assert.False(Subject(owner)); if (name == Floor) Assert.False(View().LocalGround);
        }
        [TestCase(Growth)] [TestCase(Bench)] [TestCase(Marker)]
        public void RealPartialDamageKeepsBodyAndDestructionRemovesItWithOnlyItsTrueWreckage(string name)
        {
            var floor = Add(Floor); var owner = Add(name); Native(owner); var part = owner.GetPart<DestructiblePart>(); int initial = part.HP;
            var previous = DestructionSystem.EntityFactoryRef;
            try
            {
                DestructionSystem.EntityFactoryRef = content.Factory;
                Assert.AreEqual(DestroyVerdict.Damaged,DestructionSystem.Damage(owner,1,null,zone)); Assert.That(part.HP,Is.InRange(1,initial-1)); Native(owner);
                Assert.AreEqual(DestroyVerdict.Destroyed,DestructionSystem.Damage(owner,999,null,zone)); Assert.True(part.Gone); Assert.IsNull(zone.GetEntityCell(owner)); Assert.False(Subject(owner));
                Assert.AreSame(floor,zone.GetCell(X,Y).Objects.Single(e=>e.BlueprintName==Floor));
                int expected = name == Marker ? 1 : 0; Assert.AreEqual(expected,zone.GetCell(X,Y).Objects.Count(e=>e.BlueprintName=="Rubble"));
                DestructionSystem.Destroy(owner,null,zone,"idempotence counter"); Assert.AreEqual(expected,zone.GetCell(X,Y).Objects.Count(e=>e.BlueprintName=="Rubble"));
                if (expected == 0) Native(floor);
            }
            finally { DestructionSystem.EntityFactoryRef = previous; }
        }
        [TestCase("gone")] [TestCase("zero")] [TestCase("indestructible")]
        public void InvalidCurrentStructuralStateRefusesEvenBeforePhysicalRemoval(string fault)
        {
            var owner = Add(Bench); Native(owner); var part = owner.GetPart<DestructiblePart>();
            if (fault == "gone") part.Gone = true; if (fault == "zero") part.HP = 0; if (fault == "indestructible") part.Indestructible = true;
            Fallback(); Assert.False(Subject(owner));
        }
        [TestCase("frozen")] [TestCase("flame")] [TestCase("nan")] [TestCase("foreign")]
        public void NonNeutralOrMalformedThermalStateCannotKeepNeutralWood(string state)
        {
            var owner = Add(Bench); Native(owner); var thermal = owner.GetPart<ThermalPart>();
            if (state == "frozen") thermal.Temperature = thermal.FreezeTemperature;
            if (state == "flame") thermal.Temperature = thermal.FlameTemperature;
            if (state == "nan") thermal.Temperature = float.NaN;
            if (state == "foreign") thermal.ParentEntity = new Entity();
            Fallback(); Assert.False(Subject(owner));
        }
        [TestCase("wet")] [TestCase("burning")]
        public void ActuallyAppliedRenderEffectCannotBeHiddenByNeutralPixels(string state)
        {
            var owner = Add(Growth); Native(owner);
            Assert.True(state == "wet" ? owner.ForceApplyEffect(new WetEffect(),null,zone) : owner.ForceApplyEffect(new BurningEffect(),null,zone), "Effect must actually apply before testing refusal.");
            Assert.NotNull(owner.GetPart<StatusEffectsPart>()?.GetRenderColorOverride()); Fallback(); Assert.False(Subject(owner));
        }
        [TestCase(Growth)] [TestCase(Bench)] [TestCase(Marker)]
        public void ExactDryBodyKeepsItsRealSubstrateButDoesNotInventOne(string name)
        {
            var floor = Add(Floor); var owner = Add(name); Native(owner); Assert.True(Ground(out var tile,out var actual)); Assert.AreSame(floor,actual); NativeTile(tile);
            Assert.True(zone.RemoveEntity(floor)); Assert.False(Ground(out _,out _)); Assert.False(View().LocalGround); Native(owner);
            floor = Add(Floor); Assert.True(Ground(out _,out actual)); Assert.AreSame(floor,actual);
        }
        [TestCase("coating")] [TestCase("heat")] [TestCase("charge")] [TestCase("unknown-terrain")] [TestCase("cultivation")] [TestCase("duplicate-floor")]
        public void RealFloorDoesNotEraseCompetingGroundOrIndependentTileState(string fault)
        {
            var floor = Add(Floor); Native(floor); var actor = Add("Player"); actor.GetPart<RenderPart>().RenderLayer=100;
            Assert.True(Ground(out _,out var actual)); Assert.AreSame(floor,actual);
            if (fault == "coating") zone.TileState.WriteCoating(X,Y,"water",3);
            if (fault == "heat") zone.TileState.AddHeat(X,Y,1);
            if (fault == "charge") zone.TileState.AddCharge(X,Y,1);
            if (fault == "unknown-terrain") Add("StoneFloor");
            if (fault == "cultivation") { floor.AddPart(new CultivatedSoilPart()); floor.SetTag("Plantable"); }
            if (fault == "duplicate-floor") Add(Floor);
            Assert.False(Ground(out _,out _)); Assert.False(View().LocalGround); Assert.AreSame(actor,View().Owner);
        }
        [Test] public void OnlyVisibleCurrentCultivationCompetitorCanVetoDisclosedFloor()
        {
            var floor = Add(Floor); Native(floor); var original = View().GroundTile; var other = Add("StoneFloor"); other.AddPart(new CultivatedSoilPart());
            var render = other.GetPart<RenderPart>(); render.Visible=false; Assert.True(Ground(out var tile,out var actual)); Assert.AreSame(original,tile); Assert.AreSame(floor,actual);
            render.Visible=true; Assert.False(Ground(out _,out _)); render.ParentEntity=new Entity(); Assert.True(Ground(out _,out actual)); Assert.AreSame(floor,actual);
            render.ParentEntity=other; other.GetPart<PhysicsPart>().InInventory=new Entity(); Assert.True(Ground(out _,out actual)); Assert.AreSame(floor,actual);
            other.GetPart<PhysicsPart>().InInventory=null; Assert.False(Ground(out _,out _)); Assert.True(zone.RemoveEntity(other)); Native(floor);
        }
        [TestCase(false)] [TestCase(true)]
        public void MissingOrWrongSizedAtlasDoesNotReloadOrSuppressRealOwners(bool wrongSize)
        {
            var floor = Add(Floor); var owner = Add(Bench); Native(owner); Texture2D bad = wrongSize ? new Texture2D(16,24) : null;
            try { Inject(bad); Fallback(); Assert.False(Subject(owner)); Assert.False(Ground(out _,out _)); Assert.False(View().LocalGround); Assert.AreSame(floor,zone.GetCell(X,Y).Objects.Single(e=>e.BlueprintName==Floor)); }
            finally { world.Dispose(); if (bad != null) Object.DestroyImmediate(bad); }
        }
        [Test] public void CachedEightCellAtlasOwnsOnlyTilesAndSpritesAndRequiresSelector()
        {
            var atlas=Resources.Load<Texture2D>(Atlas); Assert.NotNull(atlas); Assert.AreEqual(128,atlas.width); Assert.AreEqual(24,atlas.height); Assert.AreEqual(FilterMode.Point,atlas.filterMode); Assert.AreEqual(1,atlas.mipmapCount);
            Inject(atlas); var owner=Add(Bench); Native(owner); var tile=View().ForegroundTile; var sprite=tile.sprite;
            for(int n=0;n<5;n++){Native(owner);Assert.AreSame(tile,View().ForegroundTile);}
            world.Dispose();world.Dispose();Assert.True(tile==null);Assert.True(sprite==null);Assert.True(atlas!=null);
            Assert.True(injectedSelector.TryResolve(zone,X,Y,out var fallback));Assert.AreSame(owner,fallback.Owner);
            var noSelector=(IDisposable)Activator.CreateInstance(AdapterType(),new object[]{atlas,null});
            try { object[] args={zone,X,Y,owner,null};Assert.False((bool)noSelector.GetType().GetMethod("TryResolveForeground",I).Invoke(noSelector,args)); }
            finally { noSelector.Dispose();noSelector.Dispose(); } Assert.True(atlas!=null);
        }
        [Test] public void SubmittedDestructionClearsBodyRetainsFloorAndUsesDirtyPass()
        {
            var floor=Add(Floor);var owner=Add(Growth);
            using(var paint=new Paint(world,zone))
            {
                paint.Step();NativeTile(paint.At(X,Y));NativeTile(paint.GroundAt(X,Y));long full=EnvironmentSpriteRenderer.Perf.FullPasses;
                var previous=DestructionSystem.EntityFactoryRef;
                try { DestructionSystem.EntityFactoryRef=content.Factory;Assert.AreEqual(DestroyVerdict.Destroyed,DestructionSystem.Damage(owner,999,null,zone)); }
                finally { DestructionSystem.EntityFactoryRef=previous; }
                Assert.IsNull(zone.GetEntityCell(owner));paint.Step();Assert.AreSame(floor,View().Owner);Assert.IsNull(paint.At(X,Y));NativeTile(paint.GroundAt(X,Y));Assert.AreEqual(full,EnvironmentSpriteRenderer.Perf.FullPasses);
            }
        }
        [Test] public void SubmittedCoveredFloorKeepsPlayerAndActualCropInsteadOfSuppressingThem()
        {
            var floor=Add(Floor);var actor=Add("Player");actor.GetPart<RenderPart>().RenderLayer=100;
            using(var paint=new Paint(world,zone))
            {
                paint.Step();NativeTile(paint.GroundAt(X,Y));Assert.NotNull(paint.At(X,Y));Assert.AreSame(actor,View().Owner);long full=EnvironmentSpriteRenderer.Perf.FullPasses;
                Assert.True(zone.MoveEntity(actor,X-2,Y));var crop=Add("HollowchimeCrop");floor.AddPart(new CultivatedSoilPart());floor.SetTag("Plantable");paint.Dirty(X,Y);paint.Dirty(X-2,Y);paint.Step();
                Assert.AreSame(crop,View().Owner);Assert.False(View().LocalGround);Assert.NotNull(paint.At(X,Y));Assert.AreNotSame(Resources.Load<Texture2D>(Atlas),paint.GroundAt(X,Y)?.sprite?.texture);
                Assert.AreEqual(full,EnvironmentSpriteRenderer.Perf.FullPasses);
            }
        }
        [Test] public void SubmittedIndependentChargeAndFogRemainTruthful()
        {
            Add(Floor);Add(Growth);
            using(var paint=new Paint(world,zone))
            {
                paint.Step();NativeTile(paint.At(X,Y));NativeTile(paint.GroundAt(X,Y));zone.TileState.AddCharge(X,Y,1);paint.Step();
                Assert.False(View().LocalGround);NativeTile(paint.At(X,Y));Assert.AreSame(CP437TilesetGenerator.GetTile('*'),paint.Mark);Assert.AreEqual(1,zone.TileState.Charge(X,Y));
                zone.GetCell(X,Y).IsVisible=false;paint.Dirty(X,Y);paint.Step();Assert.False(View().NativeForeground);Assert.IsNull(paint.Mark);
                zone.GetCell(X,Y).IsVisible=true;paint.Dirty(X,Y);paint.Step();NativeTile(paint.At(X,Y));Assert.AreSame(CP437TilesetGenerator.GetTile('*'),paint.Mark);
            }
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
                    saved=new HotbarSaveFixture();root=new GameObject("Overwrit submitted grid",typeof(Grid));saved.Root.transform.SetParent(root.transform,false);
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
            public void Dirty(int x,int y)=>Renderer.MarkCellDirty(x,y,"Overwrit current material");
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
