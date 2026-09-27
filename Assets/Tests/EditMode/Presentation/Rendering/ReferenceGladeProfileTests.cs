using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class ReferenceGladeProfileTests
    {
        [TestCase("refresh")][TestCase("bind")][TestCase("frame")]
        public void ChangingAuthorityRebuildsTheOwnedProfileInBothDirections(string entry)
        {
            using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
            {
                var before=f.Get<NativeZone3DRenderSurface>("ActiveSurface");var oldRoot=f.View(f.Player);
                var localMesh=oldRoot.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
                var source=f.Library.FindModel("ring-player").GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
                var ordinaryMesh=Resources.Load<VoxelWorldMeshCatalog>(VoxelWorldMeshCatalog.ResourcePath).Resolve(source);
                Assert.AreNotSame(ordinaryMesh,localMesh);
                f.Manager.WorldMap.Tiles[11,10]=BiomeType.Sodden;Assert.IsFalse(ReferenceGladePlan.IsActive(f.Zone));
                Update(f,entry);var ordinary=f.Get<NativeZone3DRenderSurface>("ActiveSurface");
                Assert.NotNull(ordinary);Assert.AreNotSame(before,ordinary,"A stale glade profile must not survive loss of authority.");
                Assert.IsTrue(oldRoot==null);Assert.AreEqual(Vector3.one,f.View(f.Player).transform.localScale);
                Assert.AreSame(ordinaryMesh,f.View(f.Player).GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh);
                Assert.AreEqual(2.2f,ordinary.MaterialFor(f.Library.WorldMaterial).GetFloat("_Exposure"));
                f.Manager.WorldMap.Tiles[11,10]=BiomeType.Spread;Assert.IsTrue(ReferenceGladePlan.IsActive(f.Zone));
                Update(f,entry);var restored=f.Get<NativeZone3DRenderSurface>("ActiveSurface");
                Assert.NotNull(restored);Assert.AreNotSame(ordinary,restored);
                var recovered=f.View(f.Player);Assert.AreSame(localMesh,recovered.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh);
                var visible=ReferenceGladeHumanoidFormTests.VisibleBodyBounds(recovered);
                Assert.That(visible.size.y,Is.InRange(1.20f,1.36f));Assert.LessOrEqual(visible.size.x,1.11f);
                Assert.True(f.Pick(f.Player,out _));
                Assert.That(restored.MaterialFor(f.Library.WorldMaterial).GetFloat("_Exposure"),Is.InRange(1.1f,1.8f));
            }
        }
        static void Update(SpawnRing3DIntegrationFixture f,string entry)
        { if(entry=="refresh")f.Refresh();else if(entry=="bind")f.Bind(f.Zone);else f.Frame(); }
        [TestCase(true)][TestCase(false)]
        public void LightingUsesOwnedGladeProfileOnly(bool glade)
        {
            var library=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
            float ambient=library.WorldMaterial.GetFloat("_AmbientStrength"),exposure=library.WorldMaterial.GetFloat("_Exposure");
            using(var f=new SpawnRing3DIntegrationFixture(glade?ReferenceGladePlan.ZoneID:SpawnRing3DIntegrationFixture.Grove))
            {
                var surface=f.Get<NativeZone3DRenderSurface>("ActiveSurface");Assert.NotNull(surface);
                var owned=surface.MaterialFor(library.WorldMaterial);Assert.AreNotSame(library.WorldMaterial,owned);
                if(glade)
                {
                    // The second native image rejected black faces and harsh edges;
                    // the revised profile supplies owned ambient probes and softer fill.
                    Assert.That(owned.GetFloat("_AmbientStrength"),Is.InRange(.6f,.7f));Assert.That(owned.GetFloat("_Exposure"),Is.InRange(1.1f,1.8f));
                    Assert.That(surface.Sun.shadowStrength,Is.InRange(.6f,.75f));Assert.LessOrEqual(surface.Sun.shadowNormalBias,.025f);
                    Assert.Less(surface.WorldCamera.transform.position.y,25);Assert.AreEqual(FilterMode.Bilinear,surface.WorldCamera.targetTexture.filterMode);
                    Assert.Greater(surface.Sun.transform.eulerAngles.y,180);Assert.Less(surface.Sun.transform.eulerAngles.y,250);
                }
                else
                {Assert.AreEqual(2.2f,owned.GetFloat("_Exposure"));Assert.AreEqual(ambient,owned.GetFloat("_AmbientStrength"));Assert.AreEqual(.55f,surface.Sun.shadowStrength);Assert.AreEqual(35,surface.WorldCamera.transform.position.y);Assert.AreEqual(FilterMode.Bilinear,surface.WorldCamera.targetTexture.filterMode);}
                Village3DSettings.LowDetail=true;f.Frame();Assert.AreEqual(LightShadows.None,surface.Sun.shadows);
                Village3DSettings.LowDetail=false;f.Frame();Assert.AreEqual(LightShadows.Soft,surface.Sun.shadows);
            }
            Assert.AreEqual(ambient,library.WorldMaterial.GetFloat("_AmbientStrength"));Assert.AreEqual(exposure,library.WorldMaterial.GetFloat("_Exposure"));
        }
        [TestCase("Player")][TestCase("Warden")][TestCase("Villager")][TestCase("MarlbackScrabbler")][TestCase("MarlbackGleaner")]
        public void NativeActorsAreSmallRiggedPickableAndStillOwned(string blueprint)
        {
            using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
            {
                var e=blueprint=="Player"?f.Player:f.Add(blueprint);f.CleanGear(e);f.Refresh();var root=f.View(e);
                Assert.Less(root.transform.localScale.x,1f);Assert.NotNull(root.GetComponentInChildren<Animator>());
                var bounds=ReferenceGladeHumanoidFormTests.VisibleBodyBounds(root);
                // Fourth screenshot refinement enlarges only the three humanoid
                // rigs and the original low/wide Marlbacks; native bodies stay fixed.
                bool marlback=blueprint.StartsWith("Marlback");
                Assert.LessOrEqual(bounds.size.y,marlback ? .91f : 1.36f);Assert.LessOrEqual(bounds.size.x,marlback ? 1.03f : 1.11f);
                Assert.IsTrue(f.Pick(e,out var point));int version=f.Zone.EntityVersion;var original=f.Zone.GetEntityPosition(e);
                f.Refresh();Assert.AreEqual(version,f.Zone.EntityVersion);Assert.AreEqual(original,f.Zone.GetEntityPosition(e));
                e.GetPart<RenderPart>().Visible=false;f.Refresh(f.Dirty(e));Assert.IsFalse(f.Rendered(e));
            }
        }
        [Test]public void PlayerGearSharesSmallRigAndOrdinaryZoneRetainsFullScale()
        {
            using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
            {
                f.CleanGear(f.Player);var item=f.Equip(f.Player,"Dagger");f.Refresh();Assert.IsTrue(f.Equipment(f.Player,item,out var gear));
                var actor=f.View(f.Player);Assert.IsTrue(gear.transform.IsChildOf(actor.transform));Assert.Less(actor.transform.localScale.x,1f); // Actual visible height/width are independently pinned; culling bounds no longer set scale.
                Assert.AreSame(f.Player,item.GetPart<PhysicsPart>().Equipped);Assert.IsTrue(f.Pick(f.Player,out var point));
            }
            using(var f=new SpawnRing3DIntegrationFixture())Assert.AreEqual(Vector3.one,f.View(f.Player).transform.localScale);
        }
        [TestCase("Warden")][TestCase("Villager")]
        public void LocalHumanoidAliasRefusesChangedGlyphOrForeignZone(string blueprint)
        {
            using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
            {
                var e=f.Add(blueprint);f.Refresh();Assert.IsTrue(f.Rendered(e));e.GetPart<RenderPart>().RenderString="!";f.Refresh();Assert.IsFalse(f.Authored(e));
                e.GetPart<RenderPart>().RenderString="@";f.Zone.RemoveEntity(e);Assert.IsNull(SpawnRing3DRecipes.Resolve(f.Zone,e,f.Library.Definition).ModelId);
            }
            using(var f=new SpawnRing3DIntegrationFixture()){var e=f.Add(blueprint);Assert.IsNull(SpawnRing3DRecipes.Resolve(f.Zone,e,f.Library.Definition).ModelId);}
        }
        [TestCase("Chest","chest")][TestCase("WoodenBarrel","barrel")]
        public void RealPropsHaveScopedDetailedModelsAndRemovedOwnersDisappear(string blueprint,string family)
        {
            using(var f=new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
            {
                var e=f.Add(blueprint);f.Refresh();var recipe=SpawnRing3DRecipes.Resolve(f.Zone,e,f.Library.Definition);
                Assert.That(recipe.ModelId,Does.StartWith("reference-glade-"+family+"-"));Assert.AreSame(e,recipe.Owner);
                var mesh=ReferenceGladeVoxelLibrary.Load().Find(recipe.ModelId).Mesh;Assert.Greater(mesh.vertexCount,400);Assert.LessOrEqual(mesh.bounds.size.y,.7f);
                f.Zone.RemoveEntity(e);f.Refresh();Assert.IsFalse(f.Authored(e));
            }
            using(var f=new SpawnRing3DIntegrationFixture()){var e=f.Add(blueprint);Assert.IsFalse(SpawnRing3DRecipes.Resolve(f.Zone,e,f.Library.Definition).ModelId.StartsWith("reference-glade-"));}
        }
    }
}
