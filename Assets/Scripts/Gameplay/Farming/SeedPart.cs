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
                // The world-action menu fires this same event on
                // zone-resident items (WorldInteractionSystem.GatherActions),
                // so a seed lying on the ground would otherwise offer a
                // dead "Plant" row. Planting is defined only for a
                // carried seed — DoPlant's not_carried gate is the
                // enforcement; this keeps the row out of the menu.
                var actions = e.GetParameter<InventoryActionList>("Actions");
                if (actions != null && IsCarried(e.GetParameter<Entity>("Actor")))
                    actions.AddAction("Plant", "plant", "PlantSeed", 'p', 20);
                return true;
            }

            if (e.ID == "InventoryAction")
            {
                string command = e.GetStringParameter("Command");
                if (command != "PlantSeed") return true;

                var actor = e.GetParameter<Entity>("Actor");
                if (actor == null) return true;

                if (!DoPlant(actor, e)) return true;
                e.Handled = true;
                return false;
            }

            return true;
        }

        private bool DoPlant(Entity actor, GameEvent e)
        {
            // Anti-exploit gate: the seed must be in the ACTOR's inventory.
            // The world-action menu dispatches InventoryAction directly on
            // zone-resident items (InputHandler.ExecuteWorldActionSelection),
            // where consumption via InventoryPart.RemoveObject silently
            // no-ops — without this gate one dropped seed planted
            // infinitely. Audit finding SM7-F1 (2026-07-25).
            var carrierInv = actor.GetPart<InventoryPart>();
            if (carrierInv == null || !carrierInv.CanConsumeOne(ParentEntity))
            {
                bool empty = carrierInv?.Objects?.Contains(ParentEntity) == true;
                Reject(actor, empty ? "empty_stack" : "not_carried", empty ? "There is no seed left to plant." : "You aren't carrying that seed.");
                return false;
            }

            Zone zone = e.GetParameter<Zone>("Zone") ?? SettlementRuntime.ActiveZone;
            if (zone == null)
            {
                Reject(actor, "no_zone", "There is no ground here to plant in.");
                return false;
            }

            var pos = zone.GetEntityPosition(actor);
            if (pos.x < 0)
            {
                Reject(actor, "actor_not_in_zone", "There is no ground here to plant in.");
                return false;
            }

            Cell cell = zone.GetCell(pos.x, pos.y);
            if (cell == null)
            {
                Reject(actor, "no_cell", "There is no ground here to plant in.");
                return false;
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
                return false;
            }

            // Gate: one crop per cell.
            if (cell.HasObjectWithPart<CropPart>())
            {
                Reject(actor, "already_planted", "Something is already growing here.");
                return false;
            }

            // Gate: factory + blueprint resolve.
            if (Factory == null)
            {
                Reject(actor, "no_factory", "The seed refuses to take root.");
                return false;
            }
            Entity crop = Factory.CreateEntity(CropBlueprint);
            if (crop == null)
            {
                Reject(actor, "unknown_blueprint", "The seed refuses to take root.");
                return false;
            }

            if (!zone.AddEntity(crop, pos.x, pos.y))
            {
                Reject(actor, "placement_refused", "The seed cannot take root here.");
                return false;
            }
            if (!carrierInv.TryConsumeOne(ParentEntity))
            {
                zone.RemoveEntity(crop);
                Reject(actor, "payment_refused", "The seed is no longer available to plant.");
                return false;
            }
            ZoneRenderHooks.MarkCellDirty(pos.x, pos.y, "CropPlanted");

            MessageLog.Add($"{actor.GetDisplayName()} plants {InventoryPart.GetUnitDisplayName(ParentEntity)}.");
            if (Diag.IsChannelEnabled("crop"))
                Diag.Record("crop", "CropPlanted", actor: actor, target: crop,
                    payload: new { cropBlueprint = CropBlueprint, x = pos.x, y = pos.y });
            return true;
        }

        private void Reject(Entity actor, string reason, string message)
        {
            MessageLog.Add(message);
            if (Diag.IsChannelEnabled("crop"))
                Diag.Record("crop", "PlantRejected", actor: actor,
                    payload: new { reason = reason, cropBlueprint = CropBlueprint });
        }

        /// <summary>Offer planting only for a positive unit carried by the supplied
        /// actor. Actorless queries may resolve a verified actual carrier.</summary>
        private bool IsCarried(Entity actor)
        {
            if (actor != null) return actor.GetPart<InventoryPart>()?.CanConsumeOne(ParentEntity) == true;
            var carrier = ParentEntity?.GetPart<PhysicsPart>()?.InInventory;
            return carrier?.GetPart<InventoryPart>()?.CanConsumeOne(ParentEntity) == true;
        }
    }
}
