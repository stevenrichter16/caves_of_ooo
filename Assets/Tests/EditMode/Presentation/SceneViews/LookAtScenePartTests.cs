using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests.EditMode.Presentation.SceneViews
{
    /// <summary>
    /// M6 TDD tests: <see cref="LookAtScenePart"/> contributes a "Look" action
    /// to the inventory action list, and executing that action calls
    /// <see cref="SceneViewManager.Activate(string)"/> with the configured
    /// SceneID.
    ///
    /// Mirrors the ConversationPart action-declaration test pattern at
    /// Assets/Tests/EditMode/Gameplay/Conversations/ConversationPartActionTests.cs:52-73.
    ///
    /// Plan: Docs/Plans/SCENE_VIEW_SYSTEM_IMPLEMENTATION_PLAN.md M6
    /// </summary>
    [TestFixture]
    public class LookAtScenePartTests
    {
        private const string LOOK_COMMAND = "LookAtScene";

        private EntityFactory _factory;

        [SetUp]
        public void SetUp()
        {
            FactionManager.Initialize();
            MessageLog.Clear();
            // Static SceneViewManager state — must be clean per test.
            SceneViewManager.Reset();

            _factory = new EntityFactory();
            string blueprintPath = Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json");
            _factory.LoadBlueprints(File.ReadAllText(blueprintPath));
        }

        [TearDown]
        public void TearDown()
        {
            SceneViewManager.Reset();
            FactionManager.Reset();
        }

        // ==========================================================
        // Action declaration
        // ==========================================================

        [Test]
        public void LookAtScenePart_DeclaresLookAction()
        {
            var entity = new Entity { BlueprintName = "TestCampfire" };
            entity.AddPart(new RenderPart { DisplayName = "campfire" });
            entity.AddPart(new LookAtScenePart { SceneID = "Campfire" });

            var actions = new InventoryActionList();
            var e = GameEvent.New("GetInventoryActions");
            e.SetParameter("Actions", actions);
            entity.FireEvent(e);

            InventoryAction look = null;
            foreach (var a in actions.Actions)
                if (a.Command == LOOK_COMMAND) look = a;

            Assert.IsNotNull(look,
                "LookAtScenePart should add a LookAtScene action.");
        }

        [Test]
        public void LookAction_HasExpectedFields()
        {
            var entity = new Entity { BlueprintName = "TestCampfire" };
            entity.AddPart(new LookAtScenePart { SceneID = "Campfire" });

            var actions = new InventoryActionList();
            var e = GameEvent.New("GetInventoryActions");
            e.SetParameter("Actions", actions);
            entity.FireEvent(e);

            InventoryAction look = null;
            foreach (var a in actions.Actions)
                if (a.Command == LOOK_COMMAND) look = a;

            Assert.IsNotNull(look, "Precondition: Look action must exist");
            Assert.AreEqual("Look", look.Name,
                "Action Name should be 'Look'");
            Assert.AreEqual('l', look.Key,
                "Hotkey should be 'l' so muscle memory carries from a Look-equivalent verb");
        }

        [Test]
        public void EntityWithoutLookAtScenePart_DoesNotDeclareLook()
        {
            // Counter-check: an entity that lacks LookAtScenePart must not
            // surface the Look action — proves the part is what contributes
            // it, not some ambient default.
            var entity = new Entity { BlueprintName = "BareEntity" };
            entity.AddPart(new RenderPart { DisplayName = "rock" });

            var actions = new InventoryActionList();
            var e = GameEvent.New("GetInventoryActions");
            e.SetParameter("Actions", actions);
            entity.FireEvent(e);

            foreach (var a in actions.Actions)
                Assert.AreNotEqual(LOOK_COMMAND, a.Command,
                    "Entity without LookAtScenePart must not contribute a LookAtScene action");
        }

        // ==========================================================
        // Action execution
        // ==========================================================

        [Test]
        public void LookCommand_WithValidSceneID_ActivatesSceneView()
        {
            var entity = new Entity { BlueprintName = "TestCampfire" };
            entity.AddPart(new LookAtScenePart { SceneID = "Campfire" });

            Assert.IsFalse(SceneViewManager.IsActive,
                "Precondition: no scene active before command");

            // Simulate the InputHandler dispatch:
            //   InputHandler.cs:2055-2058 fires "InventoryAction" with
            //   Command + Actor parameters; the target's parts handle it.
            var actor = new Entity { BlueprintName = "Player" };
            var ev = GameEvent.New("InventoryAction");
            ev.SetParameter("Command", LOOK_COMMAND);
            ev.SetParameter("Actor", (object)actor);
            entity.FireEvent(ev);

            Assert.IsTrue(SceneViewManager.IsActive,
                "After LookAtScene command, scene view should be active");
            Assert.AreEqual("Campfire", SceneViewManager.ActiveSceneID,
                "Active scene should match the part's SceneID");
        }

        [Test]
        public void LookCommand_WithEmptySceneID_DoesNotActivate()
        {
            // Counter-check: bad config (empty SceneID) should silently
            // no-op rather than activating an undefined scene.
            var entity = new Entity { BlueprintName = "TestBroken" };
            entity.AddPart(new LookAtScenePart { SceneID = "" });

            var actor = new Entity { BlueprintName = "Player" };
            var ev = GameEvent.New("InventoryAction");
            ev.SetParameter("Command", LOOK_COMMAND);
            ev.SetParameter("Actor", (object)actor);
            entity.FireEvent(ev);

            Assert.IsFalse(SceneViewManager.IsActive,
                "Empty SceneID must not activate any scene");
        }

        [Test]
        public void LookCommand_WithNullSceneID_DoesNotActivate()
        {
            // Counter-check: null SceneID (uninitialized field) should also
            // be a silent no-op, not an exception.
            var entity = new Entity { BlueprintName = "TestBroken" };
            entity.AddPart(new LookAtScenePart { SceneID = null });

            var actor = new Entity { BlueprintName = "Player" };
            var ev = GameEvent.New("InventoryAction");
            ev.SetParameter("Command", LOOK_COMMAND);
            ev.SetParameter("Actor", (object)actor);
            Assert.DoesNotThrow(() => entity.FireEvent(ev),
                "Null SceneID must not throw");

            Assert.IsFalse(SceneViewManager.IsActive,
                "Null SceneID must not activate any scene");
        }

        [Test]
        public void DifferentCommand_DoesNotActivateScene()
        {
            // Counter-check: only the LookAtScene command should activate;
            // an unrelated command (e.g. "Chat") on the same entity must
            // be ignored by LookAtScenePart.
            var entity = new Entity { BlueprintName = "TestCampfire" };
            entity.AddPart(new LookAtScenePart { SceneID = "Campfire" });

            var actor = new Entity { BlueprintName = "Player" };
            var ev = GameEvent.New("InventoryAction");
            ev.SetParameter("Command", "Chat");
            ev.SetParameter("Actor", (object)actor);
            entity.FireEvent(ev);

            Assert.IsFalse(SceneViewManager.IsActive,
                "Non-matching command must not activate any scene");
        }

        // ==========================================================
        // Blueprint integration (production-critical)
        // ==========================================================

        [Test]
        public void Campfire_FromBlueprint_ContributesLookAction()
        {
            // Production-critical: the actual Campfire blueprint in
            // Objects.json must include a LookAtScene Part with
            // SceneID="Campfire". Without this, the player can never
            // trigger the Scene View in normal play.
            var campfire = _factory.CreateEntity("Campfire");
            Assert.IsNotNull(campfire,
                "Campfire blueprint must exist in Objects.json");

            var actions = new InventoryActionList();
            var e = GameEvent.New("GetInventoryActions");
            e.SetParameter("Actions", actions);
            campfire.FireEvent(e);

            InventoryAction look = null;
            foreach (var a in actions.Actions)
                if (a.Command == LOOK_COMMAND) look = a;

            Assert.IsNotNull(look,
                "Campfire blueprint should include LookAtScene part contributing the Look action");
        }

        [Test]
        public void Campfire_FromBlueprint_ActivatesCampfireSceneOnLook()
        {
            // End-to-end: the blueprint-loaded Campfire entity, when its
            // LookAtScene action is executed, opens the Campfire scene view.
            var campfire = _factory.CreateEntity("Campfire");
            Assert.IsNotNull(campfire);

            var actor = new Entity { BlueprintName = "Player" };
            var ev = GameEvent.New("InventoryAction");
            ev.SetParameter("Command", LOOK_COMMAND);
            ev.SetParameter("Actor", (object)actor);
            campfire.FireEvent(ev);

            Assert.IsTrue(SceneViewManager.IsActive,
                "Executing LookAtScene on a blueprint-loaded Campfire should activate the scene view");
            Assert.AreEqual("Campfire", SceneViewManager.ActiveSceneID,
                "Campfire blueprint's SceneID parameter must resolve to \"Campfire\"");
        }
    }
}
