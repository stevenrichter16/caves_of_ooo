using System;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class SpreadResidentsManifestTests
    {
        const BindingFlags All = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        [TestCase(64)][TestCase(1729)]
        public void FreshCurrentRetainsTwoResidentFamiliesAndAllThreeNearbyWorksites(int seed)
        {
            using (var scope = new HaulingContentScope())
            {
                var manager = OverworldZoneManager.CreateDetached(scope.Factory, seed, true); Assert.AreEqual(11, manager.Exploration.Version);
                Assert.AreEqual(16, Convert.ToInt32(Enum.Parse(typeof(SpreadExplorationFamily), "SeedKeepersPlot")));
                Assert.AreEqual(17, Convert.ToInt32(Enum.Parse(typeof(SpreadExplorationFamily), "WaysideKitchen")));
                Assert.AreEqual("FieldAlembic", manager.Exploration.Find("Overworld.11.9.0").Family.ToString());
                Assert.AreEqual("TemperingShelter", manager.Exploration.Find("Overworld.12.10.0").Family.ToString());
                Assert.AreEqual("TrappersStore", manager.Exploration.Find("Overworld.11.11.0").Family.ToString());
                foreach (string family in new[] { "SeedKeepersPlot", "WaysideKitchen" }) Assert.True(manager.Exploration.Entries.Any(e => e.Family.ToString() == family));
                Assert.Zero(manager.CachedZoneCount);
            }
        }
        [Test] public void LiteralVersionNineQuietSiteRoundTripsWithoutAdoptingAResident()
        {
            using (var scope = new HaulingContentScope())
            {
                var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, false); const string wire = "9|64|1\nOverworld.11.8.0|3|0|1|0";
                var world = new Entity(); world.Properties[SpreadExplorationPlan.PropertyKey] = wire;
                var restored = (SpreadExplorationPlan)typeof(SpreadExplorationPlan).GetMethod("Restore", All).Invoke(null, new object[] { manager, world });
                typeof(OverworldZoneManager).GetProperty("Exploration", All).SetValue(manager, restored);
                Assert.AreEqual(wire, SpreadExplorationPlan.BindForSave(manager, null).GetProperty(SpreadExplorationPlan.PropertyKey)); Assert.AreEqual(9, restored.Version);
                var zone = manager.GetZone("Overworld.11.8.0"); Assert.NotNull(zone);
                Assert.False(zone.GetReadOnlyEntities().Any(e => e.Properties.ContainsKey("SpreadResident.Role"))); Assert.AreEqual(1, restored.DispositionFor(zone.ZoneID));
            }
        }
        [TestCase(16)][TestCase(17)] public void LiteralVersionNineRejectsResidentIdsInsteadOfSilentlyUpgrading(int family)
        {
            using (var scope = new HaulingContentScope())
            {
                var manager = OverworldZoneManager.CreateDetached(scope.Factory, 64, false); var world = new Entity();
                world.Properties[SpreadExplorationPlan.PropertyKey] = "9|64|1\nOverworld.11.8.0|3|" + family + "|1|0";
                var error = Assert.Throws<TargetInvocationException>(() => typeof(SpreadExplorationPlan).GetMethod("Restore", All).Invoke(null, new object[] { manager, world }));
                Assert.IsInstanceOf<InvalidDataException>(error.InnerException); Assert.Zero(manager.CachedZoneCount);
            }
        }
        [TestCase(64)][TestCase(1729)] public void NewResidentsNeverOccupyProtectedAddressesOrTouchIdenticalFamilies(int seed)
        {
            using (var scope = new HaulingContentScope())
            {
                var manager = OverworldZoneManager.CreateDetached(scope.Factory, seed, true); var rows = manager.Exploration.Entries.ToArray();
                var protectedIds = new[] { ReferenceGladePlan.ZoneID, manager.Wayhouse?.ZoneID, manager.RareEncounters?.PairZoneID, manager.RareEncounters?.ViperZoneID }
                    .Concat(RegionalSituations.Definitions.SelectMany(d => new[] { d.SourceZoneId, d.RecipientZoneId }));
                foreach (string id in protectedIds.Where(id => id != null)) Assert.False(rows.Any(e => e.ZoneID == id && e.PlacementEligible), id);
                var residents = rows.Where(e => e.Family == SpreadExplorationFamily.SeedKeepersPlot || e.Family == SpreadExplorationFamily.WaysideKitchen).ToArray(); Assert.IsNotEmpty(residents);
                foreach (var a in residents) foreach (var b in residents.Where(e => e.ZoneID != a.ZoneID && e.Family == a.Family))
                {
                    var x = WorldMap.FromZoneID(a.ZoneID); var y = WorldMap.FromZoneID(b.ZoneID);
                    Assert.AreNotEqual(1, Math.Abs(x.x - y.x) + Math.Abs(x.y - y.y), a.ZoneID + " " + b.ZoneID);
                }
            }
        }
        [TestCase(64)][TestCase(1729)] public void OnlyTwoRequiredResidentAddressesMayReplaceAVersionNineFamily(int seed)
        {
            using (var scope = new HaulingContentScope())
            {
                var current = OverworldZoneManager.CreateDetached(scope.Factory, seed, true).Exploration;
                var baseline = (seed == 64 ? NativeV9Seed64 : NativeV9Seed1729).Split('\n').Skip(1).Where(line => line.Length > 0).Select(line => line.Split('|')).ToArray();
                Assert.AreEqual(baseline.Length, current.Entries.Count);
                foreach (var row in baseline)
                {
                    var entry = current.Find(row[0]); Assert.NotNull(entry, row[0]);
                    Assert.AreEqual(row[1] == "3", entry.PlacementEligible, row[0]); Assert.AreEqual(int.Parse(row[3]), (int)entry.Topology, row[0]);
                    int previous = int.Parse(row[2]);
                    if (row[0] == LocalGatheringClaimPart.ReserveZoneID || row[0] == KitchenBatchPart.KitchenZoneID)
                    {
                        Assert.AreEqual(row[0] == LocalGatheringClaimPart.ReserveZoneID
                            ? SpreadExplorationFamily.SeedKeepersPlot : SpreadExplorationFamily.WaysideKitchen, entry.Family);
                        continue;
                    }
                    if (previous != 0) Assert.AreEqual(previous, (int)entry.Family, "Previously shipped situation displaced: " + row[0]);
                    if ((int)entry.Family >= 16) Assert.Zero(previous, "Other residents and the optional satellite must fill an ordinary quiet row: " + row[0]);
                }
                Assert.AreEqual("SeedKeepersPlot", current.Find("Overworld.11.8.0").Family.ToString());
                Assert.AreEqual("WaysideKitchen", current.Find("Overworld.12.11.0").Family.ToString());
                Assert.AreEqual("CoolingWorkPatch", current.Find("Overworld.13.10.0").Family.ToString());
            }
        }
        // Frozen native pre-change v9 manifest captures; no local scratch-file dependency.
        const string NativeV9Seed64 = @"9|64|129
