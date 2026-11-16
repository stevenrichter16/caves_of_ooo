using System;
using System.Globalization;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Supervised redistribution of a single non-water liquid. No
    /// conversion, ground exposure, elapsed work or new persistent state.</summary>
    internal static class LiquidTransferActions
    {
        const string AllPrefix = "TransferLiquid|", OnePrefix = "TransferOneLiquid|";
        internal static bool IsCommand(string command) => command != null
            && (command.StartsWith(AllPrefix, StringComparison.Ordinal) || command.StartsWith(OnePrefix, StringComparison.Ordinal));

        static bool Current(Entity actor, Zone zone)
        {
            if (!PreparationActions.CurrentActor(actor, zone)
                || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true) return false;
            var cell = zone.GetEntityCell(actor);
            return cell != null && zone.GetCell(cell.X, cell.Y) == cell;
        }
        static T Unique<T>(Entity owner) where T : Part
        {
            T found = null;
            if (owner?.Parts == null) return null;
            foreach (var p in owner.Parts)
                if (p is T candidate)
                { if (found != null || candidate.ParentEntity != owner) return null; found = candidate; }
            return found;
        }
        static bool Vessel(Entity actor, Entity owner, out LiquidVesselPart liquid)
        {
            liquid = Unique<LiquidVesselPart>(owner);
            var stack = Unique<StackerPart>(owner);
            return WorldResourceActions.Carried(actor, owner) && Unique<PhysicsPart>(owner) != null
                && (!owner.HasPart<StackerPart>() || (stack != null && stack.StackCount == 1 && stack.MaxStack >= 1))
                && liquid != null && !owner.HasPart<WaterskinPart>() && !owner.HasTag("Creature")
                && !owner.HasTag("Terrain") && !owner.HasPart<SpatialFootprintPart>()
                && liquid.Capacity > 0 && liquid.Volume >= 0 && liquid.Volume <= liquid.Capacity
                && LiquidRegistry.IsInitialized && (liquid.Volume == 0 ? liquid.LiquidId == ""
                    : !string.IsNullOrEmpty(liquid.LiquidId) && LiquidRegistry.Get(liquid.LiquidId) != null);
        }
        static bool Pair(Entity actor, Entity source, Entity target, out LiquidVesselPart from, out LiquidVesselPart to)
        {
            from = to = null;
            return source != target && source != actor && target != actor && source?.ID != target?.ID
                && Vessel(actor, source, out from) && Vessel(actor, target, out to)
                && from.Volume > 0 && from.LiquidId != "water" && to.Volume < to.Capacity
                && (to.Volume == 0 || to.LiquidId == from.LiquidId);
        }
        static string Encode(string value) => Uri.EscapeDataString(value ?? "");
        static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
        static string Command(Entity source, Entity target, LiquidVesselPart from, LiquidVesselPart to, Zone zone, bool one)
            => (one ? OnePrefix : AllPrefix) + Encode(source.ID) + "|" + Encode(target.ID) + "|" + Encode(from.LiquidId)
                + "|" + Number(from.Volume) + "|" + Number(from.Capacity) + "|" + Encode(to.LiquidId)
                + "|" + Number(to.Volume) + "|" + Number(to.Capacity) + "|" + Encode(zone.ZoneID);

        internal static void AddActions(Entity actor, Entity source, Zone zone, InventoryActionList actions)
        {
            if (actions == null || !Current(actor, zone) || !Vessel(actor, source, out var from)
                || from.Volume == 0 || from.LiquidId == "water") return;
            var items = actor.GetPart<InventoryPart>().Objects;
            int receiver = 0;
            for (int i = 0; i < items.Count; i++)
            {
                var target = items[i];
                if (!Pair(actor, source, target, out from, out var to)) continue;
                receiver++;
                int amount = Math.Min(from.Volume, to.Capacity - to.Volume);
                Add(false, amount); if (amount > 1) Add(true, 1);
                void Add(bool one, int units)
                {
                    string liquid = LiquidRegistry.Get(from.LiquidId).DisplayName;
                    string text = "transfer " + units + " " + liquid + " (src " + from.Volume + "→" + (from.Volume - units)
                        + "; to #" + receiver + " " + to.Volume + "→" + (to.Volume + units) + "/" + to.Capacity + ")";
                    actions.AddAction(one ? "TransferOneLiquid" : "TransferLiquid", text,
                        Command(source, target, from, to, zone, one), '\0', one ? 21 : 22);
                }
            }
        }

        internal static bool TryAct(Entity actor, Entity source, Zone zone, string command, InventoryTransaction tx)
        {
            if (!IsCommand(command)) return false;
            if (tx == null || !Current(actor, zone)) return Reject("actor-or-transaction");
            var bits = command.Split('|');
            if (bits.Length != 10 || !Parse(bits[4], out int before) || !Parse(bits[5], out int capacity)
                || !Parse(bits[7], out int targetBefore) || !Parse(bits[8], out int targetCapacity)) return Reject("malformed-selection");
            var target = WorldResourceActions.ExactCarried(actor, bits[2]);
            if (!Pair(actor, source, target, out var from, out var to)
                || WorldResourceActions.Decode(bits[1]) != source.ID || WorldResourceActions.Decode(bits[3]) != from.LiquidId
                || WorldResourceActions.Decode(bits[6]) != to.LiquidId || WorldResourceActions.Decode(bits[9]) != zone.ZoneID
                || before != from.Volume || capacity != from.Capacity || targetBefore != to.Volume || targetCapacity != to.Capacity)
                return Reject("selection-changed-or-incompatible");
            if (!tx.TryClaim(actor, actor, command) || !tx.TryClaim(source, actor, command) || !tx.TryClaim(target, actor, command))
                return Reject("in-progress");
            int amount = command.StartsWith(OnePrefix, StringComparison.Ordinal) ? 1 : Math.Min(before, targetCapacity - targetBefore);
            string liquidId = from.LiquidId, targetId = to.LiquidId, sourceID = source.ID, recipientID = target.ID;
            var sourcePhysics = source.GetPart<PhysicsPart>(); var targetPhysics = target.GetPart<PhysicsPart>();
            var anchor = zone.GetEntityCell(actor); var definition = LiquidRegistry.Get(liquidId);
            tx.Do(() =>
            {
                from.Volume = before - amount; from.LiquidId = from.Volume == 0 ? "" : liquidId;
                to.Volume = targetBefore + amount; to.LiquidId = liquidId;
            }, () => { from.Volume = before; from.LiquidId = liquidId; to.Volume = targetBefore; to.LiquidId = targetId; });
            // AfterInventoryAction listeners may move owners, change capacities or
            // replace Parts. Refuse the whole receipt before it can become payment.
            tx.BeforeCommit(() => Current(actor, zone) && zone.GetEntityCell(actor) == anchor
                && source.ID == sourceID && target.ID == recipientID && source != target
                && Vessel(actor, source, out var currentFrom) && currentFrom == from
                && Vessel(actor, target, out var currentTo) && currentTo == to
                && source.GetPart<PhysicsPart>() == sourcePhysics && target.GetPart<PhysicsPart>() == targetPhysics
                && from.Capacity == capacity && to.Capacity == targetCapacity
                && from.Volume == before - amount && from.LiquidId == (before == amount ? "" : liquidId)
                && to.Volume == targetBefore + amount && to.LiquidId == liquidId && LiquidRegistry.Get(liquidId) == definition);
            tx.AfterCommit(() =>
            {
                Diag.Record("liquid", "VesselTransferred", actor, source,
                    new { recipient = recipientID, liquidId, amount, sourceRemaining = before - amount, received = targetBefore + amount });
                MessageLog.Add("You transfer " + amount + " " + definition.DisplayName + " between the flasks.");
            });
            return true;
            bool Reject(string reason)
            {
                Diag.Record("liquid", "VesselTransferRejected", actor, source, new { reason });
                if (actor?.HasTag("Player") == true) MessageLog.Add("The selected flasks or contents are unavailable; nothing is transferred.");
                return false;
            }
        }
        static bool Parse(string text, out int value) => int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value);
    }
}
