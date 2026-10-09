using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class CombatInventoryUtilityAdversarialTests : FiftyWorldFixture
    {
        string Select(Entity item) => Choice(item, item.BlueprintName == "FrogOil" ? "SpreadGrease|" : "ScatterGrit|",
            a => a.Command.Split('|')[4] == "11" && a.Command.Split('|')[5] == "10");
        bool Surface(string blueprint) => blueprint == "FrogOil" ? Zone.TileState.HasCoating(11, 10, "oil") : Zone.TileState.HasResidue(11, 10, "grit");
        static int Count(Entity item) => item.GetPart<StackerPart>()?.StackCount ?? 1;
        static void Stack(Entity item, int count)
        { var stack = item.GetPart<StackerPart>(); if (stack == null) { stack = new StackerPart(); item.AddPart(stack); } stack.StackCount = count; }

        [TestCase("zone")] [TestCase("far")] [TestCase("negative-count")]
        [TestCase("overflow")] [TestCase("wrong-verb")] [TestCase("extra-field")]
        public void ForgedSelectionsDoNotPayOrWrite(string fault)
        {
            var item = Carry("FrogOil"); string command = Select(item); var fields = command.Split('|');
            if (fault == "zone") fields[1] = "different-zone";
            if (fault == "far") fields[4] = "70";
            if (fault == "negative-count") fields[6] = "-1";
            if (fault == "overflow") fields[4] = "2147483648";
            if (fault == "wrong-verb") fields[0] = "ScatterGrit";
            command = string.Join("|", fields) + (fault == "extra-field" ? "|extra" : "");
            Assert.False(Act(item, command)); Assert.Contains(item, Pack.Objects); Assert.False(Surface("FrogOil"));
        }
        [TestCase("duplicate-id")] [TestCase("foreign-owner")] [TestCase("equipped-alias")]
        [TestCase("ground-alias")] [TestCase("empty-stack")] [TestCase("dead")] [TestCase("stunned")]
        public void InconsistentOwnershipOrIncapacitationCannotUseStaleSelection(string fault)
        {
            var item = Carry("SilverSand"); string command = Select(item);
            if (fault == "duplicate-id") { var duplicate = Carry("FireClay"); duplicate.ID = item.ID; }
            if (fault == "foreign-owner") item.GetPart<PhysicsPart>().InInventory = new Entity();
            if (fault == "equipped-alias") Pack.EquippedItems["forged"] = item;
            if (fault == "ground-alias") Zone.AddEntity(item, 10, 10);
            if (fault == "empty-stack") Stack(item, 0);
            if (fault == "dead") Actor.GetStat("Hitpoints").BaseValue = 0;
            if (fault == "stunned") Actor.ApplyEffect(new StunnedEffect(3));
            Assert.False(Act(item, command)); Assert.Contains(item, Pack.Objects); Assert.False(Surface("SilverSand"));
        }
        [TestCase("FrogOil")] [TestCase("SilverSand")]
        public void BeforeActionVetoCannotPublishOrSpend(string blueprint)
        {
            var item = Carry(blueprint); string command = Select(item); Actor.AddPart(new Hook("BeforeInventoryAction", () => false));
            Assert.False(Act(item, command)); Assert.Contains(item, Pack.Objects); Assert.False(Surface(blueprint));
        }
        [TestCase("FrogOil")] [TestCase("SilverSand")]
        public void RollbackPreservesIndependentWaterLayer(string blueprint)
        {
            var item = Carry(blueprint); string command = Select(item);
            Actor.AddPart(new Hook("AfterInventoryAction", () => { Zone.TileState.WriteCoating(11, 10, "water", 17); throw new InvalidOperationException("rollback"); }));
            Assert.False(Act(item, command)); Assert.Contains(item, Pack.Objects); Assert.False(Surface(blueprint));
            Assert.AreEqual(17, Zone.TileState.CoatingTurns(11, 10, "water"));
        }
        [TestCase("FrogOil")] [TestCase("SilverSand")]
        public void FailedRenewalRestoresPriorLeaseWithoutRewindingOtherLayers(string blueprint)
        {
            if (blueprint == "FrogOil") Zone.TileState.WriteCoating(11, 10, "oil", 2); else Zone.TileState.WriteResidue(11, 10, "grit", 2);
            var item = Carry(blueprint); string command = Select(item); FailAfter();
            Assert.False(Act(item, command)); Assert.Contains(item, Pack.Objects);
            Assert.AreEqual(2, blueprint == "FrogOil" ? Zone.TileState.CoatingTurns(11, 10, "oil") : Zone.TileState.Get(11, 10).Residues.Single(r => r.Id == "grit").Turns);
        }
        [TestCase("FrogOil")] [TestCase("SilverSand")]
        public void ThrowingGroundObserverCannotLeaveAFreeSurfaceAfterFailure(string blueprint)
        {
            var item = Carry(blueprint); string command = Select(item);
            Zone.TileState.OnCellChanged = (x, y) => { throw new InvalidOperationException("ground observer"); };
            try { Assert.False(Act(item, command)); Assert.Contains(item, Pack.Objects); Assert.False(Surface(blueprint)); }
            finally { Zone.TileState.OnCellChanged = null; }
        }
        [TestCase("FrogOil")] [TestCase("SilverSand")]
        public void ReentrantSecondUseCannotSpendOrPublishTwice(string blueprint)
        {
            var item = Carry(blueprint); Stack(item, 3); string command = Select(item); bool nested = true;
            Actor.AddPart(new Hook("AfterInventoryAction", () => { nested = Act(item, command); return true; }));
            Assert.True(Act(item, command)); Assert.False(nested); Assert.AreEqual(2, Count(item)); Assert.True(Surface(blueprint));
        }
        [Test] public void PermanentOilPoolCannotBeRenewedIntoAnItemFilm()
        {
            Place("OilSlick", 11, 10); var item = Carry("FrogOil");
            Assert.False(Actions(item).Any(a => a.Command.StartsWith("SpreadGrease|") && a.Command.Split('|')[4] == "11" && a.Command.Split('|')[5] == "10"));
            Assert.AreEqual(ZoneTileState.Permanent, Zone.TileState.CoatingTurns(11, 10, "oil"));
        }
        [Test] public void GritOnOneFootDoesNotSteadyAnUncoveredFarFoot()
        {
            Zone.RemoveEntity(Actor); Actor.AddPart(new SpatialFootprintPart { CellsRaw = "0,0;1,0" }); Assert.True(Zone.AddEntity(Actor, 10, 10));
            Zone.TileState.WriteCoating(11, 10, "oil", 20); Zone.TileState.WriteCoating(12, 10, "oil", 20);
            Zone.TileState.WriteResidue(11, 10, "grit", 12);
            Assert.Null(LiquidSlipSystem.FindSlipperyLiquid(Zone, Zone.GetCell(11, 10)));
            Assert.NotNull(LiquidSlipSystem.FindSlipperyLiquid(Zone, Zone.GetCell(12, 10)));
            Assert.Greater(TerrainNavigationWeight.ForStep(Zone, 11, 10, Actor), 0);
        }
        [Test] public void GritDoesNotEraseOilOrItsHeatReaction()
        {
            TileReactionSystem.ResetForTests(); TileReactionSystem.Initialize(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/TileReactions/Reactions.json")));
            try
            {
                Zone.TileState.WriteCoating(11, 10, "oil", 20); var sand = Carry("SilverSand"); Assert.True(Act(sand, Select(sand)));
                Assert.True(Zone.TileState.HasCoating(11, 10, "oil"));
                Zone.TileState.AddHeat(11, 10, 1); TileReactionSystem.ResolveZone(Zone, Actor);
                Assert.False(Zone.TileState.HasCoating(11, 10, "oil")); Assert.True(Zone.TileState.HasResidue(11, 10, "embers"));
                Assert.True(Zone.TileState.HasResidue(11, 10, "grit"));
            }
            finally { TileReactionSystem.ResetForTests(); }
        }
        [Test] public void GreaseOnHeatOnlyReactsAfterSuccessfulCommit()
        {
            TileReactionSystem.ResetForTests(); TileReactionSystem.Initialize(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Data/TileReactions/Reactions.json")));
            try
            {
                Zone.TileState.AddHeat(11, 10, 1); var item = Carry("FrogOil"); string command = Select(item); FailAfter();
                Assert.False(Act(item, command)); Assert.AreEqual(1, Zone.TileState.Heat(11, 10));
                Assert.False(Zone.TileState.HasResidue(11, 10, "embers")); Assert.Contains(item, Pack.Objects);
            }
            finally { TileReactionSystem.ResetForTests(); }
        }
        [TestCase("FrogOil")] [TestCase("SilverSand")]
        public void RenewalCostsOneAndRefreshesRatherThanAddsDuration(string blueprint)
        {
            var item = Carry(blueprint); Stack(item, 3); Assert.True(Act(item, Select(item))); Zone.TileState.Tick();
            Assert.True(Act(item, Select(item))); Assert.AreEqual(1, Count(item));
            Assert.AreEqual(blueprint == "FrogOil" ? 8 : 12,
                blueprint == "FrogOil" ? Zone.TileState.CoatingTurns(11, 10, "oil") : Zone.TileState.Get(11, 10).Residues.Single(r => r.Id == "grit").Turns);
        }
        sealed class Hook : Part
        {
            readonly string id; readonly Func<bool> action;
            public Hook(string eventId, Func<bool> callback) { id = eventId; action = callback; }
            public override bool HandleEvent(GameEvent e) => e.ID != id || action();
        }
    }
}
