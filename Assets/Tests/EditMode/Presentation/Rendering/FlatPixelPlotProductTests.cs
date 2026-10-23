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
    /// <summary>Native loose goods follow their physical owner and real use,
    /// independently of the plant, merchant, or reserve that supplied them.</summary>
    public sealed class FlatPixelPlotProductTests
    {
        const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        const string ResourcePath = "Sprites/Live16x24/plot-products";
        static readonly string[] Names = { "CandyCarrot", "CandyCarrotSeed", "EmberwheatSeed", "MarlrootClod",
            "MarlrootSeed", "PitchpodResin", "PitchpodSeed", "InertSludge" };
        EntityFactory factory;
        Texture2D products, originalItems, states, plot, keeper, baseArt;
        FlatPixelOpeningArtCatalog art;
        FlatPixelGreyboxCatalog world;
        Zone zone;
        Action<int, int, string> previousCellDirty;
        Action<string> previousFullDirty;

        [OneTimeSetUp] public void Content() => factory = DensityReferenceGladeTests.Factory();
        [SetUp] public void Setup()
        {
            products = new Texture2D(128, 24); originalItems = new Texture2D(192, 24);
            states = new Texture2D(192, 24); plot = new Texture2D(128, 24);
            keeper = new Texture2D(16, 24); baseArt = new Texture2D(160, 24);
            zone = new Zone(ReferenceGladePlan.ZoneID);
            for (int y = 9; y <= 11; y++) for (int x = 9; x <= 11; x++)
                zone.GetCell(x, y).IsVisible = zone.GetCell(x, y).Explored = true;
            previousCellDirty = ZoneRenderHooks.CellDirtyCallback; previousFullDirty = ZoneRenderHooks.FullDirtyCallback;
            ZoneRenderHooks.CellDirtyCallback = null; ZoneRenderHooks.FullDirtyCallback = null;
        }
        [TearDown] public void Cleanup()
        {
            try
            {
                art?.Dispose(); world?.Dispose(); art = null; world = null;
                foreach (var texture in new[] { products, originalItems, states, plot, keeper, baseArt })
                    if (texture != null) Object.DestroyImmediate(texture);
            }
            finally { ZoneRenderHooks.CellDirtyCallback = previousCellDirty; ZoneRenderHooks.FullDirtyCallback = previousFullDirty; }
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)]
        public void ExactCurrentLooseOwnerUsesOneCachedFullCellFrame(int index)
        {
            Create(); var owner = Add(index); var first = Resolve();
            AssertFrame(first.ForegroundTile, products, index); Assert.AreSame(owner, first.Owner);
            Assert.AreEqual(Color.white, first.ForegroundColor);
            Assert.AreEqual(new Vector2(8, 12), first.ForegroundTile.sprite.pivot);
            Assert.AreEqual(16, first.ForegroundTile.sprite.pixelsPerUnit);
            Assert.AreEqual(Tile.ColliderType.None, first.ForegroundTile.colliderType);
            Assert.AreSame(first.ForegroundTile, Resolve().ForegroundTile);
            Assert.True(zone.RemoveEntity(owner)); Assert.IsNull(Resolve().ForegroundTile);
        }

        [TestCase(0, "Food")] [TestCase(1, "Seed")] [TestCase(2, "Seed")]
        [TestCase(3, "BotanicalProcessing")] [TestCase(4, "Seed")]
        [TestCase(5, "Reagent")] [TestCase(6, "Seed")]
        public void MissingOrForeignFunctionalPartCannotAdvertiseItsKnownProduct(int index, string partName)
        {
            Create(); var owner = Add(index); AssertProduct(owner, index);
            var part = owner.GetPart(partName); Assert.NotNull(part);
            part.ParentEntity = new Entity(); AssertNotProduct(owner);
            part.ParentEntity = owner; AssertProduct(owner, index);
            Assert.True(owner.RemovePart(part)); AssertNotProduct(owner);
            Assert.AreSame(owner, Resolve().Owner, "The physical item keeps an honest fallback.");
            Assert.AreNotSame(products, Resolve().ForegroundTile.sprite.texture);
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)]
        public void ExplicitReskinsAndForeignRolesNeverAliasAPlotProduct(int index)
        {
            Create(); var owner = factory.CreateEntity(Names[index]); AssertProduct(owner, index);
            var render = owner.GetPart<RenderPart>();
            render.VisualID = "another-product"; AssertNotProduct(owner); render.VisualID = "";
            render.VisualVariant = "another-variant"; AssertNotProduct(owner); render.VisualVariant = "";
            render.GlyphVariants = "another-glyph"; AssertNotProduct(owner); render.GlyphVariants = "";
            render.ParentEntity = new Entity(); AssertNotProduct(owner); render.ParentEntity = owner;
            var physics = owner.GetPart<PhysicsPart>();
            physics.ParentEntity = new Entity(); AssertNotProduct(owner); physics.ParentEntity = owner;
            physics.Takeable = false; AssertNotProduct(owner); physics.Takeable = true;
            physics.Solid = true; AssertNotProduct(owner); physics.Solid = false;
            owner.Tags.Remove("Item"); AssertNotProduct(owner); owner.SetTag("Item");
            owner.SetTag("Player"); AssertNotProduct(owner); owner.Tags.Remove("Player");
            owner.SetTag("Creature"); AssertNotProduct(owner); owner.Tags.Remove("Creature");
            owner.SetTag("Terrain"); AssertNotProduct(owner); owner.Tags.Remove("Terrain");
            owner.SetTag("Crop"); AssertNotProduct(owner); owner.Tags.Remove("Crop");
            var crop = new CropPart(); owner.AddPart(crop); AssertNotProduct(owner); owner.RemovePart(crop);
            var stack = owner.GetPart<StackerPart>(); stack.StackCount = 0; AssertNotProduct(owner); stack.StackCount = 1;
            int limit = stack.MaxStack; stack.MaxStack = 0; AssertNotProduct(owner); stack.MaxStack = limit;
            stack.StackCount = stack.MaxStack + 1; AssertNotProduct(owner); stack.StackCount = 1;
            stack.ParentEntity = new Entity(); AssertNotProduct(owner); stack.ParentEntity = owner;
            AssertProduct(owner, index); owner.BlueprintName = "OtherHarvest"; AssertNotProduct(owner);
        }

        [TestCase(1, "CandyCarrotCrop", false)] [TestCase(2, "EmberwheatCrop", false)]
        [TestCase(4, "MarlrootCrop", true)] [TestCase(6, "PitchpodCrop", true)]
        public void SeedArtworkRequiresItsExactCropAndCultivationContract(int index, string crop, bool cultivated)
        {
            Create(); var owner = factory.CreateEntity(Names[index]); var seed = owner.GetPart<SeedPart>();
            Assert.AreEqual(crop, seed.CropBlueprint); Assert.AreEqual(cultivated, seed.RequireCultivatedSoil);
            AssertProduct(owner, index); seed.CropBlueprint = "DrawgourdCrop"; AssertNotProduct(owner);
            seed.CropBlueprint = crop; seed.RequireCultivatedSoil = !cultivated; AssertNotProduct(owner);
            seed.RequireCultivatedSoil = cultivated; AssertProduct(owner, index);
        }

        [TestCase(0)] [TestCase(-1)] [TestCase(9)] [TestCase(int.MaxValue)]
        public void ClodMustHaveABoundedRealFireClayRecipe(int invalidCount)
        {
            Create(); var owner = factory.CreateEntity("MarlrootClod"); var recipe = owner.GetPart<BotanicalProcessingPart>();
            recipe.OutputCount = 1; AssertProduct(owner, 3);
            recipe.OutputCount = 8; AssertProduct(owner, 3);
            recipe.OutputCount = invalidCount; AssertNotProduct(owner);
            recipe.OutputCount = 1; recipe.OutputBlueprint = "SalvagedTimber"; AssertNotProduct(owner);
            recipe.OutputBlueprint = "FireClay"; AssertProduct(owner, 3);
        }

        [TestCase(null)] [TestCase("heat:1")] [TestCase("combustible:1")]
        [TestCase("heat:0,combustible:1")] [TestCase("heat:1,combustible:bad")]
        public void ResinNeedsBothActualPositiveBrewProperties(string invalid)
        {
            Create(); var owner = factory.CreateEntity("PitchpodResin"); var reagent = owner.GetPart<ReagentPart>();
            AssertProduct(owner, 5); reagent.PropertiesRaw = invalid; AssertNotProduct(owner);
            reagent.PropertiesRaw = " COMBUSTIBLE:2 ; Heat:3 "; AssertProduct(owner, 5);
            Assert.False(owner.HasEffect<BurningEffect>(), "A reagent silhouette must not ignite or brew the raw resin.");
            Assert.False(owner.HasPart<BrewItemPart>());
        }

        [Test] public void FoodTuningAndNeutralCompostDoNotInventAdditionalPartsOrReadyStates()
        {
            Create(); var food = factory.CreateEntity("CandyCarrot"); var edible = food.GetPart<FoodPart>();
            edible.Healing = ""; AssertProduct(food, 0); edible.Healing = "1d8"; AssertProduct(food, 0);
            var sludge = factory.CreateEntity("InertSludge");
            Assert.IsNull(sludge.GetPart("Compost")); Assert.IsNull(sludge.GetPart<ReagentPart>());
            AssertProduct(sludge, 7); sludge.GetPart<CommercePart>().Value = 17; AssertProduct(sludge, 7);
            var stack = sludge.GetPart<StackerPart>(); stack.StackCount = 2;
            var before = stack.StackCount; AssertProduct(sludge, 7); Assert.AreEqual(before, stack.StackCount);
        }

        [TestCase("carried")] [TestCase("equipped")] [TestCase("hidden")]
        [TestCase("foreign-render")] [TestCase("foreign-physics")]
        [TestCase("empty-stack")] [TestCase("foreign-stack")] [TestCase("stale-cell")]
        public void InvalidGroundCustodyNeverLeaksEvenAProceduralPhantom(string fault)
        {
            // The existing injection route makes these ownership controls run
            // independently of the new atlas constructor's missing-API RED.
            world = new FlatPixelGreyboxCatalog(baseArt);
            var lower = Add(0); lower.GetPart<RenderPart>().RenderLayer = 2;
            var owner = Add(5); Assert.AreSame(owner, Resolve().Owner);
            if (fault == "carried") owner.GetPart<PhysicsPart>().InInventory = new Entity();
            if (fault == "equipped") owner.GetPart<PhysicsPart>().Equipped = new Entity();
            if (fault == "hidden") owner.GetPart<RenderPart>().Visible = false;
            if (fault == "foreign-render") owner.GetPart<RenderPart>().ParentEntity = new Entity();
            if (fault == "foreign-physics") owner.GetPart<PhysicsPart>().ParentEntity = new Entity();
            if (fault == "empty-stack") owner.GetPart<StackerPart>().StackCount = 0;
            if (fault == "foreign-stack") owner.GetPart<StackerPart>().ParentEntity = new Entity();
            if (fault == "stale-cell")
            {
                var old = zone.GetCell(10, 10); Assert.True(zone.MoveEntity(owner, 11, 10)); old.Objects.Add(owner);
            }
            Assert.AreSame(lower, Resolve().Owner); Assert.True(zone.RemoveEntity(lower)); Assert.IsNull(Resolve().ForegroundTile);
        }

        [Test] public void FogDoesNotRevealCurrentLooseProductsAndEqualLayerRemovalRevealsTheLowerOwner()
        {
            Create(); var clod = Add(3); var seed = Add(4); AssertFrame(Resolve().ForegroundTile, products, 4);
            Assert.True(zone.RemoveEntity(seed)); Assert.AreSame(clod, Resolve().Owner); AssertFrame(Resolve().ForegroundTile, products, 3);
            zone.GetCell(10, 10).IsVisible = false; Assert.IsNull(Resolve().ForegroundTile);
            zone.GetCell(10, 10).Explored = false; Assert.False(world.TryResolve(zone, 10, 10, out _));
        }

        [Test] public void MissingProductAtlasAndEarlierOverloadPreserveAllEarlierGroups()
        {
            art = new FlatPixelOpeningArtCatalog(baseArt, null, null, states, originalItems, null, null, null, plot, keeper);
            Assert.False(art.TryResolve(factory.CreateEntity("MarlrootClod"), 0, 0, out _));
            Assert.True(art.TryResolve(factory.CreateEntity("FireClay"), 0, 0, out var clay)); AssertFrame(clay, originalItems, 9);
            art.Dispose(); art = NewArt(null);
            Assert.False(art.TryResolve(factory.CreateEntity("PitchpodSeed"), 0, 0, out _));
            Assert.True(art.TryResolve(factory.CreateEntity("SpreadSeedKeeper"), 0, 0, out var person)); AssertFrame(person, keeper, 0);
            var crop = factory.CreateEntity("MarlrootCrop"); crop.GetPart<CropPart>().GrowthStage = 2;
            Assert.True(art.TryResolve(crop, 0, 0, out var ripe)); AssertFrame(ripe, plot, 3);
            world = NewWorld(null); var owner = Add(3); Assert.AreSame(owner, Resolve().Owner);
            Assert.NotNull(Resolve().ForegroundTile); Assert.AreNotSame(products, Resolve().ForegroundTile.sprite.texture);
        }

        [TestCase(127, 24)] [TestCase(128, 23)] [TestCase(129, 24)]
        public void MalformedOptionalAtlasKeepsFallbackAndNeverMutatesBorrowedSettings(int width, int height)
        {
            Object.DestroyImmediate(products); products = new Texture2D(width, height);
            products.filterMode = FilterMode.Bilinear; products.wrapMode = TextureWrapMode.Repeat;
            Create(); var owner = Add(7); AssertNotProduct(owner);
            Assert.AreSame(owner, Resolve().Owner); Assert.AreNotSame(products, Resolve().ForegroundTile.sprite.texture);
            Assert.True(art.TryResolve(factory.CreateEntity("FireClay"), 0, 0, out var clay)); AssertFrame(clay, originalItems, 9);
            Assert.AreEqual(FilterMode.Bilinear, products.filterMode); Assert.AreEqual(TextureWrapMode.Repeat, products.wrapMode);
        }

        [Test] public void DisposalDestroysOnlyOwnedCachedFramesAndDoesNotDisableAPeer()
        {
            Create(); var owner = factory.CreateEntity("PitchpodResin");
            Assert.True(art.TryResolve(owner, 0, 0, out var tile)); var sprite = tile.sprite;
            using (var peer = NewArt(products))
            {
                Assert.True(peer.TryResolve(owner, 0, 0, out var other)); Assert.AreNotSame(tile, other);
                art.Dispose(); art.Dispose(); Assert.True(tile == null && sprite == null); Assert.True(other != null);
                Assert.True(products != null && originalItems != null && keeper != null);
                Assert.False(art.TryResolve(owner, 0, 0, out _));
            }
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)]
        public void ActualGroundAndCarriedPortraitShareTheNativeSubjectWithoutLeakingOldAuthority(int index)
        {
            Create();
            using (var f = new PortraitFixture(world, zone, factory))
            using (var portrait = PortraitProbe())
            {
                var owner = Add(index); var ground = ExamineDocumentBuilder.BuildWorld(owner, zone, zone.GetCell(10, 10), f.Viewer);
                AssertPortrait(portrait.GetPortrait(ground, f.Presentation), index);
                Assert.True(InventorySystem.Pickup(f.Viewer, owner, zone)); Assert.IsNull(zone.GetEntityCell(owner));
                Assert.IsNull(portrait.GetPortrait(ground, f.Presentation).Texture);
                var carried = ExamineDocumentBuilder.BuildInventory(owner, f.Viewer); f.Presentation.Paused = true;
                AssertPortrait(portrait.GetPortrait(carried, f.Presentation), index); Assert.IsNull(portrait.CurrentModelId);
                f.Viewer.GetPart<InventoryPart>().RemoveObject(owner);
                Assert.IsNull(portrait.GetPortrait(carried, f.Presentation).Texture);
                Assert.True(art.TryResolve(owner, 0, 0, out _), "Semantic art is not authorization to display an old document.");
            }
        }

        [TestCase(1)] [TestCase(2)] [TestCase(4)] [TestCase(6)]
        public void RealPlantingConsumesTheLooseSeedAndSelectsTheExistingPlantedFrame(int index)
        {
            Create();
            using (var globals = new HotbarSaveFixture(false, false))
            {
                var oldTurns = TurnManager.Active;
                try
                {
                    typeof(TurnManager).GetProperty("Active", Static).SetValue(null, null);
                    SeedPart.Factory = factory; SettlementRuntime.ActiveZone = zone;
                    var actor = factory.CreateEntity("Player"); Assert.True(zone.AddEntity(actor, 10, 10));
                    var soil = factory.CreateEntity("Grass"); if (index >= 4) soil.AddPart(new CultivatedSoilPart()); Assert.True(zone.AddEntity(soil, 10, 10));
                    var seed = factory.CreateEntity(Names[index]); Assert.True(actor.GetPart<InventoryPart>().AddObject(seed));
                    AssertProduct(seed, index); var cropName = seed.GetPart<SeedPart>().CropBlueprint;
                    Assert.True(InventorySystem.PerformAction(actor, seed, "PlantSeed", zone));
                    Assert.False(actor.GetPart<InventoryPart>().Objects.Contains(seed));
                    Assert.False(InventorySystem.PerformAction(actor, seed, "PlantSeed", zone));
                    var crop = zone.GetCell(10, 10).Objects.Single(e => e.BlueprintName == cropName);
                    Assert.True(world.TryGetSubjectSprite(crop, 10, 10, out var sprite, out var tint));
                    Assert.AreSame(states, sprite.texture); Assert.AreEqual(new Rect(96, 0, 16, 24), sprite.rect); Assert.AreEqual(Color.white, tint);
                    Assert.AreEqual(0, crop.GetPart<CropPart>().GrowthStage);
                }
                finally { typeof(TurnManager).GetProperty("Active", Static).SetValue(null, oldTurns); }
            }
        }

        [Test] public void RealClodPreparationConsumesItsOldPortraitAndReusesAcceptedFireClayArt()
        {
            Create();
            using (var globals = new HotbarSaveFixture(false, false))
            using (var f = new PortraitFixture(world, zone, factory))
            using (var portrait = PortraitProbe())
            {
                SeedPart.Factory = factory; SettlementRuntime.ActiveZone = zone;
                var clod = factory.CreateEntity("MarlrootClod"); Assert.True(f.Viewer.GetPart<InventoryPart>().AddObject(clod));
                var document = ExamineDocumentBuilder.BuildInventory(clod, f.Viewer); AssertPortrait(portrait.GetPortrait(document, f.Presentation), 3);
                Assert.True(InventorySystem.PerformAction(f.Viewer, clod, BotanicalProcessingPart.Command, zone));
                Assert.IsNull(portrait.GetPortrait(document, f.Presentation).Texture);
                Assert.False(InventorySystem.PerformAction(f.Viewer, clod, BotanicalProcessingPart.Command, zone));
                var clay = f.Viewer.GetPart<InventoryPart>().Objects.Single(e => e.BlueprintName == "FireClay");
                Assert.AreEqual(1, clay.GetPart<StackerPart>().StackCount);
                var image = portrait.GetPortrait(ExamineDocumentBuilder.BuildInventory(clay, f.Viewer), f.Presentation);
                Assert.AreSame(originalItems, image.Texture); Assert.AreEqual(new Rect(9f / 12f, 0, 1f / 12f, 1), image.Uv);
            }
        }

        [Test] public void DefaultResourceBindsAllEightActualWorldAndSemanticFrames()
        {
            var resource = Resources.Load<Texture2D>(ResourcePath);
            Assert.NotNull(resource, "Real resource RED remains separate from injected selector tests.");
            Assert.AreEqual(128, resource.width); Assert.AreEqual(24, resource.height);
            Assert.AreEqual(FilterMode.Point, resource.filterMode); Assert.AreEqual(TextureWrapMode.Clamp, resource.wrapMode); Assert.AreEqual(1, resource.mipmapCount);
            art = new FlatPixelOpeningArtCatalog(); world = new FlatPixelGreyboxCatalog();
            for (int i = 0; i < Names.Length; i++)
            {
                var owner = Add(i); AssertFrame(Resolve().ForegroundTile, resource, i);
                Assert.True(art.TryResolve(owner, 0, 0, out var tile)); AssertFrame(tile, resource, i);
                Assert.True(world.TryGetSubjectSprite(owner, 10, 10, out var sprite, out var tint));
                Assert.AreSame(resource, sprite.texture); Assert.AreEqual(tile.sprite.rect, sprite.rect); Assert.AreEqual(Color.white, tint);
                Assert.True(zone.RemoveEntity(owner));
            }
        }

        Entity Add(int index) { var owner = factory.CreateEntity(Names[index]); Assert.NotNull(owner); Assert.True(zone.AddEntity(owner, 10, 10)); return owner; }
        void Create() { art = NewArt(products); world = NewWorld(products); }
        object[] Arguments(Texture2D last) => new object[] { baseArt, null, null, states, originalItems, null, null, null, plot, keeper, last };
        FlatPixelOpeningArtCatalog NewArt(Texture2D last) => (FlatPixelOpeningArtCatalog)Construct(typeof(FlatPixelOpeningArtCatalog), last);
        FlatPixelGreyboxCatalog NewWorld(Texture2D last) => (FlatPixelGreyboxCatalog)Construct(typeof(FlatPixelGreyboxCatalog), last);
        object Construct(Type type, Texture2D last)
        {
            var ctor = type.GetConstructor(Enumerable.Repeat(typeof(Texture2D), 11).ToArray());
            Assert.NotNull(ctor, "An independently optional eleventh plot-product atlas is required.");
            return ctor.Invoke(Arguments(last));
        }
        FlatPixelGreyboxCell Resolve() { Assert.True(world.TryResolve(zone, 10, 10, out var value)); return value; }
        void AssertProduct(Entity owner, int frame) { Assert.True(art.TryResolve(owner, 0, 0, out var tile), owner.BlueprintName); AssertFrame(tile, products, frame); }
        void AssertNotProduct(Entity owner) { if (art.TryResolve(owner, 0, 0, out var tile)) Assert.AreNotSame(products, tile.sprite.texture); }
        static void AssertFrame(Tile tile, Texture2D texture, int frame)
        { Assert.NotNull(tile); Assert.AreSame(texture, tile.sprite.texture); Assert.AreEqual(new Rect(frame * 16, 0, 16, 24), tile.sprite.rect); }
        void AssertPortrait(ExaminePortrait image, int frame)
        { Assert.AreSame(products, image.Texture); Assert.AreEqual(new Rect(frame / 8f, 0, 1f / 8f, 1), image.Uv); Assert.AreEqual(Color.white, image.Tint); Assert.False(image.IsModel); }
        static ExaminePortraitRenderer PortraitProbe() => (ExaminePortraitRenderer)Activator.CreateInstance(typeof(ExaminePortraitRenderer), Instance,
            null, new object[] { (Action<Camera, RenderTexture>)((camera, target) => {}) }, null);

        sealed class PortraitFixture : IDisposable
        {
            readonly GameObject root;
            public readonly Entity Viewer;
            public readonly ZoneRenderer Presentation;
            public PortraitFixture(FlatPixelGreyboxCatalog catalog, Zone zone, EntityFactory factory)
            {
                try
                {
                    Viewer = factory.CreateEntity("Player"); Assert.True(zone.AddEntity(Viewer, 9, 10));
                    root = new GameObject("Plot product portrait fixture"); root.SetActive(false);
                    Presentation = root.AddComponent<ZoneRenderer>(); var environment = root.AddComponent<EnvironmentSpriteRenderer>();
                    typeof(EnvironmentSpriteRenderer).GetField("_flatPixelGreybox", Instance).SetValue(environment, catalog);
                    environment.FlatPixelGreyboxEnabled = true;
                    typeof(ZoneRenderer).GetField("_envSpriteRenderer", Instance).SetValue(Presentation, environment);
                    typeof(ZoneRenderer).GetField("_flatPixelModeEnabled", Instance).SetValue(Presentation, true);
                    typeof(ZoneRenderer).GetProperty("CurrentZone", Instance).SetValue(Presentation, zone);
                    Assert.True(Presentation.FlatPixelModeActive);
                }
                catch { Dispose(); throw; }
            }
            public void Dispose() { if (root != null) Object.DestroyImmediate(root); }
        }
    }
}
