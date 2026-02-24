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
    },
    {
      ""ID"": ""mod_flexweave_armor"",
      ""DisplayName"": ""Apply Flexweave"",
      ""Blueprint"": ""mod_flexweave"",
      ""Type"": ""Mod"",
      ""Cost"": ""GC"",
      ""TargetPart"": ""Armor"",
      ""NumberMade"": 1
    },
    {
      ""ID"": ""mod_hardened_shell_armor"",
      ""DisplayName"": ""Apply Hardened Shell"",
      ""Blueprint"": ""mod_hardened_shell"",
      ""Type"": ""Mod"",
      ""Cost"": ""BBC"",
      ""TargetPart"": ""Armor"",
      ""NumberMade"": 1
    },
    {
      ""ID"": ""mod_duelist_cut_armor"",
      ""DisplayName"": ""Apply Duelist Cut"",
      ""Blueprint"": ""mod_duelist_cut"",
      ""Type"": ""Mod"",
      ""Cost"": ""CG"",
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
            { ""Key"": ""DV"", ""Value"": ""-1"" },
            { ""Key"": ""SpeedPenalty"", ""Value"": ""0"" }
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

        [Test]
        public void ApplyModification_Succeeds_OnCompatibleMeleeWeapon()
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

            bool success = TinkeringService.TryApplyModification(
                player,
                "mod_sharp_melee",
                knife,
                out string reason);

            Assert.IsTrue(success, reason);
            Assert.AreEqual(initialPen + 1, weapon.PenBonus);
            Assert.IsTrue(knife.HasTag("ModSharp"));
            Assert.AreEqual(0, bits.GetBitCount('B'));
            Assert.AreEqual(0, bits.GetBitCount('C'));
        }

        [Test]
        public void ApplyModification_Fails_WhenAlreadyApplied()
        {
            var factory = CreateFactory();
            var player = CreatePlayer();
            var inventory = player.GetPart<InventoryPart>();
            var bits = player.GetPart<BitLockerPart>();

            bits.LearnRecipe("mod_sharp_melee");
            bits.AddBits("BCBC");

            var knife = factory.CreateEntity("PlainKnife");
            Assert.NotNull(knife);
            Assert.IsTrue(inventory.AddObject(knife));

            bool first = TinkeringService.TryApplyModification(
                player,
                "mod_sharp_melee",
                knife,
                out string firstReason);
            Assert.IsTrue(first, firstReason);

            bool second = TinkeringService.TryApplyModification(
                player,
                "mod_sharp_melee",
                knife,
                out string secondReason);
            Assert.IsFalse(second);
            StringAssert.Contains("already", secondReason.ToLowerInvariant());
        }

        [Test]
        public void ApplyModification_Fails_WhenNoTargetItemProvided()
        {
            var player = CreatePlayer();
            var bits = player.GetPart<BitLockerPart>();

            bits.LearnRecipe("mod_sharp_melee");
            bits.AddBits("BC");

            bool success = TinkeringService.TryApplyModification(
                player,
                "mod_sharp_melee",
                null,
                out string reason);

            Assert.IsFalse(success);
            StringAssert.Contains("target", reason.ToLowerInvariant());
            Assert.AreEqual(1, bits.GetBitCount('B'));
            Assert.AreEqual(1, bits.GetBitCount('C'));
        }

        [Test]
        public void ApplyModification_Fails_OnStackedTarget_WithoutConsumingBits()
        {
            var factory = CreateFactory();
            var player = CreatePlayer();
            var inventory = player.GetPart<InventoryPart>();
            var bits = player.GetPart<BitLockerPart>();

            bits.LearnRecipe("mod_sharp_melee");
            bits.AddBits("BC");

            var knifeStack = factory.CreateEntity("PlainKnife");
            Assert.NotNull(knifeStack);
            knifeStack.AddPart(new StackerPart { StackCount = 2 });
            Assert.IsTrue(inventory.AddObject(knifeStack));

            bool success = TinkeringService.TryApplyModification(
                player,
                "mod_sharp_melee",
                knifeStack,
                out string reason);

            Assert.IsFalse(success);
            StringAssert.Contains("split", reason.ToLowerInvariant());
            Assert.AreEqual(1, bits.GetBitCount('B'));
            Assert.AreEqual(1, bits.GetBitCount('C'));
            Assert.IsFalse(knifeStack.HasTag("ModSharp"));
        }

        [Test]
        public void ApplyModification_Succeeds_OnEquippedTarget()
        {
            var factory = CreateFactory();
            var player = CreatePlayer();
            var inventory = player.GetPart<InventoryPart>();
            var bits = player.GetPart<BitLockerPart>();

            bits.LearnRecipe("mod_sharp_melee");
            bits.AddBits("BC");

            var equippedKnife = factory.CreateEntity("PlainKnife");
            Assert.NotNull(equippedKnife);
            Assert.IsTrue(inventory.AddObject(equippedKnife));
            Assert.IsTrue(inventory.Equip(equippedKnife, "Hand"));

            bool success = TinkeringService.TryApplyModification(
                player,
                "mod_sharp_melee",
                equippedKnife,
                out string reason);

            Assert.IsTrue(success, reason);
            Assert.IsTrue(equippedKnife.HasTag("ModSharp"));
            Assert.AreEqual(0, bits.GetBitCount('B'));
            Assert.AreEqual(0, bits.GetBitCount('C'));
        }

        [Test]
        public void ApplyModification_OnlyModifiesSelectedTarget_WhenMultipleAreCompatible()
        {
            var factory = CreateFactory();
            var player = CreatePlayer();
            var inventory = player.GetPart<InventoryPart>();
            var bits = player.GetPart<BitLockerPart>();

            bits.LearnRecipe("mod_sharp_melee");
            bits.AddBits("BC");

            var firstKnife = factory.CreateEntity("PlainKnife");
            var secondKnife = factory.CreateEntity("PlainKnife");
            Assert.NotNull(firstKnife);
            Assert.NotNull(secondKnife);
            Assert.IsTrue(inventory.AddObject(firstKnife));
            Assert.IsTrue(inventory.AddObject(secondKnife));

            bool success = TinkeringService.TryApplyModification(
                player,
                "mod_sharp_melee",
                secondKnife,
                out string reason);

            Assert.IsTrue(success, reason);
            Assert.IsFalse(firstKnife.HasTag("ModSharp"));
            Assert.IsTrue(secondKnife.HasTag("ModSharp"));
            Assert.AreEqual(0, bits.GetBitCount('B'));
            Assert.AreEqual(0, bits.GetBitCount('C'));
        }

        [Test]
        public void ApplyModification_ReinforcedPlating_AdjustsArmorStats()
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

            bool success = TinkeringService.TryApplyModification(
                player,
                "mod_reinforced_plating_armor",
                armorItem,
                out string reason);

            Assert.IsTrue(success, reason);
            Assert.AreEqual(4, armor.AV);
            Assert.AreEqual(-2, armor.DV);
            Assert.IsTrue(armorItem.HasTag("ModReinforcedPlating"));
            Assert.AreEqual(0, bits.GetBitCount('B'));
            Assert.AreEqual(0, bits.GetBitCount('C'));
        }

        [Test]
        public void ApplyModification_Flexweave_AdjustsArmorStats()
        {
            var factory = CreateFactory();
            var player = CreatePlayer();
            var inventory = player.GetPart<InventoryPart>();
            var bits = player.GetPart<BitLockerPart>();

            bits.LearnRecipe("mod_flexweave_armor");
            bits.AddBits("GC");

            var armorItem = factory.CreateEntity("LeatherArmor");
            Assert.NotNull(armorItem);
            Assert.IsTrue(inventory.AddObject(armorItem));

            var armor = armorItem.GetPart<ArmorPart>();
            Assert.NotNull(armor);

            bool success = TinkeringService.TryApplyModification(
                player,
                "mod_flexweave_armor",
                armorItem,
                out string reason);

            Assert.IsTrue(success, reason);
            Assert.AreEqual(2, armor.AV);
            Assert.AreEqual(1, armor.DV);
            Assert.IsTrue(armorItem.HasTag("ModFlexweave"));
            Assert.AreEqual(0, bits.GetBitCount('G'));
            Assert.AreEqual(0, bits.GetBitCount('C'));
        }

        [Test]
        public void ApplyModification_HardenedShell_AdjustsArmorAndEquippedSpeedPenalty()
        {
            var factory = CreateFactory();
            var player = CreatePlayer();
            var inventory = player.GetPart<InventoryPart>();
            var bits = player.GetPart<BitLockerPart>();

            bits.LearnRecipe("mod_hardened_shell_armor");
            bits.AddBits("BBC");

            var armorItem = factory.CreateEntity("LeatherArmor");
            Assert.NotNull(armorItem);
            Assert.IsTrue(inventory.AddObject(armorItem));
            Assert.IsTrue(inventory.Equip(armorItem, "Body"));

            var armor = armorItem.GetPart<ArmorPart>();
            Assert.NotNull(armor);
            int speedPenaltyBefore = player.GetStat("Speed").Penalty;

            bool success = TinkeringService.TryApplyModification(
                player,
                "mod_hardened_shell_armor",
                armorItem,
                out string reason);

            Assert.IsTrue(success, reason);
            Assert.AreEqual(5, armor.AV);
            Assert.AreEqual(10, armor.SpeedPenalty);
            Assert.AreEqual(speedPenaltyBefore + 10, player.GetStat("Speed").Penalty);
            Assert.IsTrue(armorItem.HasTag("ModHardenedShell"));
            Assert.AreEqual(0, bits.GetBitCount('B'));
            Assert.AreEqual(0, bits.GetBitCount('C'));
        }

        [Test]
        public void ApplyModification_DuelistCut_AdjustsArmorAndAgilityBonus()
        {
            var factory = CreateFactory();
            var player = CreatePlayer();
            var inventory = player.GetPart<InventoryPart>();
            var bits = player.GetPart<BitLockerPart>();

            bits.LearnRecipe("mod_duelist_cut_armor");
            bits.AddBits("CG");

            var armorItem = factory.CreateEntity("LeatherArmor");
            Assert.NotNull(armorItem);
            Assert.IsTrue(inventory.AddObject(armorItem));
            Assert.IsTrue(inventory.Equip(armorItem, "Body"));

            var armor = armorItem.GetPart<ArmorPart>();
            var equippable = armorItem.GetPart<EquippablePart>();
            Assert.NotNull(armor);
            Assert.NotNull(equippable);

            int agilityBonusBefore = player.GetStat("Agility").Bonus;

            bool success = TinkeringService.TryApplyModification(
                player,
                "mod_duelist_cut_armor",
                armorItem,
                out string reason);

            Assert.IsTrue(success, reason);
            Assert.AreEqual(2, armor.AV);
            StringAssert.Contains("Agility:2", equippable.EquipBonuses);
            Assert.AreEqual(agilityBonusBefore + 2, player.GetStat("Agility").Bonus);
            Assert.IsTrue(armorItem.HasTag("ModDuelistCut"));
            Assert.AreEqual(0, bits.GetBitCount('C'));
            Assert.AreEqual(0, bits.GetBitCount('G'));
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
            player.Statistics["Agility"] = new Stat
            {
                Name = "Agility",
                BaseValue = 16,
                Min = 1,
                Max = 50,
                Owner = player
            };
            player.Statistics["Speed"] = new Stat
            {
                Name = "Speed",
                BaseValue = 100,
                Min = 1,
                Max = 500,
                Owner = player
            };

            player.AddPart(new InventoryPart());
            player.AddPart(new BitLockerPart());
            return player;
        }
    }
}
