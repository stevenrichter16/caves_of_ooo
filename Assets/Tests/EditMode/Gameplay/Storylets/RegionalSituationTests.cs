using System;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using CavesOfOoo.Storylets;
using NUnit.Framework;

namespace CavesOfOoo.Tests
{
    /// <summary>
    /// First-wave player-flow specifications. Real native managers, recipients,
    /// inventories and pickup/action commands are used; no synthetic giver or
    /// direct completion-property writes stand in for successful play.
    /// Kept outside Assets until the independent art milestone has finished.
    /// </summary>
    public sealed class RegionalSituationTests
    {
        private const int Seed = 64;
        private HotbarSaveFixture scope;
        private EntityFactory factory;
        private OverworldZoneManager manager;
        private Entity player;
        private Zone playerZone;

        private static readonly string[] BindingIds =
        {
            "morrowfast-iron", "cinderhold-iron", "gantry-grain",
            "sumphold-oil", "wellmeet-filters"
        };

        [SetUp]
        public void SetUp()
        {
            scope = new HotbarSaveFixture(false, false);
            playerZone = null;
            CinderholdCompositionTests.LoadLoot();
            factory = GrovelandsCompositionTests.Factory();
            manager = OverworldZoneManager.CreateDetached(factory, Seed);
            player = factory.CreateEntity("Player");
            Assert.NotNull(player);
            player.GetPart<InventoryPart>().MaxWeight = -1;
            StoryletPart.Current = new StoryletPart();
            StoryletPart.LocalPlayer = player;
            NarrativeStatePart.Current = new NarrativeStatePart();
            MorrowfastContent.EnsureRegistered();
        }

        [TearDown]
        public void TearDown()
        {
            ConversationManager.EndConversation();
            scope?.Dispose();
            LootTableRegistry.ResetForTests();
        }

        // H1: instance identity separates template reuse and world seeds. The
        // Morrowfast source must be Grove ground: 2.5 is actually Stump.
        [Test]
        public void FiveBindingsHaveDistinctInstancesAndTheirSourcesMatchNativeBiomes()
        {
            CollectionAssert.AreEquivalent(BindingIds, RegionalSituations.Definitions.Select(d => d.Id));
            var expected = new[]
            {
                ("morrowfast-iron", "Overworld.3.6.0", "Overworld.1.5.0", "ChoirIron", 1, RegionalSituationKind.Supply, BiomeType.Grovelands),
                ("cinderhold-iron", "Overworld.6.6.0", "Overworld.5.6.0", "ChoirIron", 1, RegionalSituationKind.Supply, BiomeType.Grovelands),
                ("gantry-grain", "Overworld.7.8.0", "Overworld.7.7.0", "Emberwheat", 2, RegionalSituationKind.Supply, BiomeType.Spread),
                ("sumphold-oil", "Overworld.15.6.0", "Overworld.16.6.0", "WardOil", 2, RegionalSituationKind.Recovery, BiomeType.Sodden),
                ("wellmeet-filters", "Overworld.8.16.0", "Overworld.8.17.0", "SilverSand", 2, RegionalSituationKind.Recovery, BiomeType.Beating)
            };
            foreach (var row in expected)
            {
                var d = RegionalSituations.Find(row.Item1);
                Assert.NotNull(d, row.Item1);
                Assert.AreEqual(row.Item2, d.RecipientZoneId);
                Assert.AreEqual(row.Item3, d.SourceZoneId);
                Assert.AreEqual(row.Item4, d.ItemBlueprint);
                Assert.AreEqual(row.Item5, d.ItemCount);
                Assert.AreEqual(row.Item6, d.Kind);
                var p = WorldMap.FromZoneID(d.SourceZoneId);
                Assert.AreEqual(row.Item7, manager.WorldMap.GetBiome(p.x, p.y), d.Id);
                Assert.IsNull(manager.WorldMap.GetPOI(p.x, p.y), d.Id);
                Assert.IsFalse(SinkholeSites.IsMouth(p.x, p.y));
                Assert.Greater(d.RewardDrams, 0);
                Assert.IsNotEmpty(d.RewardBlueprint);
                Assert.AreEqual(RegionalSituations.InstanceId(d, Seed), RegionalSituations.InstanceId(d, Seed));
                Assert.AreNotEqual(RegionalSituations.InstanceId(d, Seed), RegionalSituations.InstanceId(d, 1729));
            }
            Assert.AreEqual(5, RegionalSituations.Definitions.Select(d => RegionalSituations.InstanceId(d, Seed)).Distinct().Count());
            Assert.IsNull(RegionalSituations.Find("not-a-regional-request"));
            Assert.IsNull(RegionalSituations.Find(null));
        }

