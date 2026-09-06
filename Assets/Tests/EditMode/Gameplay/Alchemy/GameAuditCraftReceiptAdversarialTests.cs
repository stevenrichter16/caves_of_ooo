using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Diagnostics;
using NUnit.Framework;
namespace CavesOfOoo.Tests
{
    public class GameAuditCraftReceiptAdversarialTests : CraftReceiptFixture
    {
        private Action<string> _oldMessage;
        [SetUp] public void CaptureMessage() { _oldMessage = MessageLog.OnMessage; }
        [TearDown] public void RestoreMessage() { MessageLog.OnMessage = _oldMessage; }
        private bool Craft(Entity actor, bool brew, Entity reagent) => brew
            ? BrewingService.TryBrew(actor, Factory, new[] { reagent }, out _, out _, out _)
            : TinkeringService.TryCraft(actor, Factory, "craft_dagger", out _, out _);

        [TestCase(false)] [TestCase(true)] public void MaxStackBoundaryReturnsEachExactReceivingStack(bool brew)
        {
            var actor = Crafter(); var resident = brew ? BrewUnit(actor) : CarryUnit(actor, "Dagger"); resident.GetPart<StackerPart>().StackCount = 98;
            List<Entity> outputs;
            if (brew) { var reagent = CarryUnit(actor, "GlimmerBrine", 2); Assert.IsTrue(BrewingService.TryBrewBatch(actor, Factory, new[] { reagent }, 2, out outputs, out _, out _, out _)); }
            else { BuildSetup(actor, 2); Assert.IsTrue(TinkeringService.TryCraft(actor, Factory, "craft_dagger", out outputs, out _)); }
            Assert.AreEqual(2, outputs.Count); Assert.AreSame(resident, outputs[0]); Assert.AreNotSame(resident, outputs[1]); Assert.AreEqual(99, Quantity(resident)); Assert.AreEqual(1, Quantity(outputs[1])); Assert.Contains(outputs[1], actor.GetPart<InventoryPart>().Objects);
        }
        [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
        public void CapacityUsesActualResidentMassAndRestoresPartsAndHandling(bool brew, bool enough)
        {
            var actor = Crafter(); var resident = brew ? BrewUnit(actor) : CarryUnit(actor, "Dagger"); var inv = actor.GetPart<InventoryPart>();
            var handling = resident.GetPart<HandlingPart>(); if (handling == null) { handling = new HandlingPart(); resident.AddPart(handling); } handling.Weight = 10; handling.CarryMovePenalty = 3;
            actor.GetStat("Speed").Penalty += 7; // Unrelated modifier must survive both branches.
            BuildSetup(actor); var reagent = brew ? CarryUnit(actor, "GlimmerBrine") : null;
            inv.MaxWeight = enough ? 20 : 15; inv.RefreshHandlingCarryPenalty(); var before = new PackState(inv); var parts = resident.Parts.ToArray(); int speed = actor.GetStat("Speed").Penalty; Assert.AreEqual(10, speed, "Nonzero handling plus unrelated penalty is required.");
            Assert.AreEqual(enough, Craft(actor, brew, reagent)); CollectionAssert.AreEqual(parts, resident.Parts);
            if (!enough) { before.AssertRestored(); Assert.AreEqual(speed, actor.GetStat("Speed").Penalty); Assert.AreEqual(2, actor.GetPart<BitLockerPart>().GetBitCount('B')); }
            else { Assert.AreEqual(2, Quantity(resident)); Assert.AreEqual(20, inv.GetCarriedWeight()); Assert.AreEqual(speed + 3, actor.GetStat("Speed").Penalty); }
        }
        [TestCase(false)] [TestCase(true)] public void ExplicitBrewSelectionIsFrozenAcrossPreparationAndBatch(bool batch)
        {
            var actor = Crafter(); var chosen = CarryUnit(actor, "GlimmerBrine", 3); var substitute = CarryUnit(actor, "SparkRoot", 3); var mix = new[] { chosen };
            Probe("BrewedTonic", created => { mix[0] = substitute; });
            if (batch) { Assert.IsTrue(BrewingService.TryBrewBatch(actor, Factory, mix, 2, out var made, out _, out int count, out _)); Assert.AreEqual(2, count); Assert.AreSame(made[0], made[1]); }
            else Assert.IsTrue(BrewingService.TryBrew(actor, Factory, mix, out _, out _, out _));
            Assert.AreEqual(batch ? 1 : 2, Quantity(chosen)); Assert.AreEqual(3, Quantity(substitute)); Assert.AreSame(substitute, mix[0], "Independent caller mutation is retained.");
        }
        [Test] public void RecipeIntentIsFrozenBeforePreparingEveryOutput()
        {
            var actor = Crafter(); BuildSetup(actor, 2, "PaleSalt"); var salt = CarryUnit(actor, "PaleSalt", 2); int calls = 0;
            Probe("Dagger", created => { calls++; Assert.IsTrue(TinkerRecipeRegistry.TryGetRecipe("craft_dagger", out var recipe)); recipe.Cost = "BBBB"; recipe.Blueprint = "Torch"; recipe.NumberMade = 9; recipe.Ingredient = "SparkRoot"; });
            Assert.IsTrue(TinkeringService.TryCraft(actor, Factory, "craft_dagger", out var made, out var why), why);
            Assert.AreEqual(2, calls); Assert.AreEqual(2, made.Count); Assert.IsTrue(made.All(e => e.BlueprintName == "Dagger")); Assert.AreEqual(1, Quantity(salt)); Assert.AreEqual(1, actor.GetPart<BitLockerPart>().GetBitCount('B')); Assert.AreEqual(1, actor.GetPart<BitLockerPart>().GetBitCount('C'));
        }
        [TestCase(false)] [TestCase(true)] public void RenamedIngredientMustStillMatchFrozenRecipe(bool caseOnly)
        {
            var actor = Crafter(); BuildSetup(actor, ingredient: "PaleSalt"); var salt = CarryUnit(actor, "PaleSalt", 2);
            Probe("Dagger", created => salt.BlueprintName = caseOnly ? "pAlEsAlT" : "ChoirIron");
            Assert.AreEqual(caseOnly, TinkeringService.TryCraft(actor, Factory, "craft_dagger", out _, out _));
            Assert.AreEqual(caseOnly ? 1 : 2, Quantity(salt)); Assert.AreEqual(caseOnly ? 1 : 2, actor.GetPart<BitLockerPart>().GetBitCount('B')); Assert.AreEqual(caseOnly ? "pAlEsAlT" : "ChoirIron", salt.BlueprintName);
        }
        [TestCase(false)] [TestCase(true)] public void PreparedBrewMustStillHaveReagentEligibility(bool remove)
        {
            var actor = Crafter(); var source = CarryUnit(actor, "GlimmerBrine", 2);
            Probe("BrewedTonic", created => { if (remove) source.RemovePart(source.GetPart<ReagentPart>()); else source.GetPart<RenderPart>().DisplayName = "renamed brine"; });
            Assert.AreEqual(!remove, BrewingService.TryBrew(actor, Factory, new[] { source }, out _, out _, out _));
            Assert.AreEqual(remove ? 2 : 1, Quantity(source)); Assert.AreEqual(!remove, source.HasPart<ReagentPart>()); if (!remove) Assert.AreEqual("renamed brine", source.GetPart<RenderPart>().DisplayName);
        }
        [TestCase(false)] [TestCase(true)] public void IndependentBitSpendingIsRetainedAndAffordabilityRechecked(bool deplete)
        {
            var actor = Crafter(); BuildSetup(actor); var bits = actor.GetPart<BitLockerPart>();
            Probe("Dagger", created => Assert.IsTrue(bits.UseBits(deplete ? "BBCC" : "BC")));
            Assert.AreEqual(!deplete, TinkeringService.TryCraft(actor, Factory, "craft_dagger", out var made, out _)); Assert.AreEqual(0, bits.GetBitCount('B')); Assert.AreEqual(0, bits.GetBitCount('C')); Assert.AreEqual(deplete ? 0 : 1, made.Count);
        }
        [TestCase(false)] [TestCase(true)] public void RecipeKnowledgeIsRecheckedWithoutUndoingIndependentChanges(bool revoke)
        {
            var actor = Crafter(); BuildSetup(actor); var bits = actor.GetPart<BitLockerPart>();
            Probe("Dagger", created => { if (revoke) bits.RestoreBitsAndRecipes(bits.GetBitsSnapshot(), Array.Empty<string>()); else bits.LearnRecipe("craft_torch"); });
            Assert.AreEqual(!revoke, TinkeringService.TryCraft(actor, Factory, "craft_dagger", out _, out _)); Assert.AreEqual(revoke ? 2 : 1, bits.GetBitCount('B')); Assert.AreEqual(!revoke, bits.KnowsRecipe("craft_dagger")); if (!revoke) Assert.IsTrue(bits.KnowsRecipe("craft_torch"));
        }
        [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
        public void LateSourceRemovalOrExhaustionRefusesWithoutUndoingPreparation(bool brew, bool remove)
        {
            var actor = Crafter(); BuildSetup(actor, ingredient: "PaleSalt"); var source = CarryUnit(actor, brew ? "GlimmerBrine" : "PaleSalt", 2); var inv = actor.GetPart<InventoryPart>();
            Probe(brew ? "BrewedTonic" : "Dagger", created => { if (remove) Assert.IsTrue(inv.RemoveObject(source)); else source.GetPart<StackerPart>().StackCount = 0; });
            Assert.IsFalse(Craft(actor, brew, source)); Assert.AreEqual(!remove, inv.Objects.Contains(source)); Assert.AreEqual(remove ? 2 : 0, Quantity(source)); Assert.AreEqual(2, actor.GetPart<BitLockerPart>().GetBitCount('B')); Assert.IsFalse(inv.Objects.Any(e => e.BlueprintName == (brew ? "BrewedTonic" : "Dagger")));
        }
        [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
        public void UnrelatedDropSurvivesBothCraftSuccessAndRefusal(bool brew, bool succeeds)
        {
            var actor = Crafter(); BuildSetup(actor); var inv = actor.GetPart<InventoryPart>(); var source = brew ? CarryUnit(actor, "GlimmerBrine", 2) : null; var torch = CarryUnit(actor, "Torch");
            var zone = new Zone("CraftPrepDrop"); Assert.IsTrue(zone.AddEntity(actor, 10, 10)); inv.MaxWeight = succeeds ? 100 : 0;
            Probe(brew ? "BrewedTonic" : "Dagger", created => Assert.IsTrue(InventorySystem.ExecuteCommand(new DropCommand(torch), actor, zone).Success));
            Assert.AreEqual(succeeds, Craft(actor, brew, source)); Assert.IsFalse(inv.Objects.Contains(torch)); Assert.Contains(torch, zone.GetCell(10, 10).Objects); Assert.IsNull(torch.GetPart<PhysicsPart>().InInventory);
            Assert.AreEqual(succeeds && !brew ? 1 : 2, actor.GetPart<BitLockerPart>().GetBitCount('B')); if (brew) Assert.AreEqual(succeeds ? 1 : 2, Quantity(source));
        }
        [TestCase(false)] [TestCase(true)] public void PublicationExceptionKeepsCommittedOutputAndReleasesGuard(bool brew)
        {
            var actor = Crafter(); BuildSetup(actor); var source = brew ? CarryUnit(actor, "GlimmerBrine", 2) : null;
            MessageLog.OnMessage = message => { if (message.Contains(brew ? " brews " : " crafts ")) throw new InvalidOperationException("publication"); };
            Assert.Throws<InvalidOperationException>(() => Craft(actor, brew, source)); MessageLog.OnMessage = _oldMessage;
            var inv = actor.GetPart<InventoryPart>(); var resident = inv.Objects.Single(e => e.BlueprintName == (brew ? "BrewedTonic" : "Dagger")); Assert.AreEqual(1, Quantity(resident));
            Assert.IsTrue(Craft(actor, brew, source)); Assert.AreEqual(2, Quantity(resident)); Assert.AreEqual(brew ? 2 : 0, actor.GetPart<BitLockerPart>().GetBitCount('B'));
        }
        [TestCase(false)] [TestCase(true)] public void OutputAdoptedByAnotherInventoryIsNotDuplicatedOrReclaimed(bool brew)
        {
            var actor = Crafter(); var other = Crafter(); BuildSetup(actor); var source = brew ? CarryUnit(actor, "GlimmerBrine", 2) : null;
            Probe(brew ? "BrewedTonic" : "Dagger", created => Assert.IsTrue(other.GetPart<InventoryPart>().AddObject(created)));
            Assert.IsFalse(Craft(actor, brew, source)); Assert.AreEqual(1, other.GetPart<InventoryPart>().Objects.Count); Assert.AreSame(other, other.GetPart<InventoryPart>().Objects[0].GetPart<PhysicsPart>().InInventory); Assert.AreEqual(2, actor.GetPart<BitLockerPart>().GetBitCount('B')); if (brew) Assert.AreEqual(2, Quantity(source));
        }
        [TestCase(false, -1)] [TestCase(false, 0)] [TestCase(false, 2)] [TestCase(true, -1)] [TestCase(true, 0)] [TestCase(true, 2)]
        public void NonUnitPreparedOutputsRefuseAndRestorePayment(bool brew, int quantity)
        {
            var actor = Crafter(); BuildSetup(actor, ingredient: "PaleSalt"); var source = CarryUnit(actor, brew ? "GlimmerBrine" : "PaleSalt", 2); var before = new PackState(actor.GetPart<InventoryPart>());
            Probe(brew ? "BrewedTonic" : "Dagger", created => created.GetPart<StackerPart>().StackCount = quantity);
            Assert.IsFalse(Craft(actor, brew, source)); before.AssertRestored(); Assert.AreEqual(2, actor.GetPart<BitLockerPart>().GetBitCount('B'));
        }
        [TestCase(false)] [TestCase(true)] public void UnstackedOutputIsStillOneCarriedUnit(bool brew)
        {
            var actor = Crafter(); BuildSetup(actor); var source = brew ? CarryUnit(actor, "GlimmerBrine") : null;
            Probe(brew ? "BrewedTonic" : "Dagger", created => created.RemovePart(created.GetPart<StackerPart>()));
            Assert.IsTrue(Craft(actor, brew, source)); var made = actor.GetPart<InventoryPart>().Objects.Single(e => e.BlueprintName == (brew ? "BrewedTonic" : "Dagger")); Assert.IsNull(made.GetPart<StackerPart>()); Assert.AreSame(actor, made.GetPart<PhysicsPart>().InInventory);
        }
        [TestCase(false)] [TestCase(true)] public void BrewReportsActualRecipientButProseDescribesOneUnit(bool merged)
        {
            var actor = Crafter(); if (merged) BrewUnit(actor); Diag.SetChannel("alchemy", true); var made = BrewUnit(actor);
            var record = DiagQuery.Apply(new DiagQuery.Filter { Kind = "BrewResolved", Actor = actor.ID }).Records.Last(); Assert.AreEqual(made.ID, record.TargetId);
            StringAssert.DoesNotContain("(x2)", MessageLog.GetLast()); StringAssert.Contains(" brews ", MessageLog.GetLast()); Assert.AreEqual(merged ? 2 : 1, Quantity(made));
        }
        [TestCase(false)] [TestCase(true)] public void ClaimedSourceRefusesWithoutChangingIndependentTransfer(bool brew)
        {
            var actor = Crafter(); BuildSetup(actor, ingredient: "PaleSalt"); var source = CarryUnit(actor, brew ? "GlimmerBrine" : "PaleSalt", 2); var before = new PackState(actor.GetPart<InventoryPart>()); var held = new InventoryTransaction();
            Assert.IsTrue((bool)typeof(InventoryTransaction).GetMethod("TryClaim", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(held, new object[] { source, actor, "independent" }));
            try { Assert.IsFalse(Craft(actor, brew, source)); before.AssertRestored(); Assert.IsFalse(held.IsCommitted); Assert.IsFalse(held.IsRolledBack); }
            finally { held.Rollback(); }
            Assert.IsTrue(Craft(actor, brew, source)); Assert.AreEqual(1, Quantity(source));
        }
        [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
        public void TinkerServiceEmitsAuthoritativeCompletionOrRefusal(bool succeeds, bool ingredient)
        {
            var actor = Crafter(); BuildSetup(actor, ingredient: ingredient ? "PaleSalt" : null); if (ingredient) CarryUnit(actor, "PaleSalt", 2); actor.GetPart<InventoryPart>().MaxWeight = succeeds ? 100 : 0; Diag.SetChannel("event", true);
            Assert.AreEqual(succeeds, TinkeringService.TryCraft(actor, Factory, "craft_dagger", out var made, out _));
            Assert.AreEqual(1, DiagQuery.Count(new DiagQuery.Filter { Kind = succeeds ? "CraftCompleted" : "CraftRejected", Actor = actor.ID }).Count);
            Assert.AreEqual(0, DiagQuery.Count(new DiagQuery.Filter { Kind = succeeds ? "CraftRejected" : "CraftCompleted", Actor = actor.ID }).Count);
            var record = DiagQuery.Apply(new DiagQuery.Filter { Kind = succeeds ? "CraftCompleted" : "CraftRejected", Actor = actor.ID }).Records.Single(); StringAssert.Contains(succeeds ? "craft_dagger" : "reason", record.PayloadJson);
        }
    }
}
