namespace CavesOfOoo.Core
{
    /// <summary>
    /// Marks an entity as a pool/puddle of a liquid (LQ.3). Mirrors a
    /// Qud GameObject carrying a <c>LiquidVolume</c> with
    /// <c>MaxVolume == -1</c> (an "open volume" — a ground pool),
    /// reduced to CoO's phase-1 single-liquid scalar model (plan §4
    /// divergence #1: no parts-per-1000 mixing yet).
    ///
    /// <para><b>Render is data-driven (review finding F1).</b> On
    /// <see cref="Initialize"/> the part overwrites the entity's
    /// <see cref="RenderPart"/> glyph + color from the liquid's
    /// <see cref="LiquidDefinition.Glyph"/>/<c>Color</c> — exactly the
    /// way <see cref="MaterialPart.Initialize"/> pushes its tags onto
    /// the entity. A new puddle blueprint therefore only needs a
    /// <c>LiquidId</c>; its appearance follows the definition. This is
    /// null-safe: if the <see cref="LiquidRegistry"/> isn't
    /// initialized, the id is empty/unknown, or there's no
    /// RenderPart, the blueprint-authored render is left untouched
    /// (review finding F2).</para>
    ///
    /// <para><b>Flyweight contract (review finding F3).</b>
    /// <see cref="LiquidRegistry.Get"/> returns the shared, mutable
    /// <see cref="LiquidDefinition"/>. This part — and every future
    /// consumer (LQ.4+) — MUST treat it as read-only: copy scalars
    /// out, never hold-and-mutate. The only legitimate writer is
    /// <see cref="LiquidRegistry.Initialize"/>.</para>
    ///
    /// <para>Fields are plain public so the part round-trips through
    /// the reflection save path with no <c>FormatVersion</c> bump
    /// (plan §A5), the same way <see cref="MaterialPart"/> does.</para>
    /// </summary>
    public class LiquidPoolPart : Part
    {
        public override string Name => "LiquidPool";

        /// <summary>Registry id of the liquid in this pool
        /// (e.g. "water", "oil", "acid").</summary>
        public string LiquidId = "";

        /// <summary>How much liquid is in the pool. Drives the
        /// contact-coating exposure in LQ.4. Clamped to ≥ 0.</summary>
        public int Volume = 0;

        public override void Initialize()
        {
            if (Volume < 0) Volume = 0;
            ApplyDefinitionRender();
        }

        /// <summary>
        /// Pull glyph/color from the liquid definition onto the
        /// entity's RenderPart. No-op (leaves authored render) when
        /// the registry is uninitialized, the id is empty/unknown, or
        /// the entity has no RenderPart.
        /// </summary>
        private void ApplyDefinitionRender()
        {
            if (ParentEntity == null) return;
            if (!LiquidRegistry.IsInitialized) return;
            if (string.IsNullOrEmpty(LiquidId)) return;

            var def = LiquidRegistry.Get(LiquidId);
            if (def == null) return;

            var render = ParentEntity.GetPart<RenderPart>();
            if (render == null) return;

            if (!string.IsNullOrEmpty(def.Glyph))
                render.RenderString = def.Glyph;
            if (!string.IsNullOrEmpty(def.Color))
                render.ColorString = def.Color;
        }
    }
}
