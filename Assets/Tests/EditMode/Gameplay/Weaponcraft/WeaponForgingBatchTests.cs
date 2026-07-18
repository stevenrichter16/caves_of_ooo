using System.Collections.Generic;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WeaponForgingService.TryForgeBatch / GetMaxBatchCount — the same
    /// "don't punish the player for gathering a lot of one component"
    /// guarantee as BrewingBatchTests, applied to forging. No command
    /// wrapper exists yet (M3-L3 is deferred), so this is service-layer
    /// only, ready for that future command to consume.
    /// </summary>
    public class WeaponForgingBatchTests
    {
        private const string TestBlueprintsJson = @"{
  ""Objects"": [
    {
      ""Name"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Physics"", ""Params"": [ { ""Key"": ""Takeable"", ""Value"": ""true"" } ] },
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""item"" } ] }
      ],
      ""Stats"": [], ""Tags"": []
    },
    {
      ""Name"": ""ForgedWeapon"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""forged weapon"" } ] },
        { ""Name"": ""MeleeWeapon"", ""Params"": [ { ""Key"": ""BaseDamage"", ""Value"": ""1d2"" } ] }
      ],
      ""Stats"": [], ""Tags"": []
    },
    {
      ""Name"": ""SteelBlade"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""steel blade"" } ] },
        { ""Name"": ""WeaponComponent"", ""Params"": [
          { ""Key"": ""Slot"", ""Value"": ""Blade"" },
          { ""Key"": ""BaseDamage"", ""Value"": ""1d6"" },
          { ""Key"": ""Attributes"", ""Value"": ""Cutting"" },
          { ""Key"": ""NameFragment"", ""Value"": ""steel blade"" }
        ]}
      ],
      ""Stats"": [], ""Tags"": []
    },
    {
      ""Name"": ""OakHaft"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""oak haft"" } ] },
        { ""Name"": ""WeaponComponent"", ""Params"": [
          { ""Key"": ""Slot"", ""Value"": ""Haft"" },
          { ""Key"": ""MaxStrengthBonus"", ""Value"": ""3"" },
          { ""Key"": ""NameFragment"", ""Value"": ""oak-hafted"" }
        ]}
      ],
      ""Stats"": [], ""Tags"": []
    },
    {
      ""Name"": ""LeatherBinding"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""leather binding"" } ] },
        { ""Name"": ""WeaponComponent"", ""Params"": [
          { ""Key"": ""Slot"", ""Value"": ""Binding"" },
          { ""Key"": ""HitBonus"", ""Value"": ""1"" },
          { ""Key"": ""NameFragment"", ""Value"": ""leather-bound"" }
        ]}
      ],
      ""Stats"": [], ""Tags"": []
    }
  ]
}";

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
        }

        private static EntityFactory CreateFactory()
        {
            var factory = new EntityFactory();
            factory.LoadBlueprints(TestBlueprintsJson);
            return factory;
        }

        private static Entity CreateCrafter()
        {
            var crafter = new Entity { ID = "smith", BlueprintName = "Player" };
            crafter.AddPart(new RenderPart { DisplayName = "smith" });
            crafter.AddPart(new InventoryPart());
            return crafter;
        }

        private static Entity GiveStacked(Entity crafter, EntityFactory factory, string blueprint, int count)
        {
            Entity item = factory.CreateEntity(blueprint);
            Assert.IsNotNull(item, $"fixture blueprint '{blueprint}' must exist");
            if (count > 1)
                item.AddPart(new StackerPart { StackCount = count });
            Assert.IsTrue(crafter.GetPart<InventoryPart>().AddObject(item));
            return item;
        }

        private static int StackOf(Entity item)
        {
            StackerPart stacker = item.GetPart<StackerPart>();
            return stacker != null ? stacker.StackCount : 1;
        }

        // ════════════════ Happy path ════════════════

        [Test]
        public void Batch_SufficientComponents_ForgesExactlyRequestedCount()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var blade = GiveStacked(crafter, factory, "SteelBlade", 4);
            var haft = GiveStacked(crafter, factory, "OakHaft", 4);
            var binding = GiveStacked(crafter, factory, "LeatherBinding", 4);

            bool ok = WeaponForgingService.TryForgeBatch(
                crafter, factory, blade, haft, binding, 3,
                out List<Entity> produced, out int madeCount, out string reason);

            Assert.IsTrue(ok, reason);
            Assert.AreEqual(3, madeCount);
            Assert.AreEqual(3, produced.Count);
            foreach (var w in produced)
            {
                Assert.IsNotNull(w);
                Assert.AreEqual("1d6", w.GetPart<MeleeWeaponPart>().BaseDamage,
                    "every forged weapon in the batch must have identical, correct stats.");
            }

            Assert.AreEqual(1, StackOf(blade));
            Assert.AreEqual(1, StackOf(haft));
            Assert.AreEqual(1, StackOf(binding));
        }

        // ════════════════ Partial batch: scarcest slot caps the count ════════════════

        [Test]
        public void Batch_PartialWhenOneSlotRunsOutFirst_ReturnsTrueWithReducedCount()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var blade = GiveStacked(crafter, factory, "SteelBlade", 2); // scarcest
            var haft = GiveStacked(crafter, factory, "OakHaft", 5);
            var binding = GiveStacked(crafter, factory, "LeatherBinding", 5);

            bool ok = WeaponForgingService.TryForgeBatch(
                crafter, factory, blade, haft, binding, 5,
                out List<Entity> produced, out int madeCount, out string reason);

            Assert.IsTrue(ok, "capped by the scarcest slot must still be a success.");
            Assert.AreEqual(2, madeCount);
            Assert.AreEqual(2, produced.Count);
            StringAssert.Contains("Ran out", reason);
            Assert.AreEqual(3, StackOf(haft), "the plentiful slots stop being consumed once the batch stops.");
        }

        [Test]
        public void Batch_ZeroAvailable_ReturnsFalse_MadeCountZero()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            Entity strayBlade = factory.CreateEntity("SteelBlade"); // never owned
            var haft = GiveStacked(crafter, factory, "OakHaft", 3);
            var binding = GiveStacked(crafter, factory, "LeatherBinding", 3);

            bool ok = WeaponForgingService.TryForgeBatch(
                crafter, factory, strayBlade, haft, binding, 5,
                out List<Entity> produced, out int madeCount, out string reason);

            Assert.IsFalse(ok);
            Assert.AreEqual(0, madeCount);
            Assert.AreEqual(0, produced.Count);
            StringAssert.Contains("own", reason);
            Assert.AreEqual(3, StackOf(haft), "nothing consumed when the batch never gets off the ground.");
        }

        [Test]
        public void Batch_RequestedCountZeroOrNegative_RejectedImmediately_NothingConsumed()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var blade = GiveStacked(crafter, factory, "SteelBlade", 3);
            var haft = GiveStacked(crafter, factory, "OakHaft", 3);
            var binding = GiveStacked(crafter, factory, "LeatherBinding", 3);

            foreach (int badCount in new[] { 0, -1 })
            {
                bool ok = WeaponForgingService.TryForgeBatch(
                    crafter, factory, blade, haft, binding, badCount,
                    out _, out int madeCount, out string reason);

                Assert.IsFalse(ok);
                Assert.AreEqual(0, madeCount);
                Assert.IsNotEmpty(reason);
            }

            Assert.AreEqual(3, StackOf(blade));
        }

        // ════════════════ GetMaxBatchCount preview helper ════════════════

        [Test]
        public void GetMaxBatchCount_ReturnsSmallestAcrossThreeSlots()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var blade = GiveStacked(crafter, factory, "SteelBlade", 4);
            var haft = GiveStacked(crafter, factory, "OakHaft", 2);
            var binding = GiveStacked(crafter, factory, "LeatherBinding", 6);

            Assert.AreEqual(2, WeaponForgingService.GetMaxBatchCount(blade, haft, binding));
        }

        [Test]
        public void GetMaxBatchCount_UnstackedComponent_CountsAsOne()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var blade = GiveStacked(crafter, factory, "SteelBlade", 1);
            var haft = GiveStacked(crafter, factory, "OakHaft", 5);
            var binding = GiveStacked(crafter, factory, "LeatherBinding", 5);

            Assert.AreEqual(1, WeaponForgingService.GetMaxBatchCount(blade, haft, binding));
        }

        [Test]
        public void GetMaxBatchCount_AnyNullComponent_ReturnsZero()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var haft = GiveStacked(crafter, factory, "OakHaft", 5);
            var binding = GiveStacked(crafter, factory, "LeatherBinding", 5);

            Assert.AreEqual(0, WeaponForgingService.GetMaxBatchCount(null, haft, binding));
        }

        [Test]
        public void GetMaxBatchCount_IsReadOnly_DoesNotConsumeAnything()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var blade = GiveStacked(crafter, factory, "SteelBlade", 4);
            var haft = GiveStacked(crafter, factory, "OakHaft", 4);
            var binding = GiveStacked(crafter, factory, "LeatherBinding", 4);

            WeaponForgingService.GetMaxBatchCount(blade, haft, binding);

            Assert.AreEqual(4, StackOf(blade));
            Assert.AreEqual(4, StackOf(haft));
            Assert.AreEqual(4, StackOf(binding));
        }
    }
}
