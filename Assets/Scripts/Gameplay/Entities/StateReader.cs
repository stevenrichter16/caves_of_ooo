using System;
using System.Collections.Generic;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Which readers a describer speaks in. Quick is the one-line world/pack Examine text; Full is the structured document.</summary>
    [Flags] public enum ReaderScope { Quick = 1, Full = 2, Both = 3 }

    /// <summary>Everything a describer may read. Describing is read-only: no events, no state change, no diag.</summary>
    public sealed class DescribeContext
    {
        public Entity Owner; public Zone Zone; public Cell Cell; public Entity Viewer; public bool Full;
    }

    /// <summary>Implemented by a Part that contributes a "current state" line to an entity's readers (a job in progress, a fault, a contained liquid, a station's terms).
    /// A new Part implements this and appears in both readers without editing either builder (Docs/MACHINE-READERS.md, spec C6).</summary>
    public interface IDescribeState
    {
        ReaderScope Scope { get; }
        /// <summary>Position among the entity's lines; ties keep Part insertion order. The shipped providers use 10, 20, 30... in their legacy order.</summary>
        int Order { get; }
        string DescribeState(DescribeContext context);
    }

    /// <summary>One collector for the quick and full readers. Isolated: a throwing describer is skipped and recorded, never allowed to hide the others.</summary>
    public static class StateReader
    {
        public static void Collect(Entity owner, bool full, Zone zone, Cell cell, Entity viewer, List<string> into)
        {
            if (owner == null || into == null) return;
            var parts = owner.Parts; int found = 0;
            for (int i = 0; i < parts.Count; i++) if (parts[i] is IDescribeState) found++;
            if (found == 0) return;
            var wanted = full ? ReaderScope.Full : ReaderScope.Quick;
            var ordered = new List<(int order, int index, IDescribeState part)>(found);
            for (int i = 0; i < parts.Count; i++)
                if (parts[i] is IDescribeState d && (d.Scope & wanted) != 0) ordered.Add((d.Order, i, d));
            ordered.Sort((a, b) => a.order != b.order ? a.order.CompareTo(b.order) : a.index.CompareTo(b.index));
            var context = new DescribeContext { Owner = owner, Zone = zone, Cell = cell, Viewer = viewer, Full = full };
            for (int i = 0; i < ordered.Count; i++)
            {
                string line;
                try { line = ordered[i].part.DescribeState(context); }
                catch (Exception error)
                {
                    if (Diag.IsChannelEnabled("event")) Diag.Record("event", "DescribeStateFailed", target: owner, payload: new { part = ordered[i].part.GetType().Name, error = error.GetType().Name, message = error.Message });
                    continue;
                }
                if (string.IsNullOrWhiteSpace(line) || into.Contains(line)) continue;
                into.Add(line);
            }
        }
    }
}
