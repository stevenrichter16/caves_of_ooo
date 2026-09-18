using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class MorrowfastSceneArtRuntimeTests
    {
        private EntityFactory factory;
        private Zone zone;
        private GameObject root;
        private MorrowfastScenePresenter presenter;
        private MorrowfastArtDefinition art;

        [SetUp] public void Setup()
        {
            factory = MorrowfastTestWorld.Factory();
            zone = new OverworldZoneManager(factory, 64).GetZone(MorrowfastSceneRuntime.ZoneID);
            Assert.IsTrue(MorrowfastSceneRuntime.IsActive(zone), "Normal world generation must install the native scene.");
            SetVisibility(true, true);
            root = new GameObject("Morrowfast native art test");
            presenter = root.AddComponent<MorrowfastScenePresenter>(); presenter.Bind(zone);
            Assert.IsTrue(presenter.IsReady, presenter.Failure);
            art = MorrowfastArtDefinition.Load();
        }
        [TearDown] public void Teardown() { if (root != null) Object.DestroyImmediate(root); }
        private SpriteRenderer View(string id) => root.GetComponentsInChildren<SpriteRenderer>(true).Single(r => r.name == id);
        private void SetVisibility(bool explored, bool visible)
        {
            for (int y = 0; y < 25; y++) for (int x = 0; x < 80; x++)
            { var cell = zone.GetCell(x, y); cell.Explored = explored; cell.IsVisible = visible; }
        }
        private Entity Owner(string id)
        {
            var owner = MorrowfastSceneRuntime.FindOwner(zone, id);
            Assert.NotNull(owner, "The visual assertion requires an actual native owner: " + id); return owner;
        }
        private Vector2 PickableSample(string id)
        {
            var owner = Owner(id);
            foreach (var layer in art.layers.Where(l => l.ownerId == id && l.role != "contact"))
            {
                var view = View(layer.id); var pixels = view.sprite.texture.GetPixels32();
                for (int i = 0; i < pixels.Length; i++)
                {
                    if (pixels[i].a == 0) continue;
                    Vector2 point = (Vector2)view.transform.position + new Vector2(i % layer.bounds[2] + .5f, i / layer.bounds[2] + .5f) / MorrowfastScenePresenter.PixelsPerCell;
                    if (presenter.TryPickWorld(point, out var selected, out _, out _) && ReferenceEquals(selected, owner)) return point;
                }
            }
            Assert.Fail("No visible opaque source pixel resolves to " + id); return Vector2.zero;
        }

        [Test] public void EverySourceLayerHasOneImportedRendererAndSeparateNativeOwner()
        {
            Assert.AreEqual(art.owners.Length, presenter.ComponentCount);
            Assert.AreEqual(art.layers.Length, presenter.LayerCount);
            Assert.AreEqual(art.layers.Length + 1, root.GetComponentsInChildren<SpriteRenderer>(true).Length);
            foreach (var layer in art.layers)
            {
                Assert.NotNull(View(layer.id).sprite, layer.id);
                Assert.IsTrue(presenter.IsAuthoredEntity(Owner(layer.ownerId)), layer.id);
            }
            Assert.IsTrue(presenter.ClaimsCell(21, 0)); Assert.IsTrue(presenter.ClaimsCell(58, 24));
            Assert.IsFalse(presenter.ClaimsCell(20, 0)); Assert.IsFalse(presenter.ClaimsCell(59, 24));
        }

        [Test] public void ActualEnabledRendererOrderReconstructsTheIntactSourcePixelForPixel()
        {
            // Checks actual imported pixels, placement, enablement and renderer sorting.
            // GPU color output is a separate PlayMode screenshot check.
            var expected = Resources.Load<Sprite>(art.baselineResource).texture.GetPixels32();
            var composed = new Color32[1536 * 1024]; int nonbinary = 0;
            foreach (var view in root.GetComponentsInChildren<SpriteRenderer>().Where(r => r.enabled)
                .OrderBy(r => r.sortingOrder).ThenByDescending(r => r.transform.position.z))
            {
                var pixels = view.sprite.texture.GetPixels32(); int width = view.sprite.texture.width, height = view.sprite.texture.height;
                int left = Mathf.RoundToInt((view.transform.position.x - MorrowfastScenePresenter.OriginX) * MorrowfastScenePresenter.PixelsPerCell);
                int bottom = Mathf.RoundToInt(view.transform.position.y * MorrowfastScenePresenter.PixelsPerCell);
                for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                {
                    var p = pixels[y * width + x]; if (p.a != 0 && p.a != 255) nonbinary++;
                    if (p.a != 0) composed[(bottom + y) * 1536 + left + x] = p;
                }
            }
            Assert.AreEqual(0, nonbinary, "Ownership masks must preserve binary pixel contributions.");
            Assert.AreEqual(0, expected.Where((p, i) => !p.Equals(composed[i])).Count(), "Native depth must preserve the intact source composite.");
        }

        [Test] public void MovingAnActualGuardMovesItsSourceSpriteAndReleasesItsOriginalContact()
        {
            const string id = "north-guard-west";
            var owner = Owner(id); var before = zone.GetEntityCell(owner);
            int oldX = before.X, oldY = before.Y; var source = View(id + "-sprite").transform.position;
            Assert.IsTrue(View(id + "-contact").enabled); Assert.IsTrue(presenter.IsRenderedEntity(owner));
            Assert.IsTrue(zone.MoveEntity(owner, 40, 23)); presenter.Refresh();
            Assert.AreEqual(source.x + 40 - oldX, View(id + "-sprite").transform.position.x, .0001f);
            Assert.AreEqual(source.y - 23 + oldY, View(id + "-sprite").transform.position.y, .0001f);
            Assert.IsFalse(View(id + "-contact").enabled, "A moving native actor must not drag old paving with its source pixels.");
            Assert.IsTrue(View(id + "-sprite").enabled); PickableSample(id);
            Assert.IsTrue(zone.MoveEntity(owner, oldX, oldY)); presenter.Refresh();
            Assert.AreEqual(source, View(id + "-sprite").transform.position); Assert.IsTrue(View(id + "-contact").enabled);
        }

        [Test] public void WalkingAcrossTheBridgeCannotPutTheBridgeSurfaceOverTheActor()
        {
            var bridge = View("western-footbridge-sprite");
            var guard = View("north-guard-west-sprite");
            Assert.Less(bridge.sortingOrder, AnimatedEntityRenderer.BodySortingOrder,
                "A flat walking surface must remain below actors on both banks, independent of north/south foot depth.");
            Assert.AreEqual(AnimatedEntityRenderer.BodySortingOrder, guard.sortingOrder,
                "The countercheck keeps upright actors in the native actor layer.");
        }

        [Test] public void OwnerRemovalHidesEveryContributionWithoutChangingItsNeighbours()
        {
            const string id = "cistern-bucket"; var owner = Owner(id); var at = zone.GetEntityPosition(owner);
            Assert.IsTrue(presenter.IsRenderedEntity(owner)); Assert.IsTrue(zone.RemoveEntity(owner)); presenter.Refresh();
            foreach (var layer in art.layers.Where(l => l.ownerId == id)) Assert.IsFalse(View(layer.id).enabled, layer.id);
            Assert.IsTrue(View("central-cistern-sprite").enabled); Assert.IsFalse(presenter.IsRenderedEntity(owner));
            presenter.SetPresentationVisible(false); Assert.IsFalse(presenter.ClaimsCell(40, 20));
            presenter.SetPresentationVisible(true); Assert.IsFalse(View(id + "-sprite").enabled);
            Assert.IsTrue(zone.AddEntity(owner, at.x, at.y)); presenter.Refresh(); Assert.IsTrue(View(id + "-sprite").enabled);
        }

        [Test] public void NativeInteriorMembershipAutomaticallyCutsOnlyTheEnteredRoof()
        {
            var building = MorrowfastSceneDefinition.Load().buildings[0];
            var player = MorrowfastTestWorld.Actor(factory, zone);
            presenter.Refresh(); Assert.IsFalse(presenter.IsRoomRevealed(building.id));
            Assert.IsTrue(View(building.roofId + "-roof").enabled);
            var interior = building.interior.First(c => !zone.GetCell(c.x, c.y).BlocksMovement(player));
            Assert.IsTrue(zone.RemoveEntity(player)); Assert.IsTrue(zone.AddEntity(player, interior.x, interior.y));
            Assert.AreEqual(building.id, MorrowfastSceneRuntime.GetRoomAt(zone, interior.x, interior.y));
            presenter.Refresh(); Assert.IsTrue(presenter.IsRoomRevealed(building.id)); Assert.IsFalse(View(building.roofId + "-roof").enabled);
            foreach (var layer in art.layers.Where(l => l.role == "interior" && l.roomId == building.id)) Assert.IsTrue(View(layer.id).enabled);
            foreach (var other in art.rooms.Where(r => r.id != building.id)) Assert.IsFalse(presenter.IsRoomRevealed(other.id));
            Assert.IsTrue(zone.RemoveEntity(player)); Assert.IsTrue(zone.AddEntity(player, 40, 24)); presenter.Refresh();
            Assert.IsFalse(presenter.IsRoomRevealed(building.id)); Assert.IsTrue(View(building.roofId + "-roof").enabled);
            foreach (var layer in art.layers.Where(l => l.role == "interior" && l.roomId == building.id)) Assert.IsFalse(View(layer.id).enabled);
        }

        [Test] public void RealDoorActionChangesOnlyTheDoorArtAndRetainsItsInteractiveOwner()
        {
            var player = MorrowfastTestWorld.Actor(factory, zone); var building = MorrowfastSceneDefinition.Load().buildings[0];
            var owner = Owner(building.doorId); var approach = MorrowfastTestWorld.Approach(zone, player, owner);
            Assert.IsTrue(zone.MoveEntity(player, approach.x, approach.y));
            var door = owner.GetPart<MorrowfastDoorPart>(); Assert.NotNull(door); Assert.IsTrue(View(building.doorId + "-door").enabled);
            Assert.IsTrue(door.TrySetOpen(player, zone, true)); presenter.Refresh(); Assert.IsFalse(View(building.doorId + "-door").enabled);
            Assert.AreSame(owner, Owner(building.doorId)); Assert.NotNull(zone.GetEntityCell(owner));
            Assert.IsTrue(door.TrySetOpen(player, zone, false)); presenter.Refresh(); Assert.IsTrue(View(building.doorId + "-door").enabled);
        }

        [Test] public void FogPreventsSourceActorLeaksAndAlphaPickingOfRememberedObjects()
        {
            const string id = "north-guard-west"; var sample = PickableSample(id); var owner = Owner(id);
            Assert.IsTrue(presenter.IsRenderedEntity(owner));
            SetVisibility(true, false); presenter.Refresh();
            Assert.IsFalse(presenter.IsRenderedEntity(owner)); Assert.IsFalse(presenter.TryPickWorld(sample, out _, out _, out _));
            SetVisibility(false, false); presenter.Refresh(); Assert.IsFalse(presenter.TryPickWorld(sample, out _, out _, out _));
            SetVisibility(true, true); presenter.Refresh(); Assert.IsTrue(presenter.TryPickWorld(sample, out var selected, out _, out _)); Assert.AreSame(owner, selected);
        }

        [Test] public void LeavingOrDisablingThePresenterReleasesAllNativeRenderingClaims()
        {
            var owner = Owner("north-guard-west"); Assert.IsTrue(presenter.IsAuthoredEntity(owner));
            presenter.SetPresentationVisible(false); Assert.IsFalse(presenter.IsAuthoredEntity(owner)); Assert.IsFalse(presenter.IsRenderedEntity(owner));
            presenter.SetPresentationVisible(true); Assert.IsTrue(presenter.IsRenderedEntity(owner));
            presenter.Bind(new Zone("other")); Assert.IsFalse(presenter.IsReady); Assert.IsFalse(presenter.ClaimsCell(40, 20));
            Assert.IsFalse(presenter.IsAuthoredEntity(owner)); Assert.AreEqual(0, root.GetComponentsInChildren<SpriteRenderer>(true).Length);
        }
    }
}
