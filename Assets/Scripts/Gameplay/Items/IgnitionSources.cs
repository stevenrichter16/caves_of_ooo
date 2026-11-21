using System.Linq;
namespace CavesOfOoo.Core
{
    internal static class IgnitionSources
    {
        internal static Entity Find(Entity actor, Zone zone, Entity exclude)
        {
            if (!WorldResourceActions.ActorCurrent(actor, zone)) return null;
            foreach (var source in zone.GetReadOnlyEntities())
                if (source != exclude && WorldResourceActions.Ground(source, zone) && SpatialQuery.Distance(zone, actor, source) <= 1 && Hot(source)) return source;
            var pack = actor.GetPart<InventoryPart>(); if (pack == null) return null;
            foreach (var source in pack.EquippedItems.Values.Distinct())
            {
                var physics = source?.GetPart<PhysicsPart>();
                if (source == exclude || source == null || source.SpatialZone != null || physics?.ParentEntity != source
                    || physics.Equipped != actor || physics.InInventory != null || pack.Objects.Contains(source)
                    || actor.HasPart<Body>() && pack.FindEquippedBodyPart(source) == null
                    || source.GetPart<TorchLightPart>()?.ParentEntity != source || source.GetPart<LightSourcePart>()?.Enabled != true
                    || (source.GetPart<StackerPart>()?.StackCount ?? 1) != 1) continue;
                if (Hot(source)) return source;
            }
            // No flame in reach: a carried flint striker can make one. Real fires always win above, so a strike is never spent while a fire burns beside the actor. The most worn
            // usable striker is spent first (ties by ID), so a pack of them is used up in a stable order.
            Entity striker = null; int least = int.MaxValue;
            foreach (var item in pack.Objects)
            {
                if (item == exclude || !FlintStrikerPart.Usable(actor, item)) continue;
                int strikes = item.GetPart<FlintStrikerPart>().Strikes;
                if (strikes < least || (strikes == least && striker != null && string.CompareOrdinal(item.ID, striker.ID) < 0)) { striker = item; least = strikes; }
            }
            return striker;
        }

        /// <summary>
        /// Pays for the ignition source <see cref="Find"/> returned, inside the caller's transaction after the caller has claimed it. A burning fire costs nothing; a flint striker
        /// spends one strike, undone with the transaction. False means the source can no longer ignite and the caller must refuse.
        /// </summary>
        internal static bool Consume(Entity actor, Entity source, CavesOfOoo.Core.Inventory.InventoryTransaction tx, string use)
        {
            var striker = source?.GetPart<FlintStrikerPart>();
            return striker == null || striker.Spend(actor, tx, use);
        }
        /// <summary>Every burning fire standing on the ground beside (or under) the actor: the places a torch could be lit at. Looks only at the cells around the actor, so it costs the same in any size of zone.</summary>
        internal static System.Collections.Generic.List<Entity> GroundFiresBeside(Entity actor, Zone zone, Entity exclude)
        {
            var found = new System.Collections.Generic.List<Entity>(); var at = zone == null || actor == null ? null : zone.GetEntityCell(actor);
            if (at == null || !WorldResourceActions.ActorCurrent(actor, zone)) return found;
            for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
            {
                var cell = zone.GetCell(at.X + dx, at.Y + dy); if (cell == null) continue;
                foreach (var source in cell.Occupants)
                    if (source != null && source != exclude && !found.Contains(source) && WorldResourceActions.Ground(source, zone) && SpatialQuery.Distance(zone, actor, source) <= 1 && Hot(source)) found.Add(source);
            }
            return found;
        }

        static bool Hot(Entity source)
        {
            var heat = source.GetPart<ThermalPart>(); var fuel = source.GetPart<FuelPart>();
            return heat?.ParentEntity == source && WorldResourceActions.Finite(heat.Temperature) && heat.IsAflame
                && !(source.GetEffect<FrozenEffect>()?.Cold > 0) && !(source.GetEffect<WetEffect>()?.Moisture > 0.35f)
                && (fuel == null || fuel.ParentEntity == source && WorldResourceActions.Finite(fuel.FuelMass) && fuel.FuelMass > 0
                    && WorldResourceActions.Finite(fuel.BurnRate) && fuel.BurnRate > 0)
                && (!source.HasPart<TorchLightPart>() || source.GetPart<LightSourcePart>()?.Enabled == true);
        }
    }
}
