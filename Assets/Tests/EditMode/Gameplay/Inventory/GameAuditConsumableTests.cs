using System.IO;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>Audit A02: actual content cannot grant repeat benefits from an unconsumed ground unit.</summary>
    public class GameAuditConsumableTests
    {
        private EntityFactory _factory;
        private Entity _actor;
        private Zone _zone;

        [OneTimeSetUp]
        public void LoadContent()
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
            _actor.Tags["Player"] = "";
            _actor.AddPart(new InventoryPart());
            _actor.AddPart(new PhysicsPart());
            _actor.Statistics["Hitpoints"] = new Stat { Name = "Hitpoints", Owner = _actor, BaseValue = 10, Min = 0, Max = 100 };
            _actor.Statistics["Strength"] = new Stat { Name = "Strength", Owner = _actor, BaseValue = 16, Min = 0, Max = 100 };
            _zone = new Zone("AuditConsumerZone");
            Assert.IsTrue(_zone.AddEntity(_actor, 10, 10));
        }

        [TestCase("HealingTonic", "ApplyTonic")]
        [TestCase("StrengthTonic", "ApplyTonic")]
        [TestCase("PoisonTonic", "ApplyTonic")]
        [TestCase("Starapple", "Eat")]
        [TestCase("InkVial", "UseInkVial")]
        public void GroundItem_DoesNotOfferConsumingAction_ButPickupEnablesIt(string blueprint, string command)
        {
            Entity item = _factory.CreateEntity(blueprint);
            Assert.IsTrue(_zone.AddEntity(item, 10, 10));
            Assert.IsFalse(WorldInteractionSystem.GatherActions(item, _actor).Exists(a => a.Command == command));
            Assert.IsTrue(InventorySystem.Pickup(_actor, item, _zone));
            Assert.IsTrue(InventorySystem.GetActions(_actor, item).Exists(a => a.Command == command));
        }

        [TestCase("HealingTonic", "ApplyTonic")]
        [TestCase("StrengthTonic", "ApplyTonic")]
        [TestCase("PoisonTonic", "ApplyTonic")]
        [TestCase("Starapple", "Eat")]
        [TestCase("InkVial", "UseInkVial")]
        public void GroundItem_RepeatedDirectDispatchRefusesWithoutBenefit(string blueprint, string command)
        {
            Entity item = _factory.CreateEntity(blueprint);
            Assert.IsTrue(_zone.AddEntity(item, 10, 10));
            for (int i = 0; i < 2; i++)
            {
                Assert.IsFalse(InventorySystem.PerformAction(_actor, item, command, _zone));
                Assert.AreEqual(10, _actor.GetStatValue("Hitpoints"));
                Assert.AreEqual(16, _actor.GetStatValue("Strength"));
                Assert.AreEqual(0, RentalSystem.GetInk(_actor));
                Assert.IsFalse(_actor.HasEffect<PoisonedEffect>());
                Assert.AreSame(_zone.GetCell(10, 10), _zone.GetEntityCell(item));
                Assert.AreEqual(1, item.GetPart<StackerPart>().StackCount);
            }
        }

        [TestCase("HealingTonic", "ApplyTonic", 1)]
        [TestCase("HealingTonic", "ApplyTonic", 3)]
        [TestCase("Starapple", "Eat", 1)]
        [TestCase("Starapple", "Eat", 3)]
        [TestCase("InkVial", "UseInkVial", 1)]
        [TestCase("InkVial", "UseInkVial", 3)]
        public void CarriedUse_SpendsExactlyOneBeforeBenefit(string blueprint, string command, int count)
        {
            Entity item = _factory.CreateEntity(blueprint);
            item.GetPart<StackerPart>().StackCount = count;
            Assert.IsTrue(_actor.GetPart<InventoryPart>().AddObject(item));
            item.AddPart(new ConsumeBeforeBenefitProbe(_actor, count));
            Assert.IsTrue(InventorySystem.PerformAction(_actor, item, command, _zone));
            Assert.AreEqual(count > 1, _actor.GetPart<InventoryPart>().Objects.Contains(item));
            Assert.AreEqual(count > 1 ? count - 1 : 1, item.GetPart<StackerPart>().StackCount);
            if (blueprint == "InkVial") Assert.AreEqual(25, RentalSystem.GetInk(_actor));
            else Assert.Greater(_actor.GetStatValue("Hitpoints"), 10);
            if (count == 1)
            {
                int hp = _actor.GetStatValue("Hitpoints");
                int ink = RentalSystem.GetInk(_actor);
                Assert.IsFalse(InventorySystem.PerformAction(_actor, item, command, _zone));
                Assert.AreEqual(hp, _actor.GetStatValue("Hitpoints"));
                Assert.AreEqual(ink, RentalSystem.GetInk(_actor));
            }
        }

        [TestCase(false)] [TestCase(true)]
        public void PublicConsumingTonic_RejectsUncarriedOrMissingUser(bool missingUser)
        {
            Entity item = _factory.CreateEntity("StrengthTonic");
            Assert.IsFalse(item.GetPart<TonicPart>().ApplyTo(_actor, missingUser ? null : _actor,
                _zone, consumeItem: true));
            Assert.AreEqual(16, _actor.GetStatValue("Strength"));
        }

        [Test]
        public void NonconsumingTonicPayload_StillAppliesWithoutInventoryOwnership()
        {
            Entity item = _factory.CreateEntity("PoisonTonic");
            Assert.IsTrue(item.GetPart<TonicPart>().ApplyTo(_actor, null, _zone, consumeItem: false));
            Assert.IsTrue(_actor.HasEffect<PoisonedEffect>());
            Assert.AreEqual(1, item.GetPart<StackerPart>().StackCount);
        }

        [TestCase(true, false)] [TestCase(true, true)] [TestCase(false, false)]
        public void Schematic_RequiresCarriageOnlyWhenStudyConsumes(bool consumes, bool carried)
        {
            TinkerRecipeRegistry.ResetForTests();
            try
            {
                TinkerRecipeRegistry.InitializeFromJson("{\"Recipes\":[{\"ID\":\"audit_recipe\",\"DisplayName\":\"Audit recipe\",\"Blueprint\":\"Dagger\",\"Type\":\"Build\",\"Cost\":\"A\",\"NumberMade\":1}]}");
                _actor.AddPart(new BitLockerPart());
                var item = new Entity { BlueprintName = "AuditSchematic" };
                item.AddPart(new PhysicsPart { Takeable = true });
                item.AddPart(new SchematicPart { RecipeID = "audit_recipe", ConsumeOnStudy = consumes });
                if (carried) _actor.GetPart<InventoryPart>().AddObject(item);
                else _zone.AddEntity(item, 10, 10);
                bool allowed = !consumes || carried;
                Assert.AreEqual(allowed, WorldInteractionSystem.GatherActions(item, _actor)
                    .Exists(a => a.Command == "StudySchematic"));
                Assert.AreEqual(allowed, InventorySystem.PerformAction(_actor, item, "StudySchematic", _zone));
                Assert.AreEqual(allowed, _actor.GetPart<BitLockerPart>().KnowsRecipe("audit_recipe"));
                Assert.IsFalse(_actor.GetPart<InventoryPart>().Contains(item));
                if (!carried) Assert.NotNull(_zone.GetEntityCell(item));
            }
            finally { TinkerRecipeRegistry.ResetForTests(); }
        }

        private sealed class ConsumeBeforeBenefitProbe : Part
        {
            private readonly Entity _consumer;
            private readonly int _count;
            public ConsumeBeforeBenefitProbe(Entity consumer, int count) { _consumer = consumer; _count = count; }
            public override string Name => "AuditConsumeBeforeBenefit";
            public override bool HandleEvent(GameEvent e)
            {
                if (e.ID == "ApplyTonic")
                {
                    Assert.AreEqual(_count > 1, _consumer.GetPart<InventoryPart>().Objects.Contains(ParentEntity),
                        "Tonic callbacks must not be able to reuse an unpaid last unit.");
                    Assert.AreEqual(_count > 1 ? _count - 1 : 1, ParentEntity.GetPart<StackerPart>().StackCount);
                }
                return true;
            }
        }
    }
}
