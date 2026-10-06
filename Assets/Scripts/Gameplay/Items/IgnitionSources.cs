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
            return null;
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
