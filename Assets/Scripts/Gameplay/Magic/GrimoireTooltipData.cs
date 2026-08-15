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
    /// activated powers. Keys match the SKILL class name exactly so that
    /// <see cref="ActivatedAbility.SourcePowerClass"/> can be used as the
    /// lookup key directly.
    ///
    /// <para><b>Load-bearing beyond tooltips:</b> InventoryUI's grimoire
    /// picker filters the "learned grimoires" list with
    /// <see cref="IsGrimoirePower"/> — a grimoire-taught power
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
            { "Pyromancy_KindleFlame", new GrimoireTooltip {
                DisplayName = "Kindle Flame",
                ColorCode   = "&r",
                Flavor      = "The smallest flame, coaxed rather than commanded.",
                Mechanics   = "Warms adjacent tinder \u2022 CD 2",
                Signature   = "150J to low-flashpoint scenery; free if nothing catches"
            } },
            { "Pyromancy_Hearthwarm", new GrimoireTooltip {
                DisplayName = "Hearthwarm",
                ColorCode   = "&r",
                Flavor      = "A hearth's kindness, carried in the palm.",
                Mechanics   = "3-turn warming aura \u2022 CD 4",
                Signature   = "60J per pulse to a chosen adjacent cell"
            } },
            { "Pyromancy_FlamingHands", new GrimoireTooltip {
                DisplayName = "Flaming Hands",
                ColorCode   = "&R",
                Flavor      = "Fire answers an open hand.",
                Mechanics   = "1d4 fire, point-blank cell \u2022 CD 10",
                Signature   = "Lights the ground itself \u2014 oil slicks catch"
            } },
            { "Pyromancy_Kindle", new GrimoireTooltip {
                DisplayName = "Kindle",
                ColorCode   = "&R",
                Flavor      = "A spark of summoned flame.",
                Mechanics   = "1d4 fire \u2022 Range 5 \u2022 CD 6",
                Signature   = "Ignites combustibles via a 600J heat pulse"
            } },
            { "Hydromancy_Quench", new GrimoireTooltip {
                DisplayName = "Quench",
                ColorCode   = "&B",
                Flavor      = "A burst of conjured water.",
                Mechanics   = "1d3 \u2022 Range 5 \u2022 CD 6",
                Signature   = "Soaks targets - amplifies later electricity"
            } },
            { "Pyromancy_Conflagration", new GrimoireTooltip {
                DisplayName = "Conflagration",
                ColorCode   = "&r",
                Flavor      = "Roaring flame engulfs you.",
                Mechanics   = "2d6 fire AoE \u2022 Radius 2 \u2022 CD 18",
                Signature   = "Ignites every combustible in radius"
            } },
            { "Cryomancy_IceLance", new GrimoireTooltip {
                DisplayName = "Ice Lance",
                ColorCode   = "&C",
                Flavor      = "A lance of bitter cold.",
                Mechanics   = "1d6 cold \u2022 Range 6 \u2022 CD 8",
                Signature   = "Shatters brittle frozen metal"
            } },
            { "Corrosion_AcidSpray", new GrimoireTooltip {
                DisplayName = "Acid Spray",
                ColorCode   = "&g",
                Flavor      = "Corrosive vapor coats the target.",
                Mechanics   = "1d4 acid \u2022 Range 4 \u2022 CD 10",
                Signature   = "Degrades organic combustibility over time"
            } },
            { "Galvanism_ArcBolt", new GrimoireTooltip {
                DisplayName = "Arc Bolt",
                ColorCode   = "&Y",
                Flavor      = "A snapping bolt of charge.",
                Mechanics   = "1d8 lightning \u2022 Range 5 \u2022 CD 7",
                Signature   = "Doubles damage on wet \u2022 chains conductors"
            } },
            { "Cryomancy_RimeNova", new GrimoireTooltip {
                DisplayName = "Rime Nova",
                ColorCode   = "&b",
                Flavor      = "Frost detonates outward.",
                Mechanics   = "1d6 cold AoE \u2022 Radius 2 \u2022 CD 15",
                Signature   = "Extinguishes burning props \u2022 freezes creatures"
            } },
            { "Galvanism_Thunderclap", new GrimoireTooltip {
                DisplayName = "Thunderclap",
                ColorCode   = "&W",
                Flavor      = "Thunder rolls from your hands.",
                Mechanics   = "2d6 lightning AoE \u2022 Radius 2 \u2022 CD 18",
                Signature   = "Doubles damage on wet \u2022 electrifies metal props"
            } },
            { "Pyromancy_EmberVein", new GrimoireTooltip {
                DisplayName = "Ember Vein",
                ColorCode   = "&r",
                Flavor      = "A vein of fire traces the path.",
                Mechanics   = "2d6 fire beam \u2022 Range 7 \u2022 CD 12",
                Signature   = "Heat pulse ignites every combustible in line"
            } },
            { "Hydromancy_ConjureRain", new GrimoireTooltip {
                DisplayName = "Conjure Rain",
                ColorCode   = "&B",
                Flavor      = "Clouds gather at your quiet call.",
                Mechanics   = "Waters crops \u2022 Radius 3 \u2022 CD 5",
                Signature   = "Soaks soil for 40 ticks \u2022 darkens wet earth"
            } },
            { "Hydromancy_ConjureWater", new GrimoireTooltip {
                DisplayName = "Conjure Water",
                ColorCode   = "&B",
                Flavor      = "A stream that seeks the flame.",
                Mechanics   = "Spawns a puddle \u2022 Range 2 \u2022 CD 4",
                Signature   = "Lands on burning things first \u2022 soaks the cell"
            } },
            { "Hydromancy_DryingBreeze", new GrimoireTooltip {
                DisplayName = "Drying Breeze",
                ColorCode   = "&b",
                Flavor      = "A warm breath against the damp.",
                Mechanics   = "Strips Wet \u2022 Radius 1 \u2022 CD 3",
                Signature   = "Dries everything adjacent, yourself included"
            } },
            { "Cryomancy_ChillDraft", new GrimoireTooltip {
                DisplayName = "Chill Draft",
                ColorCode   = "&c",
                Flavor      = "A soft exhalation of cold.",
                Mechanics   = "\u2212100J \u2022 Radius 1 \u2022 CD 5",
                Signature   = "Snuffs embers \u2022 cools overheating gear"
            } },
            { "Spellcraft_WardGleam", new GrimoireTooltip {
                DisplayName = "Ward Gleam",
                ColorCode   = "&M",
                Flavor      = "A gleam that scours the ruin from your gear.",
                Mechanics   = "Cleanses equipment \u2022 Self \u2022 CD 15",
                Signature   = "Strips Acidic + Charred \u2022 free if nothing to cleanse"
            } },
            { "Spellcraft_Calm", new GrimoireTooltip {
                DisplayName = "Calm",
                ColorCode   = "&M",
                Flavor      = "A word that unclenches the fist.",
                Mechanics   = "Pacifies 50 turns \u2022 Range 6 \u2022 CD 20",
                Signature   = "No damage \u2022 does not stack on the peaceful"
            } },

            // \u2500\u2500 Rites (SM7-SM9) \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500
            // All eleven, not just the six SM9 added. The five older
            // rites shipped without rows too, so every rite in the game
            // was invisible in the grimoire picker; a cold-eye audit
            // caught it on the new ones and the same fix covers the old.
            // GrimoireTooltipDataCompletenessTests now enforces the
            // docstring's "must" so the next rite cannot forget.
            { "Rites_StormAnvil", new GrimoireTooltip {
                DisplayName = "Storm Anvil",
                ColorCode   = "&Y",
                Flavor      = "Thunder pools in the gutter of the page.",
                Mechanics   = "Nova 2 \u2022 spends 2 statuses \u2022 CD 25",
                Signature   = "Consumes Wet/Electrified/Frozen for huge damage"
            } },
            { "Rites_HangingBolt", new GrimoireTooltip {
                DisplayName = "Hanging Bolt",
                ColorCode   = "&Y",
                Flavor      = "The bolt waits, patient, overhead.",
                Mechanics   = "Line 6 \u2022 spends 2 statuses \u2022 CD 30",
                Signature   = "Each mark becomes no-save Paralysis, not damage"
            } },
            { "Rites_RenderedSteam", new GrimoireTooltip {
                DisplayName = "Rendered Steam",
                ColorCode   = "&W",
                Flavor      = "Water and fire, made to agree at last.",
                Mechanics   = "Radius 2 \u2022 spends 2 statuses \u2022 CD 30",
                Signature   = "Wants Wet AND Burning together \u2022 blinds"
            } },
            { "Rites_ScaldingVeil", new GrimoireTooltip {
                DisplayName = "Scalding Veil",
                ColorCode   = "&W",
                Flavor      = "You wear your own drenching as armour.",
                Mechanics   = "Self \u2022 spends YOUR Wet \u2022 CD 35",
                Signature   = "Retaliation aura \u2022 scalds and confuses attackers"
            } },
            { "Rites_Fulmination", new GrimoireTooltip {
                DisplayName = "Fulmination",
                ColorCode   = "&Y",
                Flavor      = "Charge left in the ground, for the world to spend.",
                Mechanics   = "Line 5 \u2022 spends 1 status \u2022 CD 25",
                Signature   = "Writes Charge to the tile \u2022 water and metal carry it"
            } },
            { "Rites_ShatteredRime", new GrimoireTooltip {
                DisplayName = "Shattered Rime",
                ColorCode   = "&C",
                Flavor      = "What is frozen is not armoured. It is brittle.",
                Mechanics   = "Cone 3 \u2022 spends 2 statuses \u2022 CD 40",
                Signature   = "Untyped shatter \u2022 breaks ice creatures too"
            } },
            { "Rites_StillHeart", new GrimoireTooltip {
                DisplayName = "Still Heart",
                ColorCode   = "&c",
                Flavor      = "A heart taught to forget to hurry.",
                Mechanics   = "Single 5 \u2022 spends 2 statuses \u2022 CD 45",
                Signature   = "Sleeps an elite 8 turns per mark \u2022 wakes on damage"
            } },
            { "Rites_VerdigrisBloom", new GrimoireTooltip {
                DisplayName = "Verdigris Bloom",
                ColorCode   = "&g",
                Flavor      = "Green rot flowers across the margin.",
                Mechanics   = "Radius 2 \u2022 spends 2 statuses \u2022 CD 40",
                Signature   = "Strips armour and re-seeds acid across the radius"
            } },
            { "Rites_HollowCoin", new GrimoireTooltip {
                DisplayName = "Hollow Coin",
                ColorCode   = "&W",
                Flavor      = "Whatever is owed, it pays.",
                Mechanics   = "Single 4 \u2022 spends 3 of ANY \u2022 CD 50",
                Signature   = "Untyped burst \u2022 no elemental ward stops it"
            } },
            { "Rites_SunderingWord", new GrimoireTooltip {
                DisplayName = "Sundering Word",
                ColorCode   = "&K",
                Flavor      = "One word, written far too many times.",
                Mechanics   = "Radius 2 \u2022 spends 2 of ANY \u2022 CD 45",
                Signature   = "Broken + Weakened on everything \u2022 barely hurts"
            } },
            { "Rites_BloodletterLedger", new GrimoireTooltip {
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
        public static bool IsGrimoirePower(string mutationClassName)
        {
            if (string.IsNullOrEmpty(mutationClassName))
                return false;
            return _data.ContainsKey(mutationClassName);
        }
    }
}
