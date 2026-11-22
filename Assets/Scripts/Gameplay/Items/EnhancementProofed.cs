using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// A garment proofed against an element for good (Docs/MACHINE-PROOFING.md): the permanent tier a machine gives. +<see cref="Amount"/> of the element's resistance while the garment is worn, for as
    /// long as it lasts: no turns, no wear. Smaller than a coat from a bottle (<see cref="EnhancementSteeped"/>) and below a purchased +50 garment, so it stacks with a coat, a meal and gear toward
    /// immunity rather than replacing them. A garment holds one lasting proofing, and it takes one of the garment's two treatment slots. Gear only: armor that is not a creature.
    /// <para>The shift is recorded (<see cref="AppliedBonus"/>, <see cref="AppliedStat"/>) so taking the garment off, or a save in between, reverts exactly what was added.</para>
    /// </summary>
    public sealed class EnhancementProofed : IItemEnhancement
    {
        /// <summary>An element id from <see cref="ProofingRegistry"/>; an unknown value reads as acid.</summary>
        public string Kind = "acid";
        public int Amount = ProofingRules.PermanentAmount;
        /// <summary>Set while the shift is on the wearer, so a repeated equip is a no-op and an unequip reverts only what was added.</summary>
        public bool AppliedBonus;
        public string AppliedStat = "";

        public override string Name => nameof(EnhancementProofed);
        public override string GetDisplayName() => "proofed";

        public string Label => SteepedAcidEffect.ElementLabel(Kind);
        public override string GetInventoryState() => Amount > 0 ? Label + " proofed" : "proofing unavailable";

        public override string GetEffectDescription()
        {
            string status = ProofingRegistry.ById(Kind)?.Status ?? "corrosion";
            return "Proofed against " + Label + " for good: +" + Amount + " " + Label + " resistance while worn. It also blunts " + status + ". "
                + char.ToUpperInvariant(Label[0]) + Label.Substring(1) + " still does at least 1 damage per hit until your resistance reaches 100.";
        }

        public override bool Applicable(Entity item) => base.Applicable(item) && item != null && item.GetPart<ArmorPart>() != null && !item.HasTag("Creature");

        public override void OnEquipped(Entity actor, Entity item)
        {
            if (AppliedBonus || actor == null || item == null || Amount <= 0) return;
            string name = ProofingRegistry.StatFor(Kind); var stat = actor.GetStat(name);
            if (stat == null) { stat = new Stat { Name = name, Owner = actor, Min = -100, Max = 200 }; actor.Statistics[name] = stat; }   // the zero-base stat stays after removal so unrelated sources survive
            AppliedBonus = true; AppliedStat = name; stat.Boost += Amount;
            if (Diag.IsChannelEnabled(ItemEnhancing.DIAG_CATEGORY)) Diag.Record(ItemEnhancing.DIAG_CATEGORY, "BonusApplied", actor, item, new { enhancement = nameof(EnhancementProofed), stat = name, amount = Amount });
        }

        public override void OnUnequipped(Entity actor, Entity item)
        {
            if (!AppliedBonus) return;
            var stat = actor?.GetStat(string.IsNullOrEmpty(AppliedStat) ? ProofingRegistry.StatFor(Kind) : AppliedStat);
            if (stat != null) stat.Boost -= Amount;
            AppliedBonus = false;
            if (Diag.IsChannelEnabled(ItemEnhancing.DIAG_CATEGORY)) Diag.Record(ItemEnhancing.DIAG_CATEGORY, "BonusRemoved", actor, item, new { enhancement = nameof(EnhancementProofed), stat = AppliedStat, amount = Amount });
        }
    }
}
