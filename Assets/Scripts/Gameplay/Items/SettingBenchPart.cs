using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// The Setters' bench at Posy (Docs/MACHINE-INVENTED-USES.md 3w): where the Bower-Folk will work a thing you carry, for someone whose eye they have seen. Two treatments that existed in the game with no way to get them:
    /// <b>lacquer</b> (<see cref="EnhancementLacquered"/>: +1 to +4 armor value on a garment, a tier for each pitchpod resin spent) and the <b>sign</b> (<see cref="EnhancementEngraved"/> set to the Bower-Folk: +5 of their regard
    /// while it is worn, for one glow-quartz). The bench is gated by the regard the Sorters' frame (<see cref="SortingFramePart"/>) earns: the Bower-Folk "judge by the eye", so the frame is where they look, and the more regard,
    /// the higher the tier they will work (<see cref="MinRegard"/>, then a tier more for every <see cref="RegardPerTier"/>). A garment takes two treatments at most, once each of these, shared with the trough's lasting proofing.
    /// The cost is the finite resin or quartz; nothing is made from nothing and nothing is sold.
    /// </summary>
    public sealed class SettingBenchPart : Part, IDescribeState, IDescribeUse
    {
        public override string Name => "SettingBench";

        public const string Faction = "BowerFolk", LacquerPrefix = "SetLacquer|", SignPrefix = "SetSign|", ResinBlueprint = "PitchpodResin", QuartzBlueprint = "GlowQuartz";
        public const int MinRegard = 8, RegardPerTier = 10, MaxTier = 4, MaxRows = 12;

        public static bool IsCommand(string command) => command != null && (command.StartsWith(LacquerPrefix, StringComparison.Ordinal) || command.StartsWith(SignPrefix, StringComparison.Ordinal));

        /// <summary>The highest lacquer tier the bench will work at this regard: 0 below <see cref="MinRegard"/>, then 1, and one more for each full <see cref="RegardPerTier"/> above it, up to <see cref="MaxTier"/>.</summary>
        public static int TierCap(int regard) => regard < MinRegard ? 0 : Math.Min(MaxTier, 1 + (regard - MinRegard) / RegardPerTier);

        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 20;
        /// <summary>How to use the bench, what it needs and gives, from the live constants (the owner, 2026-11-21). The "right now" line is built from <see cref="AddRows"/>'s rows (refused work says why).</summary>
        public void DescribeUse(DescribeContext context, UseGuide guide)
        {
            guide.How.Add("Press C beside it and choose lacquer <garment> to tier N, or set the Bower sign on <piece>.");
            guide.Needs.Add("the Bower-Folk's regard of " + MinRegard + " or more (set found things in the Sorters' frame by the well to earn it); the tier you may lacquer to rises by one for every " + RegardPerTier + " more, up to " + MaxTier);
            guide.Needs.Add("one pitchpod resin for each tier of lacquer, or one glow quartz for a sign");
            guide.Limits.Add("Each tier of lacquer is one point of armor on a garment you wear or carry. A signed piece raises the Bower-Folk's regard while you wear it and lowers it again when you take it off.");
            guide.WhyNot = "you are not beside it";
        }

        public string DescribeState(DescribeContext context)
        {
            int regard = PlayerReputation.Get(Faction), cap = TierCap(regard);
            string known = cap == 0 ? " It will not work your things yet: the Sorters have not seen your eye (the frame by the well is where they look)."
                : cap >= MaxTier ? " The Sorters know your eye well: it will lacquer to the highest tier." : " The Sorters have seen your eye: it will lacquer to tier " + cap + ", and higher as they come to know it better.";
            return known.TrimStart();
        }

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
            return item != null && !item.HasTag("Creature") && item.SpatialZone == null && physics?.ParentEntity == item && physics.Takeable && GarmentStacks.WholeOrSplittable(actor, item)
                && Holdings(actor).Count(o => o == item || o.ID == item.ID) == 1;
        }
        static int Carried(Entity actor, string blueprint)
        {
            var inv = actor?.GetPart<InventoryPart>(); if (inv == null) return 0; int n = 0;
            foreach (var o in inv.Objects) if (o != null && o.BlueprintName == blueprint && inv.CanConsumeOne(o)) n += o.GetPart<StackerPart>()?.StackCount ?? 1;
            return n;
        }

        /// <summary>Null when this treatment could be worked now; otherwise a code and the sentence that says why not.</summary>
        static string Refusal(Entity actor, Entity item, bool lacquer, int tier, out string why)
        {
            why = null;
            if (lacquer && item.GetPart<ArmorPart>() == null) { why = "Only armor takes lacquer."; return "not-armor"; }
            if (!lacquer && item.GetPart<EquippablePart>() == null) { why = "The sign goes on something that is worn or wielded."; return "not-equippable"; }
            if (lacquer ? item.Parts.OfType<EnhancementLacquered>().Any() : item.Parts.OfType<EnhancementEngraved>().Any()) { why = lacquer ? "It is lacquered already." : "It already carries a sign."; return "already"; }
            if (ItemEnhancing.CountEnhancements(item) >= ItemEnhancing.MAX_ENHANCEMENTS_PER_ITEM) { why = "This piece already holds two treatments."; return "slot-cap"; }
            if (lacquer)
            {
                int cap = TierCap(PlayerReputation.Get(Faction)); if (tier > cap) { why = "The Sorters will lacquer only to tier " + cap + " for someone whose eye they know so far."; return "regard"; }
                int resin = Carried(actor, ResinBlueprint); if (resin < tier) { why = "You need " + tier + " pitchpod resin; you carry " + resin + "."; return "short-resin"; }
            }
            else if (Carried(actor, QuartzBlueprint) < 1) { why = "You need a glow-quartz; you carry none."; return "short-quartz"; }
            return null;
        }

        // ── rows ──

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions") { AddRows(e.GetParameter<Entity>("Actor"), e.GetParameter<Zone>("Zone") ?? e.GetParameter<Entity>("Actor")?.SpatialZone, e.GetParameter<InventoryActionList>("Actions")); return true; }
            if (e.ID == "InventoryAction" && IsCommand(e.GetStringParameter("Command")))
            {
                var actor = e.GetParameter<Entity>("Actor"); var zone = e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone;
                if (Work(actor, zone, e.GetStringParameter("Command"), e.GetParameter<InventoryTransaction>("InventoryTransaction"))) { e.Handled = true; return false; }
            }
            return true;
        }

        void AddRows(Entity actor, Zone zone, InventoryActionList actions)
        {
            if (actions == null || actor == null || zone == null || !WorldResourceActions.Nearby(actor, ParentEntity, zone)) return;
            int regard = PlayerReputation.Get(Faction), cap = TierCap(regard);
            if (cap == 0) { actions.AddDisabled("SetPiece", "have a piece set", "unknown-eye", "The Setters will work your things only for someone whose eye they have seen. The frame by the well is where they look: set found things in it.", 's', 20); return; }
            var items = Holdings(actor).Where(o => Fits(actor, o)).OrderBy(o => !Worn(actor, o)).ThenBy(o => o.GetDisplayName(), StringComparer.Ordinal).ThenBy(o => o.ID, StringComparer.Ordinal).ToList();
            var rows = new List<(Entity item, bool lacquer, int tier, string refusal, string why)>();
            foreach (var item in items)
            {
                if (item.GetPart<ArmorPart>() != null) for (int t = Math.Min(cap, MaxTier); t >= 1; t--) { string r = Refusal(actor, item, true, t, out string w); rows.Add((item, true, t, r, w)); }
                if (item.GetPart<EquippablePart>() != null) { string r = Refusal(actor, item, false, 1, out string w); rows.Add((item, false, 1, r, w)); }
            }
            if (rows.Count == 0) { actions.AddDisabled("SetPiece", "have a piece set", "nothing-to-set", "Nothing you carry can be lacquered or signed: it takes armor, or something worn or wielded.", 's', 20); return; }
            rows = rows.Select((r, i) => (r, i)).OrderBy(x => x.r.refusal != null).ThenBy(x => x.i).Select(x => x.r).ToList();
            for (int shown = 0; shown < rows.Count && shown < MaxRows; shown++)
            {
                var (item, lacquer, tier, refusal, why) = rows[shown];
                string name = item.GetDisplayName() + GarmentStacks.Suffix(item) + (Worn(actor, item) ? " (worn)" : "");
                string label = lacquer ? "lacquer " + name + " to tier " + tier + " (" + tier + " pitchpod resin)" : "set the Bower sign on " + name + " (1 glow-quartz)";
                string hint = lacquer ? "+" + tier + " armor while it is worn. It takes one of the piece's two treatment slots and cannot be taken out." : "+5 of the Bower-Folk's regard while it is worn. It takes one of the piece's two treatment slots and cannot be taken out.";
                string command = (lacquer ? LacquerPrefix : SignPrefix) + Uri.EscapeDataString(item.ID) + (lacquer ? "|" + tier : "");
                if (refusal == null) actions.AddActionWithHint("SetPiece", label, command, hint, shown == 0 ? 's' : '\0', 20 - Math.Min(shown, 15), false, ActionTime.OneAction);
                else actions.AddDisabled("SetPiece", label, refusal, why, shown == 0 ? 's' : '\0', 20 - Math.Min(shown, 15));
            }
            if (rows.Count > MaxRows) actions.AddDisabled("SetPiece", "have a piece set (" + (rows.Count - MaxRows) + " more not shown)", "more", (rows.Count - MaxRows) + " more choices are not shown: set some pieces aside to see them.", '\0', 0);
        }

        // ── the work: one action; the cost, the stack split and the treatment come back together on any failure ──

        bool Work(Entity actor, Zone zone, string command, InventoryTransaction tx)
        {
            bool lacquer = command.StartsWith(LacquerPrefix, StringComparison.Ordinal); string[] f = command.Split('|'); var inv = actor?.GetPart<InventoryPart>();
            if (actor == null || zone == null || inv == null || (lacquer ? f.Length != 3 : f.Length != 2) || !WorldResourceActions.Nearby(actor, ParentEntity, zone)) return Block(actor, "invalid-context", "You cannot have anything set here now.");
            int tier = 1; if (lacquer && (!int.TryParse(f[2], out tier) || tier < 1 || tier > MaxTier)) return Block(actor, "malformed-selection", "That is not a tier the Setters work.");
            string itemId = WorldResourceActions.Decode(f[1]); var item = Holdings(actor).FirstOrDefault(o => o.ID == itemId);
            if (item == null || !Fits(actor, item)) return Block(actor, "stale-selection", "That is no longer something you hold.");
            string refusal = Refusal(actor, item, lacquer, tier, out string why); if (refusal != null) return Block(actor, refusal, why);
            bool own = tx == null; tx ??= new InventoryTransaction();
            try
            {
                if (!tx.TryClaim(ParentEntity, actor, command) || !tx.TryClaim(item, actor, command)) return Block(actor, "in-progress", "That is already being worked.");
                bool worn = Worn(actor, item); var stack = item.GetPart<StackerPart>(); bool split = !worn && stack != null && stack.StackCount > 1; int stackBefore = stack?.StackCount ?? 1; Entity target = item; bool added = false;
                string cost = lacquer ? ResinBlueprint : QuartzBlueprint; int units = lacquer ? tier : 1;
                var receipt = InventoryTransferSnapshot.Capture(inv); tx.Do(null, receipt.Restore);
                bool Pay()
                {
                    for (int n = 0; n < units; n++) { var c = inv.Objects.FirstOrDefault(o => o != null && o.BlueprintName == cost && inv.CanConsumeOne(o)); if (c == null || !tx.TryClaim(c, actor, command) || !inv.TryConsumeOne(c)) return false; }
                    return true;
                }
                if (!receipt.Apply(Pay) || !receipt.ClaimChanges(tx, actor, command)) return Block(actor, "payment-refused", "You could not set the payment out.");
                string enhancement = lacquer ? "EnhancementLacquered" : "EnhancementEngraved";
                tx.Do(() =>
                {
                    if (split) { target = stack.RemoveOne(); if (target == null) { target = item; return; } }
                    if (!ItemEnhancing.Apply(target, enhancement, lacquer ? tier : 1, null)) return;
                    if (!lacquer) { var sign = target.Parts.OfType<EnhancementEngraved>().FirstOrDefault(); if (sign != null) sign.Faction = Faction; }
                    if (worn) target.Parts.OfType<IItemEnhancement>().First(p => p.Name == enhancement).OnEquipped(actor, target);   // set up first (the sign's faction), so it works at once
                    if (split) added = inv.AddObject(target);   // enhanced first, so it never merges back into the plain stack
                },
                () =>
                {
                    var made = target.Parts.OfType<IItemEnhancement>().FirstOrDefault(p => p.Name == enhancement);
                    if (made != null) ItemEnhancing.Remove(target, enhancement, worn ? actor : null);
                    if (split) { if (added) inv.Objects.Remove(target); if (stack != null) stack.StackCount = stackBefore; inv.RefreshHandlingCarryPenalty(); }
                });
                if (!target.Parts.OfType<IItemEnhancement>().Any(p => p.Name == enhancement) || (split && !added)) return Block(actor, "not-applied", "The piece would not take it.");
                string itemName = item.GetDisplayName(); string word = lacquer ? "lacquer it to tier " + tier : "set the Bower sign on it";
                tx.AfterCommit(() =>
                {
                    MessageLog.Add("The Setters " + word + ": " + itemName + ".");
                    Diag.Record("furniture", lacquer ? "PieceLacquered" : "PieceSigned", actor, ParentEntity, new { item = target.BlueprintName, tier = lacquer ? tier : 0, worn });
                });
                if (own) tx.Commit();
                return true;
            }
            finally { if (own) tx.Rollback(); }
        }

        bool Block(Entity actor, string code, string message)
        {
            MessageLog.Add(message);
            Diag.Record("furniture", "SettingRefused", actor: actor, target: ParentEntity, payload: new { code });
            return false;
        }
    }
}
