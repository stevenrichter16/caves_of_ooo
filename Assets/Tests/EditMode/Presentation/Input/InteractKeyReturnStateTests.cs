using System;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The world-action-menu chain has two true entry points that need to
    /// land in DIFFERENT places once the chain closes: the look-mode cursor
    /// (Enter/click) came from an active <c>LookMode</c> and should return
    /// to it; the 'c' interact key never activates a world cursor at all
    /// and must return to <c>Normal</c>.
    ///
    /// <para>Before <c>_worldActionMenuReturnState</c> existed, every close
    /// path in the chain hardcoded <c>LookMode</c>. For the cursor that was
    /// correct; for 'c' it silently spent an extra Escape press putting the
    /// player in a "LookMode" the game had never really entered — the
    /// cursor was never activated, so nothing was visibly different, but
    /// movement stayed locked until a second Escape ran <c>ExitLookMode</c>.
    /// This file pins that the two entry points land in the state they
    /// actually came from.</para>
    /// </summary>
    public class InteractKeyReturnStateTests
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

        private static Entity CreatePlayer()
        {
            var player = new Entity { BlueprintName = "Player" };
            player.SetTag("Player");
            player.SetTag("Creature");
            player.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 20, Min = 0, Max = 20 };
            player.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };
            return player;
        }

        /// <summary>An object with exactly one action (Examine) — enough to
        /// open the real menu rather than take the empty-cell shortcut.</summary>
        private static Entity CreateExaminableTarget()
        {
            var target = new Entity { BlueprintName = "Barrel" };
            target.AddPart(new ExaminablePart());
            return target;
        }

        /// <summary>An object with no interactive parts at all — GatherActions
        /// returns an empty list, exercising the empty-cell branch.</summary>
        private static Entity CreateActionlessTarget()
            => new Entity { BlueprintName = "Nothing" };

        private static (InputHandler input, WorldActionMenuUI menu, Zone zone, Entity player, Cell targetCell)
            BuildScene(Entity target)
        {
            var zone = new Zone("InteractZone");
            var player = CreatePlayer();
            zone.AddEntity(player, 10, 10);
            zone.AddEntity(target, 11, 10);

            var inputGo = new GameObject("InputHandler");
            var input = inputGo.AddComponent<InputHandler>();
            input.PlayerEntity = player;
            input.CurrentZone = zone;

            var menuGo = new GameObject("WorldActionMenuUI");
            var menu = menuGo.AddComponent<WorldActionMenuUI>();
            input.WorldActionMenuUI = menu;
            // Tilemap left null — WorldActionMenuUI.Render() no-ops without
            // one, so Open() is safe to call with no scene rendering set up.

            return (input, menu, zone, player, zone.GetCell(11, 10));
        }

        private static void SetReturnState(InputHandler input, string value)
        {
            Type enumType = typeof(InputHandler).GetNestedType("InputState", BindingFlags.NonPublic);
            object enumValue = Enum.Parse(enumType, value);
            FieldInfo field = typeof(InputHandler).GetField(
                "_worldActionMenuReturnState", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(input, enumValue);
        }

        private static string GetInputState(InputHandler input)
        {
            FieldInfo field = typeof(InputHandler).GetField(
                "_inputState", BindingFlags.Instance | BindingFlags.NonPublic);
            return field.GetValue(input).ToString();
        }

        private static void SetSelectionCancelled(WorldActionMenuUI menu, bool value)
        {
            FieldInfo field = typeof(WorldActionMenuUI).GetField(
                "_selectionCancelled", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(menu, value);
        }

        private static object InvokeNonPublic(object instance, string methodName, params object[] args)
        {
            MethodInfo method = instance.GetType().GetMethod(
                methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            return method.Invoke(instance, args);
        }

        // ════════════════════════════════════════════════════════
        // The bug, pinned
        // ════════════════════════════════════════════════════════

        [Test]
        public void OpenedViaInteractKey_CancellingTheMenu_ReturnsToNormal_NotLookMode()
        {
            var (input, menu, zone, player, cell) = BuildScene(CreateExaminableTarget());
            var target = WorldInteractionSystem.ResolveTarget(cell);

            // What HandleAwaitingTalkDirection does: the interact key never
            // activates a world cursor, so it marks Normal as home.
            SetReturnState(input, "Normal");
            InvokeNonPublic(input, "OpenWorldActionMenuFor", target, cell, true);
            Assert.AreEqual("WorldActionMenuOpen", GetInputState(input),
                "precondition: the menu must actually be open");

            SetSelectionCancelled(menu, true);
            InvokeNonPublic(input, "HandleWorldActionMenuInput");

            Assert.AreEqual("Normal", GetInputState(input),
                "cancelling a menu opened by 'c' must land in Normal — "
                + "landing in LookMode costs the player an extra Escape "
                + "before they can move");
        }

        [Test]
        public void OpenedViaLookModeCursor_CancellingTheMenu_ReturnsToLookMode()
        {
            // The counter-check: the fix must not have broken the cursor's
            // own path, which SHOULD return to LookMode.
            var (input, menu, zone, player, cell) = BuildScene(CreateExaminableTarget());
            var target = WorldInteractionSystem.ResolveTarget(cell);

            SetReturnState(input, "LookMode");
            InvokeNonPublic(input, "OpenWorldActionMenuFor", target, cell, true);
            Assert.AreEqual("WorldActionMenuOpen", GetInputState(input));

            SetSelectionCancelled(menu, true);
            InvokeNonPublic(input, "HandleWorldActionMenuInput");

            Assert.AreEqual("LookMode", GetInputState(input),
                "the cursor's own path must still return to LookMode");
        }

        // ════════════════════════════════════════════════════════
        // The same chain, other exits
        // ════════════════════════════════════════════════════════

        [Test]
        public void OpenedViaInteractKey_RunningAnOrdinaryAction_ReturnsToNormal()
        {
            // ExecuteWorldActionSelection's own final fallthrough (Examine,
            // Take, Haul, ...) had the identical hardcoded-LookMode bug.
            var (input, menu, zone, player, cell) = BuildScene(CreateExaminableTarget());
            var target = WorldInteractionSystem.ResolveTarget(cell);

            SetReturnState(input, "Normal");
            var actions = WorldInteractionSystem.GatherActions(target, player);
            var examine = actions.Find(a => a.Command == "Examine");
            Assert.IsNotNull(examine, "precondition: the fixture must offer Examine");

            InvokeNonPublic(input, "ExecuteWorldActionSelection", examine, target, cell, false);

            Assert.AreEqual("Normal", GetInputState(input),
                "running Examine from the interact-key menu must return to Normal");
        }

        [Test]
        public void OpenedViaInteractKey_TargetWithNoActions_ReturnsToNormalImmediately()
        {
            // The empty-cell branch inside OpenWorldActionMenuFor never
            // opens the menu at all, so it has its own copy of the same
            // hardcoded-return bug.
            var (input, menu, zone, player, cell) = BuildScene(CreateActionlessTarget());
            var target = WorldInteractionSystem.ResolveTarget(cell);
            Assert.IsNotNull(target, "precondition: the cell resolves a target");
            Assert.AreEqual("Nothing", target.BlueprintName);

            SetReturnState(input, "Normal");
            InvokeNonPublic(input, "OpenWorldActionMenuFor", target, cell, true);

            Assert.AreEqual("Normal", GetInputState(input));
            Assert.IsFalse(menu.IsOpen, "an empty action list must not open the menu");
        }
    }
}
