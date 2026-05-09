using System.Collections.Generic;
using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// SL.2 — Save/Load Round-Trip Audit, Tier-3 simple Parts.
    /// See <c>Docs/SAVE-LOAD-AUDIT.md</c> for the audit plan.
    ///
    /// <para>Targets Parts that fall through to the catch-all
    /// <c>WritePublicFields</c> reflection path (Tier 3) and have
    /// only "simple" public fields (int, string, bool, float). The
    /// rental audit proved this path works for `RentalPart`
    /// (int + string); this test class extends that proof to ~9
    /// other Parts of the same shape.</para>
    ///
    /// <para>Per ADVERSARIAL_TESTING.md "Strategy B (existing features)":
    /// these tests reverse-engineer the design intent + pin the
    /// reflection-based round-trip as a regression target. If a future
    /// contributor accidentally adds a private field that holds
    /// must-persist state, the existing tests still pass — but the
    /// new tests we'd add for that field would fail.</para>
    ///
    /// <para>Bug-class probes:
    /// <list type="bullet">
    ///   <item>SL-1 Public field round-trip — counter-check with
    ///         non-default values</item>
    ///   <item>SL-5 Generic collection (probes MaterialPart's
    ///         HashSet&lt;string&gt; — flagged as "likely unsupported"
    ///         in the audit plan)</item>
    ///   <item>SL-14 Empty/default Part round-trip baseline</item>
    /// </list></para>
    ///
    /// <para>Deferrals:
    /// <list type="bullet">
    ///   <item>PhysicsPart's <c>InInventory</c> and <c>Equipped</c>
    ///         Entity references → SL.3</item>
    ///   <item>Effect serialization (different code path) → SL.6</item>
    /// </list></para>
    /// </summary>
    public class Tier3SimplePartRoundTripTests
    {
        // ── A. Single-Part round-trip — one Part per entity ──────────────

        [Test]
        public void Adversarial_CommercePart_Value_RoundTrips()
        {
            var entity = new Entity { ID = "item", BlueprintName = "TestItem" };
            entity.AddPart(new CommercePart { Value = 137 });

            var loaded = PartRoundTripHelper.RoundTripEntity(entity);
            var part = loaded.GetPart<CommercePart>();
            Assert.IsNotNull(part);
            Assert.AreEqual(137, part.Value);
        }

        [Test]
        public void Adversarial_PhysicsPart_SimpleFields_RoundTrip()
        {
            // Simple fields only — Entity references (InInventory,
            // Equipped) are deferred to SL.3 because they need an
            // EntityFactory to resolve on load.
            var entity = new Entity { ID = "item", BlueprintName = "TestItem" };
            entity.AddPart(new PhysicsPart
            {
                Solid = true, Weight = 17, Takeable = false, Category = "Weapon"
            });

            var loaded = PartRoundTripHelper.RoundTripEntity(entity);
            var part = loaded.GetPart<PhysicsPart>();
            Assert.IsNotNull(part);
            Assert.IsTrue(part.Solid);
            Assert.AreEqual(17, part.Weight);
            Assert.IsFalse(part.Takeable);
            Assert.AreEqual("Weapon", part.Category);
        }

        [Test]
        public void Adversarial_RenderPart_AllFields_RoundTrip()
        {
            // RenderPart is the highest-traffic Part in the game (every
            // entity has one). Verify every public field round-trips.
            var entity = new Entity { ID = "e", BlueprintName = "TestEntity" };
            entity.AddPart(new RenderPart
            {
                DisplayName = "test entity",
                RenderString = "@",
                ColorString = "&Y",
                BackgroundColor = "&K",
                GlyphVariants = "@,#",
                DetailColor = "&W",
                TileColor = "&y",
                Tile = "tile_path",
                RenderLayer = 5,
                Visible = false,  // counter-check: non-default value
            });

            var loaded = PartRoundTripHelper.RoundTripEntity(entity);
            var part = loaded.GetPart<RenderPart>();
            Assert.IsNotNull(part);
            Assert.AreEqual("test entity", part.DisplayName);
            Assert.AreEqual("@", part.RenderString);
            Assert.AreEqual("&Y", part.ColorString);
            Assert.AreEqual("&K", part.BackgroundColor);
            Assert.AreEqual("@,#", part.GlyphVariants);
            Assert.AreEqual("&W", part.DetailColor);
            Assert.AreEqual("&y", part.TileColor);
            Assert.AreEqual("tile_path", part.Tile);
            Assert.AreEqual(5, part.RenderLayer);
            Assert.IsFalse(part.Visible,
                "Counter-check: a buggy impl that always returned default "
                + "(true) would pass tests using the default value.");
        }

        [Test]
        public void Adversarial_EquippablePart_Strings_RoundTrip()
        {
            var entity = new Entity { ID = "item", BlueprintName = "TestItem" };
            entity.AddPart(new EquippablePart
            {
                Slot = "Head", UsesSlots = "Head|Helmet", EquipBonuses = "DV+2"
            });

            var loaded = PartRoundTripHelper.RoundTripEntity(entity);
            var part = loaded.GetPart<EquippablePart>();
            Assert.IsNotNull(part);
            Assert.AreEqual("Head", part.Slot);
            Assert.AreEqual("Head|Helmet", part.UsesSlots);
            Assert.AreEqual("DV+2", part.EquipBonuses);
        }

        [Test]
        public void Adversarial_StackerPart_Counts_RoundTrip()
        {
            var entity = new Entity { ID = "stack", BlueprintName = "TestItem" };
            entity.AddPart(new StackerPart { StackCount = 42, MaxStack = 99 });

            var loaded = PartRoundTripHelper.RoundTripEntity(entity);
            var part = loaded.GetPart<StackerPart>();
            Assert.IsNotNull(part);
            Assert.AreEqual(42, part.StackCount);
            Assert.AreEqual(99, part.MaxStack);
        }

        [Test]
        public void Adversarial_ExaminablePart_Text_RoundTrips_IncludingSpecialChars()
        {
            // Counter-check special-char handling — quotes, newlines, unicode.
            // SaveWriter.WriteString must handle these or the round-trip
            // silently mangles the text.
            var entity = new Entity { ID = "e", BlueprintName = "TestEntity" };
            entity.AddPart(new ExaminablePart
            {
                Text = "\"quoted\" with\nnewline + unicode: ñ ☆ → ✦"
            });

            var loaded = PartRoundTripHelper.RoundTripEntity(entity);
            var part = loaded.GetPart<ExaminablePart>();
            Assert.IsNotNull(part);
            Assert.AreEqual(
                "\"quoted\" with\nnewline + unicode: ñ ☆ → ✦",
                part.Text);
        }

        [Test]
        public void Adversarial_LifespanPart_TurnsRemaining_RoundTrips()
        {
            var entity = new Entity { ID = "decaying", BlueprintName = "TestDecay" };
            entity.AddPart(new LifespanPart { TurnsRemaining = 7 });

            var loaded = PartRoundTripHelper.RoundTripEntity(entity);
            var part = loaded.GetPart<LifespanPart>();
            Assert.IsNotNull(part);
            Assert.AreEqual(7, part.TurnsRemaining);
        }

        [Test]
        public void Adversarial_FuelPart_FloatFields_RoundTripWithoutPrecisionLoss()
        {
            // Floats can lose precision through serialization. Use values
            // that round-trip exactly in IEEE 754 single-precision so the
            // assertion isn't "approximately equal" but exact.
            var entity = new Entity { ID = "fuel", BlueprintName = "TestFuel" };
            entity.AddPart(new FuelPart
            {
                FuelMass = 75.5f,
                MaxFuel = 100f,
                BurnRate = 0.25f,
                HeatOutput = 1.75f,
                ExhaustProduct = "AshPile"
            });

            var loaded = PartRoundTripHelper.RoundTripEntity(entity);
            var part = loaded.GetPart<FuelPart>();
            Assert.IsNotNull(part);
            Assert.AreEqual(75.5f, part.FuelMass);
            Assert.AreEqual(100f, part.MaxFuel);
            Assert.AreEqual(0.25f, part.BurnRate);
            Assert.AreEqual(1.75f, part.HeatOutput);
            Assert.AreEqual("AshPile", part.ExhaustProduct);
        }

        [Test]
        public void Adversarial_MaterialPart_SimpleFields_RoundTrip()
        {
            // SimpleFields = string + 5 floats + raw-string tag list.
            // The HashSet<string> probe is the next test below.
            var entity = new Entity { ID = "m", BlueprintName = "TestMaterial" };
            entity.AddPart(new MaterialPart
            {
                MaterialID = "Ironwood",
                Combustibility = 0.3f,
                Conductivity = 0.1f,
                Porosity = 0.5f,
                Volatility = 0.0f,
                Brittleness = 0.7f,
                MaterialTagsRaw = "wood,organic,fibrous"
            });

            var loaded = PartRoundTripHelper.RoundTripEntity(entity);
            var part = loaded.GetPart<MaterialPart>();
            Assert.IsNotNull(part);
            Assert.AreEqual("Ironwood", part.MaterialID);
            Assert.AreEqual(0.3f, part.Combustibility);
            Assert.AreEqual(0.1f, part.Conductivity);
            Assert.AreEqual(0.5f, part.Porosity);
            Assert.AreEqual(0.0f, part.Volatility);
            Assert.AreEqual(0.7f, part.Brittleness);
            Assert.AreEqual("wood,organic,fibrous", part.MaterialTagsRaw);
        }

        // ── B. Bug-class probes: HashSet<string> + Initialize-on-load ────

        [Test]
        public void Adversarial_MaterialPart_TagsRaw_DerivesToHashSet_RoundTripsBoth()
        {
            // PRODUCTION PATTERN: blueprints set MaterialTagsRaw;
            // Initialize() parses it into the MaterialTags HashSet at
            // AddPart time. Round-trip should preserve BOTH the raw
            // string AND the derived HashSet.
            //
            // Save path: SaveEntityBody writes Tags/Properties/IntProperties/
            //  Statistics/Parts. WritePublicFields walks each Part,
            //  serializing public fields via WriteFieldValue. HashSet<>
            //  is a supported type (SaveSystem.cs:1635, 1685, 1750).
            //
            // Load path: SaveSystem.cs:656 uses entity.Parts.Add (direct),
            //  NOT entity.AddPart() — so Initialize() does NOT run on
            //  load. The HashSet contents come from the save file
            //  verbatim.
            var entity = new Entity { ID = "m", BlueprintName = "TestMat" };
            var src = new MaterialPart
            {
                MaterialID = "Bone",
                MaterialTagsRaw = "bone,organic,calcified"
            };
            entity.AddPart(src); // Initialize parses the raw string
            Assert.AreEqual(3, src.MaterialTags.Count,
                "Setup: AddPart-triggered Initialize parses 3 tags.");

            var loaded = PartRoundTripHelper.RoundTripEntity(entity);
            var part = loaded.GetPart<MaterialPart>();
            Assert.IsNotNull(part);
            Assert.AreEqual("bone,organic,calcified", part.MaterialTagsRaw);
            Assert.AreEqual(3, part.MaterialTags.Count,
                "Derived HashSet round-trips with all 3 entries.");
            Assert.IsTrue(part.MaterialTags.Contains("bone"));
            Assert.IsTrue(part.MaterialTags.Contains("organic"));
            Assert.IsTrue(part.MaterialTags.Contains("calcified"));
        }

        [Test]
        public void Adversarial_MaterialPart_LoadDoesNotCallInitialize_SavedCacheWinsOverDerivation()
        {
            // ADVERSARIAL: probe whether Initialize() runs on load.
            //
            // Setup: MaterialTagsRaw="bone" (1 tag). Manually mutate the
            // HashSet to be out-of-sync (3 tags). Round-trip. If
            // Initialize runs on load → re-parsed → count=1; if NOT →
            // saved HashSet (count=3) wins.
            //
            // Per SaveSystem.cs:656 the load path is entity.Parts.Add
            // direct (NOT AddPart), so Initialize is NOT called. The
            // saved HashSet wins. This test pins that contract — if a
            // future change makes Initialize run on load, this test
            // breaks visibly.
            var entity = new Entity { ID = "m3", BlueprintName = "TestMat3" };
            var src = new MaterialPart
            {
                MaterialID = "Bone",
                MaterialTagsRaw = "bone"
            };
            entity.AddPart(src);
            Assert.AreEqual(1, src.MaterialTags.Count, "Setup: initial parse → 1 tag.");

            // Diverge HashSet from raw.
            src.MaterialTags.Add("organic");
            src.MaterialTags.Add("calcified");
            Assert.AreEqual(3, src.MaterialTags.Count, "Setup: post-mutation → 3 tags.");

            var loaded = PartRoundTripHelper.RoundTripEntity(entity);
            var part = loaded.GetPart<MaterialPart>();
            Assert.AreEqual(3, part.MaterialTags.Count,
                "MaterialTags must round-trip the SAVED state (count=3), "
                + "NOT re-derive from MaterialTagsRaw (count=1). If this "
                + "fails, Initialize() is being called on load.");
            Assert.AreEqual("bone", part.MaterialTagsRaw,
                "Raw field round-trips unchanged.");
        }

        // ── C. Multi-Part round-trip ─────────────────────────────────────

        [Test]
        public void Adversarial_EntityWithMultipleSimpleParts_AllRoundTrip()
        {
            // An entity with 5+ simple Parts, all of which should
            // round-trip independently. Catches a bug where Parts
            // serialize correctly individually but the Parts list
            // serialization gets the order wrong or skips entries.
            var entity = new Entity { ID = "compound", BlueprintName = "TestCompound" };
            entity.AddPart(new RenderPart { DisplayName = "compound" });
            entity.AddPart(new CommercePart { Value = 50 });
            entity.AddPart(new LifespanPart { TurnsRemaining = 10 });
            entity.AddPart(new ExaminablePart { Text = "an examinable thing" });
            entity.AddPart(new StackerPart { StackCount = 3, MaxStack = 10 });

            var loaded = PartRoundTripHelper.RoundTripEntity(entity);

            Assert.AreEqual(5, loaded.Parts.Count);
            Assert.AreEqual("compound", loaded.GetPart<RenderPart>()?.DisplayName);
            Assert.AreEqual(50, loaded.GetPart<CommercePart>()?.Value);
            Assert.AreEqual(10, loaded.GetPart<LifespanPart>()?.TurnsRemaining);
            Assert.AreEqual("an examinable thing", loaded.GetPart<ExaminablePart>()?.Text);
            Assert.AreEqual(3, loaded.GetPart<StackerPart>()?.StackCount);
        }

        // ── D. Default-value baseline (counter-check) ────────────────────

        [Test]
        public void Adversarial_DefaultPart_RoundTripsToSameDefaults()
        {
            // Counter-check: prove a default-constructed Part round-trips
            // to its default values. Without this, every other test in
            // this file could pass for the wrong reason — "the loaded
            // values match because both copies still hold defaults."
            //
            // Ensure non-default values WERE set in the other tests by
            // contrasting with a fresh-default round-trip here.
            var entity = new Entity { ID = "default", BlueprintName = "TestDefault" };
            entity.AddPart(new CommercePart()); // Value defaults to 1

            var loaded = PartRoundTripHelper.RoundTripEntity(entity);
            var part = loaded.GetPart<CommercePart>();
            Assert.IsNotNull(part);
            Assert.AreEqual(1, part.Value,
                "Default CommercePart.Value is 1; round-trip preserves the default.");
        }

        // ── E. Entity-level identity (Tags + IntProperties + Statistics) ─

        [Test]
        public void Adversarial_EntityIDAndBlueprintName_RoundTrip()
        {
            // Entity-level identity fields are written by SaveEntityBody
            // BEFORE any Parts. Verify they round-trip. Adversarial:
            // a buggy impl that swapped ID/BlueprintName order between
            // save and load would surface here.
            var entity = new Entity
            {
                ID = "specific_id_42",
                BlueprintName = "SpecificBlueprint"
            };
            entity.Tags["TestTag"] = "tag_value";
            entity.Properties["TestProp"] = "prop_value";
            entity.IntProperties["TestInt"] = 999;

            var loaded = PartRoundTripHelper.RoundTripEntity(entity);
            Assert.AreEqual("specific_id_42", loaded.ID);
            Assert.AreEqual("SpecificBlueprint", loaded.BlueprintName);
            Assert.IsTrue(loaded.Tags.ContainsKey("TestTag"));
            Assert.AreEqual("tag_value", loaded.Tags["TestTag"]);
            Assert.IsTrue(loaded.Properties.ContainsKey("TestProp"));
            Assert.AreEqual("prop_value", loaded.Properties["TestProp"]);
            Assert.IsTrue(loaded.IntProperties.ContainsKey("TestInt"));
            Assert.AreEqual(999, loaded.IntProperties["TestInt"]);
        }
    }
}
