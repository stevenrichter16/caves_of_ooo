using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// Adversarial sweep for the flint striker: state atomicity, rollback, several instances, corrupt saved values, boundaries, cross-actor reach, and the consequences of a spark that
    /// are not undoable (so they must never run when the action does not commit).
    /// </summary>
    public sealed class FlintStrikerAdversarialTests : FiftyWorldFixture
    {
        const string Oil = "oil";

        [SetUp] public void SetupAdversarial()
        {
            Diag.ResetAll(); MessageLog.Clear(); TileReactionSystem.ResetForTests();
            TileReactionSystem.Initialize(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/TileReactions/Reactions.json")));
        }
        [TearDown] public void TearDownAdversarial() { TileReactionSystem.ResetForTests(); }

        Entity Striker(int strikes = 20) { var s = Carry("FlintStriker"); s.GetPart<FlintStrikerPart>().Strikes = strikes; return s; }
        int Strikes(Entity s) => s.GetPart<FlintStrikerPart>().Strikes;
        Entity DarkTorch(int x = 11, int y = 10) { var t = Place("Torch", x, y); Assert.True(Act(t, "ExtinguishTorch")); return t; }
        string SparkRow(Entity s) => Actions(s).Single(a => a.Name == "StrikeSpark" && a.Display.StartsWith("strike a spark east", StringComparison.Ordinal)).Command;

        // ── atomicity: the world write must never outlive a rolled-back action ──

        [Test] public void ASparkWhoseActionFailsAfterwardWritesNoHeatAndKeepsTheStrikeAndTheSlick()
        {
            var s = Striker(); Zone.TileState.WriteCoating(11, 10, Oil, 5); string row = SparkRow(s); FailAfter();
            Assert.False(Act(s, row)); Assert.AreEqual(20, Strikes(s)); Assert.True(Zone.TileState.HasCoating(11, 10, Oil), "the slick did not burn"); Assert.AreEqual(0, Zone.TileState.Heat(11, 10));
            Assert.False(Zone.TileState.HasResidue(11, 10, "embers"));
        }

        [Test] public void ASparkIsOneStrikeOneAtATimeNotACascadeOfStrikes()
        {
            var s = Striker(); foreach (int x in new[] { 11, 9 }) Zone.TileState.WriteCoating(x, 10, Oil, 5);
            Assert.True(Act(s, SparkRow(s))); Assert.AreEqual(19, Strikes(s), "one action, one strike, even with two slicks beside you"); Assert.True(Zone.TileState.HasCoating(9, 10, Oil), "the far slick is untouched unless the reaction itself spreads");
        }

        // ── several instances, boundaries, corrupt values ──

        [Test] public void TheMostWornUsableStrikerIsSpentFirstAndAFreshOneIsKept()
        {
            var fresh = Striker(20); var worn = Striker(3); var torch = DarkTorch(); Assert.True(Act(torch, "LightTorch")); Assert.AreEqual(2, Strikes(worn)); Assert.AreEqual(20, Strikes(fresh));
        }

        [Test] public void TheLastStrikeLightsOnceAndTheNextTimeRefuses()
        {
            var s = Striker(1); var torch = DarkTorch(); Assert.True(Act(torch, "LightTorch")); Assert.AreEqual(0, Strikes(s));
            Assert.True(Act(torch, "ExtinguishTorch")); Assert.False(Act(torch, "LightTorch")); Assert.AreEqual(0, Strikes(s));
        }

        [Test] public void ACorruptNegativeCountIsWornNotADebt_AndKnappingRestoresFromZero()
        {
            var s = Striker(-5); var torch = DarkTorch(); Assert.False(Act(torch, "LightTorch")); Assert.AreEqual(-5, Strikes(s), "a refusal changes nothing");
            Carry("IronSpikeComponent"); Assert.True(Act(s, Actions(s).Single(a => a.Name == "KnapStriker").Command)); Assert.AreEqual(10, Strikes(s));
        }

        [Test] public void AnOverfullCountOffersNoKnappingAndStillSpendsDownByOne()
        {
            var s = Striker(30); s.GetPart<FlintStrikerPart>().MaxStrikes = 20; Carry("IronSpikeComponent"); Assert.False(Actions(s).Any(a => a.Name == "KnapStriker"));
            var torch = DarkTorch(); Assert.True(Act(torch, "LightTorch")); Assert.AreEqual(29, Strikes(s));
        }

        [Test] public void KnappingFromAStackOfSpikesSpendsExactlyOne()
        {
            var s = Striker(2); var spikes = Carry("IronSpikeComponent"); var more = Carry("IronSpikeComponent");
            int before = Count("IronSpikeComponent"); Assert.True(Act(s, Actions(s).Single(a => a.Name == "KnapStriker").Command)); Assert.AreEqual(before - 1, Count("IronSpikeComponent")); Assert.AreEqual(12, Strikes(s));
            Assert.NotNull(spikes); Assert.NotNull(more);
        }

        [Test] public void TheWetnessBoundaryIsExactlyTheTorchsOwn()
        {
            var s = Striker(); var torch = DarkTorch(); Assert.True(s.ApplyEffect(new WetEffect(0.35f))); Assert.True(Act(torch, "LightTorch"), "at the limit it still strikes"); Assert.AreEqual(19, Strikes(s));
            Assert.True(Act(torch, "ExtinguishTorch")); s.GetEffect<WetEffect>().Moisture = 0.36f; Assert.False(Act(torch, "LightTorch")); Assert.AreEqual(19, Strikes(s));
        }

        // ── reach and custody ──

        [Test] public void AStrikerInsideACarriedBoxIsNotInReach()
        {
            var box = Carry("CampBox"); var s = Factory.CreateEntity("FlintStriker"); Assert.True(box.GetPart<ContainerPart>().AddItem(s)); var torch = DarkTorch();
            Assert.False(Act(torch, "LightTorch")); Assert.AreEqual(20, s.GetPart<FlintStrikerPart>().Strikes);
        }

        [Test] public void ADeadActorStrikesNothing()
        {
            var s = Striker(); Zone.TileState.WriteCoating(11, 10, Oil, 5); string row = SparkRow(s); Actor.GetStat("Hitpoints").BaseValue = 0;
            Assert.False(Act(s, row)); Assert.AreEqual(20, Strikes(s)); Assert.True(Zone.TileState.HasCoating(11, 10, Oil));
        }

        [Test] public void AMalformedOrForgedSparkCommandIsRefusedAndCostsNothing()
        {
            var s = Striker(); Zone.TileState.WriteCoating(11, 10, Oil, 5); string good = SparkRow(s); string[] f = good.Split('|');
            foreach (var bad in new[] { "StrikeSpark|", "StrikeSpark|x|y|z", string.Join("|", f.Take(6)), string.Join("|", f.Take(4).Concat(new[] { "99", "99", f[6] })), string.Join("|", f.Take(6).Concat(new[] { "Entity-forged" })), f[0] + "|" + f[1] + "|a|b|c|d|" + f[6] })
                Assert.False(Act(s, bad), bad);
            Assert.AreEqual(20, Strikes(s)); Assert.True(Zone.TileState.HasCoating(11, 10, Oil));
        }

        // ── consequences are the world's, not the tool's ──

        [Test] public void ACreatureStandingInTheSlickTakesTheReactionsFireDamage()
        {
            var frog = Place("Bandfrog", 11, 10); int hp = frog.GetStatValue("Hitpoints"); var s = Striker(); Zone.TileState.WriteCoating(11, 10, Oil, 5);
            Assert.True(Act(s, SparkRow(s))); Assert.Less(frog.GetStatValue("Hitpoints"), hp, "the shared reaction's occupant damage reaches whoever stands there");
        }

        [Test] public void SparkingDoesNotWriteHeatWhereNothingWouldCatch_Counter()
        {
            var s = Striker(); Zone.TileState.WriteCoating(12, 10, "water", 5); Assert.False(Actions(s).Any(a => a.Name == "StrikeSpark"), "a puddle only steams; no row invites a strike");
            Assert.False(TileReactionSystem.HeatWouldIgnite(Zone, 12, 10)); Assert.False(TileReactionSystem.HeatWouldIgnite(null, 1, 1)); Assert.False(TileReactionSystem.HeatWouldIgnite(Zone, -1, -1));
        }

        [Test] public void TheStrikerIsNeverAFireItself_ItDoesNotWarmWaterOrLightAGroundTorchFromTheGround()
        {
            var s = Factory.CreateEntity("FlintStriker"); Assert.True(Zone.AddEntity(s, 9, 10)); var torch = DarkTorch(); Assert.False(Act(torch, "LightTorch"));
            Assert.False(s.HasPart<ThermalPart>());
        }
    }
}
