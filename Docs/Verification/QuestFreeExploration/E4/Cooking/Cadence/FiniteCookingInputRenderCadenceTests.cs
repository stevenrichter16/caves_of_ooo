using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object=UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Native integration: real input completion and ordinary renderer frame.
    // Quiet staged graph, not keyboard delivery or an ordinary travel witness.
    public sealed class FiniteCookingInputRenderCadenceTests
    {
        const BindingFlags I=BindingFlags.Instance|BindingFlags.NonPublic;
        const BindingFlags S=BindingFlags.Static|BindingFlags.NonPublic;
        const string Hot="spread-cooking-coals-hot",Cooled="spread-cooking-coals-cooled";

        [TestCase(150f,Cooled)]
        [TestCase(500f,Hot)]
        public void PaidInputUpdatesBoundThermalModelWithoutForcedRefresh(float initial,string expected)
        {
            using(var f=new Fixture(initial))
            {
                Assert.False(f.Host.FullDirty);Assert.AreEqual(0,f.Host.DirtyCount);
                var before=Mesh(f,Hot);var cell=f.Art.Zone.GetEntityCell(f.Coals);
                var playerCell=f.Art.Zone.GetEntityCell(f.Art.Player);int version=f.Art.Zone.EntityVersion;
                string id=f.Coals.ID;int tick=f.Turns.TickCount,energy=f.Turns.GetEnergy(f.Art.Player);
                int speed=f.Art.Player.GetStat("Speed").Value;Assert.AreEqual(100,speed);
                Assert.AreEqual(1000,energy);Assert.True(f.Turns.WaitingForInput);Assert.AreSame(f.Art.Player,f.Turns.CurrentActor);
                CollectionAssert.AreEqual(new[]{f.Art.Player},f.Turns.GetSavedEntries().Select(e=>e.Entity));

                // This is the normal Wait completion path. No direct material
                // tick or presentation invalidation/refresh is used below.
                Call(f.Input,"EndTurnAndProcess");
                Assert.AreEqual(tick+10,f.Turns.TickCount);
                Assert.AreEqual(energy-1000+(f.Turns.TickCount-tick)*speed,f.Turns.GetEnergy(f.Art.Player));
                Assert.True(f.Turns.WaitingForInput);Assert.AreSame(f.Art.Player,f.Turns.CurrentActor);
                Assert.AreEqual(initial-(initial-25f)*.02f,f.Coals.GetPart<ThermalPart>().Temperature,.0001f);
                Assert.AreEqual(25f,f.Coals.GetPart<FuelPart>().FuelMass);
                Assert.AreEqual(version,f.Art.Zone.EntityVersion);Assert.AreEqual(id,f.Coals.ID);
                Assert.AreSame(cell,f.Art.Zone.GetEntityCell(f.Coals));Assert.AreSame(playerCell,f.Art.Zone.GetEntityCell(f.Art.Player));
                TestContext.WriteLine("Before normal frame: start="+initial+", temperature="+f.Coals.GetPart<ThermalPart>().Temperature+", fullDirty="+f.Host.FullDirty+", dirtyCells="+f.Host.DirtyCount);
                f.Host.Frame();f.Art.Frame();
                var after=Mesh(f,expected);
                if(expected==Cooled)Assert.AreNotSame(before,after);else Assert.AreSame(before,after);
                Assert.AreEqual(initial-(initial-25f)*.02f,f.Coals.GetPart<ThermalPart>().Temperature,.0001f,"Render frames cannot cool the source.");
                Assert.AreSame(cell,f.Art.Zone.GetEntityCell(f.Coals));Assert.AreSame(playerCell,f.Art.Zone.GetEntityCell(f.Art.Player));
            }
        }
        static Mesh Mesh(Fixture f,string expected)
        {
            // Inspect the already submitted view first, not a new recipe.
            Assert.True(f.Art.Find(f.Coals,out var view,out var model));Assert.AreEqual(expected,model,"Normal render frame retained a stale thermal model.");
            Assert.True(SpawnRing3DIntegrationFixture.Drawn(view));var mesh=view.GetComponent<MeshFilter>().sharedMesh;
            var prefab=Resources.Load<GameObject>("SpreadCookingCoals3D/"+expected);Assert.NotNull(prefab);Assert.AreSame(prefab.GetComponent<MeshFilter>().sharedMesh,mesh);return mesh;
        }
        static void Call(object owner,string name)
        {var method=owner.GetType().GetMethod(name,I);Assert.NotNull(method,name);try{method.Invoke(owner,null);}catch(TargetInvocationException e){throw e.InnerException??e;}}

        sealed class Fixture:IDisposable
        {
            readonly UnityEngine.Random.State random=UnityEngine.Random.state;
            readonly FieldInfo bound=typeof(ZoneTileStateSystem).GetField("_boundRenderZone",S);
            readonly object oldBound;
            readonly Zone previous;
            readonly Action<int,int> oldChanged;
            readonly Action oldSight;
            readonly HotbarSaveFixture globals;
            public readonly SpawnRing3DIntegrationFixture Art;
            public readonly RendererHost Host;
            public readonly Entity Coals;
            public readonly TurnManager Turns;
            public InputHandler Input=>globals.Input;
            public Fixture(float temperature)
            {
                oldBound=bound.GetValue(null);
                if(oldBound is WeakReference<Zone> weak&&weak.TryGetTarget(out var zone)){previous=zone;oldChanged=zone.TileState.OnCellChanged;oldSight=zone.TileState.OnSightChanged;}
                try
                {
                    globals=new HotbarSaveFixture(true,false);
                    Art=new SpawnRing3DIntegrationFixture("Overworld.12.10.0");
                    Assert.True(Art.Zone.RemoveEntity(Art.Player));
                    // Explicit isolated quiet graph eliminates moving NPCs and
                    // their unrelated dirty writes; all displayed owners are real.
                    Art.Zone=new Zone("Overworld.12.10.0");
                    for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)Assert.True(Art.Zone.AddEntity(Art.Factory.CreateEntity("Grass"),x,y));
                    Art.Manager.ReplaceLoadedState(new Dictionary<string,Zone>{{Art.Zone.ZoneID,Art.Zone}},Art.Zone.ZoneID,new Dictionary<string,List<ZoneConnection>>());
                    Assert.True(Art.Zone.AddEntity(Art.Player,39,12));Art.Player.GetPart<BrainPart>().CurrentZone=Art.Zone;
                    Coals=Art.Factory.CreateEntity("SpreadCookingCoals");Assert.NotNull(Coals);Assert.True(Coals.GetPart<CampfirePart>().FiniteCooking);Assert.False(Coals.GetPart<CampfirePart>().AllowRest);
                    Assert.AreEqual(500,Coals.GetPart<ThermalPart>().Temperature);Assert.AreEqual(25,Coals.GetPart<FuelPart>().FuelMass);Assert.False(Coals.HasEffect<BurningEffect>());
                    Coals.GetPart<ThermalPart>().Temperature=temperature;Assert.True(Art.Zone.AddEntity(Coals,40,12));Art.Reveal();
                    Turns=new TurnManager();Turns.RestoreSavedState(0,true,Art.Player,new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=Art.Player,Energy=1000}});
                    TurnManager.World=null;MaterialReactionResolver.Factory=Art.Factory;
                    Input.CurrentZone=Art.Zone;Input.ZoneManager=Art.Manager;Input.PlayerEntity=Art.Player;Input.TurnManager=Turns;
                    Host=new RendererHost(Art);Input.ZoneRenderer=Host.Renderer;
                    Host.Frame();Art.Frame();
                }
                catch{Dispose();throw;}
            }
            public void Dispose()
            {
                try{Host?.Dispose();}finally
                {try{Art?.Dispose();}finally
                 {try{globals?.Dispose();}finally
                  {bound.SetValue(null,oldBound);if(previous!=null){previous.TileState.OnCellChanged=oldChanged;previous.TileState.OnSightChanged=oldSight;}UnityEngine.Random.state=random;}}}
            }
        }
        sealed class RendererHost:IDisposable
        {
            readonly GameObject root;
            readonly Action<int,int,string> oldCell=ZoneRenderHooks.CellDirtyCallback;
            readonly Action<string> oldFull=ZoneRenderHooks.FullDirtyCallback;
            public readonly ZoneRenderer Renderer;
            public bool FullDirty=>(bool)Field("_fullDirty").GetValue(Renderer);
            public int DirtyCount=>((HashSet<int>)Field("_dirtyCells").GetValue(Renderer)).Count;
            public RendererHost(SpawnRing3DIntegrationFixture art)
            {
                try
                {
                    root=new GameObject("Owned finite-input render host");root.SetActive(false);root.AddComponent<Grid>();
                    var tile=new GameObject("Native tilemap",typeof(Tilemap),typeof(TilemapRenderer));tile.transform.SetParent(root.transform,false);
                    Renderer=tile.AddComponent<ZoneRenderer>();Field("_tilemap").SetValue(Renderer,tile.GetComponent<Tilemap>());
                    Field("_mainCamera").SetValue(Renderer,art.Source);Field("_spawnRing3DPresenter").SetValue(Renderer,art.Presenter);
                    Field("_envSpriteRenderer").SetValue(Renderer,root.AddComponent<EnvironmentSpriteRenderer>());
                    Renderer.PlayerEntity=art.Player;Renderer.SetZone(art.Zone);
                    ZoneRenderHooks.CellDirtyCallback=Renderer.MarkCellDirty;ZoneRenderHooks.FullDirtyCallback=Renderer.MarkDirty;
                }
                catch{Dispose();throw;}
            }
            public void Frame()=>Call(Renderer,"LateUpdate");
            static FieldInfo Field(string name)=>typeof(ZoneRenderer).GetField(name,I);
            public void Dispose(){if(root!=null)Object.DestroyImmediate(root);ZoneRenderHooks.CellDirtyCallback=oldCell;ZoneRenderHooks.FullDirtyCallback=oldFull;}
        }
    }
}
