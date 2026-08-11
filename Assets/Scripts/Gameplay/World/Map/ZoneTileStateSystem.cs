using System.Collections.Generic;
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

            // Terrain asserts its own status BEFORE anything reacts, so
            // a brine pool can be electrified on the same turn the bolt
            // lands rather than the turn after.
            SeedTerrainSources(zone);

            // P3: react BEFORE decaying. A coating on its last turn
            // should still get its chance — otherwise a puddle you
            // charged on the turn it expired would silently do nothing.
            ResolveWorld(zone);

            zone.TileState.Tick();
        }

        /// <summary>
        /// Let every <see cref="TileStateSourcePart"/> in the zone push
        /// its status onto the cell it occupies.
        ///
        /// <para>This is what makes the world's own terrain part of the
        /// status system: without it, tile state only ever existed where
        /// the player had just cast something.</para>
        ///
        /// <para>Iterates <c>GetAllEntities</c>, not the live key
        /// collection — seeding writes to TileState, and the read-only
        /// view is documented as unsafe to iterate while mutating
        /// (Zone.cs:141, and the same trap is called out in
        /// CLAUDE.md).</para>
        /// </summary>
        public static void SeedTerrainSources(Zone zone)
        {
            if (zone?.TileState == null) return;

            var entities = zone.GetAllEntities();
            for (int i = 0; i < entities.Count; i++)
            {
                Entity e = entities[i];
                var source = e?.GetPart<TileStateSourcePart>();
                if (source == null) continue;

                (int x, int y) = zone.GetEntityPosition(e);
                if (x < 0 || y < 0) continue;

                source.Seed(zone, x, y);
            }
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

        /// <summary>
        /// A fire ability touching the ground. Writes heat and resolves
        /// immediately, so oil lying there ignites ON THE CAST.
        ///
        /// <para><b>Why this exists.</b> Every fire ability in the game
        /// applied heat to CREATURES — an <c>ApplyHeat</c> event on the
        /// entity — and wrote nothing to the tile layer. Tile oil was
        /// therefore invisible to all of them, including the new
        /// Pyromancy powers: a flamethrower could not light an oil
        /// slick. Reported from play, 2026-08-09.</para>
        ///
        /// <para>Every fire ability routes through here rather than
        /// writing heat itself, so a sixth one cannot quietly forget.</para>
        /// </summary>
        public static void ApplyFireToTile(Zone zone, int x, int y,
            Entity source = null, string ability = "")
        {
            if (zone == null) return;
            AddHeat(zone, x, y, 1, source, ability);
        }

        /// <summary>Fire across several cells, resolved once at the end
        /// rather than per cell — a slick should go up as one event, not
        /// as a cascade of separate ignitions.</summary>
        public static void ApplyFireToTiles(Zone zone, IEnumerable<Point> cells,
            Entity source = null, string ability = "")
        {
            if (zone == null || cells == null) return;
            foreach (var c in cells)
                AddHeat(zone, c.X, c.Y, 1, source, ability);
            ResolveAfterAbility(zone, source);
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
