using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    /// <summary>Actual imported ring art and current native entities. Private
    /// reads inspect queued presentation state/resources, not fake gameplay.
    /// No animation feel, final GPU pixel or performance claim from EditMode.</summary>
    public sealed class SpawnRing3DAdversarialTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static readonly string[] HookNames = { "MovedCallback", "AttackCallback", "DamageCallback", "DeathCallback", "CastCallback" };

        [Test] public void ActualFellingMeshHitReturnsExactNativeOwnerAndAnchor()
        {
            using (var f = new SpawnRing3DIntegrationFixture(FellingSiteBuilder.ZoneID))
            {
                Assert.IsTrue(FindFellingMeshPoint(f, false, out var owner, out var point), "Actual imported MeshCollider hit required.");
                Assert.IsTrue(Pick(f, point, out var picked, out var cell)); Assert.AreSame(owner,picked);
                Assert.AreEqual(f.Zone.GetEntityPosition(owner),cell);
                Assert.IsTrue(OwnMeshHit(f.View(owner),point));
            }
        }

        [Test] public void FellingAnchorCellGeometryHoleDoesNotFallBackToIndependentOwner()
        {
            using (var f = new SpawnRing3DIntegrationFixture(FellingSiteBuilder.ZoneID))
            {
                Assert.IsTrue(FindFellingMeshPoint(f, false, out _, out _), "Nonvacuous exact-mesh positive control.");
                Physics.SyncTransforms(); Entity candidate=null; Vector2 hole=default;
                foreach (var spec in FellingSceneDefinition.Load().layers)
                {
                    var owner=FellingSceneRuntime.FindOwner(f.Zone,spec.id); var cell=f.Zone.GetEntityCell(owner);
                    if (!ReferenceEquals(cell.GetTopVisibleObject(),owner)) continue;
                    for (int x=0;x<9 && candidate==null;x++) for(int y=0;y<9 && candidate==null;y++)
                    {
                        var point=new Vector2(cell.X+(x+.5f)/9,24-cell.Y+(y+.5f)/9);
                        var ray=new Ray(new Vector3(point.x,35,point.y),Vector3.down);
                        var hits=Physics.RaycastAll(ray,120,1<<NativeZone3DRenderSurface.WorldLayer,QueryTriggerInteraction.Collide);
                        if (hits.Any(h=>h.collider.transform.IsChildOf(f.Root.transform))) continue;
                        candidate=owner;hole=point;
                    }
                    if(candidate!=null)break;
                }
                Assert.NotNull(candidate,"Actual irregular model must leave an anchor-cell point without any owned geometry hit.");
                Assert.IsFalse(OwnMeshHit(f.View(candidate),hole));
                if(Pick(f,hole,out var selected,out _)) Assert.AreNotSame(candidate,selected,"A missed exact Felling mesh must not be rescued by native-cell fallback.");
            }
        }

        [Test] public void VisibleFellingFragmentRemainsPickableWhenItsRemoteAnchorIsUnseen()
        {
            using(var f=new SpawnRing3DIntegrationFixture(FellingSiteBuilder.ZoneID))
            {
                Assert.IsTrue(FindFellingMeshPoint(f,true,out var owner,out var point),"Actual pickable mesh fragment outside its native anchor cell required.");
                Assert.IsTrue(Village3DProjection.TryWorldToCell(new Vector3(point.x,0,point.y),out int x,out int y));
                var anchor=f.Zone.GetEntityCell(owner);Assert.AreNotEqual((anchor.X,anchor.Y),(x,y));
                anchor.IsVisible=false;anchor.Explored=false;f.Zone.GetCell(x,y).IsVisible=true;f.Zone.GetCell(x,y).Explored=true;
                f.Refresh(new HashSet<int>());Assert.IsTrue(f.Rendered(owner),"The current visible fragment still draws.");
                Assert.IsTrue(Pick(f,point,out var selected,out var result));Assert.AreSame(owner,selected);
                Assert.AreEqual((anchor.X,anchor.Y),result,"Selection returns the native owner anchor; action range remains native.");
                Assert.IsFalse(anchor.IsVisible);Assert.IsFalse(anchor.Explored);
            }
        }

        [TestCase(true)] [TestCase(false)]
        public void NativeUnknownTopObjectKeepsPickingPriorityOnlyWhileVisible(bool fallbackVisible)
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var actor=f.Add("CaveHermit");f.Refresh();Assert.IsTrue(f.Pick(actor,out var point));
                Assert.IsTrue(Village3DProjection.TryWorldToCell(new Vector3(point.x,0,point.y),out int x,out int y));
                var item=f.Factory.CreateEntity("HealingTonic");var render=item.GetPart<RenderPart>();Assert.NotNull(render);
                render.RenderLayer=10000;render.Visible=fallbackVisible;Assert.IsTrue(f.Zone.AddEntity(item,x,y));f.Refresh(f.Dirty(item));
                Assert.IsFalse(f.Authored(item));bool picked=Pick(f,point,out var selected,out _);
                if(fallbackVisible)Assert.IsFalse(picked,"The native top fallback may not be intercepted by scenery or the actor.");
                else{Assert.IsTrue(picked);Assert.AreSame(actor,selected);}
                Assert.AreSame(item,f.Zone.GetCell(x,y).Objects.Last(e=>ReferenceEquals(e,item)));
            }
        }

        [TestCase(float.NaN,1)] [TestCase(1,float.PositiveInfinity)]
        [TestCase(-.01f,1)] [TestCase(80,1)] [TestCase(1,-.01f)] [TestCase(1,25)]
        public void InvalidWorldPickResetsAllOutputs(float x,float y)
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                object[] args={new Vector2(x,y),f.Player,42,17};Assert.IsFalse((bool)f.Call("TryPickWorld",args));
                Assert.IsNull(args[1]);Assert.AreEqual(-1,args[2]);Assert.AreEqual(-1,args[3]);
                Assert.IsTrue(f.Pick(f.Player,out _),"Valid native actor remains pickable after invalid inputs.");
            }
        }

        [Test] public void NativeRenderHiddenActorCannotBePickedBeforeReconcile()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var actor=f.Add("CaveHermit");f.Refresh();Assert.IsTrue(f.Pick(actor,out var point));int version=f.Zone.EntityVersion;
                actor.GetPart<RenderPart>().Visible=false;
                if(Pick(f,point,out var selected,out _))Assert.AreNotSame(actor,selected);
                Assert.IsFalse(f.Rendered(actor));Assert.AreEqual(version,f.Zone.EntityVersion);
                actor.GetPart<RenderPart>().Visible=true;Assert.IsTrue(f.Pick(actor,out _));
            }
        }

        [Test] public void RemovedNativeActorCannotBePickedFromAStaleViewBeforeRefresh()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var actor=f.Add("CaveHermit");f.Refresh();Assert.IsTrue(f.Pick(actor,out var point));var view=f.View(actor);
                Assert.IsTrue(f.Zone.RemoveEntity(actor));Assert.IsFalse(f.Find(actor,out _,out _));
                if(Pick(f,point,out var selected,out _))Assert.AreNotSame(actor,selected);
                f.Refresh(new HashSet<int>());SpawnRing3DIntegrationFixture.Hidden(view);
            }
        }

        [Test] public void TakeableMemoryHidesItsViewAndColliderThenRecoversWithoutMembershipMutation()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var item=f.Add("Tepuibone");f.Refresh();var root=f.View(item);var cell=f.Zone.GetEntityCell(item);int version=f.Zone.EntityVersion;
                Assert.IsTrue(f.Rendered(item));Assert.IsTrue(root.GetComponentsInChildren<Collider>(true).Any(c=>c.enabled&&c.gameObject.activeInHierarchy));
                cell.IsVisible=false;f.Refresh(new HashSet<int>());Assert.IsFalse(f.Rendered(item));SpawnRing3DIntegrationFixture.Hidden(root);
                Assert.IsFalse(root.GetComponentsInChildren<Collider>(true).Any(c=>c.enabled&&c.gameObject.activeInHierarchy));Assert.IsTrue(cell.Explored);
                cell.IsVisible=true;f.Refresh(new HashSet<int>());Assert.AreSame(root,f.View(item));Assert.IsTrue(f.Rendered(item));Assert.AreEqual(version,f.Zone.EntityVersion);
            }
        }

        [Test] public void FullRevealDoesNotExploreNativeCellsOrRebuildGround()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var rock=f.Add("Rock");f.Refresh();var playerCell=f.Zone.GetEntityCell(f.Player);var rockCell=f.Zone.GetEntityCell(rock);
                playerCell.IsVisible=playerCell.Explored=rockCell.IsVisible=rockCell.Explored=false;int builds=f.Get<int>("GroundBuildCount");
                f.Set("FullReveal",true);f.Refresh(new HashSet<int>());Assert.IsTrue(f.Rendered(f.Player));Assert.IsTrue(f.Rendered(rock));
                Assert.IsFalse(playerCell.Explored);Assert.IsFalse(rockCell.Explored);Assert.AreEqual(builds,f.Get<int>("GroundBuildCount"));
                f.Set("FullReveal",false);f.Refresh(new HashSet<int>());Assert.IsFalse(f.Rendered(f.Player));Assert.IsFalse(f.Rendered(rock));
                Assert.AreEqual(builds,f.Get<int>("GroundBuildCount"));
            }
        }

        [Test] public void ActorGlyphReskinDropsAndRestoresOnlyItsOwnArtWithoutMembershipChange()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var actor=f.Add("GlasspaneFrog");f.Refresh();var old=f.View(actor);var player=f.View(f.Player);var render=actor.GetPart<RenderPart>();string glyph=render.RenderString;
                int version=f.Zone.EntityVersion,builds=f.Get<int>("GroundBuildCount");render.RenderString="?";f.Refresh(new HashSet<int>());
                Assert.IsFalse(f.Authored(actor));Assert.IsFalse(f.Find(actor,out _,out _));SpawnRing3DIntegrationFixture.Hidden(old);Assert.AreSame(player,f.View(f.Player));
                render.RenderString=glyph;f.Refresh(new HashSet<int>());Assert.IsTrue(f.Authored(actor));Assert.AreNotSame(old,f.View(actor));
                Assert.AreEqual(version,f.Zone.EntityVersion);Assert.AreEqual(builds,f.Get<int>("GroundBuildCount"));
            }
        }

        [TestCase(true)] [TestCase(false)]
        public void LiveTakeabilitySwitchRebindsBetweenBatchedAndTransientOwnership(bool makeTakeable)
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var item=f.Add("Tepuibone");var cell=f.Zone.GetEntityCell(item);var rock=f.Add("Rock",cell.X,cell.Y);
                var physics=item.GetPart<PhysicsPart>();physics.Takeable=!makeTakeable;f.Refresh();var old=f.View(item);int version=f.Zone.EntityVersion;
                Assert.AreEqual(makeTakeable,ReferenceEquals(old,f.View(rock)));
                physics.Takeable=makeTakeable;f.Refresh(new HashSet<int>());
                Assert.AreEqual(!makeTakeable,ReferenceEquals(f.View(item),f.View(rock)));Assert.AreNotSame(old,f.View(item));Assert.AreEqual(version,f.Zone.EntityVersion);
                cell.IsVisible=false;f.Refresh(new HashSet<int>());Assert.AreEqual(!makeTakeable,f.Rendered(item));
            }
        }

        [Test] public void ReplacementEntityWithSameIdNeverInheritsTheOldActorView()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var old=f.Add("GlasspaneFrog");f.Refresh();var oldView=f.View(old);var at=f.Zone.GetEntityPosition(old);string id=old.ID;
                Assert.IsTrue(f.Zone.RemoveEntity(old));var replacement=f.Factory.CreateEntity("GlasspaneFrog");replacement.ID=id;Assert.IsTrue(f.Zone.AddEntity(replacement,at.x,at.y));
                f.Refresh(new HashSet<int>());Assert.IsFalse(f.Find(old,out _,out _));SpawnRing3DIntegrationFixture.Hidden(oldView);
                Assert.AreNotSame(oldView,f.View(replacement));Assert.IsTrue(f.Rendered(replacement));Assert.AreEqual(id,replacement.ID);
            }
        }

        [Test] public void ReconcileRemovalMarkSurvivesAnExplicitEmptyDirtySet()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var rock=f.Add("Rock",21,11);f.Refresh();var root=f.View(rock);int revision=f.Revision(21,11),far=f.Revision(61,21);
                rock.GetPart<RenderPart>().Visible=false;f.Refresh(new HashSet<int>());
                Assert.IsFalse(f.Find(rock,out _,out _));Assert.Greater(f.Revision(21,11),revision);Assert.AreEqual(far,f.Revision(61,21));
                rock.GetPart<RenderPart>().Visible=true;f.Refresh(new HashSet<int>());Assert.AreSame(root,f.View(rock));
            }
        }

        [TestCase(false,false)] [TestCase(false,true)] [TestCase(true,false)] [TestCase(true,true)]
        public void WaterBoundaryEditsUpdateCardinalNeighbourPatchButKeepFarPatch(bool vertical,bool adding)
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                int ax=vertical?20:9,ay=vertical?4:10,bx=vertical?20:10,by=vertical?5:10;
                for(int y=Math.Min(ay,by)-1;y<=Math.Max(ay,by)+1;y++)for(int x=Math.Min(ax,bx)-1;x<=Math.Max(ax,bx)+1;x++)f.Zone.TileState.RemoveCoating(x,y,"water");
                f.Zone.TileState.WriteCoating(bx,by,"water",ZoneTileState.Permanent);if(!adding)f.Zone.TileState.WriteCoating(ax,ay,"water",ZoneTileState.Permanent);f.Refresh();
                int own=f.Revision(ax,ay),neighbour=f.Revision(bx,by),far=f.Revision(61,21);Assert.IsTrue(f.Water(bx,by));Assert.AreEqual(!adding,f.Water(ax,ay));
                if(adding)f.Zone.TileState.WriteCoating(ax,ay,"water",ZoneTileState.Permanent);else Assert.IsTrue(f.Zone.TileState.RemoveCoating(ax,ay,"water"));
                f.Refresh(SpawnRing3DIntegrationFixture.Dirty(ax,ay));Assert.AreEqual(adding,f.Water(ax,ay));Assert.IsTrue(f.Water(bx,by));
                Assert.Greater(f.Revision(ax,ay),own);Assert.Greater(f.Revision(bx,by),neighbour);Assert.AreEqual(far,f.Revision(61,21));
            }
        }

        [Test] public void TemporaryWaterOilEnergyAndCloudDoNotRebuildPermanentGroundGeometry()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var at=f.FreeCell();f.Zone.TileState.RemoveCoating(at.x,at.y,"water");f.Refresh();int builds=f.Get<int>("GroundBuildCount");
                f.Zone.TileState.WriteCoating(at.x,at.y,"water",7);f.Zone.TileState.WriteCoating(at.x,at.y,"oil",9);f.Zone.TileState.AddHeat(at.x,at.y,1);f.Zone.TileState.WriteCloud(at.x,at.y,"steam",3);
                f.Refresh(SpawnRing3DIntegrationFixture.Dirty(at.x,at.y));Assert.AreEqual(builds,f.Get<int>("GroundBuildCount"));Assert.IsFalse(f.Water(at.x,at.y));
                Assert.AreEqual(7,f.Zone.TileState.CoatingTurns(at.x,at.y,"water"));Assert.AreEqual(9,f.Zone.TileState.CoatingTurns(at.x,at.y,"oil"));
                Assert.AreEqual(1,f.Zone.TileState.Heat(at.x,at.y));Assert.AreEqual("steam",f.Zone.TileState.Get(at.x,at.y).Cloud);
            }
        }

        [TestCase("explicit-hide")] [TestCase("perspective")] [TestCase("null-source")] [TestCase("disable")]
        public void InterruptedPresentationDiscardsQueuedActionAndRetainsHooksOnRecovery(string reason)
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var actor=f.Add("CaveHermit");f.Refresh();var view=ViewState(f,actor);var until=view.GetType().GetField("ActionUntil");
                EntityVisualHooks.EmitAttack(actor,f.Player,f.Zone);Assert.Greater((float)until.GetValue(view),Time.unscaledTime);
                switch(reason){case "explicit-hide":f.Call("SetPresentationVisible",false);break;case "perspective":f.Source.orthographic=false;f.Bind(f.Zone);break;case "null-source":f.Call("Bind",f.Zone,null);break;case "disable":f.Presenter.enabled=false;break;}
                Assert.IsFalse(f.Get<bool>("PresentationVisible"));Assert.AreEqual(0,(float)until.GetValue(view),"No LateUpdate timer expiry before this assertion.");
                f.Source.orthographic=true;f.Presenter.enabled=true;f.Call("SetPresentationVisible",true);f.Bind(f.Zone);f.Frame();
                Assert.IsTrue(f.Get<bool>("PresentationVisible"));EntityVisualHooks.EmitAttack(actor,f.Player,f.Zone);Assert.Greater((float)until.GetValue(view),Time.unscaledTime);
            }
        }

        [Test] public void SameIdForeignZoneCannotQueueAnActionOnTheCurrentActor()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var actor=f.Add("CaveHermit");f.Refresh();var view=ViewState(f,actor);var until=view.GetType().GetField("ActionUntil");
                EntityVisualHooks.EmitAttack(actor,f.Player,new Zone(f.Zone.ZoneID));Assert.AreEqual(0,(float)until.GetValue(view));
                EntityVisualHooks.EmitAttack(actor,f.Player,f.Zone);Assert.Greater((float)until.GetValue(view),Time.unscaledTime);
                var point=f.FreeCell();Assert.IsTrue(MovementSystem.ForceMoveTo(actor,f.Zone,point.x,point.y));Assert.AreEqual(0,(float)until.GetValue(view));
                Assert.Less(Vector3.Distance(Village3DProjection.CellCentre(point.x,point.y),f.View(actor).transform.position),.001f);
            }
        }

        [Test] public void RepeatedBindDoesNotDuplicateHooksAndDisposalRestoresEveryPriorHook()
        {
            var before=HookNames.Select(Hook).ToArray();
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                for(int i=0;i<3;i++){f.Bind(f.Zone);f.Refresh(new HashSet<int>());}
                foreach(string name in HookNames)Assert.AreEqual(1,Hook(name).GetInvocationList().Count(d=>ReferenceEquals(d.Target,f.Presenter)),name);
            }
            for(int i=0;i<HookNames.Length;i++)Assert.AreSame(before[i],Hook(HookNames[i]),HookNames[i]);
        }

        [Test] public void ParentFirstDestructionReleasesOwnedGroundAndWaterMeshesButKeepsBorrowedAssets()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var field=f.Presenter.GetType().GetField("ground",Private);var ground=field.GetValue(f.Presenter);
                var masks=((Mesh[])ground.GetType().GetField("waterMeshes",Private).GetValue(ground)).ToArray();Assert.AreEqual(16,masks.Length);Assert.IsTrue(masks.All(m=>m!=null));
                var patches=(Array)ground.GetType().GetField("patches",Private).GetValue(ground);
                var meshes=new List<Mesh>();foreach(var patch in patches)foreach(string name in new[]{"WorldMesh","WaterMesh"})
                {var mesh=(Mesh)patch.GetType().GetField(name).GetValue(patch);if(mesh!=null)meshes.Add(mesh);}Assert.IsNotEmpty(meshes);
                var prefab=f.Library.FindModel(f.Library.Definition.FindBlueprint("Grass").models[0]);var borrowed=prefab.GetComponentsInChildren<MeshFilter>(true).First().sharedMesh;
                // Keep the borrowed camera alive when destroying only the
                // presenter's parent. Camera teardown may release its own RT.
                f.Source.transform.SetParent(null,true);
                try
                {
                    Assert.IsTrue(f.Borrowed.IsCreated(),"Live borrowed-source positive control.");
                    Object.DestroyImmediate(f.Root);Assert.IsTrue(masks.All(m=>m==null));Assert.IsTrue(meshes.All(m=>m==null));Assert.IsTrue(borrowed!=null);
                    Assert.IsTrue(f.Borrowed!=null&&f.Borrowed.IsCreated(),"Borrowed camera RT remains independently owned by the fixture.");
                }
                finally { if(f.Source!=null)Object.DestroyImmediate(f.Source.gameObject); }
            }
        }

        [TestCase("unknown-material")] [TestCase("empty-renderers")]
        public void RuntimeKnownModelFailureClosesClaimsAndCleansPartialResources(string defect)
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var item=f.Add("Tepuibone");var recipe=SpawnRing3DRecipes.Resolve(f.Zone,item,f.Library.Definition);
                var binding=f.Library.Models.Single(m=>m.Id==recipe.ModelId);var original=binding.Prefab;
                var clone=Object.Instantiate(original);clone.SetActive(false);Material foreign=null;
                try
                {
                    if(defect=="unknown-material")
                    {foreign=new Material(f.Library.WorldMaterial);var renderer=clone.GetComponentsInChildren<Renderer>(true).First();var materials=renderer.sharedMaterials;materials[materials.Length-1]=foreign;renderer.sharedMaterials=materials;}
                    else foreach(var renderer in clone.GetComponentsInChildren<Renderer>(true))Object.DestroyImmediate(renderer);
                    binding.Prefab=clone;f.Library.InvalidateCaches();
                    Assert.DoesNotThrow(()=>f.Refresh(),"A runtime known-model resource failure must select native fallback, not escape with stale claims.");
                    Assert.IsFalse(f.Get<bool>("PresentationVisible"));Assert.IsFalse(f.Authored(item));Assert.IsFalse(f.Find(item,out _,out _));
                    Assert.IsFalse((bool)f.Call("ClaimsCell",20,10));Assert.IsNotEmpty(f.Get<string>("Failure"));
                    Assert.IsNull(f.Get<Camera>("WorldCamera"),"Failed partial owned surface is released.");Assert.AreSame(f.Borrowed,f.Source.targetTexture);
                }
                finally{binding.Prefab=original;f.Library.InvalidateCaches();if(clone!=null)Object.DestroyImmediate(clone);if(foreign!=null)Object.DestroyImmediate(foreign);}
                // Recovery is explicit rebind via another zone, not a new promise
                // to retry the same cached failure on every frame.
                f.Bind(new Zone("Overworld.3.6.0"));f.Bind(f.Zone);f.Refresh();Assert.IsTrue(f.Rendered(item));Assert.AreSame(original,binding.Prefab);
            }
        }

        [Test] public void FailedInitialResourceBindingLeavesNoOwnedSurfaceAndCanExplicitlyRecover()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var old=f.View(f.Player);f.Bind(new Zone("Overworld.3.6.0"));int index=f.Library.RendererIndex;
                try{f.Library.RendererIndex=-1;Assert.DoesNotThrow(()=>f.Bind(f.Zone));Assert.IsFalse(f.Get<bool>("IsReady"));Assert.IsFalse(f.Authored(f.Player));Assert.IsNull(f.Get<Camera>("WorldCamera"));Assert.IsNotEmpty(f.Get<string>("Failure"));}
                finally{f.Library.RendererIndex=index;}
                SpawnRing3DIntegrationFixture.Hidden(old);Assert.IsTrue(f.Borrowed.IsCreated());
                f.Bind(new Zone("Overworld.3.6.0"));f.Bind(f.Zone);f.Refresh();Assert.IsTrue(f.Rendered(f.Player));
            }
        }

        [Test] public void GroundRebuildKeepsImportedMeshChannelsAndMaterialReferencesUnchanged()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var ground=f.Add("Grass",21,11);f.Refresh();var recipe=SpawnRing3DRecipes.Resolve(f.Zone,ground,f.Library.Definition);var prefab=f.Library.FindModel(recipe.ModelId);
                var filter=prefab.GetComponentsInChildren<MeshFilter>(true).First();var mesh=filter.sharedMesh;var renderer=filter.GetComponent<MeshRenderer>();
                var vertices=mesh.vertices;var normals=mesh.normals;var uv=mesh.uv;var colors=mesh.colors;var materials=renderer.sharedMaterials;int revision=f.Revision(21,11);
                Assert.IsNotEmpty(vertices);Assert.AreEqual(vertices.Length,normals.Length);Assert.AreEqual(vertices.Length,uv.Length);
                ground.GetPart<RenderPart>().Visible=false;f.Refresh(f.Dirty(ground));ground.GetPart<RenderPart>().Visible=true;f.Refresh(f.Dirty(ground));
                Assert.Greater(f.Revision(21,11),revision);Assert.AreSame(mesh,filter.sharedMesh);
                CollectionAssert.AreEqual(vertices,mesh.vertices);CollectionAssert.AreEqual(normals,mesh.normals);CollectionAssert.AreEqual(uv,mesh.uv);CollectionAssert.AreEqual(colors,mesh.colors);
                CollectionAssert.AreEqual(materials,renderer.sharedMaterials);
            }
        }

        [Test] public void ActualMeshCloneColorPayloadSurvivesCombinationWithoutChangingTheSource()
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var owner=f.Add("Grass",21,11);var recipe=SpawnRing3DRecipes.Resolve(f.Zone,owner,f.Library.Definition);
                var binding=f.Library.Models.Single(m=>m.Id==recipe.ModelId);var original=binding.Prefab;
                var clone=Object.Instantiate(original);clone.SetActive(true);var filter=clone.GetComponentsInChildren<MeshFilter>(true).First();
                var source=filter.sharedMesh;var oldColors=source.colors32;var mesh=Object.Instantiate(source);filter.sharedMesh=mesh;
                var color=new Color32(31,97,173,211);mesh.colors32=Enumerable.Repeat(color,mesh.vertexCount).ToArray();
                try
                {
                    binding.Prefab=clone;f.Library.InvalidateCaches();f.Bind(new Zone("Overworld.3.6.0"));f.Bind(f.Zone);f.Refresh();
                    var combined=f.View(owner).GetComponentsInChildren<MeshFilter>(true).Select(m=>m.sharedMesh).Where(m=>m!=null).ToArray();
                    Assert.IsTrue(combined.Any(m=>m.colors32.Any(c=>c.Equals(color))),"Nonempty vertex color payload must reach an owned combined mesh.");
                    CollectionAssert.AreEqual(oldColors,source.colors32);Assert.IsTrue(mesh.colors32.All(c=>c.Equals(color)));
                    Assert.IsFalse(combined.Any(m=>ReferenceEquals(m,mesh)||ReferenceEquals(m,source)),"Combination must own its output.");
                }
                finally{binding.Prefab=original;f.Library.InvalidateCaches();Object.DestroyImmediate(clone);Object.DestroyImmediate(mesh);}
            }
        }

        static Delegate Hook(string name)=>(Delegate)typeof(EntityVisualHooks).GetProperty(name,BindingFlags.Public|BindingFlags.Static).GetValue(null);
        static object ViewState(SpawnRing3DIntegrationFixture f,Entity owner)
            =>((IDictionary)f.Presenter.GetType().GetField("views",Private).GetValue(f.Presenter))[owner];
        static bool Pick(SpawnRing3DIntegrationFixture f,Vector2 point,out Entity owner,out (int x,int y) cell)
        {object[] args={point,null,-1,-1};bool hit=(bool)f.Call("TryPickWorld",args);owner=args[1]as Entity;cell=((int)args[2],(int)args[3]);return hit;}
        static bool OwnMeshHit(GameObject root,Vector2 point)
        {
            var ray=new Ray(new Vector3(point.x,35,point.y),Vector3.down);
            foreach(var collider in root.GetComponentsInChildren<MeshCollider>(true))
                if(collider.enabled&&collider.gameObject.activeInHierarchy&&collider.Raycast(ray,out _,120))return true;
            return false;
        }
        static bool FindFellingMeshPoint(SpawnRing3DIntegrationFixture f,bool awayFromAnchor,out Entity owner,out Vector2 point)
        {
            Physics.SyncTransforms();
            foreach(var spec in FellingSceneDefinition.Load().layers)
            {
                var candidate=FellingSceneRuntime.FindOwner(f.Zone,spec.id);var anchor=f.Zone.GetEntityCell(candidate);var root=f.View(candidate);
                foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    var bounds=renderer.bounds;
                    for(int x=0;x<9;x++)for(int z=0;z<9;z++)
                    {
                        var p=new Vector2(Mathf.Lerp(bounds.min.x,bounds.max.x,(x+.5f)/9),Mathf.Lerp(bounds.min.z,bounds.max.z,(z+.5f)/9));
                        if(!Village3DProjection.TryWorldToCell(new Vector3(p.x,0,p.y),out int cx,out int cy)||awayFromAnchor&&(cx,cy)==(anchor.X,anchor.Y)||!OwnMeshHit(root,p))continue;
                        if(Pick(f,p,out var picked,out _)&&ReferenceEquals(picked,candidate)){owner=candidate;point=p;return true;}
                    }
                }
            }
            owner=null;point=default;return false;
        }
    }
}
