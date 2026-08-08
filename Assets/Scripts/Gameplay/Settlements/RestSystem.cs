using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// BIOME-OVERHAUL A4 — the shared rest primitive behind campfires
    /// (free, in the field) and inn rooms (paid, adds WellRested).
    /// User-approved model: INSTANT full heal + the world clock
    /// advancing <see cref="RestClockTurns"/> ticks via
    /// <see cref="TurnManager.AdvanceClock"/> — the same pure counter
    /// bump world-map travel uses, so everything keyed off TickCount
    /// (well relapse timers, trader restocks, crop-growth checks) moves
    /// while you sleep. Resting costs world-time, not nothing.
    /// Blocked while a hostile is within <see cref="HostileScanRadius"/>
    /// (Chebyshev, no LOS — sleeping next to a wall a snapjaw is
    /// circling is still a bad idea).
    /// </summary>
    public static class RestSystem
    {
        public const int RestClockTurns = 60;
        public const int HostileScanRadius = 8;

        /// <summary>
        /// Attempt a rest. On success: heal to full, cure Bleeding,
        /// advance the clock, emit furniture/Rested. On block: emit
        /// furniture/RestBlocked with the reason, mutate nothing.
        /// </summary>
        public static bool TryRest(Entity actor, Zone zone, string site, out string blockReason)
        {
            blockReason = null;
            if (actor == null)
            {
                blockReason = "no actor";
                return false;
            }

            var hostile = FindNearbyHostile(actor, zone);
            if (hostile != null)
            {
                blockReason = $"{hostile.GetDisplayName()} is too close";
                MessageLog.Add($"You can't rest — {hostile.GetDisplayName()} is nearby.");
                if (Diag.IsChannelEnabled("furniture"))
                {
                    Diag.Record(category: "furniture", kind: "RestBlocked",
                        actor: actor,
                        payload: new { site, reason = "hostile_nearby", hostile = hostile.BlueprintName });
                }
                return false;
            }

            int healed = 0;
            var hp = actor.GetStat("Hitpoints");
            if (hp != null)
            {
                int before = hp.Value;
                hp.BaseValue = hp.Max;
                healed = hp.Value - before;
            }

            actor.GetPart<StatusEffectsPart>()?.RemoveEffect<BleedingEffect>();

            TurnManager.Active?.AdvanceClock(RestClockTurns);

            MessageLog.Add(healed > 0
                ? $"You rest by the {site}. ({healed} HP restored; time passes.)"
                : $"You rest by the {site}. Time passes.");

            if (Diag.IsChannelEnabled("furniture"))
            {
                Diag.Record(category: "furniture", kind: "Rested",
                    actor: actor,
                    payload: new { site, healed, clockAdvanced = RestClockTurns });
            }
            return true;
        }

        private static Entity FindNearbyHostile(Entity actor, Zone zone)
        {
            if (zone == null) return null;
            var actorCell = zone.GetEntityCell(actor);
            if (actorCell == null) return null;

            var entities = zone.GetAllEntities();
            for (int i = 0; i < entities.Count; i++)
            {
                var other = entities[i];
                if (other == actor || other.GetPart<BrainPart>() == null) continue;
                if (!FactionManager.IsHostile(other, actor)) continue;

                var cell = zone.GetEntityCell(other);
                if (cell == null) continue;
                int dx = cell.X - actorCell.X; if (dx < 0) dx = -dx;
                int dy = cell.Y - actorCell.Y; if (dy < 0) dy = -dy;
                if ((dx > dy ? dx : dy) <= HostileScanRadius)
                    return other;
            }
            return null;
        }
    }
}
