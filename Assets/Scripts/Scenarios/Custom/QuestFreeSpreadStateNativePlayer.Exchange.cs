using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using Newtonsoft.Json.Linq;
using UnityEngine.InputSystem;

namespace CavesOfOoo.Scenarios.Custom
{
    public sealed partial class QuestFreeSpreadStateNativePlayer
    {
        const string ExchangeZone="Overworld.11.7.0";
        const string ExchangeMerchantID="traveller:3:"+ExchangeZone;
        const string ExchangeReceipt="TravellerSpawn:3:"+ExchangeZone;
        const string ExchangeRoll="TravellerRoll:3:"+ExchangeZone;
        const string ExchangeBoundary="Ordinary seed3 N, actual map keys from11.10 to11.7, current native entry/Chat/Trade/exit-return. Fixed address is the first actual source winner in the retained prebounded census, not uninformed discovery. Original50drams; no transfer, grant, forced source, stock, actor, RNG or time edit. At most31 paid inputs,24 local approach steps,150seconds. Return is before normal300-tick restock; it does not prove this run's F5/F6 persistence or permanent finite stock. Root must view screenshots.";
        Dictionary<string,bool> _exchangeChannelStore;
        Dictionary<string,bool?> _exchangeOldChannels;
        Diag.Entry[] _exchangeLastWindow;
        static readonly string[] ExchangeChannels={"scenario","turn","worldmap","worldgen","trade"};

