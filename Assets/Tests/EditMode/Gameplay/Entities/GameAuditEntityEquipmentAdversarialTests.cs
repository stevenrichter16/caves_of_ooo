using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class GameAuditEntityEquipmentAdversarialTests
    {
        sealed class ThresholdRng : EquipmentContentRngBase
        {
            readonly int _roll;
            public ThresholdRng(int roll) { _roll = roll; }
            public override int Next(int maxValue) => maxValue == 100 ? _roll : 0;
        }
        // Deliberately distinct from the stock RNG: choosing a weapon must not alter stock rolls.
        class EquipmentContentRngBase : System.Random
        { public override int Next(int minValue, int maxValue) => minValue; }

        static List<Entity> Items(Entity actor) => actor.GetPart<InventoryPart>().Objects
            .Concat(actor.GetPart<InventoryPart>().GetAllEquipped()).Distinct().ToList();
        static string[] Units(IEnumerable<Entity> items) => items.SelectMany(x => Enumerable.Repeat(
            x.BlueprintName, x.GetPart<StackerPart>()?.StackCount ?? 1)).OrderBy(x => x).ToArray();
        static EntityEquipmentContentFixture Fixture() => new EntityEquipmentContentFixture();
        static List<MeleeWeaponPart> Weapons(Entity actor)
        {
            var slots = (IEnumerable)typeof(CombatSystem).GetMethod("GatherMeleeWeapons", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { actor, actor.GetPart<Body>() });
            return slots.Cast<object>().Select(x => (MeleeWeaponPart)x.GetType().GetField("Weapon").GetValue(x)).ToList();
        }
        static GameSessionState RoundTrip(Entity actor)
        {
            var zone = new Zone("Overworld.10.10.0"); Assert.IsTrue(zone.AddEntity(actor, 5, 5));
            var manager = new OverworldZoneManager(null, 333);
            manager.ReplaceLoadedState(new Dictionary<string, Zone> { { zone.ZoneID, zone } }, zone.ZoneID,
                new Dictionary<string, List<ZoneConnection>>());
            var turns = new TurnManager(); turns.RestoreSavedState(17, true, actor,
                new List<TurnManager.SavedTurnEntry> { new TurnManager.SavedTurnEntry { Entity = actor, Energy = 1000 } });
            return HotbarSaveFixture.RoundTrip(GameSessionState.Capture(Guid.NewGuid().ToString("N"), "equipment content", manager, turns, actor));
        }

        [TestCase("Snapjaw", "LeatherCap", 20)]
        [TestCase("SnapjawScavenger", "LeatherGloves", 35)]
        [TestCase("SnapjawHunter", "LeatherBoots", 35)]
        [TestCase("DesertBandit", "LeatherCap", 30)]
        [TestCase("RuinScavenger", "LeatherGloves", 35)]
        [TestCase("AmbushBandit", "LeatherArmor", 35)]
        [TestCase("RuneCultist", "LeatherGloves", 35)]
        public void Adversarial_OptionalArmorUsesExactProbabilityBoundary(string name, string optional, int chance)
        {
            // Hypothesis: a percent typo or inherited row makes optional armor guaranteed.
            using (var f = Fixture())
            {
                LoadoutPart.Rng = new ThresholdRng(chance - 1); var present = f.Create(name);
                LoadoutPart.Rng = new ThresholdRng(chance); var absent = f.Create(name);
                Assert.AreEqual(1, Items(present).Count(x => x.BlueprintName == optional));
                Assert.AreEqual(0, Items(absent).Count(x => x.BlueprintName == optional));
                CollectionAssert.AreEquivalent(Items(present).Where(x => x.BlueprintName != optional).Select(x => x.BlueprintName),
                    Items(absent).Select(x => x.BlueprintName));
                Assert.IsTrue(Weapons(absent).Any(x => x.ParentEntity.GetPart<EquippablePart>() != null));
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void Adversarial_ScavengerChoiceCannotRetainParentDaggerOrCap(bool gloves)
        {
            using (var f = Fixture())
            {
                LoadoutPart.Rng = new EquipmentContentRng { LastPick = true, OptionalAbsent = !gloves };
                var actor = f.Create("SnapjawScavenger");
                CollectionAssert.AreEquivalent(gloves ? new[] { "ShortSword", "LeatherGloves" } : new[] { "ShortSword" },
                    Items(actor).Select(x => x.BlueprintName));
                Assert.AreEqual(1, Weapons(actor).Count(x => x.ParentEntity.BlueprintName == "ShortSword"));
                Assert.AreEqual(1, Weapons(actor).Count(x => x.ParentEntity.HasTag("Natural")));
            }
        }

        [TestCase("Warden")] [TestCase("Quartermaster")] [TestCase("Weaponsmith")]
        [TestCase("Tinker")] [TestCase("Farmer")] [TestCase("WellKeeper")]
        [TestCase("Elder")] [TestCase("Scribe")] [TestCase("Merchant")]
        public void Adversarial_MaximumOpeningShelfSurvivesThePersonalKit(string name)
        {
            // Hypothesis: earlier grants suppress Trader, or an incoming unit merges into a shelf stack.
            using (var f = Fixture())
            {
                TraderPart.Rng = new EquipmentContentRng { MaximumCount = true };
                var actor = f.Create(name); var inv = actor.GetPart<InventoryPart>();
                var trader = actor.GetPart<TraderPart>();
                var expected = LootTableRegistry.Roll(trader.StockTable, new EquipmentContentRng { MaximumCount = true });
                Assert.Greater(expected.Count, 0);
                CollectionAssert.AreEqual(expected.OrderBy(x => x).ToArray(), Units(inv.Objects));
                Assert.Greater(inv.GetAllEquipped().Count, 0);
                foreach (var item in inv.GetAllEquipped())
                { Assert.IsFalse(expected.Contains(item.BlueprintName)); LoadoutLifecycleFixture.Equipped(actor, item); }
                Assert.Greater(TradeSystem.GetDrams(actor), 0);
                Assert.AreEqual(trader.StockTable, actor.GetProperty(TraderRestockSystem.ShopStockTableProp));
                Assert.LessOrEqual(inv.GetCarriedWeight(), 150);
            }
        }

        [TestCase("Warden", "Body", 6, 3, -1, 0)]
        [TestCase("Quartermaster", "Body", 5, 2, -1, 0)]
        [TestCase("SnapjawChieftain", "Body", 7, 4, -1, 0)]
        [TestCase("SnapjawWarlord", "Feet", 6, 4, -1, 5)]
        [TestCase("SkeletalSentry", "Head", 7, 5, 0, 0)]
        public void Adversarial_ArmorProtectsItsLocationAndRemovalReversesOnlyItsEffects(
            string name, string slot, int armored, int natural, int dv, int speed)
        {
            using (var f = Fixture())
            {
                var actor = f.Create(name); var body = actor.GetPart<Body>();
                var part = body.GetPartsByType(slot).Single(); var item = part._Equipped;
                Assert.NotNull(item); Assert.AreEqual(armored, CombatSystem.GetPartAV(actor, part));
                var bare = body.GetPartsByType("Arm").First(); Assert.AreEqual(natural, CombatSystem.GetPartAV(actor, bare));
                Assert.AreEqual(6 + StatUtils.GetModifier(actor, "Agility") + dv, CombatSystem.GetDV(actor));
                Assert.AreEqual(speed, actor.GetStat("Speed").Penalty);
                int armorDv = item.GetPart<ArmorPart>().DV;
                Assert.IsTrue(InventorySystem.UnequipItem(actor, item));
                Assert.AreEqual(natural, CombatSystem.GetPartAV(actor, part));
                Assert.AreEqual(6 + StatUtils.GetModifier(actor, "Agility") + dv - armorDv, CombatSystem.GetDV(actor));
                Assert.AreEqual(0, actor.GetStat("Speed").Penalty);
            }
        }

        [TestCase("SnapjawWarlord", "2d5", 2, "Cutting Axe")]
        [TestCase("SkeletalSentry", "1d6+1", 1, "Cutting")]
        public void Adversarial_ArmorOnlyEnemiesKeepBothStrongerNaturalHands(string name, string damage, int pen, string attributes)
        {
            using (var f = Fixture())
            {
                var actor = f.Create(name); var weapons = Weapons(actor); Assert.AreEqual(2, weapons.Count);
                foreach (var weapon in weapons)
                { Assert.IsTrue(weapon.ParentEntity.HasTag("Natural")); Assert.AreEqual(damage, weapon.BaseDamage); Assert.AreEqual(pen, weapon.PenBonus); Assert.AreEqual(attributes, weapon.Attributes); }
                Assert.IsFalse(actor.GetPart<InventoryPart>().GetAllEquipped().Any(x => x.GetPart<MeleeWeaponPart>() != null));
            }
        }

        [TestCase("Snapjaw", "Dagger", "1d4", 1, "Piercing")]
        [TestCase("DesertBandit", "ShortSword", "1d6", 1, "Cutting LongBlades")]
        [TestCase("Warden", "LongSword", "1d8", 2, "Cutting LongBlades")]
        [TestCase("Quartermaster", "Spear", "1d6+1", 2, "Piercing")]
        public void Adversarial_RealMeleeSelectionUsesGearAndKeepsOnlyTheOtherNaturalHand(
            string name, string itemName, string damage, int pen, string attributes)
        {
            using (var f = Fixture())
            {
                var actor = f.Create(name); var weapons = Weapons(actor);
                Assert.AreEqual(2, weapons.Count);
                var item = actor.GetPart<InventoryPart>().GetAllEquipped().Single(x => x.BlueprintName == itemName);
                var weapon = item.GetPart<MeleeWeaponPart>(); Assert.IsTrue(weapons.Contains(weapon));
                Assert.AreEqual(damage, weapon.BaseDamage); Assert.AreEqual(pen, weapon.PenBonus); Assert.AreEqual(attributes, weapon.Attributes);
                Assert.AreEqual(1, weapons.Count(x => x.ParentEntity.HasTag("Natural")));
                Assert.IsTrue(InventorySystem.UnequipItem(actor, item));
                Assert.AreEqual(2, Weapons(actor).Count(x => x.ParentEntity.HasTag("Natural")));
            }
        }

        [TestCase("SnapjawWarlord")] [TestCase("Quartermaster")]
        [TestCase("PeatCutter")] [TestCase("TentRightHost")]
        public void Adversarial_ActualKitsRoundTripWithoutNewIDsOrRegrant(string name)
        {
            using (var f = Fixture())
            {
                var actor = f.Create(name); var before = Items(actor).OrderBy(x => x.ID).Select(x => x.ID + ":" + x.BlueprintName + ":" + (x.GetPart<StackerPart>()?.StackCount ?? 1)).ToArray();
                var equippedIds = actor.GetPart<InventoryPart>().GetAllEquipped().Select(x => x.ID).OrderBy(x => x).ToArray();
                int speed = actor.GetStat("Speed").Penalty; f.Messages.Clear();
                var loaded = RoundTrip(actor).Player;
                CollectionAssert.AreEqual(before, Items(loaded).OrderBy(x => x.ID).Select(x => x.ID + ":" + x.BlueprintName + ":" + (x.GetPart<StackerPart>()?.StackCount ?? 1)).ToArray());
                CollectionAssert.AreEqual(equippedIds, loaded.GetPart<InventoryPart>().GetAllEquipped().Select(x => x.ID).OrderBy(x => x).ToArray());
                foreach (var item in loaded.GetPart<InventoryPart>().GetAllEquipped()) LoadoutLifecycleFixture.Equipped(loaded, item);
                Assert.AreEqual(speed, loaded.GetStat("Speed").Penalty); Assert.IsFalse(f.Messages.Any(x => x.Contains(" equips ")));
            }
        }

        [Test]
        public void Adversarial_OlderSavedActorIsNotBackfilledFromCurrentBlueprint()
        {
            using (var f = Fixture())
            {
                var bp = f.Factory.Blueprints["SnapjawWarlord"]; var loadout = bp.Parts["Loadout"];
                bp.Parts.Remove("Loadout"); var oldActor = f.Create("SnapjawWarlord"); bp.Parts.Add("Loadout", loadout);
                Assert.AreEqual(0, Items(oldActor).Count);
                var loaded = RoundTrip(oldActor).Player;
                Assert.AreEqual(0, Items(loaded).Count); Assert.IsNull(loaded.GetPart<LoadoutPart>());
                Assert.AreEqual(0, loaded.GetStat("Speed").Penalty);
                Assert.AreEqual(3, Items(f.Create("SnapjawWarlord")).Count);
            }
        }

        [TestCase("SnapjawWarlord")] [TestCase("TentRightHost")]
        public void Adversarial_DeathDropsExactKitOnceAndAnotherActorCanUseIt(string name)
        {
            using (var f = Fixture())
            {
                var actor = f.Create(name); var items = Items(actor); var zone = new Zone("GA03i-death");
                Assert.IsTrue(zone.AddEntity(actor, 5, 5));
                CombatSystem.ApplyDamage(actor, 10000, null, zone);
                Assert.IsTrue(CombatSystem.IsDeathHandled(actor)); Assert.AreEqual(0, Items(actor).Count);
                foreach (var item in items)
                { Assert.AreEqual((5, 5), zone.GetEntityPosition(item)); Assert.IsNull(item.GetPart<PhysicsPart>().Equipped); Assert.IsNull(item.GetPart<PhysicsPart>().InInventory); }
                CombatSystem.HandleDeath(actor, null, zone);
                foreach (var item in items) Assert.AreEqual(1, zone.GetReadOnlyEntities().Count(x => ReferenceEquals(x, item)));
                var player = f.Create("Player"); Assert.IsTrue(zone.AddEntity(player, 5, 5));
                var gear = items.First(x => x.GetPart<EquippablePart>() != null); f.Messages.Clear();
                Assert.IsTrue(InventorySystem.Pickup(player, gear, zone)); LoadoutLifecycleFixture.Equipped(player, gear);
                Assert.AreEqual(1, f.Messages.Count(x => x == player.GetDisplayName() + " equips " + gear.GetDisplayName() + "."));
            }
        }

        [Test]
        public void Adversarial_IdenticalActorsNeverShareEquipmentReferences()
        {
            using (var f = Fixture())
            {
                var a = f.Create("SnapjawWarlord"); var b = f.Create("SnapjawWarlord");
                var aItems = Items(a); var bItems = Items(b);
                Assert.AreEqual(0, aItems.Intersect(bItems).Count()); Assert.AreEqual(6, aItems.Concat(bItems).Select(x => x.ID).Distinct().Count());
                Assert.IsTrue(InventorySystem.UnequipItem(a, aItems.Single(x => x.BlueprintName == "IronshodBoots")));
                Assert.AreEqual(0, a.GetStat("Speed").Penalty); Assert.AreEqual(5, b.GetStat("Speed").Penalty);
                foreach (var item in bItems) LoadoutLifecycleFixture.Equipped(b, item);
            }
        }

        [Test]
        public void Adversarial_RestockDoesNotCountOrMergeWornGearAsShelfStock()
        {
            using (var f = Fixture())
            {
                var old = TraderRestockSystem.Factory;
                try
                {
                    TraderRestockSystem.Factory = f.Factory;
                    var actor = f.Create("Weaponsmith"); var inv = actor.GetPart<InventoryPart>();
                    foreach (var item in inv.Objects.ToArray()) Assert.IsTrue(inv.RemoveObject(item));
                    var worn = inv.GetAllEquipped().Single(x => x.BlueprintName == "LeatherBoots");
                    var table = LootTableRegistry.Get(actor.GetPart<TraderPart>().StockTable);
                    var savedEntries = table.Entries;
                    table.Entries = new List<LootEntryData> { new LootEntryData { Blueprint = "LeatherBoots" } };
                    try
                    {
                        var zone = new Zone("GA03i-restock"); Assert.IsTrue(zone.AddEntity(actor, 5, 5));
                        Assert.Greater(TraderRestockSystem.RestockZone(zone, 400), 0);
                        var shelf = inv.Objects.Single(); Assert.AreEqual("LeatherBoots", shelf.BlueprintName);
                        Assert.AreNotSame(worn, shelf); Assert.AreEqual(1, worn.GetPart<StackerPart>().StackCount); Assert.AreEqual(1, shelf.GetPart<StackerPart>().StackCount);
                        LoadoutLifecycleFixture.Equipped(actor, worn); LoadoutLifecycleFixture.Carried(actor, shelf);
                        Assert.AreEqual(0, TraderRestockSystem.RestockZone(zone, 400)); Assert.AreEqual(1, inv.Objects.Count);
                    }
                    finally { table.Entries = savedEntries; }
                }
                finally { TraderRestockSystem.Factory = old; }
            }
        }

        [Test]
        public void Adversarial_ActualVillageKeepsRentalStockSeparateAndCreationQuiet()
        {
            using (var f = Fixture())
            {
                var poi = new PointOfInterest(POIType.Village, "Starting Village", "Villagers");
                var zone = new Zone(WorldMap.StartingZoneID);
                Assert.IsTrue(new VillageBuilder(BiomeType.Cave, poi).BuildZone(zone, f.Factory, new System.Random(42)));
                Assert.IsTrue(new VillagePopulationBuilder(poi).BuildZone(zone, f.Factory, new System.Random(42)));
                Assert.IsTrue(new TradeStockBuilder().BuildZone(zone, f.Factory, new System.Random(42)));
                var qm = zone.GetReadOnlyEntities().Single(x => x.BlueprintName == "Quartermaster"); var inv = qm.GetPart<InventoryPart>();
                var spear = inv.GetAllEquipped().Single(x => x.BlueprintName == "Spear"); Assert.IsFalse(RentalSystem.IsRentable(spear));
                foreach (string name in new[] { "LoanerDagger", "LoanerSpear", "LoanerLongsword" })
                { var loaner = inv.Objects.Single(x => x.BlueprintName == name); Assert.IsTrue(RentalSystem.IsRentable(loaner)); Assert.AreNotSame(spear, loaner); }
                Assert.Greater(inv.Objects.Count, 3); Assert.LessOrEqual(inv.GetCarriedWeight(), 150);
                Assert.IsFalse(f.Messages.Any(x => x.Contains(" equips ")));
            }
        }

        [Test]
        public void Adversarial_OnlySelectedContentGetsLoadoutsAndDerivedHumanoidLoot()
        {
            using (var f = Fixture())
            {
                var selected = GameAuditEntityEquipmentContentTests.Kits.Select(x => x.Blueprint).ToArray();
                CollectionAssert.AreEquivalent(selected, f.Factory.Blueprints.Where(x => x.Value.Parts.ContainsKey("Loadout")).Select(x => x.Key));
                foreach (var name in selected) Assert.AreEqual(LootDropSystem.ClassHumanoid, LootDropSystem.ResolveClass(f.Create(name)));
                foreach (var name in new[] { "Villager", "SummitSinger", "Armorer" })
                { var actor = f.Create(name); Assert.IsNull(actor.GetPart<LoadoutPart>()); Assert.AreEqual(0, actor.GetPart<InventoryPart>().GetAllEquipped().Count); }
            }
        }
    }
}
