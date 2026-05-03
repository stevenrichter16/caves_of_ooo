using System;

namespace CavesOfOoo.Skills
{
    /// <summary>
    /// JSON-loaded definition for one power within a skill tree.
    /// Mirrors Qud's <c>PowerEntry</c> (XRL.World.Skills/PowerEntry.cs)
    /// flattened with the inherited fields from <c>IPartEntry</c> +
    /// <c>IBaseSkillEntry</c>. Same flatten-don't-inherit rationale as
    /// <see cref="SkillData"/> for JsonUtility compatibility.
    ///
    /// <para>PowerData carries the same field set as SkillData (Name /
    /// Class / Attribute / Snippet / Cost / Flags / Tile / Foreground /
    /// Detail / Description) plus three power-specific gating fields
    /// (Minimum, Requires, Exclusion) and one back-reference (ParentSkillName).
    /// The duplication is deliberate — JsonUtility doesn't deserialize
    /// inherited fields reliably, so each POCO is self-contained.</para>
    /// </summary>
    [Serializable]
    public class PowerData
    {
        // ── Inherited from IPartEntry (duplicated from SkillData) ────────

        public string Name = "";
        public string Class = "";
        public string Attribute = "";
        public string Snippet = "";
        public int Cost = -999;
        public int Flags = 0;

        public bool Hidden          { get => (Flags & SkillData.FLAG_HIDDEN)     != 0; set => Flags = value ? (Flags | SkillData.FLAG_HIDDEN)     : (Flags & ~SkillData.FLAG_HIDDEN);     }
        public bool Obfuscated      { get => (Flags & SkillData.FLAG_OBFUSCATED) != 0; set => Flags = value ? (Flags | SkillData.FLAG_OBFUSCATED) : (Flags & ~SkillData.FLAG_OBFUSCATED); }
        public bool Initiatory      { get => (Flags & SkillData.FLAG_INITIATORY) != 0; set => Flags = value ? (Flags | SkillData.FLAG_INITIATORY) : (Flags & ~SkillData.FLAG_INITIATORY); }
        public bool ExcludeFromPool { get => (Flags & SkillData.FLAG_EX_POOL)    != 0; set => Flags = value ? (Flags | SkillData.FLAG_EX_POOL)    : (Flags & ~SkillData.FLAG_EX_POOL);    }

        // ── Inherited from IBaseSkillEntry ───────────────────────────────

        public string Tile = "";
        public string Foreground = "w";
        public string Detail = "B";
        public string Description = "";

        // ── PowerEntry-specific (gating) ─────────────────────────────────

        /// <summary>Stat-min threshold per Qud's pipe/comma format,
        /// paired with <see cref="Attribute"/>. Two delimiters:
        /// <list type="bullet">
        ///   <item>'|' (pipe) splits OR groups — passing any group is enough.</item>
        ///   <item>',' (comma) splits AND-conjuncts within a group — all must pass.</item>
        /// </list>
        /// <para>Examples (verified against PowerEntry.cs:46-61, 124-139):
        /// <list type="bullet">
        ///   <item>Single: Attribute="Agility" Minimum="15" → Agility ≥ 15.</item>
        ///   <item>AND: Attribute="Agility,Ego" Minimum="18,12" → Agility≥18 AND Ego≥12.</item>
        ///   <item>OR: Attribute="Agility|Strength" Minimum="18|18" → Agility≥18 OR Strength≥18.</item>
        /// </list></para>
        /// <para>v1 ST.2 stores the raw string. Parser ships with ST.6
        /// (BuySkillAction). Authoring docs in
        /// Docs/SKILL-TREE-QUD-PARITY.md §Gating.</para>
        /// </summary>
        public string Minimum = "";

        /// <summary>Comma-separated list of prereq classes (skills or
        /// powers) the actor must already own to be eligible. Mirrors
        /// PowerEntry.cs:13. Validated at purchase time (ST.6).</summary>
        public string Requires = "";

        /// <summary>Comma-separated list of mutually-exclusive classes;
        /// if the actor owns ANY of them, this power can't be purchased.
        /// Mirrors PowerEntry.cs:15. Validated at purchase time (ST.6).</summary>
        public string Exclusion = "";

        // ── Back-reference (set by SkillRegistry post-load) ──────────────

        /// <summary>Name of the parent skill tree (set by SkillRegistry
        /// after JSON load — NOT set in JSON content). Used by purchase
        /// validation and UI rendering ("Acrobatics > Dodge"). Mirrors
        /// PowerEntry.ParentSkill but with a string back-ref instead of
        /// a runtime object pointer (POCOs avoid runtime references).</summary>
        [NonSerialized]
        public string ParentSkillName = "";
    }
}
