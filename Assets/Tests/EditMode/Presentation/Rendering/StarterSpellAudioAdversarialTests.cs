using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class StarterSpellAudioAdversarialTests
    {
        [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)] [TestCase(-1f)] [TestCase(5f)]
        public void MalformedContactDoesNotPartiallyAssignAColdVoice(float contact)
        {
            using (var f = new StarterAudioFixture())
            {
                Assert.IsFalse(f.Audio.Play(f.Sequence(), contact, true));
                Assert.AreEqual(0, f.Audio.ActiveVoices); Assert.AreEqual(0, f.Assigned.Length);
                Assert.AreEqual(0, f.Audio.AcceptedCount); Assert.IsTrue(f.Play());
            }
        }

        [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)] [TestCase(-1f)]
        public void MalformedDeltasDoNotPoisonAValidLaterContact(float delta)
        {
            using (var f = new StarterAudioFixture())
            {
                Assert.IsTrue(f.Play()); f.Audio.Update(delta, delta, delta);
                Assert.AreEqual(0, f.Audio.ContactLayerCount); f.Contact();
                Assert.AreEqual(3, f.Audio.ContactLayerCount);
            }
        }

        [TestCase(true)] [TestCase(false)]
        public void LostRootOrSingleSourceCancelsOldSoundAndAllowsCompletePoolRebuild(bool root)
        {
            using (var f = new StarterAudioFixture())
            {
                Assert.IsTrue(f.Play());
                UnityEngine.Object.DestroyImmediate(root ? (UnityEngine.Object)f.Audio.Root.gameObject :
                    f.Audio.Root.GetComponentInChildren<AudioSource>());
                f.Contact(); Assert.AreEqual(0, f.Audio.ActiveVoices); Assert.AreEqual(0, f.Audio.ContactLayerCount);
                Assert.IsTrue(f.Audio.Prepare()); Assert.IsTrue(f.Play());
                Assert.AreEqual(32, f.Audio.Root.GetComponentsInChildren<AudioSource>().Length);
                f.Contact(); Assert.AreEqual(3, f.Audio.ContactLayerCount);
            }
        }

        [TestCase("clear")] [TestCase("zone")] [TestCase("off")]
        [TestCase("mute")] [TestCase("pause")] [TestCase("timeout")] [TestCase("dispose")]
        public void InterruptionReleasesEveryPrefixAndNeverFiresDelayedSuffix(string reason)
        {
            using (var f = new StarterAudioFixture())
            {
                Assert.IsTrue(f.Play());
                if (reason == "clear") f.Audio.ClearAll();
                if (reason == "zone") f.Audio.SetZone(new Zone("replacement"));
                if (reason == "off") SpellFxSettings.Mode = SpellFxMode.Off;
                if (reason == "mute") SpellFxSettings.SoundVolume = 0;
                if (reason == "pause") AudioListener.pause = true;
                if (reason == "dispose") f.Audio.Dispose();
                f.Audio.Update(reason == "timeout" ? 16 : .3f, .3f, .3f);
                Assert.AreEqual(0, f.Audio.ActiveVoices); Assert.AreEqual(0, f.Audio.ContactLayerCount);
                Assert.AreEqual(0, f.Assigned.Length);
            }
        }

        [Test] public void NativeCancellationLeavesIndependentFallbackVoiceAlive()
        {
            using (var f = new StarterAudioFixture())
            {
                Assert.IsTrue(f.Play(native: true)); Assert.IsTrue(f.Play(f.Sequence(seed: 1), native: false));
                f.Audio.CancelNative(); Assert.AreEqual(1, f.Audio.ActiveVoices); f.Contact();
                Assert.AreEqual(3, f.Audio.ContactLayerCount); Assert.AreEqual(3, f.Post.Length);
            }
        }

        [Test] public void NullInputsAndRepeatedDisposeCannotResurrectOldResources()
        {
            using (var f = new StarterAudioFixture())
            {
                Assert.IsFalse(f.Audio.Play(null, .2f, false));
                Assert.IsTrue(f.Play()); f.Audio.SetZone(null); Assert.AreEqual(0, f.Audio.ActiveVoices);
                Assert.IsFalse(f.Play()); f.Audio.SetZone(f.Zone); Assert.IsTrue(f.Play());
                f.Audio.Dispose(); f.Audio.Dispose(); f.Audio.SetZone(f.Zone);
                Assert.IsFalse(f.Audio.Prepare()); Assert.IsFalse(f.Play()); Assert.AreEqual(0, f.Audio.AllocatedSources);
            }
        }

        [TestCase("unsupported")] [TestCase("wrong-zone")] [TestCase("hidden")]
        [TestCase("off")] [TestCase("muted")] [TestCase("paused")]
        public void SuppressedRequestHasNoPrefixAssignmentOrAcceptedCount(string reason)
        {
            using (var f = new StarterAudioFixture())
            {
                var sequence = f.Sequence(reason == "unsupported" ? "Pyromancy_EmberSpit" : StarterAudioFixture.Calm);
                if (reason == "wrong-zone") f.Audio.SetZone(new Zone("wrong"));
                if (reason == "hidden") foreach (var c in f.Zone.Cells) c.IsVisible = false;
                if (reason == "off") SpellFxSettings.Mode = SpellFxMode.Off;
                if (reason == "muted") SpellFxSettings.SoundVolume = 0;
                if (reason == "paused") AudioListener.pause = true;
                Assert.IsFalse(f.Play(sequence)); Assert.AreEqual(0, f.Audio.AcceptedCount);
                Assert.AreEqual(0, f.Assigned.Length); Assert.AreEqual(0, f.Audio.ActiveVoices);
            }
        }

        [TestCase(StarterAudioFixture.Jet, 3)] [TestCase(StarterAudioFixture.Surge, 2)]
        [TestCase(StarterAudioFixture.Rime, 1)] [TestCase(StarterAudioFixture.Calm, 2)]
        [TestCase(StarterAudioFixture.Rain, 0)]
        public void VisibleRemoteReactionCannotImpersonateASelectedTarget(string spell, int expected)
        {
            using (var f = new StarterAudioFixture())
            {
                var remote = StarterAudioFixture.Target(spell == StarterAudioFixture.Rime ? "FrozenEffect" : "Pacified",
                    x: 20, finalX: 21);
                Assert.AreEqual(spell != StarterAudioFixture.Rain, f.Play(f.Sequence(spell, targets: new[] { remote })));
                f.Contact(); Assert.AreEqual(expected, f.Audio.ContactLayerCount); Assert.AreEqual(expected, f.Post.Length);
            }
        }

        [TestCase(StarterAudioFixture.Jet, 3)] [TestCase(StarterAudioFixture.Surge, 2)]
        [TestCase(StarterAudioFixture.Rime, 1)] [TestCase(StarterAudioFixture.Calm, 2)]
        [TestCase(StarterAudioFixture.Rain, 0)]
        public void HidingTargetBeforeContactSkipsConditionalSoundPermanently(string spell, int expected)
        {
            using (var f = new StarterAudioFixture())
            {
                Assert.IsTrue(f.Play(f.Sequence(spell)));
                int x = spell == StarterAudioFixture.Jet ? 12 : 13;
                f.Zone.GetCell(x, 10).IsVisible = false; f.Contact();
                Assert.AreEqual(expected, f.Audio.ContactLayerCount);
                f.Zone.GetCell(x, 10).IsVisible = true; f.Audio.Update(.1f, .1f, .1f);
                Assert.AreEqual(expected, f.Audio.ContactLayerCount);
                Assert.AreEqual(expected, f.Post.Length);
            }
        }

        [TestCase("stationary")] [TestCase("dead")]
        [TestCase("hidden-origin")] [TestCase("hidden-destination")] [TestCase("unexplored-destination")]
        public void DisplacementRequiresLivingMovedOwnerAndBothVisibleCopiedCells(string reason)
        {
            using (var f = new StarterAudioFixture())
            {
                var target = StarterAudioFixture.Target(died: reason == "dead", finalX: reason == "stationary" ? 13 : 14);
                Assert.IsTrue(f.Play(f.Sequence(StarterAudioFixture.Surge, targets: new[] { target })));
                if (reason == "hidden-origin") f.Zone.GetCell(13, 10).IsVisible = false;
                if (reason == "hidden-destination") f.Zone.GetCell(14, 10).IsVisible = false;
                if (reason == "unexplored-destination") f.Zone.GetCell(14, 10).Explored = false;
                f.Contact(); Assert.AreEqual(2, f.Audio.ContactLayerCount);
                Assert.IsFalse(f.Post.Any(s => s.clip.name.Contains("grit_displacement")));
            }
        }

        [TestCase(true, 3)] [TestCase(false, 2)]
        public void MovedSurgeOwnerAtExactPathCellNeedsCopiedDirectSelection(bool direct, int expected)
        {
            using (var f = new StarterAudioFixture())
            {
                var target = StarterAudioFixture.Target(x: 13, finalX: 14, direct: direct);
                Assert.IsTrue(target.Moved);
                var sequence = f.Sequence(StarterAudioFixture.Surge, targets: new[] { target });
                Assert.Contains(target.Cell, sequence.Path.ToArray());
                Assert.IsTrue(f.Zone.GetCell(13, 10).IsVisible && f.Zone.GetCell(14, 10).IsVisible);
                Assert.IsTrue(f.Play(sequence)); Assert.AreEqual(0, f.Post.Length); f.Contact();
                Assert.AreEqual(expected, f.Audio.ContactLayerCount); Assert.AreEqual(expected, f.Post.Length);
                Assert.AreEqual(direct, f.Post.Any(source => source.clip.name.Contains("grit_displacement")));
            }
        }

        [TestCase("Frozen")] [TestCase("rejected")] [TestCase("dead")] [TestCase("earlier-path-cell")]
        public void RimeLockRequiresExactAppliedFrozenOnLivingEndpoint(string reason)
        {
            using (var f = new StarterAudioFixture())
            {
                var target = StarterAudioFixture.Target(reason == "Frozen" ? "Frozen" : reason == "rejected" ? null : "FrozenEffect",
                    died: reason == "dead", x: reason == "earlier-path-cell" ? 12 : 13,
                    finalX: reason == "earlier-path-cell" ? 12 : 13, rejected: reason == "rejected" ? "FrozenEffect" : null);
                Assert.IsTrue(f.Play(f.Sequence(StarterAudioFixture.Rime, targets: new[] { target })));
                f.Contact(); Assert.AreEqual(1, f.Audio.ContactLayerCount);
                Assert.IsFalse(f.Post.Any(s => s.clip.name.Contains("brittle_lock") || s.clip.name.Contains("settling_fragments")));
            }
        }

        [TestCase("rejected")] [TestCase("dead")] [TestCase("earlier-path-cell")]
        public void CalmResolutionRequiresAppliedPacifiedOnLivingEndpoint(string reason)
        {
            using (var f = new StarterAudioFixture())
            {
                var target = StarterAudioFixture.Target(reason == "rejected" ? null : "Pacified", died: reason == "dead",
                    x: reason == "earlier-path-cell" ? 12 : 13, finalX: reason == "earlier-path-cell" ? 12 : 13,
                    rejected: reason == "rejected" ? "Pacified" : null);
                Assert.IsTrue(f.Play(f.Sequence(targets: new[] { target }))); f.Contact();
                Assert.AreEqual(2, f.Audio.ContactLayerCount);
                Assert.IsFalse(f.Post.Any(s => s.clip.name.Contains("resolved_overtone")));
            }
        }

        [TestCase("valid", 3)] [TestCase("zero", 1)] [TestCase("wrong-kind", 1)] [TestCase("off-path", 1)]
        public void WaterOnlyFreezeRequiresPositiveActualReactionInsideCopiedPath(string reason, int count)
        {
            using (var f = new StarterAudioFixture())
            {
                var reaction = new SpellFxCellResult(new Point(reason == "off-path" ? 20 : 12, 10),
                    reason == "wrong-kind" ? "coating" : "reaction", "freeze_water", reason == "zero" ? 0 : 1);
                Assert.IsTrue(f.Play(f.Sequence(StarterAudioFixture.Rime, success: false, reactions: new[] { reaction })));
                f.Contact(); Assert.AreEqual(count, f.Audio.ContactLayerCount); Assert.AreEqual(count, f.Post.Length);
            }
        }

        [Test] public void RainWithoutCopiedAffectedCropCannotMakeAnySound()
        {
            using (var f = new StarterAudioFixture())
            {
                Assert.IsFalse(f.Play(f.Sequence(StarterAudioFixture.Rain, affected: new[] { new Point(12, 10) })));
                f.Contact(); Assert.AreEqual(0, f.Audio.ContactLayerCount); Assert.AreEqual(0, f.Audio.ActiveVoices);
                Assert.IsTrue(f.Play(f.Sequence(StarterAudioFixture.Rain))); f.Contact();
                Assert.AreEqual(3, f.Audio.ContactLayerCount);
            }
        }

        [Test] public void DuplicateInvalidPathTailCannotReplaceFilteredEndpoint()
        {
            using (var f = new StarterAudioFixture())
            {
                var sequence = f.Sequence(path: new[] { new Point(10, 10), new Point(11, 10), new Point(13, 10),
                    new Point(11, 10), new Point(-1, -1), new Point(80, 25) });
                Assert.IsTrue(f.Play(sequence)); f.Contact(); Assert.AreEqual(3, f.Audio.ContactLayerCount);
            }
        }

        [Test] public void AllConditionalPrefixesAreAbsentBeforeContact()
        {
            using (var f = new StarterAudioFixture())
            {
                foreach (string spell in new[] { StarterAudioFixture.Jet, StarterAudioFixture.Surge,
                    StarterAudioFixture.Rime, StarterAudioFixture.Calm, StarterAudioFixture.Rain })
                {
                    Assert.IsTrue(f.Play(f.Sequence(spell)));
                    Assert.IsFalse(f.Assigned.Any(s => s.clip.name.Contains("wet_slap") || s.clip.name.Contains("grit_displacement") ||
                        s.clip.name.Contains("brittle_lock") || s.clip.name.Contains("settling_fragments") || s.clip.name.Contains("resolved_overtone")));
                    if (spell == StarterAudioFixture.Rain) Assert.AreEqual(0, f.Assigned.Length);
                    f.Audio.ClearAll();
                }
            }
        }

        [Test] public void SlowCalmKeepsItsAudibleTailPastFiveSecondsThenFinishes()
        {
            using (var f = new StarterAudioFixture())
            {
                SpellFxSettings.AnimationSpeed = .25f; Assert.IsTrue(f.Play());
                for (int i = 0; i < 51; i++) f.Audio.Update(.1f, .025f, .025f);
                Assert.AreEqual(3, f.Audio.ContactLayerCount); Assert.AreEqual(1, f.Audio.ActiveVoices);
                for (int i = 0; i < 54; i++) f.Audio.Update(.1f, .025f, .025f);
                Assert.AreEqual(0, f.Audio.ActiveVoices); Assert.AreEqual(0, f.Assigned.Length);
            }
        }

        [Test] public void SteadyPreparedClockDoesNotAllocateManagedMemory()
        {
            using (var f = new StarterAudioFixture())
            {
                Assert.IsTrue(f.Play()); f.Audio.Update(0, 0, 0);
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 1000; i++) f.Audio.Update(.0001f, .0001f, .0001f);
                Assert.AreEqual(0, GC.GetAllocatedBytesForCurrentThread() - before);
                Assert.AreEqual(1, f.Audio.ActiveVoices); Assert.AreEqual(0, f.Audio.ContactLayerCount);
            }
        }

        [TestCase("hide")] [TestCase("zone")] [TestCase("clear")]
        public void CoordinatorCancellationClearsBothFamiliesSymmetrically(string reason)
        {
            using (var f = new StarterAudioFixture()) using (var world = f.World())
            {
                world.Play(f.Sequence()); world.Play(f.Sequence("Pyromancy_EmberSpit"));
                Assert.AreEqual(1, world.StarterAudio.ActiveVoices); Assert.AreEqual(1, world.EmberAudio.ActiveVoices);
                if (reason == "hide") world.Update(.1f, false, false);
                if (reason == "zone") world.SetZone(new Zone("replacement"));
                if (reason == "clear") world.CancelAll();
                Assert.AreEqual(0, world.StarterAudio.ActiveVoices); Assert.AreEqual(0, world.EmberAudio.ActiveVoices);
                world.Update(.4f, false, reason != "hide");
                Assert.AreEqual(0, world.StarterAudio.ContactLayerCount); Assert.AreEqual(0, world.EmberAudio.ImpactCount);
            }
        }
    }
}
