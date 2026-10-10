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
    /// <summary>Cache/lifecycle boundaries: direct public-state mutation, retained
    /// snapshots, mutable caller collections, sparse bindings, and null players.</summary>
    public class SteamDeckHudAdversarialTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--) Object.DestroyImmediate(_objects[i]);
            _objects.Clear();
            PerformanceDiagnostics.ResetAll();
            MessageLog.Clear();
        }

        [TestCase("name")]
        [TestCase("source")]
        [TestCase("cooldown")]
        [TestCase("binding")]
        [TestCase("remove")]
        [TestCase("selected")]
        [TestCase("pending")]
        public void Adversarial_HotbarMutation_UpdatesImmediatelyAndPreservesOldSnapshot(string mutation)
        {
            var player = CreatePlayer(out var abilities, out var ability);
            var before = HotbarStateBuilder.Build(player, 0, null);
            int selected = 0;
            ActivatedAbility pending = null;
            switch (mutation)
            {
                case "name": ability.DisplayName = "Storm Spear"; break;
                case "source": ability.SourcePowerClass = "Pyromancy_Kindle"; break;
                case "cooldown": ability.CooldownRemaining = 4; break;
                case "binding": abilities.SlotAssignments[9] = ability.ID; abilities.SlotAssignments[0] = Guid.Empty; break;
                case "remove": abilities.RemoveAbility(ability.ID); break;
                case "selected": selected = 9; break;
                case "pending": pending = ability; break;
            }
            var after = HotbarStateBuilder.Build(player, selected, pending);
            Assert.AreNotSame(before, after);
            Assert.AreEqual("Ice Lance", before.Slots[0].DisplayName);
            Assert.AreEqual(0, before.Slots[0].CooldownRemaining);
            Assert.AreEqual(0, before.SelectedSlot);
            Assert.AreEqual(-1, before.PendingSlot);
            switch (mutation)
            {
                case "name": Assert.AreEqual("Storm Spear", after.Slots[0].DisplayName); Assert.AreEqual("SS", after.Slots[0].ShortName); break;
                case "source": Assert.AreEqual("Kindle", after.Slots[0].DisplayName); Assert.AreEqual("&R", after.Slots[0].AccentColorCode); break;
                case "cooldown": Assert.AreEqual(4, after.Slots[0].CooldownRemaining); Assert.IsFalse(after.Slots[0].Usable); break;
                case "binding": Assert.IsFalse(after.Slots[0].Occupied); Assert.IsTrue(after.Slots[9].Occupied); Assert.AreEqual('0', after.Slots[9].Hotkey); break;
                case "remove": Assert.IsFalse(after.Slots[0].Occupied); break;
                case "selected": Assert.IsTrue(after.Slots[9].Selected); Assert.IsFalse(after.Slots[0].Selected); break;
                case "pending": Assert.IsTrue(after.Slots[0].Pending); StringAssert.Contains("choose a direction", after.SummaryText); break;
            }
            Assert.AreSame(after, HotbarStateBuilder.Build(player, selected, pending));
        }

        [TestCase(null, "", '?')]
        [TestCase("", "", '?')]
        [TestCase("   ", "", '?')]
        [TestCase("---", "---", '?')]
        [TestCase("a  b", "AB", 'A')]
        [TestCase("LongSingleWord", "LongSi", 'L')]
        public void Adversarial_HotbarNames_MalformedOrCompactInputIsStable(string name, string shortName, char glyph)
        {
            var player = CreatePlayer(out _, out var ability);
            ability.DisplayName = name;
            var first = HotbarStateBuilder.Build(player, 0, null);
            Assert.AreEqual(shortName, first.Slots[0].ShortName);
            Assert.AreEqual(glyph, first.Slots[0].Glyph);
            Assert.AreSame(first, HotbarStateBuilder.Build(player, 0, null));
        }

        [Test]
        public void Adversarial_PlayerReplacement_DoesNotReuseStaleAbilityOrKeepItForNullPlayer()
        {
            var first = CreatePlayer(out _, out _);
            HotbarStateBuilder.Build(first, 0, null);
            var second = CreatePlayer(out _, out var ability);
            ability.DisplayName = "Other Power";
            var replaced = HotbarStateBuilder.Build(second, 0, null);
            Assert.AreEqual("Other Power", replaced.Slots[0].DisplayName);
            var cleared = HotbarStateBuilder.Build(null, 0, null);
            Assert.IsFalse(cleared.Slots[0].Occupied);
            Assert.IsFalse(HotbarStateBuilder.Build(new Entity(), 0, null).Slots[0].Occupied);
        }

        [Test]
        public void Adversarial_PendingUnboundAbility_DoesNotMarkAnySlotPending()
        {
            var player = CreatePlayer(out var abilities, out var ability);
            var before = HotbarStateBuilder.Build(player, 0, ability);
            Assert.AreEqual(0, before.PendingSlot);
            abilities.AssignAbilityToSlot(Guid.Empty, 0);
            var after = HotbarStateBuilder.Build(player, 0, ability);
            Assert.AreEqual(-1, after.PendingSlot);
            for (int i = 0; i < after.Slots.Count; i++) Assert.IsFalse(after.Slots[i].Pending);
        }

        [Test]
        public void Adversarial_UnselectedCooldown_StillInvalidatesItsSlot()
        {
            var player = CreatePlayer(out var abilities, out _);
            var id = abilities.AddAbility("Other", "CommandOther", "Spell");
            var before = HotbarStateBuilder.Build(player, 0, null);
            abilities.GetAbility(id).CooldownRemaining = 2;
            var after = HotbarStateBuilder.Build(player, 0, null);
            Assert.AreEqual(before.SummaryText, after.SummaryText);
            Assert.AreEqual(2, after.Slots[1].CooldownRemaining);
            Assert.AreEqual(0, before.Slots[1].CooldownRemaining);
        }

        [Test]
        public void Adversarial_Renderer_MutableSlotListCannotHideChange()
        {
            var renderer = CreateRenderer(out var text, out _, out var camera);
            var slots = new[] { Slot("OLD", 'O') };
            var snapshot = new HotbarSnapshot("title", "summary", "hint", slots, 0, -1);
            renderer.Render(snapshot, camera);
            slots[0] = Slot("NEW", 'N');
            renderer.Render(snapshot, camera);
            Assert.AreEqual(CP437TilesetGenerator.GetTextTile('N'), text.GetTile(new Vector3Int(1, 1, 0)));
        }

        [Test]
        public void Adversarial_Renderer_SlotChangeDoesNotRepaintOtherSlot()
        {
            var renderer = CreateRenderer(out var text, out _, out var camera);
            var player = CreatePlayer(out var abilities, out _);
            abilities.AddAbility("Other", "CommandOther", "Spell");
            renderer.Render(HotbarStateBuilder.Build(player, 0, null), camera);
            var untouchedPosition = new Vector3Int(GameplayHotbarLayout.SlotWidth, 0, 0);
            text.SetColor(untouchedPosition, Color.magenta);
            abilities.GetAbilityBySlot(0).CooldownRemaining = 2;
            renderer.Render(HotbarStateBuilder.Build(player, 0, null), camera);
            Assert.AreEqual(Color.magenta, text.GetColor(untouchedPosition));
        }

        [Test]
        public void Adversarial_Renderer_NullCameraClears_AndNewCameraRecovers()
        {
            var renderer = CreateRenderer(out var text, out var background, out var camera);
            var snapshot = new HotbarSnapshot("title", "summary", "hint", new[] { Slot("OLD", 'O') }, 0, -1);
            renderer.Render(snapshot, camera);
            renderer.Render(snapshot, null);
            Assert.AreEqual(0, text.GetUsedTilesCount());
            Assert.AreEqual(0, background.GetUsedTilesCount());
            renderer.Render(snapshot, camera);
            Assert.Greater(text.GetUsedTilesCount(), 0);
            Assert.Greater(background.GetUsedTilesCount(), 0);
        }

        [Test]
        public void Adversarial_SidebarMissingInventory_PreservesEmptyInventoryFallbacks()
        {
            var player = new Entity();
            player.Statistics["Hitpoints"] = new Stat { Value = 9, Max = 10 };
            player.Statistics["MP"] = new Stat { Value = 3 };
            var snapshot = SidebarStateBuilder.Build(player, null, null);
            Assert.AreEqual("HP 0 | MP -", snapshot.VitalLines[0]);
            Assert.AreEqual("AV 0 | DV 0", snapshot.VitalLines[2]);
            Assert.AreEqual("WT 0/0 | DR 0", snapshot.VitalLines[3]);
            player.AddPart(new InventoryPart());
            var withInventory = SidebarStateBuilder.Build(player, null, null);
            Assert.AreEqual("HP 9/10 | MP 3", withInventory.VitalLines[0]);
            Assert.AreEqual("AV 0 | DV 6", withInventory.VitalLines[2]);
        }

        [TestCase(0, "0/0")]
        [TestCase(-1, "-1/-1")]
        [TestCase(12, "9/12")]
        public void Adversarial_SidebarHitpoints_UsesPositiveMaxOrCurrentValue(int maximum, string expected)
        {
            var player = new Entity();
            player.AddPart(new InventoryPart());
            player.Statistics["Hitpoints"] = new Stat { Value = 9, Max = maximum };
            var snapshot = SidebarStateBuilder.Build(player, null, null);
            Assert.AreEqual("HP " + expected + " | MP -", snapshot.VitalLines[0]);
        }

        private static Entity CreatePlayer(out ActivatedAbilitiesPart abilities, out ActivatedAbility ability)
        {
            var player = new Entity();
            abilities = new ActivatedAbilitiesPart();
            player.AddPart(abilities);
            ability = abilities.GetAbility(abilities.AddAbility("Ice Lance", "CommandIce", "Spell"));
            return player;
        }

        private static HotbarSlotSnapshot Slot(string shortName, char glyph) =>
            new HotbarSlotSnapshot(0, '1', shortName, shortName, "&W", "ready", glyph, 0, true, true, false, true);

        private GameplayHotbarRenderer CreateRenderer(out Tilemap text, out Tilemap background, out Camera camera)
        {
            var grid = CreateObject("HUD adversarial grid");
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
    }
}
