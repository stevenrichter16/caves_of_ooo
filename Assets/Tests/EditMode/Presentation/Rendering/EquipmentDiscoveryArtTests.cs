using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    // Factory-owned fixtures establish source/attachment identity, not acquisition or balance.
    public sealed class EquipmentDiscoveryArtTests
    {
        const string Prefix = "equipment-discovery-", Folder = "EquipmentDiscovery3D/";
        static readonly string[] Heads = { "PeatMalletHeadComponent", "CinderhookAxeHeadComponent", "CounterweightLongBladeComponent" };
        static readonly string[] Forms = { "peatmallet", "cinderhook", "counterweight" };
        static IEnumerable<string> Ids()
        {
            foreach (string head in Forms) yield return Prefix + "head-" + head;
            foreach (string head in Forms) foreach (string haft in new[] { "oak", "willow" }) foreach (string binding in new[] { "leather", "serrated" })
                yield return Prefix + "forged-" + head + "-" + haft + "-" + binding;
            yield return Prefix + "groundwire-screen"; yield return Prefix + "kilnfelt-apron"; yield return Prefix + "worn-kilnfelt-apron";
        }
        static Mesh MeshFor(string id)
        {
            var prefab = Resources.Load<GameObject>(Folder + id); Assert.NotNull(prefab, id);
            return prefab.GetComponent<MeshFilter>().sharedMesh;
        }
        [Test] public void EighteenOriginalFormsHaveActualRegisteredGeometry()
        {
            var library = Resources.Load<ScriptableObject>(Folder + "Library"); Assert.NotNull(library, "Missing original equipment discovery kit.");
            library.GetType().GetMethod("Validate").Invoke(library, null);
            var entries = (Array)library.GetType().GetField("Entries").GetValue(library); Assert.AreEqual(18, entries.Length);
            var ring = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
            var distinct = new HashSet<Mesh>();
            foreach (string id in Ids())
            {
                var mesh = MeshFor(id); Assert.True(distinct.Add(mesh), "Each declared form owns an original mesh.");
                var prefab = Resources.Load<GameObject>(Folder + id); Assert.AreSame(prefab, ring.FindModel(id));
                var spec = ring.Definition.FindModel(id); Assert.NotNull(spec, id);
                Assert.AreEqual(mesh.bounds.center, spec.boundsCenter); Assert.AreEqual(mesh.bounds.size, spec.boundsSize);
                Assert.That(mesh.vertexCount, Is.InRange(72, 4000)); Assert.Greater(mesh.triangles.Length, 0);
                Assert.GreaterOrEqual(mesh.uv.Distinct().Count(), 2, id); Assert.AreEqual(mesh.vertexCount, mesh.normals.Length);
                Assert.AreEqual(1, prefab.GetComponentsInChildren<Renderer>(true).Length);
                Assert.IsEmpty(prefab.GetComponentsInChildren<Collider>(true)); Assert.IsEmpty(prefab.GetComponentsInChildren<MonoBehaviour>(true));
                Assert.AreSame(ring.WorldMaterial, prefab.GetComponent<MeshRenderer>().sharedMaterial);
            }
        }
        [TestCase("PeatMalletHeadComponent", "head-peatmallet")]
        [TestCase("CinderhookAxeHeadComponent", "head-cinderhook")]
        [TestCase("CounterweightLongBladeComponent", "head-counterweight")]
        [TestCase("GroundwireScreen", "groundwire-screen")]
        [TestCase("KilnfeltApron", "kilnfelt-apron")]
        public void ExactPortableIdentityUsesItsOwnForm(string blueprint, string suffix)
        {
            using (var f = new DensityLootTestScope())
            {
                var item = f.Factory.CreateEntity(blueprint); Assert.NotNull(item);
                Assert.True(SpreadPortableRecipes.TryRecipe(item, out var id)); Assert.AreEqual(Prefix + suffix, id);
                Assert.NotNull(MeshFor(id));
            }
        }
        [Test] public void AllTwelveForgedVariantsHaveDistinctCurrentAssemblyForms()
        {
            using (var f = new DensityLootTestScope())
            {
                var seen = new HashSet<string>(); var shapes = new HashSet<string>();
                for (int h = 0; h < Heads.Length; h++) foreach (string haft in new[] { "Oak", "Willow" }) foreach (string binding in new[] { "LeatherBinding", "SerratedEdge" })
                {
                    var item = f.Factory.CreateEntity("ForgedWeapon");
                    item.AddPart(new WeaponAssemblyPart { BladeBlueprint = Heads[h], HaftBlueprint = haft + "HaftComponent", BindingBlueprint = binding + "Component" });
                    Assert.True(SpreadPortableRecipes.TryRecipe(item, out string id)); Assert.True(seen.Add(id));
                    Assert.AreEqual(Prefix + "forged-" + Forms[h] + "-" + haft.ToLowerInvariant() + "-" + (binding == "LeatherBinding" ? "leather" : "serrated"), id);
                    var mesh = MeshFor(id); shapes.Add(string.Join(";", mesh.vertices.Select(v => v.ToString("F4"))) + string.Join(";", mesh.uv.Select(v => v.ToString("F4"))));
                }
                Assert.AreEqual(12, shapes.Count, "Haft and binding variants must change submitted geometry or paint.");
            }
        }
        [TestCase("foreign-assembly")][TestCase("unknown-haft")][TestCase("unknown-binding")][TestCase("foreign-render")][TestCase("foreign-physics")][TestCase("hidden")][TestCase("natural")][TestCase("override")]
        public void MalformedNewForgedOwnerNeverBorrowsAValidAssembly(string fault)
        {
            using (var f = new DensityLootTestScope())
            {
                var item = f.Factory.CreateEntity("ForgedWeapon"); var assembly = new WeaponAssemblyPart { BladeBlueprint = Heads[0], HaftBlueprint = "OakHaftComponent", BindingBlueprint = "LeatherBindingComponent" }; item.AddPart(assembly);
                Assert.True(SpreadPortableRecipes.TryRecipe(item, out _));
                switch (fault) {
                    case "foreign-assembly": assembly.ParentEntity = new Entity(); break;
                    case "unknown-haft": assembly.HaftBlueprint = "Missing"; break;
                    case "unknown-binding": assembly.BindingBlueprint = "Missing"; break;
                    case "foreign-render": item.GetPart<RenderPart>().ParentEntity = new Entity(); break;
                    case "foreign-physics": item.GetPart<PhysicsPart>().ParentEntity = new Entity(); break;
                    case "hidden": item.GetPart<RenderPart>().Visible = false; break;
                    case "natural": item.SetTag("Natural"); break;
                    case "override": item.GetPart<RenderPart>().VisualID = "foreign"; break;
                }
                Assert.False(SpreadPortableRecipes.TryRecipe(item, out _), fault);
            }
        }
        [TestCase("Overworld.12.10.0")][TestCase("Overworld.15.7.0")]
        public void RealPickupDropAndUnequipPreserveExactRegionalForms(string zoneId)
        {
            using (var f = new SpawnRing3DIntegrationFixture(zoneId))
            {
                f.CleanGear(f.Player);
                foreach (string blueprint in new[] { "GroundwireScreen", "KilnfeltApron" })
                {
                    var item = f.Equip(f.Player, blueprint); f.Refresh();
                    Assert.True(f.Equipment(f.Player, item, out var view), blueprint); Assert.True(SpawnRing3DIntegrationFixture.Drawn(view));
                    string suffix = blueprint == "GroundwireScreen" ? "groundwire-screen" : "worn-kilnfelt-apron";
                    var submitted = view.GetComponentsInChildren<MeshFilter>(true).Select(x => x.sharedMesh).Concat(view.GetComponentsInChildren<SkinnedMeshRenderer>(true).Select(x => x.sharedMesh)).ToArray();
                    Assert.AreEqual(1, submitted.Length); Assert.AreSame(MeshFor(Prefix + suffix), submitted[0]);
                    Assert.True(InventorySystem.UnequipItem(f.Player, item)); f.Refresh(); Assert.False(f.Equipment(f.Player, item, out _));
                    Assert.True(InventorySystem.Drop(f.Player, item, f.Zone)); f.Refresh();
                    var recipe = SpawnRing3DRecipes.Resolve(f.Zone, item, f.Library.Definition);
                    Assert.AreEqual(Prefix + (blueprint == "GroundwireScreen" ? "groundwire-screen" : "kilnfelt-apron"), recipe.ModelId); Assert.True(f.Rendered(item));
                    Assert.True(InventorySystem.Pickup(f.Player, item, f.Zone)); f.Refresh(); Assert.False(f.Rendered(item));
                }
            }
        }
        [Test] public void ReforgingChangesActualHeldShapeAndMalformedAssemblyRemovesIt()
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.15.7.0"))
            {
                f.CleanGear(f.Player); var item = f.Equip(f.Player, "ForgedWeapon");
                var assembly = new WeaponAssemblyPart { BladeBlueprint = Heads[0], HaftBlueprint = "OakHaftComponent", BindingBlueprint = "LeatherBindingComponent" }; item.AddPart(assembly);
                var seen = new HashSet<Mesh>();
                for (int h = 0; h < Heads.Length; h++) foreach (string haft in new[] { "Oak", "Willow" }) foreach (string binding in new[] { "LeatherBinding", "SerratedEdge" }) {
                    assembly.BladeBlueprint = Heads[h]; assembly.HaftBlueprint = haft + "HaftComponent"; assembly.BindingBlueprint = binding + "Component"; f.Refresh();
                    Assert.True(f.Equipment(f.Player, item, out var view));
                    var mesh = view.GetComponent<MeshFilter>().sharedMesh; Assert.True(seen.Add(mesh));
                    Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedEquipmentStyle(f.Player, item, out var proof), proof.Failure);
                    Assert.AreSame(mesh, proof.ExpectedMesh); Assert.AreSame(mesh, proof.SubmittedMesh);
                }
                assembly.HaftBlueprint = "Missing"; f.Refresh(); Assert.False(f.Equipment(f.Player, item, out _));
                Assert.AreSame(f.Player, item.GetPart<PhysicsPart>().Equipped, "The presentation refusal never edits equipment.");
            }
        }
        [Test] public void SavedFittedApronRestoresOnlyTheCurrentNativeOwnerAndActualBone()
        {
            using (var f = new SpawnRing3DIntegrationFixture("Overworld.15.7.0")) {
                f.CleanGear(f.Player); var item = f.Equip(f.Player, "KilnfeltApron"); f.Refresh();
                Assert.True(f.Equipment(f.Player, item, out var old));
                var renderer = old.GetComponentInChildren<SkinnedMeshRenderer>(); Assert.NotNull(renderer);
                Assert.AreEqual(1, renderer.bones.Length); Assert.True(renderer.bones[0].IsChildOf(f.View(f.Player).transform));
                Assert.AreSame(renderer.bones[0], renderer.rootBone); Assert.AreSame(MeshFor(Prefix + "worn-kilnfelt-apron"), renderer.sharedMesh);
                var save = f.RoundTrip(); f.BindLoaded(save);
                var restored = f.Player.GetPart<InventoryPart>().GetAllEquipped().Single(x => x.ID == item.ID);
                Assert.AreNotSame(restored, item); Assert.False(f.Equipment(f.Player, item, out _)); Assert.True(f.Equipment(f.Player, restored, out _));
                Assert.True(((SpawnRing3DPresenter)f.Presenter).TryGetApprovedEquipmentStyle(f.Player, restored, out var proof), proof.Failure);
                Assert.AreSame(MeshFor(Prefix + "worn-kilnfelt-apron"), proof.ExpectedMesh);
            }
        }
        [TestCase("foreign-armor")][TestCase("foreign-equip")][TestCase("wrong-slot")][TestCase("foreign-render")][TestCase("foreign-physics")]
        public void NewDefenseCannotBorrowArmorAppearanceFromAForeignPart(string fault)
        {
            using (var f = new DensityLootTestScope()) {
                var item = f.Factory.CreateEntity("KilnfeltApron"); Assert.True(SpreadPortableRecipes.TryRecipe(item, out _));
                if(fault == "foreign-armor") item.GetPart<ArmorPart>().ParentEntity = new Entity();
                if(fault == "foreign-equip") item.GetPart<EquippablePart>().ParentEntity = new Entity();
                if(fault == "wrong-slot") item.GetPart<EquippablePart>().Slot = "Hand";
                if(fault == "foreign-render") item.GetPart<RenderPart>().ParentEntity = new Entity();
                if(fault == "foreign-physics") item.GetPart<PhysicsPart>().ParentEntity = new Entity();
                Assert.False(SpreadPortableRecipes.TryRecipe(item, out _));
            }
        }
        [Test] public void UnknownArtIdDoesNotManufactureAnAssetOrBorrowAnExistingHead()
        {
            var library = EquipmentDiscoveryArtLibrary.Load(); Assert.NotNull(library);
            foreach (string id in new[] { null, "", Prefix + "head-missing", Prefix + "forged-peatmallet-foreign-leather", "spread-portable-steelbladecomponent" })
                Assert.IsNull(library.Find(id));
        }
        [Test] public void AddedMeshesAreNeverVoxelizedTwice()
        {
            bool prior = Village3DSettings.Enabled; Village3DSettings.Enabled = true;
            try {
                foreach (string zone in new[] { "Overworld.12.10.0", "Overworld.15.7.0" }) {
                    var bridge = VoxelWorldPresentation.ForZone(new Zone(zone)); Assert.NotNull(bridge);
                    foreach (string id in Ids()) { var mesh = MeshFor(id); Assert.AreSame(mesh, bridge.Resolve(mesh)); }
                    Assert.AreEqual(0, bridge.MissingMeshCount); Assert.AreEqual(0, bridge.AppliedMeshCount);
                }
            } finally { Village3DSettings.Enabled = prior; }
        }
        [Test] public void OriginalSixComponentsAndEightAssembliesKeepOriginalModels()
        {
            using (var f = new DensityLootTestScope()) {
                foreach (string name in new[] { "SteelBladeComponent", "IronSpikeComponent", "OakHaftComponent", "WillowHaftComponent", "LeatherBindingComponent", "SerratedEdgeComponent" }) {
                    Assert.True(SpreadPortableRecipes.TryRecipe(f.Factory.CreateEntity(name), out string id)); Assert.AreEqual("spread-portable-" + name.ToLowerInvariant(), id);
                }
                foreach (string head in new[] { "SteelBlade", "IronSpike" }) foreach (string haft in new[] { "Oak", "Willow" }) foreach (string binding in new[] { "LeatherBinding", "SerratedEdge" }) {
                    var item = f.Factory.CreateEntity("ForgedWeapon"); item.AddPart(new WeaponAssemblyPart { BladeBlueprint = head + "Component", HaftBlueprint = haft + "HaftComponent", BindingBlueprint = binding + "Component" });
                    Assert.True(SpreadPortableRecipes.TryRecipe(item, out string id)); StringAssert.StartsWith("spread-portable-forged-", id);
                }
            }
        }
    }
}