        void BeginExchangeDiagnostics()
        {
            _exchangeChannelStore=(Dictionary<string,bool>)typeof(Diag).GetField("_channels",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
            _exchangeOldChannels=ExchangeChannels.ToDictionary(k=>k,k=>_exchangeChannelStore.TryGetValue(k,out var value)?(bool?)value:null);
            foreach(var key in ExchangeChannels)Diag.SetChannel(key,true);
        }
        void RestoreExchangeDiagnostics()
        {
            if(_exchangeOldChannels==null)return;
            foreach(var pair in _exchangeOldChannels)if(pair.Value.HasValue)_exchangeChannelStore[pair.Key]=pair.Value.Value;else _exchangeChannelStore.Remove(pair.Key);
            _exchangeOldChannels=null;_exchangeChannelStore=null;
        }
        string ExchangeMarker(string label)
        {Diag.Record("scenario",DensityCampaignNativeEvidence.MarkerKind,actor:Player,payload:new{runId=RunId,label});return Diag.Snapshot(1).Single().TraceId;}
        Diag.Entry[] ExchangeWindow(string marker)
        {
            var rows=Diag.Snapshot(Diag.BufferCapacity).SkipWhile(r=>r.TraceId!=marker).ToArray();
            Require(rows.Length>0&&rows[0].TraceId==marker,"retained exact native exchange window");return rows;
        }
        static string Payload(Diag.Entry row,string key)=>string.IsNullOrEmpty(row.PayloadJson)?null:(string)JObject.Parse(row.PayloadJson)[key];
        IEnumerator ExchangePaid(IEnumerator action,string kind,string label)
        {
            Require(_paidInputs<PaidLimit&&State=="Normal"&&Player.GetStatValue("Hitpoints")>10,"bounded living exchange input");
            var actor=Player;int tick=Tick,energy=Energy,speed=actor.GetStatValue("Speed",100);string marker=ExchangeMarker(label);
            yield return action;yield return Settled();_paidInputs++;_exchangeLastWindow=ExchangeWindow(marker);
            DensityCampaignNativeEvidence.ClockReceipt proof=default;
            Require(ReferenceEquals(actor,Player)&&actor.GetStatValue("Speed",100)==speed
                &&DensityCampaignNativeEvidence.TryClock(_exchangeLastWindow,marker,actor.ID,kind,energy,Energy,Tick-tick,speed,out proof),"actual local/map action clock and energy: "+label);
            _observations.Add(new{phase=label,kind,beforeTick=tick,afterTick=Tick,beforeEnergy=energy,afterEnergy=Energy,receipt=proof,rows=_exchangeLastWindow});WriteReport();
            Require(Player.GetStatValue("Hitpoints")>10&&!CombatSystem.IsDeathHandled(Player),"ordinary exchange HP safety stop");
        }
        IEnumerator Exchange()
        {
            Require(Zone.ZoneID=="Overworld.11.10.0"&&Manager.Exploration.Version==SpreadExplorationPlan.CurrentVersion,"ordinary current default start and current manifest");
            Check("fixed_actual_winner_metadata",Manager.Exploration.TryGetPlacement(Manager,ExchangeZone,out var selected)&&selected.Family==SpreadExplorationFamily.RoadsideExchange);
            var originalPlayer=Player;int originalCount=Player.GetIntProperty("TravellerCount:3");
            Require(originalCount<WorldTravellers.MaximumEncounters&&!Player.IntProperties.ContainsKey(ExchangeRoll)&&Player.GetProperty(ExchangeReceipt)==null,"unused actual target entry and remaining ordinary allowance");
            _observations.Add(new{phase="fixed-source-before-travel",zone=ExchangeZone,initiallyCached=Manager.CachedZones.ContainsKey(ExchangeZone),originalCount,boundary="Metadata only. Observer does not GetZone or invoke OnZoneEntered; normal map descent owns generation and entry."});
            yield return ExchangePaid(Tap(Key.LeftShift,Key.Comma),"local","outward-map-ascent");Require(WorldMap.IsWorldMapZoneID(Zone.ZoneID),"actual map ascent");
            var mapCell=WorldMap.WorldCellToZoneCell(11,7);
            for(int n=0;At.X!=mapCell.zoneX||At.Y!=mapCell.zoneY;n++)
            {
                Require(n<3&&At.X==mapCell.zoneX,"exact fixed three-step northward map route");int y=At.Y-1;
                yield return ExchangePaid(Tap(Key.W),"map","outward-map-step");Require(At.X==mapCell.zoneX&&At.Y==y,"actual requested map coordinate");
            }
            yield return ExchangePaid(Tap(Key.LeftShift,Key.Period),"local","actual-source-descent");
            Check("native_map_arrival_same_player",ReferenceEquals(Player,originalPlayer)&&Zone.ZoneID==ExchangeZone&&ReferenceEquals(Manager.ActiveZone,Zone)&&ReferenceEquals(Manager.CachedZones[ExchangeZone],Zone));
            var sourceZone=Zone;var owners=sourceZone.GetReadOnlyEntities().Where(e=>e.ID==ExchangeMerchantID).ToArray();
            if(owners.Length!=1||Manager.Exploration.DispositionFor(ExchangeZone)!=2)
            {
                _observations.Add(new{phase="actual-entry-not-complete",ownerCount=owners.Length,disposition=Manager.Exploration.DispositionFor(ExchangeZone),rows=_exchangeLastWindow.Where(r=>r.Category=="worldgen").ToArray()});
                Unverified("roadside-exchange-entry","Actual fixed source entry did not commit one merchant/verge; roll is not retried and no alternative source is chosen.");yield return Capture("98-entry-unverified");yield break;
            }
            var merchant=owners[0];var initialBrain=merchant.GetPart<BrainPart>();
            bool placed=_exchangeLastWindow.Any(r=>r.Category=="worldgen"&&r.Kind=="TravellerEntryRoll"&&r.ActorId==Player.ID&&Payload(r,"zone")==ExchangeZone&&Payload(r,"reason")=="placed"&&Payload(r,"entityId")==merchant.ID);
            bool committed=_exchangeLastWindow.Any(r=>r.Category=="worldgen"&&r.Kind=="RoadsideExchange"&&r.ActorId==merchant.ID&&Payload(r,"zone")==ExchangeZone
                &&new[]{"committed-in-place","committed-relocated"}.Contains(Payload(r,"reason")));
            Check("actual_one_shot_entry_owner_and_f8_commit",placed&&committed&&CurrentExchangeMerchant(merchant)&&UniqueExchangeMerchant()
                &&Player.GetIntProperty("TravellerCount:3")==originalCount+1&&Player.GetIntProperty(ExchangeRoll)==1
                &&Player.GetProperty(ExchangeReceipt)==merchant.ID&&merchant.GetProperty("TravellerReceipt")==ExchangeReceipt
                &&initialBrain?.CurrentZone==Zone&&_input.TurnManager.GetSavedEntries().Count(e=>ReferenceEquals(e.Entity,merchant))==1);
            _observations.Add(new{phase="actual-entry-source",merchant=merchant.ID,origin=merchant.GetProperty(WorldTravellers.OriginProperty),destination=merchant.GetProperty(WorldTravellers.DestinationProperty),stock=ExchangeStock(merchant),purse=TradeSystem.GetDrams(merchant),player=Player.ID,position=Zone.GetEntityPosition(merchant),disposition=2});
            yield return Capture("01-actual-native-roadside-entry");
            for(int n=0;SpatialQuery.Distance(Zone,Player,merchant)!=1;n++)
            {
                if(n>=24||!CurrentExchangeMerchant(merchant))
                {Unverified("roadside-exchange-approach","Current merchant unavailable or24-step bound reached.");yield return Capture("98-approach-unverified");yield break;}
                var threats=ExchangeThreats();var path=ExchangePath(merchant,threats);
                if(path==null||path.Count==0||!ExchangeSafe(At,threats))
                {
                    _observations.Add(new{phase="actual-approach-refused",player=new[]{At.X,At.Y},merchant=Zone.GetEntityPosition(merchant),threats=threats.Select(e=>new{id=e.ID,position=Zone.GetEntityPosition(e)}).ToArray()});
                    Unverified("roadside-exchange-approach","No current bounded safe approach. No waits, actor moves, seed/source retries or safety weakening.");yield return Capture("98-approach-unverified");yield break;
                }
                var next=path[0];Require(ExchangeSafe(Zone.GetCell(next.x,next.y),ExchangeThreats()),"fresh native step footprint");
                yield return ExchangePaid(Tap(Direction(next.x-At.X,next.y-At.Y)),"local","actual-merchant-approach");Require(Zone==sourceZone&&At.X==next.x&&At.Y==next.y,"actual current ground step");
            }
            Require(CurrentExchangeMerchant(merchant)&&Zone.GetEntityCell(merchant).IsVisible&&merchant.GetPart<RenderPart>()?.Visible==true,"visible current adjacent merchant");
            yield return ExchangeOpenTrade(merchant);
            var item=TradeSystem.GetTraderStock(merchant).Where(e=>ExchangeOwns(merchant,e)&&ExchangeCanTransferExact(e,Player))
                .Where(e=>TradeSystem.GetBuyPrice(e,TradeSystem.GetTradePerformance(Player),merchant)>0&&TradeSystem.GetBuyPrice(e,TradeSystem.GetTradePerformance(Player),merchant)<=TradeSystem.GetDrams(Player))
                .OrderBy(e=>TradeSystem.GetBuyPrice(e,TradeSystem.GetTradePerformance(Player),merchant)).ThenBy(e=>e.ID,StringComparer.Ordinal).FirstOrDefault();
            if(item==null){Unverified("roadside-exchange-purchase","No actual nonmerging whole stack affordable by the original purse. No grant, starter sale or extra loot route.");yield return Capture("98-stock-unverified");yield return Tap(Key.Escape);yield return Settled();yield break;}
            yield return ExchangeBuy(merchant,item);yield return Tap(Key.Escape);yield return Settled();
            string stock=ExchangeStock(merchant),gear=ExchangeInventory(Player),memory=ExchangeMemory();int purse=TradeSystem.GetDrams(merchant),playerPurse=TradeSystem.GetDrams(Player),stamp=merchant.GetIntProperty(TraderRestockSystem.LastRestockProp);
            if(Tick-stamp+40>TraderRestockSystem.RestockIntervalTurns){Unverified("roadside-exchange-return","Normal restock would become due inside the conservative return bound; do not pretend commerce never refills.");yield break;}
            var returnedRows=new List<Diag.Entry>();
            yield return ExchangePaid(Tap(Key.LeftShift,Key.Comma),"local","real-source-exit");Require(WorldMap.IsWorldMapZoneID(Zone.ZoneID)&&At.X==mapCell.zoneX&&At.Y==mapCell.zoneY,"actual same worldmap cell on exit");returnedRows.AddRange(_exchangeLastWindow);
            yield return ExchangePaid(Tap(Key.LeftShift,Key.Period),"local","real-source-return");returnedRows.AddRange(_exchangeLastWindow);
            Check("return_preserves_same_source_and_consumed_stock",ReferenceEquals(Zone,sourceZone)&&CurrentExchangeMerchant(merchant)&&UniqueExchangeMerchant()
                &&ExchangeStock(merchant)==stock&&ExchangeInventory(Player)==gear&&ExchangeOwns(Player,item)&&!ExchangeOwns(merchant,item)
                &&TradeSystem.GetDrams(merchant)==purse&&TradeSystem.GetDrams(Player)==playerPurse&&ExchangeMemory()==memory
                &&merchant.GetIntProperty(TraderRestockSystem.LastRestockProp)==stamp&&Tick-stamp<=TraderRestockSystem.RestockIntervalTurns
                &&Manager.Exploration.DispositionFor(ExchangeZone)==2&&!Manager.Exploration.TryGetAcceptedEntry(Manager,Zone,out _)
                &&!returnedRows.Any(r=>r.Category=="worldgen"&&r.Kind=="RoadsideExchange")
                &&_input.TurnManager.GetSavedEntries().Count(e=>ReferenceEquals(e.Entity,merchant))==1);
            _observations.Add(new{phase="actual-source-return",item=item.ID,stock,merchantPurse=purse,playerPurse,stamp,tick=Tick,disposition=2,rows=returnedRows,boundary="Same cached source through real exit/return before ordinary restock. Merchant movement is allowed. This is not F5/F6 evidence."});
            yield return Capture("04-returned-original-owner-and-purchase");
        }
        bool CurrentExchangeMerchant(Entity merchant)=>merchant!=null&&merchant.BlueprintName=="Merchant"&&merchant.ID==ExchangeMerchantID
            &&merchant.SpatialZone==Zone&&Zone.GetEntityCell(merchant)?.Objects.Contains(merchant)==true&&merchant.GetPart<PhysicsPart>()?.ParentEntity==merchant
            &&merchant.GetPart<InventoryPart>()?.ParentEntity==merchant&&merchant.GetStatValue("Hitpoints")>0&&!CombatSystem.IsDeathHandled(merchant)
            &&!FactionManager.IsHostile(merchant,Player)&&!FactionManager.IsHostile(Player,merchant);
        bool UniqueExchangeMerchant()=>Manager.CachedZones.Values.SelectMany(z=>z.GetReadOnlyEntities()).Count(e=>e.ID==ExchangeMerchantID)==1;
        static bool ExchangeOwns(Entity actor,Entity item)=>actor?.GetPart<InventoryPart>()?.Objects.Contains(item)==true&&item?.GetPart<PhysicsPart>()?.InInventory==actor&&item.GetPart<PhysicsPart>().Equipped==null&&item.SpatialZone==null;
        static bool ExchangeCanTransferExact(Entity item,Entity destination)=>item!=null&&!destination.GetPart<InventoryPart>().Objects.Any(e=>e.GetPart<StackerPart>()?.CanStackWith(item)==true);
        static IEnumerable<string> ExchangeUnits(Entity actor)=>actor.GetPart<InventoryPart>().Objects.Concat(actor.GetPart<InventoryPart>().GetAllEquipped()).Distinct().Select(e=>e.ID+":"+e.BlueprintName+":"+(e.GetPart<StackerPart>()?.StackCount??1));
        static string ExchangeStock(Entity actor)=>string.Join("|",actor.GetPart<InventoryPart>().Objects.OrderBy(e=>e.ID,StringComparer.Ordinal).Select(e=>e.ID+":"+e.BlueprintName+":"+(e.GetPart<StackerPart>()?.StackCount??1)+":"+e.GetPart<PhysicsPart>()?.InInventory?.ID+":"+e.GetPart<PhysicsPart>()?.Equipped?.ID));
        static string ExchangeInventory(Entity actor)=>ExchangeStock(actor)+";gear="+string.Join("|",actor.GetPart<InventoryPart>().GetAllEquipped().Distinct().OrderBy(e=>e.ID,StringComparer.Ordinal).Select(e=>e.ID+":"+e.BlueprintName+":"+(e.GetPart<StackerPart>()?.StackCount??1)+":"+e.GetPart<PhysicsPart>()?.Equipped?.ID+":"+e.GetPart<PhysicsPart>()?.InInventory?.ID));
        string ExchangeMemory()=>string.Join("|",Player.Properties.Where(p=>p.Key.StartsWith("Traveller",StringComparison.Ordinal)).OrderBy(p=>p.Key).Select(p=>p.Key+"="+p.Value))
            +";"+string.Join("|",Player.IntProperties.Where(p=>p.Key.StartsWith("Traveller",StringComparison.Ordinal)).OrderBy(p=>p.Key).Select(p=>p.Key+"="+p.Value));
        Entity[] ExchangeThreats()=>Zone.GetReadOnlyEntities().Where(e=>e!=Player&&e.HasTag("Creature")&&!CombatSystem.IsDeathHandled(e)
            &&(e.GetStat("Hitpoints")==null||e.GetStatValue("Hitpoints")>0)&&(FactionManager.IsHostile(e,Player)||FactionManager.IsHostile(Player,e)||e.GetPart<BrainPart>()?.IsPersonallyHostileTo(Player)==true||e.GetPart<BrainPart>()?.Target==Player)).ToArray();
        bool ExchangeSafe(Cell cell,Entity[] threats)
        {
            if(cell==null||!Zone.CanPlaceFootprint(Player,cell.X,cell.Y))return false;
            foreach(var c in Zone.GetOccupiedCells(Player,cell.X,cell.Y))
            {
                if(c==null||c.Occupants.Any(e=>e!=Player&&(e.HasTag("Creature")||e.HasPart<TriggerOnStepPart>()||e.HasPart<LiquidPoolPart>()||e.HasPart<GasPoolPart>()||e.HasEffect<BurningEffect>()||e.GetPart<ThermalPart>()?.IsAflame==true)))return false;
                var s=Zone.TileState.Get(c.X,c.Y);if(s!=null&&(s.Heat>0||s.Cold>0||s.Charge>0||!string.IsNullOrEmpty(s.Cloud)||s.Coatings.Count>0)||threats.Any(e=>SpatialQuery.DistanceToCell(Zone,e,c.X,c.Y)<=3))return false;
            }
            return true;
        }
        List<(int x,int y)> ExchangePath(Entity merchant,Entity[] threats)
        {
            var start=(At.X,At.Y);var queue=new Queue<(int x,int y)>();queue.Enqueue(start);var seen=new HashSet<(int x,int y)>{start};var parents=new Dictionary<(int x,int y),(int x,int y)>();
            while(queue.Count>0)
            {
                var at=queue.Dequeue();if(SpatialQuery.DistanceToCell(Zone,merchant,at.x,at.y)==1)
                {var result=new List<(int x,int y)>();while(at!=start){result.Add(at);at=parents[at];}result.Reverse();return result;}
                foreach(var d in Directions){var next=(x:at.x+d.x,y:at.y+d.y);if(!Zone.InBounds(next.x,next.y)||!seen.Add(next)||!ExchangeSafe(Zone.GetCell(next.x,next.y),threats))continue;parents[next]=at;queue.Enqueue(next);}
            }
            return null;
        }
        static Key ExchangeDirection(int dx,int dy)
        {
            dx=Math.Sign(dx);dy=Math.Sign(dy);
            if(dx==1&&dy==1)return Key.Numpad3;if(dx==1&&dy==-1)return Key.Numpad9;
            if(dx==-1&&dy==1)return Key.Numpad1;if(dx==-1&&dy==-1)return Key.Numpad7;
            Require(dx!=0||dy!=0,"current adjacent direction");return Direction(dx,dy);
        }
        IEnumerator ExchangeOpenTrade(Entity merchant)
        {
            Require(CurrentExchangeMerchant(merchant)&&SpatialQuery.Distance(Zone,Player,merchant)==1,"current adjacent actual merchant");var target=SpatialQuery.ClosestCell(Zone,merchant,At.X,At.Y);
            int tick=Tick,energy=Energy;string before=ExchangeInventory(Player)+ExchangeInventory(merchant);
            yield return Tap(Key.C);Require(State=="AwaitingTalkDirection","actual C direction prompt");yield return Tap(ExchangeDirection(target.X-At.X,target.Y-At.Y));Require(State=="WorldActionMenuOpen","actual current world menu");
            yield return SelectCurrentOwner(merchant);yield return MenuAction("Chat");Require(ConversationManager.IsActive&&ReferenceEquals(ConversationManager.Speaker,merchant),"actual merchant conversation");
            if((bool)Field(_input.DialogueUI,"_revealing"))yield return Tap(Key.Enter);
            string Village(string id){var p=WorldMap.FromZoneID(id);return p.z==0&&WorldMapAuthoring.InBounds(p.x,p.y)?Manager.WorldMap.GetPOI(p.x,p.y)?.Name:null;}
            string origin=Village(merchant.GetProperty(WorldTravellers.OriginProperty)),destination=Village(merchant.GetProperty(WorldTravellers.DestinationProperty));
            Check("actual_current_route_dialogue",!string.IsNullOrEmpty(origin)&&!string.IsNullOrEmpty(destination)&&origin!=destination&&ConversationManager.CurrentText=="I'm on the road between "+origin+" and "+destination+". I can trade while I rest.");
            yield return Capture("02-real-route-dialogue");var choices=ConversationManager.VisibleChoices.ToArray();int choice=Array.FindIndex(choices,c=>c.Actions?.Any(a=>a.Key=="StartTrade")==true);
            Require(choice>=0&&choices.Count(c=>c.Actions?.Any(a=>a.Key=="StartTrade")==true)==1,"one actual offered StartTrade choice");
            yield return Tap((Key)Enum.Parse(typeof(Key),MenuShortcutMap.Key(MenuShortcutMap.Positional(choice)).ToString()));
            Require(_input.TradeUI.IsOpen&&Tick==tick&&Energy==energy&&ExchangeInventory(Player)+ExchangeInventory(merchant)==before,"real free unchanged trade UI opening");
        }
        IEnumerator ExchangeBuy(Entity merchant,Entity item)
        {
            Require(_input.TradeUI.IsOpen&&CurrentExchangeMerchant(merchant)&&ExchangeOwns(merchant,item)&&ExchangeCanTransferExact(item,Player),"current whole-stack trade source");
            if((int)Field(_input.TradeUI,"_panel")!=0)yield return Tap(Key.Tab);Require((int)Field(_input.TradeUI,"_panel")==0,"actual merchant stock panel");
            var rows=(IList)Field(_input.TradeUI,"_leftRows");int row=-1;for(int i=0;i<rows.Count;i++)if(ReferenceEquals(Field(rows[i],"Item"),item))row=i;Require(row>=0,"exact real source row");
            for(int n=0;(int)Field(_input.TradeUI,"_leftCursor")!=row;n++){Require(n<80,"bounded native stock cursor");yield return Tap((int)Field(_input.TradeUI,"_leftCursor")<row?Key.DownArrow:Key.UpArrow);}
            int price=TradeSystem.GetBuyPrice(item,TradeSystem.GetTradePerformance(Player),merchant),playerPurse=TradeSystem.GetDrams(Player),merchantPurse=TradeSystem.GetDrams(merchant),tick=Tick,energy=Energy;
            Require(price>0&&price<=playerPurse,"actual original purse covers quoted whole stack");string[] units=ExchangeUnits(Player).Concat(ExchangeUnits(merchant)).OrderBy(s=>s,StringComparer.Ordinal).ToArray();
            yield return Capture("03-native-actual-stock-purchase");string marker=ExchangeMarker("actual-source-purchase");yield return Tap(Key.Enter);Require((bool)Field(_input.TradeUI,"_confirmActive"),"actual purchase confirmation");yield return Tap(Key.Enter);
            var window=ExchangeWindow(marker);Check("native_purchase_exact_identity_units_purses",ExchangeOwns(Player,item)&&!ExchangeOwns(merchant,item)
                &&units.SequenceEqual(ExchangeUnits(Player).Concat(ExchangeUnits(merchant)).OrderBy(s=>s,StringComparer.Ordinal))&&Tick==tick&&Energy==energy
                &&DensityCampaignNativeEvidence.TradeMatches(window,marker,Player.ID,merchant.ID,item.ID,true,price,playerPurse,TradeSystem.GetDrams(Player),merchantPurse,TradeSystem.GetDrams(merchant)));
            _observations.Add(new{phase="real-purchase",item=item.ID,blueprint=item.BlueprintName,units=item.GetPart<StackerPart>()?.StackCount??1,price,beforePlayerPurse=playerPurse,afterPlayerPurse=TradeSystem.GetDrams(Player),beforeMerchantPurse=merchantPurse,afterMerchantPurse=TradeSystem.GetDrams(merchant),rows=window});WriteReport();
        }
    }
}
