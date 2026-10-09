using System;
using System.Globalization;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Nearby, paid uses of carried water and fire clay. The selection
    /// binds the actor, recipient and available quantity. Extinguishing is an
    /// immediate rescue, not a lasting defense against a burning environment.</summary>
    public static class EmergencyDousingActions
    {
        const string WaterPrefix = "DrenchCreature|", ClayPrefix = "SmotherFire|";
        public static bool IsCommand(string command) => command != null
            && (command.StartsWith(WaterPrefix, StringComparison.Ordinal) || command.StartsWith(ClayPrefix, StringComparison.Ordinal));

        public static void AddActions(Entity actor, Entity item, Zone zone, InventoryActionList actions)
        {
            if (actions == null || ValidateSource(actor, item, zone, out bool clay, out int amount) != null) return;
            var origin = zone.GetEntityCell(actor);
            foreach (var target in zone.GetReadOnlyEntities())
            {
                if (ValidateTarget(actor, target, zone, clay, true) != null) continue;
                var cell = zone.GetEntityCell(target);
                string command = (clay ? ClayPrefix : WaterPrefix) + Uri.EscapeDataString(zone.ZoneID ?? "")
                    + "|" + Number(origin.X) + "|" + Number(origin.Y) + "|" + Uri.EscapeDataString(target.ID)
                    + "|" + Number(cell.X) + "|" + Number(cell.Y) + "|" + Number(amount);
                bool provokes = !clay && Provokes(actor, target);
                actions.AddAction(clay ? "SmotherFire" : "DrenchCreature",
                    (clay ? "smother flames on " : "drench ") + (target == actor ? "yourself" : target.GetDisplayName())
                    + (clay ? " (1 clay)" : " (1 water" + (provokes ? "; provokes" : "") + ")"), command, '\0', 18);
            }
        }

        /// <summary>Join the outer inventory receipt. Failure preserves payment
        /// and reverses only this operation's wetness, removed flame and cooling.</summary>
        internal static bool TryAct(Entity actor, Entity item, Zone zone, string command, InventoryTransaction tx)
        {
            if (!IsCommand(command)) return false;
            string invalid = ValidateSource(actor, item, zone, out bool clay, out int amount);
            if (invalid != null || tx == null) return Reject(actor, item, invalid ?? "missing-transaction");
            string[] fields = command.Split('|');
            if (fields.Length != 8 || fields[0] != (clay ? "SmotherFire" : "DrenchCreature")
                || !Parse(fields[2], out int ox) || !Parse(fields[3], out int oy)
                || !Parse(fields[5], out int x) || !Parse(fields[6], out int y) || !Parse(fields[7], out int selectedAmount))
                return Reject(actor, item, "malformed-selection");
            var origin = zone.GetEntityCell(actor);
            var target = WorldResourceActions.ExactGround(zone, fields[4]);
            var targetCell = target == null ? null : zone.GetEntityCell(target);
            if (WorldResourceActions.Decode(fields[1]) != zone.ZoneID || origin.X != ox || origin.Y != oy
                || targetCell == null || targetCell.X != x || targetCell.Y != y || selectedAmount != amount)
                return Reject(actor, item, "stale-selection");
            invalid = ValidateTarget(actor, target, zone, clay, true);
            if (invalid != null) return Reject(actor, item, invalid);
            if (!tx.TryClaim(actor, actor, command) || !tx.TryClaim(item, actor, command) || !tx.TryClaim(target, actor, command))
                return Reject(actor, item, "in-progress");

            var inventory = actor.GetPart<InventoryPart>(); var physics = item.GetPart<PhysicsPart>();
            var parts = item.Parts.ToArray(); string blueprint = item.BlueprintName;
            var skin = item.GetPart<WaterskinPart>(); var vessel = item.GetPart<LiquidVesselPart>();
            if (clay)
            {
                var payment = InventoryTransferSnapshot.Capture(inventory, item);
                tx.Do(null, payment.Restore);
                if (!payment.Apply(() => inventory.TryConsumeOne(item)) || !payment.ClaimChanges(tx, actor, command))
                    return Reject(actor, item, "payment-refused");
            }
            else
            {
                // Hold the exact Part for undo; do not overwrite a replacement
                // vessel installed by an independent callback.
                string oldLiquid = vessel?.LiquidId;
                tx.Do(null, () => { if (skin != null) skin.Charges = amount; else { vessel.Volume = amount; vessel.LiquidId = oldLiquid; } });
                WaterTransferActions.SetUnits(item, amount - 1);
            }
            Func<bool> unchanged = () => WorldResourceActions.ActorCurrent(actor, zone)
                && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true
                && zone.GetEntityCell(actor) == origin && zone.GetEntityCell(target) == targetCell
                && ValidateTarget(actor, target, zone, clay, false) == null
                && actor.GetPart<InventoryPart>() == inventory && item.BlueprintName == blueprint && item.Parts.SequenceEqual(parts)
                && (clay ? (amount > 1 ? WorldResourceActions.Carried(actor, item, false) && Quantity(item) == amount - 1
                    : !inventory.Objects.Contains(item) && item.SpatialZone == null && physics.InInventory == null && physics.Equipped == null)
                    : WaterTransferActions.Vessel(actor, item, out int remaining, out _) && remaining == amount - 1);
            if (!unchanged()) return Reject(actor, item, "changed-during-payment");

            var effects = target.GetPart<StatusEffectsPart>();
            var burning = ActiveBurn(target);
            var wet = target.GetEffect<WetEffect>();
            bool provokes = !clay && burning == null && !BrainPart.ArePartyAligned(actor, target);
            bool removedBurn = false;
            if (burning != null)
            {
                int index = effects.GetAllEffects().ToList().IndexOf(burning);
                string cause = burning.LastRemovalCause;
                tx.Do(null, () =>
                {
                    if (!removedBurn) return;
                    bool restoreAura = !effects.GetAllEffects().Contains(burning);
                    burning.LastRemovalCause = cause;
                    effects.RestoreRemovedEffectForInventoryUndo(burning, index);
                    if (restoreAura && target.GetPart<StatusEffectsPart>() == effects && target.SpatialZone != null
                        && target.SpatialZone.GetEntityCell(target)?.Objects.Contains(target) == true)
                        AsciiFxBus.StartAura(target.SpatialZone, target, burning.GetAuraTheme());
                });
            }
            if (!clay)
            {
                var incoming = new WetEffect(1);
                if (wet != null)
                {
                    float moisture = wet.Moisture; int duration = wet.Duration;
                    tx.Do(null, () => { wet.Moisture = moisture; wet.Duration = duration; });
                }
                else
                {
                    var originalEffects = effects;
                    tx.Do(null, () =>
                    {
                        var current = target.GetPart<StatusEffectsPart>();
                        current?.RemoveEffect(incoming);
                        // Applying the first status lazily adds the manager.
                        // Preserve it if a callback also added another status.
                        if (originalEffects == null && current?.EffectCount == 0) target.RemovePart(current);
                    });
                }
                if (!target.ApplyEffect(incoming, actor, zone)) return Reject(actor, item, "wetness-refused");
                effects = target.GetPart<StatusEffectsPart>();
                var appliedWet = target.GetEffect<WetEffect>();
                if (appliedWet != (wet ?? incoming) || !WorldResourceActions.Finite(appliedWet.Moisture) || appliedWet.Moisture < 1f)
                    return Reject(actor, item, "wetness-changed");
                // An expiring wet entry can still receive the stack callback.
                // Renew that same record instead of making a second wet effect.
                if (appliedWet.Duration == 0) appliedWet.Duration = Effect.DURATION_INDEFINITE;
            }
            if (!unchanged() || ActiveBurn(target) != burning) return Reject(actor, item, "changed-during-application");
            if (burning != null)
            {
                var heat = target.GetPart<ThermalPart>();
                if (heat != null)
                {
                    float temperature = heat.Temperature;
                    tx.Do(null, () => heat.Temperature = temperature);
                    heat.Temperature = Math.Min(temperature, Math.Min(heat.AmbientTemperature, IgnitionThreshold(target, heat) - 1f));
                }
                removedBurn = true;
                if (!effects.RemoveEffect(burning) || ActiveBurn(target) != null) return Reject(actor, item, "flame-removal-refused");
            }
            if (!unchanged()) return Reject(actor, item, "changed-during-rescue");
            // The rescue has already committed. One failing notification must
            // not suppress the remaining visual update or the paid-use receipt.
            if (provokes) tx.AfterCommit(() => target.GetPart<BrainPart>()?.SetPersonallyHostile(actor));
            if (removedBurn) tx.AfterCommit(() => target.FireEvent("Extinguished"));
            tx.AfterCommit(() => ZoneRenderHooks.MarkCellDirty(zone.GetEntityCell(target), clay ? "Smothered" : "Drenched"));
            tx.AfterCommit(() => Diag.Record("event", "EmergencyDousingUsed", actor, target,
                new { blueprint, clay, extinguished = removedBurn, provoked = provokes, spent = 1 }));
            tx.AfterCommit(() => MessageLog.Add(clay ? "You smother the flames with fire clay. It leaves no protective coating."
                : "You soak " + (target == actor ? "yourself" : target.GetDisplayName()) + " with one water. Wetness conducts electricity."));
            return true;
        }

        static string ValidateSource(Entity actor, Entity item, Zone zone, out bool clay, out int amount)
        {
            clay = item?.BlueprintName == "FireClay"; amount = 0;
            if (!WorldResourceActions.ActorCurrent(actor, zone) || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true) return "actor-unavailable";
            if (clay)
            {
                if (!WorldResourceActions.Carried(actor, item, false)) return "clay-not-carried";
                amount = Quantity(item);
            }
            else if (!WaterTransferActions.Vessel(actor, item, out amount, out _) || amount == 0) return "no-carried-water";
            return null;
        }
        static string ValidateTarget(Entity actor, Entity target, Zone zone, bool clay, bool requireChange)
        {
            if (!WorldResourceActions.Nearby(actor, target, zone) || CombatSystem.IsDeathHandled(target)
                || target.GetStatValue("Hitpoints", 1) <= 0 || WorldResourceActions.ExactGround(zone, Uri.EscapeDataString(target.ID)) != target
                || target.GetPart<DestructiblePart>() is DestructiblePart structure && (structure.Gone || structure.HP <= 0)) return "recipient-unavailable";
            if (!clay && !target.HasTag("Creature")) return "recipient-not-a-creature";
            var effects = target.GetPart<StatusEffectsPart>();
            if (effects != null && (effects.ParentEntity != target || effects.GetAllEffects().Count(e => e is BurningEffect) > 1
                || effects.GetAllEffects().Count(e => e is WetEffect) > 1)) return "ambiguous-status";
            var burn = target.GetEffect<BurningEffect>(); var wet = target.GetEffect<WetEffect>();
            if (burn != null && (burn.GetType() != typeof(BurningEffect) || !WorldResourceActions.Finite(burn.Intensity))) return "unsupported-flame";
            if (wet != null && (wet.GetType() != typeof(WetEffect) || !WorldResourceActions.Finite(wet.Moisture) || wet.Moisture < 0 || wet.Moisture > 1)) return "unsupported-wetness";
            var heat = target.GetPart<ThermalPart>();
            if (heat != null && (heat.ParentEntity != target || !WorldResourceActions.Finite(heat.Temperature)
                || !WorldResourceActions.Finite(heat.AmbientTemperature) || !WorldResourceActions.Finite(heat.FlameTemperature)
                || !WorldResourceActions.Finite(IgnitionThreshold(target, heat)))) return "invalid-temperature";
            if (requireChange && (clay ? ActiveBurn(target) == null
                : ActiveBurn(target) == null && wet != null && wet.Duration != 0 && wet.Moisture >= 1f)) return "nothing-to-change";
            return null;
        }
        static BurningEffect ActiveBurn(Entity target)
        { var effect = target?.GetEffect<BurningEffect>(); return effect != null && effect.Intensity > 0 && effect.Duration != 0 ? effect : null; }
        static bool Provokes(Entity actor, Entity target) => ActiveBurn(target) == null && !BrainPart.ArePartyAligned(actor, target);
        static float IgnitionThreshold(Entity target, ThermalPart heat)
        { float volatility = target.GetPart<MaterialPart>()?.Volatility ?? 0; return heat.FlameTemperature - (volatility > 0 ? volatility * 100 : 0); }
        static int Quantity(Entity item) => item.GetPart<StackerPart>()?.StackCount ?? 1;
        static string Number(int number) => number.ToString(CultureInfo.InvariantCulture);
        static bool Parse(string text, out int number) => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out number);
        static bool Reject(Entity actor, Entity item, string reason)
        {
            Diag.Record("event", "EmergencyDousingRejected", actor, item, new { reason });
            MessageLog.Add("You cannot use that supply here (" + reason.Replace('-', ' ') + ").");
            return false;
        }
    }
}
