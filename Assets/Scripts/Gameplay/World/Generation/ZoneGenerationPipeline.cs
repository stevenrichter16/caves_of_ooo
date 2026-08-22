using System.Collections.Generic;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Runs a sequence of IZoneBuilders against a Zone in priority order.
    /// Mirrors Qud's ZoneBuilderCollection: sorts builders by priority,
    /// applies each in sequence, retries on failure.
    /// </summary>
    public class ZoneGenerationPipeline
    {
        private List<IZoneBuilder> _builders = new List<IZoneBuilder>();

        /// <summary>Read-only view of the registered builders, in run
        /// order. Test-facing: lets structural pins assert which
        /// builders a given zone's pipeline carries (e.g. the R8
        /// exclusion — no BloomFrontBuilder on POI pipelines) without
        /// generating the zone.</summary>
        public IReadOnlyList<IZoneBuilder> Builders => _builders;

        /// <summary>Drop every builder of a given type. Used where a
        /// bespoke pipeline reuses a biome's wilderness recipe but one
        /// of its builders contradicts the bespoke content — e.g. a
        /// sinkhole mouth, whose hole IS the way down and which must not
        /// also roll a random cave entrance.</summary>
        public void RemoveBuilders<T>() where T : IZoneBuilder
        {
            _builders.RemoveAll(b => b is T);
        }
        public int MaxRetries = 5;

        public void AddBuilder(IZoneBuilder builder)
        {
            _builders.Add(builder);
            _builders.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }

        /// <summary>
        /// Generate a zone by running all builders in priority order.
        /// If any builder returns false, the zone is cleared and re-run
        /// (up to MaxRetries). Mirrors Qud's GenerateZone retry loop.
        /// </summary>
        public bool Generate(Zone zone, EntityFactory factory, System.Random rng)
        {
            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                ClearZone(zone);

                bool success = true;
                foreach (var builder in _builders)
                {
                    if (!builder.BuildZone(zone, factory, rng))
                    {
                        success = false;
                        break;
                    }
                }

                if (success)
                    return true;

                // New seed for retry
                rng = new System.Random(rng.Next());
            }
            return false;
        }

        private void ClearZone(Zone zone)
        {
            var entities = zone.GetAllEntities();
            foreach (var entity in entities)
                zone.RemoveEntity(entity);
        }

        /// <summary>
        /// Standard cave pipeline: Border -> Cave -> Connectivity -> Population.
        /// </summary>
        public static ZoneGenerationPipeline CreateCavePipeline(PopulationTable popTable = null)
        {
            var pipeline = new ZoneGenerationPipeline();
            pipeline.AddBuilder(new CaveBuilder());
            pipeline.AddBuilder(new ConnectivityBuilder());
            if (popTable != null)
            {
                pipeline.AddBuilder(new PopulationBuilder(popTable));
                pipeline.AddBuilder(new TradeStockBuilder());
            }
            return pipeline;
        }
    }
}