        // H2: the actual town pipeline binds one real working recipient and
        // leaves all other residents' services alone.
        [Test]
        public void ActualTownPipelinesBindExactlyTheIntendedLivingRecipient()
        {
            foreach (string id in BindingIds)
            {
                var d = RegionalSituations.Find(id);
                var b = Bind(id);
                Assert.AreEqual(d.RecipientZoneId, b.zone.ZoneID);
                Assert.AreEqual(id, b.request.DefinitionId);
                Assert.AreEqual(b.recipient.ID, b.request.RecipientId);
                Assert.AreEqual(RegionalSituations.InstanceId(d, Seed), b.request.InstanceId);
                Assert.IsFalse(b.request.Completed);
                Assert.IsFalse(b.request.Accepted);
                Assert.Greater(b.recipient.GetStatValue("Hitpoints"), 0);
                Assert.NotNull(b.recipient.GetPart<ConversationPart>());
                Assert.NotNull(b.recipient.GetPart<InventoryPart>());
                if (id == "morrowfast-iron")
                    Assert.AreSame(MorrowfastSceneRuntime.FindOwner(b.zone, "southwest-craftsperson"), b.recipient);
                else
                    Assert.AreEqual(id == "cinderhold-iron" ? "Weaponsmith" : "Merchant", b.recipient.BlueprintName);
                Assert.AreEqual(1, b.zone.GetAllEntities().Count(e => e.HasPart<RegionalRequestPart>()), id);
            }
        }

        // H3: a request is backed by real reachable content, not just a note.
        // Source lookup must be valid before any recipient is visited.
        [Test]
        public void EveryFreshSourceContainsOneRealPrimaryOwnerWithReachableStandingSpace()
        {
            foreach (string id in BindingIds)
            {
                var d = RegionalSituations.Find(id);
                var z = manager.GetZone(d.SourceZoneId);
                var owner = Source(z, id);
                var at = z.GetEntityCell(owner);
                var reachable = CinderholdCompositionTests.Flood(z);
                Assert.IsTrue(CinderholdCompositionTests.Neighbors(at.X, at.Y)
                    .Any(p => z.InBounds(p.x, p.y) && reachable[p.x, p.y]), id + " has no reachable standing space");
                if (d.Kind == RegionalSituationKind.Recovery)
                {
                    Assert.IsTrue(owner.GetPart<PhysicsPart>().Takeable, id);
                    Assert.IsFalse(owner.GetPart<PhysicsPart>().Solid, id);
                }
                else if (d.ItemBlueprint == "ChoirIron")
                {
                    Assert.AreEqual("ChoirIronVein", owner.BlueprintName);
                    Assert.IsTrue(owner.HasTag("MineralVein"));
                    Assert.AreEqual("ChoirIron", owner.GetPart<HarvestablePart>().YieldBlueprint);
                }
                else
                {
                    Assert.AreEqual("RipeCropRow", owner.BlueprintName);
                    Assert.IsFalse(owner.GetPart<FieldHarvestPart>().Harvested);
                }
            }
        }

