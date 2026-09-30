using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class SpreadFieldResidentArtTests
    {
        static string Model(string bp)=>bp=="SpreadSeedKeeper"?"spread-person-seed-keeper":"spread-person-wayside-cook";
        [TestCase("SpreadSeedKeeper","&y")][TestCase("SpreadWaysideCook","&w")]
        public void ExactFriendlyResidentHasOwnCurrentRigWithoutChangingStock(string blueprint,string color)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                Assert.True(f.Factory.Blueprints.ContainsKey(blueprint),"Adopt actual resident content before interpreting art RED.");
                var owner=f.Add(blueprint);var render=owner.GetPart<RenderPart>();Assert.AreEqual("@",render.RenderString);Assert.AreEqual(color,render.ColorString);Assert.True(owner.GetPart<BrainPart>().Passive);
                var inventory=owner.GetPart<InventoryPart>();var stock=inventory.Objects.ToArray();var counts=stock.Select(x=>x.GetPart<StackerPart>()?.StackCount??1).ToArray();var at=f.Zone.GetEntityPosition(owner);
                var native=(SpawnRing3DRecipe)typeof(SpawnRing3DRecipes).GetMethod("ResolveNative",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{f.Zone,owner,f.Library.Definition,null});
                Assert.AreEqual(Model(blueprint),native.ModelId);Assert.Null(native.Failure);
                f.Refresh();Assert.AreEqual(Model(blueprint),SpawnRing3DRecipes.Resolve(f.Zone,owner,f.Library.Definition).ModelId);
                Assert.True(f.Find(owner,out var view,out var id));Assert.AreEqual(Model(blueprint),id);Assert.True(f.Rendered(owner));
                var entry=SpreadBiomeHumanoidLibrary.Load().Find(id);Assert.NotNull(entry);var skin=view.GetComponentInChildren<SkinnedMeshRenderer>();Assert.AreSame(entry.Mesh,skin.sharedMesh);Assert.AreEqual(9,skin.bones.Length);Assert.GreaterOrEqual(skin.sharedMesh.uv.Distinct().Count(),3);
                CollectionAssert.AreEquivalent(new[]{"Idle","Walk","Interact","Attack","Hit"},view.GetComponentInChildren<Animator>().runtimeAnimatorController.animationClips.Select(x=>x.name));
                CollectionAssert.AreEqual(stock,inventory.Objects);CollectionAssert.AreEqual(counts,stock.Select(x=>x.GetPart<StackerPart>()?.StackCount??1));Assert.AreEqual(at,f.Zone.GetEntityPosition(owner));
                render.Visible=false;f.Refresh();Assert.False(f.Rendered(owner));render.Visible=true;f.Refresh();Assert.True(f.Rendered(owner));f.Zone.RemoveEntity(owner);f.Refresh();Assert.False(f.Find(owner,out _,out _));
            }
        }
        [TestCase("SpreadSeedKeeper")][TestCase("SpreadWaysideCook")]
        public void ChangedAppearanceCannotBorrowTheNewResidentIdentity(string blueprint)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {Assert.True(f.Factory.Blueprints.ContainsKey(blueprint));var owner=f.Add(blueprint);owner.GetPart<RenderPart>().RenderString="?";Assert.AreNotEqual(Model(blueprint),SpawnRing3DRecipes.Resolve(f.Zone,owner,f.Library.Definition).ModelId);}
        }
        [TestCase("Farmer")][TestCase("Provisioner")]
        public void ExistingResidentsKeepTheirExistingModels(string blueprint)
        {using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0")){var owner=f.Add(blueprint);var id=SpawnRing3DRecipes.Resolve(f.Zone,owner,f.Library.Definition).ModelId;Assert.NotNull(id);Assert.False(id.Contains("seed-keeper")||id.Contains("wayside-cook"));}}
    }
}
