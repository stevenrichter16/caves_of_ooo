using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
namespace CavesOfOoo.Tests
{
    public sealed class ReferenceGladeAmbientTests
    {
        static void Probe(Renderer renderer)
        {
            Assert.AreEqual(LightProbeUsage.CustomProvided,renderer.lightProbeUsage);
            var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
            var red=block.GetVectorArray("unity_SHAr");Assert.NotNull(red);Assert.AreEqual(1,red.Length);
            Assert.That(red[0].w,Is.InRange(.2f,.5f));
        }
        [Test]public void OwnedGladeProbePreservesNativeAndIndexedPropertiesAndGlobalAmbient()
        {
            var ambient=RenderSettings.ambientProbe;
            using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
            {
                var surface=f.Get<NativeZone3DRenderSurface>("ActiveSurface");var root=f.View(f.Player);
                var r=root.GetComponentInChildren<Renderer>();Probe(r);
                var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);block.SetColor("_ReviewTint",Color.magenta);r.SetPropertyBlock(block);
                var indexed=new MaterialPropertyBlock();indexed.SetFloat("_ReviewSentinel",23);r.SetPropertyBlock(indexed,0);
                surface.PrepareModel(root,true);
                r.GetPropertyBlock(block);Assert.AreEqual(Color.magenta,block.GetColor("_ReviewTint"));Assert.AreEqual(1,block.GetFloat("_Transient"));
                r.GetPropertyBlock(indexed,0);Assert.AreEqual(23,indexed.GetFloat("_ReviewSentinel"));Assert.AreEqual(1,indexed.GetFloat("_Transient"));Assert.NotNull(indexed.GetVectorArray("unity_SHAr"));
                f.Refresh();Assert.IsTrue(f.Pick(f.Player,out var point));Probe(r);
                foreach(var renderer in surface.ContentRoot.GetComponentsInChildren<Renderer>(true))Probe(renderer);
            }
            Assert.AreEqual(ambient,RenderSettings.ambientProbe);
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                var r=f.View(f.Player).GetComponentInChildren<Renderer>();Assert.AreNotEqual(LightProbeUsage.CustomProvided,r.lightProbeUsage);
                var block=new MaterialPropertyBlock();r.GetPropertyBlock(block);Assert.IsNull(block.GetVectorArray("unity_SHAr"));
            }
        }
        [Test]public void NewGearAndActorsInheritFillWithoutChangingNativeOwnershipOrFog()
        {
            using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
            {
                f.CleanGear(f.Player);var item=f.Equip(f.Player,"Dagger");f.Refresh();Assert.IsTrue(f.Equipment(f.Player,item,out var gear));
                foreach(var renderer in gear.GetComponentsInChildren<Renderer>(true))Probe(renderer);
                Assert.AreSame(f.Player,item.GetPart<PhysicsPart>().Equipped);
                var actor=f.Add("Warden");f.Refresh();var root=f.View(actor);foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))Probe(renderer);
                actor.GetPart<RenderPart>().Visible=false;f.Refresh(f.Dirty(actor));Assert.IsFalse(f.Rendered(actor));
                actor.GetPart<RenderPart>().Visible=true;f.Refresh(f.Dirty(actor));Assert.IsTrue(f.Rendered(actor));Assert.IsTrue(f.Pick(actor,out var point));
                foreach(var renderer in f.View(actor).GetComponentsInChildren<Renderer>(true))Probe(renderer);
            }
        }
        [Test]public void AmbientProfileIsRemovedAndRestoredWithActualGladeAuthority()
        {
            using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
            {
                Probe(f.View(f.Player).GetComponentInChildren<Renderer>());
                f.Manager.WorldMap.Tiles[11,10]=BiomeType.Beating;f.Refresh();Assert.AreNotEqual(LightProbeUsage.CustomProvided,f.View(f.Player).GetComponentInChildren<Renderer>().lightProbeUsage);
                f.Manager.WorldMap.Tiles[11,10]=BiomeType.Spread;f.Frame();Probe(f.View(f.Player).GetComponentInChildren<Renderer>());
            }
        }
        [TestCase(true)][TestCase(false)]public void SmallMushroomRingArtRequiresItsActualGladeOwner(bool active)
        {
            using(var f=new SpawnRing3DIntegrationFixture(active?ReferenceGladePlan.ZoneID:SpawnRing3DIntegrationFixture.Grove))
            {
                var owner=f.Add("MushroomRing");var p=f.Zone.GetEntityPosition(owner);int version=f.Zone.EntityVersion;f.Refresh();
                var recipe=SpawnRing3DRecipes.Resolve(f.Zone,owner,f.Library.Definition);
                Assert.AreEqual(active,recipe.ModelId.StartsWith("reference-glade-mushroom-ring-"));Assert.AreSame(owner,recipe.Owner);
                Assert.AreEqual(version,f.Zone.EntityVersion);Assert.AreEqual(p,f.Zone.GetEntityPosition(owner));
                if(active){var mesh=ReferenceGladeVoxelLibrary.Load().Find(recipe.ModelId).Mesh;Assert.LessOrEqual(mesh.bounds.size.y,.5f);Assert.Greater(mesh.vertexCount,400);}
                owner.GetPart<RenderPart>().Visible=false;f.Refresh(f.Dirty(owner));Assert.IsFalse(f.Authored(owner));
                owner.GetPart<RenderPart>().Visible=true;owner.GetPart<RenderPart>().VisualID="unrecognized";Assert.IsFalse(SpawnRing3DRecipes.Resolve(f.Zone,owner,f.Library.Definition).ModelId?.StartsWith("reference-glade-mushroom-ring-")==true);
            }
        }
    }
}
