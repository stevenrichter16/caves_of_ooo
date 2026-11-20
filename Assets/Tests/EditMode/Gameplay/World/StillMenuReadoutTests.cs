using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// A still's interact (C) menu says what the mix is and what it would make, as much as the pack's Crafting panel does: the picks, the result's name, each effect with its potency, the
    /// form, why a mix makes nothing, and the size of a full batch. It reads the same preview the pack draws, so the two cannot disagree.
    /// </summary>
    public sealed class StillMenuReadoutTests : FiftyWorldFixture
    {
        Entity Reagent(string blueprint, bool pick) { var r = Carry(blueprint); if (pick) Assert.True(CraftingMarkPart.Toggle(r)); return r; }
        List<string> Status(Entity still) => WorldActionMenuUI.BuildStatusLinesFor(still, Zone.GetEntityCell(still), Zone, Actor);

        // ── the wording, without a world ──

        [Test] public void NothingPickedSaysSoAndWhereToPick()
        {
            var lines = BrewMenuReadout.Format(new List<Entity>(), default, 0); Assert.AreEqual(1, lines.Count); StringAssert.Contains("nothing picked", lines[0]); StringAssert.Contains("Pick reagents", lines[0]);
            Assert.AreEqual(1, BrewMenuReadout.Format(null, default, 0).Count, "a null mix is an empty mix");
        }

        [Test] public void AMixThatMakesNothingGivesTheReasonTheRulesGave()
        {
            var lines = BrewMenuReadout.Format(new List<Entity> { Factory.CreateEntity("MendleafSprig") }, new BrewPreview { IsValid = false, Reason = "These do not mix." }, 0);
            Assert.AreEqual(2, lines.Count); StringAssert.Contains("mendleaf", lines[0].ToLowerInvariant()); Assert.AreEqual("Makes nothing: These do not mix.", lines[1]);
            Assert.AreEqual("Makes nothing: These reagents make nothing.", BrewMenuReadout.Format(new List<Entity> { Factory.CreateEntity("MendleafSprig") }, new BrewPreview { IsValid = false }, 0)[1], "a missing reason still says something");
        }

        [Test] public void AValidMixShowsTheNameEveryEffectWithItsPotencyTheFormAndTheBatch()
        {
            var preview = new BrewPreview { IsValid = true, DisplayName = "Healing Tonic", Form = "Tonic", Effects = new[] { new BrewPropertyAmount("Healing", 6), new BrewPropertyAmount("Poison", 2) } };
            var lines = BrewMenuReadout.Format(new List<Entity> { Factory.CreateEntity("MendleafSprig"), Factory.CreateEntity("FireMoss") }, preview, 3);
            CollectionAssert.AreEqual(new[] { "Makes: Healing Tonic", "Effects: Healing 6, Poison 2 (tonic)", "A full batch brews 3." }, lines.Skip(1).ToArray());
            Assert.AreEqual(3, BrewMenuReadout.Format(new List<Entity> { Factory.CreateEntity("MendleafSprig") }, preview, 1).Count, "a batch of one is not worth a line");
            Assert.True(BrewMenuReadout.Format(new List<Entity> { Factory.CreateEntity("MendleafSprig") }, new BrewPreview { IsValid = true, DisplayName = "Dud", Effects = new BrewPropertyAmount[0] }, 0).All(l => !l.StartsWith("Effects")), "no effects and no form: no effects line");
        }

        // ── the real mix at a real still ──

        [Test] public void TheStillsMenuStatusMatchesWhatThePackPanelWouldPreview()
        {
            var still = Place("AlchemyStill"); Reagent("MendleafSprig", true); Reagent("FireMoss", true);
            var picks = CraftingMarkPart.CollectMarked(Actor).Reagents; var preview = BrewingService.PreviewBrew(picks); var lines = Status(still);
            StringAssert.Contains("In the mix:", lines[0]); Assert.True(preview.IsValid, preview.Reason);
            Assert.True(lines.Contains("Makes: " + preview.DisplayName), string.Join(" / ", lines));
            foreach (var e in preview.Effects) Assert.True(lines.Any(l => l.StartsWith("Effects:") && l.Contains(e.Property + " " + e.Potency)), e.Property);
        }

        [Test] public void ThePicksDecideTheLines_AddingAReagentChangesThemAndUnpickingEmptiesThem()
        {
            var still = Place("AlchemyStill"); var a = Reagent("MendleafSprig", false); var b = Reagent("FireMoss", false);
            StringAssert.Contains("nothing picked", Status(still)[0]);
            CraftingMarkPart.Toggle(a); var one = Status(still); CraftingMarkPart.Toggle(b); var two = Status(still);
            CollectionAssert.AreNotEqual(one, two, "a second reagent changes the prediction the menu shows"); StringAssert.Contains("In the mix:", two[0]);
            CraftingMarkPart.Toggle(a); CraftingMarkPart.Toggle(b); StringAssert.Contains("nothing picked", Status(still)[0]);
        }

        [Test] public void OnlyAStillSaysItAndOnlyToSomeoneWhoCanBrew_Counters()
        {
            var still = Place("AlchemyStill"); Reagent("MendleafSprig", true);
            Assert.False(WorldActionMenuUI.BuildStatusLinesFor(still, Zone.GetEntityCell(still), Zone).Any(l => l.StartsWith("In the mix")), "no viewer, no mix");
            var fire = Place("Campfire", 12, 10); Assert.False(WorldActionMenuUI.BuildStatusLinesFor(fire, Zone.GetEntityCell(fire), Zone, Actor).Any(l => l.StartsWith("In the mix")), "a fire is not a still");
            var kit = Carry("CampStill"); var lines = new List<string>(); StateReader.CollectMenuStatus(kit, Zone, null, Actor, lines); Assert.IsEmpty(lines, "a carried still is a kit and brews nothing");
        }

        [Test] public void AStillOnAPileCellKeepsThePileSummaryClean()
        {
            var still = Place("AlchemyStill"); Reagent("MendleafSprig", true); Place("Dagger", 11, 10); var cell = Zone.GetEntityCell(still);
            Assert.True(WorldInteractionSystem.IsPileCell(cell), "precondition: the still shares its cell with loot"); Assert.False(WorldActionMenuUI.BuildStatusLinesFor(still, cell, Zone, Actor).Any(l => l.StartsWith("In the mix")));
        }

        // ── the rows ──

        [Test] public void TheBrewRowsDescribeThemselves_AndTheBatchRowNamesTheSize()
        {
            var still = Place("AlchemyStill"); var rows = Actions(still); var brew = rows.Single(a => a.Name == "Brew"); var batch = rows.Single(a => a.Name == "BrewBatch");
            StringAssert.Contains("Pick reagents", brew.Hint); Assert.AreEqual("brew a full batch", batch.Display, "nothing picked: no size to show");
            Reagent("MendleafSprig", true); Reagent("MendleafSprig", true); rows = Actions(still); batch = rows.Single(a => a.Name == "BrewBatch"); brew = rows.Single(a => a.Name == "Brew");
            int max = BrewingService.GetMaxBatchCount(CraftingMarkPart.CollectMarked(Actor).Reagents);
            Assert.IsNotEmpty(brew.Hint); if (max > 1) { StringAssert.Contains("up to " + max, batch.Display); StringAssert.Contains("up to " + max, batch.Hint); } else Assert.AreEqual("brew a full batch", batch.Display);
        }
    }
}
