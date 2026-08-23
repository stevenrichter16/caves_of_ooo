using System;
using System.Collections.Generic;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using static CavesOfOoo.Core.FormationReachability;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// W5.4 — a catacomb village on a sinkhole floor: the Stranded
    /// Settlement archetype.
    ///
    /// <para><b>Layout follows canon's authored sketch</b>
    /// (FELLING-WORLD-DESIGN.md:1099-1113) scaled from its 19×9
    /// schematic to the 80×25 zone, preserving the topology rather than
    /// the cell counts: the patch dead-centre, niche columns down the
    /// left and right wall faces in three tiers, the plaque-wall along
    /// the top band and the wall segments beside the niches, and two
    /// Pebble-Sundew mats flanking the way out at the bottom.</para>
    ///
    /// <para><b>The reveal order is the design</b> (:721-733): the GLOW
    /// before the people. The patch sits on the entry's sightline and
    /// the villagers stand off that axis, so the first thing a player
    /// sees from the stairs is light, not a person.</para>
    ///
    /// <para><b>Why the footprint is claimed:</b> the sinkhole floor
    /// pipeline still runs PopulationBuilder(UndergroundTier) after
    /// this builder, and at this depth that table rolls snapjaws. A
    /// village square full of snapjaws is not a village — so the
    /// chamber goes into <c>GenReservedCells</c>, which is how the
    /// surface town keeps its own square clear.</para>
    ///
    /// <para><b>Not a shop</b> (plan R6): these are a people with a
    /// culture, not a trade hub. No TraderPart, no stock table. Canon
    /// has a 22-row goods table and an 11-row services table; that is
    /// an economy phase, not this one.</para>
    /// </summary>
    public sealed class StrandedSettlementBuilder : IZoneBuilder
    {
        public string Name => "StrandedSettlement";

        /// <summary>3100 — after ConnectivityBuilder (3000) carves the
        /// floor, the W5.1 mouth lesson applied from the start.</summary>
        public int Priority => 3100;

        /// <summary>Cold-eye 🔴: the patch used to sit at
        /// (Zone.Width/2, Zone.Height/2) — which is EXACTLY where the
        /// Descent's StairsDownBuilder starts searching, so the player
        /// arrived inside the hearth and the floor's StairsUpBuilder
        /// (priority 3500, after this) planted its staircase in the
        /// middle of the village's holiest object. The chamber is now
        /// laid out relative to the ARRIVAL cell, read from the
        /// connection the Descent registered.</summary>
        public const int PatchOffsetFromArrival = 14;

        private readonly ZoneManager _zoneManager;
        public StrandedSettlementBuilder(ZoneManager zoneManager = null)
        { _zoneManager = zoneManager; }

        /// <summary>Where the player will come in, if we can know it.</summary>
        private (int x, int y) ArrivalCell(Zone zone)
        {
            if (_zoneManager != null)
            {
                var conns = _zoneManager.GetConnectionsTo(zone.ZoneID, "StairsDown");
                if (conns != null && conns.Count > 0)
                    return (conns[0].TargetX, conns[0].TargetY);
            }
            return (Zone.Width / 2, Zone.Height / 2);
        }

        public bool BuildZone(Zone zone, EntityFactory factory, Random rng)
        {
            if (zone == null || factory == null || rng == null) return true;

            var arrival = ArrivalCell(zone);
            // Put the hearth a good walk from the door, on the side with
            // more room, so the entry sightline crosses the chamber.
            int PatchCenterX = arrival.x < Zone.Width / 2
                ? System.Math.Min(arrival.x + PatchOffsetFromArrival, Zone.Width - 8)
                : System.Math.Max(arrival.x - PatchOffsetFromArrival, 7);
            int PatchCenterY = Zone.Height / 2;

            var claimed = new List<(int x, int y)>();

            // ── The patch: the town. An ellipse of mature fungus at the
            //    chamber's centre, glowing radius 4.
            int patch = 0;
            for (int x = PatchCenterX - 4; x <= PatchCenterX + 4; x++)
                for (int y = PatchCenterY - 3; y <= PatchCenterY + 3; y++)
                {
                    double nx = (x - PatchCenterX) / 4.5, ny = (y - PatchCenterY) / 3.5;
                    if (nx * nx + ny * ny > 1.0) continue;
                    if (!IsOpenGround(zone, x, y)) continue;
                    if (BuilderSpawn.TryPlaceOnce(zone, factory, "HearthPatch", x, y) != null)
                    { patch++; claimed.Add((x, y)); }
                }

            // ── The Drosera ring: broken, so the patch is approachable.
            //    Canon says the patch is ENCIRCLED; the gaps are how a
            //    village walks to its own hearth.
            int ring = 0;
            for (int deg = 0; deg < 360; deg += 30)
            {
                if (deg % 90 == 0) continue;               // the four ways in
                int x = PatchCenterX + (int)Math.Round(8 * Math.Cos(deg * Math.PI / 180));
                int y = PatchCenterY + (int)Math.Round(5 * Math.Sin(deg * Math.PI / 180));
                if (!IsOpenGround(zone, x, y)) continue;
                if (BuilderSpawn.TryPlaceOnce(zone, factory, "DroseraRing", x, y) != null)
                { ring++; claimed.Add((x, y)); }
            }

            // ── Niche homes: two columns down the wall faces, in three
            //    tiers with gaps for the carved stairs and rope ladders.
            int niches = 0;
            foreach (int nx2 in new[] { 8, 9, 70, 71 })
                foreach (var band in new[] { (4, 7), (9, 12), (14, 17) })
                    for (int y = band.Item1; y <= band.Item2; y++)
                    {
                        if (!IsOpenGround(zone, nx2, y)) continue;
                        if (BuilderSpawn.TryPlaceOnce(zone, factory, "NicheHome", nx2, y) != null)
                        { niches++; claimed.Add((nx2, y)); }
                    }

            // ── The plaque-wall: the top band, oldest at the floor.
            int wall = 0;
            for (int x = 12; x <= 68; x += 2)
            {
                if (!IsOpenGround(zone, x, 2)) continue;
                if (BuilderSpawn.TryPlaceOnce(zone, factory, "PlaqueWall", x, 2) != null)
                { wall++; claimed.Add((x, 2)); }
            }

            // ── The six authored plaques (Lore/Codex/05_PlaqueWall.md),
            //    read left to right along the wall; the oldest sits
            //    lowest, at floor level, where canon puts it.
            var named = new[] { "PlaqueRiane", "PlaqueOssu", "PlaqueMerrin",
                                "PlaqueSmoothed", "PlaqueVashti" };
            int px = 20;
            foreach (var bp in named)
            {
                for (int attempt = 0; attempt < 12; attempt++, px += 3)
                {
                    if (!IsOpenGround(zone, px, 3)) continue;
                    if (BuilderSpawn.TryPlaceOnce(zone, factory, bp, px, 3) != null)
                    { claimed.Add((px, 3)); px += 3; break; }
                }
            }
            for (int x = 30; x < 50; x++)
                if (IsOpenGround(zone, x, PatchCenterY + 8)
                    && BuilderSpawn.TryPlaceOnce(zone, factory, "PlaqueOldest", x, PatchCenterY + 8) != null)
                { claimed.Add((x, PatchCenterY + 8)); break; }

            // ── Beetle-jars: mobile light, mid-field.
            foreach (var (jx, jy) in new[] { (16, 9), (62, 9), (16, 15), (62, 15) })
                if (IsOpenGround(zone, jx, jy)
                    && BuilderSpawn.TryPlaceOnce(zone, factory, "BeetleJar", jx, jy) != null)
                    claimed.Add((jx, jy));

            // ── The threshold: two mats flanking the way out. WALKABLE
            //    on purpose — being stepped on is the entire mechanic.
            // Cold-eye 🟡: the mats used to be two groups with a
            // five-cell gap between them on open floor, so a walker
            // could stroll straight through the "threshold" and never
            // be announced. One contiguous span now, laid ACROSS the
            // line between the door and the hearth: anyone walking the
            // arrival→glow axis crosses the dew and is announced. A
            // deliberate skulker can still circle wide of it — the dew
            // is a doorbell, not a moat, and sneaking around it is a
            // choice the village would notice in other ways.
            int mats = 0;
            int midX = (arrival.x + PatchCenterX) / 2;
            for (int mx = midX - 2; mx <= midX + 2; mx++)
                for (int my = PatchCenterY - 3; my <= PatchCenterY + 3; my++)
                {
                    if (!IsOpenGround(zone, mx, my)) continue;
                    if (BuilderSpawn.TryPlaceOnce(zone, factory, "PebbleSundewThreshold", mx, my) != null)
                    { mats++; claimed.Add((mx, my)); }
                }

            // ── The people. Off the patch-to-door axis, so the glow is
            //    what the player meets first. The Warden stands where
            //    the entry light falls; the Tender is at her wall.
            PlaceVillager(zone, factory, "CatacombWarden", midX + 3, PatchCenterY + 4, claimed);
            PlaceVillager(zone, factory, "PlaqueTender", PatchCenterX, 4, claimed);

            // ── Claim the chamber so later spawners keep out of it.
            int reserved = 0;
            for (int x = 6; x <= 73; x++)
                for (int y = 1; y <= 23; y++)
                    if (zone.GenReservedCells.Add((x, y))) reserved++;

            if (Diag.IsChannelEnabled("worldgen"))
                Diag.Record("worldgen", "FloorArchetype", null, null,
                    new { zoneId = zone.ZoneID, archetype = "StrandedSettlement",
                          patch, ring, niches, wall, mats, reserved });
            return true;
        }

        private static void PlaceVillager(Zone zone, EntityFactory factory,
            string blueprint, int x, int y, List<(int x, int y)> claimed)
        {
            for (int r = 0; r < 10; r++)
                for (int dx = -r; dx <= r; dx++)
                    for (int dy = -r; dy <= r; dy++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (!IsOpenGround(zone, nx, ny)) continue;
                        if (BuilderSpawn.TryPlaceOnce(zone, factory, blueprint, nx, ny) != null)
                        { claimed.Add((nx, ny)); return; }
                    }
        }
    }
}
