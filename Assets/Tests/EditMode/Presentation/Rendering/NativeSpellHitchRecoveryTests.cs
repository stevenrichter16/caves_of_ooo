using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using CavesOfOoo.Skills;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using F = CavesOfOoo.Tests.StarterSpell3DCaptureFixture;

namespace CavesOfOoo.Tests
{
    /// <summary>The native R19/R20 failures crossed a complete contact interval in
    /// one long wall frame. Recovery must preserve real feedback, not change the
    /// resolved action, ordinary speed, fallback clocks, or wall-clock deadline.</summary>
    public sealed class NativeSpellHitchRecoveryTests
    {
        SpellFxMode oldMode;
        float oldSpeed, oldFlash;

        [SetUp] public void Setup()
        {
            oldMode=SpellFxSettings.Mode; oldSpeed=SpellFxSettings.AnimationSpeed; oldFlash=SpellFxSettings.FlashIntensity;
            SpellFxSettings.Mode=SpellFxMode.Full; SpellFxSettings.AnimationSpeed=1; SpellFxSettings.FlashIntensity=0;
            F.Setup();
        }
        [TearDown] public void Cleanup()
        {
            F.Cleanup();
            SpellFxSettings.Mode=oldMode; SpellFxSettings.AnimationSpeed=oldSpeed; SpellFxSettings.FlashIntensity=oldFlash;
        }
        static WorldFxCoordinator World(NativeSpellFixture f, out AsciiFxRenderer ascii, NativeSpellFxLibrary library=null)
        {
            var tile=new GameObject("Hitch recovery legacy surface",typeof(Tilemap),typeof(TilemapRenderer));
            tile.transform.SetParent(f.Host.transform);
            ascii=new AsciiFxRenderer(tile.GetComponent<Tilemap>());
            var world=new WorldFxCoordinator(ascii,f.Host.transform,library??f.Library);
            f.Fx=world.NativeRenderer; world.SetZone(f.Zone); world.SetNativeSurface(f.Surface); world.Update(0);
            return world;
        }
        static SpellFxSequence Short(NativeSpellFixture f, int y=10) =>
            f.Sequence(source:new Point(10,y),path:new[]{new Point(11,y)},cells:new[]{new Point(11,y)},
                targets:new[]{NativeSpellFixture.Hit("target-"+y,11,y)});
        static void FinishWithOrdinaryFrames(WorldFxCoordinator world, WorldFxPlayback handle)
        {
            for(int i=0;i<60&&!handle.IsFinished;i++) world.Update(.02f);
            Assert.AreEqual(WorldFxPlaybackState.Completed,handle.State);
            Assert.IsFalse(world.HasBlockingFx); Assert.AreEqual(0,world.NativeRenderer.ActiveMeshCount);
        }

