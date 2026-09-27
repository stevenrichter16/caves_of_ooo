using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Keyboard-only traveller trade and contextual remark acceptance.
    /// Labelled approach transfers preserve the actual generated graph; encounter
    /// entry, commerce, revisits, saves, loads and waiting use native input.</summary>
    public sealed class DensityTravellerNativePlayer : MonoBehaviour
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags Static = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
        private static readonly (int x,int y)[] Steps = {(1,0),(-1,0),(0,1),(0,-1),(1,1),(1,-1),(-1,1),(-1,-1)};
        private static readonly string[] RequiredChecks = {"ordinary_start","cached_reads_do_not_roll","actual_edge_created_merchant","entry_registered_scheduler",
            "actual_route_dialogue","native_purchase_conserves","edge_revisit_preserves_consumed_stock","checkpoint_saved","checkpoint_stock_mutation_real",
            "checkpoint_replaces_exact_stock_graph","actual_contextual_remark","shared_nineteen_action_silence","ordinary_finish"};
        public string RunId { get; }=Guid.NewGuid().ToString("N");
        public bool Finished { get; private set; }
        public int Failures=>_failures+_unexpectedErrors;
        public string ReportPath { get; private set; }
        private InputHandler _input;
        private Keyboard _keyboard,_oldKeyboard;
        private InputSettings _settings,_oldSettings;
        private bool _oldBackground,_started,_cleaned,_errorsFinalized,_summaryEmitted;
        private readonly Dictionary<string,bool> _oldChannels=new Dictionary<string,bool>();
        private readonly List<string> _audit=new List<string>(),_screenshots=new List<string>(),_notes=new List<string>();
        private readonly List<object> _observations=new List<object>(),_keys=new List<object>();
        private readonly List<Emission> _emissions=new List<Emission>();
        private readonly HashSet<string> _observedTraces=new HashSet<string>();
        private System.Diagnostics.Stopwatch _clock;
        private int _failures,_unexpectedErrors;
        private string _fatal,_ownedRoot,_merchantId,_merchantZone,_receipt,_boughtId,_checkpointHash;
        private Route _route;
        private string DirectoryPath=>Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/DensityCompletion/Ambience/NativeAcceptance",RunId));
        private Entity Player=>_input.PlayerEntity;
        private Zone Zone=>_input.CurrentZone;
        private OverworldZoneManager Manager=>_input.ZoneManager as OverworldZoneManager;
        private Cell At=>Zone.GetEntityCell(Player);
        private Entity Merchant=>Manager.CachedZones[_merchantZone].GetReadOnlyEntities().Single(e=>e.ID==_merchantId);
        private int Tick=>_input.TurnManager.TickCount;
        private int Energy=>_input.TurnManager.GetEnergy(Player);
        private int Ambient=>Player.GetIntProperty(WorldAmbience.TurnProperty);
        private string State=>Field(_input,"_inputState").ToString();
        private sealed class Route { public Zone Source,Origin; public int dx,dy,ox,oy,sx,sy; }
        private sealed class Emission
        { public string trace,kind,actor,blueprint,zone,text,context; public int ambient,tick,serial; public bool live,eligible,contextMatches; }

        public void Initialize(ScenarioContext context)
        {
            Require(!string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride),"isolated launcher owns save root");
            _ownedRoot=SaveGameService.SaveRootOverride;_clock=System.Diagnostics.Stopwatch.StartNew();
            foreach(string channel in new[]{"scenario","event","worldgen"}){_oldChannels[channel]=Diag.IsChannelEnabled(channel);Diag.SetChannel(channel,true);}
            _oldSettings=InputSystem.settings;_settings=Instantiate(_oldSettings);_settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            _settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings=_settings;_oldBackground=Application.runInBackground;Application.runInBackground=true;
            _oldKeyboard=Keyboard.current;_keyboard=InputSystem.AddDevice<Keyboard>();
            MessageLog.OnMessage+=OnMessage;
            StartCoroutine(RunSafely(RunAudit()));
        }
        private IEnumerator RunAudit()
        {
            yield return new WaitForSecondsRealtime(.8f);
            _input=FindFirstObjectByType<InputHandler>();Require(_input!=null,"ordinary bootstrap input");
            var boot=(BootMenuController)Field(_input,"_bootMenuController");Require(boot!=null&&boot.IsActive,"owned new-game menu");
            yield return Tap(Key.N);yield return Settled();_started=true;
            Require(!boot.IsActive&&Manager.WorldSeed==64,"actual native new game, seed64");RequireOrdinary();
            Check("ordinary_start",Player.GetStatValue("Hitpoints")==40&&TradeSystem.GetDrams(Player)>=0);
            Observe("ordinary-start");yield return Capture("01-ordinary-start");
            var candidates=Routes();Require(candidates.Count>0,"bounded real entry candidates with paired safe edges");
            string reads=TravellerMemory(Player);
            foreach(var candidate in candidates){Manager.GetZone(candidate.Source.ZoneID);Manager.GetZone(candidate.Origin.ZoneID);}
            Check("cached_reads_do_not_roll",TravellerMemory(Player)==reads&&candidates.All(r=>!r.Source.GetReadOnlyEntities().Any(e=>e.GetProperty("TravellerReceipt")!=null)));
            int originalCount=Player.GetIntProperty("TravellerCount:64");
            foreach(var route in candidates.Take(6))
            {
                Require(Player.GetIntProperty("TravellerCount:64")==originalCount,"no unexpected encounter before selected entry");
                Travel(route.Origin,route.Origin.GetCell(route.ox,route.oy),"POI edge approach; next step is the real wilderness entry");yield return Settled();
                Require(Safe(route.Source,route.Source.GetCell(route.sx,route.sy),3),"current physical arrival footprint remains safe");
                yield return PaidStep(route.dx,route.dy,route.Source,route.sx,route.sy);
                string id="traveller:64:"+route.Source.ZoneID;
                var actor=route.Source.GetReadOnlyEntities().SingleOrDefault(e=>e.ID==id);
                _notes.Add("ACTUAL ENTRY "+route.Source.ZoneID+" roll="+Player.GetIntProperty("TravellerRoll:64:"+route.Source.ZoneID,-1)+" actor="+(actor?.ID??"none"));WriteReport();
                if(actor==null)continue;
                _route=route;_merchantId=id;_merchantZone=route.Source.ZoneID;_receipt="TravellerSpawn:64:"+_merchantZone;break;
            }
            Require(_route!=null,"at most six real entries produce an actual merchant; rolls remain consumed on failure");
            var merchant=Merchant;
            Check("actual_edge_created_merchant",merchant.BlueprintName=="Merchant"&&merchant.GetPart<InventoryPart>()?.Objects.Count>0
                &&Player.GetProperty(_receipt)==merchant.ID&&merchant.GetProperty("TravellerReceipt")==_receipt
                &&Player.GetIntProperty("TravellerCount:64")==originalCount+1&&UniqueMerchant());
            Check("entry_registered_scheduler",merchant.GetPart<BrainPart>()?.CurrentZone==Zone
                &&_input.TurnManager.GetSavedEntries().Count(e=>ReferenceEquals(e.Entity,merchant))==1);
            Observe("entry-created-merchant");yield return Capture("02-real-entry-owner");
            yield return Approach(merchant,24);yield return OpenTrade(merchant,true);
            Entity item=Affordable(merchant);
            if(item==null)
            {
                var sale=Player.GetPart<InventoryPart>().Objects.Where(e=>CanTransferExact(e,merchant)&&e.HasPart<CommercePart>())
                    .OrderByDescending(e=>TradeSystem.GetSellPrice(e,TradeSystem.GetTradePerformance(Player),merchant))
                    .FirstOrDefault(e=>TradeSystem.GetSellPrice(e,TradeSystem.GetTradePerformance(Player),merchant)<=TradeSystem.GetDrams(merchant));
                Require(sale!=null,"ordinary affordable stock or one real eligible starter sale");
                yield return Transaction(merchant,sale,false,"actual-starter-sale");item=Affordable(merchant);
            }
            Require(item!=null,"actual available nonmerging source item or whole stack within ordinary purse");_boughtId=item.ID;
            yield return Transaction(merchant,item,true,"actual-purchase");
            Check("native_purchase_conserves",Owns(Player,item)&&!Owns(merchant,item));
            yield return Tap(Key.Escape);yield return Settled();
            string consumed=Stock(merchant),playerGear=Gear(Player),memory=TravellerMemory(Player);int purse=TradeSystem.GetDrams(merchant);
            int last=merchant.GetIntProperty(TraderRestockSystem.LastRestockProp);Entity originalMerchant=merchant;
            yield return WalkTo(Zone.GetCell(_route.sx,_route.sy),24);
            Require(Tick-last+20<=TraderRestockSystem.RestockIntervalTurns,"edge revisit finishes before normal restock becomes due");
            yield return PaidStep(-_route.dx,-_route.dy,_route.Origin,_route.ox,_route.oy);
            Require(Safe(_route.Source,_route.Source.GetCell(_route.sx,_route.sy),3),"current return footprint safe");
            yield return PaidStep(_route.dx,_route.dy,_route.Source,_route.sx,_route.sy);
            Check("edge_revisit_preserves_consumed_stock",ReferenceEquals(Merchant,originalMerchant)&&Stock(Merchant)==consumed
                &&TradeSystem.GetDrams(Merchant)==purse&&Gear(Player)==playerGear&&TravellerMemory(Player)==memory&&UniqueMerchant()
                &&Merchant.GetIntProperty(TraderRestockSystem.LastRestockProp)==last&&Tick-last<=TraderRestockSystem.RestockIntervalTurns);
            Observe("same-edge-revisit");
            yield return Approach(Merchant,24);yield return Checkpoint();
            yield return ObserveRemark();
            RequireOrdinary();Check("ordinary_finish",State=="Normal"&&Player.GetStatValue("Hitpoints")>10&&UniqueMerchant());
            Observe("finished");yield return Capture("08-ordinary-finish");
        }
        private List<Route> Routes()
        {
            var routes=new List<Route>();int generated=0;var accepted=new HashSet<string>();
            for(int y=0;y<WorldMap.Height&&generated<24;y++)for(int x=0;x<WorldMap.Width&&generated<24;x++)
            {
                string id=WorldMap.ToZoneID(x,y,0);
                if(Manager.WorldMap.GetPOI(x,y)!=null||WorldMapAuthoring.TierAt(x,y)>3||Hash("64:traveller:"+id)%8!=0
                    ||Player.IntProperties.ContainsKey("TravellerRoll:64:"+id))continue;
                foreach(var d in Steps.Take(4))
                {
                    if(accepted.Contains(id)||generated>=24)break;
                    int ox=x-d.x,oy=y-d.y;
                    if(!WorldMapAuthoring.InBounds(ox,oy)||Manager.WorldMap.GetPOI(ox,oy)==null)continue;
                    generated++;var source=Manager.GetZone(id);var origin=Manager.GetZone(WorldMap.ToZoneID(ox,oy,0));
                    _notes.Add("SOURCE SCAN "+id+" origin="+origin?.ZoneID+" actualPOI="+Manager.WorldMap.GetPOI(ox,oy)?.Name+" tier="+WorldMapAuthoring.TierAt(x,y));
                    if(source==null||origin==null)continue;
                    var threatsA=Threats(origin);var threatsB=Threats(source);
                    int length=d.x!=0?Zone.Height:Zone.Width;
                    for(int offset=2;offset<length-2;offset++)
                    {
                        int ax=d.x>0?Zone.Width-1:d.x<0?0:offset,ay=d.y>0?Zone.Height-1:d.y<0?0:offset;
                        int bx=d.x>0?0:d.x<0?Zone.Width-1:offset,by=d.y>0?0:d.y<0?Zone.Height-1:offset;
                        if(!Safe(origin,origin.GetCell(ax,ay),12,threatsA)||!Safe(source,source.GetCell(bx,by),12,threatsB))continue;
                        if(source.GetOccupiedCells(Player,bx,by).Any(c=>c.HasObjectWithTag("ExcludeZoneArrival"))||origin.GetOccupiedCells(Player,ax,ay).Any(c=>c.HasObjectWithTag("ExcludeZoneArrival")))continue;
                        routes.Add(new Route{Source=source,Origin=origin,dx=d.x,dy=d.y,ox=ax,oy=ay,sx=bx,sy=by});accepted.Add(id);break;
                    }
                    if(routes.Count>=6){WriteReport();return routes;}
                }
            }
            WriteReport();return routes;
        }
        private IEnumerator Checkpoint()
        {
            var merchant=Merchant;var info=SaveGameService.GetSaveInfo("Quick");Require(info!=null,"existing real checkpoint metadata");
            string file=Path.Combine(_ownedRoot,info.GameID,"Quick.sav.gz"),before=HashFile(file);
            var savedPlayer=Player;var savedMerchant=merchant;var at=At;var mt=Zone.GetEntityCell(merchant);
            string savedGear=Gear(Player),stock=Stock(merchant),memory=TravellerMemory(Player),stats=Stats(Player),zone=Zone.ZoneID;
            int px=at.X,py=at.Y,mx=mt.X,my=mt.Y,tick=Tick,energy=Energy,world=WorldClock.CurrentTick,pc=TradeSystem.GetDrams(Player),mc=TradeSystem.GetDrams(merchant),stamp=merchant.GetIntProperty(TraderRestockSystem.LastRestockProp);
            long serial=MessageLog.NextSerialValue;yield return Tap(Key.F5);yield return Settled();_checkpointHash=HashFile(file);
            Check("checkpoint_saved",_checkpointHash!=before&&MessageLog.NextSerialValue>serial&&MessageLog.GetLast()=="Game saved."
                &&SaveGameService.GetSaveInfo("Quick")?.GameID==info.GameID&&SaveGameService.GetSaveInfo("Quick")?.ActiveZoneID==zone);
            // Sell the exact purchased source instance. No replacement item or
            // purse grant is needed, and this is a real stock/purse mutation.
            var bought=Player.GetPart<InventoryPart>().Objects.Single(e=>e.ID==_boughtId);
            yield return OpenTrade(merchant,false);yield return Transaction(merchant,bought,false,"checkpoint-real-sale");yield return Tap(Key.Escape);yield return Settled();
            Check("checkpoint_stock_mutation_real",Stock(merchant)!=stock&&Gear(Player)!=savedGear&&Owns(merchant,bought)&&!Owns(Player,bought)
                &&TradeSystem.GetDrams(Player)!=pc&&HashFile(file)==_checkpointHash);
            yield return Tap(Key.F6);double began=Time.realtimeSinceStartupAsDouble;
            while(ReferenceEquals(Player,savedPlayer)){Require(Time.realtimeSinceStartupAsDouble-began<8,"F6 replaces actual player graph");yield return null;}
            yield return Settled();merchant=Merchant;mt=Zone.GetEntityCell(merchant);
            Check("checkpoint_replaces_exact_stock_graph",!ReferenceEquals(merchant,savedMerchant)&&Player.ID==savedPlayer.ID&&merchant.ID==savedMerchant.ID
                &&Zone.ZoneID==zone&&At.X==px&&At.Y==py&&mt!=null&&mt.X==mx&&mt.Y==my
                &&Gear(Player)==savedGear&&Stock(merchant)==stock&&TravellerMemory(Player)==memory&&Stats(Player)==stats
                &&TradeSystem.GetDrams(Player)==pc&&TradeSystem.GetDrams(merchant)==mc&&Tick==tick&&Energy==energy&&WorldClock.CurrentTick==world
                &&merchant.GetIntProperty(TraderRestockSystem.LastRestockProp)==stamp&&HashFile(file)==_checkpointHash&&UniqueMerchant());
            Observe("checkpoint-restored");yield return Capture("05-real-checkpoint-restore");
        }
        private IEnumerator OpenTrade(Entity seller,bool proveDialogue)
        {
            yield return WorldAction(seller,"Chat");Require(ConversationManager.IsActive&&ReferenceEquals(ConversationManager.Speaker,seller),"exact real merchant conversation");
            if((bool)Field(_input.DialogueUI,"_revealing"))yield return Tap(Key.Enter);
            string text=ConversationManager.CurrentText;
            if(proveDialogue)
            {
                string a=VillageName(seller.GetProperty(WorldTravellers.OriginProperty)),b=VillageName(seller.GetProperty(WorldTravellers.DestinationProperty));
                Check("actual_route_dialogue",a!=null&&b!=null&&a!=b&&text=="I'm on the road between "+a+" and "+b+". I can trade while I rest.");
                _notes.Add("ACTUAL DIALOGUE "+seller.ID+" "+text);yield return Capture("03-real-route-dialogue");
            }
            int choice=ConversationManager.VisibleChoices.ToList().FindIndex(c=>c.Actions?.Any(a=>a.Key=="StartTrade")==true);
            Require(choice>=0,"real StartTrade choice");yield return Tap(Shortcut(MenuShortcutMap.Key(MenuShortcutMap.Positional(choice)).ToString()));
            Require(_input.TradeUI.IsOpen,"native trade UI");
        }
        private Entity Affordable(Entity seller)=>TradeSystem.GetTraderStock(seller).Where(e=>CanTransferExact(e,Player))
            .Where(e=>TradeSystem.GetBuyPrice(e,TradeSystem.GetTradePerformance(Player),seller)>0&&TradeSystem.GetBuyPrice(e,TradeSystem.GetTradePerformance(Player),seller)<=TradeSystem.GetDrams(Player))
            .OrderBy(e=>TradeSystem.GetBuyPrice(e,TradeSystem.GetTradePerformance(Player),seller)).ThenBy(e=>e.ID,StringComparer.Ordinal).FirstOrDefault();
        private static bool CanTransferExact(Entity item,Entity destination)=>item!=null&&destination?.GetPart<InventoryPart>()!=null
            && !destination.GetPart<InventoryPart>().Objects.Any(e=>e.GetPart<StackerPart>()?.CanStackWith(item)==true);
        private IEnumerator Transaction(Entity seller,Entity item,bool buy,string label)
        {
            Require(_input.TradeUI.IsOpen&&CanTransferExact(item,buy?Player:seller),"real trade panel and exact nonmerging source item/whole stack");
            int panel=buy?0:1;while((int)Field(_input.TradeUI,"_panel")!=panel)yield return Tap(Key.Tab);
            var rows=(IList)Field(_input.TradeUI,buy?"_leftRows":"_rightRows");int row=-1;
            for(int i=0;i<rows.Count;i++)if(ReferenceEquals(Field(rows[i],"Item"),item))row=i;
            Require(row>=0,"exact real item offered: "+item.ID);
            string cursor=buy?"_leftCursor":"_rightCursor";
            for(int steps=0;(int)Field(_input.TradeUI,cursor)!=row;steps++){Require(steps<80,"finite native trade navigation");yield return Tap((int)Field(_input.TradeUI,cursor)<row?Key.DownArrow:Key.UpArrow);}
            int price=buy?TradeSystem.GetBuyPrice(item,TradeSystem.GetTradePerformance(Player),seller):TradeSystem.GetSellPrice(item,TradeSystem.GetTradePerformance(Player),seller);
            int playerCoins=TradeSystem.GetDrams(Player),sellerCoins=TradeSystem.GetDrams(seller),tick=Tick,energy=Energy;
            Require(price>0&&(buy?playerCoins:sellerCoins)>=price,"real payer can afford current exact price");
            var source=buy?seller:Player;var destination=buy?Player:seller;Require(Owns(source,item)&&!Owns(destination,item),"exact pre-transfer ownership");
            string[] combined=OwnedIds(Player).Concat(OwnedIds(seller)).OrderBy(s=>s,StringComparer.Ordinal).ToArray();
            yield return Capture(label);yield return Tap(Key.Enter);Require((bool)Field(_input.TradeUI,"_confirmActive"),"native trade confirmation");yield return Tap(Key.Enter);
            Require(!_input.TradeUI.IsOpen||(bool)Field(_input.TradeUI,"_confirmActive")==false,"confirmation consumed");
            Require(Owns(destination,item)&&!Owns(source,item)&&item.GetPart<PhysicsPart>()?.InInventory==destination
                &&TradeSystem.GetDrams(Player)==playerCoins+(buy?-price:price)&&TradeSystem.GetDrams(seller)==sellerCoins+(buy?price:-price)
                &&combined.SequenceEqual(OwnedIds(Player).Concat(OwnedIds(seller)).OrderBy(s=>s,StringComparer.Ordinal))&&Tick==tick&&Energy==energy,
                "real native trade exact identity/quantity/purse conservation and free UI semantics");
            _notes.Add("TRANSACTION "+label+" item="+item.ID+" price="+price+" buy="+buy+" player="+playerCoins+"→"+TradeSystem.GetDrams(Player)+" seller="+sellerCoins+"→"+TradeSystem.GetDrams(seller));Observe(label);
        }
        private IEnumerator ObserveRemark()
        {
            Entity speaker=null;Zone selected=null;Cell approach=null;int examined=0;
            for(int y=0;y<WorldMap.Height&&speaker==null;y++)for(int x=0;x<WorldMap.Width&&speaker==null;x++)
            {
                if(Manager.WorldMap.GetPOI(x,y)?.Type!=POIType.Village||WorldMapAuthoring.TierAt(x,y)>2)continue;
                if(++examined>16)break;var zone=Manager.GetZone(WorldMap.ToZoneID(x,y,0));if(zone==null)continue;
                var threats=Threats(zone);var actors=zone.GetReadOnlyEntities().Where(e=>Context(e,zone)!=null)
                    .OrderBy(e=>e.BlueprintName=="Farmer"?0:e.BlueprintName=="Innkeeper"?1:2).ThenBy(e=>e.ID,StringComparer.Ordinal).ToArray();
                _notes.Add("CONTEXT SCAN "+zone.ZoneID+" hostileOwners="+threats.Length+" actualMatching="+string.Join(",",actors.Select(e=>e.BlueprintName+":"+e.ID)));
                foreach(var candidate in actors)
                {
                    if(candidate.GetStatValue("Hitpoints")<=0||CombatSystem.IsDeathHandled(candidate)||candidate.GetPart<BrainPart>()?.Target!=null
                        ||FactionManager.IsHostile(candidate,Player)||FactionManager.IsHostile(Player,candidate))continue;
                    var at=zone.GetEntityCell(candidate);
                    for(int dy=-2;dy<=2&&speaker==null;dy++)for(int dx=-2;dx<=2&&speaker==null;dx++)
                    {
                        if(Math.Max(Math.Abs(dx),Math.Abs(dy))!=2)continue;var c=zone.GetCell(at.X+dx,at.Y+dy);
                        if(!Safe(zone,c,12,threats)||!AIHelpers.HasLineOfSight(zone,c.X,c.Y,at.X,at.Y))continue;
                        speaker=candidate;selected=zone;approach=c;
                    }
                    if(speaker!=null)break;
                }
            }
            Require(speaker!=null&&SariAmbience.TierFor(selected)<=2,"bounded actual quiet village with a generated contextual resident");
            Travel(selected,approach,"actual contextual resident approach; no speaker/context/time mutation");yield return Settled();
            yield return Approach(speaker,12);Require(Eligible(speaker,Zone)&&Context(speaker,Zone)!=null,"actual present visible speaker has live context");
            _notes.Add("CONTEXT BEFORE WAITS "+speaker.ID+" "+speaker.BlueprintName+" "+Context(speaker,Zone));Observe("context-ready");
            int before=_emissions.Count;
            for(int waits=0;waits<120&&!_emissions.Skip(before).Any(e=>e.kind=="WorldRemark");waits++)
            {
                Require(Safe(Zone,At,3),"current wait body/hazards/nearby threats permit ordinary wait");
                _notes.Add("NATIVE WAIT "+waits+" nextAmbient="+(Ambient+1)+" isolatedHash="+(Hash(Zone.ZoneID+":remark:"+(Ambient+1))%100));
                yield return PaidWait();
            }
            var emission=_emissions.Skip(before).FirstOrDefault(e=>e.kind=="WorldRemark");
            Require(emission!=null,"actual contextual line within120 native paid waits");
            Check("actual_contextual_remark",emission.live&&emission.eligible&&emission.contextMatches
                &&new[]{"Scribe","Innkeeper","Farmer","Warden"}.Contains(emission.blueprint)&&emission.zone==Zone.ZoneID
                &&emission.ambient==Player.GetIntProperty(WorldAmbience.LastMessageProperty)&&Player.GetIntProperty(WorldAmbience.HasMessageProperty)==1);
            yield return Capture("06-actual-contextual-remark");
            int mark=emission.ambient,count=_emissions.Count;
            for(int waits=0;waits<19;waits++)
            {Require(Safe(Zone,At,3),"quiet current footprint during spacing observation");yield return PaidWait();}
            Check("shared_nineteen_action_silence",Ambient==mark+19&&_emissions.Count==count
                &&Player.GetIntProperty(WorldAmbience.LastMessageProperty)==mark&&SariAmbience.TierFor(Zone)<=2);
            Observe("shared-budget-nineteen-waits");yield return Capture("07-shared-budget-spacing");
        }
        private void OnMessage(string text)
        {
            if(!_started||_input?.PlayerEntity==null)return;
            // Both emitters record immediately before their own message. Read
            // that immutable diagnostic and the actual graph synchronously.
            var row=Diag.Snapshot(1).FirstOrDefault();
            if((row.Kind!="WorldRemark"&&row.Kind!="SariHeard")||_observedTraces.Contains(row.TraceId))return;
            var actor=Zone.GetReadOnlyEntities().SingleOrDefault(e=>e.ID==row.ActorId);
            string context=actor==null?null:Context(actor,Zone);
            bool matches=row.Kind=="SariHeard"?new[]{SariAmbience.SoftLine,SariAmbience.ClearLine,SariAmbience.LoudLine,SariAmbience.PracticeLine}.Contains(text)
                :actor!=null&&context!=null&&text==PlayerMessageGrammar.Normalize(actor.GetDisplayName()+" says, \""+context+"\"");
            if(!matches)return;
            _observedTraces.Add(row.TraceId);
            _emissions.Add(new Emission{trace=row.TraceId,kind=row.Kind,actor=row.ActorId,blueprint=actor?.BlueprintName,zone=Zone.ZoneID,text=text,
                context=context,ambient=Ambient,tick=Tick,serial=MessageLog.GetRecentEntries(1).Single().Serial,
                live=actor!=null&&Zone.GetEntityCell(actor)?.Objects.Contains(actor)==true,
                eligible=row.Kind=="WorldRemark"&&row.TargetId==Player.ID&&Eligible(actor,Zone),contextMatches=matches});
        }
        private static string Context(Entity entity,Zone zone)
        {
            if(entity==null||!new[]{"Scribe","Innkeeper","Farmer","Warden"}.Contains(entity.BlueprintName))return null;
            return (string)typeof(WorldRemarks).GetMethod("Context",Static).Invoke(null,new object[]{entity,zone});
        }
        private bool Eligible(Entity entity,Zone zone)=>(bool)typeof(WorldRemarks).GetMethod("Eligible",Static).Invoke(null,new object[]{entity,Player,zone});
        private static uint Hash(string text)=>(uint)typeof(WorldRemarks).GetMethod("Hash",Static).Invoke(null,new object[]{text});
        private IEnumerator WorldAction(Entity target,string command)
        {
            Require(Zone.GetEntityCell(target)!=null&&SpatialQuery.Distance(Zone,Player,target)==1,"actual adjacent current world owner");
            var at=At;var to=SpatialQuery.ClosestCell(Zone,target,at.X,at.Y);
            yield return Tap(Key.C);Require(State=="AwaitingTalkDirection","native C direction prompt");yield return Tap(Direction(to.X-at.X,to.Y-at.Y));
            Require(State=="WorldActionMenuOpen","native world menu");
            if(_input.WorldActionMenuUI.SelectedCellIsPile)yield return MenuAction(WorldInteractionSystem.PickCellCommand);
            if(!ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget,target)||((List<InventoryAction>)Field(_input.WorldActionMenuUI,"_actions")).Any(a=>a.Command==WorldInteractionSystem.PickTargetCommandPrefix+target.ID))
                yield return MenuAction(WorldInteractionSystem.PickTargetCommandPrefix+target.ID);
            Require(ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget,target),"exact selected owner");yield return MenuAction(command);
        }
        private IEnumerator MenuAction(string command)
        {
            var actions=(List<InventoryAction>)Field(_input.WorldActionMenuUI,"_actions");int index=actions.FindIndex(a=>a.Command==command);
            Require(index>=0,"offered native command "+command);yield return Tap(Shortcut(MenuShortcutMap.Key(MenuShortcutMap.ForActions(actions)[index]).ToString()));
        }
        private IEnumerator Approach(Entity target,int budget)
        {
            for(int n=0;n<budget;n++)
            {
                Require(Zone.GetEntityCell(target)!=null,"current approach owner remains in zone");
                if(SpatialQuery.Distance(Zone,Player,target)==1)yield break;
                var path=PathTo(c=>SpatialQuery.DistanceToCell(Zone,target,c.X,c.Y)==1);
                Require(path!=null&&path.Count>0,"finite currently safe route to actual owner "+target.ID);
                yield return PaidStep(path[0].x-At.X,path[0].y-At.Y,Zone,path[0].x,path[0].y);
            }
            throw new InvalidOperationException("Finite native owner approach exceeded "+budget+" keys.");
        }
        private IEnumerator WalkTo(Cell target,int budget)
        {
            for(int n=0;n<budget;n++)
            {
                if(At.X==target.X&&At.Y==target.Y)yield break;
                var path=PathTo(c=>c.X==target.X&&c.Y==target.Y);
                Require(path!=null&&path.Count>0,"finite current safe return route to "+target.X+","+target.Y);
                yield return PaidStep(path[0].x-At.X,path[0].y-At.Y,Zone,path[0].x,path[0].y);
            }
            throw new InvalidOperationException("Finite native return exceeded "+budget+" keys.");
        }
        private List<(int x,int y)> PathTo(Func<Cell,bool> goal)
        {
            var zone=Zone;var start=At;var threats=Threats(zone);var seen=new bool[Zone.Width,Zone.Height];var safe=new sbyte[Zone.Width,Zone.Height];
            var parent=new (int x,int y)[Zone.Width,Zone.Height];var queue=new Queue<(int x,int y)>();queue.Enqueue((start.X,start.Y));seen[start.X,start.Y]=true;
            bool Allowed(int x,int y){if(!zone.InBounds(x,y))return false;if(safe[x,y]==0)safe[x,y]=(sbyte)(Safe(zone,zone.GetCell(x,y),3,threats)?1:-1);return safe[x,y]>0;}
            while(queue.Count>0)
            {
                var at=queue.Dequeue();
                if(goal(zone.GetCell(at.x,at.y)))
                {var result=new List<(int x,int y)>();while(at.x!=start.X||at.y!=start.Y){result.Add(at);at=parent[at.x,at.y];}result.Reverse();return result;}
                foreach(var d in Steps)
                {
                    int x=at.x+d.x,y=at.y+d.y;if(!zone.InBounds(x,y)||seen[x,y]||!Allowed(x,y))continue;
                    // Same native diagonal corner rule: at least one orthogonal
                    // route must physically admit the same body.
                    if(d.x!=0&&d.y!=0&&!zone.CanPlaceFootprint(Player,at.x+d.x,at.y)&&!zone.CanPlaceFootprint(Player,at.x,at.y+d.y))continue;
                    seen[x,y]=true;parent[x,y]=at;queue.Enqueue((x,y));
                }
            }
            _notes.Add("NO SAFE ROUTE "+zone.ZoneID+"@"+start.X+","+start.Y+" threats="+string.Join(";",threats.Select(e=>e.ID+"@"+zone.GetEntityCell(e).X+","+zone.GetEntityCell(e).Y)));WriteReport();return null;
        }
        private Entity[] Threats(Zone zone)=>zone.GetReadOnlyEntities().Where(e=>e!=Player&&e.HasTag("Creature")&&!CombatSystem.IsDeathHandled(e)
            &&(e.GetStat("Hitpoints")==null||e.GetStatValue("Hitpoints")>0)
            &&(FactionManager.IsHostile(e,Player)||FactionManager.IsHostile(Player,e)||e.GetPart<BrainPart>()?.IsPersonallyHostileTo(Player)==true||e.GetPart<BrainPart>()?.Target==Player)).ToArray();
        private bool Safe(Zone zone,Cell cell,int clearance)=>Safe(zone,cell,clearance,Threats(zone));
        private bool Safe(Zone zone,Cell cell,int clearance,Entity[] threats)
        {
            if(cell==null||!zone.CanPlaceFootprint(Player,cell.X,cell.Y))return false;
            foreach(var c in zone.GetOccupiedCells(Player,cell.X,cell.Y))
            {
                if(c==null||c.Occupants.Any(e=>e!=Player&&(e.HasTag("Creature")||e.HasPart<TriggerOnStepPart>()||e.HasPart<LiquidPoolPart>()||e.HasPart<GasPoolPart>()
                    ||e.HasEffect<BurningEffect>()||e.GetPart<ThermalPart>()?.IsAflame==true)))return false;
                var state=zone.TileState.Get(c.X,c.Y);if(state!=null&&(state.Heat>0||state.Cold>0||state.Charge>0||!string.IsNullOrEmpty(state.Cloud)||state.Coatings.Count>0))return false;
                if(threats.Any(e=>SpatialQuery.DistanceToCell(zone,e,c.X,c.Y)<=clearance))return false;
            }
            return true;
        }
        private void Travel(Zone destination,Cell cell,string purpose)
        {
            Require(State=="Normal"&&Safe(destination,cell,12),"labelled travel has a current safe physical destination");
            string stats=Stats(Player),gear=Gear(Player);var old=Zone;int tick=Tick,energy=Energy;
            Require(old.TryTransferEntityTo(Player,destination,cell.X,cell.Y),"actual graph transfer for labelled approach");
            if(old!=destination)typeof(InputHandler).GetMethod("HandleZoneTransition",Private).Invoke(_input,new object[]{new ZoneTransitionResult{Success=true,NewZone=destination,NewPlayerX=cell.X,NewPlayerY=cell.Y}});
            Require(Stats(Player)==stats&&Gear(Player)==gear&&Tick==tick&&Energy==energy,"approach shortcut preserves actor stats/kit/turns");
            _input.CameraFollow?.SnapToPlayer();ZoneRenderHooks.MarkFullDirty("DensityTravellerNativeApproach");
            _notes.Add("LABELLED TRAVEL "+old.ZoneID+"→"+destination.ZoneID+"@"+cell.X+","+cell.Y+" "+purpose);Observe("approach-transfer");
        }
        private IEnumerator PaidStep(int dx,int dy,Zone destination,int x,int y)
        {
            var actor=Player;int tick=Tick,energy=Energy,ambient=Ambient;
            Require(Safe(destination,destination.GetCell(x,y),3),"fresh destination footprint before native key");
            yield return Tap(Direction(dx,dy));yield return Settled();
            Require(ReferenceEquals(Player,actor)&&ReferenceEquals(Zone,destination)&&At.X==x&&At.Y==y&&OneAction(tick,energy)&&Ambient==ambient+1,
                "native paid step reaches exact current footprint with one action");Observe("native-step");
        }
        private IEnumerator PaidWait()
        {
            var at=At;int tick=Tick,energy=Energy,ambient=Ambient;yield return Tap(Key.Period);yield return Settled();
            Require(ReferenceEquals(At,at)&&OneAction(tick,energy)&&Ambient==ambient+1,"stationary native wait pays exactly one player action");Observe("native-wait");
        }
        private bool OneAction(int tick,int energy)=>Energy==energy-TurnManager.ActionThreshold+(Tick-tick)*Player.GetStatValue("Speed",TurnManager.DefaultSpeed);
        private static Key Direction(int dx,int dy)
        {
            dx=Math.Sign(dx);dy=Math.Sign(dy);
            if(dx==1&&dy==0)return Key.D;if(dx==-1&&dy==0)return Key.A;if(dx==0&&dy==1)return Key.S;if(dx==0&&dy==-1)return Key.W;
            if(dx==1&&dy==1)return Key.Numpad3;if(dx==1&&dy==-1)return Key.Numpad9;if(dx==-1&&dy==1)return Key.Numpad1;if(dx==-1&&dy==-1)return Key.Numpad7;
            throw new InvalidOperationException("No native direction for zero displacement.");
        }
        private static Key Shortcut(string text)=>(Key)Enum.Parse(typeof(Key),text);
        private string VillageName(string id){var (x,y,z)=WorldMap.FromZoneID(id);return z==0&&WorldMapAuthoring.InBounds(x,y)?Manager.WorldMap.GetPOI(x,y)?.Name:null;}
        private bool UniqueMerchant()=>Manager.CachedZones.Values.SelectMany(z=>z.GetReadOnlyEntities()).Count(e=>e.ID==_merchantId)==1;
        private static bool Owns(Entity actor,Entity item)=>actor.GetPart<InventoryPart>().Objects.Contains(item)||actor.GetPart<InventoryPart>().EquippedItems.Values.Contains(item);
        private static IEnumerable<string> OwnedIds(Entity actor)=>actor.GetPart<InventoryPart>().Objects.Concat(actor.GetPart<InventoryPart>().EquippedItems.Values).Distinct().Select(e=>e.ID+":"+(e.GetPart<StackerPart>()?.StackCount??1));
        private static string Stock(Entity actor)=>string.Join("|",actor.GetPart<InventoryPart>().Objects.OrderBy(e=>e.ID,StringComparer.Ordinal).Select(e=>e.ID+":"+e.BlueprintName+":"+(e.GetPart<StackerPart>()?.StackCount??1)+":owner="+e.GetPart<PhysicsPart>()?.InInventory?.ID));
        private static string Gear(Entity actor)
        {
            var inv=actor.GetPart<InventoryPart>();
            return Stock(actor)+";slots="+string.Join("|",inv.EquippedItems.OrderBy(p=>p.Key).Select(p=>p.Key+":"+p.Value.ID))
                +";body="+string.Join("|",(actor.GetPart<Body>()?.GetParts().Where(p=>p.Equipped!=null).OrderBy(p=>p.ID).Select(p=>p.ID+":"+p.Equipped.ID))??Enumerable.Empty<string>())
                +";links="+string.Join("|",inv.EquippedItems.Values.Distinct().OrderBy(e=>e.ID).Select(e=>e.ID+":"+e.GetPart<PhysicsPart>()?.Equipped?.ID));
        }
        private static string Stats(Entity actor)=>string.Join("|",actor.Statistics.OrderBy(p=>p.Key).Select(p=>p.Key+":"+p.Value.BaseValue+":"+p.Value.Value+":"+p.Value.Min+":"+p.Value.Max));
        private static string TravellerMemory(Entity actor)=>string.Join("|",actor.Properties.Where(p=>p.Key.StartsWith("Traveller",StringComparison.Ordinal)).OrderBy(p=>p.Key).Select(p=>p.Key+"="+p.Value))
            +";"+string.Join("|",actor.IntProperties.Where(p=>p.Key.StartsWith("Traveller",StringComparison.Ordinal)).OrderBy(p=>p.Key).Select(p=>p.Key+"="+p.Value));
        private void RequireOrdinary(){Require(Player.GetStat("Hitpoints").Max==40&&Player.GetStatValue("Strength")==18&&Player.GetStatValue("Agility")==18&&Player.GetStatValue("Toughness")==18,"ordinary unraised stats");Require(!DevMode.Enabled&&!Player.HasPart<BitLockerPart>()&&!Player.HasTag("Invulnerable")&&!CombatSystem.IsDeathHandled(Player),"ordinary living game rules");}
        private IEnumerator Settled(){double began=Time.realtimeSinceStartupAsDouble;while(State!="Normal"||_input.ZoneRenderer?.WorldFx?.HasBlockingFx==true){Require(Time.realtimeSinceStartupAsDouble-began<8,"input/FX settle: "+State);yield return null;}yield return null;}
        private IEnumerator Tap(params Key[] keys)
        {
            Require(_clock.Elapsed.TotalSeconds<550,"finite native acceptance deadline");double began=Time.realtimeSinceStartupAsDouble;
            while(_input!=null&&Time.time-(float)Field(_input,"_lastMoveTime")<_input.MoveRepeatDelay){Require(Time.realtimeSinceStartupAsDouble-began<3,"input rate gate");yield return null;}
            if(_started)Require(Player.GetStatValue("Hitpoints")>10&&!CombatSystem.IsDeathHandled(Player),"ordinary HP safety stop");
            _keys.Add(new{sequence=_keys.Count,keys=string.Join(",",keys.Select(k=>k.ToString())),state=_input==null?"bootstrap":State,tick=_input?.TurnManager?.TickCount??0});WriteReport();
            _keyboard.MakeCurrent();InputSystem.QueueStateEvent(_keyboard,new KeyboardState(keys));yield return null;InputSystem.QueueStateEvent(_keyboard,new KeyboardState());yield return null;yield return new WaitForSecondsRealtime(.13f);
        }
        private IEnumerator Capture(string name){yield return new WaitForSecondsRealtime(.15f);yield return new WaitForEndOfFrame();Directory.CreateDirectory(DirectoryPath);string path=Path.Combine(DirectoryPath,name+".png");DensityNativeScreenshot.CaptureToFile(path);Require(File.Exists(path)&&new FileInfo(path).Length>0,"native screenshot");_screenshots.Add(path);WriteReport();}
        private static object Field(object owner,string name){var f=owner.GetType().GetField(name,Private|BindingFlags.Public);if(f==null)throw new InvalidOperationException("Missing observed field "+owner.GetType().Name+"."+name);return f.GetValue(owner);}
        private static string HashFile(string path){using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        private static void Require(bool value,string reason){if(!value)throw new InvalidOperationException("Traveller audit precondition: "+reason);}
        private void Check(string name,bool passed){if(!passed)_failures++;_audit.Add((passed?"PASS ":"FAIL ")+name);Observe(name);Diag.Record("scenario","DensityTravellerNativeCase",payload:new{runId=RunId,name,passed});}
        private void Observe(string phase)
        {
            if(_input?.PlayerEntity==null)return;
            var merchant=string.IsNullOrEmpty(_merchantId)?null:Manager.CachedZones.Values.SelectMany(z=>z.GetReadOnlyEntities()).FirstOrDefault(e=>e.ID==_merchantId);
            _observations.Add(new{phase,zone=Zone.ZoneID,x=At?.X,y=At?.Y,tick=Tick,world=WorldClock.CurrentTick,energy=Energy,ambient=Ambient,hp=Player.GetStatValue("Hitpoints"),
                player=Player.ID,playerCoins=TradeSystem.GetDrams(Player),gear=Gear(Player),memory=TravellerMemory(Player),merchant=merchant?.ID,stock=merchant==null?null:Stock(merchant),
                merchantCoins=TradeSystem.GetDrams(merchant),lastRestock=merchant?.GetIntProperty(TraderRestockSystem.LastRestockProp),lastMessage=MessageLog.GetLast()});WriteReport();
        }
        private IEnumerator RunSafely(IEnumerator routine)
        {
            var stack=new Stack<IEnumerator>();stack.Push(routine);
            try
            {
                while(stack.Count>0)
                {
                    bool moved=false;object next=null;Exception error=null;
                    try{moved=stack.Peek().MoveNext();if(moved)next=stack.Peek().Current;}catch(Exception e){error=e;}
                    if(error!=null){_fatal=error.ToString();Check("native_precondition_failed",false);Debug.LogError("[DensityTravellerNative] "+error);break;}
                    if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}if(next is IEnumerator child){stack.Push(child);continue;}yield return next;
                }
            }
            finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();Finish();}
        }
        public void SetUnexpectedErrors(int errors){_unexpectedErrors=errors;_errorsFinalized=true;WriteReport();EmitSummary();}
        public void Abort(string reason){if(Finished)return;_fatal=reason;Check("native_aborted",false);StopAllCoroutines();Finish();}
        private void Finish(){Cleanup();Finished=true;WriteReport();}
        private bool Complete=>Finished&&_errorsFinalized&&Failures==0&&RequiredChecks.All(n=>_audit.Contains("PASS "+n))&&_audit.Count==RequiredChecks.Length&&_screenshots.Count>=8;
        private void WriteReport()
        {
            Directory.CreateDirectory(DirectoryPath);ReportPath=Path.Combine(DirectoryPath,"report.json");
            string json=JsonConvert.SerializeObject(new{runId=RunId,complete=Complete,failures=Failures,unexpectedErrors=_unexpectedErrors,errorsFinalized=_errorsFinalized,seconds=_clock?.Elapsed.TotalSeconds??0,
                fatal=_fatal,audit=_audit.ToArray(),requiredChecks=RequiredChecks,nativeKeys=_keys.ToArray(),observations=_observations.ToArray(),emissions=_emissions.ToArray(),notes=_notes.ToArray(),screenshots=_screenshots.ToArray(),checkpointHash=_checkpointHash,
                canVerify="Native new game; actual entry-created merchant and scheduler; native route dialogue, identity/purse-conserving trade, real edge revisit before restock due, F5/stock mutation/F6 exact replacement graph, one actual generated contextual speaker and shared19-action silence, finite ordinary scheduling and cleanup.",
                cannotVerify="Labelled approach transfers shorten travel, without spawning/granting/moving other owners or bypassing scheduler. One seeded route is not density/balance or whole-world discovery evidence. No offscreen caravan simulation or permanently finite stock claim; existing restock is >300 scheduler ticks with shelf threshold<3. Existing synthetic cases cover other voices/silent species/refusals; this route does not create them. Images require human review; no general performance/readability claim."},Formatting.Indented);
            var parsed=JObject.Parse(json);Require(parsed["audit"] is JArray a&&a.Count==_audit.Count&&parsed["emissions"] is JArray e&&e.Count==_emissions.Count&&parsed["observations"] is JArray o&&o.Count==_observations.Count,"report preserves exact nested evidence arrays");File.WriteAllText(ReportPath,json);
        }
        private void Cleanup(){if(_cleaned)return;_cleaned=true;MessageLog.OnMessage-=OnMessage;if(_keyboard!=null){InputSystem.QueueStateEvent(_keyboard,new KeyboardState());InputSystem.RemoveDevice(_keyboard);}if(_oldKeyboard!=null&&_oldKeyboard.added)_oldKeyboard.MakeCurrent();if(_oldSettings!=null)InputSystem.settings=_oldSettings;if(_settings!=null)Destroy(_settings);Application.runInBackground=_oldBackground;}
        private void EmitSummary(){if(_summaryEmitted)return;_summaryEmitted=true;Diag.Record("scenario","DensityTravellerNativeSummary",payload:new{runId=RunId,cases=_audit.Count,failures=Failures,complete=Complete,errorsFinalized=_errorsFinalized});}
        private void OnDestroy(){if(!Finished&&_clock!=null){_fatal="Play stopped before completion.";Check("native_interrupted",false);Finish();}Cleanup();EmitSummary();foreach(var p in _oldChannels)Diag.SetChannel(p.Key,p.Value);}
    }
}