Overworld.10.0.0|3|7|2|0
Overworld.10.1.0|3|15|3|0
Overworld.10.11.0|3|0|1|0
Overworld.10.12.0|3|0|2|0
Overworld.10.13.0|3|9|3|0
Overworld.10.15.0|3|0|3|0
Overworld.10.2.0|1|0|0|0
Overworld.10.3.0|3|0|2|0
Overworld.10.4.0|3|0|3|0
Overworld.10.5.0|3|3|2|0
Overworld.10.6.0|3|0|1|0
Overworld.10.7.0|3|9|2|0
Overworld.10.8.0|3|11|1|0
Overworld.10.9.0|3|14|1|0
Overworld.11.0.0|3|0|3|0
Overworld.11.1.0|3|0|2|0
Overworld.11.10.0|1|0|0|0
Overworld.11.11.0|3|15|3|0
Overworld.11.12.0|3|1|1|0
Overworld.11.13.0|3|4|3|0
Overworld.11.14.0|3|0|3|0
Overworld.11.15.0|3|0|1|0
Overworld.11.2.0|3|2|1|0
Overworld.11.3.0|3|0|1|0
Overworld.11.4.0|3|14|2|0
Overworld.11.5.0|3|0|1|0
Overworld.11.6.0|3|0|1|0
Overworld.11.7.0|1|0|0|0
Overworld.11.8.0|3|0|2|0
Overworld.11.9.0|3|13|3|0
Overworld.12.0.0|3|1|2|0
Overworld.12.1.0|3|0|2|0
Overworld.12.10.0|3|14|3|0
Overworld.12.11.0|3|0|2|0
Overworld.12.13.0|3|0|3|0
Overworld.12.14.0|3|3|3|0
Overworld.12.4.0|3|0|3|0
Overworld.12.5.0|3|2|2|0
Overworld.12.6.0|3|12|1|0
Overworld.12.7.0|3|0|2|0
Overworld.12.8.0|3|1|2|0
Overworld.12.9.0|3|4|1|0
Overworld.13.10.0|3|10|3|0
Overworld.13.11.0|3|9|2|0
Overworld.13.12.0|3|6|3|0
Overworld.13.13.0|3|3|1|0
Overworld.13.5.0|3|1|1|0
Overworld.13.6.0|3|5|1|0
Overworld.13.8.0|3|9|2|0
Overworld.13.9.0|3|0|3|0
Overworld.14.10.0|3|0|3|0
Overworld.14.11.0|3|10|2|0
Overworld.14.12.0|3|0|2|0
Overworld.14.6.0|3|1|2|0
Overworld.14.7.0|3|4|3|0
Overworld.14.8.0|3|12|2|0
Overworld.15.10.0|3|14|3|0
Overworld.15.11.0|3|0|1|0
Overworld.15.12.0|3|3|2|0
Overworld.15.9.0|3|11|2|0
Overworld.16.10.0|3|3|2|0
Overworld.16.12.0|3|9|1|0
Overworld.17.11.0|3|0|2|0
Overworld.17.12.0|3|0|1|0
Overworld.18.11.0|3|1|3|0
Overworld.18.12.0|3|5|1|0
Overworld.19.11.0|3|5|1|0
Overworld.4.10.0|3|13|3|0
Overworld.4.13.0|3|7|3|0
Overworld.4.14.0|3|2|1|0
Overworld.4.9.0|3|0|1|0
Overworld.5.10.0|3|7|3|0
Overworld.5.11.0|3|10|2|0
Overworld.5.12.0|3|3|2|0
Overworld.5.13.0|3|12|3|0
Overworld.5.14.0|3|4|3|0
Overworld.5.15.0|3|1|3|0
Overworld.5.8.0|3|12|3|0
Overworld.6.10.0|3|0|3|0
Overworld.6.11.0|3|0|3|0
Overworld.6.12.0|3|7|2|0
Overworld.6.13.0|3|5|1|0
Overworld.6.14.0|3|10|2|0
Overworld.6.15.0|3|9|2|0
Overworld.6.7.0|3|12|2|0
Overworld.6.8.0|3|0|2|0
Overworld.6.9.0|3|1|2|0
Overworld.7.1.0|3|0|1|0
Overworld.7.10.0|3|0|1|0
Overworld.7.11.0|3|6|3|0
Overworld.7.12.0|3|2|2|0
Overworld.7.13.0|3|10|1|0
Overworld.7.14.0|3|5|1|0
Overworld.7.15.0|3|11|2|0
Overworld.7.16.0|3|4|2|0
Overworld.7.2.0|3|12|2|0
Overworld.7.6.0|3|2|2|0
Overworld.7.7.0|1|0|0|0
Overworld.7.9.0|3|0|1|0
Overworld.8.0.0|3|10|3|0
Overworld.8.1.0|3|6|1|0
Overworld.8.10.0|3|9|2|0
Overworld.8.11.0|3|0|2|0
Overworld.8.13.0|3|0|2|0
Overworld.8.14.0|3|0|1|0
Overworld.8.15.0|3|10|3|0
Overworld.8.3.0|3|0|3|0
Overworld.8.4.0|3|0|2|0
Overworld.8.5.0|3|11|3|0
Overworld.8.6.0|3|4|2|0
Overworld.8.7.0|3|1|1|0
Overworld.8.8.0|3|0|2|0
Overworld.8.9.0|3|12|3|0
Overworld.9.0.0|1|0|0|0
Overworld.9.1.0|3|5|3|0
Overworld.9.10.0|3|4|3|0
Overworld.9.11.0|3|1|1|0
Overworld.9.12.0|3|15|2|0
Overworld.9.13.0|3|0|2|0
Overworld.9.14.0|3|9|1|0
Overworld.9.15.0|3|3|1|0
Overworld.9.2.0|3|1|1|0
Overworld.9.3.0|3|4|3|0
Overworld.9.4.0|3|15|1|0
Overworld.9.5.0|3|10|1|0
Overworld.9.6.0|3|3|1|0
Overworld.9.7.0|3|11|1|0
Overworld.9.8.0|3|7|1|0
Overworld.9.9.0|3|0|2|0";
        const string NativeV9Seed1729 = @"9|1729|128
