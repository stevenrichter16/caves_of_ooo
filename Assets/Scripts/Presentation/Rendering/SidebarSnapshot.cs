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
        public SidebarSnapshot(
            IReadOnlyList<string> vitalLines,
            string statusText,
            LookSnapshot focusSnapshot,
            IReadOnlyList<SidebarLogEntry> logEntriesNewestFirst)
        {
            VitalLines = vitalLines ?? new List<string>();
            StatusText = statusText ?? "-";
            FocusSnapshot = focusSnapshot;
            LogEntriesNewestFirst = logEntriesNewestFirst ?? new List<SidebarLogEntry>();
        }

        public IReadOnlyList<string> VitalLines { get; }
        public string StatusText { get; }
        public LookSnapshot FocusSnapshot { get; }
        public IReadOnlyList<SidebarLogEntry> LogEntriesNewestFirst { get; }
    }

    public struct SidebarLogEntry
    {
        public SidebarLogEntry(string text, int tick, int count)
        {
            Text = text ?? string.Empty;
            Tick = tick;
            Count = count < 1 ? 1 : count;
        }

        public string Text { get; }
        public int Tick { get; }
        public int Count { get; }
    }
}
