using System.Collections.Generic;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// G.3+G.4 — the per-turn gas dispersal + merge engine. Direct port
    /// of Qud's <c>Gas.ProcessGasBehavior</c> (qud Gas.cs:204-319) reduced
    /// to the foundation contract (no wind, no phase, no plasma-style
    /// coat semantics — those land in G.5+, G.8, G.10).
    ///
    /// <para><b>Why a static class.</b> Mirrors <see cref="LiquidRegistry"/>
    /// and <see cref="MaterialReactionResolver"/>'s shape — a stateless
    /// engine that operates on entity state. No instance fields means no
    /// save-load surface; the test surface is a single
    /// <see cref="SetRngForTests"/> seam. The <see cref="GasSystemPart"/>
    /// on the world entity is the only consumer; it listens to TickEnd
    /// and calls <see cref="OnTickEnd"/> here.</para>
    ///
    /// <para><b>Iteration safety.</b> Dispersal mutates the zone — it
    /// spawns new gas entities, removes dispersed-to-zero ones — so the
    /// per-tick pass must operate on a SNAPSHOT of the gas list, not the
    /// live tag index. Otherwise iteration would be corrupted. Mirrors
    /// the same snapshot pattern Qud's <c>XRLCore</c> uses for
    /// <c>WantTurnTick</c> dispatch.</para>
    ///
    /// <para><b>Constants ported from Qud.</b> Each one cites the Qud
    /// line it mirrors so a future contributor can tune by reference.</para>
    /// </summary>
    public static class GasSystem
    {
        /// <summary>Base % chance per tick to attempt spread (Qud Gas.cs:226 "25 + windSpeed").
        /// G.10 will plumb wind here.</summary>
        public const int BASE_SPREAD_CHANCE = 25;

        /// <summary>Below this density, dispersal switches to "low-density
        /// flicker" — <see cref="LOW_DENSITY_DISSIPATE_CHANCE"/>% chance
        /// per tick to vanish (Qud Gas.cs:313).</summary>
        public const int LOW_DENSITY_THRESHOLD = 10;

        /// <summary>Per-tick dissipation roll for low-density gas (Qud Gas.cs:313 "50 + windSpeed").</summary>
        public const int LOW_DENSITY_DISSIPATE_CHANCE = 50;

        /// <summary>Cap on the per-step density chunk that moves into a
        /// destination cell (Qud Gas.cs:273 "Math.Min(Density, 30)").</summary>
        public const int MAX_SPREAD_CHUNK = 30;

        /// <summary>Per-tick decay range for unstable gas (Qud Gas.cs:344
        /// "Stat.Random(Factor, Factor * 3)" with Factor=1).</summary>
        public const int MIN_DISPERSAL_RATE = 1;
        public const int MAX_DISPERSAL_RATE = 3;

        // 8-direction lookup for spread step. Order doesn't matter
        // (each pick is uniform random over 0..7).
        private static readonly int[] DX = { 0, 1, 1, 1, 0, -1, -1, -1 };
        private static readonly int[] DY = { -1, -1, 0, 1, 1, 1, 0, -1 };

        private static System.Random _rng = new System.Random();

        /// <summary>TEST-ONLY: replace the RNG with a deterministically
        /// seeded one. Tests that want repeatable dispersal/merge outcomes
        /// call this in [SetUp].</summary>
        public static void SetRngForTests(System.Random rng) => _rng = rng ?? new System.Random();

        /// <summary>One TickEnd pass: iterate every gas pool in the zone
        /// and run dispersal on it. Snapshot-then-iterate so mid-pass
        /// spawn/remove don't corrupt iteration.</summary>
        public static void OnTickEnd(Zone zone)
        {
            if (zone == null) return;
            // Allocates one List per tick — acceptable: O(N_gas) is
            // bounded and per-turn cost (not per-frame). Same convention
            // as Zone.GetAllEntities documents.
            var snapshot = zone.GetEntitiesWithTag("Gas");
            for (int i = 0; i < snapshot.Count; i++)
            {
                ProcessGasBehavior(snapshot[i], zone);
            }
        }

        /// <summary>
        /// Port of Qud <c>Gas.ProcessGasBehavior</c> (Gas.cs:204-319) sans
        /// wind. Decays density for unstable gas, rolls a per-tick spread
        /// attempt, and dissipates on low/zero density.
        /// </summary>
        public static void ProcessGasBehavior(Entity gas, Zone zone)
        {
            if (gas == null || zone == null) return;
            var pool = gas.GetPart<GasPoolPart>();
            if (pool == null) return;
            var pos = zone.GetEntityPosition(gas);
            if (pos.x < 0) return; // already removed by a chained dispersal

            // Decay (unstable only) — Qud Gas.cs:222-224.
            if (!pool.Stable)
            {
                int rate = GetDispersalRate(pool);
                int before = pool.Density;
                pool.Density = before - rate; // setter clamps + fires GasDensityChange
                Diag.Record("gas", "Dispersed", pool.Creator, gas,
                    new { gasId = pool.GasId, before, after = pool.Density, rate });
            }

            // Spread roll — Qud Gas.cs:226.
            if (pool.Density > LOW_DENSITY_THRESHOLD && _rng.Next(100) < BASE_SPREAD_CHANCE)
            {
                int attempts = _rng.Next(1, 5); // 1..4 inclusive (Qud Gas.cs:229)
                for (int i = 0; i < attempts; i++)
                {
                    if (pool.Density <= 0) break;
                    TrySpreadOnce(gas, pool, pos.x, pos.y, zone);
                }
            }

            // Dissipation — Qud Gas.cs:313.
            if (pool.Density <= 0 ||
                (pool.Density <= LOW_DENSITY_THRESHOLD && _rng.Next(100) < LOW_DENSITY_DISSIPATE_CHANCE))
            {
                Dissipate(gas, pool, zone, pool.Density <= 0 ? "ZeroDensity" : "LowDensityFlicker");
            }
        }

        /// <summary>One spread attempt — pick a direction, check
        /// passability, then either merge into an existing compatible gas
        /// (G.4) or spawn a new gas at the destination.</summary>
        private static void TrySpreadOnce(Entity gas, GasPoolPart pool, int x, int y, Zone zone)
        {
            int dir = _rng.Next(8);
            int nx = x + DX[dir], ny = y + DY[dir];
            if (!zone.InBounds(nx, ny)) return;
            var destCell = zone.GetCell(nx, ny);
            if (destCell == null) return;
            // Non-seeping gas is blocked by solid cells. Qud Gas.cs:239.
            if (!pool.Seeping && destCell.IsSolid()) return;

            // Chunk size for this step — Qud Gas.cs:273.
            int maxChunk = pool.Density < MAX_SPREAD_CHUNK ? pool.Density : MAX_SPREAD_CHUNK;
            if (maxChunk <= 0) return;
            int chunk = _rng.Next(1, maxChunk + 1);

            // G.4 merge check: any compatible gas in destCell?
            var existing = FindCompatibleGas(destCell, pool, gas);
            if (existing != null)
            {
                int before = existing.Density;
                MergeChunk(pool, existing, chunk);
                // Diag payload includes both donor and receiver types +
                // colors so the gate (IsMergeCompatible) can be verified
                // at runtime without grepping. A buggy compare that let
                // mismatched types through would show donorType !=
                // receiverType in the record (impossible if the gate is
                // working — used by GasSystemTests assertions).
                Diag.Record("gas", "Merged", pool.Creator, gas,
                    new { gasId = pool.GasId, fromX = x, fromY = y,
                          toX = nx, toY = ny, chunk,
                          donorAfter = pool.Density,
                          receiverBefore = before, receiverAfter = existing.Density,
                          donorType = pool.GasType, receiverType = existing.GasType,
                          donorColor = pool.ColorString, receiverColor = existing.ColorString });
                return;
            }

            // No compatible neighbor: spawn a new gas via factory.
            var spawned = GasFactory.SpawnGas(zone, nx, ny, pool.GasId,
                density: chunk, level: pool.Level, creator: pool.Creator);
            if (spawned == null) return;

            // Inherit dynamic flags (Seeping/Stable/ColorString carry the
            // source's runtime mutations — e.g. a GasTumbler-influenced
            // cloud spreads its seeping property). Qud Gas.cs:296-298.
            var newPool = spawned.GetPart<GasPoolPart>();
            if (newPool != null)
            {
                newPool.Seeping = pool.Seeping;
                newPool.Stable = pool.Stable;
                newPool.ColorString = pool.ColorString;
                newPool.GasType = pool.GasType; // already set by factory from def, but re-pin from source
            }
            pool.Density -= chunk;
            Diag.Record("gas", "Spread", pool.Creator, gas,
                new { gasId = pool.GasId, fromX = x, fromY = y,
                      toX = nx, toY = ny, chunk, donorAfter = pool.Density });
        }

        /// <summary>Decay rate per tick. Listens to
        /// <c>CreatorModifyGasDispersal</c> on the gas's Creator entity
        /// so equipment (GasTumbler in G.7) can amplify/dampen the rate.
        /// Qud Gas.cs:342-352.</summary>
        public static int GetDispersalRate(GasPoolPart pool)
        {
            int rate = _rng.Next(MIN_DISPERSAL_RATE, MAX_DISPERSAL_RATE + 1);
            if (pool.Creator == null) return rate;
            var e = GameEvent.New("CreatorModifyGasDispersal");
            e.SetParameter("Rate", (object)rate);
            e.SetParameter("Gas", (object)pool);
            pool.Creator.FireEvent(e);
            int modified = e.GetParameter<int>("Rate");
            e.Release();
            return modified;
        }

        /// <summary>G.4 — two gases merge iff same GasType + same
        /// ColorString. Mirrors Qud Gas.IsGasMergeable (Gas.cs:354-361).
        /// Color is part of identity so reskinned variants of the same
        /// GasType stay distinct.</summary>
        public static bool IsMergeCompatible(GasPoolPart a, GasPoolPart b)
        {
            if (a == null || b == null) return false;
            if (a.GasType != b.GasType) return false;
            return a.ColorString == b.ColorString;
        }

        /// <summary>G.4 — Move <paramref name="chunk"/> density from src
        /// to dst, capped at the donor's available density. Receiver
        /// takes the max Level, ORs Seeping, inherits Creator if it
        /// didn't already have one. Direct port of Qud
        /// <c>Gas.MergeToGas</c> (Gas.cs:380-400).</summary>
        public static void MergeChunk(GasPoolPart src, GasPoolPart dst, int chunk)
        {
            if (src == null || dst == null) return;
            if (chunk > src.Density) chunk = src.Density;
            if (chunk <= 0) return;
            dst.Density += chunk;
            if (src.Level > dst.Level) dst.Level = src.Level;
            if (src.Seeping && !dst.Seeping) dst.Seeping = true;
            if (src.Creator != null && dst.Creator == null) dst.Creator = src.Creator;
            src.Density -= chunk;
        }

        /// <summary>Remove a gas from the zone — Qud's
        /// <c>Gas.Dissipate</c> (Gas.cs:321-331). Plain
        /// non-creature/non-player path: just obliterate.</summary>
        public static void Dissipate(Entity gas, GasPoolPart pool, Zone zone, string cause)
        {
            if (gas == null || zone == null) return;
            Diag.Record("gas", "Dissipated", pool?.Creator, gas,
                new { gasId = pool?.GasId, density = pool?.Density ?? 0, cause });
            zone.RemoveEntity(gas);
        }

        // ──────────── Helpers ────────────

        /// <summary>Find a compatible (same GasType + Color) gas in a cell,
        /// skipping the source gas itself.</summary>
        private static GasPoolPart FindCompatibleGas(Cell cell, GasPoolPart src, Entity srcEntity)
        {
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                var other = cell.Objects[i];
                if (other == srcEntity) continue;
                var otherPool = other.GetPart<GasPoolPart>();
                if (otherPool == null) continue;
                if (IsMergeCompatible(src, otherPool)) return otherPool;
            }
            return null;
        }
    }
}
