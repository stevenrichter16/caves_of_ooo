using CavesOfOoo.Diagnostics;

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
            var item = e.GetParameter<Entity>("Item");
            var thrower = e.GetParameter<Entity>("Thrower");
            var brain = ParentEntity?.GetPart<BrainPart>();
            var zone = brain?.CurrentZone;
            if (brain == null || brain.ParentEntity != ParentEntity || !GoFetchGoal.LiveMember(ParentEntity, zone)
                || ParentEntity.GetPart<InventoryPart>()?.ParentEntity != ParentEntity)
                return Refuse(item, thrower, "actor_unavailable");
            if (brain.HasGoal<GoFetchGoal>()) return Refuse(item, thrower, "already_fetching");
            var landingCell = e.GetParameter<Cell>("LandingCell");
            if (thrower == ParentEntity || thrower == item || !GoFetchGoal.LiveMember(thrower, zone)
                || (AlliesOnly && !FactionManager.IsAllied(ParentEntity, thrower)))
                return Refuse(item, thrower, "recipient_unavailable");
            if (landingCell == null || zone.GetCell(landingCell.X, landingCell.Y) != landingCell
                || !GoFetchGoal.GroundItem(item, zone, landingCell, item?.GetPart<StackerPart>()?.StackCount ?? 1))
                return Refuse(item, thrower, "invalid_landing");
            var myCell = zone.GetEntityCell(ParentEntity);
            if (AIHelpers.ChebyshevDistance(myCell.X, myCell.Y, landingCell.X, landingCell.Y) > NoticeRadius)
                return Refuse(item, thrower, "outside_notice_radius");

            // Return to the actual thrower, not the pet's StartingCell. The goal
            // rechecks the landed source after the throw transaction has committed.
            brain.PushGoal(GoFetchGoal.ForThrow(item, thrower, AlliesOnly));
            Record(item, thrower, null);
            return false;
        }

        private bool Refuse(Entity item, Entity thrower, string reason)
        {
            Record(item, thrower, reason);
            return true;
        }

        private void Record(Entity item, Entity thrower, string reason)
        {
            if (!Diag.IsChannelEnabled("ai")) return;
            Diag.Record("ai", "FetchAdmission", ParentEntity, item, payload: new
            {
                outcome = reason == null ? "accepted" : "refused", reason,
                thrower = thrower?.ID, quantity = item?.GetPart<StackerPart>()?.StackCount ?? 1
            });
        }
    }
}
