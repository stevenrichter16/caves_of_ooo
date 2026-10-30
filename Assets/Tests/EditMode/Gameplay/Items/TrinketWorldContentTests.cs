using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Skills;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace CavesOfOoo.Tests
{
    public sealed class TrinketWorldContentTests
    {
        EntityFactory factory, previousTraderFactory, previousContainerFactory;
        Random previousRng;

        [SetUp]
        public void SetUp()
        {
            factory = new EntityFactory();
            factory.LoadBlueprints(File.ReadAllText(Path.Combine(Application.dataPath,
                "Resources/Content/Blueprints/Objects.json")));
            LootTableRegistry.InitializeFromJsonSources(Directory.GetFiles(Path.Combine(Application.dataPath,
                "Resources/Content/Data/Loot"), "*.json").OrderBy(p => p).Select(File.ReadAllText));
            SkillRegistry.ResetForTests();
            foreach (string path in Directory.GetFiles(Path.Combine(Application.dataPath,
                "Resources/Content/Data/Skills"), "*.json"))
                SkillRegistry.LoadFromJson(File.ReadAllText(path), path);
            previousTraderFactory = TraderPart.Factory;
            previousContainerFactory = ContainerPlacementService.Factory;
            previousRng = TraderPart.Rng;
            TraderPart.Factory = ContainerPlacementService.Factory = factory;
        }

        [TearDown]
        public void TearDown()
        {
            TraderPart.Factory = previousTraderFactory;
            ContainerPlacementService.Factory = previousContainerFactory;
            TraderPart.Rng = previousRng;
            LootTableRegistry.ResetForTests();
            SkillRegistry.ResetForTests();
        }

        [Test]
        public void EntireAuthoredCollectionGrantsOnlyPositiveRealSkills()
        {
            var trinkets = factory.Blueprints.Where(pair => pair.Key != "TrinketItem" && pair.Value.Tags.ContainsKey("Trinket"))
                .Select(pair => factory.CreateEntity(pair.Key)).ToArray();
            Assert.GreaterOrEqual(trinkets.Length, 8);
            foreach (var trinket in trinkets)
            {
                var equipment = trinket.GetPart<EquippablePart>();
                Assert.NotNull(equipment, trinket.BlueprintName);
                Assert.AreEqual(TrinketSlots.SlotType, equipment.Slot);
                Assert.IsEmpty(equipment.EquipBonuses ?? "", trinket.BlueprintName + " must not change attributes");
                var bonuses = StatModifiers.Parse(equipment.EquipSkillBonuses);
                Assert.IsNotEmpty(bonuses, trinket.BlueprintName);
                foreach (var bonus in bonuses)
                {
                    Assert.Greater(bonus.Amount, 0, trinket.BlueprintName);
                    Assert.True(SkillRegistry.TryGetSkillByClass(bonus.StatName, out _)
                        || SkillRegistry.TryGetPowerByClass(bonus.StatName, out _), bonus.StatName);
                    Assert.LessOrEqual(bonus.Amount, SkillRanks.Maximum(bonus.StatName));
                }
            }
            Assert.IsEmpty(LootTableRegistry.Validate(name => factory.Blueprints.ContainsKey(name)));
        }

        [TestCase("QuarryToothTrinket", "OreCacheT1", "OreCache")]
        [TestCase("TonguelessBellTrinket", "StrongBoxT1", "StrongBox")]
        [TestCase("DuelistsKnotTrinket", "HollowLogT1", "HollowLog")]
        [TestCase("SlingersWishboneTrinket", "HunterCacheT1", "Chest")]
        [TestCase("SplitHaftTokenTrinket", "WorkshopCacheT2", "Chest")]
        [TestCase("GlassEmberTrinket", "CultCacheT2", "Chest")]
        [TestCase("RainwardBeadTrinket", "BasketT1", "WovenBasket")]
        [TestCase("PalimpsestSealTrinket", "BookshelfT1", "Bookshelf")]
        [TestCase("DuelistsKnotTrinket", "ArmorerStock", "Armorer")]
        [TestCase("SlingersWishboneTrinket", "WardenStock", "Warden")]
        [TestCase("SplitHaftTokenTrinket", "WeaponsmithStock", "Weaponsmith")]
        [TestCase("GlassEmberTrinket", "DrifterStock", "GlassblownDrifter")]
        [TestCase("RainwardBeadTrinket", "WellKeeperStock", "WellKeeper")]
        [TestCase("PalimpsestSealTrinket", "ScribeStock", "Scribe")]
        public void RealSourceStocksAUsableTrinketAndDisabledEntryRemovesIt(string blueprint, string tableName, string sourceBlueprint)
        {
            var table = LootTableRegistry.Get(tableName);
            var entry = table.Entries.Single(e => e.Blueprint == blueprint);
            int seed = Enumerable.Range(0, 4096).First(s => LootTableRegistry.Roll(tableName, new Random(s)).Contains(blueprint));
            var source = Stock(sourceBlueprint, tableName, seed);
            var item = Contents(source).Single(e => e.BlueprintName == blueprint);
            Assert.AreEqual(TrinketSlots.SlotType, item.GetPart<EquippablePart>().Slot);
            Assert.IsNotEmpty(item.GetPart<EquippablePart>().EquipSkillBonuses);
            var loaded = PartRoundTripHelper.RoundTripEntityViaTokenGraph(source);
            var loadedItem = Contents(loaded).Single(e => e.ID == item.ID);
            Assert.AreEqual(item.GetPart<EquippablePart>().EquipSkillBonuses,
                loadedItem.GetPart<EquippablePart>().EquipSkillBonuses);

            if (table.PickOne) entry.Weight = 0; else entry.Chance = 0;
            Assert.False(Contents(Stock(sourceBlueprint, tableName, seed)).Any(e => e.BlueprintName == blueprint),
                "A trinket must come from its actual loot entry, not a test-only or unconditional injection.");
        }

        [TestCase(BiomeType.Stump, ContainerPlacementService.ZoneKind.Underground, "QuarryToothTrinket")]
        [TestCase(BiomeType.Spread, ContainerPlacementService.ZoneKind.Wilderness, "TonguelessBellTrinket")]
        [TestCase(BiomeType.Sodden, ContainerPlacementService.ZoneKind.Wilderness, "DuelistsKnotTrinket")]
        [TestCase(BiomeType.Grovelands, ContainerPlacementService.ZoneKind.Wilderness, "RainwardBeadTrinket")]
        [TestCase(BiomeType.Ruins, ContainerPlacementService.ZoneKind.Wilderness, "PalimpsestSealTrinket")]
        public void NormalBiomeContainerGenerationCanPlaceTrinkets(BiomeType biome,
            ContainerPlacementService.ZoneKind kind, string blueprint)
        {
            for (int seed = 0; seed < 512; seed++)
            {
                var zone = new Zone("trinket-world-" + seed);
                var placed = new List<Entity>();
                ContainerPlacementService.Populate(zone, biome, 1, kind, new Random(seed), placed);
                var owner = placed.FirstOrDefault(e => Contents(e).Any(i => i.BlueprintName == blueprint));
                if (owner == null) continue;
                Assert.NotNull(zone.GetEntityCell(owner));
                Assert.True(zone.GetEntityCell(owner).Objects.Contains(owner));
                return;
            }
            Assert.Fail(blueprint + " was never placed by the production biome container path.");
        }

        Entity Stock(string sourceBlueprint, string tableName, int seed)
        {
            TraderPart.Rng = new Random(seed);
            var source = factory.CreateEntity(sourceBlueprint);
            if (source.HasPart<TraderPart>()) Assert.AreEqual(tableName, source.GetPart<TraderPart>().StockTable);
            else LootStocker.StockContainer(source, tableName, factory, new Random(seed));
            return source;
        }

        static IEnumerable<Entity> Contents(Entity source) => source.GetPart<ContainerPart>()?.Contents
            ?? (IEnumerable<Entity>)source.GetPart<InventoryPart>()?.Objects ?? Array.Empty<Entity>();
    }
}
