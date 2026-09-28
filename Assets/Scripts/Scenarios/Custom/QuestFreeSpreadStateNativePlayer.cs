using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Bounded local activation witness. Original-player setup transfers and one supplied
    /// empty vessel are disclosed. Generated owners, their AI, stock and source geometry are not staged.</summary>
    public sealed partial class QuestFreeSpreadStateNativePlayer : MonoBehaviour
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static readonly (int x,int y)[] Directions={(1,0),(0,1),(-1,0),(0,-1)};
        public string RunId{get;}=Guid.NewGuid().ToString("N");
        public bool Finished{get;private set;}public int Failures=>_errors+(_fatal==null?0:1);public string ReportPath{get;private set;}
        ScenarioContext _context;InputHandler _input;Keyboard _keyboard,_oldKeyboard;InputSettings _settings,_oldSettings;
        bool _background,_ownsSettings,_cleaned,_complete,_errorsFinalized,_observing;string _fatal;int _errors,_paidInputs,_feedEvents,_feedAttacks;
        Entity _grazer,_meal;float _feedTime;bool _feedCommitted;readonly List<object> _checks=new List<object>(),_observations=new List<object>();readonly List<string> _images=new List<string>(),_unverifiedFamilies=new List<string>();
        System.Diagnostics.Stopwatch _clock;
        string _mode="states";
        int ExpectedSeed=>_mode=="exchange"?3:64;
        int PaidLimit=>_mode=="passage"?10:_mode=="exchange"?31:_mode=="collector"?64:_mode=="affordances"?1:6;
        string DirectoryPath=>Path.GetFullPath(Path.Combine(Application.dataPath,_mode=="passage"?"../Docs/Verification/QuestFreeExploration/E4/FieldPassage/Native":_mode=="exchange"?"../Docs/Verification/QuestFreeExploration/E3/Exchange/Native":_mode=="collector"?"../Docs/Verification/QuestFreeExploration/E3/Collector/NativeStates":_mode=="affordances"?"../Docs/Verification/QuestFreeExploration/E3/Affordances/Native":"../Docs/Verification/QuestFreeExploration/E2/NativeStates",RunId));
        Entity Player=>_input.PlayerEntity;Zone Zone=>_input.CurrentZone;OverworldZoneManager Manager=>_input.ZoneManager as OverworldZoneManager;
        Cell At=>Zone.GetEntityCell(Player);string State=>Field(_input,"_inputState").ToString();int Tick=>_input.TurnManager.TickCount;int Energy=>_input.TurnManager.GetEnergy(Player);
        SpawnRing3DPresenter Presenter=>_input.ZoneRenderer.SpawnRing3D;
        public void Initialize(ScenarioContext context)=>InitializeMode(context,"states");
        public void InitializeCollector(ScenarioContext context)=>InitializeMode(context,"collector");
        public void InitializeAffordances(ScenarioContext context)=>InitializeMode(context,"affordances");
        public void InitializePassage(ScenarioContext context)=>InitializeMode(context,"passage");
        public void InitializeExchange(ScenarioContext context)=>InitializeMode(context,"exchange");
        void InitializeMode(ScenarioContext context,string mode)
        {
            if(string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride))throw new InvalidOperationException("Isolated launcher required before bootstrap.");
            _mode=mode;if(_mode=="exchange"||_mode=="passage")BeginExchangeDiagnostics();_context=context;_clock=System.Diagnostics.Stopwatch.StartNew();_oldSettings=InputSystem.settings;_background=Application.runInBackground;_ownsSettings=true;
            _settings=Instantiate(_oldSettings);_settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            _settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings=_settings;Application.runInBackground=true;_oldKeyboard=Keyboard.current;_keyboard=InputSystem.AddDevice<Keyboard>();StartCoroutine(RunSafely(Run()));
        }
        IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(.6f);_input=FindFirstObjectByType<InputHandler>();Require(_input!=null,"ordinary bootstrap input");
            Require(((BootMenuController)Field(_input,"_bootMenuController"))?.IsActive==true,"ordinary N menu");yield return Tap(Key.N);yield return Settled();
            Check("ordinary_fresh_opted_in_world",Manager?.WorldSeed==ExpectedSeed&&Manager.Exploration.Enabled&&!DevMode.Enabled&&Player.GetStatValue("Hitpoints")==40&&TradeSystem.GetDrams(Player)==50);
            yield return Capture("00-ordinary-start");
            if(_mode=="passage"){yield return Passage();Check("bounded_passage_finish",State=="Normal"&&Player.GetStatValue("Hitpoints")==40&&!DevMode.Enabled&&!DebugInvincibility.IsEnabled(Player)&&_paidInputs<=PaidLimit);_complete=_unverifiedFamilies.Count==0;yield break;}
            if(_mode=="exchange"){yield return Exchange();Check("bounded_exchange_finish",State=="Normal"&&Player.GetStatValue("Hitpoints")>10&&!DevMode.Enabled&&!DebugInvincibility.IsEnabled(Player)&&_paidInputs<=PaidLimit);_complete=_unverifiedFamilies.Count==0;yield break;}
            if(_mode!="states"){if(_mode=="affordances")yield return Affordances();else yield return Collector();Check("bounded_mode_finish",State=="Normal"&&Player.GetStatValue("Hitpoints")==40&&!DevMode.Enabled&&!DebugInvincibility.IsEnabled(Player)&&_paidInputs<=PaidLimit);_complete=_unverifiedFamilies.Count==0;yield break;}
            yield return Feeding();yield return Territory();yield return DrawWater();
            var corpses=Manager.CachedZones.Values.SelectMany(z=>z.GetReadOnlyEntities().Where(e=>e.BlueprintName=="ReedbackGrazerCorpse").Select(e=>new{zone=z.ZoneID,id=e.ID,source=e.Properties.TryGetValue("SourceID",out var source)?source:null})).ToArray();
            _observations.Add(new{phase="natural-corpse-opportunistic",observed=corpses.Length>0,owners=corpses,boundary="No death, corpse chance or corpse source was forced. Absence leaves ordinary corpse acquisition unverified; staged native art gallery is separate."});
            Check("bounded_finish",State=="Normal"&&Player.GetStatValue("Hitpoints")==40&&!DevMode.Enabled&&!DebugInvincibility.IsEnabled(Player)&&_paidInputs<=6);
            _complete=_unverifiedFamilies.Count==0;
        }
        IEnumerator Feeding()
        {
            Zone site=null;Entity actor=null;Cell arrival=null;
            foreach(var entry in Candidates(SpreadExplorationFamily.LastGleanings))
            {
                var z=Manager.GetZone(entry.ZoneID);var candidates=z.GetReadOnlyEntities().Where(e=>e.BlueprintName=="ReedbackGrazer").ToArray();
                if(candidates.Length==0)_observations.Add(new{phase="grazer-source-qualification",zone=z.ZoneID,disposition=Manager.Exploration.DispositionFor(z.ZoneID),reason="no-grazer-owner",ripeRows=z.GetReadOnlyEntities().Count(e=>e.BlueprintName=="RipeCropRow"&&e.GetPart<FieldHarvestPart>()?.Harvested==false)});
                foreach(var e in candidates)
                {
                    var reason=GrazerObservationCell(z,e,out var candidateCell);
                    var part=e.GetPart<SpreadGrazerPart>();
                    _observations.Add(new{phase="grazer-source-qualification",zone=z.ZoneID,disposition=Manager.Exploration.DispositionFor(z.ZoneID),owner=e.ID,position=z.GetEntityPosition(e),configured=part?.Configured,fed=part?.Fed,food=part?.Food?.ID,reserve=part?.ReservedRow?.ID,foodDistance=part?.Food==null?(int?)null:SpatialQuery.Distance(z,e,part.Food),reason});
                    if(reason==null){arrival=candidateCell;site=z;actor=e;break;}
                }
                Selection(entry,z,actor,arrival,"configured adjacent unspent food; safe observer beyond flight radius");if(actor!=null)break;
            }
            if(actor==null||arrival==null){Unverified("feeding","No admitted source/view in the same eight canonical seed64 candidates; see per-owner qualification reasons.");yield break;}yield return Transfer(site,arrival,"actual LastGleanings grazer");
            var grazer=actor.GetPart<SpreadGrazerPart>();var meal=grazer.Food;var reserve=grazer.ReservedRow;var mealCell=Zone.GetEntityCell(meal);var reserveCell=Zone.GetEntityCell(reserve);var actorCell=Zone.GetEntityCell(actor);
            var view=Visual(actor,"questfree-spread-grazer",1);Require(Zone.GetEntityCell(meal).IsVisible&&Presenter.IsRenderedEntity(meal),"actual visible food before native wait");
            string ripe=Model(meal);float headBefore=HeadHeight(view);_grazer=actor;_meal=meal;EntityVisualHooks.InteractionCallback+=OnInteraction;EntityVisualHooks.AttackCallback+=OnAttack;_observing=true;
            yield return Capture("01-generated-grazer-and-ripe-row");yield return WaitForActorTurn(actor,"native-wait-feed");
            Check("actual_finite_feed_committed",grazer.Fed&&meal.GetPart<FieldHarvestPart>().Harvested&&!reserve.GetPart<FieldHarvestPart>().Harvested&&Zone.GetEntityCell(actor)==actorCell&&Zone.GetEntityCell(meal)==mealCell&&Zone.GetEntityCell(reserve)==reserveCell);
            Check("one_committed_interact_no_attack",_feedEvents==1&&_feedCommitted&&_feedAttacks==0&&Player.GetStatValue("Hitpoints")==40);
            var nativeView=((IDictionary)Field(Presenter,"views"))[actor];var animator=view.GetComponentInChildren<Animator>();
            // Observe the actual running clip. Never SampleAnimation or advance an Animator in the live proof.
            Check("actual_interact_pose_selected",(string)Field(nativeView,"ActionState")=="Interact"&&(float)Field(nativeView,"ActionUntil")>Time.unscaledTime&&animator.GetCurrentAnimatorStateInfo(0).IsName("Interact"));
            while(Time.unscaledTime<_feedTime+.29f)yield return null;
            float headAfter=HeadHeight(view);_observations.Add(new{phase="live-head-down",beforeWorldUp=headBefore,afterWorldUp=headAfter,elapsed=Time.unscaledTime-_feedTime,model=Model(actor),rootScale=view.transform.lossyScale.ToString("R"),skinScale=view.GetComponentInChildren<SkinnedMeshRenderer>().transform.lossyScale.ToString("R"),measurement="BakeMesh default then actual skin TransformPoint; same convention as accepted native coordinate probe"});
            Check("live_head_down_and_stubble",headAfter<headBefore-.04f&&Model(meal)==ripe.Replace("grain-","stubble-"));
            yield return Capture("02-actual-native-feeding-and-stubble");StopObserving();yield return new WaitForSecondsRealtime(.7f);
            nativeView=((IDictionary)Field(Presenter,"views"))[actor];Check("gesture_expires_without_second_meal",(float)Field(nativeView,"ActionUntil")<Time.unscaledTime&&grazer.Fed&&!reserve.GetPart<FieldHarvestPart>().Harvested);
        }
        IEnumerator Territory()
        {
            Zone site=null;Entity actor=null;Cell arrival=null;Cell retreat=null;
            foreach(var entry in Candidates(SpreadExplorationFamily.OccupiedBank))
            {
                var z=Manager.GetZone(entry.ZoneID);
                foreach(var e in z.GetReadOnlyEntities().Where(e=>e.GetPart<SpreadTerritoryPart>()?.Configured==true))
                {
                    var duty=e.GetPart<SpreadTerritoryPart>();var a=z.GetEntityCell(e);if(a==null||!FactionManager.IsHostile(e,Player)||duty.WarningTarget!=null)continue;
                    if(z.GetReadOnlyEntities().Any(other=>other!=e&&other!=Player&&other.HasTag("Creature")&&other.GetStatValue("Hitpoints")>0&&FactionManager.IsHostile(e,other)&&Inside(duty,z.GetEntityCell(other))))continue;
                    for(int y=duty.Top;y<=duty.Bottom&&arrival==null;y++)for(int x=duty.Left;x<=duty.Right;x++)
                    {
                        var c=z.GetCell(x,y);if(!DrySafe(z,c,e)||SpatialQuery.DistanceToCell(z,e,x,y)<2||!AIHelpers.HasLineOfSight(z,x,y,a.X,a.Y))continue;
                        foreach(var d in Directions){var outCell=z.GetCell(x+d.x,y+d.y);if(Inside(duty,outCell)||!DrySafe(z,outCell,e))continue;arrival=c;retreat=outCell;actor=e;site=z;break;}if(arrival!=null)break;
                    }
                    if(actor!=null)break;
                }
                Selection(entry,z,actor,arrival,"territory edge with one safe outward cardinal step");if(actor!=null)break;
            }
            if(actor==null||arrival==null||retreat==null){Unverified("territory","No admitted current source and safe withdrawal in the bounded candidates.");yield break;}yield return Transfer(site,arrival,"actual occupied bank edge");
            var part=actor.GetPart<SpreadTerritoryPart>();string preserved=SourceRows(new[]{actor,part.Post},site);Require(Zone.GetEntityCell(actor).IsVisible&&Presenter.TryGetApprovedStyle(actor,out _),"current visible approved territorial owner");
            int serial=MessageLog.GetRecentEntries(1).Select(e=>e.Serial).DefaultIfEmpty(-1).Max();yield return WaitForActorTurn(actor,"native-wait-territorial-warning");
            var messages=MessageLog.GetRecentEntries(30).Where(e=>e.Serial>serial).Select(e=>e.Text).ToArray();_observations.Add(new{phase="territorial-warning",owner=actor.ID,post=part.Post.ID,messages,part.GraceRemaining,region=new[]{part.Left,part.Top,part.Right,part.Bottom}});
            Check("actual_warning_before_contact",part.WarningTarget==Player&&part.GraceRemaining>0&&messages.Any(t=>t.Contains("warns you to leave the marked ground"))&&Player.GetStatValue("Hitpoints")==40&&!actor.GetPart<BrainPart>().IsPersonallyHostileTo(Player));
            yield return Capture("03-actual-territorial-warning");yield return Paid(Direction(retreat.X-At.X,retreat.Y-At.Y),"native-one-step-withdrawal");
            Check("warning_withdrawal_no_fight",At==retreat&&!Inside(part,At)&&part.WarningTarget==null&&part.GraceRemaining==0&&!actor.GetPart<BrainPart>().IsPersonallyHostileTo(Player)&&Player.GetStatValue("Hitpoints")==40&&SourceRows(new[]{actor,part.Post},site)==preserved);
            yield return Capture("04-actual-withdrawal-clear-of-post");
        }
        IEnumerator DrawWater()
        {
            Zone site=null;Entity source=null;Cell arrival=null;
            foreach(var entry in Candidates(SpreadExplorationFamily.WateringMargin))
            {
                var z=Manager.GetZone(entry.ZoneID);
                foreach(var e in z.GetReadOnlyEntities().Where(e=>e.BlueprintName=="SpreadDrawPoint"&&e.GetPart<LiquidPoolPart>()?.Volume==3))
                {
                    var a=z.GetEntityCell(e);arrival=CellsNear(z,a,1,1).FirstOrDefault(c=>DrySafe(z,c,null)&&FirstWaterAt(z,c)==e);
                    if(arrival!=null){site=z;source=e;break;}
                }
                Selection(entry,z,source,arrival,"safe dry adjacent cell whose first real water source is this finite pool");if(source!=null)break;
            }
            if(source==null||arrival==null){Unverified("finite-water","No admitted current finite source and service-selected dry approach in the bounded candidates.");yield break;}yield return Transfer(site,arrival,"actual finite bank draw point");
            Require(ReferenceEquals(typeof(WaterVesselService).GetMethod("FindSource",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{Player,Zone}),source),"actual native service selects this exact finite owner");
            var pool=source.GetPart<LiquidPoolPart>();var cell=Zone.GetEntityCell(source);string sourceId=source.ID;Visual(source,"questfree-spread-draw-full",2);yield return Examine(source,"05-actual-full-draw-reader","3 drinks");yield return Capture("06-actual-full-draw-point");
            var vessel=_context.Factory.CreateEntity("Waterskin");Require(vessel?.GetPart<WaterskinPart>()?.Charges==0&&vessel.GetPart<WaterskinPart>().Capacity==3&&Player.GetPart<InventoryPart>().AddObject(vessel),"explicitly supplied one original empty waterskin");
            _observations.Add(new{phase="disclosed-staged-empty-vessel",id=vessel.ID,blueprint=vessel.BlueprintName,charges=0,capacity=3,boundary="Supplied only to exercise actual inventory fill; no natural acquisition claim."});
            var otherPools=Zone.GetReadOnlyEntities().Where(e=>e!=source&&e.HasPart<LiquidPoolPart>()).ToDictionary(e=>e,e=>e.GetPart<LiquidPoolPart>().Volume);int tick=Tick,energy=Energy,speed=Player.GetStatValue("Speed",100);Require(_paidInputs<6,"bounded paid input is real fill");yield return ItemAction(vessel,"FillWaterskin");yield return CloseNormal();_paidInputs++;long cost=(long)energy+(long)(Tick-tick)*speed-Energy;_observations.Add(new{phase="native-inventory-fill",beforeTick=tick,afterTick=Tick,beforeEnergy=energy,afterEnergy=Energy,cost,source=source.ID,vessel=vessel.ID});Require(cost==TurnManager.ActionThreshold,"one actual paid inventory fill");
            Check("native_fill_conserves_same_finite_owner",source.ID==sourceId&&Zone.GetEntityCell(source)==cell&&source.GetPart<LiquidPoolPart>()==pool&&pool.Volume==0&&vessel.GetPart<WaterskinPart>().Charges==3&&vessel.GetPart<PhysicsPart>().InInventory==Player&&otherPools.All(p=>p.Key.GetPart<LiquidPoolPart>().Volume==p.Value)&&Player.GetStatValue("Hitpoints")==40);
            Visual(source,"questfree-spread-draw-empty",1);yield return Capture("07-native-empty-point-and-filled-skin");yield return Examine(source,"08-actual-empty-draw-reader","empty");
            Check("persistent_empty_does_not_regrow_during_free_reader",pool.Volume==0&&source.ID==sourceId&&Zone.GetEntityCell(source)==cell&&vessel.GetPart<WaterskinPart>().Charges==3);
        }
        IEnumerable<SpreadExplorationEntry> Candidates(SpreadExplorationFamily family)=>Manager.Exploration.Entries.Where(e=>e.PlacementEligible&&e.Family==family).OrderBy(e=>e.ZoneID,StringComparer.Ordinal).Take(8);
        void Selection(SpreadExplorationEntry entry,Zone zone,Entity source,Cell at,string criterion)
        { _observations.Add(new{phase="bounded-actual-source-selection",family=entry.Family.ToString(),zone=zone.ZoneID,owner=source?.ID,arrival=at==null?null:new[]{at.X,at.Y},accepted=source!=null&&at!=null,criterion,boundary="Metadata chooses up to eight addresses in canonical order; actual source/current geometry chooses an observation cell. This is not an uninformed player decision."});WriteReport(); }
        // Same admission predicates as the first witness, now with a distinct
        // reason per rejected owner. This does not loosen selection or inspect new addresses.
        string GrazerObservationCell(Zone z,Entity e,out Cell observer)
        {
            observer=null;var part=e.GetPart<SpreadGrazerPart>();
            if(part==null||!part.Configured)return "grazer-not-configured";
            if(part.Fed)return "already-fed";
            var a=z.GetEntityCell(e);if(a==null)return "missing-current-anchor";
            if(part.Food==null||part.ReservedRow==null)return "missing-food-or-reserve";
            if(part.Food.GetPart<FieldHarvestPart>()?.Harvested!=false||part.ReservedRow.GetPart<FieldHarvestPart>()?.Harvested!=false)return "food-or-reserve-unavailable";
            if(SpatialQuery.Distance(z,e,part.Food)>1)return "food-not-adjacent";
            if(z.GetReadOnlyEntities().Any(other=>other!=e&&other!=Player&&other.HasTag("Creature")&&other.GetStatValue("Hitpoints")>0&&FactionManager.IsHostile(e,other)&&SpatialQuery.Distance(z,e,other)<=3))return "competing-hostile-within-flight-radius";
            var dry=CellsNear(z,a,4,5).Where(c=>DrySafe(z,c,e)).ToArray();if(dry.Length==0)return "no-safe-dry-observer";
            observer=dry.FirstOrDefault(c=>AIHelpers.HasLineOfSight(z,c.X,c.Y,a.X,a.Y)&&AIHelpers.HasLineOfSight(z,c.X,c.Y,part.FoodX,part.FoodY));
            return observer==null?"no-observer-sight-to-actor-and-food":null;
        }
        void Unverified(string family,string reason)
        { _unverifiedFamilies.Add(family);_observations.Add(new{phase="family-unverified",family,reason,boundary="Independent available families may still run. No seed/candidate-limit/source/admission change; complete remains false while any family is unverified."});WriteReport(); }
        static bool Inside(SpreadTerritoryPart part,Cell c)=>c!=null&&c.X>=part.Left&&c.X<=part.Right&&c.Y>=part.Top&&c.Y<=part.Bottom;
        IEnumerable<Cell> CellsNear(Zone z,Cell target,int min,int max)
        {for(int y=Math.Max(1,target.Y-max);y<=Math.Min(Zone.Height-2,target.Y+max);y++)for(int x=Math.Max(1,target.X-max);x<=Math.Min(Zone.Width-2,target.X+max);x++){int d=Math.Max(Math.Abs(x-target.X),Math.Abs(y-target.Y));if(d>=min&&d<=max)yield return z.GetCell(x,y);}}
        bool DrySafe(Zone z,Cell c,Entity allowedThreat)
        {
            if(c==null||!z.CanPlaceFootprint(Player,c.X,c.Y))return false;
            if(c.Occupants.Any(e=>e!=Player&&(e.HasTag("Creature")||e.HasPart<LiquidPoolPart>()||e.HasPart<GasPoolPart>()||e.HasPart<TriggerOnStepPart>()||e.HasEffect<BurningEffect>()||e.GetPart<ThermalPart>()?.IsAflame==true)))return false;
            var s=z.TileState.Get(c.X,c.Y);if(s!=null&&(s.Heat>0||s.Cold>0||s.Charge>0||!string.IsNullOrEmpty(s.Cloud)||s.Coatings.Count>0))return false;
            return !z.GetReadOnlyEntities().Any(e=>e!=Player&&e!=allowedThreat&&e.HasTag("Creature")&&e.GetStatValue("Hitpoints")>0&&FactionManager.IsHostile(e,Player)&&SpatialQuery.DistanceToCell(z,e,c.X,c.Y)<=3);
        }
        // This is candidate screening only. After transfer the real service's read-only selector is also required to name the same owner.
        Entity FirstWaterAt(Zone z,Cell c)=>z.GetReadOnlyEntities().FirstOrDefault(e=>SpatialQuery.DistanceToCell(z,e,c.X,c.Y)<=1&&!e.HasTag("Creature")&&e.GetPart<PhysicsPart>()?.Takeable!=true&&e.GetPart<PhysicsPart>()?.InInventory==null&&e.GetPart<PhysicsPart>()?.Equipped==null&&(e.GetPart<LiquidPoolPart>() is LiquidPoolPart p?p.LiquidId=="water"&&p.Volume>0&&LiquidSourceSafety.IsUnmixedPool(z,e):e.HasPart<WellPart>()||(e.GetPart<TileStateSourcePart>()?.Coating=="water"&&e.GetPart<TileStateSourcePart>().CoatingTurns>0&&LiquidSourceSafety.IsUnmixedSource(z,e,"water"))));
        IEnumerator Transfer(Zone destination,Cell at,string label)
        {
            Require(State=="Normal"&&ReferenceEquals(Manager.CachedZones[destination.ZoneID],destination),"current managed destination");var player=Player;var manager=Manager;var originals=destination.GetReadOnlyEntities().Where(e=>!e.HasTag("Player")).ToArray();string stats=PlayerSignature(),graph=SourceRows(originals,destination);int tick=Tick,energy=Energy,world=WorldClock.CurrentTick;
            Require(Zone.TryTransferEntityTo(player,destination,at.X,at.Y),"disclosed player-only setup transfer");typeof(InputHandler).GetMethod("HandleZoneTransition",Private).Invoke(_input,new object[]{new ZoneTransitionResult{Success=true,NewZone=destination,NewPlayerX=at.X,NewPlayerY=at.Y}});
            _input.CameraFollow?.SnapToPlayer();ZoneRenderHooks.MarkFullDirty("QuestFreeSpreadStateDisclosedTransfer");yield return Settled();yield return Tap(Key.L);yield return Tap(Key.Escape);yield return Settled();
            Require(ReferenceEquals(Player,player)&&ReferenceEquals(Manager,manager)&&Zone==destination&&At==at&&PlayerSignature()==stats&&Tick==tick&&Energy==energy&&WorldClock.CurrentTick==world&&originals.All(e=>e.SpatialZone==destination&&destination.GetEntityCell(e)!=null)&&SourceRows(originals,destination)==graph,"transfer preserves original player/manager/stats and generated source owners");
            _observations.Add(new{phase="disclosed-player-only-transfer",label,zone=Zone.ZoneID,x=at.X,y=at.Y,player=Player.ID,tick=Tick,nativeEntryAdditions=destination.GetReadOnlyEntities().Where(e=>e!=player&&!originals.Contains(e)).Select(e=>new{id=e.ID,blueprint=e.BlueprintName,position=destination.GetEntityPosition(e)}).ToArray(),boundary="Setup transfer to an actual generated source; not an ordinary journey. Original source owners are preserved. Normal native entry-hook additions such as travellers are recorded separately, never suppressed."});WriteReport();
        }
        string PlayerSignature()=>Player.ID+"|"+Player.GetStatValue("Hitpoints")+"|"+TradeSystem.GetDrams(Player)+"|"+string.Join(";",Player.GetPart<InventoryPart>().Objects.Concat(Player.GetPart<InventoryPart>().GetAllEquipped()).Distinct().OrderBy(e=>e.ID,StringComparer.Ordinal).Select(e=>e.ID+":"+e.BlueprintName+":"+(e.GetPart<StackerPart>()?.StackCount??1)+":"+e.GetPart<PhysicsPart>()?.InInventory?.ID+":"+e.GetPart<PhysicsPart>()?.Equipped?.ID));
        static string SourceRows(IEnumerable<Entity> entities,Zone z)=>string.Join("|",entities.Where(e=>!e.HasTag("Player")).OrderBy(e=>e.ID,StringComparer.Ordinal).Select(e=>e.ID+":"+e.BlueprintName+":"+z.GetEntityPosition(e)+":"+e.GetStatValue("Hitpoints")+":"+e.GetPart<LiquidPoolPart>()?.Volume+":"+e.GetPart<FieldHarvestPart>()?.Harvested+":"+string.Join(",",e.GetPart<ContainerPart>()?.Contents.Select(c=>c.ID+":"+c.BlueprintName+":"+(c.GetPart<StackerPart>()?.StackCount??1))??Enumerable.Empty<string>())));
        void OnInteraction(Entity actor,Entity target,Zone zone){if(actor!=_grazer||target!=_meal||zone!=Zone)return;_feedEvents++;_feedTime=Time.unscaledTime;_feedCommitted=actor.GetPart<SpreadGrazerPart>()?.Fed==true&&target.GetPart<FieldHarvestPart>()?.Harvested==true;}
        void OnAttack(Entity actor,Entity target,Zone zone){if(actor==_grazer&&zone==Zone)_feedAttacks++;}
        void StopObserving(){if(!_observing)return;_observing=false;EntityVisualHooks.InteractionCallback-=OnInteraction;EntityVisualHooks.AttackCallback-=OnAttack;}
        GameObject Visual(Entity owner,string expected,int pieces)
        {
            Require(Zone.GetEntityCell(owner)?.IsVisible==true&&owner.GetPart<RenderPart>()?.Visible==true&&Presenter.IsRenderedEntity(owner),"actual FOV and current submitted owner "+owner.ID);
            Require(Presenter.TryGetApprovedStyle(owner,out var proof)&&proof.ModelId==expected&&proof.PieceCount==pieces,"exact approved current all-piece style "+expected);
            Require(Presenter.TryGetEntityView(owner,out var root,out var model)&&model==expected&&!proof.Batched,"exact independent owner view");var camera=Presenter.WorldCamera;bool bounds=false;
            foreach(var r in root.GetComponentsInChildren<Renderer>())if(r.enabled&&!r.forceRenderingOff&&r.gameObject.activeInHierarchy)
            {
                // Measure actual submitted geometry, not the skin's deliberately enlarged culling envelope.
                Mesh owned=null;try{var skin=r as SkinnedMeshRenderer;var mesh=skin!=null?(owned=new Mesh()):r.GetComponent<MeshFilter>()?.sharedMesh;if(skin!=null)skin.BakeMesh(mesh);Require(mesh!=null&&mesh.vertexCount>0,"actual submitted nonempty geometry");if(owned!=null)owned.RecalculateBounds();bounds=true;var b=mesh.bounds;for(int i=0;i<8;i++){var p=camera.WorldToViewportPoint(r.transform.TransformPoint(new Vector3((i&1)==0?b.min.x:b.max.x,(i&2)==0?b.min.y:b.max.y,(i&4)==0?b.min.z:b.max.z)));Require(p.z>0&&p.x>=0&&p.x<=1&&p.y>=0&&p.y<=1,"current owner fully within world camera");}}finally{if(owned!=null)Destroy(owned);}
            }
            Require(bounds,"nonempty submitted bounds");_observations.Add(new{phase="actual-current-style",owner=owner.ID,blueprint=owner.BlueprintName,model,pieces,camera=new{camera.pixelWidth,camera.pixelHeight},boundary="All-piece identity and bounds do not prove unoccluded pixels or human readability; images require review."});return root;
        }
        string Model(Entity e){Require(Presenter.TryGetEntityView(e,out _,out string model),"current model "+e.ID);return model;}
        static float HeadHeight(GameObject view)
        {var skin=view.GetComponentInChildren<SkinnedMeshRenderer>();int head=Array.FindIndex(skin.bones,b=>b.name=="Head");Require(head>=0,"actual imported Head bone");var weights=skin.sharedMesh.boneWeights;var mesh=new Mesh();try{skin.BakeMesh(mesh);var v=mesh.vertices;return Enumerable.Range(0,v.Length).Where(i=>weights[i].boneIndex0==head).Min(i=>skin.transform.TransformPoint(v[i]).y);}finally{UnityEngine.Object.Destroy(mesh);}}
        // Newly entered actors start at zero energy while the ready player keeps
        // 1000. Equal-speed ties select the earlier player entry, so the first
        // paid wait can fund an NPC turn without executing it. Never grant energy.
        IEnumerator WaitForActorTurn(Entity actor,string label)
        {
            var brain=actor.GetPart<BrainPart>();Require(brain!=null,"actual observed actor brain");bool acted=false;
            for(int attempt=1;attempt<=2&&!acted;attempt++)
            {
                var ages=brain.GetGoalsSnapshot().ToDictionary(g=>g,g=>g.Age);
                int tick=Tick,energy=_input.TurnManager.GetEnergy(actor),speed=_input.TurnManager.GetSpeed(actor);
                ActorObservation(actor,label+"-before-"+attempt);
                yield return Paid(Key.Period,label+"-"+attempt);
                long spent=(long)energy+(long)(Tick-tick)*speed-_input.TurnManager.GetEnergy(actor);
                // Only Brain.HandleTakeTurn advances GoalHandler.Age. Reading it
                // plus the real energy debit avoids attaching an observer Part.
                bool goalAdvanced=brain.GetGoalsSnapshot().Any(g=>g.Age>0&&(!ages.TryGetValue(g,out int oldAge)||g.Age>oldAge));
                acted=ReferenceEquals(actor.GetPart<BrainPart>(),brain)&&spent>=TurnManager.ActionThreshold&&goalAdvanced;
                ActorObservation(actor,label+"-after-"+attempt);
                _observations.Add(new{phase="observed-actor-turn",label,attempt,owner=actor.ID,spent,goalAdvanced,acted,boundary="Actual paid key; execution inferred from native goal-age advance plus energy debit. No part, turn, energy or goal was injected."});WriteReport();
            }
            Require(acted,"actual actor turn within two ordinary paid waits "+label);
        }
        void ActorObservation(Entity actor,string phase)
        {
            var brain=actor.GetPart<BrainPart>();var part=actor.GetPart<SpreadTerritoryPart>();var post=part?.Post;
            var physics=actor.GetPart<PhysicsPart>();var pp=post?.GetPart<PhysicsPart>();
            _observations.Add(new{phase,zone=Zone.ZoneID,owner=actor.ID,actorAt=Zone.GetEntityPosition(actor),playerAt=Zone.GetEntityPosition(Player),post=post?.ID,postAt=post==null?((int,int)?)null:Zone.GetEntityPosition(post),hp=actor.GetStatValue("Hitpoints"),actorIsCreature=actor.HasTag("Creature"),targetIsCreature=Player.HasTag("Creature"),actorCurrent=ReferenceEquals(actor.SpatialZone,Zone)&&Zone.GetEntityCell(actor)?.Objects.Contains(actor)==true,actorPartOwner=physics?.ParentEntity==actor,inInventory=physics?.InInventory?.ID,equipped=physics?.Equipped?.ID,
                sight=brain?.SightRadius,brainZone=brain?.CurrentZone?.ZoneID,brainZoneIsCurrent=ReferenceEquals(brain?.CurrentZone,Zone),brainParent=brain?.ParentEntity==actor,partyLeader=brain?.PartyLeader?.ID,target=brain?.Target?.ID,personalHostility=brain?.IsPersonallyHostileTo(Player),hostile=FactionManager.IsHostile(actor,Player),playerHostile=FactionManager.IsHostile(Player,actor),brainState=brain?.CurrentState.ToString(),inConversation=brain?.InConversation,goals=brain?.GetGoalsSnapshot().Select(g=>new{type=g.GetType().Name,g.Age}).ToArray(),
                configured=part?.Configured,partZone=part?.ZoneID,home=part==null?null:new[]{part.HomeX,part.HomeY},postExpected=part==null?null:new[]{part.PostX,part.PostY},region=part==null?null:new[]{part.Left,part.Top,part.Right,part.Bottom},postCurrent=post!=null&&ReferenceEquals(post.SpatialZone,Zone)&&Zone.GetEntityCell(post)!=null,postPartOwner=pp?.ParentEntity==post,postInventory=pp?.InInventory?.ID,postEquipped=pp?.Equipped?.ID,postIsCreature=post?.HasTag("Creature"),inside=part!=null&&Inside(part,At),warningTarget=part?.WarningTarget?.ID,grace=part?.GraceRemaining,graceTurns=part?.GraceTurns,registered=_input.TurnManager.IsRegistered(actor),energy=_input.TurnManager.GetEnergy(actor),speed=_input.TurnManager.GetSpeed(actor),playerEnergy=Energy,tick=Tick,currentActor=_input.TurnManager.CurrentActor?.ID,waitingForInput=_input.TurnManager.WaitingForInput});WriteReport();
        }
        IEnumerator Paid(Key key,string label)
        {Require(_paidInputs<PaidLimit&&State=="Normal","bounded native paid input");int tick=Tick,energy=Energy,hp=Player.GetStatValue("Hitpoints"),speed=Player.GetStatValue("Speed",100);yield return Tap(key);yield return Settled();_paidInputs++;long cost=(long)energy+(long)(Tick-tick)*speed-Energy;_observations.Add(new{phase=label,key=key.ToString(),beforeTick=tick,afterTick=Tick,beforeEnergy=energy,afterEnergy=Energy,cost,beforeHp=hp,afterHp=Player.GetStatValue("Hitpoints")});Require(cost==TurnManager.ActionThreshold,"one actual paid action "+label);}
        IEnumerator Examine(Entity target,string label,string phrase)
        {
            string before=PlayerSignature(),source=SourceRows(new[]{target},Zone);int tick=Tick,energy=Energy;yield return Tap(Key.L);Require(State=="LookMode","actual native look");var cursor=(WorldCursorState)Field(_input,"_worldCursorState");var to=Zone.GetEntityCell(target);
            for(int n=0;cursor.X!=to.X||cursor.Y!=to.Y;n++){Require(n<18,"bounded real look cursor");yield return Tap(cursor.X<to.X?Key.D:cursor.X>to.X?Key.A:cursor.Y<to.Y?Key.S:Key.W);}yield return Tap(Key.Enter);Require(State=="WorldActionMenuOpen","actual owner menu");
            string choose=WorldInteractionSystem.PickTargetCommandPrefix+target.ID;var actions=(List<InventoryAction>)Field(_input.WorldActionMenuUI,"_actions");if(!ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget,target)||_input.WorldActionMenuUI.SelectedCellIsPile){if(!actions.Any(a=>a.Command==choose)&&actions.Any(a=>a.Command==WorldInteractionSystem.PickCellCommand))yield return MenuAction(WorldInteractionSystem.PickCellCommand);}
            actions=(List<InventoryAction>)Field(_input.WorldActionMenuUI,"_actions");if(actions.Any(a=>a.Command==choose))yield return MenuAction(choose);Require(ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget,target)&&!_input.WorldActionMenuUI.SelectedCellIsPile,"exact current source selected");yield return MenuAction("Examine");
            Require(State=="AnnouncementOpen"&&_input.AnnouncementUI.IsOpen,"actual full reader");var lines=new List<string>();int pages=_input.AnnouncementUI.PageCount;Require(pages>0&&pages<=5,"bounded actual reader");for(int i=0;i<pages;i++){Require((int)Field(_input.AnnouncementUI,"_pageIndex")==i,"actual reader page");lines.AddRange(_input.AnnouncementUI.VisibleLines);if(i==0||i==pages-1)yield return Capture(label+"-"+i);if(i+1<pages)yield return Tap(Key.RightArrow);}
            string text=string.Join(" ",lines);_observations.Add(new{phase=label,owner=target.ID,pages,text});yield return CloseNormal();Check(label+"_free_current_readout",text.Contains(phrase)&&PlayerSignature()==before&&SourceRows(new[]{target},Zone)==source&&Tick==tick&&Energy==energy);
        }
        IEnumerator MenuAction(string command)
        {var actions=(List<InventoryAction>)Field(_input.WorldActionMenuUI,"_actions");int index=actions.FindIndex(a=>a.Command==command);Require(index>=0,"offered native action "+command);for(int n=0;(int)Field(_input.WorldActionMenuUI,"_cursorIndex")!=index;n++){Require(n<40,"bounded real menu cursor");yield return Tap((int)Field(_input.WorldActionMenuUI,"_cursorIndex")<index?Key.DownArrow:Key.UpArrow);}yield return Tap(Key.Enter);}
        IEnumerator ItemAction(Entity item,string command)
        {yield return Tap(Key.I);yield return Tap(Key.Tab);var rows=(IList)Field(_input.InventoryUI,"_rows");int row=-1;for(int i=0;i<rows.Count;i++)if(ReferenceEquals(((InventoryScreenData.ItemDisplay)Field(rows[i],"Item"))?.Item,item))row=i;Require(row>=0,"actual supplied vessel row");for(int n=0;(int)Field(_input.InventoryUI,"_cursorIndex")!=row;n++){Require(n<80,"bounded inventory cursor");yield return Tap((int)Field(_input.InventoryUI,"_cursorIndex")<row?Key.DownArrow:Key.UpArrow);}yield return Tap(Key.Enter);var popup=Field(_input.InventoryUI,"_itemActionPopup");Require(popup!=null,"actual inventory popup");var actions=((IList)Field(popup,"Actions")).Cast<object>().ToArray();int index=Array.FindIndex(actions,a=>(string)Field(a,"Command")==command);Require(index>=0,"actual offered vessel action");for(int n=0;(int)Field(popup,"CursorIndex")!=index;n++){Require(n<50,"bounded item cursor");yield return Tap((int)Field(popup,"CursorIndex")<index?Key.DownArrow:Key.UpArrow);}yield return Tap(Key.Enter);}
        static Key Direction(int dx,int dy)=>dx>0?Key.D:dx<0?Key.A:dy>0?Key.S:Key.W;
        IEnumerator CloseNormal(){for(int n=0;State!="Normal";n++){Require(n<8,"bounded modal closure "+State);yield return Tap(Key.Escape);}yield return Settled();}
        IEnumerator Settled(){double began=Time.realtimeSinceStartupAsDouble;while(State!="Normal"||_input.ZoneRenderer?.WorldFx?.HasBlockingFx==true){Require(Time.realtimeSinceStartupAsDouble-began<8,"finite input settling");yield return null;}yield return null;}
        IEnumerator Tap(params Key[] keys)
        {Require(_clock.Elapsed.TotalSeconds<150,"bounded total local witness");double began=Time.realtimeSinceStartupAsDouble;while(_input!=null&&Time.time-(float)Field(_input,"_lastMoveTime")<_input.MoveRepeatDelay){Require(Time.realtimeSinceStartupAsDouble-began<3,"input repeat gate");yield return null;}_keyboard.MakeCurrent();InputSystem.QueueStateEvent(_keyboard,new KeyboardState(keys));yield return null;InputSystem.QueueStateEvent(_keyboard,new KeyboardState());yield return null;yield return new WaitForSecondsRealtime(.13f);}
        IEnumerator Capture(string label){yield return new WaitForEndOfFrame();Directory.CreateDirectory(DirectoryPath);string path=Path.Combine(DirectoryPath,label+".png");DensityNativeScreenshot.CaptureToFile(path);Require(File.Exists(path)&&new FileInfo(path).Length>0,"actual screenshot saved");_images.Add(path);WriteReport();}
        void Check(string label,bool pass){_checks.Add(new{label,pass});WriteReport();Require(pass,label);}
        static object Field(object owner,string name)=>owner.GetType().GetField(name,Private|BindingFlags.Public).GetValue(owner);
        static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        IEnumerator RunSafely(IEnumerator routine)
        {var stack=new Stack<IEnumerator>();stack.Push(routine);try{while(stack.Count>0){bool moved=false;object next=null;Exception error=null;try{moved=stack.Peek().MoveNext();if(moved)next=stack.Peek().Current;}catch(Exception e){error=e;}if(error!=null){_fatal=error.ToString();break;}if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}if(next is IEnumerator child){stack.Push(child);continue;}yield return next;}if(_fatal!=null&&_input!=null){yield return new WaitForEndOfFrame();FailureFrame();}}finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();Finish();}}
        void FailureFrame(){try{Directory.CreateDirectory(DirectoryPath);string path=Path.Combine(DirectoryPath,"99-failed-current-state.png");DensityNativeScreenshot.CaptureToFile(path);if(File.Exists(path))_images.Add(path);_observations.Add(new{phase="failed-current-state",zone=Zone?.ZoneID,player=Player?.ID,position=At==null?null:new[]{At.X,At.Y},hp=Player?.GetStatValue("Hitpoints"),state=State});}catch(Exception e){_observations.Add(new{phase="failure-frame-unavailable",reason=e.Message});}}
        public void Abort(string reason){if(Finished)return;_fatal=reason;StopAllCoroutines();Finish();}
        public void SetUnexpectedErrors(int errors){_errors=errors;_errorsFinalized=true;WriteReport();}
        void Finish(){Cleanup();Finished=true;WriteReport();}
        void Cleanup(){if(_cleaned)return;_cleaned=true;RestoreExchangeDiagnostics();StopCollectorObservation();StopObserving();if(_keyboard!=null){InputSystem.QueueStateEvent(_keyboard,new KeyboardState());InputSystem.RemoveDevice(_keyboard);}if(_oldKeyboard!=null&&_oldKeyboard.added)_oldKeyboard.MakeCurrent();if(_oldSettings!=null)InputSystem.settings=_oldSettings;if(_settings!=null)Destroy(_settings);if(_ownsSettings)Application.runInBackground=_background;}
        void WriteReport(){Directory.CreateDirectory(DirectoryPath);ReportPath=Path.Combine(DirectoryPath,"report.json");File.WriteAllText(ReportPath,JsonConvert.SerializeObject(new{runId=RunId,seed=(_input?.ZoneManager as OverworldZoneManager)?.WorldSeed,requestedSeed=ExpectedSeed,mode=_mode,finished=Finished,complete=Finished&&_errorsFinalized&&_complete&&Failures==0,failures=Failures,fatal=_fatal,seconds=_clock?.Elapsed.TotalSeconds??0,paidInputs=_paidInputs,checks=_checks,observations=_observations,screenshots=_images,unverifiedFamilies=_unverifiedFamilies,humanAwarenessMeasured=false,boundary=_mode=="passage"?PassageBoundary:_mode=="exchange"?ExchangeBoundary:ModeBoundary,settings=new{screenWidth=Screen.width,screenHeight=Screen.height,vSync=QualitySettings.vSyncCount,targetFrameRate=Application.targetFrameRate,lowDetail=Village3DSettings.LowDetail}},Formatting.Indented));}
        void OnDestroy(){if(!Finished&&_clock!=null){_fatal="Play interrupted";Finish();}Cleanup();}
    }
}
