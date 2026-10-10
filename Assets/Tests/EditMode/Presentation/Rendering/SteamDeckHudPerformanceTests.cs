using System;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public class SteamDeckHudPerformanceTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            PerformanceDiagnostics.ResetAll();
            MessageLog.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
                Object.DestroyImmediate(_objects[i]);
            _objects.Clear();
            PerformanceDiagnostics.ResetAll();
            MessageLog.Clear();
        }

        [Test]
        public void Hotbar_EqualSnapshots_DoNotClearOrRepaint()
        {
            var renderer = CreateRenderer(out var text, out var background, out var camera);
            renderer.Render(EmptySnapshot("first"), camera);
            var textPosition = new Vector3Int(1, GameplayHotbarLayout.GridHeight - 1, 0);
            var bgPosition = Vector3Int.zero;
            text.SetColor(textPosition, Color.magenta);
            background.SetColor(bgPosition, Color.green);

            BeginFrame();
            renderer.Render(EmptySnapshot("first"), camera);
            PerformanceDiagnostics.EndFrame(0);

            Assert.AreEqual(0, PerformanceDiagnostics.LastCompletedFrameSnapshot.TilemapClearCount);
            Assert.AreEqual(0, PerformanceDiagnostics.LastCompletedFrameSnapshot.HotbarRenderCount);
            Assert.AreEqual(Color.magenta, text.GetColor(textPosition), "Unchanged text must not be rewritten.");
            Assert.AreEqual(Color.green, background.GetColor(bgPosition), "Unchanged background must not be rewritten.");
        }

        [Test]
        public void Hotbar_ChangedText_RepaintsWithoutRebuildingBackground()
        {
            var renderer = CreateRenderer(out var text, out var background, out var camera);
            renderer.Render(EmptySnapshot("first"), camera);
            background.SetColor(Vector3Int.zero, Color.green);

            BeginFrame();
            renderer.Render(EmptySnapshot("x"), camera);
            PerformanceDiagnostics.EndFrame(0);

            Assert.AreEqual(1, PerformanceDiagnostics.LastCompletedFrameSnapshot.HotbarRenderCount);
            Assert.AreEqual(0, PerformanceDiagnostics.LastCompletedFrameSnapshot.TilemapClearCount);
            Assert.AreEqual(Color.green, background.GetColor(Vector3Int.zero));
            Assert.IsNotNull(text.GetTile(new Vector3Int(1, GameplayHotbarLayout.GridHeight - 2, 0)));
            Assert.IsNull(text.GetTile(new Vector3Int(2, GameplayHotbarLayout.GridHeight - 2, 0)), "Shorter summary erases previous trailing text.");
        }

        [Test]
        public void Hotbar_Clear_ThenSameSnapshot_RepaintsBothMaps()
        {
            var renderer = CreateRenderer(out var text, out var background, out var camera);
            var snapshot = EmptySnapshot("first");
            renderer.Render(snapshot, camera);
            renderer.Clear();
            Assert.AreEqual(0, text.GetUsedTilesCount());
            Assert.AreEqual(0, background.GetUsedTilesCount());
            BeginFrame();
            renderer.Render(snapshot, camera);
            PerformanceDiagnostics.EndFrame(0);
            Assert.AreEqual(1, PerformanceDiagnostics.LastCompletedFrameSnapshot.HotbarRenderCount);
            Assert.Greater(text.GetUsedTilesCount(), 0);
            Assert.Greater(background.GetUsedTilesCount(), 0);
        }

        [Test]
        public void Hotbar_DisabledCamera_ClearsOnce_ThenRecovers()
        {
            var renderer = CreateRenderer(out var text, out var background, out var camera);
            var snapshot = EmptySnapshot("first");
            renderer.Render(snapshot, camera);
            camera.enabled = false;
            renderer.Render(snapshot, camera);
            Assert.AreEqual(0, text.GetUsedTilesCount());
            Assert.AreEqual(0, background.GetUsedTilesCount());
            BeginFrame();
            renderer.Render(snapshot, camera);
            PerformanceDiagnostics.EndFrame(0);
            Assert.AreEqual(0, PerformanceDiagnostics.LastCompletedFrameSnapshot.TilemapClearCount);
            camera.enabled = true;
            renderer.Render(snapshot, camera);
            Assert.Greater(background.GetUsedTilesCount(), 0);
        }

        [Test]
        public void HotbarBuilder_UnchangedState_ReusesSnapshot_ButCooldownInvalidates()
        {
            var player = new Entity();
            var abilities = new ActivatedAbilitiesPart();
            player.AddPart(abilities);
            var id = abilities.AddAbility("Ice Lance", "CommandIce", "Spell");
            var first = HotbarStateBuilder.Build(player, 0, null);
            var same = HotbarStateBuilder.Build(player, 0, null);
            Assert.AreSame(first, same, "Idle frames should reuse the immutable snapshot.");
            abilities.GetAbility(id).CooldownRemaining = 3;
            var changed = HotbarStateBuilder.Build(player, 0, null);
            Assert.AreNotSame(first, changed);
            Assert.AreEqual(3, changed.Slots[0].CooldownRemaining);
            Assert.AreEqual(0, first.Slots[0].CooldownRemaining, "Retained snapshots stay stable.");
        }

        [Test]
        public void Sidebar_DoesNotGatherInventoryActions_ButInventoryScreenDoes()
        {
            var player = new Entity();
            var inventory = new InventoryPart();
            player.AddPart(inventory);
            var item = new Entity { BlueprintName = "HUD Test Item" };
            item.AddPart(new PhysicsPart { Weight = 7 });
            var counter = new ActionCountingPart();
            item.AddPart(counter);
            inventory.AddObject(item);
            counter.Count = 0;

            var sidebar = SidebarStateBuilder.Build(player, null, null);
            Assert.AreEqual(0, counter.Count, "Sidebar must not construct full inventory item/action data.");
            StringAssert.StartsWith("WT 7/", sidebar.VitalLines[3]);
            InventoryScreenData.Build(player);
            Assert.Greater(counter.Count, 0, "The same item must exercise action gathering in the full inventory path.");
        }

        [Test]
        public void Sidebar_DirectVitals_RefreshMutableStatsWeightAndDefense()
        {
            var player = new Entity();
            var inventory = new InventoryPart();
            player.AddPart(inventory);
            player.Statistics["Hitpoints"] = new Stat { Value = 9, Max = 10 };
            player.Statistics["MP"] = new Stat { Value = 3 };
            player.Statistics["Strength"] = new Stat { Value = 10 };
            var armor = new ArmorPart { AV = 2, DV = -1 };
            player.AddPart(armor);
            var item = new Entity();
            var physics = new PhysicsPart { Weight = 7 };
            item.AddPart(physics);
            inventory.AddObject(item);
            var first = SidebarStateBuilder.Build(player, null, null);
            Assert.AreEqual("HP 9/10 | MP 3", first.VitalLines[0]);
            Assert.AreEqual("AV 2 | DV 5", first.VitalLines[2]);

            player.Statistics["Hitpoints"].Value = 4;
            player.Statistics["MP"].Value = 1;
            physics.Weight = 11;
            armor.AV = 6;
            var changed = SidebarStateBuilder.Build(player, null, null);
            Assert.AreEqual("HP 4/10 | MP 1", changed.VitalLines[0]);
            Assert.AreEqual("AV 6 | DV 5", changed.VitalLines[2]);
            StringAssert.StartsWith("WT 11/150", changed.VitalLines[3]);
        }

        private GameplayHotbarRenderer CreateRenderer(out Tilemap text, out Tilemap background, out Camera camera)
        {
            var grid = CreateObject("HUD performance grid");
            grid.AddComponent<Grid>();
            var textObject = CreateObject("HUD text");
            textObject.transform.SetParent(grid.transform);
            text = textObject.AddComponent<Tilemap>();
            var backgroundObject = CreateObject("HUD background");
            backgroundObject.transform.SetParent(grid.transform);
            background = backgroundObject.AddComponent<Tilemap>();
            camera = CreateObject("HUD camera").AddComponent<Camera>();
            return new GameplayHotbarRenderer(text, background);
        }

        private GameObject CreateObject(string name)
        {
            var value = new GameObject(name);
            _objects.Add(value);
            return value;
        }

        private static HotbarSnapshot EmptySnapshot(string summary) =>
            new HotbarSnapshot("GRIMOIRES", summary, "hint", Array.Empty<HotbarSlotSnapshot>(), -1, -1);

        private static void BeginFrame() => PerformanceDiagnostics.BeginFrame(0, false, false);

        private sealed class ActionCountingPart : Part
        {
            public int Count;
            public override string Name => "HUDActionCounter";
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "GetInventoryActions") Count++;
                return true;
            }
        }
    }
}
