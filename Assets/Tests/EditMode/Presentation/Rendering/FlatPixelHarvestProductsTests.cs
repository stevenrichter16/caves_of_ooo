using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Tests first: real public world/reader behavior fails before imported-art
    // assertions. New companion APIs are reflected so the native RED compiles.
    public sealed class FlatPixelHarvestProductsTests
    {
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags S = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        const string Outside = "Overworld.15.13.0";
        const int X = 10, Y = 10;
        static readonly string[] Paths = { "Sprites/Live16x24/world-harvest-products", "Sprites/Live16x24/opening-items", "Sprites/Live16x24/north-works-products" };
        static readonly int[] Counts = { 8, 12, 7 };
        static readonly object[][] Rows = {
            new object[] { "PaleSalt", 0, 0 }, new object[] { "Bone", 0, 1 }, new object[] { "SaltbriarSprig", 0, 2 },
            new object[] { "ChoirIron", 0, 3 }, new object[] { "Honeycomb", 0, 4 }, new object[] { "GroveRed", 0, 5 },
            new object[] { "Tepuibone", 0, 6 }, new object[] { "KnotflaxCord", 0, 7 },
            new object[] { "GlowQuartz", 1, 6 }, new object[] { "Mushroom", 1, 4 }, new object[] { "FireClay", 1, 9 },
            new object[] { "SalvagedTimber", 1, 10 }, new object[] { "StoneburrSeed", 2, 0 }, new object[] { "FrostLichen", 2, 1 },
            new object[] { "MendleafSprig", 2, 2 }, new object[] { "WildBerries", 2, 3 }, new object[] { "GoldCoin", 2, 4 }
        };
        static IEnumerable<TestCaseData> Products => Rows.Select(r => new TestCaseData(r));
        readonly List<Action> restore = new List<Action>();
        readonly List<Texture2D> textures = new List<Texture2D>();
        HotbarSaveFixture globals;
        EntityFactory factory;
        OverworldZoneManager manager;
        Zone zone;
        FlatPixelGreyboxCatalog world;
        IDisposable adapter;
        Texture2D[] injected;
        delegate bool Query(Entity owner, out Tile tile);
        Query query;

        [SetUp] public void Setup()
        {
            SnapshotStatics(typeof(MessageLog)); SnapshotStatics(typeof(EquipmentChangeBus));
            SnapshotStatics(typeof(WorldAudioHooks)); SnapshotStatics(typeof(WorldAudioUtility)); SnapshotStatics(typeof(EntityVisualHooks));
            Snapshot(typeof(AnatomyFactory).GetField("_types", S)); Snapshot(typeof(BodyPart).GetField("_nextID", S));
            foreach (string name in new[] { "_buffer", "_writeIndex", "_filledCount", "_droppedCount" }) Snapshot(typeof(Diag).GetField(name, S));
            Snapshot(typeof(HarvestablePart).GetField("Factory", S));
            globals = new HotbarSaveFixture(false, false);
            MessageLog.OnMessage = null; MessageLog.Clear(); WorldAudioHooks.Callback = null; WorldAudioUtility.Callback = null; EntityVisualHooks.Reset();
            ZoneRenderHooks.CellDirtyCallback = null; ZoneRenderHooks.FullDirtyCallback = null;
            factory = DensityReferenceGladeTests.Factory(); HarvestablePart.Factory = factory;
            manager = OverworldZoneManager.CreateDetached(factory, 65, true); Attach(Outside);
        }
        [TearDown] public void Cleanup()
        {
            try
            {
                world?.Dispose(); adapter?.Dispose(); world = null; adapter = null; query = null;
                foreach (var texture in textures) if (texture != null) Object.DestroyImmediate(texture);
                textures.Clear(); globals?.Dispose(); globals = null;
            }
            finally { for (int i = restore.Count - 1; i >= 0; i--) restore[i](); restore.Clear(); }
        }
        void Attach(string id)
        {
            zone = new Zone(id); manager.SetActiveZone(zone);
            for (int y = 8; y <= 12; y++) for (int x = 8; x <= 13; x++) zone.GetCell(x, y).Explored = zone.GetCell(x, y).IsVisible = true;
        }
        Entity Owner(string id) { var owner = factory.CreateEntity(id); Assert.NotNull(owner, id); return owner; }
        Entity Add(string id, int x = X, int y = Y) { var owner = Owner(id); Assert.True(zone.AddEntity(owner, x, y)); return owner; }
        FlatPixelGreyboxCell View(int x = X, int y = Y) { Assert.True(world.TryResolvePresentation(zone, x, y, out var value)); return value; }
        static object[] Row(string id) => Rows.Single(r => (string)r[0] == id);
        Texture2D TextureFor(string id) => injected[(int)Row(id)[1]];
        static int FrameFor(string id) => (int)Row(id)[2];
        static void Frame(Tile tile, Texture2D texture, int frame)
        {
            Assert.NotNull(tile); Assert.NotNull(tile.sprite); Assert.AreSame(texture, tile.sprite.texture);
            Assert.AreEqual(new Rect(frame * 16, 0, 16, 24), tile.sprite.rect);
            Assert.AreEqual(new Vector2(8, 12), tile.sprite.pivot); Assert.AreEqual(16, tile.sprite.pixelsPerUnit);
            Assert.AreEqual(Tile.ColliderType.None, tile.colliderType); Assert.AreEqual(TileFlags.None, tile.flags);
        }
        Tile Native(Entity owner)
        { Assert.True(query(owner, out var tile), owner.BlueprintName); Frame(tile, TextureFor(owner.BlueprintName), FrameFor(owner.BlueprintName)); return tile; }
        void Refused(Entity owner) { Assert.False(query(owner, out var tile), owner.BlueprintName); Assert.IsNull(tile); }
        static Texture2D Imported(int group)
        {
            var texture = Resources.Load<Texture2D>(Paths[group]); Assert.NotNull(texture, Paths[group]);
            Assert.AreEqual(Counts[group] * 16, texture.width); Assert.AreEqual(24, texture.height);
            Assert.AreEqual(FilterMode.Point, texture.filterMode); Assert.AreEqual(TextureWrapMode.Clamp, texture.wrapMode);
            Assert.AreEqual(1, texture.mipmapCount); return texture;
        }
        static Type AdapterType()
        { var type = typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelHarvestProductsCatalog"); Assert.NotNull(type, "The exact portable harvest-product companion is absent."); return type; }
        static IDisposable Construct(Texture2D products, Texture2D items, Texture2D works)
        {
            var ctor = AdapterType().GetConstructor(new[] { typeof(Texture2D), typeof(Texture2D), typeof(Texture2D) });
            Assert.NotNull(ctor, "Independent optional texture injection is required.");
            return (IDisposable)ctor.Invoke(new object[] { products, items, works });
        }
        static Query Resolver(IDisposable art) => (Query)Delegate.CreateDelegate(typeof(Query), art,
            AdapterType().GetMethod("TryResolve", new[] { typeof(Entity), typeof(Tile).MakeByRefType() }));
        void Create()
        {
            injected = new Texture2D[3];
            for (int i = 0; i < 3; i++) { injected[i] = new Texture2D(Counts[i] * 16, 24); textures.Add(injected[i]); }
            Inject(injected);
        }
        void Inject(Texture2D[] source)
        {
            world?.Dispose(); adapter?.Dispose(); adapter = Construct(source[0], source[1], source[2]); query = Resolver(adapter);
            world = new FlatPixelGreyboxCatalog((Texture2D)null);
            var field = typeof(FlatPixelGreyboxCatalog).GetField("_harvestProductsArt", I); Assert.NotNull(field);
            field.SetValue(world, adapter);
        }

        [TestCaseSource(nameof(Products))]
        public void RealFactoryProductPaintsNativeAndExactSubjectOutsideOpeningBeforeResourceAssertion(string id, int group, int frame)
        {
            var owner = Add(id); world = new FlatPixelGreyboxCatalog();
            Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            var value = View(); Assert.AreSame(owner, value.Owner);
            Assert.True(value.NativeForeground, "A declared harvested item is still a glyph outside the opening.");
            Frame(value.ForegroundTile, Imported(group), frame); Assert.AreEqual(Color.white, value.ForegroundColor);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, X, Y, out var sprite, out var tint));
            Assert.AreSame(value.ForegroundTile.sprite, sprite); Assert.AreEqual(Color.white, tint);
            Assert.NotNull(owner.GetPart<StackerPart>()); Assert.NotNull(owner.GetPart<CommercePart>());
            Assert.IsNull(owner.GetPart<MaterialPart>()); Assert.IsNull(owner.GetPart<ThermalPart>());
            Assert.AreEqual(id == "GroveRed" ? 3 : 5, owner.GetPart<RenderPart>().RenderLayer);
        }

        [TestCaseSource(nameof(Products))]
        public void AuthorizedCarriedProductUsesSameAtlasRegionWhileInventoryPausesWorld(string id, int group, int frame)
        {
            world = new FlatPixelGreyboxCatalog();
            using (var reader = new Reader(world, zone, Owner("Player")))
            {
                var owner = Owner(id); Assert.True(reader.Viewer.GetPart<InventoryPart>().AddObject(owner));
                reader.Presentation.Paused = true; var document = ExamineDocumentBuilder.BuildInventory(owner, reader.Viewer);
                Assert.NotNull(document); Assert.True(ExamineDocumentBuilder.IsCurrentSubject(document));
                Assert.True(world.TryGetFallbackSubjectSprite(owner, X, Y, out var glyph, out _));
                Assert.True(reader.Presentation.TryGetFlatExaminePortrait(document, out var image));
                Assert.AreNotSame(glyph.texture, image.Texture, "Authorized carried product still uses an enlarged glyph.");
                Portrait(reader, document, Imported(group), frame); Assert.IsNull(zone.GetEntityCell(owner));
            }
        }

        [Test] public void AllSeventeenFramesAreCachedWithoutMutatingBorrowedTexturesOrItemState()
        {
            Create(); var distinct = new HashSet<Tile>();
            foreach (var row in Rows)
            {
                var owner = Owner((string)row[0]); int parts = owner.Parts.Count; var tile = Native(owner); Assert.True(distinct.Add(tile));
                owner.GetPart<RenderPart>().DisplayName = "named material"; owner.GetPart<CommercePart>().Value += 13;
                owner.GetPart<StackerPart>().StackCount = Math.Min(3, owner.GetPart<StackerPart>().MaxStack);
                for (int i = 0; i < 8; i++) Assert.AreSame(tile, Native(owner));
                Assert.AreEqual(parts, owner.Parts.Count); Assert.IsNull(owner.GetPart<MaterialPart>());
                Assert.AreEqual(FilterMode.Bilinear, TextureFor(owner.BlueprintName).filterMode);
                Assert.AreEqual(TextureWrapMode.Repeat, TextureFor(owner.BlueprintName).wrapMode);
            }
        }

        [TestCase("Render")] [TestCase("Physics")] [TestCase("Stacker")] [TestCase("Commerce")]
        public void EveryProductRequiresSoleCurrentOwnedCommonParts(string partName)
        {
            Create();
            foreach (var row in Rows)
            {
                var owner = Owner((string)row[0]); Native(owner); var part = owner.GetPart(partName); Assert.NotNull(part);
                part.ParentEntity = new Entity(); Refused(owner); part.ParentEntity = owner; Native(owner);
                var duplicate = (Part)Activator.CreateInstance(part.GetType()); owner.AddPart(duplicate); Refused(owner);
                Assert.True(owner.RemovePart(duplicate)); Native(owner);
                Assert.True(owner.RemovePart(part)); Refused(owner); owner.AddPart(part); Native(owner);
            }
        }

        [TestCase("Bone", "Handling")] [TestCase("PaleSalt", "MineralPreparation")] [TestCase("ChoirIron", "MineralPreparation")]
        [TestCase("GlowQuartz", "MineralPreparation")] [TestCase("Tepuibone", "PreparationRecipe")]
        [TestCase("KnotflaxCord", "PreparationRecipe")] [TestCase("SalvagedTimber", "PreparationRecipe")]
        [TestCase("GroveRed", "PreparationRecipe")] [TestCase("GroveRed", "Reagent")]
        [TestCase("StoneburrSeed", "Reagent")] [TestCase("FrostLichen", "Reagent")] [TestCase("MendleafSprig", "Reagent")]
        [TestCase("MendleafSprig", "PreparationRecipe")] [TestCase("Mushroom", "Food")] [TestCase("Mushroom", "Cookable")]
        [TestCase("SaltbriarSprig", "Food")] [TestCase("Honeycomb", "Food")] [TestCase("WildBerries", "Food")]
        public void SpecificProductFunctionCannotBeDonatedByForeignDuplicateOrMissingParts(string id, string partName)
        {
            Create(); var owner = Owner(id); Native(owner); var part = owner.GetPart(partName); Assert.NotNull(part);
            part.ParentEntity = new Entity(); Refused(owner); part.ParentEntity = owner; Native(owner);
            var duplicate = (Part)Activator.CreateInstance(part.GetType()); owner.AddPart(duplicate); Refused(owner);
            Assert.True(owner.RemovePart(duplicate)); Native(owner);
            Assert.True(owner.RemovePart(part)); Refused(owner); owner.AddPart(part); Native(owner);
        }

        [TestCase("visual-id")] [TestCase("visual-variant")] [TestCase("glyph-variants")] [TestCase("hidden")]
        [TestCase("not-item")] [TestCase("creature")] [TestCase("terrain")] [TestCase("crop-part")]
        [TestCase("solid")] [TestCase("not-takeable")] [TestCase("empty-stack")] [TestCase("over-stack")]
        [TestCase("negative-max")]
        public void MutatedCommonSemanticsCannotMasqueradeAsNeutralPortableProduct(string fault)
        {
            Create(); var owner = Owner("PaleSalt"); Native(owner);
            var r = owner.GetPart<RenderPart>(); var p = owner.GetPart<PhysicsPart>(); var stack = owner.GetPart<StackerPart>();
            switch (fault)
            {
                case "visual-id": r.VisualID = "foreign"; break; case "visual-variant": r.VisualVariant = "altered"; break;
                case "glyph-variants": r.GlyphVariants = "abc"; break; case "hidden": r.Visible = false; break;
                case "not-item": owner.Tags.Remove("Item"); break; case "creature": owner.SetTag("Creature"); break;
                case "terrain": owner.SetTag("Terrain"); break; case "crop-part": owner.AddPart(new CropPart()); break;
                case "solid": p.Solid = true; break; case "not-takeable": p.Takeable = false; break;
                case "empty-stack": stack.StackCount = 0; break; case "over-stack": stack.StackCount = stack.MaxStack + 1; break;
                case "negative-max": stack.MaxStack = -1; break;
            }
            Refused(owner);
        }

        [TestCase("Mushroom", "missing-food")] [TestCase("Mushroom", "foreign-food")]
        [TestCase("Mushroom", "duplicate-food")] [TestCase("Mushroom", "wet")]
        [TestCase("FireClay", "solid")] [TestCase("FireClay", "visual-variant")]
        public void RefusedReusedProductsCannotFallThroughToWeakerOpeningArt(string id, string fault)
        {
            foreach (var address in new[] { ReferenceGladePlan.ZoneID, Outside })
            {
                world?.Dispose(); Attach(address); world = new FlatPixelGreyboxCatalog(); var owner = Add(id);
                Assert.True(View().NativeForeground, "Establish the native positive before refusal.");
                var food = owner.GetPart<FoodPart>();
                if (fault == "missing-food") Assert.True(owner.RemovePart(food));
                if (fault == "foreign-food") food.ParentEntity = new Entity();
                if (fault == "duplicate-food") owner.AddPart(new FoodPart());
                if (fault == "wet") { Assert.True(owner.ForceApplyEffect(new WetEffect(), null, zone)); Assert.NotNull(owner.GetPart<StatusEffectsPart>().GetRenderColorOverride()); }
                if (fault == "solid") owner.GetPart<PhysicsPart>().Solid = true;
                if (fault == "visual-variant") owner.GetPart<RenderPart>().VisualVariant = "spoof";
                Assert.False(View().NativeForeground, address + ": a stricter product refusal must not fall through to old ItemFrameOf.");
                Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, X, Y, out var fallback, out _));
                Assert.AreNotSame(Imported((int)Row(id)[1]), fallback.texture);
            }
        }

        [TestCase("GroveRed", "vital:5,toxic:2", "vital:3,toxic:0")]
        [TestCase("StoneburrSeed", " BINDING:3 ", "binding:0")]
        [TestCase("FrostLichen", "cold:4", "heat:2")]
        [TestCase("MendleafSprig", "vital:2", "toxic:2")]
        public void ReagentUsesCurrentParsedPositivePropertiesWithoutChangingCacheOrDroppingToxicity(string id, string valid, string invalid)
        {
            Create(); var owner = Owner(id); Native(owner); var reagent = owner.GetPart<ReagentPart>();
            reagent.PropertiesRaw = valid; var cache = reagent.GetProperties(); Native(owner); Assert.AreSame(cache, reagent.GetProperties());
            reagent.PropertiesRaw = invalid; Refused(owner); Assert.AreEqual(invalid, reagent.PropertiesRaw);
            reagent.PropertiesRaw = valid; Native(owner);
        }
        [TestCase("GroveRed", "detox_grove_red")] [TestCase("Tepuibone", "shape_head")]
        [TestCase("SalvagedTimber", "shape_haft")] [TestCase("MendleafSprig", "concentrate_mendleaf")]
        [TestCase("KnotflaxCord", "braid_binding,bandage")]
        public void AuthoredRecipeTokensAreRequiredButPreparationIsNeverExecutedByArt(string id, string expected)
        {
            Create(); var owner = Owner(id); Native(owner); var recipe = owner.GetPart<PreparationRecipePart>();
            Assert.AreEqual(expected, recipe.RecipeIds); int count = owner.GetPart<StackerPart>().StackCount;
            recipe.RecipeIds = "foreign_" + expected.Replace(",", ",foreign_"); Refused(owner);
            recipe.RecipeIds = expected; Native(owner); Assert.AreEqual(count, owner.GetPart<StackerPart>().StackCount);
        }
        [TestCase("SaltbriarSprig")] [TestCase("Honeycomb")] [TestCase("WildBerries")] [TestCase("Mushroom")]
        public void MealAndCookTransitionRemainTheOriginalProductRatherThanAnAlias(string id)
        {
            Create(); var owner = Owner(id); Native(owner); var food = owner.GetPart<FoodPart>(); Assert.AreEqual("Meal", food.Cooking);
            food.Cooking = "foreign"; Refused(owner); food.Cooking = "Meal"; Native(owner);
            if (id == "Mushroom") { var cook = owner.GetPart<CookablePart>(); cook.Into = "CookedMeat"; Refused(owner); cook.Into = "RoastedMushroom"; Native(owner); }
        }
        [Test] public void ProductIdentityCannotBeBorrowedBySourceNodesDerivedItemsOrMatchingPunctuation()
        {
            Create(); Native(Owner("Bone")); Native(Owner("PaleSalt")); Native(Owner("GroveRed"));
            foreach (string id in new[] { "Bones", "PaleSaltVein", "Saltbriar", "CleansedGrovePulp", "RoastedMushroom", "RawMeat", "CandyCarrotSeed", "TepuiboneHeadComponent" })
            { var other = Owner(id); other.GetPart<RenderPart>().RenderString = "*"; other.GetPart<RenderPart>().DisplayName = "pale-salt"; Refused(other); }
            var known = Owner("PaleSalt"); known.BlueprintName = "palesalt"; Refused(known); Assert.False(query(null, out _));
        }
        [Test] public void SingleCellFootprintAndActiveWetOwnerStateAreLiveGates()
        {
            Create(); var owner = Add("PaleSalt"); Native(owner);
            var footprint = new SpatialFootprintPart(); owner.AddPart(footprint); Assert.True(zone.TryChangeFootprint(owner, footprint.CellsRaw)); Native(owner);
            footprint.CellsRaw = "0,0;1,0"; Refused(owner);
            Assert.True(zone.TryChangeFootprint(owner, footprint.CellsRaw)); Refused(owner);
            footprint.CellsRaw = "1,0"; Assert.True(zone.TryChangeFootprint(owner, footprint.CellsRaw)); Refused(owner);
            footprint.CellsRaw = "0,0"; Assert.True(zone.TryChangeFootprint(owner, footprint.CellsRaw)); Native(owner);
            footprint.ParentEntity = new Entity(); Refused(owner); footprint.ParentEntity = owner;
            Assert.True(owner.ForceApplyEffect(new WetEffect(), null, zone)); Assert.NotNull(owner.GetPart<StatusEffectsPart>().GetRenderColorOverride()); Refused(owner);
            Assert.True(owner.RemoveEffect<WetEffect>()); Native(owner);
        }

        [TestCase(BiomeType.Cave)] [TestCase(BiomeType.Desert)] [TestCase(BiomeType.Jungle)] [TestCase(BiomeType.Ruins)]
        [TestCase(BiomeType.Spread)] [TestCase(BiomeType.Sodden)] [TestCase(BiomeType.Beating)]
        [TestCase(BiomeType.Grovelands)] [TestCase(BiomeType.Overwrit)] [TestCase(BiomeType.Stump)]
        public void LegitimatePortableOwnerDoesNotDependOnRegisteredBiomeOrWidenRegionalGroundAdmission(BiomeType biome)
        {
            Create(); manager.WorldMap.Tiles[15, 13] = biome; Add("PaleSalt"); var value = View();
            Assert.True(value.NativeForeground); Frame(value.ForegroundTile, injected[0], 0);
            Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone)); Assert.False(value.LocalGround);
            Assert.True(zone.RemoveEntity(value.Owner)); var terrain = Add("Grass");
            Assert.False(View().NativeForeground); Assert.False(View().LocalGround); Assert.AreSame(terrain, View().Owner);
        }
        [Test] public void UndergroundAndSameAddressReplacementStillRequireTheActualCurrentGraph()
        {
            Create(); Attach("Overworld.15.13.2"); var owner = Add("Bone"); Assert.True(View().NativeForeground);
            var oldZone = zone; Attach(oldZone.ZoneID); Add("Bone"); Assert.True(View().NativeForeground);
            Assert.False(world.TryResolvePresentation(oldZone, X, Y, out _));
            Assert.False(world.TryGetPresentationSubjectSprite(oldZone, owner, X, Y, out _, out _));
        }
        [TestCase("hidden")] [TestCase("fog")] [TestCase("unexplored")] [TestCase("carried")]
        [TestCase("foreign-render")] [TestCase("removed")]
        public void WorldAndPreviouslyAuthorizedReaderCannotLeakLostItemAuthority(string fault)
        {
            Create(); var owner = Add("PaleSalt");
            using (var reader = new Reader(world, zone, Owner("Player")))
            {
                var document = ExamineDocumentBuilder.BuildWorld(owner, zone, zone.GetCell(X, Y), reader.Viewer);
                Portrait(reader, document, injected[0], 0); Assert.True(View().NativeForeground);
                if (fault == "hidden") owner.GetPart<RenderPart>().Visible = false;
                if (fault == "fog") zone.GetCell(X, Y).IsVisible = false;
                if (fault == "unexplored") zone.GetCell(X, Y).Explored = false;
                if (fault == "carried") owner.GetPart<PhysicsPart>().InInventory = new Entity();
                if (fault == "foreign-render") owner.GetPart<RenderPart>().ParentEntity = new Entity();
                if (fault == "removed") Assert.True(zone.RemoveEntity(owner));
                Assert.False(ExamineDocumentBuilder.IsCurrentSubject(document)); Rejected(reader, document);
                if (world.TryResolvePresentation(zone, X, Y, out var value)) Assert.False(value.NativeForeground);
            }
        }
        [Test] public void CoveredProductKeepsAuthorizedExactReaderWithoutReplacingTheSelectedOwner()
        {
            Create(); var product = Add("GroveRed"); var body = Native(product);
            var cover = Add("Dagger"); cover.GetPart<RenderPart>().RenderLayer = 100;
            using (var reader = new Reader(world, zone, Owner("Player")))
            {
                Assert.AreSame(cover, View().Owner); Assert.AreNotSame(body, View().ForegroundTile);
                Portrait(reader, ExamineDocumentBuilder.BuildWorld(product, zone, zone.GetCell(X, Y), reader.Viewer), injected[0], 5);
                Assert.AreSame(cover, View().Owner); Assert.True(zone.RemoveEntity(cover)); Assert.AreSame(body, View().ForegroundTile);
            }
        }
        [Test] public void ActualPickupDropAndRecipientTransferRevokeOldReadersWithoutPhantomProducts()
        {
            Create(); var owner = Add("PaleSalt");
            using (var reader = new Reader(world, zone, Owner("Player")))
            {
                var oldWorld = ExamineDocumentBuilder.BuildWorld(owner, zone, zone.GetCell(X, Y), reader.Viewer);
                Portrait(reader, oldWorld, injected[0], 0);
                Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(owner, false), reader.Viewer, zone).Success);
                Assert.IsNull(zone.GetEntityCell(owner)); Assert.IsNull(View().Owner); Rejected(reader, oldWorld);
                var carried = ExamineDocumentBuilder.BuildInventory(owner, reader.Viewer); Portrait(reader, carried, injected[0], 0);
                Assert.True(InventorySystem.Drop(reader.Viewer, owner, zone)); Rejected(reader, carried);
                var cell = zone.GetEntityCell(owner); Assert.NotNull(cell); Assert.True(zone.MoveEntity(reader.Viewer, 8, Y));
                Frame(View(cell.X, cell.Y).ForegroundTile, injected[0], 0);
                var recipient = Add("Player", cell.X + 1, cell.Y);
                Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(owner, false), recipient, zone).Success);
                Assert.IsNull(ExamineDocumentBuilder.BuildInventory(owner, reader.Viewer));
                Portrait(reader, ExamineDocumentBuilder.BuildInventory(owner, recipient), injected[0], 0);
            }
        }
        [Test] public void RealSplitMergeAndConsumptionKeepCurrentStackIdentityAndEndStaleReader()
        {
            Create();
            using (var reader = new Reader(world, zone, Owner("Player")))
            {
                var owner = Owner("PaleSalt"); var stack = owner.GetPart<StackerPart>(); stack.StackCount = 3; var original = Native(owner);
                var split = stack.SplitStack(1); Assert.NotNull(split); Assert.AreEqual(2, stack.StackCount); Assert.AreEqual(1, split.GetPart<StackerPart>().StackCount);
                Assert.AreSame(original, Native(split)); Assert.AreEqual(1, stack.MergeFrom(split)); Assert.AreEqual(3, stack.StackCount); Refused(split);
                var pack = reader.Viewer.GetPart<InventoryPart>(); Assert.True(pack.AddObject(owner));
                var document = ExamineDocumentBuilder.BuildInventory(owner, reader.Viewer); Portrait(reader, document, injected[0], 0);
                Assert.True(pack.TryConsumeOne(owner)); Assert.AreEqual(2, stack.StackCount); Portrait(reader, document, injected[0], 0);
                Assert.True(pack.TryConsumeOne(owner)); Assert.True(pack.TryConsumeOne(owner));
                Assert.False(ExamineDocumentBuilder.IsCurrentSubject(document)); Rejected(reader, document);
            }
        }
        [TestCase("PaleSaltVein", "PaleSalt")] [TestCase("Bones", "Bone")] [TestCase("Saltbriar", "SaltbriarSprig")]
        public void ActualHarvestProducesExactNativeItemWithoutRewritingItsSourceOrMechanics(string sourceId, string productId)
        {
            Create(); var source = Add(sourceId); Refused(source); var harvest = source.GetPart<HarvestablePart>(); Assert.AreEqual(productId, harvest.YieldBlueprint);
            using (var reader = new Reader(world, zone, Owner("Player")))
            {
                var action = GameEvent.New("InventoryAction");
                try { action.SetParameter("Actor", reader.Viewer); action.SetParameter("Zone", zone); action.SetParameter("Command", "Harvest"); action.SetParameter("Random", new System.Random(11)); source.FireEvent(action); Assert.True(action.Handled); }
                finally { action.Release(); }
                Assert.True(harvest.Harvested); Assert.IsNull(zone.GetEntityCell(source));
                var product = reader.Viewer.GetPart<InventoryPart>().Objects.SingleOrDefault(e => e.BlueprintName == productId);
                Assert.NotNull(product, "Normal successful finite harvest should retain its product in the actual actor inventory.");
                Assert.That(product.GetPart<StackerPart>().StackCount, Is.InRange(harvest.YieldMin, harvest.YieldMax)); Native(product);
                Portrait(reader, ExamineDocumentBuilder.BuildInventory(product, reader.Viewer), injected[0], FrameFor(productId));
                Refused(source);
            }
        }

        [TestCase(0, false)] [TestCase(0, true)] [TestCase(1, false)] [TestCase(1, true)] [TestCase(2, false)] [TestCase(2, true)]
        public void OptionalAtlasFailureIsLocalAndNeverReloadsBehindInjection(int missing, bool malformed)
        {
            Create(); var source = (Texture2D[])injected.Clone(); source[missing] = malformed ? new Texture2D(17, 24) : null;
            if (source[missing] != null) textures.Add(source[missing]); Inject(source);
            foreach (var row in Rows)
            {
                var owner = Add((string)row[0]); bool expected = (int)row[1] != missing;
                Assert.AreEqual(expected, query(owner, out var tile)); Assert.AreEqual(expected, View().NativeForeground);
                if (expected) Frame(tile, injected[(int)row[1]], (int)row[2]); else Assert.IsNull(tile);
                Assert.True(zone.RemoveEntity(owner));
            }
        }
        [Test] public void OptionalCompanionIsLazyAndLegacyInjectedGreyboxDoesNotLoadNewResources()
        {
            var field = typeof(FlatPixelGreyboxCatalog).GetField("_harvestProductsArt", I); Assert.NotNull(field);
            using (var old = new FlatPixelGreyboxCatalog((Texture2D)null))
            { Add("PaleSalt"); Assert.True(old.TryResolvePresentation(zone, X, Y, out var value)); Assert.False(value.NativeForeground); Assert.IsNull(field.GetValue(old)); }
            zone.RemoveEntity(zone.GetCell(X, Y).Objects.Single());
            using (var normal = new FlatPixelGreyboxCatalog())
            { Add("Dagger"); Assert.True(normal.TryResolvePresentation(zone, X, Y, out _)); Assert.IsNull(field.GetValue(normal)); }
        }
        [Test] public void DisposalDestroysOnlyOwnedFramesAndIndependentPeerRetainsBorrowedTextures()
        {
            Create(); var owner = Owner("PaleSalt"); var tile = Native(owner); var sprite = tile.sprite;
            using (var peer = Construct(injected[0], injected[1], injected[2]))
            {
                var q = Resolver(peer); Assert.True(q(owner, out var peerTile)); Assert.AreNotSame(tile, peerTile);
                world.Dispose(); world.Dispose(); Assert.True(tile == null); Assert.True(sprite == null); Assert.False(query(owner, out _));
                Assert.True(q(owner, out var same)); Assert.AreSame(peerTile, same); Frame(same, injected[0], 0);
                foreach (var texture in injected) Assert.True(texture != null);
            }
        }

        [Test] public void SubmittedFullThenDirtyDropAndPickupPaintAndRemoveActualProductWithoutFullRefresh()
        {
            Create(); var actor = Add("Player", X - 1, Y); var product = Owner("PaleSalt"); Assert.True(actor.GetPart<InventoryPart>().AddObject(product));
            using (var paint = new Paint(world, zone))
            {
                paint.Step(); long full = EnvironmentSpriteRenderer.Perf.FullPasses;
                Assert.True(InventorySystem.Drop(actor, product, zone)); var cell = zone.GetEntityCell(product); Assert.NotNull(cell);
                Assert.True(zone.MoveEntity(actor, X - 2, Y));
                // Raw one-cell inventory/Zone commands do not own UI redraw.
                // Submit the actual changed cell through the renderer's dirty API.
                Assert.AreSame(product, View(cell.X, cell.Y).Owner);
                Assert.AreSame(Native(product), View(cell.X, cell.Y).ForegroundTile);
                paint.Dirty(cell.X, cell.Y); Assert.False(paint.FullDirty); paint.Step();
                Frame(paint.At(cell.X, cell.Y), injected[0], 0); Assert.AreEqual(full, EnvironmentSpriteRenderer.Perf.FullPasses);
                Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(product, false), actor, zone).Success);
                Assert.IsNull(zone.GetEntityCell(product)); paint.Dirty(cell.X, cell.Y); Assert.False(paint.FullDirty); paint.Step();
                Assert.IsNull(paint.At(cell.X, cell.Y)); Assert.AreEqual(full, EnvironmentSpriteRenderer.Perf.FullPasses);
            }
        }

        static void Portrait(Reader reader, AnnouncementDocument document, Texture2D texture, int frame)
        {
            Assert.NotNull(document); Assert.True(ExamineDocumentBuilder.IsCurrentSubject(document));
            Assert.True(reader.Presentation.TryGetFlatExaminePortrait(document, out var image));
            Assert.AreSame(texture, image.Texture); Assert.AreEqual(new Rect(frame * 16f / texture.width, 0, 16f / texture.width, 1), image.Uv);
            Assert.AreEqual(Color.white, image.Tint); Assert.False(image.IsModel);
        }
        static void Rejected(Reader reader, AnnouncementDocument document)
        { Assert.NotNull(document); Assert.True(reader.Presentation.TryGetFlatExaminePortrait(document, out var image)); Assert.IsNull(image.Texture); }
        sealed class Reader : IDisposable
        {
            readonly GameObject root;
            public readonly Entity Viewer;
            public readonly ZoneRenderer Presentation;
            public Reader(FlatPixelGreyboxCatalog catalog, Zone z, Entity viewer)
            {
                Viewer = viewer; Assert.True(z.AddEntity(viewer, X - 1, Y));
                root = new GameObject("Exact harvest-product reader"); root.SetActive(false);
                Presentation = root.AddComponent<ZoneRenderer>(); var environment = root.AddComponent<EnvironmentSpriteRenderer>();
                Write(environment, "_flatPixelGreybox", catalog); environment.FlatPixelGreyboxEnabled = true;
                Write(Presentation, "_envSpriteRenderer", environment); Write(Presentation, "_flatPixelModeEnabled", true);
                typeof(ZoneRenderer).GetProperty("CurrentZone", I).SetValue(Presentation, z);
                Assert.True(Presentation.FlatPixelModeActive);
            }
            public void Dispose() { if (root != null) Object.DestroyImmediate(root); }
        }
        static void Write(object owner, string name, object value) => owner.GetType().GetField(name, I).SetValue(owner, value);
        static object Read(object owner, string name) => owner.GetType().GetField(name, I).GetValue(owner);
        // Same bounded submitted ZoneRenderer fixture used by material suites.
        sealed class Paint : IDisposable
        {
            readonly HotbarSaveFixture saved; readonly GameObject root; readonly Tilemap overlay;
            public ZoneRenderer Renderer { get; }
            readonly Action<int, int, string> oldCell = ZoneRenderHooks.CellDirtyCallback;
            readonly Action<string> oldFull = ZoneRenderHooks.FullDirtyCallback;
            readonly FieldInfo bound = typeof(ZoneTileStateSystem).GetField("_boundRenderZone", S);
            readonly object oldBound; readonly Zone previous; readonly Action<int, int> oldChanged; readonly Action oldSight;
            readonly Dictionary<FieldInfo, object> perf = new Dictionary<FieldInfo, object>();
            public Paint(FlatPixelGreyboxCatalog catalog, Zone z)
            {
                oldBound = bound.GetValue(null);
                if (oldBound is WeakReference<Zone> weak && weak.TryGetTarget(out previous)) { oldChanged = previous.TileState.OnCellChanged; oldSight = previous.TileState.OnSightChanged; }
                try
                {
                    foreach (var field in typeof(EnvironmentSpriteRenderer.Perf).GetFields(BindingFlags.Public | BindingFlags.Static)) perf[field] = field.GetValue(null);
                    saved = new HotbarSaveFixture(); root = new GameObject("Harvest submitted grid", typeof(Grid)); saved.Root.transform.SetParent(root.transform, false);
                    var main = saved.Root.GetComponent<Tilemap>(); var bg = new GameObject("Background", typeof(Tilemap)); bg.transform.SetParent(root.transform, false);
                    var mark = new GameObject("Hazards", typeof(Tilemap)); mark.transform.SetParent(root.transform, false);
                    Renderer = saved.Renderer; Write(Renderer, "_tilemap", main); Write(Renderer, "_bgTilemap", bg.GetComponent<Tilemap>()); Write(Renderer, "_tileStateTilemap", mark.GetComponent<Tilemap>());
                    var camera = new GameObject("Camera", typeof(Camera)); camera.transform.SetParent(root.transform, false); camera.GetComponent<Camera>().orthographic = true; Write(Renderer, "_mainCamera", camera.GetComponent<Camera>());
                    var env = saved.Root.AddComponent<EnvironmentSpriteRenderer>(); env.Init(root.transform, main, bg.GetComponent<Tilemap>()); env.RenderingEnabled = true; env.FlatPixelGreyboxEnabled = true;
                    Write(env, "_flatPixelGreybox", catalog); overlay = (Tilemap)Read(env, "_overlayTilemap");
                    Write(Renderer, "_envSpriteRenderer", env); Write(Renderer, "_flatPixelModeEnabled", true); Renderer.SetZone(z);
                    ZoneRenderHooks.CellDirtyCallback = Renderer.MarkCellDirty; ZoneRenderHooks.FullDirtyCallback = Renderer.MarkDirty;
                }
                catch { Dispose(); throw; }
            }
            public bool FullDirty => (bool)Read(Renderer, "_fullDirty");
            public void Dirty(int x, int y) => Renderer.MarkCellDirty(x, y, "harvest product transaction");
            public Tile At(int x, int y) => overlay.GetTile<Tile>(new Vector3Int(x, Zone.Height - 1 - y, 0));
            public void Step() { try { typeof(ZoneRenderer).GetMethod("LateUpdate", I).Invoke(Renderer, null); } catch (TargetInvocationException e) { throw e.InnerException ?? e; } }
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
        void SnapshotStatics(Type type) { foreach (var field in type.GetFields(S)) if (!field.IsLiteral) Snapshot(field); }
        void Snapshot(FieldInfo field)
        {
            Assert.NotNull(field); object value = field.GetValue(null);
            if (value is Array array) { var copy = (Array)array.Clone(); restore.Add(() => Array.Copy(copy, array, copy.Length)); }
            else if (value is IDictionary dictionary)
            {
                var copy = new List<DictionaryEntry>(); foreach (DictionaryEntry entry in dictionary) copy.Add(entry);
                restore.Add(() => { dictionary.Clear(); foreach (var entry in copy) dictionary.Add(entry.Key, entry.Value); });
            }
            else if (value is IList list) { var copy = list.Cast<object>().ToArray(); restore.Add(() => { list.Clear(); foreach (var item in copy) list.Add(item); }); }
            else if (value is IEnumerable enumerable && value.GetType().IsGenericType && value.GetType().GetGenericTypeDefinition() == typeof(Queue<>))
            {
                var copy = enumerable.Cast<object>().ToArray(); restore.Add(() => { value.GetType().GetMethod("Clear").Invoke(value, null);
                    foreach (var item in copy) value.GetType().GetMethod("Enqueue").Invoke(value, new[] { item }); });
            }
            if (!field.IsInitOnly) restore.Add(() => field.SetValue(null, value));
        }
    }
}
