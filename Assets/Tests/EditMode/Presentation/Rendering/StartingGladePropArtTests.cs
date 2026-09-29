using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class StartingGladePropArtTests
    {
        static SpawnRing3DRecipe Resolve(SpawnRing3DIntegrationFixture f,Entity e)=>SpawnRing3DRecipes.Resolve(f.Zone,e,f.Library.Definition);
        static Mesh MeshFor(SpawnRing3DRecipe r,string family)
        {
            Assert.That(r.ModelId,Does.StartWith("reference-glade-"+family+"-"));
            var e=ReferenceGladeVoxelLibrary.Load().Find(r.ModelId);Assert.NotNull(e);return e.Mesh;
        }
        [TestCase(500f,"cooking-fire")][TestCase(25f,"cooled-fire")]
        public void CurrentFireHasStonesLogsAndThermallyTruthfulCoals(float heat,string family)
        {
            using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
            {
                var e=f.Add("Campfire",42,8);e.GetPart<CampfirePart>().FiniteCooking=true;e.GetPart<ThermalPart>().Temperature=heat;
                var mesh=MeshFor(Resolve(f,e),family);
                Assert.GreaterOrEqual(mesh.vertexCount,240,"Separated stones and crossed logs must not be one slab.");
                Assert.GreaterOrEqual(mesh.uv.Distinct().Count(),4,"Stone, char, wood and coal faces remain distinct.");
                Assert.That(mesh.bounds.size.x,Is.InRange(.65f,1f));Assert.That(mesh.bounds.size.y,Is.InRange(.18f,.7f));
                e.GetPart<RenderPart>().Visible=false;Assert.Null(Resolve(f,e).ModelId);
            }
        }
        [Test] public void EmptyFuelDoesNotPretendPhysicallyHotCoalsAreCold()
        {
            using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
            {
                var e=f.Add("Campfire",42,8);e.GetPart<CampfirePart>().FiniteCooking=true;e.GetPart<FuelPart>().FuelMass=0;
                e.GetPart<ThermalPart>().Temperature=500;var hot=Resolve(f,e);MeshFor(hot,"cooking-fire");
                e.GetPart<ThermalPart>().Temperature=25;MeshFor(Resolve(f,e),"cooled-fire");
            }
        }
        [Test] public void ActualHauledTimberKeepsDetailedShapeAndOwner()
        {
            using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
            {
                var beam=f.Zone.GetReadOnlyEntities().Single(e=>e.BlueprintName=="FallenBeam");Assert.True(f.Zone.MoveEntity(f.Player,27,19));var before=Resolve(f,beam);var mesh=MeshFor(before,"fallen-beam");
                Assert.Greater(mesh.bounds.size.x,mesh.bounds.size.z*2);Assert.GreaterOrEqual(mesh.vertexCount,144);
                Assert.GreaterOrEqual(mesh.uv.Distinct().Count(),3,"Broad grain, split end and char must survive the palette.");
                Assert.AreEqual(DragVerdict.Ok,DragSystem.TryGrab(f.Player,beam,f.Zone));
                try{Assert.True(MovementSystem.TryMove(f.Player,f.Zone,1,0));var after=Resolve(f,beam);Assert.AreSame(beam,after.Owner);Assert.AreEqual(before.ModelId,after.ModelId);Assert.AreNotEqual(before.Position,after.Position);}
                finally{DragSystem.Release(f.Player);}
            }
        }
        [TestCase("Campfire")][TestCase("FallenBeam")]
        public void OtherNativeRegionsKeepTheirExistingModel(string blueprint)
        {using(var f=new SpawnRing3DIntegrationFixture()){var e=f.Add(blueprint);Assert.That(Resolve(f,e).ModelId,Does.StartWith("ring-"));}}
    }
}
