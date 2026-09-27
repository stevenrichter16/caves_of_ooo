using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    // Actual managed graphs and persistent adopted meshes. Source candidates or
    // a bare zone string are not evidence of the requested biome appearance.
    public sealed class SpreadBiomeProfileTests
    {
        static Mesh ApprovedPlayer()=>ReferenceGladeVoxelLibrary.Load().ActorPaints.Single(p=>p.ModelId=="ring-player").Painted;
        static void Approved(SpawnRing3DIntegrationFixture f)
        {
            var surface=f.Get<NativeZone3DRenderSurface>("ActiveSurface");Assert.NotNull(surface);
            var material=surface.MaterialFor(f.Library.WorldMaterial);
            Assert.That(material.GetFloat("_Exposure"),Is.InRange(1.1f,1.8f));
            var root=f.View(f.Player);Assert.AreSame(ApprovedPlayer(),root.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh);
            var visible=ReferenceGladeHumanoidFormTests.VisibleBodyBounds(root);
            Assert.That(visible.size.y,Is.InRange(1.20f,1.36f));Assert.LessOrEqual(visible.size.x,1.11f);
            Assert.True(f.Pick(f.Player,out _));
        }
        [TestCase("Overworld.12.10.0",false)]
        [TestCase("Overworld.10.10.0",true)]
        [TestCase("Overworld.15.6.0",true)]
        public void ApprovedPlayerAndProfileReachOrdinarySpreadAndActualPois(string address,bool poi)
        {
            using(var f=new SpawnRing3DIntegrationFixture(address))
            {
                var at=WorldMap.FromZoneID(address);Assert.AreEqual(BiomeType.Spread,f.Manager.WorldMap.GetBiome(at.x,at.y));
                Assert.AreEqual(poi,f.Manager.WorldMap.GetPOI(at.x,at.y)!=null);Assert.False(ReferenceGladePlan.IsActive(f.Zone));
                int version=f.Zone.EntityVersion;string tiles=f.Zone.TileState.ToSaveString();
                var owners=f.Zone.GetReadOnlyEntities().ToArray();var points=owners.Select(f.Zone.GetEntityPosition).ToArray();
                Approved(f);f.Refresh();Approved(f);
                Assert.AreEqual(version,f.Zone.EntityVersion);Assert.AreEqual(tiles,f.Zone.TileState.ToSaveString());
                CollectionAssert.AreEquivalent(owners,f.Zone.GetReadOnlyEntities());CollectionAssert.AreEqual(points,owners.Select(f.Zone.GetEntityPosition));
            }
        }
        [TestCase("bind")][TestCase("refresh")][TestCase("frame")]
        public void CurrentMapBiomeChangeRebuildsOwnedStyleAndCanRecover(string route)
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                Approved(f);var first=f.Get<NativeZone3DRenderSurface>("ActiveSurface");var old=f.View(f.Player);
                var source=f.Library.FindModel("ring-player").GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
                var ordinary=Resources.Load<VoxelWorldMeshCatalog>(VoxelWorldMeshCatalog.ResourcePath).Resolve(source);
                f.Manager.WorldMap.Tiles[12,10]=BiomeType.Sodden;Update(f,route);
                Assert.True(old==null);var next=f.Get<NativeZone3DRenderSurface>("ActiveSurface");Assert.AreNotSame(first,next);
                Assert.AreSame(ordinary,f.View(f.Player).GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh);
                Assert.AreEqual(Vector3.one,f.View(f.Player).transform.localScale);
                Assert.AreEqual(2.2f,next.MaterialFor(f.Library.WorldMaterial).GetFloat("_Exposure"));
                f.Manager.WorldMap.Tiles[12,10]=BiomeType.Spread;Update(f,route);Approved(f);
                Assert.AreNotSame(next,f.Get<NativeZone3DRenderSurface>("ActiveSurface"));
            }
        }
        [Test]public void RestoredManagedSpreadRetainsGameplayGraphAndReceivesApprovedBody()
        {
            using(var f=new SpawnRing3DIntegrationFixture("Overworld.12.10.0"))
            {
                f.CleanGear(f.Player);var item=f.Equip(f.Player,"Dagger");f.Refresh();
                string player=f.Player.ID,gear=item.ID;var point=f.Zone.GetEntityPosition(f.Player);
                var loaded=f.RoundTrip();f.BindLoaded(loaded);Approved(f);
                Assert.AreEqual(player,f.Player.ID);Assert.AreEqual(point,f.Zone.GetEntityPosition(f.Player));
                var restored=f.Player.GetPart<InventoryPart>().GetAllEquipped().Single(e=>e.ID==gear);
                Assert.AreSame(f.Player,restored.GetPart<PhysicsPart>().Equipped);Assert.True(f.Equipment(f.Player,restored,out var view));
                Assert.True(view.transform.IsChildOf(f.View(f.Player).transform));
            }
        }
        [Test]public void ForeignBiomeKeepsBorrowedAssetsAndOrdinaryMeshTreatment()
        {
            var glade=ReferenceGladeVoxelLibrary.Load();var palette=glade.Material;var source=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).WorldMaterial;
            float exposure=source.GetFloat("_Exposure");var shader=source.shader;
            using(var f=new SpawnRing3DIntegrationFixture())
            {
                Assert.AreNotSame(ApprovedPlayer(),f.View(f.Player).GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh);
                Assert.AreEqual(Vector3.one,f.View(f.Player).transform.localScale);
                Assert.AreEqual(2.2f,f.Get<NativeZone3DRenderSurface>("ActiveSurface").MaterialFor(source).GetFloat("_Exposure"));
            }
            Assert.AreSame(palette,glade.Material);Assert.AreSame(shader,source.shader);Assert.AreEqual(exposure,source.GetFloat("_Exposure"));
        }
        static void Update(SpawnRing3DIntegrationFixture f,string route)
        {if(route=="bind")f.Bind(f.Zone);else if(route=="refresh")f.Refresh();else f.Frame();}
    }
}
