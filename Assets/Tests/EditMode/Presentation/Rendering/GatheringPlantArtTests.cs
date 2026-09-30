using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public sealed class GatheringPlantArtTests
    {
        static SpawnRing3DRecipe Recipe(SpawnRing3DIntegrationFixture f,Entity owner)=>SpawnRing3DRecipes.Resolve(f.Zone,owner,f.Library.Definition);
        static void OwnModel(SpawnRing3DIntegrationFixture f,Entity owner,string family)
        {
            var method=typeof(SpawnRing3DRecipes).GetMethod("ResolveNative",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
            var native=(SpawnRing3DRecipe)method.Invoke(null,new object[]{f.Zone,owner,f.Library.Definition,null});
            Assert.Null(native.Failure);Assert.That(native.ModelId,Does.StartWith("ring-bush-"));Assert.AreSame(owner,native.Owner);
            var recipe=Recipe(f,owner);Assert.AreSame(owner,recipe.Owner);
            Assert.That(recipe.ModelId,Does.StartWith("spread-environment-"+family+"-"));
            var entry=SpreadEnvironment3DLibrary.Load().Find(recipe.ModelId);Assert.NotNull(entry);
            Assert.GreaterOrEqual(entry.Mesh.vertexCount,480);Assert.GreaterOrEqual(entry.Mesh.uv.Distinct().Count(),3);
            Assert.That(entry.Mesh.bounds.size.y,family=="stoneburr"?Is.InRange(.35f,.65f):Is.InRange(.08f,.23f));
            f.Refresh();Assert.True(f.Rendered(owner));
        }
        [TestCase("StoneburrPatch","stoneburr")][TestCase("FrostLichenPatch","frost-lichen")]
        public void ActualGatheringPlantHasItsOwnCurrentVisibleMesh(string blueprint,string family)
        {
            using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
            {
                Assert.True(f.Factory.Blueprints.ContainsKey(blueprint),"Real content must be adopted before interpreting missing-art RED.");
                var plant=f.Add(blueprint);Assert.NotNull(plant.GetPart<HarvestablePart>());OwnModel(f,plant,family);
                plant.GetPart<RenderPart>().Visible=false;f.Refresh();Assert.False(f.Rendered(plant));Assert.Null(Recipe(f,plant).ModelId);
                plant.GetPart<RenderPart>().Visible=true;OwnModel(f,plant,family);
            }
        }
        [TestCase("StoneburrPatch","StoneburrSeed","stoneburr")][TestCase("FrostLichenPatch","FrostLichen","frost-lichen")]
        public void ActualHarvestRemovesPlantViewAndKeepsOriginalGround(string blueprint,string yield,string family)
        {
            using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
            {
                Assert.True(f.Factory.Blueprints.ContainsKey(blueprint));var plant=f.Add(blueprint);f.Approach(plant);OwnModel(f,plant,family);
                var cell=f.Zone.GetEntityCell(plant);var original=cell.Objects.Where(e=>e!=plant).ToArray();Assert.True(original.Length>0);
                var harvest=plant.GetPart<HarvestablePart>();Assert.AreEqual(yield,harvest.YieldBlueprint);Assert.AreEqual(100,harvest.YieldChance);
                var ev=GameEvent.New("InventoryAction");try{ev.SetParameter("Command","Harvest");ev.SetParameter("Actor",f.Player);ev.SetParameter("Zone",f.Zone);plant.FireEvent(ev);}finally{ev.Release();}
                Assert.True(harvest.Harvested);Assert.Null(f.Zone.GetEntityCell(plant));Assert.True(f.Player.GetPart<InventoryPart>().Objects.Any(e=>e.BlueprintName==yield));
                f.Refresh();Assert.False(f.Rendered(plant));Assert.Null(Recipe(f,plant).ModelId);
                foreach(var owner in original)Assert.True(cell.Objects.Contains(owner));
            }
        }
        [TestCase("BerryBush")][TestCase("FlowerField")]
        public void ExistingGatheringAndFlowerModelsKeepTheirIdentity(string blueprint)
        {using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID)){var plant=f.Add(blueprint);var id=Recipe(f,plant).ModelId;Assert.NotNull(id);Assert.False(id.Contains("stoneburr")||id.Contains("frost-lichen"));}}
    }
}
