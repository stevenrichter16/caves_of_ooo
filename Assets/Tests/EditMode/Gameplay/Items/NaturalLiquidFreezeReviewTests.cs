using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public abstract class NaturalLiquidFreezeReviewFixture : FiftyWorldFixture
    {
        List<TileReactionSystem.TileReaction> previous;
        bool wasInitialized;
        static readonly FieldInfo Reactions = typeof(TileReactionSystem).GetField("_reactions", BindingFlags.NonPublic | BindingFlags.Static);
        static readonly FieldInfo Initialized = typeof(TileReactionSystem).GetField("<IsInitialized>k__BackingField", BindingFlags.NonPublic | BindingFlags.Static);
        [SetUp] public void SetupFreeze()
        {
            previous = new List<TileReactionSystem.TileReaction>((List<TileReactionSystem.TileReaction>)Reactions.GetValue(null));
            wasInitialized = TileReactionSystem.IsInitialized;
            TileReactionSystem.Initialize(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/TileReactions/Reactions.json")));
            foreach (var cell in Zone.Cells) cell.IsVisible = cell.Explored = true;
        }
        [TearDown] public void RestoreFreeze()
        {
            TileReactionSystem.ResetForTests();
            ((List<TileReactionSystem.TileReaction>)Reactions.GetValue(null)).AddRange(previous);
            Initialized.SetValue(null, wasInitialized);
        }
        protected Entity AuthoredPool(string blueprint)
        {
            var pool = Place(blueprint); pool.GetPart<TileStateSourcePart>().Seed(Zone, 11, 10); return pool;
        }
        protected string Freeze(Entity source) => Choice(source, "MaterialField|Freeze|", a => a.Command.Split('|')[5] == "11" && a.Command.Split('|')[6] == "10");
        protected bool Offered(Entity source) => Actions(source).Any(a => a.Command.StartsWith("MaterialField|Freeze|", StringComparison.Ordinal));
    }
    public sealed class NaturalLiquidFreezeReviewTests : NaturalLiquidFreezeReviewFixture
    {
        [TestCase("BrinePool")] [TestCase("MirePool")]
        public void ExistingFiniteFrostLichenFreezesAuthoredNaturalSolution(string blueprint)
        {
            var pool = AuthoredPool(blueprint); var liquid = pool.GetPart<LiquidPoolPart>(); int before = liquid.Volume;
            var item = Carry("FrostLichen");
            Assert.False(Zone.TileState.HasCoating(11, 10, "water"));
            Assert.True(Act(item, Freeze(item)));
            Assert.True(Zone.TileState.HasCoating(11, 10, "ice"));
            Assert.False(Zone.TileState.HasCoating(11, 10, liquid.LiquidId));
            Assert.False(Pack.Objects.Contains(item)); Assert.AreEqual(before, liquid.Volume);
            var skin = Skin(); Assert.False(Act(skin, "FillWaterskin")); Assert.AreEqual(0, Units(skin));
        }
        [Test] public void PureWaterStillFreezesWithOneFiniteItem()
        {
            Zone.TileState.WriteCoating(11, 10, "water", 5); var item = Carry("FrostLichen");
            Assert.True(Act(item, Freeze(item))); Assert.True(Zone.TileState.HasCoating(11, 10, "ice")); Assert.False(Pack.Objects.Contains(item));
        }
        [TestCase("")] [TestCase("oil")] [TestCase("acid")] [TestCase("gel")]
        public void DryOrUnrelatedLiquidDoesNotOfferOrSpendFreeze(string coating)
        {
            if (coating != "") Zone.TileState.WriteCoating(11, 10, coating, 5);
            var item = Carry("FrostLichen"); Assert.False(Offered(item)); Assert.Contains(item, Pack.Objects);
            Assert.False(Zone.TileState.HasCoating(11, 10, "ice"));
        }
    }

    public sealed class NaturalLiquidFreezeReviewAdversarialTests : NaturalLiquidFreezeReviewFixture
    {
        [TestCase("BrinePool", "remove")] [TestCase("MirePool", "remove")]
        [TestCase("BrinePool", "replace")] [TestCase("MirePool", "replace")]
        [TestCase("BrinePool", "other-family")] [TestCase("MirePool", "other-family")]
        [TestCase("BrinePool", "water-priority")] [TestCase("MirePool", "water-priority")]
        [TestCase("BrinePool", "hidden")] [TestCase("MirePool", "hidden")]
        [TestCase("BrinePool", "blocked")] [TestCase("MirePool", "blocked")]
        public void LayerOrEligibilityChangedDuringActionRefundsSupplyAndDoesNotFreeze(string blueprint, string change)
        {
            var pool = AuthoredPool(blueprint); string id = pool.GetPart<LiquidPoolPart>().LiquidId;
            var item = Carry("FrostLichen"); string command = Freeze(item);
            Actor.AddPart(new AfterAction(() =>
            {
                if (change == "remove" || change == "replace" || change == "other-family") Zone.TileState.RemoveCoating(11, 10, id);
                if (change == "replace") Zone.TileState.WriteCoating(11, 10, id, 9);
                if (change == "other-family") Zone.TileState.WriteCoating(11, 10, id == "brine" ? "bog-mire" : "brine", 9);
                if (change == "water-priority") Zone.TileState.WriteCoating(11, 10, "water", 9);
                if (change == "hidden") Zone.GetCell(11, 10).IsVisible = false;
                if (change == "blocked") Place("StoneWall");
            }));
            Assert.False(Act(item, command)); Assert.Contains(item, Pack.Objects);
            Assert.AreSame(Actor, item.GetPart<PhysicsPart>().InInventory);
            Assert.False(Zone.TileState.HasCoating(11, 10, "ice")); Assert.Zero(Zone.TileState.Cold(11, 10));
            if (change == "water-priority") Assert.AreEqual(9, Zone.TileState.CoatingTurns(11, 10, "water"));
        }
        [TestCase("BrinePool")] [TestCase("MirePool")]
        public void LaterInventoryFailureDoesNotRunIrreversibleFreeze(string blueprint)
        {
            var pool = AuthoredPool(blueprint); string id = pool.GetPart<LiquidPoolPart>().LiquidId;
            var item = Carry("FrostLichen"); string command = Freeze(item); FailAfter();
            Assert.False(Act(item, command)); Assert.Contains(item, Pack.Objects);
            Assert.True(Zone.TileState.HasCoating(11, 10, id)); Assert.False(Zone.TileState.HasCoating(11, 10, "ice"));
        }
        [TestCase("brine")] [TestCase("bog-mire")]
        public void PureWaterTakesReactionPriorityWithoutTurningOtherCoatsIntoWater(string other)
        {
            Zone.TileState.WriteCoating(11, 10, other, 9); Zone.TileState.WriteCoating(11, 10, "water", 5);
            var item = Carry("FrostLichen"); Assert.True(Act(item, Freeze(item)));
            Assert.False(Zone.TileState.HasCoating(11, 10, "water")); Assert.True(Zone.TileState.HasCoating(11, 10, other));
            Assert.True(Zone.TileState.HasCoating(11, 10, "ice")); Assert.False(Pack.Objects.Contains(item));
        }
        sealed class AfterAction : Part
        {
            readonly Action callback;
            public AfterAction(Action callback) { this.callback = callback; }
            public override bool HandleEvent(GameEvent e) { if (e.ID == "AfterInventoryAction") callback(); return true; }
        }
    }
}
