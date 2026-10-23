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
    /// <summary>Loose forage, currency and brewed outputs keep their actual
    /// identity across real acquisition/consumption; art promises no effect.</summary>
    public sealed class FlatPixelWorksProductTests
    {
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags S = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        const string ResourcePath = "Sprites/Live16x24/north-works-products";
        static readonly string[] Names = { "StoneburrSeed", "FrostLichen", "MendleafSprig", "WildBerries",
            "GoldCoin", "BrewedTonic", "ConcentratedMendleaf" };
        EntityFactory factory;
        Texture2D products, baseArt, items, works, vegetation, plotProducts;
        FlatPixelOpeningArtCatalog art;
        FlatPixelGreyboxCatalog world;
        Zone zone;
        Action<int, int, string> oldCellDirty;
        Action<string> oldFullDirty;

        [OneTimeSetUp] public void Content() => factory = DensityReferenceGladeTests.Factory();
        [SetUp] public void Setup()
        {
            products = new Texture2D(112, 24); baseArt = new Texture2D(160, 24);
            items = new Texture2D(192, 24); works = new Texture2D(144, 24);
            vegetation = new Texture2D(64, 24); plotProducts = new Texture2D(128, 24);
            // Item semantics are location-independent. North admission has its
            // own managed-map tests; this detached fixture uses admitted glade.
            zone = new Zone(ReferenceGladePlan.ZoneID);
            for (int y = 9; y <= 11; y++) for (int x = 9; x <= 11; x++)
                zone.GetCell(x, y).Explored = zone.GetCell(x, y).IsVisible = true;
            oldCellDirty = ZoneRenderHooks.CellDirtyCallback; oldFullDirty = ZoneRenderHooks.FullDirtyCallback;
            ZoneRenderHooks.CellDirtyCallback = null; ZoneRenderHooks.FullDirtyCallback = null;
        }
        [TearDown] public void Cleanup()
        {
            try
            {
                art?.Dispose(); world?.Dispose(); art = null; world = null;
                foreach (var texture in new[] { products, baseArt, items, works, vegetation, plotProducts })
                    if (texture != null) Object.DestroyImmediate(texture);
            }
            finally { ZoneRenderHooks.CellDirtyCallback = oldCellDirty; ZoneRenderHooks.FullDirtyCallback = oldFullDirty; }
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        [TestCase(4)] [TestCase(5)] [TestCase(6)]
        public void ExactCurrentLooseOwnerUsesACachedNativeFrameAndExactSubject(int index)
        {
            Create(); var owner = Add(index); var cell = Resolve();
            Frame(cell.ForegroundTile, products, index); Assert.AreSame(owner, cell.Owner);
            Assert.AreEqual(Color.white, cell.ForegroundColor);
            Assert.AreEqual(new Vector2(8, 12), cell.ForegroundTile.sprite.pivot);
            Assert.AreEqual(16, cell.ForegroundTile.sprite.pixelsPerUnit);
            Assert.AreEqual(Tile.ColliderType.None, cell.ForegroundTile.colliderType);
            Assert.AreSame(cell.ForegroundTile, Resolve().ForegroundTile);
            Assert.True(world.TryGetSubjectSprite(owner, 10, 10, out var sprite, out var tint));
            Assert.AreSame(cell.ForegroundTile.sprite, sprite); Assert.AreEqual(Color.white, tint);
            Assert.True(zone.RemoveEntity(owner)); Assert.IsNull(Resolve().ForegroundTile);
        }

        [TestCase(0, "Reagent")] [TestCase(1, "Reagent")] [TestCase(2, "Reagent")]
        [TestCase(3, "Food")] [TestCase(4, "Commerce")] [TestCase(5, "Tonic")] [TestCase(6, "Reagent")]
        public void FunctionalIdentityNeedsItsActualOwnedPart(int index, string name)
        {
            Create(); var owner = Add(index); Native(owner, index); var part = owner.GetPart(name);
            Assert.NotNull(part); part.ParentEntity = new Entity(); NotNative(owner);
            part.ParentEntity = owner; Native(owner, index); Assert.True(owner.RemovePart(part)); NotNative(owner);
            Assert.AreSame(owner, Resolve().Owner); Assert.AreNotSame(products, Resolve().ForegroundTile.sprite.texture);
        }

        [TestCase(0, "binding")] [TestCase(1, "cold")] [TestCase(2, "vital")] [TestCase(6, "vital")]
        public void CachedReagentPropertiesFollowPositiveMeaningRatherThanRawFormatting(int index, string property)
        {
            Create(); var owner = factory.CreateEntity(Names[index]); var reagent = owner.GetPart<ReagentPart>();
            Native(owner, index);
            foreach (var invalid in new[] { "", "unknown:2", property + ":0", property + ":-1", property + ":bad" })
            { reagent.PropertiesRaw = invalid; NotNative(owner); }
            reagent.PropertiesRaw = " unrelated:2 ; " + property.ToUpperInvariant() + ":3 "; Native(owner, index);
            Assert.False(owner.HasPart<SeedPart>(), "Stoneburr is a reagent despite its name, not a plantable seed.");
            Assert.False(owner.HasPart<TonicPart>(), "Raw plant matter is not its brewed result.");
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
        [TestCase(4)] [TestCase(5)] [TestCase(6)]
        public void ReskinsForeignRolesAndInvalidPhysicalOwnersCannotAliasNativeGoods(int index)
        {
            Create(); var owner = factory.CreateEntity(Names[index]); Native(owner, index);
            var render = owner.GetPart<RenderPart>(); var physics = owner.GetPart<PhysicsPart>(); var stack = owner.GetPart<StackerPart>();
            render.VisualID = "other-art"; NotNative(owner); render.VisualID = "";
            render.VisualVariant = "other-variant"; NotNative(owner); render.VisualVariant = "";
            render.GlyphVariants = "other-glyphs"; NotNative(owner); render.GlyphVariants = "";
            render.ParentEntity = new Entity(); NotNative(owner); render.ParentEntity = owner;
            render.Visible = false; NotNative(owner); render.Visible = true;
            physics.ParentEntity = new Entity(); NotNative(owner); physics.ParentEntity = owner;
            physics.Solid = true; NotNative(owner); physics.Solid = false;
            physics.Takeable = false; NotNative(owner); physics.Takeable = true;
            owner.Tags.Remove("Item"); NotNative(owner); owner.SetTag("Item");
            foreach (string role in new[] { "Creature", "Player", "Terrain", "Crop" })
            { owner.SetTag(role); NotNative(owner); owner.Tags.Remove(role); }
            var crop = new CropPart(); owner.AddPart(crop); NotNative(owner); owner.RemovePart(crop);
            stack.StackCount = 0; NotNative(owner); stack.StackCount = stack.MaxStack + 1; NotNative(owner); stack.StackCount = 1;
            stack.ParentEntity = new Entity(); NotNative(owner); stack.ParentEntity = owner;
            Native(owner, index); owner.BlueprintName = "ForeignGoods"; NotNative(owner);
        }

        [Test] public void DescriptiveTextFoodPotencyAndCoinPriceAreNotNewSpriteStates()
        {
            Create();
            for (int i = 0; i < Names.Length; i++)
            {
                var owner = factory.CreateEntity(Names[i]); var render = owner.GetPart<RenderPart>();
                render.DisplayName = "a changed description"; render.ColorString = "&m"; render.RenderString = "?";
                Native(owner, i); owner.GetPart<StackerPart>().StackCount = 2; Native(owner, i);
            }
            var berries = factory.CreateEntity("WildBerries"); berries.GetPart<FoodPart>().Healing = ""; Native(berries, 3);
            Assert.False(berries.HasPart<CookablePart>(), "Cooking=Meal is flavor, not a cooking transformation.");
            var coin = factory.CreateEntity("GoldCoin"); coin.GetPart<CommercePart>().Value = 73; Native(coin, 4);
        }

        [TestCase("Tonic")] [TestCase("Coating")] [TestCase("Throwable")]
        public void RecognizedOwnedVesselFormsShareNeutralArtWithoutChangingEffects(string form)
        {
            Create(); var owner = factory.CreateEntity("BrewedTonic"); var tonic = owner.GetPart<TonicPart>();
            Native(owner, 5); tonic.Healing = "2d4"; tonic.Drink = false; Native(owner, 5);
            var brew = new BrewItemPart { Form = form, EffectsRaw = "Poison:2" }; owner.AddPart(brew);
            Native(owner, 5); Assert.AreEqual("Poison:2", brew.EffectsRaw); Assert.False(owner.HasEffect<PoisonedEffect>());
            brew.ParentEntity = new Entity(); NotNative(owner); brew.ParentEntity = owner; Native(owner, 5);
            brew.EffectsRaw = ""; Native(owner, 5); Assert.AreEqual("2d4", tonic.Healing); Assert.False(tonic.Drink);
        }

        [TestCase("Food")] [TestCase("food")] [TestCase("tonic")] [TestCase("unknown")] [TestCase("")] [TestCase(null)]
        public void ExplicitNonVesselOrUnknownFormRetainsFallback(string form)
        {
            Create(); var owner = Add(5); Native(owner, 5);
            owner.AddPart(new BrewItemPart { Form = form, EffectsRaw = "Stoneskin:2" }); NotNative(owner);
            Assert.AreSame(owner, Resolve().Owner); Assert.AreNotSame(products, Resolve().ForegroundTile.sprite.texture);
        }

        [TestCase("carried")] [TestCase("equipped")] [TestCase("stale-cell")] [TestCase("foreign-render")]
        public void GroundDrawingRejectsLostCustodyWithoutLosingLowerVisibleOwner(string fault)
        {
            world = new FlatPixelGreyboxCatalog(baseArt); var lower = Add(3); lower.GetPart<RenderPart>().RenderLayer = 2;
            var top = Add(0); Assert.AreSame(top, Resolve().Owner);
            if (fault == "carried") top.GetPart<PhysicsPart>().InInventory = new Entity();
            if (fault == "equipped") top.GetPart<PhysicsPart>().Equipped = new Entity();
            if (fault == "foreign-render") top.GetPart<RenderPart>().ParentEntity = new Entity();
            if (fault == "stale-cell") { var old = zone.GetCell(10, 10); Assert.True(zone.MoveEntity(top, 11, 10)); old.Objects.Add(top); }
            Assert.AreSame(lower, Resolve().Owner); Assert.True(zone.RemoveEntity(lower)); Assert.IsNull(Resolve().ForegroundTile);
        }

        [Test] public void FogSuppressesProductsAndSameLayerRemovalExposesActualLowerItem()
        {
            Create(); var first = Add(1); var top = Add(2); Frame(Resolve().ForegroundTile, products, 2);
            Assert.True(zone.RemoveEntity(top)); Assert.AreSame(first, Resolve().Owner); Frame(Resolve().ForegroundTile, products, 1);
            var cell = zone.GetCell(10, 10); cell.IsVisible = false; Assert.IsNull(Resolve().ForegroundTile);
            cell.Explored = false; Assert.False(world.TryResolve(zone, 10, 10, out _));
        }

        [TestCase(111, 24)] [TestCase(112, 23)] [TestCase(113, 24)]
        public void MalformedAtlasKeepsOtherGroupsAndBorrowedSettings(int width, int height)
        {
            Object.DestroyImmediate(products); products = new Texture2D(width, height) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Repeat };
            Create(); var owner = Add(2); NotNative(owner); Assert.AreSame(owner, Resolve().Owner);
            Assert.True(art.TryResolve(factory.CreateEntity("MarlrootClod"), 0, 0, out var clod)); Frame(clod, plotProducts, 3);
            Assert.True(art.TryResolve(factory.CreateEntity("HealingTonic"), 0, 0, out var tonic)); Frame(tonic, items, 7);
            Assert.AreEqual(FilterMode.Bilinear, products.filterMode); Assert.AreEqual(TextureWrapMode.Repeat, products.wrapMode);
        }

        [Test] public void MissingTwelfthGroupAndOldInjectionNeverImplicitlyLoadTheNewResource()
        {
            art = new FlatPixelOpeningArtCatalog(baseArt, null, null, null, items, null, vegetation, works, null, null, plotProducts);
            Assert.False(art.TryResolve(factory.CreateEntity("GoldCoin"), 0, 0, out _)); art.Dispose(); art = NewArt(null);
            Assert.False(art.TryResolve(factory.CreateEntity("MendleafSprig"), 0, 0, out _));
            Assert.True(art.TryResolve(factory.CreateEntity("PitchpodResin"), 0, 0, out var resin)); Frame(resin, plotProducts, 5);
            world = NewWorld(null); var owner = Add(0); Assert.AreSame(owner, Resolve().Owner);
            Assert.AreNotSame(products, Resolve().ForegroundTile.sprite.texture);
        }

        [Test] public void DisposalDestroysGeneratedFramesOnlyAndLeavesIndependentPeersAlive()
        {
            Create(); var owner = factory.CreateEntity("GoldCoin"); Assert.True(art.TryResolve(owner, 0, 0, out var tile)); var sprite = tile.sprite;
            using (var peer = NewArt(products))
            {
                Assert.True(peer.TryResolve(owner, 0, 0, out var other)); Assert.AreNotSame(tile, other);
                art.Dispose(); art.Dispose(); Assert.True(tile == null && sprite == null); Assert.True(other != null);
                Assert.True(products != null && plotProducts != null && items != null); Assert.False(art.TryResolve(owner, 0, 0, out _));
            }
        }

        [TestCase(0, "StoneburrPatch")] [TestCase(1, "FrostLichenPatch")]
        [TestCase(2, "MendleafPlant")] [TestCase(3, "BerryBush")]
        public void ActualFiniteHarvestOverflowPickupAndCarriedPortraitFollowTheSameGoods(int index, string sourceName)
        {
            Create();
            using (var globals = new HotbarSaveFixture(false, false))
            using (var f = new PortraitFixture(world, zone, factory))
            using (var portrait = PortraitProbe())
            {
                var oldFactory = HarvestablePart.Factory; HarvestablePart.Factory = factory;
                try
                {
                    var source = factory.CreateEntity(sourceName); Assert.True(zone.AddEntity(source, 10, 10));
                    var pack = f.Viewer.GetPart<InventoryPart>(); int capacity = pack.MaxWeight; pack.MaxWeight = 0;
                    Assert.True(world.TryResolve(zone, 10, 10, out var before)); Assert.AreSame(source, before.Owner);
                    Assert.AreSame(index == 3 ? vegetation : works, before.ForegroundTile.sprite.texture);
                    Assert.True(DensityHarvestSecurityTests.Act(source, f.Viewer, zone)); Assert.IsNull(zone.GetEntityCell(source));
                    Assert.False(DensityHarvestSecurityTests.Act(source, f.Viewer, zone));
                    var dropped = zone.GetCell(10, 10).Objects.Where(e => e.BlueprintName == Names[index]).ToArray();
                    Assert.That(dropped.Length, Is.InRange(1, index == 3 ? 3 : 2)); Assert.IsEmpty(pack.Objects);
                    Frame(Resolve().ForegroundTile, products, index); pack.MaxWeight = capacity;
                    var subject = dropped[dropped.Length - 1]; var ground = ExamineDocumentBuilder.BuildWorld(subject, zone, zone.GetCell(10, 10), f.Viewer);
                    Portrait(portrait.GetPortrait(ground, f.Presentation), index);
                    Assert.True(InventorySystem.Pickup(f.Viewer, subject, zone)); Assert.IsNull(portrait.GetPortrait(ground, f.Presentation).Texture);
                    var carried = ExamineDocumentBuilder.BuildInventory(subject, f.Viewer); f.Presentation.Paused = true;
                    Portrait(portrait.GetPortrait(carried, f.Presentation), index);
                    Assert.True(pack.RemoveObject(subject)); Assert.IsNull(portrait.GetPortrait(carried, f.Presentation).Texture);
                }
                finally { HarvestablePart.Factory = oldFactory; }
            }
        }

        [Test] public void ActualCoinPickupConvertsToCurrencyAndCannotRemainAnInventoryOrWorldPortrait()
        {
            Create();
            using (var globals = new HotbarSaveFixture(false, false))
            using (var f = new PortraitFixture(world, zone, factory))
            using (var portrait = PortraitProbe())
            {
                var coin = Add(4); coin.GetPart<StackerPart>().StackCount = 3; coin.GetPart<CommercePart>().Value = 73;
                var doc = ExamineDocumentBuilder.BuildWorld(coin, zone, zone.GetCell(10, 10), f.Viewer); Portrait(portrait.GetPortrait(doc, f.Presentation), 4);
                int before = TradeSystem.GetDrams(f.Viewer); Assert.True(InventorySystem.Pickup(f.Viewer, coin, zone));
                Assert.AreEqual(before + 15, TradeSystem.GetDrams(f.Viewer)); Assert.AreEqual(0, coin.GetPart<StackerPart>().StackCount);
                Assert.False(f.Viewer.GetPart<InventoryPart>().Objects.Contains(coin)); Assert.IsNull(Resolve().ForegroundTile);
                Assert.IsNull(portrait.GetPortrait(doc, f.Presentation).Texture);
                Assert.False(InventorySystem.Pickup(f.Viewer, coin, zone)); Assert.AreEqual(before + 15, TradeSystem.GetDrams(f.Viewer));
            }
        }

        [TestCase("StoneburrSeed", null, "Tonic", "Stoneskin", null)]
        [TestCase("FrostLichen", null, "Coating", "Frozen", null)]
        [TestCase("MendleafSprig", null, "Tonic", null, "1d4")]
        [TestCase("ConcentratedMendleaf", null, "Tonic", null, "2d4")]
        [TestCase("StoneburrSeed", "BlastcapSpore", "Throwable", "Stoneskin", null)]
        [TestCase("EmberFruit", null, "Food", null, "1d4")]
        public void ActualBrewingConsumesReagentsAndKeepsNeutralPresentationWithoutInferringSafety(string input, string second, string form, string status, string healing)
        {
            Create();
            using (var globals = new HotbarSaveFixture(false, false))
            using (var rules = new ActualBrewRules())
            using (var f = new PortraitFixture(world, zone, factory))
            using (var portrait = PortraitProbe())
            {
                var pack = f.Viewer.GetPart<InventoryPart>(); var reagent = factory.CreateEntity(input); Assert.True(pack.AddObject(reagent));
                var selection = new List<Entity> { reagent };
                if (second != null) { var other = factory.CreateEntity(second); Assert.True(pack.AddObject(other)); selection.Add(other); }
                Assert.True(BrewingService.TryBrew(f.Viewer, factory, selection, out var output, out var result, out var reason), reason);
                Assert.AreEqual(BrewOutcomeKind.Brew, result.Kind); Assert.AreEqual(form, result.Form);
                Assert.True(selection.All(e => !pack.Objects.Contains(e))); Assert.AreEqual("BrewedTonic", output.BlueprintName);
                if (status == null)
                {
                    Assert.IsNull(output.GetPart<BrewItemPart>());
                    Assert.AreEqual(healing, output.GetPart<TonicPart>().Healing);
                    // Existing service loses healing-only Food's form. Do not
                    // recover it from prose or change gameplay in this art slice.
                }
                else { Assert.AreEqual(form, output.GetPart<BrewItemPart>().Form); Assert.True(output.GetPart<BrewItemPart>().GetEffects().Any(e => e.Property == status)); }
                var doc = ExamineDocumentBuilder.BuildInventory(output, f.Viewer); Portrait(portrait.GetPortrait(doc, f.Presentation), 5);
                Assert.IsNull(zone.GetEntityCell(output)); Assert.True(InventorySystem.Drop(f.Viewer, output, zone));
                Assert.IsNull(portrait.GetPortrait(doc, f.Presentation).Texture);
                Assert.True(world.TryResolve(zone, 9, 10, out var cell));
                // The player covers the drop. Exact-subject selection must still
                // show the dropped brew without substituting the top actor.
                Assert.AreSame(f.Viewer, cell.Owner);
                Portrait(portrait.GetPortrait(ExamineDocumentBuilder.BuildWorld(output, zone, zone.GetCell(9, 10), f.Viewer), f.Presentation), 5);
            }
        }

        [Test] public void RealNearStillPreparationConsumesTwoSprigsAndProducesASeparateConcentrateFrame()
        {
            Create();
            using (var globals = new HotbarSaveFixture(false, false))
            using (var f = new PortraitFixture(world, zone, factory))
            using (var portrait = PortraitProbe())
            {
                SeedPart.Factory = factory; SettlementRuntime.ActiveZone = zone;
                var raw = factory.CreateEntity("MendleafSprig"); raw.GetPart<StackerPart>().StackCount = 2;
                Assert.True(f.Viewer.GetPart<InventoryPart>().AddObject(raw));
                var doc = ExamineDocumentBuilder.BuildInventory(raw, f.Viewer); Portrait(portrait.GetPortrait(doc, f.Presentation), 2);
                Assert.False(InventorySystem.PerformAction(f.Viewer, raw, "PrepareRecipe|concentrate_mendleaf", zone));
                Assert.AreEqual(2, raw.GetPart<StackerPart>().StackCount);
                Assert.True(zone.AddEntity(factory.CreateEntity("AlchemyStill"), 9, 9));
                Assert.True(InventorySystem.PerformAction(f.Viewer, raw, "PrepareRecipe|concentrate_mendleaf", zone));
                Assert.IsNull(portrait.GetPortrait(doc, f.Presentation).Texture);
                var output = f.Viewer.GetPart<InventoryPart>().Objects.Single(e => e.BlueprintName == "ConcentratedMendleaf");
                Assert.AreEqual(1, output.GetPart<StackerPart>().StackCount); Native(output, 6);
                Portrait(portrait.GetPortrait(ExamineDocumentBuilder.BuildInventory(output, f.Viewer), f.Presentation), 6);
                Assert.False(output.HasPart<TonicPart>()); Assert.False(output.HasPart<FoodPart>());
            }
        }

        [Test] public void DefaultResourceActuallyBindsAllSevenWorldAndSubjectFrames()
        {
            var resource = Resources.Load<Texture2D>(ResourcePath); Assert.NotNull(resource, "Imported-resource RED is separate from injected runtime RED.");
            Assert.AreEqual(112, resource.width); Assert.AreEqual(24, resource.height); Assert.AreEqual(1, resource.mipmapCount);
            Assert.AreEqual(FilterMode.Point, resource.filterMode); Assert.AreEqual(TextureWrapMode.Clamp, resource.wrapMode);
            art = new FlatPixelOpeningArtCatalog(); world = new FlatPixelGreyboxCatalog();
            for (int i = 0; i < Names.Length; i++)
            {
                var owner = Add(i); Frame(Resolve().ForegroundTile, resource, i);
                Assert.True(art.TryResolve(owner, 0, 0, out var tile)); Frame(tile, resource, i);
                Assert.True(world.TryGetSubjectSprite(owner, 10, 10, out var sprite, out var tint));
                Assert.AreSame(resource, sprite.texture); Assert.AreEqual(tile.sprite.rect, sprite.rect); Assert.AreEqual(Color.white, tint);
                Assert.True(zone.RemoveEntity(owner));
            }
        }

        Entity Add(int index) { var owner = factory.CreateEntity(Names[index]); Assert.True(zone.AddEntity(owner, 10, 10)); return owner; }
        void Create() { art = NewArt(products); world = NewWorld(products); }
        object Construct(Type type, Texture2D last)
        {
            var ctor = type.GetConstructor(Enumerable.Repeat(typeof(Texture2D), 12).ToArray());
            Assert.NotNull(ctor, "The independently optional twelfth works-product atlas is required.");
            return ctor.Invoke(new object[] { baseArt, null, null, null, items, null, vegetation, works, null, null, plotProducts, last });
        }
        FlatPixelOpeningArtCatalog NewArt(Texture2D last) => (FlatPixelOpeningArtCatalog)Construct(typeof(FlatPixelOpeningArtCatalog), last);
        FlatPixelGreyboxCatalog NewWorld(Texture2D last) => (FlatPixelGreyboxCatalog)Construct(typeof(FlatPixelGreyboxCatalog), last);
        FlatPixelGreyboxCell Resolve() { Assert.True(world.TryResolve(zone, 10, 10, out var cell)); return cell; }
        void Native(Entity owner, int index) { Assert.True(art.TryResolve(owner, 0, 0, out var tile), owner.BlueprintName); Frame(tile, products, index); }
        void NotNative(Entity owner) { if (art.TryResolve(owner, 0, 0, out var tile)) Assert.AreNotSame(products, tile.sprite.texture); }
        static void Frame(Tile tile, Texture2D texture, int index) { Assert.NotNull(tile); Assert.AreSame(texture, tile.sprite.texture); Assert.AreEqual(new Rect(index * 16, 0, 16, 24), tile.sprite.rect); }
        void Portrait(ExaminePortrait image, int index) { Assert.AreSame(products, image.Texture); Assert.AreEqual(new Rect(index / 7f, 0, 1f / 7f, 1), image.Uv); Assert.AreEqual(Color.white, image.Tint); Assert.False(image.IsModel); }
        static ExaminePortraitRenderer PortraitProbe() => (ExaminePortraitRenderer)Activator.CreateInstance(typeof(ExaminePortraitRenderer), I,
            null, new object[] { (Action<Camera, RenderTexture>)((camera, target) => {}) }, null);

        sealed class PortraitFixture : IDisposable
        {
            readonly GameObject root;
            internal readonly Entity Viewer;
            internal readonly ZoneRenderer Presentation;
            internal PortraitFixture(FlatPixelGreyboxCatalog catalog, Zone zone, EntityFactory factory)
            {
                try
                {
                    Viewer = factory.CreateEntity("Player"); Assert.True(zone.AddEntity(Viewer, 9, 10));
                    root = new GameObject("Works product portrait fixture"); root.SetActive(false);
                    Presentation = root.AddComponent<ZoneRenderer>(); var environment = root.AddComponent<EnvironmentSpriteRenderer>();
                    typeof(EnvironmentSpriteRenderer).GetField("_flatPixelGreybox", I).SetValue(environment, catalog); environment.FlatPixelGreyboxEnabled = true;
                    typeof(ZoneRenderer).GetField("_envSpriteRenderer", I).SetValue(Presentation, environment);
                    typeof(ZoneRenderer).GetField("_flatPixelModeEnabled", I).SetValue(Presentation, true);
                    typeof(ZoneRenderer).GetProperty("CurrentZone", I).SetValue(Presentation, zone);
                    Assert.True(Presentation.FlatPixelModeActive);
                }
                catch { Dispose(); throw; }
            }
            public void Dispose() { if (root != null) Object.DestroyImmediate(root); }
        }

        sealed class ActualBrewRules : IDisposable
        {
            readonly Dictionary<string, BrewRule> saved;
            readonly BrewRule[] order;
            readonly bool initialized;
            static FieldInfo Field(string name) => typeof(BrewRuleRegistry).GetField(name, S);
            internal ActualBrewRules()
            {
                saved = new Dictionary<string, BrewRule>((Dictionary<string, BrewRule>)Field("RulesById").GetValue(null));
                order = ((List<BrewRule>)Field("RulesInOrder").GetValue(null)).ToArray(); initialized = (bool)Field("_initialized").GetValue(null);
                BrewRuleRegistry.InitializeFromJson(Resources.Load<TextAsset>("Content/Data/Alchemy/BrewRules").text);
            }
            public void Dispose()
            {
                var rules = (Dictionary<string, BrewRule>)Field("RulesById").GetValue(null); rules.Clear(); foreach (var row in saved) rules[row.Key] = row.Value;
                var list = (List<BrewRule>)Field("RulesInOrder").GetValue(null); list.Clear(); list.AddRange(order); Field("_initialized").SetValue(null, initialized);
            }
        }
    }
}
