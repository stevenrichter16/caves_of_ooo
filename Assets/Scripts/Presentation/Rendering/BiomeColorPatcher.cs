using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using CavesOfOoo.Core;

namespace CavesOfOoo.Presentation.Rendering
{
    /// <summary>
    /// Pass 6 §6B — wires the Pass 3 §3.C
    /// <see cref="BiomePalette"/> data layer (Cave warm/dim,
    /// Desert washed, Jungle green, Ruins desaturated cool) into
    /// the runtime so each zone's biome immediately changes the
    /// global Volume's grading.
    ///
    /// <para><b>How it works:</b>
    /// <list type="number">
    ///   <item>On Awake, find the global Volume + cache its
    ///         <c>ColorAdjustments</c> + <c>Vignette</c> overrides.</item>
    ///   <item>Per Update (cheap; just a string compare), check
    ///         the current zone's biome.</item>
    ///   <item>If biome changed, look up the
    ///         <see cref="BiomePalette"/> and start lerping the
    ///         volume's overrides toward the target values over
    ///         a short fade duration.</item>
    /// </list>
    /// </para>
    ///
    /// <para>Why lerp instead of snap? Hard color cuts on zone
    /// transition feel jarring. A 0.8s fade reads as "the world
    /// shifts as you cross the threshold."</para>
    ///
    /// <para>This is a presentation-only component. Saves +
    /// gameplay are not affected — display preference, like the
    /// CRT toggle.</para>
    ///
    /// <para>Plan: <c>Docs/GRAPHICS-PASS6.md</c> §6B.</para>
    /// </summary>
    public class BiomeColorPatcher : MonoBehaviour
    {
        private const float FadeDuration = 0.8f;

        public string GlobalVolumeName = "Global Volume";

        private Volume _globalVolume;
        private ColorAdjustments _colorAdjustments;
        private Vignette _vignette;

        // Current target (where we're lerping toward) and current
        // displayed values (where we are now).
        private BiomeType _activeBiome = BiomeType.Cave;
        private BiomePalette _targetPalette;
        private float _currentContrast;
        private float _currentSaturation;
        private Color _currentColorFilter;
        private float _currentVignetteIntensity;
        private float _fadeProgress = 1f; // 1 = fully at target

        // Reference back to gameplay state. Set by GameBootstrap.
        // CurrentZone is updated on zone-change via SetCurrentZone()
        // (called from the existing zone-load flow). ZoneManager
        // gives us access to the WorldMap for biome lookup.
        public Zone CurrentZone { get; private set; }
        public OverworldZoneManager ZoneManager { get; set; }
        private CavesOfOoo.Rendering.ZoneRenderer _zoneRendererRef;

        public void SetCurrentZone(Zone zone)
        {
            CurrentZone = zone;
        }

        private void Awake()
        {
            FindVolume();
        }

        private void FindVolume()
        {
            var allVolumes = Object.FindObjectsByType<Volume>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var v in allVolumes)
            {
                if (v.gameObject.name == GlobalVolumeName)
                {
                    _globalVolume = v;
                    break;
                }
            }
            if (_globalVolume == null || _globalVolume.sharedProfile == null)
            {
                Debug.LogWarning("[BiomeColorPatcher] No global Volume "
                    + "found or profile missing. Component is a no-op.");
                return;
            }
            _globalVolume.sharedProfile.TryGet(out _colorAdjustments);
            _globalVolume.sharedProfile.TryGet(out _vignette);

            // Snapshot current state as the starting "current" values
            // so initial lerp from real values, not from zero.
            if (_colorAdjustments != null)
            {
                _currentContrast = _colorAdjustments.contrast.value;
                _currentSaturation = _colorAdjustments.saturation.value;
                _currentColorFilter = _colorAdjustments.colorFilter.value;
            }
            if (_vignette != null)
                _currentVignetteIntensity = _vignette.intensity.value;

            _targetPalette = BiomePalette.GetForBiome(_activeBiome);
        }

        private void Update()
        {
            if (_globalVolume == null || _colorAdjustments == null) return;

            // Auto-poll for the current zone via the global
            // ZoneRenderer so callers don't have to remember to call
            // SetCurrentZone on every transition. ZoneRenderer.CurrentZone
            // is the source of truth for "what zone is the camera
            // looking at right now."
            if (CurrentZone == null
                || (_zoneRendererRef != null && _zoneRendererRef.CurrentZone != CurrentZone))
            {
                if (_zoneRendererRef == null)
                    _zoneRendererRef = Object.FindAnyObjectByType<CavesOfOoo.Rendering.ZoneRenderer>();
                if (_zoneRendererRef != null && _zoneRendererRef.CurrentZone != null)
                    CurrentZone = _zoneRendererRef.CurrentZone;
            }

            // 1. Detect biome change via player's current zone.
            var currentBiome = ResolveCurrentBiome();
            if (currentBiome != _activeBiome)
            {
                _activeBiome = currentBiome;
                _targetPalette = BiomePalette.GetForBiome(currentBiome);
                _fadeProgress = 0f;
            }

            // 2. Lerp toward target.
            if (_fadeProgress < 1f)
            {
                _fadeProgress = Mathf.Min(1f, _fadeProgress + Time.deltaTime / FadeDuration);
                float t = _fadeProgress;

                _currentContrast = Mathf.Lerp(_currentContrast, _targetPalette.Contrast, t);
                _currentSaturation = Mathf.Lerp(_currentSaturation, _targetPalette.Saturation, t);
                _currentColorFilter = Color.Lerp(_currentColorFilter, _targetPalette.ColorFilter, t);
                _currentVignetteIntensity = Mathf.Lerp(_currentVignetteIntensity,
                    _targetPalette.VignetteIntensity, t);

                _colorAdjustments.contrast.value = _currentContrast;
                _colorAdjustments.saturation.value = _currentSaturation;
                _colorAdjustments.colorFilter.value = _currentColorFilter;
                if (_vignette != null)
                    _vignette.intensity.value = _currentVignetteIntensity;
            }
        }

        /// <summary>
        /// Determine the current biome from gameplay state. If the
        /// project's wiring hasn't supplied a Player + ZoneManager
        /// reference, falls back to <see cref="BiomeType.Cave"/> (the
        /// default starting biome) so the patcher stays defined.
        /// </summary>
        private BiomeType ResolveCurrentBiome()
        {
            if (CurrentZone == null || ZoneManager == null) return BiomeType.Cave;

            string zoneID = CurrentZone.ZoneID;
            if (string.IsNullOrEmpty(zoneID)) return BiomeType.Cave;
            // Underground zones (z > 0) all use Cave palette by default.
            var (x, y, z) = WorldMap.FromZoneID(zoneID);
            if (z != 0) return BiomeType.Cave;
            var worldMap = ZoneManager.WorldMap;
            if (worldMap == null) return BiomeType.Cave;
            return worldMap.GetBiome(x, y);
        }

        // ── Test seams ───────────────────────────────────────────────────

        public BiomeType TestOnly_ActiveBiome => _activeBiome;
        public bool TestOnly_HasVolume => _globalVolume != null;
        public bool TestOnly_HasColorAdjustments => _colorAdjustments != null;
        public float TestOnly_FadeProgress => _fadeProgress;

        public void TestOnly_ForceBiome(BiomeType biome)
        {
            _activeBiome = biome;
            _targetPalette = BiomePalette.GetForBiome(biome);
            _fadeProgress = 0f;
        }
    }
}