        // H4: outside goods are a real alternative to protected extraction.
        // Count units rather than entries: delivery may merge native stacks.
        [TestCase("morrowfast-iron")]
        [TestCase("cinderhold-iron")]
        [TestCase("gantry-grain")]
        public void BroughtSupplyEntersRealStockAndPaysOnlyOnce(string id)
        {
            var d = RegionalSituations.Find(id); var b = Bind(id);
            StandBy(b.zone, b.recipient);
            Assert.IsTrue(b.request.TryAct(player, b.zone, "accept"));
            Carry(d.ItemBlueprint, d.ItemCount);
            int stockBefore = Units(b.recipient, d.ItemBlueprint);
            int payBefore = TradeSystem.GetDrams(player);
            int rewardBefore = Units(player, d.RewardBlueprint);
            Assert.IsTrue(b.request.TryAct(player, b.zone, "deliver"));
            Assert.AreEqual(0, Units(player, d.ItemBlueprint));
            Assert.AreEqual(stockBefore + d.ItemCount, Units(b.recipient, d.ItemBlueprint));
            Assert.AreEqual(payBefore + d.RewardDrams, TradeSystem.GetDrams(player));
            Assert.AreEqual(rewardBefore + 1, Units(player, d.RewardBlueprint));
            Assert.IsTrue(b.request.Completed);
            Carry(d.ItemBlueprint, d.ItemCount);
            Assert.IsFalse(b.request.TryAct(player, b.zone, "deliver"));
            Assert.AreEqual(d.ItemCount, Units(player, d.ItemBlueprint));
            Assert.AreEqual(payBefore + d.RewardDrams, TradeSystem.GetDrams(player));
            Assert.AreEqual(stockBefore + d.ItemCount, Units(b.recipient, d.ItemBlueprint));
        }

        // H5: one grain is not two. Failure cannot consume the partial payment
        // or mark completion; supplying the missing unit then works.
        [Test]
        public void IncompleteSupplyPreservesEveryUnitAndDoesNotReward()
        {
            var d = RegionalSituations.Find("gantry-grain"); var b = Bind(d.Id);
            StandBy(b.zone, b.recipient); Assert.IsTrue(b.request.TryAct(player, b.zone, "accept"));
            Carry(d.ItemBlueprint, d.ItemCount - 1);
            int stock = Units(b.recipient, d.ItemBlueprint), money = TradeSystem.GetDrams(player);
            var before = player.GetPart<InventoryPart>().Objects.ToArray();
            Assert.IsFalse(b.request.TryAct(player, b.zone, "deliver"));
            CollectionAssert.AreEqual(before, player.GetPart<InventoryPart>().Objects);
            Assert.AreEqual(d.ItemCount - 1, Units(player, d.ItemBlueprint));
            Assert.AreEqual(stock, Units(b.recipient, d.ItemBlueprint));
            Assert.AreEqual(money, TradeSystem.GetDrams(player)); Assert.IsFalse(b.request.Completed);
            Carry(d.ItemBlueprint, 1);
            Assert.IsTrue(b.request.TryAct(player, b.zone, "deliver"));
        }

        // H6: recipient-capacity failure must roll back the payment rather
        // than turn the merchant's refusal into a free/vanished consignment.
        [Test]
        public void FullRecipientInventoryLeavesPaymentRequestAndRewardUnchanged()
        {
            var d = RegionalSituations.Find("gantry-grain"); var b = Bind(d.Id);
            StandBy(b.zone, b.recipient); Assert.IsTrue(b.request.TryAct(player, b.zone, "accept"));
            Carry(d.ItemBlueprint, d.ItemCount);
            var inv = b.recipient.GetPart<InventoryPart>(); inv.MaxWeight = 0;
            int stock = Units(b.recipient, d.ItemBlueprint), money = TradeSystem.GetDrams(player);
            var before = player.GetPart<InventoryPart>().Objects.ToArray();
            Assert.IsFalse(b.request.TryAct(player, b.zone, "deliver"));
            CollectionAssert.AreEqual(before, player.GetPart<InventoryPart>().Objects);
            Assert.AreEqual(d.ItemCount, Units(player, d.ItemBlueprint));
            Assert.AreEqual(stock, Units(b.recipient, d.ItemBlueprint));
            Assert.AreEqual(money, TradeSystem.GetDrams(player)); Assert.IsFalse(b.request.Completed);
            inv.MaxWeight = -1;
            Assert.IsTrue(b.request.TryAct(player, b.zone, "deliver"));
        }

