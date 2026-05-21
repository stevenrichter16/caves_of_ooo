namespace CavesOfOoo.Core
{
    /// <summary>
    /// G.7a — throwable gas grenade payload. Direct port of Qud
    /// <c>XRL.World.Parts.GasGrenade</c> (GasGrenade.cs:1-87).
    /// Lives on the grenade ITEM entity (carried in inventory, thrown
    /// by the player). On detonation, spawns a 3×3 grid of gas clouds
    /// around the impact cell, with each cloud at the configured
    /// density + level.
    ///
    /// <para><b>Integration with ThrowItemCommand.</b> When the player
    /// throws an item carrying this Part, ThrowItemCommand detects it
    /// via <see cref="HasGasGrenadePayload"/>, calls
    /// <see cref="Detonate"/> at the impact cell, sets
    /// <c>consumedOnImpact = true</c>. Mirrors how
    /// <c>HasThrowablePayload</c> + <c>ApplyTonicAoe</c> works for
    /// thrown tonics. Friendly fire is intentional (gases don't filter
    /// by faction).</para>
    /// </summary>
    public class GasGrenadePart : Part
    {
        public override string Name => "GasGrenade";

        /// <summary>Registry id of the gas to spawn (e.g. "poison-vapor").
        /// Looked up via <see cref="GasRegistry.Get"/> inside the factory
        /// — unknown id emits <c>gas/SpawnRejected</c> and the per-cell
        /// spawn is skipped gracefully.</summary>
        public string GasId = "";

        /// <summary>Density passed to each spawned gas. Qud's GasGrenade
        /// defaults to 20; we'll let blueprints override.</summary>
        public int Density = 20;

        /// <summary>Power tier of the spawned gas (drives effect
        /// magnitude downstream via the IObjectGasBehaviorPart filter
        /// chain).</summary>
        public int Level = 1;

        /// <summary>
        /// Spawn a 3×3 grid of gas clouds around <paramref name="center"/>
        /// (center cell + 8 adjacents). Each cell gets a fresh gas
        /// entity via <see cref="GasFactory.SpawnGas"/>. Out-of-bounds
        /// cells are skipped (factory's CellOutOfBounds rejection path
        /// handles it gracefully).
        ///
        /// <para>Returns the count of gases actually spawned (0..9) for
        /// caller diagnostics. The grenade item is NOT destroyed here —
        /// that's the caller's job (ThrowItemCommand sets
        /// consumedOnImpact = true after this returns).</para>
        /// </summary>
        public int Detonate(Entity actor, Cell center, Zone zone)
        {
            if (center == null || zone == null) return 0;
            if (string.IsNullOrEmpty(GasId)) return 0;

            int spawned = 0;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int x = center.X + dx, y = center.Y + dy;
                    var spawn = GasFactory.SpawnGas(zone, x, y, GasId,
                        density: Density, level: Level, creator: actor);
                    if (spawn != null) spawned++;
                }
            }

            Diagnostics.Diag.Record("gas", "GrenadeDetonated", actor, ParentEntity,
                new { gasId = GasId, density = Density, level = Level,
                      centerX = center.X, centerY = center.Y, cellsSpawned = spawned });
            return spawned;
        }
    }
}
