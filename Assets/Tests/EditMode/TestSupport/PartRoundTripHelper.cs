using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Reusable save/load round-trip helpers for the SAVE-LOAD-AUDIT.md
    /// sub-milestones. Centralized here so SL.2-SL.9 tests share a
    /// single implementation; if the round-trip pipeline changes shape
    /// (e.g., new SaveReader argument), the fix is one place.
    ///
    /// <para>The helpers exercise the production save pipeline used by
    /// the live game (<see cref="SaveGraphSerializer"/>), not a
    /// test-only mock — so a passing round-trip means the actual
    /// gzip-compressed save file format would round-trip too.</para>
    ///
    /// <para>NOTE: <see cref="SaveReader"/> takes an optional
    /// <see cref="EntityFactory"/> for resolving cross-entity
    /// references on load. Tests that don't have inter-entity
    /// references can pass null (the default here); SL.3
    /// (entity-reference round-trip) will introduce a variant that
    /// supplies a factory.</para>
    /// </summary>
    public static class PartRoundTripHelper
    {
        /// <summary>
        /// Serialize the entity through SaveGraphSerializer.SaveEntityBody,
        /// reload into a fresh Entity via LoadEntityBody, and return the
        /// loaded copy. Mirrors how production save/load flows through
        /// the per-entity body path; cross-entity references are NOT
        /// resolved here (factory: null).
        /// </summary>
        public static Entity RoundTripEntity(Entity src)
        {
            using var stream = new MemoryStream();
            var writer = new SaveWriter(stream);
            SaveGraphSerializer.SaveEntityBody(src, writer);
            stream.Position = 0;
            var reader = new SaveReader(stream, factory: null);
            var loaded = new Entity();
            SaveGraphSerializer.LoadEntityBody(loaded, reader);
            return loaded;
        }

        /// <summary>
        /// Variant for SL.3+ that supplies an EntityFactory so cross-
        /// entity references resolve on load. Caller provides a factory
        /// that knows how to look up entities by ID.
        /// </summary>
        public static Entity RoundTripEntityWithFactory(Entity src, EntityFactory factory)
        {
            using var stream = new MemoryStream();
            var writer = new SaveWriter(stream);
            SaveGraphSerializer.SaveEntityBody(src, writer);
            stream.Position = 0;
            var reader = new SaveReader(stream, factory: factory);
            var loaded = new Entity();
            SaveGraphSerializer.LoadEntityBody(loaded, reader);
            return loaded;
        }
    }
}
