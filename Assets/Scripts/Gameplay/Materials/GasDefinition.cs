using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Data-only definition of a gas (G.2 — gas-system foundation).
    /// Mirrors Qud's per-cell <c>Gas</c> Part's blueprint params (one
    /// shared definition per gas type — flyweight), but JSON-driven
    /// instead of <c>[XmlPart]</c>-reflected — matching CoO's
    /// <see cref="LiquidDefinition"/> loading convention so a new gas
    /// is a JSON row, not a C# class.
    ///
    /// <para>Unity <see cref="UnityEngine.JsonUtility"/> cannot
    /// deserialize <c>Dictionary</c> or nested generics beyond
    /// <c>List&lt;[Serializable]&gt;</c>, so the schema sticks to flat
    /// scalars + a wrapper collection — the same shape that works for
    /// <see cref="LiquidDefinition"/>.</para>
    /// </summary>
    [Serializable]
    public class GasDefinition
    {
        /// <summary>Stable lookup key (e.g. "poison-vapor", "cryo-mist").</summary>
        public string Id;

        /// <summary>Human-readable noun ("poison vapor").</summary>
        public string DisplayName;

        /// <summary>Adjective applied to creatures coated/affected
        /// ("poison-fumed"). Reserved for the gas-as-coat hybrid
        /// (G.8 GasPlasma → CoatedInPlasmaEffect).</summary>
        public string Adjective;

        /// <summary>Qud's <c>Gas.GasType</c> — identity key for merge
        /// compatibility ("Poison", "Cryo", "Stun", "Sleep", etc.). Two
        /// gases with the same GasType + Color merge; different GasType
        /// coexist in a cell.</summary>
        public string GasType = "BaseGas";

        /// <summary>Per-cell render glyph (default "°" = degree sign,
        /// matching Qud's first dispersal-cycle frame).</summary>
        public string Glyph = "°";

        /// <summary>CGA color code for the cell render (e.g. "&amp;g" =
        /// green for poison).</summary>
        public string Color = "&w";

        /// <summary>Default density (concentration) when spawned via
        /// <see cref="GasFactory.SpawnGas"/> with no explicit override.
        /// Qud's default is 100; higher = denser = more potent + slower
        /// to dissipate.</summary>
        public int DefaultDensity = 100;

        /// <summary>Default level (power tier). Most gases ship at 1;
        /// special-cell stronger variants ship at 2+. Drives effect
        /// magnitude in the per-creature filter pipeline (G.5+).</summary>
        public int DefaultLevel = 1;

        /// <summary>If true, the gas can move through walls (Qud's
        /// <c>Gas.Seeping</c>). False = blocked by solid cells.</summary>
        public bool Seeping;

        /// <summary>If true, the gas does NOT decay over time (Qud's
        /// <c>Gas.Stable</c>). False = density drops each turn from
        /// dispersal rate. Stable gases still spread but persist.
        /// Wired by <see cref="GasSystem"/> in G.3.</summary>
        public bool Stable;

        /// <summary>String tag identifying which <c>IGasBehaviorPart</c>
        /// subtype to attach when a gas of this def is spawned. One of
        /// "" (no behavior, visual only), "Poison" (G.5), "Cryo" /
        /// "Stun" / "Sleep" / "Confusion" / "FungalSpores" / "Plasma"
        /// (G.8). Looked up by <see cref="GasFactory.SpawnGas"/>.</summary>
        public string BehaviorKind;
    }

    /// <summary>JSON wrapper collection — Unity JsonUtility needs the
    /// root to be a plain object, not an array. Mirrors
    /// <see cref="LiquidDefinitionCollection"/>.</summary>
    [Serializable]
    public class GasDefinitionCollection
    {
        public List<GasDefinition> Gases;
    }
}
