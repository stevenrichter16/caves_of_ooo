using UnityEngine;

namespace CavesOfOoo.Rendering
{
    /// <summary>Presentation preferences outside saved games. Explicit music zero also disables licensed music for recording.</summary>
    public static class WorldAudioSettings
    {
        public const string MusicKey="CavesOfOoo.Audio.Music", AmbienceKey="CavesOfOoo.Audio.Ambience", SfxKey="CavesOfOoo.Audio.Sfx";
        static float music=.55f, ambience=.45f, sfx=1f;
        public static float MusicVolume { get=>music;set=>music=Clamp(value,.55f); }
        public static float AmbienceVolume { get=>ambience;set=>ambience=Clamp(value,.45f); }
        public static float SfxVolume { get=>sfx;set=>sfx=Clamp(value,1f); }
        static float Clamp(float value,float fallback)=>float.IsFinite(value)?Mathf.Clamp01(value):fallback;
        public static void Load()
        { MusicVolume=PlayerPrefs.GetFloat(MusicKey,.55f);AmbienceVolume=PlayerPrefs.GetFloat(AmbienceKey,.45f);SfxVolume=PlayerPrefs.GetFloat(SfxKey,1f); }
        public static void Save()
        { PlayerPrefs.SetFloat(MusicKey,music);PlayerPrefs.SetFloat(AmbienceKey,ambience);PlayerPrefs.SetFloat(SfxKey,sfx);PlayerPrefs.Save(); }
    }
}
