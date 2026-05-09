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

        // WSP6.16 — TYPE_NEGATIVE backfill so ShortBlades_Shank and
        // similar "punish status-ridden targets" mechanics can count
        // this effect as a debuff on the target. Mirrors Qud's
        // effect-type bitmask convention.
        public override int GetEffectType() => TYPE_GENERAL | TYPE_NEGATIVE;

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

            // Flat damage scaled by corrosion severity. Use the typed-Damage
            // overload with the "Acid" attribute so AcidResistance routes
            // correctly (the int overload strips attributes — pre-fix bug
            // where acid-immune creatures still corroded, and acid-vulnerable
            // ones like Scorpion didn't take amplified damage). Mirrors the
            // on-hit weapon path.
            //
            // Log the POST-resistance amount: ApplyDamage mutates
            // acidDmg.Amount in place via ApplyResistances, so reading
            // acidDmg.Amount after the call gives the actually-applied
            // damage. Same fix as BurningEffect — pre-resistance logging
            // misled players about whether resistance was firing.
            int rolledAmount = 1 + (int)System.Math.Floor(Corrosion * 4f);
            if (rolledAmount > 0 && target.GetStatValue("Hitpoints", 0) > 0)
            {
                Zone zone = context?.GetParameter<Zone>("Zone");
                var acidDmg = new Damage(rolledAmount);
                acidDmg.AddAttribute("Acid");
                CombatSystem.ApplyDamage(target, acidDmg, null, zone);
                MessageLog.Add(target.GetDisplayName() + " takes " + acidDmg.Amount + " acid damage.");
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

        public override string GetRenderColorOverride() => "&*G"; // HDR (was SDR &g) — see GRAPHICS.md §3.B.3
    }
}
