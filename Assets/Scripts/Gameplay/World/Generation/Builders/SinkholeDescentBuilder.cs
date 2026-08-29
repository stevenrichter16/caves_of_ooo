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

            // The cache ledge — and the expedition. Close-out 🔵: the
            // docstring, the W5.1 plan line and canon (:692-694) all
            // promised this and nothing built it — living-doc-vs-impl
            // drift of exactly the kind Q4 exists to catch. One sack of
            // the supplies they did not get to use, and the bones of
            // whoever packed it, beside a ledge on the LOWEST terrace:
            // they nearly made it back up.
            bool cache = false;
            for (int attempt = 0; attempt < 80 && !cache; attempt++)
            {
                int x = 4 + rng.Next(Zone.Width - 8);
                int y = 3 + (LedgeRows - 1) * ((Zone.Height - 6) / LedgeRows) + 1;
                if (!IsOpenGround(zone, x, y)) continue;
                var sack = BuilderSpawn.TryPlaceOnce(zone, factory, "Sack", x, y);
                if (sack == null) continue;
                var hold = sack.GetPart<ContainerPart>();
                if (hold != null)
                {
                    hold.AddItem(factory.CreateEntity("Torch"));
                    hold.AddItem(factory.CreateEntity("DriedMeat"));
                    hold.AddItem(factory.CreateEntity("HealingTonic"));
                }
                if (IsOpenGround(zone, x + 1, y))
                    BuilderSpawn.TryPlaceOnce(zone, factory, "Bones", x + 1, y);
                cache = true;
            }

            if (Diag.IsChannelEnabled("worldgen"))
                Diag.Record("worldgen", "SinkholeDescent", null, null,
                    new { zoneId = zone.ZoneID, ledges, anchors, cache });
            return true;
        }
    }
}
