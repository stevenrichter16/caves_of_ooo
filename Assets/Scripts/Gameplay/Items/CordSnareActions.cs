using System;
using System.Globalization;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Paid, adjacent placement of a finite physical cord snare. The
    /// command binds its origin, destination and selected stack quantity; an
    /// uncommitted placement stays inert and has no recoverable cord payload.</summary>
    public static class CordSnareActions
    {
        const string Prefix = "LayCordSnare|";
        public const string Blueprint = "KnotflaxSnare";
        static readonly (int x, int y, string label)[] Directions =
        {
            (0,-1,"north"),(1,-1,"northeast"),(1,0,"east"),(1,1,"southeast"),
            (0,1,"south"),(-1,1,"southwest"),(-1,0,"west"),(-1,-1,"northwest")
        };
        public static bool IsCommand(string command) => command?.StartsWith(Prefix, StringComparison.Ordinal) == true;
        public static void AddActions(Entity actor, Entity item, Zone zone, InventoryActionList actions)
        {
            if (actions == null || Validate(actor, item, zone) != null) return;
            var origin = zone.GetEntityCell(actor);
            foreach (var direction in Directions)
            {
                int x = origin.X + direction.x, y = origin.Y + direction.y;
                if (!EmptyDestination(zone, origin, x, y)) continue;
                string command = Prefix + Uri.EscapeDataString(zone.ZoneID ?? "") + "|" + Number(origin.X) + "|" + Number(origin.Y)
                    + "|" + Number(x) + "|" + Number(y) + "|" + Number(Quantity(item));
                actions.AddAction("LayCordSnare", "lay cord snare " + direction.label + " (1 cord)", command, '\0', 18);
            }
        }
        internal static bool TryAct(Entity actor, Entity item, Zone zone, string command, InventoryTransaction transaction)
        {
            if (!IsCommand(command)) return false;
            string invalid = Validate(actor, item, zone);
            if (invalid != null || transaction == null) return Reject(actor, item, command, invalid ?? "missing-transaction");
            string[] fields = command.Split('|');
            if (fields.Length != 7 || !Parse(fields[2], out int ox) || !Parse(fields[3], out int oy)
                || !Parse(fields[4], out int x) || !Parse(fields[5], out int y) || !Parse(fields[6], out int count))
                return Reject(actor, item, command, "malformed-selection");
            var origin = zone.GetEntityCell(actor);
            if (WorldResourceActions.Decode(fields[1]) != zone.ZoneID || origin.X != ox || origin.Y != oy || count != Quantity(item))
                return Reject(actor, item, command, "stale-selection");
            if (!EmptyDestination(zone, origin, x, y)) return Reject(actor, item, command, "occupied-or-distant-ground");
            if (!transaction.TryClaim(actor, actor, command) || !transaction.TryClaim(item, actor, command))
                return Reject(actor, item, command, "in-progress");
            var factory = HarvestablePart.Factory;
            var snare = factory.CreateEntity(Blueprint);
            var trigger = snare?.GetPart<CordSnarePart>();
            if (!Fresh(snare) || zone.GetReadOnlyEntities().Any(e => e.ID == snare.ID))
                return Reject(actor, item, command, "invalid-snare");
            if (!transaction.TryClaim(snare, actor, command)) return Reject(actor, item, command, "in-progress");
            var inventory = actor.GetPart<InventoryPart>();
            var physics = item.GetPart<PhysicsPart>(); var parts = item.Parts.ToArray();
            var receipt = InventoryTransferSnapshot.Capture(inventory, item);
            transaction.Do(null, receipt.Restore);
            if (!receipt.Apply(() => inventory.TryConsumeOne(item)) || !receipt.ClaimChanges(transaction, actor, command)
                || HarvestablePart.Factory != factory || !WorldResourceActions.ActorCurrent(actor, zone)
                || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true || zone.GetEntityCell(actor) != origin
                || actor.GetPart<InventoryPart>() != inventory || item.BlueprintName != "KnotflaxCord" || !item.Parts.SequenceEqual(parts)
                || (count > 1 ? !WorldResourceActions.Carried(actor, item, single: false) || Quantity(item) != count - 1
                    : inventory.Objects.Contains(item) || item.SpatialZone != null || physics.InInventory != null || physics.Equipped != null)
                || !Fresh(snare) || !EmptyDestination(zone, origin, x, y))
                return Reject(actor, item, command, "changed-during-payment");
            // The outer AfterInventoryAction hook can refuse this work. An
            // uncommitted snare must not catch a creature moved by that hook.
            trigger.Spent = true;
            transaction.Do(null, () =>
            {
                if (snare.SpatialZone == zone) zone.RemoveEntity(snare);
            });
            if (!zone.AddEntity(snare, x, y)) return Reject(actor, item, command, "placement-refused");
            transaction.AfterCommit(() =>
            {
                if (snare.SpatialZone != zone || snare.GetPart<CordSnarePart>() != trigger) return;
                trigger.Spent = false;
                ZoneRenderHooks.MarkCellDirty(zone.GetEntityCell(snare), "CordSnareArmed");
                Diag.Record("event", "CordSnareLaid", actor, snare, new { x, y, spent = 1, holdTurns = CordSnarePart.HoldTurns });
                MessageLog.Add("You stake a cord loop across the ground. It will hold the first creature to cross, including you; the cord cannot be recovered.");
            });
            return true;
        }
        static string Validate(Entity actor, Entity item, Zone zone)
        {
            if (item?.BlueprintName != "KnotflaxCord") return "unsupported-material";
            if (!WorldResourceActions.ActorCurrent(actor, zone) || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true) return "actor-unavailable";
            if (!WorldResourceActions.Carried(actor, item, single: false)) return "not-carried";
            if (HarvestablePart.Factory?.Blueprints.ContainsKey(Blueprint) != true) return "missing-snare-definition";
            return null;
        }
        static bool EmptyDestination(Zone zone, Cell origin, int x, int y)
        {
            if (origin == null || Math.Abs((long)x - origin.X) > 1 || Math.Abs((long)y - origin.Y) > 1 || x == origin.X && y == origin.Y) return false;
            var cell = zone.GetCell(x, y);
            return cell != null && cell.IsPassable() && cell.Occupants.All(e => e != null && e.HasTag("Terrain") && !e.HasTag("Creature"));
        }
        static bool Fresh(Entity snare) => snare != null && snare.BlueprintName == Blueprint && !string.IsNullOrEmpty(snare.ID)
            && snare.SpatialZone == null && snare.GetPart<PhysicsPart>() is PhysicsPart physics && physics.ParentEntity == snare
            && !physics.Solid && !physics.Takeable && physics.InInventory == null && physics.Equipped == null
            && snare.GetPart<RenderPart>()?.Visible == true && snare.GetPart<CordSnarePart>() is CordSnarePart trigger
            && trigger.ParentEntity == snare && !trigger.Spent && !snare.HasPart<HarvestablePart>()
            && snare.Parts.All(p => p.ParentEntity == snare);
        static int Quantity(Entity item) => item.GetPart<StackerPart>()?.StackCount ?? 1;
        static bool Parse(string text, out int result) => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
        static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        static bool Reject(Entity actor, Entity item, string command, string reason)
        {
            Diag.Record("event", "CordSnareRejected", actor, item, new { command, reason });
            MessageLog.Add("You cannot lay that snare (" + reason.Replace('-', ' ') + ")."); return false;
        }
    }
}
