using CavesOfOoo.Data;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// A plantable seed item. Declares a "Plant" inventory action
    /// (TonicPart's exact GetInventoryActions/InventoryAction event
    /// shape — zero InventoryUI changes needed; unknown commands route
    /// through PerformInventoryActionCommand's default path). Planting
    /// spawns <see cref="CropBlueprint"/> into the actor's current cell
    /// when the cell has Plantable terrain and no existing crop, then
    /// consumes one seed (StackerPart-aware).
    /// See <c>Docs/CROPS-WATERING-GRIMOIRE.md §2.2</c>.
    /// </summary>
    public class SeedPart : Part
    {
        public override string Name => "Seed";

        /// <summary>
        /// Global factory — set once by GameBootstrap, read at plant
        /// time. Mirrors the CorpsePart.Factory /
        /// MaterialReactionResolver.Factory convention. Tests that leave
        /// this null hit the graceful `no_factory` reject path.
        /// </summary>
        public static EntityFactory Factory;

        /// <summary>Crop blueprint spawned on planting. Blueprint param.</summary>
        public string CropBlueprint = "";

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var actions = e.GetParameter<InventoryActionList>("Actions");
                if (actions != null)
                    actions.AddAction("Plant", "plant", "PlantSeed", 'p', 20);
                return true;
            }

            if (e.ID == "InventoryAction")
            {
                string command = e.GetStringParameter("Command");
                if (command != "PlantSeed") return true;

                var actor = e.GetParameter<Entity>("Actor");
                if (actor == null) return true;

                DoPlant(actor, e);
                e.Handled = true;
                return false;
            }

            return true;
        }

        private void DoPlant(Entity actor, GameEvent e)
        {
            Zone zone = e.GetParameter<Zone>("Zone") ?? SettlementRuntime.ActiveZone;
            if (zone == null)
            {
                Reject(actor, "no_zone", "There is no ground here to plant in.");
                return;
            }

            var pos = zone.GetEntityPosition(actor);
            if (pos.x < 0)
            {
                Reject(actor, "no_zone", "There is no ground here to plant in.");
                return;
            }

            Cell cell = zone.GetCell(pos.x, pos.y);
            if (cell == null)
            {
                Reject(actor, "no_zone", "There is no ground here to plant in.");
                return;
            }

            // Gate: the cell's terrain must be Plantable (content-driven —
            // the Plantable tag lives on terrain blueprints like Grass).
            bool plantable = false;
            for (int i = 0; i < cell.Objects.Count; i++)
            {
                var obj = cell.Objects[i];
                if (obj.HasTag("Terrain") && obj.HasTag("Plantable"))
                {
                    plantable = true;
                    break;
                }
            }
            if (!plantable)
            {
                Reject(actor, "not_plantable", "The ground here is too hard to plant in.");
                return;
            }

            // Gate: one crop per cell.
            if (cell.HasObjectWithPart<CropPart>())
            {
                Reject(actor, "already_planted", "Something is already growing here.");
                return;
            }

            // Gate: factory + blueprint resolve.
            if (Factory == null)
            {
                Reject(actor, "no_factory", "The seed refuses to take root.");
                return;
            }
            Entity crop = Factory.CreateEntity(CropBlueprint);
            if (crop == null)
            {
                Reject(actor, "unknown_blueprint", "The seed refuses to take root.");
                return;
            }

            zone.AddEntity(crop, pos.x, pos.y);
            ZoneRenderHooks.MarkCellDirty(pos.x, pos.y, "CropPlanted");
            ConsumeOneSeed(actor);

            MessageLog.Add($"{actor.GetDisplayName()} plants {ParentEntity.GetDisplayName()}.");
            if (Diag.IsChannelEnabled("crop"))
                Diag.Record("crop", "CropPlanted", actor: actor, target: crop,
                    payload: new { cropBlueprint = CropBlueprint, x = pos.x, y = pos.y });
        }

        private void Reject(Entity actor, string reason, string message)
        {
            MessageLog.Add(message);
            if (Diag.IsChannelEnabled("crop"))
                Diag.Record("crop", "PlantRejected", actor: actor,
                    payload: new { reason = reason, cropBlueprint = CropBlueprint });
        }

        /// <summary>StackerPart-aware single-seed consumption —
        /// TonicPart.ConsumeItem's exact pattern.</summary>
        private void ConsumeOneSeed(Entity actor)
        {
            var stacker = ParentEntity.GetPart<StackerPart>();
            if (stacker != null && stacker.StackCount > 1)
            {
                stacker.StackCount--;
            }
            else
            {
                var inv = actor.GetPart<InventoryPart>();
                if (inv != null)
                    inv.RemoveObject(ParentEntity);
            }
        }
    }
}
