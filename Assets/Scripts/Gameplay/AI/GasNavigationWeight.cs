namespace CavesOfOoo.Core
{
    /// <summary>
    /// G.11 — gas-avoidance navigation weight. Computes the extra A* step
    /// cost for an actor entering a cell that holds gas it isn't immune
    /// to, so <see cref="FindPath"/> routes smart creatures AROUND gas
    /// clouds. CoO adaptation of Qud's <c>GetNavigationWeightEvent</c>
    /// gas weighting (GasConfusion.cs:30-66): density-scaled, immunity-
    /// aware, capped.
    ///
    /// <para><b>Why a direct helper, not an event</b> (Qud-divergence):
    /// Qud fires a per-cell nav-weight event and caches the result
    /// (<c>E.Uncacheable</c>). CoO has no nav-weight cache, and A* expands
    /// many nodes per search — firing+allocating a GameEvent per node
    /// would violate PERF-FOUNDATION's no-allocations-in-hot-paths rule.
    /// This helper is a pure, alloc-free method call instead.</para>
    ///
    /// <para><b>Soft cost, never a wall.</b> The penalty is finite and
    /// capped at <see cref="MAX_PENALTY"/>, so a creature avoids gas when
    /// a clear detour exists but still traverses it when it's the only
    /// route to the goal (gas is not impassable).</para>
    /// </summary>
    public static class GasNavigationWeight
    {
        /// <summary>Flat avoidance for any harmful gas the actor isn't
        /// immune to — even a thin cloud is mildly avoided (Qud's
        /// "+20" base, GasConfusion.cs:40).</summary>
        public const int BASE_PENALTY = 15;

        /// <summary>Density contribution: <c>Density / DENSITY_DIVISOR</c>
        /// added to the base. density 100 → +25.</summary>
        public const int DENSITY_DIVISOR = 4;

        /// <summary>Cap so a dense cloud costs ~100 (vs 10 for a clear
        /// cardinal step) — strong avoidance, but finite so forced
        /// traversal of a gas-only route still works.</summary>
        public const int MAX_PENALTY = 90;

        /// <summary>Extra A* step cost for <paramref name="actor"/> entering
        /// <paramref name="cell"/>. The MAX over the cell's gases of
        /// <c>BASE_PENALTY + Density/DENSITY_DIVISOR</c> (capped at
        /// <see cref="MAX_PENALTY"/>), skipping any gas whose GasType the
        /// actor is immune to. 0 for null cell/actor, no gas, or full
        /// immunity. Alloc-free (no LINQ, no event) — safe for the A*
        /// hot path.</summary>
        public static int ForCell(Cell cell, Entity actor)
        {
            if (cell == null || actor == null) return 0;
            int max = 0;
            var objs = cell.Objects;
            for (int i = 0; i < objs.Count; i++)
            {
                var pool = objs[i].GetPart<GasPoolPart>();
                if (pool == null || pool.Density <= 0) continue;
                if (IsImmune(actor, pool.GasType)) continue;
                int w = BASE_PENALTY + pool.Density / DENSITY_DIVISOR;
                if (w > MAX_PENALTY) w = MAX_PENALTY;
                if (w > max) max = w;
            }
            return max;
        }

        /// <summary>True if the actor carries a <see cref="GasImmunityPart"/>
        /// matching <paramref name="gasType"/>. Iterates all Parts so
        /// multi-immunity creatures are handled.
        ///
        /// <para><b>Case-SENSITIVE</b> (<c>Ordinal</c>) — MUST match
        /// <see cref="GasImmunityPart.HandleEvent"/>'s case-sensitive
        /// <c>==</c> (GasImmunityPart.cs:42), or a case-mismatched immunity
        /// string makes a creature nav-immune (won't path around the gas)
        /// while still being dosed by ApplyGas — it would walk into gas it
        /// isn't protected from. Surfaced by GasCrossPhaseAuditTests.H9.
        /// (If immunity matching ever goes case-insensitive, change BOTH
        /// here AND GasImmunityPart together.)</para></summary>
        private static bool IsImmune(Entity actor, string gasType)
        {
            if (string.IsNullOrEmpty(gasType)) return false;
            var parts = actor.Parts;
            for (int i = 0; i < parts.Count; i++)
                if (parts[i] is GasImmunityPart imm
                    && !string.IsNullOrEmpty(imm.GasType)
                    && string.Equals(imm.GasType, gasType, System.StringComparison.Ordinal))
                    return true;
            return false;
        }
    }
}
