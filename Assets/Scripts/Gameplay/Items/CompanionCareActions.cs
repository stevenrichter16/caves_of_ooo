using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Paid, adjacent care using the existing payloads of eighteen
    /// carried remedies and meals. Party consent, usefulness and exact selection
    /// are rechecked through the final inventory commit.</summary>
    public static class CompanionCareActions
    {
        const string Treat = "TreatCompanion|", Share = "ShareMeal|";
        public static bool IsCommand(string command) => command != null
            && (command.StartsWith(Treat, StringComparison.Ordinal) || command.StartsWith(Share, StringComparison.Ordinal));

        public static void AddActions(Entity actor, Entity item, Zone zone, InventoryActionList actions)
        {
            if (actions == null || !Source(actor, item, zone, out var payload)) return;
            var origin = zone.GetEntityCell(actor);
            foreach (var target in zone.GetReadOnlyEntities())
            {
                if (!Recipient(actor, target, zone) || !Useful(target, payload)) continue;
                var at = zone.GetEntityCell(target);
                string command = (payload.Meal ? Share : Treat) + Escape(zone.ZoneID) + "|" + origin.X + "|" + origin.Y
                    + "|" + Escape(target.ID) + "|" + at.X + "|" + at.Y + "|" + Quantity(item) + "|" + Escape(payload.Signature);
                actions.AddAction(payload.Meal ? "ShareMeal" : "TreatCompanion",
                    (payload.Meal ? "share with " : "treat ") + target.GetDisplayName() + " (1 item; " + payload.Description + ")",
                    command, '\0', 18);
            }
        }

        internal static bool TryAct(Entity actor, Entity item, Zone zone, string command, InventoryTransaction tx)
        {
            if (!IsCommand(command)) return false;
            if (tx == null || !Source(actor, item, zone, out var payload)) return Reject(actor, item, "source-unavailable");
            string[] fields = command.Split('|');
            if (fields.Length != 9 || fields[0] + "|" != (payload.Meal ? Share : Treat)
                || !Number(fields[2], out int ox) || !Number(fields[3], out int oy)
                || !Number(fields[5], out int x) || !Number(fields[6], out int y) || !Number(fields[7], out int amount))
                return Reject(actor, item, "malformed-selection");
            var origin = zone.GetEntityCell(actor); var target = WorldResourceActions.ExactGround(zone, fields[4]);
            var at = target == null ? null : zone.GetEntityCell(target);
            if (WorldResourceActions.Decode(fields[1]) != zone.ZoneID || origin.X != ox || origin.Y != oy
                || at == null || at.X != x || at.Y != y || amount != Quantity(item)
                || WorldResourceActions.Decode(fields[8]) != payload.Signature || !Recipient(actor, target, zone) || !Useful(target, payload))
                return Reject(actor, item, "stale-or-useless-selection");
            if (!tx.TryClaim(actor, actor, command) || !tx.TryClaim(item, actor, command) || !tx.TryClaim(target, actor, command))
                return Reject(actor, item, "in-progress");
            var pack = actor.GetPart<InventoryPart>(); var physics = item.GetPart<PhysicsPart>(); var parts = item.Parts.ToArray();
            var recipientHealth = target.GetStat("Hitpoints"); StatusEffectsPart treatedStatus = null;
            string blueprint = item.BlueprintName; var payment = InventoryTransferSnapshot.Capture(pack, item);
            tx.Do(null, payment.Restore);
            if (!payment.Apply(() => pack.TryConsumeOne(item)) || !payment.ClaimChanges(tx, actor, command))
                return Reject(actor, item, "payment-refused");
            Func<bool> current = () => WorldResourceActions.ActorCurrent(actor, zone)
                && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true && zone.GetEntityCell(actor) == origin
                && Recipient(actor, target, zone) && zone.GetEntityCell(target) == at
                && target.GetStat("Hitpoints") == recipientHealth
                && (treatedStatus == null || target.GetPart<StatusEffectsPart>() == treatedStatus)
                && WorldResourceActions.ExactGround(zone, fields[4]) == target && actor.GetPart<InventoryPart>() == pack
                && item.BlueprintName == blueprint && item.Parts.SequenceEqual(parts) && ReadPayload(item)?.Signature == payload.Signature
                && (amount > 1 ? WorldResourceActions.Carried(actor, item, false) && Quantity(item) == amount - 1
                    : !pack.Objects.Contains(item) && item.SpatialZone == null && physics.InInventory == null && physics.Equipped == null);
            if (!current()) return Reject(actor, item, "changed-during-payment");
            tx.BeforeCommit(current);

            bool benefited = false; int healed = 0, cured = 0;
            if (!string.IsNullOrEmpty(payload.Healing))
            {
                var hp = target.GetStat("Hitpoints"); int before = hp.Value;
                TrackMutation(target, null, tx, () => hp.BaseValue = (int)Math.Min((long)hp.Max,
                    (long)hp.BaseValue + Math.Max(0, DiceRoller.Roll(payload.Healing, new Random()))));
                healed = hp.Value - before; benefited |= healed > 0;
            }
            var status = target.GetPart<StatusEffectsPart>();
            var cures = status?.GetAllEffects().Where(e => Matches(e, payload)).ToArray() ?? Array.Empty<Effect>();
            if (payload.Kind == "FieldMeal") cures = cures.Take(1).ToArray();
            if (cures.Length > 0) treatedStatus = status;
            // Reverse deletion keeps original indexes stable during rollback.
            for (int i = cures.Length - 1; i >= 0; i--)
            {
                var effect = cures[i]; var receipt = new ChangeReceipt(target, effect, status); tx.Do(null, receipt.Undo);
                if (!status.RemoveEffectWithReceipt(effect, receipt.Before, receipt.After)) return Reject(actor, item, "cure-changed");
                cured++; benefited = true;
                if (!current()) return Reject(actor, item, "changed-during-cure");
            }
            if (payload.Cure == nameof(BurningEffect) && cured > 0 && target.GetPart<ThermalPart>() is ThermalPart thermal)
                TrackMutation(target, null, tx, () => thermal.Temperature = Math.Min(thermal.Temperature, thermal.AmbientTemperature));
            if (payload.Drink && target.GetEffect<ParchedEffect>() is ParchedEffect parch)
            {
                treatedStatus = status;
                if (parch.Stacks > 1) TrackMutation(target, parch, tx, () => ParchedEffect.ReduceOneStack(target));
                else
                {
                    var receipt = new ChangeReceipt(target, parch, status); tx.Do(null, receipt.Undo);
                    status.RemoveEffectWithReceipt(parch, receipt.Before, receipt.After);
                }
            }
            if (payload.Kind == "Food")
            {
                bool addedStatus = status == null;
                if (addedStatus) { status = new StatusEffectsPart(); target.AddPart(status); }
                var manager = status;
                treatedStatus = manager;
                // Registered before the meal receipt so lazy manager cleanup runs last.
                if (addedStatus) tx.Do(null, () => { if (target.GetPart<StatusEffectsPart>() == manager && manager.EffectCount == 0) target.RemovePart(manager); });
                var incoming = new PreparedMealEffect(payload.Stat, payload.Bonus, payload.Duration);
                var priorMeal = target.GetEffect<PreparedMealEffect>(); string priorState = MealState(priorMeal);
                var tracked = (Effect)priorMeal ?? incoming;
                var receipt = new ChangeReceipt(target, tracked, manager); tx.Do(null, receipt.Undo);
                Action beforeMeal = () =>
                {
                    // BeforeApplyEffect is external: it can change the prior
                    // preparation or the incoming payload before native stacking.
                    // Refuse before taking ownership of any such independent work.
                    if (!current() || target.GetEffect<PreparedMealEffect>() != priorMeal || MealState(priorMeal) != priorState
                        || incoming.StatName != payload.Stat || incoming.Bonus != payload.Bonus
                        || incoming.Duration != payload.Duration || incoming.AppliedBonus != 0)
                        throw new InvalidOperationException("The selected companion preparation changed before application.");
                    receipt.Before();
                };
                if (!target.ApplyEffectWithReceipt(incoming, actor, zone, beforeMeal, receipt.After))
                    return Reject(actor, item, "meal-refused");
                var meal = target.GetEffect<PreparedMealEffect>();
                if (meal == null || meal.StatName != payload.Stat || meal.Bonus != payload.Bonus || meal.Duration != payload.Duration)
                    return Reject(actor, item, "meal-changed");
                benefited = true;
            }
            if (!benefited || !current()) return Reject(actor, item, "no-benefit-or-target-changed");
            tx.AfterCommit(() => ZoneRenderHooks.MarkCellDirty(at.X, at.Y, "CompanionCare"));
            tx.AfterCommit(() => Diag.Record("event", "CompanionCareUsed", actor, target,
                new { item = item.BlueprintName, healed, cured, sharedMeal = payload.Meal, preparation = payload.Stat }));
            tx.AfterCommit(() => MessageLog.Add("You " + (payload.Meal ? "share " : "use ") + item.GetDisplayName()
                + (payload.Meal ? " with " : " to treat ") + target.GetDisplayName() + "."));
            return true;
        }

        static bool Source(Entity actor, Entity item, Zone zone, out Payload payload)
        {
            payload = ReadPayload(item);
            return payload != null && WorldResourceActions.ActorCurrent(actor, zone)
                && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true
                && WorldResourceActions.Carried(actor, item, false) && actor.GetPart<InventoryPart>().CanConsumeOne(item);
        }
        static bool Recipient(Entity actor, Entity target, Zone zone)
        {
            if (target == actor || !WorldResourceActions.Nearby(actor, target, zone) || !target.HasTag("Creature")
                || target.GetStat("Hitpoints") == null || target.GetStatValue("Hitpoints") <= 0 || CombatSystem.IsDeathHandled(target)
                || !BrainPart.ArePartyAligned(actor, target) || target.GetPart<BrainPart>()?.IsPersonallyHostileTo(actor) == true
                || actor.GetPart<BrainPart>()?.IsPersonallyHostileTo(target) == true) return false;
            var status = target.GetPart<StatusEffectsPart>(); var thermal = target.GetPart<ThermalPart>();
            return (status == null || status.ParentEntity == target && status.GetAllEffects().All(e => e != null && e.Owner == target))
                && (thermal == null || WorldResourceActions.Finite(thermal.Temperature) && WorldResourceActions.Finite(thermal.AmbientTemperature));
        }
        static bool Useful(Entity target, Payload p)
        {
            var hp = target.GetStat("Hitpoints");
            if (!string.IsNullOrEmpty(p.Healing) && hp.Value < hp.Max && hp.BaseValue < hp.Max) return true;
            if (target.GetPart<StatusEffectsPart>()?.GetAllEffects().Any(e => Matches(e, p)) == true) return true;
            if (p.Kind != "Food") return false;
            var prior = target.GetEffect<PreparedMealEffect>();
            return prior == null || prior.StatName != p.Stat || prior.Bonus != p.Bonus || prior.Duration < p.Duration;
        }
        static bool Matches(Effect effect, Payload p)
        {
            if (effect == null || effect.Duration == 0) return false;
            if (p.Kind == "Dressing") return effect.GetType() == typeof(PoisonedEffect) || effect.GetType() == typeof(BleedingEffect);
            if (p.Kind == "FieldMeal") return effect.GetType() == typeof(BleedingEffect);
            return p.Cure == "All" ? (effect.GetEffectType() & Effect.TYPE_NEGATIVE) != 0
                : !string.IsNullOrEmpty(p.Cure) && (effect.ClassName == p.Cure || p.Cure == nameof(PoisonedEffect) && effect is PoisonedByGasEffect);
        }
        static Payload ReadPayload(Entity item)
        {
            if (item == null) return null;
            switch (item.BlueprintName)
            {
                case "SoddenFieldDressing": return item.GetPart<SoddenDressingPart>()?.ParentEntity == item ? new Payload { Kind = "Dressing", Description = "ordinary poison and bleeding" } : null;
                case "FieldMeal": return item.GetPart<FieldMealPart>()?.ParentEntity == item ? new Payload { Kind = "FieldMeal", Healing = "3d4", Description = "heal 3d4; one bleed" } : null;
                case "CookedMeat": case "ToastedEmberwheat": case "RoastedMushroom": case "RoastedHearthbulb": case "RoastedStarapple":
                    var food = item.GetPart<FoodPart>();
                    return food?.ParentEntity == item && PreparedMealEffect.Valid(food.MealStat, food.MealBonus, food.MealDuration)
                        ? new Payload { Kind = "Food", Healing = food.Healing, Stat = food.MealStat, Bonus = food.MealBonus, Duration = food.MealDuration,
                            Description = "+" + food.MealBonus + " " + PreparedMealEffect.Label(food.MealStat) + "; replaces prior meal" } : null;
                case "HealingTonic": case "Antidote": case "BurnSalve": case "Panacea": case "KnotflaxBandage": case "SumpsievePad":
                case "ClaspbeanPulp": case "MargincressRibbon": case "AbsentmintLeaf": case "KnitmossPad": case "SootrootPulp":
                    var tonic = item.GetPart<TonicPart>(); var cure = item.GetPart<CureTonicPart>();
                    if (tonic?.ParentEntity != item || !string.IsNullOrEmpty(tonic.StatBoost) || item.HasPart<StatusTonicPart>() || item.HasPart<BrewItemPart>()) return null;
                    if (string.IsNullOrEmpty(tonic.Healing) && string.IsNullOrEmpty(cure?.CureEffect)) return null;
                    return new Payload { Kind = "Tonic", Healing = tonic.Healing, Cure = cure?.CureEffect ?? "", Drink = tonic.Drink,
                        Description = !string.IsNullOrEmpty(tonic.Healing) ? "heal " + tonic.Healing : cure.CureEffect == "All" ? "current ailments" : "cure " + cure.CureEffect.Replace("Effect", "").ToLowerInvariant() };
                default: return null;
            }
        }
        sealed class Payload
        {
            internal string Kind, Healing = "", Cure = "", Stat = "", Description; internal int Bonus, Duration; internal bool Drink;
            internal bool Meal => Kind == "Food" || Kind == "FieldMeal";
            internal string Signature => Kind + ";" + Healing + ";" + Cure + ";" + Drink + ";" + Stat + ";" + Bonus + ";" + Duration;
        }
        static int Quantity(Entity item) => item.GetPart<StackerPart>()?.StackCount ?? 1;
        static string MealState(PreparedMealEffect meal) => meal == null ? "none"
            : meal.StatName + ";" + meal.Bonus + ";" + meal.AppliedBonus + ";" + meal.Duration + ";" + meal.JustApplied;
        static string Escape(string text) => Uri.EscapeDataString(text ?? "");
        static bool Number(string text, out int value) => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value >= 0;
        static bool Reject(Entity actor, Entity item, string reason)
        { Diag.Record("event", "CompanionCareRejected", actor, item, new { reason }); return false; }

        static void TrackMutation(Entity target, Effect effect, InventoryTransaction tx, Action mutation)
        {
            var receipt = new ChangeReceipt(target, effect, target.GetPart<StatusEffectsPart>()); tx.Do(null, receipt.Undo);
            receipt.Before(); try { mutation(); } finally { receipt.After(); }
        }

        // Captures only an intrinsic payload's deltas; external lifecycle observers
        // run after After(). Exact effect fields include the private pacing-goal
        // reference so restored panacea ailments still own their original goal.
        sealed class ChangeReceipt
        {
            readonly Entity target; readonly Effect effect; readonly StatusEffectsPart status;
            Snapshot before, after; int index; bool present;
            bool mealPresentAfter; string mealStatAfter; int mealBonusAfter, mealAppliedAfter;
            Dictionary<FieldInfo, object> fields;
            internal ChangeReceipt(Entity target, Effect effect, StatusEffectsPart status)
            { this.target = target; this.effect = effect; this.status = status; }
            internal void Before()
            {
                before = new Snapshot(target); after = null;
                if (effect == null) return;
                index = status?.GetAllEffects().ToList().IndexOf(effect) ?? -1; present = index >= 0;
                fields = new Dictionary<FieldInfo, object>();
                for (var type = effect.GetType(); type != null && typeof(Effect).IsAssignableFrom(type); type = type.BaseType)
                    foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                        if (!field.IsInitOnly && field.Name != nameof(Effect.Owner)) fields[field] = field.GetValue(effect);
            }
            internal void After()
            {
                if (before == null) return;
                after = new Snapshot(target);
                if (effect is PreparedMealEffect meal)
                {
                    mealPresentAfter = status?.GetAllEffects().Contains(meal) == true;
                    mealStatAfter = meal.StatName; mealBonusAfter = meal.Bonus; mealAppliedAfter = meal.AppliedBonus;
                }
            }
            internal void Undo()
            {
                if (before == null || after == null) return;
                // Prepared meals are one mutable native record. An independent
                // later meal (or removal) already unapplied our preparation.
                // Its new preparation owns that record now; reversing our old
                // shift again would create a negative orphan resistance.
                // Healing has a separate receipt and still rolls back normally.
                if (effect is PreparedMealEffect meal && mealPresentAfter
                    && (!status.GetAllEffects().Contains(meal) || meal.StatName != mealStatAfter
                        || meal.Bonus != mealBonusAfter || meal.AppliedBonus != mealAppliedAfter)) return;
                bool restored = false;
                if (effect != null && status != null)
                {
                    bool contains = status.GetAllEffects().Contains(effect);
                    if (!present) status.RemoveAddedEffectForInventoryUndo(effect);
                    else if (!contains) { status.RestoreRemovedEffectForInventoryUndo(effect, index); restored = true; }
                    foreach (var field in fields) field.Key.SetValue(effect, field.Value);
                }
                before.UndoChanges(after, target);
                if (restored && effect is IAuraProvider aura && target.GetPart<StatusEffectsPart>() == status
                    && target.SpatialZone?.GetEntityCell(target)?.Objects.Contains(target) == true)
                    AsciiFxBus.StartAura(target.SpatialZone, target, aura.GetAuraTheme());
            }
        }
        sealed class Snapshot
        {
            readonly Dictionary<string, (Stat reference, Stat value)> stats;
            readonly Dictionary<(object owner, FieldInfo field), object> values = new Dictionary<(object, FieldInfo), object>();
            readonly BrainPart brain; readonly List<GoalHandler> goals;
            internal Snapshot(Entity target)
            {
                stats = target.Statistics.ToDictionary(p => p.Key, p => (p.Value, new Stat(p.Value)));
                Capture(target.GetPart<ThermalPart>(), nameof(ThermalPart.Temperature));
                Capture(target.GetPart<MaterialPart>(), nameof(MaterialPart.Combustibility));
                Capture(target.GetPart<MeleeWeaponPart>(), nameof(MeleeWeaponPart.HitBonus));
                Capture(target.GetPart<ArmorPart>(), nameof(ArmorPart.AV));
                brain = target.GetPart<BrainPart>(); goals = brain?.GetGoalsSnapshot();
            }
            void Capture(object owner, string name)
            {
                if (owner == null) return;
                var field = owner.GetType().GetField(name); if (field != null) values[(owner, field)] = field.GetValue(owner);
            }
            internal void UndoChanges(Snapshot end, Entity target)
            {
                foreach (var entry in end.stats)
                {
                    var current = target.GetStat(entry.Key); if (current != entry.Value.reference) continue;
                    Stat old = stats.TryGetValue(entry.Key, out var prior) ? prior.value : new Stat(); var last = entry.Value.value;
                    current.BaseValue -= last.BaseValue - old.BaseValue; current.Bonus -= last.Bonus - old.Bonus;
                    current.Penalty -= last.Penalty - old.Penalty; current.Boost -= last.Boost - old.Boost;
                }
                foreach (var entry in values)
                {
                    if (!end.values.TryGetValue(entry.Key, out var last)) continue;
                    var owner = entry.Key.owner; var field = entry.Key.field; var current = field.GetValue(owner);
                    if (current is float f && entry.Value is float oldFloat && last is float endFloat) field.SetValue(owner, f - (endFloat - oldFloat));
                    else if (current is int n && entry.Value is int oldInt && last is int endInt) field.SetValue(owner, n - (endInt - oldInt));
                }
                if (brain == null || target.GetPart<BrainPart>() != brain || end.brain != brain) return;
                var currentGoals = brain.GetGoalsSnapshot();
                for (int i = 0; i < goals.Count; i++)
                    if (!end.goals.Contains(goals[i]) && !currentGoals.Contains(goals[i])) currentGoals.Insert(Math.Min(i, currentGoals.Count), goals[i]);
                brain.RestoreGoalsForLoad(currentGoals);
            }
        }
    }
}
