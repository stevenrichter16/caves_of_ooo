using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using NUnit.Framework;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// A real packaged journey found that C, period counted the acting player
    /// as another pile owner and hid a lone StoneburrPatch's Harvest action.
    /// Exercise the existing input/menu seam, including its normal transaction
    /// dispatch; no new targeting API is assumed by these regression tests.
    /// </summary>
    public sealed class UnderfootWorldActionTests : ShortcutFixture
    {
        private EntityFactory _oldHarvestFactory;

        [SetUp]
        public void SetUpUnderfoot()
        {
            _oldHarvestFactory = HarvestablePart.Factory;
            HarvestablePart.Factory = Factory;
        }

        [TearDown]
        public void RestoreUnderfoot()
        {
            HarvestablePart.Factory = _oldHarvestFactory;
        }

        private InputHandler Input()
        {
            var input = Component<InputHandler>();
            input.PlayerEntity = Player;
            input.CurrentZone = Zone;
            input.WorldActionMenuUI = Component<WorldActionMenuUI>();
            // Keep this fixture's scheduler private, as in the reader tests.
            input.TurnManager = (TurnManager)Activator.CreateInstance(typeof(TurnManager),
                BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { false }, null);
            input.TurnManager.AddEntity(Player);
            input.TurnManager.ProcessUntilPlayerTurn();
            return input;
        }

        private Entity Put(string blueprint, int x = 10, int y = 10)
        {
            var entity = Item(blueprint);
            Assert.IsTrue(Zone.AddEntity(entity, x, y));
            var cell = Zone.GetCell(x, y);
            cell.IsVisible = cell.Explored = true;
            return entity;
        }

        private static List<InventoryAction> Actions(InputHandler input)
            => (List<InventoryAction>)Get(input.WorldActionMenuUI, "_actions");

        private void Open(InputHandler input, int dx = 0)
        {
            State(input, "_inputState", "AwaitingTalkDirection");
            Press(dx == 0 ? Key.Period : Key.RightArrow,
                () => Call(input, "HandleAwaitingTalkDirection"));
        }

        private static void Select(InputHandler input, string command)
        {
            var menu = input.WorldActionMenuUI;
            var action = Actions(input).SingleOrDefault(row => row.Command == command);
            Assert.IsNotNull(action, "The visible menu must offer " + command);
            Call(input, "ExecuteWorldActionSelection", action,
                menu.SelectedTarget, menu.SelectedCell, menu.SelectedCellIsPile);
        }

        private int Seeds() => Inventory.Objects.Where(item => item.BlueprintName == "StoneburrSeed")
            .Sum(item => item.GetPart<StackerPart>()?.StackCount ?? 1);

        [TestCase(0)]
        [TestCase(1)]
        public void LoneHarvestSourceUsesItsOwnMenuUnderfootAndAdjacent(int dx)
        {
            var source = Put("StoneburrPatch", 10 + dx);
            var ground = Put("Grass", 10 + dx);
            var input = Input();
            int tick = input.TurnManager.TickCount;
            int energy = input.TurnManager.GetEnergy(Player);
            int version = Zone.EntityVersion;
            var owners = Zone.GetReadOnlyEntities().ToArray();

            Open(input, dx);

            Assert.IsTrue(input.WorldActionMenuUI.IsOpen);
            Assert.AreSame(source, input.WorldActionMenuUI.SelectedTarget);
            Assert.IsFalse(input.WorldActionMenuUI.SelectedCellIsPile);
            CollectionAssert.Contains(Actions(input).Select(row => row.Command).ToArray(), "Harvest");
            Assert.IsFalse(source.GetPart<HarvestablePart>().Harvested);
            Assert.AreEqual(tick, input.TurnManager.TickCount);
            Assert.AreEqual(energy, input.TurnManager.GetEnergy(Player));
            Assert.AreEqual(version, Zone.EntityVersion);
            CollectionAssert.AreEqual(owners, Zone.GetReadOnlyEntities());
            Assert.AreSame(ground, Zone.GetCell(10 + dx, 10).Occupants.Single(e => e == ground));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ActorIdentityRatherThanPlayerTagDeterminesUnderfootExclusion(bool playerTagged)
        {
            if (!playerTagged) Player.Tags.Remove("Player");
            var source = Put("StoneburrPatch");
            var input = Input();

            Open(input);

            Assert.AreSame(source, input.WorldActionMenuUI.SelectedTarget);
            Assert.IsFalse(input.WorldActionMenuUI.SelectedCellIsPile);
            Assert.IsFalse(Actions(input).Any(row => row.Command == WorldInteractionSystem.PickCellCommand),
                "One selectable object needs no picker just because its acting owner is standing there.");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void TwoActualHarvestOwnersRemainADeliberatePileAndPreserveSelection(int dx)
        {
            var first = Put("StoneburrPatch", 10 + dx);
            var second = Put("StoneburrPatch", 10 + dx);
            var input = Input();

            Open(input, dx);

            Assert.IsTrue(input.WorldActionMenuUI.SelectedCellIsPile);
            Assert.IsFalse(Actions(input).Any(row => row.Command == "Harvest"),
                "A genuine pile must not silently harvest whichever owner renders on top.");
            Select(input, WorldInteractionSystem.PickCellCommand);
            CollectionAssert.AreEquivalent(new[] { first.ID, second.ID },
                Actions(input).Select(row => row.Command.Substring(WorldInteractionSystem.PickTargetCommandPrefix.Length)).ToArray());
            Select(input, WorldInteractionSystem.PickTargetCommandPrefix + first.ID);
            Assert.AreSame(first, input.WorldActionMenuUI.SelectedTarget);
            Assert.IsFalse(input.WorldActionMenuUI.SelectedCellIsPile);
            Select(input, WorldInteractionSystem.PickCellCommand);
            Select(input, WorldInteractionSystem.PickTargetCommandPrefix + second.ID);
            Assert.AreSame(second, input.WorldActionMenuUI.SelectedTarget);
            CollectionAssert.Contains(Actions(input).Select(row => row.Command).ToArray(), "Harvest");
            Assert.IsFalse(first.GetPart<HarvestablePart>().Harvested);
            Assert.IsFalse(second.GetPart<HarvestablePart>().Harvested);
        }

        [TestCase(0)]
        [TestCase(1)]
        public void ASeparatePlayerTaggedOwnerIsNotHiddenFromThePile(int dx)
        {
            var source = Put("StoneburrPatch", 10 + dx);
            var other = Put("Player", 10 + dx);
            var input = Input();

            Open(input, dx);
            Assert.IsTrue(input.WorldActionMenuUI.SelectedCellIsPile);
            Select(input, WorldInteractionSystem.PickCellCommand);

            CollectionAssert.AreEquivalent(new[] {
                WorldInteractionSystem.PickTargetCommandPrefix + source.ID,
                WorldInteractionSystem.PickTargetCommandPrefix + other.ID
            }, Actions(input).Select(row => row.Command).ToArray());
        }

        [Test]
        public void UnderfootTerrainRemainsSelectableWithoutAnActorOnlyPickerRow()
        {
            var ground = Put("Grass");
            var input = Input();

            Open(input);

            Assert.IsTrue(input.WorldActionMenuUI.IsOpen);
            Assert.AreSame(ground, input.WorldActionMenuUI.SelectedTarget);
            Assert.IsFalse(input.WorldActionMenuUI.SelectedCellIsPile);
            Assert.IsFalse(Actions(input).Any(row => row.Command == WorldInteractionSystem.PickCellCommand));
            CollectionAssert.Contains(Actions(input).Select(row => row.Command).ToArray(), "Examine");
        }

        [Test]
        public void TerrainBeneathALoneSourceRemainsInEverythingHere()
        {
            var source = Put("StoneburrPatch");
            var ground = Put("Grass");
            var input = Input();

            Open(input);
            Select(input, WorldInteractionSystem.PickCellCommand);

            CollectionAssert.AreEqual(new[] {
                WorldInteractionSystem.PickTargetCommandPrefix + source.ID,
                WorldInteractionSystem.PickTargetCommandPrefix + ground.ID
            }, Actions(input).Select(row => row.Command).ToArray());
            Select(input, WorldInteractionSystem.PickTargetCommandPrefix + ground.ID);
            Assert.AreSame(ground, input.WorldActionMenuUI.SelectedTarget);
            Assert.IsFalse(Actions(input).Any(row => row.Command == "Harvest"));
        }

        [Test]
        public void ABookkeepingOwnerDoesNotTurnTheLoneSourceIntoAPile()
        {
            var source = Put("StoneburrPatch");
            var metadata = Put("StoneburrPatch");
            metadata.SetTag(WorldInteractionSystem.WorldMetadataTag);
            var input = Input();

            Open(input);

            Assert.AreSame(source, input.WorldActionMenuUI.SelectedTarget);
            Assert.IsFalse(input.WorldActionMenuUI.SelectedCellIsPile);
            Assert.IsFalse(Actions(input).Any(row => row.Command == WorldInteractionSystem.PickCellCommand));
        }

        [Test]
        public void ActorlessQueriesKeepTheirOriginalStackSemantics()
        {
            var source = Put("StoneburrPatch");
            var ground = Put("Grass");
            var cell = Zone.GetCell(10, 10);

            Assert.AreSame(Player, WorldInteractionSystem.ResolveTarget(cell));
            Assert.IsTrue(WorldInteractionSystem.IsPileCell(cell));
            CollectionAssert.AreEqual(new[] { Player.ID, source.ID, ground.ID },
                WorldInteractionSystem.BuildTargetPickerActions(cell)
                    .Select(row => row.Command.Substring(WorldInteractionSystem.PickTargetCommandPrefix.Length)).ToArray());

            Assert.AreSame(source, WorldInteractionSystem.ResolveTarget(cell, Player));
            Assert.IsFalse(WorldInteractionSystem.IsPileCell(cell, Player));
            CollectionAssert.AreEqual(new[] { source.ID, ground.ID },
                WorldInteractionSystem.BuildTargetPickerActions(cell, Player)
                    .Select(row => row.Command.Substring(WorldInteractionSystem.PickTargetCommandPrefix.Length)).ToArray());
        }

        [Test]
        public void ForeignActorWithTheSameIdDoesNotExcludeTheRealOccupant()
        {
            var source = Put("StoneburrPatch");
            var foreign = new Entity { ID = Player.ID, BlueprintName = Player.BlueprintName };
            foreign.SetTag("Player");
            Assert.IsTrue(new Zone("foreign").AddEntity(foreign, 10, 10));
            var cell = Zone.GetCell(10, 10);

            Assert.AreSame(Player, WorldInteractionSystem.ResolveTarget(cell, foreign));
            Assert.IsTrue(WorldInteractionSystem.IsPileCell(cell, foreign));
            CollectionAssert.AreEqual(new[] { Player.ID, source.ID },
                WorldInteractionSystem.BuildTargetPickerActions(cell, foreign)
                    .Select(row => row.Command.Substring(WorldInteractionSystem.PickTargetCommandPrefix.Length)).ToArray());
        }

        [TestCase(false)]
        [TestCase(true)]
        public void OnlyTheUnspentUnderfootSourceOffersHarvest(bool spent)
        {
            var source = Put("StoneburrPatch");
            source.GetPart<HarvestablePart>().Harvested = spent;
            var input = Input();

            Open(input);

            Assert.AreSame(source, input.WorldActionMenuUI.SelectedTarget);
            Assert.AreEqual(!spent, Actions(input).Any(row => row.Command == "Harvest"));
        }

        [Test]
        public void UnderfootHarvestExecutesOnceOnTheExactSelectedOwnerAndPaysOneAction()
        {
            var source = Put("StoneburrPatch");
            source.GetPart<HarvestablePart>().YieldMin = source.GetPart<HarvestablePart>().YieldMax = 1;
            var input = Input();
            Open(input);
            Assert.AreSame(source, input.WorldActionMenuUI.SelectedTarget);
            var action = Actions(input).Single(row => row.Command == "Harvest");
            var cell = input.WorldActionMenuUI.SelectedCell;
            int tick = input.TurnManager.TickCount, energy = input.TurnManager.GetEnergy(Player);

            Select(input, "Harvest");

            Assert.AreEqual(1, Seeds());
            Assert.IsTrue(source.GetPart<HarvestablePart>().Harvested);
            Assert.IsNull(Zone.GetEntityCell(source));
            Assert.AreEqual(energy - TurnManager.ActionThreshold
                + (input.TurnManager.TickCount - tick) * Player.GetStatValue("Speed", TurnManager.DefaultSpeed),
                input.TurnManager.GetEnergy(Player));
            Assert.Greater(input.TurnManager.TickCount, tick);
            int paidTick = input.TurnManager.TickCount, paidEnergy = input.TurnManager.GetEnergy(Player);
            Call(input, "ExecuteWorldActionSelection", action, source, cell, false);
            Assert.AreEqual(1, Seeds());
            Assert.AreEqual(paidTick, input.TurnManager.TickCount);
            Assert.AreEqual(paidEnergy, input.TurnManager.GetEnergy(Player));
        }

        [TestCase("moved-actor")]
        [TestCase("removed-source")]
        [TestCase("dead-actor")]
        [TestCase("same-id-replacement")]
        public void AnOpenUnderfootMenuDoesNotBypassExistingActionAuthority(string change)
        {
            var source = Put("StoneburrPatch");
            var input = Input();
            Open(input);
            Assert.AreSame(source, input.WorldActionMenuUI.SelectedTarget);
            var action = Actions(input).Single(row => row.Command == "Harvest");
            var cell = input.WorldActionMenuUI.SelectedCell;
            Entity replacement = null;
            if (change == "moved-actor") Assert.IsTrue(Zone.MoveEntity(Player, 14, 10));
            if (change == "removed-source" || change == "same-id-replacement")
                Assert.IsTrue(Zone.RemoveEntity(source));
            if (change == "dead-actor") Player.GetStat("Hitpoints").BaseValue = 0;
            if (change == "same-id-replacement")
            {
                replacement = Item("StoneburrPatch");
                replacement.ID = source.ID;
                Assert.IsTrue(Zone.AddEntity(replacement, 10, 10));
            }
            int tick = input.TurnManager.TickCount, energy = input.TurnManager.GetEnergy(Player);

            Call(input, "ExecuteWorldActionSelection", action, source, cell, false);

            Assert.AreEqual(0, Seeds());
            Assert.IsFalse(source.GetPart<HarvestablePart>().Harvested);
            Assert.AreEqual(tick, input.TurnManager.TickCount);
            Assert.AreEqual(energy, input.TurnManager.GetEnergy(Player));
            if (replacement != null)
            {
                Assert.AreSame(cell, Zone.GetEntityCell(replacement));
                Assert.IsFalse(replacement.GetPart<HarvestablePart>().Harvested);
            }
        }
    }
}
