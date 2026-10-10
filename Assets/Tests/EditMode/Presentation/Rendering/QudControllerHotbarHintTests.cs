using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class QudControllerHotbarHintTests
    {
        readonly InputTestFixture input = new InputTestFixture();
        readonly List<GameObject> objects = new List<GameObject>();
        const string NativeHint = "[D-pad L/R] cycle  [X] cast";
        const string TargetHint = "[LS] direction  [A/RT] confirm  [B] cancel";

        [SetUp] public void SetUp()
        {
            input.Setup();
            PerformanceDiagnostics.ResetAll();
        }

        [TearDown] public void TearDown()
        {
            for (int i = objects.Count - 1; i >= 0; i--) Object.DestroyImmediate(objects[i]);
            objects.Clear();
            input.TearDown();
            PerformanceDiagnostics.ResetAll();
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void RealHotbarShowsControlsForDeviceAndPendingAbility(bool connected, bool pending)
        {
            if (connected) InputSystem.AddDevice<Gamepad>().MakeCurrent();
            InputSystem.Update();
            var snapshot = Snapshot(pending);
            string originalHint = snapshot.HintText;
            Assert.AreEqual(pending ? 0 : -1, snapshot.PendingSlot);
            var renderer = CreateRenderer(out var text, out _, out var camera);
            renderer.Render(snapshot, camera);
            string expected = connected ? (pending ? TargetHint : NativeHint) : originalHint;
            AssertRenderedHint(renderer, text, expected);
            Assert.AreEqual(originalHint, snapshot.HintText, "Device hints must not mutate retained gameplay snapshots.");
            if (connected) StringAssert.DoesNotContain("[Enter]", ReadHint(renderer));
            else StringAssert.Contains("[Enter]", ReadHint(renderer));
        }

        [Test]
        public void DeviceChangeRepaintsSameSnapshotHeaderOnlyAndEqualFrameDoesNoWork()
        {
            var snapshot = Snapshot(false);
            var renderer = CreateRenderer(out var text, out var background, out var camera);
            renderer.Render(snapshot, camera);
            AssertRenderedHint(renderer, text, snapshot.HintText);
            background.SetColor(Vector3Int.zero, Color.green);

            var pad = InputSystem.AddDevice<Gamepad>(); pad.MakeCurrent(); InputSystem.Update();
            Frame(renderer, snapshot, camera);
            AssertRenderedHint(renderer, text, NativeHint);
            Assert.AreEqual(1, PerformanceDiagnostics.LastCompletedFrameSnapshot.HotbarRenderCount);
            Assert.AreEqual(0, PerformanceDiagnostics.LastCompletedFrameSnapshot.TilemapClearCount);
            Assert.AreEqual(Color.green, background.GetColor(Vector3Int.zero));

            Frame(renderer, snapshot, camera);
            Assert.AreEqual(0, PerformanceDiagnostics.LastCompletedFrameSnapshot.HotbarRenderCount);
            Assert.AreEqual(0, PerformanceDiagnostics.LastCompletedFrameSnapshot.TilemapClearCount);

            InputSystem.RemoveDevice(pad); InputSystem.Update();
            Frame(renderer, snapshot, camera);
            AssertRenderedHint(renderer, text, snapshot.HintText);
            Assert.AreEqual(1, PerformanceDiagnostics.LastCompletedFrameSnapshot.HotbarRenderCount);
            Assert.AreEqual(0, PerformanceDiagnostics.LastCompletedFrameSnapshot.TilemapClearCount);
            Assert.AreEqual(Color.green, background.GetColor(Vector3Int.zero));
            int oldStart = GameplayHotbarLayout.GridWidth - 1 - NativeHint.Length;
            Assert.IsNull(text.GetTile(new Vector3Int(oldStart, GameplayHotbarLayout.GridHeight - 1, 0)),
                "The shorter keyboard hint must erase the old controller prefix.");
        }

        static HotbarSnapshot Snapshot(bool pending)
        {
            var player = new Entity();
            var abilities = new ActivatedAbilitiesPart(); player.AddPart(abilities);
            var id = abilities.AddAbility("Kindle Flame", "CommandKindle", "Spell", AbilityTargetingMode.AdjacentCell, 5, "Pyromancy_Kindle");
            return HotbarStateBuilder.Build(player, 0, pending ? abilities.GetAbility(id) : null);
        }

        static void Frame(GameplayHotbarRenderer renderer, HotbarSnapshot snapshot, Camera camera)
        {
            PerformanceDiagnostics.BeginFrame(0, false, false);
            renderer.Render(snapshot, camera);
            PerformanceDiagnostics.EndFrame(0);
        }

        static string ReadHint(GameplayHotbarRenderer renderer) => (string)typeof(GameplayHotbarRenderer)
            .GetField("_lastHint", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(renderer);

        static void AssertRenderedHint(GameplayHotbarRenderer renderer, Tilemap text, string expected)
        {
            Assert.AreEqual(expected, ReadHint(renderer));
            int start = GameplayHotbarLayout.GridWidth - 1 - expected.Length;
            for (int i = 0; i < expected.Length; i++)
            {
                var actual = text.GetTile(new Vector3Int(start + i, GameplayHotbarLayout.GridHeight - 1, 0));
                if (expected[i] == ' ') Assert.IsNull(actual);
                else
                {
                    var glyph = CP437TilesetGenerator.GetTextTile(expected[i]);
                    Assert.NotNull(glyph, "Expected text glyph exists.");
                    Assert.AreSame(glyph, actual, "Displayed hotbar glyph at character " + i);
                }
            }
        }

        GameplayHotbarRenderer CreateRenderer(out Tilemap text, out Tilemap background, out Camera camera)
        {
            var grid = Create("Controller hint grid"); grid.AddComponent<Grid>();
            var textObject = Create("Controller hint text"); textObject.transform.SetParent(grid.transform);
            text = textObject.AddComponent<Tilemap>();
            var backgroundObject = Create("Controller hint background"); backgroundObject.transform.SetParent(grid.transform);
            background = backgroundObject.AddComponent<Tilemap>();
            camera = Create("Controller hint camera").AddComponent<Camera>();
            return new GameplayHotbarRenderer(text, background);
        }
        GameObject Create(string name)
        {
            var result = new GameObject(name); objects.Add(result); return result;
        }
    }
}
