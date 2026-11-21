using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// A resistance blunts the status as well as the damage (Docs/MACHINE-PROOFING.md). The four elemental resistances used to scale only the damage numbers, so Frozen (no damage at all), the
    /// length of a burn, Electrified's stun and Acidic's corrosion never read them and cold resistance had almost nothing to resist. One rule, applied once where an effect is put on a target
    /// (<see cref="StatusEffectsPart"/>), before it stacks or runs: a positive resistance leaves (100 - resistance) percent of the status, 100 or more is immunity to it, a negative resistance
    /// (a vulnerability) changes nothing here (the damage side already amplifies it), and only the matching stat counts. What "leaves that percent" means is one line per status:
    /// Frozen's Cold (floored at <see cref="FrozenEffect.FREEZE_FLOOR"/> short of immunity), Acidic's Corrosion, Electrified's duration and contact stun, Burning's duration on a thing with no fuel
    /// (a fuelled thing burns by its fuel). The damage tick is still resisted by the damage rule, so the two compound on purpose: it is what makes proofing worth the trouble.
    /// </summary>
    public static class ElementalStatusResistance
    {
        public const string Heat = "HeatResistance", Cold = "ColdResistance", Electric = "ElectricResistance", Acid = "AcidResistance";

        /// <summary>The resistance stat that answers this effect, or null for an effect no resistance blunts.</summary>
        public static string StatFor(Effect effect)
            => effect is BurningEffect ? Heat : effect is FrozenEffect ? Cold : effect is ElectrifiedEffect ? Electric : effect is AcidicEffect ? Acid : null;

        /// <summary>The fraction of the status that gets through: 1 with no (or negative) resistance, 0 at 100 or more.</summary>
        public static float Scale(Entity target, string stat)
        {
            int resistance = target?.GetStatValue(stat, 0) ?? 0;
            if (resistance <= 0) return 1f;
            return resistance >= 100 ? 0f : (100 - resistance) / 100f;
        }

        /// <summary>Whole turns scaled, never below one short of immunity (round half away from zero).</summary>
        public static int Turns(int turns, float scale) => scale >= 1f ? turns : Math.Max(1, (int)Math.Round(turns * scale, MidpointRounding.AwayFromZero));

        /// <summary>Applies the rule to an incoming effect in place. False means the target is immune and the effect must not be applied.</summary>
        public static bool Dampen(Effect effect, Entity target)
        {
            string stat = StatFor(effect); if (stat == null || target == null) return true;
            float scale = Scale(target, stat); if (scale >= 1f) return true;
            if (scale <= 0f) return false;
            switch (effect)
            {
                case FrozenEffect frozen: frozen.Cold = Math.Max(FrozenEffect.FREEZE_FLOOR, frozen.Cold * scale); break;
                case AcidicEffect acid: acid.Corrosion *= scale; break;
                case ElectrifiedEffect zap: zap.Duration = Turns(zap.Duration, scale); zap.StunScale = scale; break;
                case BurningEffect burn: burn.DurationScale = scale; break;
            }
            return true;
        }
    }
}
