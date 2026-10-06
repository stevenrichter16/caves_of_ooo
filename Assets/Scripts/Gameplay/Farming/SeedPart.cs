using System;
using CavesOfOoo.Core.Inventory;
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
        const string AdjacentPrefix = "PlantSeedAt|";

        /// <summary>
        /// Global factory — set once by GameBootstrap, read at plant
        /// time. Mirrors the CorpsePart.Factory /
        /// MaterialReactionResolver.Factory convention. Tests that leave
        /// this null hit the graceful `no_factory` reject path.
        /// </summary>
        public static EntityFactory Factory;

        /// <summary>Crop blueprint spawned on planting. Blueprint param.</summary>
        public string CropBlueprint = "";

        /// <summary>Opt-in seeds require an existing saved CultivatedSoil
        /// marker. Legacy seeds retain the ordinary Plantable-ground rule.</summary>
        public bool RequireCultivatedSoil;

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
                {
                    actions.AddAction("Plant", "plant here", "PlantSeed", 'p', 20);
                    var actor = e.GetParameter<Entity>("Actor"); var zone = e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone ?? SettlementRuntime.ActiveZone;
                    if (WorldResourceActions.ActorCurrent(actor, zone))
                    {
                        var origin = zone.GetEntityPosition(actor);
                        for (int y = origin.y - 1; y <= origin.y + 1; y++) for (int x = origin.x - 1; x <= origin.x + 1; x++)
                        {
                            var bed = zone.GetCell(x, y);
                            if ((x == origin.x && y == origin.y) || !CultivatedSoilPart.IsCultivated(zone, bed) || bed.HasObjectWithPart<CropPart>()) continue;
                            actions.AddAction("PlantAdjacent", "plant prepared bed (" + x + "," + y + ")",
                                AdjacentPrefix + Uri.EscapeDataString(zone.ZoneID) + "|" + x + "|" + y, '\0', 19);
                        }
                    }
                }
                return true;
            }

            if (e.ID == "InventoryAction")
            {
                string command = e.GetStringParameter("Command");
                if (command != "PlantSeed" && command?.StartsWith(AdjacentPrefix, StringComparison.Ordinal) != true) return true;

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
            var actorCell = zone.GetEntityCell(actor);
            bool adjacent = e.GetStringParameter("Command")?.StartsWith(AdjacentPrefix, StringComparison.Ordinal) == true;
            if (adjacent)
            {
                var fields = e.GetStringParameter("Command").Split('|');
                if (fields.Length != 4 || WorldResourceActions.Decode(fields[1]) != zone.ZoneID
                    || !int.TryParse(fields[2], out int x) || !int.TryParse(fields[3], out int y)
                    || !WorldResourceActions.ActorCurrent(actor, zone) || SpatialQuery.DistanceToCell(zone, actor, x, y) > 1) return false;
                pos = (x, y);
            }
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

            if ((RequireCultivatedSoil || adjacent) && !CultivatedSoilPart.IsCultivated(zone, cell))
            {
                Reject(actor, "not_cultivated", "This seed needs a prepared bed of tilled soil.");
                return false;
            }

            // Gate: one crop per cell.
            if (cell.HasObjectWithPart<CropPart>())
            {
                Reject(actor, "already_planted", "Something is already growing here.");
                return false;
            }

            // Factory initialization can invoke gameplay. Capture both the seed
            // and its intended bed before creating anything, and recheck them.
            if (Factory == null)
            {
                Reject(actor, "no_factory", "The seed refuses to take root.");
                return false;
            }
            var seed = ParentEntity;
            var seedPhysics = seed?.GetPart<PhysicsPart>();
            var stack = seed?.GetPart<StackerPart>();
            string seedId = seed?.ID, blueprint = CropBlueprint;
            bool requireSoil = RequireCultivatedSoil;
            int count = stack?.StackCount ?? 1;
            bool Current()
            {
                if (seed == null || seed.ID != seedId || seed.GetPart<SeedPart>() != this || ParentEntity != seed
                    || CropBlueprint != blueprint || RequireCultivatedSoil != requireSoil
                    || actor.GetPart<InventoryPart>() != carrierInv || carrierInv.ParentEntity != actor
                    || !carrierInv.CanConsumeOne(seed) || seed.GetPart<StackerPart>() != stack
                    || (stack?.StackCount ?? 1) != count || seed.GetPart<PhysicsPart>() != seedPhysics
                    || seedPhysics == null || seedPhysics.ParentEntity != seed || seedPhysics.InInventory != actor
                    || seedPhysics.Equipped != null || seed.SpatialZone != null
                    || actor.SpatialZone != zone || zone.GetEntityCell(actor) != actorCell
                    || actorCell == null || !actorCell.Objects.Contains(actor) || SpatialQuery.DistanceToCell(zone, actor, cell.X, cell.Y) > 1 || CombatSystem.IsDeathHandled(actor)
                    || (actor.GetStat("Hitpoints") is Stat hp && hp.Value <= 0)
                    || cell.HasObjectWithPart<CropPart>() || BarrenGroundRules.IsBarren(cell)) return false;
                if ((requireSoil || adjacent) && (!ReferenceEquals(SettlementRuntime.ActiveZone, zone) || !CultivatedSoilPart.IsCultivated(zone, cell))) return false;
                foreach (var ground in cell.Objects)
                    if (ground.HasTag("Terrain") && ground.HasTag("Plantable")) return true;
                return false;
            }
            if (!Current()) { Reject(actor, "source_changed", "The seed or planting place is no longer available."); return false; }
            var transaction = e.GetParameter<InventoryTransaction>("InventoryTransaction");
            bool own = transaction == null;
            transaction ??= new InventoryTransaction();
            try
            {
                if (!transaction.TryClaim(seed, actor, "PlantSeed") || !transaction.TryClaim(actor, actor, "PlantSeed"))
                { Reject(actor, "in_progress", "That seed is already being planted."); return false; }
                Entity crop = string.IsNullOrEmpty(blueprint) ? null : Factory.CreateEntity(blueprint);
                if (crop == null) { Reject(actor, "unknown_blueprint", "The seed refuses to take root."); return false; }
                var planted = crop?.GetPart<CropPart>();
                var cropPhysics = crop?.GetPart<PhysicsPart>();
                if (!Current() || crop == null || crop.BlueprintName != blueprint || string.IsNullOrEmpty(crop.ID)
                    || crop.SpatialZone != null || !crop.HasTag("Crop") || crop.HasTag("Item") || crop.HasTag("Creature")
                    || crop.HasTag("Solid") || planted == null || planted.ParentEntity != crop
                    || planted.GrowthStage != 0 || planted.TicksInStage != 0 || planted.MoistureTicks != 0
                    || cropPhysics == null || cropPhysics.ParentEntity != crop || cropPhysics.Solid || cropPhysics.Takeable
                    || cropPhysics.InInventory != null || cropPhysics.Equipped != null)
                { Reject(actor, "invalid_crop_or_source_changed", "The seed refuses to take root."); return false; }
                var receipt = InventoryTransferSnapshot.Capture(carrierInv);
                bool placed = false, restored = false;
                Action restore = () =>
                {
                    if (restored) return;
                    restored = true;
                    receipt.Restore();
                    if (placed && crop.SpatialZone == zone) zone.RemoveEntity(crop);
                    ZoneRenderHooks.MarkCellDirty(pos.x, pos.y, "PlantRollback");
                };
                transaction.Do(null, restore);
                if (!zone.AddEntity(crop, pos.x, pos.y))
                { restore(); Reject(actor, "placement_refused", "The seed cannot take root here."); return false; }
                placed = true;
                if (!CropTime.Reconcile(planted, zone, WorldClock.CurrentTick))
                { restore(); Reject(actor, "invalid_crop_clock", "The seed cannot take root here."); return false; }
                if (!receipt.Apply(() => carrierInv.TryConsumeOne(seed)) || !receipt.ClaimChanges(transaction, actor, "PlantSeed"))
                { restore(); Reject(actor, "payment_refused", "The seed is no longer available to plant."); return false; }
                ZoneRenderHooks.MarkCellDirty(pos.x, pos.y, "CropPlanted");
                transaction.AfterCommit(() =>
                {
                    MessageLog.Add($"{actor.GetDisplayName()} plants {InventoryPart.GetUnitDisplayName(seed)}.");
                    if (Diag.IsChannelEnabled("crop")) Diag.Record("crop", "CropPlanted", actor: actor, target: crop,
                        payload: new { cropBlueprint = blueprint, x = pos.x, y = pos.y });
                });
                if (own) transaction.Commit();
                return true;
            }
            finally { if (own) transaction.Rollback(); }
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
