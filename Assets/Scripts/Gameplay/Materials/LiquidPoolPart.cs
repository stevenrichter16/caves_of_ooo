using CavesOfOoo.Diagnostics;

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
        /// LQ.4 transfer-on-contact (closes plan gap (b)). When a
        /// <c>Creature</c> steps into this pool's cell,
        /// <see cref="MovementSystem.FireCellEnteredEvents"/> fires
        /// <c>EntityEnteredCell</c> on this pool (a non-mover occupant);
        /// we coat the mover with a <see cref="LiquidCoveredEffect"/>.
        ///
        /// <para><b>Divergence #5 (once-on-enter).</b> The event fires
        /// only on the move INTO the cell — standing still or leaving
        /// does not re-coat. Re-entry merges via
        /// <see cref="LiquidCoveredEffect.OnStack"/>, never stacks.</para>
        ///
        /// <para><b>Exposure = clamp(Volume, 0, Strength+Toughness).</b>
        /// A bigger, sturdier creature picks up more of a deep pool but
        /// a shallow pool only ever transfers what's in it. A statless
        /// creature (cap 0) is a documented degenerate no-coat.</para>
        ///
        /// <para><b>Observability (CLAUDE.md §Observability).</b> Every
        /// branch emits a <c>liquid</c> diag record: <c>Coated</c> on
        /// success, <c>CoatRejected</c> with a <c>reason</c> on each
        /// gate (NullActor / NotACreature / RegistryUninitialized /
        /// NoLiquidId / UnknownLiquid / PoolEmpty / ZeroExposure).</para>
        /// </summary>
        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID != "EntityEnteredCell") return true;

            var mover = e.GetParameter<Entity>("Actor");
            if (mover == null)
            {
                Diag.Record("liquid", "CoatRejected", null, ParentEntity,
                    new { reason = "NullActor", liquidId = LiquidId, volume = Volume });
                return true;
            }

            // Items, projectiles, furniture — only creatures get coated.
            if (!mover.Tags.ContainsKey("Creature"))
            {
                Diag.Record("liquid", "CoatRejected", mover, ParentEntity,
                    new { reason = "NotACreature", liquidId = LiquidId, volume = Volume });
                return true;
            }

            if (!LiquidRegistry.IsInitialized)
            {
                Diag.Record("liquid", "CoatRejected", mover, ParentEntity,
                    new { reason = "RegistryUninitialized", liquidId = LiquidId, volume = Volume });
                return true;
            }

            if (string.IsNullOrEmpty(LiquidId))
            {
                Diag.Record("liquid", "CoatRejected", mover, ParentEntity,
                    new { reason = "NoLiquidId", liquidId = LiquidId, volume = Volume });
                return true;
            }

            if (LiquidRegistry.Get(LiquidId) == null)
            {
                Diag.Record("liquid", "CoatRejected", mover, ParentEntity,
                    new { reason = "UnknownLiquid", liquidId = LiquidId, volume = Volume });
                return true;
            }

            if (Volume <= 0)
            {
                Diag.Record("liquid", "CoatRejected", mover, ParentEntity,
                    new { reason = "PoolEmpty", liquidId = LiquidId, volume = Volume });
                return true;
            }

            int cap = mover.GetStatValue("Strength", 0) + mover.GetStatValue("Toughness", 0);
            int exposure = Volume < cap ? Volume : cap; // clamp(Volume, 0, cap); Volume > 0
            if (exposure <= 0)
            {
                Diag.Record("liquid", "CoatRejected", mover, ParentEntity,
                    new { reason = "ZeroExposure", liquidId = LiquidId, volume = Volume, cap });
                return true;
            }

            mover.ApplyEffect(new LiquidCoveredEffect(LiquidId, exposure), ParentEntity, null);
            Diag.Record("liquid", "Coated", mover, ParentEntity,
                new { liquidId = LiquidId, amount = exposure, volume = Volume, cap });
            return true;
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
