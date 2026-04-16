using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Event fired on furniture/interactable objects to ask if they offer idle behavior.
    /// Furniture parts handle this and set TargetX/Y, Action, and Cleanup on the event.
    ///
    /// When a furniture part returns an offer, it is expected to reserve itself
    /// (e.g. ChairPart.Occupied = true) at query time to prevent double-booking.
    /// The Cleanup callback is used to roll back that reservation if the NPC
    /// fails to reach the furniture (e.g. path blocked).
    /// </summary>
    public static class IdleQueryEvent
    {
        public const string ID = "IdleQuery";

        /// <summary>
        /// Query a furniture entity for an idle offer. Returns null if none offered.
        /// </summary>
        public static IdleOffer QueryOffer(Entity furniture, Entity querier)
        {
            if (furniture == null || querier == null) return null;

            var e = GameEvent.New(ID);
            e.SetParameter("Querier", (object)querier);
            furniture.FireEvent(e);

            if (!e.Handled)
            {
                e.Release();
                return null;
            }

            var offer = new IdleOffer
            {
                TargetX = e.GetIntParameter("TargetX", -1),
                TargetY = e.GetIntParameter("TargetY", -1),
                Action = e.GetParameter<Action<GoalHandler>>("Action"),
                Cleanup = e.GetParameter<Action<GoalHandler>>("Cleanup"),
                Source = furniture
            };
            e.Release();
            return offer;
        }
    }

    /// <summary>
    /// Result of an idle query: where to go, what to do on arrival,
    /// and how to roll back reservations if the trip is abandoned.
    /// </summary>
    public class IdleOffer
    {
        public int TargetX;
        public int TargetY;
        /// <summary>Called when the NPC arrives at the furniture.</summary>
        public Action<GoalHandler> Action;
        /// <summary>Called if the NPC never reaches the furniture (rollback reservation).</summary>
        public Action<GoalHandler> Cleanup;
        public Entity Source;
    }
}
