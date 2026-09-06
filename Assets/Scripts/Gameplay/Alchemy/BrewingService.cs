using System;
using System.Collections.Generic;
using System.Text;
using CavesOfOoo.Data;
using CavesOfOoo.Core.Inventory;
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
    /// FALSE means validation/execution failed and local payment/output was restored.
    /// Independent factory callbacks and post-commit publication are outside rollback.
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

        /// <summary>Creates one outcome. A produced item is the actual carried recipient,
        /// which can be an existing stack. Local payment/list changes roll back on
        /// execution refusal; independent preparation callbacks are retained. Publication
        /// (discoveries, prose, outcome diagnostics) follows commit and cannot undo paid output.</summary>
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

            // Freeze explicit selection before any factory/configuration callback.
            reagentItems = new List<Entity>(reagentItems);
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

                if (!inventory.CanConsumeOne(item))
                {
                    reason = "You must own all selected reagents, and each stack must have at least one item.";
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

            var transaction = new InventoryTransaction();
            try
            {
                if (!transaction.TryClaim(crafter, crafter, "Brew"))
                {
                    reason = "Crafting is already in progress.";
                    return RejectDiag(crafter, reason);
                }

                Entity prepared = null;
                switch (brewResult.Kind)
                {
                    case BrewOutcomeKind.Brew:
                        prepared = factory.CreateEntity(BrewBlueprintName);
                        if (prepared != null) ConfigureBrewItem(prepared, brewResult);
                        break;
                    case BrewOutcomeKind.InertSludge:
                        prepared = factory.CreateEntity(SludgeBlueprintName);
                        break;
                    case BrewOutcomeKind.Mishap:
                        break;
                    default:
                        reason = "Unknown brew outcome.";
                        return RejectDiag(crafter, reason);
                }
                if (brewResult.Kind != BrewOutcomeKind.Mishap && prepared == null)
                {
                    reason = "Failed to create '" + (brewResult.Kind == BrewOutcomeKind.Brew ? BrewBlueprintName : SludgeBlueprintName) + "'.";
                    return RejectDiag(crafter, reason);
                }
                if (!ReferenceEquals(inventory, crafter.GetPart<InventoryPart>()))
                {
                    reason = "Crafter inventory changed during preparation.";
                    return RejectDiag(crafter, reason);
                }
                var physics = prepared?.GetPart<PhysicsPart>();
                if (prepared != null && (inventory.Contains(prepared) || physics?.InInventory != null || physics?.Equipped != null))
                {
                    reason = "The prepared output already belongs to an inventory.";
                    return RejectDiag(crafter, reason);
                }
                foreach (var item in reagentItems)
                {
                    if (!inventory.CanConsumeOne(item) || !item.HasPart<ReagentPart>()
                        || !transaction.TryClaim(item, crafter, "Brew"))
                    {
                        reason = "A selected reagent is unavailable.";
                        return RejectDiag(crafter, reason);
                    }
                }

                string unitName = InventoryPart.GetUnitDisplayName(prepared);
                var receipt = InventoryTransferSnapshot.Capture(inventory, prepared);
                transaction.Do(null, receipt.Restore); // Enroll before the first mutation can throw.
                Entity recipient = null;
                bool applied = receipt.Apply(() =>
                {
                    foreach (var item in reagentItems)
                        if (!inventory.TryConsumeOne(item)) return false;
                    return prepared == null || inventory.AddCraftedUnitWithinCapacity(prepared, out recipient);
                });
                if (!applied || !receipt.ClaimChanges(transaction, crafter, "Brew"))
                {
                    reason = "Cannot complete the brew in inventory.";
                    return RejectDiag(crafter, reason);
                }
                transaction.Commit();
                producedItem = recipient;
                if (brewResult.Kind == BrewOutcomeKind.Brew)
                {
                    RecordDiscoveries(crafter, brewResult);
                    MessageLog.Add(crafter.GetDisplayName() + " brews " + unitName + ".");
                }
                else MessageLog.Add(brewResult.Reason);
                EmitResolvedDiag(crafter, brewResult, reagentItems, recipient);
                return true;
            }
            finally { transaction.Rollback(); } // No-op after commit; also releases preparation claims.
        }

        /// <summary>
        /// Pure, read-only preview: the largest batch size a UI could offer
        /// for this exact reagent selection — the smallest available
        /// quantity across all selected reagents (StackerPart.StackCount
        /// when present, else 1, since an unstacked item is "1 unit").
        /// Consumes nothing; <see cref="TryBrewBatch"/> is the authority on
        /// what actually happens.
        /// </summary>
        public static int GetMaxBatchCount(IReadOnlyList<Entity> reagentItems)
        {
            if (reagentItems == null || reagentItems.Count == 0)
                return 0;

            int max = int.MaxValue;
            for (int i = 0; i < reagentItems.Count; i++)
            {
                Entity item = reagentItems[i];
                if (item == null)
                    return 0;

                StackerPart stacker = item.GetPart<StackerPart>();
                int available = stacker?.StackCount ?? 1;
                if (available <= 0) return 0;
                for (int j = 0; j < i; j++)
                    if (ReferenceEquals(reagentItems[j], item)) return 0;
                if (available < max)
                    max = available;
            }

            return max;
        }

        /// <summary>
        /// Repeat <see cref="TryBrew"/> up to <paramref name="requestedCount"/>
        /// times against the SAME reagent selection — the answer to "let me
        /// make several at once from a stack I gathered" without punishing
        /// the player for having gathered a lot. No new consumption logic:
        /// each call to TryBrew independently re-validates ownership and
        /// consumes one unit from each reagent's stack (or removes the
        /// whole entity once its stack reaches 1), so passing the same
        /// entity references repeatedly naturally decrements stacks and
        /// naturally STOPS once any reagent runs out (the next iteration's
        /// ownership check fails) — no separate "how many are left" logic
        /// to keep in sync with TryBrew's local transfer receipt.
        ///
        /// Return contract: TRUE if at least one iteration succeeded — a
        /// partial batch (asked for 5, only had mats for 3) is a smaller
        /// SUCCESS, not a failure. FALSE only when the FIRST iteration
        /// fails, matching TryBrew's own failure contract exactly.
        /// <paramref name="madeCount"/> may be less than requestedCount;
        /// when it is, <paramref name="reason"/> explains why the batch
        /// stopped early (informational — the caller decides how to show
        /// it, e.g. a "ran out of lamp oil" message).
        ///
        /// Deliberately per-iteration, not aggregated: each successful
        /// iteration still runs the full TryBrew path (its own MessageLog
        /// line, its own "alchemy" diag record, its own discovery check —
        /// which is already idempotent, so brewing the same rule 5 times in
        /// one batch discovers it exactly once).
        /// </summary>
        public static bool TryBrewBatch(
            Entity crafter,
            EntityFactory factory,
            IReadOnlyList<Entity> reagentItems,
            int requestedCount,
            out List<Entity> producedItems,
            out List<BrewResult> results,
            out int madeCount,
            out string reason)
        {
            producedItems = new List<Entity>();
            results = new List<BrewResult>();
            madeCount = 0;
            reason = string.Empty;

            if (crafter == null)
            {
                reason = "Crafter is missing.";
                return false;
            }

            if (requestedCount <= 0)
            {
                reason = "Requested brew count must be positive.";
                return RejectDiag(crafter, reason);
            }

            // The entire batch promises one selection, even if a preparation callback
            // changes the caller's list between independently committed iterations.
            if (reagentItems != null) reagentItems = new List<Entity>(reagentItems);
            for (int i = 0; i < requestedCount; i++)
            {
                bool ok = TryBrew(crafter, factory, reagentItems, out Entity produced, out BrewResult result, out string iterationReason);
                if (!ok)
                {
                    if (madeCount == 0)
                    {
                        reason = iterationReason;
                        return false;
                    }

                    reason = "Ran out of reagents after " + madeCount + ": " + iterationReason;
                    break;
                }

                producedItems.Add(produced);
                results.Add(result);
                madeCount++;
            }

            return true;
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
                render.DisplayName = ComposeBrewName(result);
        }

        /// <summary>
        /// Pure, read-only: what this mix WOULD brew into. Consumes
        /// nothing, creates nothing, logs nothing — the Crafting panel
        /// recomputes it on every selection change
        /// (Docs/CRAFTING-FROM-THE-PACK.md §C1).
        ///
        /// <para>This is the change that stops alchemy being a slot
        /// machine: the player sees the resolved effects and form BEFORE
        /// the reagents are gone.</para>
        ///
        /// <para>Naming routes through <see cref="ComposeBrewName"/>, the
        /// same helper the real brew stamps on the item, so preview and
        /// product cannot disagree about what the thing is called.</para>
        /// </summary>
        public static BrewPreview PreviewBrew(IReadOnlyList<Entity> reagentItems)
        {
            var preview = new BrewPreview
            {
                Reason = string.Empty,
                DisplayName = string.Empty,
                Form = string.Empty,
                Properties = string.Empty,
                Effects = System.Array.Empty<BrewPropertyAmount>(),
            };

            if (reagentItems == null || reagentItems.Count == 0)
            {
                preview.Reason = "Pick reagents to see what they make.";
                return preview;
            }

            var propertyLists = new List<IReadOnlyList<BrewPropertyAmount>>(reagentItems.Count);
            for (int i = 0; i < reagentItems.Count; i++)
            {
                var reagent = reagentItems[i]?.GetPart<ReagentPart>();
                if (reagent == null)
                {
                    preview.Reason = (reagentItems[i]?.GetDisplayName() ?? "That")
                        + " is not a reagent.";
                    return preview;
                }

                if ((reagentItems[i].GetPart<StackerPart>()?.StackCount ?? 1) <= 0)
                {
                    preview.Reason = "A selected reagent is empty. Remove that pick.";
                    return preview;
                }
                for (int j = 0; j < i; j++)
                    if (ReferenceEquals(reagentItems[j], reagentItems[i]))
                    {
                        preview.Reason = "The same reagent was selected twice.";
                        return preview;
                    }

                propertyLists.Add(reagent.GetProperties());
            }

            BrewResult result = BrewResolver.Resolve(propertyLists);
            if (result == null || !result.IsBrew)
            {
                preview.Reason = string.IsNullOrEmpty(result?.Reason)
                    ? "These reagents make nothing." : result.Reason;
                return preview;
            }

            preview.IsValid = true;
            preview.Form = result.Form;
            preview.DisplayName = ComposeBrewName(result);

            var effects = new List<BrewPropertyAmount>(result.Effects.Count);
            var flat = new StringBuilder();
            for (int i = 0; i < result.Effects.Count; i++)
            {
                BrewEffect effect = result.Effects[i];
                int potency = Math.Max(1, effect.Potency);
                effects.Add(new BrewPropertyAmount(effect.Effect, potency));
                if (flat.Length > 0) flat.Append(' ');
                flat.Append(effect.Effect).Append(':').Append(potency);
            }

            preview.Effects = effects;
            preview.Properties = flat.ToString();
            return preview;
        }

        /// <summary>
        /// The brew's display name, derived from its resolved effects.
        /// Shared by <see cref="PreviewBrew"/> and the real brew so the
        /// two cannot drift.
        /// </summary>
        private static string ComposeBrewName(BrewResult result)
        {
            var nameParts = new List<string>(result.Effects.Count);
            for (int i = 0; i < result.Effects.Count; i++)
            {
                BrewEffect effect = result.Effects[i];
                nameParts.Add(
                    string.Equals(effect.Effect, HealingEffectName, StringComparison.OrdinalIgnoreCase)
                        ? "mending"
                        : effect.Effect.ToLowerInvariant());
            }

            return nameParts.Count > 0
                ? string.Join(" & ", nameParts) + " " + FormNoun(result.Form)
                : string.Empty;
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

    }
}
