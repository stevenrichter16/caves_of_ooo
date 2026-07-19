namespace CavesOfOoo.Core
{
    /// <summary>
    /// Temper state on a forged/tempered weapon (§7.1 Layer 2). Tracks how
    /// many quenches the metal has taken and the total Hitpoints-max penalty
    /// applied, so a re-forge can restore it exactly (re-forging melts the
    /// temper away — see WeaponForgingService.TryReforge).
    ///
    /// Plain public fields → save-layer friendly via reflection, like
    /// RentalPart/WeaponAssemblyPart.
    /// </summary>
    public class WeaponTemperPart : Part
    {
        public override string Name => "WeaponTemper";

        /// <summary>Quenches taken. Capped at WeaponTemperingService.MaxTempers.</summary>
        public int TemperCount = 0;

        /// <summary>Total Hitpoints-max penalty applied — restored on re-forge.</summary>
        public int HpPenaltyTotal = 0;

        /// <summary>The on-hit specs the tempers appended (';'-joined), for display/debugging.</summary>
        public string AppliedSpecsRaw = "";
    }
}
