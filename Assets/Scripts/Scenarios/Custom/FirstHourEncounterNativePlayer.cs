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

using CavesOfOoo.Skills;
namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Explicit staged M4 acceptance. Ordinary real starting actor, legal native actions;
    /// factory pair and terrain are disclosed fixtures, never natural discovery evidence.</summary>
    public sealed class FirstHourEncounterNativePlayer:MonoBehaviour
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static readonly (int x,int y)[] Steps={(1,0),(-1,0),(0,1),(0,-1),(1,1),(1,-1),(-1,1),(-1,-1)};
        static readonly string[] RequiredChecks={"ordinary_start","explicit_factory_stage_and_original_gear","keyboard_bypass_avoids_pair","original_dagger_equipped","native_player_melee_damage","native_player_attributed_cutter_death","same_authored_gear_spilled","native_same_gear_acquired","actual_cap_compare","checkpoint_saved","real_earned_item_mutation","restored_exact_earned_graph","ordinary_staged_finish"};
        public string RunId{get;}=Guid.NewGuid().ToString("N");
        public bool Finished{get;private set;}public int Failures=>_failures+_unexpectedErrors;public string ReportPath{get;private set;}
        InputHandler _input;Keyboard _keyboard,_oldKeyboard;InputSettings _settings,_oldSettings;
        bool _started,_cleaned,_oldBackground,_errorsFinalized,_summaryEmitted,_attempt,_damage,_lethal;
        readonly Dictionary<string,bool> _oldChannels=new Dictionary<string,bool>();
        readonly Dictionary<string,Entity> _startingTonics=new Dictionary<string,Entity>();
        readonly Dictionary<string,Guid> _originalSkills=new Dictionary<string,Guid>();
        readonly List<string> _audit=new List<string>(),_screenshots=new List<string>(),_notes=new List<string>();
        readonly List<object> _keys=new List<object>(),_observations=new List<object>(),_windows=new List<object>();
        readonly List<Diag.Entry> _allActionRows=new List<Diag.Entry>();
        System.Diagnostics.Stopwatch _clock;int _failures,_unexpectedErrors,_localInputs,_completedTurns,_tonics,_casts,_attacks,_capUnits,_swordUnits;
        string _fatal,_ownedRoot,_checkpointHash,_zoneID;Entity _leader,_mate,_dagger,_cap,_sword;NoFightGoal _calm;
        Diag.Entry[] _lastRows=Array.Empty<Diag.Entry>();
        string DirectoryPath=>Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/SpreadFirstHour/M4/Native",RunId));
        Entity Player=>_input.PlayerEntity;Zone Zone=>_input.CurrentZone;OverworldZoneManager Manager=>_input.ZoneManager as OverworldZoneManager;
        Cell At=>Zone.GetEntityCell(Player);int Tick=>_input.TurnManager.TickCount;int Energy=>_input.TurnManager.GetEnergy(Player);
        string State=>Field(_input,"_inputState").ToString();
        IEnumerator RunAudit()
        {
            yield return new WaitForSecondsRealtime(.8f);_input=FindFirstObjectByType<InputHandler>();Require(_input!=null,"ordinary bootstrap");
            var boot=(BootMenuController)Field(_input,"_bootMenuController");Require(boot?.IsActive==true,"ordinary N menu");yield return Tap(Key.N);yield return Settled();_started=true;
            _dagger=Player.GetPart<InventoryPart>().Objects.Single(e=>e.BlueprintName=="Dagger");
            foreach(var e in Player.GetPart<InventoryPart>().Objects.Where(e=>e.BlueprintName=="HealingTonic"))_startingTonics.Add(e.ID,e);
            foreach(var a in Player.GetPart<ActivatedAbilitiesPart>().AbilityList)_originalSkills.Add(a.Command,a.ID);
            Check("ordinary_start",Manager.WorldSeed==1&&Player.GetStatValue("Hitpoints")==40&&Player.GetStat("Hitpoints").Max==40&&Player.GetStatValue("Level")==1&&TradeSystem.GetDrams(Player)==50&&Player.GetStatValue("Strength")==18&&Player.GetStatValue("Agility")==18&&Player.GetStatValue("Toughness")==18&&TonicUnits()==2&&Ordinary());
            string stats=Stats(Player),gear=Gear(Player);int tick=Tick,energy=Energy;
            _zoneID=Manager.RareEncounters.PairZoneID;Require(!string.IsNullOrEmpty(_zoneID)&&!Manager.CachedZones.ContainsKey(_zoneID)&&SpreadRareEncounterPlan.IsEligible(Manager,_zoneID),"uncached actual Spread fixture address");
            var stage=new Zone(_zoneID);var factory=Manager.Factory;
            for(int x=24;x<=41;x++)for(int y=6;y<=21;y++)Require(stage.AddEntity(factory.CreateEntity("Grass"),x,y),"staged grass");
            for(int x=26;x<=36;x++)Require(stage.AddEntity(factory.CreateEntity("Hedge"),x,14),"staged hedge screen with open ends");
            _leader=factory.CreateEntity(SpreadRareEncounterPlan.PairLeader);_mate=factory.CreateEntity(SpreadRareEncounterPlan.PairMate);
            _cap=_leader.GetPart<InventoryPart>().EquippedItems.Values.Distinct().Single(e=>e.BlueprintName=="LeatherCap");_sword=_leader.GetPart<InventoryPart>().EquippedItems.Values.Distinct().Single(e=>e.BlueprintName=="ShortSword");
            _capUnits=Units(_cap);_swordUnits=Units(_sword);
            string leaderGear=Gear(_leader),mateGear=Gear(_mate),leaderStats=Stats(_leader),mateStats=Stats(_mate);
            Require(stage.AddEntity(_leader,30,10)&&stage.AddEntity(_mate,32,10),"two untouched factory actors staged once");
            var old=Zone;Require(old.TryTransferEntityTo(Player,stage,30,18),"one disclosed fixture player transfer");
            typeof(InputHandler).GetMethod("HandleZoneTransition",Private).Invoke(_input,new object[]{new ZoneTransitionResult{Success=true,NewZone=stage,NewPlayerX=30,NewPlayerY=18}});
            _input.CameraFollow?.SnapToPlayer();ZoneRenderHooks.MarkFullDirty("FirstHourEncounterExplicitStage");yield return Settled();
            _notes.Add("EXPLICIT STAGE: actual factory pair at30,10/32,10;11 Hedge atx26..36,y14 and288 Grass cells; original player transferred once to30,18. No source-builder/natural-discovery claim. No manual enemy stats/HP/gear/AI/goals/RNG or player resource overrides. Native zone entry performs its normal Brain.Rng initialization.");
            Check("explicit_factory_stage_and_original_gear",Zone==stage&&Stats(Player)==stats&&Gear(Player)==gear&&Tick==tick&&Energy==energy&&Stats(_leader)==leaderStats&&Stats(_mate)==mateStats&&Gear(_leader)==leaderGear&&Gear(_mate)==mateGear&&_input.TurnManager.IsRegistered(_leader)&&_input.TurnManager.IsRegistered(_mate)&&_leader.GetPart<BrainPart>().CurrentZone==stage&&_mate.GetPart<BrainPart>().CurrentZone==stage);
            yield return Capture("01-explicit-pair-and-hedge");
            int hp=Player.GetStatValue("Hitpoints"),leaderHp=_leader.GetStatValue("Hitpoints"),mateHp=_mate.GetStatValue("Hitpoints"),rowStart=_allActionRows.Count;
            for(int x=31;x<=37;x++)yield return MoveTo(x,18,"native-bypass");
            Check("keyboard_bypass_avoids_pair",At.X==37&&At.Y==18&&Player.GetStatValue("Hitpoints")==hp&&_leader.GetStatValue("Hitpoints")==leaderHp&&_mate.GetStatValue("Hitpoints")==mateHp&&Gear(_leader)==leaderGear&&Gear(_mate)==mateGear&&!_allActionRows.Skip(rowStart).Any(r=>r.Category=="damage"&&r.Kind=="HitRoll"&&r.TargetId==Player.ID));
            yield return Capture("02-real-keyboard-bypass");yield return ItemAction(_dagger,"equip_auto");yield return CloseNormal();Check("original_dagger_equipped",_dagger.GetPart<PhysicsPart>().Equipped==Player&&Player.GetPart<InventoryPart>().EquippedItems.Values.Contains(_dagger));
            yield return Encounter();
            Check("native_player_melee_damage",_attempt&&_damage);Check("native_player_attributed_cutter_death",_lethal&&CombatSystem.IsDeathHandled(_leader)&&Zone.GetEntityCell(_leader)==null);
            Check("same_authored_gear_spilled",Zone.GetEntityCell(_cap)!=null&&Zone.GetEntityCell(_sword)!=null&&_cap.GetPart<PhysicsPart>().Equipped==null&&_sword.GetPart<PhysicsPart>().Equipped==null&&Units(_cap)==_capUnits&&Units(_sword)==_swordUnits);
            yield return Capture("03-actual-combat-and-owned-drops");
            yield return Reach(Zone.GetEntityCell(_cap),30);yield return Paid(PickupExact(_cap),"native-earned-cap-pickup");
            if(!Owns(Player,_sword)){yield return Reach(Zone.GetEntityCell(_sword),15);yield return Paid(PickupExact(_sword),"native-earned-sword-pickup");}
            Check("native_same_gear_acquired",Owns(Player,_cap)&&Owns(Player,_sword)&&Zone.GetEntityCell(_cap)==null&&Zone.GetEntityCell(_sword)==null&&Units(_cap)==_capUnits&&Units(_sword)==_swordUnits);
            tick=Tick;energy=Energy;gear=Gear(Player);yield return ItemAction(_cap,"compare_equipment");
            Require(State=="AnnouncementOpen","actual carried/equipped cap comparison modal");string comparison=string.Join("\n",_input.AnnouncementUI.VisibleLines);Require(comparison.Contains("Leather")||comparison.Contains("leather"),"real cap comparison content");
            _notes.Add("CAP COMPARISON "+comparison);
            Check("actual_cap_compare",Tick==tick&&Energy==energy&&Gear(Player)==gear);yield return Capture("04-actual-recovered-cap-comparison");yield return CloseNormal();
            yield return Checkpoint();Check("ordinary_staged_finish",Ordinary()&&Player.GetStatValue("Hitpoints")>10&&Owns(Player,_cap)&&Owns(Player,_sword)&&State=="Normal");yield return Capture("05-restored-earned-graph");
        }
        bool Ordinary()=>!DevMode.Enabled&&!Player.HasPart<BitLockerPart>()&&!DebugInvincibility.IsEnabled(Player);
        int TonicUnits()=>_startingTonics.Where(p=>p.Value.ID==p.Key&&Player.GetPart<InventoryPart>().Objects.Contains(p.Value)&&p.Value.GetPart<PhysicsPart>()?.InInventory==Player).Sum(p=>p.Value.GetPart<StackerPart>()?.StackCount??1);
        static bool Owns(Entity owner,Entity item)=>item!=null&&((owner.GetPart<InventoryPart>().Objects.Contains(item)&&item.GetPart<PhysicsPart>()?.InInventory==owner)||(owner.GetPart<InventoryPart>().EquippedItems.Values.Contains(item)&&item.GetPart<PhysicsPart>()?.Equipped==owner));
        IEnumerator UseTonic()
        {
            if(_tonics>=2||Player.GetStatValue("Hitpoints")*3>Player.GetStat("Hitpoints").Max*2)yield break;
            var item=_startingTonics.Values.FirstOrDefault(e=>Player.GetPart<InventoryPart>().Objects.Contains(e)&&e.GetPart<PhysicsPart>()?.InInventory==Player);if(item==null)yield break;
            int units=TonicUnits(),tick=Tick,energy=Energy;Diag.Record("scenario",ReferenceGladeCombatEvidence.SupportMarkerKind,Player,Player);string marker=Diag.Snapshot(1).Single().TraceId;
            yield return ItemAction(item,"ApplyTonic");yield return CloseNormal();Require(ReferenceGladeCombatEvidence.HasConsumedTonic(Diag.Snapshot(Diag.BufferCapacity),marker,Player.ID,item.ID,units,TonicUnits())&&Tick==tick&&Energy==energy,"exact original free tonic consumption");_tonics++;Observe("original-tonic");
        }
        IEnumerator Encounter()
        {
            for(int n=0;n<50;n++)
            {
                if(CombatSystem.IsDeathHandled(_leader))yield break;Require(Zone.GetEntityCell(_leader)!=null,"actual live cutter remains in staged zone");yield return UseTonic();
                if(_mate.GetStatValue("Hitpoints")>0&&Zone.GetEntityCell(_mate)!=null&&!_mate.GetPart<BrainPart>().HasGoal<NoFightGoal>()&&Ready("CommandCalm")&&TryAim(_mate,6,out int cx,out int cy))
                {yield return Cast("CommandCalm",_mate,cx,cy);_calm=_mate.GetPart<BrainPart>().PeekGoal() as NoFightGoal;Require(DensityCombatNativeEvidence.CurrentCalm(_mate,Zone,_calm),"real current mate pacification");continue;}
                if(_leader.GetStatValue("Hitpoints")>4&&Ready("CommandRimeGrip")&&!_leader.HasEffect<FrozenEffect>()&&TryAim(_leader,5,out int rx,out int ry))
                {yield return Cast("CommandRimeGrip",_leader,rx,ry);Require(_leader.HasEffect<FrozenEffect>()||CombatSystem.IsDeathHandled(_leader),"real cutter freeze");continue;}
                if(SpatialQuery.Distance(Zone,Player,_leader)==1)
                {var cell=SpatialQuery.ClosestCell(Zone,_leader,At.X,At.Y);yield return Paid(Tap(Direction(cell.X-At.X,cell.Y-At.Y)),"native-dagger-attack");Require(_lastRows.Any(r=>r.Category=="damage"&&r.Kind=="HitRoll"&&r.ActorId==Player.ID&&r.TargetId==_leader.ID),"fresh native exact-target attack");_attacks++;}
                else{var path=PathTo(c=>SpatialQuery.DistanceToCell(Zone,_leader,c.X,c.Y)==1);Require(path!=null&&path.Count>0,"finite real cutter approach");yield return MoveTo(path[0].x,path[0].y,"native-combat-approach");}
            }
            throw new InvalidOperationException("Staged ordinary-kit encounter exceeded50 inputs without cutter defeat.");
        }
        bool Ready(string command)=>_originalSkills.TryGetValue(command,out Guid id)&&DensityCombatNativeEvidence.OwnsAbility(Player,id,command,out var ability)&&ability.IsUsable;
        bool TryAim(Entity target,int range,out int dx,out int dy)
        {
            dx=dy=0;if(target==null||Zone.GetEntityCell(target)==null||!Zone.GetOccupiedCells(target).Any(c=>c.IsVisible))return false;
            foreach(var d in Steps){var hits=SkillLine.Collect(Zone,Player,At.X,At.Y,d.x,d.y,range);if(hits.Count>0&&ReferenceEquals(hits[0],target)){dx=d.x;dy=d.y;return true;}}return false;
        }
        IEnumerator Cast(string command,Entity target,int dx,int dy)
        {
            Require(_originalSkills.TryGetValue(command,out Guid id),"original declared skill ID");Require(DensityCombatNativeEvidence.OwnsAbility(Player,id,command,out var ability)&&ability.IsUsable,"original current ready skill");
            int slot=Enumerable.Range(0,ActivatedAbilitiesPart.SlotCount).Single(i=>Player.GetPart<ActivatedAbilitiesPart>().GetAbilityBySlot(i)?.ID==id);
            yield return Tap((Key)Enum.Parse(typeof(Key),slot==9?"Digit0":"Digit"+(slot+1)));Require(State=="AwaitingDirection","native original skill aim");yield return Paid(Tap(Direction(dx,dy)),command);_casts++;
            Require(DensityCombatNativeEvidence.OwnsAbility(Player,id,command,out var after)&&ReferenceEquals(after,ability)&&after.CooldownRemaining>0&&_lastRows.Any(r=>r.Category=="skill"&&r.Kind=="CommandRouted"&&r.ActorId==Player.ID&&JObject.Parse(r.PayloadJson)["command"]?.Value<string>()==command),"actual original command and cooldown");
            _notes.Add("REAL CONTROL "+command+" target="+target.ID+" cooldown="+after.CooldownRemaining);
        }
        IEnumerator Paid(IEnumerator action,string label)
        {
            Require(_localInputs<100,"finite100 local action inputs");var actor=Player;int tick=Tick,energy=Energy,speed=Player.GetStatValue("Speed",100);
            Diag.Record("scenario",DensityCampaignNativeEvidence.MarkerKind,actor:Player,payload:new{runId=RunId,label});string marker=Diag.Snapshot(1).Single().TraceId;
            Diag.Record("scenario",ReferenceGladeCombatEvidence.MarkerKind,Player,_leader);string combat=Diag.Snapshot(1).Single().TraceId;
            yield return action;yield return CloseNormal();_lastRows=Diag.Snapshot(Diag.BufferCapacity).SkipWhile(r=>r.TraceId!=marker).ToArray();
            Require(ReferenceEquals(actor,Player)&&Player.GetStatValue("Speed",100)==speed,"same paid actor/speed");Require(DensityCampaignNativeEvidence.TryClock(_lastRows,marker,Player.ID,"local",energy,Energy,Tick-tick,speed,out var receipt),"actual paid action/clock "+label);
            var proof=ReferenceGladeCombatEvidence.Inspect(_lastRows,combat,Player.ID,_leader.ID);Require(proof.WindowValid,"current paired combat observation");_attempt|=proof.PlayerAttempt;_damage|=proof.PlayerDamage;_lethal|=proof.PlayerLethal;
            _localInputs++;_completedTurns+=receipt.CompletedTurns;_allActionRows.AddRange(_lastRows);_windows.Add(new{label,marker,combat,beforeTick=tick,afterTick=Tick,beforeEnergy=energy,afterEnergy=Energy,receipt,proof,rows=_lastRows});Observe(label);
        }
        bool Safe(Cell cell)
        {
            if(cell==null||!Zone.CanPlaceFootprint(Player,cell.X,cell.Y))return false;
            foreach(var c in Zone.GetOccupiedCells(Player,cell.X,cell.Y))
            {if(c.Occupants.Any(e=>e!=Player&&(e.HasTag("Creature")||e.HasPart<TriggerOnStepPart>()||e.HasPart<LiquidPoolPart>()||e.HasPart<GasPoolPart>()||e.HasEffect<BurningEffect>()||e.GetPart<ThermalPart>()?.IsAflame==true)))return false;
                var s=Zone.TileState.Get(c.X,c.Y);if(s!=null&&(s.Heat>0||s.Cold>0||s.Charge>0||!string.IsNullOrEmpty(s.Cloud)||s.Coatings.Count>0))return false;}return true;
        }
        IEnumerator MoveTo(int x,int y,string label)
        {Require(Math.Max(Math.Abs(x-At.X),Math.Abs(y-At.Y))==1&&Safe(Zone.GetCell(x,y)),"current adjacent native physical step");yield return Paid(Tap(Direction(x-At.X,y-At.Y)),label);Require(At.X==x&&At.Y==y,"actual keyboard destination");}
        List<(int x,int y)> PathTo(Func<Cell,bool> goal)
        {
            var start=At;var previous=new Dictionary<(int,int),(int,int)>();var q=new Queue<(int,int)>();previous[(start.X,start.Y)]=(start.X,start.Y);q.Enqueue((start.X,start.Y));
            while(q.Count>0){var p=q.Dequeue();if(goal(Zone.GetCell(p.Item1,p.Item2))){var path=new List<(int,int)>();while(p!=(start.X,start.Y)){path.Add(p);p=previous[p];}path.Reverse();return path;}
                foreach(var d in Steps){var n=(p.Item1+d.x,p.Item2+d.y);if(previous.ContainsKey(n)||!Safe(Zone.GetCell(n.Item1,n.Item2)))continue;if(d.x!=0&&d.y!=0&&!Zone.CanPlaceFootprint(Player,p.Item1+d.x,p.Item2)&&!Zone.CanPlaceFootprint(Player,p.Item1,p.Item2+d.y))continue;previous[n]=p;q.Enqueue(n);}}
            _notes.Add("NO PHYSICAL/HAZARD ROUTE "+At.X+","+At.Y);Observe("route-refusal");return null;
        }
        IEnumerator Reach(Cell target,int budget)
        {Require(target!=null,"current earned item source cell");for(int n=0;n<budget;n++){if(At==target)yield break;yield return UseTonic();var path=PathTo(c=>c==target);Require(path!=null&&path.Count>0,"current earned source route");yield return MoveTo(path[0].x,path[0].y,"native-earned-source-step");}throw new InvalidOperationException("Finite earned-source path exhausted.");}
        IEnumerator PickupExact(Entity item)
        {
            Require(Zone.GetEntityCell(item)==At,"exact earned item at feet");yield return Tap(Key.G);
            if(State=="PickupOpen")
            {var items=(List<Entity>)Field(_input.PickupUI,"_items");int row=items.FindIndex(e=>ReferenceEquals(e,item));Require(row>=0,"exact offered earned ground item");for(int i=0;(int)Field(_input.PickupUI,"_cursorIndex")!=row;i++){Require(i<30,"bounded pickup cursor");yield return Tap((int)Field(_input.PickupUI,"_cursorIndex")<row?Key.DownArrow:Key.UpArrow);}yield return Tap(Key.Enter);if(State=="PickupOpen")yield return Tap(Key.Escape);}
            Require(Owns(Player,item),"same actual earned owner acquired; native auto-equip allowed");
        }
        static int Units(Entity item)=>item.GetPart<StackerPart>()?.StackCount??1;
        static string Goals(Entity actor)=>string.Join("|",actor.GetPart<BrainPart>()?.GetGoalsSnapshot().Select(g=>g.GetType().Name+":"+g.Age+":"+g.GetDetails()+(g is NoFightGoal n?":"+n.Duration+":"+n.Wander:""))??Enumerable.Empty<string>());
        string Abilities(Entity actor)=>string.Join("|",actor.GetPart<ActivatedAbilitiesPart>()?.AbilityList.OrderBy(a=>a.ID).Select(a=>a.ID+":"+a.Command+":"+a.CooldownRemaining)??Enumerable.Empty<string>());
        string Ground()=>string.Join("|",Zone.GetReadOnlyEntities().Where(e=>e.HasTag("Item")||e.GetProperty("SourceID")==_leader.ID).OrderBy(e=>e.ID).Select(e=>e.ID+":"+e.BlueprintName+":"+Units(e)+":"+Zone.GetEntityPosition(e)));
        IEnumerator Checkpoint()
        {
            var oldPlayer=Player;var oldZone=Zone;var oldCap=_cap;var oldSword=_sword;var oldMate=_mate;string leaderID=_leader.ID,mateID=_mate.ID,capID=_cap.ID,swordID=_sword.ID;
            string gear=Gear(Player),stats=Stats(Player),abilities=Abilities(Player),mateStats=Stats(_mate),mateGear=Gear(_mate),mateAbilities=Abilities(_mate),mateGoals=Goals(_mate),ground=Ground();int drams=TradeSystem.GetDrams(Player),mateDrams=TradeSystem.GetDrams(_mate),mateEnergy=_input.TurnManager.GetEnergy(_mate);int x=At.X,y=At.Y,tick=Tick,energy=Energy,world=WorldClock.CurrentTick;var mateAt=Zone.GetEntityPosition(_mate);
            var info=SaveGameService.GetSaveInfo("Quick");Require(info!=null,"ordinary autosave metadata");string file=Path.Combine(_ownedRoot,info.GameID,"Quick.sav.gz"),before=HashFile(file);
            yield return Tap(Key.F5);yield return Settled();_checkpointHash=HashFile(file);Check("checkpoint_saved",before!=_checkpointHash&&MessageLog.GetLast()=="Game saved."&&SaveGameService.GetSaveInfo("Quick").ActiveZoneID==_zoneID);
            yield return ItemAction(_sword,"drop");yield return CloseNormal();Require(!Owns(Player,_sword)&&Zone.GetEntityCell(_sword)==At,"real earned sword dropped after save");var step=Steps.Select(d=>Zone.GetCell(At.X+d.x,At.Y+d.y)).FirstOrDefault(Safe);Require(step!=null,"real unsaved paid step");yield return MoveTo(step.X,step.Y,"real-checkpoint-mutation");
            Check("real_earned_item_mutation",!Owns(Player,_sword)&&Tick>tick&&HashFile(file)==_checkpointHash);yield return Tap(Key.F6);double began=Time.realtimeSinceStartupAsDouble;while(ReferenceEquals(Player,oldPlayer)){Require(Time.realtimeSinceStartupAsDouble-began<8,"F6 replaces actual actor graph");yield return null;}yield return Settled();
            _cap=Player.GetPart<InventoryPart>().Objects.Concat(Player.GetPart<InventoryPart>().EquippedItems.Values).FirstOrDefault(e=>e.ID==capID);_sword=Player.GetPart<InventoryPart>().Objects.Concat(Player.GetPart<InventoryPart>().EquippedItems.Values).FirstOrDefault(e=>e.ID==swordID);_mate=Zone.GetReadOnlyEntities().SingleOrDefault(e=>e.ID==mateID);
            Check("restored_exact_earned_graph",!ReferenceEquals(Zone,oldZone)&&Player.ID==oldPlayer.ID&&Zone.ZoneID==_zoneID&&ReferenceEquals(Manager.CachedZones[_zoneID],Zone)&&!Zone.GetReadOnlyEntities().Any(e=>e.ID==leaderID)&&_cap!=null&&!ReferenceEquals(_cap,oldCap)&&_sword!=null&&!ReferenceEquals(_sword,oldSword)&&Owns(Player,_cap)&&Owns(Player,_sword)&&_mate!=null&&!ReferenceEquals(_mate,oldMate)&&Zone.GetEntityPosition(_mate)==mateAt&&Stats(_mate)==mateStats&&Gear(_mate)==mateGear&&Abilities(_mate)==mateAbilities&&Goals(_mate)==mateGoals&&_input.TurnManager.GetEnergy(_mate)==mateEnergy&&TradeSystem.GetDrams(_mate)==mateDrams&&TradeSystem.GetDrams(Player)==drams&&Gear(Player)==gear&&Stats(Player)==stats&&Abilities(Player)==abilities&&Ground()==ground&&At.X==x&&At.Y==y&&Tick==tick&&Energy==energy&&WorldClock.CurrentTick==world&&HashFile(file)==_checkpointHash);
        }
        public void Initialize(ScenarioContext context)
        {
            Require(!string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride),"isolated launcher owns saves");_ownedRoot=SaveGameService.SaveRootOverride;_clock=System.Diagnostics.Stopwatch.StartNew();
            foreach(var channel in new[]{"scenario","event","ai","damage","turn","turn-verbose","skill"}){_oldChannels[channel]=Diag.IsChannelEnabled(channel);Diag.SetChannel(channel,true);}
            _oldSettings=InputSystem.settings;_settings=Instantiate(_oldSettings);_settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            _settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings=_settings;_oldBackground=Application.runInBackground;Application.runInBackground=true;_oldKeyboard=Keyboard.current;_keyboard=InputSystem.AddDevice<Keyboard>();StartCoroutine(RunSafely(RunAudit()));
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
                +";links="+string.Join("|",inv.EquippedItems.Values.Distinct().OrderBy(e=>e.ID).Select(e=>e.ID+":"+e.BlueprintName+":"+Units(e)+":"+e.GetPart<PhysicsPart>()?.Equipped?.ID));
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

        private static void Require(bool value,string reason){if(!value)throw new InvalidOperationException("First-hour staged encounter precondition: "+reason);}

        private void Check(string name,bool passed){if(!passed)_failures++;_audit.Add((passed?"PASS ":"FAIL ")+name);Observe(name);Diag.Record("scenario","FirstHourEncounterNativeCase",payload:new{runId=RunId,name,passed});}

        private IEnumerator RunSafely(IEnumerator routine)
        {
            var stack=new Stack<IEnumerator>();stack.Push(routine);
            try
            {
                while(stack.Count>0)
                {
                    bool moved=false;object next=null;Exception error=null;
                    try{moved=stack.Peek().MoveNext();if(moved)next=stack.Peek().Current;}catch(Exception e){error=e;}
                    if(error!=null){_fatal=error.ToString();Check("native_precondition_failed",false);Debug.LogError("[FirstHourEncounterNative] "+error);break;}
                    if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}if(next is IEnumerator child){stack.Push(child);continue;}yield return next;
                }
            }
            finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();Finish();}
        }

        public void SetUnexpectedErrors(int errors){_unexpectedErrors=errors;_errorsFinalized=true;WriteReport();EmitSummary();}

        public void Abort(string reason){if(Finished)return;_fatal=reason;Check("native_aborted",false);StopAllCoroutines();Finish();}

        void Cleanup(){if(_cleaned)return;_cleaned=true;if(_keyboard!=null){InputSystem.QueueStateEvent(_keyboard,new KeyboardState());InputSystem.RemoveDevice(_keyboard);}if(_oldKeyboard!=null&&_oldKeyboard.added)_oldKeyboard.MakeCurrent();if(_oldSettings!=null)InputSystem.settings=_oldSettings;if(_settings!=null)Destroy(_settings);Application.runInBackground=_oldBackground;}

        void EmitSummary(){if(_summaryEmitted)return;_summaryEmitted=true;Diag.Record("scenario","FirstHourEncounterNativeSummary",payload:new{runId=RunId,cases=_audit.Count,failures=Failures,complete=Complete});}

        void OnDestroy(){if(!Finished&&_clock!=null){_fatal="Play stopped before completion.";Check("native_interrupted",false);Finish();}Cleanup();EmitSummary();foreach(var p in _oldChannels)Diag.SetChannel(p.Key,p.Value);}
        void Observe(string phase)
        {
            if(_input?.PlayerEntity==null)return;
            _observations.Add(new{phase,player=Player.ID,zone=Zone.ZoneID,x=At?.X,y=At?.Y,hp=Player.GetStatValue("Hitpoints"),stats=Stats(Player),tick=Tick,energy=Energy,gear=Gear(Player),leader=_leader?.ID,leaderHP=_leader?.GetStatValue("Hitpoints"),leaderAt=_leader==null?null:(object)Zone.GetEntityPosition(_leader),mate=_mate?.ID,mateHP=_mate?.GetStatValue("Hitpoints"),mateAt=_mate==null?null:(object)Zone.GetEntityPosition(_mate),mateCalm=_mate!=null&&DensityCombatNativeEvidence.CurrentCalm(_mate,Zone,_calm),message=MessageLog.GetLast()});WriteReport();
        }
        void Finish(){Cleanup();Finished=true;WriteReport();}
        bool Complete=>Finished&&_errorsFinalized&&Failures==0&&_audit.Count==RequiredChecks.Length&&RequiredChecks.All(n=>_audit.Contains("PASS "+n))&&_screenshots.Count>=5;
        void WriteReport()
        {
            Directory.CreateDirectory(DirectoryPath);ReportPath=Path.Combine(DirectoryPath,"report.json");
            string json=JsonConvert.SerializeObject(new{runId=RunId,complete=Complete,failures=Failures,unexpectedErrors=_unexpectedErrors,seconds=_clock?.Elapsed.TotalSeconds??0,fatal=_fatal,audit=_audit,requiredChecks=RequiredChecks,passedChecks=RequiredChecks.Where(n=>_audit.Contains("PASS "+n)).ToArray(),unmetChecks=RequiredChecks.Where(n=>!_audit.Contains("PASS "+n)).ToArray(),keys=_keys,observations=_observations,windows=_windows,notes=_notes,screenshots=_screenshots,localInputs=_localInputs,completedPlayerTurns=_completedTurns,originalTonicsUsed=_tonics,originalControlCasts=_casts,daggerAttacks=_attacks,checkpointHash=_checkpointHash,
                observedTactics=_allActionRows.Where(r=>r.Category=="ai"&&r.Kind=="TacticUsed").ToArray(),
                intendedCapability="Explicit factory-pair/hedge fixture; original normal starter, native bypass and real controls/dagger combat, exact authored gear pickup/Compare and F5/drop/step/F6 graph restoration.",
                canVerify="Only named passedChecks observed in this run. Staging and one setup transfer are explicit; no manual enemy/player stats, AI or RNG overrides; native entry retains its normal initialization. No forced hits or gear/currency grants.",
                cannotVerify="Not natural discovery, population rarity, all generated-site safety or broad balance. Lunge/assistance observed opportunistically, not forced. Actual random misses/flight/survival may stop the finite route. Screenshots require visual review."},Formatting.Indented);
            var parsed=JObject.Parse(json);Require(parsed["windows"] is JArray w&&w.Count==_windows.Count&&parsed["observations"] is JArray o&&o.Count==_observations.Count,"nested report retained");File.WriteAllText(ReportPath,json);
        }
    }
}
