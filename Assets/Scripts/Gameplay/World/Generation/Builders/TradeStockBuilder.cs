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

        private static readonly string[] TradeGoods =
        {
            "Dagger", "ShortSword", "LongSword", "Mace", "Spear", "Hatchet",
            "Cudgel", "Buckler", "LeatherArmor", "ChainMail", "IronHelmet",
            "LeatherBoots", "LeatherGloves", "Cloak",
            "HealingTonic", "SpeedTonic", "StrengthTonic",
            "Starapple", "Mushroom", "DriedMeat"
        };

        public bool BuildZone(Zone zone, EntityFactory factory, System.Random rng)
        {
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
                for (int i = 0; i < itemCount; i++)
                {
                    string blueprint = TradeGoods[rng.Next(TradeGoods.Length)];
                    var item = factory.CreateEntity(blueprint);
                    if (item != null)
                        inv.AddObject(item);
                }
            }

            return true;
        }
    }
}
