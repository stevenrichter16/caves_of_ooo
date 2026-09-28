using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    /// <summary>Existing source-placement measurement; does not assert that a native frame is legible.</summary>
    public sealed class SpreadGleaningLaneMeasurementTests
    {
        [Serializable] public sealed class Row { public string zone, condition; public bool poi, rare; public int ripe, plannedRows, budget; public List<Point> positions = new List<Point>(); }
        [Serializable] public sealed class Point { public string id; public int x, y, nearestLane, nearestVisibleLane; }
        [Serializable] public sealed class Report { public int seed; public string scope; public List<Row> zones = new List<Row>(); }
        [TestCase(1)] [TestCase(64)] [TestCase(1729)]
        public void CurrentFieldGleaningsRecordActualLaneDistanceAndLineOfSightWithoutChangingTheirBudget(int seed)
        {
            using (var scope = new DensityLootTestScope()) {
                var manager = OverworldZoneManager.CreateDetached(scope.Factory, seed);
                var report = new Report { seed = seed, scope = "Fresh composition source only, all current-map Spread FieldStrips addresses including separately labelled POI/rare exclusions; no completed-pipeline placement or native image claim." };
                for (int wy = 0; wy < WorldMap.Height; wy++) for (int wx = 0; wx < WorldMap.Width; wx++) {
                    string id = WorldMap.ToZoneID(wx, wy, 0);
                    if (manager.WorldMap.Tiles[wx, wy] != BiomeType.Spread || FormationSelector.For(BiomeType.Spread, id) != Formation.FieldStrips) continue;
                    var z = new Zone(id); var source = new SpreadCompositionBuilder(seed) { FormationOverride = Formation.FieldStrips };
                    var rng = new Random(seed); var expectedRng = new Random(seed);
                    Assert.True(source.BuildZone(z, scope.Factory, rng)); Assert.AreEqual(expectedRng.Next(), rng.Next(), "Composition must not consume caller RNG.");
                    var plan = source.Plan; var lanes = new List<(int x, int y)>(); int planned = 0;
                    for (int y = 0; y < Zone.Height; y++) for (int x = 0; x < Zone.Width; x++) {
                        if (plan.IsApproach(x, y) && !z.GetCell(x, y).BlocksMovement()) lanes.Add((x, y));
                        if (plan.ObjectAt(x, y) == "CropRow") planned++;
                    }
                    Assert.IsNotEmpty(lanes); var ripe = z.GetReadOnlyEntities().Where(e => e.BlueprintName == "RipeCropRow").ToArray();
                    int budget = plan.Condition == "tended" ? 3 : plan.Condition == "returning scrub" ? 2 : 1;
                    Assert.AreEqual(Math.Min(budget, planned), ripe.Length);
                    var row = new Row { zone = id, condition = plan.Condition, poi = manager.WorldMap.GetPOI(wx, wy) != null, rare = id == manager.RareEncounters.PairZoneID || id == manager.RareEncounters.ViperZoneID, ripe = ripe.Length, plannedRows = planned, budget = budget };
                    foreach (var e in ripe) {
                        var p = z.GetEntityPosition(e); Assert.AreEqual("CropRow", plan.ObjectAt(p.x, p.y)); Assert.NotNull(e.GetPart<FieldHarvestPart>());
                        row.positions.Add(new Point { id = e.ID, x = p.x, y = p.y,
                            nearestLane = lanes.Min(l => AIHelpers.ChebyshevDistance(l.x, l.y, p.x, p.y)),
                            nearestVisibleLane = lanes.Where(l => AIHelpers.HasLineOfSight(z, l.x, l.y, p.x, p.y)).Select(l => AIHelpers.ChebyshevDistance(l.x, l.y, p.x, p.y)).DefaultIfEmpty(-1).Min() });
                    }
                    report.zones.Add(row);
                }
                Assert.IsNotEmpty(report.zones);
                string directory = Environment.GetEnvironmentVariable("COO_SPREAD_GLEANING_MEASUREMENT_OUTPUT");
                if (string.IsNullOrEmpty(directory)) directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/Verification/SpreadDiscovery/M3/Measurements"));
                Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, "current-field-lanes-" + seed + ".json"), JsonUtility.ToJson(report, true));
                TestContext.WriteLine("Measured " + report.zones.Count + " FieldStrips source graphs; " + report.zones.Sum(r => r.ripe) + " existing ripe rows.");
            }
        }
    }
}
