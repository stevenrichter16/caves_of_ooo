using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class GleanersFieldworkArtTests
    {
        [TestCase("wicket-buckled")][TestCase("wicket-closed")]
        [TestCase("wicket-open")][TestCase("timber-pallet")]
        public void WorkingYardFormsAreImportedOriginalInertModels(string suffix)
        {
            string id = "connected-spread-" + suffix;
            CollectionAssert.Contains(ConnectedSpreadSource.ModelIds, id);
            var library = ConnectedSpread3DLibrary.Load(); Assert.NotNull(library); library.Validate();
            var model = library.Find(id); Assert.NotNull(model);
            Assert.Greater(model.Mesh.vertexCount, 24);
            Assert.AreSame(ReferenceGladeVoxelLibrary.Load().Material, model.Prefab.GetComponent<MeshRenderer>().sharedMaterial);
            Assert.IsEmpty(model.Prefab.GetComponentsInChildren<Collider>(true));
            Assert.IsEmpty(model.Prefab.GetComponentsInChildren<MonoBehaviour>(true));
        }

        [TestCase(0)][TestCase(1)]
        public void ActualWicketStateAndOrientationChooseDistinctModels(int turns)
        {
            using (var globals = new HotbarSaveFixture(false, false))
            using (var f = new ConnectedKitchenFixture())
            {
                var manager = OverworldZoneManager.CreateDetached(f.Factory, 64, true); manager.SetActiveZone(f.Zone);
                Assert.True(f.Factory.Blueprints.ContainsKey("GleanersBuckledWicket"));
                var owner = f.Factory.CreateEntity("GleanersBuckledWicket"); Assert.True(f.Zone.AddEntity(owner, 14, 12));
                var door = owner.GetPart<DoorPart>(); door.QuarterTurns = turns;
                Assert.AreEqual("connected-spread-wicket-buckled", ConnectedSpread3DLibrary.ResolveModel(f.Zone, owner));
                owner.GetPart<RepairablePart>().Repaired = true;
                Assert.AreEqual("connected-spread-wicket-closed", ConnectedSpread3DLibrary.ResolveModel(f.Zone, owner));
                door.IsOpen = true; door.Initialize();
                string model = ConnectedSpread3DLibrary.ResolveModel(f.Zone, owner);
                Assert.AreEqual("connected-spread-wicket-open", model);
                var library = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
                var recipe = SpawnRing3DRecipes.Resolve(f.Zone, owner, library.Definition);
                Assert.IsNull(recipe.Failure);
                Assert.AreEqual(turns, recipe.QuarterTurns);
                f.Zone.RemoveEntity(owner); Assert.Null(ConnectedSpread3DLibrary.ResolveModel(f.Zone, owner));
            }
        }
    }
}
