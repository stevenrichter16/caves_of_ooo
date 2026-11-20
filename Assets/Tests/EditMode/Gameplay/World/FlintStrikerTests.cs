using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// The flint striker (Docs/MACHINE-INVENTED-USES.md, "Spark"): a finite tool that makes ignition where no flame burns. It is one more answer to the shared "is there a flame in
    /// reach" lookup (<c>IgnitionSources</c>), so the torch and the finite cooking fire accept it with no verb of their own; and it can put one point of heat into an adjacent cell
    /// through the same routine every fire ability uses, so an oil slick burns by the shared tile reactions and nothing about oil is written here. Every claim runs through the real
    /// shipped blueprints and the real pack verbs.
    /// </summary>
    public sealed class FlintStrikerTests : FiftyWorldFixture
    {
        const string Oil = "oil";

        [SetUp] public void SetupStriker()
        {
            Diag.ResetAll(); MessageLog.Clear();
            TileReactionSystem.ResetForTests();
            TileReactionSystem.Initialize(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/TileReactions/Reactions.json")));
        }
        [TearDown] public void TearDownStriker() { TileReactionSystem.ResetForTests(); }

        Entity Striker(int strikes = 20) { var s = Carry("FlintStriker"); s.GetPart<FlintStrikerPart>().Strikes = strikes; return s; }
        int Strikes(Entity s) => s.GetPart<FlintStrikerPart>().Strikes;
        Entity DarkTorch(int x = 11, int y = 10) { var t = Place("Torch", x, y); Assert.True(Act(t, "ExtinguishTorch")); return t; }
        Entity ColdCoals() { var c = Place("SpreadCookingCoals"); c.GetPart<FuelPart>().FuelMass = 10; c.GetPart<ThermalPart>().Temperature = 25; return c; }
        string SparkRow(Entity striker, string direction) => Actions(striker).Single(a => a.Name == "StrikeSpark" && a.Display == "strike a spark " + direction).Command;
        int Records(string kind) => DiagQuery.Apply(new DiagQuery.Filter { Category = "event", Kind = kind, Limit = 50 }).Records.Count;

        // ── the blueprint is a small finite tool, not a fire ──

        [Test] public void TheShippedBlueprintIsASmallFiniteToolAndNotAFireOrAStack()
        {
            var s = Factory.CreateEntity("FlintStriker"); Assert.NotNull(s); var part = s.GetPart<FlintStrikerPart>(); Assert.NotNull(part);
            Assert.AreEqual(20, part.Strikes); Assert.AreEqual(20, part.MaxStrikes); Assert.True(s.GetPart<PhysicsPart>().Takeable); Assert.LessOrEqual(s.GetPart<PhysicsPart>().Weight, 2);
            Assert.False(s.HasPart<StackerPart>(), "a tool with its own wear never merges into a stack that would average it away");
            Assert.False(s.HasPart<ThermalPart>() || s.HasPart<FuelPart>() || s.HasPart<LightSourcePart>(), "it is not itself a flame");
            Assert.That(s.GetPart<CommercePart>().Value, Is.InRange(5, 40));
        }

        // ── the shared ignition lookup ──

        [Test] public void ATorchLightsFromTheStrikerWhereNoFireBurnsAndSpendsOneStrike()
        {
            var torch = DarkTorch(); var s = Striker(); Assert.False(torch.GetPart<ThermalPart>().IsAflame);
            Assert.True(Act(torch, "LightTorch"), string.Join("|", MessageLog.GetRecent(2))); Assert.True(torch.GetPart<ThermalPart>().IsAflame); Assert.True(torch.GetPart<LightSourcePart>().Enabled);
            Assert.AreEqual(19, Strikes(s), "exactly one strike for one lighting");
        }

        [Test] public void ARealFireIsUsedBeforeTheStrikerAndCostsNoStrike_Counter()
        {
            var torch = DarkTorch(); var s = Striker(); Place("SpreadCookingCoals", 9, 10);
            Assert.True(Act(torch, "LightTorch")); Assert.True(torch.GetPart<ThermalPart>().IsAflame); Assert.AreEqual(20, Strikes(s), "a burning fire in reach is free; the flint is kept");
        }

        [Test] public void WithoutAStrikerOrAFireTheTorchStillRefuses_Counter()
        {
            var torch = DarkTorch(); Assert.False(Act(torch, "LightTorch")); Assert.False(torch.GetPart<ThermalPart>().IsAflame);
            StringAssert.Contains("burning fire", string.Join("|", MessageLog.GetRecent(3)));
        }

        [Test] public void ASpentStrikerLightsNothingAndKeepsItsRefusal()
        {
            var torch = DarkTorch(); var s = Striker(0); Assert.False(Act(torch, "LightTorch")); Assert.False(torch.GetPart<ThermalPart>().IsAflame); Assert.AreEqual(0, Strikes(s));
        }

        [Test] public void AWetStrikerWillNotSpark_ButADryOneInTheSamePackWill()
        {
            var torch = DarkTorch(); var wet = Striker(); Assert.True(wet.ApplyEffect(new WetEffect(1f))); Assert.False(Act(torch, "LightTorch")); Assert.AreEqual(20, Strikes(wet), "a refusal costs nothing");
            var dry = Striker(5); Assert.True(Act(torch, "LightTorch")); Assert.AreEqual(4, Strikes(dry), "the usable striker is the one that is spent"); Assert.AreEqual(20, Strikes(wet));
        }

        [Test] public void AFrozenStrikerWillNotSpark()
        {
            var torch = DarkTorch(); var s = Striker(); s.AddPart(new MaterialPart { MaterialTagsRaw = "Metal" }); Assert.True(s.ApplyEffect(new FrozenEffect(1))); Assert.False(Act(torch, "LightTorch")); Assert.AreEqual(20, Strikes(s));
        }

        [Test] public void AStrikerOnTheGroundOrAnotherCarriersPackIsNotAFlameInReach_Counter()
        {
            var torch = DarkTorch(); var s = Factory.CreateEntity("FlintStriker"); Assert.True(Zone.AddEntity(s, 12, 10)); Assert.False(Act(torch, "LightTorch"), "it must be carried by the one who lights");
            var other = Factory.CreateEntity("Player"); Assert.True(Zone.AddEntity(other, 5, 5)); var held = Factory.CreateEntity("FlintStriker"); Assert.True(other.GetPart<InventoryPart>().AddObject(held)); Assert.False(Act(torch, "LightTorch"));
            Assert.AreEqual(20, held.GetPart<FlintStrikerPart>().Strikes);
        }

        [Test] public void ColdCoalsRelightFromTheStrikerAndNoRowAppearsWithoutIt()
        {
            var coals = ColdCoals(); Assert.False(Actions(coals).Any(a => a.Command == "RelightCookingFire"), "no flame in reach, no relight row");
            var s = Striker(); Assert.True(Actions(coals).Any(a => a.Command == "RelightCookingFire")); Assert.True(Act(coals, "RelightCookingFire"), string.Join("|", MessageLog.GetRecent(2)));
            Assert.AreEqual(450, coals.GetPart<ThermalPart>().Temperature); Assert.AreEqual(10, coals.GetPart<FuelPart>().FuelMass, "relighting spends a strike, not the fuel"); Assert.AreEqual(19, Strikes(s));
        }

        [Test] public void AStrikeSpentInALightingThatLaterFailsIsGivenBack()
        {
            var torch = DarkTorch(); var s = Striker(); FailAfter(); Assert.False(Act(torch, "LightTorch")); Assert.AreEqual(20, Strikes(s), "an outer failure rolls the strike back"); Assert.False(torch.GetPart<ThermalPart>().IsAflame);
        }

        // ── a spark into the world: only where it would do something, through the shared reactions ──

        [Test] public void SparkRowsAppearOnlyForAdjacentCellsWhereHeatWouldIgnite()
        {
            var s = Striker(); Assert.False(Actions(s).Any(a => a.Name == "StrikeSpark"), "nothing here would catch, so no row invites a wasted strike");
            Zone.TileState.WriteCoating(11, 10, Oil, 5); Zone.TileState.WriteCoating(14, 10, Oil, 5); // one adjacent, one out of reach
            var rows = Actions(s).Where(a => a.Name == "StrikeSpark").ToArray(); Assert.AreEqual(1, rows.Length); Assert.AreEqual("strike a spark east", rows[0].Display);
            Assert.True(TileReactionSystem.HeatWouldIgnite(Zone, 11, 10)); Assert.False(TileReactionSystem.HeatWouldIgnite(Zone, 12, 10));
            Assert.AreEqual(ActionTime.OneAction, rows[0].Time);
        }

        [Test] public void ARowWarnsWhenYouAreStandingInTheSlickYouAreAboutToLight()
        {
            var s = Striker(); Zone.TileState.WriteCoating(11, 10, Oil, 5); Assert.AreEqual("strike a spark east", Actions(s).Single(a => a.Name == "StrikeSpark").Display);
            Zone.TileState.WriteCoating(10, 10, Oil, 5); Assert.AreEqual("strike a spark east (you are standing in it)", Actions(s).Single(a => a.Name == "StrikeSpark" && a.Display.Contains("east")).Display);
        }

        [Test] public void ASparkLightsAnOilSlickThroughTheSharedReactionAndSpendsOneStrike()
        {
            var s = Striker(); Zone.TileState.WriteCoating(11, 10, Oil, 5); Assert.False(Zone.TileState.HasResidue(11, 10, "embers"));
            Assert.True(Act(s, SparkRow(s, "east")), string.Join("|", MessageLog.GetRecent(2)));
            Assert.False(Zone.TileState.HasCoating(11, 10, Oil), "the slick burned"); Assert.True(Zone.TileState.HasResidue(11, 10, "embers")); Assert.AreEqual(19, Strikes(s));
            Assert.AreEqual(1, Records("StrikerSpark"));
        }

        [Test] public void ASparkIntoACellWithNothingToCatchIsRefusedAndKeepsTheStrike_Counter()
        {
            var s = Striker(); Zone.TileState.WriteCoating(11, 10, Oil, 5); string row = SparkRow(s, "east");
            Zone.TileState.RemoveCoating(11, 10, Oil); Assert.False(Act(s, row), "the slick is gone by the time of the strike"); Assert.AreEqual(20, Strikes(s)); Assert.AreEqual(1, Records("StrikerRejected"));
        }

        [Test] public void AStaleSparkSelectionAfterMovingIsRefused()
        {
            var s = Striker(); Zone.TileState.WriteCoating(11, 10, Oil, 5); string row = SparkRow(s, "east"); Zone.RemoveEntity(Actor); Assert.True(Zone.AddEntity(Actor, 15, 15));
            Assert.False(Act(s, row)); Assert.AreEqual(20, Strikes(s)); Assert.True(Zone.TileState.HasCoating(11, 10, Oil));
        }

        [Test] public void AWetStrikerOffersNoSparkRow_Counter()
        {
            var s = Striker(); Zone.TileState.WriteCoating(11, 10, Oil, 5); Assert.True(s.ApplyEffect(new WetEffect(1f))); Assert.False(Actions(s).Any(a => a.Name == "StrikeSpark" && !a.IsDisabled));
        }

        // ── knapping: the way to renew it uses a spent part, not a free refill ──

        [Test] public void KnappingRestoresStrikesAtOneIronSpikeAndOnlyWhenWorn()
        {
            var s = Striker(5); Assert.False(Actions(s).Any(a => a.Name == "KnapStriker" && !a.IsDisabled), "no spike, no row");
            Carry("IronSpikeComponent"); var row = Actions(s).Single(a => a.Name == "KnapStriker"); StringAssert.Contains("iron spike", row.Display);
            Assert.True(Act(s, row.Command)); Assert.AreEqual(15, Strikes(s), "ten strikes for one spike"); Assert.AreEqual(0, Count("IronSpikeComponent"));
            Carry("IronSpikeComponent"); Assert.True(Act(s, Actions(s).Single(a => a.Name == "KnapStriker").Command)); Assert.AreEqual(20, Strikes(s), "never above the maximum");
            Carry("IronSpikeComponent"); Assert.False(Actions(s).Any(a => a.Name == "KnapStriker" && !a.IsDisabled), "a full striker offers no knapping"); Assert.AreEqual(1, Count("IronSpikeComponent"));
        }

        [Test] public void AKnapThatLaterFailsKeepsTheSpikeAndTheStrikes()
        {
            var s = Striker(5); Carry("IronSpikeComponent"); string row = Actions(s).Single(a => a.Name == "KnapStriker").Command; FailAfter();
            Assert.False(Act(s, row)); Assert.AreEqual(5, Strikes(s)); Assert.AreEqual(1, Count("IronSpikeComponent"));
        }

        // ── state, reader, save, stock, diagnostics ──

        [Test] public void TheReaderStatesTheStrikesLeft()
        {
            var s = Striker(7); var lines = new List<string>(); StateReader.Collect(s, false, Zone, Zone.GetEntityCell(Actor), Actor, lines);
            Assert.True(lines.Any(l => l.Contains("7 strikes")), string.Join(" / ", lines)); s.GetPart<FlintStrikerPart>().Strikes = 0; lines.Clear(); StateReader.Collect(s, false, Zone, Zone.GetEntityCell(Actor), Actor, lines);
            Assert.True(lines.Any(l => l.ToLowerInvariant().Contains("worn")), string.Join(" / ", lines));
        }

        [Test] public void StrikesAreSavedPublicFieldsAndRoundTrip()
        {
            var s = Factory.CreateEntity("FlintStriker"); s.GetPart<FlintStrikerPart>().Strikes = 6; var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(s);
            Assert.AreEqual(6, copy.GetPart<FlintStrikerPart>().Strikes); Assert.AreEqual(20, copy.GetPart<FlintStrikerPart>().MaxStrikes);
        }

        [Test] public void TheTinkerStocksItAboutAsOftenAsItsChance()
        {
            LootTableRegistry.Initialize(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/Loot/LootTables.json")));
            int with = 0, runs = 600; for (int seed = 0; seed < runs; seed++) if (LootTableRegistry.Roll("TinkerStock", new System.Random(seed)).Contains("FlintStriker")) with++;
            Assert.That(with, Is.InRange(runs * 30 / 100, runs * 50 / 100));
        }

        [Test] public void EveryStrikerLightingLeavesADiagRecordAndIsNotCountedAsASpark()
        {
            var torch = DarkTorch(); Assert.False(Act(torch, "LightTorch")); var s = Striker(); Assert.True(Act(torch, "LightTorch"));
            Assert.AreEqual(1, Records("StrikerLit")); Assert.AreEqual(0, Records("StrikerSpark"), "lighting a torch is not a spark into a cell");
        }
    }
}
