using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// PALIMPSEST P2 — the seam between abilities and the tile layer.
    ///
    /// <para>Every write goes through here rather than touching
    /// <see cref="ZoneTileState"/> directly, for one reason: the
    /// <c>tile/TileWritten</c> diag record has to fire uniformly.
    /// Emitting from each ability instead would guarantee drift across
    /// call sites the first time someone adds a fifth one.</para>
    ///
    /// <para>Static and free of MonoBehaviour, matching
    /// <c>MaterialSimSystem</c> / <c>GasSystem</c> / <c>CropSystem</c>,
    /// so the whole thing is EditMode-testable.</para>
    /// </summary>
    public static class ZoneTileStateSystem
    {
        /// <summary>
        /// One decay step. Called once per PLAYER turn from
        /// <c>InputHandler.EndTurnAndProcess</c>.
        ///
        /// <para><b>Not TickEnd.</b> <c>TurnManager.EndTurn</c> fires
        /// TickEnd once per ACTOR, so a 6-turn coating would evaporate
        /// in about one player turn in a crowded zone and last the full
        /// six in an empty one — duration would become a function of
        /// local population. Palimpsest durations are authored in player
        /// turns, so they are decremented in player turns.</para>
        /// </summary>
        public static void OnPlayerTurnEnd(Zone zone)
        {
            if (zone == null) return;

            // P3: react BEFORE decaying. A coating on its last turn
            // should still get its chance — otherwise a puddle you
            // charged on the turn it expired would silently do nothing.
            ResolveWorld(zone);

            zone.TileState.Tick();
        }

        /// <summary>
        /// Resolves reactions immediately after an ability finishes
        /// writing. Without this, casting lightning into a puddle would
        /// not electrify it until the END of the turn, which reads as a
        /// bug rather than a rule.
        /// </summary>
        public static void ResolveAfterAbility(Zone zone, Entity caster = null)
        {
            if (zone == null) return;
            ResolveWorld(zone, caster);
        }

        /// <summary>
        /// The fixed resolution order (design §26), steps 4-6: local
        /// reactions, then propagation, then ONE secondary reaction pass
        /// over whatever the spread created.
        ///
        /// <para>The secondary pass is what makes the greenhouse work:
        /// charge propagates along the grating into a puddle three
        /// tiles away, and only then does that puddle electrify. Without
        /// it, spread charge would sit inert until the next turn and the
        /// chain would read as broken.</para>
        ///
        /// <para>Exactly one secondary pass, not a loop —
        /// <see cref="TileReactionSystem"/>'s own guards bound the
        /// reactions, and bounding the propagation/reaction alternation
        /// here keeps the whole thing terminating by construction.</para>
        /// </summary>
        public static void ResolveWorld(Zone zone, Entity caster = null)
        {
            if (zone == null) return;

            // PROPAGATE FIRST, then react. The spec's §26 puts local
            // reactions before propagation, and that order is WRONG for
            // this architecture: electrify_water CONSUMES the charge, so
            // reacting first left nothing to spread and the charge never
            // reached the far end of the puddle. Physically the spread
            // comes first anyway — current fills the conductor, and THEN
            // everything standing in it gets shocked at once.
            TilePropagationSystem.PropagateCharge(zone, caster);
            TileReactionSystem.ResolveZone(zone, caster);

            // One more round, for the other direction: a reaction can
            // CREATE something that spreads (oil ignites -> embers ->
            // embers crawl to the next oil). Exactly two rounds, not a
            // loop: TileReactionSystem's own guards bound the reactions,
            // and bounding the alternation here keeps the whole thing
            // terminating by construction.
            int spread = TilePropagationSystem.PropagateFire(zone, caster);
            spread += TilePropagationSystem.PropagateCharge(zone, caster);
            if (spread > 0)
                TileReactionSystem.ResolveZone(zone, caster);
        }

        /// <summary>
        /// Points a zone's change notifications at the renderer. Bound
        /// when a zone becomes the ACTIVE one rather than in the Zone
        /// constructor, so a cached-but-inactive zone can never dirty
        /// cells in the zone actually on screen. Assignment rather than
        /// += keeps re-binding idempotent.
        /// </summary>
        public static void BindRenderHook(Zone zone)
        {
            if (zone == null) return;
            zone.TileState.OnCellChanged =
                (x, y) => ZoneRenderHooks.MarkCellDirty(x, y, "TileState");
        }

        // ── Write wrappers — the only sanctioned path ────────────

        public static void WriteCoating(Zone zone, int x, int y,
            string liquidId, int turns, Entity source = null, string ability = "")
        {
            if (zone == null) return;
            zone.TileState.WriteCoating(x, y, liquidId, turns);
            Emit(zone, x, y, "coating", liquidId, turns, source, ability);
        }

        public static void WriteResidue(Zone zone, int x, int y,
            string residueId, int turns, Entity source = null, string ability = "")
        {
            if (zone == null) return;
            zone.TileState.WriteResidue(x, y, residueId, turns);
            Emit(zone, x, y, "residue", residueId, turns, source, ability);
        }

        public static void AddCharge(Zone zone, int x, int y,
            int amount, Entity source = null, string ability = "")
        {
            if (zone == null) return;
            zone.TileState.AddCharge(x, y, amount);
            Emit(zone, x, y, "energy", "charge", amount, source, ability);
        }

        public static void AddHeat(Zone zone, int x, int y,
            int amount, Entity source = null, string ability = "")
        {
            if (zone == null) return;
            zone.TileState.AddHeat(x, y, amount);
            Emit(zone, x, y, "energy", "heat", amount, source, ability);
        }

        public static void AddCold(Zone zone, int x, int y,
            int amount, Entity source = null, string ability = "")
        {
            if (zone == null) return;
            zone.TileState.AddCold(x, y, amount);
            Emit(zone, x, y, "energy", "cold", amount, source, ability);
        }

        private static void Emit(Zone zone, int x, int y, string layer,
            string id, int magnitude, Entity source, string ability)
        {
            if (!Diag.IsChannelEnabled("tile")) return;
            Diag.Record(
                category: "tile", kind: "TileWritten",
                actor: source, target: source,
                payload: new
                {
                    x, y, layer, id,
                    magnitude,
                    ability = ability ?? "",
                    writtenTiles = zone.TileState.WrittenCount,
                });
        }
    }
}
