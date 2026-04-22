namespace CavesOfOoo.Core
{
    /// <summary>
    /// Acidic: corrodes organic material over time. Deals flat damage each turn
    /// and degrades MaterialPart.Combustibility so wet/soaked organics lose their
    /// ability to burn — a neat cross-reaction with Quench.
    /// </summary>
    public class AcidicEffect : Effect
    {
        public override string DisplayName => "acidic";

        /// <summary>0..1. Higher values deal more damage per turn and decay slower.</summary>
        public float Corrosion;

        public AcidicEffect(float corrosion = 1.0f)
        {
            Corrosion = corrosion > 1.0f ? 1.0f : (corrosion < 0f ? 0f : corrosion);
            Duration = DURATION_INDEFINITE;
        }

        public override void OnApply(Entity target)
        {
            MessageLog.Add(target.GetDisplayName() + " is coated in acid.");
        }

        public override void OnRemove(Entity target)
        {
            MessageLog.Add(target.GetDisplayName() + " is no longer corroding.");
        }

        public override void OnTurnStart(Entity target, GameEvent context)
        {
            var material = target.GetPart<MaterialPart>();
            bool isOrganic = material != null && material.HasMaterialTag("Organic");
            if (!isOrganic)
                return;

            // Flat damage scaled by corrosion severity.
            int damage = 1 + (int)System.Math.Floor(Corrosion * 4f);
            if (damage > 0 && target.GetStatValue("Hitpoints", 0) > 0)
            {
                Zone zone = context?.GetParameter<Zone>("Zone");
                CombatSystem.ApplyDamage(target, damage, null, zone);
                MessageLog.Add(target.GetDisplayName() + " takes " + damage + " acid damage.");
            }

            // Acid degrades combustibility.
            float degrade = 0.05f * Corrosion;
            material.Combustibility = System.Math.Max(0f, material.Combustibility - degrade);
        }

        public override void OnTurnEnd(Entity target)
        {
            Corrosion = System.Math.Max(0f, Corrosion - 0.05f);
            if (Corrosion <= 0f)
                Duration = 0;
        }

        public override bool OnStack(Effect incoming)
        {
            if (incoming is AcidicEffect acid)
            {
                Corrosion += acid.Corrosion * 0.5f;
                if (Corrosion > 1.0f)
                    Corrosion = 1.0f;
                return true;
            }
            return false;
        }

        public override string GetRenderColorOverride() => "&g";
    }
}
