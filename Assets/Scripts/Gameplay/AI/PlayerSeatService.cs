using System;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Player use of ordinary chairs shares NPC ownership/reservations.
    /// Standing on the seat avoids hidden movement, hazard or teleport side effects.</summary>
    public static class PlayerSeatService
    {
        public const string SitCommand = "SitOnChair", StandCommand = "StandFromChair";
        public static bool TryAct(Entity actor, Entity furniture, Zone zone, string command, InventoryTransaction transaction = null)
        {
            if (CombatSystem.IsDeathHandled(actor) || (actor?.GetStat("Hitpoints") is Stat hp && hp.Value <= 0))
                return Reject(actor, furniture, "actor_dead", "You cannot sit while dead.");
            var chair = furniture?.GetPart<ChairPart>();
            var effects = actor?.GetPart<StatusEffectsPart>();
            if (actor?.HasTag("Player") != true || chair == null || effects == null || zone == null
                || zone.GetEntityCell(actor) == null || zone.GetEntityCell(furniture) == null)
                return Reject(actor, furniture, "invalid_context", "That chair cannot be used right now.");
            var seated = effects.GetEffect<SittingEffect>();
            bool standing = command == StandCommand;
            if (command != SitCommand && !standing) return false;
            if (standing)
            {
                if (seated?.Furniture != furniture || chair.Occupant != actor)
                    return Reject(actor, furniture, "not_your_seat", "You are not sitting in that chair.");
            }
            else
            {
                if (SpatialQuery.Distance(zone, actor, furniture) != 0)
                    return Reject(actor, furniture, "not_on_seat", "Step onto the chair's tile to sit.");
                if (!string.IsNullOrEmpty(chair.Owner) && chair.Owner != actor.ID && !actor.HasTag(chair.Owner))
                    return Reject(actor, furniture, "owned", "That chair is reserved for someone else.");
                if (chair.Occupied || chair.Occupant != null || seated != null)
                    return Reject(actor, furniture, "occupied", "That seat is already in use, or you are already sitting.");
            }
            bool own = transaction == null;
            transaction ??= new InventoryTransaction();
            try
            {
                if (!transaction.TryClaim(actor, actor, command) || !transaction.TryClaim(furniture, actor, command))
                    return Reject(actor, furniture, "in_progress", "That seat is already being used.");
                bool beforeOccupied = chair.Occupied;
                Entity beforeOccupant = chair.Occupant;
                var created = standing ? null : new SittingEffect(furniture);
                int index = -1;
                if (standing)
                { var all = effects.GetAllEffects(); for (int i = 0; i < all.Count; i++) if (ReferenceEquals(all[i], seated)) index = i; }
                bool restored = false;
                Action restore = () =>
                {
                    if (restored) return;
                    restored = true;
                    try
                    {
                        if (created != null) effects.RemoveEffect(effect => ReferenceEquals(effect, created));
                        if (seated != null) effects.RestoreRemovedEffectForInventoryUndo(seated, index);
                    }
                    finally { chair.Occupied = beforeOccupied; chair.Occupant = beforeOccupant; }
                };
                transaction.Do(null, restore);
                if (standing) effects.RemoveEffect(effect => ReferenceEquals(effect, seated));
                else
                {
                    chair.Occupied = true; chair.Occupant = actor;
                    if (!actor.ApplyEffect(created, actor, zone))
                    { restore(); return Reject(actor, furniture, "effect_refused", "You cannot sit down right now."); }
                }
                transaction.AfterCommit(() => Diag.Record("furniture", standing ? "PlayerStood" : "PlayerSat", actor, furniture,
                    new { owner = chair.Owner }));
                transaction.AfterCommit(() => MessageLog.Add(standing ? "You stand up." : "You sit in the chair."));
                if (own) transaction.Commit();
                return true;
            }
            finally { if (own) transaction.Rollback(); }
        }

        /// <summary>Called after actual movement, so a blocked move keeps the seat.</summary>
        internal static void AfterMovement(Entity actor)
        {
            if (actor?.HasTag("Player") != true) return;
            var sitting = actor.GetPart<StatusEffectsPart>()?.GetEffect<SittingEffect>();
            if (sitting != null && SpatialQuery.Distance(actor.SpatialZone, actor, sitting.Furniture) != 0)
                actor.RemoveEffect<SittingEffect>();
        }
        private static bool Reject(Entity actor, Entity furniture, string reason, string message)
        { MessageLog.Add(message); Diag.Record("furniture", "PlayerSeatRejected", actor, furniture, new { reason }); return false; }
    }
}
