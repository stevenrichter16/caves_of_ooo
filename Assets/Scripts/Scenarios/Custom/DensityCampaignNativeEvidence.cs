using System;
using System.Collections.Generic;
using System.Linq;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using Newtonsoft.Json.Linq;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Read-only joins of actual native action, currency and recovery
    /// receipts. Quoted stock is never treated as acquired money or paid turns.</summary>
    public static class DensityCampaignNativeEvidence
    {
        public const string MarkerKind="DensityCampaignActionStart";
        public struct ClockReceipt
        { public int CompletedTurns,PureClock,Healed; public string Site; }
        public static bool TryClock(IReadOnlyList<Diag.Entry> rows,string marker,string actor,string kind,
            int beforeEnergy,int afterEnergy,int elapsedTicks,int speed,out ClockReceipt result)
        {
            result=default;
            if(beforeEnergy<0||afterEnergy<0||elapsedTicks<0||speed<=0
                ||!Window(rows,marker,actor,out var fresh)||!(kind=="local"||kind=="map"||kind=="rest"))return false;
            if(!TryCompletedTurns(fresh,actor,out int ends,out int ordinaryEnds))return false;
            var pure=fresh.Where(r=>(r.Category=="worldmap"&&r.Kind=="Stepped")||(r.Category=="furniture"&&r.Kind=="Rested")).ToArray();
            if(kind=="local") {if(pure.Length!=0||ordinaryEnds!=1)return false;}
            else
            {
                if(pure.Length!=1||pure[0].ActorId!=actor)return false;
                var row=pure[0];
                if(kind=="map")
                {
                    if(row.Category!="worldmap"||row.Kind!="Stepped"||ordinaryEnds!=1
                        ||!Int(row,"turnsCost",out result.PureClock)||result.PureClock<=0
                        ||!Int(row,"toWorldX",out int x)||!Int(row,"toWorldY",out int y)
                        ||x<0||x>=WorldMap.Width||y<0||y>=WorldMap.Height)return false;
                }
                else
                {
                    if(row.Category!="furniture"||row.Kind!="Rested"||ends!=0
                        ||!Int(row,"clockAdvanced",out result.PureClock)||result.PureClock!=RestSystem.RestClockTurns
                        ||!Int(row,"healed",out result.Healed)||result.Healed<0
                        ||!Text(row,"site",out result.Site)||string.IsNullOrEmpty(result.Site))return false;
                }
            }
            if(elapsedTicks<result.PureClock||(kind=="rest"&&(elapsedTicks!=result.PureClock||beforeEnergy!=afterEnergy)))return false;
            result.CompletedTurns=ends;
            return (long)beforeEnergy+(long)(elapsedTicks-result.PureClock)*speed-(long)ends*TurnManager.ActionThreshold==afterEnergy;
        }
        // The submitted input may finish a turn whose Begin predates the marker.
        // Every additional paid turn needs its own ordered Begin/veto/End witness;
        // a balanced energy equation alone cannot legitimize another ordinary End.
        // A final unblocked Begin is allowed: the player is awaiting their next input.
        static bool TryCompletedTurns(IReadOnlyList<Diag.Entry> rows,string actor,out int ends,out int ordinaryEnds)
        {
            ends=ordinaryEnds=0;bool begun=false,blocked=false;
            foreach(var row in rows)
            {
                if(row.Category!="turn"||row.ActorId!=actor)continue;
                if(row.Kind=="Begin")
                {
                    if(begun)return false;
                    begun=true;
                }
                else if(row.Kind=="ActionBlocked")
                {
                    if(!begun||blocked||!Text(row,"phase",out string phase)||phase!="BeginTakeAction")return false;
                    blocked=true;
                }
                else if(row.Kind=="End")
                {
                    if(!blocked&&++ordinaryEnds>1)return false;
                    ends++;begun=blocked=false;
                }
            }
            return !blocked;
        }
        public static bool RecoveryMatches(ClockReceipt receipt,string site,int hpBefore,int hpAfter,int maximum)
            =>hpBefore>0&&hpBefore<maximum&&hpAfter==maximum&&receipt.Site==site
                &&receipt.PureClock==RestSystem.RestClockTurns&&receipt.Healed==(long)hpAfter-hpBefore;

        public static bool TradeMatches(IReadOnlyList<Diag.Entry> rows,string marker,string player,string trader,string item,bool buy,
            int quotedWholeStackPrice,int playerBefore,int playerAfter,int traderBefore,int traderAfter)
        {
            if(string.IsNullOrEmpty(trader)||trader==player||string.IsNullOrEmpty(item)||quotedWholeStackPrice<=0
                ||playerBefore<0||playerAfter<0||traderBefore<0||traderAfter<0||!Window(rows,marker,player,out var fresh))return false;
            var trades=fresh.Where(r=>r.Category=="trade"&&(r.Kind=="Bought"||r.Kind=="Sold")).ToArray();
            if(trades.Length!=1)return false;var row=trades[0];
            if(row.ActorId!=player||row.TargetId!=trader||row.Kind!=(buy?"Bought":"Sold")
                ||!Text(row,"itemId",out string actualItem)||actualItem!=item
                ||!Int(row,"price",out int price)||price!=quotedWholeStackPrice
                ||!Int(row,"dramsAfter",out int drams)||drams!=playerAfter)return false;
            long delta=buy?-price:price;
            return (long)playerBefore+delta==playerAfter&&(long)traderBefore-delta==traderAfter;
        }
        static bool Window(IReadOnlyList<Diag.Entry> rows,string marker,string actor,out List<Diag.Entry> fresh)
        {
            fresh=null;if(rows==null||string.IsNullOrEmpty(marker)||string.IsNullOrEmpty(actor))return false;
            int start=-1;for(int i=0;i<rows.Count;i++)if(rows[i].TraceId==marker)
            {if(start>=0||rows[i].Category!="scenario"||rows[i].Kind!=MarkerKind||rows[i].ActorId!=actor)return false;start=i;}
            if(start<0)return false;var traces=new HashSet<string>(StringComparer.Ordinal);fresh=new List<Diag.Entry>();
            for(int i=start;i<rows.Count;i++)
            {
                var r=rows[i];if(string.IsNullOrEmpty(r.TraceId)||!traces.Add(r.TraceId))return false;
                if(i==start)continue;
                // A later action marker makes this ambiguous: callers must
                // submit the exact completed native action window.
                if(r.Category=="scenario"&&r.Kind==MarkerKind)return false;fresh.Add(r);
            }
            return true;
        }
        static JToken Value(Diag.Entry row,string field)
        {try{return string.IsNullOrEmpty(row.PayloadJson)?null:JObject.Parse(row.PayloadJson)[field];}catch(Newtonsoft.Json.JsonException){return null;}}
        static bool Int(Diag.Entry row,string field,out int value)
        {value=0;var token=Value(row,field);if(token?.Type!=JTokenType.Integer)return false;try{value=token.Value<int>();return true;}catch(Exception e)when(e is OverflowException||e is FormatException){return false;}}
        static bool Text(Diag.Entry row,string field,out string value)
        {var token=Value(row,field);value=token?.Type==JTokenType.String?(string)token:null;return value!=null;}
    }
}
