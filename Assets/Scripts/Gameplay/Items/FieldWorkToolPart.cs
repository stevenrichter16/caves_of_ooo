using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CavesOfOoo.Core.Inventory;

namespace CavesOfOoo.Core
{
    /// <summary>Reusable material-powered tools. Only their paid delivery differs;
    /// heat and electricity use the world's existing reaction rules.</summary>
    public sealed class FieldWorkToolPart : Part, IDescribeUse, IDescribeUseRows, IDescribeState, IInventoryActionReceiptOwner
    {
        public override string Name=>"FieldWorkTool";
        public string Kind="";
        public int Charge;
        public const int HeatFuel=5;
        const string Heat="DirectHearthHeat|",Load="LoadDischargeRod|",Shock="DischargeRod|",Water="CondenseSteam|";
        sealed class Menu { internal readonly Dictionary<string,Quote> Quotes=new Dictionary<string,Quote>(); }
        static readonly ConditionalWeakTable<Entity,Menu> Menus=new ConditionalWeakTable<Entity,Menu>();
        sealed class Quote
        {
            internal Entity Actor;internal Zone Zone;internal Entity[] Claims;
            internal string Command,Key,Label,Hint;internal Cell Target;
            internal Func<bool> Before,After;internal Func<InventoryTransaction,bool> Pay;
            internal Action Publish;
        }
        internal static bool IsCommand(string command)=>command!=null && (command.StartsWith(Heat,StringComparison.Ordinal)
            || command.StartsWith(Load,StringComparison.Ordinal) || command.StartsWith(Shock,StringComparison.Ordinal) || command.StartsWith(Water,StringComparison.Ordinal));
        public bool OwnsInventoryReceipt(string command)=>ParentEntity?.GetPart<FieldWorkToolPart>()==this && IsCommand(command);
        bool Usable(Entity actor,Zone zone)=>WorldResourceActions.ActorCurrent(actor,zone)
            && actor.GetPart<StatusEffectsPart>()?.IsActionBlocked()!=true && WorldResourceActions.Carried(actor,ParentEntity)
            && ParentEntity.GetPart<FieldWorkToolPart>()==this && ParentEntity.Parts.Count(p=>p is FieldWorkToolPart)==1
            && !ParentEntity.HasPart<StackerPart>() && !ParentEntity.HasEffect<BrokenEffect>() && !ParentEntity.HasEffect<FrozenEffect>()
            && !RepairablePart.BlocksFunction(ParentEntity) && (Kind=="heat" || Kind=="shock" || Kind=="water")
            && (Kind=="shock"?Charge==0 || Charge==1:Charge==0);
        static bool Visible(Cell cell)=>cell!=null && cell.IsVisible && cell.Explored;
        static Func<bool> Identity(Entity entity)
        {
            string id=entity.ID,bp=entity.BlueprintName;var parts=entity.Parts.ToArray();
            return ()=>entity.ID==id && entity.BlueprintName==bp && entity.Parts.SequenceEqual(parts) && parts.All(p=>p.ParentEntity==entity);
        }
        bool Line(Zone zone,Cell origin,Cell target)
        {
            if(!Visible(target) || target==origin)return false;
            int dx=target.X-origin.X,dy=target.Y-origin.Y,d=Math.Max(Math.Abs(dx),Math.Abs(dy));
            if(d<1 || d>2 || dx!=0 && dy!=0 && Math.Abs(dx)!=Math.Abs(dy))return false;
            if(d==1)return true;
            var mid=zone.GetCell(origin.X+Math.Sign(dx),origin.Y+Math.Sign(dy));
            return Visible(mid) && !mid.BlocksMovement();
        }
        IEnumerable<Quote> Choices(Entity actor,Zone zone,InventoryActionList rows)
        {
            if(!Usable(actor,zone))yield break;
            var owner=ParentEntity;var origin=zone.GetEntityCell(actor);var pack=actor.GetPart<InventoryPart>();
            var ownerIdentity=Identity(owner);var actorPhysics=actor.GetPart<PhysicsPart>();string actorId=actor.ID,kind=Kind;
            int initialCharge=Charge;var physical=owner.GetPart<PhysicsPart>();int weight=physical.Weight;
            bool Context(int charge)=>Usable(actor,zone) && ownerIdentity() && Kind==kind && Charge==charge
                && actor.ID==actorId && actor.GetPart<InventoryPart>()==pack && actor.GetPart<PhysicsPart>()==actorPhysics
                && zone.GetEntityCell(actor)==origin && owner.GetPart<PhysicsPart>()==physical && physical.Weight==weight;
            Quote Make(string prefix,string label,string hint,Cell target,Entity[] claims,Func<bool> before,Func<bool> after,Func<InventoryTransaction,bool> pay,Action publish,int finalCharge)
            {
                string token=Guid.NewGuid().ToString("N");
                return new Quote{Actor=actor,Zone=zone,Target=target,Claims=claims,Label=label,Hint=hint,
                    Key=prefix+target.X+"|"+target.Y,Command=prefix+target.X+"|"+target.Y+"|"+token,
                    Before=()=>Context(initialCharge) && before(),After=()=>Context(finalCharge) && after(),Pay=pay,Publish=publish};
            }
            if(kind=="heat")
            {
                foreach(var source in zone.GetReadOnlyEntities().OrderBy(e=>e.ID,StringComparer.Ordinal))
                {
                    var fire=source.GetPart<CampfirePart>();
                    if(!Hearth(actor,source,zone) || !CampfireWorkActions.Fuel(fire,out var fuel) || fuel.FuelMass<HeatFuel)continue;
                    var sourceIdentity=Identity(source);var sourceCell=zone.GetEntityCell(source);var heat=source.GetPart<ThermalPart>();
                    var marker=source.GetPart<InstallationMarkerPart>();var install=source.GetPart<InstallablePart>();
                    string installation=marker.InstallationId;int revision=marker.Revision;float amount=fuel.FuelMass,max=fuel.MaxFuel,temp=heat.Temperature;
                    float paidFuel=(float)((double)amount-HeatFuel);
                    if((double)amount-(double)paidFuel!=HeatFuel)continue;
                    bool SourceCurrent(float expected)=>sourceIdentity() && Hearth(actor,source,zone,expected>0)
                        && zone.GetEntityCell(source)==sourceCell && source.GetPart<FuelPart>()==fuel && fuel.FuelMass==expected && fuel.MaxFuel==max
                        && source.GetPart<ThermalPart>()==heat && heat.Temperature==temp && source.GetPart<InstallationMarkerPart>()==marker
                        && marker.InstallationId==installation && marker.Revision==revision && source.GetPart<InstallablePart>()==install;
                    for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)
                    {
                        var target=zone.GetCell(origin.X+dx,origin.Y+dy);if(target==origin || !Visible(target))continue;
                        bool icy = zone.TileState.HasCoating(target.X, target.Y, "ice");
                        bool TargetCurrent() => Visible(target) && zone.GetCell(target.X, target.Y) == target
                            && zone.TileState.HasCoating(target.X, target.Y, "ice") == icy;
                        string aim = icy ? "heat ice at " : "direct hearth heat to ";
                        string hint = icy
                            ? "Uses 5 fuel. Heat can thaw the ice into temporary meltwater on the ground. Cold or other coatings can absorb the heat."
                            : "Uses 5 fuel from your adjacent burning hearth. Shared heat can thaw ice or ignite coatings and affect nearby things.";
                        yield return Make(Heat,aim+target.X+","+target.Y+" (5 fuel, 1 action)",hint,target,new[]{source},
                            ()=>SourceCurrent(amount) && TargetCurrent(),()=>SourceCurrent(paidFuel) && TargetCurrent(),tx=>
                            {
                                fuel.FuelMass-=HeatFuel;
                                tx.Do(null,()=>{if(source.GetPart<FuelPart>()==fuel && fuel.ParentEntity==source && fuel.MaxFuel==max
                                    && WorldResourceActions.Finite(fuel.FuelMass) && fuel.FuelMass>=0 && fuel.FuelMass<=max-HeatFuel)fuel.FuelMass+=HeatFuel;});return true;
                            },()=>{ZoneTileStateSystem.ApplyFireToTile(zone,target.X,target.Y,actor,"HearthNozzle");ZoneTileStateSystem.ResolveAfterAbility(zone,actor);
                                source.GetPart<HearthGlowPart>()?.SyncLight();MessageLog.Add("You direct heat from the hearth. Five fuel spent.");},initialCharge);
                    }
                }
            }
            if(kind=="shock" && initialCharge==0)
            {
                foreach(var root in pack.Objects.ToArray())
                {
                    if(root.BlueprintName!="SparkRoot" || !WorldResourceActions.Carried(actor,root,false) || root.GetPart<StatusEffectsPart>()?.EffectCount>0)continue;
                    var rootIdentity=Identity(root);var reagent=root.GetPart<ReagentPart>();if(reagent?.ParentEntity!=root)continue;
                    var rootPhysics=root.GetPart<PhysicsPart>();var rootStack=root.GetPart<StackerPart>();
                    string properties=reagent.PropertiesRaw,flavor=reagent.FlavorText;int count=rootStack?.StackCount??1,maxRoot=rootStack?.MaxStack??1;
                    bool Payload()=>rootIdentity() && root.GetPart<ReagentPart>()==reagent && reagent.PropertiesRaw==properties && reagent.FlavorText==flavor
                        && root.GetPart<PhysicsPart>()==rootPhysics && rootPhysics.Takeable && root.GetPart<StackerPart>()==rootStack
                        && (rootStack?.MaxStack??1)==maxRoot && !(root.GetPart<StatusEffectsPart>()?.EffectCount>0);
                    bool Paid()=>Payload() && rootPhysics.Equipped==null && (count==1?root.SpatialZone==null && rootPhysics.InInventory==null
                        && (rootStack?.StackCount??1)==1 && !pack.Objects.Any(e=>e==root || e?.ID==root.ID)
                        && !pack.EquippedItems.Values.Any(e=>e==root || e?.ID==root.ID) && pack.FindEquippedBodyPart(root)==null
                        :WorldResourceActions.Carried(actor,root,false) && rootStack.StackCount==count-1);
                    yield return Make(Load,"load discharge rod (1 spark-root, 1 action)","Stores one discharge. The rod cannot hold a second charge.",origin,new[]{root},
                        ()=>Payload() && WorldResourceActions.Carried(actor,root,false) && (root.GetPart<StackerPart>()?.StackCount??1)==count,Paid,tx=>
                        {
                            if(!MaterialProcessService.ReserveInput(root,tx) || !MaterialProcessPayment.Spend(actor,root,actor,zone,tx,Payload))return false;
                            Charge=1;tx.Do(null,()=>{if(owner.GetPart<FieldWorkToolPart>()==this && Kind==kind && Charge==1)Charge=0;});return true;
                        },()=>MessageLog.Add("You seat one spark-root in the rod. One discharge ready."),1);
                }
            }
            if(kind=="shock" && initialCharge==1 && TileReactionSystem.IsInitialized)
            {
                for(int dx=-2;dx<=2;dx++)for(int dy=-2;dy<=2;dy++)
                {
                    var target=zone.GetCell(origin.X+dx,origin.Y+dy);
                    bool Target()=>Line(zone,origin,target) && TilePropagationSystem.IsConductive(zone,target.X,target.Y)
                        && (zone.TileState.Get(target.X,target.Y)?.Charge??0)<ZoneTileState.MaxEnergy;
                    if(!Target())continue;
                    yield return Make(Shock,"discharge rod at "+target.X+","+target.Y+" (1 charge, 1 action)",
                        "One stored charge. Conductive water and metal can carry it to you or bystanders; stand clear of the connected path.",target,Array.Empty<Entity>(),Target,Target,tx=>
                        {Charge=0;tx.Do(null,()=>{if(owner.GetPart<FieldWorkToolPart>()==this && Kind==kind && Charge==0)Charge=1;});return true;},()=>
                        {ZoneTileStateSystem.AddCharge(zone,target.X,target.Y,1,actor,"DischargeRod");ZoneTileStateSystem.ResolveAfterAbility(zone,actor);MessageLog.Add("The rod discharges. Its root is spent.");},0);
                }
            }
            if(kind=="water" && LiquidRegistry.IsInitialized && LiquidRegistry.Get("water")!=null)
            {
                foreach(var vent in zone.GetReadOnlyEntities().Where(e=>Steam(actor,e,zone)).OrderBy(e=>e.ID,StringComparer.Ordinal))
                {
                    var element=ProofingSources.ElementOf(vent);var holder=ProofingSources.StockHolder(zone,vent,element);if(holder==null)continue;
                    var stock=holder.GetPart<SourceStockPart>();int now=TurnManager.Active?.TickCount??0;
                    int capacity=stock?.Capacity??SourceStockPart.CapacityFor(zone,holder),available=stock?.AvailableAt(now)??capacity;
                    if(capacity<SourceStockPart.MinCapacity || capacity>SourceStockPart.MaxCapacity || available<0 || available>capacity)continue;
                    var ventCell=zone.GetEntityCell(vent);
                    if (available == 0)
                    {
                        if (stock?.ParentEntity == holder && stock.Available == 0 && stock.LastTick >= 0 && stock.LastTick <= now
                            && holder.Parts.Count(part => part is SourceStockPart) == 1)
                        {
                            rows.AddDisabled("FieldWorkTool", $"condense water at {ventCell.X},{ventCell.Y}", "vent-spent",
                                "The vent is out of catches. Scrapers and trays share this supply. It recovers one catch per game day.", '\0', 19)
                                .AtCell(ventCell.X, ventCell.Y);
                        }
                        continue;
                    }
                    var ventIdentity=Identity(vent);var holderIdentity=Identity(holder);var holderCell=zone.GetEntityCell(holder);
                    int raw=stock?.Available??0,last=stock?.LastTick??0;SourceStockPart paidStock=null;
                    Func<bool> ventAfter=null,holderAfter=null;int paidLast=0;
                    bool SourceBefore()=>ventIdentity() && holderIdentity() && Steam(actor,vent,zone) && zone.GetEntityCell(vent)==ventCell
                        && zone.GetEntityCell(holder)==holderCell && ProofingSources.StockHolder(zone,vent,element)==holder
                        && holder.GetPart<SourceStockPart>()==stock && (stock==null || stock.Capacity==capacity && stock.Available==raw && stock.LastTick==last)
                        && (TurnManager.Active?.TickCount??0)==now;
                    foreach(var vessel in pack.Objects.ToArray())
                    {
                        if (!CanReceiveWater(actor, vessel, out int initial, out int max) || initial >= max) continue;
                        var skin = vessel.GetPart<WaterskinPart>();
                        var liquid = vessel.GetPart<LiquidVesselPart>();
                        var vesselIdentity = Identity(vessel);
                        bool Receiver(int volume) => vesselIdentity()
                            && CanReceiveWater(actor, vessel, out int units, out int capacityNow)
                            && units == volume && capacityNow == max;
                        bool SourceAfter()=>paidStock!=null && ventAfter() && holderAfter() && paidStock.ParentEntity==holder && holder.GetPart<SourceStockPart>()==paidStock
                            && paidStock.Capacity==capacity && paidStock.Available==available-1 && zone.GetEntityCell(holder)==holderCell
                            && paidStock.LastTick==paidLast && Steam(actor,vent,zone) && zone.GetEntityCell(vent)==ventCell
                            && ProofingSources.StockHolder(zone,vent,element)==holder && (TurnManager.Active?.TickCount??0)==now;
                        string fill = $"{initial}/{max} to {initial + 1}/{max}";
                        string name = InventoryPart.GetUnitDisplayName(vessel);
                        string hint = "One action spends one shared vent catch. The same supply serves scrapers and trays and restores one per game day.";
                        if (liquid != null) hint += " Split a flask stack first.";
                        yield return Make(Water,"condense 1 water into "+name+" ("+fill+")",hint,ventCell,new[]{vent,holder,vessel},
                            ()=>SourceBefore() && Receiver(initial),()=>SourceAfter() && Receiver(initial+1),tx=>
                            {
                                paidStock=stock??SourceStockPart.For(zone,holder,now);paidStock.Reconcile(now);paidLast=paidStock.LastTick;paidStock.Available--;
                                ventAfter=Identity(vent);holderAfter=Identity(holder);
                                WaterTransferActions.SetUnits(vessel, initial + 1);
                                tx.Do(null,()=>
                                {
                                    if (skin != null && skin.ParentEntity == vessel && vessel.GetPart<WaterskinPart>() == skin)
                                        skin.Charges = WaterQuantityUndo.Restore(skin.Charges, 1, skin.Capacity);
                                    else if (liquid != null && liquid.ParentEntity == vessel && vessel.GetPart<LiquidVesselPart>() == liquid && liquid.LiquidId == "water")
                                    {
                                        liquid.Volume = WaterQuantityUndo.Restore(liquid.Volume, 1, liquid.Capacity);
                                        liquid.LiquidId = liquid.Volume == 0 ? "" : "water";
                                    }
                                    if(holder.GetPart<SourceStockPart>()==paidStock && paidStock.ParentEntity==holder && paidStock.Capacity==capacity && paidStock.Available>=0 && paidStock.Available<capacity)
                                    {paidStock.Available++;if(stock==null && paidStock.Available==capacity && paidStock.LastTick==now)holder.RemovePart(paidStock);}
                                });return true;
                            },()=>
                            {
                                SettlingTrayPart.InvalidateVisibleTrays(zone);
                                MessageLog.Add("You catch one water in the "+name+". The vent's catch supply is reduced.");
                            },initialCharge);
                    }
                }
            }
        }
        static bool CanReceiveWater(Entity actor, Entity vessel, out int units, out int capacity)
            => WaterTransferActions.Vessel(actor, vessel, out units, out capacity)
                && !vessel.HasEffect<FrozenEffect>() && !vessel.HasEffect<BrokenEffect>()
                && vessel.Parts.Count(part => part is WaterskinPart || part is LiquidVesselPart) == 1
                && vessel.Parts.Count(part => part is PhysicsPart) == 1 && vessel.Parts.Count(part => part is StackerPart) <= 1;

