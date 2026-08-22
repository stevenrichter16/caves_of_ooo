using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Wx optimization review §5 — the reachability toolkit every
    /// formation builder needs, in one place.
    ///
    /// <para>Each biome's formation builder grew its own private copy of
    /// these primitives, one per phase (W1 Spread, W2 Beating, W3
    /// Sodden, W4 Grovelands): four byte-identical <c>IsOpenGround</c>,
    /// three identical <c>FloodFromWest</c>, three identical
    /// <c>FullyReached</c>, three identical <c>ClearFor</c>. The review
    /// flagged the obvious consequence — the next biome would copy them
    /// a fifth time — and W5's Mouth and Descent formations are that
    /// fifth and sixth. So they land here first.</para>
    ///
    /// <para><b>What deliberately did NOT move:</b> the repair loops
    /// (<c>EnsureCrossable</c> in Spread/Beating, <c>EnsureAllReachable</c>
    /// in Sodden/Grovelands). They look like siblings but have DRIFTED
    /// into different contracts — Grovelands breaches an entity that
    /// merely touches a pocket when nothing borders both sides, Sodden
    /// requires both and prunes stale entries as it scans, and the two
    /// iterate their placed-lists in opposite directions. Iteration
    /// order picks WHICH entity gets removed, which is observable in
    /// pinned zone output, so unifying them is a behavior change wearing
    /// a refactor's clothes. They stay per-builder and now call these
    /// shared primitives. If a future phase wants one repair loop, that
    /// is its own RED-first change with the formation pins re-run.</para>
    /// </summary>
    public static class FormationReachability
    {
        /// <summary>Walkable, and inside the border ring. The border is
        /// excluded on purpose: zone edges are transition seams, not
        /// playfield, and a formation that reasons about them produces
        /// pockets at the join.</summary>
        public static bool IsOpenGround(Zone zone, int x, int y)
        {
            if (x < 1 || y < 1 || x >= Zone.Width - 1 || y >= Zone.Height - 1) return false;
            var cell = zone.GetCell(x, y);
            return cell != null && !cell.BlocksMovement();
        }

        /// <summary>Clear a cell of everything that blocks movement —
        /// used where a formation's identity requires open ground (a
        /// road, a lane, a causeway). Note the (y, x) parameter order:
        /// preserved from the shipped copies so call sites move
        /// unchanged.</summary>
        public static void ClearFor(Zone zone, int y, int x)
        {
            var cell = zone.GetCell(x, y);
            if (cell == null) return;
            for (int i = cell.Objects.Count - 1; i >= 0; i--)
            {
                var e = cell.Objects[i];
                if (e == null) continue;
                var physics = e.GetPart<PhysicsPart>();
                bool solid = e.HasTag("Solid") || (physics != null && physics.Solid);
                if (solid) zone.RemoveEntity(e);
            }
        }

        /// <summary>8-connected flood from the western border inward.
        /// <paramref name="crossed"/> reports whether the flood reached
        /// the eastern side — the "can something walk across this zone"
        /// question the Spread and Beating repair loops ask.</summary>
        public static bool[,] FloodFromWest(Zone zone, out bool crossed)
        {
            var seen = new bool[Zone.Width, Zone.Height];
            var queue = new Queue<(int x, int y)>();
            crossed = false;

            for (int y = 1; y < Zone.Height - 1; y++)
                if (IsOpenGround(zone, 1, y)) { seen[1, y] = true; queue.Enqueue((1, y)); }

            while (queue.Count > 0)
            {
                var (x, y) = queue.Dequeue();
                if (x >= Zone.Width - 2) crossed = true;
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (nx < 1 || ny < 1 || nx >= Zone.Width - 1 || ny >= Zone.Height - 1) continue;
                        if (seen[nx, ny] || !IsOpenGround(zone, nx, ny)) continue;
                        seen[nx, ny] = true;
                        queue.Enqueue((nx, ny));
                    }
            }
            return seen;
        }

        /// <summary>The same flood when the caller only wants the map.</summary>
        public static bool[,] FloodFromWest(Zone zone) => FloodFromWest(zone, out _);

        /// <summary>True when every open cell in the zone was reached —
        /// i.e. the formation left no walled-off pocket.</summary>
        public static bool FullyReached(Zone zone, bool[,] reached)
        {
            for (int x = 1; x < Zone.Width - 1; x++)
                for (int y = 1; y < Zone.Height - 1; y++)
                    if (IsOpenGround(zone, x, y) && !reached[x, y]) return false;
            return true;
        }
    }
}
