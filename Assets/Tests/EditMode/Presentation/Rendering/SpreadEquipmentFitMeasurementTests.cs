#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace CavesOfOoo.Tests
{
    // Read-only geometry measurements after explicit native fixture equip.
    // These report actual asset coordinates; they do not assert visual acceptance.
    public sealed class SpreadEquipmentFitMeasurementTests
    {
        [TestCase("Player")][TestCase("MarlbackBreacher")][TestCase("MarlbackScrabbler")]
        [TestCase("Farmer")][TestCase("Scribe")][TestCase("Elder")]
        public void MeasureAdoptedBodyBonesSocketsAndActualWornPieces(string blueprint)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor=blueprint=="Player"?f.Player:f.Add(blueprint);f.CleanGear(actor);f.Set("FullReveal",true);f.Refresh();
                var presenter=(SpawnRing3DPresenter)f.Presenter;var root=f.View(actor);Assert.True(presenter.TryGetApprovedStyle(actor,out var proof),proof.Failure);
                var body=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(x=>x.sharedMesh==proof.SubmittedMesh);
                var mesh=body.sharedMesh;var vertices=mesh.vertices;var weights=mesh.boneWeights;var matrix=root.transform.worldToLocalMatrix*body.transform.localToWorldMatrix;
                var bones=new List<BoundRow>();
                for(int i=0;i<body.bones.Length;i++)
                {
                    var points=vertices.Where((v,j)=>weights[j].boneIndex0==i).Select(matrix.MultiplyPoint3x4).ToArray();
                    if(points.Length==0)continue;var bounds=BoundsOf(points);var bind=root.transform.worldToLocalMatrix*body.transform.localToWorldMatrix*mesh.bindposes[i].inverse;
                    bones.Add(new BoundRow{name=body.bones[i].name,min=bounds.min,max=bounds.max,count=points.Length,bindPosition=bind.MultiplyPoint3x4(Vector3.zero)});
                }
                var sockets=root.GetComponentsInChildren<Transform>(true).Where(x=>x.name.StartsWith("Equipment.",StringComparison.Ordinal))
                    .Select(x=>new TransformRow{name=x.name,position=root.transform.InverseTransformPoint(x.position),rotation=(Quaternion.Inverse(root.transform.rotation)*x.rotation).eulerAngles,scale=x.lossyScale}).ToArray();
                var equip=typeof(SpreadEquipmentFitGalleryTests).GetMethod("EquipObserved",BindingFlags.Static|BindingFlags.NonPublic);Assert.NotNull(equip);
                var items=new[]{"RivetedPlate","IronHelmet","IronshodBoots","LeatherGloves"}.Select(n=>(Entity)equip.Invoke(null,new object[]{f,actor,n})).ToArray();f.Refresh();
                var pieces=new List<PieceRow>();var baked=new Mesh();
                try
                {
                    foreach(var item in items)
                    {
                        Assert.True(presenter.TryGetApprovedEquipmentStyle(actor,item,out var evidence),evidence.Failure);
                        Assert.True(presenter.TryGetEquipmentView(actor,item,out var view));
                        foreach(var skin in view.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                        {
                            var target=skin.bones.Single();
                            var local=root.transform.worldToLocalMatrix*skin.transform.localToWorldMatrix;
                            skin.BakeMesh(baked);var defaultBake=BoundsOf(baked.vertices.Select(local.MultiplyPoint3x4));
                            skin.BakeMesh(baked,true);var scaledBake=BoundsOf(baked.vertices.Select(local.MultiplyPoint3x4));
                            // Actual rigid skinning world equation: boneWorld * bindpose * vertex.
                            // Unlike BakeMesh overload defaults this has no hidden scale convention.
                            var rigid=root.transform.worldToLocalMatrix*target.localToWorldMatrix*skin.sharedMesh.bindposes.Single();
                            var bounds=BoundsOf(skin.sharedMesh.vertices.Select(rigid.MultiplyPoint3x4));
                            Assert.True(skin.sharedMesh.boneWeights.All(w=>w.boneIndex0==0&&w.weight0==1&&w.weight1==0&&w.weight2==0&&w.weight3==0));
                            pieces.Add(new PieceRow{blueprint=item.BlueprintName,model=evidence.ModelId,min=bounds.min,max=bounds.max,
                                defaultBakeMin=defaultBake.min,defaultBakeMax=defaultBake.max,useScaleBakeMin=scaledBake.min,useScaleBakeMax=scaledBake.max,
                                target=new TransformRow{name=target.name,position=root.transform.InverseTransformPoint(target.position),rotation=(Quaternion.Inverse(root.transform.rotation)*target.rotation).eulerAngles,scale=target.lossyScale}});
                        }
                    }
                }
                finally{Object.DestroyImmediate(baked);}
                CollectionAssert.AreEqual(vertices,mesh.vertices);CollectionAssert.AreEqual(weights,mesh.boneWeights);
                string directory=Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/DensityCompletion/SpreadBiome/Equipment/Measurements"));Directory.CreateDirectory(directory);
                File.WriteAllText(Path.Combine(directory,blueprint+".json"),JsonUtility.ToJson(new Report{blueprint=blueprint,bodyModel=proof.ModelId,bodyMesh=mesh.name,
                    scope="Actual adopted body bind vertices grouped by assigned bone in actor-root coordinates. Worn min/max use actual rigid boneWorld*bindpose*vertex in actor-local coordinates; both BakeMesh conventions are recorded separately to expose scale effects. Sockets use the current native view. Explicit fixture gear; no gameplay/fit claim.",rootScale=root.transform.lossyScale,bones=bones.ToArray(),sockets=sockets,pieces=pieces.ToArray()},true));
                Assert.AreEqual(9,body.bones.Length);Assert.GreaterOrEqual(bones.Count,8);Assert.AreEqual(6,pieces.Count);
            }
        }
        static Bounds BoundsOf(IEnumerable<Vector3> points)
        {bool first=true;var b=new Bounds();foreach(var p in points){if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);}Assert.False(first);return b;}
        [Serializable]sealed class BoundRow{public string name;public Vector3 min,max,bindPosition;public int count;}
        [Serializable]sealed class TransformRow{public string name;public Vector3 position,rotation,scale;}
        [Serializable]sealed class PieceRow{public string blueprint,model;public Vector3 min,max,defaultBakeMin,defaultBakeMax,useScaleBakeMin,useScaleBakeMax;public TransformRow target;}
        [Serializable]sealed class Report{public string blueprint,bodyModel,bodyMesh,scope;public Vector3 rootScale;public BoundRow[]bones;public TransformRow[]sockets;public PieceRow[]pieces;}
    }
}
#endif
