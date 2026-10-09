using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class ReferenceGladeHumanoidFormTests
    {
        internal static Bounds Geometry(SkinnedMeshRenderer skin,string bone=null)
        {
            int index=bone==null?-1:Array.FindIndex(skin.bones,b=>b!=null&&b.name==bone);
            if(bone!=null)Assert.GreaterOrEqual(index,0,bone);
            var vertices=skin.sharedMesh.vertices;var weights=skin.sharedMesh.boneWeights;
            var points=vertices.Select((v,i)=>(v,i)).Where(pair=>index<0||(weights[pair.i].boneIndex0==index&&weights[pair.i].weight0>.99f))
                .Select(pair=>skin.transform.TransformPoint(pair.v)).ToArray();Assert.IsNotEmpty(points,bone);
            var result=new Bounds(points[0],Vector3.zero);foreach(var point in points.Skip(1))result.Encapsulate(point);return result;
        }
        internal static Bounds VisibleBodyBounds(GameObject root)
        {
            var renderers=root.GetComponentsInChildren<Renderer>(true);Assert.IsNotEmpty(renderers);var kit=ReferenceGladeVoxelLibrary.Load();
            Bounds BoundsOf(Renderer renderer)
            {
                if(renderer is SkinnedMeshRenderer skin&&kit.ActorPaints.Any(p=>p.AuthoredGeometry&&p.Painted==skin.sharedMesh))return Geometry(skin);
                return renderer.bounds;
            }
            var result=BoundsOf(renderers[0]);foreach(var renderer in renderers.Skip(1))result.Encapsulate(BoundsOf(renderer));return result;
        }
        [TestCase("Player")][TestCase("Warden")][TestCase("Villager")]
        public void ActualLocalHumanoidHasSmallHeadShortTorsoAndSeparatedLegs(string blueprint)
        {
            using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
            {
                var owner=blueprint=="Player"?f.Player:f.Add(blueprint);f.CleanGear(owner);f.Refresh();var root=f.View(owner);
                var skin=root.GetComponentInChildren<SkinnedMeshRenderer>();var whole=Geometry(skin);var head=Geometry(skin,"Head");var torso=Geometry(skin,"Spine");
                Assert.Less(head.size.x/whole.size.y,.28f,"Large pale hood still dominates the visible body.");
                Assert.Less(head.size.y/whole.size.y,.26f);Assert.Greater((torso.min.y-whole.min.y)/whole.size.y,.30f,"A long coat still covers the legs.");
                var a=Geometry(skin,"Leg.L");var b=Geometry(skin,"Leg.R");if(a.center.x>b.center.x){var swap=a;a=b;b=swap;}
                Assert.Greater(b.min.x-a.max.x,.035f,"Actual leg geometry needs a visible gap, not a stripe painted on a robe.");
                Assert.That(whole.size.y,Is.InRange(1.05f,1.36f));Assert.True(f.Pick(owner,out _));
                var model=SpawnRing3DRecipes.Resolve(f.Zone,owner,f.Library.Definition).ModelId;
                var borrowed=f.Library.FindModel(model).GetComponentInChildren<SkinnedMeshRenderer>();
                CollectionAssert.AreEqual(borrowed.sharedMesh.bindposes,skin.sharedMesh.bindposes);
                var retained=skin.localBounds;retained.Expand(.00002f);var prior=borrowed.localBounds;
                foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1})
                {var corner=prior.center+Vector3.Scale(prior.extents,new Vector3(x,y,z));Assert.True(retained.Contains(corner),"Do not tighten animation culling around the new bind pose.");}
                var pick=root.GetComponent<BoxCollider>();Assert.NotNull(pick);
                Assert.That(pick.bounds.size.y,Is.EqualTo(whole.size.y).Within(.001f),"Picking follows the visible form, not the larger culling envelope.");
                CollectionAssert.AreEqual(borrowed.bones.Select(x=>x.name),skin.bones.Select(x=>x.name));
                for(int i=0;i<skin.bones.Length;i++)Assert.AreEqual(borrowed.bones[i].localPosition,skin.bones[i].localPosition);
            }
        }
        [Test]public void OrdinaryAuthorityUsesTheUnchangedAdoptedBodyAndRecoversItsLocalForm()
        {
            using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
            {
                var source=f.Library.FindModel("ring-player").GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
                var original=Resources.Load<VoxelWorldMeshCatalog>(VoxelWorldMeshCatalog.ResourcePath).Resolve(source);
                var local=f.View(f.Player).GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;Assert.AreNotSame(original,local);
                f.Manager.WorldMap.Tiles[11,10]=BiomeType.Beating;f.Refresh();Assert.AreSame(original,f.View(f.Player).GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh);
                f.Manager.WorldMap.Tiles[11,10]=BiomeType.Spread;f.Refresh();Assert.AreSame(local,f.View(f.Player).GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh);
            }
        }
        [Test]public void LocalFormKeepsActualHeadAndHandEquipmentAndNativeVisibility()
        {
            using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
            {
                f.CleanGear(f.Player);f.Refresh();var root=f.View(f.Player);var skin=root.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=skin.sharedMesh;
                var helmet=f.Equip(f.Player,"IronHelmet");var dagger=f.Equip(f.Player,"Dagger");f.Frame();
                foreach(var item in new[]{helmet,dagger})
                {Assert.True(f.Equipment(f.Player,item,out var gear));Assert.True(gear.transform.IsChildOf(root.transform));Assert.AreSame(f.Player,item.GetPart<PhysicsPart>().Equipped);}
                var cell=f.Zone.GetEntityCell(f.Player);cell.IsVisible=false;f.Refresh();Assert.False(f.Rendered(f.Player));cell.IsVisible=true;f.Refresh();
                Assert.True(f.Rendered(f.Player));Assert.AreSame(mesh,skin.sharedMesh);Assert.True(f.Pick(f.Player,out _));
                Assert.AreEqual(56f,NativeZone3DRenderSurface.CameraPitchDegrees,"Anatomy does not change global projection to disguise covered limbs.");
            }
        }
    }
}
