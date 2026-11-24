using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>An explicit finite brine batch. No salt item or standing waste
    /// pool is created; discarded concentrate is part of the quoted loss.</summary>
    internal static class BrineDistillation
    {
        internal const int Feed = 4, Water = 2, Fuel = 5;
        const string Prefix = "DistillBrine|";
        sealed class Identity { internal readonly string Value = Guid.NewGuid().ToString("N"); }
        static readonly ConditionalWeakTable<object, Identity> Identities = new ConditionalWeakTable<object, Identity>();
        static string Key(object value) => value == null ? "-" : Identities.GetValue(value, _ => new Identity()).Value;
        internal static bool IsCommand(string command) => command?.StartsWith(Prefix, StringComparison.Ordinal) == true;

        sealed class Owner
        {
            internal readonly Entity Entity;
            readonly string id;
            readonly Part[] parts;
            internal Owner(Entity entity) { Entity = entity; id = entity.ID; parts = entity.Parts.ToArray(); }
            internal bool Current() => Entity.ID == id && Entity.Parts.SequenceEqual(parts) && parts.All(p => p.ParentEntity == Entity);
            internal string Stamp => Uri.EscapeDataString(id) + ":" + Key(Entity) + ":" + string.Join(",", parts.Select(Key));
        }
        sealed class Plan
        {
            internal readonly Entity Actor, Source, Receiver, Fire;
            internal readonly LiquidSettlingPart Still;
            internal readonly Zone Zone;
            internal readonly LiquidVesselPart Input, Output;
            internal readonly FuelPart FuelPart;
            readonly Owner actorOwner, stillOwner, sourceOwner, receiverOwner, fireOwner;
            readonly InventoryPart pack;
            readonly Cell origin, stationCell, fireCell;
            readonly ThermalPart heat;
            readonly int volume, capacity, receiverCapacity;
            readonly float fuel, maximum, temperature;
            internal Plan(Entity actor, LiquidSettlingPart still, Zone zone, Entity source, Entity receiver, Entity fire)
            {
                Actor = actor; Still = still; Zone = zone; Source = source; Receiver = receiver; Fire = fire;
                actorOwner = new Owner(actor); stillOwner = new Owner(still.ParentEntity);
                sourceOwner = new Owner(source); receiverOwner = new Owner(receiver); fireOwner = new Owner(fire);
                pack = actor.GetPart<InventoryPart>(); origin = zone.GetEntityCell(actor);
                stationCell = zone.GetEntityCell(still.ParentEntity); fireCell = zone.GetEntityCell(fire);
                Input = source.GetPart<LiquidVesselPart>(); Output = receiver.GetPart<LiquidVesselPart>();
                FuelPart = fire.GetPart<FuelPart>(); heat = fire.GetPart<ThermalPart>();
                volume = Input.Volume; capacity = Input.Capacity; receiverCapacity = Output.Capacity;
                fuel = FuelPart.FuelMass; maximum = FuelPart.MaxFuel; temperature = heat.Temperature;
            }
            internal bool Current(bool paid)
            {
                int left = volume - (paid ? Feed : 0);
                return Context(Actor, Still, Zone) && actorOwner.Current() && stillOwner.Current()
                    && sourceOwner.Current() && receiverOwner.Current() && fireOwner.Current()
                    && Actor.GetPart<InventoryPart>() == pack && Zone.GetEntityCell(Actor) == origin
                    && Zone.GetEntityCell(Still.ParentEntity) == stationCell && Zone.GetEntityCell(Fire) == fireCell
                    && Vessel(Actor, Source) && Vessel(Actor, Receiver) && Source != Receiver
                    && Source.GetPart<LiquidVesselPart>() == Input && Receiver.GetPart<LiquidVesselPart>() == Output
                    && Input.Capacity == capacity && Input.Volume == left && Input.LiquidId == (left == 0 ? "" : "brine")
                    && Output.Capacity == receiverCapacity && Output.Volume == (paid ? Water : 0) && Output.LiquidId == (paid ? "water" : "")
                    && Fire.GetPart<FuelPart>() == FuelPart && Fire.GetPart<ThermalPart>() == heat
                    && WorldResourceActions.Ground(Fire, Zone) && FuelPart.MaxFuel == maximum
                    && FuelPart.FuelMass == fuel - (paid ? Fuel : 0) && heat.Temperature == temperature
                    && WorldResourceActions.Finite(fuel) && WorldResourceActions.Finite(maximum) && maximum >= fuel
                    && (double)fuel - (float)(fuel - Fuel) == Fuel
                    && Still.DistillationFireReady(Zone, Fire, paid ? 0 : Fuel);
            }
            internal string Command
            {
                get
                {
                    string text = string.Join("/", actorOwner.Stamp, stillOwner.Stamp, sourceOwner.Stamp, receiverOwner.Stamp, fireOwner.Stamp,
                        Key(Zone), origin.X, origin.Y, stationCell.X, stationCell.Y, fireCell.X, fireCell.Y,
                        volume, capacity, receiverCapacity, fuel.ToString("R", CultureInfo.InvariantCulture),
                        maximum.ToString("R", CultureInfo.InvariantCulture), temperature.ToString("R", CultureInfo.InvariantCulture));
                    using var hash = SHA256.Create();
                    string stamp = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "");
                    return Prefix + Uri.EscapeDataString(Source.ID) + "|" + Uri.EscapeDataString(Receiver.ID) + "|" + Uri.EscapeDataString(Fire.ID) + "|" + stamp;
                }
            }
        }
        static bool Context(Entity actor, LiquidSettlingPart still, Zone zone) => actor != null && still?.ParentEntity != null && zone != null
            && WorldResourceActions.ActorCurrent(actor, zone) && actor.GetStatValue("Hitpoints") > 0 && !CombatSystem.IsDeathHandled(actor)
            && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true && actor.GetPart<InventoryPart>()?.ParentEntity == actor
            && still.ParentEntity.GetPart<LiquidSettlingPart>() == still && still.DistillationStanding
            && WorldResourceActions.Ground(still.ParentEntity, zone) && WorldResourceActions.Nearby(actor, still.ParentEntity, zone);
        static bool Vessel(Entity actor, Entity item) => WorldResourceActions.Carried(actor, item, true)
            && item.Parts.Count(p => p is LiquidVesselPart) == 1 && item.GetPart<LiquidVesselPart>()?.ParentEntity == item
            && item.GetPart<LiquidVesselPart>().Capacity > 0 && !item.HasTag("Creature") && !item.HasTag("Terrain");
        static Plan Make(Entity actor, LiquidSettlingPart still, Zone zone, Entity source, Entity receiver, Entity fire)
        {
            if (!Context(actor, still, zone) || source == receiver || !Vessel(actor, source) || !Vessel(actor, receiver)
                || source.ID == receiver.ID || source.ID == actor.ID || receiver.ID == actor.ID
                || actor.GetPart<InventoryPart>().Objects.Count(e => e?.ID == source.ID) != 1
                || actor.GetPart<InventoryPart>().Objects.Count(e => e?.ID == receiver.ID) != 1
                || source.GetPart<LiquidVesselPart>().LiquidId != "brine" || source.GetPart<LiquidVesselPart>().Volume < Feed
                || source.GetPart<LiquidVesselPart>().Volume > source.GetPart<LiquidVesselPart>().Capacity
                || receiver.GetPart<LiquidVesselPart>().Volume != 0 || receiver.GetPart<LiquidVesselPart>().LiquidId != ""
                || receiver.GetPart<LiquidVesselPart>().Capacity < Water || !still.DistillationFireReady(zone, fire, Fuel)) return null;
            var plan = new Plan(actor, still, zone, source, receiver, fire); return plan.Current(false) ? plan : null;
        }
        internal static void AddRows(Entity actor, LiquidSettlingPart still, Zone zone, InventoryActionList actions)
        {
            if (actions == null || !Context(actor, still, zone)) return;
            var items = actor.GetPart<InventoryPart>().Objects.Where(e => Vessel(actor, e)).OrderBy(e => e.ID, StringComparer.Ordinal).ToArray();
            var fire = still.DistillationFire(zone); bool any = false;
            var sources = items.Where(e => e.GetPart<LiquidVesselPart>().LiquidId == "brine").ToArray();
            var receivers = items.Where(e => e.GetPart<LiquidVesselPart>().Volume == 0).ToArray();
            foreach (var source in sources)
                foreach (var receiver in receivers)
                {
                    var plan = Make(actor, still, zone, source, receiver, fire); if (plan == null) continue; any = true;
                    string from = "brine flask " + (Array.IndexOf(sources, source) + 1) + " (" + plan.Input.Volume + "/" + plan.Input.Capacity + ")";
                    string to = "empty flask " + (Array.IndexOf(receivers, receiver) + 1) + " (0/" + plan.Output.Capacity + ")";
                    actions.AddActionWithHint("DistillBrine", "distil " + from + " into " + to + " (2 water; 5 fuel)", plan.Command,
                        "Uses 4 brine from " + from + " and receives 2 water in " + to + ". Two units of concentrate are discarded; no salt is recovered.", '\0', 7, false, ActionTime.OneAction);
                }
            if (!any) actions.AddDisabled("DistillBrine", "distill brine into water", "needs-distillation-inputs",
                "Needs 4 carried brine, a separate empty single flask, and a hot fire within 2 cells with 5 fuel. Makes 2 water; concentrate is discarded.", '\0', 7);
        }
        internal static bool TryAct(Entity actor, LiquidSettlingPart still, Zone zone, string command, InventoryTransaction tx)
        {
            if (!IsCommand(command) || tx == null || !Context(actor, still, zone)) return Reject(actor, still?.ParentEntity);
            var fields = command.Split('|'); if (fields.Length != 5) return Reject(actor, still.ParentEntity);
            var source = WorldResourceActions.ExactCarried(actor, fields[1]); var receiver = WorldResourceActions.ExactCarried(actor, fields[2]);
            var fire = WorldResourceActions.ExactGround(zone, fields[3]);
            var plan = Make(actor, still, zone, source, receiver, fire);
            if (plan == null || plan.Command != command || !tx.TryClaimAction(actor)) return Reject(actor, still.ParentEntity);
            foreach (var owner in new[] { actor, still.ParentEntity, source, receiver, fire })
                if (!tx.TryClaim(owner, actor, command)) return Reject(actor, still.ParentEntity);
            var input = plan.Input; var output = plan.Output; var fuel = plan.FuelPart;
            tx.Do(() => { input.Volume -= Feed; if (input.Volume == 0) input.LiquidId = ""; output.Volume = Water; output.LiquidId = "water"; fuel.FuelMass -= Fuel; }, () =>
            {
                UndoLiquid(source, input, "brine", -Feed, actor); UndoLiquid(receiver, output, "water", Water, actor);
                if (fire.GetPart<FuelPart>() == fuel && fuel.ParentEntity == fire && WorldResourceActions.Finite(fuel.FuelMass)
                    && WorldResourceActions.Finite(fuel.MaxFuel) && fuel.MaxFuel >= 0)
                    fuel.FuelMass = (float)Math.Max(0, Math.Min(fuel.MaxFuel, (double)fuel.FuelMass + Fuel));
            });
            tx.BeforeCommit(() => plan.Current(true));
            tx.AfterCommit(() => MessageLog.Add("You distil 4 brine into 2 clean water. The fire spends 5 fuel; concentrate is discarded."));
            tx.AfterCommit(() => Diag.Record("furniture", "BrineDistilled", actor, still.ParentEntity, new { feed = Feed, water = Water, fuel = Fuel }));
            return true;
        }
        static void UndoLiquid(Entity owner, LiquidVesselPart part, string liquid, int delta, Entity actor)
        {
            if (owner.GetPart<LiquidVesselPart>() == part && part.ParentEntity == owner && part.Capacity > 0
                && (part.LiquidId == liquid || part.Volume == 0 && part.LiquidId == ""))
            { part.Volume = WaterQuantityUndo.Restore(part.Volume, delta, part.Capacity); part.LiquidId = part.Volume == 0 ? "" : liquid; }
            else Diag.Record("liquid", "DistillationUndoIdentityChanged", actor, owner);
        }
        static bool Reject(Entity actor, Entity still)
        { MessageLog.Add("The still, selected flasks or fuel changed. Choose the batch again."); Diag.Record("furniture", "BrineDistillationRefused", actor, still); return false; }
    }
}
