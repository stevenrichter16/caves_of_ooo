#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using CavesOfOoo.Rendering;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class VoxelWorldPaintTests
    {
        const string Village="Assets/Art3D/Village/Models/";
        const string Ring="Assets/Art3D/SpawnRing/Models/";
        const string VillageAtlas="Assets/Art3D/Village/Textures/VillagePalette.png";
        const string RingAtlas="Assets/Art3D/SpawnRing/Textures/SpawnRingPalette.png";
        static object Call(string name,params object[] args)
        {
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CavesOfOoo.Editor.VoxelWorldPaintSimplifier")).FirstOrDefault(t=>t!=null);
            Assert.NotNull(type,"Ambient paint simplification must be an explicit offline build step.");
            var method=type.GetMethod(name,BindingFlags.Public|BindingFlags.Static);Assert.NotNull(method);
            try{return method.Invoke(null,args);}catch(TargetInvocationException error){throw error.InnerException??error;}
        }
        static Vector2 Center(int index,int columns)=>new Vector2((index%columns+.5f)/columns,(index/columns+.5f)/8);
        static Vector2[] Swatches(int columns)=>Enumerable.Range(0,columns*8).Select(i=>Center(i,columns)).ToArray();
        static Vector2[] Quiet(Vector2[] input,int columns)=> (Vector2[])Call("Consolidate",input,Swatches(columns),columns);

        [TestCase("ground-patch-0.fbx")][TestCase("detail-patch-04-00.fbx")][TestCase("grass-2.fbx")]
        public void ExplicitVillageAmbientModelsAreEligible(string model)
        {
            Assert.AreEqual(8,Call("AtlasColumns",Village+model,VillageAtlas,false));
            Assert.AreEqual(0,Call("AtlasColumns",Village+model,VillageAtlas,true),"Skin usage must veto ambient paint.");
            Assert.AreEqual(0,Call("AtlasColumns",Village+model,RingAtlas,false),"Atlas contracts cannot cross libraries.");
        }
        [TestCase("ring-floor-0.fbx")][TestCase("ring-grass-1.fbx")][TestCase("ring-tepui-stone-3.fbx")]
        public void RingAmbientModelsUseSixteenColumns(string model)
        {Assert.AreEqual(16,Call("AtlasColumns",Ring+model,RingAtlas,false));}
        [TestCase("character-teal.fbx")][TestCase("frog.fbx")][TestCase("equipment-blade.fbx")]
        [TestCase("central-well.fbx")][TestCase("dry-hem-guesthouse-roof.fbx")][TestCase("ground-patch-unknown.fbx")]
        public void ActorsGearPropsAndUnknownModelsRetainTheirPaint(string model)
        {Assert.AreEqual(0,Call("AtlasColumns",Village+model,VillageAtlas,false));}
        [Test] public void ForeignPathsAndPilotGroundStayOutsideTheKnownAtlasContract()
        {
            Assert.AreEqual(0,Call("AtlasColumns","Assets/Other/ground-patch-0.fbx",VillageAtlas,false));
            Assert.AreEqual(0,Call("AtlasColumns","Assets/Art3D/MultiCellPilot/Models/PilotGround_0.fbx","Assets/Art3D/MultiCellPilot/Textures/PilotGroundAlbedo.png",false));
        }
        [TestCase(8)][TestCase(16)]
        public void QuietPaintPreservesSemanticSwatchesWhileConsolidatingThreeBrightGroundVariants(int columns)
        {
            var input=Swatches(columns);var original=input.ToArray();var output=Quiet(input,columns);
            for(int i=0;i<input.Length;i++)
            {
                int expected=i==2?1:i==37?36:i==61?60:i;
                Assert.AreEqual(Center(expected,columns),output[i],"Swatch "+i);
            }
            CollectionAssert.AreEqual(original,input,"Borrowed input UVs stay immutable.");
            CollectionAssert.AreEqual(output,Quiet(output,columns),"Regeneration is idempotent.");
        }
        [TestCase(8)][TestCase(16)]
        public void VariationWithinOneMaterialConvergesButNeighboringMaterialsRemainDistinct(int columns)
        {
            var input=new[]{Center(4,columns)+new Vector2(.03f/columns,.02f),Center(4,columns)-new Vector2(.03f/columns,.02f),Center(6,columns)};
            var result=Quiet(input,columns);Assert.AreEqual(result[0],result[1]);Assert.AreNotEqual(result[1],result[2]);
        }
        [TestCase(float.NaN)][TestCase(float.PositiveInfinity)][TestCase(-.01f)][TestCase(1.01f)]
        public void InvalidUvRejectsBeforePublishingAnyReplacement(float x)
        {
            var input=new[]{Center(0,8),new Vector2(x,.5f)};var copy=input.ToArray();
            Assert.Throws<ArgumentException>(()=>Quiet(input,8));Assert.AreEqual(copy[0],input[0]);Assert.AreEqual(copy[1].y,input[1].y);
            Assert.AreEqual(BitConverter.SingleToInt32Bits(copy[1].x),BitConverter.SingleToInt32Bits(input[1].x));
        }
        [Test] public void RepresentativeTexelTracksMeanPaintRatherThanAnIsolatedBrightPixel()
        {
            var pixels=Enumerable.Repeat(new Color32(100,110,60,255),16*16).ToArray();pixels[0]=new Color32(255,255,255,255);
            var result=(Vector2[])Call("RepresentativeUvs",pixels,16,16,8);
            Assert.AreEqual(64,result.Length);var first=result[0];int index=(int)(first.y*16)*16+(int)(first.x*16);
            Assert.AreNotEqual(0,index);Assert.AreEqual(new Color32(100,110,60,255),pixels[index]);
            for(int i=0;i<64;i++)Assert.AreEqual(i,Mathf.FloorToInt(result[i].y*8)*8+Mathf.FloorToInt(result[i].x*8));
        }
        [Test] public void UnreplacedAmbientAssetsKeepEveryGeometryChannelAndFinalPaintStaysBounded()
        {
            var catalog=Resources.Load<VoxelWorldMeshCatalog>(VoxelWorldMeshCatalog.ResourcePath);Assert.NotNull(catalog);
            var village=Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath);
            var ring=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
            var renderers=village.Models.Select(m=>m.Prefab).Concat(ring.Models.Select(m=>m.Prefab))
                .SelectMany(p=>p.GetComponentsInChildren<Renderer>(true)).ToArray();
            int changed=0, coarse=0, generic=0;
            foreach(var binding in catalog.Bindings)
            {
                string path=AssetDatabase.GetAssetPath(binding.Source);
                string atlas=path.StartsWith(Village,StringComparison.Ordinal)?VillageAtlas:RingAtlas;
                int columns=(int)Call("AtlasColumns",path,atlas,binding.Source.bindposeCount>0);if(columns==0)continue;
                var uses=renderers.Where(r=>(r is SkinnedMeshRenderer s?s.sharedMesh:r.GetComponent<MeshFilter>()?.sharedMesh)==binding.Source).ToArray();
                Assert.Greater(uses.Length,0,path);
                bool excluded=uses.Any(r=>r is SkinnedMeshRenderer||r.sharedMaterials.Length!=1||!r.sharedMaterial.HasProperty("_BaseMap")
                    ||AssetDatabase.GetAssetPath(r.sharedMaterial.GetTexture("_BaseMap"))!=atlas);
                if(MorrowfastCoarseArtContract.IsVerified(binding))
                {
                    // These exact authored Village bindings deliberately replace
                    // surface-voxel geometry. Their native masks, imported frame,
                    // and shape budgets are asserted by MorrowfastCoarseVoxelTests.
                    // Verify ownership and the final color contract here rather
                    // than pretending this is still a UV-only mesh replacement.
                    Assert.AreEqual(8,columns,path); Assert.IsTrue(path.StartsWith(Village,StringComparison.Ordinal));
                    if(excluded)
                        Assert.IsTrue(uses.All(r=>r.sharedMaterials.Length==1
                            &&r.sharedMaterial.shader.name=="CavesOfOoo/Village3D/Water"
                            &&r.sharedMaterial.GetTexture("_BaseMap")==null),path+" only the exact native water sibling may lack the palette");
                    Assert.That(binding.Voxel.uv.Distinct().Count(),Is.InRange(1,2),path+" final bounded paint vocabulary");
                    coarse++; continue;
                }
                generic++;
                var plain=VoxelWorldMeshBaker.Bake(binding.Source,binding.VoxelSize);
                try
                {
                    CollectionAssert.AreEqual(plain.vertices,binding.Voxel.vertices,path+" vertices");
                    CollectionAssert.AreEqual(plain.normals,binding.Voxel.normals,path+" normals");
                    CollectionAssert.AreEqual(plain.triangles,binding.Voxel.triangles,path+" topology");
                    Assert.AreEqual(plain.bounds,binding.Voxel.bounds,path+" bounds");
                    Assert.AreEqual(plain.subMeshCount,binding.Voxel.subMeshCount,path+" material slots");
                    if(excluded)
                    {CollectionAssert.AreEqual(plain.uv,binding.Voxel.uv,path+" excluded water/material sibling");continue;}
                    // The later P1 pass reduces the complete object's paint to
                    // two colors. S1's pure swatch contract remains tested above;
                    // the final-art contract intentionally supersedes exact S1 UVs.
                    Assert.LessOrEqual(binding.Voxel.uv.Distinct().Count(),2,path+" final bounded paint vocabulary");
                    if(!plain.uv.SequenceEqual(binding.Voxel.uv))changed++;
                }
                finally{UnityEngine.Object.DestroyImmediate(plain);}
            }
            Assert.AreEqual(53,coarse,"Only the 48 explicit Village ambient models plus five native water siblings use authored geometry.");
            Assert.AreEqual(12,generic,"Every ring floor, grass and tepui-stone variant retains the complete UV-only geometry contract.");
            Assert.Greater(changed,0,"The remaining real ring ambient assets must still receive the paint simplification.");
        }
        [TestCase(false)][TestCase(true)]
        public void SharedMeshWithForeignMaterialUsageIsExcludedInEitherEncounterOrder(bool excludedFirst)
        {
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("CavesOfOoo.Editor.VoxelWorldMeshBuilder")).First(t=>t!=null);
            var sourceType=type.GetNestedType("Source",BindingFlags.NonPublic);Assert.NotNull(sourceType);
            var dictionaryType=typeof(System.Collections.Generic.Dictionary<,>).MakeGenericType(typeof(Mesh),sourceType);
            var dictionary=(System.Collections.IDictionary)Activator.CreateInstance(dictionaryType);
            var add=type.GetMethod("Add",BindingFlags.Static|BindingFlags.NonPublic);Assert.NotNull(add);
            var prefab=Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath).FindModel("ground-patch-0");
            var filter=prefab.GetComponentInChildren<MeshFilter>();var material=filter.GetComponent<MeshRenderer>().sharedMaterial;
            var owned=new GameObject("paint mixed usage countercheck");var renderer=owned.AddComponent<MeshRenderer>();var foreign=new Material(material);
            foreign.SetTexture("_BaseMap",Texture2D.whiteTexture);
            try
            {
                // Positive shared-use control requires the exact eligible texture.
                renderer.sharedMaterial=material;add.Invoke(null,new object[]{filter.sharedMesh,renderer,false,dictionary});
                var field=sourceType.GetField("PaintColumns");Assert.AreEqual(8,field.GetValue(dictionary[filter.sharedMesh]));
                dictionary.Clear();
                foreach(bool excluded in new[]{excludedFirst,!excludedFirst})
                {renderer.sharedMaterial=excluded?foreign:material;add.Invoke(null,new object[]{filter.sharedMesh,renderer,false,dictionary});}
                Assert.AreEqual(0,field.GetValue(dictionary[filter.sharedMesh]),"A later valid use cannot reactivate a vetoed shared mesh.");
            }
            finally{UnityEngine.Object.DestroyImmediate(owned);UnityEngine.Object.DestroyImmediate(foreign);}
        }
    }
}
#endif
