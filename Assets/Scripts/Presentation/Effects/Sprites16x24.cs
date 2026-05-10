namespace CavesOfOoo.Presentation.Effects
{
    /// <summary>
    /// Master gate for the 16×24 creature sprite layer (Phase A onward).
    ///
    /// Independent of <see cref="GraphicsPolish"/> — the 16×24 layer can
    /// be on while every Pass 1-11 polish layer is off, or vice versa.
    /// This lets us A/B between pure CP437, Pass 7-11 environment polish,
    /// and the Qud-style 16×24 creature overlay without coupling them.
    ///
    /// When <c>true</c> (this branch's default), creatures with a matching
    /// blueprint→family entry render as 16×24 sprites that overflow the
    /// row above (the Qud aesthetic). Creatures without a mapping fall
    /// back to their CP437 glyph, untouched.
    ///
    /// Toggle: edit this constant + recompile. No PlayerPrefs.
    /// </summary>
    public static class Sprites16x24
    {
        public const bool IsEnabled = true;
    }
}
