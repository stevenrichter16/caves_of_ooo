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

        [Test]
        public void ExamineCommand_AfflictedEntity_ListsEachEffectLine()
        {
            // Examine must show the same affliction detail the look-mode
            // FOCUS panel shows: one exact line per live effect.
            var entity = new Entity { BlueprintName = "TestCritter" };
            entity.AddPart(new RenderPart { DisplayName = "gnome" });
            entity.AddPart(new ExaminablePart());
            Assert.IsTrue(entity.ApplyEffect(new PoisonedEffect(5, "1d3")));

            var e = GameEvent.New("InventoryAction");
            e.SetParameter("Command", "Examine");
            entity.FireEvent(e);

            string last = MessageLog.GetLast() ?? string.Empty;
            StringAssert.Contains("Afflicted", last);
            StringAssert.Contains("Poisoned", last);
            StringAssert.Contains("1d3", last);
        }

        [Test]
        public void ExamineCommand_CleanEntity_NoAfflictedBlock()
        {
            // Counter-check: no effects, no affliction block.
            var entity = new Entity { BlueprintName = "TestCritter" };
            entity.AddPart(new RenderPart { DisplayName = "gnome" });
            entity.AddPart(new ExaminablePart());

            var e = GameEvent.New("InventoryAction");
            e.SetParameter("Command", "Examine");
            entity.FireEvent(e);

            StringAssert.DoesNotContain("Afflicted", MessageLog.GetLast() ?? string.Empty);
        }

        // ==========================================================
        // Docs/FELLING-W1-W2-PLAN.md SM0 — the Description/Text bind.
        // 40 shipped blueprints author their examine copy under the
        // JSON key "Description"; EntityFactory.ApplyParameters binds
        // params by exact field/property name via reflection and
        // silently no-ops on a miss, so every one of those 40 objects
        // examined as a bare "You see a {name}." with the authored
        // prose dropped on the floor.
        // ==========================================================

        [Test]
        public void DescriptionProperty_IsAnAliasForText()
        {
            // Direct part-level proof, independent of blueprint loading:
            // setting Description must be indistinguishable from setting
            // Text, since real content authors both spellings.
            var part = new ExaminablePart { Description = "Salt water." };
            Assert.AreEqual("Salt water.", part.Text,
                "Description must write straight through to Text, not a separate field.");
        }

        [Test]
        public void Blueprint_BrinePool_ExamineIncludesTheAuthoredDescription()
        {
            // End-to-end: a real shipped blueprint (BrinePool) whose
            // Examinable part is authored with a "Description" JSON key.
            // Before the fix this asserted "You see a brine pool." with
            // the flavor text silently missing.
            var pool = _factory.CreateEntity("BrinePool");
            Assert.IsNotNull(pool, "BrinePool should resolve.");

            pool.FireEvent(BuildCommand("Examine"));

            Assert.That(MessageLog.GetMessages(),
                Does.Contain("You see a brine pool. Salt water. Conducts better than fresh and freezes later."));
        }

        [Test]
        public void ExamineCommand_TextKey_StillWorksAfterTheFix()
        {
            // Counter-check: the fix is an ADDITIVE alias, not a rename —
            // blueprints/tests that already author the Text field directly
            // must be unaffected. (Pinned already by
            // ExamineCommand_AppendsFlavorTextWhenSet above; restated here
            // as an explicit SM0 regression pin next to the new tests.)
            var entity = new Entity { BlueprintName = "Chest" };
            entity.AddPart(new RenderPart { DisplayName = "chest" });
            entity.AddPart(new ExaminablePart { Text = "It smells faintly of mildew." });

            entity.FireEvent(BuildCommand("Examine"));

            Assert.That(MessageLog.GetMessages(),
                Does.Contain("You see a chest. It smells faintly of mildew."));
        }
    }
}
