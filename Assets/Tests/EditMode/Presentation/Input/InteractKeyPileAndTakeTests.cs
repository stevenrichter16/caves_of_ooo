using System;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Two gaps reported from play, both in the interact-key ('c') path:
    ///
    /// <para>1. Pointing 'c' at a PILE of dropped items (e.g. a dead NPC's
    /// loot) resolved straight to whichever single item happened to render
    /// on top and opened THAT item's menu — Examine, maybe Throw — with no
    /// way to reach the rest of the pile. The look-mode cursor already had
    /// a "what's here" picker for exactly this case
    /// (<c>WorldInteractionSystem.IsPileCell</c> /
    /// <c>BuildTargetPickerActions</c>); 'c' just never routed through it.</para>
    ///
    /// <para>2. Even once an item's own menu was reachable, there was no
    /// way to pick it up from it — "Take" was never contributed to
    /// <c>GetInventoryActions</c> by anything. The only pickup path in the
    /// whole game was the standalone key (G / ,), which only ever reaches
    /// what the player is STANDING ON — not an adjacent tile, which is
    /// exactly what 'c' can reach that the standalone key cannot.</para>
    /// </summary>
    public class InteractKeyPileAndTakeTests
    {
        [SetUp]
        public void SetUp()
        {
            FactionManager.Initialize();
            MessageLog.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            FactionManager.Reset();
            foreach (var input in UnityEngine.Object.FindObjectsOfType<InputHandler>())
                UnityEngine.Object.DestroyImmediate(input.gameObject);
            foreach (var menu in UnityEngine.Object.FindObjectsOfType<WorldActionMenuUI>())
                UnityEngine.Object.DestroyImmediate(menu.gameObject);
        }

        private static Entity CreatePlayer(int strength = 16)
        {
            var player = new Entity { BlueprintName = "Player" };
            player.SetTag("Player");
            player.SetTag("Creature");
            player.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            player.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            player.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = strength, Min = 0, Max = 40 };
            player.AddPart(new InventoryPart());
            return player;
        }

        /// <summary>A light, loose item — Examinable (universal) + Takeable.</summary>
        private static Entity CreateLooseItem(string id, int weight = 2)
        {
            var item = new Entity { ID = id, BlueprintName = "Bone" };
            item.AddPart(new ExaminablePart());
            item.AddPart(new PhysicsPart { Takeable = true, Weight = weight });
            return item;
        }

        private static InputHandler BuildInputHandler(Entity player, Zone zone)
        {
            var inputGo = new GameObject("InputHandler");
            var input = inputGo.AddComponent<InputHandler>();
            input.PlayerEntity = player;
            input.CurrentZone = zone;

            var menuGo = new GameObject("WorldActionMenuUI");
            input.WorldActionMenuUI = menuGo.AddComponent<WorldActionMenuUI>();
            return input;
        }

        private static void SetReturnState(InputHandler input, string value)
        {
            Type enumType = typeof(InputHandler).GetNestedType("InputState", BindingFlags.NonPublic);
            object enumValue = Enum.Parse(enumType, value);
            typeof(InputHandler)
                .GetField("_worldActionMenuReturnState", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(input, enumValue);
        }

        private static string GetInputState(InputHandler input)
            => typeof(InputHandler)
                .GetField("_inputState", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(input).ToString();

        private static List<InventoryAction> GetMenuActions(WorldActionMenuUI menu)
            => (List<InventoryAction>)typeof(WorldActionMenuUI)
                .GetField("_actions", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(menu);

        private static object InvokeNonPublic(object instance, string methodName, params object[] args)
        {
            MethodInfo method = instance.GetType().GetMethod(
                methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            return method.Invoke(instance, args);
        }

        // ════════════════════════════════════════════════════════
        // The pile bug
        // ════════════════════════════════════════════════════════

        [Test]
        public void PileCell_ThroughTheInteractPath_OffersOneRowPerItem_NotASingleItemsMenu()
        {
            var zone = new Zone("PileZone");
            var player = CreatePlayer();
            zone.AddEntity(player, 10, 10);
            zone.AddEntity(CreateLooseItem("sword"), 11, 10);
            zone.AddEntity(CreateLooseItem("shield"), 11, 10);

            var input = BuildInputHandler(player, zone);

            // InteractInDirection is what 'c' actually calls once a
            // direction is resolved — driving it (rather than the
            // downstream OpenWorldActionMenu helper directly) is what makes
            // this test capable of catching a regression in the delegation
            // itself, not just in the helper it delegates to.
            InvokeNonPublic(input, "InteractInDirection", 1, 0);

            Assert.AreEqual("WorldActionMenuOpen", GetInputState(input));
            var actions = GetMenuActions(input.WorldActionMenuUI);
            Assert.AreEqual(2, actions.Count, "one picker row per pile item");
            foreach (var a in actions)
                StringAssert.StartsWith(WorldInteractionSystem.PickTargetCommandPrefix, a.Command,
                    "a pile must open the picker, not a single item's own menu");
        }

        [Test]
        public void SingleItemCell_ThroughTheInteractPath_OpensThatItemsOwnMenuDirectly()
        {
            // Counter-check: the pile picker must NOT engage for an
            // ordinary single-item cell — that would be a menu full of
            // useless "which of the one thing?" indirection.
            var zone = new Zone("SingleZone");
            var player = CreatePlayer();
            zone.AddEntity(player, 10, 10);
            zone.AddEntity(CreateLooseItem("sword"), 11, 10);

            var input = BuildInputHandler(player, zone);
            InvokeNonPublic(input, "InteractInDirection", 1, 0);

            Assert.AreEqual("WorldActionMenuOpen", GetInputState(input));
            var actions = GetMenuActions(input.WorldActionMenuUI);
            foreach (var a in actions)
                StringAssert.DoesNotStartWith(WorldInteractionSystem.PickTargetCommandPrefix, a.Command,
                    "a single item must not show picker indirection");
            CollectionAssert.Contains(actions.ConvertAll(a => a.Command), "Examine");
        }

        // ════════════════════════════════════════════════════════
        // The missing "Take" row
        // ════════════════════════════════════════════════════════

        [Test]
        public void ALooseTakeableItem_OffersTake()
        {
            var item = CreateLooseItem("bone");
            var actions = WorldInteractionSystem.GatherActions(item, CreatePlayer());
            CollectionAssert.Contains(actions.ConvertAll(a => a.Command), "Take");
        }

        [Test]
        public void AnAlreadyCarriedItem_DoesNotOfferTakeAgain()
        {
            var item = CreateLooseItem("bone");
            item.GetPart<PhysicsPart>().InInventory = CreatePlayer();
            var actions = WorldInteractionSystem.GatherActions(item, CreatePlayer());
            CollectionAssert.DoesNotContain(actions.ConvertAll(a => a.Command), "Take");
        }

        [Test]
        public void ANonTakeableObject_NeverOffersTake()
        {
            // Counter-check on the gate itself: Takeable=false (a wall, a
            // hedge, a haulable millstone) must never show Take, regardless
            // of what else is going on.
            var wall = new Entity { ID = "wall", BlueprintName = "Wall" };
            wall.AddPart(new PhysicsPart { Takeable = false, Solid = true });
            var actions = WorldInteractionSystem.GatherActions(wall, CreatePlayer());
            CollectionAssert.DoesNotContain(actions.ConvertAll(a => a.Command), "Take");
        }

        [Test]
        public void SelectingTake_ActuallyPicksTheItemUp_AndReturnsToTheOriginatingState()
        {
            var zone = new Zone("TakeZone");
            var player = CreatePlayer();
            zone.AddEntity(player, 10, 10);
            var item = CreateLooseItem("bone");
            zone.AddEntity(item, 11, 10);
            var cell = zone.GetCell(11, 10);

            var input = BuildInputHandler(player, zone);
            SetReturnState(input, "Normal");

            // ExecuteWorldActionSelection's Take branch ends the turn
            // (EndTurnAndProcess -> TurnManager.EndTurn), so this fixture
            // needs a real TurnManager — the same requirement
            // InputHandlerLookModeTests documents for any path that spends
            // a turn.
            var turnManager = new TurnManager();
            turnManager.AddEntity(player);
            turnManager.ProcessUntilPlayerTurn();
            input.TurnManager = turnManager;

            var actions = WorldInteractionSystem.GatherActions(item, player);
            var take = actions.Find(a => a.Command == "Take");
            Assert.IsNotNull(take, "precondition: the fixture must offer Take");

            InvokeNonPublic(input, "ExecuteWorldActionSelection", take, item, cell, false);

            var inventory = player.GetPart<InventoryPart>();
            CollectionAssert.Contains(inventory.Objects, item, "the item must actually be in the player's inventory");
            Assert.AreEqual("Normal", GetInputState(input),
                "Take must return to the state the interact key came from");
        }
    }
}
