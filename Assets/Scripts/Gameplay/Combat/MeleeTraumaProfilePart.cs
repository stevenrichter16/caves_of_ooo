namespace CavesOfOoo.Core
{
    /// <summary>Saved attacker capability for incidental high-damage melee trauma only.
    /// Absence preserves ordinary combat. Explicit severing abilities remain independent.</summary>
    public sealed class MeleeTraumaProfilePart : Part
    {
        public override string Name => "MeleeTraumaProfile";
        public bool AllowIncidentalDismemberment = true;
    }
}
