// Unity MCP execute_code body. See the README for the four input bindings.
var handler = UnityEngine.Object.FindFirstObjectByType<CavesOfOoo.Rendering.InputHandler>();
var keyboard = UnityEngine.InputSystem.InputSystem.devices.OfType<UnityEngine.InputSystem.Keyboard>()
    .SingleOrDefault(device => device.name == keyboardName && device.enabled);
if (!EditorApplication.isPlaying || handler == null || keyboard == null ||
    !SessionState.GetBool("CavesOfOoo.NativeSaveIsolation.active", false) ||
    string.IsNullOrEmpty(CavesOfOoo.Core.SaveGameService.SaveRootOverride) ||
    SessionState.GetString("CavesOfOoo.NativeSaveIsolation.owner", "") != auditOwner ||
    UnityEngine.InputSystem.InputSystem.settings.updateMode !=
        UnityEngine.InputSystem.InputSettings.UpdateMode.ProcessEventsInDynamicUpdate ||
    SessionState.GetBool(auditOwner + "keyJob", false) || moves == null || moves.Length < 1 || moves.Length > 60)
    throw new InvalidOperationException("Guarded walk requires an idle, owned native playtest and up to 60 moves.");

var bindings = new Dictionary<UnityEngine.InputSystem.Key, int[]> {
    { UnityEngine.InputSystem.Key.LeftArrow, new[] { -1, 0 } },
    { UnityEngine.InputSystem.Key.RightArrow, new[] { 1, 0 } },
    { UnityEngine.InputSystem.Key.UpArrow, new[] { 0, -1 } },
    { UnityEngine.InputSystem.Key.DownArrow, new[] { 0, 1 } }
};
if (moves.Any(move => !bindings.ContainsKey(move)))
    throw new ArgumentException("Only the four cardinal arrow keys are supported.", "moves");

var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var mode = handler.GetType().GetField("_inputState", flags);
var pause = handler.GetType().GetField("_pauseMenuController", flags);
var lastMove = handler.GetType().GetField("_lastMoveTime", flags);
if (mode == null || pause == null || lastMove == null)
    throw new InvalidOperationException("Input inspection fields changed. Update the helper before walking.");

var actor = handler.PlayerEntity;
var zone = handler.CurrentZone;
var initial = zone?.GetEntityCell(actor);
if (initial == null || zone.ZoneID != expectedZone || !actor.HasTag("Player"))
    throw new InvalidOperationException("Unexpected starting world location.");

var steps = new List<object>();
var privateRoot = CavesOfOoo.Core.SaveGameService.SaveRootOverride;
int startX = initial.X, startY = initial.Y, targetX = startX, targetY = startY;
int tick = handler.TurnManager.TickCount, health = actor.GetStatValue("Hitpoints"), frame = -1;
double started = EditorApplication.timeSinceStartup, pressed = 0, readySince = started;
int pulsePhase = 0;
bool awaitingResult = false;
UnityEngine.InputSystem.Key pulseKey = UnityEngine.InputSystem.Key.None;
EditorApplication.CallbackFunction update = null;
Action release = () => {
    if (pulsePhase == 2 && keyboard.added)
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
    pulsePhase = 0;
};
// Use consecutive input frames for press and release. Editor polling can
// lag, and does not control how long a movement key stays held.
Action beforeInput = () => {
    if (!keyboard.added || UnityEngine.InputSystem.LowLevel.InputState.currentUpdateType !=
        UnityEngine.InputSystem.LowLevel.InputUpdateType.Dynamic) return;
    if (pulsePhase == 2) { release(); return; }
    if (pulsePhase != 1) return;
    keyboard.MakeCurrent();
    UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(pulseKey));
    pulsePhase = 2;
};
Action<string> finish = reason => {
    EditorApplication.update -= update;
    UnityEngine.InputSystem.InputSystem.onBeforeUpdate -= beforeInput;
    SessionState.SetBool(auditOwner + "keyJob", false);
    release();
    SessionState.SetString(auditOwner + "guardedWalk", Newtonsoft.Json.JsonConvert.SerializeObject(new {
        reason, expectedZone, startX = initial.X, startY = initial.Y, requested = moves.Length, completed = steps.Count,
        seconds = EditorApplication.timeSinceStartup - started, steps
    }));
};
update = () => {
    try {
        if (!EditorApplication.isPlaying || handler == null || !keyboard.added ||
            handler.PlayerEntity != actor || handler.CurrentZone != zone ||
            CavesOfOoo.Core.SaveGameService.SaveRootOverride != privateRoot ||
            mode.GetValue(handler)?.ToString() != "Normal" ||
            handler.ZoneRenderer?.Paused == true ||
            (pause.GetValue(handler) as CavesOfOoo.Rendering.PauseMenuController)?.IsOpen == true ||
            actor.GetStatValue("Hitpoints") != health) {
            finish("play state changed");
            return;
        }
        var at = zone.GetEntityCell(actor);
        int nowTick = handler.TurnManager.TickCount;
        double now = EditorApplication.timeSinceStartup;
        if (awaitingResult && (at.X != startX || at.Y != startY || nowTick != tick)) {
            release();
            awaitingResult = false;
            if (at.X != targetX || at.Y != targetY || nowTick <= tick) { finish("step diverged"); return; }
            steps.Add(new { key = moves[steps.Count].ToString(), x = at.X, y = at.Y, tick = nowTick, hp = health });
            startX = at.X;
            startY = at.Y;
            tick = nowTick;
            frame = UnityEngine.Time.frameCount;
            readySince = now;
        }
        if (steps.Count == moves.Length) { finish("complete"); return; }
        if (awaitingResult) {
            if (now - pressed > .35) finish("movement received no verified step");
            return;
        }
        if (now - readySince > 3) { finish("movement did not become ready"); return; }
        if (UnityEngine.Time.frameCount == frame ||
            !handler.CanShowCombatIntent(zone, actor) ||
            UnityEngine.Time.time - (float)lastMove.GetValue(handler) < handler.MoveRepeatDelay ||
            handler.ZoneRenderer?.WorldFx?.HasBlockingFx == true) return;

        var move = moves[steps.Count];
        var delta = bindings[move];
        targetX = at.X + delta[0];
        targetY = at.Y + delta[1];
        var target = zone.GetCell(targetX, targetY);
        var ground = target == null ? null : zone.TileState.Get(targetX, targetY);
        if (target == null || target.BlocksMovement(actor) || target.Occupants.Any(e =>
            e.HasTag("Creature") || e.HasPart<CavesOfOoo.Core.TriggerOnStepPart>() ||
            e.HasPart<CavesOfOoo.Core.LiquidPoolPart>() || e.HasPart<CavesOfOoo.Core.GasPoolPart>()) ||
            (ground != null && (ground.Heat != 0 || ground.Cold != 0 || ground.Charge != 0 ||
                !string.IsNullOrEmpty(ground.Cloud) || ground.Coatings.Count > 0))) {
            finish("destination obstructed or requires manual input");
            return;
        }
        pulseKey = move;
        pulsePhase = 1;
        pressed = now;
        awaitingResult = true;
    } catch (Exception failure) { finish("stopped: " + failure.Message); }
};
SessionState.EraseString(auditOwner + "guardedWalk");
SessionState.SetBool(auditOwner + "keyJob", true);
UnityEngine.InputSystem.InputSystem.onBeforeUpdate += beforeInput;
EditorApplication.update += update;
return moves.Length;
