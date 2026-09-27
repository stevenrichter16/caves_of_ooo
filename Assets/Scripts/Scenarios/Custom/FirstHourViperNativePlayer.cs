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
    /// <summary>One disclosed staged-builder E2 witness. Real native actions, original resources and natural rolls.</summary>
    public sealed class FirstHourViperNativePlayer:MonoBehaviour
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        static readonly (int x,int y)[] Steps={(1,0),(-1,0),(0,1),(0,-1),(1,1),(1,-1),(-1,1),(-1,-1)};
        static readonly string[] RequiredChecks={"ordinary_start","actual_builder_staged_source","native_warning_read","native_south_bypass_asleep","restored_sleeping_graph","native_sight_wake","restored_awake_graph","actual_native_bite_attempt","native_player_melee_defeat","exact_natural_corpse","finite_natural_harvest","restored_depleted_graph","ordinary_staged_finish"};
        public string RunId{get;}=Guid.NewGuid().ToString("N");
        public bool Finished{get;private set;}public int Failures=>_failures+_unexpectedErrors;public string ReportPath{get;private set;}
        InputHandler _input;Keyboard _keyboard,_oldKeyboard;InputSettings _settings,_oldSettings;
        bool _started,_cleaned,_oldBackground,_errorsFinalized,_summaryEmitted,_attempt,_damage,_lethal,_bite,_poisonSeen;
        readonly Dictionary<string,bool> _oldChannels=new Dictionary<string,bool>();
        readonly Dictionary<string,Entity> _startingTonics=new Dictionary<string,Entity>();
        readonly Dictionary<string,Guid> _originalSkills=new Dictionary<string,Guid>();
        readonly List<string> _audit=new List<string>(),_screenshots=new List<string>(),_notes=new List<string>();
        readonly List<object> _keys=new List<object>(),_observations=new List<object>(),_windows=new List<object>();
        readonly List<Diag.Entry> _allActionRows=new List<Diag.Entry>();
        System.Diagnostics.Stopwatch _clock;int _failures,_unexpectedErrors,_localInputs,_completedTurns,_tonics,_casts,_attacks,_yield=-1;
        string _fatal,_ownedRoot,_checkpointHash,_zoneID,_snakeID,_signID,_corpseID;Entity _leader,_sign,_dagger,_corpse;
        Diag.Entry[] _lastRows=Array.Empty<Diag.Entry>();
        string DirectoryPath=>Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/SpreadFirstHour/M5a/Native",RunId));
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
            _zoneID=Manager.RareEncounters.ViperZoneID;Require(!string.IsNullOrEmpty(_zoneID)&&!Manager.CachedZones.ContainsKey(_zoneID)&&Manager.RareEncounters.SelectsViper(Manager,_zoneID),"uncached actual optional Spread address");
            var stage=new Zone(_zoneID);for(int x=15;x<=42;x++)for(int y=2;y<=19;y++)Require(stage.AddEntity(Manager.Factory.CreateEntity("Grass"),x,y),"staged Grass");
            Require(stage.AddEntity(Manager.Factory.CreateEntity("Hedge"),30,10),"one declared hedge geometry fixture");
            Require(new SpreadRareEncounterBuilder(Manager).TryPlace(stage,Manager.Factory),"actual E2 source builder admitted fixture");
            _leader=stage.GetReadOnlyEntities().Single(e=>e.BlueprintName==SpreadRareEncounterPlan.ViperBlueprint);_snakeID=_leader.ID;
            _sign=stage.GetReadOnlyEntities().Single(e=>e.BlueprintName=="Signpost");_signID=_sign.ID;var origin=stage.GetEntityPosition(_leader);
            string snakeStats=Stats(_leader),snakeGear=Gear(_leader);Require(Zone.TryTransferEntityTo(Player,stage,origin.x-6,origin.y),"one disclosed original-player setup transfer");
            typeof(InputHandler).GetMethod("HandleZoneTransition",Private).Invoke(_input,new object[]{new ZoneTransitionResult{Success=true,NewZone=stage,NewPlayerX=origin.x-6,NewPlayerY=origin.y}});
            _input.CameraFollow?.SnapToPlayer();ZoneRenderHooks.MarkFullDirty("FirstHourViperExplicitStage");yield return Settled();
            _notes.Add("EXPLICIT STAGE:504 Grass atx15..42,y2..19 and one Hedge30,10 in actual optional selected Spread address; actual production builder created snake+warning. Original player transferred once to west approach. No manual actor HP/stats/AI/goals/faction/RNG/gear/cooldown changes; native entry retains normal Brain.Rng initialization.");
            Check("actual_builder_staged_source",Zone==stage&&Stats(Player)==stats&&Gear(Player)==gear&&Tick==tick&&Energy==energy&&Stats(_leader)==snakeStats&&Gear(_leader)==snakeGear&&_leader.GetProperty(SpreadRareEncounterBuilder.SourceKey)==_zoneID&&_sign.GetProperty(SpreadRareEncounterBuilder.SourceKey)==_zoneID&&Sleeping()&&_leader.GetStatValue("Hitpoints")==8&&_input.TurnManager.IsRegistered(_leader));
            tick=Tick;energy=Energy;yield return WorldAction(_sign,"Examine");Require(State=="AnnouncementOpen","actual warning full reader");string warning=string.Join("\n",_input.AnnouncementUI.VisibleLines);
            Check("native_warning_read",warning.Contains("poisonous bites")&&warning.Contains("south")&&Tick==tick&&Energy==energy);_notes.Add("NATIVE WARNING "+warning);yield return Capture("01-actual-warning");yield return CloseNormal();
            int hp=Player.GetStatValue("Hitpoints");for(int y=origin.y+1;y<=origin.y+4;y++)yield return MoveTo(origin.x-6,y,"south-bypass");
            for(int x=origin.x-5;x<=origin.x+5;x++)yield return MoveTo(x,origin.y+4,"south-bypass");
            Check("native_south_bypass_asleep",Sleeping()&&Zone.GetEntityPosition(_leader)==origin&&Player.GetStatValue("Hitpoints")==hp&&_leader.GetStatValue("Hitpoints")==8&&!_bite);yield return Capture("02-bypass-and-sleep");
            yield return Checkpoint("restored_sleeping_graph",true);
            yield return ItemAction(_dagger,"equip_auto");yield return CloseNormal();Require(_dagger.GetPart<PhysicsPart>().Equipped==Player,"real original dagger equipped");
            for(int i=0;Sleeping();i++)
            {Require(i<8,"finite sight wake approach");var path=PathTo(c=>SpatialQuery.DistanceToCell(Zone,_leader,c.X,c.Y)<=2);Require(path!=null&&path.Count>0,"actual sight approach");yield return MoveTo(path[0].x,path[0].y,"native-sight-approach");}
            Check("native_sight_wake",Awake()&&_leader.GetPart<AIAmbushPart>().DormantPushed);yield return Capture("03-actual-awake-source");yield return Checkpoint("restored_awake_graph",false);
            for(int i=0;!_bite;i++)
            {Require(i<12,"finite first bite attempt");yield return UseTonic();if(SpatialQuery.Distance(Zone,Player,_leader)<=1)yield return Paid(Tap(Key.Period),"one-native-bite-opportunity");else{var path=PathTo(c=>SpatialQuery.DistanceToCell(Zone,_leader,c.X,c.Y)==1);Require(path!=null&&path.Count>0,"current bite approach");yield return MoveTo(path[0].x,path[0].y,"native-bite-approach");}}
            Check("actual_native_bite_attempt",_bite&&_leader.GetProperty("NaturalWeapon")=="ViperBite");yield return Capture("04-actual-bite-state");
            yield return Fight();Check("native_player_melee_defeat",_attempt&&_damage&&_lethal&&CombatSystem.IsDeathHandled(_leader)&&Zone.GetEntityCell(_leader)==null);
            _corpse=Zone.GetReadOnlyEntities().SingleOrDefault(e=>e.GetProperty("SourceID")==_snakeID);Require(_corpse!=null,"actual inherited corpse drop");_corpseID=_corpse.ID;var harvest=_corpse.GetPart<HarvestablePart>();
            Check("exact_natural_corpse",_corpse.GetProperty("SourceBlueprint")==SpreadRareEncounterPlan.ViperBlueprint&&_corpse.GetProperty("KillerID")==Player.ID&&harvest!=null&&!harvest.Harvested&&harvest.YieldBlueprint=="VenomGland"&&harvest.YieldMin==1&&harvest.YieldMax==1&&harvest.YieldChance==75);
            for(int i=0;SpatialQuery.Distance(Zone,Player,_corpse)>1;i++){Require(i<12,"finite corpse approach");yield return UseTonic();var path=PathTo(c=>SpatialQuery.DistanceToCell(Zone,_corpse,c.X,c.Y)==1);Require(path!=null&&path.Count>0,"current corpse approach");yield return MoveTo(path[0].x,path[0].y,"corpse-approach");}
            yield return WorldAction(_corpse,"Examine");Require(State=="AnnouncementOpen","actual current corpse reader");yield return Capture("05-actual-corpse");yield return CloseNormal();
            yield return Harvest();yield return Checkpoint("restored_depleted_graph",null);Check("ordinary_staged_finish",Ordinary()&&Player.GetStatValue("Hitpoints")>10&&State=="Normal"&&Depleted());yield return Capture("06-restored-finite-outcome");
        }
        bool Ordinary()=>!DevMode.Enabled&&!Player.HasPart<BitLockerPart>()&&!DebugInvincibility.IsEnabled(Player);
        int TonicUnits()=>_startingTonics.Where(p=>p.Value.ID==p.Key&&Player.GetPart<InventoryPart>().Objects.Contains(p.Value)&&p.Value.GetPart<PhysicsPart>()?.InInventory==Player).Sum(p=>Units(p.Value));
        static bool Owns(Entity owner,Entity item)=>item!=null&&((owner.GetPart<InventoryPart>().Objects.Contains(item)&&item.GetPart<PhysicsPart>()?.InInventory==owner)||(owner.GetPart<InventoryPart>().EquippedItems.Values.Contains(item)&&item.GetPart<PhysicsPart>()?.Equipped==owner));
        bool Sleeping()=>_leader!=null&&Zone.GetEntityCell(_leader)!=null&&_leader.GetPart<BrainPart>()?.PeekGoal() is DormantGoal d&&!d.Finished();
        bool Awake()=>_leader!=null&&Zone.GetEntityCell(_leader)!=null&&!_leader.GetPart<BrainPart>().GetGoalsSnapshot().Any(g=>g is DormantGoal d&&!d.Finished());
        bool Ready(string command)=>_originalSkills.TryGetValue(command,out Guid id)&&DensityCombatNativeEvidence.OwnsAbility(Player,id,command,out var ability)&&ability.IsUsable;
        static int Units(Entity item)=>item.GetPart<StackerPart>()?.StackCount??1;
        string Abilities(Entity actor)=>string.Join("|",actor.GetPart<ActivatedAbilitiesPart>()?.AbilityList.OrderBy(a=>a.ID).Select(a=>a.ID+":"+a.Command+":"+a.CooldownRemaining)??Enumerable.Empty<string>());
        static string Goals(Entity actor)=>string.Join("|",actor.GetPart<BrainPart>()?.GetGoalsSnapshot().Select(g=>g.GetType().Name+":"+g.Age+":"+g.GetDetails()+(g is DormantGoal d?":"+d.WakeRequested+":"+d.LastHitpoints+":"+d.HasDamageBaseline:"")+(g is NoFightGoal n?":"+n.Duration+":"+n.Wander:""))??Enumerable.Empty<string>());
        string Ambush(Entity actor){var a=actor.GetPart<AIAmbushPart>();return a==null?"":a.DormantPushed+":"+a.WakeOnDamage+":"+a.WakeOnHostileInSight+":"+a.SleepParticleInterval;}
        IEnumerator Fight()
        {
            for(int n=0;n<30;n++)
            {if(CombatSystem.IsDeathHandled(_leader))yield break;yield return UseTonic();
                if(_casts<1&&_leader.GetStatValue("Hitpoints")>4&&!_leader.HasEffect<FrozenEffect>()&&Ready("CommandRimeGrip")&&TryAim(_leader,5,out int dx,out int dy)){yield return Cast("CommandRimeGrip",_leader,dx,dy);continue;}
                if(SpatialQuery.Distance(Zone,Player,_leader)==1){var c=SpatialQuery.ClosestCell(Zone,_leader,At.X,At.Y);yield return Paid(Tap(Direction(c.X-At.X,c.Y-At.Y)),"native-dagger-attack");Require(_lastRows.Any(r=>r.Category=="damage"&&r.Kind=="HitRoll"&&r.ActorId==Player.ID&&r.TargetId==_snakeID),"fresh exact snake melee attack");_attacks++;}
                else{var path=PathTo(c=>SpatialQuery.DistanceToCell(Zone,_leader,c.X,c.Y)==1);Require(path!=null&&path.Count>0,"finite physical snake pursuit");yield return MoveTo(path[0].x,path[0].y,"native-snake-approach");}}
            throw new InvalidOperationException("Thirty ordinary fight inputs exhausted; no reroll.");
        }
        IEnumerator Paid(IEnumerator action,string label)
        {
            Require(_localInputs<80,"finite80 paid native inputs");var actor=Player;int tick=Tick,energy=Energy,speed=Player.GetStatValue("Speed",100);
            Diag.Record("scenario",DensityCampaignNativeEvidence.MarkerKind,actor:Player,payload:new{runId=RunId,label});string marker=Diag.Snapshot(1).Single().TraceId;
            Diag.Record("scenario",ReferenceGladeCombatEvidence.MarkerKind,Player,_leader);string combat=Diag.Snapshot(1).Single().TraceId;
            yield return action;yield return CloseNormal();_lastRows=Diag.Snapshot(Diag.BufferCapacity).SkipWhile(r=>r.TraceId!=marker).ToArray();
            Require(ReferenceEquals(actor,Player)&&Player.GetStatValue("Speed",100)==speed,"same paid actor/speed");Require(DensityCampaignNativeEvidence.TryClock(_lastRows,marker,Player.ID,"local",energy,Energy,Tick-tick,speed,out var receipt),"actual native clock/energy "+label);
            var proof=ReferenceGladeCombatEvidence.Inspect(_lastRows,combat,Player.ID,_snakeID);Require(proof.WindowValid,"paired exact combat observation");_attempt|=proof.PlayerAttempt;_damage|=proof.PlayerDamage;_lethal|=proof.PlayerLethal;
            _bite|=_lastRows.Any(r=>r.Category=="damage"&&r.Kind=="HitRoll"&&r.ActorId==_snakeID&&r.TargetId==Player.ID&&JObject.Parse(r.PayloadJson)["weapon"]?.Value<string>()=="fangs");_poisonSeen|=Player.HasEffect<PoisonedEffect>();
            _localInputs++;_completedTurns+=receipt.CompletedTurns;_allActionRows.AddRange(_lastRows);_windows.Add(new{label,marker,combat,beforeTick=tick,afterTick=Tick,beforeEnergy=energy,afterEnergy=Energy,receipt,proof,rows=_lastRows});Observe(label);
        }
        int Packed()=>Player.GetPart<InventoryPart>().Objects.Where(e=>e.BlueprintName=="VenomGland"&&e.GetPart<PhysicsPart>()?.InInventory==Player).Sum(Units);
        int Floor()=>Zone.GetReadOnlyEntities().Where(e=>e.BlueprintName=="VenomGland"&&e.GetPart<PhysicsPart>()?.InInventory==null&&e.GetPart<PhysicsPart>()?.Equipped==null).Sum(Units);
        bool Depleted()=>!Manager.CachedZones.Values.SelectMany(z=>z.GetReadOnlyEntities()).Any(e=>e.ID==_snakeID||e.ID==_corpseID||e.GetProperty("SourceID")==_snakeID)&&!Player.GetPart<InventoryPart>().Objects.Any(e=>e.ID==_corpseID);
        string Ground()=>string.Join("|",Zone.GetReadOnlyEntities().Where(e=>e.HasTag("Item")||e.GetProperty("SourceID")==_snakeID).OrderBy(e=>e.ID).Select(e=>e.ID+":"+e.BlueprintName+":"+Units(e)+":"+Zone.GetEntityPosition(e)));
        IEnumerator Harvest()
        {
            yield return UseTonic();int packed=Packed(),floor=Floor();var other=Zone.GetEntityCell(_corpse).Objects.Where(e=>e!=_corpse).ToArray();yield return Paid(WorldAction(_corpse,"Harvest"),"native-one-natural-harvest");
            var rows=_lastRows.Where(r=>r.Category=="loot"&&r.Kind=="Harvested"&&r.ActorId==Player.ID&&r.TargetId==_corpseID).ToArray();Require(rows.Length==1,"one fresh exact committed harvest");var payload=JObject.Parse(rows[0].PayloadJson);int gained=payload["count"].Value<int>(),dropped=payload["dropped"].Value<int>();bool rolled=payload["rollPassed"].Value<bool>();_yield=gained+dropped;
            Check("finite_natural_harvest",payload["yield"].Value<string>()=="VenomGland"&&_yield==(rolled?1:0)&&Packed()-packed==gained&&Floor()-floor==dropped&&_corpse.GetPart<HarvestablePart>().Harvested&&Zone.GetEntityCell(_corpse)==null&&other.All(e=>Zone.GetEntityCell(e)!=null)&&Depleted());
            _notes.Add("ACTUAL75% HARVEST: "+(rolled?"one real venom gland; packed="+gained+",floor="+dropped:"valid zero-yield roll; source spent")+". No retry or RNG override.");
        }
        IEnumerator Checkpoint(string check,bool? asleep)
        {
            yield return UseTonic();var actor=Player;var zone=Zone;var snake=_leader;var sign=_sign;string gear=Gear(Player),stats=Stats(Player),abilities=Abilities(Player),ground=Ground(),snakeStats=Stats(_leader),snakeGear=Gear(_leader),goals=Goals(_leader),ambush=Ambush(_leader);int tick=Tick,energy=Energy,world=WorldClock.CurrentTick,coins=TradeSystem.GetDrams(Player),sx=At.X,sy=At.Y,se=asleep.HasValue?_input.TurnManager.GetEnergy(_leader):0;var snakeAt=Zone.GetEntityPosition(_leader);var signAt=Zone.GetEntityPosition(_sign);
            var info=SaveGameService.GetSaveInfo("Quick");Require(info!=null,"actual initial autosave metadata");string file=Path.Combine(_ownedRoot,info.GameID,"Quick.sav.gz"),before=HashFile(file);yield return Tap(Key.F5);yield return Settled();_checkpointHash=HashFile(file);Require(before!=_checkpointHash&&MessageLog.GetLast()=="Game saved."&&SaveGameService.GetSaveInfo("Quick").ActiveZoneID==_zoneID,"actual fresh checkpoint");
            var candidates=Steps.Select(d=>Zone.GetCell(At.X+d.x,At.Y+d.y)).Where(Safe);if(asleep.HasValue)candidates=candidates.OrderByDescending(c=>SpatialQuery.DistanceToCell(Zone,_leader,c.X,c.Y));var step=candidates.FirstOrDefault();Require(step!=null,"one actual checkpoint mutation step");yield return MoveTo(step.X,step.Y,"checkpoint-real-step");Require(Tick>tick&&HashFile(file)==_checkpointHash,"actual unsaved paid mutation");
            yield return Tap(Key.F6);double began=Time.realtimeSinceStartupAsDouble;while(ReferenceEquals(Player,actor)){Require(Time.realtimeSinceStartupAsDouble-began<8,"F6 replaces actual actor");yield return null;}yield return Settled();
            var restored=Zone.GetReadOnlyEntities().SingleOrDefault(e=>e.ID==_snakeID);_sign=Zone.GetReadOnlyEntities().SingleOrDefault(e=>e.ID==_signID);bool source;
            if(asleep.HasValue){_leader=restored;source=_leader!=null&&!ReferenceEquals(_leader,snake)&&Zone.GetEntityPosition(_leader)==snakeAt&&Stats(_leader)==snakeStats&&Gear(_leader)==snakeGear&&Goals(_leader)==goals&&Ambush(_leader)==ambush&&_input.TurnManager.GetEnergy(_leader)==se&&(asleep.Value?Sleeping():Awake())&&_leader.GetPart<AIAmbushPart>().AmbushSaveStateVersion==1;}
            else source=restored==null&&Depleted();
            Check(check,!ReferenceEquals(Zone,zone)&&Player.ID==actor.ID&&Zone.ZoneID==_zoneID&&ReferenceEquals(Manager.CachedZones[_zoneID],Zone)&&source&&_sign!=null&&!ReferenceEquals(_sign,sign)&&Zone.GetEntityPosition(_sign)==signAt&&_sign.GetProperty(SpreadRareEncounterBuilder.SourceKey)==_zoneID&&Gear(Player)==gear&&Stats(Player)==stats&&Abilities(Player)==abilities&&Ground()==ground&&TradeSystem.GetDrams(Player)==coins&&At.X==sx&&At.Y==sy&&Tick==tick&&Energy==energy&&WorldClock.CurrentTick==world&&HashFile(file)==_checkpointHash);
            _dagger=Player.GetPart<InventoryPart>().Objects.Concat(Player.GetPart<InventoryPart>().EquippedItems.Values).FirstOrDefault(e=>e.ID==_dagger.ID);foreach(string id in _startingTonics.Keys.ToArray()){var item=Player.GetPart<InventoryPart>().Objects.FirstOrDefault(e=>e.ID==id);if(item!=null)_startingTonics[id]=item;else _startingTonics.Remove(id);}
        }
        IEnumerator UseTonic()
        {
            if(_tonics>=2||Player.GetStatValue("Hitpoints")*3>Player.GetStat("Hitpoints").Max*2)yield break;
            var item=_startingTonics.Values.FirstOrDefault(e=>Player.GetPart<InventoryPart>().Objects.Contains(e)&&e.GetPart<PhysicsPart>()?.InInventory==Player);if(item==null)yield break;
            int units=TonicUnits(),tick=Tick,energy=Energy;Diag.Record("scenario",ReferenceGladeCombatEvidence.SupportMarkerKind,Player,Player);string marker=Diag.Snapshot(1).Single().TraceId;
            yield return ItemAction(item,"ApplyTonic");yield return CloseNormal();Require(ReferenceGladeCombatEvidence.HasConsumedTonic(Diag.Snapshot(Diag.BufferCapacity),marker,Player.ID,item.ID,units,TonicUnits())&&Tick==tick&&Energy==energy,"exact original free tonic consumption");_tonics++;Observe("original-tonic");
        }
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
        public void Initialize(ScenarioContext context)
        {
            Require(!string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride),"isolated launcher owns saves");_ownedRoot=SaveGameService.SaveRootOverride;_clock=System.Diagnostics.Stopwatch.StartNew();
            foreach(var channel in new[]{"scenario","event","ai","damage","turn","turn-verbose","skill","loot"}){_oldChannels[channel]=Diag.IsChannelEnabled(channel);Diag.SetChannel(channel,true);}
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
        private static string Stock(Entity actor)=>string.Join("|",actor.GetPart<InventoryPart>().Objects.OrderBy(e=>e.ID,StringComparer.Ordinal).Select(e=>e.ID+":"+e.BlueprintName+":"+(e.GetPart<StackerPart>()?.StackCount??1)+":owner="+e.GetPart<PhysicsPart>()?.InInventory?.ID));
        private static string Gear(Entity actor)
        {
            var inv=actor.GetPart<InventoryPart>();
            return Stock(actor)+";slots="+string.Join("|",inv.EquippedItems.OrderBy(p=>p.Key).Select(p=>p.Key+":"+p.Value.ID))
                +";body="+string.Join("|",(actor.GetPart<Body>()?.GetParts().Where(p=>p.Equipped!=null).OrderBy(p=>p.ID).Select(p=>p.ID+":"+p.Equipped.ID))??Enumerable.Empty<string>())
                +";links="+string.Join("|",inv.EquippedItems.Values.Distinct().OrderBy(e=>e.ID).Select(e=>e.ID+":"+e.BlueprintName+":"+Units(e)+":"+e.GetPart<PhysicsPart>()?.Equipped?.ID));
        }
        private static string Stats(Entity actor)=>string.Join("|",actor.Statistics.OrderBy(p=>p.Key).Select(p=>p.Key+":"+p.Value.BaseValue+":"+p.Value.Value+":"+p.Value.Bonus+":"+p.Value.Penalty+":"+p.Value.Min+":"+p.Value.Max))
            +";effects="+string.Join("|",actor.GetPart<StatusEffectsPart>()?.GetAllEffects().Select(e=>e.GetType().Name+":"+e.Duration+(e is PoisonedEffect p?":"+p.DamageDice:"")).OrderBy(x=>x)??Enumerable.Empty<string>());
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
        private void Check(string name,bool passed){if(!passed)_failures++;_audit.Add((passed?"PASS ":"FAIL ")+name);Observe(name);Diag.Record("scenario","FirstHourViperNativeCase",payload:new{runId=RunId,name,passed});}
        private IEnumerator RunSafely(IEnumerator routine)
        {
            var stack=new Stack<IEnumerator>();stack.Push(routine);
            try
            {
                while(stack.Count>0)
                {
                    bool moved=false;object next=null;Exception error=null;
                    try{moved=stack.Peek().MoveNext();if(moved)next=stack.Peek().Current;}catch(Exception e){error=e;}
                    if(error!=null){_fatal=error.ToString();Check("native_precondition_failed",false);Debug.LogError("[FirstHourViperNative] "+error);break;}
                    if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}if(next is IEnumerator child){stack.Push(child);continue;}yield return next;
                }
            }
            finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();Finish();}
        }
        public void SetUnexpectedErrors(int errors){_unexpectedErrors=errors;_errorsFinalized=true;WriteReport();EmitSummary();}
        public void Abort(string reason){if(Finished)return;_fatal=reason;Check("native_aborted",false);StopAllCoroutines();Finish();}
        void Cleanup(){if(_cleaned)return;_cleaned=true;if(_keyboard!=null){InputSystem.QueueStateEvent(_keyboard,new KeyboardState());InputSystem.RemoveDevice(_keyboard);}if(_oldKeyboard!=null&&_oldKeyboard.added)_oldKeyboard.MakeCurrent();if(_oldSettings!=null)InputSystem.settings=_oldSettings;if(_settings!=null)Destroy(_settings);Application.runInBackground=_oldBackground;}
        void EmitSummary(){if(_summaryEmitted)return;_summaryEmitted=true;Diag.Record("scenario","FirstHourViperNativeSummary",payload:new{runId=RunId,cases=_audit.Count,failures=Failures,complete=Complete});}
        void OnDestroy(){if(!Finished&&_clock!=null){_fatal="Play stopped before completion.";Check("native_interrupted",false);Finish();}Cleanup();EmitSummary();foreach(var p in _oldChannels)Diag.SetChannel(p.Key,p.Value);}
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
        void Observe(string phase)
        {
            if(_input?.PlayerEntity==null)return;_observations.Add(new{phase,player=Player.ID,zone=Zone.ZoneID,x=At?.X,y=At?.Y,hp=Player.GetStatValue("Hitpoints"),stats=Stats(Player),tick=Tick,energy=Energy,gear=Gear(Player),snake=_snakeID,snakeHP=_leader?.GetStatValue("Hitpoints"),snakeAt=_leader==null?null:(object)Zone.GetEntityPosition(_leader),goals=_leader==null?null:Goals(_leader),ambush=_leader==null?null:Ambush(_leader),asleep=Sleeping(),poisonCurrent=Player.HasEffect<PoisonedEffect>(),poisonEver=_poisonSeen,biteAttempt=_bite,harvestYield=_yield,sign=_signID,corpse=_corpseID,message=MessageLog.GetLast()});WriteReport();
        }
        void Finish(){Cleanup();Finished=true;WriteReport();}
        bool Complete=>Finished&&_errorsFinalized&&Failures==0&&_audit.Count==RequiredChecks.Length&&RequiredChecks.All(n=>_audit.Contains("PASS "+n))&&_screenshots.Count>=6;
        void WriteReport()
        {
            Directory.CreateDirectory(DirectoryPath);ReportPath=Path.Combine(DirectoryPath,"report.json");
            string json=JsonConvert.SerializeObject(new{runId=RunId,complete=Complete,failures=Failures,unexpectedErrors=_unexpectedErrors,seconds=_clock?.Elapsed.TotalSeconds??0,fatal=_fatal,audit=_audit,requiredChecks=RequiredChecks,passedChecks=RequiredChecks.Where(n=>_audit.Contains("PASS "+n)).ToArray(),unmetChecks=RequiredChecks.Where(n=>!_audit.Contains("PASS "+n)).ToArray(),keys=_keys,observations=_observations,windows=_windows,notes=_notes,screenshots=_screenshots,localInputs=_localInputs,completedPlayerTurns=_completedTurns,originalTonicsUsed=_tonics,originalControlCasts=_casts,daggerAttacks=_attacks,checkpointHash=_checkpointHash,biteAttempt=_bite,poisonObserved=_poisonSeen,harvestYield=_yield,
                biteWindows=_allActionRows.Where(r=>r.Category=="damage"&&r.ActorId==_snakeID&&r.TargetId==Player.ID).ToArray(),
                intendedCapability="One explicit staged-builder warning/bypass/sleep-save/wake/awake-save/native bite-attempt/combat/actual finite75%harvest/depletion-save witness with ordinary original starter resources.",
                canVerify="Only named passedChecks in this run. Actual bite attempts do not imply a landed hit or poison. HarvestYield0 is a valid observed failed75%roll;1 is the actual natural reward. No manufactured resources or forced RNG. Setup geometry and one player transfer are explicit.",
                cannotVerify="Not ordinary discovery, all-site avoidance, broad balance or guaranteed survival/poison/reward. One fixed-source staged run, no reroll. Pending-wake compatibility is separately covered by native127. Screenshots require visual review."},Formatting.Indented);
            var parsed=JObject.Parse(json);Require(parsed["windows"] is JArray w&&w.Count==_windows.Count&&parsed["observations"] is JArray o&&o.Count==_observations.Count,"nested report retained");File.WriteAllText(ReportPath,json);
        }
    }
}
