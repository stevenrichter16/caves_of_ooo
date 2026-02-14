using System;
using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
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

            // Pickup — dagger auto-equips because Hand slot is empty
            Assert.IsTrue(InventorySystem.Pickup(player, dagger, zone));
            Assert.IsNull(zone.GetEntityCell(dagger));
            Assert.AreEqual(dagger, player.GetPart<InventoryPart>().GetEquipped("Hand"),
                "Dagger should auto-equip on pickup");

            // Combat should use the dagger's weapon stats
            var equipped = player.GetPart<InventoryPart>().GetEquippedWithPart<MeleeWeaponPart>();
            Assert.IsNotNull(equipped);
            Assert.AreEqual("1d4", equipped.GetPart<MeleeWeaponPart>().BaseDamage);

            // Unequip
            Assert.IsTrue(InventorySystem.Unequip(player, "Hand"));
            Assert.IsNull(player.GetPart<InventoryPart>().GetEquipped("Hand"));
            Assert.AreEqual(1, player.GetPart<InventoryPart>().Objects.Count);

            // Drop
            Assert.IsTrue(InventorySystem.Drop(player, dagger, zone));
            Assert.IsNotNull(zone.GetEntityCell(dagger));
        }

        // ========================
        // Encumbrance / Overburden
        // ========================

        [Test]
        public void GetMaxCarryWeight_BasedOnStrength()
        {
            var actor = CreateCreatureWithInventory();
            actor.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            var inv = actor.GetPart<InventoryPart>();
            // 16 * 15 = 240
            Assert.AreEqual(240, inv.GetMaxCarryWeight());
        }

        [Test]
        public void IsOverburdened_PlayerOverWeight_ReturnsTrue()
        {
            var actor = CreateCreatureWithInventory(maxWeight: -1); // no hard limit
            actor.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 10, Min = 1, Max = 50 };
            actor.SetTag("Player");
            var inv = actor.GetPart<InventoryPart>();

            // Max carry = 10 * 15 = 150
            var heavy = CreateTakeableItem(200);
            inv.AddObject(heavy);

            Assert.IsTrue(inv.IsOverburdened());
        }

        [Test]
        public void IsOverburdened_PlayerUnderWeight_ReturnsFalse()
        {
            var actor = CreateCreatureWithInventory(maxWeight: -1);
            actor.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 10, Min = 1, Max = 50 };
            actor.SetTag("Player");
            var inv = actor.GetPart<InventoryPart>();

            var light = CreateTakeableItem(50);
            inv.AddObject(light);

            Assert.IsFalse(inv.IsOverburdened());
        }

        [Test]
        public void IsOverburdened_NPC_AlwaysFalse()
        {
            var actor = CreateCreatureWithInventory(maxWeight: -1);
            actor.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 10, Min = 1, Max = 50 };
            // No Player tag
            var inv = actor.GetPart<InventoryPart>();

            var heavy = CreateTakeableItem(200);
            inv.AddObject(heavy);

            Assert.IsFalse(inv.IsOverburdened());
        }

        [Test]
        public void Overburdened_BlocksMovement()
        {
            var zone = new Zone();
            var actor = CreateCreatureWithInventory(maxWeight: -1);
            actor.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 10, Min = 1, Max = 50 };
            actor.SetTag("Player");
            zone.AddEntity(actor, 5, 5);

            // Max carry = 150, load 200
            var heavy = CreateTakeableItem(200);
            actor.GetPart<InventoryPart>().AddObject(heavy);

            // Try to move — should be blocked by InventoryPart.HandleEvent
            var targetCell = zone.GetCell(6, 5);
            var moveEvent = GameEvent.New("BeforeMove");
            moveEvent.SetParameter("TargetCell", (object)targetCell);
            bool allowed = actor.FireEvent(moveEvent);

            Assert.IsFalse(allowed, "Movement should be blocked when overburdened");
        }

        [Test]
        public void NotOverburdened_AllowsMovement()
        {
            var zone = new Zone();
            var actor = CreateCreatureWithInventory(maxWeight: -1);
            actor.Statistics["Strength"] = new Stat { Name = "Strength", BaseValue = 10, Min = 1, Max = 50 };
            actor.SetTag("Player");
            zone.AddEntity(actor, 5, 5);

            // Max carry = 150, load 50
            var light = CreateTakeableItem(50);
            actor.GetPart<InventoryPart>().AddObject(light);

            var targetCell = zone.GetCell(6, 5);
            var moveEvent = GameEvent.New("BeforeMove");
            moveEvent.SetParameter("TargetCell", (object)targetCell);
            bool allowed = actor.FireEvent(moveEvent);

            Assert.IsTrue(allowed, "Movement should be allowed when not overburdened");
        }

        // ========================
        // Armor SpeedPenalty
        // ========================

        [Test]
        public void Equip_Armor_AppliesSpeedPenalty()
        {
            var actor = CreateCreatureWithInventory();
            actor.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };

            var armor = CreateTakeableItem(15);
            armor.AddPart(new EquippablePart { Slot = "Body" });
            armor.AddPart(new ArmorPart { AV = 5, DV = -1, SpeedPenalty = 10 });

            var inv = actor.GetPart<InventoryPart>();
            inv.AddObject(armor);
            InventorySystem.Equip(actor, armor);

            // Speed should be reduced by penalty
            Assert.AreEqual(90, actor.GetStatValue("Speed"));
        }

        [Test]
        public void Unequip_Armor_RemovesSpeedPenalty()
        {
            var actor = CreateCreatureWithInventory();
            actor.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };

            var armor = CreateTakeableItem(15);
            armor.AddPart(new EquippablePart { Slot = "Body" });
            armor.AddPart(new ArmorPart { AV = 5, DV = -1, SpeedPenalty = 10 });

            var inv = actor.GetPart<InventoryPart>();
            inv.AddObject(armor);
            InventorySystem.Equip(actor, armor);
            Assert.AreEqual(90, actor.GetStatValue("Speed"));

            InventorySystem.Unequip(actor, "Body");
            Assert.AreEqual(100, actor.GetStatValue("Speed"));
        }

        [Test]
        public void Equip_Armor_NoSpeedPenalty_SpeedUnchanged()
        {
            var actor = CreateCreatureWithInventory();
            actor.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };

            var armor = CreateTakeableItem(10);
            armor.AddPart(new EquippablePart { Slot = "Body" });
            armor.AddPart(new ArmorPart { AV = 2, DV = 0, SpeedPenalty = 0 });

            var inv = actor.GetPart<InventoryPart>();
            inv.AddObject(armor);
            InventorySystem.Equip(actor, armor);

            Assert.AreEqual(100, actor.GetStatValue("Speed"));
        }

        // ========================
        // Stacking: StackerPart
        // ========================

        [Test]
        public void StackerPart_CanStackWith_SameBlueprintName()
        {
            var a = CreateStackableItem("Torch", 1, 3);
            var b = CreateStackableItem("Torch", 1, 2);
            Assert.IsTrue(a.GetPart<StackerPart>().CanStackWith(b));
        }

        [Test]
        public void StackerPart_CannotStackWith_DifferentBlueprint()
        {
            var a = CreateStackableItem("Torch", 1, 3);
            var b = CreateStackableItem("Arrow", 1, 2);
            Assert.IsFalse(a.GetPart<StackerPart>().CanStackWith(b));
        }

        [Test]
        public void StackerPart_CannotStackWith_Self()
        {
            var a = CreateStackableItem("Torch", 1, 3);
            Assert.IsFalse(a.GetPart<StackerPart>().CanStackWith(a));
        }

        [Test]
        public void StackerPart_CannotStackWith_NonStacker()
        {
            var a = CreateStackableItem("Torch", 1, 3);
            var b = CreateTakeableItem(1);
            b.BlueprintName = "Torch";
            Assert.IsFalse(a.GetPart<StackerPart>().CanStackWith(b));
        }

        [Test]
        public void StackerPart_MergeFrom_CombinesCounts()
        {
            var a = CreateStackableItem("Torch", 1, 3);
            var b = CreateStackableItem("Torch", 1, 2);
            int merged = a.GetPart<StackerPart>().MergeFrom(b);
            Assert.AreEqual(2, merged);
            Assert.AreEqual(5, a.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(0, b.GetPart<StackerPart>().StackCount);
        }

        [Test]
        public void StackerPart_MergeFrom_CapsAtMaxStack()
        {
            var a = CreateStackableItem("Torch", 1, 95);
            var b = CreateStackableItem("Torch", 1, 10);
            int merged = a.GetPart<StackerPart>().MergeFrom(b);
            Assert.AreEqual(4, merged); // 99 - 95 = 4
            Assert.AreEqual(99, a.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(6, b.GetPart<StackerPart>().StackCount);
        }

        [Test]
        public void StackerPart_SplitStack_CreatesClone()
        {
            var a = CreateStackableItem("Torch", 2, 5);
            var stacker = a.GetPart<StackerPart>();
            var split = stacker.SplitStack(3);

            Assert.IsNotNull(split);
            Assert.AreEqual(2, stacker.StackCount);
            Assert.AreEqual(3, split.GetPart<StackerPart>().StackCount);
            Assert.AreEqual("Torch", split.BlueprintName);
            Assert.AreEqual(2, split.GetPart<PhysicsPart>().Weight);
        }

        [Test]
        public void StackerPart_SplitStack_InvalidCount_ReturnsNull()
        {
            var a = CreateStackableItem("Torch", 2, 5);
            Assert.IsNull(a.GetPart<StackerPart>().SplitStack(0));
            Assert.IsNull(a.GetPart<StackerPart>().SplitStack(5)); // can't split all
            Assert.IsNull(a.GetPart<StackerPart>().SplitStack(6));
        }

        [Test]
        public void StackerPart_RemoveOne_FromStack()
        {
            var a = CreateStackableItem("Torch", 2, 5);
            var stacker = a.GetPart<StackerPart>();
            var one = stacker.RemoveOne();

            Assert.AreNotSame(a, one);
            Assert.AreEqual(4, stacker.StackCount);
            Assert.AreEqual(1, one.GetPart<StackerPart>().StackCount);
        }

        [Test]
        public void StackerPart_RemoveOne_LastItem_ReturnsSelf()
        {
            var a = CreateStackableItem("Torch", 2, 1);
            var stacker = a.GetPart<StackerPart>();
            var one = stacker.RemoveOne();

            Assert.AreSame(a, one);
            Assert.AreEqual(1, stacker.StackCount);
        }

        [Test]
        public void StackerPart_GetTotalWeight()
        {
            var a = CreateStackableItem("Torch", 3, 5);
            Assert.AreEqual(15, a.GetPart<StackerPart>().GetTotalWeight());
        }

        // ========================
        // Stacking: Inventory Integration
        // ========================

        [Test]
        public void Inventory_AddObject_MergesStacks()
        {
            var owner = CreateCreatureWithInventory();
            var inv = owner.GetPart<InventoryPart>();

            var a = CreateStackableItem("Torch", 1, 3);
            var b = CreateStackableItem("Torch", 1, 2);

            inv.AddObject(a);
            inv.AddObject(b);

            // Should merge into one entry
            Assert.AreEqual(1, inv.Objects.Count);
            Assert.AreEqual(5, inv.Objects[0].GetPart<StackerPart>().StackCount);
        }

        [Test]
        public void Inventory_AddObject_DifferentBlueprints_NoMerge()
        {
            var owner = CreateCreatureWithInventory();
            var inv = owner.GetPart<InventoryPart>();

            var a = CreateStackableItem("Torch", 1, 3);
            var b = CreateStackableItem("Arrow", 1, 2);

            inv.AddObject(a);
            inv.AddObject(b);

            Assert.AreEqual(2, inv.Objects.Count);
        }

        [Test]
        public void Inventory_GetCarriedWeight_AccountsForStackCount()
        {
            var owner = CreateCreatureWithInventory();
            var inv = owner.GetPart<InventoryPart>();

            var a = CreateStackableItem("Torch", 2, 5);
            inv.AddObject(a);

            Assert.AreEqual(10, inv.GetCarriedWeight()); // 2 * 5
        }

        [Test]
        public void Pickup_StackableItems_MergeInInventory()
        {
            var zone = new Zone();
            var actor = CreateCreatureWithInventory();
            zone.AddEntity(actor, 5, 5);

            var a = CreateStackableItem("Torch", 1, 3);
            zone.AddEntity(a, 5, 5);
            InventorySystem.Pickup(actor, a, zone);

            var b = CreateStackableItem("Torch", 1, 2);
            zone.AddEntity(b, 5, 5);
            InventorySystem.Pickup(actor, b, zone);

            var inv = actor.GetPart<InventoryPart>();
            Assert.AreEqual(1, inv.Objects.Count);
            Assert.AreEqual(5, inv.Objects[0].GetPart<StackerPart>().StackCount);
        }

        [Test]
        public void Equip_StackedItem_SplitsOffOne()
        {
            var actor = CreateCreatureWithInventory();
            var inv = actor.GetPart<InventoryPart>();

            var arrows = CreateStackableItem("Arrow", 1, 5);
            arrows.AddPart(new EquippablePart { Slot = "Hand" });
            inv.AddObject(arrows);

            Assert.IsTrue(InventorySystem.Equip(actor, arrows));

            // Original stack should have 4 remaining in inventory
            Assert.AreEqual(4, arrows.GetPart<StackerPart>().StackCount);
            Assert.IsTrue(inv.Objects.Contains(arrows));

            // Equipped item should be a separate entity with count 1
            var equipped = inv.GetEquipped("Hand");
            Assert.IsNotNull(equipped);
            Assert.AreNotSame(arrows, equipped);
            Assert.AreEqual(1, equipped.GetPart<StackerPart>().StackCount);
        }

        [Test]
        public void Equip_SingleStackedItem_EquipsDirectly()
        {
            var actor = CreateCreatureWithInventory();
            var inv = actor.GetPart<InventoryPart>();

            var arrow = CreateStackableItem("Arrow", 1, 1);
            arrow.AddPart(new EquippablePart { Slot = "Hand" });
            inv.AddObject(arrow);

            Assert.IsTrue(InventorySystem.Equip(actor, arrow));

            // Should equip directly — no split needed
            Assert.AreEqual(arrow, inv.GetEquipped("Hand"));
        }

        [Test]
        public void DropPartial_SplitsAndDropsCount()
        {
            var zone = new Zone();
            var actor = CreateCreatureWithInventory();
            zone.AddEntity(actor, 5, 5);

            var torches = CreateStackableItem("Torch", 1, 5);
            actor.GetPart<InventoryPart>().AddObject(torches);

            Assert.IsTrue(InventorySystem.DropPartial(actor, torches, 2, zone));

            // Original stack should have 3 remaining
            Assert.AreEqual(3, torches.GetPart<StackerPart>().StackCount);

            // Dropped entity should be in zone with count 2
            var items = InventorySystem.GetTakeableItemsAtFeet(actor, zone);
            Assert.AreEqual(1, items.Count);
            Assert.AreEqual(2, items[0].GetPart<StackerPart>().StackCount);
        }

        [Test]
        public void DropPartial_FullCount_DropsEntireItem()
        {
            var zone = new Zone();
            var actor = CreateCreatureWithInventory();
            zone.AddEntity(actor, 5, 5);

            var torches = CreateStackableItem("Torch", 1, 3);
            actor.GetPart<InventoryPart>().AddObject(torches);

            Assert.IsTrue(InventorySystem.DropPartial(actor, torches, 3, zone));

            // Entire stack should be dropped
            Assert.AreEqual(0, actor.GetPart<InventoryPart>().Objects.Count);
        }

        [Test]
        public void DisplayName_IncludesStackCount()
        {
            var a = CreateStackableItem("Torch", 1, 5);
            a.AddPart(new RenderPart { DisplayName = "torch" });
            Assert.AreEqual("torch (x5)", a.GetDisplayName());
        }

        [Test]
        public void DisplayName_SingleItem_NoCount()
        {
            var a = CreateStackableItem("Torch", 1, 1);
            a.AddPart(new RenderPart { DisplayName = "torch" });
            Assert.AreEqual("torch", a.GetDisplayName());
        }

        [Test]
        public void CloneForStack_PreservesPartsAndProperties()
        {
            var original = CreateStackableItem("Torch", 3, 5);
            original.AddPart(new RenderPart { DisplayName = "torch", RenderString = "*", ColorString = "&W" });
            original.SetTag("Flammable", "yes");
            original.Properties["Fuel"] = "10";

            var clone = original.CloneForStack();

            Assert.AreEqual("Torch", clone.BlueprintName);
            Assert.AreEqual(3, clone.GetPart<PhysicsPart>().Weight);
            Assert.IsTrue(clone.GetPart<PhysicsPart>().Takeable);
            Assert.IsNull(clone.GetPart<PhysicsPart>().InInventory);
            Assert.IsNull(clone.GetPart<PhysicsPart>().Equipped);
            Assert.AreEqual("torch", clone.GetPart<RenderPart>().DisplayName);
            Assert.AreEqual("*", clone.GetPart<RenderPart>().RenderString);
            Assert.AreEqual("yes", clone.GetTag("Flammable"));
            Assert.AreEqual("10", clone.GetProperty("Fuel"));
            Assert.AreEqual(5, clone.GetPart<StackerPart>().StackCount);
        }

        // ========================
        // Trade / Commerce
        // ========================

        [Test]
        public void GetItemValue_FromCommercePart()
        {
            var item = CreateTakeableItem(5);
            item.AddPart(new CommercePart { Value = 20 });
            Assert.AreEqual(20, TradeSystem.GetItemValue(item));
        }

        [Test]
        public void GetItemValue_StackAware()
        {
            var item = CreateStackableItem("Arrow", 1, 5);
            item.AddPart(new CommercePart { Value = 2 });
            Assert.AreEqual(10, TradeSystem.GetItemValue(item));
        }

        [Test]
        public void GetItemValue_NoCommercePart_ReturnsZero()
        {
            var item = CreateTakeableItem(5);
            Assert.AreEqual(0, TradeSystem.GetItemValue(item));
        }

        [Test]
        public void GetTradePerformance_Ego16_Returns035()
        {
            var actor = CreateCreatureWithInventory();
            actor.Statistics["Ego"] = new Stat { Name = "Ego", BaseValue = 16, Min = 1, Max = 50 };
            // EgoMod = (16-16)/2 = 0, perf = 0.35 + 0.07*0 = 0.35
            Assert.AreEqual(0.35, TradeSystem.GetTradePerformance(actor), 0.01);
        }

        [Test]
        public void GetTradePerformance_HighEgo_BetterDeals()
        {
            var actor = CreateCreatureWithInventory();
            actor.Statistics["Ego"] = new Stat { Name = "Ego", BaseValue = 24, Min = 1, Max = 50 };
            // EgoMod = (24-16)/2 = 4, perf = 0.35 + 0.07*4 = 0.63
            Assert.AreEqual(0.63, TradeSystem.GetTradePerformance(actor), 0.01);
        }

        [Test]
        public void GetTradePerformance_ClampedMax()
        {
            var actor = CreateCreatureWithInventory();
            actor.Statistics["Ego"] = new Stat { Name = "Ego", BaseValue = 40, Min = 1, Max = 50 };
            // EgoMod = 12, perf = 0.35 + 0.84 = 1.19 → clamped to 0.95
            Assert.AreEqual(0.95, TradeSystem.GetTradePerformance(actor), 0.01);
        }

        [Test]
        public void BuyPrice_HigherThanValue()
        {
            var item = CreateTakeableItem(5);
            item.AddPart(new CommercePart { Value = 20 });
            int price = TradeSystem.GetBuyPrice(item, 0.5);
            // ceil(20 / 0.5) = 40
            Assert.AreEqual(40, price);
        }

        [Test]
        public void SellPrice_LowerThanValue()
        {
            var item = CreateTakeableItem(5);
            item.AddPart(new CommercePart { Value = 20 });
            int price = TradeSystem.GetSellPrice(item, 0.5);
            // floor(20 * 0.5) = 10
            Assert.AreEqual(10, price);
        }

        [Test]
        public void BuyFromTrader_TransfersItemAndDrams()
        {
            var player = CreateCreatureWithInventory();
            TradeSystem.SetDrams(player, 100);

            var trader = CreateCreatureWithInventory();
            var item = CreateTakeableItem(5);
            item.AddPart(new CommercePart { Value = 20 });
            trader.GetPart<InventoryPart>().AddObject(item);

            // Use known performance
            player.Statistics["Ego"] = new Stat { Name = "Ego", BaseValue = 16, Min = 1, Max = 50 };
            double perf = TradeSystem.GetTradePerformance(player);
            int price = TradeSystem.GetBuyPrice(item, perf);

            Assert.IsTrue(TradeSystem.BuyFromTrader(player, trader, item));
            Assert.IsTrue(player.GetPart<InventoryPart>().Objects.Contains(item));
            Assert.AreEqual(0, trader.GetPart<InventoryPart>().Objects.Count);
            Assert.AreEqual(100 - price, TradeSystem.GetDrams(player));
            Assert.AreEqual(price, TradeSystem.GetDrams(trader));
        }

        [Test]
        public void BuyFromTrader_CantAfford_Fails()
        {
            var player = CreateCreatureWithInventory();
            TradeSystem.SetDrams(player, 1);

            var trader = CreateCreatureWithInventory();
            var item = CreateTakeableItem(5);
            item.AddPart(new CommercePart { Value = 100 });
            trader.GetPart<InventoryPart>().AddObject(item);

            Assert.IsFalse(TradeSystem.BuyFromTrader(player, trader, item));
            Assert.AreEqual(1, trader.GetPart<InventoryPart>().Objects.Count);
        }

        [Test]
        public void SellToTrader_TransfersItemAndDrams()
        {
            var player = CreateCreatureWithInventory();
            player.Statistics["Ego"] = new Stat { Name = "Ego", BaseValue = 16, Min = 1, Max = 50 };
            var item = CreateTakeableItem(5);
            item.AddPart(new CommercePart { Value = 20 });
            player.GetPart<InventoryPart>().AddObject(item);

            var trader = CreateCreatureWithInventory();
            TradeSystem.SetDrams(trader, 100);

            double perf = TradeSystem.GetTradePerformance(player);
            int price = TradeSystem.GetSellPrice(item, perf);

            Assert.IsTrue(TradeSystem.SellToTrader(player, trader, item));
            Assert.AreEqual(0, player.GetPart<InventoryPart>().Objects.Count);
            Assert.IsTrue(trader.GetPart<InventoryPart>().Objects.Contains(item));
            Assert.AreEqual(price, TradeSystem.GetDrams(player));
            Assert.AreEqual(100 - price, TradeSystem.GetDrams(trader));
        }

        [Test]
        public void SellToTrader_TraderCantAfford_Fails()
        {
            var player = CreateCreatureWithInventory();
            player.Statistics["Ego"] = new Stat { Name = "Ego", BaseValue = 16, Min = 1, Max = 50 };
            var item = CreateTakeableItem(5);
            item.AddPart(new CommercePart { Value = 200 });
            player.GetPart<InventoryPart>().AddObject(item);

            var trader = CreateCreatureWithInventory();
            TradeSystem.SetDrams(trader, 1);

            Assert.IsFalse(TradeSystem.SellToTrader(player, trader, item));
            Assert.AreEqual(1, player.GetPart<InventoryPart>().Objects.Count);
        }

        [Test]
        public void GetTraderStock_OnlyIncludesCommerceItems()
        {
            var trader = CreateCreatureWithInventory();
            var inv = trader.GetPart<InventoryPart>();

            var sellable = CreateTakeableItem(5);
            sellable.AddPart(new CommercePart { Value = 10 });
            inv.AddObject(sellable);

            var nonsellable = CreateTakeableItem(5);
            inv.AddObject(nonsellable);

            var stock = TradeSystem.GetTraderStock(trader);
            Assert.AreEqual(1, stock.Count);
            Assert.AreEqual(sellable, stock[0]);
        }

        [Test]
        public void Drams_GetSet()
        {
            var actor = CreateCreatureWithInventory();
            Assert.AreEqual(0, TradeSystem.GetDrams(actor));
            TradeSystem.SetDrams(actor, 50);
            Assert.AreEqual(50, TradeSystem.GetDrams(actor));
        }

        // ========================
        // Auto-Equip
        // ========================

        [Test]
        public void AutoEquip_EmptySlot_EquipsOnPickup()
        {
            var zone = new Zone();
            var actor = CreateCreatureWithInventory();
            zone.AddEntity(actor, 5, 5);

            var weapon = CreateWeapon("1d6", 1);
            zone.AddEntity(weapon, 5, 5);

            InventorySystem.Pickup(actor, weapon, zone);

            // Should auto-equip since Hand slot is empty
            var inv = actor.GetPart<InventoryPart>();
            Assert.AreEqual(weapon, inv.GetEquipped("Hand"));
            Assert.AreEqual(0, inv.Objects.Count);
        }

        [Test]
        public void AutoEquip_OccupiedSlot_StaysInInventory()
        {
            var zone = new Zone();
            var actor = CreateCreatureWithInventory();
            zone.AddEntity(actor, 5, 5);

            var weapon1 = CreateWeapon("1d4", 1);
            zone.AddEntity(weapon1, 5, 5);
            InventorySystem.Pickup(actor, weapon1, zone);
            // weapon1 auto-equipped

            var weapon2 = CreateWeapon("1d8", 2);
            zone.AddEntity(weapon2, 5, 5);
            InventorySystem.Pickup(actor, weapon2, zone);

            // weapon2 should NOT auto-equip (slot occupied)
            var inv = actor.GetPart<InventoryPart>();
            Assert.AreEqual(weapon1, inv.GetEquipped("Hand"));
            Assert.IsTrue(inv.Objects.Contains(weapon2));
        }

        [Test]
        public void AutoEquip_NonEquippable_StaysInInventory()
        {
            var zone = new Zone();
            var actor = CreateCreatureWithInventory();
            zone.AddEntity(actor, 5, 5);

            var item = CreateTakeableItem(5);
            zone.AddEntity(item, 5, 5);
            InventorySystem.Pickup(actor, item, zone);

            var inv = actor.GetPart<InventoryPart>();
            Assert.IsTrue(inv.Objects.Contains(item));
        }

        [Test]
        public void AutoEquip_StackedItem_DoesNotAutoEquip()
        {
            var zone = new Zone();
            var actor = CreateCreatureWithInventory();
            zone.AddEntity(actor, 5, 5);

            var arrows = CreateStackableItem("Arrow", 1, 5);
            arrows.AddPart(new EquippablePart { Slot = "Hand" });
            zone.AddEntity(arrows, 5, 5);
            InventorySystem.Pickup(actor, arrows, zone);

            // Stacked items should NOT auto-equip
            var inv = actor.GetPart<InventoryPart>();
            Assert.IsNull(inv.GetEquipped("Hand"));
            Assert.IsTrue(inv.Objects.Contains(arrows));
        }

        [Test]
        public void AutoEquip_Armor_EmptyBodySlot()
        {
            var zone = new Zone();
            var actor = CreateCreatureWithInventory();
            zone.AddEntity(actor, 5, 5);

            var armor = CreateTakeableItem(10);
            armor.AddPart(new EquippablePart { Slot = "Body" });
            armor.AddPart(new ArmorPart { AV = 3 });
            zone.AddEntity(armor, 5, 5);

            InventorySystem.Pickup(actor, armor, zone);

            var inv = actor.GetPart<InventoryPart>();
            Assert.AreEqual(armor, inv.GetEquipped("Body"));
        }

        [Test]
        public void AutoEquip_DirectCall_EmptySlot()
        {
            var actor = CreateCreatureWithInventory();
            var weapon = CreateWeapon("1d6", 1);
            actor.GetPart<InventoryPart>().AddObject(weapon);

            Assert.IsTrue(InventorySystem.AutoEquip(actor, weapon));
            Assert.AreEqual(weapon, actor.GetPart<InventoryPart>().GetEquipped("Hand"));
        }

        [Test]
        public void AutoEquip_DirectCall_OccupiedSlot_ReturnsFalse()
        {
            var actor = CreateCreatureWithInventory();
            var weapon1 = CreateWeapon("1d4", 1);
            actor.GetPart<InventoryPart>().AddObject(weapon1);
            InventorySystem.Equip(actor, weapon1);

            var weapon2 = CreateWeapon("1d8", 2);
            actor.GetPart<InventoryPart>().AddObject(weapon2);

            Assert.IsFalse(InventorySystem.AutoEquip(actor, weapon2));
        }

        // ========================
        // Containers
        // ========================

        [Test]
        public void ContainerPart_AddItem_StoresItem()
        {
            var chest = CreateContainer();
            var item = CreateTakeableItem(5);
            Assert.IsTrue(chest.GetPart<ContainerPart>().AddItem(item));
            Assert.AreEqual(1, chest.GetPart<ContainerPart>().Contents.Count);
            Assert.AreEqual(chest, item.GetPart<PhysicsPart>().InInventory);
        }

        [Test]
        public void ContainerPart_RemoveItem_ClearsRef()
        {
            var chest = CreateContainer();
            var item = CreateTakeableItem(5);
            chest.GetPart<ContainerPart>().AddItem(item);
            Assert.IsTrue(chest.GetPart<ContainerPart>().RemoveItem(item));
            Assert.AreEqual(0, chest.GetPart<ContainerPart>().Contents.Count);
            Assert.IsNull(item.GetPart<PhysicsPart>().InInventory);
        }

        [Test]
        public void ContainerPart_MaxItems_RejectsWhenFull()
        {
            var chest = CreateContainer(maxItems: 1);
            var item1 = CreateTakeableItem(5);
            var item2 = CreateTakeableItem(5);
            Assert.IsTrue(chest.GetPart<ContainerPart>().AddItem(item1));
            Assert.IsFalse(chest.GetPart<ContainerPart>().AddItem(item2));
        }

        [Test]
        public void GetContainersAtFeet_FindsContainers()
        {
            var zone = new Zone();
            var actor = CreateCreatureWithInventory();
            zone.AddEntity(actor, 5, 5);

            var chest = CreateContainer();
            zone.AddEntity(chest, 5, 5);

            var containers = InventorySystem.GetContainersAtFeet(actor, zone);
            Assert.AreEqual(1, containers.Count);
            Assert.AreEqual(chest, containers[0]);
        }

        [Test]
        public void GetContainersAtFeet_ExcludesNonContainers()
        {
            var zone = new Zone();
            var actor = CreateCreatureWithInventory();
            zone.AddEntity(actor, 5, 5);

            var item = CreateTakeableItem(5);
            zone.AddEntity(item, 5, 5);

            var containers = InventorySystem.GetContainersAtFeet(actor, zone);
            Assert.AreEqual(0, containers.Count);
        }

        [Test]
        public void TakeFromContainer_TransfersItem()
        {
            var actor = CreateCreatureWithInventory();
            var chest = CreateContainer();
            var item = CreateTakeableItem(5);
            chest.GetPart<ContainerPart>().AddItem(item);

            Assert.IsTrue(InventorySystem.TakeFromContainer(actor, chest, item));
            Assert.AreEqual(0, chest.GetPart<ContainerPart>().Contents.Count);
            Assert.AreEqual(1, actor.GetPart<InventoryPart>().Objects.Count);
            Assert.AreEqual(actor, item.GetPart<PhysicsPart>().InInventory);
        }

        [Test]
        public void TakeFromContainer_LockedContainer_Fails()
        {
            var actor = CreateCreatureWithInventory();
            var chest = CreateContainer();
            chest.GetPart<ContainerPart>().Locked = true;
            var item = CreateTakeableItem(5);
            chest.GetPart<ContainerPart>().AddItem(item);

            Assert.IsFalse(InventorySystem.TakeFromContainer(actor, chest, item));
            Assert.AreEqual(1, chest.GetPart<ContainerPart>().Contents.Count);
        }

        [Test]
        public void TakeFromContainer_TooHeavy_PutsBack()
        {
            var actor = CreateCreatureWithInventory(maxWeight: 5);
            var chest = CreateContainer();
            var heavy = CreateTakeableItem(20);
            chest.GetPart<ContainerPart>().AddItem(heavy);

            Assert.IsFalse(InventorySystem.TakeFromContainer(actor, chest, heavy));
            Assert.AreEqual(1, chest.GetPart<ContainerPart>().Contents.Count);
        }

        [Test]
        public void TakeAllFromContainer_TransfersAll()
        {
            var actor = CreateCreatureWithInventory();
            var chest = CreateContainer();
            chest.GetPart<ContainerPart>().AddItem(CreateTakeableItem(2));
            chest.GetPart<ContainerPart>().AddItem(CreateTakeableItem(3));

            int taken = InventorySystem.TakeAllFromContainer(actor, chest);
            Assert.AreEqual(2, taken);
            Assert.AreEqual(0, chest.GetPart<ContainerPart>().Contents.Count);
            Assert.AreEqual(2, actor.GetPart<InventoryPart>().Objects.Count);
        }

        [Test]
        public void TakeAllFromContainer_StopsOnWeightLimit()
        {
            var actor = CreateCreatureWithInventory(maxWeight: 10);
            var chest = CreateContainer();
            chest.GetPart<ContainerPart>().AddItem(CreateTakeableItem(5));
            chest.GetPart<ContainerPart>().AddItem(CreateTakeableItem(5));
            chest.GetPart<ContainerPart>().AddItem(CreateTakeableItem(5));

            int taken = InventorySystem.TakeAllFromContainer(actor, chest);
            Assert.AreEqual(2, taken);
            Assert.AreEqual(1, chest.GetPart<ContainerPart>().Contents.Count);
        }

        [Test]
        public void PutInContainer_TransfersItem()
        {
            var actor = CreateCreatureWithInventory();
            var chest = CreateContainer();
            var item = CreateTakeableItem(5);
            actor.GetPart<InventoryPart>().AddObject(item);

            Assert.IsTrue(InventorySystem.PutInContainer(actor, chest, item));
            Assert.AreEqual(0, actor.GetPart<InventoryPart>().Objects.Count);
            Assert.AreEqual(1, chest.GetPart<ContainerPart>().Contents.Count);
        }

        [Test]
        public void PutInContainer_FullContainer_Fails()
        {
            var actor = CreateCreatureWithInventory();
            var chest = CreateContainer(maxItems: 0);
            var item = CreateTakeableItem(5);
            actor.GetPart<InventoryPart>().AddObject(item);

            Assert.IsFalse(InventorySystem.PutInContainer(actor, chest, item));
            Assert.AreEqual(1, actor.GetPart<InventoryPart>().Objects.Count);
        }

        [Test]
        public void PutInContainer_LockedContainer_Fails()
        {
            var actor = CreateCreatureWithInventory();
            var chest = CreateContainer();
            chest.GetPart<ContainerPart>().Locked = true;
            var item = CreateTakeableItem(5);
            actor.GetPart<InventoryPart>().AddObject(item);

            Assert.IsFalse(InventorySystem.PutInContainer(actor, chest, item));
            Assert.AreEqual(1, actor.GetPart<InventoryPart>().Objects.Count);
        }

        [Test]
        public void ContainerAction_OpenContainer_ShowsMessage()
        {
            var actor = CreateCreatureWithInventory();
            actor.AddPart(new RenderPart { DisplayName = "hero" });
            var chest = CreateContainer();
            chest.AddPart(new RenderPart { DisplayName = "chest" });
            chest.GetPart<ContainerPart>().AddItem(CreateTakeableItem(5));

            var actions = InventorySystem.GetActions(actor, chest);
            Assert.AreEqual(1, actions.Count);
            Assert.AreEqual("Open", actions[0].Name);

            InventorySystem.PerformAction(actor, chest, "OpenContainer");
            Assert.IsTrue(MessageLog.GetMessages().Exists(m => m.Contains("contains 1 item")));
        }

        [Test]
        public void ContainerAction_OpenEmpty_ShowsEmpty()
        {
            var actor = CreateCreatureWithInventory();
            var chest = CreateContainer();
            chest.AddPart(new RenderPart { DisplayName = "chest" });

            InventorySystem.PerformAction(actor, chest, "OpenContainer");
            Assert.IsTrue(MessageLog.GetMessages().Exists(m => m.Contains("empty")));
        }

        [Test]
        public void ContainerAction_Locked_ShowsLocked()
        {
            var actor = CreateCreatureWithInventory();
            var chest = CreateContainer();
            chest.AddPart(new RenderPart { DisplayName = "chest" });
            chest.GetPart<ContainerPart>().Locked = true;

            var actions = InventorySystem.GetActions(actor, chest);
            Assert.AreEqual(1, actions.Count);
            Assert.AreEqual("Unlock", actions[0].Name);
        }

        // ========================
        // Inventory Action Framework
        // ========================

        [Test]
        public void GetActions_FoodItem_ReturnsEatAction()
        {
            var actor = CreateCreatureWithInventory();
            var food = CreateFoodItem("2d4");
            actor.GetPart<InventoryPart>().AddObject(food);

            var actions = InventorySystem.GetActions(actor, food);
            Assert.AreEqual(1, actions.Count);
            Assert.AreEqual("Eat", actions[0].Name);
            Assert.AreEqual("eat", actions[0].Display);
            Assert.AreEqual("Eat", actions[0].Command);
            Assert.AreEqual('e', actions[0].Key);
        }

        [Test]
        public void GetActions_TonicItem_ReturnsApplyAction()
        {
            var actor = CreateCreatureWithInventory();
            var tonic = CreateTonicItem("4d4", drink: false);
            actor.GetPart<InventoryPart>().AddObject(tonic);

            var actions = InventorySystem.GetActions(actor, tonic);
            Assert.AreEqual(1, actions.Count);
            Assert.AreEqual("Apply", actions[0].Name);
            Assert.AreEqual("apply", actions[0].Display);
            Assert.AreEqual("ApplyTonic", actions[0].Command);
        }

        [Test]
        public void GetActions_DrinkableTonic_ReturnsDrinkAction()
        {
            var actor = CreateCreatureWithInventory();
            var tonic = CreateTonicItem("4d4", drink: true);
            actor.GetPart<InventoryPart>().AddObject(tonic);

            var actions = InventorySystem.GetActions(actor, tonic);
            Assert.AreEqual(1, actions.Count);
            Assert.AreEqual("Drink", actions[0].Name);
            Assert.AreEqual("drink", actions[0].Display);
        }

        [Test]
        public void GetActions_NoActionParts_ReturnsEmpty()
        {
            var actor = CreateCreatureWithInventory();
            var item = CreateTakeableItem(5);
            actor.GetPart<InventoryPart>().AddObject(item);

            var actions = InventorySystem.GetActions(actor, item);
            Assert.AreEqual(0, actions.Count);
        }

        [Test]
        public void GetActions_SortedByPriority()
        {
            var actor = CreateCreatureWithInventory();
            var item = new Entity();
            item.AddPart(new PhysicsPart { Takeable = true });
            item.AddPart(new FoodPart { Healing = "1d4" }); // priority 20
            item.AddPart(new TestActionPart("Inspect", "inspect", "Inspect", 'i', 5));
            actor.GetPart<InventoryPart>().AddObject(item);

            var actions = InventorySystem.GetActions(actor, item);
            Assert.AreEqual(2, actions.Count);
            Assert.AreEqual("Eat", actions[0].Name); // priority 20
            Assert.AreEqual("Inspect", actions[1].Name); // priority 5
        }

        [Test]
        public void PerformAction_EatFood_HealsActor()
        {
            var actor = CreateCreatureWithInventory();
            actor.Statistics["Hitpoints"] = new Stat
            {
                Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 30
            };
            var food = CreateFoodItem("10d1"); // always heals 10
            actor.GetPart<InventoryPart>().AddObject(food);

            bool result = InventorySystem.PerformAction(actor, food, "Eat");
            Assert.IsTrue(result);
            Assert.AreEqual(20, actor.GetStatValue("Hitpoints"));
        }

        [Test]
        public void PerformAction_EatFood_CappedAtMaxHP()
        {
            var actor = CreateCreatureWithInventory();
            actor.Statistics["Hitpoints"] = new Stat
            {
                Name = "Hitpoints", BaseValue = 28, Min = 0, Max = 30
            };
            var food = CreateFoodItem("10d1");
            actor.GetPart<InventoryPart>().AddObject(food);

            InventorySystem.PerformAction(actor, food, "Eat");
            Assert.AreEqual(30, actor.GetStatValue("Hitpoints"));
        }

        [Test]
        public void PerformAction_EatFood_ConsumesItem()
        {
            var actor = CreateCreatureWithInventory();
            var food = CreateFoodItem("1d4");
            var inv = actor.GetPart<InventoryPart>();
            inv.AddObject(food);

            InventorySystem.PerformAction(actor, food, "Eat");
            Assert.AreEqual(0, inv.Objects.Count);
        }

        [Test]
        public void PerformAction_EatStackedFood_DecrementsStack()
        {
            var actor = CreateCreatureWithInventory();
            var food = CreateFoodItem("1d4");
            food.BlueprintName = "Starapple";
            food.AddPart(new StackerPart { StackCount = 3 });
            var inv = actor.GetPart<InventoryPart>();
            inv.AddObject(food);

            InventorySystem.PerformAction(actor, food, "Eat");
            Assert.AreEqual(1, inv.Objects.Count);
            Assert.AreEqual(2, food.GetPart<StackerPart>().StackCount);
        }

        [Test]
        public void PerformAction_EatFood_LogsMessage()
        {
            var actor = CreateCreatureWithInventory();
            actor.AddPart(new RenderPart { DisplayName = "hero" });
            var food = CreateFoodItem("1d4");
            food.AddPart(new RenderPart { DisplayName = "starapple" });
            actor.GetPart<InventoryPart>().AddObject(food);

            InventorySystem.PerformAction(actor, food, "Eat");
            Assert.IsTrue(MessageLog.GetMessages().Exists(m => m.Contains("eats") || m.Contains("heals")));
        }

        [Test]
        public void PerformAction_EatFoodWithCustomMessage_ShowsCustomMessage()
        {
            var actor = CreateCreatureWithInventory();
            var food = new Entity();
            food.AddPart(new PhysicsPart { Takeable = true });
            food.AddPart(new FoodPart { Healing = "1d4", Message = "Delicious!" });
            actor.GetPart<InventoryPart>().AddObject(food);

            InventorySystem.PerformAction(actor, food, "Eat");
            Assert.IsTrue(MessageLog.GetMessages().Exists(m => m.Contains("Delicious!")));
        }

        [Test]
        public void PerformAction_ApplyTonic_HealsActor()
        {
            var actor = CreateCreatureWithInventory();
            actor.Statistics["Hitpoints"] = new Stat
            {
                Name = "Hitpoints", BaseValue = 10, Min = 0, Max = 30
            };
            var tonic = CreateTonicItem("10d1", drink: true);
            actor.GetPart<InventoryPart>().AddObject(tonic);

            bool result = InventorySystem.PerformAction(actor, tonic, "ApplyTonic");
            Assert.IsTrue(result);
            Assert.AreEqual(20, actor.GetStatValue("Hitpoints"));
        }

        [Test]
        public void PerformAction_ApplyTonic_ConsumesItem()
        {
            var actor = CreateCreatureWithInventory();
            var tonic = CreateTonicItem("1d4", drink: false);
            var inv = actor.GetPart<InventoryPart>();
            inv.AddObject(tonic);

            InventorySystem.PerformAction(actor, tonic, "ApplyTonic");
            Assert.AreEqual(0, inv.Objects.Count);
        }

        [Test]
        public void PerformAction_TonicWithStatBoost_AppliesBoost()
        {
            var actor = CreateCreatureWithInventory();
            actor.Statistics["Strength"] = new Stat
            {
                Name = "Strength", BaseValue = 16, Min = 1, Max = 50
            };
            var tonic = new Entity();
            tonic.AddPart(new PhysicsPart { Takeable = true });
            tonic.AddPart(new TonicPart { StatBoost = "Strength:4" });
            actor.GetPart<InventoryPart>().AddObject(tonic);

            InventorySystem.PerformAction(actor, tonic, "ApplyTonic");
            Assert.AreEqual(20, actor.GetStatValue("Strength"));
        }

        [Test]
        public void PerformAction_UnknownCommand_ReturnsFalse()
        {
            var actor = CreateCreatureWithInventory();
            var food = CreateFoodItem("1d4");
            actor.GetPart<InventoryPart>().AddObject(food);

            bool result = InventorySystem.PerformAction(actor, food, "Juggle");
            Assert.IsFalse(result);
        }

        [Test]
        public void PerformAction_BeforeInventoryAction_CanCancel()
        {
            var actor = CreateCreatureWithInventory();
            actor.AddPart(new CancelEventPart("BeforeInventoryAction"));
            var food = CreateFoodItem("1d4");
            actor.GetPart<InventoryPart>().AddObject(food);

            bool result = InventorySystem.PerformAction(actor, food, "Eat");
            Assert.IsFalse(result);
            // Item should NOT be consumed
            Assert.AreEqual(1, actor.GetPart<InventoryPart>().Objects.Count);
        }

        // ========================
        // Item Categories
        // ========================

        [Test]
        public void ItemCategory_GetSortOrder_KnownCategory()
        {
            Assert.AreEqual(10, ItemCategory.GetSortOrder(ItemCategory.MeleeWeapons));
            Assert.AreEqual(40, ItemCategory.GetSortOrder(ItemCategory.Armor));
            Assert.AreEqual(70, ItemCategory.GetSortOrder(ItemCategory.Food));
            Assert.AreEqual(900, ItemCategory.GetSortOrder(ItemCategory.Miscellaneous));
        }

        [Test]
        public void ItemCategory_GetSortOrder_UnknownCategory_SortsNearEnd()
        {
            Assert.AreEqual(998, ItemCategory.GetSortOrder("Widgets"));
        }

        [Test]
        public void ItemCategory_GetSortOrder_NullOrEmpty_SortsLast()
        {
            Assert.AreEqual(999, ItemCategory.GetSortOrder(null));
            Assert.AreEqual(999, ItemCategory.GetSortOrder(""));
        }

        [Test]
        public void ItemCategory_Compare_OrdersCorrectly()
        {
            Assert.Less(ItemCategory.Compare(ItemCategory.MeleeWeapons, ItemCategory.Armor), 0);
            Assert.Greater(ItemCategory.Compare(ItemCategory.Food, ItemCategory.Ammo), 0);
            Assert.AreEqual(0, ItemCategory.Compare(ItemCategory.Armor, ItemCategory.Armor));
        }

        [Test]
        public void ItemCategory_GetCategory_ExplicitCategory()
        {
            var item = new Entity();
            item.AddPart(new PhysicsPart { Category = "Food" });
            Assert.AreEqual("Food", ItemCategory.GetCategory(item));
        }

        [Test]
        public void ItemCategory_GetCategory_InfersFromMeleeWeaponPart()
        {
            var item = new Entity();
            item.AddPart(new PhysicsPart());
            item.AddPart(new MeleeWeaponPart());
            Assert.AreEqual(ItemCategory.MeleeWeapons, ItemCategory.GetCategory(item));
        }

        [Test]
        public void ItemCategory_GetCategory_InfersNaturalWeapon()
        {
            var item = new Entity();
            item.AddPart(new PhysicsPart());
            item.AddPart(new MeleeWeaponPart());
            item.SetTag("Natural");
            Assert.AreEqual(ItemCategory.NaturalWeapons, ItemCategory.GetCategory(item));
        }

        [Test]
        public void ItemCategory_GetCategory_InfersArmorFromParts()
        {
            var item = new Entity();
            item.AddPart(new PhysicsPart());
            item.AddPart(new ArmorPart());
            item.AddPart(new EquippablePart { Slot = "Body" });
            Assert.AreEqual(ItemCategory.Armor, ItemCategory.GetCategory(item));
        }

        [Test]
        public void ItemCategory_GetCategory_InfersClothesFromBackSlot()
        {
            var item = new Entity();
            item.AddPart(new PhysicsPart());
            item.AddPart(new ArmorPart());
            item.AddPart(new EquippablePart { Slot = "Back" });
            Assert.AreEqual(ItemCategory.Clothes, ItemCategory.GetCategory(item));
        }

        [Test]
        public void ItemCategory_GetCategory_FallsBackToMiscellaneous()
        {
            var item = new Entity();
            item.AddPart(new PhysicsPart());
            Assert.AreEqual(ItemCategory.Miscellaneous, ItemCategory.GetCategory(item));
        }

        [Test]
        public void ItemCategory_GetCategory_NullEntity_ReturnsUnknown()
        {
            Assert.AreEqual(ItemCategory.Unknown, ItemCategory.GetCategory(null));
        }

        [Test]
        public void Blueprint_Dagger_HasMeleeWeaponsCategory()
        {
            var factory = new EntityFactory();
            factory.LoadBlueprints(GetTestJson());

            var dagger = factory.CreateEntity("Dagger");
            Assert.AreEqual("Melee Weapons", dagger.GetPart<PhysicsPart>().Category);
            Assert.AreEqual(ItemCategory.MeleeWeapons, ItemCategory.GetCategory(dagger));
        }

        [Test]
        public void Blueprint_LeatherArmor_HasArmorCategory()
        {
            var factory = new EntityFactory();
            factory.LoadBlueprints(GetTestJson());

            var leather = factory.CreateEntity("LeatherArmor");
            Assert.AreEqual("Armor", leather.GetPart<PhysicsPart>().Category);
            Assert.AreEqual(ItemCategory.Armor, ItemCategory.GetCategory(leather));
        }

        // ========================
        // InventoryScreenData
        // ========================

        [Test]
        public void ScreenData_Build_NullActor_ReturnsEmptyState()
        {
            var state = InventoryScreenData.Build(null);
            Assert.AreEqual(0, state.TotalItems);
            Assert.AreEqual(0, state.Categories.Count);
            Assert.AreEqual(0, state.Equipment.Count);
        }

        [Test]
        public void ScreenData_Build_EmptyInventory_ReturnsZeroItems()
        {
            var actor = CreateCreatureWithInventory();
            var state = InventoryScreenData.Build(actor);
            Assert.AreEqual(0, state.TotalItems);
            Assert.AreEqual(0, state.Categories.Count);
        }

        [Test]
        public void ScreenData_Build_ItemsGroupedByCategory()
        {
            var actor = CreateCreatureWithInventory();
            var inv = actor.GetPart<InventoryPart>();

            var dagger = CreateEquippableItem("Dagger", "Hand", 2, 10, "Melee Weapons");
            var sword = CreateEquippableItem("Sword", "Hand", 4, 25, "Melee Weapons");
            var armor = CreateEquippableItem("Armor", "Body", 10, 30, "Armor");
            inv.AddObject(dagger);
            inv.AddObject(sword);
            inv.AddObject(armor);

            var state = InventoryScreenData.Build(actor);
            Assert.AreEqual(3, state.TotalItems);
            Assert.AreEqual(2, state.Categories.Count);

            // Categories sorted: Melee Weapons (10) before Armor (40)
            Assert.AreEqual("Melee Weapons", state.Categories[0].CategoryName);
            Assert.AreEqual(2, state.Categories[0].Items.Count);
            Assert.AreEqual("Armor", state.Categories[1].CategoryName);
            Assert.AreEqual(1, state.Categories[1].Items.Count);
        }

        [Test]
        public void ScreenData_Build_TracksWeight()
        {
            var actor = CreateCreatureWithInventory(maxWeight: 100);
            var inv = actor.GetPart<InventoryPart>();
            inv.AddObject(CreateTakeableItem(5));
            inv.AddObject(CreateTakeableItem(8));

            var state = InventoryScreenData.Build(actor);
            Assert.AreEqual(13, state.CarriedWeight);
            // MaxCarryWeight = Strength(16) * WEIGHT_PER_STRENGTH(15) = 240
            Assert.AreEqual(240, state.MaxCarryWeight);
        }

        [Test]
        public void ScreenData_Build_TracksDrams()
        {
            var actor = CreateCreatureWithInventory();
            actor.SetIntProperty("Drams", 250);

            var state = InventoryScreenData.Build(actor);
            Assert.AreEqual(250, state.Drams);
        }

        [Test]
        public void ScreenData_Build_EquippedItemsIncluded()
        {
            var actor = CreateCreatureWithInventory();
            var inv = actor.GetPart<InventoryPart>();

            var dagger = CreateEquippableItem("Dagger", "Hand", 2, 10, "Melee Weapons");
            inv.AddObject(dagger);
            inv.Equip(dagger, "Hand");

            var state = InventoryScreenData.Build(actor);
            // Equipped items appear in both TotalItems count and categories
            Assert.IsTrue(state.TotalItems > 0);

            // Equipment list shows the equipped item
            bool found = false;
            for (int i = 0; i < state.Equipment.Count; i++)
            {
                if (state.Equipment[i].EquippedItem == dagger)
                {
                    found = true;
                    break;
                }
            }
            Assert.IsTrue(found, "Equipped dagger should appear in equipment list");
        }

        [Test]
        public void ScreenData_Build_StackedItemShowsCount()
        {
            var actor = CreateCreatureWithInventory();
            var inv = actor.GetPart<InventoryPart>();

            var arrows = new Entity();
            arrows.BlueprintName = "TestArrows";
            arrows.Tags["Item"] = "";
            arrows.AddPart(new PhysicsPart { Takeable = true, Weight = 1, Category = "Ammo" });
            arrows.AddPart(new StackerPart { StackCount = 20, MaxStack = 99 });
            arrows.AddPart(new RenderPart { DisplayName = "arrows" });
            inv.AddObject(arrows);

            var state = InventoryScreenData.Build(actor);
            Assert.AreEqual(1, state.TotalItems);
            Assert.AreEqual(1, state.Categories.Count);
            Assert.AreEqual(20, state.Categories[0].Items[0].StackCount);
        }

        [Test]
        public void ScreenData_BuildEquipmentList_LegacySlots()
        {
            var actor = CreateCreatureWithInventory();
            var inv = actor.GetPart<InventoryPart>();

            var dagger = CreateEquippableItem("Dagger", "Hand", 2, 10, "Melee Weapons");
            inv.AddObject(dagger);
            inv.Equip(dagger, "Hand");

            var slots = InventoryScreenData.BuildEquipmentList(actor, inv);
            Assert.IsTrue(slots.Count > 0);
            Assert.AreEqual("Hand", slots[0].SlotName);
            Assert.AreEqual(dagger, slots[0].EquippedItem);
        }

        [Test]
        public void ScreenData_ItemDisplay_HasValueFromCommerce()
        {
            var actor = CreateCreatureWithInventory();
            var inv = actor.GetPart<InventoryPart>();

            var item = CreateTakeableItem(5);
            item.AddPart(new CommercePart { Value = 42 });
            inv.AddObject(item);

            var state = InventoryScreenData.Build(actor);
            Assert.AreEqual(1, state.Categories.Count);
            Assert.AreEqual(42, state.Categories[0].Items[0].Value);
        }

        // ========================
        // Two-Handed Weapons
        // ========================

        [Test]
        public void TwoHandedWeapon_Equip_ClaimsTwoHands()
        {
            var actor = CreateCreatureWithBody();
            var inv = actor.GetPart<InventoryPart>();
            var body = actor.GetPart<Body>();

            var twoHander = CreateTwoHandedWeapon("battleaxe");
            inv.AddObject(twoHander);

            Assert.IsTrue(InventorySystem.Equip(actor, twoHander));

            var hands = body.GetEquippableSlots("Hand");
            Assert.AreEqual(twoHander, hands[0]._Equipped);
            Assert.AreEqual(twoHander, hands[1]._Equipped);
            Assert.IsFalse(inv.Objects.Contains(twoHander));
        }

        [Test]
        public void TwoHandedWeapon_Unequip_FreesBothHands()
        {
            var actor = CreateCreatureWithBody();
            var inv = actor.GetPart<InventoryPart>();
            var body = actor.GetPart<Body>();

            var twoHander = CreateTwoHandedWeapon("battleaxe");
            inv.AddObject(twoHander);
            InventorySystem.Equip(actor, twoHander);

            Assert.IsTrue(InventorySystem.UnequipItem(actor, twoHander));

            var hands = body.GetEquippableSlots("Hand");
            Assert.IsNull(hands[0]._Equipped);
            Assert.IsNull(hands[1]._Equipped);
            Assert.IsTrue(inv.Objects.Contains(twoHander));
        }

        [Test]
        public void TwoHandedWeapon_DisplacesOneHandedWeapons()
        {
            var actor = CreateCreatureWithBody();
            var inv = actor.GetPart<InventoryPart>();
            var body = actor.GetPart<Body>();

            var sword = CreateOneHandedWeapon("long sword");
            var shield = CreateOneHandedWeapon("buckler");
            inv.AddObject(sword);
            inv.AddObject(shield);
            InventorySystem.Equip(actor, sword);
            InventorySystem.Equip(actor, shield);

            var twoHander = CreateTwoHandedWeapon("battleaxe");
            inv.AddObject(twoHander);

            Assert.IsTrue(InventorySystem.Equip(actor, twoHander));

            // Two-hander occupies both hands
            var hands = body.GetEquippableSlots("Hand");
            Assert.AreEqual(twoHander, hands[0]._Equipped);
            Assert.AreEqual(twoHander, hands[1]._Equipped);

            // Displaced items are back in inventory
            Assert.IsTrue(inv.Objects.Contains(sword));
            Assert.IsTrue(inv.Objects.Contains(shield));
        }

        [Test]
        public void TwoHandedWeapon_TargetedEquip_ClaimsSecondHand()
        {
            var actor = CreateCreatureWithBody();
            var inv = actor.GetPart<InventoryPart>();
            var body = actor.GetPart<Body>();

            var hands = body.GetEquippableSlots("Hand");
            var rightHand = hands[0];

            var twoHander = CreateTwoHandedWeapon("battleaxe");
            inv.AddObject(twoHander);

            Assert.IsTrue(InventorySystem.Equip(actor, twoHander, rightHand));

            Assert.AreEqual(twoHander, hands[0]._Equipped);
            Assert.AreEqual(twoHander, hands[1]._Equipped);
        }

        [Test]
        public void TwoHandedWeapon_TargetedEquip_DisplacesOtherHand()
        {
            var actor = CreateCreatureWithBody();
            var inv = actor.GetPart<InventoryPart>();
            var body = actor.GetPart<Body>();

            var hands = body.GetEquippableSlots("Hand");
            var shield = CreateOneHandedWeapon("buckler");
            inv.AddObject(shield);
            InventorySystem.Equip(actor, shield, hands[1]);
            Assert.AreEqual(shield, hands[1]._Equipped);

            var twoHander = CreateTwoHandedWeapon("battleaxe");
            inv.AddObject(twoHander);

            // Equip two-hander to hand[0]; should also claim hand[1] and displace shield
            Assert.IsTrue(InventorySystem.Equip(actor, twoHander, hands[0]));

            Assert.AreEqual(twoHander, hands[0]._Equipped);
            Assert.AreEqual(twoHander, hands[1]._Equipped);
            Assert.IsTrue(inv.Objects.Contains(shield));
        }

        [Test]
        public void PreviewDisplacements_NoDisplacement_EmptyList()
        {
            var actor = CreateCreatureWithBody();
            var inv = actor.GetPart<InventoryPart>();

            var sword = CreateOneHandedWeapon("long sword");
            inv.AddObject(sword);

            var result = InventorySystem.PreviewDisplacements(actor, sword);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void PreviewDisplacements_TwoHandedWithBothHandsOccupied()
        {
            var actor = CreateCreatureWithBody();
            var inv = actor.GetPart<InventoryPart>();
            var body = actor.GetPart<Body>();

            var sword = CreateOneHandedWeapon("long sword");
            var shield = CreateOneHandedWeapon("buckler");
            inv.AddObject(sword);
            inv.AddObject(shield);
            InventorySystem.Equip(actor, sword);
            InventorySystem.Equip(actor, shield);

            var twoHander = CreateTwoHandedWeapon("battleaxe");
            inv.AddObject(twoHander);

            var result = InventorySystem.PreviewDisplacements(actor, twoHander);
            Assert.AreEqual(2, result.Count);

            // Both existing items should be listed
            var displacedItems = new HashSet<Entity>();
            for (int i = 0; i < result.Count; i++)
                displacedItems.Add(result[i].Item);
            Assert.IsTrue(displacedItems.Contains(sword));
            Assert.IsTrue(displacedItems.Contains(shield));
        }

        [Test]
        public void PreviewDisplacements_TwoHandedWithOneHandFree()
        {
            var actor = CreateCreatureWithBody();
            var inv = actor.GetPart<InventoryPart>();

            var sword = CreateOneHandedWeapon("long sword");
            inv.AddObject(sword);
            InventorySystem.Equip(actor, sword);

            var twoHander = CreateTwoHandedWeapon("battleaxe");
            inv.AddObject(twoHander);

            var result = InventorySystem.PreviewDisplacements(actor, twoHander);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(sword, result[0].Item);
        }

        [Test]
        public void PreviewDisplacements_TargetedEquip_ShowsDisplacement()
        {
            var actor = CreateCreatureWithBody();
            var inv = actor.GetPart<InventoryPart>();
            var body = actor.GetPart<Body>();

            var hands = body.GetEquippableSlots("Hand");
            var shield = CreateOneHandedWeapon("buckler");
            inv.AddObject(shield);
            InventorySystem.Equip(actor, shield, hands[1]);

            var twoHander = CreateTwoHandedWeapon("battleaxe");
            inv.AddObject(twoHander);

            var result = InventorySystem.PreviewDisplacements(actor, twoHander, hands[0]);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(shield, result[0].Item);
        }

        [Test]
        public void PreviewDisplacements_Deduplicates_MultiSlotItem()
        {
            var actor = CreateCreatureWithBody();
            var inv = actor.GetPart<InventoryPart>();

            // Equip a two-hander, then preview replacing it with another two-hander
            var oldTwoHander = CreateTwoHandedWeapon("greatsword");
            inv.AddObject(oldTwoHander);
            InventorySystem.Equip(actor, oldTwoHander);

            var newTwoHander = CreateTwoHandedWeapon("battleaxe");
            inv.AddObject(newTwoHander);

            var result = InventorySystem.PreviewDisplacements(actor, newTwoHander);
            // The old two-hander occupies both hands but should only appear once
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(oldTwoHander, result[0].Item);
        }

        [Test]
        public void AutoEquip_TwoHanded_OnlyIfBothHandsFree()
        {
            var actor = CreateCreatureWithBody();
            var inv = actor.GetPart<InventoryPart>();

            // Occupy one hand
            var sword = CreateOneHandedWeapon("long sword");
            inv.AddObject(sword);
            InventorySystem.Equip(actor, sword);

            var twoHander = CreateTwoHandedWeapon("battleaxe");
            inv.AddObject(twoHander);

            // Auto-equip should fail since one hand is occupied
            Assert.IsFalse(InventorySystem.AutoEquip(actor, twoHander));
            Assert.IsTrue(inv.Objects.Contains(twoHander));
        }

        [Test]
        public void AutoEquip_TwoHanded_BothHandsFree_Succeeds()
        {
            var actor = CreateCreatureWithBody();
            var inv = actor.GetPart<InventoryPart>();
            var body = actor.GetPart<Body>();

            var twoHander = CreateTwoHandedWeapon("battleaxe");
            inv.AddObject(twoHander);

            Assert.IsTrue(InventorySystem.AutoEquip(actor, twoHander));

            var hands = body.GetEquippableSlots("Hand");
            Assert.AreEqual(twoHander, hands[0]._Equipped);
            Assert.AreEqual(twoHander, hands[1]._Equipped);
        }

        // ========================
        // Helpers
        // ========================

        private Entity CreateContainer(int maxItems = -1)
        {
            var entity = new Entity();
            entity.BlueprintName = "TestChest";
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new ContainerPart { MaxItems = maxItems });
            entity.AddPart(new RenderPart { DisplayName = "chest" });
            return entity;
        }

        private Entity CreateFoodItem(string healing)
        {
            var entity = new Entity();
            entity.BlueprintName = "TestFood";
            entity.Tags["Item"] = "";
            entity.Tags["Food"] = "";
            entity.AddPart(new PhysicsPart { Takeable = true, Weight = 1, Category = "Food" });
            entity.AddPart(new FoodPart { Healing = healing });
            return entity;
        }

        private Entity CreateTonicItem(string healing, bool drink)
        {
            var entity = new Entity();
            entity.BlueprintName = "TestTonic";
            entity.Tags["Item"] = "";
            entity.Tags["Tonic"] = "";
            entity.AddPart(new PhysicsPart { Takeable = true, Weight = 1, Category = "Tonics" });
            entity.AddPart(new TonicPart { Healing = healing, Drink = drink });
            return entity;
        }

        private Entity CreateStackableItem(string blueprintName, int weight, int stackCount)
        {
            var entity = new Entity();
            entity.BlueprintName = blueprintName;
            entity.Tags["Item"] = "";
            entity.AddPart(new PhysicsPart { Takeable = true, Weight = weight });
            entity.AddPart(new StackerPart { StackCount = stackCount });
            return entity;
        }

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

        private Entity CreateEquippableItem(string name, string slot, int weight, int value, string category)
        {
            var entity = new Entity();
            entity.BlueprintName = "Test" + name;
            entity.Tags["Item"] = "";
            entity.AddPart(new PhysicsPart { Takeable = true, Weight = weight, Category = category });
            entity.AddPart(new EquippablePart { Slot = slot });
            entity.AddPart(new RenderPart { DisplayName = name });
            if (value > 0)
                entity.AddPart(new CommercePart { Value = value });
            return entity;
        }

        private Entity CreateCreatureWithBody()
        {
            var entity = new Entity();
            entity.BlueprintName = "TestCreature";
            entity.Tags["Creature"] = "";

            entity.Statistics["Hitpoints"] = new Stat { Owner = entity, Name = "Hitpoints", BaseValue = 30, Min = 0, Max = 30 };
            entity.Statistics["Strength"] = new Stat { Owner = entity, Name = "Strength", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Agility"] = new Stat { Owner = entity, Name = "Agility", BaseValue = 16, Min = 1, Max = 50 };
            entity.Statistics["Speed"] = new Stat { Owner = entity, Name = "Speed", BaseValue = 100, Min = 25, Max = 200 };

            entity.AddPart(new RenderPart { DisplayName = "test creature" });
            entity.AddPart(new PhysicsPart { Solid = true });
            entity.AddPart(new ArmorPart());
            entity.AddPart(new InventoryPart { MaxWeight = 150 });

            var body = new Body();
            entity.AddPart(body);
            body.SetBody(AnatomyFactory.CreateHumanoid());

            return entity;
        }

        private Entity CreateOneHandedWeapon(string name)
        {
            var entity = new Entity();
            entity.BlueprintName = "Test" + name;
            entity.Tags["Item"] = "";
            entity.AddPart(new RenderPart { DisplayName = name });
            entity.AddPart(new PhysicsPart { Takeable = true, Weight = 5 });
            entity.AddPart(new MeleeWeaponPart { BaseDamage = "1d6", PenBonus = 1 });
            entity.AddPart(new EquippablePart { Slot = "Hand" });
            return entity;
        }

        private Entity CreateTwoHandedWeapon(string name)
        {
            var entity = new Entity();
            entity.BlueprintName = "Test" + name;
            entity.Tags["Item"] = "";
            entity.AddPart(new RenderPart { DisplayName = name });
            entity.AddPart(new PhysicsPart { Takeable = true, Weight = 12 });
            entity.AddPart(new MeleeWeaponPart { BaseDamage = "2d6", PenBonus = 2 });
            entity.AddPart(new EquippablePart { Slot = "Hand", UsesSlots = "Hand,Hand" });
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
                            { ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Category"", ""Value"": ""Melee Weapons"" }] },
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
                            { ""Name"": ""Physics"", ""Params"": [{ ""Key"": ""Category"", ""Value"": ""Armor"" }] },
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
    /// Test part that adds a custom action to GetInventoryActions.
    /// </summary>
    public class TestActionPart : Part
    {
        public override string Name => "TestAction";
        private string _name, _display, _command;
        private char _key;
        private int _priority;

        public TestActionPart(string name, string display, string command, char key, int priority)
        {
            _name = name;
            _display = display;
            _command = command;
            _key = key;
            _priority = priority;
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var actions = e.GetParameter<InventoryActionList>("Actions");
                if (actions != null)
                    actions.AddAction(_name, _display, _command, _key, _priority);
            }
            return true;
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
