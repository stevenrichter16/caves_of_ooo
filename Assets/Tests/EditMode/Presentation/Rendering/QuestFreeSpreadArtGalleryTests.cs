#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    /// <summary>Explicit staged native form images. Imported poses are sampled, not live feeding or acquired corpse evidence.</summary>
    public sealed class QuestFreeSpreadArtGalleryTests
    {
        [TestCase("ReedbackGrazer")][TestCase("SpreadDrawPoint")][TestCase("ReedbackGrazerCorpse")]
        public void CaptureOriginalFormsWithActualOwnedMaterials(string blueprint)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var owner=f.Add(blueprint);var at=f.Zone.GetEntityCell(owner);
                if(blueprint=="ReedbackGrazerCorpse"){owner.Properties["SourceBlueprint"]="ReedbackGrazer";owner.Properties["SourceID"]="staged-gallery-source";}
                Cell away=null;for(int y=1;y<24&&away==null;y++)for(int x=1;x<79&&away==null;x++)
                    if(Math.Max(Math.Abs(x-at.X),Math.Abs(y-at.Y))>=12&&f.Zone.CanPlaceFootprint(f.Player,x,y))away=f.Zone.GetCell(x,y);
                Assert.NotNull(away);Assert.True(f.Zone.MoveEntity(f.Player,away.X,away.Y));f.Set("FullReveal",true);f.Refresh();
                var library=QuestFreeSpreadArtLibrary.Load();library.Validate();
                var sourceVertices=library.Entries.ToDictionary(e=>e.Id,e=>e.Mesh.vertices);var sourceIndices=library.Entries.ToDictionary(e=>e.Id,e=>e.Mesh.triangles);
                var sourceBytes=library.Entries.SelectMany(e=>new[]{AssetDatabase.GetAssetPath(e.Mesh),AssetDatabase.GetAssetPath(e.Prefab)})
                    .Concat(new[]{AssetDatabase.GetAssetPath(ReferenceGladeVoxelLibrary.Load().Material),AssetDatabase.GetAssetPath(PouredLiquid3DLibrary.Load().Find(PouredLiquid3DLibrary.ModelId("&c")).Material)})
                    .Distinct().ToDictionary(p=>p,p=>File.ReadAllBytes(p));
                var presenter=(SpawnRing3DPresenter)f.Presenter;var camera=presenter.WorldCamera;var oldTarget=camera.targetTexture;
                string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/QuestFreeExploration/E2/NativeArt",blueprint+"-"+Guid.NewGuid().ToString("N")));Directory.CreateDirectory(folder);
                var rows=new List<Frame>();var poses=blueprint=="ReedbackGrazer"?new[]{"Idle","Idle","Idle","Walk","Interact","Attack","Hit"}:blueprint=="SpreadDrawPoint"?new[]{"full3","partial1","empty0"}:new[]{"remains","remains"};
                for(int i=0;i<poses.Length;i++)
                {
                    if(blueprint=="SpreadDrawPoint"){owner.GetPart<LiquidPoolPart>().Volume=i==0?3:i==1?1:0;f.Refresh();}
                    Assert.True(f.Find(owner,out var root,out string model));Assert.True(presenter.TryGetApprovedStyle(owner,out var proof),proof.Failure);
                    var animator=root.GetComponentInChildren<Animator>();float t=poses[i]=="Idle"?0:.5f;
                    if(animator!=null){var clip=animator.runtimeAnimatorController.animationClips.Single(c=>c.name==poses[i]);clip.SampleAnimation(animator.gameObject,clip.length*t);}
                    string before=Graph(f);float yaw=i==1&&blueprint!="SpreadDrawPoint"?90:i==2&&animator!=null?180:35;
                    var helper=typeof(SpreadLatchcoilGalleryTests);var flags=BindingFlags.NonPublic|BindingFlags.Static;
                    var bounds=(Bounds)helper.GetMethod("VisibleBounds",flags).Invoke(null,new object[]{root});
                    string name=i.ToString("00")+"-"+poses[i]+".png";
                    var settings=helper.GetMethod("Capture",flags).Invoke(null,new object[]{camera,bounds,root.transform.rotation*Quaternion.Euler(0,yaw,0),Path.Combine(folder,name)});
                    Assert.AreEqual(before,Graph(f));Assert.AreSame(oldTarget,camera.targetTexture);
                    foreach(var entry in library.Entries){CollectionAssert.AreEqual(sourceVertices[entry.Id],entry.Mesh.vertices);CollectionAssert.AreEqual(sourceIndices[entry.Id],entry.Mesh.triangles);}
                    foreach(var file in sourceBytes)CollectionAssert.AreEqual(file.Value,File.ReadAllBytes(file.Key),file.Key);
                    rows.Add(new Frame{image=name,model=model,owner=owner.ID,pose=poses[i],normalizedTime=animator==null?0:t,yaw=yaw,pieces=proof.PieceCount,bounds=bounds,camera=JsonUtility.ToJson(settings)});
                }
                File.WriteAllText(Path.Combine(folder,"report.json"),JsonUtility.ToJson(new Report{blueprint=blueprint,zone=f.Zone.ZoneID,frames=rows.ToArray(),canVerify="Factory owners in actual managed Spread fixture; exact approved persistent meshes/all material pieces; native imported clips sampled; current volume3/1/0 selects same finite draw owner; source file bytes and mesh buffers unchanged; owned close-up camera restored.",cannotVerify="Explicit staged art, not generated feature placement, ordinary acquisition, natural70% corpse chance, actual feeding callback/animation transition, survival or player awareness. Corpse metadata and draw volume are fixture setup. The unrelated fixture player is repositioned before observation. Images require visual review; fitted bounds do not exclude occlusion."},true));
            }
        }
        static string Graph(SpawnRing3DIntegrationFixture f)=>f.Zone.EntityVersion+"|"+f.Zone.TileState.ToSaveString()+"|"+string.Join(";",f.Zone.GetReadOnlyEntities().Select(e=>e.ID+":"+e.BlueprintName+":"+f.Zone.GetEntityPosition(e)+":"+e.GetStatValue("Hitpoints")+":"+(e.GetPart<LiquidPoolPart>()?.Volume??-1)).OrderBy(x=>x));
        [Serializable]sealed class Frame{public string image,model,owner,pose,camera;public float normalizedTime,yaw;public int pieces;public Bounds bounds;}
        [Serializable]sealed class Report{public string blueprint,zone,canVerify,cannotVerify;public Frame[] frames;}
    }
}
#endif
