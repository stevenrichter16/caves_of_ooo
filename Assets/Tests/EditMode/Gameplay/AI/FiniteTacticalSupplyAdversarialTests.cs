// Native adversarial RED: supply descriptions must agree with real water-command admission.
using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class FiniteTacticalSupplyAdversarialTests
    {
        FieldMedicineFixture fixture;
        [SetUp] public void Setup() => fixture = new FieldMedicineFixture();
        [TearDown] public void Teardown() => fixture.Dispose();

        Entity Actor()
        {
            var actor = fixture.Actor(20, medicine: false);
            actor.AddPart(new ThermalPart()); actor.AddPart(new TacticalSupplyPart());
            return actor;
        }
        Entity Water(Entity actor)
        {
            var shell = fixture.Factory.CreateEntity("SunbladderShell");
            Assert.True(actor.GetPart<InventoryPart>().AddObject(shell));
            Assert.True(SharedVessel(actor, shell)); return shell;
        }
        static bool SharedVessel(Entity actor, Entity shell)
        {
            var type = typeof(Entity).Assembly.GetType("CavesOfOoo.Core.WaterTransferActions");
            Assert.NotNull(type);
            var method = type.GetMethod("Vessel", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(method);
            object[] args = { actor, shell, 0, 0 };
            return (bool)method.Invoke(null, args);
        }
        Entity Corrupt(Entity actor, Entity shell, string fault)
        {
            var pack = actor.GetPart<InventoryPart>();
            var other = new Entity { ID = "other-pack-owner", BlueprintName = "Other" };
            switch (fault)
            {
                case "stacked": shell.AddPart(new StackerPart { StackCount = 2 }); break;
                case "foreign-stacker":
                    shell.AddPart(new StackerPart { StackCount = 1 }); shell.GetPart<StackerPart>().ParentEntity = other; break;
                case "duplicate-id":
                    var duplicate = fixture.Factory.CreateEntity("Dagger"); duplicate.ID = shell.ID;
                    Assert.True(pack.AddObject(duplicate)); return duplicate;
                case "duplicate-reference": pack.Objects.Add(shell); break;
                case "dual-vessel": shell.AddPart(new LiquidVesselPart { Capacity = 2, Volume = 1, LiquidId = "water" }); break;
                case "not-takeable": shell.GetPart<PhysicsPart>().Takeable = false; break;
                case "empty-id": shell.ID = ""; break;
                case "aliased-pack": other.AddPart(pack); break;
                case "wrong-owner": shell.GetPart<PhysicsPart>().InInventory = other; break;
                default: throw new ArgumentException(fault);
            }
            return null;
        }

        [TestCase("stacked")][TestCase("foreign-stacker")][TestCase("duplicate-id")]
        [TestCase("duplicate-reference")][TestCase("dual-vessel")][TestCase("not-takeable")]
        [TestCase("empty-id")][TestCase("aliased-pack")][TestCase("wrong-owner")]
        public void SupplyQueryAndDescriptionRefuseExactlyTheWaterThatNativeCommandsCannotUse(string fault)
        {
            var actor = Actor(); var shell = Water(actor); var policy = actor.GetPart<TacticalSupplyPart>();
            Assert.AreSame(shell, policy.FindCarriedWater()); Assert.That(policy.Describe(), Does.Contain("spends one water"));
            Corrupt(actor, shell, fault);
            Assert.False(SharedVessel(actor, shell), "The actual shared water command admission refuses this graph.");
            var pack = actor.GetPart<InventoryPart>(); var contents = pack.Objects.ToArray();
            int quantity = shell.GetPart<StackerPart>()?.StackCount ?? 1;
            int charges = shell.GetPart<WaterskinPart>().Charges, random = fixture.Rng.Calls;
            var messages = MessageLog.GetAllEntries();
            Assert.IsNull(policy.FindCarriedWater(), "Do not report an unusable shell as available emergency water.");
            Assert.That(policy.Describe(), Does.Contain("empty or unavailable").And.Not.Contain("spends one water"));
            Assert.AreEqual(charges, shell.GetPart<WaterskinPart>().Charges);
            Assert.AreEqual(quantity, shell.GetPart<StackerPart>()?.StackCount ?? 1);
            CollectionAssert.AreEqual(contents, pack.Objects); Assert.AreEqual(random, fixture.Rng.Calls);
            CollectionAssert.AreEqual(messages, MessageLog.GetAllEntries(), "Pure selection/description emits no refusal spam.");
        }

        [TestCase("stacked")][TestCase("duplicate-id")][TestCase("dual-vessel")]
        public void UnusableFirstShellDoesNotHideALaterRealUsableSupply(string fault)
        {
            var actor = Actor(); var unusable = Water(actor); Corrupt(actor, unusable, fault);
            var usable = Water(actor); var policy = actor.GetPart<TacticalSupplyPart>();
            Assert.False(SharedVessel(actor, unusable)); Assert.True(SharedVessel(actor, usable));
            Assert.AreSame(usable, policy.FindCarriedWater());
            var threat = fixture.Threat(actor); Assert.True(actor.ApplyEffect(new BurningEffect(1, null, fixture.Rng)));
            Assert.True(policy.TryUseEmergencyWater(threat, fixture.Zone));
            Assert.AreEqual(1, unusable.GetPart<WaterskinPart>().Charges);
            Assert.AreEqual(0, usable.GetPart<WaterskinPart>().Charges);
            Assert.False(actor.HasEffect<BurningEffect>()); Assert.True(actor.HasEffect<WetEffect>());
        }

        [TestCase(false)][TestCase(true)]
        public void ValidSavedSupplyRemainsQueryableBeforeLoadedActorPlacement(bool disabled)
        {
            var actor = Actor(); var shell = Water(actor); actor.GetPart<TacticalSupplyPart>().SelfDousing = !disabled;
            Assert.True(fixture.Zone.RemoveEntity(actor));
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor);
            var restored = loaded.GetPart<InventoryPart>().Objects.Single(e => e.ID == shell.ID);
            Assert.True(SharedVessel(loaded, restored));
            var policy = loaded.GetPart<TacticalSupplyPart>();
            Assert.AreSame(restored, policy.FindCarriedWater()); Assert.AreEqual(1, restored.GetPart<WaterskinPart>().Charges);
            Assert.That(policy.Describe(), Does.Contain(disabled ? "self-dousing is disabled" : "spends one water"));
            Assert.IsNull(fixture.Zone.GetEntityCell(loaded), "Inventory truth must not depend on world placement.");
        }
    }
}
