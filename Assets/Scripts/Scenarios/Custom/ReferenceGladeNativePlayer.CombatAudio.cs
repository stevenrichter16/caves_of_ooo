using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using UnityEngine;

namespace CavesOfOoo.Scenarios.Custom
{
    // Observes ordinary native combat without submitting sounds or game actions.
    public sealed partial class ReferenceGladeNativePlayer
    {
        Action _combatAudioCleanup;
        WorldAudioRuntime _combatAudioRuntime;
        WorldAudioPlayer _combatAudioPlayer;
        AudioSource[] _combatAudioSources;
        readonly HashSet<AudioClip> _combatAudioImported = new HashSet<AudioClip>();
        int _combatAudioInitialFocus, _combatAudioMatchedWindows;
        float _combatAudioMinimum = 1;
        bool _combatAudioCuePlayed, _combatAudioBackgroundPlayed, _combatAudioStable = true;

        sealed class CombatAudioWindow
        {
            public int FocusBefore, FocusAfter, Samples;
            public float MixBefore, MinimumMix = 1, FocusedMusicVolume, FocusedBedVolume;
            public bool ImportedCuePlaying, PositiveMusicAndBed;
            public readonly List<string> ImportedClips = new List<string>();
        }

        void BeginCombatAudio()
        {
            if (!_combatOnly) return;
            string[] keys = { WorldAudioSettings.MusicKey, WorldAudioSettings.AmbienceKey, WorldAudioSettings.SfxKey };
            var had = keys.Select(PlayerPrefs.HasKey).ToArray();
            var values = keys.Select(key => PlayerPrefs.GetFloat(key)).ToArray();
            float music = WorldAudioSettings.MusicVolume, bed = WorldAudioSettings.AmbienceVolume, sfx = WorldAudioSettings.SfxVolume;
            bool paused = AudioListener.pause;
            // Register restoration before any audio-only setup or failing precondition.
            _combatAudioCleanup = () =>
            {
                for (int i = 0; i < keys.Length; i++)
                    if (had[i]) PlayerPrefs.SetFloat(keys[i], values[i]); else PlayerPrefs.DeleteKey(keys[i]);
                PlayerPrefs.Save();
                WorldAudioSettings.MusicVolume = music; WorldAudioSettings.AmbienceVolume = bed; WorldAudioSettings.SfxVolume = sfx;
                AudioListener.pause = paused;
                bool restored = WorldAudioSettings.MusicVolume == music && WorldAudioSettings.AmbienceVolume == bed
                    && WorldAudioSettings.SfxVolume == sfx && AudioListener.pause == paused;
                for (int i = 0; i < keys.Length; i++)
                    restored &= PlayerPrefs.HasKey(keys[i]) == had[i] && (!had[i] || PlayerPrefs.GetFloat(keys[i]) == values[i]);
                Check("combat_audio_preferences_restored", restored);
            };
            WorldAudioSettings.MusicVolume = .55f; WorldAudioSettings.AmbienceVolume = .45f; WorldAudioSettings.SfxVolume = 1;
            AudioListener.pause = false;
            _combatAudioRuntime = FindFirstObjectByType<WorldAudioRuntime>();
            Require(_combatAudioRuntime?.Player?.Root != null, "standalone combat has bootstrap-owned world audio");
            _combatAudioPlayer = _combatAudioRuntime.Player;
            _combatAudioInitialFocus = _combatAudioPlayer.CombatFocusCount;
            _combatAudioSources = _combatAudioPlayer.Root.GetComponentsInChildren<AudioSource>();
            Require(_combatAudioPlayer.AllocatedSources == 16 && _combatAudioSources.Length == 16, "fixed live combat audio pool");
            // Once-only prepared resource lookup proves clip identity. It neither creates
            // a player/source nor submits any sound; per-frame observation uses this set.
            var library = WorldAudioLibrary.LoadResources();
            WorldAudioCue[] cues = { WorldAudioCue.Swing, WorldAudioCue.Hit, WorldAudioCue.SwingBlade,
                WorldAudioCue.SwingBlunt, WorldAudioCue.SwingNatural, WorldAudioCue.HitCutting,
                WorldAudioCue.HitPiercing, WorldAudioCue.HitBlunt, WorldAudioCue.HitNatural };
            foreach (var cue in cues) for (int take = 0; take < 2; take++)
            {
                var clip = library.Sound(cue, take)?.Clip;
                Require(clip != null && clip.loadState == AudioDataLoadState.Loaded, "prepared actual melee recording " + cue);
                _combatAudioImported.Add(clip);
            }
        }

