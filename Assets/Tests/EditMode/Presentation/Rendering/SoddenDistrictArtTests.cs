using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Actual saved owners and imported meshes. These tests deliberately
    /// precede the new art library and fail until its real assets are imported.</summary>
    public sealed class SoddenDistrictArtTests
    {
        const string Prefix = "sodden-district-", Folder = "SoddenDistrict3D/";
        const string Stop = "Overworld.15.7.0", Works = "Overworld.17.7.0";
        static readonly string[] Models = { "bench-broken", "bench-working", "works-salvage", "works-locker", "route-notice", "field-dressing" };
        [Test] public void DistrictRecipeClaimsOnlyItsFiveExactNativeOwners()
        {
            foreach (string blueprint in new[] { "SoddenDressingBench", "SoddenWorksSalvage", "SoddenWorksLocker", "SoddenRouteNotice", "SoddenFieldDressing" })
                Assert.IsTrue(SoddenDistrictRecipes.Handles(blueprint), blueprint);
            foreach (string blueprint in new[] { "Chest", "RepairTimberPile", "Signpost", "Tonic", "soddendressingbench", "", null })
                Assert.IsFalse(SoddenDistrictRecipes.Handles(blueprint), blueprint);
        }
        static SpawnRing3DRecipe Recipe(SpawnRing3DIntegrationFixture f, Entity owner)
            => SpawnRing3DRecipes.Resolve(f.Zone, owner, f.Library.Definition);

        static Mesh Exact(SpawnRing3DIntegrationFixture f, Entity owner, string suffix)
        {
            var recipe = Recipe(f, owner);
            Assert.AreSame(owner, recipe.Owner);
            Assert.IsNull(recipe.Failure, recipe.Failure);
            Assert.AreEqual(Prefix + suffix, recipe.ModelId);
            f.Refresh(f.Dirty(owner));
            Assert.IsTrue(f.Rendered(owner), suffix);
            var prefab = f.Library.FindModel(recipe.ModelId);
            Assert.NotNull(prefab, recipe.ModelId);
            Assert.NotNull(f.Library.Definition.FindModel(recipe.ModelId));
            return prefab.GetComponent<MeshFilter>().sharedMesh;
        }

        static Entity Add(SpawnRing3DIntegrationFixture f, string blueprint, int x = -1, int y = -1)
        {
            Assert.IsTrue(f.Factory.Blueprints.ContainsKey(blueprint), "Real native owner blueprint required: " + blueprint);
            return f.Add(blueprint, x, y);
        }

        [Test] public void SixOriginalFormsUseBuildVisibleInertSingleCellGeometry()
        {
            var library = Resources.Load<ScriptableObject>(Folder + "Library");
            Assert.NotNull(library, "Record RED before building the six original Sodden district meshes.");
            var validate = library.GetType().GetMethod("Validate");
            Assert.NotNull(validate); validate.Invoke(library, null);
            var entries = library.GetType().GetField("Entries");
            Assert.NotNull(entries); Assert.AreEqual(6, ((Array)entries.GetValue(library)).Length);
            var ring = Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
            foreach (string model in Models)
            {
                string id = Prefix + model;
                var prefab = Resources.Load<GameObject>(Folder + id);
                Assert.NotNull(prefab, id);
                Assert.AreSame(prefab, ring.FindModel(id), id);
                var spec = ring.Definition.FindModel(id); Assert.NotNull(spec, id);
                var filters = prefab.GetComponentsInChildren<MeshFilter>(true);
                var renderers = prefab.GetComponentsInChildren<MeshRenderer>(true);
                Assert.AreEqual(1, filters.Length, id); Assert.AreEqual(1, renderers.Length, id);
                var mesh = filters[0].sharedMesh;
                Assert.NotNull(mesh); Assert.IsTrue(mesh.isReadable);
                Assert.That(mesh.vertexCount, Is.InRange(72, 720), id);
                Assert.That(mesh.uv.Distinct().Count(), Is.InRange(2, 4), id);
                Assert.GreaterOrEqual(mesh.bounds.min.x, -.5001f, id); Assert.LessOrEqual(mesh.bounds.max.x, .5001f, id);
                Assert.GreaterOrEqual(mesh.bounds.min.z, -.5001f, id); Assert.LessOrEqual(mesh.bounds.max.z, .5001f, id);
                Assert.AreEqual(Vector3.zero, prefab.transform.localPosition); Assert.AreEqual(Vector3.one, prefab.transform.localScale);
                Assert.AreEqual(Quaternion.identity, prefab.transform.localRotation);
                Assert.AreSame(ring.WorldMaterial, renderers[0].sharedMaterial);
                Assert.AreEqual(1, renderers[0].sharedMaterials.Length);
                Assert.IsEmpty(prefab.GetComponentsInChildren<Collider>(true));
                Assert.IsEmpty(prefab.GetComponentsInChildren<Rigidbody>(true));
                Assert.IsEmpty(prefab.GetComponentsInChildren<MonoBehaviour>(true));
                Assert.IsEmpty(prefab.GetComponentsInChildren<Light>(true));
                Assert.AreEqual(mesh.bounds.center, spec.boundsCenter); Assert.AreEqual(mesh.bounds.size, spec.boundsSize);
                Assert.AreEqual(mesh.triangles.Length / 3, spec.triangles); Assert.AreEqual("ring-palette", spec.materialFamily);
            }
        }

        [Test] public void BenchRepairChangesSubmittedShapeAndRoundTripsWithoutRevivingOldOwner()
        {
            using (var f = new SpawnRing3DIntegrationFixture(Stop))
            {
                f.Set("FullReveal", true);
                var owner = Add(f, "SoddenDressingBench");
                var repair = owner.GetPart<RepairablePart>(); Assert.NotNull(repair); Assert.IsFalse(repair.Repaired);
                var broken = Exact(f, owner, "bench-broken");
                var at = f.Zone.GetEntityPosition(owner); var far = f.FreeCell(at.x / 10, at.y / 5);
                var control = Add(f, "SoddenRouteNotice", far.x, far.y); Exact(f, control, "route-notice");
                int previous = f.Revision(at.x, at.y), untouched = f.Revision(far.x, far.y);
                repair.Repaired = true;
                var working = Exact(f, owner, "bench-working");
                Assert.IsFalse(broken.vertices.SequenceEqual(working.vertices), "Repair must change the actual frame, not only metadata or paint.");
                Assert.Greater(f.Revision(at.x, at.y), previous); Assert.AreEqual(untouched, f.Revision(far.x, far.y));
                Assert.IsTrue(f.Rendered(control));
                string id = owner.ID; f.BindLoaded(f.RoundTrip());
                var replacement = f.Zone.GetReadOnlyEntities().Single(e => e.ID == id);
                Assert.AreEqual(at, f.Zone.GetEntityPosition(replacement)); Exact(f, replacement, "bench-working");
                Assert.IsNull(Recipe(f, owner).ModelId, "Stale pre-load reference must never draw its replacement.");
                repair = replacement.GetPart<RepairablePart>(); repair.Repaired = false;
                Assert.AreSame(broken, Exact(f, replacement, "bench-broken"));
            }
        }

        [TestCase("SoddenWorksSalvage", "works-salvage")]
        [TestCase("SoddenWorksLocker", "works-locker")]
        [TestCase("SoddenRouteNotice", "route-notice")]
        public void StaticOwnersDisappearOnRemovalWithoutRestoringFromThePlan(string blueprint, string model)
        {
            using (var f = new SpawnRing3DIntegrationFixture(Works))
            {
                f.Set("FullReveal", true);
                var owner = Add(f, blueprint); Exact(f, owner, model);
                var at = f.Zone.GetEntityPosition(owner); var far = f.FreeCell(at.x / 10, at.y / 5);
                var control = Add(f, blueprint, far.x, far.y); Exact(f, control, model);
                int distant = f.Revision(far.x, far.y);
                Assert.IsTrue(f.Zone.RemoveEntity(owner)); f.Refresh(new HashSet<int>());
                Assert.IsNull(Recipe(f, owner).ModelId); Assert.IsFalse(f.Rendered(owner)); Assert.IsFalse(f.Authored(owner));
                Assert.IsTrue(f.Rendered(control)); Assert.AreEqual(distant, f.Revision(far.x, far.y));
                f.Refresh(); Assert.IsNull(f.Zone.GetEntityCell(owner)); Assert.IsFalse(f.Rendered(owner));
            }
        }

        [Test] public void RetainedHarvestedSalvageCannotStillAdvertiseTimber()
        {
            using (var f = new SpawnRing3DIntegrationFixture(Works))
            {
                var owner = Add(f, "SoddenWorksSalvage"); Exact(f, owner, "works-salvage");
                var harvest = owner.GetPart<HarvestablePart>(); Assert.NotNull(harvest);
                harvest.Harvested = true; Assert.IsNull(Recipe(f, owner).ModelId);
                f.Refresh(new HashSet<int>()); Assert.IsFalse(f.Rendered(owner));
                harvest.Harvested = false; Exact(f, owner, "works-salvage");
            }
        }

        [TestCase(SpawnRing3DIntegrationFixture.Grove)] [TestCase(Stop)]
        public void DressingKeepsItsPortableShapeAfterPickupAndDropOutsideItsOrigin(string zoneId)
        {
            using (var f = new SpawnRing3DIntegrationFixture(zoneId))
            {
                var owner = Add(f, "SoddenFieldDressing"); var mesh = Exact(f, owner, "field-dressing");
                Assert.IsTrue(Recipe(f, owner).Transient); Assert.IsFalse(Recipe(f, owner).Batched);
                f.Approach(owner); Assert.IsTrue(InventorySystem.Pickup(f.Player, owner, f.Zone));
                f.Refresh(); Assert.IsNull(Recipe(f, owner).ModelId); Assert.IsFalse(f.Rendered(owner));
                Assert.IsTrue(InventorySystem.Drop(f.Player, owner, f.Zone)); Assert.AreSame(mesh, Exact(f, owner, "field-dressing"));
            }
        }

        [TestCase("hidden")] [TestCase("glyph")] [TestCase("color")] [TestCase("custom-visual")]
        [TestCase("foreign-render")] [TestCase("foreign-physics")] [TestCase("foreign-repair")]
        [TestCase("foreign-cell")] [TestCase("foreign-spatial")] [TestCase("carried")]
        [TestCase("takeable")] [TestCase("creature")] [TestCase("wrong-recipe")]
        public void InvalidBenchCannotBorrowAWorkingServiceAppearance(string fault)
        {
            using (var f = new SpawnRing3DIntegrationFixture(Stop))
            {
                var owner = Add(f, "SoddenDressingBench"); Exact(f, owner, "bench-broken");
                var render = owner.GetPart<RenderPart>(); var physics = owner.GetPart<PhysicsPart>();
                var repair = owner.GetPart<RepairablePart>(); var cell = f.Zone.GetEntityCell(owner);
                var spatial = typeof(Entity).GetField("SpatialZone", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(spatial); object prior = spatial.GetValue(owner); var parentZone = cell.ParentZone;
                try
                {
                    switch (fault)
                    {
                        case "hidden": render.Visible = false; break;
                        case "glyph": render.RenderString = "?"; break;
                        case "color": render.ColorString = "&M"; break;
                        case "custom-visual": render.VisualID = "foreign"; break;
                        case "foreign-render": render.ParentEntity = f.Player; break;
                        case "foreign-physics": physics.ParentEntity = f.Player; break;
                        case "foreign-repair": repair.ParentEntity = f.Player; break;
                        case "foreign-cell": cell.ParentZone = new Zone(f.Zone.ZoneID); break;
                        case "foreign-spatial": spatial.SetValue(owner, new Zone(f.Zone.ZoneID)); break;
                        case "carried": physics.InInventory = f.Player; break;
                        case "takeable": physics.Takeable = true; break;
                        case "creature": owner.SetTag("Creature"); break;
                        case "wrong-recipe": repair.RecipeId = "rope-well-line"; break;
                    }
                    Assert.IsNull(Recipe(f, owner).ModelId, fault);
                }
                finally { render.ParentEntity = owner; physics.ParentEntity = owner; repair.ParentEntity = owner; cell.ParentZone = parentZone; spatial.SetValue(owner, prior); }
            }
        }

        [Test] public void AllImportedDistrictMeshesRemainBorrowedWithoutSecondVoxelization()
        {
            bool enabled = Village3DSettings.Enabled; Village3DSettings.Enabled = true;
            try
            {
                foreach (string zoneId in new[] { Stop, Works, SpawnRing3DIntegrationFixture.Grove })
                {
                    var bridge = VoxelWorldPresentation.ForZone(new Zone(zoneId)); Assert.NotNull(bridge);
                    foreach (string model in Models)
                    {
                        var prefab = Resources.Load<GameObject>(Folder + Prefix + model); Assert.NotNull(prefab, model);
                        var mesh = prefab.GetComponent<MeshFilter>().sharedMesh; Assert.AreSame(mesh, bridge.Resolve(mesh), zoneId + " " + model);
                    }
                    Assert.AreEqual(0, bridge.MissingMeshCount); Assert.AreEqual(0, bridge.AppliedMeshCount);
                }
            }
            finally { Village3DSettings.Enabled = enabled; }
        }

        [TestCase(Stop)] [TestCase("Overworld.16.7.0")] [TestCase(Works)]
        public void GeneratedDistrictRetainsAModelForEveryActualVisibleOwner(string zoneId)
        {
            using (var f = new SpawnRing3DIntegrationFixture(zoneId))
            {
                f.Set("FullReveal", true); f.Refresh();
                var missing = new SortedSet<string>();
                foreach (var owner in f.Zone.GetReadOnlyEntities())
                {
                    if (owner.GetPart<RenderPart>()?.Visible != true) continue;
                    var recipe = Recipe(f, owner);
                    if (recipe.ModelId == null) missing.Add(owner.BlueprintName + ": " + recipe.Failure);
                    else { Assert.AreSame(owner, recipe.Owner); Assert.NotNull(f.Library.FindModel(recipe.ModelId), recipe.ModelId); }
                }
                Assert.IsEmpty(missing, string.Join("\n", missing));
                Assert.AreEqual(0, f.Get<int>("VoxelMissingMeshCount"));
            }
        }

        [TestCase("StoneFloor", "wellmeet-floor-")] [TestCase("StoneWall", "sumphold-wall-")]
        [TestCase("PeatCutter", "spread-person-peat-cutter")] [TestCase("Bandfrog", "spread-visitor-bandfrog")]
        [TestCase("Bed", "wellmeet-bed-")] [TestCase("Chair", "wellmeet-chair-")]
        public void ExistingNativeFormsKeepTheirExactOwnersWhenReusedInTheDistrict(string blueprint, string prefix)
        {
            using (var f = new SpawnRing3DIntegrationFixture(Stop))
            {
                f.Set("FullReveal", true); var owner = f.Add(blueprint); var recipe = Recipe(f, owner);
                Assert.AreSame(owner, recipe.Owner); StringAssert.StartsWith(prefix, recipe.ModelId);
                f.Refresh(); Assert.IsTrue(f.Rendered(owner)); Assert.AreEqual(0, f.Get<int>("VoxelMissingMeshCount"));
                owner.GetPart<RenderPart>().Visible = false; f.Refresh(new HashSet<int>());
                Assert.IsNull(Recipe(f, owner).ModelId); Assert.IsFalse(f.Rendered(owner));
                owner.GetPart<RenderPart>().Visible = true; f.Refresh(); Assert.IsTrue(f.Rendered(owner));
                Assert.IsTrue(f.Zone.RemoveEntity(owner)); f.Refresh(new HashSet<int>());
                Assert.IsNull(Recipe(f, owner).ModelId); Assert.IsFalse(f.Rendered(owner));
            }
        }

        [TestCase("Bed", "glyph")] [TestCase("Chair", "glyph")]
        [TestCase("Bed", "foreign-furniture")] [TestCase("Chair", "foreign-furniture")]
        [TestCase("Bed", "solid")] [TestCase("Chair", "solid")]
        public void MalformedShelterFurnitureCannotBorrowTheNativeUsableForm(string blueprint, string fault)
        {
            using (var f = new SpawnRing3DIntegrationFixture(Stop))
            {
                var owner = f.Add(blueprint);
                StringAssert.StartsWith(blueprint == "Bed" ? "wellmeet-bed-" : "wellmeet-chair-", Recipe(f, owner).ModelId,
                    "Real positive owner required before the countercheck.");
                var part = owner.GetPart(blueprint); Assert.NotNull(part);
                try
                {
                    if (fault == "glyph") owner.GetPart<RenderPart>().RenderString = "?";
                    if (fault == "foreign-furniture") part.ParentEntity = f.Player;
                    if (fault == "solid") owner.GetPart<PhysicsPart>().Solid = true;
                    Assert.IsNull(Recipe(f, owner).ModelId, fault);
                }
                finally { part.ParentEntity = owner; }
            }
        }
    }
}
