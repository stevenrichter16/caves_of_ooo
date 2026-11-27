using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    /// <summary>An explicitly selected, adjacent own box. No remote, nested or town supplies.</summary>
    internal sealed class MaterialProcessStock
    {
        internal readonly Entity Box;
        internal readonly ContainerPart Container;
        readonly Entity actor;readonly Zone zone;readonly Cell actorCell,boxCell;
        readonly Part[] parts;readonly InstallationMarkerPart marker;readonly InstallablePart install;
        readonly string id,installation;readonly int revision;
        MaterialProcessStock(Entity actor,Entity box,Zone zone)
        {
            this.actor=actor;Box=box;this.zone=zone;Container=box.GetPart<ContainerPart>();
            actorCell=zone.GetEntityCell(actor);boxCell=zone.GetEntityCell(box);parts=box.Parts.ToArray();id=box.ID;
            marker=box.GetPart<InstallationMarkerPart>();install=box.GetPart<InstallablePart>();installation=marker.InstallationId;revision=marker.Revision;
        }
        internal static MaterialProcessStock Resolve(Entity actor,Entity box,Zone zone)
        {
            if(!Accessible(actor,box,zone))return null;
            var stock=new MaterialProcessStock(actor,box,zone);return stock.Current()?stock:null;
        }
        static bool Accessible(Entity actor,Entity box,Zone zone)
        {
            if(actor?.HasTag("Player")!=true || box?.BlueprintName!="CampBox" || zone==null
                || !PreparationActions.CurrentActor(actor,zone) || !HeldFixtureActions.IsPlayerHeld(box)
                || box.GetPart<PackableFixturePart>()?.Kind!=CampKinds.Box || box.HasEffect<BrokenEffect>()
                || box.GetPart<DestructiblePart>()?.HP<=0 || box.GetPart<DestructiblePart>()?.Gone==true || box.GetPart<ContainerPart>() is not ContainerPart container
                || box.Parts.Count(p=>p is ContainerPart)!=1 || box.Parts.Count(p=>p is InstallationMarkerPart)!=1
                || box.Parts.Count(p=>p is InstallablePart)!=1 || box.Parts.Count(p=>p is PackableFixturePart)!=1
                || box.Parts.Count(p=>p is PhysicsPart)!=1
                || container.IsLocked || !container.AllowsCleanAccess || box.SpatialZone!=zone
                || box.GetPart<PhysicsPart>() is not PhysicsPart physics || physics.InInventory!=null || physics.Equipped!=null)return false;
            var a=zone.GetEntityCell(actor);var b=zone.GetEntityCell(box);
            return a!=null && b!=null && b.IsVisible && b.Explored && Math.Abs(a.X-b.X)<=1 && Math.Abs(a.Y-b.Y)<=1;
        }
        internal bool Current()
            => Accessible(actor,Box,zone) && Box.ID==id && !string.IsNullOrEmpty(id)
                && Box.Parts.SequenceEqual(parts) && parts.All(p=>p.ParentEntity==Box)
                && Box.GetPart<ContainerPart>()==Container && Box.GetPart<InstallablePart>()==install
                && Box.GetPart<InstallationMarkerPart>()==marker && marker.InstallationId==installation && marker.Revision==revision
                && zone.GetEntityCell(actor)==actorCell && zone.GetEntityCell(Box)==boxCell
                && Container.Contents.All(e=>e!=null && !string.IsNullOrEmpty(e.ID))
                && Container.Contents.Distinct().Count()==Container.Contents.Count
                && Container.Contents.Select(e=>e.ID).Distinct(StringComparer.Ordinal).Count()==Container.Contents.Count
                && actor.GetPart<InventoryPart>().Objects.Concat(actor.GetPart<InventoryPart>().EquippedItems.Values)
                    .Concat(zone.GetReadOnlyEntities()).Where(e=>e!=null && e.ID==id).Distinct().Count()==1
                && actor.ID!=id;
        internal bool Contains(Entity item)
        {
            if(!Current() || item==null || item==actor || item==Box || !item.HasTag("Item") || item.HasTag("Creature") || item.HasTag("Terrain")
                || item.HasPart<CropPart>() || item.HasPart<SpatialFootprintPart>() || item.SpatialZone!=null
                || item.GetPart<PhysicsPart>() is not PhysicsPart p || p.ParentEntity!=item || p.Solid || !p.Takeable
                || p.InInventory!=Box || p.Equipped!=null || Container.Contents.Count(e=>e==item)!=1)return false;
            var stack=item.GetPart<StackerPart>();
            if(stack!=null && (stack.StackCount<1 || stack.StackCount>stack.MaxStack))return false;
            return !ListedOutsideBox(item);
        }
        internal bool HasStoredOutput(Entity item)
            => Box.GetPart<ContainerPart>() == Container && Container.ParentEntity == Box
                && Container.Contents.Count(entry => entry == item) == 1
                && Container.Contents.Count(entry => entry?.ID == item.ID) == 1
                && item.SpatialZone == null && item.GetPart<PhysicsPart>() is PhysicsPart physics
                && physics.ParentEntity == item && physics.InInventory == Box && physics.Equipped == null
                && !ListedOutsideBox(item);
        internal bool ContainsOilFlask(Entity flask, bool allowEmpty)
        {
            if (!Contains(flask) || !LiquidVesselPart.HasOilContents(flask, allowEmpty) || flask.HasPart<ContainerPart>()
                || LocalPorterReservationPart.Blocks(flask)
                || flask.GetPart<StackerPart>() is { } stack && (stack.ParentEntity != flask || stack.StackCount != 1))
                return false;
            return !ListedInGroundedContainer(zone, flask, Box);
        }
        internal static bool ListedInGroundedContainer(Zone zone, Entity item, Entity permitted = null)
            => zone.GetReadOnlyEntitiesWithPart<ContainerPart>().Any(owner => owner != permitted
                && owner.GetPart<ContainerPart>()?.Contents.Any(entry => entry == item || entry?.ID == item.ID) == true);
        bool ListedOutsideBox(Entity item)
        {
            var pack = actor.GetPart<InventoryPart>();
            return pack != null && (HasEntry(pack.Objects, item) || HasEntry(pack.EquippedItems.Values, item))
                || HasEntry(zone.GetReadOnlyEntities(), item);
        }
        static bool HasEntry(IEnumerable<Entity> entries, Entity item)
            => entries.Any(e => e == item || e?.ID == item.ID);
        // Also checked during undo, when the box may have independently broken.
        internal bool ExcludesPaidItem(Entity item)
        {
            var current = Box.GetPart<ContainerPart>();
            return !HasEntry(Container.Contents, item)
                && (current == null || current == Container || !HasEntry(current.Contents, item))
                && !ListedOutsideBox(item);
        }
        internal bool CanRefundRemainingUnit(Entity item)
        {
            var physics = item.GetPart<PhysicsPart>();
            if (physics?.ParentEntity != item || physics.Equipped != null) return false;
            var current = Box.GetPart<ContainerPart>();
            if (physics.InInventory == Box && current == Container && item.SpatialZone == null)
                return Container.Contents.Count(entry => entry == item) == 1
                    && Container.Contents.Count(entry => entry?.ID == item.ID) == 1
                    && !ListedOutsideBox(item);

            // An independent structural spill keeps the original stack on the
            // ground. Refund its own unit there rather than re-opening the box.
            var pack = actor.GetPart<InventoryPart>();
            return WorldResourceActions.Ground(item, zone)
                && zone.GetReadOnlyEntities().Count(entry => entry.ID == item.ID) == 1
                && !HasEntry(Container.Contents, item)
                && (current == null || current == Container || !HasEntry(current.Contents, item))
                && (pack == null || !HasEntry(pack.Objects, item) && !HasEntry(pack.EquippedItems.Values, item));
        }
        internal bool Begin(Entity item,InventoryTransaction tx,string command)
            => Contains(item) && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked()!=true
                && tx.TryClaim(actor,actor,command) && tx.TryClaim(Box,actor,command) && tx.TryClaim(item,actor,command);
        internal bool Consume(Entity item,InventoryTransaction tx,bool allowClayStamp,Func<bool> refundAllowed=null)
        {
            if(!Contains(item))return false;
            var state=MaterialProcessTemplate.Capture(ForgePart.Factory,item.BlueprintName)?.Snapshot(item,true,allowClayStamp);
            return state!=null && MaterialProcessPayment.Spend(actor,item,Box,zone,tx,state.Matches,
                allowClayStamp?()=>state.MatchesAfterPayment(1):null,refundAllowed);
        }

    }

    internal static class CampBoxWork
    {
        const string Prefix="CampBoxWork|";
        sealed class Menu { internal readonly Dictionary<string,MaterialProcessService.Quote> Quotes=new Dictionary<string,MaterialProcessService.Quote>(); }
        static readonly ConditionalWeakTable<Entity,Menu> Menus=new ConditionalWeakTable<Entity,Menu>();
        internal static bool IsCommand(string command)=>command?.StartsWith(Prefix,StringComparison.Ordinal)==true;
        static string Label(CavesOfOoo.Data.EntityFactory factory,string blueprint)
            =>factory.Blueprints[blueprint].Parts.TryGetValue("Render",out var p) && p.TryGetValue("DisplayName",out var name)?name:blueprint;
        internal static bool Handle(Entity box,GameEvent e)
        {
            if(e.ID!="GetInventoryActions" && e.ID!="InventoryAction")return true;
            if(box?.BlueprintName!="CampBox")return true;
            var actor=e.GetParameter<Entity>("Actor");var zone=PreparationActions.ZoneFor(e,actor);
            if(e.ID=="GetInventoryActions")
            {
                var menu=Menus.GetValue(box,_=>new Menu());
                var stock=MaterialProcessStock.Resolve(actor,box,zone);var factory=ForgePart.Factory;
                if(stock==null || factory==null){menu.Quotes.Clear();return true;}
                var offered=new Dictionary<string,MaterialProcessService.Quote>();
                foreach (var input in stock.Container.Contents.ToArray())
                {
                    var plans = MaterialProcessRegistry.ForSource(input)
                        .Select(d => MaterialProcessPlan.TryCreateFromStock(actor, input, d.Id, zone, factory, stock));
                    if (input.GetPart<SiftableStockPart>() is SiftableStockPart sizeStock)
                        plans = plans.Append(MaterialProcessPlan.TryCreateScreening(actor, input, zone, factory, sizeStock, stock));
                    foreach (var plan in plans)
                    {
                        if (plan == null) continue;
                        AddChoice(plan, false);
                        if (plan.CanKeepGritInStock()) AddChoice(plan, true);
                    }

                    void AddChoice(MaterialProcessPlan plan, bool keepGrit)
                    {
                        var definition = plan.Definition;
                        var actions = e.GetParameter<InventoryActionList>("Actions");
                        bool screening = definition.Id == SiftableStockPart.ProcessId;
                        string label = keepGrit ? $"sieve {Label(factory, definition.SourceBlueprint)} (leave grit in box)"
                            : screening ? $"screen {Label(factory, definition.SourceBlueprint)} from box" : "";
                        int weight = plan.StoredScreeningPackWeight(keepGrit);
                        if (screening && !plan.RoomForStoredScreening(keepGrit))
                        {
                            actions?.AddDisabled("box-work", label, "box-screening-pack-full",
                                $"Make room for {weight} lb of screened material.", '\0', 19);
                            return;
                        }
                        // F1 regathers these rows. Reuse a token only while
                        // its source, definition and access still agree.
                        var retained = menu.Quotes.FirstOrDefault(p => p.Value.Plan.Actor == actor && p.Value.Plan.Zone == zone
                            && p.Value.Plan.Stock?.Box == box && p.Value.Plan.Source == input
                            && p.Value.KeepGritInStock == keepGrit
                            && p.Value.Plan.SameDefinition(plan) && p.Value.Current());
                        var quote = retained.Value ?? MaterialProcessService.TryQuote(plan, keepGritInStock: keepGrit);
                        if (quote == null) return;
                        string token = retained.Key ?? Guid.NewGuid().ToString("N");
                        offered[token] = quote;
                        string outputs = MaterialProcessActions.Outputs(factory, definition);
                        if (screening)
                        {
                            string hint = "One action. Makes " + outputs + ". Keep the sieve.";
                            if (keepGrit) hint += " The fine material goes to your pack and the grit stays here.";
                            actions?.AddActionWithHint("box-work", label + $" ({weight} lb to pack)",
                                Prefix + token, hint, '\0', 19, false, ActionTime.OneAction);
                            return;
                        }
                        string inputs = definition.InputCount + " " + Label(factory, definition.SourceBlueprint)
                            + string.Concat(definition.AdditionalInputBlueprints.Select((bp, i) => " + " + definition.AdditionalInputCounts[i] + " " + Label(factory, bp)));
                        actions?.AddAction("box-work", definition.ActionText + ": " + inputs + " from this box -> " + outputs
                            + (definition.LossText.Length > 0 ? "; " + definition.LossText : "") + " (1 action, output to pack)",
                            Prefix + token, '\0', 19, false, ActionTime.OneAction);
                    }
                }
                menu.Quotes.Clear();foreach(var pair in offered)menu.Quotes.Add(pair.Key,pair.Value);
                return true;
            }
            string command=e.GetStringParameter("Command");
            if(!IsCommand(command) || !Menus.TryGetValue(box,out var current) || !current.Quotes.TryGetValue(command.Substring(Prefix.Length),out var selected))return true;
            current.Quotes.Remove(command.Substring(Prefix.Length));
            if(selected.Plan.Actor!=actor || selected.Plan.Zone!=zone || selected.Plan.Stock?.Box!=box
                || !MaterialProcessService.TryExecute(selected,e.GetParameter<InventoryTransaction>("InventoryTransaction"),command))return true;
            e.Handled=true;return false;
        }
    }
}
