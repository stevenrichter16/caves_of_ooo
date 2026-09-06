using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public sealed class SeparateOneCloneProbePart : Part
    {
        public static Action<Entity> Callback;
        public override string Name => "SeparateOneCloneProbe";
        public override void Initialize() => Callback?.Invoke(ParentEntity);
    }
    public class GameAuditSeparateOneAdversarialTests : SeparateOneFixture
    {
        [TearDown] public void ClearProbes() { SeparateOneCloneProbePart.Callback = null; MessageLog.OnMessage = null; }

        [TestCase(false)] [TestCase(true)]
        public void CloneExceptionLeavesSourceUntouchedAndReleasesClaimForRetry(bool throws)
        {
            var actor = Crafter(); var source = Units(actor, "Dagger", 3); source.AddPart(new SeparateOneCloneProbePart());
            string id = source.ID; var inv = actor.GetPart<InventoryPart>(); var before = inv.Objects.ToArray();
            SeparateOneCloneProbePart.Callback = _ => { if (throws) throw new InvalidOperationException("Clone probe"); };
            var command = Command(source); var result = InventorySystem.ExecuteCommand(command, actor);
            Assert.AreEqual(!throws, result.Success); Assert.AreEqual(id, source.ID); Assert.AreEqual(throws ? 3 : 2, Quantity(source));
            if (throws)
            {
                Assert.AreEqual(InventoryCommandErrorCode.Exception, result.ErrorCode); CollectionAssert.AreEqual(before, inv.Objects); Assert.IsNull(Recipient(command));
                SeparateOneCloneProbePart.Callback = null; Fresh(Separate(actor, source), source);
            }
        }

        [TestCase("remove")] [TestCase("one")] [TestCase("zero")]
        public void RevalidationKeepsIndependentChangesMadeDuringClone(string change)
        {
            var actor = Crafter(); var source = Units(actor, "Dagger", 3); source.AddPart(new SeparateOneCloneProbePart());
            var inv = actor.GetPart<InventoryPart>(); string id = source.ID;
            SeparateOneCloneProbePart.Callback = _ =>
            {
                SeparateOneCloneProbePart.Callback = null;
                if (change == "remove") Assert.IsTrue(inv.RemoveObject(source)); else source.GetPart<StackerPart>().StackCount = change == "one" ? 1 : 0;
            };
            var command = Command(source); Assert.IsFalse(InventorySystem.ExecuteCommand(command, actor).Success); Assert.IsNull(Recipient(command));
            Assert.AreEqual(change == "remove" ? 3 : change == "one" ? 1 : 0, Quantity(source)); Assert.AreEqual(id, source.ID);
            Assert.AreEqual(change != "remove", inv.Objects.Contains(source)); Assert.AreEqual(change == "remove" ? 0 : 1, inv.Objects.Count);
        }

        [TestCase(2, false)] [TestCase(2, true)] [TestCase(3, false)] [TestCase(3, true)]
        public void SharedTransactionCloneNestingKeepsMutationOrder(int count, bool rollback)
        {
            var actor = Crafter(); var source = Units(actor, "Dagger", count); source.AddPart(new SeparateOneCloneProbePart());
            var inv = actor.GetPart<InventoryPart>(); var before = inv.Objects.ToArray(); var tx = new InventoryTransaction();
            IInventoryCommand inner = null; var outer = Command(source);
            try
            {
                SeparateOneCloneProbePart.Callback = _ =>
                {
                    SeparateOneCloneProbePart.Callback = null; inner = Command(source);
                    Assert.IsTrue(inner.Execute(new InventoryContext(actor), tx).Success);
                };
                var result = outer.Execute(new InventoryContext(actor), tx); Assert.AreEqual(count == 3, result.Success);
                Assert.NotNull(Recipient(inner)); Assert.AreEqual(1, Quantity(source));
                var innerUnit = Recipient(inner); var outerUnit = Recipient(outer);
                if (rollback) tx.Rollback(); else tx.Commit();
                if (rollback)
                {
                    CollectionAssert.AreEqual(before, inv.Objects); Assert.AreEqual(count, Quantity(source));
                    Assert.IsNull(Recipient(inner)); Assert.IsNull(Recipient(outer));
                    foreach (var detached in new[] { innerUnit, outerUnit }.Where(e => e != null))
                    {
                        Assert.IsNull(detached.GetPart<PhysicsPart>().InInventory);
                        Assert.IsNull(detached.GetPart<PhysicsPart>().Equipped);
                    }
                }
                else
                {
                    Assert.AreEqual(count, inv.Objects.Count); Assert.AreEqual(count, inv.Objects.Sum(Quantity));
                    Assert.AreEqual(count, inv.Objects.Select(e => e.ID).Distinct().Count()); Assert.IsTrue(inv.Objects.Contains(Recipient(inner)));
                    Assert.AreEqual(count == 3, Recipient(outer) != null);
                }
            }
            finally { tx.Rollback(); }
        }

        [TestCase("commit")] [TestCase("false")] [TestCase("throw")]
        public void OuterCommandFailureRestoresSourceAndClearsRecipient(string outcome)
        {
            var actor = Crafter(); var source = Units(actor, "Dagger", 3); var inv = actor.GetPart<InventoryPart>();
            var other = Units(actor, "SilverSand", 2); var before = inv.Objects.ToArray(); string id = source.ID;
            var command = Command(source); var result = InventorySystem.ExecuteCommand(new FinishAfter(command, outcome), actor);
            Assert.AreEqual(outcome == "commit", result.Success); Assert.AreEqual(id, source.ID); Assert.AreEqual(2, Quantity(other));
            if (outcome != "commit")
            {
                CollectionAssert.AreEqual(before, inv.Objects); Assert.AreEqual(3, Quantity(source)); Assert.IsNull(Recipient(command));
                Fresh(Separate(actor, source), source);
            }
            else Assert.AreSame(actor, Recipient(command).GetPart<PhysicsPart>().InInventory);
        }

        [TestCase("commit")] [TestCase("clone_throw")] [TestCase("outer_false")]
        public void IndependentDropDuringCloneSurvivesSeparationFailure(string outcome)
        {
            var actor = Crafter(); var source = Units(actor, "Dagger", 3); source.AddPart(new SeparateOneCloneProbePart());
            var torch = Units(actor, "Torch", 1); var inv = actor.GetPart<InventoryPart>(); var zone = new Zone("SeparateIndependent"); Assert.IsTrue(zone.AddEntity(actor, 10, 10));
            bool called = false;
            SeparateOneCloneProbePart.Callback = _ =>
            {
                SeparateOneCloneProbePart.Callback = null; called = true;
                Assert.IsFalse(InventorySystem.ExecuteCommand(Command(source), actor, zone).Success, "independent nested separation cannot reuse claimed source");
                Assert.IsTrue(InventorySystem.Drop(actor, torch, zone));
                if (outcome == "clone_throw") throw new InvalidOperationException("After independent drop");
            };
            var command = Command(source); var result = InventorySystem.ExecuteCommand(new FinishAfter(command, outcome == "outer_false" ? "false" : "commit"), actor, zone);
            Assert.IsTrue(called); Assert.AreEqual(outcome == "commit", result.Success); Assert.IsFalse(inv.Objects.Contains(torch));
            Assert.AreSame(zone.GetEntityCell(actor), zone.GetEntityCell(torch)); Assert.AreEqual(outcome == "commit" ? 2 : 3, Quantity(source));
            Assert.AreEqual(outcome == "commit" ? 2 : 1, inv.Objects.Count);
        }

        [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
        public void PublicationProtectsBothParticipantsButAllowsUnrelatedDrop(bool chooseNew, bool fail)
        {
            var actor = Crafter(); var source = Units(actor, "Dagger", 3); var torch = Units(actor, "Torch", 1);
            var inv = actor.GetPart<InventoryPart>(); var zone = new Zone("SeparatePublication"); Assert.IsTrue(zone.AddEntity(actor, 10, 10)); bool called = false;
            var command = Command(source);
            MessageLog.OnMessage = message =>
            {
                if (called || !message.Contains(" separates one ")) return; called = true;
                var target = chooseNew ? Recipient(command) : source; Assert.NotNull(target);
                Assert.IsFalse(InventorySystem.Drop(actor, target, zone), "source and singleton are claimed before notification");
                Assert.IsTrue(InventorySystem.Drop(actor, torch, zone)); if (fail) throw new InvalidOperationException("Publication probe");
            };
            var result = InventorySystem.ExecuteCommand(command, actor, zone); MessageLog.OnMessage = null;
            Assert.IsTrue(called); Assert.AreEqual(!fail, result.Success); Assert.AreEqual(fail ? 3 : 2, Quantity(source));
            Assert.IsFalse(inv.Objects.Contains(torch)); Assert.NotNull(zone.GetEntityCell(torch));
            Assert.IsNull(zone.GetEntityCell(source)); Assert.AreEqual(fail ? 1 : 2, inv.Objects.Count);
            if (fail) Assert.IsNull(Recipient(command));
        }

        [Test] public void RepeatedSeparationNeverMergesIntoEarlierIdenticalSingletons()
        {
            var actor = Crafter(); var source = Units(actor, "Dagger", 4); var inv = actor.GetPart<InventoryPart>();
            var first = Separate(actor, source); var second = Separate(actor, source); var third = Separate(actor, source);
            CollectionAssert.AreEquivalent(new[] { source, first, second, third }, inv.Objects);
            Assert.IsTrue(inv.Objects.All(e => Quantity(e) == 1)); Assert.AreEqual(4, inv.Objects.Select(e => e.ID).Distinct().Count());
            Assert.IsFalse(InventorySystem.ExecuteCommand(Command(source), actor).Success);
        }

        [TestCase("SteelBladeComponent")] [TestCase("GlimmerBrine")]
        public void SeparationKeepsOriginalMarkPartAndUnrelatedSelection(string bp)
        {
            var actor = Crafter(); var source = Units(actor, bp, 3); var other = Units(actor, "SparkRoot", 2);
            CraftingMarkPart.Toggle(source); CraftingMarkPart.Toggle(other); var mark = source.GetPart<CraftingMarkPart>();
            var otherMark = other.GetPart<CraftingMarkPart>(); var unit = Separate(actor, source);
            Assert.AreSame(mark, source.GetPart<CraftingMarkPart>()); Assert.AreSame(otherMark, other.GetPart<CraftingMarkPart>()); Assert.IsFalse(CraftingMarkPart.IsMarked(unit));
        }

        [TestCase(false)] [TestCase(true)]
        public void SaveAndOrdinaryReinsertionRetainOnlyActualPayloadDifferences(bool sharp)
        {
            var actor = Crafter(); var source = Units(actor, "Dagger", 3); var unit = Separate(actor, source); string sourceId = source.ID, unitId = unit.ID;
            if (sharp)
            {
                var bits = actor.GetPart<BitLockerPart>(); bits.LearnRecipe("mod_sharp_melee"); bits.AddBits("BC");
                Assert.IsTrue(TinkeringService.TryApplyModification(actor, "mod_sharp_melee", unit, out _));
            }
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(actor); var inv = loaded.GetPart<InventoryPart>();
            var a = inv.Objects.Single(e => e.ID == sourceId); var b = inv.Objects.Single(e => e.ID == unitId);
            Assert.AreEqual(2, Quantity(a)); Assert.AreEqual(1, Quantity(b)); Assert.AreEqual(sharp, b.HasTag("ModSharp")); Assert.IsFalse(a.HasTag("ModSharp"));
            Assert.IsTrue(inv.RemoveObject(b)); Assert.IsTrue(inv.AddObject(b));
            Assert.AreEqual(sharp ? 2 : 1, inv.Objects.Count); Assert.AreEqual(sharp ? 2 : 3, Quantity(a)); Assert.AreEqual(sourceId, a.ID);
        }

        [Test] public void ValidationQueryDoesNotErasePreviouslyCommittedResult()
        {
            var actor = Crafter(); var source = Units(actor, "Dagger", 2); var command = Command(source);
            Assert.IsTrue(InventorySystem.ExecuteCommand(command, actor).Success); var unit = Recipient(command);
            Assert.IsFalse(command.Validate(new InventoryContext(actor)).IsValid);
            Assert.AreSame(unit, Recipient(command)); Assert.IsTrue(actor.GetPart<InventoryPart>().Objects.Contains(unit));
            Assert.AreEqual(1, Quantity(source)); Assert.AreEqual(1, Quantity(unit));
        }

        [TestCase(false)] [TestCase(true)]
        public void PreparedCloneWeightChangeRefusesWithoutChangingSourceOrHandling(bool heavier)
        {
            var actor = Crafter(); var source = Units(actor, "Dagger", 3); source.AddPart(new SeparateOneCloneProbePart());
            var inv = actor.GetPart<InventoryPart>(); source.GetPart<HandlingPart>().CarryMovePenalty = 2; inv.RefreshHandlingCarryPenalty();
            int weight = inv.GetCarriedWeight(), handling = actor.GetStat("Speed").Penalty, unitWeight = HandlingService.GetWeight(source);
            Entity prepared = null;
            SeparateOneCloneProbePart.Callback = clone =>
            {
                prepared = clone; if (heavier) clone.GetPart<HandlingPart>().Weight = unitWeight + 1;
            };
            var command = Command(source); var result = InventorySystem.ExecuteCommand(command, actor);
            Assert.NotNull(prepared); Assert.AreEqual(!heavier, result.Success); Assert.AreEqual(weight, inv.GetCarriedWeight());
            Assert.AreEqual(handling, actor.GetStat("Speed").Penalty); Assert.AreEqual(unitWeight, HandlingService.GetWeight(source));
            Assert.AreEqual(heavier ? 3 : 2, Quantity(source)); Assert.AreEqual(heavier ? 1 : 2, inv.Objects.Count);
            if (heavier)
            {
                Assert.IsNull(Recipient(command)); Assert.IsNull(prepared.GetPart<PhysicsPart>().InInventory);
                Assert.IsNull(prepared.GetPart<PhysicsPart>().Equipped); Assert.AreSame(source, inv.Objects.Single());
            }
            else Assert.AreSame(prepared, Recipient(command));
        }

        private sealed class FinishAfter : IInventoryCommand
        {
            private readonly IInventoryCommand _inner; private readonly string _outcome;
            public FinishAfter(IInventoryCommand inner, string outcome) { _inner = inner; _outcome = outcome; }
            public string Name => "SeparateOuterOutcome";
            public InventoryValidationResult Validate(InventoryContext context) => _inner.Validate(context);
            public InventoryCommandResult Execute(InventoryContext context, InventoryTransaction transaction)
            {
                var result = _inner.Execute(context, transaction); if (!result.Success) return result;
                if (_outcome == "throw") throw new InvalidOperationException("Outer separation probe");
                return _outcome == "false" ? InventoryCommandResult.Fail(InventoryCommandErrorCode.ExecutionFailed, "Outer refusal") : result;
            }
        }
    }

    public class GameAuditSeparateOneUiAdversarialTests : ActionFeedbackFixture
    {
        [TestCase("!")] [TestCase("~")]
        public void ExactRecipientFocusSurvivesEitherIDSortDirection(string id)
        {
            var source = Carry("Dagger", 3); source.ID = id; OpenAction(source, "separate_one", out int action); Call("ExecuteItemAction", action);
            var unit = Inventory.Objects.Single(e => e.BlueprintName == "Dagger" && e != source);
            Assert.AreSame(unit, Field(Get("_itemActionPopup"), "Item"));
            var row = ((IList)Get("_rows"))[(int)Get("_cursorIndex")]; Assert.AreSame(unit, Field(Field(row, "Item"), "Item"));
            Assert.AreEqual(id, source.ID);
        }
        [TestCase(false)] [TestCase(true)]
        public void RefusedPopupRetriesAfterQuantityOrOwnershipReturns(bool removed)
        {
            var source = Carry("Dagger", 3); var popup = OpenAction(source, "separate_one", out int action);
            if (removed) Assert.IsTrue(Inventory.RemoveObject(source)); else source.GetPart<StackerPart>().StackCount = 1;
            Call("ExecuteItemAction", action); Assert.AreSame(popup, Get("_itemActionPopup")); Assert.IsNotEmpty(Status);
            if (removed) Assert.IsTrue(Inventory.AddObject(source)); else source.GetPart<StackerPart>().StackCount = 3;
            Call("ExecuteItemAction", action); Assert.IsTrue(string.IsNullOrEmpty(Status)); Assert.AreNotSame(popup, Get("_itemActionPopup"));
            Assert.AreNotSame(source, Field(Get("_itemActionPopup"), "Item")); Assert.AreEqual(2, source.GetPart<StackerPart>().StackCount);
        }
        [TestCase(false)] [TestCase(true)]
        public void SuccessfulOrRefusedSeparationDoesNotAdvancePlayerTurn(bool refuse)
        {
            var old = TurnManager.Active;
            try
            {
                var turns = new TurnManager(); turns.AddEntity(Player); turns.ProcessUntilPlayerTurn();
                var source = Carry("Dagger", 3); OpenAction(source, "separate_one", out int action);
                if (refuse) source.GetPart<StackerPart>().StackCount = 1;
                int tick = turns.TickCount, energy = turns.GetEnergy(Player); Call("ExecuteItemAction", action);
                Assert.AreEqual(tick, turns.TickCount); Assert.AreEqual(energy, turns.GetEnergy(Player));
                Assert.AreSame(Player, turns.CurrentActor); Assert.IsTrue(turns.WaitingForInput);
            }
            finally { typeof(TurnManager).GetProperty("Active", BindingFlags.Public | BindingFlags.Static).SetMethod.Invoke(null, new object[] { old }); }
        }
    }
}
