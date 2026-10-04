using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    // Actual cold-generated yard and native command/save bridge. Player placement
    // and typed combat damage are explicit controlled fixture setup; this does
    // not claim an ordinary input journey or naturally earned combat result.
    public sealed class MendleafDryingYardPersistenceTests
    {
        const string North = "Overworld.11.9.0", Role = "SpreadWorksite.Role";
        static int Units(Entity e) => e.GetPart<StackerPart>()?.StackCount ?? 1;
        static int Packed(Entity e, string blueprint) => e.GetPart<InventoryPart>().Objects.Where(i => i.BlueprintName == blueprint).Sum(Units);
        static string Stock(Entity e) => string.Join("|", e.GetPart<InventoryPart>().Objects.OrderBy(i => i.ID, StringComparer.Ordinal)
            .Select(i => i.ID + ":" + i.BlueprintName + ":" + Units(i) + ":" + i.GetPart<PhysicsPart>()?.InInventory?.ID));
        static void Place(Zone zone, Entity actor, (int x, int y) at)
        {
            if (zone.GetEntityCell(actor) != null) Assert.True(zone.RemoveEntity(actor));
            Assert.True(zone.AddEntity(actor, at.x, at.y));
        }
        static void AssertOwners(Entity owner, Zone zone)
        {
            foreach (var item in owner.GetPart<InventoryPart>().Objects)
            {
                Assert.AreSame(owner, item.GetPart<PhysicsPart>().InInventory);
                Assert.Null(item.GetPart<PhysicsPart>().Equipped); Assert.Null(zone.GetEntityCell(item)); Assert.Greater(Units(item), 0);
            }
        }

        [TestCase("herb-only")]
        [TestCase("used")]
        [TestCase("recovered")]
        public void ActualFiniteRemedyAndOriginalBottleOutcomeSurviveCachedReturnAndReplacementGraph(string outcome)
        {
            using (var scope = new HaulingContentScope())
            {
                var oldHarvest = HarvestablePart.Factory; var oldCorpse = CorpsePart.Factory;
                HarvestablePart.Factory = CorpsePart.Factory = scope.Factory;
                try
                {
                    var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, true);
                    scope.Seed(unchecked(64 ^ FormationSelector.StableIndex(North, int.MaxValue)));
                    var zone = manager.GetZone(North); manager.SetActiveZone(zone);
                    Assert.AreEqual(2, manager.Exploration.DispositionFor(North));
                    var herb = zone.GetReadOnlyEntities().Single(e => e.GetProperty(Role) == "medicine-forage");
                    var still = zone.GetReadOnlyEntities().Single(e => e.GetProperty(Role) == "still");
                    var medic = zone.GetReadOnlyEntities().Single(e => e.GetProperty(Role) == "patchbearer");
                    var shelves = zone.GetReadOnlyEntities().Where(e => e.GetProperty(Role) == "drying-shelf").ToArray();
                    Assert.AreEqual(2, shelves.Length); Assert.True(shelves.All(e => e.GetPart<ContainerPart>().Contents.Count == 0));
                    var medicine = medic.GetPart<FieldMedicinePart>(); var bottle = medicine.FindCarriedMedicine();
                    Assert.NotNull(bottle); Assert.AreEqual(1, Units(bottle)); Assert.AreEqual("4d6+4", bottle.GetPart<TonicPart>().Healing);
                    var player = scope.Factory.CreateEntity("Player"); Assert.AreEqual(0, Packed(player, "MendleafSprig"));
                    var h = zone.GetEntityPosition(herb);
                    var approach = Enumerable.Range(-1, 3).SelectMany(dx => Enumerable.Range(-1, 3).Select(dy => (x: h.x + dx, y: h.y + dy)))
                        .First(p => zone.CanPlaceFootprint(player, p.x, p.y) && Math.Max(Math.Abs(p.x - h.x), Math.Abs(p.y - h.y)) == 1);
                    Place(zone, player, approach); Assert.False(AlchemyStillPart.IsNearStill(player, zone));
                    Assert.True(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(herb, "Harvest"), player, zone).Success);
                    int earned = Packed(player, "MendleafSprig"); Assert.That(earned, Is.InRange(1, 2));
                    Assert.True(herb.GetPart<HarvestablePart>().Harvested); Assert.Null(zone.GetEntityCell(herb));
                    Assert.False(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(herb, "Harvest"), player, zone).Success);
                    Assert.AreEqual(earned, Packed(player, "MendleafSprig"));
                    var sprig = player.GetPart<InventoryPart>().Objects.Single(e => e.BlueprintName == "MendleafSprig");
                    Assert.True(InventorySystem.ExecuteCommand(new BrewReagentsCommand(new[] { sprig }, scope.Factory), player, zone).Success);
                    Assert.AreEqual(earned - 1, Packed(player, "MendleafSprig"));
                    var brew = player.GetPart<InventoryPart>().Objects.Single(e => e.BlueprintName == "BrewedTonic");
                    Assert.AreEqual("1d4", brew.GetPart<TonicPart>().Healing); Assert.AreEqual(1, Units(brew));
                    Assert.AreSame(bottle, medicine.FindCarriedMedicine());

                    var m = zone.GetEntityPosition(medic); var o = zone.GetEntityPosition(still);
                    int ax = (m.x - o.x) / 6, ay = (m.y - o.y) / 6;
                    Place(zone, player, (m.x - ax, m.y - ay));
                    var brain = medic.GetPart<BrainPart>(); brain.CurrentZone = zone; brain.SetPersonallyHostile(player);
                    Assert.AreEqual(20, medic.GetStatValue("Hitpoints"));
                    Assert.False(medicine.TryUseMedicine(player, zone, new FieldMedicineFixture.MedicineRandom()));
                    Assert.AreSame(bottle, medicine.FindCarriedMedicine());
                    if (outcome == "used")
                    {
                        CombatSystem.ApplyDamage(medic, new Damage(12), player, zone);
                        Assert.AreEqual(8, medic.GetStatValue("Hitpoints"));
                        Assert.True(medicine.TryUseMedicine(player, zone, new FieldMedicineFixture.MedicineRandom()));
                        Assert.AreEqual(16, medic.GetStatValue("Hitpoints")); Assert.Null(medicine.FindCarriedMedicine());
                        CombatSystem.ApplyDamage(medic, new Damage(8), player, zone);
                        Assert.False(medicine.TryUseMedicine(player, zone, new FieldMedicineFixture.MedicineRandom()), "A second low-health opportunity cannot refill the original bottle.");
                        Assert.AreEqual(8, medic.GetStatValue("Hitpoints"));
                        Assert.False(zone.GetReadOnlyEntities().Contains(bottle));
                    }
                    else if (outcome == "recovered")
                    {
                        CombatSystem.ApplyDamage(medic, new Damage(1000), player, zone);
                        Assert.True(CombatSystem.IsDeathHandled(medic)); Assert.Null(zone.GetEntityCell(medic));
                        Assert.AreEqual(m, zone.GetEntityPosition(bottle)); Assert.Null(bottle.GetPart<PhysicsPart>().InInventory);
                        Place(zone, player, m); int before = Packed(player, "HealingTonic");
                        Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(bottle), player, zone).Success);
                        Assert.AreEqual(before + 1, Packed(player, "HealingTonic"));
                        Assert.Null(zone.GetEntityCell(bottle));
                    }
                    else Assert.AreSame(bottle, medicine.FindCarriedMedicine());

                    string inventory = Stock(player); int savedHp = medic.GetStatValue("Hitpoints");
                    string medicalStock = Stock(medic); var originalItems = player.GetPart<InventoryPart>().Objects.ToArray();
                    AssertOwners(player, zone);
                    var away = new Zone("Overworld.0.0.0"); Assert.True(zone.RemoveEntity(player)); Assert.True(away.AddEntity(player, 1, 1)); manager.SetActiveZone(away);
                    Assert.AreSame(zone, manager.GetZone(North)); Assert.False(zone.GetReadOnlyEntities().Any(e => e.ID == herb.ID));
                    Assert.AreEqual(inventory, Stock(player)); Assert.AreEqual(medicalStock, Stock(medic));
                    var restored = HotbarSaveFixture.RoundTrip(GameSessionState.Capture("mendleaf-" + outcome, "fixture", manager, null, player));
                    Assert.AreNotSame(player, restored.Player); Assert.AreEqual(inventory, Stock(restored.Player));
                    Assert.AreEqual(away.ZoneID, restored.ZoneManager.ActiveZone.ZoneID);
                    var loaded = restored.ZoneManager.GetZone(North); Assert.AreNotSame(zone, loaded);
                    Assert.False(loaded.GetReadOnlyEntities().Any(e => e.ID == herb.ID || e.GetProperty(Role) == "medicine-forage"));
                    var loadedStill = loaded.GetReadOnlyEntities().Single(e => e.ID == still.ID); Assert.AreNotSame(still, loadedStill);
                    Assert.AreEqual(o, loaded.GetEntityPosition(loadedStill));
                    foreach (var shelf in shelves)
                    {
                        var current = loaded.GetReadOnlyEntities().Single(e => e.ID == shelf.ID); Assert.AreNotSame(shelf, current);
                        Assert.AreEqual(zone.GetEntityPosition(shelf), loaded.GetEntityPosition(current)); Assert.IsEmpty(current.GetPart<ContainerPart>().Contents);
                    }
                    var loadedMedic = loaded.GetReadOnlyEntities().SingleOrDefault(e => e.ID == medic.ID);
                    if (outcome == "recovered") Assert.Null(loadedMedic);
                    else
                    {
                        Assert.NotNull(loadedMedic); Assert.AreNotSame(medic, loadedMedic);
                        Assert.AreEqual(savedHp, loadedMedic.GetStatValue("Hitpoints")); Assert.AreEqual(medicalStock, Stock(loadedMedic));
                        var loadedBottle = loadedMedic.GetPart<FieldMedicinePart>().FindCarriedMedicine();
                        if (outcome == "used") Assert.Null(loadedBottle);
                        else { Assert.NotNull(loadedBottle); Assert.AreEqual(bottle.ID, loadedBottle.ID); Assert.AreNotSame(bottle, loadedBottle); Assert.AreEqual(1, Units(loadedBottle)); }
                        AssertOwners(loadedMedic, loaded);
                    }
                    Assert.AreEqual(earned - 1, Packed(restored.Player, "MendleafSprig"));
                    var loadedBrew = restored.Player.GetPart<InventoryPart>().Objects.Single(e => e.ID == brew.ID);
                    Assert.AreNotSame(brew, loadedBrew); Assert.AreEqual(1, Units(loadedBrew)); Assert.AreEqual("1d4", loadedBrew.GetPart<TonicPart>().Healing);
                    AssertOwners(restored.Player, loaded);
                    foreach (var item in originalItems) Assert.AreNotSame(item, restored.Player.GetPart<InventoryPart>().Objects.Single(e => e.ID == item.ID));
                    Assert.True(restored.ZoneManager.ActiveZone.RemoveEntity(restored.Player)); Assert.True(loaded.AddEntity(restored.Player, approach.x, approach.y));
                    restored.ZoneManager.SetActiveZone(loaded); Assert.AreSame(loaded, restored.ZoneManager.GetZone(North));
                    Assert.AreEqual(inventory, Stock(restored.Player)); Assert.False(loaded.GetReadOnlyEntities().Any(e => e.BlueprintName == "MendleafPlant"));
                    Assert.AreEqual(outcome == "herb-only" ? 1 : 0, loaded.GetReadOnlyEntities().Where(e => e.HasPart<FieldMedicinePart>())
                        .Sum(e => Packed(e, "HealingTonic")), "Cached return must not reissue the enemy's spent or recovered bottle.");
                }
                finally { HarvestablePart.Factory = oldHarvest; CorpsePart.Factory = oldCorpse; }
            }
        }
    }
}
