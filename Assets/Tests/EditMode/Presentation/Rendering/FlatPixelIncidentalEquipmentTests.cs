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
    // Tests first. Initial world/reader assertions use only shipped APIs and
    // fail on glyph fallback before resource assertions. Companion APIs are reflected.
    public sealed class FlatPixelIncidentalEquipmentTests
    {
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags S = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        const string North = "Overworld.11.9.0", Kitchen = "Overworld.12.11.0";
        static readonly string[] Identities = { "LeatherArmor", "Hatchet" };
        readonly List<Action> restore = new List<Action>();
        HotbarSaveFixture globals;
        EntityFactory factory;
        OverworldZoneManager manager;
        Zone zone;
        Texture2D leather, hatchet;
        IDisposable adapter;
        delegate bool Query(Entity owner, out Tile tile);
        Query query;
        FlatPixelGreyboxCatalog world;

        [SetUp] public void Setup()
        {
            SnapshotStatics(typeof(MessageLog)); SnapshotStatics(typeof(EquipmentChangeBus));
            SnapshotStatics(typeof(WorldAudioHooks)); SnapshotStatics(typeof(WorldAudioUtility)); SnapshotStatics(typeof(EntityVisualHooks));
            Snapshot(typeof(AnatomyFactory).GetField("_types", S)); Snapshot(typeof(BodyPart).GetField("_nextID", S));
            foreach (string name in new[] { "_buffer", "_writeIndex", "_filledCount", "_droppedCount" }) Snapshot(typeof(Diag).GetField(name, S));
            globals = new HotbarSaveFixture(false, false);
            MessageLog.OnMessage = null; MessageLog.Clear();
            WorldAudioHooks.Callback = null; WorldAudioUtility.Callback = null; EntityVisualHooks.Reset();
            ZoneRenderHooks.CellDirtyCallback = null; ZoneRenderHooks.FullDirtyCallback = null;
            factory = DensityReferenceGladeTests.Factory();
            manager = OverworldZoneManager.CreateDetached(factory, 64, true);
            zone = new Zone(North); manager.SetActiveZone(zone); Reveal(zone);
        }
        [TearDown] public void Cleanup()
        {
            try
            {
                world?.Dispose(); adapter?.Dispose(); world = null; adapter = null; query = null;
                if (leather != null) Object.DestroyImmediate(leather);
                if (hatchet != null) Object.DestroyImmediate(hatchet);
                leather = hatchet = null;
                globals?.Dispose(); globals = null;
            }
            finally { for (int i = restore.Count - 1; i >= 0; i--) restore[i](); restore.Clear(); }
        }
        static string Resource(string blueprint) => "Sprites/Live16x24/" + (blueprint == "LeatherArmor" ? "leather-armor" : "hatchet");
        static string Slot(string blueprint) => blueprint == "LeatherArmor" ? "Body" : "Hand";
        Entity Owner(string blueprint) { var owner = factory.CreateEntity(blueprint); Assert.NotNull(owner); return owner; }
        Entity Add(string blueprint, int x = 10, int y = 10)
        { var owner = Owner(blueprint); Assert.True(zone.AddEntity(owner, x, y)); return owner; }
        static void Reveal(Zone target)
        { for (int y = 8; y <= 12; y++) for (int x = 8; x <= 13; x++) target.GetCell(x, y).Explored = target.GetCell(x, y).IsVisible = true; }

        [TestCase("LeatherArmor")] [TestCase("Hatchet")]
        public void RealCurrentEquipmentUsesNativeWorldAndMatchingSubjectBeforeResourceAssertion(string blueprint)
        {
            if (blueprint == "Hatchet") { zone = new Zone(Kitchen); manager.SetActiveZone(zone); Reveal(zone); }
            var owner = Add(blueprint); world = new FlatPixelGreyboxCatalog();
            Assert.True(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            var value = Resolve(); Assert.AreSame(owner, value.Owner); Assert.NotNull(value.ForegroundTile);
            Assert.True(value.NativeForeground, "The actual equipment still paints its canonical glyph.");
            var resource = Imported(blueprint); Frame(value.ForegroundTile, resource);
            Assert.AreEqual(Color.white, value.ForegroundColor);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, 10, 10, out var subject, out var tint));
            Assert.AreSame(value.ForegroundTile.sprite, subject); Assert.AreEqual(Color.white, tint);
        }

        [TestCase("LeatherArmor")] [TestCase("Hatchet")]
        public void ActualEquippedReaderUsesExactNativeItemBeforeResourceAssertion(string blueprint)
        {
            world = new FlatPixelGreyboxCatalog();
            using (var reader = new Reader(world, zone, Owner("Player")))
            {
                var owner = Owner(blueprint); Equip(reader.Viewer, owner);
                reader.Presentation.Paused = true;
                var document = ExamineDocumentBuilder.BuildInventory(owner, reader.Viewer);
                Assert.NotNull(document); Assert.True(ExamineDocumentBuilder.IsCurrentSubject(document));
                Assert.True(world.TryGetFallbackSubjectSprite(owner, 9, 10, out var glyph, out _));
                Assert.True(reader.Presentation.TryGetFlatExaminePortrait(document, out var portrait));
                Assert.NotNull(portrait.Texture);
                Assert.AreNotSame(glyph.texture, portrait.Texture, "An authorized equipped item still shows glyph art.");
                Assert.AreSame(Imported(blueprint), portrait.Texture); Assert.AreEqual(new Rect(0, 0, 1, 1), portrait.Uv);
                Assert.False(portrait.IsModel); Assert.AreEqual(Color.white, portrait.Tint);
                Assert.AreSame(reader.Viewer, owner.GetPart<PhysicsPart>().Equipped); Assert.IsNull(zone.GetEntityCell(owner));
            }
        }

        [TestCase("LeatherArmor")] [TestCase("Hatchet")]
        public void InjectedSemanticFrameIsCachedAndSharesExactWorldAndReaderIdentity(string blueprint)
        {
            Create(); var owner = Add(blueprint); int parts = owner.Parts.Count;
            var tile = Native(owner); var value = Resolve();
            Assert.True(value.NativeForeground); Assert.AreSame(owner, value.Owner); Assert.AreSame(tile, value.ForegroundTile);
            for (int i = 0; i < 20; i++) Assert.AreSame(tile, Native(owner));
            Assert.AreEqual(parts, owner.Parts.Count); Assert.False(owner.GetPart<PhysicsPart>().Solid);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, 10, 10, out var subject, out var tint));
            Assert.AreSame(tile.sprite, subject); Assert.AreEqual(Color.white, tint);
            Assert.AreEqual(FilterMode.Bilinear, TextureFor(owner).filterMode, "Borrowed inputs are not reconfigured.");
            Assert.AreEqual(TextureWrapMode.Repeat, TextureFor(owner).wrapMode);
        }

        [TestCase("LeatherArmor", "Armor")] [TestCase("LeatherArmor", "Equippable")]
        [TestCase("Hatchet", "MeleeWeapon")] [TestCase("Hatchet", "Equippable")]
        public void ExactIdentityRequiresItsOwnFunctionalPartAndRepairsAfterValidRestoration(string blueprint, string name)
        {
            Create(); var owner = Add(blueprint); Native(owner);
            var part = owner.GetPart(name); Assert.NotNull(part);
            part.ParentEntity = new Entity(); Assert.False(query(owner, out _)); Assert.False(Resolve().NativeForeground);
            part.ParentEntity = owner; Native(owner);
            Assert.True(owner.RemovePart(part)); Assert.False(query(owner, out _)); Assert.False(Resolve().NativeForeground);
        }

        [Test]
        public void ExplicitOverridesInvalidPortableStateAndNonSingleCellGeometryRetainFallback()
        {
            Create();
            foreach (string blueprint in Identities)
            {
                var owner = Owner(blueprint); Native(owner);
                var render = owner.GetPart<RenderPart>(); var physics = owner.GetPart<PhysicsPart>();
                var stack = owner.GetPart<StackerPart>(); Assert.NotNull(stack);
                render.VisualID = "foreign"; Denied(owner); render.VisualID = "";
                render.VisualVariant = "costume"; Denied(owner); render.VisualVariant = "";
                render.GlyphVariants = "P["; Denied(owner); render.GlyphVariants = "";
                render.Visible = false; Denied(owner); render.Visible = true;
                render.ParentEntity = new Entity(); Denied(owner); render.ParentEntity = owner;
                Assert.True(owner.RemovePart(render)); Denied(owner); owner.AddPart(render);
                physics.ParentEntity = new Entity(); Denied(owner); physics.ParentEntity = owner;
                physics.Takeable = false; Denied(owner); physics.Takeable = true;
                physics.Solid = true; Denied(owner); physics.Solid = false;
                Assert.True(owner.RemovePart(physics)); Denied(owner); owner.AddPart(physics);
                owner.Tags.Remove("Item"); Denied(owner); owner.SetTag("Item");
                foreach (var tag in new[] { "Player", "Creature", "Terrain", "Crop" })
                { owner.SetTag(tag); Denied(owner); owner.Tags.Remove(tag); }
                var crop = new CropPart(); owner.AddPart(crop); Denied(owner); owner.RemovePart(crop);
                stack.StackCount = 0; Denied(owner); stack.StackCount = stack.MaxStack + 1; Denied(owner); stack.StackCount = 1;
                stack.ParentEntity = new Entity(); Denied(owner); stack.ParentEntity = owner;
                var equipment = owner.GetPart<EquippablePart>(); equipment.Slot = "Feet"; Denied(owner); equipment.Slot = Slot(blueprint);
                var footprint = new SpatialFootprintPart(); owner.AddPart(footprint); Native(owner);
                footprint.ParentEntity = new Entity(); Denied(owner); footprint.ParentEntity = owner;
                footprint.CellsRaw = "0,0;1,0"; Denied(owner); footprint.CellsRaw = "1,0"; Denied(owner);
                footprint.CellsRaw = "0,0"; Native(owner);
            }
        }

        [Test]
        public void SimilarNamesAndSharedGlyphsCannotAliasDifferentEquipment()
        {
            Create(); Native(Owner("LeatherArmor")); Native(Owner("Hatchet"));
            foreach (string blueprint in new[] { "ChainMail", "LeatherBoots", "Battleaxe", "Cudgel" })
            {
                var other = Owner(blueprint); other.GetPart<RenderPart>().DisplayName = "leather armor hatchet";
                other.GetPart<RenderPart>().RenderString = "P"; Denied(other);
            }
            foreach (string blueprint in Identities)
            { var owner = Owner(blueprint); Native(owner); owner.BlueprintName = blueprint.ToLowerInvariant(); Denied(owner); }
        }

        [TestCase("LeatherArmor")] [TestCase("Hatchet")]
        public void MutableConditionCombatValuesAndNamesDoNotInventAlternateEquipmentStates(string blueprint)
        {
            Create(); var owner = Owner(blueprint); var original = Native(owner);
            owner.GetPart<RenderPart>().DisplayName = "weathered named equipment";
            owner.GetPart<RenderPart>().ColorString = "&R"; owner.GetStat("Hitpoints").BaseValue = 1;
            if (blueprint == "LeatherArmor")
            {
                owner.GetPart<ArmorPart>().AV += 2; owner.GetPart<ArmorPart>().DV -= 1;
                var repair = owner.GetPart<RepairablePart>(); Assert.NotNull(repair); repair.Repaired = true;
                repair.RepairedBy = new Entity(); repair.RepairCauseID = "historical-repair";
            }
            else { owner.GetPart<MeleeWeaponPart>().BaseDamage = "1d8+1"; owner.GetPart<MeleeWeaponPart>().PenBonus += 1; }
            Assert.AreSame(original, Native(owner)); Assert.AreEqual(1, owner.GetStatValue("Hitpoints"));
        }

        [TestCase("LeatherArmor")] [TestCase("Hatchet")]
        public void CoveredEquipmentKeepsItsExactReaderWithoutReplacingTopCoin(string blueprint)
        {
            Create(); var owner = Add(blueprint); var tile = Native(owner);
            var coin = Add("GoldCoin"); coin.GetPart<RenderPart>().RenderLayer = owner.GetPart<RenderPart>().RenderLayer + 1;
            using (var reader = new Reader(world, zone, Owner("Player")))
            {
                Assert.AreSame(coin, Resolve().Owner); Assert.AreNotSame(tile, Resolve().ForegroundTile);
                Portrait(reader, ExamineDocumentBuilder.BuildWorld(owner, zone, zone.GetCell(10, 10), reader.Viewer), TextureFor(owner));
                Assert.AreSame(coin, Resolve().Owner);
                Assert.True(zone.RemoveEntity(coin)); Assert.AreSame(owner, Resolve().Owner); Assert.AreSame(tile, Resolve().ForegroundTile);
            }
        }

        [TestCase("LeatherArmor")] [TestCase("Hatchet")]
        public void RealPickupBodyEquipAndDropRevokeOldReadersWithoutAWorldPhantom(string blueprint)
        {
            Create(); var owner = Add(blueprint);
            using (var reader = new Reader(world, zone, Owner("Player")))
            {
                var ground = ExamineDocumentBuilder.BuildWorld(owner, zone, zone.GetCell(10, 10), reader.Viewer);
                Portrait(reader, ground, TextureFor(owner));
                Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(owner, false), reader.Viewer, zone).Success);
                Assert.IsNull(zone.GetEntityCell(owner)); Assert.IsNull(Resolve().Owner); Rejected(reader, ground);
                var carried = ExamineDocumentBuilder.BuildInventory(owner, reader.Viewer); Portrait(reader, carried, TextureFor(owner));
                Equip(reader.Viewer, owner, alreadyCarried: true);
                Portrait(reader, ExamineDocumentBuilder.BuildInventory(owner, reader.Viewer), TextureFor(owner));
                Assert.True(InventorySystem.Drop(reader.Viewer, owner, zone));
                Rejected(reader, carried); Assert.IsNull(owner.GetPart<PhysicsPart>().Equipped);
                var dropped = zone.GetEntityCell(owner); Assert.NotNull(dropped);
                Portrait(reader, ExamineDocumentBuilder.BuildWorld(owner, zone, dropped, reader.Viewer), TextureFor(owner));
                int x = dropped.X, y = dropped.Y; Assert.True(zone.MoveEntity(reader.Viewer, 8, 10));
                Assert.True(world.TryResolvePresentation(zone, x, y, out var visible)); Assert.AreSame(owner, visible.Owner);
                Frame(visible.ForegroundTile, TextureFor(owner));
            }
        }

        [TestCase("carried")] [TestCase("equipped")] [TestCase("foreign-render")]
        [TestCase("foreign-physics")] [TestCase("empty-stack")]
        public void LostWorldAuthorityExposesTheLowerOwnerInsteadOfAStaleNativeItem(string fault)
        {
            Create();
            foreach (string blueprint in Identities)
            {
                var lower = Add("Dagger"); lower.GetPart<RenderPart>().RenderLayer = 1;
                var owner = Add(blueprint); Assert.AreSame(owner, Resolve().Owner); Assert.True(Resolve().NativeForeground);
                if (fault == "carried") owner.GetPart<PhysicsPart>().InInventory = new Entity();
                if (fault == "equipped") owner.GetPart<PhysicsPart>().Equipped = new Entity();
                if (fault == "foreign-render") owner.GetPart<RenderPart>().ParentEntity = new Entity();
                if (fault == "foreign-physics") owner.GetPart<PhysicsPart>().ParentEntity = new Entity();
                if (fault == "empty-stack") owner.GetPart<StackerPart>().StackCount = 0;
                Assert.AreSame(lower, Resolve().Owner); Assert.False(Resolve().NativeForeground);
                Assert.True(zone.RemoveEntity(owner)); Assert.True(zone.RemoveEntity(lower));
            }
        }

        [TestCase("LeatherArmor")] [TestCase("Hatchet")]
        public void FogMovedEntriesReplacedGraphsAndOtherRegionsCannotGainNativeDisclosure(string blueprint)
        {
            Create(); var owner = Add(blueprint); Assert.True(Resolve().NativeForeground);
            var old = zone.GetCell(10, 10); old.IsVisible = false;
            Assert.IsNull(Resolve().Owner); old.Explored = false; Assert.False(world.TryResolvePresentation(zone, 10, 10, out _));
            old.Explored = old.IsVisible = true; Assert.True(Resolve().NativeForeground);
            Assert.True(zone.MoveEntity(owner, 11, 10)); old.Objects.Add(owner);
            Assert.IsNull(Resolve().Owner); Assert.True(world.TryResolvePresentation(zone, 11, 10, out var moved)); Assert.AreSame(owner, moved.Owner);
            using (var reader = new Reader(world, zone, Owner("Player")))
            {
                var document = ExamineDocumentBuilder.BuildWorld(owner, zone, zone.GetCell(11, 10), reader.Viewer);
                Portrait(reader, document, TextureFor(owner));
                manager.SetActiveZone(new Zone(zone.ZoneID));
                Assert.False(world.TryResolvePresentation(zone, 11, 10, out _)); Rejected(reader, document);
                Assert.True(zone.RemoveEntity(owner));
                var outside = new Zone("Overworld.20.20.0"); Reveal(outside); manager.SetActiveZone(outside);
                Assert.True(outside.AddEntity(owner, 10, 10));
                Assert.True(world.TryResolvePresentation(outside, 10, 10, out var fallback));
                Assert.AreSame(owner, fallback.Owner); Assert.False(fallback.NativeForeground);
                Assert.True(world.TryGetPresentationSubjectSprite(outside, owner, 10, 10, out var subject, out _));
                Assert.AreSame(fallback.ForegroundTile.sprite, subject); Assert.AreNotSame(TextureFor(owner), subject.texture);
            }
        }

        [TestCase("LeatherArmor")] [TestCase("Hatchet")]
        public void RawFootprintMutationCannotRestoreNativePortraitBeforeSpatialRegistrationIsRepaired(string blueprint)
        {
            Create(); var owner = Add(blueprint); var tile = Native(owner);
            using (var reader = new Reader(world, zone, Owner("Player")))
            {
                var document = ExamineDocumentBuilder.BuildWorld(owner, zone, zone.GetCell(10, 10), reader.Viewer);
                Portrait(reader, document, TextureFor(owner));

                // The original review premise was incorrect: the real AddPart
                // path refreshes spatial registration even after placement.
                var footprint = new SpatialFootprintPart { CellsRaw = "0,0" };
                owner.AddPart(footprint);
                Assert.True(FootprintRegistrationIsCurrent(zone, owner));
                Assert.AreSame(tile, Resolve().ForegroundTile);
                Portrait(reader, document, TextureFor(owner));

                // Establish a real registered non-single body, then bypass the
                // required mutation API through the public serialized field.
                Assert.True(zone.TryChangeFootprint(owner, "0,0;1,0"));
                Assert.True(FootprintRegistrationIsCurrent(zone, owner));
                Denied(owner); Assert.False(Resolve().NativeForeground);
                footprint.CellsRaw = "0,0";
                Assert.False(FootprintRegistrationIsCurrent(zone, owner),
                    "This must be an actual stale registration, not merely a late valid AddPart.");
                Assert.AreSame(owner, footprint.ParentEntity);
                var staleWorld = Resolve();
                Assert.IsNull(staleWorld.Owner); Assert.False(staleWorld.NativeForeground);
                Assert.True(reader.Presentation.TryGetFlatExaminePortrait(document, out var stalePortrait));
                Assert.AreNotSame(TextureFor(owner), stalePortrait.Texture,
                    "A stale body's exact reader must not restore native art that current world ownership refuses.");

                // Canonical repair restores both routes without replacing owner,
                // catalog, document or texture. Reading never repairs the index.
                Assert.False(FootprintRegistrationIsCurrent(zone, owner));
                Assert.True(zone.TryChangeFootprint(owner, "0,0"));
                Assert.True(FootprintRegistrationIsCurrent(zone, owner));
                Assert.AreSame(owner, Resolve().Owner); Assert.AreSame(tile, Resolve().ForegroundTile);
                Portrait(reader, document, TextureFor(owner));
            }
        }

        [TestCase("LeatherArmor")] [TestCase("Hatchet")]
        public void ValidSingleCellFootprintRemainsNativeWhenUnplacedRegisteredCarriedAndEquipped(string blueprint)
        {
            Create(); var owner = Owner(blueprint);
            owner.AddPart(new SpatialFootprintPart { CellsRaw = "0,0" });
            Assert.IsNull(zone.GetEntityCell(owner)); var tile = Native(owner);
            Assert.True(zone.AddEntity(owner, 10, 10));
            Assert.True(FootprintRegistrationIsCurrent(zone, owner));
            Assert.AreSame(owner, Resolve().Owner); Assert.AreSame(tile, Resolve().ForegroundTile);
            using (var reader = new Reader(world, zone, Owner("Player")))
            {
                var ground = ExamineDocumentBuilder.BuildWorld(owner, zone, zone.GetCell(10, 10), reader.Viewer);
                Portrait(reader, ground, TextureFor(owner));
                Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(owner, false), reader.Viewer, zone).Success);
                Assert.IsNull(zone.GetEntityCell(owner));
                Assert.AreSame(reader.Viewer, owner.GetPart<PhysicsPart>().InInventory);
                Assert.AreSame(tile, Native(owner)); Rejected(reader, ground);
                Portrait(reader, ExamineDocumentBuilder.BuildInventory(owner, reader.Viewer), TextureFor(owner));
                Equip(reader.Viewer, owner, alreadyCarried: true);
                Assert.AreSame(tile, Native(owner)); reader.Presentation.Paused = true;
                Portrait(reader, ExamineDocumentBuilder.BuildInventory(owner, reader.Viewer), TextureFor(owner));
            }
        }

        static bool FootprintRegistrationIsCurrent(Zone target, Entity owner)
        {
            var method = typeof(Zone).GetMethod("IsFootprintCurrent", I);
            Assert.NotNull(method); return (bool)method.Invoke(target, new object[] { owner });
        }

        [TestCase(false)] [TestCase(true)]
        public void MissingOrMalformedResourceIsIndependentAndNeverLoadsAnInjectedReplacement(bool malformed)
        {
            Create(); Texture2D bad = malformed ? new Texture2D(16, 16) : null;
            try
            {
                using (var a = Construct(bad, hatchet))
                { var q = Resolver(a); Assert.False(q(Owner("LeatherArmor"), out _)); Assert.True(q(Owner("Hatchet"), out var tile)); Frame(tile, hatchet); }
                using (var b = Construct(leather, bad))
                { var q = Resolver(b); Assert.False(q(Owner("Hatchet"), out _)); Assert.True(q(Owner("LeatherArmor"), out var tile)); Frame(tile, leather); }
                using (var empty = Construct(null, null))
                    foreach (string blueprint in Identities) Assert.False(Resolver(empty)(Owner(blueprint), out _));
            }
            finally { if (bad != null) Object.DestroyImmediate(bad); }
        }

        [Test]
        public void OldInjectedCatalogDoesNotOptIntoResourcesAndUnknownOwnersDoNotCreateTheCompanion()
        {
            var field = typeof(FlatPixelGreyboxCatalog).GetField("_incidentalEquipmentArt", I); Assert.NotNull(field);
            using (var old = new FlatPixelGreyboxCatalog((Texture2D)null))
            {
                foreach (string blueprint in Identities)
                { var owner = Add(blueprint); Assert.True(old.TryResolvePresentation(zone, 10, 10, out var value)); Assert.False(value.NativeForeground); zone.RemoveEntity(owner); }
                Assert.IsNull(field.GetValue(old));
            }
            using (var normal = new FlatPixelGreyboxCatalog())
            { Add("Dagger"); Assert.True(normal.TryResolvePresentation(zone, 10, 10, out _)); Assert.IsNull(field.GetValue(normal)); }
        }

        [Test]
        public void WorldDisposesOnlyItsCachedObjectsAndAnIndependentPeerKeepsBorrowedArt()
        {
            Create(); var a = Native(Owner("LeatherArmor")); var b = Native(Owner("Hatchet")); var sa = a.sprite; var sb = b.sprite;
            using (var peer = Construct(leather, hatchet))
            {
                var q = Resolver(peer); Assert.True(q(Owner("LeatherArmor"), out var other)); Assert.AreNotSame(a, other);
                world.Dispose(); world.Dispose(); Assert.True(a == null); Assert.True(b == null); Assert.True(sa == null); Assert.True(sb == null);
                Assert.False(query(Owner("LeatherArmor"), out _)); Assert.True(q(Owner("LeatherArmor"), out var alive)); Frame(alive, leather);
                Assert.True(leather != null); Assert.True(hatchet != null);
            }
        }

        static Type AdapterType()
        { var type = typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelIncidentalEquipmentCatalog"); Assert.NotNull(type, "The optional exact-equipment companion is absent."); return type; }
        static IDisposable Construct(Texture2D armor, Texture2D axe) => (IDisposable)Activator.CreateInstance(AdapterType(), new object[] { armor, axe });
        static Query Resolver(IDisposable art) => (Query)Delegate.CreateDelegate(typeof(Query), art,
            AdapterType().GetMethod("TryResolve", new[] { typeof(Entity), typeof(Tile).MakeByRefType() }));
        void Create()
        {
            leather = new Texture2D(16, 24); hatchet = new Texture2D(16, 24);
            Assert.AreEqual(Resource("LeatherArmor"), AdapterType().GetField("LeatherArmorResourcePath").GetRawConstantValue());
            Assert.AreEqual(Resource("Hatchet"), AdapterType().GetField("HatchetResourcePath").GetRawConstantValue());
            adapter = Construct(leather, hatchet); query = Resolver(adapter);
            world = new FlatPixelGreyboxCatalog((Texture2D)null);
            var field = typeof(FlatPixelGreyboxCatalog).GetField("_incidentalEquipmentArt", I); Assert.NotNull(field); field.SetValue(world, adapter);
        }
        Texture2D TextureFor(Entity owner) => owner.BlueprintName == "LeatherArmor" ? leather : hatchet;
        Tile Native(Entity owner) { Assert.True(query(owner, out var tile), owner.BlueprintName); Frame(tile, TextureFor(owner)); return tile; }
        void Denied(Entity owner) => Assert.False(query(owner, out _), owner.BlueprintName);
        FlatPixelGreyboxCell Resolve() { Assert.True(world.TryResolvePresentation(zone, 10, 10, out var value)); return value; }
        static void Frame(Tile tile, Texture2D texture)
        {
            Assert.NotNull(tile); Assert.AreSame(texture, tile.sprite.texture); Assert.AreEqual(new Rect(0, 0, 16, 24), tile.sprite.rect);
            Assert.AreEqual(new Vector2(8, 12), tile.sprite.pivot); Assert.AreEqual(16, tile.sprite.pixelsPerUnit);
            Assert.AreEqual(Tile.ColliderType.None, tile.colliderType);
        }
        static Texture2D Imported(string blueprint)
        {
            var texture = Resources.Load<Texture2D>(Resource(blueprint)); Assert.NotNull(texture);
            Assert.AreEqual(16, texture.width); Assert.AreEqual(24, texture.height);
            Assert.AreEqual(FilterMode.Point, texture.filterMode); Assert.AreEqual(TextureWrapMode.Clamp, texture.wrapMode);
            Assert.AreEqual(1, texture.mipmapCount); return texture;
        }
        static void Equip(Entity viewer, Entity item, bool alreadyCarried = false)
        {
            if (!alreadyCarried) Assert.True(viewer.GetPart<InventoryPart>().AddObject(item));
            var bodyPart = viewer.GetPart<Body>().GetParts().FirstOrDefault(p => p.Type == Slot(item.BlueprintName) && p.Equipped == null);
            Assert.NotNull(bodyPart); Assert.True(InventorySystem.Equip(viewer, item, bodyPart));
            Assert.AreSame(item, bodyPart.Equipped); Assert.AreSame(viewer, item.GetPart<PhysicsPart>().Equipped);
        }
        static void Portrait(Reader reader, AnnouncementDocument document, Texture2D texture)
        {
            Assert.NotNull(document); Assert.True(ExamineDocumentBuilder.IsCurrentSubject(document));
            Assert.True(reader.Presentation.TryGetFlatExaminePortrait(document, out var value));
            Assert.AreSame(texture, value.Texture); Assert.AreEqual(Color.white, value.Tint); Assert.AreEqual(new Rect(0, 0, 1, 1), value.Uv);
        }
        static void Rejected(Reader reader, AnnouncementDocument document)
        { Assert.NotNull(document); Assert.True(reader.Presentation.TryGetFlatExaminePortrait(document, out var value)); Assert.IsNull(value.Texture); }
        sealed class Reader : IDisposable
        {
            readonly GameObject root;
            public readonly Entity Viewer;
            public readonly ZoneRenderer Presentation;
            public Reader(FlatPixelGreyboxCatalog catalog, Zone zone, Entity viewer)
            {
                Viewer = viewer; Assert.True(zone.AddEntity(viewer, 9, 10));
                root = new GameObject("Owned exact equipment reader"); root.SetActive(false);
                Presentation = root.AddComponent<ZoneRenderer>(); var environment = root.AddComponent<EnvironmentSpriteRenderer>();
                typeof(EnvironmentSpriteRenderer).GetField("_flatPixelGreybox", I).SetValue(environment, catalog); environment.FlatPixelGreyboxEnabled = true;
                typeof(ZoneRenderer).GetField("_envSpriteRenderer", I).SetValue(Presentation, environment);
                typeof(ZoneRenderer).GetField("_flatPixelModeEnabled", I).SetValue(Presentation, true);
                typeof(ZoneRenderer).GetProperty("CurrentZone", I).SetValue(Presentation, zone);
                Assert.True(Presentation.FlatPixelModeActive);
            }
            public void Dispose() { if (root != null) Object.DestroyImmediate(root); }
        }
        void SnapshotStatics(Type type)
        { foreach (var field in type.GetFields(S)) if (!field.IsLiteral) Snapshot(field); }
        void Snapshot(FieldInfo field)
        {
            Assert.NotNull(field); object value = field.GetValue(null);
            if (value is Array array)
            { var copy = (Array)array.Clone(); restore.Add(() => Array.Copy(copy, array, copy.Length)); }
            else if (value is IDictionary dictionary)
            {
                var copy = new List<DictionaryEntry>(); foreach (DictionaryEntry entry in dictionary) copy.Add(entry);
                restore.Add(() => { dictionary.Clear(); foreach (var entry in copy) dictionary.Add(entry.Key, entry.Value); });
            }
            else if (value is IList list)
            { var copy = list.Cast<object>().ToArray(); restore.Add(() => { list.Clear(); foreach (var item in copy) list.Add(item); }); }
            else if (value is IEnumerable enumerable && value.GetType().IsGenericType && value.GetType().GetGenericTypeDefinition() == typeof(Queue<>))
            {
                var copy = enumerable.Cast<object>().ToArray(); restore.Add(() => { value.GetType().GetMethod("Clear").Invoke(value, null);
                    foreach (var item in copy) value.GetType().GetMethod("Enqueue").Invoke(value, new[] { item }); });
            }
            if (!field.IsInitOnly) restore.Add(() => field.SetValue(null, value));
        }
    }
}
