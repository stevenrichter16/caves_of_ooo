using System;
using System.Collections.Generic;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>A finite gleaning from a standing field row. Unlike mineral or
    /// corpse harvesting, the native owner remains as cut stubble. Public fields
    /// are saved by the normal Part reflection path; no growth clock is attached.</summary>
    public sealed class FieldHarvestPart : Part
    {
        public override string Name => "FieldHarvest";
        public bool Harvested;
        public string YieldBlueprint = "Emberwheat";
        public int YieldCount = 1;

        public override bool HandleEvent(GameEvent e)
        {
            var actor = e.GetParameter<Entity>("Actor");
            var zone = e.GetParameter<Zone>("Zone") ?? SettlementRuntime.ActiveZone;
            if (e.ID == "GetInventoryActions")
            {
                zone ??= actor?.SpatialZone;
                var actions = e.GetParameter<InventoryActionList>("Actions");
                if (!Harvested) actions?.AddAction("Harvest", "harvest", "Harvest", 'h', 20);
                if (FieldworkActions.CanPrepare(actor, this, zone)) actions?.AddAction("PrepareFieldBed", "prepare cut stubble for planting", "PrepareFieldBed", '\0', 19);
                return true;
            }
            if (e.ID != "InventoryAction") return true;
            string command = e.GetStringParameter("Command");
            var tx = e.GetParameter<CavesOfOoo.Core.Inventory.InventoryTransaction>("InventoryTransaction");
            bool success = command == "Harvest" ? FieldHarvestService.TryHarvest(actor, this, zone, tx)
                : command == "PrepareFieldBed" && FieldworkActions.Prepare(actor, this, zone, tx);
            if (!success) return true; e.Handled = true; return false;
        }

        // Only the scoped grazer uses this path after checking both exact rows.
        // It creates no yield, healing, inventory changes, or player action.
        internal void ConsumeByGrazer()
        {
            SetStubble();
            var row = ParentEntity.SpatialZone?.GetEntityCell(ParentEntity);
            if (row != null) ZoneRenderHooks.MarkCellDirty(row.X, row.Y, "FieldGrazed");
        }

        internal void SetStubble()
        {
            Harvested = true;
            var render = ParentEntity.GetPart<RenderPart>();
            if (render != null)
            {
                render.DisplayName = "cut emberwheat row";
                render.RenderString = "\"";
                render.ColorString = "&w";
            }
            var examine = ParentEntity.GetPart<ExaminablePart>();
            if (examine != null) examine.Text = "Cut emberwheat stubble in an old field strip. The grain has already been gathered.";
        }

        private bool Reject(Entity actor, string reason)
        {
            if (Diag.IsChannelEnabled("loot")) Diag.Record("loot", "FieldHarvestRejected", actor: actor,
                target: ParentEntity, payload: new { reason });
            return true;
        }
    }
}
