using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Cold-eye-review Finding #2 RED-then-GREEN coverage. MaterialPart
    /// parses MaterialTagsRaw → MaterialTags HashSet in Initialize().
    /// Initialize is called by Entity.AddPart but NOT by the save/load
    /// reflection-rebuilder path — so the audit predicted that a
    /// round-tripped MaterialPart would have an empty MaterialTags
    /// HashSet, silently disabling
    /// EnhancementPaleSalt / EnhancementChoirIron's tag-bonus damage
    /// post-load.
    ///
    /// <para>This fixture pins the round-trip contract regardless of
    /// which fix lands (OnAfterLoad override, computed property, etc.).</para>
    /// </summary>
    public class MaterialPartRoundTripTests
    {
        [Test]
        public void MaterialTagsRaw_RoundTrips_ViaReflection()
        {
            // Baseline check — the raw string field is a public string
            // and round-trips via the SL.6 reflection path. Pin this
            // first so the next test's failure unambiguously points at
            // the Initialize() / parse path, not at the raw field.
            var e = new Entity { ID = "skel", BlueprintName = "skel" };
            e.AddPart(new MaterialPart { MaterialTagsRaw = "Bone,Dry,Undead" });

            Entity loaded = PartRoundTripHelper.RoundTripEntity(e);

            var mat = loaded.GetPart<MaterialPart>();
            Assert.IsNotNull(mat);
            Assert.AreEqual("Bone,Dry,Undead", mat.MaterialTagsRaw,
                "Raw tag string field round-trips via reflection.");
        }

        [Test]
        public void HasMaterialTag_StillWorksAfterRoundTrip()
        {
            // The audit's Finding #2 hypothesis: after round-trip the
            // MaterialTags HashSet is empty (Initialize never re-ran),
            // so HasMaterialTag returns false. This test will go RED
            // if the hypothesis is correct, and GREEN after the fix.
            var e = new Entity { ID = "skel", BlueprintName = "skel" };
            e.AddPart(new MaterialPart { MaterialTagsRaw = "Bone,Dry,Undead" });
            // Sanity: the LIVE Part works.
            Assert.IsTrue(e.GetPart<MaterialPart>().HasMaterialTag("Undead"),
                "Precondition: pre-roundtrip MaterialPart.HasMaterialTag works.");

            Entity loaded = PartRoundTripHelper.RoundTripEntity(e);

            var mat = loaded.GetPart<MaterialPart>();
            Assert.IsNotNull(mat);
            Assert.IsTrue(mat.HasMaterialTag("Undead"),
                "After round-trip, HasMaterialTag('Undead') must still return true. " +
                "If this fails, EnhancementPaleSalt's bonus damage silently breaks " +
                "for any creature loaded from save (saved-game vs Skeleton fight).");
            Assert.IsTrue(mat.HasMaterialTag("Bone"));
            Assert.IsTrue(mat.HasMaterialTag("Dry"));
        }

        [Test]
        public void PaleSaltBonusDamage_FiresAfterRoundTrip()
        {
            // Integration: a Pale-Salt-edged sword applied to a
            // round-tripped Skeleton still fires the bonus.
            var skeleton = new Entity { ID = "skel", BlueprintName = "skel" };
            skeleton.Tags["Creature"] = "";
            skeleton.Statistics["Hitpoints"] = new Stat
                { Owner = skeleton, Name = "Hitpoints", BaseValue = 100, Min = 0, Max = 100 };
            skeleton.Statistics["Toughness"] = new Stat
                { Owner = skeleton, Name = "Toughness", BaseValue = 8, Min = 1, Max = 50 };
            skeleton.AddPart(new RenderPart { DisplayName = "skeleton" });
            skeleton.AddPart(new StatusEffectsPart());
            skeleton.AddPart(new MaterialPart { MaterialTagsRaw = "Bone,Undead" });

            Entity loaded = PartRoundTripHelper.RoundTripEntity(skeleton);

            // Re-attach Hitpoints/Toughness — RoundTripEntity preserves
            // Statistics dict via reflection. Pin that pre-condition.
            Assert.AreEqual(100, loaded.GetStatValue("Hitpoints"));

            var enh = new EnhancementPaleSalt();
            enh.ApplyTier(3);  // +6 bonus
            int hpBefore = loaded.GetStatValue("Hitpoints");

            enh.OnAttackerHit(loaded, attacker: null,
                damage: new Damage(5), actualDamage: 5, zone: null,
                rng: new System.Random(0));

            int hpAfter = loaded.GetStatValue("Hitpoints");
            Assert.AreEqual(hpBefore - 6, hpAfter,
                "Pale-Salt bonus damage (+6 at Tier 3) fires on round-tripped " +
                "Undead defender. If this fails, saved-game combat silently lacks " +
                "the bonus.");
        }
    }
}
