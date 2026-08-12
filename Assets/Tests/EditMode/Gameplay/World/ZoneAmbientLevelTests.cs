using CavesOfOoo.Core;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// W0.2 of the Felling world overhaul
    /// (<c>Docs/FELLING-IMPLEMENTATION-PLAN.md</c> §2) — ambient light
    /// becomes a property of the PLACE, not of the renderer.
    ///
    /// <para>The canon needs this before any of the underground can
    /// ship: a catacomb village lit by one cultivated fungal patch and
    /// a dead zone of true dark cannot both be "ambient 0.4". Light is
    /// the underground's terrain (Felling design §6), and terrain lives
    /// on the Zone.</para>
    ///
    /// <para><b>Two of these tests fail against the code as it stands</b>
    /// — they pin bugs the verification sweep found (plan §1, C3):</para>
    /// <list type="number">
    /// <item>The LightMap's cache key is EntityVersion + EquipmentVersion
    /// only. Change a zone's ambient and NOTHING repaints until some
    /// creature happens to move — which would have made the whole
    /// catacomb light-economy silently inert.</item>
    /// <item>The LightMap is never reset when the renderer switches
    /// zones. Two zones whose EntityVersions happen to match render each
    /// other's light. Latent today (every zone shares 0.4); a
    /// showstopper the moment ambient varies per place.</item>
    /// </list>
    ///
    /// <para>W0.2 ships NO visual change — every zone is still 0.4. This
    /// is the plumbing, verified, before the phases that use it.</para>
    /// </summary>
    public class ZoneAmbientLevelTests
    {
        private static Zone MakeZone(string id, float ambient)
        {
            var zone = new Zone { ZoneID = id, AmbientLevel = ambient };
            return zone;
        }

        // ════════════════════════════════════════════════════════
        // The field itself
        // ════════════════════════════════════════════════════════

        [Test]
        public void NewZone_DefaultsToTheSharedAmbientConstant()
        {
            // Villages, interiors and every test fixture never pass
            // through OnZoneGenerated (it runs only at generation, and
            // only for Overworld IDs), so the FIELD DEFAULT is what
            // most zones in the game actually use. It must equal the
            // brightness the renderer has always assumed.
            var zone = new Zone();
            Assert.AreEqual(Zone.DefaultAmbientLevel, zone.AmbientLevel);
            Assert.AreEqual(0.4f, Zone.DefaultAmbientLevel, 1e-6f,
                "changing this silently re-lights every un-authored zone");
        }

        // ════════════════════════════════════════════════════════
        // LightMap reads the zone
        // ════════════════════════════════════════════════════════

        [Test]
        public void Compute_UnlitCell_UsesTheZonesAmbientLevel()
        {
            var dark = MakeZone("Overworld.1.1.4", 0.12f);
            var map = new LightMap();
            map.Compute(dark);

            Assert.AreEqual(0.12f, map.GetBrightness(10, 10), 1e-4f,
                "an unlit cell should be as dark as its zone says");
        }

        [Test]
        public void Compute_BrightAndDarkZones_Differ()
        {
            // Counter-check for the above: if GetBrightness ignored the
            // zone entirely, both would read the same and the test above
            // would pass only by coincidence of the default.
            var bright = MakeZone("Overworld.1.1.0", 0.40f);
            var dark = MakeZone("Overworld.2.2.6", 0.05f);

            var a = new LightMap();
            a.Compute(bright);
            var b = new LightMap();
            b.Compute(dark);

            Assert.Greater(a.GetBrightness(5, 5), b.GetBrightness(5, 5));
        }

        [Test]
        public void GetBrightness_OutOfBounds_ReturnsTheZonesAmbient()
        {
            // Off-map queries must not report the old renderer-global
            // default — callers use this as "what is it like out there".
            var dark = MakeZone("Overworld.3.3.9", 0.08f);
            var map = new LightMap();
            map.Compute(dark);

            Assert.AreEqual(0.08f, map.GetBrightness(-1, -1), 1e-4f);
            Assert.AreEqual(0.08f, map.GetBrightness(Zone.Width, 0), 1e-4f);
        }

        [Test]
        public void AmbientLevelProperty_ReflectsTheLastComputedZone()
        {
            // ZoneRenderer's mote-spawn gate reads lightMap.AmbientLevel
            // and compares it against GetBrightness. The two must agree
            // about which zone they are describing.
            var map = new LightMap();
            map.Compute(MakeZone("Overworld.4.4.2", 0.2f));

            Assert.AreEqual(0.2f, map.AmbientLevel, 1e-4f);
            Assert.AreEqual(map.AmbientLevel, map.GetBrightness(3, 3), 1e-4f);
        }

        // ════════════════════════════════════════════════════════
        // Cache invalidation — the two latent bugs
        // ════════════════════════════════════════════════════════

        [Test]
        public void Compute_AmbientChangedAlone_Repaints()
        {
            // RED against current code. Nothing moves, nothing equips —
            // only the hour changes, or the Patch-Tender dims the
            // village. The light must follow.
            var zone = MakeZone("Overworld.5.5.3", 0.40f);
            var map = new LightMap();
            map.Compute(zone);
            Assert.AreEqual(0.40f, map.GetBrightness(6, 6), 1e-4f);

            zone.AmbientLevel = 0.10f;
            map.Compute(zone);

            Assert.AreEqual(0.10f, map.GetBrightness(6, 6), 1e-4f,
                "ambient changed but the LightMap kept its cached array");
        }

        [Test]
        public void Compute_TintChangedAlone_Repaints()
        {
            var zone = MakeZone("Overworld.5.6.0", 0.4f);
            zone.AmbientTint = Color.white;
            var map = new LightMap();
            map.Compute(zone);

            zone.AmbientTint = new Color(1f, 0.5f, 0.5f);
            map.Compute(zone);

            Assert.AreEqual(0.5f, map.GetTint(4, 4).g, 1e-4f,
                "tint changed but the LightMap kept its cached array");
        }

        [Test]
        public void Compute_DifferentZoneSameEntityVersion_DoesNotReuseTheOldLight()
        {
            // RED against current code. The renderer keeps ONE LightMap
            // across zone transitions. Two freshly-built zones both have
            // EntityVersion 0, so the second Compute early-outs and the
            // player walks into a catacomb still lit like the meadow
            // they left.
            var meadow = MakeZone("Overworld.7.7.0", 0.40f);
            var catacomb = MakeZone("Overworld.7.7.3", 0.12f);
            Assert.AreEqual(meadow.EntityVersion, catacomb.EntityVersion,
                "fixture precondition: the collision this test is about");

            var map = new LightMap();
            map.Compute(meadow);
            map.Compute(catacomb);

            Assert.AreEqual(0.12f, map.GetBrightness(8, 8), 1e-4f,
                "the LightMap rendered the previous zone's brightness");
        }

        [Test]
        public void Compute_NothingChanged_StillEarlyOuts()
        {
            // Counter-check on the cache fix: widening the key must not
            // turn every frame into a full recompute — Compute is
            // O(W×H + sources×r²) and the day-cycle would call it every
            // turn.
            //
            // Proving an early-out needs a change the cache CANNOT see.
            // Mutating a light-source Part in place is exactly that: the
            // zone's EntityVersion doesn't move, no equipment event
            // fires, ambient is untouched. If the early-out is gone, the
            // second Compute reads Radius 0 and the cell falls to
            // ambient; if it holds, the cell keeps its stale light.
            var zone = MakeZone("Overworld.9.9.0", 0.4f);
            var lamp = new Entity { ID = "lamp" };
            var light = new LightSourcePart { Radius = 5, Intensity = 1f };
            lamp.AddPart(light);
            zone.AddEntity(lamp, 9, 9);

            var map = new LightMap();
            map.Compute(zone);
            float lit = map.GetBrightness(9, 9);
            Assert.Greater(lit, zone.AmbientLevel,
                "fixture precondition: the lamp actually lights its cell");

            light.Radius = 0;
            map.Compute(zone);

            Assert.AreEqual(lit, map.GetBrightness(9, 9), 1e-6f,
                "the cache key was widened too far — this recomputed "
                + "when none of its inputs had changed");
        }
    }
}
