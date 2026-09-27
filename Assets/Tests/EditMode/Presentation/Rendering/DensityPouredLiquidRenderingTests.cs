#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class DensityPouredLiquidRenderingTests
    {
        Dictionary<string,LiquidDefinition> registry,saved;
        FieldInfo initialized;bool wasInitialized;
        [SetUp]public void SetUp()
        {
            registry=(Dictionary<string,LiquidDefinition>)typeof(LiquidRegistry).GetField("_byId",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
            saved=new Dictionary<string,LiquidDefinition>(registry);
            initialized=typeof(LiquidRegistry).GetField("_initialized",BindingFlags.NonPublic|BindingFlags.Static);wasInitialized=(bool)initialized.GetValue(null);
            try
            {
                LiquidRegistry.InitializeFromJsonSources(Resources.LoadAll<TextAsset>("Content/Data/LiquidDefinitions").Select(x=>x.text));
                Assert.AreEqual(26,LiquidRegistry.Count,"Explicit current definition census, not an empty enumeration.");
            }
            catch{RestoreRegistry();throw;}
        }
        [TearDown]public void TearDown()=>RestoreRegistry();
        void RestoreRegistry(){registry.Clear();foreach(var pair in saved)registry.Add(pair.Key,pair.Value);initialized.SetValue(null,wasInitialized);}
        static Entity Poured(string liquid,int volume=4)
        {
            var owner=new Entity{ID=Guid.NewGuid().ToString("N"),BlueprintName="PouredLiquidPool"};
            owner.AddPart(new RenderPart{DisplayName="poured liquid",RenderString="~",RenderLayer=0});
            owner.AddPart(new PhysicsPart());owner.AddPart(new LiquidPoolPart{LiquidId=liquid,Volume=volume});
            return owner;
        }
        static SpawnRing3DLibrary Library()=>Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
        static SpawnRing3DRecipe Recipe(Zone zone,Entity owner)=>SpawnRing3DRecipes.Resolve(zone,owner,Library().Definition);
        [Test]public void EveryActualRegisteredLiquidHasItsExactColorAndCachedFlatNativeAsset()
        {
            var zone=new Zone("Overworld.4.6.0");var lib=Library();var original=lib.WorldMaterial.GetColor("_BaseColor");
            var shared=StillleafVoxelLibrary.Load().Find(StillleafVoxelLibrary.ModelId("spring",0)).Mesh;var vertices=shared.vertices;var uv=shared.uv;
            var primary=new Dictionary<string,string>();var materialsByColor=new Dictionary<string,Material>();
            foreach(var definition in registry.Values.OrderBy(x=>x.Id))
            {
                var owner=Poured(definition.Id);Assert.True(zone.AddEntity(owner,20,10));int version=zone.EntityVersion;
                var recipe=Recipe(zone,owner);Assert.NotNull(recipe.ModelId,definition.Id+": "+recipe.Failure);Assert.AreSame(owner,recipe.Owner);
                Assert.False(recipe.Transient);Assert.False(recipe.Batched);Assert.AreEqual(Village3DProjection.CellCentre(20,10),recipe.Position);
                var prefab=lib.FindModel(recipe.ModelId);Assert.NotNull(prefab);Assert.AreSame(prefab,lib.FindModel(recipe.ModelId));
                Assert.AreSame(shared,prefab.GetComponent<MeshFilter>().sharedMesh);
                var material=prefab.GetComponent<MeshRenderer>().sharedMaterial;
                Assert.AreNotSame(lib.WorldMaterial,material);Assert.AreEqual(lib.WorldMaterial.shader,material.shader);
                var expectedColor = QudColorParser.Parse(definition.Color);
                var actualColor = material.GetColor("_BaseColor");
                // Native serialized material0.33 reads0.329999983; compare each
                // component within float precision, retaining the exact swatch.
                for (int component = 0; component < 4; component++)
                    Assert.AreEqual(expectedColor[component], actualColor[component], 1e-6f,
                        definition.Id + " color component " + component);
                if(materialsByColor.TryGetValue(definition.Color,out var previous))Assert.AreSame(previous,material,"Same canonical color shares one cached source.");
                else materialsByColor.Add(definition.Color,material);
                Assert.That(AssetDatabase.GetAssetPath(material),Does.StartWith("Assets/Resources/PouredLiquid3D/"));
                var texture=(Texture2D)material.GetTexture("_BaseMap");Assert.AreEqual(Color.white,texture.GetPixel(0,0));
                Assert.IsEmpty(prefab.GetComponentsInChildren<Collider>(true));Assert.IsEmpty(prefab.GetComponentsInChildren<MonoBehaviour>(true));
                Assert.AreEqual(4,owner.GetPart<LiquidPoolPart>().Volume);Assert.AreEqual(version,zone.EntityVersion);
                if(definition.Id=="water"||definition.Id=="oil"||definition.Id=="acid")primary.Add(definition.Id,recipe.ModelId);
                zone.RemoveEntity(owner);
            }
            Assert.AreEqual(12,materialsByColor.Count);Assert.AreEqual(3,primary.Values.Distinct().Count());Assert.AreEqual(original,lib.WorldMaterial.GetColor("_BaseColor"));
            CollectionAssert.AreEqual(vertices,shared.vertices);CollectionAssert.AreEqual(uv,shared.uv);
        }
        [TestCase("unknown")][TestCase("empty-id")][TestCase("empty")][TestCase("negative")]
        [TestCase("takeable")][TestCase("carried")][TestCase("equipped")][TestCase("no-physics")]
        [TestCase("no-pool")][TestCase("hidden")][TestCase("foreign")][TestCase("removed")]
        [TestCase("glyph")][TestCase("color")][TestCase("visual")][TestCase("variant")][TestCase("glyph-variants")]
        [TestCase("registry-uninitialized")][TestCase("invalid-color")][TestCase("item-tag")][TestCase("borrowed-pool-part")]
        public void UnsupportedOwnerNeverBorrowsWaterOrAnotherLiquidModel(string mode)
        {
            var zone=new Zone("Overworld.4.6.0");var owner=Poured("water");Assert.True(zone.AddEntity(owner,20,10));
            var pool=owner.GetPart<LiquidPoolPart>();var physics=owner.GetPart<PhysicsPart>();var render=owner.GetPart<RenderPart>();
            switch(mode)
            {
                case "unknown":pool.LiquidId="made-up";break;case "empty-id":pool.LiquidId="";break;
                case "empty":pool.Volume=0;break;case "negative":pool.Volume=-1;break;
                case "takeable":physics.Takeable=true;break;case "carried":physics.InInventory=new Entity();break;case "equipped":physics.Equipped=new Entity();break;
                case "no-physics":owner.RemovePart(physics);break;case "no-pool":owner.RemovePart(pool);break;
                case "hidden":render.Visible=false;break;case "foreign":owner.BlueprintName="NotPouredLiquidPool";break;case "removed":zone.RemoveEntity(owner);break;
                case "glyph":render.RenderString="@";break;case "color":render.ColorString="&R";break;
                case "visual":render.VisualID="foreign";break;case "variant":render.VisualVariant="foreign";break;
                case "glyph-variants":render.GlyphVariants="@.";break;case "registry-uninitialized":LiquidRegistry.ResetForTests();break;
                case "invalid-color":registry["water"].Color="&?";pool.Initialize();break;
                case "item-tag":owner.Tags["Item"]="";break;case "borrowed-pool-part":pool.ParentEntity=new Entity();break;
            }
            int version=zone.EntityVersion;string id=pool.LiquidId;int volume=pool.Volume;
            Assert.IsNull(Recipe(zone,owner).ModelId,mode);Assert.AreEqual(version,zone.EntityVersion);Assert.AreEqual(id,pool.LiquidId);Assert.AreEqual(volume,pool.Volume);
        }
        [Test]public void ActualViewSwitchesLiquidThenReleasesOnEmptyHiddenAndRemovedWithoutChangingTheOwner()
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.4.6.0"))
            {
                var owner=Poured("water");var point=f.FreeCell();Assert.True(f.Zone.AddEntity(owner,point.x,point.y));f.Refresh();
                Assert.True(f.Authored(owner));Assert.True(f.Rendered(owner));Assert.True(f.Find(owner,out var first,out string water));
                var part=owner.GetPart<LiquidPoolPart>();part.LiquidId="acid";part.Initialize();f.Refresh(f.Dirty(owner));
                Assert.True(f.Find(owner,out var second,out string acid));Assert.AreNotEqual(water,acid);Assert.AreNotSame(first,second);SpawnRing3DIntegrationFixture.Hidden(first);
                Assert.True(f.Pick(owner,out _));Assert.AreEqual(0,f.Get<int>("VoxelMissingMeshCount"));
                var render=owner.GetPart<RenderPart>();render.Visible=false;f.Refresh(f.Dirty(owner));Assert.False(f.Authored(owner));SpawnRing3DIntegrationFixture.Hidden(second);
                render.Visible=true;f.Refresh(f.Dirty(owner));Assert.True(f.Rendered(owner));
                part.Volume=0;f.Refresh(f.Dirty(owner));Assert.False(f.Authored(owner));
                part.Volume=4;f.Refresh(f.Dirty(owner));Assert.True(f.Find(owner,out var last,out _));
                f.Zone.RemoveEntity(owner);f.Refresh();Assert.False(f.Authored(owner));SpawnRing3DIntegrationFixture.Hidden(last);
                Assert.AreEqual("acid",part.LiquidId);Assert.AreEqual(4,part.Volume);
            }
        }
        [TestCase("AcidPool","density-pool-acid")][TestCase("OilSeep","ring-tar-seep-")][TestCase("ConvalescencePool","stillleaf-spring-")]
        public void ExistingNaturalBlueprintKeepsItsOwnAuthoredArt(string blueprint,string prefix)
        {
            var factory=GrovelandsCompositionTests.Factory();var owner=factory.CreateEntity(blueprint);var zone=new Zone("Overworld.4.6.0");zone.AddEntity(owner,20,10);
            Assert.That(Recipe(zone,owner).ModelId,Does.StartWith(prefix));
        }
    }
}
#endif
