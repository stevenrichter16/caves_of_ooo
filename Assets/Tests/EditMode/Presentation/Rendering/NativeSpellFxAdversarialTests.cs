using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>Dedicated malformed-contract, ownership, interruption and deception
    /// gates. All stimuli are presentation records; no gameplay command is replayed.</summary>
    public sealed class NativeSpellFxAdversarialTests
    {
        [SetUp] public void Setup() { SpellFxSettings.Mode=SpellFxMode.Full;SpellFxSettings.AnimationSpeed=1; }
        [TearDown] public void Teardown() { SpellFxSettings.Mode=SpellFxMode.Full;SpellFxSettings.AnimationSpeed=1; }

        [TestCase("missing_mesh")] [TestCase("empty_samples")] [TestCase("nonfinite_position")]
        [TestCase("zero_quaternion")] [TestCase("negative_scale")] [TestCase("duplicate_piece")]
        [TestCase("duplicate_entry")] [TestCase("missing_material")] [TestCase("invalid_clock")]
        [TestCase("nonfinite_color")]
        public void MalformedAuthoredDataRejectsBeforeRenderingAndKeepsBorrowedAssets(string fault)
        {
            using(var f=new NativeSpellFixture())
            {
                switch(fault)
                {
                    case "missing_mesh":f.Entry.Pieces[0].Mesh=null;break;
                    case "empty_samples":f.Entry.Pieces[0].Poses=Array.Empty<NativeSpellFxPose>();break;
                    case "nonfinite_position":f.Entry.Pieces[0].Poses[30].Position=new Vector3(float.NaN,0,0);break;
                    case "zero_quaternion":f.Entry.Pieces[0].Poses[30].Rotation=new Quaternion(0,0,0,0);break;
                    case "negative_scale":f.Entry.Pieces[0].Poses[30].Scale=new Vector3(-1,1,1);break;
                    case "duplicate_piece":f.Entry.Pieces=new[]{f.Entry.Pieces[0],f.Entry.Pieces[0]};break;
                    case "duplicate_entry":f.Library.Entries=new[]{f.Entry,f.Entry};break;
                    case "missing_material":f.Library.Material=null;break;
                    case "invalid_clock":f.Entry.SampleRate=0;break;
                    case "nonfinite_color":f.Entry.Pieces[0].Color=new Color(float.PositiveInfinity,0,0,1);break;
                }
                Assert.Throws<InvalidOperationException>(()=>f.Library.Validate());
                f.Create();Assert.AreEqual(0,f.Fx.Play(f.Sequence()));Assert.AreEqual(0,f.Fx.ActiveCount);
                Assert.IsNotEmpty(f.Fx.Failure);Assert.IsTrue(f.Mesh!=null);
            }
        }
        [Test] public void WrongZoneCannotRouteAndDoesNotReplaceCurrentNativeSurface()
        {
            using(var f=new NativeSpellFixture())
            {
                f.Create();var foreign=new SpellFxSequence(f.Entry.SpellId,new Zone("foreign"),null,new Point(10,10),affectedCells:new[]{new Point(14,10)});
                Assert.AreEqual(0,f.Fx.Play(foreign));Assert.AreEqual(0,f.Fx.ActiveCount);
                Assert.Greater(f.Fx.Play(f.Sequence()),0);f.Fx.Update(.33f);Assert.AreEqual(1,f.Drawn().Length);
            }
        }
        [Test] public void AbsentOrHiddenSurfaceCannotEmitNativeFragments()
        {
            using(var f=new NativeSpellFixture())
            using(var fx=new NativeSpellFxRenderer(f.Library))
            {
                fx.SetZone(f.Zone);Assert.AreEqual(0,fx.Play(f.Sequence()));
                fx.SetSurface(f.Surface);f.Surface.Sync(f.Source,false,false);Assert.AreEqual(0,fx.Play(f.Sequence()));
                f.Surface.Sync(f.Source,true,false);Assert.Greater(fx.Play(f.Sequence()),0);
            }
        }
        [Test] public void ZoneChangeClearsPlayingAndScheduledWork()
        {
            using(var f=new NativeSpellFixture())
            { f.Create();f.Fx.Play(f.Sequence());f.Fx.Update(.33f);Assert.AreEqual(1,f.Drawn().Length);f.Fx.SetZone(new Zone("elsewhere"));Assert.AreEqual(0,f.Fx.ActiveCount);Assert.AreEqual(0,f.Drawn().Length); }
        }
        [Test] public void SurfaceUnbindingCancelsRecordsWithoutDestroyingSurfaceOrMeshes()
        {
            using(var f=new NativeSpellFixture())
            { f.Create();f.Fx.Play(f.Sequence());f.Fx.Update(.33f);f.Fx.SetSurface(null);Assert.AreEqual(0,f.Fx.ActiveCount);Assert.IsTrue(f.Surface.ContentRoot!=null);Assert.IsTrue(f.Mesh!=null); }
        }
        [Test] public void DisposedBackendIsTerminalAndIdempotent()
        {
            using(var f=new NativeSpellFixture())
            { f.Create();f.Fx.Play(f.Sequence());f.Fx.Dispose();f.Fx.Dispose();f.Fx.Update(.33f);Assert.AreEqual(0,f.Fx.Play(f.Sequence()));Assert.AreEqual(0,f.Fx.ActiveCount); }
        }
        [Test] public void ExternallyDestroyedEffectHierarchyDoesNotLeakOrHoldInput()
        {
            using(var f=new NativeSpellFixture())
            {
                f.Create();f.Fx.Play(f.Sequence());f.Fx.Update(.33f);Assert.NotNull(f.Fx.Root);
                Object.DestroyImmediate(f.Fx.Root.gameObject);Assert.DoesNotThrow(()=>f.Fx.Update(.01f));
                Assert.AreEqual(0,f.Fx.ActiveCount);Assert.IsFalse(f.Fx.HasBlockingFx);f.Fx.Dispose();Assert.IsTrue(f.Library.Material!=null);
            }
        }
        [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)] [TestCase(-1f)]
        public void NonfiniteOrNegativeFrameDeltaDoesNotJumpClockOrPoisonTransforms(float delta)
        {
            using(var f=new NativeSpellFixture())
            { f.Create();f.Fx.Play(f.Sequence());f.Fx.Update(.33f);Vector3 before=f.Drawn().Single().transform.position;f.Fx.Update(delta);Assert.AreEqual(before,f.Drawn().Single().transform.position);Assert.Greater(f.Fx.ActiveCount,0); }
        }
        [Test] public void OutOfBoundsCopiedContactIsSkippedInsteadOfClampedIntoVisibleWorld()
        {
            using(var f=new NativeSpellFixture())
            {
                f.Create();f.Fx.Play(f.Sequence(targets:new[]{NativeSpellFixture.Hit("valid",14,10),NativeSpellFixture.Hit("left",-1,10),NativeSpellFixture.Hit("right",80,10),NativeSpellFixture.Hit("below",14,25)}));
                f.Fx.Update(.33f);Assert.AreEqual(1,f.Drawn().Length);Assert.That(f.Drawn()[0].transform.position.x,Is.EqualTo(14.5f).Within(.001f));
            }
        }
        [Test] public void PassivePresentationNeverSetsBlockingFlag()
        {
            using(var f=new NativeSpellFixture())
            { f.Create();Assert.Greater(f.Fx.Play(f.Sequence(blocking:false)),0);Assert.Greater(f.Fx.ActiveCount,0);Assert.IsFalse(f.Fx.HasBlockingFx);f.Fx.ClearAll();f.Fx.Play(f.Sequence());Assert.IsTrue(f.Fx.HasBlockingFx); }
        }
        [Test] public void ExtremeBurstHonorsBoundedScheduledAndLiveBudgetsWithoutDroppingAllWork()
        {
            using(var f=new NativeSpellFixture())
            {
                f.Entry.Pieces=Enumerable.Range(0,1000).Select(i=>f.Piece("part-"+i,NativeSpellFxRole.TargetImpact,NativeSpellFxAnchor.Target)).ToArray();f.Create();
                Assert.Greater(f.Fx.Play(f.Sequence()),0);f.Fx.Update(.33f);Assert.Greater(f.Fx.ActiveMeshCount,0);
                Assert.LessOrEqual(f.Fx.ActiveCount,NativeSpellFxRenderer.MaximumScheduledFragments);
                Assert.LessOrEqual(f.Fx.ActiveMeshCount,NativeSpellFxRenderer.MaximumFullMeshes);
                f.Fx.ClearAll();Assert.AreEqual(0,f.Fx.ActiveCount);Assert.IsFalse(f.Fx.HasBlockingFx);
            }
        }

        [TestCase(.0001f)] [TestCase(200f)]
        public void ClockMustMatchAuthored100FpsSchemaInsteadOfSilentlyRetimingOrHoldingPlayback(float clock)
        {
            using (var f = new NativeSpellFixture())
            {
                f.Entry.SampleRate = clock;
                Assert.Throws<InvalidOperationException>(() => f.Library.Validate());
                f.Create(); Assert.AreEqual(0, f.Fx.Play(f.Sequence())); Assert.IsFalse(f.Fx.HasBlockingFx);
                f.Entry.SampleRate = 100; f.Library.InvalidateCaches();
                Assert.Greater(f.Fx.Play(f.Sequence()), 0, "The same native contract with the correct clock remains usable.");
            }
        }
        [TestCase(float.PositiveInfinity)] [TestCase(-1f)] [TestCase(3600f)]
        public void CastDurationMustBeFinitePositiveAndBounded(float duration)
        {
            using (var f = new NativeSpellFixture())
            {
                f.Entry.CastDuration = duration;
                Assert.Throws<InvalidOperationException>(() => f.Library.Validate());
                f.Create(); Assert.AreEqual(0, f.Fx.Play(f.Sequence())); Assert.IsNull(f.Fx.LastEntry);
            }
        }
        [Test] public void ActualImportedReducedRimeRetainsAuthoredColdContactWhenFrozenIsRejected()
        {
            var library = Resources.Load<NativeSpellFxLibrary>(NativeSpellFxLibrary.ResourcePath);
            Assert.NotNull(library, "The production seven-spell library must be imported before final acceptance.");
            library.Validate();
            using (var f = new NativeSpellFixture("Cryomancy_RimeGrip"))
            {
                SpellFxSettings.Mode = SpellFxMode.Reduced;
                f.Fx = new NativeSpellFxRenderer(library); f.Fx.SetZone(f.Zone); f.Fx.SetSurface(f.Surface);
                var actual = library.Find("Cryomancy_RimeGrip");
                Assert.Greater(f.Fx.Play(f.Sequence(targets:new[]{NativeSpellFixture.Hit("resisted",14,10,rejected:new[]{"FrozenEffect"})})),0);
                f.Fx.Update(.34f);
                var rendered = f.Drawn(); Assert.Greater(rendered.Length,0,"Reduced mode still communicates a cold contact without inventing Frozen.");
                var neutral = actual.Pieces.Where(p=>p.Role==NativeSpellFxRole.TargetImpact&&p.Condition==NativeSpellFxCondition.Always).Select(p=>p.Mesh).ToArray();
                Assert.IsTrue(rendered.Any(r=>neutral.Contains(r.GetComponent<MeshFilter>().sharedMesh)));
                var clamps = actual.Pieces.Where(p=>p.Condition==NativeSpellFxCondition.FrozenApplied).Select(p=>p.Mesh).ToArray();
                Assert.IsFalse(rendered.Any(r=>clamps.Contains(r.GetComponent<MeshFilter>().sharedMesh)));
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void NativeHierarchyOrSurfaceLossReleasesCoordinatorWaitAtTheSameUpdate(bool hideSurface)
        {
            using (var f = new NativeSpellFixture())
            {
                var go = new GameObject("Cancellation legacy",typeof(Tilemap),typeof(TilemapRenderer)); go.transform.SetParent(f.Host.transform);
                var ascii = new AsciiFxRenderer(go.GetComponent<Tilemap>());
                using (var world = new WorldFxCoordinator(ascii,f.Host.transform,f.Library))
                {
                    world.SetZone(f.Zone); world.SetNativeSurface(f.Surface);
                    var handle = world.Play(f.Sequence()); world.Update(.33f); Assert.IsTrue(world.HasBlockingFx);
                    if (hideSurface) f.Surface.Sync(f.Source,false,false);
                    else Object.DestroyImmediate(world.NativeRenderer.Root.gameObject);
                    world.Update(0);
                    Assert.AreEqual(0,world.NativeRenderer.ActiveCount);
                    Assert.IsFalse(world.HasBlockingFx,"A cancelled native backend must release its coordinator playback handle immediately.");
                    Assert.AreEqual(WorldFxPlaybackState.Cancelled,handle.State);
                    Assert.IsTrue(f.Mesh!=null); Assert.IsTrue(f.Surface.FogTexture!=null);
                }
            }
        }

        [Test] public void NativeCancellationPreservesUnrelatedFallbackPlaybackAndNewQueuedWork()
        {
            using (var f = new NativeSpellFixture())
            {
                var go = new GameObject("Mixed legacy",typeof(Tilemap),typeof(TilemapRenderer)); go.transform.SetParent(f.Host.transform);
                using (var world = new WorldFxCoordinator(new AsciiFxRenderer(go.GetComponent<Tilemap>()),f.Host.transform,f.Library))
                {
                    world.SetZone(f.Zone); world.SetNativeSurface(f.Surface);
                    var native = world.Play(f.Sequence());
                    var fallback = world.Play(f.Sequence(spell:"UnknownNative"));
                    world.Update(.1f);
                    Object.DestroyImmediate(world.NativeRenderer.Root.gameObject);
                    SpellFxBus.Emit(f.Sequence());
                    world.Update(0);
                    Assert.AreEqual(WorldFxPlaybackState.Cancelled,native.State);
                    Assert.AreEqual(WorldFxPlaybackState.Playing,fallback.State);
                    Assert.AreEqual(0,SpellFxBus.PendingCount);
                    Assert.AreEqual(WorldFxPlaybackState.Playing,world.LastPlayback.State);
                    Assert.Greater(world.NativeRenderer.ActiveCount,0);
                    Assert.IsTrue(world.HasBlockingFx);
                }
            }
        }
        [Test] public void NewCastAfterHierarchyLossCancelsOnlyTheOldNativeHandle()
        {
            using (var f = new NativeSpellFixture())
            {
                var go = new GameObject("Restart legacy",typeof(Tilemap),typeof(TilemapRenderer)); go.transform.SetParent(f.Host.transform);
                using (var world = new WorldFxCoordinator(new AsciiFxRenderer(go.GetComponent<Tilemap>()),f.Host.transform,f.Library))
                {
                    world.SetZone(f.Zone); world.SetNativeSurface(f.Surface);
                    var old = world.Play(f.Sequence()); world.Update(.1f);
                    Object.DestroyImmediate(world.NativeRenderer.Root.gameObject);
                    var current = world.Play(f.Sequence()); world.Update(0);
                    Assert.AreEqual(WorldFxPlaybackState.Cancelled,old.State);
                    Assert.AreEqual(WorldFxPlaybackState.Playing,current.State);
                    Assert.Greater(world.NativeRenderer.ActiveCount,0); Assert.IsTrue(world.HasBlockingFx);
                }
            }
        }

        [TestCase(NativeSpellFxRole.ProjectileHead, NativeSpellFxAnchor.Target, NativeSpellFxAnchor.ProjectileCarrier)]
        [TestCase(NativeSpellFxRole.ProjectileTrail, NativeSpellFxAnchor.ProjectileCarrier, NativeSpellFxAnchor.Source)]
        [TestCase(NativeSpellFxRole.TargetImpact, NativeSpellFxAnchor.Source, NativeSpellFxAnchor.Target)]
        [TestCase(NativeSpellFxRole.GroundCell, NativeSpellFxAnchor.Source, NativeSpellFxAnchor.Cell)]
        [TestCase(NativeSpellFxRole.ConeCell, NativeSpellFxAnchor.Target, NativeSpellFxAnchor.Cell)]
        [TestCase(NativeSpellFxRole.CropCell, NativeSpellFxAnchor.Source, NativeSpellFxAnchor.Cell)]
        [TestCase(NativeSpellFxRole.ReactionCell, NativeSpellFxAnchor.Target, NativeSpellFxAnchor.Cell)]
        public void DefinedButIncompatibleAnchorRejectsInsteadOfSilentlyReinterpretingAuthoredCoordinates(
            NativeSpellFxRole role, NativeSpellFxAnchor wrong, NativeSpellFxAnchor correct)
        {
            using (var f = new NativeSpellFixture())
            {
                var piece=f.Piece("misanchored-authored-piece",role,wrong,
                    role==NativeSpellFxRole.ReactionCell?NativeSpellFxCondition.FreezeWater:NativeSpellFxCondition.Always,forward:1);
                f.Entry.Pieces=new[]{piece}; f.Entry.ProjectileProgress=Enumerable.Repeat(1f,111).ToArray();
                Assert.Throws<InvalidOperationException>(()=>f.Library.Validate());
                f.Create(); Assert.AreEqual(0,f.Fx.Play(f.Sequence())); Assert.IsNotEmpty(f.Fx.Failure);
                piece.Anchor=correct; f.Library.InvalidateCaches();
                Assert.DoesNotThrow(()=>f.Library.Validate());
                Assert.Greater(f.Fx.Play(f.Sequence()),0,"The identical mesh/track with its canonical anchor remains accepted.");
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void FlamingHandsSelectedCellRemainsItsFacingWhenPendingRemoteReactionProducesFirstTarget(bool remoteReaction)
        {
            StarterSpell3DCaptureFixture.Setup();
            try
            {
                using (var f=new NativeSpellFixture("Pyromancy_FlamingHands"))
                {
                    var caster=StarterSpell3DCaptureFixture.Actor(f.Zone,"caster",10,10);
                    if(remoteReaction)
                    {
                        StarterSpell3DCaptureFixture.Actor(f.Zone,"remote-oil-victim",8,10);
                        f.Zone.TileState.WriteCoating(8,10,"oil",6); f.Zone.TileState.AddHeat(8,10,2);
                    }
                    var context=StarterSpell3DCaptureFixture.Context(f.Zone,caster); context.TargetCell=f.Zone.GetCell(11,10);
                    Assert.IsTrue(new CavesOfOoo.Skills.Pyromancy_FlamingHands().OnCommand(context));
                    var sequence=StarterSpell3DCaptureFixture.Single();
                    Assert.AreEqual(new Point(11,10),sequence.AffectedCells[0],"The spell records its selected cell before resolving ambient reactions.");
                    if(remoteReaction) Assert.AreEqual(new Point(8,10),sequence.Targets.First().Cell,"The unrelated real reaction is the first target observation.");
                    else Assert.IsEmpty(sequence.Targets);
                    f.Entry.Pieces=new[]{f.Piece("selected-cell-fan",NativeSpellFxRole.ConeCell,NativeSpellFxAnchor.Cell,forward:1)};
                    f.Create(); Assert.Greater(f.Fx.Play(sequence),0); f.Fx.Update(.3f);
                    Assert.AreEqual(1,f.Drawn().Length,"The chosen fan cannot turn toward an unrelated material-reaction target.");
                    Assert.That(Vector3.Distance(f.Drawn()[0].transform.position,Village3DProjection.CellCentre(11,10)+Vector3.up*.3f),Is.LessThan(.001f));
                }
            }
            finally {StarterSpell3DCaptureFixture.Cleanup();}
        }

        [TestCase("Cryomancy_RimeGrip", false)] [TestCase("Cryomancy_RimeGrip", true)]
        [TestCase("Pyromancy_EmberSpit", false)] [TestCase("Pyromancy_EmberSpit", true)]
        public void SingleBodySpellDirectImpactDoesNotReplayOnUnrelatedAmbientOilVictim(string spell, bool remoteReaction)
        {
            StarterSpell3DCaptureFixture.Setup();
            try
            {
                using (var f=new NativeSpellFixture(spell))
                {
                    var caster=StarterSpell3DCaptureFixture.Actor(f.Zone,"caster",10,10);
                    StarterSpell3DCaptureFixture.Actor(f.Zone,"direct-spell-body",12,10);
                    if(remoteReaction)
                    {
                        StarterSpell3DCaptureFixture.Actor(f.Zone,"remote-oil-victim",8,10);
                        f.Zone.TileState.WriteCoating(8,10,"oil",6); f.Zone.TileState.AddHeat(8,10,2);
                    }
                    var skill=spell=="Cryomancy_RimeGrip"
                        ? (CavesOfOoo.Skills.BaseSkillPart)new CavesOfOoo.Skills.Cryomancy_RimeGrip()
                        : new CavesOfOoo.Skills.Pyromancy_EmberSpit();
                    Assert.IsTrue(skill.OnCommand(StarterSpell3DCaptureFixture.Context(f.Zone,caster)));
                    var sequence=StarterSpell3DCaptureFixture.Single();
                    Assert.AreEqual(new Point(12,10),sequence.Path.Last());
                    Assert.AreEqual(remoteReaction?2:1,sequence.Targets.Count);
                    if(remoteReaction)
                    {
                        Assert.IsTrue(sequence.Reactions.Any(r=>r.Kind=="reaction"&&r.Value=="ignite_oil_heat"&&r.Cell.Equals(new Point(8,10))));
                        Assert.Greater(sequence.Targets.Single(t=>t.Cell.Equals(new Point(8,10))).Damage,0,
                            "The separate material reaction remains real captured gameplay; only the direct-contact art is filtered.");
                    }
                    f.Entry.Pieces=new[]{f.Piece("direct-spell-contact",NativeSpellFxRole.TargetImpact,NativeSpellFxAnchor.Target)};
                    f.Create(); Assert.Greater(f.Fx.Play(sequence),0); f.Fx.Update(.3f);
                    Assert.AreEqual(1,f.Drawn().Length,"A remote pending reaction is not a second projectile contact for this single-body spell.");
                    Assert.That(Vector3.Distance(f.Drawn()[0].transform.position,Village3DProjection.CellCentre(12,10)+Vector3.up*.3f),Is.LessThan(.001f));
                }
            }
            finally {StarterSpell3DCaptureFixture.Cleanup();}
        }
    }
}
