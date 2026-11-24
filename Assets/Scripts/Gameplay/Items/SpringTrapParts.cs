using System;
using System.Collections.Generic;
using System.Globalization;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>
    /// The spring trap (Docs/MACHINE-INVENTED-USES.md 3v): a carried frame of wood and a sprung blade that is loaded with one thing you already carry and laid on a cell. Whoever next steps onto the cell
    /// sets it off, and the load is written into that cell through the same tile-state routines every fire, frost and lightning ability uses (<see cref="ZoneTileStateSystem"/>), so whatever lies there reacts:
    /// oil under a fire moss load ignites (and burns whoever stands in it), water under a frost lichen load freezes, water or brine under a spark root load is charged and shocks. Without something there to catch it,
    /// the load only marks the cell and the trap is spent: so the trap is the way to make a pool, a slick or a spill do something to the next thing that crosses it. Loaded once, laid once, spent when sprung.
    /// <para>The kit holds its load as one string and is not a stack, so differently loaded kits never merge.</para>
    /// </summary>
    public sealed class SpringTrapKitPart : Part, IDescribeState, IDescribeUse
    {
        public override string Name => "SpringTrapKit";

        /// <summary>The cartridge blueprint it is loaded with ("" unloaded) and the energy that writes ("heat", "cold" or "charge"). Saved.</summary>
        public string Loaded = "", Kind = "";

        public const string LoadPrefix = "LoadSpringTrap|", LayPrefix = "LaySpringTrap|", DeviceBlueprint = "SpringTrapLaid";
        /// <summary>What a carried thing loads as: the three energies the tile engine already knows (the same sources the material field actions use).</summary>
        static readonly (string blueprint, string kind, string word)[] Cartridges =
        {
            ("FireMoss", "heat", "fire moss"), ("FrostLichen", "cold", "frost lichen"), ("SparkRoot", "charge", "spark root"),
        };
        static readonly (int x, int y, string label)[] Directions =
        {
            (0,-1,"north"),(1,-1,"northeast"),(1,0,"east"),(1,1,"southeast"),(0,1,"south"),(-1,1,"southwest"),(-1,0,"west"),(-1,-1,"northwest")
        };

        public static bool IsCommand(string command) => command != null && (command.StartsWith(LoadPrefix, StringComparison.Ordinal) || command.StartsWith(LayPrefix, StringComparison.Ordinal));
        public static string WordFor(string kind) { foreach (var c in Cartridges) if (c.kind == kind) return c.word; return ""; }
        internal static string CartridgeFor(string kind) { foreach (var c in Cartridges) if (c.kind == kind) return c.blueprint; return ""; }
        static bool Known(string kind) { foreach (var c in Cartridges) if (c.kind == kind) return true; return false; }

        public ReaderScope Scope => ReaderScope.Both;
        public int Order => 30;

        /// <summary>How to use it, what it needs and costs, from the live rules (the owner, 2026-11-21). The "right now" line is built from <see cref="AddRows"/>'s own rows.</summary>
        public void DescribeUse(DescribeContext context, UseGuide guide)
        {
            guide.How.Add("From the pack choose load the spring trap and pick what to load it with. Then press C on an open cell beside you and choose lay the trap there (or choose it from the pack).");
            guide.How.Add("Whoever next steps on the cell springs it: the load goes into the ground and whatever lies there reacts. Oil under fire moss burns, water under frost lichen freezes, water or brine under spark root shocks; bare ground is only marked.");
            if (Loaded.Length == 0) guide.Needs.Add("one fire moss, frost lichen or spark root in your pack, to load it with");
            else guide.Needs.Add("an empty, passable cell beside you to lay it on, ideally one with oil or water on it (it is loaded with " + WordFor(Kind) + ")");
            guide.Limits.Add("Loading spends the cartridge and springing spends the trap. It does not know who laid it: you spring it too. Fire is not laid on the Choir's ground.");
            guide.How.Add("Before a laid trap springs, stand beside it and choose recover the loaded spring trap. The recovered frame keeps its load for another position.");
            var actor = context.Viewer; if (actor == null) return;
            guide.WhyNot = !UseGuides.CarriedBy(actor, ParentEntity) ? UseGuides.PickUpFirst : actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() == true ? "you cannot act just now" : "no cell beside you is free to lay it on";
        }

        public string DescribeState(DescribeContext context) => Loaded.Length == 0 ? "Not loaded." : "Loaded with " + WordFor(Kind) + ".";

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions") { AddRows(e.GetParameter<Entity>("Actor"), e.GetParameter<Zone>("Zone") ?? e.GetParameter<Entity>("Actor")?.SpatialZone, e.GetParameter<InventoryActionList>("Actions")); return true; }
            if (e.ID == "InventoryAction" && IsCommand(e.GetStringParameter("Command")))
            {
                var actor = e.GetParameter<Entity>("Actor"); var zone = e.GetParameter<Zone>("Zone") ?? actor?.SpatialZone; string command = e.GetStringParameter("Command"); var tx = e.GetParameter<InventoryTransaction>("InventoryTransaction");
                bool done = command.StartsWith(LoadPrefix, StringComparison.Ordinal) ? Load(actor, zone, command, tx) : Lay(actor, zone, command, tx);
                if (done) { e.Handled = true; return false; }
            }
            return true;
        }

        bool Usable(Entity actor, Zone zone) => actor != null && zone != null && ParentEntity != null && WorldResourceActions.ActorCurrent(actor, zone) && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true
            && WorldResourceActions.Carried(actor, ParentEntity, single: true);

        void AddRows(Entity actor, Zone zone, InventoryActionList actions)
        {
            if (actions == null || !Usable(actor, zone)) return;
            var pack = actor.GetPart<InventoryPart>();
            if (Loaded.Length == 0)
            {
                bool any = false;
                foreach (var c in Cartridges)
                {
                    var item = FindCarried(pack, actor, c.blueprint); if (item == null) continue; any = true;
                    actions.AddActionWithHint("LoadSpringTrap", "load the spring trap with " + c.word, LoadPrefix + c.blueprint, "Spends one " + c.word + ". The trap is then " + c.kind + "-loaded and can be laid.", '\0', 18, false, ActionTime.OneAction);
                }
                if (!any) actions.AddDisabled("LoadSpringTrap", "load the spring trap", "nothing-to-load", "It takes fire moss, frost lichen or spark root. You carry none.", 'l', 18);
                return;
            }
            var origin = zone.GetEntityCell(actor); if (origin == null) return;
            bool fireBanned = Kind == "heat" && GroveLaw.IsChoirGround(zone); InventoryAction banned = null;
            foreach (var d in Directions)
            {
                int x = origin.X + d.x, y = origin.Y + d.y; if (!LayableDevicePart.EmptyDestination(zone, origin, x, y)) continue;
                // On the Choir's ground one disabled row says why; the interact menu of each open cell beside you offers that same row, so the refusal is where the player looked. It names no direction for that reason.
                if (fireBanned) { (banned ??= actions.AddDisabled("LaySpringTrap", "lay the " + WordFor(Kind) + " trap", "grove-law", "Fire is not laid on the Choir's ground.", '\0', 17).InGroup(ToolGroups.Lay)).AtCell(x, y); continue; }
                string label = "lay the " + WordFor(Kind) + " trap " + d.label;
                actions.AddActionWithHint("LaySpringTrap", label, LayPrefix + Uri.EscapeDataString(zone.ZoneID ?? "") + "|" + N(origin.X) + "|" + N(origin.Y) + "|" + N(x) + "|" + N(y), "Lays it on that cell. It springs on the next thing to step there, you included, and is spent. Recover it from beside it before it springs to keep the loaded frame.", '\0', 17, false, ActionTime.OneAction).AtCell(x, y).InGroup(ToolGroups.Lay);
            }
        }

        static Entity FindCarried(InventoryPart pack, Entity actor, string blueprint)
        {
            if (pack == null) return null;
            foreach (var o in pack.Objects) if (o != null && o.BlueprintName == blueprint && pack.CanConsumeOne(o) && WorldResourceActions.Carried(actor, o, single: false)) return o;
            return null;
        }

        bool Load(Entity actor, Zone zone, string command, InventoryTransaction tx)
        {
            var pack = actor?.GetPart<InventoryPart>();
            if (!Usable(actor, zone) || pack == null || Loaded.Length != 0) return Block(actor, "invalid-context", "You cannot load that now.");
            string blueprint = command.Substring(LoadPrefix.Length); string kind = null, word = null;
            foreach (var c in Cartridges) if (c.blueprint == blueprint) { kind = c.kind; word = c.word; }
            var item = kind == null ? null : FindCarried(pack, actor, blueprint); if (item == null) return Block(actor, "no-cartridge", "You carry nothing to load it with.");
            bool own = tx == null; tx ??= new InventoryTransaction();
            try
            {
                if (!tx.TryClaim(actor, actor, command) || !tx.TryClaim(ParentEntity, actor, command) || !tx.TryClaim(item, actor, command)) return Block(actor, "in-progress", "That is already being worked.");
                var receipt = InventoryTransferSnapshot.Capture(pack, item); tx.Do(null, receipt.Restore);
                if (!receipt.Apply(() => pack.TryConsumeOne(item)) || !receipt.ClaimChanges(tx, actor, command)) return Block(actor, "payment-refused", "You could not spare it.");
                string loadedBefore = Loaded, kindBefore = Kind;
                tx.Do(() => { Loaded = blueprint; Kind = kind; }, () => { Loaded = loadedBefore; Kind = kindBefore; });
                tx.AfterCommit(() => { MessageLog.Add("You load the spring trap with " + word + "."); Diag.Record("furniture", "SpringTrapLoaded", actor, ParentEntity, new { kind }); });
                if (own) tx.Commit(); return true;
            }
            finally { if (own) tx.Rollback(); }
        }

        bool Lay(Entity actor, Zone zone, string command, InventoryTransaction tx)
        {
            if (!Usable(actor, zone) || Loaded.Length == 0 || !Known(Kind) || HarvestablePart.Factory?.Blueprints.ContainsKey(DeviceBlueprint) != true) return Block(actor, "invalid-context", "You cannot lay that now.");
            return LayAt(actor, zone, command, tx);
        }

        bool LayAt(Entity actor, Zone zone, string command, InventoryTransaction tx)
        {
            string[] f = command.Split('|');
            if (f.Length != 6 || !Int(f[2], out int ox) || !Int(f[3], out int oy) || !Int(f[4], out int x) || !Int(f[5], out int y)) return Block(actor, "malformed-selection", "You cannot lay it there.");
            var origin = zone.GetEntityCell(actor);
            if (WorldResourceActions.Decode(f[1]) != zone.ZoneID || origin == null || origin.X != ox || origin.Y != oy) return Block(actor, "stale-selection", "You cannot lay it there now.");
            if (!LayableDevicePart.EmptyDestination(zone, origin, x, y)) return Block(actor, "occupied-or-distant-ground", "You cannot lay it there.");
            if (Kind == "heat" && GroveLaw.IsChoirGround(zone)) return Block(actor, "grove-law", "Fire is not laid on the Choir's ground.");
            var pack = actor.GetPart<InventoryPart>(); var kit = ParentEntity; string kind = Kind;
            bool own = tx == null; tx ??= new InventoryTransaction();
            try
            {
                if (!tx.TryClaim(actor, actor, command) || !tx.TryClaim(kit, actor, command)) return Block(actor, "in-progress", "That is already being worked.");
                var device = HarvestablePart.Factory.CreateEntity(DeviceBlueprint); var trigger = device?.GetPart<SpringTrapPart>();
                if (device == null || trigger == null || device.SpatialZone != null) return Block(actor, "invalid-device", "You cannot lay that.");
                trigger.Kind = kind; trigger.Armed = false;   // unarmed until the action commits: an uncommitted trap must not spring
                var receipt = InventoryTransferSnapshot.Capture(pack, kit); tx.Do(null, receipt.Restore);
                if (!receipt.Apply(() => pack.RemoveObject(kit)) || !receipt.ClaimChanges(tx, actor, command)) return Block(actor, "payment-refused", "You could not let go of it.");
                tx.Do(null, () => { if (device.SpatialZone == zone) zone.RemoveEntity(device); });
                if (!zone.AddEntity(device, x, y)) return Block(actor, "placement-refused", "You cannot lay it there.");
                tx.AfterCommit(() =>
                {
                    if (device.SpatialZone != zone) return;
                    trigger.Armed = true; ZoneRenderHooks.MarkCellDirty(zone.GetEntityCell(device), "TrapLaid");
                    MessageLog.Add("You lay the " + WordFor(kind) + " trap. It springs on the next thing to step there, you included.");
                    Diag.Record("furniture", "SpringTrapLaid", actor, device, new { kind, x, y });
                });
                if (own) tx.Commit(); return true;
            }
            finally { if (own) tx.Rollback(); }
        }

        bool Block(Entity actor, string code, string message)
        {
            MessageLog.Add(message);
            Diag.Record("furniture", "SpringTrapRefused", actor: actor, target: ParentEntity, payload: new { code }); return false;
        }
        static string N(int n) => n.ToString(CultureInfo.InvariantCulture);
        static bool Int(string s, out int n) => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out n);
    }

    /// <summary>
    /// A laid spring trap (<see cref="SpringTrapKitPart"/>): sprung by the next thing to step onto its cell (anyone: it does not know its layer), it writes its load into that cell through the tile-state routines and
    /// resolves the reactions at once, then is spent. It is armed only after the laying action commits.
    /// </summary>
    public sealed class SpringTrapPart : TriggerOnStepPart, IInventoryActionReceiptOwner, IDescribeUse
    {
        public int Order => 30;
        public bool OwnsInventoryReceipt(string command) => SpringTrapRecovery.IsCommand(command);
        public void DescribeUse(DescribeContext context, UseGuide guide)
        {
            guide.How.Add("Keep off the trap's cell. Stand beside it, press C and choose recover the loaded spring trap to move the frame with its load still in it.");
            guide.Limits.Add("Recovery takes one action and needs room in your pack. A trap that has already sprung is spent.");
        }
        /// <summary>"heat", "cold" or "charge". Saved.</summary>
        public string Kind = "";
        /// <summary>False until the action that laid it commits, so a refused or interrupted laying cannot spring on anyone. Saved (a saved trap is armed).</summary>
        public bool Armed = true;

        public SpringTrapPart() { ConsumeOnTrigger = true; }

        protected override void OnTrigger(Entity actor, Zone zone)
        {
            if (!Armed || ParentEntity == null) return;
            var at = zone.GetEntityCell(ParentEntity); if (at == null) return;
            if (SpringTrapKitPart.CartridgeFor(Kind) == "") return;
            // Mark the cartridge spent before reactions can call back into it.
            // Recovery must distinguish independent firing from its own removal.
            Armed = false;
            switch (Kind)
            {
                case "heat": ZoneTileStateSystem.AddHeat(zone, at.X, at.Y, 1, actor, "SpringTrap"); break;
                case "cold": ZoneTileStateSystem.AddCold(zone, at.X, at.Y, 1, actor, "SpringTrap"); break;
                case "charge": ZoneTileStateSystem.AddCharge(zone, at.X, at.Y, 1, actor, "SpringTrap"); break;
                default: return;
            }
            ZoneTileStateSystem.ResolveAfterAbility(zone, actor);
            MessageLog.Add("A spring trap goes off under " + (actor?.GetDisplayName() ?? "something") + ".");
            Diag.Record("furniture", "SpringTrapSprung", actor, ParentEntity, new { kind = Kind, x = at.X, y = at.Y });
        }

        public override bool HandleEvent(GameEvent e)
        {
            if (e.ID == "GetInventoryActions")
            { var who = e.GetParameter<Entity>("Actor"); SpringTrapRecovery.AddRows(who, this, e.GetParameter<Zone>("Zone") ?? who?.SpatialZone, e.GetParameter<InventoryActionList>("Actions")); return true; }
            if (e.ID == "InventoryAction" && SpringTrapRecovery.IsCommand(e.GetStringParameter("Command")))
            {
                var who = e.GetParameter<Entity>("Actor");
                if (!SpringTrapRecovery.TryAct(who, this, e.GetParameter<Zone>("Zone") ?? who?.SpatialZone, e.GetStringParameter("Command"), e.GetParameter<InventoryTransaction>("InventoryTransaction"))) return true;
                e.Handled = true; return false;
            }
            if (!Armed && e.ID == "EntityEnteredCell") return true;   // an unarmed trap does nothing and is not consumed
            return base.HandleEvent(e);
        }
    }
}
