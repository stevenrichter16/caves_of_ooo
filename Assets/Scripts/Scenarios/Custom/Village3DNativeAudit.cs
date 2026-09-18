using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Same ordinary native route and 25/25/25 workload in 2D and 3D.
    /// No teleport, fake owners, fog reveal, direct gameplay action or private save load.
    /// Optional GPU/render counters explicitly report unavailable; no zero substitution.</summary>
    public sealed class Village3DNativeAudit : MonoBehaviour
    {
        public bool Finished { get; private set; }
        public int Failures => _failures + _unexpected;
        public string RunId { get; private set; }
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly (int x, int y)[] Directions = { (-1,0), (1,0), (0,-1), (0,1) };
        private const int Capacity=200000;
        private const double PhaseSeconds=25;
        private readonly List<string> _audit=new List<string>();
        private readonly List<string> _screenshots=new List<string>();
        private InputHandler _input;
        private Keyboard _keyboard;
        private InputSettings _oldSettings,_settings;
        private bool _oldBackground,_cleaned,_initialized,_profileActive,_overflow,_completed,_oldMode,_hasMode,_oldLowDetail;
        private int _failures,_unexpected,_nativeSteps,_faunaWaits,_menuCommands,_frames,_phase,_requestedWorldSeed;
        private string _mode,_root,_gameId,_markerId,_fatal,_initialZoneId;
        private int _initialX=-1,_initialY=-1;
        private string[] _startingGarden=Array.Empty<string>();
        private byte[] _markerBytes;
        private System.Diagnostics.Stopwatch _elapsed;
        private MorrowfastSceneDefinition _definition;
        private PropertyInfo _modeProperty;
        private FieldInfo _modeField;
        private ProfilerRecorder[] _recorders;
        private bool[] _valid;
        private long[][] _samples;
        private double[] _seconds,_wallFrames;
        private int[] _phases;
        private double _profileStart;
        private readonly Phase[] _phaseReports={new Phase{name="idle"},new Phase{name="walk"},new Phase{name="door"}};
        private Report _report;
        private DropPickupReceipt _dropPickup;
        private string[] _displayPreferenceKeys;
        private bool[] _displayPreferencePresent;
        private int[] _displayPreferenceValues;
        private EntityCastVisualHandler _castObserver;
        private int _observedNativeCasts, _observedNativeFx;
        private SpellFxMode _savedFxMode;
        private float _savedFxSpeed;
        private bool _fxOverride;
        private string _auditedDaggerId;
        private double Now => _elapsed?.Elapsed.TotalSeconds ?? 0;
        private string Dir=>Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/Village3D"));
        private string Prefix=>"V3D-"+_mode;
        private readonly string[] _names={"COO.ZoneRenderer.LateUpdate","COO.Input.Update","Main Thread","GC Allocated In Frame","Draw Calls Count","Triangles Count","SetPass Calls Count","Render Textures Count","Render Textures Bytes","Texture Memory","GPU Frame Time"};
        private readonly string[] _units={"nanoseconds","nanoseconds","nanoseconds","bytes","count","count","count","count","bytes","bytes","milliseconds"};

        public void Initialize(string mode)
        {
            Require(mode=="before"||mode=="after","Explicit paired variant required.");
            Require(!string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride),"Owned save root before bootstrap required.");
            _requestedWorldSeed=NativeAuditBootstrapSettings.ResolveSeed();
            _mode=mode;_root=SaveGameService.SaveRootOverride;_markerId=PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey);
            _markerBytes=File.ReadAllBytes(Path.Combine(_root,_markerId,"Quick.sav.gz"));
            RunId=Guid.NewGuid().ToString("N");_elapsed=System.Diagnostics.Stopwatch.StartNew();
            if (_mode == "after") SnapshotDisplayPreferences();
            _oldSettings=InputSystem.settings;_settings=Instantiate(_oldSettings);
            _settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            _settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings=_settings;_oldBackground=Application.runInBackground;Application.runInBackground=true;
            _keyboard=InputSystem.AddDevice<Keyboard>();_initialized=true;
            try{Require(_requestedWorldSeed!=0,"Explicit seed must be armed before ordinary bootstrap.");ConfigureMode();StartCoroutine(RunSafely(RunAudit()));}catch(Exception error){_fatal=error.ToString();_failures++;Debug.LogError(error);Finish();}
        }
        private IEnumerator RunSafely(IEnumerator steps)
        {
            var stack=new Stack<IEnumerator>();stack.Push(steps);
            while(stack.Count>0)
            {
                bool moved=false;object current=null;Exception failure=null;
                try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}catch(Exception error){failure=error;}
                if(failure!=null){_fatal=failure.ToString();Check("native_precondition: "+failure.Message,false);Debug.LogError(failure);break;}
                if(!moved){stack.Pop();continue;}if(current is IEnumerator nested){stack.Push(nested);continue;}yield return current;
            }
            _profileActive=false;DisposeRecorders();Finish();
        }
        private IEnumerator RunAudit()
        {
            yield return new WaitForSecondsRealtime(.8f);
            _input=FindFirstObjectByType<InputHandler>();Require(_input!=null,"Ordinary input exists.");
            Require(Screen.width==1920&&Screen.height==1080,"Actual GameView must be 1920x1080; no cropped/manual-camera substitute.");
            var boot=(BootMenuController)typeof(InputHandler).GetField("_bootMenuController",Private).GetValue(_input);
            Require(boot.IsActive,"Fresh owned boot menu required.");yield return Tap(Key.N);
            Require(!boot.IsActive&&State()=="Normal","Native N starts ordinary play.");
            _initialZoneId=_input.CurrentZone.ZoneID;(_initialX,_initialY)=Position();
            Check("native_requested_world_seed",_input.WorldMap.Seed==_requestedWorldSeed);
            Require(_input.WorldMap.Seed==_requestedWorldSeed,"Native N must retain the requested paired world seed.");
            Check("normal_bootstrap_started",_input.PlayerEntity!=null&&_input.CurrentZone.GetEntityCell(_input.PlayerEntity)!=null&&State()=="Normal");
            if (_mode=="after")
            {
                bool authored=MorrowfastSceneRuntime.IsActive(_input.CurrentZone);
                bool entrance=Position()==(40,23);
                Check("native_new_game_starts_in_authored_morrowfast",authored);
                Check("native_new_game_starts_at_south_road_40_23",entrance);
                bool garden=CheckStartingGarden();
                Require(authored&&entrance&&garden,"Ordinary N startup must retain authored Morrowfast (40,23) and exactly six indexed garden terrain cells; see initial coordinates and startingGarden receipts.");
                yield return WaitForScene();yield return Capture("new-game-start");
            }
            if (MorrowfastSceneRuntime.IsActive(_input.CurrentZone))
            { yield return WalkTo((40,0)); yield return CrossEdge(Key.W,FellingSiteBuilder.ZoneID,(40,24)); }
            else
            {
                yield return Tap(Key.LeftShift,Key.Comma);
                Require(WorldMap.IsWorldMapZoneID(_input.CurrentZone.ZoneID),"Native < enters world map.");
                var target=WorldMap.WorldCellToZoneCell(FellingSiteBuilder.WorldX,FellingSiteBuilder.WorldY);
                while(Position().x>target.Item1)yield return Step(Key.A,(-1,0));
                while(Position().x<target.Item1)yield return Step(Key.D,(1,0));
                while(Position().y>target.Item2)yield return Step(Key.W,(0,-1));
                while(Position().y<target.Item2)yield return Step(Key.S,(0,1));
                yield return Tap(Key.LeftShift,Key.Period);Require(FellingSceneRuntime.IsActive(_input.CurrentZone),"Native descent reaches Felling.");
            }
            yield return WalkTo((40,24));double entered=Now;
            yield return CrossEdge(Key.S,MorrowfastSceneRuntime.ZoneID,(40,0));yield return WaitForScene();
            double enterSeconds=Now-entered;
            _definition=MorrowfastSceneDefinition.Load();Require(_definition?.owners.Length==71&&_definition.buildings.Length==5,"Authored Morrowfast contract.");
            Check("all_71_actual_source_owners",_definition.owners.All(o=>Owner(o.id)!=null&&MorrowfastSceneRuntime.IsPresent(_input.CurrentZone,o.id)));
            Check("actual_requested_presentation_ready",ViewReady());
            yield return Capture("initial");yield return Tap(Key.F5);
            _gameId=SaveGameService.GetSaveInfo("Quick")?.GameID;
            Check("native_F5_private_checkpoint",OwnedCheckpoint());
            // A reachable static gatehouse entry, no actor relocation or AI suppression.
            var building=_definition.buildings[0];var door=Owner(building.doorId);
            yield return ApproachEntity(door.ID);var outside=Position();
            Require(!MorrowfastSceneRuntime.IsDoorOpen(_input.CurrentZone,building.doorId),"Initial gatehouse door is closed.");
            yield return OpenEntityMenu(door);yield return NativeCommand(MorrowfastDoorPart.OpenCommand);
            Require(MorrowfastSceneRuntime.IsDoorOpen(_input.CurrentZone,building.doorId),"Warm native door opens.");
            yield return OpenEntityMenu(door);yield return NativeCommand(MorrowfastDoorPart.CloseCommand);
            Require(!MorrowfastSceneRuntime.IsDoorOpen(_input.CurrentZone,building.doorId),"Warm native door closes.");
            yield return new WaitForSecondsRealtime(.3f);
            AllocateProfile();yield return null;yield return null;_profileStart=Now;
            BeginPhase(0);while(Now-_profileStart-_phaseReports[0].startSeconds<PhaseSeconds)yield return null;EndPhase();
            // Repeated real movement on the same short road, rerouting only for live fauna.
            yield return WalkTo((40,3));BeginPhase(1);
            while(Now-_profileStart-_phaseReports[1].startSeconds<PhaseSeconds)
            {yield return WalkTo((40,8));yield return WalkTo((40,3));_phaseReports[1].cycles++;}
            EndPhase();
            yield return ApproachEntity(Owner(building.doorId).ID);BeginPhase(2);
            while(Now-_profileStart-_phaseReports[2].startSeconds<PhaseSeconds)
            {
                door=Owner(building.doorId);yield return OpenEntityMenu(door);yield return NativeCommand(MorrowfastDoorPart.OpenCommand);
                Require(MorrowfastSceneRuntime.IsDoorOpen(_input.CurrentZone,building.doorId),"Native door opens actual state.");
                yield return OpenEntityMenu(door);yield return NativeCommand(MorrowfastDoorPart.CloseCommand);
                Require(!MorrowfastSceneRuntime.IsDoorOpen(_input.CurrentZone,building.doorId),"Native door closes actual state.");_phaseReports[2].cycles++;
            }
            EndPhase();DisposeRecorders();
            Check("three_nonvacuous_25_second_phases",!_overflow&&_frames>300&&_phaseReports.All(p=>p.seconds>=25&&p.seconds<30&&p.frames>50)
                &&_phaseReports[0].nativeSteps==0&&_phaseReports[0].commands==0&&_phaseReports[0].endTick==_phaseReports[0].startTick
                &&_phaseReports[1].nativeSteps>=20&&_phaseReports[1].cycles>=2&&_phaseReports[1].endTick>_phaseReports[1].startTick
                &&_phaseReports[2].commands>=8&&_phaseReports[2].cycles>=4&&_phaseReports[2].endTick>_phaseReports[2].startTick);
            // Functional acceptance is outside timing; each action remains native input.
            if (_mode == "after") Check3DGraph("after_profile");
            door=Owner(building.doorId);yield return OpenEntityMenu(door);yield return NativeCommand(MorrowfastDoorPart.OpenCommand);
            outside=Position();var roomTargets=new HashSet<(int,int)>(building.interior.Select(c=>(c.x,c.y)));
            var roomPath=FindPath(roomTargets,false);Require(roomPath!=null&&roomPath.Count>0,"Opened real interior is reachable.");
            yield return Follow(roomPath,roomTargets);
            Check("native_enters_actual_room",MorrowfastSceneRuntime.GetRoomAt(_input.CurrentZone,Position().x,Position().y)==building.id);
            if(_mode=="before")Check("2d_room_revealed",Presenter().IsRoomRevealed(building.id));
            else Check3DRoom(building, true);
            yield return Capture("interior");yield return WalkTo(outside);
            var roof=Owner(building.roofId);yield return ApproachEntity(roof.ID);yield return OpenEntityMenu(roof);yield return NativeCommand(MorrowfastPropPart.RoofCommand);
            Check("native_manual_roof_actual_state",MorrowfastSceneRuntime.IsRoofLifted(_input.CurrentZone,building.roofId));
            if (_mode == "after") Check3DRoom(building, false);
            yield return Capture("lifted-roof");
            var crate=Owner("western-shop-crate");Require(crate?.GetPart<ContainerPart>()?.Contents.Count>0,"Actual stocked crate required.");
            var grants=crate.GetPart<ContainerPart>().Contents.GroupBy(e=>e.BlueprintName).ToDictionary(g=>g.Key,g=>g.Sum(e=>e.GetPart<StackerPart>()?.StackCount??1));
            var prior=grants.Keys.ToDictionary(k=>k,InventoryUnits);
            yield return ApproachEntity(crate.ID);yield return OpenEntityMenu(crate);yield return NativeCommand("OpenContainer");
            Require(State()=="PickupOpen","Native loot menu opens.");yield return Tap(Key.Tab);if(State()!="Normal")yield return Tap(Key.Escape);
            Check("native_loot_exact_units",crate.GetPart<ContainerPart>().Contents.Count==0&&grants.All(g=>InventoryUnits(g.Key)==prior[g.Key]+g.Value));
            yield return AuditNativeDropAndPickup();
            string crateId=crate.ID,doorId=door.ID,roofId=roof.ID;var oldZone=_input.CurrentZone;var oldPlayer=_input.PlayerEntity;
            var oldPlayerView = _mode == "after" ? Bound3D("$player") : null;
            var inventory=InventorySnapshot();yield return Tap(Key.F5);yield return Tap(Key.F6);yield return WaitForScene();
            Check("native_save_load_rebuilds_actual_graph",OwnedCheckpoint()&&!ReferenceEquals(oldZone,_input.CurrentZone)&&!ReferenceEquals(oldPlayer,_input.PlayerEntity)
                &&Owner("western-shop-crate")?.ID==crateId&&!ReferenceEquals(crate,Owner("western-shop-crate"))&&Owner("western-shop-crate").GetPart<ContainerPart>().Contents.Count==0
                &&Owner(building.doorId)?.ID==doorId&&Owner(building.roofId)?.ID==roofId&&MorrowfastSceneRuntime.IsDoorOpen(_input.CurrentZone,building.doorId)
                &&MorrowfastSceneRuntime.IsRoofLifted(_input.CurrentZone,building.roofId)&&InventorySnapshot()==inventory&&ViewReady()&&_input.WorldMap.Seed==_requestedWorldSeed);
            if (_mode == "after")
            {
                Check3DGraph("after_F6"); Check("native_3d_F6_fresh_player_view", (oldPlayerView == null || !Drawn3D(oldPlayerView)) && oldPlayerView != Bound3D("$player"));
                Check3DGear(_input.PlayerEntity.GetPart<InventoryPart>().GetAllEquipped().SingleOrDefault(e => e.ID == _auditedDaggerId), "after_F6");
                Check3DRoom(building, false, "after_F6");
            }
            yield return WalkTo((40,0));yield return CrossEdge(Key.W,FellingSiteBuilder.ZoneID,(40,24));yield return null;
            Check("native_zone_exit_disables_village_view",!MorrowfastSceneRuntime.IsActive(_input.CurrentZone)&&!Village3DVisible());
            yield return CrossEdge(Key.S,MorrowfastSceneRuntime.ZoneID,(40,0));yield return WaitForScene();
            Check("native_zone_reentry_retains_actual_state",ViewReady()&&Owner("western-shop-crate")?.ID==crateId&&Owner("western-shop-crate").GetPart<ContainerPart>().Contents.Count==0
                &&MorrowfastSceneRuntime.IsDoorOpen(_input.CurrentZone,building.doorId)&&MorrowfastSceneRuntime.IsRoofLifted(_input.CurrentZone,building.roofId)&&InventorySnapshot()==inventory);
            if (_mode == "after")
            {
                // Clear only after the existing crate save/reentry assertions have completed.
                var cleared = Owner("western-shop-crate"); var clearedView = Bound3D("western-shop-crate");
                yield return ApproachEntity(cleared.ID); yield return OpenEntityMenu(cleared); yield return NativeCommand(MorrowfastPropPart.ClearCommand);
                Check("native_3d_empty_crate_clear", Owner("western-shop-crate") == null && !Drawn3D(clearedView) && MorrowfastSceneRuntime.GetState(_input.CurrentZone).WasRemoved("western-shop-crate"));
                yield return Audit3DControlsAndSpell();
            }
            yield return Capture("final");Check("final_private_scope",OwnedCheckpoint()&&_markerBytes.SequenceEqual(File.ReadAllBytes(Path.Combine(_root,_markerId,"Quick.sav.gz")))
                &&!Directory.Exists(Path.Combine(Application.persistentDataPath,"Saves",_markerId)));
            _completed=true;_zoneEnterSeconds=enterSeconds;
        }
        private IEnumerator AuditNativeDropAndPickup()
        {
            // Pickup may auto-equip a carried item; ownership includes each equipped
            // entity once even when it occupies several slots. Carriage alone is not
            // the round-trip invariant. Keep the normal native auto-equip flow intact.
            string before = OwnedInventorySnapshot(); yield return Tap(Key.I); Require(State() == "InventoryOpen", "Native inventory opens."); yield return Tap(Key.Tab);
            var rows = (IList)typeof(InventoryUI).GetField("_rows", Private).GetValue(_input.InventoryUI); int selected = -1; Entity item = null;
            for (int i = 0; i < rows.Count; i++)
            {
                var display = rows[i].GetType().GetField("Item").GetValue(rows[i]);
                if (display == null) continue;
                item = (Entity)display.GetType().GetField("Item").GetValue(display); if (item != null && (_mode != "after" || item.BlueprintName == "Dagger")) { selected = i; break; }
            }
            Require(item != null && selected >= 0, "Real carried item available for a reversible drop/pickup.");
            for (int step = 0; step < rows.Count + 1; step++)
            {
                int cursor = (int)typeof(InventoryUI).GetField("_cursorIndex", Private).GetValue(_input.InventoryUI);
                if (cursor == selected) break;
                Require(cursor < selected, "Inventory selection stays on the intended row."); yield return Tap(Key.DownArrow);
            }
            int units = item.GetPart<StackerPart>()?.StackCount ?? 1, count = InventoryUnits(item.BlueprintName);
            _dropPickup = new DropPickupReceipt { before = CaptureItemOwnership(item) };
            yield return Tap(Key.D);
            _dropPickup.afterDrop = CaptureItemOwnership(item);
            Check("native_inventory_drop_places_real_item", _input.CurrentZone.GetEntityCell(item) != null
                && InventoryUnits(item.BlueprintName) == count - units
                && _dropPickup.afterDrop.carriedReferences == 0 && _dropPickup.afterDrop.equippedSlotReferences == 0
                && string.IsNullOrEmpty(_dropPickup.afterDrop.inInventoryId) && string.IsNullOrEmpty(_dropPickup.afterDrop.equippedById));
            yield return Tap(Key.Escape); Require(State() == "Normal", "Inventory closes before native pickup."); yield return Tap(Key.G);
            if (State() == "PickupOpen")
            {
                var items = (List<Entity>)typeof(PickupUI).GetField("_items", Private).GetValue(_input.PickupUI); int index = items.FindIndex(e => e.ID == item.ID);
                Require(index >= 0, "Dropped individual item appears in the actual pickup popup.");
                int cursor = (int)typeof(PickupUI).GetField("_cursorIndex", Private).GetValue(_input.PickupUI);
                for (; cursor < index; cursor++) yield return Tap(Key.DownArrow);
                for (; cursor > index; cursor--) yield return Tap(Key.UpArrow);
                yield return Tap(Key.Enter); if (State() == "PickupOpen") yield return Tap(Key.Escape);
            }
            _dropPickup.afterPickup = CaptureItemOwnership(item);
            // Exact totals also allow the ordinary stack-merge path. The receipt
            // retains source identity, quantity and location so a failure is inspectable.
            Check("native_pickup_restores_exact_inventory_units", State() == "Normal"
                && OwnedInventorySnapshot() == before && _input.CurrentZone.GetEntityCell(item) == null
                && OwnedItemsHaveConsistentLocations());
            if (_mode == "after") { _auditedDaggerId = item.ID; Check3DGear(item, "after_pickup"); }
        }
        private Village3DPresenter Native3D => Village3D() as Village3DPresenter;
        private static bool Drawn3D(GameObject root) => root != null && root.GetComponentsInChildren<Renderer>(true).Any(r => r.enabled && r.gameObject.activeInHierarchy && !r.forceRenderingOff);
        private GameObject Bound3D(string id)
        {
            Entity expected = id == "$player" ? _input.PlayerEntity : Owner(id), actual = null; GameObject root = null;
            Require(expected != null && Native3D != null && Native3D.TryGetOwnerView(id, out actual, out root) && ReferenceEquals(expected, actual), "Actual 3D owner alias: " + id);
            Require(root.GetComponentsInChildren<Renderer>(true).Length > 0, "Actual imported mesh: " + id); return root;
        }
        private void Check3DGraph(string stage)
        {
            var roots = new HashSet<GameObject>(); foreach (var owner in _definition.owners) Require(roots.Add(Bound3D(owner.id)), "Unique 3D root: " + owner.id);
            Check("native_3d_owner_graph_" + stage, roots.Count == 71 && ReferenceEquals(Native3D.CurrentZone, _input.CurrentZone) && Drawn3D(Bound3D("$player")));
        }
        private void Check3DRoom(MorrowfastSceneDefinition.BuildingSpec room, bool entered, string stage = "after_lift")
        {
            bool hidden = !Drawn3D(Bound3D(room.roofId));
            if (entered)
            {
                var visibleContents = _definition.owners.Where(o => o.roomId == room.id && o.kind != "roof" && o.kind != "door" && o.kind != "building-shell")
                    .Where(o => _input.CurrentZone.GetEntityCell(Owner(o.id))?.IsVisible == true && Owner(o.id).GetPart<RenderPart>()?.Visible != false).ToArray();
                Check("native_3d_entered_roof_and_visible_interior", hidden && visibleContents.Length > 0 && visibleContents.All(o => Drawn3D(Bound3D(o.id))));
            }
            else Check("native_3d_explicit_lift_hides_roof_" + stage, hidden && MorrowfastSceneRuntime.IsRoofLifted(_input.CurrentZone, room.roofId));
        }
        private void Check3DGear(Entity item, string stage)
        {
            Check("native_3d_exact_auto_equipped_dagger_" + stage, item != null && item.BlueprintName == "Dagger" && item.GetPart<PhysicsPart>()?.Equipped == _input.PlayerEntity
                && Native3D.TryGetEquipmentView(_input.PlayerEntity, item, out var gear) && Drawn3D(gear) && gear.transform.IsChildOf(Bound3D("$player").transform));
        }
        private void SnapshotDisplayPreferences()
        {
            _displayPreferenceKeys = new[] { Village3DSettings.PreferenceKey, Village3DSettings.LowDetailPreferenceKey };
            _displayPreferencePresent = _displayPreferenceKeys.Select(PlayerPrefs.HasKey).ToArray();
            _displayPreferenceValues = _displayPreferenceKeys.Select(k => PlayerPrefs.GetInt(k)).ToArray();
        }
        private IEnumerator Audit3DControlsAndSpell()
        {
            Require(!_profileActive && State() == "Normal", "Additional native probes are outside timed phases.");
            var at = Position(); int tick = _input.TurnManager.TickCount, energy = _input.TurnManager.GetEnergy(_input.PlayerEntity);
            yield return Tap(Key.F11); Check("native_F11_original_view", !Village3DSettings.Enabled && !Native3D.PresentationVisible && Presenter().PresentationVisible);
            yield return Tap(Key.F11); yield return WaitForScene(); Check("native_F11_restores_3d", Village3DSettings.Enabled && Native3D.PresentationVisible);
            var full = Native3D.WorldCamera.targetTexture; int width = full.width, height = full.height;
            yield return Tap(Key.LeftShift, Key.F11); var small = Native3D.WorldCamera.targetTexture;
            Check("native_shift_F11_actual_low_detail", Village3DSettings.LowDetail && small != full && small.width < width && small.height < height
                && Math.Abs(small.width - Mathf.RoundToInt(width * .75f)) <= 1 && Math.Abs(small.height - Mathf.RoundToInt(height * .75f)) <= 1);
            yield return Capture("low-detail"); yield return Tap(Key.LeftShift, Key.F11); yield return WaitForScene();
            Check("native_display_controls_take_no_turn", !Village3DSettings.LowDetail && Position() == at && _input.TurnManager.TickCount == tick && _input.TurnManager.GetEnergy(_input.PlayerEntity) == energy);
            // Read-only route/target probes; actual movement and both spell attempts use keys.
            yield return WalkTo((40, 23)); at = Position();
            Require(SpellTargeting.GetCreaturesInCone(_input.CurrentZone, _input.PlayerEntity, at.x, at.y, 0, -1, 2).Count == 0, "Empty native Jet Blast cone required.");
            Require(_input.CurrentZone.GetCell(40,22).IsVisible && !_input.CurrentZone.GetCell(40,22).BlocksMovement(_input.PlayerEntity)
                && _input.CurrentZone.GetCell(40,21).IsVisible && !_input.CurrentZone.GetCell(40,21).BlocksMovement(_input.PlayerEntity), "Known clear two-cell northern path.");
            Require(!_input.CurrentZone.TileState.HasCoating(40,22,"water") && !_input.CurrentZone.TileState.HasCoating(40,21,"water"), "Fresh dry spell centreline.");
            var ability = _input.PlayerEntity.GetPart<ActivatedAbilitiesPart>().GetAbilityBySlot(2);
            Require(ability != null && ability.Command == "CommandJetBlast" && ability.IsUsable, "Ordinary starter slot 3 must be usable Jet Blast.");
            Require(_input.ZoneRenderer.WorldFx != null && !_input.ZoneRenderer.HasBlockingFx && SpellFxBus.PendingCount == 0, "No unresolved spell work before the native probe.");
            _savedFxMode = SpellFxSettings.Mode; _savedFxSpeed = SpellFxSettings.AnimationSpeed; _fxOverride = true;
            SpellFxSettings.Mode = SpellFxMode.Full; SpellFxSettings.AnimationSpeed = 1; yield return null;
            _castObserver = (caster, zone, spell, sx, sy, tx, ty, duration) =>
            {
                if (!ReferenceEquals(caster, _input.PlayerEntity) || !ReferenceEquals(zone, _input.CurrentZone) || spell != "Hydromancy_JetBlast") return;
                _observedNativeCasts++;
                ObserveSpellWork();
            };
            EntityVisualHooks.CastCallback += _castObserver; tick = _input.TurnManager.TickCount; energy = _input.TurnManager.GetEnergy(_input.PlayerEntity);
            var playbackBefore = _input.ZoneRenderer.WorldFx.LastPlayback;
            yield return Tap(Key.Digit3); Require(State() == "AwaitingDirection", "Native starter targeting starts."); yield return Tap(Key.Escape);
            Check("native_spell_escape_no_cast", State() == "Normal" && _observedNativeCasts == 0 && ReferenceEquals(playbackBefore, _input.ZoneRenderer.WorldFx.LastPlayback)
                && Position() == at && _input.TurnManager.TickCount == tick && _input.TurnManager.GetEnergy(_input.PlayerEntity) == energy && ability.IsUsable && !_input.CurrentZone.TileState.HasCoating(40,22,"water"));
            yield return Tap(Key.Digit3); Require(State() == "AwaitingDirection", "Native starter targeting restarts.");
            // Keep the ordinary pulse; capture on its first coroutine boundary after the real cast.
            bool captured = false; var pulse = Tap(Key.W);
            while (pulse.MoveNext()) { yield return pulse.Current; if (_observedNativeCasts > 0) ObserveSpellWork(); if (!captured && _observedNativeCasts > 0) { captured = true; yield return Capture("native-jet-blast"); } }
            double until = Now + WorldFxPlayback.HardTimeoutSeconds + 1; while (State() != "Normal" && Now < until) yield return null;
            var playback = _input.ZoneRenderer.WorldFx.LastPlayback;
            Check("native_spell_real_cast_fx_and_turn", _observedNativeCasts == 1 && _observedNativeFx > 0 && captured && !ReferenceEquals(playbackBefore, playback)
                && playback?.State == WorldFxPlaybackState.Completed && State() == "Normal" && Position() == at && _input.TurnManager.TickCount > tick && ability.CooldownRemaining > 0
                && _input.CurrentZone.TileState.HasCoating(40,22,"water") && _input.CurrentZone.TileState.HasCoating(40,21,"water"));
            EntityVisualHooks.CastCallback -= _castObserver; _castObserver = null;
            SpellFxSettings.Mode = _savedFxMode; SpellFxSettings.AnimationSpeed = _savedFxSpeed; _fxOverride = false;
        }
        private void ObserveSpellWork()
        {
            // CastCallback can precede capture commit. Also sample the real input
            // pulse after its frame boundary, when the selected backend owns work.
            // Native 3D draws are not sprite/ASCII work and must be counted explicitly.
            var fx = _input.ZoneRenderer.WorldFx;
            _observedNativeFx = Math.Max(_observedNativeFx, SpellFxBus.PendingCount + AsciiFxBus.PendingCount
                + (fx?.SpriteRenderer.ActiveCount ?? 0) + (fx?.NativeRenderer.ActiveCount ?? 0));
        }
        private void RestoreProbePreferencesAndObserver()
        {
            if (_castObserver != null) { EntityVisualHooks.CastCallback -= _castObserver; _castObserver = null; }
            if (_fxOverride) { SpellFxSettings.Mode = _savedFxMode; SpellFxSettings.AnimationSpeed = _savedFxSpeed; _fxOverride = false; }
            if (_displayPreferenceKeys == null) return;
            for (int i = 0; i < _displayPreferenceKeys.Length; i++)
                if (_displayPreferencePresent[i]) PlayerPrefs.SetInt(_displayPreferenceKeys[i], _displayPreferenceValues[i]); else PlayerPrefs.DeleteKey(_displayPreferenceKeys[i]);
            PlayerPrefs.Save();
            bool restored = Enumerable.Range(0, _displayPreferenceKeys.Length).All(i => PlayerPrefs.HasKey(_displayPreferenceKeys[i]) == _displayPreferencePresent[i]
                && (!_displayPreferencePresent[i] || PlayerPrefs.GetInt(_displayPreferenceKeys[i]) == _displayPreferenceValues[i]));
            if (_report != null) { _report.displayPreferencesRestored = restored; if (!restored) _failures++; WriteReport(); }
            _displayPreferenceKeys = null;
        }
        private double _zoneEnterSeconds;
        private void AllocateProfile()
        {
            _recorders=new ProfilerRecorder[_names.Length];_valid=new bool[_names.Length];_samples=new long[_names.Length][];
            _seconds=new double[Capacity];_wallFrames=new double[Capacity];_phases=new int[Capacity];
            for(int i=0;i<_names.Length;i++)
            {
                _samples[i]=new long[Capacity];var category=i<2?ProfilerCategory.Scripts:i==2?ProfilerCategory.Internal:i==3||i==9?ProfilerCategory.Memory:ProfilerCategory.Render;
                var options=ProfilerRecorderOptions.Default;if(i<2)options|=ProfilerRecorderOptions.SumAllSamplesInFrame;
                _recorders[i]=ProfilerRecorder.StartNew(category,_names[i],2,options);_valid[i]=_recorders[i].Valid;if(_valid[i])_units[i]=_recorders[i].UnitType.ToString();
                if(i<4)Require(_valid[i],"Required profiler marker unavailable: "+_names[i]);
            }
        }
        private void BeginPhase(int index)
        {
            Require(State()=="Normal"&&ViewReady()&&Screen.width==1920&&Screen.height==1080,"Healthy native phase boundary.");
            _phase=index;var p=_phaseReports[index];p.startSeconds=Now-_profileStart;p.startTick=_input.TurnManager.TickCount;p.startSteps=_nativeSteps;p.startCommands=_menuCommands;
            var at=Position();p.startX=at.x;p.startY=at.y;_profileActive=true;
        }
        private void EndPhase()
        {
            _profileActive=false;var p=_phaseReports[_phase];p.seconds=Now-_profileStart-p.startSeconds;p.endTick=_input.TurnManager.TickCount;
            p.nativeSteps=_nativeSteps-p.startSteps;p.commands=_menuCommands-p.startCommands;var at=Position();p.endX=at.x;p.endY=at.y;
            Require(State()=="Normal"&&ViewReady(),"Phase ended in live healthy native presentation.");
        }
        private void LateUpdate()
        {
            if(!_profileActive||_recorders==null)return;if(_frames>=Capacity){_overflow=true;_profileActive=false;return;}
            _seconds[_frames]=Now-_profileStart;_phases[_frames]=_phase;_wallFrames[_frames]=Time.unscaledDeltaTime*1000.0;
            for(int i=0;i<_recorders.Length;i++){_valid[i]&=_recorders[i].Valid;_samples[i][_frames]=_recorders[i].Valid?_recorders[i].LastValue:-1;}
            _phaseReports[_phase].frames++;_frames++;
        }
        private void DisposeRecorders()
        {if(_recorders==null)return;for(int i=0;i<_recorders.Length;i++){_valid[i]&=_recorders[i].Valid;_recorders[i].Dispose();}_recorders=null;}
        private IEnumerator NativeCommand(string command){yield return SelectCommand(command);_menuCommands++;}
        private IEnumerator CrossEdge(Key key,string expectedZone,(int x,int y) expectedPosition)
        {Require(State()=="Normal","Normal native edge input.");yield return Tap(key);_nativeSteps++;Require(_input.CurrentZone.ZoneID==expectedZone&&Position()==expectedPosition,"Exact native zone edge result.");}
        private string InventorySnapshot()=>string.Join("|",_input.PlayerEntity.GetPart<InventoryPart>().Objects.GroupBy(e=>e.BlueprintName).OrderBy(g=>g.Key).Select(g=>g.Key+":"+g.Sum(e=>e.GetPart<StackerPart>()?.StackCount??1)));
        private Entity[] OwnedItems()
        {
            var inventory = _input.PlayerEntity.GetPart<InventoryPart>();
            return inventory.Objects.Concat(inventory.GetAllEquipped()).Where(e => e != null).Distinct().ToArray();
        }
        private static string UnitSnapshot(IEnumerable<Entity> items) => string.Join("|", items.Where(e => e != null).Distinct()
            .GroupBy(e => e.BlueprintName).OrderBy(g => g.Key)
            .Select(g => g.Key + ":" + g.Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1)));
        private string OwnedInventorySnapshot() => UnitSnapshot(OwnedItems());
        private bool OwnedItemsHaveConsistentLocations()
        {
            var player = _input.PlayerEntity; var inventory = player.GetPart<InventoryPart>();
            foreach (var item in OwnedItems())
            {
                bool carried = inventory.Objects.Contains(item), equipped = inventory.GetAllEquipped().Contains(item);
                var physics = item.GetPart<PhysicsPart>();
                if (carried == equipped || _input.CurrentZone.GetEntityCell(item) != null
                    || (item.GetPart<StackerPart>()?.StackCount ?? 1) <= 0) return false;
                if (physics != null && carried && (physics.InInventory != player || physics.Equipped != null)) return false;
                if (physics != null && equipped && (physics.Equipped != player || physics.InInventory != null)) return false;
            }
            return true;
        }
        private static string EntityReceipt(Entity item) => item.ID + ":" + item.BlueprintName + ":" + (item.GetPart<StackerPart>()?.StackCount ?? 1);
        private ItemOwnership CaptureItemOwnership(Entity item)
        {
            var inventory = _input.PlayerEntity.GetPart<InventoryPart>(); var physics = item.GetPart<PhysicsPart>();
            var ground = _input.CurrentZone.GetEntityCell(item); var playerCell = _input.CurrentZone.GetEntityCell(_input.PlayerEntity);
            return new ItemOwnership
            {
                inputState = State(), zoneId = _input.CurrentZone.ZoneID, itemId = item.ID, blueprint = item.BlueprintName,
                units = item.GetPart<StackerPart>()?.StackCount ?? 1,
                carriedUnits = UnitSnapshot(inventory.Objects), equippedUnits = UnitSnapshot(inventory.GetAllEquipped()), ownedUnits = OwnedInventorySnapshot(),
                carriedEntities = inventory.Objects.Where(e => e != null).Select(EntityReceipt).OrderBy(x => x).ToArray(),
                equippedSlots = inventory.EquippedItems.OrderBy(p => p.Key).Select(p => p.Key + "=" + (p.Value == null ? "null" : EntityReceipt(p.Value))).ToArray(),
                carriedReferences = inventory.Objects.Count(e => ReferenceEquals(e, item)),
                equippedSlotReferences = inventory.EquippedItems.Values.Count(e => ReferenceEquals(e, item)),
                inInventoryId = physics?.InInventory?.ID ?? "", equippedById = physics?.Equipped?.ID ?? "",
                groundCell = ground == null ? "absent" : ground.X + "," + ground.Y,
                playerCell = playerCell == null ? "absent" : playerCell.X + "," + playerCell.Y,
                playerCellEntities = playerCell == null ? Array.Empty<string>() : playerCell.Objects.Select(EntityReceipt).OrderBy(x => x).ToArray()
            };
        }
        private bool CheckStartingGarden()
        {
            var zone=_input.CurrentZone;var expected=new HashSet<Entity>();var receipts=new List<string>();
            var indexed=zone.GetEntitiesWithTag("Plantable");bool cellsValid=true;
            for(int x=41;x<=42;x++)for(int y=21;y<=23;y++)
            {
                var cell=zone.GetCell(x,y);
                var terrain=cell?.Objects.Where(e=>e!=null&&e.HasTag("Terrain")).ToArray()??Array.Empty<Entity>();
                Entity tile=terrain.Length==1?terrain[0]:null;
                bool valid=tile!=null&&tile.ID=="morrowfast-terrain:"+x+":"+y&&tile.BlueprintName=="TepuiStone"
                    &&tile.HasTag(MorrowfastSceneRuntime.TerrainTag)&&tile.HasTag("Plantable")
                    &&ReferenceEquals(zone.GetEntityCell(tile),cell);
                if(tile!=null)expected.Add(tile);cellsValid&=valid;
                receipts.Add(x+","+y+";terrainCount="+terrain.Length+";id="+tile?.ID+";blueprint="+tile?.BlueprintName
                    +";plantable="+(tile?.HasTag("Plantable")==true)+";indexedRefs="+indexed.Count(e=>ReferenceEquals(e,tile))+";valid="+valid);
            }
            _startingGarden=receipts.ToArray();
            bool indexValid=expected.Count==6&&indexed.Count==6&&indexed.All(expected.Contains)
                &&expected.All(e=>indexed.Count(v=>ReferenceEquals(v,e))==1);
            var tagged=zone.GetReadOnlyEntities().Where(e=>e.HasTag("Plantable")).ToArray();
            bool noExtras=tagged.Length==6&&tagged.All(expected.Contains)
                &&!zone.GetCell(40,23).Objects.Any(e=>e.HasTag("Plantable"));
            Check("native_new_game_six_exact_authored_garden_terrain",cellsValid);
            Check("native_new_game_garden_tag_index_matches_six_refs",indexValid);
            Check("native_new_game_only_garden_plantable_road_excluded",noExtras);
            return cellsValid&&indexValid&&noExtras;
        }
        private Entity Owner(string id)=>MorrowfastSceneRuntime.FindOwner(_input.CurrentZone,id);
        private MorrowfastScenePresenter Presenter()=>FindFirstObjectByType<MorrowfastScenePresenter>();
        private (int x,int y) Position()=>_input.CurrentZone.GetEntityPosition(_input.PlayerEntity);
        private string State()=>typeof(InputHandler).GetField("_inputState",Private).GetValue(_input).ToString();
        private List<InventoryAction> Actions()=>(List<InventoryAction>)typeof(WorldActionMenuUI).GetField("_actions",Private).GetValue(_input.WorldActionMenuUI);
        private IEnumerator WaitForScene()
        {for(int i=0;i<100;i++){if(ViewReady())yield break;yield return new WaitForSecondsRealtime(.1f);}throw new InvalidOperationException("Requested Morrowfast presentation not ready.");}
        private IEnumerator Capture(string label)
        {
            Require(Screen.width==1920&&Screen.height==1080,"Actual 1080p screenshot required.");Directory.CreateDirectory(Dir);
            string path=Path.Combine(Dir,Prefix+"-"+RunId+"-"+label+".png");Require(!File.Exists(path),"Fresh capture path required.");
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(path);
            double until=Now+8;while(!File.Exists(path)&&Now<until)yield return null;
            Require(File.Exists(path)&&new FileInfo(path).Length>1000,"Native GameView screenshot was not written.");_screenshots.Add(path);
        }
        private static Type FindType(string name)
        {foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies()){var t=assembly.GetType(name,false);if(t!=null)return t;}return null;}
        private void ConfigureMode()
        {
            var t=FindType("CavesOfOoo.Rendering.Village3DSettings");if(t==null){Require(_mode=="before","AFTER requires real Village3DSettings.");return;}
            _modeProperty=t.GetProperty("Enabled",BindingFlags.Static|BindingFlags.Public);_modeField=t.GetField("Enabled",BindingFlags.Static|BindingFlags.Public);
            Require(_modeProperty!=null||_modeField!=null,"Real mode switch contract.");_oldMode=(bool)(_modeProperty!=null?_modeProperty.GetValue(null):_modeField.GetValue(null));_hasMode=true;SetMode(_mode=="after");
            _oldLowDetail=Village3DSettings.LowDetail;Village3DSettings.LowDetail=false;
        }
        private void SetMode(bool enabled){if(_modeProperty!=null)_modeProperty.SetValue(null,enabled);else _modeField?.SetValue(null,enabled);}
        private static object ReadMember(object target,string name)
        {if(target==null)return null;var t=target.GetType();return t.GetProperty(name,AnyInstance)?.GetValue(target)??t.GetField(name,AnyInstance)?.GetValue(target);}
        private MonoBehaviour Village3D()=>FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(x=>x!=null&&x.GetType().FullName=="CavesOfOoo.Rendering.Village3DPresenter");
        private bool Village3DVisible()=>ReadMember(Village3D(),"PresentationVisible") is bool shown&&shown;
        private bool ViewReady()
        {
            if(_input==null||!MorrowfastSceneRuntime.IsActive(_input.CurrentZone))return false;
            if(_mode=="before")return Presenter()?.IsReady==true&&Presenter().PresentationVisible&&ReferenceEquals(Presenter().CurrentZone,_input.CurrentZone)&&!Village3DVisible();
            var p=Village3D();return ReadMember(p,"IsReady") is bool ready&&ready&&ReadMember(p,"PresentationVisible") is bool visible&&visible
                &&ReferenceEquals(ReadMember(p,"CurrentZone"),_input.CurrentZone)&&ReadMember(p,"WorldCamera") is Camera camera&&camera.enabled
                &&ReadMember(p,"RenderedOwnerCount") is int count&&count>0;
        }
        private IEnumerator ApproachEntity(string id)
        {
            for (int step = 0; step < 180; step++)
            {
                var entity = _input.CurrentZone.GetReadOnlyEntities().SingleOrDefault(e => e.ID == id);
                Require(entity != null && (!entity.HasPart<BrainPart>() || entity.GetStatValue("Hitpoints") > 0), "Living native target " + id);
                var cell = _input.CurrentZone.GetEntityCell(entity); var here = Position();
                Require(cell != null, "Native actor is in the active zone.");
                if (InteractionTargetCell(entity, here.x, here.y) != null) yield break;
                var targets = InteractionTargets(entity); var path = FindPath(targets, false);
                if (path == null) yield return AwaitRoute(targets, false, value => path = value);
                Require(path.Count > 0, "Current route to moving native actor " + id);
                // Observe its actual new cell after each action instead of chasing
                // an obsolete full path or relocating the actor for the audit.
                var next = path[0]; yield return Step(DirectionKey(next.x - here.x, next.y - here.y), (next.x - here.x, next.y - here.y));
            }
            throw new InvalidOperationException("Moving-fauna approach exceeded 180 native steps: " + id);
        }
        private IEnumerator OpenEntityMenu(Entity owner)
        {
            var p = Position(); var target = InteractionTargetCell(owner, p.x, p.y);
            Require(target != null && Math.Abs(target.X - p.x) + Math.Abs(target.Y - p.y) == 1, "Native menu target is cardinally adjacent.");
            yield return Tap(Key.C); yield return Tap(DirectionKey(target.X - p.x, target.Y - p.y));
            Require(State() == "WorldActionMenuOpen", "Actual entity action menu.");
            string pick = WorldInteractionSystem.PickTargetCommandPrefix + owner.ID;
            if (!Actions().Any(a => a.Command == pick) && Actions().Any(a => a.Command == WorldInteractionSystem.PickCellCommand))
                yield return SelectCommand(WorldInteractionSystem.PickCellCommand);
            if (Actions().Any(a => a.Command == pick)) yield return SelectCommand(pick);
            Require(State() == "WorldActionMenuOpen"
                && ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget, owner)
                && !_input.WorldActionMenuUI.SelectedCellIsPile,
                "Native selection resolved the exact individual owner, not its pile summary: " + owner.ID);
        }
        private Cell InteractionTargetCell(Entity owner, int x, int y)
        {
            foreach (var d in Directions)
            {
                var cell = _input.CurrentZone.GetCell(x + d.x, y + d.y);
                if (CellExposesOwner(cell, owner)) return cell;
            }
            return null;
        }
        private HashSet<(int, int)> InteractionTargets(Entity owner)
        {
            var at = _input.CurrentZone.GetEntityPosition(owner); var hits = new HashSet<(int x, int y)> { at };
            var spec = _definition?.owners.FirstOrDefault(o => o.id == owner.GetPart<MorrowfastPropPart>()?.ComponentId);
            if (spec != null && spec.kind != "npc" && spec.kind != "creature")
                foreach (var c in spec.footprint) hits.Add((c.x + at.x - spec.anchorX, c.y + at.y - spec.anchorY));
            var result = new HashSet<(int, int)>();
            foreach (var hit in hits)
            {
                var cell = _input.CurrentZone.GetCell(hit.x, hit.y);
                if (!CellExposesOwner(cell, owner)) continue;
                foreach (var d in Directions) result.Add((hit.x + d.x, hit.y + d.y));
            }
            return result;
        }
        private bool CellExposesOwner(Cell cell, Entity owner)
        {
            if (cell == null) return false;
            if (cell.Objects.Contains(owner)) return true;
            if (!ReferenceEquals(MorrowfastSceneRuntime.BlockingOwner(cell), owner)) return false;
            // A real object actually in this cell retains its own native menu.
            // Approach another exposed side of a larger overlapping footprint.
            return !cell.Objects.Any(e => e != null && !WorldInteractionSystem.IsTerrain(e));
        }
        private int InventoryUnits(string blueprint) => _input.PlayerEntity.GetPart<InventoryPart>().Objects.Where(e => e.BlueprintName == blueprint).Sum(e => e.GetPart<StackerPart>()?.StackCount ?? 1);
        private IEnumerator WalkTo((int x, int y) target, bool allowSeventh = false)
        {
            var path = FindPath(new HashSet<(int, int)> { target }, allowSeventh);
            if (path == null) yield return AwaitRoute(new HashSet<(int, int)> { target }, allowSeventh, value => path = value);
            yield return Follow(path, new HashSet<(int, int)> { target }, allowSeventh);
        }
        private List<(int x, int y)> PathToAdjacent(int x, int y)
        {
            return FindPath(AdjacentTargets(x, y), false);
        }
        private static HashSet<(int, int)> AdjacentTargets(int x, int y)
        {
            var targets = new HashSet<(int, int)>();
            foreach (var d in Directions) targets.Add((x + d.x, y + d.y));
            return targets;
        }
        private List<(int x, int y)> FindPath(HashSet<(int, int)> targets, bool allowSeventh, bool ignoreMobile = false)
        {
            var start = Position(); var queue = new Queue<(int, int)>(); queue.Enqueue(start);
            var previous = new Dictionary<(int, int), (int, int)> { [start] = start };
            var seventh = FellingSceneRuntime.IsActive(_input.CurrentZone) ? FellingSceneDefinition.Load().landmarks.Single(l => l.kind == "seventh") : null;
            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                if (targets.Contains(p))
                {
                    var result = new List<(int, int)>();
                    while (p != start) { result.Add(p); p = previous[p]; }
                    result.Reverse(); return result;
                }
                foreach (var d in Directions)
                {
                    var n = (p.Item1 + d.x, p.Item2 + d.y);
                    if (previous.ContainsKey(n) || !_input.CurrentZone.InBounds(n.Item1, n.Item2)) continue;
                    if (!allowSeventh && seventh != null && n == (seventh.x, seventh.y)) continue;
                    var cell = _input.CurrentZone.GetCell(n.Item1, n.Item2);
                    // IsPassable checks Solid tags only. Native creatures use
                    // PhysicsPart.Solid, so the actual movement gate is required.
                    if (cell.BlocksMovement(_input.PlayerEntity) && !(ignoreMobile && MobileOnlyBlocker(cell))) continue;
                    previous.Add(n, p); queue.Enqueue(n);
                }
            }
            return null;
        }
        private static bool MobileOnlyBlocker(Cell cell)
        {
            if (cell == null || !cell.BlocksMovement()) return false;
            if (MorrowfastSceneRuntime.BlockingOwner(cell) != null) return false;
            var blockers = cell.Objects.Where(e => e.HasTag("Solid") || e.GetPart<PhysicsPart>()?.Solid == true || e.GetPart<SealedLibraryBarrierPart>()?.IsClosed == true).ToArray();
            return blockers.Length > 0 && blockers.All(e => e.HasPart<BrainPart>());
        }
        private IEnumerator AwaitRoute(HashSet<(int, int)> targets, bool allowSeventh, Action<List<(int x, int y)>> receive)
        {
            for (int attempt = 0; attempt < 12; attempt++)
            {
                var path = FindPath(targets, allowSeventh);
                if (path != null) { receive(path); yield break; }
                Require(FindPath(targets, allowSeventh, true) != null, "No terrain route; blockage is not solely mobile fauna.");
                Require(_faunaWaits < 24, "Bounded native waiting for mobile fauna.");
                _faunaWaits++; yield return Tap(Key.Period);
            }
            throw new InvalidOperationException("Native fauna did not clear the route after twelve real wait actions.");
        }
        private IEnumerator Follow(List<(int x, int y)> path, HashSet<(int, int)> targets, bool allowSeventh = false)
        {
            int index = 0, replans = 0;
            while (index < path.Count)
            {
                var next = path[index];
                var cell = _input.CurrentZone.GetCell(next.x, next.y);
                // Only living mobile occupancy justifies replacing a route.
                // A rejected key, fixed obstacle or unexpected state fails loudly.
                if (MobileOnlyBlocker(cell))
                {
                    Require(++replans <= 24, "Bounded rerouting around mobile fauna.");
                    path = FindPath(targets, allowSeventh);
                    if (path == null) yield return AwaitRoute(targets, allowSeventh, value => path = value);
                    index = 0;
                    continue;
                }
                var p = Position(); int dx = next.x - p.x, dy = next.y - p.y;
                Require(Math.Abs(dx) + Math.Abs(dy) == 1, "Path stays cardinal and observes current player.");
                yield return Step(DirectionKey(dx, dy), (dx, dy));
                index++;
            }
        }
        private IEnumerator Step(Key key, (int x, int y) direction)
        {
            var before = Position();
            string beforeState = InputSnapshot(before.x + direction.x, before.y + direction.y);
            var targetCell = _input.CurrentZone.GetCell(before.x + direction.x, before.y + direction.y);
            Require(targetCell != null && !targetCell.BlocksMovement(_input.PlayerEntity),
                "Audit route refuses a known native movement blocker before sending " + key + ": " + beforeState);
            yield return Tap(key); _nativeSteps++;
            Require(Position() == (before.x + direction.x, before.y + direction.y),
                "Native movement failed after " + key + "; expected " + (before.x + direction.x, before.y + direction.y)
                + "; got " + Position() + "; before {" + beforeState + "}; after {"
                + InputSnapshot(before.x + direction.x, before.y + direction.y) + "}");
        }
        private string InputSnapshot(int targetX, int targetY)
        {
            var cell = _input.CurrentZone.GetCell(targetX, targetY);
            string blockers = cell == null ? "outside-zone" : string.Join(",", cell.Objects
                .Where(e => e.HasTag("Solid") || e.GetPart<PhysicsPart>()?.Solid == true || e.GetPart<SealedLibraryBarrierPart>()?.IsClosed == true)
                .Select(e => e.BlueprintName + ":" + e.ID));
            float lastMove = (float)typeof(InputHandler).GetField("_lastMoveTime", Private).GetValue(_input);
            return "state=" + State() + ";tick=" + _input.TurnManager.TickCount + ";energy=" + _input.TurnManager.GetEnergy(_input.PlayerEntity)
                + ";waiting=" + _input.TurnManager.WaitingForInput + ";hp=" + _input.PlayerEntity.GetStatValue("Hitpoints")
                + ";actor=" + _input.TurnManager.CurrentActor?.ID + ";frame=" + Time.frameCount
                + ";time=" + Time.time + ";lastMove=" + lastMove + ";repeatDelay=" + _input.MoveRepeatDelay
                + ";keyboard=" + Keyboard.current?.deviceId + ";ownedKeyboard=" + _keyboard?.deviceId
                + ";blockers=" + blockers + ";messages=" + string.Join(" / ", MessageLog.GetMessages().TakeLast(3));
        }
        private static Key DirectionKey(int x, int y) => x < 0 ? Key.A : x > 0 ? Key.D : y < 0 ? Key.W : Key.S;
        private IEnumerator SelectCommand(string command)
        {
            var rows = Actions(); int index = rows.FindIndex(a => a.Command == command);
            Require(index >= 0, "Native menu contains " + command);
            int cursor = (int)typeof(WorldActionMenuUI).GetField("_cursorIndex", Private).GetValue(_input.WorldActionMenuUI);
            for (; cursor < index; cursor++) yield return Tap(Key.DownArrow);
            for (; cursor > index; cursor--) yield return Tap(Key.UpArrow);
            yield return Tap(Key.Enter);
        }
        private IEnumerator Tap(params Key[] keys)
        {
            Require(_elapsed.Elapsed.TotalSeconds < 600, "Bounded ten-minute native audit.");
            double began = Time.realtimeSinceStartupAsDouble;
            // Respect the production scaled-time input gate before sending a
            // single pulse. This is readiness waiting, never an action retry.
            while (_input != null && Time.time - (float)typeof(InputHandler).GetField("_lastMoveTime", Private).GetValue(_input)
                < _input.MoveRepeatDelay)
            {
                Require(Time.realtimeSinceStartupAsDouble - began < 3, "Native input rate gate did not reopen.");
                yield return null;
            }
            _keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys));
            // Coroutine continuation runs after the next Update. Each edge
            // therefore survives one complete InputSystem/MonoBehaviour frame.
            yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return null;
            yield return new WaitForSecondsRealtime(.13f);
        }
        private bool OwnedCheckpoint()=>SaveGameService.SaveRootOverride==_root&&!string.IsNullOrEmpty(_gameId)&&SaveGameService.GetSaveInfo("Quick")?.GameID==_gameId
            &&File.Exists(Path.Combine(_root,_gameId,"Quick.sav.gz"))&&!Directory.Exists(Path.Combine(Application.persistentDataPath,"Saves",_gameId));
        private void Check(string name,bool pass){_audit.Add((pass?"PASS ":"FAIL ")+name);if(!pass)_failures++;}
        private static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        public void SetUnexpectedErrors(int value){_unexpected=value;if(_report!=null)WriteReport();}
        public void Abort(string reason){if(Finished)return;StopAllCoroutines();_fatal=reason;_failures++;_profileActive=false;DisposeRecorders();Finish();}
        private void Finish()
        {
            var metrics=new List<Metric>();Directory.CreateDirectory(Dir);
            if(_samples!=null)
            {
                for(int p=0;p<3;p++)for(int m=0;m<=_names.Length;m++)
                {
                    var values=new List<double>();for(int f=0;f<_frames;f++)if(_phases[f]==p)values.Add(m==_names.Length?_wallFrames[f]:_samples[m][f]);
                    values.Sort();bool available=m==_names.Length||_valid[m];
                    metrics.Add(new Metric{phase=_phaseReports[p].name,name=m==_names.Length?"Engine frame duration":_names[m],unit=m==_names.Length?"milliseconds":_units[m],available=available,count=values.Count,
                        mean=available&&values.Count>0?values.Average():-1,max=available&&values.Count>0?values[values.Count-1]:-1,
                        p95=available&&values.Count>0?values[(int)Math.Ceiling(.95*values.Count)-1]:-1,p99=available&&values.Count>0?values[(int)Math.Ceiling(.99*values.Count)-1]:-1});
                }
                var csv=new StringBuilder("frame,seconds,phase,engine_frame_ms,"+string.Join(",",_names)+"\n");
                for(int f=0;f<_frames;f++){csv.Append(f).Append(',').Append(F(_seconds[f])).Append(',').Append(_phaseReports[_phases[f]].name).Append(',').Append(F(_wallFrames[f]));for(int m=0;m<_names.Length;m++)csv.Append(',').Append(_samples[m][f]);csv.Append('\n');}
                File.WriteAllText(Path.Combine(Dir,Prefix+"-frames.csv"),csv.ToString());
            }
            _report=new Report{nativeCastEvents=_observedNativeCasts,nativeFxAtomsObserved=_observedNativeFx,dropPickup=_dropPickup,runId=RunId,mode=_mode,saveRoot=_root,gameId=_gameId,markerId=_markerId,initialZoneId=_initialZoneId,initialX=_initialX,initialY=_initialY,startingGarden=_startingGarden,fatal=_fatal,requestedWorldSeed=_requestedWorldSeed,worldSeed=_input?.WorldMap?.Seed??0,frames=_frames,nativeSteps=_nativeSteps,faunaWaits=_faunaWaits,commands=_menuCommands,
                wallSeconds=Now,measuredSeconds=_phaseReports.Sum(p=>p.seconds),zoneEnterToReadySeconds=_zoneEnterSeconds,workloadComplete=_completed&&!_overflow&&Failures==0,overflow=_overflow,
                screenWidth=Screen.width,screenHeight=Screen.height,targetFrameRate=Application.targetFrameRate,vSyncCount=QualitySettings.vSyncCount,unityVersion=Application.unityVersion,
                os=SystemInfo.operatingSystem,cpu=SystemInfo.processorType,gpu=SystemInfo.graphicsDeviceName,graphicsApi=SystemInfo.graphicsDeviceType.ToString(),phases=_phaseReports,metrics=metrics.ToArray(),screenshots=_screenshots.ToArray(),
                bounds="Actual native New Game and observed initial-zone route: an initial Morrowfast walks north into Felling; other starts use world-map travel to Felling. Both enter the measured village through its north boundary with retained ordinary actors. Three wall-duration phases complete whole walk/door cycles, so 25s is a minimum and counts can differ. No teleports, dummy owners, AI suppression or fog reveal. All frames retained, no rolling-window truncation. Frame markers include editor, 2D UI, runtime and observer; wall delta is not CPU. Optional GPU/render counters may be unavailable or zero on this platform; zero is not proof of no work. Zone-enter timing includes key release/waits and presentation readiness, not pure loading CPU. Screenshots are actual GameView including UI, not a manually composited camera. AFTER verifies actual new-game Morrowfast (40,23), six exact indexed garden terrain cells and a starting GameView before northward input; these acceptance checks are outside profiling. AFTER also adds owner aliases, actual room/roof and dagger views, F6 reconstruction, F11 controls, empty-container clear and ordinary starter Jet Blast/cancel probes after profiling. Spell FX are temporarily Full at speed 1 for that probe only; captured callback/queue state does not prove pixel visibility or framing. Dedicated GPU tests and human GameView inspection remain required for masking, projection, draw-budget compliance and visual quality. Bootstrap route changes limit exact paired prehistory. No 60fps, speedup or parity claim follows from a single pair."};
            WriteReport();Finished=true;Debug.Log("[Village3DNativeAudit] "+JsonUtility.ToJson(_report));
        }
        private static string F(double x)=>x.ToString("F6",CultureInfo.InvariantCulture);
        private void WriteReport(){_report.failures=Failures;_report.unexpectedErrors=_unexpected;_report.cases=_audit.Count;_report.audit=_audit.ToArray();File.WriteAllText(Path.Combine(Dir,Prefix+"-native.json"),JsonUtility.ToJson(_report,true));}
        private void CleanupInput()
        {if(!_initialized||_cleaned)return;_cleaned=true;RestoreProbePreferencesAndObserver();if(_keyboard!=null&&_keyboard.added)InputSystem.RemoveDevice(_keyboard);if(_oldSettings!=null)InputSystem.settings=_oldSettings;if(_settings!=null)Destroy(_settings);Application.runInBackground=_oldBackground;if(_hasMode){SetMode(_oldMode);Village3DSettings.LowDetail=_oldLowDetail;}}
        private void OnDestroy()
        {
            if(_initialized&&!Finished){_fatal="Play ended before audit completion.";_failures++;_profileActive=false;DisposeRecorders();Finish();}
            try{if(_report!=null){const BindingFlags flags=BindingFlags.Static|BindingFlags.NonPublic;_report.shutdownObserved=true;_report.shutdownSeconds=Now;_report.shutdownRootHeld=SaveGameService.SaveRootOverride==_root;
                _report.shutdownSavingUnregistered=typeof(SaveGameService).GetField("_captureCurrent",flags).GetValue(null)==null&&typeof(SaveGameService).GetField("_applyLoaded",flags).GetValue(null)==null;
                if(!_report.shutdownRootHeld||!_report.shutdownSavingUnregistered)_failures++;WriteReport();}}
            finally{DisposeRecorders();CleanupInput();_elapsed?.Stop();}
        }
        [Serializable] private sealed class Phase{public string name;public int frames,startTick,endTick,startSteps,startCommands,nativeSteps,commands,cycles,startX,startY,endX,endY;public double startSeconds,seconds;}
        [Serializable] private sealed class Metric{public string phase,name,unit;public bool available;public int count;public double mean,max,p95,p99;}
        [Serializable] private sealed class DropPickupReceipt { public ItemOwnership before, afterDrop, afterPickup; }
        [Serializable] private sealed class ItemOwnership
        {
            public string inputState, zoneId, itemId, blueprint, carriedUnits, equippedUnits, ownedUnits, inInventoryId, equippedById, groundCell, playerCell;
            public int units, carriedReferences, equippedSlotReferences;
            public string[] carriedEntities, equippedSlots, playerCellEntities;
        }
        [Serializable] private sealed class Report
        {public int initialX,initialY;public string[] startingGarden;public int nativeCastEvents,nativeFxAtomsObserved;public bool displayPreferencesRestored;public DropPickupReceipt dropPickup;public string runId,mode,saveRoot,gameId,markerId,initialZoneId,fatal,bounds,unityVersion,os,cpu,gpu,graphicsApi;public int requestedWorldSeed,worldSeed,failures,unexpectedErrors,cases,frames,nativeSteps,faunaWaits,commands,screenWidth,screenHeight,targetFrameRate,vSyncCount;public bool workloadComplete,overflow,shutdownObserved,shutdownRootHeld,shutdownSavingUnregistered;public double wallSeconds,measuredSeconds,zoneEnterToReadySeconds,shutdownSeconds;public Phase[] phases;public Metric[] metrics;public string[] screenshots,audit;}
    }
}
