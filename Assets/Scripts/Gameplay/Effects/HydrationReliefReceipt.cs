using System;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Transient owned Parched relief shared by water actions. No clock,
    /// strain, equipment or saved state belongs to this receipt.</summary>
    internal sealed class HydrationReliefReceipt
    {
        readonly Entity actor;
        readonly ParchedEffect original;
        readonly int index, duration;
        readonly string cause;
        internal int Relieved { get; private set; }

        HydrationReliefReceipt(Entity actor)
        {
            this.actor = actor; original = actor.GetEffect<ParchedEffect>();
            if (original == null) return;
            duration = original.Duration; cause = original.LastRemovalCause;
            var all = actor.GetPart<StatusEffectsPart>().GetAllEffects();
            for (int i = 0; i < all.Count; i++) if (all[i] == original) { index = i; break; }
        }
        internal static bool ValidContext(Entity actor)
        {
            if (actor == null) return false;
            var status = actor.GetPart<StatusEffectsPart>(); var thirst = actor.GetEffect<ParchedEffect>();
            return (status == null || status.ParentEntity == actor)
                && (thirst == null || thirst.Owner == actor && thirst.Stacks >= 1 && thirst.Stacks <= ParchedEffect.MaxStacks);
        }
        internal static bool TryApply(Entity actor, int maximum, InventoryTransaction tx, out HydrationReliefReceipt receipt)
        {
            receipt = null;
            if (tx == null || maximum < 1 || maximum > ParchedEffect.MaxStacks || !ValidContext(actor)) return false;
            var result = new HydrationReliefReceipt(actor); receipt = result;
            // Do(apply,undo) enlists undo AFTER apply; lifecycle callbacks must
            // already have the undo in place, including when they throw.
            tx.Do(null, result.Undo);
            if (result.original == null) return true;
            int amount = Math.Min(maximum, result.original.Stacks);
            if (amount == result.original.Stacks)
                return actor.GetPart<StatusEffectsPart>().RemoveEffectWithReceipt(result.original, null, () => result.Relieved = amount);
            result.original.Stacks -= amount; result.Relieved = amount; Shift(actor, -amount);
            return true;
        }
        void Undo()
        {
            if (Relieved == 0) return;
            var status = actor.GetPart<StatusEffectsPart>(); var current = actor.GetEffect<ParchedEffect>();
            if (status?.ParentEntity != actor || current != null && (current.Owner != actor || current.Stacks < 1 || current.Stacks > ParchedEffect.MaxStacks))
            { Diag.Record("event", "HydrationUndoBoundary", actor: actor); return; }
            int restored = Math.Min(Relieved, ParchedEffect.MaxStacks - (current?.Stacks ?? 0));
            if (current == null)
            {
                // An independent lifecycle listener may have handed the removed
                // instance to another actor. Restore our native consequence
                // without stealing or rewriting that foreign-owned instance.
                var returned = original.Owner == null || original.Owner == actor ? original : new ParchedEffect();
                returned.Stacks = restored; returned.Duration = duration; returned.LastRemovalCause = cause;
                status.RestoreRemovedEffectForInventoryUndo(returned, index);
            }
            else current.Stacks += restored;
            Shift(actor, restored);
        }
        static void Shift(Entity actor, int levels)
        {
            if (actor.GetStat("Strength") is Stat strength) strength.Penalty += levels * ParchedEffect.PenaltyPerStack;
            if (actor.GetStat("Agility") is Stat agility) agility.Penalty += levels * ParchedEffect.PenaltyPerStack;
        }
    }
}
