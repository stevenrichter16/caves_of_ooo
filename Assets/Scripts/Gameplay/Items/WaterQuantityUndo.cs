using System;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    internal static class WaterQuantityUndo
    {
        /// <summary>Invert only this action's signed quantity change. Invalid
        /// external scalar mutations are diagnosed and bounded, never wrapped.
        /// Consuming uncommitted credit outside the claim API is not reversible.</summary>
        internal static int Restore(int current, int actionDelta, int capacity = int.MaxValue, bool endlessAllowed = false)
        {
            if (endlessAllowed && current == -1) return current;
            long restored = (long)current - actionDelta;
            if (current < 0 || capacity < 0 || restored < 0 || restored > capacity)
                Diag.Record("event", "WaterUndoBoundary", payload: new { current, actionDelta, capacity });
            return (int)Math.Max(0, Math.Min(Math.Max(0, capacity), restored));
        }
    }
}
