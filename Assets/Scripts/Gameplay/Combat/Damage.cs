using System;
using System.Collections.Generic;

namespace CavesOfOoo.Core
{
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
        /// </summary>
        public void AddAttribute(string name)
        {
            Attributes.Add(name);
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

        /// <summary>True if this damage is Cold, Ice, or Freeze.</summary>
        public bool IsColdDamage()
        {
            return HasAttribute("Cold") || HasAttribute("Ice") || HasAttribute("Freeze");
        }

        /// <summary>True if this damage is Fire or Heat.</summary>
        public bool IsHeatDamage()
        {
            return HasAttribute("Fire") || HasAttribute("Heat");
        }

        /// <summary>True if this damage is Electric/Shock/Lightning/Electricity.</summary>
        public bool IsElectricDamage()
        {
            return HasAttribute("Electric") || HasAttribute("Shock")
                || HasAttribute("Lightning") || HasAttribute("Electricity");
        }

        /// <summary>True if this damage is Cudgel or Bludgeoning.</summary>
        public bool IsBludgeoningDamage()
        {
            return HasAttribute("Cudgel") || HasAttribute("Bludgeoning");
        }

        /// <summary>True if this damage carries the Acid attribute.</summary>
        public bool IsAcidDamage()
        {
            return HasAttribute("Acid");
        }

        /// <summary>True if this damage is Light or Laser.</summary>
        public bool IsLightDamage()
        {
            return HasAttribute("Light") || HasAttribute("Laser");
        }

        /// <summary>True if this damage is Disintegrate or Disintegration.</summary>
        public bool IsDisintegrationDamage()
        {
            return HasAttribute("Disintegrate") || HasAttribute("Disintegration");
        }
    }
}
