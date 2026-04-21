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
    /// Event consumption (<c>e.Handled = true</c>, <c>return false</c>
    /// on success) stops other parts on the SAME entity from also
    /// reacting to this event — e.g. a creature carrying both
    /// AIRetriever and AIHoarder shouldn't double-dip on the same
    /// thrown item. Each entity receives its own GameEvent instance,
    /// so consumption is scoped to this entity's Parts list, not
    /// across the broadcast.
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

            // Retriever fetches WITHOUT returning home — pet walks to the
            // item's landing cell, picks it up, and stops. It does NOT walk
            // back to the thrower or drop the item. ReturnHome=true would
            // send the pet to its own StartingCell (still not the thrower).
            //
            // TODO(pet-ux): Real "dog fetches bone to owner" UX wants a third
            // mode — walk to an empty cell ADJACENT to the thrower, then
            // DropCommand the fetched item there. Requires:
            //   1. Extend GoFetchGoal with ReturnToEntity (tracks thrower).
            //   2. New Phase.WalkToThrower after Pickup, targeting a
            //      passable cell adjacent to the tracked entity (recompute
            //      each tick in case the thrower moves).
            //   3. Phase.DropAtThrower fires DropCommand on arrival.
            //   4. AIRetrieverPart passes `thrower` from the ItemLandedEvent.
            //   5. Tests: arrival + drop, thrower-moves-during-fetch,
            //      thrower-dies-during-fetch (fall back to ReturnHome).
            // Punted during M3.2 polish — fetch+hoard works, but the loop
            // is "throw once, bone gone forever into dog's inventory."
            brain.PushGoal(new GoFetchGoal(item, returnHome: false));
            return false; // consumed
        }
    }
}
