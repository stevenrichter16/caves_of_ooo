using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// G.2 — universal "I am gas" Part. Direct mirror of Qud's
    /// <c>XRL.World.Parts.Gas</c> reduced to the foundation layer
    /// (state + render-from-def + density-change event). Dispersal
    /// (G.3), merge (G.4), and per-creature behavior (G.5+) are
    /// separate sub-milestones; this Part is intentionally inert
    /// w.r.t. world simulation — it just stores state and renders
    /// correctly.
    ///
    /// <para><b>Render is data-driven (LiquidPoolPart pattern).</b> On
    /// <see cref="Initialize"/> the Part overwrites the entity's
    /// <see cref="RenderPart"/> glyph + color from the gas's
    /// <see cref="GasDefinition.Glyph"/>/<c>Color</c> — same convention
    /// as <see cref="LiquidPoolPart"/>. A new gas blueprint only needs
    /// a <c>GasId</c>; its appearance follows the definition. Null-safe:
    /// if the <see cref="GasRegistry"/> isn't initialized, the id is
    /// empty/unknown, or there's no RenderPart, the blueprint-authored
    /// render is left untouched.</para>
    ///
    /// <para><b>Flyweight contract.</b>
    /// <see cref="GasRegistry.Get"/> returns the shared, mutable
    /// <see cref="GasDefinition"/>. This Part — and every future
    /// consumer (G.3+) — MUST treat it as read-only: copy scalars out,
    /// never hold-and-mutate. The only legitimate writer is
    /// <see cref="GasRegistry.Initialize"/>.</para>
    ///
    /// <para><b>Density-change event (Qud parity).</b> Mutating the
    /// <see cref="Density"/> property fires <c>"GasDensityChange"</c>
    /// on the parent entity (mirror of Qud <c>Gas.cs:50-55</c>) so
    /// listeners (AI nav cache flush, observability) can react.
    /// Suppressed when the new value equals the old — no zero-delta
    /// noise on the event bus.</para>
    ///
    /// <para>Fields are plain public so the Part round-trips through
    /// the reflection save path with no <c>FormatVersion</c> bump
    /// (mirroring <see cref="LiquidPoolPart"/>).</para>
    /// </summary>
    public class GasPoolPart : Part
    {
        public override string Name => "GasPool";

        /// <summary>Registry id of the gas in this pool
        /// (e.g. "poison-vapor", "cryo-mist").</summary>
        public string GasId = "";

        /// <summary>Concentration. Drives effect strength + spread chunk +
        /// dispersal/dissipation thresholds in G.3+. Clamped to ≥ 0.
        /// Mutating fires <c>"GasDensityChange"</c>.</summary>
        public int Density
        {
            get => _density;
            set
            {
                int clamped = value < 0 ? 0 : value;
                if (clamped == _density) return; // suppress zero-delta events
                int oldValue = _density;
                _density = clamped;
                if (ParentEntity != null)
                {
                    var e = GameEvent.New("GasDensityChange");
                    e.SetParameter("OldValue", (object)oldValue);
                    e.SetParameter("NewValue", (object)clamped);
                    ParentEntity.FireEvent(e);
                    e.Release();
                }
            }
        }

        // Save-backing field for the Density property. MUST be public:
        // SaveGraphSerializer.WritePublicFields (Save path) walks public
        // FIELDS only, and a property's private backing field is silently
        // dropped — which reloaded every gas cloud at Density 0, so it
        // dissipated on the next tick (save inside a cloud → cloud gone).
        // Latent save bug surfaced by GasSystemAdversarialTests
        // (Adversarial_GasPoolPart_Density_RoundTrips). Mutate via the
        // Density property to keep the clamp + GasDensityChange event.
        public int _density;

        /// <summary>Power tier of the gas (Qud parity). Drives effect
        /// magnitude in G.5+ (e.g. PoisonGas Level 2 deals 4/turn vs
        /// Level 1's 2/turn).</summary>
        public int Level = 1;

        /// <summary>If true, this gas instance can pass through solid
        /// cells during dispersal (G.3). Inherited from the definition
        /// at spawn but overridable per-instance (a gas-tumbler upgrade
        /// could flip an existing cloud).</summary>
        public bool Seeping;

        /// <summary>If true, density does NOT decay over time during
        /// dispersal (G.3). Stable gases still spread but persist
        /// indefinitely. Inherited from the definition at spawn.</summary>
        public bool Stable;

        /// <summary>Qud's <c>GasType</c> — merge-compatibility key.
        /// Two gases with the same GasType + ColorString merge into
        /// one (G.4). Inherited from the definition at spawn.</summary>
        public string GasType = "BaseGas";

        /// <summary>CGA color code captured from the definition at
        /// spawn. Part of merge identity (Qud parity — colored variants
        /// of the same GasType don't merge). Also used by the G.3
        /// dispersal-cycle render to color the animated glyph.</summary>
        public string ColorString = "&w";

        /// <summary>The entity that spawned this gas (for credit on
        /// damage / kills, source-side dispersal modifiers via
        /// <c>CreatorModifyGasDispersal</c> event in G.3, and cleanup
        /// when the creator is removed via <c>GeneralAmnesty</c>).
        /// Null = environmental (no credit).</summary>
        public Entity Creator;

        public override void Initialize()
        {
            ApplyDefinitionRender();
        }

        /// <summary>
        /// Pull glyph/color from the gas definition onto the entity's
        /// RenderPart. No-op (leaves authored render) when the registry
        /// is uninitialized, the id is empty/unknown, or the entity has
        /// no RenderPart. Mirrors <see cref="LiquidPoolPart"/>.
        /// </summary>
        private void ApplyDefinitionRender()
        {
            if (ParentEntity == null) return;
            if (!GasRegistry.IsInitialized) return;
            if (string.IsNullOrEmpty(GasId)) return;

            var def = GasRegistry.Get(GasId);
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