        bool CombatAudioOwnerCurrent() => _combatAudioRuntime != null
            && ReferenceEquals(_combatAudioRuntime.Player, _combatAudioPlayer) && _combatAudioPlayer?.Root != null
            && _combatAudioPlayer.AllocatedSources == 16 && _combatAudioSources.Length == 16
            && _combatAudioSources.All(source => source != null);

        CombatAudioWindow BeginCombatAudioWindow(bool attack)
        {
            if (!_combatOnly || !attack) return null;
            Require(CombatAudioOwnerCurrent(), "same audio owner before actual combat input");
            return new CombatAudioWindow { FocusBefore = _combatAudioPlayer.CombatFocusCount,
                FocusAfter = _combatAudioPlayer.CombatFocusCount, MixBefore = _combatAudioPlayer.AtmosphereMix };
        }

        void SampleCombatAudioWindow(CombatAudioWindow window)
        {
            if (window == null) return;
            if (!CombatAudioOwnerCurrent()) { _combatAudioStable = false; return; }
            window.Samples++; window.FocusAfter = _combatAudioPlayer.CombatFocusCount;
            // A previously held dip alone is not evidence for this key. The real
            // typed combat path must admit another cue within this observation window.
            if (window.FocusAfter <= window.FocusBefore) return;
            float mix = _combatAudioPlayer.AtmosphereMix;
            _combatAudioStable &= float.IsFinite(mix) && mix >= .5999f && mix <= 1.0001f;
            window.MinimumMix = Mathf.Min(window.MinimumMix, mix);
            bool music = false, bed = false;
            foreach (var source in _combatAudioSources)
            {
                if (!source.isPlaying || source.clip == null || source.volume <= 0
                    || source.clip.loadState != AudioDataLoadState.Loaded) continue;
                if (!source.loop && source.priority == 64 && _combatAudioImported.Contains(source.clip))
                {
                    window.ImportedCuePlaying = true;
                    if (!window.ImportedClips.Contains(source.clip.name)) window.ImportedClips.Add(source.clip.name);
                }
                if (mix < .95f && source.name.StartsWith("Music ", StringComparison.Ordinal))
                { music = true; window.FocusedMusicVolume = Mathf.Max(window.FocusedMusicVolume, source.volume); }
                if (mix < .95f && source.loop && source.name.StartsWith("Ambience ", StringComparison.Ordinal))
                { bed = true; window.FocusedBedVolume = Mathf.Max(window.FocusedBedVolume, source.volume); }
            }
            window.PositiveMusicAndBed |= music && bed;
        }

        void CompleteCombatAudioWindow(CombatAudioWindow window, bool playerAttempt)
        {
            if (window == null || !playerAttempt || window.FocusAfter <= window.FocusBefore) return;
            // Correlation identifies the real player-attempt interval. Retaliation
            // may also sound in it; no individual clip is falsely attributed to an actor.
            _combatAudioMatchedWindows++;
            _combatAudioMinimum = Mathf.Min(_combatAudioMinimum, window.MinimumMix);
            _combatAudioCuePlayed |= window.ImportedCuePlaying;
            _combatAudioBackgroundPlayed |= window.PositiveMusicAndBed;
        }

