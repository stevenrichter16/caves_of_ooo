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

        public LookSnapshot(
            int x,
            int y,
            string header,
            string summary,
            IReadOnlyList<string> detailLines,
            Entity primaryEntity,
            Cell cell)
        {
            X = x;
            Y = y;
            Header = header ?? string.Empty;
            Summary = summary ?? string.Empty;
            DetailLines = detailLines ?? new List<string>();
            PrimaryEntity = primaryEntity;
            Cell = cell;
        }
    }
}
