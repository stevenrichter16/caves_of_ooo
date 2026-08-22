using System;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using static CavesOfOoo.Core.FormationReachability;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// W5.1 — the Descent (z=1): terraced ledges spiralling down the
    /// shaft, rope anchors somebody else drove into the rock, and a
    /// cache ledge holding a prior expedition's supplies — and,
    /// eventually, the expedition.
    ///
    /// <para>R2 (RPG framing): this is a place you survive, not a
    /// lottery. Climbing costs turns; the way back UP always exists
    /// (pinned) because a one-way drop into a phase's deepest content
    /// is how a recoverable-death RPG accidentally becomes a
    /// roguelike.</para>
    /// </summary>
    public sealed class SinkholeDescentBuilder : IZoneBuilder
    {
        public string Name => "SinkholeDescent";
        public int Priority => 2500;

        private readonly ZoneManager _zoneManager;
        public SinkholeDescentBuilder(ZoneManager zoneManager) { _zoneManager = zoneManager; }

        public const int LedgeRows = 4;

        public bool BuildZone(Zone zone, EntityFactory factory, Random rng)
        {
            if (zone == null || factory == null || rng == null) return true;

            // Terraces: broad shelves stepping down the shaft, offset so
            // the eye reads a spiral rather than a stack.
            int ledges = 0;
            for (int row = 0; row < LedgeRows; row++)
            {
                int y = 3 + row * ((Zone.Height - 6) / LedgeRows);
                int span = 10 + rng.Next(8);
                int startX = 4 + ((row % 2 == 0) ? 0 : Zone.Width / 2) + rng.Next(6);
                for (int i = 0; i < span; i++)
                {
                    int x = startX + i;
                    if (!IsOpenGround(zone, x, y)) continue;
                    if (BuilderSpawn.TryPlaceOnce(zone, factory, "DescentLedge", x, y) != null)
                        ledges++;
                }
            }

            // Somebody came this way before you, and left iron in the rock.
            int anchors = 0;
            for (int attempt = 0; attempt < 120 && anchors < 3; attempt++)
            {
                int x = 2 + rng.Next(Zone.Width - 4);
                int y = 2 + rng.Next(Zone.Height - 4);
                if (!IsOpenGround(zone, x, y)) continue;
                if (BuilderSpawn.TryPlaceOnce(zone, factory, "RopeAnchor", x, y) != null)
                    anchors++;
            }

            if (Diag.IsChannelEnabled("worldgen"))
                Diag.Record("worldgen", "SinkholeDescent", null, null,
                    new { zoneId = zone.ZoneID, ledges, anchors });
            return true;
        }
    }
}
