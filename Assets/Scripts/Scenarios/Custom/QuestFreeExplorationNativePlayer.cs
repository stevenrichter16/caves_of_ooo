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
    /// <summary>Finite instrumented walking observer. Reads current generated geometry for routing;
    /// never transfers, grants, opens reports, generates ahead or pretends to measure human decisions.</summary>
    public sealed class QuestFreeExplorationNativePlayer:MonoBehaviour
    {
        const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        const int MaxInputs=600;const double MaxSeconds=180;
        static readonly (int x,int y)[] Directions={(1,0),(0,1),(-1,0),(0,-1)};
        public string RunId{get;}=Guid.NewGuid().ToString("N");
        public bool Finished{get;private set;}public int Failures=>_errors+(_fatal==null?0:1);public string ReportPath{get;private set;}
        InputHandler _input;Keyboard _keyboard,_oldKeyboard;InputSettings _settings,_oldSettings;bool _background,_ownsSettings,_cleaned,_complete,_errorsFinalized;
        string _fatal,_stop,_cohortJson,_cohortHash;int _seed,_inputs,_transitions,_errors;List<string> _walk;readonly HashSet<string> _visited=new HashSet<string>();
        readonly List<object> _actions=new List<object>(),_entries=new List<object>(),_decisions=new List<object>();readonly List<string> _images=new List<string>();
        System.Diagnostics.Stopwatch _clock;string DirectoryPath=>Path.GetFullPath(Path.Combine(Application.dataPath,"../Docs/Verification/QuestFreeExploration/E0/NativeWalk",RunId));
        Entity Player=>_input.PlayerEntity;Zone Zone=>_input.CurrentZone;OverworldZoneManager Manager=>_input.ZoneManager as OverworldZoneManager;Cell At=>Zone.GetEntityCell(Player);string State=>Field(_input,"_inputState").ToString();
        public void Initialize(ScenarioContext context,string cohort)
        {
            if(string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride))throw new InvalidOperationException("Isolated launcher required before bootstrap.");
            _cohortJson=cohort;var data=JObject.Parse(cohort);_seed=(int)data["seed"];_walk=data["walk"].Values<string>().ToList();
            using(var sha=System.Security.Cryptography.SHA256.Create())_cohortHash=BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(cohort))).Replace("-","").ToLowerInvariant();
            if(_walk.Count!=39||data["nodes"].Count()!=20||_walk.Distinct().Count()!=20)throw new InvalidOperationException("Frozen complete BFS20 tree walk required.");
            for(int i=1;i<_walk.Count;i++){var a=WorldMap.FromZoneID(_walk[i-1]);var b=WorldMap.FromZoneID(_walk[i]);if(a.z!=0||b.z!=0||Math.Abs(a.x-b.x)+Math.Abs(a.y-b.y)!=1)throw new InvalidOperationException("Frozen walk must use cardinal surface neighbors.");}
            _clock=System.Diagnostics.Stopwatch.StartNew();_oldSettings=InputSystem.settings;_background=Application.runInBackground;_ownsSettings=true;_settings=Instantiate(_oldSettings);_settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            _settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings=_settings;_background=Application.runInBackground;Application.runInBackground=true;_oldKeyboard=Keyboard.current;_keyboard=InputSystem.AddDevice<Keyboard>();StartCoroutine(RunSafely(Run()));
        }
        IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(.6f);_input=FindFirstObjectByType<InputHandler>();Require(_input!=null,"ordinary bootstrap input");
            Require(((BootMenuController)Field(_input,"_bootMenuController"))?.IsActive==true,"ordinary new-game menu");yield return Tap(Key.N);yield return Settled();
            Require(Manager.WorldSeed==_seed&&Zone.ZoneID==_walk[0]&&!DevMode.Enabled,"actual ordinary seed/start; no retargeting frozen cohort");
            Require(Player.GetStatValue("Hitpoints")==40&&TradeSystem.GetDrams(Player)==50&&Player.GetPart<InventoryPart>().Objects.Any(e=>e.BlueprintName=="Dagger"),"ordinary starter state without grants");
            Enter("ordinary-start");yield return Capture("00-ordinary-start");
            for(int i=1;i<_walk.Count;i++)
            {
                if(StopBudget())yield break;var current=WorldMap.FromZoneID(Zone.ZoneID);var target=WorldMap.FromZoneID(_walk[i]);int dx=target.x-current.x,dy=target.y-current.y;
                Require(Math.Abs(dx)+Math.Abs(dy)==1,"current actor follows exact frozen walk");int replans=0;
                while(!Border(At,dx,dy))
                {
                    if(StopBudget())yield break;var route=LocalPath(dx,dy);
                    if(route==null){_stop="no-current-safe-path-to-next-frozen-border";_decisions.Add(new{zone=Zone.ZoneID,next=_walk[i],choice="stop",reason=_stop});yield break;}
                    bool changed=false;
                    foreach(var point in route)
                    {
                        if(StopBudget())yield break;if(!Passable(Zone,Zone.GetCell(point.x,point.y),Threats())){changed=true;break;}
                        var old=At;var z=Zone;yield return Move(point.x-old.X,point.y-old.Y,"walk");
                        if(Zone!=z||At.X!=point.x||At.Y!=point.y){_stop="native-step-did-not-reach-planned-cell";yield break;}
                    }
                    if(changed&&++replans>6){_stop="moving-owner-replan-cap";yield break;}
                }
                if(StopBudget())yield break;
                // The neighboring graph is not queried: only the ordinary outward key can generate/enter it.
                string before=Zone.ZoneID;yield return Move(dx,dy,"ordinary-boundary-crossing");
                if(Zone.ZoneID!=_walk[i]){_stop="native-boundary-refused-or-unexpected-destination";_decisions.Add(new{from=before,expected=_walk[i],actual=Zone.ZoneID,choice="stop"});yield break;}
                _transitions++;bool first=_visited.Add(Zone.ZoneID);Enter(first?"first-entry":"connector-revisit");if(first)yield return Capture(_visited.Count.ToString("00")+"-"+Zone.ZoneID);
            }
            _complete=true;_stop="frozen-tree-walk-complete";
        }
        bool StopBudget()
        {
            if(_clock.Elapsed.TotalSeconds>=MaxSeconds)_stop="elapsed-cap";else if(_inputs>=MaxInputs)_stop="input-cap";else if(Player.GetStatValue("Hitpoints")<=10||CombatSystem.IsDeathHandled(Player))_stop="ordinary-hp-safety-stop";
            return _stop!=null;
        }
        IEnumerator Move(int dx,int dy,string kind)
        {
            var before=new{zone=Zone.ZoneID,x=At.X,y=At.Y,hp=Player.GetStatValue("Hitpoints"),tick=_input.TurnManager.TickCount,energy=_input.TurnManager.GetEnergy(Player)};
            _inputs++;yield return Tap(dx>0?Key.D:dx<0?Key.A:dy>0?Key.S:Key.W);yield return Settled();
            _actions.Add(new{sequence=_inputs,kind,before,after=new{zone=Zone.ZoneID,x=At.X,y=At.Y,hp=Player.GetStatValue("Hitpoints"),tick=_input.TurnManager.TickCount,energy=_input.TurnManager.GetEnergy(Player)}});
            if(_inputs%10==0)WriteReport();
        }
        List<(int x,int y)> LocalPath(int dx,int dy)
        {
            var zone=Zone;var threats=Threats();var start=(At.X,At.Y);var queue=new Queue<(int x,int y)>();var seen=new HashSet<(int x,int y)>{start};var parent=new Dictionary<(int x,int y),(int x,int y)>();queue.Enqueue(start);(int x,int y)? found=null;int admitted=0;
            while(queue.Count>0){var at=queue.Dequeue();if(Border(zone.GetCell(at.x,at.y),dx,dy)){found=at;break;}foreach(var d in Directions){var n=(x:at.x+d.x,y:at.y+d.y);if(!zone.InBounds(n.x,n.y)||!seen.Add(n)||!Passable(zone,zone.GetCell(n.x,n.y),threats))continue;admitted++;parent[n]=at;queue.Enqueue(n);}}
            _decisions.Add(new{zone=zone.ZoneID,x=At.X,y=At.Y,borderDx=dx,borderDy=dy,evaluated=seen.Count,admitted,threats=threats.Select(e=>e.ID).ToArray(),choice=found.HasValue?"shortest-current-safe-cardinal-path":"stop",knowledge="full current-zone geometry including unrevealed cells; instrumented routing, not player awareness"});
            if(!found.HasValue)return null;var result=new List<(int x,int y)>();var cur=found.Value;while(cur!=start){result.Add(cur);cur=parent[cur];}result.Reverse();return result;
        }
        static bool Border(Cell at,int dx,int dy)=>dx<0?at.X==0:dx>0?at.X==Zone.Width-1:dy<0?at.Y==0:at.Y==Zone.Height-1;
        Entity[] Threats()=>Zone.GetReadOnlyEntities().Where(e=>e!=Player&&e.HasTag("Creature")&&e.GetStatValue("Hitpoints")>0&&!CombatSystem.IsDeathHandled(e)&&(FactionManager.IsHostile(e,Player)||FactionManager.IsHostile(Player,e)||e.GetPart<BrainPart>()?.Target==Player)).ToArray();
        bool Passable(Zone zone,Cell cell,Entity[] threats)
        {
            if(cell==null||!zone.CanPlaceFootprint(Player,cell.X,cell.Y))return false;
            foreach(var c in zone.GetOccupiedCells(Player,cell.X,cell.Y))
            {
                if(c.Occupants.Any(e=>e!=Player&&(e.HasTag("Creature")||e.HasPart<TriggerOnStepPart>()||e.HasPart<LiquidPoolPart>()||e.HasPart<GasPoolPart>()||e.HasEffect<BurningEffect>()||e.GetPart<ThermalPart>()?.IsAflame==true)))return false;
                var s=zone.TileState.Get(c.X,c.Y);if(s!=null&&(s.Heat>0||s.Cold>0||s.Charge>0||!string.IsNullOrEmpty(s.Cloud)||s.Coatings.Count>0))return false;
                if(threats.Any(e=>SpatialQuery.DistanceToCell(zone,e,c.X,c.Y)<=1))return false;
            }return true;
        }
        void Enter(string phase)
        {
            _visited.Add(Zone.ZoneID);var visible=Zone.GetReadOnlyEntities().Where(e=>e!=Player&&e.GetPart<RenderPart>()?.Visible==true&&Zone.GetEntityCell(e)?.IsVisible==true).ToArray();
            var relevant=visible.Where(e=>e.HasTag("Creature")||e.HasPart<ContainerPart>()||e.HasPart<HarvestablePart>()||e.HasPart<FieldHarvestPart>()||e.HasPart<LiquidPoolPart>()||e.GetPart<PhysicsPart>()?.Takeable==true).Select(e=>new{id=e.ID,blueprint=e.BlueprintName,parts=e.Parts.Select(p=>p.Name).ToArray(),x=Zone.GetEntityPosition(e).x,y=Zone.GetEntityPosition(e).y,hp=e.GetStatValue("Hitpoints"),hostile=FactionManager.IsHostile(e,Player),containerLocked=e.GetPart<ContainerPart>()?.IsLocked,containerUnits=e.GetPart<ContainerPart>()?.Contents.Sum(x=>x.GetPart<StackerPart>()?.StackCount??1),harvested=e.GetPart<FieldHarvestPart>()?.Harvested??e.GetPart<HarvestablePart>()?.Harvested}).ToArray();
            _entries.Add(new{phase,zone=Zone.ZoneID,x=At.X,y=At.Y,player=Player.ID,hp=Player.GetStatValue("Hitpoints"),tick=_input.TurnManager.TickCount,energy=_input.TurnManager.GetEnergy(Player),visibleSourceOpportunities=relevant,drams=TradeSystem.GetDrams(Player),cached=Manager.CachedZones.Keys.OrderBy(x=>x).ToArray(),boundary="FOV+declared render observation only; no claim of pixel occlusion, human noticing, reachable action, acquisition or choice to ignore."});WriteReport();
        }
        IEnumerator Settled(){double began=Time.realtimeSinceStartupAsDouble;while(State!="Normal"||_input.ZoneRenderer?.WorldFx?.HasBlockingFx==true){Require(Time.realtimeSinceStartupAsDouble-began<8,"finite input/FX settling");yield return null;}yield return null;}
        IEnumerator Tap(params Key[] keys)
        {
            double began=Time.realtimeSinceStartupAsDouble;while(_input!=null&&Time.time-(float)Field(_input,"_lastMoveTime")<_input.MoveRepeatDelay){Require(Time.realtimeSinceStartupAsDouble-began<3,"input repeat gate");yield return null;}
            _keyboard.MakeCurrent();InputSystem.QueueStateEvent(_keyboard,new KeyboardState(keys));yield return null;InputSystem.QueueStateEvent(_keyboard,new KeyboardState());yield return null;yield return new WaitForSecondsRealtime(.13f);
        }
        IEnumerator Capture(string label){yield return new WaitForEndOfFrame();Directory.CreateDirectory(DirectoryPath);string path=Path.Combine(DirectoryPath,label+".png");DensityNativeScreenshot.CaptureToFile(path);Require(File.Exists(path)&&new FileInfo(path).Length>0,"actual screenshot saved");_images.Add(path);WriteReport();}
        static object Field(object owner,string name)=>owner.GetType().GetField(name,Private|BindingFlags.Public).GetValue(owner);
        static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
        IEnumerator RunSafely(IEnumerator routine)
        {
            var stack=new Stack<IEnumerator>();stack.Push(routine);
            try{while(stack.Count>0){bool moved=false;object next=null;Exception error=null;try{moved=stack.Peek().MoveNext();if(moved)next=stack.Peek().Current;}catch(Exception e){error=e;}if(error!=null){_fatal=error.ToString();break;}if(!moved){(stack.Pop() as IDisposable)?.Dispose();continue;}if(next is IEnumerator child){stack.Push(child);continue;}yield return next;}}
            finally{while(stack.Count>0)(stack.Pop() as IDisposable)?.Dispose();Finish();}
        }
        public void Abort(string reason){if(Finished)return;_fatal=reason;StopAllCoroutines();Finish();}
        public void SetUnexpectedErrors(int errors){_errors=errors;_errorsFinalized=true;WriteReport();}
        void Finish(){Cleanup();Finished=true;WriteReport();}
        void Cleanup(){if(_cleaned)return;_cleaned=true;if(_keyboard!=null){InputSystem.QueueStateEvent(_keyboard,new KeyboardState());InputSystem.RemoveDevice(_keyboard);}if(_oldKeyboard!=null&&_oldKeyboard.added)_oldKeyboard.MakeCurrent();if(_oldSettings!=null)InputSystem.settings=_oldSettings;if(_settings!=null)Destroy(_settings);if(_ownsSettings)Application.runInBackground=_background;}
        void WriteReport(){Directory.CreateDirectory(DirectoryPath);ReportPath=Path.Combine(DirectoryPath,"report.json");File.WriteAllText(ReportPath,JsonConvert.SerializeObject(new{runId=RunId,seed=_seed,finished=Finished,complete=Finished&&_errorsFinalized&&_complete&&Failures==0,failures=Failures,stop=_stop,fatal=_fatal,seconds=_clock?.Elapsed.TotalSeconds??0,cohortSha256=_cohortHash,frozenCohort=JObject.Parse(_cohortJson),inputs=_inputs,transitions=_transitions,visited=_visited.OrderBy(x=>x).ToArray(),actions=_actions,entries=_entries,instrumentedPathChoices=_decisions,screenshots=_images,performedRewardActions=0,humanAwarenessMeasured=false,boundary="Ordinary N and cardinal local/boundary inputs only. No worldmap/transfers/grants/reports/quests/AI changes. Full current-zone geometry guides conservative paths; this is instrumented motor traversal, not unaided decision-making. Zero reward actions is observer policy, not evidence players ignore content. Partial/refused walks remain partial. No candidate-only walk is a matched baseline.",settings=new{screenWidth=Screen.width,screenHeight=Screen.height,vSync=QualitySettings.vSyncCount,targetFrameRate=Application.targetFrameRate,lowDetail=Village3DSettings.LowDetail}},Formatting.Indented));}
        void OnDestroy(){if(!Finished&&_clock!=null){_fatal="Play interrupted";Finish();}Cleanup();}
    }
}