        // H7: recovery uses the original physical cargo, not any equivalent
        // amount of its contents or an ordinary sack. Pickup crosses a real
        // inventory command before returning to the native recipient.
        [TestCase("sumphold-oil")]
        [TestCase("wellmeet-filters")]
        public void ExactRecoveredCargoDeliversRealStockWhereOrdinaryGoodsDoNot(string id)
        {
            var d = RegionalSituations.Find(id); var b = Bind(id);
            StandBy(b.zone, b.recipient); Assert.IsTrue(b.request.TryAct(player, b.zone, "accept"));
            Carry("Sack", 1); Carry(d.ItemBlueprint, d.ItemCount);
            int originalLoose = Units(player, d.ItemBlueprint);
            int stock = Units(b.recipient, d.ItemBlueprint), money = TradeSystem.GetDrams(player);
            Assert.IsFalse(b.request.TryAct(player, b.zone, "deliver"));
            Assert.AreEqual(money, TradeSystem.GetDrams(player)); Assert.IsFalse(b.request.Completed);
            var source = manager.GetZone(d.SourceZoneId); var cargo = Source(source, id);
            var at = source.GetEntityCell(cargo); MovePlayer(source, at.X, at.Y);
            Assert.IsTrue(InventorySystem.ExecuteCommand(new PickupCommand(cargo), player, source).Success);
            Assert.AreSame(player, cargo.GetPart<PhysicsPart>().InInventory);
            StandBy(b.zone, b.recipient);
            Assert.IsTrue(b.request.TryAct(player, b.zone, "deliver"));
            Assert.IsFalse(player.GetPart<InventoryPart>().Objects.Contains(cargo));
            Assert.AreEqual(originalLoose, Units(player, d.ItemBlueprint), "Unrelated loose goods must remain carried.");
            Assert.AreEqual(stock + d.ItemCount, Units(b.recipient, d.ItemBlueprint));
            Assert.GreaterOrEqual(TradeSystem.GetDrams(player), money + d.RewardDrams, "The wetland may additionally pay its documented preservation bonus.");
            Assert.IsTrue(b.request.Completed);
            int after = TradeSystem.GetDrams(player);
            Assert.IsFalse(b.request.TryAct(player, b.zone, "deliver"));
            Assert.AreEqual(after, TradeSystem.GetDrams(player));
        }

        // H8: completing one iron request does not close another copy of the
        // same template, and the same one-unit payment cannot pay both.
        [Test]
        public void SeparateIronBindingsRemainIndependentlyPlayable()
        {
            var first = Bind("morrowfast-iron"); var second = Bind("cinderhold-iron");
            StandBy(first.zone, first.recipient); Assert.IsTrue(first.request.TryAct(player, first.zone, "accept"));
            Carry("ChoirIron", 1); Assert.IsTrue(first.request.TryAct(player, first.zone, "deliver"));
            StandBy(second.zone, second.recipient); Assert.IsTrue(second.request.TryAct(player, second.zone, "accept"));
            Assert.IsFalse(second.request.Completed);
            Assert.IsFalse(second.request.TryAct(player, second.zone, "deliver"));
            Carry("ChoirIron", 1); Assert.IsTrue(second.request.TryAct(player, second.zone, "deliver"));
            Assert.IsTrue(first.request.Completed); Assert.IsTrue(second.request.Completed);
            Assert.AreNotEqual(first.request.InstanceId, second.request.InstanceId);
            Assert.AreEqual(2, RegionalSituationNotes.Read(player).Count);
        }

