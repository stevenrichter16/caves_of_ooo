using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class DensityPopulationDiagnosticsTests
    {
        EntityFactory _factory;
        NarrativeStatePart _previousNarrative;
        [OneTimeSetUp] public void Load()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,
                "Resources/Content/Blueprints/Objects.json")));
        }
        [SetUp] public void Setup()
        {
            _previousNarrative = NarrativeStatePart.Current;
            NarrativeStatePart.Current = null;
            Diag.ResetAll();
        }
        [TearDown] public void Cleanup() { NarrativeStatePart.Current = _previousNarrative; Diag.ResetAll(); }
        static Payload[] Records(string kind) => DiagQuery.Apply(new DiagQuery.Filter
            { Category = "worldgen", Kind = kind }).Records.Select(r => new Payload(r.PayloadJson)).ToArray();
        static PopulationTable Group() => new PopulationTable { Name = "intervals", Entries = new List<PopulationEntry> {
            new PopulationEntry { BlueprintName = "first", Weight = 1, MinCount = 1, MaxCount = 1, EncounterGroup = "pack" },
            new PopulationEntry { BlueprintName = "second", Weight = 3, MinCount = 1, MaxCount = 1, EncounterGroup = "pack" } } };

        [TestCase(0.0, "first")]
        [TestCase(0.25, "second")]
        [TestCase(0.75, "second")]
        public void WeightedDiagnostics_ReportActualHalfOpenSelectionIntervals(double value, string expected)
        {
            var result = Group().Roll(new Rolls { Double = value }, "test-zone");
            CollectionAssert.AreEqual(new[] { expected }, result);
            var records = Records("PopulationRolled"); Assert.AreEqual(2, records.Length);
            foreach (var record in records)
            {
                Assert.AreEqual("weighted_choice", record.Text("rollKind"));
                Assert.IsTrue(record.IsNull("threshold"), "a weight mass is not a threshold against the global roll");
                double start = record.Number("selectionStart"), end = record.Number("selectionEnd");
                bool selected = record.Text("reason") == "selected";
                Assert.AreEqual(selected, value >= start && value < end);
                Assert.AreEqual(record.Text("blueprint") == "first" ? 0 : 0.25, start);
                Assert.AreEqual(record.Text("blueprint") == "first" ? 0.25 : 1, end);
            }
        }

        [TestCase(0.2, true)] [TestCase(0.3, false)]
        public void AmbientDiagnostics_DistinguishChanceFromFixedCount(double value, bool optional)
        {
            var table = new PopulationTable { Entries = new List<PopulationEntry> {
                new PopulationEntry { BlueprintName = "optional", Weight = 1, MaxCount = 1 },
                new PopulationEntry { BlueprintName = "fixed", Weight = 3, MinCount = 1, MaxCount = 1 } } };
            var result = table.Roll(new Rolls { Double = value });
            Assert.AreEqual(optional, result.Contains("optional"));
            var chance = Records("PopulationRolled").Single(r => r.Text("blueprint") == "optional");
            Assert.AreEqual("chance", chance.Text("rollKind"));
            Assert.AreEqual(0.25, chance.Number("threshold"));
            Assert.IsTrue(chance.IsNull("selectionStart"));
            var fixedCount = Records("PopulationRolled").Single(r => r.Text("blueprint") == "fixed");
            Assert.AreEqual("fixed_count", fixedCount.Text("rollKind"));
            Assert.IsTrue(fixedCount.IsNull("threshold"));
            Assert.AreEqual(-1, fixedCount.Number("roll"));
        }

        [Test]
        public void GatedAndInvalidRows_ReportNoRollInsteadOfInventingAnOutcome()
        {
            var table = Group();
            table.Entries[0].RequiresWorldFlag = "missing";
            table.Entries[1].Weight = 0;
            Assert.IsEmpty(table.Roll(new Rolls { ThrowOnRoll = true }));
            var records = Records("PopulationRolled"); Assert.AreEqual(2, records.Length);
            foreach (var record in records)
            {
                Assert.AreEqual("not_rolled", record.Text("rollKind"));
                Assert.AreEqual(-1, record.Number("roll"));
                Assert.IsTrue(record.IsNull("threshold"));
                Assert.IsTrue(record.IsNull("selectionStart"));
            }
        }

        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void MimicDiagnostics_ReportCountRangeAndPlacedCount(int count)
        {
            var zone = Lair(new Rolls { Mimics = count });
            var record = Records("AmbusherRolled").Single(r => r.Text("blueprint") == "MimicChest");
            Assert.AreEqual("count", record.Text("rollKind"));
            Assert.AreEqual(0, record.Integer("minInclusive"));
            Assert.AreEqual(3, record.Integer("maxExclusive"));
            Assert.IsTrue(record.IsNull("threshold"));
            Assert.AreEqual(count, record.Integer("roll"));
            Assert.AreEqual(count, record.Integer("placed"));
            Assert.AreEqual(count, zone.GetAllEntities().Count(e => e.BlueprintName == "MimicChest"));
            Assert.AreEqual(count == 0 ? "roll_missed" : "placed", record.Text("reason"));
        }

        [TestCase(29, 1)] [TestCase(30, 0)]
        public void BiomeAmbusherDiagnostics_KeepPercentThresholdAndBoundary(int value, int placed)
        {
            var zone = Lair(new Rolls { Percent = value });
            var record = Records("AmbusherRolled").Single(r => r.Text("blueprint") == "AmbushBandit");
            Assert.AreEqual("chance", record.Text("rollKind"));
            Assert.AreEqual(30, record.Integer("threshold"));
            Assert.AreEqual(0, record.Integer("minInclusive"));
            Assert.AreEqual(100, record.Integer("maxExclusive"));
            Assert.AreEqual(placed, record.Integer("placed"));
            Assert.AreEqual(placed, zone.GetAllEntities().Count(e => e.BlueprintName == "AmbushBandit"));
        }

        [Test]
        public void FullLair_ReportsNoOpenCells_WithoutConsumingAnySpawnRolls()
        {
            var zone = new Zone("blocked-lair");
            zone.ForEachCell((cell, x, y) => {
                var wall = new Entity { ID = Guid.NewGuid().ToString("N") }; wall.Tags["Solid"] = "";
                Assert.IsTrue(zone.AddEntity(wall, x, y));
            });
            Assert.IsTrue(new LairPopulationBuilder(BiomeType.Beating, null).BuildZone(zone, _factory, new Rolls { ThrowOnRoll = true }));
            var record = Records("LairPopulationRejected").Single();
            Assert.AreEqual("no_open_cells", record.Text("reason"));
            Assert.AreEqual("blocked-lair", record.Text("zone"));
            Assert.IsEmpty(Records("PopulationRolled"));
            Assert.IsEmpty(Records("AmbusherRolled"));
            Diag.ResetAll();
            Lair(new Rolls()); Assert.IsEmpty(Records("LairPopulationRejected"));
        }

        [Test]
        public void DiagnosticsEnabledOrDisabled_LeaveRngSequenceAndPopulationIdentical()
        {
            var enabled = new Rolls { Double = 0.75, Percent = 29, Mimics = 2 };
            var enabledZone = Lair(enabled);
            var table = Group(); var enabledRoll = table.Roll(enabled);
            Diag.SetChannel("worldgen", false);
            var disabled = new Rolls { Double = 0.75, Percent = 29, Mimics = 2 };
            var disabledZone = Lair(disabled); var disabledRoll = table.Roll(disabled);
            CollectionAssert.AreEqual(enabled.Calls, disabled.Calls);
            CollectionAssert.AreEqual(enabledRoll, disabledRoll);
            CollectionAssert.AreEquivalent(enabledZone.GetAllEntities().Select(e => e.BlueprintName), disabledZone.GetAllEntities().Select(e => e.BlueprintName));
        }

        Zone Lair(Rolls rng)
        {
            var zone = new Zone("diagnostic-lair");
            Assert.IsTrue(new LairPopulationBuilder(BiomeType.Beating, null).BuildZone(zone, _factory, rng));
            return zone;
        }
        // Keep native tests independent of the runner's Newtonsoft implementation.
        // These diagnostics are flat scalar JSON; require each exact field to exist
        // so a missing field cannot accidentally pass a null or zero assertion.
        sealed class Payload
        {
            readonly string _json;
            public Payload(string json) { _json = json; }
            string Scalar(string key)
            {
                string pattern = "\\\"" + Regex.Escape(key) + "\\\"\\s*:\\s*(null|-?\\d+(?:\\.\\d+)?(?:[eE][+-]?\\d+)?|\\\"(?:\\\\.|[^\\\"\\\\])*\\\")";
                var match = Regex.Match(_json, pattern);
                Assert.IsTrue(match.Success, "Missing diagnostic scalar: " + key + " in " + _json);
                return match.Groups[1].Value;
            }
            public bool IsNull(string key) => Scalar(key) == "null";
            public double Number(string key) => double.Parse(Scalar(key), CultureInfo.InvariantCulture);
            public int Integer(string key) => int.Parse(Scalar(key), CultureInfo.InvariantCulture);
            public string Text(string key) => JsonUtility.FromJson<TextValue>("{\"value\":" + Scalar(key) + "}").value;
            [Serializable] sealed class TextValue { public string value; }
        }
        sealed class Rolls : System.Random
        {
            public double Double = 0.99;
            public int Mimics, Percent = 99;
            public bool ThrowOnRoll;
            public readonly List<string> Calls = new List<string>();
            void Called(string kind) { if (ThrowOnRoll) Assert.Fail("No random draws expected"); Calls.Add(kind); }
            public override double NextDouble() { Called("double"); return Double; }
            public override int Next(int max) { Called("next:" + max); return max == 100 ? Percent : max == 3 ? Mimics : 0; }
            public override int Next(int min, int max) { Called("range:" + min + ":" + max); return min; }
        }
    }
}
