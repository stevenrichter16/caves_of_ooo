namespace CavesOfOoo.Core
{
    /// <summary>
    /// The acid-proofing a steeped garment gives its wearer while it is worn (Docs/MACHINE-INVENTED-USES.md 3h). It is applied when the garment is put on, counts down on the wearer's
    /// own turns (so world-map travel, which fires no local turn, does not wear it), and is taken off with the garment, which keeps the turns that remain. One effect per garment: two steeped
    /// garments worn together each give their own. When its turns run out it ends the steeping on the garment as well, so the garment is plain again.
    /// <para>Public fields carry the exact shift applied, so a saved stat Boost is reverted without being reapplied on load (the <see cref="PreparedMealEffect"/> pattern).</para>
    /// </summary>
    public sealed class SteepedAcidEffect : Effect
    {
        public const string Stat = "AcidResistance";

        public int Amount;
        public int AppliedBonus;
        public string GarmentId = "";

        public override string DisplayName => "steeped against acid";
        public override int GetEffectType() => TYPE_METABOLIC;

        public SteepedAcidEffect() { }
        public SteepedAcidEffect(int amount, int duration, string garmentId) { Amount = amount; Duration = duration; GarmentId = garmentId ?? ""; }

        public static bool Valid(int amount, int duration, string garmentId) => amount > 0 && amount <= 60 && duration > 0 && !string.IsNullOrEmpty(garmentId);

        public override bool CanApply(Entity target) => target != null && Valid(Amount, Duration, GarmentId);

        /// <summary>Each garment's steeping is its own benefit: two worn together do not share a status.</summary>
        public override bool SharesStatusWith(Effect other) => other is SteepedAcidEffect s && s.GarmentId == GarmentId;

        /// <summary>A second steeping from the same garment cannot happen (it is applied once on equip); another garment's is added, never merged.</summary>
        public override bool OnStack(Effect incoming) => false;

        public override void OnApply(Entity target)
        {
            var stat = target.GetStat(Stat);
            if (stat == null)
            {
                // Ordinary actors need not author the stat; the zero-base stat stays after removal so unrelated sources survive.
                stat = new Stat { Name = Stat, Owner = target, Min = -100, Max = 200 };
                target.Statistics[Stat] = stat;
            }
            AppliedBonus = Amount;
            stat.Boost += AppliedBonus;
        }

        public override void OnRemove(Entity target)
        {
            var stat = target?.GetStat(Stat);
            if (stat != null) stat.Boost -= AppliedBonus;
            AppliedBonus = 0;
            // Ran out (not taken off): the garment is plain again.
            if (Duration == 0 && target != null) EnhancementSteeped.Expire(target, GarmentId);
        }

        public string Describe() => "+" + Amount + " acid resistance; " + Duration + " of your turns left in this garment.";
    }
}
