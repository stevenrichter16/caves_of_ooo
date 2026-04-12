using System;
using System.Collections.Generic;
using System.Text;
using CavesOfOoo.Core;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Pure text layout helpers shared by sidebar rendering and tests.
    /// </summary>
    public static class SidebarTextFormatter
    {
        public struct LogLine
        {
            public LogLine(string text, int ageIndex, int entryNewestSerial, int rowIndexWithinEntry, int entryRowCount)
            {
                Text = text ?? string.Empty;
                AgeIndex = ageIndex < 0 ? 0 : ageIndex;
                EntryNewestSerial = entryNewestSerial;
                RowIndexWithinEntry = rowIndexWithinEntry < 0 ? 0 : rowIndexWithinEntry;
                EntryRowCount = entryRowCount < 1 ? 1 : entryRowCount;
            }

            public string Text { get; }
            public int AgeIndex { get; }
            public int EntryNewestSerial { get; }
            public int RowIndexWithinEntry { get; }
            public int EntryRowCount { get; }

            public bool MatchesIdentity(LogLine other)
            {
                if (EntryNewestSerial >= 0 || other.EntryNewestSerial >= 0)
                {
                    return EntryNewestSerial == other.EntryNewestSerial &&
                           RowIndexWithinEntry == other.RowIndexWithinEntry &&
                           EntryRowCount == other.EntryRowCount;
                }

                return RowIndexWithinEntry == other.RowIndexWithinEntry &&
                       EntryRowCount == other.EntryRowCount &&
                       string.Equals(Text, other.Text, StringComparison.Ordinal);
            }
        }

        public static List<string> FormatVitals(SidebarSnapshot snapshot, int width, int maxLines = 7)
        {
            int safeWidth = Math.Max(1, width);
            int safeMaxLines = Math.Max(1, maxLines);
            var lines = new List<string>(safeMaxLines);

            if (snapshot?.VitalLines != null)
            {
                for (int i = 0; i < snapshot.VitalLines.Count && lines.Count < safeMaxLines; i++)
                    lines.Add(Truncate(snapshot.VitalLines[i], safeWidth));
            }

            if (lines.Count < safeMaxLines)
            {
                List<string> statusLines = WrapWithPrefixes(
                    "ST " + (snapshot?.StatusText ?? "-"),
                    safeWidth,
                    string.Empty,
                    "   ");

                for (int i = 0; i < statusLines.Count && lines.Count < safeMaxLines; i++)
                    lines.Add(statusLines[i]);
            }

            return lines;
        }

        public static List<string> FormatFocus(LookSnapshot snapshot, int width, int maxLines)
        {
            int safeWidth = Math.Max(1, width);
            int safeMaxLines = Math.Max(1, maxLines);
            var lines = new List<string>(safeMaxLines);

            if (snapshot == null)
            {
                lines.Add("No focus");
                return lines;
            }

            AppendWrapped(lines, snapshot.Header, safeWidth, safeMaxLines);
            AppendWrapped(lines, snapshot.Summary, safeWidth, safeMaxLines);

            if (snapshot.DetailLines != null)
            {
                for (int i = 0; i < snapshot.DetailLines.Count && lines.Count < safeMaxLines; i++)
                    AppendWrapped(lines, snapshot.DetailLines[i], safeWidth, safeMaxLines);
            }

            if (lines.Count == 0)
                lines.Add("No focus");

            return lines;
        }

        public static List<LogLine> FormatLog(
            IReadOnlyList<SidebarLogEntry> entriesNewestFirst,
            int width,
            int maxLines = int.MaxValue)
        {
            int safeWidth = Math.Max(1, width);
            int safeMaxLines = maxLines == int.MaxValue ? int.MaxValue : Math.Max(1, maxLines);
            var lines = new List<LogLine>();

            if (entriesNewestFirst == null || entriesNewestFirst.Count == 0)
            {
                lines.Add(new LogLine("No recent messages.", 0, -1, 0, 1));
                return lines;
            }

            for (int entryIndex = entriesNewestFirst.Count - 1; entryIndex >= 0; entryIndex--)
            {
                SidebarLogEntry entry = entriesNewestFirst[entryIndex];
                string body = entry.Count > 1
                    ? entry.Text + " (x" + entry.Count + ")"
                    : entry.Text;

                int ageIndex = entryIndex;
                List<string> wrapped = WrapWithPrefixes(body, safeWidth, ":: ", "   ");
                for (int i = 0; i < wrapped.Count; i++)
                {
                    lines.Add(new LogLine(
                        wrapped[i],
                        ageIndex,
                        entry.NewestSerial,
                        i,
                        wrapped.Count));
                }
            }

            if (safeMaxLines == int.MaxValue || lines.Count <= safeMaxLines)
                return lines;

            return lines.GetRange(lines.Count - safeMaxLines, safeMaxLines);
        }

        public static List<string> WrapWithPrefixes(
            string text,
            int width,
            string firstPrefix,
            string continuationPrefix)
        {
            int safeWidth = Math.Max(1, width);
            string first = firstPrefix ?? string.Empty;
            string cont = continuationPrefix ?? string.Empty;
            var lines = new List<string>();

            string normalized = string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim();
            if (normalized.Length == 0)
            {
                lines.Add(Truncate(first, safeWidth));
                return lines;
            }

            int index = 0;
            bool firstLine = true;
            while (index < normalized.Length)
            {
                string prefix = firstLine ? first : cont;
                int available = safeWidth - prefix.Length;
                if (available <= 0)
                {
                    lines.Add(Truncate(prefix, safeWidth));
                    break;
                }

                int remaining = normalized.Length - index;
                if (remaining <= available)
                {
                    lines.Add(prefix + normalized.Substring(index, remaining));
                    break;
                }

                int breakAt = normalized.LastIndexOf(' ', index + available - 1, available);
                if (breakAt < index)
                    breakAt = index + available;

                string slice = normalized.Substring(index, breakAt - index).TrimEnd();
                lines.Add(prefix + slice);

                index = breakAt;
                while (index < normalized.Length && normalized[index] == ' ')
                    index++;

                firstLine = false;
            }

            return lines;
        }

        private static void AppendWrapped(List<string> lines, string text, int width, int maxLines)
        {
            if (lines.Count >= maxLines || string.IsNullOrWhiteSpace(text))
                return;

            List<string> wrapped = WrapPlain(text, width);
            for (int i = 0; i < wrapped.Count && lines.Count < maxLines; i++)
                lines.Add(wrapped[i]);
        }

        private static List<string> WrapPlain(string text, int width)
        {
            int safeWidth = Math.Max(1, width);
            var lines = new List<string>();
            if (string.IsNullOrWhiteSpace(text))
                return lines;

            string normalized = text.Trim();
            int index = 0;
            while (index < normalized.Length)
            {
                int remaining = normalized.Length - index;
                if (remaining <= safeWidth)
                {
                    lines.Add(normalized.Substring(index, remaining));
                    break;
                }

                int breakAt = normalized.LastIndexOf(' ', index + safeWidth - 1, safeWidth);
                if (breakAt < index)
                    breakAt = index + safeWidth;

                string slice = normalized.Substring(index, breakAt - index).TrimEnd();
                lines.Add(slice);

                index = breakAt;
                while (index < normalized.Length && normalized[index] == ' ')
                    index++;
            }

            return lines;
        }

        private static string Truncate(string text, int width)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            if (text.Length <= width)
                return text;

            if (width <= 1)
                return text.Substring(0, width);

            return text.Substring(0, width - 1) + ">";
        }
    }
}
