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

        private float _originalCombustibility;
        private bool _hasStoredOriginal;

        public CharredEffect()
        {
            Duration = DURATION_INDEFINITE;
        }

        public override void OnApply(Entity target)
        {
            var material = target.GetPart<MaterialPart>();
            if (material != null)
            {
                _originalCombustibility = material.Combustibility;
                _hasStoredOriginal = true;
                material.Combustibility *= 0.3f;
            }
            MessageLog.Add(target.GetDisplayName() + " is charred.");
        }

        public override void OnRemove(Entity target)
        {
            if (_hasStoredOriginal)
            {
                var material = target.GetPart<MaterialPart>();
                if (material != null)
                    material.Combustibility = _originalCombustibility;
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
