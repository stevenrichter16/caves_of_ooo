using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Data;
using NUnit.Framework;
using UnityEngine;

namespace CavesOfOoo.Tests
{
    /// <summary>Real content through conversation, trade and inventory commands.
    /// Crop time is advanced through the world's actor-gated event; native input
    /// and renderer evidence are separate from this core content fixture.</summary>
    public sealed class SpreadEverydayResidentTests
    {
        DensityLootTestScope scope;
        EntityFactory Factory => scope.Factory;
        Zone zone;
        Entity player;
        Entity cropWorld;
        Dictionary<string, ConversationData> savedConversations;
        bool conversationsLoaded;
        object borrowedOffers, borrowedRevision;
        static readonly BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic;
        static readonly FieldInfo OfferState = typeof(SpreadDiscoveryReports).GetField("offers", Static);
        static readonly FieldInfo OfferRevision = typeof(SpreadDiscoveryReports).GetField("revision", Static);
        InventoryPart Inventory => player.GetPart<InventoryPart>();

        [SetUp] public void Setup()
        {
            // Capture before the fixture scope can replace borrowed conversation state.
            borrowedOffers = OfferState.GetValue(null); borrowedRevision = OfferRevision.GetValue(null);
            scope = new DensityLootTestScope(); scope.Seed(64);
            PlayerReputation.Reset(); // Native fixture scope restores the borrowed reputation after each case.
            SeedPart.Factory = CropSystem.Factory = MaterialReactionResolver.Factory = Factory;
            savedConversations = new Dictionary<string, ConversationData>((Dictionary<string, ConversationData>)
                typeof(ConversationLoader).GetField("_cache", Static).GetValue(null));
            conversationsLoaded = (bool)typeof(ConversationLoader).GetField("_loaded", Static).GetValue(null);
            foreach (string file in Directory.GetFiles(Path.Combine(Application.dataPath, "Resources/Content/Conversations"), "*.json", SearchOption.AllDirectories))
                ConversationLoader.LoadFromJson(File.ReadAllText(file), Path.GetFileName(file));
            zone = new Zone("Overworld.11.8.0"); player = Factory.CreateEntity("Player");
            Assert.True(zone.AddEntity(player, 10, 10));
            SettlementRuntime.ActiveZone = zone;
            cropWorld = new Entity { BlueprintName = "ResidentCropTestWorld" };
            cropWorld.AddPart(new CropSystemPart());
        }
        [TearDown] public void Cleanup()
        {
            ConversationManager.EndConversation();
            var cache = (Dictionary<string, ConversationData>)typeof(ConversationLoader).GetField("_cache", Static).GetValue(null);
            cache.Clear(); foreach (var entry in savedConversations) cache[entry.Key] = entry.Value;
            typeof(ConversationLoader).GetField("_loaded", Static).SetValue(null, conversationsLoaded);
            try { scope?.Dispose(); }
            finally
            {
                OfferState.SetValue(null, borrowedOffers);
                OfferRevision.SetValue(null, borrowedRevision);
            }
        }
        Entity Place(string blueprint, int x = 11, int y = 10)
        {
            var entity = Factory.CreateEntity(blueprint); Assert.NotNull(entity, blueprint);
            Assert.True(zone.AddEntity(entity, x, y), blueprint); return entity;
        }
        static int Units(Entity actor, string blueprint) => actor.GetPart<InventoryPart>().Objects
            .Where(e => e.BlueprintName == blueprint).Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);
        static Entity Stock(Entity actor, string blueprint) => actor.GetPart<InventoryPart>().Objects.FirstOrDefault(e => e.BlueprintName == blueprint);
        bool Action(Entity item, string command) => InventorySystem.ExecuteCommand(new PerformInventoryActionCommand(item, command), player, zone).Success;
        void Tick(Entity actor, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var tick = GameEvent.New("TickEnd"); tick.SetParameter("Actor", actor); cropWorld.FireEventAndRelease(tick);
                if (actor == player) player.FireEventAndRelease(GameEvent.New("EndTurn"));
            }
        }
        bool Rain()
        {
            var command = GameEvent.New("CommandConjureRain");
            command.SetParameter("Zone", zone); command.SetParameter("SourceCell", zone.GetEntityCell(player));
            player.FireEvent(command); bool handled = command.Handled; command.Release(); return handled;
        }
        void OpenTrade(Entity resident)
        {
            Assert.True(Action(resident, "Chat")); Assert.True(ConversationManager.IsActive);
            Assert.AreSame(resident, ConversationManager.Speaker);
            int choice = ConversationManager.VisibleChoices.ToList().FindIndex(c => c.Actions?.Any(a => a.Key == "StartTrade") == true);
            Assert.GreaterOrEqual(choice, 0, "The actual authored dialogue must reach its normal trade action.");
            ConversationManager.SelectChoice(choice);
            Assert.AreSame(resident, ConversationManager.PendingTradePartner);
            Assert.False(ConversationManager.IsActive);
        }
        Entity Buy(Entity resident, string blueprint)
        {
            var item = Stock(resident, blueprint); Assert.NotNull(item, blueprint);
            int count = item.GetPart<StackerPart>()?.StackCount ?? 1;
            int before = Units(player, blueprint), shelf = Units(resident, blueprint);
            Assert.True(TradeSystem.BuyFromTrader(player, resident, item, out var reason), reason);
            Assert.AreEqual(before + count, Units(player, blueprint)); Assert.AreEqual(shelf - count, Units(resident, blueprint));
            return Stock(player, blueprint);
        }

        [TestCase("SpreadSeedKeeper", "SpreadSeedKeeper_1", 40)]
        [TestCase("SpreadWaysideCook", "SpreadWaysideCook_1", 35)]
        public void OriginalFieldResidentsHaveActualRoleStockAndSmallPurses(string blueprint, string conversation, int purse)
        {
            var resident = Place(blueprint); Assert.AreEqual("Villagers", resident.GetTag("Faction"));
            Assert.True(resident.GetPart<BrainPart>().Staying); Assert.False(resident.GetPart<BrainPart>().Wanders);
            Assert.False(FactionManager.IsHostile(resident, player));
            Assert.AreEqual(conversation, resident.GetPart<ConversationPart>().ConversationID);
            Assert.NotNull(ConversationLoader.Get(conversation)); Assert.AreEqual(purse, TradeSystem.GetDrams(resident));
            var trader = resident.GetPart<TraderPart>(); Assert.NotNull(trader); Assert.IsNotEmpty(trader.StockTable);
            Assert.AreEqual(trader.StockTable, resident.GetProperty(TraderRestockSystem.ShopStockTableProp));
            string[] expected = blueprint == "SpreadSeedKeeper"
                ? new[] { "CandyCarrotSeed", "EmberwheatSeed", "WateringGrimoire" }
                : new[] { "RawMeat", "Mushroom", "Emberwheat", "DriedMeat", "ToastedEmberwheat" };
            foreach (string id in expected) Assert.Greater(Units(resident, id), 0, id);
            Assert.AreEqual(5, resident.GetPart<InventoryPart>().Objects.Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1));
            Assert.False(resident.GetPart<InventoryPart>().Objects.Any(e => e.HasPart<MeleeWeaponPart>() || e.HasPart<ArmorPart>()), "The shelf is role stock, not inherited merchant equipment.");
        }

        [TestCase("SpreadSeedKeeper", "CandyCarrotSeed")]
        [TestCase("SpreadWaysideCook", "Emberwheat")]
        public void ConversationTradeUsesActualAffordableStockAndCannotRepeatBoughtOrUnfundedItem(string blueprint, string product)
        {
            var resident = Place(blueprint); OpenTrade(resident);
            var item = Stock(resident, product); Assert.NotNull(item);
            int price = TradeSystem.GetBuyPrice(item, TradeSystem.GetTradePerformance(player), resident);
            int opening = TradeSystem.GetDrams(player); Assert.That(price, Is.InRange(1, opening), "Normal starting purse can buy this complete carried stack.");
            TradeSystem.SetDrams(player, 0);
            Assert.False(TradeSystem.BuyFromTrader(player, resident, item)); Assert.True(resident.GetPart<InventoryPart>().CanConsumeOne(item));
            Assert.AreEqual(0, Units(player, product));
            TradeSystem.SetDrams(player, opening);
            int sellerBefore = TradeSystem.GetDrams(resident); Buy(resident, product);
            Assert.AreEqual(opening - price, TradeSystem.GetDrams(player)); Assert.AreEqual(sellerBefore + price, TradeSystem.GetDrams(resident));
            Assert.False(TradeSystem.BuyFromTrader(player, resident, item)); Assert.AreEqual(opening - price, TradeSystem.GetDrams(player));
        }

        [TestCase("CandyCarrotSeed", "CandyCarrotCrop", "CandyCarrot")]
        [TestCase("EmberwheatSeed", "EmberwheatCrop", "Emberwheat")]
        public void PurchasedSeedPlantsWatersAndProducesFiniteFoodOnlyDuringPlayerRounds(string seedId, string cropId, string foodId)
        {
            var keeper = Place("SpreadSeedKeeper"); OpenTrade(keeper); var seed = Buy(keeper, seedId);
            int seeds = Units(player, seedId);
            Assert.False(Action(seed, "PlantSeed")); Assert.AreEqual(seeds, Units(player, seedId), "Bare hard ground refuses without spending seed.");
            Place("Grass", 10, 10); Assert.True(Action(seed, "PlantSeed")); Assert.AreEqual(seeds - 1, Units(player, seedId));
            Assert.False(Action(seed, "PlantSeed")); Assert.AreEqual(seeds - 1, Units(player, seedId), "An occupied crop cell refuses a second planting.");
            var cropEntity = zone.GetEntityCell(player).Objects.Single(e => e.BlueprintName == cropId); var crop = cropEntity.GetPart<CropPart>();
            Tick(player, 100); Assert.AreEqual(0, crop.TicksInStage); Assert.AreEqual(0, crop.GrowthStage);
            // The normal new-game starter kit includes this book; the new purchase is the seed.
            var book = Factory.CreateEntity("WateringGrimoire"); Assert.True(Inventory.AddObject(book));
            Assert.True(Action(book, "ReadGrimoire"));
            Assert.True(Rain()); Assert.AreEqual(40, crop.MoistureTicks);
            Tick(keeper, 100); Assert.AreEqual(40, crop.MoistureTicks); Assert.AreEqual(0, crop.TicksInStage, "NPC turns cannot accelerate garden growth.");
            Assert.False(Rain(), "The ordinary spell cooldown remains enforced.");
            Tick(player, 40);
            if (foodId == "Emberwheat")
            {
                Assert.NotNull(zone.GetEntityCell(cropEntity)); Assert.AreEqual(1, crop.GrowthStage); Assert.AreEqual(5, crop.TicksInStage);
                Assert.AreEqual(0, crop.MoistureTicks); Tick(player, 20); Assert.AreEqual(5, crop.TicksInStage, "Dry growth pauses.");
                Assert.True(Rain()); Tick(player, 30);
            }
            Assert.Null(zone.GetEntityCell(cropEntity));
            var produce = zone.GetEntityCell(player).Objects.Where(e => e.BlueprintName == foodId).ToArray(); Assert.AreEqual(2, produce.Length);
            foreach (var item in produce) Assert.True(InventorySystem.ExecuteCommand(new PickupCommand(item), player, zone).Success);
            Assert.AreEqual(2, Units(player, foodId)); Tick(player, 100); Assert.AreEqual(2, Units(player, foodId));
            Assert.False(zone.GetEntityCell(player).Objects.Any(e => e.BlueprintName == cropId || e.BlueprintName == foodId));
        }

        [Test]
        public void GeneratedWaysideOvenCooksBoughtFoodWhileRemoteAttemptPreservesIt()
        {
            var manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            zone.RemoveEntity(player); zone = manager.GetZone("Overworld.12.11.0"); SettlementRuntime.ActiveZone = zone;
            var cook = zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "SpreadWaysideCook");
            var oven = zone.GetReadOnlyEntities().Single(e => e.BlueprintName == "Oven" && e.HasPart<CampfirePart>());
            Assert.False(oven.GetPart<CampfirePart>().AllowRest); Assert.False(oven.GetPart<CampfirePart>().FiniteCooking);
            PlacePlayerBy(cook); OpenTrade(cook); var raw = Buy(cook, "Emberwheat"); int units = Units(player, "Emberwheat");
            var far = Enumerable.Range(0, Zone.Width * Zone.Height).Select(i => zone.GetCell(i % Zone.Width, i / Zone.Width)).First(c => zone.CanPlaceFootprint(player, c.X, c.Y) &&
                !zone.GetReadOnlyEntities().Any(e => e.HasPart<CampfirePart>() && Distance(c, zone.GetEntityCell(e)) <= 1));
            Assert.True(zone.MoveEntity(player, far.X, far.Y));
            Assert.False(Action(raw, "Cook")); Assert.AreEqual(units, Units(player, "Emberwheat")); Assert.AreEqual(0, Units(player, "ToastedEmberwheat"));
            PlacePlayerBy(oven); Assert.True(Action(raw, "Cook")); Assert.AreEqual(0, Units(player, "Emberwheat")); Assert.AreEqual(units, Units(player, "ToastedEmberwheat"));
            Assert.False(Action(raw, "Cook")); Assert.AreEqual(units, Units(player, "ToastedEmberwheat"));
            int before = Units(player, "ToastedEmberwheat"); Assert.True(Action(Stock(player, "ToastedEmberwheat"), "Eat")); Assert.AreEqual(before - 1, Units(player, "ToastedEmberwheat"));
        }

        [TestCase("Overworld.11.8.0", "SpreadSeedKeeper", "CandyCarrotSeed")]
        [TestCase("Overworld.12.11.0", "SpreadWaysideCook", "Emberwheat")]
        public void GeneratedResidentsAndBoughtStockRoundTripWithoutAccessRefills(string zoneId, string blueprint, string product)
        {
            var manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            zone.RemoveEntity(player); zone = manager.GetZone(zoneId); SettlementRuntime.ActiveZone = zone;
            Assert.AreEqual(2, manager.Exploration.DispositionFor(zoneId));
            var resident = zone.GetReadOnlyEntities().Single(e => e.BlueprintName == blueprint); PlacePlayerBy(resident); OpenTrade(resident); Buy(resident, product);
            string id = resident.ID; int purse = TradeSystem.GetDrams(resident), owned = Units(player, product), remaining = Units(resident, product);
            manager.SetActiveZone(zone); var state = GameSessionState.Capture("spread-residents", "core-only-fixture", manager, null, player);
            GameSessionState loaded;
            using (var stream = new MemoryStream()) { state.Save(new SaveWriter(stream)); stream.Position = 0; loaded = GameSessionState.Load(new SaveReader(stream, Factory)); }
            var returned = loaded.ZoneManager.GetZone(zoneId); var restored = returned.GetReadOnlyEntities().Single(e => e.ID == id);
            Assert.AreEqual(blueprint, restored.BlueprintName); Assert.AreEqual(purse, TradeSystem.GetDrams(restored));
            Assert.AreEqual(owned, Units(loaded.Player, product)); Assert.AreEqual(remaining, Units(restored, product));
            loaded.ZoneManager.UnloadZone(zoneId); Assert.AreSame(returned, loaded.ZoneManager.GetZone(zoneId));
            Assert.AreEqual(1, returned.GetReadOnlyEntities().Count(e => e.BlueprintName == blueprint)); Assert.AreEqual(remaining, Units(restored, product));
        }

        [Test]
        public void CachedExistingNorthPlotAddressDoesNotAcquireResidentsOrNewCropsOnAccess()
        {
            var manager = OverworldZoneManager.CreateDetached(Factory, 64, true);
            var old = new Zone("Overworld.11.8.0"); manager.SetActiveZone(old);
            Assert.AreSame(old, manager.GetZone(old.ZoneID)); Assert.IsEmpty(old.GetReadOnlyEntities());
        }
        static int Distance(Cell a, Cell b) => b == null ? int.MaxValue : Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
        void PlacePlayerBy(Entity target)
        {
            var at = zone.GetEntityCell(target); Assert.NotNull(at);
            var approach = new[] { (at.X - 1, at.Y), (at.X + 1, at.Y), (at.X, at.Y - 1), (at.X, at.Y + 1) }
                .Select(p => zone.GetCell(p.Item1, p.Item2)).FirstOrDefault(c => c != null && zone.CanPlaceFootprint(player, c.X, c.Y)
                    && !c.Objects.Any(e => e.HasTag("Creature") || e.HasPart<TriggerOnStepPart>()));
            Assert.NotNull(approach);
            Assert.True(zone.GetEntityCell(player) != null ? zone.MoveEntity(player, approach.X, approach.Y) : zone.AddEntity(player, approach.X, approach.Y));
        }
    }
}
