using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Native acceptance with an optional separately reported profile. Native N/walk/UI/F5/F6
    /// input is separate from explicitly labelled fixture positioning and the
    /// 24 real transition-API calls. Never disables AI or reconstructs saved water.</summary>
    public sealed partial class SpawnRing3DNativeAudit : MonoBehaviour
    {
        public bool Finished { get; private set; }
        public int Failures => failures + unexpected;
        public string RunId { get; private set; }
        const int Seed = 729490642;
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static readonly string[] PreferenceKeys = { Village3DSettings.PreferenceKey, Village3DSettings.LowDetailPreferenceKey };
        readonly List<CheckRow> checks = new List<CheckRow>();
        readonly List<BorderRow> borders = new List<BorderRow>();
        readonly List<ZoneRow> zones = new List<ZoneRow>();
        readonly List<string> screenshots = new List<string>();
        readonly HashSet<string> directedBorders = new HashSet<string>(StringComparer.Ordinal);
        InputHandler input;
        Keyboard keyboard, oldKeyboard;
        InputSettings originalSettings, ownedSettings;
        bool originalBackground, originalEnabled, originalLow, initialized, cleaned, completed;
        readonly bool[] prefPresent = new bool[2];
        readonly int[] prefValue = new int[2];
        int failures, unexpected, nativeSteps;
        string saveRoot, markerId, gameId, fatal;
        byte[] markerBytes;
        System.Diagnostics.Stopwatch watch;
        Report report;
        Stack<IEnumerator> activeSteps;
        string Dir => Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/Verification/SpawnRing3D"));
        string Stem => "R3D-" + RunId;
        public string ReportPath => Path.Combine(Dir, Stem + "-native.json");

        public void Initialize(string runId)
        {
            Require(Guid.TryParseExact(runId, "N", out var guid) && guid != Guid.Empty, "Explicit unique run ID required.");
            Require(!string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride), "NativeSaveIsolation must own the save root before bootstrap.");
            RunId = runId; saveRoot = SaveGameService.SaveRootOverride;
            markerId = PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey);
            markerBytes = File.ReadAllBytes(Path.Combine(saveRoot, markerId, "Quick.sav.gz"));
            Directory.CreateDirectory(Dir); watch = System.Diagnostics.Stopwatch.StartNew();
            originalEnabled = Village3DSettings.Enabled; originalLow = Village3DSettings.LowDetail;
            for (int i = 0; i < PreferenceKeys.Length; i++) { prefPresent[i] = PlayerPrefs.HasKey(PreferenceKeys[i]); prefValue[i] = PlayerPrefs.GetInt(PreferenceKeys[i]); }
            originalBackground = Application.runInBackground; originalSettings = InputSystem.settings; oldKeyboard = Keyboard.current;
            ownedSettings = Instantiate(originalSettings); ownedSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            ownedSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = ownedSettings; Application.runInBackground = true;
            keyboard = InputSystem.AddDevice<Keyboard>(); initialized = true;
            StartCoroutine(RunSafely(RunAudit()));
        }

        IEnumerator RunSafely(IEnumerator steps)
        {
            activeSteps = new Stack<IEnumerator>(); activeSteps.Push(steps);
            try
            {
                while (activeSteps.Count > 0)
                {
                    bool moved = false; object current = null; Exception error = null;
                    try { moved = activeSteps.Peek().MoveNext(); if (moved) current = activeSteps.Peek().Current; }
                    catch (Exception caught) { error = caught; }
                    if (error != null) { fatal = error.ToString(); Check("fatal", false, fatal); break; }
                    if (!moved) { DisposeStep(activeSteps.Pop()); continue; }
                    if (current is IEnumerator nested) { activeSteps.Push(nested); continue; }
                    yield return current;
                }
            }
            finally { DisposePendingSteps(); }
            Finish();
        }
        void DisposeStep(IEnumerator step)
        {
            try { (step as IDisposable)?.Dispose(); }
            catch (Exception error) { Check("iterator_cleanup",false,error.ToString()); }
        }
        void DisposePendingSteps()
        {
            // Unity owns only RunSafely, not the manually advanced nested iterators.
            // Unwind inside-out even when one cleanup fails; Abort and OnDestroy are
            // explicit backups for coroutine interruption on either lifecycle path.
            while (activeSteps != null && activeSteps.Count > 0) DisposeStep(activeSteps.Pop());
        }

        IEnumerator RunAudit()
        {
            yield return new WaitForSecondsRealtime(.8f);
            input = FindFirstObjectByType<InputHandler>(); Require(input != null, "Ordinary input exists.");
            Require(Screen.width == 1920 && Screen.height == 1080, "Actual native GameView must be 1920x1080.");
            var boot = (BootMenuController)typeof(InputHandler).GetField("_bootMenuController", Private).GetValue(input);
            Require(boot != null && boot.IsActive, "Owned boot menu is active.");
            yield return Tap(Key.N);
            Check("native_N_real_new_game", !boot.IsActive && State() == "Normal" && input.WorldMap.Seed == Seed
                && input.CurrentZone.ZoneID == Id(3,6) && Position() == (40,23));
            Require(checks.Last().pass, "Native N must start the real Morrowfast entrance at the requested seed.");
            // Explicit presentation setup only; ordinary F11 controls are exercised later.
            Village3DSettings.Enabled = true; Village3DSettings.LowDetail = false;
            int startTick = input.TurnManager.TickCount;
            yield return NativeStep(Key.W, (0,-1)); yield return NativeStep(Key.S, (0,1));
            Check("two_ordinary_road_steps_with_live_turns", nativeSteps == 2 && input.TurnManager.TickCount > startTick && Alive());
            yield return Capture("center-new-game");

            // Generate through the actual current manager; never replace native cells.
            for (int y = 5; y <= 7; y++) for (int x = 2; x <= 4; x++)
                Require(input.ZoneManager.GetZone(Id(x,y))?.ZoneID == Id(x,y), "Exact native lattice coordinate " + Id(x,y));
            Require(PlayerReferences() == 1, "Exactly one current player reference across the real cache.");

            // Optional sustained rendering/input profile before any ring border binding.
            // The existing acceptance workload remains present and runs afterwards.
            yield return RunRingProfileIfRequested();

            // 12 shared lattice borders, each in both directions. Fixture relocation
            // is not counted as a transition and does not pretend to consume a turn.
            for (int y = 5; y <= 7; y++) for (int x = 2; x <= 4; x++)
            {
                if (x < 4) { yield return Border(Id(x,y), Id(x+1,y), TransitionDirection.East); yield return Border(Id(x+1,y), Id(x,y), TransitionDirection.West); }
                if (y < 7) { yield return Border(Id(x,y), Id(x,y+1), TransitionDirection.South); yield return Border(Id(x,y+1), Id(x,y), TransitionDirection.North); }
            }
            Check("all_24_distinct_directed_borders", borders.Count == 24 && directedBorders.Count == 24 && borders.All(b => b.pass));

            for (int y = 5; y <= 7; y++) for (int x = 2; x <= 4; x++)
            {
                if (x == 3 && y == 6) continue;
                Zone zone = input.ZoneManager.GetZone(Id(x,y)); FixturePlace(zone, NearestFree(zone,40,12));
                yield return WaitForRing();
                var p = Presenter(); var player = input.PlayerEntity;
                bool view = p.TryGetEntityView(player, out var root, out var model) && root != null && root.activeInHierarchy && p.IsRenderedEntity(player);
                var at = Position(); Physics.SyncTransforms();
                bool picked = TryPickExactEntity(p,player,root,out var selectedPoint);
                Check("current_zone_binding:" + zone.ZoneID, ReferenceEquals(zone,p.CurrentZone) && ReferenceEquals(zone,input.ZoneRenderer.CurrentZone) && view && !p.FullReveal,
                    "player=" + player.ID + ";model=" + model + ";FOV remains native");
                Check("exact_player_API_pick:" + zone.ZoneID, picked, "Selection API over actual posed renderer bounds; not mouse dispatch. point="+selectedPoint);
                var row = new ZoneRow { zoneId = zone.ZoneID, playerId = player.ID, modelId = model, x = at.x, y = at.y,
                    entityCount = zone.GetReadOnlyEntities().Count, creatureCount = zone.GetEntitiesWithTag("Creature").Count,
                    pickWorldX=selectedPoint.x,pickWorldY=selectedPoint.y,groundPatches = p.GroundPatchCount, tileHash = Hash(TileSnapshot(zone)), playerVisible = zone.GetCell(at.x,at.y).IsVisible,
                    presentationReady = p.IsReady, presentationVisible = p.PresentationVisible, fullReveal = p.FullReveal };
                zones.Add(row); yield return Capture("zone-" + zone.ZoneID);
            }
            Check("all_eight_actual_current_zone_captures", zones.Count == 8 && zones.Select(z => z.zoneId).Distinct().Count() == 8 && zones.All(z => z.presentationVisible && z.playerVisible));
            yield return AuditFenSave();
            yield return AuditUiAndFallback();
            yield return RunRingActionsIfRequested();
            Check("private_boot_marker_unchanged", File.ReadAllBytes(Path.Combine(saveRoot,markerId,"Quick.sav.gz")).SequenceEqual(markerBytes));
            Check("owned_save_only", OwnedCheckpoint());
            completed = true;
        }

        IEnumerator Border(string from, string to, TransitionDirection direction)
        {
            Zone source = input.ZoneManager.GetZone(from); var edge = EdgeCell(source,direction); FixturePlace(source,edge);
            int tick = input.TurnManager.TickCount; var player = input.PlayerEntity;
            var result = ZoneTransitionSystem.TransitionPlayer(player,source,direction,edge.x,edge.y,input.ZoneManager,input.WorldMap);
            var row = new BorderRow { from=from,to=to,direction=direction.ToString(),sourceX=edge.x,sourceY=edge.y,
                tickBefore=tick,arrivalX=result.NewPlayerX,arrivalY=result.NewPlayerY,error=result.ErrorReason };
            borders.Add(row);
            Require(result.Success && result.NewZone?.ZoneID == to, "Real border API failed: " + from + " " + direction + " => " + result.ErrorReason);
            WireTransition(result);
            yield return new WaitForSecondsRealtime(.35f);
            Cell arrival = result.NewZone.GetEntityCell(player);
            row.tickAfter = input.TurnManager.TickCount;
            row.pass = arrival != null && (arrival.X,arrival.Y) == (result.NewPlayerX,result.NewPlayerY) && arrival.IsPassable()
                && !arrival.Objects.Any(e => e.HasTag("ExcludeZoneArrival")) && source.GetEntityCell(player) == null
                && ReferenceEquals(input.CurrentZone,result.NewZone) && ReferenceEquals(input.ZoneManager.ActiveZone,result.NewZone)
                && ReferenceEquals(input.ZoneRenderer.CurrentZone,result.NewZone) && PlayerReferences() == 1 && CurrentBindingIntact()
                && row.tickAfter == tick && Alive();
            Require(directedBorders.Add(from+">"+to), "No repeated border substitutes for a missing direction.");
            Check("transition_API:"+from+">"+to,row.pass,"Fixture-positioned source; real transition, ordinary binding/autosave, no player turn.");
        }

        IEnumerator AuditFenSave()
        {
            Zone fen = input.ZoneManager.GetZone(Id(3,7));
            var candidates = new List<(int x,int y)>();
            for (int y=0;y<Zone.Height;y++) for (int x=0;x<Zone.Width;x++)
            {
                Cell cell=fen.GetCell(x,y);
                if (fen.TileState.CoatingTurns(x,y,"water") == ZoneTileState.Permanent && !cell.Objects.Any(e=>e.HasPart<LiquidPoolPart>()) && !cell.BlocksMovement(input.PlayerEntity)) candidates.Add((x,y));
            }
            Require(candidates.Count>=2,"Real fen has two walkable permanent-water cells with no pool entity; no fabricated water precondition.");
            var erased=candidates[0]; var retained=candidates[1]; FixturePlace(fen,erased); yield return WaitForRing();
            Check("real_fen_water_visible_before_erasure",Presenter().HasRepresentedWater(erased.x,erased.y));
            Require(fen.TileState.RemoveCoating(erased.x,erased.y,"water"),"Explicit fixture erasure removes existing native water.");
            yield return null; yield return null;
            Check("native_erasure_removes_represented_water",!Presenter().HasRepresentedWater(erased.x,erased.y));
            Require(ReadyForPlayerInput(),"F5 checkpoint must be at a live normal player input boundary, not a pending NPC turn.");
            int savedTick=input.TurnManager.TickCount,savedEnergy=input.TurnManager.GetEnergy(input.PlayerEntity);
            var beforeCache=CacheSnapshot(); string expectedWater=WaterSnapshot(fen);
            var owners=KnownOwners(); Require(owners.Count>0,"Cached authored owner references are nonvacuous.");
            var oldPlayer=input.PlayerEntity; var oldZone=fen; var savedPosition=Position(); string playerId=oldPlayer.ID;
            yield return Tap(Key.F5); gameId=SaveGameService.GetSaveInfo("Quick")?.GameID;
            Require(OwnedCheckpoint(),"Native F5 writes the owned game checkpoint.");
            Check("native_F5_preserves_player_turn_boundary",ReadyForPlayerInput()&&input.TurnManager.TickCount==savedTick&&input.TurnManager.GetEnergy(oldPlayer)==savedEnergy);
            File.WriteAllText(Path.Combine(Dir,Stem+"-saved-owners.json"),JsonUtility.ToJson(new OwnerLedger { owners=owners.OrderBy(p=>p.Key,StringComparer.Ordinal).Select(p=>p.Value).ToArray() },true));
            File.WriteAllText(Path.Combine(Dir,Stem+"-saved-tiles.json"),JsonUtility.ToJson(new TileLedger { zones=beforeCache },true));
            fen.TileState.WriteCoating(erased.x,erased.y,"water",ZoneTileState.Permanent);
            Require(fen.TileState.RemoveCoating(retained.x,retained.y,"water"),"Postcheckpoint negative control changes another existing water cell.");
            Check("preload_mutation_countercheck",WaterSnapshot(fen)!=expectedWater);
            yield return Tap(Key.F6); yield return WaitForRing();
            bool aliases=!ReferenceEquals(oldPlayer,input.PlayerEntity)&&!ReferenceEquals(oldZone,input.CurrentZone)
                && input.PlayerEntity.ID==playerId && input.CurrentZone.ZoneID==Id(3,7) && Position()==savedPosition
                && ReferenceEquals(input.ZoneManager.ActiveZone,input.CurrentZone) && ReferenceEquals(input.ZoneRenderer.CurrentZone,input.CurrentZone)
                && input.TurnManager.Entities.Any(e=>ReferenceEquals(e,input.PlayerEntity)) && PlayerReferences()==1 && CurrentBindingIntact()
                && ReadyForPlayerInput() && input.TurnManager.TickCount==savedTick && input.TurnManager.GetEnergy(input.PlayerEntity)==savedEnergy;
            Check("native_F6_rebuilds_player_zone_turn_aliases",aliases);
            string loadedWater=WaterSnapshot(input.CurrentZone);
            Check("native_F6_exact_fen_water_and_saved_erasure",loadedWater==expectedWater
                && !input.CurrentZone.TileState.HasCoating(erased.x,erased.y,"water")
                && input.CurrentZone.TileState.CoatingTurns(retained.x,retained.y,"water")==ZoneTileState.Permanent
                && !Presenter().HasRepresentedWater(erased.x,erased.y),
                "erased="+erased+";retained="+retained+";expectedWaterSha256="+Hash(expectedWater)+";loadedWaterSha256="+Hash(loadedWater));
            var loadedCache=CacheSnapshot();
            File.WriteAllText(Path.Combine(Dir,Stem+"-loaded-tiles.json"),JsonUtility.ToJson(new TileLedger { zones=loadedCache },true));
            Check("native_F6_all_cached_tile_snapshots",SameCache(beforeCache,loadedCache),
                "Compare this run's saved-tiles.json and loaded-tiles.json; includes empty cached zones and exact ordered layers.");
            var loadedOwners=KnownOwners();
            File.WriteAllText(Path.Combine(Dir,Stem+"-loaded-owners.json"),JsonUtility.ToJson(new OwnerLedger { owners=loadedOwners.OrderBy(p=>p.Key,StringComparer.Ordinal).Select(p=>p.Value).ToArray() },true));
            Check("native_F6_cached_owner_reference_replacement",loadedOwners.Count==owners.Count && owners.All(pair=>loadedOwners.TryGetValue(pair.Key,out var e)
                && !ReferenceEquals(e.reference,pair.Value.reference) && e.id==pair.Value.id && e.blueprint==pair.Value.blueprint
                && e.componentId==pair.Value.componentId && e.family==pair.Value.family && e.x==pair.Value.x && e.y==pair.Value.y
                && pair.Value.runtimeResolvesExact && e.runtimeResolvesExact),"Immutable component/position snapshots and runtime owner lookup; saved/loaded owner ledgers accompany tile ledgers.");
            Check("presenter_rejects_old_player_reference",!Presenter().TryGetEntityView(oldPlayer,out _,out _)
                && Presenter().TryGetEntityView(input.PlayerEntity,out var currentRoot,out _) && currentRoot!=null);
            yield return Capture("fen-F6-saved-erasure");
        }

        IEnumerator AuditUiAndFallback()
        {
            int tick=input.TurnManager.TickCount,energy=input.TurnManager.GetEnergy(input.PlayerEntity); var at=Position();
            yield return Tap(Key.L); Check("native_L_look_menu",State()=="LookMode"); yield return Capture("look-ui"); yield return Tap(Key.Escape);
            yield return Tap(Key.I); Check("native_I_inventory",State()=="InventoryOpen"); yield return Capture("inventory-ui"); yield return Tap(Key.Escape);
            yield return Tap(Key.F11); yield return null;
            Check("native_F11_original_fallback_mode",!Village3DSettings.Enabled&&!Presenter().PresentationVisible&&ReferenceEquals(input.ZoneRenderer.CurrentZone,input.CurrentZone));
            yield return Capture("original-view"); yield return Tap(Key.F11); yield return WaitForRing();
            int fullWidth=Presenter().WorldCamera.targetTexture.width,fullHeight=Presenter().WorldCamera.targetTexture.height;
            yield return Tap(Key.LeftShift,Key.F11); yield return null; yield return null;
            var reduced=Presenter().WorldCamera.targetTexture;
            Check("native_low_detail_target",Village3DSettings.LowDetail&&reduced!=null&&reduced.IsCreated()&&reduced.width<=fullWidth&&reduced.height<=fullHeight&&(reduced.width<fullWidth||reduced.height<fullHeight));
            yield return Capture("low-detail"); yield return Tap(Key.LeftShift,Key.F11); yield return WaitForRing();
            Check("native_UI_display_controls_no_turn",State()=="Normal"&&input.TurnManager.TickCount==tick&&input.TurnManager.GetEnergy(input.PlayerEntity)==energy&&Position()==at&&!Village3DSettings.LowDetail);

            // Real factory item as an explicitly labelled temporary ownership probe.
            // Automatic checks prove fallback eligibility/native retention only;
            // the screenshot is for human fallback-pixel inspection.
            Entity dagger=input.ZoneManager.Factory.CreateEntity("Dagger"); Require(dagger!=null,"Existing actual Dagger blueprint.");
            var fallbackCell=FallbackCell(); var fallbackZone=input.CurrentZone;
            Require(fallbackZone.AddEntity(dagger,fallbackCell.x,fallbackCell.y),"Place labelled temporary fallback item on a nearby visible unoccupied floor.");
            try
            {
                ZoneRenderHooks.MarkCellDirty(fallbackCell.x,fallbackCell.y,"SpawnRingNative.FallbackFixture"); yield return null; yield return null;
                Check("unmapped_factory_item_keeps_native_fallback_eligibility",ReferenceEquals(input.CurrentZone.GetEntityCell(dagger),input.CurrentZone.GetCell(fallbackCell.x,fallbackCell.y))
                    && !Presenter().IsAuthoredEntity(dagger)&&!Presenter().IsRenderedEntity(dagger),"cell="+fallbackCell+";no player/creature/prop overlap. Not an automatic proof of sprite pixels; inspect the screenshot.");
                yield return Capture("unmapped-dagger-fallback-fixture");
            }
            finally
            {
                // Keep cleanup tied to the captured owner even if a failing callback
                // replaces the active zone while the screenshot is suspended.
                bool removed=fallbackZone.RemoveEntity(dagger);
                Check("fallback_fixture_removed",removed&&fallbackZone.GetEntityCell(dagger)==null);
                if(ReferenceEquals(input.CurrentZone,fallbackZone))ZoneRenderHooks.MarkCellDirty(fallbackCell.x,fallbackCell.y,"SpawnRingNative.FallbackCleanup");
            }
        }

        bool TryPickExactEntity(SpawnRing3DPresenter presenter,Entity entity,GameObject root,out Vector2 selected)
        {
            selected=default;if(root==null)return false;
            var renderers=root.GetComponentsInChildren<Renderer>();if(renderers.Length==0)return false;
            Bounds bounds=renderers[0].bounds;for(int i=1;i<renderers.Length;i++)bounds.Encapsulate(renderers[i].bounds);
            var cell=input.CurrentZone.GetEntityCell(entity);if(cell==null)return false;
            // The imported posed silhouette need not intersect the logical cell centre.
            // Search a finite grid of actual world bounds, accepting only exact identity.
            for(int z=0;z<9;z++)for(int x=0;x<9;x++)
            {
                var point=new Vector2(Mathf.Lerp(bounds.min.x,bounds.max.x,(x+.5f)/9f),Mathf.Lerp(bounds.min.z,bounds.max.z,(z+.5f)/9f));
                if(presenter.TryPickWorld(point,out var picked,out int px,out int py)&&ReferenceEquals(picked,entity)&&(px,py)==(cell.X,cell.Y))
                {selected=point;return true;}
            }
            return false;
        }
        (int x,int y) FallbackCell()
        {
            var at=Position();
            for(int distance=1;distance<=8;distance++)
                for(int y=Math.Max(0,at.y-distance);y<=Math.Min(Zone.Height-1,at.y+distance);y++)
                    for(int x=Math.Max(0,at.x-distance);x<=Math.Min(Zone.Width-1,at.x+distance);x++)
                    {
                        if(Math.Abs(x-at.x)+Math.Abs(y-at.y)!=distance)continue;
                        Cell cell=input.CurrentZone.GetCell(x,y);
                        if(cell.IsVisible&&!cell.BlocksMovement(input.PlayerEntity)&&cell.Objects.All(WorldInteractionSystem.IsTerrain))return(x,y);
                    }
            throw new InvalidOperationException("No visible unoccupied native floor within eight cells for an honest fallback screenshot.");
        }
        void FixturePlace(Zone target,(int x,int y) at)
        {
            Require(State()=="Normal"&&Alive(),"Fixture positioning begins with a live normal player.");
            Require(!target.GetCell(at.x,at.y).BlocksMovement(input.PlayerEntity),"Fixture source is currently unblocked.");
            Zone previous=input.CurrentZone; Require(previous.RemoveEntity(input.PlayerEntity),"Remove exact fixture player from its previous zone.");
            Require(target.AddEntity(input.PlayerEntity,at.x,at.y),"Place exact existing player; no clone or new world.");
            WireTransition(new ZoneTransitionResult { Success=true,NewZone=target,NewPlayerX=at.x,NewPlayerY=at.y });
            Require(PlayerReferences()==1,"Fixture positioning preserves exact single membership.");
        }
        void WireTransition(ZoneTransitionResult result)
        {
            var method=typeof(InputHandler).GetMethod("HandleZoneTransition",Private);
            Require(method!=null,"Verified ordinary transition binder is still present."); method.Invoke(input,new object[]{result});
        }
        (int x,int y) EdgeCell(Zone zone,TransitionDirection direction)
        {
            var cells=new List<(int x,int y)>();
            for(int i=0;i<(direction==TransitionDirection.East||direction==TransitionDirection.West?Zone.Height:Zone.Width);i++)
                cells.Add(direction==TransitionDirection.East?(Zone.Width-1,i):direction==TransitionDirection.West?(0,i):direction==TransitionDirection.North?(i,0):(i,Zone.Height-1));
            foreach(var p in cells.OrderBy(p=>Math.Abs(p.x-40)+Math.Abs(p.y-12)))
                if(!zone.GetCell(p.x,p.y).BlocksMovement(input.PlayerEntity))return p;
            throw new InvalidOperationException("No unblocked native source edge for "+zone.ZoneID+" "+direction);
        }
        (int x,int y) NearestFree(Zone zone,int x,int y)
        {
            for(int distance=0;distance<Zone.Width+Zone.Height;distance++)
                for(int cy=0;cy<Zone.Height;cy++)for(int cx=0;cx<Zone.Width;cx++)
                    if(Math.Abs(cx-x)+Math.Abs(cy-y)==distance&&!zone.GetCell(cx,cy).BlocksMovement(input.PlayerEntity))return(cx,cy);
            throw new InvalidOperationException("No native free cell in "+zone.ZoneID);
        }
        SpawnRing3DPresenter Presenter()=>input?.ZoneRenderer!=null?input.ZoneRenderer.SpawnRing3D:null;
        bool ReadyForPlayerInput()=>Alive()&&State()=="Normal"&&input.TurnManager.WaitingForInput&&ReferenceEquals(input.TurnManager.CurrentActor,input.PlayerEntity);
        bool CurrentBindingIntact()
        {
            var current=input.CurrentZone;
            if(!ReferenceEquals(TurnManager.Active,input.TurnManager)||!ReferenceEquals(input.ZoneRenderer.PlayerEntity,input.PlayerEntity))return false;
            foreach(var e in input.TurnManager.Entities)
                if(current.GetEntityCell(e)==null)return false;
            foreach(var e in current.GetEntitiesWithTag("Creature"))
            {
                if(!input.TurnManager.IsRegistered(e))return false;
                var brain=e.GetPart<BrainPart>();
                if(!ReferenceEquals(e,input.PlayerEntity)&&brain!=null&&!ReferenceEquals(brain.CurrentZone,current))return false;
            }
            return ReadyForPlayerInput();
        }
        IEnumerator WaitForRing()
        {
            double start=Time.realtimeSinceStartupAsDouble;
            while(true)
            {
                var p=Presenter();
                if(p!=null&&p.IsReady&&p.PresentationVisible&&ReferenceEquals(p.CurrentZone,input.CurrentZone)&&p.WorldCamera!=null
                    &&p.WorldCamera.targetTexture!=null&&p.WorldCamera.targetTexture.IsCreated()&&input.CurrentZone.GetCell(Position().x,Position().y).IsVisible)break;
                Require(Time.realtimeSinceStartupAsDouble-start<12,"Actual ring presenter readiness timeout: "+p?.Failure); yield return null;
            }
            yield return new WaitForSecondsRealtime(.4f);
        }
        IEnumerator NativeStep(Key key,(int x,int y) delta)
        {
            var at=Position(); Require(!input.CurrentZone.GetCell(at.x+delta.x,at.y+delta.y).BlocksMovement(input.PlayerEntity),"Native road step preflight.");
            yield return Tap(key); nativeSteps++; Require(Position()==(at.x+delta.x,at.y+delta.y)&&Alive(),"Exact ordinary movement result.");
        }
        IEnumerator Tap(params Key[] keys)
        {
            Require(watch.Elapsed.TotalSeconds<840,"Bounded native acceptance workload."); double start=Time.realtimeSinceStartupAsDouble;
            while(input!=null&&Time.time-(float)typeof(InputHandler).GetField("_lastMoveTime",Private).GetValue(input)<input.MoveRepeatDelay)
            {Require(Time.realtimeSinceStartupAsDouble-start<3,"Input rate gate did not reopen.");yield return null;}
            keyboard.MakeCurrent(); InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return new WaitForSecondsRealtime(.13f);
        }
        IEnumerator Capture(string label)
        {
            string file=Path.Combine(Dir,Stem+"-"+label+".png"); Require(!File.Exists(file),"Unique screenshot path, never stale proof.");
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(file);double start=Time.realtimeSinceStartupAsDouble;
            while(!File.Exists(file)||new FileInfo(file).Length<1000){Require(Time.realtimeSinceStartupAsDouble-start<8,"GameView screenshot not written.");yield return null;}
            screenshots.Add(file);
        }
        bool Alive()=>input.PlayerEntity.GetStatValue("Hitpoints")>0&&!CombatSystem.IsDeathHandled(input.PlayerEntity);
        string State()=>typeof(InputHandler).GetField("_inputState",Private).GetValue(input).ToString();
        (int x,int y) Position()=>input.CurrentZone.GetEntityPosition(input.PlayerEntity);
        static string Id(int x,int y)=>"Overworld."+x+"."+y+".0";
        int PlayerReferences()=>input.ZoneManager.CachedZones.Values.Sum(z=>z.GetReadOnlyEntities().Count(e=>ReferenceEquals(e,input.PlayerEntity)));
        bool OwnedCheckpoint()=>SaveGameService.SaveRootOverride==saveRoot&&!string.IsNullOrEmpty(gameId)&&gameId!=markerId
            &&SaveGameService.GetSaveInfo("Quick")?.GameID==gameId&&File.Exists(Path.Combine(saveRoot,gameId,"Quick.sav.gz"))
            &&!Directory.Exists(Path.Combine(Application.persistentDataPath,"Saves",gameId));
        Dictionary<string,OwnerRow> KnownOwners()
        {
            var result=new Dictionary<string,OwnerRow>(StringComparer.Ordinal);
            foreach(var pair in input.ZoneManager.CachedZones)foreach(var e in pair.Value.GetReadOnlyEntities())
            {
                var felling=e.GetPart<FellingScenePropPart>();var village=e.GetPart<MorrowfastPropPart>();
                if(felling==null&&village==null)continue;
                Require(!string.IsNullOrEmpty(e.ID),"Authored owner stable ID exists.");
                string component=felling!=null?felling.ComponentId:village.ComponentId;
                var at=pair.Value.GetEntityPosition(e);
                var resolved=felling!=null?FellingSceneRuntime.FindOwner(pair.Value,component):MorrowfastSceneRuntime.FindOwner(pair.Value,component);
                result.Add(pair.Key+"/"+e.ID,new OwnerRow {zoneId=pair.Key,id=e.ID,blueprint=e.BlueprintName,componentId=component,
                    family=felling!=null?"Felling":"Morrowfast",x=at.x,y=at.y,reference=e,runtimeResolvesExact=ReferenceEquals(resolved,e)});
            }
            return result;
        }
        static string WaterSnapshot(Zone zone)
        {
            var b=new StringBuilder();for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
            {int turns=zone.TileState.CoatingTurns(x,y,"water");if(turns>0)b.Append(y*Zone.Width+x).Append(':').Append(turns).Append(';');}return b.ToString();
        }
        static string TileSnapshot(Zone zone)
        {
            var keys=new List<int>();zone.TileState.CollectWrittenKeys(keys);keys.Sort();var b=new StringBuilder();
            foreach(int k in keys)
            {
                var t=zone.TileState.Get(k%Zone.Width,k/Zone.Width);if(t==null||t.IsEmpty)continue;b.Append(k).Append('|');
                foreach(var layer in t.Coatings){b.Append('C');Token(b,layer.Id);b.Append(layer.Turns).Append(';');}
                foreach(var layer in t.Residues){b.Append('R');Token(b,layer.Id);b.Append(layer.Turns).Append(';');}
                b.Append(t.Heat).Append(',').Append(t.Cold).Append(',').Append(t.Charge).Append(',');Token(b,t.Cloud??"");b.Append(t.CloudTurns).Append('\n');
            }
            return b.ToString();
        }
        static void Token(StringBuilder b,string s)=>b.Append(s.Length).Append(':').Append(s).Append(':');
        static string Hash(string value){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-","").ToLowerInvariant();}
        TileRow[] CacheSnapshot()=>input.ZoneManager.CachedZones.OrderBy(p=>p.Key,StringComparer.Ordinal).Select(p=>new TileRow{zoneId=p.Key,snapshot=TileSnapshot(p.Value),sha256=Hash(TileSnapshot(p.Value))}).ToArray();
        static bool SameCache(TileRow[] a,TileRow[] b)=>a.Length==b.Length&&a.Zip(b,(x,y)=>x.zoneId==y.zoneId&&x.snapshot==y.snapshot).All(v=>v);
        void Check(string name,bool pass,string detail=null){checks.Add(new CheckRow{name=name,pass=pass,detail=detail});if(!pass)failures++;}
        static void Require(bool value,string message){if(!value)throw new InvalidOperationException(message);}
        public void SetUnexpectedErrors(int count){unexpected=count;if(report!=null)WriteReport();}
        public void Abort(string reason){if(Finished)return;StopAllCoroutines();DisposePendingSteps();fatal=reason;Check("aborted",false,reason);Finish();}
        void Finish()
        {
            if(Finished)return;
            var measuredProfile=FinishRingProfile();
            report=new Report{profile=measuredProfile,runId=RunId,saveRoot=saveRoot,gameId=gameId,markerId=markerId,fatal=fatal,worldSeed=input?.WorldMap?.Seed??0,
                wallSeconds=watch?.Elapsed.TotalSeconds??0,nativeSteps=nativeSteps,workloadComplete=completed,
                screenWidth=Screen.width,screenHeight=Screen.height,unityVersion=Application.unityVersion,gpu=SystemInfo.graphicsDeviceName,
                borders=borders.ToArray(),zones=zones.ToArray(),screenshots=screenshots.ToArray(),
                bounds="Native acceptance; optional separate profile receipt is attached when explicitly requested. Actual keyboard N, two initial road moves, L/I/Escape/F11/Shift-F11/F5/F6; actual GameView captures. All 24 border directions use explicitly labelled fixture positioning of the same native player plus real TransitionPlayer and the ordinary binding/autosave handler. API checks consume no turns; NPC AI parts and registration are retained, with no health or enemy suppression. Eight zone screenshots preserve ordinary FOV, never FullReveal. Water erasure and post-save mutations are explicit scenario stimuli on real generated cells, not Scrape gameplay claims. Entity picking is the public presenter API, not mouse-input routing. A real factory Dagger is temporary fallback-eligibility setup; its visible pixels require manual screenshot inspection. No visual-quality, complete model-coverage, GPU masking, roof-interior, equipment-art, hostile-combat or stable-60fps claim. Separate EditMode/GPU/visual gates remain required; performance is measured only when the explicit optional profile is present."};
            WriteReport();Finished=true;
        }
        void WriteReport(){report.failures=Failures;report.unexpectedErrors=unexpected;report.checks=checks.ToArray();File.WriteAllText(ReportPath,JsonUtility.ToJson(report,true));}
        void CleanupInput()
        {
            if(!initialized||cleaned)return;cleaned=true;
            if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);if(oldKeyboard!=null&&oldKeyboard.added)oldKeyboard.MakeCurrent();
            if(originalSettings!=null)InputSystem.settings=originalSettings;if(ownedSettings!=null)Destroy(ownedSettings);Application.runInBackground=originalBackground;
            Village3DSettings.Enabled=originalEnabled;Village3DSettings.LowDetail=originalLow;
            for(int i=0;i<PreferenceKeys.Length;i++)if(prefPresent[i])PlayerPrefs.SetInt(PreferenceKeys[i],prefValue[i]);else PlayerPrefs.DeleteKey(PreferenceKeys[i]);PlayerPrefs.Save();
            if(report!=null)
            {
                report.displayPreferencesRestored=Village3DSettings.Enabled==originalEnabled&&Village3DSettings.LowDetail==originalLow&&Enumerable.Range(0,2).All(i=>PlayerPrefs.HasKey(PreferenceKeys[i])==prefPresent[i]&&(!prefPresent[i]||PlayerPrefs.GetInt(PreferenceKeys[i])==prefValue[i]));
                report.inputSettingsRestored=ReferenceEquals(InputSystem.settings,originalSettings)&&Application.runInBackground==originalBackground&&(keyboard==null||!keyboard.added);
                if(!report.displayPreferencesRestored||!report.inputSettingsRestored)failures++;
            }
        }
        void OnDestroy()
        {
            if(!initialized)return;
            DisposePendingSteps();
            if(!Finished){fatal="Play ended before workload completion.";Check("premature_shutdown",false);Finish();}
            try
            {
                report.shutdownObserved=true;report.shutdownRootHeld=SaveGameService.SaveRootOverride==saveRoot;
                const BindingFlags flags=BindingFlags.Static|BindingFlags.NonPublic;
                report.shutdownSavingUnregistered=typeof(SaveGameService).GetField("_captureCurrent",flags).GetValue(null)==null&&typeof(SaveGameService).GetField("_applyLoaded",flags).GetValue(null)==null;
                if(!report.shutdownRootHeld||!report.shutdownSavingUnregistered)failures++;
            }
            finally { CleanupInput();WriteReport();watch?.Stop(); }
        }
        [Serializable] public sealed class CheckRow{public string name,detail;public bool pass;}
        [Serializable] public sealed class BorderRow{public string from,to,direction,error;public int sourceX,sourceY,arrivalX,arrivalY,tickBefore,tickAfter;public bool pass;}
        [Serializable] public sealed class ZoneRow{public string zoneId,playerId,modelId,tileHash;public float pickWorldX,pickWorldY;public int x,y,entityCount,creatureCount,groundPatches;public bool playerVisible,presentationReady,presentationVisible,fullReveal;}
        [Serializable] public sealed class OwnerRow
        {public string zoneId,id,blueprint,componentId,family;public int x,y;public bool runtimeResolvesExact;[NonSerialized] public Entity reference;}
        [Serializable] public sealed class OwnerLedger{public OwnerRow[] owners;}
        [Serializable] public sealed class TileRow{public string zoneId,snapshot,sha256;}
        [Serializable] public sealed class TileLedger{public TileRow[] zones;}
        [Serializable] public sealed class Report
        {public string runId,saveRoot,gameId,markerId,fatal,bounds,unityVersion,gpu;public int failures,unexpectedErrors,worldSeed,nativeSteps,screenWidth,screenHeight;public double wallSeconds;
            public ProfileReport profile;public bool workloadComplete,shutdownObserved,shutdownRootHeld,shutdownSavingUnregistered,displayPreferencesRestored,inputSettingsRestored;public CheckRow[] checks;public BorderRow[] borders;public ZoneRow[] zones;public string[] screenshots;}
    }
}
