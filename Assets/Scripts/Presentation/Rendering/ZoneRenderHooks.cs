using System;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Decoupled hook surface that gameplay code (MovementSystem,
    /// CombatSystem, etc.) calls when a cell's contents may have visually
    /// changed. The renderer subscribes; gameplay never references the
    /// renderer directly. Mirrors the existing
    /// <see cref="SettlementRuntime.ZoneDirtyCallback"/> pattern.
    ///
    /// <para><b>Why static?</b> Gameplay systems are static utility classes
    /// (no `this` reference to inject a hook into). A static delegate sink
    /// is the simplest decoupling — the bootstrap wires the delegate; the
    /// renderer doesn't need to subscribe to per-entity events.</para>
    ///
    /// <para><b>Cell-level vs. full-zone</b> — call <see cref="MarkCellDirty"/>
    /// when only one cell's render output may have changed (a single
    /// non-player entity moved, took damage, or died). Call
    /// <see cref="MarkFullDirty"/> when the whole zone may need redrawing
    /// (player moved → FOV recompute, settlement state shifted, UI overlay
    /// stomped cells).</para>
    /// </summary>
    public static class ZoneRenderHooks
    {
        /// <summary>
        /// Wired by <c>ZoneRenderer.Awake</c> to forward into the renderer's
        /// per-cell dirty set. Null between scenes / before bootstrap.
        /// </summary>
        public static Action<int, int, string> CellDirtyCallback { get; set; }

        /// <summary>
        /// Wired by <c>ZoneRenderer.Awake</c> to forward into the renderer's
        /// full-zone dirty flag. Null between scenes / before bootstrap.
        /// </summary>
        public static Action<string> FullDirtyCallback { get; set; }

        /// <summary>
        /// Mark a single cell as needing re-render. No-op if no renderer is
        /// listening (test harness, scene without renderer, etc.).
        /// </summary>
        public static void MarkCellDirty(int x, int y, string source)
        {
            CellDirtyCallback?.Invoke(x, y, source);
        }

        /// <summary>
        /// Mark the whole zone as needing re-render. Used for changes that
        /// can't be reduced to a small cell set (FOV recompute, settlement
        /// rebuild, UI overlay close).
        /// </summary>
        public static void MarkFullDirty(string source)
        {
            FullDirtyCallback?.Invoke(source);
        }

        /// <summary>
        /// Convenience overload: mark a cell dirty by reference. Null-safe.
        /// </summary>
        public static void MarkCellDirty(Cell cell, string source)
        {
            if (cell == null) return;
            CellDirtyCallback?.Invoke(cell.X, cell.Y, source);
        }

        /// <summary>
        /// Reset on scene teardown / test harness teardown so a callback
        /// from a destroyed renderer doesn't fire.
        /// </summary>
        public static void Reset()
        {
            CellDirtyCallback = null;
            FullDirtyCallback = null;
        }
    }
}
