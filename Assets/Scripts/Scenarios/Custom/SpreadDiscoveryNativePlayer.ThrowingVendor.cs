using System;
using System.Collections;
using System.Linq;
using CavesOfOoo.Core;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Scenarios.Custom
{
    public sealed partial class SpreadDiscoveryNativePlayer
    {
        bool _throwingVendorAttempted, _throwingVendorRevisited;
        string _throwingVendorId;
        int _throwingVendorStamp;

        // A bounded optional source-guided detour in the same Bombardier29
        // campaign. Every transfer uses native trade input; missing stock,
        // insufficient cash and unsafe approaches are observations, not failures.
        IEnumerator ThrowingNaturalVisitVendor(bool revisit=false)
        {
            if(_connectedBuild!="bombardier"||_throwingNaturalSeed!=29||!NaturalAlive)yield break;
            if(revisit)
            {
                if(_throwingVendorRevisited||string.IsNullOrEmpty(_throwingVendorId)
                    ||(long)Tick-_throwingVendorStamp<=TraderRestockSystem.RestockIntervalTurns)yield break;
                _throwingVendorRevisited=true;
            }
            else {if(_throwingVendorAttempted)yield break;_throwingVendorAttempted=true;}
            if(WorldMap.FromZoneID(Zone.ZoneID).z!=0||NaturalActions>_throwingNaturalActionBudget-100
                ||Player.GetStatValue("Hitpoints")<20)
            {
                _observations.Add(new{phase="throwing-natural-vendor-unavailable",revisit,
                    reason="surface-health-or-remaining-action-bound",actions=NaturalActions});yield break;
            }
            int begin=NaturalActions;string origin=Zone.ZoneID;
            yield return CloseNormal();yield return TravelSurface(WorldMap.StartingZoneID);
            var merchant=Zone.GetReadOnlyEntities().Where(e=>e.BlueprintName=="Apothecary"
                &&e.HasPart<ConversationPart>()&&e.GetStatValue("Hitpoints")>0
                &&(!revisit||e.ID==_throwingVendorId)).OrderBy(e=>e.ID,StringComparer.Ordinal).FirstOrDefault();
            string refusal=merchant==null?"authored-apothecary-absent":null;
            for(int steps=0;merchant!=null&&SpatialQuery.Distance(Zone,Player,merchant)>1&&steps<60;steps++)
            {
                if(!NaturalAlive||Player.GetStatValue("Hitpoints")<20||NaturalActions-begin>=65)
                {refusal="unsafe-or-bounded-approach";break;}
                var path=PathTo(c=>SpatialQuery.DistanceToCell(Zone,merchant,c.X,c.Y)<=1);
                if(path==null||path.Count==0||!Safe(Zone,Zone.GetCell(path[0].x,path[0].y),ThreatClearance))
                {refusal="no-current-safe-approach";break;}
                yield return StepTo(path[0].x,path[0].y);
            }
            if(merchant!=null&&SpatialQuery.Distance(Zone,Player,merchant)>1&&refusal==null)refusal="approach-bound";
            if(refusal==null&&merchant!=null)
            {
                int beforeTick=Tick,beforeEnergy=Energy,purse=TradeSystem.GetDrams(Player);
                var stock=TradeSystem.GetTraderStock(merchant);
                double performance=TradeSystem.GetTradePerformance(Player);
                var before=stock.Select(e=>new{e.ID,e.BlueprintName,units=Units(e),
                    price=TradeSystem.GetBuyPrice(e,performance,merchant)}).ToArray();
                int oldStamp=_throwingVendorStamp;
                _throwingVendorId=merchant.ID;
                _throwingVendorStamp=merchant.GetIntProperty(TraderRestockSystem.LastRestockProp,int.MinValue);
                yield return Chat(merchant);
                if(!ConversationManager.VisibleChoices.Any(c=>c.Actions?.Any(a=>a.Key=="StartTrade")==true))
                    refusal="no-current-native-trade-choice";
                else
                {
                    yield return Choice(c=>c.Actions?.Any(a=>a.Key=="StartTrade")==true);
                    Require(State=="TradeOpen"&&_input.TradeUI.IsOpen,"real merchant trade page");
                    yield return Capture(revisit?"throwing-vendor-returned-stock":"throwing-vendor-actual-stock");
                    var desired=stock.Where(e=>e.BlueprintName=="WaterTonic"||e.BlueprintName=="AcidTonic")
                        .OrderBy(e=>TradeSystem.GetBuyPrice(e,performance,merchant)<=purse?0:1)
                        .ThenBy(e=>e.BlueprintName=="WaterTonic"?0:1)
                        .ThenBy(e=>TradeSystem.GetBuyPrice(e,performance,merchant)).FirstOrDefault();
                    int price=desired==null?0:TradeSystem.GetBuyPrice(desired,performance,merchant);
                    string result=desired==null?"water-and-acid-unavailable":price>purse?"actual-row-unaffordable":revisit?"revisit-observed-no-extra-purchase":"purchased";
                    if(desired!=null&&price<=purse&&!revisit)
                    {
                        var rows=(IList)Field(_input.TradeUI,"_leftRows");int index=-1;
                        for(int i=0;i<rows.Count;i++)if(ReferenceEquals(Field(rows[i],"Item"),desired))index=i;
                        Require(index>=0,"exact real merchant row");
                        for(int moves=0;(int)Field(_input.TradeUI,"_leftCursor")!=index;moves++)
                        {Require(moves<80,"bounded native stock navigation");yield return Tap((int)Field(_input.TradeUI,"_leftCursor")<index?Key.DownArrow:Key.UpArrow);}
                        int units=Units(desired),packed=Packed(desired.BlueprintName),merchantPurse=TradeSystem.GetDrams(merchant);
                        yield return Tap(Key.Enter);
                        Require((bool)Field(_input.TradeUI,"_confirmActive"),"ordinary purchase confirmation");
                        yield return Capture("throwing-vendor-actual-price-confirmation");yield return Tap(Key.Enter);
                        Require(TradeSystem.GetDrams(Player)==purse-price&&TradeSystem.GetDrams(merchant)==merchantPurse+price
                            &&Packed(desired.BlueprintName)==packed+units&&!merchant.GetPart<InventoryPart>().Objects.Contains(desired),
                            "exact paid whole stock row and ordinary merged or retained custody");
                    }
                    _observations.Add(new{phase="throwing-natural-vendor-source",revisit,merchant=merchant.ID,
                        merchant.BlueprintName,zone=Zone.ZoneID,actionStart=begin,actions=NaturalActions-begin,
                        schedulerTick=Tick,oldRestockStamp=oldStamp,currentRestockStamp=_throwingVendorStamp,
                        naturalRestockBoundary=revisit&&(long)Tick-oldStamp>TraderRestockSystem.RestockIntervalTurns,
                        before,after=TradeSystem.GetTraderStock(merchant).Select(e=>new{e.ID,e.BlueprintName,units=Units(e)}).ToArray(),
                        result,selected=desired?.ID,selectedBlueprint=desired?.BlueprintName,price,
                        purseBefore=purse,purseAfter=TradeSystem.GetDrams(Player),
                        limit="Existing scheduler ticks drive entry restock; a new stamp alone does not prove shelf refill. No sale, stock grant or padded waiting."});
                }
                yield return CloseNormal();Require(Tick==beforeTick&&Energy==beforeEnergy,"actual trade viewing and current trade command remain free");
            }
            if(refusal!=null)_observations.Add(new{phase="throwing-natural-vendor-unavailable",revisit,reason=refusal,actions=NaturalActions-begin});
            if(NaturalAlive&&NaturalActions<_throwingNaturalActionBudget-8)
            {yield return CloseNormal();yield return TravelSurface(origin);}
            Require(NaturalActions-begin<=80,"optional real vendor detour bound");
        }
    }
}
