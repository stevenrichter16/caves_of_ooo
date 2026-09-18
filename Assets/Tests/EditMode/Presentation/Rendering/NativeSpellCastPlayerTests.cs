#if UNITY_EDITOR
using System;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class NativeSpellCastPlayerTests
    {
        private GameObject actor;
        private Animator animator;
        private AnimationClip clip;
        private object player;
        private Type type;
        private SpellFxMode savedMode;
        private float savedSpeed;

        [SetUp]
        public void SetUp()
        {
            savedMode = SpellFxSettings.Mode; savedSpeed = SpellFxSettings.AnimationSpeed;
            SpellFxSettings.Mode = SpellFxMode.Full; SpellFxSettings.AnimationSpeed = 1;
            var library = Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath);
            Assert.NotNull(library);
            actor = Object.Instantiate(library.FindModel(library.PlayerModelId));
            animator = actor.GetComponentInChildren<Animator>(true);
            Assert.NotNull(animator);
            var bones = actor.GetComponentInChildren<SkinnedMeshRenderer>(true).bones;
            var path = AnimationUtility.CalculateTransformPath(bones[1], animator.transform);
            clip = new AnimationClip { name = "Test cast", frameRate = 100 };
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), "localEulerAnglesRaw.x"),
                new AnimationCurve(new Keyframe(0, 0), new Keyframe(.22f, 12), new Keyframe(.66f, 0)));
            type = typeof(Village3DLibrary).Assembly.GetType("CavesOfOoo.Rendering.NativeSpellCastPlayer");
            Assert.NotNull(type, "Native casters must play distinct authored spell gestures with reversible ownership.");
            player = Activator.CreateInstance(type, new object[] { animator });
        }

        [TearDown]
        public void TearDown()
        {
            (player as IDisposable)?.Dispose();
            if (actor != null) Object.DestroyImmediate(actor);
            if (clip != null) Object.DestroyImmediate(clip);
            SpellFxSettings.Mode = savedMode; SpellFxSettings.AnimationSpeed = savedSpeed;
        }

        private object Call(string name, params object[] args)
        {
            var method = type.GetMethod(name, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(method, name);
            return method.Invoke(player, args);
        }
        private bool Active => (bool)type.GetProperty("IsActive").GetValue(player);
        private bool Begin(float duration = .66f) => (bool)Call("TryPlay", clip, duration);

        [Test]
        public void Cast_OverridesOwnedControllerThenRestoresOriginalControllerAndSpeed()
        {
            var original = animator.runtimeAnimatorController; animator.speed = .73f;
            Assert.IsTrue(Begin(1.32f));
            Assert.AreNotSame(original, animator.runtimeAnimatorController);
            Assert.That(animator.speed, Is.EqualTo(.5f).Within(.001f));
            Call("Clear");
            Assert.AreSame(original, animator.runtimeAnimatorController);
            Assert.That(animator.speed, Is.EqualTo(.73f).Within(.001f));
            Assert.IsFalse(Active);
            Call("Clear");
            Assert.AreSame(original, animator.runtimeAnimatorController);
        }

        [TestCase(0f)] [TestCase(-1f)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)]
        public void InvalidDuration_DoesNotReplaceTheController(float duration)
        {
            var original = animator.runtimeAnimatorController;
            Assert.IsFalse(Begin(duration));
            Assert.AreSame(original, animator.runtimeAnimatorController);
            Assert.IsFalse(Active);
        }

        [Test]
        public void MissingClip_DoesNotCancelAnAlreadyPlayingValidCast()
        {
            Assert.IsTrue(Begin()); var owned = animator.runtimeAnimatorController;
            Assert.IsFalse((bool)Call("TryPlay", null, .66f));
            Assert.AreSame(owned, animator.runtimeAnimatorController);
            Assert.IsTrue(Active);
        }

        [Test]
        public void RepeatingSameSpell_RestartsItsPoseClock()
        {
            Assert.IsTrue(Begin()); Call("Tick", .4f);
            Assert.IsTrue(Begin()); Call("Tick", .4f);
            Assert.IsTrue(Active);
            Call("Tick", .3f); Assert.IsFalse(Active);
        }

        [Test]
        public void TurningEffectsOff_ClearsCastAndRestoresController()
        {
            var original = animator.runtimeAnimatorController;
            Assert.IsTrue(Begin()); SpellFxSettings.Mode = SpellFxMode.Off;
            Call("Tick", 0f);
            Assert.IsFalse(Active); Assert.AreSame(original, animator.runtimeAnimatorController);
        }

        [Test]
        public void ClearingWorldEffects_ClearsCastOnTheNextTick()
        {
            Assert.IsTrue(Begin()); AsciiFxBus.Clear(); Call("Tick", 0f);
            Assert.IsFalse(Active);
        }

        [Test]
        public void SpeedChange_UpdatesAnimationAndCompletionTogether()
        {
            Assert.IsTrue(Begin()); Call("Tick", .2f);
            SpellFxSettings.AnimationSpeed = 2;
            Call("Tick", .2f);
            Assert.IsTrue(Active); Assert.That(animator.speed, Is.EqualTo(2).Within(.001f));
            Call("Tick", .04f); Assert.IsFalse(Active);
        }

        [TestCase(2f, .33f)] [TestCase(.5f, 1.32f)]
        public void InitialSpeed_UsesTheAlreadyScaledWallDurationExactlyOnce(float initialSpeed, float wallDuration)
        {
            SpellFxSettings.AnimationSpeed = initialSpeed;
            Assert.IsTrue(Begin(wallDuration));
            Assert.That(animator.speed, Is.EqualTo(initialSpeed).Within(.001f));
            Call("Tick", wallDuration * .6f); Assert.IsTrue(Active);
            Call("Tick", wallDuration * .41f); Assert.IsFalse(Active);
        }

        [TestCase(false)] [TestCase(true)]
        public void CompletedOrClearedCasts_ReuseTheirPrivateController(bool explicitClear)
        {
            var original = animator.runtimeAnimatorController;
            Assert.IsTrue(Begin()); var owned = animator.runtimeAnimatorController;
            if (explicitClear) Call("Clear"); else Call("Tick", .7f);
            Assert.AreSame(original, animator.runtimeAnimatorController);
            Assert.IsFalse(Active);
            Assert.IsTrue(Begin());
            Assert.AreSame(owned, animator.runtimeAnimatorController, "Repeated casts reuse this actor's private override allocation.");
        }

        [TestCase(false)] [TestCase(true)]
        public void DisposedPlayer_RestoresBorrowedStateAndDestroysOnlyItsOwnController(bool clearFirst)
        {
            var original = animator.runtimeAnimatorController;
            Assert.IsTrue(Begin()); var owned = animator.runtimeAnimatorController;
            if (clearFirst) Call("Clear");
            ((IDisposable)player).Dispose(); ((IDisposable)player).Dispose();
            Assert.AreSame(original, animator.runtimeAnimatorController);
            Assert.IsTrue(owned == null, "Dispose releases the per-view override, including its inactive cache.");
            Assert.IsTrue(original != null); Assert.IsFalse(Active); Assert.IsFalse(Begin());
        }

        [Test]
        public void SeparateActors_NeverMutateOrShareTheirPrivateOverrides()
        {
            var original = animator.runtimeAnimatorController;
            var otherActor = Object.Instantiate(actor);
            var otherAnimator = otherActor.GetComponentInChildren<Animator>(true);
            var other = new NativeSpellCastPlayer(otherAnimator);
            try
            {
                Assert.AreSame(original, otherAnimator.runtimeAnimatorController);
                Assert.IsTrue(Begin());
                Assert.AreSame(original, otherAnimator.runtimeAnimatorController);
                Assert.IsTrue(other.TryPlay(clip, .66f));
                Assert.AreNotSame(animator.runtimeAnimatorController, otherAnimator.runtimeAnimatorController);
                Call("Clear"); Assert.IsTrue(other.IsActive);
            }
            finally { other.Dispose(); Object.DestroyImmediate(otherActor); }
        }

        [Test]
        public void ReplacedBorrowedController_IsUsedForTheNextCastAndRestoredAfterward()
        {
            var original = animator.runtimeAnimatorController;
            var replacement = Object.Instantiate(original);
            try
            {
                Assert.IsTrue(Begin()); var firstOwned = animator.runtimeAnimatorController;
                Call("Clear");
                animator.runtimeAnimatorController = replacement; animator.speed = .43f;
                Assert.IsTrue(Begin()); Assert.AreNotSame(firstOwned, animator.runtimeAnimatorController);
                Assert.IsTrue(firstOwned == null, "Replacing the borrowed controller releases the outdated override cache.");
                Call("Clear"); Assert.AreSame(replacement, animator.runtimeAnimatorController);
                Assert.That(animator.speed, Is.EqualTo(.43f).Within(.001f));
            }
            finally { Call("Clear"); animator.runtimeAnimatorController = original; Object.DestroyImmediate(replacement); }
        }

        [Test]
        public void BorrowedOverrideController_KeepsOtherOverridesAndItsOwnInteractClip()
        {
            var original = animator.runtimeAnimatorController;
            var borrowed = new AnimatorOverrideController(original);
            var idleClip = Object.Instantiate(clip); idleClip.name = "Existing idle override";
            try
            {
                borrowed["Idle"] = idleClip;
                var originalInteract = borrowed["Interact"];
                animator.runtimeAnimatorController = borrowed;
                Assert.IsTrue(Begin());
                var owned = animator.runtimeAnimatorController as AnimatorOverrideController;
                Assert.NotNull(owned); Assert.AreNotSame(borrowed, owned);
                Assert.AreSame(idleClip, owned["Idle"]); Assert.AreSame(clip, owned["Interact"]);
                Assert.AreSame(originalInteract, borrowed["Interact"]);
                Call("Clear"); Assert.AreSame(borrowed, animator.runtimeAnimatorController);
            }
            finally { Call("Clear"); animator.runtimeAnimatorController = original; Object.DestroyImmediate(borrowed); Object.DestroyImmediate(idleClip); }
        }

        [Test]
        public void CastClock_BorrowsUnscaledUpdateModeThenRestoresIt()
        {
            animator.updateMode = AnimatorUpdateMode.Normal;
            Assert.IsTrue(Begin()); Assert.AreEqual(AnimatorUpdateMode.UnscaledTime, animator.updateMode);
            Call("Clear"); Assert.AreEqual(AnimatorUpdateMode.Normal, animator.updateMode);
        }

        [TestCase(-1f)] [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)]
        public void InvalidTickDelta_DoesNotAdvanceOrPoisonTheCast(float delta)
        {
            Assert.IsTrue(Begin()); Call("Tick", delta); Call("Tick", .5f);
            Assert.IsTrue(Active); Call("Tick", .2f); Assert.IsFalse(Active);
        }
    }
}
#endif
