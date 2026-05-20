using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Data-only definition of a liquid (LQ.2). Mirrors Qud's
    /// flyweight <c>BaseLiquid</c> (one shared definition per liquid
    /// type), but JSON-driven instead of <c>[IsLiquid]</c>-reflected —
    /// matching CoO's <see cref="MaterialReactionBlueprint"/> loading
    /// convention so a new liquid is a JSON row, not a C# class.
    ///
    /// <para>Unity <see cref="UnityEngine.JsonUtility"/> cannot
    /// deserialize <c>Dictionary</c> or nested generics beyond
    /// <c>List&lt;[Serializable]&gt;</c>, so the modifier collections
    /// are <c>List&lt;LiquidStatMod&gt;</c> and per-turn damage is a
    /// nested <c>[Serializable]</c> class — the same shape that works
    /// for <see cref="MaterialReactionBlueprint"/>.</para>
    /// </summary>
    [Serializable]
    public class LiquidDefinition
    {
        /// <summary>Stable lookup key (e.g. "water", "oil", "acid").</summary>
        public string Id;

        /// <summary>Human-readable noun ("water").</summary>
        public string DisplayName;

        /// <summary>Coat adjective shown on the creature ("wet",
        /// "oily", "acid-covered").</summary>
        public string Adjective;

        /// <summary>Pool glyph + CGA color (for LQ.3 puddle render).</summary>
        public string Glyph = "~";
        public string Color = "&c";

        /// <summary>Electric-damage amplification driver (Qud's
        /// MixedElectricalConductivity). Water ~100, oil 0.</summary>
        public int Conductivity;

        /// <summary>Fire-damage amplification driver (Qud's
        /// Combustibility). Oil 90, water negative.</summary>
        public int Combustibility;

        /// <summary>Percent fire-damage REDUCTION while coated
        /// (water damps fire). 0 = none.</summary>
        public int FireDampen;

        /// <summary>Ignition point for the coat/soaked items
        /// (Qud's FlameTemperature). 99999 = won't ignite.</summary>
        public int FlameTemperature = 99999;

        /// <summary>How readily it coats per contact (Qud's
        /// Adsorbence). 100 = fully.</summary>
        public int Adsorbence = 100;

        /// <summary>Drip-off amount per turn (Qud's Fluidity).</summary>
        public int Fluidity;

        /// <summary>Evaporation amount per turn (Qud's
        /// Evaporativity).</summary>
        public int Evaporativity;

        /// <summary>Stain conversion rate (LQ.8 deferred — carried
        /// now so the data shape is stable).</summary>
        public int Staining;

        /// <summary>Pool applies a slip on enter (LQ.5).</summary>
        public bool Slippery;

        /// <summary>Pool applies stuck/slow on enter (honey-class,
        /// LQ.5/LQ.6).</summary>
        public bool Sticky;

        /// <summary>Ongoing damage each turn the coat persists
        /// (acid-class). Null/zero = no tick.</summary>
        public LiquidPerTurnDamage PerTurnDamage;

        /// <summary>Combat-stat deltas applied while coated, reversed
        /// on removal (LQ.6 stat liquids — pitch, ichor).</summary>
        public List<LiquidStatMod> StatModifiers;

        /// <summary>Resistance-stat deltas applied while coated,
        /// reversed on removal (LQ.6 — brine, ichor).</summary>
        public List<LiquidStatMod> ResistanceModifiers;

        /// <summary>Optional status-effect name applied while coated
        /// (LQ.5). Empty = none.</summary>
        public string FollowOnEffect;

        /// <summary>LB: if &gt; 0, the coat attaches a
        /// <c>LightSourcePart</c> with this radius (lantern-beetle
        /// ichor). 0 = no light. Wired by
        /// <c>LiquidCoveredEffect.OnApply/OnRemove</c>.</summary>
        public int LightRadius;

        /// <summary>LB: color code (e.g. "&amp;Y") for the attached
        /// LightSourcePart. Ignored when <see cref="LightRadius"/> is 0.</summary>
        public string LightColor;

        /// <summary>LB: if &gt; 0, the coat intercepts a killing blow
        /// (memory-bath one-shot resurrection). Restores HP to
        /// <c>Max * percent / 100</c>, consumes the coat. 0 = no anchor.
        /// Wired by <c>LiquidCoveredEffect.OnBeforeTakeDamage</c>.</summary>
        public int DeathAnchorPercent;

        /// <summary>LA: if non-empty, incoming damage carrying the matching
        /// elemental flag is fully nullified (Amount→0). Distinct from
        /// resistance, which scales. One of "Heat", "Cold", "Electric",
        /// "Acid" — matched via <c>Damage.Is{Element}Damage()</c> so the
        /// alias-collapse precedent (Lightning→Electric, Fire→Heat, etc.)
        /// applies automatically. Empty = no immunity.
        /// Wired by <c>LiquidCoveredEffect.OnBeforeTakeDamage</c>
        /// (veined-pulse-mycelium).</summary>
        public string ImmuneElement;

        /// <summary>LA: if &gt; 0, X% of the damage that lands on the wearer
        /// is dealt back to the attacker on a separate <c>ApplyDamage</c>
        /// call with <c>source=null</c> — the null source is the
        /// cycle-breaker that prevents two mirror-coated entities from
        /// infinitely bouncing damage. 0 = no reflect.
        /// Wired by <c>LiquidCoveredEffect.OnTakeDamage</c>
        /// (choir-mirror-mucilage).</summary>
        public int ReflectPercent;

        /// <summary>LA: if true, the coat snapshots HP at
        /// <c>OnApply</c>/<c>OnTurnStart</c> and writes it back at
        /// <c>OnTurnEnd</c> BEFORE the dry-down — damage taken during
        /// the turn is undone (felling-counter resin, Antikythera-style
        /// time-tech, §L3). Only undoes net damage (a higher current HP
        /// from intra-turn healing is preserved); does not resurrect
        /// (a dead wearer at OnTurnEnd is not revived).
        /// Wired by <c>LiquidCoveredEffect.OnTurnStart/OnTurnEnd</c>
        /// (felling-counter-resin).</summary>
        public bool HpRewindOnTurnEnd;
    }

    [Serializable]
    public class LiquidPerTurnDamage
    {
        public int Amount;
        public string Type;
    }

    [Serializable]
    public class LiquidStatMod
    {
        public string Stat;
        public int Delta;
    }

    [Serializable]
    public class LiquidDefinitionCollection
    {
        public List<LiquidDefinition> Liquids;
    }
}
