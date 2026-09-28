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
 /// <summary>Staged actual transactions and imported pose samples; no generated encounter or live scheduler claim.</summary>
 public sealed class SpreadCollectorArtGalleryTests
 {
  [TestCase("Hatchet")][TestCase("Cudgel")][TestCase("LeatherBoots")]
  public void CaptureOriginalBirdAndExactCommittedCargo(string item)
  {
   using(var f=new SpreadCollectorCarryTests.Fixture(item))
   {
    MovePlayerAway(f.F,f.Actor);f.F.Set("FullReveal",true);f.F.Refresh();var capture=new CaptureScope(f.F,item);
    if(item=="Hatchet")foreach(var pose in new[]{"Idle","Walk","Interact","Attack","Hit"})capture.Frame(f.Actor,pose,null);
    f.Pick();Assert.True(f.View(out _));f.Proof();foreach(var pose in item=="Hatchet"?new[]{"Pickup","CarryIdle","CarryWalk"}:new[]{"CarryIdle","CarryWalk"})capture.Frame(f.Actor,pose,f.Item);
    f.Act();Assert.AreEqual("Deposited",f.Phase);Assert.AreSame(f.Item,f.Home.GetPart<ContainerPart>().Contents.Single());f.F.Refresh();Assert.False(f.View(out _));capture.Frame(f.Actor,"Deposit",null);capture.Finish();
   }
  }
  [Test]public void CaptureOriginalAvianRemains()
  {
   using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
   {var e=f.Add("TatterjayCorpse");e.Properties["SourceBlueprint"]="Tatterjay";e.Properties["SourceID"]="explicit-gallery-source";MovePlayerAway(f,e);f.Set("FullReveal",true);f.Refresh();var c=new CaptureScope(f,"remains");c.Frame(e,"remains",null);c.Frame(e,"remains",null,110);c.Finish();}
  }
  static void MovePlayerAway(SpawnRing3DIntegrationFixture f,Entity owner)
  {var at=f.Zone.GetEntityCell(owner);Cell away=null;for(int y=1;y<24&&away==null;y++)for(int x=1;x<79&&away==null;x++)if(Math.Max(Math.Abs(x-at.X),Math.Abs(y-at.Y))>=12&&f.Zone.CanPlaceFootprint(f.Player,x,y))away=f.Zone.GetCell(x,y);Assert.NotNull(away);Assert.True(f.Zone.MoveEntity(f.Player,away.X,away.Y));}
  sealed class CaptureScope
  {
   readonly SpawnRing3DIntegrationFixture f;readonly SpawnRing3DPresenter presenter;readonly Camera camera;readonly RenderTexture oldTarget;readonly string folder,label;
   readonly Dictionary<string,byte[]> bytes;readonly Dictionary<Mesh,Vector3[]> vertices;readonly Dictionary<Mesh,int[]> indices;readonly List<FrameRow> rows=new List<FrameRow>();
   public CaptureScope(SpawnRing3DIntegrationFixture f,string label)
   {
    this.f=f;this.label=label;presenter=(SpawnRing3DPresenter)f.Presenter;camera=presenter.WorldCamera;oldTarget=camera.targetTexture;
    folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/QuestFreeExploration/E3/Collector/NativeArt",label+"-"+Guid.NewGuid().ToString("N")));Directory.CreateDirectory(folder);
    var library=SpreadCollectorArtLibrary.Load();library.Validate();var portable=SpreadPortable3DLibrary.Load();
    var meshes=library.Entries.Select(e=>e.Mesh).Concat(new[]{"Hatchet","Cudgel","LeatherBoots"}.Select(n=>portable.Find("spread-portable-"+n.ToLowerInvariant()).Mesh)).ToArray();
    vertices=meshes.ToDictionary(m=>m,m=>m.vertices);indices=meshes.ToDictionary(m=>m,m=>m.triangles);
    bytes=library.Entries.SelectMany(e=>new[]{AssetDatabase.GetAssetPath(e.Mesh),AssetDatabase.GetAssetPath(e.Prefab)})
     .Concat(new[]{AssetDatabase.GetAssetPath(library.Entries[0].Materials[0]),AssetDatabase.GetAssetPath(portable.Material)})
     .Distinct().ToDictionary(p=>p,p=>File.ReadAllBytes(p));
   }
   public void Frame(Entity owner,string pose,Entity item,float yaw=35)
   {
    Assert.True(f.Find(owner,out var root,out string model));Assert.True(presenter.TryGetApprovedStyle(owner,out var proof),proof.Failure);Assert.AreEqual(1,proof.PieceCount);
    var animator=root.GetComponentInChildren<Animator>();float sample=pose=="Idle"?0:.5f;
    if(animator!=null){var clip=animator.runtimeAnimatorController.animationClips.Single(c=>c.name==pose);clip.SampleAnimation(animator.gameObject,clip.length*sample);}
    if(item!=null){var method=typeof(SpawnRing3DPresenter).GetMethod("TryGetApprovedCollectorCarryStyle");Assert.NotNull(method);var args=new object[]{owner,item,null};Assert.True((bool)method.Invoke(presenter,args));}
    var helper=typeof(SpreadLatchcoilGalleryTests);var flags=BindingFlags.Static|BindingFlags.NonPublic;var bounds=(Bounds)helper.GetMethod("VisibleBounds",flags).Invoke(null,new object[]{root});
    string graph=Graph(f),name=rows.Count.ToString("00")+"-"+pose+".png";var settings=helper.GetMethod("Capture",flags).Invoke(null,new object[]{camera,bounds,root.transform.rotation*Quaternion.Euler(0,yaw,0),Path.Combine(folder,name)});
    Assert.AreEqual(graph,Graph(f));Assert.AreSame(oldTarget,camera.targetTexture);foreach(var m in vertices){CollectionAssert.AreEqual(m.Value,m.Key.vertices);CollectionAssert.AreEqual(indices[m.Key],m.Key.triangles);}foreach(var file in bytes)CollectionAssert.AreEqual(file.Value,File.ReadAllBytes(file.Key),file.Key);
    rows.Add(new FrameRow{image=name,model=model,owner=owner.ID,item=item?.ID,itemBlueprint=item?.BlueprintName,pose=pose,normalizedTime=animator==null?0:sample,yaw=yaw,bounds=bounds,camera=JsonUtility.ToJson(settings)});
   }
   public void Finish()=>File.WriteAllText(Path.Combine(folder,"report.json"),JsonUtility.ToJson(new Report{label=label,zone=f.Zone.ZoneID,frames=rows.ToArray(),canVerify="Factory Tatterjay/actual item/Sack in managed Spread fixture; exact role pickup/deposit transactions; actual carried item model/material and body identity; native imported pose samples; borrowed bytes/buffers unchanged; close-up camera restored.",cannotVerify="Staged art inspection, not generated placement, ordinary discovery, live scheduling, natural corpse chance, player awareness or frame-time performance. Initial source owners/player location and corpse metadata are explicit fixture setup. Pose sampling is not proof of live transition timing. Images still require human/agent visual review."},true));
  }
  static string Graph(SpawnRing3DIntegrationFixture f)=>f.Zone.EntityVersion+"|"+f.Zone.TileState.ToSaveString()+"|"+string.Join(";",f.Zone.GetReadOnlyEntities().Select(e=>e.ID+":"+e.BlueprintName+":"+f.Zone.GetEntityPosition(e)+":"+e.GetStatValue("Hitpoints")+":"+string.Join(",",e.GetPart<InventoryPart>()?.Objects.Select(i=>i.ID+"/"+i.BlueprintName+"/"+(i.GetPart<StackerPart>()?.StackCount??1))??Enumerable.Empty<string>())+":"+string.Join(",",e.GetPart<ContainerPart>()?.Contents.Select(i=>i.ID+"/"+i.BlueprintName+"/"+(i.GetPart<StackerPart>()?.StackCount??1))??Enumerable.Empty<string>())).OrderBy(x=>x));
  [Serializable]sealed class FrameRow{public string image,model,owner,item,itemBlueprint,pose,camera;public float normalizedTime,yaw;public Bounds bounds;}
  [Serializable]sealed class Report{public string label,zone,canVerify,cannotVerify;public FrameRow[] frames;}
 }
}
#endif
