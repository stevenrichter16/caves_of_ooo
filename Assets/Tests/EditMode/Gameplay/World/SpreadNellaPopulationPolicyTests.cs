using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class SpreadNellaPopulationPolicyTests
    {
        const string Group = "SpreadTier1Encounter";
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        static string[] Rewrite(PopulationTable table, IReadOnlyList<string> rolls,
            SpreadExplorationFamily family = SpreadExplorationFamily.SeedKeepersPlot, bool connectedKeeper = true)
        {
            var method = typeof(SpreadExplorationPopulationPolicy).GetMethods(All)
                .SingleOrDefault(m => m.Name == "Rewrite" && m.GetParameters().Length == 5);
            Assert.NotNull(method, "Exact connected keeper admission must opt into the pure roll policy.");
            return (string[])method.Invoke(null, new object[] { table, rolls, family, false, connectedKeeper });
        }

        [Test] public void EachActualEncounterSlotBecomesOneGrazerWithoutChangingOtherRolls()
        {
            var rolls = new[] { "Magpie", "Viper", "Hatchet", "MarlbackScrabbler", "Viper", "BerryBush", "PetDog" };
            var before = rolls.ToArray(); var table = PopulationTable.SpreadTier1(); var entries = table.Entries.ToArray();
            CollectionAssert.AreEqual(new[] { "Magpie", "ReedbackGrazer", "Hatchet", "ReedbackGrazer", "ReedbackGrazer", "BerryBush", "PetDog" }, Rewrite(table, rolls));
            CollectionAssert.AreEqual(before, rolls); CollectionAssert.AreEqual(entries, table.Entries);
        }

        [TestCase(false)] [TestCase(true)]
        public void NoActualHostileRollCannotMintAdditionalAnimals(bool empty)
        {
            var rolls = empty ? Array.Empty<string>() : new[] { "Magpie", "PetDog", "Hatchet", "ReedbackGrazer" };
            CollectionAssert.AreEqual(rolls, Rewrite(PopulationTable.SpreadTier1(), rolls));
        }

        [Test] public void ExistingThreeArgumentCallRetainsOrdinaryKeeperRolls()
        {
            var rolls = new[] { "Magpie", "Viper", "Viper" };
            CollectionAssert.AreEqual(rolls, SpreadExplorationPopulationPolicy.Rewrite(PopulationTable.SpreadTier1(), rolls, SpreadExplorationFamily.SeedKeepersPlot));
        }

        [Test] public void ExplicitFalseAdmissionRetainsOrdinaryKeeperRolls()
        {
            var rolls = new[] { "Viper", "MarlbackScrabbler", "Hatchet" };
            CollectionAssert.AreEqual(rolls, Rewrite(PopulationTable.SpreadTier1(), rolls, connectedKeeper: false));
        }

        [TestCase(SpreadExplorationFamily.None)] [TestCase(SpreadExplorationFamily.RoadSpill)]
        [TestCase(SpreadExplorationFamily.WaysideKitchen)]
        public void KeeperOptInDoesNotRewriteUnrelatedFamilies(SpreadExplorationFamily family)
        {
            var rolls = new[] { "Viper", "MarlbackScrabbler", "Magpie" };
            CollectionAssert.AreEqual(rolls, Rewrite(PopulationTable.SpreadTier1(), rolls, family));
        }

        [TestCase(false)] [TestCase(true)]
        public void MissingOrForeignTableGrantsNoEncounterAuthority(bool missing)
        {
            var rolls = new[] { "Viper", "Viper", "Magpie" }; var table = PopulationTable.SpreadTier1(); table.Name = "Foreign";
            CollectionAssert.AreEqual(rolls, Rewrite(missing ? null : table, rolls));
        }

        [TestCase("duplicate-group")] [TestCase("duplicate-ordinary")] [TestCase("missing-viper")]
        [TestCase("foreign-group")] [TestCase("extra-group-row")] [TestCase("missing-entries")]
        public void AmbiguousEncounterSchemaKeepsExactInput(string fault)
        {
            var table = PopulationTable.SpreadTier1(); var rolls = new[] { "Viper", "MarlbackScrabbler", "Viper", "Magpie" }; var before = rolls.ToArray();
            if (fault == "duplicate-group") table.Entries.Add(new PopulationEntry { BlueprintName = "Viper", EncounterGroup = Group });
            if (fault == "duplicate-ordinary") table.Entries.Add(new PopulationEntry { BlueprintName = "Viper" });
            if (fault == "missing-viper") table.Entries.RemoveAll(e => e.BlueprintName == "Viper");
            if (fault == "foreign-group") table.Entries.Single(e => e.BlueprintName == "Viper").EncounterGroup = "Foreign";
            if (fault == "extra-group-row") table.Entries.Add(new PopulationEntry { BlueprintName = "Magpie", EncounterGroup = Group });
            if (fault == "missing-entries") table.Entries = null;
            CollectionAssert.AreEqual(before, Rewrite(table, rolls)); CollectionAssert.AreEqual(before, rolls);
        }

        sealed class Probe : IZoneBuilder
        {
            readonly Action<Zone, EntityFactory> action; public string Name => "KeeperPopulationTestSnapshot";
            public int Priority { get; }
            internal Probe(int priority, Action<Zone, EntityFactory> action) { Priority = priority; this.action = action; }
            public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng) { action(zone, factory); return true; }
        }
        sealed class ObservedPopulation : OverworldZoneManager
        {
            readonly string mode;
            internal PopulationBuilder Population;
            internal Dictionary<Entity, (int x, int y)> Before;
            internal Entity[] Added;
            internal Entity Decoy;
            internal bool SourcesPreserved, ReceiptCurrent;
            internal ObservedPopulation(EntityFactory factory, string mode) : base(factory, 242967504)
            {
                this.mode = mode;
                if (mode == "legacy")
                {
                    var legacy = typeof(SpreadExplorationPlan).GetMethod("Legacy", All).Invoke(null, new object[] { this });
                    typeof(OverworldZoneManager).GetProperty("Exploration", All).SetValue(this, legacy);
                }
            }
            protected override ZoneGenerationPipeline GetPipelineForZone(string id)
            {
                var pipeline = base.GetPipelineForZone(id);
                Population = pipeline.Builders.OfType<PopulationBuilder>().Single();
                // Controlled source integration: preserve the real cold guard and
                // manager authority while fixing counts and isolating later site use.
                pipeline.RemoveBuilders<SpreadExplorationBuilder>();
                Population.SpreadEncounter = null; Population.CaptureSourceReceipts = true;
                if (mode == "unbound") Population.ExplorationManager = null;
                Population.Table = PopulationTable.SpreadTier1();
                foreach (var row in Population.Table.Entries)
                {
                    row.MinCount = row.MaxCount = row.EncounterGroup == Group ? 2
                        : row.BlueprintName == "Magpie" || row.BlueprintName == "PetDog" ? 1 : 0;
                }
                pipeline.AddBuilder(new Probe(3999, (zone, factory) =>
                {
                    if (mode == "current")
                    {
                        var cell = zone.GetCell(2, 2);
                        Decoy = factory.CreateEntity("ReedbackGrazer"); Assert.NotNull(Decoy);
                        Assert.True(zone.AddEntity(Decoy, cell.X, cell.Y));
                    }
                    Before = zone.GetReadOnlyEntities().ToDictionary(e => e, zone.GetEntityPosition);
                }));
                pipeline.AddBuilder(new Probe(4002, (zone, factory) =>
                {
                    Added = zone.GetReadOnlyEntities().Where(e => !Before.ContainsKey(e)).ToArray();
                    SourcesPreserved = Before.All(e => zone.GetEntityCell(e.Key) != null && zone.GetEntityPosition(e.Key) == e.Value);
                    ReceiptCurrent = Population.SourceReceipt?.IsCurrent == true;
                }));
                return pipeline;
            }
        }

        [TestCase("current")] [TestCase("other-zone")] [TestCase("legacy")]
        [TestCase("unbound")] [TestCase("missing-grazer")] [TestCase("other-plot")]
        public void ColdCallerKeepsExactRollOwnershipAndRefusesForeignOrMissingReplacement(string mode)
        {
            using (var scope = new HaulingContentScope())
            {
                scope.Seed(242967504);
                if (mode == "missing-grazer") Assert.True(scope.Factory.Blueprints.Remove("ReedbackGrazer"));
                var manager = new ObservedPopulation(scope.Factory, mode);
                string zoneID = mode == "other-zone" ? "Overworld.12.10.0" : "Overworld.11.8.0";
                if (mode == "other-plot")
                {
                    var other = manager.Exploration.Entries.FirstOrDefault(e => e.PlacementEligible
                        && e.Family == SpreadExplorationFamily.SeedKeepersPlot && e.ZoneID != "Overworld.11.8.0");
                    Assert.NotNull(other, "Counter must exercise a real current same-family assignment elsewhere.");
                    zoneID = other.ZoneID;
                }
                var zone = manager.GetZone(zoneID); Assert.NotNull(zone);
                Assert.True(manager.SourcesPreserved, "Population must not move or remove already published source owners.");
                Assert.AreEqual(4, manager.Added.Length); Assert.AreEqual("Magpie", manager.Added[0].BlueprintName);
                Assert.AreEqual("PetDog", manager.Added[1].BlueprintName);
                string[] encounters = manager.Added.Skip(2).Select(e => e.BlueprintName).ToArray();
                if (mode == "current") CollectionAssert.AreEqual(new[] { "ReedbackGrazer", "ReedbackGrazer" }, encounters);
                else Assert.True(encounters.All(bp => bp == "Viper" || bp == "MarlbackScrabbler"));
                Assert.True(manager.ReceiptCurrent, "Replacement indices must retain the complete encounter receipt.");
                CollectionAssert.AreEqual(manager.Added.Skip(2).ToArray(), manager.Population.SourceReceipt.Owners);
                CollectionAssert.AreEqual(manager.Added.Take(2).ToArray(), manager.Population.AmbientSourceReceipt.Owners);
                Assert.IsEmpty(manager.Population.AmbientReplacementReceipt.Owners);
                if (manager.Decoy != null) Assert.False(manager.Population.SourceReceipt.Owners.Contains(manager.Decoy));
                foreach (var grazer in manager.Added.Where(e => e.BlueprintName == "ReedbackGrazer"))
                {
                    var role = grazer.GetPart<SpreadGrazerPart>(); Assert.NotNull(role); Assert.False(role.Configured);
                    Assert.False(role.Fed); Assert.Null(role.Food); Assert.Null(role.ReservedRow); Assert.Null(role.Hunter);
                }
            }
        }
    }
}
