using System;
using System.Collections;
using System.Collections.Generic;
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
            public readonly GameObject ActorRoot;public readonly AnimatedEntityRenderer Actors;public readonly ZoneRenderer Renderer;
            public readonly Zone Zone;public readonly Entity Actor;public readonly Texture2D Motion;
            public bool FullDirty=>(bool)Field("_fullDirty").GetValue(Renderer);
            public Transform View=>ActorRoot.transform.Find("Visual_Player");
            public SpriteRenderer Body=>View.Find("Body").GetComponent<SpriteRenderer>();
            public SpriteRenderer Shadow=>View.Find("Shadow").GetComponent<SpriteRenderer>();
            public Fixture(bool motion=true)
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
                    globals=new HotbarSaveFixture(true,false);EntityVisualHooks.Reset();Zone=new Zone(ReferenceGladePlan.ZoneID);Actor=FlatPixelActorMotionTests.Actor("motion-player","Player");Zone.AddEntity(Actor,8,9);
                    for(int y=7;y<12;y++)for(int x=6;x<14;x++)Zone.GetCell(x,y).Explored=Zone.GetCell(x,y).IsVisible=true;
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
            public void Tick(float dt)=>Call(Actors,"AdvancePresentation",dt);
            public void Pose(int index){Assert.NotNull(Body.sprite);Assert.AreSame(Motion,Body.sprite.texture);Assert.AreEqual(new Rect(index*16,0,16,24),Body.sprite.rect);}
            public void Idle(){Assert.NotNull(Body.sprite);Assert.AreNotSame(Motion,Body.sprite.texture);Assert.AreEqual(new Rect(0,0,16,24),Body.sprite.rect);}
            public Tile Foreground(int x,int y)=>overlay.GetTile<Tile>(new Vector3Int(x,CavesOfOoo.Core.Zone.Height-1-y,0));
            public TileBase Background(int x,int y)=>bg.GetTile(new Vector3Int(x,CavesOfOoo.Core.Zone.Height-1-y,0));
            public void Quiet(){Assert.False(FullDirty);Assert.Zero(((HashSet<int>)Field("_dirtyCells").GetValue(Renderer)).Count);}
            public void ClearDirty(){Field("_fullDirty").SetValue(Renderer,false);((HashSet<int>)Field("_dirtyCells").GetValue(Renderer)).Clear();}
            static FieldInfo Field(string name)=>typeof(ZoneRenderer).GetField(name,I);
            public void Dispose(){try{graph?.CleanupCameraFixture();if(Motion!=null)Object.DestroyImmediate(Motion);}finally{globals?.Dispose();restore?.Invoke();}}
        }
    }
}
