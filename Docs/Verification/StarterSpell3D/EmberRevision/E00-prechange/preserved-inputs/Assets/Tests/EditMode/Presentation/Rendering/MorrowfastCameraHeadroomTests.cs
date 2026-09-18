using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class MorrowfastCameraHeadroomTests
    {
        [TestCase(false)] [TestCase(true)]
        public void NorthArrivalFitsTheActualPlayerHeadAndWholeSourceWithoutAStepZoom(bool looking)
        {
            var factory = MorrowfastTestWorld.Factory();
            var zone = new OverworldZoneManager(factory, 64).GetZone(MorrowfastSceneRuntime.ZoneID);
            var player = factory.CreateEntity("Player"); Assert.IsTrue(zone.AddEntity(player, 40, 0));
            var root = new GameObject("Morrowfast headroom fixture");
            try
            {
                var art = root.AddComponent<MorrowfastScenePresenter>(); art.Bind(zone); Assert.IsTrue(art.IsReady, art.Failure);
                var cameraObject = new GameObject("Headroom camera"); cameraObject.transform.SetParent(root.transform);
                var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.aspect = 16f / 9;
                var follow = cameraObject.AddComponent<CameraFollow>();
                typeof(CameraFollow).GetField("_morrowfastScenePresenter", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(follow, art);
                follow.Player = player; follow.CurrentZone = zone;
                if (looking) follow.SetOverrideTargetCell(40, 12);
                follow.SnapToPlayer();
                Assert.IsTrue(EntityVisualCatalog.TryGetAsset(player, out var visual));
                var sprite = visual.GetFrame(EntityVisualState.Idle, EntityVisualFacing.South, 0);
                float actualPlayerHead = 24 + sprite.bounds.max.y;
                float top = camera.transform.position.y + camera.orthographicSize;
                float bottom = camera.transform.position.y - camera.orthographicSize;
                Assert.GreaterOrEqual(top + .0001f, actualPlayerHead, "The northern arrival must not clip the native player body.");
                Assert.LessOrEqual(bottom, 0.0001f, "Headroom must not crop the southern source pixels.");
                Assert.GreaterOrEqual(camera.transform.position.x + camera.orthographicSize * camera.aspect + .0001f, 58.75f);
                Assert.LessOrEqual(camera.transform.position.x - camera.orthographicSize * camera.aspect, 21.2501f);
                float size = camera.orthographicSize; Vector3 position = camera.transform.position;
                Assert.IsTrue(zone.MoveEntity(player, 40, 1)); follow.SnapToPlayer();
                Assert.AreEqual(size, camera.orthographicSize); Assert.AreEqual(position, camera.transform.position,
                    "A small constant safety frame avoids a visible zoom jump on the first walking step.");
                art.SetPresentationVisible(false); follow.SnapToPlayer(); Assert.AreEqual(17, camera.orthographicSize);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
