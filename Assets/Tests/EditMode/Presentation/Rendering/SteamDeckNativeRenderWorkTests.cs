using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CavesOfOoo.Tests
{
    public sealed class SteamDeckNativeRenderWorkTests
    {
        static Entity Plant(SpawnRing3DIntegrationFixture f)
        {
            foreach (var e in f.Zone.GetReadOnlyEntities().ToArray()) if (e != f.Player) f.Zone.RemoveEntity(e);
            var plant = f.Add("Reeds", 20, 10);
            plant.GetPart<RenderPart>().VisualID = "reference-glade-pale-reeds";
            f.Refresh(); return plant;
        }
        [Test] public void DisabledContactDoesNoContributorRasterOrUploadWorkAndResumesCurrentGeometry()
        {
            using (var f = new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
            {
                var plant = Plant(f);
                var material = f.Get<NativeZone3DRenderSurface>("ActiveSurface").MaterialFor(ReferenceGladeVoxelLibrary.Load().Material);
                var texture = (Texture2D)material.GetTexture("_GroundContact");
                Assert.Greater(texture.GetPixels32().Max(p => p.r), 70);
                Village3DSettings.LowDetail = true; f.Frame();
                int visits = f.Get<int>("ContactContributorVisitCount"), rasters = f.Get<int>("ContactRasterizeCount"), uploads = f.Get<int>("ContactUploadCount");
                var before = texture.GetPixels32();
                Assert.True(f.Zone.MoveEntity(plant, 24, 10)); f.Refresh(); f.Refresh();
                Assert.AreEqual(visits, f.Get<int>("ContactContributorVisitCount"));
                Assert.AreEqual(rasters, f.Get<int>("ContactRasterizeCount"));
                Assert.AreEqual(uploads, f.Get<int>("ContactUploadCount"));
                CollectionAssert.AreEqual(before, texture.GetPixels32()); Assert.Zero(material.GetFloat("_GroundContactStrength"));
                Village3DSettings.LowDetail = false; f.Frame();
                Assert.Greater(f.Get<int>("ContactContributorVisitCount"), visits);
                Assert.AreEqual(rasters + 1, f.Get<int>("ContactRasterizeCount"));
                Assert.AreEqual(uploads + 1, f.Get<int>("ContactUploadCount"));
                Assert.Greater(material.GetFloat("_GroundContactStrength"), 0);
                CollectionAssert.AreNotEqual(before, texture.GetPixels32());
                int stable = f.Get<int>("ContactUploadCount"); f.Frame(); Assert.AreEqual(stable, f.Get<int>("ContactUploadCount"));
            }
        }
        [Test] public void ContactReenableRechecksFogBeforeRestoringStrength()
        {
            using (var f = new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
            {
                var plant = Plant(f);
                var material = f.Get<NativeZone3DRenderSurface>("ActiveSurface").MaterialFor(ReferenceGladeVoxelLibrary.Load().Material);
                var texture = (Texture2D)material.GetTexture("_GroundContact");
                Assert.Greater(texture.GetPixels32().Max(p => p.r), 70);
                Village3DSettings.LowDetail = true; f.Frame();
                f.Zone.GetEntityCell(plant).IsVisible = false;
                Village3DSettings.LowDetail = false; f.Frame();
                Assert.Zero(texture.GetPixels32().Max(p => p.r), "A setting toggle must not briefly restore the obsolete visible footprint.");
            }
        }
        [Test] public void DirtyMovementResolvesOnlyOwnersInAffectedCellsAndKeepsOtherRoots()
        {
            using (var f = new SpawnRing3DIntegrationFixture())
            {
                var actor = f.Add("GlasspaneFrog"); var other = f.Add("GlasspaneFrog"); f.Refresh();
                var original = f.View(actor); var unrelated = f.View(other);
                var old = f.Zone.GetEntityPosition(actor); var next = f.FreeCell(old.x / 10, old.y / 5);
                var dirt = SpawnRing3DIntegrationFixture.Dirty(old.x, old.y); dirt.Add(next.y * Zone.Width + next.x);
                var affected = new HashSet<Entity>();
                foreach (int key in dirt)
                    for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                        if (Math.Abs(dx) + Math.Abs(dy) <= 1)
                        { var cell = f.Zone.GetCell(key % Zone.Width + dx, key / Zone.Width + dy); if (cell != null) affected.UnionWith(cell.Occupants); }
                int before = f.Get<int>("RecipeResolveCount");
                Assert.True(f.Zone.MoveEntity(actor, next.x, next.y)); f.Refresh(dirt);
                Assert.That(f.Get<int>("RecipeResolveCount") - before, Is.InRange(1, affected.Count));
                Assert.AreSame(original, f.View(actor)); Assert.AreSame(unrelated, f.View(other));
                Assert.AreEqual(Village3DProjection.CellCentre(next.x, next.y), original.transform.position);
                before = f.Get<int>("RecipeResolveCount"); f.Refresh();
                Assert.Greater(f.Get<int>("RecipeResolveCount") - before, affected.Count, "Null dirt must retain a complete recovery path.");
            }
        }
        [Test] public void VisibilityOnlyRefreshResolvesNoRecipesAndHidesThenRestoresTransientOwner()
        {
            using (var f = new SpawnRing3DIntegrationFixture())
            {
                var actor = f.Add("GlasspaneFrog"); f.Refresh(); var root = f.View(actor);
                int before = f.Get<int>("RecipeResolveCount"); int builds = f.Get<int>("GroundBuildCount");
                f.Zone.GetEntityCell(actor).IsVisible = false; f.Call("RefreshVisibility", f.Light);
                Assert.AreEqual(before, f.Get<int>("RecipeResolveCount")); Assert.AreEqual(builds, f.Get<int>("GroundBuildCount"));
                Assert.False(f.Rendered(actor)); SpawnRing3DIntegrationFixture.Hidden(root);
                f.Zone.GetEntityCell(actor).IsVisible = true; f.Call("RefreshVisibility", f.Light);
                Assert.True(f.Rendered(actor)); Assert.AreSame(root, f.View(actor)); Assert.AreEqual(before, f.Get<int>("RecipeResolveCount"));
            }
        }
        [Test] public void DirtyRemovalAddAndUnsignalledMembershipFallbackRemainExact()
        {
            using (var f = new SpawnRing3DIntegrationFixture())
            {
                var actor = f.Add("GlasspaneFrog"); f.Refresh(); var root = f.View(actor); var dirt = f.Dirty(actor);
                Assert.True(f.Zone.RemoveEntity(actor)); f.Refresh(dirt); Assert.False(f.Find(actor, out _, out _)); SpawnRing3DIntegrationFixture.Hidden(root);
                var next = f.Add("GlasspaneFrog"); f.Refresh(f.Dirty(next)); Assert.True(f.Rendered(next));
                var unsignalled = f.Add("GlasspaneFrog"); f.Refresh(new HashSet<int>());
                Assert.True(f.Rendered(unsignalled), "Version drift without geometry dirt requires conservative full recovery.");
            }
        }
        [Test] public void UniformStaticMaterialsNeedNoPropertyBlockButTransientPolicyDoes()
        {
            var library = Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath);
            var root = new GameObject("Deck material family test");
            try
            {
                using (var surface = new NativeZone3DRenderSurface(root.transform, library.Renderer, library.RendererIndex,
                    library.CompositeMaterial, new[] { library.WorldMaterial, library.WaterMaterial }, 2.2f))
                {
                    var model = Object.Instantiate(library.Models.First(b => b.Id == "central-well").Prefab, surface.ContentRoot, false);
                    surface.PrepareModel(model, false);
                    foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
                    {
                        Assert.False(renderer.HasPropertyBlock(), "Uniform static visibility must be supplied by its shared owned material family.");
                        Assert.Zero(renderer.sharedMaterial.GetFloat("_Transient"));
                    }
                    surface.PrepareModel(model, true);
                    foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
                    {
                        var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block);
                        Assert.AreEqual(1, block.GetFloat("_Transient"));
                    }
                }
            }
            finally { Object.DestroyImmediate(root); }
        }
        [Test] public void StaticPreparationPreservesTintHeadwearIndexedDataAndOwnedAmbient()
        {
            var library = Resources.Load<Village3DLibrary>(Village3DLibrary.ResourcePath);
            var root = new GameObject("Deck material exceptions test");
            try
            {
                using (var surface = new NativeZone3DRenderSurface(root.transform, library.Renderer, library.RendererIndex,
                    library.CompositeMaterial, new[] { library.WorldMaterial, library.WaterMaterial }, 2.2f))
                {
                    surface.ConfigureAmbientProbe(new Color(.28f, .32f, .30f));
                    var model = Object.Instantiate(library.Models.First(b => b.Id == "central-well").Prefab, surface.ContentRoot, false);
                    var renderer = model.GetComponentInChildren<Renderer>(true);
                    var block = new MaterialPropertyBlock(); block.SetColor("_BaseColor", Color.red); block.SetFloat("_CoverHeadwear", 1);
                    renderer.SetPropertyBlock(block); block.Clear(); block.SetFloat("_DeckProbe", .7f); renderer.SetPropertyBlock(block, 0);
                    surface.PrepareModel(model, false); renderer.GetPropertyBlock(block);
                    Assert.AreEqual(Color.red, block.GetColor("_BaseColor")); Assert.AreEqual(1, block.GetFloat("_CoverHeadwear"));
                    Assert.AreEqual(UnityEngine.Rendering.LightProbeUsage.CustomProvided, renderer.lightProbeUsage);
                    renderer.GetPropertyBlock(block, 0); Assert.AreEqual(.7f, block.GetFloat("_DeckProbe")); Assert.Zero(block.GetFloat("_Transient"));
                }
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
