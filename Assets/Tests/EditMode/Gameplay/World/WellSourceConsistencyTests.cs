using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class WellSourceConsistencyTests : FiftyWorldFixture
    {
        Entity Well(bool pool = false, int stock = 3)
        {
            var well = Place(pool ? "GroveSeep" : "Well");
            well.GetPart<WellPart>().Stock = stock;
            if (pool) well.GetPart<LiquidPoolPart>().Volume = 2;
            return well;
        }
        void Thirst(int stacks = 3) => Assert.True(Actor.ApplyEffect(new ParchedEffect { Stacks = stacks }));
        void Debt(int stacks)
        {
            Assert.AreEqual(stacks, Actor.GetEffect<ParchedEffect>()?.Stacks ?? 0);
            Assert.AreEqual(stacks, Actor.GetStat("Strength").Penalty);
            Assert.AreEqual(stacks, Actor.GetStat("Agility").Penalty);
        }
        [TestCase(false)] [TestCase(true)]
        public void WorkingSourcesKeepOneUnitFullCureAndPoolPrecedence(bool pool)
        {
            var well = Well(pool); Thirst();
            Assert.True(Act(well, "DrawWaterAtWell")); Debt(0);
            Assert.AreEqual(pool ? 3 : 2, well.GetPart<WellPart>().Stock);
            if (pool) Assert.AreEqual(1, well.GetPart<LiquidPoolPart>().Volume);
        }
        [Test] public void EndlessWellRecoveryRemainsEndlessAndFreeInItsRow()
        {
            var well = Well(stock: -1); Thirst(); Assert.True(Act(well, "DrawWaterAtWell"));
            Debt(0); Assert.AreEqual(-1, well.GetPart<WellPart>().Stock);
            Assert.AreEqual(ActionTime.Free, Actions(well).Single(a => a.Command == "DrawWaterAtWell").Time);
        }
        [TestCase(false, "ice")] [TestCase(false, "cold")]
        [TestCase(true, "ice")] [TestCase(true, "cold")] [TestCase(true, "mixed")]
        [TestCase(true, "broken")]
        public void DrawAndFillRefuseUnavailableWell(bool pool, string state)
        {
            var well = Well(pool); var skin = Skin(0); Thirst();
            if (state == "ice") Zone.TileState.WriteCoating(11, 10, "ice", 5);
            if (state == "cold") well.AddPart(new ThermalPart { Temperature = -10, FreezeTemperature = 0 });
            if (state == "mixed") Zone.TileState.WriteCoating(11, 10, "oil", 5);
            if (state == "broken") well.AddPart(new RepairablePart());
            Assert.False(Act(well, "DrawWaterAtWell"), "direct drink");
            Assert.False(Act(skin, WaterVesselService.FillSourcePrefix + Id(well)), "skin fill");
            Debt(3); Assert.AreEqual(0, Units(skin)); Assert.AreEqual(3, well.GetPart<WellPart>().Stock);
            if (pool) Assert.AreEqual(2, well.GetPart<LiquidPoolPart>().Volume);
            Assert.False(Actions(well).Any(a => a.Command == "DrawWaterAtWell"));
        }
        [Test] public void PoolBackedWellCannotBypassTheExistingPlayerOathRefusal()
        {
            var old = NarrativeStatePart.Current;
            try
            {
                NarrativeStatePart.Current = new NarrativeStatePart();
                NarrativeStatePart.Current.SetFact(TentRightOath.OathbreakerFact, 1);
                Zone.RemoveEntity(Actor); Zone = new Zone("Overworld.8.16.0"); Assert.True(Zone.AddEntity(Actor, 10, 10));
                var well = Well(true); var skin = Skin(0); Thirst();
                Assert.False(Act(well, "DrawWaterAtWell"));
                Assert.False(Act(skin, WaterVesselService.FillSourcePrefix + Id(well)));
                Assert.AreEqual(2, well.GetPart<LiquidPoolPart>().Volume); Assert.AreEqual(0, Units(skin)); Debt(3);
            }
            finally { NarrativeStatePart.Current = old; }
        }
        [TestCase(false)] [TestCase(true)]
        public void LiteralWellWithoutPhysicsStillFillsASkin(bool pool)
        {
            var well = Well(pool); well.RemovePart(well.GetPart<PhysicsPart>()); var skin = Skin(0);
            Assert.True(Act(skin, WaterVesselService.FillSourcePrefix + Id(well)));
            Assert.AreEqual(pool ? 2 : 3, Units(skin));
        }
        [TestCase("well")] [TestCase("pool")]
        public void ForeignPartOwnershipCannotAuthorizeEitherWaterPath(string part)
        {
            var well = Well(true); var skin = Skin(0); var foreign = Factory.CreateEntity("Well"); Thirst();
            if (part == "well") well.GetPart<WellPart>().ParentEntity = foreign;
            else well.GetPart<LiquidPoolPart>().ParentEntity = foreign;
            Assert.False(Act(well, "DrawWaterAtWell"));
            Assert.False(Act(skin, WaterVesselService.FillSourcePrefix + Id(well)));
            Debt(3); Assert.AreEqual(0, Units(skin)); Assert.AreEqual(2, well.GetPart<LiquidPoolPart>().Volume);
        }
        [TestCase(false)] [TestCase(true)]
        public void DirectPoolDrawNotifiesItsExactCellOnlyAfterCommit(bool fail)
        {
            var well = Well(true); well.GetPart<LiquidPoolPart>().Volume = 1; Thirst();
            var old = ZoneRenderHooks.CellDirtyCallback; var dirty = new List<(int x, int y)>();
            ZoneRenderHooks.CellDirtyCallback = (x, y, reason) => { if (reason == "LiquidVessel.SourceChanged") dirty.Add((x, y)); };
            try
            {
                if (fail) FailAfter();
                Assert.AreEqual(!fail, Act(well, "DrawWaterAtWell"));
                Assert.AreEqual(fail ? 1 : 0, well.GetPart<LiquidPoolPart>().Volume);
                Assert.True(Zone.GetReadOnlyEntities().Contains(well), "natural source identity persists when exhausted");
                if (fail) Assert.IsEmpty(dirty);
                else CollectionAssert.AreEqual(new[] { (11, 10) }, dirty);
            }
            finally { ZoneRenderHooks.CellDirtyCallback = old; }
        }
        [Test] public void DrinkRollbackKeepsAnIndependentUnrelatedStatPenalty()
        {
            var well = Well(); Thirst(2);
            Actor.AddPart(new FiftyWorldEventProbe { Callback = e =>
            {
                if (e.ID != "AfterInventoryAction") return;
                Actor.GetStat("Strength").Penalty += 4;
                throw new InvalidOperationException("independent strength impairment");
            } });
            Assert.False(Act(well, "DrawWaterAtWell")); Assert.AreEqual(2, Actor.GetEffect<ParchedEffect>().Stacks);
            Assert.AreEqual(6, Actor.GetStat("Strength").Penalty); Assert.AreEqual(2, Actor.GetStat("Agility").Penalty);
        }
        [Test] public void PoolRollbackDoesNotRewindUnspentWellStock()
        {
            var well = Well(true); Thirst();
            Actor.AddPart(new FiftyWorldEventProbe { Callback = e =>
            {
                if (e.ID != "AfterInventoryAction") return;
                well.GetPart<WellPart>().Stock = 7;
                throw new InvalidOperationException("independent unspent well stock");
            } });
            Assert.False(Act(well, "DrawWaterAtWell"));
            Assert.AreEqual(2, well.GetPart<LiquidPoolPart>().Volume);
            Assert.AreEqual(7, well.GetPart<WellPart>().Stock); Debt(3);
        }
        [TestCase(1)] [TestCase(3)]
        public void RollbackPreservesReplacementExposureWithoutStackOrPenaltyOverflow(int added)
        {
            var well = Well(); Thirst(2);
            Actor.AddPart(new FiftyWorldEventProbe { Callback = e =>
            {
                if (e.ID != "AfterInventoryAction") return;
                Actor.ApplyEffect(new ParchedEffect { Stacks = added });
                throw new InvalidOperationException("independent new exposure");
            } });
            Assert.False(Act(well, "DrawWaterAtWell")); Debt(3); Assert.AreEqual(3, well.GetPart<WellPart>().Stock);
        }
        [TestCase("valid", false)] [TestCase("stale", false)] [TestCase("after", false)]
        [TestCase("valid", true)] [TestCase("stale", true)] [TestCase("after", true)]
        public void RealCMenuKeepsFreeTimingAndOnlyCommitsItsWholeDrink(string condition, bool pool)
        {
            using (var ui = new HotbarSaveFixture(true, false))
            {
                var well = Well(pool); Thirst(); var thirst = Actor.GetEffect<ParchedEffect>();
                var manager = OverworldZoneManager.CreateDetached(Factory, 64, true); manager.SetActiveZone(Zone);
                var turns = new TurnManager();
                turns.RestoreSavedState(17, true, Actor, new List<TurnManager.SavedTurnEntry> { new TurnManager.SavedTurnEntry { Entity = Actor, Energy = TurnManager.ActionThreshold } });
                ui.BindOld(GameSessionState.Capture("well-input", "normal-world-menu", manager, turns, Actor));
                var menu = ui.Root.AddComponent<WorldActionMenuUI>(); ui.Input.WorldActionMenuUI = menu;
                const BindingFlags hidden = BindingFlags.Instance | BindingFlags.NonPublic;
                var state = typeof(InputHandler).GetField("_worldActionMenuReturnState", hidden);
                state.SetValue(ui.Input, Enum.Parse(state.FieldType, "Normal"));
                typeof(InputHandler).GetMethod("OpenWorldActionMenuFor", hidden).Invoke(ui.Input, new object[] { well, Zone.GetEntityCell(well), false });
                var row = ((List<InventoryAction>)typeof(WorldActionMenuUI).GetField("_actions", hidden).GetValue(menu)).Single(a => a.Command == "DrawWaterAtWell");
                var probe = new WellActionProbe { FailAfter = condition == "after" }; Actor.AddPart(probe);
                if (condition == "stale") Zone.TileState.WriteCoating(11, 10, "ice", 5);
                typeof(InputHandler).GetMethod("ExecuteWorldActionSelection", hidden).Invoke(ui.Input, new object[] { row, well, Zone.GetEntityCell(well), false });
                Assert.AreEqual(17, turns.TickCount); Assert.AreEqual(1, probe.Before, "real menu uses common transaction");
                Debt(condition == "valid" ? 0 : 3);
                Assert.AreEqual(pool ? 3 : condition == "valid" ? 2 : 3, well.GetPart<WellPart>().Stock);
                if (pool) Assert.AreEqual(condition == "valid" ? 1 : 2, well.GetPart<LiquidPoolPart>().Volume);
                if (condition == "after") Assert.AreSame(thirst, Actor.GetEffect<ParchedEffect>());
                Assert.AreEqual(condition == "stale" ? 0 : 1, probe.After);
            }
        }
        [TestCase(false)] [TestCase(true)]
        public void SameNamedLegacyCommandWithoutAnAttachedWellKeepsItsStatSnapshot(bool detached)
        {
            var target = Place("PhysicalObject");
            if (detached)
            {
                var part = new WellPart(); target.AddPart(part);
                part.ParentEntity = Factory.CreateEntity("Well");
            }
            target.AddPart(new LegacyWellCommandProbe()); FailAfter();
            Assert.False(Act(target, "DrawWaterAtWell")); Assert.AreEqual(0, Actor.GetStat("Strength").Penalty);
        }
        public sealed class LegacyWellCommandProbe : Part
        {
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID != "InventoryAction" || e.GetStringParameter("Command") != "DrawWaterAtWell") return true;
                e.GetParameter<Entity>("Actor").GetStat("Strength").Penalty += 4;
                e.Handled = true; return false;
            }
        }
        public sealed class WellActionProbe : Part
        {
            public int Before, After; public bool FailAfter;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "BeforeInventoryAction") Before++;
                if (e.ID == "AfterInventoryAction") { After++; if (FailAfter) throw new InvalidOperationException("well outer veto"); }
                return true;
            }
        }
    }
}
