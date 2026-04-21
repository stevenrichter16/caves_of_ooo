using System.Collections.Generic;
using CavesOfOoo.Core;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Read-only snapshot of the Qud-style gameplay sidebar.
    /// Pure presentation data: no Unity dependencies.
    /// </summary>
    public sealed class SidebarSnapshot
    {
        /// <summary>Baseline ctor: no thought entries (LOG section shown).</summary>
        public SidebarSnapshot(
            IReadOnlyList<string> vitalLines,
            string statusText,
            LookSnapshot focusSnapshot,
            IReadOnlyList<SidebarLogEntry> logEntriesNewestFirst)
            : this(vitalLines, statusText, focusSnapshot, logEntriesNewestFirst,
                   thoughtEntries: null)
        {
        }

        /// <summary>
        /// Full ctor with Phase 10 thought data. When
        /// <paramref name="thoughtEntries"/> is non-null, the sidebar renderer
        /// replaces the LOG section with a THOUGHTS section listing every
        /// creature's current <see cref="BrainPart.LastThought"/>. Existing
        /// call sites that don't care about thoughts go through the baseline
        /// overload above and leave this null (LOG renders normally).
        /// </summary>
        public SidebarSnapshot(
            IReadOnlyList<string> vitalLines,
            string statusText,
            LookSnapshot focusSnapshot,
            IReadOnlyList<SidebarLogEntry> logEntriesNewestFirst,
            IReadOnlyList<SidebarThoughtEntry> thoughtEntries)
        {
            VitalLines = vitalLines ?? new List<string>();
            StatusText = statusText ?? "-";
            FocusSnapshot = focusSnapshot;
            LogEntriesNewestFirst = logEntriesNewestFirst ?? new List<SidebarLogEntry>();
            ThoughtEntries = thoughtEntries; // nullable — null means "log mode"
        }

        public IReadOnlyList<string> VitalLines { get; }
        public string StatusText { get; }
        public LookSnapshot FocusSnapshot { get; }
        public IReadOnlyList<SidebarLogEntry> LogEntriesNewestFirst { get; }

        /// <summary>
        /// Phase 10 — per-creature current-thought entries for the bottom
        /// sidebar panel. Non-null means the user has the 't' overlay on;
        /// renderer swaps the LOG section for a THOUGHTS section using the
        /// same tilemap container. Null means LOG renders normally.
        /// </summary>
        public IReadOnlyList<SidebarThoughtEntry> ThoughtEntries { get; }
    }

    /// <summary>
    /// One entry in the sidebar's THOUGHTS panel — a creature's display name
    /// and its <see cref="BrainPart.LastThought"/>. Empty <c>Thought</c>
    /// renders as "..." so creatures that haven't ticked yet stay visible
    /// in the list instead of vanishing.
    /// </summary>
    public readonly struct SidebarThoughtEntry
    {
        public SidebarThoughtEntry(string name, string thought)
        {
            Name = name ?? string.Empty;
            Thought = thought ?? string.Empty;
        }

        public string Name { get; }
        public string Thought { get; }
    }

    public struct SidebarLogEntry
    {
        public SidebarLogEntry(string text, int tick, int count)
            : this(text, tick, count, -1)
        {
        }

        public SidebarLogEntry(string text, int tick, int count, int newestSerial)
        {
            Text = text ?? string.Empty;
            Tick = tick;
            Count = count < 1 ? 1 : count;
            NewestSerial = newestSerial;
        }

        public string Text { get; }
        public int Tick { get; }
        public int Count { get; }
        public int NewestSerial { get; }
    }
}
