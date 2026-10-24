using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Opening composition is real cold geography, never a saved-world repaint.</summary>
    public sealed class SpreadWorkingLandscapeTests
    {
        const string East = "Overworld.12.10.0", Kitchen = "Overworld.12.11.0";
        static readonly SpreadExplorationTopology[] Topologies = { SpreadExplorationTopology.OffsetLanes,
            SpreadExplorationTopology.BrokenEnclosures, SpreadExplorationTopology.BankCrossing };
        static SpreadCompositionPlan Plan(string id, int seed, SpreadExplorationTopology topology)
            => SpreadCompositionPlan.Create(id, seed, Formation.None, topology);
        static int Count(SpreadCompositionPlan p, string bp)
        {
            int count = 0;
            for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++) if (p.ObjectAt(x, y) == bp) count++;
            return count;
        }
        static IEnumerable<(int x, int y)> Neighbors((int x, int y) p)
        { yield return (p.x - 1, p.y); yield return (p.x + 1, p.y); yield return (p.x, p.y - 1); yield return (p.x, p.y + 1); }
        static bool In(int x, int y) => x >= 0 && y >= 0 && x < Zone.Width && y < Zone.Height;
        static HashSet<(int x, int y)> Reach((int x, int y) start, Func<int, int, bool> walk)
        {
            var seen = new HashSet<(int x, int y)>(); var queue = new Queue<(int x, int y)>(); queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var p = queue.Dequeue(); if (!In(p.x, p.y) || !walk(p.x, p.y) || !seen.Add(p)) continue;
                foreach (var n in Neighbors(p)) queue.Enqueue(n);
            }
            return seen;
        }
        static int BroadIntentionalBreaks(SpreadCompositionPlan p, SpreadCompositionPlan.Parcel parcel)
        {
            // Existing public route/lawn gaps are not evidence of a newly broken hedge side.
            int sides = 0;
            foreach (int side in new[] { 0, 1, 2, 3 })
            {
                int run = 0, longest = 0, length = side < 2 ? parcel.Width : parcel.Height;
                for (int i = 0; i < length; i++)
                {
                    int x = side < 2 ? parcel.X + i : side == 2 ? parcel.X : parcel.X + parcel.Width - 1;
                    int y = side < 2 ? side == 0 ? parcel.Y : parcel.Y + parcel.Height - 1 : parcel.Y + i;
                    bool broken = !p.IsApproach(x, y) && !p.IsLawn(x, y) && !p.IsWater(x, y) && p.ObjectAt(x, y) == null;
                    run = broken ? run + 1 : 0; longest = Math.Max(longest, run);
                }
                if (longest >= 4) sides++;
            }
            return sides;
        }
        [TestCase(64)] [TestCase(65)]
        public void KitchenHasBroadMissingSidesAsWellAsSurvivingUnequalHedgeRuns(int seed)
        {
            foreach (var topology in Topologies)
            {
                var p = Plan(Kitchen, seed, topology); Assert.AreEqual(Formation.Hedgerow, p.Formation);
                int brokenParcels = Enumerable.Range(0, p.ParcelCount).Count(i => BroadIntentionalBreaks(p, p.GetParcel(i)) > 0);
                Assert.GreaterOrEqual(brokenParcels, 2, topology + ": centered gates alone leave repeated intact rectangles.");
                Assert.That(Count(p, "Hedge"), Is.InRange(40, 180), "Deleting the hedges is not composition.");
                Assert.GreaterOrEqual(Enumerable.Range(0, p.ParcelCount).Select(i => p.GetParcel(i).Width).Distinct().Count(), 3);
                Assert.AreEqual(p.Signature(), Plan(Kitchen, seed, topology).Signature());
            }
        }
        [TestCase(64)] [TestCase(65)]
        public void EastFlowersDoNotRepeatTheSameFourteenRowShiftedBand(int seed)
        {
            foreach (var topology in Topologies)
            {
                var p = Plan(East, seed, topology); int eligible = 0, repeated = 0;
                for (int y = 0; y < Zone.Height - 14; y++) for (int x = 0; x < Zone.Width; x++)
                    if (p.ObjectAt(x, y) == "CharmFlowers") { eligible++; if (p.ObjectAt(x, y + 14) == "CharmFlowers") repeated++; }
                Assert.GreaterOrEqual(eligible, 40, "A meaningful upper drift must survive; erasure cannot lower repetition.");
                Assert.Less((double)repeated / eligible, .35, topology + ": long matching upper/lower bands should become unequal drifts.");
                Assert.That(Count(p, "CharmFlowers"), Is.InRange(40, 360)); Assert.That(Count(p, "Tree"), Is.InRange(4, 85));
                Assert.AreEqual(0, Count(p, "FlowerField"), "Ordinary flowers must not acquire conjured expiry/charm.");
                Assert.AreEqual(p.Signature(), Plan(East, seed, topology).Signature());
            }
        }
        [TestCase(East, 64)] [TestCase(East, 65)] [TestCase(Kitchen, 64)] [TestCase(Kitchen, 65)]
        public void GeometryKeepsRealGroundFourPortsAndAllOpenCellsConnected(string id, int seed)
        {
            var legacy = Plan(id, seed, SpreadExplorationTopology.Legacy);
            foreach (var topology in Topologies)
            {
                var p = Plan(id, seed, topology);
                Assert.AreEqual(legacy.WestY, p.WestY); Assert.AreEqual(legacy.EastY, p.EastY);
                Assert.AreEqual(legacy.NorthX, p.NorthX); Assert.AreEqual(legacy.SouthX, p.SouthX);
                Assert.AreEqual(p.EastY, Plan(WorldMap.ToZoneID(p.WorldX + 1, p.WorldY), seed, topology).WestY);
                Assert.AreEqual(p.SouthX, Plan(WorldMap.ToZoneID(p.WorldX, p.WorldY + 1), seed, topology).NorthX);
                var reached = Reach((0, p.WestY), (x, y) => p.ObjectAt(x, y) != "Hedge" && p.ObjectAt(x, y) != "Tree" && !p.IsWater(x, y));
                foreach (var port in new[] { (79, p.EastY), (p.NorthX, 0), (p.SouthX, 24) }) Assert.True(reached.Contains(port));
                for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++)
                {
                    Assert.AreEqual("Grass", p.GroundAt(x, y), "A clear court is not an invented road or planted soil.");
                    string bp = p.ObjectAt(x, y);
                    if (bp != "Hedge" && bp != "Tree") Assert.True(reached.Contains((x, y)), id + " " + seed + " " + x + "," + y);
                    if (p.IsApproach(x, y) || x < 2 || x > 77 || y < 2 || y > 22) Assert.IsNull(bp);
                }
                Assert.AreEqual(0, Count(p, "CropRow")); Assert.AreNotEqual(p.Signature(), Plan(id, seed + 7, topology).Signature());
            }
        }
        static void AssertOldHedgeEdges(SpreadCompositionPlan p)
        {
            int verified = 0;
            for (int i = 0; i < p.ParcelCount; i++)
            {
                var r = p.GetParcel(i);
                for (int y = r.Y; y < r.Y + r.Height; y++) for (int x = r.X; x < r.X + r.Width; x++)
                {
                    if (!r.Boundary(x, y) || !In(x, y) || x < 2 || x > 77 || y < 2 || y > 22 || p.IsApproach(x, y) || p.IsLawn(x, y)) continue;
                    bool gate = x >= r.X + r.Width / 2 - 1 && x <= r.X + r.Width / 2 + 1;
                    Assert.AreEqual(gate ? null : "Hedge", p.ObjectAt(x, y)); verified++;
                }
            }
            Assert.Greater(verified, 20);
        }
        [TestCase(64)] [TestCase(65)]
        public void ExactLegacyAndUnrelatedHedgeAndFlowerGrammarStayOutsideTheChange(int seed)
        {
            // Captured in native Unity before this implementation; exact outside/Legacy controls.
            var unchanged = new[] {
                ("Overworld.12.10.0", 64, SpreadExplorationTopology.Legacy, "e48fbe0afb0b1c2ba2923bbc149b9e672d489fa928665c176c95d3bab5618755"),
                ("Overworld.12.11.0", 64, SpreadExplorationTopology.Legacy, "ebe29edc2975a7f310e3cd9b0ca4f8083c6d67fdb475c46a26279d0314d9bcaf"),
                ("Overworld.11.11.0", 64, SpreadExplorationTopology.Legacy, "3c0d0b961e7de77e7d873744b7fc522b143a9e94a07ba200ea058ab863f8f150"),
                ("Overworld.11.11.0", 64, SpreadExplorationTopology.BrokenEnclosures, "6c499af0cc813314acce5b288ac3217ece90746a182e7b8434f378cd92b13261"),
                ("Overworld.8.4.0", 64, SpreadExplorationTopology.Legacy, "b6e58c304b5e356166704c09e6bd3ba2929163c251268077ee3d4d5c562e800c"),
                ("Overworld.8.4.0", 64, SpreadExplorationTopology.BrokenEnclosures, "c79ae86c532fb7a0e60e111d8dbae361179e09b2cc88002de885d038d7b08f80"),
                ("Overworld.12.10.0", 65, SpreadExplorationTopology.Legacy, "60623b56dbbef54defdfd0afce59c083d7d3e6916856073744359c0600d7efd2"),
                ("Overworld.12.11.0", 65, SpreadExplorationTopology.Legacy, "5b79e66adc09e0d74f8b1c061e4b49ac7a174466ce2fec9f7ca2db065ae4d31b"),
                ("Overworld.11.11.0", 65, SpreadExplorationTopology.Legacy, "bbd66bee204b4b0f9d21032c6b425a67502235fa3d732ee6703eb4ea511fa786"),
                ("Overworld.11.11.0", 65, SpreadExplorationTopology.BrokenEnclosures, "f63b151b14bc881c6a6810f0fa1877b133afedc3207de122c064d8d37c81a089"),
                ("Overworld.8.4.0", 65, SpreadExplorationTopology.Legacy, "58298d97813c5d050b161c7fb5ced43d93d4ed59fc30d1788d8213e430ff1d51"),
                ("Overworld.8.4.0", 65, SpreadExplorationTopology.BrokenEnclosures, "a86b5615905ce195d9c250ded0815ca58d4082ac04810df2ffcdf3cdad2d76be") };
            foreach (var pin in unchanged.Where(row => row.Item2 == seed))
                Assert.AreEqual(pin.Item4, Hash(Plan(pin.Item1, pin.Item2, pin.Item3).Signature()), pin.Item1 + " " + pin.Item3);
            var oldKitchen = Plan(Kitchen, seed, SpreadExplorationTopology.Legacy);
            Assert.AreEqual(3, oldKitchen.ParcelCount); AssertOldHedgeEdges(oldKitchen);
            var oldEast = Plan(East, seed, SpreadExplorationTopology.Legacy);
            Assert.IsNull(oldEast.Landscape); Assert.Greater(Count(oldEast, "FlowerField"), 20); Assert.Zero(Count(oldEast, "CharmFlowers"));
            foreach (var topology in Topologies)
            {
                var outside = SpreadCompositionPlan.Create("Overworld.13.11.0", seed, Formation.Hedgerow, topology);
                AssertOldHedgeEdges(outside);
                Assert.AreEqual(topology == SpreadExplorationTopology.OffsetLanes ? 4 : topology == SpreadExplorationTopology.BrokenEnclosures ? 5 : 3, outside.ParcelCount);
                var north = Plan("Overworld.11.9.0", seed, topology); Assert.AreEqual("overgrown crofts", north.Landscape);
                var south = Plan("Overworld.11.11.0", seed, topology); Assert.AreEqual("crescent hollow", south.Landscape);
                // Root may capture these native baseline values during RED; no Python substitute is used.
                foreach (var p in new[] { oldKitchen, oldEast, outside, north, south })
                    TestContext.WriteLine("UNCHANGED " + p.ZoneID + " " + seed + " " + p.Topology + " " + Hash(p.Signature()));
            }
        }
        static string Hash(string text)
        {
            using (var h = System.Security.Cryptography.SHA256.Create())
                return BitConverter.ToString(h.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text))).Replace("-", "").ToLowerInvariant();
        }
        static bool Walk(Zone z, int x, int y) => In(x, y) && !z.GetCell(x, y).BlocksMovement()
            && z.TileState.Get(x, y)?.IsEmpty != false && !z.GetCell(x, y).Occupants.Any(e => e.HasTag("Creature")
                || e.HasPart<TriggerOnStepPart>() || e.HasPart<LiquidPoolPart>() || e.HasPart<GasPoolPart>());
        static Entity One(Zone z, string bp) => z.GetReadOnlyEntities().Single(e => e.BlueprintName == bp);
        static Zone Generate(HaulingContentScope scope, int seed, string id, out OverworldZoneManager manager)
        {
            scope.Seed(unchecked(seed ^ FormationSelector.StableIndex(id, int.MaxValue)));
            manager = OverworldZoneManager.CreateDetached(scope.Factory, seed, true); var zone = manager.GetZone(id);
            Assert.NotNull(zone, seed + " " + id); return zone;
        }
        [TestCase(64)] [TestCase(65)]
        public void ActualKitchenHasAReachableWorkingFrontAndAllFunctionalOwners(int seed)
        {
            using (var scope = new HaulingContentScope())
            {
                var z = Generate(scope, seed, Kitchen, out var manager); Assert.AreEqual(2, manager.Exploration.DispositionFor(Kitchen));
                var cook = One(z, "SpreadWaysideCook"); var at = z.GetEntityPosition(cook); var oven = One(z, "Oven"); var ov = z.GetEntityPosition(oven);
                int ex = (at.x - ov.x) / 2, ey = (at.y - ov.y) / 2;
                Assert.AreEqual(1, Math.Abs(ex) + Math.Abs(ey));
                (int x, int y) Local(int x, int y) => (at.x + x * ex - y * ey, at.y + x * ey + y * ex);
                var reached = Reach((40, 12), (x, y) => Walk(z, x, y)); Assert.True(reached.Contains((40, 12)));
                foreach (var offset in new[] { (-1, -1), (0, -1), (1, -1), (-1, 0), (1, 0), (-1, 1), (0, 1), (1, 1), (2, 1), (3, 1) })
                {
                    var p = Local(offset.Item1, offset.Item2); Assert.True(reached.Contains(p), "Working court blocked: " + seed + " local=" + offset + " cell=" + p);
                }
                foreach (string bp in new[] { "ConnectedBatchPan", "ConnectedKitchenEscrow", "ConnectedKitchenPickup", "Oven", "ClaspbeanCrop" })
                {
                    var owner = One(z, bp); var p = z.GetEntityPosition(owner);
                    Assert.GreaterOrEqual(Neighbors(p).Count(reached.Contains), 2, bp + " needs actual reachable standing cells.");
                    Assert.False(z.GenReservedCells.Contains(p), "The service may not be placed on its protected route.");
                }
                Assert.True(reached.Contains(z.GetEntityPosition(One(z, "Bed"))), "The cot is used by standing on it.");
                Assert.True(One(z, "Oven").GetPart<CampfirePart>().AllowRest == false);
                Assert.False(One(z, "Oven").GetPart<CampfirePart>().FiniteCooking);
                var crop = One(z, "ClaspbeanCrop").GetPart<CropPart>(); Assert.AreEqual(2, crop.GrowthStage);
                Assert.AreEqual(2, crop.YieldCount); Assert.AreEqual(1, crop.SeedYieldCount);
                Assert.That(cook.GetPart<InventoryPart>().Objects.Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1), Is.EqualTo(5));
                Assert.AreEqual(35, TradeSystem.GetDrams(cook));
            }
        }
        [TestCase(64)] [TestCase(65)]
        public void ActualEastRetainsOptionalForgeAuthorityAndReachableFiniteSources(int seed)
        {
            using (var scope = new HaulingContentScope())
            {
                var z = Generate(scope, seed, East, out var manager); int disposition = manager.Exploration.DispositionFor(East);
                Assert.That(disposition, Is.EqualTo(1).Or.EqualTo(2));
                var forge = z.GetReadOnlyEntities().SingleOrDefault(e => e.BlueprintName == "TinkersForge" && e.GetProperty(SpreadExplorationWorksites.RoleKey) == "forge");
                if (disposition == 1) { Assert.IsNull(forge); TestContext.WriteLine("Actual optional source refusal; no forge access claimed for seed=" + seed); return; }
                Assert.NotNull(forge); var reached = Reach((40, 12), (x, y) => Walk(z, x, y));
                foreach (var e in z.GetReadOnlyEntities().Where(e => e.GetProperty(SpreadExplorationWorksites.RoleKey) == "forge" || e.GetProperty(SpreadExplorationWorksites.RoleKey) == "binding-forage"))
                {
                    Assert.GreaterOrEqual(Neighbors(z.GetEntityPosition(e)).Count(reached.Contains), 2);
                    if (e.GetPart<HarvestablePart>() is HarvestablePart h) { Assert.False(h.Harvested); Assert.AreEqual("StoneburrSeed", h.YieldBlueprint); Assert.AreEqual(100, h.YieldChance); }
                }
                Assert.NotNull(forge.GetPart<ForgePart>());
            }
        }
        [Test] public void AcceptedKitchenRepairAndDepletionSurviveCacheAndRealSaveWithoutLayoutReplay()
        {
            using (var scope = new HaulingContentScope())
            {
                var z = Generate(scope, 64, Kitchen, out var manager); Assert.AreEqual(2, manager.Exploration.DispositionFor(Kitchen));
                var cook = One(z, "SpreadWaysideCook"); var pack = cook.GetPart<InventoryPart>(); var removed = pack.Objects[0]; Assert.True(pack.RemoveObject(removed));
                var pan = One(z, "ConnectedBatchPan"); pan.GetPart<RepairablePart>().Repaired = true;
                var crop = One(z, "ClaspbeanCrop"); Assert.True(z.RemoveEntity(crop));
                var player = scope.Factory.CreateEntity("Player"); Assert.True(z.AddEntity(player, 40, 12)); manager.SetActiveZone(z);
                string Snapshot(Zone zone) => string.Join(";", zone.GetReadOnlyEntities().OrderBy(e => e.ID).Select(e => e.ID + ":" + e.BlueprintName + ":" + zone.GetEntityPosition(e)));
                string before = Snapshot(z); manager.UnloadZone(Kitchen); Assert.AreSame(z, manager.GetZone(Kitchen)); Assert.AreEqual(before, Snapshot(z));
                var loaded = HotbarSaveFixture.RoundTrip(GameSessionState.Capture("working-landscape", "kitchen-court", manager, null, player));
                var restored = loaded.ZoneManager.ActiveZone; Assert.AreEqual(before, Snapshot(restored));
                Assert.True(One(restored, "ConnectedBatchPan").GetPart<RepairablePart>().Repaired);
                Assert.False(One(restored, "SpreadWaysideCook").GetPart<InventoryPart>().Objects.Any(e => e.ID == removed.ID));
                Assert.False(restored.GetReadOnlyEntities().Any(e => e.ID == crop.ID));
                Assert.AreSame(restored, loaded.ZoneManager.GetZone(Kitchen)); Assert.AreEqual(before, Snapshot(restored));
            }
        }
    }
}
