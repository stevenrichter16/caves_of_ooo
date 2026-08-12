using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W0.1 of the Felling world overhaul
    /// (<c>Docs/FELLING-IMPLEMENTATION-PLAN.md</c> §2) — the WorldClock.
    ///
    /// <para>The verification sweep's correction C1: the world clock
    /// ALREADY EXISTS as <c>TurnManager.TickCount</c> (save-persisted,
    /// advanced ~10 ticks per player action, +60 on rest, +10 per
    /// world-map step). <c>WorldClock</c> is therefore a STATELESS
    /// derivation — hour bands as a pure function of the tick — plus
    /// one small piece of session state: a last-band diff so band
    /// crossings emit exactly one <c>turn/BandChanged</c> diag record,
    /// even when a rest jumps the clock across several bands at once,
    /// and never on the first observation after a load (no "Dawn
    /// breaks" spam the moment a save opens).</para>
    ///
    /// <para>No MessageLog lines here by design: in-world flavor text
    /// is voice-gated content (Lore/Voices/VOICE-CARDS.md); W0 ships
    /// the mechanism, later phases ship the words.</para>
    /// </summary>
    public class WorldClockTests
    {
        [SetUp]
        public void SetUp()
        {
            Diag.ResetAll();
            WorldClock.ResetForTests();
        }

        // ════════════════════════════════════════════════════════
        // Band derivation — pure function of the tick
        // ════════════════════════════════════════════════════════

        [Test]
        public void GetBand_TickZero_IsDawn()
        {
            Assert.AreEqual(DayBand.Dawn, WorldClock.GetBand(0));
        }

        [Test]
        public void GetBand_BandBoundaries_SplitExactly()
        {
            // Each band is 300 ticks; the day is 1200. Boundaries are
            // half-open: tick 299 is still Dawn, tick 300 is Height.
            Assert.AreEqual(DayBand.Dawn, WorldClock.GetBand(299));
            Assert.AreEqual(DayBand.Height, WorldClock.GetBand(300));
            Assert.AreEqual(DayBand.Height, WorldClock.GetBand(599));
            Assert.AreEqual(DayBand.Dusk, WorldClock.GetBand(600));
            Assert.AreEqual(DayBand.Dusk, WorldClock.GetBand(899));
            Assert.AreEqual(DayBand.Dark, WorldClock.GetBand(900));
            Assert.AreEqual(DayBand.Dark, WorldClock.GetBand(1199));
        }

        [Test]
        public void GetBand_WrapsAtDayLength()
        {
            Assert.AreEqual(DayBand.Dawn, WorldClock.GetBand(1200));
            Assert.AreEqual(DayBand.Height, WorldClock.GetBand(1200 + 300));
            // Deep into the campaign the sun still rises.
            Assert.AreEqual(DayBand.Dawn, WorldClock.GetBand(1200 * 500));
        }

        [Test]
        public void GetBand_NegativeTick_ClampsToDawn()
        {
            // TickCount can't go negative in production, but the pure
            // function must not do modulo-of-negative surprises.
            Assert.AreEqual(DayBand.Dawn, WorldClock.GetBand(-1));
            Assert.AreEqual(DayBand.Dawn, WorldClock.GetBand(int.MinValue));
        }

        // ════════════════════════════════════════════════════════
        // Band naming — surface vs catacomb vocabularies
        // ════════════════════════════════════════════════════════

        [Test]
        public void BandName_SurfaceDepth_UsesSurfaceNames()
        {
            Assert.AreEqual("Dawn", WorldClock.BandName(DayBand.Dawn, 0));
            Assert.AreEqual("Height", WorldClock.BandName(DayBand.Height, 0));
            Assert.AreEqual("Dusk", WorldClock.BandName(DayBand.Dusk, 0));
            Assert.AreEqual("Dark", WorldClock.BandName(DayBand.Dark, 0));
        }

        [Test]
        public void BandName_NegativeDepth_IsSurface()
        {
            // WorldMap.GetDepth returns -1 for any non-"Overworld." ID
            // (including the world-map zone). That must read as surface,
            // never as an array index (sweep correction #5).
            Assert.AreEqual("Dawn", WorldClock.BandName(DayBand.Dawn, -1));
        }

        [Test]
        public void BandName_UndergroundDepth_UsesCatacombNames()
        {
            // The catacomb day-cycle is the Patch-Tender's, not the
            // sun's (catacomb_village_design.md) — same clock, its own
            // vocabulary.
            Assert.AreEqual("Bright", WorldClock.BandName(DayBand.Dawn, 1));
            Assert.AreEqual("Half-Bright", WorldClock.BandName(DayBand.Height, 3));
            Assert.AreEqual("Dim-Down", WorldClock.BandName(DayBand.Dusk, 5));
            Assert.AreEqual("Dark-Watch", WorldClock.BandName(DayBand.Dark, 10));
        }

        // ════════════════════════════════════════════════════════
        // Band-change dispatch — the last-band diff
        // ════════════════════════════════════════════════════════

        private static int CountBandChanged() => Diag.Snapshot(2000)
            .Count(r => r.Category == "turn" && r.Kind == "BandChanged");

        [Test]
        public void Notify_FirstObservation_PrimesWithoutEmitting()
        {
            // Loading a save must not announce the time of day.
            WorldClock.NotifyPlayerTurnEnd(650); // mid-Dusk
            Assert.AreEqual(0, CountBandChanged(),
                "first observation after reset/load must prime silently");
        }

        [Test]
        public void Notify_SameBand_DoesNotEmit()
        {
            // Counter-check: ordinary turns inside one band are silent.
            WorldClock.NotifyPlayerTurnEnd(10);
            WorldClock.NotifyPlayerTurnEnd(20);
            WorldClock.NotifyPlayerTurnEnd(290);
            Assert.AreEqual(0, CountBandChanged());
        }

        [Test]
        public void Notify_CrossingOneBoundary_EmitsExactlyOnce()
        {
            WorldClock.NotifyPlayerTurnEnd(290);  // Dawn (primes)
            WorldClock.NotifyPlayerTurnEnd(305);  // Height

            var records = Diag.Snapshot(2000)
                .Where(r => r.Category == "turn" && r.Kind == "BandChanged")
                .ToList();
            Assert.AreEqual(1, records.Count);
            StringAssert.Contains("Dawn", records[0].PayloadJson);
            StringAssert.Contains("Height", records[0].PayloadJson);
        }

        [Test]
        public void Notify_MultiBandJump_EmitsOnceWithEndpoints()
        {
            // Rest advances the clock +60; world-map travel +10/step —
            // a single EndTurnAndProcess can cross several bands. The
            // diff reports the endpoints once, not one record per band.
            WorldClock.NotifyPlayerTurnEnd(290);  // Dawn (primes)
            WorldClock.NotifyPlayerTurnEnd(610);  // Dusk — skipped Height

            var records = Diag.Snapshot(2000)
                .Where(r => r.Category == "turn" && r.Kind == "BandChanged")
                .ToList();
            Assert.AreEqual(1, records.Count,
                "a jump across bands is one transition, not several");
            StringAssert.Contains("Dawn", records[0].PayloadJson);
            StringAssert.Contains("Dusk", records[0].PayloadJson);
        }

        [Test]
        public void Notify_WrapAroundMidnight_Emits()
        {
            WorldClock.NotifyPlayerTurnEnd(1150); // Dark (primes)
            WorldClock.NotifyPlayerTurnEnd(1250); // Dawn of day 2
            Assert.AreEqual(1, CountBandChanged());
        }

        [Test]
        public void ResetForTests_ClearsPriming()
        {
            WorldClock.NotifyPlayerTurnEnd(290);
            WorldClock.NotifyPlayerTurnEnd(305);
            Assert.AreEqual(1, CountBandChanged());

            WorldClock.ResetForTests();
            Diag.ResetAll();

            // After a reset the next observation primes again — no emit
            // even though the internal last-band would have differed.
            WorldClock.NotifyPlayerTurnEnd(950);
            Assert.AreEqual(0, CountBandChanged());
        }

        // ════════════════════════════════════════════════════════
        // CurrentTick — reads the one true clock
        // ════════════════════════════════════════════════════════

        [Test]
        public void CurrentTick_ReadsThroughToTheActiveTurnManager()
        {
            // Honesty bound: the `?? 0` branch is NOT reachable from a
            // test. TurnManager.Active is process-static and nothing
            // ever clears it (its docstring claims GameBootstrap does;
            // no such code exists — sweep correction C11), so once any
            // test in the domain has constructed one, Active is
            // non-null forever. What IS pinnable is that WorldClock
            // reads the live instance rather than caching a tick.
            var tm = new TurnManager();
            Assert.AreEqual(tm.TickCount, WorldClock.CurrentTick);
        }

        [Test]
        public void CurrentTick_FollowsClockJumps_NotJustTurns()
        {
            // Rest (+60) and world-map steps (+10) move the clock via
            // AdvanceClock without any actor taking a turn. A clock
            // that only tracked turn-taking would freeze through a
            // night's rest.
            var tm = new TurnManager();
            int before = WorldClock.CurrentTick;
            tm.AdvanceClock(60);
            Assert.AreEqual(before + 60, WorldClock.CurrentTick);
        }
    }
}
