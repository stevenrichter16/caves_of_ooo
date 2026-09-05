using System.IO;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Core.Inventory.Commands;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>A02 ownership, aliasing, callback ordering and saved quantity boundaries.</summary>
    public class GameAuditConsumableAdversarialTests
    {
        private EntityFactory _factory;
        private Entity _actor;
        private InventoryPart Inventory => _actor.GetPart<InventoryPart>();

        [OneTimeSetUp]
        public void Load()
        {
            _factory = new EntityFactory();
            _factory.LoadBlueprints(File.ReadAllText(Path.Combine(UnityEngine.Application.dataPath,
                "Resources/Content/Blueprints/Objects.json")));
        }
        [SetUp]
        public void SetUp()
        {
            MessageLog.Clear();
            _actor = new Entity { BlueprintName = "AuditConsumer" };
            _actor.AddPart(new InventoryPart());
            _actor.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", Owner = _actor, BaseValue = 10, Max = 100 };
        }
        private Entity Item(string blueprint, int count = 1)
        {
            var item = _factory.CreateEntity(blueprint);
            item.GetPart<StackerPart>().StackCount = count;
            return item;
        }
        private static string Command(string bp) => bp == "InkVial" ? "UseInkVial" : bp == "Starapple" ? "Eat" : "ApplyTonic";
        private void NoBenefit()
        {
            Assert.AreEqual(10, _actor.GetStatValue("Hitpoints"));
            Assert.AreEqual(0, RentalSystem.GetInk(_actor));
        }

        [TestCase("HealingTonic")] [TestCase("Starapple")] [TestCase("InkVial")]
        public void AnotherOwner_CannotBeSpentByThisActor(string bp)
        {
            var other = new Entity(); other.AddPart(new InventoryPart());
            var item = Item(bp, 3); Assert.IsTrue(other.GetPart<InventoryPart>().AddObject(item));
            Assert.IsFalse(InventorySystem.PerformAction(_actor, item, Command(bp)));
            NoBenefit(); Assert.AreEqual(3, item.GetPart<StackerPart>().StackCount);
            Assert.IsTrue(other.GetPart<InventoryPart>().Objects.Contains(item));
            Assert.AreSame(other, item.GetPart<PhysicsPart>().InInventory);
            Assert.IsTrue(InventorySystem.PerformAction(other, item, Command(bp)));
            Assert.AreEqual(2, item.GetPart<StackerPart>().StackCount);
        }

        [TestCase("HealingTonic", 0)] [TestCase("HealingTonic", -1)]
        [TestCase("Starapple", 0)] [TestCase("Starapple", -1)]
        [TestCase("InkVial", 0)] [TestCase("InkVial", -1)]
        public void InvalidQuantity_RefusesDespiteCarriedMembership(string bp, int count)
        {
            var item = Item(bp); Assert.IsTrue(Inventory.AddObject(item));
            item.GetPart<StackerPart>().StackCount = count;
            Assert.IsFalse(InventorySystem.GetActions(_actor, item).Exists(a => a.Command == Command(bp)));
            Assert.IsFalse(InventorySystem.PerformAction(_actor, item, Command(bp)));
            NoBenefit(); Assert.AreEqual(count, item.GetPart<StackerPart>().StackCount);
            Assert.IsTrue(Inventory.Objects.Contains(item));
        }

        [TestCase("HealingTonic")] [TestCase("Starapple")] [TestCase("InkVial")]
        public void StaleOwnerBackreference_DoesNotAuthorizeConsumption(string bp)
        {
            var item = Item(bp); item.GetPart<PhysicsPart>().InInventory = _actor;
            Assert.IsFalse(InventorySystem.PerformAction(_actor, item, Command(bp)));
            NoBenefit(); Assert.AreEqual(1, item.GetPart<StackerPart>().StackCount);
        }

        [TestCase("HealingTonic")] [TestCase("Starapple")] [TestCase("InkVial")]
        public void MissingInventory_RefusesMenuAndExecution(string bp)
        {
            var actor = new Entity(); var item = Item(bp);
            Assert.IsFalse(WorldInteractionSystem.GatherActions(item, actor).Exists(a => a.Command == Command(bp)));
            Assert.IsFalse(InventorySystem.PerformAction(actor, item, Command(bp)));
            Assert.AreEqual(1, item.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(0, RentalSystem.GetInk(actor));
        }

        [TestCase("HealingTonic")] [TestCase("Starapple")] [TestCase("InkVial")]
        public void BeforeActionVeto_PreservesCarriedUnitAndBenefits(string bp)
        {
            var item = Item(bp, 3); Inventory.AddObject(item); _actor.AddPart(new VetoPart());
            Assert.IsFalse(InventorySystem.PerformAction(_actor, item, Command(bp)));
            NoBenefit(); Assert.AreEqual(3, item.GetPart<StackerPart>().StackCount);
            Assert.IsTrue(Inventory.Objects.Contains(item));
        }

        [TestCase("HealingTonic")] [TestCase("Starapple")] [TestCase("InkVial")]
        public void SavedStack_CanSpendEachUnitOnceAfterReload(string bp)
        {
            Inventory.AddObject(Item(bp, 2));
            _actor = PartRoundTripHelper.RoundTripEntityViaTokenGraph(_actor);
            var item = Inventory.Objects[0];
            Assert.IsTrue(InventorySystem.PerformAction(_actor, item, Command(bp)));
            Assert.AreEqual(1, item.GetPart<StackerPart>().StackCount);
            Assert.IsTrue(InventorySystem.PerformAction(_actor, item, Command(bp)));
            Assert.IsFalse(Inventory.Objects.Contains(item));
            int hp = _actor.GetStatValue("Hitpoints"), ink = RentalSystem.GetInk(_actor);
            Assert.IsFalse(InventorySystem.PerformAction(_actor, item, Command(bp)));
            Assert.AreEqual(hp, _actor.GetStatValue("Hitpoints")); Assert.AreEqual(ink, RentalSystem.GetInk(_actor));
            if (bp == "InkVial") Assert.AreEqual(50, ink); else Assert.Greater(hp, 10);
        }

        [TestCase("HealingTonic")] [TestCase("Starapple")] [TestCase("InkVial")]
        public void UnknownCommand_DoesNotAccidentallyConsume(string bp)
        {
            var item = Item(bp); Inventory.AddObject(item);
            Assert.IsFalse(InventorySystem.PerformAction(_actor, item, "AuditNotAnAction"));
            NoBenefit(); Assert.IsTrue(Inventory.Objects.Contains(item));
            Assert.IsTrue(InventorySystem.PerformAction(_actor, item, Command(bp)));
        }

        [TestCase("HealingTonic")] [TestCase("Starapple")] [TestCase("InkVial")]
        public void ActorlessMetadata_StillDescribesItemCapability(string bp)
        {
            var item = Item(bp);
            Assert.IsTrue(WorldInteractionSystem.GatherActions(item).Exists(a => a.Command == Command(bp)));
            Assert.IsFalse(WorldInteractionSystem.GatherActions(item, _actor).Exists(a => a.Command == Command(bp)));
        }

        [TestCase(1)] [TestCase(2)]
        public void ReentrantPayload_CannotSpendTheSameUnitTwice(int count)
        {
            var item = Item("HealingTonic", count); Inventory.AddObject(item);
            var probe = new ReenterPart { Actor = _actor }; item.AddPart(probe);
            Assert.IsTrue(InventorySystem.PerformAction(_actor, item, "ApplyTonic"));
            Assert.AreEqual(count == 2, probe.NestedSucceeded);
            Assert.IsFalse(Inventory.Objects.Contains(item));
            Assert.Greater(_actor.GetStatValue("Hitpoints"), 10);
        }

        [Test]
        public void NullTonicTarget_DoesNotChargeEvenWithValidOwner()
        {
            var item = Item("HealingTonic"); Inventory.AddObject(item);
            Assert.IsFalse(item.GetPart<TonicPart>().ApplyTo(null, _actor, consumeItem: true));
            Assert.IsTrue(Inventory.Objects.Contains(item)); NoBenefit();
        }

        [Test]
        public void NonstackingUnit_ConsumesOnceEvenInNowOverweightInventory()
        {
            var item = new Entity(); item.AddPart(new PhysicsPart { Weight = 3 });
            Assert.IsTrue(Inventory.AddObject(item)); Inventory.MaxWeight = 0;
            Assert.IsTrue(Inventory.TryConsumeOne(item));
            Assert.IsFalse(Inventory.TryConsumeOne(item));
            Assert.IsFalse(Inventory.CanConsumeOne(null)); Assert.IsFalse(Inventory.TryConsumeOne(null));
        }

        [TestCase(1)] [TestCase(3)]
        public void ConsumingOne_UpdatesHandlingPenaltyWithoutRemovingUnrelatedPenalty(int count)
        {
            _actor.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Penalty = 7 };
            var item = Item("Starapple", count);
            item.AddPart(new HandlingPart { CarryMovePenalty = 4 }); Inventory.AddObject(item);
            int before = _actor.GetStat("Speed").Penalty;
            Assert.Greater(before, 7);
            Assert.IsTrue(InventorySystem.PerformAction(_actor, item, "Eat"));
            Assert.AreEqual(before - 4, _actor.GetStat("Speed").Penalty);
            Inventory.RefreshHandlingCarryPenalty();
            Assert.AreEqual(before - 4, _actor.GetStat("Speed").Penalty);
        }

        [TestCase("HealingTonic", 1)] [TestCase("HealingTonic", 3)]
        [TestCase("Starapple", 1)] [TestCase("Starapple", 3)]
        public void OuterRollback_RestoresQuantityAndPenaltyWithoutNextMutationDrift(string bp, int count)
        {
            _actor.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Penalty = 7 };
            var item = Item(bp, count);
            if (item.GetPart<HandlingPart>() == null) item.AddPart(new HandlingPart());
            item.GetPart<HandlingPart>().CarryMovePenalty = 4;
            Inventory.AddObject(item);
            int before = _actor.GetStat("Speed").Penalty;
            var tx = new InventoryTransaction();
            Assert.IsTrue(new PerformInventoryActionCommand(item, Command(bp))
                .Execute(new InventoryContext(_actor), tx).Success);
            Assert.AreEqual(before - 4, _actor.GetStat("Speed").Penalty);
            tx.Rollback();
            Assert.AreEqual(count, item.GetPart<StackerPart>().StackCount);
            Assert.AreEqual(before, _actor.GetStat("Speed").Penalty, "Rollback must restore the original penalty.");
            Assert.AreEqual(10, _actor.GetStatValue("Hitpoints"));
            Assert.AreSame(_actor, item.GetPart<PhysicsPart>().InInventory);
            Inventory.RefreshHandlingCarryPenalty();
            Assert.AreEqual(before, _actor.GetStat("Speed").Penalty, "A neutral refresh must not charge carry cost twice.");
        }

        [TestCase("HealingTonic", false)] [TestCase("HealingTonic", true)]
        [TestCase("Starapple", false)] [TestCase("Starapple", true)]
        [TestCase("InkVial", false)] [TestCase("InkVial", true)]
        public void RejectionDiagnostic_ExplainsOwnershipOrEmptyStack_WithoutMenuSpam(string bp, bool empty)
        {
            Diag.ResetAll();
            try
            {
                var item = Item(bp);
                if (empty) { Inventory.AddObject(item); item.GetPart<StackerPart>().StackCount = 0; }
                InventorySystem.GetActions(_actor, item);
                Assert.AreEqual(0, DiagQuery.Count(new DiagQuery.Filter { Kind = "ItemConsumptionRejected" }).Count);
                Assert.IsFalse(InventorySystem.PerformAction(_actor, item, Command(bp)));
                var records = DiagQuery.Apply(new DiagQuery.Filter { Kind = "ItemConsumptionRejected" }).Records;
                Assert.AreEqual(1, records.Count);
                Assert.AreEqual(_actor.ID, records[0].ActorId);
                Assert.AreEqual(item.ID, records[0].TargetId);
                StringAssert.Contains(empty ? "empty_stack" : "not_carried", records[0].PayloadJson);
            }
            finally { Diag.ResetAll(); }
        }

        [TestCase(1)] [TestCase(3)]
        public void SavedHandlingStack_ConsumptionAndRefreshKeepUnrelatedPenalty(int count)
        {
            _actor.Statistics["Speed"] = new Stat { Name = "Speed", BaseValue = 100, Penalty = 7 };
            var item = Item("Starapple", count); item.AddPart(new HandlingPart { CarryMovePenalty = 4 });
            Inventory.AddObject(item);
            _actor = PartRoundTripHelper.RoundTripEntityViaTokenGraph(_actor);
            item = Inventory.Objects[0];
            Assert.AreEqual(7 + count * 4, _actor.GetStat("Speed").Penalty);
            Assert.IsTrue(InventorySystem.PerformAction(_actor, item, "Eat"));
            Assert.AreEqual(7 + (count - 1) * 4, _actor.GetStat("Speed").Penalty);
            Inventory.RefreshHandlingCarryPenalty();
            Assert.AreEqual(7 + (count - 1) * 4, _actor.GetStat("Speed").Penalty);
        }

        [Test]
        public void PublicTonic_DifferentPayerAndRecipient_ChargesOnlyPayer()
        {
            var item = Item("HealingTonic", 2); Inventory.AddObject(item);
            var recipient = new Entity();
            recipient.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", BaseValue = 1, Max = 100 };
            Assert.IsTrue(item.GetPart<TonicPart>().ApplyTo(recipient, _actor, consumeItem: true));
            Assert.Greater(recipient.GetStatValue("Hitpoints"), 1);
            Assert.AreEqual(10, _actor.GetStatValue("Hitpoints"));
            Assert.AreEqual(1, item.GetPart<StackerPart>().StackCount);
            Assert.AreSame(_actor, item.GetPart<PhysicsPart>().InInventory);
        }

        [TestCase("known")] [TestCase("unknown")] [TestCase("no_locker")]
        public void SingleUseSchematic_UnsuccessfulStudyDoesNotSpend(string reason)
        {
            TinkerRecipeRegistry.ResetForTests();
            try
            {
                TinkerRecipeRegistry.InitializeFromJson("{\"Recipes\":[{\"ID\":\"audit_recipe\",\"DisplayName\":\"Audit\",\"Blueprint\":\"Dagger\",\"Type\":\"Build\",\"Cost\":\"A\",\"NumberMade\":1}]}");
                if (reason != "no_locker") _actor.AddPart(new BitLockerPart());
                if (reason == "known") _actor.GetPart<BitLockerPart>().LearnRecipe("audit_recipe");
                var item = new Entity(); item.AddPart(new PhysicsPart()); item.AddPart(new StackerPart { StackCount = 3 });
                item.AddPart(new SchematicPart { RecipeID = reason == "unknown" ? "missing" : "audit_recipe", ConsumeOnStudy = true });
                Inventory.AddObject(item);
                Assert.IsTrue(InventorySystem.PerformAction(_actor, item, "StudySchematic"), "The explanatory study response is handled.");
                Assert.AreEqual(3, item.GetPart<StackerPart>().StackCount);
                Assert.IsTrue(Inventory.Objects.Contains(item));
            }
            finally { TinkerRecipeRegistry.ResetForTests(); }
        }

        [Test]
        public void MissingInventoryDiagnostic_ReportsRefusalFromPublicConsumingTonic()
        {
            Diag.ResetAll();
            try
            {
                var item = Item("HealingTonic");
                Assert.IsFalse(item.GetPart<TonicPart>().ApplyTo(_actor, new Entity(), consumeItem: true));
                var records = DiagQuery.Apply(new DiagQuery.Filter { Kind = "ItemConsumptionRejected" }).Records;
                Assert.AreEqual(1, records.Count); StringAssert.Contains("missing_inventory", records[0].PayloadJson);
                NoBenefit();
            }
            finally { Diag.ResetAll(); }
        }

        [TestCase(1)] [TestCase(3)]
        public void PaymentDiagnostic_ReportsSpentQuantityAndActualPayloadSuccess(int count)
        {
            Diag.ResetAll();
            try
            {
                var item = Item("Starapple", count); Inventory.AddObject(item);
                Assert.IsTrue(InventorySystem.PerformAction(_actor, item, "Eat"));
                var records = DiagQuery.Apply(new DiagQuery.Filter { Kind = "ItemUnitConsumed" }).Records;
                Assert.AreEqual(1, records.Count);
                StringAssert.Contains("\"quantityBefore\":" + count, records[0].PayloadJson);
                StringAssert.Contains("\"quantityAfter\":" + (count - 1), records[0].PayloadJson);
                Assert.AreEqual(1, DiagQuery.Count(new DiagQuery.Filter { Kind = "FoodEaten", Actor = _actor.ID }).Count);
                Assert.AreEqual(0, DiagQuery.Count(new DiagQuery.Filter { Kind = "ItemConsumptionRejected" }).Count);
            }
            finally { Diag.ResetAll(); }
        }

        [Test]
        public void BeforeActionRemoval_RefusesPaymentAndRestoresOriginalItemOnRollback()
        {
            var item = Item("Starapple"); Inventory.AddObject(item);
            _actor.AddPart(new RemoveBeforePart());
            Assert.IsFalse(InventorySystem.PerformAction(_actor, item, "Eat"));
            NoBenefit(); Assert.IsTrue(Inventory.Objects.Contains(item));
            Assert.AreSame(_actor, item.GetPart<PhysicsPart>().InInventory);
        }

        private sealed class RemoveBeforePart : Part
        {
            public override string Name => "AuditRemoveBefore";
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "BeforeInventoryAction")
                    ParentEntity.GetPart<InventoryPart>().RemoveObject(e.GetParameter<Entity>("Item"));
                return true;
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void EffectImmunity_DoesNotRefundAConsumedTonic(bool immune)
        {
            var item = Item("PoisonTonic"); Inventory.AddObject(item);
            if (immune) _actor.AddPart(new ImmunityPart());
            Assert.IsTrue(InventorySystem.PerformAction(_actor, item, "ApplyTonic"));
            Assert.AreEqual(!immune, _actor.HasEffect<PoisonedEffect>());
            Assert.IsFalse(Inventory.Objects.Contains(item));
        }

        [Test]
        public void EquippedOnlyReference_DoesNotEstablishCarriedOwnership()
        {
            var item = Item("HealingTonic");
            Inventory.EquippedItems["Hand"] = item; item.GetPart<PhysicsPart>().Equipped = _actor;
            Assert.IsTrue(Inventory.Contains(item));
            Assert.IsFalse(Inventory.CanConsumeOne(item));
            Assert.IsFalse(InventorySystem.PerformAction(_actor, item, "ApplyTonic"));
            Assert.AreSame(item, Inventory.EquippedItems["Hand"]); NoBenefit();
        }

        private sealed class ImmunityPart : Part
        {
            public override string Name => "AuditImmunity";
            public override bool HandleEvent(GameEvent e) => e.ID != "BeforeApplyEffect";
        }

        private sealed class VetoPart : Part
        {
            public override string Name => "AuditVeto";
            public override bool HandleEvent(GameEvent e) => e.ID != "BeforeInventoryAction";
        }
        private sealed class ReenterPart : Part
        {
            public Entity Actor;
            public bool NestedSucceeded;
            private bool _attempted;
            public override string Name => "AuditReenter";
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "ApplyTonic" && !_attempted)
                {
                    _attempted = true;
                    NestedSucceeded = InventorySystem.PerformAction(Actor, ParentEntity, "ApplyTonic");
                }
                return true;
            }
        }
    }
}
