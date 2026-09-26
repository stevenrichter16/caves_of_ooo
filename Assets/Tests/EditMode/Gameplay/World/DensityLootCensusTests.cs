using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    // Explicit wiring matters: factory-only tests with null Loadout/Trader
    // statics otherwise measure empty inventories, not shipped content.
    internal sealed class DensityLootTestScope : IDisposable
    {
        readonly HotbarSaveFixture _nativeScope = new HotbarSaveFixture(false, false);
        readonly EntityFactory _loadout = LoadoutPart.Factory, _container = ContainerPlacementService.Factory,
            _death = LootDropSystem.Factory, _trader = TraderPart.Factory;
        readonly Random _loadoutRng = LoadoutPart.Rng, _deathRng = LootDropSystem.Rng, _traderRng = TraderPart.Rng;
        readonly NarrativeStatePart _narrative = NarrativeStatePart.Current;
        public readonly EntityFactory Factory = new EntityFactory();
        public DensityLootTestScope()
        {
            Factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            LootTableRegistry.Initialize(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/Loot/LootTables.json")));
            LoadoutPart.Factory = ContainerPlacementService.Factory = LootDropSystem.Factory = TraderPart.Factory = Factory;
            NarrativeStatePart.Current = null;
            Seed(1); MessageLog.Clear();
        }
        public void Seed(int seed)
        {
            LoadoutPart.Rng = new Random(seed);
            TraderPart.Rng = new Random(seed ^ 0x7135);
            LootDropSystem.Rng = new Random(seed ^ 0x2143);
        }
        public void Dispose()
        {
            LoadoutPart.Factory = _loadout; ContainerPlacementService.Factory = _container;
            LootDropSystem.Factory = _death; TraderPart.Factory = _trader;
            LoadoutPart.Rng = _loadoutRng; LootDropSystem.Rng = _deathRng; TraderPart.Rng = _traderRng;
            NarrativeStatePart.Current = _narrative;
            LootTableRegistry.ResetForTests(); MessageLog.Clear();
            _nativeScope.Dispose();
        }
        public static IEnumerable<Entity> Gear(Entity actor)
        {
            var inv = actor.GetPart<InventoryPart>();
            return inv == null ? Enumerable.Empty<Entity>() : inv.Objects.Concat(inv.EquippedItems.Values).Distinct();
        }
    }

    /// <summary>Bounded actual-content census, runnable against archived baseline
    /// or current content. It observes core generation/actions, not native travel.
    /// COO_DENSITY_CENSUS_OUTPUT optionally chooses the report destination.</summary>
    public class DensityLootCensusTests
    {
        public static readonly int[] Seeds = { 1, 64, 1729, 2026, 729490642 };
        public static readonly string[] Hostiles = { "MarlbackScrabbler", "MarlbackGleaner", "MarlbackTunnelguard", "DesertBandit",
            "RuinScavenger", "SkeletalSentry", "MarlbackWallkeeper", "MarlbackBreacher", "AmbushBandit", "RuneCultist" };
        [Serializable] public class ItemRow { public string blueprint, category; public int tier, units; public double commerce, sell, weight; }
        [Serializable] public class SourceRow
        {
            public int seed, tier, containers, openingAttempts, opened, refused, locked, noApproach, creatures, equipped, carried;
            public string zone, biome, source, currentSurfaceBiome, currentPoi, currentSource;
            public bool authoredColumn, classificationChanged;
            public List<ItemRow> items = new List<ItemRow>();
            public List<string> species = new List<string>();
        }
        [Serializable] public class LoadoutRow
        {
            public string blueprint; public int trials, weapons, armor, equipped, carried;
            public double commerce, sell, av, dv;
            public List<ItemRow> items = new List<ItemRow>();
        }
        [Serializable] public class ExpectedRow { public string table; public double items, commerce, sell; }
        [Serializable] public class Report
        {
            public string runtime, blueprintSha256, lootSha256, canVerify, cannotVerify, selectionSha256, accessDefinition;
            public int schemaVersion;
            public int[] seeds;
            public List<SourceRow> sources = new List<SourceRow>();
            public List<LoadoutRow> loadouts = new List<LoadoutRow>();
            public List<ExpectedRow> expectations = new List<ExpectedRow>();
            public List<string> exclusions = new List<string>();
        }

        [TestCase("WaterTonic", "tonic")] [TestCase("StoneskinTonic", "tonic")]
        [TestCase("HealingTonic", "tonic")] [TestCase("BurnSalve", "tonic")]
        [TestCase("FireTonic", "offense")] [TestCase("PoisonTonic", "offense")]
        [TestCase("FrostTonic", "offense")] [TestCase("AcidTonic", "offense")]
        [TestCase("LightningTonic", "offense")] [TestCase("BleedTonic", "offense")]
        [TestCase("PoisonGasGrenade", "offense")] [TestCase("SleepGasGrenade", "offense")]
        [TestCase("StunGasGrenade", "offense")]
        public void UtilityTonicsAreNotCountedAsOffensiveFinds(string blueprint, string category)
        {
            using (var scope = new DensityLootTestScope())
                Assert.AreEqual(category, Category(scope.Factory.CreateEntity(blueprint)));
        }

        [TestCase(1, 1, 1)] [TestCase(20, 20, 7)]
        public void StackValuesAreCountedOnceAndSaleRoundingUsesTheActualStack(int units, int commerce, int sell)
        {
            using (var scope = new DensityLootTestScope())
            {
                var coin = scope.Factory.CreateEntity("GoldCoin");
                coin.GetPart<StackerPart>().StackCount = units;
                var rows = new List<ItemRow>(); Add(rows, coin, 0.35);
                Assert.AreEqual(units, rows.Single().units);
                Assert.AreEqual(commerce, rows.Single().commerce);
                Assert.AreEqual(sell, rows.Single().sell);
            }
        }

        [Test]
        public void FiveSeedRealZonesAndActualLoadoutsProduceAnAuditableCensus()
        {
            using (var scope = new DensityLootTestScope())
            {
                string selectionPath = Environment.GetEnvironmentVariable("COO_DENSITY_BASELINE_CENSUS");
                if (string.IsNullOrEmpty(selectionPath)) selectionPath = Path.GetFullPath(Path.Combine(Application.dataPath,
                    "../Docs/Verification/DensityCompletion/Loot/census-before.json.gz"));
                var baseline = ReadRecordedReport(selectionPath);
                // Validate the entire cohort before generating any measured source.
                foreach (int seed in Seeds) RecordedZones(baseline, seed);
                var factory = scope.Factory;
                var player = factory.CreateEntity("Player");
                double performance = TradeSystem.GetTradePerformance(player);
                var report = new Report { seeds = Seeds, runtime = Environment.Version.ToString(), schemaVersion = 2,
                    selectionSha256 = FileHash(selectionPath),
                    accessDefinition = "containers=observed; locked=combined key/legacy lock skips; noApproach=no physically legal adjacent cell; openingAttempts=dispatched actions; opened=actor OpenContainer event naming the exact container; refused=attempts without that event. Contents/value count generated stock regardless of access.",
                    blueprintSha256 = Hash("Blueprints/Objects.json"), lootSha256 = Hash("Data/Loot/LootTables.json"),
                    canVerify = "Actual detached zone generation, stocked contents, observed core OpenContainer events with legal adjacent placement, actual factory equipped loadouts, actual designed starter grant and generated shop inventories, exact table expectations and neutral sale quotes.",
                    cannotVerify = "Bounded selected cells, not all 400; no native bootstrap/input/rendering, natural player acquisition or balance. Locked/unapproachable containers remain unopened but contents are counted separately. Stable-hash standalone maps differ from Unity. Loadout RNG is explicitly seeded per zone; this census does not claim production global RNG replay. Equipment samples are factory distributions, not natural encounter frequency." };
                Assert.AreEqual(3, NewGameLoadout.Grant(player, factory));
                var starter = new SourceRow { source = "DesignedStarterGrant", biome = "none", zone = "none", tier = 1 };
                foreach (var item in DensityLootTestScope.Gear(player)) Add(starter.items, item, performance);
                report.sources.Add(starter);
                foreach (int seed in Seeds)
                {
                    var manager = OverworldZoneManager.CreateDetached(factory, seed);
                    foreach (var recorded in RecordedZones(baseline, seed))
                    {
                        var coords = WorldMap.FromZoneID(recorded.zone);
                        MeasureZone(report, scope, manager, player, seed, coords.x, coords.y, coords.z,
                            (BiomeType)Enum.Parse(typeof(BiomeType), recorded.biome), recorded.source, performance);
                    }
                    scope.Seed(seed ^ 0x4221);
                    var town = manager.GetZone(MorrowfastSceneRuntime.ZoneID);
                    Assert.NotNull(town);
                    foreach (var trader in town.GetAllEntities().Where(e => e.HasPart<TraderPart>()).OrderBy(e => e.BlueprintName))
                    {
                        var row = new SourceRow { seed = seed, source = "Shop:" + trader.BlueprintName,
                            zone = town.ZoneID, biome = "Spread", tier = 1 };
                        foreach (var item in trader.GetPart<InventoryPart>().Objects) Add(row.items, item, performance);
                        report.sources.Add(row);
                    }
                    Assert.That(report.sources.Any(r => r.seed == seed && r.source.StartsWith("Shop:")), Is.True, "real shops must be stocked");
                }
                foreach (string blueprint in Hostiles)
                {
                    var row = new LoadoutRow { blueprint = blueprint, trials = 256 };
                    for (int seed = 0; seed < row.trials; seed++)
                    {
                        scope.Seed(seed); var actor = factory.CreateEntity(blueprint);
                        var inv = actor.GetPart<InventoryPart>();
                        row.equipped += inv.EquippedItems.Values.Distinct().Count(); row.carried += inv.Objects.Count;
                        row.av += CombatSystem.GetAV(actor); row.dv += CombatSystem.GetDV(actor);
                        foreach (var item in DensityLootTestScope.Gear(actor)) Add(row.items, item, performance);
                    }
                    row.weapons = row.items.Where(r => r.category == "weapon").Sum(r => r.units);
                    row.armor = row.items.Where(r => r.category == "armor").Sum(r => r.units);
                    row.commerce = row.items.Sum(r => r.commerce); row.sell = row.items.Sum(r => r.sell);
                    report.loadouts.Add(row);
                }
                foreach (var prefix in new[] { "CrateT", "UrnT", "BoneCacheT", "StrongBoxT", "ReliquaryT" })
                    for (int tier = 1; tier <= 3; tier++) report.expectations.Add(Expected(prefix + tier, factory, performance));
                Assert.AreEqual(120, report.sources.Count(r => r.source == "Wilderness"));
                Assert.AreEqual(15, report.sources.Count(r => r.source == "Underground"));
                Assert.AreEqual(15, report.sources.Count(r => r.source == "Lair"));
                CollectionAssert.AreEqual(Seeds.SelectMany(seed => RecordedZones(baseline, seed)).Select(r => r.seed + ":" + r.zone + ":" + r.source + ":" + r.biome),
                    report.sources.Where(r => IsZoneSource(r.source)).Select(r => r.seed + ":" + r.zone + ":" + r.source + ":" + r.biome));
                Assert.That(report.sources.Sum(r => r.opened), Is.GreaterThan(0));
                Assert.That(report.sources.Where(r => r.source == "Wilderness").Sum(r => r.containers), Is.GreaterThan(0));
                string output = Environment.GetEnvironmentVariable("COO_DENSITY_CENSUS_OUTPUT");
                if (string.IsNullOrEmpty(output)) output = Path.Combine(Path.GetTempPath(), "density-loot-census-" + Guid.NewGuid().ToString("N") + ".json");
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
                File.WriteAllText(output, JsonUtility.ToJson(report, true));
                TestContext.Progress.WriteLine("Density loot census: " + output + "; source rows=" + report.sources.Count + "; opened=" + report.sources.Sum(r => r.opened));
            }
        }

        static bool IsZoneSource(string source) => source == "Wilderness" || source == "Lair" || source == "Underground";

        // Recorded selection is immutable evidence. Reject invalid input instead of
        // silently changing membership with a new scan of the current world map.
        public static List<SourceRow> RecordedZones(Report baseline, int seed)
        {
            if (baseline?.sources == null) throw new InvalidOperationException("Missing recorded census sources.");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var selected = new List<SourceRow>();
            foreach (var row in baseline.sources)
            {
                if (row == null) throw new InvalidOperationException("Null recorded source row.");
                if (row.source == "DesignedStarterGrant" || (row.source?.StartsWith("Shop:", StringComparison.Ordinal) == true && row.source.Length > 5)) continue;
                if (!IsZoneSource(row.source)) throw new InvalidOperationException("Unsupported recorded source: " + row.source);
                if (!Seeds.Contains(row.seed)) throw new InvalidOperationException("Unexpected recorded seed: " + row.seed);
                var at = WorldMap.FromZoneID(row.zone);
                if (!WorldMapAuthoring.InBounds(at.x, at.y) || at.z < 0 || row.zone != WorldMap.ToZoneID(at.x, at.y, at.z))
                    throw new InvalidOperationException("Invalid recorded zone: " + row.zone);
                if ((row.source == "Underground") != (at.z > 0)) throw new InvalidOperationException("Recorded source/depth mismatch: " + row.zone);
                if (!Enum.TryParse<BiomeType>(row.biome, out var biome) || !Enum.IsDefined(typeof(BiomeType), biome) || biome.ToString() != row.biome)
                    throw new InvalidOperationException("Invalid recorded biome: " + row.biome);
                if (!seen.Add(row.seed + ":" + row.zone)) throw new InvalidOperationException("Duplicate recorded zone: " + row.zone);
                if (row.seed == seed) selected.Add(new SourceRow { seed = row.seed, zone = row.zone, source = row.source, biome = row.biome });
            }
            if (selected.Count == 0) throw new InvalidOperationException("No recorded zones for seed " + seed);
            return selected;
        }

        public static Report ReadRecordedReport(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) throw new InvalidOperationException("Recorded census input is missing: " + path);
            using (var file = File.OpenRead(path))
            {
                if (path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
                    using (var compressed = new GZipStream(file, CompressionMode.Decompress))
                    using (var reader = new StreamReader(compressed)) return ParseRecordedReport(reader.ReadToEnd());
                using (var reader = new StreamReader(file)) return ParseRecordedReport(reader.ReadToEnd());
            }
        }
        static Report ParseRecordedReport(string json)
        {
            // Native JsonUtility and the standalone JSON adapter throw different
            // exceptions for null/non-object/malformed input. Keep refusal stable
            // across both runtimes; no parse failure may trigger a fresh scan.
            try { return JsonUtility.FromJson<Report>(json); }
            catch (Exception error) { throw new InvalidOperationException("Invalid recorded census JSON.", error); }
        }
        static string FileHash(string path)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }
        sealed class CensusOpenObserver : Part
        {
            public Entity Target; public bool Observed;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "OpenContainer" && ReferenceEquals(e.GetParameter<Entity>("Container"), Target)) Observed = true;
                return true;
            }
        }

        static void MeasureZone(Report report, DensityLootTestScope scope, OverworldZoneManager manager,
            Entity player, int seed, int x, int y, int depth, BiomeType biome, string source, double performance)
        {
            scope.Seed(unchecked(seed * 31 + x * 1009 + y * 97 + depth));
            var zone = manager.GetZone($"Overworld.{x}.{y}.{depth}");
            Assert.NotNull(zone);
            var entities = zone.GetAllEntities().ToArray();
            var row = new SourceRow { seed = seed, source = source, zone = zone.ZoneID, biome = biome.ToString(),
                tier = depth == 0 ? WorldMapAuthoring.TierAt(x, y) : Math.Min(depth / 3 + 1, 8),
                currentSurfaceBiome = manager.WorldMap.GetBiome(x, y).ToString(),
                currentPoi = manager.WorldMap.GetPOI(x, y)?.Type.ToString() ?? "none",
                authoredColumn = OverworldZoneManager.AuthoredWildernessZoneIDs.Contains(WorldMap.ToZoneID(x, y, 0))
                    || WorldMap.ToZoneID(x, y, 0) == MultiCellPilotRuntime.ZoneID };
            row.currentSource = depth > 0 ? "Underground" : row.authoredColumn ? "AuthoredWilderness" : row.currentPoi == "none" ? "Wilderness" : row.currentPoi;
            row.classificationChanged = row.currentSource != row.source || (depth == 0 && row.currentSurfaceBiome != row.biome);
            foreach (var creature in entities.Where(e => e.HasTag("Creature")))
            {
                row.creatures++; row.species.Add(creature.BlueprintName);
                var inv = creature.GetPart<InventoryPart>();
                if (inv != null) { row.equipped += inv.EquippedItems.Values.Distinct().Count(); row.carried += inv.Objects.Count; }
            }
            row.species.Sort(StringComparer.Ordinal);
            foreach (var entity in entities)
            {
                var cp = entity.GetPart<ContainerPart>(); if (cp == null) continue;
                row.containers++;
                foreach (var item in cp.Contents) Add(row.items, item, performance);
                if (cp.IsLocked) { row.locked++; continue; }
                Cell approach = null;
                foreach (var bodyCell in zone.GetOccupiedCells(entity))
                {
                    if (approach != null) break;
                    for (int dx = -1; dx <= 1 && approach == null; dx++) for (int dy = -1; dy <= 1 && approach == null; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        var candidate = zone.GetCell(bodyCell.X + dx, bodyCell.Y + dy);
                        if (candidate != null && !candidate.BlocksMovement()
                            && !candidate.Occupants.Any(e => e == entity || e.HasTag("Trap") || e.HasTag("Creature") || e.HasPart<StairsUpPart>() || e.HasPart<StairsDownPart>())) approach = candidate;
                    }
                }
                if (approach == null) { row.noApproach++; continue; }
                var observer = new CensusOpenObserver { Target = entity };
                GameEvent action = null;
                Assert.IsTrue(zone.AddEntity(player, approach.X, approach.Y));
                try
                {
                    player.AddPart(observer);
                    // Observe delivery even when another actor-side handler consumes the event.
                    player.Parts.Remove(observer); player.Parts.Insert(0, observer);
                    action = GameEvent.New("InventoryAction"); action.SetParameter("Command", "OpenContainer");
                    action.SetParameter("Actor", (object)player); action.SetParameter("Zone", (object)zone);
                    row.openingAttempts++; entity.FireEvent(action);
                    if (observer.Observed) row.opened++; else row.refused++;
                }
                finally
                {
                    if (action != null) action.Release();
                    player.RemovePart(observer); zone.RemoveEntity(player);
                }
            }
            report.sources.Add(row);
            Diag.Record("loot", "Census", payload: new { seed, source, row.zone, row.tier, row.containers, row.openingAttempts, row.opened, row.refused, row.locked, row.noApproach,
                weapons = row.items.Where(i => i.category == "weapon").Sum(i => i.units),
                armor = row.items.Where(i => i.category == "armor").Sum(i => i.units),
                offense = row.items.Where(i => i.category == "offense").Sum(i => i.units) });
        }
        public static string Category(Entity item)
        {
            if (item.HasPart<MeleeWeaponPart>()) return "weapon";
            if (item.HasPart<ArmorPart>()) return "armor";
            if (item.HasPart<GasGrenadePart>()) return "offense";
            // StatusTonic also implements water and stoneskin. Count damaging
            // payloads, not the presence of the shared implementation part.
            string effect = item.GetPart<StatusTonicPart>()?.EffectName;
            if (new[] { "Poison", "Fire", "Acid", "Lightning", "Frost", "Bleeding", "Charred" }.Contains(effect)) return "offense";
            return item.HasPart<TonicPart>() ? "tonic" : "other";
        }
        static void Add(List<ItemRow> rows, Entity item, double performance)
        {
            var row = rows.FirstOrDefault(r => r.blueprint == item.BlueprintName);
            if (row == null) { row = new ItemRow { blueprint = item.BlueprintName, category = Category(item), tier = int.TryParse(item.GetTag("Tier", "1"), out int n) ? n : 1 }; rows.Add(row); }
            int units = item.GetPart<StackerPart>()?.StackCount ?? 1;
            // Trade APIs already price the entire stack. Sell rounding belongs
            // to that actual stack, not each hypothetical single-unit sale.
            row.units += units; row.commerce += TradeSystem.GetItemValue(item);
            row.sell += TradeSystem.GetSellPrice(item, performance); row.weight += (item.GetPart<PhysicsPart>()?.Weight ?? 0) * units;
        }
        static ExpectedRow Expected(string name, EntityFactory factory, double performance)
        {
            var table = LootTableRegistry.Get(name); Assert.NotNull(table);
            var row = new ExpectedRow { table = name };
            double totalWeight = table.Entries.Where(e => e.Weight > 0).Sum(e => e.Weight);
            foreach (var entry in table.Entries)
            {
                double probability = table.PickOne ? Math.Max(0, entry.Weight) / totalWeight * (table.MinPicks + table.MaxPicks) / 2.0 : entry.Chance / 100.0;
                double count = probability * (entry.MinCount + entry.MaxCount) / 2.0;
                if (!string.IsNullOrEmpty(entry.TableRef))
                { var child = Expected(entry.TableRef, factory, performance); row.items += child.items * count; row.commerce += child.commerce * count; row.sell += child.sell * count; }
                else { var item = factory.CreateEntity(entry.Blueprint); row.items += count; row.commerce += TradeSystem.GetItemValue(item) * count; row.sell += TradeSystem.GetSellPrice(item, performance) * count; }
            }
            return row;
        }
        static string Hash(string relative)
        { using (var sha = System.Security.Cryptography.SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(Path.Combine(Application.dataPath, "Resources/Content", relative)))).Replace("-", "").ToLowerInvariant(); }
    }
}