        [TestCase("Hydromancy_JetBlast",SpellFxMode.Full)]
        [TestCase("Hydromancy_JetBlast",SpellFxMode.Reduced)]
        [TestCase("Cryomancy_RimeGrip",SpellFxMode.Full)]
        [TestCase("Cryomancy_RimeGrip",SpellFxMode.Reduced)]
        public void LongFramePreservesActualDirectionalSpellAndConditionalFeedback(string spell, SpellFxMode mode)
        {
            SpellFxSettings.Mode=mode;
            var library=Resources.Load<NativeSpellFxLibrary>(NativeSpellFxLibrary.ResourcePath);
            Assert.NotNull(library); var entry=library.Find(spell); Assert.NotNull(entry);
            using(var f=new NativeSpellFixture(spell))
            {
                bool rime=spell=="Cryomancy_RimeGrip";
                var caster=F.Actor(f.Zone,"hitch-caster",10,10);
                var target=F.Actor(f.Zone,"hitch-target",rime?13:10,rime?7:12);
                var context=F.Context(f.Zone,caster); context.DirectionX=rime?1:0; context.DirectionY=rime?-1:1;
                Assert.IsTrue((rime?(BaseSkillPart)new Cryomancy_RimeGrip():new Hydromancy_JetBlast()).OnCommand(context));
                var sequence=F.Single(); var hit=sequence.Targets.Single();
                Assert.AreEqual(rime?4:2,hit.Damage);
                Assert.Contains(rime?"FrozenEffect":"WetEffect",hit.AppliedEffects.ToArray());
                Assert.AreEqual(rime?new Point(13,7):new Point(10,12),hit.Cell);
                Assert.AreEqual(!rime,hit.Moved);
                var path=sequence.Path.ToArray(); var affected=sequence.AffectedCells.ToArray(); var reactions=sequence.Reactions.ToArray();
                int hp=target.GetStatValue("Hitpoints"), written=f.Zone.TileState.WrittenCount;
                var position=f.Zone.GetEntityPosition(target); var effects=target.GetPart<StatusEffectsPart>().GetAllEffects().ToArray();
                var durations=effects.Select(e=>e.Duration).ToArray();
                float state=rime?target.GetEffect<FrozenEffect>().Cold:target.GetEffect<WetEffect>().Moisture;
                // Consume only the copied sequence below; legacy outcome requests are
                // cleared when the coordinator binds its zone, as in other native fixtures.
                using(var world=World(f,out _,library))
                {
                    var handle=world.Play(sequence); Assert.AreSame(entry,f.Fx.LastEntry);
                    world.Update(.10f);
                    world.Update(.56f);
                    Assert.AreEqual(WorldFxPlaybackState.Playing,handle.State,"The actual long frame must not erase an unseen contact interval.");
                    Assert.IsTrue(world.HasBlockingFx);
                    int peak=0; bool sawConditional=false;
                    var conditional=entry.Pieces.Where(p=>p.Condition==NativeSpellFxCondition.FrozenApplied).Select(p=>p.Mesh).ToArray();
                    if(rime) Assert.IsNotEmpty(conditional,"The imported Rime status art is a real prerequisite.");
                    for(int i=0;i<12&&!handle.IsFinished;i++)
                    {
                        world.Update(.02f); peak=Math.Max(peak,f.Fx.ActiveMeshCount);
                        sawConditional|=f.Drawn().Any(r=>conditional.Contains(r.GetComponent<MeshFilter>().sharedMesh));
                    }
                    Assert.Greater(peak,0,"A retained handle alone is insufficient: actual imported meshes must appear.");
                    if(rime) Assert.IsTrue(sawConditional,"The actual successful Frozen result must retain its conditional art.");
                    FinishWithOrdinaryFrames(world,handle);
                    CollectionAssert.AreEqual(path,sequence.Path); CollectionAssert.AreEqual(affected,sequence.AffectedCells);
                    CollectionAssert.AreEqual(reactions,sequence.Reactions); Assert.AreSame(hit,sequence.Targets.Single());
                    Assert.AreEqual(hp,target.GetStatValue("Hitpoints")); Assert.AreEqual(position,f.Zone.GetEntityPosition(target));
                    CollectionAssert.AreEqual(effects,target.GetPart<StatusEffectsPart>().GetAllEffects());
                    CollectionAssert.AreEqual(durations,effects.Select(e=>e.Duration).ToArray());
                    Assert.AreEqual(state,rime?target.GetEffect<FrozenEffect>().Cold:target.GetEffect<WetEffect>().Moisture);
                    Assert.AreEqual(written,f.Zone.TileState.WrittenCount); Assert.AreEqual(2,f.Zone.GetAllEntities().Count);
                    Assert.AreEqual(0,SpellFxBus.PendingCount);
                }
            }
        }

