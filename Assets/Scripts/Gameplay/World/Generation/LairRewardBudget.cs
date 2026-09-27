using System;
using System.Linq;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Redistribute the old surface's stocks across a bounded stack.
    /// Each earlier floor gets one ordinary cache; the remaining budget belongs
    /// to one final cache. All keep their authored table family, and only the
    /// final instance's capacity grows. Counts are bounded, independent of prices and documented
    /// against the twelve biome/tier cohorts in DENSITY-UNDERGROUND-LAIRS.</summary>
    public static class LairRewardBudget
    {
        public const int FinalCacheCapacity = 32;
        private const int LegacyCacheBudgetTier = 2;
        private const int SoddenLooseFindChance = 75;

        private readonly struct Budget
        {
            public readonly BiomeType Biome;
            public readonly int Tier1, Tier2, Tier3;
            public Budget(BiomeType biome, int tier1, int tier2, int tier3)
            { Biome = biome; Tier1 = tier1; Tier2 = tier2; Tier3 = tier3; }
            public int Rolls(int tier) => tier == 1 ? Tier1 : tier == 2 ? Tier2 : Tier3;
        }
        // Higher-grade tables are richer, so they require fewer rolls to retain
        // the old aggregate value. Sodden also retains one possible loose find;
        // the Choir budget excludes the legacy manufactured ground scatter.
        private static readonly Budget[] Budgets =
        {
            new Budget(BiomeType.Spread, 6, 3, 2),
            new Budget(BiomeType.Sodden, 6, 3, 1),
            new Budget(BiomeType.Beating, 8, 4, 2),
            new Budget(BiomeType.Grovelands, 4, 4, 3)
        };

        /// <summary>Create stock once during staged final-floor generation.
        /// Unknown biomes/missing context refuse without consuming a roll.</summary>
        public static Entity Create(BiomeType biome, int tier, EntityFactory factory, Random rng, int earlierCaches = 0)
        {
            if (earlierCaches < 0 || earlierCaches > 2 || factory == null || rng == null || !LairStacks.AllowedBiome(biome))
            {
                Record("LairRewardRejected", new { biome, tier, reason = "unsupported-context" });
                return null;
            }
            tier = ContainerPlacementService.ClampTableTier(tier);
            var budget = Budgets.Single(b => b.Biome == biome);
            var pool = ContainerPlacementService.PoolFor(biome, ContainerPlacementService.ZoneKind.Lair);
            int draws = ContainerPlacementService.ComputeBudget(ContainerPlacementService.ZoneKind.Lair, LegacyCacheBudgetTier, rng);
            var kind = Pick(pool, rng);
            for (int i = 1; i < draws; i++)
            {
                var candidate = Pick(pool, rng);
                if (Rank(candidate) > Rank(kind)) kind = candidate;
            }
            var reward = factory.CreateEntity(kind.Blueprint);
            var contents = reward?.GetPart<ContainerPart>();
            if (contents == null || !Fresh(reward))
            {
                Record("LairRewardRejected", new { biome, tier, reason = "foreign-or-missing-container", kind.Blueprint });
                return null;
            }
            contents.MaxItems = FinalCacheCapacity;
            string table = kind.TablePrefix + tier;
            int rolls = Math.Max(1, budget.Rolls(tier) - earlierCaches);
            for (int i = 0; i < rolls; i++)
                if (!Stock(reward, table, tier, factory, rng)) return null;
            Record("LairRewardStocked", new { biome, tier, kind.Blueprint, table, draws, rolls, earlierCaches, items = contents.Contents.Count });
            return contents.Contents.Count > 0 ? reward : null;
        }

        /// <summary>One ordinary cache makes an earlier floor worth exploring.
        /// Approach stock progresses from T1 on the surface to T2 in the middle,
        /// never above the lair's own tier. Its authored capacity stays intact.</summary>
        public static Entity CreateApproach(BiomeType biome, int tier, int depth, EntityFactory factory, Random rng)
        {
            if (factory == null || rng == null || depth < 0 || depth > 1 || !LairStacks.AllowedBiome(biome))
            {
                Record("LairRewardRejected", new { biome, tier, depth, reason = "unsupported-approach-context" });
                return null;
            }
            int tableTier = Math.Min(ContainerPlacementService.ClampTableTier(tier), depth + 1);
            var kind = Pick(ContainerPlacementService.PoolFor(biome, ContainerPlacementService.ZoneKind.Lair), rng);
            var owner = factory.CreateEntity(kind.Blueprint);
            if (owner?.GetPart<ContainerPart>() == null || !Fresh(owner))
            {
                Record("LairRewardRejected", new { biome, tier, depth, reason = "foreign-or-missing-approach-owner" });
                return null;
            }
            if (!Stock(owner, kind.TablePrefix + tableTier, tableTier, factory, rng)) return null;
            Record("LairApproachStocked", new { biome, tier, depth, tableTier, kind.Blueprint, items = owner.GetPart<ContainerPart>().Contents.Count });
            return owner;
        }

        private static bool Fresh(Entity owner)
        {
            var physical = owner?.GetPart<PhysicsPart>();
            return owner != null && owner.SpatialZone == null && physical?.InInventory == null && physical?.Equipped == null;
        }
        private static bool Stock(Entity owner, string table, int tier, EntityFactory factory, Random rng)
        {
            var contents = owner.GetPart<ContainerPart>();
            if (!OwnsFreshCache(owner, contents)) return false;
            int added = 0;
            foreach (string blueprint in LootTableRegistry.Roll(table, rng))
            {
                if (!factory.Blueprints.ContainsKey(blueprint)) continue;
                var item = factory.CreateEntity(blueprint);
                if (!OwnsFreshCache(owner, contents)) return false;
                if (item == null) continue;
                if (!Fresh(item))
                {
                    Record("LairRewardRejected", new { table, blueprint, reason = "foreign-stock-owner" });
                    return false;
                }
                if (contents.AddItem(item)) added++;
            }
            if (added > 0) return true;
            // Retain the ordinary table's bounded empty-roll pocket change.
            int coins = 1 + rng.Next(2 + tier * 2);
            for (int coin = 0; coin < coins; coin++)
            {
                var item = factory.CreateEntity("GoldCoin");
                if (!OwnsFreshCache(owner, contents)) return false;
                if (item == null || !Fresh(item))
                {
                    Record("LairRewardRejected", new { table, reason = "foreign-or-missing-fallback" });
                    return false;
                }
                if (!contents.AddItem(item)) break;
            }
            return contents.Contents.Count > 0;
        }

        // Factory ObjectCreated handlers may transfer the cache or replace its
        // part. Revalidate after each callback before mutating captured contents.
        private static bool OwnsFreshCache(Entity owner, ContainerPart contents)
        {
            if (contents != null && Fresh(owner) && ReferenceEquals(owner.GetPart<ContainerPart>(), contents))
                return true;
            Record("LairRewardRejected", new { reason = "cache-owner-changed-during-stock" });
            return false;
        }

        /// <summary>A single optional tier-appropriate equipment find replaces
        /// the old loose manufactured scatter only in non-Choir Sodden. It stays
        /// outside natural caches. Other biomes consume no RNG here.</summary>
        public static Entity CreateLooseFind(BiomeType biome, int tier, EntityFactory factory, Random rng)
        {
            if (biome != BiomeType.Sodden || factory == null || rng == null) return null;
            int roll = rng.Next(100);
            if (roll >= SoddenLooseFindChance)
            {
                Record("LairLooseRewardRoll", new { biome, tier, roll, selected = false });
                return null;
            }
            tier = ContainerPlacementService.ClampTableTier(tier);
            string table = (rng.Next(2) == 0 ? "FindWeaponT" : "FindArmorT") + tier;
            var result = LootTableRegistry.Roll(table, rng);
            var item = result.Count == 0 ? null : factory.CreateEntity(result[0]);
            if (item != null && !Fresh(item)) item = null;
            Record("LairLooseRewardRoll", new { biome, tier, roll, selected = item != null, table, blueprint = item?.BlueprintName });
            return item;
        }

        private static int Rank(ContainerPlacementService.ContainerKind kind)
        {
            switch (kind.Blueprint)
            {
                case "StrongBox": return 3;
                case "Crate": case "Urn": case "BoneCache": return 2;
                default: return 1;
            }
        }
        private static ContainerPlacementService.ContainerKind Pick(ContainerPlacementService.ContainerKind[] pool, Random rng)
        {
            int roll = rng.Next(pool.Sum(kind => kind.Weight));
            foreach (var kind in pool)
            {
                roll -= kind.Weight;
                if (roll < 0) return kind;
            }
            throw new InvalidOperationException("Lair reward kind pool has no positive weight.");
        }
        private static void Record(string kind, object payload)
        {
            if (Diag.IsChannelEnabled("worldgen")) Diag.Record("worldgen", kind, payload: payload);
        }
    }
}
