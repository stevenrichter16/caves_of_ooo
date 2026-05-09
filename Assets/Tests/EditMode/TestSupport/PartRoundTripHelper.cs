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
    /// <para>All helpers exercise the production save pipeline used by
    /// the live game (<see cref="SaveGraphSerializer"/>), not a
    /// test-only mock — so a passing round-trip means the actual
    /// gzip-compressed save file format would round-trip too.</para>
    ///
    /// <para><b>Entity-reference contract</b> (verified in SL.3):
    /// <see cref="SaveWriter.WriteEntityReference"/> writes a stable
    /// integer token for each unique <see cref="Entity"/> and queues
    /// the entity for body-write. The queued bodies are written by a
    /// SEPARATE call to <see cref="SaveWriter.WriteQueuedEntityBodies"/>.
    /// On load, <see cref="SaveReader.ReadEntityReference"/> creates a
    /// placeholder <see cref="Entity"/> per token; placeholders are
    /// populated when <see cref="SaveReader.ReadEntityBodies"/> runs.
    /// Without the queue/bodies pair, Entity refs round-trip as empty
    /// placeholder Entities (ID=null, no Parts) — see SL.3 findings #1.
    /// The <c>EntityFactory</c> argument to <see cref="SaveReader"/> is
    /// NOT consulted by <c>ReadEntityReference</c>; it's used solely by
    /// <c>LoadOverworldZoneManager</c> for full-game restore.</para>
    /// </summary>
    public static class PartRoundTripHelper
    {
        /// <summary>
        /// Bare round-trip: serialize the entity's body, reload into a
        /// fresh Entity, return the loaded copy. Cross-entity references
        /// (PhysicsPart.InInventory, ContainerPart.Contents, etc.) end
        /// up as empty placeholder Entities — see SL.3 findings.
        ///
        /// <para>Use this when the Part has only simple fields with no
        /// outgoing Entity refs (the SL.2 case). For Parts with Entity
        /// refs, use <see cref="RoundTripEntityWithBodies"/>.</para>
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
        /// Full graph round-trip: serialize the entity's body PLUS all
        /// queued referenced-entity bodies. On load, reads the primary
        /// entity's body PLUS all queued bodies, populating placeholder
        /// Entities created by Entity-ref reads.
        ///
        /// <para>This mirrors the production save flow (see
        /// <c>SaveSystem.cs</c> game-state save path: SaveEntityBody is
        /// called for each tracked entity, then WriteQueuedEntityBodies
        /// flushes the queue, then ReadEntityBodies populates them on
        /// load). Tokens deduplicate across all refs in a single round-
        /// trip — two fields pointing at the same entity get the same
        /// token, and load returns the same Entity instance for both.</para>
        ///
        /// <para>Use this for SL.3 (Entity refs), SL.4 (collections of
        /// Entity), and SL.6 (Effects with Entity refs like
        /// BurningEffect.IgnitionSource and HookedEffect.Hooker).</para>
        /// </summary>
        public static Entity RoundTripEntityWithBodies(Entity src)
        {
            using var stream = new MemoryStream();
            var writer = new SaveWriter(stream);
            SaveGraphSerializer.SaveEntityBody(src, writer);
            writer.WriteQueuedEntityBodies();
            stream.Position = 0;
            var reader = new SaveReader(stream, factory: null);
            var loaded = new Entity();
            SaveGraphSerializer.LoadEntityBody(loaded, reader);
            reader.ReadEntityBodies();
            return loaded;
        }

        /// <summary>
        /// Bare round-trip with a supplied EntityFactory. The factory is
        /// stored on <see cref="SaveReader.Factory"/> but is NOT
        /// consulted by <see cref="SaveReader.ReadEntityReference"/> —
        /// it's only used by <c>LoadOverworldZoneManager</c>. Kept for
        /// symmetry with production code paths that DO need a factory
        /// (zone-manager restore, settlement restore).
        ///
        /// <para>For Entity-ref round-trip use
        /// <see cref="RoundTripEntityWithBodies"/> instead — the factory
        /// has no effect on individual Entity refs.</para>
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

        /// <summary>
        /// Production-faithful round-trip: the entity is serialized AS
        /// AN ENTITY REFERENCE (token + queued body), exactly mirroring
        /// how production restores entities through
        /// <c>GameSessionState.Restore</c>'s
        /// <c>reader.ReadEntityReference()</c> path. The returned
        /// entity has had its full <c>OnAfterLoad</c> + <c>FinalizeLoad</c>
        /// hooks invoked on every Part — matching production semantics.
        ///
        /// <para>Use this for SL.4+ tests where a Part's correctness
        /// depends on the load-hook (e.g. <c>SkillsPart.OnAfterLoad</c>
        /// rebuilds <c>SkillList</c> from <c>ParentEntity.Parts</c>;
        /// without OnAfterLoad the SkillList holds a parallel set of
        /// reflection-deserialized BaseSkillPart instances that don't
        /// match the entity's own Parts list).</para>
        ///
        /// <para>Contrast with <see cref="RoundTripEntityWithBodies"/>:
        /// the previous helper calls <c>LoadEntityBody</c> directly on
        /// a fresh Entity, which BYPASSES OnAfterLoad/FinalizeLoad
        /// because the primary entity is never in
        /// <c>SaveReader._loadedEntities</c>. This helper queues the
        /// entity via <c>WriteEntityReference</c> + reads it via
        /// <c>ReadEntityReference</c>, so it IS in the loaded-entities
        /// list and gets the post-load hooks.</para>
        /// </summary>
        public static Entity RoundTripEntityViaTokenGraph(Entity src)
        {
            using var stream = new MemoryStream();
            var writer = new SaveWriter(stream);
            writer.WriteEntityReference(src); // queue src as token
            writer.WriteQueuedEntityBodies();
            stream.Position = 0;
            var reader = new SaveReader(stream, factory: null);
            var primary = reader.ReadEntityReference(); // placeholder
            reader.ReadEntityBodies(); // populates body + runs OnAfterLoad/FinalizeLoad
            return primary;
        }
    }
}
