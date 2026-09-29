using System;using System.IO;using System.Linq;using CavesOfOoo.Core;using CavesOfOoo.Data;using NUnit.Framework;using UnityEngine;
namespace CavesOfOoo.Tests {public sealed class MerchantLoadoutStateProbe {
 [TestCase(false)][TestCase(true)]public void RealMerchantCapacityDependsOnInheritedLoadoutFactory(bool wired){
 var old=LoadoutPart.Factory;var rng=LoadoutPart.Rng;var oldTrader=TraderPart.Factory;
 try{var f=new EntityFactory();f.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,"Resources/Content/Blueprints/Objects.json")));LoadoutPart.Factory=wired?f:null;LoadoutPart.Rng=new System.Random(1);TraderPart.Factory=null;
 var merchant=f.CreateEntity("Merchant");var inv=merchant.GetPart<InventoryPart>();foreach(var item in inv.Objects.ToArray())inv.RemoveObject(item);
 Assert.AreEqual(wired?8:0,inv.GetCarriedWeight());var gear=string.Join(",",inv.EquippedItems.Values.Distinct().Select(e=>e.BlueprintName));
 var apples=f.CreateEntity("Starapple");apples.GetPart<StackerPart>().StackCount=99;Assert.True(inv.AddObject(apples));var second=f.CreateEntity("Starapple");second.GetPart<StackerPart>().StackCount=51;var accepts=inv.AddObject(second);Assert.AreEqual(!wired,accepts);
 TestContext.Out.WriteLine($"LoadoutFactory={wired}, gear={gear}, maxWeight={inv.MaxWeight}, second51ApplesAccepted={accepts}, finalWeight={inv.GetCarriedWeight()}");
 }finally{LoadoutPart.Factory=old;LoadoutPart.Rng=rng;TraderPart.Factory=oldTrader;}
 }}}
