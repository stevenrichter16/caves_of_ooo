using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
 public sealed class SpreadCollectorCarryTransformTests
 {
  [TestCase("offset-parent")][TestCase("position")][TestCase("rotation")][TestCase("scale")]
  public void ChangedOwnedGripIsRefusedUntilTheOwnedViewIsResynchronized(string change)
  {
   using(var f=new SpreadCollectorCarryTests.Fixture())
   {
    f.Pick();Assert.True(f.View(out var carry));f.Proof();var bill=carry.transform.parent;GameObject offset=null;
    var item=f.Item;var inventory=f.Actor.GetPart<InventoryPart>();var holder=item.GetPart<PhysicsPart>().InInventory;int quantity=item.GetPart<StackerPart>().StackCount;
    try
    {
     if(change=="offset-parent"){offset=new GameObject("OwnedTestOffset");offset.transform.SetParent(bill,false);offset.transform.localPosition=new Vector3(.3f,0,0);carry.transform.SetParent(offset.transform,false);}
     if(change=="position")carry.transform.localPosition+=new Vector3(.3f,0,0);
     if(change=="rotation")carry.transform.localRotation*=Quaternion.Euler(45,70,20);
     if(change=="scale")carry.transform.localScale*=1.8f;
     Assert.False(f.View(out _),"Current item identity and exact mesh are insufficient when the live grip moved.");
     var method=typeof(SpawnRing3DPresenter).GetMethod("TryGetApprovedCollectorCarryStyle");object[] args={f.Actor,item,null};Assert.False((bool)method.Invoke(f.Presenter,args),"Style proof must include the committed grip.");
     f.F.Frame();Assert.True(f.View(out var repaired));f.Proof();Assert.AreSame(bill,repaired.transform.parent);
     var filter=repaired.GetComponent<MeshFilter>();Assert.Less(filter.sharedMesh.vertices.Min(v=>Vector3.Distance(filter.transform.TransformPoint(v),bill.position)),.08f);
     Assert.AreSame(item,f.Carried);Assert.AreSame(holder,item.GetPart<PhysicsPart>().InInventory);Assert.AreEqual(quantity,item.GetPart<StackerPart>().StackCount);Assert.True(inventory.Objects.Contains(item));Assert.IsNull(item.GetPart<PhysicsPart>().Equipped);
    }
    finally{if(offset!=null)UnityEngine.Object.DestroyImmediate(offset);}
   }
  }
 }
}
