using CavesOfOoo.Diagnostics;
using CavesOfOoo.Core;
using CavesOfOoo.Skills;
using CavesOfOoo.Rendering;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    public class ExplorationDepthProgressionAdversarialTests
    {
        [SetUp] public void Setup() => SkillRegistry.ResetForTests();
        [TearDown] public void Cleanup() => SkillRegistry.ResetForTests();
        private static Entity Actor(int points)
        {
            var actor = new Entity { ID = "purchase-adversary" };
            actor.Statistics["SP"] = new Stat { Owner = actor, Name = "SP", BaseValue = points, Max = 999 };
            actor.AddPart(new SkillsPart()); actor.AddPart(new ActivatedAbilitiesPart()); return actor;
        }
        [Test]
        public void NegativeCostIsNeverShownAsBuyableOrAllowedToMintPoints()
        {
            SkillRegistry.InitializeFromJson(@"{""Skills"":[{""Name"":""Acrobatics"",""Class"":""AcrobaticsSkill"",""Cost"":-1}]}");
            var actor = Actor(10);
            Assert.AreEqual(SkillsScreenRowState.RequirementsNotMet, ExplorationDepthProgressionTests.Row(actor, "AcrobaticsSkill").State);
            Assert.AreEqual(BuySkillAction.FailureReason.NotPurchasable, BuySkillAction.Execute(actor, "AcrobaticsSkill").Reason);
            Assert.AreEqual(10, actor.GetStatValue("SP"));
            Assert.IsFalse(actor.GetPart<SkillsPart>().HasSkill("AcrobaticsSkill"));
            SkillRegistry.InitializeFromJson(@"{""Skills"":[{""Name"":""Acrobatics"",""Class"":""AcrobaticsSkill"",""Cost"":1}]}");
            Assert.AreEqual(SkillsScreenRowState.Buyable, ExplorationDepthProgressionTests.Row(actor, "AcrobaticsSkill").State);
            Assert.IsTrue(BuySkillAction.Execute(actor, "AcrobaticsSkill").Succeeded);
        }
        [Test]
        public void DisplayNameCollisionCannotSubstituteAnotherTree()
        {
            SkillRegistry.InitializeFromJson(@"{""Skills"":[
              {""Name"":""Same name"",""Class"":""AcrobaticsSkill"",""Cost"":1,""Powers"":[{""Name"":""Vault"",""Class"":""Acrobatics_Vault"",""Cost"":1}]},
              {""Name"":""Same name"",""Class"":""CudgelSkill"",""Cost"":1}]}");
            var actor = Actor(10); actor.GetPart<SkillsPart>().AddSkill("CudgelSkill");
            Assert.AreEqual(BuySkillAction.FailureReason.MissingPrereq, BuySkillAction.Execute(actor, "Acrobatics_Vault").Reason);
            Assert.AreEqual(10, actor.GetStatValue("SP"));
            Assert.AreEqual(SkillsScreenRowState.RequirementsNotMet, ExplorationDepthProgressionTests.Row(actor, "Acrobatics_Vault").State);
            actor.GetPart<SkillsPart>().AddSkill("AcrobaticsSkill");
            Assert.IsTrue(BuySkillAction.Execute(actor, "Acrobatics_Vault").Succeeded);
            Assert.AreEqual(9, actor.GetStatValue("SP"));
        }
        [Test]
        public void MissingPointStatCannotProduceBuyableZeroCostRow()
        {
            SkillRegistry.InitializeFromJson(@"{""Skills"":[{""Name"":""Acrobatics"",""Class"":""AcrobaticsSkill"",""Cost"":0}]}");
            var actor = Actor(0); actor.Statistics.Remove("SP");
            Assert.AreEqual(SkillsScreenRowState.RequirementsNotMet, ExplorationDepthProgressionTests.Row(actor, "AcrobaticsSkill").State);
            Assert.AreEqual(BuySkillAction.FailureReason.ActorMissingSPStat, BuySkillAction.Execute(actor, "AcrobaticsSkill").Reason);
            actor.Statistics["SP"] = new Stat { Owner = actor, Name = "SP", BaseValue = 0, Max = 999 };
            Assert.IsTrue(BuySkillAction.Execute(actor, "AcrobaticsSkill").Succeeded);
        }

        [Test]
        public void ScreenAndEligibilityDoNotSpendOrEmitPurchaseAttempts()
        {
            SkillRegistry.InitializeFromJson(@"{""Skills"":[{""Name"":""Acrobatics"",""Class"":""AcrobaticsSkill"",""Cost"":1}]}");
            var actor = Actor(1);
            Diag.ResetAll();
            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(SkillPurchaseEligibility.Evaluate(actor, "AcrobaticsSkill").Succeeded);
                Assert.AreEqual(SkillsScreenRowState.Buyable, ExplorationDepthProgressionTests.Row(actor, "AcrobaticsSkill").State);
            }
            Assert.AreEqual(1, actor.GetStatValue("SP"));
            Assert.IsFalse(actor.GetPart<SkillsPart>().HasSkill("AcrobaticsSkill"));
            Assert.AreEqual(0, DiagQuery.Apply(new DiagQuery.Filter { Category = "skill", Kind = "PurchaseAttempted", Limit = 20 }).Records.Count);
            Assert.IsTrue(BuySkillAction.Execute(actor, "AcrobaticsSkill").Succeeded);
            Assert.AreEqual(1, DiagQuery.Apply(new DiagQuery.Filter { Category = "skill", Kind = "PurchaseAttempted", Limit = 20 }).Records.Count);
            Diag.ResetAll();
        }

        [Test]
        public void TemporaryPointBoostCannotMakeTheScreenOfferUnaffordablePower()
        {
            SkillRegistry.InitializeFromJson(@"{""Skills"":[{""Name"":""Acrobatics"",""Class"":""AcrobaticsSkill"",""Cost"":1}]}");
            var actor = Actor(0); actor.GetStat("SP").Boost = 20;
            Assert.AreEqual(0, SkillsScreenStateBuilder.Build(actor).CurrentSP);
            Assert.AreEqual(SkillsScreenRowState.InsufficientSP, ExplorationDepthProgressionTests.Row(actor, "AcrobaticsSkill").State);
            Assert.AreEqual(BuySkillAction.FailureReason.InsufficientSP, BuySkillAction.Execute(actor, "AcrobaticsSkill").Reason);
            Assert.AreEqual(0, actor.GetStat("SP").BaseValue);
            actor.GetStat("SP").BaseValue = 1;
            Assert.IsTrue(BuySkillAction.Execute(actor, "AcrobaticsSkill").Succeeded);
            Assert.AreEqual(0, actor.GetStat("SP").BaseValue);
        }

        [Test]
        public void MissingParentPrecedesFundsForBothScreenAndCommand()
        {
            SkillRegistry.InitializeFromJson(@"{""Skills"":[{""Name"":""Acrobatics"",""Class"":""AcrobaticsSkill"",""Cost"":1,
              ""Powers"":[{""Name"":""Vault"",""Class"":""Acrobatics_Vault"",""Cost"":1}]}]}");
            var actor = Actor(0);
            Assert.AreEqual(BuySkillAction.FailureReason.MissingPrereq, BuySkillAction.Execute(actor, "Acrobatics_Vault").Reason);
            Assert.AreEqual(SkillsScreenRowState.RequirementsNotMet, ExplorationDepthProgressionTests.Row(actor, "Acrobatics_Vault").State);
            actor.GetPart<SkillsPart>().AddSkill("AcrobaticsSkill");
            Assert.AreEqual(BuySkillAction.FailureReason.InsufficientSP, BuySkillAction.Execute(actor, "Acrobatics_Vault").Reason);
            Assert.AreEqual(SkillsScreenRowState.InsufficientSP, ExplorationDepthProgressionTests.Row(actor, "Acrobatics_Vault").State);
            Assert.AreEqual(0, actor.GetStatValue("SP"));
        }

        [Test]
        public void MissingParentIdentityRefusesEvenWhenDisplayNameAndOwnedTreeMatch()
        {
            SkillRegistry.InitializeFromJson(@"{""Skills"":[{""Name"":""Acrobatics"",""Class"":""AcrobaticsSkill"",""Cost"":1,
              ""Powers"":[{""Name"":""Vault"",""Class"":""Acrobatics_Vault"",""Cost"":1}]}]}");
            var actor = Actor(1); actor.GetPart<SkillsPart>().AddSkill("AcrobaticsSkill");
            Assert.IsTrue(SkillRegistry.TryGetPowerByClass("Acrobatics_Vault", out var power));
            power.ParentSkillClass = "";
            Assert.IsFalse(BuySkillAction.Execute(actor, "Acrobatics_Vault").Succeeded);
            Assert.AreEqual(1, actor.GetStatValue("SP"));
            power.ParentSkillClass = "AcrobaticsSkill";
            Assert.IsTrue(BuySkillAction.Execute(actor, "Acrobatics_Vault").Succeeded);
        }

        [Test]
        public void LearnedPowerIsOwnedEvenAfterItsPriceBecomesUnpurchasable()
        {
            SkillRegistry.InitializeFromJson(@"{""Skills"":[{""Name"":""Acrobatics"",""Class"":""AcrobaticsSkill"",""Cost"":1,
              ""Powers"":[{""Name"":""Vault"",""Class"":""Acrobatics_Vault"",""Cost"":-1}]}]}");
            var actor = Actor(0);
            Assert.AreEqual(BuySkillAction.FailureReason.NotPurchasable, BuySkillAction.Execute(actor, "Acrobatics_Vault").Reason);
            actor.GetPart<SkillsPart>().AddSkill("Acrobatics_Vault");
            Assert.AreEqual(SkillsScreenRowState.Owned, ExplorationDepthProgressionTests.Row(actor, "Acrobatics_Vault").State);
            Assert.AreEqual(BuySkillAction.FailureReason.AlreadyOwned, BuySkillAction.Execute(actor, "Acrobatics_Vault").Reason);
            Assert.IsFalse(actor.GetPart<SkillsPart>().HasSkill("AcrobaticsSkill"));
            Assert.AreEqual(0, actor.GetStatValue("SP"));
        }

        [Test]
        public void MissingParentMessageUsesReadableTreeName()
        {
            SkillRegistry.InitializeFromJson(@"{""Skills"":[{""Name"":""Acrobatics"",""Class"":""AcrobaticsSkill"",""Cost"":1}]}");
            var format = typeof(SkillsScreenUI).GetMethod("FormatFailure", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(format);
            var missing = new BuySkillAction.Result { Reason = BuySkillAction.FailureReason.MissingPrereq, Detail = "AcrobaticsSkill" };
            Assert.AreEqual("You must learn Acrobatics first.", format.Invoke(null, new object[] { "Vault", missing }));
            missing.Detail = "";
            Assert.AreEqual("You must learn the parent skill tree first.", format.Invoke(null, new object[] { "Vault", missing }));
        }
    }
}
