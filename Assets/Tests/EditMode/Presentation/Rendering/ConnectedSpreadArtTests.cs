using System;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class ConnectedSpreadArtTests
    {
        const string Prefix = "connected-spread-";
        static string Resolve(Zone zone, Entity owner)
        {
            var type = typeof(SpawnRing3DLibrary).Assembly.GetType("CavesOfOoo.Rendering.ConnectedSpread3DLibrary");
            Assert.NotNull(type, "Connected services need original current-state scenery, not glyph fallback.");
            return (string)type.GetMethod("ResolveModel", BindingFlags.Static | BindingFlags.Public).Invoke(null, new object[] { zone, owner });
        }
        static void Connect(ConnectedKitchenFixture f)
        { var manager = OverworldZoneManager.CreateDetached(f.Factory, 64, true); manager.SetActiveZone(f.Zone); }
        static Mesh Exact(Zone zone, Entity owner, string suffix)
        {
            string id = Prefix + suffix; Assert.AreEqual(id, Resolve(zone, owner));
            var library = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath); Assert.NotNull(library);
            var recipe = SpawnRing3DRecipes.Resolve(zone, owner, library.Definition);
            Assert.Null(recipe.Failure); Assert.AreEqual(id, recipe.ModelId); Assert.AreSame(owner, recipe.Owner);
            var spec = library.Definition.FindModel(id); var prefab = library.FindModel(id);
            Assert.NotNull(spec); Assert.NotNull(prefab); var mesh = prefab.GetComponent<MeshFilter>().sharedMesh;
            Assert.Greater(mesh.vertexCount, 24); Assert.GreaterOrEqual(mesh.uv.Distinct().Count(), 2);
            Assert.AreSame(ReferenceGladeVoxelLibrary.Load().Material, prefab.GetComponent<MeshRenderer>().sharedMaterial);
            Assert.IsEmpty(prefab.GetComponentsInChildren<Collider>(true)); Assert.IsEmpty(prefab.GetComponentsInChildren<MonoBehaviour>(true));
            Assert.IsEmpty(prefab.GetComponentsInChildren<Animator>(true)); return mesh;
        }
        [Test] public void ActualKitchenStatesSelectDifferentGeometryWithoutAdvancingWork()
        {
            using (var globals = new HotbarSaveFixture(false, false))
            using (var f = new ConnectedKitchenFixture())
            {
                Connect(f); f.Pan.GetPart<RepairablePart>().Repaired = false;
                var broken = Exact(f.Zone, f.Pan, "pan-cracked");
                f.Pan.GetPart<RepairablePart>().Repaired = true; var idle = Exact(f.Zone, f.Pan, "pan-empty");
                Assert.False(broken.vertices.SequenceEqual(idle.vertices)); Assert.True(f.Start());
                var working = Exact(f.Zone, f.Pan, "pan-covered"); Assert.False(working.vertices.SequenceEqual(idle.vertices));
                Exact(f.Zone, f.Escrow, "pantry-full"); Exact(f.Zone, f.Pickup, "pickup-empty");
                f.Clock.AdvanceClock(120); int tick = f.Clock.TickCount;
                Exact(f.Zone, f.Pan, "pan-covered"); Assert.AreEqual("Working", f.State, "Rendering cannot finish work.");
                f.Advance(0); var ready = Exact(f.Zone, f.Pan, "pan-ready");
                Assert.False(ready.vertices.SequenceEqual(working.vertices)); Exact(f.Zone, f.Escrow, "pantry-empty");
                var filled = Exact(f.Zone, f.Pickup, "pickup-full"); var meal = f.Finished.Contents.Single();
                f.Finished.RemoveItem(meal); Assert.True(f.Pack.AddObject(meal));
                var empty = Exact(f.Zone, f.Pickup, "pickup-empty"); Assert.False(filled.vertices.SequenceEqual(empty.vertices));
                Exact(f.Zone, f.Pan, "pan-empty"); Assert.AreEqual(tick, f.Clock.TickCount);
                Assert.AreEqual("Ready", f.State, "A visual query cannot clear saved work either.");
            }
        }
        [Test] public void IncompleteReadyGraphRendersEmptyInsteadOfCrashing()
        {
            using (var globals = new HotbarSaveFixture(false, false))
            using (var f = new ConnectedKitchenFixture())
            {
                Connect(f); var batch = f.Pan.GetPart<KitchenBatchPart>();
                batch.State = "Ready"; batch.Output = f.Factory.CreateEntity("FieldMeal"); batch.OutputID = batch.Output.ID;
                batch.Pickup = null; batch.PickupID = null;
                Assert.AreEqual(Prefix + "pan-empty", Resolve(f.Zone, f.Pan));
            }
        }
        [TestCase("ConnectedHeavyFrame", "heavy-frame")]
        [TestCase("FieldMeal", "field-meal")]
        [TestCase("DitchkeepersFootwork", "footwork-manual")]
        [TestCase("ConnectedReserveTray", "reserve-tray")]
        [TestCase("BotanicalInkDesk", "ink-desk")]
        public void RealNewOwnerHasOriginalInertPaletteArtAndRefusesRemovedOwners(string blueprint, string suffix)
        {
            using (var globals = new HotbarSaveFixture(false, false))
            using (var f = new ConnectedKitchenFixture())
            {
                Connect(f); var owner = f.Factory.CreateEntity(blueprint); Assert.NotNull(owner);
                Assert.True(f.Zone.AddEntity(owner, 14, 12)); var all = f.Zone.GetReadOnlyEntities().ToArray();
                Exact(f.Zone, owner, suffix); CollectionAssert.AreEquivalent(all, f.Zone.GetReadOnlyEntities());
                owner.GetPart<RenderPart>().Visible = false; Assert.Null(Resolve(f.Zone, owner));
                owner.GetPart<RenderPart>().Visible = true; Exact(f.Zone, owner, suffix);
                f.Zone.RemoveEntity(owner); Assert.Null(Resolve(f.Zone, owner));
            }
        }
        [TestCase("foreign-physics")][TestCase("carried")][TestCase("wrong-zone")][TestCase("no-repair")]
        public void UnrelatedOrBrokenOwnershipCannotBorrowKitchenStateArt(string fault)
        {
            using (var globals = new HotbarSaveFixture(false, false))
            using (var f = new ConnectedKitchenFixture())
            {
                Connect(f); Assert.AreEqual(Prefix + "pan-empty", Resolve(f.Zone, f.Pan));
                if (fault == "foreign-physics") f.Pan.GetPart<PhysicsPart>().ParentEntity = f.Player;
                if (fault == "carried") f.Pan.GetPart<PhysicsPart>().InInventory = f.Player;
                if (fault == "no-repair") f.Pan.RemovePart(f.Pan.GetPart<RepairablePart>());
                Assert.Null(Resolve(fault == "wrong-zone" ? new Zone(f.Zone.ZoneID) : f.Zone, f.Pan));
            }
        }
    }
}
