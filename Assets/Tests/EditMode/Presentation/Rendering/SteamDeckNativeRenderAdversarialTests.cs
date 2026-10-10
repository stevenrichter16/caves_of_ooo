using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class SteamDeckNativeRenderAdversarialTests
    {
        [TestCase(0, 0)][TestCase(79, 0)][TestCase(0, 24)][TestCase(79, 24)]
        public void DirtyBoundaryAdditionAndRemovalNeverWrapToAnotherRow(int x, int y)
        {
            using (var f = new SpawnRing3DIntegrationFixture())
            {
                var actor = f.Add("GlasspaneFrog", x, y); var dirt = SpawnRing3DIntegrationFixture.Dirty(x, y);
                f.Refresh(dirt); Assert.True(f.Rendered(actor)); var root = f.View(actor);
                Assert.True(f.Zone.RemoveEntity(actor)); f.Refresh(dirt);
                Assert.False(f.Find(actor, out _, out _)); SpawnRing3DIntegrationFixture.Hidden(root);
            }
        }
        [TestCase(false, "hide")][TestCase(false, "remove")][TestCase(false, "move")][TestCase(false, "reveal")]
        [TestCase(true, "hide")][TestCase(true, "remove")][TestCase(true, "move")][TestCase(true, "reveal")]
        public void SuspendedContactReadsTheLatestOwnerAndRevealState(bool hideSurface, string mutation)
        {
            using (var f = new SpawnRing3DIntegrationFixture(ReferenceGladePlan.ZoneID))
            {
                foreach (var e in f.Zone.GetReadOnlyEntities().ToArray()) if (e != f.Player) f.Zone.RemoveEntity(e);
                var plant = f.Add("Reeds", 20, 10); plant.GetPart<RenderPart>().VisualID = "reference-glade-pale-reeds"; f.Refresh();
                var material = f.Get<NativeZone3DRenderSurface>("ActiveSurface").MaterialFor(ReferenceGladeVoxelLibrary.Load().Material);
                var texture = (Texture2D)material.GetTexture("_GroundContact"); var pixels = texture.GetPixels32();
                Assert.Greater(pixels.Max(p => p.r), 70);
                if (hideSurface) f.Call("SetPresentationVisible", false); else { Village3DSettings.LowDetail = true; f.Frame(); }
                int uploads = f.Get<int>("ContactUploadCount"), visits = f.Get<int>("ContactContributorVisitCount");
                if (mutation == "hide") plant.GetPart<RenderPart>().Visible = false;
                if (mutation == "remove") Assert.True(f.Zone.RemoveEntity(plant));
                if (mutation == "move") Assert.True(f.Zone.MoveEntity(plant, 24, 10));
                if (mutation == "reveal") { f.Zone.GetEntityCell(plant).IsVisible = false; f.Set("FullReveal", true); }
                f.Refresh(); Assert.AreEqual(uploads, f.Get<int>("ContactUploadCount")); Assert.AreEqual(visits, f.Get<int>("ContactContributorVisitCount"));
                if (hideSurface) f.Call("SetPresentationVisible", true); else { Village3DSettings.LowDetail = false; f.Frame(); }
                if (mutation == "hide" || mutation == "remove") Assert.Zero(texture.GetPixels32().Max(p => p.r));
                else Assert.Greater(texture.GetPixels32().Max(p => p.r), 70);
                if (mutation == "move") CollectionAssert.AreNotEqual(pixels, texture.GetPixels32());
                else if (mutation == "reveal") CollectionAssert.AreEqual(pixels, texture.GetPixels32());
            }
        }
        [TestCase("hide")][TestCase("glyph")][TestCase("remove")][TestCase("move")]
        public void DirtyOwnerMutationRetiresOldOutputWithoutResolvingTheWholeZone(string mutation)
        {
            using (var f = new SpawnRing3DIntegrationFixture())
            {
                var actor = f.Add("GlasspaneFrog"); f.Refresh(); var root = f.View(actor); var dirt = f.Dirty(actor);
                int before = f.Get<int>("RecipeResolveCount"), total = f.Zone.GetReadOnlyEntities().Count;
                if (mutation == "hide") actor.GetPart<RenderPart>().Visible = false;
                if (mutation == "glyph") actor.GetPart<RenderPart>().RenderString = "?";
                if (mutation == "remove") Assert.True(f.Zone.RemoveEntity(actor));
                if (mutation == "move") { var p = f.FreeCell(); Assert.True(f.Zone.MoveEntity(actor, p.x, p.y)); dirt.Add(p.y * Zone.Width + p.x); }
                f.Refresh(dirt); Assert.Less(f.Get<int>("RecipeResolveCount") - before, total / 2);
                if (mutation == "move") { Assert.True(f.Rendered(actor)); Assert.AreSame(root, f.View(actor)); }
                else { Assert.False(f.Find(actor, out _, out _)); SpawnRing3DIntegrationFixture.Hidden(root); }
            }
        }
        [TestCase(false)][TestCase(true)]
        public void ExplicitVisibilityChangeHonorsRevealWithoutGeometryResolution(bool reveal)
        {
            using (var f = new SpawnRing3DIntegrationFixture())
            {
                var actor = f.Add("GlasspaneFrog"); f.Refresh(); int before = f.Get<int>("RecipeResolveCount");
                f.Zone.GetEntityCell(actor).IsVisible = false; f.Set("FullReveal", reveal); f.Call("RefreshVisibility", f.Light);
                Assert.AreEqual(reveal, f.Rendered(actor)); Assert.AreEqual(before, f.Get<int>("RecipeResolveCount"));
            }
        }
        [TestCase(false)][TestCase(true)]
        public void CardinalDependencyRebuildsNeighborWhenOnlyRemovedWallCellIsDirty(bool remove)
        {
            using (var f = new SpawnRing3DIntegrationFixture(FirstTentCompositionPlan.ZoneID))
            {
                foreach (var e in f.Zone.GetReadOnlyEntities().ToArray()) if (e.BlueprintName == "TentWall") f.Zone.RemoveEntity(e);
                var center = f.Add("TentWall", 19, 10); var east = f.Add("TentWall", 20, 10); f.Add("TentWall", 19, 9); f.Refresh();
                Assert.True(f.Find(center, out _, out string before));
                var original = SpawnRing3DRecipes.Resolve(f.Zone, center, f.Library.Definition);
                int revision = f.Revision(19, 10);
                if (remove) Assert.True(f.Zone.RemoveEntity(east)); else east.GetPart<RenderPart>().Visible = false;
                f.Refresh(SpawnRing3DIntegrationFixture.Dirty(20, 10));
                var expected = SpawnRing3DRecipes.Resolve(f.Zone, center, f.Library.Definition);
                Assert.True(original.ModelId != expected.ModelId || original.QuarterTurns != expected.QuarterTurns,
                    "Precondition: this art family must actually depend on the removed neighboring wall.");
                Assert.True(f.Find(center, out _, out string actual)); Assert.AreEqual(expected.ModelId, actual);
                Assert.Greater(f.Revision(19, 10), revision, "The neighboring patch must observe connection/rotation changes across its boundary.");
                Assert.True(f.Rendered(center));
            }
        }
    }
}
