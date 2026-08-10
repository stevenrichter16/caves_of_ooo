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
    /// Static lookup of inline tooltip text for the grimoire-granted
    /// activated mutations. Keys match the mutation class name exactly so that
    /// <see cref="ActivatedAbility.SourceMutationClass"/> can be used as the
    /// lookup key directly.
    ///
    /// <para><b>Load-bearing beyond tooltips:</b> InventoryUI's grimoire
    /// picker filters the "learned grimoires" list with
    /// <see cref="IsGrimoireMutation"/> — a grimoire-taught mutation
    /// missing from this table is INVISIBLE in the picker, and once its
    /// hotbar slot is reassigned it can only be re-bound through the
    /// M-key ability manager. Every new grimoire spell must add a row
    /// here (SM7d, farming audit F3 — Conjure Rain shipped without one;
    /// the older utility spells still lack rows, tracked separately).</para>
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
            { nameof(ConjureRainMutation), new GrimoireTooltip {
                DisplayName = "Conjure Rain",
                ColorCode   = "&B",
                Flavor      = "Clouds gather at your quiet call.",
                Mechanics   = "Waters crops \u2022 Radius 3 \u2022 CD 5",
                Signature   = "Soaks soil for 40 ticks \u2022 darkens wet earth"
            } },

            // \u2500\u2500 Rites (SM7-SM9) \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500
            // All eleven, not just the six SM9 added. The five older
            // rites shipped without rows too, so every rite in the game
            // was invisible in the grimoire picker; a cold-eye audit
            // caught it on the new ones and the same fix covers the old.
            // GrimoireTooltipDataCompletenessTests now enforces the
            // docstring's "must" so the next rite cannot forget.
            { nameof(StormAnvilMutation), new GrimoireTooltip {
                DisplayName = "Storm Anvil",
                ColorCode   = "&Y",
                Flavor      = "Thunder pools in the gutter of the page.",
                Mechanics   = "Nova 2 \u2022 spends 2 statuses \u2022 CD 25",
                Signature   = "Consumes Wet/Electrified/Frozen for huge damage"
            } },
            { nameof(HangingBoltMutation), new GrimoireTooltip {
                DisplayName = "Hanging Bolt",
                ColorCode   = "&Y",
                Flavor      = "The bolt waits, patient, overhead.",
                Mechanics   = "Line 6 \u2022 spends 2 statuses \u2022 CD 30",
                Signature   = "Each mark becomes no-save Paralysis, not damage"
            } },
            { nameof(RenderedSteamMutation), new GrimoireTooltip {
                DisplayName = "Rendered Steam",
                ColorCode   = "&W",
                Flavor      = "Water and fire, made to agree at last.",
                Mechanics   = "Radius 2 \u2022 spends 2 statuses \u2022 CD 30",
                Signature   = "Wants Wet AND Burning together \u2022 blinds"
            } },
            { nameof(ScaldingVeilMutation), new GrimoireTooltip {
                DisplayName = "Scalding Veil",
                ColorCode   = "&W",
                Flavor      = "You wear your own drenching as armour.",
                Mechanics   = "Self \u2022 spends YOUR Wet \u2022 CD 35",
                Signature   = "Retaliation aura \u2022 scalds and confuses attackers"
            } },
            { nameof(FulminationMutation), new GrimoireTooltip {
                DisplayName = "Fulmination",
                ColorCode   = "&Y",
                Flavor      = "Charge left in the ground, for the world to spend.",
                Mechanics   = "Line 5 \u2022 spends 1 status \u2022 CD 25",
                Signature   = "Writes Charge to the tile \u2022 water and metal carry it"
            } },
            { nameof(ShatteredRimeMutation), new GrimoireTooltip {
                DisplayName = "Shattered Rime",
                ColorCode   = "&C",
                Flavor      = "What is frozen is not armoured. It is brittle.",
                Mechanics   = "Cone 3 \u2022 spends 2 statuses \u2022 CD 40",
                Signature   = "Untyped shatter \u2022 breaks ice creatures too"
            } },
            { nameof(StillHeartMutation), new GrimoireTooltip {
                DisplayName = "Still Heart",
                ColorCode   = "&c",
                Flavor      = "A heart taught to forget to hurry.",
                Mechanics   = "Single 5 \u2022 spends 2 statuses \u2022 CD 45",
                Signature   = "Sleeps an elite 8 turns per mark \u2022 wakes on damage"
            } },
            { nameof(VerdigrisBloomMutation), new GrimoireTooltip {
                DisplayName = "Verdigris Bloom",
                ColorCode   = "&g",
                Flavor      = "Green rot flowers across the margin.",
                Mechanics   = "Radius 2 \u2022 spends 2 statuses \u2022 CD 40",
                Signature   = "Strips armour and re-seeds acid across the radius"
            } },
            { nameof(HollowCoinMutation), new GrimoireTooltip {
                DisplayName = "Hollow Coin",
                ColorCode   = "&W",
                Flavor      = "Whatever is owed, it pays.",
                Mechanics   = "Single 4 \u2022 spends 3 of ANY \u2022 CD 50",
                Signature   = "Untyped burst \u2022 no elemental ward stops it"
            } },
            { nameof(SunderingWordMutation), new GrimoireTooltip {
                DisplayName = "Sundering Word",
                ColorCode   = "&K",
                Flavor      = "One word, written far too many times.",
                Mechanics   = "Radius 2 \u2022 spends 2 of ANY \u2022 CD 45",
                Signature   = "Broken + Weakened on everything \u2022 barely hurts"
            } },
            { nameof(BloodletterLedgerMutation), new GrimoireTooltip {
                DisplayName = "Bloodletter's Ledger",
                ColorCode   = "&r",
                Flavor      = "Two columns, in two different inks.",
                Mechanics   = "Single 5 \u2022 spends 2 of ANY \u2022 CD 40",
                Signature   = "Deep bleed \u2022 heals YOU 4 per mark spent"
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
