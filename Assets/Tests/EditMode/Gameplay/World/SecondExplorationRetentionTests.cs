using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    // The negative fixture uses the shipped vault stamp at the cold-install
    // boundary; the positive fixture uses the actual seed-64 dispatch yard.
    public sealed class SecondExplorationRetentionTests
    {
        const string Yard = "Overworld.11.11.0";
        sealed class StampRoll : Random
        {
            public override int Next(int max) => 0;
            public override int Next(int min, int max) => min;
        }
        static int Count(Entity actor, string bp) => actor.GetPart<InventoryPart>().Objects
            .Where(e => e.BlueprintName == bp).Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);
        static void Install(OverworldZoneManager manager, Zone zone)
        {
            var method = typeof(SecondExplorationSites).GetMethod("Install", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(method); method.Invoke(null, new object[] { manager, zone });
        }
        [Test]
        public void UnretainedAmbientVaultKeepsItsTrapButDoesNotGainFiniteSalvage()
        {
            using (var scope = new HaulingContentScope())
            {
                scope.Seed(64); var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, true);
                var zone = new Zone("Overworld.0.19.0");
                Assert.Null(manager.Exploration.Find(zone.ZoneID));
                Assert.False(SecondExplorationSites.Retain(manager, zone.ZoneID));
                var stamp = StampCatalog.For(BiomeType.Beating).Single(s => s.Name == "SealedVault");
                Assert.True(new LandmarkBuilder(BiomeType.Beating, 2, new[] { stamp }).BuildZone(zone, scope.Factory, new StampRoll()));
                var trap = zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "SpikeTrap");
                Assert.True(TrapJammingPart.IsSupported(trap));
                var owners = zone.GetReadOnlyEntities().ToArray();
                Install(manager, zone);
                CollectionAssert.AreEquivalent(owners, zone.GetReadOnlyEntities());
                Assert.False(trap.HasPart<TrapSalvagePart>(), "An unloadable source must not gain a finite salvage reward that regeneration can replenish.");
                Assert.True(TrapJammingPart.IsSupported(trap), "Ordinary trap jamming remains available.");
                manager.SetActiveZone(zone); manager.UnloadZone(zone.ZoneID);
                Assert.False(manager.CachedZones.ContainsKey(zone.ZoneID), "This counter actually exercises an unretained graph.");
            }
        }
        [Test]
        public void GeneratedRetainedYardStillJamsAndSalvagesOnceAcrossUnloadAndReplacementSave()
        {
            using (var scope = new HaulingContentScope())
            {
                var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, true);
                scope.Seed(unchecked(64 ^ FormationSelector.StableIndex(Yard, int.MaxValue)));
                var zone = manager.GetZone(Yard); Assert.NotNull(zone); manager.SetActiveZone(zone);
                var trap = zone.GetReadOnlyEntities().Single(e => e.GetProperty("SpreadWorksite.Role") == "trap");
                var beam = zone.GetReadOnlyEntities().Single(e => e.GetProperty("SpreadWorksite.Role") == "haul-bypass");
                Assert.True(trap.HasPart<TrapSalvagePart>(), "The ordinary generated dispatch-yard source retains its new choice.");
                var t = zone.GetEntityPosition(trap); var b = zone.GetEntityPosition(beam);
                var player = new Entity { ID = "retention-salvage-player", BlueprintName = "Player" };
                player.SetTag("Player"); player.SetTag("Creature"); player.AddPart(new PhysicsPart { Takeable = false });
                player.AddPart(new InventoryPart { MaxWeight = 10000 });
                foreach (var pair in new[] { ("Hitpoints", 100), ("Strength", 16), ("Speed", 100) })
                    player.Statistics[pair.Item1] = new Stat { Owner = player, Name = pair.Item1, BaseValue = pair.Item2, Max = 1000 };
                Assert.True(zone.AddEntity(player, t.x + Math.Sign(t.x - b.x), t.y + Math.Sign(t.y - b.y)));
                Assert.True(player.GetPart<InventoryPart>().AddObject(scope.Factory.CreateEntity("SalvagedTimber")));
                zone.GetEntityCell(trap).IsVisible = true; zone.GetEntityCell(trap).Explored = true;
                Assert.True(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(trap, "JamTrap"), player, zone).Success);
                Assert.True(TrapJammingPart.IsJammed(trap)); Assert.AreEqual(0, Count(player, "SalvagedTimber"));
                Assert.True(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(trap, "SalvageJammedTrap"), player, zone).Success);
                Assert.Null(zone.GetEntityCell(trap)); Assert.AreEqual(1, Count(player, "IronSpikeComponent"));
                Assert.False(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(trap, "SalvageJammedTrap"), player, zone).Success);
                manager.UnloadZone(Yard); Assert.AreSame(zone, manager.GetZone(Yard));
                var loaded = HotbarSaveFixture.RoundTrip(GameSessionState.Capture("retained-salvage", "generated finite aftermath", manager, null, player));
                var replacement = loaded.ZoneManager.GetZone(Yard); Assert.AreNotSame(zone, replacement);
                Assert.False(replacement.GetReadOnlyEntities().Any(e => e.ID == trap.ID || e.GetProperty("SpreadWorksite.Role") == "trap"));
                Assert.AreEqual(1, Count(loaded.Player, "IronSpikeComponent")); Assert.AreEqual(0, Count(loaded.Player, "SalvagedTimber"));
                loaded.ZoneManager.UnloadZone(Yard); Assert.AreSame(replacement, loaded.ZoneManager.GetZone(Yard));
            }
        }
    }
}
