using System.Collections.Generic;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// PALIMPSEST P4 — a local action changes another part of the room.
    ///
    /// <para>Charge travels through anything that conducts: a connected
    /// sheet of water, a run of metal grating, or a mix of both. Fire
    /// travels through anything that burns. This is the phase where the
    /// greenhouse vignette becomes possible — arc into the puddle, the
    /// charge finds the pipe, and something four tiles away happens.</para>
    ///
    /// <para><b>No Substrate layer was built</b>, and that is a
    /// deliberate divergence from the spec's §4. Conductivity and
    /// combustibility already exist as real numbers on two shipped
    /// systems: <see cref="LiquidDefinition"/> gives water and brine
    /// Conductivity 100 and oil Combustibility 90, and
    /// <see cref="MaterialPart"/> carries both per entity. A parallel
    /// tile-substrate table would have restated data the game already
    /// has, and the two would have drifted. A metal grate is simply an
    /// entity whose material conducts.</para>
    ///
    /// <para><b>Bounded by design</b> (spec §11): charge reaches
    /// <see cref="ChargeRange"/> tiles through water and
    /// <see cref="MetalChargeRange"/> through metal. Players are allowed
    /// to build ridiculous situations; the CPU is not allowed to join
    /// them.</para>
    /// </summary>
    public static class TilePropagationSystem
    {
        /// <summary>Tiles charge travels through a connected wet sheet.</summary>
        public const int ChargeRange = 3;

        /// <summary>Metal carries further than water — the whole reason
        /// to route a shot through grating rather than a puddle.</summary>
        public const int MetalChargeRange = 4;

        /// <summary>Tiles fire crawls across connected fuel.</summary>
        public const int FireRange = 2;

        /// <summary>A liquid at or above this conducts. Reuses the
        /// threshold LiquidCoveredEffect already established rather than
        /// inventing a second one.</summary>
        public const int ConductiveThreshold =
            LiquidCoveredEffect.CONDUCTIVITY_AMPLIFY_THRESHOLD;

        /// <summary>A material at or above this burns.</summary>
        public const int FlammableThreshold = 50;

        // Reusable scratch — propagation runs inside the turn loop.
        private static readonly List<int> _frontier = new List<int>(16);
        private static readonly List<int> _next = new List<int>(16);
        private static readonly HashSet<int> _seen = new HashSet<int>();
        // Wx opt review §3b freebie: the seed collection ran 3× per
        // player turn with a fresh List<int> even in inert zones —
        // inconsistent with this file's own scratch hygiene above.
        private static readonly List<int> _writtenScratch = new List<int>(64);

        // ── Conductivity ─────────────────────────────────────────

        /// <summary>
        /// True when charge can pass through this tile — a conductive
        /// coating on the floor, or something standing in it whose
        /// material conducts (grating, a metal door, an iron construct).
        /// </summary>
        public static bool IsConductive(Zone zone, int x, int y)
        {
            if (zone == null) return false;

            var state = zone.TileState.Get(x, y);
            if (state != null && LiquidRegistry.IsInitialized)
            {
                for (int i = 0; i < state.Coatings.Count; i++)
                {
                    var def = LiquidRegistry.Get(state.Coatings[i].Id);
                    if (def != null && def.Conductivity >= ConductiveThreshold) return true;
                }
            }

            return CellMaterialAtLeast(zone, x, y, conductivity: true);
        }

        /// <summary>True when fire can take hold here — flammable
        /// coating, or something flammable standing in the cell.</summary>
        public static bool IsFlammable(Zone zone, int x, int y)
        {
            if (zone == null) return false;

            var state = zone.TileState.Get(x, y);
            if (state != null && LiquidRegistry.IsInitialized)
            {
                for (int i = 0; i < state.Coatings.Count; i++)
                {
                    var def = LiquidRegistry.Get(state.Coatings[i].Id);
                    if (def != null && def.Combustibility >= FlammableThreshold) return true;
                }
            }

            return CellMaterialAtLeast(zone, x, y, conductivity: false);
        }

        private static bool CellMaterialAtLeast(Zone zone, int x, int y, bool conductivity)
        {
            var cell = zone.GetCell(x, y);
            if (cell == null) return false;

            for (int i = 0; i < cell.Objects.Count; i++)
            {
                var e = cell.Objects[i];
                if (e == null) continue;
                var mat = e.GetPart<MaterialPart>();
                if (mat == null) continue;

                if (conductivity)
                {
                    if (mat.Conductivity >= ConductiveThreshold) return true;
                }
                else if (mat.Combustibility >= FlammableThreshold) return true;
            }
            return false;
        }

        // ── Propagation ──────────────────────────────────────────

        /// <summary>
        /// Spreads charge outward from every tile that currently holds
        /// it, through connected conductive ground. Returns how many new
        /// tiles were reached.
        ///
        /// <para>The range is per-step: a tile reached through metal may
        /// continue further than one reached through water, which is
        /// what makes routing a shot along grating a real decision.</para>
        /// </summary>
        public static int PropagateCharge(Zone zone, Entity cause = null)
        {
            if (zone == null) return 0;
            var state = zone.TileState;

            _frontier.Clear();
            _seen.Clear();

            // Seed: every tile already carrying charge.
            _writtenScratch.Clear();
            state.CollectWrittenKeys(_writtenScratch);
            for (int i = 0; i < _writtenScratch.Count; i++)
            {
                int key = _writtenScratch[i];
                int x = key % Zone.Width, y = key / Zone.Width;
                if (state.Charge(x, y) <= 0) continue;
                _frontier.Add(key);
                _seen.Add(key);
            }
            if (_frontier.Count == 0) return 0;

            int seedCount = _frontier.Count;
            int reached = 0, metalReached = 0;
            int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
            int maxSteps = MetalChargeRange > ChargeRange ? MetalChargeRange : ChargeRange;

            for (int step = 0; step < maxSteps; step++)
            {
                _next.Clear();

                for (int i = 0; i < _frontier.Count; i++)
                {
                    int key = _frontier[i];
                    int cx = key % Zone.Width, cy = key / Zone.Width;

                    for (int ox = -1; ox <= 1; ox++)
                    {
                        for (int oy = -1; oy <= 1; oy++)
                        {
                            if (ox == 0 && oy == 0) continue;
                            int nx = cx + ox, ny = cy + oy;
                            if (!zone.InBounds(nx, ny)) continue;

                            int nkey = ny * Zone.Width + nx;
                            if (_seen.Contains(nkey)) continue;
                            if (!IsConductive(zone, nx, ny)) continue;

                            // Metal carries further than water. A step
                            // beyond the water range only continues if
                            // THIS tile is metal.
                            bool metal = CellMaterialAtLeast(zone, nx, ny, conductivity: true);
                            int allowed = metal ? MetalChargeRange : ChargeRange;
                            if (step >= allowed) continue;

                            _seen.Add(nkey);
                            _next.Add(nkey);
                            zone.TileState.AddCharge(nx, ny, 1);
                            reached++;
                            if (metal) metalReached++;
                            if (nx < minX) minX = nx;
                            if (ny < minY) minY = ny;
                            if (nx > maxX) maxX = nx;
                            if (ny > maxY) maxY = ny;

                            // Wx opt review §3b — per-cell steps live on
                            // the off-by-default "tile-verbose" channel:
                            // this flood runs every player turn, and a
                            // self-sustaining peat fire emitted 25-75
                            // eagerly-serialized steps per turn on the
                            // always-on channel. The aggregate Wave
                            // record below keeps the every-turn stream;
                            // the per-cell chain stays traceable when a
                            // debugger opts in. See DiagChannelSplitTests.
                            if (Diag.IsChannelEnabled("tile-verbose"))
                                Diag.Record("tile-verbose", "PropagationStep", cause, null,
                                    new
                                    {
                                        medium = metal ? "metal" : "conductive_liquid",
                                        fromX = cx, fromY = cy, toX = nx, toY = ny,
                                        step,
                                    });
                        }
                    }
                }

                if (_next.Count == 0) break;
                _frontier.Clear();
                _frontier.AddRange(_next);
            }

            // Wx §3b — ONE aggregate record per flood that moved charge:
            // the always-on trace of "the arc found the pipe" without a
            // record per cell. Silent when nothing spread (the every-turn
            // inert case costs nothing).
            if (reached > 0 && Diag.IsChannelEnabled("tile"))
                Diag.Record("tile", "PropagationWave", cause, null,
                    new
                    {
                        wave = "charge", seedCount, reached,
                        metal = metalReached, liquid = reached - metalReached,
                        minX, minY, maxX, maxY,
                    });

            return reached;
        }

        /// <summary>
        /// Spreads embers to adjacent flammable ground. Deliberately
        /// shorter-ranged and slower than charge: fire that crawled as
        /// fast as electricity would consume a room before a player
        /// could respond to it.
        /// </summary>
        public static int PropagateFire(Zone zone, Entity cause = null)
        {
            if (zone == null) return 0;
            var state = zone.TileState;

            _frontier.Clear();
            _seen.Clear();

            _writtenScratch.Clear();
            state.CollectWrittenKeys(_writtenScratch);
            for (int i = 0; i < _writtenScratch.Count; i++)
            {
                int key = _writtenScratch[i];
                int x = key % Zone.Width, y = key / Zone.Width;
                if (!state.HasResidue(x, y, "embers")) continue;
                _frontier.Add(key);
                _seen.Add(key);
            }
            if (_frontier.Count == 0) return 0;

            int seedCount = _frontier.Count;
            int spread = 0;
            int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
            for (int step = 0; step < FireRange; step++)
            {
                _next.Clear();

                for (int i = 0; i < _frontier.Count; i++)
                {
                    int key = _frontier[i];
                    int cx = key % Zone.Width, cy = key / Zone.Width;

                    for (int ox = -1; ox <= 1; ox++)
                    {
                        for (int oy = -1; oy <= 1; oy++)
                        {
                            if (ox == 0 && oy == 0) continue;
                            int nx = cx + ox, ny = cy + oy;
                            if (!zone.InBounds(nx, ny)) continue;

                            int nkey = ny * Zone.Width + nx;
                            if (_seen.Contains(nkey)) continue;
                            // Fire only takes hold where there is fuel.
                            if (!IsFlammable(zone, nx, ny)) continue;

                            _seen.Add(nkey);
                            _next.Add(nkey);
                            zone.TileState.WriteResidue(nx, ny, "embers", 3);
                            spread++;
                            if (nx < minX) minX = nx;
                            if (ny < minY) minY = ny;
                            if (nx > maxX) maxX = nx;
                            if (ny > maxY) maxY = ny;

                            // Wx §3b — verbose, mirroring PropagateCharge.
                            if (Diag.IsChannelEnabled("tile-verbose"))
                                Diag.Record("tile-verbose", "PropagationStep", cause, null,
                                    new
                                    {
                                        medium = "fuel",
                                        fromX = cx, fromY = cy, toX = nx, toY = ny,
                                        step,
                                    });
                        }
                    }
                }

                if (_next.Count == 0) break;
                _frontier.Clear();
                _frontier.AddRange(_next);
            }

            // Wx §3b — one aggregate per fire flood that spread.
            if (spread > 0 && Diag.IsChannelEnabled("tile"))
                Diag.Record("tile", "PropagationWave", cause, null,
                    new
                    {
                        wave = "fire", seedCount, reached = spread,
                        minX, minY, maxX, maxY,
                    });

            return spread;
        }
    }
}
