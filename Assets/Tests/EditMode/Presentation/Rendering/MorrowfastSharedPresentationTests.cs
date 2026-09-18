using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class MorrowfastSharedPresentationTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private GameObject root;
        private Zone zone;
        private MorrowfastScenePresenter art;
        [SetUp] public void Setup()
        {
            var factory = MorrowfastTestWorld.Factory();
            zone = new OverworldZoneManager(factory, 64).GetZone(MorrowfastSceneRuntime.ZoneID);
            for (int y = 0; y < 25; y++) for (int x = 0; x < 80; x++)
                zone.GetCell(x, y).IsVisible = zone.GetCell(x, y).Explored = true;
            root = new GameObject("Morrowfast shared presentation fixture");
            art = root.AddComponent<MorrowfastScenePresenter>(); art.Bind(zone); Assert.IsTrue(art.IsReady, art.Failure);
        }
        [TearDown] public void Teardown() { if (root != null) Object.DestroyImmediate(root); }

        [Test] public void AnimatedCatalogExclusionSuppressesOneSourceBodyAndRestoresOrdinaryBodies()
        {
            var renderer = root.AddComponent<AnimatedEntityRenderer>(); renderer.Init(null);
            var source = MorrowfastSceneRuntime.FindOwner(zone, "western-bank-frog");
            Assert.NotNull(source); Assert.IsTrue(EntityVisualCatalog.TryGetAsset(source, out _), "Source frog must overlap the actual native visual catalog.");
            var ordinary = MorrowfastTestWorld.Factory().CreateEntity("GlasspaneFrog");
            Assert.IsTrue(zone.RemoveEntity(source));
            var plain = new Zone("ordinary"); Assert.IsTrue(plain.AddEntity(source, 40, 10)); Assert.IsTrue(plain.AddEntity(ordinary, 41, 10));
            foreach (int x in new[] { 40, 41 }) plain.GetCell(x, 10).IsVisible = plain.GetCell(x, 10).Explored = true;
            bool ownsSource = true;
            renderer.SetSourceEntityPredicate(e => ownsSource && ReferenceEquals(e, source));
            Assert.IsFalse(renderer.CanRender(source)); Assert.IsTrue(renderer.CanRender(ordinary));
            renderer.SyncZone(plain); Assert.AreEqual(1, renderer.ActiveViewCount);
            ownsSource = false; renderer.SyncZone(plain);
            Assert.IsTrue(renderer.CanRender(source)); Assert.AreEqual(2, renderer.ActiveViewCount);
            ownsSource = true; renderer.SyncZone(plain); Assert.AreEqual(1, renderer.ActiveViewCount, "Already pooled native bodies must also release when source ownership starts.");
        }

        [Test] public void ZoneBindingAndDisableHooksIncludeMorrowfastWithoutClaimingOtherZones()
        {
            var renderer = root.AddComponent<ZoneRenderer>();
            typeof(ZoneRenderer).GetField("_morrowfastScenePresenter", Private).SetValue(renderer, art);
            renderer.SetZone(zone); Assert.AreSame(art, renderer.MorrowfastPresenter); Assert.AreSame(zone, art.CurrentZone);
            var claim = typeof(ZoneRenderer).GetMethod("ClaimsAuthoredSceneCell", Private);
            Assert.IsTrue((bool)claim.Invoke(renderer, new object[] { 40, 12 }));
            Assert.IsFalse((bool)claim.Invoke(renderer, new object[] { 0, 12 }));
            typeof(ZoneRenderer).GetMethod("OnDisable", Private).Invoke(renderer, null);
            Assert.IsFalse(art.PresentationVisible); Assert.IsFalse((bool)claim.Invoke(renderer, new object[] { 40, 12 }));
            art.SetPresentationVisible(true); renderer.SetZone(new Zone("ordinary"));
            Assert.IsFalse(art.IsReady); Assert.IsFalse((bool)claim.Invoke(renderer, new object[] { 40, 12 }));
        }

        [Test] public void SharedSourcePredicateFollowsActualNativeOwnerVisibilityAndZone()
        {
            var renderer = root.AddComponent<ZoneRenderer>();
            typeof(ZoneRenderer).GetField("_morrowfastScenePresenter", Private).SetValue(renderer, art); renderer.SetZone(zone);
            var source = MorrowfastSceneRuntime.FindOwner(zone, "western-bank-frog");
            var predicate = typeof(ZoneRenderer).GetMethod("IsSourceSceneBody", Private);
            Assert.IsTrue((bool)predicate.Invoke(renderer, new object[] { source }));
            var ordinary = MorrowfastTestWorld.Factory().CreateEntity("GlasspaneFrog");
            Assert.IsFalse((bool)predicate.Invoke(renderer, new object[] { ordinary }), "A shared blueprint is not source ownership.");
            art.SetPresentationVisible(false); Assert.IsFalse((bool)predicate.Invoke(renderer, new object[] { source }));
            art.SetPresentationVisible(true); Assert.IsTrue((bool)predicate.Invoke(renderer, new object[] { source }));
            renderer.SetZone(new Zone("ordinary")); Assert.IsFalse((bool)predicate.Invoke(renderer, new object[] { source }));
        }

        [TestCase(false, 0)] [TestCase(false, 79)] [TestCase(true, 0)] [TestCase(true, 79)]
        public void CameraFitsAllTopdownRowsAndKeepsPlayerOrLookTargetsVisibleOutsideArt(bool useLook, int outsideX)
        {
            var cameraObject = new GameObject("Morrowfast camera"); cameraObject.transform.SetParent(root.transform);
            var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.aspect = 16f / 9;
            var follow = cameraObject.AddComponent<CameraFollow>();
            typeof(CameraFollow).GetField("_morrowfastScenePresenter", Private).SetValue(follow, art);
            var player = new Entity { BlueprintName = "Player" }; zone.AddEntity(player, 40, 24);
            follow.Player = player; follow.CurrentZone = zone; follow.SnapToPlayer();
            Assert.AreEqual(40, camera.transform.position.x); Assert.AreEqual(12.75f, camera.transform.position.y);
            Assert.AreEqual(12.75f, camera.orthographicSize); Assert.AreEqual(37.5f / 25.5f, camera.aspect, .0001f);
            if (useLook) follow.SetOverrideTargetCell(outsideX, 24);
            else { zone.RemoveEntity(player); zone.AddEntity(player, outsideX, 24); }
            follow.SnapToPlayer();
            float halfWidth = camera.orthographicSize * camera.aspect, trackedX = outsideX + .5f;
            Assert.That(trackedX, Is.InRange(camera.transform.position.x - halfWidth, camera.transform.position.x + halfWidth));
            Assert.AreEqual(12.75f, camera.transform.position.y);
            follow.ClearOverrideTarget(); zone.RemoveEntity(player); zone.AddEntity(player, 40, 24);
            art.SetPresentationVisible(false); follow.SnapToPlayer(); Assert.AreEqual(17, camera.orthographicSize);
            art.SetPresentationVisible(true); follow.SnapToPlayer(); Assert.AreEqual(12.75f, camera.orthographicSize);
            var ordinary = new Zone("ordinary"); zone.RemoveEntity(player); ordinary.AddEntity(player, 40, 24);
            follow.CurrentZone = ordinary; follow.SnapToPlayer(); Assert.AreEqual(17, camera.orthographicSize);
        }

        [Test] public void AuthoredWaterSkipsOnlyItsOwnPermanentPlainWaterCoating()
        {
            var renderer = root.AddComponent<ZoneRenderer>();
            typeof(ZoneRenderer).GetField("_morrowfastScenePresenter", Private).SetValue(renderer, art); renderer.SetZone(zone);
            var poolOwner = zone.GetAllEntities().First(e => e.HasTag("MorrowfastAuthoredTerrain") && e.HasPart<LiquidPoolPart>());
            var at = zone.GetEntityCell(poolOwner);
            var state = new ZoneTileState.TileState(); state.Coatings.Add(new ZoneTileState.Layer { Id = "water", Turns = ZoneTileState.Permanent });
            var method = typeof(ZoneRenderer).GetMethod("IsAuthoredRiverWater", Private);
            bool Hidden() => (bool)method.Invoke(renderer, new object[] { at.X, at.Y, at, state });
            Assert.IsTrue(Hidden());
            state.Coatings[0].Turns = 3; Assert.IsFalse(Hidden(), "Fresh gameplay puddles retain their visual marks.");
            state.Coatings[0].Turns = ZoneTileState.Permanent; state.Coatings.Add(new ZoneTileState.Layer { Id = "oil", Turns = 3 });
            Assert.IsFalse(Hidden(), "A mixed or changed coating cannot be hidden as baked source water.");
            state.Coatings.RemoveAt(1); art.SetPresentationVisible(false); Assert.IsFalse(Hidden());
        }
    }
}
