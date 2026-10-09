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
    /// <summary>Finite native discovery audit with ordinary bootstrap/travel and explicitly disclosed
    /// transfers to actual generated field/site graphs. No source, item, HP or AI grants.</summary>
    public sealed partial class SpreadDiscoveryNativePlayer:MonoBehaviour
    {
        // Harness policy: ordinary risk, still avoiding adjacent active threats. This is not a safety guarantee.
        const int ThreatClearance=1;
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static readonly (int x,int y)[] Steps={(1,0),(-1,0),(0,1),(0,-1),(1,1),(1,-1),(-1,1),(-1,-1)};
        static readonly string[] MainChecks={"ordinary_start","ordinary_native_sill_entry","real_informant_report","explicit_notes_acquired","native_field_notes","generated_field_lane_transfer","native_finite_gleaning","generated_wayhouse_transfer","native_notice","native_key_acquired","native_front_unlock","native_front_open","native_reward_once","native_reward_equipped","depleted_checkpoint_saved","real_unsaved_step","restored_exact_notes_and_depleted_site","cached_depleted_site_not_refilled","bounded_finish"};
        static readonly string[] OrdinaryChecks={"ordinary_start","ordinary_native_sill_entry","real_informant_report","explicit_notes_acquired","native_field_notes","ordinary_native_field_entry","ordinary_finite_grain_acquired","ordinary_grain_used_once","ordinary_native_return_to_sill","ordinary_notes_retained_on_return","ordinary_used_checkpoint_saved","ordinary_unsaved_step","ordinary_restored_used_grain_and_notes","ordinary_finish"};
        bool _ordinary;string[] RequiredChecks=>_expeditionReport?ExpeditionReportChecks:_tacticalWater?TacticalWaterChecks:_mendleafYard?MendleafChecks:_soddenDistrict?SoddenChecks:_trapJamming?TrapJammingChecks:_predatorDiversion?DiversionChecks:_fieldwork?FieldworkChecks:ConnectedVariant?VariantChecks:Connected?ConnectedChecks:_district?DistrictChecks:_cards?CardChecks:_ordinary?OrdinaryChecks:MainChecks;
        public string RunId{get;}=Guid.NewGuid().ToString("N");
        public bool Finished{get;private set;}public int Failures=>_failures+_unexpectedErrors;public string ReportPath{get;private set;}
        InputHandler _input;Keyboard _keyboard,_oldKeyboard;InputSettings _settings,_oldSettings;
        bool _started,_cleaned,_oldBackground,_errorsFinalized,_summaryEmitted;
        readonly Dictionary<string,bool> _oldChannels=new Dictionary<string,bool>();
        readonly List<string> _audit=new List<string>(),_screenshots=new List<string>(),_notes=new List<string>();
        readonly List<object> _keys=new List<object>(),_observations=new List<object>(),_windows=new List<object>();
        System.Diagnostics.Stopwatch _clock;int _failures,_unexpectedErrors,_localInputs,_mapSteps,_rests,_completedTurns,_pureClock;
        string _fatal,_ownedRoot,_checkpointHash,_siteId,_guardId,_noticeId,_doorId,_sackId,_cacheId,_keyId,_rewardId; Entity _allowedMarker,_guard; bool _avoidGuard,_fighting,_guardAttempt,_guardDamage,_guardLethal;
        DensityCampaignNativeEvidence.ClockReceipt _lastClock;
        string DirectoryPath=>Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/SpreadDiscoveryExpeditions/Native",RunId));
        Entity Player=>_input.PlayerEntity;Zone Zone=>_input.CurrentZone;OverworldZoneManager Manager=>_input.ZoneManager as OverworldZoneManager;
        Cell At=>Zone.GetEntityCell(Player);int Tick=>_input.TurnManager.TickCount;int Energy=>_input.TurnManager.GetEnergy(Player);
        string State=>Field(_input,"_inputState").ToString();
        Entity Owner(string id)=>Zone.GetReadOnlyEntities().SingleOrDefault(e=>e.ID==id);
        public void Initialize(ScenarioContext context,bool ordinary=false,bool cards=false,bool district=false,bool districtCombat=false,bool districtPrepared=false,string connectedBuild=null,bool fieldwork=false)
        {
            _fieldwork=fieldwork;_connectedBuild=connectedBuild;_ordinary=ordinary;_cards=cards;_district=district;_districtCombat=districtCombat;_districtPrepared=districtPrepared;Require(!string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride),"isolated launcher owns saves");_ownedRoot=SaveGameService.SaveRootOverride;_clock=System.Diagnostics.Stopwatch.StartNew();
            foreach(var channel in new[]{"scenario","event","trade","worldmap","furniture","damage","turn","turn-verbose","skill","spell","crop"}.Concat(_tacticalWater?new[]{"ai"}:Array.Empty<string>()).Concat(cards?new[]{"worldgen"}:Array.Empty<string>())){_oldChannels[channel]=Diag.IsChannelEnabled(channel);Diag.SetChannel(channel,true);}
            _oldSettings=InputSystem.settings;_settings=Instantiate(_oldSettings);_settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            _settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings=_settings;_oldBackground=Application.runInBackground;Application.runInBackground=true;_oldKeyboard=Keyboard.current;_keyboard=InputSystem.AddDevice<Keyboard>();StartCoroutine(RunSafely(RunAudit()));
        }
        readonly List<double> _timingFrames=new List<double>(20000);
        readonly List<object> _timings=new List<object>();
        string _timingPhase;double _timingStart;int _timingFirstFrame;bool _timingOverflow;
        object TimingSettings()=>new{zone=_input?.CurrentZone?.ZoneID,screenWidth=Screen.width,screenHeight=Screen.height,cameraRect=_input?.CameraFollow?.GetComponent<Camera>()?.pixelRect.ToString(),vSync=QualitySettings.vSyncCount,targetFrameRate=Application.targetFrameRate,timeScale=Time.timeScale,focused=Application.isFocused,background=Application.runInBackground,lowDetail=Village3DSettings.LowDetail};
        object _timingSettings;
        void BeginTiming(string phase)
        {Require(_timingPhase==null,"nonoverlapping finite timing window");_timingFrames.Clear();_timingOverflow=false;_timingSettings=TimingSettings();_timingPhase=phase;_timingStart=Time.realtimeSinceStartupAsDouble;_timingFirstFrame=Time.frameCount;BeginSoddenProfile();}
        void Update(){SampleSoddenProfile();if(_timingPhase==null)return;if(_timingFrames.Count<20000)_timingFrames.Add(Time.unscaledDeltaTime*1000d);else _timingOverflow=true;if(_variantTiming&&Time.realtimeSinceStartupAsDouble-_timingStart>=60){_variantTiming=false;EndTiming();}}
        void EndTiming()
        {
            EndSoddenProfile();if(_timingPhase==null)return;var samples=_timingFrames.Skip(2).ToArray();var sorted=samples.OrderBy(x=>x).ToArray();
            _timings.Add(new{phase=_timingPhase,seconds=Time.realtimeSinceStartupAsDouble-_timingStart,firstUnityFrame=_timingFirstFrame,lastUnityFrame=Time.frameCount,discardedSetupFrames=Math.Min(2,_timingFrames.Count),frames=samples.Length,meanMs=samples.Length==0?0:samples.Average(),p95Ms=sorted.Length==0?0:sorted[(int)((sorted.Length-1)*.95)],p99Ms=sorted.Length==0?0:sorted[(int)Math.Ceiling((sorted.Length-1)*.99)],maxMs=sorted.Length==0?0:sorted[sorted.Length-1],overflow=_timingOverflow,settingsBefore=_timingSettings,settingsAfter=TimingSettings(),samplesMs=samples,bound="Existing frame-delta sampling pattern; observational only. Native traversal includes paced keys, turn/AI and audit report IO. Short windows are not a steady 60s benchmark, allocation measurement or causal comparison."});_timingPhase=null;
        }
        IEnumerator IdleTiming(string phase){yield return Settled();yield return null;yield return null;BeginTiming(phase);try{double end=Time.realtimeSinceStartupAsDouble+3;while(Time.realtimeSinceStartupAsDouble<end)yield return null;}finally{EndTiming();}}
        IEnumerator MeasureReport()
        {
            var ui=_input.DialogueUI;var map=ui.Tilemap;var cam=ui.PopupCamera;Require(map!=null&&cam!=null&&!((bool)Field(ui,"_revealing")),"actual revealed report tilemap/camera");
            var atlasNames=new HashSet<string>(StringComparer.Ordinal);var rect=cam.pixelRect;int glyphs=0,outside=0;float left=float.MaxValue,right=float.MaxValue,top=float.MaxValue,bottom=float.MaxValue;
            foreach(var cell in map.cellBounds.allPositionsWithin)
            {
                var sprite=map.GetSprite(cell);if(sprite==null)continue;atlasNames.Add(sprite.name);if(!sprite.name.StartsWith("UI_",StringComparison.Ordinal))continue;int code=Convert.ToInt32(sprite.name.Substring(3),16);if(code<=32||code>=127)continue;Require(ReferenceEquals(sprite,CP437TilesetGenerator.GetUiTile((char)code).sprite),"actual dialogue pure-font atlas sprite");glyphs++;
                var b=sprite.bounds;var center=map.GetCellCenterLocal(cell);var matrix=map.orientationMatrix*map.GetTransformMatrix(cell);bool escaped=false;
                for(int i=0;i<4;i++){var p=new Vector3((i&1)==0?b.min.x:b.max.x,(i&2)==0?b.min.y:b.max.y,0);var q=cam.WorldToScreenPoint(map.transform.TransformPoint(center+matrix.MultiplyPoint3x4(p)));left=Mathf.Min(left,q.x-rect.xMin);right=Mathf.Min(right,rect.xMax-q.x);bottom=Mathf.Min(bottom,q.y-rect.yMin);top=Mathf.Min(top,rect.yMax-q.y);if(q.x<rect.xMin-.01f||q.x>rect.xMax+.01f||q.y<rect.yMin-.01f||q.y>rect.yMax+.01f)escaped=true;}if(escaped)outside++;
            }
            var wrapped=(List<string>)Field(ui,"_wrappedTextLines");_observations.Add(new{phase="three_report_live_ui",screenWidth=Screen.width,screenHeight=Screen.height,cameraRect=rect.ToString(),glyphs,outside,left,right,top,bottom,textLines=wrapped.Count,popupRows=(int)Field(ui,"_popupH"),atlasNames=atlasNames.OrderBy(x=>x).ToArray(),choiceCount=ConversationManager.VisibleChoices.Count,choices=ConversationManager.VisibleChoices.Select(c=>c.Text).ToArray(),wrapped});WriteReport();
            yield return Capture("02-real-informant-report");
            Require(glyphs>100&&outside==0&&ConversationManager.VisibleChoices.Count<=10&&NormalizeText(string.Join(" ",wrapped))==NormalizeText(ConversationManager.CurrentText),"actual complete three-report text and all choice rows within current camera");
        }
        IEnumerator RunAudit()
        {
            yield return new WaitForSecondsRealtime(.8f);_input=FindFirstObjectByType<InputHandler>();Require(_input!=null,"actual ordinary bootstrap");
            var boot=(BootMenuController)Field(_input,"_bootMenuController");
            if(Connected)
            {
                if(boot?.IsActive==true)yield return Tap(Key.N);
                if(ConnectedVariant)yield return ConnectedVariantChooseBuild();else yield return ConnectedChooseBuild();_started=true;
                if(_expeditionReport)yield return ExpeditionReportJourney();else if(_tacticalWater)yield return TacticalWaterJourney();else if(_mendleafYard)yield return MendleafJourney();else if(_soddenDistrict)yield return SoddenJourney();else if(_trapJamming)yield return TrapJammingJourney();else if(_predatorDiversion)yield return PredatorDiversionJourney();else if(_fieldwork)yield return FieldworkJourney();else if(ConnectedVariant)yield return ConnectedVariantJourney();else yield return ConnectedJourney();yield break;
            }
            Require(boot?.IsActive==true,"ordinary N menu");yield return Tap(Key.N);yield return Settled();_started=true;
            Check("ordinary_start",(Manager.WorldSeed==64||_district&&Manager.WorldSeed==1729)&&ReferenceGladePlan.IsActive(Zone)&&Player.GetStatValue("Hitpoints")==40&&Player.GetStat("Hitpoints").Max==40&&Player.GetStatValue("Level")==1&&TradeSystem.GetDrams(Player)==50&&Player.GetStatValue("Strength")==18&&Player.GetStatValue("Agility")==18&&Player.GetPart<InventoryPart>().Objects.Count(e=>e.BlueprintName=="Dagger")==1&&Player.GetPart<InventoryPart>().Objects.Where(e=>e.BlueprintName=="HealingTonic").Sum(Units)==2&&!DevMode.Enabled&&!Player.HasPart<BitLockerPart>());
            yield return Capture("01-ordinary-start");if(_district){yield return DistrictJourney();yield break;}if(_cards){yield return RunCards();yield break;}yield return TravelSurface(WorldMap.StartingZoneID);Check("ordinary_native_sill_entry",Zone.ZoneID==WorldMap.StartingZoneID&&_mapSteps>0);
            yield return ApproachInformant();var speaker=_informant;Require(speaker!=null,"actual adjacent current informant");string node=speaker.GetPart<ConversationPart>().ConversationID=="Scribe_1"?"RegionOverview":"Rumors";
            int tick=Tick,energy=Energy,hp=Player.GetStatValue("Hitpoints");var caches=Manager.CachedZones.Keys.OrderBy(x=>x).ToArray();var families=new List<string>();
            yield return Chat(speaker);yield return Choice(c=>c.Target==node);
            foreach(var choice in ConversationManager.VisibleChoices)foreach(var a in choice.Actions??new List<CavesOfOoo.Data.ConversationParam>())if(a.Key==SpreadDiscoveryReports.ActionName)families.Add(a.Value.Split('|')[0]);
            Require(families.Count==3&&families.Distinct().Count()==3&&families.Contains("turnbank-wayhouse"),"three actual independent current report offers");
            _notes.Add("NATIVE REPORT "+ConversationManager.CurrentText);yield return MeasureReport();Check("real_informant_report",Tick==tick&&Energy==energy&&Player.GetStatValue("Hitpoints")==hp&&Manager.CachedZones.Keys.OrderBy(x=>x).SequenceEqual(caches)&&NoteSignature()=="");
            foreach(var family in families)
            {
                if(!ConversationManager.IsActive||ConversationManager.CurrentNode?.ID!=node){yield return CloseNormal();yield return Chat(speaker);yield return Choice(c=>c.Target==node);}
                // SelectChoice rebuilds offers, so match the family on the actual fresh action, never reuse its revision token.
                yield return Choice(c=>c.Actions?.Any(a=>a.Key==SpreadDiscoveryReports.ActionName&&a.Value.StartsWith(family+"|",StringComparison.Ordinal))==true);
            }
            Check("explicit_notes_acquired",CurrentDiscoveryNotes().Count==families.Count&&families.All(f=>Player.Properties.ContainsKey(SpreadDiscoveryNotes.Prefix+f)||Player.Properties.Keys.Any(k=>k.StartsWith(SpreadDiscoveryNotes.ExpeditionPrefix+Manager.Exploration.WorldKey+":",StringComparison.Ordinal)&&k.EndsWith(":"+f,StringComparison.Ordinal)))&&Tick==tick&&Energy==energy&&Player.GetStatValue("Hitpoints")==hp&&Manager.CachedZones.Keys.OrderBy(x=>x).SequenceEqual(caches));yield return CloseNormal();
            yield return Tap(Key.Q);yield return Tap(Key.Tab);Require(_input.QuestLogUI.IsOpen&&_input.QuestLogUI.NotesVisible,"native Q/Tab notes");var lines=((List<string>)Field(_input.QuestLogUI,"_noteLines")).ToArray();_notes.Add("NATIVE NOTES "+string.Join("\n",lines));
            int pages=Math.Max(1,(lines.Length+33)/34);for(int i=0;i<pages;i++){Require(_input.QuestLogUI.NotesPage==i,"actual notes page index");yield return Capture("03-field-notes-"+i);if(i+1<pages)yield return Tap(Key.RightArrow);}
            Check("native_field_notes",CurrentDiscoveryNotes().Count==families.Count&&CurrentDiscoveryNotes().All(s=>NormalizeText(string.Join(" ",lines)).Contains(NormalizeText(s)))&&Tick==tick&&Energy==energy&&Player.GetStatValue("Hitpoints")==hp);yield return CloseNormal();
            if(_ordinary){yield return OrdinaryJourney();yield break;}
            yield return FieldGleaning();
            _siteId=Manager.Wayhouse?.ZoneID;Require(!string.IsNullOrEmpty(_siteId),"actual selected wayhouse address");var site=Manager.GetZone(_siteId);Require(site!=null&&ReferenceEquals(Manager.CachedZones[_siteId],site),"actual generated cached site");
            RememberSite(site);_guard=site.GetReadOnlyEntities().Single(e=>e.ID==_guardId);_avoidGuard=true;
            var notice=site.GetReadOnlyEntities().Single(e=>e.ID==_noticeId);var n=site.GetEntityCell(notice);var arrival=site.GetCell(n.X+1,n.Y);Require(Safe(site,arrival,ThreatClearance),"actual generated notice reading approach");
            yield return Transfer(site,arrival,"unchanged generated wayhouse");Check("generated_wayhouse_transfer",Zone==site&&Manager.Wayhouse.ZoneID==_siteId&&CurrentSiteIntact());
            tick=Tick;energy=Energy;hp=Player.GetStatValue("Hitpoints");yield return WorldAction(Owner(_noticeId),"Examine");yield return ReadPages("05-wayhouse-notice");
            Check("native_notice",_readerText.Contains("Turnbank")&&_readerText.Contains("key")&&Tick==tick&&Energy==energy&&Player.GetStatValue("Hitpoints")==hp);yield return CloseNormal();yield return Capture("06-generated-wayhouse-approach");yield return IdleTiming("wayhouse-notice-idle");
            yield return RearBranch();_avoidGuard=false;_guard=Owner(_guardId);
            var dagger=Player.GetPart<InventoryPart>().Objects.Concat(Player.GetPart<InventoryPart>().EquippedItems.Values).Distinct().Single(e=>e.BlueprintName=="Dagger");yield return ItemAction(dagger,"equip_auto");yield return CloseNormal();Require(dagger.GetPart<PhysicsPart>().Equipped==Player,"actual original dagger equipped");
            // The guard is unchanged. Front combat is ordinary native input, not an invented sleeping state.
            yield return FightGuard();yield return Capture("08-front-guard-aftermath");
            yield return Approach(Owner(_sackId),100);yield return Paid(TakeSole(Owner(_sackId),_keyId),"local","native-key-pickup");Check("native_key_acquired",Owns(Player,OwnerOrCarried(_keyId))&&Owner(_sackId).GetPart<ContainerPart>().Contents.Count==0);yield return Capture("09-real-key-acquired");
            var door=Owner(_doorId);var dc=Zone.GetEntityCell(door);Require(door.GetPart<DoorPart>().QuarterTurns==1,"actual generated east-west door orientation");yield return WalkTo(Zone.GetCell(dc.X-1,dc.Y),100);var before=At;Require(door.GetPart<DoorPart>().IsClosed&&door.GetPart<LockPart>().IsLocked,"original locked front door");
            yield return Paid(Tap(Direction(dc.X-At.X,dc.Y-At.Y)),"local","native-key-bump-unlock");Check("native_front_unlock",At==before&&!door.GetPart<LockPart>().IsLocked&&door.GetPart<DoorPart>().IsClosed&&Owns(Player,OwnerOrCarried(_keyId)));yield return Capture("10-front-unlocked");
            yield return Paid(WorldAction(door,DoorPart.OpenCommand),"local","native-front-open");Check("native_front_open",!door.GetPart<DoorPart>().IsClosed&&!door.GetPart<LockPart>().IsLocked);yield return Capture("11-front-open");yield return StepTo(dc.X,dc.Y);Require(At==dc,"actual native front threshold crossed");
            yield return Approach(Owner(_cacheId),100);yield return Paid(TakeSole(Owner(_cacheId),_rewardId),"local","native-reward-pickup");
            Check("native_reward_once",Owns(Player,OwnerOrCarried(_rewardId))&&Owner(_cacheId).GetPart<ContainerPart>().Contents.Count==0&&CountGraphId(_rewardId)==1);yield return Capture("12-real-reward-acquired");
            yield return ItemAction(OwnerOrCarried(_rewardId),"equip_auto");yield return CloseNormal();Check("native_reward_equipped",OwnerOrCarried(_rewardId)?.GetPart<PhysicsPart>()?.Equipped==Player&&Player.GetPart<InventoryPart>().EquippedItems.Values.Any(e=>e.ID==_rewardId));yield return Capture("13-real-reward-equipped");
            yield return FinalCheckpoint();Check("cached_depleted_site_not_refilled",ReferenceEquals(Manager.GetZone(_siteId),Zone)&&Owner(_cacheId).GetPart<ContainerPart>().Contents.Count==0&&Owner(_sackId).GetPart<ContainerPart>().Contents.Count==0&&CountGraphId(_rewardId)==1&&CountGraphId(_keyId)==1);
            Check("bounded_finish",Player.GetStatValue("Hitpoints")>10&&!DebugInvincibility.IsEnabled(Player)&&!DevMode.Enabled&&!Player.HasPart<BitLockerPart>()&&State=="Normal");yield return Capture("15-finished-depleted-site");
        }
        Entity _informant;
        IEnumerator ApproachInformant()
        {
            Entity selected=null;int waits=0;
            for(int input=0;input<126;input++)
            {
                var owners=Zone.GetReadOnlyEntities().Where(e=>(e.GetPart<ConversationPart>()?.ConversationID=="Innkeeper_1"||e.GetPart<ConversationPart>()?.ConversationID=="Scribe_1")&&e.GetStatValue("Hitpoints")>0&&!CombatSystem.IsDeathHandled(e)&&Zone.GetEntityCell(e)!=null)
                    .OrderBy(e=>e.GetPart<ConversationPart>().ConversationID=="Innkeeper_1"?0:1).ThenBy(e=>e.ID,StringComparer.Ordinal).ToArray();
                var adjacent=owners.FirstOrDefault(e=>SpatialQuery.Distance(Zone,Player,e)<=1);if(adjacent!=null){_informant=adjacent;yield break;}
                List<(int x,int y)> path=null;Entity choice=null;
                foreach(var owner in owners){var route=PathTo(c=>SpatialQuery.DistanceToCell(Zone,owner,c.X,c.Y)==1);if(route!=null&&route.Count>0){choice=owner;path=route;break;}}
                if(path!=null)
                {
                    if(!ReferenceEquals(selected,choice))_notes.Add("CURRENT INFORMANT ROUTE "+choice.ID+":"+choice.GetPart<ConversationPart>().ConversationID+" at "+Zone.GetEntityPosition(choice));selected=choice;
                    yield return StepTo(path[0].x,path[0].y);continue;
                }
                if(waits>=6){if(selected!=null)RouteDiagnostic("current informant "+selected.ID,c=>SpatialQuery.DistanceToCell(Zone,selected,c.X,c.Y)==1);throw new InvalidOperationException("No current informant route after six ordinary waits; no teleport or actor clearing.");}
                Require(owners.Length>0&&Safe(Zone,At,ThreatClearance),"current safe cell before one ordinary NPC-clearance wait");waits++;_notes.Add("INFORMANT TRANSIENT ROUTE WAIT "+waits+" at "+At.X+","+At.Y);yield return Paid(Tap(Key.Period),"local","ordinary-informant-clearance-wait");
            }
            throw new InvalidOperationException("Finite126-iteration ordinary informant approach exhausted.");
        }
        IEnumerator OrdinaryJourney()
        {
            string destination="Overworld.11.8.0",notes=NoteSignature();Require(SpreadCompositionPlan.IsWildernessZone(destination)&&FormationSelector.For(BiomeType.Spread,destination)==Formation.FieldStrips,"actual ordinary FieldStrips source address");
            // TravelSurface is the only transition in this mode. Do not call the main route's Transfer helper.
            yield return TravelSurface(destination);var field=Zone;var plan=SpreadCompositionPlan.Create(destination,Manager.WorldSeed);
            Check("ordinary_native_field_entry",Zone.ZoneID==destination&&ReferenceEquals(Manager.CachedZones[destination],field)&&NoteSignature()==notes);
            var row=field.GetReadOnlyEntities().Where(e=>e.BlueprintName=="RipeCropRow"&&e.GetPart<FieldHarvestPart>()?.Harvested==false).OrderBy(e=>e.ID,StringComparer.Ordinal).FirstOrDefault(e=>PathTo(c=>SpatialQuery.DistanceToCell(field,e,c.X,c.Y)==1)!=null);
            Require(row!=null,"actual naturally generated reachable ripe row after ordinary entry");string rowId=row.ID;var rowPosition=field.GetEntityPosition(row);yield return Capture("ordinary-04-real-field-entry");yield return IdleTiming("ordinary-field-entry-idle");
            BeginTiming("ordinary-field-native-approach");try{yield return Approach(row,120);}finally{EndTiming();}
            var presenter=_input.ZoneRenderer.SpawnRing3D;var catalog=(SpawnRing3DCatalog)Field(presenter,"definition");var beforeRecipe=SpawnRing3DRecipes.Resolve(field,row,catalog);Require(presenter.TryGetApprovedStyle(row,out var proof)&&beforeRecipe.ModelId.StartsWith("spread-environment-grain-",StringComparison.Ordinal),"actual approved ripe field owner");
            int tick=Tick,energy=Energy;yield return WorldAction(row,"Examine");yield return ReadPages("ordinary-05-real-grain-reader");Require(Tick==tick&&Energy==energy,"free real grain examination");yield return CloseNormal();
            var oldGrainIds=Player.GetPart<InventoryPart>().Objects.Where(e=>e.BlueprintName=="Emberwheat").Select(e=>e.ID).ToArray();int beforePacked=PackedGrain();Require(beforePacked==0,"ordinary original starter has no prior grain");
            yield return Paid(WorldAction(row,"Harvest"),"local","ordinary-native-finite-grain-harvest");var grain=Player.GetPart<InventoryPart>().Objects.SingleOrDefault(e=>e.BlueprintName=="Emberwheat"&&!oldGrainIds.Contains(e.ID));Require(grain!=null,"actual newly harvested carried grain");string grainId=grain.ID;
            var afterRecipe=SpawnRing3DRecipes.Resolve(field,row,catalog);Check("ordinary_finite_grain_acquired",row.ID==rowId&&field.GetEntityPosition(row)==rowPosition&&row.GetPart<FieldHarvestPart>().Harvested&&row.GetPart<FieldHarvestPart>().YieldCount==1&&PackedGrain()==beforePacked+1&&Owns(Player,grain)&&afterRecipe.ModelId==beforeRecipe.ModelId.Replace("grain-","stubble-")&&presenter.TryGetApprovedStyle(row,out proof));yield return Capture("ordinary-06-earned-grain-and-stubble");
            Require(grain.GetPart<FoodPart>()?.Healing=="2d4"&&Units(grain)==1,"real edible authored grain unit");tick=Tick;energy=Energy;int hp=Player.GetStatValue("Hitpoints");string marker=Mark("ordinary-native-grain-eat");yield return ItemAction(grain,"Eat");yield return CloseNormal();var rows=Window(marker);
            bool consumed=rows.Any(r=>r.Category=="event"&&r.Kind=="ItemUnitConsumed"&&r.ActorId==Player.ID&&r.TargetId==grainId),eaten=rows.Any(r=>r.Category=="event"&&r.Kind=="FoodEaten"&&r.ActorId==Player.ID&&r.TargetId==grainId);
            Check("ordinary_grain_used_once",consumed&&eaten&&PackedGrain()==beforePacked&&!Owns(Player,grain)&&CountGraphId(grainId)==0&&Tick==tick&&Energy==energy&&Player.GetStatValue("Hitpoints")>=hp&&Player.GetStatValue("Hitpoints")<=Player.GetStat("Hitpoints").Max);
            _windows.Add(new{label="ordinary-native-grain-eat",marker,rows,beforeHp=hp,afterHp=Player.GetStatValue("Hitpoints"),beforeTick=tick,afterTick=Tick,beforeEnergy=energy,afterEnergy=Energy,bound="One actual earned grain consumed. No healing benefit is claimed when HP was already full."});yield return Capture("ordinary-07-earned-grain-used");
            yield return TravelSurface(WorldMap.StartingZoneID);Check("ordinary_native_return_to_sill",Zone.ZoneID==WorldMap.StartingZoneID&&NoteSignature()==notes&&Manager.CachedZones.TryGetValue(destination,out var cached)&&ReferenceEquals(cached,field)&&field.GetReadOnlyEntities().SingleOrDefault(e=>e.ID==rowId)?.GetPart<FieldHarvestPart>()?.Harvested==true&&CountGraphId(grainId)==0);yield return Capture("ordinary-08-real-sill-return");
            tick=Tick;energy=Energy;yield return Tap(Key.Q);yield return Tap(Key.Tab);Require(_input.QuestLogUI.IsOpen&&_input.QuestLogUI.NotesVisible,"actual returned field notes UI");var noteLines=(List<string>)Field(_input.QuestLogUI,"_noteLines");Check("ordinary_notes_retained_on_return",NoteSignature()==notes&&CurrentDiscoveryNotes().Count==3&&CurrentDiscoveryNotes().All(s=>NormalizeText(string.Join(" ",noteLines)).Contains(NormalizeText(s)))&&Tick==tick&&Energy==energy);yield return Capture("ordinary-09-returned-notes");yield return CloseNormal();
            yield return OrdinaryCheckpoint(destination,rowId,rowPosition,grainId,notes);Check("ordinary_finish",State=="Normal"&&Player.GetStatValue("Hitpoints")>10&&!DebugInvincibility.IsEnabled(Player)&&!DevMode.Enabled&&!Player.HasPart<BitLockerPart>());yield return Capture("ordinary-11-finished-real-round-trip");
        }
        int PackedGrain()=>Player.GetPart<InventoryPart>().Objects.Where(e=>e.BlueprintName=="Emberwheat"&&e.GetPart<PhysicsPart>()?.InInventory==Player).Sum(Units);
        IEnumerator OrdinaryCheckpoint(string fieldId,string rowId,(int x,int y) rowPosition,string grainId,string notes)
        {
            var actor=Player;var zone=Zone;string id=Player.ID,stats=Stats(Player),gear=Gear(Player);int tick=Tick,energy=Energy,world=WorldClock.CurrentTick,x=At.X,y=At.Y,drams=TradeSystem.GetDrams(Player);var oldField=Manager.CachedZones[fieldId];
            string file=SaveFile(),old=HashFile(file);long serial=MessageLog.NextSerialValue;yield return Tap(Key.F5);yield return Settled();_checkpointHash=HashFile(file);Check("ordinary_used_checkpoint_saved",old!=_checkpointHash&&MessageLog.NextSerialValue>serial&&MessageLog.GetLast()=="Game saved."&&SaveGameService.GetSaveInfo("Quick").ActiveZoneID==WorldMap.StartingZoneID);
            var step=Steps.Select(d=>Zone.GetCell(x+d.x,y+d.y)).FirstOrDefault(c=>Safe(Zone,c,ThreatClearance)&&Zone.CanPlaceFootprint(Player,c.X,c.Y));Require(step!=null,"one actual unsaved safe Sill step");yield return StepTo(step.X,step.Y);Check("ordinary_unsaved_step",(At.X!=x||At.Y!=y)&&Tick>tick&&HashFile(file)==_checkpointHash);
            yield return Reload(actor);Zone restoredField;Require(Manager.CachedZones.TryGetValue(fieldId,out restoredField),"actual saved field graph restored");var restoredRow=restoredField.GetReadOnlyEntities().SingleOrDefault(e=>e.ID==rowId);
            Check("ordinary_restored_used_grain_and_notes",!ReferenceEquals(Zone,zone)&&!ReferenceEquals(restoredField,oldField)&&Player.ID==id&&Zone.ZoneID==WorldMap.StartingZoneID&&At.X==x&&At.Y==y&&Stats(Player)==stats&&Gear(Player)==gear&&NoteSignature()==notes&&Tick==tick&&Energy==energy&&WorldClock.CurrentTick==world&&TradeSystem.GetDrams(Player)==drams&&HashFile(file)==_checkpointHash&&CountGraphId(grainId)==0&&restoredRow?.GetPart<FieldHarvestPart>()?.Harvested==true&&restoredField.GetEntityPosition(restoredRow)==rowPosition);
            yield return Capture("ordinary-10-restored-used-grain-and-notes");
        }
        string _readerText;
        IEnumerator ReadPages(string label)
        {
            Require(State=="AnnouncementOpen"&&_input.AnnouncementUI.IsOpen,"actual full native reader");var lines=new List<string>();int count=_input.AnnouncementUI.PageCount;Require(count>0&&count<=12,"bounded real reader pages");
            for(int i=0;i<count;i++){Require((int)Field(_input.AnnouncementUI,"_pageIndex")==i,"actual announcement page index");lines.AddRange(_input.AnnouncementUI.VisibleLines);if(i==0||i+1==count)yield return Capture(label+"-page-"+i);if(i+1<count)yield return Tap(Key.RightArrow);}
            _readerText=string.Join("\n",lines);_notes.Add("READER "+label+" "+_readerText);
        }
        static string NormalizeText(string value)=>string.Join(" ",value.Split((char[])null,StringSplitOptions.RemoveEmptyEntries));
        IReadOnlyList<string> CurrentDiscoveryNotes()=>SpreadDiscoveryNotes.Read(Player,Manager.Exploration?.WorldKey);
        string NoteSignature()=>string.Join("|",Player.Properties.Where(p=>p.Key.StartsWith(SpreadDiscoveryNotes.Prefix,StringComparison.Ordinal)||p.Key.StartsWith(SpreadDiscoveryNotes.ExpeditionPrefix,StringComparison.Ordinal)).OrderBy(p=>p.Key,StringComparer.Ordinal).Select(p=>p.Key+"="+p.Value));
        static int Units(Entity e)=>e.GetPart<StackerPart>()?.StackCount??1;
        int GrainCount()=>Player.GetPart<InventoryPart>().Objects.Concat(Zone.GetReadOnlyEntities()).Where(e=>e.BlueprintName=="Emberwheat").Distinct().Sum(Units);
        IEnumerator Transfer(Zone destination,Cell at,string label)
        {
            Require(destination!=null&&at!=null&&ReferenceEquals(Manager.CachedZones[destination.ZoneID],destination)&&Safe(destination,at,ThreatClearance),"managed actual safe transfer destination");
            var actor=Player;string stats=Stats(actor),gear=Gear(actor),notes=NoteSignature();int tick=Tick,energy=Energy,world=WorldClock.CurrentTick,drams=TradeSystem.GetDrams(Player);string graph=SourceGraph(destination);
            Require(Zone.TryTransferEntityTo(actor,destination,at.X,at.Y),"disclosed original-player-only transfer");typeof(InputHandler).GetMethod("HandleZoneTransition",Private).Invoke(_input,new object[]{new ZoneTransitionResult{Success=true,NewZone=destination,NewPlayerX=at.X,NewPlayerY=at.Y}});
            _input.CameraFollow?.SnapToPlayer();ZoneRenderHooks.MarkFullDirty("SpreadDiscoveryDisclosedTransfer");yield return Settled();yield return Tap(Key.L);yield return Tap(Key.Escape);yield return Settled();
            Require(ReferenceEquals(Player,actor)&&Zone==destination&&At.X==at.X&&At.Y==at.Y&&Stats(actor)==stats&&Gear(actor)==gear&&NoteSignature()==notes&&Tick==tick&&Energy==energy&&WorldClock.CurrentTick==world&&TradeSystem.GetDrams(Player)==drams&&SourceGraph(destination)==graph,"transfer preserves original player and actual source IDs/positions/native contents");
            _notes.Add("DISCLOSED TRANSFER: original player only to "+label+" "+destination.ZoneID+"@"+at.X+","+at.Y+"; actual Manager.GetZone graph; no source, HP, AI, inventory or reward edits.");Observe("disclosed-transfer");
        }
        string SourceGraph(Zone z)=>string.Join("|",z.GetReadOnlyEntities().Where(e=>!e.HasTag("Player")).OrderBy(e=>e.ID,StringComparer.Ordinal).Select(e=>e.ID+":"+e.BlueprintName+":"+z.GetEntityPosition(e)+":"+e.GetStatValue("Hitpoints")+":"+e.GetPart<RenderPart>()?.RenderString+":"+string.Join(",",e.GetPart<ContainerPart>()?.Contents.Select(c=>c.ID+":"+c.BlueprintName+":"+c.GetPart<PhysicsPart>()?.InInventory?.ID)??Enumerable.Empty<string>())));
        IEnumerator FieldGleaning()
        {
            // Bounded selection reads real generated sources; it never injects a row or changes formation.
            Entity row=null;Zone field=null;Cell lane=null;SpreadCompositionPlan plan=null;
            var ids=new List<string>{"Overworld.11.8.0"};for(int y=0;y<WorldMap.Height;y++)for(int x=0;x<WorldMap.Width;x++){string id=WorldMap.ToZoneID(x,y);if(!ids.Contains(id)&&SpreadCompositionPlan.IsWildernessZone(id)&&FormationSelector.For(BiomeType.Spread,id)==Formation.FieldStrips)ids.Add(id);}
            foreach(string id in ids.Take(8))
            {
                if(!SpreadCompositionPlan.IsWildernessZone(id)||FormationSelector.For(BiomeType.Spread,id)!=Formation.FieldStrips)continue;
                var z=Manager.GetZone(id);var p=SpreadCompositionPlan.Create(id,Manager.WorldSeed);foreach(var candidate in z.GetReadOnlyEntities().Where(e=>e.BlueprintName=="RipeCropRow"&&e.GetPart<FieldHarvestPart>()?.Harvested==false).OrderBy(e=>e.ID,StringComparer.Ordinal))
                {var rc=z.GetEntityCell(candidate);for(int y=0;y<Zone.Height&&lane==null;y++)for(int x=0;x<Zone.Width&&lane==null;x++){var c=z.GetCell(x,y);int distance=Math.Max(Math.Abs(x-rc.X),Math.Abs(y-rc.Y));if(p.IsApproach(x,y)&&distance>=2&&distance<=7&&Safe(z,c,ThreatClearance)&&AIHelpers.HasLineOfSight(z,x,y,rc.X,rc.Y)){row=candidate;field=z;lane=c;plan=p;}}if(lane!=null)break;}if(lane!=null)break;
            }
            Require(row!=null&&lane!=null,"actual generated ripe row visible from working lane");yield return Transfer(field,lane,"generated FieldStrips working lane");
            var rc0=Zone.GetEntityCell(row);var presenter=_input.ZoneRenderer.SpawnRing3D;var before=SpawnRing3DRecipes.Resolve(Zone,row,(SpawnRing3DCatalog)Field(presenter,"definition"));Require(presenter!=null&&presenter.TryGetApprovedStyle(row,out var style)&&before.ModelId.StartsWith("spread-environment-grain-",StringComparison.Ordinal),"actual approved current grain geometry");
            _observations.Add(new{phase="generated_field_lane",zone=Zone.ZoneID,row=row.ID,x=rc0.X,y=rc0.Y,laneX=lane.X,laneY=lane.Y,condition=plan.Condition,distance=Math.Max(Math.Abs(lane.X-rc0.X),Math.Abs(lane.Y-rc0.Y)),model=before.ModelId});
            Check("generated_field_lane_transfer",plan.IsApproach(At.X,At.Y)&&rc0.IsVisible);yield return Capture("04a-real-working-lane-grain");yield return IdleTiming("field-lane-idle");BeginTiming("field-lane-native-approach");try{yield return Approach(row,40);}finally{EndTiming();}int tick=Tick,energy=Energy;yield return WorldAction(row,"Examine");yield return ReadPages("04b-real-grain-examine");Require(Tick==tick&&Energy==energy,"free current grain reader");yield return CloseNormal();
            int units=GrainCount(),yield=row.GetPart<FieldHarvestPart>().YieldCount;string id0=row.ID;yield return Paid(WorldAction(row,"Harvest"),"local","native-finite-grain-harvest");
            var after=SpawnRing3DRecipes.Resolve(Zone,row,(SpawnRing3DCatalog)Field(presenter,"definition"));Check("native_finite_gleaning",row.ID==id0&&Zone.GetEntityCell(row)==rc0&&row.GetPart<FieldHarvestPart>().Harvested&&GrainCount()==units+yield&&after.ModelId==before.ModelId.Replace("grain-","stubble-")&&presenter.TryGetApprovedStyle(row,out style));yield return Capture("04c-real-cut-stubble");
        }
        Entity Role(Zone z,string role)=>z.GetReadOnlyEntities().Single(e=>e.GetProperty(SpreadWayhouseBuilder.RoleKey)==role);
        void RememberSite(Zone z)
        {
            _guardId=Role(z,"guard").ID;_noticeId=Role(z,"notice").ID;_doorId=Role(z,"door").ID;_sackId=Role(z,"key-sack").ID;_cacheId=Role(z,"cache").ID;
            _keyId=Role(z,"key-sack").GetPart<ContainerPart>().Contents.Single().ID;_rewardId=Role(z,"cache").GetPart<ContainerPart>().Contents.Single().ID;
            Require(new[]{_guardId,_noticeId,_doorId,_sackId,_cacheId,_keyId,_rewardId}.All(id=>!string.IsNullOrEmpty(id))&&new[]{_guardId,_noticeId,_doorId,_sackId,_cacheId,_keyId,_rewardId}.Distinct().Count()==7,"exact distinct generated packet IDs");
            var door=Role(z,"door");var sack=Role(z,"key-sack");var cache=Role(z,"cache");var key=sack.GetPart<ContainerPart>().Contents.Single();var reward=cache.GetPart<ContainerPart>().Contents.Single();
            Require(Role(z,"guard").BlueprintName=="MarlbackScrabbler"&&Role(z,"notice").BlueprintName=="Signpost"&&door.BlueprintName=="VillageDoor"&&sack.BlueprintName=="Sack"&&cache.BlueprintName=="Crate"
                &&key.BlueprintName=="IronKey"&&key.GetPart<KeyPart>()?.KeyId==door.GetPart<LockPart>()?.KeyId&&!string.IsNullOrEmpty(key.GetPart<KeyPart>()?.KeyId)&&key.GetPart<PhysicsPart>()?.InInventory==sack
                &&reward.BlueprintName=="Buckler"&&Units(reward)==1&&reward.GetPart<PhysicsPart>()?.InInventory==cache,"actual matching iron key, authored Buckler and native source families");
        }
        bool CurrentSiteIntact()=>Owner(_guardId)?.GetStatValue("Hitpoints")==15&&Owner(_doorId)?.GetPart<LockPart>()?.IsLocked==true&&Owner(_sackId)?.GetPart<ContainerPart>()?.Contents.SingleOrDefault()?.ID==_keyId&&Owner(_cacheId)?.GetPart<ContainerPart>()?.Contents.SingleOrDefault()?.ID==_rewardId;
        string SiteSignature()=>string.Join("|",Zone.GetReadOnlyEntities().Where(e=>e.GetProperty(SpreadWayhouseBuilder.RoleKey)!=null&&e.GetProperty(SpreadWayhouseBuilder.RoleKey)!="").OrderBy(e=>e.ID,StringComparer.Ordinal).Select(e=>e.ID+":"+e.BlueprintName+":"+e.GetProperty(SpreadWayhouseBuilder.RoleKey)+":"+Zone.GetEntityPosition(e)+":"+e.GetStatValue("Hitpoints")+":"+e.GetPart<DoorPart>()?.IsClosed+":"+e.GetPart<DoorPart>()?.QuarterTurns+":"+e.GetPart<LockPart>()?.IsLocked+":"+string.Join(",",e.GetPart<ContainerPart>()?.Contents.Select(c=>c.ID+":"+c.BlueprintName+":"+c.GetPart<PhysicsPart>()?.InInventory?.ID)??Enumerable.Empty<string>())));
        int CountGraphId(string id)
        {
            var seen=new HashSet<Entity>();var pending=new Queue<Entity>(Manager.CachedZones.Values.SelectMany(z=>z.GetReadOnlyEntities()));
            while(pending.Count>0){var e=pending.Dequeue();if(e==null||!seen.Add(e))continue;foreach(var c in e.GetPart<ContainerPart>()?.Contents??new List<Entity>())pending.Enqueue(c);foreach(var c in e.GetPart<InventoryPart>()?.Objects??new List<Entity>())pending.Enqueue(c);foreach(var c in e.GetPart<InventoryPart>()?.EquippedItems.Values.Distinct()??Enumerable.Empty<Entity>())pending.Enqueue(c);}
            return seen.Count(e=>e.ID==id);
        }
        string SaveFile(){var info=SaveGameService.GetSaveInfo("Quick");Require(info!=null,"real initial save metadata");return Path.Combine(_ownedRoot,info.GameID,"Quick.sav.gz");}
        IEnumerator Reload(Entity oldPlayer)
        {yield return Tap(Key.F6);double began=Time.realtimeSinceStartupAsDouble;while(ReferenceEquals(Player,oldPlayer)){Require(Time.realtimeSinceStartupAsDouble-began<8,"native F6 replaces player graph");yield return null;}yield return Settled();_guard=string.IsNullOrEmpty(_guardId)?null:Owner(_guardId);}
        IEnumerator RearBranch()
        {
            string stats=Stats(Player),gear=Gear(Player),notes=NoteSignature(),site=SiteSignature(),id=Player.ID;int tick=Tick,energy=Energy,world=WorldClock.CurrentTick,x=At.X,y=At.Y,drams=TradeSystem.GetDrams(Player);var original=Player;var originalZone=Zone;
            string file=SaveFile(),hash=HashFile(file);yield return Tap(Key.F5);yield return Settled();Require(HashFile(file)!=hash&&MessageLog.GetLast()=="Game saved.","actual rear-branch baseline checkpoint");hash=HashFile(file);
            var cache=Owner(_cacheId);var path=PathTo(c=>SpatialQuery.DistanceToCell(Zone,cache,c.X,c.Y)==1);
            if(path==null){_notes.Add("REAR NOT VERIFIED: no current safe path with real guard sight exclusion; main front route continues after exact native reload.");}
            else
            {
                int hp=Player.GetStatValue("Hitpoints"),steps=0;bool arrived=false;
                BeginTiming("wayhouse-native-rear-traversal");
                while(steps++<90&&_guard.GetPart<BrainPart>()?.Target!=Player&&Player.GetStatValue("Hitpoints")==hp)
                {
                    if(SpatialQuery.Distance(Zone,Player,cache)<=1){arrived=true;break;}path=PathTo(c=>SpatialQuery.DistanceToCell(Zone,cache,c.X,c.Y)==1);if(path==null||path.Count==0)break;yield return StepTo(path[0].x,path[0].y);
                }
                EndTiming();bool observed=arrived&&_guard.GetStatValue("Hitpoints")==15&&!CombatSystem.IsDeathHandled(_guard)&&_guard.GetPart<BrainPart>()?.Target!=Player&&Player.GetStatValue("Hitpoints")==hp&&Owner(_doorId).GetPart<LockPart>().IsLocked&&Owner(_cacheId).GetPart<ContainerPart>().Contents.Single().ID==_rewardId;
                _observations.Add(new{phase="rear_live_guard_path",observed,steps,guard=_guard.ID,guardTarget=_guard.GetPart<BrainPart>()?.Target?.ID,guardPosition=Zone.GetEntityPosition(_guard),player=Zone.GetEntityPosition(Player),hp=Player.GetStatValue("Hitpoints"),notes="Normal guard AI; no sleeping state imposed; no reward taken. This separate branch is rewound by actual F6."});_notes.Add("REAR LIVE GUARD PATH "+(observed?"VERIFIED":"NOT VERIFIED"));yield return Capture("07-real-rear-branch");
            }
            yield return Reload(original);Require(!ReferenceEquals(Zone,originalZone)&&Player.ID==id&&At.X==x&&At.Y==y&&Stats(Player)==stats&&Gear(Player)==gear&&NoteSignature()==notes&&SiteSignature()==site&&Tick==tick&&Energy==energy&&WorldClock.CurrentTick==world&&TradeSystem.GetDrams(Player)==drams&&HashFile(file)==hash,"exact real rear/front branch baseline reload");
        }
        IEnumerator FightGuard(bool requireDirectLethal=true)
        {
            _fighting=true;for(int i=0;i<45;i++)
            {
                if(CombatSystem.IsDeathHandled(_guard)){Require(_guardAttempt&&_guardDamage&&(!requireDirectLethal||_guardLethal),requireDirectLethal?"canonical exact original-player guard defeat":"observed real hostile defense resolved; direct lethal attribution remains separate");_fighting=false;yield break;}
                if(Player.GetStatValue("Hitpoints")<=24){var tonic=Player.GetPart<InventoryPart>().Objects.FirstOrDefault(e=>e.BlueprintName=="HealingTonic");if(tonic!=null){int units=Player.GetPart<InventoryPart>().Objects.Where(e=>e.BlueprintName=="HealingTonic").Sum(Units),tick=Tick,energy=Energy;yield return ItemAction(tonic,"ApplyTonic");yield return CloseNormal();Require(Player.GetPart<InventoryPart>().Objects.Where(e=>e.BlueprintName=="HealingTonic").Sum(Units)==units-1&&Tick==tick&&Energy==energy,"actual finite original tonic");}}
                if(SpatialQuery.Distance(Zone,Player,_guard)<=1){var target=SpatialQuery.ClosestCell(Zone,_guard,At.X,At.Y);yield return Paid(Tap(Direction(target.X-At.X,target.Y-At.Y)),"local",ConnectedVariant&&VariantBreaker?"native-breaker-cudgel-defense":_soddenDistrict?"native-earned-weapon-defense":"native-front-dagger-attack");}
                else{var path=PathTo(c=>SpatialQuery.DistanceToCell(Zone,_guard,c.X,c.Y)==1);Require(path!=null&&path.Count>0,"actual front guard approach");yield return StepTo(path[0].x,path[0].y);}
            }
            throw new InvalidOperationException("Bounded front combat exhausted; no grant or reroll.");
        }
        IEnumerator TakeSole(Entity container,string id)
        {
            Require(container.GetPart<ContainerPart>()?.Contents.Count==1&&container.GetPart<ContainerPart>().Contents[0].ID==id,"actual single authored container item");yield return WorldAction(container,"OpenContainer");Require(State=="PickupOpen","native exact container pickup");
            var items=(List<Entity>)Field(_input.PickupUI,"_items");Require(ReferenceEquals(Field(_input.PickupUI,"_sourceContainer"),container)&&items.Count==1&&items[0].ID==id,"native pickup source and exact sole row");yield return Tap(Key.Tab);yield return CloseNormal();
        }
        IEnumerator FinalCheckpoint()
        {
            var actor=Player;var zone=Zone;string id=Player.ID,stats=Stats(Player),gear=Gear(Player),notes=NoteSignature(),site=SiteSignature();int tick=Tick,energy=Energy,world=WorldClock.CurrentTick,x=At.X,y=At.Y,drams=TradeSystem.GetDrams(Player);
            string file=SaveFile(),old=HashFile(file);long serial=MessageLog.NextSerialValue;yield return Tap(Key.F5);yield return Settled();_checkpointHash=HashFile(file);
            Check("depleted_checkpoint_saved",old!=_checkpointHash&&MessageLog.NextSerialValue>serial&&MessageLog.GetLast()=="Game saved."&&SaveGameService.GetSaveInfo("Quick").ActiveZoneID==_siteId);
            var step=Steps.Select(d=>Zone.GetCell(x+d.x,y+d.y)).FirstOrDefault(c=>Safe(Zone,c,ThreatClearance)&&Zone.CanPlaceFootprint(Player,c.X,c.Y));Require(step!=null,"one real safe unsaved step");yield return StepTo(step.X,step.Y);Check("real_unsaved_step",(At.X!=x||At.Y!=y)&&Tick>tick&&HashFile(file)==_checkpointHash);
            yield return Reload(actor);Check("restored_exact_notes_and_depleted_site",!ReferenceEquals(Zone,zone)&&Player.ID==id&&Zone.ZoneID==_siteId&&At.X==x&&At.Y==y&&Stats(Player)==stats&&Gear(Player)==gear&&NoteSignature()==notes&&SiteSignature()==site&&Tick==tick&&Energy==energy&&WorldClock.CurrentTick==world&&TradeSystem.GetDrams(Player)==drams&&HashFile(file)==_checkpointHash&&Owns(Player,OwnerOrCarried(_keyId))&&Owns(Player,OwnerOrCarried(_rewardId)));yield return Capture("14-native-restored-depleted-site");
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
        Entity OwnerOrCarried(string id)=>Player.GetPart<InventoryPart>().Objects.Concat(Player.GetPart<InventoryPart>().EquippedItems.Values).FirstOrDefault(e=>e.ID==id)??Owner(id);
        IEnumerator TravelSurface(string target)
        {
            Require(State=="Normal"&&WorldMap.FromZoneID(Zone.ZoneID).z==0,"native map travel begins on surface");
            if(Connected)_connectedVisitedSurfaces.Add(Zone.ZoneID);
            if(Zone.ZoneID==target)yield break;
            yield return Paid(Tap(Key.LeftShift,Key.Comma),"local","native-map-ascend");Require(WorldMap.IsWorldMapZoneID(Zone.ZoneID),"actual native map ascent");
            var (wx,wy,_)=WorldMap.FromZoneID(target);var (zx,zy)=WorldMap.WorldCellToZoneCell(wx,wy);
            while(At.X!=zx||At.Y!=zy)
            {
                int dx=Math.Sign(zx-At.X),dy=Math.Sign(zy-At.Y);int x=At.X+dx,y=At.Y+dy;
                yield return Paid(Tap(Direction(dx,dy)),"map","native-worldmap-step");Require(At.X==x&&At.Y==y,"actual requested worldmap cell");
            }
            yield return Paid(Tap(Key.LeftShift,Key.Period),"local","native-map-descend");Require(Zone.ZoneID==target,"actual requested surface entered");
            if(Connected)_connectedVisitedSurfaces.Add(Zone.ZoneID);
        }
        IEnumerator StepTo(int x,int y)
        {
            if(Connected)yield return ConnectedCloseAndStabilize();
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
            // A selected spell direction is an intentional open prompt. Only
            // ordinary command boundaries drain late announcements here.
            if(Connected&&(State=="Normal"||State=="AnnouncementOpen"))yield return ConnectedCloseAndStabilize();
            Require(kind=="map"?_mapSteps<(ConnectedVariant?16:Connected?30:20):kind=="rest"?_rests<2:_localInputs<(ConnectedVariant?420:Connected?1000:360),"finite approved action budget");
            var actor=Player;int before=Energy,tick=Tick,speed=Player.GetStatValue("Speed",100);string marker=Mark(label),combat=null;if(_guard!=null&&_fighting){Diag.Record("scenario",ReferenceGladeCombatEvidence.MarkerKind,Player,_guard);combat=Diag.Snapshot(1).Single().TraceId;}
            yield return action;yield return CloseNormal();var rows=Window(marker);
            bool validClock=ReferenceEquals(actor,Player)&&Player.GetStatValue("Speed",100)==speed&&DensityCampaignNativeEvidence.TryClock(rows,marker,Player.ID,kind,before,Energy,Tick-tick,speed,out _lastClock);
            if(!validClock)_observations.Add(new{phase="rejected-native-action-clock",label,kind,marker,beforeEnergy=before,afterEnergy=Energy,beforeTick=tick,afterTick=Tick,speed,rows});
            Require(validClock,"exact native action clock/energy "+label);
            if(combat!=null){var proof=ReferenceGladeCombatEvidence.Inspect(rows,combat,Player.ID,_guardId);Require(proof.WindowValid,"canonical exact guard combat window");_guardAttempt|=proof.PlayerAttempt;_guardDamage|=proof.PlayerDamage;_guardLethal|=proof.PlayerLethal;}
            if(kind=="map")_mapSteps++;else if(kind=="rest")_rests++;else _localInputs++;_completedTurns+=_lastClock.CompletedTurns;_pureClock+=_lastClock.PureClock;
            _windows.Add(new{label,kind,marker,beforeEnergy=before,afterEnergy=Energy,beforeTick=tick,afterTick=Tick,receipt=_lastClock,rows});Observe(label);
        }
        IEnumerator ItemAction(Entity item,string command)
        {
            Require(State=="Normal"&&Owns(Player,item),"native owned inventory action");yield return Tap(Key.I);
            // Opening the full-screen view can move the pointer's grid position
            // and focus the inventory already. Tab only until the actual pane is reached.
            for(int n=0;(int)Field(_input.InventoryUI,"_panel")!=1;n++)
            {Require(n<6,"bounded native inventory pane");yield return Tap(Key.Tab);}
            var rows=(IList)Field(_input.InventoryUI,"_rows");int row=-1;for(int i=0;i<rows.Count;i++)if(ReferenceEquals(((InventoryScreenData.ItemDisplay)Field(rows[i],"Item"))?.Item,item))row=i;
            Require(row>=0,"actual current inventory row");for(int n=0;(int)Field(_input.InventoryUI,"_cursorIndex")!=row;n++){Require(n<80,"bounded inventory cursor: pane="+Field(_input.InventoryUI,"_panel")+" current="+Field(_input.InventoryUI,"_cursorIndex")+" target="+row);yield return Tap((int)Field(_input.InventoryUI,"_cursorIndex")<row?Key.DownArrow:Key.UpArrow);}
            yield return Tap(Key.Enter);var popup=Field(_input.InventoryUI,"_itemActionPopup");Require(popup!=null,"native item action popup");var actions=((IList)Field(popup,"Actions")).Cast<object>().ToArray();int index=Array.FindIndex(actions,a=>(string)Field(a,"Command")==command);Require(index>=0,"actual offered item action "+command);
            for(int n=0;(int)Field(popup,"CursorIndex")!=index;n++){Require(n<50,"bounded item action cursor");yield return Tap((int)Field(popup,"CursorIndex")<index?Key.DownArrow:Key.UpArrow);}yield return Tap(Key.Enter);
        }
        IEnumerator CloseNormal()
        {
            if(Connected){yield return ConnectedCloseAndStabilize();yield break;}
            for(int n=0;State!="Normal";n++){Require(n<8,"bounded ordinary modal closure "+State);yield return Tap(Key.Escape);}yield return Settled();
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
                +";owners="+string.Join("|",inv.Objects.Concat(inv.EquippedItems.Values).Distinct().OrderBy(e=>e.ID).Select(e=>e.ID+":"+e.BlueprintName+":"+Units(e)+":carried="+e.GetPart<PhysicsPart>()?.InInventory?.ID+":equipped="+e.GetPart<PhysicsPart>()?.Equipped?.ID));
        }

        private static string Stats(Entity actor)=>string.Join("|",actor.Statistics.OrderBy(p=>p.Key).Select(p=>p.Key+":"+p.Value.BaseValue+":"+p.Value.Value+":"+p.Value.Bonus+":"+p.Value.Penalty+":"+p.Value.Min+":"+p.Value.Max))
            +";effects="+string.Join("|",actor.GetPart<StatusEffectsPart>()?.GetAllEffects().Select(e=>e.GetType().Name+":"+e.Duration).OrderBy(x=>x)??Enumerable.Empty<string>());

        private IEnumerator Settled(){double began=Time.realtimeSinceStartupAsDouble;while(State!="Normal"||_input.ZoneRenderer?.WorldFx?.HasBlockingFx==true){Require(Time.realtimeSinceStartupAsDouble-began<8,"input/FX settle: "+State);yield return null;}yield return null;}

        private IEnumerator Tap(params Key[] keys)
        {
            Require(_clock.Elapsed.TotalSeconds<(Connected?1200:600),"finite native acceptance deadline");double began=Time.realtimeSinceStartupAsDouble;
            while(_input!=null&&Time.time-(float)Field(_input,"_lastMoveTime")<_input.MoveRepeatDelay){Require(Time.realtimeSinceStartupAsDouble-began<3,"input rate gate");yield return null;}
            if(_started)Require(Player.GetStatValue("Hitpoints")>10&&!CombatSystem.IsDeathHandled(Player),"ordinary HP safety stop");
            _keys.Add(new{sequence=_keys.Count,keys=string.Join(",",keys.Select(k=>k.ToString())),state=_input==null?"bootstrap":State,tick=_input?.TurnManager?.TickCount??0});WriteReport();
            _keyboard.MakeCurrent();InputSystem.QueueStateEvent(_keyboard,new KeyboardState(keys));yield return null;InputSystem.QueueStateEvent(_keyboard,new KeyboardState());yield return null;yield return new WaitForSecondsRealtime(.13f);
        }

        private IEnumerator Capture(string name){yield return new WaitForSecondsRealtime(.15f);yield return new WaitForEndOfFrame();Directory.CreateDirectory(DirectoryPath);string path=Path.Combine(DirectoryPath,name+".png");DensityNativeScreenshot.CaptureToFile(path);Require(File.Exists(path)&&new FileInfo(path).Length>0,"native screenshot");_screenshots.Add(path);WriteReport();}

        private static object Field(object owner,string name){var f=owner.GetType().GetField(name,Private|BindingFlags.Public);if(f==null)throw new InvalidOperationException("Missing observed field "+owner.GetType().Name+"."+name);return f.GetValue(owner);}

        private static string HashFile(string path){using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}

        private static void Require(bool value,string reason){if(!value)throw new InvalidOperationException("Spread discovery precondition: "+reason);}

        private void Check(string name,bool passed){if(!passed)_failures++;_audit.Add((passed?"PASS ":"FAIL ")+name);Observe(name);Diag.Record("scenario","SpreadDiscoveryNativeCase",payload:new{runId=RunId,name,passed});}

        private IEnumerator RunSafely(IEnumerator routine)
        {
            var stack=new Stack<IEnumerator>();stack.Push(routine);
            try
            {
                while(stack.Count>0)
                {
                    bool moved=false;object next=null;Exception error=null;
                    try{moved=stack.Peek().MoveNext();if(moved)next=stack.Peek().Current;}catch(Exception e){error=e;}
                    if(error!=null){_fatal=error.ToString();Check("native_precondition_failed",false);Debug.LogError("[SpreadDiscoveryNative] "+error);break;}
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
                if(threats.Any(e=>!(_fighting&&ReferenceEquals(e,_guard))
                    &&!((_district||Connected)&&ReferenceGladeRouteControl.HasLiveCalm(zone,e))
                    &&SpatialQuery.DistanceToCell(zone,e,c.X,c.Y)<=clearance))return false;
                if(_avoidGuard&&_guard?.SpatialZone==zone&&zone.GetEntityCell(_guard) is Cell g&&SpatialQuery.DistanceToCell(zone,_guard,c.X,c.Y)<=_guard.GetPart<BrainPart>().SightRadius&&AIHelpers.HasLineOfSight(zone,g.X,g.Y,c.X,c.Y))return false;
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
            _observations.Add(new{phase,player=Player.ID,zone=Zone.ZoneID,x=At?.X,y=At?.Y,hp=Player.GetStatValue("Hitpoints"),stats=Stats(Player),tick=Tick,energy=Energy,drams=TradeSystem.GetDrams(Player),gear=Gear(Player),notes=NoteSignature(),site=_siteId,guard=_guardId,key=_keyId,reward=_rewardId,message=MessageLog.GetLast()});WriteReport();
        }
        void Finish(){try{EndTiming();}finally{Cleanup();Finished=true;WriteReport();}}
        bool Complete=>Finished&&_errorsFinalized&&Failures==0&&_audit.Count==RequiredChecks.Length&&RequiredChecks.All(n=>_audit.Contains("PASS "+n))&&_screenshots.Count>=(_expeditionReport?4:_tacticalWater?6:_mendleafYard?6:_soddenDistrict?6:_trapJamming?6:_predatorDiversion?6:_fieldwork?6:ConnectedVariant?6:Connected?10:_district?7:_cards?6:_ordinary?10:12);
        void WriteReport()
        {
            Directory.CreateDirectory(DirectoryPath);ReportPath=Path.Combine(DirectoryPath,"report.json");
            string json=JsonConvert.SerializeObject(new{runId=RunId,mode=_expeditionReport?"ordinary-expedition-report":_tacticalWater?"ordinary-finite-emergency-water":_mendleafYard?"ordinary-mendleaf-drying-yard":_soddenDistrict?"ordinary-sodden-expedition":_trapJamming?"ordinary-timber-trap-jamming":_predatorDiversion?"ordinary-predator-diversion":_fieldwork?"ordinary-living-fieldwork":ConnectedVariant?"connected-targeted-"+_connectedBuild:Connected?"connected-selected-"+_connectedBuild:_district?"ordinary-gleaners-district":_cards?"generated-source-cards":_ordinary?"ordinary-grain-round-trip":"staged-generated-sites",complete=Complete,failures=Failures,unexpectedErrors=_unexpectedErrors,seconds=_clock?.Elapsed.TotalSeconds??0,fatal=_fatal,audit=_audit,requiredChecks=RequiredChecks,keys=_keys,observations=_observations,windows=_windows,timings=_timings,notes=_notes,screenshots=_screenshots,localInputs=_localInputs,mapSteps=_mapSteps,completedPlayerTurns=_completedTurns,pureClock=_pureClock,checkpointHash=_checkpointHash,threatClearance=ThreatClearance,
                passedChecks=RequiredChecks.Where(n=>_audit.Contains("PASS "+n)).ToArray(),unmetChecks=RequiredChecks.Where(n=>!_audit.Contains("PASS "+n)).ToArray(),
                canVerify="Only the named passedChecks were observed in this run; intendedCapability is the planned route, not a completion claim. Partial failures and unmetChecks remain explicit.",
                intendedCapability=_expeditionReport?ExpeditionReportIntent:_tacticalWater?TacticalWaterIntent:_mendleafYard?MendleafIntent:_soddenDistrict?SoddenIntent:_trapJamming?TrapJammingIntent:_predatorDiversion?DiversionIntent:_fieldwork?FieldworkIntent:ConnectedVariant?VariantIntent:Connected?ConnectedIntent:_district?DistrictIntent:_cards?CardsIntent:_ordinary?"Ordinary seed64 N, native Sill reports/notes, native map and ground route to generated FieldStrips, exact harvested grain eaten once, native return to Sill and F5/unsaved-step/F6 restores consumed grain absence/spent row/notes. No setup transfers or source grants.":"Ordinary seed64 N bootstrap and native map travel to actual Sill informant, explicit report notes/Q-Tab. Disclosed original-player transfers to unchanged generated field and selected wayhouse. Actual harvest, live-guard rear path with baseline reload, front key/door/reward, F5/unsaved-step/F6 notes and depleted-graph identity.",
                cannotVerify=_expeditionReport?ExpeditionReportLimits:_tacticalWater?TacticalWaterLimits:_mendleafYard?MendleafLimits:_soddenDistrict?SoddenLimits:_trapJamming?TrapJammingLimits:_predatorDiversion?DiversionLimits:_fieldwork?FieldworkLimits:ConnectedVariant?VariantLimits:Connected?ConnectedLimits:_district?DistrictLimits:_cards?CardsLimits:_ordinary?"One seed and one actual row only; not a balance/permanent safety/all-seed proof. Earned food use is consumption; no healing benefit at full HP. No shop-sale claim. Timing is short and includes audit IO. Screenshots require independent viewing.":"Not an ordinary continuous expedition journey: field/wayhouse transfers and a saved rear/front branch replay are explicit fixture setup. No generated source edits or HP/AI/gear/loot grants. No all-seed/balance/permanent-safety claim. Only passedChecks are observed; source generation/route refusal is retained. Screenshots require independent viewing."},Formatting.Indented);
            var parsed=JObject.Parse(json);Require(parsed["windows"] is JArray w&&w.Count==_windows.Count&&parsed["observations"] is JArray o&&o.Count==_observations.Count,"complete nested report evidence");File.WriteAllText(ReportPath,json);
        }
        void Cleanup(){if(_cleaned)return;_cleaned=true;StopDiversionObservation();if(_keyboard!=null){InputSystem.QueueStateEvent(_keyboard,new KeyboardState());InputSystem.RemoveDevice(_keyboard);}if(_oldKeyboard!=null&&_oldKeyboard.added)_oldKeyboard.MakeCurrent();if(_oldSettings!=null)InputSystem.settings=_oldSettings;if(_settings!=null)Destroy(_settings);Application.runInBackground=_oldBackground;}
        void EmitSummary(){if(_summaryEmitted)return;_summaryEmitted=true;Diag.Record("scenario","SpreadDiscoveryNativeSummary",payload:new{runId=RunId,cases=_audit.Count,failures=Failures,complete=Complete});}
        void OnDestroy(){if(!Finished&&_clock!=null){_fatal="Play stopped before completion.";Check("native_interrupted",false);Finish();}Cleanup();EmitSummary();foreach(var p in _oldChannels)Diag.SetChannel(p.Key,p.Value);}
    }
}
