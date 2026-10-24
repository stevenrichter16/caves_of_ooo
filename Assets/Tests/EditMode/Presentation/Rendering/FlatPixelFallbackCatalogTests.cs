using System;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>Pure fallback contracts. No default-mode change or live renderer wiring is claimed.</summary>
    public sealed class FlatPixelFallbackCatalogTests
    {
        const int X = 4, Y = 5;
        const BindingFlags Public = BindingFlags.Instance | BindingFlags.Public;
        delegate bool ResolveCall(Zone zone, int x, int y, out FlatPixelGreyboxCell result);
        delegate bool SubjectCall(Entity subject, int x, int y, out Sprite sprite, out Color tint);
        readonly List<IDisposable> owned = new List<IDisposable>();
        readonly List<Texture2D> textures = new List<Texture2D>();
        Texture2D source;
        Zone zone;
        Entity ground;
        Action<int, int, string> oldCellDirty;
        Action<string> oldFullDirty;

        [SetUp] public void Setup()
        {
            oldCellDirty = ZoneRenderHooks.CellDirtyCallback; oldFullDirty = ZoneRenderHooks.FullDirtyCallback;
            ZoneRenderHooks.CellDirtyCallback = null; ZoneRenderHooks.FullDirtyCallback = null;
            source = Atlas(); zone = new Zone("Unskinned.7.8.1"); Reveal(X, Y);
            ground = Add("ForeignStoneFloor", ".", 0); ground.SetTag("Terrain");
            ground.GetPart<RenderPart>().BackgroundColor = "^b";
        }
        [TearDown] public void Cleanup()
        {
            for (int i = owned.Count - 1; i >= 0; i--) owned[i].Dispose(); owned.Clear();
            foreach (var texture in textures) if (texture != null) Object.DestroyImmediate(texture); textures.Clear();
            ZoneRenderHooks.CellDirtyCallback = oldCellDirty; ZoneRenderHooks.FullDirtyCallback = oldFullDirty;
        }

        [Test] public void UnskinnedFallbackDoesNotExpandNativeAdmissionOrChangeExistingLocalArt()
        {
            using (var native = new FlatPixelGreyboxCatalog(null))
            {
                Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone)); Assert.False(native.TryResolve(zone, X, Y, out _));
                var query = BindResolve(native, "TryResolveFallback");
                Assert.True(query(zone, X, Y, out var result)); Assert.AreSame(ground, result.Owner);
                Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone)); Assert.False(native.TryResolve(zone, X, Y, out _));
                var glade = new Zone(ReferenceGladePlan.ZoneID); var local = Make("Wall", "#", 2); local.SetTag("Wall");
                Assert.True(glade.AddEntity(local, X, Y)); glade.GetCell(X, Y).Explored = glade.GetCell(X, Y).IsVisible = true;
                Assert.True(native.TryResolve(glade, X, Y, out var before));
                Assert.True(query(glade, X, Y, out var fallback)); Assert.AreNotSame(before.ForegroundTile, fallback.ForegroundTile);
                Assert.True(native.TryResolve(glade, X, Y, out var after)); Assert.AreSame(before.ForegroundTile, after.ForegroundTile);
                Assert.AreEqual(before.ForegroundColor, after.ForegroundColor);
            }
        }

        [Test] public void NativeFramesCopyActualInkWithoutStretchingAndOwnOnlyTheirCopies()
        {
            var instance = New(source); var resolve = BindResolve(instance); var first = Add("UnfamiliarCreatureA", "A", 5);
            first.SetTag("Creature"); Assert.True(resolve(zone, X, Y, out var a));
            first.GetPart<RenderPart>().RenderString = "B"; Assert.True(resolve(zone, X, Y, out var b));
            Assert.AreNotSame(a.ForegroundTile, b.ForegroundTile); Assert.AreSame(a.GroundTile, b.GroundTile);
            Assert.AreSame(a.ForegroundTile.sprite.texture, b.ForegroundTile.sprite.texture);
            AssertFrame(a.ForegroundTile.sprite, source, 'A'); AssertFrame(b.ForegroundTile.sprite, source, 'B');
            var bg = Pixels(a.GroundTile.sprite); foreach (var p in bg) Assert.AreEqual(new Color32(255,255,255,255), p);
            var borrowedPixels = source.GetPixels32(); var other = BindResolve(New(source)); Assert.True(other(zone, X, Y, out var independent));
            Assert.AreNotSame(b.ForegroundTile, independent.ForegroundTile);
            var tile = b.ForegroundTile; var sprite = tile.sprite; var texture = sprite.texture;
            instance.Dispose(); instance.Dispose(); Assert.True(tile == null); Assert.True(sprite == null); Assert.True(texture == null);
            Assert.True(source != null); CollectionAssert.AreEqual(borrowedPixels, source.GetPixels32());
            Assert.False(resolve(zone, X, Y, out _)); Assert.True(independent.ForegroundTile != null);
        }

        [Test] public void DefaultLoaderCopiesWorldBankAndDisposalNeverClearsSharedWorldOrUiBank()
        {
            var worldBank = CP437TilesetGenerator.GetAtlasTexture(); var worldTile = CP437TilesetGenerator.GetTile('T');
            var uiTile = CP437TilesetGenerator.GetUiTile('T'); var subject = Add("UnknownTree", "T", 4);
            var instance = NewDefault(); var resolve = BindResolve(instance); Assert.True(resolve(zone, X, Y, out var result));
            AssertFrame(result.ForegroundTile.sprite, worldBank, 'T');
            instance.Dispose(); Assert.AreSame(worldBank, CP437TilesetGenerator.GetAtlasTexture());
            Assert.AreSame(worldTile, CP437TilesetGenerator.GetTile('T')); Assert.AreSame(uiTile, CP437TilesetGenerator.GetUiTile('T'));
            Assert.True(worldTile.sprite != null); Assert.True(uiTile.sprite != null);
        }

        [TestCase(null)][TestCase("")][TestCase("AB")][TestCase("\n")][TestCase("\u2603")][TestCase("Z")][TestCase("\u00b0")]
        public void InvalidUnsupportedOrUndrawnGlyphsUseOneVisibleQuestionFrame(string glyph)
        {
            var resolve = BindResolve(New(source)); var subject = Add("UnfamiliarThing", "?", 4);
            Assert.True(resolve(zone, X, Y, out var expected)); var texture = expected.ForegroundTile.sprite.texture;
            subject.GetPart<RenderPart>().RenderString = glyph; Assert.True(resolve(zone, X, Y, out var actual));
            Assert.AreSame(expected.ForegroundTile, actual.ForegroundTile); Assert.AreSame(texture, actual.ForegroundTile.sprite.texture);
            AssertFrame(actual.ForegroundTile.sprite, source, '?');
        }

        [Test] public void VariantsFollowActualCoordinateHashAndSpaceRemainsIntentionallyBlank()
        {
            var resolve = BindResolve(New(source)); var actor = Add("VariableActor", "A", 4); var render = actor.GetPart<RenderPart>();
            render.GlyphVariants = "AB"; int secondX = X + 1;
            while (secondX < Zone.Width - 1 && render.ResolveGlyph(secondX, Y) == render.ResolveGlyph(X, Y)) secondX++;
            Assert.AreNotEqual(render.ResolveGlyph(X, Y), render.ResolveGlyph(secondX, Y), "Counter needs different real variant selection.");
            Assert.True(resolve(zone, X, Y, out var first)); AssertFrame(first.ForegroundTile.sprite, source, render.ResolveGlyph(X, Y));
            Assert.True(zone.MoveEntity(actor, secondX, Y)); Reveal(secondX, Y);
            Assert.True(resolve(zone, secondX, Y, out var second)); AssertFrame(second.ForegroundTile.sprite, source, render.ResolveGlyph(secondX, Y));
            Assert.AreNotSame(first.ForegroundTile, second.ForegroundTile);
            render.GlyphVariants = "\n"; Assert.True(resolve(zone, secondX, Y, out var invalid)); AssertFrame(invalid.ForegroundTile.sprite, source, '?');
            render.GlyphVariants = ""; render.RenderString = " "; Assert.True(resolve(zone, secondX, Y, out var blank));
            foreach (var p in Pixels(blank.ForegroundTile.sprite)) Assert.Zero(p.a);
        }

        [Test] public void AuthoritativeColorsAndBackgroundSurviveWithoutGameplayRenderEvents()
        {
            var resolve = BindResolve(New(source)); var actor = Add("UnfamiliarActor", "A", 4); var render = actor.GetPart<RenderPart>();
            render.ColorString = "&R"; render.TileColor = "&G"; render.DetailColor = "&B"; render.Tile = "not-world-art";
            render.VisualID = "unknown-costume"; render.VisualVariant = "striped"; var probe = new RenderProbe(); actor.AddPart(probe);
            Assert.True(resolve(zone, X, Y, out var a)); Assert.AreEqual(QudColorParser.BrightRed, a.ForegroundColor);
            Assert.AreEqual(QudColorParser.DarkenForBackground(QudColorParser.DarkBlue), a.GroundColor);
            render.BackgroundColor = "^m"; render.ColorString = "&C";
            Assert.True(resolve(zone, X, Y, out var b)); Assert.AreEqual(QudColorParser.BrightCyan, b.ForegroundColor);
            Assert.AreEqual(QudColorParser.DarkenForBackground(QudColorParser.DarkMagenta), b.GroundColor);
            Assert.AreSame(a.ForegroundTile, b.ForegroundTile); Assert.Zero(probe.Calls, "G1.2 must reuse the caller's one Render event, not dispatch a second here.");
            render.BackgroundColor = ""; ground.GetPart<RenderPart>().BackgroundColor = "";
            Assert.True(resolve(zone, X, Y, out var neutral)); Assert.AreEqual(Color.black, neutral.GroundColor);
        }

        [TestCase("invisible")][TestCase("foreign-render")][TestCase("missing-render")][TestCase("foreign-physics")]
        [TestCase("carried")][TestCase("equipped")][TestCase("empty-stack")][TestCase("foreign-stack")]
        public void InvalidTopOwnerExposesValidLowerTerrainInsteadOfGhostIdentity(string fault)
        {
            var resolve = BindResolve(New(source)); var actor = Add("UnknownOwner", "B", 5); actor.SetTag("Creature");
            Assert.True(resolve(zone, X, Y, out var valid)); Assert.AreSame(actor, valid.Owner);
            var foreign = new Entity { ID = "foreign" }; var render = actor.GetPart<RenderPart>(); var physics = actor.GetPart<PhysicsPart>();
            switch (fault)
            {
                case "invisible": render.Visible = false; break;
                case "foreign-render": render.ParentEntity = foreign; break;
                case "missing-render": actor.RemovePart(render); break;
                case "foreign-physics": physics.ParentEntity = foreign; break;
                case "carried": physics.InInventory = foreign; break;
                case "equipped": physics.Equipped = foreign; break;
                case "empty-stack": actor.AddPart(new StackerPart { StackCount = 0 }); break;
                case "foreign-stack": var stack = new StackerPart { StackCount = 1 }; actor.AddPart(stack); stack.ParentEntity = foreign; break;
            }
            Assert.True(resolve(zone, X, Y, out var rejected)); Assert.AreSame(ground, rejected.Owner);
        }

        [Test] public void CanonicalRenderOnlyOwnersRemainVisibleButForeignPresentPhysicsDoesNot()
        {
            var resolve = BindResolve(New(source)); var owner = Make("ProgrammaticRenderOnly", "A", 5);
            owner.RemovePart(owner.GetPart<PhysicsPart>()); Assert.True(zone.AddEntity(owner, X, Y));
            Assert.True(resolve(zone, X, Y, out var renderOnly)); Assert.AreSame(owner, renderOnly.Owner);
            var physics = new PhysicsPart(); owner.AddPart(physics);
            Assert.True(resolve(zone, X, Y, out var physical)); Assert.AreSame(owner, physical.Owner);
            physics.ParentEntity = new Entity { ID = "foreign" };
            Assert.True(resolve(zone, X, Y, out var foreign)); Assert.AreSame(ground, foreign.Owner);
            physics.ParentEntity = owner;
            Assert.True(resolve(zone, X, Y, out var restored)); Assert.AreSame(owner, restored.Owner);
        }

        [Test] public void MovedAndReplacedCellsCannotBorrowStaleOwnerListsOrSameIdReferences()
        {
            var resolve = BindResolve(New(source)); var owner = Add("UnknownMover", "A", 5); var old = zone.GetCell(X, Y);
            Assert.True(resolve(zone, X, Y, out var initial)); Assert.AreSame(owner, initial.Owner);
            Assert.True(zone.MoveEntity(owner, X + 1, Y)); old.Objects.Add(owner);
            Assert.True(resolve(zone, X, Y, out var moved)); Assert.AreSame(ground, moved.Owner);
            var duplicate = Make("DuplicateId", "B", 9); duplicate.ID = ground.ID; old.Objects.Add(duplicate);
            Assert.True(resolve(zone, X, Y, out var noAlias)); Assert.AreSame(ground, noAlias.Owner);
            var replacement = new Cell(X, Y, zone) { Explored = true, IsVisible = true };
            replacement.Objects.AddRange(old.Objects); zone.Cells[X, Y] = replacement;
            Assert.True(resolve(zone, X, Y, out var stale)); Assert.IsNull(stale.Owner); Assert.IsNull(stale.ForegroundTile);
            replacement.ParentZone = new Zone("foreign-cell-parent"); Assert.False(resolve(zone, X, Y, out _));
        }

        [Test] public void LegitimateSecondaryFootprintCellRendersButStaleOrForeignFootprintDoesNot()
        {
            var resolve = BindResolve(New(source)); var owner = Make("UnknownLargeActor", "B", 5); owner.SetTag("Creature");
            var footprint = new SpatialFootprintPart { CellsRaw = "0,0;1,0" }; owner.AddPart(footprint);
            Assert.True(zone.AddEntity(owner, X, Y)); Reveal(X + 1, Y);
            Assert.True(resolve(zone, X + 1, Y, out var valid)); Assert.AreSame(owner, valid.Owner);
            footprint.CellsRaw = "0,0;2,0"; Assert.True(resolve(zone, X + 1, Y, out var stale)); Assert.IsNull(stale.Owner);
            footprint.CellsRaw = "0,0;1,0"; Assert.True(resolve(zone, X + 1, Y, out var restored)); Assert.AreSame(owner, restored.Owner);
            footprint.ParentEntity = new Entity { ID = "foreign" }; Assert.True(resolve(zone, X + 1, Y, out var foreign)); Assert.IsNull(foreign.Owner);
        }

        [TestCase("creature")][TestCase("item")][TestCase("crop")][TestCase("harvest")][TestCase("field")]
        [TestCase("repair")][TestCase("pool")][TestCase("campfire")][TestCase("torch")][TestCase("door")]
        public void FogRejectsLowLayerLiveStateButKeepsDimmedStaticTerrain(string kind)
        {
            var resolve = BindResolve(New(source)); var owner = Add("LiveTerrainDisguise", "B", 1); owner.SetTag("Terrain");
            switch (kind)
            {
                case "creature": owner.SetTag("Creature"); break;
                case "item": owner.SetTag("Item"); owner.GetPart<PhysicsPart>().Takeable = true; break;
                case "crop": owner.AddPart(new CropPart()); break;
                case "harvest": owner.AddPart(new HarvestablePart()); break;
                case "field": owner.AddPart(new FieldHarvestPart()); break;
                case "repair": owner.AddPart(new RepairablePart()); break;
                case "pool": owner.AddPart(new LiquidPoolPart()); break;
                case "campfire": owner.AddPart(new CampfirePart()); break;
                case "torch": owner.AddPart(new TorchLightPart()); break;
                case "door": owner.AddPart(new DoorPart()); break;
            }
            Assert.True(resolve(zone, X, Y, out var visible)); Assert.AreSame(owner, visible.Owner);
            zone.GetCell(X, Y).IsVisible = false; zone.TileState.WriteCoating(X, Y, "water", ZoneTileState.Permanent);
            Assert.True(resolve(zone, X, Y, out var memory)); Assert.AreSame(ground, memory.Owner); Assert.True(memory.Remembered);
            var expected = QudColorParser.Gray * .35f; expected.a = 1; Assert.AreEqual(expected, memory.ForegroundColor);
            Assert.False(memory.BlocksMovement); Assert.AreEqual(FlatPixelGreyboxGround.Ground, memory.GroundKind);
            zone.GetCell(X, Y).Explored = false; Assert.False(resolve(zone, X, Y, out _));
        }

        [Test] public void CurrentGraphAndCoordinateChecksAreIndependentOfNativeBiomeScope()
        {
            var resolve = BindResolve(New(source)); Assert.True(resolve(zone, X, Y, out _));
            Assert.False(resolve(null, X, Y, out _)); Assert.False(resolve(zone, -1, Y, out _)); Assert.False(resolve(zone, X, Zone.Height, out _));
            var manager = OverworldZoneManager.CreateDetached(DensityReferenceGladeTests.Factory(), 64);
            manager.CachedZones[zone.ZoneID] = zone;
            typeof(WorldLocationContext).GetMethod("Attach", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { zone, manager });
            Assert.True(resolve(zone, X, Y, out _)); Assert.False(FlatPixelGreyboxCatalog.IsAdmitted(zone));
            manager.CachedZones[zone.ZoneID] = new Zone(zone.ZoneID); Assert.False(resolve(zone, X, Y, out _));
        }

        [Test] public void SubjectQueryPreservesCarriedIdentityWithoutGrantingWorldOccupancy()
        {
            var instance = New(source); var resolve = BindResolve(instance); var subject = BindSubject(instance);
            var item = Add("UnknownTool", "A", 5); item.SetTag("Item"); item.GetPart<PhysicsPart>().Takeable = true;
            Assert.True(resolve(zone, X, Y, out var world));
            Assert.True(subject(item, X, Y, out var image, out var tint)); Assert.AreSame(world.ForegroundTile.sprite, image); Assert.AreEqual(world.ForegroundColor, tint);
            Assert.True(zone.RemoveEntity(item)); item.GetPart<PhysicsPart>().InInventory = new Entity { ID = "holder" };
            Assert.True(subject(item, X, Y, out var carried, out _)); Assert.AreSame(image, carried);
            Assert.True(resolve(zone, X, Y, out var noGhost)); Assert.AreSame(ground, noGhost.Owner);
            item.GetPart<RenderPart>().Visible = false; Assert.False(subject(item, X, Y, out _, out _));
            item.GetPart<RenderPart>().Visible = true; item.GetPart<RenderPart>().ParentEntity = new Entity();
            Assert.False(subject(item, X, Y, out _, out _));
            using (var native = new FlatPixelGreyboxCatalog(null))
            {
                var seam = BindSubject(native, "TryGetFallbackSubjectSprite"); Assert.True(seam(ground, X, Y, out var g, out _)); Assert.NotNull(g);
            }
        }

        [TestCase("null")][TestCase("wrong-size")][TestCase("unreadable")][TestCase("blank-question")]
        public void InvalidInjectionNeverLoadsGlobalFallback(string fault)
        {
            Texture2D input = source;
            if (fault == "null") input = null;
            if (fault == "wrong-size") { input = new Texture2D(16, 16, TextureFormat.RGBA32, false); textures.Add(input); }
            if (fault == "unreadable") source.Apply(false, true);
            if (fault == "blank-question") { source.SetPixels32(new Color32[256 * 256]); source.Apply(); }
            IDisposable instance = null; Assert.DoesNotThrow(() => instance = New(input));
            Assert.False(BindResolve(instance)(zone, X, Y, out _));
            Assert.False(BindSubject(instance)(ground, X, Y, out _, out _));
        }

        [Test] public void WarmResolutionAllocatesNothingAndReadsChangedColorWithoutNewFrames()
        {
            var resolve = BindResolve(New(source)); var owner = Add("UnknownActor", "A", 5);
            for (int i = 0; i < 32; i++) resolve(zone, X, Y, out _);
            Assert.True(resolve(zone, X, Y, out var before));
            long start = GC.GetAllocatedBytesForCurrentThread(); bool all = true;
            for (int i = 0; i < 1000; i++) all &= resolve(zone, X, Y, out _);
            long end = GC.GetAllocatedBytesForCurrentThread(); Assert.True(all); Assert.AreEqual(start, end);
            owner.GetPart<RenderPart>().ColorString = "&G"; Assert.True(resolve(zone, X, Y, out var after));
            Assert.AreSame(before.ForegroundTile, after.ForegroundTile); Assert.AreEqual(QudColorParser.BrightGreen, after.ForegroundColor);
        }

        IDisposable New(Texture2D atlas)
        {
            var type = FallbackType(); var constructor = type.GetConstructor(new[] { typeof(Texture2D) });
            Assert.NotNull(constructor, "Explicit borrowed world atlas constructor is required.");
            var value = (IDisposable)constructor.Invoke(new object[] { atlas }); owned.Add(value); return value;
        }
        IDisposable NewDefault()
        { var value = (IDisposable)Activator.CreateInstance(FallbackType()); owned.Add(value); return value; }
        static Type FallbackType()
        {
            var type = typeof(FlatPixelGreyboxCatalog).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelFallbackCatalog");
            Assert.NotNull(type, "G1.1 identity-preserving native fallback is not implemented."); return type;
        }
        static ResolveCall BindResolve(object instance, string name = "TryResolve")
        { var method = instance.GetType().GetMethod(name, Public); Assert.NotNull(method, name + " must be explicitly callable."); return (ResolveCall)Delegate.CreateDelegate(typeof(ResolveCall), instance, method); }
        static SubjectCall BindSubject(object instance, string name = "TryGetSubjectSprite")
        { var method = instance.GetType().GetMethod(name, Public); Assert.NotNull(method, name + " must be explicitly callable."); return (SubjectCall)Delegate.CreateDelegate(typeof(SubjectCall), instance, method); }
        Entity Add(string name, string glyph, int layer)
        { var owner = Make(name, glyph, layer); Assert.True(zone.AddEntity(owner, X, Y)); return owner; }
        static Entity Make(string name, string glyph, int layer)
        { var owner = new Entity { ID = Guid.NewGuid().ToString("N"), BlueprintName = name }; owner.AddPart(new RenderPart { RenderString = glyph, ColorString = "&y", RenderLayer = layer }); owner.AddPart(new PhysicsPart()); return owner; }
        void Reveal(int x, int y) { zone.GetCell(x, y).Explored = zone.GetCell(x, y).IsVisible = true; }
        Texture2D Atlas()
        {
            var texture = new Texture2D(256, 256, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[256 * 256];
            for (int c = 0; c < 256; c++)
            {
                if (c == ' ' || c == 'Z' || c == 176) continue;
                int ox = (c % 16) * 16, oy = (15 - c / 16) * 16;
                pixels[(oy + 1) * 256 + ox + 2] = new Color32((byte)c, 160, 230, 255);
                pixels[(oy + 14) * 256 + ox + 13] = new Color32(220, (byte)c, 50, 255);
            }
            texture.SetPixels32(pixels); texture.Apply(); textures.Add(texture); return texture;
        }
        static Color32[] Pixels(Sprite sprite)
        {
            var rect = sprite.rect; var all = sprite.texture.GetPixels32(); var result = new Color32[(int)(rect.width * rect.height)];
            for (int y = 0; y < rect.height; y++) Array.Copy(all, ((int)rect.y + y) * sprite.texture.width + (int)rect.x, result, y * (int)rect.width, (int)rect.width);
            return result;
        }
        static void AssertFrame(Sprite sprite, Texture2D input, char glyph)
        {
            Assert.AreEqual(new Vector2(16, 24), sprite.rect.size); Assert.AreEqual(new Vector2(8, 12), sprite.pivot);
            Assert.AreEqual(16, sprite.pixelsPerUnit); Assert.AreEqual(FilterMode.Point, sprite.texture.filterMode);
            Assert.AreEqual(TextureWrapMode.Clamp, sprite.texture.wrapMode); Assert.AreEqual(1, sprite.texture.mipmapCount);
            var actual = Pixels(sprite); var original = input.GetPixels32(); int ox = glyph % 16 * 16, oy = (15 - glyph / 16) * 16;
            for (int y = 0; y < 24; y++) for (int x = 0; x < 16; x++)
                Assert.AreEqual(y < 4 || y >= 20 ? default(Color32) : original[(oy + y - 4) * input.width + ox + x], actual[y * 16 + x], x + "," + y);
        }
        sealed class RenderProbe : Part
        { public override string Name => "G1RenderProbe"; public int Calls; public override bool HandleEvent(GameEvent e) { if (e.ID == "Render") Calls++; return true; } }
    }
}