        [Test] public void OrdinaryShortWallFrameAtDoubleSpeedKeepsExistingCompletionContract()
        {
            SpellFxSettings.AnimationSpeed=2;
            using(var f=new NativeSpellFixture()) using(var world=World(f,out _))
            {
                var handle=world.Play(Short(f)); world.Update(.313f);
                Assert.AreEqual(WorldFxPlaybackState.Completed,handle.State);
                Assert.IsFalse(world.HasBlockingFx); Assert.IsEmpty(f.Drawn());
            }
        }
        [Test] public void ContactAlreadyDrawnIsNotReplayedByALaterLongFrame()
        {
            using(var f=new NativeSpellFixture()) using(var world=World(f,out _))
            {
                var handle=world.Play(Short(f)); world.Update(.33f); Assert.AreEqual(1,f.Drawn().Length);
                world.Update(.56f);
                Assert.AreEqual(WorldFxPlaybackState.Completed,handle.State); Assert.IsEmpty(f.Drawn());
                world.Update(0); Assert.IsFalse(world.HasBlockingFx);
            }
        }
        [Test] public void HardFiveSecondRawDeadlineWinsBeforeAnUnseenContactCanRecover()
        {
            using(var f=new NativeSpellFixture()) using(var world=World(f,out _))
            {
                var handle=world.Play(Short(f)); world.Update(.10f); world.Update(4.91f);
                Assert.AreEqual(WorldFxPlaybackState.TimedOut,handle.State);
                Assert.IsFalse(world.HasBlockingFx); Assert.IsEmpty(f.Drawn());
            }
        }
        [TestCase("mode")] [TestCase("hidden")] [TestCase("zone")] [TestCase("clear")]
        public void CancellationWinsOverLongFrameRecovery(string reason)
        {
            using(var f=new NativeSpellFixture()) using(var world=World(f,out _))
            {
                var handle=world.Play(Short(f)); world.Update(.10f);
                if(reason=="mode") SpellFxSettings.Mode=SpellFxMode.Off;
                if(reason=="zone") world.SetZone(new Zone("Hitch other zone"));
                if(reason=="clear") AsciiFxBus.Clear();
                world.Update(.56f,true,reason!="hidden");
                Assert.AreEqual(WorldFxPlaybackState.Cancelled,handle.State);
                Assert.IsFalse(world.HasBlockingFx); Assert.IsEmpty(f.Drawn());
            }
        }
        [Test] public void EmptyActualRainKeepsItsGestureWithoutAcquiringAnImaginaryContact()
        {
            const string spell="Hydromancy_ConjureRain";
            var library=Resources.Load<NativeSpellFxLibrary>(NativeSpellFxLibrary.ResourcePath); Assert.NotNull(library);
            var entry=library.Find(spell); Assert.NotNull(entry); Assert.NotNull(entry.CastClip);
            var previous=EntityVisualHooks.CastCallback;
            try
            {
                using(var f=new NativeSpellFixture(spell))
                {
                    var caster=F.Actor(f.Zone,"empty-rain",10,10);
                    Assert.IsTrue(new Hydromancy_ConjureRain().OnCommand(F.Context(f.Zone,caster)));
                    var sequence=F.Single(); Assert.IsEmpty(sequence.Targets);
                    int gestures=0; EntityVisualHooks.CastCallback=(actor,zone,id,sx,sy,tx,ty,duration)=>
                    { Assert.AreSame(caster,actor); Assert.AreEqual(entry.CastDuration,duration); gestures++; };
                    using(var world=World(f,out _,library))
                    {
                        var handle=world.Play(sequence); Assert.AreSame(entry,f.Fx.LastEntry); Assert.AreEqual(1,gestures);
                        Assert.AreEqual(0,f.Fx.ActiveCount); world.Update(.10f); world.Update(.56f);
                        Assert.AreEqual(WorldFxPlaybackState.Completed,handle.State);
                        Assert.IsFalse(world.HasBlockingFx); Assert.IsEmpty(f.Drawn()); Assert.AreEqual(1,gestures);
                    }
                }
            }
            finally { EntityVisualHooks.CastCallback=previous; }
        }
        [Test] public void UnmatchedConditionalGeometryDoesNotCreateAContactCheckpoint()
        {
            using(var f=new NativeSpellFixture())
            {
                f.Entry.Pieces[0].Condition=NativeSpellFxCondition.FrozenApplied;
                using(var world=World(f,out _))
                {
                    var handle=world.Play(Short(f)); Assert.AreSame(f.Entry,f.Fx.LastEntry); Assert.AreEqual(0,f.Fx.ActiveCount);
                    world.Update(.10f); world.Update(.56f);
                    Assert.AreEqual(WorldFxPlaybackState.Completed,handle.State); Assert.IsEmpty(f.Drawn()); Assert.IsFalse(world.HasBlockingFx);
                }
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void AnUnrenderableContactCannotManufactureAnExtraWait(bool sourceGatherOnly)
        {
            using(var f=new NativeSpellFixture())
            {
                if(sourceGatherOnly) f.Entry.Pieces=new[]{f.Piece("gather-only",NativeSpellFxRole.SourceGather,NativeSpellFxAnchor.Source)};
                else f.Zone.GetCell(11,10).IsVisible=false;
                using(var world=World(f,out _))
                {
                    var handle=world.Play(Short(f)); Assert.Greater(f.Fx.ActiveCount,0,"This control is scheduled, unlike an empty Rain cast.");
                    world.Update(.10f); world.Update(.56f);
                    Assert.AreEqual(WorldFxPlaybackState.Completed,handle.State); Assert.IsFalse(world.HasBlockingFx); Assert.IsEmpty(f.Drawn());
                }
            }
        }
        [Serializable] sealed class RecoveryReceipt
        {
            public string backend;
            public float wallSeconds, requestedPlaybackSeconds, appliedPlaybackSeconds;
        }
        [TestCase(false)] [TestCase(true)]
        public void RecoveryDiagnosticsRetainRawWallTimeAndDistinguishAnOrdinaryFrame(bool hitch)
        {
            bool enabled=Diag.IsChannelEnabled("effect"); Diag.SetChannel("effect",true);
            try
            {
                using(var f=new NativeSpellFixture()) using(var world=World(f,out _))
                {
                    world.Play(Short(f)); world.Update(.10f);
                    var before=Diag.Snapshot(8192).Select(e=>e.TraceId).ToArray();
                    world.Update(hitch?.56f:.20f);
                    var events=Diag.Snapshot(8192).Where(e=>!before.Contains(e.TraceId)&&e.Kind=="NativeSpellHitchRecovered").ToArray();
                    Assert.AreEqual(hitch?1:0,events.Length);
                    if(hitch)
                    {
                        var receipt=JsonUtility.FromJson<RecoveryReceipt>(events.Single().PayloadJson);
                        Assert.AreEqual("native",receipt.backend);
                        Assert.That(receipt.wallSeconds,Is.EqualTo(.56f).Within(.00001f));
                        Assert.That(receipt.requestedPlaybackSeconds,Is.EqualTo(.56f).Within(.00001f));
                        Assert.That(receipt.appliedPlaybackSeconds,Is.GreaterThan(0).And.LessThan(receipt.requestedPlaybackSeconds));
                    }
                }
            }
            finally { Diag.SetChannel("effect",enabled); }
        }
        [TestCase(.3999f,false)] [TestCase(.4f,true)]
        public void RecoveryThresholdMeasuresRawWallSecondsRatherThanSpeedScaledSeconds(float wallDelta, bool recover)
        {
            SpellFxSettings.AnimationSpeed=2;
            using(var f=new NativeSpellFixture()) using(var world=World(f,out _))
            {
                var handle=world.Play(Short(f)); world.Update(.05f); world.Update(wallDelta);
                Assert.AreEqual(recover?WorldFxPlaybackState.Playing:WorldFxPlaybackState.Completed,handle.State);
                Assert.AreEqual(recover?1:0,f.Drawn().Length);
                if(recover) FinishWithOrdinaryFrames(world,handle);
                else Assert.IsFalse(world.HasBlockingFx);
            }
        }
        [Test] public void FractionalContactUsesTheInterpolatedVisiblePoseInsteadOfTheEmptyFloorSample()
        {
            using(var f=new NativeSpellFixture())
            {
                f.Entry.StudyContactFrame=32.5f; f.Entry.StudyClearFrame=70.5f;
                var piece=f.Entry.Pieces.Single();
                for(int i=0;i<piece.Poses.Length;i++) piece.Poses[i].Scale=i>=33&&i<70?Vector3.one:Vector3.zero;
                Assert.AreEqual(Vector3.zero,piece.Poses[32].Scale); Assert.AreEqual(Vector3.one,piece.Poses[33].Scale);
                using(var world=World(f,out _))
                {
                    var handle=world.Play(Short(f)); world.Update(.10f); world.Update(.56f);
                    Assert.AreEqual(WorldFxPlaybackState.Playing,handle.State);
                    Assert.That(f.Drawn().Single().transform.localScale.x,Is.EqualTo(.5f).Within(.02f),
                        "Recovery eligibility and actual draw sampling must agree at the fractional contact frame.");
                    FinishWithOrdinaryFrames(world,handle);
                }
            }
        }
        [Test] public void VisibleOwnerDoesNotRecoverAContactWhoseSampledPoseLiesInFog()
        {
            using(var f=new NativeSpellFixture())
            {
                var offset=new Vector3(0,.3f,1);
                foreach(var piece in f.Entry.Pieces) for(int i=0;i<piece.Poses.Length;i++) piece.Poses[i].Position=offset;
                var point=Village3DProjection.CellCentre(11,10)+Quaternion.LookRotation(Vector3.right,Vector3.up)*offset;
                Assert.IsTrue(Village3DProjection.TryWorldToCell(point,out int x,out int y));
                Assert.AreNotEqual(new Point(11,10),new Point(x,y)); f.Zone.GetCell(x,y).IsVisible=false;
                Assert.IsTrue(f.Zone.GetCell(11,10).IsVisible,"The canonical owner is visible; the sampled pose is not.");
                using(var world=World(f,out _))
                {
                    var handle=world.Play(Short(f)); Assert.Greater(f.Fx.ActiveCount,0);
                    world.Update(.10f); world.Update(.56f);
                    Assert.AreEqual(WorldFxPlaybackState.Completed,handle.State);
                    Assert.IsFalse(world.HasBlockingFx); Assert.IsEmpty(f.Drawn());
                }
            }
        }
        [Test] public void ASecondLongFrameDoesNotReplayTheContactOrEmitAnotherRecovery()
        {
            bool enabled=Diag.IsChannelEnabled("effect"); Diag.SetChannel("effect",true);
            try
            {
                using(var f=new NativeSpellFixture()) using(var world=World(f,out _))
                {
                    var handle=world.Play(Short(f)); world.Update(.10f); world.Update(.56f);
                    Assert.AreEqual(WorldFxPlaybackState.Playing,handle.State); Assert.AreEqual(1,f.Drawn().Length);
                    var recoveries=Diag.Snapshot(8192).Count(e=>e.Kind=="NativeSpellHitchRecovered"); Assert.AreEqual(1,recoveries);
                    world.Update(.56f);
                    Assert.AreEqual(WorldFxPlaybackState.Completed,handle.State); Assert.IsEmpty(f.Drawn());
                    Assert.AreEqual(recoveries,Diag.Snapshot(8192).Count(e=>e.Kind=="NativeSpellHitchRecovered"));
                    Assert.IsFalse(world.HasBlockingFx);
                }
            }
            finally { Diag.SetChannel("effect",enabled); }
        }
        [Test] public void AccumulatedRawTimeoutSuppressesAnOtherwiseEligibleRecoveryAndItsDiagnostic()
        {
            bool enabled=Diag.IsChannelEnabled("effect"); Diag.SetChannel("effect",true);
            SpellFxSettings.AnimationSpeed=.25f;
            try
            {
                using(var f=new NativeSpellFixture())
                {
                    // A valid late-contact study plus a long copied route keeps the
                    // contact unseen at 4.3 wall seconds. The next 1s would cross its
                    // full remaining interval, but the raw five-second watchdog wins.
                    f.Entry.ReleaseFrame=80; f.Entry.StudyContactFrame=90; f.Entry.StudyClearFrame=110;
                    foreach(var piece in f.Entry.Pieces) for(int i=0;i<piece.Poses.Length;i++)
                        piece.Poses[i].Scale=i>=80&&i<110?Vector3.one:Vector3.zero;
                    var path=Enumerable.Range(11,12).Select(x=>new Point(x,10)).ToArray();
                    using(var world=World(f,out _))
                    {
                        var handle=world.Play(f.Sequence(path:path,cells:new[]{path.Last()},targets:new[]{NativeSpellFixture.Hit("late",22,10)}));
                        world.Update(4.3f);
                        Assert.AreEqual(WorldFxPlaybackState.Playing,handle.State); Assert.IsEmpty(f.Drawn());
                        world.Update(1f);
                        Assert.AreEqual(WorldFxPlaybackState.TimedOut,handle.State);
                        Assert.IsFalse(world.HasBlockingFx); Assert.IsEmpty(f.Drawn());
                        Assert.AreEqual(0,Diag.Snapshot(8192).Count(e=>e.Kind=="NativeSpellHitchRecovered"));
                    }
                }
            }
            finally { Diag.SetChannel("effect",enabled); }
        }
        [Test] public void ARequestQueuedDuringRecoveryStartsAtZeroRatherThanInheritingEitherDelta()
        {
            using(var f=new NativeSpellFixture()) using(var world=World(f,out _))
            {
                var older=world.Play(Short(f,10)); world.Update(.10f);
                SpellFxBus.Emit(Short(f,12)); world.Update(.56f);
                var queued=world.LastPlayback; Assert.AreNotSame(older,queued);
                Assert.AreEqual(WorldFxPlaybackState.Playing,older.State); Assert.AreEqual(WorldFxPlaybackState.Playing,queued.State);
                Assert.AreEqual(0,SpellFxBus.PendingCount); Assert.AreEqual(2,f.Fx.ActiveCount); Assert.AreEqual(1,f.Drawn().Length);
                world.Update(.10f); Assert.AreEqual(1,f.Drawn().Length,"The newly accepted target must still be before contact.");
                world.Update(.15f); Assert.AreEqual(2,f.Drawn().Length,"The queued cast reaches its own contact after .25s.");
                world.Update(.39f);
                Assert.AreEqual(WorldFxPlaybackState.Completed,older.State); Assert.AreEqual(WorldFxPlaybackState.Completed,queued.State);
                Assert.IsFalse(world.HasBlockingFx); Assert.IsEmpty(f.Drawn());
            }
        }
        [Test] public void LongWallFrameAtSlowSpeedDoesNotJumpOrDelayACastThatHasNotCrossedClear()
        {
            SpellFxSettings.AnimationSpeed=.25f;
            bool enabled=Diag.IsChannelEnabled("effect"); Diag.SetChannel("effect",true);
            try
            {
                using(var f=new NativeSpellFixture()) using(var world=World(f,out _))
                {
                    var handle=world.Play(Short(f)); world.Update(.10f); world.Update(.56f);
                    Assert.AreEqual(WorldFxPlaybackState.Playing,handle.State); Assert.IsEmpty(f.Drawn());
                    Assert.AreEqual(0,Diag.Snapshot(8192).Count(e=>e.Kind=="NativeSpellHitchRecovered"));
                    world.Update(.36f); Assert.AreEqual(1,f.Drawn().Length,"The full .165s scaled elapsed must be retained before this ordinary step.");
                    world.Update(2f); Assert.AreEqual(WorldFxPlaybackState.Completed,handle.State); Assert.IsFalse(world.HasBlockingFx);
                }
            }
            finally { Diag.SetChannel("effect",enabled); }
        }
        [Test] public void ConcurrentNativeHandlesAndTheirViewsAdvanceOnTheSameRecoveredClock()
        {
            using(var f=new NativeSpellFixture()) using(var world=World(f,out _))
            {
                var first=world.Play(Short(f,10)); world.Update(.05f);
                var second=world.Play(Short(f,12)); world.Update(.05f); world.Update(.60f);
                Assert.AreEqual(WorldFxPlaybackState.Playing,first.State); Assert.AreEqual(WorldFxPlaybackState.Playing,second.State);
                world.Update(.06f); Assert.AreEqual(2,f.Drawn().Length);
                Assert.IsTrue(f.Drawn().Any(r=>Vector3.Distance(r.transform.position,Village3DProjection.CellCentre(11,10)+Vector3.up*.3f)<.001f));
                Assert.IsTrue(f.Drawn().Any(r=>Vector3.Distance(r.transform.position,Village3DProjection.CellCentre(11,12)+Vector3.up*.3f)<.001f));
                world.Update(.39f);
                Assert.AreEqual(WorldFxPlaybackState.Completed,first.State); Assert.AreEqual(WorldFxPlaybackState.Completed,second.State);
                Assert.IsFalse(world.HasBlockingFx); Assert.IsEmpty(f.Drawn());
            }
        }
        [Test] public void NativeRecoveryDoesNotSlowLegacyParticlesOrTheSpriteFallback()
        {
            using(var f=new NativeSpellFixture()) using(var world=World(f,out var ascii))
            {
                // The small supplied native library contains Ember only: Jet must
                // exercise the actual shipped sprite fallback in this same coordinator.
                var fallback=world.Play(f.Sequence(spell:"Hydromancy_JetBlast",path:new[]{new Point(11,10)},
                    cells:new[]{new Point(11,10)},targets:new[]{NativeSpellFixture.Hit("wet",11,10)}));
                Assert.IsNull(f.Fx.LastEntry); Assert.Greater(world.SpriteRenderer.ActiveCount,0);
                // This actual one-cell cone lasts .665s. Start it .05s before the
                // native cast so .71s raw elapsed must finish it on the hitch frame.
                world.Update(.05f);
                AsciiFxBus.EmitBeam(f.Zone,new[]{new Point(11,10)},1,0,AsciiFxTheme.Fire,.45f,true);
                world.Update(0); Assert.AreEqual(1,ascii.ActiveBeamCount);
                var native=world.Play(Short(f)); world.Update(.10f); world.Update(.56f);
                Assert.AreEqual(WorldFxPlaybackState.Completed,fallback.State); Assert.AreEqual(0,world.SpriteRenderer.ActiveCount);
                Assert.AreEqual(0,ascii.ActiveBeamCount);
                Assert.AreEqual(WorldFxPlaybackState.Playing,native.State);
                FinishWithOrdinaryFrames(world,native);
            }
        }
    }
}
