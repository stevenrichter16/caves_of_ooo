using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Event fired on every <see cref="Entity"/> in a zone when a thrown
    /// item lands on the ground. Parts can handle this to react to a
    /// specific item/thrower combination — e.g., <see cref="AIRetrieverPart"/>
    /// makes a pet fetch its ally's thrown bone.
    ///
    /// Parameters set on the GameEvent:
    /// - <c>"Item"</c> (Entity) — the item that just landed
    /// - <c>"Thrower"</c> (Entity) — who threw it. May be null for
    ///   environmentally-caused drops (none today, but possible future)
    /// - <c>"LandingCell"</c> (Cell) — where the item rests now
    ///
    /// Fired from <see cref="Inventory.Commands.ThrowItemCommand.Execute"/>
    /// only when the thrown item actually lands as a takeable entity on a
    /// cell. The consumed-on-impact branch (thrown tonic applied directly
    /// to a hit target, item destroyed) does NOT fire this event — there's
    /// no landed item to fetch.
    ///
    /// Broadcast semantics (matches <see cref="AIBoredEvent"/> + the
    /// M2.3 BroadcastDeathWitnessed pattern): iterate a SNAPSHOT of
    /// <c>zone.GetAllEntities()</c> rather than the live
    /// <c>GetReadOnlyEntities</c> enumerator, because handlers may push
    /// goals / apply effects that mutate <c>Zone._entityCells</c>.
    /// </summary>
    public static class ItemLandedEvent
    {
        public const string ID = "ItemLanded";

        /// <summary>
        /// Fire the ItemLanded event on every entity currently in the zone.
        /// Passes <paramref name="thrower"/> as the "Thrower" parameter;
        /// null is tolerated.
        /// </summary>
        public static void Broadcast(Zone zone, Entity thrower, Entity item, Cell landingCell)
        {
            if (zone == null || item == null || landingCell == null) return;

            // GetAllEntities returns a freshly-allocated List snapshot, so
            // it's safe to call HandleEvent → brain.PushGoal → any
            // zone-touching side effect inside the loop without
            // invalidating the enumerator.
            List<Entity> snapshot = zone.GetAllEntities();
            for (int i = 0; i < snapshot.Count; i++)
            {
                Entity entity = snapshot[i];
                if (entity == null) continue;
                if (entity == item) continue;          // item doesn't receive its own landing
                if (entity == thrower) continue;       // thrower doesn't react to their own throw

                var e = GameEvent.New(ID);
                e.SetParameter("Item", (object)item);
                e.SetParameter("Thrower", (object)thrower);
                e.SetParameter("LandingCell", (object)landingCell);
                entity.FireEvent(e);
                e.Release();
            }
        }
    }
}
