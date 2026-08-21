using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;
using Random = System.Random;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// BIOME-OVERHAUL A3 — a one-shot "harvest" action on an entity:
    /// butcherable corpses (attached at corpse-spawn time by
    /// <see cref="CorpsePart"/> from per-creature config) and mineral
    /// veins (declared directly on the vein blueprints). Harvesting
    /// rolls the yield into the actor's inventory and CONSUMES the
    /// target — from its zone cell if placed, else from the actor's
    /// inventory — even when the yield roll fails (a botched butchery
    /// still spends the carcass; no re-roll farming).
    /// Action shape mirrors <see cref="SeedPart"/>/<see cref="TonicPart"/>.
    /// </summary>
    public class HarvestablePart : Part
    {
        public override string Name => "Harvestable";

        /// <summary>
        /// Global factory — set once by GameBootstrap, read at harvest
        /// time. Mirrors the CorpsePart.Factory / SeedPart.Factory
        /// convention. Null factory = harvest consumes but yields
        /// nothing (graceful, message says so).
        /// </summary>
        public static EntityFactory Factory;

        /// <summary>Blueprint granted on a successful harvest.</summary>
        public string YieldBlueprint = "";

        /// <summary>Yield count rolled uniformly in [YieldMin, YieldMax].</summary>
        public int YieldMin = 1;
        public int YieldMax = 1;

        /// <summary>Percent chance the harvest yields anything (0-100).</summary>
        public int YieldChance = 100;

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var actions = e.GetParameter<InventoryActionList>("Actions");
                actions?.AddAction("Harvest", "harvest", "Harvest", 'h', 20);
                return true;
            }

            if (e.ID == "InventoryAction")
            {
                if (e.GetStringParameter("Command") != "Harvest") return true;
                var actor = e.GetParameter<Entity>("Actor");
                if (actor == null) return true;

                DoHarvest(actor, e);
                e.Handled = true;
                return false;
            }

            return true;
        }

        private void DoHarvest(Entity actor, GameEvent e)
        {
            var rng = e.GetParameter<Random>("Random") ?? new Random();
            var factory = Factory;
            string sourceName = ParentEntity.GetDisplayName();

            int yielded = 0;
            bool rollPassed = YieldChance >= 100 || rng.Next(100) < YieldChance;
            if (rollPassed && factory != null && !string.IsNullOrEmpty(YieldBlueprint)
                && factory.Blueprints.ContainsKey(YieldBlueprint))
            {
                int count = YieldMax > YieldMin ? rng.Next(YieldMin, YieldMax + 1) : YieldMin;
                var inv = actor.GetPart<InventoryPart>();
                for (int i = 0; i < count; i++)
                {
                    var item = factory.CreateEntity(YieldBlueprint);
                    if (item != null && inv != null && inv.AddObject(item))
                        yielded++;
                }
            }

            if (yielded > 0)
                MessageLog.Add($"You harvest {sourceName}: {yielded} x {YieldBlueprint}.");
            else
                MessageLog.Add($"You harvest {sourceName}, but find nothing worth keeping.");

            // W4.2 — "Do not dig." Charged at the harvest seam; GroveLaw
            // self-gates on player + MineralVein tag + Grovelands
            // surface, so berry-picking and every other biome no-op.
            GroveLaw.OnDig(actor, e.GetParameter<Zone>("Zone"), ParentEntity);

            if (Diag.IsChannelEnabled("loot"))
            {
                Diag.Record(
                    category: "loot", kind: "Harvested",
                    actor: actor,
                    payload: new
                    {
                        source = ParentEntity.BlueprintName,
                        yield = YieldBlueprint,
                        count = yielded,
                        rollPassed,
                    });
            }

            ConsumeTarget(actor, e);
        }

        /// <summary>
        /// Spend the harvest target: remove from its zone cell when
        /// placed in the world, else from the actor's inventory.
        /// </summary>
        private void ConsumeTarget(Entity actor, GameEvent e)
        {
            Zone zone = e.GetParameter<Zone>("Zone") ?? SettlementRuntime.ActiveZone;
            var cell = zone?.GetEntityCell(ParentEntity);
            if (cell != null)
            {
                zone.RemoveEntity(ParentEntity);
                ZoneRenderHooks.MarkCellDirty(cell.X, cell.Y, "Harvested");
                return;
            }
            actor.GetPart<InventoryPart>()?.RemoveObject(ParentEntity);
        }
    }
}
