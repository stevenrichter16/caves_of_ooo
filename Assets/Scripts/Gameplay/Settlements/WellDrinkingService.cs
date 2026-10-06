using System;
using CavesOfOoo.Core.Inventory;
namespace CavesOfOoo.Core
{
    internal static class WellDrinkingService
    {
        internal static bool Available(Entity actor, WellPart well, Zone zone)
        {
            var owner = well?.ParentEntity;
            if (!WorldResourceActions.Nearby(actor, owner, zone) || owner.GetPart<WellPart>() != well
                || owner.GetPart<PhysicsPart>().Takeable || owner.HasTag("Creature") || !well.IsUsable) return false;
            var pool = owner.GetPart<LiquidPoolPart>();
            return pool == null || pool.ParentEntity == owner && pool.LiquidId == "water" && pool.Volume > 0
                && LiquidSourcePhase.CanDrawWater(zone, owner) && LiquidSourceSafety.IsUnmixedPool(zone, owner);
        }
        internal static bool TryDrink(Entity actor, WellPart well, Zone zone, InventoryTransaction tx)
        {
            if (!Available(actor, well, zone)) return WorldResourceActions.Reject(actor, well?.ParentEntity, "DrawWaterAtWell", "invalid_or_unavailable_water_source");
            bool own = tx == null; tx ??= new InventoryTransaction();
            try
            {
                var owner = well.ParentEntity;
                if (!tx.TryClaim(actor, actor, "DrawWaterAtWell") || !tx.TryClaim(owner, actor, "DrawWaterAtWell")) return false;
                var effects = actor.GetPart<StatusEffectsPart>(); var thirst = actor.GetEffect<ParchedEffect>();
                int index = -1, stacks = thirst?.Stacks ?? 0; string cause = thirst?.LastRemovalCause;
                if (thirst != null) for (int i = 0; i < effects.EffectCount; i++) if (effects.GetAllEffects()[i] == thirst) index = i;
                var pool = owner.GetPart<LiquidPoolPart>(); int volume = pool?.Volume ?? 0;
                tx.Do(null, () =>
                {
                    if (pool != null) pool.Volume = volume;
                    if (thirst == null) return;
                    var current = actor.GetEffect<ParchedEffect>(); int restored = stacks;
                    if (current == null) { thirst.Stacks = stacks; thirst.LastRemovalCause = cause; effects.RestoreRemovedEffectForInventoryUndo(thirst, index); }
                    else { restored = Math.Min(stacks, ParchedEffect.MaxStacks - current.Stacks); current.Stacks += restored; }
                    if (actor.GetStat("Strength") is Stat strength) strength.Penalty += restored * ParchedEffect.PenaltyPerStack;
                    if (actor.GetStat("Agility") is Stat agility) agility.Penalty += restored * ParchedEffect.PenaltyPerStack;
                });
                if (pool != null) pool.Volume--;
                if (thirst != null) effects.RemoveEffect<ParchedEffect>();
                tx.AfterCommit(() => MessageLog.Add(thirst != null ? "You draw cool water and drink until the parch lets go." : "You drink. The well water is cool."));
                tx.AfterCommit(() => CavesOfOoo.Diagnostics.Diag.Record("furniture", "WaterDrawn", actor, owner,
                    new { curedParched = thirst != null, finiteUnitsSpent = pool == null ? 0 : 1 }));
                if (own) tx.Commit(); return true;
            }
            finally { if (own) tx.Rollback(); }
        }
    }
}
