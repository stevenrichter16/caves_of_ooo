using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class ReferenceGladeArtAdversarialTests
    {
        [TestCase("Grass","ground")][TestCase("Reeds","pale-reeds")][TestCase("Bush","green-grass")]
        [TestCase("Wall","dark-ruin")][TestCase("Wall","low-wall")][TestCase("Wall","lit-wall")][TestCase("Rubble","gravel")]
        public void IdenticalVisualIdOutsideActualGladeCannotSelectGladeArt(string blueprint,string family)
        {
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var e=f.Add(blueprint,20,10);e.GetPart<RenderPart>().VisualID="reference-glade-"+family;int version=f.Zone.EntityVersion;
                var result=SpawnRing3DRecipes.Resolve(f.Zone,e,f.Library.Definition);Assert.IsFalse(result.ModelId!=null&&result.ModelId.StartsWith("reference-glade-"));Assert.AreEqual(version,f.Zone.EntityVersion);
            }
        }
        [TestCase("ground")][TestCase("pale-reeds")][TestCase("green-grass")][TestCase("dark-ruin")][TestCase("low-wall")][TestCase("lit-wall")][TestCase("gravel")]
        public void ModelIdentifiersAreCachedAndVariantBoundsReject(string family)
        {
            Assert.AreSame(ReferenceGladeVoxelLibrary.ModelId(family,2),ReferenceGladeVoxelLibrary.ModelId(family,2));
            Assert.Throws<ArgumentOutOfRangeException>(()=>ReferenceGladeVoxelLibrary.ModelId(family,-1));
            Assert.Throws<ArgumentOutOfRangeException>(()=>ReferenceGladeVoxelLibrary.ModelId(family,4));
        }
        [Test]public void UnknownVisualFamilyAndChangedManagedBiomeRemainUnprofiled()
        {
            Assert.Throws<ArgumentException>(()=>ReferenceGladeVoxelLibrary.ModelId("unknown",0));Assert.Null(ReferenceGladeVoxelLibrary.Family(null));
            using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
            {
                var entity=f.Add("Reeds",20,10);entity.GetPart<RenderPart>().VisualID="reference-glade-pale-reeds";
                Assert.That(SpawnRing3DRecipes.Resolve(f.Zone,entity,f.Library.Definition).ModelId,Does.StartWith("reference-glade-"));
                f.Manager.WorldMap.Tiles[11,10]=BiomeType.Sodden;Assert.IsFalse(ReferenceGladePlan.IsActive(f.Zone));
                var result=SpawnRing3DRecipes.Resolve(f.Zone,entity,f.Library.Definition);Assert.IsFalse(result.ModelId!=null&&result.ModelId.StartsWith("reference-glade-"));
            }
        }
        [Test]public void RemovedGroundCannotBeReplacedWithInventedGladeOrFallbackFloor()
        {
            using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
            {
                var root=f.Presenter.GetComponent<SpawnRing3DPresenter>().ActiveSurface.ContentRoot;
                Assert.IsTrue(GroundCovers(root,new Vector2(40.5f,12.5f)),"Positive control: real ground exists before removal.");
                var cell=f.Zone.GetCell(40,12);foreach(var e in cell.Objects.ToArray())f.Zone.RemoveEntity(e);
                f.Refresh();int revision=f.Revision(40,12);f.Refresh();Assert.AreEqual(revision,f.Revision(40,12),"Unchanged glade patches are retained.");Assert.IsEmpty(cell.Objects);
                Assert.IsFalse(GroundCovers(root,new Vector2(40.5f,12.5f)),"Removed native ground leaves a real mesh hole.");
                // Native geometry remains bounded to changed patches, not one object per cube.
                Assert.Less(root.GetComponentsInChildren<MeshFilter>(true).Length,300);
            }
        }
        static bool GroundCovers(Transform root,Vector2 p)
        {
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh=filter.sharedMesh;if(mesh==null||!mesh.isReadable)continue;var v=mesh.vertices;var t=mesh.triangles;
                for(int i=0;i<t.Length;i+=3)
                {
                    var a=filter.transform.TransformPoint(v[t[i]]);var b=filter.transform.TransformPoint(v[t[i+1]]);var c=filter.transform.TransformPoint(v[t[i+2]]);
                    if(Mathf.Abs(a.y)>.04f||Mathf.Abs(b.y)>.04f||Mathf.Abs(c.y)>.04f)continue;
                    Vector2 aa=new Vector2(a.x,a.z),bb=new Vector2(b.x,b.z),cc=new Vector2(c.x,c.z);
                    float area=Cross(bb-aa,cc-aa);if(Mathf.Abs(area)<.00001f)continue;
                    float u=Cross(bb-p,cc-p)/area,w=Cross(cc-p,aa-p)/area,z=1-u-w;
                    if(u>=0&&w>=0&&z>=0)return true;
                }
            }
            return false;
        }
        static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
    }
}
