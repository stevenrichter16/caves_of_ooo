using System;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using static CavesOfOoo.Core.FormationReachability;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// W5.3 — the Drowned Sima floor: standing water where the shaft's
    /// rain has been collecting since the roof fell in, semi-drowned
    /// green around it, and the gin frogs that were the reason anybody
    /// wrote the survey down.
    ///
    /// <para>The pool is a broad basin, not a flood: a floor that is
    /// entirely water is a screenshot rather than a room, so the banks
    /// stay walkable (pinned).</para>
    /// </summary>
    public sealed class DrownedSimaBuilder : IZoneBuilder
    {
        public string Name => "DrownedSima";

        /// <summary>3100 — after ConnectivityBuilder (3000) carves the
        /// floor, so the basin is cut into a connected room. The W5.1
        /// mouth lesson, applied from the start.</summary>
        public int Priority => 3100;

        public const int FrogsMin = 2;
        public const int FrogsMax = 4;

        public bool BuildZone(Zone zone, EntityFactory factory, Random rng)
        {
            if (zone == null || factory == null || rng == null) return true;

            // The basin: an ellipse of standing water, offset from centre
            // so the room does not read as a target.
            int cx = Zone.Width / 2 + rng.Next(-6, 7);
            int cy = Zone.Height / 2 + rng.Next(-2, 3);
            int rx = 16 + rng.Next(6);
            int ry = 5 + rng.Next(2);

            int water = 0;
            for (int x = 1; x < Zone.Width - 1; x++)
                for (int y = 1; y < Zone.Height - 1; y++)
                {
                    double nx = (x - cx) / (double)rx;
                    double ny = (y - cy) / (double)ry;
                    if (nx * nx + ny * ny > 1.0) continue;
                    if (!IsOpenGround(zone, x, y)) continue;
                    if (BuilderSpawn.TryPlaceOnce(zone, factory, "MirePool", x, y) != null)
                        water++;
                }

            // What lives in it.
            int frogs = 0;
            int wanted = FrogsMin + rng.Next(FrogsMax - FrogsMin + 1);
            for (int attempt = 0; attempt < 200 && frogs < wanted; attempt++)
            {
                int x = 2 + rng.Next(Zone.Width - 4);
                int y = 2 + rng.Next(Zone.Height - 4);
                if (!IsOpenGround(zone, x, y)) continue;
                if (BuilderSpawn.TryPlaceOnce(zone, factory, "GinFrog", x, y) != null)
                    frogs++;
            }

            if (Diag.IsChannelEnabled("worldgen"))
                Diag.Record("worldgen", "FloorArchetype", null, null,
                    new { zoneId = zone.ZoneID, archetype = "DrownedSima",
                          water, frogs });
            return true;
        }
    }
}
