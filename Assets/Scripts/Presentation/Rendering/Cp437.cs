namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// Maps the Unicode characters people naturally type into their CP437
    /// byte equivalents, so UI code can write <c>'═'</c> and get a double
    /// rule instead of a question mark.
    ///
    /// <para><b>Why this exists.</b> The glyph atlas holds chars 0–255
    /// only. <c>CP437TilesetGenerator.GetTextTile</c> documents the rule
    /// — <i>"Callers that need a Unicode glyph rendered correctly should
    /// map it to its CP437 equivalent before passing in"</i> — and a
    /// caller that forgets silently gets <c>'?'</c> for every character.
    /// That is exactly what happened to the inventory screen's section
    /// rules and boxes: <c>══ Melee Weapons ══════</c> rendered on screen
    /// as <c>?? Melee Weapons ??????</c>, and nothing in the test suite
    /// could see it because the tests never rasterise.</para>
    ///
    /// <para>Putting the map inside <c>DrawChar</c> rather than at each
    /// call site is deliberate: a rule that every caller must remember is
    /// a rule that will be forgotten again. CP437 genuinely contains all
    /// of these glyphs — the atlas is indexed by byte, so the only thing
    /// missing was the translation.</para>
    ///
    /// <para>The few characters CP437 has no equivalent for (em dash,
    /// ellipsis, curly quotes) degrade to the closest ASCII rather than
    /// to <c>'?'</c>, which at least keeps the line readable.</para>
    /// </summary>
    public static class Cp437
    {
        /// <summary>
        /// Unicode → CP437 byte. Characters already in 0–255 pass
        /// through untouched, so this is safe to call on everything.
        /// </summary>
        public static char Map(char c)
        {
            if (c < 256) return c;

            switch (c)
            {
                // ── Single-line box drawing ──
                case '─': return (char)0xC4;
                case '│': return (char)0xB3;
                case '┌': return (char)0xDA;
                case '┐': return (char)0xBF;
                case '└': return (char)0xC0;
                case '┘': return (char)0xD9;
                case '├': return (char)0xC3;
                case '┤': return (char)0xB4;
                case '┬': return (char)0xC2;
                case '┴': return (char)0xC1;
                case '┼': return (char)0xC5;

                // ── Double-line box drawing ──
                case '═': return (char)0xCD;
                case '║': return (char)0xBA;
                case '╔': return (char)0xC9;
                case '╗': return (char)0xBB;
                case '╚': return (char)0xC8;
                case '╝': return (char)0xBC;
                case '╠': return (char)0xCC;
                case '╣': return (char)0xB9;
                case '╦': return (char)0xCB;
                case '╩': return (char)0xCA;
                case '╬': return (char)0xCE;

                // ── Blocks and shading ──
                case '░': return (char)0xB0;
                case '▒': return (char)0xB1;
                case '▓': return (char)0xB2;
                case '█': return (char)0xDB;
                case '▄': return (char)0xDC;
                case '▀': return (char)0xDF;
                case '■': return (char)0xFE;

                // ── Punctuation and arrows ──
                case '•': return (char)0x07;
                case '·': return (char)0xFA;
                case '↑': return (char)0x18;
                case '↓': return (char)0x19;
                case '→': return (char)0x1A;
                case '←': return (char)0x1B;
                case '↔': return (char)0x1D;
                case '▲': return (char)0x1E;
                case '▼': return (char)0x1F;
                case '◄': return (char)0x11;
                case '►': return (char)0x10;

                // ── No CP437 equivalent: fall back to readable ASCII
                //    rather than to '?' ──
                case '—':
                case '–': return '-';
                case '…': return '.';
                case '“':
                case '”': return '"';
                case '‘':
                case '’': return '\'';

                default: return c;   // GetTextTile's '?' fallback handles it
            }
        }
    }
}
