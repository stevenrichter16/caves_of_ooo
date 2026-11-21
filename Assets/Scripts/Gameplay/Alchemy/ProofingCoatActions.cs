using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Coating a garment from a flask of proofing (Docs/MACHINE-PROOFING.md): the temporary tier. A flask holding an element's proofing offers one row per garment you hold, worn or not and of any
    /// armor, not just leather and cloth. One unit is one coat: <see cref="ProofingRules.CoatAmount"/> of that resistance for <see cref="ProofingRules.CoatTurns"/> worn turns, kept on the garment as an
    /// <see cref="EnhancementSteeped"/> and paused while it is off. The same element tops the turns up to <see cref="ProofingRules.CoatMaxTurns"/> (a coat adds time, never power); another element
    /// replaces what is there, and the row says so. It needs no machine and works at once on a garment you are wearing. The unit and the garment's new state commit together or not at all.
    /// </summary>
    internal static class ProofingCoatActions
    {
        const string Prefix = "CoatGarment|";
        const int MaxRows = 12;

        internal static bool IsCommand(string command) => command != null && command.StartsWith(Prefix, StringComparison.Ordinal);

        /// <summary>Everything the actor holds that could be a garment: the pack, and what is worn (equipping takes an item out of the pack's list).</summary>
        static List<Entity> Holdings(Entity actor)
        {
            var list = new List<Entity>(); var pack = actor?.GetPart<InventoryPart>(); if (pack == null) return list;
            foreach (var item in pack.Objects) if (item != null && !list.Contains(item)) list.Add(item);
            if (pack.EquippedItems != null) foreach (var item in pack.EquippedItems.Values) if (item != null && !list.Contains(item)) list.Add(item);
            return list;
        }

        /// <summary>A garment the actor holds (in the pack, worn or not): armor that is not a creature, held exactly once; a stack of identical garments counts (one unit is split off when it is treated).</summary>
        static bool Held(Entity actor, Entity item)
        {
            var physics = item?.GetPart<PhysicsPart>();
            return item != null && !string.IsNullOrEmpty(item.ID) && item.GetPart<ArmorPart>() != null && !item.HasTag("Creature") && item.SpatialZone == null
                && Holdings(actor).Count(o => o == item || o.ID == item.ID) == 1 && physics?.ParentEntity == item && physics.Takeable && GarmentStacks.WholeOrSplittable(actor, item);
        }

        static bool Worn(Entity actor, Entity item) => actor.GetPart<InventoryPart>()?.EquippedItems?.ContainsValue(item) == true;

        static EnhancementSteeped CoatOf(Entity garment) => garment.Parts.OfType<EnhancementSteeped>().FirstOrDefault();

        /// <summary>The element a flask holds proofing for, with a unit to spend; null for any other flask.</summary>
        static ProofingElement Element(Entity actor, Entity flask, out LiquidVesselPart vessel)
        {
            vessel = flask?.GetPart<LiquidVesselPart>(); var stack = flask?.GetPart<StackerPart>();
            if (vessel == null || vessel.ParentEntity != flask || flask.HasPart<WaterskinPart>() || !WorldResourceActions.Carried(actor, flask) || (stack != null && stack.StackCount != 1)) return null;
            if (vessel.Capacity <= 0 || vessel.Volume < 1 || vessel.Volume > vessel.Capacity) return null;
            return ProofingRegistry.ByLiquid(vessel.LiquidId);
        }

        /// <summary>Null when this garment could be coated with this element now; otherwise the refusal code and the sentence that says why.</summary>
        static string Refusal(Entity garment, ProofingElement element, out string why)
        {
            why = null; var coat = CoatOf(garment);
            if (coat == null && ItemEnhancing.CountEnhancements(garment) >= ItemEnhancing.MAX_ENHANCEMENTS_PER_ITEM) { why = "This piece already holds two treatments."; return "slot-cap"; }
            if (coat != null && ProofingRegistry.Canonical(coat.Kind) == element.Id && coat.TurnsLeft >= ProofingRules.CoatMaxTurns) { why = "It already holds as much " + element.Label + " coat as it can (" + ProofingRules.CoatMaxTurns + " turns)."; return "max-coat"; }
            return null;
        }

        internal static void AddActions(Entity actor, Entity flask, Zone zone, InventoryActionList actions)
        {
            if (actions == null || actor == null || !WorldResourceActions.ActorCurrent(actor, zone)) return;
            var element = Element(actor, flask, out var vessel); if (element == null) return;
            var garments = Holdings(actor).Where(o => o != flask && Held(actor, o)).ToList();
            // What you wear first (the pieces you are about to rely on), then by name; past the cap the menu says how many are not shown instead of hiding them.
            garments.Sort((a, b) => Worn(actor, a) != Worn(actor, b) ? (Worn(actor, a) ? -1 : 1) : string.CompareOrdinal(a.GetDisplayName(), b.GetDisplayName()) is int c && c != 0 ? c : string.CompareOrdinal(a.ID, b.ID));
            if (garments.Count == 0) { actions.AddDisabled("CoatGarment", "coat a garment", "no-garment", "Nothing you carry fits: armor or clothing.", '\0', 16); return; }
            string hint = "Spends one unit of " + element.Label + "-proofing. +" + ProofingRules.CoatAmount + " " + element.Label + " resistance for " + ProofingRules.CoatTurns + " worn turns, paused when taken off, and it blunts " + element.Status + ". Another coat of the same adds turns." + ProofingPressActions.SeenNote(zone, actor, element);
            for (int i = 0; i < garments.Count && i < MaxRows; i++)
            {
                var garment = garments[i]; var coat = CoatOf(garment); string refusal = Refusal(garment, element, out string why);
                string note = coat == null ? "" : ProofingRegistry.Canonical(coat.Kind) == element.Id ? " (adds " + Math.Min(ProofingRules.CoatTurns, ProofingRules.CoatMaxTurns - coat.TurnsLeft) + " turns to " + coat.TurnsLeft + ")" : " (replaces " + coat.TurnsLeft + " turns of " + coat.Label + " coat)";
                string label = "coat " + garment.GetDisplayName() + GarmentStacks.Suffix(garment) + (Worn(actor, garment) ? " (worn)" : "") + note;
                bool counted = coat != null && ProofingRegistry.Canonical(coat.Kind) == element.Id;   // a top-up of a coat you already count changes only the turns
                string rowHint = hint + ProofingPressActions.ResistanceNote(actor, garment, element, ProofingRules.CoatAmount, counted);
                if (refusal == null) actions.AddActionWithHint("CoatGarment", label, Prefix + Uri.EscapeDataString(garment.ID), rowHint, '\0', 16 - i, false, ActionTime.OneAction);
                else actions.AddDisabled("CoatGarment", label, refusal, why, '\0', 16 - i);
            }
            if (garments.Count > MaxRows) actions.AddDisabled("CoatGarment", "coat a garment (" + (garments.Count - MaxRows) + " more not shown)", "more", (garments.Count - MaxRows) + " more garments are not shown: set some aside to see them.", '\0', 3);
        }

        internal static bool TryAct(Entity actor, Entity flask, Zone zone, string command, InventoryTransaction tx)
        {
            if (!IsCommand(command)) return false;
            var fields = command.Split('|'); if (fields.Length != 2) return Reject(actor, flask, "malformed-selection");
            var element = Element(actor, flask, out var vessel); var pack = actor?.GetPart<InventoryPart>();
            if (element == null || pack == null || !WorldResourceActions.ActorCurrent(actor, zone)) return Reject(actor, flask, "unavailable");
            string garmentId = WorldResourceActions.Decode(fields[1]); var garment = Holdings(actor).FirstOrDefault(o => o.ID == garmentId);
            if (garment == null || garment == flask || !Held(actor, garment)) return Reject(actor, flask, "garment-unavailable");
            string refusal = Refusal(garment, element, out string why); if (refusal != null) { MessageLog.Add(why); return Reject(actor, flask, refusal); }
            bool own = tx == null; tx ??= new InventoryTransaction();
            try
            {
                if (!tx.TryClaim(actor, actor, command) || !tx.TryClaim(flask, actor, command) || !tx.TryClaim(garment, actor, command)) return Reject(actor, flask, "in-progress");
                bool worn = Worn(actor, garment); int volumeBefore = vessel.Volume; string liquidBefore = vessel.LiquidId; string garmentName = garment.GetDisplayName();
                var prior = CoatOf(garment); bool hadPrior = prior != null; string priorKind = prior?.Kind; int priorLeft = prior?.TurnsLeft ?? 0, priorTotal = prior?.TurnsTotal ?? 0, priorAmount = prior?.Amount ?? 0;
                bool extend = hadPrior && ProofingRegistry.Canonical(priorKind) == element.Id; int newLeft = 0;
                bool hadLive = worn && EnhancementSteeped.FindEffect(actor, garment.ID) != null;
                // A stack of identical garments gives up one unit, which is treated and put back as its own single item (never stacked with the plain ones).
                var stack = garment.GetPart<StackerPart>(); bool split = !worn && !hadPrior && stack != null && stack.StackCount > 1; int stackBefore = stack?.StackCount ?? 1; Entity target = garment; bool added = false;

                void Restore()
                {
                    // Back to exactly what was there: no coat, or the prior one (kind, amount, turns), on the wearer too when worn.
                    var current = CoatOf(target);
                    if (current != null) ItemEnhancing.Remove(target, "EnhancementSteeped", worn ? actor : null);
                    if (split)
                    {
                        if (added) pack.Objects.Remove(target);
                        if (stack != null) stack.StackCount = stackBefore;
                        pack.RefreshHandlingCarryPenalty();
                    }
                    if (!hadPrior) return;
                    if (!ItemEnhancing.Apply(garment, "EnhancementSteeped")) return;
                    var again = CoatOf(garment); if (again == null) return;
                    again.Kind = priorKind; again.Amount = priorAmount; again.TurnsTotal = priorTotal; again.TurnsLeft = priorLeft;
                    if (worn) again.OnEquipped(actor, garment);
                }

                tx.Do(() =>
                {
                    vessel.Volume = volumeBefore - 1; vessel.LiquidId = vessel.Volume == 0 ? "" : liquidBefore;
                    if (extend)
                    {
                        newLeft = Math.Min(ProofingRules.CoatMaxTurns, priorLeft + ProofingRules.CoatTurns);
                        prior.TurnsLeft = newLeft; prior.TurnsTotal = Math.Max(prior.TurnsTotal, newLeft);
                        var live = worn ? EnhancementSteeped.FindEffect(actor, garment.ID) : null; if (live != null) live.Duration = newLeft; else if (worn) prior.OnEquipped(actor, garment);   // a worn coat whose effect was dispelled gets it back
                        return;
                    }
                    if (hadPrior) ItemEnhancing.Remove(garment, "EnhancementSteeped", worn ? actor : null);
                    if (split) { target = stack.RemoveOne(); if (target == null) { target = garment; return; } }   // one unit off the stack, not yet in the pack
                    if (!ItemEnhancing.Apply(target, "EnhancementSteeped")) return;
                    var fresh = CoatOf(target); if (fresh == null) return;
                    fresh.Kind = element.Id; fresh.Amount = ProofingRules.CoatAmount; fresh.TurnsTotal = ProofingRules.CoatTurns; fresh.TurnsLeft = ProofingRules.CoatTurns; newLeft = fresh.TurnsLeft;
                    if (worn) fresh.OnEquipped(actor, garment);   // applied with the kind and turns already set, so it works at once
                    if (split) added = pack.AddObject(target);   // enhanced first, so it never merges back into the plain stack
                },
                () =>
                {
                    vessel.Volume = volumeBefore; vessel.LiquidId = liquidBefore;
                    if (extend)
                    {
                        prior.TurnsLeft = priorLeft; prior.TurnsTotal = priorTotal;
                        var live = worn ? EnhancementSteeped.FindEffect(actor, garment.ID) : null;
                        if (live != null) { if (hadLive) live.Duration = priorLeft; else prior.OnUnequipped(actor, garment); }
                        return;
                    }
                    Restore();
                });
                var after = CoatOf(target); if (after == null || ProofingRegistry.Canonical(after.Kind) != element.Id || (split && !added)) return Reject(actor, flask, "not-applied");
                tx.AfterCommit(() =>
                {
                    MessageLog.Add(extend ? "You work another coat of " + element.Label + "-proofing into " + garmentName + ": " + newLeft + " turns of wear now."
                        : "You coat " + garmentName + " with " + element.Label + "-proofing. It will turn " + element.Label + " for " + ProofingRules.CoatTurns + " turns of wear." + (hadPrior ? " The " + SteepedAcidEffect.ElementLabel(priorKind) + " coat is gone." : ""));
                    Diag.Record("liquid", "GarmentCoated", actor, target, new { element = element.Id, extended = extend, turns = newLeft, replaced = hadPrior && !extend, flaskLeft = volumeBefore - 1, worn });
                });
                if (own) tx.Commit();
                return true;
            }
            finally { if (own) tx.Rollback(); }
        }

        static bool Reject(Entity actor, Entity target, string reason)
        {
            Diag.Record("liquid", "GarmentCoatRefused", actor, target, new { reason });
            if (actor?.HasTag("Player") == true && reason != "slot-cap" && reason != "max-coat") MessageLog.Add("You cannot coat that now; nothing is spent.");
            return false;
        }
    }
}
