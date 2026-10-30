using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Anatomy;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class TrinketSlotTests
    {
        static Entity Trinket(EquipmentLifecycleFixture fixture, string bonuses = "Agility:2")
        {
            var item = new Entity { ID = Guid.NewGuid().ToString("N"), BlueprintName = "TrinketSlotTestItem" };
            item.SetTag("Item");
            item.AddPart(new RenderPart { DisplayName = "test trinket", RenderString = "*" });
            item.AddPart(new PhysicsPart { Takeable = true, Weight = 1 });
            item.AddPart(new EquippablePart { Slot = TrinketSlots.SlotType, EquipBonuses = bonuses });
            Assert.True(fixture.Inventory.AddObject(item));
            return item;
        }

        [Test]
        public void FactoryGivesPlayersTwoSlotsWithoutChangingOtherHumanoidAnatomies()
        {
            using (var f = new EntityEquipmentContentFixture())
            {
                var player = f.Create("Player");
                Assert.AreEqual(2, TrinketSlots.GetCapacity(player));
                Assert.AreEqual(2, player.GetIntProperty(TrinketSlots.CapacityProperty));
                Assert.AreEqual(0, TrinketSlots.GetCapacity(f.Create("MarlbackScrabbler")));
                Assert.AreEqual(0, AnatomyFactory.CreateHumanoid().CountParts(TrinketSlots.SlotType));
                CollectionAssert.AreEqual(new[] { "trinket 1", "trinket 2" },
                    player.GetPart<Body>().GetPartsByType(TrinketSlots.SlotType).Select(p => p.GetDisplayName()));
            }
        }

        [Test]
        public void ExpansionAndRepeatedInitializationPreserveExistingSlotsAndWornOwners()
        {
            using (var f = new EquipmentLifecycleFixture())
            {
                TrinketSlots.InitializePlayer(f.Actor);
                var original = f.Body.GetPartsByType(TrinketSlots.SlotType).ToArray();
                int[] ids = original.Select(p => p.ID).ToArray();
                var item = Trinket(f);
                Assert.True(InventorySystem.Equip(f.Actor, item, original[1]));

                Assert.True(TrinketSlots.EnsureMinimum(f.Actor, 4));
                Assert.True(TrinketSlots.EnsureMinimum(f.Actor, 4));
                Assert.True(TrinketSlots.EnsureMinimum(f.Actor, 1));
                TrinketSlots.InitializePlayer(f.Actor);
                var slots = f.Body.GetPartsByType(TrinketSlots.SlotType);
                Assert.AreEqual(4, slots.Count);
                Assert.AreEqual(4, f.Actor.GetIntProperty(TrinketSlots.CapacityProperty));
                CollectionAssert.AreEqual(original, slots.Take(2));
                CollectionAssert.AreEqual(ids, slots.Take(2).Select(p => p.ID));
                Assert.AreSame(item, slots[1]._Equipped);
                Assert.AreSame(f.Actor, item.GetPart<PhysicsPart>().Equipped);
                Assert.AreEqual(16, f.Actor.GetStatValue("Agility"));
                Assert.AreEqual(4, slots.Select(p => p.ID).Distinct().Count());
            }
        }

        [Test]
        public void ThirdItemReplacesOnlyTheChosenTrinketAndUsesNormalUnequip()
        {
            using (var f = new EquipmentLifecycleFixture())
            {
                TrinketSlots.InitializePlayer(f.Actor);
                var slots = f.Body.GetPartsByType(TrinketSlots.SlotType);
                var first = Trinket(f); var second = Trinket(f); var replacement = Trinket(f, "Agility:-1");
                Assert.True(InventorySystem.Equip(f.Actor, first, slots[0]));
                Assert.True(InventorySystem.Equip(f.Actor, second, slots[1]));
                Assert.AreEqual(18, f.Actor.GetStatValue("Agility"));
                Assert.True(InventorySystem.Equip(f.Actor, replacement, slots[0]));
                Assert.AreSame(replacement, slots[0]._Equipped);
                Assert.AreSame(second, slots[1]._Equipped);
                Assert.True(f.Inventory.Objects.Contains(first));
                Assert.AreEqual(15, f.Actor.GetStatValue("Agility"));
                Assert.True(InventorySystem.UnequipItem(f.Actor, replacement));
                Assert.True(InventorySystem.UnequipItem(f.Actor, second));
                Assert.AreEqual(14, f.Actor.GetStatValue("Agility"));
                Assert.AreEqual(2, TrinketSlots.GetCapacity(f.Actor));
            }
        }

        [Test]
        public void OrdinaryEquipmentCannotOccupyTrinketSlotsAndTrinketsCannotOccupyHands()
        {
            using (var f = new EquipmentLifecycleFixture())
            {
                TrinketSlots.InitializePlayer(f.Actor);
                var trinket = Trinket(f); var dagger = f.Item("Dagger");
                Assert.True(f.Inventory.AddObject(dagger));
                Assert.False(InventorySystem.Equip(f.Actor, dagger, f.Body.GetPartsByType(TrinketSlots.SlotType)[0]));
                Assert.False(InventorySystem.Equip(f.Actor, trinket, f.LeftHand));
                Assert.AreEqual(14, f.Actor.GetStatValue("Agility"));
                Assert.True(f.Inventory.Objects.Contains(trinket));
                Assert.True(f.Inventory.Objects.Contains(dagger));
            }
        }

        [Test]
        public void TrinketPositionsAddNoHitLocationsSeverableLimbsNaturalAttacksOrMobility()
        {
            using (var f = new EquipmentLifecycleFixture())
            {
                var originalTargets = f.Body.GetParts().Where(p => !p.Abstract && p.TargetWeight > 0).ToArray();
                int originalMobility = f.Body.GetParts().Sum(p => p.Mobility);
                int originalAttacks = f.Body.GetParts().Count(p => !string.IsNullOrEmpty(p.DefaultBehaviorBlueprint));
                Assert.True(TrinketSlots.EnsureMinimum(f.Actor, 4));
                foreach (var slot in f.Body.GetPartsByType(TrinketSlots.SlotType))
                {
                    Assert.False(slot.Contact);
                    Assert.True(slot.Integral);
                    Assert.False(slot.IsSeverable());
                    Assert.False(f.Body.Dismember(slot, f.Zone));
                    Assert.AreEqual(0, slot.TargetWeight);
                    Assert.IsNull(slot._DefaultBehavior);
                    Assert.IsNull(slot.DefaultBehaviorBlueprint);
                }
                CollectionAssert.AreEqual(originalTargets, f.Body.GetParts().Where(p => !p.Abstract && p.TargetWeight > 0));
                Assert.AreEqual(originalMobility, f.Body.GetParts().Sum(p => p.Mobility));
                Assert.AreEqual(originalAttacks, f.Body.GetParts().Count(p => !string.IsNullOrEmpty(p.DefaultBehaviorBlueprint)));
                var rng = new Random(17);
                for (int i = 0; i < 100; i++) Assert.AreNotEqual(TrinketSlots.SlotType, CombatSystem.SelectHitLocation(f.Body, rng).Type);
            }
        }

        [Test]
        public void EquipmentViewIncludesEachSlotAndGivesItADistinctPaperdollPosition()
        {
            using (var f = new EquipmentLifecycleFixture())
            {
                Assert.True(TrinketSlots.EnsureMinimum(f.Actor, 4));
                var item = Trinket(f);
                Assert.True(InventorySystem.Equip(f.Actor, item, f.Body.GetPartsByType(TrinketSlots.SlotType)[1]));
                var slots = InventoryScreenData.Build(f.Actor).Equipment.Where(s => s.SlotName == TrinketSlots.SlotType).ToArray();
                Assert.AreEqual(4, slots.Length);
                Assert.AreSame(item, slots[1].EquippedItem);
                Assert.AreEqual(4, slots.Select(s => (s.GridX, s.GridY)).Distinct().Count());
                Assert.True(slots.All(s => s.GridX > 0 && s.GridY > 0 && !string.IsNullOrEmpty(s.ShortLabel)));
            }
        }

        [Test]
        public void SessionSaveLoadPreservesExpandedSlotsTheirIdsAndTheWornBonusExactlyOnce()
        {
            using (var f = new EquipmentLifecycleFixture())
            {
                Assert.True(TrinketSlots.EnsureMinimum(f.Actor, 4));
                var slots = f.Body.GetPartsByType(TrinketSlots.SlotType);
                int[] ids = slots.Select(p => p.ID).ToArray();
                var item = Trinket(f);
                Assert.True(InventorySystem.Equip(f.Actor, item, slots[2]));
                var state = f.RoundTrip();
                for (int cycle = 0; cycle < 2; cycle++)
                {
                    var savedSlots = state.Player.GetPart<Body>().GetPartsByType(TrinketSlots.SlotType);
                    CollectionAssert.AreEqual(ids, savedSlots.Select(p => p.ID));
                    Assert.AreEqual(4, state.Player.GetIntProperty(TrinketSlots.CapacityProperty));
                    Assert.AreEqual(item.ID, savedSlots[2]._Equipped.ID);
                    Assert.AreSame(state.Player, savedSlots[2]._Equipped.GetPart<PhysicsPart>().Equipped);
                    Assert.AreEqual(16, state.Player.GetStatValue("Agility"));
                    if (cycle == 0) state = HotbarSaveFixture.RoundTrip(state);
                }
                var loadedItem = state.Player.GetPart<Body>().GetPartsByType(TrinketSlots.SlotType)[2]._Equipped;
                Assert.True(InventorySystem.UnequipItem(state.Player, loadedItem));
                Assert.AreEqual(14, state.Player.GetStatValue("Agility"));
            }
        }

        [Test]
        public void OldSavedPlayerGetsStartingSlotsOnceWithoutChangingItsExistingAnatomy()
        {
            using (var f = new EquipmentLifecycleFixture())
            {
                Assert.AreEqual(0, TrinketSlots.GetCapacity(f.Actor));
                Assert.False(f.Actor.IntProperties.ContainsKey(TrinketSlots.CapacityProperty));
                int[] oldIds = f.Body.GetParts().Select(p => p.ID).ToArray();
                var state = f.RoundTrip();
                Assert.AreEqual(2, TrinketSlots.GetCapacity(state.Player));
                CollectionAssert.AreEqual(oldIds, state.Player.GetPart<Body>().GetParts()
                    .Where(p => p.Type != TrinketSlots.SlotType).Select(p => p.ID));
                int[] slotIds = state.Player.GetPart<Body>().GetPartsByType(TrinketSlots.SlotType).Select(p => p.ID).ToArray();
                var again = HotbarSaveFixture.RoundTrip(state);
                CollectionAssert.AreEqual(slotIds, again.Player.GetPart<Body>().GetPartsByType(TrinketSlots.SlotType).Select(p => p.ID));
            }
        }

        [Test]
        public void SavedCapacityCanBeRestoredOntoAReplacementAnatomy()
        {
            using (var f = new EquipmentLifecycleFixture())
            {
                Assert.True(TrinketSlots.EnsureMinimum(f.Actor, 4));
                f.Body.SetBody(AnatomyFactory.CreateSimple());
                TrinketSlots.InitializePlayer(f.Actor);
                Assert.AreEqual(4, TrinketSlots.GetCapacity(f.Actor));
                Assert.AreEqual(0, f.Body.CountParts("Hand"));
            }
        }

        [TestCase(-1)] [TestCase(int.MaxValue)]
        public void InvalidExpansionDoesNotChangeExistingSlotsOrCapacity(int requested)
        {
            using (var f = new EquipmentLifecycleFixture())
            {
                TrinketSlots.InitializePlayer(f.Actor);
                var slots = f.Body.GetPartsByType(TrinketSlots.SlotType).ToArray();
                Assert.False(TrinketSlots.EnsureMinimum(f.Actor, requested));
                CollectionAssert.AreEqual(slots, f.Body.GetPartsByType(TrinketSlots.SlotType));
                Assert.AreEqual(2, f.Actor.GetIntProperty(TrinketSlots.CapacityProperty));
            }
        }

        [Test]
        public void MissingBodyAndNonplayerInitializationDoNotInventAnAnatomy()
        {
            var actor = new Entity(); actor.SetTag("Player");
            Assert.False(TrinketSlots.EnsureMinimum(actor, 2));
            TrinketSlots.InitializePlayer(actor);
            Assert.False(actor.IntProperties.ContainsKey(TrinketSlots.CapacityProperty));
            var body = new Body(); actor.AddPart(body); body.SetBody(AnatomyFactory.CreateHumanoid());
            actor.Tags.Remove("Player");
            TrinketSlots.InitializePlayer(actor);
            Assert.AreEqual(0, TrinketSlots.GetCapacity(actor));
            Assert.False(actor.IntProperties.ContainsKey(TrinketSlots.CapacityProperty));
        }
    }
}
