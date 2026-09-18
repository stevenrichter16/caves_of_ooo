using UnityEngine;

namespace CavesOfOoo.Rendering
{
    public enum SpellFxMode { Off, Reduced, Full }

    /// <summary>Presentation preferences only; never part of a saved game.</summary>
    public static class SpellFxSettings
    {
        public static SpellFxMode Mode { get; set; } = SpellFxMode.Full;
        private static float _speed = 1f, _flash = 0.15f, _shake;
        public static float AnimationSpeed { get => _speed; set => _speed = SafeClamp(value, .25f, 4f, 1f); }
        public static float FlashIntensity { get => _flash; set => _flash = SafeClamp(value, 0f, 1f, 0f); }
        public static float ShakeIntensity { get => _shake; set => _shake = SafeClamp(value, 0f, 1f, 0f); }

        private static float SafeClamp(float value, float min, float max, float fallback)
            => float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);

        public static void Load()
        {
            Mode = (SpellFxMode)Mathf.Clamp(PlayerPrefs.GetInt("CavesOfOoo.Fx.Mode", 2), 0, 2);
            AnimationSpeed = PlayerPrefs.GetFloat("CavesOfOoo.Fx.Speed", 1f);
            FlashIntensity = PlayerPrefs.GetFloat("CavesOfOoo.Fx.Flash", .15f);
            ShakeIntensity = PlayerPrefs.GetFloat("CavesOfOoo.Fx.Shake", 0f);
        }

        public static void Save()
        {
            PlayerPrefs.SetInt("CavesOfOoo.Fx.Mode", (int)Mode);
            PlayerPrefs.SetFloat("CavesOfOoo.Fx.Speed", AnimationSpeed);
            PlayerPrefs.SetFloat("CavesOfOoo.Fx.Flash", FlashIntensity);
            PlayerPrefs.SetFloat("CavesOfOoo.Fx.Shake", ShakeIntensity);
            PlayerPrefs.Save();
        }
    }
}
