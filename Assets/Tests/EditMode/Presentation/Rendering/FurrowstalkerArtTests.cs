using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    // Real content must precede interpreting this fixture as missing-art RED.
    // Source identity, imported mesh/poses and explicit presenter refresh only.
    // No generated hunt, scheduler, successful kill or native-pixel claim.
    public sealed class FurrowstalkerArtTests
    {
        const string Actor="Furrowstalker",Corpse="FurrowstalkerCorpse";
        const string Live="spread-furrowstalker",Remains="spread-furrowstalker-remains",Folder="Furrowstalker3D/";
        static readonly string[] Clips={"Idle","Walk","Interact","Attack","Hit"};
        static readonly string[] Bones={"Root","Body","Neck","Head","Jaw","LegFront.L","LegRear.L","LegFront.R","LegRear.R","Tail","TailTip"};
        static GameObject Prefab(string id)=>Resources.Load<GameObject>(Folder+id);
        static Mesh MeshOf(GameObject root)=>root.GetComponentInChildren<SkinnedMeshRenderer>(true)?.sharedMesh??root.GetComponentInChildren<MeshFilter>(true)?.sharedMesh;
        static bool IsOurModel(string id)=>id==Live||id==Remains;

        [Test]
        public void ActualChildAndSourceRemainsExistBeforeAnyMissingArtClaim()
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor=Create(f,Actor);var body=actor.GetPart<Body>();Assert.NotNull(body);
                Assert.AreEqual("Quadruped",actor.GetProperty("Anatomy"));Assert.True(actor.HasTag("BodyNaturalAttack"));Assert.NotNull(actor.GetPart<MeleeWeaponPart>());
                var corpse=Create(f,Corpse);Assert.IsNull(corpse.GetPart<HarvestablePart>());
                Assert.AreEqual(Corpse,actor.GetPart<CorpsePart>().CorpseBlueprint);
            }
        }

        [TestCase(Live,true,480)][TestCase(Remains,false,192)]
        public void TwoOriginalPersistentFormsUseExactApprovedPaletteAndInertOwnedBuffers(string id,bool rigged,int triangles)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))Create(f,rigged?Actor:Corpse);
            Assert.NotNull(Resources.Load<ScriptableObject>(Folder+"Library"),"Missing optional original-hunter library.");
            var prefab=Prefab(id);Assert.NotNull(prefab,id);Assert.AreEqual(Vector3.zero,prefab.transform.localPosition);Assert.AreEqual(Quaternion.identity,prefab.transform.localRotation);Assert.AreEqual(Vector3.one,prefab.transform.localScale);
            var renderers=prefab.GetComponentsInChildren<Renderer>(true);Assert.AreEqual(1,renderers.Length);Assert.AreEqual(1,renderers[0].sharedMaterials.Length);Assert.AreSame(ReferenceGladeVoxelLibrary.Load().Material,renderers[0].sharedMaterial);
            Assert.That(prefab.GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(prefab.GetComponentsInChildren<Rigidbody>(true),Is.Empty);Assert.That(prefab.GetComponentsInChildren<MonoBehaviour>(true),Is.Empty);
            Assert.AreEqual(rigged,prefab.GetComponentInChildren<Animator>(true)!=null);var mesh=MeshOf(prefab);Assert.NotNull(mesh);Assert.True(mesh.isReadable);Assert.AreEqual(1,mesh.subMeshCount);Assert.AreEqual(triangles,mesh.GetIndexCount(0)/3);
            Assert.AreEqual(mesh.vertexCount,mesh.normals.Length);Assert.AreEqual(mesh.vertexCount,mesh.uv.Length);Assert.True(mesh.vertices.All(v=>Finite(v.x)&&Finite(v.y)&&Finite(v.z)));
            foreach(var uv in mesh.uv){Assert.AreEqual(.5f,uv.y);Assert.True(Enumerable.Range(0,24).Any(i=>Mathf.Abs(uv.x-(i+.5f)/24f)<.00001f));}
            if(!rigged){Assert.That(mesh.boneWeights,Is.Empty);Assert.AreEqual(0,mesh.bindposeCount);Assert.Less(mesh.bounds.size.y,.30f);}
            Assert.AreNotSame(MeshOf(Resources.Load<GameObject>("QuestFreeSpread3D/questfree-spread-grazer")),mesh);
        }

        [Test]
        public void ActualImportedHunterHasElevenOwnedBonesAndFiveDeformingStatesWithLoweredFeedingAndJawStrike()
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor=Add(f,Actor);f.Refresh();var view=Exact(f,actor,Live);
                var skin=view.GetComponentInChildren<SkinnedMeshRenderer>();var animator=view.GetComponentInChildren<Animator>();Assert.NotNull(skin);Assert.NotNull(animator);Assert.False(animator.applyRootMotion);
                CollectionAssert.AreEquivalent(Bones,skin.bones.Select(b=>b.name));Assert.True(skin.bones.All(b=>b.IsChildOf(view.transform)));Assert.AreEqual(11,skin.sharedMesh.bindposeCount);
                var clips=animator.runtimeAnimatorController.animationClips;CollectionAssert.AreEquivalent(Clips,clips.Select(c=>c.name));var baked=new Mesh();var root=skin.bones.Single(b=>b.name=="Root");
                var head=Indices(skin,"Head");var jaw=Indices(skin,"Jaw");Assert.Greater(head.Length,0);Assert.Greater(jaw.Length,0);
                try
                {
                    foreach(var clip in clips)
                    {
                        clip.SampleAnimation(animator.gameObject,0);skin.BakeMesh(baked);var before=baked.vertices;var rootBefore=root.localPosition;
                        clip.SampleAnimation(animator.gameObject,clip.length*.31f);skin.BakeMesh(baked);Assert.True(before.Where((v,i)=>(v-baked.vertices[i]).sqrMagnitude>.0000001f).Any(),clip.name);Assert.Less((root.localPosition-rootBefore).sqrMagnitude,.0000001f,clip.name);
                    }
                    var interact=clips.Single(c=>c.name=="Interact");interact.SampleAnimation(animator.gameObject,0);float beforeHead=Centre(skin,baked,head).y;
                    interact.SampleAnimation(animator.gameObject,interact.length*.5f);Assert.Less(Centre(skin,baked,head).y,beforeHead-.04f,"Actual world-up head descent, not the rotated skin-local axis.");
                    var idle=clips.Single(c=>c.name=="Idle");idle.SampleAnimation(animator.gameObject,0);float idleHead=Centre(skin,baked,head).y;idle.SampleAnimation(animator.gameObject,idle.length*.5f);Assert.Less(Mathf.Abs(Centre(skin,baked,head).y-idleHead),.02f,"Idle does not play feeding.");
                    var attack=clips.Single(c=>c.name=="Attack");attack.SampleAnimation(animator.gameObject,0);float initialGap=Vector3.Distance(Centre(skin,baked,head),Centre(skin,baked,jaw));attack.SampleAnimation(animator.gameObject,attack.length*.5f);Assert.Greater(Vector3.Distance(Centre(skin,baked,head),Centre(skin,baked,jaw)),initialGap+.01f);attack.SampleAnimation(animator.gameObject,attack.length);Assert.That(Vector3.Distance(Centre(skin,baked,head),Centre(skin,baked,jaw)),Is.EqualTo(initialGap).Within(.001f));
                }
                finally{Object.DestroyImmediate(baked);}
            }
        }

        [Test]
        public void CurrentHunterMovementVisibilityRemovalAndFullSavedReplacementKeepOnlyTheActualOwner()
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor=Add(f,Actor);string id=actor.ID;f.Refresh();var view=Exact(f,actor,Live);var free=f.FreeCell();Assert.True(f.Zone.MoveEntity(actor,free.x,free.y));f.Refresh();Assert.AreSame(view,Exact(f,actor,Live));Assert.AreEqual(Village3DProjection.CellCentre(free.x,free.y),view.transform.position);
                var cell=f.Zone.GetEntityCell(actor);cell.IsVisible=false;f.Refresh();Assert.False(f.Rendered(actor));SpawnRing3DIntegrationFixture.Hidden(view);cell.IsVisible=true;f.Refresh();Exact(f,actor,Live);
                var loaded=f.RoundTrip();f.BindLoaded(loaded);var current=f.Zone.GetReadOnlyEntities().Single(e=>e.ID==id);Assert.AreNotSame(actor,current);Assert.NotNull(current.GetPart("SpreadPredator"));Exact(f,current,Live);Assert.False(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(actor,out _));
                var currentView=f.View(current);Assert.True(f.Zone.RemoveEntity(current));f.Refresh();Assert.False(f.Find(current,out _,out _));SpawnRing3DIntegrationFixture.Hidden(currentView);
            }
        }

        [Test]
        public void ExactSourceRemainsUseTheirOwnStaticBodyAndNativePickupDropRemovesThenRestoresOnlyThatView()
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor=Add(f,Actor);var corpse=Add(f,Corpse);corpse.Properties["SourceBlueprint"]=Actor;corpse.Properties["SourceID"]=actor.ID;f.Refresh();var view=Exact(f,corpse,Remains);Assert.That(view.GetComponentsInChildren<Animator>(true),Is.Empty);
                f.Approach(corpse);Assert.True(InventorySystem.Pickup(f.Player,corpse,f.Zone));Assert.AreSame(f.Player,corpse.GetPart<PhysicsPart>().InInventory);f.Refresh();Assert.False(f.Find(corpse,out _,out _));SpawnRing3DIntegrationFixture.Hidden(view);
                Assert.True(InventorySystem.Drop(f.Player,corpse,f.Zone));f.Refresh();Exact(f,corpse,Remains);corpse.Properties["SourceBlueprint"]="ReedbackGrazer";f.Refresh();Assert.AreNotEqual(Remains,SpawnRing3DRecipes.Resolve(f.Zone,corpse,f.Library.Definition).ModelId);
                corpse.Properties["SourceBlueprint"]=Actor;corpse.Properties["SourceID"]="";f.Refresh();Assert.AreNotEqual(Remains,SpawnRing3DRecipes.Resolve(f.Zone,corpse,f.Library.Definition).ModelId);
            }
        }

        [TestCase("glyph")][TestCase("color")][TestCase("visual")][TestCase("variant")][TestCase("carried")][TestCase("equipped")]
        [TestCase("hidden")][TestCase("foreign-role")][TestCase("foreign-physics")][TestCase("foreign-zone")][TestCase("foreign-map")]
        public void InvalidOrForeignActualHunterCannotAcquireTheScopedBody(string fault)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor=Add(f,Actor);var render=actor.GetPart<RenderPart>();var physics=actor.GetPart<PhysicsPart>();var role=actor.GetPart("SpreadPredator");var zone=f.Zone;
                try
                {
                    if(fault=="glyph")render.RenderString="?";if(fault=="color")render.ColorString="&R";if(fault=="visual")render.VisualID="custom-hunter";if(fault=="variant")render.VisualVariant="custom-current";
                    if(fault=="carried")physics.InInventory=f.Player;if(fault=="equipped")physics.Equipped=f.Player;if(fault=="hidden")render.Visible=false;if(fault=="foreign-role")role.ParentEntity=f.Player;if(fault=="foreign-physics")physics.ParentEntity=f.Player;
                    if(fault=="foreign-zone")zone=new Zone(zone.ZoneID);if(fault=="foreign-map")f.Manager.WorldMap.Tiles[12,10]=BiomeType.Sodden;
                    Assert.False(IsOurModel(SpawnRing3DRecipes.Resolve(zone,actor,f.Library.Definition).ModelId),fault);
                }
                finally{role.ParentEntity=actor;physics.ParentEntity=actor;physics.InInventory=null;physics.Equipped=null;}
            }
        }

        [Test]
        public void StaleSpatialBacklinksCannotBorrowTheStillRegisteredHuntersApprovedAppearance()
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor=Add(f,Actor);f.Refresh();Exact(f,actor,Live);var cell=f.Zone.GetEntityCell(actor);var field=typeof(Entity).GetField("SpatialZone",BindingFlags.NonPublic|BindingFlags.Instance);Assert.NotNull(field);var old=field.GetValue(actor);var parent=cell.ParentZone;
                try
                {
                    field.SetValue(actor,new Zone(f.Zone.ZoneID));Assert.AreSame(cell,f.Zone.GetEntityCell(actor));Assert.True(cell.Objects.Contains(actor));Assert.AreNotEqual(Live,SpawnRing3DRecipes.Resolve(f.Zone,actor,f.Library.Definition).ModelId);
                    field.SetValue(actor,old);cell.ParentZone=new Zone(f.Zone.ZoneID);Assert.AreNotEqual(Live,SpawnRing3DRecipes.Resolve(f.Zone,actor,f.Library.Definition).ModelId);
                }
                finally{field.SetValue(actor,old);cell.ParentZone=parent;}
                f.Refresh();Exact(f,actor,Live);
            }
        }

        [TestCase("ReedbackGrazer","questfree-spread-grazer")][TestCase("Viper","spread-biome-viper")]
        public void ExistingLivingAnimalModelsRemainTheirOriginalApprovedBodies(string blueprint,string model)
        {using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0")){var animal=f.Add(blueprint);f.Refresh();Assert.True(f.Find(animal,out _,out var actual));Assert.AreEqual(model,actual);Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(animal,out _));Assert.False(IsOurModel(actual));}}

        static Entity Create(SpawnRing3DIntegrationFixture f,string blueprint)
        {
            var owner=f.Factory.CreateEntity(blueprint);Assert.NotNull(owner,"SOURCE PRECONDITION: publish real "+blueprint+" before missing-binding RED.");Assert.AreEqual(blueprint,owner.BlueprintName);var render=owner.GetPart<RenderPart>();Assert.NotNull(render);Assert.AreEqual(blueprint==Actor?"f":"%",render.RenderString);Assert.AreEqual("&y",render.ColorString);
            if(blueprint==Actor){var role=owner.GetPart("SpreadPredator");Assert.NotNull(role,"Actual source role required.");Assert.AreSame(owner,role.ParentEntity);Assert.NotNull(owner.GetPart<BrainPart>());Assert.NotNull(owner.GetPart<Body>());Assert.False(owner.GetPart<PhysicsPart>().Takeable);}
            else{Assert.True(owner.HasTag("Corpse"));Assert.True(owner.GetPart<PhysicsPart>().Takeable);}
            return owner;
        }
        static Entity Add(SpawnRing3DIntegrationFixture f,string blueprint){var owner=Create(f,blueprint);var cell=f.FreeCell();Assert.True(f.Zone.AddEntity(owner,cell.x,cell.y));return owner;}
        static GameObject Exact(SpawnRing3DIntegrationFixture f,Entity owner,string model)
        {
            var recipe=SpawnRing3DRecipes.Resolve(f.Zone,owner,f.Library.Definition);Assert.AreEqual(model,recipe.ModelId,recipe.Failure);Assert.AreSame(owner,recipe.Owner);Assert.True(recipe.Transient);Assert.False(recipe.Batched);
            Assert.True(f.Find(owner,out var view,out var actual));Assert.AreEqual(model,actual);Assert.True(f.Rendered(owner));Assert.True(f.Pick(owner,out _));Assert.AreSame(MeshOf(Prefab(model)),MeshOf(view));
            Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(owner,out var proof),proof.Failure);Assert.AreEqual(model,proof.ModelId);Assert.AreEqual(1,proof.PieceCount);Assert.AreSame(MeshOf(Prefab(model)),proof.ExpectedMesh);Assert.AreSame(ReferenceGladeVoxelLibrary.Load().Material,proof.ExpectedMaterial);return view;
        }
        static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
        static int[] Indices(SkinnedMeshRenderer skin,string bone){int i=Array.FindIndex(skin.bones,b=>b.name==bone);Assert.GreaterOrEqual(i,0);return Enumerable.Range(0,skin.sharedMesh.vertexCount).Where(v=>skin.sharedMesh.boneWeights[v].boneIndex0==i).ToArray();}
        static Vector3 Centre(SkinnedMeshRenderer skin,Mesh bake,int[] indices){skin.BakeMesh(bake);var vertices=bake.vertices;var sum=Vector3.zero;foreach(int i in indices)sum+=skin.transform.TransformPoint(vertices[i]);return sum/indices.Length;}
    }
}
