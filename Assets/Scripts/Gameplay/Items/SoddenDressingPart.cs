using System;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>One carried dressing treats existing ordinary poison and bleeding
    /// in one native Apply action. No healing, immunity, gas or fungal treatment.</summary>
    public sealed class SoddenDressingPart : Part
    {
        public override string Name => "SoddenDressing";
        public const string ApplyCommand = "Apply";
        static bool Eligible(Effect effect) => effect?.GetType() == typeof(PoisonedEffect) || effect?.GetType() == typeof(BleedingEffect);
        bool Available(Entity actor, Zone zone) => actor != null && SoddenPreparationRules.CurrentActor(actor, zone, zone?.GetEntityCell(actor))
            && SoddenPreparationRules.Dressing(ParentEntity) && ParentEntity.GetPart<SoddenDressingPart>() == this
            && SoddenPreparationRules.Carried(actor, actor.GetPart<InventoryPart>(), ParentEntity)
            && actor.GetPart<StatusEffectsPart>()?.ParentEntity == actor
            && actor.GetPart<StatusEffectsPart>().GetAllEffects().Any(e => e.Owner == actor && Eligible(e));
        public override bool HandleEvent(GameEvent e)
        {
            var actor = e.GetParameter<Entity>("Actor"); var zone = e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone;
            if (e.ID == "GetInventoryActions")
            {
                if (Available(actor, zone)) e.GetParameter<InventoryActionList>("Actions")?.AddAction("Apply", "apply (treat ordinary poison and bleeding)", ApplyCommand, 'a', 20);
                return true;
            }
            if (e.ID != "InventoryAction" || e.GetStringParameter("Command") != ApplyCommand) return true;
            if (e.GetParameter<bool>("SoddenDressingAttempted")) return true;
            e.SetParameter("SoddenDressingAttempted", true);
            if (!Apply(actor, zone, e.GetParameter<InventoryTransaction>("InventoryTransaction"))) return true;
            e.Handled = true; return false;
        }
        bool Apply(Entity actor, Zone zone, InventoryTransaction tx)
        {
            if (tx == null || !Available(actor, zone)) return Reject(actor, "unavailable");
            var item = ParentEntity; var pack = actor.GetPart<InventoryPart>(); var status = actor.GetPart<StatusEffectsPart>();
            var at = zone.GetEntityCell(actor); var parts = item.Parts.ToArray();
            if (!tx.TryClaim(actor, actor, "ApplySoddenDressing") || !tx.TryClaim(item, actor, "ApplySoddenDressing")) return Reject(actor, "in-progress");
            var effects = status.GetAllEffects().Select((effect, index) => new { effect, index, cause = effect.LastRemovalCause })
                .Where(s => s.effect.Owner == actor && Eligible(s.effect)).ToArray();
            var receipt = InventoryTransferSnapshot.Capture(pack, item); tx.Do(null, receipt.Restore);
            if (!receipt.Apply(() => pack.TryConsumeOne(item)) || !receipt.ClaimChanges(tx, actor, "ApplySoddenDressing")
                || !SoddenPreparationRules.CurrentActor(actor, zone, at) || actor.GetPart<InventoryPart>() != pack
                || actor.GetPart<StatusEffectsPart>() != status || !item.Parts.SequenceEqual(parts)
                || effects.Any(s => s.effect.Owner != actor || !status.GetAllEffects().Contains(s.effect))) return Reject(actor, "source-changed");
            // Remove in reverse order so transaction rollback restores exact prior ordering.
            for (int i = effects.Length - 1; i >= 0; i--)
            {
                var saved = effects[i];
                tx.Do(null, () =>
                {
                    bool removed = !status.GetAllEffects().Contains(saved.effect);
                    saved.effect.LastRemovalCause = saved.cause;
                    status.RestoreRemovedEffectForInventoryUndo(saved.effect, saved.index);
                    // The generic inventory undo restores mechanics only. Poison also
                    // needs its presentation resumed after RemoveEffect queued AuraStop.
                    if (removed && saved.effect is IAuraProvider aura && actor.GetPart<StatusEffectsPart>() == status
                        && actor.SpatialZone != null && actor.SpatialZone.GetEntityCell(actor)?.Objects.Contains(actor) == true)
                        AsciiFxBus.StartAura(actor.SpatialZone, actor, aura.GetAuraTheme());
                });
                if (!status.RemoveEffect(saved.effect) || actor.GetPart<StatusEffectsPart>() != status
                    || !SoddenPreparationRules.CurrentActor(actor, zone, at) || actor.GetPart<InventoryPart>() != pack)
                    return Reject(actor, "effect-changed");
            }
            bool poisoned = effects.Any(s => s.effect.GetType() == typeof(PoisonedEffect));
            bool bleeding = effects.Any(s => s.effect.GetType() == typeof(BleedingEffect));
            tx.AfterCommit(() =>
            {
                MessageLog.Add("You bind the field dressing over your wounds.");
                Diag.Record("event", "SoddenDressingApplied", actor, item, new { treatedPoison = poisoned, stoppedBleeding = bleeding });
            });
            return true;
        }
        bool Reject(Entity actor, string reason)
        {
            MessageLog.Add("A carried field dressing treats existing ordinary poison or bleeding; it does not treat gas poison or fungal infection.");
            Diag.Record("event", "SoddenDressingRejected", actor, ParentEntity, new { reason }); return false;
        }
    }
}
