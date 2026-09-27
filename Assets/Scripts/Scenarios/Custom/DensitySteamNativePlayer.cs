using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Finite steam scheduling comparison using actual native starter
    /// spells, inventory actions, movement and checkpoint keys. Source props and
    /// tonic supplies and water grimoire are explicit factory fixtures; player stats stay ordinary.</summary>
    public sealed class DensitySteamNativePlayer : MonoBehaviour
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        public string RunId { get; }=Guid.NewGuid().ToString("N");
        public bool Finished { get; private set; }
        public int Failures=>failures+unexpected;
        public string ReportPath { get; private set; }
        ScenarioContext context; InputHandler input; Keyboard keyboard,oldKeyboard;
        InputSettings oldSettings,settings; bool oldBackground,oldScenario,cleaned,errorsFinalized,summaryEmitted;
        int failures,unexpected,originX,originY,targetX,targetY,caseCount,checkpointWater,checkpointSalve; string ownedRoot,fatal,zoneId,caseName,checkpointGameId,checkpointHash;
        Entity target,probe; readonly HashSet<string> fixtureIds=new HashSet<string>();
        readonly List<Observation> observations=new List<Observation>();
        readonly List<Check> checks=new List<Check>(); readonly List<string> screenshots=new List<string>();
        readonly List<NativeKeyAction> nativeKeys=new List<NativeKeyAction>();
        System.Diagnostics.Stopwatch clock;
        string DirectoryPath=>Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/DensityCompletion/Environment/NativeSteamScheduling",RunId));
        public const int PlannedCases=4;
        public static readonly string[] RequiredBlueprints={"OilSlick","StoneFloor","Bush","TarSeep","OilSeep","WaterTonic","BurnSalve","ConjureWaterGrimoire","WaterPuddle","Dagger"};
        public void Initialize(ScenarioContext value)
        {
            if(string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride))throw new InvalidOperationException("Thermal baseline requires its isolated native launcher.");
            if(value?.Factory==null)throw new ArgumentException("A bootstrapped scenario context is required.",nameof(value));
            context=value;ownedRoot=SaveGameService.SaveRootOverride;clock=System.Diagnostics.Stopwatch.StartNew();
            oldScenario=Diag.IsChannelEnabled("scenario");Diag.SetChannel("scenario",true);
            oldKeyboard=Keyboard.current;oldSettings=InputSystem.settings;settings=Instantiate(oldSettings);
            settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings=settings;oldBackground=Application.runInBackground;Application.runInBackground=true;
            keyboard=InputSystem.AddDevice<Keyboard>();StartCoroutine(RunSafely(RunAudit()));
        }
        IEnumerator RunAudit()
        {
            yield return new WaitForSecondsRealtime(.8f);input=FindFirstObjectByType<InputHandler>();Require(input!=null,"ordinary input bootstrap");
            var boot=(BootMenuController)Field(input,"_bootMenuController");Require(boot!=null&&boot.IsActive,"owned native new-game marker");
            yield return Tap(Key.N);yield return Settled();Require(!boot.IsActive,"native N starts disposable game");
            Require(!DevMode.Enabled&&SaveGameService.SaveRootOverride==ownedRoot,"normal rules and isolated save root");
            foreach(string blueprint in RequiredBlueprints)Require(context.Factory.Blueprints.ContainsKey(blueprint),"real blueprint "+blueprint);
            RequireOrdinaryStats(true);PrepareLane();StageSupplies();yield return LearnConjureWater();
            Observe("initial-staging");yield return Capture("ordinary-stats-and-fixture-lane");
            var initialInfo=SaveGameService.GetSaveInfo("Quick");Require(initialInfo!=null,"new-game checkpoint metadata exists");
            checkpointGameId=initialInfo.GameID;string savePath=Path.Combine(ownedRoot,checkpointGameId,"Quick.sav.gz");
            string beforeHash=FileHash(savePath);long beforeSaveSerial=MessageLog.NextSerialValue;
            checkpointWater=Units("WaterTonic");checkpointSalve=Units("BurnSalve");
            yield return Tap(Key.F5);yield return Settled();
            var savedInfo=SaveGameService.GetSaveInfo("Quick");checkpointHash=FileHash(savePath);
            Require(MessageLog.GetLast()=="Game saved."&&MessageLog.NextSerialValue>beforeSaveSerial&&savedInfo?.GameID==checkpointGameId
                &&savedInfo.ActiveZoneID==zoneId&&checkpointHash!=beforeHash,"native F5 actually replaces checkpoint with staged lane");
            CheckResult("native_checkpoint_created",true);
            yield return SteamProbe("hot-clear",false,false);
            yield return RestoreCheckpoint();yield return SteamProbe("hot-steam",false,true);
            yield return RestoreCheckpoint();yield return SteamProbe("ambient-clear",true,false);
            yield return RestoreCheckpoint();yield return SteamProbe("ambient-steam",true,true);
            CheckResult("all_paired_cases_exercised",caseCount==PlannedCases);
            CheckResult("normal_rules_remain",!DevMode.Enabled&&SaveGameService.SaveRootOverride==ownedRoot);
        }
        void PrepareLane()
        {
            var manager=input.ZoneManager as OverworldZoneManager;Require(manager!=null&&manager.WorldSeed==64,"owned seed64 world");
            const int MaxCandidateZones=24,MinimumHostileDistance=32;
            int tried=0,bestDistance=-1,bestHostiles=int.MaxValue;Zone selected=null;
            for(int wy=0;wy<WorldMap.Height&&tried<MaxCandidateZones;wy++)for(int wx=0;wx<WorldMap.Width&&tried<MaxCandidateZones;wx++)
            {
                if(manager.WorldMap.GetBiome(wx,wy)!=BiomeType.Spread||manager.WorldMap.GetPOI(wx,wy)!=null)continue;
                tried++;var zone=manager.GetZone(WorldMap.ToZoneID(wx,wy,0));if(zone==null)continue;
                var hostiles=zone.GetAllEntities().Where(e=>e.HasTag("Creature")&&e!=input.PlayerEntity
                    &&(FactionManager.IsHostile(e,input.PlayerEntity)||FactionManager.IsHostile(input.PlayerEntity,e))).ToArray();
                for(int y=4;y<Zone.Height-4;y++)for(int x=4;x<Zone.Width-5;x++)
                {
                    int distance=hostiles.Length==0?Zone.Width:hostiles.Min(e=>SpatialQuery.DistanceToCell(zone,e,x,y));
                    if(distance<MinimumHostileDistance||distance<bestDistance||(distance==bestDistance&&hostiles.Length>=bestHostiles))continue;
                    bool clear=true;
                    for(int dy=-2;dy<=2&&clear;dy++)for(int dx=-2;dx<=3&&clear;dx++)
                    {
                        var cell=zone.GetCell(x+dx,y+dy);
                        if(cell==null||cell.BlocksMovement()||cell.Occupants.Any(e=>e.HasTag("Creature")||e.HasTag("Trap")||e.HasPart<LiquidPoolPart>()||e.HasPart<GasPoolPart>()||e.HasPart<ThermalPart>()||e.HasPart<StairsUpPart>()||e.HasPart<StairsDownPart>()||!WorldInteractionSystem.IsTerrain(e)))clear=false;
                    }
                    if(clear){selected=zone;originX=x;originY=y;bestDistance=distance;bestHostiles=hostiles.Length;}
                }
            }
            Require(selected!=null,"bounded generated lane at least32 native cells from all current hostile bodies; no scenery/actor removal");
            zoneId=selected.ZoneID;targetX=originX+1;targetY=originY;var old=input.CurrentZone;
            Require(old.TryTransferEntityTo(input.PlayerEntity,selected,originX,originY),"explicit travel shortcut preserves actual actor");
            if(!ReferenceEquals(old,selected))typeof(InputHandler).GetMethod("HandleZoneTransition",Private).Invoke(input,new object[]{new ZoneTransitionResult{Success=true,NewZone=selected,NewPlayerX=originX,NewPlayerY=originY}});
            input.CameraFollow?.SnapToPlayer();ZoneRenderHooks.MarkFullDirty("DensityThermalBaselineTravel");
            CheckResult("explicit_factory_staging_not_natural_discovery",input.CurrentZone.ZoneID==zoneId);
            Diag.Record("scenario","DensityThermalLaneSelected",payload:new{zoneId,originX,originY,tried,bestDistance,bestHostiles,minimum=MinimumHostileDistance});
        }
        void StageSupplies()
        {
            foreach(var pair in new[]{("WaterTonic",4),("BurnSalve",2),("ConjureWaterGrimoire",1)})for(int i=0;i<pair.Item2;i++)
            {
                var item=context.Factory.CreateEntity(pair.Item1);Require(input.PlayerEntity.GetPart<InventoryPart>().AddObject(item),"stage bounded "+pair.Item1);
            }
            CheckResult("native_starter_fire_available",Ability("CommandFlamingHands")!=null);
        }
        IEnumerator LearnConjureWater()
        {
            Require(Ability("CommandConjureWater")==null,"water spell is not silently pre-granted");
            var book=InventoryItem("ConjureWaterGrimoire");yield return OpenItemAction(book,"ReadGrimoire");
            Require(input.AnnouncementUI.IsOpen,"native grimoire learning announcement");yield return Tap(Key.Enter);
            Require(!input.AnnouncementUI.IsOpen&&State()=="InventoryOpen","learning returns to actual inventory");
            yield return Tap(Key.I);yield return Settled();
            Require(Ability("CommandConjureWater")!=null&&input.PlayerEntity.GetPart<InventoryPart>().Contains(book),"native reading learns water spell without consuming book");
            CheckResult("staged_grimoire_learned_through_native_inventory",true);
        }
        void StageTarget(string blueprint,int offset=1)
        {
            Require(State()=="Normal"&&Position()==(originX,originY),"checkpoint restored physical starting position");
            targetX=originX+offset;targetY=originY;
            var cell=input.CurrentZone.GetCell(targetX,targetY);Require(cell!=null&&!cell.BlocksMovement(),"legal target fixture cell");
            target=context.Factory.CreateEntity(blueprint);Require(target!=null&&input.CurrentZone.AddEntity(target,targetX,targetY),"actual factory source placement");
            fixtureIds.Add(target.ID);ZoneRenderHooks.MarkFullDirty("DensityThermalBaselineSource");Observe("source-staged");
        }
        IEnumerator RestoreCheckpoint()
        {
            Require(State()=="Normal","load requested outside menus");target=null;probe=null;var before=input.PlayerEntity;
            yield return Tap(Key.F6);double began=Time.realtimeSinceStartupAsDouble;
            while(ReferenceEquals(before,input.PlayerEntity)){Require(Time.realtimeSinceStartupAsDouble-began<8,"native F6 replaces checkpoint graph");yield return null;}
            yield return Settled();Require(input.CurrentZone.ZoneID==zoneId&&Position()==(originX,originY),"checkpoint zone and position");
            RequireOrdinaryStats(true);Require(Ability("CommandFlamingHands").CooldownRemaining==0&&Ability("CommandConjureWater")?.CooldownRemaining==0,"checkpoint native ability readiness");
            Require(Units("WaterTonic")==checkpointWater&&Units("BurnSalve")==checkpointSalve,"checkpoint restores bounded supplies");
            Require(SaveGameService.GetSaveInfo("Quick")?.GameID==checkpointGameId&&FileHash(Path.Combine(ownedRoot,checkpointGameId,"Quick.sav.gz"))==checkpointHash,"F6 does not rewrite owned checkpoint");
            Observe("native-checkpoint-restored");
        }
        IEnumerator SteamProbe(string name,bool ambient,bool keepSteam)
        {
            caseName=name;StageTarget("OilSeep");yield return Cast("CommandFlamingHands");
            Require(target.HasEffect<BurningEffect>(),"actual source ignited");
            yield return Tap(Key.A);yield return Settled();
            Require(Position()==(originX-1,originY)&&targetX-Position().x==2,"actual water endpoint alignment");
            var priorClouds=new HashSet<string>(input.CurrentZone.GetAllEntities().Where(e=>e.BlueprintName=="SteamCloud").Select(e=>e.ID));
            yield return Cast("CommandConjureWater");
            Require(target.HasEffect<SteamEffect>()&&NearbySteamClouds().Any(e=>!priorClouds.Contains(e.ID)),"real native water/fire reaction owns both status and new local visual");
            for(int i=0;target.HasEffect<BurningEffect>()&&i<8;i++){yield return SafeWait();Observe("waiting-for-real-burn-end-"+i);}
            Require(input.CurrentZone.GetEntityCell(target)!=null&&!target.HasEffect<BurningEffect>()&&target.GetEffect<SteamEffect>()?.Density>.2f,"live reaction owner retains steam after actual burn ends");
            Require(!target.HasEffect<WetEffect>()&&input.CurrentZone.TileState.Heat(targetX,targetY)==0,"no unrelated wet/heat admission in isolated scheduling control");
            // Explicit diagnostic control only: the source status was produced by
            // real spells above. Normalize temperature for the ambient pair; clear
            // that status for matched no-steam controls. No actor/NPC rule changes.
            if(ambient)target.GetPart<ThermalPart>().Temperature=target.GetPart<ThermalPart>().AmbientTemperature;
            if(!keepSteam)target.RemoveEffect<SteamEffect>();
            var sourceThermal=target.GetPart<ThermalPart>();
            Require(ambient?sourceThermal.Temperature==sourceThermal.AmbientTemperature:sourceThermal.Temperature>sourceThermal.AmbientTemperature,
                "actual source is in the declared ambient/hot admission class");
            Require(keepSteam?target.GetEffect<SteamEffect>()?.Density>0:!target.HasEffect<SteamEffect>(),
                "actual retained/removed steam control is nonvacuous");
            var probeCell=input.CurrentZone.GetCell(targetX,targetY+1);
            Require(probeCell!=null&&!probeCell.BlocksMovement()&&probeCell.Occupants.All(WorldInteractionSystem.IsTerrain)
                &&input.CurrentZone.TileState.Heat(probeCell.X,probeCell.Y)==0
                &&input.CurrentZone.TileState.Get(probeCell.X,probeCell.Y)?.Coatings?.Any(c=>c.Turns>0)!=true,
                "clear dry unheated adjacent measurement fixture cell");
            probe=context.Factory.CreateEntity("Dagger");var thermometer=probe.GetPart<ThermalPart>();
            Require(thermometer!=null,"actual dropped item thermal probe");
            thermometer.Temperature=40;thermometer.AmbientDecayRate=0;
            Require(input.CurrentZone.AddEntity(probe,targetX,targetY+1),"drop explicit factory thermometer fixture");fixtureIds.Add(probe.ID);
            Observe("explicit-probe-controls-staged");yield return Capture(name+"-controls");
            yield return SafeWait();Observe("probe-after-one-native-wait");
            Require(input.CurrentZone.GetEntityCell(target)!=null&&input.CurrentZone.GetEntityCell(probe)!=null,"exact source and probe remain current owners");
            float wet=probe.GetEffect<WetEffect>()?.Moisture??0;
            CheckResult(name+"_neighbor_cooling_wetting",keepSteam?thermometer.Temperature<40&&wet>0:thermometer.Temperature==40&&wet==0);
            yield return Capture(name+"-one-wait");
            for(int i=1;i<11;i++){yield return SafeWait();Observe("probe-wait-"+(i+1));}
            CheckResult(name+"_lifetime_finishes",!target.HasEffect<SteamEffect>());
            CheckResult(name+"_ordinary_actor_unchanged",input.PlayerEntity.GetStatValue("Hitpoints")==40&&!input.PlayerEntity.HasEffect<BurningEffect>());
            yield return Capture(name+"-eleven-waits");FinishCase();
        }
        IEnumerator SafeWait()
        {
            RequireOrdinaryStats(false);Require(input.PlayerEntity.GetStatValue("Hitpoints")>10,"ordinary HP safety boundary before waiting");
            int before=input.TurnManager.TickCount;int energy=input.TurnManager.GetEnergy(input.PlayerEntity);
            yield return Tap(Key.Period);yield return Settled();Require(before!=input.TurnManager.TickCount||energy!=input.TurnManager.GetEnergy(input.PlayerEntity),"native wait spends an action");
        }
        IEnumerator Cast(string command)
        {
            var abilities=input.PlayerEntity.GetPart<ActivatedAbilitiesPart>();int slot=-1;
            for(int i=0;i<ActivatedAbilitiesPart.SlotCount;i++)if(abilities.GetAbilityBySlot(i)?.Command==command){slot=i;break;}
            Require(slot>=0&&abilities.GetAbilityBySlot(slot).CooldownRemaining==0,"actual learned/starter spell ready without reset: "+command);
            Observe("before-native-"+command);yield return Tap((Key)Enum.Parse(typeof(Key),slot==9?"Digit0":"Digit"+(slot+1)));
            Require(State()=="AwaitingDirection","native hotbar requests spell direction: "+command);yield return Tap(Key.D);yield return Settled();
            Require(Ability(command).CooldownRemaining>0,"actual spell spent its cooldown: "+command);
        }
        ActivatedAbility Ability(string command)
        {
            var abilities=input.PlayerEntity.GetPart<ActivatedAbilitiesPart>();
            for(int i=0;i<ActivatedAbilitiesPart.SlotCount;i++){var a=abilities.GetAbilityBySlot(i);if(a?.Command==command)return a;}return null;
        }
        Entity InventoryItem(string blueprint)=>input.PlayerEntity.GetPart<InventoryPart>().Objects.FirstOrDefault(e=>e.BlueprintName==blueprint);
        IEnumerator ItemAction(string blueprint,string command)
        {
            var item=InventoryItem(blueprint);Require(item!=null,"carried fixture supply "+blueprint);int units=Units(blueprint);
            yield return OpenItemAction(item,command);Require(Units(blueprint)==units-1,"actual native consumption of one "+blueprint);
            // Tonic use leaves the inventory open; close it with the normal key.
            if(State()=="InventoryOpen")yield return Tap(Key.I);yield return Settled();
        }
        IEnumerator OpenItemAction(Entity item,string command)
        {
            Require(State()=="Normal","inventory action starts from normal input");yield return Tap(Key.I);Require(State()=="InventoryOpen","native I inventory");
            yield return Tap(Key.Tab);Require((int)Field(input.InventoryUI,"_panel")==1,"native inventory item panel");
            var rows=(IList)Field(input.InventoryUI,"_rows");int row=-1;
            for(int i=0;i<rows.Count;i++)if(ReferenceEquals(((InventoryScreenData.ItemDisplay)Field(rows[i],"Item"))?.Item,item)){row=i;break;}
            Require(row>=0,"actual inventory row identity");
            for(int i=0;(int)Field(input.InventoryUI,"_cursorIndex")!=row;i++){Require(i<80,"bounded inventory navigation");yield return Tap((int)Field(input.InventoryUI,"_cursorIndex")<row?Key.DownArrow:Key.UpArrow);}
            yield return Tap(Key.Enter);var popup=Field(input.InventoryUI,"_itemActionPopup");Require(popup!=null,"native inventory action popup");
            var actions=(IList)Field(popup,"Actions");int index=-1;for(int i=0;i<actions.Count;i++)if((string)Field(actions[i],"Command")==command){index=i;break;}
            Require(index>=0,"actual inventory command "+command);
            for(int i=0;(int)Field(popup,"CursorIndex")!=index;i++){Require(i<40,"bounded item action navigation");yield return Tap((int)Field(popup,"CursorIndex")<index?Key.DownArrow:Key.UpArrow);}
            yield return Tap(Key.Enter);
        }
        int Units(string blueprint)=>input.PlayerEntity.GetPart<InventoryPart>().Objects.Where(e=>e.BlueprintName==blueprint).Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);
        void RequireOrdinaryStats(bool fullHp)
        {
            var p=input.PlayerEntity;Require(p.GetStat("Hitpoints").Max==40&&p.GetStatValue("Strength")==18&&p.GetStatValue("Agility")==18&&p.GetStatValue("Toughness")==18,"ordinary unchanged character stats");
            Require(!DevMode.Enabled&&!p.HasTag("Invulnerable")&&!p.HasPart<BitLockerPart>(),"no developer defense or tinkering grants");
            if(fullHp)Require(p.GetStatValue("Hitpoints")==40&&!p.HasEffect<BurningEffect>()&&!p.HasEffect<WetEffect>(),"fresh ordinary checkpoint state");
        }
        void Observe(string phase)
        {
            if(input==null)return;var p=input.PlayerEntity;var pos=Position();var thermal=target?.GetPart<ThermalPart>();var material=target?.GetPart<MaterialPart>();
            observations.Add(new Observation{caseName=caseName??"setup",phase=phase,sequence=observations.Count,zone=input.CurrentZone.ZoneID,playerX=pos.x,playerY=pos.y,hp=p.GetStatValue("Hitpoints"),maxHp=p.GetStat("Hitpoints").Max,
                strength=p.GetStatValue("Strength"),agility=p.GetStatValue("Agility"),toughness=p.GetStatValue("Toughness"),heatResistance=p.GetStatValue("HeatResistance"),tick=input.TurnManager.TickCount,energy=input.TurnManager.GetEnergy(p),fireCooldown=Ability("CommandFlamingHands")?.CooldownRemaining??-1,waterCooldown=Ability("CommandConjureWater")?.CooldownRemaining??-1,
                probeId=probe?.ID,probePresent=probe!=null&&input.CurrentZone.GetEntityCell(probe)!=null,probeTemperature=probe?.GetPart<ThermalPart>()?.Temperature??0,probeWet=probe?.GetEffect<WetEffect>()?.Moisture??0,
                targetId=target?.ID,targetBlueprint=target?.BlueprintName,targetX=targetX,targetY=targetY,targetPresent=target!=null&&input.CurrentZone.GetEntityCell(target)!=null,targetHp=target?.GetPart<DestructiblePart>()?.HP??target?.GetStatValue("Hitpoints")??0,
                temperature=thermal?.Temperature??0,flameTemperature=thermal?.FlameTemperature??0,volatility=material?.Volatility??0,combustibility=material?.Combustibility??0,
                effectiveFlame=thermal==null?0:thermal.FlameTemperature-Math.Max(0,material?.Volatility??0)*100,
                playerBurn=p.GetEffect<BurningEffect>()?.Intensity??0,playerBurnTurns=p.GetEffect<BurningEffect>()?.Duration??0,playerWet=p.GetEffect<WetEffect>()?.Moisture??0,
                targetBurn=target?.GetEffect<BurningEffect>()?.Intensity??0,targetBurnTurns=target?.GetEffect<BurningEffect>()?.Duration??0,targetWet=target?.GetEffect<WetEffect>()?.Moisture??0,targetSteam=target?.GetEffect<SteamEffect>()?.Density??0,
                tileHeat=input.CurrentZone.TileState.Heat(targetX,targetY),tileState=JsonUtility.ToJson(input.CurrentZone.TileState.Get(targetX,targetY)),
                targetCellOccupants=input.CurrentZone.GetOccupants(targetX,targetY).Select(e=>e.BlueprintName+":"+e.ID).ToArray(),
                steamClouds=NearbySteamClouds().Select(e=>e.ID+":"+(e.GetPart<LifespanPart>()?.TurnsRemaining??-1)).ToArray(),
                wholeZoneHostiles=input.CurrentZone.GetAllEntities().Where(e=>e!=p&&e.HasTag("Creature")&&(FactionManager.IsHostile(e,p)||FactionManager.IsHostile(p,e))).Select(e=>e.BlueprintName+":"+e.ID+":distance="+SpatialQuery.Distance(input.CurrentZone,p,e)).ToArray(),
                nearbyCreatures=input.CurrentZone.GetAllEntities().Where(e=>e!=p&&e.HasTag("Creature")).Where(e=>{var at=input.CurrentZone.GetEntityPosition(e);return Math.Max(Math.Abs(at.x-pos.x),Math.Abs(at.y-pos.y))<=5;}).Select(e=>e.BlueprintName+":"+e.ID).ToArray(),
                lastMessage=MessageLog.GetLast(),waterUnits=Units("WaterTonic"),salveUnits=Units("BurnSalve")});WriteReport();
        }
        IEnumerable<Entity> NearbySteamClouds()=>input.CurrentZone.GetAllEntities().Where(e=>e.BlueprintName=="SteamCloud")
            .Where(e=>{var at=input.CurrentZone.GetEntityPosition(e);return Math.Max(Math.Abs(at.x-targetX),Math.Abs(at.y-targetY))<=1;});
        static string FileHash(string path){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        void FinishCase(){caseCount++;CheckResult(caseName+"_native_route_recorded",true);RequireOrdinaryStats(false);WriteReport();}
        IEnumerator Settled()
        {
            double start=Time.realtimeSinceStartupAsDouble;
            while(State()!="Normal"||input.ZoneRenderer?.WorldFx?.HasBlockingFx==true){Require(Time.realtimeSinceStartupAsDouble-start<8,"native input/FX returns to normal: "+State());yield return null;}
            yield return null;
        }
        (int x,int y) Position()=>input.CurrentZone.GetEntityPosition(input.PlayerEntity);
        string State()=>Field(input,"_inputState").ToString();
        static object Field(object owner,string name)
        {
            Require(owner!=null,"observed owner "+name);var type=owner.GetType();var field=type.GetField(name,Private|BindingFlags.Public);
            if(field!=null)return field.GetValue(owner);var property=type.GetProperty(name,Private|BindingFlags.Public);if(property!=null)return property.GetValue(owner);
            throw new InvalidOperationException("Missing observed field "+type.Name+"."+name);
        }
        IEnumerator Tap(params Key[] keys)
        {
            Require(clock.Elapsed.TotalSeconds<330,"finite native baseline deadline");double began=Time.realtimeSinceStartupAsDouble;
            while(input!=null&&Time.time-(float)Field(input,"_lastMoveTime")<input.MoveRepeatDelay){Require(Time.realtimeSinceStartupAsDouble-began<3,"input rate gate reopens");yield return null;}
            var action=new NativeKeyAction{sequence=nativeKeys.Count,caseName=caseName??"setup",keys=string.Join("+",keys),stateBefore=input==null?null:State(),tickBefore=input?.TurnManager?.TickCount??-1,hpBefore=input?.PlayerEntity?.GetStatValue("Hitpoints")??-1};
            keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return new WaitForSecondsRealtime(.13f);
            action.stateAfter=input==null?null:State();action.tickAfter=input?.TurnManager?.TickCount??-1;action.hpAfter=input?.PlayerEntity?.GetStatValue("Hitpoints")??-1;nativeKeys.Add(action);WriteReport();
        }
        IEnumerator Capture(string label)
        {
            yield return new WaitForSecondsRealtime(.15f);yield return new WaitForEndOfFrame();Directory.CreateDirectory(DirectoryPath);
            string path=Path.Combine(DirectoryPath,label+".png");DensityNativeScreenshot.CaptureToFile(path);Require(File.Exists(path)&&new FileInfo(path).Length>0,"native PNG "+label);screenshots.Add(path);WriteReport();
        }
        static void Require(bool condition,string reason){if(!condition)throw new InvalidOperationException("Thermal baseline precondition: "+reason);}
        void CheckResult(string name,bool passed){if(!passed)failures++;checks.Add(new Check{name=name,passed=passed});Diag.Record("scenario","DensitySteamNativeCase",payload:new{runId=RunId,name,passed});}
        IEnumerator RunSafely(IEnumerator steps)
        {
            var stack=new Stack<IEnumerator>();stack.Push(steps);
            while(stack.Count>0)
            {
                bool moved=false;object current=null;Exception error=null;
                try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}catch(Exception caught){error=caught;}
                if(error!=null){fatal=error.ToString();CheckResult("native_precondition_failed",false);Debug.LogError("[DensitySteamNative] "+error);break;}
                if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}if(current is IEnumerator nested){stack.Push(nested);continue;}yield return current;
            }
            while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();Finish();
        }
        public void SetUnexpectedErrors(int count){unexpected=count;errorsFinalized=true;WriteReport();EmitSummary();}
        public void Abort(string reason){if(Finished)return;StopAllCoroutines();fatal=reason;CheckResult("native_aborted",false);Finish();}
        void Finish(){Cleanup();Finished=true;WriteReport();}
        bool Complete=>Finished&&errorsFinalized&&Failures==0&&caseCount==PlannedCases&&screenshots.Count>=13;
        void WriteReport()
        {
            if(clock==null)return;Directory.CreateDirectory(DirectoryPath);ReportPath=Path.Combine(DirectoryPath,"report.json");
            File.WriteAllText(ReportPath,JsonUtility.ToJson(new Report{runId=RunId,zone=zoneId,checkpointGameId=checkpointGameId,checkpointHash=checkpointHash,cases=caseCount,plannedCases=PlannedCases,failures=Failures,unexpectedErrors=unexpected,complete=Complete,errorsFinalized=errorsFinalized,seconds=clock.Elapsed.TotalSeconds,fatal=fatal,observations=observations.ToArray(),checks=checks.ToArray(),screenshots=screenshots.ToArray(),nativeKeys=nativeKeys.ToArray(),
                canVerify="Real native new-game, read, fire/water, retreat and wait routes create actual reaction-owned steam, then isolate non-burning hot/ambient status admission. Four matched controls measure current source/probe identity, neighbor cooling/wetting and eleven-wait lifetime; ordinary actor HP/stats remain unchanged.",
                cannotVerify="Explicit factory sources/book/tonics, shortened travel, source ambient-temperature normalization, no-steam status removal and a dropped Dagger thermometer (40 degrees, zero ambient decay) are diagnostic fixtures, not natural acquisition or ordinary item tuning. No fabricated SteamEffect is granted: actual spells create it first. This proves scheduling behavior only, not global fire balance, scalding, long-session safety or visual fidelity. Screenshots need independent inspection."},true));
        }
        void EmitSummary(){if(summaryEmitted||clock==null)return;summaryEmitted=true;Diag.Record("scenario","DensitySteamNativeSummary",payload:new{runId=RunId,cases=caseCount,failures=Failures,complete=Complete,errorsFinalized,screenshots=screenshots.Count});}
        void Cleanup()
        {
            if(cleaned)return;cleaned=true;if(keyboard!=null){InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.RemoveDevice(keyboard);}
            if(oldKeyboard!=null&&oldKeyboard.added)oldKeyboard.MakeCurrent();if(oldSettings!=null)InputSystem.settings=oldSettings;if(settings!=null)Destroy(settings);
            if(clock!=null)Application.runInBackground=oldBackground;
            if(input?.CurrentZone!=null)foreach(var e in input.CurrentZone.GetAllEntities().Where(e=>fixtureIds.Contains(e.ID)).ToArray())input.CurrentZone.RemoveEntity(e);
        }
        void OnDestroy(){if(!Finished&&clock!=null){fatal="Play stopped before baseline completion.";CheckResult("native_interrupted",false);Finish();}Cleanup();EmitSummary();if(clock!=null)Diag.SetChannel("scenario",oldScenario);}
        [Serializable] public sealed class NativeKeyAction{public string caseName,keys,stateBefore,stateAfter;public int sequence,tickBefore,tickAfter,hpBefore,hpAfter;}
        [Serializable] public sealed class Check{public string name;public bool passed;}
        [Serializable] public sealed class Observation
        {
            public string caseName,phase,zone,targetId,targetBlueprint,lastMessage,tileState,probeId;public bool probePresent;public float probeTemperature,probeWet;public string[] steamClouds,nearbyCreatures,targetCellOccupants,wholeZoneHostiles;
            public int sequence,playerX,playerY,targetX,targetY,hp,maxHp,strength,agility,toughness,heatResistance,tick,energy,fireCooldown,waterCooldown,targetHp,playerBurnTurns,targetBurnTurns,tileHeat,waterUnits,salveUnits;
            public bool targetPresent;public float temperature,flameTemperature,effectiveFlame,volatility,combustibility,playerBurn,playerWet,targetBurn,targetWet,targetSteam;
        }
        [Serializable] sealed class Report
        {public string runId,zone,checkpointGameId,checkpointHash,fatal,canVerify,cannotVerify;public int cases,plannedCases,failures,unexpectedErrors;public bool complete,errorsFinalized;public double seconds;public Observation[] observations;public Check[] checks;public string[] screenshots;public NativeKeyAction[] nativeKeys;}
    }
}
