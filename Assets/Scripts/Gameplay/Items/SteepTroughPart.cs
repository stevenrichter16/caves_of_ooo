using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// The steeping trough (Docs/MACHINE-INVENTED-USES.md 3h; the V1 of Docs/MachineDesign/machine-bath.md): a long tarred trough with a pot of clay slip, set down like the other camp kits. A garment steeped
    /// in it, with one pitchpod resin and one fire clay, turns acid for a while: +35 acid resistance for 100 turns of wear (<see cref="EnhancementSteeped"/>). The reagents are used up whether or not
    /// acid is ever met; there is no refund and no byproduct; the garment is not consumed. It is shared and asks nothing.
    /// <para><b>Clay-wash (Docs/MACHINE-INVENTED-USES.md 3l).</b> The same trough also gives a garment a clay-wash against fire: 2 fire clay for +35 heat resistance for 100 turns of wear, the
    /// same shape as the steeping, and a garment holds one or the other (a new one replaces it). Fire clay is what the still bakes from bog-mire, so the chain is mire, hearth fuel, clay, garment.</para>
    /// <para>Gear only: the trough offers rows for carried, unworn, unstacked garments of leather or cloth, and for nothing else, so a person, body or specimen can never qualify. Its word is
    /// "steep"; the word "bath" belongs to the Curation and the Bower and never appears in what the player reads.</para>
    /// <para>While carried it is a kit, not a station; it offers its rows only when set down and beside you.</para>
    /// </summary>
    public sealed class SteepTroughPart : Part, IDescribeState
    {
        public override string Name => "SteepTrough";

        public const string Resin = "PitchpodResin", Clay = "FireClay", Prefix = "SteepGarment|", FireSuffix = "|fire";
        public const int MaxRows = 6, FireClayCost = 2;

        public static bool IsCommand(string command) => command != null && command.StartsWith(Prefix, StringComparison.Ordinal);

        bool Standing => ParentEntity?.GetPart<PhysicsPart>()?.InInventory == null && !PackableFixturePart.IsInertKit(ParentEntity);

        // ── reader ──

        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 40;

        public string DescribeState(DescribeContext context)
        {
            if (!Standing) return null;
            string recipes = "Steeps leather and cloth armor against acid (1 pitchpod resin and 1 fire clay: +" + EnhancementSteeped.DefaultAmount + " acid resistance for " + EnhancementSteeped.DefaultTurns + " turns of wear) or clay-washes it against fire (" + FireClayCost + " fire clay: +" + EnhancementSteeped.DefaultAmount + " heat resistance, the same turns).";
            var viewer = context.Viewer; if (viewer == null) return recipes;
            var garments = Eligible(viewer); int resin = Count(viewer, Resin), clay = Count(viewer, Clay);
            string have = "You carry " + resin + " pitchpod resin and " + clay + " fire clay.";
            return garments.Count == 0 ? "Nothing you carry fits: leather or cloth armor, not worn. " + have : garments.Count + (garments.Count == 1 ? " garment" : " garments") + " you carry could be steeped. " + have;
        }

        // ── eligibility: an allow-list by Part ──

        static bool Worn(Entity actor, Entity item) { var inv = actor.GetPart<InventoryPart>(); return inv?.EquippedItems != null && inv.EquippedItems.Values.Contains(item); }

        static bool Fits(Entity actor, Entity item)
            => item != null && item.GetPart<ArmorPart>() != null && !item.HasTag("Creature") && EnhancementSteeped.GarmentMaterial(item) && (item.GetPart<StackerPart>()?.StackCount ?? 1) == 1
               && item.GetPart<PhysicsPart>()?.Takeable != false && !Worn(actor, item);

        static int Count(Entity actor, string blueprint)
        {
            var inv = actor.GetPart<InventoryPart>(); if (inv == null) return 0; int units = 0;
            foreach (var item in inv.Objects) if (item != null && item.BlueprintName == blueprint && inv.CanConsumeOne(item)) units += item.GetPart<StackerPart>()?.StackCount ?? 1;
            return units;
        }

        static List<Entity> Eligible(Entity actor)
        {
            var list = new List<Entity>(); var inv = actor.GetPart<InventoryPart>(); if (inv == null) return list;
            foreach (var item in inv.Objects) if (Fits(actor, item)) list.Add(item);
            list.Sort((a, b) => string.CompareOrdinal(a.GetDisplayName(), b.GetDisplayName()) is int c && c != 0 ? c : string.CompareOrdinal(a.ID, b.ID));
            return list;
        }

        static EnhancementSteeped SteepOf(Entity item) => item.Parts.OfType<EnhancementSteeped>().FirstOrDefault();

        /// <summary>Null when this garment could be treated now (the acid steeping), else the refusal code and the sentence that says why.</summary>
        static string Refusal(Entity actor, Entity garment, out string why) => Refusal(actor, garment, SteepedAcidEffect.AcidKind, out why);

        /// <summary>As above for the named treatment: "acid" (resin and clay) or "fire" (two clay).</summary>
        static string Refusal(Entity actor, Entity garment, string kind, out string why)
        {
            why = null;
            if (!Fits(actor, garment)) { why = "That cannot be steeped: leather or cloth armor, not worn, not stacked."; return "ineligible"; }
            if (SteepOf(garment) == null && ItemEnhancing.CountEnhancements(garment) >= ItemEnhancing.MAX_ENHANCEMENTS_PER_ITEM) { why = "This piece already holds two treatments."; return "slot-cap"; }
            int resin = Count(actor, Resin), clay = Count(actor, Clay);
            if (kind == SteepedAcidEffect.FireKind)
            {
                if (clay < FireClayCost) { why = "You need " + FireClayCost + " fire clay; you carry " + clay + "."; return "short-clay"; }
                return null;
            }
            if (resin < 1 && clay < 1) { why = "You need 1 pitchpod resin and 1 fire clay."; return "short-both"; }
            if (resin < 1) { why = "You need 1 pitchpod resin."; return "short-resin"; }
            if (clay < 1) { why = "You need 1 fire clay."; return "short-clay"; }
            return null;
        }

        // ── rows ──

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions") { AddRows(e.GetParameter<Entity>("Actor"), e.GetParameter<Zone>("Zone") ?? e.GetParameter<Entity>("Actor")?.SpatialZone, e.GetParameter<InventoryActionList>("Actions")); return true; }
            if (e.ID == "InventoryAction" && IsCommand(e.GetStringParameter("Command")))
            {
                var actor = e.GetParameter<Entity>("Actor"); var zone = e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone;
                if (Steep(actor, zone, e.GetStringParameter("Command"), e.GetParameter<InventoryTransaction>("InventoryTransaction"))) { e.Handled = true; return false; }
            }
            return true;
        }

        void AddRows(Entity actor, Zone zone, InventoryActionList actions)
        {
            if (actions == null || actor == null || zone == null || !Standing || !WorldResourceActions.Nearby(actor, ParentEntity, zone)) return;
            var garments = Eligible(actor);
            if (garments.Count == 0) { actions.AddDisabled("Steep", "steep a garment", "no-garment", "Nothing you carry fits: leather or cloth armor, not worn, not stacked.", 's', 20); return; }
            for (int i = 0; i < garments.Count && i < MaxRows; i++)
            {
                var garment = garments[i]; var prior = SteepOf(garment); string refusal = Refusal(actor, garment, out string why);
                string label = "steep " + garment.GetDisplayName() + (prior != null ? " (replaces " + prior.TurnsLeft + (prior.IsFire ? " turns of clay-wash)" : " turns)") : "");
                string hint = "Uses 1 pitchpod resin and 1 fire clay. +" + EnhancementSteeped.DefaultAmount + " acid resistance for " + EnhancementSteeped.DefaultTurns + " turns of wear, paused when taken off.";
                if (refusal == null) actions.AddActionWithHint("Steep", label, Prefix + garment.ID, hint, i == 0 ? 's' : '\0', 20 - i, false, ActionTime.OneAction);
                else actions.AddDisabled("Steep", label, refusal, why, i == 0 ? 's' : '\0', 20 - i);
            }
            // The clay-wash is offered once you carry any fire clay at all (a chain you have started), for the same garments as the steeping, and says what it lacks.
            if (Count(actor, Clay) < 1) return;
            for (int i = 0; i < garments.Count && i < MaxRows; i++)
            {
                var garment = garments[i]; var prior = SteepOf(garment); string refusal = Refusal(actor, garment, SteepedAcidEffect.FireKind, out string why);
                string label = "clay-wash " + garment.GetDisplayName() + (prior != null ? " (replaces " + prior.TurnsLeft + (prior.IsFire ? " turns)" : " turns of steeping)") : "");
                string hint = "Uses " + FireClayCost + " fire clay. +" + EnhancementSteeped.DefaultAmount + " heat resistance for " + EnhancementSteeped.DefaultTurns + " turns of wear, paused when taken off.";
                if (refusal == null) actions.AddActionWithHint("ClayWash", label, Prefix + garment.ID + FireSuffix, hint, i == 0 ? 'w' : '\0', 18 - i, false, ActionTime.OneAction);
                else actions.AddDisabled("ClayWash", label, refusal, why, i == 0 ? 'w' : '\0', 18 - i);
            }
        }

        // ── the steep: one action; reagents and the garment's new state come back together on any failure ──

        bool Steep(Entity actor, Zone zone, string command, InventoryTransaction tx)
        {
            string[] f = command.Split('|'); var inv = actor?.GetPart<InventoryPart>();
            bool wellFormed = f.Length == 2 || (f.Length == 3 && f[2] == "fire");
            if (actor == null || zone == null || inv == null || !Standing || !wellFormed || !WorldResourceActions.Nearby(actor, ParentEntity, zone)) return Block(actor, "invalid-context", "You cannot steep anything here now.");
            string kind = f.Length == 3 ? SteepedAcidEffect.FireKind : SteepedAcidEffect.AcidKind; bool fire = kind == SteepedAcidEffect.FireKind;
            var garment = inv.Objects.FirstOrDefault(o => o != null && o.ID == f[1]); if (garment == null) return Block(actor, "stale-selection", "That is no longer in your pack.");
            string refusal = Refusal(actor, garment, kind, out string why); if (refusal != null) return Block(actor, refusal, why);
            bool own = tx == null; tx ??= new InventoryTransaction();
            try
            {
                if (!tx.TryClaim(ParentEntity, actor, command) || !tx.TryClaim(garment, actor, command)) return Block(actor, "in-progress", "That is already being worked.");
                var prior = SteepOf(garment); int priorLeft = prior?.TurnsLeft ?? 0; string priorKind = prior?.Kind ?? SteepedAcidEffect.AcidKind; int priorAmount = prior?.Amount ?? 0;
                var receipt = InventoryTransferSnapshot.Capture(inv);
                tx.Do(null, receipt.Restore);
                // The reagents, set out one unit at a time: acid takes a resin and a clay; the clay-wash takes two clay (from one stack or two).
                bool Pay()
                {
                    if (!fire)
                    {
                        var resin = inv.Objects.FirstOrDefault(o => o != null && o.BlueprintName == Resin && inv.CanConsumeOne(o)); if (resin == null || !tx.TryClaim(resin, actor, command) || !inv.TryConsumeOne(resin)) return false;
                    }
                    for (int n = 0; n < (fire ? FireClayCost : 1); n++)
                    {
                        var clay = inv.Objects.FirstOrDefault(o => o != null && o.BlueprintName == Clay && inv.CanConsumeOne(o)); if (clay == null || !tx.TryClaim(clay, actor, command) || !inv.TryConsumeOne(clay)) return false;
                    }
                    return true;
                }
                if (!receipt.Apply(Pay) || !receipt.ClaimChanges(tx, actor, command)) return Block(actor, "payment-refused", "You could not set the reagents out.");
                tx.Do(() =>
                {
                    if (prior != null) ItemEnhancing.Remove(garment, "EnhancementSteeped");
                    if (ItemEnhancing.Apply(garment, "EnhancementSteeped")) { var fresh = SteepOf(garment); if (fresh != null) fresh.Kind = kind; }
                },
                () =>
                {
                    ItemEnhancing.Remove(garment, "EnhancementSteeped");
                    if (prior != null && ItemEnhancing.Apply(garment, "EnhancementSteeped")) { var again = SteepOf(garment); if (again != null) { again.TurnsLeft = priorLeft; again.TurnsTotal = prior.TurnsTotal; again.Kind = priorKind; again.Amount = priorAmount; } }
                });
                if (SteepOf(garment) == null) return Block(actor, "not-applied", "The steeping did not take.");
                tx.AfterCommit(() =>
                {
                    MessageLog.Add(fire
                        ? "You work the clay into " + garment.GetDisplayName() + ". It will turn fire for " + EnhancementSteeped.DefaultTurns + " turns of wear."
                        : "You steep " + garment.GetDisplayName() + " in the trough. It will turn acid for " + EnhancementSteeped.DefaultTurns + " turns of wear.");
                    Diag.Record("furniture", fire ? "ClayWashed" : "Steeped", actor, ParentEntity, new { garment = garment.BlueprintName, replaced = priorLeft, priorKind = prior == null ? "" : priorKind });
                });
                if (own) tx.Commit();
                return true;
            }
            finally { if (own) tx.Rollback(); }
        }

        bool Block(Entity actor, string code, string message)
        {
            MessageLog.Add(message);
            Diag.Record("furniture", "SteepRefused", actor: actor, target: ParentEntity, payload: new { code });
            return false;
        }
    }
}
