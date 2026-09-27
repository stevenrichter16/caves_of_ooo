using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using CavesOfOoo.Storylets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Finite ordinary glade-to-Sill first-hour route using actual native keys.
    /// No actor, item, source, state or travel grants; partial failures remain evidence.</summary>
    public sealed class FirstHourGuidanceNativePlayer:MonoBehaviour
    {
        // Harness policy: ordinary risk, still avoiding adjacent active threats. This is not a safety guarantee.
        const int ThreatClearance=1;
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static readonly (int x,int y)[] Steps={(1,0),(-1,0),(0,1),(0,-1),(1,1),(1,-1),(-1,1),(-1,-1)};
        static readonly string[] RequiredChecks={"ordinary_start","warden_clearing_and_current_lead","glade_sign_current_lead","native_map_sill","ellun_current_marker_directions","accepted_existing_quest","native_marker_reached","authored_quest_reward","actual_affordable_gear_purchase","checkpoint_saved","real_unsaved_step","restored_exact_quest_and_owned_graph","completed_quest_no_repeat_reward","ordinary_finish"};
        public string RunId{get;}=Guid.NewGuid().ToString("N");
        public bool Finished{get;private set;}public int Failures=>_failures+_unexpectedErrors;public string ReportPath{get;private set;}
        InputHandler _input;Keyboard _keyboard,_oldKeyboard;InputSettings _settings,_oldSettings;
        bool _started,_cleaned,_oldBackground,_errorsFinalized,_summaryEmitted;
        readonly Dictionary<string,bool> _oldChannels=new Dictionary<string,bool>();
        readonly List<string> _audit=new List<string>(),_screenshots=new List<string>(),_notes=new List<string>();
        readonly List<object> _keys=new List<object>(),_observations=new List<object>(),_windows=new List<object>();
        System.Diagnostics.Stopwatch _clock;int _failures,_unexpectedErrors,_localInputs,_mapSteps,_rests,_completedTurns,_pureClock;
        string _fatal,_ownedRoot,_checkpointHash,_ellunId,_markerId,_merchantId,_purchaseId;Entity _allowedMarker;
        DensityCampaignNativeEvidence.ClockReceipt _lastClock;
        string DirectoryPath=>Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/SpreadFirstHour/M1/Native",RunId));
        Entity Player=>_input.PlayerEntity;Zone Zone=>_input.CurrentZone;OverworldZoneManager Manager=>_input.ZoneManager as OverworldZoneManager;
        Cell At=>Zone.GetEntityCell(Player);int Tick=>_input.TurnManager.TickCount;int Energy=>_input.TurnManager.GetEnergy(Player);
        string State=>Field(_input,"_inputState").ToString();
        Entity Owner(string id)=>Zone.GetReadOnlyEntities().SingleOrDefault(e=>e.ID==id);
        public void Initialize(ScenarioContext context)
        {
            Require(!string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride),"isolated launcher owns saves");_ownedRoot=SaveGameService.SaveRootOverride;_clock=System.Diagnostics.Stopwatch.StartNew();
            foreach(var channel in new[]{"scenario","event","trade","worldmap","furniture","damage","turn","turn-verbose","skill"}){_oldChannels[channel]=Diag.IsChannelEnabled(channel);Diag.SetChannel(channel,true);}
            _oldSettings=InputSystem.settings;_settings=Instantiate(_oldSettings);_settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            _settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings=_settings;_oldBackground=Application.runInBackground;Application.runInBackground=true;_oldKeyboard=Keyboard.current;_keyboard=InputSystem.AddDevice<Keyboard>();StartCoroutine(RunSafely(RunAudit()));
        }
        IEnumerator RunAudit()
        {
            yield return new WaitForSecondsRealtime(.8f);_input=FindFirstObjectByType<InputHandler>();Require(_input!=null,"actual native input");
            var boot=(BootMenuController)Field(_input,"_bootMenuController");Require(boot?.IsActive==true,"isolated ordinary boot menu");yield return Tap(Key.N);yield return Settled();_started=true;
            Require(Manager.WorldSeed==1&&!boot.IsActive&&ReferenceGladePlan.IsActive(Zone),"declared seed1 current glade start");
            Check("ordinary_start",Player.GetStatValue("Hitpoints")==40&&Player.GetStat("Hitpoints").Max==40&&Player.GetStatValue("Level")==1&&TradeSystem.GetDrams(Player)==50&&Player.GetStatValue("Strength")==18&&Player.GetStatValue("Agility")==18&&Player.GetPart<InventoryPart>().Objects.Count(e=>e.BlueprintName=="Dagger")==1&&Player.GetPart<InventoryPart>().Objects.Where(e=>e.BlueprintName=="HealingTonic").Sum(e=>e.GetPart<StackerPart>()?.StackCount??1)==2&&!DevMode.Enabled&&!Player.HasPart<BitLockerPart>());
            yield return Capture("01-ordinary-glade-start");
            var warden=Zone.GetReadOnlyEntities().SingleOrDefault(e=>e.BlueprintName=="Warden"&&e.GetPart<ConversationPart>()?.ConversationID=="Warden_1");Require(warden!=null,"actual authored glade Warden");
            yield return Approach(warden,80);yield return Chat(warden);string opening=ConversationManager.CurrentText;Require(opening.Contains("clearing"),"native Warden identifies actual clearing");
            var lead=RegionalGuidance.BuildGeographicDirections(warden).FirstOrDefault();Require(lead!=null&&lead.DestinationZoneID==WorldMap.StartingZoneID&&Manager.WorldMap.GetPOI(10,10)?.Type==POIType.Village,"actual existing map lead is Sill");
            yield return Choice(c=>c.Target=="AboutVillage");_notes.Add("WARDEN CURRENT TEXT "+ConversationManager.CurrentText);
            Check("warden_clearing_and_current_lead",ConversationManager.CurrentText.Contains(lead.Text)&&ConversationManager.CurrentText.Contains("I can't say who"));yield return Capture("02-warden-current-directions");yield return CloseNormal();
            var sign=Zone.GetReadOnlyEntities().SingleOrDefault(e=>e.BlueprintName=="Signpost");Require(sign!=null,"actual glade sign");yield return Approach(sign,100);yield return WorldAction(sign,"Examine");
            Require(State=="AnnouncementOpen","actual current sign reader");string signText=string.Join("\n",_input.AnnouncementUI.VisibleLines);_notes.Add("SIGN FIRST PAGE "+signText);
            Check("glade_sign_current_lead",signText.Contains(lead.DestinationName)&&signText.Contains("west"));yield return Capture("03-glade-sign");yield return CloseNormal();
            yield return TravelSurface(lead.DestinationZoneID);Check("native_map_sill",Zone.ZoneID==WorldMap.StartingZoneID&&_mapSteps>0);
            var ellun=Zone.GetReadOnlyEntities().SingleOrDefault(e=>e.GetPart<ConversationPart>()?.ConversationID=="BMO_Quest"&&e.GetPart<QuestBeaconPart>()?.Quest=="BmoCartridge");Require(ellun!=null,"actual living Sill Ellun source");_ellunId=ellun.ID;
            var marker=Zone.GetReadOnlyEntities().SingleOrDefault(e=>e.ID=="OldStump"&&e.BlueprintName=="OldStump"&&e.GetPart<QuestMarkerTriggerPart>()?.Fact=="bmo_stump_reached");Require(marker!=null,"actual current quest marker");_markerId=marker.ID;
            yield return Approach(ellun,100);yield return Chat(ellun);var speakerCell=Zone.GetEntityCell(ellun);var markerCell=Zone.GetEntityCell(marker);
            string direction=(markerCell.Y<speakerCell.Y?"north":markerCell.Y>speakerCell.Y?"south":"")+(markerCell.X<speakerCell.X?"west":markerCell.X>speakerCell.X?"east":"");
            _notes.Add("ELLUN CURRENT TEXT "+ConversationManager.CurrentText+"; speaker="+speakerCell.X+","+speakerCell.Y+" marker="+markerCell.X+","+markerCell.Y);
            Check("ellun_current_marker_directions",(NarrativeStatePart.Current?.GetFact("bmo_stump_reached")??0)==0&&ConversationManager.CurrentText.Contains(direction.Length==0?"right here":direction+" of where I'm standing"));
            yield return Capture("04-ellun-actual-marker-direction");yield return Choice(c=>c.Actions?.Any(a=>a.Key=="StartQuest"&&a.Value=="BmoCartridge")==true);
            Check("accepted_existing_quest",StoryletPart.Current?.IsQuestActive("BmoCartridge")==true);yield return CloseNormal();
            _allowedMarker=marker;yield return WalkTo(markerCell,120);Check("native_marker_reached",Zone.GetEntityCell(marker)==At&&(NarrativeStatePart.Current?.GetFact("bmo_stump_reached")??0)>=1&&(StoryletPart.Current.IsObjectiveFinished("BmoCartridge","reach_stump")||(StoryletPart.Current.GetQuestState("BmoCartridge")?.CurrentStageIndex??0)>=1));yield return Capture("05-real-stump-arrival");
            yield return Approach(ellun,120);yield return Chat(ellun);int money=TradeSystem.GetDrams(Player),experience=TotalExperience(Player),tick=Tick,energy=Energy;
            yield return Choice(c=>c.Actions?.Any(a=>a.Key=="CompleteQuest"&&a.Value=="BmoCartridge")==true);
            Check("authored_quest_reward",StoryletPart.Current.IsQuestCompleted("BmoCartridge")&&TradeSystem.GetDrams(Player)==money+40&&TotalExperience(Player)==experience+120&&Tick==tick&&Energy==energy);yield return Capture("06-authored-quest-reward");yield return CloseNormal();
            // Read only actual present stock and current route. No absent services are fabricated.
            var sellers=Zone.GetReadOnlyEntities().Where(e=>e.BlueprintName=="Merchant"&&e.GetPart<ConversationPart>()!=null&&e.GetPart<InventoryPart>()!=null&&e.GetStatValue("Hitpoints")>0&&!CombatSystem.IsDeathHandled(e)).OrderBy(e=>e.ID,StringComparer.Ordinal).ToArray();
            Entity merchant=null,purchase=null;
            foreach(var seller in sellers)
            {
                _notes.Add("ACTUAL STOCK "+seller.ID+" "+Stock(seller));if(PathTo(c=>SpatialQuery.DistanceToCell(Zone,seller,c.X,c.Y)<=1)==null)continue;
                var chosen=TradeSystem.GetTraderStock(seller).Where(e=>e.HasPart<EquippablePart>()&&e.HasPart<CommercePart>()&&CanTransferExact(e,Player))
                    .OrderBy(e=>TradeSystem.GetBuyPrice(e,TradeSystem.GetTradePerformance(Player),seller)).ThenBy(e=>e.ID,StringComparer.Ordinal)
                    .FirstOrDefault(e=>TradeSystem.GetBuyPrice(e,TradeSystem.GetTradePerformance(Player),seller)>0&&TradeSystem.GetBuyPrice(e,TradeSystem.GetTradePerformance(Player),seller)<=TradeSystem.GetDrams(Player));
                if(chosen!=null){merchant=seller;purchase=chosen;break;}
            }
            Require(merchant!=null&&purchase!=null,"actual reachable Sill merchant offers affordable nonmerging finished gear");_merchantId=merchant.ID;_purchaseId=purchase.ID;
            yield return Approach(merchant,100);yield return OpenTrade(merchant,false);yield return Transaction(merchant,purchase,true,"actual-sill-finished-gear");
            Check("actual_affordable_gear_purchase",Owns(Player,purchase)&&purchase.GetPart<PhysicsPart>()?.InInventory==Player);yield return Capture("07-real-sill-purchase");yield return CloseNormal();
            yield return Checkpoint();ellun=Owner(_ellunId);yield return Approach(ellun,100);money=TradeSystem.GetDrams(Player);experience=TotalExperience(Player);yield return Chat(ellun);
            Check("completed_quest_no_repeat_reward",StoryletPart.Current.IsQuestCompleted("BmoCartridge")&&!ConversationManager.VisibleChoices.Any(c=>c.Actions?.Any(a=>a.Key=="CompleteQuest"&&a.Value=="BmoCartridge")==true)&&TradeSystem.GetDrams(Player)==money&&TotalExperience(Player)==experience);yield return CloseNormal();
            Check("ordinary_finish",Player.GetStatValue("Hitpoints")>10&&!DebugInvincibility.IsEnabled(Player)&&!DevMode.Enabled&&!Player.HasPart<BitLockerPart>()&&State=="Normal");yield return Capture("09-finished-ordinary-route");
        }
        IEnumerator Chat(Entity owner)
        {
            Require(owner!=null&&Zone.GetEntityCell(owner)!=null&&owner.GetStatValue("Hitpoints")>0&&!CombatSystem.IsDeathHandled(owner),"current living conversation owner");
            yield return WorldAction(owner,"Chat");Require(ConversationManager.IsActive&&ReferenceEquals(ConversationManager.Speaker,owner)&&ReferenceEquals(ConversationManager.Listener,Player),"actual matching conversation participants");
            if((bool)Field(_input.DialogueUI,"_revealing"))yield return Tap(Key.Enter);
        }
        IEnumerator Choice(Func<CavesOfOoo.Data.ChoiceData,bool> match)
        {
            Require(ConversationManager.IsActive,"live native conversation choice");if((bool)Field(_input.DialogueUI,"_revealing"))yield return Tap(Key.Enter);
            var choices=ConversationManager.VisibleChoices.ToArray();int index=Array.FindIndex(choices,c=>match(c));Require(index>=0&&choices.Count(match)==1,"unique actual offered choice");
            _notes.Add("NATIVE CHOICE "+ConversationManager.CurrentNode?.ID+" "+choices[index].Text);yield return Tap(Shortcut(MenuShortcutMap.Key(MenuShortcutMap.Positional(index)).ToString()));
            if(ConversationManager.IsActive&&(bool)Field(_input.DialogueUI,"_revealing"))yield return Tap(Key.Enter);
        }
        static int TotalExperience(Entity actor)
        {int level=actor.GetStatValue("Level"),total=actor.GetStatValue("Experience");Require(level>=1&&level<30,"bounded earned levels");for(int i=1;i<level;i++)total+=LevelingSystem.XPToNextLevel(i);return total;}
        IEnumerator Checkpoint()
        {
            var beforePlayer=Player;var beforeZone=Zone;var beforeEllun=Owner(_ellunId);var beforeMarker=Owner(_markerId);var beforeMerchant=Owner(_merchantId);
            Require(beforeEllun!=null&&beforeMarker!=null&&beforeMerchant!=null&&Owns(Player,OwnerOrCarried(_purchaseId)),"exact checkpoint source/ownership graph");
            string gear=Gear(Player),stats=Stats(Player),stock=Stock(beforeMerchant);int tick=Tick,energy=Energy,world=WorldClock.CurrentTick,drams=TradeSystem.GetDrams(Player),sellerDrams=TradeSystem.GetDrams(beforeMerchant),x=At.X,y=At.Y;
            var markerCell=Zone.GetEntityCell(beforeMarker);int mx=markerCell.X,my=markerCell.Y;string source=Zone.ZoneID;
            var info=SaveGameService.GetSaveInfo("Quick");Require(info!=null,"actual native initial checkpoint");string file=Path.Combine(_ownedRoot,info.GameID,"Quick.sav.gz"),beforeHash=HashFile(file);long serial=MessageLog.NextSerialValue;
            yield return Tap(Key.F5);yield return Settled();_checkpointHash=HashFile(file);Check("checkpoint_saved",_checkpointHash!=beforeHash&&MessageLog.NextSerialValue>serial&&MessageLog.GetLast()=="Game saved."&&SaveGameService.GetSaveInfo("Quick").ActiveZoneID==source);
            var step=Steps.Select(d=>Zone.GetCell(x+d.x,y+d.y)).FirstOrDefault(c=>Safe(Zone,c,ThreatClearance)&&Zone.CanPlaceFootprint(Player,c.X,c.Y));Require(step!=null,"one genuine post-save safe step");yield return StepTo(step.X,step.Y);
            Check("real_unsaved_step",At!=beforeZone.GetCell(x,y)&&Tick>tick&&HashFile(file)==_checkpointHash);yield return Tap(Key.F6);double began=Time.realtimeSinceStartupAsDouble;
            while(ReferenceEquals(Player,beforePlayer)){Require(Time.realtimeSinceStartupAsDouble-began<8,"real F6 replaces actor graph");yield return null;}yield return Settled();
            var ellun=Owner(_ellunId);var marker=Owner(_markerId);var merchant=Owner(_merchantId);var actualMarker=marker==null?null:Zone.GetEntityCell(marker);_allowedMarker=marker;
            Check("restored_exact_quest_and_owned_graph",!ReferenceEquals(Zone,beforeZone)&&ellun!=null&&!ReferenceEquals(ellun,beforeEllun)&&marker!=null&&!ReferenceEquals(marker,beforeMarker)&&merchant!=null&&!ReferenceEquals(merchant,beforeMerchant)
                &&Player.ID==beforePlayer.ID&&Zone.ZoneID==source&&At.X==x&&At.Y==y&&actualMarker?.X==mx&&actualMarker?.Y==my&&StoryletPart.Current.IsQuestCompleted("BmoCartridge")&&(NarrativeStatePart.Current?.GetFact("bmo_stump_reached")??0)>=1
                &&Gear(Player)==gear&&Stats(Player)==stats&&Stock(merchant)==stock&&Tick==tick&&Energy==energy&&WorldClock.CurrentTick==world&&TradeSystem.GetDrams(Player)==drams&&TradeSystem.GetDrams(merchant)==sellerDrams&&Owns(Player,OwnerOrCarried(_purchaseId))&&HashFile(file)==_checkpointHash);
            yield return Capture("08-real-restored-quest-purchase");
        }
        Entity OwnerOrCarried(string id)=>Player.GetPart<InventoryPart>().Objects.Concat(Player.GetPart<InventoryPart>().EquippedItems.Values).FirstOrDefault(e=>e.ID==id)??Owner(id);
        IEnumerator TravelSurface(string target)
        {
            Require(State=="Normal"&&WorldMap.FromZoneID(Zone.ZoneID).z==0,"native map travel begins on surface");
            if(Zone.ZoneID==target)yield break;
            yield return Paid(Tap(Key.LeftShift,Key.Comma),"local","native-map-ascend");Require(WorldMap.IsWorldMapZoneID(Zone.ZoneID),"actual native map ascent");
            var (wx,wy,_)=WorldMap.FromZoneID(target);var (zx,zy)=WorldMap.WorldCellToZoneCell(wx,wy);
            while(At.X!=zx||At.Y!=zy)
            {
                int dx=Math.Sign(zx-At.X),dy=Math.Sign(zy-At.Y);int x=At.X+dx,y=At.Y+dy;
                yield return Paid(Tap(Direction(dx,dy)),"map","native-worldmap-step");Require(At.X==x&&At.Y==y,"actual requested worldmap cell");
            }
            yield return Paid(Tap(Key.LeftShift,Key.Period),"local","native-map-descend");Require(Zone.ZoneID==target,"actual requested surface entered");
        }
        IEnumerator StepTo(int x,int y)
        {
            var zone=Zone;Require(Safe(zone,zone.GetCell(x,y),ThreatClearance),"fresh native movement footprint");
            var doors=zone.GetOccupiedCells(Player,x,y).SelectMany(c=>c.Occupants).Distinct().Where(e=>e.GetPart<DoorPart>()?.IsClosed==true).ToArray();
            if(doors.Length>0)
            {
                Require(doors.Length==1&&doors[0].GetPart<DoorPart>().CanOperate(Player,zone),"one actual permitted ordinary door");var at=At;var door=doors[0];
                yield return Paid(WorldAction(door,DoorPart.OpenCommand),"local","native-door-open");
                Require(Zone==zone&&At==at&&zone.GetEntityCell(door)!=null&&!door.GetPart<DoorPart>().IsClosed,"paid stationary real door open");
            }
            Require(Safe(zone,zone.GetCell(x,y),ThreatClearance)&&zone.CanPlaceFootprint(Player,x,y),"recheck current body after actual door action");
            yield return Paid(Tap(Direction(x-At.X,y-At.Y)),"local","ground-step");Require(Zone==zone&&At.X==x&&At.Y==y,"native step current destination");
        }
        string Mark(string label)
        {Diag.Record("scenario",DensityCampaignNativeEvidence.MarkerKind,actor:Player,payload:new{runId=RunId,label});return Diag.Snapshot(1).Single().TraceId;}
        Diag.Entry[] Window(string marker)
        {var rows=Diag.Snapshot(Diag.BufferCapacity).SkipWhile(e=>e.TraceId!=marker).ToArray();Require(rows.Length>0,"retained current action marker");return rows;}
        IEnumerator Paid(IEnumerator action,string kind,string label)
        {
            Require(kind=="map"?_mapSteps<20:kind=="rest"?_rests<2:_localInputs<240,"finite approved action budget");
            var actor=Player;int before=Energy,tick=Tick,speed=Player.GetStatValue("Speed",100);string marker=Mark(label);
            yield return action;yield return CloseNormal();var rows=Window(marker);
            Require(ReferenceEquals(actor,Player)&&Player.GetStatValue("Speed",100)==speed&&DensityCampaignNativeEvidence.TryClock(rows,marker,Player.ID,kind,before,Energy,Tick-tick,speed,out _lastClock),"exact native action clock/energy "+label);
            if(kind=="map")_mapSteps++;else if(kind=="rest")_rests++;else _localInputs++;_completedTurns+=_lastClock.CompletedTurns;_pureClock+=_lastClock.PureClock;
            _windows.Add(new{label,kind,marker,beforeEnergy=before,afterEnergy=Energy,beforeTick=tick,afterTick=Tick,receipt=_lastClock,rows});Observe(label);
        }
        IEnumerator ItemAction(Entity item,string command)
        {
            Require(State=="Normal"&&Owns(Player,item),"native owned inventory action");yield return Tap(Key.I);yield return Tap(Key.Tab);
            var rows=(IList)Field(_input.InventoryUI,"_rows");int row=-1;for(int i=0;i<rows.Count;i++)if(ReferenceEquals(((InventoryScreenData.ItemDisplay)Field(rows[i],"Item"))?.Item,item))row=i;
            Require(row>=0,"actual current inventory row");for(int n=0;(int)Field(_input.InventoryUI,"_cursorIndex")!=row;n++){Require(n<80,"bounded inventory cursor");yield return Tap((int)Field(_input.InventoryUI,"_cursorIndex")<row?Key.DownArrow:Key.UpArrow);}
            yield return Tap(Key.Enter);var popup=Field(_input.InventoryUI,"_itemActionPopup");Require(popup!=null,"native item action popup");var actions=((IList)Field(popup,"Actions")).Cast<object>().ToArray();int index=Array.FindIndex(actions,a=>(string)Field(a,"Command")==command);Require(index>=0,"actual offered item action "+command);
            for(int n=0;(int)Field(popup,"CursorIndex")!=index;n++){Require(n<50,"bounded item action cursor");yield return Tap((int)Field(popup,"CursorIndex")<index?Key.DownArrow:Key.UpArrow);}yield return Tap(Key.Enter);
        }
        IEnumerator CloseNormal(){for(int n=0;State!="Normal";n++){Require(n<8,"bounded ordinary modal closure "+State);yield return Tap(Key.Escape);}yield return Settled();}
        private IEnumerator OpenTrade(Entity seller,bool proveDialogue)
        {
            yield return WorldAction(seller,"Chat");Require(ConversationManager.IsActive&&ReferenceEquals(ConversationManager.Speaker,seller),"exact real merchant conversation");
            if((bool)Field(_input.DialogueUI,"_revealing"))yield return Tap(Key.Enter);
            string text=ConversationManager.CurrentText;
            if(proveDialogue){_notes.Add("ACTUAL TOWN DIALOGUE "+seller.ID+" "+text);yield return Capture("02-real-town-shop");}
            int choice=ConversationManager.VisibleChoices.ToList().FindIndex(c=>c.Actions?.Any(a=>a.Key=="StartTrade")==true);
            Require(choice>=0,"real StartTrade choice");yield return Tap(Shortcut(MenuShortcutMap.Key(MenuShortcutMap.Positional(choice)).ToString()));
            Require(_input.TradeUI.IsOpen,"native trade UI");
        }

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
            string marker=Mark(label);yield return Tap(Key.Enter);Require((bool)Field(_input.TradeUI,"_confirmActive"),"native trade confirmation");yield return Tap(Key.Enter);
            Require(!_input.TradeUI.IsOpen||(bool)Field(_input.TradeUI,"_confirmActive")==false,"confirmation consumed");
            Require(Owns(destination,item)&&!Owns(source,item)&&item.GetPart<PhysicsPart>()?.InInventory==destination
                &&TradeSystem.GetDrams(Player)==playerCoins+(buy?-price:price)&&TradeSystem.GetDrams(seller)==sellerCoins+(buy?price:-price)
                &&combined.SequenceEqual(OwnedIds(Player).Concat(OwnedIds(seller)).OrderBy(s=>s,StringComparer.Ordinal))&&Tick==tick&&Energy==energy,
                "real native trade exact identity/quantity/purse conservation and free UI semantics");
            Require(DensityCampaignNativeEvidence.TradeMatches(Window(marker),marker,Player.ID,seller.ID,item.ID,buy,price,playerCoins,TradeSystem.GetDrams(Player),sellerCoins,TradeSystem.GetDrams(seller)),"exact fresh native trade receipt");
            _notes.Add("TRANSACTION "+label+" item="+item.ID+" price="+price+" buy="+buy+" player="+playerCoins+"→"+TradeSystem.GetDrams(Player)+" seller="+sellerCoins+"→"+TradeSystem.GetDrams(seller));Observe(label);
        }

        private IEnumerator WorldAction(Entity target,string command)
        {
            Require(Zone.GetEntityCell(target)!=null&&SpatialQuery.Distance(Zone,Player,target)<=1,"actual adjacent current world owner");
            var at=At;var to=SpatialQuery.ClosestCell(Zone,target,at.X,at.Y);
            yield return Tap(Key.C);Require(State=="AwaitingTalkDirection","native C direction prompt");yield return Tap(to==at?Key.Period:Direction(to.X-at.X,to.Y-at.Y));
            Require(State=="WorldActionMenuOpen","native world menu");
            string choose=WorldInteractionSystem.PickTargetCommandPrefix+target.ID;
            var offered=(List<InventoryAction>)Field(_input.WorldActionMenuUI,"_actions");
            if((_input.WorldActionMenuUI.SelectedCellIsPile||!ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget,target))
                &&!offered.Any(a=>a.Command==choose)&&offered.Any(a=>a.Command==WorldInteractionSystem.PickCellCommand))
                yield return MenuAction(WorldInteractionSystem.PickCellCommand);
            if(!ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget,target)||((List<InventoryAction>)Field(_input.WorldActionMenuUI,"_actions")).Any(a=>a.Command==choose))
                yield return MenuAction(choose);
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
                if(SpatialQuery.Distance(Zone,Player,target)<=1)yield break;
                var path=PathTo(c=>SpatialQuery.DistanceToCell(Zone,target,c.X,c.Y)==1);
                if(path==null||path.Count==0)RouteDiagnostic("owner "+target.ID+" at "+Zone.GetEntityCell(target).X+","+Zone.GetEntityCell(target).Y,c=>SpatialQuery.DistanceToCell(Zone,target,c.X,c.Y)==1);
                Require(path!=null&&path.Count>0,"finite currently safe route to actual owner "+target.ID);
                yield return StepTo(path[0].x,path[0].y);
            }
            throw new InvalidOperationException("Finite native owner approach exceeded "+budget+" keys.");
        }

        private IEnumerator WalkTo(Cell target,int budget)
        {
            for(int n=0;n<budget;n++)
            {
                if(At.X==target.X&&At.Y==target.Y)yield break;
                var path=PathTo(c=>c.X==target.X&&c.Y==target.Y);
                if(path==null||path.Count==0)RouteDiagnostic("cell "+target.X+","+target.Y,c=>c==target);
                Require(path!=null&&path.Count>0,"finite current safe return route to "+target.X+","+target.Y);
                yield return StepTo(path[0].x,path[0].y);
            }
            throw new InvalidOperationException("Finite native return exceeded "+budget+" keys.");
        }

        private List<(int x,int y)> PathTo(Func<Cell,bool> goal)
        {
            var zone=Zone;var start=At;var threats=Threats(zone);var seen=new bool[Zone.Width,Zone.Height];var safe=new sbyte[Zone.Width,Zone.Height];
            var parent=new (int x,int y)[Zone.Width,Zone.Height];var queue=new Queue<(int x,int y)>();queue.Enqueue((start.X,start.Y));seen[start.X,start.Y]=true;
            bool Allowed(int x,int y){if(!zone.InBounds(x,y))return false;if(safe[x,y]==0)safe[x,y]=(sbyte)(Safe(zone,zone.GetCell(x,y),ThreatClearance,threats)?1:-1);return safe[x,y]>0;}
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

        void RouteDiagnostic(string goal,Func<Cell,bool> matches)
        {
            var zone=Zone;var threats=Threats(zone);var candidates=new List<object>();var neighbors=new List<object>();
            object Describe(Cell cell)
            {
                var occupied=zone.GetOccupiedCells(Player,cell.X,cell.Y).ToArray();
                return new{x=cell.X,y=cell.Y,physicallyAdmitted=zone.CanPlaceFootprint(Player,cell.X,cell.Y,allowOperableDoors:true),observerSafe=Safe(zone,cell,ThreatClearance,threats),
                    owners=occupied.SelectMany(c=>c.Occupants).Distinct().Select(e=>e.ID+":"+e.BlueprintName).ToArray(),
                    activeThreats=threats.Where(e=>occupied.Any(c=>SpatialQuery.DistanceToCell(zone,e,c.X,c.Y)<=ThreatClearance)).Select(e=>e.ID+"@"+zone.GetEntityCell(e).X+","+zone.GetEntityCell(e).Y).ToArray(),
                    tileStates=occupied.Select(c=>{var state=zone.TileState.Get(c.X,c.Y);return new{x=c.X,y=c.Y,heat=state?.Heat??0,cold=state?.Cold??0,charge=state?.Charge??0,cloud=state?.Cloud??"",cloudTurns=state?.CloudTurns??0,coatings=state?.Coatings.Select(l=>l.Id+":"+l.Turns).ToArray()??Array.Empty<string>()};}).ToArray()};
            }
            for(int x=0;x<Zone.Width;x++)for(int y=0;y<Zone.Height;y++){var cell=zone.GetCell(x,y);if(matches(cell))candidates.Add(Describe(cell));}
            foreach(var d in Steps){var cell=zone.GetCell(At.X+d.x,At.Y+d.y);if(cell!=null)neighbors.Add(Describe(cell));}
            _observations.Add(new{phase="route_refusal",goal,zone=zone.ZoneID,x=At.X,y=At.Y,hp=Player.GetStatValue("Hitpoints"),tick=Tick,threatClearance=ThreatClearance,candidates,neighbors});WriteReport();
        }

        private static Key Direction(int dx,int dy)
        {
            dx=Math.Sign(dx);dy=Math.Sign(dy);
            if(dx==1&&dy==0)return Key.D;if(dx==-1&&dy==0)return Key.A;if(dx==0&&dy==1)return Key.S;if(dx==0&&dy==-1)return Key.W;
            if(dx==1&&dy==1)return Key.Numpad3;if(dx==1&&dy==-1)return Key.Numpad9;if(dx==-1&&dy==1)return Key.Numpad1;if(dx==-1&&dy==-1)return Key.Numpad7;
            throw new InvalidOperationException("No native direction for zero displacement.");
        }

        private static Key Shortcut(string text)=>(Key)Enum.Parse(typeof(Key),text);

        private static IEnumerable<string> OwnedIds(Entity actor)=>actor.GetPart<InventoryPart>().Objects.Concat(actor.GetPart<InventoryPart>().EquippedItems.Values).Distinct().Select(e=>e.ID+":"+(e.GetPart<StackerPart>()?.StackCount??1));

        private static string Stock(Entity actor)=>string.Join("|",actor.GetPart<InventoryPart>().Objects.OrderBy(e=>e.ID,StringComparer.Ordinal).Select(e=>e.ID+":"+e.BlueprintName+":"+(e.GetPart<StackerPart>()?.StackCount??1)+":owner="+e.GetPart<PhysicsPart>()?.InInventory?.ID));

        private static string Gear(Entity actor)
        {
            var inv=actor.GetPart<InventoryPart>();
            return Stock(actor)+";slots="+string.Join("|",inv.EquippedItems.OrderBy(p=>p.Key).Select(p=>p.Key+":"+p.Value.ID))
                +";body="+string.Join("|",(actor.GetPart<Body>()?.GetParts().Where(p=>p.Equipped!=null).OrderBy(p=>p.ID).Select(p=>p.ID+":"+p.Equipped.ID))??Enumerable.Empty<string>())
                +";links="+string.Join("|",inv.EquippedItems.Values.Distinct().OrderBy(e=>e.ID).Select(e=>e.ID+":"+e.GetPart<PhysicsPart>()?.Equipped?.ID));
        }

        private static string Stats(Entity actor)=>string.Join("|",actor.Statistics.OrderBy(p=>p.Key).Select(p=>p.Key+":"+p.Value.BaseValue+":"+p.Value.Value+":"+p.Value.Bonus+":"+p.Value.Penalty+":"+p.Value.Min+":"+p.Value.Max))
            +";effects="+string.Join("|",actor.GetPart<StatusEffectsPart>()?.GetAllEffects().Select(e=>e.GetType().Name+":"+e.Duration).OrderBy(x=>x)??Enumerable.Empty<string>());

        private IEnumerator Settled(){double began=Time.realtimeSinceStartupAsDouble;while(State!="Normal"||_input.ZoneRenderer?.WorldFx?.HasBlockingFx==true){Require(Time.realtimeSinceStartupAsDouble-began<8,"input/FX settle: "+State);yield return null;}yield return null;}

        private IEnumerator Tap(params Key[] keys)
        {
            Require(_clock.Elapsed.TotalSeconds<600,"finite native acceptance deadline");double began=Time.realtimeSinceStartupAsDouble;
            while(_input!=null&&Time.time-(float)Field(_input,"_lastMoveTime")<_input.MoveRepeatDelay){Require(Time.realtimeSinceStartupAsDouble-began<3,"input rate gate");yield return null;}
            if(_started)Require(Player.GetStatValue("Hitpoints")>10&&!CombatSystem.IsDeathHandled(Player),"ordinary HP safety stop");
            _keys.Add(new{sequence=_keys.Count,keys=string.Join(",",keys.Select(k=>k.ToString())),state=_input==null?"bootstrap":State,tick=_input?.TurnManager?.TickCount??0});WriteReport();
            _keyboard.MakeCurrent();InputSystem.QueueStateEvent(_keyboard,new KeyboardState(keys));yield return null;InputSystem.QueueStateEvent(_keyboard,new KeyboardState());yield return null;yield return new WaitForSecondsRealtime(.13f);
        }

        private IEnumerator Capture(string name){yield return new WaitForSecondsRealtime(.15f);yield return new WaitForEndOfFrame();Directory.CreateDirectory(DirectoryPath);string path=Path.Combine(DirectoryPath,name+".png");DensityNativeScreenshot.CaptureToFile(path);Require(File.Exists(path)&&new FileInfo(path).Length>0,"native screenshot");_screenshots.Add(path);WriteReport();}

        private static object Field(object owner,string name){var f=owner.GetType().GetField(name,Private|BindingFlags.Public);if(f==null)throw new InvalidOperationException("Missing observed field "+owner.GetType().Name+"."+name);return f.GetValue(owner);}

        private static string HashFile(string path){using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}

        private static void Require(bool value,string reason){if(!value)throw new InvalidOperationException("First-hour guidance precondition: "+reason);}

        private void Check(string name,bool passed){if(!passed)_failures++;_audit.Add((passed?"PASS ":"FAIL ")+name);Observe(name);Diag.Record("scenario","FirstHourGuidanceNativeCase",payload:new{runId=RunId,name,passed});}

        private IEnumerator RunSafely(IEnumerator routine)
        {
            var stack=new Stack<IEnumerator>();stack.Push(routine);
            try
            {
                while(stack.Count>0)
                {
                    bool moved=false;object next=null;Exception error=null;
                    try{moved=stack.Peek().MoveNext();if(moved)next=stack.Peek().Current;}catch(Exception e){error=e;}
                    if(error!=null){_fatal=error.ToString();Check("native_precondition_failed",false);Debug.LogError("[FirstHourGuidanceNative] "+error);break;}
                    if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}if(next is IEnumerator child){stack.Push(child);continue;}yield return next;
                }
            }
            finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();Finish();}
        }
        private bool Safe(Zone zone,Cell cell,int clearance)=>Safe(zone,cell,clearance,Threats(zone));
        private bool Safe(Zone zone,Cell cell,int clearance,Entity[] threats)
        {
            if(cell==null||!zone.CanPlaceFootprint(Player,cell.X,cell.Y,allowOperableDoors:true))return false;
            foreach(var c in zone.GetOccupiedCells(Player,cell.X,cell.Y))
            {
                if(c==null||c.Occupants.Any(e=>e!=Player&&(e.HasTag("Creature")||(e.HasPart<TriggerOnStepPart>()&&!ReferenceEquals(e,_allowedMarker))||e.HasPart<LiquidPoolPart>()||e.HasPart<GasPoolPart>()
                    ||e.HasEffect<BurningEffect>()||e.GetPart<ThermalPart>()?.IsAflame==true)))return false;
                var state=zone.TileState.Get(c.X,c.Y);if(state!=null&&(state.Heat>0||state.Cold>0||state.Charge>0||!string.IsNullOrEmpty(state.Cloud)||state.Coatings.Count>0))return false;
                if(threats.Any(e=>SpatialQuery.DistanceToCell(zone,e,c.X,c.Y)<=clearance))return false;
            }
            return true;
        }
        Entity[] Threats(Zone zone)=>zone.GetReadOnlyEntities().Where(e=>e!=Player&&e.HasTag("Creature")&&e.GetStatValue("Hitpoints")>0&&!CombatSystem.IsDeathHandled(e)&&zone.GetEntityCell(e)!=null&&(FactionManager.IsHostile(e,Player)||FactionManager.IsHostile(Player,e)||e.GetPart<BrainPart>()?.Target==Player)).ToArray();
        static bool Owns(Entity owner,Entity item)=>item!=null&&((owner.GetPart<InventoryPart>().Objects.Contains(item)&&item.GetPart<PhysicsPart>()?.InInventory==owner)||(owner.GetPart<InventoryPart>().EquippedItems.Values.Contains(item)&&item.GetPart<PhysicsPart>()?.Equipped==owner));
        public void SetUnexpectedErrors(int errors){_unexpectedErrors=errors;_errorsFinalized=true;WriteReport();EmitSummary();}
        public void Abort(string reason){if(Finished)return;_fatal=reason;Check("native_aborted",false);StopAllCoroutines();Finish();}
        void Observe(string phase)
        {
            if(_input?.PlayerEntity==null)return;
            _observations.Add(new{phase,player=Player.ID,zone=Zone.ZoneID,x=At?.X,y=At?.Y,hp=Player.GetStatValue("Hitpoints"),stats=Stats(Player),tick=Tick,energy=Energy,drams=TradeSystem.GetDrams(Player),gear=Gear(Player),ellun=_ellunId,marker=_markerId,purchase=_purchaseId,questComplete=StoryletPart.Current?.IsQuestCompleted("BmoCartridge"),markerFact=NarrativeStatePart.Current?.GetFact("bmo_stump_reached"),message=MessageLog.GetLast()});WriteReport();
        }
        void Finish(){Cleanup();Finished=true;WriteReport();}
        bool Complete=>Finished&&_errorsFinalized&&Failures==0&&_audit.Count==RequiredChecks.Length&&RequiredChecks.All(n=>_audit.Contains("PASS "+n))&&_screenshots.Count>=9;
        void WriteReport()
        {
            Directory.CreateDirectory(DirectoryPath);ReportPath=Path.Combine(DirectoryPath,"report.json");
            string json=JsonConvert.SerializeObject(new{runId=RunId,complete=Complete,failures=Failures,unexpectedErrors=_unexpectedErrors,seconds=_clock?.Elapsed.TotalSeconds??0,fatal=_fatal,audit=_audit,requiredChecks=RequiredChecks,keys=_keys,observations=_observations,windows=_windows,notes=_notes,screenshots=_screenshots,localInputs=_localInputs,mapSteps=_mapSteps,completedPlayerTurns=_completedTurns,pureClock=_pureClock,checkpointHash=_checkpointHash,threatClearance=ThreatClearance,
                passedChecks=RequiredChecks.Where(n=>_audit.Contains("PASS "+n)).ToArray(),unmetChecks=RequiredChecks.Where(n=>!_audit.Contains("PASS "+n)).ToArray(),
                canVerify="Only the named passedChecks were observed in this run; intendedCapability is the planned route, not a completion claim. Partial failures and unmetChecks remain explicit.",
                intendedCapability="One ordinary seed1 actor: actual glade Warden/sign current-map lead, real worldmap/ground keys to Sill, current Ellun marker direction, original accept/step/report/reward, current affordable merchant gear, actual F5/paid-step/F6 replacement source/quest/ownership graph. Typed native keys are released between frames; no direct gameplay actions.",
                cannotVerify="Not all seeds, fallback starts, campaign balance, rare encounters or guaranteed services. Conservative threat/hazard selection may refuse playable routes. No entities, stock, HP, currency, progression, AI or RNG edits; no travel transfers. Ordinary earned level-up is permitted. Screenshots need actual viewing; stops leave named gates unmet."},Formatting.Indented);
            var parsed=JObject.Parse(json);Require(parsed["windows"] is JArray w&&w.Count==_windows.Count&&parsed["observations"] is JArray o&&o.Count==_observations.Count,"complete nested report evidence");File.WriteAllText(ReportPath,json);
        }
        void Cleanup(){if(_cleaned)return;_cleaned=true;if(_keyboard!=null){InputSystem.QueueStateEvent(_keyboard,new KeyboardState());InputSystem.RemoveDevice(_keyboard);}if(_oldKeyboard!=null&&_oldKeyboard.added)_oldKeyboard.MakeCurrent();if(_oldSettings!=null)InputSystem.settings=_oldSettings;if(_settings!=null)Destroy(_settings);Application.runInBackground=_oldBackground;}
        void EmitSummary(){if(_summaryEmitted)return;_summaryEmitted=true;Diag.Record("scenario","FirstHourGuidanceNativeSummary",payload:new{runId=RunId,cases=_audit.Count,failures=Failures,complete=Complete});}
        void OnDestroy(){if(!Finished&&_clock!=null){_fatal="Play stopped before completion.";Check("native_interrupted",false);Finish();}Cleanup();EmitSummary();foreach(var p in _oldChannels)Diag.SetChannel(p.Key,p.Value);}
    }
}
