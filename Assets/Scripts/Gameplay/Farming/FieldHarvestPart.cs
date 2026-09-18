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
            if (e.ID == "GetInventoryActions")
            {
                if (!Harvested) e.GetParameter<InventoryActionList>("Actions")
                    ?.AddAction("Harvest", "harvest", "Harvest", 'h', 20);
                return true;
            }
            if (e.ID != "InventoryAction" || e.GetStringParameter("Command") != "Harvest") return true;
            var actor = e.GetParameter<Entity>("Actor");
            var zone = e.GetParameter<Zone>("Zone") ?? SettlementRuntime.ActiveZone;
            if (Harvested) return Reject(actor, "spent");
            if (actor == null || ParentEntity == null || zone == null) return Reject(actor, "missing-context");
            var row = zone.GetEntityCell(ParentEntity);
            var standing = zone.GetEntityCell(actor);
            if (row == null || standing == null) return Reject(actor, "detached-owner-or-actor");
            if (Math.Abs(row.X - standing.X) > 1 || Math.Abs(row.Y - standing.Y) > 1)
                return Reject(actor, "out-of-reach");
            var factory = HarvestablePart.Factory;
            if (factory == null || string.IsNullOrEmpty(YieldBlueprint) || YieldCount <= 0
                || !factory.Blueprints.ContainsKey(YieldBlueprint)) return Reject(actor, "missing-yield");

            // Stage on the real row cell before spending it or packing any item.
            // A failed placement rolls back every staged item, leaving the row
            // and inventory unchanged. Overflow is already safely on the ground.
            var items = new List<Entity>();
            for (int i = 0; i < YieldCount; i++)
            {
                var item = factory.CreateEntity(YieldBlueprint);
                if (item == null || !zone.AddEntity(item, row.X, row.Y))
                {
                    foreach (var staged in items) zone.RemoveEntity(staged);
                    return Reject(actor, "yield-placement");
                }
                items.Add(item);
            }
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
            int packed = 0;
            var inventory = actor.GetPart<InventoryPart>();
            foreach (var item in items)
                if (inventory != null && inventory.AddObject(item)) { zone.RemoveEntity(item); packed++; }
            ZoneRenderHooks.MarkCellDirty(row.X, row.Y, "FieldHarvested");
            int dropped = items.Count - packed;
            MessageLog.Add(dropped > 0
                ? $"You gather the ripe grain: {packed} packed, {dropped} left on the cut row."
                : "You gather the ripe grain, leaving cut stubble.");
            if (Diag.IsChannelEnabled("loot")) Diag.Record("loot", "FieldHarvested", actor: actor,
                target: ParentEntity, payload: new { yield = YieldBlueprint, count = packed, dropped });
            e.Handled = true;
            return false;
        }

        private bool Reject(Entity actor, string reason)
        {
            if (Diag.IsChannelEnabled("loot")) Diag.Record("loot", "FieldHarvestRejected", actor: actor,
                target: ParentEntity, payload: new { reason });
            return true;
        }
    }
}
