using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Interface for modular zone builders.
    /// Mirrors Qud's ZoneBuilderSandbox: each builder applies one layer
    /// of generation to a zone. Builders are composed into pipelines.
    /// </summary>
    public interface IZoneBuilder
    {
        string Name { get; }
        int Priority { get; }
        bool BuildZone(Zone zone, EntityFactory factory, System.Random rng);
    }
}
