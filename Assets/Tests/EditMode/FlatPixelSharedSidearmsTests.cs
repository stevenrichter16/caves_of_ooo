using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Only the three square legacy identities are new. Existing 16x24 equipment
    // and portable LongSword keep their separate contracts and existing suites.
    public sealed class FlatPixelSharedSidearmsTests
    {
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags S = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly string[] Names = { "Dagger", "ShortSword", "Mace" };
        static readonly string[] Peers = { "LeatherArmor", "Hatchet", "Cudgel" };
        readonly List<Action> restore = new List<Action>();
        readonly List<Texture2D> owned = new List<Texture2D>();
        DensityLootTestScope content;
        OverworldZoneManager manager;
        Zone zone;
        FlatPixelGreyboxCatalog world;
        FlatPixelIncidentalEquipmentCatalog art;
        Texture2D[] inputs;

        [SetUp] public void Setup()
        {
            foreach (var type in new[] { typeof(MessageLog), typeof(EquipmentChangeBus), typeof(WorldAudioHooks), typeof(WorldAudioUtility), typeof(WorldAudioWork), typeof(EntityVisualHooks), typeof(RepairRecipeRegistry) })
                foreach (var field in type.GetFields(S)) if (!field.IsLiteral) Snapshot(field);
            Snapshot(typeof(AnatomyFactory).GetField("_types", S)); Snapshot(typeof(BodyPart).GetField("_nextID", S));
            foreach (string name in new[] { "_buffer", "_writeIndex", "_filledCount", "_droppedCount" }) Snapshot(typeof(Diag).GetField(name, S));
            content = new DensityLootTestScope(); MessageLog.OnMessage = null;
            WorldAudioHooks.Callback = null; WorldAudioUtility.Callback = null; WorldAudioWork.Callback = null; EntityVisualHooks.Reset();
            ZoneRenderHooks.CellDirtyCallback = null; ZoneRenderHooks.FullDirtyCallback = null;
            manager = OverworldZoneManager.CreateDetached(content.Factory, 65, true); Attach();
            inputs = Enumerable.Range(0, 6).Select(i => Texture(16, i < 3 ? 24 : 16)).ToArray();
            world = new FlatPixelGreyboxCatalog();
        }
        [TearDown] public void Cleanup()
        {
            try { world?.Dispose(); art?.Dispose(); foreach (var value in owned) if (value != null) Object.DestroyImmediate(value); owned.Clear(); content?.Dispose(); }
            finally { for (int i = restore.Count - 1; i >= 0; i--) restore[i](); restore.Clear(); }
        }
        Texture2D Texture(int w, int h) { var texture = new Texture2D(w, h); owned.Add(texture); return texture; }
        void Attach(string id = "Overworld.12.11.0")
        { zone = new Zone(id); manager.SetActiveZone(zone); foreach (var cell in zone.Cells) cell.IsVisible = cell.Explored = true; }
        Entity Owner(string name) { var value = content.Factory.CreateEntity(name); Assert.NotNull(value, name); return value; }
        Entity Add(string name, int x = 10, int y = 10) { var value = Owner(name); Assert.True(zone.AddEntity(value, x, y)); return value; }
        static string Resource(string name) => "Sprites/Environment/" + (name == "Dagger" ? "item_dagger" : name == "ShortSword" ? "item_sword" : "item_mace");
        Texture2D TextureFor(Entity owner) => inputs[3 + Array.IndexOf(Names, owner.BlueprintName)];
        static FlatPixelIncidentalEquipmentCatalog Construct(params Texture2D[] textures)
        {
            var ctor = typeof(FlatPixelIncidentalEquipmentCatalog).GetConstructor(Enumerable.Repeat(typeof(Texture2D), textures.Length).ToArray());
            Assert.NotNull(ctor, "Missing independent " + textures.Length + "-texture equipment constructor.");
            return (FlatPixelIncidentalEquipmentCatalog)ctor.Invoke(textures.Cast<object>().ToArray());
        }
        void Install(Texture2D[] textures = null)
        {
            world.Dispose(); art?.Dispose(); world = new FlatPixelGreyboxCatalog((Texture2D)null); art = Construct(textures ?? inputs);
            var field = typeof(FlatPixelGreyboxCatalog).GetField("_incidentalEquipmentArt", I); Assert.NotNull(field); field.SetValue(world, art);
        }
        static void Frame(Tile tile, Texture2D texture, int height = 16)
        {
            Assert.NotNull(tile); Assert.NotNull(tile.sprite); Assert.AreSame(texture, tile.sprite.texture);
            Assert.AreEqual(new Rect(0, 0, 16, height), tile.sprite.rect); Assert.AreEqual(new Vector2(8, height / 2f), tile.sprite.pivot);
            Assert.AreEqual(16, tile.sprite.pixelsPerUnit); Assert.AreEqual(new Vector2(1, height / 16f), (Vector2)tile.sprite.bounds.size);
            Assert.AreEqual(TileFlags.None, tile.flags); Assert.AreEqual(Tile.ColliderType.None, tile.colliderType);
        }
        FlatPixelGreyboxCell View(int x = 10, int y = 10) { Assert.True(world.TryResolvePresentation(zone, x, y, out var value)); return value; }
        Tile Native(Entity owner) { Assert.True(art.TryResolve(owner, out var tile), owner.BlueprintName); Frame(tile, TextureFor(owner)); return tile; }
        Tile World(Entity owner, Texture2D texture)
        {
            var cell = zone.GetEntityCell(owner); Assert.NotNull(cell); var value = View(cell.X, cell.Y);
            Assert.AreSame(owner, value.Owner); Assert.True(value.NativeForeground); Frame(value.ForegroundTile, texture); Assert.AreEqual(Color.white, value.ForegroundColor);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, cell.X, cell.Y, out var sprite, out var tint));
            Assert.AreSame(value.ForegroundTile.sprite, sprite); Assert.AreEqual(Color.white, tint); return value.ForegroundTile;
        }
        static void Portrait(Reader reader, AnnouncementDocument document, Texture2D texture)
        {
            Assert.NotNull(document); Assert.True(ExamineDocumentBuilder.IsCurrentSubject(document));
            Assert.True(reader.Presentation.TryGetFlatExaminePortrait(document, out var image)); Assert.AreSame(texture, image.Texture);
            Assert.AreEqual(new Rect(0, 0, 1, 1), image.Uv); Assert.AreEqual(Color.white, image.Tint); Assert.False(image.IsModel);
        }
        static void Rejected(Reader reader, AnnouncementDocument document)
        { Assert.NotNull(document); Assert.True(reader.Presentation.TryGetFlatExaminePortrait(document, out var image)); Assert.IsNull(image.Texture); }
        void Denied(Entity owner, string reason)
        { Assert.False(art.TryResolve(owner, out var tile), reason); Assert.IsNull(tile, reason); Assert.False(View().NativeForeground, reason); }

        [TestCase("Dagger")] [TestCase("ShortSword")] [TestCase("Mace")]
        public void DefaultResourceIsACompleteSquareInCurrentWorldAndAuthorizedReaderAcrossRegions(string name)
        {
            foreach (var id in new[] { "Overworld.12.11.0", "Overworld.20.20.0" })
            {
                Attach(id); var owner = Add(name);
                Assert.True(View().NativeForeground, "Existing glyph fallback must fail before resource checks.");
                var texture = Resources.Load<Texture2D>(Resource(name)); Assert.NotNull(texture); Assert.AreEqual(16, texture.width); Assert.AreEqual(16, texture.height);
                World(owner, texture);
                using (var reader = new Reader(world, zone, Owner("Player")))
                    Portrait(reader, ExamineDocumentBuilder.BuildWorld(owner, zone, zone.GetCell(10, 10), reader.Viewer), texture);
            }
        }

        [TestCase("Dagger")] [TestCase("ShortSword")] [TestCase("Mace")]
        public void RealPickupEquipAndDropKeepIdentityAndRevokeStaleReaderCustody(string name)
        {
            Install(); var owner = Add(name); var tile = World(owner, TextureFor(owner));
            using (var reader = new Reader(world, zone, Owner("Player")))
            {
                var ground = ExamineDocumentBuilder.BuildWorld(owner, zone, zone.GetCell(10, 10), reader.Viewer); Portrait(reader, ground, TextureFor(owner));
                Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(owner, false), reader.Viewer, zone).Success);
                Assert.IsNull(zone.GetEntityCell(owner)); Assert.IsNull(View().Owner); Rejected(reader, ground);
                var carried = ExamineDocumentBuilder.BuildInventory(owner, reader.Viewer); Portrait(reader, carried, TextureFor(owner));
                var hand = reader.Viewer.GetPart<Body>().GetParts().First(p => p.Type == "Hand" && p.Equipped == null);
                Assert.True(InventorySystem.Equip(reader.Viewer, owner, hand)); Assert.AreSame(owner, hand.Equipped); Assert.AreSame(reader.Viewer, owner.GetPart<PhysicsPart>().Equipped);
                reader.Presentation.Paused = true; Portrait(reader, ExamineDocumentBuilder.BuildInventory(owner, reader.Viewer), TextureFor(owner)); Assert.AreSame(tile, Native(owner));
                Assert.True(InventorySystem.Drop(reader.Viewer, owner, zone)); Rejected(reader, carried);
                var dropped = zone.GetEntityCell(owner); Assert.NotNull(dropped); Portrait(reader, ExamineDocumentBuilder.BuildWorld(owner, zone, dropped, reader.Viewer), TextureFor(owner));
                Assert.True(zone.MoveEntity(reader.Viewer, 8, 10)); Assert.AreSame(tile, World(owner, TextureFor(owner)));
            }
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void MissingOrMalformedSidearmInputCannotReloadResourcesAndPreservesEveryPeer(int missing)
        {
            foreach (var bad in new[] { null, Texture(16, 24) })
            {
                var injected = (Texture2D[])inputs.Clone(); injected[3 + missing] = bad; Install(injected);
                for (int i = 0; i < 6; i++)
                {
                    Attach(); var owner = Add(i < 3 ? Peers[i] : Names[i - 3]); bool available = art.TryResolve(owner, out var tile);
                    Assert.AreEqual(i != missing + 3, available, owner.BlueprintName);
                    if (available) { Frame(tile, inputs[i], i < 3 ? 24 : 16); Assert.True(View().NativeForeground); continue; }
                    Assert.IsNull(tile); Assert.False(View().NativeForeground);
                    if (world.TryGetPresentationSubjectSprite(zone, owner, 10, 10, out var sprite, out _)) Assert.AreNotSame(Resources.Load<Texture2D>(Resource(owner.BlueprintName)), sprite.texture);
                    using (var reader = new Reader(world, zone, Owner("Player")))
                    {
                        var document = ExamineDocumentBuilder.BuildWorld(owner, zone, zone.GetCell(10, 10), reader.Viewer);
                        Assert.True(reader.Presentation.TryGetFlatExaminePortrait(document, out var image)); Assert.AreNotSame(Resources.Load<Texture2D>(Resource(owner.BlueprintName)), image.Texture);
                    }
                }
            }
        }

        [Test] public void LegacyConstructorsAndResourceDisabledWorldKeepAllThreeNewInputsAbsent()
        {
            foreach (int count in new[] { 2, 3 }) using (var old = Construct(inputs.Take(count).ToArray()))
            {
                for (int i = 0; i < count; i++) { Assert.True(old.TryResolve(Owner(Peers[i]), out var tile)); Frame(tile, inputs[i], 24); }
                foreach (string name in Names) Assert.False(old.TryResolve(Owner(name), out _));
            }
            world.Dispose(); world = new FlatPixelGreyboxCatalog((Texture2D)null);
            foreach (var name in Names) { Attach(); Add(name); Assert.False(View().NativeForeground); }
        }

        [TestCase("missing")] [TestCase("foreign")] [TestCase("duplicate")]
        public void ExactSidearmsRequireUniqueOwnedRenderPhysicsWeaponEquipmentAndExamineParts(string fault)
        {
            Install();
            foreach (var name in Names) foreach (var type in new[] { typeof(RenderPart), typeof(PhysicsPart), typeof(MeleeWeaponPart), typeof(EquippablePart), typeof(ExaminablePart) })
            {
                Attach(); var owner = Add(name); World(owner, TextureFor(owner)); var part = owner.Parts.First(p => p.GetType() == type);
                if (fault == "missing") Assert.True(owner.RemovePart(part)); else if (fault == "foreign") part.ParentEntity = new Entity(); else owner.AddPart((Part)Activator.CreateInstance(type));
                Denied(owner, name + "/" + type.Name + "/" + fault);
            }
        }

        [Test] public void ExplicitReskinsIdentityAliasesInvalidStacksAndNonPortableStateStayFallback()
        {
            Install();
            foreach (var name in Names) foreach (var fault in new[] { "visual", "variant", "variants", "invisible", "solid", "fixed", "slot", "empty", "overfull", "stack-owner", "identity" })
            {
                Attach(); var owner = Add(name); World(owner, TextureFor(owner)); var render = owner.GetPart<RenderPart>(); var physical = owner.GetPart<PhysicsPart>(); var stack = owner.GetPart<StackerPart>();
                switch (fault)
                {
                    case "visual": render.VisualID = "foreign"; break; case "variant": render.VisualVariant = "custom"; break; case "variants": render.GlyphVariants = "!/"; break;
                    case "invisible": render.Visible = false; break; case "solid": physical.Solid = true; break; case "fixed": physical.Takeable = false; break;
                    case "slot": owner.GetPart<EquippablePart>().Slot = "Feet"; break; case "empty": stack.StackCount = 0; break; case "overfull": stack.StackCount = stack.MaxStack + 1; break;
                    case "stack-owner": stack.ParentEntity = new Entity(); break; case "identity": owner.Properties["SourceBlueprint"] = name; owner.BlueprintName = name.ToLowerInvariant(); break;
                }
                Denied(owner, name + "/" + fault);
            }
            foreach (var name in new[] { "Battleaxe", "Spear", "LongSword" }) { var owner = Owner(name); owner.Properties["SourceBlueprint"] = "ShortSword"; Assert.False(art.TryResolve(owner, out _)); }
        }

        [Test] public void ForeignActorTerrainArmorAndServiceMeaningsCannotBorrowWeaponFrames()
        {
            Install();
            foreach (var name in Names)
            {
                foreach (var tag in new[] { "Creature", "Player", "Terrain", "Crop", "no-item" })
                { Attach(); var owner = Add(name); World(owner, TextureFor(owner)); if (tag == "no-item") owner.Tags.Remove("Item"); else owner.SetTag(tag); Denied(owner, tag); }
                foreach (var type in new[] { typeof(ArmorPart), typeof(ContainerPart), typeof(CropPart), typeof(ForgePart), typeof(WellPart), typeof(CorpsePart) })
                { Attach(); var owner = Add(name); World(owner, TextureFor(owner)); owner.AddPart((Part)Activator.CreateInstance(type)); Denied(owner, type.Name); }
            }
        }

        [Test] public void NamesColorCombatHealthHeatAndOptionalRepairMetadataAreMutableWithoutPresentationSideEffects()
        {
            Install();
            foreach (var name in Names)
            {
                Attach(); var owner = Add(name); var tile = World(owner, TextureFor(owner)); var probe = new Probe(); owner.AddPart(probe);
                owner.GetPart<RenderPart>().DisplayName = "A named old weapon"; owner.GetPart<RenderPart>().ColorString = "&R";
                owner.GetStat("Hitpoints").BaseValue = 1; owner.GetPart<MeleeWeaponPart>().BaseDamage = "1d8+1"; owner.GetPart<MeleeWeaponPart>().PenBonus += 1;
                owner.GetPart<ThermalPart>().Temperature = 75;
                var repair = owner.GetPart<RepairablePart>();
                if (name == "ShortSword") { Assert.IsNull(repair); Assert.IsNull(owner.GetPart<CompositionPart>()); }
                else { Assert.NotNull(repair); repair.Repaired = true; repair.RepairedBy = new Entity(); repair.RepairCauseID = "past-repair"; }
                int parts = owner.Parts.Count; for (int i = 0; i < 3; i++) Assert.AreSame(tile, World(owner, TextureFor(owner)));
                Assert.Zero(probe.Queries); Assert.AreEqual(parts, owner.Parts.Count); Assert.AreEqual(1, owner.GetStatValue("Hitpoints")); Assert.AreEqual(75, owner.GetPart<ThermalPart>().Temperature);
            }
        }

        [TestCase("Dagger")] [TestCase("Mace")]
        public void RealBrokenAndRepairedEquipmentRetainsNeutralFrameAndConsumesOnlyActualRepairSupply(string name)
        {
            Install(); RepairRecipeRegistry.ResetForTests(); RepairRecipeRegistry.LoadDefaults();
            using (var reader = new Reader(world, zone, Owner("Player")))
            {
                var owner = Owner(name); var inventory = reader.Viewer.GetPart<InventoryPart>(); Assert.True(inventory.AddObject(owner)); var tile = Native(owner);
                var supply = Owner("SteelBladeComponent"); supply.GetPart<StackerPart>().StackCount = 2; Assert.True(inventory.AddObject(supply));
                var repair = owner.GetPart<RepairablePart>(); Assert.True(repair.PortableEquipment); Assert.AreEqual("metal-equipment-brace", repair.RecipeId);
                Assert.True(owner.GetPart<CompositionPart>().Contains("Metal")); owner.GetStat("Hitpoints").BaseValue = 1;
                int hit = owner.GetPart<MeleeWeaponPart>().HitBonus; Assert.True(owner.ApplyEffect(new BrokenEffect(), reader.Viewer, zone));
                Assert.AreEqual(hit - 2, owner.GetPart<MeleeWeaponPart>().HitBonus); Assert.AreSame(tile, Native(owner)); Portrait(reader, ExamineDocumentBuilder.BuildInventory(owner, reader.Viewer), TextureFor(owner));
                Assert.True(repair.TryRepair(reader.Viewer, zone)); Assert.False(owner.HasEffect<BrokenEffect>()); Assert.AreEqual(hit, owner.GetPart<MeleeWeaponPart>().HitBonus);
                Assert.AreEqual(1, supply.GetPart<StackerPart>().StackCount); Assert.AreSame(reader.Viewer, repair.RepairedBy); Assert.AreSame(tile, Native(owner));
                Portrait(reader, ExamineDocumentBuilder.BuildInventory(owner, reader.Viewer), TextureFor(owner)); Assert.AreEqual(1, owner.GetStatValue("Hitpoints"));
                Assert.False(repair.TryRepair(reader.Viewer, zone)); Assert.AreEqual(1, supply.GetPart<StackerPart>().StackCount);
            }
        }

        [Test] public void CoveredSidearmHasItsOwnReaderWithoutReplacingSelectedCoin()
        {
            Install();
            foreach (var name in Names)
            {
                Attach(); var owner = Add(name); var tile = World(owner, TextureFor(owner)); var coin = Add("GoldCoin"); coin.GetPart<RenderPart>().RenderLayer = 6;
                using (var reader = new Reader(world, zone, Owner("Player")))
                {
                    Assert.AreSame(coin, View().Owner); Assert.AreNotSame(tile, View().ForegroundTile);
                    Portrait(reader, ExamineDocumentBuilder.BuildWorld(owner, zone, zone.GetCell(10, 10), reader.Viewer), TextureFor(owner)); Assert.AreSame(coin, View().Owner);
                    Assert.True(zone.RemoveEntity(coin)); Assert.AreSame(tile, World(owner, TextureFor(owner)));
                }
            }
        }

        [Test] public void CurrentFootprintIsRequiredAndRawMutationDoesNotRepairItsSpatialIndex()
        {
            Install();
            foreach (var name in Names)
            {
                Attach(); var owner = Add(name); var footprint = new SpatialFootprintPart { CellsRaw = "0,0" }; owner.AddPart(footprint); var tile = World(owner, TextureFor(owner));
                using (var reader = new Reader(world, zone, Owner("Player")))
                {
                    var document = ExamineDocumentBuilder.BuildWorld(owner, zone, zone.GetCell(10, 10), reader.Viewer); Portrait(reader, document, TextureFor(owner));
                    Assert.True(zone.TryChangeFootprint(owner, "0,0;1,0")); Denied(owner, "registered multi-cell"); footprint.CellsRaw = "0,0";
                    Assert.IsNull(View().Owner); Assert.False(art.TryResolve(owner, out _));
                    Assert.True(reader.Presentation.TryGetFlatExaminePortrait(document, out var image)); Assert.AreNotSame(TextureFor(owner), image.Texture);
                    Assert.True(zone.TryChangeFootprint(owner, "0,0")); Assert.AreSame(tile, World(owner, TextureFor(owner))); Portrait(reader, document, TextureFor(owner));
                    footprint.ParentEntity = new Entity(); Assert.False(art.TryResolve(owner, out _));
                }
            }
        }

        [Test] public void InventoryTransferAndSameIdImpostorCannotKeepTheOldReadersAuthority()
        {
            Install();
            foreach (var name in Names)
            {
                Attach(); using (var reader = new Reader(world, zone, Owner("Player")))
                {
                    var owner = Owner(name); var first = reader.Viewer.GetPart<InventoryPart>(); Assert.True(first.AddObject(owner));
                    var document = ExamineDocumentBuilder.BuildInventory(owner, reader.Viewer); Portrait(reader, document, TextureFor(owner));
                    var recipient = Add("Player", 8); var second = recipient.GetPart<InventoryPart>(); Assert.True(first.RemoveObject(owner)); Assert.True(second.AddObject(owner));
                    Rejected(reader, document); Assert.IsNull(ExamineDocumentBuilder.BuildInventory(owner, reader.Viewer));
                    var current = ExamineDocumentBuilder.BuildInventory(owner, recipient); Portrait(reader, current, TextureFor(owner));
                    first.Objects.Add(owner); owner.GetPart<PhysicsPart>().InInventory = new Entity { ID = reader.Viewer.ID }; Rejected(reader, document);
                    Assert.IsNull(ExamineDocumentBuilder.BuildInventory(owner, reader.Viewer)); first.Objects.Remove(owner); owner.GetPart<PhysicsPart>().InInventory = recipient;
                    Portrait(reader, current, TextureFor(owner)); Assert.IsNull(View().Owner);
                }
            }
        }

        [Test] public void FogCustodyAndReplacedGraphsCannotKeepAStaleWorldOrReaderBody()
        {
            Install();
            foreach (var name in Names)
            {
                Attach(); var owner = Add(name); World(owner, TextureFor(owner));
                using (var reader = new Reader(world, zone, Owner("Player")))
                {
                    var document = ExamineDocumentBuilder.BuildWorld(owner, zone, zone.GetCell(10, 10), reader.Viewer);
                    zone.GetCell(10, 10).IsVisible = false; Assert.IsNull(View().Owner); Rejected(reader, document);
                    zone.GetCell(10, 10).Explored = false; Assert.False(world.TryResolvePresentation(zone, 10, 10, out _)); zone.GetCell(10, 10).IsVisible = zone.GetCell(10, 10).Explored = true;
                    Portrait(reader, document, TextureFor(owner)); var lower = Add("Spear"); lower.GetPart<RenderPart>().RenderLayer = 1;
                    owner.GetPart<PhysicsPart>().InInventory = new Entity(); Assert.AreSame(lower, View().Owner); Rejected(reader, document); owner.GetPart<PhysicsPart>().InInventory = null;
                    owner.GetPart<PhysicsPart>().Equipped = new Entity(); Assert.AreSame(lower, View().Owner); Rejected(reader, document); owner.GetPart<PhysicsPart>().Equipped = null;
                    World(owner, TextureFor(owner)); manager.SetActiveZone(new Zone(zone.ZoneID)); Assert.False(world.TryResolvePresentation(zone, 10, 10, out _)); Rejected(reader, document);
                }
            }
        }

        [Test] public void DisposalOwnsCachedFramesOnlyAndIndependentPeerKeepsUnmodifiedBorrowedTextures()
        {
            Install(); var tiles = Names.Select(name => Native(Owner(name))).ToArray(); var sprites = tiles.Select(tile => tile.sprite).ToArray();
            using (var peer = Construct(inputs))
            {
                for (int i = 0; i < Names.Length; i++) Assert.AreSame(tiles[i], Native(Owner(Names[i])));
                world.Dispose(); world.Dispose(); foreach (var tile in tiles) Assert.True(tile == null); foreach (var sprite in sprites) Assert.True(sprite == null);
                foreach (var name in Names)
                { var owner = Owner(name); Assert.False(art.TryResolve(owner, out _)); Assert.True(peer.TryResolve(owner, out var tile)); Frame(tile, TextureFor(owner)); Assert.True(TextureFor(owner) != null); Assert.AreEqual(FilterMode.Bilinear, TextureFor(owner).filterMode); Assert.AreEqual(TextureWrapMode.Repeat, TextureFor(owner).wrapMode); }
            }
        }

        sealed class Probe : Part
        {
            public int Queries;
            public override bool HandleEvent(GameEvent e) { if (e.ID == "Render" || e.ID == "GetInventoryActions" || e.ID == "InventoryAction") Queries++; return true; }
        }
        sealed class Reader : IDisposable
        {
            readonly GameObject root; readonly EnvironmentSpriteRenderer environment;
            public readonly Entity Viewer; public readonly ZoneRenderer Presentation;
            public Reader(FlatPixelGreyboxCatalog catalog, Zone zone, Entity viewer)
            {
                Viewer = viewer; Assert.True(zone.AddEntity(viewer, 9, 10)); root = new GameObject("Shared sidearm reader"); root.SetActive(false);
                Presentation = root.AddComponent<ZoneRenderer>(); environment = root.AddComponent<EnvironmentSpriteRenderer>();
                typeof(EnvironmentSpriteRenderer).GetField("_flatPixelGreybox", I).SetValue(environment, catalog); environment.FlatPixelGreyboxEnabled = true;
                typeof(ZoneRenderer).GetField("_envSpriteRenderer", I).SetValue(Presentation, environment); typeof(ZoneRenderer).GetField("_flatPixelModeEnabled", I).SetValue(Presentation, true);
                typeof(ZoneRenderer).GetProperty("CurrentZone", I).SetValue(Presentation, zone); Assert.True(Presentation.FlatPixelModeActive);
            }
            public void Dispose()
            {
                // The outer fixture owns this catalog across regions/readers.
                if (environment != null) typeof(EnvironmentSpriteRenderer).GetField("_flatPixelGreybox", I).SetValue(environment, null);
                if (root != null) Object.DestroyImmediate(root);
            }
        }
        void Snapshot(FieldInfo field)
        {
            Assert.NotNull(field); object value = field.GetValue(null);
            if (value is Array array) { var copy = (Array)array.Clone(); restore.Add(() => Array.Copy(copy, array, copy.Length)); }
            else if (value is IDictionary dictionary) { var copy = new List<DictionaryEntry>(); foreach (DictionaryEntry entry in dictionary) copy.Add(entry); restore.Add(() => { dictionary.Clear(); foreach (var entry in copy) dictionary.Add(entry.Key, entry.Value); }); }
            else if (value is IList list) { var copy = list.Cast<object>().ToArray(); restore.Add(() => { list.Clear(); foreach (var item in copy) list.Add(item); }); }
            else if (value is IEnumerable items && value.GetType().IsGenericType && value.GetType().GetGenericTypeDefinition() == typeof(Queue<>))
            { var copy = items.Cast<object>().ToArray(); restore.Add(() => { value.GetType().GetMethod("Clear").Invoke(value, null); foreach (var item in copy) value.GetType().GetMethod("Enqueue").Invoke(value, new[] { item }); }); }
            if (!field.IsInitOnly) restore.Add(() => field.SetValue(null, value));
        }
    }
}
