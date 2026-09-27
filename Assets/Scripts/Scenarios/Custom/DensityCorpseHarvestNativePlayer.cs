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
    /// <summary>Ordinary native kill, finite corpse harvest and checkpoint audit.
    /// One labelled living-player approach transfer; all combat, menus, harvest,
    /// inventory mutation and checkpoint actions use real keyboard input.</summary>
    public sealed class DensityCorpseHarvestNativePlayer : MonoBehaviour
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly (int x,int y)[] Steps = {(1,0),(-1,0),(0,1),(0,-1),(1,1),(1,-1),(-1,1),(-1,-1)};
        private static readonly string[] RequiredChecks = {
            "ordinary_start_and_exact_kit", "actual_generated_living_source", "single_labelled_approach",
            "starter_dagger_equipped_by_keyboard", "native_exact_player_melee_defeat", "actual_identified_harvestable_corpse",
            "native_exact_corpse_menu", "native_paid_harvest_conserves_all_units", "source_removed_and_no_repeat_menu",
            "checkpoint_saved_after_depletion", "native_earned_yield_drop_mutates_graph", "checkpoint_replaces_graph_and_restores_yield",
            "restored_reads_keep_corpse_depleted", "ordinary_living_finish" };
        public string RunId { get; } = Guid.NewGuid().ToString("N");
        public bool Finished { get; private set; }
        public int Failures => _failures + _unexpectedErrors;
        public string ReportPath { get; private set; }
        private InputHandler _input;
        private Keyboard _keyboard, _oldKeyboard;
        private InputSettings _settings, _oldSettings;
        private bool _oldBackground, _started, _cleaned, _errorsFinalized, _summaryEmitted;
        private readonly Dictionary<string,bool> _channels = new Dictionary<string,bool>();
        private readonly List<string> _audit = new List<string>(), _screenshots = new List<string>(), _notes = new List<string>();
        private readonly List<object> _keys = new List<object>(), _observations = new List<object>(), _windows = new List<object>();
        private readonly Dictionary<string,Entity> _startingTonics = new Dictionary<string,Entity>();
        private System.Diagnostics.Stopwatch _clock;
        private int _failures, _unexpectedErrors, _moves, _attacks, _tonics, _coins, _deathX, _deathY;
        private string _fatal, _saveRoot, _targetId, _corpseId, _sourceZoneId, _checkpointHash;
        private Entity _target, _corpse, _dagger;
        private Zone _source;
        private Cell _approach;
        private bool _playerLethal, _retaliation;
        private Entity Player => _input.PlayerEntity;
        private Zone Zone => _input.CurrentZone;
        private OverworldZoneManager Manager => _input.ZoneManager as OverworldZoneManager;
        private Cell At => Zone.GetEntityCell(Player);
        private int Tick => _input.TurnManager.TickCount;
        private int Energy => _input.TurnManager.GetEnergy(Player);
        private int Ambient => Player.GetIntProperty(WorldAmbience.TurnProperty);
        private string State => Field(_input,"_inputState").ToString();
        private string DirectoryPath => System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath,
            "../Docs/Verification/DensityCompletion/Scenery/NativeCorpseAcceptance",RunId));

        public void Initialize(ScenarioContext context)
        {
            Require(!string.IsNullOrEmpty(SaveGameService.SaveRootOverride), "isolated launcher save root");
            _saveRoot = SaveGameService.SaveRootOverride; _clock = System.Diagnostics.Stopwatch.StartNew();
            foreach (string name in new[]{"scenario","damage","event","loot","worldgen"})
            { _channels[name] = Diag.IsChannelEnabled(name); Diag.SetChannel(name,true); }
            _oldSettings = InputSystem.settings; _settings = Instantiate(_oldSettings);
            _settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            _settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = _settings; _oldBackground = Application.runInBackground; Application.runInBackground = true;
            _oldKeyboard = Keyboard.current; _keyboard = InputSystem.AddDevice<Keyboard>();
            StartCoroutine(RunSafely(RunAudit()));
        }

        private IEnumerator RunAudit()
        {
            yield return new WaitForSecondsRealtime(.8f);
            _input = FindFirstObjectByType<InputHandler>(); Require(_input != null,"normal input bootstrap");
            var boot = (BootMenuController)Field(_input,"_bootMenuController"); Require(boot != null && boot.IsActive,"new-game menu");
            yield return Tap(Key.N); yield return Settled(); _started = true;
            Require(Manager != null && Manager.WorldSeed == 64,"actual new game seed64");
            _coins = TradeSystem.GetDrams(Player);
            _dagger = Player.GetPart<InventoryPart>().Objects.Single(e=>e.BlueprintName=="Dagger");
            foreach (var e in Player.GetPart<InventoryPart>().Objects.Where(e=>e.BlueprintName=="HealingTonic")) _startingTonics.Add(e.ID,e);
            Check("ordinary_start_and_exact_kit",Ordinary() && Player.GetStatValue("Hitpoints")==40 && TonicUnits()==2
                && _coins==50 && PackedUnits()==0);
            yield return Capture("01-ordinary-start");
            FindSource();
            Check("actual_generated_living_source",_source != null && _target != null && SourceReady(_target,_source)
                && ReferenceEquals(Manager.CachedZones[_source.ZoneID],_source));
            string beforeStats=Stats(Player),beforeGear=Gear(Player); int beforeTick=Tick,beforeEnergy=Energy;
            var old=Zone; Require(Safe(_source,_approach,_target,5),"current initial approach footprint");
            Require(old.TryTransferEntityTo(Player,_source,_approach.X,_approach.Y),"labelled living-player approach transfer");
            typeof(InputHandler).GetMethod("HandleZoneTransition",Fields).Invoke(_input,new object[]{new ZoneTransitionResult {
                Success=true,NewZone=_source,NewPlayerX=_approach.X,NewPlayerY=_approach.Y }});
            _input.CameraFollow?.SnapToPlayer(); ZoneRenderHooks.MarkFullDirty("DensityCorpseNativeApproach");
            _notes.Add("ONE LABELLED APPROACH "+old.ZoneID+"→"+_sourceZoneId+"@"+_approach.X+","+_approach.Y
                +" target="+_targetId+" actualDistance="+SpatialQuery.Distance(_source,Player,_target));
            yield return Settled();
            Check("single_labelled_approach",ReferenceEquals(Zone,_source)&&Stats(Player)==beforeStats&&Gear(Player)==beforeGear
                &&Tick==beforeTick&&Energy==beforeEnergy&&TradeSystem.GetDrams(Player)==_coins
                &&ReferenceEquals(_target.GetPart<BrainPart>()?.CurrentZone,Zone)&&_input.TurnManager.IsRegistered(_target));
            yield return ItemAction(_dagger,"equip_auto"); yield return CloseInventory();
            Check("starter_dagger_equipped_by_keyboard",_dagger.GetPart<PhysicsPart>()?.Equipped==Player
                &&Player.GetPart<InventoryPart>().EquippedItems.Values.Contains(_dagger));
            yield return Fight();
            Check("native_exact_player_melee_defeat",_playerLethal && _attacks>0 && CombatSystem.IsDeathHandled(_target)
                &&_target.GetStatValue("Hitpoints")<=0&&Zone.GetEntityCell(_target)==null&&!_input.TurnManager.IsRegistered(_target));
            _corpse = Zone.GetReadOnlyEntities().SingleOrDefault(e=>e.GetProperty("SourceID")==_targetId);
            Require(_corpse!=null,"actual authored death produced a corpse"); _corpseId=_corpse.ID;
            Require(DensityCorpseHarvestEvidence.IsExactCorpse(_corpse,Zone,Player.ID,_targetId,_deathX,_deathY),"exact current corpse identity and finite recipe before interaction");
            Check("actual_identified_harvestable_corpse",true);
            yield return ApproachCorpse();
            yield return WorldMenu(_corpse);
            Check("native_exact_corpse_menu",ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget,_corpse)
                &&Actions().Any(a=>a.Command=="Harvest"));
            yield return Capture("02-exact-killed-corpse-menu");
            int serial=MessageLog.GetRecentEntries(1).LastOrDefault().Serial;
            int examineTick=Tick,examineEnergy=Energy;
            yield return MenuAction("Examine"); yield return Settled();
            Require(MessageLog.GetRecentEntries(16).Any(e=>e.Serial>serial&&e.Text.StartsWith("You see",StringComparison.OrdinalIgnoreCase)
                &&e.Text.IndexOf(_corpse.GetDisplayName(),StringComparison.OrdinalIgnoreCase)>=0)
                &&Tick==examineTick&&Energy==examineEnergy,"fresh native exact-corpse examine is free");
            yield return Harvest();
            yield return Checkpoint();
            Check("ordinary_living_finish",Ordinary()&&Player.GetStatValue("Hitpoints")>0&&TradeSystem.GetDrams(Player)==_coins
                &&Player.GetPart<InventoryPart>().EquippedItems.Values.Any(e=>e.ID==_dagger.ID));
            Observe("complete"); yield return Capture("06-restored-earned-yield");
        }

        private void FindSource()
        {
            int examined=0;
            for(int y=0;y<WorldMap.Height&&examined<24;y++) for(int x=0;x<WorldMap.Width&&examined<24;x++)
            {
                if(Manager.WorldMap.GetBiome(x,y)!=BiomeType.Beating||Manager.WorldMap.GetPOI(x,y)!=null)continue;
                string id=WorldMap.ToZoneID(x,y,0); examined++; var zone=Manager.GetZone(id);
                if(zone==null){_notes.Add("SOURCE "+id+" generation refused");continue;}
                var targets=zone.GetReadOnlyEntities().Where(e=>e.BlueprintName=="SunStriker").OrderBy(e=>e.ID,StringComparer.Ordinal).ToArray();
                _notes.Add("SOURCE "+id+" tier="+WorldMapAuthoring.TierAt(x,y)+" actualSunStrikers="+string.Join(",",targets.Select(e=>e.ID)));
                foreach(var target in targets)
                {
                    if(!SourceReady(target,zone)){_notes.Add("REFUSE "+target.ID+" current source/configuration");continue;}
                    var at=zone.GetEntityCell(target);
                    for(int cy=Math.Max(1,at.Y-7);cy<=Math.Min(Zone.Height-2,at.Y+7);cy++)
                    for(int cx=Math.Max(1,at.X-7);cx<=Math.Min(Zone.Width-2,at.X+7);cx++)
                    {
                        int distance=Math.Max(Math.Abs(cx-at.X),Math.Abs(cy-at.Y)); if(distance<5||distance>7)continue;
                        var cell=zone.GetCell(cx,cy); if(!Safe(zone,cell,target,5)||!AIHelpers.HasLineOfSight(zone,cx,cy,at.X,at.Y))continue;
                        var path=Path(zone,cell,c=>SpatialQuery.DistanceToCell(zone,target,c.X,c.Y)==1,target);
                        if(path==null||path.Count>12)continue;
                        _source=zone;_target=target;_targetId=target.ID;_sourceZoneId=id;_approach=cell;
                        _notes.Add("SELECT FIRST VALID "+id+":"+target.ID+"@"+at.X+","+at.Y+" HP="+target.GetStatValue("Hitpoints")+" speed="+target.GetStatValue("Speed")
                            +" approach="+cx+","+cy+" path="+path.Count+"; no future outcome inspected"); WriteReport(); return;
                    }
                    _notes.Add("REFUSE "+target.ID+" no legal bounded approach without other nearby threats");
                }
            }
            WriteReport(); throw new InvalidOperationException("No qualifying actual source in24 canonical current Beating wilderness zones.");
        }
        private bool SourceReady(Entity e,Zone zone)
        {
            var c=e?.GetPart<CorpsePart>();var at=e==null?null:zone.GetEntityCell(e);
            return e?.BlueprintName=="SunStriker"&&!string.IsNullOrEmpty(e.ID)&&at!=null&&at.Objects.Contains(e)
                &&e.GetStatValue("Hitpoints")>0&&!CombatSystem.IsDeathHandled(e)&&e.GetPart<BrainPart>()!=null
                &&e.GetPart<PhysicsPart>()?.InInventory==null&&e.GetPart<PhysicsPart>()?.Equipped==null
                &&!e.HasTag("SuppressCorpseDrops")&&Hostile(e)
                &&c?.CorpseBlueprint=="CreatureCorpse"&&c.CorpseChance==100&&c.BuildCorpseChance==100&&c.TestRng==null
                &&c.HarvestBlueprint=="RawMeat"&&c.HarvestMin==1&&c.HarvestMax==2&&c.HarvestChance==100;
        }
        private IEnumerator Fight()
        {
            for(int action=0;!CombatSystem.IsDeathHandled(_target);action++)
            {
                Require(action<60&&_moves<40&&_attacks<24,"finite ordinary fight bounds"); yield return Settled();
                Require(SourceReady(_target,Zone)&&_input.TurnManager.IsRegistered(_target)&&ReferenceEquals(_target.GetPart<BrainPart>().CurrentZone,Zone),"same live generated target remains scheduled");
                yield return UseOriginalTonic();
                var cell=Zone.GetEntityCell(_target); var from=At;
                if(SpatialQuery.Distance(Zone,Player,_target)>1)
                {
                    var path=Path(Zone,from,c=>SpatialQuery.DistanceToCell(Zone,_target,c.X,c.Y)==1,_target);
                    Require(path!=null&&path.Count>0,"current physical route to original target");
                    yield return PaidStep(path[0],_target); _moves++; continue;
                }
                Require(_target.GetPart<RenderPart>()?.Visible==true&&Zone.GetOccupiedCells(_target).Any(c=>c.IsVisible)
                    &&Safe(Zone,At,_target,3),"visible actual adjacent target and no unrelated immediate threat");
                int x=cell.X,y=cell.Y,tick=Tick,energy=Energy,ambient=Ambient;
                Diag.Record("scenario",ReferenceGladeCombatEvidence.MarkerKind,Player,_target,new{runId=RunId,key="melee"});
                string marker=Diag.Snapshot(1).Single().TraceId;
                try
                {
                    var to=SpatialQuery.ClosestCell(Zone,_target,from.X,from.Y);
                    yield return Tap(Direction(to.X-from.X,to.Y-from.Y)); yield return Settled();
                    var rows=Diag.Snapshot(Diag.BufferCapacity);var result=ReferenceGladeCombatEvidence.Inspect(rows,marker,Player.ID,_targetId);
                    Require(result.WindowValid&&result.PlayerAttempt&&OneAction(tick,energy)&&Ambient==ambient+1,"exact original target receives one paid native melee attempt");
                    _retaliation|=result.HostileAttempt;
                    if(result.PlayerLethal&&rows.Any(r=>r.Category=="damage"&&r.Kind=="DeathHandled"&&r.ActorId==Player.ID
                        &&r.TargetId==_targetId&&r.CauseTraceId==result.DamageCause)){_playerLethal=true;_deathX=x;_deathY=y;}
                    _attacks++;
                }
                finally{_windows.Add(new{marker,kind="melee",rows=Diag.Snapshot(Diag.BufferCapacity).SkipWhile(r=>r.TraceId!=marker).ToArray(),state=Snapshot("melee-end")});}
            }
            Require(_playerLethal,"another actor or uncorrelated effect cannot substitute for player melee death");
            yield return Capture("03-actual-player-kill");
        }
        private IEnumerator UseOriginalTonic()
        {
            var hp=Player.GetStat("Hitpoints");if(_tonics>=2||hp.Value*3>hp.Max*2)yield break;
            var item=_startingTonics.Values.FirstOrDefault(e=>Player.GetPart<InventoryPart>().Objects.Contains(e)&&e.GetPart<PhysicsPart>()?.InInventory==Player);
            if(item==null)yield break;int before=TonicUnits(),tick=Tick,energy=Energy;
            Diag.Record("scenario",ReferenceGladeCombatEvidence.SupportMarkerKind,Player,Player,new{runId=RunId,item=item.ID});
            string marker=Diag.Snapshot(1).Single().TraceId;
            yield return ItemAction(item,"ApplyTonic");yield return CloseInventory();
            Require(ReferenceGladeCombatEvidence.HasConsumedTonic(Diag.Snapshot(Diag.BufferCapacity),marker,Player.ID,item.ID,before,TonicUnits())
                &&Tick==tick&&Energy==energy,"one original tonic consumed by actual currently free native action");_tonics++;
        }
        private IEnumerator ApproachCorpse()
        {
            for(int i=0;SpatialQuery.Distance(Zone,Player,_corpse)>1;i++)
            {Require(i<16,"finite corpse approach");var path=Path(Zone,At,c=>SpatialQuery.DistanceToCell(Zone,_corpse,c.X,c.Y)<=1,null);Require(path!=null&&path.Count>0,"safe actual corpse route");yield return PaidStep(path[0],null);}
            Require(DensityCorpseHarvestEvidence.IsExactCorpse(_corpse,Zone,Player.ID,_targetId,_deathX,_deathY),"same unspent corpse remains at exact death cell");
        }
        private IEnumerator Harvest()
        {
            var cell=Zone.GetEntityCell(_corpse);var other=cell.Objects.Where(e=>e!=_corpse).ToArray();
            int packed=PackedUnits(),floor=FloorUnits(cell),tick=Tick,energy=Energy,ambient=Ambient;
            yield return WorldMenu(_corpse);
            Diag.Record("scenario",DensityCorpseHarvestEvidence.MarkerKind,Player,_corpse,new{runId=RunId});
            string marker=Diag.Snapshot(1).Single().TraceId;
            yield return MenuAction("Harvest"); yield return Settled();
            var rows=Diag.Snapshot(Diag.BufferCapacity);bool absent=Zone.GetEntityCell(_corpse)==null;
            Check("native_paid_harvest_conserves_all_units",DensityCorpseHarvestEvidence.CommittedHarvest(rows,marker,Player.ID,_corpseId,
                packed,PackedUnits(),floor,FloorUnits(cell),_corpse.GetPart<HarvestablePart>().Harvested,absent)
                &&OneAction(tick,energy)&&Ambient==ambient+1&&TradeSystem.GetDrams(Player)==_coins);
            _windows.Add(new{marker,kind="harvest",rows=rows.SkipWhile(r=>r.TraceId!=marker).ToArray(),packedBefore=packed,packedAfter=PackedUnits(),floorBefore=floor,floorAfter=FloorUnits(cell)});
            Require(absent,"spent source removed");
            // Existing native tests cover valid overflow. This ordinary starter
            // route requires actual carry capacity; it never discards kit to fit.
            Require(FloorUnits(cell)==floor&&PackedUnits()-packed>=1,"actual ordinary inventory received the entire finite yield; overflow is recorded, never ignored");
            yield return Tap(Key.C);Require(State=="AwaitingTalkDirection","native revisit direction");
            yield return Tap(Direction(cell.X-At.X,cell.Y-At.Y));Require(State=="WorldActionMenuOpen","native spent-cell menu");
            if(_input.WorldActionMenuUI.SelectedCellIsPile)yield return MenuAction(WorldInteractionSystem.PickCellCommand);
            Check("source_removed_and_no_repeat_menu",absent&&other.All(e=>cell.Objects.Contains(e))
                &&!ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget,_corpse)
                &&!Actions().Any(a=>a.Command==WorldInteractionSystem.PickTargetCommandPrefix+_corpseId));
            yield return Capture("04-spent-corpse-cell");yield return Tap(Key.Escape);yield return Settled();
        }
        private IEnumerator Checkpoint()
        {
            var actor=Player;var oldZone=Zone;var at=At;
            var earned=Player.GetPart<InventoryPart>().Objects.Single(e=>e.BlueprintName=="RawMeat");
            Require(earned.GetPart<PhysicsPart>()?.InInventory==actor,"actual carried earned stack");
            string earnedId=earned.ID,gear=Gear(actor),stats=Stats(actor);int count=PackedUnits(),tick=Tick,energy=Energy,world=WorldClock.CurrentTick,x=at.X,y=at.Y;
            string cellOwners=DeathCellOwners();
            var metadata=SaveGameService.GetSaveInfo("Quick");Require(metadata!=null,"real initial autosave metadata");
            string game=metadata.GameID,path=System.IO.Path.Combine(_saveRoot,game,"Quick.sav.gz"),beforeHash=HashFile(path);long serial=MessageLog.NextSerialValue;
            yield return Tap(Key.F5);yield return Settled();_checkpointHash=HashFile(path);
            Check("checkpoint_saved_after_depletion",_checkpointHash!=beforeHash&&MessageLog.NextSerialValue>serial&&MessageLog.GetLast()=="Game saved."
                &&SaveGameService.GetSaveInfo("Quick")?.GameID==game&&SaveGameService.GetSaveInfo("Quick")?.ActiveZoneID==_sourceZoneId);
            yield return ItemAction(earned,"drop");yield return CloseInventory();
            Check("native_earned_yield_drop_mutates_graph",PackedUnits()==0&&!Player.GetPart<InventoryPart>().Objects.Contains(earned)
                &&Zone.GetEntityCell(earned)!=null&&earned.GetPart<PhysicsPart>()?.InInventory==null&&earned.GetPart<PhysicsPart>()?.Equipped==null
                &&Units(new[]{earned})==count&&TradeSystem.GetDrams(Player)==_coins&&HashFile(path)==_checkpointHash);
            var step=Steps.Select(d=>Zone.GetCell(At.X+d.x,At.Y+d.y)).FirstOrDefault(c=>Safe(Zone,c,null,3));
            Require(step!=null,"one genuine safe paid checkpoint mutation step");yield return PaidStep(step,null);
            Require(Tick!=tick||Energy!=energy,"post-save native scheduler mutation");
            yield return Capture("05-real-earned-yield-drop");yield return Tap(Key.F6);
            double began=Time.realtimeSinceStartupAsDouble;
            while(ReferenceEquals(Player,actor)){Require(Time.realtimeSinceStartupAsDouble-began<8,"F6 replacement graph");yield return null;}
            yield return Settled();var restored=Player.GetPart<InventoryPart>().Objects.SingleOrDefault(e=>e.ID==earnedId);
            Check("checkpoint_replaces_graph_and_restores_yield",!ReferenceEquals(Zone,oldZone)&&Player.ID==actor.ID&&Zone.ZoneID==_sourceZoneId
                &&restored!=null&&!ReferenceEquals(restored,earned)&&restored.GetPart<PhysicsPart>()?.InInventory==Player
                &&restored.GetPart<PhysicsPart>()?.Equipped==null&&PackedUnits()==count&&Gear(Player)==gear&&Stats(Player)==stats
                &&At.X==x&&At.Y==y&&Tick==tick&&Energy==energy&&WorldClock.CurrentTick==world&&TradeSystem.GetDrams(Player)==_coins
                &&DeathCellOwners()==cellOwners&&HashFile(path)==_checkpointHash&&Depleted()
                &&DensityCorpseHarvestEvidence.RestoredYieldGraph(actor,oldZone,Player,Zone,Manager.CachedZones.Values.ToArray(),
                    _targetId,_corpseId,earnedId,count));
            var current=Zone;var currentPlayer=Player;string currentGear=Gear(Player);int currentTick=Tick,currentEnergy=Energy;
            Require(ReferenceEquals(Manager.GetZone(_sourceZoneId),current),"cached restored zone is reused");
            ZoneRenderHooks.MarkFullDirty("DensityCorpseNativeRestoredObservation");yield return null;
            Check("restored_reads_keep_corpse_depleted",Depleted()&&ReferenceEquals(Player,currentPlayer)&&ReferenceEquals(Zone,current)
                &&Gear(Player)==currentGear&&Tick==currentTick&&Energy==currentEnergy&&DeathCellOwners()==cellOwners&&HashFile(path)==_checkpointHash);
        }
        private bool Depleted()=>!Manager.CachedZones.Values.SelectMany(z=>z.GetReadOnlyEntities())
            .Any(e=>e.ID==_targetId||e.ID==_corpseId||e.GetProperty("SourceID")==_targetId)
            &&!Player.GetPart<InventoryPart>().Objects.Any(e=>e.ID==_corpseId||e.GetProperty("SourceID")==_targetId);
        private string DeathCellOwners()=>string.Join("|",Zone.GetCell(_deathX,_deathY).Objects.OrderBy(e=>e.ID,StringComparer.Ordinal)
            .Select(e=>e.ID+":"+e.BlueprintName+":"+(e.GetPart<StackerPart>()?.StackCount??1)));
        private List<Cell> Path(Zone zone,Cell start,Func<Cell,bool> goal,Entity ignoredThreat)
        {
            var previous=new Dictionary<(int,int),(int,int)>();var origin=(start.X,start.Y);previous[origin]=origin;
            var queue=new Queue<(int,int)>();queue.Enqueue(origin);
            var threats=Threats(zone,ignoredThreat);
            while(queue.Count>0)
            {
                var at=queue.Dequeue();if(goal(zone.GetCell(at.Item1,at.Item2)))
                {var path=new List<Cell>();while(at!=origin){path.Add(zone.GetCell(at.Item1,at.Item2));at=previous[at];}path.Reverse();return path;}
                foreach(var d in Steps)
                {
                    var next=(at.Item1+d.x,at.Item2+d.y);if(previous.ContainsKey(next)||!Safe(zone,zone.GetCell(next.Item1,next.Item2),ignoredThreat,3,threats))continue;
                    if(d.x!=0&&d.y!=0&&!zone.CanPlaceFootprint(Player,at.Item1+d.x,at.Item2)&&!zone.CanPlaceFootprint(Player,at.Item1,at.Item2+d.y))continue;
                    previous[next]=at;queue.Enqueue(next);
                }
            }
            return null;
        }
        private Entity[] Threats(Zone zone,Entity ignoredThreat)=>zone.GetReadOnlyEntities().Where(e=>e!=Player&&e!=ignoredThreat
            &&e.HasTag("Creature")&&!CombatSystem.IsDeathHandled(e)&&e.GetStatValue("Hitpoints")>0&&Hostile(e)).ToArray();
        private bool Safe(Zone zone,Cell cell,Entity ignoredThreat,int radius,Entity[] threats=null)
        {
            if(cell==null||!zone.CanPlaceFootprint(Player,cell.X,cell.Y))return false;
            threats??=Threats(zone,ignoredThreat);
            foreach(var c in zone.GetOccupiedCells(Player,cell.X,cell.Y))
            {
                if(c==null||c.Occupants.Any(e=>e!=Player&&(e.HasTag("Creature")||e.HasPart<TriggerOnStepPart>()||e.HasPart<GasPoolPart>()
                    ||e.HasPart<LiquidPoolPart>()||e.HasEffect<BurningEffect>()||e.GetPart<ThermalPart>()?.IsAflame==true)))return false;
                var s=zone.TileState.Get(c.X,c.Y);if(s!=null&&(s.Heat>0||s.Cold>0||s.Charge>0||!string.IsNullOrEmpty(s.Cloud)||s.Coatings.Count>0))return false;
                if(threats.Any(e=>SpatialQuery.DistanceToCell(zone,e,c.X,c.Y)<=radius))return false;
            }
            return true;
        }
        private bool Hostile(Entity e)=>FactionManager.IsHostile(e,Player)||FactionManager.IsHostile(Player,e)
            ||e.GetPart<BrainPart>()?.IsPersonallyHostileTo(Player)==true||ReferenceEquals(e.GetPart<BrainPart>()?.Target,Player);
        private IEnumerator PaidStep(Cell to,Entity ignoredThreat)
        {
            Require(Safe(Zone,to,ignoredThreat,3),"fresh movement footprint/hazards/unrelated threats");
            int tick=Tick,energy=Energy,ambient=Ambient;var player=Player;var origin=At;
            yield return Tap(Direction(to.X-origin.X,to.Y-origin.Y));yield return Settled();
            Require(ReferenceEquals(Player,player)&&ReferenceEquals(At,to)&&OneAction(tick,energy)&&Ambient==ambient+1,"one paid native step reaches chosen current cell");Observe("native-step");
        }
        private bool OneAction(int tick,int energy)=>Energy==energy-TurnManager.ActionThreshold+(Tick-tick)*Player.GetStatValue("Speed",TurnManager.DefaultSpeed);
        private int TonicUnits()=>_startingTonics.Values.Where(e=>Player.GetPart<InventoryPart>().Objects.Contains(e)&&e.GetPart<PhysicsPart>()?.InInventory==Player)
            .Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);
        private static int Units(IEnumerable<Entity> entities)=>entities.Where(e=>e.BlueprintName=="RawMeat").Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);
        private int PackedUnits()=>Units(Player.GetPart<InventoryPart>().Objects);
        private int FloorUnits(Cell cell)=>Units(cell.Objects);
        private bool Ordinary()=>!DevMode.Enabled&&!DebugInvincibility.IsEnabled(Player)&&!Player.HasPart<BitLockerPart>()&&!CombatSystem.IsDeathHandled(Player)
            &&Player.GetStat("Hitpoints").Max==40&&Player.GetStatValue("Strength")==18&&Player.GetStatValue("Agility")==18&&Player.GetStatValue("Toughness")==18
            &&Player.GetStatValue("Ego")==16&&Player.GetStatValue("Intelligence")==10&&Player.GetStatValue("Willpower")==10;
        private static string Stats(Entity e)=>string.Join("|",e.Statistics.OrderBy(p=>p.Key).Select(p=>p.Key+":"+p.Value.BaseValue+":"+p.Value.Value+":"+p.Value.Bonus+":"+p.Value.Penalty+":"+p.Value.Min+":"+p.Value.Max))
            +";effects="+string.Join("|",e.GetPart<StatusEffectsPart>()?.GetAllEffects().Select(f=>f.GetType().FullName+":"+f.Duration).OrderBy(s=>s)??Enumerable.Empty<string>());
        private static string Gear(Entity e)
        {
            var inv=e.GetPart<InventoryPart>();
            return string.Join("|",inv.Objects.Select(i=>"carry:"+i.ID+":"+i.BlueprintName+":"+(i.GetPart<StackerPart>()?.StackCount??1)+":"+i.GetPart<PhysicsPart>()?.InInventory?.ID)
                .Concat(inv.EquippedItems.Select(p=>"equip:"+p.Key+":"+p.Value.ID+":"+p.Value.GetPart<PhysicsPart>()?.Equipped?.ID)).OrderBy(s=>s))
                +";body="+string.Join("|",e.GetPart<Body>()?.GetParts().Where(p=>p.Equipped!=null).OrderBy(p=>p.ID).Select(p=>p.ID+":"+p.Equipped.ID)??Enumerable.Empty<string>());
        }
        private IEnumerator WorldMenu(Entity target)
        {
            Require(Zone.GetEntityCell(target)!=null&&SpatialQuery.Distance(Zone,Player,target)<=1,"current reachable world owner");
            var to=SpatialQuery.ClosestCell(Zone,target,At.X,At.Y);
            yield return Tap(Key.C);Require(State=="AwaitingTalkDirection","native C direction");yield return Tap(Direction(to.X-At.X,to.Y-At.Y));
            Require(State=="WorldActionMenuOpen","native world menu");if(_input.WorldActionMenuUI.SelectedCellIsPile)yield return MenuAction(WorldInteractionSystem.PickCellCommand);
            if(!ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget,target)||Actions().Any(a=>a.Command==WorldInteractionSystem.PickTargetCommandPrefix+target.ID))
                yield return MenuAction(WorldInteractionSystem.PickTargetCommandPrefix+target.ID);
            Require(ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget,target),"exact actual selected corpse");
        }
        private List<InventoryAction> Actions()=>(List<InventoryAction>)Field(_input.WorldActionMenuUI,"_actions");
        private IEnumerator MenuAction(string command)
        {
            var actions=Actions();int n=actions.FindIndex(a=>a.Command==command);Require(n>=0,"actual offered action "+command);
            yield return Tap((Key)Enum.Parse(typeof(Key),MenuShortcutMap.Key(MenuShortcutMap.ForActions(actions)[n]).ToString()));
        }
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
            if(_input?.PlayerEntity==null)return new{phase,state="bootstrap"};
            var at=Zone?.GetEntityCell(Player);var targetCell=_target==null?null:Zone?.GetEntityCell(_target);
            return new{phase,state=State,zone=Zone?.ZoneID,player=Player.ID,x=at?.X,y=at?.Y,hp=Player.GetStatValue("Hitpoints"),tick=Tick,energy=Energy,ambient=Ambient,
                coins=TradeSystem.GetDrams(Player),gear=Gear(Player),stats=Stats(Player),target=_targetId,targetX=targetCell?.X,targetY=targetCell?.Y,
                targetHP=_target?.GetStatValue("Hitpoints"),corpse=_corpseId,packed=PackedUnits(),lastMessages=MessageLog.GetRecentEntries(12).Select(e=>e.Text).ToArray()};
        }
        private void Observe(string phase){_observations.Add(Snapshot(phase));WriteReport();}
        private void Check(string name,bool passed){if(!passed)_failures++;_audit.Add((passed?"PASS ":"FAIL ")+name);Observe(name);Diag.Record("scenario","DensityCorpseHarvestNativeCase",payload:new{runId=RunId,name,passed});}
        private static void Require(bool value,string reason){if(!value)throw new InvalidOperationException("Corpse native audit: "+reason);}
        private static object Field(object owner,string name)
        {var f=owner.GetType().GetField(name,Fields);if(f==null)throw new InvalidOperationException("Missing observed field "+owner.GetType().Name+"."+name);return f.GetValue(owner);}
        private static string HashFile(string path){using(var h=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
        private IEnumerator Capture(string name)
        {yield return new WaitForSecondsRealtime(.15f);yield return new WaitForEndOfFrame();Directory.CreateDirectory(DirectoryPath);string p=System.IO.Path.Combine(DirectoryPath,name+".png");DensityNativeScreenshot.CaptureToFile(p);Require(File.Exists(p)&&new FileInfo(p).Length>0,"actual native capture");_screenshots.Add(p);WriteReport();}
        private IEnumerator RunSafely(IEnumerator routine)
        {
            var stack=new Stack<IEnumerator>();stack.Push(routine);
            try
            {
                while(stack.Count>0)
                {
                    bool moved=false;object current=null;Exception error=null;
                    try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}catch(Exception e){error=e;}
                    if(error!=null){_fatal=error.ToString();Check("native_precondition_failed",false);Debug.LogError("[DensityCorpseHarvestNative] "+_fatal);break;}
                    if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}if(current is IEnumerator child){stack.Push(child);continue;}yield return current;
                }
            }
            finally{while(stack.Count>0){try{(stack.Pop() as IDisposable)?.Dispose();}catch(Exception e){_fatal+="\nCleanup "+e;}}Finish();}
        }
        public void SetUnexpectedErrors(int count){_unexpectedErrors=count;_errorsFinalized=true;WriteReport();EmitSummary();}
        public void Abort(string reason){if(Finished)return;_fatal=reason;Check("native_aborted",false);StopAllCoroutines();Finish();}
        private void Finish(){Cleanup();Finished=true;WriteReport();}
        private bool Complete=>Finished&&_errorsFinalized&&Failures==0&&_audit.Count==RequiredChecks.Length&&RequiredChecks.All(n=>_audit.Contains("PASS "+n))&&_screenshots.Count==6;
        private void WriteReport()
        {
            Directory.CreateDirectory(DirectoryPath);ReportPath=System.IO.Path.Combine(DirectoryPath,"report.json");
            string json=JsonConvert.SerializeObject(new{runId=RunId,complete=Complete,failures=Failures,errorsFinalized=_errorsFinalized,seconds=_clock?.Elapsed.TotalSeconds??0,
                fatal=_fatal,audit=_audit.ToArray(),requiredChecks=RequiredChecks,keys=_keys.ToArray(),observations=_observations.ToArray(),windows=_windows.ToArray(),notes=_notes.ToArray(),screenshots=_screenshots.ToArray(),
                sourceZone=_sourceZoneId,target=_targetId,corpse=_corpseId,moves=_moves,attacks=_attacks,retaliationObserved=_retaliation,originalTonicsUsed=_tonics,checkpointHash=_checkpointHash,
                canVerify="Actual generated SunStriker; ordinary kit and native melee kill attribution; exact corpse and commit-only Harvest receipt with packed/floor conservation; current source/menu depletion; native F5/earned yield drop/paid step/F6 replacement graph and exact yielded stack ownership, stats/gear/currency/clock plus persisted corpse absence.",
                cannotVerify="One labelled live-player travel shortcut and bounded seed64 source selection, not natural discovery or typical combat/food balance. No grants, world edits, forced drops, rerolls or NPC suppression. Native source safety and menu gates may fail honestly. Overflow is counted but this bounded route requires actual ordinary carry capacity; other overflow controls remain synthetic. No generated Bones acquisition, all-family harvest or Beating3D-style claim. Screenshots require human viewing."},Formatting.Indented);
            var parsed=JObject.Parse(json);Require(parsed["windows"] is JArray w&&w.Count==_windows.Count&&parsed["keys"] is JArray k&&k.Count==_keys.Count&&parsed["screenshots"] is JArray s&&s.Count==_screenshots.Count,"complete nested evidence serialization");File.WriteAllText(ReportPath,json);
        }
        private void Cleanup()
        {
            if(_cleaned)return;_cleaned=true;if(_keyboard!=null){InputSystem.QueueStateEvent(_keyboard,new KeyboardState());InputSystem.RemoveDevice(_keyboard);}
            if(_oldKeyboard!=null&&_oldKeyboard.added)_oldKeyboard.MakeCurrent();if(_oldSettings!=null)InputSystem.settings=_oldSettings;if(_settings!=null)Destroy(_settings);Application.runInBackground=_oldBackground;
        }
        private void EmitSummary(){if(_summaryEmitted)return;_summaryEmitted=true;Diag.Record("scenario","DensityCorpseHarvestNativeSummary",payload:new{runId=RunId,complete=Complete,cases=_audit.Count,failures=Failures});}
        private void OnDestroy(){if(!Finished&&_clock!=null){_fatal="Play interrupted.";Check("native_interrupted",false);Finish();}Cleanup();EmitSummary();foreach(var p in _channels)Diag.SetChannel(p.Key,p.Value);}
    }
}
