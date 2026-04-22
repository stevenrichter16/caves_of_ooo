using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Inline description for a single grimoire spell, shown beneath the spell's
    /// name in the Abilities tab and grimoire picker popup.
    /// </summary>
    public struct GrimoireTooltip
    {
        /// <summary>
        /// Short spell name shown on the name line (e.g. "Ice Lance").
        /// </summary>
        public string DisplayName;

        /// <summary>
        /// Qud-style color code (e.g. "&amp;C" for cold, "&amp;R" for fire).
        /// Parsed by <see cref="QudColorParser.Parse"/> to tint the name row
        /// and the theme glyph in the picker popup.
        /// </summary>
        public string ColorCode;

        /// <summary>
        /// One short atmospheric line rendered in dark gray.
        /// Keep under ~60 characters.
        /// </summary>
        public string Flavor;

        /// <summary>
        /// Compact damage / range / cooldown string rendered in white.
        /// Keep under ~60 characters.
        /// </summary>
        public string Mechanics;

        /// <summary>
        /// Plain-English summary of the material-system interaction signature
        /// rendered in bright cyan. Keep under ~70 characters.
        /// </summary>
        public string Signature;
    }

    /// <summary>
    /// Static lookup of inline tooltip text for the nine grimoire-granted
    /// activated mutations. Keys match the mutation class name exactly so that
    /// <see cref="ActivatedAbility.SourceMutationClass"/> can be used as the
    /// lookup key directly.
    /// </summary>
    public static class GrimoireTooltipData
    {
        private static readonly Dictionary<string, GrimoireTooltip> _data =
            new Dictionary<string, GrimoireTooltip>
        {
            { nameof(KindleMutation), new GrimoireTooltip {
                DisplayName = "Kindle",
                ColorCode   = "&R",
                Flavor      = "A spark of summoned flame.",
                Mechanics   = "1d4 fire \u2022 Range 5 \u2022 CD 4",
                Signature   = "Ignites combustibles via +200J heat pulse"
            } },
            { nameof(QuenchMutation), new GrimoireTooltip {
                DisplayName = "Quench",
                ColorCode   = "&B",
                Flavor      = "A burst of conjured water.",
                Mechanics   = "1d4 cold \u2022 Range 5 \u2022 CD 4",
                Signature   = "Soaks targets - amplifies later electricity"
            } },
            { nameof(ConflagrationMutation), new GrimoireTooltip {
                DisplayName = "Conflagration",
                ColorCode   = "&r",
                Flavor      = "Roaring flame engulfs you.",
                Mechanics   = "2d6 fire AoE \u2022 Radius 2 \u2022 CD 18",
                Signature   = "Ignites every combustible in radius"
            } },
            { nameof(IceLanceMutation), new GrimoireTooltip {
                DisplayName = "Ice Lance",
                ColorCode   = "&C",
                Flavor      = "A lance of bitter cold.",
                Mechanics   = "1d6 cold \u2022 Range 6 \u2022 CD 8",
                Signature   = "Shatters brittle frozen metal"
            } },
            { nameof(AcidSprayMutation), new GrimoireTooltip {
                DisplayName = "Acid Spray",
                ColorCode   = "&g",
                Flavor      = "Corrosive vapor coats the target.",
                Mechanics   = "1d4 acid \u2022 Range 4 \u2022 CD 10",
                Signature   = "Degrades organic combustibility over time"
            } },
            { nameof(ArcBoltMutation), new GrimoireTooltip {
                DisplayName = "Arc Bolt",
                ColorCode   = "&Y",
                Flavor      = "A snapping bolt of charge.",
                Mechanics   = "1d8 lightning \u2022 Range 5 \u2022 CD 7",
                Signature   = "Doubles damage on wet \u2022 chains conductors"
            } },
            { nameof(RimeNovaMutation), new GrimoireTooltip {
                DisplayName = "Rime Nova",
                ColorCode   = "&b",
                Flavor      = "Frost detonates outward.",
                Mechanics   = "1d6 cold AoE \u2022 Radius 2 \u2022 CD 15",
                Signature   = "Extinguishes burning props \u2022 freezes creatures"
            } },
            { nameof(ThunderclapMutation), new GrimoireTooltip {
                DisplayName = "Thunderclap",
                ColorCode   = "&W",
                Flavor      = "Thunder rolls from your hands.",
                Mechanics   = "2d6 lightning AoE \u2022 Radius 2 \u2022 CD 18",
                Signature   = "Doubles damage on wet \u2022 electrifies metal props"
            } },
            { nameof(EmberVeinMutation), new GrimoireTooltip {
                DisplayName = "Ember Vein",
                ColorCode   = "&r",
                Flavor      = "A vein of fire traces the path.",
                Mechanics   = "2d6 fire beam \u2022 Range 7 \u2022 CD 12",
                Signature   = "Heat pulse ignites every combustible in line"
            } },
        };

        /// <summary>
        /// Try to look up the inline tooltip for a mutation class name.
        /// </summary>
        public static bool TryGet(string mutationClassName, out GrimoireTooltip tooltip)
        {
            if (string.IsNullOrEmpty(mutationClassName))
            {
                tooltip = default;
                return false;
            }
            return _data.TryGetValue(mutationClassName, out tooltip);
        }

        /// <summary>
        /// Look up the inline tooltip for a mutation class name, returning
        /// <c>default</c> if not found.
        /// </summary>
        public static GrimoireTooltip GetOrDefault(string mutationClassName)
        {
            if (string.IsNullOrEmpty(mutationClassName))
                return default;
            return _data.TryGetValue(mutationClassName, out var t) ? t : default;
        }

        /// <summary>
        /// Enumerate every grimoire mutation class name that has tooltip data.
        /// </summary>
        public static IEnumerable<string> AllClassNames => _data.Keys;

        /// <summary>
        /// True if the given mutation class name has an inline grimoire tooltip.
        /// Used by UI code to filter the "learned grimoires" list in the picker.
        /// </summary>
        public static bool IsGrimoireMutation(string mutationClassName)
        {
            if (string.IsNullOrEmpty(mutationClassName))
                return false;
            return _data.ContainsKey(mutationClassName);
        }
    }
}
