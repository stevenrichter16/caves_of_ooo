using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// LOOT OVERHAUL SM6 — the common loot-placement algorithm.
    ///
    /// <para><b>The gap this closes.</b> Before this, containers came
    /// from exactly two places: landmark structure-stamps (hard-capped
    /// at 2 structures per zone, and most chest-bearing stamps gated to
    /// tier 2+) and one village chest. <c>PopulationBuilder</c> — the
    /// thing that fills every wilderness zone — placed <b>zero</b>
    /// containers, ever. A tier-1 desert zone could not contain a
    /// single lootable object <i>by construction</i>, because both of
    /// its chest stamps required tier 2. There was no budget, no
    /// density, and no tier scaling anywhere in the codebase.</para>
    ///
    /// <para><b>The algorithm.</b> Budget from zone kind + tier, kinds
    /// weighted by biome so containers read as <i>place</i> (urns in
    /// the desert, ore caches underground, reliquaries in ruins), then
    /// placement that prefers wall-adjacent and interior cells the way
    /// a real space is used — never on stairs, reserved cells, liquid,
    /// or on top of another container.</para>
    ///
    /// <para>Deterministic from the caller's seeded RNG: the same zone
    /// seed always produces the same containers.</para>
    /// </summary>
    public static class ContainerPlacementService
    {
        public enum ZoneKind { Wilderness, Underground, Lair, Village, Camp }

        /// <summary>Injected by GameBootstrap; null = graceful no-op.</summary>
        public static EntityFactory Factory;

        private struct ContainerKind
        {
            public string Blueprint;
            public string TablePrefix;
            public int Weight;
            public ContainerKind(string bp, string prefix, int weight)
            { Blueprint = bp; TablePrefix = prefix; Weight = weight; }
        }

        // Kind pools per biome. A container should tell you where you
        // are before you open it.
        private static readonly ContainerKind[] CavePool =
        {
            new ContainerKind("Crate", "CrateT", 3),
            new ContainerKind("Sack", "SackT", 3),
            new ContainerKind("OreCache", "OreCacheT", 4),
            new ContainerKind("WoodenBarrel", "CrateT", 2),
            new ContainerKind("StrongBox", "StrongBoxT", 1),
        };
        private static readonly ContainerKind[] DesertPool =
        {
            new ContainerKind("Urn", "UrnT", 4),
            new ContainerKind("Sack", "SackT", 3),
            new ContainerKind("Crate", "CrateT", 2),
            new ContainerKind("BoneCache", "BoneCacheT", 2),
            new ContainerKind("StrongBox", "StrongBoxT", 1),
        };
        private static readonly ContainerKind[] JunglePool =
        {
            new ContainerKind("WovenBasket", "BasketT", 4),
            new ContainerKind("HollowLog", "HollowLogT", 4),
            new ContainerKind("Urn", "UrnT", 2),
            new ContainerKind("Sack", "SackT", 2),
        };
        /// <summary>Docs/FELLING-W1-W2-PLAN.md SM5 — settled-country
        /// containers for wilderness cells in the Spread (village
        /// interiors already use SettlementPool before biome is even
        /// consulted). A woven basket or a hollow log reads as jungle;
        /// a crate or a sack in a hedgerow reads as a farm. Reuses the
        /// existing generic CrateT/SackT/StrongBoxT tables — zero new
        /// loot content.</summary>
        private static readonly ContainerKind[] SpreadPool =
        {
            new ContainerKind("Crate", "CrateT", 4),
            new ContainerKind("Sack", "SackT", 3),
            new ContainerKind("StrongBox", "StrongBoxT", 1),
        };
        private static readonly ContainerKind[] RuinsPool =
        {
            new ContainerKind("StrongBox", "StrongBoxT", 3),
            new ContainerKind("Bookshelf", "BookshelfT", 3),
            new ContainerKind("Crate", "CrateT", 2),
            new ContainerKind("Reliquary", "ReliquaryT", 2),
            new ContainerKind("BoneCache", "BoneCacheT", 2),
            new ContainerKind("WeaponRack", "WeaponRackT", 1),
        };
        private static readonly ContainerKind[] UndergroundPool =
        {
            new ContainerKind("OreCache", "OreCacheT", 4),
            new ContainerKind("Crate", "CrateT", 3),
            new ContainerKind("BoneCache", "BoneCacheT", 2),
            new ContainerKind("StrongBox", "StrongBoxT", 2),
        };
        private static readonly ContainerKind[] SettlementPool =
        {
            new ContainerKind("WoodenBarrel", "CrateT", 4),
            new ContainerKind("Crate", "CrateT", 3),
            new ContainerKind("Sack", "SackT", 3),
            new ContainerKind("AlchemyShelf", "AlchemyShelfT", 1),
            new ContainerKind("WeaponRack", "WeaponRackT", 1),
        };

        /// <summary>Mimic decoy rate — user call: 1-in-12, RUINS only,
        /// never in a settlement.</summary>
        private const int MimicOneIn = 12;

        /// <summary>
        /// Place and stock containers for one zone. Safe to call for
        /// any zone: a null factory or zone no-ops.
        /// </summary>
        public static int Populate(Zone zone, BiomeType biome, int tier,
            ZoneKind kind, System.Random rng)
        {
            if (zone == null || Factory == null || rng == null) return 0;

            int budget = ComputeBudget(kind, tier, rng);
            if (budget <= 0) return 0;

            var pool = PoolFor(biome, kind);
            if (pool.Length == 0) return 0;

            var candidates = CollectCandidateCells(zone);
            if (candidates.Count == 0) return 0;

            int placed = 0;
            var used = new List<Cell>(budget);
            int attempts = 0;
            int maxAttempts = budget * 12;

            while (placed < budget && attempts < maxAttempts)
            {
                attempts++;
                var cell = candidates[rng.Next(candidates.Count)];
                if (TooCloseToPlaced(cell, used)) continue;

                var kindPick = PickKind(pool, rng);

                // Ruins mimics: a chest that bites (user call, 1-in-12).
                string blueprint = kindPick.Blueprint;
                bool isMimic = biome == BiomeType.Ruins
                    && kind != ZoneKind.Village && kind != ZoneKind.Camp
                    && rng.Next(MimicOneIn) == 0;
                if (isMimic) blueprint = "MimicChest";

                Entity container;
                try { container = Factory.CreateEntity(blueprint); }
                catch (System.Exception) { continue; }
                if (container == null) continue;

                zone.AddEntity(container, cell.X, cell.Y);
                used.Add(cell);
                placed++;

                // A mimic is stocked from the table it's imitating —
                // the reward for killing it matches the bait.
                string table = kindPick.TablePrefix + ClampTableTier(tier);
                int stocked = LootStocker.StockContainer(container, table, Factory, rng);

                // Every entry in the low-tier tables is chance-gated, so
                // roughly one container in five rolled up EMPTY in the
                // live sweep. An empty chest is worse than no chest: it
                // spends the player's walk and their expectation. Floor
                // it with pocket change.
                if (stocked == 0) StockFallback(container, tier, rng);
            }

            if (placed > 0 && Diag.IsChannelEnabled("worldgen"))
            {
                Diag.Record(
                    category: "worldgen", kind: "ContainersPlaced",
                    payload: new
                    {
                        zone = zone.ZoneID,
                        biome = biome.ToString(),
                        zoneKind = kind.ToString(),
                        tier,
                        budget,
                        placed,
                    });
            }
            return placed;
        }

        /// <summary>
        /// Guarantee a container is worth opening. Called only when the
        /// table rolled nothing at all — every entry in the low-tier
        /// tables is chance-gated, so empties are otherwise common.
        /// </summary>
        private static void StockFallback(Entity container, int tier, System.Random rng)
        {
            var cp = container.GetPart<ContainerPart>();
            if (cp == null || Factory == null) return;
            int coins = 1 + rng.Next(2 + tier * 2);
            for (int i = 0; i < coins; i++)
            {
                Entity coin;
                try { coin = Factory.CreateEntity("GoldCoin"); }
                catch (System.Exception) { return; }
                if (coin == null || !cp.AddItem(coin)) return;
            }
        }

        /// <summary>
        /// count = base(zoneKind) + (tier-1) + jitter, clamped.
        /// Public: pinned by tests, and the one number to tune if the
        /// world feels too rich or too bare.
        /// </summary>
        public static int ComputeBudget(ZoneKind kind, int tier, System.Random rng)
        {
            int baseCount;
            switch (kind)
            {
                case ZoneKind.Underground: baseCount = 2; break;
                case ZoneKind.Lair:        baseCount = 2; break;
                case ZoneKind.Village:     baseCount = 1; break;
                case ZoneKind.Camp:        baseCount = 1; break;
                default:                   baseCount = 1; break;   // Wilderness
            }
            int tierBonus = tier - 1;
            if (tierBonus < 0) tierBonus = 0;
            if (tierBonus > 4) tierBonus = 4;          // deep zones cap out

            int jitter = rng != null ? rng.Next(0, 2) : 0;
            int total = baseCount + tierBonus + jitter;
            if (total < 1) total = 1;
            if (total > 6) total = 6;                  // no zone is a warehouse
            return total;
        }

        /// <summary>Tables are authored T1-T3; deep tiers reuse T3.</summary>
        public static int ClampTableTier(int tier)
        {
            if (tier < 1) return 1;
            if (tier > 3) return 3;
            return tier;
        }

        private static ContainerKind[] PoolFor(BiomeType biome, ZoneKind kind)
        {
            if (kind == ZoneKind.Village || kind == ZoneKind.Camp) return SettlementPool;
            if (kind == ZoneKind.Underground) return UndergroundPool;
            switch (biome)
            {
                case BiomeType.Desert: return DesertPool;
                case BiomeType.Jungle: return JunglePool;
                case BiomeType.Ruins:  return RuinsPool;
                case BiomeType.Spread: return SpreadPool;
                case BiomeType.Sodden: return JunglePool;
                case BiomeType.Beating: return DesertPool;
                case BiomeType.Grovelands: return JunglePool;
                case BiomeType.Overwrit: return RuinsPool;
                case BiomeType.Stump: return CavePool;
                default:               return CavePool;
            }
        }

        private static ContainerKind PickKind(ContainerKind[] pool, System.Random rng)
        {
            int total = 0;
            for (int i = 0; i < pool.Length; i++) total += pool[i].Weight;
            if (total <= 0) return pool[0];
            int roll = rng.Next(total);
            for (int i = 0; i < pool.Length; i++)
            {
                roll -= pool[i].Weight;
                if (roll < 0) return pool[i];
            }
            return pool[pool.Length - 1];
        }

        /// <summary>
        /// One pass over the zone building the legal-cell list (rather
        /// than re-scanning per container). Wall-adjacent and interior
        /// cells are entered twice, which biases placement toward them
        /// without a second sort — containers end up against walls and
        /// inside rooms, the way a real space is used.
        /// </summary>
        private static List<Cell> CollectCandidateCells(Zone zone)
        {
            var list = new List<Cell>(256);
            for (int x = 1; x < Zone.Width - 1; x++)
            {
                for (int y = 1; y < Zone.Height - 1; y++)
                {
                    var cell = zone.GetCell(x, y);
                    if (cell == null) continue;
                    if (zone.GenReservedCells != null
                        && zone.GenReservedCells.Contains((x, y))) continue;
                    if (!IsClearFloor(cell)) continue;

                    list.Add(cell);
                    if (IsWallAdjacent(zone, x, y) || cell.IsInterior)
                        list.Add(cell);      // weight, not a second cell
                }
            }
            return list;
        }

        private static bool IsClearFloor(Cell cell)
        {
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                var o = cell.Objects[i];
                // Never stack on a solid, a stairway, another container,
                // or a liquid pool.
                if (o.HasTag("Solid")) return false;
                if (o.GetPart<ContainerPart>() != null) return false;
                if (o.GetPart<LiquidPoolPart>() != null) return false;
                var bp = o.BlueprintName;
                if (bp == "StairsDown" || bp == "StairsUp") return false;
                var phys = o.GetPart<PhysicsPart>();
                if (phys != null && phys.Solid) return false;
            }
            return true;
        }

        private static bool IsWallAdjacent(Zone zone, int x, int y)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    var n = zone.GetCell(x + dx, y + dy);
                    if (n == null) continue;
                    for (int i = 0; i < n.Objects.Count; i++)
                        if (n.Objects[i].HasTag("Solid")) return true;
                }
            }
            return false;
        }

        /// <summary>Minimum Chebyshev spacing 3, so a zone never reads
        /// as a pile of loot in one corner.</summary>
        private static bool TooCloseToPlaced(Cell cell, List<Cell> used)
        {
            for (int i = 0; i < used.Count; i++)
            {
                int dx = cell.X - used[i].X; if (dx < 0) dx = -dx;
                int dy = cell.Y - used[i].Y; if (dy < 0) dy = -dy;
                int cheb = dx > dy ? dx : dy;
                if (cheb < 3) return true;
            }
            return false;
        }
    }
}
