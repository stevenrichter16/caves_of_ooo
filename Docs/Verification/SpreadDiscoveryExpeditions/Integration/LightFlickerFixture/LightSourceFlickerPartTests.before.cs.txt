using NUnit.Framework;
using CavesOfOoo.Core;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// 3.A.3 — Tests for LightSourceFlickerPart. See
    /// <c>Docs/GRAPHICS.md</c> §3.A for the Pass 3 §3.A plan.
    ///
    /// <para>Pins: a flicker Part attached to an entity with a
    /// LightSourcePart modulates that LightSourcePart's Intensity
    /// per Render event using deterministic Perlin noise hashed from
    /// the entity ID. This gives lanterns/torches/campfires a "living
    /// flame" feel without needing per-Light2D AnimationClips and
    /// without breaking determinism (same entity ID → same flicker
    /// trajectory across reloads).</para>
    ///
    /// <para>Test surface:
    /// <list type="bullet">
    ///   <item>Without LightSourcePart → no-op (defensive).</item>
    ///   <item>With LightSourcePart → Intensity changes when
    ///         <c>UpdateIntensityAt(time)</c> is called.</item>
    ///   <item>Same entity ID → same phase offset (determinism).</item>
    ///   <item>Different IDs → different phase offsets (desync).</item>
    ///   <item>Wobble bounded by IntensityWobble setting.</item>
    /// </list>
    /// </para>
    /// </summary>
    public class LightSourceFlickerPartTests
    {
        // ── A. Defensive: no LightSourcePart → no crash ─────────────────

        [Test]
        public void UpdateIntensityAt_WithoutLightSourcePart_NoOp()
        {
            var entity = new Entity { ID = "e1", BlueprintName = "TestEntity" };
            entity.AddPart(new LightSourceFlickerPart());
            var flicker = entity.GetPart<LightSourceFlickerPart>();
            Assert.DoesNotThrow(() => flicker.UpdateIntensityAt(0.5f),
                "No LightSourcePart on entity → flicker is a no-op, "
                + "doesn't throw.");
        }

        // ── B. With LightSourcePart → modulates intensity ───────────────

        [Test]
        public void UpdateIntensityAt_WithLightSource_ModulatesIntensity()
        {
            // Setup: entity with LightSourcePart.Intensity=1.0 + flicker.
            // After UpdateIntensityAt(t) for several different t values,
            // at least one must produce a different Intensity than 1.0.
            var entity = new Entity { ID = "torch1", BlueprintName = "Torch" };
            entity.AddPart(new LightSourcePart { Intensity = 1.0f });
            entity.AddPart(new LightSourceFlickerPart
            {
                IntensityWobble = 0.30f, // strong so noise crosses threshold
                Speed = 1.5f
            });
            var flicker = entity.GetPart<LightSourceFlickerPart>();
            var lightSource = entity.GetPart<LightSourcePart>();

            bool changed = false;
            for (int i = 0; i < 30 && !changed; i++)
            {
                flicker.UpdateIntensityAt(i * 0.1f);
                if (System.Math.Abs(lightSource.Intensity - 1.0f) > 0.001f)
                    changed = true;
            }

            Assert.IsTrue(changed,
                "Across 30 sample times, flicker must have produced "
                + "an Intensity that differs from the base (1.0). If "
                + "this fails, either the Perlin sampler isn't "
                + "advancing or the wobble multiplier is broken.");
        }

        // ── C. Determinism: same entity ID → same trajectory ────────────

        [Test]
        public void UpdateIntensityAt_SameEntityId_SameIntensity()
        {
            // Counter-check: phase offset is hashed from entity ID, so
            // two flicker Parts on entities with the same ID produce
            // the same Intensity at the same `t`. (Same ID is unusual
            // in practice but the determinism contract is what gives
            // saves cross-version-stability.)
            var e1 = new Entity { ID = "shared-id", BlueprintName = "T" };
            e1.AddPart(new LightSourcePart { Intensity = 1.0f });
            e1.AddPart(new LightSourceFlickerPart
            {
                IntensityWobble = 0.20f, Speed = 1.0f
            });
            var f1 = e1.GetPart<LightSourceFlickerPart>();

            var e2 = new Entity { ID = "shared-id", BlueprintName = "T" };
            e2.AddPart(new LightSourcePart { Intensity = 1.0f });
            e2.AddPart(new LightSourceFlickerPart
            {
                IntensityWobble = 0.20f, Speed = 1.0f
            });
            var f2 = e2.GetPart<LightSourceFlickerPart>();

            f1.UpdateIntensityAt(2.5f);
            f2.UpdateIntensityAt(2.5f);

            Assert.AreEqual(
                e1.GetPart<LightSourcePart>().Intensity,
                e2.GetPart<LightSourcePart>().Intensity,
                1e-5f,
                "Same entity ID + same time → same Intensity. "
                + "Determinism contract.");
        }

        [Test]
        public void UpdateIntensityAt_DifferentEntityIds_DifferentPhaseOffsets()
        {
            // Counter-check: two torches with DIFFERENT IDs at the same
            // time `t` must produce different intensities (otherwise
            // they'd flicker in lock-step, which looks fake). Probe
            // across a few `t` values to avoid false negatives at the
            // exact crossover point.
            var e1 = new Entity { ID = "torch_alpha", BlueprintName = "T" };
            e1.AddPart(new LightSourcePart { Intensity = 1.0f });
            e1.AddPart(new LightSourceFlickerPart
            {
                IntensityWobble = 0.30f, Speed = 1.5f
            });
            var f1 = e1.GetPart<LightSourceFlickerPart>();

            var e2 = new Entity { ID = "torch_beta", BlueprintName = "T" };
            e2.AddPart(new LightSourcePart { Intensity = 1.0f });
            e2.AddPart(new LightSourceFlickerPart
            {
                IntensityWobble = 0.30f, Speed = 1.5f
            });
            var f2 = e2.GetPart<LightSourceFlickerPart>();

            bool foundDifference = false;
            for (int i = 0; i < 10 && !foundDifference; i++)
            {
                f1.UpdateIntensityAt(i * 0.3f);
                f2.UpdateIntensityAt(i * 0.3f);
                float i1 = e1.GetPart<LightSourcePart>().Intensity;
                float i2 = e2.GetPart<LightSourcePart>().Intensity;
                if (System.Math.Abs(i1 - i2) > 0.005f)
                    foundDifference = true;
            }

            Assert.IsTrue(foundDifference,
                "Different entity IDs must produce different phase "
                + "offsets, so neighboring torches don't flicker in "
                + "lock-step. Across 10 sample times, at least one "
                + "must show divergence.");
        }

        // ── D. Wobble bounded by IntensityWobble setting ────────────────

        [Test]
        public void UpdateIntensityAt_IntensityStaysWithinWobbleBound()
        {
            // The Intensity should never exceed BaseIntensity * (1 +
            // IntensityWobble) or fall below BaseIntensity * (1 -
            // IntensityWobble). Probe many sample points to bracket
            // the noise range.
            const float BASE = 1.0f;
            const float WOBBLE = 0.10f;
            var entity = new Entity { ID = "torch3", BlueprintName = "T" };
            entity.AddPart(new LightSourcePart { Intensity = BASE });
            entity.AddPart(new LightSourceFlickerPart
            {
                IntensityWobble = WOBBLE, Speed = 1.0f
            });
            var flicker = entity.GetPart<LightSourceFlickerPart>();
            var lightSource = entity.GetPart<LightSourcePart>();

            float upper = BASE * (1f + WOBBLE) + 0.001f; // tiny epsilon
            float lower = BASE * (1f - WOBBLE) - 0.001f;
            for (int i = 0; i < 50; i++)
            {
                flicker.UpdateIntensityAt(i * 0.07f);
                Assert.LessOrEqual(lightSource.Intensity, upper,
                    $"Intensity {lightSource.Intensity} > upper bound "
                    + $"{upper} at t={i * 0.07f}.");
                Assert.GreaterOrEqual(lightSource.Intensity, lower,
                    $"Intensity {lightSource.Intensity} < lower bound "
                    + $"{lower} at t={i * 0.07f}.");
            }
        }

        // ── E. Without flicker Part, intensity stays at base ────────────

        [Test]
        public void Adversarial_LightSourceWithoutFlickerPart_IntensityNeverChanges()
        {
            // Counter-check: an entity with LightSourcePart but NO
            // flicker Part has its Intensity preserved across any
            // number of Render events. If this fails, something
            // ELSE in the code is mutating Intensity.
            var entity = new Entity { ID = "static_lantern", BlueprintName = "T" };
            entity.AddPart(new LightSourcePart { Intensity = 0.7f });

            // Fire a Render event; LightSourcePart.HandleEvent is
            // a no-op. Intensity must stay at 0.7.
            var ev = GameEvent.New("Render");
            entity.GetPart<LightSourcePart>().HandleEvent(ev);

            Assert.AreEqual(0.7f,
                entity.GetPart<LightSourcePart>().Intensity,
                1e-5f,
                "Without flicker, LightSourcePart.Intensity is "
                + "static across Render events.");
        }

        // ── F. Render event hook fires UpdateIntensityAt ───────────────

        [Test]
        public void HandleEvent_RenderFires_UpdatesIntensity()
        {
            // Pin that the Render event handler calls into the flicker
            // logic, NOT just that UpdateIntensityAt works in isolation.
            // Without this test, a future change that severs the
            // Render→UpdateIntensity wiring would silently leave
            // production lights static.
            var entity = new Entity { ID = "torch_render", BlueprintName = "T" };
            entity.AddPart(new LightSourcePart { Intensity = 1.0f });
            entity.AddPart(new LightSourceFlickerPart
            {
                IntensityWobble = 0.30f, Speed = 1.5f
            });
            var flicker = entity.GetPart<LightSourceFlickerPart>();
            var lightSource = entity.GetPart<LightSourcePart>();

            // Fire 30 Render events; at least one must shift Intensity
            // away from 1.0.
            bool changed = false;
            for (int i = 0; i < 30 && !changed; i++)
            {
                var ev = GameEvent.New("Render");
                flicker.HandleEvent(ev);
                if (System.Math.Abs(lightSource.Intensity - 1.0f) > 0.001f)
                    changed = true;
            }

            Assert.IsTrue(changed,
                "Render event handler must drive flicker. Across 30 "
                + "Render events, at least one must shift Intensity "
                + "away from base.");
        }
    }
}
