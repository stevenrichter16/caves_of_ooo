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
        /// A section rule: <c>══ Title ═══════════════</c>. The em-rule
        /// runs to <paramref name="width"/> so stacked sections line up
        /// down the column.
        /// </summary>
        private void DrawSectionRule(int x, int y, string title, int width)
        {
            DrawText(x, y, "══ ", QudColorParser.DarkGray);
            int titleX = x + 3;
            DrawText(titleX, y, title, QudColorParser.Gray);

            int fillStart = titleX + title.Length + 1;
            int fillEnd = x + width;
            if (fillEnd <= fillStart) return;

            for (int i = fillStart; i < fillEnd; i++)
                DrawChar(i, y, '═', QudColorParser.DarkGray);
        }

        /// <summary>
        /// A box whose title sits inside its top edge:
        /// <c>┌ TITLE ─────────┐</c>. Draws edges only — the caller fills
        /// the interior, which starts at (x+2, y+1).
        /// </summary>
        private void DrawTitledBox(int x, int y, int width, int height, string title)
        {
            Color chrome = QudColorParser.DarkGray;

            DrawChar(x, y, '┌', chrome);
            DrawText(x + 2, y, title, QudColorParser.BrightYellow);
            int afterTitle = x + 2 + title.Length + 1;
            for (int i = x + 1; i < x + width - 1; i++)
                if (i < x + 2 || i >= afterTitle)
                    DrawChar(i, y, '─', chrome);
            DrawChar(x + width - 1, y, '┐', chrome);

            for (int row = y + 1; row < y + height - 1; row++)
            {
                DrawChar(x, row, '│', chrome);
                DrawChar(x + width - 1, row, '│', chrome);
            }

            int bottom = y + height - 1;
            DrawChar(x, bottom, '└', chrome);
            for (int i = x + 1; i < x + width - 1; i++)
                DrawChar(i, bottom, '─', chrome);
            DrawChar(x + width - 1, bottom, '┘', chrome);
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
        /// The footer key hints. Keys render brighter than their verbs so
        /// the line scans as a legend rather than a sentence.
        /// </summary>
        private void DrawKeyHints(int y, params string[] pairs)
        {
            int x = 1;
            for (int i = 0; i + 1 < pairs.Length; i += 2)
            {
                DrawText(x, y, pairs[i], QudColorParser.Gray);
                x += pairs[i].Length + 1;
                DrawText(x, y, pairs[i + 1], QudColorParser.DarkGray);
                x += pairs[i + 1].Length + 3;
            }
        }
    }
}
