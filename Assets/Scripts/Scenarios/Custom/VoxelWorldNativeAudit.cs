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
using CavesOfOoo.Skills;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Explicit isolated four-chunk voxel acceptance. Reuses the native pilot audit
    /// mechanics with extra region, renderer-coverage and live gesture gates.</summary>
    public sealed partial class VoxelWorldNativeAudit : MonoBehaviour
    {
        public bool Finished { get; private set; }
        public int Failures => failures + unexpected;
        public string RunId { get; private set; }
        const int Seed = 729490642;
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static readonly string[] PreferenceKeys = { Village3DSettings.PreferenceKey, Village3DSettings.LowDetailPreferenceKey };
        readonly List<CheckRow> checks = new List<CheckRow>();
        readonly List<string> screenshots = new List<string>();
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
        string Dir => Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/Verification/VoxelWorld"));
        string Stem => "VWN-" + RunId;
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
            initialized = true; // Arm restoration before the first mutable runtime setting/device operation.
            ownedSettings = Instantiate(originalSettings); ownedSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            ownedSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = ownedSettings; Application.runInBackground = true;
            keyboard = InputSystem.AddDevice<Keyboard>();
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

        string damagedOwnerId, removedOwnerId;
        const string PipeOwnerId="movable-copper-pipe";
        int expectedDamagedHp;
        (int x,int y) expectedPipeAt;

        IEnumerator RunAudit()
        {
            yield return new WaitForSecondsRealtime(.8f);
            input=FindFirstObjectByType<InputHandler>();Require(input!=null,"Ordinary input exists.");
            Require(Screen.width==1920&&Screen.height==1080,"Actual GameView must be1920x1080.");
            var boot=(BootMenuController)typeof(InputHandler).GetField("_bootMenuController",Private).GetValue(input);
            Require(boot!=null&&boot.IsActive,"Private boot marker must open ordinary menu.");
            yield return Tap(Key.N);
            Check("native_N_real_new_game",!boot.IsActive&&State()=="Normal"&&input.WorldMap.Seed==Seed
                &&input.CurrentZone.ZoneID==Id(3,6)&&Position()==(40,23));
            Require(checks.Last().pass,"Native N must start Morrowfast entrance at requested seed.");
            Village3DSettings.Enabled=true;Village3DSettings.LowDetail=false;
            yield return AuditTownAndWest();
            yield return NativeStep(Key.S,(0,1));
            int before=input.TurnManager.TickCount;var oldZone=input.CurrentZone;
            yield return Tap(Key.S);nativeSteps++;
            Check("native_south_entry",input.CurrentZone.ZoneID==MultiCellPilotRuntime.ZoneID&&Position()==(40,0)
                &&input.TurnManager.TickCount>before&&oldZone.GetEntityCell(input.PlayerEntity)==null
                &&PlayerReferences()==1&&CurrentBindingIntact());
            Require(checks.Last().pass,"Ordinary S must enter authored south chunk.");
            RecordInitialSouthBorder(oldZone,before);
            yield return WaitForRing();
            var zone=input.CurrentZone;var owners=PilotOwners(zone);
            Check("authored_native_owner_contract",MultiCellPilotRuntime.IsActive(zone)&&owners.Length==149
                &&owners.Select(e=>e.ID).Distinct().Count()==149&&owners.Select(e=>e.GetPart<MultiCellPilotPropPart>().OwnerId).Distinct().Count()==149
                &&owners.All(e=>e.HasPart<SpatialFootprintPart>())&&zone.GetReadOnlyEntities().Count(e=>e.BlueprintName=="TepuiStone")==2000
                &&input.WorldMap.GetBiome(3,7)==BiomeType.Stump&&StumpBands.BandAt(3,7)==StumpBand.Foothills);
            Check("native_presenter_binding",Presenter().IsReady&&Presenter().PresentationVisible&&!Presenter().FullReveal
                &&ReferenceEquals(Presenter().CurrentZone,zone)&&Presenter().IsRenderedEntity(input.PlayerEntity));
            yield return Capture("south-entry");
            yield return RecordChunk("southern-ridge");
            // Explicit fixture movement to a native free lane; no terrain, AI, health or FOV edits.
            FixturePlace(zone,NearestFree(zone,40,12));yield return WaitForRing();
            yield return RunPilotProfilePair("pristine");
            yield return AuditGameplay();
            yield return AuditEastAndReturn();
            yield return AuditRevisitAndSave();
            FixturePlace(input.CurrentZone,NearestFree(input.CurrentZone,40,12));yield return WaitForRing();
            yield return RunPilotProfilePair("restored");
            CompletePilotProfile();
            CompleteRegion();
            yield return AuditNativeSpellGesture();
            Check("private_boot_marker_unchanged",File.ReadAllBytes(Path.Combine(saveRoot,markerId,"Quick.sav.gz")).SequenceEqual(markerBytes));
            Check("owned_save_only",OwnedCheckpoint());
            completed=true;
        }

        IEnumerator AuditGameplay()
        {
            var zone=input.CurrentZone;var ridge=MultiCellPilotRuntime.FindOwner(zone,"PilotRidgeNE-6-2");Require(ridge!=null,"Native reference ridge exists.");
            var edge=zone.GetCell(8,2);var hole=zone.GetCell(6,2);
            Check("physical_edge_selection_and_hole",ReferenceEquals(WorldInteractionSystem.FindInCell(edge,ridge.ID),ridge)
                &&WorldInteractionSystem.FindInCell(hole,ridge.ID)==null&&zone.GetOccupiedCells(ridge).Count==10
                &&!hole.Occupants.Contains(ridge));
            var near=NearestReachableEdge(zone,ridge);FixturePlace(zone,near);yield return WaitForRing();
            Physics.SyncTransforms();var presenter=Presenter();
            Require(presenter.TryGetEntityView(ridge,out var ridgeView,out _)&&ridgeView!=null&&ridgeView.activeInHierarchy,
                "Native picking stimulus requires the actual visible imported ridge model.");
            var sourceCamera=(Camera)typeof(SpawnRing3DPresenter).GetField("source",Private).GetValue(presenter);
            Cell pickCell=null;Vector2 pickCursor=default;Vector3 measuredContact=default;
            var anchor=zone.GetEntityCell(ridge);
            foreach(var candidate in zone.GetOccupiedCells(ridge))
            {
                if(candidate==anchor||!candidate.IsVisible)continue;
                bool boundary=false;
                foreach(var step in new[]{(-1,0),(1,0),(0,-1),(0,1)})
                    boundary|=zone.GetCell(candidate.X+step.Item1,candidate.Y+step.Item2)?.Occupants.Contains(ridge)!=true;
                if(boundary&&TryProjectNativeMeshContact(sourceCamera,presenter.WorldCamera,ridgeView,ridge,candidate,out pickCursor,out measuredContact))
                {pickCell=candidate;break;}
            }
            Require(pickCell!=null,"An already-visible nonanchor edge must have an independently measured imported mesh contact.");
            Check("native_physical_edge_pick",presenter.TryPickWorld(pickCursor,out var picked,out int pickX,out int pickY)
                &&ReferenceEquals(picked,ridge)&&(pickX,pickY)==(pickCell.X,pickCell.Y)
                &&pickCell!=anchor&&pickCell.IsVisible&&pickCell.Occupants.Contains(ridge),
                $"Actual camera projection of imported mesh contact {measuredContact} in physical cell({pickCell.X},{pickCell.Y}); cursor {pickCursor}. The separate occupancy-hole gate remains authoritative; a tilted ray may cross geometry outside its flat cell.");
            Check("physical_edge_reach",DestructionSystem.IsWithinStrikeReach(input.PlayerEntity,ridge,zone));
            Require(checks.Last().pass,"Named structural stimulus is within native reach.");
            damagedOwnerId=ridge.GetPart<MultiCellPilotPropPart>().OwnerId;
            var neighbor=PilotOwners(zone).First(e=>e.BlueprintName=="GrainRidge"&&!ReferenceEquals(e,ridge));
            removedOwnerId=neighbor.GetPart<MultiCellPilotPropPart>().OwnerId;
            int hpBefore=ridge.GetPart<DestructiblePart>().HP,neighborHp=neighbor.GetPart<DestructiblePart>().HP;
            var contacts=zone.GetOccupiedCells(ridge).Concat(zone.GetOccupiedCells(ridge)).ToArray();
            var pulse=MultiCellAbilityQueries.SnapshotOccupants(contacts);
            Check("duplicate_body_contacts_resolve_one_damage_owner",contacts.Count(c=>c.Occupants.Contains(ridge))==20
                &&pulse.Count(e=>ReferenceEquals(e,ridge))==1
                &&MultiCellAbilityQueries.SnapshotOccupants(contacts,ridge).All(e=>!ReferenceEquals(e,ridge)),
                "Actual native cast-local occupancy snapshot with repeated body cells and excluded-owner countercheck; this is not an all-spell claim.");
            int applied=0;
            foreach(var owner in pulse)if(ReferenceEquals(owner,ridge))
                applied+=DestructionSystem.RouteDamage(owner,new Damage(10),input.PlayerEntity,zone);
            expectedDamagedHp=hpBefore-Math.Max(1,10-ridge.GetPart<DestructiblePart>().Hardness);
            Check("one_structural_damage_owner",applied==hpBefore-expectedDamagedHp&&ridge.GetPart<DestructiblePart>().HP==expectedDamagedHp
                &&neighbor.GetPart<DestructiblePart>().HP==neighborHp,"Named RouteDamage API stimulus at a physically reachable edge; independent neighbor is the countercheck.");
            var body=new List<(int x,int y)>();foreach(var cell in zone.GetOccupiedCells(neighbor))body.Add((cell.X,cell.Y));
            FixturePlace(zone,NearestReachableEdge(zone,neighbor));yield return WaitForRing();
            Require(DestructionSystem.IsWithinStrikeReach(input.PlayerEntity,neighbor,zone),"Destructive API stimulus is within reach.");
            Require(Presenter().TryGetEntityView(neighbor,out var destroyedView,out _)&&destroyedView!=null
                &&destroyedView.activeInHierarchy&&Presenter().IsRenderedEntity(neighbor),"Native destructible model is actually visible before the hit.");
            Require(DestructionSystem.RouteDamage(neighbor,new Damage(neighborHp+100),input.PlayerEntity,zone)>0,"Native lethal structure damage lands.");
            Check("destroy_clears_whole_body_once",MultiCellPilotRuntime.FindOwner(zone,removedOwnerId)==null&&zone.GetEntityCell(neighbor)==null
                &&body.All(p=>!zone.GetCell(p.x,p.y).Occupants.Contains(neighbor))
                &&ridge.GetPart<DestructiblePart>().HP==expectedDamagedHp);
            var destroyed=neighbor.GetPart<DestructiblePart>();int count=zone.EntityCount;
            Check("duplicate_destroy_is_idempotent",DestructionSystem.Destroy(neighbor,input.PlayerEntity,zone,"native-audit-repeat")==DestroyVerdict.Destroyed
                &&zone.EntityCount==count&&destroyed.Gone);
            yield return null;yield return null;
            Check("destroyed_owner_view_removed",!Presenter().TryGetEntityView(neighbor,out _,out _)
                &&(destroyedView==null||!destroyedView.activeInHierarchy),"Actual pre-hit visible GameObject must be destroyed or inactive, not merely rejected by the canonical-membership API.");
            yield return Capture("destructible-ridge");

            var toad=MultiCellPilotRuntime.FindOwner(zone,"large-maw-toad");Require(toad!=null,"Live native MawToad exists.");
            var start=zone.GetEntityPosition(toad);FindPath path=null;var target=(-1,-1);
            Require(!BodyHasSlipperyGroundAt(zone,toad,start.x,start.y),"Original ambush body can be restored without random ground displacement.");
            for(int radius=2;radius<=12&&path==null;radius++)for(int y=1;y<Zone.Height-2&&path==null;y++)for(int x=1;x<Zone.Width-2;x++)
            {
                if(Math.Abs(x-start.x)+Math.Abs(y-start.y)!=radius||!zone.CanPlaceFootprint(toad,x,y)
                    ||BodyHasSlipperyGroundAt(zone,toad,x,y))continue;
                var candidate=FindPath.Search(zone,start.x,start.y,x,y,actor:toad);
                if(candidate.Usable&&candidate.Steps.Count>=2)
                {
                    int cx=start.x,cy=start.y;bool dry=true;
                    foreach(var step in candidate.Steps){cx+=step.dx;cy+=step.dy;dry&=!BodyHasSlipperyGroundAt(zone,toad,cx,cy);}
                    if(dry){path=candidate;target=(x,y);break;}
                }
            }
            Require(path!=null,"Native large-body path has a nontrivial reachable target.");
            int px=start.x,py=start.y;bool clear=true;
            foreach(var step in path.Steps){px+=step.dx;py+=step.dy;clear&=zone.CanPlaceFootprint(toad,px,py)&&!BodyHasSlipperyGroundAt(zone,toad,px,py);}
            Check("large_creature_path_respects_full_body",clear&&(px,py)==target&&zone.GetOccupiedCells(toad).Count==4
                &&toad.GetPart<BrainPart>()!=null&&toad.GetPart<BrainPart>().Staying&&!toad.GetPart<BrainPart>().Wanders
                &&!FindPath.Search(zone,start.x,start.y,Zone.Width-1,Zone.Height-1,actor:toad).Usable,
                "Read-only native A* and out-of-bounds-body countercheck; does not manufacture a chase or alter canon ambush behavior.");

            // Live movement is an explicit forced-displacement stimulus; the
            // ambush creature's brain, home and chase policy are not modified.
            var originalBody=new List<Cell>();foreach(var cell in zone.GetOccupiedCells(toad))originalBody.Add(cell);
            string toadId=toad.ID;var nativeBrain=toad.GetPart<BrainPart>();
            bool wasStaying=nativeBrain.Staying,wasWandering=nativeBrain.Wanders;
            int homeX=nativeBrain.StartingCellX,homeY=nativeBrain.StartingCellY;
            bool movedWholeBody=false,blockedWholeBody=false;Entity blocker=null;
            try
            {
                Require(MovementSystem.ForceMoveTo(toad,zone,target.Item1,target.Item2),"Real native large creature accepts a fully valid forced destination.");
                var movedBody=new List<Cell>();foreach(var cell in zone.GetOccupiedCells(toad))movedBody.Add(cell);
                movedWholeBody=zone.GetEntityPosition(toad)==target&&movedBody.Count==4&&movedBody.All(c=>c.Occupants.Contains(toad))
                    &&originalBody.Where(c=>!movedBody.Contains(c)).All(c=>!c.Occupants.Contains(toad))
                    &&input.TurnManager.IsRegistered(toad)&&input.TurnManager.Entities.Count(e=>ReferenceEquals(e,toad))==1;
                // An actual temporary native Rock obstructs only a returning
                // off-anchor body cell, while the original anchor stays free.
                var far=originalBody.FirstOrDefault(c=>(c.X,c.Y)!=start&&!movedBody.Contains(c)&&!c.BlocksMovement(toad));
                Require(far!=null,"Non-anchor vacated body cell exists for a nonvacuous collision countercheck.");
                blocker=input.ZoneManager.Factory.CreateEntity("Rock");Require(blocker!=null&&zone.AddEntity(blocker,far.X,far.Y),"Place named temporary native blocker.");
                Require(!zone.GetCell(start.x,start.y).BlocksMovement(toad),"Returning anchor itself remains clear; only the far body is obstructed.");
                blockedWholeBody=!MovementSystem.ForceMoveTo(toad,zone,start.x,start.y)&&zone.GetEntityPosition(toad)==target
                    &&movedBody.All(c=>c.Occupants.Contains(toad))&&zone.GetEntityCell(blocker)==far;
                Check("large_creature_blocked_far_edge_atomic",blockedWholeBody,"Named temporary native Rock, released in finally; no terrain clearing.");
            }
            finally
            {
                if(blocker!=null&&zone.GetEntityCell(blocker)!=null)Require(zone.RemoveEntity(blocker),"Remove only owned temporary blocker.");
                if(zone.GetEntityCell(toad)!=null&&zone.GetEntityPosition(toad)!=start)
                    Require(MovementSystem.ForceMoveTo(toad,zone,start.x,start.y),"Restore exact native ambush creature position before subsequent captures.");
            }
            Check("large_creature_forced_motion_and_restoration",movedWholeBody&&blockedWholeBody&&toad.ID==toadId
                &&zone.GetEntityPosition(toad)==start&&originalBody.All(c=>c.Occupants.Contains(toad))
                &&ReferenceEquals(toad.GetPart<BrainPart>(),nativeBrain)&&nativeBrain.Staying==wasStaying&&nativeBrain.Wanders==wasWandering
                &&nativeBrain.StartingCellX==homeX&&nativeBrain.StartingCellY==homeY
                &&input.TurnManager.IsRegistered(toad)&&input.TurnManager.Entities.Count(e=>ReferenceEquals(e,toad))==1);

            var pipe=MultiCellPilotRuntime.FindOwner(zone,PipeOwnerId);Require(pipe!=null,"Native authored movable pipe exists.");
            FixturePlace(zone,NearestFree(zone,40,10));
            var lane=PipeLane(zone,pipe);int laneX=lane.x,laneY=lane.y;
            Require(zone.CanPlaceFootprint(pipe,laneX,laneY),"Explicit pipe displacement destination is clear.");
            Check("pipe_force_move_whole_body",MovementSystem.ForceMoveTo(pipe,zone,laneX,laneY)&&zone.GetEntityPosition(pipe)==lane
                &&Enumerable.Range(laneX,3).All(x=>zone.GetCell(x,laneY).Occupants.Contains(pipe)),"Named force-move API stimulus, no position-field edits.");
            var pipeAt=zone.GetEntityPosition(pipe);
            Check("pipe_blocked_displacement_atomic",!MovementSystem.ForceMoveTo(pipe,zone,8,2)&&zone.GetEntityPosition(pipe)==pipeAt
                &&Enumerable.Range(laneX,3).All(x=>zone.GetCell(x,laneY).Occupants.Contains(pipe)));
            FixturePlace(zone,(laneX+3,laneY));
            Require(!zone.GetCell(laneX+4,laneY).BlocksMovement(input.PlayerEntity),"Hauler next native cell is clear.");
            Require(DragSystem.TryGrab(input.PlayerEntity,pipe,zone)==DragVerdict.Ok,"Actual player can grasp pipe's far edge.");
            try
            {
                Check("pipe_native_drag_preserves_grip",MovementSystem.TryMoveTo(input.PlayerEntity,zone,laneX+4,laneY)
                    &&zone.GetEntityPosition(pipe)==(laneX+1,laneY)&&ReferenceEquals(DragSystem.GetDragged(input.PlayerEntity),pipe)
                    &&Enumerable.Range(laneX+1,3).All(x=>zone.GetCell(x,laneY).Occupants.Contains(pipe)));
            }
            finally{DragSystem.Release(input.PlayerEntity);}
            expectedPipeAt=zone.GetEntityPosition(pipe);
            Check("pipe_not_takeable_or_throwable",!pipe.GetPart<PhysicsPart>().Takeable&&!pipe.GetPart<HandlingPart>().Carryable
                &&!pipe.GetPart<HandlingPart>().Throwable&&!DragSystem.IsDragging(input.PlayerEntity));
            yield return null;yield return null;
        }

        IEnumerator AuditRevisitAndSave()
        {
            var zone=input.CurrentZone;
            FixturePlace(zone,(40,0));int northTick=input.TurnManager.TickCount;yield return Tap(Key.W);Require(input.CurrentZone.ZoneID==Id(3,6),"Ordinary north input revisits Morrowfast.");
            RecordRevisitNorthBorder(zone,northTick);
            yield return Tap(Key.S);Require(input.CurrentZone.ZoneID==MultiCellPilotRuntime.ZoneID,"Ordinary south input revisits pilot.");
            yield return WaitForRing();
            Check("revisit_preserves_damage_absence_and_pipe",ReferenceEquals(zone,input.CurrentZone)&&ExpectedState(zone));
            FixturePlace(zone,NearestFree(zone,40,12));yield return WaitForRing();
            Require(ReadyForPlayerInput(),"F5 runs at native player boundary.");
            var beforeOwners=OwnerSnapshot(zone);var beforeTiles=CacheSnapshot();var oldPlayer=input.PlayerEntity;
            var savedPosition=Position();int tick=input.TurnManager.TickCount,energy=input.TurnManager.GetEnergy(oldPlayer);
            var oldOwners=PilotOwners(zone).ToDictionary(e=>e.GetPart<MultiCellPilotPropPart>().OwnerId,e=>e);
            Require(Presenter().TryGetEntityView(oldPlayer,out var oldPlayerView,out _)&&oldPlayerView!=null&&oldPlayerView.activeInHierarchy,
                "Current native player model is visibly present before saving.");
            var oldOwnerViews=oldOwners.Values.Select(owner=>
            {Require(Presenter().TryGetEntityView(owner,out var view,out _)&&view!=null,"Every native pilot owner has an exact pre-save model view.");return view;}).ToArray();
            yield return Tap(Key.F5);gameId=SaveGameService.GetSaveInfo("Quick")?.GameID;
            Check("native_F5_preserves_player_boundary",OwnedCheckpoint()&&ReadyForPlayerInput()&&input.TurnManager.TickCount==tick
                &&input.TurnManager.GetEnergy(oldPlayer)==energy);
            File.WriteAllText(Path.Combine(Dir,Stem+"-saved-owners.json"),beforeOwners);
            File.WriteAllText(Path.Combine(Dir,Stem+"-saved-tiles.json"),JsonUtility.ToJson(new TileLedger{zones=beforeTiles},true));
            MultiCellPilotRuntime.FindOwner(zone,damagedOwnerId).GetPart<DestructiblePart>().HP=2;
            Require(zone.RemoveEntity(MultiCellPilotRuntime.FindOwner(zone,PipeOwnerId)),"Post-save negative control removes the pipe.");
            Check("postcheckpoint_countermutation",OwnerSnapshot(zone)!=beforeOwners);
            yield return Tap(Key.F6);yield return WaitForRing();
            var loaded=input.CurrentZone;var afterOwners=OwnerSnapshot(loaded);var afterTiles=CacheSnapshot();
            File.WriteAllText(Path.Combine(Dir,Stem+"-loaded-owners.json"),afterOwners);
            File.WriteAllText(Path.Combine(Dir,Stem+"-loaded-tiles.json"),JsonUtility.ToJson(new TileLedger{zones=afterTiles},true));
            Check("native_F6_exact_owner_and_tile_state",ExpectedState(loaded)&&afterOwners==beforeOwners&&SameCache(beforeTiles,afterTiles));
            Check("native_F6_replaces_all_owner_aliases",!ReferenceEquals(zone,loaded)&&!ReferenceEquals(oldPlayer,input.PlayerEntity)
                &&oldPlayer.ID==input.PlayerEntity.ID&&Position()==savedPosition&&CurrentBindingIntact()&&PlayerReferences()==1
                &&input.TurnManager.TickCount==tick&&input.TurnManager.GetEnergy(input.PlayerEntity)==energy
                &&oldOwners.All(pair=>{var owner=MultiCellPilotRuntime.FindOwner(loaded,pair.Key);return owner!=null&&!ReferenceEquals(owner,pair.Value)&&owner.ID==pair.Value.ID;})
                &&!Presenter().TryGetEntityView(oldPlayer,out _,out _)
                &&(oldPlayerView==null||!oldPlayerView.activeInHierarchy)&&oldOwnerViews.All(view=>view==null||!view.activeInHierarchy));
            yield return Capture("restored-save");
        }
        (int x,int y) PipeLane(Zone zone,Entity pipe)
        {
            for(int distance=0;distance<Zone.Width+Zone.Height;distance++)
                for(int y=1;y<Zone.Height-1;y++)for(int x=1;x<Zone.Width-5;x++)
                    if(Math.Abs(x-38)+Math.Abs(y-12)==distance&&zone.CanPlaceFootprint(pipe,x,y)
                        &&Enumerable.Range(x,5).All(cx=>!zone.GetCell(cx,y).BlocksMovement(input.PlayerEntity)
                            &&LiquidSlipSystem.FindSlipperyLiquid(zone,zone.GetCell(cx,y))==null))return(x,y);
            throw new InvalidOperationException("No native five-cell pipe/hauler lane exists.");
        }
        // Only the named exact-position displacement stimuli choose dry ground;
        // this reads the live body/tile contract without erasing or disabling hazards.
        static bool BodyHasSlipperyGroundAt(Zone zone,Entity actor,int x,int y)
        {
            if(zone==null||actor==null)return true;
            foreach(var cell in zone.GetOccupiedCells(actor,x,y))
                if(cell==null||LiquidSlipSystem.FindSlipperyLiquid(zone,cell)!=null)return true;
            return false;
        }
        bool ExpectedState(Zone zone)=>MultiCellPilotRuntime.IsActive(zone)
            &&MultiCellPilotRuntime.FindOwner(zone,damagedOwnerId)?.GetPart<DestructiblePart>()?.HP==expectedDamagedHp
            &&MultiCellPilotRuntime.FindOwner(zone,removedOwnerId)==null
            &&MultiCellPilotRuntime.FindOwner(zone,PipeOwnerId)!=null
            &&zone.GetEntityPosition(MultiCellPilotRuntime.FindOwner(zone,PipeOwnerId))==expectedPipeAt;
        static Entity[] PilotOwners(Zone zone)=>zone.GetReadOnlyEntities().Where(e=>e.HasPart<MultiCellPilotPropPart>()).ToArray();
        static string OwnerSnapshot(Zone zone)=>JsonUtility.ToJson(new OwnerLedger{owners=PilotOwners(zone)
            .OrderBy(e=>e.GetPart<MultiCellPilotPropPart>().OwnerId,StringComparer.Ordinal).Select(e=>
            {var p=e.GetPart<MultiCellPilotPropPart>();var at=zone.GetEntityPosition(e);return new OwnerRow
                {id=e.ID,owner=p.OwnerId,model=p.ModelId,role=p.Role,blueprint=e.BlueprintName,x=at.x,y=at.y,
                 cells=e.GetPart<SpatialFootprintPart>().CellsRaw,hp=e.GetPart<DestructiblePart>()?.HP??e.GetStatValue("Hitpoints"),
                 gone=e.GetPart<DestructiblePart>()?.Gone??false};}).ToArray()},true);
        // Audit stimulus only: find a real imported surface independently of
        // TryPickWorld, whose result the caller checks exactly once afterward.
        static bool TryProjectNativeMeshContact(Camera source, Camera world, GameObject view,
            Entity owner, Cell desired, out Vector2 cursor, out Vector3 contact)
        {
            cursor=default;contact=default;
            if(source==null||world==null||view==null||!view.activeInHierarchy||owner==null||desired==null
                ||!desired.IsVisible||!desired.Occupants.Contains(owner))return false;
            var colliders=view.GetComponentsInChildren<MeshCollider>()
                .Where(c=>c.enabled&&c.gameObject.activeInHierarchy&&c.sharedMesh!=null).ToArray();
            if(colliders.Length==0)return false;
            float top=colliders.Max(c=>c.bounds.max.y)+1;
            float[] samples={.5f,.25f,.75f,.1f,.9f};
            foreach(float dz in samples)foreach(float dx in samples)
            {
                var down=new Ray(new Vector3(desired.X+dx,top,Zone.Height-1-desired.Y+dz),Vector3.down);
                RaycastHit surface=default;float closest=float.PositiveInfinity;
                foreach(var collider in colliders)
                    if(collider.Raycast(down,out var candidate,120)&&candidate.distance<closest)
                    {surface=candidate;closest=candidate.distance;}
                if(float.IsPositiveInfinity(closest))continue;
                var viewport=world.WorldToViewportPoint(surface.point);if(viewport.z<=0)continue;
                var inputRay=source.ViewportPointToRay(viewport);
                if(!new Plane(Vector3.forward,Vector3.zero).Raycast(inputRay,out float distance))continue;
                var inputPoint=inputRay.GetPoint(distance);
                // A projected target can be occluded by a different surface.
                // Re-measure the nearest real collider using the actual camera,
                // without asking the picker which result would make its gate pass.
                var ray=world.ViewportPointToRay(viewport);
                var hits=Physics.RaycastAll(ray,120,1<<NativeZone3DRenderSurface.WorldLayer,QueryTriggerInteraction.Collide);
                if(hits.Length==0)continue;
                var hit=hits.OrderBy(h=>h.distance).First();
                if(!(hit.collider is MeshCollider)||!(hit.collider.transform==view.transform||hit.collider.transform.IsChildOf(view.transform))
                    ||!Village3DProjection.TryWorldToCell(hit.point,out int x,out int y)||x!=desired.X||y!=desired.Y)continue;
                cursor=new Vector2(inputPoint.x,inputPoint.y);contact=hit.point;return true;
            }
            return false;
        }

        (int x,int y) NearestReachableEdge(Zone zone,Entity target)
        {
            var origin=zone.GetEntityPosition(target);
            for(int radius=0;radius<Zone.Width+Zone.Height;radius++)
                for(int y=0;y<Zone.Height;y++)for(int x=0;x<Zone.Width;x++)
                    if(Math.Abs(x-origin.x)+Math.Abs(y-origin.y)==radius&&!zone.GetCell(x,y).BlocksMovement(input.PlayerEntity)
                        &&SpatialQuery.DistanceToCell(zone,target,x,y)==1)return(x,y);
            throw new InvalidOperationException("No native reachable object edge.");
        }
        [Serializable] public sealed class OwnerRow{public string id,owner,model,role,blueprint,cells;public int x,y,hp;public bool gone;}
        [Serializable] public sealed class OwnerLedger{public OwnerRow[] owners;}

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
            var measuredProfile=FinishPilotProfile();
            report=new Report{profile=measuredProfile,runId=RunId,saveRoot=saveRoot,gameId=gameId,markerId=markerId,fatal=fatal,worldSeed=input?.WorldMap?.Seed??0,
                wallSeconds=watch?.Elapsed.TotalSeconds??0,nativeSteps=nativeSteps,workloadComplete=completed,
                screenWidth=Screen.width,screenHeight=Screen.height,unityVersion=Application.unityVersion,gpu=SystemInfo.graphicsDeviceName,
                screenshots=screenshots.ToArray(),region=region.ToArray(),borders=borders.ToArray(),
                bounds="Actual 1920x1080 GameView, private normal seeded N bootstrap, six directed native seam crossings and four voxel chunk captures, ordinary movement/F5/F6/hotbar keyboard input.80-second performance data covers the pilot before/after save; it is not a four-biome performance claim. Explicit fixture positioning uses the existing live player without world replacement; structure damage, pipe hauling/forced movement and path checks use named gameplay APIs and are not keyboard-combat claims. Native AI, health, FOV, factions and turn behavior remain live. Captures and counters establish observable results, not artistic quality, full-world coverage, stable frame rate or subjective feel."};
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
            try
            {
                DisposePendingSteps();
                if(!Finished){fatal="Play ended before workload completion.";Check("premature_shutdown",false);Finish();}
                if(report!=null)
                {
                    report.shutdownObserved=true;report.shutdownRootHeld=SaveGameService.SaveRootOverride==saveRoot;
                    const BindingFlags flags=BindingFlags.Static|BindingFlags.NonPublic;
                    report.shutdownSavingUnregistered=typeof(SaveGameService).GetField("_captureCurrent",flags).GetValue(null)==null&&typeof(SaveGameService).GetField("_applyLoaded",flags).GetValue(null)==null;
                    if(!report.shutdownRootHeld||!report.shutdownSavingUnregistered)failures++;
                }
            }
            finally
            {
                try { DisposeRingProfile(); }
                finally
                {
                    try { CleanupInput(); }
                    finally { watch?.Stop();if(report!=null)WriteReport(); }
                }
            }
        }
        [Serializable] public sealed class CheckRow{public string name,detail;public bool pass;}
        [Serializable] public sealed class TileRow{public string zoneId,snapshot,sha256;}
        [Serializable] public sealed class TileLedger{public TileRow[] zones;}
        [Serializable] public sealed class Report
        {public string runId,saveRoot,gameId,markerId,fatal,bounds,unityVersion,gpu;public int failures,unexpectedErrors,worldSeed,nativeSteps,screenWidth,screenHeight;public double wallSeconds;
            public RegionRow[] region;public BorderRow[] borders;public ProfileReport profile;public bool workloadComplete,shutdownObserved,shutdownRootHeld,shutdownSavingUnregistered,displayPreferencesRestored,inputSettingsRestored;public CheckRow[] checks;public string[] screenshots;}
    }
}
