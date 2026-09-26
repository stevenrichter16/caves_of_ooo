using System;
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
    /// (Chebyshev, no LOS — sleeping next to a wall a marlback is
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
            => TryRestFor(actor, zone, site, RestClockTurns, out blockReason);

        /// <summary>Rest to the next 300-tick boundary, including a full band
        /// when already on a boundary. Retains the ordinary hostile/heal rules.</summary>
        public static bool TryRestUntilNextBand(Entity actor, Zone zone, string site, out string blockReason)
        {
            var clock = TurnManager.Active;
            int tick = clock?.TickCount ?? -1;
            int advance = WorldClock.BandLengthTicks - Math.Max(0, tick) % WorldClock.BandLengthTicks;
            if (actor == null || zone?.GetEntityCell(actor) == null || clock == null || tick < 0
                || (long)tick + advance > int.MaxValue)
            {
                blockReason = "no valid resting place or clock";
                MessageLog.Add("You cannot settle down here right now.");
                Diag.Record("furniture", "RestBlocked", actor: actor,
                    payload: new { site, reason = "invalid_rest_context" });
                return false;
            }
            return TryRestFor(actor, zone, site, advance, out blockReason);
        }

        private static bool TryRestFor(Entity actor, Zone zone, string site, int clockAdvance, out string blockReason)
        {
            blockReason = null;
            if (actor == null)
            {
                blockReason = "no actor";
                return false;
            }

            if (CombatSystem.IsDeathHandled(actor) || (actor.GetStat("Hitpoints") is Stat hpBefore && hpBefore.Value <= 0))
            {
                blockReason = "actor is dead";
                MessageLog.Add("You cannot rest while dead.");
                Diag.Record("furniture", "RestBlocked", actor: actor,
                    payload: new { site, reason = "actor_dead" });
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

            TurnManager.Active?.AdvanceClock(clockAdvance);

            MessageLog.Add(healed > 0
                ? $"You rest by the {site}. ({healed} HP restored; time passes.)"
                : $"You rest by the {site}. Time passes.");

            if (Diag.IsChannelEnabled("furniture"))
            {
                Diag.Record(category: "furniture", kind: "Rested",
                    actor: actor,
                    payload: new { site, healed, clockAdvanced = clockAdvance });
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
