using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    // Geometric clearance gates follow actual native screenshots. They do not
    // replace the required front/side/walk fit gallery review.
    public sealed class SpreadMarlbackEquipmentFitTests
    {
        [TestCase("MarlbackBreacher")][TestCase("MarlbackScrabbler")]
        public void ActualTorsoArmorClearsTheBroadHorizontalCarapace(string blueprint)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor=f.Add(blueprint);f.CleanGear(actor);f.Refresh();var root=f.View(actor);
                var body=BodySkin(f,actor);var original=body.sharedMesh.vertices;var spine=BoneBounds(body,root,"Spine");var head=BoneBounds(body,root,"Head");
                var item=Equip(f,actor,"RivetedPlate");f.Refresh();var gear=GearBounds(f,actor,item,root);
                Assert.GreaterOrEqual(gear.max.x,spine.max.x+.02f,"Right side must be outside the actual carapace.");
                Assert.LessOrEqual(gear.min.x,spine.min.x-.02f,"Left side must be outside the actual carapace.");
                Assert.GreaterOrEqual(gear.max.y,spine.max.y+.02f,"Upper metal must remain outside the dorsal shale.");
                Assert.GreaterOrEqual(gear.max.z,spine.max.z-.02f,"Cuirass reaches the rear torso.");
                Assert.Greater(gear.min.z,head.min.z+.06f,"The muzzle remains beyond the front armor edge.");
                Assert.Greater(gear.min.y,.1f,"A cuirass cannot enclose the feet or drag below the body.");
                CollectionAssert.AreEqual(original,body.sharedMesh.vertices);
            }
        }
        [TestCase("MarlbackBreacher")][TestCase("MarlbackScrabbler")]
        public void ActualHelmetFitsTheBroadHeadWithoutCoveringTheLowFace(string blueprint)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor=f.Add(blueprint);f.CleanGear(actor);f.Refresh();var root=f.View(actor);var body=BodySkin(f,actor);var head=BoneBounds(body,root,"Head");
                var item=Equip(f,actor,"IronHelmet");f.Refresh();var gear=GearBounds(f,actor,item,root);
                Assert.GreaterOrEqual(gear.max.x,head.max.x+.02f);Assert.LessOrEqual(gear.min.x,head.min.x-.02f);
                Assert.Greater(gear.max.y,head.max.y+.02f);Assert.Greater(gear.min.y,head.center.y+.07f,"Low blunt face remains exposed below the helmet.");
                Assert.Greater(gear.max.z,head.max.z-.04f,"Back rim reaches the upper head.");
            }
        }
        [TestCase("Player")][TestCase("Farmer")]
        public void ExistingHumanAttachmentPositionsRemainExactControls(string blueprint)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor=blueprint=="Player"?f.Player:f.Add(blueprint);f.CleanGear(actor);f.Refresh();var root=f.View(actor);
                var armor=Equip(f,actor,"RivetedPlate");var helmet=Equip(f,actor,"IronHelmet");f.Refresh();
                var presenter=(SpawnRing3DPresenter)f.Presenter;
                foreach(var item in new[]{armor,helmet})
                {
                    Assert.True(presenter.TryGetEquipmentView(actor,item,out var view));var bone=view.GetComponentInChildren<SkinnedMeshRenderer>().bones.Single();
                    var position=root.transform.InverseTransformPoint(bone.position);
                    var expected=item==armor?new Vector3(0,.99f,0):root.transform.InverseTransformPoint(root.GetComponentsInChildren<Transform>().Single(t=>t.name=="Equipment.Head").position);
                    Assert.Less((position-expected).sqrMagnitude,.000001f);
                }
            }
        }
        static Entity Equip(SpawnRing3DIntegrationFixture f,Entity actor,string blueprint)
            =>(Entity)typeof(SpreadEquipmentFitGalleryTests).GetMethod("EquipObserved",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{f,actor,blueprint});
        static SkinnedMeshRenderer BodySkin(SpawnRing3DIntegrationFixture f,Entity actor)
        {
            Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedStyle(actor,out var proof),proof.Failure);
            return f.View(actor).GetComponentsInChildren<SkinnedMeshRenderer>().Single(s=>s.sharedMesh==proof.SubmittedMesh);
        }
        static Bounds BoneBounds(SkinnedMeshRenderer skin,GameObject root,string bone)
        {
            int index=Array.FindIndex(skin.bones,b=>b.name==bone);Assert.GreaterOrEqual(index,0);
            var vertices=skin.sharedMesh.vertices;var weights=skin.sharedMesh.boneWeights;var matrix=root.transform.worldToLocalMatrix*skin.transform.localToWorldMatrix;
            return BoundsOf(vertices.Where((v,i)=>weights[i].boneIndex0==index).Select(matrix.MultiplyPoint3x4).ToArray());
        }
        static Bounds GearBounds(SpawnRing3DIntegrationFixture f,Entity actor,Entity item,GameObject root)
        {
            var presenter=(SpawnRing3DPresenter)f.Presenter;Assert.True(presenter.TryGetApprovedEquipmentStyle(actor,item,out var proof),proof.Failure);
            Assert.True(presenter.TryGetEquipmentView(actor,item,out var view));var skin=view.GetComponentsInChildren<SkinnedMeshRenderer>().Single();
            Assert.AreSame(proof.SubmittedMesh,skin.sharedMesh);Assert.True(skin.sharedMesh.boneWeights.All(w=>w.weight0==1&&w.boneIndex0==0));
            var matrix=root.transform.worldToLocalMatrix*skin.bones.Single().localToWorldMatrix*skin.sharedMesh.bindposes.Single();
            return BoundsOf(skin.sharedMesh.vertices.Select(matrix.MultiplyPoint3x4).ToArray());
        }
        static Bounds BoundsOf(Vector3[] points)
        {Assert.Greater(points.Length,0);var b=new Bounds(points[0],Vector3.zero);foreach(var p in points)b.Encapsulate(p);return b;}
    }
}
