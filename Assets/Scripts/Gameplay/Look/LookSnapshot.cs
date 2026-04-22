using System.Collections.Generic;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Immutable inspect result for a looked-at world cell.
    /// </summary>
    public sealed class LookSnapshot
    {
        public int X { get; }
        public int Y { get; }
        public string Header { get; }
        public string Summary { get; }
        public IReadOnlyList<string> DetailLines { get; }
        public Entity PrimaryEntity { get; }
        public Cell Cell { get; }

        /// <summary>
        /// Phase 10 — goal-stack summary lines for the primary entity, ordered
        /// TOP-DOWN (index 0 = currently executing goal, last index = root
        /// BoredGoal). Null when the inspector toggle is off, or the primary
        /// is not a Creature, or the Creature has no BrainPart. Consecutive
        /// duplicate descriptions are run-length collapsed with <c>xN</c>.
        /// </summary>
        public IReadOnlyList<string> GoalStackLines { get; }

        /// <summary>
        /// Phase 10 — current <see cref="BrainPart.LastThought"/> of the primary
        /// entity. Null under the same conditions as <see cref="GoalStackLines"/>.
        /// A creature that has a BrainPart but has never called Think yields
        /// "none" (never null when the other inspector fields are populated).
        /// </summary>
        public string LastThought { get; }

        /// <summary>Baseline ctor — Commit 4 overload with inspector fields defaults to null/null.</summary>
        public LookSnapshot(
            int x,
            int y,
            string header,
            string summary,
            IReadOnlyList<string> detailLines,
            Entity primaryEntity,
            Cell cell)
            : this(x, y, header, summary, detailLines, primaryEntity, cell,
                   goalStackLines: null, lastThought: null)
        {
        }

        /// <summary>
        /// Full ctor including Phase 10 inspector fields. Existing call sites
        /// go through the baseline overload above so backward compatibility is
        /// preserved — <c>LookQueryService</c> is the only caller that
        /// populates the new fields.
        /// </summary>
        public LookSnapshot(
            int x,
            int y,
            string header,
            string summary,
            IReadOnlyList<string> detailLines,
            Entity primaryEntity,
            Cell cell,
            IReadOnlyList<string> goalStackLines,
            string lastThought)
        {
            X = x;
            Y = y;
            Header = header ?? string.Empty;
            Summary = summary ?? string.Empty;
            DetailLines = detailLines ?? new List<string>();
            PrimaryEntity = primaryEntity;
            Cell = cell;
            GoalStackLines = goalStackLines; // nullable — null means "inspector off/not applicable"
            LastThought = lastThought;       // nullable — null same meaning
        }
    }
}
