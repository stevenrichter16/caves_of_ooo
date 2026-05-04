using System;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Verifies that <c>InputHandler.ExecuteWorldActionSelection</c> plays
    /// nicely with actions whose effect launches a Scene View — namely
    /// <see cref="LookAtScenePart"/>'s "LookAtScene" command. The action's
    /// effect synchronously calls <see cref="SceneViewManager.Activate"/>,
    /// which fires <c>OnActivated</c>, which sets <c>_inputState =
    /// SceneOpen</c> via the InputHandler's own subscription
    /// (<c>HandleSceneActivated</c>).
    ///
    /// <para>The post-fire reset block at the bottom of
    /// <c>ExecuteWorldActionSelection</c> must NOT clobber that with the
    /// default reset-to-LookMode — otherwise the scene's [E] dismiss key
    /// never reaches <c>HandleSceneOpenInput</c>, and the player is stuck
    /// inside the campfire scene with no way out.</para>
    ///
    /// <para>Counter-checked by a non-scene action that should still reset
    /// to LookMode (the existing default behavior).</para>
    ///
    /// <para>Plan: Docs/Plans/SCENE_VIEW_HANDOFF_2.md → "Bug B".</para>
    /// </summary>
    public class InputHandlerSceneViewTests
    {
        [SetUp]
        public void SetUp()
        {
            FactionManager.Initialize();
            MessageLog.Clear();
            // Static SceneViewManager state — must be clean per test, both
            // ActiveSceneID and the OnActivated/OnDeactivated subscriber lists
            // (otherwise stale InputHandler subs from a prior test fire).
            SceneViewManager.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            SceneViewManager.Reset();
            FactionManager.Reset();

            foreach (var input in UnityEngine.Object.FindObjectsOfType<InputHandler>())
                UnityEngine.Object.DestroyImmediate(input.gameObject);
        }

        [Test]
        public void ExecuteWorldActionSelection_LookAtSceneAction_LeavesInputStateAsSceneOpen()
        {
            var (input, target, cell) = SetupForActionExecution(sceneId: "Campfire");
            // Mirror the real call-site: ExecuteWorldActionSelection is invoked
            // from HandleWorldActionMenuInput, so _inputState is
            // WorldActionMenuOpen at the moment the player picks the action.
            SetPrivateInputState(input, "WorldActionMenuOpen");

            // The action's Command is what LookAtScenePart matches on inside
            // its HandleEvent for the "InventoryAction" event.
            var action = new InventoryAction("Look", "look at fire", "LookAtScene", 'l', 10);

            InvokeNonPublic(input, "ExecuteWorldActionSelection",
                action, target, cell, /*isPileCell:*/ false);

            // Sanity: the action's effect *did* fire and activate a scene.
            Assert.IsTrue(SceneViewManager.IsActive,
                "Setup precondition: SceneViewManager.Activate should have fired " +
                "synchronously during FireEventAndRelease, leaving IsActive=true.");

            // The actual invariant: the post-fire reset must not clobber the
            // SceneOpen state that OnActivated set.
            Assert.AreEqual("SceneOpen", GetPrivateInputState(input),
                "ExecuteWorldActionSelection must NOT reset _inputState to LookMode " +
                "after the action's effect set it to SceneOpen — otherwise [E] never " +
                "reaches HandleSceneOpenInput and the scene can't be dismissed.");
        }

        // Counter-check (CLAUDE.md §3.4): a non-scene-launching action SHOULD
        // still hit the default reset-to-LookMode at the end of
        // ExecuteWorldActionSelection. This proves the early-return guard is
        // CONDITIONAL on SceneViewManager.IsActive — not unconditional.
        [Test]
        public void ExecuteWorldActionSelection_NonSceneAction_ResetsInputStateToLookMode()
        {
            // Target has NO LookAtScenePart, so its parts won't activate any scene.
            var (input, target, cell) = SetupForActionExecution(sceneId: null);
            SetPrivateInputState(input, "WorldActionMenuOpen");

            // "Examine" is a benign command not handled by any part on this
            // bare test target, so the FireEventAndRelease is effectively a
            // no-op on state, and the post-fire reset path should run.
            var action = new InventoryAction("Examine", "examine", "Examine", 'x', 0);

            InvokeNonPublic(input, "ExecuteWorldActionSelection",
                action, target, cell, /*isPileCell:*/ false);

            Assert.IsFalse(SceneViewManager.IsActive,
                "Non-scene action must not have activated a SceneView.");
            Assert.AreEqual("LookMode", GetPrivateInputState(input),
                "Non-scene action should still reset _inputState to LookMode " +
                "(the default behavior at the bottom of ExecuteWorldActionSelection).");
        }

        // ============================================================
        // Helpers
        // ============================================================

        private static (InputHandler input, Entity target, Cell cell) SetupForActionExecution(
            string sceneId)
        {
            var player = new Entity { BlueprintName = "Player" };
            player.SetTag("Player");
            player.SetTag("Creature");
            player.AddPart(new RenderPart
            {
                DisplayName = "you",
                RenderString = "@",
                ColorString = "&Y",
                RenderLayer = 10
            });

            var target = new Entity { BlueprintName = "TestCampfire" };
            target.AddPart(new RenderPart
            {
                DisplayName = "campfire",
                RenderString = "*",
                ColorString = "&R",
                RenderLayer = 5
            });
            if (!string.IsNullOrEmpty(sceneId))
                target.AddPart(new LookAtScenePart { SceneID = sceneId });

            var cell = new Cell(0, 0);

            var inputGo = new GameObject("InputHandler");
            var input = inputGo.AddComponent<InputHandler>();
            input.PlayerEntity = player;
            // EditMode quirk (mirrored from InputHandlerLookModeTests'
            // explicit Awake invocation on ZoneRenderer): MonoBehaviour
            // OnEnable does not fire reliably for AddComponent'd instances
            // in EditMode test runs. Invoke it manually so OnEnable's
            // SceneViewManager.OnActivated/OnDeactivated subscriptions
            // are registered. SceneViewManager.Reset() in SetUp ensures
            // no stale subs from a prior test pile up here.
            InvokeNonPublic(input, "OnEnable");

            return (input, target, cell);
        }

        private static object InvokeNonPublic(object instance, string methodName, params object[] args)
        {
            MethodInfo method = instance.GetType().GetMethod(
                methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method,
                $"Reflection lookup failed for non-public method '{methodName}' on " +
                $"{instance.GetType().Name}. The method may have been renamed or its " +
                $"access modifier changed; update the test or the production code together.");
            return method.Invoke(instance, args);
        }

        private static void SetPrivateInputState(InputHandler inputHandler, string value)
        {
            Type enumType = typeof(InputHandler).GetNestedType("InputState", BindingFlags.NonPublic);
            object enumValue = Enum.Parse(enumType, value);
            FieldInfo field = typeof(InputHandler).GetField(
                "_inputState", BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(inputHandler, enumValue);
        }

        private static string GetPrivateInputState(InputHandler inputHandler)
        {
            FieldInfo field = typeof(InputHandler).GetField(
                "_inputState", BindingFlags.Instance | BindingFlags.NonPublic);
            return field.GetValue(inputHandler).ToString();
        }
    }
}
