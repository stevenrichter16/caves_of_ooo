using System;
using System.Collections.Generic;
using System.Text;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Application service for brewing: validates a reagent selection,
    /// resolves it through <see cref="BrewResolver"/> (pure), then executes
    /// the world side effects — consume reagents (atomically, stack-aware,
    /// with rollback), create the output item, record discoveries, emit
    /// "alchemy" diag records. Mirrors TinkeringService's validate-first /
    /// rollback-on-partial-failure shape.
    ///
    /// Return contract: TRUE means the brewing ACT completed — including a
    /// Mishap or InertSludge outcome (reagents consumed, outcome in
    /// <paramref name="brewResult"/>.Kind; producedItem is null for Mishap).
    /// FALSE means validation/execution failed and NO state changed.
    ///
    /// Mishap physical consequence (small telegraphed self-damage, §6.3) is
    /// applied at the command/UI layer where zone context lives — M1.3. The
    /// service reports the Mishap outcome; it does not deal damage.
    /// </summary>
    public static class BrewingService
    {
        public const string BrewBlueprintName = "BrewedTonic";
        public const string SludgeBlueprintName = "InertSludge";
        public const string DiagCategory = "alchemy";

        /// <summary>Effect name (from brew rules) that maps to instant TonicPart healing dice instead of a status effect.</summary>
        public const string HealingEffectName = "Healing";

        private struct ConsumedReagent
        {
            public Entity Entity;
            public bool ConsumedFromStack;
        }

        public static bool TryBrew(
            Entity crafter,
            EntityFactory factory,
            IReadOnlyList<Entity> reagentItems,
            out Entity producedItem,
            out BrewResult brewResult,
            out string reason)
        {
            producedItem = null;
            brewResult = null;
            reason = string.Empty;

            if (crafter == null)
            {
                reason = "Crafter is missing.";
                return false;
            }

            if (factory == null)
            {
                reason = "Entity factory is missing.";
                return RejectDiag(crafter, reason);
            }

            InventoryPart inventory = crafter.GetPart<InventoryPart>();
            if (inventory == null)
            {
                reason = "Crafter cannot brew without an inventory.";
                return RejectDiag(crafter, reason);
            }

            if (reagentItems == null || reagentItems.Count == 0)
            {
                reason = "No reagents selected.";
                return RejectDiag(crafter, reason);
            }

            var reagentProperties = new List<IReadOnlyList<BrewPropertyAmount>>(reagentItems.Count);
            for (int i = 0; i < reagentItems.Count; i++)
            {
                Entity item = reagentItems[i];
                if (item == null)
                {
                    reason = "A selected reagent is missing.";
                    return RejectDiag(crafter, reason);
                }

                // The same physical entity twice is a caller bug (a stacked
                // entity passed once contributes once); catch it before any
                // mutation so consume/rollback stays coherent.
                for (int j = 0; j < i; j++)
                {
                    if (ReferenceEquals(reagentItems[j], item))
                    {
                        reason = "The same reagent was selected twice.";
                        return RejectDiag(crafter, reason);
                    }
                }

                if (!inventory.Contains(item))
                {
                    reason = "You must own all selected reagents.";
                    return RejectDiag(crafter, reason);
                }

                ReagentPart reagent = item.GetPart<ReagentPart>();
                if (reagent == null)
                {
                    reason = item.GetDisplayName() + " is not a reagent.";
                    return RejectDiag(crafter, reason);
                }

                reagentProperties.Add(reagent.GetProperties());
            }

            brewResult = BrewResolver.Resolve(reagentProperties);
            if (brewResult.Kind == BrewOutcomeKind.Invalid)
            {
                reason = brewResult.Reason;
                return RejectDiag(crafter, reason);
            }

            // ── Execution: consume all reagents atomically ──
            var consumed = new List<ConsumedReagent>(reagentItems.Count);
            for (int i = 0; i < reagentItems.Count; i++)
            {
                if (!TryConsumeReagent(inventory, reagentItems[i], out ConsumedReagent record))
                {
                    RestoreConsumed(inventory, consumed);
                    reason = "Failed to consume " + reagentItems[i].GetDisplayName() + ".";
                    return RejectDiag(crafter, reason);
                }

                consumed.Add(record);
            }

            switch (brewResult.Kind)
            {
                case BrewOutcomeKind.Mishap:
                    MessageLog.Add(brewResult.Reason);
                    EmitResolvedDiag(crafter, brewResult, reagentItems, null);
                    return true;

                case BrewOutcomeKind.InertSludge:
                {
                    Entity sludge = factory.CreateEntity(SludgeBlueprintName);
                    if (sludge == null || !inventory.AddObject(sludge))
                    {
                        RestoreConsumed(inventory, consumed);
                        reason = "Failed to create '" + SludgeBlueprintName + "'.";
                        return RejectDiag(crafter, reason);
                    }

                    producedItem = sludge;
                    MessageLog.Add(brewResult.Reason);
                    EmitResolvedDiag(crafter, brewResult, reagentItems, sludge);
                    return true;
                }

                case BrewOutcomeKind.Brew:
                {
                    Entity brew = factory.CreateEntity(BrewBlueprintName);
                    if (brew == null)
                    {
                        RestoreConsumed(inventory, consumed);
                        reason = "Failed to create '" + BrewBlueprintName + "'.";
                        return RejectDiag(crafter, reason);
                    }

                    ConfigureBrewItem(brew, brewResult);

                    if (!inventory.AddObject(brew))
                    {
                        RestoreConsumed(inventory, consumed);
                        reason = "Cannot add the brew to inventory.";
                        return RejectDiag(crafter, reason);
                    }

                    producedItem = brew;
                    RecordDiscoveries(crafter, brewResult);
                    MessageLog.Add(crafter.GetDisplayName() + " brews " + brew.GetDisplayName() + ".");
                    EmitResolvedDiag(crafter, brewResult, reagentItems, brew);
                    return true;
                }

                default:
                    RestoreConsumed(inventory, consumed);
                    reason = "Unknown brew outcome.";
                    return RejectDiag(crafter, reason);
            }
        }

        /// <summary>
        /// Write the resolved effects onto the created brew entity:
        /// "Healing" entries become instant TonicPart healing dice (potency
        /// d4); everything else becomes BrewItemPart status entries. The
        /// display name is derived from the effects + form so the item
        /// self-describes ("burning coating", "acidic & electrified tonic").
        /// </summary>
        private static void ConfigureBrewItem(Entity brew, BrewResult result)
        {
            var statusEntries = new StringBuilder();
            var nameParts = new List<string>(result.Effects.Count);
            int healingPotency = 0;

            for (int i = 0; i < result.Effects.Count; i++)
            {
                BrewEffect effect = result.Effects[i];
                if (string.Equals(effect.Effect, HealingEffectName, StringComparison.OrdinalIgnoreCase))
                {
                    healingPotency = Math.Max(healingPotency, Math.Max(1, effect.Potency));
                    nameParts.Add("mending");
                    continue;
                }

                if (statusEntries.Length > 0)
                    statusEntries.Append(';');
                statusEntries.Append(effect.Effect).Append(':').Append(Math.Max(1, effect.Potency));
                nameParts.Add(effect.Effect.ToLowerInvariant());
            }

            if (healingPotency > 0)
            {
                TonicPart tonic = brew.GetPart<TonicPart>();
                if (tonic == null)
                {
                    tonic = new TonicPart();
                    brew.AddPart(tonic);
                }

                tonic.Healing = healingPotency + "d4";
            }

            if (statusEntries.Length > 0)
            {
                brew.AddPart(new BrewItemPart
                {
                    EffectsRaw = statusEntries.ToString(),
                    Form = result.Form
                });
            }

            RenderPart render = brew.GetPart<RenderPart>();
            if (render != null && nameParts.Count > 0)
                render.DisplayName = string.Join(" & ", nameParts) + " " + FormNoun(result.Form);
        }

        private static string FormNoun(string form)
        {
            if (string.Equals(form, "Coating", StringComparison.OrdinalIgnoreCase))
                return "coating";
            if (string.Equals(form, "Throwable", StringComparison.OrdinalIgnoreCase))
                return "flask";
            if (string.Equals(form, "Food", StringComparison.OrdinalIgnoreCase))
                return "morsel";
            return "tonic";
        }

        private static void RecordDiscoveries(Entity crafter, BrewResult result)
        {
            BrewKnowledgePart knowledge = crafter.GetPart<BrewKnowledgePart>();
            if (knowledge == null)
            {
                knowledge = new BrewKnowledgePart();
                crafter.AddPart(knowledge);
            }

            for (int i = 0; i < result.Effects.Count; i++)
            {
                string ruleId = result.Effects[i].RuleId;
                if (!knowledge.Discover(ruleId))
                    continue;

                string description = null;
                if (BrewRuleRegistry.TryGetRule(ruleId, out BrewRule rule))
                    description = rule.Description;

                MessageLog.Add(
                    "Discovery: " + (string.IsNullOrWhiteSpace(description) ? ruleId : description));

                if (Diag.IsChannelEnabled(DiagCategory))
                {
                    Diag.Record(DiagCategory, "BrewDiscovered", actor: crafter, payload: new
                    {
                        ruleId,
                        effect = result.Effects[i].Effect
                    });
                }
            }
        }

        private static void EmitResolvedDiag(
            Entity crafter,
            BrewResult result,
            IReadOnlyList<Entity> reagentItems,
            Entity produced)
        {
            if (!Diag.IsChannelEnabled(DiagCategory))
                return;

            var effectSummary = new StringBuilder();
            var ruleIds = new List<string>(result.Effects.Count);
            for (int i = 0; i < result.Effects.Count; i++)
            {
                if (effectSummary.Length > 0)
                    effectSummary.Append(';');
                effectSummary.Append(result.Effects[i].Effect).Append(':').Append(result.Effects[i].Potency);
                ruleIds.Add(result.Effects[i].RuleId);
            }

            var reagentNames = new List<string>(reagentItems.Count);
            for (int i = 0; i < reagentItems.Count; i++)
                reagentNames.Add(reagentItems[i]?.BlueprintName);

            Diag.Record(DiagCategory, "BrewResolved", actor: crafter, target: produced, payload: new
            {
                outcome = result.Kind.ToString(),
                form = result.Form,
                effects = effectSummary.ToString(),
                rules = string.Join(",", ruleIds),
                reagents = string.Join(",", reagentNames)
            });
        }

        private static bool RejectDiag(Entity crafter, string reason)
        {
            if (Diag.IsChannelEnabled(DiagCategory))
                Diag.Record(DiagCategory, "BrewRejected", actor: crafter, payload: new { reason });
            return false;
        }

        private static bool TryConsumeReagent(InventoryPart inventory, Entity item, out ConsumedReagent record)
        {
            record = new ConsumedReagent();
            if (inventory == null || item == null)
                return false;

            StackerPart stacker = item.GetPart<StackerPart>();
            if (stacker != null && stacker.StackCount > 1)
            {
                stacker.StackCount -= 1;
                record.Entity = item;
                record.ConsumedFromStack = true;
                return true;
            }

            if (inventory.RemoveObject(item))
            {
                record.Entity = item;
                record.ConsumedFromStack = false;
                return true;
            }

            return false;
        }

        private static void RestoreConsumed(InventoryPart inventory, List<ConsumedReagent> consumed)
        {
            if (inventory == null || consumed == null)
                return;

            for (int i = 0; i < consumed.Count; i++)
            {
                ConsumedReagent record = consumed[i];
                if (record.Entity == null)
                    continue;

                if (record.ConsumedFromStack)
                {
                    StackerPart stacker = record.Entity.GetPart<StackerPart>();
                    if (stacker != null)
                    {
                        stacker.StackCount += 1;
                        continue;
                    }
                }

                if (!inventory.Contains(record.Entity))
                    inventory.AddObject(record.Entity);
            }
        }
    }
}
