using UnityEngine;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Modulates a sibling <see cref="LightSourcePart"/>'s
    /// <see cref="LightSourcePart.Intensity"/> using deterministic
    /// Perlin noise so torches/lanterns/campfires "breathe" rather
    /// than emit a flat LED-like glow.
    ///
    /// <para><b>Mirrors <see cref="CavesOfOoo.Presentation.Rendering.Light2DFlicker"/></b>
    /// (Pass 2) but operates on the project's actual lighting model
    /// — the <c>LightSourcePart</c>-driven software lightmap in
    /// <c>LightMap.Compute</c>. Drop this Part on any entity that
    /// also has <c>LightSourcePart</c> and the next <c>Render</c>
    /// event will start modulating intensity.</para>
    ///
    /// <para><b>Determinism:</b> the per-instance phase offset is
    /// hashed from <see cref="Entity.ID"/>, so the same entity
    /// flickers the same way across reloads. Different IDs produce
    /// different offsets, so neighboring lights don't flicker in
    /// lock-step.</para>
    ///
    /// <para><b>Save/load:</b> public fields (IntensityWobble, Speed)
    /// round-trip via the Tier-3 reflection serializer. Private
    /// state (cached base intensity, cached phase) resets on load
    /// — pinned by SL.5's "private state resets" contract — and
    /// rebuilds lazily on the first post-load Render event. This
    /// is correct: a fresh trajectory starts after each save reload.</para>
    ///
    /// <para><b>Performance:</b> one Perlin sample per Render event
    /// per flicker-equipped entity. Render fires per-frame in
    /// <c>ZoneRenderer.cs:774</c>; for ~10 lit entities visible at
    /// once, this is &lt; 0.01ms total.</para>
    /// </summary>
    public class LightSourceFlickerPart : Part
    {
        public override string Name => "LightSourceFlicker";

        /// <summary>
        /// How much to wobble the light's intensity, as a fraction of
        /// the base value. 0.15 = ±15%. Tune per blueprint:
        /// candles/torches 0.20 (visible flicker), lanterns 0.08
        /// (subtle), campfires 0.30 (dancing flame).
        /// </summary>
        public float IntensityWobble = 0.15f;

        /// <summary>
        /// Flicker rate. Higher = faster wobble. 1.5-2.5 ≈ candle/torch;
        /// 4.0+ ≈ rapid sparking; 0.5-1.0 ≈ embers.
        /// </summary>
        public float Speed = 2.0f;

        // ── Private state (resets on save-load — see SL.5) ──────────────

        // -1 sentinel = "not yet initialized." Lazy init the first time
        // UpdateIntensityAt is called, since ParentEntity.ID isn't
        // available in the constructor (set by AddPart later).
        private float _phaseOffset = -1f;

        // Captured on first call so subsequent calls have a stable base
        // to multiply against. If a downstream system later changes the
        // base intensity (e.g., a "dim the lantern" effect), the flicker
        // doesn't notice — that's a feature: the flicker is a small
        // per-frame perturbation around whatever the gameplay logic set.
        private float _baseIntensity = -1f;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "Render")
            {
                UpdateIntensityAt(Time.time);
            }
            return true;
        }

        /// <summary>
        /// Drive the flicker at a specific time. Public for unit-test
        /// access (production calls from <see cref="HandleEvent"/>
        /// pass <c>Time.time</c>).
        /// </summary>
        public void UpdateIntensityAt(float time)
        {
            var lightSource = ParentEntity?.GetPart<LightSourcePart>();
            if (lightSource == null) return;

            if (_phaseOffset < 0f)
            {
                // Hash entity ID into a [0, 100) phase offset.
                string id = ParentEntity.ID ?? "";
                int hash = 17;
                for (int i = 0; i < id.Length; i++)
                    hash = hash * 31 + id[i];
                // Modulo into [0, 1000), divide by 10 → [0, 100). Step
                // small enough to keep Perlin's pattern visible but
                // large enough to noticeably desync neighbors.
                _phaseOffset = ((hash & 0x7FFFFFFF) % 1000) * 0.1f;
                _baseIntensity = lightSource.Intensity;
            }

            float t = (time + _phaseOffset) * Speed;
            // Mathf.PerlinNoise returns [0, 1]; remap to [-1, 1].
            float noise = Mathf.PerlinNoise(t, 0f) * 2f - 1f;
            // Clamp so noise is strictly within [-1, 1] (Perlin can
            // technically return slightly outside in edge cases).
            if (noise > 1f) noise = 1f;
            else if (noise < -1f) noise = -1f;
            lightSource.Intensity = _baseIntensity * (1f + noise * IntensityWobble);
        }
    }
}
