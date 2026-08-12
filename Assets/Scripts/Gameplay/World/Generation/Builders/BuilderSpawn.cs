using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Guarded entity placement for zone builders.
    ///
    /// <para><b>Why this exists.</b> <c>EntityFactory.CreateEntity</c>
    /// LOGS AN ERROR for an unknown blueprint, and Unity's test framework
    /// fails any test that emits one — so a builder that asks for content
    /// blindly breaks every test running against a minimal blueprint
    /// fixture. The rule is already written down ("guard every blueprint
    /// lookup; builders must fail SOFT"), and the stamp, hazard and
    /// container builders all follow it.</para>
    ///
    /// <para>The three oldest terrain builders did not. It never showed,
    /// because every fixture happened to carry the handful of blueprints
    /// they asked for — until the authored map (Felling W0.6) routed a
    /// zone through the grass palette and a dozen unrelated world-map
    /// tests went red on <c>unknown blueprint 'Grass'</c>. Latent since
    /// the builders were written; surfaced by a change that had nothing
    /// to do with it.</para>
    /// </summary>
    internal static class BuilderSpawn
    {
        /// <summary>
        /// Create <paramref name="blueprint"/> and add it at (x, y), or
        /// do nothing at all if the content pack does not have it.
        /// Returns the entity, or null when it was skipped.
        /// </summary>
        public static Entity TryPlace(Zone zone, EntityFactory factory,
            string blueprint, int x, int y)
        {
            if (zone == null || factory == null) return null;
            if (string.IsNullOrEmpty(blueprint)) return null;
            if (factory.Blueprints == null
                || !factory.Blueprints.ContainsKey(blueprint)) return null;

            Entity e = factory.CreateEntity(blueprint);
            if (e == null) return null;
            zone.AddEntity(e, x, y);
            return e;
        }
    }
}
