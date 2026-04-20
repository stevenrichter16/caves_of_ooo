namespace CavesOfOoo.Core
{
    /// <summary>
    /// M3.2 — AI behavior part that reacts to <see cref="ItemLandedEvent"/>
    /// broadcasts by pushing a <see cref="GoFetchGoal"/> on the wearer's
    /// brain. The intended archetype is a pet dog that fetches the thing
    /// its owner just threw: player throws a bone → dog runs to get it
    /// → returns next to the player.
    ///
    /// Gated by:
    /// - <see cref="AlliesOnly"/> — if true (default), only reacts when
    ///   the thrower is faction-allied with the wearer. Prevents the
    ///   dog from fetching an enemy's thrown projectile.
    /// - <see cref="NoticeRadius"/> — the wearer must be within this
    ///   Chebyshev distance of the landing cell for the event to matter.
    ///   Otherwise pets across the zone would teleport-fetch.
    /// - Idempotency: already-on-stack <c>GoFetchGoal</c> blocks a
    ///   second push so multiple simultaneous throws don't stack.
    ///
    /// Differs from <see cref="AIHoarderPart"/>:
    /// - AIHoarder is a SCAN on bored ticks — "wander around, pick up
    ///   shiny things, bring them home." Push happens on the wearer's
    ///   own idle rhythm.
    /// - AIRetriever is REACTIVE — fires only when a specific event
    ///   (ItemLanded) broadcasts, which happens at most once per throw.
    ///   Push happens when the event fires, not on a bored tick.
    ///
    /// On successful push, the event is consumed (<c>e.Handled = true</c>,
    /// <c>return false</c>) so the wearer only reacts to ONE
    /// thrown-item-this-tick even if multiple pets receive the event
    /// — actually, each pet receives its own event, so the per-pet
    /// consumption is about stopping other parts on the SAME entity
    /// from also reacting to the same event (e.g. a combined
    /// Retriever+Hoarder shouldn't double-dip).
    ///
    /// Blueprint attachment:
    ///   { "Name": "AIRetriever", "Params": [
    ///       { "Key": "AlliesOnly", "Value": "true" },
    ///       { "Key": "NoticeRadius", "Value": "8" }
    ///   ]}
    /// </summary>
    public class AIRetrieverPart : AIBehaviorPart
    {
        public override string Name => "AIRetriever";

        /// <summary>
        /// If true, only react when the thrower is faction-allied
        /// (<see cref="FactionManager.IsAllied"/>). Default true so pets
        /// don't run to fetch an enemy's thrown rock.
        /// </summary>
        public bool AlliesOnly = true;

        /// <summary>
        /// Maximum Chebyshev distance from wearer to landing cell for
        /// the event to trigger a fetch. Default 8 = the default sight
        /// radius; prevents cross-zone teleport-fetch.
        /// </summary>
        public int NoticeRadius = 8;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == ItemLandedEvent.ID)
            {
                bool result = HandleItemLanded(e);
                if (!result) e.Handled = true;
                return result;
            }
            return true;
        }

        private bool HandleItemLanded(GameEvent e)
        {
            var brain = ParentEntity.GetPart<BrainPart>();
            if (brain == null || brain.CurrentZone == null)
                return true;

            // Idempotency.
            if (brain.HasGoal("GoFetchGoal"))
                return true;

            // Inventory required — no stow, no fetch.
            if (ParentEntity.GetPart<InventoryPart>() == null)
                return true;

            var item = e.GetParameter<Entity>("Item");
            if (item == null) return true;

            var thrower = e.GetParameter<Entity>("Thrower");
            if (AlliesOnly && thrower != null
                && !FactionManager.IsAllied(ParentEntity, thrower))
            {
                // Different faction + we only fetch for allies.
                return true;
            }
            // Thrower==null (environmental drop) currently unreachable via
            // ThrowItemCommand, but passes the gate if AlliesOnly=false. A
            // future wild-drop source (tornado, item rain) would trigger
            // universal fetch; adjust if that shows up.

            var landingCell = e.GetParameter<Cell>("LandingCell");
            if (landingCell == null) return true;

            var myCell = brain.CurrentZone.GetEntityCell(ParentEntity);
            if (myCell == null) return true;

            int dist = AIHelpers.ChebyshevDistance(
                myCell.X, myCell.Y, landingCell.X, landingCell.Y);
            if (dist > NoticeRadius) return true;

            // Retriever fetches WITHOUT returning home — pet brings the
            // item back to the player's general area, not its own
            // spawn cell.
            brain.PushGoal(new GoFetchGoal(item, returnHome: false));
            return false; // consumed
        }
    }
}
