using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CavesOfOoo.Tests
{
    internal sealed class StarterAudioFixture : IDisposable
    {
        public const string Hands = "Pyromancy_FlamingHands", Jet = "Hydromancy_JetBlast",
            Surge = "Galvanism_GroundSurge", Rime = "Cryomancy_RimeGrip",
            Calm = "Spellcraft_Calm", Rain = "Hydromancy_ConjureRain";
        public readonly GameObject Host = new GameObject("Starter audio test");
        public readonly Zone Zone = new Zone("starter-audio");
        public readonly StarterSpellAudioPlayer Audio;
        readonly float volume = SpellFxSettings.SoundVolume, speed = SpellFxSettings.AnimationSpeed;
        readonly SpellFxMode mode = SpellFxSettings.Mode;
        readonly bool pause = AudioListener.pause;
        public StarterAudioFixture()
        {
            SpellFxSettings.SoundVolume = 1; SpellFxSettings.AnimationSpeed = 1;
            SpellFxSettings.Mode = SpellFxMode.Full; AudioListener.pause = false;
            foreach (var cell in Zone.Cells) { cell.Explored = true; cell.IsVisible = true; }
            Audio = new StarterSpellAudioPlayer(Host.transform); Audio.SetZone(Zone);
        }
        public static SpellFxTargetResult Target(string effect = null, bool died = false,
            int x = 13, int y = 10, int finalX = 13, int finalY = 10,
            string rejected = null, string id = "copied-owner", bool direct = true) =>
            new SpellFxTargetResult(id, new Point(x, y), new Point(finalX, finalY),
                damage: 0, died: died, appliedEffects: effect == null ? null : new[] { effect },
                rejectedEffects: rejected == null ? null : new[] { rejected }, isDirectTarget: direct);
        public SpellFxSequence Sequence(string spell = Calm, bool success = true, int seed = 0,
            IEnumerable<Point> path = null, IEnumerable<Point> affected = null,
            IEnumerable<SpellFxTargetResult> targets = null, IEnumerable<SpellFxCellResult> reactions = null)
        {
            int endpoint = spell == Hands ? 11 : spell == Jet ? 12 : 13;
            if (targets == null)
                targets = !success ? Array.Empty<SpellFxTargetResult>() : new[] {
                    Target(spell == Rime ? "FrozenEffect" : spell == Calm ? "Pacified" : null,
                        x: endpoint, finalX: spell == Surge ? 14 : endpoint) };
            return new SpellFxSequence(spell, Zone, null, new Point(10, 10),
                path ?? Enumerable.Range(11, endpoint - 10).Select(x => new Point(x, 10)),
                affected ?? new[] { new Point(endpoint, 10) }, targets, cosmeticSeed: seed, reactions: reactions);
        }
        public bool Play(SpellFxSequence sequence = null, bool native = true) =>
            Audio.Play(sequence ?? Sequence(), .295f, native);
        public AudioSource[] Assigned => Audio.Root == null ? Array.Empty<AudioSource>() :
            Audio.Root.GetComponentsInChildren<AudioSource>().Where(s => s.clip != null).ToArray();
        public AudioSource[] Post => Assigned.Where(s => s.clip.name.EndsWith("_post", StringComparison.Ordinal)).ToArray();
        public void Contact() => Audio.Update(.3f, .3f, .3f);
        public WorldFxCoordinator World()
        {
            var grid = new GameObject("Audio grid", typeof(Grid)); grid.transform.SetParent(Host.transform);
            var map = new GameObject("Audio FX", typeof(Tilemap), typeof(TilemapRenderer)); map.transform.SetParent(grid.transform);
            var world = new WorldFxCoordinator(new AsciiFxRenderer(map.GetComponent<Tilemap>()), Host.transform);
            world.SetZone(Zone); world.Update(0, false); return world;
        }
        public void Dispose()
        {
            Audio.Dispose(); UnityEngine.Object.DestroyImmediate(Host);
            SpellFxSettings.SoundVolume = volume; SpellFxSettings.AnimationSpeed = speed;
            SpellFxSettings.Mode = mode; AudioListener.pause = pause;
        }
    }

    public sealed class StarterSpellAudioTests
    {
        [Test] public void PrepareOwnsExactlyThirtyTwoReusableSourcesAndIsIdempotent()
        {
            using (var f = new StarterAudioFixture())
            {
                Assert.IsTrue(f.Audio.IsPrepared); Assert.AreEqual(32, f.Audio.AllocatedSources);
                Assert.AreEqual(32, f.Audio.Root.GetComponentsInChildren<AudioSource>().Length);
                var root = f.Audio.Root; Assert.IsTrue(f.Audio.Prepare()); Assert.AreSame(root, f.Audio.Root);
                Assert.IsTrue(f.Audio.Root.GetComponentsInChildren<AudioSource>().All(s =>
                    !s.playOnAwake && !s.loop && s.spatialBlend == 0 && s.dopplerLevel == 0));
            }
        }

        [TestCase(StarterAudioFixture.Hands, 3, 2, 0)] [TestCase(StarterAudioFixture.Jet, 4, 2, 0)]
        [TestCase(StarterAudioFixture.Surge, 3, 2, 0)] [TestCase(StarterAudioFixture.Rime, 3, 1, 0)]
        [TestCase(StarterAudioFixture.Calm, 3, 2, 0)] [TestCase(StarterAudioFixture.Rain, 3, 0, 0)]
        [TestCase(StarterAudioFixture.Surge, 4, 2, 1)] [TestCase(StarterAudioFixture.Surge, 3, 2, 2)]
        public void SuccessStartsOnlyPrefixesThenExactlyOneSetOfContactSuffixes(string spell, int postCount, int prefixCount, int seed)
        {
            using (var f = new StarterAudioFixture())
            {
                Assert.IsTrue(f.Play(f.Sequence(spell, seed: seed))); Assert.AreEqual(1, f.Audio.AcceptedCount);
                Assert.AreEqual(0, f.Audio.ContactLayerCount); Assert.AreEqual(0, f.Post.Length);
                Assert.AreEqual(prefixCount, f.Assigned.Length, "Every authored cast prefix must actually be assigned.");
                Assert.IsTrue(f.Assigned.All(s => s.clip.name.EndsWith("_pre", StringComparison.Ordinal)));
                f.Audio.Update(.294f, .294f, .294f); Assert.AreEqual(0, f.Post.Length);
                f.Audio.Update(.002f, .002f, .002f);
                Assert.AreEqual(postCount, f.Audio.ContactLayerCount); Assert.AreEqual(postCount, f.Post.Length);
                Assert.IsTrue(f.Post.All(s => s.clip.frequency == 48000 && s.clip.channels == 1 && s.clip.samples > 0));
                for (int i = 0; i < 160; i++) f.Audio.Update(.02f, .02f, .02f);
                Assert.AreEqual(postCount, f.Audio.ContactLayerCount); Assert.AreEqual(0, f.Audio.ActiveVoices);
                Assert.AreEqual(0, f.Assigned.Length);
            }
        }

        [TestCase(StarterAudioFixture.Jet, 3)] [TestCase(StarterAudioFixture.Surge, 2)]
        [TestCase(StarterAudioFixture.Rime, 1)] [TestCase(StarterAudioFixture.Calm, 2)]
        [TestCase(StarterAudioFixture.Rain, 0)]
        public void NoCopiedOutcomeKeepsNeutralSoundButOmitsConditionalLayers(string spell, int expected)
        {
            using (var f = new StarterAudioFixture())
            {
                Assert.AreEqual(spell != StarterAudioFixture.Rain, f.Play(f.Sequence(spell, success: false)));
                f.Contact(); Assert.AreEqual(expected, f.Audio.ContactLayerCount); Assert.AreEqual(expected, f.Post.Length);
                Assert.IsFalse(f.Post.Any(s => s.clip.name.Contains("wet_slap") || s.clip.name.Contains("grit_displacement") ||
                    s.clip.name.Contains("brittle_lock") || s.clip.name.Contains("settling_fragments") || s.clip.name.Contains("resolved_overtone")));
            }
        }

        [TestCase(0, 1)] [TestCase(1, 2)] [TestCase(2, 3)] [TestCase(int.MinValue, 3)]
        public void SeedSelectsOneMatchedVariantForEveryAssignedLayer(int seed, int expected)
        {
            using (var f = new StarterAudioFixture())
            {
                Assert.IsTrue(f.Play(f.Sequence(seed: seed))); Assert.AreEqual(expected, f.Audio.LastVariant);
                f.Contact(); Assert.Greater(f.Assigned.Length, 0);
                Assert.IsTrue(f.Assigned.All(s => s.clip.name.Contains("_" + expected.ToString("D2") + "_")));
            }
        }

        [TestCase(true, 4)] [TestCase(false, 3)]
        public void AmbientAtExactJetCellCannotImpersonateTheCopiedDirectTarget(bool direct, int expected)
        {
            using (var f = new StarterAudioFixture())
            {
                var target = StarterAudioFixture.Target(x: 12, finalX: 12, direct: direct);
                var sequence = f.Sequence(StarterAudioFixture.Jet, targets: new[] { target });
                Assert.IsTrue(f.Play(sequence)); Assert.AreEqual(2, f.Assigned.Length);
                Assert.AreEqual(0, f.Post.Length); f.Contact();
                Assert.AreEqual(expected, f.Audio.ContactLayerCount); Assert.AreEqual(expected, f.Post.Length);
                Assert.AreEqual(direct, f.Post.Any(s => s.clip.name.Contains("wet_slap")));
            }
        }

        [Test] public void NativeContactUsesRecoveredClockWhileFallbackUsesOrdinaryClock()
        {
            using (var f = new StarterAudioFixture())
            {
                Assert.IsTrue(f.Play()); f.Audio.Update(.1f, .1f, .1f); f.Audio.Update(.5f, .5f, .15f);
                Assert.AreEqual(0, f.Audio.ContactLayerCount); Assert.AreEqual(0, f.Post.Length);
                f.Audio.Update(.05f, .05f, .05f); Assert.AreEqual(3, f.Audio.ContactLayerCount);
                f.Audio.ClearAll(); Assert.IsTrue(f.Play(native: false)); f.Audio.Update(.5f, .5f, .15f);
                Assert.AreEqual(6, f.Audio.ContactLayerCount);
            }
        }

        [Test] public void DuplicateMultiCellOwnerResultsAndReplayedSequenceDoNotMultiplyLayers()
        {
            using (var f = new StarterAudioFixture())
            {
                var target = StarterAudioFixture.Target("FrozenEffect");
                var sequence = f.Sequence(StarterAudioFixture.Rime, targets: new[] { target, target,
                    StarterAudioFixture.Target("FrozenEffect", x: 12, finalX: 12) });
                Assert.IsTrue(f.Play(sequence)); Assert.IsFalse(f.Play(sequence)); f.Contact();
                Assert.AreEqual(3, f.Audio.ContactLayerCount); Assert.AreEqual(3, f.Post.Length);
                Assert.AreEqual(1, f.Audio.AcceptedCount); Assert.AreEqual(1, f.Audio.ActiveVoices);
            }
        }

        [Test] public void FourVoiceLimitReusesThePoolAndSharedMixCountLowersAllAssignedLayers()
        {
            using (var f = new StarterAudioFixture())
            {
                Assert.IsTrue(f.Play()); float singleVoiceGain = f.Assigned[0].volume;
                Assert.Greater(singleVoiceGain, 0);
                for (int i = 1; i < 4; i++) Assert.IsTrue(f.Play(f.Sequence(seed: i)));
                Assert.IsFalse(f.Play(f.Sequence(seed: 99))); Assert.AreEqual(4, f.Audio.ActiveVoices);
                f.Audio.SetMixVoiceCount(8); f.Contact();
                Assert.IsTrue(f.Assigned.All(s => Mathf.Approximately(s.volume, singleVoiceGain / 8)));
                Assert.AreEqual(32, f.Audio.AllocatedSources);
                f.Audio.SetMixVoiceCount(-99); f.Audio.Update(0, 0, 0);
                Assert.IsTrue(f.Assigned.All(s => Mathf.Approximately(s.volume, singleVoiceGain / 4)));
                f.Audio.ClearAll(); f.Audio.SetMixVoiceCount(1); Assert.IsTrue(f.Play());
                Assert.IsTrue(f.Assigned.All(s => Mathf.Approximately(s.volume, singleVoiceGain)));
                Assert.AreEqual(32, f.Audio.AllocatedSources);
            }
        }

        [TestCase(.25f)] [TestCase(1f)] [TestCase(4f)]
        public void ContactFollowsVisualSpeedAndPitchIsBounded(float speed)
        {
            using (var f = new StarterAudioFixture())
            {
                SpellFxSettings.AnimationSpeed = speed; Assert.IsTrue(f.Play());
                f.Audio.Update(.29f / speed, .29f, .29f); Assert.AreEqual(0, f.Audio.ContactLayerCount);
                f.Audio.Update(.01f / speed, .01f, .01f); Assert.AreEqual(3, f.Audio.ContactLayerCount);
                Assert.IsTrue(f.Assigned.All(s => Mathf.Approximately(s.pitch, Mathf.Min(3, speed))));
            }
        }

        [TestCase(StarterAudioFixture.Calm, 3)] [TestCase(StarterAudioFixture.Rime, 3)]
        [TestCase(StarterAudioFixture.Surge, 3)]
        public void WarmContactGateStartsSuffixesWithoutManagedAllocation(string spell, int expected)
        {
            using (var f = new StarterAudioFixture())
            {
                Assert.IsTrue(f.Play(f.Sequence(spell))); f.Contact();
                Assert.AreEqual(expected, f.Audio.ContactLayerCount); f.Audio.ClearAll();
                Assert.IsTrue(f.Play(f.Sequence(spell)));
                long before = GC.GetAllocatedBytesForCurrentThread();
                f.Audio.Update(.3f, .3f, .3f);
                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                Assert.AreEqual(0, allocated, "Actual warmed contact must not box copied Point values.");
                Assert.AreEqual(expected * 2, f.Audio.ContactLayerCount);
            }
        }

        [TestCase(true, 3)] [TestCase(false, 0)]
        public void RainMayReserveSilentVisibleCasterCastButCropVisibilityIsDecidedAtContact(bool reveal, int expected)
        {
            using (var f = new StarterAudioFixture())
            {
                f.Zone.GetCell(13, 10).IsVisible = false;
                Assert.IsTrue(f.Play(f.Sequence(StarterAudioFixture.Rain)));
                Assert.AreEqual(0, f.Assigned.Length); Assert.AreEqual(0, f.Audio.ContactLayerCount);
                f.Zone.GetCell(13, 10).IsVisible = reveal; f.Contact();
                Assert.AreEqual(expected, f.Audio.ContactLayerCount); Assert.AreEqual(expected, f.Post.Length);
            }
        }

        [TestCase(true)] [TestCase(false)]
        public void DamagedOldFamilySourceCannotCancelAnUnrelatedCastBeforeTheNextUpdate(bool damageEmber)
        {
            using (var f = new StarterAudioFixture()) using (var world = f.World())
            {
                string oldSpell = damageEmber ? "Pyromancy_EmberSpit" : StarterAudioFixture.Calm;
                string newSpell = damageEmber ? StarterAudioFixture.Calm : "Pyromancy_EmberSpit";
                var oldPlayback = world.Play(f.Sequence(oldSpell));
                Assert.AreEqual(WorldFxPlaybackState.Playing, oldPlayback.State);
                Assert.AreEqual(1, damageEmber ? world.EmberAudio.ActiveVoices : world.StarterAudio.ActiveVoices);
                var oldRoot = damageEmber ? world.EmberAudio.Root : world.StarterAudio.Root;
                var damaged = oldRoot.GetComponentsInChildren<AudioSource>().First(source => source.clip != null);
                var warnings = new List<string>();
                Application.LogCallback observe = (message, stack, type) =>
                {
                    if (type == LogType.Warning || type == LogType.Error || type == LogType.Exception)
                        warnings.Add(type + ": " + message);
                };
                Application.logMessageReceived += observe;
                try
                {
                    UnityEngine.Object.DestroyImmediate(damaged);
                    Assert.IsFalse(damageEmber ? world.EmberAudio.IsPrepared : world.StarterAudio.IsPrepared);
                    // There is deliberately no Update between damage and the new family cast.
                    // RefreshAudioMix must not dereference the old family's destroyed source.
                    WorldFxPlayback next = null;
                    Assert.DoesNotThrow(() => next = world.Play(f.Sequence(newSpell)));
                    Assert.NotNull(next);
                    Assert.AreEqual(WorldFxPlaybackState.Playing, next.State, string.Join("\n", warnings));
                    Assert.AreEqual(0, warnings.Count, string.Join("\n", warnings));
                    world.Update(.3f, false);
                    Assert.AreEqual(0, damageEmber ? world.EmberAudio.ActiveVoices : world.StarterAudio.ActiveVoices);
                    Assert.AreEqual(1, damageEmber ? world.StarterAudio.ActiveVoices : world.EmberAudio.ActiveVoices);
                    Assert.AreEqual(damageEmber ? 3 : 1,
                        damageEmber ? world.StarterAudio.ContactLayerCount : world.EmberAudio.ImpactCount);
                    Assert.AreEqual(WorldFxPlaybackState.Playing, next.State);
                    world.Update(.3f, false);
                    Assert.AreEqual(WorldFxPlaybackState.Completed, next.State); Assert.IsFalse(world.HasBlockingFx);
                    Assert.AreEqual(0, warnings.Count, string.Join("\n", warnings));
                }
                finally { Application.logMessageReceived -= observe; }
            }
        }

        [Test] public void CoordinatorRoutesBothAudioFamiliesWithSharedGainAndNonblockingTails()
        {
            using (var f = new StarterAudioFixture()) using (var world = f.World())
            {
                world.Play(f.Sequence("Pyromancy_EmberSpit")); world.Update(0, false);
                float emberGain = world.EmberAudio.Root.GetComponentsInChildren<AudioSource>().First(s => s.clip != null).volume;
                Assert.Greater(emberGain, 0); world.CancelAll();
                var starter = world.Play(f.Sequence()); world.Update(0, false);
                float starterGain = world.StarterAudio.Root.GetComponentsInChildren<AudioSource>().First(s => s.clip != null).volume;
                Assert.Greater(starterGain, 0);
                var ember = world.Play(f.Sequence("Pyromancy_EmberSpit"));
                Assert.AreEqual(1, world.StarterAudio.ActiveVoices); Assert.AreEqual(1, world.EmberAudio.ActiveVoices);
                world.Update(.6f, false);
                Assert.IsTrue(starter.IsFinished); Assert.IsTrue(ember.IsFinished); Assert.IsFalse(world.HasBlockingFx);
                Assert.AreEqual(1, world.StarterAudio.ActiveVoices); Assert.AreEqual(1, world.EmberAudio.ActiveVoices);
                Assert.IsTrue(world.StarterAudio.Root.GetComponentsInChildren<AudioSource>().Where(s => s.clip != null)
                    .All(s => Mathf.Approximately(s.volume, starterGain / 2)));
                Assert.IsTrue(world.EmberAudio.Root.GetComponentsInChildren<AudioSource>().Where(s => s.clip != null)
                    .All(s => Mathf.Approximately(s.volume, emberGain / 2)));
                world.CancelAll(); Assert.AreEqual(0, world.StarterAudio.ActiveVoices); Assert.AreEqual(0, world.EmberAudio.ActiveVoices);
            }
        }
    }
}
