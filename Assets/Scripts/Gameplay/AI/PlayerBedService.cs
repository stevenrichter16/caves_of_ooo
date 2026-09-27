using System;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Instant player rest on existing ordinary beds. Shares NPC owner
    /// and reservation rules, and the existing rest primitive's clock/healing
    /// behavior. This direct world action cannot join an inventory transaction:
    /// RestSystem has no undo for health, effect callbacks or world time.</summary>
    public static class PlayerBedService
    {
        public const string RestCommand = "SleepOnBed";
        public const string NextBandCommand = "SleepUntilNextBand";

        public static bool TryAct(Entity actor, Entity furniture, Zone zone, string command,
            InventoryTransaction transaction = null)
        {
            if (command != RestCommand && command != NextBandCommand) return false;
            if (transaction != null)
                return Reject(actor, furniture, "outer_transaction", "Use that bed directly in the world.");
            var bed = furniture?.GetPart<BedPart>();
            var physics = furniture?.GetPart<PhysicsPart>();
            var health = actor?.GetStat("Hitpoints");
            if (actor?.HasTag("Player") != true || bed == null || health == null
                || zone?.GetEntityCell(actor) == null || zone.GetEntityCell(furniture) == null)
                return Reject(actor, furniture, "invalid_context", "That bed cannot be used right now.");
            if (CombatSystem.IsDeathHandled(actor) || health.Value <= 0)
                return Reject(actor, furniture, "actor_dead", "You cannot sleep while dead.");
            if (physics == null || physics.Takeable || physics.InInventory != null || physics.Equipped != null)
                return Reject(actor, furniture, "portable_bed", "That is not a bed placed for sleeping.");
            if (SpatialQuery.Distance(zone, actor, furniture) != 0)
                return Reject(actor, furniture, "not_on_bed", "Step onto the bed's tile to sleep.");
            if (!string.IsNullOrEmpty(bed.Owner) && bed.Owner != actor.ID && !actor.HasTag(bed.Owner))
                return Reject(actor, furniture, "owned", "That bed is reserved for someone else.");
            if (bed.Occupied || actor.HasEffect<SittingEffect>())
                return Reject(actor, furniture, "occupied", "That bed is in use, or you must stand up first.");
            foreach (var cell in zone.GetOccupiedCells(furniture))
                foreach (var occupant in cell.Occupants)
                    if (occupant != actor && occupant.HasTag("Creature"))
                        return Reject(actor, furniture, "occupied", "Someone is already on that bed.");
            var clock = TurnManager.Active;
            int tick = clock?.TickCount ?? -1;
            int advance = command == RestCommand ? RestSystem.RestClockTurns
                : WorldClock.BandLengthTicks - Math.Max(0, tick) % WorldClock.BandLengthTicks;
            if (clock == null || tick < 0 || (long)tick + advance > int.MaxValue)
                return Reject(actor, furniture, "invalid_clock", "You cannot settle down here right now.");

            // Keep actor and furniture claims across all synchronous rest callbacks,
            // so another colocated bed cannot rest the same actor reentrantly.
            var claims = new InventoryTransaction();
            try
            {
                if (!claims.TryClaim(actor, actor, command) || !claims.TryClaim(furniture, actor, command))
                    return Reject(actor, furniture, "in_progress", "You are already using that bed.");
                bed.Occupied = true;
                try
                {
                    return command == RestCommand
                        ? RestSystem.TryRest(actor, zone, "bed", out _)
                        : RestSystem.TryRestUntilNextBand(actor, zone, "bed", out _);
                }
                finally { bed.Occupied = false; }
            }
            finally { claims.Rollback(); }
        }

        private static bool Reject(Entity actor, Entity furniture, string reason, string message)
        {
            MessageLog.Add(message);
            Diag.Record("furniture", "PlayerBedRejected", actor, furniture, new { reason });
            return false;
        }
    }
}
