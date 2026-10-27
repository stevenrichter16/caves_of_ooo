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
    public sealed class FlatPixelShaleParityArtTests
    {
        const string Target = "Overworld.16.13.6", CandidatePath = "Sprites/Live16x24/shale-parity";
        const string CandidateRubblePath = "Sprites/Live16x24/shale-parity-rubble";
        const int X = 10, Y = 10;
        const BindingFlags Instance = BindingFlags.Instance | BindingFlags.NonPublic;
        DensityLootTestScope content;
        OverworldZoneManager manager;
        Zone zone;
        FlatPixelGreyboxCatalog world;
        FlatPixelShaleCavesCatalog art;
        FlatPixelFallbackCatalog selector;
        FieldInfo enabled;
        bool oldEnabled;
        string oldRoot;
        Texture2D baseline, candidate, rubble, candidateRubble, stalagmite, anchors, cellar;
        readonly List<Texture2D> owned = new List<Texture2D>();

        [SetUp] public void Setup()
        {
            oldRoot = SaveGameService.SaveRootOverride;
            enabled = typeof(ZoneRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelShaleParityStudy")
                ?.GetField("Enabled", BindingFlags.Static | BindingFlags.Public);
            oldEnabled = enabled != null && enabled.FieldType == typeof(bool) && (bool)enabled.GetValue(null);
            content = new DensityLootTestScope();
            Assert.False(string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride));
            manager = OverworldZoneManager.CreateDetached(content.Factory, 65, true);
            Assert.IsNull(manager.WorldMap.GetPOI(16, 13));
            zone = Attach(Target);
            baseline = Texture(96, 24); candidate = Texture(1152, 24);
            rubble = Texture(16, 16); candidateRubble = Texture(16, 16); stalagmite = Texture(16, 16);
            anchors = Texture(192, 24); cellar = Texture(144, 24);
        }

        [TearDown] public void Cleanup()
        {
            try
            {
                world?.Dispose(); art?.Dispose(); selector?.Dispose();
                foreach (var texture in owned) if (texture != null) Object.DestroyImmediate(texture);
                owned.Clear();
            }
            finally
            {
                if (enabled != null && enabled.FieldType == typeof(bool)) enabled.SetValue(null, oldEnabled);
                content?.Dispose();
                SaveGameService.SaveRootOverride = oldRoot;
            }
        }

        Texture2D Texture(int width, int height)
        { var texture = new Texture2D(width, height); owned.Add(texture); return texture; }
        Zone Attach(string id)
        { var graph = new Zone(id); manager.SetActiveZone(graph); foreach (var c in graph.Cells) c.Explored = c.IsVisible = true; return graph; }
        Entity Add(string name, int x = X, int y = Y)
        { var owner = content.Factory.CreateEntity(name); Assert.NotNull(owner); Assert.True(zone.AddEntity(owner, x, y)); return owner; }
        void Enable(bool value)
        { Assert.NotNull(enabled, "Missing study gate."); Assert.AreEqual(typeof(bool), enabled.FieldType); enabled.SetValue(null, value); }
        void Inject(Texture2D suppliedCandidate, bool legacy = false, bool includeRubble = false, Texture2D suppliedRubble = null)
        {
            Enable(true);
            world?.Dispose(); art?.Dispose(); selector?.Dispose();
            selector = new FlatPixelFallbackCatalog();
            if (legacy) art = new FlatPixelShaleCavesCatalog(baseline, rubble, stalagmite, anchors, cellar, selector);
            else
            {
                var signature = includeRubble
                    ? new[] { typeof(Texture2D), typeof(Texture2D), typeof(Texture2D), typeof(Texture2D), typeof(Texture2D), typeof(Texture2D), typeof(FlatPixelFallbackCatalog), typeof(Texture2D) }
                    : new[] { typeof(Texture2D), typeof(Texture2D), typeof(Texture2D), typeof(Texture2D), typeof(Texture2D), typeof(Texture2D), typeof(FlatPixelFallbackCatalog) };
                var ctor = typeof(FlatPixelShaleCavesCatalog).GetConstructor(signature);
                Assert.NotNull(ctor, includeRubble ? "Missing explicit eight-argument rubble injection constructor." : "Missing explicit seven-argument candidate injection constructor.");
                var inputs = includeRubble
                    ? new object[] { baseline, rubble, stalagmite, anchors, cellar, suppliedCandidate, selector, suppliedRubble }
                    : new object[] { baseline, rubble, stalagmite, anchors, cellar, suppliedCandidate, selector };
                art = (FlatPixelShaleCavesCatalog)ctor.Invoke(inputs);
            }
            world = new FlatPixelGreyboxCatalog((Texture2D)null);
            typeof(FlatPixelGreyboxCatalog).GetField("_shaleCavesArt", Instance).SetValue(world, art);
        }
        FlatPixelGreyboxCell View(int x = X, int y = Y)
        { Assert.True(world.TryResolvePresentation(zone, x, y, out var view)); return view; }
        Tile Wall(Entity owner, Texture2D texture, int? mask = null)
        {
            Assert.True(art.TryResolveForeground(zone, X, Y, owner, out var tile));
            Assert.AreSame(texture, tile.sprite.texture);
            Assert.AreEqual(new Vector2(16, 24), tile.sprite.rect.size);
            Assert.AreEqual(new Vector2(8, 12), tile.sprite.pivot);
            Assert.AreEqual(16, tile.sprite.pixelsPerUnit);
            Assert.AreEqual(Tile.ColliderType.None, tile.colliderType);
            int frame = (int)tile.sprite.rect.x / 16;
            if (mask.HasValue) Assert.That(frame, Is.InRange(8 + mask.Value * 4, 11 + mask.Value * 4));
            var view = View(); Assert.True(view.NativeForeground); Assert.AreSame(owner, view.Owner);
            Assert.AreSame(tile, view.ForegroundTile); Assert.AreEqual(Color.white, view.ForegroundColor);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, X, Y, out var sprite, out var tint));
            Assert.AreSame(tile.sprite, sprite); Assert.AreEqual(Color.white, tint);
            return tile;
        }
        void Floor(Entity owner, Texture2D texture, int x = X, int y = Y)
        {
            Assert.True(art.TryResolveGround(zone, x, y, out var tile, out var actual));
            Assert.AreSame(owner, actual); Assert.AreSame(texture, tile.sprite.texture);
            Assert.That((int)tile.sprite.rect.x / 16, Is.InRange(0, texture == candidate ? 7 : 3));
            var view = View(x, y); Assert.True(view.LocalGround); Assert.AreSame(tile, view.GroundTile);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, x, y, out var sprite, out _));
            Assert.AreSame(tile.sprite, sprite);
        }
        Tile Rubble(Entity owner, Texture2D texture)
        {
            Assert.True(art.TryResolveForeground(zone, X, Y, owner, out var tile));
            Assert.AreSame(texture, tile.sprite.texture);
            Assert.AreEqual(new Rect(0, 0, 16, 16), tile.sprite.rect);
            Assert.AreEqual(new Vector2(8, 8), tile.sprite.pivot);
            Assert.AreEqual(16, tile.sprite.pixelsPerUnit);
            Assert.AreEqual(new Vector2(1, 1), new Vector2(tile.sprite.bounds.size.x, tile.sprite.bounds.size.y));
            Assert.AreEqual(Tile.ColliderType.None, tile.colliderType);
            var view = View(); Assert.True(view.NativeForeground); Assert.AreSame(owner, view.Owner);
            Assert.AreSame(tile, view.ForegroundTile); Assert.AreEqual(Color.white, view.ForegroundColor);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, X, Y, out var sprite, out var tint));
            Assert.AreSame(tile.sprite, sprite); Assert.AreEqual(Color.white, tint);
            return tile;
        }
        bool Recess(Entity owner, out Tile tile, out Color tint)
        {
            var method = typeof(FlatPixelShaleCavesCatalog).GetMethod("TryResolveWallRecessBackdrop");
            Assert.NotNull(method, "Missing selected-wall recess underlay policy.");
            object[] args = { zone, X, Y, owner, null, null };
            bool resolved = (bool)method.Invoke(art, args);
            tile = args[4] as Tile; tint = args[5] is Color color ? color : default;
            return resolved;
        }
        void PreserveWallClassification(Entity wall, FlatPixelGreyboxCell studied)
        {
            bool wasEnabled = (bool)enabled.GetValue(null);
            try
            {
                Enable(false);
                var original = View();
                Assert.True(original.NativeForeground); Assert.AreSame(baseline, original.ForegroundTile.sprite.texture);
                Assert.AreSame(wall, original.Owner); Assert.True(original.BlocksMovement);
                // Non-opening regions retain fallback.Kind; the study must not
                // introduce a separate classification change through its backdrop.
                Assert.AreEqual(original.Kind, studied.Kind);
            }
            finally { Enable(wasEnabled); }
        }
        void RecessView(Entity wall, int mask = 1)
        {
            Assert.True(Recess(wall, out var tile, out var tint));
            Assert.AreSame(candidate, tile.sprite.texture);
            Assert.That((int)tile.sprite.rect.x / 16, Is.InRange(0, 7));
            Assert.AreEqual(Color.white, tint);
            Wall(wall, candidate, mask);
            var view = View(); Assert.AreSame(tile, view.GroundTile); Assert.AreEqual(Color.white, view.GroundColor);
            Assert.True(view.AmbientBackdrop); Assert.False(view.LocalGround);
            PreserveWallClassification(wall, view); Assert.AreSame(wall, view.Owner);
            Assert.True(view.BlocksMovement); Assert.True(zone.GetCell(X, Y).BlocksMovement());
            Assert.False(art.TryResolveGround(zone, X, Y, out _, out _), "The underlay must not invent a terrain owner.");
            Assert.IsNull(zone.GetCell(X, Y).Objects.Find(e => e.BlueprintName == "ShaleFloor"));
        }
        void NoRecessView(Entity wall, Texture2D foreground, int? mask = null)
        {
            Assert.False(Recess(wall, out _, out _));
            Wall(wall, foreground, mask);
            var view = View(); Assert.False(view.AmbientBackdrop);
            PreserveWallClassification(wall, view); Assert.AreSame(wall, view.Owner);
            Assert.True(view.BlocksMovement);
        }

        [Test] public void DefaultWorldLoadsSiblingCandidateOnlyForExplicitIsolatedStudy()
        {
            Enable(true); world = new FlatPixelGreyboxCatalog();
            var texture = Resources.Load<Texture2D>(CandidatePath);
            Assert.NotNull(texture, CandidatePath); Assert.AreEqual(new Vector2Int(1152, 24), new Vector2Int(texture.width, texture.height));
            var wall = Add("ShaleWall"); Add("ShaleFloor", X, Y - 1);
            var view = View(); Assert.True(view.NativeForeground); Assert.AreSame(texture, view.ForegroundTile.sprite.texture);
            Assert.That((int)view.ForegroundTile.sprite.rect.x / 16, Is.InRange(12, 15));
            Assert.True(world.TryGetPresentationSubjectSprite(zone, wall, X, Y, out var portrait, out _));
            Assert.AreSame(view.ForegroundTile.sprite, portrait);
            Enable(false); Assert.AreSame(Resources.Load<Texture2D>(FlatPixelShaleCavesCatalog.ResourcePath), View().ForegroundTile.sprite.texture);
        }

        [TestCase(0, -1, 1)] [TestCase(1, 0, 2)] [TestCase(0, 1, 4)] [TestCase(-1, 0, 8)]
        public void CardinalMaskUsesActualDisclosedWalkableShaleFloor(int dx, int dy, int bit)
        {
            Inject(candidate); var wall = Add("ShaleWall"); Wall(wall, candidate, 0);
            var floor = Add("ShaleFloor", X + dx, Y + dy); Wall(wall, candidate, bit);
            Assert.True(zone.RemoveEntity(floor)); Wall(wall, candidate, 0);
        }

        [Test] public void CombinedMaskAndAdjacentSolidWallKeepDistinctTruthfulFaces()
        {
            Inject(candidate); var wall = Add("ShaleWall");
            Add("ShaleFloor", X, Y - 1); Add("ShaleFloor", X + 1, Y); Add("ShaleFloor", X, Y + 1); Add("ShaleFloor", X - 1, Y);
            Wall(wall, candidate, 15);
            var blocker = Add("ShaleWall", X + 1, Y); Wall(wall, candidate, 13);
            Assert.True(zone.RemoveEntity(blocker)); Wall(wall, candidate, 15);
        }

        [TestCase("remembered")] [TestCase("unexplored")] [TestCase("invisible")]
        [TestCase("custom-floor")] [TestCase("heated-floor")]
        public void UndisclosedOrInvalidFloorCannotAdvertiseAnExposedFace(string fault)
        {
            Inject(candidate); var wall = Add("ShaleWall"); var floor = Add("ShaleFloor", X, Y - 1);
            Wall(wall, candidate, 1); var cell = zone.GetCell(X, Y - 1);
            if (fault == "remembered") cell.IsVisible = false;
            if (fault == "unexplored") cell.Explored = cell.IsVisible = false;
            if (fault == "invisible") floor.GetPart<RenderPart>().Visible = false;
            if (fault == "custom-floor") floor.GetPart<RenderPart>().VisualVariant = "other material";
            if (fault == "heated-floor") zone.TileState.AddHeat(X, Y - 1, 1);
            Wall(wall, candidate, 0);
        }

        [TestCase("invisible")] [TestCase("carried")] [TestCase("equipped")] [TestCase("foreign-physics")]
        public void HiddenOrCustodiedSolidCannotChangeDisclosedFloorBoundary(string fault)
        {
            Inject(candidate); var wall = Add("ShaleWall"); Add("ShaleFloor", X, Y - 1); Wall(wall, candidate, 1);
            var blocker = Add("Stalagmite", X, Y - 1); Wall(wall, candidate, 0);
            if (fault == "invisible") blocker.GetPart<RenderPart>().Visible = false;
            if (fault == "carried") blocker.GetPart<PhysicsPart>().InInventory = new Entity();
            if (fault == "equipped") blocker.GetPart<PhysicsPart>().Equipped = new Entity();
            if (fault == "foreign-physics") blocker.GetPart<PhysicsPart>().ParentEntity = new Entity();
            Wall(wall, candidate, 1);
        }

        [TestCase("off")] [TestCase("normal-save")] [TestCase("other-depth")]
        public void GateRevocationReturnsBothFloorAndWallToOriginalAtlas(string fault)
        {
            Inject(candidate); var wall = Add("ShaleWall"); var floor = Add("ShaleFloor", X + 2, Y);
            Wall(wall, candidate, 0); Floor(floor, candidate, X + 2, Y);
            if (fault == "off") Enable(false);
            if (fault == "normal-save") SaveGameService.SaveRootOverride = null;
            if (fault == "other-depth") { zone = Attach("Overworld.16.13.5"); wall = Add("ShaleWall"); floor = Add("ShaleFloor", X + 2, Y); }
            Wall(wall, baseline); Floor(floor, baseline, X + 2, Y);
        }

        [TestCase("missing")] [TestCase("wrong-width")] [TestCase("wrong-height")]
        public void InvalidCandidateFallsBackToOriginalArtAndOriginalAmbient(string fault)
        {
            // The former 36-frame resource is now incomplete, not a valid partial candidate.
            var input = fault == "missing" ? null : fault == "wrong-width" ? Texture(576, 24) : Texture(1152, 16);
            Inject(input); Wall(Add("ShaleWall"), baseline); Floor(Add("ShaleFloor", X + 2, Y), baseline, X + 2, Y);
            Assert.True(art.TryResolveCellBackdrop(zone, X + 3, Y, out _, out var tint));
            Assert.AreEqual((Color)new Color32(84, 88, 92, 255), tint);
        }

        [Test] public void LegacyExplicitConstructorNeverImplicitlyLoadsCandidate()
        {
            Inject(null, legacy: true); var wall = Add("ShaleWall"); Wall(wall, baseline); Floor(Add("ShaleFloor", X + 2, Y), baseline, X + 2, Y);
            Assert.True(art.TryResolveCellBackdrop(zone, X + 3, Y, out _, out var tint));
            Assert.AreEqual((Color)new Color32(84, 88, 92, 255), tint);
            Assert.True(zone.RemoveEntity(wall));
            Rubble(Add("Rubble"), rubble);
        }

        [Test] public void CandidateAmbientIsNotARealFloorAndNeverConcealsLiveState()
        {
            Inject(candidate);
            Assert.False(art.TryResolveGround(zone, X, Y, out _, out _));
            Assert.True(art.TryResolveCellBackdrop(zone, X, Y, out _, out var tint));
            Assert.AreEqual((Color)new Color32(63, 68, 74, 255), tint);
            Assert.True(View().AmbientBackdrop); Assert.False(View().LocalGround); Assert.IsNull(View().Owner);
            Enable(false); Assert.True(art.TryResolveCellBackdrop(zone, X, Y, out _, out tint));
            Assert.AreEqual((Color)new Color32(84, 88, 92, 255), tint);
            Enable(true); zone.TileState.AddHeat(X, Y, 1); string state = zone.TileState.ToSaveString();
            Assert.False(art.TryResolveCellBackdrop(zone, X, Y, out _, out _));
            Assert.AreEqual(state, zone.TileState.ToSaveString());
        }

        [Test] public void NonShaleOwnersAndCustomWallIdentityRetainExistingPresentation()
        {
            Inject(candidate); var wall = Add("ShaleWall"); Wall(wall, candidate, 0);
            wall.GetPart<RenderPart>().VisualVariant = "other identity";
            Assert.False(art.TryResolveForeground(zone, X, Y, wall, out _)); Assert.False(View().NativeForeground);
            Assert.True(zone.RemoveEntity(wall)); var debris = Add("Rubble");
            Assert.True(art.TryResolveForeground(zone, X, Y, debris, out var tile)); Assert.AreSame(rubble, tile.sprite.texture);
            Assert.True(zone.RemoveEntity(debris)); var other = Add("LimestoneWall");
            Assert.False(art.TryResolveForeground(zone, X, Y, other, out _));
        }

        [Test] public void CandidateRetainsSurvivingDamageIdentityAndRefusesGoneWall()
        {
            Inject(candidate); var wall = Add("ShaleWall"); Add("ShaleFloor", X, Y + 1);
            var structure = wall.GetPart<DestructiblePart>();
            Assert.AreEqual(28, structure.MaxHP); Assert.AreEqual(2, structure.Hardness);
            var intact = Wall(wall, candidate, 4);
            structure.HP = 1; Assert.AreSame(intact, Wall(wall, candidate, 4));
            structure.HP = 0; Assert.False(art.TryResolveForeground(zone, X, Y, wall, out _));
            Assert.False(View().NativeForeground);
            structure.HP = 28; structure.Gone = true;
            Assert.False(art.TryResolveForeground(zone, X, Y, wall, out _));
        }

        [Test] public void SpatialVariationUsesAllEightFloorsAndFourInteriorWallFrames()
        {
            Inject(candidate);
            var floorFrames = new HashSet<int>(); var wallFrames = new HashSet<int>();
            for (int x = 2; x < 10; x++) for (int y = 2; y < 10; y++)
            {
                var floor = Add("ShaleFloor", x, y);
                Assert.True(art.TryResolveGround(zone, x, y, out var tile, out var owner));
                Assert.AreSame(floor, owner); Assert.AreSame(candidate, tile.sprite.texture);
                floorFrames.Add((int)tile.sprite.rect.x / 16);
                Assert.True(zone.RemoveEntity(floor));
                var wall = Add("ShaleWall", x, y);
                Assert.True(art.TryResolveForeground(zone, x, y, wall, out tile));
                Assert.AreSame(candidate, tile.sprite.texture); wallFrames.Add((int)tile.sprite.rect.x / 16);
                Assert.True(zone.RemoveEntity(wall));
            }
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3, 4, 5, 6, 7 }, floorFrames);
            CollectionAssert.AreEquivalent(new[] { 8, 9, 10, 11 }, wallFrames);
        }

        [Test] public void DefaultWorldUsesScopedRubbleDerivativeAndRestoresOriginalWhenOff()
        {
            Enable(true); world = new FlatPixelGreyboxCatalog(); var owner = Add("Rubble");
            var texture = Resources.Load<Texture2D>(CandidateRubblePath);
            Assert.NotNull(texture, CandidateRubblePath);
            Assert.AreEqual(new Vector2Int(16, 16), new Vector2Int(texture.width, texture.height));
            var view = View(); Assert.True(view.NativeForeground); Assert.AreSame(owner, view.Owner);
            Assert.AreSame(texture, view.ForegroundTile.sprite.texture);
            Assert.AreEqual(new Rect(0, 0, 16, 16), view.ForegroundTile.sprite.rect);
            Assert.True(world.TryGetPresentationSubjectSprite(zone, owner, X, Y, out var sprite, out _));
            Assert.AreSame(view.ForegroundTile.sprite, sprite);
            Enable(false);
            Assert.AreSame(Resources.Load<Texture2D>("Sprites/Environment/rubble"), View().ForegroundTile.sprite.texture);
        }

        [Test] public void ExplicitRubbleDerivativePreservesNativeSquareAndSameOwnerReader()
        {
            Inject(candidate, includeRubble: true, suppliedRubble: candidateRubble);
            var owner = Add("Rubble"); Rubble(owner, candidateRubble);
            Assert.AreEqual("Rubble", owner.BlueprintName);
            Assert.False(owner.GetPart<PhysicsPart>().Solid);
            Assert.IsNull(owner.GetPart<DestructiblePart>());
            // Existing seven-argument injection intentionally supplies no derivative.
            Inject(candidate); Rubble(owner, rubble);
        }

        [TestCase("missing")] [TestCase("wrong-width")] [TestCase("wrong-height")]
        public void InvalidRubbleDerivativeRetainsOriginalWithoutDisablingCandidateWalls(string fault)
        {
            var input = fault == "missing" ? null : fault == "wrong-width" ? Texture(32, 16) : Texture(16, 24);
            Inject(candidate, includeRubble: true, suppliedRubble: input);
            var owner = Add("Rubble"); Rubble(owner, rubble);
            Assert.True(zone.RemoveEntity(owner)); Wall(Add("ShaleWall"), candidate, 0);
        }

        [TestCase("off")] [TestCase("normal-save")] [TestCase("other-depth")]
        public void RubbleDerivativeRevokesWithStudyScope(string fault)
        {
            Inject(candidate, includeRubble: true, suppliedRubble: candidateRubble);
            var owner = Add("Rubble"); Rubble(owner, candidateRubble);
            if (fault == "off") Enable(false);
            if (fault == "normal-save") SaveGameService.SaveRootOverride = null;
            if (fault == "other-depth") { zone = Attach("Overworld.16.13.5"); owner = Add("Rubble"); }
            Rubble(owner, rubble);
        }

        [TestCase("custom")] [TestCase("carried")] [TestCase("equipped")]
        [TestCase("invisible")] [TestCase("remembered")] [TestCase("unexplored")] [TestCase("actor")]
        public void InvalidOrUndisclosedRubbleCannotClaimStudyDerivative(string fault)
        {
            Inject(candidate, includeRubble: true, suppliedRubble: candidateRubble);
            var owner = Add("Rubble"); Rubble(owner, candidateRubble);
            if (fault == "custom") owner.GetPart<RenderPart>().VisualVariant = "other material";
            if (fault == "carried") owner.GetPart<PhysicsPart>().InInventory = new Entity();
            if (fault == "equipped") owner.GetPart<PhysicsPart>().Equipped = new Entity();
            if (fault == "invisible") owner.GetPart<RenderPart>().Visible = false;
            if (fault == "remembered") zone.GetCell(X, Y).IsVisible = false;
            if (fault == "unexplored") zone.GetCell(X, Y).Explored = zone.GetCell(X, Y).IsVisible = false;
            if (fault == "actor") owner.SetTag("Creature", "true");
            Assert.False(art.TryResolveForeground(zone, X, Y, owner, out _));
            if (world.TryResolvePresentation(zone, X, Y, out var view) && view.ForegroundTile?.sprite != null)
                Assert.AreNotSame(candidateRubble, view.ForegroundTile.sprite.texture);
            if (world.TryGetPresentationSubjectSprite(zone, owner, X, Y, out var sprite, out _) && sprite != null)
                Assert.AreNotSame(candidateRubble, sprite.texture);
        }

        [Test] public void RubbleDerivativeCannotActivateWithoutCompleteMainCandidate()
        {
            Inject(null, includeRubble: true, suppliedRubble: candidateRubble);
            Rubble(Add("Rubble"), rubble);
        }

        [Test] public void RubbleDerivativeDisposalOwnsViewsButPreservesBorrowedTextureAndPeer()
        {
            Inject(candidate, includeRubble: true, suppliedRubble: candidateRubble);
            var owner = Add("Rubble"); var tile = Rubble(owner, candidateRubble); var sprite = tile.sprite;
            var peerSelector = new FlatPixelFallbackCatalog(); FlatPixelShaleCavesCatalog peer = null;
            try
            {
                var ctor = typeof(FlatPixelShaleCavesCatalog).GetConstructor(new[] { typeof(Texture2D), typeof(Texture2D), typeof(Texture2D), typeof(Texture2D), typeof(Texture2D), typeof(Texture2D), typeof(FlatPixelFallbackCatalog), typeof(Texture2D) });
                Assert.NotNull(ctor);
                peer = (FlatPixelShaleCavesCatalog)ctor.Invoke(new object[] { baseline, rubble, stalagmite, anchors, cellar, candidate, peerSelector, candidateRubble });
                art.Dispose(); Assert.True(tile == null); Assert.True(sprite == null);
                Assert.True(candidateRubble != null); Assert.True(rubble != null); Assert.True(candidate != null);
                Assert.False(art.TryResolveForeground(zone, X, Y, owner, out _));
                Assert.True(peer.TryResolveForeground(zone, X, Y, owner, out var other));
                Assert.AreSame(candidateRubble, other.sprite.texture);
            }
            finally { peer?.Dispose(); peerSelector.Dispose(); }
        }

        [Test] public void DisclosedWallRecessUsesGroundPixelsWithoutInventingFloorOrChangingSolidOwner()
        {
            Inject(candidate); var wall = Add("ShaleWall"); var floor = Add("ShaleFloor", X, Y - 1);
            var structure = wall.GetPart<DestructiblePart>(); int hp = structure.HP;
            RecessView(wall);
            Assert.True(zone.RemoveEntity(floor)); NoRecessView(wall, candidate, 0);
            Assert.True(zone.AddEntity(floor, X, Y - 1)); RecessView(wall);
            Assert.AreEqual(hp, structure.HP); Assert.False(structure.Gone);
        }

        [TestCase("water")] [TestCase("heat")] [TestCase("charge")]
        public void CurrentCellStateRefusesRecessAndUsesOriginalOpaqueWall(string state)
        {
            Inject(candidate); var wall = Add("ShaleWall"); Add("ShaleFloor", X, Y - 1); RecessView(wall);
            if (state == "water") zone.TileState.WriteCoating(X, Y, "water", ZoneTileState.Permanent);
            if (state == "heat") zone.TileState.AddHeat(X, Y, 1);
            if (state == "charge") zone.TileState.AddCharge(X, Y, 1);
            string before = zone.TileState.ToSaveString(); NoRecessView(wall, baseline);
            Assert.AreEqual(before, zone.TileState.ToSaveString(), "Presentation cannot consume the blocking state.");
            Assert.Greater(zone.TileState.Clear(X, Y), 0); RecessView(wall);
        }

        [TestCase("real-floor")] [TestCase("foreign-ground")] [TestCase("background")] [TestCase("pool")]
        public void DisclosedCompetingCurrentOwnerRefusesRecessWithoutChangingWallIdentity(string kind)
        {
            Inject(candidate); var wall = Add("ShaleWall"); Add("ShaleFloor", X, Y - 1); RecessView(wall);
            var other = Add(kind == "real-floor" ? "ShaleFloor" : kind == "foreign-ground" ? "StoneFloor" : kind == "pool" ? "BrinePool" : "Signpost");
            if (kind == "background") other.GetPart<RenderPart>().BackgroundColor = "^B";
            // Preserve the valid authored floor definition and exact selected wall.
            if (kind == "background" || kind == "pool") other.GetPart<RenderPart>().RenderLayer = -1;
            Assert.True(zone.RemoveEntity(wall)); Assert.True(zone.AddEntity(wall, X, Y));
            zone.TileState.Clear(X, Y); // Isolate the visible owner veto from any pool seeding.
            Assert.True(zone.TileState.Get(X, Y) == null || zone.TileState.Get(X, Y).IsEmpty);
            NoRecessView(wall, baseline);
            Assert.True(zone.RemoveEntity(other)); RecessView(wall);
        }

        [TestCase("invisible")] [TestCase("carried")] [TestCase("foreign-physics")]
        public void UndisclosedCurrentCoOwnerCannotLeakThroughWallRecess(string fault)
        {
            Inject(candidate); var wall = Add("ShaleWall"); Add("ShaleFloor", X, Y - 1); RecessView(wall);
            var other = Add("Signpost"); var render = other.GetPart<RenderPart>(); var physics = other.GetPart<PhysicsPart>();
            render.RenderLayer = -1; render.BackgroundColor = "^B";
            NoRecessView(wall, baseline);
            if (fault == "invisible") render.Visible = false;
            if (fault == "carried") physics.InInventory = new Entity();
            if (fault == "foreign-physics") physics.ParentEntity = new Entity();
            RecessView(wall);
            render.Visible = true; physics.InInventory = null; physics.ParentEntity = other;
            NoRecessView(wall, baseline);
        }

        [Test] public void RememberedNeighborCannotCreateRecessAndRedisclosureRestoresIt()
        {
            Inject(candidate); var wall = Add("ShaleWall"); Add("ShaleFloor", X, Y - 1); RecessView(wall);
            var neighbor = zone.GetCell(X, Y - 1); neighbor.IsVisible = false;
            NoRecessView(wall, candidate, 0);
            neighbor.IsVisible = true; RecessView(wall);
        }

        [TestCase("off")] [TestCase("missing-candidate")] [TestCase("replaced-graph")]
        public void RecessRevokesWhenStudyResourceOrCurrentAuthorityIsLost(string fault)
        {
            Inject(candidate); var wall = Add("ShaleWall"); Add("ShaleFloor", X, Y - 1); RecessView(wall);
            if (fault == "off") Enable(false);
            if (fault == "missing-candidate") Inject(null);
            if (fault == "replaced-graph") Attach(Target);
            Assert.False(Recess(wall, out _, out _));
            if (fault == "replaced-graph") Assert.False(world.TryResolvePresentation(zone, X, Y, out _));
            else NoRecessView(wall, baseline);
            manager.SetActiveZone(zone); Inject(candidate); RecessView(wall);
        }

        [Test] public void RecessNeverBelongsToAnOccludedWallOrItsCoveringActor()
        {
            Inject(candidate); var wall = Add("ShaleWall"); Add("ShaleFloor", X, Y - 1); RecessView(wall);
            var actor = Add("Player");
            Assert.False(Recess(wall, out _, out _)); Assert.False(Recess(actor, out _, out _));
            var view = View(); Assert.AreSame(actor, view.Owner); Assert.False(view.AmbientBackdrop);
            Assert.True(zone.RemoveEntity(actor)); RecessView(wall);
        }

        [Test] public void DisposalOwnsOnlyCachedViewsAndDoesNotBreakPeerUsingBorrowedTextures()
        {
            Inject(candidate); var wall = Add("ShaleWall"); var first = Wall(wall, candidate, 0); var sprite = first.sprite;
            var peerSelector = new FlatPixelFallbackCatalog(); FlatPixelShaleCavesCatalog peer = null;
            try
            {
                var ctor = typeof(FlatPixelShaleCavesCatalog).GetConstructor(new[] { typeof(Texture2D), typeof(Texture2D), typeof(Texture2D), typeof(Texture2D), typeof(Texture2D), typeof(Texture2D), typeof(FlatPixelFallbackCatalog) });
                peer = (FlatPixelShaleCavesCatalog)ctor.Invoke(new object[] { baseline, rubble, stalagmite, anchors, cellar, candidate, peerSelector });
                art.Dispose(); Assert.True(first == null); Assert.True(sprite == null); Assert.True(candidate != null); Assert.True(baseline != null);
                Assert.False(art.TryResolveForeground(zone, X, Y, wall, out _));
                Assert.True(peer.TryResolveForeground(zone, X, Y, wall, out var peerTile)); Assert.AreSame(candidate, peerTile.sprite.texture);
            }
            finally { peer?.Dispose(); peerSelector.Dispose(); }
        }
    }
}
