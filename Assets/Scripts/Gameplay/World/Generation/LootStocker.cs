using System.Collections.Generic;
using CavesOfOoo.Data;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// BIOME-OVERHAUL A1 — fills a <see cref="ContainerPart"/> from a
    /// named loot table. The bridge between LootTableRegistry (Data)
    /// and world entities; used by lair/village builders today and the
    /// A2 structure stamps' chest markers next.
    /// </summary>
    public static class LootStocker
    {
        /// <summary>
        /// Roll <paramref name="tableName"/> and add the results to the
        /// container entity. Returns items actually added (unknown
        /// blueprints and full containers are skipped, never a crash).
        /// Emits one loot/TableRolled diag per call.
        /// </summary>
        public static int StockContainer(Entity container, string tableName,
            EntityFactory factory, System.Random rng)
        {
            var containerPart = container?.GetPart<ContainerPart>();
            if (containerPart == null || factory == null)
                return 0;

            List<string> rolled = LootTableRegistry.Roll(tableName, rng);
            int added = 0;
            for (int i = 0; i < rolled.Count; i++)
            {
                if (!factory.Blueprints.ContainsKey(rolled[i]))
                    continue;
                Entity item = factory.CreateEntity(rolled[i]);
                if (item != null && containerPart.AddItem(item))
                    added++;
            }

            if (CavesOfOoo.Diagnostics.Diag.IsChannelEnabled("loot"))
            {
                CavesOfOoo.Diagnostics.Diag.Record(
                    category: "loot", kind: "TableRolled",
                    actor: container,
                    payload: new { table = tableName, rolledCount = rolled.Count, added });
            }

            return added;
        }
    }
}