Overworld.10.0.0|3|7|1|0
Overworld.10.11.0|3|0|3|0
Overworld.10.12.0|3|0|1|0
Overworld.10.13.0|3|9|1|0
Overworld.10.15.0|3|0|1|0
Overworld.10.2.0|3|0|1|0
Overworld.10.3.0|3|4|3|0
Overworld.10.4.0|3|12|2|0
Overworld.10.5.0|3|10|3|0
Overworld.10.6.0|3|0|3|0
Overworld.10.7.0|3|2|1|0
Overworld.10.8.0|3|11|1|0
Overworld.10.9.0|3|5|2|0
Overworld.11.0.0|3|0|2|0
Overworld.11.1.0|3|13|1|0
Overworld.11.10.0|1|0|0|0
Overworld.11.11.0|3|15|3|0
Overworld.11.12.0|3|1|3|0
Overworld.11.13.0|3|4|1|0
Overworld.11.14.0|3|6|2|0
Overworld.11.15.0|3|10|2|0
Overworld.11.2.0|3|11|1|0
Overworld.11.3.0|3|0|2|0
Overworld.11.4.0|3|5|2|0
Overworld.11.5.0|3|2|1|0
Overworld.11.6.0|3|4|1|0
Overworld.11.7.0|3|1|1|0
Overworld.11.8.0|3|0|1|0
Overworld.11.9.0|3|13|3|0
Overworld.12.0.0|3|1|2|0
Overworld.12.1.0|3|4|1|0
Overworld.12.10.0|3|14|1|0
Overworld.12.11.0|3|0|2|0
Overworld.12.13.0|3|0|3|0
Overworld.12.14.0|3|0|1|0
Overworld.12.4.0|3|10|3|0
Overworld.12.5.0|3|7|3|0
Overworld.12.6.0|3|12|3|0
Overworld.12.7.0|3|3|1|0
Overworld.12.8.0|3|1|1|0
Overworld.12.9.0|3|4|3|0
Overworld.13.10.0|3|10|1|0
Overworld.13.11.0|3|9|3|0
Overworld.13.12.0|3|6|1|0
Overworld.13.13.0|3|0|2|0
Overworld.13.5.0|3|1|3|0
Overworld.13.6.0|3|5|1|0
Overworld.13.8.0|3|0|3|0
Overworld.13.9.0|3|3|1|0
Overworld.14.10.0|3|7|3|0
Overworld.14.11.0|3|0|2|0
Overworld.14.12.0|3|0|3|0
Overworld.14.13.0|1|0|0|0
Overworld.14.6.0|3|1|1|0
Overworld.14.7.0|3|4|3|0
Overworld.14.8.0|3|12|3|0
Overworld.15.10.0|3|0|3|0
Overworld.15.11.0|3|3|1|0
Overworld.15.12.0|3|0|2|0
Overworld.15.9.0|3|9|3|0
Overworld.16.10.0|3|10|3|0
Overworld.16.12.0|3|2|2|0
Overworld.17.11.0|3|4|2|0
Overworld.17.12.0|3|0|3|0
Overworld.18.11.0|3|1|2|0
Overworld.18.12.0|3|5|3|0
Overworld.19.11.0|3|5|1|0
Overworld.4.10.0|3|6|1|0
Overworld.4.13.0|3|2|2|0
Overworld.4.9.0|3|11|3|0
Overworld.5.10.0|3|11|3|0
Overworld.5.11.0|3|3|1|0
Overworld.5.12.0|3|0|1|0
Overworld.5.13.0|3|12|3|0
Overworld.5.14.0|3|4|1|0
Overworld.5.15.0|3|0|2|0
Overworld.5.8.0|3|6|2|0
Overworld.6.10.0|3|1|1|0
Overworld.6.11.0|3|0|3|0
Overworld.6.12.0|1|0|0|0
Overworld.6.13.0|3|5|3|0
Overworld.6.14.0|3|10|2|0
Overworld.6.15.0|3|9|2|0
Overworld.6.7.0|3|0|1|0
Overworld.6.8.0|3|4|3|0
Overworld.6.9.0|3|0|1|0
Overworld.7.1.0|3|0|3|0
Overworld.7.10.0|3|0|1|0
Overworld.7.11.0|3|0|3|0
Overworld.7.12.0|3|7|2|0
Overworld.7.13.0|3|3|1|0
Overworld.7.14.0|3|5|3|0
Overworld.7.15.0|3|0|1|0
Overworld.7.16.0|3|4|3|0
Overworld.7.2.0|3|0|3|0
Overworld.7.6.0|3|9|2|0
Overworld.7.7.0|1|0|0|0
Overworld.7.9.0|3|7|1|0
Overworld.8.0.0|3|0|1|0
Overworld.8.1.0|3|6|2|0
Overworld.8.10.0|3|2|2|0
Overworld.8.12.0|3|1|3|0
Overworld.8.13.0|3|0|2|0
Overworld.8.14.0|3|12|2|0
Overworld.8.15.0|3|3|2|0
Overworld.8.2.0|1|0|0|0
Overworld.8.3.0|3|3|3|0
Overworld.8.4.0|3|5|1|0
Overworld.8.5.0|3|9|2|0
Overworld.8.6.0|3|4|3|0
Overworld.8.7.0|3|0|2|0
Overworld.8.8.0|3|0|1|0
Overworld.8.9.0|3|0|1|0
Overworld.9.0.0|3|2|1|0
Overworld.9.1.0|3|5|2|0
Overworld.9.10.0|3|0|1|0
Overworld.9.11.0|3|1|1|0
Overworld.9.12.0|3|5|2|0
Overworld.9.13.0|3|0|3|0
Overworld.9.14.0|3|7|1|0
Overworld.9.15.0|3|0|2|0
Overworld.9.2.0|3|1|3|0
Overworld.9.3.0|3|0|2|0
Overworld.9.4.0|3|0|1|0
Overworld.9.6.0|3|10|3|0
Overworld.9.7.0|3|0|2|0
Overworld.9.8.0|3|0|1|0
Overworld.9.9.0|3|0|1|0";
    }
}
