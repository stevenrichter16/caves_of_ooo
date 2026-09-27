using System;
using System.Collections.Generic;
namespace CavesOfOoo.Core
{
    /// <summary>Hot SteamEffect contact only. Tile clouds and the separate
    /// SteamCloud lifespan visual have no inferred temperature or damage.
    /// Pure admission/distance queries are shared by pulses, readout and AI.</summary>
    internal static class SteamContact
    {
        // This set is only the current synchronous pulse stack, not a dedup
        // cache or saved state. Remove in finally; it holds no source between
        // calls. Keying the owner survives replacement of its effect instance.
        [ThreadStatic] private static HashSet<Entity> _activeSources;
        internal static bool TryBeginPulse(Entity source)
        {
            if (source == null) return false;
            if (_activeSources == null) _activeSources = new HashSet<Entity>();
            return _activeSources.Add(source);
        }
        internal static void EndPulse(Entity source) => _activeSources?.Remove(source);

        internal const float ScaldTemperature = 100f;
        internal const int DamagePerPulse = 2;
        internal const int NavigationPenalty = TerrainNavigationWeight.BasePenalty + DamagePerPulse * 6;

        internal static string RejectionReason(Entity source, SteamEffect steam, Zone zone)
        {
            if (zone == null) return "no-zone";
            if (source == null || zone.GetEntityCell(source) == null) return "detached-source";
            return SourceRejectionReason(source, steam);
        }

        // Descriptions can also inspect a detached item: this describes its
        // current steam temperature, not a claim of a placed hazard footprint.
        internal static string SourceRejectionReason(Entity source, SteamEffect steam)
        {
            if (source == null || steam == null || steam.Owner != source
                || source.GetPart<StatusEffectsPart>()?.ParentEntity != source
                || source.GetEffect<SteamEffect>() != steam) return "stale-steam-owner";
            if (steam.Duration == 0 || steam.Duration < Effect.DURATION_INDEFINITE) return "expired-steam";
            if (float.IsNaN(steam.Density) || float.IsInfinity(steam.Density) || steam.Density <= 0f) return "invalid-density";
            if (source.HasTag("_DeathHandled") || (source.HasTag("Creature")
                && source.GetStat("Hitpoints") != null && source.GetStat("Hitpoints").BaseValue <= 0)) return "dead-source";
            var thermal = source.GetPart<ThermalPart>();
            if (thermal == null || thermal.ParentEntity != source) return "missing-thermal";
            if (float.IsNaN(thermal.Temperature) || float.IsInfinity(thermal.Temperature)) return "nonfinite-temperature";
            return thermal.Temperature > ScaldTemperature ? null : "cool-steam";
        }

        internal static bool IsLivingCreature(Entity actor) => actor != null
            && actor.HasTag("Creature") && !actor.HasTag("_DeathHandled")
            && actor.GetStat("Hitpoints")?.BaseValue > 0;

        internal static bool Touches(Zone zone, Entity source, Entity actor) => actor != source
            && IsLivingCreature(actor) && zone?.GetEntityCell(actor) != null
            && SpatialQuery.Distance(zone, source, actor) <= 1;

        internal static int ForCell(Cell cell, Entity actor)
        {
            if (cell?.ParentZone == null || !IsLivingCreature(actor)
                || actor.GetStatValue("HeatResistance") >= 100) return 0;
            var zone = cell.ParentZone;
            // A source's physical foot may be here or in the immediate perimeter;
            // its anchor can be elsewhere. No all-zone scan or query-side effects.
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    var nearby = zone.GetCell(cell.X + dx, cell.Y + dy);
                    if (nearby == null) continue;
                    foreach (var source in nearby.Occupants)
                    {
                        if (source == actor || source == null) continue;
                        var steam = source.GetEffect<SteamEffect>();
                        if (RejectionReason(source, steam, zone) == null
                            && SpatialQuery.DistanceToCell(zone, source, cell.X, cell.Y) <= 1)
                            return NavigationPenalty;
                    }
                }
            return 0;
        }
    }
}
