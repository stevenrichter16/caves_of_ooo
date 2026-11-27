using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using CavesOfOoo.Data;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>One finite brine batch with a choice of what to retain.</summary>
    internal static class BrineDistillation
    {
        internal const int Feed = 4, Water = 2, Fuel = 5;
        const int CrustWater = 1;
        const string WaterAction = "DistillBrine", CrustAction = "DistillBrineKeepCrust";
        const string CrustBlueprint = "BrineCrust";
        static readonly (string Name, Type Type)[] CrustParts =
        {
            ("Render", typeof(RenderPart)), ("Physics", typeof(PhysicsPart)), ("Examinable", typeof(ExaminablePart)),
            ("Commerce", typeof(CommercePart)), ("Stacker", typeof(StackerPart)), ("Reagent", typeof(ReagentPart))
        };
        sealed class Identity { internal readonly string Value = Guid.NewGuid().ToString("N"); }
        static readonly ConditionalWeakTable<object, Identity> Identities = new ConditionalWeakTable<object, Identity>();
        static string Key(object value) => value == null ? "-" : Identities.GetValue(value, _ => new Identity()).Value;
        internal static bool IsCommand(string command) => IsCrustCommand(command)
            || command?.StartsWith(WaterAction + "|", StringComparison.Ordinal) == true;
        static bool IsCrustCommand(string command) => command?.StartsWith(CrustAction + "|", StringComparison.Ordinal) == true;

        static Blueprint QualifiedCrust(EntityFactory factory)
        {
            if (factory?.Blueprints.TryGetValue(CrustBlueprint, out var blueprint) != true || blueprint == null
                || blueprint.Name != CrustBlueprint || !MaterialProcessRegistry.Portable(factory, CrustBlueprint)
                || !blueprint.Tags.ContainsKey("NoTrade") || blueprint.Parts.Count != CrustParts.Length
                || CrustParts.Any(part => !blueprint.Parts.ContainsKey(part.Name)
                    || !factory.TryGetRegisteredPartType(part.Name, out var type) || type != part.Type)
                || !Parameter("Physics", "Weight", "1") || !Parameter("Commerce", "Value", "0")
                || !Parameter("Reagent", "PropertiesRaw", "conductive:1")) return null;
            return blueprint;

            bool Parameter(string part, string key, string expected) =>
                blueprint.Parts[part]?.TryGetValue(key, out string value) == true && value == expected;
        }

        static bool ClearCrustGround(Entity actor, Zone zone, Cell cell, Entity staged = null)
            => cell != null && cell.ParentZone == zone && zone.GetCell(cell.X, cell.Y) == cell
                && cell.Explored && cell.IsVisible && !cell.BlocksMovement(actor)
                && !cell.Occupants.Any(item => item != actor && item != staged
                    && (!item.HasTag("Terrain") || item.GetPart<LiquidPoolPart>() is { Volume: > 0 }))
                && zone.TileState.Get(cell.X, cell.Y)?.Coatings.Any(layer => layer.Turns > 0) != true;

        static bool CanonicalCrust(Entity crust)
            => crust?.BlueprintName == CrustBlueprint && !string.IsNullOrEmpty(crust.ID)
                && crust.HasTag("Item") && crust.HasTag("NoTrade") && !crust.HasTag("Barren")
                && !crust.HasTag("Creature") && !crust.HasTag("Terrain") && !crust.HasTag("Solid")
                && crust.Parts.Count == CrustParts.Length && crust.Parts.All(part => part.ParentEntity == crust)
                && CrustParts.All(part => crust.Parts.Count(actual => actual.GetType() == part.Type) == 1)
                && crust.GetPart<PhysicsPart>() is { Weight: 1, Takeable: true, Solid: false, InInventory: null, Equipped: null }
                && crust.GetPart<StackerPart>()?.StackCount == 1 && crust.GetPart<CommercePart>()?.Value == 0
                && crust.GetPart<ReagentPart>()?.PropertiesRaw == "conductive:1";

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
            internal int WaterYield => KeepsCrust ? CrustWater : Water;
            internal bool KeepsCrust => crustTemplate != null;
            readonly Owner actorOwner, stillOwner, sourceOwner, receiverOwner, fireOwner;
            readonly EntityFactory crustFactory;
            readonly Blueprint crustTemplate;
            readonly InventoryPart pack;
            readonly Cell origin, stationCell, fireCell;
            readonly ThermalPart heat;
            readonly int volume, capacity, receiverCapacity;
            readonly float fuel, maximum, temperature;
            internal Plan(Entity actor, LiquidSettlingPart still, Zone zone, Entity source, Entity receiver, Entity fire,
                EntityFactory crustFactory, Blueprint crustTemplate)
            {
                Actor = actor; Still = still; Zone = zone; Source = source; Receiver = receiver; Fire = fire;
                this.crustFactory = crustFactory;
                this.crustTemplate = crustTemplate;
                actorOwner = new Owner(actor); stillOwner = new Owner(still.ParentEntity);
                sourceOwner = new Owner(source); receiverOwner = new Owner(receiver); fireOwner = new Owner(fire);
                pack = actor.GetPart<InventoryPart>(); origin = zone.GetEntityCell(actor);
                stationCell = zone.GetEntityCell(still.ParentEntity); fireCell = zone.GetEntityCell(fire);
                Input = source.GetPart<LiquidVesselPart>(); Output = receiver.GetPart<LiquidVesselPart>();
                FuelPart = fire.GetPart<FuelPart>(); heat = fire.GetPart<ThermalPart>();
                volume = Input.Volume; capacity = Input.Capacity; receiverCapacity = Output.Capacity;
                fuel = FuelPart.FuelMass; maximum = FuelPart.MaxFuel; temperature = heat.Temperature;
            }
            internal bool Current(bool paid, Entity crust = null)
            {
                int left = volume - (paid ? Feed : 0);
                return Context(Actor, Still, Zone) && actorOwner.Current() && stillOwner.Current()
                    && sourceOwner.Current() && receiverOwner.Current() && fireOwner.Current()
                    && Actor.GetPart<InventoryPart>() == pack && Zone.GetEntityCell(Actor) == origin
                    && Zone.GetEntityCell(Still.ParentEntity) == stationCell && Zone.GetEntityCell(Fire) == fireCell
                    && Vessel(Actor, Source) && Vessel(Actor, Receiver) && Source != Receiver
                    && Source.GetPart<LiquidVesselPart>() == Input && Receiver.GetPart<LiquidVesselPart>() == Output
                    && Input.Capacity == capacity && Input.Volume == left && Input.LiquidId == (left == 0 ? "" : "brine")
                    && Output.Capacity == receiverCapacity && Output.Volume == (paid ? WaterYield : 0) && Output.LiquidId == (paid ? "water" : "")
                    && Fire.GetPart<FuelPart>() == FuelPart && Fire.GetPart<ThermalPart>() == heat
                    && WorldResourceActions.Ground(Fire, Zone) && FuelPart.MaxFuel == maximum
                    && FuelPart.FuelMass == fuel - (paid ? Fuel : 0) && heat.Temperature == temperature
                    && WorldResourceActions.Finite(fuel) && WorldResourceActions.Finite(maximum) && maximum >= fuel
                    && (double)fuel - (float)(fuel - Fuel) == Fuel
                    && Still.DistillationFireReady(Zone, Fire, paid ? 0 : Fuel)
                    && (!KeepsCrust || AlchemyStillPart.Factory == crustFactory && QualifiedCrust(crustFactory) == crustTemplate
                        && ClearCrustGround(Actor, Zone, origin, crust)
                        && (!paid || CanonicalCrust(crust) && Zone.GetEntityCell(crust) == origin
                            && WorldResourceActions.Ground(crust, Zone) && origin.Objects.Count(item => item == crust) == 1
                            && UniqueCrust(crust, true)));
            }

            bool UniqueCrust(Entity crust, bool placed) =>
                Zone.GetReadOnlyEntities().Count(item => item.ID == crust.ID) == (placed ? 1 : 0)
                && !pack.Objects.Concat(pack.EquippedItems.Values).Any(item => item?.ID == crust.ID);

            internal Entity CreateCrust(InventoryTransaction tx, string command)
            {
                // Inputs and the action are already claimed before factory callbacks.
                var crust = crustFactory.CreateEntity(crustTemplate);
                return CanonicalCrust(crust) && crust.SpatialZone == null && UniqueCrust(crust, false)
                    && tx.TryClaim(crust, Actor, command) && Current(false) ? crust : null;
            }

            internal bool PlaceCrust(Entity crust) => Zone.AddEntity(crust, origin.X, origin.Y);

            internal string Command
            {
                get
                {
                    string text = string.Join("/", actorOwner.Stamp, stillOwner.Stamp, sourceOwner.Stamp, receiverOwner.Stamp, fireOwner.Stamp,
                        Key(Zone), origin.X, origin.Y, stationCell.X, stationCell.Y, fireCell.X, fireCell.Y,
                        volume, capacity, receiverCapacity, fuel.ToString("R", CultureInfo.InvariantCulture),
                        maximum.ToString("R", CultureInfo.InvariantCulture), temperature.ToString("R", CultureInfo.InvariantCulture));
                    if (KeepsCrust) text += "/" + CrustAction + "/" + Key(crustFactory) + "/" + Key(crustTemplate);
                    using var hash = SHA256.Create();
                    string stamp = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "");
                    return (KeepsCrust ? CrustAction : WaterAction) + "|" + Uri.EscapeDataString(Source.ID) + "|"
                        + Uri.EscapeDataString(Receiver.ID) + "|" + Uri.EscapeDataString(Fire.ID) + "|" + stamp;
                }
            }
        }
        static bool Context(Entity actor, LiquidSettlingPart still, Zone zone) => actor != null && still?.ParentEntity != null && zone != null
            && WorldResourceActions.ActorCurrent(actor, zone) && actor.GetStatValue("Hitpoints") > 0 && !CombatSystem.IsDeathHandled(actor)
            && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked() != true && actor.GetPart<InventoryPart>()?.ParentEntity == actor
            && still.ParentEntity.GetPart<LiquidSettlingPart>() == still && still.DistillationStanding
            && WorldResourceActions.Ground(still.ParentEntity, zone) && WorldResourceActions.Nearby(actor, still.ParentEntity, zone);
        static bool Vessel(Entity actor, Entity item) => WorldResourceActions.Carried(actor, item, true)
            && item.ID != actor.ID
            && item.Parts.Count(p => p is LiquidVesselPart) == 1 && item.GetPart<LiquidVesselPart>()?.ParentEntity == item
            && item.GetPart<LiquidVesselPart>().Capacity > 0 && !item.HasTag("Creature") && !item.HasTag("Terrain");
        static bool BatchFeed(LiquidVesselPart part) => part.LiquidId == "brine" && part.Volume >= Feed && part.Volume <= part.Capacity;
        static bool EmptyReceiver(LiquidVesselPart part, int yield) =>
            part.LiquidId == "" && part.Volume == 0 && part.Capacity >= yield;

        static Plan Make(Entity actor, LiquidSettlingPart still, Zone zone, Entity source, Entity receiver, Entity fire, bool keepCrust)
        {
            if (!Context(actor, still, zone) || source == receiver || !Vessel(actor, source) || !Vessel(actor, receiver)
                || source.ID == receiver.ID || !BatchFeed(source.GetPart<LiquidVesselPart>())
                || !EmptyReceiver(receiver.GetPart<LiquidVesselPart>(), keepCrust ? CrustWater : Water)
                || !still.DistillationFireReady(zone, fire, Fuel)) return null;
            var factory = keepCrust ? AlchemyStillPart.Factory : null;
            var template = keepCrust ? QualifiedCrust(factory) : null;
            if (keepCrust && template == null) return null;
            var plan = new Plan(actor, still, zone, source, receiver, fire, factory, template);
            return plan.Current(false) ? plan : null;
        }
        internal static void AddRows(Entity actor, LiquidSettlingPart still, Zone zone, InventoryActionList actions)
        {
            if (actions == null || !Context(actor, still, zone)) return;
            var items = actor.GetPart<InventoryPart>().Objects.Where(e => Vessel(actor, e)).OrderBy(e => e.ID, StringComparer.Ordinal).ToArray();
            var sources = items.Where(e => BatchFeed(e.GetPart<LiquidVesselPart>())).ToArray();
            var receivers = items.Where(e => EmptyReceiver(e.GetPart<LiquidVesselPart>(), CrustWater)).ToArray();
            var fire = sources.Length > 0 && receivers.Length > 0 ? still.DistillationFire(zone) : null;
            bool anyWater = false, anyCrust = false;
            foreach (var source in sources)
                foreach (var receiver in receivers)
                {
                    var water = Make(actor, still, zone, source, receiver, fire, false);
                    var crust = Make(actor, still, zone, source, receiver, fire, true);
                    var plan = water ?? crust;
                    if (plan == null) continue;
                    int sourceNumber = Array.IndexOf(sources, source) + 1;
                    int receiverNumber = Array.IndexOf(receivers, receiver) + 1;
                    string from = "brine flask " + sourceNumber + " (" + plan.Input.Volume + "/" + plan.Input.Capacity + ")";
                    string to = "empty flask " + receiverNumber + " (0/" + plan.Output.Capacity + ")";
                    if (water != null)
                    {
                        anyWater = true;
                        actions.AddActionWithHint(WaterAction, "distil " + from + " into " + to + " (2 water; 5 fuel)", water.Command,
                            "Uses 4 brine from " + from + " and receives 2 water in " + to + ". Two units of concentrate are discarded; no salt is recovered.",
                            '\0', 7, false, ActionTime.OneAction);
                    }
                    if (crust != null)
                    {
                        anyCrust = true;
                        actions.AddActionWithHint(CrustAction, "keep crust, get 1 water (brine " + sourceNumber + " to flask " + receiverNumber + "; 5 fuel)", crust.Command,
                            "Uses 4 brine from " + from + " and 5 fuel in one action. Receives 1 water in " + to
                            + " and leaves one pound of brine crust at your feet. Keeping the crust costs one water.",
                            '\0', 7, false, ActionTime.OneAction);
                    }
                }
            if (!anyWater) AddBlocked(WaterAction, "distill brine into water", Water);
            if (!anyCrust && sources.Length > 0) AddBlocked(CrustAction, "keep brine crust", CrustWater, keepCrust: true);

            void AddBlocked(string action, string label, int waterYield, bool keepCrust = false)
            {
                string reason, code = "needs-distillation-inputs";
                if (sources.Length == 0)
                    reason = $"Carry at least {Feed} brine in one flask.";
                else if (!receivers.Any(e => EmptyReceiver(e.GetPart<LiquidVesselPart>(), waterYield)))
                    reason = $"Carry a separate empty single flask that holds {waterYield} water.";
                else if (fire == null)
                    reason = $"Needs a hot fire within {LiquidSettlingPart.FireReach} cells of the still that can spare {Fuel} fuel.";
                else if (keepCrust && QualifiedCrust(AlchemyStillPart.Factory) == null)
                    reason = "This still cannot retain crust in its current state.";
                else if (keepCrust && !ClearCrustGround(actor, zone, zone.GetEntityCell(actor)))
                {
                    code = "needs-clear-ground";
                    reason = "Stand on clear, dry ground beside the still to keep one pound of crust in place of one water.";
                }
                else
                    reason = "The selected flasks or fire cannot supply this batch.";
                actions.AddDisabled(action, label, code, reason, '\0', 7).Hint = reason;
            }
        }
        internal static bool TryAct(Entity actor, LiquidSettlingPart still, Zone zone, string command, InventoryTransaction tx)
        {
            if (!IsCommand(command) || tx == null || !Context(actor, still, zone)) return Reject(actor, still?.ParentEntity);
            var fields = command.Split('|'); if (fields.Length != 5) return Reject(actor, still.ParentEntity);
            var source = WorldResourceActions.ExactCarried(actor, fields[1]); var receiver = WorldResourceActions.ExactCarried(actor, fields[2]);
            var fire = WorldResourceActions.ExactGround(zone, fields[3]);
            var plan = Make(actor, still, zone, source, receiver, fire, IsCrustCommand(command));
            if (plan == null || plan.Command != command || !tx.TryClaimAction(actor)) return Reject(actor, still.ParentEntity);
            foreach (var owner in new[] { actor, still.ParentEntity, source, receiver, fire })
                if (!tx.TryClaim(owner, actor, command)) return Reject(actor, still.ParentEntity);
            var crust = plan.KeepsCrust ? plan.CreateCrust(tx, command) : null;
            if (plan.KeepsCrust && crust == null) return Reject(actor, still.ParentEntity);
            var input = plan.Input; var output = plan.Output; var fuel = plan.FuelPart;
            bool crustEscaped = false;
            tx.Do(() => { input.Volume -= Feed; if (input.Volume == 0) input.LiquidId = ""; output.Volume = plan.WaterYield; output.LiquidId = "water"; fuel.FuelMass -= Fuel; }, () =>
            {
                UndoLiquid(receiver, output, "water", plan.WaterYield, actor);
                if (crustEscaped) return;
                UndoLiquid(source, input, "brine", -Feed, actor);
                if (fire.GetPart<FuelPart>() == fuel && fuel.ParentEntity == fire && WorldResourceActions.Finite(fuel.FuelMass)
                    && WorldResourceActions.Finite(fuel.MaxFuel) && fuel.MaxFuel >= 0)
                    fuel.FuelMass = (float)Math.Max(0, Math.Min(fuel.MaxFuel, (double)fuel.FuelMass + Fuel));
            });
            if (crust != null)
            {
                // Register retraction before placing. Foreign custody must never
                // produce both a retained crust and a restored feed batch.
                tx.Do(null, () =>
                {
                    if (WorldResourceActions.Ground(crust, zone) && zone.RemoveEntity(crust)) return;
                    var body = crust.GetPart<PhysicsPart>();
                    crustEscaped = crust.SpatialZone != null || body?.InInventory != null || body?.Equipped != null;
                    if (crustEscaped)
                    {
                        Diag.Record("furniture", "BrineCrustUndoCustodyChanged", actor, crust);
                        MessageLog.Add("The crust could not be recovered safely. The batch was spent.");
                    }
                });
                if (!plan.PlaceCrust(crust) || !plan.Current(true, crust)) return Reject(actor, still.ParentEntity);
            }
            tx.BeforeCommit(() => plan.Current(true, crust));
            tx.AfterCommit(() => MessageLog.Add(plan.KeepsCrust
                ? "You distil 4 brine into 1 clean water and leave a pound of brine crust at your feet. The fire spends 5 fuel."
                : "You distil 4 brine into 2 clean water. The fire spends 5 fuel; concentrate is discarded."));
            tx.AfterCommit(() => Diag.Record("furniture", "BrineDistilled", actor, still.ParentEntity,
                new { feed = Feed, water = plan.WaterYield, fuel = Fuel, retainedCrust = crust?.ID }));
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
