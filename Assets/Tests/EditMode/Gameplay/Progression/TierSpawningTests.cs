using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// P4 (Docs/PLACE-WORK-AND-PROGRESSION.md §3 rules 3-4): spawns follow the zone's
    /// tier. Every zone has one tier (cell tier on the surface, one deeper below); a
    /// population roll in a tier-T zone never places a hostile creature below tier T-1;
    /// canon's ecology indicator species are neutral Wildlife, not threats; and every
    /// table a zone can use still offers threats of its own tier.
    /// </summary>
    public sealed class TierSpawningTests
    {
        static EntityFactory factory;
        static Entity player;
        static readonly string[] WildlifeSpecies =
            { "CascadeFather", "GlasspaneFrog", "SummitSinger", "YellowfootWayfarer", "Wardline", "GroveLanternMoth", "GinFrog", "BrocchiniaSentinel" };

        [OneTimeSetUp] public void Load()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            FactionManager.Initialize(Resources.Load<TextAsset>("Content/Data/Factions").text);
            player = factory.CreateEntity("Player");
        }

        // ════════════════ the zone's tier ════════════════

        [Test] public void ASurfaceZoneIsItsCellsTier()
        {
            Assert.AreEqual(1, ZoneTierPolicy.TierFor("Overworld.10.10.0"), "Sill");
            Assert.AreEqual(WorldMapAuthoring.TierAt(3, 3), ZoneTierPolicy.TierFor("Overworld.3.3.0"), "the Root");
            Assert.AreEqual(5, ZoneTierPolicy.TierFor("Overworld.3.3.0"));
        }

        [Test] public void BelowGroundIsOneTierDeeperAndCapped()
        {
            Assert.AreEqual(2, ZoneTierPolicy.TierFor("Overworld.10.10.1"));
            Assert.AreEqual(2, ZoneTierPolicy.TierFor("Overworld.10.10.3"), "floors 1-3 share a tier");
            Assert.AreEqual(3, ZoneTierPolicy.TierFor("Overworld.10.10.4"), "and every three floors deeper adds one");
            Assert.AreEqual(ProgressionCurve.MaxTier, ZoneTierPolicy.TierFor("Overworld.3.3.2"), "never above the top tier");
        }

        [Test] public void NonWorldZonesHaveNoTier()
        {
            Assert.AreEqual(0, ZoneTierPolicy.TierFor("ProgressionBench.Duel"));
            Assert.AreEqual(0, ZoneTierPolicy.TierFor(null));
        }

        // ════════════════ wildlife ════════════════

        [Test] public void IndicatorSpeciesAreNeutralWildlife()
        {
            foreach (var name in WildlifeSpecies)
            {
                var e = factory.CreateEntity(name);
                Assert.AreEqual("Wildlife", FactionManager.GetFaction(e), name);
                Assert.False(FactionManager.IsHostile(e, player), name + " does not hunt the player");
            }
            Assert.True(FactionManager.IsHostile(factory.CreateEntity("Viper"), player), "counter-check: beasts still do");
        }

        // ════════════════ the filter ════════════════

        static PopulationTable Mixed() => new PopulationTable
        {
            Name = "TierFilterProbe",
            Entries = new List<PopulationEntry>
            {
                new PopulationEntry { BlueprintName = "Viper", Weight = 5, MinCount = 3, MaxCount = 3 },          // tier 1 hostile
                new PopulationEntry { BlueprintName = "MawToad", Weight = 5, MinCount = 2, MaxCount = 2 },        // tier 2 hostile
                new PopulationEntry { BlueprintName = "Mosshulk", Weight = 5, MinCount = 1, MaxCount = 1 },       // tier 3 hostile
                new PopulationEntry { BlueprintName = "CascadeFather", Weight = 5, MinCount = 2, MaxCount = 2 }, // wildlife
                new PopulationEntry { BlueprintName = "Stalagmite", Weight = 5, MinCount = 2, MaxCount = 2 },    // not a creature
            }
        };

        static Dictionary<string, int> Spawn(string zoneId)
        {
            var zone = new Zone(zoneId);
            new PopulationBuilder(Mixed()).BuildZone(zone, factory, new System.Random(7));
            var counts = new Dictionary<string, int>();
            foreach (var e in zone.GetAllEntities()) counts[e.BlueprintName] = counts.TryGetValue(e.BlueprintName, out var n) ? n + 1 : 1;
            return counts;
        }

        [Test] public void ATierFourZoneDropsTrivialThreatsButKeepsWildlifeAndTerrain()
        {
            var counts = Spawn("Overworld.3.4.0");
            Assume.That(ZoneTierPolicy.TierFor("Overworld.3.4.0"), Is.EqualTo(4), "precondition: a tier-4 cell");
            Assert.False(counts.ContainsKey("Viper"), "tier 1 is below the floor");
            Assert.False(counts.ContainsKey("MawToad"), "tier 2 is below the floor");
            Assert.True(counts.ContainsKey("Mosshulk"), "tier 3 meets it");
            Assert.True(counts.ContainsKey("CascadeFather"), "wildlife is not a threat");
            Assert.True(counts.ContainsKey("Stalagmite"), "terrain is untouched");
        }

        [Test] public void ATierTwoZoneKeepsEverything()
        {
            var counts = Spawn("Overworld.16.4.0");
            Assume.That(ZoneTierPolicy.TierFor("Overworld.16.4.0"), Is.EqualTo(2), "precondition: a tier-2 cell");
            Assert.True(counts.ContainsKey("Viper") && counts.ContainsKey("MawToad") && counts.ContainsKey("Mosshulk"), "counter-check: tier 1 is within T-1");
        }

        [Test] public void ATestZoneOutsideTheWorldIsNotFiltered()
        {
            var counts = Spawn("ProgressionBench.Duel");
            Assert.True(counts.ContainsKey("Viper"), "no tier, no filter");
        }

        // ════════════════ every tier still has threats ════════════════

        static int ThreatsInBand(PopulationTable table, int tier)
        {
            var seen = new HashSet<string>();
            foreach (var e in table.Entries)
            {
                if (e == null || !factory.Blueprints.ContainsKey(e.BlueprintName) || !seen.Add(e.BlueprintName)) continue;
                var c = factory.CreateEntity(e.BlueprintName);
                if (c == null || !c.HasTag("Creature") || !FactionManager.IsHostile(c, player)) continue;
                int t = ProgressionCurve.CreatureTier(c);
                if (t >= tier - 1 && t <= tier + 1) seen.Add("ok:" + e.BlueprintName);
            }
            return seen.Count(s => s.StartsWith("ok:"));
        }

        [Test] public void EverySurfaceBiomeOffersThreatsAtEachOfItsTiers()
        {
            var missing = new List<string>();
            // The Overwrit is left out on purpose: its sparse Unsaying circulation is authored,
            // and its threats are designed with that region (dead-regions work), not here.
            var biomes = new[] { BiomeType.Spread, BiomeType.Sodden, BiomeType.Beating, BiomeType.Grovelands };
            foreach (var biome in biomes)
                foreach (int tier in TiersUsedBy(biome))
                    if (ThreatsInBand(PopulationTable.GetBiomeTable(biome, tier), tier) < 2) missing.Add(biome + " t" + tier);
            foreach (StumpBand band in System.Enum.GetValues(typeof(StumpBand)))
                foreach (int tier in TiersUsedBy(BiomeType.Stump))
                    if (ThreatsInBand(PopulationTable.GetStumpTable(band, tier), tier) < 2) missing.Add("Stump " + band + " t" + tier);
            Assert.IsEmpty(missing, "tables without at least two threats of their tier: " + string.Join(", ", missing));
        }

        [Test] public void UndergroundOffersThreatsAtEachZoneTier()
        {
            var missing = new List<string>();
            for (int tier = 2; tier <= ProgressionCurve.MaxTier; tier++)
                for (int depth = 1; depth <= 3; depth++)
                    if (ThreatsInBand(PopulationTable.UndergroundTier(depth, false, tier), tier) < 2) missing.Add("depth " + depth + " t" + tier);
            Assert.IsEmpty(missing, string.Join(", ", missing));
        }

        static IEnumerable<int> TiersUsedBy(BiomeType biome)
        {
            var tiers = new SortedSet<int>();
            for (int x = 0; x < WorldMap.Width; x++)
                for (int y = 0; y < WorldMap.Height; y++)
                    if (WorldMapAuthoring.BiomeAt(x, y) == biome) tiers.Add(WorldMapAuthoring.TierAt(x, y));
            return tiers;
        }
    }
}
