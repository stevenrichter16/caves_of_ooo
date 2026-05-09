using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Pass 3 wiring adversarial tests — probe the END-TO-END pipeline,
    /// not just the data layer. The Pass 3 commits added HDR colors,
    /// flicker Parts, and biome palettes; the unit tests verified each
    /// in isolation. These tests verify the **wiring** — that the data
    /// actually flows from production code paths to the rendering
    /// pipeline.
    ///
    /// <para><b>Why this file exists:</b> the user reported "I don't
    /// see the visual changes." The original Pass 3 tests pinned
    /// invariants at the data layer (e.g., BurningEffect.Get..()
    /// returns "&amp;*R" → parses to HDR red). But "data is correct"
    /// doesn't prove "data reaches the screen." This file tests the
    /// chain.</para>
    ///
    /// <para>Bug-class probes (per ADVERSARIAL_TESTING.md):
    /// <list type="bullet">
    ///   <item>Blueprint loader recognizes new Part type names —
    ///         "LightSourceFlicker" → LightSourceFlickerPart resolved
    ///         + parameters applied</item>
    ///   <item>Status-effect color override flows through
    ///         <c>StatusEffectsPart.HandleRender</c> to the
    ///         GameEvent's ColorString param</item>
    ///   <item>End-to-end: a Burning entity's effective rendered color
    ///         (after the full Render-event chain) parses to HDR</item>
    ///   <item>Flicker on a factory-spawned entity actually modulates
    ///         intensity (not just on test-fabricated entities)</item>
    /// </list>
    /// </para>
    /// </summary>
    public class Pass3WiringAdversarialTests
    {
        // ── Helper: build a minimal EntityFactory with the Campfire blueprint ──

        private static EntityFactory BuildFactoryFromBlueprintsJson()
        {
            var factory = new EntityFactory();
            // Load the actual blueprints file — same path GameBootstrap uses.
            var blueprintsAsset = Resources.Load<TextAsset>("Content/Blueprints/Objects");
            Assert.IsNotNull(blueprintsAsset,
                "Test infra: Content/Blueprints/Objects.json must be loadable "
                + "via Resources.Load — same path production uses.");
            factory.LoadBlueprints(blueprintsAsset.text);
            return factory;
        }

        // ── 1. Blueprint loader recognizes LightSourceFlicker ────────────

        [Test]
        public void Adversarial_CampfireBlueprint_AttachesBothLightSourceAndFlicker()
        {
            // The Pass 3.A wire-up added "LightSourceFlicker" Part to
            // Campfire blueprint. If the EntityFactory's Part-name
            // resolver doesn't recognize the suffix-less name, the
            // flicker is silently dropped + no warning at runtime
            // (well, there IS a warning at EntityFactory.cs:211, but
            // a player wouldn't see it). Pin that the resolved entity
            // has BOTH parts.
            var factory = BuildFactoryFromBlueprintsJson();
            var campfire = factory.CreateEntity("Campfire");

            Assert.IsNotNull(campfire,
                "Campfire blueprint resolves to an entity at all.");
            var light = campfire.GetPart<LightSourcePart>();
            Assert.IsNotNull(light,
                "Campfire has a LightSourcePart (existed pre-Pass 3).");

            var flicker = campfire.GetPart<LightSourceFlickerPart>();
            Assert.IsNotNull(flicker,
                "Campfire has a LightSourceFlickerPart — the Pass 3.A "
                + "wire is reaching production. If this fails, the "
                + "blueprint loader's Part-name resolver did NOT match "
                + "'LightSourceFlicker' to LightSourceFlickerPart, "
                + "OR the parameter parse silently dropped the part.");
        }

        [Test]
        public void Adversarial_CampfireBlueprint_FlickerHasTunedParameters()
        {
            // The blueprint sets IntensityWobble=0.30, Speed=1.5 for
            // Campfire (the "dancing flame" preset). If parameter
            // parsing fails, the flicker uses its default 0.15+2.0 —
            // which would still flicker but not match the design.
            var factory = BuildFactoryFromBlueprintsJson();
            var campfire = factory.CreateEntity("Campfire");
            var flicker = campfire.GetPart<LightSourceFlickerPart>();
            Assert.IsNotNull(flicker, "Setup: flicker exists.");

            Assert.AreEqual(0.30f, flicker.IntensityWobble, 0.001f,
                "Campfire blueprint sets IntensityWobble=0.30 (dancing "
                + "flame). If this fails, parameter parsing dropped "
                + "the value.");
            Assert.AreEqual(1.5f, flicker.Speed, 0.001f,
                "Campfire blueprint sets Speed=1.5.");
        }

        [Test]
        public void Adversarial_TorchBlueprint_AttachesFlicker()
        {
            var factory = BuildFactoryFromBlueprintsJson();
            var torch = factory.CreateEntity("Torch");
            Assert.IsNotNull(torch, "Torch blueprint resolves.");
            Assert.IsNotNull(torch.GetPart<LightSourceFlickerPart>(),
                "Torch has LightSourceFlickerPart.");
        }

        [Test]
        public void Adversarial_WatchLanternBlueprint_AttachesFlicker()
        {
            var factory = BuildFactoryFromBlueprintsJson();
            var lantern = factory.CreateEntity("WatchLantern");
            Assert.IsNotNull(lantern, "WatchLantern blueprint resolves.");
            Assert.IsNotNull(lantern.GetPart<LightSourceFlickerPart>(),
                "WatchLantern has LightSourceFlickerPart.");
        }

        // ── 2. Flicker actually fires on a real factory-spawned entity ──

        [Test]
        public void Adversarial_FactorySpawnedCampfire_RenderEventChangesIntensity()
        {
            // Direct unit test of LightSourceFlickerPartTests covered
            // hand-fabricated entities. This test uses a REAL
            // factory-spawned Campfire — confirms the wiring all the
            // way through (blueprint → factory → entity → Render
            // event → flicker handler → LightSourcePart.Intensity).
            var factory = BuildFactoryFromBlueprintsJson();
            var campfire = factory.CreateEntity("Campfire");
            campfire.ID = "test-campfire-render-loop";

            var lightSource = campfire.GetPart<LightSourcePart>();
            Assert.IsNotNull(lightSource);
            float startIntensity = lightSource.Intensity;
            Assert.Greater(startIntensity, 0f,
                "Setup: LightSourcePart.Intensity is non-zero from "
                + "the blueprint (Campfire blueprint sets 0.8).");

            // Fire 30 Render events through the entity's event flow.
            // (The flicker uses Time.time which is non-deterministic
            // in tests — but across 30 frames at varying Time.time
            // values, intensity must shift at least once.)
            bool changed = false;
            for (int i = 0; i < 30 && !changed; i++)
            {
                var ev = GameEvent.New("Render");
                campfire.FireEventAndRelease(ev);
                if (System.Math.Abs(lightSource.Intensity - startIntensity) > 0.001f)
                    changed = true;
            }

            Assert.IsTrue(changed,
                "Across 30 Render events on a factory-spawned Campfire, "
                + "the LightSourcePart.Intensity must shift at least "
                + "once. If this fails, the flicker is NOT wired to "
                + "the production Render-event flow on real entities.");
        }

        // ── 3. Status-effect color override flows through Render event ──

        [Test]
        public void Adversarial_BurningEntity_RenderEvent_ColorStringSetToHdr()
        {
            // Apply BurningEffect to an entity. Fire a Render event
            // with an initial ColorString (e.g., the entity's normal
            // color "&Y"). Verify that StatusEffectsPart's HandleRender
            // overrides the ColorString param to "&*R" (the HDR
            // burning color). If this fails, the color override is
            // not flowing through to the Render-event chain at all.
            var entity = new Entity { ID = "burnable", BlueprintName = "Test" };
            entity.AddPart(new RenderPart
            {
                DisplayName = "test creature",
                ColorString = "&Y"
            });
            entity.AddPart(new StatusEffectsPart());

            var statusEffects = entity.GetPart<StatusEffectsPart>();
            statusEffects.ApplyEffect(new BurningEffect());

            // Fire a Render event with the entity's base color.
            var ev = GameEvent.New("Render");
            ev.SetParameter("ColorString", "&Y");
            entity.FireEvent(ev);

            string finalColor = ev.GetStringParameter("ColorString", null);
            Assert.AreEqual("&*R", finalColor,
                "After Render event flow, ColorString must reflect "
                + "BurningEffect's HDR override. If this is '&Y' (the "
                + "base) or '&R' (the old SDR), the override didn't fire.");

            // End-to-end: parse the resulting color and confirm it's HDR.
            Color rendered = QudColorParser.Parse(finalColor);
            Assert.Greater(rendered.r, 1.05f,
                "Parsed color exceeds bloom threshold (1.05). End-to-"
                + "end: BurningEffect → Render event → HDR color → "
                + "(would bloom in URP).");
        }

        [Test]
        public void Adversarial_AcidicEntity_RenderEvent_ColorStringSetToHdr()
        {
            var entity = new Entity { ID = "acidic", BlueprintName = "Test" };
            entity.AddPart(new RenderPart { ColorString = "&y" });
            entity.AddPart(new StatusEffectsPart());
            entity.GetPart<StatusEffectsPart>().ApplyEffect(new AcidicEffect());

            var ev = GameEvent.New("Render").SetParameter("ColorString", "&y");
            entity.FireEvent(ev);

            string finalColor = ev.GetStringParameter("ColorString", null);
            Assert.AreEqual("&*G", finalColor,
                "Acidic override flows through to ColorString param.");
            Color c = QudColorParser.Parse(finalColor);
            Assert.Greater(c.g, 1.05f,
                "Parsed acidic color exceeds bloom threshold on green channel.");
        }

        // ── 4. The blueprint-loader Part-name resolver actually finds the new type

        [Test]
        public void Adversarial_EntityFactory_RegistersLightSourceFlickerPart()
        {
            // Probe the registry directly via reflection. If
            // LightSourceFlickerPart is missing from _partTypes, the
            // blueprint loader silently drops the part with a warning.
            var factory = new EntityFactory();
            var partTypesField = typeof(EntityFactory).GetField(
                "_partTypes", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(partTypesField,
                "Test infra: EntityFactory._partTypes field exists for "
                + "introspection. If this fails, the field was renamed.");

            var partTypes = (Dictionary<string, System.Type>)partTypesField.GetValue(factory);
            Assert.IsTrue(partTypes.ContainsKey("LightSourceFlickerPart"),
                "EntityFactory's auto-scan registered "
                + "LightSourceFlickerPart by class name. If this "
                + "fails, the assembly-scan path is broken or the "
                + "Part class is in a different assembly.");
        }

        // ── 5. HDR Color survives passing through Tilemap.SetColor ──────

        [Test]
        public void Adversarial_UnityColor_AllowsRgbAboveOne()
        {
            // The most basic adversarial: does Unity even let you
            // construct a Color with RGB > 1.0? If this fails, the
            // entire HDR plan is broken at the language level.
            var hdr = new Color(2.4f, 0.4f, 0.1f);
            Assert.AreEqual(2.4f, hdr.r, 0.001f);
            Assert.AreEqual(0.4f, hdr.g, 0.001f);
            Assert.AreEqual(0.1f, hdr.b, 0.001f);
        }

        [Test]
        public void Adversarial_QudColorParser_AllConvertedHdrCodes_ExceedBloomThreshold()
        {
            // For each HDR code (R, G, Y, C, M), the parsed color
            // should exceed the URP Bloom threshold (1.05) on at
            // least its primary channel.
            const float BLOOM_THRESHOLD = 1.05f;

            Assert.Greater(QudColorParser.Parse("&*R").r, BLOOM_THRESHOLD,
                "&*R red channel > bloom threshold.");
            Assert.Greater(QudColorParser.Parse("&*G").g, BLOOM_THRESHOLD,
                "&*G green channel > bloom threshold.");
            Assert.Greater(QudColorParser.Parse("&*Y").r, BLOOM_THRESHOLD,
                "&*Y red channel > bloom threshold.");
            Assert.Greater(QudColorParser.Parse("&*Y").g, BLOOM_THRESHOLD,
                "&*Y green channel > bloom threshold.");
            Assert.Greater(QudColorParser.Parse("&*C").b, BLOOM_THRESHOLD,
                "&*C blue channel > bloom threshold.");
            Assert.Greater(QudColorParser.Parse("&*M").r, BLOOM_THRESHOLD,
                "&*M red channel > bloom threshold.");
        }

        // ── 6. Sanity: SDR Burning code from the OLD effect file would NOT bloom ──

        [Test]
        public void Adversarial_OldSdrBurningCode_WouldNotBloom_RegressionGuard()
        {
            // Counter-check: pre-Pass-3, BurningEffect returned "&R"
            // (SDR). Confirm that &R parses to a value BELOW the
            // bloom threshold — proving the HDR upgrade is what
            // unlocks bloom.
            const float BLOOM_THRESHOLD = 1.05f;
            Color sdr = QudColorParser.Parse("&R");
            Assert.LessOrEqual(sdr.r, BLOOM_THRESHOLD,
                "Pre-Pass-3 SDR &R red channel STAYS BELOW bloom "
                + "threshold. If this fails, the threshold is too "
                + "low + the SDR-everything-blooms problem from the "
                + "Pass 1 plan is back.");
        }
    }
}
