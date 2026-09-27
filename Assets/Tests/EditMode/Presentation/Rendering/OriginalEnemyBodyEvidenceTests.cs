using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Scenarios.Custom;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class OriginalEnemyBodyEvidenceTests
    {
        [TestCase("bare",true)][TestCase("equipped",true)]
        [TestCase("copied-body",false)][TestCase("duplicate-body",false)]
        [TestCase("unknown-skin",false)][TestCase("unowned-gear-copy",false)]
        public void DiagnosticCountsOnlyTheExactBodyAfterOwnedEquipmentProof(string mode,bool expected)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                var actor=f.Add("MarlbackBreacher");if(mode=="bare")f.CleanGear(actor);f.Refresh();
                var root=f.View(actor);var presenter=(SpawnRing3DPresenter)f.Presenter;
                Assert.True(presenter.TryGetApprovedStyle(actor,out var proof),proof.Failure);
                var body=root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(x=>x.sharedMesh==proof.SubmittedMesh);
                Mesh copied=null;GameObject extra=null;
                try
                {
                    if(mode=="copied-body"){copied=UnityEngine.Object.Instantiate(body.sharedMesh);body.sharedMesh=copied;}
                    if(mode=="duplicate-body")extra=UnityEngine.Object.Instantiate(body.gameObject,root.transform);
                    if(mode=="unknown-skin")
                    {
                        extra=new GameObject("Unknown unowned skin");extra.transform.SetParent(root.transform,false);
                        var skin=extra.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=SpreadEquipment3DLibrary.Load().Find("spread-worn-leatherboots").Mesh;
                        skin.bones=new[]{root.transform};skin.rootBone=root.transform;
                    }
                    if(mode=="unowned-gear-copy")
                    {
                        var gear=actor.GetPart<InventoryPart>().GetAllEquipped().First(x=>x.GetPart<EquippablePart>().Slot=="Body");
                        Assert.True(presenter.TryGetEquipmentView(actor,gear,out var view));extra=UnityEngine.Object.Instantiate(view,root.transform);
                    }
                    var driver=f.Root.AddComponent<OriginalEnemyNativePlayer>();
                    typeof(OriginalEnemyNativePlayer).GetField("_presenter",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(driver,presenter);
                    // The legacy overload is the actual pre-repair audit; this
                    // adapter records its failing behavior before the owner-aware
                    // signature is available, rather than reproducing its logic.
                    const BindingFlags flags=BindingFlags.Static|BindingFlags.Instance|BindingFlags.NonPublic;
                    var type=typeof(OriginalEnemyNativePlayer);
                    var owned=type.GetMethod("UsesAdoptedBody",flags,null,new[]{typeof(string),typeof(Entity),typeof(GameObject)},null);
                    bool actual=owned!=null?(bool)owned.Invoke(driver,new object[]{"ring-snapjaw-warlord",actor,root})
                        :(bool)type.GetMethod("UsesAdoptedBody",flags).Invoke(null,new object[]{"ring-snapjaw-warlord",root.GetComponentsInChildren<SkinnedMeshRenderer>(true)});
                    Assert.AreEqual(expected,actual,mode);
                }
                finally
                {if(extra!=null)UnityEngine.Object.DestroyImmediate(extra);if(copied!=null)UnityEngine.Object.DestroyImmediate(copied);}
            }
        }
    }
}
