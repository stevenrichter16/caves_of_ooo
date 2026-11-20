using System.Linq;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// The proofing a steeped garment gives its wearer while it is worn (Docs/MACHINE-INVENTED-USES.md 3h; the clay-wash against fire is 3l). The class keeps its saved name; <see cref="Kind"/> says which
    /// resistance it gives (acid by default, so every earlier save loads as it always was). It is applied when the garment is put on, counts down on the wearer's
    /// own turns (so world-map travel, which fires no local turn, does not wear it), and is taken off with the garment, which keeps the turns that remain. One effect per garment: two steeped
    /// garments worn together each give their own. When its turns run out it ends the steeping on the garment as well, so the garment is plain again.
    /// <para>Public fields carry the exact shift applied, so a saved stat Boost is reverted without being reapplied on load (the <see cref="PreparedMealEffect"/> pattern).</para>
    /// </summary>
    public sealed class SteepedAcidEffect : Effect
    {
        public const string Stat = "AcidResistance", FireStat = "HeatResistance", AcidKind = "acid", FireKind = "fire";

        public int Amount;
        public int AppliedBonus;
        public string GarmentId = "";
        /// <summary>"acid" (the original steeping) or "fire" (the clay-wash). An unknown or missing value reads as acid.</summary>
        public string Kind = AcidKind;
        /// <summary>The stat the shift was applied to, so removal reverts exactly what was added whatever the kind says now. Empty in a save from before kinds existed: acid.</summary>
        public string AppliedStat = "";

        public static string StatFor(string kind) => kind == FireKind ? FireStat : Stat;

        public override string DisplayName => Kind == FireKind ? "clay-washed against fire" : "steeped against acid";
        public override int GetEffectType() => TYPE_METABOLIC;

        public SteepedAcidEffect() { }
        public SteepedAcidEffect(int amount, int duration, string garmentId, string kind = AcidKind) { Amount = amount; Duration = duration; GarmentId = garmentId ?? ""; Kind = kind == FireKind ? FireKind : AcidKind; }

        public static bool Valid(int amount, int duration, string garmentId) => amount > 0 && amount <= 60 && duration > 0 && !string.IsNullOrEmpty(garmentId);

        public override bool CanApply(Entity target) => target != null && Valid(Amount, Duration, GarmentId);

        /// <summary>Each garment's steeping is its own benefit: two worn together do not share a status.</summary>
        public override bool SharesStatusWith(Effect other) => other is SteepedAcidEffect s && s.GarmentId == GarmentId;

        /// <summary>A second steeping from the same garment cannot happen (it is applied once on equip); another garment's is added, never merged.</summary>
        public override bool OnStack(Effect incoming) => false;

        public override void OnApply(Entity target)
        {
            string name = StatFor(Kind);
            var stat = target.GetStat(name);
            if (stat == null)
            {
                // Ordinary actors need not author the stat; the zero-base stat stays after removal so unrelated sources survive.
                stat = new Stat { Name = name, Owner = target, Min = -100, Max = 200 };
                target.Statistics[name] = stat;
            }
            AppliedStat = name;
            AppliedBonus = Amount;
            stat.Boost += AppliedBonus;
        }

        public override void OnRemove(Entity target)
        {
            var stat = target?.GetStat(string.IsNullOrEmpty(AppliedStat) ? Stat : AppliedStat);
            if (stat != null) stat.Boost -= AppliedBonus;
            AppliedBonus = 0;
            // Ran out (not taken off): the garment is plain again.
            if (Duration == 0 && target != null) EnhancementSteeped.Expire(target, GarmentId);
        }

        /// <summary>Counts down on the wearer's own turn (the base rule) and keeps the garment's remaining turns in step, so its reader is right while it is worn and a re-equip after any other end of the effect never refills it.</summary>
        public override void OnTurnEnd(Entity target)
        {
            base.OnTurnEnd(target);
            var inventory = target?.GetPart<InventoryPart>(); if (inventory?.EquippedItems == null) return;
            foreach (var pair in inventory.EquippedItems)
                if (pair.Value != null && pair.Value.ID == GarmentId) { var enhancement = pair.Value.Parts.OfType<EnhancementSteeped>().FirstOrDefault(); if (enhancement != null) enhancement.TurnsLeft = System.Math.Max(0, Duration); return; }
        }

        public string Describe() => "+" + Amount + (Kind == FireKind ? " heat resistance; " : " acid resistance; ") + Duration + " of your turns left in this garment.";
    }
}
