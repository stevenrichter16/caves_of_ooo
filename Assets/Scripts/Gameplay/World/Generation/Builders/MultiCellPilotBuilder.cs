using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>The exact image-led ridge chunk south of Morrowfast.</summary>
    public sealed class MultiCellPilotBuilder : IZoneBuilder
    {
        public string Name => "MultiCellPilot";
        public int Priority => 1000;
        public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng)
            => MultiCellPilotRuntime.Ensure(zone, factory);
    }
}
