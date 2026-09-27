#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadNativeModelLookupTests
 {
  private static readonly string[] Paths={SpreadEnvironment3DLibrary.ResourcePath,SpreadVisitorCreatureLibrary.ResourcePath,SpreadCreature3DLibrary.ResourcePath,SpreadScenery3DLibrary.ResourcePath,SpreadPortable3DLibrary.ResourcePath,SpreadBiomeHumanoidLibrary.ResourcePath,SpreadBiomeActorLibrary.ResourcePath,PouredLiquid3DLibrary.ResourcePath,ReferenceGladeVoxelLibrary.ResourcePath};
  private static readonly Type[] Types={typeof(SpreadEnvironment3DLibrary),typeof(SpreadVisitorCreatureLibrary),typeof(SpreadCreature3DLibrary),typeof(SpreadScenery3DLibrary),typeof(SpreadPortable3DLibrary),typeof(SpreadBiomeHumanoidLibrary),typeof(SpreadBiomeActorLibrary),typeof(PouredLiquid3DLibrary),typeof(ReferenceGladeVoxelLibrary)};
  [TestCase(0,false)][TestCase(0,true)][TestCase(1,false)][TestCase(1,true)][TestCase(2,false)][TestCase(2,true)]
  [TestCase(3,false)][TestCase(3,true)][TestCase(4,false)][TestCase(4,true)][TestCase(5,false)][TestCase(5,true)]
  [TestCase(6,false)][TestCase(6,true)][TestCase(7,false)][TestCase(7,true)][TestCase(8,false)][TestCase(8,true)]
  public void ExactExtensionRequestsOnlyItsOwningResourceAndReturnsOriginalIdentity(int family,bool prefab)
  {
   var ring=Ring();var row=First(family);
   var spec=(SpawnRing3DCatalog.Model)row.GetType().GetField("Spec").GetValue(row);Assert.NotNull(spec);
   // Poured-liquid entries store ColorCode rather than an Id field; every actual
   // library exposes its validated model identity through the common Spec.
   string id=spec.id;Assert.IsNotEmpty(id);
   if(family==7)Assert.AreEqual(PouredLiquid3DLibrary.ModelId(((PouredLiquid3DLibrary.Entry)row).ColorCode),id);
   object expected=prefab?row.GetType().GetField("Prefab").GetValue(row):(object)spec;Assert.NotNull(expected);
   Assert.AreSame(expected,prefab?(object)ring.FindModel(id):ring.Definition.FindModel(id));
   var requests=new List<string>();
   object actual=Find(ring,prefab,id,(path,type)=>{requests.Add(path);return Resources.Load(path,type);});
   Assert.AreSame(expected,actual);CollectionAssert.AreEqual(new[]{Paths[family]},requests,"A known extension must not load unrelated libraries per owner.");
  }
  [TestCase(false)][TestCase(true)]
  public void NativeBaseModelUsesNoExtensionResource(bool prefab)
  {
   var ring=Ring();string id="ring-player";object expected=prefab?(object)ring.FindModel(id):ring.Definition.FindModel(id);Assert.NotNull(expected);
   int calls=0;Assert.AreSame(expected,Find(ring,prefab,id,(path,type)=>{calls++;throw new AssertionException("Native base attempted extension load.");}));Assert.Zero(calls);
  }
  [TestCase(false,null)][TestCase(true,null)][TestCase(false,"")][TestCase(true,"")]
  public void NullOrEmptyNeverLoads(bool prefab,string id)
  {var ring=Ring();Assert.IsNull(Find(ring,prefab,id,(path,type)=>{throw new AssertionException("Empty ID loaded an asset.");}));}
  [TestCase(false)][TestCase(true)]
  public void UnknownAndWrongCaseRemainUnresolved(bool prefab)
  {
   var ring=Ring();foreach(string id in new[]{"reference-glade-not-a-model-0","REFERENCE-GLADE-GROUND-0","unknown-native-family"})
   {Assert.IsNull(prefab?(object)ring.FindModel(id):ring.Definition.FindModel(id));Assert.IsNull(Find(ring,prefab,id,Resources.Load));}
  }
  [TestCase(false)][TestCase(true)]
  public void PerCallLoaderDoesNotCacheMissingOrPreviousOwnerResults(bool prefab)
  {
   var ring=Ring();var row=First(8);string id=(string)row.GetType().GetField("Id").GetValue(row);object expected=row.GetType().GetField(prefab?"Prefab":"Spec").GetValue(row);
   Assert.IsNull(Find(ring,prefab,id,(path,type)=>null));
   Assert.AreSame(expected,Find(ring,prefab,id,Resources.Load));
   Assert.IsNull(Find(ring,prefab,id,(path,type)=>null));
  }
  [TestCase(false)][TestCase(true)]
  public void OwningLibraryFailureIsNotSilencedOrReplaced(bool prefab)
  {
   var ring=Ring();var row=First(8);string id=(string)row.GetType().GetField("Id").GetValue(row);
   var sentinel=new InvalidOperationException("owned library unavailable");
   var thrown=Assert.Throws<InvalidOperationException>(()=>Find(ring,prefab,id,(path,type)=>{if(path==Paths[8])throw sentinel;return Resources.Load(path,type);}));Assert.AreSame(sentinel,thrown);
  }
  [TestCase(false)][TestCase(true)]
  public void ExistingBaseIndexKeepsPriorityEvenForAnExtensionShapedIdentity(bool prefab)
  {
   const string id="reference-glade-ground-0";
   if(prefab)
   {
    var owned=ScriptableObject.CreateInstance<SpawnRing3DLibrary>();var body=new GameObject("owned priority control");
    try{typeof(SpawnRing3DLibrary).GetField("models",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(owned,new Dictionary<string,GameObject>{{id,body}});
     Assert.AreSame(body,Find(owned,true,id,(path,type)=>{throw new AssertionException("Base collision priority bypassed.");}));}
    finally{Object.DestroyImmediate(body);Object.DestroyImmediate(owned);}
   }
   else
   {
    var catalog=new SpawnRing3DCatalog();var model=new SpawnRing3DCatalog.Model{id=id};
    typeof(SpawnRing3DCatalog).GetField("modelIndex",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(catalog,new Dictionary<string,SpawnRing3DCatalog.Model>{{id,model}});
    Assert.AreSame(model,Invoke(catalog,id,(path,type)=>{throw new AssertionException("Base collision priority bypassed.");}));
   }
  }
  private static object First(int family)
  {var asset=Resources.Load(Paths[family],Types[family]);Assert.NotNull(asset,Paths[family]);var entries=(Array)Types[family].GetField("Entries").GetValue(asset);Assert.Greater(entries.Length,0);return entries.GetValue(0);}
  private static SpawnRing3DLibrary Ring(){var ring=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);Assert.NotNull(ring);ring.Validate();return ring;}
  private static object Find(SpawnRing3DLibrary ring,bool prefab,string id,Func<string,Type,Object> load)=>Invoke(prefab?(object)ring:ring.Definition,id,load);
  private static object Invoke(object target,string id,Func<string,Type,Object> load)
  {
   var method=target.GetType().GetMethod("FindModel",BindingFlags.Instance|BindingFlags.NonPublic,null,new[]{typeof(string),typeof(Func<string,Type,Object>)},null);
   Assert.NotNull(method,"Per-call lookup observation seam required before optimizing resource work.");
   try{return method.Invoke(target,new object[]{id,load});}catch(TargetInvocationException e){throw e.InnerException;}
  }
 }
}
#endif
