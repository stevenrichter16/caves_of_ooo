using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// The <c>Slippery</c> flag's one consumer: Qud's slide
    /// (<c>BaseLiquid.ObjectEnteredCell</c>, BaseLiquid.cs:402-423). A
    /// creature that steps onto a slippery liquid — a tile coating, which
    /// is also how pool entities appear on the ground (Zone.ProjectPool)
    /// — may be flung one random cell. Docs/LIQUID-SLIP.md.
    ///
    /// <para><b>Flat chance, no save.</b> Qud rolls an Agility save
    /// against <c>5 + amount − Stable</c>; here the roll is
    /// <c>Next(100) &lt; LiquidDefinition.SlipChance</c> (user decision,
    /// 2026-08-15). When the save arrives it modifies that base in THIS
    /// one line — content rows stay valid.</para>
    ///
    /// <para><b>Called by <see cref="MovementSystem"/> after
    /// <c>FireCellEnteredEvents</c> returns — never from inside an
    /// <c>EntityEnteredCell</c> handler.</b> The slide is a nested move
    /// (<see cref="MovementSystem.ForceMoveTo"/>), resolved after the
    /// original landing's contact recipients have reacted. The body's tile
    /// layer is read here, so <see cref="LiquidPoolPart"/>'s own
    /// entered-cell handler stays a coat-only handler.</para>
    ///
    /// <para><b>The slide chains</b> — the forced move lands somewhere,
    /// and if that is slippery too the roll repeats — but is capped at
    /// <see cref="MAX_SLIDE_CHAIN"/> per originating move. With a flat
    /// chance the cap is load-bearing (a <c>SlipChance:100</c> liquid on
    /// a long sheet would otherwise slide until it hit a wall), not just
    /// defensive.</para>
    /// </summary>
    public static class LiquidSlipSystem
    {
        /// <summary>Slides per originating move. Three: an ice sheet can
        /// carry you, but not across the map.</summary>
        public const int MAX_SLIDE_CHAIN = 3;

        /// <summary>Tests inject a seeded RNG here (the
        /// <c>GasFungalSporesPart</c> pattern). Null ⇒ the shared default.
        /// Reset to null in TearDown.</summary>
        public static System.Random TestRng;
        private static readonly System.Random _defaultRng = new System.Random();

        // The 8 directions, index by rng.Next(8). Static so the pick allocates nothing.
        private static readonly int[] DirX = { 1, -1, 0, 0, 1, 1, -1, -1 };
        private static readonly int[] DirY = { 0, 0, 1, -1, 1, -1, 1, -1 };

        /// <summary>
        /// Resolve a slip for <paramref name="mover"/> which has just
        /// landed in <paramref name="landed"/>. Returns true if a slip
        /// occurred (whether or not it displaced anything).
        /// </summary>
        /// <param name="chainDepth">Slides already taken in this
        /// originating move; ≥ <see cref="MAX_SLIDE_CHAIN"/> ⇒ no roll.</param>
        public static bool ResolveAfterMove(Entity mover, Zone zone, Cell landed, int chainDepth = 0)
        {
            if (mover == null || zone == null || landed == null) return false;
            if (chainDepth >= MAX_SLIDE_CHAIN) return false;
            if (!mover.HasTag("Creature")) return false;      // items and props have no feet to lose
            if (!LiquidRegistry.IsInitialized) return false;
            // The mover must actually be standing in `landed`. A cell-entry
            // handler may have killed it (removed from the zone) or moved
            // it (a trap door, a pull); sliding a body that is no longer
            // there would re-add it via MoveEntity. Adversarial:
            // MoverDiesOnLanding_ChainStopsCleanly.
            if (!ReferenceEquals(zone.GetEntityCell(mover), landed)) return false;

            LiquidDefinition def = null;
            // One decision for the whole physical body, including a far foot.
            // An unoccupied anchor/hole contributes no ground contact.
            foreach (var contact in zone.GetOccupiedCells(mover))
            {
                def = FindSlipperyLiquid(zone, contact);
                if (def != null) break;
            }
            if (def == null) return false;                     // the overwhelmingly common early-out

            int chance = Clamp(def.SlipChance, 0, 100);
            var rng = TestRng ?? _defaultRng;
            int roll = rng.Next(100);
            bool slipped = roll < chance;

            if (Diag.IsChannelEnabled("liquid"))
            {
                Diag.Record("liquid", "SlipRolled", mover, null, new
                {
                    liquidId = def.Id,
                    chance,
                    roll,
                    slipped,
                    chainDepth,
                });
            }
            if (!slipped) return false;

            MessageLog.Add(mover.GetDisplayName() + " slips on the " + def.DisplayName + "!");

            // One random direction, one attempt — Qud's Move(GetRandomDirection(), Forced).
            // Illegal destination ⇒ the slip happened, nothing moved.
            int d = rng.Next(DirX.Length);
            int nx = landed.X + DirX[d], ny = landed.Y + DirY[d];
            bool displaced = false;
            if (zone.CanPlaceFootprint(mover, nx, ny) && !BodyHasOtherCreature(zone, mover, nx, ny))
                displaced = MovementSystem.ForceMoveTo(mover, zone, nx, ny, slipChain: chainDepth + 1);

            if (Diag.IsChannelEnabled("liquid"))
            {
                Diag.Record("liquid", "Slipped", mover, null, new
                {
                    liquidId = def.Id,
                    fromX = landed.X,
                    fromY = landed.Y,
                    toX = displaced ? nx : landed.X,
                    toY = displaced ? ny : landed.Y,
                    displaced,
                    chainDepth,
                });
            }
            return true;
        }

        /// <summary>
        /// The slippery liquid on the ground in <paramref name="cell"/>,
        /// or null. Reads ONLY the tile layer: pool entities are already
        /// mirrored into it as permanent coatings by <c>Zone.ProjectPool</c>
        /// (Zone.cs — Palimpsest P2b), so a puddle and a spill are one
        /// question here, and one roll per move — never two. First
        /// slippery coating wins.
        /// </summary>
        public static LiquidDefinition FindSlipperyLiquid(Zone zone, Cell cell)
        {
            if (zone == null || cell == null) return null;
            var state = zone.TileState.Get(cell.X, cell.Y);
            if (state == null) return null;
            for (int i = 0; i < state.Coatings.Count; i++)
            {
                var def = LiquidRegistry.Get(state.Coatings[i].Id);
                if (def != null && def.Slippery) return def;
            }
            return null;
        }

        private static bool BodyHasOtherCreature(Zone zone, Entity mover, int x, int y)
        {
            foreach (var cell in zone.GetOccupiedCells(mover, x, y))
            {
                if (cell == null) continue;
                foreach (var other in cell.Occupants)
                    if (other != null && other != mover && other.HasTag("Creature")) return true;
            }
            return false;
        }

        private static int Clamp(int v, int lo, int hi) => v < lo ? lo : (v > hi ? hi : v);
    }
}
