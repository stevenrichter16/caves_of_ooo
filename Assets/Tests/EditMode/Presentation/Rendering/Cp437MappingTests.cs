using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Rendering;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Pins the Unicode→CP437 translation, and — more importantly —
    /// catches the whole bug class without rasterising anything.
    ///
    /// <para><b>The bug this exists for.</b> The glyph atlas is indexed
    /// by byte and holds chars 0–255. A UI drawing <c>'═'</c> (U+2550)
    /// got the <c>'?'</c> fallback for every character, so the inventory
    /// screen's section rules shipped reading
    /// <c>?? Melee Weapons ??????????</c>. The full EditMode suite was
    /// green throughout: no test rasterises, so nothing could see it.
    /// A screenshot found it immediately.</para>
    ///
    /// <para>The source sweep below is the part that generalises. It
    /// reads the UI source, extracts every non-ASCII character in a
    /// string or char literal, and asserts the mapper turns it into
    /// something the atlas can actually render. That fails on the next
    /// person to paste a nice-looking Unicode glyph into a panel,
    /// which is the only durable defence available without a render
    /// harness.</para>
    /// </summary>
    public class Cp437MappingTests
    {
        [Test]
        public void EveryMappedCharacter_LandsInsideTheAtlas()
        {
            // The atlas is byte-indexed. Anything the mapper emits above
            // 255 would still render as '?'.
            foreach (char c in "─│┌┐└┘├┤┬┴┼═║╔╗╚╝╠╣╦╩╬░▒▓█▄▀■•·↑↓→←↔▲▼◄►—–…“”‘’")
            {
                char mapped = Cp437.Map(c);
                Assert.Less((int)mapped, 256,
                    "U+" + ((int)c).ToString("X4") + " maps to U+"
                    + ((int)mapped).ToString("X4") + ", still outside the atlas");
            }
        }

        [Test]
        public void PlainAsciiPassesThroughUntouched()
        {
            // Counter-check: a mapper that rewrote ordinary text would be
            // far worse than the bug it fixes.
            for (char c = (char)32; c < (char)127; c++)
                Assert.AreEqual(c, Cp437.Map(c), "ASCII " + (int)c + " was altered");
        }

        [Test]
        public void TheBoxDrawingCharactersMapToTheirRealCp437Bytes()
        {
            // Spot-check against the actual code page rather than
            // trusting the switch to be self-consistent.
            Assert.AreEqual((char)0xCD, Cp437.Map('═'), "double horizontal");
            Assert.AreEqual((char)0xC4, Cp437.Map('─'), "single horizontal");
            Assert.AreEqual((char)0xB3, Cp437.Map('│'), "single vertical");
            Assert.AreEqual((char)0xDA, Cp437.Map('┌'), "top-left");
            Assert.AreEqual((char)0xBF, Cp437.Map('┐'), "top-right");
            Assert.AreEqual((char)0xC0, Cp437.Map('└'), "bottom-left");
            Assert.AreEqual((char)0xD9, Cp437.Map('┘'), "bottom-right");
        }

        [Test]
        public void CharactersWithNoCp437Equivalent_DegradeToReadableAscii()
        {
            // An em dash has no CP437 byte. Falling back to '?' would be
            // the same defect in a different costume; '-' keeps the line
            // legible. The inventory uses '—' for an empty craft slot.
            Assert.AreEqual('-', Cp437.Map('—'));
            Assert.AreEqual('-', Cp437.Map('–'));
            Assert.AreEqual('.', Cp437.Map('…'));
            Assert.AreEqual('"', Cp437.Map('“'));
            Assert.AreEqual('\'', Cp437.Map('’'));
        }

        // ════════════════════════════════════════════════════════
        // The sweep — the part that catches the NEXT one
        // ════════════════════════════════════════════════════════

        [Test]
        public void NoUiSourceDrawsACharacterTheAtlasCannotRender()
        {
            string uiDir = Path.Combine(Application.dataPath, "Scripts/Presentation/UI");
            Assert.IsTrue(Directory.Exists(uiDir), uiDir);

            var offenders = new List<string>();

            foreach (string file in Directory.GetFiles(uiDir, "*.cs", SearchOption.AllDirectories))
            {
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];

                    // Comments and docstrings are not drawn — and this
                    // file's own explanations are full of box glyphs.
                    string trimmed = line.TrimStart();
                    if (trimmed.StartsWith("//") || trimmed.StartsWith("///")
                        || trimmed.StartsWith("*")) continue;

                    foreach (Match m in Regex.Matches(line, "\"([^\"\\\\]|\\\\.)*\"|'([^'\\\\]|\\\\.)'"))
                    {
                        foreach (char c in m.Value)
                        {
                            if (c < 256) continue;
                            if (Cp437.Map(c) < 256) continue;   // handled

                            offenders.Add(Path.GetFileName(file) + ":" + (i + 1)
                                + "  U+" + ((int)c).ToString("X4") + " '" + c + "'");
                        }
                    }
                }
            }

            if (offenders.Count > 0)
            {
                var sb = new StringBuilder();
                sb.Append(offenders.Count).Append(" drawn character(s) the CP437 ")
                  .Append("atlas cannot render — each will appear on screen as '?'. ")
                  .Append("Add them to Cp437.Map:\n");
                foreach (string o in offenders) sb.Append("  ").Append(o).Append('\n');
                Assert.Fail(sb.ToString());
            }
        }
    }
}
