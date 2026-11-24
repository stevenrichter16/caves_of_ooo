using System;
using System.Globalization;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// A carried thing that is laid on the ground (Docs/MACHINE-INVENTED-USES.md 3t): a hush strip, a spring trap. The capability is generic: the item names the device blueprint it becomes; "lay" puts one of the stack
    /// on an empty, passable cell beside you (one device to a cell); the cost is the thing itself, paid inside the action and given back whole if the action fails. What the device then does is the device's own Part
    /// (<see cref="HushStripPart"/>, a spring trap's trigger), so any later laid thing is one blueprint and one Part, with no edit here. Modelled on the cord snare's laying (<see cref="CordSnareActions"/>), without its one-off checks.
    /// </summary>
    public sealed class LayableDevicePart : Part, IDescribeState, IDescribeUse
    {
        public override string Name => "LayableDevice";

        /// <summary>The device blueprint this item becomes, and what the player calls it ("hush strip").</summary>
        public string Device = "", Label = "device";

        /// <summary>What the device does once laid, in one sentence of the thing's own words; shown in the item's "how to use" block so the player knows what laying it is for.</summary>
        public string Effect = "";

        public const string Prefix = "LayDevice|";
        static readonly (int x, int y, string label)[] Directions =
        {
            (0,-1,"north"),(1,-1,"northeast"),(1,0,"east"),(1,1,"southeast"),(0,1,"south"),(-1,1,"southwest"),(-1,0,"west"),(-1,-1,"northwest")
        };

        public static bool IsCommand(string command) => command != null && command.StartsWith(Prefix, StringComparison.Ordinal);

        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 30;
        public string DescribeState(DescribeContext context) => "Laid on the ground beside you, one " + Label + " to a cell.";

        /// <summary>How to use it, from the live rules (the owner, 2026-11-21). The "right now" line is built from <see cref="AddRows"/>'s own rows.</summary>
        public void DescribeUse(DescribeContext context, UseGuide guide)
        {
            guide.How.Add("Press C on an open cell beside you and choose lay " + Label + " (or choose it from the pack)." + (Recoverable() ? " Press C beside a laid one and choose take up to carry it again." : ""));
            guide.Needs.Add("an empty, passable cell beside you, with no creature, solid thing or other laid device on it");
            guide.Limits.Add("One " + Label + " to a cell, one from the stack each time." + (Recoverable() ? "" : " Once laid it is spent.") + (string.IsNullOrEmpty(Effect) ? "" : " " + Effect));
            var actor = context.Viewer; if (actor == null) return;
            guide.WhyNot = !UseGuides.CarriedBy(actor, ParentEntity) ? UseGuides.PickUpFirst : actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true ? "you cannot act just now" : "no cell beside you is free to lay one on";
        }

        // ── rows ──

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions") { AddRows(e.GetParameter<Entity>("Actor"), e.GetParameter<Zone>("Zone") ?? e.GetParameter<Entity>("Actor")?.SpatialZone, e.GetParameter<InventoryActionList>("Actions")); return true; }
            if (e.ID == "InventoryAction" && IsCommand(e.GetStringParameter("Command")))
            {
                var actor = e.GetParameter<Entity>("Actor"); var zone = e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone;
                if (Lay(actor, zone, e.GetStringParameter("Command"), e.GetParameter<InventoryTransaction>("InventoryTransaction"))) { e.Handled = true; return false; }
            }
            return true;
        }

        string Invalid(Entity actor, Zone zone)
        {
            if (actor == null || zone == null || ParentEntity == null) return "no-context";
            if (!WorldResourceActions.ActorCurrent(actor, zone) || actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true) return "actor-unavailable";
            if (!WorldResourceActions.Carried(actor, ParentEntity, single: false)) return "not-carried";
            if (string.IsNullOrEmpty(Device) || HarvestablePart.Factory?.Blueprints.ContainsKey(Device) != true) return "missing-device-definition";
            return null;
        }

        void AddRows(Entity actor, Zone zone, InventoryActionList actions)
        {
            if (actions == null || Invalid(actor, zone) != null) return;
            var origin = zone.GetEntityCell(actor); if (origin == null) return;
            foreach (var d in Directions)
            {
                int x = origin.X + d.x, y = origin.Y + d.y; if (!EmptyDestination(zone, origin, x, y)) continue;
                string command = Prefix + Uri.EscapeDataString(zone.ZoneID ?? "") + "|" + N(origin.X) + "|" + N(origin.Y) + "|" + N(x) + "|" + N(y) + "|" + Uri.EscapeDataString(ParentEntity.ID);
                actions.AddActionWithHint("LayDevice", "lay " + Label + " " + d.label, command, "Lays one " + Label + " on that cell. You can take it up again from beside it" + (Recoverable() ? "." : ": no, once laid it is spent."), '\0', 18, false, ActionTime.OneAction).AtCell(x, y).InGroup(ToolGroups.Lay);
            }
        }

        bool Recoverable()
        {
            var made = HarvestablePart.Factory?.GetBlueprint(Device);
            return made != null && made.Parts.TryGetValue("LaidDevice", out var p) && p.TryGetValue("ItemBlueprint", out var item) && !string.IsNullOrEmpty(item);
        }

        /// <summary>A passable cell beside the actor holding nothing but ground: no creature, no solid thing, no other laid device.</summary>
        internal static bool EmptyDestination(Zone zone, Cell origin, int x, int y)
        {
            if (zone == null || origin == null || Math.Abs((long)x - origin.X) > 1 || Math.Abs((long)y - origin.Y) > 1 || (x == origin.X && y == origin.Y)) return false;
            var cell = zone.GetCell(x, y);
            if (cell == null || !cell.IsPassable()) return false;
            foreach (var e in cell.Occupants) if (e == null || !e.HasTag("Terrain") || e.HasTag("Creature") || e.HasPart<LaidDevicePart>()) return false;
            return true;
        }

        // ── laying: one action; the item and the device come back together on any failure ──

        bool Lay(Entity actor, Zone zone, string command, InventoryTransaction tx)
        {
            string invalid = Invalid(actor, zone); if (invalid != null) return Reject(actor, command, invalid);
            string[] f = command.Split('|');
            if (f.Length != 7 || !Int(f[2], out int ox) || !Int(f[3], out int oy) || !Int(f[4], out int x) || !Int(f[5], out int y)) return Reject(actor, command, "malformed-selection");
            var origin = zone.GetEntityCell(actor);
            if (WorldResourceActions.Decode(f[1]) != zone.ZoneID || origin == null || origin.X != ox || origin.Y != oy || WorldResourceActions.Decode(f[6]) != ParentEntity.ID) return Reject(actor, command, "stale-selection");
            if (!EmptyDestination(zone, origin, x, y)) return Reject(actor, command, "occupied-or-distant-ground");
            var pack = actor.GetPart<InventoryPart>(); var item = ParentEntity;
            bool own = tx == null; tx ??= new InventoryTransaction();
            try
            {
                if (!tx.TryClaim(actor, actor, command) || !tx.TryClaim(item, actor, command)) return Reject(actor, command, "in-progress");
                var device = HarvestablePart.Factory.CreateEntity(Device);
                if (device == null || device.GetPart<LaidDevicePart>() == null || device.SpatialZone != null) return Reject(actor, command, "invalid-device");
                var receipt = InventoryTransferSnapshot.Capture(pack, item);
                tx.Do(null, receipt.Restore);
                if (!receipt.Apply(() => pack.TryConsumeOne(item)) || !receipt.ClaimChanges(tx, actor, command)) return Reject(actor, command, "payment-refused");
                tx.Do(null, () => { if (device.SpatialZone == zone) zone.RemoveEntity(device); });
                if (!zone.AddEntity(device, x, y)) return Reject(actor, command, "placement-refused");
                string label = Label, itemName = item.GetDisplayName(); string deviceBlueprint = Device;
                tx.AfterCommit(() =>
                {
                    ZoneRenderHooks.MarkCellDirty(zone.GetEntityCell(device), "DeviceLaid");
                    MessageLog.Add("You lay a " + label + " on the ground.");
                    Diag.Record("furniture", "DeviceLaid", actor, device, new { device = deviceBlueprint, x, y });
                });
                if (own) tx.Commit();
                return true;
            }
            finally { if (own) tx.Rollback(); }
        }

        static bool Reject(Entity actor, string command, string reason)
        {
            Diag.Record("furniture", "DeviceLayRefused", actor: actor, payload: new { command, reason });
            MessageLog.Add("You cannot lay that here (" + reason.Replace('-', ' ') + ")."); return false;
        }
        static string N(int n) => n.ToString(CultureInfo.InvariantCulture);
        static bool Int(string s, out int n) => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out n);
    }

    /// <summary>
    /// What a laid device is (the Part every <see cref="LayableDevicePart"/> device carries): its own label, and, when it can be picked up again, the item it goes back to. A device with no item is spent for good.
    /// Taking one up is one action from beside it; the item is made fresh and the device leaves the world together, or neither happens.
    /// </summary>
    public sealed class LaidDevicePart : Part, IDescribeState, IDescribeUse
    {
        public override string Name => "LaidDevice";
        public string ItemBlueprint = "", Label = "device";
        public const string TakeUpCommand = "TakeUpDevice";

        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 30;
        public string DescribeState(DescribeContext context) => SpringTrapRecovery.Supported(ParentEntity)
            ? "Armed spring trap. Recover it from beside it before it springs to keep the loaded frame."
            : string.IsNullOrEmpty(ItemBlueprint) ? "Laid for good: it cannot be taken up again." : "Laid on the ground. You can take it up from beside it.";

        /// <summary>How to take it up, or why you cannot (the owner, 2026-11-21). The "right now" line is built from the take-up row.</summary>
        public void DescribeUse(DescribeContext context, UseGuide guide)
        {
            if (SpringTrapRecovery.Supported(ParentEntity)) { guide.How.Add("Stand beside the armed spring trap and choose recover the loaded spring trap; its load stays in the frame."); return; }
            if (string.IsNullOrEmpty(ItemBlueprint)) { guide.Limits.Add("Once laid it stays: it cannot be taken up again."); return; }
            guide.How.Add("Stand beside it, press C on it and choose take up the " + Label + "; it comes back to your pack to lay elsewhere.");
            guide.Needs.Add("you beside it, with room in your pack");
            guide.WhyNot = "you are not beside it";
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            {
                var actor = e.GetParameter<Entity>("Actor"); var zone = e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone; var actions = e.GetParameter<InventoryActionList>("Actions");
                if (actions != null && !string.IsNullOrEmpty(ItemBlueprint) && actor != null && zone != null && WorldResourceActions.Nearby(actor, ParentEntity, zone))
                    actions.AddActionWithHint("TakeUpDevice", "take up the " + Label, TakeUpCommand, "Picks it up again; it is yours to lay elsewhere.", 't', 19, false, ActionTime.OneAction);
                return true;
            }
            if (e.ID == "InventoryAction" && e.GetStringParameter("Command") == TakeUpCommand)
            {
                var actor = e.GetParameter<Entity>("Actor"); var zone = e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone;
                if (TakeUp(actor, zone, e.GetParameter<InventoryTransaction>("InventoryTransaction"))) { e.Handled = true; return false; }
            }
            return true;
        }

        bool TakeUp(Entity actor, Zone zone, InventoryTransaction tx)
        {
            var pack = actor?.GetPart<InventoryPart>(); var device = ParentEntity;
            if (actor == null || zone == null || pack == null || string.IsNullOrEmpty(ItemBlueprint) || !WorldResourceActions.Nearby(actor, device, zone) || !WorldResourceActions.ActorCurrent(actor, zone))
                return Block(actor, "invalid-context");
            var at = zone.GetEntityCell(device); if (at == null) return Block(actor, "invalid-context");
            var item = HarvestablePart.Factory?.CreateEntity(ItemBlueprint); if (item == null) return Block(actor, "missing-item-definition");
            bool own = tx == null; tx ??= new InventoryTransaction();
            try
            {
                if (!tx.TryClaim(actor, actor, TakeUpCommand) || !tx.TryClaim(device, actor, TakeUpCommand)) return Block(actor, "in-progress");
                var receipt = InventoryTransferSnapshot.Capture(pack, item);
                tx.Do(null, receipt.Restore);
                if (!receipt.Apply(() => pack.AddObject(item)) || !receipt.ClaimChanges(tx, actor, TakeUpCommand)) return Block(actor, "no-room");
                int x = at.X, y = at.Y; string label = Label;
                tx.Do(() => zone.RemoveEntity(device), () => { if (device.SpatialZone == null) zone.AddEntity(device, x, y); });
                tx.AfterCommit(() =>
                {
                    ZoneRenderHooks.MarkCellDirty(zone.GetCell(x, y), "DeviceTakenUp");
                    MessageLog.Add("You take up the " + label + ".");
                    Diag.Record("furniture", "DeviceTakenUp", actor, null, new { x, y, item = item.BlueprintName });
                });
                if (own) tx.Commit();
                return true;
            }
            finally { if (own) tx.Rollback(); }
        }

        static bool Block(Entity actor, string code)
        {
            MessageLog.Add("You cannot take that up (" + code.Replace('-', ' ') + ").");
            Diag.Record("furniture", "DeviceTakeUpRefused", actor: actor, payload: new { code }); return false;
        }
    }

    /// <summary>
    /// A hush strip (Docs/MACHINE-INVENTED-USES.md 3t): felt over cord, laid flat. The footfall of whoever steps onto its cell is not heard: <see cref="Noise.MarkStep"/> skips the stamp, so an eyeless hunter that perceives by sound
    /// (<see cref="HuntsBySoundPart"/>, the pale stalkers of the deep) does not perceive a thing that only walked on it. It deadens nothing else: a blow, a cast and the sounding rod still ring, and the next step off the strip is heard
    /// again. It is a marker; the rule lives in one place (<see cref="Noise.MarkStep"/>).
    /// </summary>
    public sealed class HushStripPart : Part, IDescribeState
    {
        public override string Name => "HushStrip";
        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 25;
        public string DescribeState(DescribeContext context) => "Felt over cord, laid flat. Footfalls on it are not heard. Blows, spells and the sounding rod still are, and the next step off it is heard again.";
    }
}
