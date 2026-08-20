using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// W2.3 — Height-band glare in the Beating, WorldClock's first
    /// gameplay consumer (Docs/FELLING-W1-W2-PLAN.md §7.6). During the
    /// Height hours, an exposed player on the open pan accumulates sun:
    /// every <see cref="ExposureTurnsPerStack"/> consecutive exposed
    /// turns applies (or deepens) <see cref="ParchedEffect"/>.
    ///
    /// <para><b>Exposed means:</b> surface Beating zone, the Height
    /// band, a non-interior cell (a tent's inside is architecture —
    /// W2.4 marks stamp interiors), and nothing worn on the head. Any
    /// turn that fails a condition RESETS the counter — shade is relief,
    /// not a pause button.</para>
    ///
    /// <para><b>Player-only, deliberately</b> (plan D2): the design doc
    /// says "creatures", but Tent-Right NPCs live outdoors in the
    /// Beating — a literal reading permanently parches every native.
    /// Natives are adapted; the sun is the traveller's problem.</para>
    /// </summary>
    public static class BeatingGlareSystem
    {
        /// <summary>Consecutive exposed turns per Parched stack. At 10,
        /// crossing one Height band bareheaded (300 ticks) is badly
        /// parched long before the band turns — the pan is a place you
        /// PREPARE for, which is the biome's whole argument.</summary>
        public const int ExposureTurnsPerStack = 10;

        private static int _exposure;

        public static void ResetForTests() => _exposure = 0;

        /// <summary>Called once per player turn, beside
        /// <c>WorldClock.NotifyPlayerTurnEnd</c> (InputHandler). Cheap
        /// checks first; every early-out resets the streak.</summary>
        public static void OnPlayerTurnEnd(Entity player, Zone zone, int tick)
        {
            if (player == null || zone == null) { _exposure = 0; return; }
            if (WorldClock.GetBand(tick) != DayBand.Height) { _exposure = 0; return; }

            var (wx, wy, wz) = WorldMap.FromZoneID(zone.ZoneID);
            if (wx < 0 || wz != 0) { _exposure = 0; return; }
            if (!WorldMapAuthoring.InBounds(wx, wy)
                || WorldMapAuthoring.BiomeAt(wx, wy) != BiomeType.Beating)
            { _exposure = 0; return; }

            var cell = zone.GetEntityCell(player);
            if (cell == null || cell.IsInterior) { _exposure = 0; return; }
            if (HasHeadCover(player)) { _exposure = 0; return; }

            _exposure++;
            if (_exposure < ExposureTurnsPerStack) return;
            _exposure = 0;

            player.ApplyEffect(new ParchedEffect(), source: null, zone: zone);

            if (Diag.IsChannelEnabled("effect"))
            {
                Diag.Record("effect", "GlareExposure", actor: player, payload: new
                {
                    zoneID = zone.ZoneID,
                    tick,
                    threshold = ExposureTurnsPerStack,
                });
            }
        }

        /// <summary>Anything worn on the head is shade. First head wins
        /// on multi-headed anatomies — cover one, cover enough.</summary>
        public static bool HasHeadCover(Entity e)
        {
            var body = e?.GetPart<Body>();
            var head = body?.GetPartByType("Head");
            return head?.Equipped != null;
        }
    }
}
