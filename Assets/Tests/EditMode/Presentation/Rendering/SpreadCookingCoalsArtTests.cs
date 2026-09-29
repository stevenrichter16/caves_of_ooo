using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    // Execute only after the actual source child exists. This proves current
    // model re-evaluation, not normal input dirty cadence or gameplay pixels.
    public sealed class SpreadCookingCoalsArtTests
    {
        const string Blueprint="SpreadCookingCoals",Hot="spread-cooking-coals-hot",Cooled="spread-cooking-coals-cooled",Folder="SpreadCookingCoals3D/";
        static GameObject Prefab(string id)=>Resources.Load<GameObject>(Folder+id);
        [Test] public void OriginalPairHasSameGeometryAndFootprintWithoutBorrowedFlameOrLights()
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))Create(f);
            Assert.NotNull(Resources.Load<ScriptableObject>(Folder+"Library"),"Original coals library missing.");
            Mesh previous=null;foreach(var id in new[]{Hot,Cooled})
            {
                var p=Prefab(id);Assert.NotNull(p,id);Assert.AreEqual(0,p.transform.childCount);Assert.AreEqual(Vector3.zero,p.transform.localPosition);Assert.AreEqual(Quaternion.identity,p.transform.localRotation);Assert.AreEqual(Vector3.one,p.transform.localScale);
                Assert.AreEqual(3,p.GetComponents<Component>().Length);Assert.That(p.GetComponentsInChildren<Light>(true),Is.Empty);Assert.That(p.GetComponentsInChildren<ParticleSystem>(true),Is.Empty);Assert.That(p.GetComponentsInChildren<Collider>(true),Is.Empty);Assert.That(p.GetComponentsInChildren<Rigidbody>(true),Is.Empty);Assert.That(p.GetComponentsInChildren<MonoBehaviour>(true),Is.Empty);Assert.That(p.GetComponentsInChildren<Animator>(true),Is.Empty);
                var mesh=p.GetComponent<MeshFilter>().sharedMesh;Assert.True(mesh.isReadable);Assert.AreEqual(1,mesh.subMeshCount);Assert.AreEqual(260,mesh.GetIndexCount(0)/3);Assert.AreEqual(1,p.GetComponent<MeshRenderer>().sharedMaterials.Length);Assert.AreSame(ReferenceGladeVoxelLibrary.Load().Material,p.GetComponent<MeshRenderer>().sharedMaterial);
                Assert.True(mesh.vertices.All(v=>Mathf.Abs(v.x)<=.46f&&Mathf.Abs(v.z)<=.46f&&v.y>=-.00001f&&v.y<.16f));Assert.Greater(mesh.bounds.size.x,.7f);Assert.Greater(mesh.bounds.size.z,.6f);
                if(previous!=null){Assert.AreNotSame(previous,mesh);CollectionAssert.AreEqual(previous.vertices,mesh.vertices);CollectionAssert.AreEqual(previous.triangles,mesh.triangles);CollectionAssert.AreEqual(previous.normals,mesh.normals);Assert.AreEqual(previous.bounds,mesh.bounds);Assert.False(previous.uv.SequenceEqual(mesh.uv),"Only actual thermal face palette differs.");}previous=mesh;
            }
        }
        [TestCase(500f,25f,Hot)][TestCase(150f,25f,Hot)][TestCase(149.99f,25f,Cooled)][TestCase(500f,0f,Hot)]
        public void ExactCurrentThermalAppearanceDoesNotConfuseFuelExhaustionWithCold(float temperature,float fuel,string expected)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {var owner=Add(f);owner.GetPart<ThermalPart>().Temperature=temperature;owner.GetPart<FuelPart>().FuelMass=fuel;f.Refresh();Exact(f,owner,expected);Assert.AreEqual(temperature,owner.GetPart<ThermalPart>().Temperature);Assert.AreEqual(fuel,owner.GetPart<FuelPart>().FuelMass);}
        }
        [TestCase(150f,Cooled)][TestCase(500f,Hot)]
        public void ActualMaterialPassReevaluatesSameOwnerWithoutMovementOrForcedRebind(float start,string expected)
        {
            var old=MaterialReactionResolver.Factory;
            try
            {
                using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
                {
                    MaterialReactionResolver.Factory=f.Factory;var owner=Add(f);owner.GetPart<ThermalPart>().Temperature=start;f.Refresh();Exact(f,owner,Hot);var cell=f.Zone.GetEntityCell(owner);var player=f.Zone.GetEntityPosition(f.Player);string id=owner.ID;var before=f.View(owner).GetComponent<MeshFilter>().sharedMesh;int version=f.Zone.EntityVersion;
                    MaterialSimSystem.TickMaterialEntities(f.Zone);
                    Assert.AreEqual(start-(start-25f)*.02f,owner.GetPart<ThermalPart>().Temperature,.0001f);Assert.AreEqual(25f,owner.GetPart<FuelPart>().FuelMass);Assert.AreEqual(version,f.Zone.EntityVersion);Assert.AreSame(cell,f.Zone.GetEntityCell(owner));Assert.AreEqual(player,f.Zone.GetEntityPosition(f.Player));Assert.AreEqual(id,owner.ID);
                    // Ordinary presenter refresh is explicit here. No Bind, move,
                    // MarkDirty, or invented input/renderer cadence assertion.
                    f.Refresh(new HashSet<int>());Exact(f,owner,expected);
                    if(expected==Cooled)Assert.AreNotSame(before,f.View(owner).GetComponent<MeshFilter>().sharedMesh);else Assert.AreSame(before,f.View(owner).GetComponent<MeshFilter>().sharedMesh);
                }
            }
            finally{MaterialReactionResolver.Factory=old;}
        }
        [Test] public void SavedCooledExhaustedSourceKeepsReplacementIdentityAndDoesNotReheat()
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var owner=Add(f);owner.GetPart<ThermalPart>().Temperature=140;owner.GetPart<FuelPart>().FuelMass=0;string id=owner.ID;var at=f.Zone.GetEntityPosition(owner);f.Refresh();Exact(f,owner,Cooled);f.BindLoaded(f.RoundTrip());var loaded=f.Zone.GetReadOnlyEntities().Single(e=>e.ID==id);
                Assert.AreNotSame(owner,loaded);Assert.AreEqual(at,f.Zone.GetEntityPosition(loaded));Assert.AreEqual(140,loaded.GetPart<ThermalPart>().Temperature);Assert.AreEqual(0,loaded.GetPart<FuelPart>().FuelMass);Assert.True(loaded.GetPart<CampfirePart>().FiniteCooking);Assert.False((bool)typeof(CampfirePart).GetField("AllowRest").GetValue(loaded.GetPart<CampfirePart>()));Exact(f,loaded,Cooled);
            }
        }
        [TestCase("hidden")][TestCase("removed")][TestCase("foreign-physics")][TestCase("foreign-thermal")][TestCase("foreign-fuel")]
        [TestCase("nonfinite-heat")][TestCase("custom-visual")][TestCase("unknown-owner")][TestCase("foreign-zone")][TestCase("foreign-map")]
        public void InvalidCurrentSourceDoesNotAcquireConfidentThermalArt(string fault)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var owner=Add(f);var zone=f.Zone;
                if(fault=="hidden")owner.GetPart<RenderPart>().Visible=false;if(fault=="removed")Assert.True(zone.RemoveEntity(owner));if(fault=="foreign-physics")owner.GetPart<PhysicsPart>().ParentEntity=f.Player;if(fault=="foreign-thermal")owner.GetPart<ThermalPart>().ParentEntity=f.Player;if(fault=="foreign-fuel")owner.GetPart<FuelPart>().ParentEntity=f.Player;
                if(fault=="nonfinite-heat")owner.GetPart<ThermalPart>().Temperature=float.NaN;if(fault=="custom-visual")owner.GetPart<RenderPart>().VisualID="custom-owned-coals";if(fault=="unknown-owner")owner.BlueprintName="UnregisteredCoals";if(fault=="foreign-zone")zone=new Zone(zone.ZoneID);if(fault=="foreign-map")f.Manager.WorldMap.Tiles[12,10]=BiomeType.Sodden;
                string model=SpawnRing3DRecipes.Resolve(zone,owner,f.Library.Definition).ModelId;Assert.AreNotEqual(Hot,model,fault);Assert.AreNotEqual(Cooled,model,fault);
            }
        }
        [Test] public void RememberedButUnseenSourceDoesNotSubmitCurrentThermalState()
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {var owner=Add(f);f.Refresh();Exact(f,owner,Hot);var view=f.View(owner);var cell=f.Zone.GetEntityCell(owner);cell.IsVisible=false;Assert.True(cell.Explored);owner.GetPart<ThermalPart>().Temperature=140;f.Refresh(new HashSet<int>());Assert.False(f.Rendered(owner));SpawnRing3DIntegrationFixture.Hidden(view);cell.IsVisible=true;f.Refresh(new HashSet<int>());Exact(f,owner,Cooled);}
        }
        [Test] public void OrdinaryAuthoredCampfireKeepsItsActualExistingModel()
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {Create(f);var fire=f.Add("Campfire");Assert.False(fire.GetPart<CampfirePart>().FiniteCooking);var before=SpawnRing3DRecipes.Resolve(f.Zone,fire,f.Library.Definition);Assert.IsNull(before.Failure);Assert.IsNotEmpty(before.ModelId);fire.GetPart<ThermalPart>().Temperature=25;fire.GetPart<FuelPart>().FuelMass=0;f.Refresh();var after=SpawnRing3DRecipes.Resolve(f.Zone,fire,f.Library.Definition);Assert.AreEqual(before.ModelId,after.ModelId);Assert.AreNotEqual(Hot,after.ModelId);Assert.AreNotEqual(Cooled,after.ModelId);Assert.True(f.Rendered(fire));}
        }
        [Test] public void CurrentSpatialBacklinksCannotBorrowAStaleRegisteredCell()
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var owner=Add(f);f.Refresh();Exact(f,owner,Hot);var cell=f.Zone.GetEntityCell(owner);
                var spatial=typeof(Entity).GetField("SpatialZone",BindingFlags.Instance|BindingFlags.NonPublic);
                Assert.NotNull(spatial);var originalSpatial=spatial.GetValue(owner);var originalCellZone=cell.ParentZone;
                try
                {
                    spatial.SetValue(owner,new Zone(f.Zone.ZoneID));
                    Assert.AreSame(cell,f.Zone.GetEntityCell(owner));Assert.True(cell.Objects.Contains(owner));
                    Assert.AreNotEqual(Hot,SpawnRing3DRecipes.Resolve(f.Zone,owner,f.Library.Definition).ModelId);
                    spatial.SetValue(owner,originalSpatial);cell.ParentZone=new Zone(f.Zone.ZoneID);
                    Assert.AreSame(cell,f.Zone.GetEntityCell(owner));Assert.True(cell.Objects.Contains(owner));
                    Assert.AreNotEqual(Hot,SpawnRing3DRecipes.Resolve(f.Zone,owner,f.Library.Definition).ModelId);
                }
                finally{spatial.SetValue(owner,originalSpatial);cell.ParentZone=originalCellZone;}
                f.Refresh();Exact(f,owner,Hot);
            }
        }
        static Entity Create(SpawnRing3DIntegrationFixture f)
        {
            var owner=f.Factory.CreateEntity(Blueprint);Assert.NotNull(owner,"SOURCE PRECONDITION: real SpreadCookingCoals child must precede art RED.");Assert.AreEqual("*",owner.GetPart<RenderPart>().RenderString);Assert.AreEqual("&K",owner.GetPart<RenderPart>().ColorString);Assert.False(owner.GetPart<PhysicsPart>().Takeable);Assert.False(owner.GetPart<PhysicsPart>().Solid);Assert.True(owner.GetPart<CampfirePart>().FiniteCooking);var rest=typeof(CampfirePart).GetField("AllowRest");Assert.NotNull(rest,"SOURCE PRECONDITION: source's tested no-Rest flag must exist.");Assert.False((bool)rest.GetValue(owner.GetPart<CampfirePart>()));Assert.AreEqual(500,owner.GetPart<ThermalPart>().Temperature);Assert.AreEqual(25,owner.GetPart<FuelPart>().FuelMass);Assert.IsNull(owner.GetPart<LightSourcePart>());Assert.IsNull(owner.GetPart<LightSourceFlickerPart>());Assert.False(owner.HasEffect<BurningEffect>());return owner;
        }
        static Entity Add(SpawnRing3DIntegrationFixture f){var e=Create(f);var p=f.FreeCell();Assert.True(f.Zone.AddEntity(e,p.x,p.y));return e;}
        static void Exact(SpawnRing3DIntegrationFixture f,Entity owner,string expected)
        {
            var recipe=SpawnRing3DRecipes.Resolve(f.Zone,owner,f.Library.Definition);Assert.AreEqual(expected,recipe.ModelId,recipe.Failure);Assert.AreSame(owner,recipe.Owner);Assert.True(recipe.Transient);Assert.False(recipe.Batched);Assert.True(f.Find(owner,out var view,out var actual));Assert.AreEqual(expected,actual);Assert.True(f.Rendered(owner));Assert.True(f.Pick(owner,out _));
            var mesh=Prefab(expected).GetComponent<MeshFilter>().sharedMesh;Assert.AreSame(mesh,view.GetComponent<MeshFilter>().sharedMesh);Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(owner,out var proof),proof.Failure);Assert.AreSame(mesh,proof.ExpectedMesh);Assert.AreSame(ReferenceGladeVoxelLibrary.Load().Material,proof.ExpectedMaterial);Assert.AreEqual(1,proof.PieceCount);
        }
    }
}
