using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class ReferenceGladeFourthProfileTests
    {
        [TestCase("Player")][TestCase("Warden")][TestCase("Villager")]
        public void HumanoidsHaveReadableProjectedHeightWithTheirRealRigAndPicking(string blueprint)
        {
            using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
            {
                var owner=blueprint=="Player"?f.Player:f.Add(blueprint);f.CleanGear(owner);f.Refresh();
                var root=f.View(owner);var bounds=BodyBounds(root);var camera=f.Get<Camera>("WorldCamera");
                // Sixth now measures visible form rather than the old padded
                // animation envelope. With the unchanged 56-degree camera,
                // projection is physical depth + height / tan(pitch).
                Assert.That(bounds.size.y,Is.InRange(1.20f,1.36f));
                float projected=ProjectedCells(camera,bounds);
                Assert.That(projected,Is.EqualTo(bounds.size.z+bounds.size.y/Mathf.Tan(NativeZone3DRenderSurface.CameraPitchDegrees*Mathf.Deg2Rad)).Within(.001f));
                Assert.That(projected,Is.InRange(1.10f,1.30f));
                Assert.LessOrEqual(bounds.size.x,1.11f);Assert.NotNull(root.GetComponentInChildren<Animator>());
                Assert.IsTrue(f.Pick(owner,out _));var at=f.Zone.GetEntityPosition(owner);int version=f.Zone.EntityVersion;
                f.Refresh();Assert.AreSame(root,f.View(owner));Assert.AreEqual(at,f.Zone.GetEntityPosition(owner));Assert.AreEqual(version,f.Zone.EntityVersion);
                owner.GetPart<RenderPart>().Visible=false;f.Refresh(f.Dirty(owner));Assert.IsFalse(f.Rendered(owner));
                owner.GetPart<RenderPart>().Visible=true;f.Refresh(f.Dirty(owner));Assert.IsTrue(f.Pick(owner,out _));
                f.Zone.RemoveEntity(owner);f.Refresh();Assert.IsFalse(f.Authored(owner));
            }
        }
        [TestCase("MarlbackScrabbler")][TestCase("MarlbackGleaner")][TestCase("MarlbackTunnelguard")]
        [TestCase("MarlbackWallkeeper")][TestCase("MarlbackBreacher")]
        public void OriginalMarlbacksStayLowAndWideButDoNotCollapseIntoSmallDots(string blueprint)
        {
            using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
            {
                var owner=f.Add(blueprint);f.CleanGear(owner);f.Refresh();var bounds=BodyBounds(f.View(owner));
                Assert.That(bounds.size.x,Is.InRange(.95f,1.03f));Assert.LessOrEqual(bounds.size.y,.91f);
                Assert.Greater(bounds.size.x,bounds.size.y);Assert.IsTrue(f.Pick(owner,out _));
                f.Zone.RemoveEntity(owner);f.Refresh();Assert.IsFalse(f.Authored(owner));
            }
        }
        [Test]
        public void DynamicGearAndFogUseTheResizedOwnedRigWithoutChangingNativeEquipment()
        {
            using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
            {
                f.CleanGear(f.Player);f.Refresh();var root=f.View(f.Player);float scale=root.transform.localScale.x;
                Assert.That(BodyBounds(root).size.y,Is.InRange(1.20f,1.36f));
                var dagger=f.Equip(f.Player,"Dagger");f.Frame();Assert.IsTrue(f.Equipment(f.Player,dagger,out var gear));
                Assert.IsTrue(gear.transform.IsChildOf(root.transform));Assert.AreSame(f.Player,dagger.GetPart<PhysicsPart>().Equipped);
                Assert.AreEqual(scale,root.transform.localScale.x);Assert.IsTrue(f.Pick(f.Player,out _));
                var cell=f.Zone.GetEntityCell(f.Player);cell.IsVisible=false;f.Refresh(new HashSet<int>());Assert.IsFalse(f.Rendered(f.Player));Assert.IsFalse(SpawnRing3DIntegrationFixture.Drawn(gear));
                cell.IsVisible=true;f.Refresh(new HashSet<int>());Assert.IsTrue(f.Rendered(f.Player));Assert.IsTrue(SpawnRing3DIntegrationFixture.Drawn(gear));
                Assert.IsTrue(InventorySystem.UnequipItem(f.Player,dagger));f.Frame();Assert.IsFalse(f.Equipment(f.Player,dagger,out _));
            }
        }
        [Test]
        public void AuthorityLossAndRecoveryRestoreNativeThenReadableScale()
        {
            using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
            {
                f.CleanGear(f.Player);f.Refresh();var original=f.Library.FindModel("ring-player").transform.localScale;
                Assert.That(BodyBounds(f.View(f.Player)).size.y,Is.InRange(1.20f,1.36f));
                f.Manager.WorldMap.Tiles[11,10]=BiomeType.Sodden;f.Refresh();Assert.AreEqual(original,f.View(f.Player).transform.localScale);
                f.Manager.WorldMap.Tiles[11,10]=BiomeType.Spread;f.Refresh();Assert.That(BodyBounds(f.View(f.Player)).size.y,Is.InRange(1.20f,1.36f));
            }
            using(var f=new SpawnRing3DIntegrationFixture())Assert.AreEqual(Vector3.one,f.View(f.Player).transform.localScale);
        }
        [Test]
        public void UnpaintedMothKeepsItsExistingSmallPresentationContract()
        {
            using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
            {
                var owner=f.Add("GroveLanternMoth");f.Refresh();var b=BodyBounds(f.View(owner));
                Assert.LessOrEqual(b.size.y,.91f);Assert.LessOrEqual(b.size.x,.69f);
                Assert.IsTrue(f.Pick(owner,out _));
                Assert.IsFalse(ReferenceGladeVoxelLibrary.ActorModelIds.Contains("ring-grove-lantern-moth"));
            }
        }
        private static Bounds BodyBounds(GameObject root)
        {
            // Sixth body forms retain larger safety culling bounds. Measure the
            // actual visible vertices only for these exact three scoped meshes.
            return ReferenceGladeHumanoidFormTests.VisibleBodyBounds(root);
        }
        private static float ProjectedCells(Camera camera,Bounds bounds)
        {
            float min=float.PositiveInfinity,max=float.NegativeInfinity;
            foreach(int x in new[]{-1,1})foreach(int y in new[]{-1,1})foreach(int z in new[]{-1,1})
            {var point=bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z));float py=camera.WorldToViewportPoint(point).y;min=Mathf.Min(min,py);max=Mathf.Max(max,py);}
            float oneCell=Mathf.Abs(camera.WorldToViewportPoint(bounds.center+Vector3.forward).y-camera.WorldToViewportPoint(bounds.center).y);
            Assert.Greater(oneCell,0);return(max-min)/oneCell;
        }
    }
}
