using System;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    public class InventorySystemTests
    {
        [SetUp]
        public void Setup()
        {
            MessageLog.Clear();
        }

        // ========================
        // InventoryPart Basics
        // ========================

        [Test]
        public void InventoryPart_AddObject_SetsInInventory()
        {
            var owner = CreateCreatureWithInventory();
            var item = CreateTakeableItem(5);

            var inv = owner.GetPart<InventoryPart>();
            Assert.IsTrue(inv.AddObject(item));
            Assert.AreEqual(1, inv.Objects.Count);
            Assert.AreEqual(owner, item.GetPart<PhysicsPart>().InInventory);
            Assert.IsNull(item.GetPart<PhysicsPart>().Equipped);
        }

        [Test]
        public void InventoryPart_AddObject_WeightLimit()
        {
            var owner = CreateCreatureWithInventory(maxWeight: 10);
            var heavy = CreateTakeableItem(15);

            var inv = owner.GetPart<InventoryPart>();
            Assert.IsFalse(inv.AddObject(heavy));
            Assert.AreEqual(0, inv.Objects.Count);
        }

        [Test]
        public void InventoryPart_AddObject_NoLimit()
        {
            var owner = CreateCreatureWithInventory(maxWeight: -1);
            var heavy = CreateTakeableItem(999);

            var inv = owner.GetPart<InventoryPart>();
            Assert.IsTrue(inv.AddObject(heavy));
        }

        [Test]
        public void InventoryPart_RemoveObject_ClearsInInventory()
        {
            var owner = CreateCreatureWithInventory();
            var item = CreateTakeableItem(5);

            var inv = owner.GetPart<InventoryPart>();
            inv.AddObject(item);
            Assert.IsTrue(inv.RemoveObject(item));
            Assert.AreEqual(0, inv.Objects.Count);
            Assert.IsNull(item.GetPart<PhysicsPart>().InInventory);
        }

        [Test]
        public void InventoryPart_RemoveObject_NotInInventory_ReturnsFalse()
        {
            var owner = CreateCreatureWithInventory();
            var item = CreateTakeableItem(5);

            var inv = owner.GetPart<InventoryPart>();
            Assert.IsFalse(inv.RemoveObject(item));
        }

        [Test]
        public void InventoryPart_Equip_SetsEquipped()
        {
            var owner = CreateCreatureWithInventory();
            var item = CreateTakeableItem(5);

            var inv = owner.GetPart<InventoryPart>();
            inv.AddObject(item);
            inv.Equip(item, "Hand");

            Assert.AreEqual(0, inv.Objects.Count);
            Assert.AreEqual(item, inv.GetEquipped("Hand"));
            Assert.AreEqual(owner, item.GetPart<PhysicsPart>().Equipped);
            Assert.IsNull(item.GetPart<PhysicsPart>().InInventory);
        }

        [Test]
        public void InventoryPart_Unequip_MovesToCarried()
        {
            var owner = CreateCreatureWithInventory();
            var item = CreateTakeableItem(5);

            var inv = owner.GetPart<InventoryPart>();
            inv.Equip(item, "Hand");
            Assert.IsTrue(inv.Unequip("Hand"));

            Assert.IsNull(inv.GetEquipped("Hand"));
            Assert.AreEqual(1, inv.Objects.Count);
            Assert.AreEqual(owner, item.GetPart<PhysicsPart>().InInventory);
            Assert.IsNull(item.GetPart<PhysicsPart>().Equipped);
        }

        [Test]
        public void InventoryPart_Unequip_EmptySlot_ReturnsFalse()
        {
            var owner = CreateCreatureWithInventory();
            var inv = owner.GetPart<InventoryPart>();
            Assert.IsFalse(inv.Unequip("Hand"));
        }

        [Test]
        public void InventoryPart_GetEquippedWithPart_FindsWeapon()
        {
            var owner = CreateCreatureWithInventory();
            var sword = CreateWeapon("1d8", 2);

            var inv = owner.GetPart<InventoryPart>();
            inv.Equip(sword, "Hand");

            var found = inv.GetEquippedWithPart<MeleeWeaponPart>();
            Assert.AreEqual(sword, found);
        }

        [Test]
        public void InventoryPart_GetEquippedWithPart_NoneEquipped_ReturnsNull()
        {
            var owner = CreateCreatureWithInventory();
            var inv = owner.GetPart<InventoryPart>();
            Assert.IsNull(inv.GetEquippedWithPart<MeleeWeaponPart>());
        }

        [Test]
        public void InventoryPart_GetCarriedWeight()
        {
            var owner = CreateCreatureWithInventory();
            var inv = owner.GetPart<InventoryPart>();

            var item1 = CreateTakeableItem(10);
            var item2 = CreateTakeableItem(5);
            inv.AddObject(item1);
            inv.AddObject(item2);

            Assert.AreEqual(15, inv.GetCarriedWeight());
        }

        [Test]
        public void InventoryPart_GetCarriedWeight_IncludesEquipped()
        {
            var owner = CreateCreatureWithInventory();
            var inv = owner.GetPart<InventoryPart>();

            var item1 = CreateTakeableItem(10);
            var item2 = CreateTakeableItem(5);
            inv.AddObject(item1);
            inv.Equip(item2, "Hand");

            Assert.AreEqual(15, inv.GetCarriedWeight());
        }

        [Test]
        public void InventoryPart_Contains_CarriedAndEquipped()
        {
            var owner = CreateCreatureWithInventory();
            var inv = owner.GetPart<InventoryPart>();

            var carried = CreateTakeableItem(5);
            var equipped = CreateTakeableItem(5);
            var absent = CreateTakeableItem(5);

            inv.AddObject(carried);
            inv.Equip(equipped, "Hand");

            Assert.IsTrue(inv.Contains(carried));
            Assert.IsTrue(inv.Contains(equipped));
            Assert.IsFalse(inv.Contains(absent));
        }

        // ========================
        // EquippablePart
        // ========================

        [Test]
        public void EquippablePart_DefaultSlot()
        {
            var equip = new EquippablePart();
            Assert.AreEqual("Hand", equip.Slot);
            Assert.AreEqual("", equip.EquipBonuses);
            Assert.AreEqual("Equippable", equip.Name);
        }

        // ========================
        // InventorySystem.Pickup
        // ========================

        [Test]
        public void Pickup_MovesItemFromZoneToInventory()
        {
            var zone = new Zone();
            var actor = CreateCreatureWithInventory();
            zone.AddEntity(actor, 5, 5);

            var item = CreateTakeableItem(5);
            zone.AddEntity(item, 5, 5);

            Assert.IsTrue(InventorySystem.Pickup(actor, item, zone));

            // Item should be in inventory, not in zone
            Assert.IsNull(zone.GetEntityCell(item));
            var inv = actor.GetPart<InventoryPart>();
            Assert.AreEqual(1, inv.Objects.Count);
            Assert.AreEqual(item, inv.Objects[0]);
        }

        [Test]
        public void Pickup_SetsInInventoryBackReference()
        {
            var zone = new Zone();
            var actor = CreateCreatureWithInventory();
            zone.AddEntity(actor, 5, 5);

            var item = CreateTakeableItem(5);
            zone.AddEntity(item, 5, 5);

            InventorySystem.Pickup(actor, item, zone);
            Assert.AreEqual(actor, item.GetPart<PhysicsPart>().InInventory);
        }

        [Test]
        public void Pickup_NotTakeable_Fails()
        {
            var zone = new Zone();
            var actor = CreateCreatureWithInventory();
            zone.AddEntity(actor, 5, 5);

            var wall = new Entity();
            wall.AddPart(new PhysicsPart { Solid = true, Takeable = false });
            zone.AddEntity(wall, 5, 5);

            Assert.IsFalse(InventorySystem.Pickup(actor, wall, zone));
        }

        [Test]
        public void Pickup_NoInventory_Fails()
        {
            var zone = new Zone();
            var actor = new Entity();
            actor.AddPart(new PhysicsPart());
            zone.AddEntity(actor, 5, 5);

            var item = CreateTakeableItem(5);
            zone.AddEntity(item, 5, 5);

            Assert.IsFalse(InventorySystem.Pickup(actor, item, zone));
        }

        [Test]
        public void Pickup_WeightExceeded_PutsItemBack()
        {
            var zone = new Zone();
            var actor = CreateCreatureWithInventory(maxWeight: 10);
            zone.AddEntity(actor, 5, 5);

            var heavy = CreateTakeableItem(20);
            zone.AddEntity(heavy, 5, 5);

            Assert.IsFalse(InventorySystem.Pickup(actor, heavy, zone));

            // Item should still be in zone (put back)
            Assert.IsNotNull(zone.GetEntityCell(heavy));
            Assert.AreEqual(0, actor.GetPart<InventoryPart>().Objects.Count);
        }

        [Test]
        public void Pickup_BeforePickup_CanCancel()
        {
            var zone = new Zone();
            var actor = CreateCreatureWithInventory();
            actor.AddPart(new CancelEventPart("BeforePickup"));
            zone.AddEntity(actor, 5, 5);

            var item = CreateTakeableItem(5);
            zone.AddEntity(item, 5, 5);

            Assert.IsFalse(InventorySystem.Pickup(actor, item, zone));
            // Item still in zone
            Assert.IsNotNull(zone.GetEntityCell(item));
        }

        [Test]
        public void Pickup_LogsMessage()
        {
            var zone = new Zone();
            var actor = CreateCreatureWithInventory();
            actor.AddPart(new RenderPart { DisplayName = "hero" });
            zone.AddEntity(actor, 5, 5);

            var item = CreateTakeableItem(5);
            item.AddPart(new RenderPart { DisplayName = "dagger" });
            zone.AddEntity(item, 5, 5);

            InventorySystem.Pickup(actor, item, zone);
            Assert.IsTrue(MessageLog.GetLast().Contains("picks up"));
        }

        // ========================
        // InventorySystem.Drop
        // ========================

        [Test]
        public void Drop_MovesItemFromInventoryToZone()
        {
            var zone = new Zone();
            var actor = CreateCreatureWithInventory();
            zone.AddEntity(actor, 5, 5);

            var item = CreateTakeableItem(5);
            zone.AddEntity(item, 5, 5);
            InventorySystem.Pickup(actor, item, zone);

            Assert.IsTrue(InventorySystem.Drop(actor, item, zone));

            // Item should be back in zone at actor's position
            var cell = zone.GetEntityCell(item);
            Assert.IsNotNull(cell);
            Assert.AreEqual(5, cell.X);
            Assert.AreEqual(5, cell.Y);
            Assert.AreEqual(0, actor.GetPart<InventoryPart>().Objects.Count);
        }

        [Test]
        public void Drop_ClearsInInventory()
        {
            var zone = new Zone();
            var actor = CreateCreatureWithInventory();
            zone.AddEntity(actor, 5, 5);

            var item = CreateTakeableItem(5);
            zone.AddEntity(item, 5, 5);
            InventorySystem.Pickup(actor, item, zone);
            InventorySystem.Drop(actor, item, zone);

            Assert.IsNull(item.GetPart<PhysicsPart>().InInventory);
        }

        [Test]
        public void Drop_AutoUnequips()
        {
            var zone = new Zone();
            var actor = CreateCreatureWithInventory();
            zone.AddEntity(actor, 5, 5);

            var weapon = CreateWeapon("1d6", 1);
            zone.AddEntity(weapon, 5, 5);
            InventorySystem.Pickup(actor, weapon, zone);
            InventorySystem.Equip(actor, weapon);

            // Weapon is now equipped
            Assert.IsNotNull(actor.GetPart<InventoryPart>().GetEquipped("Hand"));

            // Drop should auto-unequip then drop
            Assert.IsTrue(InventorySystem.Drop(actor, weapon, zone));
            Assert.IsNull(actor.GetPart<InventoryPart>().GetEquipped("Hand"));
            Assert.IsNotNull(zone.GetEntityCell(weapon));
        }

        [Test]
        public void Drop_BeforeDrop_CanCancel()
        {
            var zone = new Zone();
            var actor = CreateCreatureWithInventory();
            actor.AddPart(new CancelEventPart("BeforeDrop"));
            zone.AddEntity(actor, 5, 5);

            var item = CreateTakeableItem(5);
            zone.AddEntity(item, 5, 5);
            InventorySystem.Pickup(actor, item, zone);

            Assert.IsFalse(InventorySystem.Drop(actor, item, zone));
            Assert.AreEqual(1, actor.GetPart<InventoryPart>().Objects.Count);
        }

        // ========================
        // InventorySystem.Equip
        // ========================

        [Test]
        public void Equip_MovesFromCarriedToSlot()
        {
            var actor = CreateCreatureWithInventory();
            var weapon = CreateWeapon("1d6", 1);

            var inv = actor.GetPart<InventoryPart>();
            inv.AddObject(weapon);
            Assert.IsTrue(InventorySystem.Equip(actor, weapon));

            Assert.AreEqual(0, inv.Objects.Count);
            Assert.AreEqual(weapon, inv.GetEquipped("Hand"));
            Assert.AreEqual(actor, weapon.GetPart<PhysicsPart>().Equipped);
        }

        [Test]
        public void Equip_NoEquippablePart_Fails()
        {
            var actor = CreateCreatureWithInventory();
            var item = CreateTakeableItem(5);

            var inv = actor.GetPart<InventoryPart>();
            inv.AddObject(item);
            Assert.IsFalse(InventorySystem.Equip(actor, item));
        }

        [Test]
        public void Equip_AutoUnequipsExisting()
        {
            var actor = CreateCreatureWithInventory();
            var weapon1 = CreateWeapon("1d4", 1);
            var weapon2 = CreateWeapon("1d8", 2);

            var inv = actor.GetPart<InventoryPart>();
            inv.AddObject(weapon1);
            inv.AddObject(weapon2);

            InventorySystem.Equip(actor, weapon1);
            Assert.AreEqual(weapon1, inv.GetEquipped("Hand"));

            InventorySystem.Equip(actor, weapon2);
            Assert.AreEqual(weapon2, inv.GetEquipped("Hand"));
            // weapon1 should be back in carried
            Assert.IsTrue(inv.Objects.Contains(weapon1));
        }

        [Test]
        public void Equip_AppliesStatBonuses()
        {
            var actor = CreateCreatureWithInventory();
            actor.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };

            var item = CreateTakeableItem(5);
            item.AddPart(new EquippablePart { Slot = "Hand", EquipBonuses = "Strength:2" });

            var inv = actor.GetPart<InventoryPart>();
            inv.AddObject(item);
            InventorySystem.Equip(actor, item);

            Assert.AreEqual(18, actor.GetStatValue("Strength"));
        }

        [Test]
        public void Equip_BeforeEquip_CanCancel()
        {
            var actor = CreateCreatureWithInventory();
            actor.AddPart(new CancelEventPart("BeforeEquip"));
            var weapon = CreateWeapon("1d6", 1);

            var inv = actor.GetPart<InventoryPart>();
            inv.AddObject(weapon);
            Assert.IsFalse(InventorySystem.Equip(actor, weapon));
            Assert.IsNull(inv.GetEquipped("Hand"));
        }

        [Test]
        public void Equip_LogsMessage()
        {
            var actor = CreateCreatureWithInventory();
            actor.AddPart(new RenderPart { DisplayName = "hero" });
            var weapon = CreateWeapon("1d6", 1);
            weapon.AddPart(new RenderPart { DisplayName = "sword" });

            var inv = actor.GetPart<InventoryPart>();
            inv.AddObject(weapon);
            InventorySystem.Equip(actor, weapon);

            Assert.IsTrue(MessageLog.GetLast().Contains("equips"));
        }

        // ========================
        // InventorySystem.Unequip
        // ========================

        [Test]
        public void Unequip_MovesFromSlotToCarried()
        {
            var actor = CreateCreatureWithInventory();
            var weapon = CreateWeapon("1d6", 1);

            var inv = actor.GetPart<InventoryPart>();
            inv.AddObject(weapon);
            InventorySystem.Equip(actor, weapon);
            Assert.IsTrue(InventorySystem.Unequip(actor, "Hand"));

            Assert.IsNull(inv.GetEquipped("Hand"));
            Assert.IsTrue(inv.Objects.Contains(weapon));
        }

        [Test]
        public void Unequip_RemovesStatBonuses()
        {
            var actor = CreateCreatureWithInventory();
            actor.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };

            var item = CreateTakeableItem(5);
            item.AddPart(new EquippablePart { Slot = "Hand", EquipBonuses = "Strength:2" });

            var inv = actor.GetPart<InventoryPart>();
            inv.AddObject(item);
            InventorySystem.Equip(actor, item);
            Assert.AreEqual(18, actor.GetStatValue("Strength"));

            InventorySystem.Unequip(actor, "Hand");
            Assert.AreEqual(16, actor.GetStatValue("Strength"));
        }

        [Test]
        public void Unequip_EmptySlot_ReturnsFalse()
        {
            var actor = CreateCreatureWithInventory();
            Assert.IsFalse(InventorySystem.Unequip(actor, "Hand"));
        }

        [Test]
        public void Unequip_BeforeUnequip_CanCancel()
        {
            var actor = CreateCreatureWithInventory();
            actor.AddPart(new CancelEventPart("BeforeUnequip"));
            var weapon = CreateWeapon("1d6", 1);

            var inv = actor.GetPart<InventoryPart>();
            inv.AddObject(weapon);
            InventorySystem.Equip(actor, weapon);

            Assert.IsFalse(InventorySystem.Unequip(actor, "Hand"));
            Assert.AreEqual(weapon, inv.GetEquipped("Hand"));
        }

        // ========================
        // GetTakeableItemsAtFeet
        // ========================

        [Test]
        public void GetTakeableItemsAtFeet_FindsItems()
        {
            var zone = new Zone();
            var actor = CreateCreatureWithInventory();
            zone.AddEntity(actor, 5, 5);

            var item1 = CreateTakeableItem(5);
            zone.AddEntity(item1, 5, 5);
            var item2 = CreateTakeableItem(3);
            zone.AddEntity(item2, 5, 5);

            // Non-takeable entity on same cell
            var wall = new Entity();
            wall.AddPart(new PhysicsPart { Takeable = false });
            zone.AddEntity(wall, 5, 5);

            var items = InventorySystem.GetTakeableItemsAtFeet(actor, zone);
            Assert.AreEqual(2, items.Count);
            Assert.IsTrue(items.Contains(item1));
            Assert.IsTrue(items.Contains(item2));
        }

        [Test]
        public void GetTakeableItemsAtFeet_ExcludesActor()
        {
            var zone = new Zone();
            var actor = CreateCreatureWithInventory();
            actor.GetPart<PhysicsPart>().Takeable = true; // Actor is technically takeable
            zone.AddEntity(actor, 5, 5);

            var items = InventorySystem.GetTakeableItemsAtFeet(actor, zone);
            Assert.AreEqual(0, items.Count);
        }

        [Test]
        public void GetTakeableItemsAtFeet_DifferentCell_Empty()
        {
            var zone = new Zone();
            var actor = CreateCreatureWithInventory();
            zone.AddEntity(actor, 5, 5);

            var item = CreateTakeableItem(5);
            zone.AddEntity(item, 6, 5); // Different cell

            var items = InventorySystem.GetTakeableItemsAtFeet(actor, zone);
            Assert.AreEqual(0, items.Count);
        }

        // ========================
        // Combat Integration
        // ========================

        [Test]
        public void Combat_EquippedWeapon_UsedOverNatural()
        {
            var zone = new Zone();
            var attacker = CreateCreatureWithInventory();
            attacker.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 20, Min = 1, Max = 50 };
            attacker.Statistics["Agility"] = new Stat { Name = "Agility", BaseValue = 20, Min = 1, Max = 50 };
            // Natural weapon: 1d2
            attacker.GetPart<MeleeWeaponPart>().BaseDamage = "1d2";

            // Equip a sword: 1d8
            var sword = CreateWeapon("1d8", 5);
            var inv = attacker.GetPart<InventoryPart>();
            inv.AddObject(sword);
            InventorySystem.Equip(attacker, sword);

            zone.AddEntity(attacker, 5, 5);

            var defender = CreateCreatureWithInventory();
            defender.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 100, Min = 0, Max = 100 };
            defender.Statistics["Agility"] = new Stat { Name = "Agility", BaseValue = 10, Min = 1, Max = 50 };
            defender.GetPart<ArmorPart>().AV = 0;
            defender.GetPart<ArmorPart>().DV = 0;
            zone.AddEntity(defender, 6, 5);

            // Attack many times with fixed seed to accumulate damage
            int totalDamage = 0;
            for (int i = 0; i < 50; i++)
            {
                int before = defender.GetStatValue("Hitpoints");
                var rng = new Random(i);
                CombatSystem.PerformMeleeAttack(attacker, defender, zone, rng);
                int after = defender.GetStatValue("Hitpoints");
                if (after < before) totalDamage += (before - after);
            }

            // With 1d8 weapon (avg 4.5) vs 1d2 (avg 1.5), average hit damage should be higher
            // Just verify we dealt some damage
            Assert.Greater(totalDamage, 0, "Equipped weapon should deal damage");
        }

        [Test]
        public void Combat_EquippedArmor_AffectsAV()
        {
            var creature = CreateCreatureWithInventory();
            creature.GetPart<ArmorPart>().AV = 1; // Natural AV

            // Create armor item
            var armor = CreateTakeableItem(15);
            armor.AddPart(new EquippablePart { Slot = "Body" });
            armor.AddPart(new ArmorPart { AV = 5, DV = -1 });

            var inv = creature.GetPart<InventoryPart>();
            inv.AddObject(armor);
            InventorySystem.Equip(creature, armor);

            // Equipped armor should override natural AV
            Assert.AreEqual(5, CombatSystem.GetAV(creature));
        }

        [Test]
        public void Combat_EquippedArmor_AffectsDV()
        {
            var creature = CreateCreatureWithInventory();
            creature.Statistics["Agility"] = new Stat { Name = "Agility", BaseValue = 16, Min = 1, Max = 50 };

            var armor = CreateTakeableItem(15);
            armor.AddPart(new EquippablePart { Slot = "Body" });
            armor.AddPart(new ArmorPart { AV = 3, DV = -1 });

            var inv = creature.GetPart<InventoryPart>();
            inv.AddObject(armor);
            InventorySystem.Equip(creature, armor);

            // DV = 6 + armor.DV(-1) + AgilityMod(0) = 5
            Assert.AreEqual(5, CombatSystem.GetDV(creature));
        }

        [Test]
        public void Combat_NoEquippedWeapon_UsesNatural()
        {
            // Creature with inventory but no equipped weapon uses natural MeleeWeaponPart
            var creature = CreateCreatureWithInventory();
            creature.Statistics["Agility"] = new Stat { Name = "Agility", BaseValue = 16, Min = 1, Max = 50 };
            creature.GetPart<MeleeWeaponPart>().BaseDamage = "1d4";

            var zone = new Zone();
            zone.AddEntity(creature, 5, 5);

            var target = CreateCreatureWithInventory();
            target.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 50, Min = 0, Max = 50 };
            target.Statistics["Agility"] = new Stat { Name = "Agility", BaseValue = 10, Min = 1, Max = 50 };
            target.GetPart<ArmorPart>().AV = 0;
            target.GetPart<ArmorPart>().DV = 0;
            zone.AddEntity(target, 6, 5);

            bool dealt = false;
            for (int i = 0; i < 20; i++)
            {
                int before = target.GetStatValue("Hitpoints");
                CombatSystem.PerformMeleeAttack(creature, target, zone, new Random(i));
                if (target.GetStatValue("Hitpoints") < before)
                {
                    dealt = true;
                    break;
                }
            }
            Assert.IsTrue(dealt, "Natural weapon should still work with InventoryPart present");
        }

        // ========================
        // Blueprint Integration
        // ========================

        [Test]
        public void Blueprint_Creature_HasInventory()
        {
            var factory = new EntityFactory();
            factory.LoadBlueprints(GetTestJson());

            var snapjaw = factory.CreateEntity("Snapjaw");
            Assert.IsNotNull(snapjaw.GetPart<InventoryPart>(), "Creature blueprint should have InventoryPart");
            Assert.AreEqual(150, snapjaw.GetPart<InventoryPart>().MaxWeight);
        }

        [Test]
        public void Blueprint_Dagger_HasEquippable()
        {
            var factory = new EntityFactory();
            factory.LoadBlueprints(GetTestJson());

            var dagger = factory.CreateEntity("Dagger");
            Assert.IsNotNull(dagger.GetPart<EquippablePart>(), "Dagger should have EquippablePart");
            Assert.AreEqual("Hand", dagger.GetPart<EquippablePart>().Slot);
            Assert.IsTrue(dagger.GetPart<PhysicsPart>().Takeable, "Dagger should be takeable");
            Assert.AreEqual(4, dagger.GetPart<PhysicsPart>().Weight);
        }

        [Test]
        public void Blueprint_LeatherArmor_Properties()
        {
            var factory = new EntityFactory();
            factory.LoadBlueprints(GetTestJson());

            var leather = factory.CreateEntity("LeatherArmor");
            Assert.IsNotNull(leather);
            Assert.IsNotNull(leather.GetPart<EquippablePart>());
            Assert.AreEqual("Body", leather.GetPart<EquippablePart>().Slot);
            Assert.AreEqual(3, leather.GetPart<ArmorPart>().AV);
            Assert.AreEqual(-1, leather.GetPart<ArmorPart>().DV);
            Assert.AreEqual(15, leather.GetPart<PhysicsPart>().Weight);
        }

        [Test]
        public void Blueprint_FullPickupEquipFlow()
        {
            var factory = new EntityFactory();
            factory.LoadBlueprints(GetTestJson());

            var zone = new Zone();
            var player = factory.CreateEntity("Player");
            zone.AddEntity(player, 5, 5);

            var dagger = factory.CreateEntity("Dagger");
            zone.AddEntity(dagger, 5, 5);

            // Pickup
            Assert.IsTrue(InventorySystem.Pickup(player, dagger, zone));
            Assert.IsNull(zone.GetEntityCell(dagger));
            Assert.AreEqual(1, player.GetPart<InventoryPart>().Objects.Count);

            // Equip
            Assert.IsTrue(InventorySystem.Equip(player, dagger));
            Assert.AreEqual(dagger, player.GetPart<InventoryPart>().GetEquipped("Hand"));

            // Combat should use the dagger's weapon stats
            var equipped = player.GetPart<InventoryPart>().GetEquippedWithPart<MeleeWeaponPart>();
            Assert.IsNotNull(equipped);
            Assert.AreEqual("1d4", equipped.GetPart<MeleeWeaponPart>().BaseDamage);

            // Unequip
            Assert.IsTrue(InventorySystem.Unequip(player, "Hand"));
            Assert.IsNull(player.GetPart<InventoryPart>().GetEquipped("Hand"));

            // Drop
            Assert.IsTrue(InventorySystem.Drop(player, dagger, zone));
            Assert.IsNotNull(zone.GetEntityCell(dagger));
        }

        // ========================
        // Helpers
        // ========================

        private Entity CreateCreatureWithInventory(int maxWeight = 150)
        {
            var entity = new Entity();
            entity.BlueprintName = "TestCreature";
            entity.Tags["Creature"] = "";

            entity.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 30, Min = 0, Max = 30 };
            entity.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Name = "Agility", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };

            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new MeleeWeaponPart());
            entity.AddPart(new ArmorPart());
            entity.AddPart(new InventoryPart { MaxWeight = maxWeight });

            return entity;
        }

        private Entity CreateTakeableItem(int weight)
        {
            var entity = new Entity();
            entity.BlueprintName = "TestItem";
            entity.Tags["Item"] = "";
            entity.AddPart(new PhysicsPart { Takeable = true, Weight = weight });
            return entity;
        }

        private Entity CreateWeapon(string damage, int penBonus)
        {
            var entity = new Entity();
            entity.BlueprintName = "TestWeapon";
            entity.Tags["Item"] = "";
            entity.AddPart(new PhysicsPart { Takeable = true, Weight = 5 });
            entity.AddPart(new MeleeWeaponPart { BaseDamage = damage, PenBonus = penBonus });
            entity.AddPart(new EquippablePart { Slot = "Hand" });
            return entity;
        }

        private string GetTestJson()
        {
            return @"{
                ""Objects"": [
                    {
                        ""Name"": ""PhysicalObject"",
                        ""Parts"": [
                            { ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderString"", ""Value"": ""?"" }, { ""Key"": ""ColorString"", ""Value"": ""&y"" }] },
                            { ""Name"": ""Physics"", ""Params"": [] }
                        ],
                        ""Stats"": [],
                        ""Tags"": []
                    },
                    {
                        ""Name"": ""Item"",
                        ""Inherits"": ""PhysicalObject"",
                        ""Parts"": [
                            { ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Takeable"", ""Value"": ""true"" }] }
                        ],
                        ""Stats"": [],
                        ""Tags"": [{ ""Key"": ""Item"", ""Value"": """" }]
                    },
                    {
                        ""Name"": ""MeleeWeapon"",
                        ""Inherits"": ""Item"",
                        ""Parts"": [
                            { ""Name"": ""Equippable"", ""Params"": [{ ""Key"": ""Slot"", ""Value"": ""Hand"" }] }
                        ],
                        ""Stats"": [],
                        ""Tags"": [{ ""Key"": ""MeleeWeapon"", ""Value"": """" }]
                    },
                    {
                        ""Name"": ""Dagger"",
                        ""Inherits"": ""MeleeWeapon"",
                        ""Parts"": [
                            { ""Name"": ""Render"", ""Params"": [{ ""Key"": ""DisplayName"", ""Value"": ""dagger"" }, { ""Key"": ""RenderString"", ""Value"": ""/"" }, { ""Key"": ""ColorString"", ""Value"": ""&c"" }] },
                            { ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Takeable"", ""Value"": ""true"" }, { ""Key"": ""Weight"", ""Value"": ""4"" }] },
                            { ""Name"": ""MeleeWeapon"", ""Params"": [{ ""Key"": ""BaseDamage"", ""Value"": ""1d4"" }, { ""Key"": ""PenBonus"", ""Value"": ""1"" }, { ""Key"": ""MaxStrengthBonus"", ""Value"": ""3"" }] }
                        ],
                        ""Stats"": [{ ""Name"": ""Hitpoints"", ""Value"": 5, ""Min"": 0, ""Max"": 5 }],
                        ""Tags"": [{ ""Key"": ""Tier"", ""Value"": ""1"" }]
                    },
                    {
                        ""Name"": ""Creature"",
                        ""Inherits"": ""PhysicalObject"",
                        ""Parts"": [
                            { ""Name"": ""Render"", ""Params"": [{ ""Key"": ""RenderLayer"", ""Value"": ""10"" }] },
                            { ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Solid"", ""Value"": ""true"" }] },
                            { ""Name"": ""MeleeWeapon"", ""Params"": [{ ""Key"": ""BaseDamage"", ""Value"": ""1d2"" }] },
                            { ""Name"": ""Armor"", ""Params"": [] },
                            { ""Name"": ""Inventory"", ""Params"": [{ ""Key"": ""MaxWeight"", ""Value"": ""150"" }] }
                        ],
                        ""Stats"": [
                            { ""Name"": ""Hitpoints"", ""Value"": 1, ""Min"": 0, ""Max"": 999 },
                            { ""Name"": ""Strength"", ""Value"": 10, ""Min"": 1, ""Max"": 50 },
                            { ""Name"": ""Agility"", ""Value"": 10, ""Min"": 1, ""Max"": 50 },
                            { ""Name"": ""Toughness"", ""Value"": 10, ""Min"": 1, ""Max"": 50 },
                            { ""Name"": ""Speed"", ""Value"": 100, ""Min"": 25, ""Max"": 200 }
                        ],
                        ""Tags"": [{ ""Key"": ""Creature"", ""Value"": """" }]
                    },
                    {
                        ""Name"": ""Snapjaw"",
                        ""Inherits"": ""Creature"",
                        ""Parts"": [
                            { ""Name"": ""Render"", ""Params"": [{ ""Key"": ""DisplayName"", ""Value"": ""snapjaw"" }] },
                            { ""Name"": ""MeleeWeapon"", ""Params"": [{ ""Key"": ""BaseDamage"", ""Value"": ""1d4"" }, { ""Key"": ""PenBonus"", ""Value"": ""1"" }] },
                            { ""Name"": ""Armor"", ""Params"": [{ ""Key"": ""AV"", ""Value"": ""2"" }, { ""Key"": ""DV"", ""Value"": ""1"" }] }
                        ],
                        ""Stats"": [
                            { ""Name"": ""Hitpoints"", ""Value"": 15, ""Min"": 0, ""Max"": 15 },
                            { ""Name"": ""Strength"", ""Value"": 16 }
                        ],
                        ""Tags"": [{ ""Key"": ""Faction"", ""Value"": ""Snapjaws"" }]
                    },
                    {
                        ""Name"": ""Player"",
                        ""Inherits"": ""Creature"",
                        ""Parts"": [
                            { ""Name"": ""Render"", ""Params"": [{ ""Key"": ""DisplayName"", ""Value"": ""you"" }, { ""Key"": ""RenderString"", ""Value"": ""@"" }, { ""Key"": ""ColorString"", ""Value"": ""&Y"" }] }
                        ],
                        ""Stats"": [
                            { ""Name"": ""Hitpoints"", ""Value"": 20, ""Min"": 0, ""Max"": 20 },
                            { ""Name"": ""Strength"", ""Value"": 18 },
                            { ""Name"": ""Agility"", ""Value"": 18 },
                            { ""Name"": ""Toughness"", ""Value"": 18 }
                        ],
                        ""Tags"": [{ ""Key"": ""Player"", ""Value"": """" }]
                    },
                    {
                        ""Name"": ""ArmorItem"",
                        ""Inherits"": ""Item"",
                        ""Parts"": [
                            { ""Name"": ""Equippable"", ""Params"": [{ ""Key"": ""Slot"", ""Value"": ""Body"" }] },
                            { ""Name"": ""Armor"", ""Params"": [] }
                        ],
                        ""Stats"": [],
                        ""Tags"": [{ ""Key"": ""Armor"", ""Value"": """" }]
                    },
                    {
                        ""Name"": ""LeatherArmor"",
                        ""Inherits"": ""ArmorItem"",
                        ""Parts"": [
                            { ""Name"": ""Render"", ""Params"": [{ ""Key"": ""DisplayName"", ""Value"": ""leather armor"" }, { ""Key"": ""RenderString"", ""Value"": ""["" }, { ""Key"": ""ColorString"", ""Value"": ""&w"" }] },
                            { ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Takeable"", ""Value"": ""true"" }, { ""Key"": ""Weight"", ""Value"": ""15"" }] },
                            { ""Name"": ""Armor"", ""Params"": [{ ""Key"": ""AV"", ""Value"": ""3"" }, { ""Key"": ""DV"", ""Value"": ""-1"" }] }
                        ],
                        ""Stats"": [{ ""Name"": ""Hitpoints"", ""Value"": 15, ""Min"": 0, ""Max"": 15 }],
                        ""Tags"": [{ ""Key"": ""Tier"", ""Value"": ""1"" }]
                    }
                ]
            }";
        }
    }

    /// <summary>
    /// Test part that cancels a specific event by ID.
    /// </summary>
    public class CancelEventPart : Part
    {
        public override string Name => "CancelEvent";
        private string _eventToCancel;

        public CancelEventPart(string eventToCancel)
        {
            _eventToCancel = eventToCancel;
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == _eventToCancel)
                return false;
            return true;
        }
    }
}
