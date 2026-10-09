using System;
using System.Globalization;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Improvised combat uses for carried supplies, including items
    /// restored from saves without a new Part. Commands bind the selected zone,
    /// actor origin, target and quantity; execution rechecks their actual owners.
    /// The outer inventory transaction owns payment and the precise layer delta.</summary>
    public static class CombatUtilityActions
    {
        public const int GreaseTurns = 8;
        public const int GritTurns = 12;
        public const string GritResidue = "grit";
        const string GreasePrefix = "SpreadGrease|";
        const string GritPrefix = "ScatterGrit|";
        static readonly (int x, int y, string name)[] Directions =
        {
            (0, 0, "here"), (0, -1, "north"), (1, -1, "northeast"), (1, 0, "east"), (1, 1, "southeast"),
            (0, 1, "south"), (-1, 1, "southwest"), (-1, 0, "west"), (-1, -1, "northwest")
        };

        public static bool IsCommand(string command) => command != null
            && (command.StartsWith(GreasePrefix, StringComparison.Ordinal) || command.StartsWith(GritPrefix, StringComparison.Ordinal)
                || CordSnareActions.IsCommand(command) || EmergencyDousingActions.IsCommand(command)
                || CompanionCareActions.IsCommand(command) || MaterialFieldActions.IsCommand(command)
                || EquipmentUtilityActions.IsCommand(command));

        public static void AddActions(Entity actor, Entity item, Zone zone, InventoryActionList actions)
        {
            CordSnareActions.AddActions(actor, item, zone, actions);
            EmergencyDousingActions.AddActions(actor, item, zone, actions);
            CompanionCareActions.AddActions(actor, item, zone, actions);
            MaterialFieldActions.AddActions(actor, item, zone, actions);
            EquipmentUtilityActions.AddActions(actor, item, zone, actions);
            if (actions == null || Validate(actor, item, zone, out bool grease) != null) return;
            var origin = zone.GetEntityCell(actor);
            int count = Quantity(item);
            foreach (var direction in Directions)
            {
                int x = origin.X + direction.x, y = origin.Y + direction.y;
                if (Destination(zone, origin, x, y, grease) != null) continue;
                string command = (grease ? GreasePrefix : GritPrefix) + Uri.EscapeDataString(zone.ZoneID ?? "")
                    + "|" + Number(origin.X) + "|" + Number(origin.Y) + "|" + Number(x) + "|" + Number(y) + "|" + Number(count);
                actions.AddAction(grease ? "SpreadGrease" : "ScatterGrit",
                    (grease ? "spread grease " : "scatter grit ") + direction.name + " (1 item)", command, '\0', 18);
            }
        }

        /// <summary>Joins the native command transaction. False preserves all
        /// supplies; only commit publishes the success receipt and tile reactions.</summary>
        internal static bool TryAct(Entity actor, Entity item, Zone zone, string command, InventoryTransaction transaction)
        {
            if (CordSnareActions.IsCommand(command)) return CordSnareActions.TryAct(actor, item, zone, command, transaction);
            if (EmergencyDousingActions.IsCommand(command)) return EmergencyDousingActions.TryAct(actor, item, zone, command, transaction);
            if (CompanionCareActions.IsCommand(command)) return CompanionCareActions.TryAct(actor, item, zone, command, transaction);
            if (MaterialFieldActions.IsCommand(command)) return MaterialFieldActions.TryAct(actor, item, zone, command, transaction);
            if (EquipmentUtilityActions.IsCommand(command)) return EquipmentUtilityActions.TryAct(actor, item, zone, command, transaction);
            if (!IsCommand(command)) return false;
            string invalid = Validate(actor, item, zone, out bool grease);
            if (invalid != null || transaction == null) return Reject(actor, item, command, invalid ?? "missing-transaction");
            string[] fields = command.Split('|');
            if (fields.Length != 7 || (grease ? fields[0] != "SpreadGrease" : fields[0] != "ScatterGrit")
                || !Parse(fields[2], out int originX) || !Parse(fields[3], out int originY)
                || !Parse(fields[4], out int x) || !Parse(fields[5], out int y) || !Parse(fields[6], out int count))
                return Reject(actor, item, command, "malformed-selection");
            var origin = zone.GetEntityCell(actor);
            if (WorldResourceActions.Decode(fields[1]) != zone.ZoneID || origin.X != originX || origin.Y != originY || count != Quantity(item))
                return Reject(actor, item, command, "stale-selection");
            invalid = Destination(zone, origin, x, y, grease);
            if (invalid != null) return Reject(actor, item, command, invalid);
            if (!transaction.TryClaim(actor, actor, command) || !transaction.TryClaim(item, actor, command))
                return Reject(actor, item, command, "in-progress");

            var inventory = actor.GetPart<InventoryPart>();
            var physics = item.GetPart<PhysicsPart>();
            var parts = item.Parts.ToArray();
            string blueprint = item.BlueprintName;
            var receipt = InventoryTransferSnapshot.Capture(inventory, item);
            transaction.Do(null, receipt.Restore);
            if (!receipt.Apply(() => inventory.TryConsumeOne(item)) || !receipt.ClaimChanges(transaction, actor, command)
                || !WorldResourceActions.ActorCurrent(actor, zone) || zone.GetEntityCell(actor) != origin
                || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true
                || actor.GetPart<InventoryPart>() != inventory || item.BlueprintName != blueprint || !item.Parts.SequenceEqual(parts)
                || (count > 1 ? !WorldResourceActions.Carried(actor, item, single: false) || Quantity(item) != count - 1
                    : inventory.Objects.Contains(item) || item.SpatialZone != null || physics.InInventory != null || physics.Equipped != null)
                || Destination(zone, origin, x, y, grease) != null)
                return Reject(actor, item, command, "changed-during-payment");

            string layerId = grease ? "oil" : GritResidue;
            int duration = grease ? GreaseTurns : GritTurns;
            var oldLayer = FindLayer(zone, x, y, grease, layerId);
            int oldTurns = oldLayer?.Turns ?? 0;
            ZoneTileState.Layer stagedLayer = null;
            transaction.Do(null, () =>
            {
                var current = FindLayer(zone, x, y, grease, layerId);
                // Independent changes to other layers, or a later stronger lease,
                // belong to their own operation and must survive this rollback.
                if (!ReferenceEquals(current, stagedLayer) || current == null || current.Turns != duration) return;
                if (oldLayer == null)
                {
                    if (grease) zone.TileState.RemoveCoating(x, y, layerId); else zone.TileState.RemoveResidue(x, y, layerId);
                }
                else
                {
                    current.Turns = oldTurns;
                    if (grease) zone.TileState.WriteCoating(x, y, layerId, oldTurns); else zone.TileState.WriteResidue(x, y, layerId, oldTurns);
                }
            });
            // Stage without publishing a success receipt or firing damaging
            // reactions. Tile state is rolled back if AfterInventoryAction fails.
            try
            {
                if (grease) zone.TileState.WriteCoating(x, y, layerId, duration); else zone.TileState.WriteResidue(x, y, layerId, duration);
            }
            finally { stagedLayer = FindLayer(zone, x, y, grease, layerId); }
            transaction.AfterCommit(() =>
            {
                // Do not resurrect a layer an independent post-action removed.
                if (FindLayer(zone, x, y, grease, layerId) == stagedLayer)
                {
                    if (grease) ZoneTileStateSystem.WriteCoating(zone, x, y, layerId, stagedLayer.Turns, actor, "SpreadGrease");
                    else ZoneTileStateSystem.WriteResidue(zone, x, y, layerId, stagedLayer.Turns, actor, "ScatterGrit");
                }
                Diag.Record("event", "CombatUtilityUsed", actor, item, new { blueprint, x, y, layer = layerId, turns = duration, spent = 1 });
                MessageLog.Add(grease ? "You spread a thin grease film. Anything crossing it may slip; it can still catch fire."
                    : "You scatter silver grit for firm footing. It steadies oil and ice without making their other hazards safe.");
                if (grease) ZoneTileStateSystem.ResolveAfterAbility(zone, actor);
            });
            return true;
        }

        static string Validate(Entity actor, Entity item, Zone zone, out bool grease)
        {
            grease = item?.BlueprintName == "FrogOil";
            if (!grease && item?.BlueprintName != "SilverSand") return "unsupported-material";
            if (!WorldResourceActions.ActorCurrent(actor, zone) || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true) return "actor-unavailable";
            if (!WorldResourceActions.Carried(actor, item, single: false)) return "not-carried";
            if (grease && (!LiquidRegistry.IsInitialized || LiquidRegistry.Get("oil") == null)) return "missing-oil-definition";
            return null;
        }
        static string Destination(Zone zone, Cell origin, int x, int y, bool grease)
        {
            if (origin == null || Math.Abs((long)x - origin.X) > 1 || Math.Abs((long)y - origin.Y) > 1) return "out-of-reach";
            var cell = zone.GetCell(x, y);
            if (cell == null || !cell.IsPassable()) return "blocked-ground";
            var existing = FindLayer(zone, x, y, grease, grease ? "oil" : GritResidue);
            if (existing != null && existing.Turns >= (grease ? GreaseTurns : GritTurns)) return "already-covered";
            return null;
        }
        static ZoneTileState.Layer FindLayer(Zone zone, int x, int y, bool coating, string id)
        {
            var state = zone.TileState.Get(x, y);
            var layers = coating ? state?.Coatings : state?.Residues;
            if (layers != null) foreach (var layer in layers) if (layer != null && layer.Id == id) return layer;
            return null;
        }
        static int Quantity(Entity item) => item.GetPart<StackerPart>()?.StackCount ?? 1;
        static bool Parse(string value, out int result) => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        static bool Reject(Entity actor, Entity item, string command, string reason)
        {
            Diag.Record("event", "CombatUtilityRejected", actor, item, new { command, reason });
            MessageLog.Add("You cannot use that ground supply (" + reason.Replace('-', ' ') + ").");
            return false;
        }
    }
}
