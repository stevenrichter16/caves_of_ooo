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
    /// uses actual generated stock in an isolated disposable new game. All movement and interactions use real keys; labelled full-reveal captures are visual comparisons only.</summary>
    public sealed partial class ReferenceGladeNativePlayer : MonoBehaviour
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        public string RunId { get; } = Guid.NewGuid().ToString("N");
        public bool Finished { get; private set; }
        public int Failures => _failures + _unexpectedErrors;
        public string ReportPath { get; private set; }
        private readonly List<string> _audit = new List<string>();
        private readonly List<string> _screenshots = new List<string>();
        private readonly List<Description> _descriptions = new List<Description>();
        private InputHandler _input;
        private ScenarioContext _context;
        private Zone _stagedZone;
        private Keyboard _keyboard;
        private InputSettings _oldSettings, _settings;
        private bool _oldBackground, _oldScenario, _cleaned, _errorsFinalized, _summaryEmitted;
        private int _failures, _unexpectedErrors, _discoveryTonicsUsed;
        private readonly List<Entity> _discoveryTonics=new List<Entity>();
        private string _ownedRoot, _fatal;
        private System.Diagnostics.Stopwatch _clock;
        private bool _measurePerformance,_profiling;
        private readonly List<float> _frameTimes=new List<float>(12000);
        private double _profileSeconds;
        private string DirectoryPath => Path.GetFullPath(Path.Combine(Application.dataPath,
            _biomeOnly ? "../Docs/Verification/DensityCompletion/SpreadBiome/NativeWalkthrough" : _combatOnly ? "../Docs/Verification/DensityCompletion/ReferenceGlade/NativeCombat" : "../Docs/Verification/DensityCompletion/ReferenceGlade/Native", RunId));

        public void Initialize(ScenarioContext context,bool measurePerformance=false,bool combatOnly=false,bool biomeOnly=false)
        {
            if (string.IsNullOrWhiteSpace(SaveGameService.SaveRootOverride))
                throw new InvalidOperationException("Completion audit requires its isolated native launcher.");
            _context = context; _ownedRoot = SaveGameService.SaveRootOverride;_measurePerformance=measurePerformance;_combatOnly=combatOnly;_biomeOnly=biomeOnly;
            if (_combatOnly)
            {
                _oldDamage = Diag.IsChannelEnabled("damage");
                _combatOldEvent = Diag.IsChannelEnabled("event");
                Diag.SetChannel("damage", true); Diag.SetChannel("event", true);
            }
            _clock = System.Diagnostics.Stopwatch.StartNew();
            _oldScenario = Diag.IsChannelEnabled("scenario"); Diag.SetChannel("scenario", true);
            _oldSettings = InputSystem.settings; _settings = Instantiate(_oldSettings);
            _settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            _settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = _settings;
            _oldBackground = Application.runInBackground; Application.runInBackground = true;
            _keyboard = InputSystem.AddDevice<Keyboard>();
            StartCoroutine(RunSafely(RunAudit()));
        }

        private IEnumerator RunAudit()
        {
            yield return new WaitForSecondsRealtime(.8f);
            _input=FindFirstObjectByType<InputHandler>();Require(_input!=null,"native input");
            var boot=(BootMenuController)Field(_input,"_bootMenuController");
            Require(boot!=null&&boot.IsActive,"isolated new-game menu");yield return Tap(Key.N);
            if(State()!="Normal"||_input.CurrentZone.ZoneID!=ReferenceGladePlan.ZoneID)
                yield return Capture("00-startup-rejected");
            Require(State()=="Normal"&&_input.CurrentZone.ZoneID==ReferenceGladePlan.ZoneID,
                "actual glade new game; state="+State()+"; zone="+_input.CurrentZone.ZoneID+"; fresh="+FindFirstObjectByType<GameBootstrap>().FreshGameZoneID+"; bootActive="+boot.IsActive);
            _stagedZone=_input.CurrentZone;
            var view=FindFirstObjectByType<SpawnRing3DPresenter>();
            Require(view!=null&&view.IsReady&&view.PresentationVisible,"native glade presentation: "+view?.Failure);
            Check("ordinary_player_and_native_zone",!DevMode.Enabled&&!_input.PlayerEntity.HasPart<BitLockerPart>()&&_input.PlayerEntity.GetStat("Hitpoints").Max==40);
            yield return Capture("01-gameplay-arrival");
            var renderer=FindFirstObjectByType<GameBootstrap>().ZoneRenderer;
            Require(!renderer.RevealEntire3DZone&&!view.FullReveal,"ordinary scene visibility");
            if (_combatOnly) { yield return RunCombatAudit(); yield break; }
            if (_biomeOnly) { yield return RunBiomeAudit(); yield break; }
            renderer.RevealEntire3DZone=true;view.FullReveal=true;view.Refresh(null);yield return Capture("02-composition-full-reveal");
            yield return CaptureWorld("03-world-only-full-reveal");
            renderer.RevealEntire3DZone=false;view.FullReveal=false;view.Refresh(null);
            var before=Cell();yield return Tap(Key.D);Check("keyboard_moves_one_native_cell",Cell().X==before.X+1&&Cell().Y==before.Y);
            var dagger=_input.PlayerEntity.GetPart<InventoryPart>().Objects.First(e=>e.BlueprintName=="Dagger");
            yield return ItemAction(dagger,"equip_auto");if(State()=="InventoryOpen")yield return Tap(Key.I);
            Require(State()=="Normal","equipment menu closes");
            Check("starting_dagger_equipped_by_keyboard",dagger.GetPart<PhysicsPart>().Equipped==_input.PlayerEntity);
            _discoveryTonics.AddRange(_input.PlayerEntity.GetPart<InventoryPart>().Objects.Where(e=>e.BlueprintName=="HealingTonic"));
            yield return WalkDiscoveries();
            var chest=_stagedZone.GetAllEntities().Single(e=>e.BlueprintName=="Chest");
            yield return WalkTo(50,8);yield return WorldAction(chest,"OpenContainer");
            Require(State()=="PickupOpen","native chest loot popup");yield return Capture("04-chest-loot");
            int contents=chest.GetPart<ContainerPart>().Contents.Count;yield return Tap(Key.Tab);
            if(State()!="Normal")yield return Tap(Key.Escape);
            Check("native_chest_contents_taken",contents==3&&chest.GetPart<ContainerPart>().Contents.Count==0&&_input.PlayerEntity.GetPart<InventoryPart>().Objects.Any(e=>e.BlueprintName=="Torch"));
            yield return WalkTo(52,5);before=Cell();yield return Tap(Key.D);
            Check("gray_wall_blocks_keyboard_movement",Cell().X==before.X&&Cell().Y==before.Y);yield return Capture("05-solid-wall");
            var vein=_stagedZone.GetAllEntities().First(e=>e.BlueprintName=="GlowQuartzVein");
            string veinID=vein.ID;int initialVeins=_stagedZone.GetAllEntities().Count(e=>e.BlueprintName=="GlowQuartzVein");
            yield return WalkTo(32,10);yield return WorldAction(vein,"Harvest");
            Check("actual_lit_seam_harvested",vein.GetPart<HarvestablePart>().Harvested&&_input.PlayerEntity.GetPart<InventoryPart>().Objects.Any(e=>e.BlueprintName=="GlowQuartz"));
            if(State()=="LookMode")yield return Tap(Key.Escape);Require(State()=="Normal","harvest closes to gameplay");
            yield return Capture("06-harvested-green-seam");
            var saveCell=Cell();int sx=saveCell.X,sy=saveCell.Y;
            var checkpoint=SaveGameService.GetSaveInfo("Quick");Require(checkpoint!=null,"initial native checkpoint metadata");
            string gameID=checkpoint.GameID,savePath=Path.Combine(_ownedRoot,gameID,"Quick.sav.gz");
            string previousHash=CheckpointHash(savePath);long serial=MessageLog.NextSerialValue;
            int savedTick=_input.TurnManager.TickCount,savedEnergy=_input.TurnManager.GetEnergy(_input.PlayerEntity);
            int savedHP=_input.PlayerEntity.GetStatValue("Hitpoints");string savedID=_input.PlayerEntity.ID;
            yield return Tap(Key.F5);string savedHash=CheckpointHash(savePath);
            Require(MessageLog.GetLast()=="Game saved."&&MessageLog.NextSerialValue>serial&&savedHash!=previousHash
                &&SaveGameService.GetSaveInfo("Quick")?.GameID==gameID
                &&SaveGameService.GetSaveInfo("Quick")?.ActiveZoneID==ReferenceGladePlan.ZoneID,"F5 writes the current native checkpoint");
            yield return Tap(Key.D);
            Require(Cell().X==sx+1&&Cell().Y==sy&&CheckpointHash(savePath)==savedHash,"real post-save keyboard movement without rewriting checkpoint");
            var oldPlayer=_input.PlayerEntity;yield return Tap(Key.F6);double loadStarted=Time.realtimeSinceStartupAsDouble;
            while(ReferenceEquals(oldPlayer,_input.PlayerEntity))
            {Require(Time.realtimeSinceStartupAsDouble-loadStarted<8,"F6 replaces the saved player graph");yield return null;}
            _descriptions.Add(new Description{subject="Native checkpoint proof",source="F5, one verified keyboard step, F6",
                text="Checkpoint "+savedHash+"; restored player "+savedID+", tick "+savedTick+", energy "+savedEnergy+", HP "+savedHP+"."});
            Check("native_save_load_restores_position_and_depletion",Cell().X==sx&&Cell().Y==sy
                &&_input.PlayerEntity.ID==savedID&&_input.PlayerEntity.GetStatValue("Hitpoints")==savedHP
                &&_input.TurnManager.TickCount==savedTick&&_input.TurnManager.GetEnergy(_input.PlayerEntity)==savedEnergy
                &&CheckpointHash(savePath)==savedHash&&_input.CurrentZone.GetAllEntities().Single(e=>e.BlueprintName=="Chest").GetPart<ContainerPart>().Contents.Count==0
                &&!_input.CurrentZone.GetAllEntities().Any(e=>e.ID==veinID)
                &&_input.CurrentZone.GetAllEntities().Count(e=>e.BlueprintName=="GlowQuartzVein")==initialVeins-1
                &&_input.PlayerEntity.GetPart<InventoryPart>().Objects.Any(e=>e.BlueprintName=="GlowQuartz"));
            Check("discoveries_keep_spent_and_moved_state_after_native_load",
                _input.CurrentZone.GetCell(44,10).Objects.Single(e=>e.BlueprintName=="RipeCropRow").GetPart<FieldHarvestPart>().Harvested
                &&_input.CurrentZone.GetCell(36,9).Objects.Single(e=>e.BlueprintName=="SpreadDrawPoint").GetPart<LiquidPoolPart>().Volume==0
                &&_input.CurrentZone.GetCell(27,19).Objects.Any(e=>e.BlueprintName=="FallenBeam")
                &&_input.PlayerEntity.GetPart<InventoryPart>().Objects.Any(e=>e.BlueprintName=="Waterskin"&&e.GetPart<WaterskinPart>().Charges==3));
            Check("observed_live_hostile_attrition",_input.CurrentZone.GetAllEntities().Count(e=>e.BlueprintName.StartsWith("Marlback")&&e.HasTag("Creature"))<3
                ||_input.CurrentZone.GetAllEntities().Any(e=>e.BlueprintName=="MarlbackCorpse")
                ||_input.CurrentZone.GetAllEntities().Any(e=>e.BlueprintName.StartsWith("Marlback")&&e.GetStat("Hitpoints")!=null&&e.GetStat("Hitpoints").Value<e.GetStat("Hitpoints").Max));
            yield return WalkTo(79,12);yield return Tap(Key.D);
            Check("native_east_exit",_input.CurrentZone.ZoneID=="Overworld.12.10.0");yield return Tap(Key.A);
            Check("return_does_not_refill_glade_chest",_input.CurrentZone.ZoneID==ReferenceGladePlan.ZoneID&&_input.CurrentZone.GetAllEntities().Single(e=>e.BlueprintName=="Chest").GetPart<ContainerPart>().Contents.Count==0);
            yield return Capture("07-returned-from-world");
            Check("normal_input_after_return",State()=="Normal"&&!FindFirstObjectByType<SpawnRing3DPresenter>().FullReveal);
            if(_measurePerformance)
            {
                yield return WalkTo(40,12);BeginNativeMarkerProfile();double start=Time.realtimeSinceStartupAsDouble;_profiling=true;
                while(Time.realtimeSinceStartupAsDouble-start<60)
                {yield return Tap(Key.D);yield return Tap(Key.A);}
                _profileSeconds=Time.realtimeSinceStartupAsDouble-start;_profiling=false;EndNativeMarkerProfile();
                Check("sixty_seconds_native_movement_profile",_profileSeconds>=60&&_frameTimes.Count>100&&State()=="Normal"&&!CombatSystem.IsDeathHandled(_input.PlayerEntity));
                yield return Capture("08-profile-complete");
            }
        }
        private static string CheckpointHash(string path)
        {
            using(var hash=System.Security.Cryptography.SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();
        }
        private void Update(){if(_profiling&&_frameTimes.Count<60000)_frameTimes.Add(Time.unscaledDeltaTime*1000);}
        private Cell Cell()=>_input.CurrentZone.GetEntityCell(_input.PlayerEntity);
        private IEnumerator WalkDiscoveries()
        {
            // Exercise the actual new-game owners with normal keys. The existing
            // isolated launcher and later F5/F6 check own setup and restoration.
            var zone=_input.CurrentZone;var inventory=_input.PlayerEntity.GetPart<InventoryPart>();
            var vessel=zone.GetCell(37,10).Objects.Single(e=>e.BlueprintName=="Waterskin");
            yield return WalkTo(37,10);yield return Tap(Key.G);
            if(State()=="PickupOpen")yield return Tap(Key.Tab);
            if(State()!="Normal")yield return Tap(Key.Escape);
            Check("native_pickup_of_basin_vessel",inventory.Objects.Contains(vessel)&&vessel.GetPart<WaterskinPart>().Charges==0);
            yield return WalkTo(36,10);yield return ItemAction(vessel,"FillWaterskin");
            if(State()=="InventoryOpen")yield return Tap(Key.I);
            Check("native_draw_spends_exact_basin",vessel.GetPart<WaterskinPart>().Charges==3
                &&zone.GetCell(36,9).Objects.Single(e=>e.BlueprintName=="SpreadDrawPoint").GetPart<LiquidPoolPart>().Volume==0);
            yield return Capture("discovery-01-reed-bank-drawn");
            var row=zone.GetCell(44,10).Objects.Single(e=>e.BlueprintName=="RipeCropRow");
            yield return WalkTo(43,10);yield return WorldAction(row,"Harvest");
            Check("native_grain_leaves_spent_stubble",row.GetPart<FieldHarvestPart>().Harvested);
            if(State()=="LookMode")yield return Tap(Key.Escape);
            var food=inventory.Objects.Single(e=>e.BlueprintName=="Emberwheat");string cooked=food.GetPart<CookablePart>().Into;
            int before=inventory.Objects.Where(e=>e.BlueprintName==cooked).Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);
            yield return WalkTo(42,9);yield return ItemAction(food,"Cook");
            if(State()=="InventoryOpen")yield return Tap(Key.I);
            Check("native_shelter_cooks_real_grain",!inventory.Objects.Contains(food)
                &&inventory.Objects.Where(e=>e.BlueprintName==cooked).Sum(e=>e.GetPart<StackerPart>()?.StackCount??1)==before+1);
            yield return Capture("discovery-02-working-shelter");
            var beam=zone.GetCell(26,19).Objects.Single(e=>e.BlueprintName=="FallenBeam");
            yield return WalkTo(27,19);yield return WorldAction(beam,"HaulObject");
            if(State()=="LookMode")yield return Tap(Key.Escape);
            yield return Tap(Key.D);
            yield return WorldAction(beam,"ReleaseHaul");
            if(State()=="LookMode")yield return Tap(Key.Escape);
            Check("native_pull_opens_ruin_shortcut",zone.GetEntityPosition(beam)==(27,19)
                &&!zone.GetCell(26,19).BlocksMovement(_input.PlayerEntity)&&!DragSystem.IsDragging(_input.PlayerEntity));
            yield return Capture("discovery-03-opened-ruin-shortcut");
        }
        private IEnumerator WalkTo(int x,int y)
        {
            for(int step=0;step<160;step++)
            {
                if(!_biomeOnly&&!_combatOnly)yield return CombatDismissEarnedAdvancement();
                Require(State()=="Normal"&&!CombatSystem.IsDeathHandled(_input.PlayerEntity),"live ordinary actor while walking; state="+State()+"; HP="+_input.PlayerEntity.GetStatValue("Hitpoints"));
                var inventory=_input.PlayerEntity.GetPart<InventoryPart>();var hp=_input.PlayerEntity.GetStat("Hitpoints");
                var tonic=_discoveryTonics.FirstOrDefault(e=>inventory.Objects.Contains(e));
                if(!_biomeOnly&&!_combatOnly&&_discoveryTonicsUsed<2&&hp.Value*3<=hp.Max*2&&tonic!=null)
                {
                    int units=_discoveryTonics.Where(e=>inventory.Objects.Contains(e)).Sum(e=>e.GetPart<StackerPart>()?.StackCount??1);
                    int beforeHP=hp.Value;yield return ItemAction(tonic,"ApplyTonic");
                    if(State()=="InventoryOpen")yield return Tap(Key.I);
                    Require(_discoveryTonics.Where(e=>inventory.Objects.Contains(e)).Sum(e=>e.GetPart<StackerPart>()?.StackCount??1)==units-1,"one original carried tonic consumed");
                    _discoveryTonicsUsed++;
                    _descriptions.Add(new Description{subject="Native route healing",source=tonic.ID,text="Original starting tonic consumed via inventory; HP "+beforeHP+" -> "+hp.Value,units=1});
                    continue;
                }
                var c=Cell();if(c.X==x&&c.Y==y)yield break;
                (int dx,int dy) d;
                if(_biomeOnly)
                {
                    int defensiveCastsBefore=_biomeDefensiveCalmUses;
                    yield return BiomeCalmPursuingThreat();
                    if(_biomeDefensiveCalmUses>defensiveCastsBefore)continue;
                    var safe=BiomeSafePath(_input.CurrentZone,c,_input.CurrentZone.GetCell(x,y));
                    _biomeSteps.Add("safe-route "+_input.CurrentZone.ZoneID+"@"+c.X+","+c.Y+" -> "+x+","+y+" steps="+(safe==null?-1:safe.Count));
                    if(safe==null||safe.Count==0)BiomeRouteDiagnostic(_input.CurrentZone,_input.CurrentZone.GetCell(x,y));
                    Require(safe!=null&&safe.Count>0,"finite route using unchanged biome footprint/hazard/threat predicates");
                    d=safe[0];
                    Require(BiomeSafe(_input.CurrentZone,_input.CurrentZone.GetCell(c.X+d.dx,c.Y+d.dy),2),"next actual footprint remains safe");
                }
                else
                {
                    var path=FindPath.Search(_input.CurrentZone,c.X,c.Y,x,y,actor:_input.PlayerEntity);
                    Require(path.Usable&&path.Steps.Count>0,"walkable native route");d=path.Steps[0];
                }
                yield return Tap(Direction(d.dx,d.dy));
                if(_biomeOnly)Require(Cell().X==c.X+d.dx&&Cell().Y==c.Y+d.dy,"actual native walking step reaches selected safe footprint");
            }
            throw new InvalidOperationException("Finite native walking route exceeded160 steps.");
        }
        private static Key Direction(int x,int y)
        {
            if(x==0)return y<0?Key.W:Key.S;if(y==0)return x<0?Key.A:Key.D;
            return y<0?(x<0?Key.Numpad7:Key.Numpad9):(x<0?Key.Numpad1:Key.Numpad3);
        }
        private IEnumerator WorldAction(Entity target,string command)
        {
            var from=Cell();var to=SpatialQuery.ClosestCell(_input.CurrentZone,target,from.X,from.Y);
            Require(Math.Max(Math.Abs(from.X-to.X),Math.Abs(from.Y-to.Y))<=1,"walked into actual reach");
            yield return Tap(Key.L);Require(State()=="LookMode","native look");
            if(to.X!=from.X)yield return Tap(to.X>from.X?Key.D:Key.A);
            if(to.Y!=from.Y)yield return Tap(to.Y>from.Y?Key.S:Key.W);
            yield return Tap(Key.Enter);
            Require(State()=="WorldActionMenuOpen"&&ReferenceEquals(_input.WorldActionMenuUI.SelectedTarget,target),"native target selection");
            var actions=(List<InventoryAction>)Field(_input.WorldActionMenuUI,"_actions");int index=actions.FindIndex(a=>a.Command==command);
            Require(index>=0,"native action "+command);
            yield return Tap((Key)Enum.Parse(typeof(Key),MenuShortcutMap.Key(MenuShortcutMap.ForActions(actions)[index]).ToString()));
        }
        private IEnumerator CaptureWorld(string name)
        {
            yield return new WaitForEndOfFrame();var view=FindFirstObjectByType<SpawnRing3DPresenter>();var target=view.WorldCamera.targetTexture;
            Require(target!=null,"actual rendered world target");var old=RenderTexture.active;Texture2D pixels=null;
            try
            {
                RenderTexture.active=target;pixels=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);
                pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();
                string path=Path.Combine(DirectoryPath,name+".png");File.WriteAllBytes(path,pixels.EncodeToPNG());_screenshots.Add(path);
            }
            finally{RenderTexture.active=old;if(pixels!=null)Destroy(pixels);}
        }

        private IEnumerator ItemAction(Entity item, string command)
        {
            yield return Tap(Key.I); Require(State() == "InventoryOpen", "native I opens inventory");
            yield return Tap(Key.Tab); Require(InvField<int>("_panel") == 1, "native item list");
            int row = RowIndex(item); Require(row >= 0, "owned item row");
            for (int step = 0; InvField<int>("_cursorIndex") != row; step++)
            { Require(step < 80, "bounded item navigation"); yield return Tap(InvField<int>("_cursorIndex") < row ? Key.DownArrow : Key.UpArrow); }
            yield return Tap(Key.Enter);
            object popup = InvField<object>("_itemActionPopup"); Require(popup != null, "native item actions");
            var actions = ((IList)Field(popup, "Actions")).Cast<object>().ToArray();
            int action = Array.FindIndex(actions, a => (string)Field(a, "Command") == command);
            Require(action >= 0 && action < 9, "native action shortcut " + command + "; available="+string.Join(",",actions.Select(a=>(string)Field(a,"Command"))));
            yield return Tap((Key)Enum.Parse(typeof(Key), ((char)('A' + action)).ToString()));
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
            Require(_clock.Elapsed.TotalSeconds < 240, "finite native content-completion deadline");
            double began = Time.realtimeSinceStartupAsDouble;
            while (_input != null && Time.time - (float)Field(_input, "_lastMoveTime") < _input.MoveRepeatDelay)
            { Require(Time.realtimeSinceStartupAsDouble - began < 3, "input rate gate reopens"); yield return null; }
            if (_biomeOnly) { Require(_input.PlayerEntity.GetStatValue("Hitpoints") > 10 && !CombatSystem.IsDeathHandled(_input.PlayerEntity), "biome ordinary actor safety stop"); _biomeSteps.Add("KEY " + string.Join("+",keys) + " before " + BiomeState()); }
            _keyboard.MakeCurrent();
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(keys)); yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); yield return null;
            yield return new WaitForSecondsRealtime(.13f);
            if (_biomeOnly) { _biomeSteps.Add("after " + BiomeState()); if(!_profiling)WriteReport(); }
        }
        private IEnumerator Capture(string name)
        {
            yield return new WaitForSecondsRealtime(.15f); yield return new WaitForEndOfFrame();
            Directory.CreateDirectory(DirectoryPath);
            string path = Path.Combine(DirectoryPath, name + ".png");
            DensityNativeScreenshot.CaptureToFile(path);
            Require(File.Exists(path) && new FileInfo(path).Length > 0, "screenshot " + name);
            _screenshots.Add(path);
        }
        private static void Require(bool condition, string reason)
        { if (!condition) throw new InvalidOperationException("Density completion precondition: " + reason); }
        private void Check(string name, bool passed)
        {
            if (!passed)
            {
                _failures++;
                if(_biomeOnly) _biomeSteps.Add("FAILED CHECK "+name+"; zone="+_input.CurrentZone.ZoneID
                    +"; cell="+Cell().X+","+Cell().Y+"; state="+State()+"; hp="+_input.PlayerEntity.GetStatValue("Hitpoints")
                    +"; tick="+_input.TurnManager.TickCount+"; energy="+_input.TurnManager.GetEnergy(_input.PlayerEntity)
                    +"; gear="+CombatGear(_input.PlayerEntity));
            }
            _audit.Add((passed ? "PASS " : "FAIL ") + name);
            Diag.Record("scenario", "ReferenceGladeNativeCase", payload: new { runId = RunId, name, passed });
        }
        private IEnumerator RunSafely(IEnumerator steps)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(steps);
            while (stack.Count > 0)
            {
                bool moved = false; object current = null; Exception error = null;
                try { moved = stack.Peek().MoveNext(); if (moved) current = stack.Peek().Current; }
                catch (Exception caught) { error = caught; }
                if (error != null)
                {
                    _fatal = error.ToString();
                    // Dispose nested observation windows so their finally blocks
                    // preserve real death/refusal evidence before cleanup.
                    while (stack.Count > 0)
                    {
                        try { (stack.Pop() as IDisposable)?.Dispose(); }
                        catch (Exception disposal) { _fatal += "\nObserver cleanup: " + disposal; }
                    }
                    Check("native_precondition_failed", false); Debug.LogError("[ReferenceGladeNative] " + error); break;
                }
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
            Diag.Record("scenario", "ReferenceGladeNativeSummary", payload: new
            { runId = RunId, cases = _audit.Count, failures = Failures, complete = Complete, errorsFinalized = _errorsFinalized, screenshots = _screenshots.Count });
        }
        private bool Complete => Finished && _errorsFinalized && Failures == 0
            && (_biomeOnly ? BiomeComplete : _combatOnly ? CombatComplete : _audit.Count == (_measurePerformance?18:17) && _screenshots.Count >= 10);
        private void WriteReport()
        {
            Directory.CreateDirectory(DirectoryPath); ReportPath = Path.Combine(DirectoryPath, "report.json");
            File.WriteAllText(ReportPath, JsonUtility.ToJson(new Report
            {
                runId = RunId, cases = _audit.Count, failures = Failures, unexpectedErrors = _unexpectedErrors,
                complete = Complete, errorsFinalized = _errorsFinalized, seconds = _clock?.Elapsed.TotalSeconds ?? 0,
                mode=_biomeOnly?"native-spread-biome-route":_combatOnly?"native-keyboard-combat":_measurePerformance?"movement-profile":"visual-content-route",
                combatSteps=_combatSteps.ToArray(),combatState=CombatState(),biomeSteps=_biomeSteps.ToArray(),biomeShortcuts=_biomeShortcuts,biomeDefensiveCalmUses=_biomeDefensiveCalmUses,
                profileSeconds=_profileSeconds,profileFrames=_frameTimes.Count,profileFrameMeanMs=_frameTimes.Count==0?0:_frameTimes.Average(),
                profileFrameP95Ms=_frameTimes.Count==0?0:_frameTimes.OrderBy(x=>x).ElementAt((int)((_frameTimes.Count-1)*.95)),
                zone = _stagedZone?.ZoneID, fatal = _fatal, audit = _audit.ToArray(), screenshots = _screenshots.ToArray(), descriptions = _descriptions.ToArray(),
                canVerify = _biomeOnly ? "Real isolated seed64 starting graph; original dagger drop/pickup; native F5, movement, F6 replacement and unchanged saved bytes; real keyboard Spread border exit/return; actual finite harvest, one paid action and saved depletion; generated village door and committed lair stairs; current-owner submitted profile/body observation; foreign-biome negative profile; finite60-second editor keyboard movement sample." : _combatOnly ? "Actual authored northern encounter; ordinary starting actor and kit; real movement and attack keys; exact diagnostic actor/target/cause correlation, attempted retaliation, exact death attribution and corpse/original-owned-drop provenance, and native animation/frame observation. No direct attack, pose or damage calls." : "Native glade new game; ordinary HP and starting kit; keyboard walking, gear, real chest loot, blocking, finite quartz/grain harvest, vessel pickup and basin draw, cooking, beam hauling, F5/F6 persistence and world exit/return. Actual rendered screenshots.",
                cannotVerify = _biomeOnly ? "Four explicitly logged actor travel shortcuts cover actual generated POI, lair, foreign control and original return. Source selection is script-selected and first-floor generation is preflighted; no natural acquisition, discovery, combat balance or all-chunk visual coverage claim. At most eight actual original Calm casts may defend against a visible pursuing threat on an exact legal ray before native movement/harvest; exact owner, payment, cooldown and live pacification are recorded. No Rime, cooldown waits or scripted combat. Ordinary scheduler remains active; no grants, reseeding or forced models. Native screenshots require visual inspection. Frame times describe this editor run, not standalone build performance or allocations. Separate census and normal randomized N/Continue gates remain necessary." : _combatOnly ? "One fixed authored encounter and finite script-selected keyboard route. At most two original starting healing tonics may be used at or below two-thirds HP through native inventory keys. One ready Rime Grip may be used only after real retaliation. Their actual costs/effects are recorded. The prior dagger-only death remains a separate failed receipt. No combat-balance, natural discovery, animation quality or enjoyment claim. Native pose observation and saved frames still need visual assessment; unrelated NPC activity remains enabled." : "Routes are script-selected through real keyboard input. At most two original starting tonic units may be used at or below two-thirds HP through native inventory keys; actual consumption is recorded. Naturally earned advancement announcements are dismissed without changing gains. Full-reveal composition frames are labelled and reveal is restored before play. No natural discovery, player-solo combat balance or enjoyment claim. Screenshots need visual inspection. Optional frame sampling measures this editor session during real keyboard movement; it is not a player-build benchmark or allocation claim."
            }, true));
            Debug.Log("[ReferenceGladeNative] report=" + ReportPath + " failures=" + Failures);
        }
        private void Cleanup()
        {
            if (_cleaned) return; _cleaned = true;_profiling=false;DisposeNativeMarkerRecorders();
            if (_combatOnly) { Diag.SetChannel("damage", _oldDamage); Diag.SetChannel("event", _combatOldEvent); }
            if (_keyboard != null) { InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); InputSystem.RemoveDevice(_keyboard); }
            if (_oldSettings != null) InputSystem.settings = _oldSettings;
            if (_settings != null) Destroy(_settings);
            Application.runInBackground = _oldBackground;

        }
        private void OnDestroy()
        {
            if (!Finished && _clock != null) { _fatal = "Play stopped before completion."; Check("native_interrupted", false); Finish(); }
            Cleanup(); EmitSummary(); Diag.SetChannel("scenario", _oldScenario);
        }
        [Serializable] private sealed class Description
        { public string subject, source, text; public int units; }
        [Serializable] private sealed class Report
        {
            public string runId, zone, fatal, canVerify, cannotVerify, mode, combatState;
            public string[] combatSteps,biomeSteps;
            public int biomeShortcuts,biomeDefensiveCalmUses;
            public string[] audit, screenshots;
            public Description[] descriptions;
            public int cases, failures, unexpectedErrors;
            public bool complete, errorsFinalized;
            public double seconds,profileSeconds;
            public int profileFrames;
            public float profileFrameMeanMs,profileFrameP95Ms;
        }
    }
}
