using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests.Gameplay.Weaponcraft
{
    /// <summary>
    /// M3-L3 SM3 — the command layer that makes forging/tempering/re-forging
    /// player-exercisable: ForgeWeaponCommand / TemperWeaponCommand /
    /// ReforgeWeaponCommand, each mirroring BrewReagentsCommand's shape.
    /// The rule that lives HERE (not in the services, which deliberately
    /// take no zone): every one of these acts requires standing at the
    /// tinker's forge — gated via ForgePart.IsNearForge in Validate.
    /// Counter-checks per §3.4: away-from-forge rejections consume NOTHING.
    /// </summary>
    public class ForgeCommandsTests
    {
        private const string TestBlueprintsJson = @"{
  ""Objects"": [
    {
      ""Name"": ""ForgedWeapon"",
      ""Parts"": [
        { ""Name"": ""Physics"", ""Params"": [ { ""Key"": ""Takeable"", ""Value"": ""true"" } ] },
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""forged weapon"" } ] },
        { ""Name"": ""MeleeWeapon"", ""Params"": [ { ""Key"": ""BaseDamage"", ""Value"": ""1d2"" } ] }
      ]
    },
    {
      ""Name"": ""SteelBlade"",
      ""Parts"": [
        { ""Name"": ""Physics"", ""Params"": [ { ""Key"": ""Takeable"", ""Value"": ""true"" } ] },
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""steel blade"" } ] },
        { ""Name"": ""WeaponComponent"", ""Params"": [
          { ""Key"": ""Slot"", ""Value"": ""Blade"" },
          { ""Key"": ""BaseDamage"", ""Value"": ""1d6"" },
          { ""Key"": ""NameFragment"", ""Value"": ""steel blade"" }
        ]}
      ]
    },
    {
      ""Name"": ""IronSpike"",
      ""Parts"": [
        { ""Name"": ""Physics"", ""Params"": [ { ""Key"": ""Takeable"", ""Value"": ""true"" } ] },
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""iron spike"" } ] },
        { ""Name"": ""WeaponComponent"", ""Params"": [
          { ""Key"": ""Slot"", ""Value"": ""Blade"" },
          { ""Key"": ""BaseDamage"", ""Value"": ""1d4"" },
          { ""Key"": ""NameFragment"", ""Value"": ""iron spike"" }
        ]}
      ]
    },
    {
      ""Name"": ""OakHaft"",
      ""Parts"": [
        { ""Name"": ""Physics"", ""Params"": [ { ""Key"": ""Takeable"", ""Value"": ""true"" } ] },
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""oak haft"" } ] },
        { ""Name"": ""WeaponComponent"", ""Params"": [
          { ""Key"": ""Slot"", ""Value"": ""Haft"" },
          { ""Key"": ""NameFragment"", ""Value"": ""oak-hafted"" }
        ]}
      ]
    },
    {
      ""Name"": ""LeatherBinding"",
      ""Parts"": [
        { ""Name"": ""Physics"", ""Params"": [ { ""Key"": ""Takeable"", ""Value"": ""true"" } ] },
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""leather binding"" } ] },
        { ""Name"": ""WeaponComponent"", ""Params"": [
          { ""Key"": ""Slot"", ""Value"": ""Binding"" },
          { ""Key"": ""NameFragment"", ""Value"": ""leather-bound"" }
        ]}
      ]
    },
    {
      ""Name"": ""TinkersForge"",
      ""Parts"": [
        { ""Name"": ""Render"", ""Params"": [ { ""Key"": ""DisplayName"", ""Value"": ""tinker's forge"" } ] },
        { ""Name"": ""Forge"", ""Params"": [] }
      ]
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

        private static Entity CreateSmith()
        {
            var smith = new Entity { ID = "smith", BlueprintName = "Player" };
            smith.AddPart(new RenderPart { DisplayName = "smith" });
            smith.AddPart(new InventoryPart());
            return smith;
        }

        private static Entity GiveItem(Entity smith, EntityFactory factory, string blueprint)
        {
            Entity item = factory.CreateEntity(blueprint);
            Assert.IsNotNull(item, $"fixture blueprint '{blueprint}' must exist");
            Assert.IsTrue(smith.GetPart<InventoryPart>().AddObject(item));
            return item;
        }

        private static Entity GiveCoating(Entity smith, string effects = "Burning:2")
        {
            var coating = new Entity { ID = "coating", BlueprintName = "coating" };
            coating.AddPart(new RenderPart { DisplayName = "flame coating" });
            coating.AddPart(new BrewItemPart { Form = "Coating", EffectsRaw = effects });
            Assert.IsTrue(smith.GetPart<InventoryPart>().AddObject(coating));
            return coating;
        }

        /// <summary>Zone with the smith at (5,5); optionally a forge adjacent.</summary>
        private static Zone MakeZone(Entity smith, EntityFactory factory, bool withForge)
        {
            var zone = new Zone("ForgeCmdZone");
            Assert.IsTrue(zone.AddEntity(smith, 5, 5));

            if (withForge)
            {
                Entity forge = factory.CreateEntity("TinkersForge");
                Assert.IsNotNull(forge);
                Assert.IsTrue(zone.AddEntity(forge, 6, 5));
            }

            return zone;
        }

        private static List<Entity> Carried(Entity smith)
        {
            return smith.GetPart<InventoryPart>().Objects;
        }

        // ════════════════ ForgeWeaponCommand ════════════════

        [Test]
        public void Forge_NearForge_ProducesWeaponAndConsumesComponents()
        {
            var factory = CreateFactory();
            var smith = CreateSmith();
            var blade = GiveItem(smith, factory, "SteelBlade");
            var haft = GiveItem(smith, factory, "OakHaft");
            var binding = GiveItem(smith, factory, "LeatherBinding");
            var zone = MakeZone(smith, factory, withForge: true);

            var result = InventorySystem.ExecuteCommand(
                new ForgeWeaponCommand(blade, haft, binding, factory), smith, zone);

            Assert.IsTrue(result.Success, result.ErrorMessage);
            Assert.IsFalse(Carried(smith).Contains(blade), "blade consumed");
            Assert.IsFalse(Carried(smith).Contains(haft), "haft consumed");
            Assert.IsFalse(Carried(smith).Contains(binding), "binding consumed");

            Entity weapon = Carried(smith).Find(e => e.HasPart<WeaponAssemblyPart>());
            Assert.IsNotNull(weapon, "a forged weapon with WeaponAssemblyPart lands in inventory");
            Assert.AreEqual("1d6", weapon.GetPart<MeleeWeaponPart>().BaseDamage,
                "blade drives the dice");
        }

        [Test]
        public void Forge_AwayFromForge_RejectedAndNothingConsumed()
        {
            // Counter-check: no forge in the zone → legible rejection and the
            // components stay exactly where they were.
            var factory = CreateFactory();
            var smith = CreateSmith();
            var blade = GiveItem(smith, factory, "SteelBlade");
            var haft = GiveItem(smith, factory, "OakHaft");
            var binding = GiveItem(smith, factory, "LeatherBinding");
            var zone = MakeZone(smith, factory, withForge: false);

            var result = InventorySystem.ExecuteCommand(
                new ForgeWeaponCommand(blade, haft, binding, factory), smith, zone);

            Assert.IsFalse(result.Success);
            Assert.IsTrue(Carried(smith).Contains(blade), "blade untouched");
            Assert.IsTrue(Carried(smith).Contains(haft), "haft untouched");
            Assert.IsTrue(Carried(smith).Contains(binding), "binding untouched");
            Assert.IsNull(Carried(smith).Find(e => e.HasPart<WeaponAssemblyPart>()),
                "no weapon minted on rejection");
        }

        [Test]
        public void Forge_NullZone_RejectedLegiblyNotCrash()
        {
            var factory = CreateFactory();
            var smith = CreateSmith();
            var blade = GiveItem(smith, factory, "SteelBlade");
            var haft = GiveItem(smith, factory, "OakHaft");
            var binding = GiveItem(smith, factory, "LeatherBinding");

            var result = InventorySystem.ExecuteCommand(
                new ForgeWeaponCommand(blade, haft, binding, factory), smith, null);

            Assert.IsFalse(result.Success, "no zone = not near a forge, never a crash");
        }

        [Test]
        public void Forge_MissingComponent_RejectedBeforeAnyConsume()
        {
            var factory = CreateFactory();
            var smith = CreateSmith();
            var blade = GiveItem(smith, factory, "SteelBlade");
            var haft = GiveItem(smith, factory, "OakHaft");
            var zone = MakeZone(smith, factory, withForge: true);

            var result = InventorySystem.ExecuteCommand(
                new ForgeWeaponCommand(blade, haft, null, factory), smith, zone);

            Assert.IsFalse(result.Success);
            Assert.IsTrue(Carried(smith).Contains(blade));
            Assert.IsTrue(Carried(smith).Contains(haft));
        }

        [Test]
        public void Forge_BatchRequest_SinglesMakeExactlyOne()
        {
            // Batch semantics ride the service: unstacked components supply
            // exactly one forge; a request for 3 succeeds with 1 made
            // (partial batch = smaller success, mirroring brew).
            var factory = CreateFactory();
            var smith = CreateSmith();
            var blade = GiveItem(smith, factory, "SteelBlade");
            var haft = GiveItem(smith, factory, "OakHaft");
            var binding = GiveItem(smith, factory, "LeatherBinding");
            var zone = MakeZone(smith, factory, withForge: true);

            var result = InventorySystem.ExecuteCommand(
                new ForgeWeaponCommand(blade, haft, binding, factory, count: 3), smith, zone);

            Assert.IsTrue(result.Success);
            int weapons = Carried(smith).FindAll(e => e.HasPart<WeaponAssemblyPart>()).Count;
            Assert.AreEqual(1, weapons, "singles supply exactly one forge, not three");
        }

        // ════════════════ TemperWeaponCommand ════════════════

        [Test]
        public void Quench_NearForge_TempersWeaponAndConsumesCoating()
        {
            var factory = CreateFactory();
            var smith = CreateSmith();
            var weapon = GiveItem(smith, factory, "ForgedWeapon");
            var coating = GiveCoating(smith);
            var zone = MakeZone(smith, factory, withForge: true);

            var result = InventorySystem.ExecuteCommand(
                new TemperWeaponCommand(weapon, coating), smith, zone);

            Assert.IsTrue(result.Success, result.ErrorMessage);
            Assert.IsFalse(Carried(smith).Contains(coating), "coating consumed as quench medium");

            var temper = weapon.GetPart<WeaponTemperPart>();
            Assert.IsNotNull(temper, "temper state recorded");
            Assert.AreEqual(1, temper.TemperCount);
            StringAssert.Contains("Burning", weapon.GetPart<MeleeWeaponPart>().OnHitEffectsRaw,
                "quench writes the coating effect as an on-hit spec");
        }

        [Test]
        public void Quench_AwayFromForge_RejectedAndNothingConsumed()
        {
            // Counter-check: quenching happens where the metal is worked —
            // no forge, no temper, coating intact.
            var factory = CreateFactory();
            var smith = CreateSmith();
            var weapon = GiveItem(smith, factory, "ForgedWeapon");
            var coating = GiveCoating(smith);
            var zone = MakeZone(smith, factory, withForge: false);

            var result = InventorySystem.ExecuteCommand(
                new TemperWeaponCommand(weapon, coating), smith, zone);

            Assert.IsFalse(result.Success);
            Assert.IsTrue(Carried(smith).Contains(coating), "coating untouched");
            Assert.IsNull(weapon.GetPart<WeaponTemperPart>(), "no temper state on rejection");
        }

        [Test]
        public void Quench_NullWeaponOrCoating_Rejected()
        {
            var factory = CreateFactory();
            var smith = CreateSmith();
            var weapon = GiveItem(smith, factory, "ForgedWeapon");
            var coating = GiveCoating(smith);
            var zone = MakeZone(smith, factory, withForge: true);

            Assert.IsFalse(InventorySystem.ExecuteCommand(
                new TemperWeaponCommand(null, coating), smith, zone).Success);
            Assert.IsFalse(InventorySystem.ExecuteCommand(
                new TemperWeaponCommand(weapon, null), smith, zone).Success);
        }

        // ════════════════ ReforgeWeaponCommand ════════════════

        [Test]
        public void Reforge_NearForge_SwapsComponentAndReturnsDisplaced()
        {
            var factory = CreateFactory();
            var smith = CreateSmith();
            var blade = GiveItem(smith, factory, "SteelBlade");
            var haft = GiveItem(smith, factory, "OakHaft");
            var binding = GiveItem(smith, factory, "LeatherBinding");
            var zone = MakeZone(smith, factory, withForge: true);

            Assert.IsTrue(InventorySystem.ExecuteCommand(
                new ForgeWeaponCommand(blade, haft, binding, factory), smith, zone).Success);
            Entity weapon = Carried(smith).Find(e => e.HasPart<WeaponAssemblyPart>());
            Assert.IsNotNull(weapon);

            var spike = GiveItem(smith, factory, "IronSpike");
            var result = InventorySystem.ExecuteCommand(
                new ReforgeWeaponCommand(weapon, spike, factory), smith, zone);

            Assert.IsTrue(result.Success, result.ErrorMessage);
            Assert.IsFalse(Carried(smith).Contains(spike), "new component consumed");
            Assert.AreEqual("1d4", weapon.GetPart<MeleeWeaponPart>().BaseDamage,
                "stats recomputed from the swapped-in blade");
            Assert.IsNotNull(
                Carried(smith).Find(e => e != weapon
                    && e.GetPart<WeaponComponentPart>()?.Slot == "Blade"
                    && e.BlueprintName == "SteelBlade"),
                "displaced steel blade returned as a fresh component");
        }

        [Test]
        public void Reforge_AwayFromForge_RejectedAndNothingConsumed()
        {
            var factory = CreateFactory();
            var smith = CreateSmith();
            var blade = GiveItem(smith, factory, "SteelBlade");
            var haft = GiveItem(smith, factory, "OakHaft");
            var binding = GiveItem(smith, factory, "LeatherBinding");
            var forgeZone = MakeZone(smith, factory, withForge: true);

            Assert.IsTrue(InventorySystem.ExecuteCommand(
                new ForgeWeaponCommand(blade, haft, binding, factory), smith, forgeZone).Success);
            Entity weapon = Carried(smith).Find(e => e.HasPart<WeaponAssemblyPart>());

            // Walk away: a fresh zone with no forge.
            var fieldZone = new Zone("Field");
            Assert.IsTrue(fieldZone.AddEntity(smith, 5, 5));

            var spike = GiveItem(smith, factory, "IronSpike");
            var result = InventorySystem.ExecuteCommand(
                new ReforgeWeaponCommand(weapon, spike, factory), smith, fieldZone);

            Assert.IsFalse(result.Success);
            Assert.IsTrue(Carried(smith).Contains(spike), "spike untouched");
            Assert.AreEqual("1d6", weapon.GetPart<MeleeWeaponPart>().BaseDamage,
                "weapon stats unchanged on rejection");
        }
    }
}
