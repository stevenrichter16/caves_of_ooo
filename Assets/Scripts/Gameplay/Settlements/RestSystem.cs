using System;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Core.Inventory;

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
            => TryRestFor(actor, zone, site, RestClockTurns, out blockReason, null);

        internal static bool TryRestWithTransaction(Entity actor, Zone zone, string site, out string blockReason, InventoryTransaction transaction)
            => TryRestFor(actor, zone, site, RestClockTurns, out blockReason, transaction);

        /// <summary>Rest to the next 300-tick boundary, including a full band
        /// when already on a boundary. Retains the ordinary hostile/heal rules.</summary>
        public static bool TryRestUntilNextBand(Entity actor, Zone zone, string site, out string blockReason)
            => TryRestUntilNextBandWithTransaction(actor, zone, site, out blockReason, null);

        internal static bool TryRestUntilNextBandWithTransaction(Entity actor, Zone zone, string site, out string blockReason, InventoryTransaction transaction)
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
            return TryRestFor(actor, zone, site, advance, out blockReason, transaction);
        }

        private static bool TryRestFor(Entity actor, Zone zone, string site, int clockAdvance, out string blockReason, InventoryTransaction transaction)
        {
            blockReason = null;
            if (actor != null && (CombatSystem.IsDeathHandled(actor) || (actor.GetStat("Hitpoints") is Stat hpBefore && hpBefore.Value <= 0)))
            {
                blockReason = "actor is dead";
                MessageLog.Add("You cannot rest while dead.");
                Diag.Record("furniture", "RestBlocked", actor: actor,
                    payload: new { site, reason = "actor_dead" });
                return false;
            }
            var clock = TurnManager.Active;
            if (!WorldResourceActions.ActorCurrent(actor, zone) || clock == null || clock.TickCount < 0
                || (long)clock.TickCount + clockAdvance > int.MaxValue)
            {
                blockReason = "no valid resting place or clock";
                MessageLog.Add("You cannot settle down here right now.");
                Diag.Record("furniture", "RestBlocked", actor: actor,
                    payload: new { site, reason = "invalid_rest_context" });
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
            void Heal()
            {
                var hp = actor.GetStat("Hitpoints");
                if (hp != null) { int before = hp.Value; hp.BaseValue = hp.Max; healed = hp.Value - before; }
            }
            void CureBleeding() => actor.GetPart<StatusEffectsPart>()?.RemoveEffect<BleedingEffect>();
            void Report()
            {
                MessageLog.Add(healed > 0
                    ? $"You rest by the {site}. ({healed} HP restored; time passes.)"
                    : $"You rest by the {site}. Time passes.");
                Diag.Record("furniture", "Rested", actor: actor,
                    payload: new { site, healed, clockAdvanced = clockAdvance });
            }
            if (transaction == null)
            {
                // Direct world/conversation callers retain their synchronous contract.
                // PlayerBedService owns its reservation across these callbacks.
                Heal(); CureBleeding(); clock.AdvanceClock(clockAdvance); Report();
                return true;
            }
            if (!transaction.TryClaim(actor, actor, "Rest"))
            {
                blockReason = "another action is already in progress";
                Diag.Record("furniture", "RestBlocked", actor: actor,
                    payload: new { site, reason = "rest_in_progress" });
                return false;
            }
            // An outer inventory action may still refuse or throw. Publish its rest
            // only after payment commits. Separate observers keep a removal listener
            // from suppressing the already-committed clock advance or final receipt.
            bool benefitsAllowed = false;
            transaction.AfterCommit(() =>
            {
                // The outer action may have killed the actor after admission. One
                // commit-time snapshot keeps every benefit and receipt consistent;
                // the accepted rest still pays its already-committed world time.
                benefitsAllowed = !CombatSystem.IsDeathHandled(actor)
                    && (!(actor.GetStat("Hitpoints") is Stat committedHp) || committedHp.Value > 0);
                clock.AdvanceClock(clockAdvance);
            });
            transaction.AfterCommit(() => { if (benefitsAllowed) Heal(); });
            transaction.AfterCommit(() => { if (benefitsAllowed) CureBleeding(); });
            transaction.AfterCommit(() =>
            {
                if (benefitsAllowed) { Report(); return; }
                MessageLog.Add("Your rest is interrupted. Time passes.");
                Diag.Record("furniture", "RestInterrupted", actor: actor,
                    payload: new { site, reason = "actor_dead", clockAdvanced = clockAdvance });
            });
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
                if (SpatialQuery.Distance(zone, actor, other) <= HostileScanRadius)
                    return other;
            }
            return null;
        }
    }
}
