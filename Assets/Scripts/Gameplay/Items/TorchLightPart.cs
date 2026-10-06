using System;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>A controlled tool flame, separate from destructive BurningEffect.
    /// Stowed torches keep state; equipped/loose torches consume existing fuel.</summary>
    public sealed class TorchLightPart : Part
    {
        public override string Name => "TorchLight";
        public const string LightCommand = "LightTorch", ExtinguishCommand = "ExtinguishTorch";
        public float LightTemperature = 450f;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var actor = e.GetParameter<Entity>("Actor");
                if (CanReach(actor, actor?.SpatialZone))
                {
                    bool lit = ParentEntity.GetPart<LightSourcePart>()?.Enabled == true;
                    e.GetParameter<InventoryActionList>("Actions")?.AddAction(lit ? "Extinguish" : "Light",
                        lit ? "extinguish torch" : "light torch at nearby fire", lit ? ExtinguishCommand : LightCommand, 'l', 15);
                }
            }
            if (e.ID == "InventoryAction")
            {
                string command = e.GetStringParameter("Command");
                if (command == LightCommand || command == ExtinguishCommand)
                {
                    if (TryAct(e.GetParameter<Entity>("Actor"), e.GetParameter<Zone>("Zone"), command,
                        e.GetParameter<InventoryTransaction>("InventoryTransaction")))
                    { e.Handled = true; return false; }
                }
            }
            if (e.ID == "EndTurn" && ParentEntity.SpatialZone != null)
                Tick(ParentEntity.SpatialZone);
            return true;
        }

        /// <summary>Exactly one turn tick for an actual equipped or loose torch.
        /// Fuel drives the controlled flame, with no damage to the tool or bearer.</summary>
        internal void Tick(Zone zone)
        {
            var light = ParentEntity.GetPart<LightSourcePart>();
            var fuel = ParentEntity.GetPart<FuelPart>();
            var thermal = ParentEntity.GetPart<ThermalPart>();
            if (light?.Enabled != true || fuel == null || thermal == null) return;
            if (!UsableFuel(fuel) || ParentEntity.HasEffect<FrozenEffect>() || ParentEntity.GetEffect<WetEffect>()?.Moisture > 0.35f
                || thermal.Temperature < thermal.FlameTemperature)
            { Extinguish(null); return; }
            // Destructive fire already draws fuel in BeginTakeAction. Preserve
            // its normal damage/heat rules rather than burning the same fuel twice.
            if (ParentEntity.HasEffect<BurningEffect>()) return;
            var consume = GameEvent.New("ConsumeFuel");
            consume.SetParameter("Intensity", (object)1f);
            ParentEntity.FireEventAndRelease(consume);
            if (!UsableFuel(fuel)) Extinguish(null);
            else thermal.Temperature = Math.Max(LightTemperature, thermal.FlameTemperature);
        }

        private bool TryAct(Entity actor, Zone zone, string command, InventoryTransaction transaction)
        {
            if (actor == null || CombatSystem.IsDeathHandled(actor)
                || (actor.GetStat("Hitpoints") is Stat hp && hp.Value <= 0))
                return Reject(actor, "actor_dead", "You cannot use a torch right now.");
            if (!CanReach(actor, zone)) return Reject(actor, "not_accessible", "Hold one torch, or stand beside a loose torch, to use it.");
            var light = ParentEntity.GetPart<LightSourcePart>();
            var fuel = ParentEntity.GetPart<FuelPart>();
            var thermal = ParentEntity.GetPart<ThermalPart>();
            if (light == null || thermal == null || fuel == null)
                return Reject(actor, "invalid_torch", "That torch cannot be used.");
            bool lighting = command == LightCommand;
            if (light.Enabled == lighting) return Reject(actor, "already_set", lighting ? "The torch is already lit." : "The torch is already out.");
            Entity source = null;
            if (lighting)
            {
                if (!UsableFuel(fuel) || !Finite(LightTemperature) || LightTemperature < thermal.FlameTemperature)
                    return Reject(actor, "no_fuel", "That torch has no usable fuel.");
                if (ParentEntity.HasEffect<FrozenEffect>())
                    return Reject(actor, "frozen", "The torch must thaw before it can be lit.");
                if (ParentEntity.GetEffect<WetEffect>()?.Moisture > 0.35f)
                    return Reject(actor, "wet", "The torch is too wet to light.");
                source = FindFire(actor, zone);
                if (source == null) return Reject(actor, "no_fire", "Stand beside a burning fire to light the torch.");
            }
            bool own = transaction == null;
            transaction ??= new InventoryTransaction();
            try
            {
                if (!transaction.TryClaim(actor, actor, command) || !transaction.TryClaim(ParentEntity, actor, command)
                    || (source != null && !transaction.TryClaim(source, actor, command)))
                    return Reject(actor, "in_progress", "That torch is already being used.");
                bool wasEnabled = light.Enabled;
                float oldTemperature = thermal.Temperature;
                var effects = ParentEntity.GetPart<StatusEffectsPart>();
                var burning = effects?.GetEffect<BurningEffect>();
                int burningIndex = -1;
                if (burning != null)
                { var all = effects.GetAllEffects(); for (int i=0;i<all.Count;i++) if (ReferenceEquals(all[i],burning)) burningIndex=i; }
                transaction.Do(null, () =>
                {
                    light.Enabled = wasEnabled; thermal.Temperature = oldTemperature;
                    if (burning != null && effects.GetEffect<BurningEffect>() == null)
                        effects.RestoreRemovedEffectForInventoryUndo(burning, burningIndex);
                    EquipmentChangeBus.NotifyChanged(actor);
                });
                if (lighting) { light.Enabled = true; thermal.Temperature = LightTemperature; }
                else Extinguish(actor);
                transaction.AfterCommit(() => EquipmentChangeBus.NotifyChanged(actor));
                transaction.AfterCommit(() => Diag.Record("event", lighting ? "TorchLit" : "TorchExtinguished", actor, ParentEntity,
                    new { fuel = fuel.FuelMass }));
                transaction.AfterCommit(() => MessageLog.Add(lighting ? "You light the torch." : "You extinguish the torch."));
                if (own) transaction.Commit();
                return true;
            }
            finally { if (own) transaction.Rollback(); }
        }

        private void Extinguish(Entity actor)
        {
            var light = ParentEntity.GetPart<LightSourcePart>();
            var thermal = ParentEntity.GetPart<ThermalPart>();
            if (light != null) light.Enabled = false;
            if (thermal != null) thermal.Temperature = thermal.AmbientTemperature;
            ParentEntity.RemoveEffect<BurningEffect>();
            EquipmentChangeBus.NotifyChanged(actor);
        }
        /// <summary>Splits preserve the known torch environmental states. Unknown
        /// effects refuse splitting before quantity changes instead of being lost.</summary>
        internal bool CanSplitSafely()
        {
            var effects = ParentEntity.GetPart<StatusEffectsPart>()?.GetAllEffects();
            if (effects == null) return true;
            foreach (var effect in effects)
            {
                var type = effect.GetType();
                if (type != typeof(WetEffect) && type != typeof(FrozenEffect) && type != typeof(BurningEffect))
                    return false;
            }
            return true;
        }

        internal void ReportSplitRefusal()
        {
            MessageLog.Add("That torch stack cannot be separated until its current effect ends.");
            Diag.Record("event", "TorchRejected", target: ParentEntity, payload: new { reason = "unsafe_effect_split" });
        }

        /// <summary>Called after the ordinary part/stat clone. Restore effect
        /// fields directly: reapplying would double wet porosity, heat or stats.</summary>
        internal void CopySplitEffectsTo(Entity copy)
        {
            var effects = ParentEntity.GetPart<StatusEffectsPart>()?.GetAllEffects();
            if (effects == null || effects.Count == 0) return;
            var cloned = new System.Collections.Generic.List<Effect>(effects.Count);
            foreach (var effect in effects)
            {
                Effect value;
                if (effect.GetType() == typeof(WetEffect)) value = new WetEffect(((WetEffect)effect).Moisture);
                else if (effect.GetType() == typeof(FrozenEffect)) value = new FrozenEffect(((FrozenEffect)effect).Cold);
                else if (effect.GetType() == typeof(BurningEffect))
                {
                    var burning = (BurningEffect)effect;
                    value = new BurningEffect(burning.Intensity, burning.IgnitionSource, burning.Rng);
                }
                else throw new InvalidOperationException("Unsupported torch effect split.");
                value.Duration = effect.Duration; value.JustApplied = effect.JustApplied;
                value.LastRemovalCause = effect.LastRemovalCause;
                cloned.Add(value);
            }
            copy.GetPart<StatusEffectsPart>().RestoreEffectsForLoad(cloned);
        }

        private bool CanReach(Entity actor, Zone zone)
        {
            if (actor == null || (ParentEntity.GetPart<StackerPart>()?.StackCount ?? 1) != 1) return false;
            var physics = ParentEntity.GetPart<PhysicsPart>();
            var inventory = actor.GetPart<InventoryPart>();
            if (physics?.Equipped == actor && inventory?.EquippedItems.ContainsValue(ParentEntity) == true) return true;
            return physics != null && physics.InInventory == null && physics.Equipped == null
                && zone?.GetEntityCell(actor) != null && zone.GetEntityCell(ParentEntity) != null
                && SpatialQuery.Distance(zone, actor, ParentEntity) <= 1;
        }
        private Entity FindFire(Entity actor, Zone zone) => IgnitionSources.Find(actor, zone, ParentEntity);
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool UsableFuel(FuelPart fuel) => Finite(fuel.FuelMass) && fuel.FuelMass > 0
            && Finite(fuel.BurnRate) && fuel.BurnRate > 0;
        private bool Reject(Entity actor, string reason, string message)
        { MessageLog.Add(message); Diag.Record("event", "TorchRejected", actor, ParentEntity, new { reason }); return false; }
    }
}
