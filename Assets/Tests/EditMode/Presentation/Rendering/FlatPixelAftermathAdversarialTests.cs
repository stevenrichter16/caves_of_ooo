using System;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Post-RED taxonomy sweep, separate from the per-invariant fixture. These
    // cases are not independently RED-proven; root runs the native editor gate.
    public sealed class FlatPixelAftermathAdversarialTests
    {
        const string Garden = "Overworld.11.8.0";
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        DensityLootTestScope content;
        OverworldZoneManager manager;
        Zone zone;
        Texture2D atlas;
        FlatPixelAftermathCatalog art;
        FlatPixelGreyboxCatalog world;

        [SetUp] public void Setup()
        {
            content = new DensityLootTestScope();
            manager = OverworldZoneManager.CreateDetached(content.Factory, 65, true);
            zone = new Zone(Garden); manager.SetActiveZone(zone);
            for (int y = 8; y <= 12; y++) for (int x = 8; x <= 12; x++)
                zone.GetCell(x, y).IsVisible = zone.GetCell(x, y).Explored = true;
            ZoneRenderHooks.CellDirtyCallback = null; ZoneRenderHooks.FullDirtyCallback = null;
            atlas = new Texture2D(48, 24);
        }
        [TearDown] public void Cleanup()
        {
            try { world?.Dispose(); art?.Dispose(); if (atlas != null) Object.DestroyImmediate(atlas); }
            finally { world = null; art = null; atlas = null; content?.Dispose(); }
        }
        void Create(Texture2D texture)
        {
            art = new FlatPixelAftermathCatalog(texture);
            world = new FlatPixelGreyboxCatalog((Texture2D)null);
            ArtField.SetValue(world, art);
        }
        static FieldInfo ArtField => typeof(FlatPixelGreyboxCatalog).GetField("_aftermathArt", I);
        Entity Owner(int frame)
        {
            var owner = content.Factory.CreateEntity(frame == 2 ? "VenomGland" : "CreatureCorpse");
            if (frame < 2)
            {
                owner.Properties["SourceBlueprint"] = frame == 0 ? "Magpie" : "Viper";
                owner.Properties["SourceID"] = "absent-original-animal";
            }
            return owner;
        }
        Entity Add(int frame) { var owner = Owner(frame); Assert.True(zone.AddEntity(owner, 10, 10)); return owner; }
        FlatPixelGreyboxCell Resolve()
        { Assert.True(world.TryResolvePresentation(zone, 10, 10, out var result)); return result; }
        void Native(Reader reader, AnnouncementDocument document, int frame)
        {
            Assert.NotNull(document); Assert.True(ExamineDocumentBuilder.IsCurrentSubject(document));
            Assert.True(reader.Presentation.TryGetFlatExaminePortrait(document, out var portrait));
            Assert.AreSame(atlas, portrait.Texture); Assert.AreEqual(new Rect(frame / 3f, 0, 1f / 3f, 1), portrait.Uv);
        }
        static void NoPortrait(Reader reader, AnnouncementDocument document)
        { Assert.True(reader.Presentation.TryGetFlatExaminePortrait(document, out var portrait)); Assert.IsNull(portrait.Texture); }

        // Multi-instance identity: a loaded replacement may legitimately reuse
        // the saved ID, but an old open document must not acquire its authority.
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void Adversarial_SameIdReplacementRevokesOldWorldReader(int frame)
        {
            Create(atlas); var owner = Add(frame);
            using (var reader = new Reader(world, zone, content.Factory.CreateEntity("Player")))
            {
                var document = ExamineDocumentBuilder.BuildWorld(owner, zone, zone.GetCell(10, 10), reader.Viewer);
                Native(reader, document, frame);
                var replacement = PartRoundTripHelper.RoundTripEntityViaTokenGraph(owner);
                Assert.AreEqual(owner.ID, replacement.ID); Assert.AreNotSame(owner, replacement);
                Assert.True(zone.RemoveEntity(owner)); Assert.True(zone.AddEntity(replacement, 10, 10));
                Assert.False(ExamineDocumentBuilder.IsCurrentSubject(document)); NoPortrait(reader, document);
                Assert.AreSame(replacement, Resolve().Owner); Assert.True(Resolve().NativeForeground);
                Native(reader, ExamineDocumentBuilder.BuildWorld(replacement, zone, zone.GetCell(10, 10), reader.Viewer), frame);
            }
        }

        // Cross-actor custody: even a forged same-ID Physics backlink and stale
        // list entry cannot let the old actor inspect an item another actor owns.
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void Adversarial_TransferToAnotherActorRevokesCarriedReader(int frame)
        {
            Create(atlas); var owner = Owner(frame);
            using (var reader = new Reader(world, zone, content.Factory.CreateEntity("Player")))
            {
                var first = reader.Viewer.GetPart<InventoryPart>(); Assert.True(first.AddObject(owner));
                var document = ExamineDocumentBuilder.BuildInventory(owner, reader.Viewer); Native(reader, document, frame);
                var recipient = content.Factory.CreateEntity("Player"); Assert.True(zone.AddEntity(recipient, 8, 10));
                Assert.True(first.RemoveObject(owner)); Assert.True(recipient.GetPart<InventoryPart>().AddObject(owner));
                Assert.False(ExamineDocumentBuilder.IsCurrentSubject(document)); NoPortrait(reader, document);
                Assert.IsNull(ExamineDocumentBuilder.BuildInventory(owner, reader.Viewer));
                var actual = ExamineDocumentBuilder.BuildInventory(owner, recipient); Native(reader, actual, frame);
                var impostor = new Entity { ID = reader.Viewer.ID };
                first.Objects.Add(owner); owner.GetPart<PhysicsPart>().InInventory = impostor;
                Assert.False(ExamineDocumentBuilder.IsCurrentSubject(document)); NoPortrait(reader, document);
                first.Objects.Remove(owner); owner.GetPart<PhysicsPart>().InInventory = recipient;
                Native(reader, actual, frame); Assert.IsNull(Resolve().Owner);
            }
        }

        // Cached spatial state: editing the serialized shape alone cannot make
        // a stale registered footprint look valid through the exact reader.
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void Adversarial_RawFootprintMutationNeedsCanonicalRepair(int frame)
        {
            Create(atlas); var owner = Add(frame);
            using (var reader = new Reader(world, zone, content.Factory.CreateEntity("Player")))
            {
                var document = ExamineDocumentBuilder.BuildWorld(owner, zone, zone.GetCell(10, 10), reader.Viewer);
                Native(reader, document, frame);
                var footprint = new SpatialFootprintPart { CellsRaw = "0,0" }; owner.AddPart(footprint);
                Assert.True(zone.TryChangeFootprint(owner, "0,0;1,0"));
                Assert.False(art.TryResolve(owner, out _));
                footprint.CellsRaw = "0,0"; Assert.False(CurrentFootprint(owner));
                Assert.IsNull(Resolve().Owner); Assert.False(Resolve().NativeForeground);
                Assert.True(reader.Presentation.TryGetFlatExaminePortrait(document, out var stale));
                Assert.AreNotSame(atlas, stale.Texture); Assert.False(CurrentFootprint(owner), "A read must not repair the index.");
                Assert.True(zone.TryChangeFootprint(owner, "0,0")); Assert.True(CurrentFootprint(owner));
                Assert.AreSame(owner, Resolve().Owner); Assert.True(Resolve().NativeForeground); Native(reader, document, frame);
            }
        }
        bool CurrentFootprint(Entity owner)
            => (bool)typeof(Zone).GetMethod("IsFootprintCurrent", I).Invoke(zone, new object[] { owner });

        // Graph replacement: a still internally valid old snapshot cannot be
        // painted after the manager has installed a different graph at its ID.
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void Adversarial_ReplacedGraphRevokesWorldAndCarriedPresentation(int frame)
        {
            Create(atlas); var owner = Add(frame);
            using (var reader = new Reader(world, zone, content.Factory.CreateEntity("Player")))
            {
                var ground = ExamineDocumentBuilder.BuildWorld(owner, zone, zone.GetCell(10, 10), reader.Viewer);
                Native(reader, ground, frame);
                Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(owner, false), reader.Viewer, zone).Success);
                var carried = ExamineDocumentBuilder.BuildInventory(owner, reader.Viewer); Native(reader, carried, frame);
                manager.CachedZones[Garden] = new Zone(Garden);
                Assert.True(ExamineDocumentBuilder.IsCurrentSubject(carried), "Custody remains valid while presentation graph is stale.");
                NoPortrait(reader, carried); Assert.False(world.TryResolvePresentation(zone, 10, 10, out _));
                Assert.False(world.TryGetPresentationSubjectSprite(zone, owner, 10, 10, out _, out _));
                manager.CachedZones[Garden] = zone; Native(reader, carried, frame);
                Assert.True(InventorySystem.Drop(reader.Viewer, owner, zone));
                var cell = zone.GetEntityCell(owner);
                var dropped = ExamineDocumentBuilder.BuildWorld(owner, zone, cell, reader.Viewer); Native(reader, dropped, frame);
                manager.CachedZones[Garden] = new Zone(Garden); NoPortrait(reader, dropped);
            }
        }

        // Missing/malformed optional resources must fall back through both real
        // routing seams, without substituting the globally available resource.
        [TestCase(0, 0)] [TestCase(16, 24)] [TestCase(48, 23)]
        public void Adversarial_UnavailableAtlasPreservesWorldAndReaderGlyphs(int width, int height)
        {
            var bad = width == 0 ? null : new Texture2D(width, height);
            try
            {
                Create(bad); var owner = Add(1);
                var resolved = Resolve(); Assert.AreSame(owner, resolved.Owner); Assert.False(resolved.NativeForeground);
                Assert.NotNull(resolved.ForegroundTile?.sprite);
                using (var reader = new Reader(world, zone, content.Factory.CreateEntity("Player")))
                {
                    var document = ExamineDocumentBuilder.BuildWorld(owner, zone, zone.GetCell(10, 10), reader.Viewer);
                    Assert.NotNull(document); Assert.True(reader.Presentation.TryGetFlatExaminePortrait(document, out var portrait));
                    Assert.AreSame(resolved.ForegroundTile.sprite.texture, portrait.Texture);
                    Assert.AreNotSame(bad, portrait.Texture); Assert.AreNotSame(atlas, portrait.Texture);
                    Assert.AreSame(art, ArtField.GetValue(world)); Assert.False(art.TryResolve(owner, out _));
                }
            }
            finally { world?.Dispose(); if (bad != null) Object.DestroyImmediate(bad); }
        }

        // Lazy resource boundaries: visually similar or living entities cannot
        // eagerly allocate the aftermath companion by their name or glyph.
        [TestCase("CreatureCorpse")] [TestCase("Magpie")] [TestCase("BogSap")]
        public void Adversarial_UnknownOrLivingIdentityDoesNotLoadAftermath(string blueprint)
        {
            world = new FlatPixelGreyboxCatalog(); var other = content.Factory.CreateEntity(blueprint);
            other.Properties["SourceBlueprint"] = "UnknownBird"; other.Properties["SourceID"] = "other-source";
            other.GetPart<RenderPart>().DisplayName = "magpie corpse";
            other.GetPart<RenderPart>().RenderString = "%";
            Assert.True(zone.AddEntity(other, 10, 10)); Assert.AreSame(other, Resolve().Owner);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, other, 10, 10, out _, out _));
            Assert.IsNull(ArtField.GetValue(world));
            Assert.True(zone.RemoveEntity(other)); Add(0); Assert.True(Resolve().NativeForeground);
            Assert.NotNull(ArtField.GetValue(world), "The positive known-role route must actually load the companion.");
        }

        // Stack lifecycle: a fully merged donor remains a C# reference but is
        // no longer an inspectable unit; only the retained recipient stays native.
        [Test] public void Adversarial_FullyMergedGlandCannotKeepAnOldPortrait()
        {
            Create(atlas);
            using (var reader = new Reader(world, zone, content.Factory.CreateEntity("Player")))
            {
                var inventory = reader.Viewer.GetPart<InventoryPart>(); var donor = Owner(2);
                Assert.True(inventory.AddObject(donor)); var old = ExamineDocumentBuilder.BuildInventory(donor, reader.Viewer);
                Native(reader, old, 2); Assert.True(inventory.RemoveObject(donor));
                var retained = Owner(2); Assert.True(inventory.AddObject(retained)); Assert.True(inventory.AddObject(donor));
                Assert.AreEqual(0, donor.GetPart<StackerPart>().StackCount); Assert.False(inventory.Objects.Contains(donor));
                Assert.AreEqual(2, retained.GetPart<StackerPart>().StackCount);
                Assert.False(art.TryResolve(donor, out _)); NoPortrait(reader, old);
                Native(reader, ExamineDocumentBuilder.BuildInventory(retained, reader.Viewer), 2);
            }
        }

        // Transaction refusal: failed pickup leaves the same real world owner,
        // reader and frame intact; presentation cannot transfer or consume it.
        [Test] public void Adversarial_OverweightPickupDoesNotLoseWorldArtwork()
        {
            Create(atlas); var owner = Add(0);
            using (var reader = new Reader(world, zone, content.Factory.CreateEntity("Player")))
            {
                var document = ExamineDocumentBuilder.BuildWorld(owner, zone, zone.GetCell(10, 10), reader.Viewer);
                Native(reader, document, 0); var original = Resolve().ForegroundTile;
                var inventory = reader.Viewer.GetPart<InventoryPart>(); inventory.MaxWeight = 0;
                Assert.Greater(owner.GetPart<PhysicsPart>().Weight, 0);
                Assert.False(InventorySystem.ExecuteCommand(new PickupCommand(owner, false), reader.Viewer, zone).Success);
                Assert.AreSame(owner, Resolve().Owner); Assert.AreSame(original, Resolve().ForegroundTile);
                Assert.IsNull(owner.GetPart<PhysicsPart>().InInventory); Assert.False(inventory.Objects.Contains(owner));
                Native(reader, document, 0);
                inventory.MaxWeight = -1;
                Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(owner, false), reader.Viewer, zone).Success);
                Assert.IsNull(Resolve().Owner); NoPortrait(reader, document);
                Native(reader, ExamineDocumentBuilder.BuildInventory(owner, reader.Viewer), 0);
            }
        }

        sealed class Reader : IDisposable
        {
            readonly GameObject root;
            public readonly Entity Viewer;
            public readonly ZoneRenderer Presentation;
            public Reader(FlatPixelGreyboxCatalog catalog, Zone target, Entity viewer)
            {
                Viewer = viewer; Assert.True(target.AddEntity(viewer, 9, 10));
                root = new GameObject("Aftermath adversarial exact reader"); root.SetActive(false);
                Presentation = root.AddComponent<ZoneRenderer>(); var environment = root.AddComponent<EnvironmentSpriteRenderer>();
                typeof(EnvironmentSpriteRenderer).GetField("_flatPixelGreybox", I).SetValue(environment, catalog);
                environment.FlatPixelGreyboxEnabled = true;
                typeof(ZoneRenderer).GetField("_envSpriteRenderer", I).SetValue(Presentation, environment);
                typeof(ZoneRenderer).GetField("_flatPixelModeEnabled", I).SetValue(Presentation, true);
                typeof(ZoneRenderer).GetProperty("CurrentZone", I).SetValue(Presentation, target);
                Assert.True(Presentation.FlatPixelModeActive);
            }
            public void Dispose() { if (root != null) Object.DestroyImmediate(root); }
        }
    }
}
