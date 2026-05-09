using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CavesOfOoo.Presentation.Rendering
{
    /// <summary>
    /// Subtle perlin-noise-driven flicker for a <see cref="Light2D"/>.
    /// Wobbles intensity + outer-radius by a small amount to give
    /// torches, lanterns, and fire FX a "living flame" feel without
    /// needing the AnimatorController + AnimationClip pipeline.
    ///
    /// <para>Drop this MonoBehaviour onto any GameObject that has a
    /// <see cref="Light2D"/> component. On Awake it caches the base
    /// values; per-Update it samples Perlin noise to compute small
    /// offsets. Performance is negligible — one Perlin sample per
    /// frame per flickering light.</para>
    ///
    /// <para>Companion to <c>Assets/Animations/TorchFlicker.anim</c>
    /// (which uses the same intensity/radius properties via Unity's
    /// AnimationClip system; either approach works, this script is
    /// easier to wire up at scene-setup time).</para>
    ///
    /// <para>See <c>Docs/GRAPHICS-POLISH.md</c> § Pass 2 for
    /// per-feature tuning rationale.</para>
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light2D))]
    public class Light2DFlicker : MonoBehaviour
    {
        [Header("Wobble amounts (fraction of base value)")]
        [Tooltip("How much to vary the light's intensity. 0.15 = ±15%.")]
        [Range(0f, 0.5f)] public float IntensityWobble = 0.15f;

        [Tooltip("How much to vary the light's outer radius. 0.04 = ±4%.")]
        [Range(0f, 0.2f)] public float RadiusWobble = 0.04f;

        [Header("Speed")]
        [Tooltip("Higher = faster flicker. 2.5 ≈ candle; 4.0 ≈ torch; 1.2 ≈ ember.")]
        [Range(0.1f, 8f)] public float Speed = 2.5f;

        [Tooltip("Random phase offset; set per-light in Awake to "
                 + "desync neighboring torches.")]
        public float PhaseOffset = 0f;

        private Light2D _light;
        private float _baseIntensity;
        private float _baseRadius;

        private void Awake()
        {
            _light = GetComponent<Light2D>();
            _baseIntensity = _light.intensity;
            _baseRadius = _light.pointLightOuterRadius;
            // Random per-instance phase so multiple torches don't
            // flicker in lock-step. Hash transform position so the
            // same torch is reproducible across reloads (deterministic).
            PhaseOffset = (transform.position.x * 7.31f
                           + transform.position.y * 13.17f) % 100f;
        }

        private void Update()
        {
            if (_light == null) return;
            float t = (Time.time + PhaseOffset) * Speed;

            // Two decoupled Perlin samples — one for intensity, one
            // for radius — so they don't move in lock-step (a torch
            // that gets brighter AND bigger at the same instant looks
            // like a single-axis pulse; uncorrelated wobble looks
            // like a real flame).
            float iNoise = Mathf.PerlinNoise(t, 0f) * 2f - 1f;     // -1..1
            float rNoise = Mathf.PerlinNoise(0f, t * 0.7f) * 2f - 1f;

            _light.intensity = _baseIntensity * (1f + iNoise * IntensityWobble);
            _light.pointLightOuterRadius = _baseRadius * (1f + rNoise * RadiusWobble);
        }

        private void OnDisable()
        {
            // Restore base values when disabled so post-disable state
            // is deterministic (e.g., for screenshots).
            if (_light != null)
            {
                _light.intensity = _baseIntensity;
                _light.pointLightOuterRadius = _baseRadius;
            }
        }
    }
}
