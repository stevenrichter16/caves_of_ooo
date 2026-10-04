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
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace CavesOfOoo.Scenarios.Custom
{
    /// <summary>Bounded native-input proof using a real generated role. One
    /// labelled player transfer and one 8HP injury are fixtures, not normal
    /// exploration or naturally earned damage. No inventory/AI grants.</summary>
    public sealed class FieldMedicineNativePlayer : MonoBehaviour
    {
        const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        readonly string runId = Guid.NewGuid().ToString("N");
        readonly List<string> checks = new List<string>(), notes = new List<string>(), screenshots = new List<string>();
        readonly List<object> observations = new List<object>();
        readonly Dictionary<string, bool> channels = new Dictionary<string, bool>();
        InputHandler input;
        Keyboard keyboard, oldKeyboard;
        InputSettings settings, oldSettings;
        bool oldBackground, cleaned, errorsFinalized;
        int failures, errors;
        string fatal, targetId;
        Entity target;
        Zone source;
        Cell approach;
        System.Diagnostics.Stopwatch clock;
        public bool Finished { get; private set; }
        public int Failures => failures + errors;
        Entity Player => input.PlayerEntity;
        Zone Zone => input.CurrentZone;
        OverworldZoneManager Manager => input.ZoneManager as OverworldZoneManager;
        string State => Field(input, "_inputState").ToString();
        string DirectoryPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/Verification/EnemyFieldMedicine/Native", runId));

        public void Initialize(ScenarioContext context)
        {
            Require(!string.IsNullOrEmpty(SaveGameService.SaveRootOverride), "isolated save launcher");
            clock = System.Diagnostics.Stopwatch.StartNew();
            foreach (var name in new[] { "ai", "event", "turn", "damage" })
            { channels[name] = Diag.IsChannelEnabled(name); Diag.SetChannel(name, true); }
            oldSettings = InputSystem.settings; settings = Instantiate(oldSettings);
            settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings = settings; oldBackground = Application.runInBackground; Application.runInBackground = true;
            oldKeyboard = Keyboard.current; keyboard = InputSystem.AddDevice<Keyboard>();
            StartCoroutine(RunSafely(Run()));
        }

        IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(.8f);
            input = FindFirstObjectByType<InputHandler>(); Require(input != null, "ordinary bootstrap");
            Require(((BootMenuController)Field(input, "_bootMenuController")).IsActive, "native new-game menu");
            yield return Tap(Key.N); yield return Settled();
            Check("ordinary_start", Manager.WorldSeed == 1729 && Player.GetStatValue("Hitpoints") == 40 && !Player.HasPart<BitLockerPart>());
            FindSource();
            targetId = target.ID;
            Check("generated_owned_supply", target.GetPart<FieldMedicinePart>().FindCarriedMedicine() != null && target.GetStatValue("Hitpoints") == 20);
            Require(Zone.TryTransferEntityTo(Player, source, approach.X, approach.Y), "labelled approach transfer");
            typeof(InputHandler).GetMethod("HandleZoneTransition", Fields).Invoke(input, new object[] {
                new ZoneTransitionResult { Success = true, NewZone = source, NewPlayerX = approach.X, NewPlayerY = approach.Y } });
            input.CameraFollow?.SnapToPlayer(); ZoneRenderHooks.MarkFullDirty("FieldMedicineNativeApproach");
            // Controlled injury is explicit. The source, loot, personality and
            // ordinary decision stack remain exactly what generation supplied.
            CombatSystem.ApplyDamage(target, target.GetStatValue("Hitpoints") - 8, null, Zone);
            notes.Add("FIXTURES: one player approach transfer; generated target damaged to 8/20HP. No tonic, hostility, goal, RNG or energy grant.");
            yield return Settled();
            Check("native_full_harness", Rendered("patchbearer-full"));
            yield return Capture("01-carried-medicine");
            int hp = Player.GetStatValue("Hitpoints"); var at = Zone.GetEntityPosition(target);
            long serial = MessageLog.NextSerialValue;
            string bottle = target.GetPart<FieldMedicinePart>().FindCarriedMedicine().ID;
            var probe = new ActionProbe(); target.AddPart(probe);
            var playerProbe = new ActionProbe(); Player.AddPart(playerProbe);
            for (int wait = 0; wait < 3 && probe.Ends == 0; wait++)
            {
                int beforeEnds = playerProbe.Ends;
                int tickBefore = input.TurnManager.TickCount;
                int energyBefore = input.TurnManager.GetEnergy(target);
                yield return Tap(Key.Period); yield return Settled();
                Require(playerProbe.Ends == beforeEnds + 1, "one native paid wait, without synthetic scheduler advancement");
                observations.Add(new { wait = wait + 1, tickBefore, tickAfter = input.TurnManager.TickCount,
                    energyBefore, energyAfter = input.TurnManager.GetEnergy(target), actorBegins = probe.Begins, actorEnds = probe.Ends });
            }
            Player.RemovePart(playerProbe);
            var used = Diag.Snapshot(Diag.BufferCapacity).Where(e => e.Kind == "FieldMedicineUsed" && e.ActorId == targetId).ToArray();
            Check("one_paid_medicine_action", probe.Begins == 1 && probe.Ends == 1 && used.Length == 1
                && target.GetStatValue("Hitpoints") > 8 && Zone.GetEntityPosition(target) == at
                && Player.GetStatValue("Hitpoints") == hp && target.GetPart<FieldMedicinePart>().FindCarriedMedicine() == null
                && !target.GetPart<InventoryPart>().Objects.Any(e => e.ID == bottle));
            target.RemovePart(probe);
            Check("native_use_log", MessageLog.NextSerialValue > serial && MessageLog.GetRecentEntries(12).Any(e => e.Text.Contains("marlback patchbearer") && e.Text.Contains("tonic")));
            Check("native_empty_harness", Rendered("patchbearer-empty"));
            observations.Add(new { targetId, bottle, used, hp = target.GetStatValue("Hitpoints"), begins = probe.Begins, ends = probe.Ends,
                messages = MessageLog.GetRecentEntries(12).Select(e => e.Text).ToArray() });
            yield return Capture("02-spent-medicine");
            var previousPlayer = Player; var previousTarget = target; int healed = target.GetStatValue("Hitpoints");
            yield return Tap(Key.F5); yield return Settled();
            Check("native_quicksave", SaveGameService.GetSaveInfo("Quick")?.ActiveZoneID == Zone.ZoneID && MessageLog.GetLast() == "Game saved.");
            yield return Tap(Key.F6);
            double loadStarted = Time.realtimeSinceStartupAsDouble;
            while (ReferenceEquals(Player, previousPlayer))
            { Require(Time.realtimeSinceStartupAsDouble - loadStarted < 8, "F6 replaces live player graph"); yield return null; }
            yield return Settled();
            target = Zone.GetReadOnlyEntities().Single(e => e.ID == targetId);
            Check("native_saved_depletion", Player != previousPlayer && target != previousTarget && target.GetStatValue("Hitpoints") == healed
                && target.GetPart<FieldMedicinePart>().FindCarriedMedicine() == null && Rendered("patchbearer-empty"));
            yield return Capture("03-restored-empty-harness");
        }

        void FindSource()
        {
            var origin = WorldMap.FromZoneID(ReferenceGladePlan.ZoneID);
            var columns = (from y in Enumerable.Range(0, WorldMap.Height) from x in Enumerable.Range(0, WorldMap.Width)
                let plan = UndergroundLayoutPlan.Select(Manager.WorldSeed, WorldMap.ToZoneID(x, y, 3), Manager.WorldMap.GetBiome(x, y), Manager.WorldMap.GetPOI(x, y), Manager.Factory)
                where plan.IsOrdinaryColumn && plan.SurfaceBiome == BiomeType.Spread
                orderby Math.Abs(x - origin.x) + Math.Abs(y - origin.y), y, x select (x, y)).Take(32).ToArray();
            foreach (var column in columns)
            {
                var zone = Manager.GetZone(WorldMap.ToZoneID(column.x, column.y, 0));
                for (int depth = 1; depth <= 5; depth++)
                {
                    string below = WorldMap.GetZoneBelow(zone.ZoneID);
                    var edge = Manager.GetConnections(zone.ZoneID).FirstOrDefault(e => e.SourceZoneID == zone.ZoneID && e.TargetZoneID == below && e.Type == "StairsDown");
                    if (edge == null || !zone.GetCell(edge.SourceX, edge.SourceY).Objects.Any(e => e.HasPart<StairsDownPart>())) break;
                    zone = Manager.GetZone(below);
                    Require(zone.GetCell(edge.TargetX, edge.TargetY).Objects.Any(e => e.HasPart<StairsUpPart>()), "physical generated return stairs");
                    if (depth < 3) continue;
                    notes.Add("Inspected " + zone.ZoneID);
                    foreach (var actor in zone.GetReadOnlyEntities().Where(e => e.BlueprintName == "MarlbackPatchbearer"))
                    {
                        if (!FactionManager.IsHostile(actor, Player)) continue;
                        var p = zone.GetEntityCell(actor);
                        for (int y = p.Y - 1; y <= p.Y + 1; y++) for (int x = p.X - 1; x <= p.X + 1; x++)
                        {
                            var cell = zone.GetCell(x, y);
                            if (cell == null || !zone.CanPlaceFootprint(Player, x, y) || cell.Objects.Any(e => e.HasPart<LiquidPoolPart>() || e.HasPart<GasPoolPart>() || e.HasPart<TriggerOnStepPart>() || e.HasEffect<BurningEffect>())) continue;
                            if (zone.GetReadOnlyEntities().Any(e => e != actor && e.HasTag("Creature") && FactionManager.IsHostile(e, Player)
                                && SpatialQuery.DistanceToCell(zone, e, x, y) < 8)) continue;
                            target = actor; source = zone; approach = cell; return;
                        }
                    }
                }
            }
            throw new InvalidOperationException("No quiet eligible source in predeclared nearest32 seed1729 columns; no reroll or actor removal.");
        }

        bool Rendered(string expected)
        {
            var presenter = input.ZoneRenderer.SpawnRing3D;
            return SpawnRing3DRecipes.Resolve(Zone, target, Resources.Load<SpawnRing3DLibrary>(SpawnRing3DLibrary.ResourcePath).Definition).ModelId == expected
                && presenter.TryGetApprovedStyle(target, out _);
        }
        IEnumerator Tap(Key key)
        {
            yield return new WaitForSecondsRealtime(.2f);
            keyboard.MakeCurrent(); InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
            yield return new WaitForSecondsRealtime(.2f);
        }
        IEnumerator Settled()
        {
            double start = Time.realtimeSinceStartupAsDouble;
            while (State != "Normal" || input.ZoneRenderer.WorldFx?.HasBlockingFx == true)
            { Require(Time.realtimeSinceStartupAsDouble - start < 10, "native input settles"); yield return null; }
            yield return null;
        }
        IEnumerator Capture(string name)
        {
            yield return new WaitForSecondsRealtime(.2f); yield return new WaitForEndOfFrame();
            Directory.CreateDirectory(DirectoryPath); string path = Path.Combine(DirectoryPath, name + ".png");
            DensityNativeScreenshot.CaptureToFile(path); Require(File.Exists(path), "native capture exists"); screenshots.Add(path); WriteReport();
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
            Directory.CreateDirectory(DirectoryPath);
            File.WriteAllText(Path.Combine(DirectoryPath, "report.json"), JsonConvert.SerializeObject(new {
                runId, complete = Finished && errorsFinalized && Failures == 0 && checks.Count == 8, failures = Failures, errors, fatal,
                seconds = clock?.Elapsed.TotalSeconds, checks, notes, observations, screenshots, source = source?.ZoneID,
                canVerify = "Generated owned inventory, controlled-injury native wait action, actual full/empty rendered body, log and F5/F6 saved depletion.",
                cannotVerify = "Not an ordinary walking route or naturally earned injury. Player approach and injury are declared fixtures. One encounter does not establish balance, rarity, visual readability or fun; screenshots require independent inspection."
            }, Formatting.Indented));
        }
        void Cleanup()
        {
            if (cleaned) return; cleaned = true;
            if (keyboard != null) { InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.RemoveDevice(keyboard); }
            if (oldKeyboard != null && oldKeyboard.added) oldKeyboard.MakeCurrent();
            if (oldSettings != null) InputSystem.settings = oldSettings; if (settings != null) Destroy(settings);
            Application.runInBackground = oldBackground;
        }
        void OnDestroy() { Cleanup(); foreach (var p in channels) Diag.SetChannel(p.Key, p.Value); }
        sealed class ActionProbe : Part
        {
            public int Begins, Ends;
            public override bool HandleEvent(GameEvent e) { if (e.ID == "BeginTakeAction") Begins++; if (e.ID == "EndTurn") Ends++; return true; }
        }
    }
}
