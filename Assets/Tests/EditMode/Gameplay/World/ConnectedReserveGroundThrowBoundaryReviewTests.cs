using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class ConnectedReserveGroundThrowBoundaryReviewTests : ConnectedReserveTestBase
    {
        Entity UncollectedProduce(bool stack)
        {
            Assert.True(Reserve.MoveEntity(Player, 4, 6));
            Assert.True(Reserve.MoveEntity(Keeper, 25, 5));
            Assert.True(Harvest(Crop()).Success);
            var produce = Reserve.GetCell(4, 5).Objects.First(e => e.BlueprintName == "MarlrootClod");
            if (stack)
            {
                var other = Reserve.GetCell(4, 5).Objects.First(e => e.BlueprintName == "MarlrootClod" && e != produce);
                Assert.AreEqual(1, produce.GetPart<StackerPart>().MergeFrom(other));
                Assert.True(Reserve.RemoveEntity(other));
            }
            Assert.True(Reserve.MoveEntity(Keeper, 5, 5));
            return produce;
        }

        // Exercise rollback after real extraction and landing, when the taking
        // receipt has already been staged but may not publish knowledge.
        [TestCase(false)] [TestCase(true)]
        public void OuterThrowFailureRestoresGroundUnitsAndExactClaimWithoutWitness(bool split)
        {
            var produce = UncollectedProduce(split);
            var marker = produce.GetPart<ReserveYieldPart>();
            var result = InventorySystem.ExecuteCommand(new RefuseAfterThrow(produce), Player, Reserve);
            Assert.False(result.Success);
            Assert.AreEqual((4, 5), Reserve.GetEntityPosition(produce));
            Assert.AreEqual(split ? 2 : 1, produce.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(2, Reserve.GetCell(4, 5).Objects.Where(e => e.BlueprintName == "MarlrootClod")
                .Sum(e => e.GetPart<StackerPart>().StackCount));
            Assert.False(Reserve.GetCell(4, 7).Objects.Any(e => e.BlueprintName == "MarlrootClod"));
            Assert.AreSame(marker, produce.GetPart<ReserveYieldPart>());
            Assert.False(marker.Released);
            Assert.Null(marker.AlreadyWitnessedPlayer);
            Assert.AreEqual("Unknown", State());
        }

        [TestCase(false)] [TestCase(true)]
        public void ActualGroundThrowPopupPreservesContextualWarningBeforeAim(bool permitted)
        {
            var produce = UncollectedProduce(false);
            if (permitted) Assert.True(Act("pay"));
            using (var ui = new HotbarSaveFixture(true, false))
            {
                var turns = new TurnManager();
                turns.RestoreSavedState(17, true, Player, new List<TurnManager.SavedTurnEntry>
                    { new TurnManager.SavedTurnEntry { Entity = Player, Energy = TurnManager.ActionThreshold } });
                ui.BindOld(GameSessionState.Capture("reserve-throw-popup", "read-only", Manager, turns, Player));
                SettlementRuntime.ActiveZone = Reserve;
                const BindingFlags hidden = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(InputHandler).GetMethod("OpenWorldThrowActionPopup", hidden)
                    .Invoke(ui.Input, new object[] { produce, 4, 5 });
                var popup = typeof(InputHandler).GetField("_throwPopup", hidden).GetValue(ui.Input);
                var rows = (IList)popup.GetType().GetField("Options").GetValue(popup);
                var label = (string)rows[0].GetType().GetField("Label").GetValue(rows[0]);
                Assert.AreEqual(!permitted, label.Contains("Nella's tied reserve"));
                Assert.LessOrEqual(label.Length, 28, "The real fixed-width popup must show the whole warning.");
                Assert.AreEqual(17, turns.TickCount);
                Assert.AreEqual(permitted ? "Granted" : "Unknown", State());
                Assert.NotNull(produce.GetPart<ReserveYieldPart>());
            }
        }

        sealed class RefuseAfterThrow : IInventoryCommand
        {
            readonly ThrowItemCommand inner;
            public RefuseAfterThrow(Entity item) => inner = new ThrowItemCommand(item, 4, 7, new Random(64));
            public string Name => "ReserveThrowOuterFailureReview";
            public InventoryValidationResult Validate(InventoryContext context) => inner.Validate(context);
            public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
            {
                var result = inner.Execute(context, transaction);
                return result.Success ? InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed,
                    "Intentional outer failure after the actual throw.") : result;
            }
        }
    }
}