        // H9: ordering cannot duplicate source owners, while a removed source
        // stays removed after both ordinary access and a repeated hook call.
        [Test]
        public void SourceFirstAndRecipientFirstResolveSameInstanceWithoutReplenishment()
        {
            foreach (string id in BindingIds)
            {
                var d = RegionalSituations.Find(id);
                var sourceFirst = OverworldZoneManager.CreateDetached(factory, Seed);
                var recipientFirst = OverworldZoneManager.CreateDetached(factory, Seed);
                var aSource = sourceFirst.GetZone(d.SourceZoneId);
                var bTown = recipientFirst.GetZone(d.RecipientZoneId);
                var aTown = sourceFirst.GetZone(d.RecipientZoneId);
                var bSource = recipientFirst.GetZone(d.SourceZoneId);
                var a = Source(aSource, id); var b = Source(bSource, id);
                Assert.AreEqual(a.ID, b.ID); Assert.AreEqual(a.BlueprintName, b.BlueprintName);
                var ac = aSource.GetEntityCell(a); var bc = bSource.GetEntityCell(b);
                Assert.AreEqual((ac.X, ac.Y), (bc.X, bc.Y));
                Assert.AreEqual(aTown.GetAllEntities().Single(e => e.HasPart<RegionalRequestPart>()).GetPart<RegionalRequestPart>().InstanceId,
                    bTown.GetAllEntities().Single(e => e.HasPart<RegionalRequestPart>()).GetPart<RegionalRequestPart>().InstanceId);
                Assert.IsTrue(aSource.RemoveEntity(a));
                RegionalSituations.OnZoneGenerated(aSource, sourceFirst);
                Assert.IsFalse(sourceFirst.GetZone(d.SourceZoneId).GetAllEntities().Any(e => e.ID == a.ID), id + " silently replenished");
            }
        }

        // H10: the request has a real action surface; acceptance records actionable
        // locations, neutral rereads preserve notes, and release is unpaid.
        [Test]
        public void NativeAcceptanceRecordsUsefulNotesAndReleaseDoesNotPayOrDeleteSource()
        {
            var d = RegionalSituations.Find("sumphold-oil"); var b = Bind(d.Id);
            StandBy(b.zone, b.recipient); Assert.IsEmpty(RegionalSituationNotes.Read(player));
            var actions = new InventoryActionList(); var gather = GameEvent.New("GetInventoryActions");
            gather.SetParameter("Actor", player); gather.SetParameter("Zone", b.zone); gather.SetParameter("Actions", actions);
            b.recipient.FireEventAndRelease(gather);
            Assert.IsTrue(actions.Actions.Any(a => a.Command == "RegionalRequest:read"));
            Assert.IsTrue(InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(b.recipient, "RegionalRequest:read"), player, b.zone).Success);
            Assert.IsFalse(b.request.Accepted);Assert.IsEmpty(RegionalSituationNotes.Read(player));
            Assert.IsTrue(actions.Actions.Any(a=>a.Command=="RegionalRequest:accept"));
            Assert.IsTrue(b.request.TryAct(player,b.zone,"accept"));
            Assert.IsTrue(b.request.Accepted);
            var notes = RegionalSituationNotes.Read(player); Assert.AreEqual(1, notes.Count);
            string text = string.Join(" ", notes).Replace(" ", "");
            StringAssert.Contains("16,6", text); StringAssert.Contains("15,6", text);
            Assert.IsTrue(b.request.TryAct(player, b.zone, "read")); CollectionAssert.AreEqual(notes,RegionalSituationNotes.Read(player));
            int money = TradeSystem.GetDrams(player);
            Assert.IsTrue(b.request.TryAct(player, b.zone, "release"));
            Assert.IsFalse(b.request.Accepted); Assert.IsFalse(b.request.Completed);
            Assert.AreEqual(money, TradeSystem.GetDrams(player));
            Assert.NotNull(Source(manager.GetZone(d.SourceZoneId), d.Id));
            Assert.AreEqual(1, RegionalSituationNotes.Read(player).Count, "A released request retains its truthful historical note.");
        }

