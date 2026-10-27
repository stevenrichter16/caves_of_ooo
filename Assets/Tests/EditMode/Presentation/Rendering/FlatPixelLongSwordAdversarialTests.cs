using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Post-RED counters staged outside Assets. These are not independently RED-proven.
    public sealed class FlatPixelLongSwordAdversarialTests
    {
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        EntityFactory factory;
        Texture2D bladeArt, baseArt;
        FlatPixelOpeningArtCatalog art;
        FlatPixelGreyboxCatalog world;
        Zone zone;
        Action<int, int, string> oldCell;
        Action<string> oldFull;
        [OneTimeSetUp] public void Content() => factory = DensityReferenceGladeTests.Factory();
        [SetUp] public void Setup()
        {
            oldCell = ZoneRenderHooks.CellDirtyCallback; oldFull = ZoneRenderHooks.FullDirtyCallback;
            ZoneRenderHooks.CellDirtyCallback = null; ZoneRenderHooks.FullDirtyCallback = null;
            bladeArt = new Texture2D(16, 24); baseArt = new Texture2D(160, 24);
            zone = new Zone(ReferenceGladePlan.ZoneID);
            for (int y = 9; y <= 11; y++) for (int x = 9; x <= 12; x++)
                zone.GetCell(x, y).Explored = zone.GetCell(x, y).IsVisible = true;
        }
        [TearDown] public void Cleanup()
        {
            try
            {
                art?.Dispose(); world?.Dispose(); art = null; world = null;
                if (bladeArt != null) Object.DestroyImmediate(bladeArt);
                if (baseArt != null) Object.DestroyImmediate(baseArt);
            }
            finally { ZoneRenderHooks.CellDirtyCallback = oldCell; ZoneRenderHooks.FullDirtyCallback = oldFull; }
        }

        [Test] public void InjectedExactBladeCachesOneFullFrameForWorldAndExplicitSubject()
        {
            Create(); var owner = Add(); var cell = Resolve();
            Assert.True(cell.NativeForeground); Assert.AreSame(owner, cell.Owner); Frame(cell.ForegroundTile);
            Assert.AreEqual(Color.white, cell.ForegroundColor);
            Assert.AreEqual(new Vector2(8, 12), cell.ForegroundTile.sprite.pivot);
            Assert.AreEqual(16, cell.ForegroundTile.sprite.pixelsPerUnit);
            Assert.AreEqual(Tile.ColliderType.None, cell.ForegroundTile.colliderType);
            Assert.AreSame(cell.ForegroundTile, Resolve().ForegroundTile);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, 10, 10, out var sprite, out var tint));
            Assert.AreSame(cell.ForegroundTile.sprite, sprite); Assert.AreEqual(Color.white, tint);
        }

        [TestCase("LoanerLongsword")] [TestCase("TemperedLongSword")] [TestCase("ForgedWeapon")]
        [TestCase("ShortSword")] [TestCase("Dagger")]
        public void DistinctAuthoredOrAssembledIdentityNeverBecomesLongSword(string blueprint)
        {
            Create(); var known = factory.CreateEntity("LongSword"); Native(known);
            var other = factory.CreateEntity(blueprint); Assert.NotNull(other);
            other.GetPart<RenderPart>().DisplayName = "long sword";
            other.GetPart<RenderPart>().RenderString = "/";
            other.GetPart<MeleeWeaponPart>().Attributes = "Cutting LongBlades";
            NotNative(other);
            Assert.True(zone.AddEntity(other, 10, 10)); Assert.False(Resolve().NativeForeground);
            Assert.AreSame(other, Resolve().Owner);
        }

        [TestCase("MeleeWeapon")] [TestCase("Equippable")]
        public void ExactBladeStillRequiresItsOwnFunctionalParts(string name)
        {
            Create(); var owner = Add(); Native(owner); var part = owner.GetPart(name); Assert.NotNull(part);
            part.ParentEntity = new Entity(); NotNative(owner);
            part.ParentEntity = owner; Native(owner);
            Assert.True(owner.RemovePart(part)); NotNative(owner); Assert.False(Resolve().NativeForeground);
        }

        [Test] public void ExplicitReskinsForeignRolesAndInvalidPortablePartsDoNotAliasTheBlade()
        {
            Create(); var owner = factory.CreateEntity("LongSword"); Native(owner);
            var render = owner.GetPart<RenderPart>(); var physics = owner.GetPart<PhysicsPart>();
            var stack = owner.GetPart<StackerPart>(); Assert.NotNull(stack);
            render.VisualID = "other"; NotNative(owner); render.VisualID = "";
            render.VisualVariant = "other"; NotNative(owner); render.VisualVariant = "";
            render.GlyphVariants = "/|"; NotNative(owner); render.GlyphVariants = "";
            render.ParentEntity = new Entity(); NotNative(owner); render.ParentEntity = owner;
            render.Visible = false; NotNative(owner); render.Visible = true;
            physics.ParentEntity = new Entity(); NotNative(owner); physics.ParentEntity = owner;
            physics.Solid = true; NotNative(owner); physics.Solid = false;
            physics.Takeable = false; NotNative(owner); physics.Takeable = true;
            owner.Tags.Remove("Item"); NotNative(owner); owner.SetTag("Item");
            foreach (var tag in new[] { "Creature", "Player", "Terrain", "Crop" })
            { owner.SetTag(tag); NotNative(owner); owner.Tags.Remove(tag); }
            var crop = new CropPart(); owner.AddPart(crop); NotNative(owner); owner.RemovePart(crop);
            stack.StackCount = 0; NotNative(owner); stack.StackCount = stack.MaxStack + 1; NotNative(owner); stack.StackCount = 1;
            stack.ParentEntity = new Entity(); NotNative(owner); stack.ParentEntity = owner;
            owner.GetPart<EquippablePart>().Slot = "Body"; NotNative(owner); owner.GetPart<EquippablePart>().Slot = "Hand";
            Native(owner);
        }

        [Test] public void MutableDescriptionConditionAndStatsAreNotBaseShapeStates()
        {
            Create(); var owner = factory.CreateEntity("LongSword"); var original = Native(owner);
            owner.GetPart<RenderPart>().DisplayName = "frost-quenched renamed blade";
            owner.GetPart<RenderPart>().ColorString = "&R"; owner.GetPart<RenderPart>().RenderString = "!";
            owner.GetPart<MeleeWeaponPart>().BaseDamage = "1d10+1";
            owner.GetStat("Hitpoints").BaseValue = 1;
            Assert.AreSame(original, Native(owner)); Assert.IsNull(owner.GetPart<WeaponTemperPart>());
            owner.GetStat("Hitpoints").BaseValue = owner.GetStat("Hitpoints").Max;
            Assert.AreSame(original, Native(owner));
        }

        [TestCase(false, 1)] [TestCase(true, 1)] [TestCase(false, 2)]
        public void RealQuenchChangesOnlyTheActualWeaponUnitAndKeepsItsBaseArt(bool equipped, int units)
        {
            using (var globals = new HotbarSaveFixture(false, false))
            {
                Create();
                using (var f = new Reader(world, zone))
                {
                    var inventory = f.Viewer.GetPart<InventoryPart>();
                    var blade = factory.CreateEntity("LongSword"); blade.GetPart<StackerPart>().StackCount = units;
                    Assert.True(inventory.AddObject(blade)); if (equipped) Assert.True(inventory.Equip(blade, "Hand"));
                    var brew = factory.CreateEntity("BrewedTonic"); Assert.NotNull(brew);
                    // Brewing attaches this resolved payload at runtime; the
                    // authored BrewedTonic shell intentionally has no BrewItem.
                    brew.AddPart(new BrewItemPart { EffectsRaw = "Frozen:2", Form = "Coating" });
                    Assert.True(inventory.AddObject(brew));
                    var original = Native(blade); int oldMax = blade.GetStat("Hitpoints").Max;
                    Assert.True(WeaponTemperingService.TryTemper(f.Viewer, blade, brew, out var changed, out var reason), reason);
                    Assert.NotNull(changed); Assert.AreEqual("LongSword", changed.BlueprintName);
                    Assert.AreEqual(1, changed.GetPart<WeaponTemperPart>().TemperCount);
                    StringAssert.Contains("Frozen,40,,0,2", changed.GetPart<MeleeWeaponPart>().OnHitEffectsRaw);
                    StringAssert.StartsWith("frost-quenched ", changed.GetPart<RenderPart>().DisplayName);
                    Assert.AreEqual(oldMax - 2, changed.GetStat("Hitpoints").Max);
                    Assert.False(inventory.Objects.Contains(brew)); Assert.AreSame(original, Native(changed));
                    if (units == 1) Assert.AreSame(blade, changed);
                    else
                    {
                        Assert.AreNotSame(blade, changed); Assert.AreEqual(1, blade.GetPart<StackerPart>().StackCount);
                        Assert.IsNull(blade.GetPart<WeaponTemperPart>()); Assert.AreEqual(oldMax, blade.GetStat("Hitpoints").Max);
                        Assert.AreSame(original, Native(blade));
                    }
                    var document = ExamineDocumentBuilder.BuildInventory(changed, f.Viewer);
                    Assert.True(ExamineDocumentBuilder.IsCurrentSubject(document));
                    int tick = WorldClock.CurrentTick, entities = zone.EntityCount, count = inventory.Objects.Count;
                    int hp = changed.GetStat("Hitpoints").BaseValue, max = changed.GetStat("Hitpoints").Max;
                    string text = document.ToPlainText(), specs = changed.GetPart<MeleeWeaponPart>().OnHitEffectsRaw;
                    for (int i = 0; i < 3; i++) { Native(changed); Portrait(f, document); }
                    Assert.AreEqual(tick, WorldClock.CurrentTick); Assert.AreEqual(entities, zone.EntityCount);
                    Assert.AreEqual(count, inventory.Objects.Count); Assert.AreEqual(hp, changed.GetStat("Hitpoints").BaseValue);
                    Assert.AreEqual(max, changed.GetStat("Hitpoints").Max); Assert.AreEqual(specs, changed.GetPart<MeleeWeaponPart>().OnHitEffectsRaw);
                    Assert.AreEqual(text, document.ToPlainText()); Assert.AreEqual(1, changed.GetPart<WeaponTemperPart>().TemperCount);
                }
            }
        }

        [TestCase("inventory")] [TestCase("equipped")] [TestCase("hidden")]
        [TestCase("foreign-render")] [TestCase("foreign-physics")] [TestCase("zero-stack")]
        public void LostGroundAuthorityRevealsTheRealLowerOwner(string mutation)
        {
            Create(); var blade = Add(); Assert.True(Resolve().NativeForeground);
            var lower = factory.CreateEntity("Dagger"); lower.GetPart<RenderPart>().RenderLayer = 2;
            Assert.True(zone.AddEntity(lower, 10, 10));
            if (mutation == "inventory") blade.GetPart<PhysicsPart>().InInventory = new Entity();
            if (mutation == "equipped") blade.GetPart<PhysicsPart>().Equipped = new Entity();
            if (mutation == "hidden") blade.GetPart<RenderPart>().Visible = false;
            if (mutation == "foreign-render") blade.GetPart<RenderPart>().ParentEntity = new Entity();
            if (mutation == "foreign-physics") blade.GetPart<PhysicsPart>().ParentEntity = new Entity();
            if (mutation == "zero-stack") blade.GetPart<StackerPart>().StackCount = 0;
            Assert.AreSame(lower, Resolve().Owner); Assert.False(Resolve().NativeForeground);
        }

        [Test] public void MovingTheBladeCannotDuplicateItAtAStaleOldCell()
        {
            Create(); var blade = Add(); Assert.True(Resolve().NativeForeground);
            var old = zone.GetCell(10, 10); Assert.True(zone.MoveEntity(blade, 11, 10)); old.Objects.Add(blade);
            Assert.IsNull(Resolve().Owner); Assert.IsNull(Resolve().ForegroundTile);
            Assert.True(world.TryResolvePresentation(zone, 11, 10, out var moved)); Assert.AreSame(blade, moved.Owner); Frame(moved.ForegroundTile);
        }

        [Test] public void FogStillRejectsWhileCurrentDifferentRegionKeepsSwordIdentity()
        {
            Create(); var blade = Add(); Assert.True(Resolve().NativeForeground);
            zone.GetCell(10, 10).IsVisible = false; Assert.IsNull(Resolve().Owner); Assert.False(Resolve().NativeForeground);
            zone.GetCell(10, 10).Explored = false; Assert.False(world.TryResolvePresentation(zone, 10, 10, out _));
            Assert.True(zone.RemoveEntity(blade));
            var other = new Zone("Overworld.20.20.0"); other.GetCell(10, 10).Explored = other.GetCell(10, 10).IsVisible = true;
            Assert.True(other.AddEntity(blade, 10, 10));
            Assert.True(world.TryResolvePresentation(other, 10, 10, out var portable));
            Assert.AreSame(blade, portable.Owner); Assert.True(portable.NativeForeground); Assert.False(portable.LocalGround);
            Assert.True(world.TryGetPresentationSubjectSprite(other, blade, 10, 10, out var subject, out _));
            Assert.AreSame(portable.ForegroundTile.sprite, subject); Assert.AreSame(bladeArt, subject.texture);
        }

        [Test] public void ActualDropPickupAndEquipRevokeOldDocumentsWithoutAWorldPhantom()
        {
            using (var globals = new HotbarSaveFixture(false, false))
            {
                Create();
                using (var f = new Reader(world, zone))
                {
                    var blade = factory.CreateEntity("LongSword"); var inventory = f.Viewer.GetPart<InventoryPart>();
                    Assert.True(inventory.AddObject(blade)); Assert.True(inventory.Equip(blade, "Hand"));
                    var carried = ExamineDocumentBuilder.BuildInventory(blade, f.Viewer); Portrait(f, carried);
                    Assert.True(InventorySystem.Drop(f.Viewer, blade, zone));
                    Assert.False(ExamineDocumentBuilder.IsCurrentSubject(carried)); NoPortrait(f, carried);
                    var groundCell = zone.GetEntityCell(blade); Assert.NotNull(groundCell);
                    var ground = ExamineDocumentBuilder.BuildWorld(blade, zone, groundCell, f.Viewer); Portrait(f, ground);
                    Assert.True(zone.MoveEntity(f.Viewer, 10, 10));
                    Assert.True(world.TryResolvePresentation(zone, groundCell.X, groundCell.Y, out var loose));
                    Assert.AreSame(blade, loose.Owner); Frame(loose.ForegroundTile);
                    Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(blade, false), f.Viewer, zone).Success);
                    Assert.IsNull(zone.GetEntityCell(blade)); NoPortrait(f, ground);
                    Assert.True(inventory.Equip(blade, "Hand"));
                    Portrait(f, ExamineDocumentBuilder.BuildInventory(blade, f.Viewer));
                    Assert.True(world.TryResolvePresentation(zone, groundCell.X, groundCell.Y, out var after)); Assert.IsNull(after.Owner);
                }
            }
        }

        [Test] public void ExplicitLowerBladeReaderDoesNotBorrowTheCoveringOwner()
        {
            Create();
            using (var f = new Reader(world, zone))
            {
                var blade = Add(); var higher = factory.CreateEntity("Dagger"); higher.GetPart<RenderPart>().RenderLayer = 8;
                Assert.True(zone.AddEntity(higher, 10, 10)); Assert.AreSame(higher, Resolve().Owner);
                Portrait(f, ExamineDocumentBuilder.BuildWorld(blade, zone, zone.GetCell(10, 10), f.Viewer));
            }
        }

        [Test] public void FactoryPlayerHandEquipmentKeepsReaderArtUntilActualDrop()
        {
            using (var globals = new HotbarSaveFixture(false, false))
            {
                Create(); var player = factory.CreateEntity("Player");
                using (var f = new Reader(world, zone, player))
                {
                    var body = player.GetPart<Body>(); Assert.NotNull(body);
                    var hand = body.GetParts().FirstOrDefault(part => part.Type == "Hand"); Assert.NotNull(hand);
                    var blade = factory.CreateEntity("LongSword");
                    Assert.True(player.GetPart<InventoryPart>().AddObject(blade));
                    Assert.True(InventorySystem.Equip(player, blade, hand));
                    Assert.AreSame(blade, hand.Equipped); Assert.AreSame(player, blade.GetPart<PhysicsPart>().Equipped);
                    var document = ExamineDocumentBuilder.BuildInventory(blade, player);
                    f.Presentation.Paused = true; Portrait(f, document);
                    Assert.True(InventorySystem.Drop(player, blade, zone));
                    Assert.AreNotSame(blade, hand.Equipped); Assert.IsNull(blade.GetPart<PhysicsPart>().Equipped);
                    NoPortrait(f, document); Assert.NotNull(zone.GetEntityCell(blade));
                    Portrait(f, ExamineDocumentBuilder.BuildWorld(blade, zone, zone.GetEntityCell(blade), player));
                }
            }
        }

        [TestCase("lost")] [TestCase("transfer")] [TestCase("hidden")] [TestCase("foreign-graph")] [TestCase("viewer-left")]
        public void PreviouslyAuthorizedReaderCannotRetainTheBladeAfterAuthorityChanges(string change)
        {
            Create();
            using (var f = new Reader(world, zone))
            {
                var blade = factory.CreateEntity("LongSword"); var inventory = f.Viewer.GetPart<InventoryPart>();
                Assert.True(inventory.AddObject(blade)); var document = ExamineDocumentBuilder.BuildInventory(blade, f.Viewer);
                Portrait(f, document);
                if (change == "lost") Assert.True(inventory.RemoveObject(blade));
                if (change == "transfer") { Assert.True(inventory.RemoveObject(blade)); var actor = new Entity(); actor.AddPart(new InventoryPart()); Assert.True(actor.GetPart<InventoryPart>().AddObject(blade)); }
                if (change == "hidden") blade.GetPart<RenderPart>().Visible = false;
                if (change == "foreign-graph") f.SetZone(new Zone(zone.ZoneID));
                if (change == "viewer-left") Assert.True(zone.RemoveEntity(f.Viewer));
                NoPortrait(f, document);
            }
        }

        [TestCase(0, 0)] [TestCase(15, 24)] [TestCase(16, 23)] [TestCase(32, 24)]
        public void MissingOrMalformedIndependentFramePreservesFallbackAndBaseArt(int width, int height)
        {
            Object.DestroyImmediate(bladeArt); bladeArt = width == 0 ? null : new Texture2D(width, height); Create();
            var blade = Add(); NotNative(blade); Assert.False(Resolve().NativeForeground); Assert.AreSame(blade, Resolve().Owner);
            var player = factory.CreateEntity("Player"); Assert.True(art.TryResolve(player, 0, 0, out var tile)); Assert.AreSame(baseArt, tile.sprite.texture);
        }

        [Test] public void OlderInjectedConstructorNeverLoadsTheOmittedBladeResource()
        {
            var blade = factory.CreateEntity("LongSword");
            using (var old = new FlatPixelOpeningArtCatalog(baseArt)) Assert.False(old.TryResolve(blade, 0, 0, out _));
            using (var old = new FlatPixelGreyboxCatalog(baseArt))
            {
                Assert.True(zone.AddEntity(blade, 10, 10)); Assert.True(old.TryResolvePresentation(zone, 10, 10, out var value));
                Assert.False(value.NativeForeground); Assert.AreNotSame(bladeArt, value.ForegroundTile.sprite.texture);
            }
        }

        [Test] public void DisposingOneCatalogLeavesBorrowedTextureAndIndependentPeerAlive()
        {
            Create(); var blade = Add(); var tile = Native(blade); var sprite = tile.sprite;
            art.Dispose(); Assert.True(tile == null); Assert.True(sprite == null);
            Assert.False(art.TryResolve(blade, 0, 0, out _)); Assert.NotNull(bladeArt); Assert.AreEqual(16, bladeArt.width);
            Frame(Resolve().ForegroundTile);
        }

        void Create() { art = New<FlatPixelOpeningArtCatalog>(); world = New<FlatPixelGreyboxCatalog>(); }
        T New<T>()
        {
            var ctor = typeof(T).GetConstructor(Enumerable.Repeat(typeof(Texture2D), 13).ToArray());
            Assert.NotNull(ctor, "An independently optional thirteenth resource is required.");
            return (T)ctor.Invoke(new object[] { baseArt, null, null, null, null, null, null, null, null, null, null, null, bladeArt });
        }
        Entity Add() { var owner = factory.CreateEntity("LongSword"); Assert.True(zone.AddEntity(owner, 10, 10)); return owner; }
        FlatPixelGreyboxCell Resolve() { Assert.True(world.TryResolvePresentation(zone, 10, 10, out var value)); return value; }
        Tile Native(Entity owner) { Assert.True(art.TryResolve(owner, 0, 0, out var tile)); Frame(tile); return tile; }
        void NotNative(Entity owner) { if (art.TryResolve(owner, 0, 0, out var tile)) Assert.AreNotSame(bladeArt, tile.sprite.texture); }
        void Frame(Tile tile) { Assert.NotNull(tile); Assert.AreSame(bladeArt, tile.sprite.texture); Assert.AreEqual(new Rect(0, 0, 16, 24), tile.sprite.rect); }
        void Portrait(Reader f, AnnouncementDocument document)
        {
            Assert.True(ExamineDocumentBuilder.IsCurrentSubject(document));
            Assert.True(f.Presentation.TryGetFlatExaminePortrait(document, out var value));
            Assert.AreSame(bladeArt, value.Texture); Assert.AreEqual(new Rect(0, 0, 1, 1), value.Uv);
            Assert.False(value.IsModel); Assert.AreEqual(Color.white, value.Tint);
        }
        static void NoPortrait(Reader f, AnnouncementDocument document)
        { Assert.True(f.Presentation.TryGetFlatExaminePortrait(document, out var value)); Assert.IsNull(value.Texture); }

        sealed class Reader : IDisposable
        {
            readonly GameObject root;
            internal readonly Entity Viewer;
            internal readonly ZoneRenderer Presentation;
            internal Reader(FlatPixelGreyboxCatalog catalog, Zone zone, Entity existingViewer = null)
            {
                try
                {
                    Viewer = existingViewer ?? new Entity { ID = Guid.NewGuid().ToString("N"), BlueprintName = "ExactBladeViewer" };
                    if (existingViewer == null)
                    {
                        Viewer.SetTag("Player"); Viewer.AddPart(new PhysicsPart()); Viewer.AddPart(new InventoryPart());
                        Viewer.AddPart(new RenderPart { RenderString = "@", Visible = true }); Viewer.AddPart(new ExaminablePart());
                        Viewer.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
                    }
                    Assert.True(zone.AddEntity(Viewer, 9, 10));
                    root = new GameObject("Exact blade reader fixture"); root.SetActive(false);
                    Presentation = root.AddComponent<ZoneRenderer>(); var environment = root.AddComponent<EnvironmentSpriteRenderer>();
                    typeof(EnvironmentSpriteRenderer).GetField("_flatPixelGreybox", I).SetValue(environment, catalog); environment.FlatPixelGreyboxEnabled = true;
                    typeof(ZoneRenderer).GetField("_envSpriteRenderer", I).SetValue(Presentation, environment);
                    typeof(ZoneRenderer).GetField("_flatPixelModeEnabled", I).SetValue(Presentation, true);
                    SetZone(zone); Assert.True(Presentation.FlatPixelModeActive);
                }
                catch { Dispose(); throw; }
            }
            internal void SetZone(Zone zone) => typeof(ZoneRenderer).GetProperty("CurrentZone", I).SetValue(Presentation, zone);
            public void Dispose() { if (root != null) Object.DestroyImmediate(root); }
        }
    }
}