        static bool Hearth(Entity actor,Entity source,Zone zone,bool burning=true)=>actor?.HasTag("Player")==true && source!=null && source.GetPart<CampfirePart>() is CampfirePart fire
            && HeldFixtureActions.IsPlayerHeld(source) && CampfireWorkActions.Source(actor,fire,zone) && CampfireWorkActions.Fuel(fire,out _)
            && (!burning || CampfireWorkActions.Hot(fire)) && !RepairablePart.BlocksFunction(source) && !source.HasEffect<BrokenEffect>()
            && Visible(zone.GetEntityCell(source));
        static bool Steam(Entity actor,Entity source,Zone zone)=>source?.BlueprintName=="SteamVent" && WorldResourceActions.Nearby(actor,source,zone)
            && Visible(zone.GetEntityCell(source)) && LiquidSourcePhase.CanDrawWater(zone,source) && source.GetPart<TileStateSourcePart>() is TileStateSourcePart tile
            && tile.ParentEntity==source && tile.Heat>0 && tile.Coating=="water"
            && source.GetPart<MaterialPart>()?.MaterialID=="Steam" && ProofingSources.ElementOf(source)!=null;
        void Rows(Entity actor,Zone zone,InventoryActionList rows,bool publish)
        {
            if(rows==null)return;Menu menu=null;Quote[] previous=Array.Empty<Quote>();
            if(publish){menu=Menus.GetValue(ParentEntity,_=>new Menu());previous=menu.Quotes.Values.ToArray();menu.Quotes.Clear();}
            foreach(var quote in Choices(actor,zone,rows))
            {
                if(!quote.Before())continue;var offered=quote;
                if(publish)
                {
                    // Hint/reader/world queries may gather again while the native
                    // popup still displays its first token. Retain that exact quote
                    // only while it remains current and the same choice is offered.
                    offered=previous.FirstOrDefault(p=>p.Actor==actor && p.Zone==zone && p.Key==quote.Key && p.Target==quote.Target
                        && p.Claims.SequenceEqual(quote.Claims) && p.Label==quote.Label && p.Hint==quote.Hint && p.Before())??quote;
                    menu.Quotes[offered.Command]=offered;
                }
                rows.AddActionWithHint("FieldWorkTool",offered.Label,offered.Command,offered.Hint,'\0',19,false,ActionTime.OneAction).AtCell(offered.Target.X,offered.Target.Y);
            }
        }
        public void AddUseRows(Entity actor,Zone zone,InventoryActionList rows)=>Rows(actor,zone,rows,false);
        public override bool HandleEvent(GameEvent e)
        {
            if(ParentEntity?.GetPart<FieldWorkToolPart>()!=this)return true;
            var actor=e.GetParameter<Entity>("Actor");var zone=PreparationActions.ZoneFor(e,actor);
            if(e.ID=="GetInventoryActions"){Rows(actor,zone,e.GetParameter<InventoryActionList>("Actions"),true);return true;}
            string command=e.GetStringParameter("Command");
            if(e.ID!="InventoryAction" || !IsCommand(command) || !Menus.TryGetValue(ParentEntity,out var menu) || !menu.Quotes.TryGetValue(command,out var q))return true;
            menu.Quotes.Remove(command);var tx=e.GetParameter<InventoryTransaction>("InventoryTransaction");
            if(tx==null || q.Actor!=actor || q.Zone!=zone || !q.Before() || !tx.TryClaimAction(actor)
                || !tx.TryClaim(actor,actor,command) || !tx.TryClaim(ParentEntity,actor,command))return true;
            foreach(var claim in q.Claims.Distinct())if(claim==actor || claim==ParentEntity || !tx.TryClaim(claim,actor,command))return true;
            if(!q.Before() || !q.Pay(tx) || !q.After())return true;
            tx.BeforeCommit(q.After);tx.AfterCommit(()=>
            {
                if(q.After())q.Publish();
                else CavesOfOoo.Diagnostics.Diag.Record("event","FieldWorkPaidContextChanged",actor:actor,target:ParentEntity,payload:new{command});
            });e.Handled=true;return false;
        }
        public ReaderScope Scope=>ReaderScope.Both;
        public int Order=>40;
        public string DescribeState(DescribeContext context)=>Kind=="shock"?(Charge==1?"One discharge ready.":Charge==0?"Unloaded.":"Charge state invalid; cannot use."):null;
        public void DescribeUse(DescribeContext context,UseGuide guide)
        {
            if(Kind=="heat")
            {guide.How.Add("Carry it beside your installed burning hearth. Choose a cell to direct heat from its inventory menu or that cell's C menu.");guide.Needs.Add("five hearth fuel per use");guide.Limits.Add("One action. Shared heat thaws ice and can ignite coatings; connected fires and nearby occupants remain at risk. Melted coatings are not drinking water.");}
            if(Kind=="shock")
            {guide.How.Add("Load one spark-root in your pack; discharge into conductive ground or metal one or two straight cells away.");guide.Needs.Add("one root per discharge; visible clear line");guide.Limits.Add("Loading and discharge each take one action. Holds one charge. Connected water and metal can carry the shock to you or bystanders.");}
            if(Kind=="water")
            {guide.How.Add("Carry it beside a natural steam vent. Choose condense from the tool or vent's C menu.");guide.Needs.Add("one carried waterskin, drawgourd shell or flask with room for clean water; split a flask stack first");guide.Limits.Add("One action catches one water and spends one shared source catch, also used by scrapers and trays. Supply restores one catch per game day. Ash beds and ordinary heat give no water; frozen vents refuse.");}
            guide.WhyNot="carry a sound tool and bring its stated supplies within reach";
        }
    }
}
