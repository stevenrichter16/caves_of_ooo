using System;
using System.Globalization;
using System.Linq;
using CavesOfOoo.Core.Inventory;
using CavesOfOoo.Diagnostics;

namespace CavesOfOoo.Core
{
    /// <summary>Generic vessels draw from the same usable fresh-water wells as
    /// waterskins. Pools remain authoritative; no source or saved state is made.</summary>
    internal static class WellFlaskFilling
    {
        const string Prefix="FillWellFlask|";
        internal static bool IsCommand(string command)=>command!=null&&command.StartsWith(Prefix,StringComparison.Ordinal);
        internal static bool OwnsReceipt(Entity vessel,string command)=>IsCommand(command)&&vessel?.GetPart<LiquidVesselPart>()?.ParentEntity==vessel;
        static bool Recipient(Entity actor,Entity vessel,Zone zone,out LiquidVesselPart p)
        {
            p=vessel?.GetPart<LiquidVesselPart>();
            return WorldResourceActions.ActorCurrent(actor,zone)&&actor.GetPart<StatusEffectsPart>()?.IsActionBlocked()!=true
                &&WorldResourceActions.Carried(actor,vessel)&&p?.ParentEntity==vessel&&p.Capacity>0&&p.Volume>=0&&p.Volume<=p.Capacity
                &&(p.Volume==0?string.IsNullOrEmpty(p.LiquidId):p.LiquidId=="water")&&LiquidRegistry.IsInitialized&&LiquidRegistry.Get("water")!=null;
        }
        static bool Source(Entity actor,Entity source,Zone zone,bool requireWater=true)
        {
            var well=source?.GetPart<WellPart>();
            return well?.ParentEntity==source&&source!=null&&well!=null&&well.Stock>=-1
                &&!source.HasPart<ContainedLiquidSourcePart>()&&WaterVesselService.IsFreshSource(actor,zone,source,requireWater);
        }
        static int Units(Entity source)=>source.GetPart<LiquidPoolPart>()?.Volume??source.GetPart<WellPart>().Stock;
        static string Kind(Entity source)=>source.HasPart<LiquidPoolPart>()?"p":"s";
        internal static void AddActions(Entity actor,Entity vessel,Entity source,Zone zone,InventoryActionList actions)
        {
            if(actions==null||!Recipient(actor,vessel,zone,out var p)||p.Volume==p.Capacity||!Source(actor,source,zone))return;
            int stock=Units(source),amount=stock==-1?p.Capacity-p.Volume:Math.Min(stock,p.Capacity-p.Volume);
            if(amount<=0)return;
            string command=Prefix+Uri.EscapeDataString(source.ID)+"|"+Kind(source)+"|"+stock.ToString(CultureInfo.InvariantCulture)
                +"|"+p.Volume.ToString(CultureInfo.InvariantCulture)+"|"+p.Capacity.ToString(CultureInfo.InvariantCulture);
            string hint="Draws "+amount+" water units into this flask ("+(p.Volume+amount)+"/"+p.Capacity+"). "
                +(stock==-1?"This well has its ordinary renewing supply.":"The source holds "+stock+" units; the draw leaves "+(stock-amount)+".")+" One action.";
            actions.AddActionWithHint("FillWellFlask","draw "+amount+" water into flask from "+source.GetDisplayName(),command,hint,'\0',19,false,ActionTime.OneAction)
                .AtEntity(zone,source).InGroup(ToolGroups.Fill);
        }
        internal static bool TryAct(Entity actor,Entity vessel,Zone zone,string command,InventoryTransaction tx)
        {
            if(!IsCommand(command)||!Recipient(actor,vessel,zone,out var part))return false;
            var bits=command.Split('|');
            if(bits.Length!=6||(bits[2]!="s"&&bits[2]!="p")
                ||!int.TryParse(bits[3],NumberStyles.Integer,CultureInfo.InvariantCulture,out int quote)
                ||!int.TryParse(bits[4],NumberStyles.None,CultureInfo.InvariantCulture,out int before)
                ||!int.TryParse(bits[5],NumberStyles.None,CultureInfo.InvariantCulture,out int capacity))return Reject(actor,vessel,"malformed_quote");
            var matches=zone.GetReadOnlyEntities().Where(e=>e.ID==Uri.UnescapeDataString(bits[1])).ToArray();
            var source=matches.Length==1?matches[0]:null;
            if(source==null||!Source(actor,source,zone)||Kind(source)!=bits[2]||Units(source)!=quote
                ||part.Volume!=before||part.Capacity!=capacity||before==capacity)return Reject(actor,vessel,"selection_changed");
            var pack=actor.GetPart<InventoryPart>();var well=source.GetPart<WellPart>();var pool=source.GetPart<LiquidPoolPart>();
            var actorPhysics=actor.GetPart<PhysicsPart>();var physical=vessel.GetPart<PhysicsPart>();var sourcePhysical=source.GetPart<PhysicsPart>();
            var actorCell=zone.GetEntityCell(actor);var sourceCell=zone.GetEntityCell(source);
            var vesselParts=vessel.Parts.ToArray();var sourceParts=source.Parts.ToArray();
            string actorId=actor.ID,vesselId=vessel.ID,sourceId=source.ID;int stock=well.Stock;
            bool metered=well.MeteredHydration;int amount=quote==-1?capacity-before:Math.Min(quote,capacity-before);
            bool Current(int filled,int water)
            {
                return Recipient(actor,vessel,zone,out var current)&&current==part&&actor.ID==actorId&&vessel.ID==vesselId&&source.ID==sourceId
                    &&actor.GetPart<InventoryPart>()==pack&&actor.GetPart<PhysicsPart>()==actorPhysics&&zone.GetEntityCell(actor)==actorCell
                    &&vessel.GetPart<PhysicsPart>()==physical&&part.Capacity==capacity&&part.Volume==filled
                    &&(filled==0?string.IsNullOrEmpty(part.LiquidId):part.LiquidId=="water")
                    &&Source(actor,source,zone,false)&&zone.GetEntityCell(source)==sourceCell&&source.GetPart<PhysicsPart>()==sourcePhysical
                    &&source.GetPart<WellPart>()==well&&source.GetPart<LiquidPoolPart>()==pool&&well.MeteredHydration==metered
                    &&(pool==null?well.Stock==water:pool.Volume==water&&well.Stock==stock)
                    &&vessel.Parts.SequenceEqual(vesselParts)&&source.Parts.SequenceEqual(sourceParts)
                    &&vesselParts.All(p=>p.ParentEntity==vessel)&&sourceParts.All(p=>p.ParentEntity==source);
            }
            if(amount<=0||!Current(before,quote))return Reject(actor,vessel,"invalid_source");
            bool own=tx==null;tx??=new InventoryTransaction();
            try
            {
                if(!tx.TryClaimAction(actor)||!tx.TryClaim(actor,actor,command)||!tx.TryClaim(vessel,actor,command)||!tx.TryClaim(source,actor,command))return Reject(actor,vessel,"in_progress");
                tx.Do(null,()=>
                {
                    if(vessel.ID==vesselId&&vessel.GetPart<LiquidVesselPart>()==part&&part.ParentEntity==vessel
                        &&(part.LiquidId=="water"||part.Volume==0&&string.IsNullOrEmpty(part.LiquidId)))
                    {part.Volume=WaterQuantityUndo.Restore(part.Volume,amount,part.Capacity);part.LiquidId=part.Volume==0?"":"water";}
                    else Diag.Record("liquid","WellFlaskCreditUndoIdentityChanged",actor,vessel);
                    if(quote==-1)return;
                    if(source.ID!=sourceId){Diag.Record("liquid","WellFlaskDebitUndoIdentityChanged",actor,source);return;}
                    if(pool!=null)
                    {
                        if(source.GetPart<LiquidPoolPart>()==pool&&pool.ParentEntity==source&&pool.LiquidId=="water")pool.Volume=WaterQuantityUndo.Restore(pool.Volume,-amount);
                        else Diag.Record("liquid","WellFlaskDebitUndoIdentityChanged",actor,source);
                    }
                    else if(source.GetPart<WellPart>()==well&&well.ParentEntity==source)well.Stock=WaterQuantityUndo.Restore(well.Stock,-amount,endlessAllowed:true);
                    else Diag.Record("liquid","WellFlaskDebitUndoIdentityChanged",actor,source);
                });
                part.Volume+=amount;part.LiquidId="water";
                if(pool!=null)pool.Volume-=amount;else if(quote>=0)well.Stock-=amount;
                int after=quote==-1?-1:quote-amount;
                tx.BeforeCommit(()=>Current(before+amount,after));
                var audio=WorldAudioUtility.Take(actor,vessel,zone,WorldAudioCue.WaterFill,source,false,()=>Current(before+amount,after));
                tx.AfterCommit(()=>audio?.Publish());
                tx.AfterCommit(()=>
                {
                    if(source.ID==sourceId&&zone.GetEntityCell(source)==sourceCell&&source.GetPart<WellPart>()==well&&source.GetPart<LiquidPoolPart>()==pool)
                        ZoneRenderHooks.MarkCellDirty(sourceCell,"WellFlask.SourceChanged");
                    Diag.Record("liquid","WellFlaskFilled",actor,vessel,new{source=sourceId,amount,remaining=after});
                    MessageLog.Add("You draw "+amount+" water into the flask. ("+(before+amount)+"/"+capacity+")");
                });
                if(own)tx.Commit();return true;
            }
            finally{if(own)tx.Rollback();}
        }
        static bool Reject(Entity actor,Entity vessel,string reason)
        {Diag.Record("liquid","WellFlaskRejected",actor,vessel,new{reason});MessageLog.Add("You cannot draw into that flask; the well, flask or access has changed.");return false;}
    }
}
