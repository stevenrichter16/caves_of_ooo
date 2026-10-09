using System;
using System.Globalization;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Local physical uses of ordinary, carried materials. Selection
    /// binds the source quantity, origin and exact recipient or ground layer.
    /// Irreversible heat/network reactions wait until the inventory commit.
    /// Existing liquid definitions remain the authority for coat consequences.</summary>
    public static class MaterialFieldActions
    {
        internal const float KindlingJoules = FireDose.Ignition;
        public const float CoolingJoules = -300f, WarmingJoules = 150f;
        public const int FilmTurns = 8, PitchAmount = 28, HoneyAmount = 22;
        const string Prefix = "MaterialField|";
        static readonly (int x,int y,string label)[] Directions = {
            (0,0,"here"),(0,-1,"north"),(1,-1,"northeast"),(1,0,"east"),(1,1,"southeast"),
            (0,1,"south"),(-1,1,"southwest"),(-1,0,"west"),(-1,-1,"northwest")
        };
        public static bool IsCommand(string command) => command?.StartsWith(Prefix,StringComparison.Ordinal)==true;

        public static void AddActions(Entity actor,Entity item,Zone zone,InventoryActionList actions)
        {
            if(actions==null || ValidateSource(actor,item,zone,out string verb,out string liquid)!=null)return;
            var origin=zone.GetEntityCell(actor);int count=Quantity(item);
            if(verb=="Kindle"||verb=="Cool"||verb=="Warm"||verb=="Coat")
                foreach(var target in zone.GetReadOnlyEntities())
                {
                    if(!CanTarget(actor,target,zone,verb,liquid,null))continue;
                    var at=zone.GetEntityCell(target);
                    Add(actions,item,zone,origin,verb,verb=="Kindle"?"":liquid,target,at.X,at.Y,count,
                        Label(verb)+" "+(target==actor?"yourself":target.GetDisplayName())
                        +(Provokes(actor,target,verb)?" (1 item; provokes)":" (1 item)"));
                }
            if(verb=="Cool"||verb=="Warm"||verb=="Coat")return;
            foreach(var direction in Directions)
            {
                int x=origin.X+direction.x,y=origin.Y+direction.y;
                if(verb=="Wick")
                {
                    var state=zone.TileState.Get(x,y);if(state==null)continue;
                    foreach(var layer in state.Coatings)
                        if(layer!=null&&CanGround(zone,origin,verb,layer.Id,x,y))
                            Add(actions,item,zone,origin,verb,layer.Id,null,x,y,count,"wick "+TileStateCatalog.DisplayName(layer.Id)+" "+direction.label+" (1 pith)");
                }
                else if(CanGround(zone,origin,verb,liquid,x,y))
                    Add(actions,item,zone,origin,verb,liquid,null,x,y,count,
                        (verb=="Film"?"spread "+liquid:Label(verb))+" "+direction.label+" (1 item)");
            }
        }
        static void Add(InventoryActionList actions,Entity item,Zone zone,Cell origin,string verb,string liquid,Entity target,int x,int y,int count,string label)
        {
            string command=Prefix+verb+"|"+Escape(zone.ZoneID)+"|"+N(origin.X)+"|"+N(origin.Y)+"|"+N(x)+"|"+N(y)
                +"|"+Escape(target?.ID)+"|"+N(count)+"|"+Escape(liquid);
            actions.AddAction("MaterialField",label,command,'\0',18);
        }

        internal static bool TryAct(Entity actor,Entity item,Zone zone,string command,InventoryTransaction tx)
        {
            if(!IsCommand(command))return false;
            string invalid=ValidateSource(actor,item,zone,out string verb,out string liquid);
            if(invalid!=null||tx==null)return Reject(actor,item,invalid??"missing-transaction");
            string[] f=command.Split('|');
            if(f.Length!=10||f[1]!=verb||!Parse(f[3],out int ox)||!Parse(f[4],out int oy)
                ||!Parse(f[5],out int x)||!Parse(f[6],out int y)||!Parse(f[8],out int count))return Reject(actor,item,"malformed-selection");
            var origin=zone.GetEntityCell(actor);
            if(WorldResourceActions.Decode(f[2])!=zone.ZoneID||origin.X!=ox||origin.Y!=oy||count!=Quantity(item))return Reject(actor,item,"stale-selection");
            string selectedLiquid=WorldResourceActions.Decode(f[9]);
            if(verb=="Wick")liquid=selectedLiquid;
            else if(verb=="Kindle"&&!string.IsNullOrEmpty(f[7])) { if(selectedLiquid!="")return Reject(actor,item,"wrong-material-layer"); }
            else if(selectedLiquid!=liquid)return Reject(actor,item,"wrong-material-layer");
            Entity target=string.IsNullOrEmpty(f[7])?null:WorldResourceActions.ExactGround(zone,f[7]);
            bool entitySelection=!string.IsNullOrEmpty(f[7]);
            if(entitySelection?(target==null||zone.GetEntityCell(target)?.X!=x||zone.GetEntityCell(target)?.Y!=y
                ||!CanTarget(actor,target,zone,verb,liquid,null)):!CanGround(zone,origin,verb,liquid,x,y))return Reject(actor,item,"recipient-unavailable");
            if(!tx.TryClaim(actor,actor,command)||!tx.TryClaim(item,actor,command)||(target!=null&&!tx.TryClaim(target,actor,command)))return Reject(actor,item,"in-progress");

            var inventory=actor.GetPart<InventoryPart>();var physics=item.GetPart<PhysicsPart>();var itemParts=item.Parts.ToArray();
            string blueprint=item.BlueprintName;
            var targetPhysics=target?.GetPart<PhysicsPart>();var thermal=target?.GetPart<ThermalPart>();var material=target?.GetPart<MaterialPart>();
            float temperature=thermal?.Temperature??0,capacity=thermal?.HeatCapacity??0,flame=thermal?.FlameTemperature??0;
            float combustibility=material?.Combustibility??0,volatility=material?.Volatility??0;
            var freeze=target?.GetEffect<FrozenEffect>();float cold=freeze?.Cold??0;
            var originalLayer=Layer(zone,x,y,liquid);int originalTurns=originalLayer?.Turns??0,originalCharge=zone.TileState.Charge(x,y);
            LiquidCoveredEffect incoming=null; bool coatInstalled=false;
            Func<bool> targetCurrent=()=>entitySelection
                ?WorldResourceActions.ExactGround(zone,f[7])==target&&zone.GetEntityCell(target)?.X==x&&zone.GetEntityCell(target)?.Y==y
                    &&target.GetPart<PhysicsPart>()==targetPhysics&&target.GetPart<ThermalPart>()==thermal&&target.GetPart<MaterialPart>()==material
                    &&(thermal==null||thermal.Temperature==temperature&&thermal.HeatCapacity==capacity&&thermal.FlameTemperature==flame)
                    &&(material==null||material.Combustibility==combustibility&&material.Volatility==volatility)
                    &&(verb!="Warm"||target.GetEffect<FrozenEffect>()==freeze&&freeze.Cold==cold)
                    &&CanTarget(actor,target,zone,verb,liquid,coatInstalled?incoming:null)
                :CanGround(zone,origin,verb,liquid,x,y)&&ReferenceEquals(Layer(zone,x,y,liquid),originalLayer)
                    &&(originalLayer==null||originalLayer.Turns==originalTurns)&&(verb!="Charge"||zone.TileState.Charge(x,y)==originalCharge);

            var payment=InventoryTransferSnapshot.Capture(inventory,item);tx.Do(null,payment.Restore);
            if(!payment.Apply(()=>inventory.TryConsumeOne(item))||!payment.ClaimChanges(tx,actor,command))return Reject(actor,item,"payment-refused");
            Func<bool> current=()=>WorldResourceActions.ActorCurrent(actor,zone)&&actor.GetPart<StatusEffectsPart>()?.IsActionBlocked()!=true
                &&zone.GetEntityCell(actor)==origin&&actor.GetPart<InventoryPart>()==inventory&&item.BlueprintName==blueprint&&item.Parts.SequenceEqual(itemParts)
                &&(count>1?WorldResourceActions.Carried(actor,item,false)&&Quantity(item)==count-1
                    :!inventory.Objects.Contains(item)&&item.SpatialZone==null&&physics.InInventory==null&&physics.Equipped==null)
                &&targetCurrent();
            if(!current())return Reject(actor,item,"changed-during-payment");
            if(verb=="Coat")
            {
                // The normal effect veto belongs before payment commits. Exact
                // lifecycle removal reverses its stat deltas on outer failure.
                var originalEffects=target.GetPart<StatusEffectsPart>();
                incoming=new LiquidCoveredEffect(liquid,liquid=="pitch"?PitchAmount:HoneyAmount);
                var staged=incoming;
                tx.Do(null,()=>
                {
                    var status=target.GetPart<StatusEffectsPart>();status?.RemoveEffect(staged);
                    if(originalEffects==null&&status?.EffectCount==0)target.RemovePart(status);
                });
                // A BeforeApplyEffect observer can independently add a coat.
                // Recheck at the intrinsic mutation boundary so OnStack never
                // merges our refunded dose into that independent replacement.
                if(!target.ApplyEffectWithReceipt(incoming,actor,zone,
                    ()=>{if(!current())throw new InvalidOperationException("The recipient changed before the coating could be applied.");},
                    ()=>coatInstalled=target.GetEffect<LiquidCoveredEffect>()==incoming)
                    ||target.GetEffect<LiquidCoveredEffect>()!=incoming||!current())
                    return Reject(actor,item,"coating-refused-or-changed");
            }
            tx.BeforeCommit(()=>
            {
                if(current())return true;
                Reject(actor,item,"changed-before-commit");return false;
            });
            bool provokes=target!=null&&Provokes(actor,target,verb);
            // Thermal effects and network reactions can destroy or hurt actors;
            // they must never execute while a later inventory veto can refund.
            if(verb!="Coat")tx.AfterCommit(()=>Apply(actor,target,zone,verb,liquid,x,y));
            if(provokes)tx.AfterCommit(()=>target.GetPart<BrainPart>()?.SetPersonallyHostile(actor));
            tx.AfterCommit(()=>ZoneRenderHooks.MarkCellDirty(x,y,"MaterialField"));
            tx.AfterCommit(()=>Diag.Record("event","MaterialFieldUsed",actor,target??item,new{blueprint,verb,liquid,x,y,spent=1,provoked=provokes}));
            tx.AfterCommit(()=>MessageLog.Add(Message(verb,liquid)));
            return true;
        }

        static void Apply(Entity actor,Entity target,Zone zone,string verb,string liquid,int x,int y)
        {
            if(verb=="Kindle") {if(target!=null)Kindle(actor,target,zone);else KindleTile(actor,zone,x,y);}
            else if(verb=="Cool")Heat(actor,target,zone,CoolingJoules);
            else if(verb=="Warm")Heat(actor,target,zone,WarmingJoules);
            else if(verb=="Wick")zone.TileState.RemoveCoating(x,y,liquid);
            else
            {
                if(verb=="Film")ZoneTileStateSystem.WriteCoating(zone,x,y,liquid,FilmTurns,actor,"MaterialField");
                else if(verb=="Freeze")ZoneTileStateSystem.ApplyColdToTile(zone,x,y,actor,"MaterialField");
                else if(verb=="Charge")ZoneTileStateSystem.AddCharge(zone,x,y,1,actor,"MaterialField");
                ZoneTileStateSystem.ResolveAfterAbility(zone,actor);
            }
        }
        static string ValidateSource(Entity actor,Entity item,Zone zone,out string verb,out string liquid)
        {
            verb=liquid="";
            switch(item?.BlueprintName)
            {
                case "FireMoss":verb="Kindle";liquid="oil";break;
                case "FrostLichen":verb="Freeze";liquid="water";break;
                case "GlacierSalt":verb="Cool";break;
                case "EmberFruit":verb="Warm";break;
                case "GlimmerBrine":verb="Film";liquid="brine";break;
                case "SparkRoot":verb="Charge";break;
                case "PrismreedPith":verb="Wick";break;
                case "LampOil":verb="Film";liquid="oil";break;
                case "SlipsedgeGel":verb="Film";liquid="gel";break;
                case "PitchpodResin":verb="Coat";liquid="pitch";break;
                case "Honeycomb":verb="Coat";liquid="honey";break;
                default:return "unsupported-material";
            }
            if(!WorldResourceActions.ActorCurrent(actor,zone)||actor.GetPart<StatusEffectsPart>()?.IsActionBlocked()==true)return "actor-unavailable";
            if(!WorldResourceActions.Carried(actor,item,false))return "not-carried";
            if(liquid!=""&&(!LiquidRegistry.IsInitialized||LiquidRegistry.Get(liquid)==null))return "missing-liquid-definition";
            return null;
        }
        static bool CanTarget(Entity actor,Entity target,Zone zone,string verb,string liquid,LiquidCoveredEffect staged)
        {
            if(!WorldResourceActions.Nearby(actor,target,zone)||WorldResourceActions.ExactGround(zone,Escape(target.ID))!=target
                ||CombatSystem.IsDeathHandled(target)||target.GetStatValue("Hitpoints",1)<=0
                ||target.GetPart<RenderPart>() is not RenderPart render||render.ParentEntity!=target||!render.Visible
                ||target.GetPart<DestructiblePart>() is DestructiblePart structure&&(structure.Gone||structure.HP<=0))return false;
            bool visible=false;
            foreach(var contact in zone.GetOccupiedCells(target))
                if(contact!=null&&contact.IsVisible&&SpatialQuery.DistanceToCell(zone,actor,contact.X,contact.Y)<=1){visible=true;break;}
            if(!visible)return false;
            if(verb=="Kindle")return CanKindle(target);
            var thermal=target.GetPart<ThermalPart>();
            if(verb=="Cool")return ValidThermal(target,thermal)&&thermal.Temperature>thermal.AmbientTemperature;
            if(verb=="Warm")return ValidThermal(target,thermal)&&target.GetEffect<FrozenEffect>() is FrozenEffect frozen
                &&frozen.Owner==target&&frozen.Duration!=0&&Finite(frozen.Cold)&&frozen.Cold>0;
            if(verb!="Coat"||!target.HasTag("Creature"))return false;
            var status=target.GetPart<StatusEffectsPart>();
            if(status!=null&&status.ParentEntity!=target)return false;
            var coats=status?.GetAllEffects().OfType<LiquidCoveredEffect>().ToArray();
            return staged==null?(coats==null||coats.Length==0)
                :coats!=null&&coats.Length==1&&coats[0]==staged&&staged.Owner==target&&staged.LiquidId==liquid
                    &&staged.Amount==(liquid=="pitch"?PitchAmount:HoneyAmount)&&staged.Duration==Effect.DURATION_INDEFINITE;
        }
        static bool CanGround(Zone zone,Cell origin,string verb,string liquid,int x,int y)
        {
            if(origin==null||Math.Abs((long)x-origin.X)>1||Math.Abs((long)y-origin.Y)>1)return false;
            var cell=zone.GetCell(x,y);if(cell==null||!cell.IsVisible||!cell.IsPassable())return false;
            if(verb=="Film")return (liquid=="oil"||liquid=="brine"||liquid=="gel")&&zone.TileState.CoatingTurns(x,y,liquid)<FilmTurns;
            if(verb=="Kindle")return liquid=="oil"&&CanKindleTile(zone,x,y);
            if(verb=="Freeze")return liquid=="water"&&zone.TileState.HasCoating(x,y,"water")&&TileReactionSystem.IsInitialized;
            if(verb=="Charge")return string.IsNullOrEmpty(liquid)&&TileReactionSystem.IsInitialized&&TilePropagationSystem.IsConductive(zone,x,y)
                &&zone.TileState.Charge(x,y)<ZoneTileState.MaxEnergy;
            if(verb!="Wick"||string.IsNullOrEmpty(liquid)||liquid=="ice"||liquid=="lava"||!LiquidRegistry.IsInitialized||LiquidRegistry.Get(liquid)==null)return false;
            var layer=Layer(zone,x,y,liquid);if(layer==null||layer.Turns<=0||layer.Turns==ZoneTileState.Permanent)return false;
            foreach(var owner in cell.Occupants)
            {
                if(owner.GetPart<LiquidPoolPart>() is LiquidPoolPart pool&&pool.Volume>0)return false;
                if(owner.GetPart<TileStateSourcePart>() is TileStateSourcePart source&&source.Coating==liquid&&source.CoatingTurns>0)return false;
            }
            return true;
        }
        /// <summary>Shared with held-torch ignition: exact simulation threshold,
        /// never a combustible-name guess or heat that cannot kindle this owner.</summary>
        internal static bool CanKindle(Entity target)
        {
            var heat=target?.GetPart<ThermalPart>();var material=target?.GetPart<MaterialPart>();var fuel=target?.GetPart<FuelPart>();
            if(target==null||target.HasTag("Creature")||!ValidThermal(target,heat)||material?.ParentEntity!=target
                ||!Finite(material.Combustibility)||material.Combustibility<=0||!Finite(material.Volatility)
                ||target.GetEffect<BurningEffect>() is BurningEffect burn&&burn.Duration!=0&&burn.Intensity>0
                ||target.GetEffect<WetEffect>() is WetEffect wet&&(!Finite(wet.Moisture)||wet.Moisture>.35f)
                ||fuel!=null&&(fuel.ParentEntity!=target||!Finite(fuel.FuelMass)||fuel.FuelMass<=0))return false;
            float threshold=heat.FlameTemperature-(material.Volatility>0?material.Volatility*100:0);
            return Finite(threshold)&&heat.Temperature<threshold&&heat.Temperature+KindlingJoules/heat.HeatCapacity>=threshold;
        }
        internal static void Kindle(Entity actor,Entity target,Zone zone)=>Heat(actor,target,zone,KindlingJoules);
        internal static bool CanKindleTile(Zone zone,int x,int y)=>zone!=null&&TileReactionSystem.IsInitialized
            &&zone.TileState.HasCoating(x,y,"oil")&&!zone.TileState.HasResidue(x,y,"embers");
        internal static void KindleTile(Entity actor,Zone zone,int x,int y)
        {ZoneTileStateSystem.AddHeat(zone,x,y,1,actor,"MaterialField");ZoneTileStateSystem.ResolveAfterAbility(zone,actor);}
        static void Heat(Entity actor,Entity target,Zone zone,float joules)
        {
            var e=GameEvent.New("ApplyHeat");e.SetParameter("Joules",joules);e.SetParameter("Radiant",false);e.SetParameter("Source",actor);e.SetParameter("Zone",zone);
            try{target.FireEvent(e);}finally{e.Release();}
        }
        static bool ValidThermal(Entity target,ThermalPart heat)=>heat?.ParentEntity==target&&heat!=null
            &&Finite(heat.Temperature)&&Finite(heat.AmbientTemperature)&&Finite(heat.HeatCapacity)&&heat.HeatCapacity>0
            &&Finite(heat.FlameTemperature)&&Finite(heat.FreezeTemperature)&&Finite(heat.VaporTemperature)
            &&Finite(heat.Temperature+KindlingJoules/heat.HeatCapacity)&&Finite(heat.Temperature+CoolingJoules/heat.HeatCapacity);
        static bool Provokes(Entity actor,Entity target,string verb)=>(verb=="Coat"||verb=="Cool")&&target!=actor&&!BrainPart.ArePartyAligned(actor,target);
        static ZoneTileState.Layer Layer(Zone zone,int x,int y,string id)
        {var state=zone.TileState.Get(x,y);if(state!=null)foreach(var layer in state.Coatings)if(layer?.Id==id)return layer;return null;}
        static string Label(string verb)=>verb=="Kindle"?"kindle":verb=="Cool"?"apply cold pack to":verb=="Warm"?"warm frozen":verb=="Coat"?"smear sticky coating on":verb=="Freeze"?"freeze wet ground":"discharge into conductor";
        static string Message(string verb,string liquid)=>verb=="Coat"?"You smear "+liquid+" onto the creature. Sticky coating spoils agility and dodge, and worsens fire damage."
            :verb=="Cool"?"You press on the cold pack. Cooling can extinguish hot flames, but may freeze the recipient."
            :verb=="Warm"?"You press warm pulp against the ice. The warmth helps it thaw."
            :verb=="Kindle"?"You kindle the existing fuel. Fire can spread."
            :verb=="Freeze"?"The wet ground freezes. Ice can catch allies and trip anyone crossing it."
            :verb=="Charge"?"The root discharges into the conductor. Anyone on the connected wet or metal route is at risk."
            :verb=="Wick"?"You wick away the thin "+liquid+" film. Other ground hazards remain."
            :"You spread a thin "+liquid+" film. "+(liquid=="oil"?"It is slippery and flammable.":liquid=="gel"?"It is slippery and conducts electricity.":"It conducts electricity.");
        static int Quantity(Entity item)=>item.GetPart<StackerPart>()?.StackCount??1;
        static string N(int n)=>n.ToString(CultureInfo.InvariantCulture);
        static bool Parse(string s,out int n)=>int.TryParse(s,NumberStyles.Integer,CultureInfo.InvariantCulture,out n);
        static string Escape(string s)=>Uri.EscapeDataString(s??"");
        static bool Finite(float x)=>WorldResourceActions.Finite(x);
        static bool Reject(Entity actor,Entity item,string reason)
        {Diag.Record("event","MaterialFieldRejected",actor,item,new{reason});MessageLog.Add("You cannot use that material here ("+reason.Replace('-',' ')+").");return false;}
    }
}
