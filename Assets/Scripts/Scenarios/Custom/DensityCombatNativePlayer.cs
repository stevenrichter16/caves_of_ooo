using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using CavesOfOoo.Skills;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Three declared ordinary starter policies, each in its own native
    /// new game. Observes real enemy tactics and player commands, never supplies
    /// skills, resources, faction, enemy state, cooldowns or simulation turns.</summary>
    public sealed class DensityCombatNativePlayer : MonoBehaviour
    {
        private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        private static readonly (int x,int y)[] Steps={(1,0),(-1,0),(0,1),(0,-1),(1,1),(1,-1),(-1,1),(-1,-1)};
        public string RunId{get;}=Guid.NewGuid().ToString("N");
        public string Policy{get;private set;}
        public bool Finished{get;private set;}
        public int Failures=>_failures+_unexpectedErrors;
        public string ReportPath{get;private set;}
        private InputHandler _input;private Keyboard _keyboard,_oldKeyboard;private InputSettings _settings,_oldSettings;
        private bool _oldBackground,_started,_cleaned,_errorsFinalized,_summaryEmitted;
        private readonly Dictionary<string,bool> _channels=new Dictionary<string,bool>();
        private readonly List<string> _audit=new List<string>(),_screenshots=new List<string>(),_notes=new List<string>();
        private readonly List<object> _keys=new List<object>(),_observations=new List<object>(),_windows=new List<object>();
        private readonly Dictionary<string,Entity> _startingTonics=new Dictionary<string,Entity>();
        private readonly Dictionary<string,Guid> _originalSkills=new Dictionary<string,Guid>(),_enemySkills=new Dictionary<string,Guid>();
        private System.Diagnostics.Stopwatch _clock;private EntityMovedVisualHandler _moveObserver;
        private int _failures,_unexpectedErrors,_moves,_attacks,_tonics,_casts,_paidActions,_nativeActionInputs,_coins;
        private string _fatal,_saveRoot,_sourceZoneId,_targetId,_usedCommand,_checkpointHash;
        private Entity _target,_dagger;private Zone _source;private Cell _approach;
        private bool _used,_fallback,_primary,_playerDamage;private NoFightGoal _calm;
        private string _lastMarker,_lastCombatMarker;private Diag.Entry[] _lastRows;
        private Entity Player=>_input.PlayerEntity;private Zone Zone=>_input.CurrentZone;
        private OverworldZoneManager Manager=>_input.ZoneManager as OverworldZoneManager;
        private Cell At=>Zone.GetEntityCell(Player);private int Tick=>_input.TurnManager.TickCount;
        private int Energy=>_input.TurnManager.GetEnergy(Player);private int Ambient=>Player.GetIntProperty(WorldAmbience.TurnProperty);
        private string State=>Field(_input,"_inputState").ToString();
        private string DirectoryPath=>Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/DensityCompletion/Combat/NativeAcceptance",RunId));
        private bool RangedSource=>Policy=="ranged"||Policy=="ranged-threat";
        private string[] RequiredChecks=>new[]{"ordinary_start_and_original_skills","actual_generated_tactic_owner","labelled_approach_scheduler",
            "native_original_dagger_equipped","ordinary_living_finish"}
            .Concat(Policy=="ranged"?new[]{"native_ranged_damage"}:new[]{"exact_enemy_tactic_turn","cooldown_ordinary_fallback"})
            .Concat(Policy=="melee"||Policy=="control"?new[]{"primary_policy_action"}:Array.Empty<string>())
            .Concat(Policy=="control"?new[]{"native_calm_paid_current_goal","three_live_controlled_actions","checkpoint_saved","real_checkpoint_mutation","checkpoint_replaces_combat_graph"}:Array.Empty<string>()).ToArray();

        public void Initialize(ScenarioContext context,string policy)
        {
            Require(new[]{"melee","ranged","ranged-threat","control"}.Contains(policy),"declared starter policy");Policy=policy;
            Require(!string.IsNullOrEmpty(SaveGameService.SaveRootOverride),"isolated launcher save root");
            _saveRoot=SaveGameService.SaveRootOverride;_clock=System.Diagnostics.Stopwatch.StartNew();
            foreach(string name in new[]{"scenario","ai","skill","damage","event","turn","turn-verbose"})
            {_channels[name]=Diag.IsChannelEnabled(name);Diag.SetChannel(name,true);}
            _oldSettings=InputSystem.settings;_settings=Instantiate(_oldSettings);
            _settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            _settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings=_settings;_oldBackground=Application.runInBackground;Application.runInBackground=true;
            _oldKeyboard=Keyboard.current;_keyboard=InputSystem.AddDevice<Keyboard>();
            _moveObserver=(owner,zone,ox,oy,nx,ny,forced)=>
            {
                if(_target==null||owner!=_target||_input==null||zone!=Zone)return;
                Diag.Record("scenario",DensityCombatNativeEvidence.MoveKind,owner,payload:new
                    {scheduledActor=_input.TurnManager.CurrentActor?.ID,forced,oldX=ox,oldY=oy,newX=nx,newY=ny});
            };
            EntityVisualHooks.MovedCallback+=_moveObserver;
            StartCoroutine(RunSafely(RunAudit()));
        }
        private IEnumerator RunAudit()
        {
            yield return new WaitForSecondsRealtime(.8f);_input=FindFirstObjectByType<InputHandler>();Require(_input!=null,"ordinary bootstrap");
            var boot=(BootMenuController)Field(_input,"_bootMenuController");Require(boot!=null&&boot.IsActive,"ordinary N menu");
            yield return Tap(Key.N);yield return Settled();_started=true;
            Require(Manager!=null&&Manager.WorldSeed==64,"declared current world seed64");
            _coins=TradeSystem.GetDrams(Player);_dagger=Player.GetPart<InventoryPart>().Objects.Single(e=>e.BlueprintName=="Dagger");
            foreach(var e in Player.GetPart<InventoryPart>().Objects.Where(e=>e.BlueprintName=="HealingTonic"))_startingTonics.Add(e.ID,e);
            foreach(var a in Player.GetPart<ActivatedAbilitiesPart>().AbilityList)_originalSkills.Add(a.Command,a.ID);
            string[] expected={"CommandEmberSpit","CommandFlamingHands","CommandJetBlast","CommandGroundSurge","CommandRimeGrip","CommandCalm"};
            Check("ordinary_start_and_original_skills",Ordinary()&&Player.GetStat("Hitpoints").Max==40&&Player.GetStatValue("Hitpoints")==40
                &&Player.GetStatValue("Strength")==18&&Player.GetStatValue("Agility")==18&&Player.GetStatValue("Toughness")==18
                &&_coins==50&&TonicUnits()==2&&expected.All(c=>_originalSkills.ContainsKey(c)&&DensityCombatNativeEvidence.OwnsAbility(Player,_originalSkills[c],c,out _)));
            yield return Capture("01-ordinary-"+Policy);FindSource();
            Check("actual_generated_tactic_owner",SourceReady(_target,_source)&&ReferenceEquals(Manager.CachedZones[_sourceZoneId],_source));
            string stats=Stats(Player),gear=Gear(Player);int tick=Tick,energy=Energy;var from=Zone;
            Require(Safe(_source,_approach,_target,5),"fresh actual approach geometry/hazards/unrelated threats");
            Require(from.TryTransferEntityTo(Player,_source,_approach.X,_approach.Y),"single labelled player approach");
            typeof(InputHandler).GetMethod("HandleZoneTransition",Fields).Invoke(_input,new object[]{new ZoneTransitionResult
                {Success=true,NewZone=_source,NewPlayerX=_approach.X,NewPlayerY=_approach.Y}});
            _input.CameraFollow?.SnapToPlayer();ZoneRenderHooks.MarkFullDirty("DensityCombatNativeApproach");yield return Settled();
            _notes.Add("ONE LABELLED PLAYER APPROACH "+from.ZoneID+"→"+_sourceZoneId+"@"+_approach.X+","+_approach.Y+" target="+_targetId+". No NPC or gameplay state was staged.");
            Check("labelled_approach_scheduler",Zone==_source&&Stats(Player)==stats&&Gear(Player)==gear&&Tick==tick&&Energy==energy
                &&_input.TurnManager.IsRegistered(_target)&&_target.GetPart<BrainPart>().CurrentZone==Zone);
            yield return ItemAction(_dagger,"equip_auto");yield return CloseInventory();
            Check("native_original_dagger_equipped",_dagger.GetPart<PhysicsPart>()?.Equipped==Player&&Player.GetPart<InventoryPart>().EquippedItems.Values.Contains(_dagger));
            yield return Encounter();
            if(Policy=="ranged")Check("native_ranged_damage",_primary);
            else {Check("exact_enemy_tactic_turn",_used);Check("cooldown_ordinary_fallback",_fallback);}
            if(Policy=="melee"||Policy=="control")Check("primary_policy_action",_primary);
            yield return Capture("02-actual-"+Policy+"-witnesses");
            if(Policy=="control")
            {
                Require(DensityCombatNativeEvidence.CurrentCalm(_target,Zone,_calm),"actual current Calm retained for checkpoint");
                for(int i=0;i<3;i++)
                {
                    Require(DensityCombatNativeEvidence.CurrentCalm(_target,Zone,_calm),"control active before paid observation");
                    int age=_calm.Age;var targetCell=Zone.GetEntityCell(_target);
                    yield return ActionKey(Key.Period,"calm-observation");
                    Require(DensityCombatNativeEvidence.CurrentCalm(_target,Zone,_calm)&&ReferenceEquals(Zone.GetEntityCell(_target),targetCell)
                        &&_calm.Age>age&&!_lastRows.Any(e=>e.TraceId!=_lastMarker&&e.ActorId==_targetId
                            &&((e.Category=="ai"&&e.Kind=="TacticUsed")||(e.Category=="damage"&&e.Kind=="HitRoll"&&e.TargetId==Player.ID))),"same live stationary goal actually suppresses combat");
                }
                Check("three_live_controlled_actions",true);yield return Capture("03-actual-calm");yield return Checkpoint();
            }
            Check("ordinary_living_finish",Ordinary()&&Player.GetStatValue("Hitpoints")>10&&TradeSystem.GetDrams(Player)==_coins
                &&Player.GetPart<InventoryPart>().EquippedItems.Values.Any(e=>e.ID==_dagger.ID));
            yield return Capture("04-ordinary-finish");
        }
        private void FindSource()
        {
            int scanned=0;int depth=RangedSource?3:1;
            for(int y=0;y<WorldMap.Height&&scanned<12;y++)for(int x=0;x<WorldMap.Width&&scanned<12;x++)
            {
                var biome=Manager.WorldMap.GetBiome(x,y);if(Manager.WorldMap.GetPOI(x,y)!=null
                    ||(biome!=BiomeType.Spread&&biome!=BiomeType.Sodden&&biome!=BiomeType.Beating))continue;
                string id=WorldMap.ToZoneID(x,y,depth);
                var layout=UndergroundLayoutPlan.Select(Manager.WorldSeed,id,biome,null,Manager.Factory);
                if(layout==null||!layout.IsOrdinaryColumn)continue;scanned++;
                // Generate the actual column and its native stairs; no entry or
                // encounter rolls are invoked by the observer directly.
                Zone zone=null;for(int d=0;d<=depth;d++){zone=Manager.GetZone(WorldMap.ToZoneID(x,y,d));if(zone==null)break;}
                if(zone==null){_notes.Add("SOURCE "+id+" generation refused");continue;}
                var candidates=zone.GetReadOnlyEntities().Where(e=>RangedSource?e.BlueprintName=="IceWight"
                    :e.BlueprintName=="MarlbackGleaner"||e.BlueprintName=="MarlbackTunnelguard").OrderBy(e=>e.ID,StringComparer.Ordinal).ToArray();
                _notes.Add("SOURCE "+id+" biome="+biome+" depthTableTier="+(depth/3+1)+" candidates="+string.Join(",",candidates.Select(e=>e.ID+":"+e.BlueprintName)));
                foreach(var target in candidates)
                {
                    if(!SourceReady(target,zone)){_notes.Add("REFUSE "+target.ID+" current actor/kit/hostility");continue;}
                    var pos=zone.GetEntityCell(target);var up=zone.GetReadOnlyEntities().FirstOrDefault(e=>e.HasPart<StairsUpPart>());
                    if(up==null){_notes.Add("REFUSE "+target.ID+" no actual up stair");continue;}
                    for(int cy=Math.Max(1,pos.Y-7);cy<=Math.Min(Zone.Height-2,pos.Y+7);cy++)for(int cx=Math.Max(1,pos.X-7);cx<=Math.Min(Zone.Width-2,pos.X+7);cx++)
                    {
                        int distance=Math.Max(Math.Abs(cx-pos.X),Math.Abs(cy-pos.Y));if(distance<5||distance>7)continue;
                        var cell=zone.GetCell(cx,cy);if(!Safe(zone,cell,target,5)||!AIHelpers.HasLineOfSight(zone,cx,cy,pos.X,pos.Y))continue;
                        var path=SafePath(zone,cell,c=>SpatialQuery.DistanceToCell(zone,target,c.X,c.Y)==1,target);
                        if(path==null||path.Count>16)continue;
                        var upCell=zone.GetEntityCell(up);
                        var entryPath=upCell==null||!Safe(zone,upCell,target,3)?null:SafePath(zone,upCell,c=>c==cell,target);
                        if(entryPath==null){_notes.Add("REFUSE approach "+cx+","+cy+" no current physical route from real up stair "+up.ID);continue;}
                        _target=target;_targetId=target.ID;_source=zone;_sourceZoneId=id;_approach=cell;
                        foreach(var ability in target.GetPart<ActivatedAbilitiesPart>().AbilityList)
                            if(target.GetPart<SkillsPart>().SkillList.Any(s=>s.ActivatedAbilityID==ability.ID))_enemySkills.Add(ability.Command,ability.ID);
                        _notes.Add("FIRST CURRENT QUALIFYING SOURCE "+id+":"+_targetId+"@"+pos.X+","+pos.Y+" HP="+target.GetStatValue("Hitpoints")
                            +" speed="+target.GetStatValue("Speed")+" stats="+Stats(target)+" gear="+Gear(target)+" abilities="+Abilities(target)+" actualUp="+up.ID+" approach="+cx+","+cy+" path="+path.Count+" stairRoute="+entryPath.Count);
                        WriteReport();return;
                    }
                    _notes.Add("REFUSE "+target.ID+" no current bounded quiet approach");
                }
            }
            WriteReport();throw new InvalidOperationException("No qualifying actual C2 source in12 ordinary columns at depth"+depth+"; no source was rerolled.");
        }
        private bool SourceReady(Entity target,Zone zone)
        {
            if(target==null||zone==null||target.GetStatValue("Hitpoints")<=0||CombatSystem.IsDeathHandled(target)||!Hostile(target))return false;
            var cell=zone.GetEntityCell(target);var brain=target.GetPart<BrainPart>();var tactics=target.GetPart<CombatTacticsPart>();
            if(cell==null||!cell.Objects.Contains(target)||brain==null||tactics==null||tactics.ParentEntity!=target
                ||target.GetPart<PhysicsPart>()?.InInventory!=null||target.GetPart<PhysicsPart>()?.Equipped!=null||brain.HasGoal<NoFightGoal>())return false;
            return target.GetPart<ActivatedAbilitiesPart>()?.AbilityList.Any(a=>a!=null
                &&DensityCombatNativeEvidence.OwnsAbility(target,a.ID,a.Command,out _))==true;
        }
        private IEnumerator Encounter()
        {
            for(int step=0;step<60;step++)
            {
                yield return Settled();Require(Ordinary()&&Player.GetStatValue("Hitpoints")>10,"ordinary living safety stop");
                // Distinct bounded observations: taking a lethal fire opening
                // and waiting through IceLance are incompatible on this source.
                if((Policy=="ranged"&&_primary)||(Policy=="ranged-threat"&&_used&&_fallback)
                    ||((Policy=="melee"||Policy=="control")&&_used&&_fallback&&_primary))yield break;
                Require(_target.GetStatValue("Hitpoints")>0&&!CombatSystem.IsDeathHandled(_target)&&Zone.GetEntityCell(_target)!=null,"same source remains alive until missing witnesses complete");
                if(Player.GetPart<StatusEffectsPart>()?.IsActionBlocked()==true)
                {
                    Require(_paidActions<60,"finite blocked-turn bound");
                    var from=At;yield return ActionKey(Key.Period,"ordinary-blocked-observation");
                    Require(At==from,"blocked native wait remains stationary");continue;
                }
                yield return UseOriginalTonic();
                Require(_paidActions<60&&_moves<40,"finite encounter action bounds");
                if(Policy=="control"&&_used&&_fallback&&!_primary&&TryAim(At,_target,6,out int cdx,out int cdy))
                {
                    Require(!_target.GetPart<BrainPart>().HasGoal<NoFightGoal>(),"no existing Calm before exact cast");
                    yield return Cast("CommandCalm",cdx,cdy);
                    _calm=_target.GetPart<BrainPart>().PeekGoal() as NoFightGoal;
                    _primary=DensityCombatNativeEvidence.CurrentCalm(_target,Zone,_calm);
                    Check("native_calm_paid_current_goal",_primary);continue;
                }
                if(Policy=="ranged"&&!_primary&&SpatialQuery.Distance(Zone,Player,_target)>=2&&TryAim(At,_target,4,out int rdx,out int rdy))
                {
                    yield return Cast("CommandEmberSpit",rdx,rdy);
                    _primary=_playerDamage;continue;
                }
                if(Policy=="melee"&&!_primary&&SpatialQuery.Distance(Zone,Player,_target)==1)
                {
                    var to=SpatialQuery.ClosestCell(Zone,_target,At.X,At.Y);yield return ActionKey(Direction(to.X-At.X,to.Y-At.Y),"melee");
                    _attacks++;var result=ReferenceGladeCombatEvidence.Inspect(_lastRows,_lastCombatMarker,Player.ID,_targetId);
                    Require(result.WindowValid&&result.PlayerAttempt,"same target native melee dispatch");_primary|=result.PlayerDamage;continue;
                }
                // The threat-only policy ends immediately on both enemy
                // witnesses, even if the ordinary player is still frozen.
                bool observingRangedEnemy=Policy=="ranged-threat";
                bool needsCastingPosition=(Policy=="ranged"&&!_primary&&!observingRangedEnemy)||(Policy=="control"&&_used&&_fallback&&!_primary);
                bool inRange=!needsCastingPosition&&(RangedSource?TryAim(At,_target,6,out _,out _):SpatialQuery.Distance(Zone,Player,_target)<=2);
                if(inRange&&Safe(Zone,At,_target,3)){yield return ActionKey(Key.Period,"ordinary-observe-enemy");continue;}
                var path=SafePath(Zone,At,c=>RangedSource?SpatialQuery.DistanceToCell(Zone,_target,c.X,c.Y)>=2&&TryAim(c,_target,observingRangedEnemy?6:4,out _,out _)
                    :needsCastingPosition?TryAim(c,_target,6,out _,out _):SpatialQuery.DistanceToCell(Zone,_target,c.X,c.Y)==1,_target);
                Require(path!=null&&path.Count>0,"current physical safe approach/casting route");yield return PaidStep(path[0],_target);_moves++;
            }
            throw new InvalidOperationException("Finite C2 encounter ended without all actual policy/tactic/fallback witnesses.");
        }
        private IEnumerator Cast(string command,int dx,int dy)
        {
            Require(_originalSkills.TryGetValue(command,out Guid id),"original declared starter ability");
            Require(DensityCombatNativeEvidence.OwnsAbility(Player,id,command,out var ability)&&ability.IsUsable,"original owned ready starter ability");
            int slot=Enumerable.Range(0,ActivatedAbilitiesPart.SlotCount).Where(i=>Player.GetPart<ActivatedAbilitiesPart>().GetAbilityBySlot(i)?.ID==id).DefaultIfEmpty(-1).First();
            Require(slot>=0,"actual original ability slot");int hp=_target.GetStatValue("Hitpoints");
            yield return Tap((Key)Enum.Parse(typeof(Key),slot==9?"Digit0":"Digit"+(slot+1)));Require(State=="AwaitingDirection","native ready spell direction prompt");
            yield return ActionKey(Direction(dx,dy),command);_casts++;
            Require(DensityCombatNativeEvidence.OwnsAbility(Player,id,command,out var current)&&ReferenceEquals(current,ability)&&ability.CooldownRemaining>0
                &&_lastRows.Any(r=>r.Category=="skill"&&r.Kind=="CommandRouted"&&r.ActorId==Player.ID&&Command(r)==command),"same original ability actually dispatched and cooled");
            if(command=="CommandEmberSpit")
                _playerDamage=hp>_target.GetStatValue("Hitpoints")&&_lastRows.Any(r=>r.Category=="damage"&&r.Kind=="DamageDealt"&&r.ActorId==Player.ID&&r.TargetId==_targetId&&PositiveDamage(r));
        }
        private bool TryAim(Cell from,Entity target,int range,out int dx,out int dy)
        {
            dx=dy=0;if(from==null||target==null||Zone.GetEntityCell(target)==null||target.GetPart<RenderPart>()?.Visible!=true||!Zone.GetOccupiedCells(target).Any(c=>c.IsVisible))return false;
            foreach(var d in Steps)
            {
                var hits=SkillLine.Collect(Zone,Player,from.X,from.Y,d.x,d.y,range);
                if(hits.Count>0&&ReferenceEquals(hits[0],target)){dx=d.x;dy=d.y;return true;}
            }
            return false;
        }
        private IEnumerator ActionKey(Key key,string kind)
        {
            int tick=Tick,energy=Energy,ambient=Ambient,speed=Player.GetStatValue("Speed",100),enemyEnergy=_input.TurnManager.GetEnergy(_target),enemySpeed=_target.GetStatValue("Speed",100);
            var cooldowns=_enemySkills.ToDictionary(p=>p.Key,p=>_target.GetPart<ActivatedAbilitiesPart>().GetAbility(p.Value)?.CooldownRemaining??-1);
            Diag.Record("scenario",DensityCombatNativeEvidence.MarkerKind,Player,_target,new{runId=RunId,policy=Policy,kind});_lastMarker=Diag.Snapshot(1).Single().TraceId;
            Diag.Record("scenario",ReferenceGladeCombatEvidence.MarkerKind,Player,_target,new{runId=RunId,kind});_lastCombatMarker=Diag.Snapshot(1).Single().TraceId;
            try
            {
                yield return Tap(key);yield return Settled();
                _lastRows=Diag.Snapshot(Diag.BufferCapacity).SkipWhile(r=>r.TraceId!=_lastMarker).ToArray();
                int enemyEnds=DensityCombatNativeEvidence.CountEnds(_lastRows,_lastMarker,Player.ID,_targetId,_targetId);
                int playerEnds=-1;bool scheduledPlayerHasZone=ReferenceEquals(Player.GetPart<BrainPart>()?.CurrentZone,Zone);
                Require(Player.GetStatValue("Speed",100)==speed
                    &&DensityCombatNativeEvidence.TryPaidPlayerInput(_lastRows,_lastMarker,Player.ID,_targetId,
                        energy,Energy,Tick-tick,speed,Ambient-ambient,scheduledPlayerHasZone,out playerEnds),
                    "one native input and exact paid player turns including automatic blocked actions");
                _paidActions+=playerEnds;_nativeActionInputs++;
                if(_input.TurnManager.IsRegistered(_target))Require(enemyEnds>=0&&_target.GetStatValue("Speed",100)==enemySpeed
                    &&DensityCombatNativeEvidence.EnergyMatches(enemyEnergy,_input.TurnManager.GetEnergy(_target),Tick-tick,enemySpeed,enemyEnds),"exact enemy actor turns/energy");
                foreach(var pair in _enemySkills)
                {
                    if(!DensityCombatNativeEvidence.OwnsAbility(_target,pair.Value,pair.Key,out var ability))continue;
                    if(DensityCombatNativeEvidence.TryTacticTurn(_lastRows,_lastMarker,Player.ID,_targetId,pair.Key,out var used))
                    {_used=true;_usedCommand=pair.Key;_notes.Add("ACTUAL ENEMY TACTIC "+JsonConvert.SerializeObject(used));}
                    if(_usedCommand==pair.Key&&(DensityCombatNativeEvidence.TryCooldownFallback(_lastRows,_lastMarker,Player.ID,_targetId,pair.Key,cooldowns[pair.Key],ability.CooldownRemaining,out var fallback)
                        ||DensityCombatNativeEvidence.TryTacticThenCooldownFallback(_lastRows,_lastMarker,Player.ID,_targetId,pair.Key,ability.CooldownRemaining,out fallback)))
                    {_fallback=true;_notes.Add("ACTUAL SAME-ABILITY COOLDOWN FALLBACK "+JsonConvert.SerializeObject(fallback));}
                }
            }
            finally
            {
                _lastRows=Diag.Snapshot(Diag.BufferCapacity).SkipWhile(r=>r.TraceId!=_lastMarker).ToArray();
                _windows.Add(new{marker=_lastMarker,kind,rows=_lastRows,state=Snapshot("action-end")});WriteReport();
            }
        }
        private IEnumerator UseOriginalTonic()
        {
            var hp=Player.GetStat("Hitpoints");if(_tonics>=2||hp.Value*3>hp.Max*2)yield break;
            var item=_startingTonics.Values.FirstOrDefault(e=>Player.GetPart<InventoryPart>().Objects.Contains(e)&&e.GetPart<PhysicsPart>()?.InInventory==Player);
            if(item==null)yield break;int before=TonicUnits(),tick=Tick,energy=Energy;
            Diag.Record("scenario",ReferenceGladeCombatEvidence.SupportMarkerKind,Player,Player,new{runId=RunId,item=item.ID});string marker=Diag.Snapshot(1).Single().TraceId;
            yield return ItemAction(item,"ApplyTonic");yield return CloseInventory();
            Require(ReferenceGladeCombatEvidence.HasConsumedTonic(Diag.Snapshot(Diag.BufferCapacity),marker,Player.ID,item.ID,before,TonicUnits())
                &&Tick==tick&&Energy==energy,"actual original tonic consumption under current free native action");_tonics++;
        }
        private IEnumerator Checkpoint()
        {
            var actor=Player;var target=_target;var oldZone=Zone;var at=At;var tc=Zone.GetEntityCell(target);
            string playerStats=Stats(actor),targetStats=Stats(target),gear=Gear(actor),targetGear=Gear(target),abilities=Abilities(actor),enemyAbilities=Abilities(target),goals=Goals(target);
            int tick=Tick,energy=Energy,enemyEnergy=_input.TurnManager.GetEnergy(target),ambient=Ambient,world=WorldClock.CurrentTick,x=at.X,y=at.Y,tx=tc.X,ty=tc.Y;
            var metadata=SaveGameService.GetSaveInfo("Quick");Require(metadata!=null,"ordinary real quicksave metadata");
            string game=metadata.GameID,path=Path.Combine(_saveRoot,game,"Quick.sav.gz"),before=HashFile(path);
            long serial=MessageLog.NextSerialValue;yield return Tap(Key.F5);yield return Settled();_checkpointHash=HashFile(path);
            Check("checkpoint_saved",_checkpointHash!=before&&MessageLog.NextSerialValue>serial&&MessageLog.GetLast()=="Game saved."
                &&SaveGameService.GetSaveInfo("Quick")?.ActiveZoneID==_sourceZoneId);
            var next=Steps.Select(d=>Zone.GetCell(At.X+d.x,At.Y+d.y)).FirstOrDefault(c=>Safe(Zone,c,_target,3));
            Require(next!=null,"safe native checkpoint movement");yield return PaidStep(next,_target);
            Check("real_checkpoint_mutation",(At.X!=x||At.Y!=y)&&Tick>tick&&Goals(target)!=goals&&Abilities(actor)!=abilities&&HashFile(path)==_checkpointHash);
            yield return Tap(Key.F6);double began=Time.realtimeSinceStartupAsDouble;
            while(ReferenceEquals(Player,actor)){Require(Time.realtimeSinceStartupAsDouble-began<8,"F6 replaces player graph");yield return null;}
            yield return Settled();_target=Zone.GetReadOnlyEntities().SingleOrDefault(e=>e.ID==_targetId);var restoredCell=_target==null?null:Zone.GetEntityCell(_target);
            _calm=_target?.GetPart<BrainPart>()?.PeekGoal() as NoFightGoal;
            Check("checkpoint_replaces_combat_graph",Zone!=oldZone&&Player.ID==actor.ID&&_target!=null&&_target!=target
                &&At.X==x&&At.Y==y&&restoredCell?.X==tx&&restoredCell?.Y==ty&&Stats(Player)==playerStats&&Stats(_target)==targetStats
                &&Gear(Player)==gear&&Gear(_target)==targetGear&&Abilities(Player)==abilities&&Abilities(_target)==enemyAbilities&&Goals(_target)==goals
                &&Tick==tick&&Energy==energy&&_input.TurnManager.GetEnergy(_target)==enemyEnergy&&Ambient==ambient&&WorldClock.CurrentTick==world
                &&HashFile(path)==_checkpointHash&&DensityCombatNativeEvidence.CurrentCalm(_target,Zone,_calm)
                &&_originalSkills.All(p=>DensityCombatNativeEvidence.OwnsAbility(Player,p.Value,p.Key,out _))
                &&_enemySkills.All(p=>DensityCombatNativeEvidence.OwnsAbility(_target,p.Value,p.Key,out _)));
        }
        private List<Cell> SafePath(Zone zone,Cell start,Func<Cell,bool> goal,Entity ignoredThreat)
        {
            var previous=new Dictionary<(int,int),(int,int)>();var origin=(start.X,start.Y);previous[origin]=origin;var queue=new Queue<(int,int)>();queue.Enqueue(origin);
            var threats=Threats(zone,ignoredThreat);var visited=new HashSet<(int,int)>{origin};
            while(queue.Count>0)
            {
                var at=queue.Dequeue();if(goal(zone.GetCell(at.Item1,at.Item2)))
                {var result=new List<Cell>();while(at!=origin){result.Add(zone.GetCell(at.Item1,at.Item2));at=previous[at];}result.Reverse();return result;}
                foreach(var d in Steps)
                {
                    var next=(at.Item1+d.x,at.Item2+d.y);if(visited.Contains(next))continue;
                    if(!Safe(zone,zone.GetCell(next.Item1,next.Item2),ignoredThreat,3,threats)){visited.Add(next);continue;}
                    if(d.x!=0&&d.y!=0&&!zone.CanPlaceFootprint(Player,at.Item1+d.x,at.Item2)&&!zone.CanPlaceFootprint(Player,at.Item1,at.Item2+d.y))continue;
                    visited.Add(next);previous[next]=at;queue.Enqueue(next);
                }
            }
            return null;
        }
        private Entity[] Threats(Zone zone,Entity ignoredThreat)=>zone.GetReadOnlyEntities().Where(e=>e!=Player&&e!=ignoredThreat&&e.HasTag("Creature")
            &&e.GetStatValue("Hitpoints")>0&&!CombatSystem.IsDeathHandled(e)&&Hostile(e)).ToArray();
        private bool Safe(Zone zone,Cell cell,Entity ignoredThreat,int radius,Entity[] threats=null)
        {
            if(cell==null||!zone.CanPlaceFootprint(Player,cell.X,cell.Y))return false;threats??=Threats(zone,ignoredThreat);
            foreach(var c in zone.GetOccupiedCells(Player,cell.X,cell.Y))
            {
                if(c==null||c.Occupants.Any(e=>e!=Player&&(e.HasTag("Creature")||e.HasPart<TriggerOnStepPart>()||e.HasPart<GasPoolPart>()
                    ||e.HasPart<LiquidPoolPart>()||e.HasEffect<BurningEffect>()||e.GetPart<ThermalPart>()?.IsAflame==true)))return false;
                var state=zone.TileState.Get(c.X,c.Y);if(state!=null&&(state.Heat>0||state.Cold>0||state.Charge>0||!string.IsNullOrEmpty(state.Cloud)||state.Coatings.Count>0))return false;
                if(threats.Any(e=>SpatialQuery.DistanceToCell(zone,e,c.X,c.Y)<=radius))return false;
            }
            return true;
        }
        private bool Hostile(Entity e)=>FactionManager.IsHostile(e,Player)||FactionManager.IsHostile(Player,e)
            ||e.GetPart<BrainPart>()?.IsPersonallyHostileTo(Player)==true||ReferenceEquals(e.GetPart<BrainPart>()?.Target,Player);
        private IEnumerator PaidStep(Cell destination,Entity ignoredThreat)
        {
            Require(Safe(Zone,destination,ignoredThreat,3),"fresh physical safe step");var from=At;
            yield return ActionKey(Direction(destination.X-from.X,destination.Y-from.Y),"movement");Require(At==destination,"actual key reaches selected cell");
        }
        private int TonicUnits()=>_startingTonics.Values.Where(e=>Player.GetPart<InventoryPart>().Objects.Contains(e)&&e.GetPart<PhysicsPart>()?.InInventory==Player).Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);
        private bool Ordinary()=>!DevMode.Enabled&&!DebugInvincibility.IsEnabled(Player)&&!Player.HasPart<BitLockerPart>()&&!CombatSystem.IsDeathHandled(Player);
        private static string Stats(Entity e)=>string.Join("|",e.Statistics.OrderBy(p=>p.Key).Select(p=>p.Key+":"+p.Value.BaseValue+":"+p.Value.Value+":"+p.Value.Bonus+":"+p.Value.Penalty+":"+p.Value.Min+":"+p.Value.Max));
        private static string Gear(Entity e)
        {
            var inv=e.GetPart<InventoryPart>();if(inv==null)return "no-inventory";
            return string.Join("|",inv.Objects.Select(i=>"carry:"+i.ID+":"+i.BlueprintName+":"+(i.GetPart<StackerPart>()?.StackCount??1)+":"+i.GetPart<PhysicsPart>()?.InInventory?.ID)
                .Concat(inv.EquippedItems.Select(p=>"equip:"+p.Key+":"+p.Value.ID+":"+p.Value.GetPart<PhysicsPart>()?.Equipped?.ID)).OrderBy(s=>s))
                +";body="+string.Join("|",e.GetPart<Body>()?.GetParts().Where(p=>p.Equipped!=null).OrderBy(p=>p.ID).Select(p=>p.ID+":"+p.Equipped.ID)??Enumerable.Empty<string>());
        }
        private static string Abilities(Entity e)=>string.Join("|",e.GetPart<ActivatedAbilitiesPart>()?.AbilityList.OrderBy(a=>a.ID).Select(a=>a.ID+":"+a.Command+":"+a.Range+":"+a.MaxCooldown+":"+a.CooldownRemaining)??Enumerable.Empty<string>());
        private static string Goals(Entity e)=>string.Join("|",e.GetPart<BrainPart>()?.GetGoalsSnapshot().Select(g=>g.GetType().Name+":"+g.Age+":"+g.GetDetails()
            +(g is NoFightGoal n?":"+n.Duration+":"+n.Wander:""))??Enumerable.Empty<string>());
        private static string Command(Diag.Entry e){try{return (string)JObject.Parse(e.PayloadJson)["command"];}catch{return null;}}
        private static bool PositiveDamage(Diag.Entry e){try{var amount=JObject.Parse(e.PayloadJson)["amount"];return amount?.Type==JTokenType.Integer&&amount.Value<long>()>0;}catch{return false;}}
        private IEnumerator ItemAction(Entity item,string command)
        {
            yield return Tap(Key.I);Require(State=="InventoryOpen","real I inventory");
            for(int i=0;(int)Field(_input.InventoryUI,"_panel")!=1;i++)
            {Require(i<5,"finite observed inventory panel");yield return Tap((int)Field(_input.InventoryUI,"_panel")==0?Key.Tab:Key.LeftArrow);}
            var rows=(IList)Field(_input.InventoryUI,"_rows");int index=-1;
            for(int i=0;i<rows.Count;i++)if(ReferenceEquals(((InventoryScreenData.ItemDisplay)Field(rows[i],"Item"))?.Item,item))index=i;
            Require(index>=0,"actual same source item row");
            for(int i=0;(int)Field(_input.InventoryUI,"_cursorIndex")!=index;i++)
            {Require(i<80,"finite item row navigation");yield return Tap((int)Field(_input.InventoryUI,"_cursorIndex")<index?Key.DownArrow:Key.UpArrow);}
            yield return Tap(Key.Enter);var popup=Field(_input.InventoryUI,"_itemActionPopup");Require(popup!=null,"native item actions");
            var actions=((IList)Field(popup,"Actions")).Cast<object>().ToArray();int action=Array.FindIndex(actions,a=>(string)Field(a,"Command")==command);
            Require(action>=0,"offered owned item action "+command);
            for(int i=0;(int)Field(popup,"CursorIndex")!=action;i++)
            {Require(i<80,"finite item action navigation");yield return Tap((int)Field(popup,"CursorIndex")<action?Key.DownArrow:Key.UpArrow);}
            yield return Tap(Key.Enter);
        }
        private IEnumerator CloseInventory()
        {
            // Starter dagger fits an ordinary free hand. A displacement prompt
            // is an honest changed-premise failure, never silently cancelled.
            Require(Field(_input.InventoryUI,"_displaceConfirm")==null,"no unexpected native equip displacement");
            if(State=="InventoryOpen")yield return Tap(Key.I);yield return Settled();
        }
        private IEnumerator Settled()
        {
            double began=Time.realtimeSinceStartupAsDouble;
            while(State!="Normal"||_input.ZoneRenderer?.WorldFx?.HasBlockingFx==true)
            {Require(Time.realtimeSinceStartupAsDouble-began<8,"native input/FX settles: "+State);yield return null;}
            yield return null;
        }
        private IEnumerator WaitForFx()
        {
            double began=Time.realtimeSinceStartupAsDouble;
            while(_input!=null&&(State=="WaitingForFxResolution"||_input.ZoneRenderer?.WorldFx?.HasBlockingFx==true))
            {Require(Player.GetStatValue("Hitpoints")>0&&!CombatSystem.IsDeathHandled(Player)&&Time.realtimeSinceStartupAsDouble-began<8,"live native FX resolves");yield return null;}
        }
        private IEnumerator Tap(params Key[] keys)
        {
            Require(_clock.Elapsed.TotalSeconds<550,"finite native audit deadline");yield return WaitForFx();
            double began=Time.realtimeSinceStartupAsDouble;
            while(_input!=null&&Time.time-(float)Field(_input,"_lastMoveTime")<_input.MoveRepeatDelay)
            {Require(Time.realtimeSinceStartupAsDouble-began<3,"native input rate gate");yield return null;}
            var before=Snapshot("before-key");_keyboard.MakeCurrent();InputSystem.QueueStateEvent(_keyboard,new KeyboardState(keys));yield return null;
            InputSystem.QueueStateEvent(_keyboard,new KeyboardState());yield return null;yield return new WaitForSecondsRealtime(.13f);
            yield return WaitForFx();_keys.Add(new{sequence=_keys.Count,keys=string.Join(",",keys.Select(k=>k.ToString())),before,after=Snapshot("after-key")});WriteReport();
            if(_started)Require(Player.GetStatValue("Hitpoints")>0&&!CombatSystem.IsDeathHandled(Player),"ordinary actor died after real key");
        }
        private static Key Direction(int dx,int dy)
        {
            dx=Math.Sign(dx);dy=Math.Sign(dy);
            if(dx==0&&dy==0)return Key.Period;
            if(dx==1&&dy==0)return Key.D;if(dx==-1&&dy==0)return Key.A;if(dx==0&&dy==1)return Key.S;if(dx==0&&dy==-1)return Key.W;
            if(dx==1&&dy==1)return Key.Numpad3;if(dx==1&&dy==-1)return Key.Numpad9;if(dx==-1&&dy==1)return Key.Numpad1;return Key.Numpad7;
        }

        private object Snapshot(string phase)
        {
            if(_input?.PlayerEntity==null)return new{phase,state="bootstrap"};var at=Zone?.GetEntityCell(Player);var tc=_target==null?null:Zone?.GetEntityCell(_target);
            return new{phase,policy=Policy,state=State,zone=Zone?.ZoneID,player=Player.ID,x=at?.X,y=at?.Y,hp=Player.GetStatValue("Hitpoints"),tick=Tick,energy=Energy,ambient=Ambient,
                coins=TradeSystem.GetDrams(Player),stats=Stats(Player),gear=Gear(Player),abilities=Abilities(Player),target=_targetId,targetX=tc?.X,targetY=tc?.Y,
                targetHP=_target?.GetStatValue("Hitpoints"),targetAbilities=_target==null?null:Abilities(_target),goals=_target==null?null:Goals(_target),used=_used,fallback=_fallback,primary=_primary,
                messages=MessageLog.GetRecentEntries(12).Select(e=>e.Text).ToArray()};
        }
        private void Observe(string phase){_observations.Add(Snapshot(phase));WriteReport();}
        private void Check(string name,bool passed){if(!passed)_failures++;_audit.Add((passed?"PASS ":"FAIL ")+name);Observe(name);Diag.Record("scenario","DensityCombatNativeCase",payload:new{runId=RunId,policy=Policy,name,passed});}
        private static void Require(bool value,string reason){if(!value)throw new InvalidOperationException("Combat native audit: "+reason);}
        private static object Field(object owner,string name){var f=owner.GetType().GetField(name,Fields);if(f==null)throw new InvalidOperationException("Missing observed field "+owner.GetType().Name+"."+name);return f.GetValue(owner);}
        private static string HashFile(string path){using(var h=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        private IEnumerator Capture(string name){yield return new WaitForSecondsRealtime(.15f);yield return new WaitForEndOfFrame();Directory.CreateDirectory(DirectoryPath);string path=Path.Combine(DirectoryPath,name+".png");DensityNativeScreenshot.CaptureToFile(path);Require(File.Exists(path)&&new FileInfo(path).Length>0,"actual native screenshot");_screenshots.Add(path);WriteReport();}
        private IEnumerator RunSafely(IEnumerator routine)
        {
            var stack=new Stack<IEnumerator>();stack.Push(routine);
            try
            {
                while(stack.Count>0)
                {
                    bool moved=false;object current=null;Exception error=null;
                    try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}catch(Exception e){error=e;}
                    if(error!=null){_fatal=error.ToString();Check("native_precondition_failed",false);Debug.LogError("[DensityCombatNative] "+_fatal);break;}
                    if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}if(current is IEnumerator child){stack.Push(child);continue;}yield return current;
                }
            }
            finally{while(stack.Count>0){try{(stack.Pop() as IDisposable)?.Dispose();}catch(Exception e){_fatal+="\nCleanup "+e;}}Finish();}
        }
        public void SetUnexpectedErrors(int count){_unexpectedErrors=count;_errorsFinalized=true;WriteReport();EmitSummary();}
        public void Abort(string reason){if(Finished)return;_fatal=reason;Check("native_aborted",false);StopAllCoroutines();Finish();}
        private void Finish(){Cleanup();Finished=true;WriteReport();}

        private bool Complete=>Finished&&_errorsFinalized&&Failures==0&&_audit.Count==RequiredChecks.Length&&RequiredChecks.All(n=>_audit.Contains("PASS "+n));
        private void WriteReport()
        {
            Directory.CreateDirectory(DirectoryPath);ReportPath=Path.Combine(DirectoryPath,"report.json");
            string json=JsonConvert.SerializeObject(new{runId=RunId,policy=Policy,complete=Complete,failures=Failures,unexpectedErrors=_unexpectedErrors,errorsFinalized=_errorsFinalized,seconds=_clock?.Elapsed.TotalSeconds??0,
                fatal=_fatal,audit=_audit,requiredChecks=RequiredChecks,keys=_keys,observations=_observations,windows=_windows,notes=_notes,screenshots=_screenshots,
                sourceZone=_sourceZoneId,target=_targetId,actualTacticCommand=_usedCommand,moves=_moves,attacks=_attacks,casts=_casts,paidActions=_paidActions,nativeActionInputs=_nativeActionInputs,paidActionsMeaning="completed player scheduler turns, including automatic blocked turns",originalTonicsUsed=_tonics,checkpointHash=_checkpointHash,
                canVerify=Policy=="ranged"?"Ordinary original EmberSpit damage against an actual generated unchanged IceWight through native input; no enemy tactic is required or inferred by this ranged-first policy."
                    :Policy=="ranged-threat"?"Actual generated unchanged IceWight dispatch and cooldown fallback in exact completed enemy turns; ends alive above the safety floor immediately after both witnesses, even while frozen. No player damage cast or recovery is required or claimed."
                    :"One ordinary melee/control policy, actual unchanged source and enemy owned tactic/turn/cooldown/fallback. Control additionally validates current exact stationary goal and F5/real native mutation/F6 replacement graph.",
                cannotVerify="Three starter tactical policies plus a separate ranged-threat observation are not progression/class builds or a balance ranking. The independent ranged damage and enemy-threat runs do not prove one actor survives freezing then returns fire or recovers. One labelled player approach per mode, finite source/action selection, unchanged authored AI RNG/chance and possible honest failure. No grants, enemy suppression, forced effects or direct gameplay events. Existing core controls cover broader rule refusals. Captures require actual visual review."},Formatting.Indented);
            var parsed=JObject.Parse(json);Require(parsed["windows"] is JArray w&&w.Count==_windows.Count&&parsed["keys"] is JArray k&&k.Count==_keys.Count,"nested evidence serialized");File.WriteAllText(ReportPath,json);
        }
        private void Cleanup()
        {
            if(_cleaned)return;_cleaned=true;if(_moveObserver!=null)EntityVisualHooks.MovedCallback-=_moveObserver;
            if(_keyboard!=null){InputSystem.QueueStateEvent(_keyboard,new KeyboardState());InputSystem.RemoveDevice(_keyboard);}
            if(_oldKeyboard!=null&&_oldKeyboard.added)_oldKeyboard.MakeCurrent();if(_oldSettings!=null)InputSystem.settings=_oldSettings;if(_settings!=null)Destroy(_settings);Application.runInBackground=_oldBackground;
        }
        private void EmitSummary(){if(_summaryEmitted)return;_summaryEmitted=true;Diag.Record("scenario","DensityCombatNativeSummary",payload:new{runId=RunId,policy=Policy,complete=Complete,cases=_audit.Count,failures=Failures});}
        private void OnDestroy(){if(!Finished&&_clock!=null){_fatal="Play interrupted.";Check("native_interrupted",false);Finish();}Cleanup();EmitSummary();foreach(var pair in _channels)Diag.SetChannel(pair.Key,pair.Value);}
    }
}
