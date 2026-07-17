using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Tests for SchematicPart — the tinker-recipe learn flow (readable
    /// schematic items, mirroring GrimoirePart's spell teaching).
    /// </summary>
    public class SchematicPartTests
    {
        private const string TestRecipesJson = @"{
  ""Recipes"": [
    {
      ""ID"": ""craft_thorn_dagger"",
      ""DisplayName"": ""Craft Thorn Dagger"",
      ""Blueprint"": ""ThornDagger"",
      ""Type"": ""Build"",
      ""Cost"": ""BC"",
      ""NumberMade"": 1
    }
  ]
}";

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            TinkerRecipeRegistry.ResetForTests();
            TinkerRecipeRegistry.InitializeFromJson(TestRecipesJson);
        }

        [TearDown]
        public void TearDown()
        {
            TinkerRecipeRegistry.ResetForTests();
        }

        private static Entity CreateReader(bool withBitLocker = true)
        {
            var reader = new Entity { BlueprintName = "Player" };
            reader.AddPart(new InventoryPart());
            if (withBitLocker)
                reader.AddPart(new BitLockerPart());
            return reader;
        }

        private static Entity CreateSchematic(
            string recipeId,
            bool consumeOnStudy = false)
        {
            var schematic = new Entity { BlueprintName = "TestSchematic" };
            schematic.AddPart(new RenderPart { DisplayName = "test schematic" });
            schematic.AddPart(new SchematicPart
            {
                RecipeID = recipeId,
                ConsumeOnStudy = consumeOnStudy
            });
            return schematic;
        }

        private static void FireStudy(Entity schematic, Entity actor)
        {
            var e = GameEvent.New("InventoryAction");
            e.SetParameter("Command", "StudySchematic");
            e.SetParameter("Actor", (object)actor);
            schematic.FireEvent(e);
            e.Release();
        }

        [Test]
        public void Study_TeachesRecipe()
        {
            var reader = CreateReader();
            var schematic = CreateSchematic("craft_thorn_dagger");
            reader.GetPart<InventoryPart>().AddObject(schematic);

            FireStudy(schematic, reader);

            Assert.IsTrue(
                reader.GetPart<BitLockerPart>().KnowsRecipe("craft_thorn_dagger"),
                "Studying a schematic should teach its recipe.");
        }

        [Test]
        public void Study_AlreadyKnown_DoesNotDuplicate_AndDoesNotConsume()
        {
            var reader = CreateReader();
            var inventory = reader.GetPart<InventoryPart>();
            var bitLocker = reader.GetPart<BitLockerPart>();
            bitLocker.LearnRecipe("craft_thorn_dagger");

            var schematic = CreateSchematic("craft_thorn_dagger", consumeOnStudy: true);
            inventory.AddObject(schematic);

            FireStudy(schematic, reader);

            // Counter-check: an already-known study must not consume the
            // schematic — otherwise re-reading destroys the item for nothing.
            Assert.IsTrue(inventory.Contains(schematic),
                "Schematic must not be consumed when the recipe is already known.");
            Assert.AreEqual(1, bitLocker.GetKnownRecipes().Count);
        }

        [Test]
        public void Study_WithoutBitLocker_FailsGracefully()
        {
            var reader = CreateReader(withBitLocker: false);
            var schematic = CreateSchematic("craft_thorn_dagger");
            reader.GetPart<InventoryPart>().AddObject(schematic);

            Assert.DoesNotThrow(() => FireStudy(schematic, reader));
        }

        [Test]
        public void Study_UnknownRecipeId_DoesNotLearn()
        {
            var reader = CreateReader();
            var schematic = CreateSchematic("no_such_recipe");
            reader.GetPart<InventoryPart>().AddObject(schematic);

            FireStudy(schematic, reader);

            // Counter-check: a corrupt schematic must not create phantom
            // knowledge — the registry gate has to hold.
            Assert.IsFalse(
                reader.GetPart<BitLockerPart>().KnowsRecipe("no_such_recipe"),
                "An unknown recipe ID must not be learned.");
            Assert.AreEqual(0, reader.GetPart<BitLockerPart>().GetKnownRecipes().Count);
        }

        [Test]
        public void Study_ConsumeOnStudy_RemovesSchematicAfterLearning()
        {
            var reader = CreateReader();
            var inventory = reader.GetPart<InventoryPart>();
            var schematic = CreateSchematic("craft_thorn_dagger", consumeOnStudy: true);
            inventory.AddObject(schematic);

            FireStudy(schematic, reader);

            Assert.IsTrue(
                reader.GetPart<BitLockerPart>().KnowsRecipe("craft_thorn_dagger"),
                "Recipe should be learned before the schematic is consumed.");
            Assert.IsFalse(inventory.Contains(schematic),
                "ConsumeOnStudy schematic should be removed after a successful study.");
        }

        [Test]
        public void Study_PersistentByDefault_SchematicRemains()
        {
            var reader = CreateReader();
            var inventory = reader.GetPart<InventoryPart>();
            var schematic = CreateSchematic("craft_thorn_dagger");
            inventory.AddObject(schematic);

            FireStudy(schematic, reader);

            // Counter-check for the consume flag: default schematics persist.
            Assert.IsTrue(inventory.Contains(schematic),
                "Default (non-consuming) schematic should remain after study.");
        }

        [Test]
        public void GetInventoryActions_OffersStudy()
        {
            var schematic = CreateSchematic("craft_thorn_dagger");

            var e = GameEvent.New("GetInventoryActions");
            var actions = new InventoryActionList();
            e.SetParameter("Actions", (object)actions);
            schematic.FireEvent(e);
            e.Release();

            bool found = false;
            for (int i = 0; i < actions.Actions.Count; i++)
            {
                if (actions.Actions[i].Command == "StudySchematic")
                    found = true;
            }

            Assert.IsTrue(found, "Schematic should offer a Study inventory action.");
        }
    }
}
