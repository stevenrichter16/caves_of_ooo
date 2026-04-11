using System.Collections.Generic;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// Stocks friendly NPC inventories with random trade goods after population.
    /// Runs after PopulationBuilder to find spawned Villager-faction creatures
    /// and give each one 2-5 random items from the trade goods pool.
    /// Priority: 4100 (after PopulationBuilder at 4000).
    /// </summary>
    public class TradeStockBuilder : IZoneBuilder
    {
        public string Name => "TradeStockBuilder";
        public int Priority => 4100;
        private readonly SettlementManager _settlementManager;

        private static readonly string[] TradeGoods =
        {
            "Dagger", "ShortSword", "LongSword", "Mace", "Spear", "Hatchet",
            "Cudgel", "Buckler", "LeatherArmor", "ChainMail", "IronHelmet",
            "LeatherBoots", "LeatherGloves", "Cloak",
            "HealingTonic", "SpeedTonic", "StrengthTonic",
            "Starapple", "Mushroom", "DriedMeat"
        };

        private static readonly string[] FouledWellTradeGoods =
        {
            "Dagger", "ShortSword", "Mace", "Spear", "Hatchet",
            "Cudgel", "Buckler", "LeatherArmor", "Cloak",
            "HealingTonic", "Mushroom", "SilverSand"
        };

        private static readonly string[] ImprovedWellTradeGoods =
        {
            "Dagger", "ShortSword", "LongSword", "Mace", "Spear", "Hatchet",
            "Buckler", "LeatherArmor", "ChainMail", "Cloak",
            "HealingTonic", "SpeedTonic", "StrengthTonic",
            "Starapple", "Mushroom", "DriedMeat", "Starapple", "DriedMeat"
        };

        public TradeStockBuilder(SettlementManager settlementManager = null)
        {
            _settlementManager = settlementManager;
        }

        public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng)
        {
            string[] goods = GetTradeGoodsForZone(zone.ZoneID);
            RepairableSiteState site = _settlementManager?.GetSite(zone.ZoneID, SettlementSiteDefinitions.MainWellSiteId);

            var creatures = zone.GetEntitiesWithTag("Creature");
            foreach (var creature in creatures)
            {
                // Only stock friendly NPCs (Villagers faction) that have inventories
                string faction;
                if (!creature.Tags.TryGetValue("Faction", out faction)) continue;
                if (faction != "Villagers") continue;

                var inv = creature.GetPart<InventoryPart>();
                if (inv == null) continue;

                // Give 2-5 random items
                int itemCount = rng.Next(2, 6);
                if (creature.BlueprintName == "Merchant" && goods == ImprovedWellTradeGoods)
                    itemCount += 1;

                for (int i = 0; i < itemCount; i++)
                {
                    string blueprint = goods[rng.Next(goods.Length)];
                    var item = TryCreateEntity(factory, blueprint);
                    if (item != null)
                        inv.AddObject(item);
                }

                if (creature.BlueprintName == "Merchant"
                    && site != null
                    && (site.Stage == RepairStage.Fouled || site.Stage == RepairStage.TemporarilyPurified)
                    && !HasItem(inv, SettlementRepairDefinitions.SilverSandBlueprint))
                {
                    var silverSand = TryCreateEntity(factory, SettlementRepairDefinitions.SilverSandBlueprint);
                    if (silverSand != null)
                        inv.AddObject(silverSand);
                }
            }

            return true;
        }

        private string[] GetTradeGoodsForZone(string zoneId)
        {
            RepairableSiteState site = _settlementManager?.GetSite(zoneId, SettlementSiteDefinitions.MainWellSiteId);
            if (site == null)
                return TradeGoods;

            switch (site.Stage)
            {
                case RepairStage.Fouled:
                case RepairStage.TemporarilyPurified:
                    return FouledWellTradeGoods;
                case RepairStage.ImprovedWithCaretaker:
                    return ImprovedWellTradeGoods;
                default:
                    return TradeGoods;
            }
        }

        private static bool HasItem(InventoryPart inventory, string blueprint)
        {
            for (int i = 0; i < inventory.Objects.Count; i++)
            {
                if (inventory.Objects[i].BlueprintName == blueprint)
                    return true;
            }

            return false;
        }

        private static Entity TryCreateEntity(EntityFactory factory, string blueprint)
        {
            if (factory == null || string.IsNullOrEmpty(blueprint) || !factory.Blueprints.ContainsKey(blueprint))
                return null;

            return factory.CreateEntity(blueprint);
        }
    }
}
