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
    /// <summary>Finite continuous resource journey. Every travel, trade, loot,
    /// combat, rest and checkpoint mutation uses ordinary native keys. This
    /// driver observes source graphs and never grants, relocates or heals them.</summary>
    public sealed class DensityCampaignNativePlayer:MonoBehaviour
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static readonly (int x,int y)[] Steps={(1,0),(-1,0),(0,1),(0,-1),(1,1),(1,-1),(-1,1),(-1,-1)};
        static readonly string[] RequiredChecks={"ordinary_start","native_map_town","purchased_empty_water","filled_actual_well","original_dagger_equipped",
            "actual_finished_find","real_encounter_damage","earned_sale","affordable_resupply","actual_injury_recovery","revisited_depleted_source",
            "checkpoint_saved","real_unsaved_transaction","replacement_resource_graph","ordinary_finish"};
        static readonly HashSet<string> Containers=new HashSet<string>{"Chest","Crate","WoodenBarrel","Sack","Urn","StrongBox","LockedChest"};
        public string RunId{get;}=Guid.NewGuid().ToString("N");
        public bool Finished{get;private set;} public int Failures=>_failures+_unexpectedErrors;public string ReportPath{get;private set;}
        InputHandler _input;Keyboard _keyboard,_oldKeyboard;InputSettings _settings,_oldSettings;
        bool _started,_cleaned,_oldBackground,_errorsFinalized,_summaryEmitted;
        readonly Dictionary<string,bool> _oldChannels=new Dictionary<string,bool>();
        readonly List<string> _audit=new List<string>(),_screenshots=new List<string>(),_notes=new List<string>();
        readonly List<object> _keys=new List<object>(),_observations=new List<object>(),_windows=new List<object>();
        System.Diagnostics.Stopwatch _clock;int _failures,_unexpectedErrors,_localInputs,_mapSteps,_rests,_opened,_completedTurns,_pureClock,_originalTonicsUsed;
        string _tonicId;string _fatal,_ownedRoot,_checkpointHash,_merchantId,_waterId,_lootId,_containerId,_sourceZone,_sourceContents,_resupplyId;
        Entity _encounter;DensityCampaignNativeEvidence.ClockReceipt _lastClock;
        string DirectoryPath=>Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/DensityCompletion/Campaign/NativeAcceptance",RunId));
        Entity Player=>_input.PlayerEntity;Zone Zone=>_input.CurrentZone;OverworldZoneManager Manager=>_input.ZoneManager as OverworldZoneManager;
        Cell At=>Zone.GetEntityCell(Player);int Tick=>_input.TurnManager.TickCount;int Energy=>_input.TurnManager.GetEnergy(Player);
        string State=>Field(_input,"_inputState").ToString();
        Entity Merchant=>Manager.CachedZones["Overworld.3.6.0"].GetReadOnlyEntities().Single(e=>e.ID==_merchantId);
        Entity Container=>Manager.CachedZones[_sourceZone].GetReadOnlyEntities().Single(e=>e.ID==_containerId);
        Entity Carried(string id)=>Player.GetPart<InventoryPart>().Objects.SingleOrDefault(e=>e.ID==id);
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
            yield return new WaitForSecondsRealtime(.8f);_input=FindFirstObjectByType<InputHandler>();Require(_input!=null,"ordinary input");
            var boot=(BootMenuController)Field(_input,"_bootMenuController");Require(boot?.IsActive==true,"real isolated boot menu");yield return Tap(Key.N);yield return Settled();_started=true;
            Require(Manager.WorldSeed==1&&!boot.IsActive,"declared native seed1 new game");
            Check("ordinary_start",Player.GetStatValue("Hitpoints")==40&&Player.GetStat("Hitpoints").Max==40&&Player.GetStatValue("Level")==1&&TradeSystem.GetDrams(Player)==50&&Player.GetStatValue("Strength")==18&&Player.GetStatValue("Agility")==18);
            _notes.Add("No travel transfers. All map/ground steps below use ordinary native keys; macro travel is not local overland exploration.");yield return Capture("01-ordinary-start");
            _tonicId=Player.GetPart<InventoryPart>().Objects.Single(e=>e.BlueprintName=="HealingTonic").ID;var dagger=Player.GetPart<InventoryPart>().Objects.Single(e=>e.BlueprintName=="Dagger");yield return ItemAction(dagger,"equip_auto");yield return CloseNormal();
            Check("original_dagger_equipped",dagger.GetPart<PhysicsPart>().Equipped==Player&&Player.GetPart<Body>().GetParts().Any(b=>b.Equipped==dagger));
            yield return TravelSurface("Overworld.3.6.0");Check("native_map_town",Zone.ZoneID=="Overworld.3.6.0"&&_mapSteps>0);
            var seller=Zone.GetReadOnlyEntities().Where(e=>e.BlueprintName=="MorrowfastSella"&&e.GetPart<InventoryPart>()!=null).OrderBy(e=>e.ID,StringComparer.Ordinal).FirstOrDefault();Require(seller!=null,"actual Sella stock owner");_merchantId=seller.ID;
            yield return Approach(seller,40);yield return OpenTrade(seller,true);LogQuotes(seller);
            var skin=TradeSystem.GetTraderStock(seller).SingleOrDefault(e=>e.BlueprintName=="Waterskin");Require(skin?.GetPart<WaterskinPart>()?.Charges==0,"actual empty offered Waterskin");_waterId=skin.ID;
            yield return Transaction(seller,skin,true,"prepare-water");yield return Tap(Key.Escape);yield return Settled();
            Check("purchased_empty_water",Owns(Player,skin)&&skin.GetPart<WaterskinPart>().Charges==0);yield return FillWater();
            Check("filled_actual_well",Carried(_waterId).GetPart<WaterskinPart>().Charges==3);yield return Capture("03-purchased-filled-water");
            foreach(string address in SourceAddresses())
            {
                yield return TravelSurface(address);_notes.Add("ENTERED SOURCE "+address+" biome="+Manager.WorldMap.GetBiome(WorldMap.FromZoneID(address).x,WorldMap.FromZoneID(address).y));
                foreach(var box in Zone.GetReadOnlyEntities().Where(e=>Containers.Contains(e.BlueprintName)&&e.GetPart<ContainerPart>()!=null).OrderBy(e=>e.ID,StringComparer.Ordinal).ToArray())
                {
                    var cp=box.GetPart<ContainerPart>();if(cp.IsLocked){_notes.Add("LOCKED SKIP "+box.ID);continue;}
                    if(PathTo(c=>SpatialQuery.DistanceToCell(Zone,box,c.X,c.Y)<=1)==null){_notes.Add("NO CURRENT SAFE APPROACH "+box.ID);continue;}
                    if(_opened>=4)break;yield return Approach(box,35);_opened++;yield return OpenBox(box);
                    var item=cp.Contents.Where(e=>e.HasPart<CommercePart>()&&(e.HasPart<MeleeWeaponPart>()||e.HasPart<ArmorPart>())&&CanTransferExact(e,Player))
                        .OrderBy(e=>e.ID,StringComparer.Ordinal).FirstOrDefault();
                    _notes.Add("OPENED "+address+":"+box.ID+" actualStock="+Contents(box)+" selected="+item?.ID);
                    if(item==null){if(State=="PickupOpen")yield return Tap(Key.Escape);yield return Settled();continue;}
                    _containerId=box.ID;_sourceZone=address;_lootId=item.ID;yield return AcquireBoxItem(item);_sourceContents=Contents(box);
                    Check("actual_finished_find",Owns(Player,item)&&!cp.Contents.Contains(item)&&item.GetPart<PhysicsPart>().InInventory==Player);
                    yield return ItemAction(item,"examine_item");Require(State=="AnnouncementOpen","native actual find inspection");yield return Capture("04-actual-find");yield return CloseNormal();break;
                }
                if(_lootId!=null)break;if(_opened>=4)break;
            }
            Require(_lootId!=null,"six actual addresses/four permitted opens produced a carryable finished find; no forced content");
            yield return Encounter();yield return Capture("05-actual-expedition-injury");
            yield return TravelSurface("Overworld.3.6.0");LogRestock(Merchant,"return");yield return Approach(Merchant,40);yield return OpenTrade(Merchant,false);
            var earned=Carried(_lootId);Require(earned!=null&&CanTransferExact(earned,Merchant),"actual acquired find still carried and legally saleable");
            int beforeSale=TradeSystem.GetDrams(Player);yield return Transaction(Merchant,earned,false,"earned-find-sale");Check("earned_sale",Owns(Merchant,earned)&&TradeSystem.GetDrams(Player)>beforeSale);
            var supply=TradeSystem.GetTraderStock(Merchant).Where(e=>e.ID!=_lootId&&new[]{"LiquidFlask","BurnSalve","HealingTonic","WaterTonic","DriedMeat"}.Contains(e.BlueprintName)&&CanTransferExact(e,Player))
                .OrderBy(e=>TradeSystem.GetBuyPrice(e,TradeSystem.GetTradePerformance(Player),Merchant)).ThenBy(e=>e.ID,StringComparer.Ordinal)
                .FirstOrDefault(e=>TradeSystem.GetBuyPrice(e,TradeSystem.GetTradePerformance(Player),Merchant)<=TradeSystem.GetDrams(Player));
            Require(supply!=null,"actual post-sale purse affords current whole-stack supply");_resupplyId=supply.ID;int quote=TradeSystem.GetBuyPrice(supply,TradeSystem.GetTradePerformance(Player),Merchant);
            _notes.Add("RESUPPLY funding: quote="+quote+" dramsBeforeEarnedSale="+beforeSale+"; money is fungible, sale is not claimed necessary when quote was already affordable.");
            yield return Transaction(Merchant,supply,true,"actual-resupply");Check("affordable_resupply",Owns(Player,supply));yield return Tap(Key.Escape);yield return Settled();yield return FillWater();yield return Recover();
            yield return TravelSurface(_sourceZone);yield return Approach(Container,35);yield return OpenBox(Container);
            Check("revisited_depleted_source",Contents(Container)==_sourceContents&&!Container.GetPart<ContainerPart>().Contents.Any(e=>e.ID==_lootId));if(State=="PickupOpen")yield return Tap(Key.Escape);yield return Settled();
            yield return TravelSurface("Overworld.3.6.0");LogRestock(Merchant,"final-return");yield return Approach(Merchant,40);yield return Checkpoint();
            Check("ordinary_finish",Player.GetStatValue("Hitpoints")>10&&!DebugInvincibility.IsEnabled(Player)&&!DevMode.Enabled&&!Player.HasPart<BitLockerPart>()&&State=="Normal");yield return Capture("08-final-resources");
        }
        IEnumerable<string> SourceAddresses()
        {
            // Authoritative map/address order is chosen before reading contents.
            return Enumerable.Range(0,WorldMap.Height).SelectMany(y=>Enumerable.Range(0,WorldMap.Width).Select(x=>(x,y)))
                .Where(p=>Manager.WorldMap.GetPOI(p.x,p.y)==null&&Manager.WorldMap.GetBiome(p.x,p.y)==BiomeType.Spread
                    &&!OverworldZoneManager.AuthoredWildernessZoneIDs.Contains(WorldMap.ToZoneID(p.x,p.y,0)))
                .OrderBy(p=>Math.Abs(p.x-3)+Math.Abs(p.y-6)).ThenBy(p=>p.y).ThenBy(p=>p.x).Take(6).Select(p=>WorldMap.ToZoneID(p.x,p.y,0)).ToArray();
        }
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
        {var zone=Zone;Require(Safe(zone,zone.GetCell(x,y),3),"fresh native movement footprint");yield return Paid(Tap(Direction(x-At.X,y-At.Y)),"local","ground-step");Require(Zone==zone&&At.X==x&&At.Y==y,"native step current destination");}
        string Mark(string label)
        {Diag.Record("scenario",DensityCampaignNativeEvidence.MarkerKind,actor:Player,payload:new{runId=RunId,label});return Diag.Snapshot(1).Single().TraceId;}
        Diag.Entry[] Window(string marker)
        {var rows=Diag.Snapshot(Diag.BufferCapacity).SkipWhile(e=>e.TraceId!=marker).ToArray();Require(rows.Length>0,"retained current action marker");return rows;}
        IEnumerator Paid(IEnumerator action,string kind,string label)
        {
            Require(kind=="map"?_mapSteps<60:kind=="rest"?_rests<2:_localInputs<120,"finite approved action budget");
            var actor=Player;int before=Energy,tick=Tick,speed=Player.GetStatValue("Speed",100);string marker=Mark(label);
            yield return action;yield return CloseNormal();var rows=Window(marker);
            Require(ReferenceEquals(actor,Player)&&Player.GetStatValue("Speed",100)==speed&&DensityCampaignNativeEvidence.TryClock(rows,marker,Player.ID,kind,before,Energy,Tick-tick,speed,out _lastClock),"exact native action clock/energy "+label);
            if(kind=="map")_mapSteps++;else if(kind=="rest")_rests++;else _localInputs++;_completedTurns+=_lastClock.CompletedTurns;_pureClock+=_lastClock.PureClock;
            _windows.Add(new{label,kind,marker,beforeEnergy=before,afterEnergy=Energy,beforeTick=tick,afterTick=Tick,receipt=_lastClock,rows});Observe(label);
        }
        IEnumerator FillWater()
        {
            var skin=Carried(_waterId);Require(skin!=null,"same purchased vessel");
            if(skin.GetPart<WaterskinPart>().Charges==skin.GetPart<WaterskinPart>().Capacity){_notes.Add("REFILL already full; no invented water consumption");yield break;}
            var well=Zone.GetReadOnlyEntities().Where(e=>e.HasPart<WellPart>()).OrderBy(e=>SpatialQuery.Distance(Zone,Player,e)).FirstOrDefault(e=>PathTo(c=>SpatialQuery.DistanceToCell(Zone,e,c.X,c.Y)<=1)!=null);
            Require(well!=null,"actual reachable fresh-water well");yield return Approach(well,30);
            var actualSource=typeof(WaterVesselService).GetMethod("FindSource",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{Player,Zone}) as Entity;
            Require(ReferenceEquals(actualSource,well),"actual read-only source selection is this nearby well");int before=skin.GetPart<WaterskinPart>().Charges;
            yield return Paid(ItemAction(skin,"FillWaterskin"),"local","native-fill-waterskin");Require(skin.GetPart<WaterskinPart>().Charges>before,"actual paid fill increases same water units");
            _notes.Add("WATER source="+well.ID+" vessel="+skin.ID+" "+before+"→"+skin.GetPart<WaterskinPart>().Charges);
        }
        IEnumerator OpenBox(Entity box)
        {Require(!box.GetPart<ContainerPart>().IsLocked,"live combined lock authority");yield return WorldAction(box,"OpenContainer");Require((box.GetPart<ContainerPart>().Contents.Count==0&&State=="Normal")||(State=="PickupOpen"&&ReferenceEquals(Field(_input.PickupUI,"_sourceContainer"),box)),"actual opened current container or truthful empty result");}
        IEnumerator AcquireBoxItem(Entity item)
        {
            string all=Contents(Container);var rows=(IList)Field(_input.PickupUI,"_items");int index=rows.IndexOf(item);Require(index>=0,"actual offered find");
            for(int n=0;(int)Field(_input.PickupUI,"_cursorIndex")!=index;n++){Require(n<80,"bounded pickup cursor");yield return Tap((int)Field(_input.PickupUI,"_cursorIndex")<index?Key.DownArrow:Key.UpArrow);}
            yield return Paid(TakeAndClose(),"local","native-container-acquisition");Require(Owns(Player,item)&&all==string.Join("|",Container.GetPart<ContainerPart>().Contents.Concat(new[]{item}).OrderBy(e=>e.ID,StringComparer.Ordinal).Select(e=>e.ID+":"+e.BlueprintName+":"+(e.GetPart<StackerPart>()?.StackCount??1))),"actual carrying capacity, exact acquired source units and remaining content conservation");
        }
        IEnumerator TakeAndClose(){yield return Tap(Key.Enter);if(State=="PickupOpen")yield return Tap(Key.Escape);}
        IEnumerator Encounter()
        {
            var candidates=Threats(Zone).Where(e=>e.GetStatValue("Hitpoints")<=25&&e.GetStatValue("Hitpoints")>0&&e.GetPart<BrainPart>()?.CurrentZone==Zone)
                .OrderBy(e=>SpatialQuery.Distance(Zone,Player,e)).ThenBy(e=>e.ID,StringComparer.Ordinal).ToArray();
            foreach(var candidate in candidates){_encounter=candidate;var path=PathTo(c=>SpatialQuery.DistanceToCell(Zone,candidate,c.X,c.Y)==1);
                if(path!=null&&path.Count<=20)break;_notes.Add("ENCOUNTER no current bounded physical approach "+candidate.ID);_encounter=null;}
            Require(_encounter!=null,"one actual ordinary local encounter; no enemy grant");_notes.Add("ENCOUNTER actual "+_encounter.ID+":"+_encounter.BlueprintName+" HP="+_encounter.GetStatValue("Hitpoints"));
            int startingHP=Player.GetStatValue("Hitpoints");bool dealt=false,took=false;int attempts=0;
            while(Zone.GetEntityCell(_encounter)!=null&&!CombatSystem.IsDeathHandled(_encounter)&&_encounter.GetStatValue("Hitpoints")>0)
            {
                Require(attempts++<24,"finite single encounter");
                var tonic=Carried(_tonicId);
                if(Player.GetStatValue("Hitpoints")<=20&&tonic!=null&&_originalTonicsUsed<2)
                {int units=tonic.GetPart<StackerPart>()?.StackCount??1,tick=Tick,energy=Energy;string tonicMarker=Mark("original-tonic-use");
                    yield return ItemAction(tonic,"ApplyTonic");yield return CloseNormal();
                    int remaining=Carried(_tonicId)?.GetPart<StackerPart>()?.StackCount??(Carried(_tonicId)==null?0:1);
                    Require(remaining==units-1&&Tick==tick&&Energy==energy&&Window(tonicMarker).Any(r=>r.Category=="event"&&r.Kind=="TonicApplied"&&r.ActorId==Player.ID),"one original tonic consumed through the actual free inventory action");
                    _originalTonicsUsed++;_windows.Add(new{label="free-original-tonic",tonicMarker,rows=Window(tonicMarker),units,remaining,tick,energy});continue;}
                if(SpatialQuery.Distance(Zone,Player,_encounter)>1){yield return Approach(_encounter,24);continue;}
                var at=At;var target=SpatialQuery.ClosestCell(Zone,_encounter,at.X,at.Y);string marker=Mark("encounter-attribution");
                yield return Paid(Tap(Direction(target.X-at.X,target.Y-at.Y)),"local","ordinary-dagger-attack");
                dealt|=Window(marker).Any(r=>r.Category=="damage"&&r.Kind=="DamageDealt"&&r.ActorId==Player.ID&&r.TargetId==_encounter.ID&&JObject.Parse(r.PayloadJson).Value<int>("amount")>0);
                took|=Window(marker).Any(r=>r.Category=="damage"&&r.Kind=="DamageDealt"&&r.ActorId==_encounter.ID&&r.TargetId==Player.ID&&JObject.Parse(r.PayloadJson).Value<int>("amount")>0);
            }
            _notes.Add("ENCOUNTER end target="+_encounter.ID+" HP="+_encounter.GetStatValue("Hitpoints")+" death="+CombatSystem.IsDeathHandled(_encounter)+" player="+startingHP+"→"+Player.GetStatValue("Hitpoints"));
            Check("real_encounter_damage",dealt&&took&&Player.GetStatValue("Hitpoints")<startingHP&&Player.GetStatValue("Hitpoints")>10);_encounter=null;
            Require(_audit.Last()=="PASS real_encounter_damage","actual damage-free or unsafe encounter leaves recovery gate unmet");
        }
        Entity[] Threats(Zone zone)=>zone.GetReadOnlyEntities().Where(e=>e!=Player&&e!=_encounter&&e.HasTag("Creature")&&!CombatSystem.IsDeathHandled(e)&&e.GetStatValue("Hitpoints")>0
            &&(FactionManager.IsHostile(e,Player)||FactionManager.IsHostile(Player,e)||e.GetPart<BrainPart>()?.IsPersonallyHostileTo(Player)==true||e.GetPart<BrainPart>()?.Target==Player)).ToArray();
        IEnumerator Recover()
        {
            var fire=Zone.GetReadOnlyEntities().Where(e=>e.HasPart<CampfirePart>()).OrderBy(e=>SpatialQuery.Distance(Zone,Player,e))
                .FirstOrDefault(e=>PathTo(c=>SpatialQuery.DistanceToCell(Zone,e,c.X,c.Y)==1)!=null);
            Require(fire!=null,"actual reachable campfire recovery");yield return Approach(fire,35);int hp=Player.GetStatValue("Hitpoints"),max=Player.GetStat("Hitpoints").Max;
            Require(hp>10&&hp<max,"actual expedition injury still needs recovery");yield return Paid(WorldAction(fire,"RestAtCampfire"),"rest","native-real-injury-rest");
            Check("actual_injury_recovery",DensityCampaignNativeEvidence.RecoveryMatches(_lastClock,"campfire",hp,Player.GetStatValue("Hitpoints"),max));yield return Capture("06-real-recovery");
        }
        void LogQuotes(Entity merchant){foreach(var e in TradeSystem.GetTraderStock(merchant))_notes.Add("QUOTE "+e.ID+":"+e.BlueprintName+" units="+(e.GetPart<StackerPart>()?.StackCount??1)+" price="+TradeSystem.GetBuyPrice(e,TradeSystem.GetTradePerformance(Player),merchant));}
        void LogRestock(Entity merchant,string label)=>_notes.Add("RESTOCK "+label+" tick="+Tick+" stamp="+merchant.GetIntProperty(TraderRestockSystem.LastRestockProp)+" count="+merchant.GetPart<InventoryPart>().Objects.Count+" drams="+TradeSystem.GetDrams(merchant)+" stock="+Stock(merchant)+"; real >300/shelf<3 entry rule may legitimately refill");
        static string Contents(Entity box)=>string.Join("|",box.GetPart<ContainerPart>().Contents.OrderBy(e=>e.ID,StringComparer.Ordinal).Select(e=>e.ID+":"+e.BlueprintName+":"+(e.GetPart<StackerPart>()?.StackCount??1)));
        static bool Owns(Entity owner,Entity item)=>item!=null&&((owner.GetPart<InventoryPart>().Objects.Contains(item)&&item.GetPart<PhysicsPart>()?.InInventory==owner)
            ||(owner.GetPart<InventoryPart>().EquippedItems.Values.Contains(item)&&item.GetPart<PhysicsPart>()?.Equipped==owner&&owner.GetPart<Body>().GetParts().Any(p=>p.Equipped==item)));
        IEnumerator Checkpoint()
        {
            var oldPlayer=Player;var oldMerchant=Merchant;var oldBox=Container;string gear=Gear(Player),stock=Stock(Merchant),contents=Contents(Container),stats=Stats(Player),zone=Zone.ZoneID;
            int stamp=Merchant.GetIntProperty(TraderRestockSystem.LastRestockProp),tick=Tick,energy=Energy,world=WorldClock.CurrentTick,pc=TradeSystem.GetDrams(Player),mc=TradeSystem.GetDrams(Merchant),x=At.X,y=At.Y,water=Carried(_waterId).GetPart<WaterskinPart>().Charges;
            var info=SaveGameService.GetSaveInfo("Quick");Require(info!=null,"current actual checkpoint metadata");string file=Path.Combine(_ownedRoot,info.GameID,"Quick.sav.gz"),before=HashFile(file);long serial=MessageLog.NextSerialValue;
            yield return Tap(Key.F5);yield return Settled();_checkpointHash=HashFile(file);Check("checkpoint_saved",before!=_checkpointHash&&MessageLog.NextSerialValue>serial&&MessageLog.GetLast()=="Game saved."&&SaveGameService.GetSaveInfo("Quick").ActiveZoneID==zone);
            var supply=Carried(_resupplyId);Require(CanTransferExact(supply,Merchant),"actual supply can be sold back without merging");yield return OpenTrade(Merchant,false);yield return Transaction(Merchant,supply,false,"checkpoint-real-supply-sale");yield return Tap(Key.Escape);yield return Settled();
            Check("real_unsaved_transaction",Gear(Player)!=gear&&Stock(Merchant)!=stock&&Owns(Merchant,supply)&&HashFile(file)==_checkpointHash);yield return Capture("07-unsaved-real-sale");
            yield return Tap(Key.F6);double began=Time.realtimeSinceStartupAsDouble;while(ReferenceEquals(Player,oldPlayer)){Require(Time.realtimeSinceStartupAsDouble-began<8,"actual replacement player graph");yield return null;}yield return Settled();
            Check("replacement_resource_graph",!ReferenceEquals(Merchant,oldMerchant)&&!ReferenceEquals(Container,oldBox)&&Player.ID==oldPlayer.ID&&Zone.ZoneID==zone&&At.X==x&&At.Y==y
                &&Gear(Player)==gear&&Stock(Merchant)==stock&&Contents(Container)==contents&&Stats(Player)==stats&&Tick==tick&&Energy==energy&&WorldClock.CurrentTick==world
                &&Merchant.GetIntProperty(TraderRestockSystem.LastRestockProp)==stamp&&TradeSystem.GetDrams(Player)==pc&&TradeSystem.GetDrams(Merchant)==mc&&Carried(_waterId).GetPart<WaterskinPart>().Charges==water&&HashFile(file)==_checkpointHash);
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
                Require(path!=null&&path.Count>0,"finite current safe return route to "+target.X+","+target.Y);
                yield return StepTo(path[0].x,path[0].y);
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
            Require(_clock.Elapsed.TotalSeconds<900,"finite native acceptance deadline");double began=Time.realtimeSinceStartupAsDouble;
            while(_input!=null&&Time.time-(float)Field(_input,"_lastMoveTime")<_input.MoveRepeatDelay){Require(Time.realtimeSinceStartupAsDouble-began<3,"input rate gate");yield return null;}
            if(_started)Require(Player.GetStatValue("Hitpoints")>10&&!CombatSystem.IsDeathHandled(Player),"ordinary HP safety stop");
            _keys.Add(new{sequence=_keys.Count,keys=string.Join(",",keys.Select(k=>k.ToString())),state=_input==null?"bootstrap":State,tick=_input?.TurnManager?.TickCount??0});WriteReport();
            _keyboard.MakeCurrent();InputSystem.QueueStateEvent(_keyboard,new KeyboardState(keys));yield return null;InputSystem.QueueStateEvent(_keyboard,new KeyboardState());yield return null;yield return new WaitForSecondsRealtime(.13f);
        }

        private IEnumerator Capture(string name){yield return new WaitForSecondsRealtime(.15f);yield return new WaitForEndOfFrame();Directory.CreateDirectory(DirectoryPath);string path=Path.Combine(DirectoryPath,name+".png");DensityNativeScreenshot.CaptureToFile(path);Require(File.Exists(path)&&new FileInfo(path).Length>0,"native screenshot");_screenshots.Add(path);WriteReport();}

        private static object Field(object owner,string name){var f=owner.GetType().GetField(name,Private|BindingFlags.Public);if(f==null)throw new InvalidOperationException("Missing observed field "+owner.GetType().Name+"."+name);return f.GetValue(owner);}

        private static string HashFile(string path){using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}

        private static void Require(bool value,string reason){if(!value)throw new InvalidOperationException("Campaign audit precondition: "+reason);}

        private void Check(string name,bool passed){if(!passed)_failures++;_audit.Add((passed?"PASS ":"FAIL ")+name);Observe(name);Diag.Record("scenario","DensityCampaignNativeCase",payload:new{runId=RunId,name,passed});}

        private IEnumerator RunSafely(IEnumerator routine)
        {
            var stack=new Stack<IEnumerator>();stack.Push(routine);
            try
            {
                while(stack.Count>0)
                {
                    bool moved=false;object next=null;Exception error=null;
                    try{moved=stack.Peek().MoveNext();if(moved)next=stack.Peek().Current;}catch(Exception e){error=e;}
                    if(error!=null){_fatal=error.ToString();Check("native_precondition_failed",false);Debug.LogError("[DensityCampaignNative] "+error);break;}
                    if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}if(next is IEnumerator child){stack.Push(child);continue;}yield return next;
                }
            }
            finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();Finish();}
        }
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
        public void SetUnexpectedErrors(int errors){_unexpectedErrors=errors;_errorsFinalized=true;WriteReport();EmitSummary();}
        public void Abort(string reason){if(Finished)return;_fatal=reason;Check("native_aborted",false);StopAllCoroutines();Finish();}
        void Observe(string phase)
        {
            if(_input?.PlayerEntity==null)return;
            _observations.Add(new{phase,player=Player.ID,zone=Zone.ZoneID,x=At?.X,y=At?.Y,hp=Player.GetStatValue("Hitpoints"),stats=Stats(Player),tick=Tick,energy=Energy,
                drams=TradeSystem.GetDrams(Player),gear=Gear(Player),water=_waterId==null?(int?)null:Carried(_waterId)?.GetPart<WaterskinPart>()?.Charges,
                sourceZone=_sourceZone,container=_containerId,depletedContents=_sourceZone==null?null:Contents(Container),localInputs=_localInputs,mapSteps=_mapSteps,rests=_rests,completedTurns=_completedTurns,pureClock=_pureClock,message=MessageLog.GetLast()});WriteReport();
        }
        void Finish(){Cleanup();Finished=true;WriteReport();}
        bool Complete=>Finished&&_errorsFinalized&&Failures==0&&_audit.Count==RequiredChecks.Length&&RequiredChecks.All(n=>_audit.Contains("PASS "+n))&&_screenshots.Count>=8;
        void WriteReport()
        {
            Directory.CreateDirectory(DirectoryPath);ReportPath=Path.Combine(DirectoryPath,"report.json");
            string json=JsonConvert.SerializeObject(new{runId=RunId,complete=Complete,failures=Failures,unexpectedErrors=_unexpectedErrors,seconds=_clock?.Elapsed.TotalSeconds??0,
                fatal=_fatal,audit=_audit,requiredChecks=RequiredChecks,keys=_keys,observations=_observations,windows=_windows,notes=_notes,screenshots=_screenshots,
                localInputs=_localInputs,mapSteps=_mapSteps,rests=_rests,opened=_opened,originalTonicsUsed=_originalTonicsUsed,completedPlayerTurns=_completedTurns,pureClock=_pureClock,checkpointHash=_checkpointHash,
                canVerify="One ordinary continuous actor and acquired resources; real surface/worldmap/ground keys, whole-stack shop quotes and exact units/purses, actual permitted source acquisition, actual local fight/injury, earned sale then affordable resupply, injury recovery with pure-clock evidence, same depleted source and real economic mutation/F6 replacement graph.",
                cannotVerify="Finite seed1 surface journey, not campaign balance/frequency/all builds or deep progression. Worldmap macro travel is a real game feature but not ground exploration. No stock/HP/actor/content/RNG grants or transfers. No simulated NPC sleeping turns. Earned sale need not be financially necessary when pre-sale money already covers supply. Source absence, danger, unaffordability, damage-free route and caps leave named gates unmet; screenshots require actual viewing."},Formatting.Indented);
            var parsed=JObject.Parse(json);Require(parsed["windows"] is JArray w&&w.Count==_windows.Count&&parsed["observations"] is JArray o&&o.Count==_observations.Count,"report retains exact nested evidence");File.WriteAllText(ReportPath,json);
        }
        void Cleanup(){if(_cleaned)return;_cleaned=true;if(_keyboard!=null){InputSystem.QueueStateEvent(_keyboard,new KeyboardState());InputSystem.RemoveDevice(_keyboard);}if(_oldKeyboard!=null&&_oldKeyboard.added)_oldKeyboard.MakeCurrent();if(_oldSettings!=null)InputSystem.settings=_oldSettings;if(_settings!=null)Destroy(_settings);Application.runInBackground=_oldBackground;}
        void EmitSummary(){if(_summaryEmitted)return;_summaryEmitted=true;Diag.Record("scenario","DensityCampaignNativeSummary",payload:new{runId=RunId,cases=_audit.Count,failures=Failures,complete=Complete});}
        void OnDestroy(){if(!Finished&&_clock!=null){_fatal="Play stopped before completion.";Check("native_interrupted",false);Finish();}Cleanup();EmitSummary();foreach(var p in _oldChannels)Diag.SetChannel(p.Key,p.Value);}
    }
}
