using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Tests precede production. Shipped world/reader seams establish visible RED;
    // the optional companion is reflected so its absence is an assertion failure.
    public sealed class FlatPixelAftermathTests
    {
        const string ResourcePath = "Sprites/Live16x24/opening-aftermath";
        const string Garden = "Overworld.11.8.0";
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        delegate bool Query(Entity owner, out Tile tile);
        DensityLootTestScope content;
        OverworldZoneManager manager;
        Zone zone;
        FlatPixelGreyboxCatalog world;
        Texture2D atlas;
        IDisposable adapter;
        Query query;

        [SetUp] public void Setup()
        {
            content = new DensityLootTestScope();
            manager = OverworldZoneManager.CreateDetached(content.Factory, 65, true);
            zone = Attach(Garden);
            atlas = new Texture2D(48, 24);
            ZoneRenderHooks.CellDirtyCallback = null; ZoneRenderHooks.FullDirtyCallback = null;
        }
        [TearDown] public void Cleanup()
        {
            try { world?.Dispose(); adapter?.Dispose(); if (atlas != null) Object.DestroyImmediate(atlas); }
            finally { world = null; adapter = null; atlas = null; query = null; content?.Dispose(); }
        }
        Zone Attach(string id)
        {
            var target = new Zone(id); manager.SetActiveZone(target);
            for (int y = 8; y <= 12; y++) for (int x = 8; x <= 12; x++)
                target.GetCell(x, y).IsVisible = target.GetCell(x, y).Explored = true;
            return target;
        }
        Entity Owner(int frame)
        {
            var owner = content.Factory.CreateEntity(frame == 2 ? "VenomGland" : "CreatureCorpse");
            Assert.NotNull(owner);
            if (frame < 2)
            {
                owner.Properties["SourceBlueprint"] = frame == 0 ? "Magpie" : "Viper";
                owner.Properties["SourceID"] = "deceased-source-not-in-any-live-zone";
                Assert.True(owner.HasTag("Corpse")); Assert.False(owner.HasTag("Item"));
            }
            return owner;
        }
        Entity Add(int frame) { var owner = Owner(frame); Assert.True(zone.AddEntity(owner, 10, 10)); return owner; }
        FlatPixelGreyboxCell Resolve()
        { Assert.True(world.TryResolvePresentation(zone, 10, 10, out var result)); return result; }
        static Type AdapterType()
        {
            var type = typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelAftermathCatalog");
            Assert.NotNull(type, "The optional exact-source aftermath adapter is absent."); return type;
        }
        static IDisposable Construct(Texture2D texture)
            => (IDisposable)Activator.CreateInstance(AdapterType(), new object[] { texture });
        static Query Resolver(IDisposable art) => (Query)Delegate.CreateDelegate(typeof(Query), art,
            AdapterType().GetMethod("TryResolve", new[] { typeof(Entity), typeof(Tile).MakeByRefType() }));
        void Create()
        {
            Assert.AreEqual(ResourcePath, AdapterType().GetField("ResourcePath").GetRawConstantValue());
            adapter = Construct(atlas); query = Resolver(adapter);
            world = new FlatPixelGreyboxCatalog((Texture2D)null);
            var field = typeof(FlatPixelGreyboxCatalog).GetField("_aftermathArt", I); Assert.NotNull(field);
            field.SetValue(world, adapter);
        }
        Tile Native(Entity owner, int frame)
        { Assert.True(query(owner, out var tile), owner.BlueprintName); Frame(tile, atlas, frame); return tile; }
        static void Frame(Tile tile, Texture2D texture, int frame)
        {
            Assert.NotNull(tile); Assert.NotNull(tile.sprite); Assert.AreSame(texture, tile.sprite.texture);
            Assert.AreEqual(new Rect(frame * 16, 0, 16, 24), tile.sprite.rect);
            Assert.AreEqual(new Vector2(8, 12), tile.sprite.pivot); Assert.AreEqual(16, tile.sprite.pixelsPerUnit);
            Assert.AreEqual(Tile.ColliderType.None, tile.colliderType); Assert.AreEqual(TileFlags.None, tile.flags);
        }

        [TestCase("Overworld.11.10.0")] [TestCase("Overworld.11.10.1")]
        [TestCase("Overworld.11.9.0")] [TestCase(Garden)]
        [TestCase("Overworld.12.10.0")] [TestCase("Overworld.12.11.0")]
        public void ThreeRealIdentitiesUseNativeWorldAndMatchingSubjectInEachAdmittedGraph(string id)
        {
            zone = Attach(id); world = new FlatPixelGreyboxCatalog();
            Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            for (int frame = 0; frame < 3; frame++)
            {
                var owner = Add(frame); var result = Resolve(); Assert.AreSame(owner, result.Owner);
                Assert.True(result.NativeForeground, "Actual aftermath still paints its canonical glyph: frame " + frame);
                var resource = Resources.Load<Texture2D>(ResourcePath); Assert.NotNull(resource);
                Assert.AreEqual(48, resource.width); Assert.AreEqual(24, resource.height);
                Assert.AreEqual(FilterMode.Point, resource.filterMode); Assert.AreEqual(TextureWrapMode.Clamp, resource.wrapMode);
                Assert.AreEqual(1, resource.mipmapCount); Frame(result.ForegroundTile, resource, frame);
                Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, 10, 10, out var sprite, out var tint));
                Assert.AreSame(result.ForegroundTile.sprite, sprite); Assert.AreEqual(Color.white, tint);
                Assert.AreEqual(Color.white, result.ForegroundColor); Assert.False(result.BlocksMovement);
                Assert.True(zone.RemoveEntity(owner));
            }
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void CachedNeutralIdentityDoesNotRequireADeadSourceOrMutateSimulation(int frame)
        {
            Create(); var owner = Add(frame); var tile = Native(owner, frame);
            var parts = owner.Parts.ToArray(); var props = owner.Properties.ToArray();
            var entities = zone.GetReadOnlyEntities().ToArray();
            for (int i = 0; i < 20; i++)
            {
                Assert.AreSame(tile, Native(owner, frame)); Assert.AreSame(tile, Resolve().ForegroundTile);
                Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, 10, 10, out var sprite, out _));
                Assert.AreSame(tile.sprite, sprite);
            }
            CollectionAssert.AreEqual(parts, owner.Parts); CollectionAssert.AreEqual(props, owner.Properties.ToArray());
            CollectionAssert.AreEqual(entities, zone.GetReadOnlyEntities().ToArray());
            owner.GetPart<RenderPart>().DisplayName = "changed name"; owner.GetPart<RenderPart>().ColorString = "&Y";
            Assert.AreSame(tile, Native(owner, frame));
            Assert.AreEqual(FilterMode.Bilinear, atlas.filterMode); Assert.AreEqual(TextureWrapMode.Repeat, atlas.wrapMode);
        }

        [TestCase(0)] [TestCase(1)]
        public void ExactCorpseProvenanceAndOptionalHarvestStateAreRequired(int frame)
        {
            Create(); var owner = Owner(frame); Native(owner, frame);
            owner.Tags.Remove("Corpse"); Assert.False(query(owner, out _)); owner.SetTag("Corpse");
            owner.Properties.Remove("SourceID"); Assert.False(query(owner, out _));
            owner.Properties["SourceID"] = "   "; Assert.False(query(owner, out _)); owner.Properties["SourceID"] = "dead-source";
            string species = owner.Properties["SourceBlueprint"];
            foreach (string wrong in new[] { "", species.ToLowerInvariant(), "Villager", "MarlbackScrabbler" })
            { owner.Properties["SourceBlueprint"] = wrong; Assert.False(query(owner, out _)); }
            owner.Properties["SourceBlueprint"] = species; Native(owner, frame);
            owner.BlueprintName = "MarlbackCorpse"; Assert.False(query(owner, out _)); owner.BlueprintName = "CreatureCorpse";
            var harvest = new HarvestablePart { YieldBlueprint = "VenomGland", YieldMin = 1, YieldMax = 2, YieldChance = 75 };
            owner.AddPart(harvest); var tile = Native(owner, frame);
            harvest.Harvested = true; Assert.False(query(owner, out _)); harvest.Harvested = false;
            harvest.ParentEntity = new Entity(); Assert.False(query(owner, out _)); harvest.ParentEntity = owner;
            // Neutral remains do not promise a particular roll, yield, or harvestability.
            harvest.YieldBlueprint = "RawMeat"; harvest.YieldChance = 0; Assert.AreSame(tile, Native(owner, frame));
            owner.RemovePart(harvest); Assert.AreSame(tile, Native(owner, frame));
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void ExplicitReskinsForeignPartsAndInvalidPortableShapesCannotBorrowNativeArt(int frame)
        {
            Create(); var owner = Owner(frame); Native(owner, frame);
            var render = owner.GetPart<RenderPart>(); var physics = owner.GetPart<PhysicsPart>();
            render.VisualID = "other"; Denied(owner); render.VisualID = "";
            render.VisualVariant = "other"; Denied(owner); render.VisualVariant = "";
            render.GlyphVariants = "%$"; Denied(owner); render.GlyphVariants = "";
            render.Visible = false; Denied(owner); render.Visible = true;
            render.ParentEntity = new Entity(); Denied(owner); render.ParentEntity = owner;
            owner.RemovePart(render); Denied(owner); owner.AddPart(render);
            physics.ParentEntity = new Entity(); Denied(owner); physics.ParentEntity = owner;
            physics.Takeable = false; Denied(owner); physics.Takeable = true;
            physics.Solid = true; Denied(owner); physics.Solid = false;
            owner.RemovePart(physics); Denied(owner); owner.AddPart(physics);
            foreach (string tag in new[] { "Creature", "Player", "Terrain", "Crop" })
            { owner.SetTag(tag); Denied(owner); owner.Tags.Remove(tag); }
            var crop = new CropPart(); owner.AddPart(crop); Denied(owner); owner.RemovePart(crop);
            var stack = owner.GetPart<StackerPart>(); bool temporary = stack == null;
            if (temporary) { stack = new StackerPart(); owner.AddPart(stack); }
            stack.StackCount = 0; Denied(owner); stack.StackCount = stack.MaxStack + 1; Denied(owner); stack.StackCount = 1;
            stack.ParentEntity = new Entity(); Denied(owner); stack.ParentEntity = owner;
            if (temporary) owner.RemovePart(stack);
            var footprint = new SpatialFootprintPart(); owner.AddPart(footprint); Native(owner, frame);
            footprint.ParentEntity = new Entity(); Denied(owner); footprint.ParentEntity = owner;
            footprint.CellsRaw = "0,0;1,0"; Denied(owner); footprint.CellsRaw = "1,0"; Denied(owner);
            footprint.CellsRaw = "0,0"; Native(owner, frame);
        }
        void Denied(Entity owner) => Assert.False(query(owner, out _));

        [Test] public void VenomGlandRequiresItsActualOwnedPositiveToxicReagent()
        {
            Create(); var owner = Owner(2); Native(owner, 2); var reagent = owner.GetPart<ReagentPart>();
            owner.Tags.Remove("Item"); Denied(owner); owner.SetTag("Item");
            reagent.ParentEntity = new Entity(); Denied(owner); reagent.ParentEntity = owner;
            owner.RemovePart(reagent); Denied(owner); owner.AddPart(reagent);
            foreach (string value in new[] { "", "heat:2", "toxic:0", "toxic:-1", "toxic:bad" })
            { reagent.PropertiesRaw = value; Denied(owner); }
            reagent.PropertiesRaw = "toxic:1"; Native(owner, 2);
            owner.BlueprintName = "BogSap"; Denied(owner);
        }

        [TestCase(0)] [TestCase(1)]
        public void RealCorpseSpawnerProducesPersistentIdentityWithoutKeepingTheLivingEntity(int frame)
        {
            Create(); string species = frame == 0 ? "Magpie" : "Viper";
            var living = content.Factory.CreateEntity(species); Assert.True(zone.AddEntity(living, 10, 10));
            Assert.False(query(living, out _), "The aftermath catalog cannot replace a living animal.");
            var spawner = living.GetPart<CorpsePart>(); Assert.NotNull(spawner);
            CorpsePart.Factory = content.Factory;
            var died = GameEvent.New("Died"); died.SetParameter("Zone", (object)zone);
            try { spawner.HandleEvent(died); } finally { died.Release(); }
            var corpse = spawner.CreatedCorpse; Assert.NotNull(corpse);
            Assert.AreEqual(living.ID, corpse.GetProperty("SourceID")); Assert.AreEqual(species, corpse.GetProperty("SourceBlueprint"));
            Assert.True(zone.RemoveEntity(living)); Native(corpse, frame); Assert.AreSame(corpse, Resolve().Owner);
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(corpse);
            Assert.AreNotSame(corpse, loaded); Assert.AreEqual(corpse.ID, loaded.ID);
            Assert.AreEqual(living.ID, loaded.GetProperty("SourceID")); Assert.AreEqual(species, loaded.GetProperty("SourceBlueprint"));
            Assert.True(zone.RemoveEntity(corpse)); Assert.True(zone.AddEntity(loaded, 10, 10));
            Native(loaded, frame); Assert.AreSame(loaded, Resolve().Owner);
            Assert.AreEqual(frame == 1, loaded.HasPart<HarvestablePart>());
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void PickupDropAndExactCarriedReaderRevokeStaleWorldDocuments(int frame)
        {
            Create(); var owner = Add(frame);
            using (var reader = new Reader(world, zone, content.Factory.CreateEntity("Player")))
            {
                var document = ExamineDocumentBuilder.BuildWorld(owner, zone, zone.GetCell(10, 10), reader.Viewer);
                Portrait(reader, document, frame);
                Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(owner, false), reader.Viewer, zone).Success);
                Assert.IsNull(zone.GetEntityCell(owner)); Assert.IsNull(Resolve().Owner); Rejected(reader, document);
                var carried = ExamineDocumentBuilder.BuildInventory(owner, reader.Viewer); Portrait(reader, carried, frame);
                Assert.True(InventorySystem.Drop(reader.Viewer, owner, zone)); Rejected(reader, carried);
                var cell = zone.GetEntityCell(owner); Assert.NotNull(cell);
                Portrait(reader, ExamineDocumentBuilder.BuildWorld(owner, zone, cell, reader.Viewer), frame);
                Assert.True(zone.MoveEntity(reader.Viewer, 8, 10));
                Assert.True(world.TryResolvePresentation(zone, cell.X, cell.Y, out var result));
                Assert.AreSame(owner, result.Owner); Frame(result.ForegroundTile, atlas, frame);
            }
        }

        [Test] public void PileTopAndCustodyRemainAuthoritativeForCorpseAndProduct()
        {
            Create(); var corpse = Add(0); var gland = Add(2);
            Assert.AreSame(gland, Resolve().Owner); Frame(Resolve().ForegroundTile, atlas, 2);
            using (var reader = new Reader(world, zone, content.Factory.CreateEntity("Player")))
            {
                Portrait(reader, ExamineDocumentBuilder.BuildWorld(corpse, zone, zone.GetCell(10, 10), reader.Viewer), 0);
                Assert.AreSame(gland, Resolve().Owner);
                gland.GetPart<PhysicsPart>().InInventory = new Entity(); Assert.AreSame(corpse, Resolve().Owner);
                gland.GetPart<PhysicsPart>().InInventory = null; Assert.AreSame(gland, Resolve().Owner);
                gland.GetPart<PhysicsPart>().Equipped = new Entity(); Assert.AreSame(corpse, Resolve().Owner);
                gland.GetPart<PhysicsPart>().Equipped = null;
                Assert.True(zone.RemoveEntity(gland)); Assert.AreSame(corpse, Resolve().Owner);
            }
        }

        [Test] public void RealCudgelPileKeepsItsTopOwnerWhileTheExactCorpseReaderShowsTheBody()
        {
            Create(); var corpse = Add(0); var cudgel = content.Factory.CreateEntity("Cudgel");
            Assert.True(zone.AddEntity(cudgel, 10, 10));
            Assert.Greater(cudgel.GetPart<RenderPart>().RenderLayer, corpse.GetPart<RenderPart>().RenderLayer);
            using (var reader = new Reader(world, zone, content.Factory.CreateEntity("Player")))
            {
                Assert.AreSame(cudgel, Resolve().Owner); Assert.False(Resolve().NativeForeground);
                Portrait(reader, ExamineDocumentBuilder.BuildWorld(corpse, zone, zone.GetCell(10, 10), reader.Viewer), 0);
                Assert.AreSame(cudgel, Resolve().Owner);
                Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(cudgel, false), reader.Viewer, zone).Success);
                Assert.AreSame(corpse, Resolve().Owner); Frame(Resolve().ForegroundTile, atlas, 0);
            }
        }

        [TestCase(0)] [TestCase(100)]
        public void ActualViperHarvestRemovesTheBodyEvenWhenTheFiniteYieldRollFails(int chance)
        {
            Create(); var corpse = Add(1); var harvest = new HarvestablePart
            { YieldBlueprint = "VenomGland", YieldMin = 1, YieldMax = 1, YieldChance = chance };
            corpse.AddPart(harvest); Native(corpse, 1);
            var actor = content.Factory.CreateEntity("Player"); Assert.True(zone.AddEntity(actor, 9, 10));
            var previous = HarvestablePart.Factory;
            try
            {
                HarvestablePart.Factory = content.Factory;
                Assert.True(DensityHarvestSecurityTests.Act(corpse, actor, zone));
                Assert.True(harvest.Harvested); Assert.IsNull(zone.GetEntityCell(corpse)); Denied(corpse);
                Assert.IsNull(Resolve().Owner); Assert.False(DensityHarvestSecurityTests.Act(corpse, actor, zone));
                var glands = actor.GetPart<InventoryPart>().Objects.Where(e => e.BlueprintName == "VenomGland").ToArray();
                Assert.AreEqual(chance == 100 ? 1 : 0, glands.Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1));
                if (glands.Length != 0) Native(glands[0], 2);
            }
            finally { HarvestablePart.Factory = previous; }
        }

        [Test] public void ExistingLivingAnimalsAndApprovedMarlbackCorpseKeepTheirCurrentArtwork()
        {
            world = new FlatPixelGreyboxCatalog();
            foreach (string name in new[] { "Magpie", "Viper", "MarlbackCorpse" })
            {
                var owner = content.Factory.CreateEntity(name);
                if (name == "MarlbackCorpse")
                { owner.Properties["SourceBlueprint"] = "MarlbackScrabbler"; owner.Properties["SourceID"] = "earlier-death"; }
                Assert.True(zone.AddEntity(owner, 10, 10)); var result = Resolve();
                Assert.True(result.NativeForeground); Assert.AreSame(owner, result.Owner);
                string path = name == "MarlbackCorpse" ? FlatPixelOpeningArtCatalog.ItemsResourcePath : FlatPixelOpeningArtCatalog.WorksResourcePath;
                int frame = name == "Magpie" ? 5 : name == "Viper" ? 6 : 11;
                Frame(result.ForegroundTile, Resources.Load<Texture2D>(path), frame);
                Assert.True(zone.RemoveEntity(owner));
            }
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void FogMovedCellReplacedGraphAndOutsideOpeningCannotDiscloseNativeArt(int frame)
        {
            Create(); var owner = Add(frame); Frame(Resolve().ForegroundTile, atlas, frame);
            var cell = zone.GetCell(10, 10); cell.IsVisible = false; Assert.IsNull(Resolve().Owner);
            cell.Explored = false; Assert.False(world.TryResolvePresentation(zone, 10, 10, out _));
            cell.Explored = cell.IsVisible = true; Assert.True(zone.MoveEntity(owner, 11, 10)); cell.Objects.Add(owner);
            Assert.IsNull(Resolve().Owner); Assert.True(world.TryResolvePresentation(zone, 11, 10, out var moved)); Assert.AreSame(owner, moved.Owner);
            manager.CachedZones[Garden] = new Zone(Garden); Assert.False(world.TryResolvePresentation(zone, 11, 10, out _));
            Assert.True(zone.RemoveEntity(owner)); zone = Attach("Overworld.12.12.0"); Assert.True(zone.AddEntity(owner, 10, 10));
            Assert.AreSame(owner, Resolve().Owner); Assert.False(Resolve().NativeForeground);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, 10, 10, out var sprite, out _)); Assert.AreNotSame(atlas, sprite.texture);
        }

        [TestCase(false)] [TestCase(true)]
        public void MissingOrMalformedResourceKeepsGlyphFallback(bool malformed)
        {
            var bad = malformed ? new Texture2D(16, 24) : null;
            try
            {
                using (var art = Construct(bad))
                    for (int frame = 0; frame < 3; frame++) Assert.False(Resolver(art)(Owner(frame), out _));
                using (var old = new FlatPixelGreyboxCatalog((Texture2D)null))
                {
                    var owner = Add(0); Assert.True(old.TryResolvePresentation(zone, 10, 10, out var result));
                    Assert.AreSame(owner, result.Owner); Assert.False(result.NativeForeground);
                    var field = typeof(FlatPixelGreyboxCatalog).GetField("_aftermathArt", I); Assert.NotNull(field); Assert.IsNull(field.GetValue(old));
                }
            }
            finally { if (bad != null) Object.DestroyImmediate(bad); }
        }

        [Test] public void DisposalReleasesOnlyCachedFramesAndLeavesBorrowedTextureAndPeerAlive()
        {
            Create(); var first = Native(Owner(0), 0); var sprite = first.sprite;
            using (var peer = Construct(atlas))
            {
                var otherQuery = Resolver(peer); Assert.True(otherQuery(Owner(0), out var other)); Assert.AreNotSame(first, other);
                world.Dispose(); world.Dispose(); Assert.True(first == null); Assert.True(sprite == null);
                Assert.False(query(Owner(0), out _)); Assert.True(atlas != null);
                Assert.True(otherQuery(Owner(0), out var stillAlive)); Frame(stillAlive, atlas, 0);
            }
        }

        void Portrait(Reader reader, AnnouncementDocument document, int frame)
        {
            Assert.NotNull(document); Assert.True(ExamineDocumentBuilder.IsCurrentSubject(document));
            Assert.True(reader.Presentation.TryGetFlatExaminePortrait(document, out var result));
            Assert.AreSame(atlas, result.Texture); Assert.AreEqual(Color.white, result.Tint); Assert.False(result.IsModel);
            Assert.AreEqual(new Rect(frame / 3f, 0, 1f / 3f, 1), result.Uv);
        }
        static void Rejected(Reader reader, AnnouncementDocument document)
        { Assert.True(reader.Presentation.TryGetFlatExaminePortrait(document, out var result)); Assert.IsNull(result.Texture); }
        sealed class Reader : IDisposable
        {
            readonly GameObject root;
            public readonly Entity Viewer;
            public readonly ZoneRenderer Presentation;
            public Reader(FlatPixelGreyboxCatalog catalog, Zone zone, Entity viewer)
            {
                Viewer = viewer; Assert.True(zone.AddEntity(viewer, 9, 10));
                root = new GameObject("Aftermath exact owner reader"); root.SetActive(false);
                Presentation = root.AddComponent<ZoneRenderer>(); var environment = root.AddComponent<EnvironmentSpriteRenderer>();
                typeof(EnvironmentSpriteRenderer).GetField("_flatPixelGreybox", I).SetValue(environment, catalog); environment.FlatPixelGreyboxEnabled = true;
                typeof(ZoneRenderer).GetField("_envSpriteRenderer", I).SetValue(Presentation, environment);
                typeof(ZoneRenderer).GetField("_flatPixelModeEnabled", I).SetValue(Presentation, true);
                typeof(ZoneRenderer).GetProperty("CurrentZone", I).SetValue(Presentation, zone);
                Assert.True(Presentation.FlatPixelModeActive);
            }
            public void Dispose() { if (root != null) Object.DestroyImmediate(root); }
        }
    }
}
