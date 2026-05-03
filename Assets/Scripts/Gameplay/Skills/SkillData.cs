using System;
using System.Collections.Generic;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// JSON-loaded definition for one skill tree. Mirrors Qud's
    /// <c>SkillEntry</c> (XRL.World.Skills/SkillEntry.cs) flattened with
    /// the inherited fields from <c>IPartEntry</c> + <c>IBaseSkillEntry</c>:
    ///
    /// <para>Field provenance (per
    /// /Users/steven/qud-decompiled-project/XRL/IPartEntry.cs +
    /// IBaseSkillEntry.cs + XRL.World.Skills/SkillEntry.cs):</para>
    /// <list type="bullet">
    ///   <item>From <c>IPartEntry</c>: Name, Class, Attribute, Snippet,
    ///         Cost, Flags (with FLAG_HIDDEN=1, FLAG_OBFUSCATED=2,
    ///         FLAG_INITIATORY=4, FLAG_EX_POOL=8 bits).</item>
    ///   <item>From <c>IBaseSkillEntry</c>: Tile, Foreground, Detail,
    ///         Description.</item>
    ///   <item>From <c>SkillEntry</c>: <c>Powers</c> child list.</item>
    /// </list>
    ///
    /// <para>v1 ships these fields for parity but only USES Name / Class /
    /// Cost / Description / Powers in the runtime path. UI fields (Tile /
    /// Foreground / Detail) and the lesser-used Snippet field are loaded
    /// + round-tripped but not yet rendered. Carrying the full schema
    /// from day 1 means content authors can add fields incrementally
    /// without re-keying existing JSON.</para>
    ///
    /// <para><b>JsonUtility constraint:</b> POCO uses public fields (not
    /// properties), no inheritance, default values inline — matches
    /// <see cref="CavesOfOoo.Core.MutationDefinition"/> precedent.
    /// Inheritance was considered (mirroring Qud's IPartEntry → ...
    /// → SkillEntry chain) but flattened for JsonUtility safety.</para>
    /// </summary>
    [Serializable]
    public class SkillData
    {
        // ── Inherited from IPartEntry ─────────────────────────────────────

        /// <summary>Display name shown in the UI ("Acrobatics", "Long Blades").</summary>
        public string Name = "";

        /// <summary>The C# Part class implementing this skill at runtime
        /// (e.g. "AcrobaticsSkill"). Used by SkillsPart.AddSkill to
        /// reflectively instantiate via Type.GetType.</summary>
        public string Class = "";

        /// <summary>Stat-name list paired with <see cref="Minimum"/>
        /// (PowerData only; SkillData inherits the field for parity but
        /// skill trees in Qud don't use it). See PowerData docstring for
        /// pipe/comma format.</summary>
        public string Attribute = "";

        /// <summary>Short flavor text fragment for content blurbs;
        /// distinct from Description. Mirrors Qud's IPartEntry.Snippet.</summary>
        public string Snippet = "";

        /// <summary>SP cost to purchase. Default -999 matches Qud's
        /// SkillFactory sentinel — "missing or unset" — so JSON authors
        /// MUST specify Cost explicitly or the registry will warn.</summary>
        public int Cost = -999;

        /// <summary>Bitfield (per IPartEntry):
        ///   bit 0 (1) = HIDDEN — entry hidden in pool until acquired
        ///   bit 1 (2) = OBFUSCATED — name renders as "???" until
        ///               requirements met
        ///   bit 2 (4) = INITIATORY — tree must be bought in order
        ///   bit 3 (8) = EX_POOL — excluded from random skill-pool
        ///               acquisition.
        /// Bit accessors are available via <see cref="Hidden"/>,
        /// <see cref="Obfuscated"/>, <see cref="Initiatory"/>,
        /// <see cref="ExcludeFromPool"/>.</summary>
        public int Flags = 0;

        // ── IPartEntry flag accessors (mirror Qud's bit semantics) ────────

        public const int FLAG_HIDDEN     = 1;
        public const int FLAG_OBFUSCATED = 2;
        public const int FLAG_INITIATORY = 4;
        public const int FLAG_EX_POOL    = 8;

        public bool Hidden          { get => (Flags & FLAG_HIDDEN)     != 0; set => Flags = value ? (Flags | FLAG_HIDDEN)     : (Flags & ~FLAG_HIDDEN);     }
        public bool Obfuscated      { get => (Flags & FLAG_OBFUSCATED) != 0; set => Flags = value ? (Flags | FLAG_OBFUSCATED) : (Flags & ~FLAG_OBFUSCATED); }
        public bool Initiatory      { get => (Flags & FLAG_INITIATORY) != 0; set => Flags = value ? (Flags | FLAG_INITIATORY) : (Flags & ~FLAG_INITIATORY); }
        public bool ExcludeFromPool { get => (Flags & FLAG_EX_POOL)    != 0; set => Flags = value ? (Flags | FLAG_EX_POOL)    : (Flags & ~FLAG_EX_POOL);    }

        // ── Inherited from IBaseSkillEntry (UI-render fields) ───────────

        /// <summary>Sprite/tile reference for UI (Qud renders a
        /// per-skill icon). Used by ST.7's UI overlay; loaded at
        /// ST.2 for forward compat.</summary>
        public string Tile = "";

        /// <summary>Qud color code for the foreground / glyph color
        /// in the UI ("w" = white, "g" = green, etc.). Default "w".</summary>
        public string Foreground = "w";

        /// <summary>Qud color code for the detail / accent color in
        /// the UI. Default "B".</summary>
        public string Detail = "B";

        /// <summary>Long-form description shown in the skill-info
        /// popup. Authoring-team writes this per-skill.</summary>
        public string Description = "";

        // ── SkillEntry-specific ──────────────────────────────────────────

        /// <summary>Child powers within this tree. Order matters for
        /// initiatory trees (must-buy-in-order). Empty list = a tree
        /// with no individual powers — unusual but supported.</summary>
        public List<PowerData> Powers = new List<PowerData>();
    }
}
