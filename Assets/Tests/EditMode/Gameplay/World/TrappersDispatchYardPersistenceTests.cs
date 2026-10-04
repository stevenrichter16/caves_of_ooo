using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    // Generated-yard command/save bridge, not an ordinary native journey.
    // Actor placement and the initial two timber are explicit fixture setup;
    // the actual generated yard, cache stock and source actors are not edited.
    public sealed class TrappersDispatchYardPersistenceTests
    {
        const string YardID = "Overworld.11.11.0";
        const string RoleKey = "SpreadWorksite.Role";
        static int Units(Entity e) => e.GetPart<StackerPart>()?.StackCount ?? 1;
        static int Packed(Entity actor, string blueprint) => actor.GetPart<InventoryPart>().Objects
            .Where(e => e.BlueprintName == blueprint).Sum(Units);
        static string Inventory(Entity actor) => string.Join("|", actor.GetPart<InventoryPart>().Objects
            .OrderBy(e => e.ID, StringComparer.Ordinal).Select(e => e.ID + ":" + e.BlueprintName + ":" + Units(e)
                + ":" + e.GetPart<PhysicsPart>()?.InInventory?.ID));
        static Entity Actor(string id, bool player)
        {
            var e = new Entity { ID = id, BlueprintName = player ? "Player" : "YardPersistenceStepper" };
            e.SetTag("Creature"); if (player) e.SetTag("Player");
            e.AddPart(new PhysicsPart { Takeable = false });
            e.AddPart(new InventoryPart { MaxWeight = 10000 });
            foreach (var pair in new[] { ("Hitpoints", 100), ("Strength", 16), ("Speed", 100) })
                e.Statistics[pair.Item1] = new Stat { Owner = e, Name = pair.Item1, BaseValue = pair.Item2, Max = 1000 };
            return e;
        }
        static void Place(Zone zone, Entity actor, (int x, int y) point)
        {
            if (zone.GetEntityCell(actor) != null) Assert.True(zone.RemoveEntity(actor));
            Assert.True(zone.AddEntity(actor, point.x, point.y));
        }
        static void Owned(Entity player, Zone zone)
        {
            foreach (var item in player.GetPart<InventoryPart>().Objects)
            {
                Assert.AreSame(player, item.GetPart<PhysicsPart>().InInventory);
                Assert.Null(item.GetPart<PhysicsPart>().Equipped);
                Assert.Null(zone.GetEntityCell(item)); Assert.Greater(Units(item), 0);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ActualChangedYardSurvivesCachedReturnAndReplacementSaveGraph(bool jammed)
        {
            using (var scope = new HaulingContentScope())
            {
                var oldHarvest = HarvestablePart.Factory; HarvestablePart.Factory = scope.Factory;
                try
                {
                    var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, true);
                    scope.Seed(unchecked(64 ^ FormationSelector.StableIndex(YardID, int.MaxValue)));
                    var zone = manager.GetZone(YardID); manager.SetActiveZone(zone);
                    var beam = zone.GetReadOnlyEntities().Single(e => e.GetProperty(RoleKey) == "haul-bypass");
                    var trap = zone.GetReadOnlyEntities().Single(e => e.GetProperty(RoleKey) == "trap");
                    var forage = zone.GetReadOnlyEntities().Single(e => e.GetProperty(RoleKey) == "cold-forage");
                    var b = zone.GetEntityPosition(beam); var t = zone.GetEntityPosition(trap);
                    var center = (x: (b.x + t.x) / 2, y: (b.y + t.y) / 2);
                    var cache = zone.GetCell(center.x, center.y).Objects.Single(e => e.HasPart<ContainerPart>());
                    var stock = cache.GetPart<ContainerPart>().Contents.ToArray(); Assert.IsNotEmpty(stock);
                    var amounts = stock.GroupBy(e => e.BlueprintName).ToDictionary(g => g.Key, g => g.Sum(Units));
                    var d = (x: Math.Sign(b.x - center.x), y: Math.Sign(b.y - center.y));
                    var grab = (x: b.x + d.x, y: b.y + d.y);
                    var pull = (x: b.x + 2 * d.x, y: b.y + 2 * d.y);
                    var aside = (x: pull.x - d.y, y: pull.y + d.x);
                    var player = Actor("dispatch-persistence-player", true);
                    var timber = scope.Factory.CreateEntity("SalvagedTimber"); timber.GetPart<StackerPart>().StackCount = 2;
                    Assert.True(player.GetPart<InventoryPart>().AddObject(timber));
                    Place(zone, player, zone.GetEntityPosition(forage));
                    Assert.True(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(forage, "Harvest"), player, zone).Success);
                    Assert.True(forage.GetPart<HarvestablePart>().Harvested); Assert.Null(zone.GetEntityCell(forage));
                    int frost = Packed(player, "FrostLichen"); Assert.That(frost, Is.InRange(1, 2));
                    Assert.False(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(forage, "Harvest"), player, zone).Success);
                    Assert.AreEqual(frost, Packed(player, "FrostLichen"));

                    Place(zone, player, grab);
                    Assert.AreEqual(DragVerdict.Ok, DragSystem.TryGrab(player, beam, zone)); Assert.AreEqual(76, player.GetStatValue("Speed"));
                    Assert.True(MovementSystem.TryMoveTo(player, zone, pull.x, pull.y));
                    Assert.AreEqual(grab, zone.GetEntityPosition(beam));
                    Assert.True(MovementSystem.TryMoveTo(player, zone, aside.x, aside.y));
                    Assert.AreEqual(pull, zone.GetEntityPosition(beam)); Assert.True(DragSystem.Release(player));
                    Assert.AreEqual(100, player.GetStatValue("Speed")); Assert.AreEqual(2, Packed(player, "SalvagedTimber"));
                    foreach (var step in new[] { (grab.x - d.y, grab.y + d.x), grab, b, (b.x - d.x, b.y - d.y), (b.x - 2 * d.x, b.y - 2 * d.y) })
                        Assert.True(MovementSystem.TryMoveTo(player, zone, step.Item1, step.Item2));
                    Assert.LessOrEqual(SpatialQuery.Distance(zone, player, cache), 1);
                    var beforeStock = amounts.Keys.ToDictionary(k => k, k => Packed(player, k));
                    foreach (var item in stock)
                        Assert.True(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(cache, item), player, zone).Success);
                    Assert.IsEmpty(cache.GetPart<ContainerPart>().Contents);
                    foreach (var row in amounts) Assert.AreEqual(beforeStock[row.Key] + row.Value, Packed(player, row.Key));
                    string depletedInventory = Inventory(player);
                    Assert.False(InventorySystem.ExecuteCommand(new TakeFromContainerCommand(cache, stock[0]), player, zone).Success);
                    Assert.AreEqual(depletedInventory, Inventory(player));

                    var outside = (x: t.x - d.x, y: t.y - d.y);
                    if (jammed)
                    {
                        Place(zone, player, outside); zone.GetCell(t.x, t.y).IsVisible = true; zone.GetCell(t.x, t.y).Explored = true;
                        Assert.True(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(trap, "JamTrap"), player, zone).Success);
                        Assert.True(TrapJammingPart.IsJammed(trap)); Assert.AreEqual(1, Packed(player, "SalvagedTimber"));
                        Assert.True(MovementSystem.TryMoveTo(player, zone, t.x, t.y)); Assert.AreEqual(100, player.GetStatValue("Hitpoints"));
                    }
                    else
                    {
                        Assert.False(TrapJammingPart.IsJammed(trap));
                        var stepper = Actor("dispatch-persistence-native-trigger", false); Place(zone, stepper, outside);
                        Assert.True(MovementSystem.TryMoveTo(stepper, zone, t.x, t.y));
                        Assert.AreEqual(100 - trap.GetPart<SpikeTrapTriggerPart>().Damage, stepper.GetStatValue("Hitpoints"));
                        Assert.Null(zone.GetEntityCell(trap)); Assert.AreEqual(2, Packed(player, "SalvagedTimber"));
                    }
                    Owned(player, zone); string inventory = Inventory(player);
                    var away = new Zone("Overworld.0.0.0"); Assert.True(zone.RemoveEntity(player));
                    Assert.True(away.AddEntity(player, 1, 1)); manager.SetActiveZone(away);
                    Assert.AreSame(zone, manager.GetZone(YardID));
                    Assert.AreEqual(pull, zone.GetEntityPosition(beam)); Assert.IsEmpty(cache.GetPart<ContainerPart>().Contents);
                    Assert.False(zone.GetReadOnlyEntities().Any(e => e.ID == forage.ID));
                    Assert.AreEqual(jammed, zone.GetReadOnlyEntities().Any(e => e.ID == trap.ID));
                    var restored = HotbarSaveFixture.RoundTrip(GameSessionState.Capture("dispatch-yard-" + jammed, "fixture", manager, null, player));
                    Assert.AreNotSame(player, restored.Player); Assert.AreEqual(inventory, Inventory(restored.Player));
                    Assert.AreEqual(away.ZoneID, restored.ZoneManager.ActiveZone.ZoneID);
                    var loaded = restored.ZoneManager.GetZone(YardID); Assert.AreNotSame(zone, loaded);
                    var savedBeam = loaded.GetReadOnlyEntities().Single(e => e.ID == beam.ID);
                    var savedCache = loaded.GetReadOnlyEntities().Single(e => e.ID == cache.ID);
                    Assert.AreNotSame(beam, savedBeam); Assert.AreNotSame(cache, savedCache);
                    Assert.AreEqual(pull, loaded.GetEntityPosition(savedBeam)); Assert.False(savedBeam.HasPart<DraggedPart>());
                    Assert.False(DragSystem.IsDragging(restored.Player)); Assert.AreEqual(100, restored.Player.GetStatValue("Speed"));
                    Assert.IsEmpty(savedCache.GetPart<ContainerPart>().Contents);
                    Assert.False(loaded.GetReadOnlyEntities().Any(e => e.ID == forage.ID));
                    var savedTrap = loaded.GetReadOnlyEntities().SingleOrDefault(e => e.ID == trap.ID);
                    if (jammed) { Assert.NotNull(savedTrap); Assert.AreNotSame(trap, savedTrap); Assert.True(TrapJammingPart.IsJammed(savedTrap)); }
                    else Assert.Null(savedTrap);
                    Assert.AreEqual(jammed ? 1 : 2, Packed(restored.Player, "SalvagedTimber"));
                    Owned(restored.Player, loaded);
                    foreach (var item in player.GetPart<InventoryPart>().Objects)
                        Assert.AreNotSame(item, restored.Player.GetPart<InventoryPart>().Objects.Single(e => e.ID == item.ID));
                    Assert.True(restored.ZoneManager.ActiveZone.RemoveEntity(restored.Player));
                    Assert.True(loaded.AddEntity(restored.Player, b.x, b.y)); restored.ZoneManager.SetActiveZone(loaded);
                    Assert.AreSame(loaded, restored.ZoneManager.GetZone(YardID)); Assert.AreEqual(inventory, Inventory(restored.Player));
                    Assert.IsEmpty(savedCache.GetPart<ContainerPart>().Contents); Assert.AreEqual(pull, loaded.GetEntityPosition(savedBeam));
                    Assert.False(loaded.GetReadOnlyEntities().Any(e => e.ID == forage.ID));
                    Assert.AreEqual(jammed, loaded.GetReadOnlyEntities().Any(e => e.ID == trap.ID));
                }
                finally { HarvestablePart.Factory = oldHarvest; }
            }
        }
    }
}
