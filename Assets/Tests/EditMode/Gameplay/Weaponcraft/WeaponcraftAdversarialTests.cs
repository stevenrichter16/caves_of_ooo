using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Dedicated adversarial sweep for weaponcraft (CLAUDE.md gate —
    /// surfaces hit: state atomicity, stacking semantics, anti-exploit
    /// gates, self-referential gates, diag contracts, cross-system
    /// temper↔reforge consistency). Each test names the bug class probed
    /// and why a buggy impl would fail it.
    /// </summary>
    public class WeaponcraftAdversarialTests
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
      ""Stats"": [ { ""Name"": ""Hitpoints"", ""Value"": 8, ""Min"": 0, ""Max"": 8 } ],
      ""Tags"": []
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
            Diag.ResetAll();
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

        private static Entity GiveItem(Entity crafter, EntityFactory factory, string blueprint)
        {
            Entity item = factory.CreateEntity(blueprint);
            Assert.IsNotNull(item, $"fixture blueprint '{blueprint}' must exist");
            Assert.IsTrue(crafter.GetPart<InventoryPart>().AddObject(item));
            return item;
        }

        private static Entity ForgeWeapon(Entity crafter, EntityFactory factory)
        {
            var blade = GiveItem(crafter, factory, "SteelBlade");
            var haft = GiveItem(crafter, factory, "OakHaft");
            var binding = GiveItem(crafter, factory, "LeatherBinding");
            Assert.IsTrue(WeaponForgingService.TryForge(
                crafter, factory, blade, haft, binding, out Entity weapon, out string reason), reason);
            return weapon;
        }

        /// <summary>A coating brew item added to the crafter's inventory.</summary>
        private static Entity GiveCoating(Entity crafter, string effectsRaw, string form = "Coating")
        {
            var coating = new Entity { ID = "coat_" + effectsRaw.GetHashCode(), BlueprintName = "BurningCoating" };
            coating.AddPart(new RenderPart { DisplayName = "test coating" });
            coating.AddPart(new BrewItemPart { EffectsRaw = effectsRaw, Form = form });
            Assert.IsTrue(crafter.GetPart<InventoryPart>().AddObject(coating));
            return coating;
        }

        // ════════════════ TEMPER — the alchemy bridge ════════════════

        [Test]
        public void Adversarial_Temper_AppendsOnHitSpec_AppliesHpPenalty()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var weapon = ForgeWeapon(crafter, factory);
            var coating = GiveCoating(crafter, "Burning:2");

            bool ok = WeaponTemperingService.TryTemper(crafter, weapon, coating, out string reason);

            Assert.IsTrue(ok, reason);
            var melee = weapon.GetPart<MeleeWeaponPart>();
            StringAssert.Contains("Burning,40,,0,2", melee.OnHitEffectsRaw,
                "potency 2 → 40% chance, magnitude 2 on-hit spec.");
            Assert.AreEqual(6, weapon.GetStat("Hitpoints").Max,
                "the metal fatigues: max HP 8 → 6.");
            Assert.IsFalse(crafter.GetPart<InventoryPart>().Contains(coating),
                "the quench medium is consumed.");
            StringAssert.StartsWith("flame-quenched", weapon.GetDisplayName());
        }

        [Test]
        public void Adversarial_Temper_SecondAllowed_ThirdRejected()
        {
            // Anti-exploit: the diminishing-returns cap. Without it a player
            // stacks unlimited on-hit effects onto one blade.
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var weapon = ForgeWeapon(crafter, factory);

            Assert.IsTrue(WeaponTemperingService.TryTemper(
                crafter, weapon, GiveCoating(crafter, "Burning:1"), out _));
            Assert.IsTrue(WeaponTemperingService.TryTemper(
                crafter, weapon, GiveCoating(crafter, "Frozen:1"), out _));

            var thirdCoating = GiveCoating(crafter, "Acidic:1");
            bool third = WeaponTemperingService.TryTemper(crafter, weapon, thirdCoating, out string reason);

            Assert.IsFalse(third);
            StringAssert.Contains("no more", reason);
            Assert.IsTrue(crafter.GetPart<InventoryPart>().Contains(thirdCoating),
                "a rejected temper must not consume the coating.");
            Assert.AreEqual(2, weapon.GetPart<WeaponTemperPart>().TemperCount);
        }

        [Test]
        public void Adversarial_Temper_TonicForm_Rejected()
        {
            // Only Coating-form brews quench. A drinkable tonic poured on a
            // blade must not become a free weapon mod.
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var weapon = ForgeWeapon(crafter, factory);
            var tonic = GiveCoating(crafter, "Burning:2", form: "Tonic");

            bool ok = WeaponTemperingService.TryTemper(crafter, weapon, tonic, out string reason);

            Assert.IsFalse(ok);
            StringAssert.Contains("coating", reason);
            Assert.IsTrue(crafter.GetPart<InventoryPart>().Contains(tonic));
        }

        [Test]
        public void Adversarial_Temper_NonWeaponTarget_Rejected()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var rock = GiveItem(crafter, factory, "OakHaft"); // no MeleeWeaponPart
            var coating = GiveCoating(crafter, "Burning:2");

            bool ok = WeaponTemperingService.TryTemper(crafter, rock, coating, out string reason);

            Assert.IsFalse(ok);
            StringAssert.Contains("not a melee weapon", reason);
        }

        [Test]
        public void Adversarial_Temper_EmptyCoating_Rejected()
        {
            // A coating whose EffectsRaw parses to nothing has nothing to
            // quench with — rejecting beats silently eating the item.
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var weapon = ForgeWeapon(crafter, factory);
            var husk = GiveCoating(crafter, "");

            bool ok = WeaponTemperingService.TryTemper(crafter, weapon, husk, out string reason);

            Assert.IsFalse(ok);
            Assert.IsTrue(crafter.GetPart<InventoryPart>().Contains(husk));
        }

        [Test]
        public void Adversarial_Temper_SelfQuench_Rejected()
        {
            // Self-referential gate: a weapon that somehow carries a
            // BrewItemPart must not quench itself (consume-self while
            // writing to self corrupts the ledger).
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var weapon = ForgeWeapon(crafter, factory);
            weapon.AddPart(new BrewItemPart { EffectsRaw = "Burning:1", Form = "Coating" });

            bool ok = WeaponTemperingService.TryTemper(crafter, weapon, weapon, out string reason);

            Assert.IsFalse(ok);
            StringAssert.Contains("itself", reason);
        }

        [Test]
        public void Adversarial_Temper_StackedCoating_ConsumesOneUnit()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var weapon = ForgeWeapon(crafter, factory);
            var coating = GiveCoating(crafter, "Burning:2");
            coating.AddPart(new StackerPart { StackCount = 3 });

            Assert.IsTrue(WeaponTemperingService.TryTemper(crafter, weapon, coating, out string reason), reason);

            Assert.IsTrue(crafter.GetPart<InventoryPart>().Contains(coating));
            Assert.AreEqual(2, coating.GetPart<StackerPart>().StackCount);
        }

        [Test]
        public void Adversarial_Temper_HpFloorAtOne()
        {
            // Boundary: a weapon whose max HP is already tiny must floor at
            // 1, never 0/negative (a 0-max weapon would be born broken).
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var weapon = ForgeWeapon(crafter, factory);
            var hp = weapon.GetStat("Hitpoints");
            hp.Max = 2;
            hp.BaseValue = 2;

            WeaponTemperingService.TryTemper(crafter, weapon, GiveCoating(crafter, "Burning:1"), out _);

            Assert.AreEqual(1, weapon.GetStat("Hitpoints").Max);
            Assert.AreEqual(1, weapon.GetPart<WeaponTemperPart>().HpPenaltyTotal,
                "the recorded penalty must equal what was ACTUALLY applied (1, not 2), or the reforge refund over-heals.");
        }

        // ════════════════ TEMPER ↔ REFORGE consistency ════════════════

        [Test]
        public void Adversarial_Reforge_MeltsTemper_RestoresHp_RemovesSpecs()
        {
            // Cross-system consistency: recompute wipes temper specs from
            // OnHitEffectsRaw; the HP penalty must be refunded in the same
            // motion or the weapon keeps fatigue for a temper it no longer
            // has.
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var weapon = ForgeWeapon(crafter, factory);
            WeaponTemperingService.TryTemper(crafter, weapon, GiveCoating(crafter, "Burning:2"), out _);
            Assert.AreEqual(6, weapon.GetStat("Hitpoints").Max);

            var newHaft = GiveItem(crafter, factory, "OakHaft");
            bool ok = WeaponForgingService.TryReforge(
                crafter, factory, weapon, newHaft, out _, out string reason);

            Assert.IsTrue(ok, reason);
            Assert.AreEqual(8, weapon.GetStat("Hitpoints").Max, "the temper's HP penalty must be refunded.");
            Assert.AreEqual(0, weapon.GetPart<WeaponTemperPart>().TemperCount);
            StringAssert.DoesNotContain("Burning", weapon.GetPart<MeleeWeaponPart>().OnHitEffectsRaw ?? "",
                "temper specs must be gone after recompute.");
            StringAssert.DoesNotContain("quenched", weapon.GetDisplayName(),
                "the quench name prefix must be gone after recompute.");
        }

        [Test]
        public void Adversarial_Reforge_ThenRetemper_WorksFreshFromZero()
        {
            // The melt must actually reset the cap — a stale TemperCount
            // would lock a re-forged weapon out of tempering forever.
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var weapon = ForgeWeapon(crafter, factory);
            WeaponTemperingService.TryTemper(crafter, weapon, GiveCoating(crafter, "Burning:1"), out _);
            WeaponTemperingService.TryTemper(crafter, weapon, GiveCoating(crafter, "Frozen:1"), out _);

            var newHaft = GiveItem(crafter, factory, "OakHaft");
            WeaponForgingService.TryReforge(crafter, factory, weapon, newHaft, out _, out _);

            bool retemper = WeaponTemperingService.TryTemper(
                crafter, weapon, GiveCoating(crafter, "Acidic:1"), out string reason);

            Assert.IsTrue(retemper, reason);
            Assert.AreEqual(1, weapon.GetPart<WeaponTemperPart>().TemperCount);
        }

        // ════════════════ FORGE — stacking + exploit probes ════════════════

        [Test]
        public void Adversarial_Forge_StackedComponent_ConsumesOneUnit()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var blade = GiveItem(crafter, factory, "SteelBlade");
            blade.AddPart(new StackerPart { StackCount = 2 });
            var haft = GiveItem(crafter, factory, "OakHaft");
            var binding = GiveItem(crafter, factory, "LeatherBinding");

            Assert.IsTrue(WeaponForgingService.TryForge(
                crafter, factory, blade, haft, binding, out _, out string reason), reason);

            Assert.IsTrue(crafter.GetPart<InventoryPart>().Contains(blade),
                "a stacked blade stays in inventory after consuming one unit.");
            Assert.AreEqual(1, blade.GetPart<StackerPart>().StackCount);
        }

        [Test]
        public void Adversarial_Reforge_ImmediateReuseOfReturnedComponent_LegalAndStable()
        {
            // Anti-exploit probe: swap blade out, then immediately swap the
            // returned blade back in. Must be legal (components are
            // stateless) and must NOT duplicate items — inventory ends with
            // exactly one displaced component either way.
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var weapon = ForgeWeapon(crafter, factory);
            var spare = GiveItem(crafter, factory, "SteelBlade");

            Assert.IsTrue(WeaponForgingService.TryReforge(
                crafter, factory, weapon, spare, out Entity returnedA, out _));
            Assert.IsTrue(WeaponForgingService.TryReforge(
                crafter, factory, weapon, returnedA, out Entity returnedB, out _));

            var inventory = crafter.GetPart<InventoryPart>();
            Assert.IsFalse(inventory.Contains(returnedA), "installed again — consumed.");
            Assert.IsTrue(inventory.Contains(returnedB));
            int bladeCount = 0;
            foreach (Entity item in inventory.Objects)
                if (item.BlueprintName == "SteelBlade")
                    bladeCount++;
            Assert.AreEqual(1, bladeCount, "the swap loop must not mint extra blades.");
        }

        // ════════════════ DIAG contracts ════════════════

        [Test]
        public void Adversarial_Diag_TemperEmits_WeaponTempered_RejectionEmits_TemperRejected()
        {
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var weapon = ForgeWeapon(crafter, factory);

            WeaponTemperingService.TryTemper(crafter, weapon, GiveCoating(crafter, "Burning:1"), out _);
            WeaponTemperingService.TryTemper(crafter, weapon, null, out _);

            Assert.AreEqual(1, Count("WeaponTempered"));
            Assert.AreEqual(1, Count("TemperRejected"));
        }

        [Test]
        public void Adversarial_Diag_TemperChannelDisabled_BehaviorUnchanged()
        {
            Diag.SetChannel("craft", false);
            var factory = CreateFactory();
            var crafter = CreateCrafter();
            var weapon = ForgeWeapon(crafter, factory);

            bool ok = WeaponTemperingService.TryTemper(
                crafter, weapon, GiveCoating(crafter, "Burning:1"), out string reason);

            Assert.IsTrue(ok, reason);
            Assert.AreEqual(0, Count("WeaponTempered"));
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
