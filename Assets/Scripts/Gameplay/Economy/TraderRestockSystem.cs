using System.Collections.Generic;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// ALPHA-READINESS item 7 SM3 — periodic trader dram restock. Once
    /// the player sold a village out of drams, the trader could NEVER
    /// buy again: blueprint Drams were seeded at spawn and only ever
    /// drained. On each zone entry (InputHandler.HandleZoneTransition),
    /// villager-faction entities and explicitly authored TraderPart merchants
    /// that carry a Drams property and whose last restock is more than
    /// <see cref="RestockIntervalTurns"/> old are topped back up to
    /// <see cref="DramsFloor"/>. Other factions require a nonempty declared
    /// TraderPart.StockTable; a stock property alone does not grant eligibility.
    ///
    /// <para>Save-safe: the per-trader stamp lives in the entity's int
    /// properties (round-trip through the save graph, like Drams
    /// itself). Deliberately a FLOOR top-up, not a blueprint-value
    /// restore — resolving each trader's original blueprint value would
    /// need factory plumbing through the input layer; the alpha need is
    /// "traders can buy again", and rich traders are unaffected
    /// (top-up only applies below the floor).</para>
    /// </summary>
    public static class TraderRestockSystem
    {
        public const int RestockIntervalTurns = 300;
        public const int DramsFloor = 100;
        public const string LastRestockProp = "LastRestockTurn";

        /// <summary>STARTING TOWN — shop-shelf restock threshold: a
        /// keeper with fewer positive units from their stock table than this
        /// receives one full authored roll on the restock tick. Sold unrelated
        /// items are retained but do not block useful stock.</summary>
        public const int ShelfLowWaterMark = 3;
        public const string ShopStockTableProp = "ShopStockTable";

        /// <summary>
        /// STARTING TOWN — factory for shelf refills, wired by
        /// GameBootstrap (CorpsePart.Factory convention). Null = the
        /// original drams-only restock (documented A6 behavior).
        /// </summary>
        public static CavesOfOoo.Data.EntityFactory Factory;

        /// <summary>Restock eligible traders in the zone. Returns the
        /// number of purse top-ups plus shelf refill attempts; one trader
        /// may contribute both.</summary>
        public static int RestockZone(Zone zone, int currentTurn)
        {
            if (zone == null) return 0;

            int restocked = 0;
            var creatures = zone.GetEntitiesWithTag("Creature");
            for (int i = 0; i < creatures.Count; i++)
            {
                var e = creatures[i];
                string faction;
                // Authored non-village merchants seed their purse and stock
                // through TraderPart too. Preserve legacy Villagers, while a
                // forged stock property alone cannot opt another creature in.
                var declaredTrader = e.GetPart<TraderPart>();
                bool authoredTrader = declaredTrader != null && !string.IsNullOrEmpty(declaredTrader.StockTable);
                if ((!e.Tags.TryGetValue("Faction", out faction) || faction != "Villagers") && !authoredTrader)
                    continue;
                // "Is a trader" = carries the Drams property at all
                // (blueprint-seeded); ordinary villagers have none.
                int drams = e.GetIntProperty(TradeSystem.CURRENCY_PROP, -1);
                if (drams < 0)
                    continue;

                int last = e.GetIntProperty(LastRestockProp, int.MinValue);
                if (last != int.MinValue && (long)currentTurn - last <= RestockIntervalTurns)
                    continue;

                if (drams < DramsFloor)
                {
                    TradeSystem.SetDrams(e, DramsFloor);
                    restocked++;
                }

                // STARTING TOWN: a shopkeeper whose shelf has run low
                // re-rolls their themed stock table (Factory-gated —
                // contexts without the factory keep drams-only).
                string stockTable = e.GetProperty(ShopStockTableProp);
                if (Factory != null && !string.IsNullOrEmpty(stockTable))
                {
                    var inv = e.GetPart<InventoryPart>();
                    if (inv != null && CountStockUnits(inv, stockTable) < ShelfLowWaterMark)
                    {
                        var rng = new System.Random(
                            unchecked(currentTurn * 31 + e.ID.GetHashCode()));
                        foreach (var bp in CavesOfOoo.Data.LootTableRegistry.Roll(stockTable, rng))
                        {
                            if (!Factory.Blueprints.ContainsKey(bp)) continue;
                            var item = Factory.CreateEntity(bp);
                            if (item != null) inv.AddObject(item);
                        }
                        restocked++;
                    }
                }

                e.SetIntProperty(LastRestockProp, currentTurn);
            }
            return restocked;
        }
        // Membership is rebuilt per refill check: registry replacement and a keeper's
        // current authored table remain authoritative, with no persisted cache.
        private static int CountStockUnits(InventoryPart inventory, string stockTable)
        {
            var names = new HashSet<string>(System.StringComparer.Ordinal);
            var visited = new HashSet<string>(System.StringComparer.Ordinal);
            var pending = new Stack<string>(); pending.Push(stockTable);
            while (pending.Count > 0)
            {
                string name = pending.Pop();
                if (!visited.Add(name)) continue;
                var table = LootTableRegistry.Get(name);
                if (table?.Entries == null || (table.PickOne && System.Math.Max(table.MinPicks, table.MaxPicks) <= 0)) continue;
                foreach (var entry in table.Entries)
                {
                    if (entry == null || System.Math.Max(entry.MinCount, entry.MaxCount) <= 0
                        || (table.PickOne ? entry.Weight <= 0 : entry.Chance <= 0)) continue;
                    // The roller ignores Chance in weighted mode and prefers TableRef
                    // when an entry contains both fields. Match that exact contract.
                    if (!string.IsNullOrEmpty(entry.TableRef)) pending.Push(entry.TableRef);
                    else if (!string.IsNullOrEmpty(entry.Blueprint)) names.Add(entry.Blueprint);
                }
            }
            int units = 0;
            var seen = new HashSet<Entity>();
            foreach (var item in inventory.Objects)
            {
                if (item == null || !seen.Add(item) || !names.Contains(item.BlueprintName)) continue;
                int count = item.GetPart<StackerPart>()?.StackCount ?? 1;
                if (count <= 0) continue;
                if (count >= ShelfLowWaterMark - units) return ShelfLowWaterMark;
                units += count;
            }
            return units;
        }
    }
}
