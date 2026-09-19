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
            Check("native_opening_starts_vulnerable",!DebugInvincibility.IsEnabled(input.PlayerEntity));
            Require(checks.Last().pass,"Ordinary opening starts without debug invincibility.");
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
            Check("native_opening_completed_without_debug",!DebugInvincibility.IsEnabled(input.PlayerEntity)
                &&input.PlayerEntity.GetStatValue("Hitpoints")>0&&StoryletPart.Current.IsQuestCompleted(MorrowfastExpedition.QuestId),
                "hp="+input.PlayerEntity.GetStatValue("Hitpoints")+"; no F12 or synthetic healing used in the opening.");
            Require(checks.Last().pass,"Ordinary expedition and five interiors completed while vulnerable.");
            yield return InspectRegionalIron();
            yield return InspectMaterialAndBell();

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
            int expectedFireClay=InventoryUnits("FireClay");
            int expectedDrams=TradeSystem.GetDrams(input.PlayerEntity);string oldPlayer=input.PlayerEntity.ID;var oldReference=input.PlayerEntity;
            yield return Tap(Key.F5);Require(SaveGameService.HasQuickSave(),"Native F5 makes a checkpoint.");gameId=SaveGameService.GetSaveInfo("Quick").GameID;
            Check("native_F5_owned_checkpoint",OwnedCheckpoint());yield return Tap(Key.F6);yield return WaitForVoxel();
            Check("native_F6_reloads_completion",input.PlayerEntity.ID==oldPlayer&&!ReferenceEquals(oldReference,input.PlayerEntity)
                &&StoryletPart.Current.IsQuestCompleted(MorrowfastExpedition.QuestId)&&TradeSystem.GetDrams(input.PlayerEntity)==expectedDrams
                &&EntityById(MorrowfastExpedition.ParcelId)?.GetIntProperty("MorrowfastDryGoodsReturned")==1);
            Check("material_native_bell_restored",InventoryUnits("FireClay")==expectedFireClay
                &&StoryletPart.Current.IsQuestCompleted(MorrowfastQuests.BellQuestId)
                &&input.PlayerEntity.GetIntProperty(MorrowfastQuests.BellDiagnosed)==1
                &&input.PlayerEntity.GetIntProperty(MorrowfastQuests.BellWorked)==1
                &&MorrowfastSceneRuntime.FindOwner(input.CurrentZone,"north-oath-arch")?.GetProperty("MorrowfastBellMode")=="quiet");
            Check("native_notes_restored",RegionalTravelNotes.Read(input.PlayerEntity).SequenceEqual(expectedNotes));
            var restoredOrrit=MorrowfastSceneRuntime.FindOwner(input.CurrentZone,"southwest-craftsperson");
            Check("regional_native_reload",restoredOrrit.GetPart<RegionalRequestPart>().Completed
                &&RegionalSituationNotes.Read(input.PlayerEntity).SequenceEqual(expectedRegionalNotes)
                &&EntityUnits(restoredOrrit,"ChoirIron")==expectedRegionalStock);
            var sourceDefinition=RegionalSituations.Find("morrowfast-iron");
            yield return Tap(Key.Q);yield return Tap(Key.Tab);
            yield return InspectRegionalReceipt("regional_native_reloaded_receipt_visible",sourceDefinition,"regional-receipt-restored");yield return Tap(Key.Escape);
            yield return Approach(restoredOrrit.ID);yield return OpenMenu(restoredOrrit);
            Check("regional_native_reloaded_no_repeat",!Actions().Any(a=>a.Command=="RegionalRequest:deliver"));yield return Tap(Key.Escape);
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
            int previewCached=input.ZoneManager.CachedZoneCount;
            yield return OpenMenu(giver);yield return SelectCommand("RegionalRequest:read");
            Check("regional_native_read",!request.Accepted&&RegionalSituationNotes.Read(input.PlayerEntity).Count==0
                &&input.ZoneManager.CachedZoneCount==previewCached
                &&QuestCueStateQuery.Evaluate(giver,input.CurrentZone,input.PlayerEntity)==QuestCueState.Available);
            Require(checks.Last().pass,"Reading terms must remain a neutral preview.");
            yield return WaitForVoxel();yield return Capture("regional-offer-preview");
            var giverAt=input.CurrentZone.GetEntityPosition(giver);
            var away=new HashSet<(int,int)>();
            for(int x=1;x<Zone.Width-1;x++)for(int y=1;y<Zone.Height-1;y++)
                if(Math.Max(Math.Abs(x-giverAt.x),Math.Abs(y-giverAt.y))==3&&!input.CurrentZone.GetCell(x,y).BlocksMovement(input.PlayerEntity))away.Add((x,y));
            yield return Walk(away);
            Check("regional_native_preview_walkaway",!request.Accepted&&RegionalSituationNotes.Read(input.PlayerEntity).Count==0
                &&QuestCueStateQuery.Evaluate(giver,input.CurrentZone,input.PlayerEntity)==QuestCueState.Available);
            yield return Approach(giver.ID);yield return OpenMenu(giver);yield return SelectCommand("RegionalRequest:accept");
            Check("regional_native_accept",request.Accepted&&RegionalSituationNotes.Read(input.PlayerEntity).Count==1
                &&QuestCueStateQuery.Evaluate(giver,input.CurrentZone,input.PlayerEntity)==QuestCueState.Active);
            Require(checks.Last().pass,"Only explicit acceptance records an undertaking.");
            yield return OpenMenu(giver);yield return SelectCommand("RegionalRequest:release");
            string released=RegionalSituationNotes.Read(input.PlayerEntity).Single();
            yield return OpenMenu(giver);yield return SelectCommand("RegionalRequest:read");
            Check("regional_native_release_preview",!request.Accepted&&released.Contains("[released]")
                &&RegionalSituationNotes.Read(input.PlayerEntity).Single()==released);
            yield return OpenMenu(giver);yield return SelectCommand("RegionalRequest:accept");
            Check("regional_native_reaccept",request.Accepted&&RegionalSituationNotes.Read(input.PlayerEntity).Count==1);
            yield return WaitForVoxel();yield return Capture("regional-request");
            yield return Tap(Key.Q);yield return Tap(Key.Tab);
            string activePage=DrawnJournalText();
            Check("regional_native_active_note",input.QuestLogUI.NotesVisible&&activePage.Contains("[accepted]")
                &&activePage.Contains(definition.Title)&&activePage.Contains("Payment: "+definition.RewardDrams+" drams"));
            yield return Capture("regional-accepted-note");yield return Tap(Key.Escape);
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
            yield return InspectRegionalReceipt("regional_native_receipt_visible",definition,"regional-notes");yield return Tap(Key.Escape);
            var pageCamera=input.CameraFollow.GetComponent<Camera>();
            var pagePosition=pageCamera.transform.position;var pageRotation=pageCamera.transform.rotation;
            var pageRect=pageCamera.rect;float pageZoom=pageCamera.orthographicSize;
            var playerCell=Position();var ownerIds=input.CurrentZone.GetReadOnlyEntities().Select(e=>e.ID).OrderBy(id=>id).ToArray();
            string playerGoods=InventoryFingerprint(input.PlayerEntity),traderGoods=InventoryFingerprint(giver);
            int playerMoney=TradeSystem.GetDrams(input.PlayerEntity),traderMoney=TradeSystem.GetDrams(giver);
            yield return OpenMenu(giver);yield return SelectCommand("Chat");
            yield return SelectDialogue(choice=>choice.Actions?.Any(a=>a.Key=="StartTrade")==true,"inspect delivered trade stock");
            var tradeRows=(IList)typeof(TradeUI).GetField("_leftRows",Private).GetValue(input.TradeUI);
            bool shownIron=false;
            foreach(var row in tradeRows)
            {
                var item=(Entity)row.GetType().GetField("Item").GetValue(row);
                if(item?.BlueprintName=="ChoirIron")shownIron=true;
            }
            Check("regional_native_delivered_stock_in_trade",State()=="TradeOpen"&&input.TradeUI.IsOpen&&shownIron);
            Require(tradeRows.Count<31,"Sparse native trader stock leaves the inspected lower row empty.");
            var mainCanvas=input.ZoneRenderer.GetComponent<UnityEngine.Tilemaps.Tilemap>();
            bool cleanTrade=input.TradeUI.Tilemap==mainCanvas&&mainCanvas.GetUsedTilesCount()>0;
            for(int x=1;x<39;x++)cleanTrade&=mainCanvas.GetTile(new Vector3Int(x,9,0))==null;
            Check("fullscreen_native_trade_clean",cleanTrade&&PopupCanvasesEmpty());
            yield return Capture("regional-trade-stock");yield return Tap(Key.Escape);
            yield return Tap(Key.F);
            bool cleanFaction=State()=="FactionOpen"&&input.FactionUI.Tilemap==mainCanvas&&mainCanvas.GetUsedTilesCount()>0;
            // UI row 35 is a deliberately empty spacer between faction rows.
            for(int x=0;x<80;x++)cleanFaction&=mainCanvas.GetTile(new Vector3Int(x,9,0))==null;
            Check("fullscreen_native_faction_clean",cleanFaction&&PopupCanvasesEmpty());
            yield return Capture("faction-standings");yield return Tap(Key.Escape);yield return WaitForVoxel();
            Check("fullscreen_native_world_restored",State()=="Normal"&&!input.ZoneRenderer.Paused
                &&Position()==playerCell&&pageCamera.transform.position==pagePosition&&pageCamera.transform.rotation==pageRotation
                &&pageCamera.rect==pageRect&&Mathf.Approximately(pageCamera.orthographicSize,pageZoom)
                &&ownerIds.SequenceEqual(input.CurrentZone.GetReadOnlyEntities().Select(e=>e.ID).OrderBy(id=>id))
                &&InventoryFingerprint(input.PlayerEntity)==playerGoods&&InventoryFingerprint(giver)==traderGoods
                &&TradeSystem.GetDrams(input.PlayerEntity)==playerMoney&&TradeSystem.GetDrams(giver)==traderMoney);
            yield return Capture("fullscreen-world-restored");
            yield return Tap(Key.F12);Check("regional_native_debug_restored",!DebugInvincibility.IsEnabled(input.PlayerEntity));
        }
        // R4 native proof: examine EARNED fire clay through the real inventory
        // with native keys only (I -> Tab -> arrows -> Enter -> Examine ->
        // Enter -> Enter -> I). The inventory opens on the equipment panel, so
        // Tab is part of the ordinary path. Reflection observes panel, cursor,
        // popup rows and the modal's text; it never selects or executes.
        private IEnumerator InspectCarriedFireClay()
        {
            var inventory=input.PlayerEntity.GetPart<InventoryPart>();
            var clay=inventory.Objects.FirstOrDefault(e=>e.BlueprintName=="FireClay");
            Require(clay!=null,"Earned fire clay is carried.");
            int units=InventoryUnits("FireClay"),ticks=TurnManager.Active.TickCount;
            int diagnosed=input.PlayerEntity.GetIntProperty(MorrowfastQuests.BellDiagnosed);
            int worked=input.PlayerEntity.GetIntProperty(MorrowfastQuests.BellWorked);
            bool bellActive=StoryletPart.Current.IsQuestActive(MorrowfastQuests.BellQuestId);

            yield return Tap(Key.I);Require(State()=="InventoryOpen","Native I opens the inventory.");
            yield return Tap(Key.Tab);Require(InvField<int>("_panel")==1,"Native Tab reaches the item list.");
            int target=RowIndexOf(clay);Require(target>=0,"Fire clay has an inventory row.");
            for(int guard=0;InvField<int>("_cursorIndex")!=target;guard++)
            {Require(guard<64,"Bounded native row navigation.");yield return Tap(InvField<int>("_cursorIndex")<target?Key.DownArrow:Key.UpArrow);}
            yield return Tap(Key.Enter);
            var popup=InvField<object>("_itemActionPopup");Require(popup!=null,"Native Enter opens the item's actions.");
            var examine=((IList)Field(popup,"Actions")).Cast<object>()
                .Select((a,i)=>(label:(string)Field(a,"Label"),command:(string)Field(a,"Command"),index:i))
                .Where(a=>string.Equals(a.label,"Examine",StringComparison.OrdinalIgnoreCase)).ToList();
            Check("material_native_single_examine",examine.Count==1&&examine[0].command=="examine_material",
                "examine rows="+examine.Count+"; command="+(examine.Count>0?examine[0].command:"none"));
            Require(examine.Count>=1,"An Examine action exists.");
            for(int guard=0;(int)Field(popup,"CursorIndex")!=examine[0].index;guard++)
            {Require(guard<32,"Bounded native action navigation.");yield return Tap((int)Field(popup,"CursorIndex")<examine[0].index?Key.DownArrow:Key.UpArrow);}
            yield return Tap(Key.Enter);
            // InputHandler pops the queued announcement over the open inventory on its next tick.
            double began=Time.realtimeSinceStartupAsDouble;
            while(State()!="AnnouncementOpen"){Require(Time.realtimeSinceStartupAsDouble-began<5,"Native announcement opens over the inventory.");yield return null;}
            string text=(string)typeof(AnnouncementUI).GetField("_message",Private).GetValue(input.AnnouncementUI)??"";
            Check("material_native_description_visible",input.AnnouncementUI.IsOpen&&text.Contains("One measure")
                &&text.Contains("oven builder's guide")&&text.Contains("Morrowfast's bell")&&text.Contains("costs no material"),
                text.Replace("\n"," / "));
            yield return Capture("material-fire-clay-examine");
            yield return Tap(Key.Enter);
            Check("material_native_returns_to_row",State()=="InventoryOpen"&&InvField<object>("_itemActionPopup")==null
                &&InvField<int>("_panel")==1&&InvField<int>("_cursorIndex")==target);
            yield return Tap(Key.I);Require(State()=="Normal","Native I closes the inventory.");
            Check("material_native_examine_spends_nothing",InventoryUnits("FireClay")==units&&inventory.Objects.Contains(clay)
                &&input.PlayerEntity.GetIntProperty(MorrowfastQuests.BellDiagnosed)==diagnosed
                &&input.PlayerEntity.GetIntProperty(MorrowfastQuests.BellWorked)==worked
                &&StoryletPart.Current.IsQuestActive(MorrowfastQuests.BellQuestId)==bellActive
                &&TurnManager.Active.TickCount==ticks);
        }
        private T InvField<T>(string name)=>(T)typeof(InventoryUI).GetField(name,Private).GetValue(input.InventoryUI);
        private int RowIndexOf(Entity item)
        {
            var rows=(IList)typeof(InventoryUI).GetField("_rows",Private).GetValue(input.InventoryUI);
            for(int i=0;i<rows.Count;i++)if(ReferenceEquals(((InventoryScreenData.ItemDisplay)Field(rows[i],"Item"))?.Item,item))return i;
            return -1;
        }
        private static object Field(object owner,string name)
        {
            var type=owner.GetType();const BindingFlags any=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
            var field=type.GetField(name,any);if(field!=null)return field.GetValue(owner);
            var property=type.GetProperty(name,any);Require(property!=null,"Observed member "+type.Name+"."+name);
            return property.GetValue(owner);
        }
        // Uses fire clay earned by the two prior native deliveries, never an
        // injected material. Help only inspects; all bell work uses real menus.
        private IEnumerator InspectMaterialAndBell()
        {
            Require(!DebugInvincibility.IsEnabled(input.PlayerEntity)&&InventoryUnits("FireClay")>0,
                "Real delivery clay remains available after restoring vulnerable play.");
            yield return InspectCarriedFireClay();
            int clay=InventoryUnits("FireClay");
            var nemm=MorrowfastSceneRuntime.FindOwner(input.CurrentZone,"north-guard-west");
            Require(nemm!=null,"Native Nemm exists.");
            yield return Approach(nemm.ID);yield return OpenMenu(nemm);yield return SelectCommand("Chat");
            yield return SelectDialogue(c=>c.Target=="Bell","Nemm's bell topic");
            yield return SelectDialogue(c=>c.Actions?.Any(a=>a.Key=="MorrowfastAction"&&a.Value=="accept-bell")==true,"accept bell work");
            Check("material_native_bell_accepted",State()=="Normal"&&StoryletPart.Current.IsQuestActive(MorrowfastQuests.BellQuestId)
                &&InventoryUnits("FireClay")==clay&&input.PlayerEntity.GetIntProperty(MorrowfastQuests.BellDiagnosed)==0);
            var door=MorrowfastSceneRuntime.FindOwner(input.CurrentZone,"ropeshop-door");
            Require(door!=null,"Native rope shop door exists.");
            if(!MorrowfastSceneRuntime.IsDoorOpen(input.CurrentZone,"ropeshop-door"))
            {yield return Approach(door.ID);yield return OpenMenu(door);yield return SelectCommand(MorrowfastDoorPart.OpenCommand);}
            var cord=MorrowfastSceneRuntime.FindOwner(input.CurrentZone,"rope-reserve-coil");
            Require(cord!=null,"Native reserve cord exists.");
            yield return Approach(cord.ID);yield return OpenMenu(cord);
            int before=TurnManager.Active.TickCount;
            yield return SelectCommand(MorrowfastQuests.InspectCordCommand);
            Check("material_native_bell_diagnosed",input.PlayerEntity.GetIntProperty(MorrowfastQuests.BellDiagnosed)==1
                &&InventoryUnits("FireClay")==clay&&TurnManager.Active.TickCount>before);
            var arch=MorrowfastSceneRuntime.FindOwner(input.CurrentZone,"north-oath-arch");
            Require(arch!=null,"Native bell arch exists.");
            yield return Approach(arch.ID);yield return OpenMenu(arch);before=TurnManager.Active.TickCount;
            yield return SelectCommand(MorrowfastQuests.RepairBellCommand);
            Check("material_native_quiet_bell",InventoryUnits("FireClay")==clay-1&&arch.GetProperty("MorrowfastBellMode")=="quiet"
                &&input.PlayerEntity.GetIntProperty(MorrowfastQuests.BellWorked)==1&&TurnManager.Active.TickCount>before);
            yield return Capture("material-quiet-bell");
            yield return OpenMenu(arch);before=TurnManager.Active.TickCount;
            yield return SelectCommand(MorrowfastQuests.RepairBellCommand);
            Check("material_native_no_repeat_payment",InventoryUnits("FireClay")==clay-1&&TurnManager.Active.TickCount==before
                &&arch.GetProperty("MorrowfastBellMode")=="quiet");
            if(State()=="WorldActionMenuOpen")yield return Tap(Key.Escape);
            var residents=input.CurrentZone.GetReadOnlyEntities().Where(e=>e.GetPart<MorrowfastResidentPart>()!=null&&e.GetStatValue("Hitpoints")>0)
                .Select(e=>e.GetPart<MorrowfastResidentPart>()).ToArray();
            var oldBell=residents.ToDictionary(r=>r.ResidentId,r=>r.LastBellTurn);
            Require(residents.Count(r=>r.ResidentId=="north-guard-west"||r.ResidentId=="north-guard-east")==2&&residents.Length>2,
                "Both watchkeepers and other living townspeople are native controls.");
            yield return OpenMenu(arch);yield return SelectCommand(MorrowfastQuests.RingBellCommand);
            Check("material_native_quiet_reaches_watch",residents.All(r=>r.ResidentId=="north-guard-west"||r.ResidentId=="north-guard-east"
                ?r.LastBellTurn>oldBell[r.ResidentId]:r.LastBellTurn==oldBell[r.ResidentId])&&InventoryUnits("FireClay")==clay-1);
            nemm=MorrowfastSceneRuntime.FindOwner(input.CurrentZone,"north-guard-west");
            yield return Approach(nemm.ID);yield return OpenMenu(nemm);yield return SelectCommand("Chat");
            int drams=TradeSystem.GetDrams(input.PlayerEntity);
            yield return SelectDialogue(c=>c.Actions?.Any(a=>a.Key=="MorrowfastAction"&&a.Value=="report-bell")==true,"report quiet bell");
            Check("material_native_bell_reported",State()=="Normal"&&StoryletPart.Current.IsQuestCompleted(MorrowfastQuests.BellQuestId)
                &&InventoryUnits("FireClay")==clay-1&&TradeSystem.GetDrams(input.PlayerEntity)==drams);
        }
        private bool PopupCanvasesEmpty()=>input.ZoneRenderer.PopupFgTilemap.GetUsedTilesCount()==0
            &&input.ZoneRenderer.CenteredPopupFgTilemap.GetUsedTilesCount()==0
            &&input.ZoneRenderer.CenteredPopupBgTilemap.GetUsedTilesCount()==0;
        private static string InventoryFingerprint(Entity owner)=>string.Join("|",owner.GetPart<InventoryPart>().Objects
            .Select(e=>e.ID+":"+e.BlueprintName+":"+(e.GetPart<StackerPart>()?.StackCount??1)).OrderBy(value=>value));
        private IEnumerator InspectRegionalReceipt(string name,RegionalSituationDefinition definition,string capture)
        {
            // Read actual displayed glyphs, not the unpaginated backing buffer.
            bool visible=false;
            for(int page=0;page<10;page++)
            {
                string drawn=DrawnJournalText();
                visible=input.QuestLogUI.NotesVisible&&drawn.Contains("[completed]")
                    &&drawn.Contains("Paid: "+definition.RewardDrams+" drams")&&drawn.Contains("trade stock")
                    &&drawn.Contains("cannot pay again")&&!drawn.Contains("Bring ")&&!drawn.Contains("Read, deliver, or release");
                if(visible)break;
                int previous=input.QuestLogUI.NotesPage;yield return Tap(Key.PageDown);
                if(previous==input.QuestLogUI.NotesPage)break;
            }
            Check(name,visible);Require(visible,"The actual journal page must draw the settled outcome and follow-up.");
            yield return Capture(capture);
        }
        private string DrawnJournalText()
        {
            var glyphs=Enumerable.Range(32,95).ToDictionary(i=>CP437TilesetGenerator.GetTextTile((char)i),i=>(char)i);
            var text=new System.Text.StringBuilder();
            for(int y=0;y<45;y++)for(int x=0;x<80;x++)
            {
                var tile=input.QuestLogUI.Tilemap.GetTile(new Vector3Int(x,44-y,0)) as UnityEngine.Tilemaps.Tile;
                text.Append(tile!=null&&glyphs.TryGetValue(tile,out char c)?c:' ');
            }
            return System.Text.RegularExpressions.Regex.Replace(text.ToString(),@"\s+"," ");
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
            bool startsTrade=selected.Actions?.Any(a=>a.Key=="StartTrade")==true;
            Require(selected.Target=="End"?State()==(startsTrade?"TradeOpen":"Normal"):State()=="DialogueOpen"
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
                bounds="Actual native 1080p GameView and ordinary seed64 N bootstrap. No fixture placement, world replacement, direct quest action, direct inventory transfer, camera or reveal changes. Cardinal movement, nine border crossings, action/loot/dialogue menus, regional request/harvest/delivery, Q/Tab field notes and F5/F6 use queued native keys. F12 invincibility is explicitly enabled only for the long regional journey; combat balance is not tested. Profiling restarts at that journey. Reflection only observes live UI state. Earned fire clay is examined in the real inventory modal, then spent through Nemm/cord/arch actions to create, ring and report a quiet bell, with a native repeat-cost control and checkpoint restoration. Other two material descriptions and guide branches are EditMode-covered, not native-captured. Crops and summit water are covered by separate EditMode integration tests, not this journey. ProfilerRecorder covers a sixty-second native editor window including harness planning, screenshots, existing systems and loads; aggregate timing/allocation maxima do not isolate cue cost or establish FPS. No complete-world, subjective readability or enjoyment claim."};
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
