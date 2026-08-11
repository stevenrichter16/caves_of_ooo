namespace CavesOfOoo.Core
{
    /// <summary>
    /// Terrain that continuously asserts a status onto the tile it
    /// stands on: a brine pool keeps its cell wet, a bed of hot ash
    /// keeps its cell hot, a frost vent keeps its cell cold.
    ///
    /// <para><b>Why this exists.</b> Before it, the tile-state layer
    /// could only be written by abilities — a spell put oil on the
    /// ground, and that was the only way ground ever had a status. So
    /// every reaction in <c>Reactions.json</c> and every propagation
    /// rule was reachable only from something the player had just cast.
    /// The world itself was inert.</para>
    ///
    /// <para>The extracted design conversation
    /// (Docs/STATUS-SYSTEM-MODULAR-LADDER.md §6) names exactly this as
    /// the highest-value next slice — "tile state that shares code with
    /// creature state, plus one residue writer… it would immediately
    /// make the already-built lightning-plus-conductor reaction
    /// reachable from terrain instead of only from creature effects,
    /// which is the greenhouse vignette at the smallest possible
    /// scope."</para>
    ///
    /// <para>That vignette — puddles on the floor, copper pipes
    /// overhead, cast lightning into the water and the arc leaps into
    /// the pipe — needs the puddle to be a real Wet tile that the player
    /// did not create. This part is what makes a room able to explain
    /// itself.</para>
    ///
    /// <para><b>It re-asserts rather than writes once.</b> Coatings
    /// decay every turn; a pool that seeded itself once would dry up and
    /// stop being a pool. Re-asserting each turn is also what makes the
    /// interactions honest in both directions — freeze a brine pool and
    /// it re-wets as it thaws, boil it away with fire and it comes
    /// back, because the pool is still physically there.</para>
    /// </summary>
    public class TileStateSourcePart : Part
    {
        public override string Name => "TileStateSource";

        /// <summary>Liquid coating to keep on the cell — "water",
        /// "oil", "brine". Empty for none.</summary>
        public string Coating = "";

        /// <summary>Turns of coating asserted per refresh. Kept short:
        /// this is a lease the terrain renews, not a permanent stamp, so
        /// destroying the terrain lets the tile dry out on its own.</summary>
        public int CoatingTurns = 3;

        /// <summary>Heat asserted onto the cell each turn (0 = none).
        /// Hot ash and vents use this; it is the same energy an ignition
        /// ability writes, so oil on hot ash catches by the existing
        /// rule rather than a special case.</summary>
        public int Heat = 0;

        /// <summary>Cold asserted onto the cell each turn (0 = none).</summary>
        public int Cold = 0;

        /// <summary>Charge asserted onto the cell each turn (0 = none).
        /// Reserved for live wiring and storm-touched metal; no shipped
        /// blueprint uses it yet.</summary>
        public int Charge = 0;

        /// <summary>
        /// Push this source's status onto <paramref name="zone"/> at
        /// (<paramref name="x"/>, <paramref name="y"/>).
        ///
        /// <para>Called by <c>ZoneTileStateSystem.SeedTerrainSources</c>
        /// at the start of each world resolution, BEFORE reactions run —
        /// so a pool that is wet this turn can be electrified this turn
        /// rather than next.</para>
        /// </summary>
        public void Seed(Zone zone, int x, int y)
        {
            if (zone?.TileState == null) return;

            if (!string.IsNullOrEmpty(Coating) && CoatingTurns > 0)
                zone.TileState.WriteCoating(x, y, Coating, CoatingTurns);

            if (Heat > 0) zone.TileState.AddHeat(x, y, Heat);
            if (Cold > 0) zone.TileState.AddCold(x, y, Cold);
            if (Charge > 0) zone.TileState.AddCharge(x, y, Charge);
        }
    }
}
