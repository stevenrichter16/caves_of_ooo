using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class BiomeAffordanceRenderingTests
    {
        [Test] public void SpentStubbleHasFourQuietLowVariantsDistinctFromStandingRipeGrain()
        {
            var kit=SpreadVoxelLibrary.Load();Assert.NotNull(kit);kit.Validate();Assert.AreEqual(20,kit.Entries.Length);
            foreach(int variant in Enumerable.Range(0,4))
            {
                string id=SpreadVoxelLibrary.ModelId("stubble",variant);Assert.AreEqual("spread-stubble-"+variant,id);
                var e=kit.Find(id);Assert.NotNull(e);
                Assert.That(e.Mesh.bounds.max.y,Is.InRange(.035f,.18f));
                Assert.Less(e.Mesh.bounds.max.y,kit.Find(SpreadVoxelLibrary.ModelId("barley",variant)).Mesh.bounds.max.y*.4f);
                Assert.That(e.Mesh.uv.Distinct().Count(),Is.InRange(1,2));Assert.LessOrEqual(e.Mesh.vertexCount,144);
                Assert.LessOrEqual(e.Mesh.bounds.size.x,1);Assert.LessOrEqual(e.Mesh.bounds.size.z,1);
            }
        }
        [Test] public void RipeAndSpentRowsKeepNativeOwnersWhileSelectingDifferentArt()
        {
            var f=GrovelandsCompositionTests.Factory();var z=new Zone(SpreadCompositionTests.Id);
            var catalog=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).Definition;
            var ripe=f.CreateEntity("RipeCropRow");Assert.NotNull(ripe);z.AddEntity(ripe,11,10);
            var spent=f.CreateEntity("CropRow");z.AddEntity(spent,12,10);
            var a=SpawnRing3DRecipes.Resolve(z,ripe,catalog);var b=SpawnRing3DRecipes.Resolve(z,spent,catalog);
            StringAssert.StartsWith("spread-barley-",a.ModelId);StringAssert.StartsWith("spread-stubble-",b.ModelId);
            Assert.AreSame(ripe,a.Owner);Assert.AreSame(spent,b.Owner);
            Assert.IsTrue(a.Batched);Assert.IsTrue(b.Batched);Assert.IsFalse(a.Transient);
            int version=z.EntityVersion;SpawnRing3DRecipes.Resolve(z,ripe,catalog);Assert.AreEqual(version,z.EntityVersion);
        }
        [Test] public void HarvestedNativeStateSelectsStubbleAndDroppedGrainRetainsAnExplicitPortableModel()
        {
            var factory=GrovelandsCompositionTests.Factory();var z=new Zone(SpreadCompositionTests.Id);
            var old=HarvestablePart.Factory;HarvestablePart.Factory=factory;
            try
            {
                var actor=BiomeAffordanceTests.Actor(z,capacity:0);var row=factory.CreateEntity("RipeCropRow");Assert.NotNull(row);z.AddEntity(row,11,10);
                Assert.IsTrue(BiomeAffordanceTests.Act(row,actor,z));
                var catalog=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).Definition;
                StringAssert.StartsWith("spread-stubble-",SpawnRing3DRecipes.Resolve(z,row,catalog).ModelId);
                var grain=z.GetEntityCell(row).Objects.Single(e=>e.BlueprintName=="Emberwheat");
                var recipe=SpawnRing3DRecipes.Resolve(z,grain,catalog);
                Assert.NotNull(recipe.ModelId,recipe.Failure);Assert.NotNull(catalog.FindModel(recipe.ModelId));
                Assert.IsTrue(recipe.Transient);Assert.IsFalse(recipe.Batched);Assert.AreSame(grain,recipe.Owner);
            }
            finally{HarvestablePart.Factory=old;}
        }
        [Test] public void ActualPresenterReplacesOnlyTheHarvestedRowsMeshAndRemovesTheSpentOwnerNormally()
        {
            using(var f=new SpawnRing3DIntegrationFixture(SpreadCompositionTests.Id))
            {
                f.Set("FullReveal",true);var at=f.FreeCell();var row=f.Add("RipeCropRow",at.x,at.y);
                var far=f.FreeCell(at.x/10,at.y/5);var control=f.Add("CropRow",far.x,far.y);
                f.Approach(row);f.Refresh();Assert.IsTrue(f.Rendered(row));Assert.IsTrue(f.Rendered(control));
                Assert.IsTrue(f.Find(row,out _,out string before));StringAssert.StartsWith("spread-barley-",before);
                int localRevision=f.Revision(at.x,at.y),farRevision=f.Revision(far.x,far.y);
                Assert.IsTrue(BiomeAffordanceTests.Act(row,f.Player,f.Zone));
                f.Call("Refresh",null,new HashSet<int>());f.Frame();
                Assert.IsTrue(f.Find(row,out _,out string after));StringAssert.StartsWith("spread-stubble-",after);
                Assert.Greater(f.Revision(at.x,at.y),localRevision);
                Assert.AreEqual(farRevision,f.Revision(far.x,far.y));Assert.IsTrue(f.Rendered(control));
                Assert.AreSame(f.Zone.GetCell(at.x,at.y),f.Zone.GetEntityCell(row));
                Assert.IsTrue(f.Zone.RemoveEntity(row));f.Call("Refresh",null,new HashSet<int>());f.Frame();
                Assert.IsFalse(f.Rendered(row));Assert.IsTrue(f.Rendered(control));Assert.AreEqual(farRevision,f.Revision(far.x,far.y));
            }
        }
        [Test] public void ExistingTwoDimensionalCropArtDistinguishesRipeFromCutWithoutBorrowingUnrelatedEntities()
        {
            var factory=GrovelandsCompositionTests.Factory();var ripe=factory.CreateEntity("RipeCropRow");
            var spent=factory.CreateEntity("CropRow");
            Assert.AreEqual(EnvironmentSpriteRenderer.CropSpriteKind.Emberwheat,EnvironmentSpriteRenderer.ResolveFieldCropKind(ripe));
            Assert.AreEqual(EnvironmentSpriteRenderer.CropSpriteKind.Seed,EnvironmentSpriteRenderer.ResolveFieldCropKind(spent));
            ripe.GetPart<FieldHarvestPart>().Harvested=true;
            Assert.AreEqual(EnvironmentSpriteRenderer.CropSpriteKind.Seed,EnvironmentSpriteRenderer.ResolveFieldCropKind(ripe));
            Assert.AreEqual(EnvironmentSpriteRenderer.CropSpriteKind.None,EnvironmentSpriteRenderer.ResolveFieldCropKind(factory.CreateEntity("Grass")));
            Assert.AreEqual(EnvironmentSpriteRenderer.CropSpriteKind.None,EnvironmentSpriteRenderer.ResolveFieldCropKind(factory.CreateEntity("EmberwheatCrop")));
            Assert.AreEqual(EnvironmentSpriteRenderer.CropSpriteKind.None,EnvironmentSpriteRenderer.ResolveFieldCropKind(null));
        }
        [TestCase(null,0)] [TestCase("unknown",0)] [TestCase("stubble",-1)] [TestCase("stubble",4)]
        public void InvalidFamilyOrVariantCannotSilentlyBorrowAnotherPlant(string family,int variant)
        {Assert.Catch<ArgumentException>(()=>SpreadVoxelLibrary.ModelId(family,variant));}
    }
}
