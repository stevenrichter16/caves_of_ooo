using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
namespace CavesOfOoo.Tests
{
    public sealed class DensityOriginalEnemyArtTests
    {
        [TestCase("MarlbackScrabbler", "ring-snapjaw", "s")]
        [TestCase("MarlbackGleaner", "ring-marlback-gleaner", "s")]
        [TestCase("MarlbackTunnelguard", "ring-marlback-tunnelguard", "s")]
        [TestCase("MarlbackWallkeeper", "ring-marlback-wallkeeper", "S")]
        [TestCase("MarlbackBreacher", "ring-snapjaw-warlord", "S")]
        [TestCase("GroveLanternMoth", "ring-grove-lantern-moth", "m")]
        public void CurrentOriginalBodiesResolveWithNativeIdentityAndRefusalControls(string blueprint,string model,string glyph)
        {
            var library=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);Assert.NotNull(library);
            var binding=library.Definition.FindBlueprint(blueprint);Assert.NotNull(binding,blueprint);CollectionAssert.AreEqual(new[]{model},binding.models);
            var zone=new Zone("Overworld.4.6.0");var entity=new Entity{ID="original-art-owner",BlueprintName=blueprint};
            var render=new RenderPart{RenderString=glyph,Visible=true};entity.AddPart(render);zone.AddEntity(entity,20,10);
            int version=zone.EntityVersion;var result=SpawnRing3DRecipes.Resolve(zone,entity,library.Definition);
            Assert.AreEqual(model,result.ModelId,result.Failure);Assert.AreSame(entity,result.Owner);Assert.AreEqual(version,zone.EntityVersion);
            Assert.NotNull(library.FindModel(model));Assert.AreSame(library.FindModel(model),library.FindModel(model));
            render.Visible=false;Assert.Null(SpawnRing3DRecipes.Resolve(zone,entity,library.Definition).ModelId);render.Visible=true;
            render.RenderString="?";Assert.Null(SpawnRing3DRecipes.Resolve(zone,entity,library.Definition).ModelId);render.RenderString=glyph;
            zone.RemoveEntity(entity);Assert.Null(SpawnRing3DRecipes.Resolve(zone,entity,library.Definition).ModelId);
        }
        [TestCase("MarlbackScrabbler")][TestCase("MarlbackGleaner")][TestCase("MarlbackTunnelguard")][TestCase("MarlbackWallkeeper")][TestCase("MarlbackBreacher")]
        public void MarlbackBodiesAreLowWideAndRetainActualEquipmentRig(string blueprint)
        {
            var library=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);var binding=library.Definition.FindBlueprint(blueprint);Assert.NotNull(binding);
            var model=library.Definition.FindModel(binding.models.Single());Assert.Less(model.boundsSize.y,1.3f,"Original low bank-burrower silhouette");Assert.Greater(model.boundsSize.x,0.75f);
            CollectionAssert.AreEquivalent(new[]{"Equipment.Head","Equipment.Hand.L","Equipment.Hand.R","Equipment.Back"},model.sockets);
            CollectionAssert.AreEquivalent(new[]{"Idle","Walk","Interact","Attack","Hit"},model.clips);
            Assert.IsTrue(model.rigged);Assert.AreEqual("humanoid",model.rigFamily);
        }
        [TestCase("MarlbackScrabbler","actor.marlback_scrabbler","s")]
        [TestCase("MarlbackGleaner","actor.marlback_gleaner","s")]
        [TestCase("MarlbackTunnelguard","actor.marlback_tunnelguard","s")]
        [TestCase("MarlbackWallkeeper","actor.marlback_wallkeeper","S")]
        [TestCase("MarlbackBreacher","actor.marlback_breacher","S")]
        [TestCase("GroveLanternMoth","actor.grove_lantern_moth","m")]
        public void CurrentSpriteRoleHasItsOwnAuthoredSheetAndKeepsReskinGuard(string blueprint,string visual,string glyph)
        {
            var entity=new Entity{ID="sprite-owner",BlueprintName=blueprint};var render=new RenderPart{RenderString=glyph};entity.AddPart(render);
            Assert.IsTrue(EntityVisualCatalog.TryGetDefinition(entity,out var definition));Assert.AreEqual(visual,definition.ID);
            Assert.IsTrue(EntityVisualCatalog.TryGetAsset(entity,out var asset));Assert.NotNull(asset.GetFrame(EntityVisualState.Idle,EntityVisualFacing.South,0));
            render.RenderString="?";Assert.IsFalse(EntityVisualCatalog.TryGetDefinition(entity,out _));
            render.VisualID="actor.missing";Assert.IsFalse(EntityVisualCatalog.TryGetDefinition(entity,out _));
        }
        [TestCase("MarlbackScrabbler")][TestCase("MarlbackGleaner")][TestCase("MarlbackTunnelguard")]
        [TestCase("MarlbackWallkeeper")][TestCase("MarlbackBreacher")][TestCase("GroveLanternMoth")]
        public void CurrentBodyHasOfflineVoxelSkinAndPreservesIndependentUnknownMesh(string blueprint)
        {
            var library=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);var binding=library.Definition.FindBlueprint(blueprint);Assert.NotNull(binding);
            var prefab=library.FindModel(binding.models.Single());var voxel=Resources.Load<VoxelWorldMeshCatalog>(VoxelWorldMeshCatalog.ResourcePath);voxel.Validate();
            var skins=prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true);Assert.IsNotEmpty(skins);
            foreach(var skin in skins){var baked=voxel.Resolve(skin.sharedMesh);Assert.AreNotSame(skin.sharedMesh,baked,blueprint);Assert.AreEqual(skin.sharedMesh.bindposeCount,baked.bindposeCount);}
            var unknown=new Mesh();try{Assert.AreSame(unknown,voxel.Resolve(unknown));}finally{Object.DestroyImmediate(unknown);}
        }
        [Test]public void RetiredBlueprintNamesAreNotLiveArtBindings()
        {
            var library=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
            foreach(var name in new[]{"Snapjaw","SnapjawScavenger","SnapjawHunter","SnapjawChieftain","SnapjawWarlord","GlowMoth"})
                Assert.Null(library.Definition.FindBlueprint(name),name);
        }
    }
}
