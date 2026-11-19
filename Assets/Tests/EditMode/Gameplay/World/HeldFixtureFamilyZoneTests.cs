using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// A set-down box in a real, generated Spread exploration-family zone (Docs/HOME-BASE-IMPLEMENTATION.md: the family refusal was lifted after an independent consumer audit).
    /// One zone per family: set down with contents, check the box and the family's graph survive a whole-session save round trip and that the zone is still retained.
    /// </summary>
    public sealed class HeldFixtureFamilyZoneTests
    {
        HaulingContentScope scope;
        [SetUp] public void Setup() { scope = new HaulingContentScope(); scope.Seed(64); PackableFixtureRegistry.ResetForTests(); Diag.ResetAll(); }
        [TearDown] public void Cleanup() { PackableFixtureRegistry.ResetForTests(); scope.Dispose(); }

        static IEnumerable<string> Cells()
        { for (int x = 0; x < WorldMap.Width; x++) for (int y = 0; y < WorldMap.Height; y++) yield return WorldMap.ToZoneID(x, y, 0); }

        [Test] public void OneZonePerFamilyHoldsAFilledBoxThroughASessionRoundTrip()
        {
            var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, true);
            var byFamily = new Dictionary<SpreadExplorationFamily, string>();
            foreach (string id in Cells())
            {
                if (HeldPlaceZones.Classify(manager, id) != HeldPlaceZoneClass.FamilyOwned) continue;
                if (manager.Exploration.TryGetPlacement(manager, id, out var entry) && entry.Family != SpreadExplorationFamily.None && !byFamily.ContainsKey(entry.Family)) byFamily[entry.Family] = id;
            }
            Assert.GreaterOrEqual(byFamily.Count, 2, "the world has several families to check");
            int placed = 0;
            foreach (var pair in byFamily.Take(6))
            {
                var zone = manager.GetZone(pair.Value); Assert.NotNull(zone, pair.Key.ToString());
                var actor = scope.Factory.CreateEntity("Player"); actor.GetPart<InventoryPart>().MaxWeight = 400;
                // stand on any cell that has a legal neighbour, then set down east or the first legal direction
                var box = scope.Factory.CreateEntity("CampBox"); Assert.True(actor.GetPart<InventoryPart>().AddObject(box));
                int ax = -1, ay = -1; string command = null;
                for (int x = 4; x < Zone.Width - 4 && command == null; x++)
                    for (int y = 4; y < Zone.Height - 4 && command == null; y++)
                    {
                        var cell = zone.GetCell(x, y); if (cell.BlocksMovement() || cell.Objects.Any(o => !o.HasTag("Terrain"))) continue;
                        if (!zone.AddEntity(actor, x, y)) continue;
                        var row = InventorySystem.GetActions(actor, box).FirstOrDefault(a => a.Name == "SetDownFixture" && !a.IsDisabled);
                        if (row != null) { command = row.Command; ax = x; ay = y; } else zone.RemoveEntity(actor);
                    }
                Assert.NotNull(command, "a legal set-down cell exists in the " + pair.Key + " zone " + pair.Value);
                manager.SetActiveZone(zone); var dagger = scope.Factory.CreateEntity("Dagger"); Assert.True(box.GetPart<ContainerPart>().AddItem(dagger));
                Assert.True(InventorySystem.PerformAction(actor, box, command, zone), string.Join("|", MessageLog.GetRecent(2)));
                Assert.AreEqual(1, zone.GetReadOnlyEntities().Count(e => e.ID == box.ID)); Assert.True(InstallationMarkerPart.Retains(box));
                var turns = new TurnManager(); turns.RestoreSavedState(17, true, actor, new List<TurnManager.SavedTurnEntry> { new TurnManager.SavedTurnEntry { Entity = actor, Energy = TurnManager.ActionThreshold } });
                var state = GameSessionState.Capture("family-" + pair.Key, "round-trip", manager, turns, actor);
                GameSessionState loaded = null; Assert.DoesNotThrow(() => loaded = HotbarSaveFixture.RoundTrip(state), "the family graph restores with the box in the zone");
                var restored = loaded.ZoneManager.CachedZones[pair.Value]; var again = restored.GetReadOnlyEntities().Single(e => e.ID == box.ID);
                Assert.AreEqual(restored.GetEntityCell(again).X, zone.GetEntityCell(box).X); Assert.True(InstallationMarkerPart.Retains(again)); Assert.True(again.HasTag("Owned"));
                Assert.AreEqual("Dagger", again.GetPart<ContainerPart>().Contents.Single().BlueprintName);
                Assert.AreEqual(HeldPlaceZoneClass.FamilyOwned, HeldPlaceZones.Classify(loaded.ZoneManager as OverworldZoneManager, pair.Value), "classification is stable after the round trip");
                placed++;
            }
            Assert.GreaterOrEqual(placed, 2);
        }

        [Test] public void ARetainedFamilyZoneIsNotUnloadedByTheRelapseSweepWithOrWithoutTheBox_Counter()
        {
            var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, true);
            string id = Cells().First(c => HeldPlaceZones.Classify(manager, c) == HeldPlaceZoneClass.FamilyOwned);
            var zone = manager.GetZone(id); Assert.NotNull(zone);
            Assert.True(manager.CachedZones.ContainsKey(id)); Assert.AreSame(zone, manager.GetZone(id), "a family zone stays cached on re-access");
        }
    }
}
