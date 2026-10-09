using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Core.Inventory.Commands;
using CavesOfOoo.Rendering;
using CavesOfOoo.Skills;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Isolated native inventory/turn witness for situational item uses.
    /// Finite controlled supplies; preserves the real campaign and scene.</summary>
    public sealed class ItemUtility35NativePlayer : MonoBehaviour
    {
        const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        readonly string runId = Guid.NewGuid().ToString("N");
        readonly List<string> checks = new List<string>(), screenshots = new List<string>();
        readonly List<object> observations = new List<object>();
        static readonly string[] Required = { "ordinary_classic_start", "companion_healing", "companion_meal", "conductive_film", "wick_film",
            "freeze_water", "warm_frozen_companion", "grease_ground", "ignite_grease", "fan_transient_gas", "finite_light", "light_native_model",
            "brace_one_push", "second_push_moves" };
        InputHandler input; Keyboard keyboard, oldKeyboard; InputSettings settings, oldSettings;
        CameraFollow framedCamera; float oldCameraZoom; bool cameraFramed;
        bool oldBackground, cleaned, errorsFinalized; int failures, errors; string fatal;
        Entity companion, healing, meal, brine, pith, lichen, fruit, oil, moss, fan, quartz, boots;
        System.Diagnostics.Stopwatch clock;
        public bool Finished { get; private set; }
        public int Failures => failures + errors + (Finished && !Required.All(n => checks.Contains("PASS " + n)) ? 1 : 0);
        Entity Player => input.PlayerEntity;
        string State => Field(input, "_inputState").ToString();
        string DirectoryPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/Verification/ItemUtility35/Native", runId));
        public void Initialize(ScenarioContext context)
        {
            Require(!string.IsNullOrEmpty(SaveGameService.SaveRootOverride), "isolated save launcher");
            clock = System.Diagnostics.Stopwatch.StartNew(); oldSettings = InputSystem.settings; settings = Instantiate(oldSettings);
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = settings; oldBackground = Application.runInBackground; Application.runInBackground = true;
            oldKeyboard = Keyboard.current; keyboard = InputSystem.AddDevice<Keyboard>(); StartCoroutine(RunSafely(Run()));
        }
        IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(.8f); input = FindFirstObjectByType<InputHandler>(); Require(input != null, "ordinary bootstrap");
            Require(((BootMenuController)Field(input, "_bootMenuController")).IsActive, "new-game menu");
            yield return Tap(Key.N); var build = (StartingBuildMenuController)Field(input, "_buildMenuController");
            Require(build.IsOpen, "build picker"); int classic = build.Model.Options.ToList().FindIndex(b => b.Id == "classic");
            Require(classic >= 0 && classic < 9, "Classic authored build"); yield return Tap((Key)Enum.Parse(typeof(Key), "Digit" + (classic + 1)));
            yield return Tap(Key.Enter); yield return new WaitForSecondsRealtime(.3f); Require(State == "Normal", "normal gameplay");
            Check("ordinary_classic_start", Player.GetProperty(StartingBuildService.PropertyName) == "classic" && !DebugInvincibility.IsEnabled(Player));
            BuildFixture(); yield return Capture("01-finite-controlled-supplies");

            int hp = companion.GetStatValue("Hitpoints");
            yield return Paid(healing, "TreatCompanion|", c => c.Split('|')[4] == Uri.EscapeDataString(companion.ID), "02-treat-companion");
            Check("companion_healing", companion.GetStatValue("Hitpoints") > hp && !Player.GetPart<InventoryPart>().Objects.Contains(healing));
            yield return Paid(meal, "ShareMeal|", c => c.Split('|')[4] == Uri.EscapeDataString(companion.ID), "03-share-preparation");
            Check("companion_meal", companion.GetStatValue("HeatResistance") == 20 && !Player.HasEffect<PreparedMealEffect>());
            yield return Paid(brine, "MaterialField|Film|", c => Tile(c,21,13) && c.Split('|')[9] == "brine", "04-conductive-route");
            Check("conductive_film", input.CurrentZone.TileState.HasCoating(21,13,"brine"));
            yield return Paid(pith, "MaterialField|Wick|", c => Tile(c,21,13) && c.Split('|')[9] == "brine", "05-wick-route");
            Check("wick_film", !input.CurrentZone.TileState.HasCoating(21,13,"brine"));
            input.CurrentZone.TileState.WriteCoating(21,12,"water",8);
            yield return Paid(lichen, "MaterialField|Freeze|", c => Tile(c,21,12), "06-freeze-wet-ground");
            Check("freeze_water", input.CurrentZone.TileState.HasCoating(21,12,"ice") && companion.HasEffect<FrozenEffect>());
            float cold = companion.GetEffect<FrozenEffect>()?.Cold ?? 0;
            yield return Paid(fruit, "MaterialField|Warm|", c => Tile(c,21,12), "07-warm-companion");
            Check("warm_frozen_companion", cold > 0 && (companion.GetEffect<FrozenEffect>()?.Cold ?? 0) < cold);
            yield return Paid(oil, "MaterialField|Film|", c => Tile(c,19,13) && c.Split('|')[9] == "oil", "08-oil-shortcut");
            Check("grease_ground", input.CurrentZone.TileState.HasCoating(19,13,"oil"));
            yield return Paid(moss, "MaterialField|Kindle|", c => Tile(c,19,13) && c.Split('|')[9] == "oil", "09-kindle-oil");
            Check("ignite_grease", !input.CurrentZone.TileState.HasCoating(19,13,"oil") && input.CurrentZone.TileState.HasResidue(19,13,"embers"));
            var gas = new Entity { ID = "utility-gas-"+runId, BlueprintName = "NativeTransientGas" };
            gas.AddPart(new PhysicsPart { Solid = false, Takeable = false });
            gas.AddPart(new RenderPart()); gas.SetTag("Gas");
            gas.AddPart(new GasPoolPart { GasId = "poison-vapor", Density = 15, Stable = false });
            Require(input.CurrentZone.AddEntity(gas,19,12), "controlled gas");
            yield return Paid(fan, "FanGas|", c => c.Split('|')[4] == "19" && c.Split('|')[5] == "12", "10-fan-gas");
            Check("fan_transient_gas", input.CurrentZone.GetEntityCell(gas) == null || gas.GetPart<GasPoolPart>().Density <= 10);
            yield return Paid(quartz, "CrackQuartz|", c => c.Split('|')[4] == "20" && c.Split('|')[5] == "11", "11-crack-ground-light");
            var light = input.CurrentZone.GetReadOnlyEntities().SingleOrDefault(e => e.BlueprintName == "CrackedGlowQuartz");
            Check("finite_light", light != null && light.GetPart<LifespanPart>().TurnsRemaining > 0
                && light.GetPart<LifespanPart>().TurnsRemaining <= 30 && !Player.GetPart<InventoryPart>().Objects.Contains(quartz));
            yield return Capture("12-crystal-model");
            Check("light_native_model", light != null && input.ZoneRenderer.SpawnRing3D.TryGetApprovedStyle(light,out var proof)
                && proof.ModelId.StartsWith("spread-scenery-crackedglowquartz-",StringComparison.Ordinal));
            yield return Paid(boots, "BraceBoots|", c => true, "13-plant-feet");
            Require(input.CurrentZone.MoveEntity(companion,19,12),"controlled pusher position");
            var origin = input.CurrentZone.GetEntityPosition(Player);
            Check("brace_one_push", !SkillCombatHelpers.TryPush(companion,Player,input.CurrentZone,1) && input.CurrentZone.GetEntityPosition(Player)==origin);
            Check("second_push_moves", SkillCombatHelpers.TryPush(companion,Player,input.CurrentZone,1) && input.CurrentZone.GetEntityPosition(Player)!=origin);
            yield return Capture("14-finished");
        }
        static bool Tile(string command,int x,int y) => command.Split('|')[5]==x.ToString() && command.Split('|')[6]==y.ToString();
        IEnumerator Paid(Entity item,string prefix,Func<string,bool> match,string capture)
        {
            int before=input.TurnManager.TickCount, energy=input.TurnManager.GetEnergy(Player), speed=input.TurnManager.GetSpeed(Player);
            yield return InventoryChoice(item,prefix,match,capture);
            int elapsed=input.TurnManager.TickCount-before;
            int spent=energy+elapsed*speed-input.TurnManager.GetEnergy(Player);
            observations.Add(new { action=prefix, elapsedTicks=elapsed, speed, energySpent=spent });
            // The worn ironshod boots retain their real -5 Speed penalty, so
            // one action need not take exactly ten global energy ticks.
            Require(input.TurnManager.GetSpeed(Player)==speed&&spent==TurnManager.ActionThreshold,"one ordinary action for "+prefix);
        }
        void BuildFixture()
        {
            var zone=input.CurrentZone;
            foreach(var owner in zone.GetAllEntities().ToArray())
                if(owner!=Player){input.TurnManager.RemoveEntity(owner);zone.RemoveEntity(owner);}
            zone.GenReservedCells.Clear();
            for(int x=0;x<Zone.Width;x++)for(int y=0;y<Zone.Height;y++)
            {
                zone.TileState.Clear(x,y);
                if(x>0&&y>0&&x<Zone.Width-1&&y<Zone.Height-1)zone.AddEntity(input.EntityFactory.CreateEntity("StoneFloor"),x,y);
            }
            Require(zone.MoveEntity(Player,20,12),"controlled player placement");
            foreach(var worn in Player.GetPart<InventoryPart>().EquippedItems.Values.Distinct().ToArray())
                Require(InventorySystem.UnequipItem(Player,worn),"controlled equipment setup");
            // Starting tonics can merge with a granted test dose. Remove the
            // isolated starter pack so each finite stimulus keeps its exact ID.
            var pack=Player.GetPart<InventoryPart>();
            foreach(var starter in pack.Objects.ToArray())
                Require(pack.RemoveObject(starter),"controlled empty pack");
            Entity Carry(string bp){var e=input.EntityFactory.CreateEntity(bp);Require(e!=null&&Player.GetPart<InventoryPart>().AddObject(e),"finite supply "+bp);return e;}
            healing=Carry("HealingTonic");meal=Carry("ToastedEmberwheat");brine=Carry("GlimmerBrine");pith=Carry("PrismreedPith");
            lichen=Carry("FrostLichen");fruit=Carry("EmberFruit");oil=Carry("LampOil");moss=Carry("FireMoss");
            fan=Carry("LampveinFan");quartz=Carry("GlowQuartz");boots=Carry("IronshodBoots");
            Require(InventorySystem.Equip(Player,fan)&&InventorySystem.Equip(Player,boots),"controlled held fan and worn boots");
            companion=input.EntityFactory.CreateEntity("MarlbackScrabbler");Require(zone.AddEntity(companion,21,12),"controlled companion");
            Require(companion.GetPart<BrainPart>().SetPartyLeader(Player),"willing companion");
            companion.GetStat("Hitpoints").BaseValue=1;
            // This subject is deliberately not registered for AI turns: it holds
            // position so each menu action measures a known recipient and tile.
            framedCamera=input.CameraFollow;Require(framedCamera!=null,"gameplay camera");oldCameraZoom=framedCamera.GameplayZoomMultiplier;cameraFramed=true;
            framedCamera.GameplayZoomMultiplier=.42f;framedCamera.SnapToPlayer();RevealFixture();ZoneRenderHooks.MarkFullDirty("ItemUtility35Native.ControlledFixture");
        }
        void RevealFixture()
        { for (int x = 13; x <= 25; x++) for (int y = 8; y <= 16; y++) { var cell = input.CurrentZone.GetCell(x, y); cell.IsVisible = cell.Explored = true; } }
        IEnumerator InventoryChoice(Entity item, string prefix, Func<string, bool> match, string capture)
        {
            yield return Tap(Key.I); Require(State == "InventoryOpen", "native inventory entry");
            // Exact-owner public host entry; arrow navigation and Enter execute
            // the real menu selection and its ordinary pending-turn handoff.
            Require(input.InventoryUI.ReopenItemActionPopupFor(item), "inventory host entry");
            object popup = Field(input.InventoryUI, "_itemActionPopup"); var actions = (IList)Field(popup, "Actions");
            int wanted = -1;
            for (int i = 0; i < actions.Count; i++)
            {
                string command = (string)Field(actions[i], "Command");
                if (command.StartsWith(prefix, StringComparison.Ordinal) && match(command)) wanted = i;
            }
            Require(wanted >= 0, "native action " + prefix);
            int guard = 150; while ((int)Field(popup, "CursorIndex") != wanted && guard-- > 0)
                yield return Tap((int)Field(popup, "CursorIndex") < wanted ? Key.DownArrow : Key.UpArrow);
            Require((int)Field(popup, "CursorIndex") == wanted, "native navigation reached selected tile");
            yield return Capture(capture); yield return Tap(Key.Enter); Require(State == "Normal", "paid action returns to play");
        }
        string Snapshot() => JsonConvert.SerializeObject(new { tick = input.TurnManager.TickCount, energy = input.TurnManager.GetEnergy(Player),
            hp = Player.GetStatValue("Hitpoints"), zone = input.CurrentZone.ZoneID, player = Player.ID,
            pack = Player.GetPart<InventoryPart>().Objects.Select(e => new { e.ID, count = e.GetPart<StackerPart>()?.StackCount }).ToArray(),
            ground = input.CurrentZone.TileState.ToSaveString() });
        IEnumerator Tap(Key key)
        {
            yield return new WaitForSecondsRealtime(.12f); keyboard.MakeCurrent(); InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null; yield return new WaitForSecondsRealtime(.06f);
        }
        IEnumerator Capture(string name)
        {
            yield return new WaitForSecondsRealtime(.2f); yield return new WaitForEndOfFrame(); Directory.CreateDirectory(DirectoryPath);
            string path = Path.Combine(DirectoryPath, name + ".png"); DensityNativeScreenshot.CaptureToFile(path);
            Require(File.Exists(path) && new FileInfo(path).Length > 0, "rendered capture"); screenshots.Add(path);
            observations.Add(new { phase = name, state = State, snapshot = Snapshot() }); WriteReport();
        }
        IEnumerator RunSafely(IEnumerator routine)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(routine);
            try
            {
                while (stack.Count > 0)
                {
                    bool moved = false; object current = null; Exception error = null;
                    try { moved = stack.Peek().MoveNext(); if (moved) current = stack.Peek().Current; } catch (Exception e) { error = e; }
                    if (error != null) { fatal = error.ToString(); Check("precondition", false); break; }
                    if (!moved) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                    if (current is IEnumerator child) { stack.Push(child); continue; } yield return current;
                }
            }
            finally { while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose(); Cleanup(); Finished = true; WriteReport(); }
        }
        void Check(string name, bool pass) { checks.Add((pass ? "PASS " : "FAIL ") + name); if (!pass) failures++; WriteReport(); }
        static void Require(bool value, string reason) { if (!value) throw new InvalidOperationException(reason); }
        static object Field(object owner, string name) => owner.GetType().GetField(name, Fields).GetValue(owner);
        public void SetUnexpectedErrors(int count) { errors = count; errorsFinalized = true; WriteReport(); }
        public void Abort(string reason) { fatal = reason; failures++; StopAllCoroutines(); Cleanup(); Finished = true; WriteReport(); }
        void WriteReport()
        {
            Directory.CreateDirectory(DirectoryPath); File.WriteAllText(Path.Combine(DirectoryPath, "report.json"), JsonConvert.SerializeObject(new {
                runId, complete = Finished && errorsFinalized && Failures == 0 && checks.Count == Required.Length,
                failures = Failures, errors, fatal, seconds = clock?.Elapsed.TotalSeconds, checks, observations, screenshots,
                fixture = "Ordinary N/Classic start with isolated saves; its starter pack is replaced by finite controlled supplies. Flat Spread stone floor, one deliberately stationary companion outside the AI turn roster, held fan and worn boots. One water film and one transient gas are explicit stimuli. Real I/arrow/Enter menus and paid turns operate supplies. Two physical pushes are scripted measurements. Runtime camera zoom0.42 restored afterward; no campaign loaded.",
                canVerify = "Native menu availability and exact one-action costs, recipient healing/preparation, real finite films/wicking, freezing/thawing, oil ignition, transient gas reduction, finite ground-light geometry, one-use physical brace. Screenshots permit separate visual inspection.",
                cannotVerify = "Controlled item grants and stationary companion do not prove natural acquisition, encounter balance, autonomous NPC utility choice or long-term fun. Electricity and every remaining item variant are tested in EditMode, not claimed as visually exercised here."


            }, Formatting.Indented));
        }
        void Cleanup()
        {
            if (cleaned) return; cleaned = true;
            if (cameraFramed && framedCamera != null) { framedCamera.GameplayZoomMultiplier = oldCameraZoom; framedCamera.SnapToPlayer(); }
            if (keyboard != null) { InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.RemoveDevice(keyboard); }
            if (oldKeyboard != null && oldKeyboard.added) oldKeyboard.MakeCurrent();
            if (oldSettings != null) InputSystem.settings = oldSettings; if (settings != null) Destroy(settings); Application.runInBackground = oldBackground;
        }
        void OnDestroy()
        {
            if (!Finished && clock != null) { fatal = "Play stopped before native audit completion."; failures++; Finished = true; WriteReport(); }
            Cleanup();
        }
    }
}
