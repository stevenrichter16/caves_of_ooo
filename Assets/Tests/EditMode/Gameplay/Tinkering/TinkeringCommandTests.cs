using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    public class TinkeringCommandTests
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
      ""ID"": ""craft_plain_knife"",
      ""DisplayName"": ""Craft Plain Knife"",
      ""Blueprint"": ""PlainKnife"",
      ""Type"": ""Build"",
      ""Cost"": ""BR"",
      ""NumberMade"": 1
    },
    {
      ""ID"": ""mod_sharp_melee"",
      ""DisplayName"": ""Apply Sharp"",
      ""Blueprint"": ""mod_sharp"",
      ""Type"": ""Mod"",
      ""Cost"": ""BC"",
      ""TargetPart"": ""MeleeWeapon"",
      ""NumberMade"": 1
    },
    {
      ""ID"": ""mod_reinforced_plating_armor"",
      ""DisplayName"": ""Apply Reinforced Plating"",
      ""Blueprint"": ""mod_reinforced_plating"",
      ""Type"": ""Mod"",
      ""Cost"": ""BC"",
      ""TargetPart"": ""Armor"",
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
      ""Name"": ""LeatherArmor"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        {
          ""Name"": ""Render"",
          ""Params"": [
            { ""Key"": ""DisplayName"", ""Value"": ""leather armor"" },
            { ""Key"": ""RenderString"", ""Value"": ""["" },
            { ""Key"": ""ColorString"", ""Value"": ""&y"" }
          ]
        },
        {
          ""Name"": ""Equippable"",
          ""Params"": [
            { ""Key"": ""Slot"", ""Value"": ""Body"" },
            { ""Key"": ""EquipBonuses"", ""Value"": """" }
          ]
        },
        {
          ""Name"": ""Armor"",
          ""Params"": [
            { ""Key"": ""AV"", ""Value"": ""3"" },
            { ""Key"": ""DV"", ""Value"": ""-1"" }
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
        public void CraftFromRecipeCommand_Succeeds_ThroughExecutor()
        {
            var factory = CreateFactory();
            var player = CreatePlayer();
            var bits = player.GetPart<BitLockerPart>();

            bits.LearnRecipe("craft_thorn_dagger");
            bits.AddBits("BC");

            var result = InventorySystem.ExecuteCommand(
                new CraftFromRecipeCommand("craft_thorn_dagger", factory),
                player);

            Assert.IsTrue(result.Success, result.ErrorMessage);

            var inventory = player.GetPart<InventoryPart>();
            Assert.AreEqual(1, inventory.Objects.Count);
            Assert.AreEqual("ThornDagger", inventory.Objects[0].BlueprintName);
        }

        [Test]
        public void CraftFromRecipeCommand_FailsValidation_ForUnknownRecipe()
        {
            var factory = CreateFactory();
            var player = CreatePlayer();

            var result = InventorySystem.ExecuteCommand(
                new CraftFromRecipeCommand("missing_recipe", factory),
                player);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(InventoryCommandErrorCode.ValidationFailed, result.ErrorCode);
        }

        [Test]
        public void DisassembleCommand_Succeeds_ThroughExecutor()
        {
            var factory = CreateFactory();
            var player = CreatePlayer();
            var inventory = player.GetPart<InventoryPart>();
            var bits = player.GetPart<BitLockerPart>();

            var dagger = factory.CreateEntity("ThornDagger");
            Assert.NotNull(dagger);
            Assert.IsTrue(inventory.AddObject(dagger));

            var result = InventorySystem.ExecuteCommand(new DisassembleCommand(dagger), player);

            Assert.IsTrue(result.Success, result.ErrorMessage);
            Assert.AreEqual(0, inventory.Objects.Count);
            Assert.AreEqual(1, bits.GetBitCount('B'));
            Assert.AreEqual(1, bits.GetBitCount('C'));
        }

        [Test]
        public void DisassembleCommand_MeleeWithoutTinkerItem_Succeeds_ThroughExecutor()
        {
            var factory = CreateFactory();
            var player = CreatePlayer();
            var inventory = player.GetPart<InventoryPart>();
            var bits = player.GetPart<BitLockerPart>();

            var knife = factory.CreateEntity("PlainKnife");
            Assert.NotNull(knife);
            Assert.IsTrue(inventory.AddObject(knife));

            var result = InventorySystem.ExecuteCommand(new DisassembleCommand(knife), player);

            Assert.IsTrue(result.Success, result.ErrorMessage);
            Assert.AreEqual(0, inventory.Objects.Count);
            Assert.AreEqual(1, bits.GetBitCount('B'));
            Assert.AreEqual(1, bits.GetBitCount('R'));
        }

        [Test]
        public void ApplyModificationCommand_Succeeds_ThroughExecutor()
        {
            var factory = CreateFactory();
            var player = CreatePlayer();
            var inventory = player.GetPart<InventoryPart>();
            var bits = player.GetPart<BitLockerPart>();

            bits.LearnRecipe("mod_sharp_melee");
            bits.AddBits("BC");

            var knife = factory.CreateEntity("PlainKnife");
            Assert.NotNull(knife);
            Assert.IsTrue(inventory.AddObject(knife));

            var weapon = knife.GetPart<MeleeWeaponPart>();
            Assert.NotNull(weapon);
            int initialPen = weapon.PenBonus;

            var result = InventorySystem.ExecuteCommand(
                new ApplyModificationCommand("mod_sharp_melee", knife),
                player);

            Assert.IsTrue(result.Success, result.ErrorMessage);
            Assert.AreEqual(initialPen + 1, weapon.PenBonus);
            Assert.IsTrue(knife.HasTag("ModSharp"));
        }

        [Test]
        public void ApplyModificationCommand_FailsValidation_WhenTargetIsMissing()
        {
            var player = CreatePlayer();

            var result = InventorySystem.ExecuteCommand(
                new ApplyModificationCommand("mod_sharp_melee", null),
                player);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(InventoryCommandErrorCode.ValidationFailed, result.ErrorCode);
        }

        [Test]
        public void ApplyModificationCommand_FailsValidation_ForStackedTarget()
        {
            var factory = CreateFactory();
            var player = CreatePlayer();
            var inventory = player.GetPart<InventoryPart>();
            var bits = player.GetPart<BitLockerPart>();

            bits.LearnRecipe("mod_sharp_melee");
            bits.AddBits("BC");

            var knife = factory.CreateEntity("PlainKnife");
            Assert.NotNull(knife);
            knife.AddPart(new StackerPart { StackCount = 2 });
            Assert.IsTrue(inventory.AddObject(knife));

            var result = InventorySystem.ExecuteCommand(
                new ApplyModificationCommand("mod_sharp_melee", knife),
                player);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(InventoryCommandErrorCode.ValidationFailed, result.ErrorCode);
            Assert.IsFalse(knife.HasTag("ModSharp"));
            Assert.AreEqual(1, bits.GetBitCount('B'));
            Assert.AreEqual(1, bits.GetBitCount('C'));
        }

        [Test]
        public void ApplyModificationCommand_Succeeds_OnEquippedTarget()
        {
            var factory = CreateFactory();
            var player = CreatePlayer();
            var inventory = player.GetPart<InventoryPart>();
            var bits = player.GetPart<BitLockerPart>();

            bits.LearnRecipe("mod_sharp_melee");
            bits.AddBits("BC");

            var knife = factory.CreateEntity("PlainKnife");
            Assert.NotNull(knife);
            Assert.IsTrue(inventory.AddObject(knife));
            Assert.IsTrue(inventory.Equip(knife, "Hand"));

            var result = InventorySystem.ExecuteCommand(
                new ApplyModificationCommand("mod_sharp_melee", knife),
                player);

            Assert.IsTrue(result.Success, result.ErrorMessage);
            Assert.IsTrue(knife.HasTag("ModSharp"));
            Assert.AreEqual(0, bits.GetBitCount('B'));
            Assert.AreEqual(0, bits.GetBitCount('C'));
        }

        [Test]
        public void ApplyModificationCommand_ArmorMod_Succeeds_ThroughExecutor()
        {
            var factory = CreateFactory();
            var player = CreatePlayer();
            var inventory = player.GetPart<InventoryPart>();
            var bits = player.GetPart<BitLockerPart>();

            bits.LearnRecipe("mod_reinforced_plating_armor");
            bits.AddBits("BC");

            var armorItem = factory.CreateEntity("LeatherArmor");
            Assert.NotNull(armorItem);
            Assert.IsTrue(inventory.AddObject(armorItem));

            var armor = armorItem.GetPart<ArmorPart>();
            Assert.NotNull(armor);

            var result = InventorySystem.ExecuteCommand(
                new ApplyModificationCommand("mod_reinforced_plating_armor", armorItem),
                player);

            Assert.IsTrue(result.Success, result.ErrorMessage);
            Assert.AreEqual(4, armor.AV);
            Assert.AreEqual(-2, armor.DV);
            Assert.IsTrue(armorItem.HasTag("ModReinforcedPlating"));
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

            player.AddPart(new InventoryPart());
            player.AddPart(new BitLockerPart());
            return player;
        }
    }
}
