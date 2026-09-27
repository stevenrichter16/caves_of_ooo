using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace CavesOfOoo.Tests
{
    public sealed class ReferenceGladeContactTests
    {
        const string Field="_GroundContact",Strength="_GroundContactStrength";
        static Material Owned(SpawnRing3DIntegrationFixture f)=>f.Get<NativeZone3DRenderSurface>("ActiveSurface").MaterialFor(ReferenceGladeVoxelLibrary.Load().Material);
        static Texture2D Mask(Material material)
        {Assert.True(material.HasProperty(Field));Assert.True(material.HasProperty(Strength));var value=material.GetTexture(Field) as Texture2D;Assert.NotNull(value);Assert.AreEqual(640,value.width);Assert.AreEqual(200,value.height);return value;}
        static Entity IsolatedPlant(SpawnRing3DIntegrationFixture f)
        {
            foreach(var e in f.Zone.GetReadOnlyEntities().ToArray())if(e!=f.Player)f.Zone.RemoveEntity(e);
            f.Set("FullReveal",false);for(int y=0;y<25;y++)for(int x=0;x<80;x++){f.Zone.GetCell(x,y).IsVisible=true;f.Zone.GetCell(x,y).Explored=true;}
            var plant=f.Factory.CreateEntity("Reeds");plant.GetPart<RenderPart>().VisualID="reference-glade-pale-reeds";Assert.True(f.Zone.AddEntity(plant,20,10));f.Refresh();return plant;
        }
        static float Around(Texture2D map,int x,int y)
        {float maximum=0;for(int py=(24-y)*8-4;py<(25-y)*8+4;py++)for(int px=x*8-4;px<(x+1)*8+4;px++)if(px>=0&&px<map.width&&py>=0&&py<map.height)maximum=Mathf.Max(maximum,map.GetPixel(px,py).r);return maximum;}
        [TestCase("broad-base",0)][TestCase("raised-face",0)][TestCase("grounded-stem",1)]
        public void MeshExtractionRejectsFlatFloorAndElevatedGeometry(string shape,int expected)
        {
            var type=typeof(SpawnRing3DPresenter).Assembly.GetType("CavesOfOoo.Rendering.ReferenceGladeGroundContact");Assert.NotNull(type);
            var extract=type.GetMethod("ExtractFootprints",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public);Assert.NotNull(extract);
            var primitive=GameObject.CreatePrimitive(PrimitiveType.Cube);Mesh owned=null;
            try
            {
                owned=Object.Instantiate(primitive.GetComponent<MeshFilter>().sharedMesh);
                Vector3 size=shape=="broad-base"?new Vector3(1,.03f,1):new Vector3(.12f,.06f,.12f);
                Vector3 centre=new Vector3(0,shape=="raised-face"?.6f:size.y/2,0);
                var vertices=owned.vertices;for(int i=0;i<vertices.Length;i++)vertices[i]=Vector3.Scale(vertices[i],size)+centre;
                owned.vertices=vertices;owned.RecalculateBounds();
                var before=owned.vertices;var footprints=extract.Invoke(null,new object[]{owned}) as Array;Assert.NotNull(footprints);
                Assert.AreEqual(expected,footprints.Length,shape);CollectionAssert.AreEqual(before,owned.vertices);
            }
            finally{if(owned!=null)Object.DestroyImmediate(owned);Object.DestroyImmediate(primitive);}
        }
        [TestCase("move")][TestCase("remove")][TestCase("hide")][TestCase("foreign")]
        public void CurrentGeometryContactClearsWhenItsActualOwnerChanges(string change)
        {
            using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
            {
                var e=IsolatedPlant(f);var material=Owned(f);var mask=Mask(material);Assert.Greater(Around(mask,20,10),.3f);Assert.Less(Around(mask,24,10),.001f);
                if(change=="move")Assert.True(f.Zone.MoveEntity(e,24,10));
                else if(change=="hide")f.Zone.GetEntityCell(e).IsVisible=false;
                else {f.Zone.RemoveEntity(e);if(change=="foreign")Assert.True(new Zone("foreign").AddEntity(e,20,10));}
                f.Refresh();Assert.AreSame(mask,Mask(material));Assert.Less(Around(mask,20,10),.001f,"No ghost contact may remain at the old owner cell.");
                if(change=="move")Assert.Greater(Around(mask,24,10),.3f);
            }
        }
        [Test]public void RealMeshContactIsBoundedAndDoesNotAlterBorrowedGeometryOrNativeState()
        {
            var library=ReferenceGladeVoxelLibrary.Load();var meshes=library.Entries.Select(e=>e.Mesh).ToArray();var vertices=meshes.Select(m=>m.vertices).ToArray();
            using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
            {
                var e=IsolatedPlant(f);var material=Owned(f);var mask=Mask(material);var pixels=mask.GetPixels32();
                Assert.Greater(pixels.Count(c=>c.r>0),8);Assert.Less(pixels.Count(c=>c.r>0),256,"One real reed clump must not shade distant cells.");
                Assert.True(pixels.Any(c=>c.r>0&&c.r<240),"A bounded penumbra must contain intermediate levels.");
                int version=f.Zone.EntityVersion;var position=f.Zone.GetEntityPosition(e);string tiles=f.Zone.TileState.ToSaveString();f.Refresh();
                CollectionAssert.AreEqual(pixels,mask.GetPixels32());Assert.AreEqual(version,f.Zone.EntityVersion);Assert.AreEqual(position,f.Zone.GetEntityPosition(e));Assert.AreEqual(tiles,f.Zone.TileState.ToSaveString());
                Assert.AreNotSame(library.Material,material);Assert.AreEqual(0,library.Material.GetFloat(Strength));
            }
            for(int i=0;i<meshes.Length;i++)CollectionAssert.AreEqual(vertices[i],meshes[i].vertices);
        }
        [Test]public void LowDetailAuthorityLossAndDisposalReleaseOnlyOwnedContactState()
        {
            Texture2D retained=null;Material owned=null;
            using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
            {
                IsolatedPlant(f);owned=Owned(f);retained=Mask(owned);Assert.Greater(owned.GetFloat(Strength),0);
                Village3DSettings.LowDetail=true;f.Frame();Assert.AreEqual(0,owned.GetFloat(Strength));
                Village3DSettings.LowDetail=false;f.Frame();Assert.Greater(owned.GetFloat(Strength),0);Assert.AreSame(retained,Mask(owned));
                f.Manager.WorldMap.Tiles[11,10]=BiomeType.Sodden;f.Refresh();Assert.True(retained==null);Assert.True(owned==null);
                var normal=f.Get<NativeZone3DRenderSurface>("ActiveSurface").MaterialFor(f.Library.WorldMaterial);Assert.AreEqual(0,normal.GetFloat(Strength));
                f.Manager.WorldMap.Tiles[11,10]=BiomeType.Spread;f.Refresh();owned=Owned(f);retained=Mask(owned);Assert.Greater(owned.GetFloat(Strength),0);
            }
            Assert.True(retained==null);Assert.True(owned==null);Assert.NotNull(ReferenceGladeVoxelLibrary.Load().Material);
        }
        [Test]public void OrdinaryZoneAndBorrowedMaterialsKeepZeroContactStrength()
        {
            var source=ReferenceGladeVoxelLibrary.Load().Material;
            Assert.AreEqual(0,source.HasProperty(Strength)?source.GetFloat(Strength):0);
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var owned=f.Get<NativeZone3DRenderSurface>("ActiveSurface").MaterialFor(f.Library.WorldMaterial);
                Assert.AreEqual(0,owned.HasProperty(Strength)?owned.GetFloat(Strength):0);
                Assert.AreEqual(0,f.Library.WorldMaterial.HasProperty(Strength)?f.Library.WorldMaterial.GetFloat(Strength):0);
            }
        }
        [TestCase("live")][TestCase("raised")][TestCase("vertical")][TestCase("transient")][TestCase("remembered")][TestCase("hidden")]
        public void ActualGpuContactChangesOnlyLiveGroundNearTheFootprint(string mode)
        {
            var plain=Draw(mode,0);var enabled=Draw(mode,.38f);
            if(mode=="hidden"){Assert.Less(plain.Max(c=>c.r),.001f);Assert.Less(enabled.Max(c=>c.r),.001f);return;}
            Assert.Greater(plain.Average(c=>c.r),.05f,"A black/empty fragment is not a passing control.");
            float maximum=plain.Zip(enabled,(a,b)=>Mathf.Abs(a.r-b.r)).Max();
            if(mode=="live")
            {
                Assert.Greater(maximum,.02f,"The real shader must darken ground under the owned footprint.");
                Assert.Less(Mathf.Abs(plain[0].r-enabled[0].r),.001f,"Far ground stays unchanged.");
                Assert.LessOrEqual(enabled.Max(c=>c.r),plain.Max(c=>c.r)+.001f);
            }
            else Assert.Less(maximum,.001f,mode);
        }
        [TestCase(0)][TestCase(1)][TestCase(2)][TestCase(3)]
        public void ContactMatchesActualImportedMeshAtItsNativePositionAndQuarterTurn(int quarter)
        {
            using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
            {
                var owner=IsolatedPlant(f);owner.SetIntProperty("ReferenceGladeQuarterTurns",quarter);f.Refresh();
                var recipe=SpawnRing3DRecipes.Resolve(f.Zone,owner,f.Library.Definition);Assert.AreEqual(quarter,recipe.QuarterTurns);
                var source=ReferenceGladeVoxelLibrary.Load().Find(recipe.ModelId).Mesh;
                var vertices=source.vertices;var before=source.vertices;var rotation=Quaternion.Euler(0,quarter*90,0);
                var packed=new float[vertices.Length*3];
                // Independent native transform of actual source vertices catches
                // mirrored yaw, wrong cell origin and an implicit scale in integration.
                for(int i=0;i<vertices.Length;i++)
                {var v=rotation*vertices[i]+recipe.Position;packed[3*i]=v.x;packed[3*i+1]=v.y;packed[3*i+2]=v.z;}
                var shapes=ReferenceGladeContactGeometry.Extract(packed,source.triangles);Assert.Greater(shapes.Length,0);
                var expected=new byte[640*200];Assert.True(ReferenceGladeContactGeometry.Rasterize(shapes.Select(x=>new ReferenceGladeContactGeometry.Placed(x,0,0)).ToArray(),expected));
                var actual=Mask(Owned(f)).GetPixels32();Assert.AreEqual(expected.Length,actual.Length);
                for(int i=0;i<actual.Length;i++)Assert.LessOrEqual(Math.Abs(actual[i].r-expected[i]),2,"Native placed contact pixel "+i);
                CollectionAssert.AreEqual(before,source.vertices);Assert.AreEqual((20,10),f.Zone.GetEntityPosition(owner));
            }
        }
        static Color[] Draw(string mode,float strength)
        {
            var shader=Shader.Find("Hidden/CavesOfOoo/Tests/ReferenceGladeContactProbe");Assert.NotNull(shader);Assert.True(shader.isSupported);
            var material=new Material(shader);var fog=new Texture2D(80,25,TextureFormat.RGBA32,false,true);var mask=new Texture2D(640,200,TextureFormat.RGBA32,false,true){filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var target=new RenderTexture(128,128,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear){memorylessMode=RenderTextureMemoryless.None};Texture2D readback=null;var previous=RenderTexture.active;
            string[] globals={"unity_SHAr","unity_SHAg","unity_SHAb","unity_SHBr","unity_SHBg","unity_SHBb","unity_SHC","_MainLightColor"};var old=globals.Select(Shader.GetGlobalVector).ToArray();
            try
            {
                for(int i=0;i<globals.Length;i++)Shader.SetGlobalVector(globals[i],i<3?new Vector4(0,0,0,.6f):Vector4.zero);
                float visible=mode=="hidden"?0:mode=="remembered"?.5f:1;fog.SetPixels(Enumerable.Repeat(new Color(1,1,1,visible),2000).ToArray());fog.Apply();fog.filterMode=FilterMode.Point;
                var samples=new Color[640*200];for(int y=0;y<200;y++)for(int x=0;x<640;x++){float d=Vector2.Distance(new Vector2((x+.5f)/8,(y+.5f)/8),new Vector2(18,13));samples[y*640+x]=new Color(Mathf.Clamp01(1-d/.7f),0,0,1);}mask.SetPixels(samples);mask.Apply();
                material.SetTexture("_FogLight",fog);material.SetTexture("_BaseMap",Texture2D.whiteTexture);material.SetTexture(Field,mask);material.SetFloat(Strength,strength);
                material.SetFloat("_ProbeHeight",mode=="raised"?.25f:0);material.SetVector("_ProbeNormal",mode=="vertical"?Vector3.right:Vector3.up);material.SetFloat("_Transient",mode=="transient"?1:0);
                Assert.True(target.Create());RenderTexture.active=target;GL.Clear(false,true,Color.black);Graphics.Blit(Texture2D.whiteTexture,target,material,0);
                RenderTexture.active=target;readback=new Texture2D(128,128,TextureFormat.RGBA32,false,true);readback.ReadPixels(new Rect(0,0,128,128),0,0);readback.Apply();return readback.GetPixels();
            }
            finally
            {for(int i=0;i<globals.Length;i++)Shader.SetGlobalVector(globals[i],old[i]);RenderTexture.active=previous;if(readback!=null)Object.DestroyImmediate(readback);target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(mask);Object.DestroyImmediate(fog);Object.DestroyImmediate(material);}
        }
    }
}
