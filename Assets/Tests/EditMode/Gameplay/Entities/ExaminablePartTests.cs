using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Phase 4a of the World Action Menu plan — verifies <see cref="ExaminablePart"/>:
    /// - Adds an "Examine" action when <c>GetInventoryActions</c> fires
    /// - Logs a description line when the "Examine" InventoryAction command fires
    /// - Uses the optional <c>Text</c> field for flavor when set
    /// - Cascades to every blueprint inheriting <c>PhysicalObject</c>
    ///   (sampled via Chest, Snapjaw, Warden, HealingTonic)
    /// </summary>
    [TestFixture]
    public class ExaminablePartTests
    {
        private EntityFactory _factory;

        [SetUp]
        public void SetUp()
        {
            FactionManager.Initialize();
            MessageLog.Clear();
            _factory = new EntityFactory();
            string blueprintPath = Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json");
            _factory.LoadBlueprints(File.ReadAllText(blueprintPath));
        }

        [TearDown]
        public void TearDown()
        {
            FactionManager.Reset();
        }

        // ==========================================================
        // Direct unit tests — construct an entity with just the part
        // ==========================================================

        [Test]
        public void GetInventoryActions_AddsExamineAction()
        {
            var entity = new Entity { BlueprintName = "TestItem" };
            entity.AddPart(new RenderPart { DisplayName = "widget" });
            entity.AddPart(new ExaminablePart());

            var actions = new InventoryActionList();
            var e = GameEvent.New("GetInventoryActions");
            e.SetParameter("Actions", actions);
            entity.FireEvent(e);

            Assert.AreEqual(1, actions.Actions.Count);
            var action = actions.Actions[0];
            Assert.AreEqual("Examine", action.Name);
            Assert.AreEqual("examine", action.Display);
            Assert.AreEqual("Examine", action.Command);
            Assert.AreEqual('x', action.Key);
            Assert.AreEqual(0, action.Priority);
        }

        [Test]
        public void ExamineCommand_LogsDefaultLine_WithIndefiniteArticle()
        {
            var entity = new Entity { BlueprintName = "TestItem" };
            entity.AddPart(new RenderPart { DisplayName = "dagger" });
            entity.AddPart(new ExaminablePart());

            var e = GameEvent.New("InventoryAction");
            e.SetParameter("Command", "Examine");
            entity.FireEvent(e);

            Assert.That(MessageLog.GetMessages(), Does.Contain("You see a dagger."),
                "Default examine line should read 'You see a {name}.'");
        }

        [Test]
        public void ExamineCommand_AnArticleForVowelNames()
        {
            var entity = new Entity { BlueprintName = "TestItem" };
            entity.AddPart(new RenderPart { DisplayName = "apple" });
            entity.AddPart(new ExaminablePart());

            entity.FireEvent(BuildCommand("Examine"));

            Assert.That(MessageLog.GetMessages(), Does.Contain("You see an apple."));
        }

        [Test]
        public void ExamineCommand_NoArticleForProperNouns()
        {
            var entity = new Entity { BlueprintName = "Person" };
            entity.AddPart(new RenderPart { DisplayName = "Asphodel" });
            entity.AddPart(new ExaminablePart());

            entity.FireEvent(BuildCommand("Examine"));

            Assert.That(MessageLog.GetMessages(), Does.Contain("You see Asphodel."),
                "Proper nouns (uppercase first letter) should skip the article.");
        }

        [Test]
        public void ExamineCommand_NoArticleWhenNameAlreadyHasOne()
        {
            // "the warden" or "some coins" — preserve author-provided determiner.
            var entity = new Entity { BlueprintName = "Item" };
            entity.AddPart(new RenderPart { DisplayName = "some coins" });
            entity.AddPart(new ExaminablePart());

            entity.FireEvent(BuildCommand("Examine"));

            Assert.That(MessageLog.GetMessages(), Does.Contain("You see some coins."));
        }

        [Test]
        public void ExamineCommand_AppendsFlavorTextWhenSet()
        {
            var entity = new Entity { BlueprintName = "Chest" };
            entity.AddPart(new RenderPart { DisplayName = "chest" });
            entity.AddPart(new ExaminablePart { Text = "It smells faintly of mildew." });

            entity.FireEvent(BuildCommand("Examine"));

            Assert.That(MessageLog.GetMessages(),
                Does.Contain("You see a chest. It smells faintly of mildew."));
        }

        [Test]
        public void NonExamineCommand_IsIgnored()
        {
            var entity = new Entity { BlueprintName = "TestItem" };
            entity.AddPart(new RenderPart { DisplayName = "thing" });
            entity.AddPart(new ExaminablePart());

            // A non-Examine inventory command (e.g., OpenContainer) should not
            // log anything from Examinable — unrelated commands pass through.
            entity.FireEvent(BuildCommand("SomeOtherCommand"));

            foreach (var msg in MessageLog.GetMessages())
                StringAssert.DoesNotContain("You see", msg);
        }

        // ==========================================================
        // Blueprint cascade — every PhysicalObject-inheriting blueprint
        // should get Examinable automatically
        // ==========================================================

        [TestCase("Chest")]
        [TestCase("MimicChest")]
        [TestCase("Snapjaw")]
        [TestCase("Warden")]
        [TestCase("HealingTonic")]
        [TestCase("ShortSword")]
        [TestCase("Scribe")]
        [TestCase("SleepingTroll")]
        // Terrain cascade — these override Parts in their own blueprint,
        // so their presence pins "the blueprint loader MERGES child and parent
        // Parts rather than replacing." A loader refactor that flipped that
        // semantic would silently break Examine on every terrain tile; these
        // TestCases would catch it immediately.
        [TestCase("Wall")]
        [TestCase("StoneWall")]
        [TestCase("Floor")]
        public void Blueprint_CascadesExaminable(string blueprintName)
        {
            var entity = _factory.CreateEntity(blueprintName);
            Assert.IsNotNull(entity, $"Blueprint '{blueprintName}' should resolve.");
            Assert.IsNotNull(entity.GetPart<ExaminablePart>(),
                $"{blueprintName} inherits PhysicalObject so should have ExaminablePart attached.");
        }

        [Test]
        public void Blueprint_Chest_DeclaresBothExamineAndOpen()
        {
            // Integration check: ContainerPart declares Open, ExaminablePart
            // declares Examine, both fire on the same GetInventoryActions event.
            var chest = _factory.CreateEntity("Chest");
            var actions = new InventoryActionList();
            var e = GameEvent.New("GetInventoryActions");
            e.SetParameter("Actions", actions);
            chest.FireEvent(e);

            bool foundOpen = false, foundExamine = false;
            foreach (var a in actions.Actions)
            {
                if (a.Command == "OpenContainer") foundOpen = true;
                if (a.Command == "Examine") foundExamine = true;
            }
            Assert.IsTrue(foundOpen, "Chest should declare Open action via ContainerPart.");
            Assert.IsTrue(foundExamine, "Chest should declare Examine action via ExaminablePart.");
        }

        [Test]
        public void Blueprint_Chest_ExamineFiresDescriptionLine()
        {
            var chest = _factory.CreateEntity("Chest");
            chest.FireEvent(BuildCommand("Examine"));

            Assert.That(MessageLog.GetMessages(), Does.Contain("You see a chest."));
        }

        // ==========================================================
        // Helpers
        // ==========================================================

        private static GameEvent BuildCommand(string command)
        {
            var e = GameEvent.New("InventoryAction");
            e.SetParameter("Command", command);
            return e;
        }
    }
}
