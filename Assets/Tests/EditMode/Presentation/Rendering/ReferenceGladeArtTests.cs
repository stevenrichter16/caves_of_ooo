using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class ReferenceGladeArtTests
    {
        static readonly string[] Families={"ground","pale-reeds","green-grass","dark-ruin","low-wall","lit-wall","gravel","chest","barrel","mushroom-ring","cooking-fire","cooled-fire","fallen-beam"};
        static Type KitType(){var t=typeof(SpawnRing3DLibrary).Assembly.GetType("CavesOfOoo.Rendering.ReferenceGladeVoxelLibrary");Assert.NotNull(t,"Reference glade art library must exist.");return t;}
        static object Load(){var value=Resources.Load("ReferenceGlade3D/Library",KitType());Assert.NotNull(value,"Import actual reference kit before GREEN.");KitType().GetMethod("Validate").Invoke(value,null);return value;}
        [Test]public void NativeKitContainsExactlyFiftyTwoMeasuredModelsWithPrivatePalette()
        {
            var kit=Load();var entries=(Array)KitType().GetField("Entries").GetValue(kit);Assert.AreEqual(52,entries.Length);
            var material=(Material)KitType().GetField("Material").GetValue(kit);Assert.NotNull(material);Assert.AreNotSame(Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).WorldMaterial,material);
            Assert.IsTrue(material.HasProperty("_FogLight"));Assert.IsTrue(material.HasProperty("_Transient"));Assert.NotNull(material.GetTexture("_BaseMap"));
            foreach(string family in Families)for(int i=0;i<4;i++)
            {
                string id="reference-glade-"+family+"-"+i;var entry=KitType().GetMethod("Find").Invoke(kit,new object[]{id});Assert.NotNull(entry,id);
                var type=entry.GetType();var mesh=(Mesh)type.GetField("Mesh").GetValue(entry);var prefab=(GameObject)type.GetField("Prefab").GetValue(entry);var spec=(SpawnRing3DCatalog.Model)type.GetField("Spec").GetValue(entry);
                Assert.AreSame(mesh,prefab.GetComponent<MeshFilter>().sharedMesh);Assert.AreSame(material,prefab.GetComponent<MeshRenderer>().sharedMaterial);
                Assert.Greater(mesh.vertexCount,0);Assert.AreEqual(mesh.bounds.size,spec.boundsSize);Assert.AreEqual((int)mesh.GetIndexCount(0)/3,spec.triangles);
                Assert.IsEmpty(prefab.GetComponentsInChildren<Collider>(true));Assert.IsEmpty(prefab.GetComponentsInChildren<MonoBehaviour>(true));
            }
            Assert.Null(KitType().GetMethod("Find").Invoke(kit,new object[]{"ring-player"}));
        }
        // Third native visual review replaces low hedge mats with upright0.40–0.50 sprouts.
        [TestCase("ground",.005f,.10f)][TestCase("pale-reeds",.40f,.95f)][TestCase("green-grass",.40f,.50f)]
        [TestCase("dark-ruin",.35f,.9f)][TestCase("low-wall",.35f,.75f)][TestCase("lit-wall",.35f,.75f)][TestCase("gravel",.02f,.16f)]
        public void ArtScaleMatchesLowReferenceHierarchy(string family,float min,float max)
        {
            var kit=Load();for(int i=0;i<4;i++){var e=KitType().GetMethod("Find").Invoke(kit,new object[]{"reference-glade-"+family+"-"+i});var mesh=(Mesh)e.GetType().GetField("Mesh").GetValue(e);Assert.That(mesh.bounds.size.y,Is.InRange(min-0.000001f,max+0.000001f),"One-microcell tolerance covers native float bounds recomputation.");Assert.LessOrEqual(mesh.bounds.size.x,1.01f);Assert.LessOrEqual(mesh.bounds.size.z,1.01f);}
        }
        [TestCase("Grass","ground")][TestCase("Reeds","pale-reeds")][TestCase("Bush","green-grass")]
        [TestCase("Wall","dark-ruin")][TestCase("BrokenColumn","dark-ruin")][TestCase("Wall","low-wall")]
        [TestCase("Wall","lit-wall")][TestCase("GlowQuartzVein","lit-wall")][TestCase("Rubble","gravel")]
        public void ExactNativeVisualIdentityAndWrongBlueprintCounter(string blueprint,string family)
        {
            var e=new Entity{BlueprintName=blueprint};var render=new RenderPart{RenderString="?",VisualID="reference-glade-"+family};e.AddPart(render);
            var method=KitType().GetMethod("Family");Assert.NotNull(method);Assert.AreEqual(family,method.Invoke(null,new object[]{e}));
            e.BlueprintName="Chest";Assert.Null(method.Invoke(null,new object[]{e}));e.BlueprintName=blueprint;render.VisualID="reference-glade-missing";Assert.Null(method.Invoke(null,new object[]{e}));
        }
        [TestCase("Grass","ground")][TestCase("Reeds","pale-reeds")][TestCase("Bush","green-grass")]
        [TestCase("Wall","dark-ruin")][TestCase("Wall","low-wall")][TestCase("Wall","lit-wall")][TestCase("Rubble","gravel")]
        public void RealGladeRecipePreservesOwnerAndCurrentVisibility(string blueprint,string family)
        {
            Load();using(var f=new SpawnRing3DIntegrationFixture("Overworld.11.10.0"))
            {
                var entity=f.Add(blueprint,20,10);var render=entity.GetPart<RenderPart>();render.VisualID="reference-glade-"+family;int version=f.Zone.EntityVersion;
                var recipe=SpawnRing3DRecipes.Resolve(f.Zone,entity,f.Library.Definition);Assert.That(recipe.ModelId,Does.StartWith("reference-glade-"+family+"-"));Assert.AreSame(entity,recipe.Owner);Assert.AreEqual(version,f.Zone.EntityVersion);Assert.IsTrue(recipe.Batched);
                f.Refresh();Assert.IsTrue(f.Get<bool>("IsReady"),f.Get<string>("Failure"));
                render.Visible=false;Assert.Null(SpawnRing3DRecipes.Resolve(f.Zone,entity,f.Library.Definition).ModelId);render.Visible=true;
                render.VisualID="";Assert.IsNull(ReferenceGladeVoxelLibrary.Family(entity),"Clearing the marker removes explicit authored glade identity.");
                var other=SpawnRing3DRecipes.Resolve(f.Zone,entity,f.Library.Definition).ModelId;
                // C15 intentionally reuses the approved kit for ordinary native
                // terrain throughout managed Spread; it does not retain the marker.
                if(blueprint=="Rubble")Assert.IsFalse(other!=null&&other.StartsWith("reference-glade-"),"Unmodeled native rubble keeps its existing terminal fallback when the explicit marker is removed.");
                else StringAssert.StartsWith("reference-glade-"+(blueprint=="Wall"?"low-wall":family)+"-",other);
                render.VisualID="reference-glade-"+family;
                var originalBiome=f.Manager.WorldMap.Tiles[11,10];
                try
                {
                    f.Manager.WorldMap.Tiles[11,10]=BiomeType.Sodden;
                    var foreign=SpawnRing3DRecipes.Resolve(f.Zone,entity,f.Library.Definition).ModelId;
                    Assert.IsFalse(foreign!=null&&foreign.StartsWith("reference-glade-"),"A marker cannot authorize the kit outside the current map scope.");
                }
                finally{f.Manager.WorldMap.Tiles[11,10]=originalBiome;}
                Assert.AreEqual(recipe.ModelId,SpawnRing3DRecipes.Resolve(f.Zone,entity,f.Library.Definition).ModelId,"Restored authority restores the exact authored recipe.");
                f.Zone.RemoveEntity(entity);Assert.Null(SpawnRing3DRecipes.Resolve(f.Zone,entity,f.Library.Definition).ModelId);
            }
        }
    }
}
