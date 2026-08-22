using NUnit.Framework;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W5.2 (Docs/FELLING-W5-PLAN.md §3) — the dark, made authored.
    /// W0.2 built <c>GetDepthAmbient</c> as a documented stub for this
    /// phase to fill: "W5 (catacombs) replaces the body with the
    /// authored ladder … and that phase must not also be inventing
    /// where the number comes from."
    ///
    /// <para><b>R1, resolved here.</b> Remembered cells rendered at a
    /// flat 0.2 grey, unmodulated — so the design's 0.12 catacomb and
    /// 0.02 dead-zone tiers would have drawn ground you are LOOKING AT
    /// darker than ground you merely remember, inverting fog-of-war.
    /// The remembered floor now derives from the zone's own ambient and
    /// is always dimmer than sight, at every rung.</para>
    /// </summary>
    public class DepthAmbientTests
    {
        [Test]
        public void TheLadder_DescendsByDepth()
        {
            // Surface unchanged; each step down is darker than the last.
            Assert.AreEqual(Zone.DefaultAmbientLevel,
                OverworldZoneManager.GetDepthAmbient(0), 0.0001f,
                "the surface is not the phase's business");
            float descent = OverworldZoneManager.GetDepthAmbient(1);
            float floor = OverworldZoneManager.GetDepthAmbient(2);
            float catacomb = OverworldZoneManager.GetDepthAmbient(3);

            Assert.Less(descent, Zone.DefaultAmbientLevel,
                "the shaft is dimmer than the day it opens onto");
            Assert.Less(floor, descent, "the floor is dimmer than the shaft");
            Assert.Less(catacomb, floor, "and the catacombs are dimmer still");
        }

        [Test]
        public void TheLadder_StaysNavigable()
        {
            // The introspection doc's UX warning, honored: a room the
            // player cannot navigate AT ALL is a bad room. Light is
            // content; it is not a wall.
            for (int depth = 0; depth <= 8; depth++)
                Assert.Greater(OverworldZoneManager.GetDepthAmbient(depth), 0f,
                    $"depth {depth} went fully black");
        }

        [Test]
        public void TheLadder_Bottoms_RatherThanFallingForever()
        {
            // Counter-check: deeper than the authored rungs must not
            // keep dividing toward zero.
            Assert.AreEqual(OverworldZoneManager.GetDepthAmbient(4),
                            OverworldZoneManager.GetDepthAmbient(12), 0.0001f,
                "below the deepest authored rung the ladder holds");
        }

        // ════════════════════════════════════════════════════════════
        //   R1 — memory is always dimmer than sight
        // ════════════════════════════════════════════════════════════

        [Test]
        public void MemoryIsNeverBrighterThanSight()
        {
            for (int depth = 0; depth <= 8; depth++)
            {
                float ambient = OverworldZoneManager.GetDepthAmbient(depth);
                float remembered = ZoneRenderer.RememberedBrightnessFor(ambient);
                Assert.Less(remembered, ambient,
                    $"depth {depth}: remembered ground outshone visible ground " +
                    "— fog-of-war inverted");
                Assert.Greater(remembered, 0f, $"depth {depth}: memory went black");
            }
        }

        [Test]
        public void TheSurfaceLook_IsUnchanged()
        {
            // Counter-check: the historical flat 0.2 is exactly what the
            // surface still gets, so nothing above ground shifts.
            Assert.AreEqual(0.2f,
                ZoneRenderer.RememberedBrightnessFor(Zone.DefaultAmbientLevel), 0.0001f,
                "the surface's remembered grey is the one it always was");
        }
    }
}
