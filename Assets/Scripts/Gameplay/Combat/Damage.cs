using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Bitmask of common damage-class attributes used by the <c>IsXDamage</c>
    /// helpers on <see cref="Damage"/>. Maintained as a derived index over
    /// <see cref="Damage.Attributes"/> so the helpers can answer with a
    /// single bit test instead of repeated string scans (Tier-B Fix #2).
    ///
    /// <para>Aliases collapse to the same bit — <c>Cold | Ice | Freeze</c>
    /// all set <see cref="Cold"/>; <c>Cudgel | Bludgeoning</c> both set
    /// <see cref="Bludgeoning"/>. The text list still preserves which
    /// alias was used, for systems that care (combat log "freezing" vs
    /// "cold" wording).</para>
    /// </summary>
    [Flags]
    public enum DamageAttributeFlags
    {
        None = 0,
        Cold = 1 << 0,           // Cold | Ice | Freeze
        Heat = 1 << 1,           // Fire | Heat
        Electric = 1 << 2,       // Electric | Shock | Lightning | Electricity
        Bludgeoning = 1 << 3,    // Cudgel | Bludgeoning
        Acid = 1 << 4,           // Acid
        Light = 1 << 5,          // Light | Laser
        Disintegration = 1 << 6, // Disintegrate | Disintegration
    }

    /// <summary>
    /// A piece of damage flowing through the combat pipeline. Mirrors
    /// <c>XRL.World.Damage</c> from the Caves of Qud reference.
    ///
    /// Two pieces of state:
    ///   • <see cref="Amount"/> — the integer damage value, clamped to ≥ 0.
    ///   • <see cref="Attributes"/> — a flexible tag list describing the damage
    ///     (e.g., <c>"Melee"</c>, <c>"Cutting"</c>, <c>"Fire"</c>, <c>"Strength"</c>).
    ///
    /// We use a tag set rather than a single <c>DamageType</c> enum because:
    ///   • A single piece of damage often has multiple meaningful descriptors
    ///     (a flaming sword: Melee + Cutting + Fire + LongBlades + Strength).
    ///   • Resistances, reactions, and event hooks compose by tag-matching
    ///     instead of enum-matching.
    ///   • Stat tracking and achievements can hook on any tag without growing
    ///     a centralized enum.
    ///
    /// See <c>Docs/COMBAT-QUD-PARITY-PORT.md</c> Phase C for the design rationale.
    /// </summary>
    [Serializable]
    public class Damage
    {
        // Backing field is public for parity with Qud's _Amount, which save
        // serialization touches directly. Prefer the property in normal code.
        public int _Amount;

        public List<string> Attributes = new List<string>();

        /// <summary>
        /// Bitmask of well-known attributes that the <c>IsXDamage</c>
        /// helpers test for. Maintained alongside <see cref="Attributes"/>
        /// by <see cref="AddAttribute"/> and <see cref="AddAttributes"/>.
        /// Lets <see cref="IsHeatDamage"/> et al. answer with a single bit
        /// test instead of N <see cref="HasAttribute"/> string scans.
        ///
        /// <para>Tier-B Fix #2 in PERF-COMBAT-INVESTIGATION.md. Per
        /// <c>ApplyDamage</c> on a 5-attribute Damage, the four
        /// <c>IsXDamage</c> helpers used to do ~10 string comparisons; now
        /// they're four bit-and ops.</para>
        ///
        /// <para>Unknown / future attributes (mod content, bespoke tags)
        /// fall through to the original <see cref="HasAttribute"/> string
        /// scan — only the well-known names update the mask. The list is
        /// the source of truth; the mask is a derived index.</para>
        /// </summary>
        public DamageAttributeFlags AttributeFlags;

        public bool SuppressionMessageDone;

        /// <summary>
        /// The damage value. Setter clamps to ≥ 0 (matching Qud).
        /// </summary>
        public int Amount
        {
            get => _Amount;
            set => _Amount = Math.Max(value, 0);
        }

        /// <summary>
        /// Construct a damage instance with a starting amount (clamped to ≥ 0).
        /// </summary>
        public Damage(int amount)
        {
            Amount = amount;
        }

        // -- Attribute predicates -----------------------------------------------------------

        public bool HasAttribute(string name)
        {
            if (Attributes == null) return false;
            return Attributes.Contains(name);
        }

        /// <summary>
        /// Returns true if any attribute of this damage appears in <paramref name="names"/>.
        /// Returns false if either side is null.
        /// </summary>
        public bool HasAnyAttribute(List<string> names)
        {
            if (names == null || Attributes == null) return false;
            for (int i = 0; i < Attributes.Count; i++)
            {
                if (names.Contains(Attributes[i]))
                    return true;
            }
            return false;
        }

        // -- Attribute mutators -------------------------------------------------------------

        /// <summary>
        /// Append a single attribute. Does NOT dedupe — Qud allows duplicates so
        /// counts like "how many Strength tags?" are meaningful in extension code.
        /// Also updates <see cref="AttributeFlags"/> if <paramref name="name"/>
        /// matches a known damage-class alias (Tier-B Fix #2).
        /// </summary>
        public void AddAttribute(string name)
        {
            Attributes.Add(name);
            AttributeFlags |= GetFlagForAttribute(name);
        }

        /// <summary>
        /// Map a known attribute name to its <see cref="DamageAttributeFlags"/>
        /// bit. Returns <see cref="DamageAttributeFlags.None"/> for unknown
        /// names (which fall through to the string-based <see cref="HasAttribute"/>
        /// scan). Aliases collapse — <c>"Ice"</c> and <c>"Freeze"</c> both
        /// return <see cref="DamageAttributeFlags.Cold"/>.
        ///
        /// <para>Ordinal string equality is used for parity with
        /// <c>List&lt;string&gt;.Contains</c> in <see cref="HasAttribute"/>.</para>
        /// </summary>
        private static DamageAttributeFlags GetFlagForAttribute(string name)
        {
            if (string.IsNullOrEmpty(name)) return DamageAttributeFlags.None;
            switch (name)
            {
                case "Cold":
                case "Ice":
                case "Freeze":
                    return DamageAttributeFlags.Cold;
                case "Fire":
                case "Heat":
                    return DamageAttributeFlags.Heat;
                case "Electric":
                case "Shock":
                case "Lightning":
                case "Electricity":
                    return DamageAttributeFlags.Electric;
                case "Cudgel":
                case "Bludgeoning":
                    return DamageAttributeFlags.Bludgeoning;
                case "Acid":
                    return DamageAttributeFlags.Acid;
                case "Light":
                case "Laser":
                    return DamageAttributeFlags.Light;
                case "Disintegrate":
                case "Disintegration":
                    return DamageAttributeFlags.Disintegration;
                default:
                    return DamageAttributeFlags.None;
            }
        }

        /// <summary>
        /// Append zero or more attributes from a space-separated string. Empty
        /// or null input is a no-op.
        /// </summary>
        public void AddAttributes(string list)
        {
            if (string.IsNullOrEmpty(list)) return;
            if (list.Contains(" "))
            {
                string[] parts = list.Split(' ');
                for (int i = 0; i < parts.Length; i++)
                    AddAttribute(parts[i]);
            }
            else
            {
                AddAttribute(list);
            }
        }

        // -- Damage-type alias helpers (mirror Qud) -----------------------------------------
        // Each helper checks for any of several alias attributes that all denote
        // the same conceptual damage type. Used by resistance code, status effect
        // listeners, and visual-effects code to react to damage by category.

        // Bit-test helpers — single AND op against the cached
        // AttributeFlags bitmask, replacing the 1–4 string scans the
        // pre-Fix-#2 implementations did. Behavior is identical to the
        // original alias lists; see DamageAttributeFlags for which names
        // map to which bit.

        /// <summary>True if this damage is Cold, Ice, or Freeze.</summary>
        public bool IsColdDamage() => (AttributeFlags & DamageAttributeFlags.Cold) != 0;

        /// <summary>True if this damage is Fire or Heat.</summary>
        public bool IsHeatDamage() => (AttributeFlags & DamageAttributeFlags.Heat) != 0;

        /// <summary>True if this damage is Electric/Shock/Lightning/Electricity.</summary>
        public bool IsElectricDamage() => (AttributeFlags & DamageAttributeFlags.Electric) != 0;

        /// <summary>True if this damage is Cudgel or Bludgeoning.</summary>
        public bool IsBludgeoningDamage() => (AttributeFlags & DamageAttributeFlags.Bludgeoning) != 0;

        /// <summary>True if this damage carries the Acid attribute.</summary>
        public bool IsAcidDamage() => (AttributeFlags & DamageAttributeFlags.Acid) != 0;

        /// <summary>True if this damage is Light or Laser.</summary>
        public bool IsLightDamage() => (AttributeFlags & DamageAttributeFlags.Light) != 0;

        /// <summary>True if this damage is Disintegrate or Disintegration.</summary>
        public bool IsDisintegrationDamage() => (AttributeFlags & DamageAttributeFlags.Disintegration) != 0;
    }
}
