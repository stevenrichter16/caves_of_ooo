using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Rendering;
using CavesOfOoo.Storylets;
using UnityEngine;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Seeded ordinary N bootstrap, native walking and border travel,
    /// real action/dialogue/loot menus, journal and F5/F6. No fixture placement,
    /// quest mutation, teleportation, camera changes or simulated screenshots.</summary>
    public sealed class ChunkGameplayNativeAudit : MonoBehaviour
    {
        public const int Seed=64;
        public bool Finished {get;private set;}
        public int Failures {get;private set;}
        public string RunId {get;private set;}
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        private static readonly (int x,int y)[] Directions={(-1,0),(1,0),(0,-1),(0,1)};
        private readonly List<CheckRow> checks=new List<CheckRow>();
        private readonly List<string> screenshots=new List<string>();
        private InputHandler input;
        private Keyboard keyboard,oldKeyboard;
        private InputSettings originalSettings,ownedSettings;
        private bool initialized,cleaned,originalBackground,originalEnabled,originalLow,originalReveal,complete;
        private float cameraSize,townCameraSize,zoomMultiplier,fallbackZoom;
        private int visibleTileRows;
        private Quaternion cameraRotation;
        private string saveRoot,gameId,markerId,fatal;
        private byte[] markerBytes;
        private int nativeSteps,unexpected,waits,stunnedTurns;
        private System.Diagnostics.Stopwatch watch;
        private Stack<IEnumerator> activeSteps;
        private Report report;
        private const double ProfileWindowSeconds=60;
        private ProfilerRecorder[] profileRecorders;
        private ProfileMetric[] profileMetrics;
        private double profileStarted,profileSeconds,profileIdleSeconds;
        private bool profileFinished;
        private static readonly string[] ProfileNames={"Main Thread","COO.Input.Update","COO.ZoneRenderer.LateUpdate","GC Allocated In Frame"};
        private string Dir=>Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/ChunkGameplayImplementation"));
        private string Stem=>"CGN-"+RunId;
        private string ReportPath=>Path.Combine(Dir,Stem+"-native.json");
        public void Initialize(string runId)
        {
            Require(Guid.TryParseExact(runId,"N",out var guid)&&guid!=Guid.Empty,"Unique run ID is required.");
            Require(!string.IsNullOrEmpty(SaveGameService.SaveRootOverride),"Private root must precede bootstrap.");
            RunId=runId;saveRoot=SaveGameService.SaveRootOverride;markerId=PlayerPrefs.GetString(SaveGameService.LastGameIDPrefsKey);
            markerBytes=File.ReadAllBytes(Path.Combine(saveRoot,markerId,"Quick.sav.gz"));
            Directory.CreateDirectory(Dir);watch=System.Diagnostics.Stopwatch.StartNew();
            originalEnabled=Village3DSettings.Enabled;originalLow=Village3DSettings.LowDetail;
            originalBackground=Application.runInBackground;originalSettings=InputSystem.settings;oldKeyboard=Keyboard.current;
            initialized=true;
            ownedSettings=Instantiate(originalSettings);ownedSettings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            ownedSettings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings=ownedSettings;Application.runInBackground=true;keyboard=InputSystem.AddDevice<Keyboard>();
            StartCoroutine(RunSafely(RunAudit()));
        }
        private IEnumerator RunSafely(IEnumerator steps)
        {
            activeSteps=new Stack<IEnumerator>();activeSteps.Push(steps);
            try
            {
                while(activeSteps.Count>0)
                {
                    bool moved=false;object current=null;Exception error=null;
                    try{moved=activeSteps.Peek().MoveNext();if(moved)current=activeSteps.Peek().Current;}catch(Exception e){error=e;}
                    if(error!=null){fatal=error.ToString();Check("fatal",false,fatal);break;}
                    if(!moved){(activeSteps.Pop() as IDisposable)?.Dispose();continue;}
                    if(current is IEnumerator nested){activeSteps.Push(nested);continue;}yield return current;
                }
            }
            finally{DisposeSteps();}
            Finish();
        }
        private IEnumerator RunAudit()
        {
            yield return new WaitForSecondsRealtime(.8f);input=FindFirstObjectByType<InputHandler>();
            Require(input!=null&&Screen.width==1920&&Screen.height==1080,"Ordinary 1080p GameView and input.");
            var boot=(BootMenuController)typeof(InputHandler).GetField("_bootMenuController",Private).GetValue(input);
            Require(boot!=null&&boot.IsActive,"The private marker presents the native new-game menu.");
            yield return Tap(Key.N);
            Check("native_N_real_new_game",!boot.IsActive&&State()=="Normal"&&input.WorldMap.Seed==Seed&&input.CurrentZone.ZoneID==MorrowfastExpedition.FieldZoneId);
            Require(checks.Last().pass,"Native N must start the western spawn at seed64.");
            originalReveal=input.ZoneRenderer.RevealEntire3DZone;
            yield return WaitForVoxel();
            cameraSize=Camera.main.orthographicSize;cameraRotation=Camera.main.transform.rotation;
            zoomMultiplier=input.CameraFollow.GameplayZoomMultiplier;visibleTileRows=input.CameraFollow.TargetVisibleTileRows;fallbackZoom=input.CameraFollow.ZoomSize;
            StartProfile();
            var basket=EntityById(MorrowfastExpedition.CacheId);Require(basket!=null,"A naturally generated expedition basket exists.");
            Check("native_cache_and_parcel_generated",basket.GetPart<ContainerPart>()?.Contents.Count(e=>e.ID==MorrowfastExpedition.ParcelId)==1);
            yield return Capture("western-spawn");
            yield return Cross(MorrowfastSceneRuntime.ZoneID,Key.D,"east_initial");
            townCameraSize=Camera.main.orthographicSize;
            yield return Approach(Farra().ID);yield return WaitForVoxel();
            CheckCue("available",QuestCueState.Available);yield return Capture("cue-available");
            yield return OpenMenu(Farra());yield return SelectCommand("Chat");
            yield return SelectDialogue(choice=>choice.Target=="DryGoods","outside-town expedition topic");
            yield return SelectDialogue(choice=>choice.Actions?.Any(a=>a.Key=="MorrowfastExpedition"&&a.Value=="accept")==true,"accept expedition");
            if(State()=="DialogueOpen")yield return Tap(Key.Escape);
            Check("native_conversation_accept",State()=="Normal"&&StoryletPart.Current.IsQuestActive(MorrowfastExpedition.QuestId));
            yield return WaitForVoxel();CheckCue("active",QuestCueState.Active);yield return Capture("cue-active");
            yield return Tap(Key.Q);
            var journal=(QuestLogSnapshot)typeof(QuestLogUI).GetField("_snapshot",Private).GetValue(input.QuestLogUI);
            Check("native_Q_journal",State()=="QuestLogOpen"&&input.QuestLogUI.IsOpen&&journal.Active.Any(e=>e.QuestId==MorrowfastExpedition.QuestId));
            yield return Capture("journal");yield return Tap(Key.Escape);Require(State()=="Normal","Journal closes natively.");
            yield return Cross(MorrowfastExpedition.FieldZoneId,Key.A,"west_recovery");
            Check("field_camera_restored",Mathf.Abs(Camera.main.orthographicSize-cameraSize)<.001f);
            basket=EntityById(MorrowfastExpedition.CacheId);Require(basket!=null,"The same native basket survives revisiting.");
            yield return Approach(basket.ID);yield return OpenMenu(basket);yield return SelectCommand("OpenContainer");
            Require(State()=="PickupOpen","Opening the actual container invokes native loot UI.");
            var pickup=input.PickupUI;var rows=(List<Entity>)typeof(PickupUI).GetField("_items",Private).GetValue(pickup);
            int index=rows.FindIndex(e=>e.ID==MorrowfastExpedition.ParcelId);Require(index>=0,"Actual parcel is in the native loot list.");
            Check("native_container_command_mode",ReferenceEquals(typeof(PickupUI).GetField("_sourceContainer",Private).GetValue(pickup),basket));
            int cursor=(int)typeof(PickupUI).GetField("_cursorIndex",Private).GetValue(pickup);
            for(;cursor<index;cursor++)yield return Tap(Key.DownArrow);for(;cursor>index;cursor--)yield return Tap(Key.UpArrow);
            yield return Tap(Key.Enter);if(State()=="PickupOpen")yield return Tap(Key.Escape);
            Check("native_TakeFromContainer_parcel",CarriedParcel()!=null&&!basket.GetPart<ContainerPart>().Contents.Any(e=>e.ID==MorrowfastExpedition.ParcelId));
            yield return Capture("recovered-parcel");
            yield return Cross(MorrowfastSceneRuntime.ZoneID,Key.D,"east_delivery");
            Check("native_carried_parcel_crosses_border",CarriedParcel()!=null);
            yield return Approach(Farra().ID);int drams=TradeSystem.GetDrams(input.PlayerEntity);int clay=InventoryUnits("FireClay");
            yield return OpenMenu(Farra());yield return SelectCommand("Chat");
            yield return SelectDialogue(choice=>choice.Actions?.Any(a=>a.Key=="MorrowfastExpedition"&&a.Value=="deliver")==true,"deliver recovered cloth");
            if(State()=="DialogueOpen")yield return Tap(Key.Escape);
            Check("native_conversation_delivery",StoryletPart.Current.IsQuestCompleted(MorrowfastExpedition.QuestId)&&!StoryletPart.Current.IsQuestActive(MorrowfastExpedition.QuestId));
            Check("native_one_reward",TradeSystem.GetDrams(input.PlayerEntity)==drams+MorrowfastExpedition.RewardDrams&&InventoryUnits("FireClay")==clay+1);
            Check("native_parcel_laid_at_supper",CarriedParcel()==null&&EntityById(MorrowfastExpedition.ParcelId)?.GetIntProperty("MorrowfastDryGoodsReturned")==1
                &&MorrowfastSceneRuntime.FindOwner(input.CurrentZone,"guest-supper-table")?.GetIntProperty("MorrowfastSupperSupplied")==1);
            yield return WaitForVoxel();CheckCue("completed",QuestCueState.None);yield return Capture("completed-supper");
            yield return OpenMenu(Farra());yield return SelectCommand("Chat");
            Require(State()=="DialogueOpen"&&ReferenceEquals(ConversationManager.Speaker,Farra())
                &&ConversationManager.VisibleChoices.Any(c=>c.Text=="Leave."&&c.Target=="End"),"The real Farra dialogue remains open with its normal exit.");
            Check("native_no_repeat_delivery_choice",!ConversationManager.VisibleChoices.Any(c=>c.Actions?.Any(a=>a.Key=="MorrowfastExpedition"&&a.Value=="deliver")==true));
            yield return Tap(Key.Escape);
            yield return InspectCoarseInteriors();
            yield return InspectRegionalIron();

            var clerk=MorrowfastSceneRuntime.FindOwner(input.CurrentZone,"east-robed-resident");
            Require(clerk!=null,"The actual starting-town clerk exists.");
            yield return Approach(clerk.ID);yield return OpenMenu(clerk);yield return SelectCommand("Chat");
            yield return SelectDialogue(choice=>choice.Target=="RegionOverview","regional directions topic");
            yield return SelectDialogue(choice=>choice.Actions?.Any(a=>a.Key==RegionalGuidance.ActionName)==true,"remember actual destination");
            if(State()=="DialogueOpen")yield return Tap(Key.Escape);
            var expectedNotes=RegionalTravelNotes.Read(input.PlayerEntity).ToArray();
            Check("native_regional_directions",expectedNotes.Length==1&&expectedNotes[0].Contains("from (3,6)"));
            yield return Tap(Key.Q);yield return Tap(Key.Tab);
            var noteLines=(List<string>)typeof(QuestLogUI).GetField("_noteLines",Private).GetValue(input.QuestLogUI);
            Check("native_travel_notes",State()=="QuestLogOpen"&&input.QuestLogUI.NotesVisible
                &&string.Join(" ",noteLines).Replace(" ","").Contains(expectedNotes[0].Replace(" ","")));
            yield return Capture("travel-notes");yield return Tap(Key.Escape);
            var expectedRegionalNotes=RegionalSituationNotes.Read(input.PlayerEntity).ToArray();
            int expectedRegionalStock=EntityUnits(MorrowfastSceneRuntime.FindOwner(input.CurrentZone,"southwest-craftsperson"),"ChoirIron");
            int expectedDrams=TradeSystem.GetDrams(input.PlayerEntity);string oldPlayer=input.PlayerEntity.ID;var oldReference=input.PlayerEntity;
            yield return Tap(Key.F5);Require(SaveGameService.HasQuickSave(),"Native F5 makes a checkpoint.");gameId=SaveGameService.GetSaveInfo("Quick").GameID;
            Check("native_F5_owned_checkpoint",OwnedCheckpoint());yield return Tap(Key.F6);yield return WaitForVoxel();
            Check("native_F6_reloads_completion",input.PlayerEntity.ID==oldPlayer&&!ReferenceEquals(oldReference,input.PlayerEntity)
                &&StoryletPart.Current.IsQuestCompleted(MorrowfastExpedition.QuestId)&&TradeSystem.GetDrams(input.PlayerEntity)==expectedDrams
                &&EntityById(MorrowfastExpedition.ParcelId)?.GetIntProperty("MorrowfastDryGoodsReturned")==1);
            Check("native_notes_restored",RegionalTravelNotes.Read(input.PlayerEntity).SequenceEqual(expectedNotes));
            var restoredOrrit=MorrowfastSceneRuntime.FindOwner(input.CurrentZone,"southwest-craftsperson");
            Check("regional_native_reload",restoredOrrit.GetPart<RegionalRequestPart>().Completed
                &&RegionalSituationNotes.Read(input.PlayerEntity).SequenceEqual(expectedRegionalNotes)
                &&EntityUnits(restoredOrrit,"ChoirIron")==expectedRegionalStock);
            var sourceDefinition=RegionalSituations.Find("morrowfast-iron");
            Check("regional_native_mined_source_stays_gone",!input.ZoneManager.GetZone(sourceDefinition.SourceZoneId).GetReadOnlyEntities()
                .Any(e=>e.ID==RegionalSituations.SourceId(sourceDefinition,input.WorldMap.Seed)));
            yield return Approach(Farra().ID);CheckCue("reloaded",QuestCueState.None);yield return Capture("restored-completion");
            Check("camera_and_reveal_untouched",input.ZoneRenderer.RevealEntire3DZone==originalReveal&&Village3DSettings.Enabled==originalEnabled
                &&Village3DSettings.LowDetail==originalLow&&Mathf.Abs(Camera.main.orthographicSize-townCameraSize)<.001f
                &&Quaternion.Angle(Camera.main.transform.rotation,cameraRotation)<.001f
                &&Mathf.Abs(input.CameraFollow.GameplayZoomMultiplier-zoomMultiplier)<.001f
                &&input.CameraFollow.TargetVisibleTileRows==visibleTileRows&&Mathf.Abs(input.CameraFollow.ZoomSize-fallbackZoom)<.001f,
                "fieldSize="+cameraSize+" townSize="+townCameraSize+" loadedTownSize="+Camera.main.orthographicSize+" zoomMultiplier="+input.CameraFollow.GameplayZoomMultiplier);
            Check("private_boot_marker_unchanged",File.ReadAllBytes(Path.Combine(saveRoot,markerId,"Quick.sav.gz")).SequenceEqual(markerBytes));
            Check("owned_save_only",OwnedCheckpoint());
            double idleStarted=Time.realtimeSinceStartupAsDouble;
            while(!profileFinished){Require(Time.realtimeSinceStartupAsDouble-profileStarted<90,"Bounded sixty-second profiler window.");yield return null;}
            profileIdleSeconds=Time.realtimeSinceStartupAsDouble-idleStarted;
            Check("native_profile_evidence",profileSeconds>=60&&profileSeconds<=90&&profileMetrics.All(m=>m.available&&m.count>1
                &&m.units==(m.name=="GC Allocated In Frame"?"Bytes":"TimeNanoseconds")&&(m.name=="GC Allocated In Frame"||m.max>0)));
            complete=true;
        }
        // Actual door actions and walking verify that the coarse art follows
        // native interiors. Neither roof visibility nor player cells are injected.
        private IEnumerator InspectCoarseInteriors()
        {
            var definition=MorrowfastSceneDefinition.Load();
            foreach(var room in definition.buildings)
            {
                var door=MorrowfastSceneRuntime.FindOwner(input.CurrentZone,room.doorId);
                var roof=MorrowfastSceneRuntime.FindOwner(input.CurrentZone,room.roofId);
                Require(door!=null&&roof!=null,"Original roof and door owners survive restyling.");
                yield return Approach(door.ID);yield return OpenMenu(door);yield return SelectCommand(MorrowfastDoorPart.OpenCommand);
                Require(MorrowfastSceneRuntime.IsDoorOpen(input.CurrentZone,room.doorId),"Native door opens.");
                yield return Walk(new HashSet<(int,int)>(room.interior.Select(c=>(c.x,c.y))));yield return WaitForVoxel();
                var presenter=input.ZoneRenderer.Village3D;
                Check("coarse_interior_"+room.id,MorrowfastSceneRuntime.GetRoomAt(input.CurrentZone,Position().x,Position().y)==room.id
                    &&!presenter.IsRenderedEntity(roof)&&presenter.TryGetOwnerView(room.doorId,out var nativeDoor,out var view)
                    &&ReferenceEquals(nativeDoor,door)&&view.GetComponent<BoxCollider>()?.bounds.size.sqrMagnitude>0);
                yield return Capture("interior-"+room.id);
                yield return Walk(new HashSet<(int,int)>{(room.entryX,room.entryY)});
                yield return Approach(door.ID);yield return OpenMenu(door);yield return SelectCommand(MorrowfastDoorPart.CloseCommand);
                yield return WaitForVoxel();
                Check("coarse_closed_"+room.id,!MorrowfastSceneRuntime.IsDoorOpen(input.CurrentZone,room.doorId)&&presenter.IsRenderedEntity(roof));
            }
        }
        // Use the same physical goods and native C menus as a player. F12 is
        // explicitly used during the long route to isolate interaction proof
        // from encounter balance; it does not change harvest law or inventory.
        private IEnumerator InspectRegionalIron()
        {
            var definition=RegionalSituations.Find("morrowfast-iron");
            var giver=MorrowfastSceneRuntime.FindOwner(input.CurrentZone,"southwest-craftsperson");
            var request=giver.GetPart<RegionalRequestPart>();Require(request!=null,"Native Orrit has the actual regional request.");
            yield return Approach(giver.ID);yield return WaitForVoxel();
            Check("regional_native_available",QuestCueStateQuery.Evaluate(giver,input.CurrentZone,input.PlayerEntity)==QuestCueState.Available);
            yield return OpenMenu(giver);yield return SelectCommand("RegionalRequest:read");
            Check("regional_native_read",request.Accepted&&RegionalSituationNotes.Read(input.PlayerEntity).Count==1);
            Require(checks.Last().pass,"The actual world menu must accept the request before the journey.");
            yield return WaitForVoxel();yield return Capture("regional-request");
            yield return Tap(Key.F12);Require(DebugInvincibility.IsEnabled(input.PlayerEntity),"Explicit native F12 protects only the long verification journey.");
            StopProfile();profileFinished=false;StartProfile();
            yield return Cross("Overworld.2.6.0",Key.A,"regional_west_spawn");
            yield return Cross("Overworld.2.5.0",Key.W,"regional_north_stump");
            yield return Cross(definition.SourceZoneId,Key.A,"regional_west_source");
            var vein=EntityById(RegionalSituations.SourceId(definition,input.WorldMap.Seed));Require(vein!=null,"Real finite request vein exists in Grove ground.");
            yield return Approach(vein.ID);yield return Capture("regional-protected-vein");
            int standing=PlayerReputation.Get("RotChoir"),iron=InventoryUnits("ChoirIron");
            yield return OpenMenu(vein);yield return SelectCommand("Harvest");
            Check("regional_native_harvest",InventoryUnits("ChoirIron")>iron&&EntityById(vein.ID)==null);
            Check("regional_native_grove_law",PlayerReputation.Get("RotChoir")==standing+GroveLaw.DigRepLoss);
            yield return Cross("Overworld.2.5.0",Key.D,"regional_east_stump");
            yield return Cross("Overworld.2.6.0",Key.S,"regional_south_spawn");
            yield return Cross(MorrowfastSceneRuntime.ZoneID,Key.D,"regional_east_recipient");
            giver=MorrowfastSceneRuntime.FindOwner(input.CurrentZone,"southwest-craftsperson");request=giver.GetPart<RegionalRequestPart>();
            yield return Approach(giver.ID);
            int stock=EntityUnits(giver,"ChoirIron"),money=TradeSystem.GetDrams(input.PlayerEntity),clay=InventoryUnits("FireClay"),carried=InventoryUnits("ChoirIron");
            yield return OpenMenu(giver);yield return SelectCommand("RegionalRequest:deliver");
            Check("regional_native_delivery",request.Completed&&!request.Accepted&&EntityUnits(giver,"ChoirIron")==stock+1&&InventoryUnits("ChoirIron")==carried-1);
            Check("regional_native_reward",TradeSystem.GetDrams(input.PlayerEntity)==money+definition.RewardDrams&&InventoryUnits("FireClay")==clay+1);
            Check("regional_native_completed_cue",QuestCueStateQuery.Evaluate(giver,input.CurrentZone,input.PlayerEntity)==QuestCueState.None);
            yield return WaitForVoxel();yield return Capture("regional-delivered");
            yield return OpenMenu(giver);Check("regional_native_no_repeat_command",!Actions().Any(a=>a.Command=="RegionalRequest:deliver"));yield return Tap(Key.Escape);
            yield return Tap(Key.Q);yield return Tap(Key.Tab);
            var lines=(List<string>)typeof(QuestLogUI).GetField("_noteLines",Private).GetValue(input.QuestLogUI);
            Check("regional_native_note_visible",input.QuestLogUI.NotesVisible&&string.Join(" ",lines).Replace(" ","").Contains(definition.Title.Replace(" ","")));
            yield return Capture("regional-notes");yield return Tap(Key.Escape);
            yield return Tap(Key.F12);Check("regional_native_debug_restored",!DebugInvincibility.IsEnabled(input.PlayerEntity));
        }
        private static int EntityUnits(Entity owner,string blueprint)=>owner.GetPart<InventoryPart>().Objects.Where(e=>e.BlueprintName==blueprint).Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);

        private void StartProfile()
        {
            var handles=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(handles);
            var descriptions=handles.Select(ProfilerRecorderHandle.GetDescription).ToArray();
            profileRecorders=new ProfilerRecorder[ProfileNames.Length];profileMetrics=new ProfileMetric[ProfileNames.Length];
            for(int i=0;i<ProfileNames.Length;i++)
            {
                var metric=new ProfileMetric{name=ProfileNames[i]};profileMetrics[i]=metric;
                var matches=descriptions.Where(d=>d.Name==metric.name).ToArray();
                if(matches.Length==0)continue;
                var d=matches[0];var options=ProfilerRecorderOptions.Default;
                if(metric.name.StartsWith("COO.",StringComparison.Ordinal))options|=ProfilerRecorderOptions.SumAllSamplesInFrame;
                profileRecorders[i]=ProfilerRecorder.StartNew(d.Category,d.Name,2,options);
                metric.available=profileRecorders[i].Valid;metric.units=d.UnitType.ToString();
            }
            profileStarted=Time.realtimeSinceStartupAsDouble;
        }
        private void LateUpdate()
        {
            if(profileRecorders==null||profileFinished)return;
            for(int i=0;i<profileRecorders.Length;i++)
            {
                if(!profileMetrics[i].available)continue;
                if(!profileRecorders[i].Valid){profileMetrics[i].available=false;continue;}
                if(profileRecorders[i].Count==0)
                {if(profileMetrics[i].count>0)profileMetrics[i].available=false;continue;}
                long value=profileRecorders[i].LastValue;var metric=profileMetrics[i];
                metric.count++;metric.sum+=value;if(value>metric.max)metric.max=value;
            }
            if(Time.realtimeSinceStartupAsDouble-profileStarted>=ProfileWindowSeconds)StopProfile();
        }
        private void StopProfile()
        {
            if(profileRecorders==null||profileFinished)return;
            profileSeconds=Time.realtimeSinceStartupAsDouble-profileStarted;profileFinished=true;
            for(int i=0;i<profileRecorders.Length;i++)profileRecorders[i].Dispose();
        }
        private Entity Farra()=>MorrowfastSceneRuntime.FindOwner(input.CurrentZone,"farra-sprig");
        private Entity EntityById(string id)=>input.CurrentZone.GetReadOnlyEntities().SingleOrDefault(e=>e.ID==id);
        private Entity CarriedParcel()=>input.PlayerEntity.GetPart<InventoryPart>().Objects.SingleOrDefault(e=>e.ID==MorrowfastExpedition.ParcelId);
        private int InventoryUnits(string bp)=>input.PlayerEntity.GetPart<InventoryPart>().Objects.Where(e=>e.BlueprintName==bp).Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);
        private void CheckCue(string label,QuestCueState expected)
        {
            var owner=Farra();var v=input.ZoneRenderer.Village3D;Require(owner!=null&&v!=null,"Native Farra and village presenter.");
            bool drawn=v.TryGetQuestCue(owner,out var root,out string state);
            Check("quest_cue_"+label,QuestCueStateQuery.Evaluate(owner,input.CurrentZone,input.PlayerEntity)==expected
                &&(expected==QuestCueState.None?!drawn:drawn&&state==expected.ToString()&&root.GetComponentsInChildren<Renderer>().Any(r=>r.enabled&&r.gameObject.activeInHierarchy)),state);
        }
        private IEnumerator WaitForVoxel()
        {
            double began=Time.realtimeSinceStartupAsDouble;
            while(true)
            {
                bool town=input.CurrentZone.ZoneID==MorrowfastSceneRuntime.ZoneID;var v=input.ZoneRenderer.Village3D;var r=FindFirstObjectByType<SpawnRing3DPresenter>();
                if(town?v!=null&&ReferenceEquals(v.CurrentZone,input.CurrentZone)&&v.IsReady&&v.PresentationVisible&&v.VoxelPresentationActive
                    :r!=null&&ReferenceEquals(r.CurrentZone,input.CurrentZone)&&r.IsReady&&r.PresentationVisible&&r.VoxelPresentationActive)break;
                Require(Time.realtimeSinceStartupAsDouble-began<12,"Native voxel presentation must become ready.");yield return null;
            }
            yield return new WaitForSecondsRealtime(.3f);
        }
        private IEnumerator Cross(string targetId,Key key,string label)
        {
            int dx=key==Key.D?1:key==Key.A?-1:0,dy=key==Key.S?1:key==Key.W?-1:0;
            Require(Math.Abs(dx)+Math.Abs(dy)==1,"A cardinal native border key is required.");
            var source=input.CurrentZone;var from=WorldMap.FromZoneID(source.ZoneID);var to=WorldMap.FromZoneID(targetId);
            Require(to.x==from.x+dx&&to.y==from.y+dy&&to.z==from.z,"The requested border must actually neighbor this chunk.");
            var target=input.ZoneManager.GetZone(targetId);var candidates=new HashSet<(int,int)>();
            int edge=dx!=0?Zone.Height:Zone.Width;
            for(int i=1;i<edge-1;i++)
            {
                int sx=dx!=0?(dx>0?Zone.Width-1:0):i,sy=dy!=0?(dy>0?Zone.Height-1:0):i;
                int tx=dx!=0?(dx>0?0:Zone.Width-1):i,ty=dy!=0?(dy>0?0:Zone.Height-1):i;
                if(!target.GetCell(tx,ty).BlocksMovement(input.PlayerEntity))candidates.Add((sx,sy));
            }
            yield return Walk(candidates);var before=Position();var player=input.PlayerEntity;int tick=input.TurnManager.TickCount;
            yield return Tap(key);nativeSteps++;
            var expected=(dx!=0?(dx>0?0:Zone.Width-1):before.x,dy!=0?(dy>0?0:Zone.Height-1):before.y);
            Check("native_border_"+label,input.CurrentZone.ZoneID==targetId&&ReferenceEquals(player,input.PlayerEntity)&&Position()==expected
                &&source.GetEntityCell(player)==null&&input.TurnManager.TickCount>tick);
            Require(checks.Last().pass,"Actual border input failed.");yield return WaitForVoxel();
        }
        private IEnumerator Approach(string id)
        {
            for(int attempt=0;attempt<350;attempt++)
            {
                var e=EntityById(id);Require(e!=null,"Live approach target "+id);var p=Position();
                if(Directions.Any(d=>input.CurrentZone.GetCell(p.x+d.x,p.y+d.y)?.Occupants.Contains(e)==true))yield break;
                var at=input.CurrentZone.GetEntityPosition(e);var path=FindPath(new HashSet<(int,int)>(Directions.Select(d=>(at.x+d.x,at.y+d.y))));
                if(path==null){Require(++waits<25,"Bounded native fauna wait.");yield return Tap(Key.Period);continue;}
                Require(path.Count>0,"Native approach has a next step.");yield return Step(path[0]);
            }
            throw new InvalidOperationException("Approach exceeded its native movement budget: "+id);
        }
        private IEnumerator Walk(HashSet<(int,int)> targets)
        {
            for(int attempt=0;attempt<500;attempt++)
            {
                if(targets.Contains(Position()))yield break;var path=FindPath(targets);
                if(path==null){Require(++waits<25,"Bounded native route wait.");yield return Tap(Key.Period);continue;}
                Require(path.Count>0,"Native route has a next step.");yield return Step(path[0]);
            }
            throw new InvalidOperationException("Walk exceeded native movement budget.");
        }
        private List<(int x,int y)> FindPath(HashSet<(int,int)> targets)
        {
            var start=Position();var queue=new Queue<(int,int)>();queue.Enqueue(start);var prior=new Dictionary<(int,int),(int,int)>{{start,start}};
            while(queue.Count>0)
            {
                var at=queue.Dequeue();if(targets.Contains(at)){var path=new List<(int,int)>();while(at!=start){path.Add(at);at=prior[at];}path.Reverse();return path;}
                foreach(var d in Directions){var next=(at.Item1+d.x,at.Item2+d.y);var c=input.CurrentZone.GetCell(next.Item1,next.Item2);
                    if(prior.ContainsKey(next)||c==null||c.BlocksMovement(input.PlayerEntity))continue;prior.Add(next,at);queue.Enqueue(next);}
            }
            return null;
        }
        private IEnumerator Step((int x,int y) next)
        {
            var at=Position();Require(Math.Abs(next.x-at.x)+Math.Abs(next.y-at.y)==1&&State()=="Normal","Native cardinal movement only.");
            bool stunned=input.PlayerEntity.HasEffect<StunnedEffect>();int tick=input.TurnManager.TickCount;
            yield return Tap(DirectionKey(next.x-at.x,next.y-at.y));nativeSteps++;
            if(Position()==at&&stunned&&input.TurnManager.TickCount>tick&&State()=="Normal"&&input.PlayerEntity.GetStatValue("Hitpoints")>0)
            {
                Require(++stunnedTurns<=16,"Bounded native stun recovery; do not suppress a lethal encounter.");
                Debug.Log("[ChunkGameplayNativeAudit] Native stun consumed a movement attempt; replanning after ordinary turn "+input.TurnManager.TickCount);
                yield break;
            }
            Require(Position()==next,"Native movement failed: "+at+" -> "+next+"; state="+State());
        }
        private IEnumerator OpenMenu(Entity owner)
        {
            var at=Position();var d=Directions.FirstOrDefault(p=>input.CurrentZone.GetCell(at.x+p.x,at.y+p.y)?.Occupants.Contains(owner)==true);
            Require(Math.Abs(d.x)+Math.Abs(d.y)==1,"Exact action owner is cardinally adjacent.");
            yield return Tap(Key.C);yield return Tap(DirectionKey(d.x,d.y));Require(State()=="WorldActionMenuOpen","Actual entity menu opens.");
            string pick=WorldInteractionSystem.PickTargetCommandPrefix+owner.ID;
            if(!Actions().Any(a=>a.Command==pick)&&Actions().Any(a=>a.Command==WorldInteractionSystem.PickCellCommand))yield return SelectCommand(WorldInteractionSystem.PickCellCommand);
            if(Actions().Any(a=>a.Command==pick))yield return SelectCommand(pick);
            Require(State()=="WorldActionMenuOpen"&&ReferenceEquals(input.WorldActionMenuUI.SelectedTarget,owner)&&!input.WorldActionMenuUI.SelectedCellIsPile,"Exact native owner selected.");
        }
        private IEnumerator SelectCommand(string command)
        {
            int index=Actions().FindIndex(a=>a.Command==command);Require(index>=0,"Actual menu command "+command);
            int cursor=(int)typeof(WorldActionMenuUI).GetField("_cursorIndex",Private).GetValue(input.WorldActionMenuUI);
            for(;cursor<index;cursor++)yield return Tap(Key.DownArrow);for(;cursor>index;cursor--)yield return Tap(Key.UpArrow);yield return Tap(Key.Enter);
        }
        private IEnumerator SelectDialogue(Func<CavesOfOoo.Data.ChoiceData,bool> match,string label)
        {
            Require(State()=="DialogueOpen","Actual dialogue for "+label);int index=-1;var choices=ConversationManager.VisibleChoices;
            for(int n=0;n<choices.Count;n++)if(match(choices[n])){index=n;break;}Require(index>=0,"Visible choice "+label);
            // The real pointer may hover a different row and reset the arrow
            // cursor each frame. Use the same labelled shortcut a player can,
            // after natural text reveal, so Enter cannot select a hovered exit.
            double began=Time.realtimeSinceStartupAsDouble;
            while((bool)typeof(DialogueUI).GetField("_revealing",Private).GetValue(input.DialogueUI))
            {Require(Time.realtimeSinceStartupAsDouble-began<30,"Bounded native dialogue reveal.");yield return null;}
            var selected=ConversationManager.VisibleChoices[index];
            Require(match(selected),"The visible dialogue choice remains the intended one.");
            string shortcut=MenuShortcutMap.Key(MenuShortcutMap.Positional(index)).ToString();
            Require(Enum.TryParse(shortcut,out Key key)&&key!=Key.None,"A real labelled dialogue shortcut exists.");
            Debug.Log("[ChunkGameplayNativeAudit] Select "+label+" via "+shortcut+" from "+ConversationManager.CurrentNode?.ID+" to "+selected.Target);
            yield return Tap(key);
            Require(selected.Target=="End"?State()=="Normal":State()=="DialogueOpen"
                &&(string.IsNullOrEmpty(selected.Target)||ConversationManager.CurrentNode?.ID==selected.Target),"Native dialogue selected "+label+"; state="+State());
        }
        private IEnumerator Tap(params Key[] keys)
        {
            Require(watch.Elapsed.TotalSeconds<1000,"Bounded native audit.");double began=Time.realtimeSinceStartupAsDouble;
            while(input!=null&&Time.time-(float)typeof(InputHandler).GetField("_lastMoveTime",Private).GetValue(input)<input.MoveRepeatDelay)
            {Require(Time.realtimeSinceStartupAsDouble-began<3,"Native input gate must reopen.");yield return null;}
            keyboard.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState(keys));yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return new WaitForSecondsRealtime(.13f);
        }
        private IEnumerator Capture(string label)
        {
            string file=Path.Combine(Dir,Stem+"-"+label+".png");Require(!File.Exists(file),"Never reuse an existing capture.");
            yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(file);double start=Time.realtimeSinceStartupAsDouble;
            while(!File.Exists(file)||new FileInfo(file).Length<1000){Require(Time.realtimeSinceStartupAsDouble-start<8,"Native capture must be written.");yield return null;}screenshots.Add(file);
        }
        private (int x,int y) Position()=>input.CurrentZone.GetEntityPosition(input.PlayerEntity);
        private string State()=>typeof(InputHandler).GetField("_inputState",Private).GetValue(input).ToString();
        private List<InventoryAction> Actions()=>(List<InventoryAction>)typeof(WorldActionMenuUI).GetField("_actions",Private).GetValue(input.WorldActionMenuUI);
        private static Key DirectionKey(int x,int y)=>x<0?Key.A:x>0?Key.D:y<0?Key.W:Key.S;
        private bool OwnedCheckpoint()=>SaveGameService.SaveRootOverride==saveRoot&&!string.IsNullOrEmpty(gameId)&&SaveGameService.GetSaveInfo("Quick")?.GameID==gameId
            &&File.Exists(Path.Combine(saveRoot,gameId,"Quick.sav.gz"))&&!Directory.Exists(Path.Combine(Application.persistentDataPath,"Saves",gameId));
        private static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        private void Check(string name,bool pass,string detail=null){checks.Add(new CheckRow{name=name,pass=pass,detail=detail});if(!pass)Failures++;Debug.Log("[ChunkGameplayNativeAudit] "+(pass?"PASS ":"FAIL ")+name+" "+detail);}
        public void SetUnexpectedErrors(int count){unexpected=count;if(report!=null)WriteReport();}
        public void Abort(string reason){if(Finished)return;StopAllCoroutines();DisposeSteps();fatal=reason;Check("aborted",false,reason);Finish();}
        private void DisposeSteps(){while(activeSteps!=null&&activeSteps.Count>0)(activeSteps.Pop() as IDisposable)?.Dispose();}
        private void Finish()
        {
            if(Finished)return;StopProfile();
            report=new Report{runId=RunId,saveRoot=saveRoot,gameId=gameId,markerId=markerId,fatal=fatal,worldSeed=input?.WorldMap?.Seed??0,
                nativeSteps=nativeSteps,stunnedTurns=stunnedTurns,screenWidth=Screen.width,screenHeight=Screen.height,wallSeconds=watch?.Elapsed.TotalSeconds??0,
                profileSeconds=profileSeconds,profileIdleSeconds=profileIdleSeconds,profileMetrics=profileMetrics,workloadComplete=complete,screenshots=screenshots.ToArray(),fullReveal=originalReveal,cameraSize=cameraSize,townCameraSize=townCameraSize,
                bounds="Actual native 1080p GameView and ordinary seed64 N bootstrap. No fixture placement, world replacement, direct quest action, direct inventory transfer, camera or reveal changes. Cardinal movement, nine border crossings, action/loot/dialogue menus, regional request/harvest/delivery, Q/Tab field notes and F5/F6 use queued native keys. F12 invincibility is explicitly enabled only for the long regional journey; combat balance is not tested. Profiling restarts at that journey. Reflection observes live UI cursors only. Crops and summit water are covered by separate EditMode integration tests, not this journey. ProfilerRecorder covers a sixty-second native editor window including harness planning, screenshots, existing systems and loads; aggregate timing/allocation maxima do not isolate cue cost or establish FPS. No complete-world, subjective readability or enjoyment claim."};
            WriteReport();Finished=true;
        }
        private void WriteReport(){report.failures=Failures;report.unexpectedErrors=unexpected;report.checks=checks.ToArray();File.WriteAllText(ReportPath,JsonUtility.ToJson(report,true));}
        private void CleanupInput()
        {
            if(!initialized||cleaned)return;cleaned=true;
            if(keyboard!=null&&keyboard.added)InputSystem.RemoveDevice(keyboard);if(oldKeyboard!=null&&oldKeyboard.added)oldKeyboard.MakeCurrent();
            if(originalSettings!=null)InputSystem.settings=originalSettings;if(ownedSettings!=null)Destroy(ownedSettings);Application.runInBackground=originalBackground;
            if(report!=null){report.inputSettingsRestored=ReferenceEquals(InputSystem.settings,originalSettings)&&Application.runInBackground==originalBackground&&(keyboard==null||!keyboard.added);
                report.displayPreferencesRestored=Village3DSettings.Enabled==originalEnabled&&Village3DSettings.LowDetail==originalLow;if(!report.inputSettingsRestored||!report.displayPreferencesRestored)Failures++;}
        }
        private void OnDestroy()
        {
            if(!initialized)return;
            try
            {
                DisposeSteps();if(!Finished){fatal="Play ended before workload completion.";Check("premature_shutdown",false);Finish();}
                report.shutdownObserved=true;report.shutdownRootHeld=SaveGameService.SaveRootOverride==saveRoot;
                const BindingFlags flags=BindingFlags.Static|BindingFlags.NonPublic;
                report.shutdownSavingUnregistered=typeof(SaveGameService).GetField("_captureCurrent",flags).GetValue(null)==null&&typeof(SaveGameService).GetField("_applyLoaded",flags).GetValue(null)==null;
                if(!report.shutdownRootHeld||!report.shutdownSavingUnregistered)Failures++;
            }
            finally{CleanupInput();watch?.Stop();if(report!=null)WriteReport();}
        }
        [Serializable] public sealed class CheckRow{public string name,detail;public bool pass;}
        [Serializable] public sealed class ProfileMetric{public string name,units;public bool available;public int count;public long sum,max;}
        [Serializable] public sealed class Report
        {
            public string runId,saveRoot,gameId,markerId,fatal,bounds;
            public int failures,unexpectedErrors,worldSeed,nativeSteps,stunnedTurns,screenWidth,screenHeight;
            public double wallSeconds,profileSeconds,profileIdleSeconds;public ProfileMetric[] profileMetrics;public float cameraSize,townCameraSize;public bool fullReveal;
            public bool workloadComplete,shutdownObserved,shutdownRootHeld,shutdownSavingUnregistered,displayPreferencesRestored,inputSettingsRestored;
            public CheckRow[] checks;public string[] screenshots;
        }
    }
}
