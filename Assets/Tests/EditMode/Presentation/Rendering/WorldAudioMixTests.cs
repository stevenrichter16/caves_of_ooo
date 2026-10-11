using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Acceptance tests for bounded combat focus. Synthetic clips prove routing and
    /// gain arithmetic; the separate native combat audit must prove audible playback.</summary>
    public sealed class WorldAudioMixTests
    {
        GameObject root;
        WorldAudioPlayer audio;
        WorldAudioEventRouter router;
        WorldAudioLibrary library;
        Zone zone;
        Entity actor, target;
        readonly List<AudioClip> clips = new List<AudioClip>();
        Action<WorldAudioCombatReceipt> priorCombat;
        Action<WorldAudioWorkReceipt> priorWork;
        Action<WorldAudioCue, Entity, Zone, int, int> priorSemantic;
        EntityAttackVisualHandler priorVisual;
        float priorMusic, priorAmbience, priorSfx;
        bool priorPause;

        [SetUp] public void Setup()
        {
            priorMusic = WorldAudioSettings.MusicVolume;
            priorAmbience = WorldAudioSettings.AmbienceVolume;
            priorSfx = WorldAudioSettings.SfxVolume;
            priorPause = AudioListener.pause;
            WorldAudioSettings.MusicVolume = .5f;
            WorldAudioSettings.AmbienceVolume = .4f;
            WorldAudioSettings.SfxVolume = .8f;
            AudioListener.pause = false;
            priorCombat = WorldAudioCombat.Callback;
            priorWork = WorldAudioWork.Callback;
            priorSemantic = WorldAudioHooks.Callback;
            priorVisual = EntityVisualHooks.AttackCallback;
            WorldAudioCombat.Callback = null;
            WorldAudioWork.Callback = null;
            WorldAudioHooks.Callback = null;
            EntityVisualHooks.AttackCallback = null;
            root = new GameObject("combat atmosphere mix tests");
            library = new WorldAudioLibrary();
            library.AddMusic("organic", Clip("music"), .4f);
            library.AddAmbience("field", Clip("bed"), .3f);
            library.AddDetail("test-detail", Clip("detail"), .25f);
            foreach (WorldAudioCue cue in Enum.GetValues(typeof(WorldAudioCue)))
                library.AddSound(cue, Clip("action-" + cue), .6f);
            audio = new WorldAudioPlayer(root.transform, library);
            router = new WorldAudioEventRouter(audio);
            zone = WorldAudioCombatTests.World(out actor, out target);
            actor.AddPart(new MeleeWeaponPart { Attributes = "Cutting", BaseDamage = "1d1", MaxStrengthBonus = 0 });
            router.SetWorld(zone, actor, true);
            audio.SetContext(WorldAudioContext.Select(BiomeType.Spread, 0, false));
            audio.Update(3f);
            Assert.That(audio.AtmosphereMix, Is.EqualTo(1f));
            MessageLog.Clear();
        }

        [TearDown] public void Cleanup()
        {
            router?.Dispose();
            audio?.Dispose();
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            foreach (var clip in clips) UnityEngine.Object.DestroyImmediate(clip);
            clips.Clear();
            WorldAudioCombat.Callback = priorCombat;
            WorldAudioWork.Callback = priorWork;
            WorldAudioHooks.Callback = priorSemantic;
            EntityVisualHooks.AttackCallback = priorVisual;
            WorldAudioSettings.MusicVolume = priorMusic;
            WorldAudioSettings.AmbienceVolume = priorAmbience;
            WorldAudioSettings.SfxVolume = priorSfx;
            AudioListener.pause = priorPause;
            MessageLog.Clear();
        }

        AudioClip Clip(string name)
        {
            // Long enough to measure recovery without a short voice naturally expiring.
            var clip = AudioClip.Create(name, 8000 * 8, 1, 8000, false);
            clips.Add(clip);
            return clip;
        }
        AudioSource Playing(string name) => root.GetComponentsInChildren<AudioSource>()
            .Single(source => source.clip != null && source.clip.name == name);
        void Attack() => Assert.True(CombatSystem.PerformMeleeAttack(actor, target, zone, new WorldAudioCombatTests.BranchRandom()));
        void Mix(float value) => Assert.That(audio.AtmosphereMix, Is.EqualTo(value).Within(.0001f));
        void PreferencesUnchanged()
        {
            Assert.That(WorldAudioSettings.MusicVolume, Is.EqualTo(.5f));
            Assert.That(WorldAudioSettings.AmbienceVolume, Is.EqualTo(.4f));
            Assert.That(WorldAudioSettings.SfxVolume, Is.EqualTo(.8f));
        }

        [Test] public void ActualVisibleWeaponAttemptFocusesOnlyItsTwoAcceptedCombatSounds()
        {
            Attack();
            Assert.Less(target.GetStatValue("Hitpoints"), 1000);
            Assert.AreEqual(2, audio.AcceptedSoundCount);
            Assert.AreEqual(2, audio.CombatFocusCount);
            Assert.NotNull(Playing("action-SwingBlade"));
            Assert.NotNull(Playing("action-HitCutting"));
            Mix(1f); // Attack smoothing starts on a wall-clock update, not a sudden gain jump.
            audio.Update(.1f);
            Mix(.6f);
            Assert.AreEqual(16, audio.AllocatedSources);
            PreferencesUnchanged();
        }

        [Test] public void GenericSwingDamageAndVisualAnimationCannotClaimTypedCombatFocus()
        {
            Assert.True(audio.TryPlay(WorldAudioCue.Swing));
            CombatSystem.ApplyDamage(target, 3, actor, zone);
            EntityVisualHooks.EmitAttack(actor, target, zone);
            WorldAudioHooks.Emit(WorldAudioCue.SwingBlade, actor, zone, 11, 10);
            audio.Update(.2f);
            Mix(1f);
            Assert.AreEqual(0, audio.CombatFocusCount);
            Assert.AreEqual(1, audio.AcceptedSoundCount);
            Attack();
            Assert.AreEqual(2, audio.CombatFocusCount, "The same fixture must accept a real typed attack.");
        }

        [TestCase("cell-hidden")]
        [TestCase("world-hidden")]
        [TestCase("muted")]
        [TestCase("paused")]
        public void UnperceivedOrInaudibleRealCombatCannotLowerTheBackground(string refusal)
        {
            if (refusal == "cell-hidden") zone.GetCell(11, 10).IsVisible = false;
            if (refusal == "world-hidden") router.SetWorld(zone, actor, false);
            if (refusal == "muted") WorldAudioSettings.SfxVolume = 0;
            if (refusal == "paused") AudioListener.pause = true;
            Attack();
            Assert.Less(target.GetStatValue("Hitpoints"), 1000, "Audio refusal cannot cancel gameplay damage.");
            audio.Update(.2f);
            Mix(1f);
            Assert.AreEqual(0, audio.CombatFocusCount);
            Assert.AreEqual(0, audio.AcceptedSoundCount);
            zone.GetCell(11, 10).IsVisible = true;
            router.SetWorld(zone, actor, true);
            WorldAudioSettings.SfxVolume = .8f;
            AudioListener.pause = false;
            audio.Update(.2f);
            Mix(1f); // Restoring audibility does not replay a refused combat event.
            Attack();
            Assert.AreEqual(2, audio.CombatFocusCount);
        }

        [Test] public void WholeAttackVetoCannotCreateACombatFocusWindow()
        {
            var veto = new WorldAudioCombatTests.EventProbe { Event = "BeforeMeleeAttack", Veto = true };
            actor.AddPart(veto);
            Assert.False(CombatSystem.PerformMeleeAttack(actor, target, zone, new WorldAudioCombatTests.BranchRandom()));
            audio.Update(.1f);
            Mix(1f);
            Assert.AreEqual(0, audio.CombatFocusCount);
            veto.Veto = false;
            Attack();
            Assert.AreEqual(2, audio.CombatFocusCount);
        }

        [Test] public void NonCombatAndInvalidCueIdsAreRejectedBeforeTheyPlayOrFocus()
        {
            var combat = new HashSet<WorldAudioCue> {
                WorldAudioCue.Swing, WorldAudioCue.Hit, WorldAudioCue.SwingBlade,
                WorldAudioCue.SwingBlunt, WorldAudioCue.SwingNatural, WorldAudioCue.HitCutting,
                WorldAudioCue.HitPiercing, WorldAudioCue.HitBlunt, WorldAudioCue.HitNatural };
            foreach (WorldAudioCue cue in Enum.GetValues(typeof(WorldAudioCue)))
                if (!combat.Contains(cue)) Assert.False(audio.TryPlayCombat(cue), cue.ToString());
            Assert.False(audio.TryPlayCombat((WorldAudioCue)(-1)));
            Assert.False(audio.TryPlayCombat((WorldAudioCue)9999));
            Assert.AreEqual(0, audio.AcceptedSoundCount);
            Assert.AreEqual(0, audio.CombatFocusCount);
            Mix(1f);
            foreach (var cue in combat) Assert.True(audio.TryPlayCombat(cue), cue.ToString());
            Assert.AreEqual(9, audio.CombatFocusCount);
        }

        [Test] public void MissingClipDisposedPlayerAndDestroyedAudioGraphCannotFocus()
        {
            using (var empty = new WorldAudioPlayer(root.transform, new WorldAudioLibrary()))
            {
                Assert.False(empty.TryPlayCombat(WorldAudioCue.Swing));
                Assert.AreEqual(0, empty.CombatFocusCount);
                Assert.AreEqual(1f, empty.AtmosphereMix);
                empty.Dispose();
                Assert.False(empty.TryPlayCombat(WorldAudioCue.Swing));
                Assert.AreEqual(0, empty.CombatFocusCount);
            }
            UnityEngine.Object.DestroyImmediate(audio.Root.gameObject);
            Assert.False(audio.TryPlayCombat(WorldAudioCue.Swing));
            Assert.AreEqual(0, audio.CombatFocusCount);
            Mix(1f);
        }

        [Test] public void CooldownRefusalDoesNotExtendTheAcceptedEventsRecoveryDeadline()
        {
            Assert.True(audio.TryPlayCombat(WorldAudioCue.Swing));
            audio.Update(.05f);
            Mix(.8f);
            Assert.False(audio.TryPlayCombat(WorldAudioCue.Swing));
            Assert.AreEqual(1, audio.CombatFocusCount);
            audio.Update(1.75f);
            Mix(1f); // .3 seconds of hold plus 1.5 seconds of recovery from the accepted event.
            Assert.True(audio.TryPlayCombat(WorldAudioCue.Swing));
            Assert.AreEqual(2, audio.CombatFocusCount);
        }

        [Test] public void FullActionPoolRefusalDoesNotFocusOrAllocateMoreVoices()
        {
            // Generic calls occupy the entire existing pool without a focus side effect.
            var cues = ((WorldAudioCue[])Enum.GetValues(typeof(WorldAudioCue))).Take(WorldAudioPlayer.SoundCapacity);
            foreach (var cue in cues) Assert.True(audio.TryPlay(cue), cue.ToString());
            Assert.AreEqual(12, audio.ActiveSoundSources);
            Assert.False(audio.TryPlayCombat(WorldAudioCue.HitNatural));
            Assert.AreEqual(0, audio.CombatFocusCount);
            Mix(1f);
            Assert.AreEqual(16, audio.AllocatedSources);
            audio.ClearSounds();
            Assert.True(audio.TryPlayCombat(WorldAudioCue.HitNatural));
            Assert.AreEqual(1, audio.CombatFocusCount);
        }

        [Test] public void FocusAttacksSmoothlyHoldsForPointThreeAndRecoversOverOnePointFiveSeconds()
        {
            Assert.True(audio.TryPlayCombat(WorldAudioCue.Swing));
            Mix(1f);
            audio.Update(.05f); Mix(.8f);
            audio.Update(.05f); Mix(.6f);
            audio.Update(.2f); Mix(.6f);
            audio.Update(.75f); Mix(.8f);
            audio.Update(.75f); Mix(1f);
            audio.Update(3f); Mix(1f);
            Assert.AreEqual(1, audio.CombatFocusCount);
        }

        [Test] public void AFrameCrossingTheHoldBoundaryMatchesEquivalentSplitUpdates()
        {
            Assert.True(audio.TryPlayCombat(WorldAudioCue.Swing));
            audio.Update(.9f);
            float singleFrame = audio.AtmosphereMix;
            Assert.That(singleFrame, Is.EqualTo(.76f).Within(.0001f));
            audio.ClearSounds();
            Assert.True(audio.TryPlayCombat(WorldAudioCue.Swing));
            audio.Update(.1f);
            audio.Update(.2f);
            audio.Update(.6f);
            Assert.That(audio.AtmosphereMix, Is.EqualTo(singleFrame).Within(.0001f));
            audio.Update(.9f);
            Mix(1f);
        }

        [Test] public void ANewAcceptedStrikeRefocusesFromTheCurrentGainAndExtendsOnlyItsOwnHold()
        {
            Assert.True(audio.TryPlayCombat(WorldAudioCue.Swing));
            audio.Update(1.05f);
            Mix(.8f);
            Assert.True(audio.TryPlayCombat(WorldAudioCue.Hit));
            Mix(.8f);
            audio.Update(.05f); Mix(.6f);
            audio.Update(.25f); Mix(.6f);
            audio.Update(1.5f); Mix(1f);
            Assert.AreEqual(2, audio.CombatFocusCount);
        }

        [Test] public void MusicBedAndExistingDetailsShareTheEnvelopeWhileActionGainAndPreferencesRemainIntact()
        {
            Assert.True(audio.TryPlayDetail("test-detail", gain: .5f));
            Assert.That(Playing("music").volume, Is.EqualTo(.2f).Within(.0001f));
            Assert.That(Playing("bed").volume, Is.EqualTo(.12f).Within(.0001f));
            Assert.That(Playing("detail").volume, Is.EqualTo(.05f).Within(.0001f));
            Assert.True(audio.TryPlayCombat(WorldAudioCue.Swing));
            audio.Update(.1f);
            Mix(.6f);
            Assert.That(Playing("music").volume, Is.EqualTo(.12f).Within(.0001f));
            Assert.That(Playing("bed").volume, Is.EqualTo(.072f).Within(.0001f));
            Assert.That(Playing("detail").volume, Is.EqualTo(.03f).Within(.0001f));
            Assert.That(Playing("action-Swing").volume, Is.EqualTo(.48f).Within(.0001f));
            int musicStarts = audio.MusicStartCount;
            audio.Update(1.7f);
            Mix(1f);
            Assert.That(Playing("music").volume, Is.EqualTo(.2f).Within(.0001f));
            Assert.That(Playing("bed").volume, Is.EqualTo(.12f).Within(.0001f));
            Assert.That(Playing("detail").volume, Is.EqualTo(.05f).Within(.0001f));
            Assert.That(Playing("action-Swing").volume, Is.EqualTo(.48f).Within(.0001f));
            Assert.AreEqual(musicStarts, audio.MusicStartCount, "Focus must not restart the music stream.");
            Assert.AreEqual(16, audio.AllocatedSources);
            PreferencesUnchanged();
        }

        [Test] public void ADetailStartedDuringFocusUsesTheCurrentMixImmediately()
        {
            Assert.True(audio.TryPlayCombat(WorldAudioCue.Swing));
            audio.Update(.1f);
            Mix(.6f);
            Assert.True(audio.TryPlayDetail("test-detail", gain: .5f));
            Assert.That(Playing("detail").volume, Is.EqualTo(.03f).Within(.0001f), "No one-frame full-volume environmental burst.");
            Assert.True(audio.TryPlay(WorldAudioCue.Pickup));
            Assert.That(Playing("action-Pickup").volume, Is.EqualTo(.48f).Within(.0001f));
            Assert.AreEqual(1, audio.CombatFocusCount);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SfxMuteOrPauseClearsFocusAndCannotQueueARecoveryDip(bool pause)
        {
            Assert.True(audio.TryPlayCombat(WorldAudioCue.Swing));
            audio.Update(.1f);
            Mix(.6f);
            if (pause) AudioListener.pause = true;
            else WorldAudioSettings.SfxVolume = 0;
            audio.Update(.05f);
            Mix(1f);
            Assert.False(audio.TryPlayCombat(WorldAudioCue.Hit));
            AudioListener.pause = false;
            WorldAudioSettings.SfxVolume = .8f;
            audio.Update(2f);
            Mix(1f);
            Assert.AreEqual(1, audio.CombatFocusCount);
            Assert.AreEqual(0, audio.ActiveSoundSources);
            Assert.That(Playing("music").volume, Is.EqualTo(.2f).Within(.0001f));
            PreferencesUnchanged();
            Assert.True(audio.TryPlayCombat(WorldAudioCue.Hit));
            audio.Update(.1f);
            Mix(.6f);
        }

        [Test] public void ClearingSoundsAndChangingWorldGraphRetireThePreviousCombatEnvelope()
        {
            Assert.True(audio.TryPlayCombat(WorldAudioCue.Swing));
            audio.Update(.1f);
            Mix(.6f);
            audio.ClearSounds();
            Mix(1f);
            Assert.AreEqual(0, audio.ActiveSoundSources);
            Assert.True(audio.TryPlayCombat(WorldAudioCue.Swing));
            audio.Update(.1f);
            Mix(.6f);
            var other = WorldAudioCombatTests.World(out var otherPlayer, out _);
            router.SetWorld(other, otherPlayer, true);
            Mix(1f);
            Assert.AreEqual(0, audio.ActiveSoundSources);
            audio.Update(2f);
            Mix(1f);
            Assert.AreEqual(16, audio.AllocatedSources);
            PreferencesUnchanged();
        }

        [Test] public void ZeroNegativeAndNonFiniteDeltasCannotAdvanceOrCorruptTheEnvelope()
        {
            Assert.True(audio.TryPlayCombat(WorldAudioCue.Swing));
            audio.Update(.05f);
            Mix(.8f);
            foreach (var delta in new[] { 0f, -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                audio.Update(delta);
                Mix(.8f);
                Assert.That(Playing("music").volume, Is.EqualTo(.16f).Within(.0001f));
            }
            Assert.AreEqual(1, audio.CombatFocusCount);
            PreferencesUnchanged();
            audio.Update(.05f);
            Mix(.6f);
        }
    }
}
