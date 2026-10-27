// Native current-owner, finite-stock and submitted-render contracts for Grovelands.
// New production is intentionally absent at the first native RED run.
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
    public sealed class FlatPixelGrovelandsMaterialsTests
    {
        const string Home = "Overworld.4.7.0";
        const string Atlas = "Sprites/Live16x24/grovelands-materials";
        const string Sill = "Sprites/Live16x24/sill-envelope";
        const string Vegetation = "Sprites/Live16x24/north-vegetation";
        const string Opening = "Sprites/Live16x24/opening-base";
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
            previousLiquids = new Dictionary<string, LiquidDefinition>(liquids); previouslyInitialized = LiquidRegistry.IsInitialized;
            LiquidRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/LiquidDefinitions").Select(t => t.text));
            Assert.NotNull(LiquidRegistry.Get("water"));
            previousHarvestFactory = HarvestablePart.Factory; content = new DensityLootTestScope(); HarvestablePart.Factory = content.Factory;
            manager = OverworldZoneManager.CreateDetached(content.Factory, 65, true);
            Assert.AreEqual(BiomeType.Grovelands, manager.WorldMap.GetBiome(4, 7)); Assert.IsNull(manager.WorldMap.GetPOI(4, 7));
            Assert.True(GrovelandsCompositionPlan.IsWildernessZone(Home)); zone = Attach(Home); world = new FlatPixelGreyboxCatalog();
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
        Entity Add(string name, int x = X, int y = Y)
        { var e = content.Factory.CreateEntity(name); Assert.NotNull(e, name); Assert.True(zone.AddEntity(e, x, y)); return e; }
        FlatPixelGreyboxCell View(int x = X, int y = Y)
        { Assert.True(world.TryResolvePresentation(zone, x, y, out var value)); return value; }
        static Type AdapterType()
        { var type = typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelGrovelandsMaterialsCatalog"); Assert.NotNull(type, "The Grovelands adapter is not implemented."); return type; }
        bool Supported(Zone graph) => (bool)AdapterType().GetMethod("IsSupportedZone", S).Invoke(null, new object[] { graph });
        static void Write(object owner, string name, object value)
        { var field = owner.GetType().GetField(name, I); Assert.NotNull(field, name); field.SetValue(owner, value); }
        static object Read(object owner, string name)
        { var field = owner.GetType().GetField(name, I); Assert.NotNull(field, name); return field.GetValue(owner); }
        object Adapter()
        {
            var value = Read(world, "_grovelandsMaterialsArt");
            if (value == null) { View(); value = Read(world, "_grovelandsMaterialsArt"); }
            Assert.NotNull(value); return value;
        }
        bool Ground(out Tile tile, out Entity floor)
        { var adapter = Adapter(); object[] args = { zone, X, Y, null, null }; bool ok = (bool)adapter.GetType().GetMethod("TryResolveGround", I).Invoke(adapter, args); tile = (Tile)args[3]; floor = (Entity)args[4]; return ok; }
        bool Subject(Entity owner, int x = X, int y = Y)
        { var adapter = Adapter(); object[] args = { zone, owner, x, y, null }; return (bool)adapter.GetType().GetMethod("TryGetSubjectSprite", I).Invoke(adapter, args); }
        static string ResourceFor(string name) => name == "Grass" || name == "VineWall" ? Sill : name == "Tree" ? Vegetation : name == "Bush" ? Opening : Atlas;
        static void NativeTile(Tile tile, string resource)
        {
            var texture = Resources.Load<Texture2D>(resource); Assert.NotNull(texture, resource); Assert.NotNull(tile); Assert.NotNull(tile.sprite);
            Assert.AreSame(texture, tile.sprite.texture); Assert.AreEqual(new Vector2(16, 24), tile.sprite.rect.size);
            Assert.AreEqual(new Vector2(8, 12), tile.sprite.pivot); Assert.AreEqual(16, tile.sprite.pixelsPerUnit);
            Assert.AreEqual(TileFlags.None, tile.flags); Assert.AreEqual(Tile.ColliderType.None, tile.colliderType);
        }
        static void FamilyFrame(Entity owner, Tile tile)
        {
            int first = -1, last = -1;
            switch (owner.BlueprintName)
            {
                case "MycelialColumn": first = 0; last = 1; break;
                case "GroveSeep": first = last = owner.GetPart<LiquidPoolPart>().Volume > 0 ? 2 : 3; break;
                case "GroveSign": first = last = 4; break;
                case "GroveRedGrowth": first = last = 5; break;
                case "FruitingBody": first = 6; last = 7; break;
                case "CompostRow": first = 8; last = 10; break;
                case "CompostCache": first = last = 11; break;
                case "ChoirIronVein": first = last = 12; break;
                case "WineLeafSundew": first = last = 13; break;
                case "Grass": first = 20; last = 23; break;
                case "VineWall": first = 0; last = 15; break;
                case "Tree": first = last = 0; break;
                case "Bush": first = last = 5; break;
            }
            Assert.That((int)tile.sprite.rect.x / 16, Is.InRange(first, last), owner.BlueprintName);
        }
        void Native(Entity owner, int x = X, int y = Y)
        {
            var value = View(x, y); Assert.AreSame(owner, value.Owner);
            bool ground = owner.BlueprintName == "Grass"; var tile = ground ? value.GroundTile : value.ForegroundTile;
            if (ground) { Assert.True(value.LocalGround); Assert.AreEqual(Color.white, value.GroundColor); }
            else { Assert.True(value.NativeForeground); Assert.AreEqual(Color.white, value.ForegroundColor); }
            NativeTile(tile, ResourceFor(owner.BlueprintName)); FamilyFrame(owner, tile);
            Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone), "This material adapter must not expand opening admission.");
            Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, x, y, out var sprite, out var tint));
            Assert.AreSame(tile.sprite.texture, sprite.texture); Assert.AreEqual(Color.white, tint);
            if (owner.BlueprintName == "VineWall") Assert.AreEqual(0, sprite.rect.x, "Portrait is an isolated owner, not disclosed neighbor geometry.");
            else Assert.AreEqual(tile.sprite.rect, sprite.rect);
        }
        void Fallback(int x = X, int y = Y)
        {
            Assert.True(world.TryResolveFallback(zone, x, y, out var fallback)); var value = View(x, y);
            Assert.AreSame(fallback.Owner, value.Owner); Assert.False(value.NativeForeground); Assert.AreSame(fallback.ForegroundTile, value.ForegroundTile);
        }
        void Inject(Texture2D main, string missing = null)
        {
            world.Dispose(); world = new FlatPixelGreyboxCatalog((Texture2D)null); injectedSelector = new FlatPixelFallbackCatalog();
            injectedAdapter = (IDisposable)Activator.CreateInstance(AdapterType(), new object[] { main,
                missing == "sill" ? null : Resources.Load<Texture2D>(Sill), missing == "vegetation" ? null : Resources.Load<Texture2D>(Vegetation),
                missing == "opening" ? null : Resources.Load<Texture2D>(Opening), injectedSelector });
            Write(world, "_grovelandsMaterialsArt", injectedAdapter);
        }
        static bool HasLedger(OverworldZoneManager owner)
        { var field = typeof(LairStacks).GetField("Ledgers", S); Assert.NotNull(field); return ((System.Runtime.CompilerServices.ConditionalWeakTable<OverworldZoneManager, LairStackLedgerPart>)field.GetValue(null)).TryGetValue(owner, out _); }
        bool Act(Entity owner, Entity actor, string command, System.Random random = null)
        {
            var action = GameEvent.New("InventoryAction");
            try { action.SetParameter("Actor", actor); action.SetParameter("Zone", zone); action.SetParameter("Command", command); if (random != null) action.SetParameter("Random", random); owner.FireEvent(action); return action.Handled; }
            finally { action.Release(); }
        }
        sealed class FixedRoll : System.Random
        { readonly int roll; public FixedRoll(int value) { roll = value; } public override int Next(int maxValue) => roll % maxValue; public override int Next(int minValue, int maxValue) => minValue + roll % (maxValue - minValue); }

        [TestCase("MycelialColumn")] [TestCase("GroveSeep")] [TestCase("GroveSign")]
        [TestCase("GroveRedGrowth")] [TestCase("FruitingBody")] [TestCase("CompostRow")]
        [TestCase("CompostCache")] [TestCase("ChoirIronVein")] [TestCase("WineLeafSundew")]
        [TestCase("Grass")] [TestCase("VineWall")] [TestCase("Tree")] [TestCase("Bush")]
        public void ExactFactoryOwnerUsesItsOwnBodyAndPublicReaderButReskinDoesNot(string name)
        {
            var owner = Add(name); string before = zone.TileState.ToSaveString(); int version = zone.EntityVersion; Native(owner);
            Assert.AreEqual(owner.GetPart<PhysicsPart>().Solid, View().BlocksMovement);
            Assert.AreEqual(before, zone.TileState.ToSaveString()); Assert.AreEqual(version, zone.EntityVersion);
            owner.GetPart<RenderPart>().VisualVariant = "custom current identity";
            if (name == "Grass") Assert.False(View().LocalGround); else Fallback();
            Assert.False(Subject(owner)); owner.GetPart<RenderPart>().VisualVariant = ""; Native(owner);
        }
        [TestCase(Formation.Grove, "Overworld.4.7.0", "GroveSeep")]
        [TestCase(Formation.CompostingField, "Overworld.4.8.0", "CompostRow")]
        [TestCase(Formation.FruitingWall, "Overworld.5.7.0", "FruitingBody")]
        [TestCase(Formation.TendrilFen, "Overworld.0.8.0", "FruitingBody")]
        public void FourNaturalFormationGraphsSupplyOwnersWithoutForcingAPlan(Formation formation, string id, string signature)
        {
            manager = OverworldZoneManager.CreateDetached(content.Factory, 65, true); var point = WorldMap.FromZoneID(id);
            Assert.True(GrovelandsCompositionPlan.IsWildernessZone(id)); Assert.AreEqual(BiomeType.Grovelands, manager.WorldMap.GetBiome(point.x, point.y));
            Assert.IsNull(manager.WorldMap.GetPOI(point.x, point.y)); Assert.AreEqual(formation, FormationSelector.For(BiomeType.Grovelands, id));
            zone = manager.GetZone(id); manager.SetActiveZone(zone); Reveal(zone); Assert.True(Supported(zone));
            var owner = zone.GetReadOnlyEntities().FirstOrDefault(e => e.BlueprintName == signature && zone.GetEntityCell(e) is Cell at
                && world.TryResolveFallback(zone, at.X, at.Y, out var picked) && ReferenceEquals(picked.Owner, e) && !picked.Remembered);
            Assert.NotNull(owner, "Actual formation must expose its declared signature owner."); var cell = zone.GetEntityCell(owner);
            int version = zone.EntityVersion; string state = zone.TileState.ToSaveString(); Native(owner, cell.X, cell.Y);
            Assert.AreEqual(version, zone.EntityVersion); Assert.AreEqual(state, zone.TileState.ToSaveString());
            owner.GetPart<RenderPart>().VisualID = "different owner"; Fallback(cell.X, cell.Y);
        }
        [TestCase("biome")] [TestCase("poi")] [TestCase("map-shape")] [TestCase("poi-shape")]
        public void CurrentWorldAuthorityCanRevokeWarmAdmission(string fault)
        {
            var owner = Add("MycelialColumn"); Native(owner); var map = manager.WorldMap; var tiles = map.Tiles; var pois = map.POIs;
            var biome = tiles[4,7]; var poi = pois[4,7];
            try
            {
                Assert.True(Supported(zone));
                if (fault == "biome") tiles[4,7] = BiomeType.Sodden;
                if (fault == "poi") map.SetPOI(4,7,new PointOfInterest(POIType.MerchantCamp,"current saved camp"));
                if (fault == "map-shape") map.Tiles = new BiomeType[1,1];
                if (fault == "poi-shape") map.POIs = new PointOfInterest[1,1];
                Assert.False(Supported(zone)); Fallback(); Assert.False(Subject(owner));
            }
            finally { map.Tiles = tiles; map.POIs = pois; tiles[4,7] = biome; pois[4,7] = poi; }
            Native(owner);
        }
        [TestCase("Overworld.3.6.0")] [TestCase("Overworld.6.6.0")] [TestCase("Overworld.5.4.0")]
        [TestCase("Overworld.1.6.0")] [TestCase("Overworld.4.7.1")] [TestCase("Overworld.4.07.0")]
        public void ErasingLabelsCannotAdmitAuthoredSitesDollGroveDepthOrAliases(string id)
        {
            Native(Add("MycelialColumn")); var point = WorldMap.FromZoneID(id); var map = manager.WorldMap; var biome = map.Tiles[point.x,point.y]; var poi = map.POIs[point.x,point.y];
            try { map.Tiles[point.x,point.y] = BiomeType.Grovelands; map.POIs[point.x,point.y] = null; zone = Attach(id); Add("MycelialColumn"); Assert.False(Supported(zone)); Fallback(); }
            finally { map.Tiles[point.x,point.y] = biome; map.POIs[point.x,point.y] = poi; }
        }
        [TestCase(false)] [TestCase(true)]
        public void SavedModernAndLegacyLairsRemainOutsideAfterLabelsAreErased(bool legacy)
        {
            Native(Add("MycelialColumn")); manager = OverworldZoneManager.CreateDetached(content.Factory,65,true);
            manager.WorldMap.SetPOI(4,7,new PointOfInterest(POIType.Lair,"saved lair",null,1,"DesertProwler"));
            if (legacy) LairStacks.Restore(manager,null);
            zone = manager.GetZone(Home); manager.SetActiveZone(zone); Reveal(zone); var claim = LairStacks.Inspect(manager,Home); Assert.NotNull(claim); Assert.AreEqual(legacy,claim.Legacy);
            manager.WorldMap.SetPOI(4,7,null); Assert.False(Supported(zone)); Assert.True((bool)typeof(LairStacks).GetMethod("HasSavedColumn",S).Invoke(null,new object[]{manager,Home}));
        }
        [Test] public void QueriesAreReadOnlyAndRequireTheExactCurrentGraph()
        {
            var owner = Add("MycelialColumn"); Assert.False(HasLedger(manager)); int count = manager.CachedZoneCount, version = zone.EntityVersion;
            var members = zone.GetReadOnlyEntities().ToArray(); string state = zone.TileState.ToSaveString();
            for (int i = 0; i < 8; i++) { Native(owner); Assert.True(Supported(zone)); }
            Assert.False(HasLedger(manager)); Assert.AreEqual(count,manager.CachedZoneCount); Assert.AreEqual(version,zone.EntityVersion);
            CollectionAssert.AreEquivalent(members,zone.GetReadOnlyEntities()); Assert.AreEqual(state,zone.TileState.ToSaveString());
            var prior = zone; manager.SetActiveZone(new Zone(Home)); Assert.False(Supported(prior)); Assert.False(world.TryResolvePresentation(prior,X,Y,out _));
            zone = new Zone(Home); Reveal(zone); Add("MycelialColumn"); Assert.IsNull(WorldLocationContext.For(zone)); Assert.False(Supported(zone)); Fallback();
            manager.SetActiveZone(prior); zone = prior; Native(owner);
        }
        [TestCase("hidden")] [TestCase("remembered")] [TestCase("unexplored")]
        public void CurrentBodyAndReaderDoNotDiscloseHiddenOrRememberedOwners(string fault)
        {
            var owner = Add("MycelialColumn"); Native(owner);
            if (fault == "hidden") owner.GetPart<RenderPart>().Visible = false;
            else { zone.GetCell(X,Y).IsVisible = false; zone.GetCell(X,Y).Explored = fault == "remembered"; }
            Assert.False(Subject(owner));
            if (fault == "unexplored") Assert.False(world.TryResolvePresentation(zone,X,Y,out _)); else Assert.False(View().NativeForeground);
            owner.GetPart<RenderPart>().Visible = true; zone.GetCell(X,Y).Explored = zone.GetCell(X,Y).IsVisible = true; Native(owner);
        }
        [Test] public void CoverRemovedOwnerAndAlteredPhysicalBodyCannotRetainPortrait()
        {
            var owner = Add("MycelialColumn"); owner.AddPart(new SpatialFootprintPart{CellsRaw="0,0"}); Native(owner);
            var actor = Add("Player"); actor.GetPart<RenderPart>().RenderLayer = 100; Assert.AreSame(actor,View().Owner); Assert.False(Subject(owner));
            Assert.True(zone.MoveEntity(actor,X-2,Y)); Native(owner);
            Assert.True(zone.TryChangeFootprint(owner,"0,0;1,0")); Assert.False(Subject(owner)); Fallback();
            Assert.True(zone.TryChangeFootprint(owner,"0,0")); Native(owner);
            owner.GetPart<SpatialFootprintPart>().CellsRaw="0,0;0,1"; Assert.False(Subject(owner)); owner.GetPart<SpatialFootprintPart>().CellsRaw="0,0"; Native(owner);
            Assert.True(zone.RemoveEntity(owner)); Assert.False(Subject(owner)); Assert.AreNotSame(owner,View().Owner);
        }
        [TestCase("MycelialColumn","foreign-render")] [TestCase("GroveSign","foreign-physics")]
        [TestCase("FruitingBody","duplicate-material")] [TestCase("CompostRow","duplicate-render")]
        [TestCase("CompostCache","carried")] [TestCase("GroveRedGrowth","takeable")]
        [TestCase("ChoirIronVein","foreign-harvest")] [TestCase("FruitingBody","wrong-blueprint")]
        public void MalformedPartsCustodyAndIdentityCannotBorrowApprovedPixels(string name,string fault)
        {
            var owner = Add(name); Native(owner); var foreign = new Entity();
            switch(fault)
            {
                case "foreign-render": owner.GetPart<RenderPart>().ParentEntity=foreign; break;
                case "foreign-physics": owner.GetPart<PhysicsPart>().ParentEntity=foreign; break;
                case "duplicate-material": owner.AddPart(new MaterialPart{MaterialID="Plant"}); break;
                case "duplicate-render": owner.AddPart(new RenderPart{RenderString="%"}); break;
                case "carried": owner.GetPart<PhysicsPart>().InInventory=foreign; break;
                case "takeable": owner.GetPart<PhysicsPart>().Takeable=true; break;
                case "foreign-harvest": owner.GetPart<HarvestablePart>().ParentEntity=foreign; break;
                case "wrong-blueprint": owner.BlueprintName="BloomingFruitingBody"; break;
            }
            Assert.False(Subject(owner)); if(View().Owner==owner) Assert.False(View().NativeForeground);
        }
        [TestCase("MycelialColumn","foreign-light")] [TestCase("MycelialColumn","missing-light")]
        [TestCase("FruitingBody","zero-hp")] [TestCase("GroveSign","regional-sign")]
        [TestCase("GroveRedGrowth","wrong-yield")] [TestCase("CompostCache","spent")]
        [TestCase("WineLeafSundew","consumed-trigger")] [TestCase("WineLeafSundew","foreign-trigger")]
        public void EssentialMeaningIsNotInferredFromTheBlueprintNameAlone(string name,string fault)
        {
            var owner=Add(name); Native(owner);
            switch(fault)
            {
                case "foreign-light": owner.GetPart<LightSourcePart>().ParentEntity=new Entity(); break;
                case "missing-light": owner.RemovePart(owner.GetPart<LightSourcePart>()); break;
                case "zero-hp": owner.GetPart<DestructiblePart>().HP=0; break;
                case "regional-sign": owner.AddPart(new RegionalSignpostPart()); break;
                case "wrong-yield": owner.GetPart<HarvestablePart>().YieldBlueprint="Mushroom"; break;
                case "spent": owner.GetPart<HarvestablePart>().Harvested=true; break;
                case "consumed-trigger": owner.GetPart<GreatdewSnarePart>().ConsumeOnTrigger=true; break;
                case "foreign-trigger": owner.GetPart<GreatdewSnarePart>().ParentEntity=new Entity(); break;
            }
            Fallback(); Assert.False(Subject(owner));
        }
        [TestCase("MycelialColumn","wet")] [TestCase("Tree","burning")] [TestCase("FruitingBody","nan")]
        public void NeutralBodyCannotEraseActiveTintOrInvalidThermalState(string name,string state)
        {
            var owner=Add(name); Native(owner);
            if(state=="wet") Assert.True(owner.ForceApplyEffect(new WetEffect(),null,zone));
            if(state=="burning") Assert.True(owner.ForceApplyEffect(new BurningEffect(),null,zone));
            if(state=="nan") owner.GetPart<ThermalPart>().Temperature=float.NaN;
            Fallback(); Assert.False(Subject(owner));
        }
        [TestCase("MycelialColumn")] [TestCase("FruitingBody")] [TestCase("VineWall")] [TestCase("Bush")]
        [TestCase("CompostRow")] [TestCase("WineLeafSundew")] [TestCase("GroveRedGrowth")]
        public void ExactDryForegroundKeepsOnlyItsExistingGrassSubstrate(string name)
        {
            var grass=Add("Grass"); var owner=Add(name); Native(owner); Assert.True(Ground(out var tile,out var floor)); Assert.AreSame(grass,floor); NativeTile(tile,Sill);
            Assert.True(zone.RemoveEntity(grass)); Assert.False(Ground(out _,out _)); Assert.False(View().LocalGround); Native(owner);
            grass=Add("Grass"); Assert.True(Ground(out _,out floor)); Assert.AreSame(grass,floor);
        }
        [TestCase("coating")] [TestCase("cloud")] [TestCase("heat")] [TestCase("unknown-terrain")] [TestCase("cultivation")]
        public void GreenGroundCannotEraseOtherCurrentTerrainOrIndependentState(string fault)
        {
            var grass=Add("Grass"); Native(grass); var actor=Add("Player");actor.GetPart<RenderPart>().RenderLayer=100; Assert.True(Ground(out _,out var floor));Assert.AreSame(grass,floor);
            if(fault=="coating")zone.TileState.WriteCoating(X,Y,"water",3);
            if(fault=="cloud")zone.TileState.WriteCloud(X,Y,"veil-mist",3);
            if(fault=="heat")zone.TileState.AddHeat(X,Y,1);
            if(fault=="unknown-terrain")Add("StoneFloor");
            if(fault=="cultivation")grass.AddPart(new CultivatedSoilPart());
            Assert.False(Ground(out _,out _));Assert.False(View().LocalGround);Assert.AreSame(actor,View().Owner);
        }
        [Test] public void HiddenForeignOrCustodiedGroundConflictCannotEraseDisclosedGrass()
        {
            var grass=Add("Grass");Native(grass);var original=View().GroundTile;var other=Add("StoneFloor");var render=other.GetPart<RenderPart>();render.Visible=false;
            Assert.True(Ground(out var tile,out var floor));Assert.AreSame(grass,floor);Assert.AreSame(original,tile);
            render.Visible=true;Assert.False(Ground(out _,out _));render.ParentEntity=new Entity();Assert.True(Ground(out _,out floor));Assert.AreSame(grass,floor);
            render.ParentEntity=other;other.GetPart<PhysicsPart>().InInventory=new Entity();Assert.True(Ground(out _,out floor));Assert.AreSame(grass,floor);
        }
        [Test] public void ActualFinalWellDrawExhaustsStockWithoutErasingPermanentWater()
        {
            var owner=Add("GroveSeep");var pool=owner.GetPart<LiquidPoolPart>();Assert.IsNull(owner.GetPart<TileStateSourcePart>());
            Assert.IsNull(owner.GetPart<MaterialPart>());Assert.IsNull(owner.GetPart<ThermalPart>());Assert.True(zone.TileState.HasCoating(X,Y,"water"));
            Assert.AreEqual(ZoneTileState.Permanent,zone.TileState.CoatingTurns(X,Y,"water"));pool.Volume=1;Native(owner);var filled=View().ForegroundTile;
            var actor=Add("Player",X-1,Y);Assert.True(Act(owner,actor,"DrawWaterAtWell"));Assert.AreEqual(0,pool.Volume);
            Assert.True(zone.TileState.HasCoating(X,Y,"water"));Assert.AreEqual(ZoneTileState.Permanent,zone.TileState.CoatingTurns(X,Y,"water"));
            Native(owner);Assert.AreNotSame(filled,View().ForegroundTile);Assert.False(Act(owner,actor,"DrawWaterAtWell"));Assert.AreEqual(0,pool.Volume);
            pool.Volume=7;Native(owner);Assert.AreSame(filled,View().ForegroundTile,"A usable current volume does not have to equal authored40.");
            Assert.True(zone.RemoveEntity(owner));Assert.False(zone.TileState.HasCoating(X,Y,"water"));Assert.False(Subject(owner));
        }
        [TestCase("oil")] [TestCase("ice")] [TestCase("missing-projection")] [TestCase("wrong-liquid")]
        [TestCase("negative-stock")] [TestCase("duplicate-pool")] [TestCase("foreign-well")] [TestCase("broken-well")]
        public void FiniteSeepDoesNotUseCleanBodyForUnsupportedMaterialOrService(string fault)
        {
            var owner=Add("GroveSeep");Native(owner);
            if(fault=="oil"||fault=="ice")zone.TileState.WriteCoating(X,Y,fault,3);
            if(fault=="missing-projection")zone.TileState.Clear(X,Y);
            if(fault=="wrong-liquid")owner.GetPart<LiquidPoolPart>().LiquidId="brine";
            if(fault=="negative-stock")owner.GetPart<LiquidPoolPart>().Volume=-1;
            if(fault=="duplicate-pool")owner.AddPart(new LiquidPoolPart{LiquidId="water",Volume=40});
            if(fault=="foreign-well")owner.GetPart<WellPart>().ParentEntity=new Entity();
            if(fault=="broken-well"){owner.AddPart(new RepairablePart());Assert.False(owner.GetPart<WellPart>().IsUsable);}
            Fallback();Assert.False(Subject(owner));Assert.False(View().LocalGround);
        }
        [TestCase("GroveRedGrowth","GroveRed")] [TestCase("ChoirIronVein","ChoirIron")]
        public void ActualHarvestConsumesSourceAndProducesDistinctPortableProduct(string name,string product)
        {
            Add("Grass");var owner=Add(name);Native(owner);var harvest=owner.GetPart<HarvestablePart>();Assert.AreEqual(product,harvest.YieldBlueprint);
            harvest.Harvested=true;Fallback();harvest.Harvested=false;Native(owner);var actor=Add("Player",X-1,Y);
            Assert.True(Act(owner,actor,"Harvest",new FixedRoll(0)));Assert.True(harvest.Harvested);Assert.IsNull(zone.GetEntityCell(owner));Assert.False(Subject(owner));
            Assert.True(DensityLootTestScope.Gear(actor).Any(e=>e.BlueprintName==product)||zone.GetCell(X,Y).Objects.Any(e=>e.BlueprintName==product));
        }
        [TestCase(false)] [TestCase(true)]
        public void ActualCacheRollConsumesOnlyCacheAndRevealsExistingNativeRow(bool succeeds)
        {
            Add("Grass");var row=Add("CompostRow");var cache=Add("CompostCache");Assert.AreEqual(row.GetPart<RenderPart>().RenderLayer,cache.GetPart<RenderPart>().RenderLayer);
            Native(cache);Assert.False(Subject(row));var actor=Add("Player",X-1,Y);
            int before=DensityLootTestScope.Gear(actor).Where(e=>e.BlueprintName=="GoldCoin").Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);
            Assert.True(Act(cache,actor,"Harvest",new FixedRoll(succeeds?0:99)));Assert.True(cache.GetPart<HarvestablePart>().Harvested);Assert.IsNull(zone.GetEntityCell(cache));
            Assert.AreSame(zone.GetCell(X,Y),zone.GetEntityCell(row));Native(row);Assert.False(Subject(cache));
            int after=DensityLootTestScope.Gear(actor).Where(e=>e.BlueprintName=="GoldCoin").Sum(e=>e.GetPart<StackerPart>()?.StackCount??1)
                +zone.GetCell(X,Y).Objects.Where(e=>e.BlueprintName=="GoldCoin").Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);
            Assert.AreEqual(before+(succeeds?1:0),after,"A valid failed yield spends cache without inventing coins.");
        }
        [Test] public void ReversedEqualLayerRowCacheOrderKeepsActualSelectedOwner()
        {
            var cache=Add("CompostCache");var row=Add("CompostRow");Native(row);Assert.False(Subject(cache));
            Assert.True(zone.RemoveEntity(row));Native(cache);
        }
        [Test] public void VineMaskUsesOnlyDisclosedSelectedNeighborsAndDirtyRemovalRepaintsJoin()
        {
            var wall=Add("VineWall");var east=Add("VineWall",X+1,Y);Native(wall);Assert.AreEqual(2,View().ForegroundTile.sprite.rect.x/16);
            east.GetPart<RenderPart>().Visible=false;Native(wall);Assert.AreEqual(0,View().ForegroundTile.sprite.rect.x/16);
            east.GetPart<RenderPart>().Visible=true;zone.GetCell(X+1,Y).IsVisible=false;Native(wall);Assert.AreEqual(0,View().ForegroundTile.sprite.rect.x/16);
            zone.GetCell(X+1,Y).IsVisible=true;Native(wall);Assert.AreEqual(2,View().ForegroundTile.sprite.rect.x/16);
            using(var paint=new Paint(world,zone))
            {
                paint.Step();Assert.AreEqual(2,paint.At(X,Y).sprite.rect.x/16);long full=EnvironmentSpriteRenderer.Perf.FullPasses;
                Assert.True(zone.RemoveEntity(east));paint.Dirty(X+1,Y);paint.Step();Assert.AreEqual(0,paint.At(X,Y).sprite.rect.x/16);
                Assert.AreEqual(full,EnvironmentSpriteRenderer.Perf.FullPasses,"Neighbor joins update through actual incremental path.");
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void MissingOrMalformedMainDoesNotReloadOrDisableUnchangedForestGroups(bool malformed)
        {
            Texture2D bad=malformed?new Texture2D(17,24):null;
            try
            {
                var owner=Add("MycelialColumn");Native(owner);Inject(bad);Fallback();Assert.False(Subject(owner));Assert.True(zone.RemoveEntity(owner));
                foreach(var name in new[]{"Grass","VineWall","Tree","Bush"}){var borrowed=Add(name);Native(borrowed);Assert.True(zone.RemoveEntity(borrowed));}
            }
            finally{world.Dispose();if(bad!=null)Object.DestroyImmediate(bad);}
        }
        [TestCase("sill","VineWall")] [TestCase("vegetation","Tree")] [TestCase("opening","Bush")]
        public void MissingBorrowedGroupDoesNotDisableMainAndOwnsNoSourceTexture(string missing,string name)
        {
            var source=Resources.Load<Texture2D>(Atlas);Assert.NotNull(source);Inject(source,missing);var borrowed=Add(name);Fallback();Assert.False(Subject(borrowed));Assert.True(zone.RemoveEntity(borrowed));
            var owner=Add("MycelialColumn");Native(owner);var tile=View().ForegroundTile;var sprite=tile.sprite;world.Dispose();world.Dispose();
            Assert.True(tile==null);Assert.True(sprite==null);Assert.True(source!=null);Assert.True(injectedSelector.TryResolve(zone,X,Y,out var old));Assert.AreSame(owner,old.Owner);
        }
        [Test] public void NativeAtlasAndCachedBodyPreserveCurrentIndependentLight()
        {
            var atlas=Resources.Load<Texture2D>(Atlas);Assert.NotNull(atlas);Assert.AreEqual(224,atlas.width);Assert.AreEqual(24,atlas.height);Assert.AreEqual(FilterMode.Point,atlas.filterMode);Assert.AreEqual(1,atlas.mipmapCount);
            var owner=Add("MycelialColumn");var light=owner.GetPart<LightSourcePart>();Native(owner);var tile=View().ForegroundTile;
            for(int i=0;i<8;i++){Native(owner);Assert.AreSame(tile,View().ForegroundTile);}
            Assert.AreEqual(3,light.Radius);Assert.AreEqual(.5f,light.Intensity);Assert.AreEqual("&G",light.LightColor);Assert.True(light.Enabled);
            light.Enabled=false;Native(owner);Assert.AreSame(tile,View().ForegroundTile);Assert.False(light.Enabled,"Static body is not an invented light switch.");
            Assert.AreEqual(14,AdapterType().GetField("FrameCount",S).GetValue(null));
        }
        [TestCase(1,false)] [TestCase(0,false)] [TestCase(0,true)]
        public void SubmittedSeepKeepsItsActualWetOrHeatOverlayAndNoDryGrass(int stock,bool heat)
        {
            Add("Grass");var owner=Add("GroveSeep");owner.GetPart<LiquidPoolPart>().Volume=stock;if(heat)zone.TileState.AddHeat(X,Y,1);
            string before=zone.TileState.ToSaveString();
            using(var paint=new Paint(world,zone))
            {
                paint.Step();NativeTile(paint.At(X,Y),Atlas);FamilyFrame(owner,paint.At(X,Y));Assert.False(View().LocalGround);
                // The existing painter chooses an unrepresented coating before
                // heat. Keeping the real water wave must not erase the heat state.
                Assert.AreSame(CP437TilesetGenerator.GetTile('~'),paint.Mark);Assert.AreEqual(before,zone.TileState.ToSaveString());
                if(heat)
                {
                    Assert.AreEqual(1,zone.TileState.Heat(X,Y));
                    zone.TileState.Clear(X,Y);zone.TileState.AddHeat(X,Y,1);paint.Dirty(X,Y);paint.Step();
                    Fallback();Assert.AreSame(CP437TilesetGenerator.GetTile('^'),paint.Mark,
                        "Without the mandatory water projection the body refuses, and the existing heat mark becomes highest priority.");
                    Assert.AreEqual(1,zone.TileState.Heat(X,Y));Assert.False(zone.TileState.HasCoating(X,Y,"water"));
                }
                zone.GetCell(X,Y).IsVisible=false;paint.Dirty(X,Y);paint.Step();Assert.IsNull(paint.Mark);
            }
        }
        [Test] public void SubmittedColumnDestructionClearsOnlyItsBodyAndRetainsRealGrass()
        {
            var grass=Add("Grass");var owner=Add("MycelialColumn");
            using(var paint=new Paint(world,zone))
            {
                paint.Step();NativeTile(paint.At(X,Y),Atlas);NativeTile(paint.GroundAt(X,Y),Sill);long full=EnvironmentSpriteRenderer.Perf.FullPasses;
                var previous=DestructionSystem.EntityFactoryRef;
                try{DestructionSystem.EntityFactoryRef=content.Factory;Assert.AreEqual(DestroyVerdict.Destroyed,DestructionSystem.Damage(owner,999,null,zone));}
                finally{DestructionSystem.EntityFactoryRef=previous;}
                Assert.IsNull(zone.GetEntityCell(owner));Assert.False(Subject(owner));paint.Step();Assert.AreSame(grass,View().Owner);Assert.IsNull(paint.At(X,Y));NativeTile(paint.GroundAt(X,Y),Sill);
                Assert.AreEqual(full,EnvironmentSpriteRenderer.Perf.FullPasses);
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
                    saved = new HotbarSaveFixture(); root = new GameObject("Grovelands submitted grid", typeof(Grid)); saved.Root.transform.SetParent(root.transform, false);
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
            public void Dirty(int x, int y) => Renderer.MarkCellDirty(x, y, "Grovelands current material");
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
