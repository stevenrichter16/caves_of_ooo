using NUnit.Framework;
using CavesOfOoo.Rendering;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Pass 10 §10D — pure-static blueprint matchers used by the
    /// per-blueprint sprite resolver in EnvironmentSpriteRenderer.
    /// These tests pin the exact match contract so a future content
    /// rename / refactor surfaces here, not as a silent visual bug.
    /// </summary>
    public class EnvironmentSpriteRendererBlueprintTests
    {
        // ── BlueprintIsChest ──────────────────────────────────────

        [Test]
        public void Chest_ExactName_Matches()
        {
            // The base "Chest" blueprint in Objects.json.
            Assert.IsTrue(InvokeIsChest("Chest"));
        }

        [Test]
        public void Chest_LockedChest_Matches()
        {
            Assert.IsTrue(InvokeIsChest("LockedChest"));
        }

        [Test]
        public void Chest_MimicChest_Matches()
        {
            Assert.IsTrue(InvokeIsChest("MimicChest"));
        }

        [Test]
        public void Chest_LeatherArmor_DoesNotMatch()
        {
            // Counter-check: armor at `[` must NOT render as a chest.
            Assert.IsFalse(InvokeIsChest("LeatherArmor"));
        }

        [Test]
        public void Chest_NullOrEmpty_DoesNotMatch()
        {
            Assert.IsFalse(InvokeIsChest(null));
            Assert.IsFalse(InvokeIsChest(""));
        }

        // ── BlueprintIsLantern ────────────────────────────────────

        [Test]
        public void Lantern_ExactName_Matches()
        {
            Assert.IsTrue(InvokeIsLantern("Lantern"));
        }

        [Test]
        public void Lantern_WatchLantern_Matches()
        {
            // Real blueprint name in the current Objects.json.
            Assert.IsTrue(InvokeIsLantern("WatchLantern"));
        }

        [Test]
        public void Lantern_LanternOil_DoesNotMatch()
        {
            // Adversarial: the FUEL tonic shares the prefix but is
            // not a light source. If this regresses, every potion of
            // lantern oil would suddenly emit a Light2D.
            Assert.IsFalse(InvokeIsLantern("LanternOil"),
                "LanternOil is a tonic, not a light source — must not match.");
        }

        [Test]
        public void Lantern_LanternGroundMarker_DoesNotMatch()
        {
            // Marker entity (placement helper) — also should not
            // light up. Only entities that END with "Lantern" qualify.
            Assert.IsFalse(InvokeIsLantern("LanternGroundMarker"));
        }

        [Test]
        public void Lantern_HealingTonic_DoesNotMatch()
        {
            Assert.IsFalse(InvokeIsLantern("HealingTonic"));
        }

        [Test]
        public void Lantern_NullOrEmpty_DoesNotMatch()
        {
            Assert.IsFalse(InvokeIsLantern(null));
            Assert.IsFalse(InvokeIsLantern(""));
        }

        // ── BlueprintIsBed (Pass 11) ──────────────────────────────

        [Test]
        public void Bed_ExactName_Matches()
        {
            Assert.IsTrue(InvokeIsBed("Bed"));
        }

        [Test]
        public void Bed_StrawBed_Matches()
        {
            // Variant naming convention.
            Assert.IsTrue(InvokeIsBed("StrawBed"));
        }

        [Test]
        public void Bed_Bedroll_DoesNotMatch()
        {
            // Adversarial: "Bedroll" is a carryable item, not bed
            // furniture. Suffix matching prevents the spurious claim.
            Assert.IsFalse(InvokeIsBed("Bedroll"),
                "Bedroll is a carryable, not bed furniture — must not match.");
        }

        [Test]
        public void Bed_NullOrEmpty_DoesNotMatch()
        {
            Assert.IsFalse(InvokeIsBed(null));
            Assert.IsFalse(InvokeIsBed(""));
        }

        // ── BlueprintIsCorpse (Pass 11) ───────────────────────────

        [Test]
        public void Corpse_SnapjawCorpse_Matches()
        {
            Assert.IsTrue(InvokeIsCorpse("SnapjawCorpse"));
        }

        [Test]
        public void Corpse_HumanCorpse_Matches()
        {
            Assert.IsTrue(InvokeIsCorpse("HumanCorpse"));
        }

        [Test]
        public void Corpse_Mushroom_DoesNotMatch()
        {
            // Counter-check: mushroom shares the `%` glyph with corpse
            // but should never render as a corpse — wrong content.
            Assert.IsFalse(InvokeIsCorpse("Mushroom"));
        }

        [Test]
        public void Corpse_NullOrEmpty_DoesNotMatch()
        {
            Assert.IsFalse(InvokeIsCorpse(null));
            Assert.IsFalse(InvokeIsCorpse(""));
        }

        // ══════════════════════════════════════════════════════════
        //   Pass 12 — blueprint-keyed terrain identity + farming +
        //   village fixtures (10 new tiles). Public static resolvers
        //   so the contract is pinned without reflection.
        // ══════════════════════════════════════════════════════════

        // ── ResolveFixtureKind: EXACT blueprint name → kind ──────

        [Test]
        public void Fixture_TerrainIdentity_Matches()
        {
            Assert.AreEqual(EnvironmentSpriteRenderer.EnvFixtureKind.Grass,
                EnvironmentSpriteRenderer.ResolveFixtureKind("Grass"));
            Assert.AreEqual(EnvironmentSpriteRenderer.EnvFixtureKind.Sand,
                EnvironmentSpriteRenderer.ResolveFixtureKind("Sand"));
            Assert.AreEqual(EnvironmentSpriteRenderer.EnvFixtureKind.Bank,
                EnvironmentSpriteRenderer.ResolveFixtureKind("Bank"));
        }

        [Test]
        public void Fixture_VillageStations_Match()
        {
            Assert.AreEqual(EnvironmentSpriteRenderer.EnvFixtureKind.Well,
                EnvironmentSpriteRenderer.ResolveFixtureKind("Well"));
            Assert.AreEqual(EnvironmentSpriteRenderer.EnvFixtureKind.MarketStall,
                EnvironmentSpriteRenderer.ResolveFixtureKind("MarketStall"));
            Assert.AreEqual(EnvironmentSpriteRenderer.EnvFixtureKind.TinkersForge,
                EnvironmentSpriteRenderer.ResolveFixtureKind("TinkersForge"));
            Assert.AreEqual(EnvironmentSpriteRenderer.EnvFixtureKind.AlchemyStill,
                EnvironmentSpriteRenderer.ResolveFixtureKind("AlchemyStill"));
        }

        [Test]
        public void Fixture_NearMissNames_DoNotMatch()
        {
            // Exact-name contract: prefix/superstring blueprints must NOT
            // inherit the tile. SandstoneFloor is a stone floor, not sand;
            // SilverSand is a carried ITEM; WellGroundMarker is a placement
            // helper; WellKeeper is a creature.
            var none = EnvironmentSpriteRenderer.EnvFixtureKind.None;
            Assert.AreEqual(none, EnvironmentSpriteRenderer.ResolveFixtureKind("SandstoneFloor"));
            Assert.AreEqual(none, EnvironmentSpriteRenderer.ResolveFixtureKind("SilverSand"));
            Assert.AreEqual(none, EnvironmentSpriteRenderer.ResolveFixtureKind("WellGroundMarker"));
            Assert.AreEqual(none, EnvironmentSpriteRenderer.ResolveFixtureKind("WellKeeper"));
            Assert.AreEqual(none, EnvironmentSpriteRenderer.ResolveFixtureKind("StoneFloor"));
        }

        [Test]
        public void Fixture_NullOrEmpty_DoesNotMatch()
        {
            var none = EnvironmentSpriteRenderer.EnvFixtureKind.None;
            Assert.AreEqual(none, EnvironmentSpriteRenderer.ResolveFixtureKind(null));
            Assert.AreEqual(none, EnvironmentSpriteRenderer.ResolveFixtureKind(""));
        }

        // ── ResolveCropKind: *Crop suffix + growth stage ─────────

        [Test]
        public void Crop_SeedStage_AnyCropBlueprint_GetsSeedMound()
        {
            // Stage 0 is a generic tilled mound — safe for ANY crop,
            // including future ones this table doesn't know.
            Assert.AreEqual(EnvironmentSpriteRenderer.CropSpriteKind.Seed,
                EnvironmentSpriteRenderer.ResolveCropKind("CandyCarrotCrop", 0));
            Assert.AreEqual(EnvironmentSpriteRenderer.CropSpriteKind.Seed,
                EnvironmentSpriteRenderer.ResolveCropKind("EmberwheatCrop", 0));
            Assert.AreEqual(EnvironmentSpriteRenderer.CropSpriteKind.Seed,
                EnvironmentSpriteRenderer.ResolveCropKind("SomeFutureCrop", 0));
        }

        [Test]
        public void Crop_SproutStage_KnownCrops_GetTheirTile()
        {
            Assert.AreEqual(EnvironmentSpriteRenderer.CropSpriteKind.CandyCarrot,
                EnvironmentSpriteRenderer.ResolveCropKind("CandyCarrotCrop", 1));
            Assert.AreEqual(EnvironmentSpriteRenderer.CropSpriteKind.Emberwheat,
                EnvironmentSpriteRenderer.ResolveCropKind("EmberwheatCrop", 1));
        }

        [Test]
        public void Crop_SproutStage_UnknownCrop_KeepsGlyph()
        {
            // A future crop with no sprite keeps its CP437 glyph rather
            // than borrowing another crop's look.
            Assert.AreEqual(EnvironmentSpriteRenderer.CropSpriteKind.None,
                EnvironmentSpriteRenderer.ResolveCropKind("SomeFutureCrop", 1));
        }

        [Test]
        public void Crop_ProduceAndNonCrops_DoNotMatch()
        {
            // CandyCarrot is the harvested PRODUCE item — the sprout tile
            // must not claim it (glyph '%' → existing mushroom handling).
            Assert.AreEqual(EnvironmentSpriteRenderer.CropSpriteKind.None,
                EnvironmentSpriteRenderer.ResolveCropKind("CandyCarrot", 1));
            Assert.AreEqual(EnvironmentSpriteRenderer.CropSpriteKind.None,
                EnvironmentSpriteRenderer.ResolveCropKind("Grass", 0));
            Assert.AreEqual(EnvironmentSpriteRenderer.CropSpriteKind.None,
                EnvironmentSpriteRenderer.ResolveCropKind(null, 0));
            Assert.AreEqual(EnvironmentSpriteRenderer.CropSpriteKind.None,
                EnvironmentSpriteRenderer.ResolveCropKind("", 1));
        }

        [Test]
        public void Crop_NoCropPart_SentinelStage_DoesNotMatch()
        {
            // Stage -1 = the entity had no CropPart (name ends in Crop but
            // isn't a real crop). Keep the glyph.
            Assert.AreEqual(EnvironmentSpriteRenderer.CropSpriteKind.None,
                EnvironmentSpriteRenderer.ResolveCropKind("CandyCarrotCrop", -1));
        }

        [Test]
        public void Crop_CorruptHighStage_TreatedAsSprout()
        {
            // Defensive: stage 2+ (corrupt save) renders as the sprout,
            // not a crash or a fallthrough to None.
            Assert.AreEqual(EnvironmentSpriteRenderer.CropSpriteKind.CandyCarrot,
                EnvironmentSpriteRenderer.ResolveCropKind("CandyCarrotCrop", 7));
        }

        // ── Reflection helpers (the matchers are private static) ──

        private static bool InvokeIsChest(string bp)
        {
            var t = typeof(EnvironmentSpriteRenderer);
            var m = t.GetMethod("BlueprintIsChest",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            return (bool)m.Invoke(null, new object[] { bp });
        }

        private static bool InvokeIsLantern(string bp)
        {
            var t = typeof(EnvironmentSpriteRenderer);
            var m = t.GetMethod("BlueprintIsLantern",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            return (bool)m.Invoke(null, new object[] { bp });
        }

        private static bool InvokeIsBed(string bp)
        {
            var t = typeof(EnvironmentSpriteRenderer);
            var m = t.GetMethod("BlueprintIsBed",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            return (bool)m.Invoke(null, new object[] { bp });
        }

        private static bool InvokeIsCorpse(string bp)
        {
            var t = typeof(EnvironmentSpriteRenderer);
            var m = t.GetMethod("BlueprintIsCorpse",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            return (bool)m.Invoke(null, new object[] { bp });
        }
    }
}
