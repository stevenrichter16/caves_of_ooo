using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// G.2 — static helper for spawning gas cloud entities. Mirrors
    /// Qud's <c>GameObject.Create("PoisonGas")</c> → blueprint factory
    /// path, condensed to one method that:
    /// <list type="number">
    ///   <item>Looks up the <see cref="GasDefinition"/> in the registry</item>
    ///   <item>Builds an <see cref="Entity"/> with the standard 3 Parts
    ///         (RenderPart, PhysicsPart{Solid=false}, GasPoolPart)</item>
    ///   <item>Copies the def's tuning onto the GasPoolPart
    ///         (Density / Level / Seeping / Stable / GasType / Color / Creator)</item>
    ///   <item>Places the entity in the zone at (x, y) via
    ///         <see cref="Zone.AddEntity"/></item>
    ///   <item>Emits a <c>gas/Created</c> diag record for observability</item>
    /// </list>
    ///
    /// <para>The behavior sibling Part (<c>GasPoisonPart</c>,
    /// <c>GasCryoPart</c>, etc.) is attached in G.5+ based on
    /// <see cref="GasDefinition.BehaviorKind"/>. G.2 ships with that
    /// field read but unused — the gas is visually present but
    /// behaviorally inert.</para>
    ///
    /// <para>Failure modes (return null, emit diag with reason):
    /// <list type="bullet">
    ///   <item>Registry uninitialized</item>
    ///   <item>Unknown <paramref name="gasId"/></item>
    ///   <item>Null zone</item>
    ///   <item>Cell out-of-bounds (Zone.AddEntity returns false)</item>
    /// </list>
    /// </para>
    /// </summary>
    public static class GasFactory
    {
        /// <summary>
        /// Spawn a gas cloud at (<paramref name="x"/>, <paramref name="y"/>)
        /// in <paramref name="zone"/>. Returns the spawned entity on
        /// success, null on any failure (with a <c>gas/SpawnRejected</c>
        /// diag record carrying the reason).
        /// </summary>
        /// <param name="density">Density override; -1 = use
        /// <see cref="GasDefinition.DefaultDensity"/></param>
        /// <param name="level">Level override; -1 = use
        /// <see cref="GasDefinition.DefaultLevel"/></param>
        public static Entity SpawnGas(Zone zone, int x, int y, string gasId,
            int density = -1, int level = -1, Entity creator = null)
        {
            if (!GasRegistry.IsInitialized)
            {
                Diag.Record("gas", "SpawnRejected", creator, null,
                    new { reason = "RegistryUninitialized", gasId, x, y });
                return null;
            }

            var def = GasRegistry.Get(gasId);
            if (def == null)
            {
                Diag.Record("gas", "SpawnRejected", creator, null,
                    new { reason = "UnknownGas", gasId, x, y });
                return null;
            }

            if (zone == null)
            {
                Diag.Record("gas", "SpawnRejected", creator, null,
                    new { reason = "NullZone", gasId, x, y });
                return null;
            }

            int useDensity = density < 0 ? def.DefaultDensity : density;
            int useLevel = level < 0 ? def.DefaultLevel : level;

            var entity = new Entity
            {
                ID = $"gas_{gasId}_{x}_{y}_{System.Guid.NewGuid().ToString("N").Substring(0, 6)}",
                BlueprintName = gasId + "Cloud",
            };
            entity.Tags["Gas"] = "";

            entity.AddPart(new RenderPart
            {
                DisplayName = def.DisplayName ?? gasId,
                RenderString = string.IsNullOrEmpty(def.Glyph) ? "°" : def.Glyph,
                ColorString = string.IsNullOrEmpty(def.Color) ? "&w" : def.Color,
            });
            entity.AddPart(new PhysicsPart { Solid = false });

            // GasPoolPart created with default Density=0 then assigned
            // — using the property setter would emit a DensityChange
            // event for the spawn delta, which is misleading (the gas
            // didn't change from a prior density, it was just born).
            // The post-Initialize assignment via the setter is the
            // legitimate "spawn density" emit point.
            var pool = new GasPoolPart
            {
                GasId = gasId,
                Level = useLevel,
                Seeping = def.Seeping,
                Stable = def.Stable,
                GasType = string.IsNullOrEmpty(def.GasType) ? "BaseGas" : def.GasType,
                ColorString = string.IsNullOrEmpty(def.Color) ? "&w" : def.Color,
                Creator = creator,
            };
            entity.AddPart(pool);

            // G.5 — attach the behavior Part keyed by def.BehaviorKind.
            // Empty string = visual-only gas (no per-creature effect),
            // intentional for G.2/G.3 content stubs. The factory is the
            // ONE place that translates the JSON tag → concrete C# Part,
            // mirroring how MaterialPart routes a material tag → behavior.
            var behavior = CreateBehaviorPart(def.BehaviorKind);
            if (behavior != null)
                entity.AddPart(behavior);

            pool.Density = useDensity; // fires GasDensityChange (0 → useDensity)

            if (!zone.AddEntity(entity, x, y))
            {
                Diag.Record("gas", "SpawnRejected", creator, null,
                    new { reason = "CellOutOfBounds", gasId, x, y });
                return null;
            }

            // Set the density-scaled cloud glyph (░▒▓) + mark the cell dirty
            // so the renderer actually paints the new cloud (gas cells are
            // otherwise never flagged for repaint).
            GasVisuals.Refresh(entity, pool, zone);

            Diag.Record("gas", "Created", creator, entity,
                new { gasId, density = useDensity, level = useLevel, x, y,
                      gasType = pool.GasType, seeping = pool.Seeping, stable = pool.Stable,
                      behaviorKind = def.BehaviorKind });

            return entity;
        }

        /// <summary>Map a <see cref="GasDefinition.BehaviorKind"/> string
        /// to the concrete behavior Part. Returns null for empty/unknown
        /// kinds — the gas is then visual-only. Mirrors the StatusTonicPart
        /// effect-name → C# class switch (StatusTonicPart.cs:35-84) so
        /// content authors can add a new gas variety as one JSON row +
        /// one switch case (one C# class per BehaviorKind).</summary>
        private static IGasBehaviorPart CreateBehaviorPart(string behaviorKind)
        {
            if (string.IsNullOrEmpty(behaviorKind)) return null;
            // G.5 ships with one kind. G.8 will fan this out to Cryo /
            // Stun / Sleep / Confusion / FungalSpores / Plasma.
            switch (behaviorKind)
            {
                case "Poison":    return new GasPoisonPart();
                case "Stun":      return new GasStunPart();      // G.8a
                case "Confusion": return new GasConfusionPart(); // G.8a
                case "Cryo":      return new GasCryoPart();      // G.8b
                case "Sleep":     return new GasSleepPart();     // G.8c
                case "FungalSpores": return new GasFungalSporesPart(); // G.8d
                case "Plasma":    return new GasPlasmaPart();    // G.8e
                default:
                    // Unknown kinds log but don't crash — same resilient
                    // posture as the registry's malformed-JSON path.
                    UnityEngine.Debug.LogWarning(
                        $"[GasFactory] Unknown BehaviorKind '{behaviorKind}'; gas will be visual-only.");
                    return null;
            }
        }
    }
}
