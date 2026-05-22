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
        /// and run dispersal + per-turn-apply on it. Snapshot-then-iterate
        /// so mid-pass spawn/remove don't corrupt iteration.</summary>
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
                // G.5: after dispersal, dispatch the per-turn apply pass
                // to any IObjectGasBehaviorPart sibling. Skipped when the
                // gas dissipated mid-dispersal (the in-zone check inside
                // DispatchPerTurnApply protects).
                DispatchPerTurnApply(snapshot[i], zone);
            }
        }

        /// <summary>G.5 — per-turn dose for creatures currently in the
        /// gas's cell. Skipped if the gas dissipated during its own
        /// dispersal this tick. Mirror of Qud's
        /// <c>IObjectGasBehavior.TurnTick → ApplyGas(Cell)</c>
        /// (IObjectGasBehavior.cs:43-51).</summary>
        private static void DispatchPerTurnApply(Entity gas, Zone zone)
        {
            if (gas == null || zone == null) return;
            var pos = zone.GetEntityPosition(gas);
            if (pos.x < 0) return; // dissipated during this tick's ProcessGasBehavior
            var behavior = gas.GetPart<IObjectGasBehaviorPart>();
            if (behavior == null) return; // visual-only gas (no behavior Part)
            var cell = zone.GetCell(pos.x, pos.y);
            if (cell == null) return;
            behavior.ApplyToCell(cell, zone);
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

            // G.10 — read prevailing wind. Clamp ≥0 so a stray negative
            // value behaves like no wind (every term is additive, so
            // windSpeed=0 reproduces the pre-G.10 dispersal exactly).
            int windSpeed = zone.CurrentWindSpeed;
            if (windSpeed < 0) windSpeed = 0;
            int windDirIndex = WindDirectionToIndex(zone.CurrentWindDirection);

            // Decay (unstable only) — Qud Gas.cs:222-224.
            if (!pool.Stable)
            {
                int rate = GetDispersalRate(pool);
                int before = pool.Density;
                pool.Density = before - rate; // setter clamps + fires GasDensityChange
                Diag.Record("gas", "Dispersed", pool.Creator, gas,
                    new { gasId = pool.GasId, before, after = pool.Density, rate });
            }

            // Spread roll — Qud Gas.cs:226 ("25 + windSpeed").
            if (pool.Density > LOW_DENSITY_THRESHOLD &&
                _rng.Next(100) < BASE_SPREAD_CHANCE + windSpeed)
            {
                int attempts = ComputeSpreadAttempts(windSpeed, _rng); // Qud Gas.cs:229
                for (int i = 0; i < attempts; i++)
                {
                    if (pool.Density <= 0) break;
                    TrySpreadOnce(gas, pool, pos.x, pos.y, zone, windSpeed, windDirIndex);
                }
            }

            // Sync the cloud glyph to its (decayed/spread) density + mark
            // the cell dirty so the renderer repaints it. Done before the
            // dissipation check while the gas is still placed; if it then
            // dissipates, Dissipate marks the cell dirty again (cell → floor).
            GasVisuals.Refresh(gas, pool, zone);

            // Dissipation — Qud Gas.cs:313 ("50 + windSpeed" for thin gas).
            // Stable gas is EXEMPT from the low-density flicker-out: per the
            // GasPoolPart.Stable contract ("persist indefinitely"), a stable
            // cloud must not vanish just because spreading thinned it. Only a
            // true zero-density (fully spread away) removes a stable cloud.
            if (pool.Density <= 0 ||
                (!pool.Stable && pool.Density <= LOW_DENSITY_THRESHOLD &&
                 _rng.Next(100) < LOW_DENSITY_DISSIPATE_CHANCE + windSpeed))
            {
                Dissipate(gas, pool, zone, pool.Density <= 0 ? "ZeroDensity" : "LowDensityFlicker");
            }
        }

        /// <summary>One spread attempt — pick a direction (wind-biased per
        /// G.10), check passability, then either merge into an existing
        /// compatible gas (G.4) or spawn a new gas at the destination.</summary>
        private static void TrySpreadOnce(Entity gas, GasPoolPart pool, int x, int y, Zone zone,
            int windSpeed, int windDirIndex)
        {
            int dir = PickSpreadDirection(windSpeed, windDirIndex, _rng);
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
                          donorColor = pool.ColorString, receiverColor = existing.ColorString,
                          windSpeed, dir, windBiased = (windDirIndex >= 0 && dir == windDirIndex) });
                // Receiver grew — resync its glyph + repaint its cell.
                GasVisuals.Refresh(existing.ParentEntity, existing, zone);
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
                      toX = nx, toY = ny, chunk, donorAfter = pool.Density,
                      windSpeed, dir, windBiased = (windDirIndex >= 0 && dir == windDirIndex) });
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
            var pos = zone.GetEntityPosition(gas); // capture before removal
            Diag.Record("gas", "Dissipated", pool?.Creator, gas,
                new { gasId = pool?.GasId, density = pool?.Density ?? 0, cause });
            zone.RemoveEntity(gas);
            // Repaint the now-gas-free cell (it falls back to floor/contents).
            if (pos.x >= 0)
                ZoneRenderHooks.MarkCellDirty(pos.x, pos.y, "GasDissipate");
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

        // ──────────── G.10 wind helpers (pure, deterministic) ────────────

        /// <summary>Map a wind-direction string to a DX/DY index (0-7), or
        /// -1 if empty/unrecognized. N=0, NE=1, E=2, SE=3, S=4, SW=5, W=6,
        /// NW=7 — matches the <see cref="DX"/>/<see cref="DY"/> order.</summary>
        public static int WindDirectionToIndex(string dir)
        {
            if (string.IsNullOrWhiteSpace(dir)) return -1;
            switch (dir.Trim().ToUpperInvariant())
            {
                case "N": return 0;
                case "NE": return 1;
                case "E": return 2;
                case "SE": return 3;
                case "S": return 4;
                case "SW": return 5;
                case "W": return 6;
                case "NW": return 7;
                default: return -1;
            }
        }

        /// <summary>Pick a spread direction index (0-7). With probability
        /// <c>(windSpeed/100) × 0.9</c> returns <paramref name="windDirIndex"/>
        /// (downwind); otherwise a uniform-random direction. windSpeed ≤ 0
        /// or windDirIndex &lt; 0 ⇒ always random. Qud Gas.cs:231.</summary>
        public static int PickSpreadDirection(int windSpeed, int windDirIndex, System.Random rng)
        {
            // && short-circuits: a failed speed roll skips the 90 roll
            // (matches Qud's `num.in100() && 90.in100()` evaluation order).
            if (windSpeed > 0 && windDirIndex >= 0
                && rng.Next(100) < windSpeed && rng.Next(100) < 90)
                return windDirIndex;
            return rng.Next(8);
        }

        /// <summary>Per-tick spread attempt count, wind-scaled:
        /// <c>Random(1 + windSpeed/30, 4 + windSpeed/20)</c> inclusive.
        /// Qud Gas.cs:229. windSpeed clamped ≥0 by the caller.</summary>
        public static int ComputeSpreadAttempts(int windSpeed, System.Random rng)
        {
            int min = 1 + windSpeed / 30;
            int max = 4 + windSpeed / 20;
            if (max < min) max = min;
            return rng.Next(min, max + 1); // inclusive upper
        }
    }
}
