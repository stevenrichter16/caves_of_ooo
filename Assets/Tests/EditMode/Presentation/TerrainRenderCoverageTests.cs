using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using Application = UnityEngine.Application;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Terrain that has neither a ground material nor a sprite renders as a
    /// bare CP437 glyph sitting on the flowing ground field — which reads,
    /// on screen, as a wave of ASCII characters running through the terrain.
    ///
    /// <para><b>This is a silent regression surface.</b>
    /// <c>EnvironmentSpriteRenderer.ResolveGroundMaterial</c> is a hardcoded
    /// blueprint-name switch. Every new piece of terrain content defaults to
    /// <c>GroundMaterial.None</c> and nothing warns anybody — the visual
    /// simply degrades, one blueprint at a time, and is only noticed when a
    /// human looks at the screen. It was noticed exactly that way.</para>
    ///
    /// <para>This test does not demand that all terrain be ground. Plenty of
    /// it legitimately stays a glyph — stairs must read as stairs, a
    /// campfire is an object. What it demands is that the decision be
    /// <b>deliberate</b>: every terrain blueprint is either mapped to a
    /// ground material, or listed below as knowingly glyph-only. Adding
    /// terrain without doing one or the other fails here.</para>
    /// </summary>
    public class TerrainRenderCoverageTests
    {
        private static EntityFactory _factory;
        private static List<string> _terrainBlueprints;

        /// <summary>
        /// Terrain that is deliberately drawn as a glyph rather than as a
        /// ground surface.
        ///
        /// <para>Two honest categories here. Some entries are OBJECTS that
        /// merely inherit Terrain for its render layer — stairs, campfires,
        /// the invisible placement markers. Those are correct as glyphs.
        /// </para>
        ///
        /// <para>The rest are marked ⚠ — they are features standing ON
        /// ground (crops, flowers, pipes, bogs) that want real 16×16 sprite
        /// art. Mapping them to a ground material would erase them: the
        /// renderer claims the cell for the ground tile and the glyph goes
        /// away, so a flower meadow would become a lawn. They are listed
        /// here so the debt is recorded rather than forgotten.</para>
        /// </summary>
        private static readonly HashSet<string> GlyphOnlyByDesign = new HashSet<string>
        {
            // Objects that merely sit on the terrain layer.
            "StairsDown", "StairsUp", "Campfire", "BrokenColumn", "Bush",
            "CampfireGroundMarker", "LanternGroundMarker",
            "OvenGroundMarker", "WellGroundMarker",

            // Vents, clouds and railings are features standing on the
            // ground, not the ground itself.
            "SteamCloud", "SteamVent", "FrostVent", "RustedRailing", "DryBrush",

            // ⚠ Want sprite art. Glyph-only until it exists. Every one of
            // these is a surface the player walks on, so each is currently
            // a CP437 character laid across the flowing ground field —
            // which is exactly the reported symptom. They are NOT mapped to
            // a ground material because that would erase them: the renderer
            // claims the cell for the ground tile and the glyph disappears,
            // so a flower meadow would become a lawn and a tar seep would
            // become clean stone. They need their own 16×16 tiles.
            "CharmFlowers", "FlowerField", "Reeds",
            "SaltCrust", "DuneCrest", "Bones", "TentWall", "UntendedFire",
            "MirePool", "Duckboard", "DeadTree", "PeatBank",
            "MycelialColumn", "GroveSeep", "FruitingBody",
                        "BloomingFruitingBody", // W4.4 SM-D — the front's growth, same glyph family as FruitingBody
            "CopperPipe", "PeatBog", "TarSeep", "OilSeep", "OilSlick",
            "BrinePool", "AcidPool", "AcidPond", "IceSheet",
            "ConvalescencePool", "MemoryBathPool", "MirrorMucilagePool",
        };

        [OneTimeSetUp]
        public void LoadOnce()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(
                Application.dataPath, "Resources/Content/Blueprints/Objects.json")));

            _terrainBlueprints = new List<string>();
            foreach (var name in _factory.Blueprints.Keys)
            {
                if (name == "Terrain") continue;
                Entity e;
                try { e = _factory.CreateEntity(name); }
                catch (System.Exception) { continue; }
                if (e == null || !e.HasTag("Terrain")) continue;
                _terrainBlueprints.Add(name);
            }
        }

        [Test]
        public void ThereIsTerrainToAudit()
        {
            // Guards the guard: if the tag query silently stopped matching,
            // every assertion below would pass over an empty set.
            Assert.Greater(_terrainBlueprints.Count, 10,
                "the terrain census came back suspiciously small");
        }

        [Test]
        public void EveryTerrainBlueprintIsEitherGroundOrDeliberatelyAGlyph()
        {
            var unaccounted = new List<string>();
            foreach (var name in _terrainBlueprints)
            {
                if (GlyphOnlyByDesign.Contains(name)) continue;
                if (EnvironmentSpriteRenderer.ResolveGroundMaterial(name)
                    != EnvironmentSpriteRenderer.GroundMaterial.None) continue;
                // A blueprint-keyed fixture sprite is the THIRD honest
                // answer, and the guard did not know about it: terrain
                // with real art was still forced onto the debt list,
                // which made the list read as bigger than the debt.
                if (HasFixtureSprite(name)) continue;
                unaccounted.Add(name);
            }

            CollectionAssert.IsEmpty(unaccounted,
                "These terrain blueprints resolve no ground material and are not "
                + "listed as glyph-only, so they will render as bare ASCII over the "
                + "flowing ground field. Either map them in "
                + "EnvironmentSpriteRenderer.ResolveGroundMaterial or add them to "
                + "GlyphOnlyByDesign with a reason: " + string.Join(", ", unaccounted));
        }

        private static bool HasFixtureSprite(string blueprint)
        {
            foreach (var (bp, _) in EnvironmentSpriteRenderer.FixtureSprites)
                if (bp == blueprint) return true;
            return false;
        }

        [Test]
        public void TheVerticalWorldHasArt_NotJustLetters()
        {
            // The W5 content ships with real 16x16 sprites rather than
            // joining the "wants sprite art" debt list. Each entry
            // needs BOTH halves: a blueprint->file mapping, and a file
            // that actually loads.
            foreach (var bp in new[] { "SinkholeLip", "DescentLedge", "RopeAnchor",
                                       "HearthPatch", "NicheHome", "DroseraRing",
                                       "BeetleJar", "PebbleSundewThreshold",
                                       "PlaqueWall", "PlaqueSmoothed" })
            {
                Assert.IsTrue(HasFixtureSprite(bp), bp + " is mapped to art");
                string file = null;
                foreach (var (b, f) in EnvironmentSpriteRenderer.FixtureSprites)
                    if (b == bp) file = f;
                Assert.IsNotNull(Resources.Load<Sprite>("Sprites/Environment/" + file),
                    bp + " maps to " + file + ", which must actually load");
            }
        }

        [Test]
        public void TheSmoothedNiche_HasItsOwnArt()
        {
            // It must NOT share the plaque slab: the entire point of
            // the niche is that nothing is cut into it.
            string wall = null, smoothed = null;
            foreach (var (b, f) in EnvironmentSpriteRenderer.FixtureSprites)
            {
                if (b == "PlaqueWall") wall = f;
                if (b == "PlaqueSmoothed") smoothed = f;
            }
            Assert.AreNotEqual(wall, smoothed,
                "the absence is the content; it cannot wear the same face");
        }

        [Test]
        public void TheExemptionListDoesNotNameThingsThatNoLongerExist()
        {
            // A stale exemption is a hole in the guard: a blueprint could be
            // renamed, lose its mapping, and stay silently excused.
            var ghosts = new List<string>();
            foreach (var name in GlyphOnlyByDesign)
                if (!_factory.Blueprints.ContainsKey(name)) ghosts.Add(name);

            CollectionAssert.IsEmpty(ghosts,
                "GlyphOnlyByDesign names blueprints that do not exist: "
                + string.Join(", ", ghosts));
        }

        [Test]
        public void TheRoadIsGround()
        {
            // The specific fix: a packed road is a surface, not an object
            // standing on one.
            Assert.AreEqual(EnvironmentSpriteRenderer.GroundMaterial.Floor,
                EnvironmentSpriteRenderer.ResolveGroundMaterial("RoadStone"));
        }

        [Test]
        public void GrassIsStillGrass()
        {
            // Counter-check: the mapping edit must not have disturbed the
            // entries that already worked.
            Assert.AreEqual(EnvironmentSpriteRenderer.GroundMaterial.Grass,
                EnvironmentSpriteRenderer.ResolveGroundMaterial("Grass"));
            Assert.AreEqual(EnvironmentSpriteRenderer.GroundMaterial.Water,
                EnvironmentSpriteRenderer.ResolveGroundMaterial("WaterPuddle"));
            Assert.AreEqual(EnvironmentSpriteRenderer.GroundMaterial.None,
                EnvironmentSpriteRenderer.ResolveGroundMaterial("NotARealBlueprint"));
        }

        [TestCase("CropRow",EnvironmentSpriteRenderer.CropSpriteKind.Seed,"crop_seed")]
        [TestCase("RipeCropRow",EnvironmentSpriteRenderer.CropSpriteKind.Emberwheat,"emberwheat_crop")]
        public void FieldRowsKeepGroundCoverageAndTheirDistinctRealCropSprites(string blueprint,EnvironmentSpriteRenderer.CropSpriteKind kind,string sprite)
        {
            Assert.AreEqual(EnvironmentSpriteRenderer.GroundMaterial.Grass,EnvironmentSpriteRenderer.ResolveGroundMaterial(blueprint));
            Assert.AreEqual(kind,EnvironmentSpriteRenderer.ResolveFieldCropKind(_factory.CreateEntity(blueprint)));
            Assert.NotNull(Resources.Load<Sprite>("Sprites/Environment/"+sprite));
            Assert.IsFalse(GlyphOnlyByDesign.Contains(blueprint),"Authored crop art is not a glyph-only debt exception.");
            Assert.AreEqual(EnvironmentSpriteRenderer.CropSpriteKind.None,EnvironmentSpriteRenderer.ResolveFieldCropKind(_factory.CreateEntity("Grass")));
        }

        // ════════════════════════════════════════════════════════
        // Readability at ambient light
        // ════════════════════════════════════════════════════════

        [Test]
        public void StandingPlantsAreNotNearlyBlackAtAmbientLight()
        {
            // The hedgerow bug. A surface zone sits at ambient 0.4, and the
            // final colour is the entity's colour scaled by light — so a
            // DarkGreen (0, 0.67, 0) plant lands at (0, 0.27, 0), which
            // reads as black until the player's own light reaches it.
            // Tree and Bush have always used BrightGreen; the hedge was the
            // odd one out.
            foreach (string name in new[] { "Hedge", "Tree", "Bush" })
            {
                var render = _factory.CreateEntity(name).GetPart<RenderPart>();
                Color lit = QudColorParser.Parse(render.ColorString)
                            * Zone.DefaultAmbientLevel;
                Assert.Greater(lit.maxColorComponent, 0.3f,
                    $"{name} is nearly black at ambient light (ColorString {render.ColorString})");
            }
        }
    }
}
