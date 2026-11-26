// Unity MCP execute_code body. See the README for the three input bindings.
var handler = UnityEngine.Object.FindFirstObjectByType<CavesOfOoo.Rendering.InputHandler>();
var keyboard = UnityEngine.InputSystem.InputSystem.devices.OfType<UnityEngine.InputSystem.Keyboard>()
    .SingleOrDefault(device => device.name == keyboardName && device.enabled);
var keys = new Dictionary<char, UnityEngine.InputSystem.Key> {
    { ' ', UnityEngine.InputSystem.Key.Space },
    { '-', UnityEngine.InputSystem.Key.Minus },
    { '\'', UnityEngine.InputSystem.Key.Quote }
};
for (char letter = 'a'; letter <= 'z'; letter++)
    keys.Add(letter, UnityEngine.InputSystem.Key.A + (letter - 'a'));
for (char digit = '0'; digit <= '9'; digit++)
    keys.Add(digit, (UnityEngine.InputSystem.Key)Enum.Parse(typeof(UnityEngine.InputSystem.Key), "Digit" + digit));
if (string.IsNullOrEmpty(searchText) || searchText.Length > 48 || searchText.Any(letter => !keys.ContainsKey(letter)))
    throw new ArgumentException("Use 1 to 48 supported inventory search characters.", "searchText");

var privateRoot = CavesOfOoo.Core.SaveGameService.SaveRootOverride;
Func<bool> ownsPlaytest = () => EditorApplication.isPlaying && keyboard != null && keyboard.added && keyboard.enabled &&
    UnityEngine.InputSystem.InputSystem.settings.updateMode ==
        UnityEngine.InputSystem.InputSettings.UpdateMode.ProcessEventsInDynamicUpdate &&
    SessionState.GetBool("CavesOfOoo.NativeSaveIsolation.active", false) &&
    SessionState.GetString("CavesOfOoo.NativeSaveIsolation.owner", "") == auditOwner &&
    !string.IsNullOrEmpty(privateRoot) && CavesOfOoo.Core.SaveGameService.SaveRootOverride == privateRoot;
if (!ownsPlaytest() || handler == null || handler.InventoryUI == null ||
    SessionState.GetBool(auditOwner + "keyJob", false))
    throw new InvalidOperationException("Inventory search requires an idle, owned native playtest.");

var inventory = handler.InventoryUI;
var actor = handler.PlayerEntity;
var zone = handler.CurrentZone;
int tick = handler.TurnManager.TickCount;
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var mode = handler.GetType().GetField("_inputState", flags);
var searching = inventory.GetType().GetField("_inventorySearchActive", flags);
var query = inventory.GetType().GetField("_inventoryQuery", flags);
var popup = inventory.GetType().GetProperty("LedgerHasPopup", flags);
if (mode == null || searching == null || query == null || popup == null)
    throw new InvalidOperationException("Inventory search inspection fields changed.");
Func<bool> canType = () => ownsPlaytest() && handler != null && handler.InventoryUI == inventory &&
    handler.PlayerEntity == actor && handler.CurrentZone == zone && handler.TurnManager.TickCount == tick &&
    mode.GetValue(handler)?.ToString() == "InventoryOpen" && inventory.IsOpen &&
    !(bool)popup.GetValue(inventory) && (bool)searching.GetValue(inventory);
Func<string> readQuery = () => (string)query.GetValue(inventory);
if (!canType() || readQuery() != "")
    throw new InvalidOperationException("Open the inventory search with an empty query first.");

int completed = 0, phase = 0;
bool awaiting = false;
double started = EditorApplication.timeSinceStartup, requestedAt = started;
EditorApplication.CallbackFunction update = null;
Action release = () => {
    if (phase == 2 && keyboard.added)
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
    phase = 0;
};
Action beforeInput = null;
Action<string> finish = reason => {
    EditorApplication.update -= update;
    UnityEngine.InputSystem.InputSystem.onBeforeUpdate -= beforeInput;
    release();
    SessionState.SetBool(auditOwner + "keyJob", false);
    SessionState.SetString(auditOwner + "inventorySearch", Newtonsoft.Json.JsonConvert.SerializeObject(new {
        reason, requested = searchText, confirmed = searchText.Substring(0, completed), tick,
        seconds = EditorApplication.timeSinceStartup - started
    }));
};
beforeInput = () => {
    try {
        if (UnityEngine.InputSystem.LowLevel.InputState.currentUpdateType !=
            UnityEngine.InputSystem.LowLevel.InputUpdateType.Dynamic) return;
        if (phase == 2) { release(); return; }
        if (phase != 1 || !canType() || readQuery() != searchText.Substring(0, completed)) return;
        keyboard.MakeCurrent();
        UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,
            new UnityEngine.InputSystem.LowLevel.KeyboardState(keys[searchText[completed]]));
        phase = 2;
    } catch (Exception failure) { finish("stopped: " + failure.Message); }
};
update = () => {
    try {
        if (!canType()) { finish("play state changed"); return; }
        string current = readQuery();
        string prefix = searchText.Substring(0, completed);
        if (awaiting && current == searchText.Substring(0, completed + 1)) {
            completed++;
            awaiting = false;
        } else if (current != prefix) { finish("query changed unexpectedly"); return; }
        if (completed == searchText.Length && phase == 0) { finish("complete"); return; }
        double now = EditorApplication.timeSinceStartup;
        if (!awaiting && phase == 0) {
            requestedAt = now;
            awaiting = true;
            phase = 1;
        } else if (now - requestedAt > 1) { finish("search input did not settle"); }
    } catch (Exception failure) { finish("stopped: " + failure.Message); }
};
SessionState.EraseString(auditOwner + "inventorySearch");
SessionState.SetBool(auditOwner + "keyJob", true);
UnityEngine.InputSystem.InputSystem.onBeforeUpdate += beforeInput;
EditorApplication.update += update;
return searchText.Length;
