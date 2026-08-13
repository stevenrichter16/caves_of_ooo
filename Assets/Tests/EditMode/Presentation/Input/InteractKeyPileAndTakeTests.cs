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

        private static List<Entity> GetPickupItems(PickupUI pickup)
            => (List<Entity>)typeof(PickupUI)
                .GetField("_items", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(pickup);

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
        public void PileCell_ThroughTheInteractPath_OffersTakeItemsAndExamine()
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
            var commands = GetMenuActions(input.WorldActionMenuUI).ConvertAll(a => a.Command);

            CollectionAssert.Contains(commands, WorldInteractionSystem.ViewPileCommand,
                "a loot pile's whole point is taking from it");
            CollectionAssert.Contains(commands, "Examine");
            CollectionAssert.Contains(commands, WorldInteractionSystem.PickCellCommand,
                "the per-object picker must still be reachable for non-loot");

            foreach (var c in commands)
                StringAssert.DoesNotStartWith(WorldInteractionSystem.PickTargetCommandPrefix, c,
                    "the pile now opens a SUMMARY, not the raw one-row-per-object picker");
        }

        [Test]
        public void PileSummary_LeadsWithTakeItems_AndCountsThem()
        {
            var zone = new Zone("PileZone");
            var player = CreatePlayer();
            zone.AddEntity(player, 10, 10);
            zone.AddEntity(CreateLooseItem("sword"), 11, 10);
            zone.AddEntity(CreateLooseItem("shield"), 11, 10);
            zone.AddEntity(CreateLooseItem("helm"), 11, 10);

            var rows = WorldInteractionSystem.BuildPileSummaryActions(zone.GetCell(11, 10), player);

            Assert.AreEqual(WorldInteractionSystem.ViewPileCommand, rows[0].Command,
                "taking is the first thing offered");
            StringAssert.Contains("3", rows[0].Display, "the row states how many items are there");
            Assert.AreEqual(WorldInteractionSystem.PickCellCommand, rows[rows.Count - 1].Command,
                "'everything here' sorts last");
        }

        [Test]
        public void APileOfScenery_OffersNoTakeRow()
        {
            // Counter-check on the take row's gate: two non-takeable things
            // sharing a cell is still a "pile" by IsPileCell's count, but
            // offering an empty pickup list would be a dead end.
            var zone = new Zone("SceneryZone");
            var player = CreatePlayer();
            zone.AddEntity(player, 10, 10);
            foreach (var id in new[] { "rockA", "rockB" })
            {
                var rock = new Entity { ID = id, BlueprintName = "Rock" };
                rock.AddPart(new ExaminablePart());
                rock.AddPart(new PhysicsPart { Takeable = false, Solid = true });
                zone.AddEntity(rock, 11, 10);
            }

            var rows = WorldInteractionSystem.BuildPileSummaryActions(zone.GetCell(11, 10), player);
            CollectionAssert.DoesNotContain(rows.ConvertAll(a => a.Command),
                WorldInteractionSystem.ViewPileCommand);
            CollectionAssert.Contains(rows.ConvertAll(a => a.Command), "Examine");
        }

        // ════════════════════════════════════════════════════════
        // "take items" → the persistent list
        // ════════════════════════════════════════════════════════

        [Test]
        public void ChoosingTakeItems_OpensThePickupList()
        {
            var zone = new Zone("PileZone");
            var player = CreatePlayer();
            zone.AddEntity(player, 10, 10);
            zone.AddEntity(CreateLooseItem("sword"), 11, 10);
            zone.AddEntity(CreateLooseItem("shield"), 11, 10);
            var cell = zone.GetCell(11, 10);

            var input = BuildInputHandler(player, zone);
            var pickupGo = new GameObject("PickupUI");
            input.PickupUI = pickupGo.AddComponent<PickupUI>();

            var viewPile = new InventoryAction("ViewPile", "take items (2)",
                WorldInteractionSystem.ViewPileCommand, 'g', 40);
            InvokeNonPublic(input, "ExecuteWorldActionSelection",
                viewPile, WorldInteractionSystem.ResolveTarget(cell), cell, true);

            Assert.AreEqual("PickupOpen", GetInputState(input));
            Assert.IsTrue(input.PickupUI.IsOpen, "the pickup list must be showing");
        }

        [Test]
        public void TakingOneItemFromTheList_LeavesTheListOpenWithTheRest()
        {
            // The actual reported complaint: the list must NOT close after
            // each pickup. PickupUI already implements this loop; this pins
            // it so the pile flow's reliance on it cannot silently break.
            var zone = new Zone("PileZone");
            var player = CreatePlayer();
            zone.AddEntity(player, 10, 10);
            var sword = CreateLooseItem("sword");
            var shield = CreateLooseItem("shield");
            zone.AddEntity(sword, 11, 10);
            zone.AddEntity(shield, 11, 10);

            var pickupGo = new GameObject("PickupUI");
            var pickup = pickupGo.AddComponent<PickupUI>();
            pickup.PlayerEntity = player;
            pickup.CurrentZone = zone;
            pickup.Open(new List<Entity> { sword, shield });

            InvokeNonPublic(pickup, "PickupItem", 0);

            Assert.IsTrue(pickup.IsOpen, "the list stays open after taking one item");
            Assert.AreEqual(1, GetPickupItems(pickup).Count, "the taken item leaves the list");
            Assert.IsTrue(pickup.PickedUpAny);

            UnityEngine.Object.DestroyImmediate(pickupGo);
        }

        [Test]
        public void TakingTheLastItem_ClosesTheList()
        {
            // Counter-check to the above — "stays open" must not mean
            // "never closes", or the player is stuck on an empty list.
            var zone = new Zone("PileZone");
            var player = CreatePlayer();
            zone.AddEntity(player, 10, 10);
            var sword = CreateLooseItem("sword");
            zone.AddEntity(sword, 11, 10);

            var pickupGo = new GameObject("PickupUI");
            var pickup = pickupGo.AddComponent<PickupUI>();
            pickup.PlayerEntity = player;
            pickup.CurrentZone = zone;
            pickup.Open(new List<Entity> { sword });

            InvokeNonPublic(pickup, "PickupItem", 0);

            Assert.IsFalse(pickup.IsOpen, "an emptied list closes itself");

            UnityEngine.Object.DestroyImmediate(pickupGo);
        }

        [Test]
        public void EverythingHere_FromTheSummary_OpensThePerObjectPicker()
        {
            // The "<< everything here" row must reach the per-object picker.
            // It previously routed back through OpenWorldActionMenu, which
            // on a pile cell now opens the SUMMARY — i.e. the very menu the
            // row was selected from, making it a no-op.
            var zone = new Zone("PileZone");
            var player = CreatePlayer();
            zone.AddEntity(player, 10, 10);
            zone.AddEntity(CreateLooseItem("sword"), 11, 10);
            zone.AddEntity(CreateLooseItem("shield"), 11, 10);
            var cell = zone.GetCell(11, 10);

            var input = BuildInputHandler(player, zone);
            var back = new InventoryAction("PickCell", "<< everything here",
                WorldInteractionSystem.PickCellCommand, '\0', -1);
            InvokeNonPublic(input, "ExecuteWorldActionSelection",
                back, WorldInteractionSystem.ResolveTarget(cell), cell, true);

            Assert.AreEqual("WorldActionMenuOpen", GetInputState(input));
            var commands = GetMenuActions(input.WorldActionMenuUI).ConvertAll(a => a.Command);
            Assert.AreEqual(2, commands.Count, "one row per object in the cell");
            foreach (var c in commands)
                StringAssert.StartsWith(WorldInteractionSystem.PickTargetCommandPrefix, c,
                    "'everything here' must open the per-object picker");
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
