using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    // Only run meaningful missing-binding RED after real content adoption.
    // Current model/ownership/save proof; no generated cooking-site or paid-key claim.
    public sealed class ToastedEmberwheatArtTests
    {
        const string Blueprint="ToastedEmberwheat", Model="spread-toasted-emberwheat", Folder="SpreadCooking3D/";
        static GameObject Source()=>Resources.Load<GameObject>(Folder+Model);
        [Test] public void RealPreparedContentHasTheApprovedRecipeUnitsBeforeAnyArtClaim()
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var food=Create(f);var raw=f.Factory.CreateEntity("Emberwheat");
                Assert.NotNull(raw.GetPart<CookablePart>());Assert.AreEqual(Blueprint,raw.GetPart<CookablePart>().Into);
                Assert.AreEqual("2d4",raw.GetPart<FoodPart>().Healing);Assert.AreEqual("3d4",food.GetPart<FoodPart>().Healing);
                Assert.AreEqual(1,food.GetPart<PhysicsPart>().Weight);Assert.AreEqual(12,food.GetPart<CommercePart>().Value);
                Assert.IsNull(food.GetPart<CookablePart>());Assert.AreEqual(1,food.GetPart<StackerPart>().StackCount);
            }
        }
        [Test] public void OriginalPreparedGrainIsOneInertApprovedPersistentForm()
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))Create(f);
            Assert.NotNull(Resources.Load<ScriptableObject>(Folder+"Library"),"Missing original prepared-grain library.");
            var prefab=Source();Assert.NotNull(prefab);Assert.AreEqual(Vector3.zero,prefab.transform.localPosition);Assert.AreEqual(Quaternion.identity,prefab.transform.localRotation);Assert.AreEqual(Vector3.one,prefab.transform.localScale);
            var filters=prefab.GetComponentsInChildren<MeshFilter>(true);var renderers=prefab.GetComponentsInChildren<Renderer>(true);
            Assert.AreEqual(1,filters.Length);Assert.AreEqual(1,renderers.Length);Assert.AreEqual(1,renderers[0].sharedMaterials.Length);
            Assert.AreSame(ReferenceGladeVoxelLibrary.Load().Material,renderers[0].sharedMaterial);
            Assert.That(prefab.GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(prefab.GetComponentsInChildren<Rigidbody>(true),Is.Empty);Assert.That(prefab.GetComponentsInChildren<MonoBehaviour>(true),Is.Empty);Assert.That(prefab.GetComponentsInChildren<Animator>(true),Is.Empty);
            var mesh=filters[0].sharedMesh;Assert.True(mesh.isReadable);Assert.AreEqual(1,mesh.subMeshCount);Assert.AreEqual(160,mesh.GetIndexCount(0)/3,"Exactly eight original faceted kernels; no plate, raw sheaf or field mesh.");Assert.AreEqual(mesh.vertexCount,mesh.normals.Length);Assert.AreEqual(mesh.vertexCount,mesh.uv.Length);
            var v=mesh.vertices;Assert.That(v.Max(p=>p.y),Is.InRange(.055f,.10f));Assert.GreaterOrEqual(v.Min(p=>p.y),0f);Assert.That(v.Max(p=>p.x)-v.Min(p=>p.x),Is.InRange(.4f,.62f));Assert.True(v.All(p=>Mathf.Abs(p.x)<.5f&&Mathf.Abs(p.z)<.5f));
            Assert.AreNotSame(SpreadPortable3DLibrary.Load().Find("spread-portable-emberwheat").Mesh,mesh);
        }
        [Test] public void ActualCookedOutputDropsAsItsOwnExactApprovedPreparedFood()
        {
            var previous=MaterialReactionResolver.Factory;
            try
            {
                using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
                {
                    Create(f);MaterialReactionResolver.Factory=f.Factory;var raw=f.Factory.CreateEntity("Emberwheat");raw.GetPart<StackerPart>().StackCount=2;
                    Assert.True(f.Player.GetPart<InventoryPart>().AddObject(raw));var at=f.FreeCell();var fire=f.Add("Campfire",at.x,at.y);f.Approach(fire);
                    Assert.True(InventorySystem.PerformAction(f.Player,raw,"Cook",f.Zone));var meal=f.Player.GetPart<InventoryPart>().Objects.Single(e=>e.BlueprintName==Blueprint);
                    Assert.AreEqual(2,meal.GetPart<StackerPart>().StackCount);Assert.AreEqual(0,raw.GetPart<StackerPart>().StackCount);Assert.AreNotSame(raw,meal);
                    Assert.True(InventorySystem.Drop(f.Player,meal,f.Zone));f.Refresh();Exact(f,meal);Assert.AreEqual(2,meal.GetPart<StackerPart>().StackCount);Assert.IsNull(f.Zone.GetEntityCell(raw));
                }
            }
            finally{MaterialReactionResolver.Factory=previous;}
        }
        [Test] public void PickupDropAndFullSavedReplacementKeepTheRealFoodAndAppearance()
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var meal=Add(f);meal.GetPart<StackerPart>().StackCount=3;string id=meal.ID;f.Approach(meal);f.Refresh();Exact(f,meal);var old=f.View(meal);
                Assert.True(InventorySystem.Pickup(f.Player,meal,f.Zone));Assert.AreSame(f.Player,meal.GetPart<PhysicsPart>().InInventory);f.Refresh();Assert.False(f.Authored(meal));SpawnRing3DIntegrationFixture.Hidden(old);
                Assert.True(InventorySystem.Drop(f.Player,meal,f.Zone));f.Refresh();Exact(f,meal);var saved=f.RoundTrip();f.BindLoaded(saved);var restored=f.Zone.GetReadOnlyEntities().Single(e=>e.ID==id);
                Assert.AreNotSame(meal,restored);Assert.AreEqual(3,restored.GetPart<StackerPart>().StackCount);Assert.AreEqual("3d4",restored.GetPart<FoodPart>().Healing);Assert.IsNull(restored.GetPart<CookablePart>());Exact(f,restored);
                old=f.View(restored);Assert.True(f.Zone.RemoveEntity(restored));f.Refresh();Assert.False(f.Authored(restored));SpawnRing3DIntegrationFixture.Hidden(old);
            }
        }
        [Test] public void MovingTheSameGroundStackNeverReplacesItWithRawGrainOrChangesGeometry()
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var meal=Add(f);f.Refresh();Exact(f,meal);var original=f.View(meal).GetComponent<MeshFilter>().sharedMesh;var before=f.Zone.GetEntityPosition(meal);var cell=f.FreeCell(before.x/10,before.y/5);Assert.AreNotEqual(before,cell);Assert.True(f.Zone.MoveEntity(meal,cell.x,cell.y));f.Refresh();Exact(f,meal);Assert.AreSame(original,f.View(meal).GetComponent<MeshFilter>().sharedMesh);
            }
        }
        [TestCase("hidden")][TestCase("removed")][TestCase("carried")][TestCase("foreign-physics")]
        [TestCase("custom-visual")][TestCase("unknown-food")][TestCase("foreign-zone")][TestCase("foreign-map")]
        public void InvalidOrForeignFoodCannotAcquireThePreparedForm(string fault)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var meal=Add(f);var zone=f.Zone;
                if(fault=="hidden")meal.GetPart<RenderPart>().Visible=false;
                if(fault=="removed")Assert.True(zone.RemoveEntity(meal));
                if(fault=="carried")meal.GetPart<PhysicsPart>().InInventory=f.Player;
                if(fault=="foreign-physics")meal.GetPart<PhysicsPart>().ParentEntity=f.Player;
                if(fault=="custom-visual")meal.GetPart<RenderPart>().VisualID="custom-current-food";
                if(fault=="unknown-food")meal.BlueprintName="UnregisteredPreparedGrain";
                if(fault=="foreign-zone")zone=new Zone(zone.ZoneID);
                if(fault=="foreign-map")f.Manager.WorldMap.Tiles[12,10]=BiomeType.Sodden;
                Assert.AreNotEqual(Model,SpawnRing3DRecipes.Resolve(zone,meal,f.Library.Definition).ModelId,fault);
            }
        }
        [TestCase("Emberwheat","spread-portable-emberwheat")][TestCase("CookedMeat","spread-portable-cookedmeat")][TestCase("RoastedMushroom","spread-portable-roastedmushroom")]
        public void ExistingRawAndPreparedFoodsKeepTheirExactOriginalModels(string blueprint,string expected)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var at=f.FreeCell();var owner=f.Add(blueprint,at.x,at.y);f.Refresh();Assert.AreEqual(expected,SpawnRing3DRecipes.Resolve(f.Zone,owner,f.Library.Definition).ModelId);Assert.True(f.Find(owner,out var view,out var actual));Assert.AreEqual(expected,actual);Assert.AreSame(SpreadPortable3DLibrary.Load().Find(expected).Mesh,view.GetComponent<MeshFilter>().sharedMesh);Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(owner,out _));
            }
        }
        static Entity Create(SpawnRing3DIntegrationFixture f)
        {
            var meal=f.Factory.CreateEntity(Blueprint);Assert.NotNull(meal,"SOURCE PRECONDITION: publish real ToastedEmberwheat content before missing-binding RED.");Assert.AreEqual("%",meal.GetPart<RenderPart>().RenderString);Assert.AreEqual("&y",meal.GetPart<RenderPart>().ColorString);Assert.True(meal.GetPart<PhysicsPart>().Takeable);Assert.NotNull(meal.GetPart<FoodPart>());return meal;
        }
        static Entity Add(SpawnRing3DIntegrationFixture f){var meal=Create(f);var cell=f.FreeCell();Assert.True(f.Zone.AddEntity(meal,cell.x,cell.y));return meal;}
        static void Exact(SpawnRing3DIntegrationFixture f,Entity meal)
        {
            var recipe=SpawnRing3DRecipes.Resolve(f.Zone,meal,f.Library.Definition);Assert.AreEqual(Model,recipe.ModelId,recipe.Failure);Assert.AreSame(meal,recipe.Owner);Assert.True(recipe.Transient);Assert.False(recipe.Batched);
            Assert.True(f.Find(meal,out var view,out var id));Assert.AreEqual(Model,id);Assert.True(f.Rendered(meal));Assert.True(f.Pick(meal,out _));
            var mesh=Source().GetComponent<MeshFilter>().sharedMesh;Assert.AreSame(mesh,view.GetComponent<MeshFilter>().sharedMesh);
            Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(meal,out var proof),proof.Failure);Assert.AreEqual(Model,proof.ModelId);Assert.AreSame(mesh,proof.ExpectedMesh);Assert.AreSame(ReferenceGladeVoxelLibrary.Load().Material,proof.ExpectedMaterial);Assert.AreEqual(1,proof.PieceCount);
        }
    }
}
