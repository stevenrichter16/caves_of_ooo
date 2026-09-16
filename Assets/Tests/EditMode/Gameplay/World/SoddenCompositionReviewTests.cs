using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Independent cold-eye hypotheses, kept separate from implementation
    /// assertions. The parent runner records whether these expose RED or pin GREEN.</summary>
    public class SoddenCompositionReviewTests
    {
        [Test]
        public void Review_CopseResidentsCannotClosePreviouslyRepairedPocketsAcrossAddresses()
        {
            var factory = GrovelandsCompositionTests.Factory();
            var addresses = new List<string>();
            for (int y = 0; y < WorldMap.Height; y++) for (int x = 0; x < WorldMap.Width; x++)
            {
                string id = WorldMap.ToZoneID(x, y);
                if (SoddenCompositionPlan.IsWildernessZone(id)) addresses.Add(id);
            }
            Assert.Greater(addresses.Count, 1);
            // Resident placement follows terrain repair. A solid, stationary
            // toad next to a snag might close the final opening into a pocket.
            for (int sample = 0; sample < 256; sample++)
            {
                string id = addresses[sample % addresses.Count];
                int seed = unchecked(sample * 104729 - 1234567);
                var zone = new Zone(id);
                var builder = new SoddenCompositionBuilder(seed) { FormationOverride = Formation.DrownedCopse };
                Assert.IsTrue(builder.BuildZone(zone, factory, new System.Random(1)));
                var residents = zone.GetAllEntities().Where(e => e.BlueprintName == "MawToad").ToArray();
                Assert.That(residents.Length, Is.InRange(1, 2), id + " seed " + seed);
                var reached = ConnectivityBuilder.FloodFill(zone, 0, 0);
                for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                    if (!zone.GetCell(x, y).BlocksMovement())
                        Assert.IsTrue(reached[x, y], id + " seed " + seed + " resident-inclusive pocket " + x + "," + y);
                // Countercheck: removing residents must not strand ground either.
                foreach (var resident in residents) zone.RemoveEntity(resident);
                Assert.IsTrue(FormationReachability.FullyReached(zone, ConnectivityBuilder.FloodFill(zone, 0, 0)));
            }
        }

        [Test]
        public void Review_BothPrebuiltKitsRemainBorrowedInSoddenWithoutRevoxelizing()
        {
            bool old = Village3DSettings.Enabled;
            Village3DSettings.Enabled = true;
            try
            {
                var zone = new Zone(SoddenCompositionTests.Id);
                Assert.IsTrue(SoddenCompositionPlan.IsWildernessZone(zone.ZoneID));
                var bridge = VoxelWorldPresentation.ForZone(zone);
                Assert.NotNull(bridge);
                foreach (var entry in SoddenVoxelLibrary.Load().Entries)
                    Assert.AreSame(entry.Mesh, bridge.Resolve(entry.Mesh), entry.Id);
                for (int i = 0; i < 4; i++)
                {
                    var entry = SpreadVoxelLibrary.Load().Find(SpreadVoxelLibrary.ModelId("reeds", i));
                    Assert.AreSame(entry.Mesh, bridge.Resolve(entry.Mesh), entry.Id);
                }
                Assert.AreEqual(0, bridge.MissingMeshCount);
                Assert.AreEqual(0, bridge.AppliedMeshCount);
                Assert.AreEqual(0, zone.EntityCount);
                // The surface Counter now has its own kit; its depth1 remains uncomposed.
                Assert.IsNull(VoxelWorldPresentation.ForZone(new Zone("Overworld.18.18.1")));
            }
            finally { Village3DSettings.Enabled = old; }
        }

        [TestCase("MirePool")]
        [TestCase("Duckboard")]
        [TestCase("PeatBank")]
        [TestCase("Reeds")]
        public void Review_RealPresenterDropsRemovedSoddenOwnerAndKeepsSurvivingControl(string blueprint)
        {
            using (var f = new SpawnRing3DIntegrationFixture(SoddenCompositionTests.Id))
            {
                f.Set("FullReveal", true);
                var removed = f.Add(blueprint, 20, 10);
                var control = f.Add(blueprint, 60, 20);
                f.Refresh();
                Assert.IsTrue(f.Authored(removed)); Assert.IsTrue(f.Authored(control));
                Assert.IsTrue(f.Rendered(removed)); Assert.IsTrue(f.Rendered(control));
                Assert.IsTrue(f.Get<bool>("VoxelPresentationActive"));
                int revision = f.Revision(20, 10), distant = f.Revision(60, 20);
                Assert.IsTrue(f.Find(removed, out _, out _));
                f.Zone.RemoveEntity(removed);
                // Empty dirty input must not suppress native membership reconciliation.
                f.Refresh(new HashSet<int>());
                Assert.IsFalse(f.Authored(removed)); Assert.IsFalse(f.Rendered(removed));
                Assert.IsFalse(f.Find(removed, out _, out _));
                Assert.IsTrue(f.Authored(control)); Assert.IsTrue(f.Rendered(control));
                Assert.Greater(f.Revision(20, 10), revision);
                Assert.AreEqual(distant, f.Revision(60, 20));
                Assert.IsTrue(f.Get<bool>("IsReady"), f.Get<string>("Failure"));
            }
        }
    }
}
