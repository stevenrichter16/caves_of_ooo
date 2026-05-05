using System.Collections.Generic;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Pure-data snapshot consumed by the (forthcoming) Skills-screen UI
    /// renderer. Mirrors <see cref="QuestLogSnapshot"/> + <see cref="HotbarSnapshot"/>
    /// pattern — state-builder is testable in EditMode without Tilemap setup.
    ///
    /// Per Docs/SKILL-TREE-QUD-PARITY.md ST.7a. The MonoBehaviour
    /// rendering layer that consumes this snapshot (with hotkey 'x'
    /// binding + centered tilemap popup) is ST.7b — same staging as
    /// QuestLogStateBuilder shipped before its UI MonoBehaviour.
    /// </summary>
    public readonly struct SkillsScreenSnapshot
    {
        public readonly IReadOnlyList<SkillsScreenRow> Rows;
        public readonly int CurrentSP;
        public readonly int RowCount;

        public SkillsScreenSnapshot(IReadOnlyList<SkillsScreenRow> rows, int currentSP)
        {
            Rows = rows ?? System.Array.Empty<SkillsScreenRow>();
            CurrentSP = currentSP;
            RowCount = Rows.Count;
        }
    }

    /// <summary>
    /// One row in the skills screen. Two row shapes flattened together:
    /// <list type="bullet">
    ///   <item><b>Tree-root row</b>: <see cref="IsTreeRoot"/> = true,
    ///         <see cref="ParentSkillName"/> = empty. Owning the tree
    ///         unlocks its powers.</item>
    ///   <item><b>Power row</b>: <see cref="IsTreeRoot"/> = false,
    ///         <see cref="ParentSkillName"/> = display name of the parent
    ///         skill (for indentation / grouping in the UI).</item>
    /// </list>
    /// </summary>
    public readonly struct SkillsScreenRow
    {
        /// <summary>Runtime class identifier (e.g. "AcrobaticsSkill").
        /// Used by the UI's purchase callback when the player presses
        /// Enter on this row.</summary>
        public readonly string Class;

        /// <summary>Display name shown in the UI. Renders as
        /// <c>"???"</c> when <see cref="IsObfuscated"/> is true (FLAG_OBFUSCATED
        /// set on the entry AND requirements not met). Otherwise the
        /// registry-supplied <c>Name</c>.</summary>
        public readonly string DisplayName;

        /// <summary>Long-form description (registry-supplied). Empty
        /// for entries that didn't author one.</summary>
        public readonly string Description;

        /// <summary>True for tree-root entries (skills); false for
        /// power entries within a tree.</summary>
        public readonly bool IsTreeRoot;

        /// <summary>For power rows, the display name of the parent
        /// tree (e.g. "Acrobatics"). Empty for tree-root rows.</summary>
        public readonly string ParentSkillName;

        /// <summary>SP cost. Tracked for UI rendering — even owned
        /// rows show their original cost in some Qud-style screens.</summary>
        public readonly int Cost;

        /// <summary>Computed state per actor's current SP / owned skills /
        /// stat values. UI uses this for color-coding (see <see cref="SkillsScreenRowState"/>
        /// docstring for the 2-axis Qud convention).</summary>
        public readonly SkillsScreenRowState State;

        /// <summary>True iff <c>FLAG_OBFUSCATED</c> is set on the entry
        /// AND the actor's requirements aren't met. Drives the
        /// <c>"???"</c> name rendering. Once requirements are met (or
        /// the skill is acquired), this flips to false and the real
        /// name shows.</summary>
        public readonly bool IsObfuscated;

        public SkillsScreenRow(
            string @class, string displayName, string description,
            bool isTreeRoot, string parentSkillName, int cost,
            SkillsScreenRowState state, bool isObfuscated)
        {
            Class = @class ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Description = description ?? string.Empty;
            IsTreeRoot = isTreeRoot;
            ParentSkillName = parentSkillName ?? string.Empty;
            Cost = cost;
            State = state;
            IsObfuscated = isObfuscated;
        }
    }

    /// <summary>
    /// State of a skill / power row from the actor's perspective.
    /// The renderer's color-coding follows Qud's 2-axis convention
    /// (verified against PowerEntry.Render + IBaseSkillEntry):
    /// <list type="bullet">
    ///   <item><b>Owned</b>: name = white; no cost shown.</item>
    ///   <item><b>Buyable</b>: name = green ({{g|...}}); cost = cyan
    ///         ({{C|N}}sp).</item>
    ///   <item><b>InsufficientSP</b>: name = green ({{g|...}})
    ///         (requirements ARE met, just can't afford); cost = red
    ///         ({{R|N}}sp).</item>
    ///   <item><b>RequirementsNotMet</b>: name = gray ({{K|...}})
    ///         (or "???" if also obfuscated); cost = gray ({{K|N}}sp).</item>
    /// </list>
    /// </summary>
    public enum SkillsScreenRowState
    {
        Owned,
        Buyable,
        InsufficientSP,
        RequirementsNotMet,
    }
}
