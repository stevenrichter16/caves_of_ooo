using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>
    /// The shared visual language of the inventory screen — section
    /// rules, titled boxes, key-hint footers, marked list rows.
    ///
    /// <para>Introduced with the Crafting panel and deliberately written
    /// as panel-agnostic helpers rather than inline drawing, because the
    /// next step is making Equipment, Inventory, Tinkering and Abilities
    /// look the same way (Docs/CRAFTING-FROM-THE-PACK.md §C8). Every rule
    /// about how the screen looks should live here, once.</para>
    ///
    /// <para><b>ASCII only, deliberately.</b> The UI font atlas is ~95
    /// hand-authored hex bitmaps covering ASCII; CP437 box-drawing bytes
    /// (0xCD, 0xC4, 0xB3, 0xDA…) have <i>no bitmap at all</i>, so a rule
    /// drawn with U+2550 renders as literally nothing on screen. This
    /// shipped once and was invisible to the whole test suite — no test
    /// rasterises. Everything here uses <c>=</c>, <c>-</c>, <c>|</c> and
    /// <c>+</c>, the same characters the equipment doll already draws
    /// its slots with.</para>
    ///
    /// <para><b>The colour language</b>, applied everywhere:</para>
    /// <list type="bullet">
    /// <item><c>DarkGray</c> — chrome: rules, dividers, box edges, unpicked marks.</item>
    /// <item><c>Gray</c> — labels and inactive text.</item>
    /// <item><c>White</c> — values, and the row under the cursor.</item>
    /// <item><c>BrightYellow</c> — panel and box titles.</item>
    /// <item><c>BrightGreen</c> / <c>BrightRed</c> — good / bad magnitudes.</item>
    /// </list>
    /// </summary>
    public partial class InventoryUI
    {
        /// <summary>
        /// A section rule: <c>== Title =================</c>. The rule
        /// runs to <paramref name="width"/> so stacked sections line up
        /// down the column.
        /// </summary>
        private void DrawSectionRule(int x, int y, string title, int width)
        {
            DrawChar(x, y, '=', QudColorParser.DarkGray);
            DrawChar(x + 1, y, '=', QudColorParser.DarkGray);

            int titleX = x + 3;
            DrawText(titleX, y, title, QudColorParser.Gray);

            int fillStart = titleX + title.Length + 1;
            int fillEnd = x + width;
            for (int i = fillStart; i < fillEnd; i++)
                DrawChar(i, y, '=', QudColorParser.DarkGray);
        }

        /// <summary>
        /// A box whose title sits inside its top edge:
        /// <c>+- TITLE --------+</c>. Draws edges only — the caller fills
        /// the interior, which starts at (x+2, y+1).
        /// </summary>
        private void DrawTitledBox(int x, int y, int width, int height, string title)
        {
            Color chrome = QudColorParser.DarkGray;

            DrawChar(x, y, '+', chrome);
            DrawChar(x + 1, y, '-', chrome);
            DrawText(x + 3, y, title, QudColorParser.BrightYellow);
            int afterTitle = x + 3 + title.Length + 1;
            for (int i = x + 2; i < x + width - 1; i++)
                if (i < x + 3 || i >= afterTitle)
                    DrawChar(i, y, '-', chrome);
            DrawChar(x + width - 1, y, '+', chrome);

            for (int row = y + 1; row < y + height - 1; row++)
            {
                DrawChar(x, row, '|', chrome);
                DrawChar(x + width - 1, row, '|', chrome);
            }

            int bottom = y + height - 1;
            DrawChar(x, bottom, '+', chrome);
            for (int i = x + 1; i < x + width - 1; i++)
                DrawChar(i, bottom, '-', chrome);
            DrawChar(x + width - 1, bottom, '+', chrome);
        }

        /// <summary>
        /// Word-wraps <paramref name="text"/> into at most
        /// <paramref name="maxLines"/> lines of <paramref name="width"/>
        /// columns, breaking on spaces.
        ///
        /// <para>The RESULT box explains itself in sentences — "Pick a
        /// blade, a haft and a binding to finish the weapon", "The
        /// volatile mix has no stable form". Hard truncation turned
        /// those into "Pick a blade, a haft and~", which reads as a bug
        /// rather than an explanation.</para>
        ///
        /// <para>Returns the row after the last one written.</para>
        /// </summary>
        private int DrawWrapped(int x, int y, int width, int maxLines, string text, Color color)
        {
            if (string.IsNullOrEmpty(text) || width <= 0) return y;

            string[] words = text.Split(' ');
            var line = new System.Text.StringBuilder();
            int row = y, used = 0;

            for (int i = 0; i < words.Length && used < maxLines; i++)
            {
                string w = words[i];
                if (line.Length > 0 && line.Length + 1 + w.Length > width)
                {
                    DrawText(x, row++, line.ToString(), color);
                    used++;
                    line.Clear();
                    if (used >= maxLines) break;
                }

                if (line.Length > 0) line.Append(' ');
                line.Append(w.Length > width ? w.Substring(0, width) : w);
            }

            if (line.Length > 0 && used < maxLines)
            {
                DrawText(x, row++, line.ToString(), color);
            }

            return row;
        }

        /// <summary>
        /// A label/value pair inside a box or panel — label in Gray at
        /// <paramref name="x"/>, value in <paramref name="valueColor"/>
        /// at <paramref name="valueX"/>.
        /// </summary>
        private void DrawLabelled(int x, int y, string label, int valueX,
            string value, Color valueColor)
        {
            DrawText(x, y, label, QudColorParser.Gray);
            DrawText(valueX, y, value, valueColor);
        }

        /// <summary>
        /// One pickable list row: <c>&gt; [x] name            x3</c>.
        /// The cursor, the mark and the stack count each carry meaning,
        /// so each gets its own colour rather than one flat line colour.
        /// </summary>
        private void DrawPickRow(int x, int y, int width, string name,
            bool selected, bool marked, int count)
        {
            if (selected)
                DrawChar(x, y, '>', QudColorParser.White);

            DrawChar(x + 2, y, '[', QudColorParser.DarkGray);
            DrawChar(x + 3, y, marked ? 'x' : ' ',
                marked ? QudColorParser.BrightGreen : QudColorParser.DarkGray);
            DrawChar(x + 4, y, ']', QudColorParser.DarkGray);

            string countText = count > 1 ? "x" + count : string.Empty;
            int nameX = x + 6;
            int maxName = width - 6 - (countText.Length > 0 ? countText.Length + 2 : 0);
            if (maxName > 0 && name.Length > maxName)
                name = name.Substring(0, maxName - 1) + "~";

            DrawText(nameX, y, name,
                selected ? QudColorParser.White
                         : marked ? QudColorParser.Gray : QudColorParser.Gray);

            if (countText.Length > 0)
                DrawText(x + width - countText.Length, y, countText, QudColorParser.DarkGray);
        }

        /// <summary>
        /// Draws an action-bar string like
        /// <c>[Enter]equip [e]unequip [Esc]close</c> two-tone: the
        /// bracketed KEY brighter than the verb after it, so the row
        /// scans as a legend rather than a sentence.
        ///
        /// <para>There is exactly one action bar on this screen, at row
        /// <c>H-2</c>. A second hint row was briefly drawn at
        /// <c>CONTENT_END+1</c> — the same cell — and the two printed
        /// over each other. Everything routes here now.</para>
        /// </summary>
        private void DrawKeyLegend(int x, int y, string actions)
        {
            if (string.IsNullOrEmpty(actions)) return;

            bool inKey = false;
            for (int i = 0; i < actions.Length && x + i < W; i++)
            {
                char c = actions[i];
                if (c == '[') inKey = true;
                else if (c == ']') { inKey = false; DrawChar(x + i, y, c, QudColorParser.DarkGray); continue; }

                DrawChar(x + i, y, c,
                    c == '[' ? QudColorParser.DarkGray
                    : inKey ? QudColorParser.White
                    : QudColorParser.Gray);
            }
        }

    }
}
