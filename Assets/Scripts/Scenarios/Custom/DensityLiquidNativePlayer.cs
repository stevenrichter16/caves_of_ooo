using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Diagnostics;
using CavesOfOoo.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Finite native keyboard evidence for content-completion. Explicitly
    /// uses generated stock and pools in an isolated disposable new game. Travel
    /// shortcuts reposition the unchanged actor; purchases and transfers use real keys.</summary>
    public sealed class DensityLiquidNativePlayer : MonoBehaviour
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        // Source sampling takes one paid fill before leaving. The original
        // 32-cell selection bound excluded every actual acid source in the
        // recorded native sweep. Keep the separate longer visual-site buffer.
        private const int SourceHostileClearanceCells = 12;
        private const int VisualHostileClearanceCells = 32;
        public string RunId { get; } = Guid.NewGuid().ToString("N");
        public bool Finished { get; private set; }
        public int Failures => _failures + _unexpectedErrors;
        public string ReportPath { get; private set; }
        private readonly List<string> _audit = new List<string>();
        private readonly List<string> _screenshots = new List<string>();
        private readonly List<Description> _descriptions = new List<Description>();
        private InputHandler _input;
        private ScenarioContext _context;
        private string _flaskId,_skinId,_waterPuddleId,_waterZoneId,_gameId,_savePath,_checkpointHash,_waterSourceId,_waterSourceZoneId;
        private int _waterSourceRemaining;
        private int _startingCoins,_savedCoins,_savedTick,_savedEnergy;
        private bool _started;
        private (int x,int y) _savedPosition;
        private readonly List<Observation> _observations=new List<Observation>();
        private readonly List<KeyStep> _keys=new List<KeyStep>();
        private sealed class Source {public Zone Zone;public Entity Entity;}
        private static readonly string[] RequiredChecks={"ordinary_start","flask_bought_actual_instance","waterskin_bought_actual_instance","purchased_sources_empty","water_fill_conserves","water_pour_conserves","waterskin_draw_conserves","partial_refill_conserves","water_inspection_exact_and_free","checkpoint_saved","checkpoint_mutation_real","checkpoint_loaded_exact","mixed_menu_refused_without_cost","water_emptied_before_unsafe_sampling","oil_fill_conserves","oil_inspection_exact_and_free","oil_pour_conserves","acid_fill_conserves","acid_inspection_exact_and_free","acid_pour_conserves","ordinary_finish"};
        private Keyboard _keyboard, _oldKeyboard;
        private InputSettings _oldSettings, _settings;
        private bool _oldBackground, _oldScenario, _cleaned, _errorsFinalized, _summaryEmitted;
        private int _failures, _unexpectedErrors;
        private string _ownedRoot, _fatal;
        private System.Diagnostics.Stopwatch _clock;
        private string DirectoryPath => Path.GetFullPath(Path.Combine(Application.dataPath,
            "../Docs/Verification/DensityCompletion/NativeLiquids", RunId));

        public void Initialize(ScenarioContext context)
        {
            if (string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride))
                throw new InvalidOperationException("Completion audit requires its isolated native launcher.");
            _context = context; _ownedRoot = SaveGameService.SaveRootOverride;
            _clock = System.Diagnostics.Stopwatch.StartNew();
            _oldScenario = Diag.IsChannelEnabled("scenario"); Diag.SetChannel("scenario", true);
            _oldSettings = InputSystem.settings; _settings = Instantiate(_oldSettings);
            _settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            _settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = _settings;
            _oldBackground = Application.runInBackground; Application.runInBackground = true;
            _oldKeyboard=Keyboard.current;
            _keyboard = InputSystem.AddDevice<Keyboard>();
            StartCoroutine(RunSafely(RunAudit()));
        }

        private IEnumerator RunAudit()
        {
            yield return new WaitForSecondsRealtime(.8f);
            _input = FindFirstObjectByType<InputHandler>(); Require(_input != null, "ordinary input bootstrap");
            var boot = (BootMenuController)Field(_input, "_bootMenuController");
            Require(boot != null && boot.IsActive, "owned new-game menu");
            yield return Tap(Key.N); yield return Settled();
            Require(!boot.IsActive && Manager().WorldSeed == 64, "native new game seed64"); _started=true;
            RequireOrdinary(); _startingCoins = TradeSystem.GetDrams(_input.PlayerEntity);
            Check("ordinary_start", _input.PlayerEntity.GetStatValue("Hitpoints") == 40 && _startingCoins >= 0);
            var town = Manager().GetZone(MorrowfastSceneRuntime.ZoneID);
            var seller = MorrowfastSceneRuntime.FindOwner(town, "southern-food-vendor");
            Require(seller?.BlueprintName == "MorrowfastSella", "real Sella source");
            var flask = seller.GetPart<InventoryPart>().Objects.Single(e => e.BlueprintName == "LiquidFlask");
            var skin = seller.GetPart<InventoryPart>().Objects.Single(e => e.BlueprintName == "Waterskin");
            yield return Buy(seller, flask, "flask"); _flaskId = flask.ID;
            yield return Buy(seller, skin, "waterskin"); _skinId = skin.ID;
            Check("purchased_sources_empty", FlaskVolume() == 0 && FlaskLiquid() == "" && Skin().GetPart<WaterskinPart>().Charges == 0);

            var water = FindGeneratedSource("water"); Require(water != null, "bounded generated pure-water source");
            PositionForAudit(water.Zone, water.Entity); yield return Settled();
            int oldWater = water.Entity.GetPart<LiquidPoolPart>().Volume; int tick = Tick(), energy = Energy();
            yield return ItemAction(Flask(), FillCommand(water.Entity)); yield return Settled();
            Check("water_fill_conserves", FlaskVolume() == 12 && FlaskLiquid() == "water"
                && water.Entity.GetPart<LiquidPoolPart>().Volume == oldWater - 12 && SingleTurnSince(tick, energy));
            _waterSourceId=water.Entity.ID;_waterSourceZoneId=water.Zone.ZoneID;_waterSourceRemaining=water.Entity.GetPart<LiquidPoolPart>().Volume;
            Observe("water-filled", water.Entity); yield return Capture("water-natural-source-and-filled-flask");
            PositionAtDryPatch(_input.CurrentZone); yield return Settled();
            var standing = _input.CurrentZone.GetEntityPosition(_input.PlayerEntity); int px = standing.x + 1, py = standing.y;
            tick = Tick(); energy = Energy();
            yield return ItemAction(Flask(), PourCommand(px, py)); yield return Settled();
            var puddle = ActualPuddle(px, py, "water"); _waterPuddleId = puddle.ID; _waterZoneId = _input.CurrentZone.ZoneID;
            Check("water_pour_conserves", FlaskVolume() == 0 && FlaskLiquid() == ""
                && puddle.GetPart<LiquidPoolPart>().Volume == 12 && SingleTurnSince(tick, energy));
            Observe("water-poured", puddle); yield return CapturePouredOwner(puddle,"water");
            tick = Tick(); energy = Energy();
            yield return ItemAction(Skin(), "FillWaterskin"); yield return Settled();
            Check("waterskin_draw_conserves", Skin().GetPart<WaterskinPart>().Charges == 3
                && puddle.GetPart<LiquidPoolPart>().Volume == 9 && SingleTurnSince(tick, energy));
            tick = Tick(); energy = Energy();
            yield return ItemAction(Flask(), FillCommand(puddle)); yield return Settled();
            Check("partial_refill_conserves", FlaskVolume() == 9 && FlaskLiquid() == "water"
                && puddle.GetPart<LiquidPoolPart>().Volume == 0 && _input.CurrentZone.GetEntityCell(puddle)==null
                && _input.CurrentZone.TileState.CoatingTurns(px,py,"water")!=ZoneTileState.Permanent && SingleTurnSince(tick, energy));
            Observe("partial-water-refilled", puddle); yield return InspectFlask("water", 9);

            // Save the actual purchased objects and consumed/created pool graph,
            // change it through a real pour, then reload through the native keys.
            var info = SaveGameService.GetSaveInfo("Quick"); Require(info != null, "ordinary new-game checkpoint metadata");
            _gameId = info.GameID; _savePath = Path.Combine(_ownedRoot, _gameId, "Quick.sav.gz");
            string previousHash = HashFile(_savePath); long serial = MessageLog.NextSerialValue;
            _savedCoins = TradeSystem.GetDrams(_input.PlayerEntity); _savedPosition = _input.CurrentZone.GetEntityPosition(_input.PlayerEntity);
            _savedTick = Tick(); _savedEnergy = Energy();
            yield return Tap(Key.F5); yield return Settled(); _checkpointHash = HashFile(_savePath);
            Check("checkpoint_saved", MessageLog.GetLast() == "Game saved." && MessageLog.NextSerialValue > serial
                && SaveGameService.GetSaveInfo("Quick")?.GameID == _gameId
                && SaveGameService.GetSaveInfo("Quick")?.ActiveZoneID == _waterZoneId && _checkpointHash != previousHash);
            tick = Tick(); energy = Energy();
            yield return ItemAction(Flask(), PourCommand(px, py)); yield return Settled();
            var mutationPuddle=ActualPuddle(px,py,"water");string mutationPuddleId=mutationPuddle.ID;
            Check("checkpoint_mutation_real", FlaskVolume() == 0 && mutationPuddle.GetPart<LiquidPoolPart>().Volume == 9
                && mutationPuddleId!=_waterPuddleId
                && SingleTurnSince(tick, energy));
            var oldPlayer = _input.PlayerEntity; yield return Tap(Key.F6);
            double began = Time.realtimeSinceStartupAsDouble;
            while (ReferenceEquals(oldPlayer, _input.PlayerEntity))
            { Require(Time.realtimeSinceStartupAsDouble - began < 8, "F6 replaces player graph"); yield return null; }
            yield return Settled();
            puddle = _input.CurrentZone.GetAllEntities().SingleOrDefault(e => e.ID == _waterPuddleId);
            Check("checkpoint_loaded_exact", _input.CurrentZone.ZoneID == _waterZoneId
                && _input.CurrentZone.GetEntityPosition(_input.PlayerEntity) == _savedPosition
                && FlaskVolume() == 9 && FlaskLiquid() == "water" && Skin().GetPart<WaterskinPart>().Charges == 3
                && puddle==null && !_input.CurrentZone.GetAllEntities().Any(e=>e.ID==mutationPuddleId)
                && _input.CurrentZone.TileState.CoatingTurns(px,py,"water")!=ZoneTileState.Permanent
                && Manager().GetZone(_waterSourceZoneId).GetAllEntities().Single(e=>e.ID==_waterSourceId).GetPart<LiquidPoolPart>().Volume==_waterSourceRemaining
                && TradeSystem.GetDrams(_input.PlayerEntity) == _savedCoins
                && Tick() == _savedTick && Energy() == _savedEnergy && HashFile(_savePath) == _checkpointHash);
            Observe("native-checkpoint-restored", puddle); yield return Capture("restored-real-liquid-state");

            var oil = FindGeneratedSource("oil"); Require(oil != null, "bounded generated oil source");
            PositionForAudit(oil.Zone, oil.Entity); yield return Settled();
            yield return MixedMenuRefusal(oil);
            PositionAtDryPatch(_input.CurrentZone); yield return Settled(); standing = _input.CurrentZone.GetEntityPosition(_input.PlayerEntity);
            tick = Tick(); energy = Energy();
            yield return ItemAction(Flask(), PourCommand(standing.x + 1, standing.y)); yield return Settled();
            Check("water_emptied_before_unsafe_sampling", FlaskVolume() == 0
                && ActualPuddle(standing.x + 1, standing.y, "water").GetPart<LiquidPoolPart>().Volume == 9 && SingleTurnSince(tick, energy));
            yield return SampleAndPour(oil, "oil");
            var acid = FindGeneratedSource("acid"); Require(acid != null, "bounded generated acid source");
            yield return SampleAndPour(acid, "acid");
            RequireOrdinary(); Check("ordinary_finish", State() == "Normal" && FlaskVolume() == 0
                && Skin().GetPart<WaterskinPart>().Charges == 3 && TradeSystem.GetDrams(_input.PlayerEntity) == _savedCoins
                && _input.PlayerEntity.GetStatValue("Hitpoints") == 40);
            Observe("finished", null); yield return Capture("ordinary-finish-no-grants");
        }

        private IEnumerator SampleAndPour(Source source, string liquid)
        {
            PositionForAudit(source.Zone, source.Entity); yield return Settled();
            int oldVolume = source.Entity.GetPart<LiquidPoolPart>().Volume, tick = Tick(), energy = Energy();
            yield return ItemAction(Flask(), FillCommand(source.Entity)); yield return Settled();
            Check(liquid + "_fill_conserves", FlaskLiquid() == liquid && FlaskVolume() == 12
                && source.Entity.GetPart<LiquidPoolPart>().Volume == oldVolume - 12 && SingleTurnSince(tick, energy));
            Observe(liquid + "-filled", source.Entity); yield return InspectFlask(liquid, 12);
            PositionAtDryPatch(_input.CurrentZone); yield return Settled();
            var at = _input.CurrentZone.GetEntityPosition(_input.PlayerEntity); tick = Tick(); energy = Energy();
            yield return ItemAction(Flask(), PourCommand(at.x + 1, at.y)); yield return Settled();
            var puddle = ActualPuddle(at.x + 1, at.y, liquid);
            Check(liquid + "_pour_conserves", FlaskVolume() == 0 && FlaskLiquid() == ""
                && puddle.GetPart<LiquidPoolPart>().Volume == 12 && SingleTurnSince(tick, energy));
            Observe(liquid + "-poured", puddle); yield return CapturePouredOwner(puddle,liquid);
        }
        private IEnumerator MixedMenuRefusal(Source source)
        {
            int tick = Tick(), energy = Energy(), amount = source.Entity.GetPart<LiquidPoolPart>().Volume;
            yield return OpenActions(Flask()); var popup = InvField<object>("_itemActionPopup");
            var actions = ((IList)Field(popup,"Actions")).Cast<object>().Select(a => (string)Field(a,"Command")).ToArray();
            var at = source.Zone.GetEntityPosition(source.Entity);
            bool unavailable = !actions.Contains(FillCommand(source.Entity)) && !actions.Contains(PourCommand(at.x, at.y));
            _descriptions.Add(new Description { subject="mixed-refusal",source="actual inventory commands; forbidden choices are absent",text=string.Join("\n",actions) });
            yield return Capture("unlike-oil-options-refused"); yield return Tap(Key.Escape); yield return Tap(Key.I); yield return Settled();
            Check("mixed_menu_refused_without_cost", unavailable && FlaskLiquid()=="water" && FlaskVolume()==9
                && source.Entity.GetPart<LiquidPoolPart>().Volume==amount && Tick()==tick && Energy()==energy);
            Observe("mixed-menu-refusal",source.Entity);
        }
        private IEnumerator InspectFlask(string liquid,int volume)
        {
            int tick=Tick(),energy=Energy();yield return ItemAction(Flask(),"examine_item");
            double began=Time.realtimeSinceStartupAsDouble;
            while(State()!="AnnouncementOpen"){Require(Time.realtimeSinceStartupAsDouble-began<5,"truthful flask inspection opens");yield return null;}
            string text=(string)Field(_input.AnnouncementUI,"_message");
            string label=LiquidRegistry.Get(liquid)?.DisplayName??liquid;
            bool matches=text.Contains("Contents: "+label+" ("+volume+"/12 volume).")&&text.Contains("tight stopper");
            _descriptions.Add(new Description{subject=liquid+"-flask",source="native item Examine",text=text,units=volume});
            yield return Capture(liquid+"-truthful-inspection");yield return Tap(Key.Escape);Require(State()=="InventoryOpen","inspection returns to inventory");yield return Tap(Key.I);yield return Settled();
            Check(liquid+"_inspection_exact_and_free",matches&&FlaskVolume()==volume&&FlaskLiquid()==liquid&&Tick()==tick&&Energy()==energy);
        }
        private Source FindGeneratedSource(string liquid)
        {
            var manager=Manager();var checkedZones=new HashSet<string>();
            foreach(var zone in manager.CachedZones.Values.ToArray())
            {checkedZones.Add(zone.ZoneID);var found=SourceIn(zone,liquid);if(found!=null)return found;}
            // Actual surface table: acid belongs to Sodden, not Beating. The
            // former twelve-zone Beating budget could never satisfy this route.
            var allowed=liquid=="acid"?new[]{BiomeType.Sodden}:liquid=="oil"?new[]{BiomeType.Sodden,BiomeType.Beating}:new[]{BiomeType.Spread,BiomeType.Sodden,BiomeType.Stump};
            var eligibleColumns=new List<(int x,int y)>();
            foreach(var biome in allowed)
            {
                int perBiome=0,limit=liquid=="acid"?64:12;
                for(int y=0;y<WorldMap.Height&&perBiome<limit;y++)for(int x=0;x<WorldMap.Width&&perBiome<limit;x++)
                {
                    if(manager.WorldMap.GetBiome(x,y)!=biome||manager.WorldMap.GetPOI(x,y)!=null)continue;
                    eligibleColumns.Add((x,y));string id=WorldMap.ToZoneID(x,y,0);if(!checkedZones.Add(id))continue;
                    perBiome++;var zone=manager.GetZone(id);_descriptions.Add(new Description{subject="generated-source-search",source=id,text=liquid+"; actual eligible surface table"});
                    var found=SourceIn(zone,liquid);if(found!=null)return found;
                }
            }
            // The actual ordinary underground table also contains acid. Keep
            // depth one, a fixed32-column bound, and every original safety gate.
            if(liquid=="acid")foreach(var column in eligibleColumns.Take(32))
            {
                string id=WorldMap.ToZoneID(column.x,column.y,1);if(!checkedZones.Add(id))continue;
                var zone=manager.GetZone(id);_descriptions.Add(new Description{subject="generated-source-search",source=id,text="acid; actual ordinary underground table"});
                var found=SourceIn(zone,liquid);if(found!=null)return found;
            }
            WriteReport();return null;
        }
        private Source SourceIn(Zone zone,string liquid)
        {
            if(zone==null)return null;int matching=0;
            foreach(var entity in zone.GetAllEntities())
            {
                var pool=entity.GetPart<LiquidPoolPart>();if(pool==null||pool.LiquidId!=liquid)continue;matching++;
                var at=zone.GetEntityCell(entity);var physics=entity.GetPart<PhysicsPart>();string reason=null;
                if(pool.Volume<12)reason="finite volume below12";
                else if(entity.HasTag("Creature")||physics?.Takeable==true||physics?.InInventory!=null||physics?.Equipped!=null)reason="not an actual ground source";
                else if(zone.GetOccupiedCells(entity).Any(c=>!PureCell(zone,c.X,c.Y,liquid)))reason="unlike pool, renewing source or active coating";
                else if(AdjacentSafeCell(zone,entity)==null)reason="no legal dry approach beyond"+SourceHostileClearanceCells+"-cell hostile clearance";
                var hostile=zone.GetAllEntities().Where(e=>e!=_input.PlayerEntity&&e.HasTag("Creature")&&FactionManager.IsHostile(e,_input.PlayerEntity)).ToArray();
                int nearest=hostile.Length==0?999:hostile.Min(e=>SpatialQuery.DistanceToCell(zone,e,at.X,at.Y));
                _descriptions.Add(new Description{subject="source-candidate",source=zone.ZoneID+":"+entity.ID,text=liquid+" "+entity.BlueprintName+" @ "+at.X+","+at.Y+"; volume="+pool.Volume+"; nearestHostileToSource="+nearest+"; "+(reason??"selected"),units=pool.Volume});
                if(reason==null)foreach(var enemy in hostile)
                {
                    var cell=zone.GetEntityCell(enemy);var brain=enemy.GetPart<BrainPart>();
                    _descriptions.Add(new Description{subject="source-hostile-context",source=zone.ZoneID+":"+enemy.ID,
                        text=enemy.BlueprintName+" @ "+cell?.X+","+cell?.Y+"; distance="+SpatialQuery.DistanceToCell(zone,enemy,at.X,at.Y)
                            +"; speed="+enemy.GetStatValue("Speed",TurnManager.DefaultSpeed)+"; liveBrain="+ReferenceEquals(brain?.CurrentZone,zone)
                            +"; tactics="+(enemy.GetPart<CombatTacticsPart>()?.SkillClasses??"")});
                }
                if(reason==null)return new Source{Zone=zone,Entity=entity};
            }
            _descriptions.Add(new Description{subject="source-zone-summary",source=zone.ZoneID,text=liquid+"; matching pools="+matching,units=matching});return null;
        }
        private bool PureCell(Zone zone,int x,int y,string liquid)
        {
            if(zone.GetOccupants(x,y).Any(e=>e.GetPart<LiquidPoolPart>() is LiquidPoolPart p && p.Volume>0 && p.LiquidId!=liquid))return false;
            if(zone.GetOccupants(x,y).Any(e=>e.GetPart<TileStateSourcePart>() is TileStateSourcePart p&&p.CoatingTurns>0&&!string.IsNullOrEmpty(p.Coating)&&p.Coating!=liquid))return false;
            return zone.TileState.Get(x,y)?.Coatings?.Any(c=>c.Turns>0&&c.Id!=liquid)!=true;
        }
        private Cell AdjacentSafeCell(Zone zone,Entity target)
        {
            foreach(var occupied in zone.GetOccupiedCells(target))
                foreach(var d in new[]{(1,0),(-1,0),(0,-1),(0,1),(1,1),(1,-1),(-1,1),(-1,-1)})
                {var cell=zone.GetCell(occupied.X+d.Item1,occupied.Y+d.Item2);if(SafeStanding(zone,cell,SourceHostileClearanceCells))return cell;}
            return null;
        }
        private bool SafeStanding(Zone zone,Cell cell,int hostileClearance=VisualHostileClearanceCells)
        {
            if(cell==null||cell.BlocksMovement(_input.PlayerEntity)||zone.TileState.Get(cell.X,cell.Y)?.Coatings?.Any(c=>c.Turns>0)==true||zone.TileState.Heat(cell.X,cell.Y)>0)return false;
            if(cell.Occupants.Any(e=>e.HasTag("Trap")||e.HasPart<GasPoolPart>()||e.HasPart<LiquidPoolPart>()||e.HasPart<CampfirePart>()))return false;
            return !zone.GetAllEntities().Any(e=>e!=_input.PlayerEntity&&e.HasTag("Creature")&&FactionManager.IsHostile(e,_input.PlayerEntity)&&SpatialQuery.DistanceToCell(zone,e,cell.X,cell.Y)<=hostileClearance);
        }
        private void PositionAtDryPatch(Zone zone)
        {
            // Acquisition may be in authored Morrowfast, whose separate presenter
            // does not own these ring models. Carry the same liquid into a real
            // supported ordinary zone; never widen authored scene authority.
            var checkedZones=new HashSet<string>();
            foreach(var candidate in new[]{zone}.Concat(Manager().CachedZones.Values.ToArray()))
                if(candidate!=null&&checkedZones.Add(candidate.ZoneID)&&TryVisualDryCell(candidate,out var chosen))
                {TravelTo(candidate,chosen,"supported dry transfer site; held contents unchanged");return;}
            int generated=0;
            for(int y=0;y<WorldMap.Height&&generated<24;y++)for(int x=0;x<WorldMap.Width&&generated<24;x++)
            {
                if(Manager().WorldMap.GetBiome(x,y)!=BiomeType.Spread||Manager().WorldMap.GetPOI(x,y)!=null)continue;
                string id=WorldMap.ToZoneID(x,y,0);if(!checkedZones.Add(id))continue;generated++;
                var candidate=Manager().GetZone(id);
                if(TryVisualDryCell(candidate,out var chosen)){TravelTo(candidate,chosen,"supported generated dry transfer site; held contents unchanged");return;}
            }
            throw new InvalidOperationException("No bounded safe supported dry patch; the audit never clears or stages terrain.");
        }
        private bool TryVisualDryCell(Zone zone,out Cell chosen)
        {
            chosen=null;
            var supports=typeof(SpawnRing3DPresenter).GetMethod("SupportsZone",BindingFlags.NonPublic|BindingFlags.Static);
            Require(supports!=null,"actual ring presenter support predicate");
            if(!(bool)supports.Invoke(null,new object[]{zone.ZoneID})||!AreaCompositionScope.Allows(zone))
            {_descriptions.Add(new Description{subject="dry-site-rejected",source=zone.ZoneID,text="outside current ring presenter authority"});return false;}
            for(int y=3;y<Zone.Height-3;y++)for(int x=3;x<Zone.Width-4;x++)
            {
                bool clear=true;
                for(int dy=-1;dy<=1&&clear;dy++)for(int dx=-1;dx<=2&&clear;dx++)
                {
                    var cell=zone.GetCell(x+dx,y+dy);
                    if(!SafeStanding(zone,cell)||cell.Occupants.Any(e=>e.HasPart<WellPart>()||e.HasPart<TileStateSourcePart>()))clear=false;
                }
                if(clear){chosen=zone.GetCell(x,y);return true;}
            }
            _descriptions.Add(new Description{subject="dry-site-rejected",source=zone.ZoneID,text="no dry unobstructed patch with original32-cell hostile clearance"});return false;
        }
        private void TravelTo(Zone zone,Cell chosen,string reason)
        {
            Require(State()=="Normal","travel shortcut outside menus");var old=_input.CurrentZone;
            Require(old.TryTransferEntityTo(_input.PlayerEntity,zone,chosen.X,chosen.Y),"legal travel shortcut preserves actor");
            if(!ReferenceEquals(old,zone))typeof(InputHandler).GetMethod("HandleZoneTransition",Private).Invoke(_input,new object[]{new ZoneTransitionResult{Success=true,NewZone=zone,NewPlayerX=chosen.X,NewPlayerY=chosen.Y}});
            _input.CameraFollow?.SnapToPlayer();ZoneRenderHooks.MarkFullDirty("DensityLiquidAuditTravel");
            _descriptions.Add(new Description{subject="travel-shortcut",source=zone.ZoneID,text=reason+" @ "+chosen.X+","+chosen.Y});Observe("travel",null);
        }
        private OverworldZoneManager Manager()=>_input.ZoneManager as OverworldZoneManager;
        private Zone ZoneOf(Entity entity)=>Manager().CachedZones.Values.FirstOrDefault(z=>z.GetEntityCell(entity)!=null);
        private Entity Flask()=>_input.PlayerEntity.GetPart<InventoryPart>().Objects.Single(e=>e.ID==_flaskId);
        private Entity Skin()=>_input.PlayerEntity.GetPart<InventoryPart>().Objects.Single(e=>e.ID==_skinId);
        private int FlaskVolume()=>(int)Field(Flask().GetPart("LiquidVessel"),"Volume");
        private string FlaskLiquid()=>(string)Field(Flask().GetPart("LiquidVessel"),"LiquidId");
        private int Tick()=>_input.TurnManager.TickCount;
        private int Energy()=>_input.TurnManager.GetEnergy(_input.PlayerEntity);
        private string FillCommand(Entity pool)=>"FillLiquidVessel|"+Uri.EscapeDataString(pool.ID)+"|"+Uri.EscapeDataString(pool.GetPart<LiquidPoolPart>().LiquidId);
        private string PourCommand(int x,int y)=>"PourLiquidVessel|"+Uri.EscapeDataString(FlaskLiquid())+"|"+FlaskVolume()+"|"+Uri.EscapeDataString(_input.CurrentZone.ZoneID)+"|"+x+"|"+y;
        private Entity ActualPuddle(int x,int y,string liquid)=>_input.CurrentZone.GetOccupants(x,y).Single(e=>e.BlueprintName=="PouredLiquidPool"&&e.GetPart<LiquidPoolPart>()?.LiquidId==liquid);
        private void RequireOrdinary()
        {
            var actor=_input.PlayerEntity;Require(actor.GetStat("Hitpoints").Max==40&&actor.GetStatValue("Strength")==18&&actor.GetStatValue("Agility")==18&&actor.GetStatValue("Toughness")==18,"ordinary unraised stats");
            Require(!DevMode.Enabled&&!actor.HasPart<BitLockerPart>()&&!actor.HasTag("Invulnerable")&&!CombatSystem.IsDeathHandled(actor),"normal living rules");
        }
        private IEnumerator Settled()
        {
            double began=Time.realtimeSinceStartupAsDouble;
            while(State()!="Normal"||_input.ZoneRenderer?.WorldFx?.HasBlockingFx==true)
            {Require(Time.realtimeSinceStartupAsDouble-began<8,"input and FX return to normal: "+State());yield return null;}
            // A labelled same-zone travel shortcut must not leave the previous
            // merchant Look target framing a different place. Use native keys.
            if(_input.CameraFollow?.HasOverrideTarget==true)
            {
                int tick=Tick(),energy=Energy();yield return Tap(Key.L);Require(State()=="LookMode","native recenter Look");yield return Tap(Key.Escape);
                Require(State()=="Normal"&&!_input.CameraFollow.HasOverrideTarget&&Tick()==tick&&Energy()==energy,"native recenter returns to player without a turn");
            }
            yield return null;
        }
        private static string HashFile(string path)
        {using(var sha=System.Security.Cryptography.SHA256.Create())return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}

        private bool SingleTurnSince(int ticks, int energy)
        {
            int elapsed = _input.TurnManager.TickCount - ticks;
            int after = _input.TurnManager.GetEnergy(_input.PlayerEntity);
            int speed = _input.PlayerEntity.GetStatValue("Speed", TurnManager.DefaultSpeed);
            _descriptions.Add(new Description { subject = "turn-cost", source = "scheduler energy accounting", text =
                "ticks=" + elapsed + "; before=" + energy + "; after=" + after + "; speed=" + speed });
            return after == energy - TurnManager.ActionThreshold + elapsed * speed;
        }

        private int Count(string blueprint) => _input.PlayerEntity.GetPart<InventoryPart>().Objects
            .Where(e => e.BlueprintName == blueprint).Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);

        private void PositionForAudit(Zone zone, Entity target)
        {
            Require(zone!=null&&zone.GetEntityCell(target)!=null,"actual source belongs to generated zone");
            var chosen=AdjacentSafeCell(zone,target);Require(chosen!=null,"safe adjacent source position");
            TravelTo(zone,chosen,target.BlueprintName+":"+target.ID);
        }

        private IEnumerator WorldAction(Entity target, string command)
        {
            PositionForAudit(ZoneOf(target), target);
            var from = _input.CurrentZone.GetEntityCell(_input.PlayerEntity);
            var to = SpatialQuery.ClosestCell(_input.CurrentZone, target, from.X, from.Y);
            yield return Tap(Key.L); Require(State() == "LookMode", "native look");
            if (to.X != from.X) yield return Tap(to.X > from.X ? Key.D : Key.A);
            if (to.Y != from.Y) yield return Tap(to.Y > from.Y ? Key.S : Key.W);
            yield return Tap(Key.Enter);
            Require(State() == "WorldActionMenuOpen" && ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget, target), "real source world menu " + target.BlueprintName);
            var actions = (List<InventoryAction>)Field(_input.WorldActionMenuUI, "_actions");
            int index = actions.FindIndex(a => a.Command == command); Require(index >= 0, command + " action");
            string shortcut = MenuShortcutMap.Key(MenuShortcutMap.ForActions(actions)[index]).ToString();
            yield return Tap((Key)Enum.Parse(typeof(Key), shortcut));
        }

        private IEnumerator Buy(Entity seller, Entity item, string label)
        {
            int coins = TradeSystem.GetDrams(_input.PlayerEntity);
            int price = TradeSystem.GetBuyPrice(item, TradeSystem.GetTradePerformance(_input.PlayerEntity), seller);
            Require(coins >= price, "ordinary starting money affords " + label + ": " + coins + " / " + price);
            yield return WorldAction(seller, "Chat");
            Require(ConversationManager.IsActive, "actual authored conversation");
            if ((bool)Field(_input.DialogueUI, "_revealing")) yield return Tap(Key.Enter);
            int trade = ConversationManager.VisibleChoices.ToList().FindIndex(c => c.Actions?.Any(a => a.Key == "StartTrade") == true);
            Require(trade >= 0, "real trade choice");
            yield return Tap((Key)Enum.Parse(typeof(Key), MenuShortcutMap.Key(MenuShortcutMap.Positional(trade)).ToString()));
            Require(_input.TradeUI.IsOpen, "native trade UI");
            var rows = (IList)Field(_input.TradeUI, "_leftRows");
            int row = -1; for (int i = 0; i < rows.Count; i++) if (ReferenceEquals(Field(rows[i], "Item"), item)) row = i;
            Require(row >= 0, "generated item offered for sale");
            for (int step = 0; (int)Field(_input.TradeUI, "_leftCursor") != row; step++)
            { Require(step < 80, "bounded purchase navigation"); yield return Tap(Key.DownArrow); }
            yield return Capture(label + "-actual-stock");
            yield return Tap(Key.Enter); yield return Tap(Key.Enter);
            Check(label + "_bought_actual_instance", _input.PlayerEntity.GetPart<InventoryPart>().Objects.Contains(item)
                && !seller.GetPart<InventoryPart>().Objects.Contains(item) && TradeSystem.GetDrams(_input.PlayerEntity) == coins - price);
            yield return Tap(Key.Escape); Require(State() == "Normal", "trade closes to play");
        }

        private IEnumerator OpenActions(Entity item)
        {
            Require(State()=="Normal","inventory starts from normal input");
            yield return Tap(Key.I);Require(State()=="InventoryOpen","native I inventory");
            yield return Tap(Key.Tab);Require(InvField<int>("_panel")==1,"native inventory item panel");
            int row=RowIndex(item);Require(row>=0,"actual owned item row");
            for(int step=0;InvField<int>("_cursorIndex")!=row;step++)
            {Require(step<80,"bounded inventory navigation");yield return Tap(InvField<int>("_cursorIndex")<row?Key.DownArrow:Key.UpArrow);}
            yield return Tap(Key.Enter);Require(InvField<object>("_itemActionPopup")!=null,"native item actions");
        }
        private IEnumerator ItemAction(Entity item,string command)
        {
            yield return OpenActions(item);object popup=InvField<object>("_itemActionPopup");
            var actions=((IList)Field(popup,"Actions")).Cast<object>().ToArray();
            int index=Array.FindIndex(actions,a=>(string)Field(a,"Command")==command);
            Require(index>=0&&index<25,"visible native action "+command);
            for(int step=0;(int)Field(popup,"CursorIndex")!=index;step++)
            {Require(step<50,"bounded action navigation");yield return Tap((int)Field(popup,"CursorIndex")<index?Key.DownArrow:Key.UpArrow);}
            yield return Tap(Key.Enter);
        }

        private int RowIndex(Entity item)
        {
            var rows = (IList)Field(_input.InventoryUI, "_rows");
            for (int i = 0; i < rows.Count; i++)
                if (ReferenceEquals(((InventoryScreenData.ItemDisplay)Field(rows[i], "Item"))?.Item, item)) return i;
            return -1;
        }
        private T InvField<T>(string name) => (T)Field(_input.InventoryUI, name);
        private string State() => Field(_input, "_inputState").ToString();
        private static object Field(object owner, string name)
        {
            var member = owner.GetType().GetField(name, Private | BindingFlags.Public);
            if (member == null) throw new InvalidOperationException("Missing observed field " + owner.GetType().Name + "." + name);
            return member.GetValue(owner);
        }
        private IEnumerator Tap(params Key[] keys)
        {
            Require(_clock.Elapsed.TotalSeconds < 330, "finite native liquid deadline");
            double began = Time.realtimeSinceStartupAsDouble;
            while (_input != null && Time.time - (float)Field(_input, "_lastMoveTime") < _input.MoveRepeatDelay)
            { Require(Time.realtimeSinceStartupAsDouble - began < 3, "input rate gate reopens"); yield return null; }
            if(_started&&_input?.PlayerEntity!=null)Require(_input.PlayerEntity.GetStatValue("Hitpoints")>10&&!CombatSystem.IsDeathHandled(_input.PlayerEntity),"ordinary HP safety boundary");
            _keys.Add(new KeyStep{sequence=_keys.Count,keys=string.Join(",",keys.Select(k=>k.ToString())),state=_input==null?"bootstrap":State(),tick=_input?.TurnManager?.TickCount??0});WriteReport();
            _keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys)); yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); yield return null;
            yield return new WaitForSecondsRealtime(.13f);
        }
        private IEnumerator CapturePouredOwner(Entity owner,string liquid)
        {
            Require(State()=="Normal"&&_input.CurrentZone.GetEntityCell(owner)!=null,"current poured owner before capture");
            var at=_input.CurrentZone.GetEntityCell(owner);var actorCell=_input.CurrentZone.GetEntityCell(_input.PlayerEntity);int tick=Tick(),energy=Energy();
            Require(Math.Abs(at.X-actorCell.X)<=1&&Math.Abs(at.Y-actorCell.Y)<=1,"poured visual is adjacent to actual player");
            yield return Tap(Key.L);Require(State()=="LookMode","native poured-owner look");
            if(at.X!=actorCell.X)yield return Tap(at.X>actorCell.X?Key.D:Key.A);
            if(at.Y!=actorCell.Y)yield return Tap(at.Y>actorCell.Y?Key.S:Key.W);
            yield return new WaitForSecondsRealtime(.2f);yield return new WaitForEndOfFrame();
            var presenter=_input.ZoneRenderer?.SpawnRing3D;GameObject view=null;string model=null;
            Require(presenter!=null&&ReferenceEquals(presenter.CurrentZone,_input.CurrentZone)&&presenter.IsRenderedEntity(owner)&&presenter.TryGetEntityView(owner,out view,out model),"exact poured owner has a currently rendered native view");
            var definition=LiquidRegistry.Get(liquid);
            Require(definition!=null&&definition.Id==liquid&&owner.GetPart<LiquidPoolPart>()?.LiquidId==liquid,"known exact poured liquid before model comparison");
            string expected=PouredLiquid3DLibrary.ModelId(definition.Color);
            Require(expected!=null&&model==expected,"actual identity-specific model "+liquid+": "+model);
            var camera=presenter.WorldCamera;var renderers=view.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&!r.forceRenderingOff).ToArray();
            Require(camera!=null&&renderers.Length>0,"native camera and submitted owner geometry");
            var bounds=renderers[0].bounds;foreach(var renderer in renderers.Skip(1))bounds.Encapsulate(renderer.bounds);
            float minX=1,minY=1,maxX=0,maxY=0,minDepth=float.PositiveInfinity;
            foreach(float x in new[]{bounds.min.x,bounds.max.x})foreach(float y in new[]{bounds.min.y,bounds.max.y})foreach(float z in new[]{bounds.min.z,bounds.max.z})
            {var v=camera.WorldToViewportPoint(new Vector3(x,y,z));minX=Math.Min(minX,v.x);minY=Math.Min(minY,v.y);maxX=Math.Max(maxX,v.x);maxY=Math.Max(maxY,v.y);minDepth=Math.Min(minDepth,v.z);}
            Require(minDepth>0&&minX>=0&&minY>=0&&maxX<=1&&maxY<=1&&maxX>minX&&maxY>minY,"actual poured mesh bounds must be fully inside native viewport: "+minX+","+minY+".."+maxX+","+maxY);
            _descriptions.Add(new Description{subject="native-poured-visual-preflight",source=owner.ID,text=liquid+"; model="+model+"; zone="+_input.CurrentZone.ZoneID+"; cell="+at.X+","+at.Y+"; viewport="+minX+","+minY+".."+maxX+","+maxY+"; pixels="+(maxX-minX)*camera.pixelWidth+"x"+(maxY-minY)*camera.pixelHeight});
            yield return Capture(liquid+"-poured-native-body-owner-"+owner.ID);yield return Tap(Key.Escape);yield return Settled();
            Require(Tick()==tick&&Energy()==energy,"native visual inspection is free");
        }
        private IEnumerator Capture(string name)
        {
            yield return new WaitForSecondsRealtime(.15f); yield return new WaitForEndOfFrame();
            Directory.CreateDirectory(DirectoryPath);
            string path = Path.Combine(DirectoryPath, name + ".png");
            DensityNativeScreenshot.CaptureToFile(path);
            Require(File.Exists(path) && new FileInfo(path).Length > 0, "screenshot " + name);
            _screenshots.Add(path); WriteReport();
        }
        private static void Require(bool condition, string reason)
        { if (!condition) throw new InvalidOperationException("Density liquid precondition: " + reason); }
        private void Check(string name, bool passed)
        {
            if (!passed) _failures++;
            _audit.Add((passed ? "PASS " : "FAIL ") + name); WriteReport();
            Diag.Record("scenario", "DensityLiquidNativeCase", payload: new { runId = RunId, name, passed });
        }
        private IEnumerator RunSafely(IEnumerator steps)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(steps);
            while (stack.Count > 0)
            {
                bool moved = false; object current = null; Exception error = null;
                try { moved = stack.Peek().MoveNext(); if (moved) current = stack.Peek().Current; }
                catch (Exception caught) { error = caught; }
                if (error != null) { _fatal = error.ToString(); Check("native_precondition_failed", false); Debug.LogError("[DensityLiquidNative] " + error); break; }
                if (!moved) { stack.Pop(); continue; }
                if (current is IEnumerator nested) { stack.Push(nested); continue; }
                yield return current;
            }
            Finish();
        }
        public void SetUnexpectedErrors(int errors)
        { _unexpectedErrors = errors; _errorsFinalized = true; WriteReport(); EmitSummary(); }
        public void Abort(string reason)
        { if (Finished) return; StopAllCoroutines(); _fatal = reason; Check("native_aborted", false); Finish(); }
        private void Finish() { Cleanup(); Finished = true; WriteReport(); }
        private void EmitSummary()
        {
            if (_summaryEmitted) return; _summaryEmitted = true;
            Diag.Record("scenario", "DensityLiquidNativeSummary", payload: new
            { runId = RunId, cases = _audit.Count, failures = Failures, complete = Complete, errorsFinalized = _errorsFinalized, screenshots = _screenshots.Count });
        }
        private bool Complete => Finished && _errorsFinalized && Failures == 0 && RequiredChecks.All(name=>_audit.Contains("PASS "+name)) && _audit.Count==RequiredChecks.Length && _screenshots.Count>=10;
        private void WriteReport()
        {
            Directory.CreateDirectory(DirectoryPath); ReportPath = Path.Combine(DirectoryPath, "report.json");
            File.WriteAllText(ReportPath, JsonUtility.ToJson(new Report
            {
                runId = RunId, cases = _audit.Count, failures = Failures, unexpectedErrors = _unexpectedErrors,
                complete = Complete, errorsFinalized = _errorsFinalized, seconds = _clock?.Elapsed.TotalSeconds ?? 0,
                zone = _input?.CurrentZone?.ZoneID, observations=_observations.ToArray(), nativeKeys=_keys.ToArray(), startingCoins=_startingCoins, checkpointHash=_checkpointHash, fatal = _fatal, audit = _audit.ToArray(), screenshots = _screenshots.ToArray(), descriptions = _descriptions.ToArray(),
                canVerify = "Native new game; real generated Sella stock purchased with starting coins; native inventory fill/pour/Examine; finite water/oil/acid quantities, waterskin compatibility, unavailable unlike-liquid menu choices, ordinary scheduler costs, real F5/F6 object graph and checkpoint evidence including the remote original water source; exact poured native owner/model and fully on-screen viewport bounds; screenshots.",
                cannotVerify = "Only actor travel is shortened. No source, item, currency, HP or terrain is granted or rewritten by the harness. There is no scheduler bypass or manual NPC scheduling. Generated-source scanning is bounded, not natural discovery/frequency evidence. Acid scans real Sodden surface/ordinary underground tables; each matching candidate records refusals. Poured model captures deliberately use supported ordinary zones; authored Morrowfast fallback is not a native-model acceptance claim. Unlike actions are intentionally absent from the menu; executed stale-selection refusals are covered by core tests. Screenshots need visual review; no whole-world balance, natural route, cold-start performance or feel claim."
            }, true));
            Debug.Log("[DensityLiquidNative] report=" + ReportPath + " failures=" + Failures);
        }
        private void Cleanup()
        {
            if (_cleaned) return; _cleaned = true;
            if (_keyboard != null) { InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); InputSystem.RemoveDevice(_keyboard); }
            if (_oldKeyboard != null && _oldKeyboard.added) _oldKeyboard.MakeCurrent();
            if (_oldSettings != null) InputSystem.settings = _oldSettings;
            if (_settings != null) Destroy(_settings);
            Application.runInBackground = _oldBackground;

        }
        private void OnDestroy()
        {
            if (!Finished && _clock != null) { _fatal = "Play stopped before completion."; Check("native_interrupted", false); Finish(); }
            Cleanup(); EmitSummary(); Diag.SetChannel("scenario", _oldScenario);
        }
        private void Observe(string phase,Entity source)
        {
            if(_input?.PlayerEntity==null)return;var actor=_input.PlayerEntity;var at=_input.CurrentZone.GetEntityPosition(actor);
            _observations.Add(new Observation{phase=phase,zone=_input.CurrentZone.ZoneID,x=at.x,y=at.y,tick=Tick(),energy=Energy(),hp=actor.GetStatValue("Hitpoints"),maxHp=actor.GetStat("Hitpoints").Max,coins=TradeSystem.GetDrams(actor),flaskId=_flaskId,flaskLiquid=string.IsNullOrEmpty(_flaskId)?null:FlaskLiquid(),flaskVolume=string.IsNullOrEmpty(_flaskId)?-1:FlaskVolume(),skinCharges=string.IsNullOrEmpty(_skinId)?-1:Skin().GetPart<WaterskinPart>().Charges,sourcePresent=source!=null&&_input.CurrentZone.GetEntityCell(source)!=null,sourceId=source?.ID,sourceBlueprint=source?.BlueprintName,sourceLiquid=source?.GetPart<LiquidPoolPart>()?.LiquidId,sourceVolume=source?.GetPart<LiquidPoolPart>()?.Volume??-1,sourceDisplay=source?.GetDisplayName(),sourceRender=source?.GetPart<RenderPart>()?.RenderString,sourceColor=source?.GetPart<RenderPart>()?.ColorString,inventory=actor.GetPart<InventoryPart>().Objects.Select(e=>e.BlueprintName+":"+e.ID).ToArray(),lastMessage=MessageLog.GetLast()});WriteReport();
        }
        [Serializable]private sealed class Observation
        {public string phase,zone,flaskId,flaskLiquid,sourceId,sourceBlueprint,sourceLiquid,sourceDisplay,sourceRender,sourceColor,lastMessage;public int x,y,tick,energy,hp,maxHp,coins,flaskVolume,skinCharges,sourceVolume;public string[] inventory;public bool sourcePresent;}
        [Serializable]private sealed class KeyStep{public int sequence,tick;public string keys,state;}
        [Serializable] private sealed class Description
        { public string subject, source, text; public int units; }
        [Serializable] private sealed class Report
        {
            public string runId, zone, fatal, canVerify, cannotVerify;
            public string[] audit, screenshots;
            public Description[] descriptions;
            public int cases, failures, unexpectedErrors, startingCoins;
            public string checkpointHash;public Observation[] observations;public KeyStep[] nativeKeys;
            public bool complete, errorsFinalized;
            public double seconds;
        }
    }
}