        IEnumerator ObserveCombatAudioRecovery()
        {
            if (!_combatOnly) yield break;
            Require(CombatAudioOwnerCurrent(), "same bootstrap audio owner after encounter");
            var actor = _input.PlayerEntity; var at = Cell(); var zone = _input.CurrentZone;
            int tick = _input.TurnManager.TickCount, energy = _input.TurnManager.GetEnergy(actor);
            string stats = CombatStats(actor), gear = CombatGear(actor);
            int focus = _combatAudioPlayer.CombatFocusCount, restarts = 0;
            double began = Time.realtimeSinceStartupAsDouble, quietUntil = began + 2.2;
            float recoveryStart = _combatAudioPlayer.AtmosphereMix;
            while (Time.realtimeSinceStartupAsDouble < quietUntil)
            {
                yield return new WaitForEndOfFrame();
                Require(CombatAudioOwnerCurrent(), "same audio owner throughout passive recovery");
                if (_combatAudioPlayer.CombatFocusCount != focus)
                {
                    restarts++;
                    _descriptions.Add(new Description { subject = "Combat audio recovery interference", source = "Actual native audio owner",
                        text = "Accepted focus counter changed from " + focus + " to " + _combatAudioPlayer.CombatFocusCount
                            + "; restarting one bounded quiet observation window. No input or world mutation by the observer." });
                    Require(restarts <= 1, "at most one genuine combat cue may restart the quiet audio window");
                    focus = _combatAudioPlayer.CombatFocusCount; quietUntil = Time.realtimeSinceStartupAsDouble + 2.2;
                }
                Require(Time.realtimeSinceStartupAsDouble - began < 4.6, "bounded passive combat audio recovery");
            }
            Check("combat_audio_imported_melee_plays", _combatAudioMatchedWindows > 0 && _combatAudioCuePlayed
                && _combatAudioPlayer.CombatFocusCount > _combatAudioInitialFocus);
            Check("combat_audio_atmosphere_dips", _combatAudioMinimum < .95f && _combatAudioMinimum >= .5999f
                && _combatAudioBackgroundPlayed && _combatAudioStable);
            Check("combat_audio_quiet_recovery", _combatAudioPlayer.AtmosphereMix >= .9999f
                && _combatAudioPlayer.AtmosphereMix <= 1.0001f && _combatAudioPlayer.CombatFocusCount == focus);
            Check("combat_audio_observation_no_turn", ReferenceEquals(actor, _input.PlayerEntity)
                && ReferenceEquals(at, Cell()) && ReferenceEquals(zone, _input.CurrentZone)
                && _input.TurnManager.TickCount == tick && _input.TurnManager.GetEnergy(actor) == energy
                && CombatStats(actor) == stats && CombatGear(actor) == gear);
            Check("combat_audio_pool_preferences_unchanged", CombatAudioOwnerCurrent()
                && _combatAudioPlayer.Root.GetComponentsInChildren<AudioSource>().Length == 16
                && WorldAudioSettings.MusicVolume == .55f && WorldAudioSettings.AmbienceVolume == .45f
                && WorldAudioSettings.SfxVolume == 1 && !AudioListener.pause);
            _descriptions.Add(new Description { subject = "Iteration 4 actual combat audio", source = "Native keyboard encounter and EndOfFrame observation",
                text = "Correlated player-attempt windows=" + _combatAudioMatchedWindows + "; focus count before="
                    + _combatAudioInitialFocus + ", after=" + _combatAudioPlayer.CombatFocusCount
                    + "; minimum mix=" + _combatAudioMinimum + "; recovery start=" + recoveryStart
                    + ", end=" + _combatAudioPlayer.AtmosphereMix + "; observed seconds="
                    + (Time.realtimeSinceStartupAsDouble - began) + "; quiet restarts=" + restarts
                    + "; actual clip names and positive background volumes are in combatSteps audio fields. "
                    + "These observations do not establish subjective clarity, globally unclipped output, physical playback or Deck performance." });
        }

        void AbortCombatAudio()
        {
            var restore = _combatAudioCleanup; _combatAudioCleanup = null;
            restore?.Invoke();
        }
    }
}
