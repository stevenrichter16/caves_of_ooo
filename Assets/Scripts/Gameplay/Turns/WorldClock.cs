using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// The four bands of the world's day. Surface names; underground
    /// cultures keep the same clock under their own vocabulary — see
    /// <see cref="WorldClock.BandName"/>.
    /// </summary>
    public enum DayBand
    {
        Dawn = 0,
        Height = 1,
        Dusk = 2,
        Dark = 3,
    }

    /// <summary>
    /// Hour bands derived from the one true clock.
    ///
    /// <para><b>There is no second clock.</b> The world's time is
    /// <see cref="TurnManager.TickCount"/> — already advanced ~10 ticks
    /// per player action, +60 by rest (<c>RestSystem.RestClockTurns</c>),
    /// +10 per world-map step (<c>WorldMapTravelCostPart</c>), and
    /// already save-persisted and restored. This class is a stateless
    /// derivation over it: no persisted state, no FormatVersion bump,
    /// correct immediately after a load for free.
    /// (Felling overhaul W0.1; sweep correction C1 in
    /// <c>Docs/FELLING-IMPLEMENTATION-PLAN.md</c> §1.)</para>
    ///
    /// <para><b>The one piece of session state</b> is a last-band diff
    /// so a band crossing emits exactly one <c>turn/BandChanged</c>
    /// diag record — including when a rest jumps the clock across
    /// several bands in a single <c>EndTurnAndProcess</c> — and never
    /// on the first observation after a reset/load. It is primed
    /// lazily and cleared by <see cref="ResetForTests"/> (domain
    /// reload is off in this project; statics outlive play sessions).</para>
    ///
    /// <para><b>Band widths are authored in ticks</b>, calibrated
    /// against the existing tick economy: a 300-tick band ≈ 30 player
    /// actions; a full day is 1,200 ticks. Rest (+60) moves the clock
    /// a fifth of a band; crossing the world map moves it fast.</para>
    /// </summary>
    public static class WorldClock
    {
        /// <summary>Ticks in a full day-cycle.</summary>
        public const int DayLengthTicks = 1200;

        /// <summary>Ticks per band; four bands per day.</summary>
        public const int BandLengthTicks = DayLengthTicks / 4;

        private static readonly string[] SurfaceNames =
            { "Dawn", "Height", "Dusk", "Dark" };

        /// <summary>
        /// The catacomb vocabulary for the same clock — the villagers'
        /// day is the Patch-Tender's cycle, not the sun's
        /// (catacomb_village_design.md; Felling design §4.2).
        /// </summary>
        private static readonly string[] CatacombNames =
            { "Bright", "Half-Bright", "Dim-Down", "Dark-Watch" };

        /// <summary>Last band observed by <see cref="NotifyPlayerTurnEnd"/>;
        /// null = unprimed (fresh session or post-reset).</summary>
        private static DayBand? _lastBand;

        /// <summary>
        /// The current tick, read from the active TurnManager — the
        /// majority idiom in this codebase (StoryletPart,
        /// ConversationPredicates), and valid earliest during load
        /// because <c>LoadTurnManager</c> re-registers
        /// <see cref="TurnManager.Active"/> before providers rewire.
        /// Zero when no TurnManager exists (EditMode fixtures are
        /// permanently Dawn).
        /// </summary>
        public static int CurrentTick =>
            TurnManager.Active != null ? TurnManager.Active.TickCount : 0;

        /// <summary>
        /// Pure band derivation. Negative ticks clamp to Dawn — the
        /// clock never runs before the world began.
        /// </summary>
        public static DayBand GetBand(int tick)
        {
            if (tick < 0) return DayBand.Dawn;
            return (DayBand)(tick % DayLengthTicks / BandLengthTicks);
        }

        /// <summary>
        /// The band's display vocabulary by depth. Depth ≤ 0 is the
        /// surface set — deliberately including −1, which is what
        /// <c>WorldMap.GetDepth</c> returns for every non-Overworld
        /// zone ID (the world-map zone, test zones): those must read
        /// as surface, never index an array.
        /// </summary>
        public static string BandName(DayBand band, int depth)
        {
            var names = depth > 0 ? CatacombNames : SurfaceNames;
            return names[(int)band];
        }

        /// <summary>
        /// The per-player-turn seam: called from
        /// <c>InputHandler.EndTurnAndProcess</c> AFTER
        /// <c>ZoneTileStateSystem.OnPlayerTurnEnd</c> — never from
        /// inside <c>TurnManager.EndTurn</c> (per-actor TickEnd trap +
        /// turn-divider coupling; sweep hazards 2 and 6).
        ///
        /// <para>First observation primes silently — loading a save or
        /// starting a session must not announce the time of day. After
        /// that, each band change emits ONE <c>turn/BandChanged</c>
        /// record with the endpoints, however many bands the jump
        /// skipped: a jump is one transition, not several.</para>
        /// </summary>
        public static void NotifyPlayerTurnEnd(int tick)
        {
            DayBand band = GetBand(tick);

            if (_lastBand == null)
            {
                _lastBand = band;
                return;
            }

            if (band == _lastBand.Value) return;

            DayBand from = _lastBand.Value;
            _lastBand = band;

            Diag.Record(
                category: "turn",
                kind: "BandChanged",
                payload: new
                {
                    fromBand = from.ToString(),
                    toBand = band.ToString(),
                    tick,
                });
        }

        /// <summary>Clears the last-band priming. New-game bootstrap
        /// needs this because statics survive domain reloads in this
        /// project; the next observation primes silently.</summary>
        public static void Reset()
        {
            _lastBand = null;
        }

        /// <summary>Test alias for <see cref="Reset"/>, matching the
        /// codebase's ResetForTests convention.</summary>
        public static void ResetForTests() => Reset();
    }
}
