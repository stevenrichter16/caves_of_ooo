using NUnit.Framework;
using CavesOfOoo.Skills;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// ST.2 — SkillRegistry data-layer tests. Pins the JSON parsing
    /// contract (per-field round-trip), the four lookup paths
    /// (skill-by-name, skill-by-class, power-by-class, any-entry-by-class
    /// for gating), the missing-key counter-check, and the Flags bit-field
    /// accessor round-trip.
    ///
    /// <para>Pattern mirrors StoryletRegistryTests.cs (the precedent for
    /// JSON-loaded static-registry tests).</para>
    /// </summary>
    public class SkillRegistryTests
    {
        [SetUp]
        public void Setup()
        {
            SkillRegistry.ResetForTests();
        }

        // ====================================================================
        // 1. Round-trip: JSON parses + skill is queryable by name
        // ====================================================================

        [Test]
        public void LoadFromJson_SingleSkillWithPower_RegistersByName()
        {
            string json = @"{""Skills"":[{
                ""Name"":""Acrobatics"",
                ""Class"":""AcrobaticsSkill"",
                ""Cost"":100,
                ""Description"":""Athletic finesse."",
                ""Powers"":[
                    {""Name"":""Dodge"",""Class"":""AcrobaticsDodgePower"",
                     ""Cost"":50,""Attribute"":""Agility"",""Minimum"":""15"",
                     ""Description"":""+2 DV.""}
                ]
            }]}";

            SkillRegistry.InitializeFromJson(json);

            Assert.IsTrue(SkillRegistry.TryGetSkillByName("Acrobatics", out var skill),
                "Acrobatics must be queryable by name after JSON load.");
            Assert.IsNotNull(skill);
            Assert.AreEqual("Acrobatics", skill.Name);
            Assert.AreEqual("AcrobaticsSkill", skill.Class);
            Assert.AreEqual(100, skill.Cost);
            Assert.AreEqual("Athletic finesse.", skill.Description);
            Assert.AreEqual(1, skill.Powers.Count, "Dodge power should be a child.");
            Assert.AreEqual("Dodge", skill.Powers[0].Name);
            Assert.AreEqual(50, skill.Powers[0].Cost);
            Assert.AreEqual("Agility", skill.Powers[0].Attribute);
            Assert.AreEqual("15", skill.Powers[0].Minimum);
        }

        // ====================================================================
        // 2. Lookup by class works for skills
        // ====================================================================

        [Test]
        public void TryGetSkillByClass_ReturnsRegisteredSkill()
        {
            string json = @"{""Skills"":[{
                ""Name"":""Acrobatics"",""Class"":""AcrobaticsSkill"",""Cost"":100
            }]}";
            SkillRegistry.InitializeFromJson(json);

            Assert.IsTrue(SkillRegistry.TryGetSkillByClass("AcrobaticsSkill", out var skill),
                "Skill must be queryable by Class field.");
            Assert.AreEqual("Acrobatics", skill.Name);
        }

        // ====================================================================
        // 3. Power-by-class cross-tree lookup. The whole point of having
        //    a flat _powersByClass dict (mirroring Qud's PowersByClass) is
        //    that any power can be looked up without first knowing which
        //    tree it belongs to. Pin the parent back-reference too.
        // ====================================================================

        [Test]
        public void TryGetPowerByClass_FindsPowerAndPopulatesParentBackReference()
        {
            string json = @"{""Skills"":[{
                ""Name"":""Acrobatics"",""Class"":""AcrobaticsSkill"",""Cost"":100,
                ""Powers"":[
                    {""Name"":""Dodge"",""Class"":""AcrobaticsDodgePower"",""Cost"":50}
                ]
            }]}";
            SkillRegistry.InitializeFromJson(json);

            Assert.IsTrue(SkillRegistry.TryGetPowerByClass("AcrobaticsDodgePower", out var power),
                "Power must be queryable cross-tree by its own Class field.");
            Assert.AreEqual("Dodge", power.Name);
            Assert.AreEqual("Acrobatics", power.ParentSkillName,
                "Registry must populate ParentSkillName during load — " +
                "the back-reference is critical for ST.6 purchase gating " +
                "(Initiatory ordering uses parent context).");
        }

        // ====================================================================
        // 4. COUNTER-CHECK: missing keys return false, don't throw, don't
        //    populate the out-parameter. Adversarial null/empty/whitespace
        //    handling.
        // ====================================================================

        [Test]
        public void Lookups_MissingOrEmptyKey_ReturnFalseGracefully()
        {
            // Empty registry — every lookup must fail cleanly.
            Assert.IsFalse(SkillRegistry.TryGetSkillByName("DoesNotExist", out _),
                "Unknown skill name must return false.");
            Assert.IsFalse(SkillRegistry.TryGetSkillByClass("DoesNotExist", out _),
                "Unknown skill class must return false.");
            Assert.IsFalse(SkillRegistry.TryGetPowerByClass("DoesNotExist", out _),
                "Unknown power class must return false.");
            Assert.IsFalse(SkillRegistry.HasEntry("DoesNotExist"),
                "Unknown class must report HasEntry=false.");

            // Empty/whitespace inputs must not throw.
            Assert.IsFalse(SkillRegistry.TryGetSkillByName(null, out _));
            Assert.IsFalse(SkillRegistry.TryGetSkillByName("", out _));
            Assert.IsFalse(SkillRegistry.TryGetSkillByName("   ", out _));
            Assert.IsFalse(SkillRegistry.HasEntry(null));
            Assert.IsFalse(SkillRegistry.HasEntry(""));
        }

        // ====================================================================
        // 5. HasEntry: cross-class gating lookup must find both skills AND
        //    powers. Used by ST.6's BuySkillAction to validate the
        //    Requires / Exclusion lists (which can name either a skill or a
        //    power class).
        // ====================================================================

        [Test]
        public void HasEntry_FindsBothSkillsAndPowers()
        {
            string json = @"{""Skills"":[{
                ""Name"":""Acrobatics"",""Class"":""AcrobaticsSkill"",""Cost"":100,
                ""Powers"":[
                    {""Name"":""Dodge"",""Class"":""AcrobaticsDodgePower"",""Cost"":50}
                ]
            }]}";
            SkillRegistry.InitializeFromJson(json);

            Assert.IsTrue(SkillRegistry.HasEntry("AcrobaticsSkill"),
                "HasEntry must report true for a registered skill class.");
            Assert.IsTrue(SkillRegistry.HasEntry("AcrobaticsDodgePower"),
                "HasEntry must report true for a registered power class — " +
                "one shared dict makes Requires/Exclusion validation a " +
                "single lookup, not a multi-step skill-or-power probe.");
        }

        // ====================================================================
        // 6. Flags bit-field round-trip. Each of the 4 IPartEntry flag bits
        //    (Hidden/Obfuscated/Initiatory/ExcludeFromPool) round-trips
        //    independently through JSON via the underlying int Flags.
        //    Pinned because the bit accessors mutate the int field in
        //    different ways (each tested for isolation).
        // ====================================================================

        [Test]
        public void Flags_BitAccessors_RoundTripThroughJson()
        {
            // Encode all 4 flags set: 1+2+4+8 = 15
            string json = @"{""Skills"":[{
                ""Name"":""SecretArt"",""Class"":""SecretArtSkill"",""Cost"":200,
                ""Flags"":15
            }]}";
            SkillRegistry.InitializeFromJson(json);

            Assert.IsTrue(SkillRegistry.TryGetSkillByName("SecretArt", out var skill));
            Assert.IsTrue(skill.Hidden,          "Flags bit 0 (HIDDEN) should be set when Flags=15.");
            Assert.IsTrue(skill.Obfuscated,      "Flags bit 1 (OBFUSCATED) should be set when Flags=15.");
            Assert.IsTrue(skill.Initiatory,      "Flags bit 2 (INITIATORY) should be set when Flags=15.");
            Assert.IsTrue(skill.ExcludeFromPool, "Flags bit 3 (EX_POOL) should be set when Flags=15.");

            // Counter-check: defaults are all FALSE for a Flags=0 (omitted) skill.
            string noFlagsJson = @"{""Skills"":[{
                ""Name"":""Plain"",""Class"":""PlainSkill"",""Cost"":50
            }]}";
            SkillRegistry.InitializeFromJson(noFlagsJson);
            Assert.IsTrue(SkillRegistry.TryGetSkillByName("Plain", out var plain));
            Assert.IsFalse(plain.Hidden,          "Default Flags=0 must NOT set HIDDEN.");
            Assert.IsFalse(plain.Obfuscated,      "Default Flags=0 must NOT set OBFUSCATED.");
            Assert.IsFalse(plain.Initiatory,      "Default Flags=0 must NOT set INITIATORY.");
            Assert.IsFalse(plain.ExcludeFromPool, "Default Flags=0 must NOT set EX_POOL.");

            // Setter round-trip: the bit accessors must be able to set/clear
            // their respective bits without affecting other bits.
            plain.Hidden = true;
            Assert.AreEqual(SkillData.FLAG_HIDDEN, plain.Flags,
                "Setting Hidden=true should set bit 0 only.");
            plain.Initiatory = true;
            Assert.AreEqual(SkillData.FLAG_HIDDEN | SkillData.FLAG_INITIATORY, plain.Flags,
                "Setting Initiatory=true should ADD bit 2 without clearing bit 0.");
            plain.Hidden = false;
            Assert.AreEqual(SkillData.FLAG_INITIATORY, plain.Flags,
                "Clearing Hidden should remove bit 0 without affecting bit 2.");
        }
    }
}
