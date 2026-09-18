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
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    internal sealed class Village3DIntegrationFixture : IDisposable
    {
        const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        readonly EntityEquipmentContentFixture scope = new EntityEquipmentContentFixture();
        readonly List<(MemberInfo member, object value)> settings = new List<(MemberInfo, object)>();
        Camera ownedWorldCamera;
        public readonly GameObject Root;
        public readonly Camera Source;
        public readonly RenderTexture BorrowedTarget;
        public readonly Village3DPresenter Presenter;
        public readonly EntityFactory Factory;
        public readonly OverworldZoneManager Manager;
        public Zone Zone;
        public readonly Entity Player;
        public LightMap Light = new LightMap();

        public Village3DIntegrationFixture()
        {
            try
            {
                // EquipmentContent owns factory/RNG/loot registry state; its nested scopes restore
                // save root, prefs, Active, message/reputation, pooled requests and render hooks.
                Factory = scope.Factory;
                SetStaticSetting("Enabled", true, required: true);
                SetStaticSetting("LowDetail", false, required: false);
                // Optional naming adaptation: normal gameplay visibility is mandatory here.
                SetStaticSetting("FullReveal", false, required: false);
                SetStaticSetting("Showcase", false, required: false);
                Root = new GameObject("Village 3D integration owned fixture");
                var cameraObject = new GameObject("Village 3D source camera");
                cameraObject.transform.SetParent(Root.transform, false);
                Source = cameraObject.AddComponent<Camera>();
                Source.enabled = false; Source.orthographic = true; Source.orthographicSize = 12.75f;
                Source.transform.position = new Vector3(40, 12.75f, -10);
                BorrowedTarget = new RenderTexture(960, 640, 16) { name = " borrowed source target" };
                Source.targetTexture = BorrowedTarget; Source.rect = new Rect(.05f, .10f, .75f, .80f);
                Source.aspect = Source.pixelRect.width / Source.pixelRect.height;
                Source.cullingMask = (1 << 8) | 1;
                Manager = new OverworldZoneManager(Factory, 64);
                Zone = Manager.GetZone(MorrowfastSceneRuntime.ZoneID);
                Assert.NotNull(Zone); Manager.SetActiveZone(Zone);
                Player = MorrowfastTestWorld.Actor(Factory, Zone);
                RevealAll();
                Presenter = Root.AddComponent<Village3DPresenter>();
                Presenter.Bind(Zone, Source); Refresh();
                ownedWorldCamera = Presenter.WorldCamera;
                Assert.IsTrue(Presenter.IsReady, Presenter.Failure);
                Assert.IsTrue(Presenter.PresentationVisible, "Normal ready fixture must actually be shown.");
            }
            catch { Dispose(); throw; }
        }
        void SetStaticSetting(string name, object value, bool required)
        {
            var type = typeof(Village3DPresenter).Assembly.GetType("CavesOfOoo.Rendering.Village3DSettings");
            Assert.NotNull(type);
            var p = type.GetProperty(name, Static);
            if (p != null && p.CanWrite) { settings.Add((p, p.GetValue(null))); p.SetValue(null, value); return; }
            var field = type.GetField(name, Static);
            if (field != null) { settings.Add((field, field.GetValue(null))); field.SetValue(null, value); return; }
            if (required) Assert.Fail("Required setting missing: " + name);
        }
        public void RevealAll()
        { for (int y=0;y<25;y++) for (int x=0;x<80;x++) Zone.GetCell(x,y).Explored=Zone.GetCell(x,y).IsVisible=true; }
        public void Refresh()
        { Light.Compute(Zone); Presenter.Refresh(Light); TickFrame(Presenter); }
        // Presenter may synchronize camera in LateUpdate or Update. Adjust only this adapter if
        // root chooses a named synchronization method; this is not a real engine-lifetime claim.
        public static void TickFrame(Village3DPresenter p)
        {
            var m=typeof(Village3DPresenter).GetMethod("LateUpdate",Instance)
                ??typeof(Village3DPresenter).GetMethod("Update",Instance);
            if(m!=null)m.Invoke(p,null);
        }
        public (Entity owner, GameObject root) View(string id)
        {
            Assert.IsTrue(Presenter.TryGetOwnerView(id,out var owner,out var root),"Catalogue must bind " + id);
            Assert.NotNull(owner,id); Assert.NotNull(root,id);
            Assert.AreSame(MorrowfastSceneRuntime.FindOwner(Zone,id),owner,id);
            Assert.Greater(root.GetComponentsInChildren<Renderer>(true).Length,0,"No renderer under owner " + id);
            return(owner,root);
        }
        public static bool Drawn(GameObject root) => root != null && root.GetComponentsInChildren<Renderer>(true)
            .Any(r=>r.enabled&&r.gameObject.activeInHierarchy&&!r.forceRenderingOff);
        public static void Hidden(GameObject root)
        {
            if(root==null)return;
            foreach(var r in root.GetComponentsInChildren<Renderer>(true))
                Assert.IsFalse(r.enabled&&r.gameObject.activeInHierarchy&&!r.forceRenderingOff,"Hidden owner still submits a renderer: "+r.name);
            foreach(var c in root.GetComponentsInChildren<Collider>(true))
                Assert.IsFalse(c.enabled&&c.gameObject.activeInHierarchy,"Hidden owner still selectable: "+c.name);
            // Disabling a renderer/GameObject (or forceRenderingOff) prevents submission.
            // Merely setting material alpha to zero is deliberately insufficient here.
        }
        public bool PickOwner(string id,out Vector2 point)
        {
            var v=View(id); Physics.SyncTransforms();
            // Search a rendered footprint; an anchor is NOT guaranteed to be inside a mesh.
            foreach(var r in v.root.GetComponentsInChildren<Renderer>(true))
            {
                if(!r.enabled||!r.gameObject.activeInHierarchy)continue;
                var b=r.bounds;
                for(int ix=0;ix<9;ix++)for(int iz=0;iz<9;iz++)
                {
                    var p=new Vector2(Mathf.Lerp(b.min.x,b.max.x,(ix+.5f)/9),Mathf.Lerp(b.min.z,b.max.z,(iz+.5f)/9));
                    if(Presenter.TryPickWorld(p,out var owner,out _,out _)&&ReferenceEquals(owner,v.owner)){point=p;return true;}
                }
            }
            point=default;return false;
        }
        public void Approach(Entity target)
        {var at=MorrowfastTestWorld.Approach(Zone,Player,target);Assert.IsTrue(Zone.MoveEntity(Player,at.x,at.y));}
        public MorrowfastSceneDefinition.BuildingSpec FirstRoom=>MorrowfastSceneDefinition.Load().buildings[0];
        public void Inside(MorrowfastSceneDefinition.BuildingSpec room)
        {
            var point=room.interior.First(p=>!Zone.GetCell(p.x,p.y).BlocksMovement(Player));
            Assert.IsTrue(Zone.MoveEntity(Player,point.x,point.y));
            Assert.AreEqual(room.id,MorrowfastSceneRuntime.GetRoomAt(Zone,point.x,point.y));
        }
        public void Outside()=>Assert.IsTrue(Zone.MoveEntity(Player,40,24));
        public GameSessionState RoundTrip()
        {
            Manager.SetActiveZone(Zone);
            var turns=new TurnManager();turns.RestoreSavedState(17,true,Player,new List<TurnManager.SavedTurnEntry>{new TurnManager.SavedTurnEntry{Entity=Player,Energy=1000}});
            var state=GameSessionState.Capture("village3d-draft", "village3d-owned",Manager,turns,Player);
            using(var stream=new MemoryStream()) {state.Save(new SaveWriter(stream));stream.Position=0;return GameSessionState.Load(new SaveReader(stream,Factory));}
        }
        public void BindLoaded(GameSessionState loaded)
        {Zone=loaded.ZoneManager.ActiveZone;Assert.NotNull(Zone);Light=new LightMap();Presenter.Bind(Zone,Source);Refresh();}
        public static string Pose(GameObject root)=>string.Join("|",root.GetComponentsInChildren<Transform>(true)
            .Select(t=>t.name+":"+t.localPosition+":"+t.localRotation+":"+t.localScale+":"+t.gameObject.activeSelf));
        public void Dispose()
        {
            if(Root!=null)Object.DestroyImmediate(Root);
            // Clean only the exact owned camera if a failed lifecycle assertion exposed a leak.
            if(ownedWorldCamera!=null)Object.DestroyImmediate(ownedWorldCamera.gameObject);
            if(BorrowedTarget!=null){BorrowedTarget.Release();Object.DestroyImmediate(BorrowedTarget);}
            for(int i=settings.Count-1;i>=0;i--)
            {var s=settings[i];if(s.member is PropertyInfo p)p.SetValue(null,s.value);else ((FieldInfo)s.member).SetValue(null,s.value);}
            scope.Dispose();
        }
    }

    public sealed class Village3DIntegrationTests
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static void AssertNear(Vector3 expected,Vector3 actual,string label)
        {Assert.Less((expected-actual).magnitude,.001f,label);}

        // A — ownership, idempotence, mutation-free presentation.
        [Test] public void AllAuthoredOwnersBindExistingEntitiesAndIndependentViewRoots()
        {
            using(var f=new Village3DIntegrationFixture())
            {var roots=new HashSet<GameObject>();var refs=new HashSet<Entity>();foreach(var spec in MorrowfastSceneDefinition.Load().owners){var v=f.View(spec.id);Assert.IsTrue(roots.Add(v.root),spec.id);Assert.IsTrue(refs.Add(v.owner),spec.id);}Assert.AreEqual(71,roots.Count);}
        }
        [Test] public void SameZoneBindRetainsOwnersCameraAndRenderTarget()
        {
            using(var f=new Village3DIntegrationFixture())
            {var v=f.View("central-cistern");var camera=f.Presenter.WorldCamera;var rt=camera.targetTexture;f.Presenter.Bind(f.Zone,f.Source);f.Refresh();Assert.AreSame(v.owner,f.View("central-cistern").owner);Assert.AreSame(v.root,f.View("central-cistern").root);Assert.AreSame(camera,f.Presenter.WorldCamera);Assert.AreSame(rt,camera.targetTexture);}
        }
        [Test] public void RepeatedRefreshDoesNotMutateNativeMembershipPositionsDoorsRoofsOrFog()
        {
            using(var f=new Village3DIntegrationFixture())
            {var members=f.Zone.GetReadOnlyEntities().ToArray();var positions=members.Select(f.Zone.GetEntityPosition).ToArray();var state=MorrowfastSceneRuntime.GetState(f.Zone);string open=state.OpenDoorIds,lift=state.LiftedRoofIds,removed=state.RemovedIds;int version=f.Zone.EntityVersion;for(int i=0;i<5;i++)f.Refresh();CollectionAssert.AreEquivalent(members,f.Zone.GetReadOnlyEntities());CollectionAssert.AreEqual(positions,members.Select(f.Zone.GetEntityPosition));Assert.AreEqual(version,f.Zone.EntityVersion);Assert.AreEqual(open,state.OpenDoorIds);Assert.AreEqual(lift,state.LiftedRoofIds);Assert.AreEqual(removed,state.RemovedIds);for(int y=0;y<25;y++)for(int x=0;x<80;x++){Assert.IsTrue(f.Zone.GetCell(x,y).Explored);Assert.IsTrue(f.Zone.GetCell(x,y).IsVisible);}}
        }
        [Test] public void ForeignZoneReleasesClaimsAndPriorOwners()
        {
            using(var f=new Village3DIntegrationFixture())
            {var v=f.View("central-cistern");f.Presenter.Bind(new Zone("ordinary"),f.Source);Assert.IsFalse(f.Presenter.PresentationVisible);Assert.IsFalse(f.Presenter.ClaimsCell(40,12));Assert.IsFalse(f.Presenter.IsRenderedEntity(v.owner));Village3DIntegrationFixture.Hidden(v.root);}
        }
        [Test] public void NullZoneSafelyFallsBackWithoutDestroyingSourceCamera()
        {
            using(var f=new Village3DIntegrationFixture())
            {f.Presenter.Bind(null,f.Source);Assert.IsFalse(f.Presenter.PresentationVisible);Assert.IsFalse(f.Presenter.ClaimsCell(40,12));Assert.IsFalse(f.Presenter.TryPickWorld(new Vector2(40,12),out _,out _,out _));Assert.NotNull(f.Source);Assert.AreSame(f.BorrowedTarget,f.Source.targetTexture);}
        }
        [Test] public void HiddenModeDisablesRenderingSelectionAndWorldCameraTogether()
        {
            using(var f=new Village3DIntegrationFixture())
            {var v=f.View("north-guard-west");Assert.IsTrue(f.Presenter.IsRenderedEntity(v.owner));Assert.IsTrue(f.PickOwner("north-guard-west",out var p));f.Presenter.SetPresentationVisible(false);Assert.IsFalse(f.Presenter.PresentationVisible);Assert.IsFalse(f.Presenter.ClaimsCell(40,12));Assert.IsFalse(f.Presenter.IsRenderedEntity(v.owner));Assert.IsFalse(f.Presenter.TryPickWorld(p,out _,out _,out _));Village3DIntegrationFixture.Hidden(v.root);Assert.IsTrue(f.Presenter.WorldCamera==null||!f.Presenter.WorldCamera.enabled||!f.Presenter.WorldCamera.gameObject.activeInHierarchy);}
        }
        [Test] public void RepeatedHideShowRestoresSameVisibleOwnerWithoutDuplicates()
        {
            using(var f=new Village3DIntegrationFixture())
            {var v=f.View("north-guard-west");for(int i=0;i<3;i++){f.Presenter.SetPresentationVisible(false);f.Presenter.SetPresentationVisible(true);f.Refresh();Assert.AreSame(v.root,f.View("north-guard-west").root);Assert.AreSame(v.owner,f.View("north-guard-west").owner);Assert.IsTrue(f.Presenter.IsRenderedEntity(v.owner));Assert.IsTrue(Village3DIntegrationFixture.Drawn(v.root));}}
        }
        [Test] public void NewZoneGraphWithSameIDCannotReuseOldOwnerReferences()
        {
            using(var f=new Village3DIntegrationFixture())
            {var old=f.View("central-cistern");var replacement=new OverworldZoneManager(f.Factory,64).GetZone(MorrowfastSceneRuntime.ZoneID);for(int y=0;y<25;y++)for(int x=0;x<80;x++)replacement.GetCell(x,y).Explored=replacement.GetCell(x,y).IsVisible=true;f.Zone=replacement;f.Presenter.Bind(replacement,f.Source);f.Refresh();var next=f.View("central-cistern");Assert.AreNotSame(old.owner,next.owner);Assert.AreEqual(old.owner.ID,next.owner.ID);Assert.IsFalse(f.Presenter.IsRenderedEntity(old.owner));Village3DIntegrationFixture.Hidden(old.root);}
        }
        [Test] public void RealOwnerRelocationMovesViewByNativeCellDelta()
        {
            using(var f=new Village3DIntegrationFixture())
            {var v=f.View("north-guard-west");var at=f.Zone.GetEntityPosition(v.owner);var before=v.root.transform.position;Assert.IsTrue(f.Zone.MoveEntity(v.owner,40,23));f.Refresh();Assert.AreSame(v.owner,f.View("north-guard-west").owner);AssertNear(before+new Vector3(40-at.x,0,at.y-23),v.root.transform.position,"Authored pivot must follow native displacement");Assert.AreEqual((40,23),f.Zone.GetEntityPosition(v.owner));}
        }
        [Test] public void SameBlueprintOrdinaryActorIsNotClaimedAsAnAuthoredOwner()
        {
            using(var f=new Village3DIntegrationFixture())
            {var source=f.View("western-bank-frog").owner;var other=f.Factory.CreateEntity(source.BlueprintName);Assert.NotNull(other);Assert.IsTrue(f.Zone.AddEntity(other,40,23));f.Refresh();Assert.AreNotSame(source,other);Assert.IsFalse(f.Presenter.IsRenderedEntity(other),"Unmapped actor must stay eligible for the ordinary fallback.");Assert.AreSame(source,f.View("western-bank-frog").owner);}
        }
        [Test] public void MappedActorCanMoveBeyondOldArtXBoundsWithoutLosingItsRepresentation()
        {
            using(var f=new Village3DIntegrationFixture())
            {var v=f.View("north-guard-west");Assert.IsTrue(f.Zone.MoveEntity(v.owner,79,12));f.Refresh();Assert.IsTrue(f.Presenter.IsRenderedEntity(v.owner));Assert.IsTrue(Village3DIntegrationFixture.Drawn(v.root));Assert.AreEqual((79,12),f.Zone.GetEntityPosition(v.owner));}
        }
        [Test] public void RemovingLiveOwnerStopsViewAndSelectionWithoutRemovingAnotherOwner()
        {
            using(var f=new Village3DIntegrationFixture())
            {var gone=f.View("north-guard-west");var alive=f.View("north-guard-east");Assert.IsTrue(f.PickOwner("north-guard-west",out var p));Assert.IsTrue(f.Zone.RemoveEntity(gone.owner));f.Refresh();Assert.IsFalse(f.Presenter.IsRenderedEntity(gone.owner));Village3DIntegrationFixture.Hidden(gone.root);if(f.Presenter.TryPickWorld(p,out var hit,out _,out _))Assert.AreNotSame(gone.owner,hit);Assert.IsTrue(f.Presenter.IsRenderedEntity(alive.owner));Assert.IsTrue(Village3DIntegrationFixture.Drawn(alive.root));}
        }
        [Test] public void SameOwnerTemporarilyRemovedThenReaddedRebindsWithoutNewEntity()
        {
            using(var f=new Village3DIntegrationFixture())
            {var v=f.View("north-guard-west");var at=f.Zone.GetEntityPosition(v.owner);Assert.IsTrue(f.Zone.RemoveEntity(v.owner));f.Refresh();Assert.IsTrue(f.Zone.AddEntity(v.owner,at.x,at.y));f.Refresh();Assert.AreSame(v.owner,f.View("north-guard-west").owner);Assert.IsTrue(f.Presenter.IsRenderedEntity(v.owner));}
        }

        // B — actual mutable owner and room APIs, with positive/refusal controls.
        [Test] public void NativeClearRemovesExactOwnerAndEntireVisualContribution()
        {
            using(var f=new Village3DIntegrationFixture())
            {var owner=MorrowfastTestWorld.Clearable(f.Zone,f.Player);string id=owner.GetPart<MorrowfastPropPart>().ComponentId;var view=f.View(id);Assert.IsTrue(owner.GetPart<MorrowfastPropPart>().TryRemove(f.Player,f.Zone));f.Refresh();Assert.IsTrue(MorrowfastSceneRuntime.GetState(f.Zone).WasRemoved(id));Assert.IsNull(MorrowfastSceneRuntime.FindOwner(f.Zone,id));Village3DIntegrationFixture.Hidden(view.root);Assert.IsFalse(f.Presenter.IsRenderedEntity(owner));}
        }
        [Test] public void NonemptyNativeContainerClearRefusalLeavesItsContentsAndView()
        {
            using(var f=new Village3DIntegrationFixture())
            {var spec=MorrowfastSceneDefinition.Load().owners.First(s=>s.mutable&&s.kind=="container"&&MorrowfastSceneRuntime.FindOwner(f.Zone,s.id).GetPart<ContainerPart>()?.Contents.Count>0);var v=f.View(spec.id);f.Approach(v.owner);var contents=v.owner.GetPart<ContainerPart>().Contents.ToArray();Assert.IsFalse(v.owner.GetPart<MorrowfastPropPart>().TryRemove(f.Player,f.Zone));f.Refresh();Assert.AreSame(v.owner,f.View(spec.id).owner);CollectionAssert.AreEqual(contents,v.owner.GetPart<ContainerPart>().Contents);Assert.IsFalse(MorrowfastSceneRuntime.GetState(f.Zone).WasRemoved(spec.id));}
        }
        [Test] public void NativeDoorOpenChangesDoorViewWithoutChangingOwnerIdentity()
        {
            using(var f=new Village3DIntegrationFixture())
            {var room=f.FirstRoom;var v=f.View(room.doorId);string before=Village3DIntegrationFixture.Pose(v.root);f.Approach(v.owner);Assert.IsTrue(v.owner.GetPart<MorrowfastDoorPart>().TrySetOpen(f.Player,f.Zone,true));f.Refresh();Assert.IsTrue(MorrowfastSceneRuntime.IsDoorOpen(f.Zone,room.doorId));Assert.AreSame(v.owner,f.View(room.doorId).owner);Assert.IsTrue(!Village3DIntegrationFixture.Drawn(v.root)||before!=Village3DIntegrationFixture.Pose(v.root),"Opening must either hide or animate the real door geometry.");}
        }
        [Test] public void OccupiedDoorCloseRefusalKeepsOpenNativeAndVisualState()
        {
            using(var f=new Village3DIntegrationFixture())
            {var room=f.FirstRoom;var v=f.View(room.doorId);f.Approach(v.owner);var door=v.owner.GetPart<MorrowfastDoorPart>();Assert.IsTrue(door.TrySetOpen(f.Player,f.Zone,true));var foot=MorrowfastSceneDefinition.Load().FindOwner(room.doorId).footprint.First();Assert.IsTrue(f.Zone.MoveEntity(f.Player,foot.x,foot.y));f.Refresh();string pose=Village3DIntegrationFixture.Pose(v.root);Assert.IsFalse(door.TrySetOpen(f.Player,f.Zone,false));f.Refresh();Assert.IsTrue(MorrowfastSceneRuntime.IsDoorOpen(f.Zone,room.doorId));Assert.AreEqual(pose,Village3DIntegrationFixture.Pose(v.root));}
        }
        [Test] public void EnteringOneRoomHidesOnlyItsRoofWithoutPersistingALift()
        {
            using(var f=new Village3DIntegrationFixture())
            {var room=f.FirstRoom;var roofs=MorrowfastSceneDefinition.Load().buildings.ToDictionary(b=>b.id,b=>f.View(b.roofId));string lifts=MorrowfastSceneRuntime.GetState(f.Zone).LiftedRoofIds;f.Inside(room);f.Refresh();Village3DIntegrationFixture.Hidden(roofs[room.id].root);foreach(var pair in roofs)if(pair.Key!=room.id)Assert.IsTrue(Village3DIntegrationFixture.Drawn(pair.Value.root),pair.Key);Assert.AreEqual(lifts,MorrowfastSceneRuntime.GetState(f.Zone).LiftedRoofIds);Assert.IsFalse(MorrowfastSceneRuntime.IsRoofLifted(f.Zone,room.roofId));}
        }
        [Test] public void LeavingUnliftedRoomRestoresRoofWithoutChangingCollision()
        {
            using(var f=new Village3DIntegrationFixture())
            {var room=f.FirstRoom;var roof=f.View(room.roofId);var cells=MorrowfastSceneDefinition.Load().FindOwner(room.doorId).footprint;var blocked=cells.Select(p=>f.Zone.GetCell(p.x,p.y).BlocksMovement(f.Player)).ToArray();f.Inside(room);f.Refresh();Village3DIntegrationFixture.Hidden(roof.root);f.Outside();f.Refresh();Assert.IsTrue(Village3DIntegrationFixture.Drawn(roof.root));CollectionAssert.AreEqual(blocked,cells.Select(p=>f.Zone.GetCell(p.x,p.y).BlocksMovement(f.Player)));}
        }
        [Test] public void DoorOpenOutsideDoesNotPretendPlayerEnteredTheRoom()
        {
            using(var f=new Village3DIntegrationFixture())
            {var room=f.FirstRoom;var door=f.View(room.doorId);f.Approach(door.owner);Assert.IsTrue(door.owner.GetPart<MorrowfastDoorPart>().TrySetOpen(f.Player,f.Zone,true));f.Outside();f.Refresh();Assert.IsTrue(Village3DIntegrationFixture.Drawn(f.View(room.roofId).root));Assert.IsFalse(MorrowfastSceneRuntime.IsRoofLifted(f.Zone,room.roofId));}
        }
        [Test] public void ValidExplicitLiftPersistsAfterPlayerLeavesRoom()
        {
            using(var f=new Village3DIntegrationFixture())
            {var room=f.FirstRoom;var door=f.View(room.doorId);f.Approach(door.owner);Assert.IsTrue(door.owner.GetPart<MorrowfastDoorPart>().TrySetOpen(f.Player,f.Zone,true));var roof=f.View(room.roofId);f.Approach(roof.owner);Assert.IsTrue(roof.owner.GetPart<MorrowfastPropPart>().TrySetRoofLifted(f.Player,f.Zone,true));f.Outside();f.Refresh();Assert.IsTrue(MorrowfastSceneRuntime.IsRoofLifted(f.Zone,room.roofId));Village3DIntegrationFixture.Hidden(roof.root);}
        }
        [Test] public void ClosedUnenteredRoofLiftRefusalCannotHideRoof()
        {
            using(var f=new Village3DIntegrationFixture())
            {var room=f.FirstRoom;var roof=f.View(room.roofId);f.Approach(roof.owner);var at=f.Zone.GetEntityPosition(f.Player);Assert.AreNotEqual(room.id,MorrowfastSceneRuntime.GetRoomAt(f.Zone,at.x,at.y));Assert.IsFalse(roof.owner.GetPart<MorrowfastPropPart>().TrySetRoofLifted(f.Player,f.Zone,true));f.Refresh();Assert.IsFalse(MorrowfastSceneRuntime.IsRoofLifted(f.Zone,room.roofId));Assert.IsTrue(Village3DIntegrationFixture.Drawn(roof.root));}
        }

        // C — visible/remembered/unseen and per-instance symmetry.
        [Test] public void VisibleActorIsRenderedAndPickReturnsExactLiveOwnerCell()
        {
            using(var f=new Village3DIntegrationFixture())
            {var v=f.View("north-guard-west");Assert.IsTrue(Village3DIntegrationFixture.Drawn(v.root));Assert.IsTrue(f.PickOwner("north-guard-west",out var p));Assert.IsTrue(f.Presenter.TryPickWorld(p,out var hit,out int x,out int y));Assert.AreSame(v.owner,hit);Assert.AreEqual(f.Zone.GetEntityPosition(v.owner),(x,y));}
        }
        [Test] public void RememberedActorHasNoDrawShadowOrPickLeak()
        {
            using(var f=new Village3DIntegrationFixture())
            {var v=f.View("north-guard-west");Assert.IsTrue(f.PickOwner("north-guard-west",out var p));var cell=f.Zone.GetEntityCell(v.owner);cell.Explored=true;cell.IsVisible=false;f.Refresh();Village3DIntegrationFixture.Hidden(v.root);Assert.IsFalse(f.Presenter.IsRenderedEntity(v.owner));if(f.Presenter.TryPickWorld(p,out var hit,out _,out _))Assert.AreNotSame(v.owner,hit);Assert.IsTrue(cell.Explored);Assert.IsFalse(cell.IsVisible);}
        }
        [Test] public void UnseenActorHasNoDrawShadowOrPickLeakAndDoesNotExploreItsCell()
        {
            using(var f=new Village3DIntegrationFixture())
            {var v=f.View("north-guard-west");Assert.IsTrue(f.PickOwner("north-guard-west",out var p));var cell=f.Zone.GetEntityCell(v.owner);cell.Explored=cell.IsVisible=false;f.Refresh();Village3DIntegrationFixture.Hidden(v.root);Assert.IsFalse(f.Presenter.IsRenderedEntity(v.owner));if(f.Presenter.TryPickWorld(p,out var hit,out _,out _))Assert.AreNotSame(v.owner,hit);Assert.IsFalse(cell.Explored);}
        }
        [Test] public void RenderPartInvisibleActorIsNotLeakedByItsMappedPrefab()
        {
            using(var f=new Village3DIntegrationFixture())
            {var v=f.View("north-guard-west");Assert.IsTrue(f.PickOwner("north-guard-west",out var p));v.owner.GetPart<RenderPart>().Visible=false;f.Refresh();Village3DIntegrationFixture.Hidden(v.root);Assert.IsFalse(f.Presenter.IsRenderedEntity(v.owner));if(f.Presenter.TryPickWorld(p,out var hit,out _,out _))Assert.AreNotSame(v.owner,hit);v.owner.GetPart<RenderPart>().Visible=true;f.Refresh();Assert.IsTrue(Village3DIntegrationFixture.Drawn(v.root));}
        }
        [Test] public void HidingOneGuardDoesNotHideOtherSharedMaterialInstance()
        {
            using(var f=new Village3DIntegrationFixture())
            {var west=f.View("north-guard-west");var east=f.View("north-guard-east");f.Zone.GetEntityCell(west.owner).IsVisible=false;f.Refresh();Village3DIntegrationFixture.Hidden(west.root);Assert.IsTrue(Village3DIntegrationFixture.Drawn(east.root));Assert.IsTrue(f.Presenter.IsRenderedEntity(east.owner));f.Zone.GetEntityCell(west.owner).IsVisible=true;f.Refresh();Assert.IsTrue(Village3DIntegrationFixture.Drawn(west.root));Assert.IsTrue(Village3DIntegrationFixture.Drawn(east.root));}
        }

        // D — camera/RT ownership and frame adapter lifecycle. Real engine lifetime is native.
        [Test] public void IndependentWorldCameraDoesNotBecomeMainOrRenderUIAndCompositeLayers()
        {
            using(var f=new Village3DIntegrationFixture())
            {var world=f.Presenter.WorldCamera;Assert.NotNull(world);Assert.AreNotSame(f.Source,world);Assert.IsTrue(world.orthographic);Assert.IsFalse(world.CompareTag("MainCamera"));Assert.AreEqual(0,world.cullingMask&((1<<8)|(1<<9)|(1<<10)|(1<<11)|1),"3D world must not recursively see 2D composite/UI/default layers.");Assert.Greater(world.cullingMask,0);Assert.AreSame(f.BorrowedTarget,f.Source.targetTexture);}
        }
        [Test] public void UnchangedCameraRefreshReusesRenderTexture()
        {
            using(var f=new Village3DIntegrationFixture())
            {var rt=f.Presenter.WorldCamera.targetTexture;Assert.NotNull(rt);for(int i=0;i<20;i++)f.Refresh();Assert.AreSame(rt,f.Presenter.WorldCamera.targetTexture);Assert.AreSame(f.BorrowedTarget,f.Source.targetTexture);}
        }
        [Test] public void HideShowDoesNotReleaseBorrowedSourceTargetOrChangeItsLayout()
        {
            using(var f=new Village3DIntegrationFixture())
            {Rect rect=f.Source.rect;float size=f.Source.orthographicSize;int mask=f.Source.cullingMask;f.Presenter.SetPresentationVisible(false);f.Presenter.SetPresentationVisible(true);f.Refresh();Assert.AreSame(f.BorrowedTarget,f.Source.targetTexture);Assert.IsTrue(f.BorrowedTarget!=null);Assert.AreEqual(rect,f.Source.rect);Assert.AreEqual(size,f.Source.orthographicSize);Assert.AreEqual(mask,f.Source.cullingMask);}
        }
        [Test] public void ResizingDisplayedMapReplacesOnlyOwnedTargetThenStabilizes()
        {
            using(var f=new Village3DIntegrationFixture())
            {var old=f.Presenter.WorldCamera.targetTexture;Assert.NotNull(old);f.Source.rect=new Rect(.05f,.10f,.50f,.60f);f.Source.aspect=f.Source.pixelRect.width/f.Source.pixelRect.height;f.Refresh();var next=f.Presenter.WorldCamera.targetTexture;Assert.NotNull(next);Assert.AreNotSame(old,next);Assert.IsTrue(old==null||!old.IsCreated(),"Old owned target must be released.");Assert.AreSame(f.BorrowedTarget,f.Source.targetTexture);f.Refresh();Assert.AreSame(next,f.Presenter.WorldCamera.targetTexture);}
        }
        [Test] public void PresenterDestructionReleasesOwnedCameraAndTargetButNotBorrowedCamera()
        {
            using(var f=new Village3DIntegrationFixture())
            {var world=f.Presenter.WorldCamera;var target=world.targetTexture;Object.DestroyImmediate(f.Presenter);Assert.IsTrue(world==null,"Presenter owns world camera lifetime.");Assert.IsTrue(target==null||!target.IsCreated());Assert.NotNull(f.Source);Assert.AreSame(f.BorrowedTarget,f.Source.targetTexture);}
        }
        [Test] public void DestroyedSourceCameraDoesNotThrowAndStopsClaims()
        {
            using(var f=new Village3DIntegrationFixture())
            {Object.DestroyImmediate(f.Source.gameObject);Assert.DoesNotThrow(()=>f.Refresh());Assert.IsFalse(f.Presenter.PresentationVisible);Assert.IsFalse(f.Presenter.ClaimsCell(40,12));}
        }

        // E — production graph load; reference identity changes, semantic state survives.
        [Test] public void ClearedOwnerStaysAbsentAfterFullSaveLoadAndRebind()
        {
            using(var f=new Village3DIntegrationFixture())
            {var owner=MorrowfastTestWorld.Clearable(f.Zone,f.Player);string id=owner.GetPart<MorrowfastPropPart>().ComponentId;Assert.IsTrue(owner.GetPart<MorrowfastPropPart>().TryRemove(f.Player,f.Zone));var loaded=f.RoundTrip();f.BindLoaded(loaded);Assert.IsTrue(MorrowfastSceneRuntime.GetState(f.Zone).WasRemoved(id));Assert.IsNull(MorrowfastSceneRuntime.FindOwner(f.Zone,id));Assert.IsFalse(f.Presenter.IsRenderedEntity(owner));if(f.Presenter.TryGetOwnerView(id,out var rebound,out var root)){Assert.IsNull(rebound);Village3DIntegrationFixture.Hidden(root);}}
        }
        [Test] public void OpenDoorAndExplicitLiftSurviveSaveLoadWithoutOldReferences()
        {
            using(var f=new Village3DIntegrationFixture())
            {var room=f.FirstRoom;var oldDoor=f.View(room.doorId);f.Approach(oldDoor.owner);Assert.IsTrue(oldDoor.owner.GetPart<MorrowfastDoorPart>().TrySetOpen(f.Player,f.Zone,true));var oldRoof=f.View(room.roofId);f.Approach(oldRoof.owner);Assert.IsTrue(oldRoof.owner.GetPart<MorrowfastPropPart>().TrySetRoofLifted(f.Player,f.Zone,true));f.Outside();var loaded=f.RoundTrip();f.BindLoaded(loaded);Assert.IsTrue(MorrowfastSceneRuntime.IsDoorOpen(f.Zone,room.doorId));Assert.IsTrue(MorrowfastSceneRuntime.IsRoofLifted(f.Zone,room.roofId));Assert.AreNotSame(oldDoor.owner,f.View(room.doorId).owner);Assert.AreNotSame(oldRoof.owner,f.View(room.roofId).owner);Village3DIntegrationFixture.Hidden(f.View(room.roofId).root);Assert.IsFalse(f.Presenter.IsRenderedEntity(oldDoor.owner));}
        }
        [Test] public void SavedMovedGuardBindsLoadedReferenceAndActualCell()
        {
            using(var f=new Village3DIntegrationFixture())
            {var old=f.View("north-guard-west").owner;Assert.IsTrue(f.Zone.MoveEntity(old,40,23));f.Refresh();var pose=f.View("north-guard-west").root.transform.position;var loaded=f.RoundTrip();f.BindLoaded(loaded);var next=f.View("north-guard-west");Assert.AreNotSame(old,next.owner);Assert.AreEqual(old.ID,next.owner.ID);Assert.AreEqual((40,23),f.Zone.GetEntityPosition(next.owner));AssertNear(pose,next.root.transform.position,"Loaded displacement");Assert.IsFalse(f.Presenter.IsRenderedEntity(old));}
        }
        [Test] public void ViewRefreshAndRebindNeverRestockNativeContainer()
        {
            using(var f=new Village3DIntegrationFixture())
            {var spec=MorrowfastSceneDefinition.Load().owners.First(s=>MorrowfastSceneRuntime.FindOwner(f.Zone,s.id).GetPart<ContainerPart>()?.Contents.Count>0);var v=f.View(spec.id);var contents=v.owner.GetPart<ContainerPart>().Contents;var first=contents[0];contents.RemoveAt(0);var remaining=contents.ToArray();f.Refresh();f.Presenter.Bind(f.Zone,f.Source);f.Refresh();CollectionAssert.AreEqual(remaining,contents);Assert.IsFalse(contents.Contains(first));Assert.AreSame(v.owner,f.View(spec.id).owner);}
        }
        [Test] public void OrdinaryDroppedItemStaysNativeAndUnclaimedInsideAuthoredFootprint()
        {
            using(var f=new Village3DIntegrationFixture())
            {var well=f.View("central-cistern");var spec=MorrowfastSceneDefinition.Load().FindOwner("central-cistern");var p=spec.footprint.First();var item=f.Factory.CreateEntity("Dagger");Assert.IsTrue(f.Zone.AddEntity(item,p.x,p.y));f.Refresh();Assert.IsFalse(f.Presenter.IsRenderedEntity(item));Assert.AreSame(item,WorldInteractionSystem.ResolveTarget(f.Zone.GetCell(p.x,p.y)));Assert.AreSame(well.owner,f.View("central-cistern").owner);}
        }
        [Test] public void CameraFollowUsesReady3DSourceFitAndRestoresNormalFramingOnHide()
        {
            using(var f=new Village3DIntegrationFixture())
            {var follow=f.Source.gameObject.AddComponent<CameraFollow>();follow.Player=f.Player;follow.CurrentZone=f.Zone;var field=typeof(CameraFollow).GetFields(Private).SingleOrDefault(p=>p.FieldType==typeof(Village3DPresenter));Assert.NotNull(field,"CameraFollow must recognize 3D source fit.");field.SetValue(follow,f.Presenter);follow.SnapToPlayer();Assert.AreEqual(40,f.Source.transform.position.x,.001f);Assert.AreEqual(12.75f,f.Source.transform.position.y,.001f);Assert.AreEqual(13.75f,f.Source.orthographicSize,.001f);Assert.AreEqual(37.5f/25.5f,f.Source.aspect,.001f);f.Presenter.SetPresentationVisible(false);follow.SnapToPlayer();Assert.AreEqual(17,f.Source.orthographicSize,.001f);}
        }
        [Test] public void ActualWorldCameraProjectsXZCellsToSameViewportAsLegacyXYCamera()
        {
            using(var f=new Village3DIntegrationFixture())
            {f.Refresh();var world=f.Presenter.WorldCamera;foreach(var c in new[]{new Vector2Int(0,0),new Vector2Int(40,12),new Vector2Int(79,24),new Vector2Int(21,3)}){var legacy=f.Source.WorldToViewportPoint(new Vector3(c.x+.5f,24.5f-c.y,0));var actual=world.WorldToViewportPoint(Village3DProjection.CellCentre(c.x,c.y));Assert.AreEqual(legacy.x,actual.x,.0001f,c.ToString());Assert.AreEqual(legacy.y,actual.y,.0001f,c.ToString());}f.Source.transform.position+=new Vector3(7,2,0);f.Source.orthographicSize=10;f.Refresh();var a=f.Source.WorldToViewportPoint(new Vector3(48.5f,12.5f,0));var b=world.WorldToViewportPoint(Village3DProjection.CellCentre(48,12));Assert.AreEqual(a.x,b.x,.0001f);Assert.AreEqual(a.y,b.y,.0001f);}
        }
    }
}
