using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests {
 public sealed class TorchReactionStateProbe {
  [TestCase("empty",49.7f,1f)]
  [TestCase("organic",49.475f,1.5f)]
  [TestCase("all-shipped",49.475f,1.5f)]
  public void OriginalFixtureDependsOnReactionRegistry(string registry,float fuel,float intensity) {
   var type=typeof(MaterialReactionResolver); const BindingFlags f=BindingFlags.Static|BindingFlags.NonPublic;
   var listField=type.GetField("_reactions",f);var initializedField=type.GetField("_initialized",f);
   var original=listField.GetValue(null);var rows=(IList)original;var old=rows.Cast<object>().ToArray();var initialized=initializedField.GetValue(null);var factory=MaterialReactionResolver.Factory;
   try {
    var path=Path.Combine(Application.dataPath,"Resources/Content/Data/MaterialReactions");
    if(registry=="empty")MaterialReactionResolver.Initialize("{\"Reactions\":[]}");
    else if(registry=="organic")MaterialReactionResolver.Initialize(File.ReadAllText(Path.Combine(path,"fire_plus_organic.json")));
    else MaterialReactionResolver.InitializeFromJsonSources(Directory.GetFiles(path,"*.json").OrderBy(x=>x).Select(File.ReadAllText));
    var test=new DensityTorchAdversarialTests();test.Setup();
    AssertionException failure=null;try{test.DestructiveBurning_LooseTorch_ConsumesFuelOnceAndStillDamagesTool();}catch(AssertionException ex){failure=ex;}
    var torch=(Entity)typeof(DensityTorchAdversarialTests).GetField("torch",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(test);
    Assert.AreEqual(fuel,torch.GetPart<FuelPart>().FuelMass,.0001f);
    Assert.AreEqual(intensity,torch.GetEffect<BurningEffect>().Intensity,.0001f);
    Assert.Less(torch.GetStatValue("Hitpoints"),1000);
    if(registry=="empty")Assert.IsNull(failure);else Assert.IsNotNull(failure,"The unchanged original 49.7 assertion must fail with real reactions enabled.");
    TestContext.Out.WriteLine($"registry={registry}, reactions={MaterialReactionResolver.ReactionCount}, originalAssertion={(failure==null?"PASS":"FAIL")}, fuel={torch.GetPart<FuelPart>().FuelMass:R}, intensity={torch.GetEffect<BurningEffect>().Intensity:R}, hp={torch.GetStatValue("Hitpoints")}");
   } finally { rows.Clear();foreach(var row in old)rows.Add(row);listField.SetValue(null,original);initializedField.SetValue(null,initialized);MaterialReactionResolver.Factory=factory; }
  }
 }
}
