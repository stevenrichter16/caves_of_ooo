using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CavesOfOoo.Core;
using CavesOfOoo.Data;
using CavesOfOoo.Rendering;
using CavesOfOoo.Skills;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;

namespace CavesOfOoo.Editor
{
    /// <summary>Isolated acceptance through the real Main bootstrap, skill command router,
    /// coordinator and Unity DSP. This audit never drains/replays SpellFxBus sequences.</summary>
    [InitializeOnLoad]
    public static class StarterSpellAudioNativeAudit
    {
        const string Prefix = "StarterSpellAudio.Audit.";
        const int DurableHp = 10000;
        const double RowSeconds = 3.0;
        const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        static readonly string[] Ids = { "Pyromancy_FlamingHands", "Hydromancy_JetBlast", "Galvanism_GroundSurge",
            "Cryomancy_RimeGrip", "Spellcraft_Calm", "Hydromancy_ConjureRain", "Pyromancy_EmberSpit" };
        static readonly string[][] Stems = {
            new[] { "palms_catch", "combustion_body", "dry_afterburn" },
            new[] { "liquid_gather", "pressurized_water", "wet_slap", "bubbles_and_runoff" },
            new[] { "charging_current", "advancing_arcs", "ground_fracture", "grit_displacement" },
            new[] { "inward_ice_stress", "brittle_lock", "settling_fragments" },
            new[] { "warm_bowl_body", "resolved_overtone", "human_exhale" },
            new[] { "leaf_rain", "close_leaf_patter", "individual_drips" },
            new[] { "wind", "fire", "impact" } };
        // Surge variants 1/3 have a prefix-only charging layer; variant 2 also has a real suffix.
        static readonly int[] SuccessContacts = { 3, 4, 3, 3, 3, 3, 1 };
        static readonly float[] Output = new float[1024];
        static readonly float[] SourceOutput = new float[1024];
        static readonly List<LoggedError> LoggedErrors = new List<LoggedError>();
        static InputHandler input;
        static Entity dummy, cropEntity, blocker;
        static CropPart crop;
        static int originX, originY, stage, rowIndex, profileIndex, lastFrame;
        static int beforeAccepted, beforeContacts, beforeEmberAccepted, beforeEmberImpacts;
        static double readyAt, rowStart, profileStart, lastCast;
        static double lastObservationWall, lastObservedFrameWall;
        static int lastObservedFrame;
        static AudioSource[] starterSources, emberSources;
        static ProfilerRecorder recorder;
        static Report report;
        static Row current;
        static bool[] observedStems, observedPrefixes;
        static bool settingsCaptured, inputStateCaptured, oldInputEnabled;
        static float oldVolume, oldSpeed, oldListenerVolume;
        static bool oldBackground, oldNative, oldListenerPause;
        static SpellFxMode oldMode;

        [Serializable] sealed class Row
        {
            public string spell, scenario;
            public bool muted, expectedConsumed = true, expectAudio = true, passed, consumed, blocks;
            public bool primarySuccess, prefixProgress, contactProgress, naturalTailCleared;
            public int expectedContacts, acceptedDelta, contactsDelta, pendingAfterCommand, variant;
            public double outputPeak, prefixOutputPeak, contactOutputPeak;
            public double prefixSourceOutputPeak, contactSourceOutputPeak, prefixObservedMinVolume = -1;
            public double commandWallTime, commandDspTime, commandCompletedWallAge, observedWallAge, observedDspAge;
            public double maxPollGapSeconds, maxFrameGapSeconds, maxUnscaledFrameDeltaSeconds;
            public double firstPrefixWallAge = -1, lastPrefixWallAge = -1, firstPrefixDspAge = -1, lastPrefixDspAge = -1;
            public double firstContactWallAge = -1, lastContactWallAge = -1, firstContactDspAge = -1, lastContactDspAge = -1;
            public int polls, prefixPolls, contactPolls;
            public string[] contactStemsObserved, prefixStemsObserved;
            public List<SourceObservation> sources = new List<SourceObservation>();
        }
        [Serializable] sealed class SourceObservation
        {
            public string stem, phase, clip;
            public int observations, firstTimeSamples = -1, lastTimeSamples, maximumTimeSamples;
            public double outputPeak, firstWallAge, lastWallAge, firstDspAge, lastDspAge;
            public float minimumVolume = -1, maximumVolume, minimumPitch = -1, maximumPitch;
        }
        [Serializable] sealed class LoggedError
        {
            public string kind, message, trace, spell, scenario;
            public int stage;
            public double wallTime, dspTime;
        }
        [Serializable] sealed class Report
        {
            public string runId, privateRoot, error;
            public string canVerify = "Real Main boot, TryRouteSkillCommand resolution, owned fixture outcomes, normal coordinator acceptance, AudioSource timeSamples/isPlaying and sampled source output, sampled Unity mixer output, phase observation timing/gain, fixed pools, and the reported completed or partial update-marker window.";
            public string cannotVerify = "Physical speaker/headphone routing, subjective timbre/comfort, visual quality, or raw keyboard handling. After ordinary boot/fixture setup, InputHandler.Update is disabled to isolate this routed-command audit; ZoneRenderer remains enabled and owns normal FX playback.";
            public bool passed, bootAccepted, nativeReady, rainLearnedForFixtureOnly, poolStable, tailCleared;
            public bool profileStarted, profileComplete;
            public bool previousInputEnabled, rawInputDisabled, inputEnabledRestored;
            public int commands, profileCommands, profileFrames, profileSamples, unexpectedErrors;
            public int starterAccepted, starterContactLayers, emberAccepted, emberImpacts, starterSources, emberSources;
            public double profileSeconds, updateTotalMicroseconds, updateMeanMicroseconds, updateMaxMicroseconds, dspStart, dspEnd;
            public int failureStage = -1;
            public string failedSpell, failedScenario;
            public double failedWallAge, failedDspAge;
            public LoggedError[] loggedErrors;
            public List<Row> rows = new List<Row>();
        }
        sealed class ZeroRng : System.Random
        {
            public override int Next(int maximum) => 0;
            public override int Next(int minimum, int maximum) => minimum;
        }
        static readonly ZeroRng Rng = new ZeroRng();
        sealed class NewGameProbe : IInputProbe { public bool GetKeyDown(KeyCode key) => key == KeyCode.N; }

