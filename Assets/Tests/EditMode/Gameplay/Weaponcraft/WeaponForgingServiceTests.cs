using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// WeaponForgingService — modular assembly (§7.1 L1). Pins the
    /// combination math (blade drives dice, bonuses sum, strength cap is
    /// MAX, attributes union, quirks concatenate), the re-forge swap
    /// contract (displaced component returned; stats recomputed
    /// deterministically), atomic rollback, and diag emissions.
    /// </summary>
    public class WeaponForgingServiceTests
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
      ""Name"": ""IronSpike"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""iron spike"" } ] },
        { ""Name"": ""WeaponComponent"", ""Params"": [
          { ""Key"": ""Slot"", ""Value"": ""Blade"" },
          { ""Key"": ""BaseDamage"", ""Value"": ""1d4"" },
          { ""Key"": ""PenBonus"", ""Value"": ""1"" },
          { ""Key"": ""Attributes"", ""Value"": ""Piercing"" },
          { ""Key"": ""NameFragment"", ""Value"": ""iron spike"" }
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
      ""Name"": ""SerratedEdge"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""serrated edge kit"" } ] },
        { ""Name"": ""WeaponComponent"", ""Params"": [
          { ""Key"": ""Slot"", ""Value"": ""Binding"" },
          { ""Key"": ""PenBonus"", ""Value"": ""1"" },
          { ""Key"": ""Attributes"", ""Value"": ""Cutting"" },
          { ""Key"": ""OnHitEffectSpec"", ""Value"": ""Bleeding,20,1d2,15,0"" },
          { ""Key"": ""NameFragment"", ""Value"": ""serrated"" }
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
    },
    {
      ""Name"": ""PlainRock"",
      ""Inherits"": ""Item"",
      ""Parts"": [
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""plain rock"" } ] }
      ],
      ""Stats"": [], ""Tags"": []
    }
  ]
}";

        /// <summary>Fixture missing ForgedWeapon — forces the creation-failure rollback.</summary>
        private const string BlueprintsWithoutForgedJson = @"{
  ""Objects"": [
    {
      ""Name"": ""Item"",
      ""Parts"": [ { ""Name"": ""Physics"", ""Params"": [ { ""Key"": ""Takeable"", ""Value"": ""true"" } ] } ],
      ""Stats"": [], ""Tags"": []
    },
    {
      ""Name"": ""SteelBlade"",
      ""Inherits"": ""Item"",
      ""Parts"": [ { ""Name"": ""WeaponComponent"", ""Params"": [
        { ""Key"": ""Slot"", ""Value"": ""Blade"" }, { ""Key"": ""BaseDamage"", ""Value"": ""1d6"" } ] } ],
      ""Stats"": [], ""Tags"": []
    },
    {
      ""Name"": ""OakHaft"",
      ""Inherits"": ""Item"",
      ""Parts"": [ { ""Name"": ""WeaponComponent"", ""Params"": [
        { ""Key"": ""Slot"", ""Value"": ""Haft"" } ] } ],
      ""Stats"": [], ""Tags"": []
    },
    {
      ""Name"": ""LeatherBinding"",
      ""Inherits"": ""Item"",
      ""Parts"": [ { ""Name"": ""WeaponComponent"", ""Params"": [
        { ""Key"": ""Slot"", ""Value"": ""Binding"" } ] } ],
      ""Stats"": [], ""Tags"": []
    }
  ]
}";

        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
            Diag.ResetAll();
        }

        private static EntityFactory CreateFactory(string json = TestBlueprintsJson)
        {
            var factory = new EntityFactory();
            factory.LoadBlueprints(json);
            return factory;
        }

        private static Entity CreateCrafter()
        {
            var crafter = new Entity { ID = "smith", BlueprintName = "Player" };
            crafter.AddPart(new RenderPart { DisplayName = "smith" });
            crafter.AddPart(new InventoryPart());
            return crafter;
        }

        private static Entity GiveItem(Entity crafter, EntityFactory factory, string blueprint)
        {
            Entity item = factory.CreateEntity(blueprint);
            Assert.IsNotNull(item, $"fixture blueprint '{blueprint}' must exist");
            Assert.IsTrue(crafter.GetPart<InventoryPart>().AddObject(item));
            return item;
        }

        // ════════════════ Assembly math ════════════════

        [Test]
        public void Forge_BladeDrivesDamageDice()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var blade = GiveItem(crafter, factory, "SteelBlade");
            var haft = GiveItem(crafter, factory, "OakHaft");
            var binding = GiveItem(crafter, factory, "LeatherBinding");

            bool ok = WeaponForgingService.TryForge(
                crafter, factory, blade, haft, binding, out Entity weapon, out string reason);

            Assert.IsTrue(ok, reason);
            Assert.AreEqual("1d6", weapon.GetPart<MeleeWeaponPart>().BaseDamage);
        }

        [Test]
        public void Forge_BonusesSum_StrengthCapIsMax()
        {
            // IronSpike pen 1 + SerratedEdge pen 1 = 2; hit 0;
            // MaxStrengthBonus = max(-1, 3, -1) = 3 (only the haft contributes).
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var blade = GiveItem(crafter, factory, "IronSpike");
            var haft = GiveItem(crafter, factory, "OakHaft");
            var binding = GiveItem(crafter, factory, "SerratedEdge");

            WeaponForgingService.TryForge(
                crafter, factory, blade, haft, binding, out Entity weapon, out string reason);

            var melee = weapon.GetPart<MeleeWeaponPart>();
            Assert.AreEqual(2, melee.PenBonus, reason);
            Assert.AreEqual(0, melee.HitBonus);
            Assert.AreEqual(3, melee.MaxStrengthBonus);
        }

        [Test]
        public void Forge_AttributesUnion_Deduplicated()
        {
            // SteelBlade "Cutting" + SerratedEdge "Cutting" → one "Cutting",
            // not "Cutting Cutting" (which would double class effects).
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var blade = GiveItem(crafter, factory, "SteelBlade");
            var haft = GiveItem(crafter, factory, "OakHaft");
            var binding = GiveItem(crafter, factory, "SerratedEdge");

            WeaponForgingService.TryForge(
                crafter, factory, blade, haft, binding, out Entity weapon, out _);

            Assert.AreEqual("Cutting", weapon.GetPart<MeleeWeaponPart>().Attributes);
        }

        [Test]
        public void Forge_QuirkFlowsIntoOnHitEffectsRaw()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var blade = GiveItem(crafter, factory, "SteelBlade");
            var haft = GiveItem(crafter, factory, "OakHaft");
            var binding = GiveItem(crafter, factory, "SerratedEdge");

            WeaponForgingService.TryForge(
                crafter, factory, blade, haft, binding, out Entity weapon, out _);

            StringAssert.Contains("Bleeding,20", weapon.GetPart<MeleeWeaponPart>().OnHitEffectsRaw);
        }

        [Test]
        public void Forge_NameAssembledFromFragments_HaftBindingBladeOrder()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var blade = GiveItem(crafter, factory, "SteelBlade");
            var haft = GiveItem(crafter, factory, "OakHaft");
            var binding = GiveItem(crafter, factory, "SerratedEdge");

            WeaponForgingService.TryForge(
                crafter, factory, blade, haft, binding, out Entity weapon, out _);

            Assert.AreEqual("oak-hafted serrated steel blade", weapon.GetDisplayName());
        }

        [Test]
        public void Forge_ComponentsConsumed_WeaponInInventory_AssemblyRecorded()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var blade = GiveItem(crafter, factory, "SteelBlade");
            var haft = GiveItem(crafter, factory, "OakHaft");
            var binding = GiveItem(crafter, factory, "LeatherBinding");

            WeaponForgingService.TryForge(
                crafter, factory, blade, haft, binding, out Entity weapon, out _);

            var inventory = crafter.GetPart<InventoryPart>();
            Assert.IsTrue(inventory.Contains(weapon));
            Assert.IsFalse(inventory.Contains(blade));
            Assert.IsFalse(inventory.Contains(haft));
            Assert.IsFalse(inventory.Contains(binding));

            var assembly = weapon.GetPart<WeaponAssemblyPart>();
            Assert.IsNotNull(assembly, "the weapon must remember its components for re-forging.");
            Assert.AreEqual("SteelBlade", assembly.BladeBlueprint);
            Assert.AreEqual("OakHaft", assembly.HaftBlueprint);
            Assert.AreEqual("LeatherBinding", assembly.BindingBlueprint);
        }

        // ════════════════ Validation ════════════════

        [Test]
        public void Forge_WrongSlotInBladePosition_Rejected()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var haftAsBlade = GiveItem(crafter, factory, "OakHaft");
            var haft = GiveItem(crafter, factory, "OakHaft");
            var binding = GiveItem(crafter, factory, "LeatherBinding");

            bool ok = WeaponForgingService.TryForge(
                crafter, factory, haftAsBlade, haft, binding, out _, out string reason);

            Assert.IsFalse(ok);
            StringAssert.Contains("blade", reason);
            Assert.IsTrue(crafter.GetPart<InventoryPart>().Contains(haftAsBlade),
                "rejection must consume nothing.");
        }

        [Test]
        public void Forge_NonComponentItem_Rejected()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var rock = GiveItem(crafter, factory, "PlainRock");
            var haft = GiveItem(crafter, factory, "OakHaft");
            var binding = GiveItem(crafter, factory, "LeatherBinding");

            bool ok = WeaponForgingService.TryForge(
                crafter, factory, rock, haft, binding, out _, out string reason);

            Assert.IsFalse(ok);
            StringAssert.Contains("not a weapon component", reason);
        }

        [Test]
        public void Forge_UnownedComponent_Rejected()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            Entity strayBlade = factory.CreateEntity("SteelBlade"); // never added
            var haft = GiveItem(crafter, factory, "OakHaft");
            var binding = GiveItem(crafter, factory, "LeatherBinding");

            bool ok = WeaponForgingService.TryForge(
                crafter, factory, strayBlade, haft, binding, out _, out string reason);

            Assert.IsFalse(ok);
            StringAssert.Contains("own", reason);
        }

        // ════════════════ Atomicity ════════════════

        [Test]
        public void Forge_MissingForgedBlueprint_RollsBackAllThreeComponents()
        {
            var factory = CreateFactory(BlueprintsWithoutForgedJson);
            var crafter = CreateCrafter();
            var blade = GiveItem(crafter, factory, "SteelBlade");
            var haft = GiveItem(crafter, factory, "OakHaft");
            var binding = GiveItem(crafter, factory, "LeatherBinding");

            bool ok = WeaponForgingService.TryForge(
                crafter, factory, blade, haft, binding, out _, out string reason);

            Assert.IsFalse(ok);
            StringAssert.Contains("ForgedWeapon", reason);
            var inventory = crafter.GetPart<InventoryPart>();
            Assert.IsTrue(inventory.Contains(blade), "rollback must restore the blade.");
            Assert.IsTrue(inventory.Contains(haft), "rollback must restore the haft.");
            Assert.IsTrue(inventory.Contains(binding), "rollback must restore the binding.");
        }

        // ════════════════ Re-forge (the RPG-identity hook) ════════════════

        [Test]
        public void Reforge_SwapsBlade_ReturnsDisplacedComponent_RecomputesStats()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var blade = GiveItem(crafter, factory, "SteelBlade");
            var haft = GiveItem(crafter, factory, "OakHaft");
            var binding = GiveItem(crafter, factory, "LeatherBinding");
            WeaponForgingService.TryForge(
                crafter, factory, blade, haft, binding, out Entity weapon, out _);

            var spike = GiveItem(crafter, factory, "IronSpike");
            bool ok = WeaponForgingService.TryReforge(
                crafter, factory, weapon, spike, out Entity returned, out string reason);

            Assert.IsTrue(ok, reason);

            var melee = weapon.GetPart<MeleeWeaponPart>();
            Assert.AreEqual("1d4", melee.BaseDamage, "the new blade's dice must apply.");
            Assert.AreEqual("Piercing", melee.Attributes, "the old blade's Cutting must be gone.");
            Assert.AreEqual(1, melee.PenBonus, "IronSpike's pen bonus must apply.");

            Assert.IsNotNull(returned);
            Assert.AreEqual("SteelBlade", returned.BlueprintName,
                "the displaced blade comes back as a fresh component.");
            Assert.IsTrue(crafter.GetPart<InventoryPart>().Contains(returned));
            Assert.IsFalse(crafter.GetPart<InventoryPart>().Contains(spike),
                "the installed component is consumed.");

            Assert.AreEqual("IronSpike", weapon.GetPart<WeaponAssemblyPart>().BladeBlueprint);
        }

        [Test]
        public void Reforge_IsDeterministic_SwapBackRestoresOriginalStats()
        {
            // forge(A) → reforge(B) → reforge(A) must equal forge(A). If it
            // doesn't, stats drift with every visit to the forge.
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var blade = GiveItem(crafter, factory, "SteelBlade");
            var haft = GiveItem(crafter, factory, "OakHaft");
            var binding = GiveItem(crafter, factory, "LeatherBinding");
            WeaponForgingService.TryForge(
                crafter, factory, blade, haft, binding, out Entity weapon, out _);
            string originalDamage = weapon.GetPart<MeleeWeaponPart>().BaseDamage;
            string originalAttributes = weapon.GetPart<MeleeWeaponPart>().Attributes;
            int originalPen = weapon.GetPart<MeleeWeaponPart>().PenBonus;

            var spike = GiveItem(crafter, factory, "IronSpike");
            WeaponForgingService.TryReforge(crafter, factory, weapon, spike, out Entity returnedSteel, out _);
            WeaponForgingService.TryReforge(crafter, factory, weapon, returnedSteel, out _, out string reason);

            var melee = weapon.GetPart<MeleeWeaponPart>();
            Assert.AreEqual(originalDamage, melee.BaseDamage, reason);
            Assert.AreEqual(originalAttributes, melee.Attributes);
            Assert.AreEqual(originalPen, melee.PenBonus);
        }

        [Test]
        public void Reforge_NonForgedWeapon_Rejected()
        {
            // A weapon without WeaponAssemblyPart (found loot) can't be
            // re-forged — it has no recorded components to swap.
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            Entity loot = factory.CreateEntity("ForgedWeapon"); // blueprint alone: no assembly part
            crafter.GetPart<InventoryPart>().AddObject(loot);
            var spike = GiveItem(crafter, factory, "IronSpike");

            bool ok = WeaponForgingService.TryReforge(
                crafter, factory, loot, spike, out _, out string reason);

            Assert.IsFalse(ok);
            StringAssert.Contains("not forged", reason);
            Assert.IsTrue(crafter.GetPart<InventoryPart>().Contains(spike),
                "rejection must consume nothing.");
        }

        [Test]
        public void Reforge_UnownedWeapon_Rejected()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            Entity strayWeapon = factory.CreateEntity("ForgedWeapon");
            var spike = GiveItem(crafter, factory, "IronSpike");

            Assert.IsFalse(WeaponForgingService.TryReforge(
                crafter, factory, strayWeapon, spike, out _, out _));
        }

        // ════════════════ Diag ════════════════

        [Test]
        public void Diag_ForgeEmitsWeaponForged_RejectionEmitsForgeRejected()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var blade = GiveItem(crafter, factory, "SteelBlade");
            var haft = GiveItem(crafter, factory, "OakHaft");
            var binding = GiveItem(crafter, factory, "LeatherBinding");

            WeaponForgingService.TryForge(crafter, factory, blade, haft, binding, out _, out _);
            WeaponForgingService.TryForge(crafter, factory, null, null, null, out _, out _);

            Assert.AreEqual(1, Count("WeaponForged"));
            Assert.AreEqual(1, Count("ForgeRejected"));
        }

        [Test]
        public void Diag_ReforgeEmitsWeaponReforged_WithSlotAndSwap()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var blade = GiveItem(crafter, factory, "SteelBlade");
            var haft = GiveItem(crafter, factory, "OakHaft");
            var binding = GiveItem(crafter, factory, "LeatherBinding");
            WeaponForgingService.TryForge(crafter, factory, blade, haft, binding, out Entity weapon, out _);
            var spike = GiveItem(crafter, factory, "IronSpike");

            WeaponForgingService.TryReforge(crafter, factory, weapon, spike, out _, out _);

            var records = DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "craft", Kind = "WeaponReforged", Limit = 5
            }).Records;
            Assert.AreEqual(1, records.Count);
            StringAssert.Contains("SteelBlade", records[0].PayloadJson);
            StringAssert.Contains("IronSpike", records[0].PayloadJson);
        }

        // ════════════════ Production content pins ════════════════

        [Test]
        public void Production_ComponentBlueprints_ExistWithValidSlots()
        {
            // Content-typo pin, same pattern as the alchemy sweep: every
            // shipped component must carry WeaponComponentPart with a known
            // slot, and blades must define damage dice.
            UnityEngine.TextAsset asset = UnityEngine.Resources.Load<UnityEngine.TextAsset>("Content/Blueprints/Objects");
            Assert.IsNotNull(asset);
            var factory = new EntityFactory();
            factory.LoadBlueprints(asset.text);

            string[] componentNames =
            {
                "SteelBladeComponent", "IronSpikeComponent", "OakHaftComponent",
                "WillowHaftComponent", "LeatherBindingComponent", "SerratedEdgeComponent"
            };

            foreach (string name in componentNames)
            {
                Entity item = factory.CreateEntity(name);
                Assert.IsNotNull(item, $"production component '{name}' must exist.");

                var part = item.GetPart<WeaponComponentPart>();
                Assert.IsNotNull(part, $"'{name}' must carry WeaponComponentPart.");
                Assert.IsTrue(
                    WeaponForgingService.IsBladeSlot(part.Slot)
                    || WeaponForgingService.IsHaftSlot(part.Slot)
                    || WeaponForgingService.IsBindingSlot(part.Slot),
                    $"'{name}' has unknown slot '{part.Slot}'.");

                if (WeaponForgingService.IsBladeSlot(part.Slot))
                    Assert.IsFalse(string.IsNullOrWhiteSpace(part.BaseDamage),
                        $"blade '{name}' must define damage dice.");

                Assert.IsFalse(string.IsNullOrWhiteSpace(part.NameFragment),
                    $"'{name}' must define a NameFragment for weapon naming.");
            }

            Entity forgedBase = factory.CreateEntity("ForgedWeapon");
            Assert.IsNotNull(forgedBase, "the ForgedWeapon base blueprint must exist.");
            Assert.IsNotNull(forgedBase.GetPart<MeleeWeaponPart>());
        }

        private static int Count(string kind)
        {
            return DiagQuery.Apply(new DiagQuery.Filter
            {
                Category = "craft", Kind = kind, Limit = 50
            }).Records.Count;
        }
    }
}
