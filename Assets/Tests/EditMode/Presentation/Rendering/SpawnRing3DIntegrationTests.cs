using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>Actual ring assets + actual native graphs/commands. Reflection only
    /// permits an honest missing-presenter RED. Import real art before GREEN;
    /// there are no fake-positive model bindings or manual native regeneration.</summary>
    internal sealed class SpawnRing3DIntegrationFixture:IDisposable
    {
        public const string Grove="Overworld.2.6.0",Fen="Overworld.2.7.0";
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        readonly EntityEquipmentContentFixture scope;
        readonly bool oldEnabled,oldLow;
        readonly EntityFactory oldHarvest;
        readonly Type presenterType;
        public readonly GameObject Root;
        public readonly Camera Source;
        public readonly RenderTexture Borrowed;
        public readonly MonoBehaviour Presenter;
        public readonly SpawnRing3DLibrary Library;
        public readonly EntityFactory Factory;
        public OverworldZoneManager Manager;
        public Zone Zone;
        public Entity Player;
        public LightMap Light=new LightMap();
        Camera ownedCamera;
        public SpawnRing3DIntegrationFixture(string zoneId=Grove,bool legacyFen=false)
        {
            presenterType=typeof(Village3DPresenter).Assembly.GetType("CavesOfOoo.Rendering.SpawnRing3DPresenter");
            Assert.NotNull(presenterType,"Record missing-presenter RED before implementing the ring owner adapter.");
            oldEnabled=Village3DSettings.Enabled;oldLow=Village3DSettings.LowDetail;oldHarvest=HarvestablePart.Factory;
            try
            {
                scope=new EntityEquipmentContentFixture();Factory=scope.Factory;HarvestablePart.Factory=Factory;
                Village3DSettings.Enabled=true;Village3DSettings.LowDetail=false;
                Library=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
                Assert.NotNull(Library,"Actual final ring export must be imported before this integration GREEN.");Library.Validate();
                Root=new GameObject("Owned ring integration");var cameraObject=new GameObject("Borrowed ordinary camera");cameraObject.transform.SetParent(Root.transform,false);
                Source=cameraObject.AddComponent<Camera>();Source.enabled=false;Source.orthographic=true;Source.orthographicSize=12.75f;
                Source.transform.position=new Vector3(40,12.75f,-10);Source.cullingMask=(1<<8)|1;
                Borrowed=new RenderTexture(960,640,16);Borrowed.Create();Source.targetTexture=Borrowed;Source.rect=new Rect(.05f,.1f,.75f,.8f);
                Source.aspect=Source.pixelRect.width/Source.pixelRect.height;
                Manager=new OverworldZoneManager(Factory,729490642);
                if(legacyFen)Assert.AreEqual(Fen,zoneId,"The explicit fen fixture must not replace the authored pilot site.");
                Zone=legacyFen?CreateLegacyFen(Factory):Manager.GetZone(zoneId);Manager.SetActiveZone(Zone);
                Player=Factory.CreateEntity("Player");Assert.NotNull(Player);var at=FreeCell();Assert.IsTrue(Zone.AddEntity(Player,at.x,at.y));
                Reveal();Presenter=(MonoBehaviour)Root.AddComponent(presenterType);Set("FullReveal",false);Bind(Zone);Refresh();ownedCamera=Get<Camera>("WorldCamera");
                Assert.IsTrue(Get<bool>("IsReady"),Get<string>("Failure"));Assert.IsTrue(Get<bool>("PresentationVisible"));
            }
            catch{Dispose();throw;}
        }
        // Water renderer regression uses real native formation generation in
        // a supported neighbour. Fresh3.7 is now the ridge pilot, not a fen.
        public static Zone CreateLegacyFen(EntityFactory factory)
        {
            var zone=new Zone(Fen);
            for(int y=0;y<25;y++)for(int x=0;x<80;x++)Assert.IsTrue(zone.AddEntity(factory.CreateEntity("Grass"),x,y));
            var builder=new GrovelandsFormationBuilder{Override=Formation.TendrilFen};
            Assert.IsTrue(builder.BuildZone(zone,factory,new System.Random(729490642)));
            Assert.AreEqual(Formation.TendrilFen,builder.LastFormation);return zone;
        }
        public T Get<T>(string name)
        {
            var p=presenterType.GetProperty(name,BindingFlags.Public|BindingFlags.Instance);Assert.NotNull(p,name);
            try{return(T)p.GetValue(Presenter);}catch(TargetInvocationException e){throw e.InnerException??e;}
        }
        public void Set(string name,object value)
        {var p=presenterType.GetProperty(name,BindingFlags.Public|BindingFlags.Instance);Assert.NotNull(p,name);p.SetValue(Presenter,value);}
        public object Call(string name,params object[] args)
        {
            var method=presenterType.GetMethod(name,BindingFlags.Public|BindingFlags.Instance);Assert.NotNull(method,name);
            try{return method.Invoke(Presenter,args);}catch(TargetInvocationException e){throw e.InnerException??e;}
        }
        public void Bind(Zone zone)=>Call("Bind",zone,Source);
        public void Refresh(HashSet<int> dirty=null){Light.Compute(Zone);Call("Refresh",Light,dirty);Frame();}
        public void Frame()
        {
            var method=presenterType.GetMethod("LateUpdate",Private)??presenterType.GetMethod("Update",Private);
            Assert.NotNull(method,"Ordinary frame synchronization is required.");
            try{method.Invoke(Presenter,null);}catch(TargetInvocationException e){throw e.InnerException??e;}
        }
        public void Reveal(){for(int y=0;y<25;y++)for(int x=0;x<80;x++)Zone.GetCell(x,y).Explored=Zone.GetCell(x,y).IsVisible=true;}
        public bool Authored(Entity e)=>(bool)Call("IsAuthoredEntity",e);
        public bool Rendered(Entity e)=>(bool)Call("IsRenderedEntity",e);
        public int Revision(int x,int y)=>(int)Call("GroundPatchRevision",x,y);
        public bool Water(int x,int y)=>(bool)Call("HasRepresentedWater",x,y);
        public bool Find(Entity entity,out GameObject root,out string model)
        {object[] args={entity,null,null};bool found=(bool)Call("TryGetEntityView",args);root=args[1]as GameObject;model=args[2]as string;return found;}
        public GameObject View(Entity entity)
        {Assert.IsTrue(Find(entity,out var root,out string model),entity.BlueprintName);Assert.NotNull(root);Assert.NotNull(Library.Definition.FindModel(model));return root;}
        public bool Equipment(Entity actor,Entity item,out GameObject root)
        {object[] args={actor,item,null};bool found=(bool)Call("TryGetEquipmentView",args);root=args[2]as GameObject;return found;}
        public static bool Drawn(GameObject root)=>root!=null&&root.GetComponentsInChildren<Renderer>(true).Any(r=>r.enabled&&r.gameObject.activeInHierarchy&&!r.forceRenderingOff);
        public static void Hidden(GameObject root)
        {if(root!=null)Assert.IsFalse(Drawn(root),"Detached/hidden independent view still submits geometry.");}
        public (int x,int y) FreeCell(int excludePatchX=-1,int excludePatchY=-1)
        {
            for(int y=1;y<24;y++)for(int x=1;x<79;x++)
            {var cell=Zone.GetCell(x,y);if((x/10!=excludePatchX||y/5!=excludePatchY)&&!cell.BlocksMovement(Player)
                &&!cell.Objects.Any(e=>e.HasPart<BrainPart>()||e.HasTag("Player")))return(x,y);}
            Assert.Fail("Actual native graph lacks a free fixture cell.");return(-1,-1);
        }
        public Entity Add(string blueprint,int x=-1,int y=-1)
        {var entity=Factory.CreateEntity(blueprint);Assert.NotNull(entity,blueprint);if(x<0){var p=FreeCell();x=p.x;y=p.y;}Assert.IsTrue(Zone.AddEntity(entity,x,y));return entity;}
        public HashSet<int> Dirty(Entity entity){var p=Zone.GetEntityPosition(entity);return Dirty(p.x,p.y);}
        public static HashSet<int> Dirty(int x,int y)=>new HashSet<int>{y*Zone.Width+x};
        public static string Tiles(Zone zone)
        {var keys=new List<int>();zone.TileState.CollectWrittenKeys(keys);return string.Join("|",keys.OrderBy(k=>k).Select(k=>k+":"+JsonUtility.ToJson(zone.TileState.Get(k%80,k/80))));}
        public void Approach(Entity target)
        {
            var p=Zone.GetEntityPosition(target);
            for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)
            {var cell=Zone.GetCell(p.x+dx,p.y+dy);if(cell!=null&&!cell.BlocksMovement(Player))
                {Assert.IsTrue(Zone.MoveEntity(Player,cell.X,cell.Y));return;}}
            Assert.Fail("No native approach for "+target.BlueprintName);
        }
        public Entity Equip(Entity actor,string blueprint)
        {
            var item=Factory.CreateEntity(blueprint);Assert.NotNull(item);var inv=actor.GetPart<InventoryPart>();Assert.NotNull(inv);
            Assert.IsTrue(inv.AddObject(item));Assert.IsTrue(InventorySystem.Equip(actor,item),"Native equip precondition "+actor.BlueprintName+"/"+blueprint);return item;
        }
        public void CleanGear(Entity actor)
        {foreach(var item in actor.GetPart<InventoryPart>().GetAllEquipped().ToArray())Assert.IsTrue(InventorySystem.UnequipItem(actor,item));}
        public bool Pick(Entity expected,out Vector2 point)
        {
            var root=View(expected);Physics.SyncTransforms();
            foreach(var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if(!r.enabled||!r.gameObject.activeInHierarchy)continue;var b=r.bounds;
                for(int ix=0;ix<9;ix++)for(int iz=0;iz<9;iz++)
                {
                    var p=new Vector2(Mathf.Lerp(b.min.x,b.max.x,(ix+.5f)/9),Mathf.Lerp(b.min.z,b.max.z,(iz+.5f)/9));
                    object[] args={p,null,0,0};if((bool)Call("TryPickWorld",args)&&ReferenceEquals(args[1],expected)){point=p;return true;}
                }
            }
            point=default;return false;
        }
        public GameSessionState RoundTrip()
        {
            Manager.SetActiveZone(Zone);var turns=new TurnManager();turns.RestoreSavedState(19,true,Player,
                new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=Player,Energy=1000}});
            var state=GameSessionState.Capture("ring-integration-owned","ring-integration",Manager,turns,Player);
            using(var stream=new MemoryStream()){state.Save(new SaveWriter(stream));stream.Position=0;return GameSessionState.Load(new SaveReader(stream,Factory));}
        }
        public void BindLoaded(GameSessionState loaded)
        {Manager=loaded.ZoneManager;Zone=Manager.ActiveZone;Player=loaded.Player;Assert.NotNull(Zone);Reveal();Light=new LightMap();Bind(Zone);Refresh();}
        public void Dispose()
        {
            if(Root!=null)Object.DestroyImmediate(Root);if(ownedCamera!=null)Object.DestroyImmediate(ownedCamera.gameObject);
            if(Borrowed!=null){Borrowed.Release();Object.DestroyImmediate(Borrowed);}
            HarvestablePart.Factory=oldHarvest;Village3DSettings.Enabled=oldEnabled;Village3DSettings.LowDetail=oldLow;scope?.Dispose();
        }
    }

    public sealed class SpawnRing3DIntegrationTests
    {
        static void Near(Vector3 expected,Vector3 actual)=>Assert.Less(Vector3.Distance(expected,actual),.001f);
        [Test] public void ActualGraphBindsWithoutChangingNativeEntitiesPositionsTilesOrEquipment()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var members=f.Zone.GetReadOnlyEntities().ToArray();var points=members.Select(f.Zone.GetEntityPosition).ToArray();var ids=members.Select(e=>e.ID).ToArray();
                string tiles=f.Zone.TileState.ToSaveString();int version=f.Zone.EntityVersion,bus=EquipmentChangeBus.GlobalVersion;
                for(int i=0;i<3;i++)f.Refresh(new HashSet<int>());
                CollectionAssert.AreEquivalent(members,f.Zone.GetReadOnlyEntities());CollectionAssert.AreEqual(points,members.Select(f.Zone.GetEntityPosition));
                CollectionAssert.AreEqual(ids,members.Select(e=>e.ID));Assert.AreEqual(tiles,f.Zone.TileState.ToSaveString());Assert.AreEqual(version,f.Zone.EntityVersion);Assert.AreEqual(bus,EquipmentChangeBus.GlobalVersion);
                Assert.AreSame(f.Zone,f.Get<Zone>("CurrentZone"));Assert.IsTrue(f.Rendered(f.Player));Assert.IsTrue(SpawnRing3DIntegrationFixture.Drawn(f.View(f.Player)));
                Assert.That(f.Get<int>("GroundPatchCount"),Is.InRange(1,40));
            }
        }
        [Test] public void SameGraphBindAndEmptyDirtyRefreshReuseCameraTargetAndIndependentActorRoot()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {var actor=f.View(f.Player);var camera=f.Get<Camera>("WorldCamera");var target=camera.targetTexture;int builds=f.Get<int>("GroundBuildCount");
             f.Bind(f.Zone);f.Refresh(new HashSet<int>());Assert.AreSame(actor,f.View(f.Player));Assert.AreSame(camera,f.Get<Camera>("WorldCamera"));Assert.AreSame(target,camera.targetTexture);Assert.AreEqual(builds,f.Get<int>("GroundBuildCount"));}
        }
        [Test] public void DirtyStaticVisibilityRebuildsOnlyItsTenByFivePatchWithoutMembershipChange()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var a=f.Add("Rock");var at=f.Zone.GetEntityPosition(a);var far=f.FreeCell(at.x/10,at.y/5);var b=f.Add("Rock",far.x,far.y);f.Refresh();
                Assert.IsTrue(f.Rendered(a));Assert.IsTrue(f.Rendered(b));int own=f.Revision(at.x,at.y),other=f.Revision(far.x,far.y),version=f.Zone.EntityVersion;
                a.GetPart<RenderPart>().Visible=false;f.Refresh(SpawnRing3DIntegrationFixture.Dirty(at.x,at.y));
                Assert.AreEqual(version,f.Zone.EntityVersion);Assert.IsFalse(f.Rendered(a));Assert.IsTrue(f.Rendered(b));Assert.Greater(f.Revision(at.x,at.y),own);Assert.AreEqual(other,f.Revision(far.x,far.y));
                a.GetPart<RenderPart>().Visible=true;f.Refresh(SpawnRing3DIntegrationFixture.Dirty(at.x,at.y));Assert.IsTrue(f.Rendered(a));
            }
        }
        [Test] public void BatchedNeighborsShareAPatchRootWhileActorsAndTakeablesOwnTheirRoots()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var a=f.Add("Rock",20,10);var b=f.Add("Bush",21,10);var c=f.Add("Tepuibone",22,10);var d=f.Add("GlasspaneFrog",23,10);f.Refresh();
                Assert.AreSame(f.View(a),f.View(b));Assert.AreNotSame(f.View(a),f.View(c));Assert.AreNotSame(f.View(a),f.View(d));Assert.AreNotSame(f.View(c),f.View(d));
                foreach(var e in new[]{a,b,c,d})Assert.IsTrue(f.Authored(e));
            }
        }
        [Test] public void NativeHarvestRemovesItsBatchedContributionWithoutRegrowingTheOwner()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var vein=f.Add("TepuiboneVein");var at=f.Zone.GetEntityPosition(vein);f.Refresh();Assert.NotNull(vein.GetPart<HarvestablePart>());Assert.IsTrue(f.Rendered(vein));int revision=f.Revision(at.x,at.y);
                var e=GameEvent.New("InventoryAction");try{e.SetParameter("Command","Harvest");e.SetParameter("Actor",f.Player);e.SetParameter("Zone",f.Zone);e.SetParameter("Random",new System.Random(4));vein.FireEvent(e);Assert.IsTrue(e.Handled);}finally{e.Release();}
                Assert.IsNull(f.Zone.GetEntityCell(vein));f.Refresh(SpawnRing3DIntegrationFixture.Dirty(at.x,at.y));Assert.IsFalse(f.Authored(vein));Assert.IsFalse(f.Find(vein,out _,out _));
                Assert.Greater(f.Revision(at.x,at.y),revision);f.Refresh();Assert.IsNull(f.Zone.GetEntityCell(vein));Assert.IsFalse(f.Find(vein,out _,out _));
            }
        }
        [Test] public void ActualPickupDisposesTakeableViewAndKeepsNativeInventoryOwnership()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var p=f.Zone.GetEntityPosition(f.Player);var item=f.Add("Tepuibone",p.x,p.y);f.Refresh();var root=f.View(item);
                Assert.IsTrue(InventorySystem.Pickup(f.Player,item,f.Zone));Assert.IsNull(f.Zone.GetEntityCell(item));Assert.AreSame(f.Player,item.GetPart<PhysicsPart>().InInventory);
                f.Refresh(SpawnRing3DIntegrationFixture.Dirty(p.x,p.y));Assert.IsFalse(f.Find(item,out _,out _));SpawnRing3DIntegrationFixture.Hidden(root);
            }
        }
        [Test] public void ActualForcedMovementUpdatesOnlyTheExactActorAndLeavesOtherActorRootStable()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var actor=f.Add("GlasspaneFrog");var other=f.Add("GlasspaneFrog");f.Refresh();var own=f.View(actor);var untouched=f.View(other);var before=untouched.transform.position;
                var dest=f.FreeCell();Assert.IsTrue(MovementSystem.ForceMoveTo(actor,f.Zone,dest.x,dest.y));f.Frame();Near(Village3DProjection.CellCentre(dest.x,dest.y),own.transform.position);
                Assert.AreSame(untouched,f.View(other));Near(before,untouched.transform.position);Assert.AreSame(own,f.View(actor));
            }
        }
        [Test] public void ActorMemoryAndRenderVisibilityHideWithoutTurningNativeActorsIntoStaticGhosts()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var actor=f.Add("GlasspaneFrog");f.Refresh();var root=f.View(actor);var cell=f.Zone.GetEntityCell(actor);Assert.IsTrue(f.Rendered(actor));
                cell.IsVisible=false;f.Refresh(new HashSet<int>());Assert.IsFalse(f.Rendered(actor));SpawnRing3DIntegrationFixture.Hidden(root);Assert.IsTrue(cell.Explored);
                cell.IsVisible=true;f.Refresh(new HashSet<int>());Assert.IsTrue(f.Rendered(actor));actor.GetPart<RenderPart>().Visible=false;f.Refresh(f.Dirty(actor));Assert.IsFalse(f.Rendered(actor));
                actor.GetPart<RenderPart>().Visible=true;f.Refresh(f.Dirty(actor));Assert.IsTrue(f.Rendered(actor));
            }
        }
        [Test] public void NativeVisibilityRefreshDoesNotRebuildUnchangedGroundGeometry()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {var rock=f.Add("Rock");f.Refresh();int before=f.Get<int>("GroundBuildCount");var cell=f.Zone.GetEntityCell(rock);cell.IsVisible=false;cell.Explored=false;f.Refresh(new HashSet<int>());
             Assert.AreEqual(before,f.Get<int>("GroundBuildCount"));Assert.IsFalse(f.Rendered(rock));Assert.IsFalse(cell.Explored);cell.Explored=cell.IsVisible=true;f.Refresh(new HashSet<int>());Assert.IsTrue(f.Rendered(rock));}
        }
        [Test] public void RealFenCoatingWithoutPoolIsRepresentedAndDirtyErasureRebuildsOnlyThatPatch()
        {
            using(var f=new SpawnRing3DIntegrationFixture(SpawnRing3DIntegrationFixture.Fen,legacyFen:true))
            {
                var keys=new List<int>();f.Zone.TileState.CollectWrittenKeys(keys);int key=keys.First(k=>f.Zone.TileState.CoatingTurns(k%80,k/80,"water")==ZoneTileState.Permanent&&!f.Zone.GetCell(k%80,k/80).Objects.Any(e=>e.HasPart<LiquidPoolPart>()));
                int x=key%80,y=key/80;Assert.IsTrue(f.Water(x,y));int own=f.Revision(x,y);var far=f.FreeCell(x/10,y/5);int other=f.Revision(far.x,far.y);
                Assert.IsTrue(f.Zone.TileState.RemoveCoating(x,y,"water"));f.Refresh(SpawnRing3DIntegrationFixture.Dirty(x,y));Assert.IsFalse(f.Water(x,y));Assert.Greater(f.Revision(x,y),own);Assert.AreEqual(other,f.Revision(far.x,far.y));
            }
        }
        [Test] public void TemporaryWaterAndOtherNativeMarksAreNotClaimedAsPermanentRingWater()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var p=f.FreeCell();f.Zone.TileState.RemoveCoating(p.x,p.y,"water");f.Zone.TileState.WriteCoating(p.x,p.y,"water",7);f.Zone.TileState.WriteCoating(p.x,p.y,"oil",9);
                f.Refresh(SpawnRing3DIntegrationFixture.Dirty(p.x,p.y));Assert.IsFalse(f.Water(p.x,p.y));Assert.AreEqual(7,f.Zone.TileState.CoatingTurns(p.x,p.y,"water"));Assert.AreEqual(9,f.Zone.TileState.CoatingTurns(p.x,p.y,"oil"));
                f.Zone.TileState.WriteCoating(p.x,p.y,"water",ZoneTileState.Permanent);f.Refresh(SpawnRing3DIntegrationFixture.Dirty(p.x,p.y));Assert.IsTrue(f.Water(p.x,p.y));
                f.Call("SetPresentationVisible",false);Assert.IsFalse(f.Water(p.x,p.y));Assert.AreEqual(ZoneTileState.Permanent,f.Zone.TileState.CoatingTurns(p.x,p.y,"water"));
            }
        }
        [Test] public void FullSessionReloadUsesChangedSavedWaterWithoutRestoringTheGenerationMask()
        {
            using(var f=new SpawnRing3DIntegrationFixture(SpawnRing3DIntegrationFixture.Fen,legacyFen:true))
            {
                var keys=new List<int>();f.Zone.TileState.CollectWrittenKeys(keys);int erased=keys.First(k=>f.Zone.TileState.CoatingTurns(k%80,k/80,"water")==ZoneTileState.Permanent);
                Assert.IsTrue(f.Zone.TileState.RemoveCoating(erased%80,erased/80,"water"));
                int added=Enumerable.Range(0,2000).First(k=>k!=erased&&!f.Zone.TileState.HasCoating(k%80,k/80,"water"));
                f.Zone.TileState.WriteCoating(added%80,added/80,"water",ZoneTileState.Permanent);
                string expected=SpawnRing3DIntegrationFixture.Tiles(f.Zone);var loaded=f.RoundTrip();f.BindLoaded(loaded);
                Assert.AreEqual(expected,SpawnRing3DIntegrationFixture.Tiles(f.Zone));Assert.IsFalse(f.Water(erased%80,erased/80));Assert.IsTrue(f.Water(added%80,added/80));
            }
        }
        [Test] public void AllFellingComponentsHaveIndependentExactNativeOwnerRoots()
        {
            using(var f=new SpawnRing3DIntegrationFixture(FellingSiteBuilder.ZoneID))
            {
                var roots=new HashSet<GameObject>();foreach(var spec in FellingSceneDefinition.Load().layers)
                {var owner=FellingSceneRuntime.FindOwner(f.Zone,spec.id);Assert.NotNull(owner);Assert.IsTrue(f.Find(owner,out var root,out string id));Assert.AreEqual(f.Library.Definition.FindFellingOwner(spec.id).modelId,id);Assert.IsTrue(roots.Add(root),spec.id);}
                Assert.AreEqual(55,roots.Count);Assert.AreEqual(7,f.Zone.GetReadOnlyEntities().Count(e=>e.BlueprintName=="FellingBarePosition"||e.BlueprintName=="SeventhPosition"));
            }
        }
        [Test] public void NativeFellingClearRemovesOnlyOneOwnerViewAndPreservesOtherOwnerAliases()
        {
            using(var f=new SpawnRing3DIntegrationFixture(FellingSiteBuilder.ZoneID))
            {
                var spec=FellingSceneDefinition.Load().layers.First(l=>l.mutable);var owner=FellingSceneRuntime.FindOwner(f.Zone,spec.id);f.Approach(owner);f.Refresh();
                var root=f.View(owner);var others=FellingSceneDefinition.Load().layers.Where(l=>l.id!=spec.id).Select(l=>FellingSceneRuntime.FindOwner(f.Zone,l.id)).ToArray();var roots=others.Select(f.View).ToArray();
                Assert.IsTrue(owner.GetPart<FellingScenePropPart>().TryClear(f.Player,f.Zone));Assert.IsTrue(FellingSceneRuntime.GetState(f.Zone).WasRemoved(spec.id));
                f.Refresh(SpawnRing3DIntegrationFixture.Dirty(spec.anchorX,spec.anchorY));Assert.IsFalse(f.Find(owner,out _,out _));SpawnRing3DIntegrationFixture.Hidden(root);
                for(int i=0;i<others.Length;i++)Assert.AreSame(roots[i],f.View(others[i]));Assert.AreEqual(54,others.Length);
            }
        }
        [Test] public void FixedFellingClearRefusalRetainsExactViewAndNativeLandmarks()
        {
            using(var f=new SpawnRing3DIntegrationFixture(FellingSiteBuilder.ZoneID))
            {
                var spec=FellingSceneDefinition.Load().layers.First(l=>!l.mutable);var owner=FellingSceneRuntime.FindOwner(f.Zone,spec.id);var root=f.View(owner);
                var markers=f.Zone.GetReadOnlyEntities().Where(e=>e.BlueprintName=="FellingBarePosition"||e.BlueprintName=="SeventhPosition").ToArray();
                Assert.IsFalse(owner.GetPart<FellingScenePropPart>().TryClear(f.Player,f.Zone));f.Refresh();Assert.AreSame(root,f.View(owner));Assert.AreSame(owner,FellingSceneRuntime.FindOwner(f.Zone,spec.id));
                CollectionAssert.AreEquivalent(markers,f.Zone.GetReadOnlyEntities().Where(e=>e.BlueprintName=="FellingBarePosition"||e.BlueprintName=="SeventhPosition"));
            }
        }
        [Test] public void FullFellingReloadPreservesClearedAbsenceAndRebindsFreshSurvivingAliases()
        {
            using(var f=new SpawnRing3DIntegrationFixture(FellingSiteBuilder.ZoneID))
            {
                var specs=FellingSceneDefinition.Load().layers;var gone=specs.First(l=>l.mutable);var keep=specs.First(l=>l.id!=gone.id);
                var owner=FellingSceneRuntime.FindOwner(f.Zone,gone.id);var survivor=FellingSceneRuntime.FindOwner(f.Zone,keep.id);var oldRoot=f.View(survivor);f.Approach(owner);
                Assert.IsTrue(owner.GetPart<FellingScenePropPart>().TryClear(f.Player,f.Zone));var loaded=f.RoundTrip();f.BindLoaded(loaded);
                Assert.IsNull(FellingSceneRuntime.FindOwner(f.Zone,gone.id));Assert.IsFalse(f.Find(owner,out _,out _));Assert.IsFalse(f.Find(survivor,out _,out _));SpawnRing3DIntegrationFixture.Hidden(oldRoot);
                var current=FellingSceneRuntime.FindOwner(f.Zone,keep.id);Assert.AreNotSame(survivor,current);Assert.AreEqual(survivor.ID,current.ID);Assert.NotNull(f.View(current));
            }
        }
        [Test] public void ActualBipedEquipUnequipChangesTheAttachmentWithoutRebuildingGround()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                f.CleanGear(f.Player);f.Refresh();int builds=f.Get<int>("GroundBuildCount");var item=f.Equip(f.Player,"Dagger");f.Frame();
                Assert.IsTrue(f.Equipment(f.Player,item,out var view));Assert.IsTrue(SpawnRing3DIntegrationFixture.Drawn(view));Assert.IsTrue(view.transform.parent.name.StartsWith("Equipment.Hand.",StringComparison.Ordinal));
                Assert.AreSame(f.Player,item.GetPart<PhysicsPart>().Equipped);Assert.AreEqual(builds,f.Get<int>("GroundBuildCount"));
                Assert.IsTrue(InventorySystem.UnequipItem(f.Player,item));f.Frame();Assert.IsFalse(f.Equipment(f.Player,item,out _));SpawnRing3DIntegrationFixture.Hidden(view);Assert.AreSame(f.Player,item.GetPart<PhysicsPart>().InInventory);
            }
        }
        [TestCase(SpawnRing3DIntegrationFixture.Grove,true)]
        [TestCase(SpawnRing3DIntegrationFixture.Fen,true)]
        [TestCase("Overworld.4.6.0",true)]
        [TestCase("Overworld.3.5.0",false)]
        public void TwoHandedItemMakesOneRealSharedBladeAttachment(string zoneId,bool voxelExpected)
        {
            using(var f=new SpawnRing3DIntegrationFixture(zoneId))
            {
                Assert.AreEqual(voxelExpected,VoxelWorldPresentation.IsEnabledFor(f.Zone),"Composed Ginmere and Olderdeep retain the shared native equipment contract.");
                f.CleanGear(f.Player);var item=f.Equip(f.Player,"Greatsword");f.Refresh();Assert.AreEqual(2,f.Player.GetPart<InventoryPart>().EquippedItems.Values.Count(v=>ReferenceEquals(v,item)));
                Assert.IsTrue(f.Equipment(f.Player,item,out var view));var model=f.Library.FindEquipmentModel("equipment-blade");Assert.NotNull(model);
                var meshes=model.GetComponentsInChildren<MeshFilter>(true).Select(m=>m.sharedMesh).ToArray();Assert.IsNotEmpty(meshes);
                if(VoxelWorldPresentation.IsEnabledFor(f.Zone))
                {
                    var catalog=Resources.Load<VoxelWorldMeshCatalog>(VoxelWorldMeshCatalog.ResourcePath);Assert.NotNull(catalog);catalog.Validate();
                    meshes=meshes.Select(catalog.Resolve).ToArray();
                }
                CollectionAssert.AreEquivalent(meshes,view.GetComponentsInChildren<MeshFilter>(true).Select(m=>m.sharedMesh));
                Assert.AreEqual(meshes.Length,f.View(f.Player).GetComponentsInChildren<MeshFilter>(true).Count(m=>meshes.Contains(m.sharedMesh)),"Two native slots must not duplicate blade geometry.");
            }
        }
        [Test] public void NonHumanoidActualNativeGearGetsOneFallbackWithoutInventedHandsOrUnequip()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                // This authored tortoise inherits native compatible body slots. Art must
                // report the species/socket mismatch rather than silently changing anatomy.
                var actor=f.Add("YellowfootWayfarer");f.CleanGear(actor);var item=f.Equip(actor,"Dagger");f.Refresh();
                Assert.IsFalse(f.Equipment(actor,item,out _));Assert.AreSame(actor,item.GetPart<PhysicsPart>().Equipped);
                var rows=f.Get<IReadOnlyList<Village3DEquipmentFallback>>("EquipmentFallbacks").Where(r=>ReferenceEquals(r.Actor,actor)&&ReferenceEquals(r.Item,item)).ToArray();
                Assert.AreEqual(1,rows.Length);Assert.IsNotEmpty(rows[0].Reason);Assert.IsFalse(f.View(actor).GetComponentsInChildren<Transform>(true).Any(t=>t.name.StartsWith("Equipment.",StringComparison.Ordinal)));
                f.Refresh(new HashSet<int>());Assert.AreEqual(1,f.Get<IReadOnlyList<Village3DEquipmentFallback>>("EquipmentFallbacks").Count(r=>ReferenceEquals(r.Actor,actor)&&ReferenceEquals(r.Item,item)));
            }
        }
        [Test] public void ReloadedEquippedPlayerUsesLoadedItemRefsAndDisposesOldAttachments()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                f.CleanGear(f.Player);var actor=f.Player;var item=f.Equip(actor,"Dagger");f.Refresh();Assert.IsTrue(f.Equipment(actor,item,out var old));
                var loaded=f.RoundTrip();f.BindLoaded(loaded);var current=f.Player.GetPart<InventoryPart>().GetAllEquipped().Single(e=>e.ID==item.ID);
                Assert.AreNotSame(actor,f.Player);Assert.AreNotSame(item,current);Assert.IsFalse(f.Equipment(actor,item,out _));SpawnRing3DIntegrationFixture.Hidden(old);
                Assert.IsFalse(f.Equipment(f.Player,item,out _));Assert.IsTrue(f.Equipment(f.Player,current,out var view));Assert.IsTrue(SpawnRing3DIntegrationFixture.Drawn(view));
            }
        }
        [Test] public void UnknownDroppedItemRemainsInNativeStateAndIsNeverClaimedAsRingArt()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var item=f.Factory.CreateEntity("HealingTonic");Assert.NotNull(item);Assert.IsTrue(f.Player.GetPart<InventoryPart>().AddObject(item));
                Assert.IsTrue(InventorySystem.Drop(f.Player,item,f.Zone));f.Refresh();Assert.NotNull(f.Zone.GetEntityCell(item));Assert.IsFalse(f.Authored(item));Assert.IsFalse(f.Rendered(item));Assert.IsFalse(f.Find(item,out _,out _));
                Assert.IsTrue(InventorySystem.Pickup(f.Player,item,f.Zone));Assert.AreSame(f.Player,item.GetPart<PhysicsPart>().InInventory);Assert.IsNull(f.Zone.GetEntityCell(item));
            }
        }
        [Test] public void NativePickingStopsWithHiddenPresentationAndRejectsRemovedActor()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var actor=f.Add("CaveHermit");f.Refresh();Assert.IsTrue(f.Pick(actor,out var point),"Actual imported humanoid must be pickable before the negative control.");
                f.Call("SetPresentationVisible",false);object[] args={point,null,0,0};Assert.IsFalse((bool)f.Call("TryPickWorld",args));Assert.IsFalse((bool)f.Call("ClaimsCell",40,12));
                f.Call("SetPresentationVisible",true);f.Refresh();Assert.IsTrue(f.Pick(actor,out point));Assert.IsTrue(f.Zone.RemoveEntity(actor));f.Refresh();args=new object[]{point,null,0,0};
                if((bool)f.Call("TryPickWorld",args))Assert.AreNotSame(actor,args[1]);Assert.IsFalse(f.Authored(actor));
            }
        }
        [Test] public void LowDetailAndModeTogglesKeepNativeOwnersAndRecoverRealCameraReadiness()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var root=f.View(f.Player);var camera=f.Get<Camera>("WorldCamera");var old=camera.targetTexture;var point=f.Zone.GetEntityPosition(f.Player);int version=f.Zone.EntityVersion;
                Village3DSettings.LowDetail=true;f.Frame();Assert.IsTrue(f.Get<bool>("PresentationVisible"));Assert.AreNotSame(old,camera.targetTexture);Assert.IsTrue(f.Rendered(f.Player));Assert.AreSame(root,f.View(f.Player));
                Assert.AreEqual(Mathf.Max(1,Mathf.RoundToInt(f.Source.pixelRect.width*.75f)),camera.targetTexture.width);
                Village3DSettings.Enabled=false;f.Frame();Assert.IsFalse(f.Get<bool>("PresentationVisible"));Assert.IsFalse(f.Rendered(f.Player));Assert.IsFalse((bool)f.Call("ClaimsCell",point.x,point.y));
                Village3DSettings.Enabled=true;Village3DSettings.LowDetail=false;f.Frame();Assert.IsTrue(f.Get<bool>("PresentationVisible"));Assert.AreSame(root,f.View(f.Player));
                f.Source.orthographic=false;f.Frame();Assert.IsFalse(f.Get<bool>("PresentationVisible"));Assert.IsFalse((bool)f.Call("ClaimsCell",point.x,point.y));
                f.Source.orthographic=true;f.Frame();Assert.IsTrue(f.Get<bool>("PresentationVisible"));Assert.AreEqual(point,f.Zone.GetEntityPosition(f.Player));Assert.AreEqual(version,f.Zone.EntityVersion);Assert.AreSame(f.Borrowed,f.Source.targetTexture);
            }
        }
        [Test] public void RebindingAnUnrelatedZoneDropsClaimsRootsAndOnlyOwnedCameraResources()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var root=f.View(f.Player);var camera=f.Get<Camera>("WorldCamera");var target=camera.targetTexture;f.Bind(new Zone("Overworld.3.6.0"));
                Assert.IsFalse(f.Get<bool>("PresentationVisible"));Assert.IsFalse((bool)f.Call("ClaimsCell",40,12));Assert.IsFalse(f.Find(f.Player,out _,out _));SpawnRing3DIntegrationFixture.Hidden(root);
                Assert.IsTrue(camera==null||!camera.enabled);Assert.IsTrue(target==null||!target.IsCreated()||camera==null||!camera.enabled);Assert.IsTrue(f.Source!=null);Assert.AreSame(f.Borrowed,f.Source.targetTexture);Assert.IsTrue(f.Borrowed.IsCreated());
            }
        }
    }
}
