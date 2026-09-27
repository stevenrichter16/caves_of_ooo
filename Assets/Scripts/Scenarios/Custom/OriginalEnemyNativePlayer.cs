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
    /// <summary>Explicit staged art demonstration in an isolated ordinary new
    /// game. Factory owners/loadouts and actual native rendering; scripted
    /// movement/pose hooks do not claim player input, AI or combat balance.</summary>
    public sealed class OriginalEnemyNativePlayer : MonoBehaviour
    {
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        private static readonly string[] Blueprints={"MarlbackScrabbler","MarlbackGleaner","MarlbackTunnelguard","MarlbackWallkeeper","MarlbackBreacher","GroveLanternMoth"};
        private static readonly string[] Models={"ring-snapjaw","ring-marlback-gleaner","ring-marlback-tunnelguard","ring-marlback-wallkeeper","ring-snapjaw-warlord","ring-grove-lantern-moth"};
        public string RunId{get;}=Guid.NewGuid().ToString("N");
        public bool Finished{get;private set;}
        public int Failures=>_failures+_unexpectedErrors;
        public string ReportPath{get;private set;}
        private readonly List<string> _audit=new List<string>(),_screenshots=new List<string>();
        private readonly List<ActorRow> _actors=new List<ActorRow>();
        private readonly List<Entity> _owned=new List<Entity>();
        private InputHandler _input;private ScenarioContext _context;private Zone _zone;
        private SpawnRing3DPresenter _presenter;private CameraFollow _camera;
        private Keyboard _keyboard,_oldKeyboard;private InputSettings _oldSettings,_settings;
        private bool _oldBackground,_oldScenario,_cleaned,_errorsFinalized,_summaryEmitted,_workloadComplete;
        private bool _oldReveal,_oldRendererReveal,_oldOverride,_presentationCaptured;
        private Vector2Int _oldOverrideCell;private float _oldZoom;private int _failures,_unexpectedErrors,_startTick;
        private string _ownedRoot,_fatal;private string[] _before;
        private System.Diagnostics.Stopwatch _clock;
        private string DirectoryPath=>Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/DensityCompletion/OriginalEnemies/Native",RunId));
        public void Initialize(ScenarioContext context)
        {
            if(string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride))throw new InvalidOperationException("Original art audit requires its isolated launcher.");
            _context=context;_ownedRoot=SaveGameService.SaveRootOverride;_clock=System.Diagnostics.Stopwatch.StartNew();
            _oldScenario=Diag.IsChannelEnabled("scenario");Diag.SetChannel("scenario",true);
            _oldKeyboard=Keyboard.current;_oldSettings=InputSystem.settings;_settings=Instantiate(_oldSettings);_settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            _settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings=_settings;_oldBackground=Application.runInBackground;Application.runInBackground=true;
            _keyboard=InputSystem.AddDevice<Keyboard>();StartCoroutine(RunSafely(RunAudit()));
        }
        private IEnumerator RunAudit()
        {
            yield return new WaitForSecondsRealtime(.8f);_input=FindFirstObjectByType<InputHandler>();Require(_input!=null,"actual input");
            var boot=(BootMenuController)Field(_input,"_bootMenuController");Require(boot!=null&&boot.IsActive,"native new-game menu");
            yield return Tap(Key.N);Require(State()=="Normal"&&!boot.IsActive,"native N ordinary new game");
            _zone=_input.CurrentZone;_presenter=FindFirstObjectByType<SpawnRing3DPresenter>();_camera=_input.CameraFollow;
            Diag.Record("scenario","OriginalEnemyNativeStart",payload:new { zoneId=_zone?.ZoneID,
                managedSpread=SpreadPresentationScope.IsActive(_zone),referenceGlade=ReferenceGladePlan.IsActive(_zone),
                ready=_presenter?.IsReady==true,visible=_presenter?.PresentationVisible==true });
            Require(_presenter!=null&&_presenter.IsReady&&_presenter.PresentationVisible&&SpreadPresentationScope.IsActive(_zone),"actual managed Spread start and current native view");
            Require(_camera!=null&&_input.ZoneRenderer!=null,"native camera and renderer");
            Require(ReferenceEquals(LoadoutPart.Factory,_context.Factory),"actual bootstrap loadout factory");
            Check("isolated_ordinary_player",!DevMode.Enabled&&!_input.PlayerEntity.HasPart<BitLockerPart>()&&SaveGameService.SaveRootOverride==_ownedRoot);
            _startTick=_input.TurnManager.TickCount;_before=Snapshot();
            _oldReveal=_presenter.FullReveal;_oldRendererReveal=_input.ZoneRenderer.RevealEntire3DZone;
            _oldZoom=_camera.GameplayZoomMultiplier;_oldOverride=_camera.HasOverrideTarget;_oldOverrideCell=_camera.OverrideZoneCell;_presentationCaptured=true;
            _presenter.FullReveal=true;_input.ZoneRenderer.RevealEntire3DZone=true;_camera.GameplayZoomMultiplier=.5f;
            for(int index=0;index<Blueprints.Length;index++)
            {
                Require(_clock.Elapsed.TotalSeconds<180,"finite art audit");
                var owner=_context.Factory.CreateEntity(Blueprints[index]);Require(owner!=null,"actual factory "+Blueprints[index]);_owned.Add(owner);
                var stage=Stage(owner);Require(_zone.AddEntity(owner,stage.x,stage.y),"declared owner placement");
                // No registration or turn simulation: these are explicit staged
                // visual subjects, not a claim about population/AI behaviour.
                Require(!_input.TurnManager.IsRegistered(owner),"staged owner is not an AI turn workload");
                _camera.SetOverrideTargetCell(stage.x,stage.y);_camera.SnapToPlayer();Refresh();yield return new WaitForSecondsRealtime(.3f);
                Require(_presenter.TryGetEntityView(owner,out var root,out var model),"native owner view");
                var skins=root.GetComponentsInChildren<SkinnedMeshRenderer>(true);var animator=root.GetComponentInChildren<Animator>();
                var row=new ActorRow{blueprint=Blueprints[index],model=model,owner=owner.ID,stageX=stage.x,stageY=stage.y};_actors.Add(row);
                Check(row.blueprint+"_correct_real_body",model==Models[index]&&_presenter.IsRenderedEntity(owner)&&skins.Length>0&&animator!=null);
                Require(animator!=null&&skins.Length>0,"actual adopted rig");
                Check(row.blueprint+"_adopted_voxel_mesh",UsesAdoptedBody(Models[index],owner,root));
                Check(row.blueprint+"_idle_walk_states",animator.HasState(0,Animator.StringToHash("Idle"))&&animator.HasState(0,Animator.StringToHash("Walk")));
                row.gear=Gear(owner,root);Check(row.blueprint+"_authored_loadout_present",AuthoredLoadout(owner));
                Check(row.blueprint+"_exact_owned_equipped_style",row.gear.All(g=>g.owned&&g.rendered&&g.approvedStyle));
                Check(row.blueprint+"_pickable",Pick(owner,root));
                row.height=Bounds(root).size.y;row.width=Bounds(root).size.x;
                yield return Capture(row.blueprint+"-01-idle",false);yield return Capture(row.blueprint+"-02-world-idle",true);
                var pose=Pose(skins);Require(_zone.MoveEntity(owner,stage.x+1,stage.y),"staged one-cell move");
                EntityVisualHooks.EmitMoved(owner,_zone,stage.x,stage.y,stage.x+1,stage.y,false);
                yield return new WaitForSecondsRealtime(.04f);row.walkMotion=Motion(pose,Pose(skins));
                Check(row.blueprint+"_walk_bones_change",row.walkMotion>.001f&&InState(animator,"Walk"));
                yield return Capture(row.blueprint+"-03-scripted-walk",true);yield return new WaitForSecondsRealtime(.25f);
                Check(row.blueprint+"_walk_reaches_actual_cell",Vector3.Distance(root.transform.position,Village3DProjection.CellCentre(stage.x+1,stage.y))<.02f);
                if(index<5)
                {
                    Require(animator.HasState(0,Animator.StringToHash("Attack")),"Marlback Attack state");pose=Pose(skins);
                    owner.GetPart<RenderPart>().VisualFacing=EntityVisualFacing.South;
                    EntityVisualHooks.EmitAttack(owner,_input.PlayerEntity,_zone);yield return new WaitForSecondsRealtime(.09f);
                    row.attackMotion=Motion(pose,Pose(skins));Check(row.blueprint+"_attack_pose_changes_bones",row.attackMotion>.001f&&InState(animator,"Attack"));
                    yield return Capture(row.blueprint+"-04-scripted-attack",true);yield return new WaitForSecondsRealtime(.3f);
                }
                else Check("passive_moth_not_given_attack_or_light",!owner.HasPart<LightSourcePart>()&&owner.GetPart<BrainPart>()?.Passive==true);
                var render=owner.GetPart<RenderPart>();render.Visible=false;Refresh();yield return null;Check(row.blueprint+"_hidden_owner_not_drawn",!_presenter.IsRenderedEntity(owner));
                render.Visible=true;Refresh();yield return null;Check(row.blueprint+"_visible_owner_recovers",_presenter.IsRenderedEntity(owner));
                Require(_zone.RemoveEntity(owner),"remove exact staged owner");Refresh();yield return null;Check(row.blueprint+"_removed_owner_gone",!_presenter.IsAuthoredEntity(owner)&&!_presenter.TryGetEntityView(owner,out _,out _));
            }
            Check("all_six_original_roles_observed",_actors.Select(a=>a.blueprint).SequenceEqual(Blueprints));
            Check("staging_preserves_existing_world_and_turn",Snapshot().SequenceEqual(_before)&&_input.TurnManager.TickCount==_startTick);
            _workloadComplete=true;
        }
        private (int x,int y) Stage(Entity owner)
        {
            var player=_zone.GetEntityCell(_input.PlayerEntity);
            // The first live audit allowed a two-cell diagonal/northern lane:
            // projected bodies overlapped the untouched player. Keep both pose
            // cells three to five columns to one side, with a clear margin.
            foreach(int dy in new[]{0,-1,1,-2,2})
            foreach(int dx in new[]{3,-4,4,-5})
            {
                int x=player.X+dx,y=player.Y+dy;bool clear=true;
                for(int yy=y-1;yy<=y+1&&clear;yy++)for(int xx=x-1;xx<=x+2;xx++)
                {
                    var cell=_zone.GetCell(xx,yy);
                    if(cell==null||cell.BlocksMovement(owner)||cell.Objects.Any(e=>
                        !e.HasTag("Terrain")||e.HasTag("Creature")||e.HasTag("Player")||e.HasTag("Trap")
                        ||e.HasPart<LiquidPoolPart>()||e.HasPart<GasPoolPart>()||e.HasPart<ThermalPart>()))
                    {clear=false;break;}
                }
                if(clear)return(x,y);
            }
            throw new InvalidOperationException("No unobstructed native art lane three to five cells beside the real player.");
        }
        private void Refresh(){ZoneRenderHooks.MarkFullDirty("OriginalEnemyNative.StagedArt");_presenter.Refresh(null);}
        private static bool InState(Animator a,string state)=>a.GetCurrentAnimatorStateInfo(0).IsName(state)||(a.IsInTransition(0)&&a.GetNextAnimatorStateInfo(0).IsName(state));
        private static Bounds Bounds(GameObject root){var r=root.GetComponentsInChildren<SkinnedMeshRenderer>(true);var b=r[0].bounds;foreach(var skin in r.Skip(1))b.Encapsulate(skin.bounds);return b;}
        private static PoseRow[] Pose(IEnumerable<SkinnedMeshRenderer> skins)=>skins.SelectMany(s=>s.bones).Where(b=>b!=null).Distinct().Select(b=>new PoseRow{position=b.localPosition,rotation=b.localRotation}).ToArray();
        private static float Motion(PoseRow[] a,PoseRow[] b){if(a.Length!=b.Length)return 0;float max=0;for(int i=0;i<a.Length;i++)max=Mathf.Max(max,Vector3.Distance(a[i].position,b[i].position)+Quaternion.Angle(a[i].rotation,b[i].rotation));return max;}
        private bool UsesAdoptedBody(string modelId,Entity owner,GameObject root)
        {
            if(_presenter==null||owner==null||root==null
                ||!_presenter.TryGetEntityView(owner,out var currentRoot,out var currentModel)
                ||!ReferenceEquals(root,currentRoot)||currentModel!=modelId)return false;
            var library=Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath);
            var source=library?.FindModel(modelId)?.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if(source==null||source.Length!=1||source[0].sharedMesh==null)return false;
            Mesh expected;
            if(_presenter.TryGetApprovedStyle(owner,out var proof))expected=proof.ExpectedMesh;
            else
            {
                // The original moth's fine-baked body has not yet joined the
                // new Spread palette. Preserve its exact prior source proof;
                // this exception is not an approved C15 style claim.
                if(modelId!="ring-grove-lantern-moth"||proof.Failure!="unmapped-style-source")return false;
                var catalog=Resources.Load<VoxelWorldMeshCatalog>(VoxelWorldMeshCatalog.ResourcePath);
                expected=catalog?.Resolve(source[0].sharedMesh);
                if(expected==source[0].sharedMesh)return false;
            }
            if(expected==null)return false;
            var gearRenderers=new HashSet<Renderer>();
            foreach(var item in owner.GetPart<InventoryPart>()?.GetAllEquipped().Distinct()??Enumerable.Empty<Entity>())
            {
                if(!_presenter.TryGetApprovedEquipmentStyle(owner,item,out _)
                    ||!_presenter.TryGetEquipmentView(owner,item,out var equipmentRoot)
                    ||equipmentRoot==null||!equipmentRoot.transform.IsChildOf(root.transform))return false;
                foreach(var renderer in equipmentRoot.GetComponentsInChildren<Renderer>(true))gearRenderers.Add(renderer);
            }
            // One exact body remains after excluding only real, currently
            // approved owned equipment. Unknown skins or copied gear stay in
            // this set and cause refusal rather than hiding as an extra part.
            var remaining=root.GetComponentsInChildren<Renderer>(true).Where(r=>!gearRenderers.Contains(r)).ToArray();
            if(remaining.Length!=1||!(remaining[0]is SkinnedMeshRenderer body))return false;
            return ReferenceEquals(body.sharedMesh,expected)
                &&expected.bindposeCount==source[0].sharedMesh.bindposeCount
                &&body.bones.Length==expected.bindposeCount;
        }
        private static bool AuthoredLoadout(Entity owner)
        {
            var gear=owner.GetPart<InventoryPart>()?.GetAllEquipped().Distinct().ToArray()??Array.Empty<Entity>();
            var weapons=gear.Where(e=>e.HasPart<MeleeWeaponPart>()).Select(e=>e.BlueprintName).ToArray();
            if(owner.BlueprintName=="GroveLanternMoth")return gear.Length==0;
            if(weapons.Length!=1)return false;
            string[] allowed;
            switch(owner.BlueprintName)
            {
                case "MarlbackScrabbler":allowed=new[]{"Dagger","Hatchet","Cudgel"};break;
                case "MarlbackGleaner":allowed=new[]{"Dagger","ShortSword"};break;
                case "MarlbackTunnelguard":allowed=new[]{"Spear"};break;
                case "MarlbackWallkeeper":allowed=new[]{"LongSword"};break;
                case "MarlbackBreacher":allowed=new[]{"BreacherCleaver"};break;
                default:return false;
            }
            if(!allowed.Contains(weapons[0]))return false;
            var ids=gear.Select(e=>e.BlueprintName).ToArray();
            if(owner.BlueprintName=="MarlbackWallkeeper")return new[]{"LeatherArmor","LeatherCap"}.All(ids.Contains);
            if(owner.BlueprintName=="MarlbackBreacher")return new[]{"LeatherArmor","IronHelmet","IronshodBoots"}.All(ids.Contains);
            return true;
        }
        private GearRow[] Gear(Entity owner,GameObject actor)
        {
            var inv=owner.GetPart<InventoryPart>();if(inv==null)return Array.Empty<GearRow>();
            return inv.GetAllEquipped().Distinct().Select(item=>
            {bool rendered=_presenter.TryGetEquipmentView(owner,item,out var model);var fallback=_presenter.EquipmentFallbacks.FirstOrDefault(f=>ReferenceEquals(f.Actor,owner)&&ReferenceEquals(f.Item,item));
             return new GearRow{blueprint=item.BlueprintName,id=item.ID,owned=ReferenceEquals(item.GetPart<PhysicsPart>()?.Equipped,owner),rendered=rendered&&model!=null&&model.transform.IsChildOf(actor.transform),approvedStyle=_presenter.TryGetApprovedEquipmentStyle(owner,item,out _),fallback=fallback?.Reason};}).ToArray();
        }
        private bool Pick(Entity owner,GameObject root)
        {
            Physics.SyncTransforms();var b=Bounds(root);
            for(int ix=0;ix<9;ix++)for(int iz=0;iz<9;iz++)
            {var p=new Vector2(Mathf.Lerp(b.min.x,b.max.x,(ix+.5f)/9),Mathf.Lerp(b.min.z,b.max.z,(iz+.5f)/9));if(_presenter.TryPickWorld(p,out var found,out _,out _)&&ReferenceEquals(found,owner))return true;}
            return false;
        }
        private string[] Snapshot()=>_zone.GetAllEntities().Where(e=>!_owned.Contains(e)).Select(e=>e.ID+"|"+e.BlueprintName+"|"+_zone.GetEntityPosition(e)+"|"+e.GetStatValue("Hitpoints")+"|"+string.Join(",",e.GetPart<InventoryPart>()?.GetAllEquipped().Select(i=>i.ID).OrderBy(s=>s)??Enumerable.Empty<string>())).OrderBy(s=>s).ToArray();
        private IEnumerator Capture(string name,bool worldOnly)
        {
            yield return new WaitForEndOfFrame();Directory.CreateDirectory(DirectoryPath);string path=Path.Combine(DirectoryPath,name+".png");
            if(!worldOnly)DensityNativeScreenshot.CaptureToFile(path);
            else
            {
                var target=_presenter.WorldCamera.targetTexture;Require(target!=null,"native world render texture");var previous=RenderTexture.active;Texture2D pixels=null;
                try{RenderTexture.active=target;pixels=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());}
                finally{RenderTexture.active=previous;if(pixels!=null)Destroy(pixels);}
            }
            Require(File.Exists(path)&&new FileInfo(path).Length>0,"rendered screenshot");_screenshots.Add(path);
        }
        private IEnumerator Tap(Key key){_keyboard.MakeCurrent();InputSystem.QueueStateEvent(_keyboard,new KeyboardState(key));yield return null;InputSystem.QueueStateEvent(_keyboard,new KeyboardState());yield return null;yield return new WaitForSecondsRealtime(.2f);}
        private string State()=>Field(_input,"_inputState").ToString();
        private static object Field(object owner,string name)=>owner.GetType().GetField(name,Private|BindingFlags.Public)?.GetValue(owner);
        private static void Require(bool condition,string reason){if(!condition)throw new InvalidOperationException("Original art audit precondition: "+reason);}
        private void Check(string name,bool pass){if(!pass)_failures++;_audit.Add((pass?"PASS ":"FAIL ")+name);Diag.Record("scenario","OriginalEnemyNativeCase",payload:new{runId=RunId,name,passed=pass});}
        private IEnumerator RunSafely(IEnumerator steps)
        {
            var stack=new Stack<IEnumerator>();stack.Push(steps);
            while(stack.Count>0)
            {
                bool moved=false;object current=null;Exception failure=null;
                try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}catch(Exception e){failure=e;}
                if(failure!=null){_fatal=failure.ToString();Check("native_precondition_failed",false);Debug.LogError("[OriginalEnemyNative] "+failure);break;}
                if(!moved){(stack.Pop()as IDisposable)?.Dispose();continue;}
                if(current is IEnumerator nested){stack.Push(nested);continue;}yield return current;
            }
            while(stack.Count>0)(stack.Pop()as IDisposable)?.Dispose();Finish();
        }
        public void SetUnexpectedErrors(int errors){_unexpectedErrors=errors;_errorsFinalized=true;WriteReport();EmitSummary();}
        public void Abort(string reason){if(Finished)return;StopAllCoroutines();_fatal=reason;Check("native_aborted",false);Finish();}
        private void Finish(){Cleanup();Finished=true;WriteReport();}
        private bool Complete=>Finished&&_errorsFinalized&&Failures==0&&_workloadComplete&&_actors.Count==6&&_screenshots.Count==23;
        private void WriteReport()
        {
            Directory.CreateDirectory(DirectoryPath);ReportPath=Path.Combine(DirectoryPath,"report.json");
            File.WriteAllText(ReportPath,JsonUtility.ToJson(new Report{runId=RunId,zone=_zone?.ZoneID,saveRoot=_ownedRoot,fatal=_fatal,complete=Complete,errorsFinalized=_errorsFinalized,
                cases=_audit.Count,failures=Failures,unexpectedErrors=_unexpectedErrors,seconds=_clock?.Elapsed.TotalSeconds??0,audit=_audit.ToArray(),screenshots=_screenshots.ToArray(),actors=_actors.ToArray(),
                canVerify="Six exact factory creature identities/loadouts in the actual managed Spread new-game start (zone recorded); exact adopted body identity and every current equipped item style, picking, scripted movement and presentation Attack hook/bone motion, hidden/removal controls, unchanged preexisting entity identities/positions/HP/equipped IDs and turn. Actual PNG output.",
                cannotVerify="Explicit staged art demo: one factory owner at a time, not naturally acquired or spawned population; staged owners are not scheduled for AI. Zone placement plus explicit Moved/Attack presentation hooks are not ordinary keyboard combat, AI, damage or balance. Full reveal and doubled inspection camera zoom are temporary and restored. The passive moth is not given an attack or light; its exact prior fine-baked body proof does not claim the new Spread palette. Native backend log review, human deformation/appearance acceptance, and ordinary encounter balance remain separate."},true));
        }
        private void EmitSummary(){if(_summaryEmitted)return;_summaryEmitted=true;Diag.Record("scenario","OriginalEnemyNativeSummary",payload:new{runId=RunId,complete=Complete,cases=_audit.Count,failures=Failures,screenshots=_screenshots.Count});}
        private void Cleanup()
        {
            if(_cleaned||_clock==null)return;_cleaned=true;
            try
            {
                if(_zone!=null)foreach(var owner in _owned)if(_zone.GetEntityCell(owner)!=null)_zone.RemoveEntity(owner);
                if(_presentationCaptured)
                {
                    if(_presenter!=null){_presenter.FullReveal=_oldReveal;_presenter.Refresh(null);}
                    if(_input?.ZoneRenderer!=null)_input.ZoneRenderer.RevealEntire3DZone=_oldRendererReveal;
                    if(_camera!=null){_camera.GameplayZoomMultiplier=_oldZoom;if(_oldOverride)_camera.SetOverrideTargetCell(_oldOverrideCell.x,_oldOverrideCell.y);else _camera.ClearOverrideTarget();_camera.SnapToPlayer();}
                }
            }
            finally
            {
                try{if(_keyboard!=null&&_keyboard.added){InputSystem.QueueStateEvent(_keyboard,new KeyboardState());InputSystem.RemoveDevice(_keyboard);}}
                finally
                {
                    if(_oldKeyboard!=null&&_oldKeyboard.added)_oldKeyboard.MakeCurrent();
                    if(_oldSettings!=null)InputSystem.settings=_oldSettings;if(_settings!=null)Destroy(_settings);Application.runInBackground=_oldBackground;
                }
            }
        }
        private void OnDestroy(){if(!Finished&&_clock!=null){_fatal="Play stopped before completion.";Check("native_interrupted",false);Finish();}Cleanup();if(_clock!=null){EmitSummary();Diag.SetChannel("scenario",_oldScenario);}}
        private struct PoseRow{public Vector3 position;public Quaternion rotation;}
        [Serializable]private sealed class GearRow{public string blueprint,id,fallback;public bool owned,rendered,approvedStyle;}
        [Serializable]private sealed class ActorRow{public string blueprint,model,owner;public int stageX,stageY;public float height,width,walkMotion,attackMotion;public GearRow[] gear;}
        [Serializable]private sealed class Report{public string runId,zone,saveRoot,fatal,canVerify,cannotVerify;public bool complete,errorsFinalized;public int cases,failures,unexpectedErrors;public double seconds;public string[] audit,screenshots;public ActorRow[] actors;}
    }
}
