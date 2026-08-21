namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Wx optimization review §2 (Docs/FELLING-WX-OPTIMIZATION-REVIEW.md)
    /// — the stationary water shimmer's skip logic, extracted pure so the
    /// EditMode suite can pin it.
    ///
    /// <para>The stationary branch of
    /// <c>ZoneRenderer.UpdateAmbientAnimations</c> ran
    /// <c>SetTileFlags</c>+<c>SetColor</c> for EVERY stationary water
    /// cell EVERY frame. That was invisible at village-puddle scale, but
    /// W3's Sodden formations put 250-450 MirePool/PeatBog cells in one
    /// zone: ~500-900 native tilemap calls per frame, ~97% of which
    /// rewrote an unchanged color — the phase band only advances
    /// 2×/sec/cell. Skipping unchanged writes is visually lossless and
    /// cuts the native-call count ~30×.</para>
    ///
    /// <para>The cache is a byte per cached water cell, indexed in step
    /// with <c>_waterTilePositions</c>, reset to <see cref="Unpainted"/>
    /// whenever <c>RefreshWaterCache</c> rebuilds (which runs inside the
    /// full-render path — so every full-zone stomp forces one repaint of
    /// every cell, keeping the tilemap honest).</para>
    /// </summary>
    public static class WaterShimmer
    {
        /// <summary>Sentinel: this cell has not been painted since the
        /// last cache reset — the first frame always paints.</summary>
        public const byte Unpainted = 255;

        /// <summary>The stationary shimmer's palette index — the exact
        /// formula the branch always used (time-advancing spatial phase,
        /// band advancing 2×/sec), kept pure for pinning.</summary>
        public static int ColorIndex(float ambientTimer, int x, int y, int paletteLength)
        {
            float phase = ambientTimer * 2f + x * 0.7f + y * 1.3f;
            int colorIndex = ((int)phase) % paletteLength;
            if (colorIndex < 0) colorIndex += paletteLength;
            return colorIndex;
        }

        /// <summary>True when cell <paramref name="i"/> must repaint —
        /// no cache, out-of-range index, first paint since reset, or a
        /// changed color band. Stamps the cache when claiming, so a
        /// caller paints exactly when this returns true.</summary>
        public static bool ClaimPaint(byte[] lastPainted, int i, int colorIndex)
        {
            if (lastPainted == null || i < 0 || i >= lastPainted.Length)
                return true; // no cache to consult — always paint
            if (lastPainted[i] == (byte)colorIndex)
                return false;
            lastPainted[i] = (byte)colorIndex;
            return true;
        }

        /// <summary>Forget cell <paramref name="i"/>'s last paint — used
        /// when another painter (fog clear, a creature's glyph) took the
        /// tile, so the shimmer repaints as soon as the water is visible
        /// again instead of skipping on a stale match.</summary>
        public static void Invalidate(byte[] lastPainted, int i)
        {
            if (lastPainted == null || i < 0 || i >= lastPainted.Length) return;
            lastPainted[i] = Unpainted;
        }
    }
}