        static StarterSpellAudioNativeAudit()
        { if (SessionState.GetBool(Prefix + "active", false)) Subscribe(); }
        [MenuItem("Caves Of Ooo/Scenarios/Magic/Starter Spell Audio Acceptance")]
        public static void Launch() => Run(false);
        public static void RunFromCommandLine() => Run(true);
        static void Run(bool exit)
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Start starter audio acceptance from an idle native Editor.");
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Audio audit refuses to discard dirty scenes.");
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path != "Assets/Scenes/Main/SampleScene.unity")
                throw new InvalidOperationException("Open the Main SampleScene first.");
            input = null; report = null; current = null; settingsCaptured = inputStateCaptured = false;
            LoggedErrors.Clear();
            stage = rowIndex = profileIndex = 0;
            SessionState.SetString(Prefix + "id", Guid.NewGuid().ToString("N"));
            SessionState.SetInt(Prefix + "seed", NativeAuditBootstrapSettings.RequestedSeed);
            SessionState.SetInt(Prefix + "errors", 0);
            string token = NativeSaveIsolation.Begin(Prefix, "starter-audio-private-marker", exit);
            SessionState.SetString(Prefix + "token", token);
            SessionState.SetBool(Prefix + "active", true);
            SessionState.SetFloat(Prefix + "deadline", (float)EditorApplication.timeSinceStartup + 300);
            Subscribe(); EditorApplication.isPlaying = true;
        }
        static void Subscribe()
        {
            NativeSaveIsolation.Restore(Prefix, SessionState.GetString(Prefix + "token", ""), Finish);
            NativeAuditBootstrapSettings.RequestedSeed = 729490642;
            GameBootstrap.OnAfterBootstrap -= Boot; GameBootstrap.OnAfterBootstrap += Boot;
            EditorApplication.update -= Poll; EditorApplication.update += Poll;
            Application.logMessageReceived -= Log; Application.logMessageReceived += Log;
        }
        static void Log(string message, string trace, LogType kind)
        {
            if (kind == LogType.Error || kind == LogType.Exception || kind == LogType.Assert)
            {
                SessionState.SetInt(Prefix + "errors", SessionState.GetInt(Prefix + "errors", 0) + 1);
                if (LoggedErrors.Count < 16) LoggedErrors.Add(new LoggedError { kind = kind.ToString(), message = message,
                    trace = trace, spell = current?.spell, scenario = current?.scenario, stage = stage,
                    wallTime = EditorApplication.timeSinceStartup, dspTime = AudioSettings.dspTime });
            }
        }
        static void Boot(Zone zone, EntityFactory factory, Entity player, TurnManager turns)
        {
            GameBootstrap.OnAfterBootstrap -= Boot;
            report = new Report { runId = SessionState.GetString(Prefix + "id", ""), privateRoot = SaveGameService.SaveRootOverride };
            for (int i = 0; i < Ids.Length; i++) report.rows.Add(NewRow(i, "success"));
            for (int i = 0; i < Ids.Length; i++) report.rows.Add(NewRow(i, "muted", muted: true));
            report.rows.Add(NewRow(3, "water-only"));
            report.rows.Add(NewRow(3, "dry-refusal", consumed: false, audible: false));
            report.rows.Add(NewRow(4, "already-peaceful", contacts: 2));
            report.rows.Add(NewRow(2, "push-blocked", contacts: 2));
            report.rows.Add(NewRow(5, "no-crops", audible: false));
            readyAt = EditorApplication.timeSinceStartup + 1; stage = 0;
        }
        static Row NewRow(int i, string scenario, bool muted = false, bool consumed = true, bool audible = true, int contacts = -1)
            => new Row { spell = Ids[i], scenario = scenario, muted = muted, expectedConsumed = consumed,
                expectAudio = audible && !muted, expectedContacts = audible && !muted ? contacts >= 0 ? contacts : SuccessContacts[i] : 0 };

        static void Poll()
        {
            if (!SessionState.GetBool(Prefix + "active", false)) return;
            try
            {
                double now = EditorApplication.timeSinceStartup;
                Require(now < SessionState.GetFloat(Prefix + "deadline", 0), "Native audio audit watchdog expired.");
                if (report == null || now < readyAt || !EditorApplication.isPlaying) return;
                if (stage == 0)
                {
                    input = UnityEngine.Object.FindFirstObjectByType<InputHandler>(); Require(input != null, "Main input missing.");
                    oldVolume = SpellFxSettings.SoundVolume; oldSpeed = SpellFxSettings.AnimationSpeed; oldMode = SpellFxSettings.Mode;
                    oldBackground = Application.runInBackground; oldNative = Village3DSettings.Enabled;
                    oldListenerPause = AudioListener.pause; oldListenerVolume = AudioListener.volume; settingsCaptured = true;
                    Application.runInBackground = true; Village3DSettings.Enabled = true;
                    AudioListener.pause = false; AudioListener.volume = 1;
                    SpellFxSettings.SoundVolume = 1; SpellFxSettings.AnimationSpeed = 1; SpellFxSettings.Mode = SpellFxMode.Full;
                    var boot = (BootMenuController)typeof(InputHandler).GetField("_bootMenuController", Private).GetValue(input);
                    var service = (ISaveLoadService)typeof(InputHandler).GetField("_saveLoadService", Private).GetValue(null);
                    Require(boot.IsActive, "Private marker must select the normal boot menu.");
                    boot.Tick(new NewGameProbe(), service, MessageLog.Add);
                    report.bootAccepted = !boot.IsActive; Require(report.bootAccepted, "New-game dispatch failed.");
                    PlaceFixture();
                    // Leave the ordinary boot path intact, then prevent real
                    // keyboard polling from injecting casts into this owned audit.
                    // ZoneRenderer.LateUpdate continues normal coordinator playback.
                    oldInputEnabled = input.enabled; inputStateCaptured = true;
                    report.previousInputEnabled = oldInputEnabled; input.enabled = false;
                    report.rawInputDisabled = !input.enabled;
                    stage = 1; readyAt = now + 2; return;
                }
                var world = input.ZoneRenderer.WorldFx;
                if (stage == 1)
                {
                    if (!world.NativeRenderer.IsPrepared) { Require(now - readyAt < 20, "Native surface did not prepare."); return; }
                    report.nativeReady = true;
                    Require(world.StarterAudio.Root != null && world.StarterAudio.AllocatedSources == 32, "Starter pool not prepared before casting.");
                    Require(world.EmberAudio.IsPrepared, "Preserved Ember audio did not prepare.");
                    starterSources = world.StarterAudio.Root.GetComponentsInChildren<AudioSource>();
                    emberSources = world.EmberAudio.Root.GetComponentsInChildren<AudioSource>();
                    report.starterSources = starterSources.Length; report.emberSources = emberSources.Length;
                    Require(starterSources.Length == 32 && emberSources.Length == 12, "Unexpected initial source pools.");
                    report.dspStart = AudioSettings.dspTime; stage = 2;
                }
                if (stage == 2)
                {
                    if (rowIndex >= report.rows.Count)
                    {
                        current = null;
                        SpellFxSettings.SoundVolume = 1; profileStart = lastCast = now; lastFrame = -1;
                        report.profileStarted = true;
                        recorder = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "COO.StarterSpellAudio.Update", 1,
                            ProfilerRecorderOptions.Default | ProfilerRecorderOptions.SumAllSamplesInFrame);
                        Require(recorder.Valid, "Starter audio profiler marker unavailable."); stage = 4; return;
                    }
                    current = report.rows[rowIndex]; StartCast(current); rowStart = now; stage = 3; return;
                }
                if (stage == 3)
                {
                    ObserveCurrent();
                    if (now - rowStart < RowSeconds) return;
                    FinishRow(); rowIndex++; stage = 2; return;
                }
                if (stage == 4)
                {
                    report.profileSeconds = now - profileStart;
                    if (Time.frameCount != lastFrame)
                    {
                        lastFrame = Time.frameCount; report.profileFrames++;
                        if (recorder.Count > 0)
                        {
                            double microseconds = recorder.LastValue / 1000.0;
                            report.updateTotalMicroseconds += microseconds;
                            report.updateMaxMicroseconds = Math.Max(report.updateMaxMicroseconds, microseconds); report.profileSamples++;
                            report.updateMeanMicroseconds = report.updateTotalMicroseconds / report.profileSamples;
                        }
                    }
                    Require(world.StarterAudio.AllocatedSources == 32 && world.EmberAudio.AllocatedSources == 12, "Source pool grew during mixed casts.");
                    if (current != null)
                    {
                        ObserveCurrent();
                        if (now - rowStart >= RowSeconds) { FinishRow(); current = null; }
                    }
                    if (current == null && now - lastCast >= RowSeconds + .1 && now - profileStart < 57)
                    {
                        current = NewRow(profileIndex++ % Ids.Length, "profile-success"); report.rows.Add(current);
                        StartCast(current); rowStart = lastCast = now; report.profileCommands++;
                    }
                    if (now - profileStart < 61 || current != null) return;
                    report.profileSeconds = now - profileStart;
                    report.starterAccepted = world.StarterAudio.AcceptedCount; report.starterContactLayers = world.StarterAudio.ContactLayerCount;
                    report.emberAccepted = world.EmberAudio.AcceptedCount; report.emberImpacts = world.EmberAudio.ImpactCount;
                    report.poolStable = world.StarterAudio.AllocatedSources == 32 && world.EmberAudio.AllocatedSources == 12;
                    report.tailCleared = world.StarterAudio.ActiveVoices == 0 && world.EmberAudio.ActiveVoices == 0;
                    report.dspEnd = AudioSettings.dspTime;
                    Require(report.poolStable && report.tailCleared && report.profileSamples > 30 && report.profileCommands >= 14,
                        "Mixed-cast lifetime/profile gate failed.");
                    report.profileComplete = true;
                    Finish(0);
                }
            }
            catch (Exception error)
            {
                if (report == null) report = new Report { runId = SessionState.GetString(Prefix + "id", "") };
                report.error = error.ToString(); report.failureStage = stage;
                report.failedSpell = current?.spell; report.failedScenario = current?.scenario;
                if (current != null)
                {
                    report.failedWallAge = EditorApplication.timeSinceStartup - current.commandWallTime;
                    report.failedDspAge = AudioSettings.dspTime - current.commandDspTime;
                }
                Finish(1);
            }
        }

        static void PlaceFixture()
        {
            var zone = input.CurrentZone; var actor = input.PlayerEntity; originX = originY = -1;
            // Find existing floor: no scenery erasure and no unrelated crops within Rain's radius.
            for (int distance = 0; distance < Zone.Width + Zone.Height && originX < 0; distance++)
                for (int y = 3; y < Zone.Height - 3 && originX < 0; y++)
                    for (int x = 3; x < Zone.Width - 7; x++)
                    {
                        if (Math.Abs(x - 40) + Math.Abs(y - 12) != distance) continue;
                        bool clear = true;
                        for (int k = 0; k <= 6 && clear; k++) clear = Open(zone, actor, x + k, y);
                        if (clear) clear = Open(zone, actor, x + 2, y - 1) && Open(zone, actor, x + 2, y + 1) && Open(zone, actor, x, y + 1);
                        for (int dy = -3; dy <= 3 && clear; dy++) for (int dx = -3; dx <= 3 && clear; dx++)
                            foreach (var owner in zone.GetCell(x + dx, y + dy).Occupants)
                                if (owner.GetPart<CropPart>() != null) { clear = false; break; }
                        if (clear) { originX = x; originY = y; break; }
                    }
            Require(originX >= 0, "No existing lane/crop-free radius; audit will not erase scenery.");
            Require(zone.RemoveEntity(actor) && zone.AddEntity(actor, originX, originY), "Fixture player placement failed.");
            typeof(InputHandler).GetMethod("HandleZoneTransition", Private).Invoke(input, new object[] {
                new ZoneTransitionResult { Success = true, NewZone = zone, NewPlayerX = originX, NewPlayerY = originY } });
            dummy = new Entity { ID = "starter-audio-owned-target", BlueprintName = "CaveHermit" }; dummy.SetTag("Creature");
            dummy.AddPart(new RenderPart { DisplayName = "audio study target", RenderString = "h", ColorString = "&W" });
            dummy.AddPart(new PhysicsPart { Solid = false }); dummy.AddPart(new StatusEffectsPart());
            dummy.AddPart(new BrainPart { Passive = true, Wanders = false, WandersRandomly = false });
            dummy.Statistics["Hitpoints"] = new Stat { Owner = dummy, Name = "Hitpoints", BaseValue = DurableHp, Min = 0, Max = DurableHp };
            cropEntity = new Entity { ID = "starter-audio-owned-crop", BlueprintName = "AudioStudyCrop" };
            cropEntity.AddPart(new RenderPart { DisplayName = "audio study crop", RenderString = ".", ColorString = "&g" });
            cropEntity.AddPart(new PhysicsPart { Solid = false }); crop = new CropPart { TicksPerStage = 100000 }; cropEntity.AddPart(crop);
            blocker = new Entity { ID = "starter-audio-owned-push-blocker", BlueprintName = "AudioStudyBlocker" };
            blocker.SetTag("Solid"); blocker.AddPart(new PhysicsPart { Solid = true });
            var skills = actor.GetPart<SkillsPart>(); Require(skills != null, "Ordinary player skills missing.");
            for (int i = 0; i < Ids.Length; i++)
                if (i != 5) Require(skills.GetSkill(Ids[i]) != null, "Ordinary starter kit missing " + Ids[i]);
            if (skills.GetSkill(Ids[5]) == null)
            {
                Require(skills.AddSkill(new Hydromancy_ConjureRain()), "Fixture-only Rain learning failed.");
                report.rainLearnedForFixtureOnly = true;
            }
            input.ZoneRenderer.RenderZone();
        }
        static bool Open(Zone zone, Entity actor, int x, int y)
        {
            var cell = zone.GetCell(x, y);
            if (cell == null || cell.IsSolid() || cell.BlocksMovement(actor)) return false;
            foreach (var owner in cell.Occupants)
                if (owner != actor && AbilityTargeting.IsElementalTarget(owner, actor)) return false;
            return true;
        }
        static void RemoveOwned(Zone zone, Entity entity)
        { if (zone.GetEntityCell(entity) != null) Require(zone.RemoveEntity(entity), "Owned fixture removal failed."); }
        static void StartCast(Row row)
        {
            var zone = input.CurrentZone; var actor = input.PlayerEntity; var world = input.ZoneRenderer.WorldFx;
            Require(!world.HasBlockingFx && SpellFxBus.PendingCount == 0 && world.StarterAudio.ActiveVoices == 0 && world.EmberAudio.ActiveVoices == 0,
                "Previous visual/audio cast must finish naturally before fixture reset.");
            RemoveOwned(zone, dummy); RemoveOwned(zone, cropEntity); RemoveOwned(zone, blocker);
            // Only transient material in this owned save's selected fixture lane is reset; entities remain intact.
            for (int dx = 0; dx <= 6; dx++) for (int dy = -1; dy <= 1; dy++) zone.TileState.Clear(originX + dx, originY + dy);
            dummy.GetPart<StatusEffectsPart>().RemoveAllEffects(); dummy.GetStat("Hitpoints").Value = DurableHp;
            var brain = dummy.GetPart<BrainPart>(); brain.ClearGoals(); brain.PersonalEnemies.Clear(); brain.Target = null;
            int index = Array.IndexOf(Ids, row.spell);
            bool noBody = row.scenario == "water-only" || row.scenario == "dry-refusal" || index == 5;
            int distance = index == 0 ? 1 : index == 1 ? 2 : 3;
            if (!noBody) Require(zone.AddEntity(dummy, originX + distance, originY), "Owned target placement failed.");
            if (index == 5 && row.scenario != "no-crops")
            {
                crop.MoistureTicks = 0; crop.OnDriedOut();
                Require(zone.AddEntity(cropEntity, originX, originY + 1), "Owned crop placement failed.");
            }
            if (row.scenario == "already-peaceful") brain.PushGoal(new NoFightGoal(50, wander: false));
            if (row.scenario == "push-blocked") Require(zone.AddEntity(blocker, originX + distance + 1, originY), "Owned blocker placement failed.");
            if (row.scenario == "water-only") ZoneTileStateSystem.WriteCoating(zone, originX + 2, originY, "water", 6);
            input.ZoneRenderer.RenderZone();
            var source = zone.GetEntityCell(actor); Require(source != null && source.IsVisible && source.Explored, "Real FOV must contain caster.");
            if (!noBody) { var cell = zone.GetEntityCell(dummy); Require(cell.IsVisible && cell.Explored, "Real FOV must contain target."); }
            if (index == 5 && row.scenario != "no-crops") Require(zone.GetEntityCell(cropEntity).IsVisible, "Real FOV must contain crop.");
            var skills = actor.GetPart<SkillsPart>(); var skill = skills.GetSkill(row.spell);
            var ability = actor.GetPart<ActivatedAbilitiesPart>().GetAbility(skill.ActivatedAbilityID);
            Require(ability != null, "Actual activated ability missing."); ability.CooldownRemaining = 0;
            SpellFxSettings.SoundVolume = row.muted ? 0 : 1;
            beforeAccepted = world.StarterAudio.AcceptedCount; beforeContacts = world.StarterAudio.ContactLayerCount;
            beforeEmberAccepted = world.EmberAudio.AcceptedCount; beforeEmberImpacts = world.EmberAudio.ImpactCount;
            observedStems = new bool[Stems[index].Length]; observedPrefixes = new bool[Stems[index].Length];
            row.commandWallTime = EditorApplication.timeSinceStartup; row.commandDspTime = AudioSettings.dspTime;
            lastObservationWall = lastObservedFrameWall = row.commandWallTime; lastObservedFrame = Time.frameCount;
            row.consumed = skills.TryRouteSkillCommand(ability.Command, zone, Rng, 1, 0, source,
                zone.GetCell(originX + 1, originY), ability.Range, out row.blocks);
            row.commandCompletedWallAge = EditorApplication.timeSinceStartup - row.commandWallTime;
            row.pendingAfterCommand = SpellFxBus.PendingCount; report.commands++;
            Require(row.consumed == row.expectedConsumed && row.blocks == row.expectedConsumed
                && row.pendingAfterCommand == (row.expectedConsumed ? 1 : 0)
                && (row.expectedConsumed ? ability.CooldownRemaining > 0 : ability.CooldownRemaining == 0),
                row.spell + "/" + row.scenario + ": real command/cooldown/queue contract failed.");
            row.primarySuccess = PrimaryOutcome(row, index);
            Require(row.primarySuccess, row.spell + "/" + row.scenario + ": actual simulation outcome failed.");
        }
        static bool PrimaryOutcome(Row row, int index)
        {
            var zone = input.CurrentZone; int hp = dummy.GetStatValue("Hitpoints"); var position = zone.GetEntityPosition(dummy);
            if (row.scenario == "water-only") return zone.TileState.HasCoating(originX + 2, originY, "ice") && !zone.TileState.HasCoating(originX + 2, originY, "water");
            if (row.scenario == "dry-refusal") return !row.consumed && zone.TileState.Cold(originX + 2, originY) == 0;
            if (index == 0 || index == 6) return hp < DurableHp;
            if (index == 1) return hp < DurableHp && dummy.HasEffect<WetEffect>() && position.x == originX + 3;
            if (index == 2) return hp < DurableHp && dummy.HasEffect<ElectrifiedEffect>()
                && position.x == originX + (row.scenario == "push-blocked" ? 3 : 4);
            if (index == 3) return hp < DurableHp && dummy.HasEffect<FrozenEffect>();
            if (index == 4) return hp == DurableHp && dummy.GetPart<BrainPart>().HasGoal<NoFightGoal>();
            return row.scenario == "no-crops" ? zone.GetEntityCell(cropEntity) == null : crop.MoistureTicks == 40;
        }
        static void ObserveCurrent()
        {
            int index = Array.IndexOf(Ids, current.spell); double peak = 0;
            double wall = EditorApplication.timeSinceStartup, dsp = AudioSettings.dspTime;
            current.observedWallAge = wall - current.commandWallTime; current.observedDspAge = dsp - current.commandDspTime;
            current.maxPollGapSeconds = Math.Max(current.maxPollGapSeconds, wall - lastObservationWall);
            current.maxFrameGapSeconds = Math.Max(current.maxFrameGapSeconds, wall - lastObservedFrameWall);
            lastObservationWall = wall; current.polls++;
            if (Time.frameCount != lastObservedFrame)
            {
                current.maxUnscaledFrameDeltaSeconds = Math.Max(current.maxUnscaledFrameDeltaSeconds, Time.unscaledDeltaTime);
                lastObservedFrame = Time.frameCount; lastObservedFrameWall = wall;
            }
            AudioListener.GetOutputData(Output, 0);
            for (int i = 0; i < Output.Length; i++) peak = Math.Max(peak, Math.Abs(Output[i]));
            current.outputPeak = Math.Max(current.outputPeak, peak);
            var pool = index == 6 ? emberSources : starterSources;
            bool prefix = false, contact = false;
            for (int i = 0; i < pool.Length; i++)
            {
                var source = pool[i];
                if (source == null || source.clip == null || !source.isPlaying || source.timeSamples <= 0) continue;
                string name = source.clip.name;
                bool isPost = index == 6 ? name.Contains("impact") : name.EndsWith("_post", StringComparison.Ordinal);
                bool isPre = !isPost && (index == 6 || name.EndsWith("_pre", StringComparison.Ordinal));
                if (!isPost && !isPre) continue;
                // Independent source tap distinguishes a progressing clip from
                // a nonzero waveform; listener gating remains unchanged below.
                Array.Clear(SourceOutput, 0, SourceOutput.Length);
                source.GetOutputData(SourceOutput, 0);
                double sourcePeak = 0;
                for (int sample = 0; sample < SourceOutput.Length; sample++)
                    sourcePeak = Math.Max(sourcePeak, Math.Abs(SourceOutput[sample]));
                for (int k = 0; k < Stems[index].Length; k++)
                    if (name.Contains(Stems[index][k])) ObserveSource(source, Stems[index][k], isPost ? "post" : "pre", sourcePeak);
                if (isPost)
                {
                    contact = true;
                    current.contactSourceOutputPeak = Math.Max(current.contactSourceOutputPeak, sourcePeak);
                    for (int k = 0; k < Stems[index].Length; k++) if (name.Contains(Stems[index][k])) observedStems[k] = true;
                }
                else
                {
                    prefix = true;
                    current.prefixSourceOutputPeak = Math.Max(current.prefixSourceOutputPeak, sourcePeak);
                    current.prefixObservedMinVolume = current.prefixObservedMinVolume < 0 ? source.volume
                        : Math.Min(current.prefixObservedMinVolume, source.volume);
                    for (int k = 0; k < Stems[index].Length; k++) if (name.Contains(Stems[index][k])) observedPrefixes[k] = true;
                }
            }
            current.prefixProgress |= prefix; current.contactProgress |= contact;
            if (prefix)
            {
                current.prefixOutputPeak = Math.Max(current.prefixOutputPeak, peak); current.prefixPolls++;
                if (current.firstPrefixWallAge < 0)
                { current.firstPrefixWallAge = current.observedWallAge; current.firstPrefixDspAge = current.observedDspAge; }
                current.lastPrefixWallAge = current.observedWallAge; current.lastPrefixDspAge = current.observedDspAge;
            }
            if (contact)
            {
                current.contactOutputPeak = Math.Max(current.contactOutputPeak, peak); current.contactPolls++;
                if (current.firstContactWallAge < 0)
                { current.firstContactWallAge = current.observedWallAge; current.firstContactDspAge = current.observedDspAge; }
                current.lastContactWallAge = current.observedWallAge; current.lastContactDspAge = current.observedDspAge;
            }
        }
        static void ObserveSource(AudioSource source, string stem, string phase, double peak)
        {
            SourceObservation observation = null;
            for (int i = 0; i < current.sources.Count; i++)
                if (current.sources[i].stem == stem && current.sources[i].phase == phase)
                { observation = current.sources[i]; break; }
            if (observation == null)
            {
                observation = new SourceObservation { stem = stem, phase = phase, clip = source.clip.name,
                    firstTimeSamples = source.timeSamples, firstWallAge = current.observedWallAge, firstDspAge = current.observedDspAge };
                current.sources.Add(observation);
            }
            observation.observations++; observation.lastTimeSamples = source.timeSamples;
            observation.maximumTimeSamples = Math.Max(observation.maximumTimeSamples, source.timeSamples);
            observation.outputPeak = Math.Max(observation.outputPeak, peak);
            observation.minimumVolume = observation.minimumVolume < 0 ? source.volume : Math.Min(observation.minimumVolume, source.volume);
            observation.maximumVolume = Math.Max(observation.maximumVolume, source.volume);
            observation.minimumPitch = observation.minimumPitch < 0 ? source.pitch : Math.Min(observation.minimumPitch, source.pitch);
            observation.maximumPitch = Math.Max(observation.maximumPitch, source.pitch);
            observation.lastWallAge = current.observedWallAge; observation.lastDspAge = current.observedDspAge;
        }
        static void FinishRow()
        {
            var world = input.ZoneRenderer.WorldFx; int index = Array.IndexOf(Ids, current.spell);
            current.acceptedDelta = index == 6 ? world.EmberAudio.AcceptedCount - beforeEmberAccepted : world.StarterAudio.AcceptedCount - beforeAccepted;
            current.contactsDelta = index == 6 ? world.EmberAudio.ImpactCount - beforeEmberImpacts : world.StarterAudio.ContactLayerCount - beforeContacts;
            Require(index == 6 ? world.StarterAudio.AcceptedCount == beforeAccepted : world.EmberAudio.AcceptedCount == beforeEmberAccepted,
                current.spell + ": unrelated audio player accepted this cast.");
            current.naturalTailCleared = world.StarterAudio.ActiveVoices == 0 && world.EmberAudio.ActiveVoices == 0;
            var names = new List<string>(); for (int i = 0; i < observedStems.Length; i++) if (observedStems[i]) names.Add(Stems[index][i]);
            current.contactStemsObserved = names.ToArray();
            var prefixNames = new List<string>();
            for (int i = 0; i < observedPrefixes.Length; i++) if (observedPrefixes[i]) prefixNames.Add(Stems[index][i]);
            current.prefixStemsObserved = prefixNames.ToArray();
            if (current.expectAudio && index != 6)
            {
                current.variant = world.StarterAudio.LastVariant;
                Require(current.variant >= 1 && current.variant <= 3, current.spell + ": accepted variant missing.");
                if (index == 2) current.expectedContacts = (current.variant == 2 ? 4 : 3)
                    - (current.scenario == "push-blocked" ? 1 : 0);
            }
            Require(current.acceptedDelta == (current.expectAudio ? 1 : 0) && current.contactsDelta == current.expectedContacts,
                current.spell + "/" + current.scenario + ": copied-outcome audio counts failed; accepted=" + current.acceptedDelta + ", contacts=" + current.contactsDelta);
            if (current.expectAudio)
            {
                Require(current.contactProgress && current.contactOutputPeak > 1e-5, current.spell + ": actual contact DSP/source output missing.");
                if (index != 5) Require(current.prefixProgress && current.prefixOutputPeak > 1e-5, current.spell + ": actual prefix DSP/source output missing.");
                if (index != 6) Require(names.Count == current.expectedContacts, current.spell + ": did not observe each eligible recorded suffix advancing.");
                if (index == 2)
                {
                    Require(prefixNames.Contains("charging_current"), "Surge charging material must actually advance in its prefix.");
                    Require(names.Contains("charging_current") == (current.variant == 2),
                        "Only Surge variant 2 contains recorded charging material after contact; silent suffixes must not be fabricated.");
                }
                if (current.scenario == "already-peaceful") Require(!names.Contains("resolved_overtone"), "Already-peaceful target played pacification success.");
                if (current.scenario == "push-blocked") Require(!names.Contains("grit_displacement"), "Blocked target played displacement success.");
            }
            else Require(!current.prefixProgress && !current.contactProgress && current.outputPeak < 1e-6,
                current.spell + "/" + current.scenario + ": silent control produced audio.");
            Require(current.naturalTailCleared, current.spell + ": audio tail did not finish naturally."); current.passed = true;
        }
        static void Finish(int code)
        {
            if (!SessionState.GetBool(Prefix + "active", false)) return;
            EditorApplication.update -= Poll; GameBootstrap.OnAfterBootstrap -= Boot; Application.logMessageReceived -= Log;
            // Snapshot the real partial state before CancelAll changes counts and
            // lifetimes. A failed early profile remains explicitly incomplete.
            if (report != null)
            {
                report.dspEnd = AudioSettings.dspTime;
                if (report.profileStarted) report.profileSeconds = EditorApplication.timeSinceStartup - profileStart;
                report.updateMeanMicroseconds = report.profileSamples > 0 ? report.updateTotalMicroseconds / report.profileSamples : 0;
                report.loggedErrors = LoggedErrors.ToArray();
                var world = input != null ? input.ZoneRenderer?.WorldFx : null;
                if (world != null)
                {
                    report.starterAccepted = world.StarterAudio.AcceptedCount; report.starterContactLayers = world.StarterAudio.ContactLayerCount;
                    report.emberAccepted = world.EmberAudio.AcceptedCount; report.emberImpacts = world.EmberAudio.ImpactCount;
                    report.poolStable = world.StarterAudio.AllocatedSources == 32 && world.EmberAudio.AllocatedSources == 12;
                    report.tailCleared = world.StarterAudio.ActiveVoices == 0 && world.EmberAudio.ActiveVoices == 0;
                }
            }
            if (recorder.Valid) recorder.Dispose();
            if (settingsCaptured)
            {
                input?.ZoneRenderer?.WorldFx?.CancelAll();
                SpellFxSettings.SoundVolume = oldVolume; SpellFxSettings.AnimationSpeed = oldSpeed; SpellFxSettings.Mode = oldMode;
                Village3DSettings.Enabled = oldNative; Application.runInBackground = oldBackground;
                AudioListener.pause = oldListenerPause; AudioListener.volume = oldListenerVolume;
            }
            if (inputStateCaptured && input != null)
            {
                input.enabled = oldInputEnabled;
                if (report != null) report.inputEnabledRestored = input.enabled == oldInputEnabled;
            }
            NativeAuditBootstrapSettings.RequestedSeed = SessionState.GetInt(Prefix + "seed", 0);
            if (report != null)
            {
                report.unexpectedErrors = SessionState.GetInt(Prefix + "errors", 0); if (report.unexpectedErrors > 0) code = 1;
                report.passed = code == 0;
                string directory = Path.GetFullPath("Docs/Verification/StarterSpellAudioIntegration/Native");
                Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, report.runId + ".json"), JsonUtility.ToJson(report, true));
            }
            SessionState.SetBool(Prefix + "active", false); SaveGameService.RegisterRuntime(null, null);
            NativeSaveIsolation.Finish(Prefix, SessionState.GetString(Prefix + "token", ""), code);
        }
        static void Require(bool pass, string message) { if (!pass) throw new InvalidOperationException(message); }
    }
}
