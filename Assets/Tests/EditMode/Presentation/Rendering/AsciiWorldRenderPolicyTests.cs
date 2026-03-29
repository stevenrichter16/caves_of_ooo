using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public class AsciiWorldRenderPolicyTests
    {
        [Test]
        public void GetGlyphOrFallback_UsesSinglePrintableGlyph()
        {
            var render = new RenderPart
            {
                RenderString = "#",
                ColorString = "&w"
            };

            char glyph = AsciiWorldRenderPolicy.GetGlyphOrFallback(render, out string issue);

            Assert.AreEqual('#', glyph);
            Assert.IsNull(issue);
        }

        [Test]
        public void GetGlyphOrFallback_RejectsMultiCharacterRenderString()
        {
            var render = new RenderPart
            {
                RenderString = "##",
                ColorString = "&w"
            };

            char glyph = AsciiWorldRenderPolicy.GetGlyphOrFallback(render, out string issue);

            Assert.AreEqual(AsciiWorldRenderPolicy.FallbackGlyph, glyph);
            StringAssert.Contains("must be exactly one printable glyph", issue);
        }

        [Test]
        public void GetGlyphOrFallback_RejectsControlCharacters()
        {
            var render = new RenderPart
            {
                RenderString = "\n",
                ColorString = "&w"
            };

            char glyph = AsciiWorldRenderPolicy.GetGlyphOrFallback(render, out string issue);

            Assert.AreEqual(AsciiWorldRenderPolicy.FallbackGlyph, glyph);
            StringAssert.Contains("not printable", issue);
        }

        [Test]
        public void GetColorOrFallback_RejectsBlankColorString()
        {
            var render = new RenderPart
            {
                RenderString = ".",
                ColorString = ""
            };

            string color = AsciiWorldRenderPolicy.GetColorOrFallback(render, out string issue);

            Assert.AreEqual(AsciiWorldRenderPolicy.FallbackColorString, color);
            Assert.AreEqual("missing ColorString", issue);
        }

        [Test]
        public void RealContent_ConcreteTerrainBlueprintsPassAsciiValidation()
        {
            var factory = new EntityFactory();
            string blueprintPath = Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json");
            factory.LoadBlueprints(File.ReadAllText(blueprintPath));

            string[] blueprintNames =
            {
                "Floor",
                "Rubble",
                "Sand",
                "Grass",
                "StoneFloor",
                "Wall",
                "SandstoneWall",
                "StoneWall",
                "VineWall",
                "Bush",
                "Tree",
                "StairsDown",
                "StairsUp"
            };

            for (int i = 0; i < blueprintNames.Length; i++)
            {
                var issues = factory.ValidateAsciiWorldBlueprint(blueprintNames[i]);
                Assert.IsEmpty(issues, $"{blueprintNames[i]}: {string.Join("; ", issues)}");
            }
        }
    }
}
