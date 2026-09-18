using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    // Native commands specify preview versus commitment, including cached
    // source observations, transaction callbacks and complete session persistence.
    public sealed class RegionalSituationOptInTests
    {
        private const int Seed = 64;
        private HotbarSaveFixture scope;
        private EntityFactory factory;
        private OverworldZoneManager manager;
        private Entity player;
        private Zone playerZone;

        [SetUp] public void SetUp()
        {
            scope = new HotbarSaveFixture(false, false);
            CinderholdCompositionTests.LoadLoot();
            factory = GrovelandsCompositionTests.Factory();
            manager = OverworldZoneManager.CreateDetached(factory, Seed);
            player = factory.CreateEntity("Player");
            player.GetPart<InventoryPart>().MaxWeight = -1;
            playerZone = null;
            StoryletPart.Current = new StoryletPart();
            StoryletPart.LocalPlayer = player;
            NarrativeStatePart.Current = new NarrativeStatePart();
            MorrowfastContent.EnsureRegistered();
        }

        [TearDown] public void TearDown()
        {
            ConversationManager.EndConversation();
            scope?.Dispose();
            LootTableRegistry.ResetForTests();
            Diag.ResetAll();
        }

        [TestCase("morrowfast-iron")][TestCase("cinderhold-iron")][TestCase("gantry-grain")]
        [TestCase("sumphold-oil")][TestCase("wellmeet-filters")]
        public void ReadingUnvisitedOfferDoesNotCommitOrGenerateItsSource(string id)
        {
            var d = RegionalSituations.Find(id); var b = Bind(id); StandBy(b.zone, b.recipient);
            Assert.IsFalse(manager.CachedZones.ContainsKey(d.SourceZoneId), "Positive control: the source is cold.");
            string[] possessions = Possessions(player), stock = Possessions(b.recipient);
            var reputation = PlayerReputation.GetAll().OrderBy(p => p.Key).ToArray();
            var properties = player.Properties.OrderBy(p => p.Key).ToArray();
            var integerProperties = player.IntProperties.OrderBy(p => p.Key).ToArray();
            int cached = manager.CachedZoneCount, money = TradeSystem.GetDrams(player);
            Assert.IsTrue(WorldInteractionSystem.GatherActions(b.recipient, player).Any(a => a.Command == "RegionalRequest:read"));
            MessageLog.Clear(); Assert.IsTrue(Act(b, "read"));
            Assert.IsFalse(b.request.Accepted); Assert.IsFalse(b.request.Completed);
            Assert.AreEqual(QuestCueState.Available, b.request.GetCueState(player, b.zone));
            Assert.AreEqual(cached, manager.CachedZoneCount);
            Assert.IsFalse(manager.CachedZones.ContainsKey(d.SourceZoneId));
            Assert.IsEmpty(RegionalSituationNotes.Read(player), "Inspecting an offer does not create an undertaking note.");
            CollectionAssert.AreEqual(possessions, Possessions(player)); CollectionAssert.AreEqual(stock, Possessions(b.recipient));
            CollectionAssert.AreEqual(reputation, PlayerReputation.GetAll().OrderBy(p => p.Key).ToArray());
            CollectionAssert.AreEqual(properties, player.Properties.OrderBy(p => p.Key).ToArray());
            CollectionAssert.AreEqual(integerProperties, player.IntProperties.OrderBy(p => p.Key).ToArray());
            Assert.AreEqual(money, TradeSystem.GetDrams(player));
            string text = Messages();
            StringAssert.Contains(d.Title.ToLowerInvariant(), text);
            StringAssert.Contains(ItemLabel(d.ItemBlueprint), text, "The offer must name its actual requested goods, not only its reward.");
            AssertRequestedQuantity(text, d.ItemCount, ItemLabel(d.ItemBlueprint));
            StringAssert.Contains(b.recipient.GetDisplayName().ToLowerInvariant(), text, "Name the actual return recipient.");
            var destination = WorldMap.FromZoneID(d.RecipientZoneId);
            StringAssert.Contains(manager.WorldMap.GetPOI(destination.x, destination.y).Name.ToLowerInvariant(), text);
            StringAssert.Contains(destination.x + "," + destination.y, text.Replace(" ", ""));
            if (d.Kind == RegionalSituationKind.Recovery)
            {
                StringAssert.Contains("sealed", text);
                Assert.IsTrue(text.Contains("consignment") || text.Contains("cargo") || text.Contains("parcel"));
                Assert.IsTrue(text.Contains("ordinary") || text.Contains("loose goods"), "The unique cargo requirement must not read as a normal supply purchase.");
            }
            else Assert.IsTrue(text.Contains("bought") || text.Contains("already carried") || text.Contains("outside goods"));
            StringAssert.Contains(ItemLabel(d.RewardBlueprint), text);
            StringAssert.Contains(d.RewardDrams + " drams", text);
            var source = WorldMap.FromZoneID(d.SourceZoneId);
            StringAssert.Contains(source.x + "," + source.y, text.Replace(" ", ""));
            Assert.IsTrue(text.Contains("not accepted") || text.Contains("not undertaken"), "Make the uncommitted state explicit.");
            AssertUnknown(text);
            StringAssert.Contains("accept this request",text,"An unvisited source is not a reason to hide opt-in guidance.");
            StringAssert.DoesNotContain("exhausted", text, "An ungenerated source is not an observed exhausted one.");
        }

        [TestCase("morrowfast-iron")][TestCase("sumphold-oil")]
        public void GatheredActionsEnableWorkOnlyAfterExplicitAcceptance(string id)
        {
            var b = Bind(id); StandBy(b.zone, b.recipient);
            var rows = Commands(b.recipient);
            CollectionAssert.Contains(rows, "RegionalRequest:read"); CollectionAssert.Contains(rows, "RegionalRequest:accept");
            CollectionAssert.DoesNotContain(rows, "RegionalRequest:deliver"); CollectionAssert.DoesNotContain(rows, "RegionalRequest:release");
            Assert.IsFalse(Act(b, "deliver")); Assert.IsFalse(Act(b, "release"));
            Assert.IsTrue(Act(b, "accept")); Assert.IsTrue(b.request.Accepted);
            Assert.AreEqual(QuestCueState.Active, b.request.GetCueState(player, b.zone));
            Assert.AreEqual(1, RegionalSituationNotes.Read(player).Count);
            string[] note = RegionalSituationNotes.Read(player).ToArray();
            int cached = manager.CachedZoneCount, money = TradeSystem.GetDrams(player);
            rows = Commands(b.recipient);
            CollectionAssert.Contains(rows, "RegionalRequest:read"); CollectionAssert.Contains(rows, "RegionalRequest:deliver");
            CollectionAssert.Contains(rows, "RegionalRequest:release"); CollectionAssert.DoesNotContain(rows, "RegionalRequest:accept");
            Assert.IsFalse(Act(b, "accept"), "Already active work is not a second undertaking.");
            MessageLog.Clear();
            Assert.IsTrue(Act(b, "read"), "Active terms can still be inspected.");
            string preview = Messages();
            Assert.IsTrue(preview.Contains("already accepted") || preview.Contains("already undertaken") || preview.Contains("[accepted]")
                || preview.Contains("active request") || preview.Contains("request is active") || preview.Contains("you have accepted"),
                "Active rereading must acknowledge the existing commitment.");
            StringAssert.DoesNotContain("not accepted", preview); StringAssert.DoesNotContain("not undertaken", preview);
            CollectionAssert.AreEqual(note, RegionalSituationNotes.Read(player));
            Assert.AreEqual(cached, manager.CachedZoneCount); Assert.AreEqual(money, TradeSystem.GetDrams(player));
        }

        [TestCase(false)][TestCase(true)]
        public void KnownLostRecoveryCanBeReadButCannotBeAcceptedOrRecreated(bool removeCargo)
        {
            var d = RegionalSituations.Find("sumphold-oil"); var b = Bind(d.Id);
            var source = manager.GetZone(d.SourceZoneId); string sourceId = RegionalSituations.SourceId(d, Seed);
            var cargo = source.GetAllEntities().Single(e => e.ID == sourceId);
            if (removeCargo) Assert.IsTrue(source.RemoveEntity(cargo));
            StandBy(b.zone, b.recipient); int cached = manager.CachedZoneCount;
            MessageLog.Clear(); Assert.IsTrue(Act(b, "read"), "A local recipient can explain known unavailable work without undertaking it.");
            Assert.IsFalse(b.request.Accepted); Assert.IsEmpty(RegionalSituationNotes.Read(player));
            if (removeCargo)
            {
                AssertUnavailable(Messages());
                StringAssert.DoesNotContain("choose accept",Messages(),"Do not advertise an action absent from this known-lost offer.");
            }
            else StringAssert.Contains("accept this request",Messages(),"A viable offer retains explicit opt-in guidance.");
            Assert.AreEqual(removeCargo ? QuestCueState.None : QuestCueState.Available, b.request.GetCueState(player, b.zone));
            Assert.AreEqual(!removeCargo, Act(b, "accept"));
            Assert.AreEqual(!removeCargo, b.request.Accepted);
            Assert.AreEqual(removeCargo ? 0 : 1, source.GetAllEntities().Count(e => e.ID == sourceId));
            Assert.AreEqual(cached, manager.CachedZoneCount); Assert.IsFalse(b.request.Completed);
        }

        [TestCase("read")][TestCase("accept")]
        public void PreviouslyGatheredCommandCannotActAtADistance(string verb)
        {
            var b = Bind("gantry-grain"); StandBy(b.zone, b.recipient);
            var action = WorldInteractionSystem.GatherActions(b.recipient, player).Single(a => a.Command == "RegionalRequest:" + verb);
            var owner = b.zone.GetEntityCell(b.recipient);
            var far = Enumerable.Range(0, Zone.Width * Zone.Height).Select(i => b.zone.GetCell(i % Zone.Width, i / Zone.Width))
                .First(c => !c.BlocksMovement() && Math.Abs(c.X - owner.X) + Math.Abs(c.Y - owner.Y) > 12);
            Assert.IsTrue(b.zone.MoveEntity(player, far.X, far.Y)); int cached = manager.CachedZoneCount;
            Assert.IsFalse(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(b.recipient, action.Command), player, b.zone).Success);
            Assert.IsFalse(b.request.Accepted); Assert.IsEmpty(RegionalSituationNotes.Read(player));
            Assert.AreEqual(cached, manager.CachedZoneCount);
            StandBy(b.zone, b.recipient); Assert.IsTrue(Act(b, verb), "The same physical recipient becomes usable when approached.");
            Assert.AreEqual(verb == "accept", b.request.Accepted);
        }

        [TestCase("gantry-grain")][TestCase("sumphold-oil")]
        public void ReleasedReceiptSurvivesInspectionUntilExplicitReacceptance(string id)
        {
            var b = Bind(id); StandBy(b.zone, b.recipient);
            Assert.IsTrue(Act(b, "accept")); Assert.IsTrue(Act(b, "release"));
            string[] receipt = RegionalSituationNotes.Read(player).ToArray(); Assert.AreEqual(1, receipt.Length);
            StringAssert.Contains("[released]", receipt[0]);
            int money = TradeSystem.GetDrams(player); string[] before = Possessions(player);
            Assert.IsTrue(Act(b, "read")); Assert.IsFalse(b.request.Accepted);
            CollectionAssert.AreEqual(receipt, RegionalSituationNotes.Read(player));
            Assert.IsTrue(Act(b, "accept")); Assert.IsTrue(b.request.Accepted);
            Assert.AreEqual(1, RegionalSituationNotes.Read(player).Count);
            Assert.AreNotEqual(receipt[0], RegionalSituationNotes.Read(player)[0]);
            Assert.AreEqual(QuestCueState.Active, b.request.GetCueState(player, b.zone));
            Assert.AreEqual(money, TradeSystem.GetDrams(player)); CollectionAssert.AreEqual(before, Possessions(player));
        }

        [Test]
        public void HavingGoodsAndReadingTermsDoesNotAuthorizePayment()
        {
            var d = RegionalSituations.Find("gantry-grain"); var b = Bind(d.Id); StandBy(b.zone, b.recipient);
            Carry(d.ItemBlueprint, d.ItemCount); int stock = Units(b.recipient, d.ItemBlueprint), money = TradeSystem.GetDrams(player);
            int rewards = Units(player, d.RewardBlueprint);
            Assert.IsTrue(Act(b, "read")); Assert.IsFalse(Act(b, "deliver"));
            Assert.AreEqual(d.ItemCount, Units(player, d.ItemBlueprint)); Assert.AreEqual(stock, Units(b.recipient, d.ItemBlueprint));
            Assert.AreEqual(money, TradeSystem.GetDrams(player)); Assert.IsFalse(b.request.Completed);
            Assert.IsTrue(Act(b, "accept")); Assert.IsTrue(Act(b, "deliver"));
            Assert.AreEqual(stock + d.ItemCount, Units(b.recipient, d.ItemBlueprint)); Assert.AreEqual(0, Units(player, d.ItemBlueprint));
            Assert.AreEqual(money + d.RewardDrams, TradeSystem.GetDrams(player)); Assert.AreEqual(rewards + 1, Units(player, d.RewardBlueprint));
            var receipt = RegionalSituationNotes.Read(player).ToArray();
            Carry(d.ItemBlueprint, d.ItemCount);
            Assert.IsFalse(Act(b, "accept")); Assert.IsFalse(Act(b, "deliver"));
            CollectionAssert.AreEqual(receipt, RegionalSituationNotes.Read(player));
            Assert.AreEqual(money + d.RewardDrams, TradeSystem.GetDrams(player));
            Assert.AreEqual(d.ItemCount, Units(player, d.ItemBlueprint));
        }

        [TestCase("before")][TestCase("after")][TestCase("none")]
        public void NativeCallbacksCannotPublishARolledBackAcceptance(string failure)
        {
            var d = RegionalSituations.Find("gantry-grain"); var b = Bind(d.Id); StandBy(b.zone, b.recipient);
            manager.GetZone(d.SourceZoneId); // Isolate commitment rollback from independent cache hydration.
            var probe = new AcceptanceProbe { Failure = failure }; player.AddPart(probe);
            int money = TradeSystem.GetDrams(player); MessageLog.Clear(); Diag.ResetAll(); Diag.SetChannel("quest", true);
            Assert.AreEqual(failure == "none", Act(b, "accept"));
            Assert.AreEqual(1, probe.Before); Assert.AreEqual(failure == "before" ? 0 : 1, probe.After);
            Assert.AreEqual(0, probe.AppliedDuringAfter, "Success diagnostics cannot precede native AfterInventoryAction completion.");
            Assert.AreEqual(0, probe.MessagesDuringAfter, "Acceptance confirmation cannot precede native AfterInventoryAction completion.");
            Assert.AreEqual(failure == "none" ? 1 : 0, Applied(player, b.recipient), "Only committed acceptance emits an applied diagnostic.");
            Assert.AreEqual(failure == "none", b.request.Accepted);
            Assert.AreEqual(failure == "none" ? 1 : 0, RegionalSituationNotes.Read(player).Count);
            Assert.AreEqual(money, TradeSystem.GetDrams(player));
            if (failure != "none")
            {
                Assert.IsFalse(MessageLog.GetMessages().Any(m => m.Contains("[accepted]") || m.Contains("Recorded in [Q]")),
                    "No success receipt before the outer native transaction commits.");
                probe.Failure = "none"; Assert.IsTrue(Act(b, "accept"), "Rollback releases the claim and permits an honest retry.");
            }
            Assert.IsTrue(b.request.Accepted); Assert.IsFalse(b.request.Completed);
            Assert.AreEqual(1, RegionalSituationNotes.Read(player).Count);
            Assert.AreEqual(1, Applied(player, b.recipient), "Failure plus successful retry is exactly one committed acceptance.");
            var applied = DiagQuery.Apply(AppliedFilter(player, b.recipient)).Records.Single();
            Assert.IsTrue(Regex.IsMatch(applied.PayloadJson ?? "", "\"action\"\\s*:\\s*\"accept\""), "The receipt must describe acceptance, not the old read mutation.");
            Assert.Greater(MessageLog.Count, 0, "Successful acceptance still confirms the action to the player.");
        }

        [Test]
        public void FullSessionKeepsPreviewActiveReleasedAndCompletedInstancesDistinct()
        {
            var preview = Bind("morrowfast-iron"); StandBy(preview.zone, preview.recipient); Assert.IsTrue(Act(preview, "read"));
            var active = Bind("cinderhold-iron"); StandBy(active.zone, active.recipient); Assert.IsTrue(Act(active, "accept"));
            var released = Bind("sumphold-oil"); StandBy(released.zone, released.recipient);
            Assert.IsTrue(Act(released, "accept")); Assert.IsTrue(Act(released, "release"));
            var completed = Bind("gantry-grain"); StandBy(completed.zone, completed.recipient);
            Assert.IsTrue(Act(completed, "accept")); Carry("Emberwheat", 2); Assert.IsTrue(Act(completed, "deliver"));
            var notes = RegionalSituationNotes.Read(player).ToArray(); Assert.AreEqual(3, notes.Length);
            int cached = manager.CachedZoneCount, money = TradeSystem.GetDrams(player);
            Assert.IsFalse(manager.CachedZones.ContainsKey(RegionalSituations.Find("morrowfast-iron").SourceZoneId));
            RoundTripWorld();
            CollectionAssert.AreEqual(notes, RegionalSituationNotes.Read(player)); Assert.AreEqual(cached, manager.CachedZoneCount);
            Assert.AreEqual(money, TradeSystem.GetDrams(player));
            preview = Bind("morrowfast-iron"); active = Bind("cinderhold-iron"); released = Bind("sumphold-oil"); completed = Bind("gantry-grain");
            Assert.IsFalse(preview.request.Accepted); Assert.IsFalse(preview.request.Completed);
            Assert.IsTrue(active.request.Accepted); Assert.IsFalse(active.request.Completed);
            Assert.IsFalse(released.request.Accepted); Assert.IsFalse(released.request.Completed);
            Assert.IsFalse(completed.request.Accepted); Assert.IsTrue(completed.request.Completed);
            StandBy(preview.zone, preview.recipient); Assert.IsTrue(Act(preview, "read"));
            CollectionAssert.AreEqual(notes, RegionalSituationNotes.Read(player));
            Assert.IsFalse(manager.CachedZones.ContainsKey(RegionalSituations.Find("morrowfast-iron").SourceZoneId));
            StandBy(completed.zone, completed.recipient); Carry("Emberwheat", 2);
            Assert.IsFalse(Act(completed, "accept")); Assert.IsFalse(Act(completed, "deliver"));
            Assert.AreEqual(money, TradeSystem.GetDrams(player)); CollectionAssert.AreEqual(notes, RegionalSituationNotes.Read(player));
        }

        [TestCase("morrowfast-iron", false)][TestCase("gantry-grain", false)][TestCase("gantry-grain", true)]
        public void PreviewDistinguishesUnknownLiveAndSpentSupplyWithoutCreatingWork(string id, bool harvestRows)
        {
            var d = RegionalSituations.Find(id); var b = Bind(id); StandBy(b.zone, b.recipient);
            Assert.IsFalse(manager.CachedZones.ContainsKey(d.SourceZoneId));
            MessageLog.Clear(); Assert.IsTrue(Act(b, "read")); AssertUnknown(Messages());
            Assert.IsFalse(manager.CachedZones.ContainsKey(d.SourceZoneId));
            var source = manager.GetZone(d.SourceZoneId); string sourceId = RegionalSituations.SourceId(d, Seed);
            var owners = source.GetAllEntities().Where(e => e.ID == sourceId || e.ID.StartsWith(sourceId + ":", StringComparison.Ordinal)).ToArray();
            Assert.Greater(owners.Length, 0, "Actual finite source, not a fake missing-owner premise.");
            MessageLog.Clear(); Assert.IsTrue(Act(b, "read")); string live = Messages();
            Assert.IsTrue(Regex.IsMatch(live, @"\b(available|remaining|ripe|present|intact)\b"), "A cached live source needs a positive observation.");
            Assert.IsFalse(HasUnknownAvailability(live)); StringAssert.DoesNotContain("unavailable", live); StringAssert.DoesNotContain("exhausted", live);
            Assert.IsFalse(b.request.Accepted); Assert.IsEmpty(RegionalSituationNotes.Read(player));
            if (harvestRows)
            {
                var previousFactory = HarvestablePart.Factory; HarvestablePart.Factory = factory;
                try
                {
                    foreach (var owner in owners)
                    {
                        Assert.NotNull(owner.GetPart<FieldHarvestPart>()); Assert.IsFalse(owner.GetPart<FieldHarvestPart>().Harvested);
                        StandBy(source, owner);
                        Assert.IsTrue(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(owner, "Harvest"), player, source).Success);
                        Assert.IsTrue(owner.GetPart<FieldHarvestPart>().Harvested);
                        Assert.NotNull(source.GetEntityCell(owner), "Spent rows remain real owners; existence alone does not prove available grain.");
                    }
                }
                finally { HarvestablePart.Factory = previousFactory; }
                StandBy(b.zone, b.recipient);
            }
            else foreach (var owner in owners) Assert.IsTrue(source.RemoveEntity(owner));
            MessageLog.Clear(); Assert.IsTrue(Act(b, "read")); string text = Messages(); AssertUnavailable(text);
            Assert.IsTrue(text.Contains("bought") || text.Contains("already carried") || text.Contains("outside goods"));
            Assert.IsFalse(b.request.Accepted); Assert.IsEmpty(RegionalSituationNotes.Read(player));
            Assert.IsTrue(Act(b, "accept"), "Supply work remains possible using actual alternative goods.");
            Assert.AreEqual(harvestRows ? owners.Length : 0,
                source.GetAllEntities().Count(e => e.ID == sourceId || e.ID.StartsWith(sourceId + ":", StringComparison.Ordinal)),
                "Acceptance must neither recreate removed sources nor replace real stubble.");
            if (harvestRows) Assert.IsTrue(owners.All(e => e.GetPart<FieldHarvestPart>().Harvested));
        }

        private sealed class AcceptanceProbe : Part
        {
            public override string Name => "RegionalOptInProbe";
            public string Failure; public int Before, After, AppliedDuringAfter, MessagesDuringAfter;
            public override bool HandleEvent(GameEvent e)
            {
                if (e.GetStringParameter("Command") != "RegionalRequest:accept") return true;
                if (e.ID == "BeforeInventoryAction") { Before++; return Failure != "before"; }
                if (e.ID == "AfterInventoryAction")
                {
                    After++; AppliedDuringAfter = Applied(e.GetParameter<Entity>("Actor"), e.GetParameter<Entity>("Item"));
                    MessagesDuringAfter = MessageLog.Count;
                    if (Failure == "after") throw new InvalidOperationException("Acceptance rollback probe");
                }
                return true;
            }
        }
        private bool Act((Zone zone, Entity recipient, RegionalRequestPart request) b, string verb)
            => InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(b.recipient, "RegionalRequest:" + verb), player, b.zone).Success;
        private string[] Commands(Entity recipient) => WorldInteractionSystem.GatherActions(recipient, player).Select(a => a.Command).ToArray();
        private static string Messages() => string.Join(" ", MessageLog.GetMessages()).ToLowerInvariant();
        private static bool HasUnknownAvailability(string text) => text.Contains("not checked") || text.Contains("unverified") || text.Contains("unknown");
        private static void AssertUnknown(string text) => Assert.IsTrue(HasUnknownAvailability(text), "Cold source availability must be explicitly unknown.");
        private static void AssertUnavailable(string text) => Assert.IsTrue(text.Contains("unavailable") || text.Contains("exhausted") || text.Contains("no longer") || text.Contains("gone"), "Known missing source must be disclosed.");
        private static DiagQuery.Filter AppliedFilter(Entity actor, Entity recipient) => new DiagQuery.Filter
            { Category = "quest", Kind = "RegionalRequestApplied", Actor = actor.ID, Target = recipient.ID };
        private static int Applied(Entity actor, Entity recipient) => DiagQuery.Count(AppliedFilter(actor, recipient)).Count;
        private static void AssertRequestedQuantity(string text, int count, string item)
        {
            string word = count == 1 ? "one" : count == 2 ? "two" : count.ToString();
            string amount = @"\b(" + count + "|" + word + @")\b";
            string label = Regex.Escape(item);
            Assert.IsTrue(Regex.IsMatch(text, amount + @"\s*(?:[x×]\s*|(?:units?|pieces?|portions?|bundles?|chunks?)\s+of\s+)?" + label)
                || Regex.IsMatch(text, label + @"\s*(?:[x×:]\s*|\(\s*)" + amount),
                "Associate the quantity with the requested item; a coordinate or the reward's 'one' is not sufficient.");
        }
        private string ItemLabel(string blueprint) => factory.Blueprints[blueprint].Parts["Render"]["DisplayName"].ToLowerInvariant();
        private static string[] Possessions(Entity owner) => owner.GetPart<InventoryPart>().Objects
            .Select(e => e.ID + ":" + e.BlueprintName + ":" + (e.GetPart<StackerPart>()?.StackCount ?? 1) + ":" + e.GetPart<PhysicsPart>()?.InInventory?.ID)
            .OrderBy(s => s, StringComparer.Ordinal).ToArray();
        private static int Units(Entity owner, string blueprint) => owner.GetPart<InventoryPart>().Objects
            .Where(e => e.BlueprintName == blueprint && ReferenceEquals(e.GetPart<PhysicsPart>()?.InInventory, owner))
            .Sum(e => Math.Max(0, e.GetPart<StackerPart>()?.StackCount ?? 1));
        private void Carry(string blueprint, int count)
        {
            for (int i = 0; i < count; i++) Assert.IsTrue(player.GetPart<InventoryPart>().AddObject(factory.CreateEntity(blueprint)));
        }
        private (Zone zone, Entity recipient, RegionalRequestPart request) Bind(string id)
        {
            var d = RegionalSituations.Find(id); var zone = manager.GetZone(d.RecipientZoneId);
            var recipient = zone.GetAllEntities().Single(e => e.GetPart<RegionalRequestPart>()?.DefinitionId == id);
            return (zone, recipient, recipient.GetPart<RegionalRequestPart>());
        }
        private void StandBy(Zone zone, Entity recipient)
        {
            var at = zone.GetEntityCell(recipient);
            var standing = CinderholdCompositionTests.Neighbors(at.X, at.Y).Select(p => zone.GetCell(p.x, p.y))
                .First(c => c != null && !c.BlocksMovement());
            if (playerZone != null) Assert.IsTrue(playerZone.RemoveEntity(player));
            Assert.IsTrue(zone.AddEntity(player, standing.X, standing.Y)); playerZone = zone;
            manager.SetActiveZone(zone); SettlementRuntime.ActiveZone = zone;
        }
        private void RoundTripWorld()
        {
            string id = player.ID;
            using (var stream = new MemoryStream())
            {
                GameSessionState.Capture("regional-opt-in", "test", manager, null, player).Save(new SaveWriter(stream));
                stream.Position = 0; var restored = GameSessionState.Load(new SaveReader(stream, factory));
                manager = restored.ZoneManager; Assert.AreEqual(id, restored.Player.ID);
            }
            playerZone = manager.CachedZones.Values.Single(z => z.GetAllEntities().Any(e => e.ID == id));
            player = playerZone.GetAllEntities().Single(e => e.ID == id);
            StoryletPart.LocalPlayer = player; manager.SetActiveZone(playerZone); SettlementRuntime.ActiveZone = playerZone;
        }
    }
}
