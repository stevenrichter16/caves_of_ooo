using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// G.7b — one on-hit gas-emission declaration parsed from a weapon's
    /// <see cref="MeleeWeaponPart.EmitGasOnHitRaw"/> string. Mirrors the
    /// shape of <see cref="OnHitEffectSpec"/> (G.5/G.6 status-effect dispatcher).
    /// Format per spec, comma-separated:
    ///
    ///   GasId , ChancePercent , CellDensity , AdjacentDensity , GasLevel
    ///
    /// Multiple specs separated by <c>;</c>. Empty fields fall back to
    /// per-spec defaults (CellDensity=30, AdjacentDensity=15, GasLevel=1).
    ///
    /// Example raw strings:
    ///   <c>"poison-vapor,30,40,15,1"</c>        (poisonous fang)
    ///   <c>"cryo-mist,20,25,10,2"</c>           (frost claws)
    ///   <c>"poison-vapor,30,,,1;cryo-mist,10,,,1"</c>  (dual-gas)
    /// </summary>
    public class EmitGasOnHitSpec
    {
        public string GasId;
        public int ChancePercent;
        public int CellDensity;
        public int AdjacentDensity;
        public int GasLevel;

        public const int DEFAULT_CELL_DENSITY = 30;
        public const int DEFAULT_ADJACENT_DENSITY = 15;
        public const int DEFAULT_GAS_LEVEL = 1;

        /// <summary>
        /// Parse a raw EmitGasOnHitRaw string into a list of specs.
        /// Malformed specs are silently skipped — the caller gets only
        /// valid entries. Mirrors <see cref="OnHitEffectSpec.Parse"/>
        /// shape exactly so a content author who learned one knows the
        /// other.
        /// </summary>
        public static List<EmitGasOnHitSpec> Parse(string raw)
        {
            var result = new List<EmitGasOnHitSpec>();
            if (string.IsNullOrWhiteSpace(raw)) return result;

            var specs = raw.Split(';');
            foreach (var s in specs)
            {
                var trimmed = s.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                var fields = trimmed.Split(',');
                // Need at least: GasId + ChancePercent.
                if (fields.Length < 2) continue;

                var gasId = fields[0].Trim();
                if (string.IsNullOrEmpty(gasId)) continue;

                if (!int.TryParse(fields[1].Trim(), out int chance))
                    continue;
                if (chance <= 0) continue; // 0% chance is a no-op spec; skip.

                var spec = new EmitGasOnHitSpec
                {
                    GasId = gasId,
                    ChancePercent = chance,
                    CellDensity = DEFAULT_CELL_DENSITY,
                    AdjacentDensity = DEFAULT_ADJACENT_DENSITY,
                    GasLevel = DEFAULT_GAS_LEVEL,
                };

                if (fields.Length > 2 && int.TryParse(fields[2].Trim(), out int cell))
                    spec.CellDensity = cell;
                if (fields.Length > 3 && int.TryParse(fields[3].Trim(), out int adj))
                    spec.AdjacentDensity = adj;
                if (fields.Length > 4 && int.TryParse(fields[4].Trim(), out int lvl))
                    spec.GasLevel = lvl;

                result.Add(spec);
            }
            return result;
        }
    }
}
