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
    /// <summary>Native keyboard proof of the authored Breaker's chosen-cell
    /// skills. Crowd geometry and stationary targets are explicit fixtures;
    /// player equipment, skills, statistics, damage and cooldown rules are real.</summary>
    public sealed class DirectedMeleeNativePlayer : MonoBehaviour
    {
        const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        readonly string runId = Guid.NewGuid().ToString("N");
        readonly List<string> checks = new List<string>(), screenshots = new List<string>();
        readonly List<object> observations = new List<object>();
        readonly Dictionary<string, bool> channels = new Dictionary<string, bool>();
        InputHandler input;
        Keyboard keyboard, oldKeyboard;
        InputSettings settings, oldSettings;
        bool oldBackground, cleaned, errorsFinalized;
        int failures, errors;
        string fatal, originalWeaponId;
        Entity east, north;
        TurnProbe playerProbe;
        System.Diagnostics.Stopwatch clock;
        public bool Finished { get; private set; }
        public int Failures => failures + errors;
        Entity Player => input.PlayerEntity;
        Zone Zone => input.CurrentZone;
        string State => Field(input, "_inputState").ToString();
        string DirectoryPath => Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/Verification/GalleryTactics/NativeTargeting", runId));

        public void Initialize(ScenarioContext context)
        {
            Require(!string.IsNullOrEmpty(SaveGameService.SaveRootOverride), "isolated save launcher");
            clock = System.Diagnostics.Stopwatch.StartNew();
            foreach (var name in new[] { "skill", "damage", "turn", "effect" })
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
            yield return Tap(Key.N);
            var menu = (StartingBuildMenuController)Field(input, "_buildMenuController");
            Require(menu.IsOpen, "native starting-build picker");
            int index = menu.Model.Options.ToList().FindIndex(b => b.Id == "breaker");
            Require(index >= 0 && index < 9, "authored Breaker card");
            yield return Tap((Key)Enum.Parse(typeof(Key), "Digit" + (index + 1)));
            Require(menu.SelectedIndex == index, "native Breaker selection");
            var definition = menu.Model.Selected;
            yield return Capture("00-native-breaker-picker");
            yield return Tap(Key.Enter); yield return Settled();
            originalWeaponId = StartingBuildService.PrimaryHandWeapon(Player)?.ID;
            Check("ordinary_breaker_start", Player.GetProperty(StartingBuildService.PropertyName) == "breaker"
                && Player.GetStatValue("Hitpoints") == 40 && Player.GetStatValue("Strength") == definition.Attributes.Strength
                && Player.GetStatValue("Agility") == definition.Attributes.Agility
                && StartingBuildService.PrimaryHandWeapon(Player)?.BlueprintName == "Cudgel"
                && !DebugInvincibility.IsEnabled(Player) && !Player.HasPart<BitLockerPart>());
            var slam = Ability("CommandSlam"); var conk = Ability("CommandConk");
            Check("original_ready_bound_skills", slam != null && conk != null && slam.IsUsable && conk.IsUsable
                && Player.GetPart<ActivatedAbilitiesPart>().GetSlotForAbility(slam.ID) >= 0
                && Player.GetPart<ActivatedAbilitiesPart>().GetSlotForAbility(conk.ID) >= 0);
            BuildControlledCrowd();
            playerProbe = new TurnProbe(); Player.AddPart(playerProbe);
            Check("controlled_crowd_geometry", Zone.GetEntityPosition(Player) == (20, 12)
                && Zone.GetEntityPosition(east) == (21, 12) && Zone.GetEntityPosition(north) == (20, 11)
                && Zone.GetCell(22, 12).IsSolid() && east.GetStatValue("Hitpoints") > 0 && north.GetStatValue("Hitpoints") > 0);
            yield return Capture("01-controlled-crowd-before");

            int eastBefore = east.GetStatValue("Hitpoints"), northBefore = north.GetStatValue("Hitpoints");
            int beforeEnds = playerProbe.Ends;
            string marker = LatestTrace();
            yield return Activate(slam); yield return Tap(Key.RightArrow); yield return Settled();
            var rows = Since(marker);
            Check("native_selected_slam_paid_once", playerProbe.Ends == beforeEnds + 1 && slam.CooldownRemaining > 0
                && rows.Any(e => e.Category == "skill" && e.Kind == "CommandRouted" && e.ActorId == Player.ID
                    && e.PayloadJson.Contains("CommandSlam")));
            Check("east_target_actual_wall_damage", east.GetStatValue("Hitpoints") > 0 && east.GetStatValue("Hitpoints") < eastBefore
                && Zone.GetEntityPosition(east) == (21, 12)
                && rows.Any(e => e.Category == "damage" && e.Kind == "DamageDealt" && e.ActorId == Player.ID && e.TargetId == east.ID));
            Check("north_bystander_untouched", north.GetStatValue("Hitpoints") == northBefore
                && Zone.GetEntityPosition(north) == (20, 11) && (north.GetPart<StatusEffectsPart>()?.GetAllEffects().Count ?? 0) == 0
                && !rows.Any(e => e.Category == "damage" && e.ActorId == Player.ID && e.TargetId == north.ID));
            observations.Add(new { phase = "native-right-slam", eastBefore, eastAfter = east.GetStatValue("Hitpoints"), northBefore,
                northAfter = north.GetStatValue("Hitpoints"), playerEnds = playerProbe.Ends - beforeEnds,
                cooldown = slam.CooldownRemaining, records = rows, messages = MessageLog.GetRecentEntries(12).Select(e => e.Text).ToArray() });
            yield return Capture("02-selected-wall-impact");

            int eastAfter = east.GetStatValue("Hitpoints"); beforeEnds = playerProbe.Ends;
            int beforeTick = input.TurnManager.TickCount, beforeEnergy = input.TurnManager.GetEnergy(Player);
            marker = LatestTrace();
            yield return Activate(conk); yield return Tap(Key.LeftArrow); yield return Settled();
            rows = Since(marker);
            Check("native_empty_choice_is_free", playerProbe.Ends == beforeEnds && conk.CooldownRemaining == 0
                && input.TurnManager.TickCount == beforeTick && input.TurnManager.GetEnergy(Player) == beforeEnergy
                && east.GetStatValue("Hitpoints") == eastAfter && north.GetStatValue("Hitpoints") == northBefore
                && rows.Any(e => e.Category == "skill" && e.Kind == "SkillRejected" && e.ActorId == Player.ID)
                && !rows.Any(e => e.Category == "damage" && e.ActorId == Player.ID));
            observations.Add(new { phase = "native-left-empty-conk", beforeTick, afterTick = input.TurnManager.TickCount,
                beforeEnergy, afterEnergy = input.TurnManager.GetEnergy(Player), playerEnds = playerProbe.Ends - beforeEnds,
                cooldown = conk.CooldownRemaining, records = rows, messages = MessageLog.GetRecentEntries(8).Select(e => e.Text).ToArray() });
            Check("original_player_finish", Player.GetStatValue("Hitpoints") == 40
                && Player.GetStatValue("Strength") == definition.Attributes.Strength
                && Player.GetStatValue("Agility") == definition.Attributes.Agility
                && StartingBuildService.PrimaryHandWeapon(Player)?.ID == originalWeaponId && !DebugInvincibility.IsEnabled(Player));
            yield return Capture("03-empty-selection-refused");
        }

        void BuildControlledCrowd()
        {
            // Explicit disposable fixture: preserve the actual chosen player;
            // flatten this isolated starting zone and add two stationary factory
            // enemies. No player/target HP, weapon, skill or RNG is rewritten.
            foreach (var entity in Zone.GetAllEntities().ToArray())
                if (entity != Player) { input.TurnManager.RemoveEntity(entity); Zone.RemoveEntity(entity); }
            Zone.GenReservedCells.Clear();
            for (int x = 0; x < Zone.Width; x++) for (int y = 0; y < Zone.Height; y++)
            {
                Zone.TileState.Clear(x, y);
                if (x > 0 && x < Zone.Width - 1 && y > 0 && y < Zone.Height - 1)
                    Place(input.EntityFactory.CreateEntity("StoneFloor"), x, y);
            }
            Require(Zone.MoveEntity(Player, 20, 12), "controlled player placement");
            east = StationaryEnemy(21, 12); north = StationaryEnemy(20, 11);
            Place(input.EntityFactory.CreateEntity("StoneWall"), 22, 12);
            input.CameraFollow?.SnapToPlayer(); ZoneRenderHooks.MarkFullDirty("DirectedMeleeNativeFixture");
        }
        Entity StationaryEnemy(int x, int y)
        {
            var entity = input.EntityFactory.CreateEntity("MarlbackGleaner"); Require(entity != null, "factory target");
            var brain = entity.GetPart<BrainPart>(); if (brain != null) entity.RemovePart(brain);
            Place(entity, x, y); input.TurnManager.AddEntity(entity); return entity;
        }
        void Place(Entity entity, int x, int y) => Require(entity != null && Zone.AddEntity(entity, x, y), "controlled fixture placement");
        ActivatedAbility Ability(string command) => Player.GetPart<ActivatedAbilitiesPart>().AbilityList.SingleOrDefault(a => a.Command == command);
        IEnumerator Activate(ActivatedAbility ability)
        {
            Require(ability != null && ability.IsUsable, "original ready ability");
            int slot = Player.GetPart<ActivatedAbilitiesPart>().GetSlotForAbility(ability.ID);
            Require(slot >= 0 && slot < 10, "actual skill hotbar slot");
            yield return Tap((Key)Enum.Parse(typeof(Key), slot == 9 ? "Digit0" : "Digit" + (slot + 1)));
            Require(State == "AwaitingDirection", "actual native direction prompt");
        }
        static string LatestTrace() => Diag.Snapshot(1).LastOrDefault().TraceId;
        static Diag.Entry[] Since(string marker)
        {
            var all = Diag.Snapshot(Diag.BufferCapacity).ToArray();
            if (marker == null) return all.ToArray();
            int index = Array.FindIndex(all, e => e.TraceId == marker);
            Require(index >= 0, "diagnostic observation window retained");
            return all.Skip(index + 1).ToArray();
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
                seconds = clock?.Elapsed.TotalSeconds, checks, observations, screenshots, source = input?.CurrentZone?.ZoneID,
                fixture = "Native N/Breaker picker and original player; isolated starting zone cleared to floor; actual factory MarlbackGleaners staged east/north with Brain removed and registered for turns; real east wall. No HP/stat/skill/weapon/RNG edits. All casts use keyboard hotbar/direction input.",
                canVerify = "Native chosen-cell input, paid Slam turn/cooldown, actual selected target HP loss and damage attribution, untouched bystander, empty-cell refusal with no turn/energy/cooldown cost.",
                cannotVerify = "Controlled stationary crowd, not ordinary generated encounter discovery, enemy decisions, sustained balance or fun. Screenshots need independent inspection; this does not establish all eight skills visually."
            }, Formatting.Indented));
        }
        void Cleanup()
        {
            if (cleaned) return; cleaned = true;
            if (playerProbe != null) input?.PlayerEntity?.RemovePart(playerProbe);
            if (keyboard != null) { InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.RemoveDevice(keyboard); }
            if (oldKeyboard != null && oldKeyboard.added) oldKeyboard.MakeCurrent();
            if (oldSettings != null) InputSystem.settings = oldSettings; if (settings != null) Destroy(settings);
            Application.runInBackground = oldBackground;
        }
        void OnDestroy() { Cleanup(); foreach (var p in channels) Diag.SetChannel(p.Key, p.Value); }
        sealed class TurnProbe : Part
        {
            public int Ends;
            public override bool HandleEvent(GameEvent e) { if (e.ID == "EndTurn") Ends++; return true; }
        }
    }
}
