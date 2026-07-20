using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests.Presentation.Rendering
{
    /// <summary>
    /// The UI text tile bank (GetUiTile) — pure font, no gameplay glyph
    /// overrides. Regression target for the live finding that popup text
    /// rendered creatures and terrain mid-sentence ('s'=snapjaw, 'T'=tree,
    /// apostrophe=floor pebble, '?'=unknown-blob, '&gt;'=stairs...). The map
    /// keeps GetTile; UI surfaces draw from this bank.
    /// </summary>
    public class CP437UiTileBankTests
    {
        // Every character the GAME bank repurposes as a gameplay sprite.
        private static readonly char[] OverriddenChars =
        {
            '@', '#', '/', 's', '?', '>', '<', '+', '-', '|', '~',
            '.', ',', '\'', '`', 'T'
        };

        [SetUp]
        public void Setup()
        {
            CP437TilesetGenerator.ClearCache();
        }

        private static Color[] SpritePixels(Sprite sprite)
        {
            Rect r = sprite.rect;
            return sprite.texture.GetPixels((int)r.x, (int)r.y, (int)r.width, (int)r.height);
        }

        private static int InkCount(Sprite sprite)
        {
            int ink = 0;
            foreach (Color p in SpritePixels(sprite))
                if (p.a > 0.5f)
                    ink++;
            return ink;
        }

        [Test]
        public void UiBank_IsASeparateAtlas_ForEveryOverriddenChar()
        {
            foreach (char c in OverriddenChars)
            {
                Tile ui = CP437TilesetGenerator.GetUiTile(c);
                Tile game = CP437TilesetGenerator.GetTile(c);

                Assert.IsNotNull(ui, $"UI tile for '{c}'");
                Assert.IsNotNull(game, $"game tile for '{c}'");
                StringAssert.StartsWith("UI_", ui.sprite.name, $"'{c}' comes from the UI atlas");
                StringAssert.StartsWith("CP437_", game.sprite.name, $"'{c}' game tile untouched");
                Assert.AreNotSame(ui.sprite.texture, game.sprite.texture,
                    $"'{c}': the banks must not share a texture");
            }
        }

        [Test]
        public void UiBank_TextGlyphs_DifferFromGameSprites()
        {
            // The point of the split: for chars the game bank overrides, the
            // UI glyph must be the FONT form, i.e. different ink than the
            // game sprite. (Identical ink would mean the override leaked in.)
            // '.' is deliberately absent: its game "floor speck" override
            // draws pixels that all fall INSIDE the font period's ink
            // (DrawChar16 sets pixels without clearing), so the two banks
            // are legitimately identical for that one character.
            foreach (char c in new[] { 's', 'T', '?', '/', '>', '<', '\'' })
            {
                Color[] ui = SpritePixels(CP437TilesetGenerator.GetUiTile(c).sprite);
                Color[] game = SpritePixels(CP437TilesetGenerator.GetTile(c).sprite);

                bool identical = true;
                for (int i = 0; i < ui.Length && identical; i++)
                    if ((ui[i].a > 0.5f) != (game[i].a > 0.5f))
                        identical = false;

                Assert.IsFalse(identical,
                    $"'{c}': UI glyph must be the font form, not the gameplay sprite");
            }
        }

        [Test]
        public void UiBank_EveryTextCriticalGlyph_HasInk()
        {
            // A blank glyph would silently swallow characters. Every letter,
            // digit, and repurposed punctuation must actually draw something.
            var critical = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789"
                + "?/><+-|~@#.,'`!\"():;[]";
            foreach (char c in critical)
            {
                Tile tile = CP437TilesetGenerator.GetUiTile(c);
                Assert.IsNotNull(tile, $"tile for '{c}'");
                Assert.Greater(InkCount(tile.sprite), 0, $"glyph '{c}' must have ink");
            }
        }

        [Test]
        public void UiBank_FrameGlyphsAndBlock_MatchTheGameBankShapes()
        {
            // Popup borders/fills draw from the UI bank now — the frame
            // glyphs are SHARED with the game atlas (DrawFrameGlyphs), so
            // their ink must be identical bank-to-bank.
            foreach (char c in new[]
            {
                CP437TilesetGenerator.BoxTopLeft,
                CP437TilesetGenerator.BoxTopRight,
                CP437TilesetGenerator.BoxBottomLeft,
                CP437TilesetGenerator.BoxBottomRight,
                CP437TilesetGenerator.BoxHorizontal,
                CP437TilesetGenerator.BoxVertical,
                CP437TilesetGenerator.SolidBlock
            })
            {
                Color[] ui = SpritePixels(CP437TilesetGenerator.GetUiTile(c).sprite);
                Color[] game = SpritePixels(CP437TilesetGenerator.GetTile(c).sprite);

                Assert.AreEqual(game.Length, ui.Length);
                for (int i = 0; i < ui.Length; i++)
                {
                    Assert.AreEqual(game[i].a > 0.5f, ui[i].a > 0.5f,
                        $"frame glyph 0x{(int)c:X2} pixel {i} must match the game bank");
                }
            }
        }

        [Test]
        public void UiBank_UnknownChar_FallsBackToQuestionMark_NoRegen()
        {
            // Same perf contract as GetTextTile: a non-CP437 char returns
            // the '?' tile and never rebuilds the atlas.
            Tile q = CP437TilesetGenerator.GetUiTile('?');
            Tile emDash = CP437TilesetGenerator.GetUiTile('—');

            Assert.AreSame(q, emDash, "unknown chars fall back to the font '?'");
        }
    }
}
