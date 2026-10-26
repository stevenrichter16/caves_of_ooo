using System;
using System.Collections.Generic;
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
    public sealed class FlatPixelEastKitchenTests
    {
        const string East = "Overworld.12.10.0", Kitchen = "Overworld.12.11.0";
        const string Resource = "Sprites/Live16x24/east-kitchen", CookResource = "Sprites/Live16x24/east-kitchen-cook";
        const int X = 10, Y = 10;
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly string[] Frames = { "TinkersForge", "Oven", "Bed", "Chair", "ConnectedBatchPan", "ConnectedBatchPan",
            "ConnectedKitchenEscrow", "ConnectedKitchenPickup", "ClaspbeanCrop", "ClaspbeanCrop", "ClaspbeanCrop",
            "ClaspbeanPulp", "ClaspbeanSeed", "FieldMeal", "CharmFlowers", "CopperPipe", "SteamVent", "MillStone" };
        delegate bool Query(Entity owner, out Tile tile);
        DensityLootTestScope content;
        EntityFactory Factory => content.Factory;
        Texture2D atlas, cook;
        IDisposable adapter;
        Query resolve;
        FlatPixelGreyboxCatalog world;
        OverworldZoneManager manager;

        [SetUp] public void Setup()
        {
            content = new DensityLootTestScope();
            atlas = new Texture2D(288, 24); cook = new Texture2D(16, 24);
            manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
        }
        [TearDown] public void Cleanup()
        {
            world?.Dispose(); adapter?.Dispose();
            if (atlas != null) Object.DestroyImmediate(atlas);
            if (cook != null) Object.DestroyImmediate(cook);
            content?.Dispose();
        }
        static Type AdapterType()
        {
            var type = typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelEastKitchenCatalog");
            Assert.NotNull(type, "The optional east/kitchen companion is not implemented."); return type;
        }
        static IDisposable Construct(Texture2D strip, Texture2D cookArt)
        {
            var ctor = AdapterType().GetConstructor(new[] { typeof(Texture2D), typeof(Texture2D) }); Assert.NotNull(ctor);
            return (IDisposable)ctor.Invoke(new object[] { strip, cookArt });
        }
        static Query Resolver(IDisposable art)
        {
            var method = art.GetType().GetMethod("TryResolve", I); Assert.NotNull(method);
            return (Query)Delegate.CreateDelegate(typeof(Query), art, method);
        }
        void Create(bool injectWorld = false)
        {
            adapter = Construct(atlas, cook); resolve = Resolver(adapter);
            if (injectWorld)
            {
                world = new FlatPixelGreyboxCatalog((Texture2D)null);
                Set(world, "_eastKitchenArt", adapter); // Ownership transfers to this test's disposable world catalog.
            }
        }
        Entity Owner(int frame)
        {
            var owner = Factory.CreateEntity(frame == 18 ? "SpreadWaysideCook" : Frames[frame]); Assert.NotNull(owner);
            if (frame == 1) owner.AddPart(new CampfirePart { FiniteCooking = false, AllowRest = false });
            if (frame == 5) owner.GetPart<RepairablePart>().Repaired = true;
            if (frame >= 8 && frame <= 10) owner.GetPart<CropPart>().GrowthStage = frame - 8;
            return owner;
        }
        static void Expect(Tile tile, Texture2D texture, int frame)
        {
            Assert.NotNull(tile); Assert.NotNull(tile.sprite); Assert.AreSame(texture, tile.sprite.texture);
            Assert.AreEqual(new Rect(frame * 16, 0, 16, 24), tile.sprite.rect);
            Assert.AreEqual(new Vector2(8, 12), tile.sprite.pivot); Assert.AreEqual(16, tile.sprite.pixelsPerUnit);
            Assert.AreEqual(TileFlags.None, tile.flags); Assert.AreEqual(Tile.ColliderType.None, tile.colliderType);
        }
        Tile Frame(Entity owner, int frame)
        {
            Assert.True(resolve(owner, out var tile), owner.BlueprintName);
            Expect(tile, frame == 18 ? cook : atlas, frame == 18 ? 0 : frame); return tile;
        }
        Zone Attach(string id, OverworldZoneManager owner = null)
        {
            owner = owner ?? manager; var zone = new Zone(id); owner.SetActiveZone(zone);
            for (int y = Y - 2; y <= Y + 2; y++) for (int x = X - 2; x <= X + 2; x++)
                zone.GetCell(x, y).Explored = zone.GetCell(x, y).IsVisible = true;
            return zone;
        }

        [TestCase(East)] [TestCase(Kitchen)]
        public void ExactCurrentGraphsUseTheirOwnColumnTwelvePoiAndSurviveSiteRemoval(string id)
        {
            var zone = Attach(id); var at = WorldMap.FromZoneID(id);
            Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            manager.WorldMap.SetPOI(11, at.y, new PointOfInterest(POIType.Village, "other column", null, 1));
            Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone), "Column11 is not the new site's address.");
            manager.WorldMap.SetPOI(12, at.y, new PointOfInterest(POIType.Village, "actual site", null, 1));
            Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            manager.WorldMap.SetPOI(12, at.y, null); Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            var owner = Owner(id == East ? 0 : 18); Assert.True(zone.AddEntity(owner, X, Y)); Assert.True(zone.RemoveEntity(owner));
            Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone)); Assert.Zero(zone.EntityCount);
            manager.WorldMap.Tiles[at.x, at.y] = BiomeType.Sodden; Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone));
        }
        [TestCase(East)] [TestCase(Kitchen)]
        public void SameAddressWithoutTheCurrentGraphDoesNotAdmitButLegacyExplorationDoes(string id)
        {
            Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(new Zone(id)));
            var legacy = OverworldZoneManager.CreateDetached(Factory, 64, false); var zone = Attach(id, legacy);
            Assert.False(legacy.Exploration.Enabled); Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            legacy.CachedZones[id] = new Zone(id); Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(legacy.CachedZones[id]));
        }
        [Test] public void OtherAddressesStayUnsupportedAndEastBorrowsOnlyLivePureWater()
        {
            foreach (var id in new[] { "Overworld.13.10.0", "Overworld.12.12.0", "Overworld.12.10.1", "Overworld.012.11.0" })
                Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(Attach(id)), id);
            var zone = Attach(East);
            using (var selector = new FlatPixelFallbackCatalog())
            {
                var ground = new Texture2D(336, 24);
                try
                {
                    using (var art = new FlatPixelOpeningGroundCatalog(ground, selector))
                    {
                        Assert.False(art.TryResolve(zone, X, Y, out _, out _), "East does not borrow dry glade ground.");
                        zone.TileState.WriteCoating(X, Y, "water", 5);
                        Assert.True(art.TryResolve(zone, X, Y, out _, out _), "Existing East water continuity remains available.");
                        zone.TileState.WriteCoating(X, Y, "oil", 5);
                        Assert.False(art.TryResolve(zone, X, Y, out _, out _), "Mixed liquids retain their hazard presentation.");
                        zone.TileState.RemoveCoating(X, Y, "oil");
                        zone.TileState.RemoveCoating(X, Y, "water");
                        Assert.False(art.TryResolve(zone, X, Y, out _, out _));
                    }
                }
                finally { Object.DestroyImmediate(ground); }
            }
        }
        [Test] public void EveryAuthoredFrameIsDistinctCachedAndDoesNotMutateItsOwner()
        {
            Create(); var seen = new HashSet<Tile>();
            for (int i = 0; i <= 18; i++)
            {
                var owner = Owner(i); var physics = owner.GetPart<PhysicsPart>(); bool solid = physics.Solid;
                int parts = owner.Parts.Count; string glyph = owner.GetPart<RenderPart>().RenderString;
                var tile = Frame(owner, i); Assert.True(seen.Add(tile)); Assert.AreSame(tile, Frame(owner, i));
                Assert.AreEqual(solid, physics.Solid); Assert.AreEqual(parts, owner.Parts.Count); Assert.AreEqual(glyph, owner.GetPart<RenderPart>().RenderString);
                owner.GetPart<RenderPart>().Visible = false; Assert.False(resolve(owner, out _));
            }
            Assert.AreEqual(FilterMode.Bilinear, atlas.filterMode); Assert.AreEqual(TextureWrapMode.Repeat, atlas.wrapMode);
        }
        [TestCase("visual-id")] [TestCase("visual-variant")] [TestCase("glyph-variants")]
        [TestCase("foreign-render")] [TestCase("foreign-physics")] [TestCase("foreign-identity")]
        public void AuthoredOverridesAndForeignOwnersNeverBorrowTheNewArt(string fault)
        {
            Create();
            for (int i = 0; i <= 18; i++)
            {
                var owner = Owner(i); Frame(owner, i); var render = owner.GetPart<RenderPart>();
                if (fault == "visual-id") render.VisualID = "custom-owner";
                if (fault == "visual-variant") render.VisualVariant = "custom-costume";
                if (fault == "glyph-variants") render.GlyphVariants = ":;";
                if (fault == "foreign-render") render.ParentEntity = new Entity();
                if (fault == "foreign-physics") owner.GetPart<PhysicsPart>().ParentEntity = new Entity();
                if (fault == "foreign-identity") owner.BlueprintName = "UnknownOwner";
                Assert.False(resolve(owner, out _), Frames[Math.Min(i, 17)] + ": " + fault);
            }
        }
        [Test] public void FunctionalPartsRequireExactOwnershipAndMissingPartsFallBack()
        {
            Create();
            int[] frames = { 0, 1, 2, 3, 4, 4, 4, 6, 7, 9, 11, 11, 12, 13, 15, 16, 16, 16, 17 };
            string[] parts = { "Forge", "Campfire", "Bed", "Chair", "Repairable", "Composition", "KitchenBatch", "Container", "Container", "Crop", "Tonic", "CureTonic", "Seed", "FieldMeal", "Material", "Material", "Thermal", "TileStateSource", "Handling" };
            for (int n = 0; n < frames.Length; n++)
            {
                var owner = Owner(frames[n]); Frame(owner, frames[n]); var part = owner.GetPart(parts[n]); Assert.NotNull(part);
                part.ParentEntity = new Entity(); Assert.False(resolve(owner, out _), parts[n]); part.ParentEntity = owner;
                Frame(owner, frames[n]); Assert.True(owner.RemovePart(part)); Assert.False(resolve(owner, out _), "Missing " + parts[n]);
            }
        }
        [Test] public void NeutralFurnitureAndCookDoNotInventAvailabilityOrHeatState()
        {
            Create(); var resident = Owner(18); var sprite = Frame(resident, 18);
            resident.GetPart<RenderPart>().DisplayName = "Renamed cook"; resident.GetStat("Hitpoints").BaseValue = 1;
            Assert.AreSame(sprite, Frame(resident, 18)); resident.Tags.Remove("Creature"); Assert.False(resolve(resident, out _));
            var bed = Owner(2); var chair = Owner(3); bed.GetPart<BedPart>().Owner = "someone"; bed.GetPart<BedPart>().Occupied = true;
            chair.GetPart<ChairPart>().Occupied = true; Frame(bed, 2); Frame(chair, 3);
            var oven = Owner(1); Frame(oven, 1); Assert.IsNull(oven.GetPart<FuelPart>()); Assert.IsNull(oven.GetPart<ThermalPart>());
            oven.GetPart<CampfirePart>().FiniteCooking = true; Assert.False(resolve(oven, out _));
            var vent = Owner(16); var ventTile = Frame(vent, 16); vent.GetPart<ThermalPart>().Temperature = -50;
            Assert.AreSame(ventTile, Frame(vent, 16));
            foreach (int i in new[] { 6, 7 })
            {
                var tray = Owner(i); var tile = Frame(tray, i); tray.GetPart<ContainerPart>().Locked = true;
                Assert.True(tray.GetPart<ContainerPart>().AddItem(Factory.CreateEntity("FieldMeal")));
                Assert.AreSame(tile, Frame(tray, i), "Neutral tray must not invent ready/access state.");
            }
        }
        [Test] public void RepairPairUsesTheOwnedMasonryFaultNotJobStringsOrCombatHp()
        {
            Create(); var pan = Owner(4); var repair = pan.GetPart<RepairablePart>(); Frame(pan, 4);
            pan.GetPart<KitchenBatchPart>().State = "Ready"; Frame(pan, 4);
            repair.Repaired = true; Frame(pan, 5); repair.PortableEquipment = true; Assert.False(resolve(pan, out _)); repair.PortableEquipment = false;
            repair.RecipeId = "clay-well-lining"; Assert.False(resolve(pan, out _)); repair.RecipeId = "clay-batch-pan";
            pan.GetPart<CompositionPart>().MaterialsRaw = "Iron"; Assert.False(resolve(pan, out _)); pan.GetPart<CompositionPart>().MaterialsRaw = "Masonry";
            Frame(pan, 5); pan.AddPart(new RepairablePart { RecipeId = repair.RecipeId }); Assert.False(resolve(pan, out _));
        }
        [Test] public void CropStagesAndProductsRetainTheirActualNonFoodContracts()
        {
            Create(); var plant = Owner(8); var crop = plant.GetPart<CropPart>();
            for (int stage = 0; stage <= 2; stage++) { crop.GrowthStage = stage; crop.MoistureTicks = 0; Frame(plant, 8 + stage); }
            crop.MoistureTicks = 100; crop.TicksPerStage = 77; Frame(plant, 10);
            crop.GrowthStage = 3; Assert.False(resolve(plant, out _)); crop.GrowthStage = 2;
            crop.HarvestAtMaturity = false; Assert.False(resolve(plant, out _)); crop.HarvestAtMaturity = true;
            crop.YieldBlueprint = "MarlrootClod"; Assert.False(resolve(plant, out _)); crop.YieldBlueprint = "ClaspbeanPulp";
            crop.SeedYieldBlueprint = "MarlrootSeed"; Assert.False(resolve(plant, out _)); crop.SeedYieldBlueprint = "ClaspbeanSeed";
            crop.YieldCount = 0; Assert.False(resolve(plant, out _)); crop.YieldCount = 2; crop.SeedYieldCount = 0; Assert.False(resolve(plant, out _));
            var pulp = Owner(11); Assert.IsNull(pulp.GetPart<FoodPart>()); Frame(pulp, 11);
            pulp.GetPart<TonicPart>().Drink = true; Assert.False(resolve(pulp, out _)); pulp.GetPart<TonicPart>().Drink = false;
            pulp.GetPart<CureTonicPart>().CureEffect = nameof(BurningEffect); Assert.False(resolve(pulp, out _));
            var seed = Owner(12); Frame(seed, 12); seed.GetPart<SeedPart>().RequireCultivatedSoil = false; Assert.False(resolve(seed, out _));
            var meal = Owner(13); Assert.IsNull(meal.GetPart<FoodPart>()); Frame(meal, 13);
        }
        [Test] public void StructuralCustodyAndProductStackCountersRemainDistinct()
        {
            Create();
            foreach (int i in new[] { 0, 4, 6, 14, 15, 16, 17, 18 })
            {
                var owner = Owner(i); Frame(owner, i); var p = owner.GetPart<PhysicsPart>(); p.InInventory = new Entity();
                Assert.False(resolve(owner, out _)); p.InInventory = null; p.Equipped = new Entity(); Assert.False(resolve(owner, out _));
            }
            foreach (int i in new[] { 11, 12, 13 })
            {
                var owner = Owner(i); Frame(owner, i); var stack = owner.GetPart<StackerPart>();
                if (stack == null) { stack = new StackerPart(); owner.AddPart(stack); }
                stack.StackCount = 0; Assert.False(resolve(owner, out _)); stack.StackCount = 1; Frame(owner, i);
                stack.ParentEntity = new Entity(); Assert.False(resolve(owner, out _));
            }
        }
        [Test] public void PhysicalRolesAndMaterialSpeciesCannotMasqueradeAsTheAuthoredObjects()
        {
            Create();
            for (int i = 0; i <= 18; i++)
            {
                var owner = Owner(i); Frame(owner, i); var physics = owner.GetPart<PhysicsPart>();
                bool portable = physics.Takeable; physics.Takeable = !portable; Assert.False(resolve(owner, out _), owner.BlueprintName); physics.Takeable = portable;
                if (i != 18) { bool solid = physics.Solid; physics.Solid = !solid; Assert.False(resolve(owner, out _), owner.BlueprintName); physics.Solid = solid; }
                Frame(owner, i); owner.SetTag(i == 18 ? "Item" : "Creature"); Assert.False(resolve(owner, out _));
            }
            foreach (int i in new[] { 15, 16 })
            { var owner = Owner(i); Frame(owner, i); owner.GetPart<MaterialPart>().MaterialID = "Iron"; Assert.False(resolve(owner, out _)); }
        }
        [Test] public void WorldAndExactSubjectShareNativeFramesButNotWorldOrFogAuthority()
        {
            Create(true); var zone = Attach(Kitchen); var owner = Owner(18); Assert.True(zone.AddEntity(owner, X, Y));
            Assert.True(world.TryResolvePresentation(zone, X, Y, out var visible)); Assert.AreSame(owner, visible.Owner);
            Assert.True(visible.NativeForeground); Expect(visible.ForegroundTile, cook, 0); Assert.AreEqual(Color.white, visible.ForegroundColor);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, X, Y, out var sprite, out var tint)); Assert.AreSame(visible.ForegroundTile.sprite, sprite); Assert.AreEqual(Color.white, tint);
            var cover = Factory.CreateEntity("Player"); cover.GetPart<RenderPart>().RenderLayer = 100; Assert.True(zone.AddEntity(cover, X, Y));
            Assert.True(world.TryResolvePresentation(zone, X, Y, out var covered)); Assert.AreSame(cover, covered.Owner); Assert.AreNotSame(cook, covered.ForegroundTile.sprite.texture);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, X, Y, out sprite, out _)); Assert.AreSame(cook, sprite.texture);
            zone.GetCell(X, Y).IsVisible = false; Assert.True(world.TryResolvePresentation(zone, X, Y, out var memory)); Assert.IsNull(memory.Owner); Assert.IsNull(memory.ForegroundTile);
            zone.GetCell(X, Y).Explored = false; Assert.False(world.TryResolvePresentation(zone, X, Y, out _));
            manager.CachedZones[Kitchen] = new Zone(Kitchen); Assert.False(world.TryGetPresentationSubjectSprite(zone, owner, X, Y, out _, out _));
        }
        [Test] public void NewArtIsLocalAndStaleCellReferencesCannotRetainIt()
        {
            Create(true); var zone = Attach(East); var owner = Owner(0); Assert.True(zone.AddEntity(owner, X, Y));
            Assert.True(world.TryResolvePresentation(zone, X, Y, out var positive)); Assert.True(positive.NativeForeground);
            Assert.True(zone.MoveEntity(owner, X + 1, Y)); zone.GetCell(X, Y).Objects.Add(owner);
            Assert.True(world.TryResolvePresentation(zone, X, Y, out var stale)); Assert.IsNull(stale.Owner);
            Assert.True(world.TryResolvePresentation(zone, X + 1, Y, out var moved)); Assert.AreSame(owner, moved.Owner); Assert.True(moved.NativeForeground);
            var old = zone.GetCell(X + 1, Y); zone.Cells[X + 1, Y] = new Cell(X + 1, Y, zone) { IsVisible = true, Explored = true };
            zone.Cells[X + 1, Y].Objects.Add(owner); Assert.True(world.TryResolvePresentation(zone, X + 1, Y, out var replaced)); Assert.IsNull(replaced.Owner);
            var glade = Attach(ReferenceGladePlan.ZoneID); Assert.True(world.TryGetPresentationSubjectSprite(glade, owner, X, Y, out var foreignSprite, out _)); Assert.AreNotSame(atlas, foreignSprite.texture);
        }
        [Test] public void StaticTerrainMemoryDimsOnceWhileServiceAndCropStateStayHidden()
        {
            Create(true); var zone = Attach(East); var pipe = Owner(15); Assert.True(zone.AddEntity(pipe, X, Y));
            Assert.True(world.TryResolvePresentation(zone, X, Y, out var visible)); Expect(visible.ForegroundTile, atlas, 15);
            zone.GetCell(X, Y).IsVisible = false; Assert.True(world.TryResolvePresentation(zone, X, Y, out var memory));
            Assert.AreSame(visible.ForegroundTile, memory.ForegroundTile); Assert.AreEqual(new Color(.35f, .35f, .35f, 1), memory.ForegroundColor);
            foreach (int i in new[] { 4, 9, 18 })
            {
                var cell = zone.GetCell(X + 1, Y); cell.Explored = cell.IsVisible = true; var owner = Owner(i); Assert.True(zone.AddEntity(owner, X + 1, Y));
                Assert.True(world.TryResolvePresentation(zone, X + 1, Y, out var current)); Assert.AreSame(owner, current.Owner);
                cell.IsVisible = false; Assert.True(world.TryResolvePresentation(zone, X + 1, Y, out var hidden)); Assert.IsNull(hidden.Owner);
                Assert.True(zone.RemoveEntity(owner));
            }
        }
        [Test] public void MissingAndMalformedOptionalGroupsRemainIndependentAndDoNotLoadResources()
        {
            using (var noCook = Construct(atlas, null)) { var query = Resolver(noCook); Assert.True(query(Owner(0), out _)); Assert.False(query(Owner(18), out _)); }
            var malformed = new Texture2D(287, 24);
            try { using (var onlyCook = Construct(malformed, cook)) { var query = Resolver(onlyCook); Assert.False(query(Owner(0), out _)); Assert.True(query(Owner(18), out var tile)); Expect(tile, cook, 0); } }
            finally { Object.DestroyImmediate(malformed); }
            using (var neither = Construct(null, null)) Assert.False(Resolver(neither)(Owner(0), out _));
            using (var old = new FlatPixelGreyboxCatalog((Texture2D)null))
            {
                var zone = Attach(Kitchen); var owner = Owner(18); Assert.True(zone.AddEntity(owner, X, Y));
                Assert.True(old.TryResolvePresentation(zone, X, Y, out var fallback)); Assert.False(fallback.NativeForeground);
            }
        }
        [Test] public void GeneratedObjectsDisposeIndependentlyButBorrowedTexturesSurvive()
        {
            Create(); var tile = Frame(Owner(0), 0); var sprite = tile.sprite;
            using (var second = Construct(atlas, cook))
            {
                var query = Resolver(second); Assert.True(query(Owner(0), out var other)); Assert.AreNotSame(tile, other);
                adapter.Dispose(); adapter.Dispose(); Assert.True(tile == null); Assert.True(sprite == null);
                Assert.False(resolve(Owner(0), out _)); Assert.True(query(Owner(0), out _)); Assert.True(atlas != null); Assert.True(cook != null);
            }
        }
        [Test] public void WarmSemanticQueriesAllocateNothingAndNeverAdvanceCropTime()
        {
            Create(); var owner = Owner(9); var crop = owner.GetPart<CropPart>(); crop.TicksInStage = 3;
            for (int i = 0; i < 50; i++) resolve(owner, out _);
            long before = GC.GetAllocatedBytesForCurrentThread(); int count = 0;
            for (int i = 0; i < 1000; i++) if (resolve(owner, out _)) count++;
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.AreEqual(1000, count); Assert.Zero(allocated); Assert.AreEqual(3, crop.TicksInStage);
        }
        [Test] public void DefaultResourcesBindEveryFrameAndTheApprovedFullCookCanvas()
        {
            var actual = Resources.Load<Texture2D>(Resource); var actualCook = Resources.Load<Texture2D>(CookResource);
            Assert.NotNull(actual, "Actual east/kitchen atlas has not been imported."); Assert.NotNull(actualCook);
            Assert.AreEqual(288, actual.width); Assert.AreEqual(24, actual.height); Assert.AreEqual(16, actualCook.width); Assert.AreEqual(24, actualCook.height);
            foreach (var texture in new[] { actual, actualCook }) { Assert.AreEqual(FilterMode.Point, texture.filterMode); Assert.AreEqual(TextureWrapMode.Clamp, texture.wrapMode); Assert.AreEqual(1, texture.mipmapCount); }
            using (var native = (IDisposable)Activator.CreateInstance(AdapterType()))
            {
                var query = Resolver(native); for (int i = 0; i <= 18; i++) { Assert.True(query(Owner(i), out var tile)); Expect(tile, i == 18 ? actualCook : actual, i == 18 ? 0 : i); }
            }
            using (var nativeWorld = new FlatPixelGreyboxCatalog())
            {
                var zone = Attach(Kitchen); var owner = Owner(18); Assert.True(zone.AddEntity(owner, X, Y));
                Assert.True(nativeWorld.TryResolvePresentation(zone, X, Y, out var result)); Expect(result.ForegroundTile, actualCook, 0);
                Assert.True(nativeWorld.TryGetPresentationSubjectSprite(zone, owner, X, Y, out var portrait, out _)); Assert.AreSame(actualCook, portrait.texture);
            }
        }
        [Test] public void RealPanRepairChangesTheSubmittedTileOnceWithoutManualRedraw()
        {
            using (var f = new PaintFixture(Attach(Kitchen)))
            {
                var art = Construct(atlas, cook); Set(f.Catalog, "_eastKitchenArt", art);
                var pan = Owner(4); var player = Factory.CreateEntity("Player"); player.GetPart<InventoryPart>().MaxWeight = 200;
                Assert.True(f.Zone.AddEntity(pan, X, Y)); Assert.True(f.Zone.AddEntity(player, X - 1, Y));
                var clay = Factory.CreateEntity("FireClay"); clay.GetPart<StackerPart>().StackCount = 2;
                Assert.True(player.GetPart<InventoryPart>().AddObject(clay));
                f.Renderer.MarkDirty("Initial pan fixture"); f.Frame(); f.Quiet(); Expect(f.ForegroundAt(X, Y), atlas, 4);
                Assert.NotNull(RepairRecipeRegistry.Get("clay-batch-pan"));
                Assert.True(pan.GetPart<RepairablePart>().TryRepair(player, f.Zone)); Assert.True(f.Dirty.Contains(Y * Zone.Width + X));
                long full = EnvironmentSpriteRenderer.Perf.FullPasses; f.Frame(); f.Quiet(); Expect(f.ForegroundAt(X, Y), atlas, 5);
                Assert.AreEqual(full, EnvironmentSpriteRenderer.Perf.FullPasses); Assert.False(player.GetPart<InventoryPart>().Objects.Contains(clay));
                Assert.False(pan.GetPart<RepairablePart>().TryRepair(player, f.Zone)); f.Quiet();
                long resolved = EnvironmentSpriteRenderer.Perf.CellsResolved; f.Frame(); Assert.AreEqual(resolved, EnvironmentSpriteRenderer.Perf.CellsResolved);
            }
        }
        static object Get(object owner, string name) => owner.GetType().GetField(name, I).GetValue(owner);
        static void Set(object owner, string name, object value)
        { var field = owner.GetType().GetField(name, I); Assert.NotNull(field, "Missing integration seam: " + name); field.SetValue(owner, value); }

        sealed class PaintFixture : IDisposable
        {
            readonly HotbarSaveFixture saved;
            readonly GameObject grid;
            readonly FieldInfo bound = typeof(ZoneTileStateSystem).GetField("_boundRenderZone", BindingFlags.Static | BindingFlags.NonPublic);
            readonly object oldBound; readonly Zone previous; readonly Action<int, int> oldChanged; readonly Action oldSight;
            readonly Action<int, int, string> oldCell = ZoneRenderHooks.CellDirtyCallback;
            readonly Action<string> oldFull = ZoneRenderHooks.FullDirtyCallback;
            readonly List<MessageLog.Entry> messages = MessageLog.GetAllEntries();
            readonly List<string> announcements = MessageLog.GetPendingAnnouncementsSnapshot();
            readonly int flash = MessageLog.FlashStamp, serial = MessageLog.NextSerialValue;
            readonly Action<string> oldMessage = MessageLog.OnMessage; readonly Func<int> oldTick = MessageLog.TickProvider;
            readonly Dictionary<FieldInfo, object> perf = new Dictionary<FieldInfo, object>();
            readonly Tilemap overlay;
            public readonly FlatPixelGreyboxCatalog Catalog; public readonly Zone Zone; public readonly ZoneRenderer Renderer;
            public HashSet<int> Dirty => (HashSet<int>)Get(Renderer, "_dirtyCells");
            public PaintFixture(Zone zone)
            {
                oldBound = bound.GetValue(null);
                if (oldBound is WeakReference<Zone> weak && weak.TryGetTarget(out previous))
                { oldChanged = previous.TileState.OnCellChanged; oldSight = previous.TileState.OnSightChanged; }
                try
                {
                    foreach (var field in typeof(EnvironmentSpriteRenderer.Perf).GetFields(BindingFlags.Public | BindingFlags.Static)) perf[field] = field.GetValue(null);
                    MessageLog.OnMessage = null; MessageLog.TickProvider = null; saved = new HotbarSaveFixture();
                    grid = new GameObject("East kitchen Grid", typeof(Grid)); saved.Root.transform.SetParent(grid.transform, false);
                    var main = saved.Root.GetComponent<Tilemap>(); Assert.NotNull(main);
                    var bg = new GameObject("East kitchen background", typeof(Tilemap)); bg.transform.SetParent(saved.Root.transform, false);
                    Renderer = saved.Renderer; Set(Renderer, "_tilemap", main); Set(Renderer, "_bgTilemap", bg.GetComponent<Tilemap>());
                    var camera = new GameObject("East kitchen camera", typeof(Camera)); camera.transform.SetParent(saved.Root.transform, false);
                    camera.GetComponent<Camera>().orthographic = true; Set(Renderer, "_mainCamera", camera.GetComponent<Camera>());
                    var env = saved.Root.AddComponent<EnvironmentSpriteRenderer>(); env.Init(saved.Root.transform, main, bg.GetComponent<Tilemap>()); env.RenderingEnabled = true;
                    Catalog = new FlatPixelGreyboxCatalog((Texture2D)null); Set(env, "_flatPixelGreybox", Catalog); overlay = (Tilemap)Get(env, "_overlayTilemap");
                    Set(Renderer, "_envSpriteRenderer", env); Set(Renderer, "_flatPixelModeEnabled", true); Zone = zone; Renderer.SetZone(zone);
                    ZoneRenderHooks.CellDirtyCallback = Renderer.MarkCellDirty; ZoneRenderHooks.FullDirtyCallback = Renderer.MarkDirty;
                    Frame(); Quiet();
                }
                catch { Dispose(); throw; }
            }
            public Tile ForegroundAt(int x, int y) => overlay.GetTile<Tile>(new Vector3Int(x, CavesOfOoo.Core.Zone.Height - 1 - y, 0));
            public void Frame() { try { typeof(ZoneRenderer).GetMethod("LateUpdate", I).Invoke(Renderer, null); } catch (TargetInvocationException ex) { throw ex.InnerException ?? ex; } }
            public void Quiet() { Assert.False((bool)Get(Renderer, "_fullDirty")); Assert.Zero(Dirty.Count); }
            public void Dispose()
            {
                try { saved?.Dispose(); if (grid != null) Object.DestroyImmediate(grid); }
                finally
                {
                    Catalog?.Dispose(); ZoneRenderHooks.CellDirtyCallback = oldCell; ZoneRenderHooks.FullDirtyCallback = oldFull; bound.SetValue(null, oldBound);
                    if (previous != null) { previous.TileState.OnCellChanged = oldChanged; previous.TileState.OnSightChanged = oldSight; }
                    foreach (var entry in perf) entry.Key.SetValue(null, entry.Value);
                    MessageLog.Restore(messages, announcements, flash, serial); MessageLog.OnMessage = oldMessage; MessageLog.TickProvider = oldTick;
                }
            }
        }
    }
}
