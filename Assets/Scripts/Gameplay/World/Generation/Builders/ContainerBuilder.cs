using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// LOOT OVERHAUL SM6 — the pipeline hook for
    /// <see cref="ContainerPlacementService"/>.
    ///
    /// <para>Priority 4100 puts it AFTER LandmarkBuilder (3790-3860)
    /// and PopulationBuilder (4000), so it sees the stamps' reserved
    /// cells and the population's spawns and places around both.</para>
    ///
    /// <para>Adding this one builder to a pipeline is what gives a zone
    /// type containers at all — before this, only landmark stamps and a
    /// single village chest produced any, and ordinary wilderness zones
    /// got none.</para>
    /// </summary>
    public class ContainerBuilder : IZoneBuilder
    {
        public string Name => "ContainerBuilder";
        public int Priority => 4100;

        private readonly BiomeType _biome;
        private readonly int _tier;
        private readonly ContainerPlacementService.ZoneKind _kind;

        public ContainerBuilder(BiomeType biome, int tier,
            ContainerPlacementService.ZoneKind kind)
        {
            _biome = biome;
            _tier = tier;
            _kind = kind;
        }

        public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng)
        {
            // The service takes the factory from its own static hook
            // (bootstrap-wired, CorpsePart convention); fall back to the
            // pipeline's factory so headless/test pipelines work too.
            if (ContainerPlacementService.Factory == null)
                ContainerPlacementService.Factory = factory;

            ContainerPlacementService.Populate(zone, _biome, _tier, _kind, rng);
            return true;
        }
    }
}
