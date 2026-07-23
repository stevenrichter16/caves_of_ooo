namespace CavesOfOoo.Core
{
    /// <summary>
    /// Charred: permanent degradation from burning. Reduces Combustibility by 70%.
    /// Restores original value on removal.
    /// </summary>
    public class CharredEffect : Effect
    {
        public override string DisplayName => "charred";

        // WSP6.16 — TYPE_NEGATIVE backfill (see AcidicEffect.cs).
        public override int GetEffectType() => TYPE_GENERAL | TYPE_NEGATIVE;

        // Docs/COMBAT-AUDIT-BUGFIX-PLAN-2026-07.md SM1/A1 — must be public
        // FIELDS, not private (mirrors HibernatingEffect's SL.6.4 pattern).
        // SaveSystem.WritePublicFields only walks BindingFlags.Public
        // fields, so private state here is silently dropped on save; a
        // Charred creature that persists across any save/load would never
        // have its Combustibility restored when the effect wears off
        // (nothing re-runs OnApply post-load to re-capture it).
        public float OriginalCombustibility;
        public bool HasStoredOriginal;

        public CharredEffect()
        {
            Duration = DURATION_INDEFINITE;
        }

        public override void OnApply(Entity target)
        {
            var material = target.GetPart<MaterialPart>();
            if (material != null)
            {
                OriginalCombustibility = material.Combustibility;
                HasStoredOriginal = true;
                material.Combustibility *= 0.3f;
            }
            MessageLog.Add(target.GetDisplayName() + " is charred.");
        }

        public override void OnRemove(Entity target)
        {
            if (HasStoredOriginal)
            {
                var material = target.GetPart<MaterialPart>();
                if (material != null)
                    material.Combustibility = OriginalCombustibility;
            }
        }

        public override bool OnStack(Effect incoming)
        {
            // Charred doesn't stack — already charred
            return true;
        }

        public override string GetRenderColorOverride() => "&K";
    }
}
