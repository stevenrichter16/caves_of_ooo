using System;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using static CavesOfOoo.Core.FormationReachability;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// W5.1 — the Mouth (z=0): a void in the world, ringed by
    /// spray-fed green. Canon's rule is that overgrown mouths are
    /// FOUND, not shown: the hole is not a POI marker on the map UI,
    /// it is a thing you walk up to.
    ///
    /// <para>The lip is an ellipse of <c>SinkholeLip</c> — solid, so
    /// you cannot stroll into a hole by accident — with one gap where
    /// the way down sits. The rest of the chunk stays ordinary
    /// wilderness and, critically, stays crossable: a void must not
    /// seal the zone (pinned).</para>
    /// </summary>
    public sealed class SinkholeMouthBuilder : IZoneBuilder
    {
        public string Name => "SinkholeMouth";

        /// <summary>3100 — AFTER ConnectivityBuilder (3000). The hole is
        /// cut into an already-carved zone, so "is this still crossable"
        /// is a question with a meaningful baseline. Cutting it at
        /// formation time (2500) meant asking that question of a raw
        /// jungle chunk that was not crossable yet, and the repair loop
        /// dutifully ate the entire rim.</summary>
        public int Priority => 3100;

        private readonly ZoneManager _zoneManager;
        public SinkholeMouthBuilder(ZoneManager zoneManager) { _zoneManager = zoneManager; }

        /// <summary>Ellipse radii of the hole, in cells. Wide enough
        /// to read as "the chunk has a hole in it", short enough that
        /// lanes remain north and south of it — a void must not seal
        /// the zone (pinned).</summary>
        public const int RadiusX = 13;
        public const int RadiusY = 6;

        public bool BuildZone(Zone zone, EntityFactory factory, Random rng)
        {
            if (zone == null || factory == null || rng == null) return true;

            // Baseline for the delta contract, taken before we cut.
            FloodFromWest(zone, out bool crossedBefore);

            int cx = Zone.Width / 2, cy = Zone.Height / 2;
            // The gap: one arc of the rim where the ground slopes in and
            // the way down begins. Kept on the east side so a player
            // crossing from the west meets the rim before the entrance.
            int gapY = cy;

            var placed = new System.Collections.Generic.List<Entity>();
            for (int x = 1; x < Zone.Width - 1; x++)
                for (int y = 1; y < Zone.Height - 1; y++)
                {
                    double nx = (x - cx) / (double)RadiusX;
                    double ny = (y - cy) / (double)RadiusY;
                    double d = nx * nx + ny * ny;
                    if (d > 1.0) continue;               // outside the hole

                    bool rim = d > 0.72;                 // the lip band
                    bool isGap = x > cx && System.Math.Abs(y - gapY) <= 1;
                    if (rim && !isGap)
                    {
                        ClearFor(zone, y, x);
                        var lip = BuilderSpawn.TryPlaceOnce(zone, factory, "SinkholeLip", x, y);
                        if (lip != null) placed.Add(lip);
                    }
                    else if (!rim)
                    {
                        // Inside the rim: nothing grows on a hole.
                        ClearFor(zone, y, x);
                    }
                }

            // A void must not seal the chunk — but only OUR damage is
            // ours to repair. The DELTA contract (the W4.1 lesson): if
            // the zone could not be crossed before we touched it, the
            // rim is not the reason and eating it fixes nothing.
            if (!crossedBefore) placed.Clear();
            for (int attempt = 0; attempt < 60 && placed.Count > 0; attempt++)
            {
                FloodFromWest(zone, out bool crossed);
                if (crossed) break;
                var victim = placed[placed.Count - 1];
                placed.RemoveAt(placed.Count - 1);
                zone.RemoveEntity(victim);
            }

            // The way down, at the gap.
            int sx = System.Math.Min(cx + RadiusX - 1, Zone.Width - 2);
            var stairsCell = FindOpenNear(zone, sx, gapY);
            if (stairsCell.HasValue)
            {
                var stairs = factory.CreateEntity("StairsDown");
                if (stairs != null)
                {
                    zone.AddEntity(stairs, stairsCell.Value.x, stairsCell.Value.y);
                    string belowID = WorldMap.GetZoneBelow(zone.ZoneID);
                    if (_zoneManager != null && belowID != null)
                        _zoneManager.RegisterConnection(new ZoneConnection
                        {
                            SourceZoneID = zone.ZoneID,
                            SourceX = stairsCell.Value.x,
                            SourceY = stairsCell.Value.y,
                            TargetZoneID = belowID,
                            TargetX = stairsCell.Value.x,
                            TargetY = stairsCell.Value.y,
                            Type = "StairsDown"
                        });
                }
            }

            if (Diag.IsChannelEnabled("worldgen"))
                Diag.Record("worldgen", "SinkholeMouth", null, null,
                    new { zoneId = zone.ZoneID, lip = placed.Count,
                          wayDown = stairsCell.HasValue });
            return true;
        }

        /// <summary>Nearest open cell to (x,y), spiralling outward — the
        /// gap arc is cleared, but terrain may still occupy the exact
        /// cell we wanted.</summary>
        private static (int x, int y)? FindOpenNear(Zone zone, int x, int y)
        {
            for (int r = 0; r < 8; r++)
                for (int dx = -r; dx <= r; dx++)
                    for (int dy = -r; dy <= r; dy++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (IsOpenGround(zone, nx, ny)) return (nx, ny);
                    }
            return null;
        }
    }
}
