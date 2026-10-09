using System;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Player-flow regression cases for ordinary combat supplies. These
    /// start at the real inventory menu/command and keep equal-setup controls.</summary>
    public sealed class CombatInventoryUtilityTests : FiftyWorldFixture
    {
        System.Random previousRng;
        [SetUp] public void SetupUtility()
        {
            previousRng = LiquidSlipSystem.TestRng;
            LiquidSlipSystem.TestRng = new System.Random(1);
            MessageLog.Clear(); Diag.ResetAll();
        }
        [TearDown] public void CleanupUtility() => LiquidSlipSystem.TestRng = previousRng;

        string Selection(Entity item, int x = 11, int y = 10)
        {
            string prefix = item.BlueprintName == "FrogOil" ? "SpreadGrease|" : "ScatterGrit|";
            return Choice(item, prefix, a => a.Command.Split('|')[4] == x.ToString()
                && a.Command.Split('|')[5] == y.ToString());
        }
        static void SetAmount(Entity item, int count)
        { var stack = item.GetPart<StackerPart>(); if (stack == null) { stack = new StackerPart(); item.AddPart(stack); } stack.StackCount = count; }
        static int Amount(Entity item) => item.GetPart<StackerPart>()?.StackCount ?? 1;
        int Remaining(Entity item) => Pack.Objects.Contains(item) ? Amount(item) : 0;
        static string Film(string item) => item == "FrogOil" ? "oil" : "grit";
        bool Has(int x, int y, string film) => film == "oil" ? Zone.TileState.HasCoating(x, y, film) : Zone.TileState.HasResidue(x, y, film);
        static void CertainSlipping()
        {
            LiquidRegistry.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(Application.dataPath,
                "Resources/Content/Data/LiquidDefinitions"), "*.json").Select(File.ReadAllText)
                .Concat(new[] { "{\"Liquids\":[{\"Id\":\"oil\",\"Slippery\":true,\"SlipChance\":100},{\"Id\":\"ice\",\"Slippery\":true,\"SlipChance\":100}]}" }));
        }

        [TestCase("FrogOil", "oil", 8)] [TestCase("SilverSand", "grit", 12)]
        public void OrdinarySupplyMenuCreatesOnlyTheChosenFiniteSurfaceAndSpendsOneUnit(string blueprint, string film, int duration)
        {
            var item = Carry(blueprint); SetAmount(item, 3);
            string command = Selection(item);
            Assert.True(Act(item, command)); Assert.AreEqual(2, Remaining(item));
            Assert.True(Has(11, 10, film)); Assert.False(Has(10, 10, film)); Assert.False(Has(12, 10, film));
            int turns = film == "oil" ? Zone.TileState.CoatingTurns(11, 10, film)
                : Zone.TileState.Get(11, 10).Residues.Single(r => r.Id == film).Turns;
            Assert.AreEqual(duration, turns);
            Assert.False(Zone.GetReadOnlyEntities().Any(e => e.HasPart<LiquidPoolPart>()), "a finite film must not mint recoverable liquid");
        }

        [TestCase("FrogOil")] [TestCase("SilverSand")]
        public void AlreadySavedSupplyWithoutANewPartStillOffersAndPerformsTheAction(string blueprint)
        {
            var item = Carry(blueprint);
            var copy = PartRoundTripHelper.RoundTripEntityViaTokenGraph(Actor);
            Zone.RemoveEntity(Actor); Actor = copy; Assert.True(Zone.AddEntity(Actor, 10, 10));
            item = Pack.Objects.Single(e => e.BlueprintName == blueprint);
            Assert.True(Act(item, Selection(item))); Assert.AreEqual(0, Remaining(item));
            Assert.True(Has(11, 10, Film(blueprint)));
        }

        [TestCase("FrogOil")] [TestCase("SilverSand")]
        public void DistantBlockedAndUnrelatedItemsOfferNoInvalidTargets(string blueprint)
        {
            var item = Carry(blueprint); Place("StoneWall", 11, 10);
            var actions = Actions(item).Where(a => a.Command.StartsWith(blueprint == "FrogOil" ? "SpreadGrease|" : "ScatterGrit|")).ToArray();
            Assert.Greater(actions.Length, 0, "the valid nearby choices must exist");
            Assert.False(actions.Any(a => a.Command.Split('|')[4] == "11" && a.Command.Split('|')[5] == "10"));
            Assert.False(actions.Any(a => Math.Abs(int.Parse(a.Command.Split('|')[4]) - 10) > 1));
            var unrelated = Carry("FireClay");
            Assert.False(Actions(unrelated).Any(a => a.Command.StartsWith("SpreadGrease|") || a.Command.StartsWith("ScatterGrit|")));
        }

        [TestCase("FrogOil", "moved")] [TestCase("SilverSand", "moved")]
        [TestCase("FrogOil", "dropped")] [TestCase("SilverSand", "dropped")]
        [TestCase("FrogOil", "count")] [TestCase("SilverSand", "count")]
        public void StaleSelectionsRefuseWithoutConsumingOrWriting(string blueprint, string change)
        {
            var item = Carry(blueprint); SetAmount(item, 3); string command = Selection(item);
            if (change == "moved") Assert.True(Zone.MoveEntity(Actor, 9, 10));
            else if (change == "dropped") { Assert.True(Pack.RemoveObject(item)); Assert.True(Zone.AddEntity(item, 10, 10)); }
            else item.GetPart<StackerPart>().StackCount = 2;
            Assert.False(Act(item, command)); Assert.AreEqual(change == "count" ? 2 : 3, Amount(item));
            Assert.False(Has(11, 10, Film(blueprint)));
        }

        [TestCase("FrogOil")] [TestCase("SilverSand")]
        public void OuterFailureRestoresExactSupplyAndLeavesNoSurfaceOrSuccessReceipt(string blueprint)
        {
            var item = Carry(blueprint); string command = Selection(item); FailAfter();
            Assert.False(Act(item, command)); Assert.Contains(item, Pack.Objects); Assert.AreSame(Actor, item.GetPart<PhysicsPart>().InInventory);
            Assert.False(Has(11, 10, Film(blueprint)));
            Assert.False(DiagQuery.Apply(new DiagQuery.Filter { Category = "event", Kind = "CombatUtilityUsed" }).Records.Any());
        }

        [TestCase("FrogOil")] [TestCase("SilverSand")]
        public void DuplicateCommandCannotSpendAgainOrStackLifetime(string blueprint)
        {
            var item = Carry(blueprint); SetAmount(item, 3); string command = Selection(item);
            Assert.True(Act(item, command)); Assert.False(Act(item, command)); Assert.AreEqual(2, Remaining(item));
            Assert.False(Actions(item).Any(a => a.Command.StartsWith(blueprint == "FrogOil" ? "SpreadGrease|" : "ScatterGrit|")
                && a.Command.Split('|')[4] == "11" && a.Command.Split('|')[5] == "10"));
        }

        [TestCase("FrogOil")] [TestCase("SilverSand")]
        public void SurfaceStateRoundTripsAndExpiresWithoutRecoverableItems(string blueprint)
        {
            var item = Carry(blueprint); Assert.True(Act(item, Selection(item)));
            string saved = Zone.TileState.ToSaveString(); Zone.TileState.Clear(11, 10); Zone.TileState.LoadFromString(saved);
            Assert.True(Has(11, 10, Film(blueprint)));
            int duration = blueprint == "FrogOil" ? 8 : 12;
            for (int i = 0; i < duration - 1; i++) Zone.TileState.Tick();
            Assert.True(Has(11, 10, Film(blueprint))); Zone.TileState.Tick(); Assert.False(Has(11, 10, Film(blueprint)));
            Assert.AreEqual(0, Remaining(item));
        }

        [TestCase("oil", false)] [TestCase("oil", true)] [TestCase("ice", false)] [TestCase("ice", true)]
        public void GritPreventsActualMovementSlipForPlayersAndOtherCreatures(string coating, bool npc)
        {
            CertainSlipping(); Zone.TileState.WriteCoating(11, 10, coating, 50); Zone.TileState.WriteResidue(11, 10, "grit", 12);
            if (npc) { Zone.RemoveEntity(Actor); Actor = Place("MarlbackScrabbler", 10, 10); }
            Assert.True(MovementSystem.TryMove(Actor, Zone, 1, 0)); Assert.AreEqual((11, 10), Zone.GetEntityPosition(Actor));
            Assert.True(Zone.TileState.HasCoating(11, 10, coating));
            Assert.False(DiagQuery.Apply(new DiagQuery.Filter { Category = "liquid", Kind = "SlipRolled" }).Records.Any());
        }

        [TestCase("oil")] [TestCase("ice")]
        public void WithoutGritTheSameSurfaceStillSlipsAndExpiryRestoresThatRisk(string coating)
        {
            CertainSlipping(); Zone.TileState.WriteCoating(11, 10, coating, 50); Zone.TileState.WriteResidue(11, 10, "grit", 1); Zone.TileState.Tick();
            Assert.True(MovementSystem.TryMove(Actor, Zone, 1, 0)); Assert.AreNotEqual((11, 10), Zone.GetEntityPosition(Actor));
            Assert.True(DiagQuery.Apply(new DiagQuery.Filter { Category = "liquid", Kind = "Slipped" }).Records.Any());
        }

        [Test] public void NavigationRecognizesTractionButRetainsAcidAndPoolDanger()
        {
            Zone.TileState.WriteCoating(11, 10, "oil", 50);
            int slickCost = TerrainNavigationWeight.ForCell(Zone.GetCell(11, 10), Actor); Assert.Greater(slickCost, 0);
            Zone.TileState.WriteResidue(11, 10, "grit", 12);
            Assert.AreEqual(0, TerrainNavigationWeight.ForCell(Zone.GetCell(11, 10), Actor));
            var acid = Place("AcidPool", 11, 10);
            Assert.Greater(TerrainNavigationWeight.ForCell(Zone.GetCell(11, 10), Actor), 0);
            Assert.Greater(acid.GetPart<LiquidPoolPart>().Volume, 0);
            Assert.False(LiquidSourceSafety.IsUnmixedPool(Zone, acid), "traction must not purify an oil/acid mixture");
        }
    }
}
