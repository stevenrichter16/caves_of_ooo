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
    /// <summary>Ordinary generated-dog fetch, moving recipient and mid-return
    /// checkpoint. One labelled player approach, then actual keyboard actions.
    /// No dog/item/clock/stat/faction/AI edits or forced source outcomes.</summary>
    public sealed class DensityPetNativePlayer : MonoBehaviour
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly (int x,int y)[] Steps = {(1,0),(-1,0),(0,1),(0,-1),(1,1),(1,-1),(-1,1),(-1,-1)};
        private static readonly string[] RequiredChecks = {
            "ordinary_start_original_dagger", "actual_generated_pet_and_route", "one_labelled_player_approach",
            "native_first_throw_admitted", "same_dagger_carried_by_actual_dog", "native_recipient_movement",
            "checkpoint_saved_during_return", "paid_return_mutation_after_save", "checkpoint_restores_exact_return_graph",
            "native_return_and_local_drop", "native_original_dagger_pickup", "native_second_throw_admitted",
            "second_exact_return_no_duplicates", "ordinary_living_finish" };
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
        private readonly List<Diag.Entry> _records = new List<Diag.Entry>();
        private System.Diagnostics.Stopwatch _clock;
        private int _failures, _unexpectedErrors, _moves, _paid, _coins, _dogCoins;
        private string _fatal, _saveRoot, _dogId, _itemId, _checkpointHash, _residentGear;
        private Entity _dog, _dagger;
        private Zone _source;
        private Cell _approach, _landing;
        private Entity Player => _input?.PlayerEntity;
        private Zone Zone => _input?.CurrentZone;
        private OverworldZoneManager Manager => _input.ZoneManager as OverworldZoneManager;
        private Cell At => Zone.GetEntityCell(Player);
        private int Tick => _input.TurnManager.TickCount;
        private int Energy => _input.TurnManager.GetEnergy(Player);
        private int Ambient => Player.GetIntProperty(WorldAmbience.TurnProperty);
        private string State => Field(_input,"_inputState").ToString();
        private GoFetchGoal Fetch => _dog?.GetPart<BrainPart>()?.FindGoal<GoFetchGoal>();
        private string DirectoryPath => Path.GetFullPath(Path.Combine(Application.dataPath,
            "../Docs/Verification/DensityCompletion/Completeness/PetRetrieval/Native",RunId));
        public void Initialize(ScenarioContext context)
        {
            Require(!string.IsNullOrEmpty(SaveGameService.SaveRootOverride), "isolated launcher save root");
            _saveRoot = SaveGameService.SaveRootOverride; _clock = System.Diagnostics.Stopwatch.StartNew();
            foreach (string name in new[]{"scenario","ai","damage","event","worldgen","turn","turn-verbose"})
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
            _input=FindFirstObjectByType<InputHandler>(); Require(_input!=null,"normal input bootstrap");
            var boot=(BootMenuController)Field(_input,"_bootMenuController");Require(boot!=null&&boot.IsActive,"new-game menu");
            yield return Tap(Key.N);yield return Settled();_started=true;
            Require(Manager!=null&&Manager.WorldSeed==64,"actual new seed64 world");
            _coins=TradeSystem.GetDrams(Player);_dagger=Player.GetPart<InventoryPart>().Objects.Single(e=>e.BlueprintName=="Dagger");_itemId=_dagger.ID;
            Check("ordinary_start_original_dagger",Ordinary()&&Player.GetStatValue("Hitpoints")==40&&_coins==50&&Quantity(_dagger)==1);
            yield return Capture("01-ordinary-original-dagger");FindSource();
            Check("actual_generated_pet_and_route",CurrentDog(_source)&&_landing!=null&&ReferenceEquals(Manager.CachedZones[_source.ZoneID],_source));
            string stats=Stats(Player),gear=Gear(Player);int tick=Tick,energy=Energy;var old=Zone;
            Require(Safe(_source,_approach,_dog,5),"fresh actual approach safety");
            Require(old.TryTransferEntityTo(Player,_source,_approach.X,_approach.Y),"single labelled player approach");
            typeof(InputHandler).GetMethod("HandleZoneTransition",Fields).Invoke(_input,new object[]{new ZoneTransitionResult{Success=true,NewZone=_source,NewPlayerX=_approach.X,NewPlayerY=_approach.Y}});
            _input.CameraFollow?.SnapToPlayer();ZoneRenderHooks.MarkFullDirty("DensityPetNativeApproach");
            _notes.Add("ONE LABELLED PLAYER APPROACH "+old.ZoneID+"→"+_source.ZoneID+"@"+_approach.X+","+_approach.Y+" dog="+_dogId+" item="+_itemId);
            yield return Settled();_residentGear=DogGear();_dogCoins=TradeSystem.GetDrams(_dog);
            Check("one_labelled_player_approach",Stats(Player)==stats&&Gear(Player)==gear&&Tick==tick&&Energy==energy&&TradeSystem.GetDrams(Player)==_coins
                &&ReferenceEquals(_dog.GetPart<BrainPart>().CurrentZone,Zone)&&_input.TurnManager.IsRegistered(_dog));
            yield return ThrowOriginal("native_first_throw_admitted");
            for(int n=0;!Carried();n++){Require(n<24&&Fetch!=null,"bounded active outward fetch");yield return WaitPaid("await-acquisition");}
            Check("same_dagger_carried_by_actual_dog",Carried()&&DogGear()==_residentGear&&TradeSystem.GetDrams(_dog)==_dogCoins);
            var original=At;
            for(int n=0;n<2;n++)
            {
                var to=Steps.Select(d=>Zone.GetCell(At.X+d.x,At.Y+d.y)).Where(c=>Safe(Zone,c,_dog,4))
                    .OrderByDescending(c=>SpatialQuery.DistanceToCell(Zone,_dog,c.X,c.Y)).FirstOrDefault();
                Require(to!=null&&SpatialQuery.DistanceToCell(Zone,_dog,to.X,to.Y)>=3,"genuine away-step leaves observable return");
                yield return PaidStep(to,_dog);_moves++;Require(Carried(),"dog still returning after real recipient movement");
            }
            Check("native_recipient_movement",At!=original&&Carried()&&ReferenceEquals(Fetch.Thrower,Player));
            yield return Capture("02-moving-recipient-return");yield return Checkpoint();
            yield return AwaitReturn();Check("native_return_and_local_drop",Returned());
            yield return Capture("04-first-returned-original-dagger");
            yield return WorldMenu(_dagger);yield return Paid(MenuAction("Take"),"take-returned-dagger");
            if(State=="WorldActionMenuOpen")yield return Tap(Key.Escape);yield return Settled();
            Check("native_original_dagger_pickup",Owns(Player,_dagger)&&Quantity(_dagger)==1&&Zone.GetEntityCell(_dagger)==null);
            yield return ThrowOriginal("native_second_throw_admitted");yield return AwaitReturn();
            Check("second_exact_return_no_duplicates",Returned()&&UniquePayload()&&DogGear()==_residentGear&&TradeSystem.GetDrams(_dog)==_dogCoins);
            Check("ordinary_living_finish",Ordinary()&&Player.GetStatValue("Hitpoints")>0&&TradeSystem.GetDrams(Player)==_coins&&CurrentDog(Zone));
            yield return Capture("05-second-return-complete");Observe("complete");
        }
        private void FindSource()
        {
            _source=Manager.GetZone("Overworld.11.10.0");Require(_source!=null,"actual canonical generated glade");
            foreach(var dog in _source.GetReadOnlyEntities().Where(e=>e.BlueprintName=="PetDog").OrderBy(e=>e.ID,StringComparer.Ordinal))
            {
                _dog=dog;_dogId=dog.ID;if(!CurrentDog(_source) || Fetch != null){_notes.Add("REFUSE dog "+dog.ID+" current source");continue;}
                var at=_source.GetEntityCell(dog);
                for(int y=Math.Max(1,at.Y-7);y<=Math.Min(Zone.Height-2,at.Y+7);y++)for(int x=Math.Max(1,at.X-7);x<=Math.Min(Zone.Width-2,at.X+7);x++)
                {
                    int d=AIHelpers.ChebyshevDistance(x,y,at.X,at.Y);if(d<5||d>7)continue;
                    var approach=_source.GetCell(x,y);if(!Safe(_source,approach,dog,5))continue;
                    var landing=ChooseLanding(_source,approach,dog);if(landing==null)continue;
                    _approach=approach;_landing=landing;_notes.Add("FIRST ACTUAL DOG "+dog.ID+"@"+at.X+","+at.Y+" HP="+dog.GetStatValue("Hitpoints")+" gear="+DogGear()+" landing="+landing.X+","+landing.Y);return;
                }
            }
            throw new InvalidOperationException("No legal original-dagger route around the actual glade dog; no alternate seed or source grant.");
        }
        private bool CurrentDog(Zone zone)
        {
            var at=_dog==null?null:zone?.GetEntityCell(_dog);var brain=_dog?.GetPart<BrainPart>();var retriever=_dog?.GetPart<AIRetrieverPart>();
            return _dog?.BlueprintName=="PetDog"&&at!=null&&at.Objects.Contains(_dog)&&_dog.GetStatValue("Hitpoints")>0&&!CombatSystem.IsDeathHandled(_dog)
                &&brain?.ParentEntity==_dog&&retriever?.ParentEntity==_dog&&retriever.AlliesOnly&&retriever.NoticeRadius==10
                &&_dog.GetPart<InventoryPart>()?.ParentEntity==_dog&&_dog.GetPart<PhysicsPart>()?.ParentEntity==_dog&&FactionManager.IsAllied(_dog,Player);
        }
        private Cell ChooseLanding(Zone zone,Cell origin,Entity dog)
        {
            var at=zone.GetEntityCell(dog);int range=HandlingService.GetThrowRange(Player,_dagger);
            for(int y=Math.Max(1,at.Y-5);y<=Math.Min(Zone.Height-2,at.Y+5);y++)for(int x=Math.Max(1,at.X-5);x<=Math.Min(Zone.Width-2,at.X+5);x++)
            {
                int dogDistance=SpatialQuery.DistanceToCell(zone,dog,x,y),playerDistance=AIHelpers.ChebyshevDistance(origin.X,origin.Y,x,y);
                if(dogDistance<3||dogDistance>5||playerDistance<4||playerDistance>range)continue;
                var c=zone.GetCell(x,y);if(!Safe(zone,c,dog,4))continue;
                var trace=LineTargeting.TraceFirstImpactToTarget(zone,Player,origin.X,origin.Y,x,y,range);
                if(trace.HitEntity!=null||trace.BlockedBySolid||trace.ImpactCell!=c)continue;
                if(zone.GetReadOnlyEntities().Any(e=>e!=dog&&e.HasPart<AIRetrieverPart>()&&zone.GetEntityCell(e)!=null
                    &&SpatialQuery.DistanceToCell(zone,e,x,y)<=e.GetPart<AIRetrieverPart>().NoticeRadius))continue;
                var path=FindPath.Search(zone,at.X,at.Y,x,y,actor:dog);if(!path.Usable||path.Steps.Count>12)continue;
                return c;
            }
            return null;
        }
        private IEnumerator ThrowOriginal(string check)
        {
            Require(Owns(Player,_dagger)&&Quantity(_dagger)==1&&CurrentDog(Zone)&&Fetch==null,"same ordinary owned dagger and idle current dog");
            _landing=ChooseLanding(Zone,At,_dog);Require(_landing!=null,"current physically clear no-impact throw and dog path");
            int admitted=FetchRecords("FetchAdmission",null);yield return ItemAction(_dagger,"throw");Require(State=="ThrowTargeting","actual inventory throw cursor");
            var pending=Field(_input,"_pendingThrowTarget");Require(ReferenceEquals(Field(pending,"Item"),_dagger),"exact original throw selection");
            var cursor=(WorldCursorState)Field(_input,"_worldCursorState");
            for(int n=0;cursor.X!=_landing.X||cursor.Y!=_landing.Y;n++)
            {Require(n<20,"bounded real throw cursor");yield return Tap(Direction(Math.Sign(_landing.X-cursor.X),Math.Sign(_landing.Y-cursor.Y)));}
            var trace=LineTargeting.TraceFirstImpactToTarget(Zone,Player,At.X,At.Y,cursor.X,cursor.Y,HandlingService.GetThrowRange(Player,_dagger));
            Require(trace.HitEntity==null&&!trace.BlockedBySolid&&trace.ImpactCell==_landing,"fresh actual no-creature projectile ray");
            yield return Paid(Tap(Key.Enter),"throw-original-dagger");yield return Settled();
            Check(check,!Owns(Player,_dagger)&&Quantity(_dagger)==1&&UniquePayload()&&FetchRecords("FetchAdmission",null)==admitted+1
                &&Fetch!=null&&Fetch.ReturnsToThrower&&ReferenceEquals(Fetch.Item,_dagger)&&ReferenceEquals(Fetch.Thrower,Player));
        }
        private IEnumerator AwaitReturn()
        {
            int dropped = FetchRecords("FetchProgress", "dropped");
            Require(Fetch != null, "this return begins with the actual active saved fetch");
            for (int n = 0; !Returned(); n++)
            {
                Require(n < 36 && Fetch != null && CurrentDog(Zone), "finite ordinary return remains active");
                yield return WaitPaid("await-return");
            }
            Require(FetchRecords("FetchProgress", "dropped") == dropped + 1,
                "one fresh exact completed drop after this return begins, including after F6");
        }
        private IEnumerator WaitPaid(string reason)
        {Require(Safe(Zone,At,_dog,3),"current ordinary waiting footprint");var at=At;yield return Paid(Tap(Key.Period),reason);Require(At==at,"native wait stationary");}
        private IEnumerator Paid(IEnumerator input,string reason)
        {
            Require(_paid<80&&Ordinary()&&CurrentDog(Zone),"finite ordinary paid actions");int tick=Tick,energy=Energy,ambient=Ambient;var actor=Player;
            Diag.Record("scenario","DensityPetNativeKeyStart",actor:Player,target:_dog,payload:new{runId=RunId,reason,item=_itemId});
            string marker=Diag.Snapshot(1).Single().TraceId;
            yield return input;yield return Settled();
            var rows=Diag.Snapshot(Diag.BufferCapacity).SkipWhile(e=>e.TraceId!=marker).ToArray();
            Require(rows.Length>0&&rows[0].TraceId==marker&&rows.Select(e=>e.TraceId).Distinct().Count()==rows.Length,"fresh complete native key window");
            Require(ReferenceEquals(Player,actor)&&OneAction(tick,energy)&&Ambient==ambient+1,"exact single paid action clock/energy");
            _paid++;_records.AddRange(rows);_windows.Add(new{reason,marker,rows});Observe(reason);
        }
        private int FetchRecords(string kind,string reason)
        {
            return _records.Count(r=>r.Category=="ai"&&r.Kind==kind&&r.ActorId==_dogId&&r.TargetId==_itemId
                &&JObject.Parse(r.PayloadJson).Value<string>("thrower")==Player.ID
                &&(kind=="FetchAdmission"?JObject.Parse(r.PayloadJson).Value<string>("outcome")=="accepted":JObject.Parse(r.PayloadJson).Value<string>("reason")==reason));
        }
        private bool Carried()=>CurrentDog(Zone)&&Fetch!=null&&Fetch.ReturnsToThrower&&Fetch.CurrentPhase==GoFetchGoal.Phase.ReturnToThrower
            &&ReferenceEquals(Fetch.Item,_dagger)&&ReferenceEquals(Fetch.Thrower,Player)&&Fetch.ExpectedQuantity==1&&Quantity(_dagger)==1
            &&_dog.GetPart<InventoryPart>().Objects.Contains(_dagger)&&_dagger.GetPart<PhysicsPart>()?.InInventory==_dog
            &&_dagger.GetPart<PhysicsPart>()?.Equipped==null&&Zone.GetEntityCell(_dagger)==null&&UniquePayload();
        private bool Returned()=>CurrentDog(Zone)&&Fetch==null&&Quantity(_dagger)==1&&Zone.GetEntityCell(_dagger)!=null
            &&ReferenceEquals(Zone.GetEntityCell(_dagger),Zone.GetEntityCell(_dog))&&Zone.GetEntityCell(_dagger).Objects.Contains(_dagger)
            &&_dagger.GetPart<PhysicsPart>()?.InInventory==null&&_dagger.GetPart<PhysicsPart>()?.Equipped==null
            &&SpatialQuery.Distance(Zone,_dog,Player)<=1&&UniquePayload();
        private static int Quantity(Entity e)=>e.GetPart<StackerPart>()?.StackCount??1;
        private static bool Owns(Entity owner,Entity item)
        {
            var inv=owner.GetPart<InventoryPart>();var p=item.GetPart<PhysicsPart>();
            return p?.ParentEntity==item&&((inv.Objects.Contains(item)&&p.InInventory==owner&&p.Equipped==null)
                ||(inv.EquippedItems.Values.Contains(item)&&p.Equipped==owner&&owner.GetPart<Body>()?.GetParts().Any(b=>b.Equipped==item)==true));
        }
        private bool UniquePayload()
        {
            // Count ownership roles, not only distinct object references: a
            // duplicated reference in two inventories must still fail. Multiple
            // body slots for one equipped item represent only one owner role.
            var pending = new Stack<Entity>();
            var inspected = new HashSet<Entity>();
            var matches = new List<Entity>();
            Action<Entity> observe = entity =>
            {
                if (entity == null) return;
                if (entity.ID == _itemId) matches.Add(entity);
                pending.Push(entity);
            };
            foreach (var zone in Manager.CachedZones.Values)
                foreach (var entity in zone.GetReadOnlyEntities()) observe(entity);
            while (pending.Count > 0)
            {
                var owner = pending.Pop();
                if (!inspected.Add(owner)) continue;
                var inventory = owner.GetPart<InventoryPart>();
                if (inventory != null)
                {
                    foreach (var item in inventory.Objects) observe(item);
                    foreach (var item in inventory.EquippedItems.Values.Distinct()) observe(item);
                }
                var container = owner.GetPart<ContainerPart>();
                if (container != null)
                    foreach (var item in container.Contents) observe(item);
            }
            return matches.Count == 1 && ReferenceEquals(matches[0], _dagger) && Quantity(_dagger) == 1;
        }
        private string DogGear()=>GearWithout(_dog,_itemId);
        private static string GearWithout(Entity owner,string except)
        {
            var inv=owner.GetPart<InventoryPart>();return string.Join("|",inv.Objects.Where(e=>e.ID!=except).Select(e=>"carry:"+e.ID+":"+e.BlueprintName+":"+Quantity(e))
                .Concat(inv.EquippedItems.Where(p=>p.Value.ID!=except).Select(p=>"equip:"+p.Key+":"+p.Value.ID)).OrderBy(x=>x,StringComparer.Ordinal));
        }
        private static string GoalState(GoFetchGoal g)=>g==null?"none":g.ProgressVersion+"|"+g.CurrentPhase+"|"+g.Age+"|"+g.WalkAttempts+"|"+g.ReturnHome+"|"+g.ExpectedQuantity+"|"+g.ApproachActions+"|"+g.ReturnActions+"|"+g.BlockedActions+"|"+g.DropAttempts+"|"+g.FallbackReason+"|"+g.ReturnsToThrower+"|"+g.RequireAlliedThrower+"|"+g.Thrower?.ID+"|"+g.Item?.ID;
        private IEnumerator Checkpoint()
        {
            Require(Carried(),"save while actual dog carries exact original unit");var player=Player;var dog=_dog;var item=_dagger;var zone=Zone;var goal=Fetch;
            string state=GoalState(goal),playerStats=Stats(Player),dogStats=Stats(dog),gear=Gear(Player),dogGear=Gear(dog);
            var playerAt=At;var dogAt=Zone.GetEntityCell(dog);int tick=Tick,energy=Energy,world=WorldClock.CurrentTick,ambient=Ambient;
            int dogEnergy=_input.TurnManager.GetEnergy(dog);var info=SaveGameService.GetSaveInfo("Quick");Require(info!=null,"actual autosave metadata");
            string path=Path.Combine(_saveRoot,info.GameID,"Quick.sav.gz"),before=HashFile(path);long serial=MessageLog.NextSerialValue;
            yield return Tap(Key.F5);yield return Settled();_checkpointHash=HashFile(path);
            Check("checkpoint_saved_during_return",_checkpointHash!=before&&MessageLog.NextSerialValue>serial&&MessageLog.GetLast()=="Game saved."
                &&SaveGameService.GetSaveInfo("Quick")?.ActiveZoneID==zone.ZoneID&&Carried()&&GoalState(Fetch)==state);
            yield return WaitPaid("post-save-return-mutation");
            Check("paid_return_mutation_after_save",Tick!=tick||Energy!=energy||GoalState(Fetch)!=state);
            Require(HashFile(path)==_checkpointHash,"real unsaved mutation leaves checkpoint bytes unchanged");yield return Capture("03-post-save-real-return-progress");
            yield return Tap(Key.F6);double began=Time.realtimeSinceStartupAsDouble;
            while(ReferenceEquals(Player,player)){Require(Time.realtimeSinceStartupAsDouble-began<8,"actual loaded player graph replacement");yield return null;}
            yield return Settled();_dog=Zone.GetReadOnlyEntities().SingleOrDefault(e=>e.ID==_dogId);_dagger=_dog?.GetPart<InventoryPart>()?.Objects.SingleOrDefault(e=>e.ID==_itemId);
            Check("checkpoint_restores_exact_return_graph",_dog!=null&&_dagger!=null&&!ReferenceEquals(_dog,dog)&&!ReferenceEquals(_dagger,item)&&!ReferenceEquals(Zone,zone)
                &&!ReferenceEquals(Fetch,goal)&&Player.ID==player.ID&&Zone.ZoneID==zone.ZoneID&&Carried()&&GoalState(Fetch)==state
                &&Stats(Player)==playerStats&&Stats(_dog)==dogStats&&Gear(Player)==gear&&Gear(_dog)==dogGear
                &&At.X==playerAt.X&&At.Y==playerAt.Y&&Zone.GetEntityCell(_dog).X==dogAt.X&&Zone.GetEntityCell(_dog).Y==dogAt.Y
                &&Tick==tick&&Energy==energy&&_input.TurnManager.GetEnergy(_dog)==dogEnergy&&WorldClock.CurrentTick==world&&Ambient==ambient
                &&TradeSystem.GetDrams(Player)==_coins&&TradeSystem.GetDrams(_dog)==_dogCoins&&HashFile(path)==_checkpointHash);
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
            yield return Paid(Tap(Direction(to.X-origin.X,to.Y-origin.Y)),"native-step");yield return Settled();
            Require(ReferenceEquals(Player,player)&&ReferenceEquals(At,to)&&OneAction(tick,energy)&&Ambient==ambient+1,"one paid native step reaches chosen current cell");Observe("native-step");
        }
        private bool OneAction(int tick,int energy)=>Energy==energy-TurnManager.ActionThreshold+(Tick-tick)*Player.GetStatValue("Speed",TurnManager.DefaultSpeed);
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
            Require(ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget,target),"exact actual returned dagger");
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
        private void Observe(string phase){_observations.Add(Snapshot(phase));WriteReport();}
        private void Check(string name,bool passed){if(!passed)_failures++;_audit.Add((passed?"PASS ":"FAIL ")+name);Observe(name);Diag.Record("scenario","DensityPetNativeCase",payload:new{runId=RunId,name,passed});}
        private static void Require(bool value,string reason){if(!value)throw new InvalidOperationException("Pet native audit: "+reason);}
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
                    if(error!=null){_fatal=error.ToString();Check("native_precondition_failed",false);Debug.LogError("[DensityPetNative] "+_fatal);break;}
                    if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}if(current is IEnumerator child){stack.Push(child);continue;}yield return current;
                }
            }
            finally{while(stack.Count>0){try{(stack.Pop() as IDisposable)?.Dispose();}catch(Exception e){_fatal+="\nCleanup "+e;}}Finish();}
        }
        public void SetUnexpectedErrors(int count){_unexpectedErrors=count;_errorsFinalized=true;WriteReport();EmitSummary();}
        public void Abort(string reason){if(Finished)return;_fatal=reason;Check("native_aborted",false);StopAllCoroutines();Finish();}
        private void Finish(){Cleanup();Finished=true;WriteReport();}
        private void Cleanup()
        {
            if(_cleaned)return;_cleaned=true;if(_keyboard!=null){InputSystem.QueueStateEvent(_keyboard,new KeyboardState());InputSystem.RemoveDevice(_keyboard);}
            if(_oldKeyboard!=null&&_oldKeyboard.added)_oldKeyboard.MakeCurrent();if(_oldSettings!=null)InputSystem.settings=_oldSettings;if(_settings!=null)Destroy(_settings);Application.runInBackground=_oldBackground;
        }
        private void EmitSummary(){if(_summaryEmitted)return;_summaryEmitted=true;Diag.Record("scenario","DensityPetNativeSummary",payload:new{runId=RunId,complete=Complete,cases=_audit.Count,failures=Failures});}
        private void OnDestroy(){if(!Finished&&_clock!=null){_fatal="Play interrupted.";Check("native_interrupted",false);Finish();}Cleanup();EmitSummary();foreach(var p in _channels)Diag.SetChannel(p.Key,p.Value);}
        private object Snapshot(string phase)=>new{phase,state=_input==null?"unbound":State,zone=_input?.CurrentZone?.ZoneID,
            player=Player?.ID,hp=Player?.GetStatValue("Hitpoints"),x=_input==null?0:At?.X,y=_input==null?0:At?.Y,
            tick=_input==null?0:Tick,energy=_input==null?0:Energy,dog=_dogId,dogHP=_dog?.GetStatValue("Hitpoints"),
            dogX=_dog==null||_input?.CurrentZone==null?(int?)null:_input.CurrentZone.GetEntityCell(_dog)?.X,
            dogY=_dog==null||_input?.CurrentZone==null?(int?)null:_input.CurrentZone.GetEntityCell(_dog)?.Y,goal=GoalState(Fetch),
            item=_itemId,itemQuantity=_dagger==null?0:Quantity(_dagger),itemInventory=_dagger?.GetPart<PhysicsPart>()?.InInventory?.ID,
            itemEquipped=_dagger?.GetPart<PhysicsPart>()?.Equipped?.ID,messages=MessageLog.GetRecent(8)};
        private bool Complete=>Finished&&_errorsFinalized&&Failures==0&&_audit.Count==RequiredChecks.Length&&RequiredChecks.All(n=>_audit.Contains("PASS "+n))&&_screenshots.Count==5;
        private void WriteReport()
        {
            Directory.CreateDirectory(DirectoryPath);ReportPath=Path.Combine(DirectoryPath,"report.json");
            string json=JsonConvert.SerializeObject(new{runId=RunId,complete=Complete,failures=Failures,errorsFinalized=_errorsFinalized,seconds=_clock?.Elapsed.TotalSeconds??0,
                fatal=_fatal,audit=_audit,requiredChecks=RequiredChecks,keys=_keys,observations=_observations,windows=_windows,notes=_notes,screenshots=_screenshots,
                dog=_dogId,originalDagger=_itemId,paidActions=_paid,moves=_moves,checkpointHash=_checkpointHash,
                canVerify="Actual generated PetDog and original starting Dagger, native inventory throw/paid movement/wait/pickup, fresh exact fetch admission and completed local-drop receipts; current moving recipient, finite exact source ownership, F5/real return progress/F6 replacement dog/player/item/goal and numeric save invariants; repeated original-unit fetch with no stock/currency grants.",
                cannotVerify="One labelled living-player approach in actual seed64 glade; not natural discovery, all dogs/seeds, combat safety, feel or animation quality. No dog/item/terrain/stat/faction/reputation/schedule/cooldown grants or direct gameplay actions. Native source route may fail honestly. Old saves lacking a thrower remain a separate legacy bound. Pictures require actual viewing."},Formatting.Indented);
            var parsed=JObject.Parse(json);Require(parsed["windows"] is JArray w&&w.Count==_windows.Count&&parsed["keys"] is JArray k&&k.Count==_keys.Count,"nested native evidence serialized");File.WriteAllText(ReportPath,json);
        }
    }
}
