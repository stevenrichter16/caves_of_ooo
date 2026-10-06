using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class FiftySecondVisualTests
    {
        [TestCase(SoddenDistrictPlan.WorksZoneID, "FilterHood")]
        [TestCase(LastCounterCompositionPlan.ZoneID, "ColdwardCloak")]
        public void RegionalEquipmentUsesRegisteredVoxelGeometryOnBodyAndGround(string zone, string blueprint)
        {
            using (var f = new SpawnRing3DIntegrationFixture(zone))
            {
                f.Set("FullReveal", true); f.CleanGear(f.Player);
                var item = f.Equip(f.Player, blueprint); f.Refresh();
                Assert.True(f.Equipment(f.Player, item, out var held));
                Assert.True(SpawnRing3DIntegrationFixture.Drawn(held));
                Assert.AreEqual(0, f.Get<int>("VoxelMissingMeshCount"), "Regional equipment must be registered in the voxel pipeline.");
                Assert.True(InventorySystem.UnequipItem(f.Player, item));
                Assert.True(InventorySystem.Drop(f.Player, item, f.Zone)); f.Refresh();
                Assert.True(f.Rendered(item));
                Assert.AreEqual(0, f.Get<int>("VoxelMissingMeshCount"));
            }
        }

        [Test]
        public void ActualTallyBurialGroundHasAVisibleMarkerAndCannotLeaveAGhost()
        {
            using (var f = new SpawnRing3DIntegrationFixture())
            using (var source = new HaulingContentScope())
            {
                // Cold generation needs matching factory statics and ordinary seeded content RNG,
                // independently of the equipment fixture's deterministic endpoint loadouts.
                source.Seed(64);
                f.Manager = OverworldZoneManager.CreateDetached(source.Factory, 64, true);
                f.Zone = f.Manager.GetZone(TallyCompositionPlan.ZoneID);
                Assert.NotNull(f.Zone, "The current cold pipeline must admit the real exchange.");
                var grave = f.Zone.GetReadOnlyEntities().SingleOrDefault(e => e.GetProperty(SecondExplorationSites.RoleKey) == "rest-court-graveyard");
                Assert.NotNull(grave, "The accepted exchange must contain its actual burial-service owner.");
                Assert.NotNull(grave.GetPart<BurialPart>());
                f.Set("FullReveal", true); f.Reveal(); f.Bind(f.Zone); f.Refresh();
                Assert.True(f.Rendered(grave), "The real cold-generated burial service needs a physical marker.");
                Assert.AreEqual(OlderdeepVoxelLibrary.ModelId("plaque", 0), SpawnRing3DRecipes.Resolve(f.Zone, grave, f.Library.Definition).ModelId);
                grave.GetPart<RenderPart>().Visible = false; f.Refresh();
                Assert.False(f.Rendered(grave));
                grave.GetPart<RenderPart>().Visible = true; f.Zone.RemoveEntity(grave); f.Refresh();
                Assert.False(f.Rendered(grave));
            }
        }

        [TestCase("WellmeetGuestLocker")]
        [TestCase("QuillholdLoanShelf")]
        [TestCase("QuillholdLoanWardGleam")]
        [TestCase("QuillholdLoanDryingBreeze")]
        [TestCase("FrontierClothScreen")]
        [TestCase("SoddenPassagePost")]
        [TestCase("SoddenPassageGuard")]
        [TestCase("CounterStoreChest")]
        [TestCase("CounterStoreKey")]
        [TestCase("CausticFilm")]
        public void ExplorationOwnersKeepTheirIdentityAndVisiblePhysicalForm(string blueprint)
        {
            using (var f = new SpawnRing3DIntegrationFixture())
            {
                f.Set("FullReveal", true); // Geometry coverage is independent of generated lighting.
                Assert.NotNull(f.Factory.CreateEntity(blueprint), "Authored owner exists.");
                var owner = f.Add(blueprint);
                string id = owner.ID;
                f.Refresh();
                Assert.True(f.Rendered(owner), blueprint + ": " + SpawnRing3DRecipes.Resolve(f.Zone, owner, f.Library.Definition).Failure);
                var recipe = SpawnRing3DRecipes.Resolve(f.Zone, owner, f.Library.Definition);
                Assert.IsNull(recipe.Failure);
                Assert.IsNotEmpty(recipe.ModelId);
                Assert.AreSame(owner, recipe.Owner);
                Assert.AreEqual(id, owner.ID);
                owner.GetPart<RenderPart>().Visible = false;
                f.Refresh(); Assert.False(f.Rendered(owner), "Hidden owner cannot retain geometry.");
                owner.GetPart<RenderPart>().Visible = true;
                f.Zone.RemoveEntity(owner);
                f.Refresh(); Assert.False(f.Rendered(owner), "Removed owner cannot retain geometry.");
            }
        }

        [Test]
        public void RecoveredLampUsesTheSamePhysicalJarInHandAndOnTheGround()
        {
            using (var f = new SpawnRing3DIntegrationFixture())
            {
                f.Set("FullReveal", true);
                f.CleanGear(f.Player);
                var lamp = f.Add("BeetleJar"); lamp.AddPart(new RecoverableLampPart());
                f.Approach(lamp);
                Assert.True(InventorySystem.PerformAction(f.Player, lamp, "RecoverLamp", f.Zone));
                Assert.True(InventorySystem.Equip(f.Player, lamp));
                f.Refresh();
                Assert.True(f.Equipment(f.Player, lamp, out var held));
                Assert.True(SpawnRing3DIntegrationFixture.Drawn(held));
                Assert.True(InventorySystem.UnequipItem(f.Player, lamp));
                Assert.True(InventorySystem.Drop(f.Player, lamp, f.Zone));
                f.Refresh(); Assert.True(f.Rendered(lamp));
                Assert.AreEqual(OlderdeepVoxelLibrary.ModelId("jar", 0), SpawnRing3DRecipes.Resolve(f.Zone, lamp, f.Library.Definition).ModelId);
            }
        }

        [Test]
        public void PenGateGeometryFollowsItsRealDoorState()
        {
            using (var f = new SpawnRing3DIntegrationFixture())
            {
                f.Set("FullReveal", true); // Geometry coverage is independent of generated lighting.
                Assert.NotNull(f.Factory.CreateEntity("FrontierPenGate"));
                var gate = f.Add("FrontierPenGate");
                f.Refresh(); Assert.True(f.Rendered(gate));
                var closed = SpawnRing3DRecipes.Resolve(f.Zone, gate, f.Library.Definition);
                Assert.AreEqual(SpreadFieldGate3DLibrary.Closed, closed.ModelId);
                var door = gate.GetPart<DoorPart>();
                f.Approach(gate);
                Assert.True(door.TrySetOpen(f.Player, f.Zone, true));
                f.Refresh(); Assert.True(f.Rendered(gate));
                var open = SpawnRing3DRecipes.Resolve(f.Zone, gate, f.Library.Definition);
                Assert.AreEqual(SpreadFieldGate3DLibrary.Open, open.ModelId);
                Assert.AreEqual(door.QuarterTurns, open.QuarterTurns);
            }
        }

        [Test]
        public void StormbinderRemainsKeepMarlbackCorpseShapeWithActualSourceIdentity()
        {
            using (var f = new DensityLootTestScope())
            {
                var corpse = f.Factory.CreateEntity("MarlbackCorpse");
                corpse.Properties["SourceBlueprint"] = "MarlbackStormbinder";
                corpse.Properties["SourceID"] = "dead-stormbinder-owner";
                Assert.True(SpreadPortableRecipes.TryRecipe(corpse, out string model));
                Assert.AreEqual("spread-portable-marlbackcorpse", model);
                corpse.Properties["SourceID"] = corpse.ID;
                Assert.False(SpreadPortableRecipes.TryRecipe(corpse, out _), "A corpse cannot attest itself as its living source.");
            }
        }

        [TestCase("FieldHaftComponent")]
        [TestCase("PlainCordBindingComponent")]
        [TestCase("TepuiboneHeadComponent")]
        [TestCase("BracedHaftComponent")]
        [TestCase("GuardLashingComponent")]
        [TestCase("FilterHood")]
        [TestCase("AcidworkerApron")]
        [TestCase("ColdwardCloak")]
        [TestCase("KnotflaxBandage")]
        [TestCase("ConcentratedMendleaf")]
        [TestCase("CleansedGrovePulp")]
        public void PreparationItemsResolveRegisteredPhysicalModelsWithoutChangingIdentity(string blueprint)
        {
            using (var f = new DensityLootTestScope())
            {
                var item = f.Factory.CreateEntity(blueprint);
                Assert.NotNull(item, "Authored item must exist before visual acceptance.");
                string id = item.ID;
                Assert.True(SpreadPortableRecipes.TryRecipe(item, out string model), blueprint);
                var library = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
                Assert.NotNull(library.FindModel(model), model);
                Assert.NotNull(library.Definition.FindModel(model), model);
                Assert.AreEqual(blueprint, item.BlueprintName);
                Assert.AreEqual(id, item.ID);
                item.GetPart<RenderPart>().VisualID = "foreign-override";
                Assert.False(SpreadPortableRecipes.TryRecipe(item, out _), "A visual alias must not claim a reskinned owner.");
            }
        }

        [TestCase("SteelBladeComponent")]
        [TestCase("IronSpikeComponent")]
        [TestCase("PeatMalletHeadComponent")]
        [TestCase("CinderhookAxeHeadComponent")]
        [TestCase("CounterweightLongBladeComponent")]
        [TestCase("TepuiboneHeadComponent")]
        public void NewHaftsAndBindingsKeepEveryAuthoredHeadVisible(string head)
        {
            using (var f = new DensityLootTestScope())
            {
                var library = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
                foreach (string haft in new[] { "FieldHaftComponent", "BracedHaftComponent" })
                foreach (string binding in new[] { "PlainCordBindingComponent", "GuardLashingComponent" })
                {
                    var item = f.Factory.CreateEntity("ForgedWeapon");
                    var assembly = new WeaponAssemblyPart { BladeBlueprint = head, HaftBlueprint = haft, BindingBlueprint = binding };
                    item.AddPart(assembly);
                    Assert.True(SpreadPortableRecipes.TryRecipe(item, out string model), head + "/" + haft + "/" + binding);
                    Assert.NotNull(library.FindModel(model), model);
                    Assert.AreEqual(head, assembly.BladeBlueprint);
                    Assert.AreEqual(haft, assembly.HaftBlueprint);
                    Assert.AreEqual(binding, assembly.BindingBlueprint);
                    assembly.BindingBlueprint = "foreign-binding";
                    Assert.False(SpreadPortableRecipes.TryRecipe(item, out _), "Unknown components must still refuse.");
                }
            }
        }

        [TestCase("FilterHood")]
        [TestCase("AcidworkerApron")]
        [TestCase("ColdwardCloak")]
        public void ActualBodyEquipAndFloorDropBothSubmitGeometry(string blueprint)
        {
            using (var f = new SpawnRing3DIntegrationFixture())
            {
                f.Set("FullReveal", true); // Geometry coverage is independent of generated lighting.
                f.CleanGear(f.Player);
                var item = f.Equip(f.Player, blueprint);
                f.Refresh();
                Assert.True(f.Equipment(f.Player, item, out var view), "Recipe=" + (SpreadEquipmentRecipes.TryRecipe(f.Player,item,out var fitted) ? fitted.ModelId : "refused"));
                Assert.True(SpawnRing3DIntegrationFixture.Drawn(view));
                Assert.True(InventorySystem.UnequipItem(f.Player, item));
                f.Refresh();
                Assert.False(f.Equipment(f.Player, item, out _));
                Assert.True(InventorySystem.Drop(f.Player, item, f.Zone));
                f.Refresh();
                Assert.True(f.Rendered(item));
                Assert.True(InventorySystem.Pickup(f.Player, item, f.Zone));
                f.Refresh();
                Assert.False(f.Rendered(item));
            }
        }

        [Test]
        public void StormbinderHasARealAnimatedBodyAndDisappearsWhenRemoved()
        {
            using (var f = new SpawnRing3DIntegrationFixture())
            {
                f.Set("FullReveal", true); // Geometry coverage is independent of generated lighting.
                var enemy = f.Add("MarlbackStormbinder");
                f.Refresh();
                Assert.True(f.Rendered(enemy));
                var view = f.View(enemy);
                Assert.True(SpawnRing3DIntegrationFixture.Drawn(view));
                var animator = view.GetComponentInChildren<Animator>(true);
                Assert.NotNull(animator);
                Assert.True(animator.runtimeAnimatorController.animationClips.Any(c => c.name == "Attack" && c.length > 0));
                f.Zone.RemoveEntity(enemy);
                f.Refresh();
                Assert.False(f.Rendered(enemy));
            }
        }
    }
}
