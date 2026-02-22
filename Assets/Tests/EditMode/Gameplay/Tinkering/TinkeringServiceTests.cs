using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    public class TinkeringServiceTests
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
    },
    {
      ""ID"": ""craft_torch_from_scrap"",
      ""DisplayName"": ""Craft Torch"",
      ""Blueprint"": ""Torch"",
      ""Type"": ""Build"",
      ""Cost"": ""C"",
      ""Ingredient"": ""ScrapMetal"",
      ""NumberMade"": 1
    },
    {
      ""ID"": ""craft_plain_knife"",
      ""DisplayName"": ""Craft Plain Knife"",
      ""Blueprint"": ""PlainKnife"",
      ""Type"": ""Build"",
      ""Cost"": ""BR"",
      ""NumberMade"": 1
    }
  ]
}";

        private const string TestBlueprintsJson = @"{
  ""Objects"": [
    {
      ""Name"": ""Item"",
      ""Parts"": [
        {
          ""Name"": ""Physics"",
          ""Params"": [
            { ""Key"": ""Takeable"", ""Value"": ""true"" },
            { ""Key"": ""Weight"", ""Value"": ""1"" }
          ]
        },
        {
          ""Name"": ""Render"",
          ""Params"": [
            { ""Key"": ""DisplayName"", ""Value"": ""item"" },
            { ""Key"": ""RenderString"", ""Value"": ""?"" },
            { ""Key"": ""ColorString"", ""Value"": ""&y"" }
          ]
        }
      ],
      ""Stats"": [],
      ""Tags"": [ { ""Key"": ""Item"", ""Value"": """" } ]
    },
    {
      ""Name"": ""ThornDagger"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        {
          ""Name"": ""Render"",
          ""Params"": [
            { ""Key"": ""DisplayName"", ""Value"": ""thorn dagger"" },
            { ""Key"": ""RenderString"", ""Value"": ""/"" },
            { ""Key"": ""ColorString"", ""Value"": ""&C"" }
          ]
        },
        {
          ""Name"": ""TinkerItem"",
          ""Params"": [
            { ""Key"": ""CanDisassemble"", ""Value"": ""true"" },
            { ""Key"": ""BuildCost"", ""Value"": ""BC"" }
          ]
        }
      ],
      ""Stats"": [],
      ""Tags"": [ { ""Key"": ""Item"", ""Value"": """" } ]
    },
    {
      ""Name"": ""PlainKnife"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        {
          ""Name"": ""Render"",
          ""Params"": [
            { ""Key"": ""DisplayName"", ""Value"": ""plain knife"" },
            { ""Key"": ""RenderString"", ""Value"": ""/"" },
            { ""Key"": ""ColorString"", ""Value"": ""&w"" }
          ]
        },
        {
          ""Name"": ""MeleeWeapon"",
          ""Params"": [
            { ""Key"": ""BaseDamage"", ""Value"": ""1d4"" },
            { ""Key"": ""PenBonus"", ""Value"": ""1"" }
          ]
        }
      ],
      ""Stats"": [],
      ""Tags"": [ { ""Key"": ""Item"", ""Value"": """" } ]
    },
    {
      ""Name"": ""Torch"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        {
          ""Name"": ""Render"",
          ""Params"": [
            { ""Key"": ""DisplayName"", ""Value"": ""torch"" },
            { ""Key"": ""RenderString"", ""Value"": ""!"" },
            { ""Key"": ""ColorString"", ""Value"": ""&o"" }
          ]
        },
        {
          ""Name"": ""TinkerItem"",
          ""Params"": [
            { ""Key"": ""CanDisassemble"", ""Value"": ""true"" },
            { ""Key"": ""BuildCost"", ""Value"": ""C"" }
          ]
        }
      ],
      ""Stats"": [],
      ""Tags"": [ { ""Key"": ""Item"", ""Value"": """" } ]
    },
    {
      ""Name"": ""ScrapMetal"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        {
          ""Name"": ""Render"",
          ""Params"": [
            { ""Key"": ""DisplayName"", ""Value"": ""scrap metal"" },
            { ""Key"": ""RenderString"", ""Value"": ""*"" },
            { ""Key"": ""ColorString"", ""Value"": ""&w"" }
          ]
        }
      ],
      ""Stats"": [],
      ""Tags"": [ { ""Key"": ""Item"", ""Value"": """" } ]
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

        [Test]
        public void BitLocker_AddAndUseBits_WorksWithStringEncodedCosts()
        {
            var player = CreatePlayer();
            var bits = player.GetPart<BitLockerPart>();

            bits.AddBits("RBC");
            Assert.AreEqual(1, bits.GetBitCount('R'));
            Assert.AreEqual(1, bits.GetBitCount('B'));
            Assert.AreEqual(1, bits.GetBitCount('C'));

            Assert.IsTrue(bits.HasBits("BC"));
            Assert.IsTrue(bits.UseBits("BC"));
            Assert.AreEqual(0, bits.GetBitCount('B'));
            Assert.AreEqual(0, bits.GetBitCount('C'));
            Assert.IsFalse(bits.UseBits("C"));
        }

        [Test]
        public void Craft_Fails_WhenRecipeNotKnown()
        {
            var factory = CreateFactory();
            var player = CreatePlayer();
            var bits = player.GetPart<BitLockerPart>();
            bits.AddBits("BC");

            bool success = TinkeringService.TryCraft(
                player,
                factory,
                "craft_thorn_dagger",
                out var crafted,
                out string reason);

            Assert.IsFalse(success);
            Assert.IsEmpty(crafted);
            StringAssert.Contains("known", reason);
        }

        [Test]
        public void Craft_Fails_WhenBitsAreInsufficient()
        {
            var factory = CreateFactory();
            var player = CreatePlayer();
            var bits = player.GetPart<BitLockerPart>();
            bits.LearnRecipe("craft_thorn_dagger");
            bits.AddBits("B");

            bool success = TinkeringService.TryCraft(
                player,
                factory,
                "craft_thorn_dagger",
                out var crafted,
                out string reason);

            Assert.IsFalse(success);
            Assert.IsEmpty(crafted);
            StringAssert.Contains("bits", reason.ToLowerInvariant());
        }

        [Test]
        public void Craft_Succeeds_ConsumesBits_AndAddsCraftedItem()
        {
            var factory = CreateFactory();
            var player = CreatePlayer();
            var inventory = player.GetPart<InventoryPart>();
            var bits = player.GetPart<BitLockerPart>();

            bits.LearnRecipe("craft_thorn_dagger");
            bits.AddBits("BC");

            bool success = TinkeringService.TryCraft(
                player,
                factory,
                "craft_thorn_dagger",
                out var crafted,
                out string reason);

            Assert.IsTrue(success, reason);
            Assert.AreEqual(1, crafted.Count);
            Assert.AreEqual("ThornDagger", crafted[0].BlueprintName);
            Assert.AreEqual(1, inventory.Objects.Count);
            Assert.AreEqual("ThornDagger", inventory.Objects[0].BlueprintName);
            Assert.AreEqual(0, bits.GetBitCount('B'));
            Assert.AreEqual(0, bits.GetBitCount('C'));
        }

        [Test]
        public void Craft_WithIngredient_ConsumesIngredient()
        {
            var factory = CreateFactory();
            var player = CreatePlayer();
            var inventory = player.GetPart<InventoryPart>();
            var bits = player.GetPart<BitLockerPart>();

            bits.LearnRecipe("craft_torch_from_scrap");
            bits.AddBits("C");

            var scrap = factory.CreateEntity("ScrapMetal");
            Assert.NotNull(scrap);
            Assert.IsTrue(inventory.AddObject(scrap));
            Assert.AreEqual(1, inventory.Objects.Count);

            bool success = TinkeringService.TryCraft(
                player,
                factory,
                "craft_torch_from_scrap",
                out var crafted,
                out string reason);

            Assert.IsTrue(success, reason);
            Assert.AreEqual(1, crafted.Count);
            Assert.AreEqual("Torch", crafted[0].BlueprintName);

            // Ingredient consumed and one crafted result remains.
            Assert.AreEqual(1, inventory.Objects.Count);
            Assert.AreEqual("Torch", inventory.Objects[0].BlueprintName);
        }

        [Test]
        public void Disassemble_Succeeds_AddsBits_AndRemovesItem()
        {
            var factory = CreateFactory();
            var player = CreatePlayer();
            var inventory = player.GetPart<InventoryPart>();
            var bits = player.GetPart<BitLockerPart>();

            var dagger = factory.CreateEntity("ThornDagger");
            Assert.NotNull(dagger);
            Assert.IsTrue(inventory.AddObject(dagger));
            Assert.AreEqual(1, inventory.Objects.Count);

            bool success = TinkeringService.TryDisassemble(
                player,
                dagger,
                out string yieldedBits,
                out string reason);

            Assert.IsTrue(success, reason);
            Assert.AreEqual("BC", yieldedBits);
            Assert.AreEqual(0, inventory.Objects.Count);
            Assert.AreEqual(1, bits.GetBitCount('B'));
            Assert.AreEqual(1, bits.GetBitCount('C'));
        }

        [Test]
        public void Disassemble_Fails_WhenItemIsNotOwned()
        {
            var factory = CreateFactory();
            var player = CreatePlayer();
            var dagger = factory.CreateEntity("ThornDagger");

            bool success = TinkeringService.TryDisassemble(
                player,
                dagger,
                out string yieldedBits,
                out string reason);

            Assert.IsFalse(success);
            Assert.AreEqual(string.Empty, yieldedBits);
            StringAssert.Contains("own", reason.ToLowerInvariant());
        }

        [Test]
        public void Disassemble_MeleeWeaponWithoutTinkerItem_UsesBuildRecipeCost()
        {
            var factory = CreateFactory();
            var player = CreatePlayer();
            var inventory = player.GetPart<InventoryPart>();
            var bits = player.GetPart<BitLockerPart>();

            var knife = factory.CreateEntity("PlainKnife");
            Assert.NotNull(knife);
            Assert.IsTrue(inventory.AddObject(knife));
            Assert.AreEqual(1, inventory.Objects.Count);

            bool success = TinkeringService.TryDisassemble(
                player,
                knife,
                out string yieldedBits,
                out string reason);

            Assert.IsTrue(success, reason);
            Assert.AreEqual("BR", yieldedBits);
            Assert.AreEqual(0, inventory.Objects.Count);
            Assert.AreEqual(1, bits.GetBitCount('B'));
            Assert.AreEqual(1, bits.GetBitCount('R'));
        }

        private static EntityFactory CreateFactory()
        {
            var factory = new EntityFactory();
            factory.LoadBlueprints(TestBlueprintsJson);
            return factory;
        }

        private static Entity CreatePlayer()
        {
            var player = new Entity
            {
                BlueprintName = "Player"
            };

            player.Statistics["Strength"] = new Stat
            {
                Name = "Strength",
                BaseValue = 16,
                Min = 1,
                Max = 50,
                Owner = player
            };

            player.AddPart(new InventoryPart());
            player.AddPart(new BitLockerPart());
            return player;
        }
    }
}
