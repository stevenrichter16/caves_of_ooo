using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// The proofing trough (Docs/MACHINE-PROOFING.md; once the steeping trough of MACHINE-INVENTED-USES.md 3h and 3l): a long tarred trough with a pot of clay slip, set down like the other camp kits. It does the
    /// permanent tier. A garment you hold, worn or not and of any armor, gets one lasting proofing against one element (<see cref="EnhancementProofed"/>: +25, never wears out) for
    /// <see cref="ProofingRules.PermanentUnits"/> units of that element's proofing, drawn from your flasks, and <see cref="ProofingRules.PermanentClay"/> fire clay (what the still bakes from bog-mire, so the
    /// chain mire, fire, clay, trough stays alive). The short-term work is the bottle's: pressing a reagent into a flask and coating a garment from it needs no machine.
    /// <para>Gear only: armor that is not a creature, so a person, body or specimen can never qualify. Its word is "proof"; "bath" belongs to the Curation and the Bower and never appears in what the player reads.
    /// A garment holds one lasting proofing, and it takes one of the garment's two treatment slots. While carried it is a kit, not a station.</para>
    /// </summary>
    public sealed class SteepTroughPart : Part, IDescribeState
    {
        public override string Name => "SteepTrough";

        public const string Clay = "FireClay", Prefix = "ProofGarment|", UnpickPrefix = "UnpickGarment|";
        public const int MaxRows = 12, MaxUnpickRows = 6;

        public static bool IsCommand(string command) => command != null && command.StartsWith(Prefix, StringComparison.Ordinal);
        static bool IsUnpick(string command) => command != null && command.StartsWith(UnpickPrefix, StringComparison.Ordinal);

        bool Standing => ParentEntity?.GetPart<PhysicsPart>()?.InInventory == null && !PackableFixturePart.IsInertKit(ParentEntity);

        // ── what you hold ──

        static List<Entity> Holdings(Entity actor)
        {
            var list = new List<Entity>(); var pack = actor?.GetPart<InventoryPart>(); if (pack == null) return list;
            foreach (var item in pack.Objects) if (item != null && !list.Contains(item)) list.Add(item);
            if (pack.EquippedItems != null) foreach (var item in pack.EquippedItems.Values) if (item != null && !list.Contains(item)) list.Add(item);
            return list;
        }

        static bool Worn(Entity actor, Entity item) => actor.GetPart<InventoryPart>()?.EquippedItems?.ContainsValue(item) == true;

        static bool Fits(Entity actor, Entity item)
        {
            var physics = item?.GetPart<PhysicsPart>();
            return item != null && item.GetPart<ArmorPart>() != null && !item.HasTag("Creature") && item.SpatialZone == null && physics?.ParentEntity == item && physics.Takeable
                && GarmentStacks.WholeOrSplittable(actor, item) && Holdings(actor).Count(o => o == item || o.ID == item.ID) == 1;
        }

        static List<Entity> Eligible(Entity actor)
        {
            var list = Holdings(actor).Where(o => Fits(actor, o)).ToList();
            list.Sort((a, b) => string.CompareOrdinal(a.GetDisplayName(), b.GetDisplayName()) is int c && c != 0 ? c : string.CompareOrdinal(a.ID, b.ID));
            return list;
        }

        /// <summary>The flasks that hold this element's proofing, whole flasks only (a stack of flasks or a malformed vessel is left alone).</summary>
        static List<LiquidVesselPart> Flasks(Entity actor, ProofingElement element)
        {
            var list = new List<LiquidVesselPart>(); var pack = actor?.GetPart<InventoryPart>(); if (pack == null) return list;
            foreach (var item in pack.Objects)
            {
                var vessel = item?.GetPart<LiquidVesselPart>(); var stack = item?.GetPart<StackerPart>();
                if (vessel == null || vessel.ParentEntity != item || item.HasPart<WaterskinPart>() || vessel.Volume < 1 || vessel.Volume > vessel.Capacity || vessel.LiquidId != element.Liquid) continue;
                if (stack != null && stack.StackCount != 1) continue;
                list.Add(vessel);
            }
            return list;
        }

        static int UnitsCarried(Entity actor, ProofingElement element) => Flasks(actor, element).Sum(v => v.Volume);

        static int ClayCarried(Entity actor)
        {
            var inv = actor?.GetPart<InventoryPart>(); if (inv == null) return 0; int units = 0;
            foreach (var item in inv.Objects) if (item != null && item.BlueprintName == Clay && inv.CanConsumeOne(item)) units += item.GetPart<StackerPart>()?.StackCount ?? 1;
            return units;
        }

        static EnhancementProofed ProofedOf(Entity item) => item.Parts.OfType<EnhancementProofed>().FirstOrDefault();

        // ── reader ──

        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 40;

        public string DescribeState(DescribeContext context)
        {
            if (!Standing) return null;
            string cost = ProofingRules.PermanentUnits + " units of an element's proofing and " + ProofingRules.PermanentClay + " fire clay";
            string recipes = "Proofs a garment against heat, cold, shock or acid for good (+" + ProofingRules.PermanentAmount + " resistance, never wears out) for " + cost + ". A coat from a flask of proofing does the short-term work and needs no trough.";
            var viewer = context.Viewer; if (viewer == null) return recipes;
            var have = new List<string>(); foreach (var element in ProofingRegistry.All) { int units = UnitsCarried(viewer, element); if (units > 0) have.Add(units + " " + element.Liquid); }
            int clay = ClayCarried(viewer); int garments = Eligible(viewer).Count;
            string seen = ElementalThreats.Summary(context.Zone ?? ParentEntity?.SpatialZone, viewer);
            int proofed = Holdings(viewer).Count(o => Fits(viewer, o) && ProofedOf(o) != null);
            if (proofed > 0) recipes += " " + proofed + (proofed == 1 ? " garment you hold has" : " garments you hold have") + " a lasting proofing: unpicking one frees its slot and returns nothing.";
            return recipes + " You carry " + (have.Count == 0 ? "no proofing" : string.Join(", ", have)) + " and " + clay + " fire clay; " + garments + (garments == 1 ? " garment" : " garments") + " could be proofed." + (seen.Length > 0 ? " " + seen : "");
        }

        // ── rows ──

        /// <summary>Null when this garment could be proofed against this element now; otherwise the refusal code and the sentence that says why.</summary>
        static string Refusal(Entity actor, Entity garment, ProofingElement element, out string why)
        {
            why = null; var done = ProofedOf(garment);
            if (done != null) { why = "It is already proofed against " + done.Label + " for good; a garment keeps one."; return "already-proofed"; }
            if (ItemEnhancing.CountEnhancements(garment) >= ItemEnhancing.MAX_ENHANCEMENTS_PER_ITEM) { why = "This piece already holds two treatments."; return "slot-cap"; }
            int units = UnitsCarried(actor, element), clay = ClayCarried(actor);
            if (units < ProofingRules.PermanentUnits) { why = "You need " + ProofingRules.PermanentUnits + " units of " + element.Liquid + "; you carry " + units + "."; return "short-units"; }
            if (clay < ProofingRules.PermanentClay) { why = "You need " + ProofingRules.PermanentClay + " fire clay; you carry " + clay + "."; return "short-clay"; }
            return null;
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions") { AddRows(e.GetParameter<Entity>("Actor"), e.GetParameter<Zone>("Zone") ?? e.GetParameter<Entity>("Actor")?.SpatialZone, e.GetParameter<InventoryActionList>("Actions")); return true; }
            if (e.ID == "InventoryAction" && IsCommand(e.GetStringParameter("Command")))
            {
                var actor = e.GetParameter<Entity>("Actor"); var zone = e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone;
                if (Proof(actor, zone, e.GetStringParameter("Command"), e.GetParameter<InventoryTransaction>("InventoryTransaction"))) { e.Handled = true; return false; }
            }
            if (e.ID == "InventoryAction" && IsUnpick(e.GetStringParameter("Command")))
            {
                var actor = e.GetParameter<Entity>("Actor"); var zone = e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone;
                if (Unpick(actor, zone, e.GetStringParameter("Command"), e.GetParameter<InventoryTransaction>("InventoryTransaction"))) { e.Handled = true; return false; }
            }
            return true;
        }

        void AddRows(Entity actor, Zone zone, InventoryActionList actions)
        {
            if (actions == null || actor == null || zone == null || !Standing || !WorldResourceActions.Nearby(actor, ParentEntity, zone)) return;
            var garments = Eligible(actor);
            AddUnpickRows(actor, garments, actions);
            if (garments.Count == 0) { actions.AddDisabled("ProofGarment", "proof a garment for good", "no-garment", "Nothing you carry fits: armor or clothing.", 'p', 20); return; }
            var elements = ProofingRegistry.All.Where(el => UnitsCarried(actor, el) > 0).ToList();
            if (elements.Count == 0)
            {
                actions.AddDisabled("ProofGarment", "proof a garment for good", "no-proofing", "You carry no proofing. Press a reagent into a flask first (heat, cold, conductive and corrosive reagents each make their own); a lasting proofing takes "
                    + ProofingRules.PermanentUnits + " units of one element and " + ProofingRules.PermanentClay + " fire clay.", 'p', 20);
                return;
            }
            // Every garment x element, the ones you could use first (what you wear before what you carry), then the refused ones; the overflow is counted, not hidden.
            var rows = new List<(Entity garment, ProofingElement element, string refusal, string why)>();
            foreach (var garment in garments) foreach (var element in elements) { string refusal = Refusal(actor, garment, element, out string why); rows.Add((garment, element, refusal, why)); }
            rows = rows.Select((r, index) => (r, index)).OrderBy(x => x.r.refusal != null).ThenBy(x => !Worn(actor, x.r.garment)).ThenBy(x => x.index).Select(x => x.r).ToList();
            for (int shown = 0; shown < rows.Count && shown < MaxRows; shown++)
            {
                var (garment, element, refusal, why) = rows[shown];
                string label = "proof " + garment.GetDisplayName() + GarmentStacks.Suffix(garment) + (Worn(actor, garment) ? " (worn)" : "") + " against " + element.Label + " for good (" + ProofingRules.PermanentUnits + " " + element.Liquid + " + " + ProofingRules.PermanentClay + " fire clay)";
                string hint = "+" + ProofingRules.PermanentAmount + " " + element.Label + " resistance for as long as it lasts, and it blunts " + element.Status + ". Uses " + ProofingRules.PermanentUnits + " units of " + element.Liquid + " from your flasks and " + ProofingRules.PermanentClay + " fire clay. A garment keeps one lasting proofing." + ProofingPressActions.ResistanceNote(actor, garment, element, ProofingRules.PermanentAmount, false);
                string command = Prefix + Uri.EscapeDataString(garment.ID) + "|" + element.Id; char key = shown == 0 ? 'p' : '\0'; int priority = 20 - Math.Min(shown, 15);
                if (refusal == null) actions.AddActionWithHint("ProofGarment", label, command, hint, key, priority, false, ActionTime.OneAction);
                else actions.AddDisabled("ProofGarment", label, refusal, why, key, priority);
            }
            if (rows.Count > MaxRows) actions.AddDisabled("ProofGarment", "proof a garment for good (" + (rows.Count - MaxRows) + " more not shown)", "more", (rows.Count - MaxRows) + " more choices are not shown: set some garments aside to see them.", '\0', 4);
        }

        /// <summary>Rows to take a lasting proofing back out: one per garment that has one (worn ones first), so a wrong choice is never a permanent loss of a slot.</summary>
        void AddUnpickRows(Entity actor, List<Entity> garments, InventoryActionList actions)
        {
            var proofed = garments.Where(g => ProofedOf(g) != null).OrderBy(g => !Worn(actor, g)).ToList();
            for (int i = 0; i < proofed.Count && i < MaxUnpickRows; i++)
            {
                var garment = proofed[i]; var done = ProofedOf(garment);
                string label = "unpick the " + done.Label + " proofing from " + garment.GetDisplayName() + (Worn(actor, garment) ? " (worn)" : "");
                string hint = "Takes the lasting proofing back out, so the garment's slot is free to be proofed again. The proofing and what went into it are lost: nothing is returned." + (Worn(actor, garment) ? " Its +" + done.Amount + " " + done.Label + " resistance goes with it." : "");
                actions.AddActionWithHint("UnpickGarment", label, UnpickPrefix + Uri.EscapeDataString(garment.ID), hint, '\0', 5 - Math.Min(i, 4), false, ActionTime.OneAction);
            }
            if (proofed.Count > MaxUnpickRows) actions.AddDisabled("UnpickGarment", "unpick a proofing (" + (proofed.Count - MaxUnpickRows) + " more not shown)", "more", (proofed.Count - MaxUnpickRows) + " more proofed garments are not shown: set some aside to see them.", '\0', 0);
        }

        bool Unpick(Entity actor, Zone zone, string command, InventoryTransaction tx)
        {
            string[] f = command.Split('|'); var inv = actor?.GetPart<InventoryPart>();
            if (actor == null || zone == null || inv == null || !Standing || f.Length != 2 || !WorldResourceActions.Nearby(actor, ParentEntity, zone)) return Block(actor, "invalid-context", "You cannot unpick anything here now.");
            string garmentId = WorldResourceActions.Decode(f[1]); var garment = Holdings(actor).FirstOrDefault(o => o.ID == garmentId);
            if (garment == null || !Fits(actor, garment)) return Block(actor, "stale-selection", "That is no longer something you hold.");
            var done = ProofedOf(garment); if (done == null) return Block(actor, "not-proofed", "It has no lasting proofing to unpick.");
            bool own = tx == null; tx ??= new InventoryTransaction();
            try
            {
                if (!tx.TryClaim(ParentEntity, actor, command) || !tx.TryClaim(garment, actor, command)) return Block(actor, "in-progress", "That is already being worked.");
                bool worn = Worn(actor, garment); string kind = done.Kind; int amount = done.Amount; string label = done.Label;
                tx.Do(() => ItemEnhancing.Remove(garment, "EnhancementProofed", worn ? actor : null),
                      () =>
                      {
                          if (ProofedOf(garment) != null || !ItemEnhancing.Apply(garment, "EnhancementProofed")) return;
                          var back = ProofedOf(garment); if (back == null) return;
                          back.Kind = kind; back.Amount = amount; if (worn) back.OnEquipped(actor, garment);
                      });
                if (ProofedOf(garment) != null) return Block(actor, "not-removed", "The proofing would not come out.");
                string name = garment.GetDisplayName();
                tx.AfterCommit(() =>
                {
                    MessageLog.Add("You pick the " + label + " proofing out of " + name + ". Its slot is free again.");
                    Diag.Record("furniture", "GarmentUnpicked", actor, ParentEntity, new { garment = garment.BlueprintName, element = kind, worn });
                });
                if (own) tx.Commit();
                return true;
            }
            finally { if (own) tx.Rollback(); }
        }

        // ── the proofing: one action; the units, the clay and the garment's new state come back together on any failure ──

        bool Proof(Entity actor, Zone zone, string command, InventoryTransaction tx)
        {
            string[] f = command.Split('|'); var inv = actor?.GetPart<InventoryPart>();
            if (actor == null || zone == null || inv == null || !Standing || f.Length != 3 || !WorldResourceActions.Nearby(actor, ParentEntity, zone)) return Block(actor, "invalid-context", "You cannot proof anything here now.");
            var element = ProofingRegistry.ById(f[2]); string garmentId = WorldResourceActions.Decode(f[1]);
            var garment = Holdings(actor).FirstOrDefault(o => o.ID == garmentId);
            if (element == null || garment == null || !Fits(actor, garment)) return Block(actor, "stale-selection", "That is no longer something you hold.");
            string refusal = Refusal(actor, garment, element, out string why); if (refusal != null) return Block(actor, refusal, why);
            bool own = tx == null; tx ??= new InventoryTransaction();
            try
            {
                if (!tx.TryClaim(ParentEntity, actor, command) || !tx.TryClaim(garment, actor, command)) return Block(actor, "in-progress", "That is already being worked.");
                bool worn = Worn(actor, garment); var stack = garment.GetPart<StackerPart>(); bool split = !worn && stack != null && stack.StackCount > 1; int stackBefore = stack?.StackCount ?? 1; Entity target = garment; bool added = false;
                var flasks = Flasks(actor, element); var before = flasks.Select(v => (v, v.Volume, v.LiquidId)).ToList();
                foreach (var v in flasks) if (!tx.TryClaim(v.ParentEntity, actor, command)) return Block(actor, "in-progress", "That is already being worked.");
                var receipt = InventoryTransferSnapshot.Capture(inv);
                tx.Do(null, receipt.Restore);
                bool PayClay()
                {
                    for (int n = 0; n < ProofingRules.PermanentClay; n++)
                    {
                        var clay = inv.Objects.FirstOrDefault(o => o != null && o.BlueprintName == Clay && inv.CanConsumeOne(o)); if (clay == null || !tx.TryClaim(clay, actor, command) || !inv.TryConsumeOne(clay)) return false;
                    }
                    return true;
                }
                if (!receipt.Apply(PayClay) || !receipt.ClaimChanges(tx, actor, command)) return Block(actor, "payment-refused", "You could not set the clay out.");
                tx.Do(() =>
                {
                    int need = ProofingRules.PermanentUnits;
                    foreach (var v in flasks) { int take = Math.Min(need, v.Volume); v.Volume -= take; if (v.Volume == 0) v.LiquidId = ""; need -= take; if (need == 0) break; }
                    if (split) { target = stack.RemoveOne(); if (target == null) { target = garment; return; } }   // one unit off the stack, not yet in the pack
                    if (!ItemEnhancing.Apply(target, "EnhancementProofed")) return;
                    var fresh = ProofedOf(target); if (fresh == null) return;
                    fresh.Kind = element.Id; fresh.Amount = ProofingRules.PermanentAmount;
                    if (worn) fresh.OnEquipped(actor, garment);   // set up with its kind and amount first, so it works at once
                    if (split) added = inv.AddObject(target);   // enhanced first, so it never merges back into the plain stack
                },
                () =>
                {
                    foreach (var (v, volume, liquid) in before) { v.Volume = volume; v.LiquidId = liquid; }
                    if (ProofedOf(target) != null) ItemEnhancing.Remove(target, "EnhancementProofed", worn ? actor : null);
                    if (split)
                    {
                        if (added) inv.Objects.Remove(target);
                        if (stack != null) stack.StackCount = stackBefore;
                        inv.RefreshHandlingCarryPenalty();
                    }
                });
                if (ProofedOf(target) == null || (split && !added)) return Block(actor, "not-applied", "The proofing did not take.");
                string garmentName = garment.GetDisplayName();
                tx.AfterCommit(() =>
                {
                    MessageLog.Add("You work the proofing into " + garmentName + ". It will turn " + element.Label + " for good.");
                    Diag.Record("furniture", "GarmentProofed", actor, ParentEntity, new { garment = target.BlueprintName, element = element.Id, worn });
                });
                if (own) tx.Commit();
                return true;
            }
            finally { if (own) tx.Rollback(); }
        }

        bool Block(Entity actor, string code, string message)
        {
            MessageLog.Add(message);
            Diag.Record("furniture", "ProofRefused", actor: actor, target: ParentEntity, payload: new { code });
            return false;
        }
    }
}
