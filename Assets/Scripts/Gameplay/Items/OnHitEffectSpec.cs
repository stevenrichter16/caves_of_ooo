using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// One on-hit effect declaration parsed from a weapon's
    /// <c>OnHitEffectsRaw</c> string. Format per spec, comma-separated:
    ///
    ///   EffectName , ChancePercent , DamageDice , DurationTurns , Magnitude
    ///
    /// Multiple specs separated by <c>;</c>. Empty fields fall back to
    /// the per-effect default (handled in <see cref="OnHitEffectFactory"/>).
    ///
    /// Example raw strings:
    ///   <c>"Burning,30,,5,1.0"</c>            (one effect, default dice)
    ///   <c>"Burning,30,,5,1.0;Stunned,5,,1,0"</c>  (two effects)
    ///   <c>"Acidic,40,,5,1.5"</c>             (DissolutionMaul: stronger acid)
    ///
    /// Mirrors the <see cref="MaterialPart"/> <c>MaterialTagsRaw</c> flat-string
    /// pattern (no nested JSON; everything is a string parsed at use-time).
    /// </summary>
    public class OnHitEffectSpec
    {
        public string EffectName;
        public int ChancePercent;
        public string DamageDice;
        public int DurationTurns;
        public float Magnitude;

        /// <summary>
        /// Parse a raw OnHitEffectsRaw string into a list of specs. Malformed
        /// specs are silently skipped — the caller gets only valid entries.
        /// </summary>
        public static List<OnHitEffectSpec> Parse(string raw)
        {
            var result = new List<OnHitEffectSpec>();
            if (string.IsNullOrWhiteSpace(raw)) return result;

            var specs = raw.Split(';');
            foreach (var s in specs)
            {
                var trimmed = s.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                var fields = trimmed.Split(',');
                // Need at least: EffectName + ChancePercent. Other fields optional.
                if (fields.Length < 2) continue;

                var name = fields[0].Trim();
                if (string.IsNullOrEmpty(name)) continue;

                if (!int.TryParse(fields[1].Trim(), out int chance))
                    continue;
                if (chance <= 0) continue;  // 0% chance is a no-op spec; skip.

                var spec = new OnHitEffectSpec
                {
                    EffectName = name,
                    ChancePercent = chance,
                    DamageDice = fields.Length > 2 ? fields[2].Trim() : "",
                    DurationTurns = 0,
                    Magnitude = 0f,
                };

                if (fields.Length > 3 && int.TryParse(fields[3].Trim(), out int dur))
                    spec.DurationTurns = dur;
                if (fields.Length > 4 && float.TryParse(fields[4].Trim(),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float mag))
                    spec.Magnitude = mag;

                result.Add(spec);
            }
            return result;
        }
    }
}