        // H11: a live-map POI veto must win over static coordinate eligibility.
        // Normal generation and services continue, but no cargo is promised.
        [Test]
        public void ReplacedSourcePoiCannotAdvertiseOrInstallRecovery()
        {
            var d = RegionalSituations.Find("sumphold-oil"); var p = WorldMap.FromZoneID(d.SourceZoneId);
            manager.WorldMap.SetPOI(p.x, p.y, new PointOfInterest(POIType.Village, "Counterfixture village"));
            var b = Bind(d.Id); StandBy(b.zone, b.recipient);
            Assert.IsFalse(b.request.TryAct(player, b.zone, "accept"));
            Assert.IsFalse(b.request.Accepted); Assert.IsFalse(b.request.Completed);
            Assert.IsFalse(manager.GetZone(d.SourceZoneId).GetAllEntities().Any(e => e.ID == RegionalSituations.SourceId(d, Seed)));
            Assert.IsEmpty(RegionalSituationNotes.Read(player));
        }

        // H12: the feature is five authored bindings, not population sprinkled
        // into every village or into the original western expedition.
        [Test]
        public void UnboundTownAndStartingFieldRemainQuiet()
        {
            foreach (string id in new[] { "Overworld.14.9.0", "Overworld.2.6.0" })
            {
                var z = manager.GetZone(id);
                Assert.IsFalse(z.GetAllEntities().Any(e => e.HasPart<RegionalRequestPart>()), id);
                foreach (var d in RegionalSituations.Definitions)
                    Assert.IsFalse(z.GetAllEntities().Any(e => e.ID == RegionalSituations.SourceId(d, Seed)), id);
                if (id == "Overworld.2.6.0")
                    Assert.AreEqual(1, z.GetAllEntities().Count(e => e.ID == MorrowfastExpedition.CacheId));
            }
            Assert.IsEmpty(RegionalSituationNotes.Read(player));
        }

        private (Zone zone, Entity recipient, RegionalRequestPart request) Bind(string id)
        {
            var d = RegionalSituations.Find(id); Assert.NotNull(d, id);
            var z = manager.GetZone(d.RecipientZoneId);
            var matches = z.GetAllEntities().Where(e => e.GetPart<RegionalRequestPart>()?.DefinitionId == id).ToArray();
            Assert.AreEqual(1, matches.Length, "Native recipient binding " + id);
            return (z, matches[0], matches[0].GetPart<RegionalRequestPart>());
        }

        private static Entity Source(Zone zone, string id)
        {
            string sourceId = RegionalSituations.SourceId(RegionalSituations.Find(id), Seed);
            var owners = zone.GetAllEntities().Where(e => e.ID == sourceId).ToArray();
            Assert.AreEqual(1, owners.Length, "Actual generated primary source for " + id);
            return owners[0];
        }

        private void StandBy(Zone zone, Entity owner)
        {
            var at = zone.GetEntityCell(owner); Assert.NotNull(at);
            var standing = CinderholdCompositionTests.Neighbors(at.X, at.Y)
                .Select(p => zone.GetCell(p.x, p.y)).FirstOrDefault(c => c != null && !c.BlocksMovement());
            Assert.NotNull(standing, "Native recipient needs a standing frontage.");
            MovePlayer(zone, standing.X, standing.Y);
        }

        private void MovePlayer(Zone zone, int x, int y)
        {
            if (playerZone != null) Assert.IsTrue(playerZone.RemoveEntity(player));
            Assert.IsTrue(zone.AddEntity(player, x, y)); playerZone = zone;
            manager.SetActiveZone(zone); SettlementRuntime.ActiveZone = zone;
        }

        private void Carry(string blueprint, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var item = factory.CreateEntity(blueprint); Assert.NotNull(item, blueprint);
                Assert.IsTrue(player.GetPart<InventoryPart>().AddObject(item), blueprint);
            }
        }

        private static int Units(Entity owner, string blueprint) => owner.GetPart<InventoryPart>().Objects
            .Where(e => e.BlueprintName == blueprint && ReferenceEquals(e.GetPart<PhysicsPart>()?.InInventory, owner))
            .Sum(e => Math.Max(0, e.GetPart<StackerPart>()?.StackCount ?? 1));
    }
}
