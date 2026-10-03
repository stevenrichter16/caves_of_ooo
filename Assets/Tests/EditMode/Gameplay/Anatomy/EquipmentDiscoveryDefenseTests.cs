using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class EquipmentDiscoveryDefenseTests
    {
        static Entity BonusItem(EquipmentLifecycleFixture f, string bonuses)
        {
            var item = f.Item("Buckler");
            item.GetPart<EquippablePart>().EquipBonuses = bonuses;
            Assert.IsTrue(f.Inventory.AddObject(item));
            return item;
        }

        static int Hurt(Entity actor, string attribute, int amount = 10)
        {
            var hp = actor.GetStat("Hitpoints");
            hp.BaseValue = 200; hp.Max = 200; hp.Penalty = 0;
            int before = hp.Value;
            var damage = new Damage(amount);
            if (!string.IsNullOrEmpty(attribute)) damage.AddAttribute(attribute);
            CombatSystem.ApplyDamage(actor, damage, null, null);
            return before - hp.Value;
        }

        [TestCase("HeatResistance", "Fire")]
        [TestCase("ColdResistance", "Cold")]
        [TestCase("ElectricResistance", "Electric")]
        [TestCase("AcidResistance", "Acid")]
        public void MissingElementalStatEquipReducesTypedDamageAndUnequipRestoresIt(string stat, string type)
        {
            using (var f = new EquipmentLifecycleFixture())
            {
                Assert.IsNull(f.Actor.GetStat(stat));
                var item = BonusItem(f, stat + ":50");
                Assert.AreEqual(10, Hurt(f.Actor, type));
                Assert.IsTrue(InventorySystem.Equip(f.Actor, item, f.LeftHand));
                Assert.AreEqual(50, f.Actor.GetStatValue(stat));
                Assert.AreEqual(5, Hurt(f.Actor, type));
                Assert.AreEqual(10, Hurt(f.Actor, null));
                Assert.AreEqual(1, Hurt(f.Actor, type, 1), "Resistance is not immunity from one-point ticks.");
                Assert.IsTrue(InventorySystem.UnequipItem(f.Actor, item));
                Assert.AreEqual(0, f.Actor.GetStatValue(stat));
                Assert.AreEqual(10, Hurt(f.Actor, type));
            }
        }

        [TestCase(-50, 15)] [TestCase(50, 5)] [TestCase(100, 0)]
        public void ElementalStatUsesSignedPercentageBounds(int bonus, int damage)
        {
            using (var f = new EquipmentLifecycleFixture())
            {
                var item = BonusItem(f, "HeatResistance:" + bonus);
                Assert.IsTrue(InventorySystem.Equip(f.Actor, item, f.LeftHand));
                Assert.AreEqual(bonus, f.Actor.GetStatValue("HeatResistance"));
                Assert.AreEqual(damage, Hurt(f.Actor, "Fire"));
                Assert.AreEqual(10, Hurt(f.Actor, "Cold"));
            }
        }

        [Test]
        public void UnknownBonusStatStillDoesNotCreateArbitraryActorState()
        {
            using (var f = new EquipmentLifecycleFixture())
            {
                var item = BonusItem(f, "UnknownResistance:50,HeatResistance:nope,HeatResistanceExtra:90");
                Assert.IsTrue(InventorySystem.Equip(f.Actor, item, f.LeftHand));
                Assert.IsNull(f.Actor.GetStat("UnknownResistance"));
                Assert.IsNull(f.Actor.GetStat("HeatResistance"));
                Assert.IsNull(f.Actor.GetStat("HeatResistanceExtra"));
                Assert.AreEqual(10, Hurt(f.Actor, "Fire"));
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void ExistingBaseResistanceAndUnrelatedBonusesSurviveRemoval(bool injured)
        {
            using (var f = new EquipmentLifecycleFixture())
            {
                f.Actor.Statistics["ElectricResistance"] = new Stat { Owner=f.Actor, Name="ElectricResistance", BaseValue=10, Bonus=7, Min=-100, Max=100 };
                var item = BonusItem(f, "ElectricResistance:50");
                Assert.IsTrue(InventorySystem.Equip(f.Actor, item, f.LeftHand));
                Assert.AreEqual(67, f.Actor.GetStatValue("ElectricResistance"));
                if (injured) Assert.IsTrue(f.Body.Dismember(f.LeftArm, f.Zone));
                else Assert.IsTrue(InventorySystem.UnequipItem(f.Actor, item));
                Assert.AreEqual(17, f.Actor.GetStatValue("ElectricResistance"));
                Assert.AreEqual(7, f.Actor.GetStat("ElectricResistance").Bonus);
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void EquipVetoOrTransactionRollbackLeavesNoProtection(bool veto)
        {
            using (var f = new EquipmentLifecycleFixture())
            {
                var item = BonusItem(f, "HeatResistance:50");
                if (veto) f.Actor.AddPart(new EquipmentLifecycleObserver { VetoEvent="BeforeEquip" });
                var tx = new InventoryTransaction();
                try
                {
                    var result = new EquipCommand(item, f.LeftHand).Execute(new InventoryContext(f.Actor, f.Zone), tx);
                    Assert.AreEqual(!veto, result.Success);
                    if (!veto) Assert.AreEqual(50, f.Actor.GetStatValue("HeatResistance"));
                }
                finally { tx.Rollback(); }
                Assert.AreEqual(0, f.Actor.GetStatValue("HeatResistance"));
                Assert.AreEqual(10, Hurt(f.Actor, "Fire"));
                Assert.IsFalse(InventorySystem.IsEquipped(f.Actor, item));
            }
        }

        [TestCase("KilnfeltApron", "HeatResistance", "Fire", "Body", 1, 5)]
        [TestCase("GroundwireScreen", "ElectricResistance", "Electric", "Hand", 0, 0)]
        public void AuthoredDefenseHasRealBenefitCostAndSaveSymmetry(string blueprint, string stat, string type, string slot, int av, int speed)
        {
            using (var f = new EquipmentLifecycleFixture())
            {
                var item = f.Item(blueprint); Assert.NotNull(item);
                Assert.AreEqual(slot, item.GetPart<EquippablePart>().Slot);
                Assert.AreEqual(av, item.GetPart<ArmorPart>().AV);
                Assert.AreEqual(speed, item.GetPart<ArmorPart>().SpeedPenalty);
                Assert.IsTrue(f.Inventory.AddObject(item));
                int initial = f.Actor.GetStat("Speed").Penalty;
                Assert.IsTrue(InventorySystem.Equip(f.Actor, item));
                Assert.AreEqual(initial + speed, f.Actor.GetStat("Speed").Penalty);
                Assert.AreEqual(5, Hurt(f.Actor, type));
                Assert.AreEqual(10, Hurt(f.Actor, type == "Fire" ? "Electric" : "Fire"));
                var loaded = f.RoundTrip();
                var carried = loaded.Player.GetPart<InventoryPart>();
                var savedItem = carried.EquippedItems.Values.Single(e=>e.BlueprintName == blueprint);
                Assert.AreEqual(50, loaded.Player.GetStatValue(stat));
                Assert.AreEqual(5, Hurt(loaded.Player, type));
                Assert.IsTrue(InventorySystem.UnequipItem(loaded.Player, savedItem));
                Assert.AreEqual(0, loaded.Player.GetStatValue(stat));
                Assert.AreEqual(initial, loaded.Player.GetStat("Speed").Penalty);
                Assert.AreEqual(10, Hurt(loaded.Player, type));
            }
        }

        [Test]
        public void ActualAuthoredPlayerReceivesBothProtectionAndBodyMobilityCost()
        {
            using (var f = new EquipmentLifecycleFixture())
            {
                var player = f.Item("Player"); var apron = f.Item("KilnfeltApron");
                Assert.IsNull(player.GetStat("HeatResistance"));
                int speed = player.GetStatValue("Speed");
                Assert.IsTrue(player.GetPart<InventoryPart>().AddObject(apron));
                Assert.IsTrue(InventorySystem.Equip(player, apron));
                Assert.AreEqual(50, player.GetStatValue("HeatResistance"));
                Assert.AreEqual(speed - 5, player.GetStatValue("Speed"));
                Assert.AreEqual(5, Hurt(player, "Fire"));
                Assert.IsTrue(InventorySystem.UnequipItem(player, apron));
                Assert.AreEqual(speed, player.GetStatValue("Speed"));
                Assert.AreEqual(10, Hurt(player, "Fire"));
            }
        }

        [Test]
        public void TwoHandedWeaponDisplacesScreenAndCannotKeepItsElectricalBonus()
        {
            using (var f = new EquipmentLifecycleFixture())
            {
                var screen = f.Item("GroundwireScreen"); var sword = f.Item("Greatsword");
                Assert.IsTrue(f.Inventory.AddObject(screen)); Assert.IsTrue(f.Inventory.AddObject(sword));
                Assert.IsTrue(InventorySystem.Equip(f.Actor, screen, f.LeftHand));
                Assert.AreEqual(50, f.Actor.GetStatValue("ElectricResistance"));
                Assert.IsTrue(InventorySystem.Equip(f.Actor, sword));
                Assert.IsFalse(InventorySystem.IsEquipped(f.Actor, screen));
                Assert.AreEqual(0, f.Actor.GetStatValue("ElectricResistance"));
                Assert.AreEqual(2, f.Body.GetPartsByType("Hand").Count(p=>ReferenceEquals(p._Equipped,sword)));
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void NewlyCreatedResistanceFollowsScreenWhenArmIsLost(bool veto)
        {
            using (var f = new EquipmentLifecycleFixture())
            {
                var screen = f.Item("GroundwireScreen"); Assert.IsTrue(f.Inventory.AddObject(screen));
                Assert.IsTrue(InventorySystem.Equip(f.Actor, screen, f.LeftHand));
                f.Actor.AddPart(new EquipmentLifecycleObserver { VetoEvent=veto?"BeforeDismember":null });
                Assert.AreEqual(!veto, f.Body.Dismember(f.LeftArm, f.Zone));
                Assert.AreEqual(veto?50:0, f.Actor.GetStatValue("ElectricResistance"));
                Assert.AreEqual(veto?5:10, Hurt(f.Actor, "Electric"));
                Assert.AreEqual(veto, InventorySystem.IsEquipped(f.Actor, screen));
            }
        }

        [Test]
        public void ScreenReplacesHandEquipmentAndLosesProtectionWhenReplaced()
        {
            using (var f = new EquipmentLifecycleFixture())
            {
                var screen = f.Item("GroundwireScreen"); Assert.NotNull(screen);
                var shield = f.Item("Buckler");
                Assert.IsTrue(f.Inventory.AddObject(screen)); Assert.IsTrue(f.Inventory.AddObject(shield));
                Assert.IsTrue(InventorySystem.Equip(f.Actor, shield, f.LeftHand));
                Assert.IsTrue(InventorySystem.Equip(f.Actor, screen, f.LeftHand));
                Assert.IsFalse(InventorySystem.IsEquipped(f.Actor, shield));
                Assert.AreEqual(50, f.Actor.GetStatValue("ElectricResistance"));
                Assert.IsTrue(InventorySystem.Equip(f.Actor, shield, f.LeftHand));
                Assert.IsFalse(InventorySystem.IsEquipped(f.Actor, screen));
                Assert.AreEqual(0, f.Actor.GetStatValue("ElectricResistance"));
            }
        }
    }
}
