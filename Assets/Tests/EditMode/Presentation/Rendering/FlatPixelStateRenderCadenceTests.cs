using System;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object=UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Actual submitted overlay after native actions and a normal renderer frame.
    // Quiet staged graph; does not claim keyboard/controller or travel acceptance.
    public sealed class FlatPixelStateRenderCadenceTests
    {
        const BindingFlags I=BindingFlags.Instance|BindingFlags.NonPublic;
        const BindingFlags S=BindingFlags.Static|BindingFlags.NonPublic;
        [Test] public void LooseTorchTogglesWithoutMovementOrForcedRefresh()
        {
            using(var f=new Fixture("Torch"))
            {
                f.Expect(10);f.Quiet();var cell=f.Zone.GetEntityCell(f.Owner);int version=f.Zone.EntityVersion;
                Assert.True(InventorySystem.PerformAction(f.Actor,f.Owner,TorchLightPart.ExtinguishCommand,f.Zone));
                f.Frame();f.Expect(9);Assert.AreSame(cell,f.Zone.GetEntityCell(f.Owner));Assert.AreEqual(version,f.Zone.EntityVersion);
                f.Quiet();Assert.False(InventorySystem.PerformAction(f.Actor,f.Owner,TorchLightPart.ExtinguishCommand,f.Zone));
                f.Quiet();f.Frame();f.Expect(9);
                var fire=f.Factory.CreateEntity("Campfire");Assert.True(f.Zone.AddEntity(fire,9,11));f.Frame();f.Quiet();
                Assert.True(InventorySystem.PerformAction(f.Actor,f.Owner,TorchLightPart.LightCommand,f.Zone));
                f.Frame();f.Expect(10);Assert.AreSame(cell,f.Zone.GetEntityCell(f.Owner));
            }
        }
        [TestCase(.1f,9)][TestCase(50f,10)]
        public void ActualLooseMaterialTickRepaintsExhaustionButNotUnchangedFlame(float fuel,int expected)
        {
            using(var f=new Fixture("Torch"))
            {
                f.Owner.GetPart<FuelPart>().FuelMass=fuel;f.Expect(10);f.Quiet();
                var cell=f.Zone.GetEntityCell(f.Actor);MaterialSimSystem.TickMaterialEntities(f.Zone);
                Assert.False(f.FullDirty);if(expected==10)f.Quiet();
                f.Frame();f.Expect(expected);Assert.AreSame(cell,f.Zone.GetEntityCell(f.Actor));
                Assert.AreEqual(Math.Max(0,fuel-.3f),f.Owner.GetPart<FuelPart>().FuelMass,.0001f);
            }
        }
        [Test] public void FailedInventoryActionRollsBackAndRepaintsFinalTorchState()
        {
            using(var f=new Fixture("Torch"))
            {
                bool sawPendingOff=false;
                f.Actor.AddPart(new ObserveAndThrowPart(()=>{f.Frame();sawPendingOff=f.Submitted?.sprite.rect.x==144;}));
                f.Expect(10);f.Quiet();
                Assert.False(InventorySystem.PerformAction(f.Actor,f.Owner,TorchLightPart.ExtinguishCommand,f.Zone));
                Assert.True(sawPendingOff,"Rollback must repaint even if a listener observed the tentative state.");
                Assert.False(f.FullDirty);f.Frame();f.Expect(10);
                Assert.True(f.Owner.GetPart<LightSourcePart>().Enabled);Assert.AreEqual(450,f.Owner.GetPart<ThermalPart>().Temperature);
            }
        }
        [Test] public void DirectBurningOnsetAndRemovalRepaintResidualCoals()
        {
            using(var f=new Fixture("Campfire",200))
            {
                f.Expect(4);f.Quiet();Assert.True(f.Owner.ApplyEffect(new BurningEffect()));f.Frame();f.Expect(5);
                f.Quiet();Assert.True(f.Owner.RemoveEffect<BurningEffect>());f.Frame();f.Expect(4);
                f.Quiet();Assert.False(f.Owner.RemoveEffect<BurningEffect>());f.Quiet();
            }
        }
        [TestCase(false)][TestCase(true)]
        public void DirectWetOrFrozenTransitionUpdatesControlledTorchBeforeItsNextTick(bool frozen)
        {
            using(var f=new Fixture("Torch"))
            {
                f.Expect(10);f.Quiet();
                Assert.True(f.Owner.ApplyEffect(frozen?(Effect)new FrozenEffect(1):new WetEffect(1)));
                f.Frame();f.Expect(9);f.Quiet();
                Assert.True(frozen?f.Owner.RemoveEffect<FrozenEffect>():f.Owner.RemoveEffect<WetEffect>());
                f.Frame();f.Expect(10);
                Assert.AreEqual(50,f.Owner.GetPart<FuelPart>().FuelMass,"Presentation never performs a gameplay tick.");
            }
        }
        [TestCase(150f,false,3)][TestCase(500f,false,4)][TestCase(300f,true,4)]
        public void NativeThermalEndTurnUpdatesOnlyTheChangedState(float temperature,bool burning,int expected)
        {
            using(var f=new Fixture("Campfire",temperature,burning))
            {
                f.Expect(burning?5:4);f.Quiet();var cell=f.Zone.GetEntityCell(f.Actor);
                var e=GameEvent.New("EndTurn");e.SetParameter("Zone",f.Zone);
                try{f.Owner.GetPart<ThermalPart>().HandleEvent(e);}finally{e.Release();}
                Assert.False(f.FullDirty);if(temperature==500)f.Quiet();
                f.Frame();f.Expect(expected);Assert.AreSame(cell,f.Zone.GetEntityCell(f.Actor));
            }
        }
        [Test] public void HiddenFireChangesDoNotRevealAStateTile()
        {
            using(var f=new Fixture("Campfire",200))
            {
                f.Expect(4);f.Zone.GetCell(10,10).IsVisible=false;
                Assert.True(f.Owner.ApplyEffect(new BurningEffect()));f.Frame();Assert.IsNull(f.Submitted);
                f.Quiet();Assert.True(f.Owner.RemoveEffect<BurningEffect>());f.Frame();Assert.IsNull(f.Submitted);
            }
        }
        [TestCase("removed")][TestCase("foreign-part")][TestCase("foreign-physics")][TestCase("carried")]
        public void RetainedTorchCannotDirtyAnUnrelatedCurrentCell(string fault)
        {
            using(var f=new Fixture("Torch"))
            {
                var torch=f.Owner.GetPart<TorchLightPart>();
                if(fault=="removed"){f.Zone.RemoveEntity(f.Owner);f.Frame();}
                if(fault=="foreign-part")torch.ParentEntity=new Entity();
                if(fault=="foreign-physics")f.Owner.GetPart<PhysicsPart>().ParentEntity=new Entity();
                if(fault=="carried")f.Owner.GetPart<PhysicsPart>().InInventory=f.Actor;
                f.Quiet();var e=GameEvent.New("EffectApplied");e.SetParameter("Target",f.Owner);e.SetParameter("Effect",new BurningEffect());
                try{torch.HandleEvent(e);}finally{e.Release();}f.Quiet();
            }
        }
        static void Call(object owner,string name)
        {try{owner.GetType().GetMethod(name,I).Invoke(owner,null);}catch(TargetInvocationException e){throw e.InnerException??e;}}
        sealed class ObserveAndThrowPart:Part
        {
            readonly Action callback;public ObserveAndThrowPart(Action callback){this.callback=callback;}
            public override bool HandleEvent(GameEvent e)
            {if(e.ID=="AfterInventoryAction"){callback();throw new InvalidOperationException("state rollback");}return true;}
        }
        sealed class Fixture:IDisposable
        {
            readonly GameObject root;
            readonly Texture2D atlas;
            readonly Action<int,int,string> oldCell=ZoneRenderHooks.CellDirtyCallback;
            readonly Action<string> oldFull=ZoneRenderHooks.FullDirtyCallback;
            readonly FieldInfo bound=typeof(ZoneTileStateSystem).GetField("_boundRenderZone",S);
            readonly object oldBound; readonly Zone previous;readonly Action<int,int> oldChanged;readonly Action oldSight;
            readonly Zone oldActive=SettlementRuntime.ActiveZone;
            readonly HotbarSaveFixture globals;
            readonly Tilemap overlay;
            readonly ZoneRenderer renderer;
            public readonly CavesOfOoo.Data.EntityFactory Factory;
            public readonly Zone Zone;public readonly Entity Owner,Actor;
            public bool FullDirty=>(bool)Field("_fullDirty").GetValue(renderer);
            public Tile Submitted=>overlay.GetTile<Tile>(new Vector3Int(10,CavesOfOoo.Core.Zone.Height-1-10,0));
            public Fixture(string name,float temperature=500,bool burning=false)
            {
                oldBound=bound.GetValue(null);
                if(oldBound is WeakReference<Zone> weak&&weak.TryGetTarget(out var old)){previous=old;oldChanged=old.TileState.OnCellChanged;oldSight=old.TileState.OnSightChanged;}
                try
                {
                    globals=new HotbarSaveFixture(true,false);Factory=DensityReferenceGladeTests.Factory();Zone=new Zone(ReferenceGladePlan.ZoneID);
                    Actor=Factory.CreateEntity("Player");Owner=Factory.CreateEntity(name);
                    if(name=="Campfire"){Owner.GetPart<CampfirePart>().FiniteCooking=true;Owner.GetPart<ThermalPart>().Temperature=temperature;}
                    Assert.True(Zone.AddEntity(Actor,9,10));Assert.True(Zone.AddEntity(Owner,10,10));
                    for(int y=9;y<=11;y++)for(int x=9;x<=11;x++)Zone.GetCell(x,y).IsVisible=Zone.GetCell(x,y).Explored=true;
                    if(burning){Assert.True(Owner.ApplyEffect(new BurningEffect()));Owner.GetPart<ThermalPart>().Temperature=temperature;}
                    SettlementRuntime.ActiveZone=Zone;
                    root=new GameObject("Owned state cadence",typeof(Grid));root.SetActive(false);
                    var main=new GameObject("Main",typeof(Tilemap),typeof(TilemapRenderer));main.transform.SetParent(root.transform,false);
                    var bg=new GameObject("Background",typeof(Tilemap));bg.transform.SetParent(root.transform,false);
                    var camera=new GameObject("Camera",typeof(Camera));camera.transform.SetParent(root.transform,false);camera.GetComponent<Camera>().orthographic=true;
                    renderer=main.AddComponent<ZoneRenderer>();Field("_tilemap").SetValue(renderer,main.GetComponent<Tilemap>());
                    Field("_bgTilemap").SetValue(renderer,bg.GetComponent<Tilemap>());Field("_mainCamera").SetValue(renderer,camera.GetComponent<Camera>());
                    var env=root.AddComponent<EnvironmentSpriteRenderer>();env.Init(root.transform,main.GetComponent<Tilemap>(),bg.GetComponent<Tilemap>());
                    atlas=new Texture2D(192,24);typeof(EnvironmentSpriteRenderer).GetField("_flatPixelGreybox",I).SetValue(env,FlatPixelOpeningStatesTests.NewCatalog(null,null,null,atlas));
                    overlay=(Tilemap)typeof(EnvironmentSpriteRenderer).GetField("_overlayTilemap",I).GetValue(env);
                    Field("_envSpriteRenderer").SetValue(renderer,env);Field("_flatPixelModeEnabled").SetValue(renderer,true);
                    renderer.SetZone(Zone);ZoneRenderHooks.CellDirtyCallback=renderer.MarkCellDirty;ZoneRenderHooks.FullDirtyCallback=renderer.MarkDirty;
                    Frame();Assert.True(renderer.FlatPixelModeActive);Quiet();
                }
                catch{Dispose();throw;}
            }
            public void Frame()=>Call(renderer,"LateUpdate");
            public void Quiet(){Assert.False(FullDirty);Assert.Zero(((HashSet<int>)Field("_dirtyCells").GetValue(renderer)).Count);}
            public void Expect(int frame){Assert.NotNull(Submitted);Assert.AreSame(atlas,Submitted.sprite.texture);Assert.AreEqual(new Rect(frame*16,0,16,24),Submitted.sprite.rect,"Already submitted foreground is stale.");}
            static FieldInfo Field(string name)=>typeof(ZoneRenderer).GetField(name,I);
            public void Dispose()
            {
                try{if(root!=null)Object.DestroyImmediate(root);if(atlas!=null)Object.DestroyImmediate(atlas);}
                finally
                {
                    globals?.Dispose();ZoneRenderHooks.CellDirtyCallback=oldCell;ZoneRenderHooks.FullDirtyCallback=oldFull;SettlementRuntime.ActiveZone=oldActive;
                    bound.SetValue(null,oldBound);if(previous!=null){previous.TileState.OnCellChanged=oldChanged;previous.TileState.OnSightChanged=oldSight;}
                }
            }
        }
    }
}
