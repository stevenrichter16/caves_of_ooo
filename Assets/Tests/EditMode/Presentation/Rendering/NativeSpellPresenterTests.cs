using System;
using System.Collections;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class NativeSpellPresenterTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        sealed class Fixture : IDisposable
        {
            readonly Village3DIntegrationFixture town;
            readonly SpawnRing3DIntegrationFixture ring;
            readonly SpellFxMode oldMode;
            readonly float oldSpeed;
            public readonly MonoBehaviour Presenter;
            public readonly Entity Player;
            public readonly Zone Zone;
            public readonly GameObject Actor;
            public readonly Animator Animator;
            public Fixture(bool inTown)
            {
                oldMode = SpellFxSettings.Mode; oldSpeed = SpellFxSettings.AnimationSpeed;
                SpellFxSettings.Mode = SpellFxMode.Full; SpellFxSettings.AnimationSpeed = 1;
                if (inTown) { town = new Village3DIntegrationFixture(); Presenter = town.Presenter; Player = town.Player; Zone = town.Zone; Assert.IsTrue(town.Presenter.TryGetOwnerView("$player", out var owner, out var root)); Assert.AreSame(Player, owner); Actor = root; }
                else { ring = new SpawnRing3DIntegrationFixture(); Presenter = ring.Presenter; Player = ring.Player; Zone = ring.Zone; Actor = ring.View(Player); }
                Animator = Actor.GetComponentInChildren<Animator>(true); Assert.NotNull(Animator);
            }
            public void Refresh() { if (town != null) town.Refresh(); else ring.Refresh(); }
            public void Hide() => Presenter.GetType().GetMethod("SetPresentationVisible").Invoke(Presenter, new object[] { false });
            public void Cast(string spell = "Pyromancy_EmberSpit", int dx = 1, int dy = 0)
            { var p = Zone.GetEntityPosition(Player); EntityVisualHooks.EmitCast(Player, Zone, spell, p.x, p.y, p.x + dx, p.y + dy, .66f); }
            public NativeSpellCastPlayer CastPlayer()
            {
                var view = ViewState();
                var cast = view.GetType().GetField("Cast"); Assert.NotNull(cast, "A real native view must own its reversible spell gesture.");
                return (NativeSpellCastPlayer)cast.GetValue(view);
            }
            public object ViewState()
            {
                var field = Presenter.GetType().GetField(town != null ? "byEntity" : "views", Private);
                var view = ((IDictionary)field.GetValue(Presenter))[Player]; Assert.NotNull(view);
                return view;
            }
            public NativeZone3DRenderSurface Surface()
            { var p = Presenter.GetType().GetProperty("ActiveSurface"); Assert.NotNull(p, "Coordinator must receive the actual visible native surface."); return (NativeZone3DRenderSurface)p.GetValue(Presenter); }
            public void Dispose() { town?.Dispose(); ring?.Dispose(); SpellFxSettings.Mode = oldMode; SpellFxSettings.AnimationSpeed = oldSpeed; }
        }

        [TestCase(true)] [TestCase(false)]
        public void KnownSpellHook_PlaysActualImportedGestureAndRefreshKeepsIt(bool town)
        {
            using (var f = new Fixture(town))
            {
                var original = f.Animator.runtimeAnimatorController; f.Cast();
                Assert.AreNotSame(original, f.Animator.runtimeAnimatorController);
                Assert.IsTrue(f.CastPlayer().IsActive); f.Refresh(); Assert.IsTrue(f.CastPlayer().IsActive);
            }
        }
        [TestCase(true)] [TestCase(false)]
        public void HidingPresentation_ClearsGestureAndRestoresBorrowedAnimator(bool town)
        {
            using (var f = new Fixture(town))
            {
                var original = f.Animator.runtimeAnimatorController; f.Cast(); Assert.IsTrue(f.CastPlayer().IsActive);
                f.Hide(); Assert.IsFalse(f.CastPlayer().IsActive); Assert.AreSame(original, f.Animator.runtimeAnimatorController);
            }
        }
        [TestCase(true)] [TestCase(false)]
        public void ActualAttackHook_InterruptsCast(bool town)
        {
            using (var f = new Fixture(town))
            {
                f.Cast(); Assert.IsTrue(f.CastPlayer().IsActive);
                EntityVisualHooks.EmitAttack(f.Player, f.Player, f.Zone); Assert.IsFalse(f.CastPlayer().IsActive);
            }
        }
        [TestCase(true)] [TestCase(false)]
        public void UnknownSpell_RetainsFallbackWithoutOverridingTheController(bool town)
        {
            using (var f = new Fixture(town))
            { var original = f.Animator.runtimeAnimatorController; f.Cast("not-an-authored-spell"); Assert.AreSame(original, f.Animator.runtimeAnimatorController); }
        }
        [TestCase(true)] [TestCase(false)]
        public void OnlyActuallyVisibleSurface_IsExposedToTheCoordinator(bool town)
        {
            using (var f = new Fixture(town))
            { Assert.NotNull(f.Surface()); Assert.IsTrue(f.Surface().IsVisible); f.Hide(); Assert.IsNull(f.Surface()); }
        }
        [TestCase(true)] [TestCase(false)]
        public void CastHook_FacesAllEightCapturedDirections(bool town)
        {
            using (var f = new Fixture(town))
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    f.Cast(dx: dx, dy: dy);
                    Assert.Greater(Vector3.Dot(f.Actor.transform.forward, new Vector3(dx, 0, -dy).normalized), .999f);
                }
        }
        [TestCase(true)] [TestCase(false)]
        public void CastingDuringMovement_SnapsToTheResolvedCellBeforeStartingTheGesture(bool town)
        {
            using (var f = new Fixture(town))
            {
                var view = f.ViewState(); var fields = view.GetType();
                var target = (Vector3)fields.GetField("Target").GetValue(view);
                fields.GetField("MoveDuration").SetValue(view, .1f);
                fields.GetField("MoveStart").SetValue(view, Time.unscaledTime);
                f.Actor.transform.position = target + Vector3.left * .35f;
                Assert.Greater(Vector3.Distance(f.Actor.transform.position, target), .3f);
                f.Cast(); Assert.IsTrue(f.CastPlayer().IsActive);
                Assert.Less(Vector3.Distance(f.Actor.transform.position, target), .001f,
                    "A cast interrupts tweening at the already resolved gameplay cell, where its effect originates.");
            }
        }
    }
}
