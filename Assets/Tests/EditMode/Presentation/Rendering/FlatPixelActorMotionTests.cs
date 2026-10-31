using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class FlatPixelActorMotionTests
    {
        const BindingFlags I = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        [Test] public void AcceptedBodySuppressesOnlyItsStaticForegroundAndKeepsGround()
        {
            using(var f = new Fixture())
            {
                Assert.AreEqual(1, f.Actors.ActiveViewCount);
                Assert.IsNull(f.Foreground(8,9)); Assert.NotNull(f.Background(8,9));
                Assert.AreEqual(new Rect(0,0,16,24), f.Body.sprite.rect);
                Assert.AreEqual(new Vector2(8,0), f.Body.sprite.pivot);
                Assert.AreEqual(Color.white,f.Body.color); Assert.False(f.Shadow.enabled);
                Assert.That(f.Body.sharedMaterial.shader.name, Does.Not.Contain("Lit-Default"));
                Assert.That(Vector3.Distance(new Vector3(8.5f, (Zone.Height-10)*1.5f,-.009f),f.View.position),Is.LessThan(.0001f));
            }
        }
        [Test] public void MissingMotionKeepsStaticTileAndDoesNotCreateBody()
        {
            using(var f = new Fixture(false))
            { Assert.Zero(f.Actors.ActiveViewCount); Assert.NotNull(f.Foreground(8,9)); Assert.NotNull(f.Background(8,9)); }
        }
        [Test] public void IncrementalUnrelatedRenderRetainsUntouchedActorAndCachedBody()
        {
            using(var f = new Fixture())
            {
                var view=f.View;var sprite=f.Body.sprite;int children=f.Actors.transform.childCount;
                for(int i=0;i<6;i++){f.Renderer.MarkCellDirty(20,20,"counter");f.Frame();f.Tick(.02f);}
                Assert.AreSame(view,f.View); Assert.AreSame(sprite,f.Body.sprite);
                Assert.AreEqual(children,f.Actors.transform.childCount);Assert.AreEqual(1,f.Actors.ActiveViewCount);f.Quiet();
            }
        }
        [Test] public void NativeMoveUsesBothPosesAndPreservesImmediateSimulation()
        {
            using(var f = new Fixture())
            {
                Vector3 start=f.View.position; Assert.True(MovementSystem.TryMove(f.Actor,f.Zone,1,0));
                Assert.AreEqual((9,9),f.Zone.GetEntityPosition(f.Actor));f.Frame();
                Assert.AreEqual(start.x,f.View.position.x);f.Pose(0);
                f.Tick(.055f);f.Pose(1);Assert.That(f.View.position.x,Is.InRange(start.x,start.x+1));
                f.Tick(.045f);f.Idle();Assert.AreEqual(start.x+1,f.View.position.x);Assert.False(f.Shadow.enabled);
                Assert.IsNull(f.Foreground(9,9));Assert.IsNull(f.Foreground(8,9));
            }
        }
        [Test] public void BlockedNativeMoveDoesNotStartAnimation()
        {
            using(var f = new Fixture())
            {
                var wall=new Entity{BlueprintName="Wall"};wall.AddPart(new PhysicsPart{Solid=true});f.Zone.AddEntity(wall,9,9);
                Assert.False(MovementSystem.TryMove(f.Actor,f.Zone,1,0));f.Tick(.03f);f.Idle();
                Assert.AreEqual((8,9),f.Zone.GetEntityPosition(f.Actor));
            }
        }
        [Test] public void ConsecutiveMovesBeginAtDisplayedAnchorWithoutRewind()
        {
            using(var f = new Fixture())
            {
                Assert.True(MovementSystem.TryMove(f.Actor,f.Zone,1,0));f.Tick(.04f);var current=f.View.position;
                Assert.True(MovementSystem.TryMove(f.Actor,f.Zone,1,0));
                Assert.AreEqual(current,f.View.position);f.Tick(.02f);
                Assert.That(f.View.position.x,Is.GreaterThanOrEqualTo(current.x));f.Tick(.08f);
                Assert.AreEqual(10.5f,f.View.position.x);f.Idle();
            }
        }
        [TestCase(true,1)][TestCase(false,3)]
        public void ForcedRelocationUsesIdleAndSnapsOnlyWhenNonAdjacent(bool adjacent,int distance)
        {
            using(var f = new Fixture())
            {
                var start=f.View.position;f.Zone.MoveEntity(f.Actor,8+distance,9);
                EntityVisualHooks.EmitMoved(f.Actor,f.Zone,8,9,8+distance,9,true);f.Idle();
                if(adjacent){Assert.AreEqual(start,f.View.position);f.Tick(.04f);Assert.That(f.View.position.x,Is.InRange(start.x,start.x+1));}
                f.Tick(.08f);f.Idle();Assert.AreEqual(8.5f+distance,f.View.position.x);
            }
        }
        [Test] public void AttackInterruptsWalkAtActualDestinationWithoutLunge()
        {
            using(var f = new Fixture())
            {
                Assert.True(MovementSystem.TryMove(f.Actor,f.Zone,1,0));f.Tick(.02f);
                EntityVisualHooks.EmitAttack(f.Actor,null,f.Zone);f.Pose(2);
                Assert.AreEqual(9.5f,f.View.position.x);Assert.AreEqual(Vector3.zero,f.Body.transform.localPosition);
                f.Tick(.119f);f.Pose(2);f.Tick(.002f);f.Idle();f.Tick(.04f);f.Idle();
                Assert.False(f.Shadow.enabled);Assert.AreEqual(Color.white,f.Body.color);
            }
        }
        [Test] public void NativeAcceptedMissShowsAttackButBeforeMeleeAttackVetoDoesNot()
        {
            using(var f = new Fixture())
            {
                var target=Actor("defender","PetDog");target.Statistics["DV"]=new Stat{Name="DV",BaseValue=10000};f.Zone.AddEntity(target,9,9);
                f.Actor.AddPart(new MeleeWeaponPart{BaseDamage="1d2"});int hp=target.GetStat("Hitpoints").Value;
                Assert.True(CombatSystem.PerformMeleeAttack(f.Actor,target,f.Zone,new System.Random(7)));f.Pose(2);
                Assert.AreEqual(hp,target.GetStat("Hitpoints").Value);f.Tick(.2f);f.Idle();
                f.Actor.AddPart(new VetoAttack());Assert.False(CombatSystem.PerformMeleeAttack(f.Actor,target,f.Zone,new System.Random(7)));f.Idle();
            }
        }
        [Test] public void PositiveNativeDamageShowsHurtWhileZeroDamageDoesNot()
        {
            using(var f = new Fixture())
            {
                CombatSystem.ApplyDamage(f.Actor,0,null,f.Zone);f.Idle();
                CombatSystem.ApplyDamage(f.Actor,1,null,f.Zone);f.Pose(3);f.Tick(.099f);f.Pose(3);
                f.Tick(.002f);f.Idle();Assert.AreEqual(Vector3.zero,f.Body.transform.localPosition);Assert.AreEqual(Color.white,f.Body.color);
            }
        }
        [Test] public void SameFrameRetaliationPreservesAttackThenHurtWithBoundedCompletion()
        {
            using(var f = new Fixture())
            {
                EntityVisualHooks.EmitAttack(f.Actor,null,f.Zone);CombatSystem.ApplyDamage(f.Actor,1,null,f.Zone);
                for(int i=0;i<100;i++)EntityVisualHooks.EmitDamage(f.Actor,null,f.Zone,1,false);
                f.Pose(2);f.Tick(.059f);f.Pose(2);f.Tick(.002f);f.Pose(3);
                f.Tick(.141f);f.Idle();f.Tick(1);f.Idle();Assert.False(f.Shadow.enabled);
            }
        }
        [Test] public void DuplicateAttackDoesNotRestartPoseAndCastingHasNoBorrowedAttackArt()
        {
            using(var f = new Fixture())
            {
                EntityVisualHooks.EmitAttack(f.Actor,null,f.Zone);f.Tick(.1f);
                EntityVisualHooks.EmitAttack(f.Actor,null,f.Zone);f.Tick(.025f);f.Idle();f.Tick(.04f);
                EntityVisualHooks.EmitCast(f.Actor,f.Zone,"unillustrated",8,9,9,9,.16f);f.Idle();f.Tick(.06f);f.Idle();
            }
        }
        [TestCase("hidden")][TestCase("unexplored")][TestCase("render-hidden")][TestCase("foreign-render")]
        [TestCase("identity")][TestCase("removed")][TestCase("replaced")]
        [TestCase("replaced-cell")][TestCase("foreign-cell")][TestCase("foreign-physics")][TestCase("inventory")]
        public void QuietTrackedValidationDropsInvalidBodyAndRestoresVisibleFallback(string fault)
        {
            using(var f = new Fixture())
            {
                f.Quiet();var cell=f.Zone.GetCell(8,9);
                if(fault=="hidden")cell.IsVisible=false;if(fault=="unexplored")cell.Explored=false;
                if(fault=="replaced-cell")f.Zone.Cells[8,9]=new Cell(8,9){ParentZone=f.Zone};
                if(fault=="foreign-cell")cell.ParentZone=new Zone("wrong-cell-owner");
                if(fault=="foreign-physics")f.Actor.GetPart<PhysicsPart>().ParentEntity=new Entity();
                if(fault=="inventory")f.Actor.GetPart<PhysicsPart>().InInventory=new Entity();
                if(fault=="render-hidden")f.Actor.GetPart<RenderPart>().Visible=false;
                if(fault=="foreign-render")f.Actor.GetPart<RenderPart>().ParentEntity=new Entity();
                if(fault=="identity"){f.Actor.Tags.Remove("Player");f.Actor.BlueprintName="UnknownActor";}
                if(fault=="removed")f.Zone.RemoveEntity(f.Actor);
                if(fault=="replaced"){var other=Actor("replacement","Villager");other.GetPart<RenderPart>().RenderLayer=99;f.Zone.AddEntity(other,8,9);}
                // No input/global refresh. Presenter reconciliation itself must schedule bounded fallback.
                f.ClearDirty();f.Tick(.01f);Assert.Zero(f.Actors.ActiveViewCount);Assert.False(f.FullDirty);f.Frame();
                if(fault=="identity"||fault=="replaced")Assert.NotNull(f.Foreground(8,9));
                if(fault=="hidden"||fault=="unexplored"||fault=="removed")Assert.IsNull(f.Foreground(8,9));
            }
        }
        [Test] public void HiddenHooksDoNotReplayAfterReveal()
        {
            using(var f = new Fixture())
            {
                f.Zone.GetCell(8,9).IsVisible=false;
                EntityVisualHooks.EmitAttack(f.Actor,null,f.Zone);EntityVisualHooks.EmitDamage(f.Actor,null,f.Zone,1,false);
                f.Tick(.01f);Assert.Zero(f.Actors.ActiveViewCount);
                f.Zone.GetCell(8,9).IsVisible=true;f.Renderer.MarkCellDirty(8,9,"reveal");f.Frame();f.Idle();f.Tick(.07f);f.Idle();
            }
        }
        [TestCase("fog")][TestCase("replacement")][TestCase("elapsed")]
        public void NativeDeathCapturesRemovedOwnerButStopsAtVisibilityReplacementOrDeadline(string stop)
        {
            using(var f = new Fixture())
            {
                CombatSystem.ApplyDamage(f.Actor,100,null,f.Zone);Assert.IsNull(f.Zone.GetEntityCell(f.Actor));
                Assert.AreEqual(1,f.Actors.ActiveViewCount);f.Pose(3);f.Tick(.04f);Assert.AreEqual(1,f.Actors.ActiveViewCount);
                if(stop=="fog")f.Zone.GetCell(8,9).IsVisible=false;
                if(stop=="replacement"){var living=Actor("new","Villager");f.Zone.AddEntity(living,8,9);}
                f.Tick(stop=="elapsed"?.3f:.01f);Assert.Zero(f.Actors.ActiveViewCount);
            }
        }
        [Test] public void PauseAndModeTransitionsTwiceClearMotionAndResumeIdle()
        {
            using(var f = new Fixture())
            {
                for(int i=0;i<2;i++)
                {
                    EntityVisualHooks.EmitAttack(f.Actor,null,f.Zone);f.Renderer.Paused=true;Assert.Zero(f.Actors.ActiveViewCount);
                    EntityVisualHooks.EmitDamage(f.Actor,null,f.Zone,1,false);f.Renderer.Paused=false;f.Frame();f.Idle();Assert.False(f.Shadow.enabled);
                    f.Renderer.FlatPixelModeEnabled=false;Assert.Zero(f.Actors.ActiveViewCount);
                    f.Renderer.FlatPixelModeEnabled=true;f.Frame();f.Idle();Assert.False(f.Shadow.enabled);
                }
            }
        }
        [Test] public void ForeignZoneHooksLeaveCurrentPoseAndPositionUnchanged()
        {
            using(var f = new Fixture())
            {
                var foreign=new Zone("foreign");var p=f.View.position;
                EntityVisualHooks.EmitMoved(f.Actor,foreign,8,9,12,15);EntityVisualHooks.EmitAttack(f.Actor,null,foreign);
                EntityVisualHooks.EmitDamage(f.Actor,null,foreign,1,false);EntityVisualHooks.EmitDeath(f.Actor,null,foreign,12,15);
                f.Tick(.1f);f.Idle();Assert.AreEqual(p,f.View.position);
            }
        }
        [Test] public void DestroyingOnlyComponentDestroysItsViewsAndPreservesOtherSubscribers()
        {
            using(var f = new Fixture())
            {
                int calls=0;EntityAttackVisualHandler listener=(_,__,___)=>calls++;EntityVisualHooks.AttackCallback+=listener;
                Object.DestroyImmediate(f.Actors);Assert.Zero(f.ActorRoot.transform.childCount);
                EntityVisualHooks.EmitAttack(f.Actor,null,f.Zone);Assert.AreEqual(1,calls);
                Assert.True(f.Motion!=null,"Borrowed texture survives component disposal.");EntityVisualHooks.AttackCallback-=listener;
            }
        }
        [TestCase("walk")][TestCase("attack")]
        public void CastInterruptsPriorMotionAndPendingReactionWithoutBorrowingMelee(string prior)
        {
            using(var f=new Fixture())
            {
                if(prior=="walk"){Assert.True(MovementSystem.TryMove(f.Actor,f.Zone,1,0));f.Tick(.02f);}
                else {EntityVisualHooks.EmitAttack(f.Actor,null,f.Zone);EntityVisualHooks.EmitDamage(f.Actor,null,f.Zone,1,false);}
                var cell=f.Zone.GetEntityCell(f.Actor);
                EntityVisualHooks.EmitCast(f.Actor,f.Zone,"unillustrated",cell.X,cell.Y,cell.X-1,cell.Y,.16f);
                f.Idle();Assert.AreEqual(cell.X+.5f,f.View.position.x);Assert.True(f.Body.flipX);
                f.Tick(.07f);f.Idle();Assert.False(f.Shadow.enabled);
            }
        }
        [Test] public void NativeHorizontalFacingMirrorsAndVerticalMovementKeepsThatOrientation()
        {
            using(var f=new Fixture())
            {
                Assert.True(MovementSystem.TryMove(f.Actor,f.Zone,-1,0));Assert.True(f.Body.flipX);f.Tick(.1f);
                Assert.True(MovementSystem.TryMove(f.Actor,f.Zone,0,1));Assert.True(f.Body.flipX);f.Tick(.1f);
                Assert.True(MovementSystem.TryMove(f.Actor,f.Zone,1,0));Assert.False(f.Body.flipX);f.Tick(.1f);
                Assert.True(MovementSystem.TryMove(f.Actor,f.Zone,0,-1));Assert.False(f.Body.flipX);
            }
        }
        [Test] public void QuietFramesAllocateNoManagedObjectsAndDoNotDiscoverUnpaintedActors()
        {
            using(var f=new Fixture())
            {
                var other=Actor("quiet-other","PetDog");f.Zone.AddEntity(other,12,9);f.ClearDirty();
                for(int i=0;i<10;i++)f.Actors.AdvancePresentation(.01f);
                long before=GC.GetAllocatedBytesForCurrentThread();
                for(int i=0;i<100;i++)f.Actors.AdvancePresentation(.01f);
                long after=GC.GetAllocatedBytesForCurrentThread();
                Assert.AreEqual(before,after,"Steady tracked-view updates must not allocate.");
                Assert.AreEqual(1,f.Actors.ActiveViewCount,"A quiet update must not scan the zone to discover actors.");
                f.Renderer.MarkCellDirty(12,9,"first observed");f.Frame();Assert.AreEqual(2,f.Actors.ActiveViewCount);
            }
        }
        [Test] public void ComponentDisableAndEnableRestoresStaticThenReacquiresIdleWithoutMovement()
        {
            using(var f=new Fixture())
            {
                EntityVisualHooks.EmitAttack(f.Actor,null,f.Zone);f.Actors.enabled=false;Assert.Zero(f.Actors.ActiveViewCount);
                f.Frame();Assert.NotNull(f.Foreground(8,9));
                f.Actors.enabled=true;f.Frame();Assert.AreEqual(1,f.Actors.ActiveViewCount);f.Idle();Assert.IsNull(f.Foreground(8,9));
            }
        }
        [Test] public void ZoneTransitionsTwiceDropOldBodiesAndPendingReactions()
        {
            using(var f=new Fixture())
            {
                var foreign=new Zone("ordinary-foreign");
                for(int i=0;i<2;i++)
                {
                    EntityVisualHooks.EmitAttack(f.Actor,null,f.Zone);f.Renderer.SetZone(foreign);Assert.Zero(f.Actors.ActiveViewCount);
                    EntityVisualHooks.EmitDamage(f.Actor,null,f.Zone,1,false);f.Renderer.SetZone(f.Zone);f.Frame();f.Idle();
                    Assert.AreEqual(1,f.Actors.ActiveViewCount);Assert.False(f.Shadow.enabled);
                }
            }
        }

        [TestCase(1, 0, 0f)]
        [TestCase(1, 0, .05f)]
        [TestCase(1, 1, .05f)]
        [TestCase(0, -1, .05f)]
        public void LethalReadoutDoesNotPaintOverOrBeyondADepartingNativeBody(int dx, int dy, float elapsed)
        {
            using (var f = new Fixture())
            {
                f.ClearReadouts();
                Assert.True(MovementSystem.TryMove(f.Actor, f.Zone, dx, dy));
                if (elapsed > 0) f.Tick(elapsed);
                Assert.AreEqual((8 + dx, 9 + dy), f.Zone.GetEntityPosition(f.Actor));
                Assert.True(OverlapsReadoutCell(f, 8, 9), "The real native body must still overlap the departing cell.");
                var victim = DamageReadoutOwner(f, 1);
                int version = f.Zone.EntityVersion;

                f.ReadoutFrame();

                Assert.Zero(f.Readouts.ActiveNumberCount,
                    "All higher same-column lanes cross the disclosed moving body; omit the whole lethal receipt.");
                Assert.Zero(f.GlyphPositions('1').Count);
                Assert.AreEqual(20, f.Actor.GetStatValue("Hitpoints"));
                Assert.Zero(victim.GetStatValue("Hitpoints"));
                Assert.Null(f.Zone.GetEntityCell(victim));
                Assert.AreEqual(version, f.Zone.EntityVersion, "Presentation cannot move an actor or change world ownership.");
                Assert.False(f.Fx.HasBlockingFx);
            }
        }

        [TestCase(.099f)]
        [TestCase(.1f)]
        public void ANewLethalReadoutRemainsVisibleWhenTheNativeBodyNoLongerOverlaps(float elapsed)
        {
            using (var f = new Fixture())
            {
                f.ClearReadouts();
                Assert.True(MovementSystem.TryMove(f.Actor, f.Zone, 1, 0));
                f.Tick(elapsed);
                Assert.False(OverlapsReadoutCell(f, 8, 9),
                    "Pixel-snapped bounds touching the cell edge must not count as overlap, even just before Walk ends.");
                DamageReadoutOwner(f, 1);
                f.ReadoutFrame();
                CollectionAssert.AreEqual(new[] { new Vector2Int(8, 9) }, f.GlyphPositions('1'));
                Assert.AreEqual(1, f.Readouts.ActiveNumberCount);
            }
        }

        [TestCase("current-cell-hidden")]
        [TestCase("render-hidden")]
        [TestCase("foreign-render")]
        [TestCase("foreign-physics")]
        [TestCase("removed")]
        [TestCase("component-disabled")]
        [TestCase("presentation-hidden")]
        [TestCase("other-zone")]
        public void UnavailableNativeBodiesCannotSuppressAnOtherwiseTruthfulReadout(string fault)
        {
            using (var f = new Fixture())
            {
                f.ClearReadouts();
                Assert.True(MovementSystem.TryMove(f.Actor, f.Zone, 1, 0));
                Assert.True(OverlapsReadoutCell(f, 8, 9));
                if (fault == "current-cell-hidden") f.Zone.GetCell(9, 9).IsVisible = false;
                if (fault == "render-hidden") f.Actor.GetPart<RenderPart>().Visible = false;
                if (fault == "foreign-render") f.Actor.GetPart<RenderPart>().ParentEntity = new Entity();
                if (fault == "foreign-physics") f.Actor.GetPart<PhysicsPart>().ParentEntity = new Entity();
                if (fault == "removed") f.Zone.RemoveEntity(f.Actor);
                if (fault == "component-disabled") f.Actors.enabled = false;
                if (fault == "presentation-hidden") f.Actors.SetPresentationVisible(false);
                if (fault == "other-zone") f.Actors.SetZone(new Zone("foreign-tween-readout"));
                DamageReadoutOwner(f, 1);

                // Deliberately no actor reconciliation: the provider must reject
                // stale visible views itself, not depend on an earlier cleanup.
                f.ReadoutFrame();

                CollectionAssert.AreEqual(new[] { new Vector2Int(8, 9) }, f.GlyphPositions('1'));
                Assert.AreEqual(1, f.Readouts.ActiveNumberCount);
            }
        }

        [Test] public void MissingNativeMotionKeepsTheLogicalCellReadoutBehavior()
        {
            using (var f = new Fixture(false))
            {
                f.ClearReadouts();
                Assert.Zero(f.Actors.ActiveViewCount);
                Assert.True(MovementSystem.TryMove(f.Actor, f.Zone, 1, 0));
                DamageReadoutOwner(f, 1);
                f.ReadoutFrame();
                CollectionAssert.AreEqual(new[] { new Vector2Int(8, 9) }, f.GlyphPositions('1'));
            }
        }

        [Test] public void ForeignMovementHookCannotCreateAVisualReadoutObstruction()
        {
            using (var f = new Fixture())
            {
                f.ClearReadouts();
                Assert.True(MovementSystem.TryMove(f.Actor, f.Zone, 1, 0));
                f.Tick(.1f);
                EntityVisualHooks.EmitMoved(f.Actor, new Zone("foreign-motion"), 9, 9, 8, 9);
                Assert.False(OverlapsReadoutCell(f, 8, 9));
                DamageReadoutOwner(f, 1);
                f.ReadoutFrame();
                CollectionAssert.AreEqual(new[] { new Vector2Int(8, 9) }, f.GlyphPositions('1'));
            }
        }

        [Test] public void DifferentMovingActorWithEqualIdStillObstructsTheReadout()
        {
            using (var f = new Fixture())
            {
                f.ClearReadouts();
                Assert.True(MovementSystem.TryMove(f.Actor, f.Zone, 1, 0));
                var victim = DamageReadoutOwner(f, 1, f.Actor.ID);
                Assert.AreNotSame(f.Actor, victim);
                Assert.AreEqual(f.Actor.ID, victim.ID);
                f.ReadoutFrame();
                Assert.Zero(f.Readouts.ActiveNumberCount);
                Assert.Zero(f.GlyphPositions('1').Count);
            }
        }

        [Test] public void MovingRecipientsOwnBodyIsExcludedOnlyFromTheAttributionCorridor()
        {
            using (var f = new Fixture())
            {
                f.ClearReadouts();
                Assert.True(MovementSystem.TryMove(f.Actor, f.Zone, 0, 1));
                Assert.True(OverlapsReadoutCell(f, 8, 9));
                AsciiFxBus.EmitEventText(f.Zone, f.Actor, "Guard", "&C");
                f.ReadoutFrame();

                CollectionAssert.AreEqual(new[] { new Vector2Int(6, 7) }, f.GlyphPositions('G'),
                    "The row must not paint over its own body, but the next lane may cross that exact recipient in its corridor.");
                Assert.AreEqual(1, f.Readouts.ActiveEventTextCount);
                Assert.AreEqual(20, f.Actor.GetStatValue("Hitpoints"));
            }
        }

        [Test] public void RetiredTweenObstructedReceiptDoesNotReplayAfterTheActorLeaves()
        {
            using (var f = new Fixture())
            {
                f.ClearReadouts();
                Assert.True(MovementSystem.TryMove(f.Actor, f.Zone, 1, 0));
                DamageReadoutOwner(f, 1);
                f.ReadoutFrame();
                Assert.Zero(f.Readouts.ActiveNumberCount);
                f.Tick(.1f);
                f.ReadoutFrame();
                Assert.Zero(f.GlyphPositions('1').Count, "Completing movement must not resurrect a retired outcome.");

                DamageReadoutOwner(f, 1, "new-readout-owner");
                f.ReadoutFrame();
                CollectionAssert.AreEqual(new[] { new Vector2Int(8, 9) }, f.GlyphPositions('1'),
                    "A genuinely new receipt remains allowed once the visual obstruction has left.");
            }
        }

        [TestCase("queried-cell-hidden")]
        [TestCase("foreign-zone-argument")]
        [TestCase("body-disabled")]
        [TestCase("not-top")]
        public void MovingBodyQueryRejectsUndisclosedOrSuppressedPresentation(string fault)
        {
            using (var f = new Fixture())
            {
                Assert.True(MovementSystem.TryMove(f.Actor, f.Zone, 1, 0));
                f.Tick(.05f);
                Assert.True(f.Actors.HasDisclosedMovingBodyAt(f.Zone, 8, 9, null),
                    "The same real partial-overlap setup must obstruct before its authority is revoked.");
                var requestedZone = f.Zone;
                if (fault == "queried-cell-hidden") f.Zone.GetCell(8, 9).IsVisible = false;
                if (fault == "foreign-zone-argument") requestedZone = new Zone(f.Zone.ZoneID);
                if (fault == "body-disabled") f.Body.enabled = false;
                if (fault == "not-top")
                {
                    var replacement = Actor("top-replacement", "Villager");
                    replacement.GetPart<RenderPart>().RenderLayer = 99;
                    Assert.True(f.Zone.AddEntity(replacement, 9, 9));
                }
                Assert.False(f.Actors.HasDisclosedMovingBodyAt(requestedZone, 8, 9, null));
            }
        }

        [Test] public void WarmMovingBodyReadoutQueriesAllocateNothingWhenTheRuntimeCounterWorks()
        {
            long calibrationBefore = GC.GetAllocatedBytesForCurrentThread();
            var calibration = new byte[4096];
            long calibrationAfter = GC.GetAllocatedBytesForCurrentThread();
            GC.KeepAlive(calibration);
            if (calibrationAfter <= calibrationBefore)
                Assert.Ignore("This runtime allocation counter does not observe a known allocation; zero cannot prove the query allocates nothing.");

            using (var f = new Fixture())
            {
                f.ClearReadouts();
                Assert.True(MovementSystem.TryMove(f.Actor, f.Zone, 1, 0));
                f.Tick(.05f);
                Assert.True(f.Actors.HasDisclosedMovingBodyAt(f.Zone, 8, 9, null));
                var recipient = Actor("visible-warm-recipient", "MarlbackScrabbler");
                Assert.True(f.Zone.AddEntity(recipient, 12, 10));
                CombatSystem.ApplyDamage(recipient, 1, null, f.Zone);
                f.ReadoutFrame();
                CollectionAssert.AreEqual(new[] { new Vector2Int(12, 9) }, f.GlyphPositions('1'));
                var readouts = f.Readouts;
                int version = f.Zone.EntityVersion;
                for (int i = 0; i < 20; i++) readouts.Update(0, 0);
                long before = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 100; i++) readouts.Update(0, 0);
                long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
                Assert.Zero(allocated, "Measure the warmed actual renderer/query, not reflection or test collections.");
                CollectionAssert.AreEqual(new[] { new Vector2Int(12, 9) }, f.GlyphPositions('1'));
                Assert.AreEqual(version, f.Zone.EntityVersion);
                Assert.AreEqual(19, recipient.GetStatValue("Hitpoints"));
            }
        }

        // Contact-shadow RED uses the actual SpriteRenderer already owned by
        // the accepted native view. The ordinary Glade fixture remains unchanged.
        static bool Renders(SpriteRenderer sprite) => sprite != null && sprite.enabled
            && sprite.gameObject.activeInHierarchy && sprite.sprite != null && sprite.color.a > 0;
        static bool ContactShadowOption(AnimatedEntityRenderer renderer, bool? enabled = null)
        {
            var option = typeof(AnimatedEntityRenderer).GetProperty("FlatContactShadowEnabled", I);
            Assert.NotNull(option, "The reversible native contact-shadow option is absent.");
            Assert.True(option.CanRead && option.CanWrite); Assert.AreEqual(typeof(bool), option.PropertyType);
            if (enabled.HasValue) option.SetValue(renderer, enabled.Value);
            return (bool)option.GetValue(renderer);
        }
        static void ContactShadow(Fixture f)
        {
            Assert.True(Renders(f.Shadow), "A disclosed Sill player's existing view must submit its contact shadow.");
            Assert.AreSame(Resources.Load<Sprite>("Sprites/Actors/actor_shadow"), f.Shadow.sprite);
            Assert.AreEqual(new Vector2(10, 4), f.Shadow.sprite.rect.size);
            Assert.AreEqual(new Vector2(5, 2), f.Shadow.sprite.pivot);
            Assert.AreEqual(16, f.Shadow.sprite.pixelsPerUnit);
            Assert.AreEqual(FilterMode.Point, f.Shadow.sprite.texture.filterMode);
            Assert.AreEqual(new Color(0, 0, 0, .18f), f.Shadow.color);
            Assert.AreEqual(new Vector3(1f / 16, 2f / 16, 0), f.Shadow.transform.localPosition);
            Assert.AreEqual(Vector3.one, f.Shadow.transform.localScale);
            Assert.AreSame(f.Body.sharedMaterial, f.Shadow.sharedMaterial);
            Assert.That(f.Shadow.sharedMaterial.shader.name, Does.Not.Contain("Lit-Default"));
            Assert.Less(f.Shadow.sortingOrder, f.Body.sortingOrder);
        }
        static void Inside(Bounds actual, Bounds cell)
        {
            Assert.GreaterOrEqual(actual.min.x, cell.min.x - .0001f);
            Assert.GreaterOrEqual(actual.min.y, cell.min.y - .0001f);
            Assert.LessOrEqual(actual.max.x, cell.max.x + .0001f);
            Assert.LessOrEqual(actual.max.y, cell.max.y + .0001f);
        }

        [TestCase("Grass")] [TestCase("StoneFloor")]
        public void ContactShadow_ActualSillSubmissionFitsDryFootprintWithoutChangingBody(string ground)
        {
            using (var f = new Fixture(sill: true, ground: ground))
            {
                ContactShadow(f); Assert.True(ContactShadowOption(f.Actors)); Inside(f.Shadow.bounds, f.CellBounds(8, 9));
                var view = f.View; var body = f.Body.sprite; var substrate = f.Background(8, 9);
                int count = f.ActorRoot.transform.childCount, version = f.Zone.EntityVersion;
                int hp = f.Actor.GetStatValue("Hitpoints"); string state = f.Zone.TileState.ToSaveString();
                for (int i = 0; i < 8; i++) { f.Tick(.02f); ContactShadow(f); }
                Assert.AreSame(view, f.View); Assert.AreSame(body, f.Body.sprite);
                Assert.AreSame(substrate, f.Background(8, 9)); Assert.IsNull(f.Foreground(8, 9));
                Assert.AreEqual(Color.white, f.Body.color); Assert.AreEqual(count, f.ActorRoot.transform.childCount);
                Assert.AreEqual(1, f.Actors.ActiveViewCount); Assert.AreEqual(version, f.Zone.EntityVersion);
                Assert.AreEqual(hp, f.Actor.GetStatValue("Hitpoints")); Assert.AreEqual(state, f.Zone.TileState.ToSaveString());
                Assert.AreEqual((8, 9), f.Zone.GetEntityPosition(f.Actor));
            }
        }

        [Test] public void ContactShadow_OptionDisablesAndRestoresSameViewAcrossReactions()
        {
            using (var f = new Fixture(sill: true))
            {
                Assert.True(ContactShadowOption(f.Actors)); ContactShadow(f);
                var view = f.View; var body = f.Body.sprite;
                Assert.False(ContactShadowOption(f.Actors, false)); f.Tick(0); Assert.False(Renders(f.Shadow));
                Assert.AreSame(view, f.View); Assert.AreSame(body, f.Body.sprite); Assert.True(Renders(f.Body));
                Assert.True(ContactShadowOption(f.Actors, true)); f.Tick(0); ContactShadow(f);
                EntityVisualHooks.EmitAttack(f.Actor, null, f.Zone); ContactShadow(f); f.Pose(2);
                f.Tick(.13f); ContactShadow(f); f.Idle();
                CombatSystem.ApplyDamage(f.Actor, 1, null, f.Zone); f.Tick(.01f); ContactShadow(f); f.Pose(3);
                f.Tick(.11f); ContactShadow(f); f.Idle(); Assert.AreSame(view, f.View);
            }
        }

        [TestCase("memory")] [TestCase("unexplored")] [TestCase("render-hidden")]
        [TestCase("removed")] [TestCase("death")] [TestCase("presentation-hidden")]
        public void ContactShadow_QuietRevocationNeverLeavesAVisibleOrphan(string fault)
        {
            using (var f = new Fixture(sill: true))
            {
                ContactShadow(f); var shadow = f.Shadow; var cell = f.Zone.GetCell(8, 9);
                f.ClearDirty();
                if (fault == "memory") cell.IsVisible = false;
                if (fault == "unexplored") cell.Explored = false;
                if (fault == "render-hidden") f.Actor.GetPart<RenderPart>().Visible = false;
                if (fault == "removed") Assert.True(f.Zone.RemoveEntity(f.Actor));
                if (fault == "death") CombatSystem.ApplyDamage(f.Actor, 100, null, f.Zone);
                if (fault == "presentation-hidden") f.Actors.SetPresentationVisible(false);
                f.Tick(.01f); Assert.False(Renders(shadow), "No full repaint is needed to retire a shadow.");
            }
        }

        [TestCase("water-pool")] [TestCase("oil")] [TestCase("heat")]
        public void ContactShadow_RealWetOrHazardStateSuppressesOnlyShadeAndDryRecoveryRestoresIt(string state)
        {
            using (var f = new Fixture(sill: true))
            {
                ContactShadow(f); var view = f.View; var body = f.Body.sprite; Entity pool = null;
                if (state == "water-pool") pool = f.AddActual("WaterPuddle", 8, 9);
                if (state == "oil") f.Zone.TileState.WriteCoating(8, 9, "oil", 5);
                if (state == "heat") f.Zone.TileState.AddHeat(8, 9, 20);
                f.Frame(); f.Tick(0); Assert.False(Renders(f.Shadow));
                Assert.True(Renders(f.Body)); Assert.AreSame(view, f.View); Assert.AreSame(body, f.Body.sprite);
                Assert.AreEqual(1, f.Actors.ActiveViewCount); Assert.AreEqual((8, 9), f.Zone.GetEntityPosition(f.Actor));
                if (pool != null)
                {
                    Assert.True(f.Zone.TileState.HasCoating(8, 9, "water"));
                    Assert.AreEqual(120, pool.GetPart<LiquidPoolPart>().Volume);
                    Assert.True(f.Zone.RemoveEntity(pool));
                }
                else Assert.NotNull(f.Hazard(8, 9), "The shade refusal must not remove the independent hazard mark.");
                f.Zone.TileState.Clear(8, 9); f.Frame(); f.Tick(0); ContactShadow(f);
                Assert.AreSame(view, f.View); Assert.AreSame(body, f.Body.sprite);
            }
        }

        [TestCase("hidden")] [TestCase("water")]
        public void ContactShadow_MovingFootprintCannotSpillOntoUndisclosedOrUnsafeDeparture(string fault)
        {
            using (var f = new Fixture(sill: true))
            {
                ContactShadow(f); Assert.True(MovementSystem.TryMove(f.Actor, f.Zone, 1, 0)); f.Frame();
                f.Tick(.04f); ContactShadow(f);
                Assert.AreEqual((9, 9), f.Zone.GetEntityPosition(f.Actor));
                Assert.True(f.Zone.GetCell(9, 9).IsVisible);
                Assert.Less(f.Shadow.bounds.min.x, f.CellBounds(9, 9).min.x,
                    "The real interpolated mask must overlap the departing cell, not merely its logical destination.");
                Entity pool = null;
                if (fault == "hidden") f.Zone.GetCell(8, 9).IsVisible = false;
                else pool = f.AddActual("WaterPuddle", 8, 9);
                f.Tick(0); Assert.False(Renders(f.Shadow)); Assert.True(Renders(f.Body));
                f.Tick(.06f); f.Idle(); ContactShadow(f); Inside(f.Shadow.bounds, f.CellBounds(9, 9));
                Assert.AreEqual((9, 9), f.Zone.GetEntityPosition(f.Actor));
                if (pool != null) Assert.AreEqual(120, pool.GetPart<LiquidPoolPart>().Volume);
            }
        }

        [Test] public void ContactShadow_DisclosedWallReceiverRefusesShadeButHiddenWallCannotRevealItself()
        {
            using (var f = new Fixture(sill: true))
            {
                ContactShadow(f); var body = f.Body.sprite;
                var wall = f.AddActual("StoneWall", 8, 9);
                Assert.AreSame(f.Actor, f.Zone.GetCell(8, 9).GetTopVisibleObject());
                f.Frame(); f.Tick(0); Assert.False(Renders(f.Shadow));
                Assert.True(Renders(f.Body)); Assert.AreSame(body, f.Body.sprite);
                wall.GetPart<RenderPart>().Visible = false;
                f.Tick(0); ContactShadow(f);
                wall.GetPart<RenderPart>().Visible = true;
                f.Tick(0); Assert.False(Renders(f.Shadow));
                Assert.True(f.Zone.RemoveEntity(wall)); f.Tick(0); ContactShadow(f);
                Assert.AreSame(body, f.Body.sprite);
            }
        }

        [Test] public void ContactShadow_PauseAndActualLegacyPoolReuseRestoreEachModesOwnSettings()
        {
            using (var f = new Fixture(sill: true))
            {
                ContactShadow(f); var shadow = f.Shadow;
                f.Renderer.Paused = true; Assert.False(Renders(shadow)); Assert.Zero(f.Actors.ActiveViewCount);
                f.Renderer.Paused = false; f.Frame(); f.Tick(0); ContactShadow(f); Assert.AreSame(shadow, f.Shadow);
                f.Actors.SetFlatPixelMode(false); f.Actors.SyncZone(f.Zone);
                Assert.AreSame(shadow, f.Shadow, "Exercise the recycled view, not an untouched new legacy renderer.");
                Assert.True(Renders(f.Shadow));
                Assert.AreEqual(new Color(.10f, .13f, .11f, .55f), f.Shadow.color);
                Assert.AreEqual(new Vector3(0, .10f, 0), f.Shadow.transform.localPosition);
                Assert.AreEqual(Vector3.one, f.Shadow.transform.localScale);
                Assert.AreSame(f.Body.sharedMaterial, f.Shadow.sharedMaterial);
                f.Actors.SetFlatPixelMode(true); f.Renderer.MarkCellDirty(8, 9, "shadow mode return"); f.Frame(); f.Tick(0);
                ContactShadow(f); Assert.AreSame(shadow, f.Shadow); Assert.AreEqual(1, f.Actors.ActiveViewCount);
            }
        }

        [Test] public void ContactShadow_CurrentSillAndPlayerAdmissionDoesNotExtendToOtherNativeActors()
        {
            using (var f = new Fixture(sill: true))
            {
                ContactShadow(f); var dog = Actor("shadow-dog", "PetDog"); Assert.True(f.Zone.AddEntity(dog, 10, 9));
                f.Renderer.MarkCellDirty(10, 9, "native dog control"); f.Frame(); f.Tick(0);
                var dogView = f.ActorRoot.transform.Find("Visual_PetDog"); Assert.NotNull(dogView);
                Assert.True(Renders(dogView.Find("Body").GetComponent<SpriteRenderer>()));
                Assert.False(Renders(dogView.Find("Shadow").GetComponent<SpriteRenderer>()));
                var shadow = f.Shadow;
                f.Manager.WorldMap.GetPOI(10, 10).Name = "Other village";
                f.Tick(0); Assert.False(Renders(shadow)); Assert.True(Renders(f.Body));
            }
        }

        [Test] public void ContactShadow_MissingBorrowedSpriteKeepsAcceptedBodyAndExistingView()
        {
            using (var f = new Fixture(sill: true))
            {
                ContactShadow(f); var view = f.View; var body = f.Body.sprite;
                typeof(AnimatedEntityRenderer).GetField("_shadowSprite", I).SetValue(f.Actors, null);
                f.Tick(0); Assert.False(Renders(f.Shadow)); Assert.AreSame(view, f.View);
                Assert.True(Renders(f.Body)); Assert.AreSame(body, f.Body.sprite); Assert.AreEqual(1, f.Actors.ActiveViewCount);
            }
        }

        // ════════════════ flat quest cue ════════════════

        static SpriteRenderer[] Cues(Fixture f)
        {
            var layer = (FlatQuestCueLayer)typeof(ZoneRenderer).GetField("_questCueLayer", I).GetValue(f.Renderer);
            Assert.NotNull(layer, "the flat world owns a quest-cue layer");
            layer.Sync(true);
            return layer.GetComponentsInChildren<SpriteRenderer>(true).Where(r => r.enabled).ToArray();
        }

        [Test] public void QuestCue_AConcernOwnerIsMarkedAvailableThenActiveAndANeighbourIsNot()
        {
            var prior = (CavesOfOoo.Storylets.StoryletPart.Current, NarrativeStatePart.Current, CavesOfOoo.Storylets.StoryletPart.LocalPlayer);
            try
            {
                CavesOfOoo.Storylets.StoryletRegistry.Reset(); CavesOfOoo.Concerns.ConcernRegistry.LoadAll();
                CavesOfOoo.Storylets.StoryletPart.Current = new CavesOfOoo.Storylets.StoryletPart();
                NarrativeStatePart.Current = new NarrativeStatePart();
                using (var f = new Fixture(sill: true))
                {
                    CavesOfOoo.Storylets.StoryletPart.LocalPlayer = f.Actor;
                    Assert.IsEmpty(Cues(f), "nobody here has anything to ask yet");
                    var keeper = Actor("cue-keeper", "WellKeeper"); Assert.True(f.Zone.AddEntity(keeper, 10, 9));
                    var neighbour = Actor("cue-neighbour", "Villager"); Assert.True(f.Zone.AddEntity(neighbour, 7, 9));
                    var cues = Cues(f);
                    Assert.AreEqual(1, cues.Length, "the owner, not the neighbour");
                    StringAssert.Contains("available", cues[0].sprite.name);
                    Assert.Greater(cues[0].sortingOrder, AnimatedEntityRenderer.BodySortingOrder, "drawn over bodies");

                    Assert.IsNull(CavesOfOoo.Concerns.ConcernConversation.Accept(keeper, f.Actor, "sill.well"));
                    cues = Cues(f);
                    Assert.AreEqual(1, cues.Length); StringAssert.Contains("active", cues[0].sprite.name, "work under way");

                    f.Zone.GetCell(10, 9).IsVisible = false;
                    Assert.IsEmpty(Cues(f), "an unseen owner shows nothing");
                    f.Zone.GetCell(10, 9).IsVisible = true;
                    f.Renderer.Paused = true;
                    Assert.IsEmpty(Cues(f), "fullscreen UI hides cues");
                    f.Renderer.Paused = false;
                    Assert.IsNull(CavesOfOoo.Concerns.ConcernConversation.Refuse(keeper, f.Actor, "sill.well"));
                    Assert.IsEmpty(Cues(f), "a spoken no clears it");
                }
            }
            finally
            {
                CavesOfOoo.Storylets.StoryletPart.Current = prior.Item1; NarrativeStatePart.Current = prior.Item2;
                CavesOfOoo.Storylets.StoryletPart.LocalPlayer = prior.Item3;
                CavesOfOoo.Storylets.StoryletRegistry.Reset(); CavesOfOoo.Concerns.ConcernRegistry.Reset();
            }
        }

        static Entity DamageReadoutOwner(Fixture f, int hp, string id = "tween-readout-victim")
        {
            var victim = Actor(id, "MarlbackScrabbler");
            victim.GetStat("Hitpoints").BaseValue = hp;
            Assert.True(f.Zone.AddEntity(victim, 8, 10));
            CombatSystem.ApplyDamage(victim, hp, null, f.Zone);
            return victim;
        }

        static bool OverlapsReadoutCell(Fixture f, int x, int y)
        {
            var lo = f.FxTiles.CellToWorld(new Vector3Int(x, Zone.Height - 1 - y, 0));
            var hi = f.FxTiles.CellToWorld(new Vector3Int(x + 1, Zone.Height - y, 0));
            Bounds body = f.Body.bounds;
            return body.max.x > lo.x + .0001f && body.min.x < hi.x - .0001f
                && body.max.y > lo.y + .0001f && body.min.y < hi.y - .0001f;
        }

        sealed class VetoAttack:Part { public override bool HandleEvent(GameEvent e)=>e.ID!="BeforeMeleeAttack"; }
        static Entity Actor(string id,string blueprint)
        {
            var e=new Entity{ID=id,BlueprintName=blueprint};e.Tags["Creature"]="";if(blueprint=="Player")e.Tags["Player"]="";
            e.Statistics["Hitpoints"]=new Stat{Name="Hitpoints",BaseValue=20,Min=0,Max=20};
            e.Statistics["Strength"]=new Stat{Name="Strength",BaseValue=18};e.Statistics["Agility"]=new Stat{Name="Agility",BaseValue=18};
            e.AddPart(new PhysicsPart{Solid=true});e.AddPart(new RenderPart{RenderString=blueprint=="Player"?"@":"d",RenderLayer=20});return e;
        }
        static object Call(object owner,string name,params object[] args)
        {
            var method=owner.GetType().GetMethod(name,I);Assert.NotNull(method,"Missing native motion API: "+name);
            try{return method.Invoke(owner,args);}catch(TargetInvocationException e){throw e.InnerException??e;}
        }
        sealed class NativeGraph:CameraCleanupFixture
        {
            public ZoneRenderer ZoneRenderer=>Renderer;
            public AnimatedEntityRenderer Actors=>Read<AnimatedEntityRenderer>(Renderer,"_entityVisualRenderer");
            public EnvironmentSpriteRenderer Environment=>Read<EnvironmentSpriteRenderer>(Renderer,"_envSpriteRenderer");
            public Tilemap Overlay=>Read<Tilemap>(Environment,"_overlayTilemap");
            public Tilemap Background=>Read<Tilemap>(Renderer,"_bgTilemap");
        }
        sealed class Fixture:IDisposable
        {
            readonly NativeGraph graph;readonly Tilemap overlay,bg;readonly HotbarSaveFixture globals;readonly Action restore;
            readonly DensityLootTestScope content;
            public readonly OverworldZoneManager Manager;
            public readonly GameObject ActorRoot;public readonly AnimatedEntityRenderer Actors;public readonly ZoneRenderer Renderer;
            public readonly Zone Zone;public readonly Entity Actor;public readonly Texture2D Motion;
            public bool FullDirty=>(bool)Field("_fullDirty").GetValue(Renderer);
            public Transform View=>ActorRoot.transform.Find("Visual_Player");
            public SpriteRenderer Body=>View.Find("Body").GetComponent<SpriteRenderer>();
            public SpriteRenderer Shadow=>View.Find("Shadow").GetComponent<SpriteRenderer>();
            public Tilemap FxTiles=>(Tilemap)Field("_fxTilemap").GetValue(Renderer);
            public AsciiFxRenderer Readouts=>(AsciiFxRenderer)Field("_asciiFxRenderer").GetValue(Renderer);
            public WorldFxCoordinator Fx=>(WorldFxCoordinator)Field("_worldFxCoordinator").GetValue(Renderer);
            public Fixture(bool motion=true, bool sill=false, string ground="Grass")
            {
                var moved=EntityVisualHooks.MovedCallback;var attack=EntityVisualHooks.AttackCallback;var damage=EntityVisualHooks.DamageCallback;
                var cast=EntityVisualHooks.CastCallback;var death=EntityVisualHooks.DeathCallback;
                var interaction=EntityVisualHooks.InteractionCallback;var selfUse=EntityVisualHooks.SelfUseCallback;
                var cell=ZoneRenderHooks.CellDirtyCallback;var full=ZoneRenderHooks.FullDirtyCallback;
                var bound=typeof(ZoneTileStateSystem).GetField("_boundRenderZone",BindingFlags.Static|BindingFlags.NonPublic);var oldBound=bound.GetValue(null);
                Zone previous=null;Action<int,int> oldChanged=null;Action oldSight=null;
                if(oldBound is WeakReference<Zone> weak&&weak.TryGetTarget(out previous)){oldChanged=previous.TileState.OnCellChanged;oldSight=previous.TileState.OnSightChanged;}
                restore=()=>{EntityVisualHooks.MovedCallback=moved;EntityVisualHooks.AttackCallback=attack;EntityVisualHooks.DamageCallback=damage;
                    EntityVisualHooks.CastCallback=cast;EntityVisualHooks.DeathCallback=death;
                    EntityVisualHooks.InteractionCallback=interaction;EntityVisualHooks.SelfUseCallback=selfUse;ZoneRenderHooks.CellDirtyCallback=cell;ZoneRenderHooks.FullDirtyCallback=full;
                    bound.SetValue(null,oldBound);if(previous!=null){previous.TileState.OnCellChanged=oldChanged;previous.TileState.OnSightChanged=oldSight;}};
                try
                {
                    if(sill)content=new DensityLootTestScope();
                    globals=new HotbarSaveFixture(true,false);EntityVisualHooks.Reset();
                    Zone=new Zone(sill?FlatPixelSillEnvelopeCatalog.ZoneID:ReferenceGladePlan.ZoneID);
                    if(sill)
                    {
                        Manager=OverworldZoneManager.CreateDetached(content.Factory,64,true);Manager.SetActiveZone(Zone);
                        Assert.True(FlatPixelSillEnvelopeCatalog.IsSupportedZone(Zone));
                    }
                    for(int y=7;y<12;y++)for(int x=6;x<14;x++)
                    {
                        Zone.GetCell(x,y).Explored=Zone.GetCell(x,y).IsVisible=true;
                        if(sill)AddActual(ground,x,y);
                    }
                    Actor=FlatPixelActorMotionTests.Actor("motion-player","Player");Zone.AddEntity(Actor,8,9);
                    graph=new NativeGraph();graph.SetupCameraCleanup();Renderer=graph.ZoneRenderer;
                    var env=graph.Environment;overlay=graph.Overlay;bg=graph.Background;
                    Actors=graph.Actors;ActorRoot=Actors.gameObject;
                    Type catalog=typeof(AnimatedEntityRenderer).Assembly.GetType("CavesOfOoo.Rendering.FlatPixelActorMotionCatalog");Assert.NotNull(catalog,"Compact native motion catalog is absent.");
                    Motion=motion?new Texture2D(256,24):null;
                    object injected=Activator.CreateInstance(catalog,new object[]{Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.ResourcePath),Resources.Load<Texture2D>(FlatPixelOpeningArtCatalog.CastResourcePath),Motion});
                    var field=typeof(AnimatedEntityRenderer).GetField("_flatMotionCatalog",I);Assert.NotNull(field,"Renderer has no compact catalog ownership.");field.SetValue(Actors,injected);
                    Field("_flatPixelModeEnabled").SetValue(Renderer,true);
                    Renderer.SetZone(Zone);ZoneRenderHooks.CellDirtyCallback=Renderer.MarkCellDirty;ZoneRenderHooks.FullDirtyCallback=Renderer.MarkDirty;Frame();Assert.True(Renderer.FlatPixelModeActive);Quiet();
                }
                catch{Dispose();throw;}
            }
            public void Frame()=>Call(Renderer,"LateUpdate");
            public void ReadoutFrame()=>Fx.Update(0);
            public void ClearReadouts()=>Fx.CancelAll();
            public List<Vector2Int> GlyphPositions(char glyph)
            {
                var positions=new List<Vector2Int>();
                var gameplay=CP437TilesetGenerator.GetTile(glyph);var ui=CP437TilesetGenerator.GetUiTile(glyph);
                for(int y=0;y<CavesOfOoo.Core.Zone.Height;y++)for(int x=0;x<CavesOfOoo.Core.Zone.Width;x++)
                {var tile=FxTiles.GetTile(new Vector3Int(x,CavesOfOoo.Core.Zone.Height-1-y,0));if(tile==gameplay||tile==ui)positions.Add(new Vector2Int(x,y));}
                return positions;
            }
            public void Tick(float dt)=>Call(Actors,"AdvancePresentation",dt);
            public void Pose(int index){Assert.NotNull(Body.sprite);Assert.AreSame(Motion,Body.sprite.texture);Assert.AreEqual(new Rect(index*16,0,16,24),Body.sprite.rect);}
            public void Idle(){Assert.NotNull(Body.sprite);Assert.AreNotSame(Motion,Body.sprite.texture);Assert.AreEqual(new Rect(0,0,16,24),Body.sprite.rect);}
            public Tile Foreground(int x,int y)=>overlay.GetTile<Tile>(new Vector3Int(x,CavesOfOoo.Core.Zone.Height-1-y,0));
            public TileBase Background(int x,int y)=>bg.GetTile(new Vector3Int(x,CavesOfOoo.Core.Zone.Height-1-y,0));
            public TileBase Hazard(int x,int y)=>((Tilemap)Field("_tileStateTilemap").GetValue(Renderer)).GetTile(new Vector3Int(x,CavesOfOoo.Core.Zone.Height-1-y,0));
            public Entity AddActual(string blueprint,int x,int y)
            {Assert.NotNull(content);var owner=content.Factory.CreateEntity(blueprint);Assert.NotNull(owner);Assert.True(Zone.AddEntity(owner,x,y));return owner;}
            public Bounds CellBounds(int x,int y)
            {
                var low=bg.CellToWorld(new Vector3Int(x,CavesOfOoo.Core.Zone.Height-1-y,0));
                var high=bg.CellToWorld(new Vector3Int(x+1,CavesOfOoo.Core.Zone.Height-y,0));
                return new Bounds((low+high)*.5f,high-low);
            }
            public void Quiet(){Assert.False(FullDirty);Assert.Zero(((HashSet<int>)Field("_dirtyCells").GetValue(Renderer)).Count);}
            public void ClearDirty(){Field("_fullDirty").SetValue(Renderer,false);((HashSet<int>)Field("_dirtyCells").GetValue(Renderer)).Clear();}
            static FieldInfo Field(string name)=>typeof(ZoneRenderer).GetField(name,I);
            public void Dispose(){try{graph?.CleanupCameraFixture();if(Motion!=null)Object.DestroyImmediate(Motion);}finally{try{globals?.Dispose();}finally{try{content?.Dispose();}finally{restore?.Invoke();}}}}
        }
    }
}
