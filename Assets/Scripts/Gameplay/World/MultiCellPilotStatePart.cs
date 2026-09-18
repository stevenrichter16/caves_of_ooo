namespace CavesOfOoo.Core
{
    /// <summary>Installation marker on protected native ground. Owner absence
    /// is authoritative after installation, including death, harvest, travel
    /// and destruction; Ensure never repopulates missing owners.</summary>
    public sealed class MultiCellPilotStatePart : Part
    {
        public int Revision = 1;
        public string SkippedOwnerIds = "";
    }
}
