using System;
using CavesOfOoo.Core.Inventory;
namespace CavesOfOoo.Core
{
    internal static class WaterTransferActions
    {
        const string Prefix = "TransferWater|";
        internal static bool IsCommand(string command) => command?.StartsWith(Prefix, StringComparison.Ordinal) == true;
        internal static bool Vessel(Entity actor, Entity owner, out int units, out int capacity)
        {
            units = capacity = 0; if (!WorldResourceActions.Carried(actor, owner)) return false;
            var skin = owner.GetPart<WaterskinPart>(); var liquid = owner.GetPart<LiquidVesselPart>();
            if ((skin == null) == (liquid == null)) return false;
            if (skin != null) { if (skin.ParentEntity != owner) return false; units = skin.Charges; capacity = skin.Capacity; }
            else { if (liquid.ParentEntity != owner || (liquid.Volume == 0 ? liquid.LiquidId != "" : liquid.LiquidId != "water")) return false; units = liquid.Volume; capacity = liquid.Capacity; }
            return capacity > 0 && units >= 0 && units <= capacity;
        }
        internal static void SetUnits(Entity owner, int units)
        {
            var skin = owner.GetPart<WaterskinPart>(); if (skin != null) skin.Charges = units;
            else { var liquid = owner.GetPart<LiquidVesselPart>(); liquid.Volume = units; liquid.LiquidId = units == 0 ? "" : "water"; }
        }
        internal static void AddActions(Entity actor, Entity source, Zone zone, InventoryActionList actions)
        {
            if (actions == null || !WorldResourceActions.ActorCurrent(actor, zone) || !Vessel(actor, source, out int amount, out _) || amount == 0) return;
            foreach (var other in actor.GetPart<InventoryPart>().Objects)
                if (other != source && Vessel(actor, other, out int filled, out int capacity) && filled < capacity)
                    actions.AddAction("TransferWater", "transfer water to " + other.GetDisplayName() + " (" + Math.Min(amount, capacity - filled) + ")",
                        Prefix + Uri.EscapeDataString(other.ID), '\0', 17);
        }
        internal static bool TryAct(Entity actor, Entity source, Zone zone, string command, InventoryTransaction tx)
        {
            if (!IsCommand(command)) return false;
            if (!WorldResourceActions.ActorCurrent(actor, zone)) return WorldResourceActions.Reject(actor, source, command, "invalid_actor_context");
            var fields = command.Split('|'); if (fields.Length != 2) return WorldResourceActions.Reject(actor, source, command, "malformed_selection");
            var target = WorldResourceActions.ExactCarried(actor, fields[1]);
            if (source == target || !Vessel(actor, source, out int from, out _) || !Vessel(actor, target, out int to, out int cap) || from == 0 || to == cap) return WorldResourceActions.Reject(actor, target, command, "invalid_or_unavailable_vessels");
            bool own = tx == null; tx ??= new InventoryTransaction();
            try
            {
                if (!tx.TryClaim(actor, actor, command) || !tx.TryClaim(source, actor, command) || !tx.TryClaim(target, actor, command)) return false;
                int amount = Math.Min(from, cap - to);
                tx.Do(() => { SetUnits(source, from - amount); SetUnits(target, to + amount); }, () => { SetUnits(source, from); SetUnits(target, to); });
                tx.AfterCommit(() => MessageLog.Add("You transfer " + amount + " water to " + target.GetDisplayName() + "."));
                if (own) tx.Commit(); return true;
            }
            finally { if (own) tx.Rollback(); }
        }
    }
}
