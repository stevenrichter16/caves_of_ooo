namespace CavesOfOoo.Core
{
    /// <summary>
    /// G.5 — abstract base for "what does this gas do" Parts. Direct
    /// mirror of Qud's <c>XRL.World.Parts.IGasBehavior</c>
    /// (IGasBehavior.cs:5-21). Sits as a sibling Part next to
    /// <see cref="GasPoolPart"/> on the gas cloud entity; provides a
    /// cached accessor to that pool plus a stepped-density helper.
    ///
    /// <para>Inheritance tree (Qud parity):
    /// <list type="bullet">
    ///   <item><see cref="IGasBehaviorPart"/> — base; just the accessor.
    ///         <see cref="GasCryoPart"/> (G.8) extends this directly
    ///         because cryo bypasses the Respires gate and hits all
    ///         matter via thermal coupling.</item>
    ///   <item><see cref="IObjectGasBehaviorPart"/> — the
    ///         creature/object-targeting dispatch loop; subclasses
    ///         override <c>ApplyGas(Entity)</c>.</item>
    /// </list></para>
    /// </summary>
    public abstract class IGasBehaviorPart : Part
    {
        // Cached lookup; mirrors Qud's `[NonSerialized] private Gas _BaseGas`.
        private GasPoolPart _baseGas;

        /// <summary>The sibling <see cref="GasPoolPart"/> on this entity.
        /// Cached after first lookup. Mirrors Qud
        /// <c>IGasBehavior.BaseGas</c>.</summary>
        public GasPoolPart BaseGas => _baseGas ??= ParentEntity?.GetPart<GasPoolPart>();

        /// <summary>Current gas density (0 if no pool found).</summary>
        public int GasDensity() => BaseGas?.Density ?? 0;

        /// <summary>Density quantised to multiples of <paramref name="step"/>.
        /// Used by AI nav-weight calculations (G.11) to avoid pathfinding
        /// cache thrash on tiny density wobbles. Mirrors Qud
        /// <c>IGasBehavior.GasDensityStepped</c>.</summary>
        public int GasDensityStepped(int step = 5)
        {
            if (step <= 1) return GasDensity();
            return (GasDensity() / step) * step;
        }
    }
}
