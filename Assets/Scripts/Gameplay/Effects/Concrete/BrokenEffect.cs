namespace CavesOfOoo.Core
{
    /// <summary>Combat damage marker. Explicitly repairable portable gear has
    /// a bounded penalty; other objects keep their existing marker semantics.</summary>
    public class BrokenEffect : Effect
    {
        public override string DisplayName => "broken";

        // WSP6.16 — TYPE_NEGATIVE backfill (see AcidicEffect.cs).
        public override int GetEffectType() => TYPE_GENERAL | TYPE_NEGATIVE;

        // Public fields persist the exact applied deltas through saves. Loading
        // does not reapply them, and removal restores rather than resets gear.
        public int HitPenalty, ArmorPenalty;
        public bool PenaltyApplied;

        public BrokenEffect(int duration = DURATION_INDEFINITE)
        {
            Duration = duration;
        }

        public override void OnApply(Entity target)
        {
            if (!PenaltyApplied && target.GetPart<RepairablePart>()?.PortableEquipment == true)
            {
                var melee = target.GetPart<MeleeWeaponPart>();
                var armor = target.GetPart<ArmorPart>();
                HitPenalty = melee == null ? 0 : 2;
                ArmorPenalty = armor == null ? 0 : System.Math.Min(1, System.Math.Max(0, armor.AV));
                if (melee != null) melee.HitBonus -= HitPenalty;
                if (armor != null) armor.AV -= ArmorPenalty;
                PenaltyApplied = true;
                EquipmentChangeBus.NotifyChanged(target.GetPart<PhysicsPart>()?.Equipped);
            }
            MessageLog.Add(target.GetDisplayName() + " is broken!");
        }

        public override void OnRemove(Entity target)
        {
            if (PenaltyApplied)
            {
                var melee = target.GetPart<MeleeWeaponPart>();
                var armor = target.GetPart<ArmorPart>();
                if (melee != null) melee.HitBonus += HitPenalty;
                if (armor != null) armor.AV += ArmorPenalty;
                PenaltyApplied = false;
                EquipmentChangeBus.NotifyChanged(target.GetPart<PhysicsPart>()?.Equipped);
            }
            MessageLog.Add(target.GetDisplayName() + " is repaired.");
        }

        public override bool OnStack(Effect incoming)
        {
            // Multiple Broken applies are no-ops — the item's already broken.
            // Do not extend duration; the first apply wins.
            return true;
        }

        public override string GetRenderColorOverride() => "&K";
    }
}
