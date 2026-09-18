using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    public sealed class MorrowfastContentTests
    {
        private EntityFactory factory;
        private StoryletPart previousStorylets;
        private Entity previousPlayer;
        private Zone previousZone;

        [SetUp] public void SetUp()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/Content/Blueprints/Objects.json")));
            previousStorylets = StoryletPart.Current;
            previousPlayer = StoryletPart.LocalPlayer;
            previousZone = SettlementRuntime.ActiveZone;
        }

        [TearDown] public void TearDown()
        {
            ConversationManager.EndConversation();
            StoryletPart.Current = previousStorylets;
            StoryletPart.LocalPlayer = previousPlayer;
            SettlementRuntime.ActiveZone = previousZone;
        }

        [Test] public void EightNamedResidentsAreDistinctNativeLivingActors()
        {
            var people = MorrowfastContent.ResidentIds.Select(id => MorrowfastContent.CreateResident(id, factory)).ToArray();
            Assert.AreEqual(8, people.Length);
            Assert.AreEqual(8, people.Select(e => e.BlueprintName).Distinct().Count());
            Assert.AreEqual(8, people.Select(e => e.GetDisplayName()).Distinct().Count());
            foreach (var person in people)
            {
                Assert.IsTrue(person.HasTag("Creature"));
                Assert.Greater(person.GetStatValue("Hitpoints"), 1);
                Assert.NotNull(person.GetPart<Body>());
                Assert.NotNull(person.GetPart<BrainPart>());
                Assert.NotNull(person.GetPart<InventoryPart>());
                Assert.NotNull(person.GetPart<ConversationPart>());
                Assert.NotNull(person.GetPart<MorrowfastResidentPart>());
                Assert.AreEqual("Stillcord", person.GetTag("Faction"));
                Assert.IsTrue(factory.Blueprints.ContainsKey(person.BlueprintName));
            }
        }

        [Test] public void UnknownOrMissingFactoryDoesNotInventAResident()
        {
            Assert.IsNull(MorrowfastContent.CreateResident("central-cistern", factory));
            Assert.IsNull(MorrowfastContent.CreateResident("north-guard-west", null));
            Assert.IsNull(MorrowfastContent.CreateResident(null, factory));
        }

        [Test] public void AdditionalCastHaveStableDistinctIdentityAndOrdinarySpriteFallbacks()
        {
            var additions = MorrowfastContent.AdditionalResidents;
            CollectionAssert.AreEquivalent(new[] { "farra-sprig", "edden-brack" }, additions.Select(x => x.Id));
            foreach (var spec in additions)
            {
                Assert.IsTrue(factory.Blueprints.ContainsKey(spec.SpriteBlueprint));
                Assert.That(spec.X, Is.InRange(0, Zone.Width - 1));
                Assert.That(spec.Y, Is.InRange(0, Zone.Height - 1));
                Assert.NotNull(MorrowfastContent.CreateResident(spec.Id, factory));
            }
        }

        [Test] public void GuardsStayAtTheirPostsAndDoNotTreatOrdinaryVisitorsAsPrey()
        {
            foreach (var id in new[] { "north-guard-west", "north-guard-east" })
            {
                var guard = MorrowfastContent.CreateResident(id, factory);
                Assert.IsFalse(guard.GetPart<BrainPart>().WandersRandomly);
                Assert.IsFalse(guard.GetPart<BrainPart>().Wanders);
                Assert.GreaterOrEqual(guard.GetStatValue("Hitpoints"), 20);
                Assert.IsTrue(guard.GetPart<BrainPart>().Passive);
            }
        }

        [Test] public void FoodAndRepairMerchantsHaveFiniteRealStockWithNormalCommerce()
        {
            var food = MorrowfastContent.CreateResident("southern-food-vendor", factory);
            var mender = MorrowfastContent.CreateResident("southwest-craftsperson", factory);
            foreach (var person in new[] { food, mender })
            {
                Assert.Greater(person.GetPart<InventoryPart>().Objects.Count, 0);
                Assert.IsTrue(person.GetPart<InventoryPart>().Objects.All(e => e.GetPart<CommercePart>() != null));
                Assert.IsTrue(person.GetPart<InventoryPart>().Objects.All(e => e.GetPart<PhysicsPart>().InInventory == person));
            }
            Assert.IsTrue(food.GetPart<InventoryPart>().Objects.Any(e => e.BlueprintName == "Mushroom"));
            Assert.IsTrue(mender.GetPart<InventoryPart>().Objects.Any(e => e.BlueprintName == "FireClay"));
        }

        [Test] public void NormalPurchaseTransfersGoodsAndMoneyAndRegistrationDoesNotRefillStock()
        {
            var seller = MorrowfastContent.CreateResident("southern-food-vendor", factory);
            var buyer = factory.CreateEntity("Player");
            TradeSystem.SetDrams(buyer, 1000);
            var goods = seller.GetPart<InventoryPart>().Objects.First(e => e.BlueprintName == "Mushroom");
            int beforeUnits = Count(seller, "Mushroom"), beforeBuyerUnits = Count(buyer, "Mushroom"), beforeMoney = TradeSystem.GetDrams(buyer);
            int boughtUnits = goods.GetPart<StackerPart>()?.StackCount ?? 1;
            int price = TradeSystem.GetBuyPrice(goods, TradeSystem.GetTradePerformance(buyer), seller);
            int sellerMoney = TradeSystem.GetDrams(seller);
            Assert.IsTrue(TradeSystem.BuyFromTrader(buyer, seller, goods));
            Assert.AreEqual(beforeUnits - boughtUnits, Count(seller, "Mushroom"));
            Assert.AreEqual(beforeBuyerUnits + boughtUnits, Count(buyer, "Mushroom"));
            Assert.AreEqual(beforeMoney - price, TradeSystem.GetDrams(buyer));
            Assert.AreEqual(sellerMoney + price, TradeSystem.GetDrams(seller));
            MorrowfastContent.EnsureRegistered();
            Assert.AreEqual(beforeUnits - boughtUnits, Count(seller, "Mushroom"));
        }

        [Test] public void AllResidentDialogueChoicesHaveRegisteredActionsPredicatesAndRealTargets()
        {
            MorrowfastContent.EnsureRegistered();
            foreach (var id in MorrowfastContent.ResidentIds)
            {
                var resident = MorrowfastContent.CreateResident(id, factory);
                var dialogue = ConversationLoader.Get(resident.GetPart<ConversationPart>().ConversationID);
                Assert.NotNull(dialogue, id);
                Assert.Greater(dialogue.Nodes.Count, 1, id);
                foreach (var node in dialogue.Nodes)
                    foreach (var choice in node.Choices)
                    {
                        Assert.IsTrue(string.IsNullOrEmpty(choice.Target) || choice.Target == "End" || dialogue.GetNode(choice.Target) != null, id + ":" + choice.Target);
                        foreach (var action in choice.Actions ?? new List<ConversationParam>())
                            Assert.IsTrue(ConversationActions.IsRegistered(action.Key), action.Key);
                        foreach (var predicate in choice.Predicates ?? new List<ConversationParam>())
                            Assert.IsTrue(ConversationPredicates.IsRegistered(predicate.Key), predicate.Key);
                    }
            }
        }

        [Test] public void ThreeOptionalQuestsAreNativeJournalDefinitions()
        {
            MorrowfastContent.EnsureRegistered();
            foreach (var id in new[] { MorrowfastQuests.ReturnQuestId, MorrowfastQuests.BellQuestId, MorrowfastQuests.SupperQuestId })
            {
                var quest = StoryletRegistry.FindQuest(id);
                Assert.NotNull(quest, id);
                Assert.IsFalse(string.IsNullOrWhiteSpace(quest.Name));
                Assert.Greater(quest.Stages.Count, 0);
            }
        }

        private (Zone zone, Entity player, Entity speaker) Session(string resident)
        {
            var zone = new OverworldZoneManager(factory, 64).GetZone(MorrowfastSceneRuntime.ZoneID);
            var player = factory.CreateEntity("Player");
            var speaker = MorrowfastSceneRuntime.FindOwner(zone, resident);
            zone.AddEntity(player, 10, 9); zone.MoveEntity(speaker, 10, 10);
            speaker.GetPart<BrainPart>().CurrentZone = zone;
            StoryletPart.Current = new StoryletPart(); StoryletPart.LocalPlayer = player;
            SettlementRuntime.ActiveZone = zone;
            return (zone, player, speaker);
        }

        [Test] public void GuardBriefingIsFreeAndVoluntaryWithoutAQuestGate()
        {
            var s = Session("north-guard-east");
            TradeSystem.SetDrams(s.player, 0);
            Assert.IsTrue(MorrowfastQuests.TryConversation(s.speaker, s.player, "decline-register"));
            Assert.AreEqual(0, TradeSystem.GetDrams(s.player));
            Assert.AreEqual(0, StoryletPart.Current.GetActiveQuests().Count);
            Assert.IsFalse(s.speaker.GetPart<BrainPart>().IsPersonallyHostileTo(s.player));
        }

        [Test] public void ReturnQuestCannotBeReportedWithoutActualConsentingEddenConversation()
        {
            var s = Session("north-guard-east");
            Assert.IsTrue(MorrowfastQuests.TryConversation(s.speaker, s.player, "accept-return"));
            Assert.IsFalse(MorrowfastQuests.TryConversation(s.speaker, s.player, "report-return"));
            Assert.IsTrue(StoryletPart.Current.IsQuestActive(MorrowfastQuests.ReturnQuestId));
            var edden = MorrowfastSceneRuntime.FindOwner(s.zone, "edden-brack");
            s.zone.MoveEntity(edden, 11, 9);
            Assert.IsTrue(MorrowfastQuests.TryConversation(edden, s.player, "eddens-account"));
            Assert.IsTrue(MorrowfastQuests.TryConversation(s.speaker, s.player, "report-return"));
            Assert.IsTrue(StoryletPart.Current.IsQuestCompleted(MorrowfastQuests.ReturnQuestId));
            Assert.IsFalse(MorrowfastQuests.TryConversation(s.speaker, s.player, "report-return"), "Reward and resolution must be one-time.");
        }

        [Test] public void RefusalClosesAnAcceptedPromiseWithoutPretendingItWasPerformed()
        {
            var s = Session("north-guard-east");
            Assert.IsTrue(MorrowfastQuests.TryConversation(s.speaker, s.player, "accept-return"));
            Assert.IsTrue(MorrowfastQuests.TryConversation(s.speaker, s.player, "refuse-return"));
            Assert.IsFalse(StoryletPart.Current.IsQuestActive(MorrowfastQuests.ReturnQuestId));
            Assert.IsFalse(StoryletPart.Current.IsQuestCompleted(MorrowfastQuests.ReturnQuestId));
            Assert.IsFalse(MorrowfastQuests.TryConversation(s.speaker, s.player, "report-return"));
        }

        [Test] public void RemoteDetachedDeadOrWrongResidentCallsCannotGrantQuestProgress()
        {
            var s = Session("north-guard-east");
            Assert.IsFalse(MorrowfastQuests.TryConversation(s.speaker, s.player, "eddens-account"));
            s.zone.RemoveEntity(s.player); s.zone.AddEntity(s.player, 30, 20);
            Assert.IsFalse(MorrowfastQuests.TryConversation(s.speaker, s.player, "accept-return"));
            s.zone.RemoveEntity(s.player); s.zone.AddEntity(s.player, 10, 9);
            s.player.SetStatValue("Hitpoints", 0);
            Assert.IsFalse(MorrowfastQuests.TryConversation(s.speaker, s.player, "accept-return"));
            Assert.AreEqual(0, StoryletPart.Current.GetActiveQuests().Count);
        }

        [Test] public void BellWorkRejectsAbsentQuestAndDoesNotChargeOrMutate()
        {
            var s = Session("north-guard-west");
            var arch = factory.CreateEntity("PhysicalObject"); arch.ID = "morrowfast-owner:north-oath-arch";
            MorrowfastQuests.AttachProp(arch, "north-oath-arch"); s.zone.AddEntity(arch, 9, 9);
            Assert.IsFalse(MorrowfastQuests.TryWorldAction(arch, s.player, s.zone, MorrowfastQuests.RepairBellCommand, out int energy));
            Assert.AreEqual(0, energy);
            Assert.IsNull(arch.GetProperty("MorrowfastBellMode"));
        }

        [Test] public void ReleasingAPrivacyPromiseRevokesCarriedConsentBeforeItCanBeAcceptedAgain()
        {
            var s = Session("north-guard-east");
            Assert.IsTrue(MorrowfastQuests.TryConversation(s.speaker, s.player, "accept-return"));
            var edden = MorrowfastSceneRuntime.FindOwner(s.zone, "edden-brack");
            s.zone.MoveEntity(edden, 11, 9);
            Assert.IsTrue(MorrowfastQuests.TryConversation(edden, s.player, "eddens-account"));
            Assert.AreEqual(1, StoryletPart.Current.GetQuestState(MorrowfastQuests.ReturnQuestId).CurrentStageIndex);
            Assert.IsTrue(MorrowfastQuests.TryConversation(s.speaker, s.player, "refuse-return"));
            Assert.IsTrue(MorrowfastQuests.TryConversation(s.speaker, s.player, "accept-return"));
            Assert.IsFalse(MorrowfastQuests.TryConversation(s.speaker, s.player, "report-return"));
            Assert.AreEqual(0, StoryletPart.Current.GetQuestState(MorrowfastQuests.ReturnQuestId).CurrentStageIndex);
        }

        [Test] public void ADeadDetachedOrHostileSpeakerCannotResolveAPromise()
        {
            var s = Session("north-guard-east");
            Assert.IsTrue(MorrowfastQuests.TryConversation(s.speaker, s.player, "accept-return"));
            s.zone.RemoveEntity(s.speaker);
            Assert.IsFalse(MorrowfastQuests.TryConversation(s.speaker, s.player, "refuse-return"));
            s.zone.AddEntity(s.speaker, 10, 10);
            s.speaker.GetPart<BrainPart>().SetPersonallyHostile(s.player);
            Assert.IsFalse(MorrowfastQuests.TryConversation(s.speaker, s.player, "refuse-return"));
            s.speaker.GetPart<BrainPart>().PersonalEnemies.Clear();
            s.speaker.SetStatValue("Hitpoints", 0);
            Assert.IsFalse(MorrowfastQuests.TryConversation(s.speaker, s.player, "refuse-return"));
            Assert.IsTrue(StoryletPart.Current.IsQuestActive(MorrowfastQuests.ReturnQuestId));
        }

        [Test] public void ArchiveEntryIsVoluntaryAndWithdrawalActuallyClearsTheSavedConsent()
        {
            var s = Session("east-robed-resident");
            Assert.IsFalse(MorrowfastQuests.TryConversation(s.speaker, s.player, "withdraw-record"));
            Assert.IsTrue(MorrowfastQuests.TryConversation(s.speaker, s.player, "consent-record"));
            Assert.AreEqual(1, s.player.GetIntProperty("MorrowfastRecordConsent"));
            Assert.IsTrue(MorrowfastQuests.TryConversation(s.speaker, s.player, "withdraw-record"));
            Assert.AreEqual(0, s.player.GetIntProperty("MorrowfastRecordConsent"));
            Assert.AreEqual(0, StoryletPart.Current.GetActiveQuests().Count);
        }

        [Test] public void OrdinaryConversationUiSelectsTheNativeQuestAndHidesItsRepeatAcceptance()
        {
            var s = Session("north-guard-west");
            Assert.IsTrue(ConversationManager.StartConversation(s.speaker, s.player));
            int first = ConversationManager.VisibleChoices.ToList().FindIndex(c => c.Target == "Bell");
            Assert.GreaterOrEqual(first, 0); Assert.IsTrue(ConversationManager.SelectChoice(first));
            int accept = ConversationManager.VisibleChoices.ToList().FindIndex(c => c.Actions != null && c.Actions.Any(a => a.Value == "accept-bell"));
            Assert.GreaterOrEqual(accept, 0); Assert.IsFalse(ConversationManager.SelectChoice(accept), "Selecting End closes the native conversation.");
            Assert.IsTrue(StoryletPart.Current.IsQuestActive(MorrowfastQuests.BellQuestId));
            Assert.IsFalse(MorrowfastQuests.TryConversation(s.speaker, s.player, "accept-bell"));
            Assert.IsTrue(ConversationManager.StartConversation(s.speaker, s.player));
            first = ConversationManager.VisibleChoices.ToList().FindIndex(c => c.Target == "Bell");
            Assert.IsTrue(ConversationManager.SelectChoice(first));
            Assert.IsFalse(ConversationManager.VisibleChoices.Any(c => c.Actions != null && c.Actions.Any(a => a.Value == "accept-bell")));
        }

        private static int Count(Entity e, string blueprint) => e.GetPart<InventoryPart>().Objects
            .Where(i => i.BlueprintName == blueprint).Sum(i => i.GetPart<StackerPart>()?.StackCount ?? 1);
    }
}
