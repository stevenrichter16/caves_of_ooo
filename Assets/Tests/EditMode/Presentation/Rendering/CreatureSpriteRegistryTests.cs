using NUnit.Framework;
using CavesOfOoo.Rendering;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Phase A — pure-logic tests for CreatureSpriteRegistry's
    /// snake_case matcher. The actual sprite-loading path requires
    /// the AssetDatabase + Editor mode, exercised via the playmode
    /// screenshot pipeline (phaseA_creatures.png).
    /// </summary>
    public class CreatureSpriteRegistryTests
    {
        // ── snake_case conversion ────────────────────────────────

        [Test]
        public void ToSnakeCase_PallidArchivist_BecomesUnderscored()
        {
            Assert.AreEqual("pallid_archivist",
                CreatureSpriteRegistry.ToSnakeCase("PallidArchivist"));
        }

        [Test]
        public void ToSnakeCase_VoidmouthCantor_BecomesUnderscored()
        {
            Assert.AreEqual("voidmouth_cantor",
                CreatureSpriteRegistry.ToSnakeCase("VoidmouthCantor"));
        }

        [Test]
        public void ToSnakeCase_AllLower_StaysSame()
        {
            // Already snake_case → no change.
            Assert.AreEqual("snapjaw",
                CreatureSpriteRegistry.ToSnakeCase("snapjaw"));
            Assert.AreEqual("pallid_archivist",
                CreatureSpriteRegistry.ToSnakeCase("pallid_archivist"));
        }

        [Test]
        public void ToSnakeCase_TripleCamelCase_InsertsBothUnderscores()
        {
            // Adversarial: nested compound names like "MireAugerOfDoom"
            // should split between every camel boundary.
            Assert.AreEqual("mire_auger_of_doom",
                CreatureSpriteRegistry.ToSnakeCase("MireAugerOfDoom"));
        }

        [Test]
        public void ToSnakeCase_NullOrEmpty_ReturnsInput()
        {
            Assert.AreEqual(null, CreatureSpriteRegistry.ToSnakeCase(null));
            Assert.AreEqual("", CreatureSpriteRegistry.ToSnakeCase(""));
        }

        [Test]
        public void ToSnakeCase_DigitBoundary_InsertsUnderscore()
        {
            // Counter-check: digits are treated as lower-class so the
            // next uppercase still gets an underscore in front.
            Assert.AreEqual("snapjaw2_chief",
                CreatureSpriteRegistry.ToSnakeCase("Snapjaw2Chief"));
        }

        // ── Lookup contract ──────────────────────────────────────

        [Test]
        public void TryGet_NullBlueprint_ReturnsFalse()
        {
            CreatureSpriteRegistry.TestOnly_Reset();
            Assert.IsFalse(CreatureSpriteRegistry.TryGet(null, out var s));
            Assert.IsNull(s);
        }

        [Test]
        public void TryGet_EmptyBlueprint_ReturnsFalse()
        {
            CreatureSpriteRegistry.TestOnly_Reset();
            Assert.IsFalse(CreatureSpriteRegistry.TryGet("", out var s));
            Assert.IsNull(s);
        }

        [Test]
        public void TryGet_UnmappedBlueprint_ReturnsFalse()
        {
            // Adversarial: a blueprint that snake_cases to a name no
            // family folder exists for must NOT spuriously match.
            CreatureSpriteRegistry.TestOnly_Reset();
            Assert.IsFalse(
                CreatureSpriteRegistry.TryGet("ThisBlueprintDoesNotExist", out var s));
            Assert.IsNull(s);
        }
    }
}
